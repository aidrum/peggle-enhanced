using System;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The Quick Play / Duel / Challenge level picker (Sexy::LevelScreen): a full-screen dialog
    /// holding the scrolling LevelThumbnailsWidget, Peggle/Nights jump buttons, and the
    /// LevelDetailsDialog that flips open when a thumbnail is tapped.
    /// Port addition: in Quick Play further pages hold the bonus levels (Nights bonus, Extreme), and the two jump
    /// buttons are GameArrowButtons showing the logo of the page they go to.
    /// </summary>
    public class LevelScreen : ThunderDialog, ThumbnailDialogListener, LevelThumbnailsWidgetListener
    {
        public ThunderballApp mApp;                               // 0x130

        public LevelDetailsDialog mDetailsDialog;                 // 0x134

        /// <summary>Port: the original's Peggle button (0x138); goes to the previous page.</summary>
        public GameArrowButton mLeftButton;

        /// <summary>Port: the original's Nights button (0x13c); goes to the next page.</summary>
        public GameArrowButton mRightButton;

        public bool mNewGame;                                     // 0x140

        public LevelThumbnailsWidget mThumbnails;                 // 0x144

        public ThunderScrollWidget mScrollWidget;                 // 0x148

        /// <summary>Port addition: the page shown, 0 Peggle, 1 Nights, then the bonus pages (StageMgr.BONUS_PAGES).</summary>
        public int mPage;

        /// <summary>Port addition: false when choosing a level for a new game, which never had the jump buttons.</summary>
        public bool mShowArrows;

        public LevelScreen(ThunderballApp theApp, bool duel, bool newGame)
            : base(DialogId.DIALOG_LEVEL_SCREEN, true, SexyAppBase.Loc("SELECT LEVEL"), "", SexyAppBase.Loc(""), 3)
        {
            LevelThumbnailsWidget.RANDOM_BUTTON_HEIGHT = LEVEL_SCREEN_STAGE_THUMB_H + 2;
            LevelThumbnailsWidget.THUMB_STAGE_WIDTH = (LEVEL_SCREEN_STAGE_THUMB_W + LEVEL_SCREEN_STAGE_THUMB_GAP) * 5 + LEVEL_SCREEN_STAGE_THUMB_INSET;
            LevelThumbnailsWidget.ROW_HEIGHT = LevelThumbnailsWidget.RANDOM_BUTTON_HEIGHT;
            mApp = theApp;
            mApp.UnloadMainMenuResources();
            mApp.UnloadGameResources();
            mApp.LoadQuickplayResources();
            mThumbnails = new LevelThumbnailsWidget(mApp, this);
            mNewGame = newGame;
            mScrollWidget = new ThunderScrollWidget();
            mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Vertical);
            mScrollWidget.AddWidget(mThumbnails);
            AddWidget(mScrollWidget);
            mLeftButton = new GameArrowButton(GameArrowButton.ARROW_LEFT, 0, this);
            mRightButton = new GameArrowButton(GameArrowButton.ARROW_RIGHT, 1, this);
            mPage = 0;
            mShowArrows = !newGame;
            if (!newGame)
            {
                if (!duel)
                {
                    float aScrollX = mApp.mLevelScreenScrollX;
                    if (aScrollX < 0)
                    {
                        mPage = Math.Max(1, mApp.mLevelScreenPage);
                        if (mPage >= GetNumPages())
                        {
                            // Port: the bonus pages are Quick Play only; anywhere else, open on Nights.
                            mPage = 1;
                            aScrollX = -LevelThumbnailsWidget.THUMB_STAGE_WIDTH;
                        }
                    }
                    mScrollWidget.SetScrollOffset(new CGPoint { x = aScrollX, y = mApp.mLevelScreenScrollY }, false);
                }
            }
            else
            {
                float aScrollX = ThunderballApp.mGameLevelPack == 1 ? (float)-LevelThumbnailsWidget.THUMB_STAGE_WIDTH : 0.0f;
                mScrollWidget.SetScrollOffset(new CGPoint { x = aScrollX, y = 0.0f }, false);
                mPage = aScrollX < 0 ? 1 : 0;
            }
            UpdateArrows();
            AddWidget(mLeftButton);
            AddWidget(mRightButton);
            mYesButton.mLabel = SexyAppBase.Loc("Main Menu");
            mDetailsDialog = new LevelDetailsDialog(mApp, this);
            bool isDuel = mApp.mGameMode == GameMode.GAMEMODE_DUEL;
            bool isChallenge = mApp.mGameMode == GameMode.GAMEMODE_CHALLENGE;
            if (isDuel)
            {
                mDialogHeader = SexyAppBase.Loc("Two Player Duel");
            }
            else if (isChallenge)
            {
                TrophyInfo aTrophy = mApp.mTrophyMgr.GetTrophyInfoById(mApp.mChallengeId);
                if (aTrophy == null)
                {
                    mDialogHeader = SexyAppBase.Loc("Pick a Level");
                }
                else
                {
                    mDialogHeader = aTrophy.mName;
                }
            }
            else
            {
                mDialogHeader = SexyAppBase.Loc("Quick Play");
            }
            mAutoSizeYesButton = true;
        }

        public override void Dispose()
        {
            mApp.UnloadQuickplayResources();
            RemoveAllWidgets(true, false);
            mDetailsDialog?.Dispose();
            mDetailsDialog = null;
            base.Dispose();
        }

        /// <summary>Port addition: the bonus pages are offered in Quick Play only.</summary>
        public bool HasBonusPages()
        {
            // Quick Play and Duel; challenges keep the original levels.
            return (mApp.mGameMode == GameMode.GAMEMODE_QUICKPLAY || mApp.mGameMode == GameMode.GAMEMODE_DUEL) &&
                mApp.mStageMgr.GetNumBonusStages() > 0;
        }

        public int GetNumPages()
        {
            return 2 + (HasBonusPages() ? StageMgr.GetNumBonusPages() : 0);
        }

        /// <summary>Port addition: a page's logo in the arrow sheet.</summary>
        public static int GetPageLogo(int thePage)
        {
            if (thePage < 2)
            {
                return thePage == 0 ? GameArrowButton.LOGO_PEGGLE : GameArrowButton.LOGO_NIGHTS;
            }
            return StageMgr.GetBonusPageAt(thePage - 2).mLogoCel;
        }

        /// <summary>Port addition: shows the arrows for mPage, each with the logo of the page it leads to.</summary>
        public void UpdateArrows()
        {
            mLeftButton.mVisible = mShowArrows && mPage > 0;
            if (mLeftButton.mVisible)
            {
                mLeftButton.mLogo = GetPageLogo(mPage - 1);
            }
            mRightButton.mVisible = mShowArrows && mPage < GetNumPages() - 1;
            if (mRightButton.mVisible)
            {
                mRightButton.mLogo = GetPageLogo(mPage + 1);
            }
        }

        /// <summary>Port addition: scrolls to a page (the original's buttons 0 and 1 did this for Peggle and Nights).</summary>
        public void ShowPage(int thePage)
        {
            mPage = Math.Clamp(thePage, 0, GetNumPages() - 1);
            int aX = mThumbnails.GetPageX(mPage);
            // The bonus pages have only a few rows, so they open at the top.
            int aY = mPage >= 2 ? 0 : -mThumbnails.mY;
            mScrollWidget.ScrollRectIntoView(new TRect(aX, aY, mScrollWidget.mWidth, mScrollWidget.mHeight), true);
            UpdateArrows();
        }

        /// <summary>Closes the full-game upsell dialog. The store itself is not part of this port.</summary>
        public void FinishBuyFullGameDialog(bool showStore)
        {
            ThunderDialog aDialog = (ThunderDialog)mApp.GetDialog(0x35);
            if (aDialog != null)
            {
                aDialog.DoScrollOff(true);
            }
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int aTop = mContentInsets.mTop + LEVEL_SCREEN_OFFSET_TOP_Y;
            int aBottom = mYesButton.mY - LEVEL_SCREEN_OFFSET_BOTTOM_Y;
            int aLeft = mContentInsets.mLeft + 2;
            int aRight = mWidth - mContentInsets.mRight - 2;
            int aRows = Math.Max(NUM_STAGES_NIGHTS, NUM_STAGES_PEGGLE) + 1;
            if (mNewGame && ThunderballApp.mGameLevelPack == 0)
            {
                aRows = Math.Max(NUM_STAGES_NIGHTS, NUM_STAGES_PEGGLE);
            }
            // Port: room for the bonus pages, up to the last one's right edge.
            int aPages = 1 + (HasBonusPages() ? StageMgr.GetNumBonusPages() : 0);
            mThumbnails.Resize(0, 0, LevelThumbnailsWidget.GetPageX(aPages, aRight - aLeft) + (aRight - aLeft),
                (LevelThumbnailsWidget.ROW_HEIGHT + LEVEL_SCREEN_STAGE_ROW_GAP) * aRows);
            mScrollWidget.Resize(aLeft, aTop, aRight - aLeft, aBottom - aTop);
            // Port: placed where the original Peggle/Nights buttons were, offset so the arrows line up.
            mLeftButton.Resize(mX + mContentInsets.mLeft / 2 + GameArrowButton.LEFT_OFFSET_X,
                mYesButton.mY - LEVEL_SCREEN_PEGGLEBT_OFFSET_BOTTOM_Y + GameArrowButton.LEFT_OFFSET_Y,
                mLeftButton.mNormalCelRect.mWidth, mLeftButton.mNormalCelRect.mHeight);
            mRightButton.Resize(mWidth - mContentInsets.mLeft / 2 - Res.IMAGE_DLG_BUTTON_NIGHTS.mWidth + GameArrowButton.RIGHT_OFFSET_X,
                mYesButton.mY - LEVEL_SCREEN_NIGHTSBT_OFFSET_BOTTOM_Y + GameArrowButton.RIGHT_OFFSET_Y,
                mRightButton.mNormalCelRect.mWidth, mRightButton.mNormalCelRect.mHeight);
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return SCREEN_HEIGHT;
        }

        public void DoPlay()
        {
            mApp.mLevelScreenScrollX = mThumbnails.mX;
            mApp.mLevelScreenScrollY = mThumbnails.mY;
            mApp.mLevelScreenPage = mPage;
            mApp.ShowBoard(false, true, false, false);
            // The original indexes unchecked; for the Random Level row that reads past the end of the
            // vector (no real level is marked) where C# throws. With the bonus pages the list is longer,
            // so the Random Level index lands on an empty slot (no level) inside it instead: skip both.
            int aSelected = mThumbnails.mSelected;
            if (aSelected >= 0 && aSelected < mThumbnails.mLevels.Count && aSelected != mThumbnails.GetRandomLevelIndex())
            {
                LevelInfo aLevelInfo = mThumbnails.mLevels[aSelected].mLevelInfo;
                if (aLevelInfo != null)
                {
                    mApp.mStageMgr.MarkLastUseTime(aLevelInfo);
                }
            }
        }

        public void PlayPressed()
        {
            mWidgetManager?.RemoveWidget(mDetailsDialog);
            mApp.mCurLevel = mThumbnails.mSelected % 5;
            mApp.mCurStage = mThumbnails.mSelected / 5;
            DoPlay();
        }

        public void Closed()
        {
            mWidgetManager?.RemoveWidget(mDetailsDialog);
            mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Vertical);
        }

        public void SelectionChanged()
        {
            if (mDetailsDialog.mWidgetManager != null)
            {
                return;
            }
            if (mThumbnails.mSelected == mThumbnails.GetRandomLevelIndex())
            {
                int aStage = -1;
                int aLevel = -1;
                bool isDuel = mApp.mGameMode == GameMode.GAMEMODE_DUEL;
                // Port: the bonus pages' levels too, where they are shown (Quick Play, Duel).
                mApp.mStageMgr.GetRandomLevel(mApp.mPlayerInfo, ref aStage, ref aLevel, isDuel, 0, true, false, HasBonusPages());
                if (aStage > -1 && aLevel > -1)
                {
                    mApp.mCurLevel = aLevel;
                    mApp.mCurStage = aStage;
                    mApp.SetLevelPack(StageMgr.GetLevelPackForStage(mApp.mCurStage));
                    DoPlay();
                }
                return;
            }
            LevelInfo aLevelInfo = mThumbnails.mLevels[mThumbnails.mSelected].mLevelInfo;
            mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Disabled);
            mDetailsDialog.mLevel = mThumbnails.mSelected % 5;
            mDetailsDialog.mStage = mThumbnails.mSelected / 5;
            mApp.SetLevelPack(StageMgr.GetLevelPackForStage(mDetailsDialog.mStage));
            mDetailsDialog.SetCaption(mThumbnails.GetSelectedLevelName());
            mDetailsDialog.mScores = mApp.mHighScoreMgr.GetScores(aLevelInfo.mName, false, aLevelInfo.mAceScore);
            mDetailsDialog.mCleared = mThumbnails.mLevels[mThumbnails.mSelected].mCleared;
            mDetailsDialog.mAced = mThumbnails.mLevels[mThumbnails.mSelected].mAced;
#if PEGGLE_IPHONE
            // One line; the dialog spans the gutters too.
            mDetailsDialog.mAceScore = CFormat.StrFormat(SexyAppBase.Loc("Ace Score: %S"), SexyUtil.CommaSeperate(aLevelInfo.mAceScore));
            int aDialogW = SexyUtil.gGutterWidth * 2 + SCREEN_WIDTH;
            mDetailsDialog.Resize(mWidth / 2 - aDialogW / 2, mHeight / 2 - SCREEN_HEIGHT / 2, aDialogW, SCREEN_HEIGHT);
#else
            mDetailsDialog.mAceLabel = SexyAppBase.Loc("Ace Score: ");
            mDetailsDialog.mAceScore = SexyUtil.CommaSeperate(aLevelInfo.mAceScore);
            mDetailsDialog.Resize(mWidth / 2 - SCREEN_WIDTH / 2, mHeight / 2 - SCREEN_HEIGHT / 2, SCREEN_WIDTH, SCREEN_HEIGHT);
#endif
            mWidgetManager.AddWidget(mDetailsDialog);
            mDetailsDialog.Show();
        }

        public void GetThumbnailInfo(out Image theImage, out TRect theRect)
        {
            theImage = mThumbnails.GetSelectedThumbnail();
            theRect = mThumbnails.GetSelectedRect();
            theRect.mX = mScrollWidget.mX + theRect.mX;
            theRect.mY = mScrollWidget.mY + mThumbnails.mY + theRect.mY;
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            TRect aRect = mScrollWidget.GetRect();
            aRect.Inflate(LEVEL_SCREEN_SCROLL_ALPHA_EXTRA_PIX, LEVEL_SCREEN_SCROLL_ALPHA_EXTRA_PIX);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0, 0x7f));
            SexyUtil.MyDrawImageBox(g, aRect, Res.IMAGE_DLG_TEXTRECT);
            g.SetColorizeImages(false);
        }

        public override void ButtonDepress(int theId)
        {
            if (theId == 1000)
            {
                mWidgetManager?.RemoveWidget(mDetailsDialog);
                GlobalStaticVars.gSexyAppBase.KillDialog(this);
                mApp.DoToMenu();
            }
            else if (theId == 0)
            {
                ShowPage(mPage - 1);
            }
            else if (theId == 1)
            {
                ShowPage(mPage + 1);
            }
            else if (theId == 2)
            {
                FinishBuyFullGameDialog(true);
            }
            else if (theId == 3)
            {
                FinishBuyFullGameDialog(false);
            }
        }
    }
}
