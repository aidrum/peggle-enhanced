#if PEGGLE_IPHONE
namespace Sexy
{
    /// <summary>
    /// Port addition (ultrawide, iPhone only): draws the full-width (1136) backdrop layers of the loading
    /// screen and main menu so they cover a window wider than the game. The art is centred on the game;
    /// on a game narrower than the art (3:2 to 16:9) it overhangs both sides, so a window up to the art's
    /// width just shows more of it, and only a wider one moves pieces out. At 16:9 in a 16:9 window every
    /// piece lands where the whole image does. theScreenX is the x, in g's coordinates, of the game
    /// area's left edge.
    /// </summary>
    public static class Ultrawide
    {
        /// <summary>How much of each side of the bottom front layer stays unscaled at the window's corners.</summary>
        public const int FRONT_BOTTOM_SIDE_W = 367;

        /// <summary>
        /// g, or an unclipped copy when the window reaches past the game; theWindowX/theWindowW is the
        /// window's span in g's coordinates, and theArtX where the art's left edge goes when centred.
        /// </summary>
        private static Graphics Wide(Graphics g, Image theImage, int theScreenX, out int theWindowX, out int theWindowW, out int theArtX)
        {
            int aGameW = GlobalStaticVars.gSexyAppBase.mWidth;
            ThunderDialog.GetWindowMargins(out int aLeft, out int aRight);
            theWindowX = theScreenX - aLeft;
            theWindowW = aGameW + aLeft + aRight;
            theArtX = theScreenX + (aGameW - theImage.mWidth) / 2;
            if (aLeft == 0 && aRight == 0)
            {
                return g;
            }
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            return aWide;
        }

        /// <summary>True when theImage is backdrop art but the window is wider than it, so it is spread out.</summary>
        private static bool Spreads(Image theImage, int theWindowW)
        {
            return theImage.mWidth >= GlobalStaticVars.gSexyAppBase.mWidth && theWindowW > theImage.mWidth;
        }

        /// <summary>The farthest layer: zoomed (about its centre) until it covers the window's width.</summary>
        public static void DrawBack(Graphics g, Image theImage, int theScreenX, int theY)
        {
            DrawBack(g, theImage, theScreenX, theY, out _, out _, out _);
        }

        /// <summary>
        /// DrawBack, giving the Graphics it drew with and where the image went: its top-left and zoom, for
        /// what is drawn over it (the Nights fireplace).
        /// </summary>
        public static Graphics DrawBack(Graphics g, Image theImage, int theScreenX, int theY, out int theLeft, out int theTop, out float theZoom)
        {
            Graphics aWide = Wide(g, theImage, theScreenX, out int aWindowX, out int aWindowW, out int anArtX);
            if (!Spreads(theImage, aWindowW))
            {
                theLeft = anArtX;
                theTop = theY;
                theZoom = 1;
                aWide.DrawImage(theImage, anArtX, theY);
                return aWide;
            }
            float aScale = aWindowW / (float)theImage.mWidth;
            int aWidth = (int)System.Math.Ceiling(theImage.mWidth * aScale);
            int aHeight = (int)System.Math.Ceiling(theImage.mHeight * aScale);
            theLeft = aWindowX - (aWidth - aWindowW) / 2;
            theTop = theY - (aHeight - theImage.mHeight) / 2;
            theZoom = aWidth / (float)theImage.mWidth;
            aWide.DrawImage(theImage, theLeft, theTop, aWidth, aHeight);
            return aWide;
        }

        /// <summary>The middle layer: stretched sideways to the window's width, at its own height.</summary>
        public static void DrawMid(Graphics g, Image theImage, int theScreenX, int theY)
        {
            Graphics aWide = Wide(g, theImage, theScreenX, out int aWindowX, out int aWindowW, out int anArtX);
            if (!Spreads(theImage, aWindowW))
            {
                aWide.DrawImage(theImage, anArtX, theY);
                return;
            }
            aWide.DrawImage(theImage, aWindowX, theY, aWindowW, theImage.mHeight);
        }

        /// <summary>The top front layer: split in half, each half in its own corner of the window.</summary>
        public static void DrawFrontTop(Graphics g, Image theImage, int theScreenX, int theY)
        {
            Graphics aWide = Wide(g, theImage, theScreenX, out int aWindowX, out int aWindowW, out int anArtX);
            if (!Spreads(theImage, aWindowW))
            {
                aWide.DrawImage(theImage, anArtX, theY);
                return;
            }
            int aHalf = theImage.mWidth / 2;
            int aRightW = theImage.mWidth - aHalf;
            int h = theImage.mHeight;
            aWide.DrawImage(theImage, new TRect(aWindowX, theY, aHalf, h), new TRect(0, 0, aHalf, h));
            aWide.DrawImage(theImage, new TRect(aWindowX + aWindowW - aRightW, theY, aRightW, h), new TRect(aHalf, 0, aRightW, h));
        }

        /// <summary>
        /// The bottom front layer: its outer FRONT_BOTTOM_SIDE_W pixels on each side go to the window's
        /// corners (under the top layer's halves), and the middle stretches to join them.
        /// </summary>
        public static void DrawFrontBottom(Graphics g, Image theImage, int theScreenX, int theY)
        {
            Graphics aWide = Wide(g, theImage, theScreenX, out int aWindowX, out int aWindowW, out int anArtX);
            if (!Spreads(theImage, aWindowW))
            {
                aWide.DrawImage(theImage, anArtX, theY);
                return;
            }
            int aSide = FRONT_BOTTOM_SIDE_W;
            int aMidW = theImage.mWidth - aSide * 2;
            int h = theImage.mHeight;
            int aLeftX = aWindowX;
            int aRightX = aWindowX + aWindowW - aSide;
            // The middle reaches a pixel under each side piece, so no seam shows.
            aWide.DrawImage(theImage, new TRect(aLeftX + aSide - 1, theY, aRightX - (aLeftX + aSide) + 2, h), new TRect(aSide, 0, aMidW, h));
            aWide.DrawImage(theImage, new TRect(aLeftX, theY, aSide, h), new TRect(0, 0, aSide, h));
            aWide.DrawImage(theImage, new TRect(aRightX, theY, aSide, h), new TRect(theImage.mWidth - aSide, 0, aSide, h));
        }
    }
}
#endif
