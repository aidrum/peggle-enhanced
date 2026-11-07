using System;
using System.Collections.Generic;
using System.IO;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>Game modes (Sexy::GameMode) as used by ThunderballApp::mGameMode.</summary>
    public static class GameMode
    {
        public const int GAMEMODE_NONE = 0;

        public const int GAMEMODE_ADVENTURE = 1;

        public const int GAMEMODE_QUICKPLAY = 2;

        public const int GAMEMODE_DUEL = 3;

        public const int GAMEMODE_CHALLENGE = 4;

        public const int GAMEMODE_REPLAY = 5;

        public const int GAMEMODE_DEMO = 6;

        public const int GAMEMODE_ADVENTURE_NIGHTS = 8;
    }

    /// <summary>Dialog ids used by ThunderballApp (values from the original binary).</summary>
    public static class DialogId
    {
        public const int DIALOG_GENERIC = 1;

        public const int DIALOG_CHARACTER = 1;

        public const int DIALOG_CONFIRM_DELETE_USER = 2;

        public const int DIALOG_CONFIRM_MAIN_MENU = 4;

        public const int DIALOG_CONFIRM_QUIT = 5;

        public const int DIALOG_CONFIRM_RESTART_LEVEL = 6;

        public const int DIALOG_CONFIRM_NEW_CHALLENGE = 7;

        public const int DIALOG_CONFIRM_RESTART_ADVENTURE = 8;

        public const int DIALOG_CREATE_USER = 10;

        public const int DIALOG_CREATE_USER_ERROR = 11;

        public const int DIALOG_END_LEVEL = 12;

        public const int DIALOG_GAME_TIP = 13;

        public const int DIALOG_TIP = 14;

        public const int DIALOG_LEVEL_SCREEN = 15;

        public const int DIALOG_HIGH_SCORE_ENTRY = 16;

        public const int DIALOG_OPTIONS = 19;

        public const int DIALOG_RENAME_USER = 20;

        public const int DIALOG_RENAME_USER_ERROR = 21;

        public const int DIALOG_REPLAY = 22;

        public const int DIALOG_SLOT_MACHINE = 23;

        public const int DIALOG_USER = 24;

        public const int DIALOG_CONTINUE = 25;

        public const int DIALOG_SAVE_GAME_ERROR = 26;

        public const int DIALOG_PAUSED = 27;

        public const int DIALOG_REPLAY_FILE = 28;

        public const int DIALOG_PAUSED_2 = 34;

        public const int DIALOG_STAGE = 35;

        public const int DIALOG_GENERIC_3 = 38;

        public const int DIALOG_LEVEL_DETAILS = 39;
        public const int DIALOG_TROPHY_DETAILS = 40;

        public const int DIALOG_TROPHY_ROOM = 41;

        public const int DIALOG_HELP = 42;

        public const int DIALOG_CREDITS = 44;

        public const int DIALOG_GAME_SELECT = 45;

        public const int DIALOG_TRIAL_SCREEN = 47;

        public const int DIALOG_FINISHED_TRIAL = 51;

        public const int DIALOG_NIGHTS_TRIAL_INTRO = 52;

        public const int DIALOG_NIGHTS_ADVENTURE_TRIAL_COMPLETE = 54;

        public const int DIALOG_RESTORE_DESC = 55;

        public const int DIALOG_REACHABILITY = 56;

        /// <summary>Port addition: the port's own options (PortOptionsDialog).</summary>
        public const int DIALOG_PORT_OPTIONS = 60;

        /// <summary>Port addition: the App Icon page's warning that the game closes to change the icon.</summary>
        public const int DIALOG_CONFIRM_APP_ICON = 61;

        /// <summary>Port addition: a newer Peggle Enhanced release is out (UpdateChecker).</summary>
        public const int DIALOG_UPDATE_AVAILABLE = 62;
    }

    /// <summary>Port addition: the Menu Theme choices of the port options (ThunderballApp.mMenuTheme).</summary>
    public static class MenuTheme
    {
        /// <summary>Peggle by day, Nights after 18:59, by the clock when the game started (the original's behaviour).</summary>
        public const int MENU_THEME_TIME = 0;

        /// <summary>The game of the last level played, kept across restarts.</summary>
        public const int MENU_THEME_LAST_PLAYED = 1;

        public const int MENU_THEME_PEGGLE = 2;

        public const int MENU_THEME_NIGHTS = 3;

        /// <summary>The Peggle Extreme look (a day theme).</summary>
        public const int MENU_THEME_EXTREME = 4;

        /// <summary>The Peggle WoW look (runs as Nights).</summary>
        public const int MENU_THEME_WOW = 5;

        public const int MENU_THEME_COUNT = 6;
    }

    /// <summary>
    /// Port of Sexy::ThunderballApp, Peggle Classic HD's application class ("Thunderball" was the
    /// game's internal codename). Owns the managers, the current screen widgets and the dialogs.
    /// Online services (Game Center, store, content updates, tracking, rating prompts) are not
    /// part of this port: the game behaves as if Peggle Nights is owned.
    /// </summary>
    public class ThunderballApp : SexyAppBase
    {
        public static int mGameLevelPack;

        /// <summary>
        /// Port addition: the bonus page whose Adventure is being played (Peggle Extreme, Peggle WoW),
        /// or null for Peggle's and Nights'. It plays as its page's level pack (mGameLevelPack) with the
        /// Adventure game mode of that pack; PlayerInfo's accessors then use its own progress. Set by
        /// SetLevelPack(pack, page); every other SetLevelPack and the main menu clear it.
        /// </summary>
        public static BonusPage mBonusAdventure;

        /// <summary>
        /// Port addition: a bonus Adventure (which has no Master stage) keeps each stage's master until it has
        /// been beaten; after that its levels open with the character select, as the Master stage's do.
        /// </summary>
        public static bool BonusMastersFixed(PlayerInfo thePlayer)
        {
            return mBonusAdventure != null && (thePlayer == null || thePlayer.GetmAdventureWinCount() == 0);
        }

        /// <summary>Port addition: a beaten bonus Adventure, whose levels open with the character select.</summary>
        public static bool BonusCharacterSelect(PlayerInfo thePlayer)
        {
            return mBonusAdventure != null && !BonusMastersFixed(thePlayer);
        }

        /// <summary>The global colour-blind flag the peg and brick images are picked by (PegInfo::mColorblind).</summary>
        public static bool mColorblind
        {
            get { return PegInfo.mColorblind; }
            set { PegInfo.mColorblind = value; }
        }

        public bool mLazyResourceLoading;                         // 0x1fc

        /// <summary>Port addition: the Lazy Loading option as last set; mLazyResourceLoading takes it on the next start.</summary>
        public bool mLazyLoadingOption;

        public DateTime mLocalTime;                               // 0x200 (struct tm)

        /// <summary>The menu theme: the main menu, loading screen and game-agnostic menus look and sound like Nights.</summary>
        public bool mNightsMode;                                  // 0x22c

        /// <summary>Port addition: the Menu Theme option (MenuTheme), saved in the registry.</summary>
        public int mMenuTheme;

        /// <summary>Port addition: the Nights Menu option: the Nights main menu shows the PC edition's art (nights_pc), saved in the registry.</summary>
        public bool mNightsMenuPC;

        /// <summary>Port addition: the Main Menu Theme option: the main menu plays the ambience loop instead of the menu theme, saved in the registry.</summary>
        public bool mMainMenuAmbience;

        /// <summary>
        /// Port addition: the Random Music option: a level plays a random level song, a new one each time it
        /// starts or restarts, as the PC games do; off, each level has its set song. Saved in the registry.
        /// </summary>
        public bool mRandomMusic;

        /// <summary>
        /// Port addition (controller): the Stick Aim option, 0 to 100: how fast the left stick turns the gun, from
        /// 0.4 to 2.5 times the standard speed (50). Saved in the registry.
        /// </summary>
        public int mPadAimSpeed = 50;

        /// <summary>
        /// Port addition: the PC Controls option (desktop only, on by default): the gun follows the mouse, a click
        /// on the board fires, a held right button fast-forwards, and the wheel and arrow keys fine-aim.
        /// Off, the mouse acts as a finger, as on the phone. Saved in the registry; always off on Android.
        /// </summary>
        public bool mPCControls;

        /// <summary>Port addition: the level song Random Music picked (3-12, before the Nights swap), or -1.</summary>
        private int mRandomLevelSong = -1;

        /// <summary>Port addition: while set (Board.PlayMusic for a level start or restart), PlaySong restarts a song that is already playing.</summary>
        public bool mRestartSameSong;

        /// <summary>Port addition: the last level played was a Nights one (for MENU_THEME_LAST_PLAYED), saved in the registry.</summary>
        public bool mLastPlayedNights;

        /// <summary>Port addition: the last level played was a Peggle Extreme one (for MENU_THEME_LAST_PLAYED), saved in the registry.</summary>
        public bool mLastPlayedExtreme;

        /// <summary>Port addition: the last level played was a Peggle WoW one (for MENU_THEME_LAST_PLAYED), saved in the registry.</summary>
        public bool mLastPlayedWoW;

        /// <summary>Port addition: the clock said Nights when the game started or MENU_THEME_TIME was picked again.</summary>
        public bool mTimeNights;

        public bool mShowAceAllStory;                             // 0x22d

        public int mLevelScreenScrollX;                           // 0x230

        /// <summary>Port addition: the level screen page to reopen on (LevelScreen.mPage).</summary>
        public int mLevelScreenPage;

        public int mLevelScreenScrollY;                           // 0x234

        public int mTrophyScreenScrollY;                          // 0x238

        public Buffer mSaveGameBuffer = new Buffer();             // 0x23c

        public int mScrollDirection;                              // 0x258

        public bool mShowedGameCenterLogin;                       // 0x278

        public int mGameMode;                                     // 0x27c

        public int mCurStage;                                     // 0x280

        public int mCurLevel;                                     // 0x284

        public int mChallengeId;                                  // 0x288

        public int mMaxStage;                                     // 0x28c

        public int mMaxLevel;                                     // 0x290

        public bool mHelpReturnsToBoard;                          // 0x294

        public bool mMusicLoaded;                                 // 0x295

        public int mCurMusic;                                     // 0x298

        public int mMusicIntensityOrder;                          // 0x2a4

        public bool mMusicIntensityIncreasePending;               // 0x2a8

        public double mFeverVolume;                               // 0x2ac

        public bool mHasFocus;                                    // 0x2b4

        /// <summary>Port addition: the dialog WriteRestoreInfo hid, shown again by GotFocus.</summary>
        private Dialog mRestoreHiddenDialog;

#if PEGGLE_IPHONE
        /// <summary>iPhone: where the replay list was left (its scroll y and selection).</summary>
        public int mReplayListScrollY;                            // 0x240 (iPhone)

        public int mReplayListSelected = -1;                      // 0x244 (iPhone)
#endif

        public int mNextReplayId;                                 // 0x2b8

        public int mRestoreLocation;                              // 0x2bc

        public int mRestoreGameMode;                              // 0x2c0

        public int mRestoreGameLevelPack;                         // 0x2c4

        /// <summary>Port addition: the bonus Adventure (BonusPage.mAdventureKey) being played when suspended, or "".</summary>
        public string mRestoreBonusAdventure = "";

        public string mReplayFileName = "";                       // 0x2cc

        public Board mBoard;                                      // 0x2d0

        public HelpScreen mHelpScreen;                            // 0x2d4

        public LevelScreen mLevelScreen;                          // 0x2d8

        public LoadingScreen mLoadingScreen;                      // 0x2dc

        public MainMenu mMainMenu;                                // 0x2e0

        public TrophyScreen mTrophyScreen;                        // 0x2e4

        public StoryScreen mStoryScreen;                          // 0x2e8

        /// <summary>Port addition: the Peggle WoW stage cutscene, while it plays.</summary>
        public WoWCutscene mWoWCutscene;

        /// <summary>Port addition: the PC Nights ending (the credits room), while it's up.</summary>
        public NightsEndingScreen mNightsEnding;

        public ProgressScreen mProgressScreen;                    // 0x2ec

        public AdventureScreen mAdventureScreen;                  // 0x2f0

        public TrophyRoom mTrophyRoom;                            // 0x2f4

        public MoreGamesScreen mMoreGamesScreen;                  // 0x2f8

        public CreditsScreen mCreditsScreen;                      // 0x300

        public Image[] mFeverStars = new Image[12];               // 0x308

        public int mDialogMode;                                   // 0x338

        public CharacterMgr mCharacterMgr;                        // 0x340

        public HighScoreMgr mHighScoreMgr;                        // 0x344

        public ImageMgr mImageMgr;                                // 0x348

        public ProfileMgr mProfileMgr;                            // 0x34c

        public StageMgr mStageMgr;                                // 0x354

        public StatsMgr mStatsMgr;                                // 0x358

        public StatsMgr mCheatStatsMgr;                           // 0x35c

        public TrophyMgr mTrophyMgr;                              // 0x360

        public WidgetMover mWidgetMover;                          // 0x364

        public FloatingTextPatMgr mFloatingTextPatMgr;            // 0x368

        public PlayerInfo mPlayerInfo;                            // 0x36c

        public int[] mTwoPlayerWins = new int[2];                 // 0x370

        public int[] mTwoPlayerFevers = new int[2];               // 0x378

        public int[] mTwoPlayerPoints = new int[2];               // 0x380

        public int mTwoPlayerWinner;                              // 0x388

        public int mTwoPlayerWinStreak;                           // 0x38c

        public GameStats[] mGameStats = { new GameStats(), new GameStats() }; // 0x390

        public int mFirstLaunchHour;                              // 0x488

        public int mFirstLaunchDay;                               // 0x48c

        public int mFirstLaunchMonth;                             // 0x490

        public int mFirstLaunchYear;                              // 0x494

        public bool mFirstLaunchDayPassed;                        // 0x498

        public bool mMenuMusicPlaying;                            // 0x499

        /// <summary>Port addition: set by the Options dialog's language link, handled in UpdateFrames.</summary>
        public bool mLanguageChangePending;

        /// <summary>Port addition: the results screen is waiting for Ode to Joy to end before the menu theme.</summary>
        public bool mResultsMusicPending;

        public static ThunderballApp gApp => (ThunderballApp)GlobalStaticVars.gSexyAppBase;

        public ThunderballApp(Main theGame) : base(theGame)
        {
            mFloatingTextPatMgr = new FloatingTextPatMgr();
            // Port: the shared generator (Sexy::Rand) starts from a fixed seed and the original never
            // reseeds it; on a phone its picks varied only with how many effects ran first. Here the
            // first Random Level after a launch was always the same, so it is seeded from the clock.
            // Scripted test runs keep the fixed seed, so they repeat exactly.
            if (!DebugScript.Active)
            {
                SexyUtil.gCommonMTRand.SRand((uint)DateTime.Now.Ticks);
            }
#if PEGGLE_IPHONE
            // The original reads the device's screen: an iPhone 4 (960x640) takes the 960x640 art as is,
            // an iPhone 5 (1136x640) centres it between two 88-pixel gutters. The port runs at any width
            // in between (Constants.ChooseScreenWidth, from the screen's aspect), with gutters to match.
            SexyUtil.gPhysicalDisplayHeight = 640;
            SexyUtil.gGutterWidth = 0;
            if (mWidth == 0x1e0)
            {
                SexyUtil.mResoultionType = 0;
            }
            else if (mWidth >= Constants.IPHONE_MIN_SCREEN_WIDTH && mWidth <= Constants.IPHONE_MAX_SCREEN_WIDTH)
            {
                SexyUtil.mResoultionType = 2;
                SexyUtil.gGutterWidth = (mWidth - Constants.IPHONE_MIN_SCREEN_WIDTH) / 2;
            }
            else if (mWidth == 0x400)
            {
                // The original's iPad layout; 1024 now falls in the range above (16:10 on the 960 art).
                SexyUtil.mResoultionType = 1;
            }
            else
            {
                SexyUtil.mResoultionType = 0;
            }
            SexyUtil.mLayoutScaleFactor = RESOLUTION_SCALE_FACTOR_ALL[SexyUtil.mResoultionType];
            if (SexyUtil.mLayoutScaleFactor != 1)
            {
                ScaleLayoutConstants(SexyUtil.mLayoutScaleFactor);
            }
            GlobalStaticVars.mResPathPrefix = PATH_RESOLUTION_PREFIX[SexyUtil.mResoultionType];
#endif
            mProdName = "Peggle";
            mLazyResourceLoading = true;
            mLocalTime = DateTime.Now;
            mNightsMode = false;
            mFirstLaunchDayPassed = false;
            // The original requires the Nights unlock purchase; this port treats Nights as owned.
            if (mLocalTime.Hour > 18)
            {
                mNightsMode = true;
            }
            mTimeNights = mNightsMode;
            mCharacterMgr = new CharacterMgr();
            mHighScoreMgr = new HighScoreMgr();
            mImageMgr = new ImageMgr();
            mProfileMgr = new ProfileMgr();
            mStageMgr = new StageMgr(this);
            mStatsMgr = new StatsMgr();
            mCheatStatsMgr = new StatsMgr();
            mTrophyMgr = new TrophyMgr(this);
            mWidgetMover = new WidgetMover();
            mCurStage = 0;
            mCurLevel = 0;
            mMaxStage = 1;
            mMaxLevel = 5;
            mFeverVolume = 0.85;
            mHasFocus = false;
            mShowAceAllStory = false;
            mShowedGameCenterLogin = false;
            mDialogMode = 0;
            mScrollDirection = 0;
            mNextReplayId = 1000;
            mCurMusic = -1;
            mMusicIntensityIncreasePending = false;
            mMusicIntensityOrder = -1;
            mMenuMusicPlaying = false;
        }

        public ImageMgr GetImageMgr()
        {
            return mImageMgr;
        }

        public override bool ShouldAutorotateToInterfaceOrientation(UI_ORIENTATION theOrientation)
        {
            return base.ShouldAutorotateToInterfaceOrientation(theOrientation) &&
                   (theOrientation == UI_ORIENTATION.UI_ORIENTATION_LANDSCAPE_LEFT || theOrientation == UI_ORIENTATION.UI_ORIENTATION_LANDSCAPE_RIGHT);
        }

        public override void Init()
        {
            base.Init();
            // As SexyAppBase::Init: volumes, mute, max stage, replay ids and first-launch time come from
            // the registry. (The port never called it, so none of these were loaded or saved.)
            ReadFromRegistry();
            // Port: the iOS build took the device language; here the first launch takes the system
            // language (English if the game lacks it) and keeps it, and the Options dialog changes it.
            int language = 0;
            if (!RegistryReadInteger("Language", ref language))
            {
                language = GetSystemLanguage();
                RegistryWriteInteger("Language", language);
            }
            if (language > 0 && language < LANGUAGE_COUNT)
            {
                Constants.Language = (Constants.LanguageIndex)language;
            }
            LoadLanguageStrings();
            // Port: the Menu Theme option picks the loading screen's and the main menu's look.
            RegistryReadInteger("MenuTheme", ref mMenuTheme);
            if (mMenuTheme < 0 || mMenuTheme >= MenuTheme.MENU_THEME_COUNT)
            {
                mMenuTheme = MenuTheme.MENU_THEME_TIME;
            }
            RegistryReadBoolean("LastPlayedNights", ref mLastPlayedNights);
            RegistryReadBoolean("LastPlayedExtreme", ref mLastPlayedExtreme);
            RegistryReadBoolean("LastPlayedWoW", ref mLastPlayedWoW);
            mNightsMode = GetMenuThemeNights();
            RegistryReadBoolean("NightsMenuPC", ref mNightsMenuPC);
            RegistryReadBoolean("MainMenuAmbience", ref mMainMenuAmbience);
            // Port: lazy loading is an option, read once here (a change applies on the next start).
            // Phones keep the original's lazy loading; PCs load everything while the loading screen shows.
            mLazyResourceLoading = Main.IsMobile;
            RegistryReadBoolean("LazyLoading", ref mLazyResourceLoading);
            // The web always loads everything during the loading screen (the option is hidden there): loading
            // lazily stalled the game on downloads whenever a screen opened.
            if (OperatingSystem.IsBrowser())
            {
                mLazyResourceLoading = false;
            }
            mLazyLoadingOption = mLazyResourceLoading;
            // Port: High Quality Pegs is on by default on PCs, off on phones.
            bool aHighQualityPegs = !Main.IsMobile;
            RegistryReadBoolean("HighQualityPegs", ref aHighQualityPegs);
            RegistryReadBoolean("RandomMusic", ref mRandomMusic);
            RegistryReadInteger("PadAimSpeed", ref mPadAimSpeed);
            mPadAimSpeed = Math.Clamp(mPadAimSpeed, 0, 100);
            mPCControls = !Main.IsMobile;
            if (mPCControls)
            {
                RegistryReadBoolean("PCControls", ref mPCControls);
            }
            PegInfo.mHighQualityPegs = aHighQualityPegs;
            ReadRestoreInfo();
            mResourceManager.ReadAllMaps();
            if (!mResourceManager.LoadResources("LoadCommon"))
            {
                ShowResourceError(true);
            }
            if (!Res.ExtractLoadCommonResources(mResourceManager, false))
            {
                ShowResourceError(true);
            }
            string loadGroup = mNightsMode ? "LoadNights" : "Load";
            if (!mResourceManager.LoadResources(loadGroup))
            {
                ShowResourceError(true);
            }
            if (!(mNightsMode ? Res.ExtractLoadNightsResources(mResourceManager, false) : Res.ExtractLoadResources(mResourceManager, false)))
            {
                ShowResourceError(true);
            }
#if PEGGLE_IPHONE
            // The iPhone build keeps the loading screen's sun glow (Peggle) and front layer (Nights)
            // in groups of their own, shared with the main menu.
            string frontGroup = mNightsMode ? "NightsForeground" : "Sunglow";
            if (!mResourceManager.LoadResources(frontGroup))
            {
                ShowResourceError(true);
            }
            if (!(mNightsMode ? Res.ExtractNightsForegroundResources(mResourceManager, false) : Res.ExtractSunglowResources(mResourceManager, false)))
            {
                ShowResourceError(true);
            }
#endif
            LoadMusic(0, "music/odetojoy.caf");
            LoadMusic(2, "music/opening.caf");
            for (int i = 1; i <= 12; i++)
            {
                LoadMusic(i + 2, string.Format("music/{0:00}.caf", i));
            }
            // Port addition: Peggle Nights' own soundtrack, played in place of songs 2-14 (see PlayMusic).
            LoadMusic(2 + NIGHTS_SONG_OFFSET, "music/nights/opening.caf");
            for (int i = 1; i <= 12; i++)
            {
                LoadMusic(i + 2 + NIGHTS_SONG_OFFSET, string.Format("music/nights/{0:00}.caf", i));
            }
            LoadMusic(AMBIENCE_SONG, "music/ambience.caf");
            LoadMusic(NIGHTS_AMBIENCE_SONG, "music/nights/ambience.caf");
            LoadMusic(WOW_AMBIENCE_SONG, "music/wow/ambience.caf");
            LoadMusic(WOW_OPENING_SONG, "music/wow/opening.caf");
            LoadMusic(WOW_OPENING_AMBIENCE_SONG, "music/wow/opening_ambience.caf");
            LoadMusic(NIGHTS_PC_AMBIENCE_SONG, "music/nights/ambience_pc.caf");
            LoadMusic(NIGHTS_CREDITS_SONG, "music/nights/credits.caf");
            LoadMusic(DUALSHOT_SONG_1, "music/dualshot/bonus1.caf");
            LoadMusic(DUALSHOT_SONG_2, "music/dualshot/bonus2.caf");
            mMusicLoaded = true;
            mCharacterMgr.Load();
            mHighScoreMgr.Load();
            mProfileMgr.Load();
            mStageMgr.Load(null);
            mStatsMgr.Load(null);
            mCheatStatsMgr.Load("LevelEditor/stats_cheat.dat");
            mTrophyMgr.Load(null);
            string curUser = "";
            if (mPlayerInfo == null && RegistryReadString("CurUser", ref curUser))
            {
                mPlayerInfo = mProfileMgr.GetProfile(curUser);
                mPlayerInfo?.UpdatePeggleAcedLevels(this);
            }
            if (mPlayerInfo == null)
            {
                mPlayerInfo = mProfileMgr.GetAnyProfile();
                mPlayerInfo?.UpdatePeggleAcedLevels(this);
            }
            if (string.IsNullOrEmpty(mStatsMgr.mPlayerName) && mPlayerInfo != null)
            {
                mStatsMgr.mPlayerName = mPlayerInfo.mName;
            }
            CheckMaxStage();
            ShowLoadingScreen();
            UpdateChecker.Start();
        }

        private void LoadLanguageStrings()
        {
            LoadProperties();
            LoadPopLoc();
            if (Constants.Language != Constants.LanguageIndex.en)
            {
                mPopLoc.mStrings["QUIT"] = QUIT_LABELS[(int)Constants.Language];
                foreach (KeyValuePair<string, string[]> aString in PORT_STRINGS)
                {
                    mPopLoc.mStrings[aString.Key] = aString.Value[(int)Constants.Language - 1];
                }
            }
            GlobalMembersDialog.DIALOG_YES_STRING = Loc("YES");
            GlobalMembersDialog.DIALOG_NO_STRING = Loc("NO");
            GlobalMembersDialog.DIALOG_OK_STRING = Loc("OK");
            GlobalMembersDialog.DIALOG_CANCEL_STRING = Loc("CANCEL");
            SexyUtil.SetCommaSeparator(Loc(",")[0]);
        }

        public const int LANGUAGE_COUNT = 5;

        /// <summary>The language names shown on the Options dialog's language link, in LanguageIndex order.</summary>
        public static readonly string[] LANGUAGE_NAMES = { "English", "Français", "Deutsch", "Español", "Italiano" };

        /// <summary>The main menu's QUIT button is a port addition, so the shipped string tables lack it.</summary>
        private static readonly string[] QUIT_LABELS = { "QUIT", "QUITTER", "BEENDEN", "SALIR", "ESCI" };

        /// <summary>The port options' strings, which the shipped string tables lack either (fr, de, es, it).</summary>
        private static readonly Dictionary<string, string[]> PORT_STRINGS = new Dictionary<string, string[]>
        {
            { "Extra Options", new[] { "Options extra", "Weitere Optionen", "Opciones extra", "Altre opzioni" } },
            { "EXTRA OPTIONS", new[] { "OPTIONS EXTRA", "WEITERE OPTIONEN", "OPCIONES EXTRA", "ALTRE OPZIONI" } },
            { "Menu Theme", new[] { "Thème du menu", "Menü-Design", "Tema del menú", "Tema del menu" } },
            { "Time-based", new[] { "Selon l'heure", "Nach Uhrzeit", "Según la hora", "In base all'ora" } },
            { "Last Played", new[] { "Dernier joué", "Zuletzt gespielt", "Último jugado", "Ultimo giocato" } },
            { "Low Memory Mode", new[] { "Mode mémoire réduite", "Sparespeicher-Modus", "Modo de baja memoria", "Risparmio memoria" } },
            { "Takes effect after a restart", new[] { "Prend effet au redémarrage", "Gilt nach einem Neustart", "Se aplica al reiniciar", "Si applica al riavvio" } },
            { "Theme Settings", new[] { "Thèmes", "Design-Einstellungen", "Ajustes de tema", "Impostazioni tema" } },
            { "General Settings", new[] { "Réglages généraux", "Allgemein", "Ajustes generales", "Impostazioni generali" } },
            { "Nights Menu", new[] { "Menu Nights", "Nights-Menü", "Menú de Nights", "Menu Nights" } },
            { "Mobile", new[] { "Mobile", "Mobil", "Móvil", "Mobile" } },
            { "PC", new[] { "PC", "PC", "PC", "PC" } },
            { "Main Menu Sound", new[] { "Son du menu principal", "Hauptmenü-Sound", "Sonido del menú principal", "Suono del menu principale" } },
            { "Ambience", new[] { "Ambiance", "Atmosphäre", "Ambiente", "Atmosfera" } },
            { "Music", new[] { "Musique", "Musik", "Música", "Musica" } },
            { "Random Music", new[] { "Musique aléatoire", "Zufällige Musik", "Música aleatoria", "Musica casuale" } },
            // Pegs as the game's own strings call them: taquets, Spielsteine, fichas, tondini.
            { "High Quality Pegs", new[] { "Taquets haute qualité", "Hochwertige Spielsteine", "Fichas de alta calidad", "Tondini in alta qualità" } },
            { "More Adventures", new[] { "Plus d'aventures", "Weitere Abenteuer", "Más aventuras", "Altre avventure" } },
            { "Main Adventures", new[] { "Principales", "Hauptstory", "Aventuras base", "Avventure base" } },
            // The update prompt (UpdateChecker).
            { "Update Available", new[] { "Mise à jour disponible", "Update verfügbar", "Actualización disponible", "Aggiornamento disponibile" } },
            { "A new version of Peggle Enhanced is available!", new[] { "Une nouvelle version de Peggle Enhanced est disponible !",
                "Eine neue Version von Peggle Enhanced ist verfügbar!", "¡Hay una nueva versión de Peggle Enhanced disponible!",
                "È disponibile una nuova versione di Peggle Enhanced!" } },
            { "Current version: ", new[] { "Version actuelle : ", "Aktuelle Version: ", "Versión actual: ", "Versione attuale: " } },
            { "New version: ", new[] { "Nouvelle version : ", "Neue Version: ", "Nueva versión: ", "Nuova versione: " } },
            { "Download", new[] { "Télécharger", "Herunterladen", "Descargar", "Scarica" } },
        };

        /// <summary>Port addition: the bundled language matching the system's, or English (0).</summary>
        private static int GetSystemLanguage()
        {
            string code = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Enum.TryParse(code, true, out Constants.LanguageIndex index) && Enum.IsDefined(index) ? (int)index : 0;
        }

        /// <summary>
        /// Port addition: switches to the next bundled language (en, fr, de, es, it, then back to en)
        /// while the main-menu Options dialog is up. Strings, localized images and the level,
        /// character and trophy configs are reloaded, and the main menu and the dialog are rebuilt.
        /// </summary>
        public void ChangeLanguage()
        {
            OptionsDialog options = (OptionsDialog)GetDialog(DialogId.DIALOG_OPTIONS);
            if (options == null || mBoard != null || mMainMenu == null)
            {
                return;
            }
            int language = ((int)Constants.Language + 1) % LANGUAGE_COUNT;
            Constants.Language = (Constants.LanguageIndex)language;
            RegistryWriteInteger("Language", language);
            CommitSettings();
            KillDialog(DialogId.DIALOG_OPTIONS);
            // The menu unloads its groups when it goes, so they load fresh in the new language.
            CleanupScreen(mMainMenu);
            mMainMenu = null;
            LoadLanguageStrings();
            foreach (string group in mResourceManager.ReloadLocalizedImages())
            {
                if (!Res.ExtractResourcesByName(mResourceManager, group, true))
                {
                    ShowResourceError(true);
                }
            }
            MakeFeverStars();
            mCharacterMgr.Load();
            mStageMgr.Load(null);
            mTrophyMgr.Load(null);
            mMainMenu = new MainMenu(this);
            AddScreen(mMainMenu);
            OptionsDialog dialog = new OptionsDialog(false);
#if PEGGLE_IPHONE
            SexyUtil.PositionDialog(dialog, mWidth, false, -1);
#else
            SexyUtil.PositionDialog(dialog, SCREEN_WIDTH, false, -1);
#endif
            AddDialog(DialogId.DIALOG_OPTIONS, dialog);
        }

        /// <summary>Port addition: opens the port options over the main-menu Options dialog.</summary>
        public void DoPortOptionsDialog()
        {
            KillDialog(DialogId.DIALOG_PORT_OPTIONS);
            PortOptionsDialog dialog = new PortOptionsDialog(this);
#if PEGGLE_IPHONE
            SexyUtil.PositionDialog(dialog, mWidth, false, -1);
#else
            SexyUtil.PositionDialog(dialog, SCREEN_WIDTH, false, -1);
#endif
            dialog.DoScroll(1);
            AddDialog(DialogId.DIALOG_PORT_OPTIONS, dialog);
        }

        /// <summary>Port addition: whether the loading screen takes the Peggle Extreme look (Last Played, after an Extreme level).</summary>
        public bool GetMenuThemeExtreme()
        {
            return (mMenuTheme == MenuTheme.MENU_THEME_EXTREME || (mMenuTheme == MenuTheme.MENU_THEME_LAST_PLAYED && mLastPlayedExtreme)) && !mNightsMode;
        }

        /// <summary>Port addition: whether the loading screen takes the Peggle WoW look (Last Played, after a WoW level; WoW plays as Nights).</summary>
        public bool GetMenuThemeWoW()
        {
            return (mMenuTheme == MenuTheme.MENU_THEME_WOW || (mMenuTheme == MenuTheme.MENU_THEME_LAST_PLAYED && mLastPlayedWoW)) && mNightsMode;
        }

        /// <summary>Port addition: whether the Menu Theme option asks for the Nights look now.</summary>
        public bool GetMenuThemeNights()
        {
            switch (mMenuTheme)
            {
            case MenuTheme.MENU_THEME_LAST_PLAYED:
                return mLastPlayedNights;
            case MenuTheme.MENU_THEME_PEGGLE:
            case MenuTheme.MENU_THEME_EXTREME:
                return false;
            case MenuTheme.MENU_THEME_NIGHTS:
            case MenuTheme.MENU_THEME_WOW:
                return true;
            default:
                return mTimeNights;
            }
        }

        /// <summary>
        /// Port addition: sets the Menu Theme option and saves it. Picking Time-based again reads the
        /// clock again; the theme changes at once (the main menu is rebuilt if it is up).
        /// </summary>
        public void SetMenuTheme(int theTheme)
        {
            if (theTheme == mMenuTheme)
            {
                return;
            }
            mMenuTheme = theTheme;
            RegistryWriteInteger("MenuTheme", theTheme);
            if (theTheme == MenuTheme.MENU_THEME_TIME)
            {
                mTimeNights = DateTime.Now.Hour > 18;
            }
            ApplyMenuTheme();
        }

        /// <summary>Port addition: sets the Nights Menu option and saves it; a Nights main menu that is up is rebuilt.</summary>
        public void SetNightsMenuPC(bool thePC)
        {
            if (thePC == mNightsMenuPC)
            {
                return;
            }
            mNightsMenuPC = thePC;
            RegistryWriteBoolean("NightsMenuPC", thePC);
            if (mNightsMode)
            {
                RebuildMainMenu();
            }
        }

        /// <summary>Port addition: sets the Main Menu Theme option and saves it; the main menu, if it is up, switches at once.</summary>
        public void SetMainMenuAmbience(bool theAmbience)
        {
            if (theAmbience == mMainMenuAmbience)
            {
                return;
            }
            mMainMenuAmbience = theAmbience;
            RegistryWriteBoolean("MainMenuAmbience", theAmbience);
            if (mMainMenu != null && !mWidgetMover.WillRemove(mMainMenu) && mHelpScreen == null)
            {
                PlayMainMenuMusic(mMainMenu.MusicNights, mMainMenu.mWoW);
            }
        }

        /// <summary>Port addition: sets the High Quality Pegs option and saves it; it applies at once.</summary>
        public void SetHighQualityPegs(bool theOn)
        {
            PegInfo.mHighQualityPegs = theOn;
            RegistryWriteBoolean("HighQualityPegs", theOn);
        }

        /// <summary>Port addition: sets the Random Music option and saves it; it applies from the next level start.</summary>
        public void SetRandomMusic(bool theOn)
        {
            mRandomMusic = theOn;
            RegistryWriteBoolean("RandomMusic", theOn);
        }

        /// <summary>Port addition (controller): sets the Stick Aim option (0 to 100) and saves it; it applies at once.</summary>
        public void SetPadAimSpeed(int theSpeed)
        {
            mPadAimSpeed = Math.Clamp(theSpeed, 0, 100);
            RegistryWriteInteger("PadAimSpeed", mPadAimSpeed);
        }

        /// <summary>Port addition (controller): the Stick Aim option as a factor on the stick's speed, 0.4 to 2.5.</summary>
        public float GetPadAimScale()
        {
            return (float)Math.Pow(2.5, mPadAimSpeed / 50.0 - 1.0);
        }

        /// <summary>
        /// Port addition: whether the board is laid out left-handed. PC Controls force it: always on HD (the
        /// left-handed frames), never on iPhone. Otherwise the player's Left Handed option.
        /// </summary>
        public bool IsLeftHanded()
        {
#if PEGGLE_IPHONE
            if (mPCControls)
            {
                return false;
            }
#else
            if (mPCControls)
            {
                return true;
            }
#endif
            return mPlayerInfo != null && mPlayerInfo.mLeftHanded;
        }

        /// <summary>Port addition: sets the PC Controls option and saves it; it applies at once.</summary>
        public void SetPCControls(bool theOn)
        {
            if (Main.IsMobile)
            {
                return;
            }
            mPCControls = theOn;
            RegistryWriteBoolean("PCControls", theOn);
            if (mBoard != null && mPlayerInfo != null)
            {
                // HD: PC Controls change the layout (left-handed frames, no wheel).
                mBoard.SetPadLayout(!IsLeftHanded());
                if (mBoard.mReplayDialog.mVisible)
                {
                    mBoard.PositionReplayDialog();
                }
            }
        }

        /// <summary>Port addition: sets the Lazy Loading option and saves it; it applies on the next start.</summary>
        public void SetLazyLoading(bool theLazy)
        {
            mLazyLoadingOption = theLazy;
            RegistryWriteBoolean("LazyLoading", theLazy);
        }

        /// <summary>Port addition: the bonus page (Extreme, WoW, ...) a level (theLevelName, a level file path) belongs to, or null.</summary>
        public BonusPage GetLevelBonusPage(string theLevelName)
        {
            LevelInfo aLevel = mStageMgr.GetLevelInfoByName(SexyUtil.GetFileName(theLevelName, true));
            return aLevel == null ? null : StageMgr.GetBonusPage(aLevel.mStage);
        }

        /// <summary>
        /// Port addition: a level of the current level pack is being played (adventure, quick play,
        /// duel, challenge or demo). Remembered for the Last Played menu theme, which follows it;
        /// a Peggle Extreme level (theLevelName, a level file path) also gives the loading screen its Extreme look.
        /// </summary>
        public void NoteLevelPlayed(string theLevelName)
        {
            bool nights = mGameLevelPack == 1;
            if (nights != mLastPlayedNights)
            {
                mLastPlayedNights = nights;
                RegistryWriteBoolean("LastPlayedNights", nights);
            }
            BonusPage aPage = GetLevelBonusPage(theLevelName);
            bool extreme = aPage != null && aPage.mLogoCel == GameArrowButton.LOGO_EXTREME;
            if (extreme != mLastPlayedExtreme)
            {
                mLastPlayedExtreme = extreme;
                RegistryWriteBoolean("LastPlayedExtreme", extreme);
            }
            bool wow = aPage != null && aPage.mLogoCel == GameArrowButton.LOGO_WOW;
            if (wow != mLastPlayedWoW)
            {
                mLastPlayedWoW = wow;
                RegistryWriteBoolean("LastPlayedWoW", wow);
            }
            if (mMenuTheme == MenuTheme.MENU_THEME_LAST_PLAYED)
            {
                ApplyMenuTheme();
            }
        }

        /// <summary>
        /// Port addition: switches mNightsMode to what the Menu Theme option asks for. The main menu
        /// and loading screen keep the look they were made with, so one that is up is rebuilt;
        /// otherwise the next one is made with the new look.
        /// </summary>
        public void ApplyMenuTheme()
        {
            bool nights = GetMenuThemeNights();
            if (nights == mNightsMode && (mMainMenu == null || (mMainMenu.mExtreme == GetMenuThemeExtreme() && mMainMenu.mWoW == GetMenuThemeWoW())))
            {
                return;
            }
            mNightsMode = nights;
            RebuildMainMenu();
        }

        /// <summary>Port addition: makes the main menu again (under any dialogs) if it is up, for a changed look.</summary>
        public void RebuildMainMenu()
        {
            if (mMainMenu == null || mWidgetMover.WillRemove(mMainMenu))
            {
                return;
            }
            CleanupScreen(mMainMenu);
            mMainMenu = new MainMenu(this);
            AddScreen(mMainMenu);
            foreach (Dialog aDialog in mDialogList)
            {
                mWidgetManager.BringToFront(aDialog);
            }
        }

        public void ReadRestoreInfo()
        {
            mRestoreLocation = 0;
            RegistryReadInteger("RestoreLocation", ref mRestoreLocation);
            if (mRestoreLocation == 1)
            {
                RegistryReadInteger("RestoreGameMode", ref mRestoreGameMode);
            }
            RegistryReadInteger("RestoreGameLevelPack", ref mRestoreGameLevelPack);
            mRestoreBonusAdventure = "";
            RegistryReadString("RestoreBonusAdventure", ref mRestoreBonusAdventure);
            RegistryReadInteger("mTwoPlayerWins0", ref mTwoPlayerWins[0]);
            RegistryReadInteger("mTwoPlayerWins1", ref mTwoPlayerWins[1]);
            RegistryReadInteger("mTwoPlayerFevers0", ref mTwoPlayerFevers[0]);
            RegistryReadInteger("mTwoPlayerFevers1", ref mTwoPlayerFevers[1]);
            RegistryReadInteger("mTwoPlayerPoints0", ref mTwoPlayerPoints[0]);
            RegistryReadInteger("mTwoPlayerPoints1", ref mTwoPlayerPoints[1]);
            RegistryReadInteger("mTwoPlayerWinner", ref mTwoPlayerWinner);
            RegistryReadInteger("mTwoPlayerWinStreak", ref mTwoPlayerWinStreak);
        }

        public void LoadMusic(int theSongId, string theFileName)
        {
            bool ok = mMusicInterface.LoadMusic(theSongId, ResourceManager.GetResourceDir() + theFileName);
            Debug.OutputDebug(string.Format("Loaded music {0} -- {1}", theFileName, ok ? "ok" : "failed"));
        }

        public void CheckMaxStage()
        {
            if (mPlayerInfo == null)
            {
                return;
            }
            if (mMaxStage < mPlayerInfo.mMaxStage)
            {
                mMaxStage = mPlayerInfo.mMaxStage;
                mMaxLevel = mPlayerInfo.mMaxLevel;
            }
            else if (mPlayerInfo.mMaxStage == mMaxStage && mMaxLevel < mPlayerInfo.mMaxLevel)
            {
                mMaxLevel = mPlayerInfo.mMaxLevel;
            }
        }

        public void ShowLoadingScreen()
        {
            CleanupScreens(true, true);
            // Port: the Peggle WoW look has its own loading screen, and Nights shows the PC edition's intro.
            mLoadingScreen = GetMenuThemeWoW() ? new WoWLoadingScreen(this) : mNightsMode ? new NightsLoadingScreen(this) : new LoadingScreen(this);
            mLoadingScreen.Resize(0, 0, mWidth, mHeight);
            mWidgetManager.AddWidget(mLoadingScreen);
            mWidgetManager.SetFocus(mLoadingScreen);
        }

        public override void InitHook()
        {
            mDialogMode = 0;
        }

#if PEGGLE_IPHONE
        /// <summary>
        /// Port addition (ultrawide): during a level, a touch where there's nothing (the side bands beside the
        /// board and dial, which cover the original 960 px, and the window's margins) goes to the board, which
        /// takes it as a tap anywhere (Claude's flippers, skipping) and ignores it otherwise (Board.TouchBegan).
        /// Not while a modal dialog is up.
        /// </summary>
        public override Widget GetUncoveredTouchWidget(int x, int y)
        {
            if (mBoard != null && mBoard.mWidgetManager != null && mBoard.mVisible && mWidgetManager.mBaseModalWidget == null)
            {
                return mBoard;
            }
            return null;
        }
#endif

        public override void Shutdown()
        {
            if (!mShutdown)
            {
                base.Shutdown();
                CleanupScreens(true, true);
                mWidgetMover.Clear();
            }
        }

        public void CleanupScreens(bool removeBoard, bool removeMainMenu)
        {
            if (removeBoard)
            {
                if (mBoard != null)
                {
                    if (mBoard.NeedSaveGame())
                    {
                        mBoard.SaveGame();
                    }
                    mBoard.NotifyRemoving();
                }
                CleanupScreen(mBoard);
                mBoard = null;
            }
            if (removeMainMenu && mMainMenu != null)
            {
                CleanupScreen(mMainMenu);
                mMainMenu = null;
            }
            CleanupScreen(mHelpScreen);
            mHelpScreen = null;
            CleanupScreen(mProgressScreen);
            mProgressScreen = null;
            CleanupScreen(mLevelScreen);
            mLevelScreen = null;
            CleanupScreen(mLoadingScreen);
            mLoadingScreen = null;
            CleanupScreen(mTrophyScreen);
            mTrophyScreen = null;
            CleanupScreen(mStoryScreen);
            mStoryScreen = null;
            CleanupScreen(mWoWCutscene);
            mWoWCutscene = null;
            CleanupScreen(mNightsEnding);
            mNightsEnding = null;
            CleanupScreen(mAdventureScreen);
            mAdventureScreen = null;
            CleanupScreen(mTrophyRoom);
            mTrophyRoom = null;
            CleanupScreen(mMoreGamesScreen);
            mMoreGamesScreen = null;
            CleanupScreen(mCreditsScreen);
            mCreditsScreen = null;
            KillDialog(DialogId.DIALOG_GENERIC);
            KillDialog(DialogId.DIALOG_STAGE);
            KillDialog(DialogId.DIALOG_GAME_SELECT);
            KillDialog(DialogId.DIALOG_REPLAY_FILE);
        }

        public void ShutdownHook()
        {
            mSoundManager?.StopAllSounds();
            mMusicInterface?.StopMusic(0f);
        }

        public void UpdateMusic()
        {
        }

        /// <summary>
        /// Port addition: once Ode to Joy has played out under the results screen, starts the menu
        /// theme of the level pack just played. Leaving the results (next level, main menu, ...)
        /// changes or stops the music itself, which drops the wait.
        /// </summary>
        private void UpdateResultsMusic()
        {
            if (mBoard == null || mBoard.mEndLevelDialog.mWidgetManager == null || mCurMusic != 0)
            {
                mResultsMusicPending = false;
                return;
            }
            if (!mMusicInterface.IsPlaying(0))
            {
                mResultsMusicPending = false;
                PlayMenuMusic(mGameLevelPack == 1);
            }
        }

        public static int GetMusicOrder(int theTrack)
        {
            return -1;
        }

        public void SetMusicIntensityIncreasePending(int theIntensity)
        {
            if (theIntensity < 7)
            {
                int order = GetMusicOrder(theIntensity);
                if (order != -1)
                {
                    mMusicIntensityIncreasePending = true;
                    mMusicIntensityOrder = order;
                }
            }
        }

        /// <summary>Port addition: the Nights version of song N (2 = opening, 3-14 = 01-12) is N + this.</summary>
        public const int NIGHTS_SONG_OFFSET = 15;

        /// <summary>Port addition: the main menu's ambience loops (Main Menu Theme option), past the Nights songs.</summary>
        public const int AMBIENCE_SONG = 30;

        public const int NIGHTS_AMBIENCE_SONG = 31;

        /// <summary>Port addition: the Peggle WoW menu's ambience loop (WoW's Menu_Amb_2).</summary>
        public const int WOW_AMBIENCE_SONG = 32;

        /// <summary>Port addition: the Peggle WoW loading screen's opening piece.</summary>
        public const int WOW_OPENING_SONG = 33;

        /// <summary>Port addition: the loop the Peggle WoW loading screen plays once its opening is done.</summary>
        public const int WOW_OPENING_AMBIENCE_SONG = 34;

        /// <summary>Port addition: the Nights main menu's ambience loop with the Nights Menu option on PC.</summary>
        public const int NIGHTS_PC_AMBIENCE_SONG = 35;

        /// <summary>Port addition: the PC Nights credits room's music (NightsEndingScreen).</summary>
        public const int NIGHTS_CREDITS_SONG = 36;

        /// <summary>Port addition: the Peggle Dual Shot levels' two songs (one picked at random per level).</summary>
        public const int DUALSHOT_SONG_1 = 37;

        public const int DUALSHOT_SONG_2 = 38;

        /// <summary>Port addition: the opening PlayOpeningMusic is waiting on (-1 for none), the loop that follows it, and whether it has started.</summary>
        private int mOpeningSong = -1;

        private int mOpeningLoopSong;

        private bool mOpeningStarted;

        /// <summary>Port addition: plays a loading screen's opening once, then theLoopSongId on loop.</summary>
        public void PlayOpeningMusic(int theSongId, int theLoopSongId)
        {
            PlaySong(theSongId, false);
            mOpeningSong = theSongId;
            mOpeningLoopSong = theLoopSongId;
            mOpeningStarted = false;
        }

        /// <summary>Port addition: starts the opening's loop once the opening has played out; any other song played first drops the wait.</summary>
        private void UpdateOpeningMusic()
        {
            if (mCurMusic != mOpeningSong)
            {
                mOpeningSong = -1;
                return;
            }
            if (mMusicInterface.IsPlaying(mOpeningSong))
            {
                mOpeningStarted = true;
            }
            else if (mOpeningStarted)
            {
                mOpeningSong = -1;
                PlaySong(mOpeningLoopSong, true);
            }
        }

        /// <summary>
        /// Port addition: swaps in the Peggle Nights soundtrack. The opening and menu songs follow the
        /// main menu's look (mNightsMode); the level songs and 12 follow the level pack being played.
        /// </summary>
        private int GetNightsSong(int theSongId)
        {
            if (theSongId < 2 || theSongId > 14)
            {
                return theSongId;
            }
            bool nights = theSongId == 2 || theSongId == 13 ? mNightsMode : mGameLevelPack == 1;
            return nights ? theSongId + NIGHTS_SONG_OFFSET : theSongId;
        }

        /// <summary>Port addition: the Nights progress screen plays its own song (Nights 12) instead of the menu song.</summary>
        public void PlayProgressMusic()
        {
            mMenuMusicPlaying = false;
            PlayMusic(14 + NIGHTS_SONG_OFFSET, true);
        }

        public void PlayMusic(int theSongId, bool loop)
        {
            PlaySong(GetNightsSong(theSongId), loop);
        }

        /// <summary>Port: PlayMusic without the Nights soundtrack swap.</summary>
        private void PlaySong(int theSongId, bool loop)
        {
            int prev = mCurMusic;
            mCurMusic = theSongId;
            if (prev == theSongId && mRestartSameSong)
            {
                // Port addition: the music interface ignores a request for the song already playing.
                mMusicInterface.StopMusic(0f);
            }
            else if (prev != theSongId)
            {
                Debug.OutputDebug(string.Format("Music: song {0} -> {1}", prev, theSongId));
                mMusicInterface.StopMusic(0.5f);
            }
            mMusicInterface.PlayMusic(theSongId, loop);
        }

        public void SetMusicSpeed(float theSpeed)
        {
            // The iOS build computes a playback rate here but never applies it.
        }

        public override void UpdateFrames()
        {
            base.UpdateFrames();
            mWidgetMover.Update();
            if (!mWidgetMover.IsRemovingAny())
            {
                mResourceManager.DoDeferredDeletes();
            }
            if (mLanguageChangePending)
            {
                mLanguageChangePending = false;
                ChangeLanguage();
            }
            if (mResultsMusicPending)
            {
                UpdateResultsMusic();
            }
            if (UpdateChecker.HasUpdate)
            {
                CheckUpdatePrompt();
            }
            if (mOpeningSong != -1)
            {
                UpdateOpeningMusic();
            }
#if PEGGLE_IPHONE
            UpdateLayoutWidth();
#endif
            UpdateMusic();
        }

#if PEGGLE_IPHONE
        /// <summary>
        /// Port addition (desktop window): switches between the 1136 layout (window 16:9 or wider) and
        /// the 960 one (narrower), see Constants.ChooseScreenWidth(). Everything is laid out for the
        /// gutters when it is built, so the switch waits for a moment when only the loading screen, the
        /// main menu and/or a level is up (no dialogs, nothing sliding); until then the current layout letterboxes or gets
        /// the ultrawide margins as before.
        /// </summary>
        private void UpdateLayoutWidth()
        {
            ScreenScales s = mScreenScales;
            if (!Constants.DynamicScreenWidth || s.mWidth <= 0 || s.mHeight <= 0)
            {
                return;
            }
            int aWidth = Constants.GetWindowLayoutWidth(s.mWidth, s.mHeight);
            if (aWidth == mWidth || !CanChangeLayoutWidth())
            {
                return;
            }
            Constants.SetScreenWidth(aWidth);
            mWidth = aWidth;
            SexyUtil.gGutterWidth = (aWidth - Constants.IPHONE_MIN_SCREEN_WIDTH) / 2;
            mWidgetManager.Resize(mWidth, mHeight);
            s.Init(s.mWidth, s.mHeight, Constants.ScreenWidth, Constants.ScreenHeight);
            Graphics.Resized();
            if (mLoadingScreen != null && mLoadingScreen.mWidgetManager != null)
            {
                mLoadingScreen.UpdateLayoutWidth();
            }
            if (mMainMenu != null && mMainMenu.mWidgetManager != null)
            {
                // Rebuilt like a language change: the menu places its buttons for the gutters.
                CleanupScreen(mMainMenu);
                mMainMenu = new MainMenu(this);
                AddScreen(mMainMenu);
            }
            if (mBoard != null)
            {
                mBoard.SetPadLayout(!IsLeftHanded());
                if (mBoard.mReplayDialog.mVisible)
                {
                    mBoard.PositionReplayDialog();
                }
            }
        }

        /// <summary>Port addition: true when only screens UpdateLayoutWidth can lay out again are up.</summary>
        private bool CanChangeLayoutWidth()
        {
            // Any other screen up (the loading screen, level select...) shows in the widget check below.
            if (mDialogList.Count != 0 || mWidgetMover.mWidgets.Count != 0 || mLanguageChangePending)
            {
                return false;
            }
            foreach (Widget aWidget in mWidgetManager.mWidgets)
            {
                bool aKnown = aWidget == mMainMenu || aWidget == mLoadingScreen ||
                    (mBoard != null && (aWidget == mBoard || aWidget == mBoard.mDialWidget || aWidget == mBoard.mReplayDialog));
                if (!aKnown)
                {
                    return false;
                }
            }
            return true;
        }
#endif

        public override void ReadFromRegistry()
        {
            base.ReadFromRegistry();
            int feverVolume = 85;
            RegistryReadInteger("FeverVolume", ref feverVolume);
            mFeverVolume = feverVolume * 0.01;
            int maxStage = 0;
            int maxLevel = 0;
            int nextReplayId = 0;
            RegistryReadInteger("MaxStage", ref maxStage);
            RegistryReadInteger("MaxLevel", ref maxLevel);
            RegistryReadInteger("NextReplayId", ref nextReplayId);
            if (maxStage < mMaxStage)
            {
                maxStage = mMaxStage;
            }
            else if (maxStage == mMaxStage && maxLevel < mMaxLevel)
            {
                maxLevel = mMaxLevel;
            }
            mMaxStage = maxStage;
            mMaxLevel = maxLevel;
            mNextReplayId = nextReplayId;
            mLocalTime = DateTime.Now;
            mFirstLaunchDayPassed = false;
            RegistryReadInteger("FirstLaunchTimeHour", ref mFirstLaunchHour);
            RegistryReadInteger("FirstLaunchTimeDay", ref mFirstLaunchDay);
            RegistryReadInteger("FirstLaunchTimeMonth", ref mFirstLaunchMonth);
            RegistryReadInteger("FirstLaunchTimeYear", ref mFirstLaunchYear);
            int yday = mLocalTime.DayOfYear - 1;
            int year = mLocalTime.Year - 1900;
            if (mFirstLaunchHour == 0 && mFirstLaunchDay == 0 && mFirstLaunchMonth == 0 && mFirstLaunchYear == 0)
            {
                RegistryWriteInteger("FirstLaunchTimeHour", mLocalTime.Hour);
                RegistryWriteInteger("FirstLaunchTimeDay", yday);
                RegistryWriteInteger("FirstLaunchTimeMonth", mLocalTime.Month - 1);
                RegistryWriteInteger("FirstLaunchTimeYear", year);
            }
            else
            {
                int hours = (mLocalTime.Hour - mFirstLaunchHour) + (yday - mFirstLaunchDay) * 24 + (year - mFirstLaunchYear) * 8760;
                mFirstLaunchDayPassed = hours >= 24;
            }
        }

        public override void WriteToRegistry()
        {
            base.WriteToRegistry();
            RegistryWriteInteger("FeverVolume", (int)(mFeverVolume * 100.0));
            RegistryWriteInteger("MaxStage", mMaxStage);
            RegistryWriteInteger("MaxLevel", mMaxLevel);
            RegistryWriteInteger("NextReplayId", mNextReplayId);
            if (mPlayerInfo != null)
            {
                RegistryWriteString("CurUser", mPlayerInfo.mName);
                mPlayerInfo.SaveIfDirty();
            }
        }

        public void MakeFeverStars()
        {
            for (int i = 0; i < 12; i++)
            {
                mFeverStars[i] = Res.GetImageById((ResourceId)((int)ResourceId.IMAGE_FEVERSTAR00_ID + i));
            }
        }

        public bool SetColorblind(bool colorblind)
        {
            bool changed = colorblind != mColorblind;
            if (changed)
            {
                mColorblind = colorblind;
            }
            return changed;
        }

        private string[] GetLoadingThreadGroups()
        {
#if PEGGLE_IPHONE
            return new[]
            {
                "Game", "LevelSelect", "CharSelect", "CharInGame", "Dialog",
                mNightsMode ? "MainMenuNights" : "MainMenuPeggle", "Interface", "Fever", "MainMenuCommon", "LuckySpin",
                "LoadingThread", "Character"
            };
#else
            return new[]
            {
                "Game", "LevelSelect", "CharSelect", "CharInGame", "Dialog",
                mNightsMode ? "MainMenuNights" : "MainMenuPeggle", "Fever", "MainMenuCommon", "LoadingThread", "Character"
            };
#endif
        }

        /// <summary>
        /// Port addition (lazy loading off): the loading thread extracts its groups without unpacking their
        /// atlases (that happens on the main thread). This unpacks them all when loading is done, as
        /// loading each group lazily would; a hand-picked list missed atlases (HD's CharDialog8888, so
        /// the character dialog found no character images).
        /// </summary>
        public void UnpackLoadingThreadGroups()
        {
            foreach (string aGroup in GetLoadingThreadGroups())
            {
                if (mResourceManager.IsGroupLoaded(aGroup) && !Res.ExtractResourcesByName(mResourceManager, aGroup, true))
                {
                    ShowResourceError(true);
                }
            }
        }

        public override void LoadingThreadProc()
        {
            foreach (bool aStep in LoadingThreadSteps())
            {
            }
        }

        /// <summary>The loading thread's work, yielding after each resource (the web runs it a slice per frame).</summary>
        public override IEnumerable<bool> LoadingThreadSteps()
        {
            string[] groups = GetLoadingThreadGroups();
            int first = mLazyResourceLoading ? 4 : 0;
            mNumLoadingThreadTasks = 0;
            for (int i = first; i < groups.Length; i++)
            {
                mNumLoadingThreadTasks += mResourceManager.GetNumResources(groups[i]);
            }
            for (int i = first; i < groups.Length; i++)
            {
                mResourceManager.StartLoadResources(groups[i]);
                while (mResourceManager.LoadNextResource() && !mShutdown)
                {
                    mCompletedLoadingThreadTasks++;
                    yield return true;
                }
                if (mShutdown)
                {
                    yield break;
                }
                if (mResourceManager.HadError())
                {
                    ShowResourceError(false);
                    mLoadingFailed = true;
                    yield break;
                }
                mResourceManager.SetGroupAsLoaded(groups[i]);
                if (!Res.ExtractResourcesByName(mResourceManager, groups[i], false))
                {
                    ShowResourceError(false);
                    mLoadingFailed = true;
                    yield break;
                }
                yield return true;
            }
            MakeFeverStars();
        }

        public override void LoadingThreadCompleted()
        {
            // The original checked PopCap's content-update server here; not part of this port.
        }

        public override void GotFocus()
        {
            base.GotFocus();
            mHasFocus = true;
            if (mRestoreHiddenDialog != null)
            {
                if (GetDialog(DialogId.DIALOG_GENERIC) == mRestoreHiddenDialog)
                {
                    mRestoreHiddenDialog.mVisible = true;
                }
                mRestoreHiddenDialog = null;
            }
            if (mBoard != null && mBoard.mSuspendCount == 0)
            {
                ResumeAudioSFX();
                PauseBoard(false);
            }
        }

        public void ResumeAudioSFX()
        {
            if (mBoard == null)
            {
                return;
            }
            foreach (Ball ball in mBoard.mBalls)
            {
                if (!ball.mIsPeg)
                {
                    RestartLoopSounds(ball);
                }
            }
            if (mBoard.mGun != null && mBoard.mGun.mBall != null)
            {
                RestartLoopSounds(mBoard.mGun.mBall);
            }
        }

        /// <summary>Re-creates a ball's looping fireball/lightning sounds after the audio was suspended.</summary>
        private void RestartLoopSounds(Ball theBall)
        {
            if (theBall.mLightningSound != null)
            {
                theBall.mLightningSound = mSoundManager.GetSoundInstance((uint)Res.SOUND_LIGHTNING_LOOP);
                theBall.mLightningSound?.Play(true, true);
            }
            if (theBall.mFireballSound != null)
            {
                theBall.mFireballSound = mSoundManager.GetSoundInstance((uint)Res.SOUND_FIREBALLLOOP);
                theBall.mFireballSound?.Play(true, true);
            }
        }

        public void PauseBoard(bool pause)
        {
            mBoard?.Pause(pause);
        }

        public override void LostFocus()
        {
            base.LostFocus();
            if (mBoard != null && mBoard.mSuspendCount == 0)
            {
                PauseBoard(true);
            }
#if PEGGLE_IPHONE
            // The iPhone build only pauses here; AppEnteredBackground does the saving.
            return;
#endif
            WriteRestoreInfo();
            if (mReadFromRegistry)
            {
                WriteToRegistry();
            }
            if (GetDialog(DialogId.DIALOG_OPTIONS) != null)
            {
                CommitSettings();
            }
            mPlayerInfo?.SaveDetails();
            if (mBoard != null && mGameMode != GameMode.GAMEMODE_REPLAY && !mBoard.mIsReplay)
            {
                mBoard.SaveGame();
            }
        }

        public void WriteRestoreInfo()
        {
            if (mBoard != null)
            {
                mBoard.mFFwdPressed = false;
            }
            Dialog generic = GetDialog(DialogId.DIALOG_GENERIC);
            if (generic != null)
            {
                // Dialog id 1 is also the CharacterDialog. The original hid it for good, which on PC
                // (where focus comes and goes) left the character select gone; GotFocus restores it.
                generic.mVisible = false;
                mRestoreHiddenDialog = generic;
            }
            mSoundManager.StopAllSounds();
            int location = 0;
            if (mBoard != null)
            {
                location = 1;
                RegistryWriteInteger("RestoreGameMode", mGameMode);
            }
            RegistryWriteInteger("RestoreLocation", location);
            RegistryWriteInteger("RestoreGameLevelPack", mGameLevelPack);
            RegistryWriteString("RestoreBonusAdventure", mBonusAdventure?.mAdventureKey ?? "");
            RegistryWriteInteger("mTwoPlayerWins0", mTwoPlayerWins[0]);
            RegistryWriteInteger("mTwoPlayerWins1", mTwoPlayerWins[1]);
            RegistryWriteInteger("mTwoPlayerFevers0", mTwoPlayerFevers[0]);
            RegistryWriteInteger("mTwoPlayerFevers1", mTwoPlayerFevers[1]);
            RegistryWriteInteger("mTwoPlayerPoints0", mTwoPlayerPoints[0]);
            RegistryWriteInteger("mTwoPlayerPoints1", mTwoPlayerPoints[1]);
            RegistryWriteInteger("mTwoPlayerWinner", mTwoPlayerWinner);
            RegistryWriteInteger("mTwoPlayerWinStreak", mTwoPlayerWinStreak);
        }

        public void CommitSettings()
        {
            OptionsDialog options = (OptionsDialog)GetDialog(DialogId.DIALOG_OPTIONS);
            if (options == null)
            {
                return;
            }
            if (mPlayerInfo != null)
            {
                mPlayerInfo.SetColorblind(options.mColorblindCheckbox.IsChecked());
                mPlayerInfo.SetLeftHanded(options.mLeftHandedCheckbox.IsChecked());
                mPlayerInfo.SaveIfDirty();
            }
            if (SetColorblind(options.mColorblindCheckbox.IsChecked()) && mBoard != null)
            {
                Board.SyncColorblind();
            }
            if (mBoard != null)
            {
                mBoard.SetPadLayout(!IsLeftHanded());
                if (mBoard.mReplayDialog.mVisible)
                {
                    mBoard.PositionReplayDialog();
                }
            }
        }

        public void ResetTwoPlayerStats()
        {
            mTwoPlayerWins[0] = mTwoPlayerWins[1] = 0;
            mTwoPlayerFevers[0] = mTwoPlayerFevers[1] = 0;
            mTwoPlayerPoints[0] = mTwoPlayerPoints[1] = 0;
            mTwoPlayerWinner = -1;
            mTwoPlayerWinStreak = 0;
            mGameStats[0].Reset();
            mGameStats[1].Reset();
        }

        public LevelInfo GetCurLevelInfo()
        {
            StageInfo stage = mStageMgr.GetStageInfo(mCurStage);
            return stage?.GetLevelInfo(mCurLevel);
        }

        public int GetMaxLevel()
        {
            return mPlayerInfo == null ? 0 : mPlayerInfo.GetMaxLevel();
        }

        public int GetCurLevel()
        {
            return mCurLevel + mCurStage * 5;
        }

        public bool OnMaxLevel()
        {
            return mPlayerInfo != null && mPlayerInfo.GetMaxLevel() == GetCurLevel();
        }

        public void IncLevel(int theInc)
        {
            StageInfo stage = mStageMgr.GetStageInfo(mCurStage);
            if (stage == null)
            {
                return;
            }
            if (mBoard != null)
            {
                mBoard.mLevelPlayCount = 0;
            }
            mCurLevel += theInc;
            mPlayerInfo?.SetmNeedShowStory(false);
            if (mCurLevel < stage.mLevels.Count)
            {
                if (mCurLevel < 0)
                {
                    if (mCurStage < 1)
                    {
                        mCurLevel -= theInc;
                    }
                    else
                    {
                        mCurStage--;
                        StageInfo prev = mStageMgr.GetStageInfo(mCurStage);
                        if (prev != null)
                        {
                            mCurLevel = prev.mLevels.Count - 1;
                        }
                    }
                }
            }
            else if (mPlayerInfo == null || mPlayerInfo.mLastAdventureStage < mCurStage)
            {
                mCurLevel -= theInc;
            }
            else
            {
                mCurLevel = 0;
                mCurStage++;
                mPlayerInfo?.SetmNeedShowStory(true);
            }
            if (mPlayerInfo == null)
            {
                return;
            }
            mPlayerInfo.SetmAdventureLevelReplay(0);
            if (mPlayerInfo.GetmMaxStage() < mCurStage)
            {
                mPlayerInfo.SetmMaxStage(mCurStage);
                mPlayerInfo.SetmMaxLevel(0);
                if (mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
                {
                    mPlayerInfo.mCurCharacter = SexyUtil.GetAdventureCharacter(mCurStage - mPlayerInfo.mFirstAdventureStage, mGameLevelPack);
                }
            }
            if (mPlayerInfo.mLastAdventureStage < mCurStage && mPlayerInfo.GetmAdventureWinCount() < 1000000)
            {
                mPlayerInfo.SetmAdventureWinCount(mPlayerInfo.GetmAdventureWinCount() + 1);
            }
            if (mCurStage == mPlayerInfo.GetmMaxStage() && mPlayerInfo.GetmMaxLevel() < mCurLevel)
            {
                mPlayerInfo.SetmMaxLevel(mCurLevel);
            }
            mPlayerInfo.SetmLastStageLevel(mCurLevel);
            mPlayerInfo.SetmLastStageStage(mCurStage);
            CheckMaxStage();
            mPlayerInfo.SaveDetails();
        }

        public override Dialog NewDialog(int theDialogId, bool isModal, string theDialogHeader, string theDialogLines, string theDialogFooter, int theButtonMode)
        {
            ThunderDialog dialog = new ThunderDialog(theDialogId, isModal, theDialogHeader, theDialogLines, theDialogFooter, theButtonMode);
            SexyUtil.PositionDialog(dialog, DIALOG_DEFAULT_WIDTH, false, -1);
            return dialog;
        }

        public void CheckScrollOff(Widget theWidget, int theScroll, bool remove = true)
        {
            if (theWidget != null)
            {
                mWidgetMover.ScrollOff(theWidget, theScroll, remove);
                if (theScroll != 0)
                {
                    mScrollDirection = theScroll;
                }
            }
        }

        public void ScrollOn(Widget theWidget)
        {
            if (mScrollDirection != 0)
            {
                mWidgetMover.ScrollOn(theWidget, mScrollDirection);
            }
        }

        public void CleanupScreen(Widget theWidget)
        {
            if (theWidget != null && !mWidgetMover.WillRemove(theWidget))
            {
                mWidgetMover.RemoveWidget(theWidget);
                mWidgetManager.RemoveWidget(theWidget);
                SafeDeleteWidget(theWidget);
            }
        }

        private void AddScreen(Widget theScreen, int theX = 0)
        {
            theScreen.Resize(theX, 0, mWidth, mHeight);
            mWidgetManager.AddWidget(theScreen);
            mWidgetManager.SetFocus(theScreen);
        }

        public void ShowBoard(bool resetBoard, bool scrollOn, bool fromStory, bool restoring)
        {
            CheckScrollOff(mLoadingScreen, 1);
            CheckScrollOff(mMainMenu, 1);
            CheckScrollOff(mLevelScreen, 1);
            CheckScrollOff(mTrophyScreen, 1);
            CheckScrollOff(mHelpScreen, 1);
            CheckScrollOff(mStoryScreen, fromStory ? 5 : 1);
            CheckScrollOff(mAdventureScreen, 1);
            CheckScrollOff(mTrophyRoom, 1);
            StoryScreen story = mStoryScreen;
            if (fromStory)
            {
                mWidgetManager.SetFocus(mStoryScreen);
                mStoryScreen.StartDreamTransition();
            }
            CleanupScreens(true, true);
            if (mPlayerInfo != null)
            {
                SetColorblind(mPlayerInfo.mColorblind);
            }
            mBoard = new Board(this);
            mWidgetManager.AddWidget(mBoard);
            if (!fromStory)
            {
                mWidgetManager.SetFocus(mBoard);
                ScrollOn(mBoard);
            }
            if (scrollOn)
            {
                mBoard.Reset();
            }
            if (fromStory)
            {
                mWidgetManager.AddWidget(story);
                mBoard.mDreamTransitionCounter = (int)(story.mDreamTransitionPos * 100.0 + -50.0);
            }
#if !PEGGLE_IPHONE
            if (restoring)
            {
                mBoard.mReplayButtonState = 1;
            }
#endif
        }

        public void ViewReplaysWithTip()
        {
            if (!DoTipDialog(9))
            {
                CheckScrollOff(mMainMenu, 1);
                CleanupScreens(true, true);
                ViewReplays();
            }
        }

        public bool DoTipDialog(int theTip)
        {
            if (mPlayerInfo == null)
            {
                return false;
            }
            if (!mPlayerInfo.SetTip(theTip) && theTip != 11)
            {
                if (theTip != 9 || DoReplaysExist())
                {
                    return false;
                }
            }
            KillDialog(DialogId.DIALOG_TIP);
            ThunderDialog dialog = null;
            if (theTip == 11)
            {
                dialog = DoDialogScroll(DialogId.DIALOG_TIP, true, Loc("Trial Version"),
                    Loc("The rest of Adventure mode is locked in this free trial version. You can continue playing ^00FF00^Quick Play^oldclr^ and ^00FF00^Duel^oldclr^ modes.\n\nBuy Peggle now to instantly unlock the game and continue your adventure!"),
                    Loc("Close"), 3);
            }
            else if (theTip == 10)
            {
                dialog = DoDialogScroll(DialogId.DIALOG_TIP, true, Loc("Demo"),
                    Loc("Watch how a Peggle Master might play a level. It's more entertaining than a cup of cocoa and a fishtank on a cold winter's day!"),
                    Loc("Ok"), 3);
            }
            else if (theTip == 9)
            {
                if (!DoReplaysExist())
                {
                    dialog = DoDialogScroll(DialogId.DIALOG_TIP, true, Loc("Replays"),
                        Loc("When you make a good shot you can save it using the ^00FF00^Camera^oldclr^ icon on the bottom right of the screen. You can then view them here or show them to a friend!\n\nYou don't have any saved replays, save one while playing the game!"),
                        Loc("Ok"), 3);
                }
                else
                {
                    dialog = DoDialogScroll(DialogId.DIALOG_TIP, true, Loc("Replays"),
                        Loc("When you make a good shot you can save it using the ^00FF00^Camera^oldclr^ icon on the bottom right of the screen. You can then view them here or show them to a friend!"),
                        Loc("Ok"), 3);
                }
                SexyUtil.PositionDialog(dialog, DIALOG_REPLAY_TIP_WIDTH, false, -1);
                dialog.DoScroll(1);
            }
            if (dialog == null)
            {
                return false;
            }
            dialog.mTipId = theTip;
            dialog.mCloseOnButton = true;
            dialog.mAutoSizeYesButton = true;
            return true;
        }

        public void ViewReplays()
        {
            if (mPlayerInfo != null)
            {
                SetColorblind(mPlayerInfo.mColorblind);
            }
            mGameMode = GameMode.GAMEMODE_REPLAY;
            DoReplayFileDialog(true, true);
        }

        /// <summary>Rewrites a replay file's header with a new caption (current format 0x57), keeping its time and data.</summary>
        public bool ChangeReplayCaption(string theFileName, string theCaption)
        {
            Buffer aBuffer = new Buffer();
            if (!ReadBufferFromFile(theFileName, ref aBuffer, true))
            {
                return false;
            }
            DataReader aReader = new DataReader();
            aReader.OpenMemory(aBuffer.GetDataPtr(), aBuffer.GetDataLen(), false);
            int aTime;
            if (aReader.ReadLong() < 0x54)
            {
                string aFullPath = System.IO.Path.Combine(applicationStoragePath, theFileName);
                aTime = (int)new DateTimeOffset(System.IO.File.GetLastWriteTimeUtc(aFullPath)).ToUnixTimeSeconds();
            }
            else
            {
                aReader.ReadWString();
                aTime = aReader.ReadLong();
            }
            aReader.ReadShort();
            int aLen = aReader.ReadLong();
            if (!aReader.CanReadBytes(aLen))
            {
                return false;
            }
            DataWriter aWriter = new DataWriter();
            aWriter.OpenMemory(0x20);
            aWriter.WriteLong(0x57);
            aWriter.WriteWString(theCaption);
            aWriter.WriteLong(aTime);
            aWriter.WriteShort(1);
            aWriter.WriteLong(aLen);
            aWriter.WriteBytes(aReader.ReadBytesFromMem(aLen), 0, aLen);
            WriteBytesToFile(theFileName, aWriter.GetDataPtr(), aWriter.GetDataLen());
            return true;
        }

        public void DoReplayFileDialog(bool theFirstTime, bool theFresh = false)
        {
#if PEGGLE_IPHONE
            PlayMenuMusic();
            CleanupScreens(true, true);
            ReplayFileDialog dialog = new ReplayFileDialog(this, theFresh);
            SexyUtil.PositionDialog(dialog, SexyUtil.gGutterWidth * 2 + SCREEN_WIDTH, false, -1);
#else
            if (!mMenuMusicPlaying)
            {
                PlayMenuMusic();
            }
            CleanupScreens(true, true);
            ReplayFileDialog dialog = new ReplayFileDialog(this);
            SexyUtil.PositionDialog(dialog, SCREEN_WIDTH, false, -1);
#endif
            AddDialog(dialog.mId, dialog);
        }

        public void ShowReplay(bool killDialogs)
        {
            if (mPlayerInfo == null || mHelpScreen != null || !mLoaded)
            {
                return;
            }
            if (mProgressScreen != null && (mProgressScreen.GetCloseTo() == 4 || mProgressScreen.GetCloseTo() == 3))
            {
                return;
            }
            if (killDialogs)
            {
                while (mDialogMap.Count > 0)
                {
                    int id = -1;
                    foreach (int key in mDialogMap.Keys)
                    {
                        id = key;
                        break;
                    }
                    if (id == DialogId.DIALOG_TIP)
                    {
                        return;
                    }
                    KillDialog(id);
                }
            }
            if (mDialogMap.Count > 0)
            {
                bool handled = false;
                if (mDialogMap.Count == 1 && mDialogMap.ContainsKey(DialogId.DIALOG_REPLAY_FILE))
                {
                    KillDialog(DialogId.DIALOG_REPLAY_FILE);
                    PauseBoard(false);
                    handled = true;
                    if (!mBoard.mVisible)
                    {
                        CheckScrollOff(mBoard.mApp.mMainMenu, 1);
                        CleanupScreens(false, true);
                        mBoard.SetVisible(true);
                        mBoard.PlayMusic();
                    }
                }
                if (!handled)
                {
                    return;
                }
            }
            if (!killDialogs && mMainMenu == null && mLoadingScreen == null && mBoard == null && mLevelScreen == null && mTrophyScreen == null)
            {
                return;
            }
            if (mBoard == null)
            {
                if (mPlayerInfo != null)
                {
                    SetColorblind(mPlayerInfo.mColorblind);
                }
                mGameMode = GameMode.GAMEMODE_REPLAY;
                mBoard = new Board(this);
                mBoard.Resize(SexyUtil.gGutterWidth, 0, mWidth, mHeight);
                mWidgetManager.AddWidget(mBoard);
                mBoard.Reset();
            }
            else if (mGameMode != GameMode.GAMEMODE_REPLAY)
            {
                if (mBoard.NeedSaveGame())
                {
                    mBoard.SaveGame();
                }
                mGameMode = GameMode.GAMEMODE_REPLAY;
                mBoard.mLevelName = "";
                mBoard.Reset();
            }
            if (!mBoard.LoadReplayFile(mReplayFileName))
            {
                ShowMainMenu();
                return;
            }
            GetImageMgr()?.Clear();
            mBoard.mReplayStarted = true;
            mWidgetManager.RemoveWidget(mBoard.mReplayDialog);
            mBoard.mReplayDialog.CancelScroll();
            mBoard.mReplayDialog.SetMode(mBoard.mIsReplay, mBoard.mReplayWriters.Count > 1, true);
            mBoard.ShowReplay(0, false);
            mBoard.mReplayDialog.mReplayIndex = 0;
            CleanupScreens(false, true);
        }

        public void ShowMainMenu()
        {
            mSoundManager.StopAllSounds();
            if (!mMenuMusicPlaying)
            {
                StopMusic();
            }
            ResetTwoPlayerStats();
            if (mMainMenu == null)
            {
                mScrollDirection = 3;
                CheckScrollOff(mLoadingScreen, 1);
                CheckScrollOff(mLevelScreen, 2);
                CheckScrollOff(mTrophyScreen, 2);
                CheckScrollOff(mStoryScreen, 2);
                CheckScrollOff(mWoWCutscene, 2);
                CheckScrollOff(mNightsEnding, 2);
                CheckScrollOff(mAdventureScreen, 2);
                CheckScrollOff(mTrophyRoom, 2);
                CheckScrollOff(mMoreGamesScreen, 2);
                CheckScrollOff(mCreditsScreen, 2);
                CheckScrollOff(mBoard, 2);
                CheckScrollOff(mProgressScreen, 2);
                ThunderDialog options = (ThunderDialog)GetDialog(DialogId.DIALOG_OPTIONS);
                if (options != null)
                {
                    options.mScrollDX = 40;
                }
                CleanupScreens(true, true);
                EndBonusAdventure();
                FinishOptionsDialog(true, true);
                mMainMenu = new MainMenu(this);
                AddScreen(mMainMenu);
                ScrollOn(mMainMenu);
                BringDialogsToFront();
                if (mPlayerInfo == null)
                {
                    DoCreateUserDialog();
                }
            }
            else
            {
                MainMenu menu = mMainMenu;
                mMainMenu = null;
                CleanupScreens(true, true);
                EndBonusAdventure();
                mMainMenu = menu;
                PlayMainMenuMusic(mMainMenu.MusicNights, mMainMenu.mWoW);
                mWidgetManager.SetFocus(mMainMenu);
                mMainMenu.mUpdateCnt = 0;
            }
        }

        /// <summary>
        /// Port addition (launch target): where the app that started the game asked it to open (GameActivity reads
        /// the intent extra "peggle_target"): "peggle" and "nights" (their Adventures), "extreme" and "wow" (the bonus
        /// Adventures), "dualshot" (Quick Play on the Dual Shot page; it has no Adventure). Null when the game was
        /// started on its own, as from the home screen: then nothing changes.
        /// </summary>
        public static string sLaunchTarget;

        /// <summary>Port addition (launch target): the next StartAdventureGame goes to the board without the story screen.</summary>
        public static bool sLaunchSkipStory;

        private void LaunchAdventure(int theLevelPack, BonusPage theAdventure)
        {
            int aMode = theLevelPack == 1 ? GameMode.GAMEMODE_ADVENTURE_NIGHTS : GameMode.GAMEMODE_ADVENTURE;
            SetLevelPack(theLevelPack, theAdventure);
            mGameMode = aMode;
            // The level the Adventure stands at (as StartAdventureGame works it out), which a save must be of.
            mCurStage = SexyUtil.ClampRange(mPlayerInfo.GetmLastStageStage(), mPlayerInfo.mFirstAdventureStage, mPlayerInfo.mLastAdventureStage);
            mCurLevel = SexyUtil.ClampRange(mPlayerInfo.GetmLastStageLevel(), 0, 5);
            if (CheckSaveGame(false, true))
            {
                return;  // the saved level, loaded (FinishContinueDialog)
            }
            sLaunchSkipStory = true;
            StartAdventureGame(aMode);
        }

        public void RunLaunchTarget()
        {
            string aTarget = sLaunchTarget;
            sLaunchTarget = null;
            if (mMainMenu == null)
            {
                return;
            }
            // The Adventures go straight to the board: the level in progress if there is one (no "continue?"
            // prompt), else where the player's Adventure stands (1-1 the first time). No story screen between.
            switch (aTarget)
            {
            case "peggle":
            case "nights":
                LaunchAdventure(aTarget == "nights" ? 1 : 0, null);
                break;
            case "extreme":
            case "wow":
                {
                    BonusPage anAdventure = StageMgr.GetAdventurePage(aTarget == "extreme" ? StageMgr.ADVENTURE_EXTREME : StageMgr.ADVENTURE_WOW);
                    if (anAdventure != null)
                    {
                        LaunchAdventure(anAdventure.mLevelPack, anAdventure);
                    }
                    break;
                }
            case "dualshot":
                mGameMode = GameMode.GAMEMODE_QUICKPLAY;
                ShowLevelScreen(false, false, false);
                if (mLevelScreen != null)
                {
                    for (int aPage = 2; aPage < mLevelScreen.GetNumPages(); aPage++)
                    {
                        if (LevelScreen.GetPageLogo(aPage) == GameArrowButton.LOGO_DUALSHOT)
                        {
                            mLevelScreen.ShowPage(aPage);
                            break;
                        }
                    }
                }
                break;
            }
        }

        /// <summary>Port addition: leaves a bonus Adventure (mBonusAdventure), back to its pack's own stage range.</summary>
        public void EndBonusAdventure()
        {
            if (mBonusAdventure != null)
            {
                SetLevelPack(mGameLevelPack);
            }
        }

        public void ShowHelpScreen(bool returnToBoard, bool fromBoard)
        {
            if (mHelpScreen != null)
            {
                return;
            }
            if (mPlayerInfo != null)
            {
                SetColorblind(mPlayerInfo.mColorblind);
            }
            LoadGroup("Help", Res.ExtractHelpResources);
            EndMainMenuAmbience();
            mScrollDirection = 1;
            mHelpScreen = new HelpScreen(this, fromBoard);
            AddScreen(mHelpScreen);
            mHelpReturnsToBoard = returnToBoard;
            ScrollOn(mHelpScreen);
        }

        public void EndHelpScreen()
        {
            if (mBoard != null)
            {
                mBoard.mVisible = true;
            }
            if (mMainMenu != null)
            {
                mMainMenu.mVisible = true;
                mWidgetManager.SetFocus(mMainMenu);
                if (mBoard == null && !mHelpReturnsToBoard)
                {
                    PlayMainMenuMusic(mMainMenu.MusicNights, mMainMenu.mWoW);
                }
            }
            if (!mHelpReturnsToBoard)
            {
                CheckScrollOff(mHelpScreen, 1);
                CleanupScreen(mHelpScreen);
                mHelpScreen = null;
            }
            else
            {
                ShowBoard(true, true, false, false);
            }
        }

        public static int GetMaxUnlockedCharacter()
        {
            return 20;
        }

        public bool TryShowNewTrophy()
        {
            // Port: a bonus Adventure (Extreme) has none of Peggle's trophy stories.
            if (mPlayerInfo == null || mGameLevelPack == 1 || mBonusAdventure != null)
            {
                return false;
            }
            bool acedAll = false;
            int shownStory = mPlayerInfo.mStoryShownLevel;
            int storyLevel = 0;
            if (mPlayerInfo.mAdventureWins > 0)
            {
                storyLevel = 1;
                if (mPlayerInfo.mTrophySet.Count >= TrophyMgr.GetNumTrophiesCurGame())
                {
                    storyLevel = 2;
                    if (mPlayerInfo.mClearedLevelSet.Count >= StageMgr.GetNumLevelsCurGame())
                    {
                        storyLevel = 3;
                    }
                }
                if (mPlayerInfo.mAcedLevelSet.Count >= StageMgr.GetNumLevelsCurGame() && !mPlayerInfo.mShownAceAllStory)
                {
                    acedAll = true;
                    mShowAceAllStory = storyLevel <= shownStory;
                }
            }
            if (shownStory < storyLevel || acedAll)
            {
                ShowStoryScreen(true, false);
                return true;
            }
            return false;
        }

        public void ShowStoryScreen(bool isTrophyStory, bool isNights)
        {
            if (mPlayerInfo != null)
            {
                mPlayerInfo.SetmNeedShowStory(false);
                mPlayerInfo.mDirty = true;
                // Port addition: a bonus Adventure without credits (WoW) has no win screen; its win goes
                // back to the main menu, as the PC game returns to its lounge.
                if (mBonusAdventure != null && mBonusAdventure.mCreditsEnding < 0 && mPlayerInfo.GetmLastStageStage() > mPlayerInfo.mLastAdventureStage)
                {
                    ShowMainMenu();
                    return;
                }
                // Port: the Nights win screen (the diploma) is skipped, straight on to the progress
                // screen its Continue goes to, which leads to the PC ending (NightsEndingScreen).
                if (mBonusAdventure == null && mGameLevelPack == 1 &&
                    (isTrophyStory || mPlayerInfo.GetmLastStageStage() > mPlayerInfo.mLastAdventureStage))
                {
                    ShowProgressScreen(ProgressScreen.CLOSE_TO_MAIN_MENU);
                    return;
                }
            }
            CheckScrollOff(mBoard, 2);
            CheckScrollOff(mMainMenu, 1);
            CheckScrollOff(mWoWCutscene, 1);
            CleanupScreens(true, true);
            mStoryScreen = new StoryScreen(this, isTrophyStory, isNights);
            AddScreen(mStoryScreen);
            ScrollOn(mStoryScreen);
        }

        /// <summary>Port addition: the Peggle WoW stage cutscene (WoWCutscene).</summary>
        public void ShowWoWCutscene()
        {
            CheckScrollOff(mBoard, 2);
            CheckScrollOff(mMainMenu, 1);
            CleanupScreens(true, true);
            mWoWCutscene = new WoWCutscene(this);
            AddScreen(mWoWCutscene);
            ScrollOn(mWoWCutscene);
        }

        /// <summary>
        /// Port addition: the PC Nights ending (0x42f640), once the trophy room's curtain has closed on a
        /// newly won trophy: the credits room for theTrophy and the trophies won so far, in place at once
        /// (no scroll), its curtain down where the trophy room's came to rest.
        /// </summary>
        public void ShowNightsEnding(int theTrophy, int theTrophyCount)
        {
            CleanupScreens(true, true);
            mNightsEnding = new NightsEndingScreen(this, theTrophy, theTrophyCount);
            AddScreen(mNightsEnding);
        }

        public ProgressScreen ShowProgressScreen(int theCloseTo)
        {
#if PEGGLE_IPHONE
            PlayProgressMusic();
            CheckScrollOff(mBoard, 2);
            CheckScrollOff(mMainMenu, 1);
            CheckScrollOff(mAdventureScreen, 1);
            CleanupScreen(mBoard);
            mBoard = null;
            CleanupScreen(mMainMenu);
            mMainMenu = null;
            CleanupScreen(mAdventureScreen);
            mAdventureScreen = null;
            if (mProgressScreen == null)
            {
                CleanupScreens(true, true);
                mProgressScreen = new ProgressScreen(this, theCloseTo);
                AddScreen(mProgressScreen, SexyUtil.gGutterWidth);
                ScrollOn(mProgressScreen);
            }
#else
            PlayProgressMusic();
            CheckScrollOff(mBoard, 2);
            CleanupScreen(mBoard);
            mBoard = null;
            if (mProgressScreen == null)
            {
                CleanupScreens(true, false);
                mProgressScreen = new ProgressScreen(this, theCloseTo);
                AddScreen(mProgressScreen);
                ScrollOn(mProgressScreen);
            }
#endif
            return mProgressScreen;
        }

        /// <summary>Starts the menu theme unless one (Peggle or Nights) is already playing.</summary>
        /// <summary>Port addition: one of the main menu's ambience loops is playing (Main Menu Theme option on Ambience).</summary>
        public bool IsMainMenuAmbiencePlaying()
        {
            return mCurMusic == AMBIENCE_SONG || mCurMusic == NIGHTS_AMBIENCE_SONG || mCurMusic == WOW_AMBIENCE_SONG || mCurMusic == NIGHTS_PC_AMBIENCE_SONG;
        }

        public void PlayMenuMusic()
        {
            if (!mMenuMusicPlaying)
            {
                PlayMenuMusic(mNightsMode);
            }
        }

        /// <summary>
        /// Port: plays the Peggle or the Nights menu theme, switching over when the other one is
        /// playing (the original had only the one menu theme). The main menu uses its own look's
        /// theme; the results screen uses the theme of the level pack just played.
        /// </summary>
        public void PlayMenuMusic(bool nights)
        {
            int song = nights ? 13 + NIGHTS_SONG_OFFSET : 13;
            if (!mMenuMusicPlaying || mCurMusic != song)
            {
                PlaySong(song, true);
                mMenuMusicPlaying = true;
            }
        }

        /// <summary>
        /// Port addition: what the main menu itself (and nothing else) plays: the menu theme, or with
        /// the Main Menu Theme option on Ambience, the Peggle or Nights ambience loop. The ambience
        /// doesn't count as menu music, so the next screen's PlayMenuMusic() switches to the theme.
        /// </summary>
        public void PlayMainMenuMusic(bool nights, bool wow = false)
        {
            if (!mMainMenuAmbience)
            {
                PlayMenuMusic(nights);
                return;
            }
            mMenuMusicPlaying = false;
            PlaySong(wow ? WOW_AMBIENCE_SONG : nights ? (mNightsMenuPC ? NIGHTS_PC_AMBIENCE_SONG : NIGHTS_AMBIENCE_SONG) : AMBIENCE_SONG, true);
        }

        /// <summary>Port addition: a screen opened over the main menu that keeps the music as it is gets the menu theme instead of the ambience.</summary>
        public void EndMainMenuAmbience()
        {
            if (IsMainMenuAmbiencePlaying())
            {
                PlayMenuMusic(mNightsMode);
            }
        }

        public void EndProgressScreen()
        {
            mSoundManager.StopAllSounds();
            ShowStoryScreen(false, false);
        }

        public void ShowLevelScreen(bool noScroll, bool duel, bool newGame)
        {
#if PEGGLE_IPHONE
            PlayMenuMusic();
#else
            if (!mMenuMusicPlaying)
            {
                PlayMenuMusic();
            }
#endif
            if (!noScroll)
            {
                CheckScrollOff(mBoard, 2);
                CheckScrollOff(mMainMenu, 1);
                CheckScrollOff(mTrophyScreen, 1);
            }
            CleanupScreens(true, true);
            mLevelScreen = new LevelScreen(this, duel, newGame);
            AddScreen(mLevelScreen);
            if (!noScroll)
            {
                ScrollOn(mLevelScreen);
            }
        }

        public void StopMusic()
        {
            mMenuMusicPlaying = false;
            mResultsMusicPending = false;
            mMusicInterface.StopMusic(0f);
        }

        public void FinishOptionsDialog(bool commit, bool scrollOff)
        {
            ThunderDialog dialog = (ThunderDialog)GetDialog(DialogId.DIALOG_OPTIONS);
            if (dialog == null)
            {
                return;
            }
            if (commit)
            {
                CommitSettings();
                // Port: save the slider volumes now too, in case the app is killed rather than closed.
                if (mReadFromRegistry)
                {
                    WriteToRegistry();
                }
            }
            if (scrollOff)
            {
                dialog.DoScrollOff(true);
            }
            else
            {
                KillDialog(DialogId.DIALOG_OPTIONS);
            }
            PauseBoard(false);
        }

        public void BringDialogsToFront()
        {
            Widget dialog = null;
            LinkedListNode<Widget> node = mWidgetManager.mWidgets.Last;
            while (node != null)
            {
                if (node.Value is Dialog)
                {
                    dialog = node.Value;
                    break;
                }
                node = node.Previous;
            }
            if (dialog != null)
            {
                mWidgetManager.BringToFront(dialog);
            }
        }

        public void DoCreateUserDialog()
        {
            KillDialog(DialogId.DIALOG_CREATE_USER);
            NewUserDialog dialog = new NewUserDialog(this, false, true, mPlayerInfo != null);
            int width = dialog.mWidth;
            int height = dialog.GetPreferredHeight(width);
            if (mDialogMap.Count > 0)
            {
                SexyUtil.PositionDialog(dialog, width, false, -1);
            }
            else
            {
                dialog.Resize((mWidth - width) / 2, (mHeight - height) / 2, width, height);
            }
            dialog.DoScroll(1);
            AddDialog(DialogId.DIALOG_CREATE_USER, dialog);
        }

        public void DoScrollOff(int theDialogId)
        {
            ThunderDialog dialog = GetDialog(theDialogId) as ThunderDialog;
            dialog?.DoScrollOff(true);
        }

        public ThunderDialog DoDialogScroll(int theDialogId, bool isModal, string theHeader, string theLines, string theFooter, int theButtonMode)
        {
            ThunderDialog dialog = (ThunderDialog)DoDialog(theDialogId, isModal, theHeader, theLines, theFooter, theButtonMode);
            dialog.DoScroll(1);
            return dialog;
        }

        public void StartGame(int theGameMode)
        {
            StartGame(theGameMode, null);
        }

        /// <summary>Port: theAdventure picks a bonus page's Adventure (Extreme, WoW), played as its page's pack.</summary>
        public void StartGame(int theGameMode, BonusPage theAdventure)
        {
            if (theAdventure != null)
            {
                SetLevelPack(theAdventure.mLevelPack, theAdventure);
                mGameMode = theAdventure.mLevelPack == 1 ? GameMode.GAMEMODE_ADVENTURE_NIGHTS : GameMode.GAMEMODE_ADVENTURE;
                mCurStage = theAdventure.mFirstStage;
                mCurLevel = 0;
                if (mPlayerInfo == null || !CheckSaveGame(true, false))
                {
                    mMainMenu.StartGame();
                }
            }
            else if (theGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                SetLevelPack(1);
                mGameMode = GameMode.GAMEMODE_ADVENTURE_NIGHTS;
                mCurStage = mMainMenu.mNightsLastStage;
                mCurLevel = mMainMenu.mNightsLastLevel;
                if (mPlayerInfo == null || !CheckSaveGame(true, false))
                {
                    mMainMenu.StartGame();
                }
            }
            else if (theGameMode == GameMode.GAMEMODE_ADVENTURE)
            {
                SetLevelPack(0);
                mGameMode = GameMode.GAMEMODE_ADVENTURE;
                mCurStage = mMainMenu.mLastStage;
                mCurLevel = mMainMenu.mLastLevel;
                if (mPlayerInfo == null || !CheckSaveGame(true, false))
                {
                    mMainMenu.StartGame();
                }
            }
        }

        public void ShowTrophyScreen(bool newChallenge)
        {
#if PEGGLE_IPHONE
            PlayMenuMusic();
#else
            if (!mMenuMusicPlaying)
            {
                PlayMenuMusic();
            }
#endif
            mStageMgr.ResetLastUseTimes();
            CheckScrollOff(mBoard, 2);
            CheckScrollOff(mLevelScreen, 2);
            CheckScrollOff(mMainMenu, 1);
            CleanupScreens(true, true);
            mTrophyScreen = new TrophyScreen(this, newChallenge);
            AddScreen(mTrophyScreen);
            ScrollOn(mTrophyScreen);
        }

        public void ShowMoreGamesScreen()
        {
            EndMainMenuAmbience();
            CleanupScreens(true, false);
            mMoreGamesScreen = new MoreGamesScreen(this);
            AddScreen(mMoreGamesScreen);
            ScrollOn(mMoreGamesScreen);
        }

        public void ShowCreditsScreen()
        {
            CheckScrollOff(mProgressScreen, 0);
            EndMainMenuAmbience();
            CleanupScreens(true, false);
            mCreditsScreen = new CreditsScreen(this);
            AddScreen(mCreditsScreen);
            ScrollOn(mCreditsScreen);
        }

        public void StartAdventureGame(int theGameMode)
        {
            SetLevelPack(theGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS ? 1 : 0, mBonusAdventure);
            if (mPlayerInfo != null && mPlayerInfo.JustBeatAdventure())
            {
                mPlayerInfo.RestartAdventure();
            }
            mGameMode = theGameMode;
            if (mPlayerInfo == null)
            {
                mCurStage = 0;
                mCurLevel = 0;
            }
            else
            {
                mCurStage = SexyUtil.ClampRange(mPlayerInfo.GetmLastStageStage(), mPlayerInfo.mFirstAdventureStage, mPlayerInfo.mLastAdventureStage);
                mCurLevel = SexyUtil.ClampRange(mPlayerInfo.GetmLastStageLevel(), 0, 5);
            }
            if (sLaunchSkipStory)
            {
                sLaunchSkipStory = false;
                ShowBoard(false, true, false, false);
            }
            else if (mPlayerInfo == null || !mPlayerInfo.GetmNeedShowStory() || mCurLevel != 0)
            {
                ShowBoard(false, true, false, false);
            }
            else if (mBonusAdventure != null && mBonusAdventure.mAdventureKey == StageMgr.ADVENTURE_WOW)
            {
                // Port addition: Peggle WoW's cutscene (the PC game's IntroScreen, which its lounge plays when
                // the adventure is started at a stage's beginning), then straight to the board.
                ShowWoWCutscene();
            }
            else
            {
                ShowStoryScreen(false, false);
            }
        }

        public void SetLevelPack(int theLevelPack)
        {
            SetLevelPack(theLevelPack, null);
        }

        /// <summary>
        /// Port: theAdventure (or null) is the bonus Adventure being played (mBonusAdventure). Its stage
        /// range is its page's Adventure stages, and its last stage is an ordinary one (no Master stage).
        /// </summary>
        public void SetLevelPack(int theLevelPack, BonusPage theAdventure)
        {
            mGameLevelPack = theLevelPack;
            mBonusAdventure = theAdventure;
            if (mPlayerInfo == null)
            {
                return;
            }
            if (theAdventure != null)
            {
                mPlayerInfo.mFirstAdventureStage = theAdventure.mFirstStage;
                mPlayerInfo.mLastAdventureStage = theAdventure.mLastStage;
                mPlayerInfo.mNumAdventureStages = theAdventure.AdventureStages - 1;
            }
            else if (theLevelPack == 1)
            {
                mPlayerInfo.mFirstAdventureStage = 11;
                mPlayerInfo.mLastAdventureStage = 22;
                mPlayerInfo.mNumAdventureStages = 11;
            }
            else
            {
                mPlayerInfo.mFirstAdventureStage = 0;
                mPlayerInfo.mLastAdventureStage = 10;
                mPlayerInfo.mNumAdventureStages = 10;
            }
        }

        public void ShowAdventureScreen(bool fromBoard)
        {
#if PEGGLE_IPHONE
            PlayMenuMusic();
            CheckScrollOff(mMainMenu, 1);
            CheckScrollOff(mTrophyRoom, 2);
            CleanupScreens(true, true);
#else
            if (!mMenuMusicPlaying)
            {
                PlayMenuMusic();
            }
            CleanupScreens(true, false);
#endif
            mAdventureScreen = new AdventureScreen(this, fromBoard);
            AddScreen(mAdventureScreen);
            ScrollOn(mAdventureScreen);
        }

        public void ShowTrophyRoom(int theDestination)
        {
            // Port: the trophy room belongs to Peggle (Nights has the progress screen), so it plays the
            // Peggle menu theme even when the Nights menu (evening) is active.
#if PEGGLE_IPHONE
            PlayMenuMusic(false);
            CheckScrollOff(mBoard, 1);
            CheckScrollOff(mAdventureScreen, 1);
            CheckScrollOff(mStoryScreen, 1);
            CleanupScreens(true, true);
            mTrophyRoom = new TrophyRoom(this, theDestination);
            AddScreen(mTrophyRoom, SexyUtil.gGutterWidth);
#else
            PlayMenuMusic(false);
            CheckScrollOff(mBoard, 1);
            CheckScrollOff(mStoryScreen, 1);
            CleanupScreens(true, false);
            mTrophyRoom = new TrophyRoom(this, theDestination);
            AddScreen(mTrophyRoom);
#endif
            ScrollOn(mTrophyRoom);
        }

        public void DoOptionsDialog()
        {
            PauseBoard(true);
            OptionsDialog dialog = new OptionsDialog(mBoard != null);
#if PEGGLE_IPHONE
            SexyUtil.PositionDialog(dialog, mWidth, false, -1);
#else
            SexyUtil.PositionDialog(dialog, SCREEN_WIDTH, false, -1);
#endif
            dialog.DoScroll(1);
            AddDialog(DialogId.DIALOG_OPTIONS, dialog);
        }

        /// <summary>
        /// Port addition for Android's back button (iOS has none): during play it opens the options
        /// menu, on the main menu it asks to quit. With any dialog up it does nothing.
        /// </summary>
        public override bool BackButtonPress()
        {
            if (base.BackButtonPress() || mDialogMap.Count > 0)
            {
                return true;
            }
            if (mBoard != null && mBoard.mWidgetManager != null)
            {
                DoOptionsDialog();
                return true;
            }
            if (mMoreGamesScreen != null)
            {
                DoToMenu();
                return true;
            }
            if (mMainMenu != null && mMainMenu.mWidgetManager != null)
            {
                DoConfirmQuitDialog();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Port addition (controller): play takes the pad while the board is up with no dialog over it
        /// (Board.GamePadUpdate); elsewhere the pad moves the pointer.
        /// </summary>
        public override bool GamePadUpdate(Microsoft.Xna.Framework.Input.GamePadState thePad, Microsoft.Xna.Framework.Input.GamePadState thePrevPad)
        {
            if (mBoard == null)
            {
                return false;
            }
            if (mBoard.mWidgetManager != null && mDialogMap.Count == 0 && mBoard.GamePadUpdate(thePad, thePrevPad))
            {
                return true;
            }
            mBoard.ReleasePad();
            return false;
        }

        public void DoConfirmQuitDialog()
        {
            ThunderDialog dialog = DoDialogScroll(DialogId.DIALOG_CONFIRM_QUIT, true, Loc("Quit Peggle?"), Loc("Are you sure you want to\nquit the game?"), "", 1);
            // The original moved it to (235, 280), a PC-era spot that is off-centre on this screen;
            // keep NewDialog's centred x and centre it vertically too.
            dialog.mY = (mHeight - dialog.mHeight) / 2;
            dialog.DoScroll(-1);
            dialog.mCloseOnButton = false;
        }

        /// <summary>Port addition: warns that the game closes to change its icon (Extra Options' DONE with a new icon picked).</summary>
        public void DoConfirmAppIconDialog()
        {
            ThunderDialog dialog = DoDialogScroll(DialogId.DIALOG_CONFIRM_APP_ICON, true, Loc("Change Icon?"),
                Loc("The game will close to change its icon.\nAre you sure you want to continue?"), "", 2);
            dialog.mY = (mHeight - dialog.mHeight) / 2;
            dialog.DoScroll(-1);
            dialog.mCloseOnButton = false;
        }

        /// <summary>Port addition: OK applies the icon (closing the game); Cancel goes back to the current icon.</summary>
        /// <summary>Port addition: the release page of the update the prompt offers.</summary>
        private string mUpdateUrl = UpdateChecker.RELEASES_PAGE_URL;

        /// <summary>
        /// Port addition: shows the update prompt (UpdateChecker found a newer release) once the main
        /// menu is settled: it has the focus, nothing is sliding, no other dialog is up (the new-user
        /// certificate, for one) and it has been up a second.
        /// </summary>
        private void CheckUpdatePrompt()
        {
            if (mMainMenu == null || mDialogList.Count != 0 || mWidgetMover.IsRemovingAny() ||
                mWidgetManager.mFocusWidget != mMainMenu || mMainMenu.mUpdateCnt < 100)
            {
                return;
            }
            if (!UpdateChecker.TakeUpdate(out string aNewVersion, out mUpdateUrl))
            {
                return;
            }
            string aText = Loc("A new version of Peggle Enhanced is available!") + "\n\n" +
                Loc("Current version: ") + SexyUtil.GetPortVersionString() + "\n" +
                Loc("New version: ") + aNewVersion;
            ThunderDialog aDialog = (ThunderDialog)DoDialog(DialogId.DIALOG_UPDATE_AVAILABLE, true, Loc("Update Available"), aText, "", 2);
            aDialog.mYesButton.mLabel = Loc("Download");
            aDialog.mNoButton.mLabel = Loc("Cancel");
        }

        /// <summary>Port addition: Download opens the release page; either button closes the prompt.</summary>
        private void FinishUpdateDialog(bool theDownload)
        {
            DoScrollOff(DialogId.DIALOG_UPDATE_AVAILABLE);
            if (theDownload)
            {
                OpenURL(mUpdateUrl);
            }
        }

        public void FinishConfirmAppIconDialog(bool theOk)
        {
            PortOptionsDialog anOptions = GetDialog(DialogId.DIALOG_PORT_OPTIONS) as PortOptionsDialog;
            if (theOk)
            {
                KillDialog(DialogId.DIALOG_CONFIRM_APP_ICON);
                anOptions?.ApplyAppIcon();
            }
            else
            {
                DoScrollOff(DialogId.DIALOG_CONFIRM_APP_ICON);
                anOptions?.RestoreAppIcon();
            }
        }

        public void FinishConfirmQuitDialog(bool quit)
        {
            if (quit)
            {
                KillDialog(DialogId.DIALOG_CONFIRM_QUIT);
                WantsToExit = true;
            }
            else
            {
                DoScrollOff(DialogId.DIALOG_CONFIRM_QUIT);
            }
        }

        public void DoToMenu()
        {
            KillDialog(DialogId.DIALOG_GAME_TIP);
            ShowMainMenu();
        }

        public bool CheckSaveGame(bool askToContinue, bool autoContinue)
        {
            if (mPlayerInfo == null)
            {
                return false;
            }
            string saveName = mPlayerInfo.GetSavedGameName(mGameMode, mGameLevelPack);
            if (string.IsNullOrEmpty(saveName))
            {
                return false;
            }
            mSaveGameBuffer.Clear();
            if (!ReadBufferFromFile(saveName, ref mSaveGameBuffer, false))
            {
                return false;
            }
            DataReader reader = new DataReader();
            reader.OpenMemory(mSaveGameBuffer.GetDataPtr(), mSaveGameBuffer.GetDataLen(), false);
            int version = reader.ReadLong();
            bool valid = reader.ReadBool();
            if (!valid || version > 0x57)
            {
                return false;
            }
            int level = reader.ReadLong();
            int hasShot = reader.ReadLong();
            int score = reader.ReadLong();
            int ballsLeft = reader.ReadLong();
            int challengeId = reader.ReadLong();
            string levelName = reader.ReadString();
            int score2 = 0;
            bool duel = false;
            string name1 = "";
            string name2 = "";
            if (version > 0x33)
            {
                duel = reader.ReadBit();
                reader.EndBit();
                if (duel)
                {
                    score2 = reader.ReadLong();
                    name1 = reader.ReadString();
                    name2 = reader.ReadString();
                }
            }
            string text;
            if (mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                TrophyInfo trophy = mTrophyMgr.GetTrophyInfoById(challengeId);
                if (trophy == null)
                {
                    return false;
                }
                text = Loc("Do you want to ^00FF00^continue^oldclr^ the game in progress or start a ^00FF00^new game^oldclr^?\n\n");
                text += "^44DAFB^";
                text += Loc("Challenge: ") + trophy.mName + "\n";
                if (trophy.mLevels.Count > 1)
                {
                    text += CFormat.StrFormat(Loc("Level: %d/%d\n"), level + 1, trophy.mLevels.Count);
                }
                if (trophy.mBalls > 10)
                {
                    text += CFormat.StrFormat(Loc("Balls Left: %d\n"), ballsLeft);
                }
                text += ScoreText(duel, score, score2, name1, name2);
                mChallengeId = challengeId;
            }
            else if (mGameMode == GameMode.GAMEMODE_QUICKPLAY || (mGameMode == GameMode.GAMEMODE_DUEL && duel))
            {
                LevelInfo levelInfo = mStageMgr.GetLevelInfoByName(levelName);
                if (levelInfo == null)
                {
                    return false;
                }
                mCurStage = levelInfo.mStage;
                mCurLevel = levelInfo.mLevel;
                text = Loc("Do you want to ^00FF00^continue^oldclr^ the game in progress or start a ^00FF00^new game^oldclr^?\n\n");
                text += Loc("^44DAFB^");
                if (!duel)
                {
                    text += Loc("Level: ") + levelInfo.mDisplayName + "\n";
                }
                text += ScoreText(duel, score, score2, name1, name2);
            }
            else if (mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                LevelInfo levelInfo = GetCurLevelInfo();
                if (levelInfo == null || levelInfo.mName != levelName)
                {
                    return false;
                }
                if (hasShot == 0 || !askToContinue)
                {
                    FinishContinueDialog(true);
                    return true;
                }
                text = Loc("Do you want to ^00FF00^continue^oldclr^ the level in progress or ^00FF00^restart^oldclr^ the level?\n\n");
                text += Loc("^44DAFB^");
#if PEGGLE_IPHONE
                // Nights stages (11+) are numbered from 1 again (port: and each bonus page's).
                int aStage = StageMgr.GetStageNumberInPack(levelInfo.mStage);
                text += CFormat.StrFormat(Loc("Stage %d-%d: %S"), aStage, levelInfo.mLevel + 1, levelInfo.mDisplayName);
                text += "\n";
#endif
                text += Loc("Score: ") + SexyUtil.CommaSeperate(score) + "\n";
                text += CFormat.StrFormat(Loc("Balls Left: %d\n"), ballsLeft);
            }
            else
            {
                return false;
            }
            if (autoContinue)
            {
                FinishContinueDialog(true);
                return true;
            }
            text += "\n";
            ContinueDialog dialog = new ContinueDialog(this, text);
            SexyUtil.PositionDialog(dialog, DIALOG_CONTINUE_W, true, 10);
            dialog.DoScroll(1);
            AddDialog(dialog.mId, dialog);
            return true;
        }

        private static string ScoreText(bool duel, int score, int score2, string name1, string name2)
        {
            if (!duel)
            {
                return Loc("Score: ") + SexyUtil.CommaSeperate(score) + "\n";
            }
            return name1 + Loc(" - ") + SexyUtil.CommaSeperate(score) + Loc(" points\n") +
                   name2 + Loc(" - ") + SexyUtil.CommaSeperate(score2) + Loc(" points\n");
        }

        public void FinishContinueDialog(bool continueGame)
        {
            if (mPlayerInfo != null)
            {
                string saveName = mPlayerInfo.GetSavedGameName(mGameMode, mGameLevelPack);
                if (!string.IsNullOrEmpty(saveName))
                {
                    EraseFile(saveName);
                }
            }
            if (!continueGame)
            {
                mMainMenu?.StartGame();
                return;
            }
            if (mGameMode == GameMode.GAMEMODE_QUICKPLAY || mGameMode == GameMode.GAMEMODE_DUEL)
            {
                // Port: Quick Play and Duel keep one save for both level packs, so the pack comes
                // from the saved level (CheckSaveGame set mCurStage), as the level screen does.
                // Otherwise the last pack played stuck, and a Peggle level got Nights music.
                SetLevelPack(StageMgr.GetLevelPackForStage(mCurStage));
            }
            ShowBoard(true, false, false, true);
            if (!mBoard.LoadGame(mSaveGameBuffer))
            {
                ShowMainMenu();
                DoDialogScroll(DialogId.DIALOG_SAVE_GAME_ERROR, true, Loc("Save Game Error"), Loc("The save game file was corrupt."), Loc("OK"), 3);
            }
            else
            {
                mBoard.PlayMusic();
                NoteLevelPlayed(mBoard.mLevelName);
            }
        }

        public void DoConfirmMainMenuDialog()
        {
            bool ask = false;
            if (mBoard != null && mBoard.NeedSaveGame() && mPlayerInfo != null && mPlayerInfo.mLeaveGameWarnings < 3 && mBoard.HasShot())
            {
                ask = true;
            }
            if (!ask)
            {
                DoToMenu();
                return;
            }
            ThunderDialog dialog = (ThunderDialog)DoDialog(DialogId.DIALOG_CONFIRM_MAIN_MENU, true, Loc("Leave Game?"),
                Loc("Your game session will be saved upon leaving.  Do you want to continue?"), "", 2);
            dialog.mYesButton.mLabel = Loc("Leave");
            dialog.mNoButton.mLabel = Loc("Cancel");
            if (GetDialog(DialogId.DIALOG_OPTIONS) != null)
            {
                dialog.Resize(dialog.mX, mHeight / 2 - dialog.mHeight / 2, dialog.mWidth, dialog.mHeight);
            }
            dialog.DoScroll(1);
        }

        public void FinishConfirmMainMenuDialog(bool leave)
        {
            DoScrollOff(DialogId.DIALOG_CONFIRM_MAIN_MENU);
            if (leave)
            {
                if (mPlayerInfo != null && mPlayerInfo.mLeaveGameWarnings < 255)
                {
                    mPlayerInfo.mLeaveGameWarnings++;
                    mPlayerInfo.mDirty = true;
                }
                DoToMenu();
            }
        }

        public void DoConfirmRestartLevelDialog(int theY)
        {
            string header;
            string lines;
            string yes = Loc("Restart");
            if (mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                header = Loc("Restart Challenge?");
                lines = Loc("Are you sure that you want to restart the challenge?");
            }
            else if (mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                header = Loc("Restart Level?");
                lines = Loc("Are you sure that you want to restart the level?");
            }
            else if (mBoard == null || mBoard.NeedSaveGame())
            {
                header = Loc("New Level?");
                lines = Loc("Are you sure that you want to pick a new level?");
                yes = Loc("New Level");
            }
            else
            {
                FinishConfirmRestartLevelDialog(true);
                return;
            }
            ThunderDialog dialog = DoDialogScroll(DialogId.DIALOG_CONFIRM_RESTART_LEVEL, true, header, lines, "", 2);
            dialog.mYesButton.mLabel = yes;
            dialog.mNoButton.mLabel = Loc("Cancel");
            if (theY < 1)
            {
                if (GetDialog(DialogId.DIALOG_OPTIONS) != null)
                {
                    dialog.Resize(dialog.mX, mHeight / 2 - dialog.mHeight / 2, dialog.mWidth, dialog.mHeight);
                }
            }
            else
            {
                dialog.Move(dialog.mX, theY);
            }
            dialog.DoScroll(1);
        }

        public void FinishConfirmRestartLevelDialog(bool restart)
        {
            ThunderDialog dialog = (ThunderDialog)GetDialog(DialogId.DIALOG_CONFIRM_RESTART_LEVEL);
            if (dialog != null)
            {
                DoScrollOff(DialogId.DIALOG_CONFIRM_RESTART_LEVEL);
            }
            if (!restart || mBoard == null)
            {
                return;
            }
            FinishOptionsDialog(true, true);
            if (mGameMode == GameMode.GAMEMODE_QUICKPLAY || mGameMode == GameMode.GAMEMODE_DUEL)
            {
                if (mBoard != null)
                {
                    mBoard.mSaveGameOnRemove = false;
                    mBoard.SubmitTotalStats();
                }
                ShowLevelScreen(false, false, false);
                return;
            }
            if (mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                mBoard.mResetCount = 0;
                mBoard.mTrophyLevelIdx = 0;
            }
            if (dialog != null)
            {
                dialog.mYesButton.mDisabled = true;
                dialog.mNoButton.mDisabled = true;
            }
            mBoard.Reset();
        }

        public void DoConfirmNewChallengeDialog()
        {
            ThunderDialog dialog = (ThunderDialog)DoDialog(DialogId.DIALOG_CONFIRM_NEW_CHALLENGE, true, Loc("New Challenge?"),
                Loc("Are you sure that you want to choose a new challenge? Your progress on this challenge will be lost."), "", 2);
            dialog.mYesButton.mLabel = Loc("New Challenge");
            SexyUtil.PositionDialog(dialog, DIALOG_NEW_CHALLENGE_W, false, -1);
            dialog.DoScroll(1);
        }

        public void FinishConfirmNewChallengeDialog(bool newChallenge)
        {
            DoScrollOff(DialogId.DIALOG_CONFIRM_NEW_CHALLENGE);
            if (newChallenge)
            {
                FinishOptionsDialog(true, true);
                ShowTrophyScreen(false);
                mPlayerInfo.DeleteSavedGame(GameMode.GAMEMODE_CHALLENGE, mGameLevelPack == 1 ? 1 : 0);
            }
        }

        public void DoConfirmRestartAdventureDialog()
        {
            ThunderDialog dialog = (ThunderDialog)DoDialog(DialogId.DIALOG_CONFIRM_RESTART_ADVENTURE, true, Loc("Adventure Complete"),
                Loc("You've already completed the last level in Adventure. Do you want to start a ^00FF00^NEW GAME?^oldclr^"), "", 2);
            dialog.mTextAlign = 0;
            dialog.mYesButton.mLabel = Loc("New Game");
            SexyUtil.PositionDialog(dialog, DIALOG_RESTART_ADVENTURE_W, false, -1);
            dialog.DoScroll(1);
        }

        public void FinishConfirmRestartAdventureDialog(bool restart)
        {
            DoScrollOff(DialogId.DIALOG_CONFIRM_RESTART_ADVENTURE);
            if (restart && mPlayerInfo != null)
            {
                mPlayerInfo.RestartAdventure();
                mMainMenu.StartGame();
            }
            else
            {
                // Port addition: staying on the main menu leaves a bonus Adventure that was picked.
                EndBonusAdventure();
            }
        }

        public void DoUserDialog()
        {
            KillDialog(DialogId.DIALOG_USER);
            UserDialog dialog = new UserDialog(this);
            dialog.mContentInsets.mBottom /= 2;
#if PEGGLE_IPHONE
            SexyUtil.PositionDialog(dialog, mWidth, false, -1);
#else
            SexyUtil.PositionDialog(dialog, SCREEN_WIDTH, false, -1);
#endif
            dialog.DoScroll(1);
            AddDialog(DialogId.DIALOG_USER, dialog);
        }

        public void FinishUserDialog(bool ok)
        {
            UserDialog dialog = (UserDialog)GetDialog(DialogId.DIALOG_USER);
            if (dialog == null)
            {
                return;
            }
            if (ok)
            {
                mPlayerInfo?.SaveIfDirty();
                PlayerInfo selected = mProfileMgr.GetProfile(dialog.GetSelName());
                if (selected != null)
                {
                    mPlayerInfo = selected;
                    mPlayerInfo.UpdatePeggleAcedLevels(this);
                    mWidgetManager.MarkAllDirty();
                    mMainMenu?.SyncPlayerInfo();
                    CheckMaxStage();
                }
            }
            if (mMainMenu != null)
            {
                mWidgetManager.SetFocus(mMainMenu);
            }
            DoScrollOff(DialogId.DIALOG_USER);
        }

        public bool DoReplaysExist()
        {
            string dir = Path.Combine(applicationStoragePath, SexyUtil.GetDocumentsDir() + "userdata", "replays");
            return Directory.Exists(dir) && Directory.GetFiles(dir, "*.peg*").Length > 0;
        }

        public void ShowGameSelectDialog(int theType)
        {
            KillDialog(DialogId.DIALOG_GAME_SELECT);
            GameSelectDialog dialog = new GameSelectDialog(this, "", theType);
#if PEGGLE_IPHONE
            // Full screen width, sliding in sideways like a screen.
            SexyUtil.PositionDialog(dialog, mWidth, true, 0);
            mScrollDirection = 1;
            ScrollOn(dialog);
#else
            SexyUtil.PositionDialog(dialog, DIALOG_DEFAULT_WIDTH, false, -1);
            dialog.DoScroll(1);
#endif
            AddDialog(DialogId.DIALOG_GAME_SELECT, dialog);
        }

        public void FinishGameSelectDialog()
        {
            if (GetDialog(DialogId.DIALOG_GAME_SELECT) != null)
            {
                DoScrollOff(DialogId.DIALOG_GAME_SELECT);
                ShowMainMenu();
            }
        }

        public void FinishTipDialog()
        {
            ThunderDialog dialog = (ThunderDialog)GetDialog(DialogId.DIALOG_TIP);
            if (dialog != null)
            {
                DoScrollOff(DialogId.DIALOG_TIP);
                if (dialog.mTipId == 9 && DoReplaysExist())
                {
                    ViewReplays();
                }
            }
        }

        public void FinishCreateUserDialog(bool ok)
        {
            NewUserDialog dialog = (NewUserDialog)GetDialog(DialogId.DIALOG_CREATE_USER);
            if (dialog == null)
            {
                return;
            }
            string name = dialog.GetName();
            if (string.IsNullOrEmpty(name) && mPlayerInfo != null && !ok)
            {
                DoScrollOff(DialogId.DIALOG_CREATE_USER);
                return;
            }
            string prompt = Loc("Please enter your name to create a new user profile for storing high score data and game progress.");
            if (ok && string.IsNullOrEmpty(name))
            {
                DoDialogScroll(DialogId.DIALOG_CREATE_USER_ERROR, true, Loc("Enter Your Name"), prompt, Loc("OK"), 3);
                return;
            }
            if (mPlayerInfo == null && (!ok || string.IsNullOrEmpty(name)))
            {
                DoDialogScroll(DialogId.DIALOG_CREATE_USER_ERROR, true, Loc("Enter Your Name"), prompt, Loc("OK"), 3);
                return;
            }
            if (!ok)
            {
                DoScrollOff(DialogId.DIALOG_CREATE_USER);
                return;
            }
            PlayerInfo profile = mProfileMgr.AddProfile(name);
            if (profile == null)
            {
                DoDialogScroll(DialogId.DIALOG_CREATE_USER_ERROR, true, Loc("Name Conflict"),
                    Loc("The name you entered is already being used.  Please enter a unique player name."), Loc("OK"), 3);
                return;
            }
            mProfileMgr.Save();
            mPlayerInfo?.SaveIfDirty();
            mPlayerInfo = profile;
            if (string.IsNullOrEmpty(mStatsMgr.mPlayerName) && mPlayerInfo != null)
            {
                mStatsMgr.mPlayerName = mPlayerInfo.mName;
            }
            mWidgetManager.MarkAllDirty();
            if (dialog.mCertificate)
            {
                dialog.DoApproved();
            }
            else
            {
                DoScrollOff(DialogId.DIALOG_USER);
                DoScrollOff(DialogId.DIALOG_CREATE_USER);
            }
            if (mMainMenu != null)
            {
                mMainMenu.SyncPlayerInfo();
                mWidgetManager.SetFocus(mMainMenu);
            }
        }

        public void DoConfirmDeleteUserDialog(string theName)
        {
            KillDialog(DialogId.DIALOG_CONFIRM_DELETE_USER);
            string lines = CFormat.StrFormat(Loc("This will permanently remove '%S' from the player roster!"), theName);
            ThunderDialog dialog = (ThunderDialog)DoDialog(DialogId.DIALOG_CONFIRM_DELETE_USER, true, Loc("Delete User?"), lines, "", 2);
            dialog.DoScroll(1);
            dialog.mYesButton.mLabel = Loc("Delete");
            dialog.mNoButton.mLabel = Loc("Cancel");
        }

        public void FinishConfirmDeleteUserDialog(bool delete)
        {
            DoScrollOff(DialogId.DIALOG_CONFIRM_DELETE_USER);
            if (!delete)
            {
                return;
            }
            UserDialog userDialog = (UserDialog)GetDialog(DialogId.DIALOG_USER);
            if (userDialog == null)
            {
                return;
            }
            string current = mPlayerInfo != null ? mPlayerInfo.mName : "";
            string selected = userDialog.GetSelName();
            if (selected == current)
            {
                mPlayerInfo = null;
            }
            mProfileMgr.DeleteProfile(selected);
            userDialog.FinishDeleteUser();
            if (mPlayerInfo == null)
            {
                mPlayerInfo = mProfileMgr.GetProfile(userDialog.GetSelName()) ?? mProfileMgr.GetAnyProfile();
                mPlayerInfo?.UpdatePeggleAcedLevels(this);
            }
            mProfileMgr.Save();
            if (mPlayerInfo == null)
            {
                DoCreateUserDialog();
            }
            mWidgetManager.MarkAllDirty();
            mMainMenu?.SyncPlayerInfo();
        }

        public void DoRenameUserDialog(string theName)
        {
            KillDialog(DialogId.DIALOG_RENAME_USER);
            NewUserDialog dialog = new NewUserDialog(this, true, false, true);
            int width = dialog.mWidth;
#if PEGGLE_IPHONE
            dialog.Resize((mWidth - width) / 2, 10, width, dialog.GetPreferredHeight(width));
#else
            dialog.Resize((mWidth - width) / 2, RENAME_USER_TEXT_OFFSET_Y, width, dialog.GetPreferredHeight(width));
#endif
            dialog.SetName(theName);
            dialog.DoScroll(1);
            AddDialog(DialogId.DIALOG_RENAME_USER, dialog);
        }

        public void FinishRenameUserDialog(bool ok)
        {
            if (!ok)
            {
                DoScrollOff(DialogId.DIALOG_RENAME_USER);
                return;
            }
            UserDialog userDialog = (UserDialog)GetDialog(DialogId.DIALOG_USER);
            NewUserDialog renameDialog = (NewUserDialog)GetDialog(DialogId.DIALOG_RENAME_USER);
            if (userDialog == null || renameDialog == null)
            {
                return;
            }
            string oldName = userDialog.GetSelName();
            string newName = renameDialog.GetName();
            if (string.IsNullOrEmpty(newName))
            {
                DoScrollOff(DialogId.DIALOG_RENAME_USER);
                return;
            }
            mPlayerInfo?.SaveIfDirty();
            bool wasCurrent = mProfileMgr.GetProfile(oldName) == mPlayerInfo;
            if (!mProfileMgr.RenameProfile(oldName, newName))
            {
                DoDialogScroll(DialogId.DIALOG_RENAME_USER_ERROR, true, Loc("Name Conflict"),
                    Loc("The name you entered is already being used.  Please enter a unique player name."), Loc("OK"), 3);
                return;
            }
            mProfileMgr.Save();
            if (wasCurrent)
            {
                mPlayerInfo = mProfileMgr.GetProfile(newName);
            }
            mPlayerInfo?.UpdatePeggleAcedLevels(this);
            userDialog.FinishRenameUser(newName);
            mWidgetManager.MarkAllDirty();
            DoScrollOff(DialogId.DIALOG_RENAME_USER);
        }

        public void FinishNameErrorDialog(int theDialogId)
        {
            DoScrollOff(theDialogId);
            int ownerId = theDialogId == DialogId.DIALOG_CREATE_USER_ERROR ? DialogId.DIALOG_CREATE_USER : DialogId.DIALOG_RENAME_USER;
            NewUserDialog owner = GetDialog(ownerId) as NewUserDialog;
            if (owner != null)
            {
                mWidgetManager.SetFocus(owner.mNameWidget);
            }
        }

        public void FinishHighScoreEntryDialog(bool ok)
        {
            EditDialog dialog = GetDialog(DialogId.DIALOG_HIGH_SCORE_ENTRY) as EditDialog;
            if (dialog == null)
            {
                return;
            }
            string name = dialog.GetString();
            if (!string.IsNullOrEmpty(name) && name[0] == ' ')
            {
                if (!ok)
                {
                    DoScrollOff(DialogId.DIALOG_HIGH_SCORE_ENTRY);
                }
                return;
            }
            if (!string.IsNullOrEmpty(name) && mBoard != null && ok)
            {
                mBoard.FinishHighScoreEntryDialog(name);
            }
            DoScrollOff(DialogId.DIALOG_HIGH_SCORE_ENTRY);
        }

        public override void DialogButtonDepress(int theDialogId, int theButtonId)
        {
            bool yes = theButtonId == 1000;
            switch (theDialogId)
            {
            case DialogId.DIALOG_CONFIRM_DELETE_USER:
                FinishConfirmDeleteUserDialog(yes);
                break;
            case DialogId.DIALOG_CONFIRM_MAIN_MENU:
                FinishConfirmMainMenuDialog(yes);
                break;
            case DialogId.DIALOG_CONFIRM_QUIT:
                FinishConfirmQuitDialog(yes);
                break;
            case DialogId.DIALOG_CONFIRM_APP_ICON:
                FinishConfirmAppIconDialog(yes);
                break;
            case DialogId.DIALOG_UPDATE_AVAILABLE:
                FinishUpdateDialog(yes);
                break;
            case DialogId.DIALOG_CONFIRM_RESTART_LEVEL:
                FinishConfirmRestartLevelDialog(yes);
                break;
            case DialogId.DIALOG_CONFIRM_NEW_CHALLENGE:
                FinishConfirmNewChallengeDialog(yes);
                break;
            case DialogId.DIALOG_CONFIRM_RESTART_ADVENTURE:
                FinishConfirmRestartAdventureDialog(yes);
                break;
            case DialogId.DIALOG_CREATE_USER:
                FinishCreateUserDialog(yes);
                break;
            case DialogId.DIALOG_CREATE_USER_ERROR:
            case DialogId.DIALOG_RENAME_USER_ERROR:
                FinishNameErrorDialog(theDialogId);
                break;
            case DialogId.DIALOG_GAME_TIP:
                DoScrollOff(theDialogId);
                if (mBoard != null)
                {
                    mBoard.mShowingGameTip = false;
                }
                break;
            case DialogId.DIALOG_TIP:
                FinishTipDialog();
                break;
            case DialogId.DIALOG_HIGH_SCORE_ENTRY:
                FinishHighScoreEntryDialog(yes);
                break;
            case DialogId.DIALOG_OPTIONS:
                FinishOptionsDialog(yes, true);
                break;
            case DialogId.DIALOG_RENAME_USER:
                FinishRenameUserDialog(yes);
                break;
            case DialogId.DIALOG_USER:
                FinishUserDialog(yes);
                break;
            case DialogId.DIALOG_PAUSED:
            case DialogId.DIALOG_PAUSED_2:
                KillDialog(theDialogId);
                if (mBoard != null)
                {
                    mBoard.Pause(false);
                    mWidgetManager.SetFocus(mBoard);
                    if (mBoard.mIsReplay)
                    {
                        mBoard.mApp.DoReplayFileDialog(true);
                    }
                }
                break;
            default:
                DoScrollOff(theDialogId);
                break;
            }
        }

        public override void ButtonDepress(int theId)
        {
            base.ButtonDepress(theId);
            if (theId == 1)
            {
                Shutdown();
            }
        }

        public void PlayLevelMusicTrack()
        {
            PlayLevelMusicTrack(false);
        }

        /// <summary>
        /// Port: theNewLevel is a level starting or restarting. With Random Music, that picks one of the ten
        /// level songs at random (never the one just playing); otherwise the song picked last keeps playing.
        /// </summary>
        public void PlayLevelMusicTrack(bool theNewLevel)
        {
            mMenuMusicPlaying = false;
            if (mRandomMusic)
            {
                if (theNewLevel || mRandomLevelSong < 0)
                {
                    int aSong;
                    do
                    {
                        aSong = SexyUtil.Rand() % 10 + 3;
                    }
                    while (aSong == mRandomLevelSong);
                    mRandomLevelSong = aSong;
                }
                PlayMusic(mRandomLevelSong, true);
                return;
            }
            if (mCurStage % 2 == 0)
            {
                PlayMusic(mCurLevel + 3, true);
            }
            else
            {
                PlayMusic(mCurLevel + 8, true);
            }
        }

        /// <summary>
        /// Port addition: the level music of a Peggle Dual Shot level, in place of the Peggle songs.
        /// theNewLevel (a level starting or restarting) picks one of its two songs at random; otherwise
        /// the one playing carries on.
        /// </summary>
        public void PlayDualShotTrack(bool theNewLevel)
        {
            mMenuMusicPlaying = false;
            int aSong = mCurMusic;
            if (theNewLevel || (aSong != DUALSHOT_SONG_1 && aSong != DUALSHOT_SONG_2))
            {
                aSong = SexyUtil.Rand() % 2 == 0 ? DUALSHOT_SONG_1 : DUALSHOT_SONG_2;
            }
            PlaySong(aSong, true);
        }

        public void PlayVictoryTrack()
        {
            mMenuMusicPlaying = true;
            PlayMusic(14, true);
        }

        public void PlayChallengeTrack()
        {
            mMenuMusicPlaying = false;
            PlayMusic(SexyUtil.Rand() % 10 + 3, true);
        }

        public override void AppEnteredBackground()
        {
            Suspend();
            WriteRestoreInfo();
            mPlayerInfo?.SaveDetails();
#if PEGGLE_IPHONE
            if (mBoard != null)
            {
                mBoard.mSavingInBackground = true;
                mBoard.SaveGame();
                mBoard.mSavingInBackground = false;
            }
#else
            mBoard?.SaveGame();
#endif
        }

        public void Suspend()
        {
            if (mBoard != null && mGameMode == GameMode.GAMEMODE_REPLAY && mBoard.mIsReplay && GetDialog(DialogId.DIALOG_REPLAY_FILE) == null)
            {
                DoReplayFileDialog(false);
            }
        }

        public bool RestoreGame()
        {
            mGameMode = mRestoreGameMode;
            SetLevelPack(mRestoreGameLevelPack);
            if (mPlayerInfo != null && (mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS))
            {
                // Port: a bonus Adventure comes back as itself, with its own progress.
                BonusPage anAdventure = string.IsNullOrEmpty(mRestoreBonusAdventure) ? null : StageMgr.GetAdventurePage(mRestoreBonusAdventure);
                SetLevelPack(mGameMode == GameMode.GAMEMODE_ADVENTURE ? 0 : 1, anAdventure);
                int aLastStage = anAdventure != null ? mPlayerInfo.GetmLastStageStage() : mPlayerInfo.mLastStageStage;
                int aLastLevel = anAdventure != null ? mPlayerInfo.GetmLastStageLevel() : mPlayerInfo.mLastStageLevel;
                mCurStage = SexyUtil.ClampRange(aLastStage, mPlayerInfo.mFirstAdventureStage, mPlayerInfo.mLastAdventureStage);
                mCurLevel = SexyUtil.ClampRange(aLastLevel, 0, 5);
            }
            if (mPlayerInfo == null || !CheckSaveGame(true, true))
            {
                return false;
            }
#if !PEGGLE_IPHONE
            mBoard.mReplayButtonState = 1;
#endif
            mBoard.mRestored = true;
            mBoard.mInterfaceMgr.UpdateInterfaceColor();
            return true;
        }

        private void LoadGroup(string theGroup, Func<ResourceManager, bool, bool> theExtract)
        {
            if (mResourceManager.IsGroupLoaded(theGroup))
            {
                return;
            }
            if (!mResourceManager.LoadResources(theGroup))
            {
                ShowResourceError(true);
            }
            if (!theExtract(mResourceManager, true))
            {
                ShowResourceError(true);
            }
        }

        private void UnloadGroup(string theGroup)
        {
            // Port: a new screen unloads the old screen's group in its constructor, while the old
            // screen is still sliding off and drawing those images (the main menu's background went
            // black for a few frames). The unload waits until nothing is scrolling off.
            if (mWidgetMover.IsRemovingAny())
            {
                mResourceManager.DeleteResourcesLater(theGroup);
                return;
            }
            mResourceManager.DeleteResources(theGroup);
        }

        public void UnloadGameResources()
        {
            if (mLazyResourceLoading)
            {
                UnloadGroup("Game");
                if (mStoryScreen == null)
                {
                    UnloadGroup("CharInGame");
                }
            }
        }

        public void LoadGameResources()
        {
            if (mLazyResourceLoading)
            {
                LoadGroup("Game", Res.ExtractGameResources);
                LoadGroup("CharInGame", Res.ExtractCharInGameResources);
            }
        }

        public void UnloadMainMenuResources()
        {
            if (mLazyResourceLoading)
            {
#if PEGGLE_IPHONE
                if (mStoryScreen == null && (mBoard == null || !mBoard.mShowingGameTip))
                {
                    UnloadGroup("CharSelect");
                }
#endif
                UnloadGroup("MainMenuCommon");
                // Port: the menu theme can change while a menu of the other theme is loaded, so
                // whichever theme's group is loaded goes.
                UnloadLoadedGroup("MainMenuPeggle");
                UnloadLoadedGroup("MainMenuNights");
#if PEGGLE_IPHONE
                // Port: the loading screen draws from this group while it scrolls off (resuming a saved
                // game goes straight to the board); it unloads the group itself when it goes.
                // (mLoadingScreen is already null by then: CleanupScreens drops it mid-scroll.)
                if (!IsLoadingScreenShown())
                {
                    UnloadLoadedGroup("Sunglow");
                    UnloadLoadedGroup("NightsForeground");
                }
#endif
            }
        }

#if PEGGLE_IPHONE
        private bool IsLoadingScreenShown()
        {
            foreach (Widget aWidget in mWidgetManager.mWidgets)
            {
                if (aWidget is LoadingScreen)
                {
                    return true;
                }
            }
            return false;
        }
#endif

        private void UnloadLoadedGroup(string theGroup)
        {
            if (mResourceManager.IsGroupLoaded(theGroup))
            {
                UnloadGroup(theGroup);
            }
        }

        public void LoadMainMenuResources()
        {
            LoadMainMenuResources(mNightsMode);
        }

        /// <summary>
        /// Loads the main menu's groups for the Peggle or the Nights look. Port: without lazy loading
        /// too, since the loading thread only loads the startup theme's group and the menu theme can
        /// change later (nothing is unloaded then, so this loads each group once).
        /// </summary>
        public void LoadMainMenuResources(bool nights)
        {
            LoadGroup("MainMenuCommon", Res.ExtractMainMenuCommonResources);
            if (nights)
            {
                LoadGroup("MainMenuNights", Res.ExtractMainMenuNightsResources);
#if PEGGLE_IPHONE
                LoadGroup("NightsForeground", (m, u) => Res.ExtractNightsForegroundResources(m, false));
#endif
            }
            else
            {
                LoadGroup("MainMenuPeggle", Res.ExtractMainMenuPeggleResources);
#if PEGGLE_IPHONE
                LoadGroup("Sunglow", Res.ExtractSunglowResources);
#endif
            }
#if PEGGLE_IPHONE
            LoadGroup("CharSelect", Res.ExtractCharSelectResources);
#else
            // Port: the PC menu also draws the loading screen's sun glow (Peggle) or front layers
            // (Nights), which only the startup theme's Load group brought in. After a menu theme
            // change they are loaded here, and kept like the startup ones.
            if (nights)
            {
                Res.IMAGE_LOAD_NIGHTS_FRONT = mResourceManager.LoadImage("IMAGE_LOAD_NIGHTS_FRONT");
                Res.IMAGE_LOAD_NIGHTS_FRONT_TOP = mResourceManager.LoadImage("IMAGE_LOAD_NIGHTS_FRONT_TOP");
            }
            else
            {
                Res.IMAGE_LOAD_SUNGLOW_LOAD = mResourceManager.LoadImage("IMAGE_LOAD_SUNGLOW_LOAD");
            }
#endif
        }

#if PEGGLE_IPHONE
        /// <summary>Port: takes the loading screen's look, which the menu theme may have left since.</summary>
        public void UnloadLoadingBarResources(bool nights)
        {
            UnloadGroup("Load");
            UnloadGroup(nights ? "LoadNights" : "LoadPeggle");
            if (mLazyResourceLoading && mMainMenu == null)
            {
                UnloadGroup(nights ? "NightsForeground" : "Sunglow");
            }
        }

#endif
        public void UnloadQuickplayResources()
        {
            if (mLazyResourceLoading && mTrophyScreen == null)
            {
                UnloadGroup("LevelSelect");
            }
        }

        public void LoadQuickplayResources()
        {
            if (mLazyResourceLoading)
            {
                LoadGroup("LevelSelect", Res.ExtractLevelSelectResources);
            }
        }

        public void LoadCharSelectResources()
        {
            if (mLazyResourceLoading)
            {
                LoadGroup("CharSelect", Res.ExtractCharSelectResources);
            }
        }

        public void UnloadCharSelectResources()
        {
            if (mLazyResourceLoading)
            {
                UnloadGroup("CharSelect");
            }
        }
    }
}
