using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The play field (Sexy::Board): owns the level objects, balls and launcher, the game
    /// managers (logic, collisions, effects, text, interface, sound, AI), replays and save games,
    /// and draws the board in its layers.
    /// </summary>
    public class Board : Widget, ButtonListener
    {
        public const int BOARD_MENU_BUTTON = 0;
        public const int BOARD_REPLAY_BUTTON = 1;
        public const int BOARD_FIRE_BUTTON = 2;

        /// <summary>The touch-scroll strip at the board's right edge (Sexy::Board::DialObj).</summary>
        public class DialObj
        {
            public TRect mRect;                                   // 0x00

            public int mLastY;                                    // 0x10

            public int mDelta;                                    // 0x14

            public int mDialPos;                                 // 0x18

            public bool mDown;                                    // 0x1c

            public DialObj()
            {
                mDown = false;
                mDelta = 0;
                mLastY = 0;
                mDialPos = 0;
                mRect = new TRect(BOARD_DIAL_RECT_X, BOARD_DIAL_RECT_Y, BOARD_DIAL_RECT_W, BOARD_DIAL_RECT_H);
            }
        }

        private static readonly string[] CHEAT_PHRASES = { "kathy", "steve", "shanon", "zoom", "scott", "seattle", "piano", "pitcher", "tennis" };

        public static int[] gLastCharacter = { -1, -1 };

        public static bool mLevelEditing;

        public static bool mSlowMoPending;

        public TypingCheck[] mTypingChecks = new TypingCheck[9];  // 0x7c

        public ThunderballApp mApp;                               // 0xa0

        public int mGameMode;                                     // 0xa4

        public bool mLevelBeaten;                                 // 0xac

        public bool mRestored;                                    // 0xad (0xae on the iPhone)

#if PEGGLE_IPHONE
        /// <summary>iPhone: set while AppEnteredBackground saves the game (ball loop sounds are then left alone).</summary>
        public bool mSavingInBackground;                          // 0xad (iPhone)
#endif

        public int mDreamTransitionCounter;                       // 0xb0

        public Gun mGun;                                          // 0xb4

        public bool mShowBackground;                              // 0xb8

        public bool mSlowMo;                                      // 0xb9

        /// <summary>This frame is a slow-motion sub-step (objects only advance fractionally).</summary>
        public bool mFlagBA;                                      // 0xba

        public bool mSlowMoEnding;                                // 0xbb

        public bool mBaseLevel;                                   // 0xbc

        public bool mIsReplay;                                    // 0xbd

        public bool mReplayStarted;                               // 0xbe

        public bool mFlagBF;                                      // 0xbf (level previously cleared)

        public bool mFlagC0;                                      // 0xc0 (level previously aced)

        public int mSlowMoHold;                                   // 0xc4

        public int mSlowMoAccum;                                  // 0xc8

        public int mSlowMoRate;                                   // 0xcc

        public int mSlowMoTargetRate;                             // 0xd0

        public int mSlowMoRateStep;                               // 0xd4

        public int mTrophyLives;                                  // 0xd8

        public int mReplayViews;                                  // 0xdc

        public int mZoomTaps;                                     // 0xe0

        public int mGameId;                                       // 0xe4

        public int mReplayPauseCount;                             // 0xe8

        public bool mShowSpaceCracks;                             // 0xec (the level has a full background image)

        public bool mGenericBackground;                           // 0xed

        public bool mShowingGameTip;                              // 0xee

        public int mResetCount;                                   // 0xf0

        public int mTrophyLevelIdx;                               // 0xf4

        public int mLevelPlayCount;                               // 0xf8

        public float mZoom;                                       // 0xfc

        public float mZoomX;                                      // 0x100

        public float mZoomY;                                      // 0x104

        public int mZoomHold;                                     // 0x108

        public int mZoomFrame;                                    // 0x10c

        public int mShakeCount;                                   // 0x110

        public bool mShakeSmall;                                  // 0x114

        public int mShakeX;                                       // 0x118

        public int mShakeY;                                       // 0x11c

        public bool mZoomIn;                                      // 0x120

        public bool mZoomActive;                                  // 0x121 (zoom enabled)

        public bool mRightClickSpeedup;                           // 0x122

        public bool mStatsSubmitted;                              // 0x123

        public bool mSaveGameOnRemove;                            // 0x124

        public bool mUnpaused;                                    // 0x125

        public bool mIncTipPending;                               // 0x126

        public bool mEasterEgg;                                   // 0x127

        public int mEasterEggIdx;                                 // 0x128

        public TrophyInfo mTrophyInfo;                            // 0x12c

        public bool mInputEnabled;                                // 0x130

        public bool mTrophyCharacter;                             // 0x131

        public int mSuspendCount;                                 // 0x134

        public AIMgr mAIMgr;                                      // 0x138

        public CharacterMgr mCharacterMgr;                        // 0x13c

        public CollisionMgr mCollisionMgr;                        // 0x140

        public EffectMgr mEffectMgr;                              // 0x144

        public FloatingTextMgr mFloatingTextMgr;                  // 0x148

        public InterfaceMgr mInterfaceMgr;                        // 0x14c

        public LogicMgr mLogicMgr;                                // 0x154

        public SoundMgr mSoundMgr;                                // 0x158

        public ReplayDialog mReplayDialog;                        // 0x15c

        public EndLevelDialog mEndLevelDialog;                    // 0x160

        public SlotMachineDialog mSlotMachineDialog;              // 0x164

        public ThunderButton mMenuButton;                         // 0x168

        public int mReplayButtonState;                            // 0x170

        public ThunderButton mFireButton;                         // 0x174

        public DialObj mDialObj;                                  // 0x17c

        public Image mCrackImage;                                 // 0x180

        public List<DataWriter> mReplayWriters = new List<DataWriter>(); // 0x184 (deque)

        public DataWriter mReplayRestorePoint;                    // 0x1ac

        public DataWriter mReplayUnused;                          // 0x1b0

        public List<PhysObj> mPhysObjs = new List<PhysObj>();     // 0x1b4

        public List<Ball> mBalls = new List<Ball>();              // 0x1bc

        public TRect mBoardRect;                                  // 0x1c4

        public int mPauseCount;                                   // 0x1d4

        public int mPauseFade;                                    // 0x1d8

        public int mUnpauseUpdateCnt;                             // 0x1dc

        public int mGameUpdateCnt;                                // 0x1e0 (game ticks this level)

        public int mAttractDelay;                                 // 0x1e4

        public int mCharMoveCount;                                // 0x1e8

        public int mExtraUpdates;                                 // 0x1ec

        public bool mFeverCam;                                    // 0x1f0

        public int mZoomTapX;                                     // 0x1f4

        public int mZoomTapY;                                     // 0x1f8

        public bool mPeggleZoom;                                  // 0x1fc

        public bool mShowZoomHint;                                // 0x1fd

        public bool mFFwdPressed;                                 // 0x1fe

        public bool mReplayPressed;                               // 0x1ff

        public bool mHasReplay;                                   // 0x200

        public TRect mFFwdRect;                                   // 0x204

        public TRect mReplayRect;                                 // 0x214

        public MarqueeWidget mLeftScorePod;                       // 0x224

        public MarqueeWidget mRightScorePod;                      // 0x228

        public bool mScorePodsPending;                            // 0x22c

        public string mLevelName = "";                            // 0x230 (level file path)

        public List<PhysObj> mBackgroundLayer = new List<PhysObj>(); // 0x234

        public List<PhysObj> mMainLayer = new List<PhysObj>();       // 0x240

        public List<PhysObj> mBaseLayer = new List<PhysObj>();       // 0x24c

        public List<PhysObj> mBaseLayer2 = new List<PhysObj>();      // 0x258

        public List<PhysObj> mLightLayer = new List<PhysObj>();      // 0x264

        public List<Emitter> mEmitters = new List<Emitter>();        // 0x270

        public int mInt27C;                                       // 0x27c

#if PEGGLE_IPHONE
        /// <summary>iPhone: the instant replay button (HD has a replay touch zone in the Ball-o-tron instead).</summary>
        public ThunderButton mInstantReplayButton;                // 0x16c (iPhone)

        /// <summary>iPhone: the aiming wheel and fire button strip beside the board.</summary>
        public DialWidget mDialWidget;                            // 0x170 (iPhone)
#endif

        public Board(ThunderballApp theApp)
        {
            mApp = theApp;
            mHasReplay = false;
            mApp.UnloadMainMenuResources();
            mApp.UnloadQuickplayResources();
            mApp.LoadGameResources();
            mTrophyLives = 1;
            mGameId = 0;
            mTrophyInfo = null;
            mIsReplay = false;
            mReplayStarted = false;
            mStatsSubmitted = false;
            mInputEnabled = false;
            mReplayViews = 0;
            mZoomTaps = 0;
            mAIMgr = new AIMgr(this);
            mCharacterMgr = theApp.mCharacterMgr;
            mCollisionMgr = new CollisionMgr();
            mEffectMgr = new EffectMgr();
            mFloatingTextMgr = new FloatingTextMgr();
            mInterfaceMgr = new InterfaceMgr(this);
            mLogicMgr = new LogicMgr(this);
            mSoundMgr = new SoundMgr();
            mReplayDialog = new ReplayDialog(this);
            mEndLevelDialog = new EndLevelDialog(this);
            mSlotMachineDialog = new SlotMachineDialog(this);
            mCrackImage = null;
            mScorePodsPending = true;
            mRestored = false;
            mSlowMo = false;
            mFlagBA = false;
            mSlowMoEnding = false;
            mShowingGameTip = false;
            mFlagBF = false;
            mFlagC0 = false;
            mShowBackground = true;
            mZoomActive = true;
            mRightClickSpeedup = false;
            mSaveGameOnRemove = true;
            mUnpaused = false;
            mIncTipPending = false;
            mEasterEgg = false;
            mSlowMoAccum = 0;
            mSlowMoRate = 0xfa;
            mSlowMoHold = 0;
            mSlowMoTargetRate = 0;
            mSlowMoRateStep = 0x14;
            mExtraUpdates = 0;
            mPauseCount = 0;
            mPauseFade = 0;
            mUnpauseUpdateCnt = 0;
            mResetCount = 0;
            mTrophyLevelIdx = 0;
            mLevelPlayCount = 0;
            mAttractDelay = 0;
            mCharMoveCount = 0;
            mSuspendCount = 0;
            mReplayRestorePoint = null;
            mReplayUnused = null;
            mBoardRect = new TRect(0x4b, 0x28, 0x28a, 0x230);
            mGun = new Gun(this);
            mGun.SetPos((float)(400 - mBoardRect.mX + 0.0 / SexyUtil.gPhysicalDisplayHeight),
                        (float)(0x46 - mBoardRect.mY + 0.0 / SexyUtil.gPhysicalDisplayHeight));
            mMenuButton = SexyUtil.MakeEmbeddedButton(BOARD_MENU_BUTTON, this, Res.IMAGE_IP_MENU, false);
            mMenuButton.Move(BOARD_MENU_BUTTON_X, BOARD_MENU_BUTTON_Y);
            AddWidget(mMenuButton);
#if PEGGLE_IPHONE
            mInstantReplayButton = SexyUtil.MakeEmbeddedButton(BOARD_REPLAY_BUTTON, this, Res.IMAGE_IP_INSTANTREPLAY, false);
            mInstantReplayButton.Move(BOARD_REPLAY_BUTTON_X, BOARD_REPLAY_BUTTON_Y);
            mInstantReplayButton.SetDraw(false);
            mInstantReplayButton.mDisabled = true;
            mInstantReplayButton.mTouchCancelled = true;
            AddWidget(mInstantReplayButton);
            mDialWidget = new DialWidget(this);
#else
            mFireButton = SexyUtil.MakeEmbeddedButton(BOARD_FIRE_BUTTON, this, Res.IMAGE_INT_FIRE, false);
            mFireButton.Move(BOARD_FIRE_BUTTON_X, BOARD_FIRE_BUTTON_Y);
            AddWidget(mFireButton);
            mDialObj = new DialObj();
#endif
            mLeftScorePod = new MarqueeWidget(Res.FONT_SMALL_DISP, 0xf0, 3, 0x1e, 0xf0, -1);
            mRightScorePod = new MarqueeWidget(Res.FONT_SMALL_DISP, 0xf0, 3, 0x1e, 0xf0, -1);
            mLeftScorePod.Resize(BOARD_LEFT_SCORE_POD_X, BOARD_SCORE_POD_Y, BOARD_SCORE_POD_WIDTH, BOARD_SCORE_POD_HEIGHT);
            mRightScorePod.Resize(BOARD_RIGHT_SCORE_POD_X, BOARD_SCORE_POD_Y, BOARD_SCORE_POD_WIDTH, BOARD_SCORE_POD_HEIGHT);
            mLeftScorePod.Pause();
            mRightScorePod.Pause();
            AddWidget(mLeftScorePod);
            AddWidget(mRightScorePod);
            for (int i = 0; i < 9; i++)
            {
                mTypingChecks[i] = new TypingCheck(CHEAT_PHRASES[i]);
            }
            Clear(true);
#if !PEGGLE_IPHONE
            mInt27C = 0x32;
#endif
        }

        public override void Dispose()
        {
            CheckIncTip();
            mApp.UnloadGameResources();
            // As ~Board deletes its InterfaceMgr: that unloads the character's "IP" group, so the
            // next board's UpdateInterfaceColor reloads it and sets its frame image ids. Leaving it
            // loaded made the next board skip that and crash on a null shooter-back image.
            mInterfaceMgr?.Dispose();
            if (mEndLevelDialog.mWidgetManager != null)
            {
                mApp.mWidgetManager.RemoveWidget(mEndLevelDialog);
            }
            if (mSlotMachineDialog.mWidgetManager != null)
            {
                mApp.mWidgetManager.RemoveWidget(mSlotMachineDialog);
            }
            DeleteReplays(false);
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        /// <summary>Clears the level and all managers; clearTrophy also forgets the trophy character.</summary>
        public void Clear(bool clearTrophy)
        {
            mReplayButtonState = 0;
            mDreamTransitionCounter = 0;
            mEmitters.Clear();
            mLevelBeaten = false;
            if (clearTrophy)
            {
                mTrophyCharacter = false;
            }
            mAIMgr.Clear();
            PhysObj.GetImageMgr().Clear();
            mCollisionMgr.Clear();
            mEffectMgr.Clear();
            mFloatingTextMgr.Clear();
            mInterfaceMgr.Clear();
            foreach (Ball aBall in mBalls)
            {
                aBall.StopFireballSound();
                aBall.StopLightningSound();
            }
            mSoundMgr.Clear();
            mGun.Clear();
            mLogicMgr.Clear(clearTrophy, false);
            mCharacterMgr.Clear(false);
            mCharacterMgr.CalcLookPos(this, true);
            mPhysObjs.Clear();
            mBalls.Clear();
            mLevelName = "";
            PhysObj.mCurSortId = 0;
            mReplayPauseCount = 0;
            mShowSpaceCracks = false;
            mGenericBackground = false;
            mBaseLevel = false;
            SetSlowMo(false, 0xfa);
            DeleteReplays(false);
#if PEGGLE_IPHONE
            mDialWidget.Reset();
#else
            mFFwdPressed = false;
#endif
            mReplayPressed = false;
            mLevelBeaten = false;
        }

        public void CheckIncTip()
        {
            if (mIncTipPending)
            {
                if (mApp.mPlayerInfo != null)
                {
                    mApp.mPlayerInfo.IncTip(mEndLevelDialog.mTipIndex);
                }
                mIncTipPending = false;
            }
        }

        public void DeleteReplays(bool keepRestorePoint)
        {
            if (!keepRestorePoint)
            {
                mReplayRestorePoint = null;
            }
            mReplayUnused = null;
            mReplayWriters.Clear();
        }

        public override void Move(int theNewX, int theNewY)
        {
#if PEGGLE_IPHONE
            // The dial strip moves with the board (screen scrolls).
            mDialWidget.Move(mDialWidget.Left() + (theNewX - mX), mDialWidget.Top() + (theNewY - mY));
#endif
            base.Move(theNewX, theNewY);
        }

        /// <summary>Places the board (and its ffwd/replay touch zones) for left- or right-handed play.</summary>
        public void SetPadLayout(bool rightHanded)
        {
#if PEGGLE_IPHONE
            // Port addition: PC Controls drop the wheel and fire strip (the mouse aims and fires) and centre the board.
            mDialWidget.SetVisible(!UsePCControls());
            if (UsePCControls())
            {
                Resize(SexyUtil.gGutterWidth + (SCREEN_WIDTH - BOARD_W) / 2, 0, BOARD_W, SCREEN_HEIGHT);
                mFFwdRect = new TRect(0, 0, BOARD_FFWD_RECT_W, BOARD_FFWD_RECT_H);
            }
            else if (rightHanded)
            {
                Resize(SexyUtil.gGutterWidth, 0, BOARD_W, SCREEN_HEIGHT);
                mDialWidget.Resize(BOARD_W + SexyUtil.gGutterWidth, 0, SCREEN_WIDTH - BOARD_W, SCREEN_HEIGHT);
                mFFwdRect = new TRect(0, 0, BOARD_FFWD_RECT_W, BOARD_FFWD_RECT_H);
            }
            else
            {
                int aDialW = SCREEN_WIDTH - BOARD_W;
                Resize(aDialW + SexyUtil.gGutterWidth, 0, BOARD_W, SCREEN_HEIGHT);
                mDialWidget.Resize(SexyUtil.gGutterWidth, 0, aDialW, SCREEN_HEIGHT);
                mFFwdRect = new TRect(SCREEN_WIDTH - aDialW - BOARD_FFWD_RECT_W, 0, BOARD_FFWD_RECT_W, BOARD_FFWD_RECT_H);
            }
#else
            // Port: left-handed HD finished. The original moved only the fast-forward rect (to the right). Here
            // the Ball-o-tron (dial, replay controls) and the fever meter swap sides (InterfaceMgr's
            // LEFT_HANDED_SHIFT_X), as do the fire and menu buttons; the fast-forward and replay buttons
            // are part of the frames' tops, which stay, so their rects do too.
            // PC Controls always use the left-handed frames but keep the buttons where they are, and the
            // Ball-o-tron shows no wheel, so the wheel has no touch area.
            bool aLeft = IsLeftHandedLayout();
            mSwapFireButton = aLeft && !UsePCControls();
            Resize(0, 0, BOARD_W, SCREEN_HEIGHT);
            mFFwdRect = new TRect(0, 0, BOARD_FFWD_RECT_W, BOARD_FFWD_RECT_H);
            mReplayRect = new TRect(SCREEN_WIDTH - BOARD_FFWD_RECT_W, 0, BOARD_FFWD_RECT_W, BOARD_FFWD_RECT_H);
            mDialObj.mRect = new TRect(BOARD_DIAL_RECT_X - (aLeft ? InterfaceMgr.LEFT_HANDED_SHIFT_X : 0), BOARD_DIAL_RECT_Y,
                UsePCControls() ? 0 : BOARD_DIAL_RECT_W, BOARD_DIAL_RECT_H);
            mFireButton.Move(GetFireButtonX(), BOARD_FIRE_BUTTON_Y);
            mMenuButton.Move(mSwapFireButton ? BOARD_FIRE_BUTTON_X : BOARD_MENU_BUTTON_X, BOARD_MENU_BUTTON_Y);
#endif
            if (mEndLevelDialog.mWidgetManager != null && mEndLevelDialog.mVisible)
            {
                mEndLevelDialog.Move(mX + BOARD_END_LEVEL_DIALOG_X, mEndLevelDialog.mY);
            }
            Widget aTip = mApp.GetDialog(DialogId.DIALOG_GAME_TIP);
            if (aTip != null)
            {
                aTip.Move(BOARD_GAMETIP_DIALOG_X + mX, aTip.mY);
            }
            Widget aStage = mApp.GetDialog(DialogId.DIALOG_STAGE);
            if (aStage != null)
            {
                aStage.Move(BOARD_STAGE_DIALOG_X + mX, aStage.mY);
            }
        }

        /// <summary>Saves or restores the whole game (objects, launcher, managers, slow-mo, zoom...).</summary>
        public void SyncState(DataSync theSync)
        {
            int aVersion = 0x57;
            bool aCanSyncPointers = true;
            string aLevelName = "";
            theSync.SyncLong(ref aVersion);
            theSync.SyncBool(ref aCanSyncPointers);
            theSync.SetVersion(aVersion);
            if (aVersion > 0x14)
            {
                int aTrophyLevel = mTrophyLevelIdx;
                int aShots = mLogicMgr.mStats[0].mShots;
                int aScore = mLogicMgr.mScore[0];
                int aBallsLeft = mLogicMgr.mBallsLeft[0];
                int aTrophyId = mTrophyInfo == null ? 0 : mTrophyInfo.mId;
                aLevelName = SexyUtil.GetFileName(mLevelName, true);
                theSync.SyncLong(ref aTrophyLevel);
                theSync.SyncLong(ref aShots);
                theSync.SyncLong(ref aScore);
                theSync.SyncLong(ref aBallsLeft);
                theSync.SyncLong(ref aTrophyId);
                theSync.SyncString(ref aLevelName);
                if (theSync.GetReader() != null)
                {
                    mLevelName = ResourceManager.GetResourceDir() + "levels/" + aLevelName + ".dat";
                }
            }
            if (aVersion > 0x33)
            {
                bool isDuel = mLogicMgr.mDuelMode;
                theSync.SyncBoolBit(ref isDuel);
                theSync.EndBit();
                if (isDuel)
                {
                    string aName1 = mLogicMgr.mPlayerNames[0];
                    string aName2 = mLogicMgr.mPlayerNames[1];
                    int aScore2 = mLogicMgr.mScore[1];
                    theSync.SyncLong(ref aScore2);
                    theSync.SyncString(ref aName1);
                    theSync.SyncString(ref aName2);
                }
            }
            PhysObj.mSyncType = 0;
            theSync.SyncList(mPhysObjs, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            if (theSync.GetVersion() > 3)
            {
                mGun.SyncState(theSync);
                theSync.SyncBool(ref mSlowMo);
                theSync.SyncLong(ref mSlowMoAccum);
                theSync.SyncLong(ref mSlowMoRate);
                if (theSync.GetVersion() < 0xe)
                {
                    mFlagBA = false;
                    mSlowMoEnding = false;
                    mSlowMoHold = 0;
                    mSlowMoAccum = 0;
                    mSlowMoRate = 0xfa;
                }
                else
                {
                    theSync.SyncBool(ref mFlagBA);
                    theSync.SyncBool(ref mSlowMoEnding);
                    theSync.EndBit();
                    theSync.SyncLong(ref mSlowMoHold);
                }
                if (theSync.GetVersion() > 0x11)
                {
                    theSync.SyncLong(ref mSlowMoTargetRate);
                    theSync.SyncLong(ref mSlowMoRateStep);
                }
            }
            mCharacterMgr.SyncState(theSync);
            mEffectMgr.SyncState(theSync);
            mFloatingTextMgr.SyncState(theSync);
            mLogicMgr.SyncState(theSync);
            mSoundMgr.SyncState(theSync);
            mInterfaceMgr.SyncState(theSync);
            if (theSync.GetReader() != null)
            {
                mCollisionMgr.Clear();
                List<PhysObj> anObjs = new List<PhysObj>(mPhysObjs);
                mPhysObjs.Clear();
                mBalls.Clear();
                foreach (PhysObj anObj in anObjs)
                {
                    if (anObj.IsBackground())
                    {
                        PhysObj aBackground = anObj;
                        if (aBackground.mImageName.IndexOf(GlobalStaticVars.mResPathPrefix) < 0)
                        {
                            int aSlash = aBackground.mImageName.IndexOf('/') + 1;
                            string aPath = aBackground.mImageName.Substring(0, aSlash);
                            int aLastSlash = aBackground.mImageName.LastIndexOf('/') + 1;
                            aPath += GlobalStaticVars.mResPathPrefix + aBackground.mImageName.Substring(aLastSlash);
                            aBackground.mImageName = aPath;
                            aBackground.DeleteImage();
                            aBackground.LoadImage();
                        }
                        mCrackImage = anObj.mImage;
                    }
                    AddObj(anObj);
                }
                mCharacterMgr.Resync();
                if (mGun.mDoBouncyGuide)
                {
                    mGun.UpdateBouncyGuide();
                }
                mZoom = 0;
                mZoomFrame = 0;
                mZoomIn = false;
            }
            if (theSync.GetVersion() > 4)
            {
                theSync.SyncLong(ref mGameUpdateCnt);
            }
            if (theSync.GetVersion() > 7)
            {
                theSync.SyncLong(ref mTrophyLives);
                theSync.SyncLong(ref mGameId);
            }
            if (theSync.GetVersion() < 0x13)
            {
                mGameMode = mApp.mGameMode;
            }
            else
            {
                theSync.SyncLong(ref mResetCount);
                theSync.SyncLong(ref mTrophyLevelIdx);
                byte[] aMode = BitConverter.GetBytes(mGameMode);
                theSync.SyncBytes(aMode, 4);
                mGameMode = BitConverter.ToInt32(aMode, 0);
                int aTrophyId = 0;
                if (theSync.GetWriter() != null && mTrophyInfo != null)
                {
                    aTrophyId = mTrophyInfo.mId;
                }
                theSync.SyncLong(ref aTrophyId);
                if (theSync.GetReader() != null)
                {
                    if (aTrophyId == 0)
                    {
                        mTrophyInfo = null;
                    }
                    else
                    {
                        mTrophyInfo = mApp.mTrophyMgr.GetTrophyInfoById(aTrophyId);
                        if (mTrophyInfo == null)
                        {
                            throw new DataReaderException(CFormat.StrFormat("Trophy not found: %d", aTrophyId));
                        }
                    }
                }
            }
            if (theSync.GetVersion() > 0x18)
            {
                bool anUnused1 = false;
                bool anUnused2 = false;
                theSync.SyncBoolBit(ref anUnused1);
                theSync.SyncBoolBit(ref anUnused2);
                theSync.SyncBoolBit(ref mFlagBF);
                theSync.EndBit();
            }
            bool hasZoom = false;
            if (theSync.GetVersion() > 0x2c)
            {
                hasZoom = mZoom != 0.0f;
                theSync.SyncBoolBit(ref mZoomIn);
                theSync.SyncBoolBit(ref hasZoom);
                theSync.EndBit();
            }
            if (hasZoom)
            {
                theSync.SyncFloat(ref mZoom);
                theSync.SyncFloat(ref mZoomX);
                theSync.SyncFloat(ref mZoomY);
                theSync.SyncLong(ref mZoomFrame);
            }
            if (theSync.GetVersion() > 0x14 && mLogicMgr.mState == LogicMgr.STATE_LEVEL_DONE)
            {
                byte[] aType = BitConverter.GetBytes(mEndLevelDialog.mType);
                theSync.SyncBytes(aType, 4);
                mEndLevelDialog.mType = BitConverter.ToInt32(aType, 0);
                if (theSync.GetReader() != null)
                {
                    InitEndLevelDialog(mEndLevelDialog.mType, true);
                }
                if (mGameMode != GameMode.GAMEMODE_DEMO)
                {
                    bool isShowing = true;
                    if (theSync.GetVersion() > 0x34)
                    {
                        isShowing = mEndLevelDialog.mWidgetManager != null && mEndLevelDialog.mVisible;
                        theSync.SyncBoolBit(ref isShowing);
                        theSync.EndBit();
                    }
                    if (theSync.GetReader() != null && isShowing)
                    {
                        ShowEndLevelDialog();
                    }
                }
            }
            if (theSync.GetVersion() > 0x3c)
            {
                bool wasShowing = mSlotMachineDialog.mWidgetManager != null;
                bool isShowing = wasShowing;
                theSync.SyncBoolBit(ref isShowing);
                theSync.EndBit();
                if (isShowing)
                {
                    mSlotMachineDialog.SyncState(theSync);
                }
                if (wasShowing != isShowing)
                {
                    if (!isShowing)
                    {
                        RemoveSlotMachineDialog();
                    }
                    else
                    {
                        mWidgetManager.AddWidget(mSlotMachineDialog);
                        mSoundMgr.PauseMusic(true);
                    }
                }
            }
            if (theSync.GetVersion() > 0x52)
            {
                theSync.SyncBoolBit(ref mFeverCam);
                theSync.SyncBoolBit(ref mPeggleZoom);
            }
            if (theSync.GetVersion() > 0x54)
            {
                theSync.SyncBoolBit(ref mFlagC0);
            }
            if (theSync.GetVersion() > 0x56)
            {
                theSync.SyncLong(ref mReplayViews);
                theSync.SyncLong(ref mZoomTaps);
            }
            if (theSync.GetReader() != null)
            {
                mShakeCount = 0;
                RefreshEmitters();
                CheckForBackground();
                if (mReplayDialog.mVisible)
                {
                    mZoomFrame = 0;
                    PositionReplayDialog();
                }
            }
            mScorePodsPending = mLogicMgr.mState == LogicMgr.STATE_AIMING;
        }

        public void AddObj(PhysObj theObj)
        {
            AddObj(theObj, mPhysObjs.Count);
        }

        /// <summary>Inserts theObj into the object list (and the ball list, assigning a free player slot).</summary>
        public void AddObj(PhysObj theObj, int theIndex)
        {
            mPhysObjs.Insert(theIndex, theObj);
            mCollisionMgr.AddObj(theObj);
            if (theObj.mShapeType == PhysObj.SHAPE_BALL)
            {
                Ball aBall = (Ball)theObj;
                if (!aBall.mIsHatBall)
                {
                    bool[] aUsed = new bool[2];
                    foreach (Ball anOther in mBalls)
                    {
                        int anId = anOther.mId;
                        if (anId > -1 && anId < 2)
                        {
                            aUsed[anId] = true;
                        }
                    }
                    int aFree = 0;
                    while (aFree < 2 && aUsed[aFree])
                    {
                        aFree++;
                    }
                    if (aFree < 2)
                    {
                        aBall.mId = aFree;
                    }
                    mBalls.Add(aBall);
                }
            }
            theObj.AddedToGame();
        }

        public void InitEndLevelDialog(int theMode, bool scrollIn)
        {
            string aLevel = SexyUtil.GetFileName(mLevelName, true);
            List<HighScoreEntry> aScores = null;
            if (mGameMode == GameMode.GAMEMODE_QUICKPLAY || mGameMode == GameMode.GAMEMODE_NONE)
            {
                aScores = mApp.mHighScoreMgr.GetScores(aLevel, true, -1);
            }
            mEndLevelDialog.Init(theMode, aScores);
            ResizeEndLevelDialog();
            if (scrollIn)
            {
                mEndLevelDialog.DoScroll(1);
            }
        }

        public void ShowEndLevelDialog()
        {
            if (mLogicMgr.mDuelMode && mApp.mPlayerInfo != null && mApp.mPlayerInfo.mMusicEnabled)
            {
                mApp.mPlayerInfo.mMusicEnabled = false;
                mApp.mPlayerInfo.mDirty = true;
            }
            // Port: the results screen plays the menu theme of the level pack just played
            // (Peggle or Nights), whatever the main menu's look. A board that is sliding off still
            // updates and calls this again once the dialog has gone; it must not take the music back.
            if (mApp.mBoard == this)
            {
                // Port: Extreme Fever's Ode to Joy plays on over the results; the menu theme waits
                // until it ends (ThunderballApp.UpdateResultsMusic) or the player leaves.
                if (mApp.mCurMusic == 0 && mApp.mMusicInterface.IsPlaying(0))
                {
                    mApp.mResultsMusicPending = true;
                }
                else
                {
                    mApp.PlayMenuMusic(ThunderballApp.mGameLevelPack == 1);
                }
            }
            else if (!mApp.IsMainMenuAmbiencePlaying())
            {
                // Port: not over the main menu's ambience, which the menu this board slides off to started.
                mApp.PlayMenuMusic();
            }
            mEndLevelDialog.SetVisible(true);
            mWidgetManager.AddWidget(mEndLevelDialog);
            mEndLevelDialog.Move(mX + BOARD_END_LEVEL_DIALOG_X, mEndLevelDialog.mY);
        }

        public void RemoveSlotMachineDialog()
        {
            if (mSlotMachineDialog.mWidgetManager != null)
            {
                mWidgetManager.RemoveWidget(mSlotMachineDialog);
                mSoundMgr.PauseMusic(false);
                mUnpauseUpdateCnt = 0;
                mIsOver = Contains(mWidgetManager.mLastMouseX, mWidgetManager.mLastMouseY);
            }
        }

        public void RefreshEmitters()
        {
            mEmitters.Clear();
            foreach (PhysObj anObj in mPhysObjs)
            {
                if (anObj.mShapeType == PhysObj.SHAPE_EMITTER)
                {
                    mEmitters.Add((Emitter)anObj);
                }
            }
        }

        /// <summary>Finds the full-board background image (showing or hiding it per mShowBackground).</summary>
        public void CheckForBackground()
        {
            mShowSpaceCracks = false;
            mInterfaceMgr.SyncStageNum();
            foreach (PhysObj anObj in mPhysObjs)
            {
                if (anObj.mBackground && anObj.mImage != null && anObj.mImage.mWidth > BOARD_BACKGROUND_OBJ_SIZE &&
                    anObj.mImage.mHeight > BOARD_BACKGROUND_OBJ_SIZE)
                {
                    mShowSpaceCracks = mShowBackground;
                    anObj.mVisible = mShowBackground;
                    if (!anObj.mImageCached && anObj.mImageName.IndexOf("levels/") != -1)
                    {
                        anObj.GetCenter();
                    }
                }
            }
        }

        public void PositionReplayDialog()
        {
#if PEGGLE_IPHONE
            int aWidth = Res.IMAGE_IP_REPLAYFRAME.mWidth;
#else
            int aWidth = REPLAY_FRAME_W;
#endif
            int aHeight = mReplayDialog.GetPreferredHeight(aWidth);
            int aX = (mWidth - aWidth) + REPLAY_CONTROLS_OFFSET_X;
#if !PEGGLE_IPHONE
            aX += mInterfaceMgr.GetBallotronShift();
#endif
            int aY = SCREEN_HEIGHT - aHeight;
            if (mZoomFrame > 10 && GlobalStaticVars.gSexyAppBase.Is3DAccelerated())
            {
#if PEGGLE_IPHONE
                double aShift = (mZoomFrame + -10) * 0x10 / 10 * SexyUtil.gPhysicalDisplayHeight / 600.0;
#else
                double aShift = (mZoomFrame + -10) / 10 * SexyUtil.gPhysicalDisplayHeight / 600.0;
#endif
                aX = (int)(aX - aShift);
                aY = (int)(aY - aShift);
            }
            mReplayDialog.Resize(mX + aX, aY, aWidth, aHeight);
        }

        public void SaveGame()
        {
            EndReplay();
            mIncTipPending = false;
            if (mApp.mPlayerInfo == null)
            {
                return;
            }
            string aName = mApp.mPlayerInfo.GetSavedGameName(mGameMode, ThunderballApp.mGameLevelPack);
            if (aName.Length != 0)
            {
                DataSync aSync = new DataSync();
                aSync.StartWriteMemory(0x20);
                SyncState(aSync);
                DataWriter aWriter = aSync.GetWriter();
                mApp.WriteBytesToFile(aName, aWriter.GetDataPtr(), aWriter.GetDataLen());
            }
        }

        /// <summary>Leaves instant replay, restoring the live game from the restore point.</summary>
        public void EndReplay()
        {
            if (mReplayRestorePoint == null)
            {
                if (mLogicMgr.mMuteLoops)
                {
                    mLogicMgr.mMuteLoops = false;
                    if (mLogicMgr.mState == LogicMgr.STATE_NONE)
                    {
                        mLogicMgr.BeginTurn(false);
                    }
                }
            }
            else
            {
                SetSyncPoint(mReplayRestorePoint, false);
                mReplayRestorePoint = null;
                if (!mIsReplay)
                {
                    mLogicMgr.mMuteLoops = false;
                }
            }
            if (!mIsReplay)
            {
                if (mReplayDialog.mWidgetManager != null)
                {
#if PEGGLE_IPHONE
                    // Slides back down instead of vanishing.
                    mReplayDialog.DoScrollOff(false);
                    mReplayDialog.mScrollDir = 1;
                    mReplayDialog.mScrollTargetY = BOARD_REPLAY_DIALOG_SCROLL_Y;
#else
                    mWidgetManager.RemoveWidget(mReplayDialog);
                    mReplayButtonState = 1;
#endif
                    mInterfaceMgr.mButtonCover = 0;
                }
                if (mEndLevelDialog.mWidgetManager != null)
                {
                    mEndLevelDialog.SetVisible(true);
                }
            }
        }

        public bool LoadGame(Buffer theBuffer)
        {
            DataSync aSync = new DataSync();
            aSync.StartReadMemory(theBuffer.GetDataPtr(), theBuffer.GetDataLen(), false);
            SyncState(aSync);
            if ((mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS) &&
                mLogicMgr.mStats[0].mShots == 0)
            {
                mLevelPlayCount = 1;
                mGun.Clear();
                mEffectMgr.Clear();
                mFloatingTextMgr.Clear();
                mInterfaceMgr.Clear();
                mSoundMgr.Clear();
                mLogicMgr.ReInitLevel();
            }
            else if (mLogicMgr.mState == LogicMgr.STATE_CHARACTER_DIALOG)
            {
                DoCharacterDialog(false);
            }
            mInterfaceMgr.UpdateHighScore(false);
            return true;
        }

        public void DoCharacterDialog(bool unused)
        {
            mShowingGameTip = true;
            CharacterDialog aDialog = new CharacterDialog(this);
            int aWidth = aDialog.mWidth;
#if PEGGLE_IPHONE
            aDialog.Resize((SCREEN_WIDTH - aWidth) / 2, 0, aWidth + SexyUtil.gGutterWidth * 2, aDialog.GetPreferredHeight(aWidth));
#else
            aDialog.Resize((SCREEN_WIDTH - aWidth) / 2, 0, aWidth, aDialog.GetPreferredHeight(aWidth));
#endif
            mApp.AddDialog(aDialog.mId, aDialog);
        }

        public bool NeedSaveGame()
        {
            EndReplay();
            if (!mSaveGameOnRemove)
            {
                return false;
            }
            if (mLogicMgr.mState == LogicMgr.STATE_LEVEL_DONE)
            {
                if (mGameMode == GameMode.GAMEMODE_CHALLENGE && mTrophyInfo != null)
                {
                    uint aType = (uint)mEndLevelDialog.mType;
                    return aType < 6 && ((1 << (int)aType) & 0x39) != 0;
                }
                return false;
            }
            if (mApp.mPlayerInfo == null)
            {
                return false;
            }
            switch (mGameMode)
            {
                case GameMode.GAMEMODE_ADVENTURE:
                case GameMode.GAMEMODE_ADVENTURE_NIGHTS:
                    return true;
                case GameMode.GAMEMODE_QUICKPLAY:
                    return mLogicMgr.mScore[0] > 0;
                case GameMode.GAMEMODE_CHALLENGE:
                    return mTrophyLevelIdx >= 1 || mLogicMgr.mScore[0] > 0;
                case GameMode.GAMEMODE_DUEL:
                    return mLogicMgr.mScore[0] >= 1 || mLogicMgr.mScore[1] > 0;
                default:
                    return false;
            }
        }

        public void SubmitTotalStats()
        {
            int aMode = mApp.mGameMode;
            if (!mStatsSubmitted && !mLogicMgr.mDuelMode &&
                (aMode == GameMode.GAMEMODE_ADVENTURE || aMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS ||
                 aMode == GameMode.GAMEMODE_QUICKPLAY || aMode == GameMode.GAMEMODE_CHALLENGE) &&
                mLogicMgr.mStats[0].mShots > 0)
            {
                mStatsSubmitted = true;
                mApp.mPlayerInfo.AddTotalStats(mLogicMgr.mStats[0], mApp.mGameMode);
            }
        }

        public static bool Cheated()
        {
            return false;
        }

        public bool HasShot()
        {
            return mLogicMgr.mStats[0].mShots > 0;
        }

        public DataWriter GetSyncPoint()
        {
            DataWriter aWriter = new DataWriter();
            aWriter.OpenMemory(0x20);
            DataSync aSync = new DataSync(aWriter);
            SyncState(aSync);
            aSync.SyncPointers();
            return aWriter;
        }

        public bool SetSyncPoint(DataWriter theWriter, bool withClickTimes)
        {
            if (theWriter == null)
            {
                return false;
            }
            DataSync aSync = new DataSync();
            aSync.StartReadMemory(theWriter.GetDataPtr(), theWriter.GetDataLen(), false);
            SyncState(aSync);
            if (withClickTimes && aSync.GetVersion() > 5 && aSync.GetReader().CanReadBytes(1))
            {
                mLogicMgr.SyncClickTimes(aSync);
            }
            aSync.SyncPointers();
            if (aSync.GetVersion() < 0x1d && mBalls.Count != 0)
            {
                mBalls[0].mId = 0;
            }
            if (aSync.GetVersion() > 0x52 && mPeggleZoom)
            {
                mPeggleZoom = false;
                mZoomFrame = 0;
                mZoomIn = false;
                mZoom = 0;
                SetSlowMo(false, 1000);
            }
            MarkDirty();
            return true;
        }

        public void SetSlowMo(bool on, int theRate)
        {
            mSlowMo = on;
            mSlowMoEnding = false;
            mSlowMoHold = 0;
            mFlagBA = false;
            mSlowMoAccum = 0;
            mSlowMoRate = theRate;
            mSlowMoTargetRate = 0;
            if (!on)
            {
                if (mLogicMgr.mSlowMoSoundPlaying)
                {
                    mLogicMgr.KillSlowMoSound();
                    mLogicMgr.mSlowMoSoundPlaying = false;
                }
                mZoomIn = false;
            }
        }

        /// <summary>Saves a replay snapshot for this shot, keeping only the latest one.</summary>
        public void GetReplayPoint()
        {
            mReplayWriters.Add(GetSyncPoint());
            if (mReplayWriters.Count > 1)
            {
                mReplayWriters.RemoveAt(0);
            }
        }

        public static void CheckShowReplayTip()
        {
        }

        public long GameID()
        {
            return ((long)mApp.mPlayerInfo.mId << 32) + (uint)mResetCount + ((long)(uint)mGameMode << 16);
        }

        public void ShowReplayDialog()
        {
#if !PEGGLE_IPHONE
            mFFwdPressed = false;
#endif
            mReplayPressed = false;
            mLogicMgr.mMuteLoops = true;
            mReplayDialog.StopBlinkingButtons();
            if (mReplayDialog.mWidgetManager == null)
            {
                PositionReplayDialog();
                if (!mIsReplay)
                {
                    mReplayDialog.DoScroll(-1);
                    mReplayDialog.mY = mHeight + 10;
                }
                mWidgetManager.AddWidget(mReplayDialog);
                CheckShowReplayTip();
            }
        }

        /// <summary>Restores replay snapshot theIndex (-1 = latest); restart re-takes the shot instead of watching it.</summary>
        public void ShowReplay(int theIndex, bool restart)
        {
            if (theIndex < 0)
            {
                theIndex = mReplayWriters.Count - 1;
            }
            if (theIndex < 0 || theIndex >= mReplayWriters.Count)
            {
                return;
            }
            DataWriter aWriter = mReplayWriters[theIndex];
            if (aWriter == null)
            {
                return;
            }
            if (mReplayRestorePoint == null)
            {
                mReplayRestorePoint = GetSyncPoint();
                if (mLogicMgr.mState == LogicMgr.STATE_SHOOTING)
                {
                    mLogicMgr.WriteClickTimes();
                }
            }
            if (!SetSyncPoint(aWriter, true))
            {
                DeleteReplays(true);
                Pause(true);
                mApp.DoDialogScroll(0x22, true, SexyAppBase.Loc("Replay Load Error"), SexyAppBase.Loc("There was an error loading the replay."),
                    SexyAppBase.Loc("Ok"), 3);
                if (mIsReplay)
                {
                    EndReplay();
                }
            }
            else if (!restart)
            {
                mReplayPauseCount = 0x32;
                mEndLevelDialog.SetVisible(false);
                ShowReplayDialog();
            }
            else
            {
                mReplayRestorePoint = null;
                mWidgetManager.RemoveWidget(mEndLevelDialog);
                RemoveSlotMachineDialog();
                KillAllBalls();
                mLogicMgr.IncNumBalls(1, 0, true);
                mLogicMgr.BeginTurn2();
                mLogicMgr.mStats[mLogicMgr.mCurPlayer].mShots--;
                if (mLogicMgr.mPowerupCount[7][mLogicMgr.mCurPlayer] > 0)
                {
                    mGun.mSuperGuide = false;
                    mGun.SetDoBouncyGuide(mLogicMgr.mPowerupCount[1][mLogicMgr.mCurPlayer] > 0);
                    mCharacterMgr.SetYinYangEye(true);
                }
                mFloatingTextMgr.Clear();
                mEffectMgr.Clear();
                mSoundMgr.Clear();
            }
        }

        public void KillAllBalls()
        {
            foreach (Ball aBall in new List<Ball>(mBalls))
            {
                if (!aBall.mIsPeg)
                {
                    RemoveObj(aBall);
                }
            }
        }

        public void Pause(bool pause)
        {
            if (pause)
            {
                mMenuButton.mMouseVisible = false;
#if PEGGLE_IPHONE
                mInstantReplayButton.mMouseVisible = false;
#endif
                mReplayDialog.SetMouseVisibility(false);
                mPauseCount++;
                if (GlobalStaticVars.gSexyAppBase.GetDialog(DialogId.DIALOG_HIGH_SCORE_ENTRY) != null || GlobalStaticVars.gSexyAppBase.GetDialog(DialogId.DIALOG_END_LEVEL) != null)
                {
                    return;
                }
                // The original tests bit 0 of byte +0x12c of dialog 15 (LevelScreen/TrophyScreen). That byte is
                // the low byte of the thumbnails-listener sub-object's vptr, which is always even, so the test
                // always passes: any open level/trophy screen keeps the music playing.
                if (GlobalStaticVars.gSexyAppBase.GetDialog(DialogId.DIALOG_LEVEL_SCREEN) != null)
                {
                    return;
                }
                if (mApp.mCurMusic == 0)
                {
                    mSoundMgr.PauseMusic(true);
                }
            }
            else
            {
                mPauseCount--;
                if (mPauseCount < 0)
                {
                    mPauseCount = 0;
                }
                if (mPauseCount == 0)
                {
                    mUnpaused = true;
                    mSoundMgr.PauseMusic(false);
                    mUnpauseUpdateCnt = mUpdateCnt;
                    if (mWidgetManager != null && mWidgetManager.mMouseIn)
                    {
                        MouseMove(mWidgetManager.mLastMouseX, mWidgetManager.mLastMouseY);
                    }
                }
            }
            MarkDirty();
        }

        public static void SyncColorblind()
        {
        }

        public void RecordTrophyResult(bool won)
        {
            if (mTrophyInfo != null && mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                StatsMgr aStats = mApp.mStatsMgr;
                TrophyStats aTrophyStats = aStats.GetStatsForTrophy(mTrophyInfo.mId);
                if (won)
                {
                    aTrophyStats.mNumWins++;
                }
                else
                {
                    aTrophyStats.mNumLosses++;
                }
                aStats.Save(null);
            }
        }

        public bool IsCurTrophyBeaten(bool forDialog)
        {
            if (mTrophyInfo == null)
            {
                return false;
            }
            bool beaten = true;
            if (!mLogicMgr.BeatLevel())
            {
                beaten = false;
                if (mTrophyInfo.mNoForceWin)
                {
                    beaten = true;
                    if (!forDialog)
                    {
                        beaten = !mTrophyInfo.mNoEndOnWin;
                    }
                }
            }
            bool aResult = beaten;
            if (mTrophyInfo.mScoreCeil > 0 && mTrophyLevelIdx >= mTrophyInfo.mLevels.Count - 1)
            {
                aResult = false;
                if (beaten)
                {
                    aResult = mLogicMgr.mScore[0] < mTrophyInfo.mScoreCeil;
                }
            }
            if (mTrophyInfo.mScoreReq > 0 && mTrophyLevelIdx >= mTrophyInfo.mLevels.Count - 1)
            {
                bool wasBeaten = aResult;
                aResult = false;
                if (wasBeaten)
                {
                    aResult = mTrophyInfo.mScoreReq < mLogicMgr.mScore[0];
                }
            }
            return aResult && VerifyStyleShotsCompleted();
        }

        public bool VerifyStyleShotsCompleted()
        {
            bool anyRequired = false;
            bool allDone = true;
            for (int i = 0; i < 0x18; i++)
            {
                if (mTrophyInfo.mStyleShotsReq[i] > 0)
                {
                    anyRequired = true;
                }
                if (mLogicMgr.mStyleShotsLeft[i] > 0)
                {
                    allDone = false;
                }
            }
            if (anyRequired && !allDone)
            {
                return false;
            }
            return mTrophyInfo.mUniqueStyleShotsReq <= mLogicMgr.GetUniqueStyleShots() &&
                   mTrophyInfo.mStyleScoreReq <= mLogicMgr.mStyleScore;
        }

        /// <summary>Chooses the end-of-level dialog mode (won, lost, retry, next challenge level...).</summary>
        public void CalcEndLevelDialog()
        {
            bool beaten = mLogicMgr.BeatLevel();
            if (mTrophyInfo != null && (beaten || mTrophyInfo.mNoForceWin))
            {
                beaten = IsCurTrophyBeaten(true);
            }
            int aMode;
            if (mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                aMode = beaten ? 0 : 2;
            }
            else if (mGameMode == GameMode.GAMEMODE_CHALLENGE && mTrophyInfo != null)
            {
                if (!mTrophyInfo.mScoreReset && mTrophyLevelIdx < mTrophyInfo.mLevels.Count - 1)
                {
                    aMode = 4;
                }
                else if (!beaten)
                {
                    mTrophyLives--;
                    aMode = mTrophyLives < 1 ? 2 : 3;
                }
                else
                {
                    aMode = mTrophyLevelIdx < mTrophyInfo.mLevels.Count - 1 ? 5 : 6;
                }
                if (aMode == 2)
                {
                    RecordTrophyResult(false);
                }
                else if (aMode == 6)
                {
                    RecordTrophyResult(true);
                }
            }
            else
            {
                aMode = 1;
            }
            mEndLevelDialog.mType = aMode;
        }

        public void ResizeEndLevelDialog()
        {
            mEndLevelDialog.Resize(mX + BOARD_END_LEVEL_DIALOG_X, BOARD_END_LEVEL_DIALOG_Y, BOARD_END_LEVEL_DIALOG_W, SCREEN_HEIGHT);
        }

        /// <summary>Shows the stage intro dialog for the current level (first attempt only).</summary>
        public void DoStageDialog(bool scrollIn)
        {
            LevelInfo aLevel = mApp.GetCurLevelInfo();
            if (aLevel == null || aLevel.mDialogText.Length == 0 || mLevelPlayCount >= 2)
            {
                return;
            }
            mShowingGameTip = true;
            if (mApp.mPlayerInfo != null)
            {
                if ((mApp.mPlayerInfo.GetmMaxStage() < mApp.mPlayerInfo.mLastAdventureStage || ThunderballApp.BonusMastersFixed(mApp.mPlayerInfo)) &&
                    mLogicMgr.mCharacters[0] != mApp.mCurStage)
                {
                    SyncCharacterOptions(true);
                }
            }
            StageDialog aDialog = new StageDialog(this);
            aDialog.mContentInsets.mBottom /= 2;
            aDialog.Resize(BOARD_STAGE_DIALOG_X + mX, BOARD_STAGE_DIALOG_Y, BOARD_STAGE_DIALOG_W, BOARD_STAGE_DIALOG_H);
            if (scrollIn)
            {
                aDialog.DoScroll(1);
            }
            mApp.AddDialog(aDialog.mId, aDialog);
            mWidgetManager.SetFocus(aDialog);
        }

        /// <summary>Picks the two characters (and the computer opponent) for the current game mode.</summary>
        public void SyncCharacterOptions(bool reroll)
        {
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (aPlayer == null)
            {
                return;
            }
            string aPlayer2Name = aPlayer.mPlayer2Name;
            if (aPlayer2Name.Length == 0)
            {
                aPlayer2Name = SexyAppBase.Loc("Player2");
            }
            int aMaxStage = Math.Min(aPlayer.mMaxStage, 10);
            int aNightsStage = Math.Min(aPlayer.mNightsMaxStage + -0xb, SexyUtil.gCharCount);
            int[] aChars = new int[4];
            aChars[3] = aMaxStage;
            aChars[2] = 0;
            if (aMaxStage < aNightsStage)
            {
                aChars[3] = aNightsStage;
                aChars[2] = 1;
            }
            aChars[2] = aMaxStage < aNightsStage ? 1 : 0;
            for (int i = 0; i < 2; i++)
            {
                aChars[i] = i == 0 ? aPlayer.mCurCharacter : aPlayer.mCurCharacter2;
                if (mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
                {
                    // Port: a bonus Adventure has no Master stage, so its stages keep their master until it
                    // has been beaten.
                    if (aPlayer.GetmMaxStage() < aPlayer.mLastAdventureStage || ThunderballApp.BonusMastersFixed(aPlayer))
                    {
                        aChars[i] = SexyUtil.GetAdventureCharacter(mApp.mCurStage - aPlayer.mFirstAdventureStage, ThunderballApp.mGameLevelPack);
                    }
                }
                else if (mGameMode == GameMode.GAMEMODE_CHALLENGE && mTrophyInfo != null)
                {
                    if (i == 0)
                    {
                        int aChar = mTrophyInfo.GetCharacterId(mTrophyLevelIdx);
                        if (aChar != 0 && aChar > -1)
                        {
                            mTrophyCharacter = true;
                            aChars[0] = aChar;
                        }
                    }
                    else
                    {
                        OpponentInfo anOpponent = mTrophyInfo.GetOpponentInfo(mTrophyLevelIdx);
                        if (anOpponent == null || anOpponent.mCharacterId < 0)
                        {
                            mLogicMgr.mDuelMode = false;
                        }
                        else
                        {
                            mLogicMgr.mDuelMode = true;
                            mLogicMgr.mIsComputer[1] = true;
                            aPlayer2Name = SexyAppBase.Loc("Computer");
                            aChars[i] = anOpponent.mCharacterId;
                            mLogicMgr.mComputerSkill[1] = SkillToAIDepth(anOpponent.mDifficulty);
                        }
                    }
                }
                else if (mGameMode == GameMode.GAMEMODE_DEMO)
                {
                    aChars[i] = 0xb;
                    mLogicMgr.mDuelMode = false;
                    mLogicMgr.mIsComputer[0] = true;
                    mLogicMgr.mComputerSkill[0] = 10;
                }
                if (aChars[i] == 0xb)
                {
                    if (!reroll && mLogicMgr.mCharacters[i] > -1 && mLogicMgr.mCharacters[i] < 0xb)
                    {
                        aChars[i] = mLogicMgr.mCharacters[i];
                    }
                    else
                    {
                        int aPool = aChars[3] + 1;
                        if (ThunderballApp.GetMaxUnlockedCharacter() + 1 < aPool)
                        {
                            aPool = ThunderballApp.GetMaxUnlockedCharacter() + 1;
                        }
                        aPool = SexyUtil.ClampRange(aPool, 1, 0xb);
                        int aTries = aPool < 2 ? 1 : 0x32;
                        for (int t = 0; t < aTries; t++)
                        {
                            int aRand = SexyUtil.Rand();
                            aChars[i] = SexyUtil.GetCharacterForStage(aRand - aRand / aPool * aPool, aChars[2]);
#if !PEGGLE_IPHONE
                            if (aChars[i] > 10)
                            {
                                aChars[i] = 1;
                            }
#endif
                            if (aChars[i] != gLastCharacter[i])
                            {
                                break;
                            }
                        }
                    }
                }
                gLastCharacter[i] = aChars[i];
            }
            mLogicMgr.SetCharacters(aChars[0], aChars[1]);
            CharacterInfo anInfo1 = mCharacterMgr.GetCharacterInfoSafe(aChars[0]);
            CharacterInfo anInfo2 = mCharacterMgr.GetCharacterInfoSafe(aChars[1]);
            mLogicMgr.mPlayerNames[0] = mApp.mPlayerInfo.mName;
            mLogicMgr.mPlayerNames[1] = aPlayer2Name;
            if (mGameMode == GameMode.GAMEMODE_DUEL)
            {
                if (aPlayer.mUnused80.Length != 0)
                {
                    mLogicMgr.mPlayerNames[0] = aPlayer.mUnused80;
                }
                if (!aPlayer.mComputerPlayer)
                {
                    mLogicMgr.mIsComputer[1] = false;
                }
                else
                {
                    mLogicMgr.mIsComputer[1] = true;
                    mLogicMgr.mComputerSkill[1] = SkillToAIDepth(aPlayer.mComputerSkill);
                }
            }
            if (anInfo1 != null)
            {
                mLogicMgr.mPowerupTypes[0] = anInfo1.mPowerup;
            }
            if (anInfo2 != null)
            {
                mLogicMgr.mPowerupTypes[1] = anInfo2.mPowerup;
            }
            mInterfaceMgr.mMarqueesDirty = true;
        }

        private static int SkillToAIDepth(int theSkill)
        {
            switch (theSkill)
            {
                case -1: return -1;
                case 1: return 1;
                case 2: return 10;
                default: return 0;
            }
        }

        public void DoSlotMachineDialog(Ball theBall, PhysObj theHole)
        {
            mSoundMgr.PauseMusic(true);
            SlotMachineDialog aDialog = mSlotMachineDialog;
            aDialog.Init(theBall, theHole);
            int aWidth = aDialog.mWidth;
            aDialog.Resize(Left() + (mWidth - aWidth) / 2, (mHeight - aDialog.mHeight) / 2, aWidth, aDialog.mHeight);
            aDialog.DoScroll(1);
            mWidgetManager.AddWidget(mSlotMachineDialog);
        }

        /// <summary>Loads a .peg replay: a single snapshot (old format) or a counted list of them.</summary>
        public bool LoadReplayFile(string theFileName)
        {
            Buffer aBuffer = new Buffer();
            if (!mApp.ReadBufferFromFile(theFileName, ref aBuffer, true))
            {
                return false;
            }
            DeleteReplays(true);
            DataReader aReader = new DataReader();
            aReader.OpenMemory(aBuffer.GetDataPtr(), aBuffer.GetDataLen(), false);
            int aVersion = aReader.ReadLong();
            if (aVersion < 0x32)
            {
                DataWriter aWriter = new DataWriter();
                aWriter.OpenMemory(aBuffer.GetDataLen());
                aWriter.WriteBytes(aBuffer.GetDataPtr(), 0, aBuffer.GetDataLen());
                mReplayWriters.Add(aWriter);
                return true;
            }
            if (aVersion > 0x53)
            {
                aReader.ReadString();
                aReader.ReadLong();
            }
            int aCount = aReader.ReadShort() & 0xffff;
            for (int i = 0; i < aCount; i++)
            {
                int aLen = aReader.ReadLong();
                if (!aReader.CanReadBytes(aLen))
                {
                    return false;
                }
                DataWriter aWriter = new DataWriter();
                aWriter.OpenMemory(aLen);
                aWriter.WriteBytes(aReader.ReadBytesFromMem(aLen), 0, aLen);
                mReplayWriters.Add(aWriter);
            }
            return true;
        }

        public bool SaveReplayFile(int theId, string theDescription)
        {
            string aFileName = CFormat.StrFormat("userdata/replays/Replay%d.peg", theId);
            DataWriter aWriter = new DataWriter();
            aWriter.OpenMemory(0x20);
            aWriter.WriteLong(0x57);
            aWriter.WriteWString(theDescription);
            aWriter.WriteLong((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            aWriter.WriteShort(1);
            DataWriter aReplay = mReplayWriters.Count != 0 ? mReplayWriters[0] : null;
            if (aReplay != null)
            {
                if (!mLogicMgr.mMuteLoops && mLogicMgr.mState == LogicMgr.STATE_SHOOTING)
                {
                    mLogicMgr.WriteClickTimes();
                }
                aWriter.WriteLong(aReplay.GetDataLen());
                aWriter.WriteBytes(aReplay.GetDataPtr(), 0, aReplay.GetDataLen());
            }
            return mApp.WriteBytesToFile(aFileName, aWriter.GetDataPtr(), aWriter.GetDataLen());
        }

        public static bool EditingLevel()
        {
            return false;
        }

        public override void AddedToManager(WidgetManager theWidgetManager)
        {
            base.AddedToManager(theWidgetManager);
#if PEGGLE_IPHONE
            theWidgetManager.AddWidget(mDialWidget);
#endif
            SetPadLayout(!mApp.IsLeftHanded());
        }

        public override void RemovedFromManager(WidgetManager theWidgetManager)
        {
            base.RemovedFromManager(theWidgetManager);
#if PEGGLE_IPHONE
            theWidgetManager.RemoveWidget(mDialWidget);
#endif
            NotifyRemoving();
        }

        public void NotifyRemoving()
        {
            mSoundMgr.PauseMusic(false);
            WidgetManager aManager = mApp.mWidgetManager;
            aManager.RemoveWidget(mReplayDialog);
            aManager.RemoveWidget(mSlotMachineDialog);
            if (mEndLevelDialog.mWidgetManager == null || !mEndLevelDialog.mVisible || mWidgetManager == null)
            {
                aManager.RemoveWidget(mEndLevelDialog);
            }
            else
            {
                mEndLevelDialog.DoScrollOff(false);
            }
            mApp.KillDialog(DialogId.DIALOG_OPTIONS);
            mApp.KillDialog(DialogId.DIALOG_STAGE);
        }

        public override void GotFocus()
        {
            base.GotFocus();
        }

        public override void LostFocus()
        {
            base.LostFocus();
        }

        public override void SetVisible(bool isVisible)
        {
            base.SetVisible(isVisible);
#if PEGGLE_IPHONE
            mDialWidget.SetVisible(isVisible);
#endif
        }

        /// <summary>Applies the game mode to the next level: which file, the duel/demo setup and characters.</summary>
        public void SyncModeOptions()
        {
            if (mGameMode == GameMode.GAMEMODE_NONE)
            {
                return;
            }
            if (mGameMode == GameMode.GAMEMODE_REPLAY)
            {
                mIsReplay = true;
                mLogicMgr.mMuteLoops = true;
                mReplayDialog.SetMode(true, false, true);
                return;
            }
            if (mGameMode == GameMode.GAMEMODE_DEMO)
            {
                int aStage = -1;
                int aLevel = -1;
                int aMaxStage = mApp.mMaxStage;
                int aMaxLevel = mApp.mMaxLevel;
                int aNightsStage = -1;
                int aNightsLevel = -1;
                if (mApp.mPlayerInfo != null)
                {
                    aMaxStage = mApp.mPlayerInfo.mMaxStage;
                    aMaxLevel = mApp.mPlayerInfo.mMaxLevel;
                    aNightsStage = mApp.mPlayerInfo.mNightsMaxStage;
                    aNightsLevel = mApp.mPlayerInfo.mNightsMaxLevel;
                }
                mApp.mStageMgr.GetRandomLevel(aMaxStage, aMaxLevel, aNightsStage, aNightsLevel, ref aStage, ref aLevel, false, 10, true, false);
                StageInfo aStageInfo = mApp.mStageMgr.GetStageInfo(aStage);
                LevelInfo aLevelInfo = aStageInfo?.GetLevelInfo(aLevel);
                if (aLevelInfo != null)
                {
                    // Port: the stage range, launcher and music follow the level pack, which nothing
                    // has set yet when the demo is the first thing played (the launcher image was
                    // then never made on iPhone and the first draw crashed).
                    mApp.SetLevelPack(StageMgr.GetLevelPackForStage(aStage));
                    mLevelName = ResourceManager.GetResourceDir() + "levels/" + aLevelInfo.mName + ".dat";
                    SyncCharacterOptions(true);
                }
                return;
            }
            if (mGameMode != GameMode.GAMEMODE_CHALLENGE)
            {
                LevelInfo aLevelInfo = mApp.GetCurLevelInfo();
                if (aLevelInfo != null)
                {
                    if (!mEasterEgg)
                    {
                        mLevelName = ResourceManager.GetResourceDir() + "levels/" + aLevelInfo.mName + ".dat";
                    }
                    mLogicMgr.mDuelMode = false;
                    mLogicMgr.mIsComputer[1] = false;
                    mLogicMgr.mIsComputer[0] = false;
                    mLogicMgr.mDuelMode = mGameMode == GameMode.GAMEMODE_DUEL;
                    SyncCharacterOptions(true);
                }
                return;
            }
            mTrophyInfo = mApp.mTrophyMgr.GetTrophyInfoById(mApp.mChallengeId);
            if (mTrophyInfo == null || mTrophyLevelIdx < 0 || mTrophyLevelIdx >= mTrophyInfo.mLevels.Count)
            {
                mApp.mGameMode = GameMode.GAMEMODE_NONE;
                mGameMode = GameMode.GAMEMODE_NONE;
                return;
            }
            mLogicMgr.mDuelMode = false;
            mLogicMgr.mIsComputer[1] = false;
            mLogicMgr.mIsComputer[0] = false;
            string aName = mTrophyInfo.mLevels[mTrophyLevelIdx];
            if (aName.StartsWith("rand", StringComparison.OrdinalIgnoreCase))
            {
                if (mTrophyLevelIdx == 0)
                {
                    mApp.mStageMgr.ResetLastUseTimes();
                }
                bool excludeFlagged = string.Equals(aName, "rand2", StringComparison.OrdinalIgnoreCase);
                LevelInfo aLevelInfo = mApp.mStageMgr.GetRandomLevel(mApp.mPlayerInfo, excludeFlagged, 10, true);
                if (aLevelInfo != null)
                {
                    aName = aLevelInfo.mName;
                }
            }
            else if (string.Equals(aName, "pick", StringComparison.OrdinalIgnoreCase))
            {
                LevelInfo aLevelInfo = mApp.GetCurLevelInfo();
                if (aLevelInfo == null)
                {
                    return;
                }
                aName = aLevelInfo.mName;
            }
            mLevelName = ResourceManager.GetResourceDir() + "levels/" + aName + ".dat";
            SyncCharacterOptions(true);
        }

        /// <summary>Restarts (or advances to) the level for the current mode.</summary>
        public new void Reset()
        {
            SubmitTotalStats();
            CheckIncTip();
            mReplayViews = 0;
            mZoomTaps = 0;
            mGameMode = mApp.mGameMode;
            mZoom = 0;
            mZoomHold = 0;
            mZoomFrame = 0;
            mShakeCount = 0;
            mShakeSmall = false;
            mZoomIn = false;
#if PEGGLE_IPHONE
            mFeverCam = true;
#else
            mFeverCam = false;
#endif
            mSaveGameOnRemove = true;
            mInputEnabled = false;
            mResetCount++;
            mLevelPlayCount++;
            if (mApp.mPlayerInfo != null &&
                (mApp.mGameMode == GameMode.GAMEMODE_ADVENTURE || mApp.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS))
            {
                PlayerInfo aPlayer = mApp.mPlayerInfo;
                aPlayer.SetmAdventureLevelReplay(aPlayer.GetmAdventureLevelReplay() + 1);
                if (aPlayer.GetmAdventureLevelReplay() > 1)
                {
                    aPlayer.SetmAdventureNumRetries(aPlayer.GetmAdventureNumRetries() + 1);
                }
                aPlayer.mDirty = true;
            }
            mShowingGameTip = false;
            mFlagBF = false;
            mFlagC0 = false;
            mStatsSubmitted = false;
            mApp.KillDialog(DialogId.DIALOG_OPTIONS);
            mWidgetManager?.RemoveWidget(mReplayDialog);
            RemoveSlotMachineDialog();
            if (mEndLevelDialog.mWidgetManager == null || !mEndLevelDialog.mVisible)
            {
                mWidgetManager?.RemoveWidget(mEndLevelDialog);
            }
            else
            {
                mEndLevelDialog.DoScrollOff(false);
            }
            SyncModeOptions();
            // Port addition: for the Last Played menu theme (a replay only shows a level).
            if (mGameMode != GameMode.GAMEMODE_NONE && mGameMode != GameMode.GAMEMODE_REPLAY)
            {
                mApp.NoteLevelPlayed(mLevelName);
            }
            if (mLevelName.Length != 0)
            {
                LoadLevel(mLevelName);
                if (!mLogicMgr.mMuteLoops)
                {
                    PlayMusic(true);
                }
            }
            else
            {
                if (!mLogicMgr.mMuteLoops)
                {
                    PlayMusic(true);
                }
                Clear(false);
                SetupLevel();
                mShowZoomHint = false;
                EndPeggleZoom();
                mScorePodsPending = true;
            }
        }

        public bool LoadLevel(string theFileName)
        {
            Brick.mGenImageOnLoad = false;
            bool ok = LoadLevel2(theFileName);
            Brick.mGenImageOnLoad = true;
            return ok;
        }

        public void PlayMusic()
        {
            PlayMusic(false);
        }

        /// <summary>
        /// Starts the mode's level music. Port addition: theFromStart (a level being started or
        /// restarted, see Reset) plays the track from its beginning even when it is already playing.
        /// </summary>
        public void PlayMusic(bool theFromStart)
        {
            mApp.mRestartSameSong = theFromStart;
            // Port addition: get Extreme Fever's Ode to Joy ready now, so it doesn't stall the game
            // while it loads when the last orange peg is hit.
            mApp.mMusicInterface.PreloadMusic(0);
            if (mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS ||
                mGameMode == GameMode.GAMEMODE_QUICKPLAY || mGameMode == GameMode.GAMEMODE_DUEL)
            {
                mApp.PlayLevelMusicTrack(theFromStart);
            }
            else if (mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                mApp.PlayChallengeTrack();
            }
            mApp.mRestartSameSong = false;
        }

        /// <summary>Applies the trophy rules (balls, lives, style shots) and starts the level logic.</summary>
        public void SetupLevel()
        {
            mGameUpdateCnt = 0;
            mRestored = mTrophyCharacter;
            bool isNormal = true;
            if (mTrophyInfo != null)
            {
                if (mResetCount == 1)
                {
                    mLogicMgr.mNumGoalPegs = mTrophyInfo.mNumOrange < 1 ? 0x19 : mTrophyInfo.mNumOrange;
                    mTrophyLives = mTrophyInfo.mLives < 1 ? 1 : mTrophyInfo.mLives;
                }
                if (mTrophyInfo.mBalls == 0)
                {
                    int aBalls = mLogicMgr.mDuelMode ? 6 : 10;
                    mLogicMgr.mBallsLeft[1] = aBalls;
                    mLogicMgr.mBallsLeft[0] = aBalls;
                }
                else if (mResetCount == 1)
                {
                    mLogicMgr.mBallsLeft[0] = mTrophyInfo.mBalls;
                }
                else if (mLogicMgr.mBallsLeft[0] < 1)
                {
                    mLogicMgr.mBallsLeft[0] = 1;
                }
                if (mResetCount == 1 || mTrophyInfo.mScoreReset)
                {
                    mLogicMgr.mScore[1] = 0;
                    mLogicMgr.mScore[0] = 0;
                }
                mLogicMgr.mClearLevelMode = mTrophyInfo.mClearLevel;
                for (int i = 0; i < 0x18; i++)
                {
                    mLogicMgr.mStyleShotsLeft[i] = mTrophyInfo.mStyleShotsReq[i];
                }
                isNormal = false;
            }
            mLogicMgr.InitLevel(isNormal, true, false);
            mInterfaceMgr.InitLevel();
            if (mGun.mTargetAngle != mGun.mAngle)
            {
                mGun.SetAngle(mGun.mTargetAngle, true);
            }
            mCharacterMgr.Resync();
            mInterfaceMgr.SyncStageNum();
            if (mApp.mPlayerInfo != null && !mLogicMgr.mDuelMode)
            {
                string aName = SexyUtil.GetFileName(mLevelName, true);
                if (mApp.mPlayerInfo.GetClearedLevelSet(aName, mApp.mStageMgr).Contains(aName))
                {
                    mFlagBF = true;
                }
                if (mApp.mPlayerInfo.GetAcedLevelSet(aName, mApp.mStageMgr).Contains(aName))
                {
                    mFlagC0 = true;
                }
            }
            MakeThumbnail();
        }

        public void EndPeggleZoom()
        {
            if (mPeggleZoom)
            {
                mPeggleZoom = false;
            }
        }

        public static void MakeThumbnail()
        {
        }

        /// <summary>Reads a level file's objects into theObjs (missing files are fine when optional).</summary>
        public bool LoadLevelBase(string theFileName, List<PhysObj> theObjs, bool isOptional)
        {
            byte[] aData = ContentFS.ReadAllBytes(theFileName);
            if (aData == null)
            {
                return isOptional;
            }
            return LoadLevelBase(aData, theObjs);
        }

        /// <summary>
        /// Reads a level (or the objects of a saved game): a version, the sync type and the
        /// polymorphic object list (Sexy::Board::LoadLevelBase).
        /// </summary>
        public static bool LoadLevelBase(byte[] theData, List<PhysObj> theObjs)
        {
            DataSync aSync = new DataSync();
            DataReader aReader = aSync.StartReadMemory(theData, theData.Length, false);
            int aVersion = aReader.ReadLong();
            if (aVersion > 0x57)
            {
                throw new DataReaderException("Save game version is too new.");
            }
            aSync.SetVersion(aVersion);
            PhysObj.mSyncType = aReader.ReadBool() ? 1 : 0;
            aSync.SyncList(theObjs, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            aSync.SyncPointers();
            return true;
        }

        public static void DrawShadow(Graphics g)
        {
#if PEGGLE_IPHONE
            g.DrawImage(Res.IMAGE_INT_BACKSHADOW, BOARD_SHADOW_X, BOARD_SHADOW_Y);
#endif
        }

        /// <summary>Loads a level: the shared _base objects first, the level background image, then the level.</summary>
        public bool LoadLevel2(string theFileName)
        {
            string aFileName = theFileName;
            mGenericBackground = true;
            Clear(false);
            mLevelName = "";
            string aName = SexyUtil.GetFileName(aFileName, false);
            List<PhysObj> aBaseObjs = new List<PhysObj>();
            List<PhysObj> aLevelObjs = new List<PhysObj>();
            if (!LoadLevelBase(aFileName, aLevelObjs, false))
            {
                Clear(true);
                return false;
            }
            if (aName.Length != 0 && aName[0] == '_')
            {
                mBaseLevel = true;
            }
            else if (!LoadLevelBase(ResourceManager.GetResourceDir() + "levels/_base.dat", aBaseObjs, true))
            {
                Clear(true);
                return false;
            }
            foreach (PhysObj anObj in aBaseObjs)
            {
                anObj.mDrawSort = true;
                AddObj(anObj);
            }
            if (!mBaseLevel)
            {
                string aBackground = "levels/" + GlobalStaticVars.mResPathPrefix + SexyUtil.GetFileName(aName, true);
                if (mTrophyInfo != null && mTrophyInfo.mBackgroundOverride.Length != 0)
                {
                    aBackground = "levels/" + GlobalStaticVars.mResPathPrefix + mTrophyInfo.mBackgroundOverride;
                }
                bool wasCached = false;
                Image anImage = PhysObj.GetImageMgr().GetImage(ResourceManager.GetResourceDir() + aBackground, out wasCached, true);
                if (anImage != null)
                {
                    Poly aPoly = new Poly();
                    aPoly.mImageName = aBackground;
                    aPoly.mBackground = true;
                    aPoly.LoadImage();
                    aPoly.mCollidable = false;
                    aPoly.mImageCached = wasCached;
                    if (aPoly.mImage != null)
                    {
                        mCrackImage = aPoly.mImage;
                        Image aBgImage = aPoly.mImage;
                        aPoly.InitFromRect(0, 0, aBgImage.mWidth, aBgImage.mHeight);
                        aPoly.mNormalDir = -1;
                        int aX;
                        int aY;
                        if (aBgImage.mWidth == BACKGROUND_IMAGE_WIDTH && aBgImage.mHeight == BACKGROUND_IMAGE_HEIGHT)
                        {
                            mGenericBackground = false;
                            aX = 0x145;
                            aY = 0x118;
                        }
                        else
                        {
                            aX = 400 - mBoardRect.mX;
                            aY = 300 - mBoardRect.mY;
                        }
                        aPoly.SetPos(aX, aY);
                    }
                    aPoly.mDrawSort = true;
                    AddObj(aPoly);
                }
            }
            foreach (PhysObj anObj in aLevelObjs)
            {
                anObj.mDrawSort = false;
                AddObj(anObj);
            }
            mLevelName = aFileName;
            if (mApp.mGameMode == GameMode.GAMEMODE_NONE)
            {
                SexyUtil.SetAppTitle(SexyUtil.GetFileName(mLevelName, false));
            }
            else
            {
                SexyUtil.SetAppTitle("");
            }
            SetupLevel();
            CheckForBackground();
            return true;
        }

        /// <summary>Records the level's high score (or the trophy's best result).</summary>
        public void SubmitHighScore()
        {
            if (mLogicMgr.mDuelMode || mLogicMgr.mClearLevelMode ||
                (mLogicMgr.mNumGoalPegs != 0x19 && mLogicMgr.mNumGoalPegs != 0) ||
                mLogicMgr.mPowerupTypesActive[0] != 0 || mGameMode == GameMode.GAMEMODE_DEMO ||
                (mTrophyInfo != null && mTrophyInfo.mLevels.Count > 1 && !mTrophyInfo.mScoreReset))
            {
                return;
            }
            string aLevel = SexyUtil.GetFileName(mLevelName, true);
            if (aLevel.Length == 0)
            {
                return;
            }
            if (mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                if (mTrophyInfo.mNoForceWin || mLogicMgr.ClearedLevel(false))
                {
                    bool hasScoreGoal = mTrophyInfo.mScoreReq != 0 || mTrophyInfo.mScoreCeil != 0;
                    bool hasStyleScore = mTrophyInfo.mStyleScoreReq > 0;
                    bool hasUnique = mTrophyInfo.mUniqueStyleShotsReq > -1;
                    int aTotalReq = 0;
                    int aTotalDone = 0;
                    for (int i = 0; i < 0x18; i++)
                    {
                        aTotalReq += mTrophyInfo.mStyleShotsReq[i];
                        aTotalDone += mTrophyInfo.mStyleShotsReq[i] - mLogicMgr.mStyleShotsLeft[i];
                    }
                    int aGoals = (hasUnique ? 1 : 0) + (aTotalReq > 0 ? 1 : 0) + (hasScoreGoal ? 1 : 0) + (hasStyleScore ? 1 : 0);
                    if (aGoals < 2)
                    {
                        PlayerInfo aPlayer = mApp.mPlayerInfo;
                        if (hasScoreGoal)
                        {
                            aPlayer.CheckTrophyTopScore(mTrophyInfo.mId, mLogicMgr.mScore[0], mTrophyInfo.mScoreCeil > 0);
                        }
                        else if (hasStyleScore)
                        {
                            aPlayer.CheckTrophyTopScore(mTrophyInfo.mId, mLogicMgr.mStyleScore, false);
                        }
                        else if (hasUnique)
                        {
                            aPlayer.CheckTrophyTopScore(mTrophyInfo.mId, mLogicMgr.GetUniqueStyleShots(), false);
                        }
                        else if (aTotalReq > 0)
                        {
                            aPlayer.CheckTrophyTopScore(mTrophyInfo.mId, aTotalDone, false);
                        }
                        else
                        {
                            aPlayer.CheckTrophyTopScore(mTrophyInfo.mId, mLogicMgr.mScore[0], false);
                        }
                    }
                }
                return;
            }
            bool reloadFirst = true;
            LevelInfo aLevelInfo = mApp.mStageMgr.GetLevelInfoByName(aLevel);
            int anAceScore = aLevelInfo != null ? aLevelInfo.mAceScore : -1;
            for (int i = 0; i < 2; i++)
            {
                if (!mLogicMgr.mIsComputer[i])
                {
                    if (mApp.mHighScoreMgr.Submit(aLevel, mLogicMgr.mPlayerNames[i], mLogicMgr.mScore[i], true, reloadFirst, anAceScore))
                    {
                        reloadFirst = false;
                    }
                }
            }
        }

        public void CheckTrophyAccomplishments()
        {
            if (mApp.mPlayerInfo != null && mTrophyInfo != null && mGameMode != GameMode.GAMEMODE_DEMO &&
                VerifyStyleShotsCompleted() && mEndLevelDialog.mType == 6)
            {
                mApp.mPlayerInfo.AwardTrophy(mTrophyInfo.mId);
                mTrophyInfo.mFlag24 = true;
            }
        }

        /// <summary>Marks the level cleared (and awards single-level "clear" trophies for it).</summary>
        public void CheckClearAccomplishment()
        {
            if (mLogicMgr.mDuelMode || mGameMode == GameMode.GAMEMODE_DEMO || !mLogicMgr.mLevelCleared ||
                mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                return;
            }
            string aLevel = SexyUtil.GetFileName(mLevelName, true);
            if (aLevel.Length == 0 || mApp.mPlayerInfo == null)
            {
                return;
            }
            SortedSet<string> aCleared = mApp.mPlayerInfo.GetClearedLevelSet(aLevel, mApp.mStageMgr);
            if (!aCleared.Contains(aLevel))
            {
                aCleared.Add(aLevel);
                mApp.mPlayerInfo.mDirty = true;
            }
            if (mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                foreach (TrophyInfo aTrophy in mApp.mTrophyMgr.mTrophyMap.Values)
                {
                    if (aTrophy.mLevels.Count == 1 && aTrophy.mClearLevel &&
                        string.Equals(aTrophy.mLevels[0], aLevel, StringComparison.OrdinalIgnoreCase))
                    {
                        mApp.mPlayerInfo.AwardTrophy(aTrophy.mId);
                    }
                }
            }
        }

        public void CheckAcedAccomplishment()
        {
            if (mGameMode == GameMode.GAMEMODE_CHALLENGE || mLogicMgr.mDuelMode || mLogicMgr.mClearLevelMode ||
                (mLogicMgr.mNumGoalPegs != 0x19 && mLogicMgr.mNumGoalPegs != 0) ||
                mLogicMgr.mPowerupTypesActive[0] != 0 || mGameMode == GameMode.GAMEMODE_DEMO)
            {
                return;
            }
            if (!mLogicMgr.BeatLevel() || mTrophyInfo != null)
            {
                return;
            }
            string aLevel = SexyUtil.GetFileName(mLevelName, true);
            if (aLevel.Length == 0)
            {
                return;
            }
            LevelInfo aLevelInfo = mApp.mStageMgr.GetLevelInfoByName(aLevel);
            if (aLevelInfo == null || mLogicMgr.mScore[0] < aLevelInfo.mAceScore)
            {
                return;
            }
            if (mApp.mPlayerInfo != null)
            {
                SortedSet<string> anAced = mApp.mPlayerInfo.GetAcedLevelSet(aLevel, mApp.mStageMgr);
                if (!anAced.Contains(aLevel))
                {
                    anAced.Add(aLevel);
                    mApp.mPlayerInfo.mDirty = true;
                }
            }
        }

        /// <summary>Port addition: true while a bonus level (Nights bonus, Extreme) is loaded.</summary>
        public bool IsBonusLevel()
        {
            return mApp.mStageMgr.IsBonusLevel(SexyUtil.GetFileName(mLevelName, true));
        }

        /// <summary>Level over: results, high scores, accomplishments, adventure progress, end dialog.</summary>
        public void DoLevelDone()
        {
            if (mLogicMgr.mDuelMode)
            {
                UpdateTwoPlayerStats();
            }
            CalcEndLevelDialog();
            SubmitHighScore();
            CheckClearAccomplishment();
            if (mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                CheckTrophyAccomplishments();
            }
            CheckAcedAccomplishment();
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (aPlayer != null)
            {
                SubmitTotalStats();
                if ((mGameMode == GameMode.GAMEMODE_ADVENTURE || mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS) && mLogicMgr.BeatLevel())
                {
                    aPlayer.SetmAdventureScore(aPlayer.GetmAdventureScore() + mLogicMgr.mScore[0]);
                    aPlayer.SetmAdventureScore(mLogicMgr.GetAdventureLevelReplayBonus() + aPlayer.GetmAdventureScore());
                    mApp.IncLevel(1);
                    mLevelBeaten = true;
                    if (aPlayer.JustBeatAdventure())
                    {
                        aPlayer.SetmAdventureScore(aPlayer.GetmAdventureScore() + aPlayer.GetAdventureEfficiencyBonus());
                        if (ThunderballApp.mBonusAdventure != null)
                        {
                            // Port: a bonus Adventure's scores stay out of Peggle's adventure high scores.
                            mApp.mHighScoreMgr.Submit("_adventure_" + ThunderballApp.mBonusAdventure.mAdventureKey, aPlayer.mName,
                                aPlayer.GetmAdventureScore(), true, true, -1);
                        }
                        else
                        {
                            mApp.mHighScoreMgr.Submit("_adventure", aPlayer.mName, aPlayer.GetmAdventureScore(), true, true, -1);
                            aPlayer.SubmitAdventureHighScore(aPlayer.GetmAdventureScore());
                        }
                    }
                }
            }
            aPlayer?.SaveIfDirty();
            InitEndLevelDialog(mEndLevelDialog.mType, true);
            mIncTipPending = mEndLevelDialog.mTipText.Length != 0;
        }

        public void UpdateTwoPlayerStats()
        {
            int aWinner = -1;
            if (mLogicMgr.mScore[1] < mLogicMgr.mScore[0])
            {
                mApp.mTwoPlayerWins[0]++;
                aWinner = 0;
            }
            else if (mLogicMgr.mScore[0] < mLogicMgr.mScore[1])
            {
                mApp.mTwoPlayerWins[1]++;
                aWinner = 1;
            }
            if (aWinner == mApp.mTwoPlayerWinner)
            {
                mApp.mTwoPlayerWinStreak++;
            }
            else
            {
                mApp.mTwoPlayerWinner = aWinner;
                mApp.mTwoPlayerWinStreak = 1;
            }
            mApp.mGameStats[0].Add(mLogicMgr.mStats[0]);
            mApp.mGameStats[1].Add(mLogicMgr.mStats[1]);
            if (mApp.mTwoPlayerPoints[0] > 999999999 || mApp.mTwoPlayerPoints[1] > 999999999)
            {
                mApp.mTwoPlayerPoints[0] = 0;
                mApp.mTwoPlayerPoints[1] = 0;
            }
            mApp.mTwoPlayerPoints[0] += mLogicMgr.mScore[0];
            mApp.mTwoPlayerPoints[1] += mLogicMgr.mScore[1];
        }

        /// <summary>Removes theObj; a removed hatted ball drops its hat as an effect, and takes its hat ball along.</summary>
        public void RemoveObj(PhysObj theObj)
        {
            int anIndex = FindObj(theObj, false);
            if (anIndex == mPhysObjs.Count)
            {
                return;
            }
            if (theObj.mShapeType == PhysObj.SHAPE_BALL)
            {
                Ball aBall = (Ball)theObj;
                // Port: the loops belong to the ball (the C++ Ball destructor ended them); without
                // this, a fireball that fell off the board kept its loop playing.
                aBall.StopFireballSound();
                aBall.StopLightningSound();
                int aBallIdx = mBalls.IndexOf(aBall);
                if (aBall.mHasHat && aBallIdx >= 0)
                {
                    float aHatX = aBall.mDrawX + aBall.mHatX - aBall.mX;
                    float aHatY = aBall.mDrawY + aBall.mHatY - aBall.mY;
                    Effect aHat = mEffectMgr.AddEffect(Effect.TYPE_HAT, aHatX, aHatY, 0, false);
                    aHat.mVelX = aBall.mVelX;
                    aHat.mVelY = aBall.mVelY;
                    aHat.mGravity = Ball.mGravity;
                    aHat.mDuration = 0;
                    aHat.mFlags = aBall.mHatBall != null ? 1 : 0;
                }
                if (aBall.mHatBall != null)
                {
                    Ball aHatBall = aBall.mHatBall;
                    aBall.mHatBall = null;
                    RemoveObj(aHatBall);
                }
                aBallIdx = mBalls.IndexOf(aBall);
                if (aBallIdx >= 0)
                {
                    mBalls.RemoveAt(aBallIdx);
                }
            }
            mCollisionMgr.RemoveObj(theObj);
            anIndex = FindObj(theObj, false);
            if (anIndex < mPhysObjs.Count)
            {
                mPhysObjs.RemoveAt(anIndex);
            }
        }

        /// <summary>Index of theObj in the object list (Count if absent); after = the slot just past it.</summary>
        public int FindObj(PhysObj theObj, bool after)
        {
            int anIndex = mPhysObjs.IndexOf(theObj);
            if (anIndex < 0)
            {
                anIndex = mPhysObjs.Count;
            }
            if (after && anIndex != mPhysObjs.Count)
            {
                anIndex++;
            }
            return anIndex;
        }

        public void BeginShot()
        {
            mLeftScorePod.SetRepeats(0);
            mRightScorePod.SetRepeats(0);
        }

        public void EndShot()
        {
            mScorePodsPending = true;
        }

        // Desktop input: the original is touch-only, so mouse input is fed through the touch handlers.
        private int mTouchClickCount;

        /// <summary>Port addition (PC Controls): the right button is held, fast-forwarding like the on-screen button.</summary>
        public bool mRightFFwd;

        /// <summary>
        /// Port addition (PC Controls): the framework passes the right button as theBtnNum 1 (0 is the left);
        /// with PC Controls it only fast-forwards (Update), so the board's touch handling ignores it.
        /// </summary>
        public bool IsPCRightButton(int theBtnNum)
        {
            return theBtnNum == 1 && UsePCControls();
        }

        /// <summary>Port addition (PC Controls): ticks a held left/right arrow waits before repeating, then ticks between repeats (like Windows key repeat).</summary>
        public const int PC_KEY_REPEAT_DELAY = 50;

        public const int PC_KEY_REPEAT_RATE = 3;

        private int mPCLeftHeld;

        private int mPCRightHeld;

        /// <summary>Port addition (PC Controls): 1 when a held key steps this update: when pressed, then repeating.</summary>
        private int PCKeyStep(KeyCode theKey, ref int theHeld)
        {
            if (!mInputEnabled || !mWidgetManager.mKeyDown[(int)theKey])
            {
                theHeld = 0;
                return 0;
            }
            theHeld++;
            if (theHeld == 1 || (theHeld > PC_KEY_REPEAT_DELAY && (theHeld - PC_KEY_REPEAT_DELAY) % PC_KEY_REPEAT_RATE == 0))
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Port addition (PC Controls): left/right turn the gun one wheel step a press, repeating while held,
        /// like the PC games' key-repeat stepping. Once an update, however fast the board runs.
        /// </summary>
        private void UpdatePCArrowSteps()
        {
            if (!UsePCControls())
            {
                mPCLeftHeld = mPCRightHeld = 0;
                return;
            }
            int aLeft = PCKeyStep(KeyCode.Left, ref mPCLeftHeld);
            int aRight = PCKeyStep(KeyCode.Right, ref mPCRightHeld);
            if (aRight != aLeft)
            {
                mLogicMgr.MouseWheel(aRight - aLeft);
            }
        }

        /// <summary>
        /// Port addition (PC Controls): a middle click opens the Peggle zoom where it points, as a double tap
        /// does on the phone, or closes it.
        /// </summary>
        private void PCToggleZoom(int theX, int theY)
        {
            if (mPauseCount != 0 || !mInputEnabled)
            {
                return;
            }
            if (mPeggleZoom)
            {
                EndPeggleZoom();
            }
            else if (!mLogicMgr.mMuteLoops && !mLogicMgr.mIsComputer[mLogicMgr.mCurPlayer] && mLogicMgr.mState == LogicMgr.STATE_AIMING)
            {
                mZoomTaps++;
                mZoomTapX = theX;
                mZoomTapY = theY;
                BeginPeggleZoom();
            }
        }

#if !PEGGLE_IPHONE
        /// <summary>Port addition (HD): the fire and menu buttons are swapped (left-handed without PC Controls).</summary>
        private bool mSwapFireButton;

        /// <summary>
        /// Port addition (HD): whether the Ball-o-tron and fever meter are swapped: left-handed, or always with
        /// PC Controls.
        /// </summary>
        public bool IsLeftHandedLayout()
        {
            return mApp.IsLeftHanded();
        }

        /// <summary>
        /// Port addition (HD): the fire button's x. Swapped, it goes back to the right while the zoom frame is
        /// up, where the frame draws its own fire button.
        /// </summary>
        private int GetFireButtonX()
        {
            return mSwapFireButton && mZoomFrame == 0 ? BOARD_MENU_BUTTON_X : BOARD_FIRE_BUTTON_X;
        }
#endif

        /// <summary>Port addition: whether the PC Controls option is on (never on phones/tablets).</summary>
        public bool UsePCControls()
        {
            return mApp.mPCControls && !Main.IsMobile;
        }

        /// <summary>
        /// Port addition (PC Controls): whether the board point is one of the on-screen controls (wheel,
        /// fast-forward, replay) or beside the board, where hovering doesn't aim and a click doesn't fire.
        /// </summary>
        private bool IsOverPCControl(int theX, int theY)
        {
            if (mFFwdRect.Contains(theX, theY))
            {
                return true;
            }
#if PEGGLE_IPHONE
            return IsMarginTouch(theX);
#else
            return mDialObj.mRect.Contains(theX, theY) || mReplayRect.Contains(theX, theY);
#endif
        }

        /// <summary>Port addition (PC Controls): the gun follows the mouse as if it were always held down.</summary>
        public override void MouseMove(int x, int y)
        {
            base.MouseMove(x, y);
            PCHover(x, y);
        }

        /// <summary>
        /// Port addition (PC Controls): aims at the mouse with no left button down, including while the right
        /// button fast-forwards (unlike a finger, the mouse isn't held on the fast-forward button then).
        /// </summary>
        private void PCHover(int x, int y)
        {
            if (!UsePCControls() || !mInputEnabled || mPauseCount != 0 || IsOverPCControl(x, y))
            {
                return;
            }
            PCAimAt(x, y);
        }

        /// <summary>
        /// Port addition (PC Controls): aims at the board point under the mouse. Zoomed in, the point is mapped back
        /// through DoZoom's transform, so the gun points at the cursor there too (touch zoom turns it by drags).
        /// </summary>
        private void PCAimAt(int x, int y)
        {
            if (mZoom > 0.0f && mZoomActive)
            {
                double aH = SexyUtil.gPhysicalDisplayHeight;
                double aScale = mZoom + 1.0;
                double cx = mZoomX * aH / 600.0 + mX;
                double cy = mZoomY * aH / 600.0;
                x = (int)((x + mX - cx) / aScale + cx) - mX;
                y = (int)((y - cy) / aScale + cy);
            }
            mLogicMgr.MouseDrag(x, y);
        }

        public override void MouseDown(int x, int y, int theBtnNum, int theClickCount)
        {
            if (theBtnNum == 2)
            {
                // Port addition: the middle button (theBtnNum 2) only toggles the zoom, with PC Controls.
                if (UsePCControls())
                {
                    PCToggleZoom(x, y);
                }
                return;
            }
            if (theBtnNum < 0 || IsPCRightButton(theBtnNum))
            {
                return;
            }
            mTouchClickCount = theClickCount;
            _Touch aTouch = default(_Touch);
            aTouch.location = new CGPoint(x, y);
            aTouch.previousLocation = aTouch.location;
            aTouch.tapCount = theClickCount;
            TouchBegan(aTouch);
        }

        public override void MouseDrag(int x, int y)
        {
            // Port addition (PC Controls): moving with only the right button held still aims, and so does any
            // drag while zoomed in (at the cursor, not by the drag distance as on touch).
            if (UsePCControls() && (!mWidgetManager.IsLeftButtonDown() || mPeggleZoom))
            {
                PCHover(x, y);
                return;
            }
            _Touch aTouch = default(_Touch);
            aTouch.location = new CGPoint(x, y);
            aTouch.previousLocation = new CGPoint(mLastTouchX, mLastTouchY);
            aTouch.tapCount = mTouchClickCount;
            TouchMoved(aTouch);
            mLastTouchX = x;
            mLastTouchY = y;
        }

        public override void MouseUp(int x, int y, int theBtnNum, int theClickCount)
        {
            if (theBtnNum < 0 || theBtnNum == 2 || IsPCRightButton(theBtnNum))
            {
                return;
            }
            _Touch aTouch = default(_Touch);
            aTouch.location = new CGPoint(x, y);
            aTouch.previousLocation = aTouch.location;
            aTouch.tapCount = theClickCount;
            TouchEnded(aTouch);
        }

        private int mLastTouchX;

        private int mLastTouchY;

#if PEGGLE_IPHONE
        // The wheel and fire button belong to the DialWidget; the board only handles aiming drags,
        // the fast-forward corner and double-tap zoom.
        /// <summary>Port addition (ultrawide): the touch began beside the board (IsMarginTouch), and whether it pressed the fire button.</summary>
        private bool mMarginTouch;

        private bool mMarginPressed;

        /// <summary>
        /// Port addition: whether a touch at the board's theX is beside it: in the side bands of a screen wider
        /// than the original 960 px, or the window's margins (ThunderballApp.GetUncoveredTouchWidget).
        /// </summary>
        private bool IsMarginTouch(int theX)
        {
            return theX < 0 || theX >= mWidth;
        }

        public override void TouchMoved(_Touch theTouch)
        {
            if ((mFFwdPressed && !mRightFFwd) || mMarginTouch)
            {
                return;
            }
            if (!mPeggleZoom)
            {
                mLogicMgr.MouseDrag((int)theTouch.location.X, (int)theTouch.location.Y);
            }
            else
            {
                mLogicMgr.MouseWheel((float)((theTouch.location.X - theTouch.previousLocation.X) / 4.5));
            }
        }

        public override void TouchBegan(_Touch theTouch)
        {
            int aX = (int)theTouch.location.X;
            int aY = (int)theTouch.location.Y;
            mLastTouchX = aX;
            mLastTouchY = aY;
            if (mUnpauseUpdateCnt == mUpdateCnt)
            {
                return;
            }
            if (mSuspendCount != 0)
            {
                mSuspendCount = 1;
                return;
            }
            if (mPauseCount != 0)
            {
                Pause(false);
                return;
            }
            // Port addition (ultrawide): a touch beside the board is only a tap anywhere (Claude's
            // flippers, skipping); it doesn't aim, zoom or fast-forward.
            mMarginTouch = IsMarginTouch(aX);
            mMarginPressed = false;
            if (mMarginTouch)
            {
                if (mLogicMgr.WantTapsAnywhere())
                {
                    mDialWidget.PressButton();
                    mMarginPressed = true;
                }
                return;
            }
            if (mLogicMgr.WantTapsAnywhere())
            {
                mDialWidget.PressButton();
            }
            if (mFFwdRect.Contains(aX, aY) && mLogicMgr.mState != LogicMgr.STATE_LEVEL_DONE)
            {
                mFFwdPressed = true;
                return;
            }
            if (UsePCControls())
            {
                PCClick(aX, aY);
                return;
            }
            if (!mLogicMgr.mMuteLoops && !mLogicMgr.mIsComputer[mLogicMgr.mCurPlayer] &&
                mLogicMgr.mState == LogicMgr.STATE_AIMING && theTouch.tapCount == 2)
            {
                if (!mPeggleZoom)
                {
                    mZoomTapX = aX;
                    mZoomTapY = aY;
                    BeginPeggleZoom();
                    mZoomTaps++;
                }
                else
                {
                    EndPeggleZoom();
                }
            }
            if (theTouch.tapCount == 1 && !mPeggleZoom)
            {
                mLogicMgr.MouseMove(aX, aY);
            }
        }

        public override void TouchEnded(_Touch theTouch)
        {
            mFFwdPressed = false;
            if (mMarginTouch)
            {
                mMarginTouch = false;
                if (mMarginPressed)
                {
                    mMarginPressed = false;
                    mDialWidget.ReleaseButton();
                }
                return;
            }
            if (mPauseCount != 0)
            {
                return;
            }
            if (!mLogicMgr.WantTapsAnywhere())
            {
                mDialWidget.Reset();
                mLogicMgr.MouseUp((int)theTouch.location.X, (int)theTouch.location.Y, 1, false);
            }
            else
            {
                mDialWidget.ReleaseButton();
            }
        }
#else
        public override void TouchMoved(_Touch theTouch)
        {
            int aX = (int)theTouch.location.X;
            int aY = (int)theTouch.location.Y;
            if ((mFFwdPressed && !mRightFFwd) || mReplayPressed)
            {
                return;
            }
            if (!mDialObj.mRect.Contains(aX, aY) || mZoomFrame != 0)
            {
                if (!mPeggleZoom)
                {
                    mLogicMgr.MouseDrag(aX, aY);
                }
                else
                {
                    mLogicMgr.MouseWheel((float)((theTouch.location.X - theTouch.previousLocation.X) / 4.5));
                }
                return;
            }
            if (mDialObj.mLastY > -1)
            {
                mDialObj.mDelta = mDialObj.mLastY - aY;
                if (!mPeggleZoom)
                {
                    mLogicMgr.MouseWheel(mDialObj.mDelta);
                }
                else
                {
                    mLogicMgr.MouseWheel(mDialObj.mDelta * 0.2f);
                }
                if (Math.Abs(mDialObj.mDelta) < 4)
                {
                    mDialObj.mDelta = 0;
                }
            }
            mDialObj.mLastY = aY;
        }

        public override void TouchBegan(_Touch theTouch)
        {
            int aX = (int)theTouch.location.X;
            int aY = (int)theTouch.location.Y;
            mLastTouchX = aX;
            mLastTouchY = aY;
            if (mUnpauseUpdateCnt == mUpdateCnt)
            {
                return;
            }
            if (mSuspendCount != 0)
            {
                mSuspendCount = 1;
                return;
            }
            if (mPauseCount != 0)
            {
                Pause(false);
                return;
            }
            if (mLogicMgr.WantTapsAnywhere())
            {
                mLogicMgr.MouseDown(0, 0, 1, false, false);
            }
            if (mInputEnabled && mDialObj.mRect.Contains(aX, aY) && mZoomFrame == 0)
            {
                mDialObj.mLastY = aY;
                mDialObj.mDelta = 0;
                mDialObj.mDown = true;
                return;
            }
            if (mReplayRect.Contains(aX, aY) && mReplayButtonState != 2)
            {
                mReplayPressed = true;
                return;
            }
            if (mFFwdRect.Contains(aX, aY) && mLogicMgr.mState != LogicMgr.STATE_LEVEL_DONE)
            {
                mFFwdPressed = true;
                return;
            }
            if (mLogicMgr.mMuteLoops && !mSlowMo)
            {
                SetSlowMo(true, 0xfa);
            }
            if (UsePCControls())
            {
                PCClick(aX, aY);
                return;
            }
            if (!mLogicMgr.mMuteLoops && !mLogicMgr.mIsComputer[mLogicMgr.mCurPlayer] &&
                mLogicMgr.mState == LogicMgr.STATE_AIMING && theTouch.tapCount == 2)
            {
                if (!mPeggleZoom)
                {
                    mZoomTaps++;
                    mZoomTapX = aX;
                    mZoomTapY = aY;
                    BeginPeggleZoom();
                }
                else
                {
                    EndPeggleZoom();
                }
            }
            if (theTouch.tapCount == 1 && !mPeggleZoom)
            {
                mLogicMgr.MouseMove(aX, aY);
            }
        }
#endif

        public void BeginPeggleZoom()
        {
            if (mPeggleZoom)
            {
                return;
            }
            mPeggleZoom = true;
            mZoomX = (float)(mZoomTapX * 600.0 / SexyUtil.gPhysicalDisplayHeight);
            mZoomY = (float)(mZoomTapY * 600.0 / SexyUtil.gPhysicalDisplayHeight);
            mFeverCam = false;
            mShowZoomHint = mApp.mPlayerInfo.SetTip(0xc);
        }

#if !PEGGLE_IPHONE
        public override void TouchEnded(_Touch theTouch)
        {
            int aX = (int)theTouch.location.X;
            int aY = (int)theTouch.location.Y;
            if (mLogicMgr.mMuteLoops && mSlowMo)
            {
                SetSlowMo(false, 0xfa);
            }
            if (mReplayRect.Contains(aX, aY))
            {
                ButtonDepress(BOARD_REPLAY_BUTTON);
                return;
            }
            if (!mDialObj.mRect.Contains(aX, aY))
            {
                mFFwdPressed = false;
                mReplayPressed = false;
                if (mPauseCount == 0)
                {
                    mLogicMgr.MouseUp(aX, aY, 1, false);
                }
            }
            else
            {
                mDialObj.mLastY = -1;
                mDialObj.mDown = false;
                mLogicMgr.MouseUp(0, 0, 1, false);
            }
        }
#endif

        /// <summary>
        /// Port addition (PC Controls): a click on the board aims there and presses the fire button: it fires,
        /// works the flippers, speeds up the level intro... (TouchEnded releases it). There's no
        /// double-tap zoom, as the first click already fires. A tap-anywhere state was handled already.
        /// </summary>
        private void PCClick(int theX, int theY)
        {
            if (mLogicMgr.WantTapsAnywhere() || !mInputEnabled)
            {
                return;
            }
            PCAimAt(theX, theY);
#if PEGGLE_IPHONE
            mDialWidget.PressButton();
#else
            mLogicMgr.MouseDown(theX, theY, 1, false, false);
#endif
        }

        public override void MouseWheel(int theDelta)
        {
            if (mPauseCount == 0)
            {
                // Port: PC Controls turn the gun like Peggle Nights PC, one wheel step (0.1 degrees) per
                // notch; MonoGame reports 120 per notch.
                if (UsePCControls())
                {
                    mLogicMgr.MouseWheel(theDelta / 120.0f);
                    return;
                }
                mLogicMgr.MouseWheel(theDelta);
            }
        }

        public void SetShowBackground(bool show)
        {
            mShowBackground = show;
            foreach (PhysObj anObj in mPhysObjs)
            {
                if (anObj.mBackground && anObj.mImage != null && anObj.mImage.mWidth > 0x118 && anObj.mImage.mHeight > 0x118)
                {
                    anObj.mVisible = mShowBackground;
                    mShowSpaceCracks = mShowBackground;
                }
            }
        }

        public void LoadEasterEggLevel(string theName, int theIndex)
        {
            int aMode = mApp.mGameMode;
            if (aMode == GameMode.GAMEMODE_QUICKPLAY || aMode == GameMode.GAMEMODE_DUEL)
            {
                mLevelName = ResourceManager.GetResourceDir() + "levels/" + theName + ".dat";
                mEasterEgg = true;
                mEasterEggIdx = theIndex;
                Reset();
            }
        }

        /// <summary>A cheat phrase was typed.</summary>
        public void ActivateTypingCheck(int theCheck)
        {
            string aText = "";
            switch (theCheck)
            {
                case 0:
                    if (mLogicMgr.mState == LogicMgr.STATE_AIMING && mGameMode == GameMode.GAMEMODE_QUICKPLAY)
                    {
                        int aPlayer = mLogicMgr.mCurPlayer;
                        bool activate = mLogicMgr.mPowerupTypesActive[aPlayer] != 2;
                        mLogicMgr.mPowerupTypesActive[aPlayer] = activate ? 2 : 0;
                        mLogicMgr.mPowerupCount[2][aPlayer] = activate ? 1000 : 0;
                        mLogicMgr.ActivatePowerup(2, activate);
                    }
                    break;
                case 1:
                    if (mLogicMgr.mState == LogicMgr.STATE_AIMING)
                    {
                        mLogicMgr.SetWearHat(!mLogicMgr.mWearHat);
                    }
                    break;
                case 2:
                    SetShowBackground(!mShowBackground);
                    break;
                case 3:
                    mZoomActive = !mZoomActive;
                    aText = mZoomActive ? SexyAppBase.Loc("Zoom Enabled") : SexyAppBase.Loc("Zoom Disabled");
                    break;
                case 4:
                    mRightClickSpeedup = !mRightClickSpeedup;
                    aText = mRightClickSpeedup ? "RightClick Speedup Enabled" : "RightClick Speedup Disabled";
                    break;
                case 5:
                    LoadEasterEggLevel("seattle", 0);
                    break;
                case 6:
                    LoadEasterEggLevel("piano", 1);
                    break;
                case 7:
                    LoadEasterEggLevel("pitcher", 2);
                    break;
                case 8:
                    LoadEasterEggLevel("tennisracket", 3);
                    break;
            }
            if (aText.Length != 0)
            {
                FloatingText aFloat = mLogicMgr.AddStandardText(aText, mGun.mCenterX, mGun.mCenterY + 90.0f, 0xc);
                aFloat.mDuration = 0x6e;
                aFloat.mColor = 0xffff00;
                aFloat.mJustifyY = -1;
                aFloat.mVelY = 0;
                aFloat.mFadeOut = 10;
                aFloat.mFadeIn = 10;
            }
        }

        public override void KeyChar(SexyChar theChar)
        {
            char aChar = (char)theChar.value_type;
            if (mShowingGameTip || mLogicMgr.KeyChar(aChar) || (mPauseCount != 0 && aChar != ' '))
            {
                return;
            }
            for (int i = 0; i < 9; i++)
            {
                if (mTypingChecks[i].Check(aChar))
                {
                    ActivateTypingCheck(i);
                }
            }
            if (aChar == ' ')
            {
                Pause(mPauseCount == 0);
            }
        }

        /// <summary>
        /// Releasing Enter releases the click it pressed (Peggle Nights PC Board vf49), so Claude's flippers drop
        /// again. (Port: was missing, leaving them up.)
        /// </summary>
        public override void KeyUp(KeyCode theKey)
        {
            if (mShowingGameTip || mPauseCount != 0)
            {
                return;
            }
            if (theKey == KeyCode.Return)
            {
                MouseUp(mWidgetManager.mLastMouseX, mWidgetManager.mLastMouseY, 1);
            }
        }

        public override void KeyDown(KeyCode theKey)
        {
            if (mShowingGameTip || mLogicMgr.KeyDown(theKey) || mPauseCount != 0)
            {
                return;
            }
            if (theKey == KeyCode.Return)
            {
                MouseDown(mWidgetManager.mLastMouseX, mWidgetManager.mLastMouseY, 1);
            }
            else if (UsePCControls())
            {
                // The arrow keys aim in Update while held.
            }
            else if (theKey == KeyCode.Left)
            {
                mLogicMgr.MouseWheel(-1.0f);
            }
            else if (theKey == KeyCode.Right)
            {
                mLogicMgr.DoJimmy(false);
            }
        }

        /// <summary>Sorts the visible objects into the draw layers (foreground objects by y).</summary>
        public void CreateLayers()
        {
            mBackgroundLayer.Clear();
            mMainLayer.Clear();
            mBaseLayer.Clear();
            mBaseLayer2.Clear();
            mLightLayer.Clear();
            List<KeyValuePair<float, PhysObj>> aSorted = new List<KeyValuePair<float, PhysObj>>();
            foreach (PhysObj anObj in mPhysObjs)
            {
                PegInfo aPeg = anObj.mPegInfo;
                if (aPeg != null && (aPeg.mHitCount != 0 || aPeg.mFlashCount != 0 || aPeg.mType == PegType.PEG_SCORE || aPeg.mType == PegType.PEG_POWERUP))
                {
                    mLightLayer.Add(anObj);
                }
                if (!anObj.IsVisible())
                {
                    continue;
                }
                if (anObj.IsBackground())
                {
                    mBackgroundLayer.Add(anObj);
                }
                else if (!anObj.mBaseObject)
                {
                    if (!anObj.mForeground)
                    {
                        if (anObj.mShapeType != PhysObj.SHAPE_BALL)
                        {
                            mMainLayer.Add(anObj);
                        }
                    }
                    else
                    {
                        float aY = anObj.GetYPos();
                        int anIdx = aSorted.Count;
                        while (anIdx > 0 && aSorted[anIdx - 1].Key > aY)
                        {
                            anIdx--;
                        }
                        aSorted.Insert(anIdx, new KeyValuePair<float, PhysObj>(aY, anObj));
                    }
                }
                else if (!anObj.mFlag29)
                {
                    mBaseLayer.Add(anObj);
                }
                else
                {
                    mBaseLayer2.Add(anObj);
                }
            }
            foreach (KeyValuePair<float, PhysObj> anEntry in aSorted)
            {
                mMainLayer.Add(anEntry.Value);
            }
        }

        /// <summary>Pushes the Peggle-zoom transform (when zoomed); false if not zoomed.</summary>
        public bool DoZoom(Graphics g)
        {
            if (mZoom <= 0.0f || !g.Is3D() || !mZoomActive)
            {
                // Port: the High Quality Pegs option keeps the zoom's large peg images.
                PegInfo.mFullSized = PegInfo.mHighQualityPegs;
                mLeftScorePod.SetVisible(true);
                mRightScorePod.SetVisible(true);
                return false;
            }
            float aZoom = mZoom;
            mLeftScorePod.SetVisible(false);
            mRightScorePod.SetVisible(false);
            XnaTransform2D aTrans = new XnaTransform2D();
            float aScale = aZoom * 1.0f;
            int aH = SexyUtil.gPhysicalDisplayHeight;
            aTrans.Translate((float)-((double)(mZoomX * aH) / 600.0 + mX), (float)-((double)(mZoomY * aH) / 600.0));
            aTrans.Scale(aScale + 1.0f, aScale + 1.0f);
            aTrans.Translate((float)((double)(mZoomX * aH) / 600.0 + mX), (float)((double)(mZoomY * aH) / 600.0));
            Graphics.PushTransform(ref aTrans, true);
            PegInfo.mFullSized = true;
            return true;
        }

        public override void Draw(Graphics g)
        {
            if (EditingLevel())
            {
                return;
            }
            if (mShakeCount == 0)
            {
                mShakeSmall = false;
                mShakeX = 0;
                mShakeY = 0;
            }
            else
            {
                if (!mShakeSmall)
                {
                    mShakeX = SexyUtil.Rand() % BOARD_SHAKE_LARGE - BOARD_SHAKE_LARGE / 2;
                    mShakeY = SexyUtil.Rand() % BOARD_SHAKE_LARGE - BOARD_SHAKE_LARGE / 2;
                }
                else
                {
                    mShakeX = SexyUtil.Rand() % BOARD_SHAKE_SMALL - BOARD_SHAKE_SMALL / 2;
                    mShakeY = SexyUtil.Rand() % BOARD_SHAKE_SMALL - BOARD_SHAKE_SMALL / 2;
                }
                g.Translate(mShakeX, mShakeY);
            }
            int aH = SexyUtil.gPhysicalDisplayHeight;
            TRect aClip = mBoardRect;
            aClip.mX = (int)((double)(aClip.mX * aH) / 600.0);
            aClip.mY = (int)((double)(aClip.mY * aH) / 600.0);
            aClip.mWidth = (int)((double)(aClip.mWidth * aH) / 600.0);
            aClip.mHeight = (int)((double)(aClip.mHeight * aH) / 600.0);
            Graphics gBoard = g.Create();
            if (mEndLevelDialog.mWidgetManager != null && mEndLevelDialog.mVisible)
            {
                SexyUtil.DrawDialogFill(g, new TRect(BOARD_CHAR_DIALOG_FILL_X, BOARD_CHAR_DIALOG_FILL_Y, BOARD_CHAR_DIALOG_FILL_W, BOARD_CHAR_DIALOG_FILL_H), 0x120952, 0x170b64);
                DrawShadow(g);
                if (mEndLevelDialog.mY != 0)
                {
                    mCharacterMgr.DrawBack(gBoard, g, this);
                    mCharacterMgr.Draw(g, this);
                }
                mInterfaceMgr.DrawBack(g);
                mInterfaceMgr.DrawFront(g);
#if PEGGLE_IPHONE
                mInterfaceMgr.DrawSidebars(g);
#endif
                return;
            }
            if (mLogicMgr.mState == LogicMgr.STATE_INIT_LEVEL || mLogicMgr.mState == LogicMgr.STATE_CHARACTER_DIALOG)
            {
                if (mApp.GetDialog(DialogId.DIALOG_GAME_TIP) != null || mApp.GetDialog(DialogId.DIALOG_STAGE) != null)
                {
                    Graphics gClip = g.Create();
                    gClip.ClipRect(aClip);
                    gClip.Translate(aClip.mX, (int)((double)(aH * mBoardRect.mY) / 600.0));
                    foreach (PhysObj anObj in mMainLayer)
                    {
                        if (anObj.mShapeType == PhysObj.SHAPE_CIRCLE && anObj.GetEditVal() == null && anObj.mMover != null && anObj.IsMoving())
                        {
                            anObj.Draw(gClip);
                        }
                    }
                    SexyUtil.DrawDialogFill(g, new TRect(BOARD_CHAR_DIALOG_FILL_X, BOARD_CHAR_DIALOG_FILL_Y, BOARD_CHAR_DIALOG_FILL_W, BOARD_CHAR_DIALOG_FILL_H), 0x120952, 0x170b64);
                    DrawShadow(g);
                    mCharacterMgr.DrawBack(gBoard, g, this);
                    mCharacterMgr.Draw(g, this);
                    mInterfaceMgr.DrawBack(g);
                    mInterfaceMgr.DrawFront(g);
#if PEGGLE_IPHONE
                    mInterfaceMgr.DrawSidebars(g);
#endif
                    return;
                }
            }
            DrawBoard(g, gBoard, aClip);
        }

        private void DrawBoard(Graphics g, Graphics gBoard, TRect theClip)
        {
            int aH = SexyUtil.gPhysicalDisplayHeight;
            Graphics gClip = g.Create();
            gClip.ClipRect(theClip);
            gClip.Translate(theClip.mX, (int)((double)(aH * mBoardRect.mY) / 600.0 + 1.0));
            gBoard.Translate(theClip.mX, (int)((double)(aH * mBoardRect.mY) / 600.0));
            CreateLayers();
            int aFadeTime = 10;
            int aBlack = 0;
            bool zoomed = DoZoom(g);
            if (zoomed)
            {
                gClip.mClipRect = new TRect(-10000, -10000, 20000, 20000);
            }
            if (mLogicMgr.mState == LogicMgr.STATE_INIT_LEVEL && mLogicMgr.mStateCount <= aFadeTime)
            {
                g.SetColor(SexyUtil.MakeColor(aBlack));
                g.FillRect(mBoardRect);
            }
            else
            {
                bool fading = false;
                if (mLogicMgr.mState == LogicMgr.STATE_INIT_LEVEL && aFadeTime < mLogicMgr.mStateCount)
                {
                    int aFade = mLogicMgr.mStateCount - aFadeTime;
                    if (aFade < 0x14)
                    {
                        g.SetColor(SexyUtil.MakeColor(aBlack));
                        g.FillRect(mBoardRect);
                        fading = true;
                        gClip.SetColorizeImages(true);
                        gClip.SetColor(SexyUtil.MakeColor(0xffffff, aFade * 0xff / 0x14));
                    }
                }
                if (!fading && (!mShowSpaceCracks || PhysObj.mOutlineMode == 2))
                {
                    g.SetColor(SexyUtil.MakeColor(aBlack));
                    g.FillRect(mBoardRect);
                }
                foreach (PhysObj anObj in mBackgroundLayer)
                {
                    anObj.Draw(gClip);
                }
                gClip.SetColorizeImages(false);
            }
            mLogicMgr.DrawBack(g);
            mEffectMgr.Draw(gBoard, -1);
            g.Is3D();
            DrawShadow(g);
            foreach (PhysObj anObj in mMainLayer)
            {
                anObj.Draw(gClip);
            }
            mEffectMgr.Draw(gBoard, 0);
            mFloatingTextMgr.Draw(gBoard, -1);
            foreach (Ball aBall in mBalls)
            {
                if (aBall.IsVisible())
                {
                    aBall.Draw(gClip);
                }
            }
            float aHitX = mGun.mGuideHitPos.x;
            bool guideDrawn = false;
            if (aHitX < 20.0f || aHitX > 630.0f)
            {
                if (mInputEnabled || mLogicMgr.mIsComputer[mLogicMgr.mCurPlayer])
                {
                    guideDrawn = true;
                    bool aiming = mLogicMgr.mState == LogicMgr.STATE_AIMING || mLogicMgr.mState == LogicMgr.STATE_ZEN_SHOT;
                    mGun.DrawGuide(gBoard, aiming);
                }
            }
            mLogicMgr.DrawFore(gClip);
            foreach (PhysObj anObj in mBaseLayer)
            {
                anObj.Draw(gClip);
            }
            foreach (PhysObj anObj in mLightLayer)
            {
                anObj.DrawLight(gClip);
            }
            mEffectMgr.Draw(gBoard, 1);
            mCharacterMgr.DrawBack(gBoard, g, this);
            if (mCharMoveCount == 0 || !mCharacterMgr.CharMoving())
            {
                mCharacterMgr.Draw(g, this);
            }
            else
            {
                DeferOverlay(2);
            }
#if !PEGGLE_IPHONE
            Board aAIBoard = mAIMgr.mBoard;
            if (aAIBoard.mLogicMgr.mDuelMode && aAIBoard.mLogicMgr.mIsComputer[aAIBoard.mLogicMgr.mCurPlayer] && mReplayButtonState != 2)
            {
                mReplayButtonState = 0;
            }
#endif
            mInterfaceMgr.DrawBack(g);
            mInterfaceMgr.DrawFront(g);
#if PEGGLE_IPHONE
            mInterfaceMgr.DrawSidebars(g);
#endif
            mCharacterMgr.DrawFront(gBoard, g, this);
            if (!guideDrawn && (mInputEnabled || mLogicMgr.mIsComputer[mLogicMgr.mCurPlayer]))
            {
                bool aiming = mLogicMgr.mState == LogicMgr.STATE_AIMING || mLogicMgr.mState == LogicMgr.STATE_ZEN_SHOT;
                mGun.DrawGuide(gBoard, aiming);
            }
            foreach (PhysObj anObj in mBaseLayer2)
            {
                gClip.mClipRect.mHeight += BOARD_CLIP_EXTRA_H;
                anObj.Draw(gClip);
                gClip.mClipRect.mHeight -= BOARD_CLIP_EXTRA_H;
            }
            mFloatingTextMgr.Draw(gBoard, 0);
            mFloatingTextMgr.Draw(gBoard, 1);
            mAIMgr.Draw(gBoard);
            mLogicMgr.Draw(g);
            foreach (Ball aBall in mBalls)
            {
                int aHalf = 100;
                float aMinX = 400 - aHalf - mBoardRect.mX;
                float aMinY = 0x43 - aHalf - mBoardRect.mY;
                if (aBall.CheckRectCollision(aMinX, aMinY, aMinX + aHalf * 2, aMinY + aHalf * 2))
                {
                    aBall.Draw(gBoard);
                }
                if (mLogicMgr.mSuperGuide)
                {
                    gBoard.SetDrawMode(1);
                    if (gBoard.Is3D())
                    {
                        Transform aTrans = new Transform();
                        aTrans.Scale(1.0f, 1.0f);
                        aTrans.RotateRad((float)(mUpdateCnt % 100 * 0.0628));
                        gBoard.DrawImageTransform(Res.IMAGE_YINYANG, aTrans, (float)((double)(aBall.mDrawX * aH) / 600.0),
                            (float)((double)(aBall.mDrawY * aH) / 600.0));
                    }
                    else
                    {
                        Image anImage = Res.IMAGE_YINYANG;
                        gBoard.DrawImageF(anImage, (float)((double)(aBall.mDrawX * aH) / 600.0 - anImage.mWidth / 2.0f),
                            (float)((double)(aBall.mDrawY * aH) / 600.0 - anImage.mHeight / 2.0f));
                    }
                    gBoard.SetDrawMode(0);
                }
            }
#if PEGGLE_IPHONE
            // The ribbons hang on the side away from the dial: one or both, right to left.
            bool aLeftHanded = mApp.IsLeftHanded();
            int aRibbonX;
            if (!mFlagBF || !mFlagC0)
            {
                aRibbonX = aLeftHanded ? BOARD_SINGLE_RIBBON_LHS_OFFSET_X : BOARD_SINGLE_RIBBON_RHS_OFFSET_X;
            }
            else
            {
                aRibbonX = aLeftHanded ? BOARD_DOUBLE_RIBBON_LHS_OFFSET_X : BOARD_DOUBLE_RIBBON_RHS_OFFSET_X;
            }
            if (mFlagBF)
            {
                g.DrawImage(Res.IMAGE_TINYBLUERIBBON, aRibbonX, BOARD_RIBBON_OFFSET_Y);
            }
            if (mFlagC0)
            {
                g.DrawImage(Res.IMAGE_TINYACEDRIBBON, mFlagBF ? aRibbonX - Res.IMAGE_TINYBLUERIBBON.mWidth : aRibbonX, 1);
            }
#else
            if (mFlagBF)
            {
                g.DrawImage(Res.IMAGE_TINYBLUERIBBON, BOARD_CLEARED_RIBBON_OFFSET_X, BOARD_RIBBON_OFFSET_Y);
            }
            if (mFlagC0)
            {
                g.DrawImage(Res.IMAGE_TINYACEDRIBBON, BOARD_ACED_RIBBON_OFFSET_X, 1);
            }
#endif
            if (zoomed)
            {
                Graphics.PopTransform();
            }
            g.Translate(-mShakeX, -mShakeY);
            DeferOverlay(0);
        }

        public override void DrawOverlay(Graphics g)
        {
#if !PEGGLE_IPHONE
            // (The iPhone fire button is drawn by the DialWidget.)
            bool flip = false;
            if (mLogicMgr.mState != LogicMgr.STATE_AIMING)
            {
                flip = mLogicMgr.mPowerupCount[2][mLogicMgr.mCurPlayer] > 0;
            }
            mFireButton.mButtonImage = flip ? Res.IMAGE_INT_FIRE_BUTTON_FLIP_TEXT : Res.IMAGE_INT_FIRE;
#endif
            SexyVector2 aHintPos = new SexyVector2();
            bool showHint = false;
            g.Translate(mShakeX, mShakeY);
            Graphics gBoard = g.Create();
            int aH = SexyUtil.gPhysicalDisplayHeight;
            gBoard.Translate((int)((double)(aH * mBoardRect.mX) / 600.0), (int)((double)(mBoardRect.mY * aH) / 600.0));
            bool zoomed = DoZoom(g);
            mEffectMgr.Draw(gBoard, 2);
            mFloatingTextMgr.Draw(gBoard, 2);
            if (mCharacterMgr.CharMoving())
            {
                mCharacterMgr.Draw(g, this);
            }
            if (zoomed)
            {
                if (mGun.mGuidePoints.Count != 0)
                {
                    aHintPos = mGun.mGuidePoints[mGun.mGuidePoints.Count - 1];
                    if (mLogicMgr.mState == LogicMgr.STATE_AIMING)
                    {
                        aHintPos = new SexyVector2((mBoardRect.mX + aHintPos.x) * aH / 600.0f, (float)((double)((mBoardRect.mY + aHintPos.y) * aH) / 600.0));
                        Microsoft.Xna.Framework.Vector2 aXformed = Microsoft.Xna.Framework.Vector2.Transform(new Microsoft.Xna.Framework.Vector2(aHintPos.x, aHintPos.y), Graphics.GetTransformTop().mMatrix);
                        aHintPos = new SexyVector2(aXformed.X, aXformed.Y);
                        float aScale = mZoom * 1.0f + 1.0f;
                        showHint = true;
                        if (aHintPos.y >= 0.0f && aHintPos.x >= 0.0f)
                        {
                            double aLimit = (mBoardRect.mX + mBoardRect.mWidth) * aScale * aH / 600.0;
                            showHint = !(aHintPos.x < aLimit);
                        }
                    }
                }
                Graphics.PopTransform();
            }
            mEffectMgr.Draw(gBoard, 3);
            mFloatingTextMgr.Draw(gBoard, 3);
            bool frameDrawn = false;
            if (mZoomFrame > 0 && g.Is3D())
            {
                frameDrawn = true;
                float aScale = (0x14 - mZoomFrame) * 0.1f / 20.0f + 1.0f;
                bool pushed = false;
                if (aScale > 1.0f)
                {
                    pushed = true;
                    XnaTransform2D aTrans = new XnaTransform2D();
                    aTrans.Translate(-mWidth / 2, -mHeight / 2);
                    aTrans.Scale(aScale, aScale);
                    aTrans.Translate(mWidth / 2, mHeight / 2);
                    Graphics.PushTransform(ref aTrans, true);
                }
                SexyUtil.DrawScreenFrame(g);
#if PEGGLE_IPHONE
                if (!mFeverCam)
                {
                    if (!mApp.IsLeftHanded())
                    {
                        g.DrawImage(Res.IMAGE_ZOOMCAM_RIGHT, mWidth - Res.IMAGE_ZOOMCAM_RIGHT.mWidth, 0);
                        g.DrawImage(Res.IMAGE_FFWD_BACKING, 0, 0);
                    }
                    else
                    {
                        g.DrawImage(Res.IMAGE_ZOOMCAM, 0, 0);
                        g.DrawImageMirror(Res.IMAGE_FFWD_BACKING, mWidth - Res.IMAGE_FFWD_BACKING.mWidth, 0, true);
                    }
                }
                else
                {
                    g.DrawImage(Res.IMAGE_FEVERCAM, 0, 0);
                }
#else
                if (mFeverCam)
                {
                    g.DrawImage(Res.IMAGE_FEVERCAM, 0, 0);
                }
#endif
                if (pushed)
                {
                    Graphics.PopTransform();
                }
            }
            if (!frameDrawn)
            {
                EndPeggleZoom();
            }
#if PEGGLE_IPHONE
            mInterfaceMgr.DrawSidebars(g);
#endif
            if (mPauseFade != 0)
            {
                if (mSlotMachineDialog.mWidgetManager != null)
                {
                    int aFade = mPauseFade;
                    mPauseFade = 0;
                    Graphics gDialog = g.Create();
                    gDialog.ClipRect(mSlotMachineDialog.GetRect());
                    gDialog.Translate(mSlotMachineDialog.mX, mSlotMachineDialog.mY);
                    mSlotMachineDialog.Draw(gDialog);
                    mPauseFade = aFade;
                }
                if (mApp.mDialogMap.Count == 0 && (!mEndLevelDialog.mVisible || mEndLevelDialog.mWidgetManager == null))
                {
                    g.SetColor(SexyUtil.MakeColor(0, mPauseFade * 100 / 10));
                    g.FillRect(0, 0, mWidth, mHeight);
                    Image aPaused = Res.IMAGE_PAUSED;
                    g.SetColor(SexyUtil.MakeColor(0xffffff, mPauseFade * 0xff / 10));
                    g.SetColorizeImages(true);
                    g.DrawImage(aPaused, (mWidth - aPaused.mWidth) / 2, (mHeight - aPaused.mHeight) / 2);
                    g.SetColorizeImages(false);
                }
            }
            g.Translate(-mShakeX, -mShakeY);
#if PEGGLE_IPHONE
            if (mLogicMgr.mState == LogicMgr.STATE_AIMING || (mLogicMgr.mDimLowObjects && mZoomFrame == 0))
            {
                g.DrawImage(mFFwdPressed ? Res.IMAGE_IP_FFWD_GLOW_BUTTON : Res.IMAGE_IP_FFWD_BUTTON,
                    mFFwdRect.mX + BOARD_FFWD_RECT_EXTRA_X, mFFwdRect.mY + BOARD_FFWD_RECT_EXTRA_Y);
#else
            if ((mLogicMgr.mState == LogicMgr.STATE_AIMING || (mLogicMgr.mDimLowObjects && mZoomFrame == 0)) && !mPeggleZoom)
            {
                if (mFFwdPressed)
                {
                    g.DrawImage(Res.IMAGE_IP_FFWD_GLOW_BUTTON, BOARD_FFWD_RECT_EXTRA_X + mFFwdRect.mX, mFFwdRect.mY + BOARD_FFWD_RECT_EXTRA_Y);
                }
                if (mInterfaceMgr.mButtonCover < 1 && mReplayPressed)
                {
                    g.DrawImage(Res.IMAGE_IP_INSTANTREPLAY_GLOW, BOARD_REPLAY_RECT_EXTRA_X + mReplayRect.mX, mReplayRect.mY + BOARD_REPLAY_RECT_EXTRA_Y);
                }
#endif
                if (showHint)
                {
                    Image aHint = Res.IMAGE_GUIDEHINT;
                    int aX = (int)aHintPos.x;
                    if (mWidth < aX)
                    {
                        aX = mWidth - aHint.mWidth / 2;
                    }
                    if (aX < 0)
                    {
                        aX = aHint.mWidth / 2;
                    }
                    g.DrawImage(aHint, aX - aHint.mWidth / 2, 10);
                }
            }
            if (mPeggleZoom && mShowZoomHint)
            {
                g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
                g.SetColor(SexyUtil.MakeColor(0xffff00));
                g.DrawString(UsePCControls() ? SexyAppBase.Loc("Middle-click to exit Zoom") : SexyAppBase.Loc("Double-tap to exit Zoom"), BOARD_ZOOM_HINT_X, BOARD_ZOOM_HINT_Y);
            }
        }

        /// <summary>Tracks the first ball (or the zoom tap) and eases the Peggle zoom in and out.</summary>
        public void UpdateZoom()
        {
            if (!mPeggleZoom)
            {
                if (mBalls.Count != 0)
                {
                    Ball aBall = mBalls[0];
                    mZoomX = aBall.mDrawX + mBoardRect.mX;
                    mZoomY = aBall.mDrawY + mBoardRect.mY;
                    ClampZoomY();
                }
            }
            else
            {
                ClampZoomY();
            }
            if (!mSlowMo || !mZoomIn)
            {
                if (mZoom > 0.0f)
                {
                    if (!mSlowMo)
                    {
                        mZoomHold = 0;
                        mZoom -= 0.01f;
                    }
                    else if (mZoomHold < 0)
                    {
                        if (!mLogicMgr.mDimLowObjects)
                        {
                            mZoom -= !mFeverCam ? 0.06f : 0.01f;
                        }
                        else
                        {
                            mZoom -= 0.003f;
                        }
                    }
                    else
                    {
                        mZoomHold--;
                    }
                    if (mZoom < 0.0f)
                    {
                        mZoom = 0;
                    }
                }
            }
            else if (mBalls.Count < 2)
            {
                mZoom += !mFeverCam ? 0.06f : 0.02f;
                if (mZoom >= 1.0f)
                {
                    mZoomHold = 10;
                    mZoom = 1.0f;
                    mZoomIn = false;
                }
            }
            else
            {
                mZoomIn = false;
            }
            if (!mZoomActive || (mZoom <= 0.1f && (mZoom <= 0.0f || !mZoomIn)))
            {
                if (mZoomFrame > 0)
                {
                    mZoomFrame--;
                    if (mZoomFrame == 0)
                    {
                        mMenuButton.mDoFinger = true;
                    }
                    if (mReplayDialog.mVisible)
                    {
                        PositionReplayDialog();
                    }
                }
            }
            else
            {
                mMenuButton.mDoFinger = false;
                if (mZoomFrame < 0x14)
                {
                    mZoomFrame++;
                    if (mReplayDialog.mVisible)
                    {
                        PositionReplayDialog();
                    }
                }
            }
        }

        private void ClampZoomY()
        {
            if (mZoomY > 400.0f)
            {
                mZoomY = mZoomY + (mZoomY - 400.0f) * 1.0f;
                if (mZoomY > 0x262)
                {
                    mZoomY = 0x262;
                }
            }
        }

        public void ClearZoom()
        {
            mZoom = 0;
            mZoomHold = 0;
            mZoomFrame = 0;
            PositionReplayDialog();
        }

        /// <summary>For an exploding power fever: whether no other unhit goal peg is within 100 units.</summary>
        public bool CheckExploderForFever(PhysObj thePeg)
        {
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            float aRangeSq = 10000.0f;
            IEnumerable<PhysObj> aPegs = !mLogicMgr.mClearLevelMode ? (IEnumerable<PhysObj>)mLogicMgr.mGoalPegs : mPhysObjs;
            foreach (PhysObj aPeg in aPegs)
            {
                if (!aPeg.mPegInfo.mHit && aPeg.IsVisible())
                {
                    float aDX = aPeg.GetXPos() - aX;
                    float aDY = aPeg.GetYPos() - aY;
                    if (aRangeSq < aDX * aDX + aDY * aDY)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Simulates theBall a few frames ahead: true if it will hit the last orange peg (start the fever zoom).</summary>
        public bool CheckForFever(Ball theBall)
        {
            PhysObj aLastGoal = null;
            PhysObj[] aPowerPegs = new PhysObj[2];
            if (mLogicMgr.GetGoalPegsLeft() == 1)
            {
                aLastGoal = mLogicMgr.GetRemainingGoalPeg();
            }
            int aPower = mLogicMgr.mPowerupTypes[mLogicMgr.mCurPlayer];
            bool checkPowerPegs = false;
            if (aPower == 5)
            {
                checkPowerPegs = true;
            }
            else if (aPower == 9)
            {
                if (!mLogicMgr.mClearLevelMode)
                {
                    if (mLogicMgr.GetGoalPegsLeft() == 1)
                    {
                        checkPowerPegs = true;
                    }
                }
                else if (mLogicMgr.mGoalPegs.Count == 1 && mLogicMgr.mNonGoalPegs.Count < 2)
                {
                    checkPowerPegs = true;
                }
            }
            if (checkPowerPegs)
            {
                foreach (PhysObj aPeg in mLogicMgr.mGoalPegs)
                {
                    if (aPeg.mPegInfo != null && aPeg.mPegInfo.mType == PegType.PEG_POWERUP && !aPeg.mPegInfo.mHit)
                    {
                        if (aPower == 5 && !CheckExploderForFever(aPeg))
                        {
                            continue;
                        }
                        if (aPowerPegs[0] != null)
                        {
                            aPowerPegs[1] = aPeg;
                            break;
                        }
                        aPowerPegs[0] = aPeg;
                    }
                }
            }
            if (aLastGoal == null && aPowerPegs[0] == null && aPowerPegs[1] == null)
            {
                return false;
            }
            Ball aSim = new Ball(false);
            aSim.SetPos(theBall.mX, theBall.mY);
            aSim.SetRadius(theBall.GetRadius());
            aSim.SetVelocity(theBall.mVelX, theBall.mVelY);
            aSim.mPlaySounds = false;
            int aSteps = 4;
            CollisionMgr aMgr = mCollisionMgr;
            aMgr.mIgnorePegs = theBall.mFireball;
            PhysObj.mNotifyCollisionFunc = null;
            aMgr.BeginUpdateOneBall(aSim);
            bool willHit = false;
            int aLimitY = mBoardRect.mHeight + 0x14;
            for (int i = 0; i < aSteps; i++)
            {
                aSim.Update();
                aMgr.UpdateOneBall(aSim, null);
                if (aSim.mY > aLimitY)
                {
                    break;
                }
                if (aLastGoal != null && aSim.CheckCollision(aLastGoal, 1.0f, 10.0f))
                {
                    willHit = true;
                    break;
                }
                for (int p = 0; p < 2; p++)
                {
                    if (aPowerPegs[p] != null && aSim.CheckCollision(aPowerPegs[p], 1.0f, 10.0f))
                    {
                        willHit = true;
                        break;
                    }
                }
                if (willHit)
                {
                    break;
                }
            }
            aMgr.mIgnorePegs = false;
            return willHit;
        }

        /// <summary>Steps every object, the launcher and collisions; balls leaving the board are removed.</summary>
        public void UpdateObjects(bool isExtraStep)
        {
            if (!mFlagBA)
            {
                if (mSlowMo)
                {
                    mSlowMoPending = true;
                }
                foreach (PhysObj anObj in mPhysObjs.ToArray())
                {
                    anObj.Update();
                }
                if (isExtraStep && mLogicMgr.mState != LogicMgr.STATE_SHOOTING)
                {
                    return;
                }
                mGun.Update();
                mCollisionMgr.Update(true);
                bool checkFever = false;
                if (mLogicMgr.mFeverPending)
                {
                    int aPower = mLogicMgr.mPowerupTypes[mLogicMgr.mCurPlayer];
                    if (mLogicMgr.GetGoalPegsLeft() == 1)
                    {
                        checkFever = true;
                    }
                    else if (aPower == 9)
                    {
                        if (mLogicMgr.mClearLevelMode && mLogicMgr.mGoalPegs.Count == 1 && mLogicMgr.mGoalPegs.Count == 1)
                        {
                            checkFever = true;
                        }
                    }
                    else if (aPower == 5 && mLogicMgr.GetGoalPegsLeft() < 6)
                    {
                        checkFever = true;
                    }
                }
                foreach (Ball aBall in mBalls.ToArray())
                {
                    if (!aBall.mInCollideUpdate)
                    {
                        aBall.DoNonCollideUpdate();
                    }
                    float aMinX = aBall.mX - aBall.mRadius;
                    float aMaxX = aBall.mX + aBall.mRadius;
                    float aMinY = aBall.mY - aBall.mRadius;
                    float aMaxY = aBall.mY + aBall.mRadius;
                    if (aMinY > mBoardRect.mHeight + 10 || aMinX > mBoardRect.mWidth + 10 || aMaxX < -10.0f || aMaxY < -10.0f)
                    {
                        RemoveObj(aBall);
                    }
                    else if (checkFever && mBalls.Count < 4 && CheckForFever(aBall))
                    {
                        checkFever = false;
                        if (!mSlowMo)
                        {
                            mLogicMgr.DoFeverSlow();
                        }
                        else if (mZoomHold < 0x14)
                        {
                            mZoomHold = 0x14;
                        }
                    }
                }
                if (mSlowMo)
                {
                    mSlowMoPending = false;
                }
            }
            if (mSlowMo)
            {
                float aFraction = mSlowMoAccum / 1000.0f;
                foreach (PhysObj anObj in mPhysObjs.ToArray())
                {
                    anObj.UpdateSlowMo(aFraction);
                }
            }
            mLogicMgr.CheckCollisions();
        }

        public void Reload()
        {
            Ball aBall = new Ball(false);
            if (mLogicMgr.mHatBall)
            {
                aBall.SetHat(true, true);
            }
            mGun.Reload(aBall);
        }

        public void UpdateMgrs()
        {
            if (!mLogicMgr.mMuteLoops && !mLogicMgr.mZenThinking)
            {
                mAIMgr.Update();
            }
            mLogicMgr.Update();
            if (!mFlagBA)
            {
                mCharacterMgr.CalcLookPos(this, false);
                mCharacterMgr.Update();
                mEffectMgr.Update(mSlowMo);
                mSoundMgr.Update();
            }
            else
            {
                mCharacterMgr.UpdateSlowMo();
                mSoundMgr.UpdateSounds();
            }
            if (mSlowMo)
            {
                mEffectMgr.UpdateSlowMo(mSlowMoRate / 1000.0f);
            }
            mFloatingTextMgr.Update();
            mInterfaceMgr.Update();
        }

        /// <summary>Advances the slow-motion clock: whole steps happen every 1000 accumulated rate units.</summary>
        public void UpdateSlowMo()
        {
            if (!mSlowMo)
            {
                return;
            }
            mSlowMoAccum += mSlowMoRate;
            if (mSlowMoAccum < 1000)
            {
                mFlagBA = true;
            }
            else
            {
                mSlowMoAccum %= 1000;
                mFlagBA = false;
            }
            if (!mSlowMoEnding)
            {
                if (mSlowMoHold > 0 && --mSlowMoHold == 0)
                {
                    mSlowMoEnding = true;
                }
            }
            else
            {
                mSlowMoRate += 0x14;
                if (mSlowMoRate > 899 && mSlowMoRate < 0x398 && mLogicMgr.mFeverPending)
                {
                    mLogicMgr.DoFeverMissed();
                }
                if (mSlowMoRate > 999)
                {
                    SetSlowMo(false, 0xfa);
                }
            }
            if (mSlowMoTargetRate != 0)
            {
                if (mSlowMoTargetRate < mSlowMoRate)
                {
                    mSlowMoRate -= mSlowMoRateStep;
                    if (mSlowMoRate <= mSlowMoTargetRate)
                    {
                        mSlowMoRate = mSlowMoTargetRate;
                        mSlowMoTargetRate = 0;
                    }
                }
                else
                {
                    mSlowMoRate += mSlowMoRateStep;
                    if (mSlowMoTargetRate <= mSlowMoRate)
                    {
                        mSlowMoRate = mSlowMoTargetRate;
                        mSlowMoTargetRate = 0;
                    }
                    if (mSlowMoRate > 999)
                    {
                        SetSlowMo(false, 0xfa);
                    }
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (EditingLevel())
            {
                return;
            }
            if (mDreamTransitionCounter > 0)
            {
                mDreamTransitionCounter--;
                MarkDirty();
                return;
            }
            if (mPauseFade != 0 || mPauseCount != 0)
            {
                if (mApp.mDialogMap.Count != 0)
                {
                    if (mPauseFade != 0)
                    {
                        mPauseFade = 0;
                        MarkDirty();
                    }
                }
                else if (mPauseCount == 0)
                {
                    if (mPauseFade > 0)
                    {
                        mPauseFade--;
                        MarkDirty();
                    }
                }
                else if (mPauseFade < 10)
                {
                    mPauseFade++;
                    MarkDirty();
                }
            }
            if (mAttractDelay != 0 && mGameMode == GameMode.GAMEMODE_DEMO)
            {
                mAttractDelay--;
                MarkDirty();
                return;
            }
            if (mCharMoveCount != 0)
            {
                mCharMoveCount--;
            }
            mInputEnabled = mPauseCount == 0 && mSlotMachineDialog.mWidgetManager == null;
#if !PEGGLE_IPHONE
            if (mFireButton.mX != GetFireButtonX())
            {
                mFireButton.Move(GetFireButtonX(), BOARD_FIRE_BUTTON_Y);
            }
#endif
#if PEGGLE_IPHONE
            if (!mInputEnabled)
            {
                mDialWidget.Reset();
            }
#endif
            int aSteps = 1;
            if (mWidgetManager == null)
            {
                return;
            }
            // Port addition (PC Controls): holding the right button presses the fast-forward button.
            bool aRight = UsePCControls() && mInputEnabled && mWidgetManager.IsRightButtonDown();
            if (aRight != mRightFFwd)
            {
                mRightFFwd = aRight;
                mFFwdPressed = aRight && mLogicMgr.mState != LogicMgr.STATE_LEVEL_DONE;
            }
            if (!mLogicMgr.mMuteLoops || !mReplayDialog.IsFast())
            {
                if (mFFwdPressed)
                {
                    if (mLogicMgr.mState == LogicMgr.STATE_SHOOTING)
                    {
                        if (mRightClickSpeedup)
                        {
                            SetSlowMo(false, 0xfa);
                            aSteps = 4;
                        }
                    }
                    else
                    {
                        SetSlowMo(false, 0xfa);
                        aSteps = 8;
                    }
                }
            }
            else
            {
                SetSlowMo(false, 0xfa);
                mReplayPauseCount = 0;
                aSteps = 4;
            }
            if (mLogicMgr.mState != LogicMgr.STATE_AIMING)
            {
                EndPeggleZoom();
            }
            if (mPeggleZoom)
            {
                SetSlowMo(true, 1000);
                mSlowMoHold = 100;
                mSlowMo = true;
                mZoomIn = true;
            }
            if (mExtraUpdates > 0)
            {
                aSteps = mExtraUpdates + aSteps;
                mExtraUpdates = 0;
            }
            UpdatePCArrowSteps();
            for (int i = 0; i < aSteps; i++)
            {
                if (mWidgetManager == null)
                {
                    continue;
                }
                if (mPauseCount != 0 || mSlotMachineDialog.mWidgetManager != null)
                {
                    mCharacterMgr.UpdatePaused();
                    InterfaceMgr.UpdatePaused();
                    break;
                }
                if (UsePCControls())
                {
                    // Port: PC Controls: held up/down turn the gun five wheel steps a tick (the rate Peggle Nights
                    // PC uses for held left/right), up to the left and down to the right as in the PC games.
                    // Left/right step (UpdatePCArrowSteps).
                    if (mWidgetManager.mKeyDown[(int)KeyCode.Up])
                    {
                        mLogicMgr.MouseWheel(-5.0f);
                    }
                    if (mWidgetManager.mKeyDown[(int)KeyCode.Down])
                    {
                        mLogicMgr.MouseWheel(5.0f);
                    }
                }
                else
                {
                    if (mWidgetManager.mKeyDown[(int)KeyCode.Left])
                    {
                        mLogicMgr.MouseWheel(-5.0f);
                    }
                    if (mWidgetManager.mKeyDown[(int)KeyCode.Right])
                    {
                        mLogicMgr.MouseWheel(5.0f);
                    }
                }
                if (mReplayPauseCount > 0)
                {
                    if (mLogicMgr.mMuteLoops)
                    {
                        mCharacterMgr.UpdatePaused();
                        mReplayPauseCount--;
                        continue;
                    }
                    mReplayPauseCount = 0;
                }
                UpdateSlowMo();
                if (!mFlagBA)
                {
                    mGameUpdateCnt++;
                }
                if (mShakeCount != 0 && mSlotMachineDialog.mWidgetManager == null)
                {
                    mShakeCount--;
                }
                UpdateMgrs();
                if (mLogicMgr.mState == LogicMgr.STATE_AIMING && mScorePodsPending && mInterfaceMgr.mScoreUpdateDelay == 0)
                {
                    mLeftScorePod.Resume();
                    mRightScorePod.Resume();
                    mLeftScorePod.SetRepeats(mLogicMgr.mDuelMode ? -1 : 1);
                    mRightScorePod.SetRepeats(-1);
                    mLeftScorePod.Reset();
                    mRightScorePod.Reset();
                    mScorePodsPending = false;
                }
                if (mLogicMgr.mState != 10)
                {
                    UpdateObjects(i != aSteps - 1);
                }
                UpdateZoom();
                MarkDirty();
            }
            if (mUnpaused)
            {
                mUnpaused = false;
                mMenuButton.mMouseVisible = true;
#if PEGGLE_IPHONE
                mInstantReplayButton.mMouseVisible = true;
#endif
                mReplayDialog.SetMouseVisibility(true);
            }
            bool hasReplay = false;
            if (mLogicMgr.mState == LogicMgr.STATE_AIMING || mLogicMgr.mState == LogicMgr.STATE_LEVEL_DONE)
            {
                hasReplay = mReplayWriters.Count != 0;
            }
#if PEGGLE_IPHONE
            // The instant replay button is live whenever there is a shot to replay.
            if (mInstantReplayButton.mMouseVisible != hasReplay && mPauseCount == 0)
            {
                mInstantReplayButton.SetDisabled(!hasReplay);
                mInstantReplayButton.mMouseVisible = hasReplay;
                MarkDirty();
            }
#else
            mHasReplay = hasReplay;
            if (!mHasReplay)
            {
                mInterfaceMgr.mButtonCover = 10;
            }
#endif
        }

        public void ButtonDepress(int theId)
        {
#if PEGGLE_IPHONE
            if (mZoomFrame != 0)
            {
                return;
            }
            if (theId == BOARD_REPLAY_BUTTON)
            {
                if ((mLogicMgr.mState | 4) == 5)
                {
                    mReplayViews++;
                    if (mReplayWriters.Count != 0 && mReplayDialog.mWidgetManager == null)
                    {
                        mReplayDialog.SetMode(false, mInstantReplayButton.mTouchCancelled, true);
                        if (mLogicMgr.mState == LogicMgr.STATE_LEVEL_DONE)
                        {
                            PlayMusic();
                        }
                        ShowReplay(-1, false);
                        ShowReplayDialog();
                    }
                }
            }
            else if (theId == BOARD_MENU_BUTTON)
            {
                mApp.DoOptionsDialog();
            }
            return;
#endif
            if (mZoomFrame != 0 && theId != BOARD_FIRE_BUTTON)
            {
                return;
            }
            if (theId == BOARD_FIRE_BUTTON)
            {
                if (mInputEnabled)
                {
                    mLogicMgr.MouseUp(0, 0, 1, false);
                }
            }
            else if (theId == BOARD_REPLAY_BUTTON)
            {
                if ((mLogicMgr.mState | 4) == 5)
                {
                    mReplayViews++;
                    if (mReplayWriters.Count != 0 && mReplayDialog.mWidgetManager == null)
                    {
                        if (mLogicMgr.mState == LogicMgr.STATE_LEVEL_DONE)
                        {
                            PlayMusic();
                        }
                        ShowReplay(-1, false);
                        ShowReplayDialog();
                        mReplayButtonState = 2;
                    }
                }
            }
            else if (theId == BOARD_MENU_BUTTON)
            {
                mApp.DoOptionsDialog();
            }
        }

        public void ButtonPress(int theId)
        {
            if ((mZoomFrame == 0 || theId == BOARD_FIRE_BUTTON) && theId == BOARD_FIRE_BUTTON && mInputEnabled)
            {
                mLogicMgr.MouseDown(0, 0, 1, false, false);
            }
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
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

        public bool IsLightningBallActive()
        {
            if (mGun != null && mGun.mBall != null && mGun.mBall.mElectrified)
            {
                return true;
            }
            foreach (Ball aBall in mBalls)
            {
                if (aBall != null && aBall.mElectrified)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The free-ball bucket: the base-level hole with id "freeball".</summary>
        public PhysObj GetBucket()
        {
            foreach (PhysObj anObj in mPhysObjs)
            {
                if (anObj.mDrawSort && anObj.mShapeType == PhysObj.SHAPE_HOLE && anObj.mId == "freeball")
                {
                    return anObj;
                }
            }
            return null;
        }

        public void FinishHighScoreEntryDialog(string theName)
        {
            mEndLevelDialog.FinishHighScoreEntryDialog(theName);
        }
    }
}
