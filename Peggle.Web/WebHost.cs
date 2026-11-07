using System;
using System.Runtime.InteropServices;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;
using Sexy;

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

        /// <summary>Decodes an image on the CPU, straight alpha packed as SurfaceFormat.Color.</summary>
        private static uint[] DecodeImage(byte[] theFileData, out int theWidth, out int theHeight)
        {
            StbImageSharp.ImageResult anImage = StbImageSharp.ImageResult.FromMemory(theFileData, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
            theWidth = anImage.Width;
            theHeight = anImage.Height;
            return MemoryMarshal.Cast<byte, uint>(anImage.Data).ToArray();
        }
    }
}
