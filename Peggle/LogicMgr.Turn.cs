using System;
using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    public partial class LogicMgr
    {
        public const int STATE_LEVEL_DONE = 5;

        public const int END_REWARD_MASTER_BADGE = 0;

        public const int END_REWARD_ACE = 1;

        public const int END_REWARD_CLEARED = 2;

        public const int END_REWARD_ACE_CLEARED = 3;

        private static int sExploderCount;

        private static bool sHadRecursiveExploder;

        private static bool sPlayedExplodeSound;

        /// <summary>MakeNextTurnString: "&lt;name&gt;\nNext Turn" / "\n%d Turns" / "\nNext %d Turns".</summary>
        private static string MakeNextTurnString(string theName, int theTurns, bool theNext)
        {
            if (theTurns < 1)
            {
                return "";
            }
            if (theTurns == 1)
            {
                return CFormat.StrFormat(SexyAppBase.Loc("%S\nNext Turn"), theName);
            }
            if (!theNext)
            {
                return CFormat.StrFormat(SexyAppBase.Loc("%S\n%d Turns"), theName, theTurns);
            }
            return CFormat.StrFormat(SexyAppBase.Loc("%S\nNext %d Turns"), theName, theTurns);
        }

        /// <summary>The label shown in the corner for a temporary power-up of the current player.</summary>
        public void CalcCornerDisplay()
        {
            if (mTripleScore)
            {
                mCornerText = SexyAppBase.Loc("Triple Score");
            }
            else if (mPowerupCount[7][mCurPlayer] > 0)
            {
                mCornerText = SexyAppBase.Loc("ZenBall");
            }
            else if (mPowerupCount[14][mCurPlayer] > 0)
            {
                mCornerText = SexyAppBase.Loc("Electrobolt");
            }
            else if (mPowerupCount[12][mCurPlayer] > 0)
            {
                mCornerText = SexyAppBase.Loc("Fireball");
            }
            else if (mHatBall)
            {
                mCornerText = SexyAppBase.Loc("Magic Hat");
            }
            else
            {
                mCornerText = "";
            }
        }

        /// <summary>"3 Balls Left" / "Last Ball!" (or "&lt;name&gt;'s Turn" in duels) floating over the launcher.</summary>
        public void DoBeginTurnText()
        {
            if (!mDuelMode && mBallsLeft[mCurPlayer] >= 4)
            {
                return;
            }
            float aX = mBoard.mGun.mCenterX;
            float aY = mBoard.mGun.mCenterY + (float)LOGICMGR_BEGIN_TURN_TEXT_Y;
            bool lastBall = mBallsLeft[mCurPlayer] == 1;
            string aText;
            if (!mDuelMode)
            {
                if (mBallsLeft[mCurPlayer] == 3)
                {
                    aText = SexyAppBase.Loc("3 Balls Left");
                }
                else if (mBallsLeft[mCurPlayer] == 2)
                {
                    aText = SexyAppBase.Loc("2 Balls Left");
                }
                else
                {
                    aText = SexyAppBase.Loc("Last Ball!");
                }
            }
            else if (!lastBall)
            {
                aText = CFormat.StrFormat(SexyAppBase.Loc("%S's Turn"), mPlayerNames[mCurPlayer]);
            }
            else
            {
                aText = CFormat.StrFormat(SexyAppBase.Loc("%S's Last Ball!"), mPlayerNames[mCurPlayer]);
            }
            FloatingText aFloat = AddStandardText(aText, aX, aY, 0xc);
            AvoidSides(aFloat);
            int aDuration = 0x15e;
            if (mStats[0].mShots > 0)
            {
                aFloat.mUpdateCnt = mDuelMode ? 0 : -0x50;
            }
            aFloat.AddScale(5.0f, 0);
            aFloat.mDuration = aDuration + 10;
            aFloat.mColor = 0xffff00;
            aFloat.mJustifyY = -1;
            float aH = SexyUtil.gPhysicalDisplayHeight;
            if (!mDuelMode)
            {
#if !PEGGLE_IPHONE
                int aLeft = SCREEN_WIDTH / 2 - Res.FONT_OVERLOAD24GRAD2.StringWidth(aText) / 2;
                aFloat.MoveTo((float)aLeft, (float)((double)LOGICMGR_BEGIN_TURN_TEXT_MOVETO_Y * 600.0 / (double)aH), 0x1e);
#endif
                aFloat.AddScale(1.0f, 0x1d);
                aFloat.mVelY = -0.1f;
            }
            else
            {
                aFloat.HoldPos(0xf);
#if PEGGLE_IPHONE
                aFloat.MoveTo(aFloat.mX, (float)((double)LOGICMGR_BEGIN_TURN_TEXT_MOVETO_Y * 600.0 / (double)aH), 0x1e);
#else
                aFloat.MoveTo((float)((double)LOGICMGR_BEGIN_TURN_TEXT_MOVETO_X * 600.0 / (double)aH),
                    (float)((double)LOGICMGR_BEGIN_TURN_TEXT_MOVETO_Y * 600.0 / (double)aH), 0x1e);
#endif
                aFloat.mVelY = 0;
                aFloat.AddScale(4.0f, 5);
                aFloat.AddScale(3.0f, 0xc);
                aFloat.AddScale(2.0f, 0x15);
                aFloat.AddScale(1.0f, 0x23);
            }
            aFloat.mFadeOut = 0x1e;
            mTurnTextId = aFloat.mId;
        }

        /// <summary>Shows a one-time tip (or theTip when given) for the human player in single-player games.</summary>
        public void DoBeginTurnTip(int theTip)
        {
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            if (aPlayer == null || mDuelMode || mIsComputer[0])
            {
                return;
            }
            int aTip = theTip;
            int aMaxLevel = mBoard.mApp.GetMaxLevel();
            int aDelay = 0x46;
            if (aTip < 0)
            {
                if (mFlag19 && !mDuelMode && aPlayer.SetTip(0))
                {
                    aTip = 0;
                }
                else if (mGotExtraBall && aPlayer.SetTip(1))
                {
                    aTip = 1;
                }
                else if (mFlag1B && aPlayer.SetTip(2))
                {
                    aTip = 2;
                }
                else if (mBallsLeft[0] == 4 && aPlayer.SetTip(3))
                {
                    aTip = 3;
                }
                else if ((mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE || mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS) &&
                    aMaxLevel == 0 && mGoalPegs.Count < 0xb && aPlayer.SetTip(4))
                {
                    aTip = 4;
                }
                else if (mPowerupCount[1][0] > 1 && aPlayer.SetTip(5))
                {
                    aTip = 5;
                }
                else if (aMaxLevel < 10)
                {
                    // Tip 7 is consumed but, as in the original, shown with tip 8's text.
                    if ((mFlag1B && aPlayer.SetTip(7)) || (mFlagA2 && aPlayer.SetTip(8)))
                    {
                        aTip = 8;
                    }
                }
            }
            else
            {
                aDelay = 0;
            }
            if (aTip < 0)
            {
                return;
            }
            int aFont = 6;
            int aDuration = 800;
            int anArrowType = 0;
            int anArrowX = 0;
            int anArrowY = 0;
            int anArrowDir = 0;
            float aTipX = -1.0f;
            float aTipY = -1.0f;
            string aText = "";
            switch (aTip)
            {
            case 0:
                aText = SexyAppBase.Loc("You got the ^00FF00^ball in the bucket!^oldclr^\nThis ^00FF00^returns the ball^oldclr^ to the Ball-O-Tron!");
                anArrowType = 2;
                aTipX = 20.0f;
                aTipY = 110.0f;
                break;
            case 1:
                aText = SexyAppBase.Loc("You got over ^FF00FF^25,000 points^oldclr^ on your last shot!\nThat earns you a ^00FF00^free ball!^oldclr^");
                break;
            case 2:
                aText = SexyAppBase.Loc("If you ^00FF00^don't hit any pegs on a shot^oldclr^ then you can\nget your ball back by ^00FF00^winning a coin flip!^oldclr^");
                break;
            case 3:
                aText = SexyAppBase.Loc("You have ^00FF00^4 balls left!^oldclr^");
                anArrowType = 1;
#if PEGGLE_IPHONE
                aTipX = 20.0f;
                aTipY = 110.0f;
#else
                aTipX = (float)LOGICMGR_TIP_4_BALLS_LEFT_X;
                aTipY = (float)LOGICMGR_TIP_4_BALLS_LEFT_Y;
#endif
                break;
            case 4:
                if (mGoalPegs.Count == 1)
                {
                    aText = SexyAppBase.Loc("You have ^FF8800^ONE ORANGE PEG^oldclr^ to go!\nClear all of the ^FF8800^ORANGE PEGS^oldclr^ to win!");
                }
                else
                {
                    aText = CFormat.StrFormat(SexyAppBase.Loc("You have ^FF8800^%d ORANGE PEGS^oldclr^ to go!\nClear all of the ^FF8800^ORANGE PEGS^oldclr^ to win!"), mGoalPegs.Count);
                }
                break;
            case 5:
                aText = SexyAppBase.Loc("Super Guide Tip: ^00FF00^Fine-tune^oldclr^ your shot with the ^00FF00^wheel.^oldclr^");
                break;
            case 8:
                {
                    aText = SexyAppBase.Loc("Use your ^00ff00^finger^oldclr^ to help aim!\nThe shooter moves to ^00FF00^exactly^oldclr^ where you tap.");
                    anArrowType = 3;
                    aDuration = 0x5dc;
                    // The arrow underlines the first coloured word: its start/end x and the baseline.
                    int aFirst = aText.IndexOf('^');
                    string aBefore = aFirst >= 0 ? aText.Substring(0, aFirst) : aText;
                    int aSecond = aFirst >= 0 ? aText.IndexOf('^', aFirst + 1) : -1;
                    int aThird = aSecond >= 0 ? aText.IndexOf('^', aSecond + 1) : -1;
                    string aWord = aThird > aSecond && aSecond >= 0 ? aText.Substring(aSecond + 1, aThird - aSecond - 1) : "";
                    Font aTipFont = SexyUtil.GetFontById(aFont);
                    anArrowX = aTipFont.StringWidth(aBefore);
                    anArrowY = anArrowX + aTipFont.StringWidth(aWord);
                    anArrowDir = aTipFont.GetHeight() + -6;
                    break;
                }
            }
            if (aText.Length == 0)
            {
                return;
            }
            if (mTurnTextId != 0)
            {
                mBoard.mFloatingTextMgr.EraseText(mTurnTextId);
            }
            bool centerOnTip = false;
            if (aTipX != -1.0f && aTipY != -1.0f)
            {
                centerOnTip = true;
            }
            else
            {
                aTipX = mBoard.mGun.mCenterX;
                aTipY = mBoard.mGun.mCenterY + 110.0f;
            }
            FloatingText aFloat = AddStandardText(aText, aTipX, aTipY, aFont);
            if (centerOnTip)
            {
                float aH = SexyUtil.gPhysicalDisplayHeight;
                aFloat.mX = (float)((double)aFloat.mX + (double)(aFloat.mWidth / 2) * 600.0 / (double)aH);
                aFloat.mY = (float)((double)aFloat.mY + (double)(aFloat.mHeight / 2) * 600.0 / (double)aH);
            }
            aFloat.mColor = 0xffffff;
            aFloat.mVelY = 0;
            aFloat.mDuration = aDuration;
            aFloat.mFadeIn = 0x14;
            aFloat.mFadeOut = 0x1e;
            aFloat.mBackShade = true;
            aFloat.mArrowType = anArrowType;
            aFloat.mUpdateCnt = -aDelay;
            aFloat.mArrowX = anArrowX;
            aFloat.mArrowY = anArrowY;
            aFloat.mArrowDir = anArrowDir;
            mTurnTextId = aFloat.mId;
        }

        public void BeginTurn(bool nextPlayer)
        {
            mBoard.mCharacterMgr.SetYinYangEye(false);
            if (mMuteLoops)
            {
                SetState(STATE_LEVEL_DONE);
                return;
            }
            bool outOfBalls;
            if (!mDuelMode)
            {
                outOfBalls = mBallsLeft[0] < 1;
            }
            else
            {
                outOfBalls = mBallsLeft[0] < 1 && mBallsLeft[1] < 1;
            }
            if (outOfBalls)
            {
                DoLevelDone();
                return;
            }
            if (mFlag1E)
            {
                if (mPowerupCount[2][mCurPlayer] > 0)
                {
                    FlipperClick(false);
                }
                mFlag1E = false;
            }
            if (mDuelMode)
            {
                float anAngle = mWheelMoveRad;
                if (nextPlayer)
                {
                    mWheelMoveRad = mBoard.mGun.mAngle;
                    mCurPlayer = 1 - mCurPlayer;
                    if (mBallsLeft[mCurPlayer] < 1)
                    {
                        mCurPlayer = 1 - mCurPlayer;
                    }
                }
                SetGunAngle(anAngle);
                mBoard.mGun.SetAngle(anAngle, false);
                mBoard.mInterfaceMgr.SetNumBalls(mBallsLeft[mCurPlayer]);
            }
            mBoard.mCharacterMgr.SetCurCharacter(mCharacters[mCurPlayer]);
            if (mDuelMode && nextPlayer)
            {
                mBoard.mInterfaceMgr.UpdateInterfaceColor();
            }
            DoBeginTurnText();
            mFlag404 = true;
            if (!mDuelMode && mInt374 > -1)
            {
                mInt374 = -mInt374;
            }
            else
            {
                mInt374 = 0;
            }
            mGuideCount = 0;
            mShotScore = 0;
            mInt164 = 0;
            mStyleScore = 0;
            mInt178 = 0;
            mInt0c = 0;
            mInt10 = 0;
            mInt14 = 0;
            mInt50 = 0;
            mFlag18 = false;
            mFlag19 = false;
            mGotExtraBall = false;
            mFlagE4 = false;
            mFlag20 = false;
            mTripleScore = false;
            mHatBall = false;
            mSlowMoSoundPlaying = false;
            mSuperGuide = false;
            mBoard.mGun.mSuperGuide = false;
            mVec384 = new SexyVector2(320.0f, 240.0f);
            mEyeTarget = new SexyVector2(320.0f, 240.0f);
            ActivatePowerups();
            if (mInt188 > 0)
            {
                mInt188 = 0;
                MakeScorePeg();
            }
            if (mFlag1C)
            {
                mFlag1C = false;
                if (mInt24 > 0)
                {
                    mInt24--;
                    MakePowerupPeg();
                }
            }
            if (mPowerupCount[7][mCurPlayer] > 0)
            {
                mSuperGuide = true;
            }
            CalcCornerDisplay();
            BeginTurn2();
        }

        public void DoLevelDone()
        {
            string aLevel = SexyUtil.GetFileName(mBoard.mLevelName, true);
            if (aLevel.Length == 0)
            {
                return;
            }
            LevelInfo anInfo = mBoard.mApp.mStageMgr.GetLevelInfoByName(aLevel);
            bool aced = false;
            if (mBoard.mGameMode != GameMode.GAMEMODE_DUEL && anInfo != null && BeatLevel())
            {
                aced = anInfo.mAceScore <= mScore[0];
            }
            SetState(STATE_LEVEL_DONE);
            if (!mMuteLoops)
            {
                RecordStats();
                mBoard.DoLevelDone();
            }
            uint aType = (uint)mBoard.mEndLevelDialog.mType;
            if (aType < 2)
            {
                if (mBoard.mGameMode != GameMode.GAMEMODE_DEMO && mBoard.mGameMode != GameMode.GAMEMODE_CHALLENGE)
                {
                    if (aced && mLevelCleared)
                    {
                        AddEndReward(END_REWARD_ACE_CLEARED, false);
                    }
                    else if (aced)
                    {
                        AddEndReward(END_REWARD_ACE, false);
                    }
                    else if (mLevelCleared)
                    {
                        AddEndReward(END_REWARD_CLEARED, false);
                    }
                }
            }
            else if (aType == 6)
            {
                AddEndReward(END_REWARD_MASTER_BADGE, false);
            }
        }

        public void BeginTurn2()
        {
            if (mMuteLoops)
            {
                SetState(STATE_NONE);
                return;
            }
            mBoard.mAIMgr.Clear();
            SetState(STATE_AIMING);
            ActivateFreeBall(true);
            mInt1b4 = 0;
            mInt1b0 = 0;
            mInt3c = 0;
            mInt40 = 0;
            mInt38 = 0;
            mInt44 = 0;
            mInt48 = 0;
            mInt4c = 0;
            mInt1a0 = -1;
            mFlagE3 = false;
            mFlag1D = false;
            mFlag1E = false;
            mFeverPending = true;
            mGuideCount = 1;
            mBoard.Reload();
            mBoard.mInterfaceMgr.LoadGun();
            if (mPowerupCount[14][mCurPlayer] > 0)
            {
                mBoard.mGun.SetElectrified(true);
            }
            if (mPowerupCount[12][mCurPlayer] > 0)
            {
                mBoard.mGun.SetFireball(true);
            }
            if (mWearHat)
            {
                SetWearHat(true);
            }
        }

        public bool BeatLevel()
        {
            if (mDuelMode)
            {
                return mScore[1] < mScore[0];
            }
            if (!mClearLevelMode)
            {
                return mGoalPegs.Count == 0;
            }
            return mGoalPegs.Count == 0 && mNonGoalPegs.Count == 0;
        }

        public int GetGoalPegsLeft()
        {
            if (!mClearLevelMode)
            {
                return mGoalPegs.Count;
            }
            return mGoalPegs.Count + mNonGoalPegs.Count;
        }

        public PhysObj GetRemainingGoalPeg()
        {
            foreach (PhysObj anObj in mGoalPegs)
            {
                return anObj;
            }
            foreach (PhysObj anObj in mNonGoalPegs)
            {
                return anObj;
            }
            return null;
        }

        /// <summary>Adds this level's result to the per-level stats (adventure and quick play only) and saves them.</summary>
        public void RecordStats()
        {
            if (mMuteLoops)
            {
                return;
            }
            StatsMgr aStatsMgr = mBoard.mApp.mStatsMgr;
            if (Board.Cheated())
            {
                aStatsMgr = mBoard.mApp.mCheatStatsMgr;
            }
            int aMode = mBoard.mGameMode;
            if (aMode != GameMode.GAMEMODE_ADVENTURE && aMode != GameMode.GAMEMODE_ADVENTURE_NIGHTS && aMode != GameMode.GAMEMODE_QUICKPLAY)
            {
                return;
            }
            string aLevel = SexyUtil.GetFileName(mBoard.mLevelName, true);
            if (aLevel.Length == 0)
            {
                return;
            }
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            bool firstAdventure = false;
            if (aPlayer.GetmAdventureWinCount() == 0)
            {
                firstAdventure = aMode == GameMode.GAMEMODE_ADVENTURE || aMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS;
            }
            LevelStats aStats = firstAdventure ? aStatsMgr.GetStageStatsForLevel(aLevel) : aStatsMgr.GetStatsForLevel(aLevel);
            if (aPlayer != null && aPlayer.mFirstAdventureStage < aPlayer.GetmMaxStage())
            {
                if (aMode == GameMode.GAMEMODE_CHALLENGE)
                {
                    aStatsMgr.IncTrophyCharacterCount(aPlayer.mCurCharacter);
                }
                else if (!((aMode == GameMode.GAMEMODE_ADVENTURE || aMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS) &&
                    aPlayer.GetmMaxStage() < aPlayer.mLastAdventureStage))
                {
                    aStatsMgr.IncCharacterCount(aPlayer.mCurCharacter);
                }
            }
            if (!ClearedLevel(false))
            {
                if (!firstAdventure)
                {
                    aStatsMgr.IncCharacterLossCount(mPowerupTypes[0]);
                }
                else
                {
                    aStatsMgr.IncStageCharacterLossCount(mPowerupTypes[0]);
                }
                mLevelStats.mNumLosses++;
                mLevelStats.mLossScore += mScore[0] / 1000;
                for (int i = 0; i < 4; i++)
                {
                    mLevelStats.mCounts[i] = 0;
                }
                mLevelStats.mLossPegsLeft = GetGoalPegsLeft();
            }
            else
            {
                mLevelStats.mNumWins++;
                mLevelStats.mWinScore += mScore[0] / 1000;
                mLevelStats.mBestScore = (uint)mScore[0];
                mLevelStats.mBestScoreCharacter = mPowerupTypes[0];
                if (!firstAdventure)
                {
                    aStatsMgr.IncCharacterWinCount(mPowerupTypes[0]);
                }
                else
                {
                    aStatsMgr.IncStageCharacterWinCount(mPowerupTypes[0]);
                }
            }
            aStats.Add(mLevelStats);
            aStatsMgr.mNumGames++;
            if (!Board.Cheated())
            {
                aStatsMgr.Save(null);
            }
            else
            {
                aStatsMgr.Save("LevelEditor/stats_cheat.dat");
            }
        }

        /// <summary>The end-of-level badge/ribbon and the "Tap to Continue" prompt.</summary>
        public Effect AddEndReward(int theType, bool instant)
        {
            int aDelay = 0x82;
            Effect anEffect = null;
            switch (theType)
            {
            case END_REWARD_MASTER_BADGE:
                anEffect = mBoard.mEffectMgr.AddMasterBadge(325.0f, 275.0f, aDelay);
                mFlag155 = true;
                break;
            case END_REWARD_ACE:
                anEffect = mBoard.mEffectMgr.AddRibbon(325.0f, 320.0f, 1, aDelay);
                mFlag155 = true;
                break;
            case END_REWARD_CLEARED:
                if (!instant)
                {
                    anEffect = mBoard.mEffectMgr.AddRibbon(325.0f, 320.0f, 0, aDelay);
                }
                else
                {
                    anEffect = mBoard.mEffectMgr.AddRibbon(465.0f, 280.0f, 0, 0);
                }
                mFlag155 = true;
                break;
            case END_REWARD_ACE_CLEARED:
                anEffect = mBoard.mEffectMgr.AddRibbon(325.0f, 320.0f, 2, aDelay);
                mFlag155 = true;
                break;
            }
            if (mFlag155 && !mMuteLoops)
            {
                string aText = SexyAppBase.Loc("Tap to Continue");
                FloatingText aFloat = AddStandardText(aText, 325.0f, (float)(14400.0 / (double)SexyUtil.gPhysicalDisplayHeight + 440.0), 8);
                aFloat.mLayer = 2;
                aFloat.mFadeIn = 0x1e;
                aFloat.mUpdateCnt = -300;
                aFloat.mDuration = 0;
                aFloat.mVelY = 0;
                aFloat.mVelX = 0;
                mTurnTextId = aFloat.mId;
                if (!instant)
                {
                    mBoard.mSoundMgr.AddSoundDelay(Res.SOUND_TING, aDelay);
                    mBoard.mSoundMgr.AddSoundDelay(Res.SOUND_APPLAUSE, aDelay + 0x1e);
                }
                else
                {
                    mBoard.mSoundMgr.AddSound(Res.SOUND_TING);
                    mBoard.mSoundMgr.AddSound(Res.SOUND_APPLAUSE);
                }
            }
            return anEffect;
        }

        public int GetUniqueStyleShots()
        {
            int aCount = 0;
            for (int i = 0; i < mStyleShotCounts.Count; i++)
            {
                if (mStyleShotCounts[i] > 0)
                {
                    aCount++;
                }
            }
            return aCount;
        }

        public void BeginShot(bool saveReplayPoint)
        {
            mFlagA0 = false;
            mFlagA2 = false;
            mFlag156 = false;
            mFlagE3 = false;
            mLastStyleShotTime = -1000;
            SetState(STATE_SHOOTING);
            mPegHits.Clear();
            mBoard.BeginShot();
            mStats[mCurPlayer].mShots++;
            foreach (PhysObj anObj in mGoalPegs)
            {
                if (anObj.mPegInfo != null && anObj.mPegInfo.mFlashType == 10)
                {
                    anObj.mPegInfo.mFlashType = 0;
                    anObj.mPegInfo.mFlashCount = 0;
                }
            }
            if (mTurnTextId != 0)
            {
                mBoard.mFloatingTextMgr.EraseText(mTurnTextId);
                mTurnTextId = 0;
            }
            IncNumBalls(-1, 0, true);
            mInt198 = -1000;
            mInt19c = -1;
            mInt1a8 = -1;
            mInt1ac = -1;
            mInt2c = SexyUtil.Rand();
            mInt30 = SexyUtil.Rand();
            mBoard.mInterfaceMgr.BeginShot();
            mClicks.Clear();
            mBallPositions.Clear();
            if (saveReplayPoint)
            {
                mBoard.GetReplayPoint();
            }
        }

        /// <summary>The green "multiball" peg: spawns a second ball bouncing away from the hitting ball.</summary>
        public void DoMultiball(Ball theBall, PhysObj thePeg)
        {
            float aVelX = 0.5f;
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            float aDX = aX - theBall.GetXPos();
            if (aDX < 0.0f)
            {
                aVelX = aVelX * -1.0f;
            }
            else if (aDX == 0.0f && mInt2c % 2 == 0)
            {
                aVelX = aVelX * -1.0f;
            }
            Ball aNewBall = new Ball(false);
            aNewBall.SetPos(aX, aY);
            aNewBall.SetVelocity(aVelX, -3.0f);
            mBoard.AddObj(aNewBall);
            if (theBall.mHatBall != null || theBall.mIsHatBall)
            {
                aNewBall.SetHat(true, true);
            }
            thePeg.SetActive(false);
            if (mDimLowObjects && mBoard.mBalls.Count == 2)
            {
                DoStyleShot(theBall, 5);
                return;
            }
            mBoard.mSoundMgr.AddSound(Res.SOUND_POWERUP_01_MULTIBALL);
        }

        public void DoStyleShot(Ball theBall, int theStyleShot)
        {
            DoStyleShot(theBall.mX, theBall.mY - 10.0f, theStyleShot, null);
        }

        /// <summary>The "Space Blast" green peg: hits every unhit peg within 100 units.</summary>
        public void DoExploder(Ball theBall, PhysObj thePeg)
        {
            if (sExploderCount == 0)
            {
                sHadRecursiveExploder = false;
                sPlayedExplodeSound = false;
            }
            sExploderCount++;
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            thePeg.mPegInfo.mFlashCount = 0;
            int aDelay = SexyUtil.ClampRange((sExploderCount + -1) * 0x1e, 0, 0x32);
            mBoard.mEffectMgr.AddSpaceBlast(aX, aY, aDelay);
            float aRadiusSq = 100.0f * 100.0f;
            foreach (PhysObj anObj in new List<PhysObj>(mList394))
            {
                if (!anObj.mPegInfo.mHit && anObj.IsVisible())
                {
                    float dx = aX - anObj.GetXPos();
                    float dy = aY - anObj.GetYPos();
                    if (!(dx * dx + dy * dy > aRadiusSq))
                    {
                        PegHit(theBall, anObj, true);
                    }
                }
            }
            int aShake = 0x14;
            int aPitch = 0;
            if (sExploderCount > 1)
            {
                sHadRecursiveExploder = true;
                aPitch = -0x18;
                aShake = 0x32;
            }
            if (mBoard.mShakeCount < aShake)
            {
                mBoard.mShakeCount = aShake;
            }
            if (aPitch == 0)
            {
                mBoard.mSoundMgr.AddSound(Res.SOUND_POWERUP_03_SPACEBLAST);
            }
            else
            {
                mBoard.mSoundMgr.AddSoundPitch(Res.SOUND_EXPLODE, (float)aPitch);
            }
            sExploderCount--;
        }
    }
}
