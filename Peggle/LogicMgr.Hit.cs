using System;
using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    public partial class LogicMgr
    {
        /// <summary>Port debug aid (not in the original): SEXY_DEBUG_FEVER=1 makes the first peg hit of a level start Extreme Fever.</summary>
        public static readonly bool gDebugFever = Environment.GetEnvironmentVariable("SEXY_DEBUG_FEVER") == "1";

        private const int KILLBALL_TIME1 = 1500;

        private const int KILLBALL_TIME2 = 1000;

        private const int KILLBALL_TIME3 = 500;

        /// <summary>
        /// A ball touched a peg: scoring and its floating text, goal-peg bookkeeping and style shots,
        /// level-cleared fever, and the green peg's power-up. byPowerup is set for pegs hit by a power-up
        /// (space blast, flower power, lightning) rather than by a ball bounce.
        /// </summary>
        public void PegHit(Ball theBall, PhysObj thePeg, bool byPowerup)
        {
            PegInfo aPeg = thePeg.mPegInfo;
            if (!byPowerup && theBall.mBounceCount == 0 && aPeg.mHitCount < 0x14 && thePeg.mShapeType == PhysObj.SHAPE_POLY)
            {
                if (mStateCount - theBall.mValue114 < 0x14)
                {
                    float aSpeedSq = theBall.mVelX * theBall.mVelX + theBall.mVelY * theBall.mVelY;
                    if (aSpeedSq > 2.5f)
                    {
                        theBall.mCount10C = 0x14;
                    }
                }
                theBall.mValue114 = mStateCount;
            }
            aPeg.IncCrumble();
            if (aPeg.mHit)
            {
                if (aPeg.mHitCount < 0x14)
                {
                    theBall.mImpactSpeed = 0;
                }
                return;
            }
            theBall.mImpactSpeed = 0;
            if (theBall.mFireball && !theBall.mIsHatBall)
            {
                thePeg.SetActive(false);
            }
            mInt17c++;
            if (!byPowerup)
            {
                if (theBall.mId == 0)
                {
                    if (!mFlagA0 && mFlagA1)
                    {
                        float aTapX = (float)(mInt98 - mBoard.mBoardRect.mX);
                        float aTapY = (float)(mInt9c - mBoard.mBoardRect.mY);
                        float dx = thePeg.GetXPos() - aTapX;
                        float dy = thePeg.GetYPos() - aTapY;
                        if ((float)Math.Sqrt(dx * dx + dy * dy) < 60.0f)
                        {
                            mFlagA2 = true;
                        }
                    }
                    CheckPegHitSkillShot(theBall, thePeg);
                }
                if (theBall.mFlagFC)
                {
                    theBall.mFlagFC = false;
                    mInt48++;
                    if (mInt48 == 4)
                    {
                        DoStyleShot(theBall, 1);
                    }
                }
            }
            if (theBall.mCount10C > 0 && !byPowerup)
            {
                theBall.mValue110++;
                if (theBall.mValue110 == 0xc)
                {
                    DoStyleShot(theBall, 2);
                }
                else if (theBall.mValue110 == 0x10 && mInt2c % 100 < 0x14)
                {
                    DoJimmy(false);
                }
                else if (theBall.mValue110 == 0x14)
                {
                    DoJimmy(false);
                }
            }
            if (KILLBALL_TIME2 <= mInt38)
            {
                mInt38 = KILLBALL_TIME2;
            }
            if (gDebugFever && !mDimLowObjects)
            {
                // Port debug aid: every other orange peg counts as already hit, so this hit starts Extreme Fever.
                foreach (PhysObj aGoal in mGoalPegs)
                {
                    if (aGoal != thePeg)
                    {
                        aGoal.mPegInfo.SetHit(true);
                    }
                }
                mGoalPegs.RemoveWhere(aGoal => aGoal != thePeg);
            }
            mInt170 = CalcScoreMult(mGoalPegs.Count);
            aPeg.SetHit(true);
            aPeg.DoFlash(0);
            if (thePeg.mId.Length != 0)
            {
                for (int i = 0; i < mBoard.mEmitters.Count; i++)
                {
                    Emitter anEmitter = mBoard.mEmitters[i];
                    if (anEmitter != null && thePeg.mId == anEmitter.mName)
                    {
                        anEmitter.Trigger();
                    }
                }
            }
            if (aPeg.mType == 2)
            {
                mFlag18 = true;
                mGoalPegs.Remove(thePeg);
                int aNewMult = CalcScoreMult(mGoalPegs.Count);
                if (aNewMult != mInt170 && aNewMult < 100)
                {
                    mBoard.mInterfaceMgr.DoFeverMultBlink();
                }
                mInt4c++;
                int aTotal = mGoalPegs.Count + mInt4c;
                int aNeeded = 1000;
                if (aTotal >= 0x1f)
                {
                    aNeeded = 0xf;
                }
                else if (aTotal >= 0x15)
                {
                    aNeeded = 10;
                }
                else if (aTotal >= 0x10)
                {
                    aNeeded = 9;
                }
                else if (aTotal >= 0xb)
                {
                    aNeeded = 8;
                }
                else if (aTotal >= 7)
                {
                    aNeeded = aTotal + -2;
                }
                else if (aTotal > 4)
                {
                    aNeeded = aTotal + -1;
                }
                if (mInt4c == aNeeded)
                {
                    DoStyleShot(theBall, 3);
                }
                if (mGoalPegs.Count == 0xf || mGoalPegs.Count == 5)
                {
                    mBoard.mApp.SetMusicSpeed(0.0f);
                }
                int aPrevIntensity = mInt174;
                mInt174 = CalcMusicIntensity(mGoalPegs.Count);
                if (aPrevIntensity < mInt174)
                {
                    mBoard.mApp.SetMusicIntensityIncreasePending(mInt174);
                }
            }
            else
            {
                mNonGoalPegs.Remove(thePeg);
            }
            if (mGoalPegs.Count == 0 && mNonGoalPegs.Count == 0 && !mDuelMode)
            {
                mLevelCleared = true;
                bool checkCoolClear = true;
                if (!mDimLowObjects && mPegHits.Count < 2)
                {
                    checkCoolClear = false;
                }
                if (checkCoolClear)
                {
                    if (mPegHits.Count > 1 && mNonGoalPegs.Count + mGoalPegs.Count == 0)
                    {
                        PegHitInfo aLast = mPegHits[mPegHits.Count - 1];
                        if (aLast.mInt04 != 2 || mPegHits.Count >= 2)
                        {
                            for (int i = 0; i < mPegHits.Count; i++)
                            {
                                if (mPegHits[i].mInt04 == 2)
                                {
                                    DoStyleShot(theBall, 4);
                                    break;
                                }
                            }
                        }
                    }
                    if (mDimLowObjects)
                    {
                        AddExtremeFeverEffect();
                    }
                }
            }
            int aBase;
            int aColor;
            switch (aPeg.mType)
            {
            case 1:
                aBase = 10;
                aColor = 0xccccff;
                break;
            case 2:
                aBase = 100;
                aColor = 0xffaa44;
                break;
            case 3:
                aBase = 500;
                aColor = 0xff88ff;
                break;
            case 4:
                aBase = 10;
                aColor = 0x88ff88;
                break;
            default:
                aBase = 10;
                aColor = 0xffff00;
                break;
            }
            int aMult = mInt170;
            if (aMult > 99 && aBase > 499)
            {
                aBase = 100;
            }
            int aPoints = aMult * aBase;
            mInt164 += aPoints;
            IncShotScore(aPoints * mInt188 + mInt164);
            mList39c.Add(thePeg);
            mInt188++;
            SexyVector2 aCenter = thePeg.GetCenter();
            float aTextY = aCenter.y;
            if (thePeg.mShapeType == PhysObj.SHAPE_CIRCLE)
            {
                aTextY = aCenter.y + ((Ball)thePeg).mRadius;
            }
            if (aPeg.mType == 2)
            {
                int aLeft = GetGoalPegsLeft();
                if (aLeft < 4 && aLeft > -1)
                {
                    mLevelStats.mCounts[aLeft] = mBallsLeft[0];
                }
            }
            FloatingText aText = AddStandardText(CFormat.StrFormat("%d", aPoints), aCenter.x, aTextY, 6);
            if (!mDimLowObjects)
            {
                aText.mScreenAscent = true;
            }
            else
            {
                aText.AddScale(0.2f, 0);
                aText.AddScale(1.6f, 10);
                aText.AddScale(1.3f, 0xc);
                mBoard.mEffectMgr.AddFeverPointsBlast(aCenter.x, aTextY);
            }
            aText.mColor = aColor;
            aText.mDuration = 0x32;
            aText.mJustifyY = -1;
            aText.mVelY = 0;
            aText.mFadeOut = 10;
            mInt178 = aText.mId;
            if (aPeg.mType == 3)
            {
                double aY = (double)aCenter.y + (double)LOGICMGR_PEG_HIT_TEXT_OFFSET_Y * 600.0 / (double)SexyUtil.gPhysicalDisplayHeight;
                FloatingText aBoost = AddStandardText(SexyAppBase.Loc("Point Boost!"), aCenter.x, (float)aY, -1);
                AvoidSides(aBoost);
                aBoost.mColor = 0xff88ff;
                aBoost.mVelY = -0.5f;
                aBoost.mUpdateCnt = -0x23;
                aBoost.mFadeOut = 10;
                mBoard.mSoundMgr.AddSound(Res.SOUND_POINTBOOST);
            }
            if (!byPowerup)
            {
                int aSound = Res.SOUND_PEGHIT;
                if (mShotScore > 24999)
                {
                    aSound = Res.SOUND_PEGHIT2;
                }
                if (mShotScore < 75000)
                {
                    int aPitch = mInt188 + -0xc;
                    if (aPitch > -1)
                    {
                        aSound = Res.SOUND_PEGHIT_HIGH;
                        aPitch = Math.Min(2, mInt188 - 0x18);
                    }
                    mBoard.mSoundMgr.AddSoundPitch(aSound, (float)aPitch);
                }
                else
                {
                    mBoard.mSoundMgr.AddSound(Res.SOUND_PEGHIT3);
                }
            }
            if (ClearedLevel(true) && !mDimLowObjects)
            {
                mBoard.mEffectMgr.AddFeverBlast(aCenter.x, aCenter.y);
                mBoard.mSoundMgr.AddSound(Res.SOUND_CYMBAL);
                for (int i = 0; i < 8; i++)
                {
                    float anAngle = SexyUtil.DegToRad((float)(i * 0x168 >> 3));
                    float aVX = (float)Math.Cos(anAngle) * 4.0f;
                    float aVY = 4.0f * (float)Math.Sin(anAngle);
                    Effect aSpark = mBoard.mEffectMgr.AddEffect(4, aCenter.x + aVX, aCenter.y + aVY, 2, false);
                    aSpark.mDuration = 1000;
                    aSpark.mVelX = aVX * ((float)(SexyUtil.Rand() % 10) / 20.0f + 0.75f);
                    aSpark.mVelY = aVY * (0.75f + (float)(SexyUtil.Rand() % 10) / 20.0f);
                    aSpark.mGravity = Ball.mGravity;
                    aSpark.mFlags = i;
                }
                if (mBoard.mBalls.Count == 2)
                {
                    DoStyleShot(theBall, 5);
                }
                else if (theBall.mValue138 > 0)
                {
                    DoStyleShot(theBall, 0xb);
                }
                DoFever();
            }
            if (mGoalPegs.Count == 0 && mNonGoalPegs.Count == 0 && !mDuelMode)
            {
                foreach (PhysObj anObj in mBoard.mPhysObjs)
                {
                    if (anObj.mShapeType == PhysObj.SHAPE_HOLE && anObj.mId == "fever")
                    {
                        ((Hole)anObj).mValue = 100000;
                    }
                }
            }
            if (aPeg.mType == 4)
            {
                mFlag159 = true;
                mFlag1C = true;
                mInt50++;
                DoPowerup(theBall, thePeg);
            }
            if (theBall.mElectrified)
            {
                theBall.SetElectrified(false, 0);
                DoChainLightning(theBall, thePeg);
            }
        }

        /// <summary>The time-bomb ball blows up: hits every unhit peg within 80 units, then disappears.</summary>
        public void DoTimeBomb()
        {
            if (mBoard.mBalls.Count == 0)
            {
                return;
            }
            Ball aBall = mBoard.mBalls[0];
            float aX = aBall.mX;
            float aY = aBall.mY;
            Effect anEffect = mBoard.mEffectMgr.AddEffect(1, aX, aY, 2, false);
            anEffect.mValue = 0x50;
            int aRadiusSq = 0x1900;
            int aDelay = 0;
            foreach (PhysObj anObj in new List<PhysObj>(mList394))
            {
                if (!anObj.mPegInfo.mHit && anObj.IsVisible())
                {
                    float dx = anObj.GetXPos() - aX;
                    float dy = anObj.GetYPos() - aY;
                    if (!(dx * dx + dy * dy > (float)aRadiusSq))
                    {
                        aDelay += 2;
                        mBoard.mSoundMgr.AddSoundPitchDelay(Res.SOUND_PEGHIT, (float)(mInt188 + -0xc), aDelay);
                        PegHit(aBall, anObj, true);
                    }
                }
            }
            mBoard.RemoveObj(aBall);
            mBoard.mSoundMgr.AddSound(Res.SOUND_EXPLODE);
        }

        /// <summary>Flower Power: lights up the nearest fifth (at least two) of the remaining orange pegs.</summary>
        public void DoFeverTron(Ball theBall, PhysObj thePeg)
        {
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            List<KeyValuePair<float, PhysObj>> aByDist = new List<KeyValuePair<float, PhysObj>>();
            foreach (PhysObj anObj in mList394)
            {
                if (!anObj.mPegInfo.mHit && anObj.IsVisible() && anObj.mPegInfo.mType == 2)
                {
                    float dx = aX - anObj.GetXPos();
                    float dy = aY - anObj.GetYPos();
                    aByDist.Add(new KeyValuePair<float, PhysObj>(dx * dx + dy * dy, anObj));
                }
            }
            // std::multimap keeps equal keys in insertion order: a stable sort.
            List<KeyValuePair<float, PhysObj>> aSorted = new List<KeyValuePair<float, PhysObj>>();
            foreach (KeyValuePair<float, PhysObj> aPair in aByDist)
            {
                int anIndex = aSorted.Count;
                while (anIndex > 0 && aSorted[anIndex - 1].Key > aPair.Key)
                {
                    anIndex--;
                }
                aSorted.Insert(anIndex, aPair);
            }
            int aCount = 0;
            int aDelay = 0;
            int aWanted = (int)Math.Ceiling((float)mGoalPegs.Count * 0.2f);
            if (aWanted < 2 && mGoalPegs.Count > 1)
            {
                aWanted = 2;
            }
            for (int i = 0; i < aSorted.Count && aCount < aWanted; i++)
            {
                PhysObj anObj = aSorted[i].Value;
                aDelay += 2;
                mBoard.mSoundMgr.AddSoundPitchDelay(Res.SOUND_PEGHIT, (float)(mInt188 + -0xc), aDelay);
                PegHit(theBall, anObj, true);
                anObj.mPegInfo.mShowFlower = true;
                mBoard.mEffectMgr.AddFlowerSparkles(anObj.GetXPos(), anObj.GetYPos());
                aCount++;
            }
            mBoard.mSoundMgr.AddSound(Res.SOUND_POWERUP_06_FLOWERPOWER);
        }
    }
}
