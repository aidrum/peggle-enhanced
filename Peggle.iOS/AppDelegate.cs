using System;
using System.IO;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Sexy;
using StbImageSharp;
using UIKit;

namespace Peggle.iOS
{
    /// <summary>
    /// iOS host (MonoGame's iOS template): UIKit starts this app delegate, which runs the game. Mirrors
    /// Peggle.Android's GameActivity: the game data is read as loose files from the app bundle's Content
    /// folder (ContentFS's default), saves go to the app's Documents folder (visible in the Files app).
    /// </summary>
    [Register("AppDelegate")]
    internal class AppDelegate : UIApplicationDelegate
    {
        // Held for the app's lifetime; UIKit keeps running the game loop after FinishedLaunching returns.
        private static Sexy.Main sGame;

        private static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }

        public override void FinishedLaunching(UIApplication app)
        {
            string saveDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(saveDir);
            // Port addition: custom level packs (.pak, CustomLevelPacks) go in Documents/levelpacks, which the
            // Files app shows under On My iPhone > Peggle (UIFileSharingEnabled, LSSupportsOpeningDocumentsInPlace).
            string levelPackDir = Path.Combine(saveDir, "levelpacks");
            Directory.CreateDirectory(levelPackDir);
            CustomLevelPacks.sFolders.Add(levelPackDir);

            // Text fields open the system keyboard prompt, as the original showed its keyboard.
            EditManager.TextPromptHandler = (theText, theDone) =>
                KeyboardInput.Show("Peggle", "", theText).ContinueWith(t => theDone(t.IsCompletedSuccessfully ? t.Result : null));

            ImageFileLoader.PlatformDecoder = DecodeImage;
            Sexy.Main.NativeScreenSize = GetScreenSize();
            Sexy.Main.AppFactory = game => new ThunderballApp(game);
            // iOS apps aren't supposed to quit themselves (Game.Exit does nothing there), but the game has
            // already saved and shut down when QUIT is chosen, so the process ends as on Android.
            Sexy.Main.ExitHandler = () => Environment.Exit(0);
            // Links (More Games) open in Safari / the App Store.
            Sexy.Main.OpenUrlHandler = theUrl => app.BeginInvokeOnMainThread(() =>
            {
                NSUrl url = NSUrl.FromString(theUrl);
                if (url != null)
                {
                    app.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
                }
            });
            if (app.SupportsAlternateIcons)
            {
                SetUpAppIcons(app);
            }

            sGame = new Sexy.Main();
            sGame.applicationStoragePath = saveDir;
            GraphicsState.mGraphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            sGame.Run();
        }

        /// <summary>
        /// The App Icon page (Extra Options): the aliases are the alternate icon sets' names (Peggle.iOS.csproj's
        /// AlternateAppIcon), the first one (IconIOS) being the main icon. The switch happens in place, with
        /// iOS's own "You have changed the icon" notice, so the game neither confirms nor closes.
        /// </summary>
        private static void SetUpAppIcons(UIApplication app)
        {
            Sexy.Main.AppIconChangeQuits = false;
            Sexy.Main.TransparentAppIcons = false;
            Sexy.Main.SetAppIconHandler = (theAlias, theAll) => app.BeginInvokeOnMainThread(() =>
            {
                string aName = theAlias == theAll[0] ? null : theAlias;
                app.SetAlternateIconName(aName, theError =>
                {
                    if (theError != null)
                    {
                        Console.WriteLine("Could not switch the app icon: " + theError.LocalizedDescription);
                    }
                });
            });
            Sexy.Main.GetAppIconHandler = theAll => app.AlternateIconName ?? theAll[0];
        }

        /// <summary>The whole screen in landscape, in pixels.</summary>
        private static Point GetScreenSize()
        {
            UIScreen screen = UIScreen.MainScreen;
            int w = (int)Math.Round(screen.Bounds.Width * screen.Scale);
            int h = (int)Math.Round(screen.Bounds.Height * screen.Scale);
            return new Point(Math.Max(w, h), Math.Min(w, h));
        }

        /// <summary>
        /// Decodes an image file's bytes on the CPU into straight-alpha pixels packed as SurfaceFormat.Color (RGBA
        /// bytes, i.e. ABGR in a little-endian uint). MonoGame's iOS Texture2D.FromStream isn't used because it
        /// goes through CoreGraphics, which premultiplies alpha.
        /// </summary>
        private static uint[] DecodeImage(byte[] theFileData, out int theWidth, out int theHeight)
        {
            return IsJpeg(theFileData)
                ? DecodeOpaqueImage(theFileData, out theWidth, out theHeight)
                : DecodeWithStb(theFileData, out theWidth, out theHeight);
        }

        private static bool IsJpeg(byte[] theFileData)
        {
            return theFileData.Length > 3 && theFileData[0] == 0xFF && theFileData[1] == 0xD8 && theFileData[2] == 0xFF;
        }

        /// <summary>
        /// JPEGs (the level backgrounds) are decoded by CoreGraphics: StbImageSharp's JPEG decoder corrupts blocks
        /// of them into bright green on iOS, though it decodes the same files correctly on desktop .NET. A JPEG has
        /// no alpha, so CoreGraphics' premultiplied output is exact here.
        /// </summary>
        private static uint[] DecodeOpaqueImage(byte[] theFileData, out int theWidth, out int theHeight)
        {
            theWidth = 0;
            theHeight = 0;
            using (CGDataProvider provider = new CGDataProvider(theFileData))
            using (CGImage image = CGImage.FromJPEG(provider, null, false, CGColorRenderingIntent.Default))
            {
                if (image == null)
                {
                    return null;
                }
                int w = (int)image.Width;
                int h = (int)image.Height;
                // Drawn into an sRGB RGBX buffer (also converts grayscale or profiled JPEGs).
                byte[] rgbx = new byte[w * h * 4];
                GCHandle pin = GCHandle.Alloc(rgbx, GCHandleType.Pinned);
                try
                {
                    using (CGColorSpace colorSpace = CGColorSpace.CreateSrgb())
                    using (CGBitmapContext context = new CGBitmapContext(pin.AddrOfPinnedObject(), w, h, 8, w * 4, colorSpace, CGImageAlphaInfo.NoneSkipLast))
                    {
                        context.DrawImage(new CGRect(0, 0, w, h), image);
                    }
                }
                finally
                {
                    pin.Free();
                }
                uint[] pixels = new uint[w * h];
                System.Buffer.BlockCopy(rgbx, 0, pixels, 0, rgbx.Length);
                for (int i = 0; i < pixels.Length; i++)
                {
                    // The skipped fourth byte is undefined; the image is opaque.
                    pixels[i] |= 0xFF000000u;
                }
                theWidth = w;
                theHeight = h;
                return pixels;
            }
        }

        /// <summary>Everything else (PNGs, which may have alpha) is decoded by StbImageSharp, in straight alpha.</summary>
        private static uint[] DecodeWithStb(byte[] theFileData, out int theWidth, out int theHeight)
        {
            theWidth = 0;
            theHeight = 0;
            ImageResult image;
            try
            {
                image = ImageResult.FromMemory(theFileData, ColorComponents.RedGreenBlueAlpha);
            }
            catch (Exception)
            {
                return null;
            }
            if (image == null)
            {
                return null;
            }
            theWidth = image.Width;
            theHeight = image.Height;
            uint[] pixels = new uint[theWidth * theHeight];
            System.Buffer.BlockCopy(image.Data, 0, pixels, 0, pixels.Length * 4);
            return pixels;
        }
    }
}
