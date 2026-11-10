using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The player stats screen (Sexy::AdventureScreen), opened from the main menu's Stats button
    /// after picking Peggle or Nights. Four swipeable pages in a paging ScrollWidget: high scores,
    /// the player's best scores, all-time stats and current adventure stats.
    /// </summary>
    public class AdventureScreen : Widget, ButtonListener, INavHandler
    {
        // 0x78: ButtonListener sub-object

        public ThunderballApp mApp;                               // 0x7c

        public PlayerInfo mPlayerInfo;                            // 0x80

        public List<HighScoreEntry> mScores;                      // 0x84

        public AdventureStatsWidget mStatsWidget;                 // 0x88

        public ScrollWidget mScrollWidget;                        // 0x8c

        public ThunderButton mBackButton;                         // 0x90

#if PEGGLE_IPHONE
        /// <summary>iPhone: the trophy room is reached from here (its main menu has no Trophies button).</summary>
        public ThunderButton mTrophiesButton;                     // 0x94 (iPhone)
#endif

        public bool mFromBoard;                                   // 0x98

        public AdventureScreen(ThunderballApp theApp, bool fromBoard)
        {
            mFromBoard = fromBoard;
            mApp = theApp;
            mPlayerInfo = theApp.mPlayerInfo;
            mWidth = theApp.mWidth;
            mHeight = theApp.mHeight;
#if PEGGLE_IPHONE
            mTrophiesButton = SexyUtil.MakeButton(0, this, SexyAppBase.Loc("Trophies"), null);
#endif
            mBackButton = SexyUtil.MakeButton(1, this, SexyAppBase.Loc("Back"), null);
            mScores = mApp.mHighScoreMgr.GetScores("_adventure", false, -1);
#if PEGGLE_IPHONE
            AddWidget(mTrophiesButton);
            AddWidget(mBackButton);
            mBackButton.Resize(ADVERNTURE_SCREEN_MENU_BUTTON_X + SexyUtil.gGutterWidth, ADVERNTURE_SCREEN_MENU_BUTTON_Y,
                ADVERNTURE_SCREEN_MENU_BUTTON_WIDTH, mBackButton.mHeight);
            mTrophiesButton.Resize(ADVERNTURE_SCREEN_CONT_BUTTON_X + SexyUtil.gGutterWidth, ADVERNTURE_SCREEN_MENU_BUTTON_Y,
                ADVERNTURE_SCREEN_MENU_BUTTON_WIDTH, mBackButton.mHeight);
#else
            AddWidget(mBackButton);
            mBackButton.Resize((SCREEN_WIDTH - ADVERNTURE_SCREEN_MENU_BUTTON_WIDTH) / 2, ADVERNTURE_SCREEN_MENU_BUTTON_Y,
                ADVERNTURE_SCREEN_MENU_BUTTON_WIDTH, mBackButton.mHeight);
#endif
            mStatsWidget = new AdventureStatsWidget(theApp, fromBoard);
            mStatsWidget.Resize(0, 0, DIALOG_STATS_PAGE_WIDTH * 4, DIALOG_STATS_PAGE_HEIGHT);
            mScrollWidget = new ScrollWidget();
            mScrollWidget.Resize(new TRect(mWidth / 2 - DIALOG_STATS_PAGE_WIDTH / 2, ADVERNTURE_SCREEN_SCROLL_TOP_Y,
                DIALOG_STATS_PAGE_WIDTH, DIALOG_STATS_PAGE_HEIGHT));
            mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Horizontal);
            mScrollWidget.EnablePaging(true);
            mScrollWidget.AddWidget(mStatsWidget);
            AddWidget(mScrollWidget);
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        public override void Draw(Graphics g)
        {
            TRect aRect = new TRect(0, 0, mWidth, mHeight);
#if PEGGLE_IPHONE
            // Port addition (ultrawide): the fill and frame reach the window's edges.
            TRect aWideRect = ThunderDialog.GetWideRect(mWidth, aRect);
            Graphics aWideG = ThunderDialog.CreateWideGraphics(g, mWidth);
            SexyUtil.DrawDialogFill(aWideG, aWideRect, 0x120952, 0x170b64);
            aWideG.DrawImageBox(aWideRect, Res.IMAGE_DLG_DIALOGBOX);
#else
            SexyUtil.DrawDialogFill(g, aRect, 0x120952, 0x170b64);
            g.DrawImageBox(aRect, Res.IMAGE_DLG_DIALOGBOX);
#endif
            string aTitle = CFormat.StrFormat(SexyAppBase.Loc("%s's Stats"), mPlayerInfo.mName);
            g.SetColor(Color.White);
            g.SetFont(SexyUtil.GetFontById(0xb));
            WriteCenteredLine(g, DIALOG_HEADER_FONT_OFFSET_Y, aTitle);

            aRect = mScrollWidget.GetRect();
            aRect = aRect.Inflate(ADVERNTURE_TEXTRECT_INFLATE_OFFSET, ADVERNTURE_TEXTRECT_INFLATE_OFFSET);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0, 0x7f));
            SexyUtil.MyDrawImageBox(g, aRect, Res.IMAGE_DLG_TEXTRECT);

            // Page dots: the current page is yellow.
            int aBulletY = aRect.mY + aRect.mHeight + ADVERNTURE_SCREEN_BULLET_OFFSETY;
            int aBulletX = mWidth / 2 - (Res.IMAGE_DLG_BULLET.mWidth * 7) / 2;
            int aPage = Math.Abs(mStatsWidget.mX - DIALOG_STATS_PAGE_WIDTH / 2) / DIALOG_STATS_PAGE_WIDTH;
            Color aCurColor = new Color(0xff, 0xff, 0);
            Color anOtherColor = new Color(0x7f, 0x7f, 0xff);
            for (int i = 0; i < 4; i++)
            {
                g.SetColor(i == aPage ? aCurColor : anOtherColor);
                g.DrawImage(Res.IMAGE_DLG_BULLET, aBulletX, aBulletY);
                aBulletX += Res.IMAGE_DLG_BULLET.mWidth * 2;
            }
            g.SetColorizeImages(false);
            aRect = aRect.Inflate(ADVERNTURE_BLUEFRAME_INFLATE_OFFSET, ADVERNTURE_BLUEFRAME_INFLATE_OFFSET);
            g.DrawImageBox(aRect, Res.IMAGE_DLG_BLUEFRAME);
        }

        public void ButtonPress(int theId)
        {
        }

        public void ButtonPress(int theId, int theClickCount)
        {
        }

        public void ButtonDepress(int theId)
        {
            if (theId == 1)
            {
                mApp.CheckScrollOff(mApp.mAdventureScreen, 2, false);
                mApp.ShowGameSelectDialog(2);
            }
#if PEGGLE_IPHONE
            else if (theId == 0)
            {
                // (mFromBoard holds the constructor's flag: true for the Nights stats.)
                if (!mFromBoard)
                {
                    mApp.ShowTrophyRoom(0);
                }
                else
                {
                    mApp.ShowProgressScreen(0);
                }
            }
#endif
        }

        public void ButtonDownTick(int theId)
        {
        }

        public void ButtonMouseEnter(int theId)
        {
        }

        public void ButtonMouseLeave(int theId)
        {
        }

        public void ButtonMouseMove(int theId, int theX, int theY)
        {
        }

        public override void KeyChar(SexyChar theChar)
        {
        }

        // ---------------------------------------------------------------- port addition: menu navigation

        /// <summary>Port addition (menu navigation): Back is selected first.</summary>
        NavTarget INavHandler.NavDefault()
        {
            return new NavTarget(mBackButton);
        }

        /// <summary>
        /// Port addition (menu navigation): left and right turn the stats pages; up and down go between the buttons
        /// (the iPhone has Trophies beside Back); B is Back.
        /// </summary>
        bool INavHandler.NavAction(NavAction theAction)
        {
            switch (theAction)
            {
                case NavAction.Left:
                case NavAction.Right:
                case NavAction.PageLeft:
                case NavAction.PageRight:
                {
                    int aPage = (int)Math.Round(-mStatsWidget.mX / (double)DIALOG_STATS_PAGE_WIDTH);
                    bool aLeft = theAction == NavAction.Left || theAction == NavAction.PageLeft;
                    int aNew = Math.Clamp(aPage + (aLeft ? -1 : 1), 0, 3);
                    if (aNew != aPage)
                    {
                        mApp.PlaySample(Res.SOUND_BUTTON1);
                        mScrollWidget.ScrollRectIntoView(new TRect(aNew * DIALOG_STATS_PAGE_WIDTH, 0, DIALOG_STATS_PAGE_WIDTH, DIALOG_STATS_PAGE_HEIGHT), true);
                    }
                    return true;
                }
                case NavAction.Up:
                case NavAction.Down:
                {
                    List<NavTarget> aButtons = MenuNav.GetTargets(this);
                    if (aButtons.Count > 1)
                    {
                        int anIndex = aButtons.IndexOf(MenuNav.Selected);
                        MenuNav.Select(aButtons[(anIndex + 1) % aButtons.Count]);
                    }
                    return true;
                }
                case NavAction.Back:
                    MenuNav.Press(mBackButton);
                    return true;
            }
            return false;
        }
    }

    /// <summary>The four stats pages side by side, scrolled by AdventureScreen (Sexy::AdventureStatsWidget).</summary>
    public class AdventureStatsWidget : Widget
    {
        public ThunderballApp mApp;                               // 0x78

        public PlayerInfo mPlayerInfo;                            // 0x7c

        public List<HighScoreEntry> mScores;                      // 0x80

        public bool mFromBoard;                                   // 0x84

        public int mTitleHeight;                                  // 0x88

        public AdventureStatsWidget(ThunderballApp theApp, bool fromBoard)
        {
            mFromBoard = fromBoard;
            mApp = theApp;
            mPlayerInfo = theApp.mPlayerInfo;
            mScores = mApp.mHighScoreMgr.GetScores("_adventure", false, -1);
            mTitleHeight = Res.FONT_OVERLOAD16OUTLINE.GetHeight();
        }

        public void DrawStatsPageTitle(Graphics g, int theX, int theY, int theWidth, int theHeight, string theTitle)
        {
            g.SetFont(SexyUtil.GetFontById(8));
            g.SetColor(SexyUtil.MakeColor(0xffd305));
            g.DrawString(theTitle, theX + (theWidth - g.GetFont().StringWidth(theTitle)) / 2, mTitleHeight + theY + mY);
        }

        private static bool PageVisible(Graphics g, int theX, int theY)
        {
            TRect aRect = new TRect((int)((float)theX + g.mTransX), (int)((float)theY + g.mTransY), DIALOG_STATS_PAGE_WIDTH, DIALOG_STATS_PAGE_HEIGHT);
            return aRect.Intersects(g.mClipRect);
        }

        /// <summary>The first text baseline, centring <paramref name="theLines"/> lines under the title.</summary>
        private int GetFirstLineY(Graphics g, int theY, int theLines, out int theLineSpacing)
        {
            Font aFont = g.GetFont();
            int aSpacing = aFont.GetLineSpacing();
            theLineSpacing = aSpacing;
            int aTop = theY + mY + mTitleHeight;
            int anOffset = (mHeight - (aTop + aSpacing * theLines)) / 2 - (aSpacing - aFont.GetAscent());
            return aFont.GetHeight() + theY + mY + mTitleHeight + anOffset;
        }

        private static Color RankColor(int theRank)
        {
            return SexyUtil.MakeColor(GlobalStaticVars.gSexyAppBase.HSLToRGB((theRank * 0xf5 + 0x46) % 0x100, 0xff, 0x80));
        }

        public void DrawHighScores(Graphics g, int theX, int theY)
        {
            if (!PageVisible(g, theX, theY))
            {
                return;
            }
            DrawStatsPageTitle(g, theX, theY, DIALOG_STATS_PAGE_WIDTH, DIALOG_STATS_PAGE_HEIGHT, SexyAppBase.Loc("High Scores"));
            g.SetFont(Res.FONT_OVERLOAD10OUTLINE);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            int y = GetFirstLineY(g, theY, 10, out int aSpacing);
            int aRank = 1;
            int aRankX = theX + ADVERNTURE_SCREEN_HS_COL1_OFFSET_X;
            int aNameX = aRankX + ADVERNTURE_SCREEN_HS_COL2_OFFSET_X;
            int aScoreRight = aNameX + ADVERNTURE_SCREEN_HS_COL3_OFFSET_X;
            foreach (HighScoreEntry anEntry in mScores)
            {
                if (aRank > 10)
                {
                    break;
                }
                g.SetColor(RankColor(aRank));
                g.SetFont(Res.FONT_OVERLOAD10OUTLINE);
                g.DrawString(CFormat.StrFormat(SexyAppBase.Loc("%d."), aRank), aRankX, y);
                g.DrawString(SexyUtil.GetLocalizedPlayerName(anEntry.mName), aNameX, y);
                string aScore = SexyUtil.CommaSeperate(anEntry.mScore);
                g.SetFont(Res.FONT_OVERLOADMONO12);
                g.DrawString(aScore, aScoreRight - g.GetFont().StringWidth(aScore), y);
                y += aSpacing;
                aRank++;
            }
            for (; aRank < 0xb; aRank++)
            {
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                g.DrawString(CFormat.StrFormat(SexyAppBase.Loc("%d."), aRank), aRankX, y);
                y += aSpacing;
            }
        }

        public void DrawPersonalHighScores(Graphics g, int theX, int theY)
        {
            if (!PageVisible(g, theX, theY))
            {
                return;
            }
            DrawStatsPageTitle(g, theX, theY, DIALOG_STATS_PAGE_WIDTH, DIALOG_STATS_PAGE_HEIGHT, SexyAppBase.Loc("Your Best Scores"));
            g.SetFont(Res.FONT_OVERLOAD10OUTLINE);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            int y = GetFirstLineY(g, theY, 10, out int aSpacing);
            int aRank = 1;
            int aRankX = theX + ADVERNTURE_SCREEN_HS_COL1_OFFSET_X;
            int aNameX = aRankX + ADVERNTURE_SCREEN_HS_COL2_OFFSET_X;
            int aScoreRight = aNameX + ADVERNTURE_SCREEN_HS_COL3_OFFSET_X;
            // The multiset is ascending; walk it from the top.
            List<int> aScores = mPlayerInfo.mAdventureHighScores;
            int anIndex = aScores.Count - 1;
            while (anIndex >= 0 || aRank <= 10)
            {
                int aValue = -1;
                if (anIndex >= 0)
                {
                    aValue = aScores[anIndex--];
                }
                g.SetColor(RankColor(aRank));
                g.SetFont(Res.FONT_OVERLOAD10OUTLINE);
                g.DrawString(CFormat.StrFormat(SexyAppBase.Loc("%d."), aRank), aRankX, y);
                string aScore = SexyUtil.CommaSeperate(aValue);
                if (aValue > -1)
                {
                    g.DrawString(SexyUtil.GetLocalizedPlayerName(mPlayerInfo.mName), aNameX, y);
                    g.SetFont(Res.FONT_OVERLOADMONO12);
                    g.DrawString(aScore, aScoreRight - g.GetFont().StringWidth(aScore), y);
                }
                y += aSpacing;
                aRank++;
            }
        }

        public void DrawStats(Graphics g, int theX, int theY, bool adventure)
        {
            if (!PageVisible(g, theX, theY))
            {
                return;
            }
            string aTitle = adventure ? SexyAppBase.Loc("Your Current Adventure Stats") : SexyAppBase.Loc("Your All-Time Stats");
            DrawStatsPageTitle(g, theX, theY, DIALOG_STATS_PAGE_WIDTH, DIALOG_STATS_PAGE_HEIGHT, aTitle);
            g.SetFont(Res.FONT_OVERLOAD10OUTLINE);
            int y = GetFirstLineY(g, theY, adventure ? 7 : 8, out int aSpacing);
            GameStats aStats = adventure ? mPlayerInfo.GetmCurAdventureStats() : mPlayerInfo.mTotalStats;
            int aLeft = theX + 10;
            int aRight = theX + DIALOG_STATS_PAGE_WIDTH + -10;
            DoDrawStat(g, SexyAppBase.Loc("Shots:"), aStats.mShots, aLeft, aRight, y, 0);
            y += aSpacing;
            DoDrawStat(g, SexyAppBase.Loc("Free Balls:"), aStats.GetTotalFreeBalls(), aLeft, aRight, y, 0);
            y += aSpacing;
            DoDrawStat(g, SexyAppBase.Loc("Style Shots:"), aStats.GetTotalStyleShots(), aLeft, aRight, y, 0);
            y += aSpacing;
            DoDrawStat(g, SexyAppBase.Loc("Best Shot:"), aStats.mBestShot, aLeft, aRight, y, 0);
            y += aSpacing;
            DoDrawStat(g, SexyAppBase.Loc("Best Fever:"), aStats.mBestValue, aLeft, aRight, y, 0);
            y += aSpacing;
            if (!adventure)
            {
                DoDrawStat(g, SexyAppBase.Loc("Challenges Finished:"), mPlayerInfo.mPeggleTrophySet.Count + mPlayerInfo.mNightsTrophySet.Count,
                    aLeft, aRight, y, 100);
                y += aSpacing;
                DoDrawStat(g, SexyAppBase.Loc("Levels 100% Cleared:"), mPlayerInfo.mPeggleClearedLevelSet.Count + mPlayerInfo.mNightsClearedLevelSet.Count,
                    aLeft, aRight, y, 0x73);
                y += aSpacing;
                DoDrawStat(g, SexyAppBase.Loc("Levels Aced:"), mPlayerInfo.mPeggleAcedLevelSet.Count + mPlayerInfo.mNightsAcedLevelSet.Count,
                    aLeft, aRight, y, 0x73);
            }
            else
            {
                DoDrawStat(g, SexyAppBase.Loc("Total Score:"), mPlayerInfo.GetmAdventureScore(), aLeft, aRight, y, 0);
                y += aSpacing;
                DoDrawStat(g, SexyAppBase.Loc("Retries:"), mPlayerInfo.GetmAdventureNumRetries(), aLeft, aRight, y, 0);
            }
        }

        public override void Draw(Graphics g)
        {
            DrawHighScores(g, 0, 0);
            DrawPersonalHighScores(g, DIALOG_STATS_PAGE_WIDTH, 0);
            DrawStats(g, DIALOG_STATS_PAGE_WIDTH << 1, 0, false);
            DrawStats(g, DIALOG_STATS_PAGE_WIDTH * 3, 0, true);
        }

        /// <summary>DoDrawStat(Graphics*, wchar_t const*, int, int, int, int, int): "label ...... value[ / total]".</summary>
        public static void DoDrawStat(Graphics g, string theLabel, int theValue, int theLeft, int theRight, int theY, int theTotal)
        {
            string aValue = SexyUtil.CommaSeperate(theValue);
            if (theTotal > 0)
            {
                aValue += CFormat.StrFormat(SexyAppBase.Loc(" / %d"), theTotal);
            }
            DoDrawStat(g, theLabel, aValue, theLeft, theRight, theY);
        }

        public static void DoDrawStat(Graphics g, string theLabel, string theValue, int theLeft, int theRight, int theY)
        {
            g.SetColor(SexyUtil.MakeColor(0xffff00));
            g.DrawString(theLabel, theLeft, theY);
            g.SetColor(SexyUtil.MakeColor(0x88ff88));
            g.DrawString(theValue, theRight - g.GetFont().StringWidth(theValue), theY);
        }
    }
}
