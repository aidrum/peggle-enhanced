using System;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The main menu (Sexy::MainMenu): Bjorn (or Peggle Nights) backdrop with the rotating sun,
    /// the welcome speech bubble, the four mode buttons, More Games and the Stats/Trophies/Replays/Options row.
    /// The original's Store button (in-app purchases) is replaced by a QUIT button in the same spot;
    /// its "rate me" and update-available elements are online features and are not part of this port.
    /// </summary>
    public class MainMenu : Widget, ButtonListener
    {
        public const int MAINMENU_ADVENTURE = 0;

        public const int MAINMENU_QUICKPLAY = 1;

        public const int MAINMENU_DUEL = 2;

        public const int MAINMENU_CHALLENGE = 3;

        public const int MAINMENU_REPLAYS = 4;

        public const int MAINMENU_OPTIONS = 5;

        public const int MAINMENU_STATS = 6;

        public const int MAINMENU_TROPHIES = 7;

        public const int MAINMENU_QUIT = 8;

        public const int MAINMENU_CHANGE_USER = 9;

        public const int MAINMENU_MORE_GAMES = 10;

        private static readonly int[] sNoteList = { 2, 2, 3, 4, 4, 3, 2, 1, 0, 0, 1, 2, 2, 1, 1, -1 };

        public ThunderballApp mApp;                               // 0x7c

        public int mUpdateCount;                                  // 0x88

        public HyperlinkWidget mChangeUserLink;                   // 0xa4

        public ThunderButton mAdventureButton;                    // 0xa8

        public ThunderButton mQuickPlayButton;                    // 0xac

        public ThunderButton mDuelButton;                         // 0xb0

        public ThunderButton mChallengeButton;                    // 0xb4

        public ThunderButton mQuitButton;                         // 0xb8 (the Store button in the original)

        public ThunderButton mStatsButton;                        // 0xbc

        public ThunderButton mTrophiesButton;                     // 0xc0

        public ThunderButton mReplaysButton;                      // 0xc4

        public ThunderButton mOptionsButton;                      // 0xc8

        public ThunderButton mMoreGamesButton;                    // 0xcc

        public int mLastStage;                                    // 0xd0

        public int mLastLevel;                                    // 0xd4

        public int mNightsLastStage;                              // 0xd8

        public int mNightsLastLevel;                              // 0xdc

        public bool mRandomLevel;                                 // 0xe0

        public bool mFlagE1;                                      // 0xe1

        public int mResizeCounter;                                // 0xe4

        public int mNoteIndex;                                    // 0xe8

        public int mIntF0;                                        // 0xf0

        public int mSunCounter;                                   // 0xfc

        /// <summary>Port addition: the menu theme the menu was made with (the app's may change as it scrolls off).</summary>
        public bool mNightsMode;

        /// <summary>Port addition (Nights Menu option): the PC edition's Nights menu art, drawn instead of the Nights layers; null otherwise.</summary>
        public Image mNightsPCImage;

        private static Image sNightsPCImage;

        /// <summary>Port addition: the PC Nights menu's fireplace, drawn over sNightsPCImage (kept like it).</summary>
        private static NightsFireplace sNightsFire;

        /// <summary>
        /// Port addition: the Peggle Extreme menu (Menu Theme Last Played after an Extreme level): the day
        /// menu with a faceless sun, two walkers, and a headcrab on Bjorn's head that chomps twice a second.
        /// Its speech bubble is part of the headcrab art, so only the bubble's text is drawn.
        /// </summary>
        public bool mExtreme;

        /// <summary>Port addition: the Extreme art (tools/build_extreme_assets.py), PC art scaled by H / 600; kept once loaded.</summary>
        private static Image sExtremeImage;

        /// <summary>
        /// Port addition: the Peggle WoW menu (Menu Theme Last Played after a WoW level): the day menu's
        /// layout and buttons (no More Games) over WoW's animated scene, with Nights music.
        /// </summary>
        public bool mWoW;

        /// <summary>Port addition: the WoW scene; kept, as a menu may be drawn while it scrolls off after Dispose.</summary>
        private static WoWMenuScene sWoWScene;

        /// <summary>
        /// Port: the WoW scene follows the player's WoW Adventure, as the PC menu does: Splork stands there
        /// once it has been won (PC vf71/vf72), and Bjorn's quest marker shows whether it has been started.
        /// </summary>
        private void SyncWoWScene()
        {
            BonusAdventureInfo anInfo = null;
            mApp.mPlayerInfo?.mBonusAdventures.TryGetValue(StageMgr.ADVENTURE_WOW, out anInfo);
            bool aSplork = anInfo != null && anInfo.mWins > 0;
            if (sWoWScene == null || sWoWScene.mSplork != aSplork)
            {
                sWoWScene?.Dispose();
                sWoWScene = new WoWMenuScene(aSplork);
            }
            sWoWScene.mQuestStarted = anInfo != null && anInfo.mLastStageStage != -1;
            // The adventure's banner makes its entrance the first time this player's menu shows it (vf70).
            if (aSplork && mApp.mPlayerInfo.SeeDecal("WoW:Trophy:0"))
            {
                sWoWScene.StartBannerEntrance();
                mApp.mPlayerInfo.SaveIfDirty();
            }
        }

        /// <summary>Port addition: the menu plays Nights music (the WoW menu does, with the day layout).</summary>
        public bool MusicNights => mNightsMode || mWoW;

        private static int[] sChewSounds;

        private static readonly System.Random sRandom = new System.Random();

        /// <summary>Port addition: where the layout put the change-user link (the Extreme menu moves it into its bubble).</summary>
        private int mLinkX;

        private int mLinkY;

        /// <summary>Port addition: the cels of sExtremeImage at PC (800x600) scale.</summary>
        private static readonly TRect EXTREME_HEADCRAB_RECT = new TRect(0, 0, 387, 318);

        private static readonly TRect EXTREME_HEADCRAB2_RECT = new TRect(450, 0, 387, 318);

        private static readonly TRect EXTREME_WALKER_L_RECT = new TRect(0, 375, 292, 307);

        private static readonly TRect EXTREME_WALKER_R_RECT = new TRect(300, 375, 181, 198);

        private static readonly TRect EXTREME_BLANKSUN_RECT = new TRect(525, 375, 149, 149);

        /// <summary>Port addition: the tip of the horn in the headcrab art, and its bubble's middle (PC px).</summary>
        private const int EXTREME_HEADCRAB_HORN_X = 97;

        private const int EXTREME_HEADCRAB_HORN_Y = 60;

        private const int EXTREME_BUBBLE_CENTER_X = 254;

        private const int EXTREME_BUBBLE_CENTER_Y = 57;

        /// <summary>Port addition: how far (PC px) the headcrab sits above where its horn meets Bjorn's.</summary>
        private const int EXTREME_HEADCRAB_RAISE = 10;

        /// <summary>Port addition: the left walker hangs from the top of the screen, this far (PC px) from Bjorn's horn, raised past the top.</summary>
        private const int EXTREME_WALKER_L_X = -234;

        private const int EXTREME_WALKER_L_Y = -40;

#if PEGGLE_IPHONE
        /// <summary>
        /// Port addition: the tip of Bjorn's horn in IMAGE_MM_BJORN, and where the right walker stands on it
        /// (this edition's px): behind Bjorn's head, clear of the More Games sign.
        /// </summary>
        private const int EXTREME_BJORN_HORN_X = 452;

        private const int EXTREME_BJORN_HORN_Y = 1;

        private const int EXTREME_WALKER_R_X = 380;

        private const int EXTREME_WALKER_R_Y = 110;
#else
        /// <summary>
        /// Port addition: the tip of Bjorn's horn in IMAGE_MM_BJORN, and where the right walker stands on it
        /// (this edition's px): behind Bjorn's head, clear of the More Games sign.
        /// </summary>
        private const int EXTREME_BJORN_HORN_X = 310;

        private const int EXTREME_BJORN_HORN_Y = 143;

        private const int EXTREME_WALKER_R_X = 225;

        private const int EXTREME_WALKER_R_Y = 268;
#endif

        /// <summary>Port addition: the headcrab's chew sounds (SOUND_HEADCRAB, 2, 3).</summary>
        private const int EXTREME_CHEW_SOUNDS = 3;

        private static int H => SexyUtil.gPhysicalDisplayHeight;

        public MainMenu(ThunderballApp theApp)
        {
            mApp = theApp;
            // Port: the WoW look runs as Nights but shows the day menu over its own scene.
            mWoW = mApp.GetMenuThemeWoW();
            mNightsMode = mApp.mNightsMode && !mWoW;
            if (mWoW)
            {
                SyncWoWScene();
            }
            mApp.UnloadGameResources();
            mApp.UnloadQuickplayResources();
            mApp.LoadMainMenuResources(mNightsMode);
            if (mNightsMode && mApp.mNightsMenuPC)
            {
                // Kept once loaded: a menu may still be drawn while it scrolls off after Dispose.
                sNightsPCImage ??= mApp.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "mainmenuNights/nights_pc", true);
                mNightsPCImage = sNightsPCImage;
                sNightsFire ??= NightsFireplace.Load();
            }
            if (!mNightsMode && mApp.GetMenuThemeExtreme())
            {
                sExtremeImage ??= SexyUtil.LoadPCImage("images/pc/mainmenu_extreme");
                if (sChewSounds == null)
                {
                    sChewSounds = new int[EXTREME_CHEW_SOUNDS];
                    for (int i = 0; i < sChewSounds.Length; i++)
                    {
                        string aSuffix = i == 0 ? "" : (i + 1).ToString();
                        sChewSounds[i] = mApp.mSoundManager.LoadSound("sounds/headchew_extreme" + aSuffix + ".wav");
                    }
                }
                mExtreme = sExtremeImage != null;
            }
            mRandomLevel = false;
            mResizeCounter = 0;
            mFlagE1 = false;
            mSunCounter = 0;
            mNoteIndex = 0;
#if PEGGLE_IPHONE
            Image buttonImage = mNightsMode ? Res.IMAGE_DLG_BUTTON_PURPLE : Res.IMAGE_DLG_BUTTON_BLUE;
#else
            Image buttonImage = mNightsMode ? Res.IMAGE_MM_NIGHTS_BUTTON_PURPLE : Res.IMAGE_MM_BUTTON_BLUE;
#endif
            mAdventureButton = MakeModeButton(buttonImage, MAINMENU_ADVENTURE, "ADVENTURE");
            mQuickPlayButton = MakeModeButton(buttonImage, MAINMENU_QUICKPLAY, "QUICK PLAY");
            mChallengeButton = MakeModeButton(buttonImage, MAINMENU_CHALLENGE, "CHALLENGE");
            mDuelButton = MakeModeButton(buttonImage, MAINMENU_DUEL, "DUEL");
            mMoreGamesButton = SexyUtil.MakeEmbeddedButton(MAINMENU_MORE_GAMES, this,
                mNightsMode ? Res.IMAGE_MM_NIGHTS_MOREGAMES_BUTTON : Res.IMAGE_MM_MOREGAMES_BUTTON, false);
            mQuitButton = MakeModeButton(buttonImage, MAINMENU_QUIT, "QUIT");
            if (OperatingSystem.IsBrowser())
            {
                // Port addition (web): a page can't quit, so the button links to the project instead. A name,
                // so it isn't translated.
                mQuitButton.mOverlayText = "GitHub";
            }
            mReplaysButton = SexyUtil.MakeButton(MAINMENU_REPLAYS, this, SexyAppBase.Loc("Replays"));
#if !PEGGLE_IPHONE
            // The iPhone menu has no Trophies button.
            mTrophiesButton = SexyUtil.MakeButton(MAINMENU_TROPHIES, this, SexyAppBase.Loc("Trophies"));
#endif
            mOptionsButton = SexyUtil.MakeButton(MAINMENU_OPTIONS, this, SexyAppBase.Loc("Options"));
            mStatsButton = SexyUtil.MakeButton(MAINMENU_STATS, this, SexyAppBase.Loc("Stats"));
            mChangeUserLink = new HyperlinkWidget(MAINMENU_CHANGE_USER, this);
            mChangeUserLink.SetFont(Res.FONT_SMALL);
            mChangeUserLink.mColor = SexyUtil.MakeColor(0x86065d);
            mChangeUserLink.mOverColor = SexyUtil.MakeColor(0xff);
            mChangeUserLink.mDoFinger = true;
            mChangeUserLink.mLabel = SexyAppBase.Loc("Not You? Tap Here");
            mChangeUserLink.mUnderlineSize = 1;
            mChangeUserLink.mUnderlineOffset = 1;
            int linkWidth = mChangeUserLink.mFont.StringWidth(mChangeUserLink.mLabel);
#if PEGGLE_IPHONE
            int bubbleX = SexyUtil.gGutterWidth + MAINMENU_SPEECH_BUBBLE_POS_X + (mNightsMode ? MAINMENU_NIGHTS_SPEECH_BUBBLE_OFFSET_X : 0);
            mChangeUserLink.Resize(bubbleX + (Res.IMAGE_SPEECHBUBBLE.mWidth - linkWidth) / 2,
                MAINMENU_LINK_MENU_Y + (mNightsMode ? MAINMENU_NIGHTS_SPEECH_BUBBLE_OFFSET_Y : 0), linkWidth, mChangeUserLink.mFont.GetHeight());
#else
            if (!mNightsMode)
            {
                mChangeUserLink.Resize(MAINMENU_SPEECH_BUBBLE_POS_X + (Res.IMAGE_MM_SPEECHBUBBLE.mWidth - linkWidth) / 2,
                    MAINMENU_LINK_MENU_Y, linkWidth, mChangeUserLink.mFont.GetHeight());
            }
            else
            {
                mChangeUserLink.Resize(MAINMENU_SPEECH_BUBBLE_POS_X + MAINMENU_NIGHTS_SPEECH_BUBBLE_OFFSET_X + (Res.IMAGE_MM_NIGHTS_SPEECHBUBBLE.mWidth - linkWidth) / 2,
                    MAINMENU_LINK_MENU_Y + MAINMENU_NIGHTS_SPEECH_BUBBLE_OFFSET_Y, linkWidth, mChangeUserLink.mFont.GetHeight());
            }
#endif
            mChangeUserLink.SetVisible(true);
            AddWidget(mAdventureButton);
            AddWidget(mQuickPlayButton);
            AddWidget(mDuelButton);
            AddWidget(mChallengeButton);
            AddWidget(mQuitButton);
            AddWidget(mReplaysButton);
            AddWidget(mOptionsButton);
            AddWidget(mStatsButton);
            AddWidget(mMoreGamesButton);
#if !PEGGLE_IPHONE
            AddWidget(mTrophiesButton);
#endif
            AddWidget(mChangeUserLink);
            mApp.PlayMainMenuMusic(MusicNights, mWoW);
            mIntF0 = 0;
            mLastLevel = 0;
            mLastStage = 0;
            mNightsLastStage = 0xb;
            mNightsLastLevel = 0;
#if PEGGLE_IPHONE
            int x = MAINMENU_VERTICAL_BUTTONS_X + SexyUtil.gGutterWidth * 2;
#else
            int x = MAINMENU_VERTICAL_BUTTONS_X;
#endif
            int bh = mAdventureButton.mHeight;
            int gap = (SCREEN_HEIGHT - bh * 5) / 6;
            int y = gap;
            mAdventureButton.Resize(x, y, mAdventureButton.mWidth, bh);
            y += bh + gap;
            mQuickPlayButton.Resize(x, y, mQuickPlayButton.mWidth, bh);
            y += bh + gap;
            mChallengeButton.Resize(x, y, mChallengeButton.mWidth, bh);
            y += bh + gap;
            mDuelButton.Resize(x, y, mDuelButton.mWidth, bh);
            y += bh + gap;
            mQuitButton.Resize(x, y, mQuitButton.mWidth, bh);
#if PEGGLE_IPHONE
            // One row of three, bottom-aligned with the last mode button.
            int hx = MAINMENU_HORIZONTAL_BUTTONS_X;
            int hy = mQuitButton.mY + mQuitButton.mHeight - mStatsButton.mHeight;
            mStatsButton.Resize(hx, hy, MAINMENU_BUTTON_W, mStatsButton.mHeight);
            mOptionsButton.Resize(hx + MAINMENU_HORIZONTAL_BUTTONS_GAP, hy, MAINMENU_BUTTON_W, mOptionsButton.mHeight);
            mReplaysButton.Resize(hx + MAINMENU_HORIZONTAL_BUTTONS_GAP * 2, hy, MAINMENU_BUTTON_W, mReplaysButton.mHeight);
            if (!mNightsMode)
            {
                mMoreGamesButton.Move(SexyUtil.gGutterWidth + MAINMENU_MOREGAMES_BUTTON_X, MAINMENU_MOREGAMES_BUTTON_Y);
            }
            else
            {
                mMoreGamesButton.Move(SexyUtil.gGutterWidth + MAINMENU_MOREGAMES_BUTTON_X, MAINMENU_MOREGAMES_BUTTON_Y + MAINMENU_NIGHTS_MOREGAMES_BUTTON_OFFSET_Y);
            }
#else
            int hx = MAINMENU_HORIZONTAL_BUTTONS_X;
            int hy = SCREEN_HEIGHT - mStatsButton.mHeight * 2 - 10;
            mStatsButton.Resize(hx, hy, MAINMENU_BUTTON_W, mStatsButton.mHeight);
            mTrophiesButton.Resize(hx + MAINMENU_HORIZONTAL_BUTTONS_GAP, hy, MAINMENU_BUTTON_W, mTrophiesButton.mHeight);
            hy += mStatsButton.mHeight;
            mReplaysButton.Resize(hx, hy, MAINMENU_BUTTON_W, mReplaysButton.mHeight);
            mOptionsButton.Resize(hx + MAINMENU_HORIZONTAL_BUTTONS_GAP, hy, MAINMENU_BUTTON_W, mOptionsButton.mHeight);
            if (!mNightsMode)
            {
                mMoreGamesButton.Move(MAINMENU_MOREGAMES_BUTTON_X, MAINMENU_MOREGAMES_BUTTON_Y);
            }
            else
            {
                mMoreGamesButton.Move(MAINMENU_NIGHTS_MOREGAMES_BUTTON_X, MAINMENU_MOREGAMES_BUTTON_Y + MAINMENU_NIGHTS_MOREGAMES_BUTTON_OFFSET_Y);
            }
#endif
            mLinkX = mChangeUserLink.mX;
            mLinkY = mChangeUserLink.mY;
            SyncPlayerInfo();
            // Port addition: the PC Nights art has no spot for More Games.
            if (mNightsPCImage != null || mWoW)
            {
                mMoreGamesButton.SetVisible(false);
            }
#if PEGGLE_IPHONE
            // Port addition (ultrawide): buttons may sit past the game's edges, so the menu doesn't clip them.
            mClip = false;
            UpdateUltrawideButtons();
#endif
        }

#if PEGGLE_IPHONE
        /// <summary>Port addition (ultrawide): how far the button groups are moved now (left group left, right group right).</summary>
        private int mUltrawideShiftL;

        private int mUltrawideShiftR;

        /// <summary>
        /// Port addition (ultrawide): keeps the mode buttons (Adventure .. Quit) at the window's right edge
        /// and the Stats/Options/Replays row at its left edge, by the window's margins past the game.
        /// At 16:9 the margins are 0 and the buttons are where the layout put them. More Games stays.
        /// </summary>
        private void UpdateUltrawideButtons()
        {
            ThunderDialog.GetWindowMargins(out int aLeft, out int aRight);
            int dR = aRight - mUltrawideShiftR;
            int dL = aLeft - mUltrawideShiftL;
            if (dR != 0)
            {
                foreach (ThunderButton aButton in new[] { mAdventureButton, mQuickPlayButton, mChallengeButton, mDuelButton, mQuitButton })
                {
                    aButton.Move(aButton.mX + dR, aButton.mY);
                }
                mUltrawideShiftR = aRight;
            }
            if (dL != 0)
            {
                foreach (ThunderButton aButton in new[] { mStatsButton, mOptionsButton, mReplaysButton })
                {
                    aButton.Move(aButton.mX - dL, aButton.mY);
                }
                mUltrawideShiftL = aLeft;
            }
        }
#endif

        private ThunderButton MakeModeButton(Image theImage, int theId, string theLabel)
        {
            int w = theImage.mWidth;
            int h = theImage.mHeight;
            ThunderButton button = new ThunderButton(theImage, theId, this);
            button.mOverlayText = SexyAppBase.Loc(theLabel);
            button.mButtonImage = theImage;
            button.mComponentImage = theImage;
            button.mWidth = w;
            button.mHeight = h / 2;
            button.mNormalRect = new TRect(0, 0, w, h / 2);
            button.mOverRect = new TRect(0, 0, w, h / 2);
            button.mDownRect = new TRect(0, h / 2, w, h / 2);
            return button;
        }

        /// <summary>Caches the player's progress and updates the Bjorn badge level (0-3).</summary>
        public void SyncPlayerInfo()
        {
            mLastLevel = 0;
            mLastStage = 0;
            mNightsLastStage = 0xb;
            mNightsLastLevel = 0;
            int badge = 0;
            PlayerInfo player = mApp.mPlayerInfo;
            if (player == null)
            {
                return;
            }
            int shown = player.mStoryShownLevel;
            mLastStage = player.mLastStageStage;
            mLastLevel = player.mLastStageLevel;
            if (mLastStage < 0)
            {
                mLastStage = 0;
            }
            if (mLastLevel < 0)
            {
                mLastLevel = 0;
            }
            mNightsLastStage = player.mNightsLastStageStage;
            mNightsLastLevel = player.mNightsLastStageLevel;
            if (mNightsLastStage < 0xb)
            {
                mNightsLastStage = 0xb;
            }
            if (mNightsLastLevel < 0)
            {
                mNightsLastLevel = 0;
            }
            if (player.mAdventureWins > 0)
            {
                badge = 1;
                if (player.mPeggleTrophySet.Count > 0x27)
                {
                    badge = 2;
                    if (player.mPeggleClearedLevelSet.Count > 0x36)
                    {
                        badge = 3;
                    }
                }
            }
            if (shown < badge)
            {
                player.mStoryShownLevel = badge;
                player.SaveDetails();
            }
        }

        public override void Dispose()
        {
            mApp.UnloadMainMenuResources();
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            mResizeCounter = 10;
            mFlagE1 = false;
        }

        /// <summary>
        /// Port addition: in the WoW menu the day speech bubble moves left as far as it must to sit inside the
        /// scene's frame (with a little room); elsewhere theX is kept.
        /// </summary>
        private int WoWBubbleX(int theX, int theBubbleW)
        {
            if (!mWoW || sWoWScene == null || sWoWScene.mFrameInnerRight == int.MaxValue)
            {
                return theX;
            }
            int aRight = sWoWScene.mFrameInnerRight - 8;
            return System.Math.Min(theX, aRight - theBubbleW);
        }

        /// <summary>Port addition: how far WoWBubbleX moves the day speech bubble (0 outside the WoW menu).</summary>
        private int WoWBubbleShift()
        {
#if PEGGLE_IPHONE
            int x = MAINMENU_SPEECH_BUBBLE_POS_X + SexyUtil.gGutterWidth;
            return WoWBubbleX(x, Res.IMAGE_SPEECHBUBBLE.mWidth) - x;
#else
            int x = MAINMENU_SPEECH_BUBBLE_POS_X;
            return WoWBubbleX(x, Res.IMAGE_MM_SPEECHBUBBLE.mWidth) - x;
#endif
        }

        public void DrawSpeechBubble(Graphics g, int unused, int theAlpha, bool unused2)
        {
            string name = "";
            int lastStage = 0;
            PlayerInfo player = mApp.mPlayerInfo;
            if (player != null)
            {
                name = player.mName;
                lastStage = player.GetmLastStageStage();
            }
            int x;
            int y;
#if PEGGLE_IPHONE
            if (!mNightsMode)
            {
                x = MAINMENU_SPEECH_BUBBLE_POS_X + SexyUtil.gGutterWidth;
                y = MAINMENU_SPEECH_BUBBLE_POS_Y;
                x = WoWBubbleX(x, Res.IMAGE_SPEECHBUBBLE.mWidth);
            }
            else
            {
                x = MAINMENU_SPEECH_BUBBLE_POS_X + MAINMENU_NIGHTS_SPEECH_BUBBLE_OFFSET_X + SexyUtil.gGutterWidth;
                y = MAINMENU_SPEECH_BUBBLE_POS_Y + MAINMENU_NIGHTS_SPEECH_BUBBLE_OFFSET_Y;
            }
            if (!mExtreme)
            {
                g.DrawImage(Res.IMAGE_SPEECHBUBBLE, x, y + 10);
            }
#else
            if (!mNightsMode)
            {
                x = MAINMENU_SPEECH_BUBBLE_POS_X;
                y = MAINMENU_SPEECH_BUBBLE_POS_Y;
                x = WoWBubbleX(x, Res.IMAGE_MM_SPEECHBUBBLE.mWidth);
                if (!mExtreme)
                {
                    g.DrawImage(Res.IMAGE_MM_SPEECHBUBBLE, x, y + 10);
                }
            }
            else
            {
                x = MAINMENU_SPEECH_BUBBLE_POS_X + MAINMENU_NIGHTS_SPEECH_BUBBLE_OFFSET_X;
                y = MAINMENU_SPEECH_BUBBLE_POS_Y + MAINMENU_NIGHTS_SPEECH_BUBBLE_OFFSET_Y;
                g.DrawImage(Res.IMAGE_MM_NIGHTS_SPEECHBUBBLE, x, y + 10);
            }
#endif
            if (mExtreme)
            {
                // Port addition: the text goes in the headcrab's bubble.
                GetExtremeBubbleOffset(out int dx, out int dy);
                x += dx;
                y += dy;
            }
            TRect rect = new TRect(MAINMENU_SPEECHBUBBLE_WELCOMETEXT_OFFSET_X + x, MAINMENU_SPEECHBUBBLE_WELCOMETEXT_OFFSET_Y + y,
                MAINMENU_SPEECHBUBBLE_WELCOMETEXT_W, MAINMENU_SPEECHBUBBLE_WELCOMETEXT_H);
            if (name.Length == 0)
            {
                return;
            }
            string text = lastStage < 0 ? SexyAppBase.Loc("Welcome to Peggle,") : SexyAppBase.Loc("Welcome Back,");
            Font font = Res.FONT_SMALL;
            int tx = rect.mX + (rect.mWidth - font.StringWidth(text)) / 2 + mApp.GetInteger("MAINMENU_SPEECHBUBBLE_WELCOMETEXT_OFFSETX", 0);
            int ty = rect.mY + font.GetHeight();
            g.SetColor(SexyUtil.MakeColor(0x86065d, theAlpha));
            g.SetFont(font);
            g.DrawString(text, tx, ty);
            text = name + SexyAppBase.Loc("!");
            ty += font.GetHeight() + 4;
            tx = rect.mX + (rect.mWidth - font.StringWidth(text)) / 2;
            g.SetFont(font);
            g.DrawString(text, tx, ty);
        }

        public override void Draw(Graphics g)
        {
            if (mApp.mHelpScreen != null && mApp.mHelpScreen.mX == 0)
            {
                return;
            }
#if PEGGLE_IPHONE
            DrawIPhone(g);
#else
            if (mWoW)
            {
                DrawWoWStripes(g, new TRect(0, 0, mWidth, mHeight));
                sWoWScene.DrawFramed(g, 0, 0, mWidth, WoWSceneHeight());
                DrawSpeechBubble(g, 0, 0xff, true);
                return;
            }
            if (mNightsPCImage != null)
            {
                // Port addition (Nights Menu option): the PC art is already at the game's size, centred, with
                // the PC menu's fireplace.
                int aPCX = (mWidth - mNightsPCImage.mWidth) / 2;
                int aPCY = (mHeight - mNightsPCImage.mHeight) / 2;
                g.DrawImage(mNightsPCImage, aPCX, aPCY);
                sNightsFire?.Draw(g, aPCX, aPCY, 1, mUpdateCount);
                DrawSpeechBubble(g, 0, 0xff, true);
                return;
            }
            if (!mNightsMode)
            {
                g.DrawImage(Res.IMAGE_MM_BACKGROUND, 0, 0);
            }
            else
            {
                g.DrawImage(Res.IMAGE_MM_NIGHTS_BACKGROUND, 0, 0);
            }
            int sunX = MAIN_MENU_SUN_X;
            int sunY = MAIN_MENU_SUN_Y;
            g.SetColorizeImages(false);
            if (!mNightsMode)
            {
                g.DrawImageRotated(Res.IMAGE_LOAD_SUNGLOW_LOAD, (int)(sunX * H / 600.0), (int)(sunY * H / 600.0),
                    MathHelper.ToRadians(mSunCounter * 0.05f), (int)(H * 0x18a / 600.0), (int)(H * 0x17f / 600.0));
                DrawSunFace(g, (int)((sunX + 0x13f) * H / 600.0), (int)((sunY + 0x133) * H / 600.0));
                DrawExtremeBack(g);
                g.DrawImage(Res.IMAGE_MM_BJORN, 0, mHeight - Res.IMAGE_MM_BJORN.mHeight);
                DrawExtremeFront(g);
            }
            else
            {
                g.DrawImage(Res.IMAGE_MM_NIGHTS_MOON, (int)(MAIN_MENU_MOON_X * H / 600.0), (int)(MAIN_MENU_MOON_Y * H / 600.0));
                g.DrawImage(Res.IMAGE_MM_NIGHTS_BJORN, 0, mHeight - Res.IMAGE_MM_NIGHTS_BJORN.mHeight);
            }
            DrawSpeechBubble(g, 0, 0xff, true);
            if (mNightsMode)
            {
                g.DrawImage(Res.IMAGE_LOAD_NIGHTS_FRONT_TOP, 0, 0);
                g.DrawImage(Res.IMAGE_LOAD_NIGHTS_FRONT, 0, 0x203);
            }
#endif
        }

#if PEGGLE_IPHONE
        /// <summary>The iPhone menu: art centred on the wide screen, the sun and moon at fixed spots.</summary>
        private void DrawIPhone(Graphics g)
        {
            // The menu may still be on screen (scrolling off) after its art was unloaded: reload it.
            // The original skips this frame too; here the unload is deferred while the menu scrolls
            // off (ThunderballApp.UnloadGroup), so the reload just keeps the art and the frame is drawn.
            if (mApp.mLazyResourceLoading)
            {
                ResourceManager aRes = mApp.mResourceManager;
                bool aMissing = !aRes.IsGroupLoaded("MainMenuCommon") || !aRes.IsGroupLoaded(mNightsMode ? "MainMenuNights" : "MainMenuPeggle") ||
                    !aRes.IsGroupLoaded("CharSelect");
                if (aMissing)
                {
                    mApp.LoadMainMenuResources(mNightsMode);
                }
            }
            int aScreenX = (mWidth - mApp.mWidth) / 2;
            if (mWoW)
            {
                // Port addition: the blue stripes over the whole window (ultrawide margins too); the framed WoW
                // scene sits as at the top-left of a 4:3 screen, from where the day speech bubble is measured
                // (the gutter), so it lines up with the bubble whatever the window's width.
                ThunderDialog.GetWindowMargins(out int aLeft, out int aRight);
                Graphics aWideG = g.Create();
                aWideG.ClearClipRect();
                DrawWoWStripes(aWideG, new TRect(aScreenX - aLeft, 0, mApp.mWidth + aLeft + aRight, mHeight));
                sWoWScene.DrawFramed(aWideG, SexyUtil.gGutterWidth, 0, mHeight * 4 / 3, WoWSceneHeight());
                DrawSpeechBubble(g, 0, 0xff, true);
                return;
            }
            if (mNightsPCImage != null)
            {
                // Port addition (Nights Menu option): the PC art (larger than the screen) is centred,
                // and zooms like the backdrop to cover a window wider than it; the PC menu's fireplace goes with it.
                Graphics aBackG = Ultrawide.DrawBack(g, mNightsPCImage, aScreenX, (mHeight - mNightsPCImage.mHeight) / 2,
                    out int aPCX, out int aPCY, out float aPCZoom);
                sNightsFire?.Draw(aBackG, aPCX, aPCY, aPCZoom, mUpdateCount);
                DrawSpeechBubble(g, 0, 0xff, true);
                return;
            }
            Image back = mNightsMode ? Res.IMAGE_MM_NIGHTS_BACKGROUND : Res.IMAGE_MM_BACKGROUND;
            // Port addition (ultrawide): the backdrop (Peggle or Nights) zooms to cover a window wider than the game.
            Ultrawide.DrawBack(g, back, aScreenX, 0);
            int sunX = -170;
            int sunY = -190;
            g.SetColorizeImages(false);
            if (!mNightsMode)
            {
                if (mApp.mResourceManager.IsGroupLoaded("sunglow"))
                {
                    g.DrawImageRotated(Res.IMAGE_LOAD_SUNGLOW_LOAD, (int)(sunX * H / 600.0), (int)(sunY * H / 600.0),
                        MathHelper.ToRadians(mSunCounter * 0.05f), (int)(H * 0x18a / 600.0), (int)(H * 0x17f / 600.0));
                }
                DrawSunFace(g, (int)((sunX + 0x13f) * H / 600.0), (int)((sunY + 0x133) * H / 600.0));
                DrawExtremeBack(g);
                // Port addition (ultrawide): the whole MainMenu atlas, whose right part continues the hill.
                // Bjorn's piece stays where it was; the rest reaches out to the right on a wide window.
                Graphics aWide = g.Create();
                aWide.ClearClipRect();
                aWide.DrawImage(Res.IMAGE_MM_MAINMENU, (mWidth - Res.IMAGE_MM_BJORN.mWidth) / 2, mHeight - Res.IMAGE_MM_BJORN.mHeight);
                DrawExtremeFront(g);
            }
            else
            {
                g.DrawImage(Res.IMAGE_MM_NIGHTS_MOON, (int)((sunX + 0x199) * H / 600.0), (int)((sunY + 0x89) * H / 600.0));
                g.DrawImage(Res.IMAGE_MM_NIGHTS_BJORN, (mWidth - Res.IMAGE_MM_NIGHTS_BJORN.mWidth) / 2, mHeight - Res.IMAGE_MM_NIGHTS_BJORN.mHeight);
            }
            DrawSpeechBubble(g, 0, 0xff, true);
            if (mNightsMode && mApp.mResourceManager.IsGroupLoaded("NightsForeground"))
            {
                // Port: the wide (1136) layers. The loading screen swaps them into the plain ids when it draws,
                // but loading the group again (back from a level) puts the 960-wide ones back.
                Image aFrontTop = Res.IMAGE_LOAD_NIGHTS_FRONT_TOP_WIDE ?? Res.IMAGE_LOAD_NIGHTS_FRONT_TOP;
                Image aFront = Res.IMAGE_LOAD_NIGHTS_FRONT_WIDE ?? Res.IMAGE_LOAD_NIGHTS_FRONT;
                Ultrawide.DrawFrontTop(g, aFrontTop, aScreenX, 0);
                Ultrawide.DrawFrontBottom(g, aFront, aScreenX, aFrontTop.mHeight);
            }
        }

#endif
        /// <summary>The sun's face (IMAGE_MM_BLANKSUN); the Extreme menu's sun has none, centred in the same spot.</summary>
        private void DrawSunFace(Graphics g, int theX, int theY)
        {
            if (!mExtreme)
            {
                g.DrawImage(Res.IMAGE_MM_BLANKSUN, theX, theY);
                return;
            }
            TRect src = SexyUtil.ScalePCRect(EXTREME_BLANKSUN_RECT);
            g.DrawImage(sExtremeImage, theX + (Res.IMAGE_MM_BLANKSUN.mWidth - src.mWidth) / 2, theY + (Res.IMAGE_MM_BLANKSUN.mHeight - src.mHeight) / 2, src);
        }

        /// <summary>Port addition: the top-left of IMAGE_MM_BJORN on the screen, as Draw puts it.</summary>
        private int BjornX()
        {
#if PEGGLE_IPHONE
            return (mWidth - Res.IMAGE_MM_BJORN.mWidth) / 2;
#else
            return 0;
#endif
        }

        private int BjornY()
        {
            return mHeight - Res.IMAGE_MM_BJORN.mHeight;
        }

        /// <summary>Port addition: the top-left of the headcrab art, its horn on Bjorn's.</summary>
        private void GetHeadcrabPos(out int theX, out int theY)
        {
            double aScale = H / 600.0;
            theX = BjornX() + EXTREME_BJORN_HORN_X - (int)(EXTREME_HEADCRAB_HORN_X * aScale);
            theY = BjornY() + EXTREME_BJORN_HORN_Y - (int)((EXTREME_HEADCRAB_HORN_Y + EXTREME_HEADCRAB_RAISE) * aScale);
        }

        /// <summary>Port addition: how far the headcrab's bubble is from where the day bubble goes (middle to middle).</summary>
        private void GetExtremeBubbleOffset(out int theDX, out int theDY)
        {
#if PEGGLE_IPHONE
            Image aBubble = Res.IMAGE_SPEECHBUBBLE;
            int aBubbleX = MAINMENU_SPEECH_BUBBLE_POS_X + SexyUtil.gGutterWidth;
#else
            Image aBubble = Res.IMAGE_MM_SPEECHBUBBLE;
            int aBubbleX = MAINMENU_SPEECH_BUBBLE_POS_X;
#endif
            // The day bubble's tail hangs below its body; the body's middle is at 42% of its height.
            int aBubbleY = MAINMENU_SPEECH_BUBBLE_POS_Y + 10;
            double aScale = H / 600.0;
            GetHeadcrabPos(out int aCrabX, out int aCrabY);
            theDX = aCrabX + (int)(EXTREME_BUBBLE_CENTER_X * aScale) - (aBubbleX + aBubble.mWidth / 2);
            theDY = aCrabY + (int)(EXTREME_BUBBLE_CENTER_Y * aScale) - (aBubbleY + aBubble.mHeight * 42 / 100);
        }

        /// <summary>Port addition: the Extreme layers behind Bjorn: the walker hanging from the top of the screen and the one on the ground.</summary>
        private void DrawExtremeBack(Graphics g)
        {
            if (!mExtreme)
            {
                return;
            }
            int x = BjornX() + EXTREME_BJORN_HORN_X + (int)(EXTREME_WALKER_L_X * H / 600.0);
            g.DrawImage(sExtremeImage, x, (int)(EXTREME_WALKER_L_Y * H / 600.0), SexyUtil.ScalePCRect(EXTREME_WALKER_L_RECT));
            g.DrawImage(sExtremeImage, BjornX() + EXTREME_WALKER_R_X, BjornY() + EXTREME_WALKER_R_Y, SexyUtil.ScalePCRect(EXTREME_WALKER_R_RECT));
        }

        /// <summary>
        /// Port addition: the Extreme layer over Bjorn: the headcrab. As in Peggle Extreme's menu (PeggleExtreme.exe
        /// 0x4a8697): it shows its chewing frame (MM_CRAB2) when (update count / 10) % 10 is 1 or 3, so it
        /// chomps twice a second, a tenth of a second each.
        /// </summary>
        private void DrawExtremeFront(Graphics g)
        {
            if (!mExtreme)
            {
                return;
            }
            GetHeadcrabPos(out int x, out int y);
            int aPhase = mUpdateCnt / 10 % 10;
            bool aChewing = aPhase == 1 || aPhase == 3;
            g.DrawImage(sExtremeImage, x, y, SexyUtil.ScalePCRect(aChewing ? EXTREME_HEADCRAB2_RECT : EXTREME_HEADCRAB_RECT));
        }

        /// <summary>
        /// Port addition: the headcrab's chew sound, as in Peggle Extreme's menu Update (PeggleExtreme.exe
        /// 0x4aef61): once a second, at update count % 100 == 10 (its first chomp), one of the three at random.
        /// The update count only runs with no dialog up, so it pauses with the animation. Also keeps the
        /// change-user link in the headcrab's bubble.
        /// </summary>
        private void UpdateExtreme(bool theCanChew)
        {
            if (theCanChew && mUpdateCnt % 100 == 10)
            {
                int aChew = sRandom.Next(sChewSounds.Length);
                if (sChewSounds[aChew] >= 0)
                {
                    mApp.PlaySample(sChewSounds[aChew]);
                }
            }
            GetExtremeBubbleOffset(out int dx, out int dy);
            if (mChangeUserLink.mX != mLinkX + dx || mChangeUserLink.mY != mLinkY + dy)
            {
                mChangeUserLink.Move(mLinkX + dx, mLinkY + dy);
            }
        }

        /// <summary>
        /// Port addition: the WoW menu's backdrop, the stage cutscene's blue stripes (the story screen's dialog
        /// fill) over theRect edge to edge, without its border or dialog box.
        /// </summary>
        private static void DrawWoWStripes(Graphics g, TRect theRect)
        {
            SexyUtil.DrawDialogFill(g, new TRect(theRect.mX - DIALOG_FILL_MARGIN_LEFT, theRect.mY - DIALOG_FILL_MARGIN_TOP,
                theRect.mWidth + DIALOG_FILL_MARGIN_LEFT + DIALOG_FILL_MARGIN_RIGHT,
                theRect.mHeight + DIALOG_FILL_MARGIN_TOP + DIALOG_FILL_MARGIN_BOTTOM), 0x120952, 0x170b64);
            g.SetColor(new Color(255, 255, 255));
        }

        /// <summary>Port addition: the WoW scene stops above the bottom row of buttons (Stats, Options, ...).</summary>
        private int WoWSceneHeight()
        {
            return System.Math.Max(1, mStatsButton.mY - 8);
        }

        public override void Update()
        {
            // Port addition (launch target): the game the starting app asked for, once the menu is up with a player
            // and nothing over it (the new-player dialog).
            if (ThunderballApp.sLaunchTarget != null && mApp.mPlayerInfo != null && mUpdateCnt > 15 && mApp.mDialogList.Count == 0)
            {
                mApp.RunLaunchTarget();
            }
            if (mWoW)
            {
                SyncWoWScene();
                sWoWScene.Update();
                // The "Not You?" link goes with the speech bubble.
                int aShift = WoWBubbleShift();
                if (mChangeUserLink.mX != mLinkX + aShift)
                {
                    mChangeUserLink.Move(mLinkX + aShift, mChangeUserLink.mY);
                }
                MarkDirty();
            }
            if (mNightsPCImage != null && sNightsFire != null)
            {
                // The fire flickers every update.
                MarkDirty();
            }
#if PEGGLE_IPHONE
            if (mUpdateCount < 0x7fffffff)
            {
                mUpdateCount++;
            }
#else
            mUpdateCount = (mUpdateCount + 1) % 0x7fffffff;
#endif
#if PEGGLE_IPHONE
            UpdateUltrawideButtons();
#endif
            if (mExtreme)
            {
                UpdateExtreme(mApp.GetDialogCount() == 0 && mApp.mHelpScreen == null);
            }
            if (mApp.GetDialogCount() == 0 && mApp.mHelpScreen == null)
            {
                base.Update();
                if (mResizeCounter != 0)
                {
                    mResizeCounter--;
                }
                if (mIntF0 > 0)
                {
                    mIntF0--;
                    MarkDirty();
                }
                mSunCounter++;
                MarkDirty();
            }
        }

        public void DoNotYet(string theText, int theWidth)
        {
            ThunderDialog dialog = (ThunderDialog)mApp.DoDialog(DialogId.DIALOG_GENERIC_3, true, SexyAppBase.Loc("Locked"), theText, SexyAppBase.Loc("Ok"), 3);
            if (theWidth > -1)
            {
                SexyUtil.PositionDialog(dialog, theWidth, false, -1);
            }
            dialog.mAutoSizeYesButton = true;
            dialog.mCloseOnButton = true;
            dialog.DoScroll(1);
        }

        public bool HandleControlClick(int theId)
        {
            return false;
        }

        public void ButtonDepress(int theId)
        {
            if (HandleControlClick(theId))
            {
                return;
            }
            mRandomLevel = false;
            switch (theId)
            {
            case MAINMENU_ADVENTURE:
                mApp.ShowGameSelectDialog(0);
                break;
            case MAINMENU_QUICKPLAY:
                mApp.mGameMode = GameMode.GAMEMODE_QUICKPLAY;
                mRandomLevel = mQuickPlayButton.mTouchCancelled;
                if (!mApp.CheckSaveGame(true, false))
                {
                    StartGame();
                }
                break;
            case MAINMENU_DUEL:
                mApp.mGameMode = GameMode.GAMEMODE_DUEL;
                mRandomLevel = mDuelButton.mTouchCancelled;
                if (!mApp.CheckSaveGame(true, false))
                {
                    StartGame();
                }
                break;
            case MAINMENU_CHALLENGE:
                if (mApp.mPlayerInfo == null || PlayerInfo.gDebugUnlockAll || mApp.mPlayerInfo.mAdventureWins > 0 || mApp.mPlayerInfo.mNightsAdventureWins > 0)
                {
                    // Peggle Nights is always owned in this port.
                    mApp.ShowGameSelectDialog(1);
                }
                else
                {
                    DoNotYet(SexyAppBase.Loc("^FFFF00^CHALLENGE MODE LOCKED!^oldclr^\nThis mode presents challenges for advanced players.\n\n^FFFF00^To Unlock:^oldclr^\nComplete Peggle or Peggle Nights Adventure Mode!"), -1);
                }
                break;
            case MAINMENU_REPLAYS:
                mApp.ViewReplaysWithTip();
                break;
            case MAINMENU_OPTIONS:
                mApp.DoOptionsDialog();
                break;
            case MAINMENU_STATS:
                mApp.ShowGameSelectDialog(2);
                break;
            case MAINMENU_TROPHIES:
                mApp.ShowGameSelectDialog(3);
                break;
            case MAINMENU_QUIT:
                if (OperatingSystem.IsBrowser())
                {
                    mApp.PlaySample(Res.SOUND_BUTTON1);
                    mApp.OpenURL(UpdateChecker.REPOSITORY_URL);
                    break;
                }
                mApp.DoConfirmQuitDialog();
                break;
            case MAINMENU_CHANGE_USER:
                mApp.PlaySample(Res.SOUND_BUTTON1);
                mApp.DoUserDialog();
                break;
            case MAINMENU_MORE_GAMES:
                mApp.ShowMoreGamesScreen();
                break;
            }
        }

        public void StartGame()
        {
            PlayerInfo player = mApp.mPlayerInfo;
            switch (mApp.mGameMode)
            {
            case GameMode.GAMEMODE_ADVENTURE:
            case GameMode.GAMEMODE_ADVENTURE_NIGHTS:
                if (player != null && player.GetmAdventureWinCount() > 0 && player.JustBeatAdventure())
                {
                    mApp.DoConfirmRestartAdventureDialog();
                    break;
                }
                mApp.StartAdventureGame(mApp.mGameMode);
                break;
            case GameMode.GAMEMODE_QUICKPLAY:
            case GameMode.GAMEMODE_DUEL:
                if (mApp.mGameMode == GameMode.GAMEMODE_DUEL && player != null && player.mMaxStage < 1 && player.mNightsMaxStage < 0xc && !PlayerInfo.gDebugUnlockAll)
                {
                    DoNotYet(SexyAppBase.Loc("^FFFF00^DUEL MODE LOCKED!^oldclr^\nThis mode lets you compete against a friend or the computer.\n\n^FFFF00^To Unlock:^oldclr^\nFinish Stage 1 of Adventure Mode!"), -1);
                }
                else if (!mRandomLevel || player == null)
                {
                    mApp.ShowLevelScreen(false, true, false);
                }
                else
                {
                    // Port: the bonus pages' levels are in the pool too (Quick Play and Duel both get here),
                    // and the level plays as its own pack (music, characters), as from the level screen's
                    // Random Level.
                    bool aBonus = true;
                    mApp.mStageMgr.GetRandomLevel(player, ref mApp.mCurStage, ref mApp.mCurLevel, false, 0, true, false, aBonus);
                    mApp.SetLevelPack(StageMgr.GetLevelPackForStage(mApp.mCurStage));
                    mApp.ShowBoard(false, true, false, false);
                }
                break;
            case GameMode.GAMEMODE_CHALLENGE:
                if (player != null && !PlayerInfo.gDebugUnlockAll)
                {
                    if (ThunderballApp.mGameLevelPack == 1 && player.mNightsAdventureWins < 1)
                    {
                        DoNotYet(SexyAppBase.Loc("^FFFF00^PEGGLE NIGHTS CHALLENGE MODE LOCKED!^oldclr^\nThis mode presents 60 challenges for advanced players.\n\n^FFFF00^To Unlock:^oldclr^\nComplete Peggle Nights Adventure Mode!"),
                            MAINMENU_CHALLENGE_MODE_LOCKED_DIALOG_WIDTH);
                        break;
                    }
                    if (ThunderballApp.mGameLevelPack == 0 && player.mAdventureWins < 1)
                    {
                        DoNotYet(SexyAppBase.Loc("^FFFF00^PEGGLE CHALLENGE MODE LOCKED!^oldclr^\nThis mode presents 40 challenges for advanced players.\n\n^FFFF00^To Unlock:^oldclr^\nComplete Peggle Adventure Mode!"),
                            MAINMENU_CHALLENGE_MODE_LOCKED_DIALOG_WIDTH);
                        break;
                    }
                }
                mApp.ShowTrophyScreen(true);
                break;
            }
        }

        public void ButtonPress(int theId)
        {
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
        }

        public void ButtonDownTick(int theId)
        {
        }

        /// <summary>Easter egg: "Ode to Joy" on the mode buttons opens the Nights story.</summary>
        public void ButtonMouseEnter(int theId)
        {
            if (theId == sNoteList[mNoteIndex])
            {
                mNoteIndex++;
            }
            else
            {
                mNoteIndex = 0;
            }
            if (mNoteIndex > 0xe)
            {
                mApp.ShowStoryScreen(true, true);
            }
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
    }
}
