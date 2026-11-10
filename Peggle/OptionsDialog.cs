using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Options dialog (Sexy::OptionsDialog). From the main menu it shows the volume sliders, Help,
    /// Demo, Colorblind/Left Handed and a Credits link; opened from the board it becomes the pause
    /// "MENU" with Main Menu and Restart/Next Level (or Restart/New Challenge) buttons.
    /// </summary>
    public class OptionsDialog : ThunderDialog, SliderListener, CheckboxListener, INavHandler
    {
        public const int OptionsDialog_MusicSlider = 0;

        public const int OptionsDialog_SfxSlider = 1;

        public const int OptionsDialog_Colorblind = 5;

        public const int OptionsDialog_Help = 6;

        public const int OptionsDialog_LeftHanded = 7;

        public const int OptionsDialog_Fullscreen = 15;

        public const int OptionsDialog_MainMenu = 8;

        public const int OptionsDialog_NextLevel = 9;

        public const int OptionsDialog_NewChallenge = 10;

        public const int OptionsDialog_Demo = 11;

        public const int OptionsDialog_Credits = 12;

        public const int OptionsDialog_Language = 13;

        public const int OptionsDialog_PortOptions = 14;

        private const int LAY_STACK_BELOW = (int)(LayoutFlags.LAY_SameLeft | LayoutFlags.LAY_Below | LayoutFlags.LAY_SameWidth | LayoutFlags.LAY_SameHeight);

        private const int LAY_FIRST_BELOW = (int)(LayoutFlags.LAY_Below | LayoutFlags.LAY_SetLeft | LayoutFlags.LAY_SetWidth);

        private const int LAY_BESIDE = (int)(LayoutFlags.LAY_SameTop | LayoutFlags.LAY_Right | LayoutFlags.LAY_SameWidth);

        public ThunderSlider mMusicSlider;                        // 0x130

        public ThunderSlider mSfxSlider;                          // 0x134

        public ThunderCheckbox mColorblindCheckbox;               // 0x138

        public ThunderCheckbox mLeftHandedCheckbox;               // 0x13c

        /// <summary>Port addition: with PC Controls, Fullscreen takes Left Handed's place (PC Controls set the handedness).</summary>
        public ThunderCheckbox mFullscreenCheckbox;

        public ThunderButton mHelpButton;                         // 0x140

        public ThunderButton mMainMenuButton;                     // 0x14c

        public ThunderButton mNextLevelButton;                    // 0x150

        public ThunderButton mNewChallengeButton;                 // 0x154

        public ThunderButton mDemoButton;                         // 0x158

        public ThunderButton mDoneButton;                         // 0x15c

        public HyperlinkWidget mCreditsLink;                      // 0x160

        /// <summary>Port addition: cycles the game language, styled like the Credits link.</summary>
        public HyperlinkWidget mLanguageLink;

        /// <summary>Port addition: opens the port's own options (main menu only).</summary>
        public ThunderButton mPortOptionsButton;

        public bool mFromBoard;                                   // 0x164

        public bool mMusicOn;                                     // 0x165

        public string mMusicLabel = "";                           // 0x168

        public string mMusicOffLabel = "";                        // 0x16c

        public string mSfxLabel = "";                             // 0x170

        public string mSfxOffLabel = "";                          // 0x174

        public ThunderballApp mApp;                               // 0x178

        public string mVersionString = "";                        // 0x17c

        public OptionsDialog(bool fromBoard)
            : base(DialogId.DIALOG_OPTIONS, true, fromBoard ? SexyAppBase.Loc("MENU") : SexyAppBase.Loc("OPTIONS"), "", SexyAppBase.Loc(""), 0)
        {
            ThunderballApp app = (ThunderballApp)GlobalStaticVars.gSexyAppBase;
            mCloseOnButton = true;
            mFromBoard = fromBoard;
            mMusicLabel = SexyAppBase.Loc("Music Volume");
            mMusicOffLabel = SexyAppBase.Loc("Music Volume (off)");
            mSfxLabel = SexyAppBase.Loc("Sound Effects");
            mSfxOffLabel = SexyAppBase.Loc("Sound Effects (off)");
            mApp = app;
            mMusicOn = mApp.mMusicEnabled;
            mMusicSlider = new ThunderSlider(mMusicOn ? mMusicLabel : mMusicOffLabel, OptionsDialog_MusicSlider, this);
            mMusicSlider.SetValue(mMusicOn ? mApp.GetMusicVolume() : 0.0);
            mSfxSlider = new ThunderSlider(GlobalStaticVars.gSexyAppBase.GetSfxVolume() > 0.0 ? mSfxLabel : mSfxOffLabel, OptionsDialog_SfxSlider, this);
            mSfxSlider.SetValue(GlobalStaticVars.gSexyAppBase.GetSfxVolume());
            mColorblindCheckbox = SexyUtil.MakeCheckbox2(OptionsDialog_Colorblind, this, SexyAppBase.Loc("Colorblind Mode"), 0);
            mHelpButton = SexyUtil.MakeButton(OptionsDialog_Help, this, SexyAppBase.Loc("Help"));
            mDemoButton = SexyUtil.MakeButton(OptionsDialog_Demo, this, SexyAppBase.Loc("Demo"));
            mPortOptionsButton = SexyUtil.MakeButton(OptionsDialog_PortOptions, this, SexyAppBase.Loc("Extra Options"));
            mLeftHandedCheckbox = SexyUtil.MakeCheckbox2(OptionsDialog_LeftHanded, this, SexyAppBase.Loc("Left Handed"), 0);
            // The original reads the player unconditionally here.
            mLeftHandedCheckbox.mChecked = app.mPlayerInfo != null && app.mPlayerInfo.mLeftHanded;
            mFullscreenCheckbox = SexyUtil.MakeCheckbox2(OptionsDialog_Fullscreen, this, SexyAppBase.Loc("Fullscreen"), 0);
            mFullscreenCheckbox.mChecked = Main.graphics.IsFullScreen;
            Image orange = Res.IMAGE_BUTTON_ORANGE;
            int w = orange.mWidth;
            int h = orange.mHeight;
            mMainMenuButton = MakeNavButton(orange, OptionsDialog_MainMenu, SexyAppBase.Loc("MAIN MENU"), w, h);
            mNextLevelButton = SexyUtil.MakeButton(OptionsDialog_NextLevel, this, SexyAppBase.Loc("New Level"));
            mNewChallengeButton = SexyUtil.MakeButton(OptionsDialog_NewChallenge, this, SexyAppBase.Loc("New Challenge"));
            // DONE uses the blue image but keeps the orange button's size, as in the original.
            mDoneButton = MakeNavButton(Res.IMAGE_MM_BUTTON_BLUE, 1000, SexyAppBase.Loc("DONE"), w, h);
            if (fromBoard && app.mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                mNextLevelButton.mLabel = SexyAppBase.Loc("Restart Challenge");
            }
            else if (app.mGameMode == GameMode.GAMEMODE_ADVENTURE || app.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                if (app.mBoard == null || !app.mBoard.mLevelBeaten)
                {
                    mNextLevelButton.mLabel = SexyAppBase.Loc("Restart Level");
                }
                else
                {
                    mNextLevelButton.mLabel = SexyAppBase.Loc("Next Level");
                }
            }
            if (app.mPlayerInfo != null)
            {
                mColorblindCheckbox.mChecked = app.mPlayerInfo.mColorblind;
            }
            mDemoButton.SetVisible(!fromBoard && app.mPlayerInfo != null);
            mPortOptionsButton.SetVisible(!fromBoard);
            mMainMenuButton.SetVisible(fromBoard);
            mNextLevelButton.SetVisible(fromBoard);
            mNewChallengeButton.SetVisible(fromBoard && app.mGameMode == GameMode.GAMEMODE_CHALLENGE);
            mHelpButton.SetVisible(true);
            // Port: PC Controls set the handedness (left-handed on HD, right-handed on iPhone), so Fullscreen
            // shows in the Left Handed checkbox's place then.
            mLeftHandedCheckbox.SetVisible(!app.mPCControls);
            mFullscreenCheckbox.SetVisible(app.mPCControls);
            if (fromBoard && (app.mGameMode == GameMode.GAMEMODE_REPLAY || app.mGameMode == GameMode.GAMEMODE_DEMO))
            {
                mNextLevelButton.SetVisible(false);
                mHelpButton.SetVisible(false);
            }
            mCreditsLink = new HyperlinkWidget(OptionsDialog_Credits, this);
            mCreditsLink.SetFont(Res.FONT_OVERLOAD12OUTLINE);
            mCreditsLink.mColor = SexyUtil.MakeColor(0xffff88);
            mCreditsLink.mOverColor = SexyUtil.MakeColor(0xff);
            mCreditsLink.mDoFinger = true;
            mCreditsLink.mLabel = SexyAppBase.Loc("Credits");
            mCreditsLink.mUnderlineSize = 1;
            mCreditsLink.mUnderlineOffset = 1;
            if (!mFromBoard)
            {
                mCreditsLink.SetVisible(true);
            }
            AddWidget(mCreditsLink);
            mLanguageLink = new HyperlinkWidget(OptionsDialog_Language, this);
            mLanguageLink.SetFont(Res.FONT_OVERLOAD12OUTLINE);
            mLanguageLink.mColor = mCreditsLink.mColor;
            mLanguageLink.mOverColor = mCreditsLink.mOverColor;
            mLanguageLink.mDoFinger = true;
            mLanguageLink.mLabel = ThunderballApp.LANGUAGE_NAMES[(int)Constants.Language];
            mLanguageLink.mUnderlineSize = 1;
            mLanguageLink.mUnderlineOffset = 1;
            mLanguageLink.SetVisible(!mFromBoard);
            AddWidget(mLanguageLink);
            AddWidget(mMusicSlider);
            AddWidget(mSfxSlider);
            AddWidget(mColorblindCheckbox);
            // Port: HD adds Left Handed too (the original never did, its left-handed layout unfinished).
            AddWidget(mLeftHandedCheckbox);
            AddWidget(mFullscreenCheckbox);
            AddWidget(mMainMenuButton);
            AddWidget(mNextLevelButton);
            AddWidget(mNewChallengeButton);
            AddWidget(mHelpButton);
            AddWidget(mDemoButton);
            AddWidget(mPortOptionsButton);
            AddWidget(mDoneButton);
            // Port: the port's name and version instead of the iOS bundle's ("2.0.9(4840)" / "1.7.3(4824)").
            mVersionString = "Peggle Enhanced v" + SexyUtil.GetPortVersionString();
        }

        private ThunderButton MakeNavButton(Image theImage, int theId, string theLabel, int w, int h)
        {
            ThunderButton button = new ThunderButton(theImage, theId, this);
            button.mOverlayText = theLabel;
            button.mButtonImage = theImage;
            button.mComponentImage = theImage;
            button.mWidth = w;
            button.mHeight = h / 2;
            button.mNormalRect = new TRect(0, 0, w, h / 2);
            button.mOverRect = new TRect(0, 0, w, h / 2);
            button.mDownRect = new TRect(0, h / 2, w, h / 2);
            return button;
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        public override void Update()
        {
            base.Update();
            if (mMusicOn && !mApp.mMusicEnabled)
            {
                mMusicOn = false;
                mMusicSlider.SetValue(0.0);
            }
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            ThunderballApp app = mApp;
            base.Resize(theX, theY, theWidth, theHeight);
#if PEGGLE_IPHONE
            int aGutter = SexyUtil.gGutterWidth;
#else
            int aGutter = 0;
#endif
            int left = GetLeft() + aGutter + OPTIONS_CHECKBOX_LEFT_OFFSET;
            int checkboxW = OPTIONS_CHECKBOX_W;
            int top = mFromBoard ? OPTIONS_TOP_INGAME : OPTIONS_TOP;
            mSfxSlider.Resize(OPTIONS_SFX_SLIDER_X + aGutter, top, OPTIONS_SFX_SLIDER_OFFSET_W + Res.IMAGE_DLG_SLIDERHILIGHT.mWidth, OPTIONS_SFX_SLIDER_H);
            mMusicSlider.Layout(LAY_STACK_BELOW, mSfxSlider, 0, OPTIONS_MUSIC_SLIDER_PAD_H, 0, 0);
            if (!mFromBoard)
            {
                mHelpButton.Layout(LAY_FIRST_BELOW, mMusicSlider, left, 0xf, checkboxW, 0);
                mDemoButton.Layout(LAY_STACK_BELOW, mHelpButton, 0, 5, 0, 0);
                mColorblindCheckbox.Layout(LAY_BESIDE, mHelpButton, 5, 0, 0, 0);
                mLeftHandedCheckbox.Layout(LAY_STACK_BELOW, mColorblindCheckbox, 0, 5, 0, 0);
                mPortOptionsButton.Layout(LAY_STACK_BELOW, mDemoButton, 0, 5, 0, 0);
                mDoneButton.Resize(OPTIONS_DONE_BT_X + aGutter * 2, OPTIONS_DONE_BT_Y, mDoneButton.mNormalRect.mWidth, mDoneButton.mNormalRect.mHeight);
#if PEGGLE_IPHONE
                mCreditsLink.Resize(OPTIONS_CREDITS_BT_X, OPTIONS_CREDITS_BT_Y, OPTIONS_CREDITS_BT_W,
                    mCreditsLink.mFont.GetHeight() + OPTIONS_CREDITS_BT_PAD_H);
                // No room below Credits on the short screen: it goes beside it.
                mLanguageLink.Resize(OPTIONS_CREDITS_BT_X + OPTIONS_CREDITS_BT_W, OPTIONS_CREDITS_BT_Y, OPTIONS_CREDITS_BT_W,
                    mCreditsLink.mHeight);
#else
                // Port: Credits and the language link share Credits' slot side by side (as on iPhone); below it,
                // the language link sat too close to the Extra Options button. DONE leaves no room to the right.
                // They line up with DONE's bottom, as on iPhone (OPTIONS_CREDITS_BT_Y left them higher up), Credits
                // starting at the buttons' visible left edge (as wide as its text, which is centred in it).
                // The buttons' art starts this far into the widget (its transparent margin).
                const int BUTTON_ART_INSET = 30;
                int aLinkH = mCreditsLink.mFont.GetHeight() + OPTIONS_CREDITS_BT_PAD_H;
                int aLinkY = mDoneButton.mY + mDoneButton.mHeight - aLinkH;
                int aCreditsW = mCreditsLink.mFont.StringWidth(mCreditsLink.mLabel);
                int aLinkX = mPortOptionsButton.mX + BUTTON_ART_INSET;
                mCreditsLink.Resize(aLinkX, aLinkY, aCreditsW, aLinkH);
                // The language link keeps its place: the right half of Credits' old slot.
                int aLangW = OPTIONS_CREDITS_BT_W / 2;
                mLanguageLink.Resize(OPTIONS_CREDITS_BT_X + aLangW, aLinkY, OPTIONS_CREDITS_BT_W - aLangW, aLinkH);
#endif
                PlaceFullscreenCheckbox();
                return;
            }
            mMainMenuButton.Resize(OPTIONS_MAINMENU_BT_X + aGutter, OPTIONS_NAV_BUTTONS_Y, mMainMenuButton.mNormalRect.mWidth, mMainMenuButton.mNormalRect.mHeight);
            mDoneButton.Resize(OPTIONS_INGAME_DONE_BT_X + aGutter, OPTIONS_NAV_BUTTONS_Y, mDoneButton.mNormalRect.mWidth, mDoneButton.mNormalRect.mHeight);
            int mode = app.mGameMode;
            if (mode == GameMode.GAMEMODE_ADVENTURE || mode == GameMode.GAMEMODE_ADVENTURE_NIGHTS
                || mode == GameMode.GAMEMODE_QUICKPLAY || mode == GameMode.GAMEMODE_DUEL)
            {
                mHelpButton.Layout(LAY_FIRST_BELOW, mMusicSlider, left, OPTIONS_HELP_BT_PAD_TOP, checkboxW, 0);
                mNextLevelButton.Layout(LAY_STACK_BELOW, mHelpButton, 0, OPTIONS_RESTART_BT_PAD_TOP, 0, 0);
                mColorblindCheckbox.Layout(LAY_BESIDE, mHelpButton, 5, 0, 0, 0);
                mLeftHandedCheckbox.Layout(LAY_STACK_BELOW, mColorblindCheckbox, 0, OPTIONS_LEFTHAND_BT_PAD_TOP, 0, 0);
            }
            else if (mode == GameMode.GAMEMODE_REPLAY || mode == GameMode.GAMEMODE_DEMO)
            {
                mColorblindCheckbox.Layout(LAY_FIRST_BELOW, mMusicSlider, mWidth / 2 - checkboxW / 2,
                    OPTIONS_REPLAY_COLORBLIND_BT_PAD_TOP, checkboxW, OPTIONS_REPLAY_COLORBLIND_BT_PAD_H);
                mLeftHandedCheckbox.Layout(LAY_STACK_BELOW, mColorblindCheckbox, 0, OPTIONS_REPLAY_LEFTHAND_BT_PAD_TOP, 0, 0);
            }
            if (mode == GameMode.GAMEMODE_CHALLENGE)
            {
                mNextLevelButton.Layout(LAY_FIRST_BELOW, mMusicSlider, left, OPTIONS_TROPHY_RESTART_BT_PAD_TOP, checkboxW, OPTIONS_TROPHY_RESTART_BT_PAD_H);
                mNewChallengeButton.Layout(LAY_STACK_BELOW, mNextLevelButton, 0, OPTIONS_TROPHY_NEWCHALLENGE_BT_PAD_TOP, 0, 0);
                mHelpButton.SetVisible(false);
                mColorblindCheckbox.Layout(LAY_BESIDE, mNextLevelButton, 5, 0, 0, 0);
                mLeftHandedCheckbox.Layout(LAY_STACK_BELOW, mColorblindCheckbox, 0, OPTIONS_TROPHY_COLORBLIND_BT_PAD_TOP, 0, 0);
            }
            PlaceFullscreenCheckbox();
        }

        /// <summary>Port addition: Fullscreen goes exactly where Left Handed was laid out.</summary>
        private void PlaceFullscreenCheckbox()
        {
            mFullscreenCheckbox.Resize(mLeftHandedCheckbox.mX, mLeftHandedCheckbox.mY, mLeftHandedCheckbox.mWidth, mLeftHandedCheckbox.mHeight);
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return SCREEN_HEIGHT;
        }

        public override void AddedToManager(WidgetManager theWidgetManager)
        {
            base.AddedToManager(theWidgetManager);
        }

        public override void RemovedFromManager(WidgetManager theWidgetManager)
        {
            base.RemovedFromManager(theWidgetManager);
        }

        public override void OrderInManagerChanged()
        {
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            g.SetFont(Res.FONT_SMALL);
            g.SetColor(SexyUtil.MakeColor(0x30307f));
            // Port: "Peggle Enhanced v1.0" is longer than the bundle version was; where DONE would cover it (iPad), it
            // goes below DONE's row, centred. (The iPhone's DONE is off to the side, with no room below it.)
            Font aFont = g.GetFont();
            int aTextW = aFont.StringWidth(mVersionString);
            int aTextX = mWidth / 2 - aTextW / 2;
            int aTextY = OPTIONS_VERSION_Y;
            TRect aDone = new TRect(mDoneButton.mX, mDoneButton.mY, mDoneButton.mWidth, mDoneButton.mHeight);
            if (aDone.Intersects(new TRect(aTextX, aTextY - aFont.GetAscent(), aTextW, aFont.GetHeight())))
            {
                aTextY = aDone.mY + aDone.mHeight + aFont.GetAscent() + 4;
            }
            g.DrawString(mVersionString, aTextX, aTextY);
        }

        public void SliderVal(int theId, double theVal)
        {
            if (IsScrolling())
            {
                return;
            }
            if (theId == OptionsDialog_SfxSlider)
            {
                mApp.SetSfxVolume(theVal);
                mSfxSlider.mLabel = GlobalStaticVars.gSexyAppBase.GetSfxVolume() > 0.0 ? mSfxLabel : mSfxOffLabel;
                if (!mSfxSlider.mDragging)
                {
                    mApp.PlaySample(Res.SOUND_BUTTON1);
                }
            }
            else if (theId == OptionsDialog_MusicSlider)
            {
                if (theVal > 0.01 && !mApp.mMusicEnabled)
                {
                    mApp.EnableMusic(true);
                    mApp.ResumeAudioSFX();
                }
                else if (theVal < 0.01 && mApp.mMusicEnabled)
                {
                    mApp.EnableMusic(false);
                }
                mMusicOn = mApp.mMusicEnabled;
                mMusicSlider.mLabel = mMusicOn ? mMusicLabel : mMusicOffLabel;
                mApp.SetMusicVolume(theVal);
            }
        }

        public void CheckboxChecked(int theId, bool @checked)
        {
            if (theId == OptionsDialog_Fullscreen)
            {
                // Port addition: applies at once (as Alt+Enter does).
                if (@checked != Main.graphics.IsFullScreen)
                {
                    SexyAppBase.XnaGame.ToggleFullscreen();
                }
                return;
            }
            // Settings are committed when the dialog closes (ThunderballApp::CommitSettings).
            IsScrolling();
        }

        public override void ButtonPress(int theId)
        {
            if (!IsScrolling())
            {
                base.ButtonPress(theId);
            }
        }

        public override void ButtonDepress(int theId)
        {
            if (IsScrolling())
            {
                return;
            }
            base.ButtonDepress(theId);
            ThunderballApp app = (ThunderballApp)GlobalStaticVars.gSexyAppBase;
            bool isDuel = mFromBoard && app.mBoard != null && app.mBoard.mGameMode == GameMode.GAMEMODE_DUEL;
            switch (theId)
            {
            case OptionsDialog_Help:
                app.ShowHelpScreen(false, isDuel);
                if (app.mBoard == null)
                {
                    app.FinishOptionsDialog(true, true);
                }
                break;
            case OptionsDialog_MainMenu:
                app.DoConfirmMainMenuDialog();
                break;
            case OptionsDialog_NextLevel:
                if (app.mBoard == null || app.mBoard.mEndLevelDialog.mWidgetManager != null || app.mBoard.mLevelBeaten
                    || (app.mBoard.mLogicMgr.mState == LogicMgr.STATE_LEVEL_DONE && app.mBoard.mLogicMgr.BeatLevel()))
                {
                    if (app.mGameMode == GameMode.GAMEMODE_CHALLENGE)
                    {
                        app.mBoard.mResetCount = 0;
                        app.mBoard.mTrophyLevelIdx = 0;
                    }
                    if (NextLevelSafteyCheck(app))
                    {
                        app.mBoard.Reset();
                    }
                    app.FinishOptionsDialog(true, true);
                }
                else
                {
                    app.DoConfirmRestartLevelDialog(-1);
                }
                break;
            case OptionsDialog_NewChallenge:
                app.DoConfirmNewChallengeDialog();
                if (app.mBoard == null || app.mBoard.mEndLevelDialog.mWidgetManager != null)
                {
                    app.FinishConfirmNewChallengeDialog(true);
                }
                break;
            case OptionsDialog_Demo:
                if (app.mPlayerInfo == null || app.mPlayerInfo.mMaxStage > 1 || app.mPlayerInfo.mNightsMaxStage > 0xc)
                {
                    app.mGameMode = GameMode.GAMEMODE_DEMO;
                    app.FinishOptionsDialog(true, true);
                    app.ShowBoard(true, true, false, false);
                }
                else
                {
                    string lines = SexyAppBase.Loc("^FFFF00^DEMO MODE LOCKED!^oldclr^\nThis mode shows how the Masters play.\n\n^FFFF00^To Unlock:^oldclr^\nFinish Stage 2 of Adventure Mode!");
                    ThunderDialog dialog = (ThunderDialog)app.DoDialog(DialogId.DIALOG_GENERIC_3, true, SexyAppBase.Loc("Locked"), lines, SexyAppBase.Loc("Ok"), 3);
                    dialog.mAutoSizeYesButton = true;
                    dialog.DoScroll(1);
                }
                break;
            case OptionsDialog_Credits:
                app.ShowCreditsScreen();
                if (app.mBoard == null)
                {
                    app.FinishOptionsDialog(true, true);
                }
                break;
            case OptionsDialog_Language:
                app.mLanguageChangePending = true;
                break;
            case OptionsDialog_PortOptions:
                app.DoPortOptionsDialog();
                break;
            }
        }

        /// <summary>
        /// Before restarting/advancing from the pause menu, diverts to the story, trophy room or
        /// progress screen when the level just finished calls for one. Returns true to reset the board.
        /// </summary>
        public bool NextLevelSafteyCheck(ThunderballApp theApp)
        {
            Board board = theApp.mBoard;
            if (ThunderballApp.mBonusAdventure != null)
            {
                // Port addition: a bonus Adventure (Extreme, WoW) has no trophy room or progress screen
                // between stages: a new stage (or the win) goes straight to its story screen.
                if (board != null && board.mLevelBeaten && theApp.mPlayerInfo != null && theApp.mPlayerInfo.GetmNeedShowStory()
                    && board.mEndLevelDialog != null && board.mEndLevelDialog.mType == 0)
                {
                    theApp.ShowStoryScreen(false, false);
                    return false;
                }
            }
            else if (theApp.mGameMode == GameMode.GAMEMODE_ADVENTURE)
            {
                if (board != null && board.mLevelBeaten && theApp.mCurLevel == 0 && theApp.mCurStage > 10)
                {
                    theApp.StopMusic();
                    theApp.ShowStoryScreen(false, false);
                    return false;
                }
                if (board != null && board.mEndLevelDialog != null && theApp.mCurLevel == 0
                    && theApp.mCurStage < theApp.mPlayerInfo.mLastAdventureStage + 1)
                {
                    if (theApp.mPlayerInfo.GetmMaxStage() == theApp.mCurStage && board.mEndLevelDialog.mType == 0)
                    {
                        theApp.ShowTrophyRoom(2);
                        return false;
                    }
                }
            }
            else if (theApp.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS && board != null && board.mLevelBeaten)
            {
                // The Nights trial-end screen is skipped: Nights is always owned in this port.
                if (theApp.mCurLevel == 0 && theApp.mCurStage > 0x16)
                {
                    theApp.ShowStoryScreen(true, false);
                    return false;
                }
                if (theApp.mPlayerInfo != null && theApp.mPlayerInfo.GetmNeedShowStory() && board.mEndLevelDialog != null
                    && board.mEndLevelDialog.mType == 0)
                {
                    theApp.ShowProgressScreen(1);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Port addition (menu navigation): B (Escape) is DONE; Start is too in the pause menu (opened from the board),
        /// whatever is selected, so Start both pauses and unpauses.
        /// </summary>
        /// <summary>Port addition (menu navigation): the pause menu opens on MAIN MENU; the main-menu Options on its first control.</summary>
        NavTarget INavHandler.NavDefault()
        {
            return mFromBoard ? new NavTarget(mMainMenuButton) : NavTarget.None;
        }

        bool INavHandler.NavAction(NavAction theAction)
        {
            if (theAction == NavAction.Back || (theAction == NavAction.Start && mFromBoard))
            {
                if (mScrollingOff)
                {
                    return true;
                }
                ((ThunderballApp)GlobalStaticVars.gSexyAppBase).FinishOptionsDialog(true, true);
                return true;
            }
            return false;
        }
    }
}
