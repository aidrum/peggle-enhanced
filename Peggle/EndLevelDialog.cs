using System;
using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>The scrolling content of the end-of-level panel; draws through EndLevelDialog.DrawContents.</summary>
    public class EndLevelWidget : Widget
    {
        public EndLevelDialog mDialog;                            // 0x78

        public EndLevelWidget(EndLevelDialog theDialog)
        {
            mDialog = theDialog;
        }

        public override void Draw(Graphics g)
        {
            mDialog.DrawContents(g);
        }
    }

    /// <summary>
    /// The end-of-level panel that slides over the board (Sexy::EndLevelDialog): the result header,
    /// counting-up score breakdown and stats, duel results, challenge outcome text, a tip, the level's
    /// high score table, and the follow-up buttons. The Nights trial / store branches are stripped
    /// (the full game is always owned in this port).
    /// </summary>
    public class EndLevelDialog : ThunderDialog
    {
        public const int MODE_LEVEL_COMPLETE = 0;

        public const int MODE_QUICKPLAY_DONE = 1;

        public const int MODE_FAILED = 2;

        public const int MODE_CHALLENGE_LIFE_LOST = 3;

        public const int MODE_CHALLENGE_LEVEL_DONE = 4;

        public const int MODE_CHALLENGE_LEVEL_PASSED = 5;

        public const int MODE_CHALLENGE_WON = 6;

        public ThunderballApp mApp;                               // 0x128

        public EndLevelWidget mContentWidget;                     // 0x12c

        public ThunderScrollWidget mScrollWidget;                 // 0x130

        public Board mBoard;                                      // 0x134

        public int mType;                                         // 0x138

        public ThunderButton mExtraButton;                        // 0x13c

        public ThunderButton mBuyButton;                          // 0x140

        public bool mShowExtraButton;                             // 0x144

        public List<HighScoreEntry> mScores;                      // 0x148

        public bool mShowNoButton;                                // 0x14c

        public bool mShowYesButton;                               // 0x14d

        public int mHighScoreY;                                   // 0x150

        public int mDrawY;                                        // 0x154

        public new int mUpdateCnt;                                // 0x158

        public int mStageProgress;                                // 0x15c

        public int mStage;                                        // 0x160

        public int mLevel;                                        // 0x164

        public int mAdventureWinBottom;                           // 0x168

        public bool mShowAdventureWin;                            // 0x16c

        public bool mJustBeatAdventure;                           // 0x16d

        public int mAdventureCompleteCount;                       // 0x170

        public int mBonusCount;                                   // 0x174

        public string mChallengeText = "";                        // 0x178

        public string mTipText = "";                              // 0x17c

        public string mTrialText = "";                            // 0x180

        public HighScoreEntry mNewScoreEntry;                     // 0x184

        public bool mGrabTrophy;                                  // 0x188

        public bool mTrophyWon;                                   // 0x189

        public int mAceScore;                                     // 0x18c

        public int mTotalScore;                                   // 0x190

        public int mTotalScoreDisp;                               // 0x194

        public int mLevelScore;                                   // 0x198

        public int mLevelScoreDisp;                               // 0x19c

        public int mTryBonus;                                     // 0x1a0

        public int mTryBonusDisp;                                 // 0x1a4

        public int mFeverPoints;                                  // 0x1a8

        public int mFeverPointsDisp;                              // 0x1ac

        public int mStylePoints;                                  // 0x1b0

        public int mStylePointsDisp;                              // 0x1b4

        public int mShotPoints;                                   // 0x1b8

        public int mShotPointsDisp;                               // 0x1bc

        public int mBestShot;                                     // 0x1c0

        public int mFreeBalls;                                    // 0x1c4

        public int mShots;                                        // 0x1c8

        public int mBallsLeft;                                    // 0x1cc

        public int mPctCleared;                                   // 0x1d0

        public int mStatDelay;                                    // 0x1d4

        public int mEfficiencyBonus;                              // 0x1d8

        public int mEfficiencyBonusDisp;                          // 0x1dc

        public int mRetries;                                      // 0x1e0

        public bool mTwoPlayer;                                   // 0x1e4

        public bool mTwoPlayerCountUp;                            // 0x1e5

        public bool mShowStats;                                   // 0x1e6

        public bool mShowHighScores;                              // 0x1e7

        public bool mRaiseNoButton;                               // 0x1e8

        public int mTipIndex;                                     // 0x1ec

        public float mCounterPitch;                               // 0x1f0

        public int mContentHeight;                                // 0x1f4

        public bool mTrial;                                       // 0x1f8 (Nights trial; always false here)

        private static readonly int[] PROGRESS_BAR_SEG_W = { 0, 105, 210, 315, 420, 525 };

        public EndLevelDialog(Board theBoard)
            : base(DialogId.DIALOG_END_LEVEL, false, "", "", "", 2)
        {
            mContentInsets = new Insets(0, 0, 0, END_LEVEL_CONTENT_INSET_H);
            mCloseOnButton = true;
            mBoard = theBoard;
            mApp = mBoard.mApp;
            mContentWidget = new EndLevelWidget(this);
            mScrollWidget = new ThunderScrollWidget();
            mScrollWidget.mDrawTopShadow = false;
            mScrollWidget.mShadowColor = 0xb0e56;
            mScrollWidget.AddWidget(mContentWidget);
            AddWidget(mScrollWidget);
            mExtraButton = SexyUtil.MakeButton(0, this, "");
            mBuyButton = SexyUtil.MakeButton(2, this, "");
            mBuyButton.SetVisible(false);
            mScores = null;
            mNewScoreEntry = null;
            mRaiseNoButton = false;
            mContentHeight = -1;
            mGrabTrophy = false;
            mTrophyWon = false;
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            mExtraButton?.Dispose();
            mBuyButton?.Dispose();
            base.Dispose();
        }

        public override void AddedToManager(WidgetManager theWidgetManager)
        {
            base.AddedToManager(theWidgetManager);
            theWidgetManager.AddWidget(mExtraButton);
            theWidgetManager.AddWidget(mBuyButton);
        }

        public override void RemovedFromManager(WidgetManager theWidgetManager)
        {
            base.RemovedFromManager(theWidgetManager);
            theWidgetManager.RemoveWidget(mExtraButton);
            theWidgetManager.RemoveWidget(mBuyButton);
        }

        private static string Loc(string theText)
        {
            return SexyAppBase.Loc(theText);
        }

        /// <summary>Sets up the panel for one of the seven end-of-level outcomes.</summary>
        public void Init(int theMode, List<HighScoreEntry> theScores)
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            ThunderballApp anApp = mBoard.mApp;
            mScores = theScores;
            mTrial = false;
            mWidth = END_LEVEL_W;
            mNewScoreEntry = null;
            mCounterPitch = 0.0f;
            mStatDelay = 0xf;
            mUpdateCnt = 0;
            mTwoPlayerTickAccum = 100 - TWO_PLAYER_TICK_RATE;
            mStageProgress = 1000;
            bool isAdventure = mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE || mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS;
            mShowAdventureWin = theMode == MODE_LEVEL_COMPLETE && isAdventure;
            mJustBeatAdventure = false;
            mAdventureWinBottom = 0;
            mChallengeText = "";
            if (mShowAdventureWin && anApp.mPlayerInfo != null)
            {
                bool aJustBeat = anApp.mPlayerInfo.JustBeatAdventure();
                int aWins = anApp.mPlayerInfo.GetmAdventureWinCount();
                if (aJustBeat)
                {
                    aWins--;
                }
                mJustBeatAdventure = aJustBeat;
                if (aWins > 0)
                {
                    mShowAdventureWin = false;
                }
            }
            mAdventureCompleteCount = 0;
            TrophyInfo aTrophy = mBoard.mTrophyInfo;
            if (!aLogic.mDuelMode || (aTrophy != null && !aTrophy.mScoreReset))
            {
                mTwoPlayer = false;
                mTwoPlayerCountUp = false;
            }
            else
            {
                mTwoPlayer = true;
                mTwoPlayerCountUp = aTrophy == null;
            }
            mShowStats = isAdventure || mBoard.mGameMode == GameMode.GAMEMODE_QUICKPLAY;
            mShowHighScores = mBoard.mGameMode == GameMode.GAMEMODE_QUICKPLAY && mScores != null;
            InitTip(theMode);
            InitTrialText();
            mStage = anApp.mCurStage;
            mLevel = anApp.mCurLevel;
            if (anApp.mGameMode != GameMode.GAMEMODE_QUICKPLAY && anApp.mGameMode != GameMode.GAMEMODE_DUEL)
            {
                if (mLevel < 1)
                {
                    mStage--;
                    mLevel = 4;
                }
                else
                {
                    mLevel--;
                }
            }
            if (mShowAdventureWin && mStage == -1)
            {
                mStage = 10;
                mLevel = 4;
            }
            mShowExtraButton = false;
            mShowNoButton = true;
            mShowYesButton = true;
            mType = theMode;
            mNoButton.SetVisible(true);
            mYesButton.SetVisible(true);
            ThunderButton aNext = (ThunderButton)mNoButton;
            aNext.StopBlink();
            aNext.mPulseBlink = true;
            aNext.mBlinkPeriod = 0x78;
            if (isAdventure)
            {
                aNext.Blink(1000, true);
            }
            string aComplete = Loc("Level Complete!");
            if (aLogic.mLevelCleared)
            {
                aComplete = Loc("Level Cleared!");
            }
            mYesButton.mLabel = Loc("Main Menu");
            ComputeStats();
            mTrophyWon = anApp.mPlayerInfo != null && anApp.mPlayerInfo.RefreshTrophyWins() > 0;
            mGrabTrophy = mGrabTrophy || mTrophyWon;
            switch (theMode)
            {
            case MODE_LEVEL_COMPLETE:
                mDialogHeader = aComplete;
                if (ThunderballApp.mGameLevelPack == 1 && mGrabTrophy)
                {
                    mNoButton.mLabel = Loc("Grab Trophy!");
                    mYesButton.SetVisible(false);
                }
                else if (anApp.mCurLevel == 0 && anApp.mCurStage == 0)
                {
                    mDialogHeader = Loc("Stage Complete!");
                    mNoButton.mLabel = Loc("Continue");
                    mYesButton.SetVisible(false);
                }
                else
                {
                    if (anApp.mCurLevel == 0)
                    {
                        mDialogHeader = Loc("Stage Complete!");
                        if (!mJustBeatAdventure)
                        {
                            mNoButton.mLabel = Loc("Next Stage");
                        }
                        else
                        {
                            mNoButton.mLabel = Loc("Continue");
                        }
                        mYesButton.SetVisible(false);
                    }
                    else
                    {
                        mNoButton.mLabel = Loc("Next Level");
                        mYesButton.SetVisible(false);
                    }
                    Image aPlay = Res.IMAGE_DLG_PLAYBUTTON3;
                    ThunderButton aButton = (ThunderButton)mNoButton;
                    aButton.mComponentImage = aPlay;                  // 0xf8 (DialogButton draws this one)
                    aButton.mWidth = aPlay.mWidth;
                    aButton.mHeight = aPlay.mHeight / 2;
                    aButton.mNormalRect = new TRect(0, 0, aPlay.mWidth, aPlay.mHeight / 2);
                    aButton.mOverRect = new TRect(0, 0, aPlay.mWidth, aPlay.mHeight / 2);
                    aButton.mDownRect = new TRect(0, aPlay.mHeight / 2, aPlay.mWidth, aPlay.mHeight / 2);
                    mRaiseNoButton = true;
                }
                break;
            case MODE_QUICKPLAY_DONE:
                mShowExtraButton = true;
                mExtraButton.mLabel = Loc("Play New Level");
                if (!aLogic.BeatLevel())
                {
                    mDialogHeader = Loc("Try Again!");
                }
                else
                {
                    mDialogHeader = aComplete;
                }
                mNoButton.mLabel = Loc("Replay Level");
                break;
            case MODE_FAILED:
                if (mBoard.mGameMode == GameMode.GAMEMODE_CHALLENGE && aTrophy != null)
                {
                    mDialogHeader = Loc("Try Again!");
                    mChallengeText = GetChallengeFailText(aTrophy, aLogic);
                    mChallengeText += Loc("Would you like to retry the challenge?");
                    mNoButton.mLabel = Loc("Retry Challenge");
                }
                else
                {
                    mYesButton.SetVisible(false);
                    mDialogHeader = Loc("Try Again!");
                    mChallengeText = "";
                    mNoButton.mLabel = Loc("Retry Level");
                }
                break;
            case MODE_CHALLENGE_LIFE_LOST:
                mDialogHeader = Loc("Continue Challenge?");
                mChallengeText = Loc("Don't let this level stop you.  You can do it!");
                if (aTrophy != null && aTrophy.mLives > 1)
                {
                    if (mBoard.mTrophyLives == 1)
                    {
                        mChallengeText += Loc("\n\nThis is your last life.");
                    }
                    else
                    {
                        mChallengeText += string.Format(Loc("\n\nYou have %d lives left.").Replace("%d", "{0}"), mBoard.mTrophyLives);
                    }
                }
                mYesButton.mLabel = Loc("Restart");
                mNoButton.mLabel = Loc("Continue");
                break;
            case MODE_CHALLENGE_LEVEL_DONE:
            {
                mDialogHeader = Loc("Level Done");
                int aLeft = 0;
                if (aTrophy != null)
                {
                    aLeft = aTrophy.mLevels.Count - mBoard.mTrophyLevelIdx - 1;
                }
                if (aLeft == 1)
                {
                    mChallengeText = Loc("There is 1 level left in this challenge.");
                }
                else
                {
                    mChallengeText = string.Format(Loc("There are %d levels left in this challenge.").Replace("%d", "{0}"), aLeft);
                }
                mYesButton.mLabel = Loc("Restart");
                mNoButton.mLabel = Loc("Continue");
                break;
            }
            case MODE_CHALLENGE_LEVEL_PASSED:
            case MODE_CHALLENGE_WON:
                if (theMode == MODE_CHALLENGE_WON)
                {
                    mDialogHeader = Loc("Way to Go!");
                    mChallengeText = Loc("Yay!  You beat the challenge!");
                    mYesButton.mLabel = Loc("Trophy Menu");
                }
                else
                {
                    mDialogHeader = Loc("Level Passed!");
                    mChallengeText = Loc("You're doing great.  Keep up the excellent work on the next level!");
                    mYesButton.mLabel = Loc("Continue");
                }
                if (ThunderballApp.mGameLevelPack == 1 && mGrabTrophy)
                {
                    mNoButton.mLabel = Loc("Grab Trophy!");
                }
                else
                {
                    mNoButton.SetVisible(false);
                }
                break;
            }
            if (mTwoPlayer)
            {
                mDialogHeader = Loc("Results");
            }
            mShowNoButton = mNoButton.mVisible;
            mShowYesButton = mYesButton.mVisible;
            mExtraButton.SetVisible(mShowExtraButton);
            mContentHeight = GetContentHeight();
            mScrollWidget.ScrollToMin(false);
        }

        /// <summary>Why a challenge was failed, checked in the original's priority order.</summary>
        private string GetChallengeFailText(TrophyInfo theTrophy, LogicMgr theLogic)
        {
            if (theTrophy.mScoreReq >= 1 && theLogic.mScore[0] < theTrophy.mScoreReq)
            {
                return Loc("You didn't get ") + SexyUtil.CommaSeperate(theTrophy.mScoreReq) + Loc(" points.\n\n");
            }
            if (theTrophy.mScoreCeil >= 1 && theTrophy.mScoreCeil < theLogic.mScore[0])
            {
                return Loc("You didn't stay under ") + SexyUtil.CommaSeperate(theTrophy.mScoreCeil) + Loc(" points.\n\n");
            }
            if (theLogic.mDuelMode)
            {
#if PEGGLE_IPHONE
                return Loc("You failed to beat the computer's score");
#else
                return "";
#endif
            }
            if (theTrophy.mClearLevel)
            {
                return Loc("You didn't clear all of the pegs on the level.\n\n");
            }
            if (theTrophy.mLives >= 2)
            {
                return Loc("You ran out of lives.\n\n");
            }
            if (theTrophy.mStyleScoreReq >= 1 && theLogic.mInt16c < theTrophy.mStyleScoreReq)
            {
                return Loc("You didn't get ") + SexyUtil.CommaSeperate(theTrophy.mStyleScoreReq) + Loc(" style points.\n\n");
            }
            if (!theTrophy.mNoForceWin && !theLogic.mDimLowObjects)
            {
                return Loc("You didn't clear all of the orange pegs on the level.\n\n");
            }
            return "";
        }

        /// <summary>Picks a character tip every fifth level (from level 2 on), otherwise a general tip.</summary>
        public void InitTip(int theMode)
        {
            ThunderballApp anApp = mBoard.mApp;
            mTipText = "";
            mTipIndex = -1;
            int aMode = mBoard.mGameMode;
            bool aModeOk = aMode == GameMode.GAMEMODE_ADVENTURE || aMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS || aMode == GameMode.GAMEMODE_QUICKPLAY;
            if (!aModeOk || mShowHighScores || (mType == MODE_LEVEL_COMPLETE && (mShowAdventureWin || mJustBeatAdventure)))
            {
                return;
            }
            PlayerInfo aPlayer = anApp.mPlayerInfo;
            if (aPlayer == null)
            {
                return;
            }
            int aCount = aPlayer.mTotalTipCount;
            if ((uint)aCount % 5 == 0 && aCount != 0 && aPlayer.GetMaxLevel() > 1)
            {
                int aChar = mBoard.mLogicMgr.mCharacters[0] % 10;
                mTipIndex = aChar;
                mTipText = anApp.mCharacterMgr.GetCharacterTip(aChar, (uint)aPlayer.mTipCounts[aChar]);
            }
            if (mTipText.Length == 0)
            {
                mTipIndex = -1;
                mTipText = anApp.mStageMgr.GetTip((uint)aPlayer.mOtherTipCount);
            }
        }

        /// <summary>The Nights trial upsell lines. The port always owns the full game, so this stays empty.</summary>
        public void InitTrialText()
        {
            mTrialText = "";
        }

        public void ComputeStats()
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            GameStats aStats = aLogic.mStats[0];
            mBonusCount = 0;
            mAceScore = 0;
            StageInfo aStage = mApp.mStageMgr.GetStageInfo(mStage);
            if (aStage != null)
            {
                LevelInfo aLevel = aStage.GetLevelInfo(mLevel);
                if (aLevel != null)
                {
                    mAceScore = aLevel.mAceScore;
                }
            }
            mTotalScoreDisp = 0;
            mLevelScoreDisp = 0;
            mTryBonusDisp = 0;
            mFeverPointsDisp = 0;
            mShotPointsDisp = 0;
            mStylePointsDisp = 0;
            mEfficiencyBonusDisp = 0;
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            int anAdventureScore = aPlayer != null ? aPlayer.GetmAdventureScore() : 0;
            mEfficiencyBonus = 0;
            mRetries = 0;
            if (mJustBeatAdventure && aPlayer != null)
            {
                mEfficiencyBonus = aPlayer.GetAdventureEfficiencyBonus();
                mRetries = aPlayer.GetmAdventureNumRetries();
            }
            mLevelScore = aLogic.mScore[0] - aLogic.mInt37c[0];
            mTryBonus = aLogic.GetAdventureLevelReplayBonus();
            mTotalScore = anAdventureScore;
            mTotalScoreDisp = anAdventureScore - mLevelScore - mTryBonus - mEfficiencyBonus;
            mFeverPoints = aStats.mBestValue;
            mBestShot = aStats.mBestShot;
            mShots = aStats.mShots;
            if (aLogic.mList394.Count > 0)
            {
                mPctCleared = aLogic.mInt17c * 100 / aLogic.mList394.Count;
            }
            mFreeBalls = aStats.GetTotalFreeBalls();
            mBallsLeft = aLogic.mBallsLeft[0];
            mStylePoints = aStats.GetTotalStylePoints();
            mShotPoints = mLevelScore - mFeverPoints - mStylePoints;
            if (!aLogic.BeatLevel() || (mBoard.mGameMode != GameMode.GAMEMODE_ADVENTURE && mBoard.mGameMode != GameMode.GAMEMODE_ADVENTURE_NIGHTS))
            {
                mTotalScoreDisp = mTotalScore;
                mLevelScoreDisp = mLevelScore;
                mTryBonusDisp = mTryBonus;
                mFeverPointsDisp = mFeverPoints;
                mShotPointsDisp = mShotPoints;
                mStylePointsDisp = mStylePoints;
            }
        }

        /// <summary>Measures the content by drawing it into a fully clipped context.</summary>
        public int GetContentHeight()
        {
            Graphics g = Graphics.GetNew();
            int anOldCnt = mUpdateCnt;
            mUpdateCnt = 0xffff;
            g.SetClipRect(0, 0, 0, 0);
            int anOldComplete = mAdventureCompleteCount;
            if (mJustBeatAdventure)
            {
                mAdventureCompleteCount = 0xffff;
            }
            DrawContents(g);
            mUpdateCnt = anOldCnt;
            mAdventureCompleteCount = anOldComplete;
            g.PrepareForReuse();
            return mDrawY;
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int aButtonW = END_LEVEL_BUTTON_W;
            if (mNoButton != null && !mNoButton.mVisible)
            {
                mYesButton.Resize(mX + mWidth / 2 - aButtonW / 2, mYesButton.mY, aButtonW, mYesButton.mHeight);
            }
            if (mYesButton != null && mNoButton != null && !mYesButton.mVisible)
            {
                int aRaise = mRaiseNoButton ? END_LEVEL_GAP_H : 0;
                mNoButton.Resize(mX + mWidth / 2 - aButtonW / 2, mYesButton.mY - aRaise, aButtonW, mYesButton.mHeight);
            }
            if (mShowExtraButton)
            {
                mExtraButton.Layout(0x1103, mYesButton, 0, -5, 0, 0);
                mExtraButton.Layout(0x10000, mNoButton, 0, 0, 0, 0);
                int aY = mExtraButton.mY;
                mExtraButton.mY = mYesButton.mY;
                mYesButton.mY = aY;
                if (mNoButton != null)
                {
                    mNoButton.mY = aY;
                }
                int anExtraW = END_LEVEL_EXTRA_BUTTON_W;
                mExtraButton.Resize(mX + mWidth / 2 - anExtraW / 2, mExtraButton.mY, anExtraW, mExtraButton.mHeight);
            }
            int aBottom = SCREEN_HEIGHT;
            if (mYesButton.mVisible)
            {
                aBottom = Math.Min(aBottom, mYesButton.mY);
            }
            if (mNoButton.mVisible)
            {
                aBottom = Math.Min(aBottom, mNoButton.mY);
            }
            if (mExtraButton.mVisible)
            {
                aBottom = Math.Min(aBottom, mExtraButton.mY);
            }
            if (mBuyButton.mVisible)
            {
                aBottom = Math.Min(aBottom, mBuyButton.mY);
            }
            int aScrollW = END_LEVEL_SCROLL_W;
            int aScrollH = aBottom - END_LEVEL_SCROLL_Y;
            mContentWidget.Resize(0, 0, aScrollW, mContentHeight);
            mScrollWidget.Resize(theWidth / 2 - aScrollW / 2, END_LEVEL_SCROLL_Y, aScrollW, aScrollH - END_LEVEL_SCROLL_GAP_H);
        }

        public void DrawContents(Graphics g)
        {
            mDrawY = 0;
            if (!mShowAdventureWin)
            {
                mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Vertical);
#if PEGGLE_IPHONE
                mScrollWidget.mDrawBottomShadow = true;
#else
                mScrollWidget.mDrawBottomShadow = false;
#endif
                if (mAdventureCompleteCount != 0)
                {
                    DrawAdventureComplete(g);
                }
            }
            else if (mAdventureCompleteCount == 0)
            {
#if !PEGGLE_IPHONE
                // (iPhone: DrawAdventureWin disables scrolling itself, only for the final stage.)
                mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Disabled);
#endif
                mScrollWidget.mDrawBottomShadow = false;
                DrawAdventureWin(g);
            }
            else
            {
                mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Vertical);
#if PEGGLE_IPHONE
                mScrollWidget.mDrawBottomShadow = true;
#else
                mScrollWidget.mDrawBottomShadow = false;
#endif
                DrawAdventureComplete(g);
            }
            if (mTwoPlayer)
            {
                DrawTwoPlayerResult(g);
            }
            if (mShowStats)
            {
                DrawStats(g);
            }
            if (mTrialText.Length != 0)
            {
                DrawTrialText(g);
            }
            if (mChallengeText.Length != 0)
            {
                DrawChallengeResult(g);
            }
            if (mTipText.Length != 0)
            {
                DrawTip(g);
            }
            if (mShowHighScores)
            {
                DrawHighScores(g);
            }
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return SCREEN_HEIGHT;
        }

        public override void SetVisible(bool isVisible)
        {
            if (mVisible == isVisible)
            {
                return;
            }
            base.SetVisible(isVisible);
            if (!isVisible || mShowYesButton)
            {
                mYesButton.SetVisible(isVisible);
            }
            if (!isVisible || mShowNoButton)
            {
                mNoButton.SetVisible(isVisible);
            }
        }

        public override void ButtonDepress(int theId)
        {
            if (mScores != null)
            {
                foreach (HighScoreEntry anEntry in mScores)
                {
                    anEntry.mIsNew = false;
                }
            }
            ThunderballApp anApp = mBoard.mApp;
            if (theId != 2)
            {
                anApp.StopMusic();
            }
            if (theId < 1000)
            {
                switch (theId)
                {
                case 0:
                    if (mBoard.mGameMode == GameMode.GAMEMODE_CHALLENGE)
                    {
                        mApp.ShowTrophyScreen(false);
                    }
                    else
                    {
                        if (mBoard.mGameMode == GameMode.GAMEMODE_QUICKPLAY)
                        {
                            anApp.mPlayerInfo?.DeleteSavedGame(GameMode.GAMEMODE_QUICKPLAY, -1);
                        }
                        mApp.ShowLevelScreen(false, false, false);
                    }
                    break;
                case 1:
                    mBoard.DoCharacterDialog(false);
                    break;
                case 2:
                case 3:
                    // Store / trial follow-ups are not part of this port.
                    break;
                }
                return;
            }
            if (theId == 1000)
            {
                if (mType == MODE_CHALLENGE_LIFE_LOST || mType == MODE_CHALLENGE_LEVEL_DONE)
                {
                    anApp.DoConfirmRestartLevelDialog(mY);
                    return;
                }
                mWidgetManager?.RemoveWidget(this);
                if (mType == MODE_CHALLENGE_LEVEL_PASSED)
                {
                    mBoard.mTrophyLevelIdx++;
                    mBoard.Reset();
                }
                else if (mType == MODE_CHALLENGE_WON)
                {
                    anApp.ShowTrophyScreen(false);
                }
                else
                {
                    if (mBoard.mGameMode == GameMode.GAMEMODE_QUICKPLAY)
                    {
                        anApp.mPlayerInfo?.DeleteSavedGame(GameMode.GAMEMODE_QUICKPLAY, -1);
                    }
                    anApp.ShowMainMenu();
                }
                return;
            }
            if (theId != 1001)
            {
                return;
            }
            if (mBoard.mGameMode == GameMode.GAMEMODE_CHALLENGE)
            {
                if (mType == MODE_FAILED)
                {
                    mBoard.mResetCount = 0;
                    mBoard.mTrophyLevelIdx = 0;
                }
                else if (mType == MODE_CHALLENGE_LEVEL_DONE)
                {
                    mBoard.mTrophyLevelIdx++;
                }
            }
            else if (ThunderballApp.mBonusAdventure != null && (mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE ||
                mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS))
            {
                // Port addition: a bonus Adventure (Extreme, WoW) has no trophy room or progress screen
                // between stages: a new stage (or the win) goes straight to its story screen.
                PlayerInfo aPlayer = anApp.mPlayerInfo;
                if (aPlayer != null && aPlayer.GetmNeedShowStory() && mType == MODE_LEVEL_COMPLETE)
                {
                    anApp.ShowStoryScreen(false, false);
                    FinishScroll();
                    return;
                }
            }
            else if (mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE)
            {
                PlayerInfo aPlayer = anApp.mPlayerInfo;
                if (anApp.mCurLevel == 0 && anApp.mCurStage < aPlayer.mLastAdventureStage + 1 &&
                    aPlayer.GetmMaxStage() == anApp.mCurStage && mType == MODE_LEVEL_COMPLETE)
                {
                    anApp.ShowTrophyRoom(2);
                    FinishScroll();
                    return;
                }
                if (aPlayer != null && aPlayer.GetmNeedShowStory() && mType == MODE_LEVEL_COMPLETE)
                {
                    anApp.ShowStoryScreen(false, false);
                    FinishScroll();
                    return;
                }
            }
            else if (mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                PlayerInfo aPlayer = anApp.mPlayerInfo;
                if (aPlayer == null || !aPlayer.GetmNeedShowStory() || mType != MODE_LEVEL_COMPLETE)
                {
                    if (mGrabTrophy)
                    {
                        if (!mTrophyWon)
                        {
                            anApp.ShowProgressScreen(1);
                        }
                        else
                        {
                            anApp.ShowStoryScreen(true, false);
                        }
                        FinishScroll();
                    }
                }
                else
                {
                    if (aPlayer.GetmAdventureWinCount() == 0 || mGrabTrophy)
                    {
                        if (mStage == 0x16)
                        {
                            if (!mTrophyWon)
                            {
                                anApp.ShowProgressScreen(2);
                            }
                            else
                            {
                                anApp.ShowStoryScreen(true, false);
                            }
                            FinishScroll();
                            return;
                        }
                        if (!mTrophyWon)
                        {
                            anApp.ShowProgressScreen(1);
                            FinishScroll();
                            return;
                        }
                        anApp.ShowStoryScreen(true, false);
                    }
                    else
                    {
                        anApp.ShowStoryScreen(false, false);
                    }
                    FinishScroll();
                }
            }
            if (mBoard.mGameMode == GameMode.GAMEMODE_QUICKPLAY)
            {
                anApp.mPlayerInfo?.DeleteSavedGame(GameMode.GAMEMODE_QUICKPLAY, -1);
            }
#if PEGGLE_IPHONE
            mWidgetManager?.RemoveWidget(this);
#endif
            mBoard.Reset();
        }

        /// <summary>Gives the name typed into the high score entry dialog to the new score.</summary>
        public void FinishHighScoreEntryDialog(string theName)
        {
            if (mScores == null)
            {
                return;
            }
            foreach (HighScoreEntry anEntry in mScores)
            {
                if (anEntry.mIsNew)
                {
                    anEntry.mName = theName;
                }
            }
            mApp.mHighScoreMgr.Save();
        }

        private static int GetScoreInc(int theCur, int theTarget, int theStep)
        {
            int aNew = theCur + theStep;
            if (aNew > theTarget)
            {
                aNew = theTarget;
            }
            return aNew - theCur;
        }

        private const int TWO_PLAYER_TICK_RATE = 30;

        /// <summary>Port addition: starts at 100 - rate so the first update ticks.</summary>
        private int mTwoPlayerTickAccum = 100 - TWO_PLAYER_TICK_RATE;

        /// <summary>Counts the score breakdown up with the ticking counter sound, feeding the total marquee.</summary>
        public void UpdateStats()
        {
            if (mShowAdventureWin)
            {
                if (mUpdateCnt < 0x1e)
                {
                    return;
                }
                int anIndex = mStage * 5 + mLevel;
                if (mStageProgress <= anIndex)
                {
                    if (mUpdateCnt % 2 == 0)
                    {
                        mStageProgress++;
                    }
                    return;
                }
            }
            if (mAdventureCompleteCount != 0)
            {
                mAdventureCompleteCount++;
            }
            if (mJustBeatAdventure && mBonusCount > 0 && mBonusCount < 0x32)
            {
                mBonusCount++;
                if (mBonusCount == 0x32)
                {
                    GlobalStaticVars.gSexyAppBase.PlaySample(Res.SOUND_TING);
                    mAdventureCompleteCount = 1;
                }
            }
            if (mTwoPlayer && mUpdateCnt < 200)
            {
                // Port change (user request): the count-up tick plays at most
                // TWO_PLAYER_TICK_RATE times a second instead of on every 100 Hz update.
                mTwoPlayerTickAccum += TWO_PLAYER_TICK_RATE;
                if (mTwoPlayerTickAccum >= 100)
                {
                    mTwoPlayerTickAccum -= 100;
                    mBoard.mSoundMgr.AddSound(Res.SOUND_SCORECOUNTER);
                }
            }
            if (mStatDelay >= 1)
            {
                mStatDelay--;
                return;
            }
            int[][] aPairs = null;
            int anInc = 0;
            int aWhich;
            int[] aDisp = { mShotPointsDisp, mFeverPointsDisp, mStylePointsDisp, mTryBonusDisp, mEfficiencyBonusDisp };
            int[] aTarget = { mShotPoints, mFeverPoints, mStylePoints, mTryBonus, mEfficiencyBonus };
            for (aWhich = 0; aWhich < 5; aWhich++)
            {
                if (aDisp[aWhich] != aTarget[aWhich])
                {
                    anInc = GetScoreInc(aDisp[aWhich], aTarget[aWhich], aWhich == 4 ? 100000 : 0x5dc);
                    break;
                }
            }
            _ = aPairs;
            if (anInc == 0)
            {
                return;
            }
            switch (aWhich)
            {
            case 0: mShotPointsDisp += anInc; break;
            case 1: mFeverPointsDisp += anInc; break;
            case 2: mStylePointsDisp += anInc; break;
            case 3: mTryBonusDisp += anInc; break;
            case 4: mEfficiencyBonusDisp += anInc; break;
            }
            mTotalScoreDisp += anInc;
            if (aWhich < 3)
            {
                mLevelScoreDisp += anInc;
                if (mLevelScoreDisp == mLevelScore)
                {
                    mStatDelay = 0x32;
                }
            }
            else if (mBonusCount == 0 && mTryBonusDisp == mTryBonus)
            {
                mBonusCount = 1;
                mStatDelay = 100;
                mCounterPitch = 0.0f;
            }
            if (mUpdateCnt % 10 == 0)
            {
                mCounterPitch += 1.0f;
                mBoard.mSoundMgr.AddSoundPitch(Res.SOUND_SCORECOUNTER, mCounterPitch);
            }
            mBoard.mInterfaceMgr.mAdventureScore = mTotalScoreDisp;
            mBoard.mInterfaceMgr.mMarqueesDirty = true;
            mBoard.MarkDirty();
        }

        public override void Update()
        {
            base.Update();
            if (mVisible && !IsScrolling() && mWidgetManager != null)
            {
                UpdateStats();
                mUpdateCnt++;
            }
            if (mScores != null)
            {
                MarkDirty();
            }
        }

        /// <summary>The level's top ten, with the new entry cycling through colours.</summary>
        public void DrawHighScores(Graphics g)
        {
            if (mScores == null)
            {
                return;
            }
            g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            int y = mDrawY;
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.FillRect(END_LEVEL_STATS_X1A_X, y, END_LEVEL_STATS_X1B_X - END_LEVEL_STATS_X1A_X, 1);
            y += g.GetFont().GetMaxCharHeight();
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            WriteCenteredLine(g, y, Loc("New High Score!"));
            y += g.GetFont().GetMaxCharHeight();
            int aRank = 1;
            int aRankX = END_LEVEL_HIGHSCORE_COL1_X;
            int aNameX = aRankX + END_LEVEL_HIGHSCORE_COL2_X;
            int aScoreX = aNameX + END_LEVEL_HIGHSCORE_COL3_X;
            int aLineH = g.GetFont().GetHeight();
            foreach (HighScoreEntry anEntry in mScores)
            {
                if (aRank > 10)
                {
                    break;
                }
                if (!anEntry.mIsNew)
                {
                    g.SetColor(SexyUtil.MakeColor(0xffffff));
                }
                else
                {
                    mNewScoreEntry = anEntry;
                    mHighScoreY = g.GetFont().GetHeight() / 2 + (y - g.GetFont().GetAscent());
                    g.SetColor(SexyUtil.MakeColor(GlobalStaticVars.gSexyAppBase.HSLToRGB(mUpdateCnt % 0xff, 0xff, 0x80)));
                }
                g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
                g.DrawString(aRank + ".", aRankX, y);
                g.DrawString(SexyUtil.GetLocalizedPlayerName(anEntry.mName), aNameX, y);
                string aScore = SexyUtil.CommaSeperate(anEntry.mScore);
                g.SetFont(Res.FONT_OVERLOADMONO12);
                g.DrawString(aScore, aScoreX - g.GetFont().StringWidth(aScore), y);
                y += aLineH;
                aRank++;
            }
            for (; aRank < 0xb; aRank++)
            {
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                g.DrawString(aRank + ".", aRankX, y);
                y += aLineH;
            }
            mDrawY = END_LEVEL_HIGHSCORE_GAP_H + y;
        }

        private static void DrawBigStat(Graphics g, int theLeft, int theRight, int theY, string theLabel, int theValue, bool underline)
        {
            g.SetFont(SexyUtil.GetFontById(8));
            g.SetColor(SexyUtil.MakeColor(0x66ffff));
            if (underline)
            {
                g.FillRect(theLeft, g.GetFont().GetDecorationOffset() + theY, theRight - theLeft, 1);
            }
            g.DrawString(theLabel, theLeft, theY);
            g.SetFont(SexyUtil.GetFontById(8));
            g.SetColor(SexyUtil.MakeColor(0xfeed00));
            string aValue = SexyUtil.CommaSeperate(theValue);
            g.DrawString(aValue, theRight - g.GetFont().StringWidth(aValue), theY);
        }

        private static void DrawBigStatAced(Graphics g, int theLeft, int theRight, int theY, string theLabel, int theValue, bool underline)
        {
            g.SetFont(SexyUtil.GetFontById(8));
            g.SetColor(SexyUtil.MakeColor(0x66ffff));
            if (underline)
            {
                g.FillRect(theLeft, g.GetFont().GetDecorationOffset() + theY, theRight - theLeft, 1);
            }
            g.DrawString(theLabel, theLeft, theY);
            int x = Res.FONT_OVERLOAD16OUTLINE.StringWidth(theLabel) + theLeft;
            g.SetColor(SexyUtil.MakeColor(0xffff99));
            g.DrawString(Loc(" Aced!!"), x, theY);
            g.SetFont(SexyUtil.GetFontById(8));
            g.SetColor(SexyUtil.MakeColor(0xfeed00));
            string aValue = SexyUtil.CommaSeperate(theValue);
            g.DrawString(aValue, theRight - g.GetFont().StringWidth(aValue), theY);
        }

        private static void DrawSmallStat(Graphics g, int theLeft, int theRight, int theY, string theLabel, int theValue)
        {
            g.SetFont(SexyUtil.GetFontById(0x12));
            g.SetColor(SexyUtil.MakeColor(0xe3d540));
            g.DrawString(theLabel, theLeft, theY);
            g.SetFont(SexyUtil.GetFontById(0x12));
            g.SetColor(SexyUtil.MakeColor(0xb9f811));
            string aValue = SexyUtil.CommaSeperate(theValue);
            g.DrawString(aValue, theRight - g.GetFont().StringWidth(aValue), theY);
        }

        /// <summary>The score breakdown (total, ace, level score, try bonus) and the small stats.</summary>
        public void DrawStats(Graphics g)
        {
            if (mDrawY == 0)
            {
                mDrawY += END_LEVEL_STATS_DRAW_Y;
            }
            else if (!mJustBeatAdventure || mAdventureCompleteCount != 0 || mShowAdventureWin)
            {
                mDrawY += END_LEVEL_GAP_H;
            }
            else
            {
                mDrawY += END_LEVEL_STATS_DRAW2_Y;
            }
            Font aBigFont = Res.FONT_OVERLOAD16OUTLINE;
            Font aSmallFont = Res.FONT_OVERLOAD12OUTLINE;
            int aLeft = END_LEVEL_STATS_X1A_X;
            int aRight = END_LEVEL_STATS_X1B_X;
            int y = aBigFont.GetHeight() + mDrawY - 5;
            int aBigStep = aBigFont.GetHeight() + 5;
            int aCol1 = END_LEVEL_STATS_X2A_X;
            int aCol1Right = aCol1 + END_LEVEL_STATS_X2B_X;
            int aCol2 = aCol1Right + END_LEVEL_STATS_X3A_X;
            int aSmallStep = aSmallFont.GetHeight();
            bool underline = mType == MODE_LEVEL_COMPLETE;
            if (!underline)
            {
                aBigStep -= 5;
            }
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            if (mAdventureCompleteCount == 0)
            {
                if (mType == MODE_LEVEL_COMPLETE)
                {
                    DrawBigStat(g, aLeft, aRight, y, Loc("Total Score:"), mTotalScoreDisp, underline);
                    y += aBigStep;
                }
            }
            else
            {
                y += END_LEVEL_GAP_H;
            }
            bool aBeat = mBoard.mLogicMgr.BeatLevel();
            if (aBeat && mAceScore > 0)
            {
                DrawBigStat(g, aLeft, aRight, y, Loc("Ace Score:"), mAceScore, underline);
                y += aBigStep;
            }
            string aLabel;
            if (mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE || mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                aLabel = Loc("Level Score:");
            }
            else
            {
                aLabel = Loc("Score:");
            }
            if (mAceScore < mLevelScoreDisp && aBeat)
            {
                DrawBigStatAced(g, aLeft, aRight, y, aLabel, mLevelScoreDisp, underline);
            }
            else
            {
                DrawBigStat(g, aLeft, aRight, y, aLabel, mLevelScoreDisp, underline);
            }
            y += aBigStep;
            if (mType == MODE_LEVEL_COMPLETE && mTryBonus > 0)
            {
                int aTries = mBoard.mLogicMgr.mInt1bc;
                string aTryLabel;
                if (aTries == 3)
                {
                    aTryLabel = Loc("Win on 3rd Try:");
                }
                else if (aTries == 2)
                {
                    aTryLabel = Loc("Win on 2nd Try:");
                }
                else if (aTries == 1)
                {
                    aTryLabel = Loc("Win on 1st Try:");
                }
                else
                {
                    aTryLabel = Loc("Try Bonus:");
                }
                DrawBigStat(g, aLeft, aRight, y, aTryLabel, mTryBonusDisp, underline);
                y += aBigStep;
            }
            int aSmallY = mType == MODE_LEVEL_COMPLETE ? (underline ? 0 : 10) + y : y;
            g.SetFont(aSmallFont);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            y = aSmallY;
            DrawSmallStat(g, aCol1, aCol1Right, y, Loc("Shots:"), mShots);
            y += aSmallStep;
            DrawSmallStat(g, aCol1, aCol1Right, y, Loc("Free Balls:"), mFreeBalls);
            y += aSmallStep;
            DrawSmallStat(g, aCol1, aCol1Right, y, Loc("% Cleared:"), mPctCleared);
            y = aSmallY;
            DrawSmallStat(g, aCol2, aRight, y, Loc("Shot Points:"), mShotPoints);
            y += aSmallStep;
            DrawSmallStat(g, aCol2, aRight, y, Loc("Fever Points:"), mFeverPoints);
            y += aSmallStep;
            DrawSmallStat(g, aCol2, aRight, y, Loc("Style Points:"), mStylePoints);
            y += aSmallStep;
            mDrawY = y;
        }

        /// <summary>The Master Stage plaque with its five-segment progress bar (the current level blinks).</summary>
        public void DrawAdventureWin(Graphics g)
        {
            int aStage = mStage;
            int aLevel = mLevel;
            // Port: a bonus Adventure has no Master stage.
            if (aStage < mApp.mPlayerInfo.mLastAdventureStage || ThunderballApp.mBonusAdventure != null)
            {
                return;
            }
#if PEGGLE_IPHONE
            mScrollWidget.SetScrollMode(ScrollWidget.ScrollMode.Disabled);
#endif
            Image aPlaque = Res.IMAGE_DLG_MASTERSTAGE;
            int y = END_LEVEL_ADV_WIN_DRAW_Y + mDrawY;
            int x = mContentWidget.mWidth / 2 - aPlaque.mWidth / 2;
            g.SetClipRect(0, y, mWidth, aPlaque.GetHeight());
            g.DrawImage(aPlaque, x, y);
            int aSegs = SexyUtil.ClampRange(aLevel + 1, 1, 5);
            if (mUpdateCnt / 0x28 % 2 == 0 && mUpdateCnt < 800)
            {
                aSegs--;
            }
            TRect aRect = Res.IMAGE_DLG_MASTERSTAGEPROGBAR.GetCelRect(0);
#if PEGGLE_IPHONE
            aRect.mWidth = END_LEVEL_MASTER_STAGE_PROGRESS_BAR_SEG_OFFSET_W_ALL[aSegs];
#else
            aRect.mWidth = PROGRESS_BAR_SEG_W[aSegs];
#endif
            g.DrawImage(Res.IMAGE_DLG_MASTERSTAGEPROGBAR, END_LEVEL_MASTER_STAGE_PROGRESS_BAR_SEG_OFFSET_X + x,
                END_LEVEL_MASTER_STAGE_PROGRESS_BAR_SEG_OFFSET_Y + y, aRect);
            mDrawY = END_LEVEL_GAP_H + y + aPlaque.mHeight;
            mAdventureWinBottom = mDrawY;
        }

        /// <summary>"Adventure Complete!" with retries, the efficiency bonus and the final score, fading in.</summary>
        public void DrawAdventureComplete(Graphics g)
        {
            int y = mDrawY + END_LEVEL_ADV_COMPLETE_DRAW_Y;
            int anAlpha = 0xff;
            int aFade = 0x14;
            if (mAdventureCompleteCount < 0x14)
            {
                anAlpha = mAdventureCompleteCount * 0xff / aFade;
            }
            g.SetFont(SexyUtil.GetFontById(0x10));
            g.SetColor(SexyUtil.MakeColor(0xffff00, anAlpha));
            WriteCenteredLine(g, y, Loc("Adventure Complete!"));
            y = g.GetFont().GetHeight() + y + 10;
            string aBonus = SexyUtil.CommaSeperate(mEfficiencyBonusDisp);
            string aRetries = mRetries.ToString();
            string aFinal = SexyUtil.CommaSeperate(mTotalScoreDisp);
            Font aFont = SexyUtil.GetFontById(8);
            int aLeft = END_LEVEL_COL_SPACING_W;
            int aRight = mContentWidget.mWidth - END_LEVEL_COL_SPACING_W;
            int aStep = aFont.GetHeight() + GlobalStaticVars.gSexyAppBase.GetInteger("ENDLEVELDIALOG_ADVENTURECOMPLETE_FONT_LINESPACING", 0);
            g.SetFont(aFont);
            var aLabelColor = SexyUtil.MakeColor(0xffff88, anAlpha);
            var aValueColor = SexyUtil.MakeColor(0x88ff88, anAlpha);
            string[] aLabels = { Loc("Retries:"), Loc("Efficiency Bonus:"), Loc("Final Score:") };
            string[] aValues = { aRetries, aBonus, aFinal };
            for (int i = 0; i < 3; i++)
            {
                g.SetColor(aLabelColor);
                g.DrawString(aLabels[i], aLeft, y);
                g.SetColor(aValueColor);
                g.DrawString(aValues[i], aRight - aFont.StringWidth(aValues[i]), y);
                if (i < 2)
                {
                    y += aStep;
                }
            }
            y += g.GetFont().GetDecorationOffset();
            g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
            g.FillRect(aLeft, y, aRight - aLeft, 1);
            y++;
            g.SetColor(SexyUtil.MakeColor(0, anAlpha));
            g.FillRect(aLeft, y, aRight - aLeft, 1);
            mDrawY = y + END_LEVEL_GAP_H;
        }

        /// <summary>Duel results: winner line, then each player's score counting up and their match stats.</summary>
        public void DrawTwoPlayerResult(Graphics g)
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            int y = mDrawY + END_LEVEL_TWO_PLAYER_DRAW_Y;
            int aShown = Math.Max(aLogic.mScore[0], aLogic.mScore[1]);
            if (mUpdateCnt < 200)
            {
                aShown = aShown * mUpdateCnt / 200;
            }
            string aText = "";
            int aWinner = 0;
            int aLoser = 1;
            if (aLogic.mScore[0] < aLogic.mScore[1])
            {
                aWinner = 1;
                aLoser = 0;
            }
            _ = aLoser;
            g.SetFont(SexyUtil.GetFontById(0x10));
            g.SetColor(SexyUtil.MakeColor(0x88ff88));
            if (aLogic.mScore[0] == aLogic.mScore[1])
            {
                aText = Loc("It's a Tie!");
            }
            else if (!mTwoPlayerCountUp || mUpdateCnt > 199 || aLogic.mScore[0] <= aShown || aLogic.mScore[1] <= aShown)
            {
                aText = Loc("%S Wins!").Replace("%S", SexyUtil.GetLocalizedPlayerName(aLogic.mPlayerNames[aWinner]));
            }
            int aContentW = mContentWidget.mWidth;
            g.DrawString(aText, aContentW / 2 - g.GetFont().StringWidth(aText) / 2, y);
            if (!mTwoPlayerCountUp)
            {
                y = g.GetFont().GetDescent() + 10 + y;
                mDrawY = y;
                return;
            }
            int[] aCenters = { aContentW / 4 + END_LEVEL_COL_SPACING_W, aContentW * 3 / 4 - END_LEVEL_COL_SPACING_W };
            // Port change (user request): both columns start at the same height, side by side.
            int aColumnTop = y;
            int aColumnBottom = y;
            for (int i = 0; i < 2; i++)
            {
                y = aColumnTop;
                int aLeft = i == 0 ? END_LEVEL_COL_SPACING_W : END_LEVEL_COL_SPACING_W + aContentW / 2;
                int aRight = i == 0 ? aContentW / 2 - END_LEVEL_COL_SPACING_W : aContentW - END_LEVEL_COL_SPACING_W;
                int aStreak = mApp.mTwoPlayerWinner == i ? mApp.mTwoPlayerWinStreak : 0;
                g.SetFont(SexyUtil.GetFontById(8));
                g.SetColor(SexyUtil.MakeColor(0xffff88));
                y = g.GetFont().GetMaxCharHeight() + Res.FONT_OVERLOAD24OUTLINE.GetDescent() + y;
                string aName = SexyUtil.GetLocalizedPlayerName(aLogic.mPlayerNames[i]);
                g.DrawString(aName, aCenters[i] - g.GetFont().StringWidth(aName) / 2, y);
                string aScore = SexyUtil.CommaSeperate(aShown < aLogic.mScore[i] ? aShown : aLogic.mScore[i]);
                g.SetFont(Res.FONT_OVERLOAD24OUTLINE);
                y += g.GetFont().GetMaxCharHeight();
                g.DrawString(aScore, aCenters[i] - g.GetFont().StringWidth(aScore) / 2, y);
                y += Res.FONT_OVERLOAD24OUTLINE.GetDescent();
                g.SetColor(SexyUtil.MakeColor(0xffff88));
                g.FillRect(aLeft, y - 2, aRight - aLeft, 1);
                g.SetFont(SexyUtil.GetFontById(0x12));
                y += g.GetFont().GetMaxCharHeight();
                int aStep = g.GetFont().GetHeight() - 3;
                if (mUpdateCnt > 199 || aLogic.mScore[0] <= aShown || aLogic.mScore[1] <= aShown)
                {
                    DrawSmallStat(g, aLeft, aRight, y, Loc("Wins:"), mApp.mTwoPlayerWins[i]);
                    y += aStep;
                    DrawSmallStat(g, aLeft, aRight, y, Loc("Streak:"), aStreak);
                    y += aStep;
                    DrawSmallStat(g, aLeft, aRight, y, Loc("Fevers:"), mApp.mTwoPlayerFevers[i]);
                    y += aStep;
                    DrawSmallStat(g, aLeft, aRight, y, Loc("Points:"), mApp.mTwoPlayerPoints[i]);
                }
                aColumnBottom = Math.Max(aColumnBottom, y);
            }
            mDrawY = aColumnBottom;
        }

        public void DrawChallengeResult(Graphics g)
        {
            int aBoxH = mDrawY == 0 ? END_LEVEL_CHALLENGE_TEXT_BIG_H : END_LEVEL_CHALLENGE_TEXT_SMALL_H;
            int aLeft = 2;
            int aRightPad = 2;
            int aWidth = END_LEVEL_SCROLL_W - 2;
            g.SetFont(Res.FONT_OVERLOAD24OUTLINE);
            int aTextH = g.GetWordWrappedHeight(aWidth, mChallengeText);
            int aPad = aBoxH / 2 - aTextH / 2 - 8;
            if (aPad < 0)
            {
                aPad = 0;
            }
            int y = mDrawY + aPad;
            g.SetColor(SexyUtil.MakeColor(0xffff88));
#if PEGGLE_IPHONE
            TRect aRect = new TRect(aLeft, y, END_LEVEL_SCROLL_W - aRightPad, 0);
            int aHeight = WriteWordWrapped(g, aRect, mChallengeText, -1, mTextAlign);
            mDrawY = aHeight + y + 10;
#else
            TRect aRect = new TRect(aLeft, END_LEVEL_DIALOG_IPAD_TEXT_OFFSET_Y + y, END_LEVEL_SCROLL_W - aRightPad, 0);
            int aHeight = WriteWordWrapped(g, aRect, mChallengeText, -1, mTextAlign);
            mDrawY = aHeight + y + 100;
#endif
        }

        public void DrawTip(Graphics g)
        {
            int aRight = END_LEVEL_TEXT_RIGHT_X;
            int anIconX = END_LEVEL_TEXT1_X;
            int aTextX = END_LEVEL_TEXT2_X;
            int y = mDrawY;
            g.SetFont(SexyUtil.GetFontById(0x12));
            g.SetColor(SexyUtil.MakeColor(0xfdfd87));
            int aHeight = g.WriteWordWrapped(new TRect(aTextX, y, aRight - aTextX, 0), mTipText, -1, -1);
            Image aTip = Res.IMAGE_DLG_TIP;
            g.DrawImage(aTip, anIconX, (aHeight - aTip.mHeight) / 2 + y);
            if (aHeight < aTip.mHeight)
            {
                aHeight = aTip.mHeight;
            }
            mDrawY = aHeight + 2 + mDrawY;
        }

        public void DrawTrialText(Graphics g)
        {
            int aRight = END_LEVEL_TEXT_RIGHT_X;
            int aTextX = END_LEVEL_TEXT1_X;
            g.SetFont(SexyUtil.GetFontById(0x12));
            g.SetColor(SexyUtil.MakeColor(0xfdfd87));
            int aHeight = g.WriteWordWrapped(new TRect(aTextX, mDrawY, aRight - aTextX, 0), mTrialText, -1, -1);
            mDrawY = aHeight + 2 + mDrawY;
        }

        /// <summary>While docked at the top, redraws the launcher and frame over the panel so they stay in front.</summary>
        public override void DrawOverlay(Graphics g)
        {
            if (mY != 0)
            {
                return;
            }
            g.Translate(-(mX - mBoard.mX), 0);
            g.SetClipRect(END_LEVEL_OVERLAY_CLIP_X, END_LEVEL_OVERLAY_CLIP_Y, END_LEVEL_OVERLAY_CLIP_W, END_LEVEL_OVERLAY_CLIP_H);
#if PEGGLE_IPHONE
            Board.DrawShadow(g);
#endif
            mBoard.mCharacterMgr.DrawBack(g, g, mBoard);
            mBoard.mCharacterMgr.Draw(g, mBoard);
            mBoard.mInterfaceMgr.DrawBack(g);
        }

        public override void Draw(Graphics g)
        {
            DeferOverlay(0);
        }

        public override void MouseMove(int x, int y)
        {
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            base.MouseDown(x, y, theClickCount);
        }
    }
}
