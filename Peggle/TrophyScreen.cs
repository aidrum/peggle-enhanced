using System;
using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    public interface TrophyThumbnailsWidgetListener
    {
        void SelectionChanged();
    }

    /// <summary>
    /// The Master Challenges screen (Sexy::TrophyScreen): a full-screen dialog holding the
    /// scrolling TrophyThumbnailsWidget (one row of five challenges per trophy page) and the
    /// TrophyDetailsDialog that flips open when a challenge is tapped.
    /// </summary>
    public class TrophyScreen : ThunderDialog, ThumbnailDialogListener, TrophyThumbnailsWidgetListener
    {
        public ThunderballApp mApp;                               // 0x130

        public TrophyDetailsDialog mDetailsDialog;                // 0x134

        public TrophyThumbnailsWidget mThumbnails;                // 0x138

        public ThunderScrollWidget mScrollWidget;                 // 0x13c

        public TrophyScreen(ThunderballApp theApp, bool newChallenge)
            : base(DialogId.DIALOG_LEVEL_SCREEN, true, SexyAppBase.Loc("MASTER CHALLENGES"), "", SexyAppBase.Loc(""), 3)
        {
            TrophyThumbnailsWidget.ROW_HEIGHT = TROPHY_SCR_THUMB_H + TROPHY_SCR_CAPTION_H + TROPHY_SCR_THUMB_GAP_W * 2;
            TrophyThumbnailsWidget.ROW_WIDTH = TROPHY_SCR_THUMB_W * 5 + TROPHY_SCR_THUMB_INSET_W * 2 + TROPHY_SCR_THUMB_GAP_W * 4;
            mApp = theApp;
            mApp.UnloadMainMenuResources();
            mApp.UnloadGameResources();
            mApp.LoadQuickplayResources();
            mThumbnails = new TrophyThumbnailsWidget(mApp, this);
            mScrollWidget = new ThunderScrollWidget();
            mScrollWidget.AddWidget(mThumbnails);
            AddWidget(mScrollWidget);
            if (!newChallenge)
            {
                mScrollWidget.SetScrollOffset(new CGPoint { x = 0.0f, y = mApp.mTrophyScreenScrollY }, false);
            }
            mYesButton.mLabel = SexyAppBase.Loc("Main Menu");
            mDetailsDialog = new TrophyDetailsDialog(mApp, this);
            mAutoSizeYesButton = true;
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            mDetailsDialog?.Dispose();
            mDetailsDialog = null;
            base.Dispose();
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int aTop = TROPHY_SCR_SCROLL_OFFSET_TOP + mContentInsets.mTop;
            int aBottom = mYesButton.mY - TROPHY_SCR_SCROLL_OFFSET_BOTTOM;
            int aWidth = TrophyThumbnailsWidget.GetPreferredWidth();
            int aPages = TrophyMgr.GetNumPagesCurGame();
            mThumbnails.Resize(0, 0, aWidth, (TrophyThumbnailsWidget.ROW_HEIGHT + TROPHY_SCR_ROW_GAP_W) * aPages - TROPHY_SCR_ROW_GAP_W);
            mScrollWidget.Resize(theWidth / 2 - aWidth / 2, aTop, aWidth, aBottom - aTop);
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            TRect aRect = mScrollWidget.GetRect();
            aRect.Inflate(TROPHY_SCR_SCROLL_ALPHA_EXTRA_PIX, TROPHY_SCR_SCROLL_ALPHA_EXTRA_PIX);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0, 0x7f));
            SexyUtil.MyDrawImageBox(g, aRect, Res.IMAGE_DLG_TEXTRECT);
            g.SetColorizeImages(false);
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return SCREEN_HEIGHT;
        }

        public void DoPlay()
        {
            mDetailsDialog.SetVisible(false);
            TrophyInfo aTrophy = mThumbnails.mTrophies[mThumbnails.mSelected].mTrophy;
            mApp.mChallengeId = aTrophy.mId;
            mApp.mTrophyScreenScrollY = mThumbnails.mY;
            if (aTrophy.mLevels.Count == 1 && string.Equals(aTrophy.mLevels[0], "pick", StringComparison.OrdinalIgnoreCase))
            {
                mApp.ShowLevelScreen(false, false, true);
                return;
            }
            mApp.ShowBoard(true, true, false, false);
        }

        public void PlayPressed()
        {
            mWidgetManager?.RemoveWidget(mDetailsDialog);
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
            TrophyThumbnailsWidget.TrophyDisplayInfo anInfo = mThumbnails.mTrophies[mThumbnails.mSelected];
            TrophyInfo aTrophy = anInfo.mTrophy;
            mDetailsDialog.mText = aTrophy.mDesc;
            if (mDetailsDialog.mText.Length == 0)
            {
                mDetailsDialog.mText = anInfo.mPage.mDesc;
            }
            mDetailsDialog.SetCaption(aTrophy.mName);
            mDetailsDialog.mNumLevels = aTrophy.mLevels.Count;
            mDetailsDialog.mIsRandom = anInfo.mIsRandom;
            mDetailsDialog.mWon = anInfo.mWon;
            mDetailsDialog.mBadgeDelay = 50;
            if (!mDetailsDialog.mIsRandom)
            {
                for (int i = 0; i < mDetailsDialog.mNumLevels; i++)
                {
                    mDetailsDialog.mLevels[i] = mApp.mStageMgr.GetLevelInfoByName(aTrophy.mLevels[i]);
                }
            }
            mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Disabled);
#if PEGGLE_IPHONE
            int aDialogW = SexyUtil.gGutterWidth * 2 + SCREEN_WIDTH;
            mDetailsDialog.Resize(mWidth / 2 - aDialogW / 2, mHeight / 2 - SCREEN_HEIGHT / 2, aDialogW, SCREEN_HEIGHT);
#else
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

        public override void ButtonDepress(int theId)
        {
            if (theId == 1000)
            {
                mWidgetManager?.RemoveWidget(mDetailsDialog);
                GlobalStaticVars.gSexyAppBase.KillDialog(this);
                mApp.DoToMenu();
            }
        }
    }

    /// <summary>
    /// The grid of challenge thumbnails on the trophy screen (Sexy::TrophyThumbnailsWidget): one
    /// captioned row of five per trophy page, a question mark for random/pick challenges, a level
    /// count for multi-level challenges and a gold star on challenges already won.
    /// </summary>
    public class TrophyThumbnailsWidget : Widget
    {
        public static int ROW_HEIGHT;

        public static int ROW_WIDTH;

        public class TrophyDisplayInfo
        {
            public bool mWon;                                     // 0x01

            public bool mNew;                                     // 0x02

            public int mTopScore;                                 // 0x04

            public TrophyInfo mTrophy;                            // 0x08

            public TrophyPage mPage;                              // 0x0c

            public bool mIsRandom;                                // 0x10
        }

        public int mSelected;                                     // 0x78

        public ThunderballApp mApp;                               // 0x7c

        public TrophyThumbnailsWidgetListener mListener;          // 0x80

        public List<TrophyDisplayInfo> mTrophies = new List<TrophyDisplayInfo>(); // 0x84

        public MemoryImage mSelectedThumbnail;                    // 0x90

        public bool mThumbnailDirty;                              // 0x94

        public TrophyThumbnailsWidget(ThunderballApp theApp, TrophyThumbnailsWidgetListener theListener)
        {
            mApp = theApp;
            LoadDisplayInfo();
            mSelected = -1;
            mListener = theListener;
            mSelectedThumbnail = null;
            mThumbnailDirty = true;
        }

        public void LoadDisplayInfo()
        {
            int aNumTrophies = mApp.mTrophyMgr.GetNumTrophies();
            for (int i = 0; i < aNumTrophies; i++)
            {
                TrophyDisplayInfo anInfo = new TrophyDisplayInfo();
                TrophyPage aPage = mApp.mTrophyMgr.GetTrophyPage(i / 5);
                TrophyInfo aTrophy = aPage.mTrophies[i % 5];
                if (aTrophy != null)
                {
                    anInfo.mNew = aTrophy.mFlag24;
                    if (anInfo.mNew)
                    {
                        aTrophy.mFlag24 = false;
                    }
                }
                if (aTrophy != null && mApp.mPlayerInfo != null)
                {
                    anInfo.mWon = mApp.mPlayerInfo.GetmTrophySet().Contains(aTrophy.mId);
                    anInfo.mTopScore = mApp.mPlayerInfo.GetTrophyTopScore(aTrophy.mId);
                }
                string aLevel = aTrophy.mLevels[0];
                if (aLevel.StartsWith("rand", StringComparison.OrdinalIgnoreCase) || string.Equals(aLevel, "pick", StringComparison.OrdinalIgnoreCase))
                {
                    anInfo.mIsRandom = true;
                }
                anInfo.mTrophy = aTrophy;
                anInfo.mPage = aPage;
                mTrophies.Add(anInfo);
            }
        }

        public override void Dispose()
        {
            mSelectedThumbnail?.Dispose();
            mSelectedThumbnail = null;
            base.Dispose();
        }

        public override void MouseUp(int x, int y, int theClickCount)
        {
            if (x <= TROPHY_SCR_THUMB_INSET_W)
            {
                return;
            }
            int aCol = (x - TROPHY_SCR_THUMB_INSET_W) / (TROPHY_SCR_THUMB_GAP_W + TROPHY_SCR_THUMB_W);
            if (aCol > 4)
            {
                aCol = 4;
            }
            int aRow = y / (TROPHY_SCR_ROW_GAP_W + ROW_HEIGHT);
            int aPage = ThunderballApp.mGameLevelPack == 1 ? aRow + 8 : aRow;
            int anIndex = aPage * 5 + aCol;
            if (mSelected != anIndex)
            {
                mThumbnailDirty = true;
            }
            mSelected = anIndex;
            ((ScrollWidget)mParent).ScrollRectIntoView(new TRect(0, aRow * (TROPHY_SCR_ROW_GAP_W + ROW_HEIGHT), mWidth, ROW_HEIGHT), true);
            mListener.SelectionChanged();
        }

        public static int GetPreferredWidth()
        {
            return ROW_WIDTH;
        }

        public override void Draw(Graphics g)
        {
            mApp.LoadQuickplayResources();
            int aPages = TrophyMgr.GetNumPagesCurGame();
            Font aCaptionFont = Res.FONT_CAPTION;
            Widget aParent = (Widget)mParent;
            TRect aVisible = new TRect(0, -mY, mWidth, aParent.mHeight);
            for (int aRow = 0; aRow < aPages; aRow++)
            {
                int aPage = ThunderballApp.mGameLevelPack == 1 ? aRow + 8 : aRow;
                int aRowY = aRow * (TROPHY_SCR_ROW_GAP_W + ROW_HEIGHT);
                TRect aRowRect = new TRect(0, aRowY, ROW_WIDTH, ROW_HEIGHT);
                if (!aRowRect.Intersects(aVisible))
                {
                    continue;
                }
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForStage(aRow, ThunderballApp.mGameLevelPack, true), 0x7f));
                SexyUtil.MyDrawImageBox(g, aRowRect, Res.IMAGE_DLG_GROUPRECT);
                g.SetColorizeImages(false);
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                g.SetFont(aCaptionFont);
                WriteCenteredLine(g, TROPHY_SCR_CAPTION_H + aRowY - TROPHY_SCR_CAPTION_GAP_H, mTrophies[aPage * 5].mPage.mName);
                int x = TROPHY_SCR_THUMB_INSET_W;
                int y = aRowY + TROPHY_SCR_CAPTION_H;
                for (int aCol = 0; aCol < 5; aCol++)
                {
                    TrophyDisplayInfo anInfo = mTrophies[aPage * 5 + aCol];
                    TrophyInfo aTrophy = anInfo.mTrophy;
                    if (!anInfo.mIsRandom)
                    {
                        LevelInfo aLevel = mApp.mStageMgr.GetLevelInfoByName(aTrophy.mLevels[0]);
                        SexyUtil.DrawLevelThumbnail(g, aLevel.mStage, aLevel.mLevel, new TRect(x, y, TROPHY_SCR_THUMB_W, TROPHY_SCR_THUMB_H));
                    }
                    else
                    {
                        g.DrawImage(Res.IMAGE_DLG_QUESTIONMARK, x, y, TROPHY_SCR_THUMB_W, TROPHY_SCR_THUMB_H);
                    }
                    int aNumLevels = aTrophy.mLevels.Count;
                    if (aNumLevels > 1)
                    {
                        g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
                        int aNumX = aNumLevels < 10 ? TROPHY_SCR_LEVEL_SMALL_NUM_X : TROPHY_SCR_LEVEL_BIG_NUM_X;
                        int aNumY = TROPHY_SCR_THUMB_H - g.GetFont().GetDecorationOffset();
                        g.DrawString(aNumLevels.ToString(), x + aNumX, y + aNumY);
                    }
                    if (anInfo.mWon)
                    {
                        g.DrawImage(Res.IMAGE_DLG_LITTLE_GOLD_STAR, x + 4, y + 4);
                    }
                    x += TROPHY_SCR_THUMB_GAP_W + TROPHY_SCR_THUMB_W;
                }
                x = TROPHY_SCR_THUMB_INSET_W;
                g.SetColor(SexyUtil.MakeColor(0x808080));
                for (int aCol = 0; aCol < 5; aCol++)
                {
                    g.DrawRect(x, y, TROPHY_SCR_THUMB_W - 1, TROPHY_SCR_THUMB_H - 1);
                    x += TROPHY_SCR_THUMB_GAP_W + TROPHY_SCR_THUMB_W;
                }
            }
        }

        public Image GetSelectedThumbnail()
        {
            if (mSelectedThumbnail != null && !mThumbnailDirty)
            {
                return mSelectedThumbnail;
            }
            TrophyDisplayInfo anInfo = mTrophies[mSelected];
            TRect aCelRect = Res.IMAGE_LS_THUMBNAILS_PEGGLE.GetCelRect(0);
            int w = aCelRect.mWidth;
            int h = aCelRect.mHeight;
            if (mSelectedThumbnail == null)
            {
                mSelectedThumbnail = new MemoryImage();
                mSelectedThumbnail.Create(w, h);
            }
            else
            {
                mSelectedThumbnail.Clear();
            }
            bool inFrame = Graphics.IsSpriteBatchBegun();
            Graphics g = Graphics.GetNew(mSelectedThumbnail);
            if (!anInfo.mIsRandom)
            {
                LevelInfo aLevel = mApp.mStageMgr.GetLevelInfoByName(anInfo.mTrophy.mLevels[0]);
                SexyUtil.DrawLevelThumbnail(g, aLevel.mStage, aLevel.mLevel, new TRect(0, 0, w, h));
            }
            else
            {
                g.DrawImage(Res.IMAGE_DLG_QUESTIONMARK, 0, 0, w, h);
            }
            if (anInfo.mWon)
            {
                Image aStar = Res.IMAGE_DLG_LITTLE_GOLD_STAR;
                g.DrawImage(aStar, 4, 4, aStar.mWidth * 2, aStar.mHeight * 2);
            }
            g.SetColor(SexyUtil.MakeColor(0x808080));
            g.DrawRect(0, 0, w - 1, h - 1);
            g.EndOffscreen(inFrame);
            mThumbnailDirty = false;
            return mSelectedThumbnail;
        }

        public TRect GetSelectedRect()
        {
            int aRow = mSelected / 5;
            int aCol = mSelected % 5;
            if (aRow > 7)
            {
                aRow -= 8;
            }
            return new TRect(aCol * (TROPHY_SCR_THUMB_GAP_W + TROPHY_SCR_THUMB_W) + TROPHY_SCR_THUMB_INSET_W,
                TROPHY_SCR_CAPTION_H + aRow * (TROPHY_SCR_ROW_GAP_W + ROW_HEIGHT), TROPHY_SCR_THUMB_W, TROPHY_SCR_THUMB_H);
        }
    }

    /// <summary>
    /// The challenge details popup (Sexy::TrophyDetailsDialog): the challenge's level thumbnail
    /// (three small ones for a three-level challenge, a question mark for random ones), its
    /// description, and a master badge that fades in over the picture once it has been won.
    /// </summary>
    public class TrophyDetailsDialog : ThumbnailDialog
    {
        public ThunderballApp mApp;                               // 0x1b4

        public string mText = "";                                 // 0x1b8

        public LevelInfo[] mLevels = new LevelInfo[3];            // 0x1bc

        public int mNumLevels;                                    // 0x1c8

        public bool mIsRandom;                                    // 0x1cc

        public bool mWon;                                         // 0x1cd

        public int mBadgeAlpha;                                   // 0x1d4 (0..40)

        public int mBadgeDelay;                                   // 0x1d8

        public TrophyDetailsDialog(ThunderballApp theApp, ThumbnailDialogListener theListener)
            : base(DialogId.DIALOG_TROPHY_DETAILS, theListener)
        {
            mApp = theApp;
        }

        public override void DrawContent(Graphics g)
        {
            base.DrawContent(g);
            if (!mIsRandom)
            {
                if (mNumLevels == 3)
                {
                    int w = TROPHY_DETAILS_W;
                    int h = TROPHY_DETAILS_H;
                    int aPad = 2;
                    TRect aTop = new TRect(mThumbRect.mX + mThumbRect.mWidth / 2 - w / 2, mThumbRect.mY + 2, w, h);
                    TRect aLeft = new TRect(mThumbRect.mX + aPad, mThumbRect.mY + mThumbRect.mHeight - h - aPad * 2, w, h);
                    TRect aRight = new TRect(mThumbRect.mX + mThumbRect.mWidth - w - aPad, mThumbRect.mY + mThumbRect.mHeight - h - aPad * 2, w, h);
                    g.SetColor(SexyUtil.MakeColor(0x808080));
                    SexyUtil.DrawLevelThumbnail(g, mLevels[2].mStage, mLevels[2].mLevel, aRight);
                    g.DrawRect(aRight);
                    SexyUtil.DrawLevelThumbnail(g, mLevels[1].mStage, mLevels[1].mLevel, aLeft);
                    g.DrawRect(aLeft);
                    SexyUtil.DrawLevelThumbnail(g, mLevels[0].mStage, mLevels[0].mLevel, aTop);
                    g.DrawRect(aTop);
                }
                else
                {
                    SexyUtil.DrawLevelThumbnail(g, mLevels[0].mStage, mLevels[0].mLevel, mThumbRect);
                    DrawThumbFrame(g);
                }
            }
            else
            {
                g.DrawImage(Res.IMAGE_DLG_QUESTIONMARK, mThumbRect.mX, mThumbRect.mY, mThumbRect.mWidth, mThumbRect.mHeight);
                g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
                int anOffsetX = mNumLevels < 10 ? TROPHY_DETAILS_LEVEL_NUM_SMALL_OFFSET_X : TROPHY_DETAILS_LEVEL_NUM_BIG_OFFSET_X;
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                g.DrawString(mNumLevels.ToString(), mThumbRect.mX + mThumbRect.mWidth - anOffsetX,
                    mThumbRect.mY + mThumbRect.mHeight - g.GetFont().GetDecorationOffset() * 2);
                DrawThumbFrame(g);
            }
            g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
            TRect aRect = mContentRect;
            aRect.Inflate(0, -20);
            aRect.Offset(0, 10);
            g.SetColor(SexyUtil.MakeColor(0xffff00));
            WriteWordWrapped(g, aRect, mText, -1, 0);
            if (mWon && mBadgeDelay == 0)
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, mBadgeAlpha * 0xff / 0x28));
                g.DrawImage(Res.IMAGE_MASTERBADGE, mThumbRect.mX, mThumbRect.mY, mThumbRect.mWidth, mThumbRect.mHeight);
                g.SetColorizeImages(true);
            }
        }

        public override void Update()
        {
            base.Update();
            if (!IsAnimating() && mWon)
            {
                if (mBadgeDelay < 1)
                {
                    if (mBadgeAlpha != 0x28)
                    {
                        mBadgeAlpha++;
                    }
                }
                else
                {
                    mBadgeDelay--;
                    if (mBadgeDelay == 0)
                    {
                        mBadgeAlpha = 0;
                    }
                }
            }
        }
    }
}
