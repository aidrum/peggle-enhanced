using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// "Select Game" dialog (Sexy::GameSelectDialog): picks Peggle or Peggle Nights for adventure
    /// (type 0), challenge (1), stats (2) or the trophy room (3). The original's Nights trial badge
    /// and Game Center achievements button are not part of this port.
    /// Port addition: for adventure, a "More" button (bottom right, Back moving to the left) turns to
    /// a second page with the Peggle Extreme and Peggle WoW adventures (StageMgr.BONUS_PAGES), with their
    /// own buttons (gameselect/moreadventures); there it reads "Back" and returns to Peggle and Nights.
    /// </summary>
    public class GameSelectDialog : ThunderDialog
    {
        public const int TYPE_ADVENTURE = 0;

        public const int TYPE_CHALLENGE = 1;

        public const int TYPE_STATS = 2;

        public const int TYPE_TROPHIES = 3;

        /// <summary>Port addition: button ids of the second adventure page (not 1000/1001, the dialog's own).</summary>
        public const int GameSelect_Extreme = 2;

        public const int GameSelect_WoW = 3;

        public const int GameSelect_More = 4;

        public ThunderballApp mApp;                               // 0x128

        public ThunderButton mPeggleButton;                       // 0x12c

        public ThunderButton mNightsButton;                       // 0x130

        public int mType;                                         // 0x13c

        /// <summary>Port addition: the second adventure page's buttons (null unless both bonus Adventures are loaded).</summary>
        public ThunderButton mExtremeButton;

        public ThunderButton mWoWButton;

        public ThunderButton mMoreButton;

        /// <summary>Port addition: the adventure page shown, 0 Peggle/Nights, 1 Extreme/WoW.</summary>
        public int mPage;

        /// <summary>
        /// Port addition: the second page's buttons' sheet (gameselect/moreadventures, built by
        /// tools/build_more_adventures_buttons.py): a column per button, normal over highlight, its pill
        /// where IMAGE_DLG_BUTTON_NIGHTS_BIG has it.
        /// </summary>
        private static Image sMoreAdventuresSheet;

        private const int MORE_ADVENTURES_EXTREME = 0;

        private const int MORE_ADVENTURES_WOW = 1;

        /// <summary>
        /// Port addition: how much further left and higher IMAGE_DLG_BUTTON_PEGGLE_BIG has its pill than the
        /// sheet (whose pill is where IMAGE_DLG_BUTTON_NIGHTS_BIG has it). iPhone: only x, as Extreme sits
        /// level with WoW (the side-by-side Peggle button is lower than Nights').
        /// </summary>
#if PEGGLE_IPHONE
        private const int MORE_ADVENTURES_PEGGLE_PILL_X = 1;
#else
        private const int MORE_ADVENTURES_PEGGLE_PILL_X = 0;

        private const int MORE_ADVENTURES_PEGGLE_PILL_Y = 12;
#endif

        public GameSelectDialog(ThunderballApp theApp, string theLines, int theType)
            : base(DialogId.DIALOG_GAME_SELECT, true, SexyAppBase.Loc("Select Game"), theLines, SexyAppBase.Loc("Back"), 3)
        {
            mApp = theApp;
            mTextAlign = -1;
            mCloseOnButton = true;
            mType = theType;
            mPeggleButton = SexyUtil.MakeEmbeddedButton(0, this, Res.IMAGE_DLG_BUTTON_PEGGLE_BIG, false);
            mNightsButton = SexyUtil.MakeEmbeddedButton(1, this, Res.IMAGE_DLG_BUTTON_NIGHTS_BIG, false);
            if (mType == TYPE_ADVENTURE && StageMgr.GetAdventurePage(StageMgr.ADVENTURE_EXTREME) != null &&
                StageMgr.GetAdventurePage(StageMgr.ADVENTURE_WOW) != null)
            {
                mExtremeButton = MakeMoreAdventuresButton(GameSelect_Extreme, MORE_ADVENTURES_EXTREME);
                mWoWButton = MakeMoreAdventuresButton(GameSelect_WoW, MORE_ADVENTURES_WOW);
                mMoreButton = SexyUtil.MakeButton(GameSelect_More, this, "", null);
                ShowPage(0);
            }
        }

        /// <summary>Port addition: an embedded button showing theColumn of the More Adventures sheet.</summary>
        private ThunderButton MakeMoreAdventuresButton(int theId, int theColumn)
        {
            if (sMoreAdventuresSheet == null)
            {
                sMoreAdventuresSheet = mApp.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "gameselect/moreadventures", true);
                sMoreAdventuresSheet.mNumCols = 2;
                sMoreAdventuresSheet.mNumRows = 2;
            }
            Image aSheet = sMoreAdventuresSheet;
            int w = aSheet.mWidth / 2;
            int h = aSheet.mHeight / 2;
            ThunderButton aButton = SexyUtil.MakeEmbeddedButton(theId, this, aSheet, false);
            aButton.mWidth = w;
            aButton.mHeight = h;
            aButton.mNormalRect = new TRect(theColumn * w, 0, w, h);
            aButton.mOverRect = new TRect(theColumn * w, 0, w, h);
            aButton.mDownRect = new TRect(theColumn * w, h, w, h);
            return aButton;
        }

        public override void Dispose()
        {
            mPeggleButton?.Dispose();
            mNightsButton?.Dispose();
            mExtremeButton?.Dispose();
            mWoWButton?.Dispose();
            mMoreButton?.Dispose();
            base.Dispose();
        }

        public override void AddedToManager(WidgetManager theWidgetManager)
        {
            base.AddedToManager(theWidgetManager);
            theWidgetManager.AddWidget(mPeggleButton);
            theWidgetManager.AddWidget(mNightsButton);
            if (mMoreButton != null)
            {
                theWidgetManager.AddWidget(mExtremeButton);
                theWidgetManager.AddWidget(mWoWButton);
                theWidgetManager.AddWidget(mMoreButton);
            }
        }

        public override void RemovedFromManager(WidgetManager theWidgetManager)
        {
            base.RemovedFromManager(theWidgetManager);
            theWidgetManager.RemoveWidget(mPeggleButton);
            theWidgetManager.RemoveWidget(mNightsButton);
            if (mMoreButton != null)
            {
                theWidgetManager.RemoveWidget(mExtremeButton);
                theWidgetManager.RemoveWidget(mWoWButton);
                theWidgetManager.RemoveWidget(mMoreButton);
            }
        }

        /// <summary>Port addition: shows one adventure page's two buttons and labels the page button for the other.</summary>
        public void ShowPage(int thePage)
        {
            mPage = thePage;
            bool aMore = thePage == 1;
            ThunderButton[] aFirst = { mPeggleButton, mNightsButton };
            ThunderButton[] aSecond = { mExtremeButton, mWoWButton };
            foreach (ThunderButton aButton in aFirst)
            {
                aButton.SetVisible(!aMore);
                aButton.SetDisabled(aMore);
            }
            foreach (ThunderButton aButton in aSecond)
            {
                aButton.SetVisible(aMore);
                aButton.SetDisabled(!aMore);
            }
            mMoreButton.mLabel = aMore ? SexyAppBase.Loc("Main Adventures") : SexyAppBase.Loc("More Adventures");
            MarkDirty();
        }

        public override int GetPreferredHeight(int theWidth)
        {
#if PEGGLE_IPHONE
            return SCREEN_HEIGHT;
#else
            return SCREEN_HEIGHT - 100;
#endif
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int contentWidth = mWidth - mContentInsets.mLeft - mContentInsets.mRight - mBackgroundInsets.mLeft - mBackgroundInsets.mRight;
            int backWidth = (contentWidth - mButtonSidePadding * 2 - mButtonHorzSpacing) / 2;
#if PEGGLE_IPHONE
            // The two big buttons sit side by side, centred between the header and the Back button.
            int aPeggleH = Res.IMAGE_DLG_BUTTON_PEGGLE_BIG.GetHeight() / 2;
            int aNightsH = Res.IMAGE_DLG_BUTTON_NIGHTS_BIG.GetHeight() / 2;
            int aMidY = (mYesButton.mY - GetTop()) / 2;
            mPeggleButton.Resize(mX + mBackgroundInsets.mLeft + mContentInsets.mLeft + mButtonSidePadding + SexyUtil.gGutterWidth / 2,
                GetTop() + aMidY - aPeggleH / 2, Res.IMAGE_DLG_BUTTON_PEGGLE_BIG.GetWidth(), aPeggleH);
            mNightsButton.Resize(mPeggleButton.mX + backWidth + mButtonHorzSpacing,
                GetTop() + aMidY - aNightsH / 2 + 8, Res.IMAGE_DLG_BUTTON_NIGHTS_BIG.GetWidth(), aNightsH);
#else
            Image image = Res.IMAGE_DLG_BUTTON_PEGGLE_BIG;
            mPeggleButton.Resize(mX + mWidth / 2 - (contentWidth - image.mWidth / 2) / 2, mContentInsets.mTop + GetTop(),
                image.mWidth, image.mHeight / 2);
            mNightsButton.Layout((int)(LayoutFlags.LAY_Below | LayoutFlags.LAY_SameLeft), mPeggleButton, 0, 0, 0, 0);
#endif
            if (mMoreButton == null)
            {
                mYesButton.Resize(mX + mWidth / 2 - backWidth / 2, mYesButton.mY, backWidth, mYesButton.mHeight);
                return;
            }
            // Port addition: the second page's buttons take the first page's places, their pills where the
            // Peggle and Nights buttons' are (iPhone: Extreme level with WoW); Back moves left of the centre
            // and More sits right of it.
            int aMoreW = mExtremeButton.mWidth;
            int aMoreH = mExtremeButton.mHeight;
#if PEGGLE_IPHONE
            mExtremeButton.Resize(mPeggleButton.mX - MORE_ADVENTURES_PEGGLE_PILL_X, mNightsButton.mY, aMoreW, aMoreH);
#else
            mExtremeButton.Resize(mPeggleButton.mX - MORE_ADVENTURES_PEGGLE_PILL_X, mPeggleButton.mY - MORE_ADVENTURES_PEGGLE_PILL_Y, aMoreW, aMoreH);
#endif
            mWoWButton.Resize(mNightsButton.mX, mNightsButton.mY, aMoreW, aMoreH);
            mYesButton.Resize(mX + mWidth / 2 - mButtonHorzSpacing / 2 - backWidth, mYesButton.mY, backWidth, mYesButton.mHeight);
            mMoreButton.Resize(mX + mWidth / 2 + mButtonHorzSpacing / 2, mYesButton.mY, backWidth, mYesButton.mHeight);
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            DeferOverlay(0);
        }

        public override void ButtonDepress(int theId)
        {
            switch (mType)
            {
            case TYPE_ADVENTURE:
                if (theId == 0)
                {
                    mApp.DoScrollOff(mId);
                    mApp.StartGame(GameMode.GAMEMODE_ADVENTURE);
                }
                else if (theId == 1)
                {
                    // Peggle Nights is always owned in this port.
                    mApp.DoScrollOff(mId);
                    mApp.StartGame(GameMode.GAMEMODE_ADVENTURE_NIGHTS);
                }
                else if (theId == GameSelect_Extreme || theId == GameSelect_WoW)
                {
                    // Port addition: the bonus Adventures, each played as its page's pack.
                    BonusPage anAdventure = StageMgr.GetAdventurePage(theId == GameSelect_Extreme ? StageMgr.ADVENTURE_EXTREME : StageMgr.ADVENTURE_WOW);
                    mApp.DoScrollOff(mId);
                    mApp.StartGame(anAdventure.mLevelPack == 1 ? GameMode.GAMEMODE_ADVENTURE_NIGHTS : GameMode.GAMEMODE_ADVENTURE, anAdventure);
                }
                else if (theId == GameSelect_More)
                {
                    ShowPage(mPage == 0 ? 1 : 0);
                }
                else if (theId == 1000)
                {
                    mApp.DoScrollOff(mId);
                }
                break;
            case TYPE_CHALLENGE:
                if (theId == 0 || theId == 1)
                {
                    mApp.DoScrollOff(mId);
                    mApp.SetLevelPack(theId);
                    mApp.mGameMode = GameMode.GAMEMODE_CHALLENGE;
                    if (!mApp.CheckSaveGame(true, false))
                    {
                        mApp.mMainMenu?.StartGame();
                    }
                }
                else if (theId == 1000)
                {
                    mApp.DoScrollOff(mId);
                }
                break;
            case TYPE_STATS:
                if (theId == 0 || theId == 1)
                {
                    mApp.DoScrollOff(mId);
                    mApp.SetLevelPack(theId);
                    mApp.mGameMode = GameMode.GAMEMODE_CHALLENGE;
                    mApp.ShowAdventureScreen(theId == 1);
                }
                else if (theId == 1000)
                {
                    mApp.FinishGameSelectDialog();
                }
                break;
            case TYPE_TROPHIES:
                if (theId == 0)
                {
                    mApp.DoScrollOff(mId);
                    mApp.ShowTrophyRoom(1);
                }
                else if (theId == 1)
                {
                    mApp.SetLevelPack(1);
                    mApp.DoScrollOff(mId);
                    mApp.ShowProgressScreen(2);
                }
                else if (theId == 1000)
                {
                    mApp.FinishGameSelectDialog();
                }
                break;
            }
        }
    }
}
