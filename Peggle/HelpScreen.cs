using System;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The How to Play screen (Sexy::HelpScreen, id 42): a full-screen dialog with a page title,
    /// the scrolling HelpWidget, Previous/Next arrow buttons and OK.
    /// </summary>
    public class HelpScreen : ThunderDialog
    {
        public ThunderballApp mApp;                               // 0x128

        public bool mFromBoard;                                   // 0x12c

        public HelpWidget mHelpWidget;                            // 0x130

        public ThunderScrollWidget mScrollWidget;                 // 0x134

        public ThunderButton mNextButton;                         // 0x138

        public ThunderButton mPrevButton;                         // 0x13c

        private static int SCROLL_INSET = -1;

        public HelpScreen(ThunderballApp theApp, bool fromBoard)
            : base(DialogId.DIALOG_HELP, true, SexyAppBase.Loc("How to Play"), "", SexyAppBase.Loc(""), 3)
        {
            mApp = theApp;
            mFromBoard = fromBoard;
            mAutoSizeYesButton = true;
            mYesButton.mLabel = SexyAppBase.Loc("OK");
            mNextButton = SexyUtil.MakeButton(2, this, SexyAppBase.Loc("Next"), Res.IMAGE_DLG_RIGHT_ARROW_BUTTON);
            AddWidget(mNextButton);
            mPrevButton = SexyUtil.MakeButton(1, this, SexyAppBase.Loc("Previous"), Res.IMAGE_DLG_LEFT_ARROW_BUTTON);
            AddWidget(mPrevButton);
            mHelpWidget = new HelpWidget(theApp);
            mScrollWidget = new ThunderScrollWidget();
            mScrollWidget.mShadowColor = 0x181436;
            mScrollWidget.AddWidget(mHelpWidget);
            AddWidget(mScrollWidget);
            mHelpWidget.SetCurPage(1);
            UpdateButtons();
        }

        public override void Dispose()
        {
            mApp.mResourceManager.DeleteResources("Help");
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        public void UpdateButtons()
        {
            mNextButton.mVisible = mHelpWidget.mCurPage != 13;
            mPrevButton.mVisible = mHelpWidget.mCurPage != 1;
        }

        public override void Draw(Graphics g)
        {
#if PEGGLE_IPHONE
            // A plain full-size dialog box (no ThunderDialog header); "How to Play" is drawn below.
            // Port addition (ultrawide): the fill and frame reach the window's edges.
            TRect aBox = ThunderDialog.GetWideRect(mWidth, new TRect(0, 0, mWidth, mHeight));
            Graphics aWideG = ThunderDialog.CreateWideGraphics(g, mWidth);
            SexyUtil.DrawDialogFill(aWideG, aBox, 0x292073, 0x31297e);
            aWideG.DrawImageBox(aBox, Res.IMAGE_DLG_DIALOGBOX);
#else
            base.Draw(g);
#endif
            TRect aRect = mScrollWidget.GetRect();
            aRect.Inflate(0, HELPSCREEN_SCROLL_INFLATE_OFFSET_Y);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0, 0x7f));
            SexyUtil.MyDrawImageBox(g, aRect, Res.IMAGE_DLG_TEXTRECT);
            g.SetColorizeImages(false);
            string aTitle = SexyAppBase.Loc("Instructions");
            switch (mHelpWidget.mCurPage)
            {
            case 1: aTitle = SexyAppBase.Loc("Controls"); break;
            case 2: aTitle = SexyAppBase.Loc("Pegs"); break;
            case 3: aTitle = SexyAppBase.Loc("Scoring"); break;
            case 4: aTitle = SexyAppBase.Loc("Free Balls"); break;
            case 5: aTitle = SexyAppBase.Loc("Game Modes"); break;
            case 6: aTitle = SexyAppBase.Loc("Duel Mode"); break;
            case 7: aTitle = SexyAppBase.Loc("Instant Replays"); break;
            case 8: aTitle = SexyAppBase.Loc("Extra Tidbits"); break;
            case 9: aTitle = SexyAppBase.Loc("Style Shots"); break;
            case 10: aTitle = SexyAppBase.Loc("More Style Shots"); break;
            case 11: aTitle = SexyAppBase.Loc("Still More Style Shots"); break;
            case 12: aTitle = SexyAppBase.Loc("Master-Specific Style Shots"); break;
            case 13: aTitle = SexyAppBase.Loc("Even More Master-Specific Style Shots"); break;
            }
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.SetFont(Res.FONT_OVERLOAD24GRAD1);
#if PEGGLE_IPHONE
            WriteCenteredLine(g, g.GetFont().GetHeight() - g.GetFont().GetDecorationOffset(), SexyAppBase.Loc("How to Play"));
#endif
            g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
            WriteCenteredLine(g, HELPSCREEN_TITLE_Y, aTitle);
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            if (SCROLL_INSET == -1)
            {
                SCROLL_INSET = HELPSCREEN_SCROLL_INSET_W;
            }
            mHelpWidget.Resize(0, 0, theWidth - HELPSCREEN_SCROLL_INSET_W, GetScrollHeight(mHelpWidget.mCurPage));
            mScrollWidget.Resize(SCROLL_INSET, HELPSCREEN_SCROLL_Y, mWidth - SCROLL_INSET * 2, HELPSCREEN_SCROLL_H);
            // The original adds HELPSCREEN_BTN_Y to the OK button's y on every Resize.
            mYesButton.mY = HELPSCREEN_BTN_Y + mYesButton.mY;
            mYesButton.Resize((mWidth - mYesButton.mWidth) / 2, mYesButton.mY, mYesButton.mWidth, mYesButton.mHeight);
#if PEGGLE_IPHONE
            mNextButton.Resize(HELPSCREEN_NEXT_BTN_X + SexyUtil.gGutterWidth, mYesButton.mY, HELPSCREEN_BTN_W, mNextButton.mHeight);
            mPrevButton.Resize(HELPSCREEN_PREV_BTN_X + SexyUtil.gGutterWidth, mYesButton.mY, HELPSCREEN_BTN_W, mPrevButton.mHeight);
#else
            mNextButton.Resize(HELPSCREEN_NEXT_BTN_X, HELPSCREEN_NEXT_BTN_Y, HELPSCREEN_BTN_W, mNextButton.mHeight);
            mPrevButton.Resize(HELPSCREEN_PREV_BTN_X, HELPSCREEN_NEXT_BTN_Y, HELPSCREEN_BTN_W, mPrevButton.mHeight);
#endif
        }

        public int GetScrollHeight(int thePage)
        {
            int anIconsHeight;
            if (thePage < 9)
            {
                anIconsHeight = (HELPSCREEN_ICON_SPACING_W + HELPSCREEN_ICON_H) * (thePage == 7 ? 3 : 4);
            }
            else
            {
                anIconsHeight = (HELPSCREEN_ICON_STYLSHOT_EXTRA_H + HELPSCREEN_ICON_SPACING_W + HELPSCREEN_ICON_H) * 4;
            }
            return HelpWidget.GetSummaryHeight(thePage) + anIconsHeight;
        }

        public override void ButtonDepress(int theId)
        {
            if (theId == 2 || theId == 1)
            {
                int aPage = mHelpWidget.mCurPage + (theId == 2 ? 1 : -1);
                mHelpWidget.Resize(0, 0, mApp.mWidth - HELPSCREEN_SCROLL_INSET_W, GetScrollHeight(aPage));
                mScrollWidget.ClientSizeChanged();
                mHelpWidget.SetCurPage(aPage);
                UpdateButtons();
                mScrollWidget.ScrollToMin(false);
            }
            else
            {
                mApp.EndHelpScreen();
            }
        }
    }

    /// <summary>
    /// The contents of one How to Play page (Sexy::HelpWidget): a summary line, then four (three
    /// on the replay page) framed icons from IMAGE_HELP_ICONS with their explanations; the Game
    /// Modes page shows pre-rendered mode buttons instead, and pages 9-13 list style shots.
    /// </summary>
    public class HelpWidget : Widget
    {
        public int mCurPage;                                      // 0x78

        public ThunderballApp mApp;                               // 0x7c

        public MemoryImage[] mModeButtons = new MemoryImage[4];   // 0x80 (Game Modes page icons)

        public HelpWidget(ThunderballApp theApp)
        {
            mApp = theApp;
            mCurPage = -1;
            string[] aLabels =
            {
                SexyAppBase.Loc("ADVENTURE"), SexyAppBase.Loc("QUICK PLAY"), SexyAppBase.Loc("DUEL"), SexyAppBase.Loc("CHALLENGE")
            };
            for (int i = 0; i < 4; i++)
            {
                Image aButton = mApp.mNightsMode ? Res.IMAGE_MM_NIGHTS_BUTTON_PURPLE : Res.IMAGE_MM_BUTTON_BLUE;
                mModeButtons[i] = new MemoryImage();
                mModeButtons[i].Create(aButton.mWidth, aButton.mHeight / 2);
                mModeButtons[i].Clear();
                bool inFrame = Graphics.IsSpriteBatchBegun();
                Graphics g = Graphics.GetNew(mModeButtons[i]);
                g.SetFont(Res.FONT_PLAINOUTLINE);
                if (!mApp.mNightsMode)
                {
                    g.SetFont(Res.FONT_OVERLOAD24GRAD_BLUE);
                    g.SetColor(SexyUtil.MakeColor(0x1e8ff));
                }
                else
                {
                    g.SetFont(Res.FONT_OVERLOAD24GRAD_PURPLE);
                    g.SetColor(SexyUtil.MakeColor(0xffddf6));
                }
                Image aPurple = Res.IMAGE_MM_NIGHTS_BUTTON_PURPLE;
#if PEGGLE_IPHONE
                TRect aTextRect = new TRect(0, HELPSCREEN_BUTTON_TEXT_RECT_Y, (int)(aPurple.mWidth * 1.25), (int)(aPurple.mHeight * 0.5 * 1.58));
#else
                TRect aTextRect = new TRect(0, 0x11, (int)(aPurple.mWidth * 1.25), (int)(aPurple.mHeight * 0.5 * 1.25));
#endif
                g.DrawImage(aButton, 0, 0);
                g.SetScale(0.8f, 1.0f, 0.0f, 0.0f);
                if (aLabels[i].Length < 13)
                {
#if PEGGLE_IPHONE
                    g.DrawString(aLabels[i], (int)((double)(aTextRect.mWidth / 2) + (double)g.GetFont().StringWidth(aLabels[i]) * -0.5), HELPSCREEN_BUTTON_TEXT_Y_OFFSET);
#else
                    g.DrawString(aLabels[i], (int)((double)(aTextRect.mWidth / 2) + (double)g.GetFont().StringWidth(aLabels[i]) * -0.5), 0x5a);
#endif
                }
                else
                {
                    WriteWordWrapped(g, aTextRect, aLabels[i], -1, 0);
                }
                g.EndOffscreen(inFrame);
            }
        }

        public override void Dispose()
        {
            for (int i = 0; i < 4; i++)
            {
                mModeButtons[i]?.Dispose();
                mModeButtons[i] = null;
            }
            base.Dispose();
        }

        public void SetCurPage(int thePage)
        {
            mCurPage = Math.Clamp(thePage, 1, 13);
        }

        public static string GetSummaryText(int thePage)
        {
            switch (thePage)
            {
            case 1: return SexyAppBase.Loc("Touch where you want the ball to go. The Launcher will automatically aim so that the ball heads to that point.");
            case 2: return SexyAppBase.Loc("There are four types of pegs in Peggle.");
            case 3: return SexyAppBase.Loc("The number of points you get for a shot is the total value of all of the pegs multiplied by the number of pegs you hit.");
            case 4: return SexyAppBase.Loc("There are a few different ways to get a Free Ball.");
            case 5: return SexyAppBase.Loc("There are four game modes in Peggle.");
            case 6: return SexyAppBase.Loc("Duel Mode has slightly different rules than the single player modes.");
            case 7: return SexyAppBase.Loc("Instant replays are useful for reliving your favorite shots and showing them to your friends and family over and over again.");
            case 8: return SexyAppBase.Loc("What else, what else, what else?");
            case 9:
            case 10:
            case 11: return SexyAppBase.Loc("Certain kinds of shots earn special recognition and bonus points! The second score listed is specific to duel mode.");
            case 12:
            case 13: return SexyAppBase.Loc("All Masters have a Style Shot awarded for hitting both green pegs in the same shot. Some Masters have additional Style Shots specific to their powers.");
            }
            return "";
        }

        /// <summary>Height of the word-wrapped summary text plus one line: where the icon rows start.</summary>
        public static int GetSummaryHeight(int thePage)
        {
            Graphics g = Graphics.GetNew();
            g.SetFont(Res.FONT_PLAINOUTLINE);
            int aMaxWidth = 0;
            int aHeight = g.GetWordWrappedHeight(DIALOG_HELP_SUMMARY_RECT_WIDTH, GetSummaryText(thePage), -1, ref aMaxWidth, -1);
            g.PrepareForReuse();
            return aHeight + Res.FONT_PLAINOUTLINE.GetLineSpacing();
        }

        public override void Draw(Graphics g)
        {
            switch (mCurPage)
            {
            case 1: DrawPage1(g); break;
            case 2: DrawPage2(g); break;
            case 3: DrawPage3(g); break;
            case 4: DrawPage4(g); break;
            case 5: DrawPage5(g); break;
            case 6: DrawPage6(g); break;
            case 7: DrawPage7(g); break;
            case 8: DrawPage8(g); break;
            case 9: DrawPage9(g); break;
            case 10: DrawPage10(g); break;
            case 11: DrawPage11(g); break;
            case 12: DrawPage12(g); break;
            case 13: DrawPage13(g); break;
            }
            int aRowStep = (mCurPage < 9 ? 0 : HELPSCREEN_ICON_STYLSHOT_EXTRA_H) + HELPSCREEN_ICON_SPACING_W + HELPSCREEN_ICON_H;
            if (mCurPage == 5)
            {
                TRect aCel = Res.IMAGE_HELP_ICONS.GetCelRect(0, 0);
                Image aPurple = Res.IMAGE_MM_NIGHTS_BUTTON_PURPLE;
                for (int i = 0; i < 4; i++)
                {
                    int x = HELPSCREEN_SUMMARY_ICON_GAP_X;
                    int y = i * aRowStep + GetSummaryHeight(mCurPage);
                    TRect aSrc = new TRect(0, 0, aPurple.mWidth, aPurple.mHeight / 2);
                    TRect aDest = new TRect(x + aCel.mWidth / 2 - HELPSCREEN_SCALE_W / 2, y + aCel.mHeight / 2 - HELPSCREEN_SCALE_H / 2,
                        HELPSCREEN_SCALE_W, HELPSCREEN_SCALE_H);
                    g.SetColor(SexyUtil.MakeColor(0));
                    g.FillRect(x, y, aCel.mWidth, aCel.mHeight);
                    g.DrawImage(mModeButtons[i], aDest, aSrc);
                    g.DrawImageBox(new TRect(x - HELPSCREEN_FRAME_W, y - HELPSCREEN_FRAME_W,
                        aCel.mWidth + HELPSCREEN_FRAME_DOUBLE_W, aCel.mHeight + HELPSCREEN_FRAME_DOUBLE_W), Res.IMAGE_DLG_BLUEFRAME);
                }
                return;
            }
            // Icon columns skip the Game Modes page.
            int aColumn = mCurPage - 1;
            if (aColumn > 4)
            {
                aColumn = mCurPage - 2;
            }
            int aCount = mCurPage == 7 ? 3 : 4;
            for (int i = 0; i < aCount; i++)
            {
                int x = HELPSCREEN_SUMMARY_ICON_GAP_X;
                g.SetFont(Res.FONT_PLAINOUTLINE);
                int y = i * aRowStep + GetSummaryHeight(mCurPage);
#if !PEGGLE_IPHONE
                if (mCurPage == 7 && i == 1)
                {
                    // The replay page has no second icon: the loop jumps to the third, keeping the second row's y.
                    i = 2;
                }
#endif
                TRect aCel = Res.IMAGE_HELP_ICONS.GetCelRect(aColumn, i);
                g.DrawImage(Res.IMAGE_HELP_ICONS, x, y, aCel);
#if PEGGLE_IPHONE
                g.DrawImageBox(new TRect(x - HELPSCREEN_FRAME_W, y - HELPSCREEN_FRAME_W,
                    aCel.mWidth + HELPSCREEN_FRAME_DOUBLE_W, aCel.mHeight + HELPSCREEN_FRAME_DOUBLE_W), Res.IMAGE_DLG_BLUEFRAME);
#else
                g.DrawImageBox(new TRect(x - HELPSCREEN_FRAME_W, y - HELPSCREEN_FRAME_W + 5,
                    aCel.mWidth + HELPSCREEN_FRAME_DOUBLE_W, aCel.mHeight + HELPSCREEN_FRAME_DOUBLE_W), Res.IMAGE_DLG_BLUEFRAME);
#endif
            }
        }

        private static string L(string theText)
        {
            return SexyAppBase.Loc(theText);
        }

        public void DrawPage1(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(1));
            DrawPanelRect2(g, 0, 1, L("\nTouch the fire button to fire the ball. The ball will head right to where you last aimed."));
            DrawPanelRect2(g, 1, 1, L("Touch the fast forward button to speed up time. This is helpful when waiting for moving objects."));
            DrawPanelRect2(g, 2, 1, L("You can also aim using the new scroll wheel on the side of the screen."));
            DrawPanelRect2(g, 3, 1, L("Need more control?  Double-tap the screen to enable Peggle Zoom for precision aiming."));
        }

        public void DrawPage2(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(2));
            DrawPanelRect2(g, 0, 2, L("\nPegs can be both round and rectangular."));
            DrawPanelRect2(g, 1, 2, L("Orange pegs are worth 10x the value of blue pegs. Clear all of the orange pegs on a level to beat the level."));
            DrawPanelRect2(g, 2, 2, L("The purple peg is worth 50x the value of the blue pegs. This peg changes location on every shot."));
            DrawPanelRect2(g, 3, 2, L("The green peg activates your Magic Power. There are two green pegs per level."));
        }

        public void DrawPage3(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(3));
            DrawPanelRect2(g, 0, 3, L("Every peg that you hit on a shot adds to a subtotal score. The shot score is the subtotal score multiplied by the number of pegs hit."));
            DrawPanelRect2(g, 1, 3, L("The value of the pegs goes up as you eliminate orange pegs. The Fevermeter on the right shows the current score multiplier."));
            DrawPanelRect2(g, 2, 3, L("Certain cool shots give you extra style points. These points add directly to your score and don't factor into getting a 25,000 point Free Ball."));
            DrawPanelRect2(g, 3, 3, L("After beating a level you earn extra points from the bonus buckets. You also earn 10,000 points per extra ball left over."));
        }

        public void DrawPage4(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(4));
            DrawPanelRect2(g, 0, 4, L("You get a Free Ball if your ball lands in the Free Ball Bucket."));
            DrawPanelRect2(g, 1, 4, L("Scoring 25K, 75K, and 125K on a shot also gives you a Free Ball."));
            DrawPanelRect2(g, 2, 4, L("If you don't hit any pegs on a shot then you may be awarded a Free Ball by winning a coin flip!"));
            DrawPanelRect2(g, 3, 4, L("In Duel Mode, you get style points instead of Free Balls. Also, there are no coin flip Free Balls in Duel Mode."));
        }

        public void DrawPage5(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(5));
            DrawPanelRect2(g, 0, 5, L("Adventure Mode allows you to train with all the Peggle Masters. Progress through all the levels to become a Peggle Master yourself."));
            DrawPanelRect2(g, 1, 5, L("Quick Play Mode lets you select an individual level to play. You may only play levels that you've completed in Adventure Mode."));
            DrawPanelRect2(g, 2, 5, L("Duel Mode allows you to compete in a two player version of Peggle. You can play against the computer or another person."));
            DrawPanelRect2(g, 3, 5, L("Challenge Mode presents you with extra difficult Peggle feats.  Beat all of the challenges to become a Peggle Grand Master."));
        }

        public void DrawPage6(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(6));
            DrawPanelRect2(g, 0, 6, L("In Duel Mode, two players take turns shooting the ball. The player with the most points at the end of the game wins."));
            DrawPanelRect2(g, 1, 6, L("You must hit at least one orange peg on a shot. If you don't then you are penalized by losing 25% of your total score."));
            DrawPanelRect2(g, 2, 6, L("In Duel Mode, you get style points instead of Free Balls. Also, there are no coin flip Free Balls in Duel Mode."));
            DrawPanelRect2(g, 3, 6, L("The game ends either when a player eliminates the last orange peg or when both players run out of balls."));
        }

        public void DrawPage7(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(7));
            DrawPanelRect2(g, 0, 7, L("Touch the Camera icon on the bottom right of the screen to see a replay of the last shot."));
#if PEGGLE_IPHONE
            DrawPanelRect2(g, 1, 7, L("Hold down the fast forward button to activate fast forward during replays."));
#endif
            DrawPanelRect2(g, 2, 7, L("You can save replays and view them later!\nPress the Replays button on the main menu to view saved replays."));
        }

        public void DrawPage8(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(8));
            DrawPanelRect2(g, 0, 8, L("After beating Adventure Mode, you can replay it again using any Peggle Master on any level!  Can you top your high score?"));
            DrawPanelRect2(g, 1, 8, L("Clearing all of the pegs on a level gives you a bonus blue ribbon!"));
            DrawPanelRect2(g, 2, 8, L("Each level has an Ace Score. Beat that score for a bonus red ribbon! Get Ace Scores on all levels for an even fancier trophy!"));
            DrawPanelRect2(g, 3, 8, L("Left-handed and color blind modes are available in the options menu.  "));
        }

        public void DrawPage9(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(9));
            DrawStyleShotText(g, 0, 9, L("FREE BALL SKILLS"), L("5,000 / 2,500 points"), L("Bounce the ball directly from one (and only one) peg into the Free Ball Bucket."));
            DrawStyleShotText(g, 1, 9, L("LONG SHOT"), L("25,000 / 5,000 points"), L("Hit a non-blue peg first, travel 1/3 the width of the screen and hit another non-blue peg."));
            DrawStyleShotText(g, 2, 9, L("DOUBLE LONG SHOT"), L("25,000 / 10,000 points"), L("Get a LONG SHOT, then travel 1/3 the width of the screen and hit a third non-blue peg."));
            DrawStyleShotText(g, 3, 9, L("SUPER LONG SHOT"), L("50,000 / 10,000 points"), L("Hit a non-blue peg first, travel 2/3 the width of the screen and hit another non-blue peg."));
        }

        public void DrawPage10(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(10));
            DrawStyleShotText(g, 0, 10, L("EXTREME SLIDE"), L("50,000 / 5,000 points"), L("Slide above a certain speed for 12 pegs."));
            DrawStyleShotText(g, 1, 10, L("OFF THE WALL"), L("25,000 / 5,000 points"), L("Bounce the ball off the wall, travel 1/5 the width of the screen and hit a non-blue peg."));
            DrawStyleShotText(g, 2, 10, L("KICK THE BUCKET"), L("25,000 / 5,000 points"), L("Bounce the ball off the Free Ball Bucket to hit the final orange peg for Fever."));
            DrawStyleShotText(g, 3, 10, L("LUCKY BOUNCE"), L("25,000 / 2,500 points"), L("Bounce the ball off the Free Ball Bucket, then catch it again."));
        }

        public void DrawPage11(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(11));
            DrawStyleShotText(g, 0, 11, L("MAD SKILLZ"), L("25,000 / 5,000 points"), L("Have 5 balls in a row fall in the Free Ball Bucket."));
            DrawStyleShotText(g, 1, 11, L("CRAZY MAD SKILLZ"), L("100,000 /   N/A   points"), L("Have 10 balls in a row fall in the Free Ball Bucket."));
            DrawStyleShotText(g, 2, 11, L("ORANGE ATTACK"), L("50,000 / 5,000 points"), L("Hit a large percentage of the remaining orange pegs in one shot."));
            DrawStyleShotText(g, 3, 11, L("COOL CLEAR"), L("50,000 /   N/A   points"), L("Get Ultra Extreme Fever (100% pegs cleared) on a shot that hits at least two pegs."));
        }

        public void DrawPage12(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(12));
            DrawStyleShotText(g, 0, 12, L("MULTIBALL MADNESS"), L("25,000 / 5,000 points"), L("Get the Multiball after hitting the final orange peg for Fever."));
            DrawStyleShotText(g, 1, 12, L("FREE BALL FRENZY"), L("100,000 / 10,000 points"), L("Have more than one ball go into the Free Ball Bucket in one shot."));
            DrawStyleShotText(g, 2, 12, L("FLIPPER MANIAC"), L("25,000 / 5,000 points"), L("Bounce off a Flipper and hit at least one peg; do this four times."));
            DrawStyleShotText(g, 3, 12, L("SHOCK IT TO ME"), L("25,000 / 5,000 points"), L("Zap a dozen or more pegs with Electrobolt."));
        }

        public void DrawPage13(Graphics g)
        {
            DrawSummaryText(g, GetSummaryText(13));
            DrawStyleShotText(g, 0, 13, L("EYE OF THE PYRAMID"), L("10,000 / 5,000 points"), L("Get a ball into the Free Ball Bucket without hitting the Pyramid bumpers."));
            DrawStyleShotText(g, 1, 13, L("PYRAMID PICKUP"), L("10,000 / 5,000 points"), L("Bounce a ball off the Pyramid bumpers and into the Free Ball Bucket."));
            DrawStyleShotText(g, 2, 13, L("HAT TRICK"), L("10,000 / 2,500 points"), L("Have a ball wearing the Magic Hat go into the Free Ball Bucket."));
            DrawStyleShotText(g, 3, 13, L("HOT STUFF"), L("25,000 / 5,000 points"), L("Bounce a Fireball off the cap on the Free Ball Bucket, then have the Fireball land in the Free Ball Bucket."));
        }

        public static void DrawSummaryText(Graphics g, string theText)
        {
            TRect aRect = new TRect(HELPSCREEN_SUMMARY_TEXT_X, 0, DIALOG_HELP_SUMMARY_RECT_WIDTH, 0);
            g.SetFont(Res.FONT_PLAINOUTLINE);
            g.SetColor(SexyUtil.MakeColor(0xffffaa));
            g.WriteWordWrapped(aRect, theText, -1, 0);
        }

        public static void DrawStyleShotText(Graphics g, int theIndex, int thePage, string theName, string thePoints, string theText)
        {
            TRect aRect = GetPanelRect2(theIndex, thePage, true);
            g.SetFont(Res.FONT_PLAINOUTLINE);
            g.SetColor(SexyUtil.MakeColor(0xffffaa));
            int aHalfHeight = g.GetFont().GetHeight() / 2;
            g.DrawString(theName, aRect.mX, aHalfHeight + aRect.mY);
            g.DrawString(thePoints, aRect.mX + aRect.mWidth - g.GetFont().StringWidth(thePoints), aRect.mY + aHalfHeight);
            aRect.mY = HELPSCREEN_ICON_STYLSHOT_EXTRA_H + aHalfHeight + aRect.mY;
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.WriteWordWrapped(aRect, theText, -1, -1);
        }

        public static TRect GetPanelRect2(int theIndex, int thePage, bool isStyleShot)
        {
            int aHeight = HELPSCREEN_ICON_H + (isStyleShot ? HELPSCREEN_ICON_STYLSHOT_EXTRA_H : 0);
            return new TRect(HELPSCREEN_PANEL_RECT_X, (HELPSCREEN_ICON_SPACING_W + aHeight) * theIndex + GetSummaryHeight(thePage),
                HELPSCREEN_PANEL_RECT_W, aHeight);
        }

        public static void DrawPanelRect2(Graphics g, int theIndex, int thePage, string theText)
        {
            TRect aRect = GetPanelRect2(theIndex, thePage, false);
            g.SetFont(Res.FONT_PLAINOUTLINE);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.WriteWordWrapped(aRect, theText, -1, -1);
        }
    }
}
