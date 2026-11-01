using System;
using System.IO;
using Foundation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Sexy;
using StbImageSharp;
using UIKit;

namespace Peggle.iOS
{
    /// <summary>
    /// iOS host (MonoGame's iOS template): UIKit starts the app delegate, which runs the game. Mirrors
    /// Peggle.Android's GameActivity: the game data is read as loose files from the app bundle's Content
    /// folder (ContentFS's default), saves go to the app's Documents folder (visible in the Files app).
    /// </summary>
    [Register("AppDelegate")]
    internal class Program : UIApplicationDelegate
    {
        private static Sexy.Main sGame;

        private static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(Program));
        }

        public override void FinishedLaunching(UIApplication app)
        {
            string saveDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(saveDir);

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

            sGame = new Sexy.Main();
            sGame.applicationStoragePath = saveDir;
            GraphicsState.mGraphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            sGame.Run();
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
        /// Decodes an image file's bytes on the CPU (StbImageSharp). MonoGame's iOS Texture2D.FromStream goes
        /// through CoreGraphics, which premultiplies alpha; the loader wants straight alpha, packed as
        /// SurfaceFormat.Color (RGBA bytes, i.e. ABGR in a little-endian uint).
        /// </summary>
        private static uint[] DecodeImage(byte[] theFileData, out int theWidth, out int theHeight)
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
