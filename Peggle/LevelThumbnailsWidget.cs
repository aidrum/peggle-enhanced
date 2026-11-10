using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    public interface LevelThumbnailsWidgetListener
    {
        void SelectionChanged();
    }

    /// <summary>
    /// The scrolling grid of level thumbnails on the level screen (Sexy::LevelThumbnailsWidget).
    /// Peggle stages sit in a left column and Nights stages in a right one; the stage labels in the
    /// middle are shared, and the screen scrolls sideways between the two. Row 0 is Random Level.
    /// Port addition: further pages to the right hold the bonus stages (StageMgr.BONUS_PAGES: Nights
    /// bonus, Dual Shot, Extreme, WoW). Nights bonus and Dual Shot have their own labels; Extreme and
    /// WoW share theirs, like Peggle and Nights. The web demo levels (StageMgr.GetPeggleRowsPage) are
    /// rows under Peggle's stages on the Peggle page.
    /// </summary>
    public class LevelThumbnailsWidget : Widget, INavCells
    {
        public class LevelDisplayInfo
        {
            public bool mCleared;                                 // 0x0

            public bool mAced;                                    // 0x1

            public bool mLocked;                                  // 0x2

            public int mLockAlpha;                                // 0x4

            public string mName = "";                             // 0x8

            public LevelInfo mLevelInfo;                          // 0xc
        }

        public static int RANDOM_BUTTON_HEIGHT;

        public static int THUMB_STAGE_WIDTH;

        public static int ROW_HEIGHT;

        private static int aStringWidth = -1;

        public int mSelected;                                     // 0x78

        public ThunderballApp mApp;                               // 0x7c

        public LevelThumbnailsWidgetListener mListener;           // 0x80

        public List<LevelDisplayInfo> mLevels = new List<LevelDisplayInfo>(); // 0x84

        public bool mThumbnailDirty;                              // 0x90

        public MemoryImage mSelectedThumbnail;                    // 0x94

        /// <summary>Port addition: the web demo rows are shown on the Peggle page (Quick Play and Duel, LevelScreen.HasBonusPages).</summary>
        public bool mShowPeggleRows;

        public LevelThumbnailsWidget(ThunderballApp theApp, LevelThumbnailsWidgetListener theListener)
        {
            mApp = theApp;
            LoadDisplayInfo();
            mSelected = -1;
            mListener = theListener;
            mSelectedThumbnail = null;
            mThumbnailDirty = true;
        }

        public override void Dispose()
        {
            mSelectedThumbnail?.Dispose();
            mSelectedThumbnail = null;
            base.Dispose();
        }

        public void LoadDisplayInfo()
        {
            int aCount = 0x73;
            int aBonusStages = mApp.mStageMgr.GetNumBonusStages();
            if (aBonusStages > 0)
            {
                aCount = (StageMgr.BONUS_FIRST_STAGE + aBonusStages) * 5;
            }
            mLevels.Capacity = aCount;
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            for (int i = 0; i < aCount; i++)
            {
                LevelDisplayInfo anInfo = new LevelDisplayInfo();
                StageInfo aStage = mApp.mStageMgr.GetStageInfo(i / 5);
                LevelInfo aLevel = aStage?.GetLevelInfo(i % 5);
                anInfo.mLevelInfo = aLevel;
                anInfo.mName = aLevel != null ? aLevel.mDisplayName : "";
                anInfo.mLocked = !CanSelectLevel(i / 5, i % 5);
                if (aPlayer != null && aLevel != null)
                {
                    if (StageMgr.IsBonusStage(i / 5))
                    {
                        anInfo.mCleared = aPlayer.mBonusClearedLevelSet.Contains(aLevel.mName);
                        anInfo.mAced = aPlayer.mBonusAcedLevelSet.Contains(aLevel.mName);
                    }
                    else if (i < 0x37)
                    {
                        anInfo.mCleared = aPlayer.mPeggleClearedLevelSet.Contains(aLevel.mName);
                        anInfo.mAced = aPlayer.mPeggleAcedLevelSet.Contains(aLevel.mName);
                    }
                    else
                    {
                        anInfo.mCleared = aPlayer.mNightsClearedLevelSet.Contains(aLevel.mName);
                        anInfo.mAced = aPlayer.mNightsAcedLevelSet.Contains(aLevel.mName);
                    }
                }
                mLevels.Add(anInfo);
            }
        }

        public bool CanSelectLevel(int theStage, int theLevel)
        {
            if (theLevel < 0 || theLevel > 4)
            {
                return false;
            }
            // Port addition: a bonus stage can have fewer than five levels (the WoW addon's last).
            if (StageMgr.IsBonusStage(theStage) && mApp.mStageMgr.GetStageInfo(theStage)?.GetLevelInfo(theLevel) == null)
            {
                return false;
            }
            if (mApp.mPlayerInfo == null)
            {
                return true;
            }
            return mApp.mPlayerInfo.IsLevelUnlocked(theStage, theLevel);
        }

        /// <summary>Port addition: the bonus stages shown under Peggle's (the web demo levels), or null when they aren't shown.</summary>
        public BonusPage GetPeggleRows()
        {
            return mShowPeggleRows ? StageMgr.GetPeggleRowsPage() : null;
        }

        /// <summary>Port addition: the number of rows of stages on the Peggle page.</summary>
        public int GetNumPeggleRows()
        {
            BonusPage aRows = GetPeggleRows();
            return 0xb + (aRows != null ? aRows.mNumStages : 0);
        }

        public int GetRandomLevelIndex()
        {
            // Port: the bonus levels aren't counted, so the index stays clear of their slots.
            return mApp.mStageMgr.GetNumLevels() - mApp.mStageMgr.GetNumBonusLevels();
        }

        /// <summary>
        /// Port addition: the scroll x of a page: 0 Peggle, 1 Nights, then the bonus pages, each a page
        /// width further right (labels on the left, thumbnails at the page's right). The WoW page
        /// follows the Extreme page the way Nights follows Peggle, a stage width on (IsSharedLeftPage).
        /// </summary>
        public int GetPageX(int thePage)
        {
            return GetPageX(thePage, ((Widget)mParent).mWidth);
        }

        /// <summary>Port addition: GetPageX for a page (scroll view) width, before the widget is placed.</summary>
        public static int GetPageX(int thePage, int theViewWidth)
        {
            if (thePage == 0)
            {
                return 0;
            }
            int x = THUMB_STAGE_WIDTH;
            for (int aPage = 2; aPage <= thePage; aPage++)
            {
                x += IsSharedLeftPage(aPage - 1) ? THUMB_STAGE_WIDTH : theViewWidth;
            }
            return x;
        }

        /// <summary>Port addition: the x of a bonus page's first thumbnail.</summary>
        public int GetPageThumbX(int thePage)
        {
            int x = GetPageX(thePage);
            return IsSharedLeftPage(thePage) ? x + LEVEL_SCREEN_STAGE_THUMB_INSET : x + ((Widget)mParent).mWidth - THUMB_STAGE_WIDTH;
        }

        /// <summary>
        /// Port addition: true for the Extreme page when the WoW page follows it. The two share their
        /// stage labels like Peggle and Nights: Extreme's thumbnails on the left, the labels between.
        /// </summary>
        public static bool IsSharedLeftPage(int thePage)
        {
            int anIndex = thePage - 2;
            return anIndex >= 0 && anIndex + 1 < StageMgr.GetNumBonusPages()
                && StageMgr.GetBonusPageAt(anIndex).mLogoCel == GameArrowButton.LOGO_EXTREME
                && StageMgr.GetBonusPageAt(anIndex + 1).mLogoCel == GameArrowButton.LOGO_WOW;
        }

        /// <summary>
        /// Port addition: the alpha of a row (label and box) only the right page of a shared pair has: 0 with the
        /// view on the left page, 255 on the right one, in between while scrolling (the client is
        /// moved by minus the scroll position).
        /// </summary>
        private int GetRightPageLabelAlpha(int theRightPage)
        {
            int aLeftX = GetPageX(theRightPage - 1);
            int aRightX = GetPageX(theRightPage);
            float t = (-mX - aLeftX) / (float)(aRightX - aLeftX);
            return (int)(System.Math.Clamp(t, 0.0f, 1.0f) * 0xff);
        }

        /// <summary>Port addition: the alpha of the Nights page's own rows and labels: 0 on the Peggle page, 255 on Nights.</summary>
        private int GetNightsLabelAlpha()
        {
            return GetRightPageLabelAlpha(1);
        }

        /// <summary>Port addition: true for the WoW page when it shares labels with Extreme.</summary>
        public static bool IsSharedRightPage(int thePage)
        {
            return IsSharedLeftPage(thePage - 1);
        }

        public bool CanSelectItem(int theIndex)
        {
            if (theIndex < 0)
            {
                return false;
            }
            if (theIndex == GetRandomLevelIndex())
            {
                return CanSelectLevel(0, 1) || CanSelectLevel(0xb, 1);
            }
            return CanSelectLevel(theIndex / 5, theIndex % 5);
        }

        private void DrawStageThumbs(Graphics g, int theStage, int theX, int theY)
        {
            int x = theX;
            for (int i = 0; i < 5; i++)
            {
                LevelDisplayInfo anInfo = mLevels[theStage * 5 + i];
                if (anInfo.mLevelInfo == null && StageMgr.IsBonusStage(theStage))
                {
                    break;  // Port addition: a short bonus stage leaves the rest of its row empty.
                }
                TRect aRect = new TRect(x, theY, LEVEL_SCREEN_STAGE_THUMB_W, LEVEL_SCREEN_STAGE_THUMB_H);
                SexyUtil.DrawLevelThumbnail(g, theStage, i, aRect);
                if (!anInfo.mLocked)
                {
                    int aRibbons = 1;
                    if (anInfo.mCleared)
                    {
                        g.DrawImage(Res.IMAGE_TINYBLUERIBBON, LEVEL_SCREEN_STAGE_THUMB_W + x - Res.IMAGE_TINYBLUERIBBON.mWidth,
                            LEVEL_SCREEN_STAGE_THUMB_H + theY - Res.IMAGE_TINYBLUERIBBON.mHeight);
                        aRibbons++;
                    }
                    if (anInfo.mAced)
                    {
                        g.DrawImage(Res.IMAGE_TINYACEDRIBBON, LEVEL_SCREEN_STAGE_THUMB_W + x - Res.IMAGE_TINYACEDRIBBON.mWidth * aRibbons,
                            LEVEL_SCREEN_STAGE_THUMB_H + theY - Res.IMAGE_TINYACEDRIBBON.mHeight);
                    }
                }
                else
                {
                    g.SetColor(new Color(0, 0, 0, 0x96));
                    g.FillRect(aRect);
                    if (anInfo.mLockAlpha > 0)
                    {
                        g.SetColorizeImages(true);
                        g.SetColor(SexyUtil.MakeColor(0xffffff, anInfo.mLockAlpha));
                        g.DrawImage(Res.IMAGE_LS_LOCK_SMALL, LEVEL_SCREEN_STAGE_THUMB_W / 2 + x - Res.IMAGE_LS_LOCK_SMALL.mWidth / 2,
                            LEVEL_SCREEN_STAGE_THUMB_H / 2 + theY - Res.IMAGE_LS_LOCK_SMALL.mHeight / 2);
                        g.SetColorizeImages(false);
                    }
                }
                x += LEVEL_SCREEN_STAGE_THUMB_GAP + LEVEL_SCREEN_STAGE_THUMB_W;
            }
        }

        private void DrawThumbOutlines(Graphics g, int theX, int theY, int theColor, int theCount = 5)
        {
            g.SetColor(SexyUtil.MakeColor(theColor));
            int x = theX;
            for (int i = 0; i < theCount; i++)
            {
                g.DrawRect(x, theY, LEVEL_SCREEN_STAGE_THUMB_W - 1, LEVEL_SCREEN_STAGE_THUMB_H - 1);
                x += LEVEL_SCREEN_STAGE_THUMB_GAP + LEVEL_SCREEN_STAGE_THUMB_W;
            }
        }

        /// <summary>Port addition: the Nights DLC page's stages are named after their packs, in two lines.</summary>
        private static readonly string[][] NIGHTSDLC_STAGE_NAMES =
        {
            new[] { "Spring Fever:", "Day 1" },
            new[] { "Spring Fever:", "Day 2" },
            new[] { "Holiday", "2008" },
            new[] { "Peggle Art", "Contest" },
        };

        private void DrawStageLabel(Graphics g, Font theLabelFont, int theLabelX, int theLabelWidth, int theRowY, int theStage, BonusPage theBonusPage = null,
            int theAlpha = 0xff)
        {
            if (theBonusPage != null && StageMgr.IsCustomStage(theBonusPage.mFirstStage + theStage))
            {
                // Port addition: the custom level pack stages under the Nights DLC page's own.
                DrawCustomStageLabel(g, theLabelFont, theLabelX, theRowY, theBonusPage.mFirstStage + theStage, theAlpha);
                return;
            }
            string aTop = SexyAppBase.Loc("Stage");
            string aBottom = (theStage + 1).ToString();
            if (theBonusPage != null && theBonusPage.mLogoCel == GameArrowButton.LOGO_NIGHTSDLC && theStage < NIGHTSDLC_STAGE_NAMES.Length)
            {
                aTop = SexyAppBase.Loc(NIGHTSDLC_STAGE_NAMES[theStage][0]);
                aBottom = SexyAppBase.Loc(NIGHTSDLC_STAGE_NAMES[theStage][1]);
                theLabelWidth = theLabelFont.StringWidth(aTop);
            }
            else if (theBonusPage != null && theBonusPage.mOnPeggleRows)
            {
                // Port addition: the web demo rows under Peggle's stages.
                aTop = SexyAppBase.Loc("Demo Levels");
                theLabelWidth = theLabelFont.StringWidth(aTop);
            }
            else if (theBonusPage != null && theBonusPage.HasAdventure && theStage >= theBonusPage.AdventureStages)
            {
                // Port addition: the stages after a page's Adventure (the WoW addon levels).
                aTop = SexyAppBase.Loc("WoW Addon");
                aBottom = (theStage - theBonusPage.AdventureStages + 1).ToString();
                theLabelWidth = theLabelFont.StringWidth(aTop);
            }
            g.SetColor(new Color(0xff, 0xff, 0xff, theAlpha));
            g.SetFont(theLabelFont);
#if PEGGLE_IPHONE
            int y = theRowY + ROW_HEIGHT / 2;
#else
            int y = theRowY + ROW_HEIGHT / 2 + LEVEL_SCREEN_STAGE_TEXT_OFFSET_Y;
#endif
            g.DrawString(aTop, theLabelX - theLabelWidth / 2, y);
            g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
            y += g.GetFont().GetMaxCharHeight();
            g.DrawString(aBottom, theLabelX - g.GetFont().StringWidth(aBottom) / 2, y);
        }

        /// <summary>
        /// Port addition: a custom level pack stage is labelled with its name from the pack, word-wrapped to
        /// the label column in up to three lines (the last cut short with "..." if it still doesn't fit) and
        /// centred on the row.
        /// </summary>
        private void DrawCustomStageLabel(Graphics g, Font theFont, int theLabelX, int theRowY, int theStage, int theAlpha)
        {
            string aName = mApp.mStageMgr.GetStageInfo(theStage)?.mCustomName ?? "";
            int aMaxWidth = System.Math.Max(40, ((Widget)mParent).mWidth - THUMB_STAGE_WIDTH - 8);
            List<string> aLines = WrapLabel(theFont, aName, aMaxWidth, 3);
            int aLineHeight = theFont.GetMaxCharHeight();
            g.SetColor(new Color(0xff, 0xff, 0xff, theAlpha));
            g.SetFont(theFont);
            int y = theRowY + ROW_HEIGHT / 2 - aLineHeight * aLines.Count / 2 + theFont.GetAscent();
            foreach (string aLine in aLines)
            {
                g.DrawString(aLine, theLabelX - theFont.StringWidth(aLine) / 2, y);
                y += aLineHeight;
            }
        }

        private static List<string> WrapLabel(Font theFont, string theText, int theMaxWidth, int theMaxLines)
        {
            List<string> aLines = new List<string>();
            string aLine = "";
            foreach (string aWord in theText.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
            {
                string aTry = aLine.Length == 0 ? aWord : aLine + " " + aWord;
                if (aLine.Length == 0 || theFont.StringWidth(aTry) <= theMaxWidth)
                {
                    aLine = aTry;
                }
                else
                {
                    aLines.Add(aLine);
                    aLine = aWord;
                }
            }
            if (aLine.Length > 0)
            {
                aLines.Add(aLine);
            }
            if (aLines.Count > theMaxLines)
            {
                aLines[theMaxLines - 1] = string.Join(" ", aLines.GetRange(theMaxLines - 1, aLines.Count - theMaxLines + 1));
                aLines.RemoveRange(theMaxLines, aLines.Count - theMaxLines);
            }
            for (int i = 0; i < aLines.Count; i++)
            {
                if (theFont.StringWidth(aLines[i]) > theMaxWidth)
                {
                    string aCut = aLines[i];
                    while (aCut.Length > 1 && theFont.StringWidth(aCut + "...") > theMaxWidth)
                    {
                        aCut = aCut.Substring(0, aCut.Length - 1);
                    }
                    aLines[i] = aCut.TrimEnd() + "...";
                }
            }
            return aLines;
        }

        public override void Draw(Graphics g)
        {
            mApp.LoadQuickplayResources();
            Font aLabelFont = Res.FONT_OVERLOAD12OUTLINE;
            Widget aParent = (Widget)mParent;
            TRect aVisible = new TRect(aParent.mX, -mY, aParent.mWidth, aParent.mHeight);
            int aLabelWidth = aLabelFont.StringWidth(SexyAppBase.Loc("Stage"));
            int aLabelX = (aParent.mWidth - THUMB_STAGE_WIDTH) / 2 + THUMB_STAGE_WIDTH;
            int aRowStep = LEVEL_SCREEN_STAGE_ROW_GAP + ROW_HEIGHT;
            int aBonusPages = mApp.mStageMgr.GetNumBonusStages() > 0 ? StageMgr.GetNumBonusPages() : 0;

            TRect aRandomRect = new TRect(0, 0, mWidth, ROW_HEIGHT);
            if (aRandomRect.Intersects(aVisible))
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xd033c3, 0x7f));
                SexyUtil.MyDrawImageBox(g, aRandomRect, Res.IMAGE_DLG_GROUPRECT);
                g.SetColor(mSelected == GetRandomLevelIndex() ? new Color(0xff, 0xff, 0) : Color.White);
                g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
                string aText = SexyAppBase.Loc("RANDOM LEVEL");
                if (aStringWidth < 0)
                {
                    aStringWidth = g.GetFont().StringWidth(aText);
                }
                int aTextY = aRandomRect.mY + aRandomRect.mHeight / 2 + g.GetFont().GetCenteredHeight();
                g.DrawString(aText, aParent.mWidth / 2 - aStringWidth / 2, aTextY);
                g.DrawString(aText, THUMB_STAGE_WIDTH + aParent.mWidth / 2 - aStringWidth / 2, aTextY);
                for (int aPage = 2; aPage < aBonusPages + 2; aPage++)
                {
                    g.DrawString(aText, GetPageX(aPage) + aParent.mWidth / 2 - aStringWidth / 2, aTextY);
                }
                g.SetColorizeImages(false);
                PlayerInfo aPlayer = mApp.mPlayerInfo;
                if (aPlayer != null && !aPlayer.IsLevelUnlocked(0, 1) && !aPlayer.IsLevelUnlocked(0xb, 1))
                {
                    g.SetColor(new Color(0, 0, 0, 0x96));
                    g.FillRect(aRandomRect);
                }
            }

            for (int aStage = 0; aStage < 0xb; aStage++)
            {
                TRect aRowRect = new TRect(0, (aStage + 1) * aRowStep, mWidth, ROW_HEIGHT);
                if (!aRowRect.Intersects(aVisible))
                {
                    continue;
                }
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForStage(aStage, ThunderballApp.mGameLevelPack, true), 0x7f));
                // Port: the box spans the Peggle and Nights pages only, not the bonus ones.
                SexyUtil.MyDrawImageBox(g, new TRect(0, aRowRect.mY, THUMB_STAGE_WIDTH + aParent.mWidth, ROW_HEIGHT), Res.IMAGE_DLG_GROUPRECT);
                g.SetColorizeImages(false);
                int aThumbY = aRowRect.mY + 1;
                DrawStageThumbs(g, aStage, LEVEL_SCREEN_STAGE_THUMB_INSET, aThumbY);
                DrawThumbOutlines(g, LEVEL_SCREEN_STAGE_THUMB_INSET, aThumbY, 0xeac76a);
                DrawStageLabel(g, aLabelFont, aLabelX, aLabelWidth, aRowRect.mY, aStage);
            }

            // Port addition: the web demo rows under Peggle's stages. The first shares its row with
            // Nights' twelfth stage: one box across both pages, and the label changes from "Demo Levels"
            // to Nights' "Stage 12" as the view scrolls over. The rows only Peggle has fade out, box and
            // label, as the view leaves the Peggle page (like the WoW addon rows from the WoW page).
            BonusPage aDemo = GetPeggleRows();
            int aNightsAlpha = GetNightsLabelAlpha();
            int aDemoRows = aDemo != null ? aDemo.mNumStages : 0;
            for (int aRow = 0; aRow < aDemoRows; aRow++)
            {
                int aPageRow = 0xb + aRow;
                TRect aRowRect = new TRect(0, (aPageRow + 1) * aRowStep, mWidth, ROW_HEIGHT);
                if (!aRowRect.Intersects(aVisible))
                {
                    continue;
                }
                bool isShared = aPageRow < 0xc;
                int aRowAlpha = 0xff - aNightsAlpha;
                if (isShared || aRowAlpha > 0)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForStage(aPageRow, ThunderballApp.mGameLevelPack, true),
                        isShared ? 0x7f : 0x7f * aRowAlpha / 0xff));
                    SexyUtil.MyDrawImageBox(g, new TRect(0, aRowRect.mY, isShared ? THUMB_STAGE_WIDTH + aParent.mWidth : aParent.mWidth, ROW_HEIGHT),
                        Res.IMAGE_DLG_GROUPRECT);
                    g.SetColorizeImages(false);
                }
                int aStage = aDemo.mFirstStage + aRow;
                int aThumbY = aRowRect.mY + 1;
                DrawStageThumbs(g, aStage, LEVEL_SCREEN_STAGE_THUMB_INSET, aThumbY);
                DrawThumbOutlines(g, LEVEL_SCREEN_STAGE_THUMB_INSET, aThumbY, aDemo.mOutlineColor, mApp.mStageMgr.GetStageInfo(aStage).mLevels.Count);
                if (aRowAlpha > 0)
                {
                    DrawStageLabel(g, aLabelFont, aLabelX, aLabelWidth, aRowRect.mY, aRow, aDemo, aRowAlpha);
                }
            }

            for (int aRow = 0; aRow < 0xc; aRow++)
            {
                int aStage = aRow + 0xb;
                TRect aRowRect = new TRect(aParent.mWidth, (aRow + 1) * aRowStep, mWidth, ROW_HEIGHT);
                if (!aRowRect.Intersects(aVisible))
                {
                    continue;
                }
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForStage(aRow, ThunderballApp.mGameLevelPack, true), 0x7f));
                if (aRow > 10 && aDemoRows == 0)
                {
                    // Nights has a twelfth stage with no Peggle row to share its box (port: unless the
                    // web demo rows are shown, whose first draws it across both pages).
                    SexyUtil.MyDrawImageBox(g, new TRect(THUMB_STAGE_WIDTH, aRowRect.mY, aParent.mWidth, ROW_HEIGHT), Res.IMAGE_DLG_GROUPRECT);
                }
                g.SetColorizeImages(false);
                // Port: was mWidth - THUMB_STAGE_WIDTH, which is a bonus page now that mWidth includes them.
                int aThumbX = aParent.mWidth;
                int aThumbY = aRowRect.mY + 1;
                DrawStageThumbs(g, aStage, aThumbX, aThumbY);
                DrawThumbOutlines(g, aThumbX, aThumbY, 0xff55e9);
                // Port: sharing its row with the first web demo row, the label fades in from the Peggle page.
                int aLabelAlpha = aDemoRows > 0 ? aNightsAlpha : 0xff;
                if (aRow > 10 && aLabelAlpha > 0)
                {
                    DrawStageLabel(g, aLabelFont, aLabelX, aLabelWidth, aRowRect.mY, aRow, null, aLabelAlpha);
                }
            }

            // Port addition: the bonus pages, each laid out like the Nights page, a page further right.
            for (int aPage = 2; aPage < aBonusPages + 2; aPage++)
            {
                BonusPage aBonus = StageMgr.GetBonusPageAt(aPage - 2);
                // Extreme and WoW share their labels: a row both have gets one box across the two
                // pages (drawn with Extreme's row) and one label (drawn with WoW's), as Peggle/Nights do.
                bool isLeft = IsSharedLeftPage(aPage);
                bool isRight = IsSharedRightPage(aPage);
                BonusPage aPartner = isLeft ? StageMgr.GetBonusPageAt(aPage - 1) : isRight ? StageMgr.GetBonusPageAt(aPage - 3) : null;
                int aPageLabelX = GetPageX(aPage) + aLabelX - (isLeft ? 0 : THUMB_STAGE_WIDTH);
                for (int aRow = 0; aRow < aBonus.mNumStages; aRow++)
                {
                    int aStage = aRow + aBonus.mFirstStage;
                    bool isShared = aPartner != null && aRow < aPartner.mNumStages;
                    TRect aRowRect = new TRect(GetPageX(aPage), (aRow + 1) * aRowStep, aParent.mWidth, ROW_HEIGHT);
                    // aVisible's x isn't the scroll position, so only test the rows vertically, as the others do.
                    if (!new TRect(0, aRowRect.mY, mWidth, ROW_HEIGHT).Intersects(aVisible))
                    {
                        continue;
                    }
                    // A row only the WoW page has (the WoW addon stages) keeps its label and box off the
                    // Extreme page, whose view shares the label column: they fade in as the view
                    // scrolls from Extreme to WoW.
                    int aRowAlpha = isRight && !isShared ? GetRightPageLabelAlpha(aPage) : 0xff;
                    if (!(isRight && isShared) && aRowAlpha > 0)
                    {
                        TRect aBoxRect = aRowRect;
                        if (isLeft && isShared)
                        {
                            aBoxRect.mWidth += THUMB_STAGE_WIDTH;
                        }
                        g.SetColorizeImages(true);
                        g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForStage(aRow, ThunderballApp.mGameLevelPack, true), 0x7f * aRowAlpha / 0xff));
                        SexyUtil.MyDrawImageBox(g, aBoxRect, Res.IMAGE_DLG_GROUPRECT);
                        g.SetColorizeImages(false);
                    }
                    int aThumbX = GetPageThumbX(aPage);
                    int aThumbY = aRowRect.mY + 1;
                    DrawStageThumbs(g, aStage, aThumbX, aThumbY);
                    DrawThumbOutlines(g, aThumbX, aThumbY, aBonus.mOutlineColor, mApp.mStageMgr.GetStageInfo(aStage).mLevels.Count);
                    if (!(isLeft && isShared) && aRowAlpha > 0)
                    {
                        DrawStageLabel(g, aLabelFont, aPageLabelX, aLabelWidth, aRowRect.mY, aRow, aBonus, aRowAlpha);
                    }
                }
            }
        }

        public string GetSelectedLevelName()
        {
            if (mSelected < 0)
            {
                return "";
            }
            if (mSelected == GetRandomLevelIndex())
            {
                return SexyAppBase.Loc("Random Level");
            }
            return mLevels[mSelected].mName;
        }

        /// <summary>The selected level's thumbnail with its ribbons, rendered once per selection.</summary>
        public Image GetSelectedThumbnail()
        {
            if (mSelectedThumbnail != null && !mThumbnailDirty)
            {
                return mSelectedThumbnail;
            }
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
            SexyUtil.DrawLevelThumbnail(g, mSelected / 5, mSelected % 5, new TRect(0, 0, w, h));
            int aRibbons = 1;
            LevelDisplayInfo anInfo = mLevels[mSelected];
            if (anInfo.mCleared)
            {
                Image anImage = Res.IMAGE_TINYBLUERIBBON;
                g.DrawImage(anImage, w - anImage.mWidth, h - anImage.mHeight, anImage.mWidth, anImage.mHeight);
                aRibbons++;
            }
            if (anInfo.mAced)
            {
                Image anImage = Res.IMAGE_TINYACEDRIBBON;
                g.DrawImage(anImage, w - anImage.mWidth * aRibbons, h - anImage.mHeight, anImage.mWidth, anImage.mHeight);
            }
            g.SetColor(SexyUtil.MakeColor(0x808080));
            g.DrawRect(0, 0, w - 1, h - 1);
            g.EndOffscreen(inFrame);
            mThumbnailDirty = false;
            return mSelectedThumbnail;
        }

        public TRect GetSelectedRect()
        {
            int aRow = mSelected / 5 + 1;
            int aCol = mSelected % 5;
            int x = LEVEL_SCREEN_STAGE_THUMB_INSET + aCol * (LEVEL_SCREEN_STAGE_THUMB_W + LEVEL_SCREEN_STAGE_THUMB_GAP);
            BonusPage aSelectedPage = StageMgr.GetBonusPage(mSelected / 5);
            if (aSelectedPage != null && aSelectedPage.mOnPeggleRows)
            {
                // Port addition: the web demo rows follow Peggle's stages on the Peggle page.
                aRow = 0xb + mSelected / 5 - aSelectedPage.mFirstStage + 1;
            }
            else if (StageMgr.IsBonusStage(mSelected / 5))
            {
                // Port addition: a bonus page puts its thumbnails where the Nights page does.
                // The Extreme page, when it shares labels with WoW, puts them where the Peggle page does.
                aRow = mSelected / 5 - StageMgr.GetBonusPage(mSelected / 5).mFirstStage + 1;
                if (!IsSharedLeftPage(StageMgr.GetBonusPageIndex(mSelected / 5) + 2))
                {
                    x = aCol * (LEVEL_SCREEN_STAGE_THUMB_GAP + LEVEL_SCREEN_STAGE_THUMB_W) + (((Widget)mParent).mWidth - THUMB_STAGE_WIDTH);
                }
            }
            else if (aRow > 0xb)
            {
                aRow = mSelected / 5 - 10;
                x = aCol * (LEVEL_SCREEN_STAGE_THUMB_GAP + LEVEL_SCREEN_STAGE_THUMB_W) + (((Widget)mParent).mWidth - THUMB_STAGE_WIDTH);
            }
            return new TRect(x, aRow * (LEVEL_SCREEN_STAGE_ROW_GAP + ROW_HEIGHT) + 1, LEVEL_SCREEN_STAGE_THUMB_W, LEVEL_SCREEN_STAGE_THUMB_H);
        }

        public override void Update()
        {
            base.Update();
            foreach (LevelDisplayInfo anInfo in mLevels)
            {
                if (anInfo.mLocked && anInfo.mLockAlpha > 0)
                {
                    anInfo.mLockAlpha -= 8;
                }
            }
            MarkDirty();
        }

        public override void MouseUp(int x, int y, int theClickCount)
        {
            if (x <= LEVEL_SCREEN_STAGE_THUMB_INSET)
            {
                return;
            }
            int aRow = y / (LEVEL_SCREEN_STAGE_ROW_GAP + ROW_HEIGHT);
            int aCol = (x - LEVEL_SCREEN_STAGE_THUMB_INSET) / (LEVEL_SCREEN_STAGE_THUMB_GAP + LEVEL_SCREEN_STAGE_THUMB_W);
            bool isRandom = aRow == 0;
            Widget aParent = (Widget)mParent;
            int aPage = 0;
            BonusPage aBonus = null;
            if (GetPageX(2) < x)
            {
                // Port addition: a bonus page.
                // The pages aren't evenly spaced (GetPageX), so find the one whose thumbnails x is on.
                aPage = -1;
                for (int p = 2; p < StageMgr.GetNumBonusPages() + 2; p++)
                {
                    int aPageThumbX = GetPageThumbX(p);
                    int aMinX = IsSharedLeftPage(p) ? aPageThumbX : aPageThumbX + LEVEL_SCREEN_STAGE_THUMB_GAP * 2;
                    if (aMinX < x && x < aPageThumbX + THUMB_STAGE_WIDTH - LEVEL_SCREEN_STAGE_THUMB_INSET)
                    {
                        aPage = p;
                        break;
                    }
                }
                if (aPage < 0)
                {
                    return;
                }
                aBonus = StageMgr.GetBonusPageAt(aPage - 2);
                int aThumbX = GetPageThumbX(aPage);
                if (!isRandom && aRow > aBonus.mNumStages)
                {
                    return;
                }
                aRow += aBonus.mFirstStage;
                aCol = (x - aThumbX) / (LEVEL_SCREEN_STAGE_THUMB_GAP + LEVEL_SCREEN_STAGE_THUMB_W);
            }
            else if (THUMB_STAGE_WIDTH < x)
            {
                // Port: mWidth now includes the bonus pages, so the Nights bounds use the page width.
                if (x <= LEVEL_SCREEN_STAGE_THUMB_GAP * 2 + aParent.mWidth)
                {
                    return;
                }
                if (THUMB_STAGE_WIDTH + aParent.mWidth - LEVEL_SCREEN_STAGE_THUMB_INSET <= x)
                {
                    return;
                }
                if (aRow > 0xc)
                {
                    return;  // Port: below Nights' last stage (the list is taller with the web demo rows).
                }
                aRow += 0xb;
                aCol = (x - aParent.mWidth) / (LEVEL_SCREEN_STAGE_THUMB_GAP + LEVEL_SCREEN_STAGE_THUMB_W);
            }
            else if (aRow > 0xb)
            {
                // Port addition: the web demo rows under Peggle's stages.
                BonusPage aDemo = GetPeggleRows();
                if (aDemo == null || aRow > GetNumPeggleRows())
                {
                    return;
                }
                aBonus = aDemo;
                aRow = aDemo.mFirstStage + (aRow - 0xb);
            }
            int anIndex = isRandom ? GetRandomLevelIndex() : (aRow - 1) * 5 + aCol;
            if (!CanSelectItem(anIndex))
            {
                // The original writes past the end of the list when the random row is locked.
                if (anIndex >= 0 && anIndex < mLevels.Count)
                {
                    mLevels[anIndex].mLockAlpha = 0xff;
                }
                return;
            }
            if (mSelected != anIndex)
            {
                mThumbnailDirty = true;
                mSelected = anIndex;
                int aScrollX = 0;
                int aScrollRow = aRow;
                if (aBonus != null && aBonus.mOnPeggleRows)
                {
                    aScrollRow = aRow - aBonus.mFirstStage + 0xb;
                }
                else if (aBonus != null)
                {
                    aScrollRow = aRow - aBonus.mFirstStage;
                    aScrollX = GetPageX(aPage);
                }
                else if (aRow > 0xb)
                {
                    aScrollRow = aRow - 0xb;
                    aScrollX = THUMB_STAGE_WIDTH;
                }
                ((ScrollWidget)mParent).ScrollRectIntoView(new TRect(aScrollX, aScrollRow * (LEVEL_SCREEN_STAGE_ROW_GAP + ROW_HEIGHT),
                    aParent.mWidth, ROW_HEIGHT), true);
            }
            mListener.SelectionChanged();
        }

        // ---------------------------------------------------------------- port addition: menu navigation

        /// <summary>Port addition (menu navigation): one thumbnail, or a page's Random Level row (row 0).</summary>
        public struct NavCell
        {
            public int mIndex;

            public int mPage;

            public int mRow;

            public int mCol;

            public TRect mRect;

            /// <summary>Where A clicks, when not mRect (the Random Level row).</summary>
            public TRect? mClickRect;
        }

        /// <summary>Port addition (menu navigation): only this page's cells can be selected (-1 for all pages).</summary>
        public int mNavOnlyPage = -1;

        private readonly List<NavCell> mNavCells = new List<NavCell>();

        public int GetNumNavPages()
        {
            return 2 + (mShowPeggleRows ? StageMgr.GetNumBonusPages() : 0);
        }

        /// <summary>Port addition (menu navigation): the x of a page's first thumbnail, in the widget.</summary>
        private int GetNavThumbX(int thePage)
        {
            if (thePage == 0)
            {
                return LEVEL_SCREEN_STAGE_THUMB_INSET;
            }
            return GetPageThumbX(thePage);
        }

        /// <summary>Port addition (menu navigation): the stages shown on a page, top to bottom.</summary>
        public List<int> GetNavPageStages(int thePage)
        {
            List<int> aStages = new List<int>();
            if (thePage == 0)
            {
                for (int i = 0; i < NUM_STAGES_PEGGLE; i++)
                {
                    aStages.Add(i);
                }
                BonusPage aRows = GetPeggleRows();
                if (aRows != null)
                {
                    for (int i = 0; i < aRows.mNumStages; i++)
                    {
                        aStages.Add(aRows.mFirstStage + i);
                    }
                }
            }
            else if (thePage == 1)
            {
                for (int i = 0; i < NUM_STAGES_NIGHTS; i++)
                {
                    aStages.Add(NUM_STAGES_PEGGLE + i);
                }
            }
            else if (thePage - 2 < StageMgr.GetNumBonusPages())
            {
                BonusPage aPage = StageMgr.GetBonusPageAt(thePage - 2);
                for (int i = 0; i < aPage.mNumStages; i++)
                {
                    aStages.Add(aPage.mFirstStage + i);
                }
            }
            return aStages;
        }

        private void BuildNavCells()
        {
            mNavCells.Clear();
            if (mParent == null)
            {
                return;
            }
            int aStep = LEVEL_SCREEN_STAGE_THUMB_W + LEVEL_SCREEN_STAGE_THUMB_GAP;
            int aRowStep = LEVEL_SCREEN_STAGE_ROW_GAP + ROW_HEIGHT;
            for (int aPage = 0; aPage < GetNumNavPages(); aPage++)
            {
                if (mNavOnlyPage >= 0 && aPage != mNavOnlyPage)
                {
                    continue;
                }
                int aX = GetNavThumbX(aPage);
                // The highlight hugs the RANDOM LEVEL text (centred in the view, as Draw puts it); A still clicks the
                // row where its thumbnails are, which is where MouseUp looks.
                Font aRandomFont = Res.FONT_OVERLOAD16OUTLINE;
                int aTextW = aRandomFont.StringWidth(SexyAppBase.Loc("RANDOM LEVEL"));
                int aTextH = aRandomFont.GetHeight();
                int aPad = LEVEL_SCREEN_STAGE_THUMB_GAP;
                mNavCells.Add(new NavCell
                {
                    mIndex = GetRandomLevelIndex(), mPage = aPage, mRow = 0, mCol = 2,
                    mRect = new TRect(GetPageX(aPage) + ((Widget)mParent).mWidth / 2 - aTextW / 2 - aPad,
                        System.Math.Max(0, ROW_HEIGHT / 2 - aTextH / 2 - aPad / 2), aTextW + aPad * 2,
                        System.Math.Min(ROW_HEIGHT, aTextH + aPad)),
                    mClickRect = new TRect(aX, 0, aStep * 5 - LEVEL_SCREEN_STAGE_THUMB_GAP, ROW_HEIGHT)
                });
                List<int> aStages = GetNavPageStages(aPage);
                for (int aRow = 0; aRow < aStages.Count; aRow++)
                {
                    int aStage = aStages[aRow];
                    for (int aCol = 0; aCol < 5; aCol++)
                    {
                        int anIndex = aStage * 5 + aCol;
                        if (anIndex >= mLevels.Count ||
                            (StageMgr.IsBonusStage(aStage) && mApp.mStageMgr.GetStageInfo(aStage)?.GetLevelInfo(aCol) == null))
                        {
                            continue;
                        }
                        mNavCells.Add(new NavCell
                        {
                            mIndex = anIndex, mPage = aPage, mRow = aRow + 1, mCol = aCol,
                            mRect = new TRect(aX + aCol * aStep, (aRow + 1) * aRowStep + 1, LEVEL_SCREEN_STAGE_THUMB_W, LEVEL_SCREEN_STAGE_THUMB_H)
                        });
                    }
                }
            }
        }

        public NavCell GetNavCell(int theCell)
        {
            return mNavCells[theCell];
        }

        /// <summary>Port addition (menu navigation): the cell on a page nearest a row and column, or -1.</summary>
        public int FindNavCell(int thePage, int theRow, int theCol)
        {
            int aBest = -1;
            int aBestScore = int.MaxValue;
            for (int i = 0; i < mNavCells.Count; i++)
            {
                NavCell aCell = mNavCells[i];
                if (aCell.mPage != thePage)
                {
                    continue;
                }
                int aScore = System.Math.Abs(aCell.mRow - theRow) * 10 + System.Math.Abs(aCell.mCol - theCol);
                if (aScore < aBestScore)
                {
                    aBestScore = aScore;
                    aBest = i;
                }
            }
            return aBest;
        }

        public int GetNavCellCount()
        {
            BuildNavCells();
            return mNavCells.Count;
        }

        public TRect GetNavCellRect(int theCell)
        {
            return theCell >= 0 && theCell < mNavCells.Count ? mNavCells[theCell].mRect : default(TRect);
        }

        /// <summary>A on a thumbnail or the Random Level row: a click on it.</summary>
        public void NavCellActivate(int theCell)
        {
            if (theCell < 0 || theCell >= mNavCells.Count)
            {
                return;
            }
            TRect aRect = mNavCells[theCell].mClickRect ?? mNavCells[theCell].mRect;
            MouseUp(aRect.mX + aRect.mWidth / 2, aRect.mY + aRect.mHeight / 2, 1);
        }

        public override bool IsNavigable()
        {
            return true;
        }
    }
}
