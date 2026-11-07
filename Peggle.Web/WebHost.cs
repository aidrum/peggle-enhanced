using System;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;
using Sexy;
using SkiaSharp;

namespace Peggle.Web
{
    /// <summary>Sets the game up for the browser, as GameActivity (Android) and AppDelegate (iOS) do.</summary>
    internal static class WebHost
    {
        public static Sexy.Main CreateGame(IJSInProcessRuntime theJs)
        {
            // The game data is downloaded from the site's Content/ folder as it's needed.
            ContentFS.Root = "Content";
            ContentFS.Source = new WebContentSource();
            ImageFileLoader.PlatformDecoder = DecodeImage;

            // Phones and tablets play as the mobile versions (touch, no PC Controls); browsers with a mouse as
            // the desktop ones.
            Sexy.Main.IsTouchWeb = theJs.Invoke<bool>("peggleWeb.isTouch");
            int[] aSize = theJs.Invoke<int[]>("peggleWeb.canvasSize");
            Sexy.Main.NativeScreenSize = new Point(Math.Max(aSize[0], 1), Math.Max(aSize[1], 1));
            if (Sexy.Main.IsTouchWeb)
            {
                // Text fields ask with the browser's prompt, as the phones open their keyboard.
                EditManager.TextPromptHandler = (theText, theDone) =>
                    theDone(theJs.Invoke<string>("peggleWeb.prompt", theText ?? ""));
            }
            Sexy.Main.OpenUrlHandler = theUrl => theJs.InvokeVoid("peggleWeb.openUrl", theUrl);
            // A page can't close itself: quitting saves and starts over.
            Sexy.Main.ExitHandler = () =>
            {
                WebSaves.Flush(theJs);
                theJs.InvokeVoid("peggleWeb.reload");
            };
            Sexy.Main.AppFactory = theGame => new ThunderballApp(theGame);

            Sexy.Main aGame = new Sexy.Main();
            aGame.applicationStoragePath = WebSaves.Dir;
#if PEGGLE_IPHONE
            aGame.Window.Title = "Peggle";
#else
            aGame.Window.Title = "Peggle HD";
#endif
            return aGame;
        }

        /// <summary>
        /// Decodes an image (PNG, WebP, JPEG, GIF) with SkiaSharp, straight into straight-alpha pixels packed as
        /// SurfaceFormat.Color (RGBA bytes); null hands it to the default loader.
        /// </summary>
        private static unsafe uint[] DecodeImage(byte[] theFileData, out int theWidth, out int theHeight)
        {
            theWidth = 0;
            theHeight = 0;
            using (SKData aData = SKData.CreateCopy(theFileData))
            using (SKCodec aCodec = SKCodec.Create(aData))
            {
                if (aCodec == null)
                {
                    return null;
                }
                SKImageInfo anInfo = new SKImageInfo(aCodec.Info.Width, aCodec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                uint[] aPixels = new uint[anInfo.Width * anInfo.Height];
                fixed (uint* aDest = aPixels)
                {
                    SKCodecResult aResult = aCodec.GetPixels(anInfo, (System.IntPtr)aDest);
                    if (aResult != SKCodecResult.Success && aResult != SKCodecResult.IncompleteInput)
                    {
                        return null;
                    }
                }
                theWidth = anInfo.Width;
                theHeight = anInfo.Height;
                return aPixels;
            }
        }
    }
}
