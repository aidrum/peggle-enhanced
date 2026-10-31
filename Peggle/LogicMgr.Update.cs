using System;
using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    public partial class LogicMgr
    {
        public const int STATE_TOTAL_MISS = 4;

        public const int STATE_POST_POST_SHOT = 6;

        public const int STATE_SHOT_EXTENDER = 7;

        public const int STATE_ZEN_SHOT = 10;

        /// <summary>A shot that hit no orange peg is penalised in duels and in trophies that ask for it.</summary>
        public bool IsShotScorePenalty()
        {
            if (mFlag18)
            {
                return false;
            }
            if (mDuelMode)
            {
                return true;
            }
            return mBoard.mTrophyInfo != null && mBoard.mTrophyInfo.mScorePenaltyForNoOrange != 0;
        }

        public void AddStyleScoreText(int x, int y)
        {
            if ((mFlag18 || !mDuelMode) && mStyleScore > 0)
            {
                string aText = CFormat.StrFormat(SexyAppBase.Loc("+%s Style Points"), SexyUtil.CommaSeperate(mStyleScore));
                FloatingText aFloat = AddStandardText(aText, (float)x, (float)y, 8);
                aFloat.mUpdateCnt = 0;
                aFloat.mJustifyY = -1;
                aFloat.mVelY = 0;
                aFloat.mDuration = 100000;
                aFloat.mFadeIn = 0x14;
                aFloat.mUpdateCnt = -10000;
                aFloat.mColor = 0x88ff88;
                mInt40 = aFloat.mId;
            }
        }

        /// <summary>All balls are gone: shows the shot score, queues the hit pegs for removal, or grants a Shot Extender.</summary>
        public void FinishShot()
        {
            mBoard.mCharacterMgr.SetYinYangEye(false);
            if (mIsComputer[mCurPlayer] && mPowerupCount[2][mCurPlayer] > 0 && mInt19c > -1)
            {
                FlipperClick(false);
            }
            mBoard.SetSlowMo(false, 0xfa);
            WriteClickTimes();
            if (!mFlagE4 || mDimLowObjects)
            {
                mBoard.mInterfaceMgr.DoFeverBlink();
                SetState(STATE_POST_SHOT);
                FloatingTextMgr aTextMgr = mBoard.mFloatingTextMgr;
                FloatingText aLast = aTextMgr.GetTextById(mInt178);
                if (aLast != null)
                {
                    aTextMgr.EraseText(mInt178);
                    mInt178 = 0;
                }
                aTextMgr.EraseText(mTurnTextId);
                mTurnTextId = 0;
                if ((mInt164 != 0 || mDuelMode) && !mDimLowObjects)
                {
                    int aHold = 0;
                    string aLine1;
                    string aLine2;
                    if (!IsShotScorePenalty())
                    {
                        aLine1 = CFormat.StrFormat(SexyAppBase.Loc("%s x Pegs\n"), SexyUtil.CommaSeperate(mInt164));
                        aLine2 = "";
                    }
                    else
                    {
                        aLine1 = SexyAppBase.Loc("No Orange");
                        if (mBoard.mTrophyInfo == null || mBoard.mTrophyInfo.mScorePenaltyForNoOrange == 0)
                        {
                            aLine2 = SexyAppBase.Loc("-25%");
                        }
                        else
                        {
                            aLine2 = CFormat.StrFormat("%d%%", mBoard.mTrophyInfo.mScorePenaltyForNoOrange);
                        }
                        aHold = -1000;
                    }
                    double aH = (double)SexyUtil.gPhysicalDisplayHeight;
                    FloatingText aCount = AddStandardText(aLine1, (float)BOARD_FLOATING_TEXT_POS_X,
                        (float)((double)BOARD_FLOATING_TEXT_POS_Y * 600.0 / aH), 8);
                    aCount.mVelY = 0;
                    aCount.mJustifyY = -1;
                    aCount.mDuration = 100000;
                    aCount.mFadeIn = 0x14;
                    aCount.mUpdateCnt = aHold;
                    mInt178 = aCount.mId;
                    FloatingText aScore = AddStandardText(aLine2, (float)BOARD_FLOATING_TEXT_POS_X,
                        (float)((double)(BOARD_FLOATING_TEXT_POS_Y + BOARD_FLOATING_TEXT_SCORE_OFFSET_Y) * 600.0 / aH), 0x10);
                    aScore.mUpdateCnt = 0;
                    aScore.mJustifyY = -1;
                    aScore.mVelY = 0;
                    aScore.mDuration = 100000;
                    aScore.mFadeIn = 0x14;
                    aScore.mUpdateCnt = aHold;
                    mInt3c = aScore.mId;
                    AddStyleScoreText(BOARD_FLOATING_TEXT_POS_X,
                        (int)((double)(BOARD_FLOATING_TEXT_POS_Y + BOARD_FLOATING_TEXT_SCORE_OFFSET_Y + BOARD_FLOATING_TEXT_STYLE_SCORE_OFFSET_Y) * 600.0 / aH));
                }
                mInt17c = 0;
                foreach (PhysObj anObj in mList394)
                {
                    if (anObj.mPegInfo.mHit || !anObj.mVisible)
                    {
                        mInt17c++;
                    }
                }
                if (ClearedLevel(false))
                {
                    foreach (PhysObj anObj in mNonGoalPegs)
                    {
                        mList39c.Add(anObj);
                    }
                    mNonGoalPegs.Clear();
                }
                if (mInt188 > 0)
                {
                    mInt14 = 0;
                    if (!IsShotScorePenalty())
                    {
                        mInt10 = mInt188 - mList39c.Count;
                        if (mInt10 < 0)
                        {
                            mInt10 = 0;
                        }
                        mInt0c = 0x32;
                    }
                    else
                    {
                        mInt0c = 0x19;
                        mInt14 = mInt188 - mList39c.Count;
                    }
                }
            }
            else
            {
                mBoard.mFloatingTextMgr.EraseText(mInt178);
                Gun aGun = mBoard.mGun;
                FloatingText aText = AddStandardText(SexyAppBase.Loc("Shot Extender!"), aGun.mCenterX, aGun.mCenterY + 60.0f, -1);
                aText.mDuration = 200;
                aText.mBlinkCnt = 0x78;
                aText.mVelY = 0;
                mFlagE4 = false;
                if (mBallsLeft[mCurPlayer] == 0 || (mShotScore > 24999 && !mDuelMode))
                {
                    IncNumBalls(1, 0, false);
                }
                mCornerText = "ShotExtender";
                SetState(STATE_SHOT_EXTENDER);
            }
        }

        public void UpdateShot()
        {
            mInt38++;
            if (!mMuteLoops && mIsComputer[mCurPlayer])
            {
                if (mPowerupCount[2][mCurPlayer] < 1)
                {
                    if (mStateCount == mInt1a0)
                    {
                        MouseDown(0, 0, 1, false, true);
                    }
                }
                else
                {
                    CheckDoFlippers();
                }
            }
            if (mFlag1E && mInt1a4 > 0)
            {
                mInt1a4--;
                foreach (Ball aBall in mBoard.mBalls)
                {
                    aBall.mVelY = aBall.mVelY - Ball.mGravity * 2.0f;
                }
            }
            if (KILLBALL_TIME1 <= mInt38 && (mInt38 - KILLBALL_TIME1) % 700 == 0)
            {
                mFlagE3 = true;
                mInt38 = KILLBALL_TIME3;
            }
            if (mInt1b0 != 0)
            {
                mInt1b0--;
            }
            if (!UpdateShotBalls() && mInt0c == 0)
            {
                FinishShot();
            }
        }

        public void UpdateShotExtender()
        {
            if (mBoard.mInterfaceMgr.mBallsSettled)
            {
                BeginTurn2();
            }
        }

        /// <summary>Fever end: each unused ball is worth 10,000 (5,000 in duels), counted into the shot score.</summary>
        public bool UpdateBallBonus(bool checkOnly)
        {
            InterfaceMgr anInterface = mBoard.mInterfaceMgr;
            if (anInterface.GetNumBalls() < 1)
            {
                return false;
            }
            TrophyInfo aTrophy = mBoard.mTrophyInfo;
            if (aTrophy != null && aTrophy.mLevels.Count >= 2 && aTrophy.mBalls != 0)
            {
                return false;
            }
            FloatingText aScoreText = mBoard.mFloatingTextMgr.GetTextById(mInt3c);
            if (aScoreText == null)
            {
                return false;
            }
            if (!checkOnly)
            {
                int aBonus = mDuelMode ? 5000 : 10000;
                double aH = (double)SexyUtil.gPhysicalDisplayHeight;
                int aX = (int)((double)(anInterface.GetBallX() - mBoard.mBoardRect.mX) +
                    (double)(Res.IMAGE_INT_BALL.mWidth / 2) * 600.0 / aH);
#if !PEGGLE_IPHONE
                if (mMuteLoops)
                {
                    aX = EXTREMEFEVER_BALLSCORE_OFFSET_X + aX;
                }
#endif
                int aY = anInterface.GetTopBallY() - mBoard.mBoardRect.mY;
                if (aY > -1)
                {
                    FloatingText aText = AddStandardText("+" + SexyUtil.CommaSeperate(aBonus), (float)aX, (float)aY, -1);
                    aText.mDuration = 100;
                    aText.mFadeOut = 10;
                    aText.mVelX = 0;
                    aText.mVelY = -1.0f;
                }
                anInterface.SetNumBalls(anInterface.GetNumBalls() + -1);
                int aPrev = mShotScore;
                mShotScore = aBonus + aPrev;
                mInt1c0 = aBonus + aPrev;
                aScoreText.SetText(SexyUtil.CommaSeperate(mShotScore));
                mInt1c4 = 10;
                mBoard.mSoundMgr.AddSoundPitch(Res.SOUND_SCORECOUNTER, (float)(mBallsLeft[mCurPlayer] - anInterface.GetNumBalls()));
            }
            return true;
        }

        public void AddFeverScoreText()
        {
            mBoard.mEffectMgr.AddFeverScoreText();
            int aX = 0x145;
            int aY = 0xaf;
            int aGap = 0x1e;
            FloatingText aTitle = AddStandardText("", (float)aX, (float)aY, 0xc);
            aTitle.mVelY = 0;
            aTitle.mJustifyY = -1;
            aTitle.mDuration = 100000;
            aTitle.AddScale(5.0f, 0);
            aTitle.AddScale(1.0f, 0x1e);
            mInt178 = aTitle.mId;
            double aH = (double)SexyUtil.gPhysicalDisplayHeight;
            FloatingText aScore = AddStandardText("", (float)aX, (float)((double)(aY + aGap) + 30000.0 / aH), 0xc);
            aScore.mUpdateCnt = 0;
            aScore.mJustifyY = -1;
            aScore.mVelY = 0;
            aScore.mDuration = 100000;
            mInt3c = aScore.mId;
            AddStyleScoreText(aX, (int)((double)(aY + aGap) + (double)LOGICMGR_STYLESHOT_FEVER_SCORE_OFFSET_Y * 600.0 / aH));
        }

        /// <summary>Extreme Fever's end: waits for the fever slots, then counts the shot score up and adds the ball bonus.</summary>
        public bool UpdateFeverScoreText()
        {
            bool accelerated = true; // SexyAppBase::Is3DAccelerated()
            int aTextTime = accelerated ? 0x46 : 0x32;
            int aCountTime = accelerated ? 0x78 : 100;
            if (mStateCount < 2)
            {
                if (mList39c.Count != 0)
                {
                    // Still popping the hit pegs.
                    mStateCount = 0;
                    return true;
                }
                mBoard.mFloatingTextMgr.GetTextById(mInt178);
                Effect aSlot = mBoard.mEffectMgr.GetEffectByType(0x11);
                if (aSlot == null || aSlot.mDuration + -0x1e <= aSlot.mUpdateCnt)
                {
                    mStateCount = 2;
                    if (accelerated)
                    {
                        mBoard.mEffectMgr.AddFeverRainbow(100000);
                        mBoard.mSoundMgr.AddSound(Res.SOUND_RAINBOW);
                    }
                }
                else
                {
                    mStateCount = 0;
                }
                return true;
            }
            if (aCountTime < mStateCount)
            {
                if (mInt1c4 >= 1)
                {
                    mInt1c4--;
                    return true;
                }
                if (mInt1c0 < mShotScore)
                {
                    int aLeft = mShotScore - mInt1c0;
                    if (aLeft >= 0x186a1)
                    {
                        mInt1c0 += 5000;
                    }
                    else if (aLeft >= 0x2711)
                    {
                        mInt1c0 += 1000;
                    }
                    else if (aLeft >= 0x3e9)
                    {
                        mInt1c0 += 200;
                    }
                    else if (aLeft >= 0x65)
                    {
                        mInt1c0 += 100;
                    }
                    else
                    {
                        mInt1c0 += 10;
                    }
                    if (mShotScore < mInt1c0)
                    {
                        mInt1c0 = mShotScore;
                    }
                    // Port change (user request): the digits count up on every update as in the original,
                    // but the tick sound plays at most FEVER_COUNT_RATE times a second.
                    mFeverCountAccum += FEVER_COUNT_RATE;
                    if (mFeverCountAccum >= UPDATE_RATE)
                    {
                        mFeverCountAccum -= UPDATE_RATE;
                        mBoard.mSoundMgr.AddSound(Res.SOUND_SCORECOUNTER);
                    }
                    if (mInt3c == 0)
                    {
                        return false;
                    }
                    FloatingText aScoreText = mBoard.mFloatingTextMgr.GetTextById(mInt3c);
                    if (aScoreText == null)
                    {
                        return false;
                    }
                    aScoreText.SetText(SexyUtil.CommaSeperate(mInt1c0));
                    aScoreText.mFontId = 0;
                    if (mShotScore <= mInt1c0 && UpdateBallBonus(true))
                    {
                        mInt1c4 = 0x14;
                    }
                    return true;
                }
                return UpdateBallBonus(false);
            }
            if (mStateCount == aTextTime)
            {
                AddFeverScoreText();
            }
            mFeverCountAccum = UPDATE_RATE - FEVER_COUNT_RATE;
            return true;
        }

        private const int UPDATE_RATE = 100;

        private const int FEVER_COUNT_RATE = 30;

        private int mFeverCountAccum;

        /// <summary>After the pegs are cleared: triple score, the no-orange penalty, the "Total Miss" coin flip, and banking the score.</summary>
        public void UpdatePostShot()
        {
            bool busy = false;
            if (mDimLowObjects)
            {
                busy = UpdateFeverScoreText();
            }
            if (mInt0c != 0 || busy)
            {
                return;
            }
            SoundMgr aSoundMgr = mBoard.mSoundMgr;
            FloatingText aCountText = mBoard.mFloatingTextMgr.GetTextById(mInt178);
            FloatingText aScoreText = mBoard.mFloatingTextMgr.GetTextById(mInt3c);
            FloatingText aStyleText = mBoard.mFloatingTextMgr.GetTextById(mInt40);
            if (mTripleScore && mInt1b4 == 0 && (!mDuelMode || mFlag18))
            {
                mInt1b4 = 1;
            }
            if (mInt1b4 >= 1 && mInt1b4 <= 0x7c)
            {
                mInt1b4++;
                if (mInt1b4 == 0x4b && aCountText != null)
                {
                    aSoundMgr.AddSound(Res.SOUND_AAH);
                    IncShotScore(mShotScore << 1);
                    aCountText.SetText(SexyAppBase.Loc("Score Tripled!"));
                    mInt1c0 = mShotScore;
                    if (aScoreText != null)
                    {
                        aScoreText.SetText(SexyUtil.CommaSeperate(mShotScore));
                    }
                }
                return;
            }
            if (!IsShotScorePenalty())
            {
                bool noFreeBalls = mBoard.mTrophyInfo != null && mBoard.mTrophyInfo.mNoFreeballs;
                if (mShotScore < 1 && mInt188 == 0 && !mDuelMode && !mFlag19 && !noFreeBalls)
                {
                    aSoundMgr.AddSound(Res.SOUND_COINSPIN);
                    mBoard.EndShot();
                    mBoard.mInterfaceMgr.EndShot();
                    mBoard.mEffectMgr.AddCoinFlip(325.0f, 280.0f, GetTotalMissIsFreeBall());
                    mFlag1B = true;
                    SetState(STATE_TOTAL_MISS);
                    return;
                }
            }
            else
            {
                float aPenalty = -0.25f;
                if (mBoard.mTrophyInfo != null && mBoard.mTrophyInfo.mScorePenaltyForNoOrange != 0)
                {
                    aPenalty = (float)mBoard.mTrophyInfo.mScorePenaltyForNoOrange / 100.0f;
                }
                if (aCountText != null)
                {
                    int aDelay = mInt188 < 1 ? 0 : 0x1e;
                    aCountText.mUpdateCnt = -aDelay;
                    if (aScoreText != null)
                    {
                        aScoreText.mUpdateCnt = aCountText.mUpdateCnt;
                    }
                    if (0.0f <= aPenalty)
                    {
                        aSoundMgr.AddSoundDelay(Res.SOUND_RAINBOW, aDelay);
                    }
                    else
                    {
                        aSoundMgr.AddSoundDelay(Res.SOUND_PENALTY, aDelay);
                    }
                }
                int aSign = 0.0f <= aPenalty ? 1 : -1;
                mShotScore = 0;
                mStyleScore = 0;
                float aCurScore = (float)mScore[mCurPlayer];
                double aSteps = Math.Floor((double)(aCurScore * Math.Abs(aPenalty) / 5.0f));
                int aChange = aSign * Math.Abs((int)(aSteps * 5.0f));
                if (aChange != 0)
                {
                    IncScore(aChange, false);
                }
            }
            if (!mDimLowObjects)
            {
                mStats[mCurPlayer].CheckBestShot(mShotScore);
            }
            else
            {
                mStats[mCurPlayer].mBestValue = mShotScore;
            }
            mInt16c = mStyleScore + mInt16c;
            IncScore(mShotScore + mStyleScore, false);
            if (mDimLowObjects)
            {
                mBoard.mEffectMgr.FinishFeverRainbow();
            }
            if (aScoreText != null)
            {
                int aCountWait = aCountText.mUpdateCnt < 1 ? 0 : aCountText.mUpdateCnt;
                int aScoreWait = aScoreText.mUpdateCnt < 1 ? 0 : aScoreText.mUpdateCnt;
                int aHold = aStyleText != null ? 0x82 : 100;
                aScoreText.HoldPos(aHold);
                aScoreText.MoveTo(mCurPlayer == 0 ? 170.0f : 530.0f, -60.0f, 10);
                aScoreText.AddScale(1.0f, aHold + aScoreWait);
                aScoreText.AddScale(0.5f, aHold + aScoreWait + 10);
                aScoreText.mDuration = aScoreWait + aHold + 10;
                mBoard.mInterfaceMgr.AddScoreUpdateDelay(aScoreText.mDuration - aScoreText.mUpdateCnt);
                if (aCountText != null)
                {
                    aCountText.mDuration = aCountWait + aHold;
                }
                if (aStyleText != null)
                {
                    aHold = aHold + -0x14;
                    aStyleText.mUpdateCnt = -0x14;
                    aStyleText.HoldPos(aHold);
                    aStyleText.MoveTo(mCurPlayer == 0 ? 170.0f : 530.0f, -60.0f, 10);
                    aStyleText.AddScale(1.0f, aHold);
                    aStyleText.AddScale(0.5f, aHold + 10);
                    aStyleText.mDuration = aHold + 10;
                }
            }
            mBoard.EndShot();
            mBoard.mInterfaceMgr.EndShot();
            SetState(STATE_POST_POST_SHOT);
        }

        /// <summary>Whether a total miss's coin flip comes up "free ball": always the first time, then 60% (80% on the last ball).</summary>
        public bool GetTotalMissIsFreeBall()
        {
            int aChance = mBallsLeft[mCurPlayer] < 1 ? 0x50 : 0x3c;
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            if (!mDuelMode && !mIsComputer[0] && aPlayer != null && !aPlayer.IsTipSet(2))
            {
                return true;
            }
            return mInt30 % 100 <= aChance;
        }

        public void UpdateTotalMiss()
        {
            Effect aCoin = mBoard.mEffectMgr.GetEffectByType(8);
            if (aCoin != null && aCoin.mValue >= 0)
            {
                return;
            }
            bool freeBall = false;
            if (!GetTotalMissIsFreeBall())
            {
                mBoard.mSoundMgr.AddSound(Res.SOUND_COINSPIN_NO);
            }
            else
            {
                mBoard.mSoundMgr.AddSound(Res.SOUND_FREEBALL);
                IncNumBalls(1, 0, false);
                freeBall = true;
            }
            if (!freeBall)
            {
                BeginTurn(true);
            }
            else
            {
                SetState(STATE_POST_POST_SHOT);
            }
        }

        public void UpdatePostPostShot()
        {
            if ((!mDuelMode || mStateCount > 0x95) && mBoard.mInterfaceMgr.mBallsSettled)
            {
                if (!ClearedLevel(false))
                {
                    BeginTurn(true);
                }
                else
                {
                    DoLevelDone();
                }
            }
        }

        /// <summary>Port addition: Peggle Extreme's levels in its order (stages_extreme.cfg: stage * 5 + level).</summary>
        private static readonly string[] EXTREME_LEVELS =
        {
            "vortogon", "hunter", "headcrab", "toasty", "bjornnheavy",
            "sentry", "headshot", "portal1", "waterfall", "crates"
        };

        /// <summary>
        /// Port addition: Peggle Extreme's level-won and level-lost sounds, LEVELWON..LEVELWON4 and
        /// LEVELLOST..LEVELLOST4 (tools/build_extreme_assets.py), loaded once.
        /// </summary>
        private static int[] sExtremeWinSounds;

        private static int[] sExtremeLostSounds;

        /// <summary>
        /// Port addition: Peggle Extreme picks its level-won and level-lost sounds by the level (PeggleExtreme.exe
        /// LogicMgr, 0x46ccc0-0x46cd95): levels 1-3 the first, 4-6 the second, 7 the fourth, 8-10 the third
        /// (won: gman_win03, heavy_goodjob, excellent_work, clear; lost: flatline, heavy_neg, PortalLost, blow).
        /// Here for its Adventure and Quick Play; every other level, or a missing file, gets theDefault.
        /// </summary>
        private int GetExtremeSound(ref int[] theSounds, string theFile, int theDefault)
        {
            int aMode = mBoard.mGameMode;
            if (aMode != GameMode.GAMEMODE_ADVENTURE && aMode != GameMode.GAMEMODE_QUICKPLAY)
            {
                return theDefault;
            }
            int anIndex = Array.IndexOf(EXTREME_LEVELS, SexyUtil.GetFileName(mBoard.mLevelName, true).ToLowerInvariant());
            if (anIndex < 0)
            {
                return theDefault;
            }
            if (theSounds == null)
            {
                theSounds = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    theSounds[i] = mBoard.mApp.mSoundManager.LoadSound("sounds/" + theFile + (i == 0 ? "" : (i + 1).ToString()) + ".wav");
                }
            }
            int aSound = anIndex == 6 ? 3 : anIndex < 3 ? 0 : anIndex > 6 ? 2 : 1;
            return theSounds[aSound] >= 0 ? theSounds[aSound] : theDefault;
        }

        /// <summary>Port addition: whether the level being played is one of Peggle Extreme's.</summary>
        private bool IsExtremeLevel()
        {
            return Array.IndexOf(EXTREME_LEVELS, SexyUtil.GetFileName(mBoard.mLevelName, true).ToLowerInvariant()) >= 0;
        }

        /// <summary>Port addition: Peggle Extreme's SOUND_PORTAL (portal_extreme.wav), loaded once; -2 before then.</summary>
        private static int sExtremePortalSound = -2;

        /// <summary>
        /// Port addition: in Peggle Extreme's levels a ball going through a portal plays SOUND_PORTAL
        /// (PeggleExtreme.exe 0x470058, after the portal effect and the move to the exit).
        /// </summary>
        private void PlayPortalSound()
        {
            if (!IsExtremeLevel())
            {
                return;
            }
            if (sExtremePortalSound == -2)
            {
                sExtremePortalSound = mBoard.mApp.mSoundManager.LoadSound("sounds/portal_extreme.wav");
            }
            if (sExtremePortalSound >= 0)
            {
                mBoard.mSoundMgr.AddSound(sExtremePortalSound);
            }
        }

        /// <summary>The level-won sound (Port: Peggle Extreme's levels have their own, GetExtremeSound).</summary>
        private int GetLevelWonSound()
        {
            return GetExtremeSound(ref sExtremeWinSounds, "levelwon_extreme", Res.SOUND_LEVELWON);
        }

        /// <summary>The level-lost sound (Port: Peggle Extreme's levels have their own, GetExtremeSound).</summary>
        private int GetLevelLostSound()
        {
            return GetExtremeSound(ref sExtremeLostSounds, "levellost_extreme", Res.SOUND_LEVELLOST);
        }

        public void UpdateLevelDone()
        {
            if (!mMuteLoops)
            {
                if (mBoard.mGameMode == GameMode.GAMEMODE_DEMO)
                {
                    if (!mBoard.mInterfaceMgr.IsScoreAccurate())
                    {
                        mStateCount = 0;
                    }
                    if (mStateCount > 100)
                    {
                        mBoard.Reset();
                    }
                    return;
                }
                if (mFlag155 && mStateCount < 100)
                {
                    mStateCount = 0;
                }
                if (!mMuteLoops && mStateCount > 100 && mBoard.mEndLevelDialog.mWidgetManager == null &&
                    mBoard.mInterfaceMgr.IsScoreAccurate())
                {
                    mBoard.ShowEndLevelDialog();
                    SoundMgr aSoundMgr = mBoard.mSoundMgr;
                    if (!mDuelMode)
                    {
                        if (ClearedLevel(false))
                        {
                            aSoundMgr.AddSound(GetLevelWonSound());
                        }
                        else if (mBoard.mLogicMgr.BeatLevel())
                        {
                            aSoundMgr.AddSound(Res.SOUND_LEVELDONE);
                        }
                        else
                        {
                            aSoundMgr.AddSound(GetLevelLostSound());
                        }
                    }
                    else
                    {
                        aSoundMgr.AddSound(Res.SOUND_LEVELDONE);
                    }
                }
                return;
            }
            if (!mBoard.mIsReplay && mStateCount == 0x8c)
            {
                FloatingText aText = AddStandardText(SexyAppBase.Loc("Instant Replay Finished!\nHit ^FF0101^Stop Button^oldclr^ to Return to Game.\nHit ^4B69EF^Play Button^oldclr^ to Watch Again."),
                    325.0f, 160.0f, 6);
                aText.mVelY = 0;
                aText.mVelX = 0;
                aText.mColor = 0xffffff;
                aText.mBackShade = true;
                aText.mFadeIn = 0x14;
                aText.mDuration = 0;
                aText.mLayer = 2;
                mBoard.mReplayDialog.BlinkDoneButton();
            }
            if (mStateCount == 100 && mBoard.mIsReplay && mBoard.mReplayWriters.Count < 2 && !mBoard.mReplayStarted)
            {
                bool ready = true;
                if (!mBoard.mInterfaceMgr.IsScoreAccurate())
                {
                    ready = false;
                }
                else
                {
                    Effect aRibbon = mBoard.mEffectMgr.GetEffectByType(7);
                    if (aRibbon != null && aRibbon.mUpdateCnt < 100)
                    {
                        ready = false;
                    }
                }
                if (!ready)
                {
                    mStateCount--;
                }
                else
                {
                    mBoard.mApp.DoReplayFileDialog(true);
                }
            }
        }

        /// <summary>ZenBall: the AI searches for the best shot from the current aim, then fires it.</summary>
        public void UpdateZenShot()
        {
            Gun aGun = mBoard.mGun;
            float aTextX = aGun.mCenterX;
            float aTextY = aGun.mCenterY + 110.0f;
            if (mStateCount == 1)
            {
                mBoard.mSoundMgr.AddSoundPitch(Res.SOUND_GONG, 5.0f, 5);
                FloatingText aText = AddStandardText(SexyAppBase.Loc("Divining Best Shot..."), aGun.mCenterX, aGun.mCenterY + 110.0f, 8);
                aText.mFadeOut = 10;
                aText.mFadeIn = 10;
                aText.mDuration = 0x78;
                aText.mLayer = 3;
            }
            else if (mStateCount == 0x82 || mZenThinking)
            {
                string aMessage = SexyAppBase.Loc("Zen Achieved!");
                if (!mMuteLoops)
                {
                    mZenThinking = true;
                    DoHelperShot(false);
                    if (!mZenThinking)
                    {
                        mInt6c = mStateCount;
                        AIBall anAimBall = mBoard.mAIMgr.mAimBall;
                        AIBall aBestBall = mBoard.mAIMgr.mBestBall;
                        bool improved = false;
                        if (anAimBall == null)
                        {
                            improved = true;
                        }
                        else if (aBestBall == null)
                        {
                            improved = false;
                        }
                        else if (anAimBall.mGoalPegsLeft == 0)
                        {
                            improved = false;
                        }
                        else if (anAimBall.mScore < aBestBall.mScore)
                        {
                            improved = true;
                        }
                        else if (aBestBall.mGoalPegsLeft == 0 && !mClearLevelMode)
                        {
                            improved = true;
                        }
                        if (!improved)
                        {
                            aMessage = SexyAppBase.Loc("No Improvement!");
                        }
                        else if (anAimBall != null && aBestBall != null && anAimBall.mPoints < aBestBall.mPoints && anAimBall.mPoints != 0)
                        {
                            int aPercent = aBestBall.mPoints * 100 / anAimBall.mPoints + -100;
                            if (aPercent % 10 != 0)
                            {
                                aPercent = aPercent - aPercent % 10 + 10;
                            }
                            if (aPercent < 500)
                            {
                                aMessage = CFormat.StrFormat(SexyAppBase.Loc("%d%% More Zen!"), aPercent);
                            }
                            else
                            {
                                aMessage = SexyAppBase.Loc("MAXIMUM ZEN ACHIEVED!!");
                            }
                        }
                    }
                }
                if (!mZenThinking)
                {
                    FloatingText aText = AddStandardText(aMessage, aTextX, aTextY, 8);
                    aText.mFadeOut = 10;
                    aText.mFadeIn = 10;
                    aText.mLayer = 3;
                    mBoard.mSoundMgr.AddSound(Res.SOUND_GONG);
                    aGun.SetAngle(mFloatD0, true);
                    aGun.UpdateBouncyGuide();
                    Ball aBall = aGun.mBall;
                    double anAngle = (double)(mFloatD0 + 1.5707964f);
                    float aX = (float)((double)aBall.GetX() + Math.Sin(anAngle) * 24.0);
                    float aY = (float)((double)aBall.GetY() + Math.Cos(anAngle) * 24.0);
                    mBoard.mEffectMgr.AddZenPowerup(aX, aY);
                }
            }
            else if (mInt6c > 0 && mInt6c + 0x1e <= mStateCount)
            {
                mBoard.mCharacterMgr.SetYinYangEye(false);
                mInt6c = -1;
                Ball aBall = aGun.Fire(true);
                mBoard.mSoundMgr.AddSound(Res.SOUND_CANNONSHOT);
                mBoard.mEffectMgr.AddCannonBlast(aBall.GetX(), aBall.GetY(), aGun.mAngle);
                mBoard.AddObj(aBall);
                mFlagD8 = false;
                mFlagD9 = false;
                BeginShot(true);
            }
        }

        /// <summary>Asks the AI for the best shot (ZenBall, or the stealth helper for struggling adventure players).</summary>
        public bool DoHelperShot(bool stealth)
        {
            if (stealth && mBoard.mGun.mDoBouncyGuide)
            {
                return false;
            }
            float aSaved = mGunAngle;
            mFloatD0 = aSaved;
            AIMgr anAI = mBoard.mAIMgr;
            if ((stealth || mZenThinking) && !anAI.mThinking)
            {
                AIMgr.mSkillLevel = 10;
                anAI.mDelayFrames = 0;
                anAI.mFramesPerUpdate = 10000;
                if (stealth)
                {
                    anAI.mFramesPerUpdate = 600 / mIntDC;
                }
                else if (mZenThinking)
                {
                    anAI.mFramesPerUpdate = 1;
                }
                // Mode 1 = ZenBall (search around the player's aim), 2 = stealth helper.
                anAI.BeginThinking(false, aSaved, stealth ? 2 : 1, stealth ? mIntDC : -1);
            }
            if (!stealth)
            {
                anAI.Update();
            }
            float aTemp = mGunAngle;
            mGunAngle = mFloatD0;
            mFloatD0 = aTemp;
            return true;
        }

        public int GetAdventureLevelReplayBonus()
        {
            switch (mInt1bc)
            {
            case 3:
                return 25000;
            case 2:
                return 50000;
            case 1:
                return 100000;
            default:
                return 0;
            }
        }

        /// <summary>Secretly aims for players who keep failing an adventure level on their last balls.</summary>
        public bool CheckDoStealthHelperShot()
        {
            if (mBoard.mGameMode != GameMode.GAMEMODE_ADVENTURE && mBoard.mGameMode != GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                return false;
            }
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            if (aPlayer == null || aPlayer.GetmAdventureWinCount() >= 1)
            {
                return false;
            }
            int aReplays = mInt1bc;
            int aBalls = 0;
            int aTries = aReplays + -1;
            if (mBoard.mApp.mCurStage == 0 && mBoard.mApp.mCurLevel == 0)
            {
                aTries = aReplays;
                aBalls = 6;
            }
            if (aTries >= 4 && aBalls <= 2)
            {
                aBalls = 3;
            }
            else if (aTries >= 3 && aBalls <= 1)
            {
                aBalls = 2;
            }
            else if (aTries > 1 && aBalls == 0)
            {
                aBalls = 1;
            }
            if (aBalls < mBallsLeft[mCurPlayer])
            {
                return false;
            }
            return DoHelperShot(true);
        }

        /// <summary>Aims the launcher, shows the fireball sparks, and fires (or starts a Zen shot) once a tap is released.</summary>
        public void UpdateGun()
        {
            Gun aGun = mBoard.mGun;
            if (aGun == null)
            {
                return;
            }
            if (mFlag1F && !mMuteLoops)
            {
                mFlag1F = false;
                if ((mBoard.mEndLevelDialog.mWidgetManager == null || !mBoard.mEndLevelDialog.mVisible) && !mBoard.mShowingGameTip)
                {
                    CalcGunAngle(false);
                }
            }
            float aPrevAngle = aGun.mAngle;
            if (aGun.SetAngle(mGunAngle, false) && aGun.mBall != null && mPowerupCount[12][mCurPlayer] > 0 && mState == STATE_AIMING)
            {
                int aEvery = 5;
                if (Math.Abs((double)(aPrevAngle - mGunAngle)) < (double)SexyUtil.DegToRad(3.0f))
                {
                    aEvery = 10;
                }
                if (mStateCount % aEvery == 0)
                {
                    Ball aBall = aGun.mBall;
                    Effect aFlame = mBoard.mEffectMgr.AddEffect(0x2e, aBall.mX, aBall.mY, 0, false);
                    aFlame.mDuration = 100;
                    aFlame.mGravity = Ball.mGravity * 0.5f;
                }
            }
            if (aGun.mBall != null && aGun.mBall.mElectrified)
            {
                mObj400 = aGun.TryFindCollisionPeg();
            }
            if (aGun.mBall == null || !aGun.mBall.mElectrified)
            {
                mObj400 = null;
            }
            if (mIntDC < 1)
            {
                if (mFlagD9)
                {
                    mFlagD9 = false;
                    mFlagD8 = true;
                    mInt98 = mBoard.mWidgetManager.mLastMouseX;
                    mInt9c = mBoard.mWidgetManager.mLastMouseY;
                    mBoard.mSoundMgr.AddSound(Res.SOUND_CANNONCOCK);
                    if (mPowerupCount[7][mCurPlayer] < 1)
                    {
                        mIntDC = 8;
                        mFlagDA = CheckDoStealthHelperShot();
                        return;
                    }
                }
            }
            else
            {
                mIntDC--;
                mFlagD9 = false;
            }
            if (mIntDC != 0 || !mFlagD8)
            {
                return;
            }
            if (!aGun.CanFire() || mBoard.mFlagBA || mState == STATE_ZEN_SHOT)
            {
                return;
            }
            mFlagA1 = aGun.NeedDrawMouseClick();
            if (mPowerupCount[7][mCurPlayer] > 0)
            {
                SetState(STATE_ZEN_SHOT);
                aGun.SetDoBouncyGuide(true);
                aGun.mSuperGuide = true;
                aGun.SetAngle(mGunAngle, true);
                aGun.UpdateBouncyGuide();
            }
            else
            {
                mFlagD8 = false;
                mFlagD9 = false;
                mIntDC = 0;
                Ball aBall = aGun.Fire(true);
                mBoard.AddObj(aBall);
                BeginShot(true);
                if (mFlagDA)
                {
                    aGun.SetAngle(mGunAngle, true);
                }
                mFloatD0 = 0;
                if (aBall.mFireball || aBall.mElectrified)
                {
                    mBoard.mSoundMgr.AddSound(Res.SOUND_FIREBALLSHOT);
                }
                else
                {
                    mBoard.mSoundMgr.AddSound(Res.SOUND_CANNONSHOT);
                }
                mBoard.mEffectMgr.AddCannonBlast(aBall.GetX(), aBall.GetY(), aGun.mAngle);
            }
            mFlagE0 = false;
        }

        public void Update()
        {
#if PEGGLE_IPHONE
            // (No HD fire button to auto-release the flippers from.)
            if (mMuteLoops && mState == STATE_SHOOTING)
#else
            if (!mMuteLoops || mState != STATE_SHOOTING)
            {
                if (mBoard.mFireButton.mButtonImage == Res.IMAGE_INT_FIRE_BUTTON_FLIP_TEXT && !mBoard.mFireButton.mIsDown)
                {
                    FlipperClick(false);
                }
            }
            else
#endif
            {
                // Replays: feed the recorded taps back in at their recorded times.
                while (mClicks.Count != 0 && mClicks[0].mTime <= mStateCount)
                {
                    ClickInfo aClick = mClicks[0];
                    int aButton = !aClick.mFlag5 ? 1 : -1;
                    if (!aClick.mFlag4)
                    {
                        MouseDown(0, 0, aButton, true, false);
                    }
                    else
                    {
                        MouseUp(0, 0, aButton, false);
                    }
                    mClicks.RemoveAt(0);
                }
            }
            if (mIntD4 != 0)
            {
                mIntD4--;
            }
            UpdateGun();
            if (mLightningOrb != null)
            {
                mLightningOrb.Update();
            }
            if (mBoard.mFlagBA)
            {
                return;
            }
            mStateCount++;
            if (mFreeBallRadius != 0)
            {
                UpdateFreeBallRadius();
            }
            if (mInt0c != 0)
            {
                mInt0c--;
                if (mInt0c == 0)
                {
                    RemoveHitPegs();
                }
            }
            switch (mState)
            {
            case STATE_AIMING:
                UpdatePreShot();
                break;
            case STATE_SHOOTING:
                UpdateShot();
                break;
            case STATE_POST_SHOT:
                UpdatePostShot();
                break;
            case STATE_TOTAL_MISS:
                UpdateTotalMiss();
                break;
            case STATE_LEVEL_DONE:
                UpdateLevelDone();
                break;
            case STATE_POST_POST_SHOT:
                UpdatePostPostShot();
                break;
            case STATE_SHOT_EXTENDER:
                UpdateShotExtender();
                break;
            case STATE_INIT_LEVEL:
                UpdateInitLevel();
                break;
            case STATE_CHARACTER_DIALOG:
                UpdateCharacterDialog();
                break;
            case STATE_ZEN_SHOT:
                UpdateZenShot();
                break;
            }
            if (mFlag404 && !mBoard.mFloatingTextMgr.AnyActive())
            {
                mFlag404 = false;
                DoBeginTurnTip(-1);
            }
        }

        /// <summary>In replays, "Replay" in both top corners; and the current temporary power-up's label.</summary>
        public void DrawBack(Graphics g)
        {
            int aY = mBoard.mBoardRect.mY + 0x2d;
            int aLeft = mBoard.mBoardRect.mX + 0x18;
            int aRight = mBoard.mBoardRect.mX + mBoard.mBoardRect.mWidth + -0x18;
            int aH = SexyUtil.gPhysicalDisplayHeight;
            if (mMuteLoops)
            {
                g.SetColor(SexyUtil.MakeColor(0xffff00));
                g.SetFont(Res.FONT_OVERLOAD10OUTLINE);
                string aReplay = SexyAppBase.Loc("Replay");
                int aWidth = g.GetFont().StringWidth(aReplay);
                if (mCornerText.Length != 0)
                {
                    int aCornerWidth = g.GetFont().StringWidth(mCornerText);
                    if (aWidth < aCornerWidth)
                    {
                        aWidth = aCornerWidth;
                    }
                }
                g.DrawString(aReplay, (int)((double)(aH * aLeft) / 600.0), (int)((double)(aY * aH) / 600.0));
                g.DrawString(aReplay, (int)((double)(aH * aRight) / 600.0 - (double)aWidth), (int)((double)(aY * aH) / 600.0));
                aY = (int)((double)aY + (double)g.GetFont().GetHeight() * 600.0 / (double)aH);
            }
            if (mCornerText.Length != 0)
            {
                g.SetColor(SexyUtil.MakeColor(0xffff00));
                g.SetFont(Res.FONT_OVERLOAD10OUTLINE);
                int aWidth = g.GetFont().StringWidth(mCornerText);
                g.DrawString(mCornerText, (int)((double)(aLeft * aH) / 600.0), (int)((double)(aY * aH) / 600.0));
                g.DrawString(mCornerText, (int)((double)(aRight * aH) / 600.0 - (double)aWidth), (int)((double)(aY * aH) / 600.0));
            }
        }

        /// <summary>"Tap to Flip" hints while flippers are up, and the Electrobolt aim hint glow on pegs.</summary>
        public void Draw(Graphics g)
        {
            int aH = SexyUtil.gPhysicalDisplayHeight;
            if (mFlag20 && mState == STATE_SHOOTING && !mMuteLoops)
            {
                g.SetColor(SexyUtil.MakeColor(0xffff00));
                g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
                string aText = SexyAppBase.Loc("Tap to Flip");
                int aY = LOGICMGR_TAP_TO_FLIP_Y;
                int aLeft = (int)((double)(mBoard.mBoardRect.mX * aH) / 600.0 + (double)LOGICMGR_TAP_TO_FLIP_PAD_X);
                int aRight = (int)((double)((mBoard.mBoardRect.mWidth + mBoard.mBoardRect.mX) * aH) / 600.0 - (double)(LOGICMGR_TAP_TO_FLIP_PAD_X << 1));
                g.DrawString(aText, aLeft, aY);
                g.DrawString(aText, aRight - g.GetFont().StringWidth(aText), aY);
            }
            foreach (PhysObj anObj in mList394)
            {
                anObj.mPegInfo.mGlowTarget = 0;
            }
            PhysObj aBucket = mBoard.GetBucket();
            if (mObj400 != null && aBucket != null)
            {
                g.Translate(mBoard.mBoardRect.mX, mBoard.mBoardRect.mY);
                List<PhysObj> aPegs = new List<PhysObj>();
                List<float> aWeights = new List<float>();
                getLightningHintPegs(mObj400, aPegs, aWeights);
                for (int i = 0; i < aPegs.Count; i++)
                {
                    aPegs[i].mPegInfo.mGlowTarget = aWeights[i];
                }
                mObj400.mPegInfo.mGlowTarget = 2.0f;
                g.Translate(-mBoard.mBoardRect.mX, -mBoard.mBoardRect.mY);
            }
        }

        public void DrawFore(Graphics g)
        {
            if (SexyUtil.GetBoard() != null && mLightningOrb.mState != 0)
            {
                mLightningOrb.Draw(g);
            }
        }

        /// <summary>Whether a tap anywhere on screen should reach the board (shooting with no flippers, end of level...).</summary>
        public bool WantTapsAnywhere()
        {
            if (mFlag155)
            {
                return true;
            }
            if (mState != STATE_SHOOTING)
            {
                return false;
            }
            if (!(mPowerupCount[2][mCurPlayer] < 1))
            {
                return true;
            }
            if (!mBoard.mSlowMo)
            {
                return false;
            }
            return !mBoard.mPeggleZoom;
        }
    }
}
