namespace Sexy
{
    /// <summary>
    /// Port addition: the level screen's game-switch button, replacing the original's one-piece
    /// IMAGE_DLG_BUTTON_PEGGLE / _NIGHTS. A blank blue arrow is drawn with the logo of the game it
    /// goes to on top, both from levelselect/gamearrows (built by tools/build_arrow_sheet.py). The
    /// pulsing highlight and pressed look are GameButton's, done one layer at a time.
    /// </summary>
    public class GameArrowButton : GameButton
    {
        public const int ARROW_LEFT = 0;

        public const int ARROW_RIGHT = 1;

        /// <summary>Logo cels: Peggle and Nights, then the bonus pages' (StageMgr.BONUS_PAGES).</summary>
        public const int LOGO_PEGGLE = 2;

        public const int LOGO_NIGHTS = 3;

        public const int LOGO_NIGHTSDLC = 4;

        public const int LOGO_EXTREME = 5;

        public const int LOGO_WOW = 6;

        public const int LOGO_DUALSHOT = 7;

#if PEGGLE_IPHONE
        // Where a frame sits relative to the original button it replaces, so the arrows line up.
        public static int LEFT_OFFSET_X = 7;
        public static int LEFT_OFFSET_Y = -8;
        public static int RIGHT_OFFSET_X = 10;
        public static int RIGHT_OFFSET_Y = 7;
#else
        public static int LEFT_OFFSET_X = -3;
        public static int LEFT_OFFSET_Y = -23;
        public static int RIGHT_OFFSET_X = -4;
        public static int RIGHT_OFFSET_Y = -2;
#endif

        private static Image sSheet;

        public int mArrow;

        public int mLogo;

        public GameArrowButton(int theArrow, int theId, ButtonListener theListener) : base(GetSheet(), theId, theListener, true)
        {
            mArrow = theArrow;
            mLogo = LOGO_PEGGLE;
            mNormalCelRect = new TRect(0, 0, mImage.mWidth / mImage.mNumCols, mImage.mHeight / mImage.mNumRows);
            mDownCelRect = mNormalCelRect;
        }

        public static Image GetSheet()
        {
            if (sSheet == null)
            {
                sSheet = GlobalStaticVars.gSexyAppBase.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "levelselect/gamearrows", true);
                sSheet.mNumCols = 8;
                sSheet.mNumRows = 2;
            }
            return sSheet;
        }

        /// <summary>
        /// The opaque part of a logo cel (normal row), for drawing the logo on its own without the
        /// cel's blank margins, as the replay list does for the Extreme, WoW and Dual Shot levels.
        /// </summary>
        public static TRect GetLogoRect(int theLogo)
        {
            Image aSheet = GetSheet();
            int aCelW = aSheet.mWidth / aSheet.mNumCols;
#if PEGGLE_IPHONE
            TRect aBounds = theLogo == LOGO_EXTREME ? new TRect(40, 6, 162, 108)
                : theLogo == LOGO_DUALSHOT ? new TRect(48, 15, 157, 94) : new TRect(38, 15, 156, 96);
#else
            TRect aBounds = theLogo == LOGO_EXTREME ? new TRect(48, 7, 195, 129)
                : theLogo == LOGO_DUALSHOT ? new TRect(58, 18, 188, 112) : new TRect(46, 18, 187, 115);
#endif
            aBounds.mX += theLogo * aCelW;
            return aBounds;
        }

        public override void Draw(Graphics g)
        {
            if (!IsButtonDown())
            {
                // Each layer is faded towards its highlight before the next goes on top, so opaque
                // areas come out as the normal/highlight blend of the whole button.
                DrawLayer(g, mArrow);
                DrawLayer(g, mLogo);
            }
            else
            {
                g.DrawImageCel(mImage, 0, 0, mArrow, 1);
                g.DrawImageCel(mImage, 0, 0, mLogo, 1);
            }
        }

        private void DrawLayer(Graphics g, int theCel)
        {
            g.DrawImageCel(mImage, 0, 0, theCel, 0);
            g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mGlow * 255.0f)));
            g.SetColorizeImages(true);
            g.DrawImageCel(mImage, 0, 0, theCel, 1);
            g.SetColorizeImages(false);
        }
    }
}
