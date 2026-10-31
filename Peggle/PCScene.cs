using System;

namespace Sexy
{
    /// <summary>
    /// Port addition: where the PC intros' 800x600 scenes (NightsLoadingScreen, WoWLoadingScreen) go. The
    /// scene is the PC screen scaled to the display height and centred (a 4:3 box), with nothing clipped to
    /// it. On iPhone the backdrop layers also spread over the whole window (the game and any ultrawide
    /// margins), as the iOS menus' backdrop does (Ultrawide): far layers zoom about the PC screen's centre,
    /// middle ones stretch sideways, front ones split with their sides at the window's edges. On iPad every
    /// layer just sits in the box. Positions are PC pixels; sizes are the sheet's (already scaled by S).
    /// </summary>
    public class PCScene
    {
        /// <summary>Display px per PC px.</summary>
        public float S = 1;

        /// <summary>The x of the PC screen's left edge.</summary>
        public int OX;

        /// <summary>The window's span, in the screen's coordinates.</summary>
        public int mWindowX;

        public int mWindowW;

        private float mZoom = 1;

        /// <summary>theZoom: the scene's own zoom (about its top centre), on top of fitting the display height.</summary>
        public void Update(int theWidth, int theHeight, float theZoom = 1)
        {
            S = theHeight / 600.0f * theZoom;
            OX = (theWidth - (int)(800 * S)) / 2;
            mWindowX = 0;
            mWindowW = theWidth;
#if PEGGLE_IPHONE
            ThunderDialog.GetWindowMargins(out int aLeft, out int aRight);
            mWindowX = -aLeft;
            mWindowW = theWidth + aLeft + aRight;
#endif
        }

        public int PX(float x) => OX + (int)(x * S);

        public int PY(float y) => (int)(y * S);

        private float CX => OX + 400 * S;

        private float CY => 300 * S;

        /// <summary>
        /// iPhone: the zoom the far layers get, the least that makes the PC span theLeft..theRight (the part
        /// they all cover) reach both of the window's edges when scaled about the PC screen's centre.
        /// </summary>
        public void SetFarSpan(float theLeft, float theRight)
        {
            mZoom = 1;
#if PEGGLE_IPHONE
            float aLeft = CX - (OX + theLeft * S);
            float aRight = OX + theRight * S - CX;
            if (aLeft > 0 && aRight > 0)
            {
                mZoom = Math.Max(1, Math.Max((CX - mWindowX) / aLeft, (mWindowX + mWindowW - CX) / aRight));
            }
#endif
        }

        /// <summary>
        /// A far layer's rect: at PC (x, y), theW x theH display px, zoomed about the screen's centre, so its
        /// movement (y changing) is zoomed along with it.
        /// </summary>
        public TRect Far(float x, float y, float theW, float theH)
        {
            float aX = CX + (OX + x * S - CX) * mZoom;
            float aY = CY + (y * S - CY) * mZoom;
            return new TRect((int)Math.Floor(aX), (int)Math.Floor(aY), (int)Math.Ceiling(theW * mZoom), (int)Math.Ceiling(theH * mZoom));
        }

        /// <summary>A middle layer's rect: iPhone stretches the PC screen's width to the window's.</summary>
        public TRect Mid(float x, float y, int theW, int theH)
        {
#if PEGGLE_IPHONE
            float k = mWindowW / (800 * S);
            return new TRect((int)(mWindowX + x * S * k), PY(y), (int)Math.Ceiling(theW * k), theH);
#else
            return new TRect(PX(x), PY(y), theW, theH);
#endif
        }

        /// <summary>
        /// A front layer at PC (x, y). iPhone: its outer theSidePC PC px on each side go to the window's edges
        /// and the middle stretches to join them.
        /// </summary>
        public void DrawFront(Graphics g, Image theImage, TRect theSrc, float x, float y, int theSidePC)
        {
            int aY = PY(y);
#if PEGGLE_IPHONE
            int aSide = Math.Min(theSrc.mWidth / 2, (int)Math.Round(theSidePC * S));
            int h = theSrc.mHeight;
            int aRightX = mWindowX + mWindowW - aSide;
            // The middle reaches a pixel under each side piece, so no seam shows.
            g.DrawImage(theImage, new TRect(mWindowX + aSide - 1, aY, Math.Max(0, aRightX - mWindowX - aSide) + 2, h),
                new TRect(theSrc.mX + aSide, theSrc.mY, theSrc.mWidth - aSide * 2, h));
            g.DrawImage(theImage, new TRect(mWindowX, aY, aSide, h), new TRect(theSrc.mX, theSrc.mY, aSide, h));
            g.DrawImage(theImage, new TRect(aRightX, aY, aSide, h), new TRect(theSrc.mX + theSrc.mWidth - aSide, theSrc.mY, aSide, h));
#else
            g.DrawImage(theImage, PX(x), aY, theSrc);
#endif
        }

        /// <summary>g, or on iPhone an unclipped copy that reaches the window's margins.</summary>
        public static Graphics Wide(Graphics g)
        {
#if PEGGLE_IPHONE
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            return aWide;
#else
            return g;
#endif
        }
    }
}
