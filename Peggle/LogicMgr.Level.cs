using System;
using System.Collections.Generic;

namespace Sexy
{
    public partial class LogicMgr
    {
        public const int STATE_INIT_LEVEL = 8;

        private static int sLastCurPlayer;

        private static int sLastCurPlayerCount;

        private static void ShufflePegs(List<PhysObj> thePegs)
        {
            if (thePegs.Count > 1)
            {
                for (int i = 0; i < 1000; i++)
                {
                    int a = SexyUtil.Rand() % thePegs.Count;
                    int b = SexyUtil.Rand() % thePegs.Count;
                    PhysObj aTemp = thePegs[a];
                    thePegs[a] = thePegs[b];
                    thePegs[b] = aTemp;
                }
            }
        }

        /// <summary>Collects the level's pegs and turns up to mNumGoalPegs (default 25) of them orange.</summary>
        public void MakeGoalPegs()
        {
            List<PhysObj> aCandidates = new List<PhysObj>();
            foreach (PhysObj anObj in mBoard.mPhysObjs)
            {
                PegInfo aPeg = anObj.mPegInfo;
                if (aPeg == null)
                {
                    continue;
                }
                aPeg.mCrumbleCount = 0;
                aPeg.SetHit(false);
                anObj.SetActive(true);
                mList394.Add(anObj);
                if (!aPeg.mVariable)
                {
                    if (aPeg.mType == 2)
                    {
                        mGoalPegs.Add(anObj);
                    }
                    else
                    {
                        mNonGoalPegs.Add(anObj);
                    }
                }
                else
                {
                    anObj.SetPegType(1);
                    aCandidates.Add(anObj);
                }
            }
            ShufflePegs(aCandidates);
            int aNumGoals = mNumGoalPegs;
            if (aNumGoals == 0)
            {
                aNumGoals = 0x19;
            }
            while (mGoalPegs.Count < aNumGoals && aCandidates.Count != 0)
            {
                mGoalPegs.Add(aCandidates[aCandidates.Count - 1]);
                aCandidates.RemoveAt(aCandidates.Count - 1);
            }
            for (int i = 0; i < aCandidates.Count; i++)
            {
                mNonGoalPegs.Add(aCandidates[i]);
            }
            foreach (PhysObj anObj in mGoalPegs)
            {
                if (anObj.mPegInfo.mType != 1)
                {
                    mBoard.mApp.GetMaxLevel();
                    if (!mBoard.mApp.OnMaxLevel())
                    {
                        anObj.SetPegType(1);
                    }
                }
            }
            foreach (PhysObj anObj in mGoalPegs)
            {
                anObj.SetPegType(2);
            }
        }

        /// <summary>Turns two (one in duels) high-up blue pegs green, at least 150 apart.</summary>
        public void MakePowerupPeg()
        {
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            int aStage = aPlayer.GetmMaxStage() - aPlayer.mFirstAdventureStage;
            int aMinLevel = ThunderballApp.mGameLevelPack == 1 ? 1 : 2;
            // Port: bonus levels don't depend on Adventure progress, except in their own Adventure
            // (Extreme, WoW), whose first stage teaches the pegs as Peggle's and Nights' do.
            if (aStage == 0 && aPlayer.GetmMaxLevel() < aMinLevel && (!mBoard.IsBonusLevel() || ThunderballApp.mBonusAdventure != null))
            {
                return;
            }
            int aWanted = mDuelMode ? 1 : 2;
            float aLastX = -1000.0f;
            float aLastY = -1000.0f;
            List<PhysObj> aCandidates = new List<PhysObj>();
            int aHave = 0;
            foreach (PhysObj anObj in mNonGoalPegs)
            {
                if (anObj.mPegInfo.mType == 4)
                {
                    aLastX = anObj.GetXPos();
                    aLastY = anObj.GetYPos();
                    aHave++;
                }
                else if (anObj.mPegInfo.mType == 1 && anObj.GetYPos() < (float)0x15e)
                {
                    aCandidates.Add(anObj);
                }
            }
            aWanted -= aHave;
            if (aWanted < 1)
            {
                return;
            }
            if (aCandidates.Count == 0)
            {
                foreach (PhysObj anObj in mNonGoalPegs)
                {
                    if (anObj.mPegInfo.mType == 1 && anObj.mPegInfo.mVariable)
                    {
                        aCandidates.Add(anObj);
                    }
                }
            }
            if (aCandidates.Count == 0)
            {
                return;
            }
            ShufflePegs(aCandidates);
            for (int aPass = 0; aPass < 2 && aWanted > 0; aPass++)
            {
                for (int i = 0; i < aCandidates.Count; i++)
                {
                    PhysObj anObj = aCandidates[i];
                    if (anObj.mPegInfo.mType == 4)
                    {
                        continue;
                    }
                    if (aPass == 0)
                    {
                        float x = anObj.GetXPos();
                        float y = anObj.GetYPos();
                        float dx = x - aLastX;
                        float dy = y - aLastY;
                        if (dx * dx + dy * dy < (float)22500)
                        {
                            continue;
                        }
                        aLastX = x;
                        aLastY = y;
                    }
                    anObj.SetPegType(4);
                    if (mState != STATE_NONE)
                    {
                        anObj.mPegInfo.DoFlash(2);
                    }
                    aWanted--;
                    if (aWanted < 1)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>Moves the purple (score) peg to a different random blue peg.</summary>
        public void MakeScorePeg()
        {
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            int aStage = aPlayer.GetmMaxStage() - aPlayer.mFirstAdventureStage;
            int aMinLevel = ThunderballApp.mGameLevelPack != 1 ? 3 : 2;
            if (aStage == 0 && aPlayer.GetmMaxLevel() < aMinLevel && (!mBoard.IsBonusLevel() || ThunderballApp.mBonusAdventure != null))
            {
                return;
            }
            List<PhysObj> aCandidates = new List<PhysObj>();
            foreach (PhysObj anObj in mNonGoalPegs)
            {
                if (anObj.mPegInfo.mType == 3 && anObj != mObj1cc)
                {
                    mObj1cc = anObj;
                    return;
                }
                if (anObj.mPegInfo.mType == 1)
                {
                    aCandidates.Add(anObj);
                }
            }
            if (mObj1cc != null && aCandidates.Count != 0)
            {
                if (mObj1cc.mPegInfo.mType != 2 && !mObj1cc.mPegInfo.mHit)
                {
                    mObj1cc.SetPegType(1);
                }
                mObj1cc = null;
            }
            if (aCandidates.Count != 0)
            {
                int anIndex = SexyUtil.Rand() % aCandidates.Count;
                mObj1cc = aCandidates[anIndex];
                mObj1cc.SetPegType(3);
                if (mState != STATE_NONE)
                {
                    mObj1cc.mPegInfo.DoFlash(2);
                }
            }
        }

        public void ReInitLevel()
        {
            InitLevel(true, false, true);
        }

        public void InitLevel(bool newGame, bool pickRandomPlayer, bool keepObjects)
        {
            int aCurPlayer = mCurPlayer;
            if (aCurPlayer != 0 && aCurPlayer != 1)
            {
                aCurPlayer = 0;
            }
            mBoard.mAIMgr.Clear();
            Clear(false, keepObjects);
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            if (aPlayer != null)
            {
                mInt1b8 = aPlayer.GetmMaxStage();
                mInt1bc = aPlayer.GetmAdventureLevelReplay();
            }
            if (newGame)
            {
                for (int i = 0; i < 2; i++)
                {
                    mBallsLeft[i] = mDuelMode ? 6 : 10;
                    mScore[i] = 0;
                    mInt37c[i] = 0;
                }
            }
            mFreeBallRadius = 0;
            if (!keepObjects)
            {
                // Flippers go to the end of the object list so they're drawn and updated last.
                List<PhysObj> aFlippers = new List<PhysObj>();
                for (int i = 0; i < mBoard.mPhysObjs.Count;)
                {
                    PhysObj anObj = mBoard.mPhysObjs[i];
                    if (anObj.mId == "flipper")
                    {
                        aFlippers.Add(anObj);
                        mBoard.mPhysObjs.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                foreach (PhysObj anObj in aFlippers)
                {
                    mBoard.mPhysObjs.Add(anObj);
                }
            }
            else
            {
                foreach (PhysObj anObj in mList394)
                {
                    anObj.mPegInfo.Reset();
                }
            }
            CalcGunAngle(true);
            if (!Board.EditingLevel() && !keepObjects)
            {
                MakeGoalPegs();
                MakeScorePeg();
                MakePowerupPeg();
            }
            mInt170 = CalcScoreMult(mGoalPegs.Count);
            mInt174 = CalcMusicIntensity(mGoalPegs.Count);
            mInt24 = mDuelMode ? 1 : 0;
            mDimLowObjects = false;
            mFlagE3 = false;
            if (!mDuelMode)
            {
                mCurPlayer = 0;
            }
            else if (!pickRandomPlayer)
            {
                mCurPlayer = aCurPlayer;
            }
            else
            {
                mCurPlayer = SexyUtil.Rand() % 2;
                if (mCurPlayer == sLastCurPlayer)
                {
                    sLastCurPlayerCount++;
                    if (sLastCurPlayerCount > 1)
                    {
                        mCurPlayer = 1 - mCurPlayer;
                        sLastCurPlayerCount = 1;
                        sLastCurPlayer = mCurPlayer;
                    }
                }
                else
                {
                    sLastCurPlayerCount = 1;
                    sLastCurPlayer = mCurPlayer;
                }
            }
            bool covered = false;
            if (mBoard.mTrophyInfo != null)
            {
                covered = mBoard.mTrophyInfo.mFreeballCovered;
            }
            ActivateFreeBallCover(covered);
            mBoard.mInterfaceMgr.SettleDown();
            mBoard.mCharacterMgr.SetCurCharacter(mCharacters[mCurPlayer]);
            if (mList394.Count != 0)
            {
                if (!newGame && mBoard.mResetCount != 1)
                {
                    mBoard.mInterfaceMgr.SetNumBalls(mBallsLeft[mCurPlayer]);
                }
                else
                {
                    mBoard.mInterfaceMgr.SetNumBalls(0);
                    mBoard.mInterfaceMgr.AddTopBalls(mBallsLeft[mCurPlayer]);
                }
            }
            if (mDuelMode && pickRandomPlayer)
            {
                foreach (PhysObj anObj in mBoard.mPhysObjs)
                {
                    if (anObj.mShapeType == PhysObj.SHAPE_HOLE && anObj.mId == "fever")
                    {
                        Hole aHole = (Hole)anObj;
                        aHole.mValue = aHole.mValue / 2;
                    }
                }
            }
            TrophyInfo aTrophy = mBoard.mTrophyInfo;
            if (aTrophy != null && !mMuteLoops)
            {
                for (int i = 0; i < 0xf; i++)
                {
                    mPowerupCount[i][0] = aTrophy.mPowerups[i] + mPowerupCount[i][0];
                    mPowerupCount[i][1] = aTrophy.mPowerups[i] + mPowerupCount[i][1];
                }
                if (aTrophy.mGravityMod != 1.0f)
                {
                    Ball.mGravity = Ball.mGravity * aTrophy.mGravityMod;
                }
                if (aTrophy.mProjSpeedMod != 1.0f)
                {
                    mBoard.mGun.mFireSpeed = mBoard.mGun.mFireSpeed * aTrophy.mProjSpeedMod;
                }
                mHatCount[mCurPlayer] += aTrophy.mMagicHat;
                mTripleScoreCount[mCurPlayer] += aTrophy.mTripleScore;
                if (aTrophy.mNoGreens)
                {
                    foreach (PhysObj anObj in mList394)
                    {
                        if (anObj.mPegInfo.mType == 4)
                        {
                            anObj.mPegInfo.mType = 1;
                        }
                    }
                }
            }
            BeginInitLevel();
        }

        public int CalcScoreMult(int theGoalPegsLeft)
        {
            if (theGoalPegsLeft < 1 && !mClearLevelMode)
            {
                return 100;
            }
            if (theGoalPegsLeft < 4)
            {
                return 10;
            }
            if (theGoalPegsLeft < 7)
            {
                return 5;
            }
            if (theGoalPegsLeft < 0xb)
            {
                return 3;
            }
            if (theGoalPegsLeft < 0x10)
            {
                return 2;
            }
            return 1;
        }

        public int CalcMusicIntensity(int theGoalPegsLeft)
        {
            if (theGoalPegsLeft < 4)
            {
                return 6;
            }
            if (theGoalPegsLeft < 7)
            {
                return 5;
            }
            if (theGoalPegsLeft < 0xb)
            {
                return 4;
            }
            if (theGoalPegsLeft < 0x10)
            {
                return 3;
            }
            if (theGoalPegsLeft < 0x15)
            {
                return 2;
            }
            return 1;
        }

        public void ActivateFreeBallCover(bool active)
        {
            if (!mDimLowObjects || !active)
            {
                foreach (PhysObj anObj in mBoard.mPhysObjs)
                {
                    if (anObj.mId == "freeballcover")
                    {
                        anObj.SetActiveWithGrowAnim(active);
                    }
                }
            }
        }

        public void BeginInitLevel()
        {
            if (mList394.Count != 0)
            {
                SetState(STATE_INIT_LEVEL);
                foreach (PhysObj anObj in mList394)
                {
                    anObj.mVisible = false;
                }
                mInt40 = 0;
                mInt44 = 0;
                mInt3c = 0;
                mTurnTextId = 0;
                mBoard.RefreshEmitters();
            }
        }

        /// <summary>CheckFlipperOverlap: whether an object sits where the flippers would be (or is flagged to).</summary>
        private static bool CheckFlipperOverlap(PhysObj theObj, Poly theLeft, Poly theRight)
        {
            if (theObj.mFlipperFlags != 0)
            {
                if (theObj.mFlipperFlags == 1)
                {
                    return true;
                }
                if (theObj.mFlipperFlags == 2)
                {
                    return false;
                }
            }
            SexyVector2 aPos = theObj.GetCenter();
            if (aPos.y < 400.0f)
            {
                return false;
            }
            if (aPos.x < 150.0f)
            {
                if (aPos.x < 80.0f)
                {
                    return true;
                }
                if (theLeft.PointInside(aPos.x, aPos.y))
                {
                    return true;
                }
            }
            if (500.0f < aPos.x)
            {
                if (570.0f < aPos.x)
                {
                    return true;
                }
                if (theRight.PointInside(aPos.x, aPos.y))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Before flippers appear: deactivates pegs in the bottom corners, and re-homes any orange
        /// pegs lost that way onto shuffled blue pegs outside those corners.
        /// </summary>
        public void ClearFlipperSpace()
        {
            if (mFlag157)
            {
                return;
            }
            if (mState != STATE_INIT_LEVEL)
            {
                mFlag157 = true;
            }
            int aLostGoals = 0;
            Poly aLeft = new Poly();
            Poly aRight = new Poly();
            aLeft.AddPoint(0.0f, 380.0f);
            aLeft.AddPoint(110.0f, 470.0f);
            aLeft.AddPoint(110.0f, 600.0f);
            aLeft.AddPoint(0.0f, 600.0f);
            aLeft.Close();
            aRight.AddPoint(650.0f, 380.0f);
            aRight.AddPoint(540.0f, 470.0f);
            aRight.AddPoint(540.0f, 600.0f);
            aRight.AddPoint(650.0f, 600.0f);
            aRight.Close();
            foreach (PhysObj anObj in mList394)
            {
                if (anObj.IsVisible() && CheckFlipperOverlap(anObj, aLeft, aRight))
                {
                    anObj.SetActive(false);
                    mNonGoalPegs.Remove(anObj);
                    if (mGoalPegs.Remove(anObj))
                    {
                        aLostGoals++;
                    }
                }
            }
            foreach (PhysObj anObj in mList394)
            {
                if (!(mNonGoalPegs.Count < aLostGoals))
                {
                    break;
                }
                if (!anObj.mVisible && anObj.mPegInfo.mVariable && !CheckFlipperOverlap(anObj, aLeft, aRight))
                {
                    anObj.SetPegType(1);
                    anObj.mPegInfo.SetHit(false);
                    anObj.SetActive(true);
                    mNonGoalPegs.Add(anObj);
                }
            }
            List<PhysObj> aCandidates = new List<PhysObj>(mNonGoalPegs);
            ShufflePegs(aCandidates);
            if (aCandidates.Count < aLostGoals)
            {
                aLostGoals = aCandidates.Count;
            }
            for (int i = 0; i < aLostGoals; i++)
            {
                PhysObj anObj = aCandidates[i];
                anObj.SetPegType(2);
                mNonGoalPegs.Remove(anObj);
                mGoalPegs.Add(anObj);
            }
        }

        /// <summary>Opens/closes the free-ball holes and bumper holes (the green "Spooky"/"Pyramid" bucket covers).</summary>
        public void ActivateFreeBall(bool active)
        {
            if (mDimLowObjects && active)
            {
                return;
            }
            bool aBumpersActive = false;
            if (active)
            {
                aBumpersActive = mPowerupCount[4][mCurPlayer] > 0;
            }
            foreach (PhysObj anObj in mBoard.mPhysObjs)
            {
                if (anObj.mId == "bumperhole")
                {
                    anObj.SetActiveWithGrowAnim(aBumpersActive);
                    if (anObj.mShapeType == PhysObj.SHAPE_POLY && anObj.mSubId == 1 && mState == STATE_SHOOTING)
                    {
                        ((Poly)anObj).mHighlightCounter = 300;
                    }
                    if (anObj.mSubId == 1)
                    {
                        anObj.mCollidable = false;
                    }
                    else
                    {
                        anObj.mVisible = false;
                    }
                }
                else if (anObj.mId == "freeball")
                {
                    anObj.SetActive(active);
                    if (anObj.mShapeType != PhysObj.SHAPE_HOLE)
                    {
                        if (!anObj.mBaseObject)
                        {
                            anObj.mVisible = false;
                        }
                        else
                        {
                            anObj.mCollidable = false;
                        }
                    }
                }
            }
        }

        public void ActivatePowerup(int theType, bool active)
        {
            switch (theType)
            {
            case 1:
                if (mState != STATE_SHOOTING)
                {
                    mBoard.mGun.SetDoBouncyGuide(active);
                }
                break;
            case 2:
                if (!active)
                {
                    FlipperClick(false);
                }
                else
                {
                    ClearFlipperSpace();
                    PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
                    if (!mIsComputer[mCurPlayer] && !mMuteLoops && aPlayer != null && aPlayer.mDuelRounds < 2)
                    {
                        mFlag20 = true;
                    }
                }
                foreach (PhysObj anObj in mBoard.mPhysObjs)
                {
                    if (anObj.mId == "flipper")
                    {
                        anObj.SetActiveWithGrowAnim(active);
                        if (active && mState == STATE_SHOOTING && anObj.mShapeType == PhysObj.SHAPE_POLY)
                        {
                            ((Poly)anObj).mHighlightCounter = 0xb4;
                        }
                        if (anObj.mMover == null)
                        {
                            anObj.mCollidable = false;
                        }
                    }
                }
                break;
            case 4:
                if (active && mState == STATE_SHOOTING)
                {
                    ActivateFreeBall(true);
                }
                else if (!active)
                {
                    mBoard.mEffectMgr.EraseAllOfType(0x18);
                }
                break;
            case 7:
                if (active && mState != STATE_SHOOTING)
                {
                    mBoard.mCharacterMgr.SetYinYangEye(true);
                }
                break;
            case 10:
                mInt1a4 = active ? 100 : 0;
                break;
            case 0xc:
                if (mState != STATE_SHOOTING)
                {
                    bool covered = false;
                    if (mBoard.mTrophyInfo != null)
                    {
                        covered = mBoard.mTrophyInfo.mFreeballCovered;
                    }
                    ActivateFreeBallCover(covered || active);
                }
                break;
            }
            if (theType == 2 || theType == 4)
            {
                if (mPowerupCount[2][mCurPlayer] == 0)
                {
                    mFreeBallRadius = mPowerupCount[4][mCurPlayer] == 0 ? 0x104 : 200;
                }
                else
                {
                    mFreeBallRadius = 0x82;
                }
            }
        }

        /// <summary>Raises (or drops) every flipper that has a mover; plays the flipper sound if any moved.</summary>
        public void FlipperClick(bool up)
        {
            bool moved = false;
            foreach (PhysObj anObj in mBoard.mPhysObjs)
            {
                if (anObj.mId != "flipper" || anObj.mMover == null)
                {
                    continue;
                }
                int aPeriod = anObj.mMover.mPeriod;
                if (aPeriod > 0)
                {
                    int aPhase = anObj.mMoveUpdateCnt % aPeriod;
                    if (!up)
                    {
                        if (aPhase > 0)
                        {
                            anObj.SetMovingPercent(1.0f, false);
                            moved = true;
                        }
                    }
                    else
                    {
#if !PEGGLE_IPHONE
                        mBoard.mFireButton.mIsDown = true;
#endif
                        if (aPeriod / 2 < aPhase)
                        {
                            anObj.SetMoveUpdateCnt(aPeriod - aPhase);
                        }
                        anObj.SetMovingPercent(0.5f, false);
                        moved = true;
                    }
                }
            }
            if (moved)
            {
                mBoard.mSoundMgr.AddSound(up ? Res.SOUND_FLIPPERUP : Res.SOUND_FLIPPERDOWN);
            }
        }

        /// <summary>Counts down every power-up of the current player and (de)activates each accordingly.</summary>
        public void ActivatePowerups()
        {
            if (mPowerupTypesActive[mCurPlayer] != 0)
            {
                mPowerupCount[mPowerupTypesActive[mCurPlayer]][mCurPlayer] = 1000;
            }
            for (int i = 0; i < 0xf; i++)
            {
                if (mPowerupCount[i][mCurPlayer] > 0)
                {
                    mPowerupCount[i][mCurPlayer]--;
                }
                ActivatePowerup(i, mPowerupCount[i][mCurPlayer] != 0);
            }
            if (mTripleScoreCount[mCurPlayer] != 0)
            {
                mTripleScore = true;
                mTripleScoreCount[mCurPlayer]--;
            }
            if (mHatCount[mCurPlayer] != 0)
            {
                mHatBall = true;
                mHatCount[mCurPlayer]--;
            }
        }

        public void SetWearHat(bool wear)
        {
            mWearHat = wear;
            Ball aBall = mBoard.mGun.mBall;
            if (aBall != null)
            {
                aBall.SetHat(wear, false);
                if (wear)
                {
                    aBall.SetAbsPos(aBall.mX, aBall.mY);
                }
            }
        }

        public void SetCharacters(int theChar1, int theChar2)
        {
            mCharacters[0] = theChar1;
            mCharacters[1] = theChar2;
            mBoard.mCharacterMgr.SetCurCharacter(mCharacters[mCurPlayer]);
        }
    }
}
