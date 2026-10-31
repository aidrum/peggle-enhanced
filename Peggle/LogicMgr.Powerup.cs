using System;
using System.Collections.Generic;

namespace Sexy
{
    public partial class LogicMgr
    {
        private static readonly string[] sPowerupNames =
        {
            "", "SuperGuide!", "Flippers!", "Multiball!", "Pyramid!", "SpaceBlast!", "SpookyBall!", "ZenShot!",
            "ShotExtender!", "FlowerPower!", "Nudge!", "TimeBomb!", "Fireball!", "", "Electrobolt"
        };

        /// <summary>
        /// The band from a peg up to the lightning orb (bucket): its two edges 7 units either side of
        /// the centre line, the perpendicular used to test which side a point is on, and the far end.
        /// Shared by getLightningHintPegs and DoChainLightning, which compute it identically.
        /// </summary>
        private struct LightningBand
        {
            public float mSrcX;

            public float mSrcY;

            public float mOrbY;

            public SexyVector2 mLeft;

            public SexyVector2 mRight;

            public SexyVector2 mPerp;

            public float mFarX;

            public float mFarY;

            public LightningBand(float theSrcX, float theSrcY, float theOrbX, float theOrbY)
            {
                mSrcX = theSrcX;
                mSrcY = theSrcY;
                mOrbY = theOrbY;
                SexyVector2 aDir = new SexyVector2(theOrbX - theSrcX, theOrbY - theSrcY);
                float aLen = aDir.Magnitude();
                mLeft = new SexyVector2(theOrbX - theSrcX - 7.0f, theOrbY - theSrcY + 0.0f);
                mRight = new SexyVector2(theOrbX - theSrcX + 7.0f, theOrbY - theSrcY + 0.0f);
                mPerp = aDir.Perp() * (100.0f / aLen);
                SexyVector2 aFar = aDir.Normalize() * aLen * 10.0f;
                mFarX = theSrcX + aFar.x;
                mFarY = theSrcY + aFar.y;
                mLeft = mLeft * 10.0f;
                mRight = mRight * 10.0f;
            }

            /// <summary>Whether (x, y)/(theOtherY) lies between the two perpendicular lines through the source and far end.</summary>
            public bool Between(float thePtX, float thePtY, float theTestY)
            {
                float aSlope = mPerp.y / mPerp.x;
                if (mSrcY > mOrbY)
                {
                    return !(aSlope * (thePtX - mSrcX) + mSrcY < theTestY) && aSlope * (thePtY - mFarX) + mFarY <= theTestY;
                }
                return !(aSlope * (thePtX - mSrcX) + mSrcY > theTestY) && aSlope * (thePtY - mFarX) + mFarY >= theTestY;
            }

            /// <summary>Signed cross-product parameter and perpendicular distance of theDelta from an edge.</summary>
            public static void EdgeDist(SexyVector2 theDelta, SexyVector2 theEdge, out float theT, out float theDist)
            {
                float aMagSq = theEdge.MagnitudeSquared();
                theT = (theDelta.y * theEdge.x - theDelta.x * theEdge.y) / aMagSq;
                theDist = Math.Abs(theT) * (float)Math.Sqrt(aMagSq);
            }
        }

        /// <summary>
        /// Pegs that lie in the lightning band from thePeg towards the orb, with a 0..1 weight for how
        /// close each is to the band's edges (used to show which pegs an Electrobolt would chain to).
        /// </summary>
        public void getLightningHintPegs(PhysObj thePeg, List<PhysObj> thePegs, List<float> theWeights)
        {
            float aMargin = 15.0f;
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            if (mBoard.GetBucket() == null)
            {
                throw new InvalidOperationException("getLightningHintPegs: no bucket");
            }
            LightningBand aBand = new LightningBand(aX, aY, (float)mLightningOrb.mX, mLightningOrb.mY);
            foreach (PhysObj anObj in mList394)
            {
                PegInfo aPeg = anObj.mPegInfo;
                if (aPeg.mHit || !anObj.IsVisible() ||
                    !(aPeg.mType == 1 || aPeg.mType == 3 || aPeg.mType == 4 || aPeg.mType == 2))
                {
                    continue;
                }
                float tx = anObj.GetXPos();
                float ty = anObj.GetYPos();
                Poly aBrick = null;
                int anIndex = 0;
                if (anObj.GetClass() == 6)
                {
                    aBrick = (Poly)anObj;
                    aBrick.EnsureLines();
                    anIndex = aBrick.mLines.Count - 1;
                }
                bool found = false;
                for (; !found && anIndex > -1; anIndex--)
                {
                    float px = tx;
                    float py = ty;
                    if (aBrick != null)
                    {
                        px = aBrick.mLines[anIndex].GetXPos();
                        py = aBrick.mLines[anIndex].GetYPos();
                    }
                    found = aBand.Between(px, py, ty);
                }
                if (!found)
                {
                    continue;
                }
                int aRadius;
                if (aBrick == null)
                {
                    aRadius = aPeg.mType == 1 ? 10 : 0xc;
                }
                else
                {
                    SexyVector2[] aCorners = new SexyVector2[4];
                    if (!((Brick)aBrick).GetCorners(aCorners))
                    {
                        throw new InvalidOperationException("getLightningHintPegs: gotCorners");
                    }
                    float aSize = new SexyVector2(aCorners[0].x - tx, aCorners[0].y - ty).Magnitude();
                    aRadius = aPeg.mType == 1 ? (int)(aSize * 0.55f) : (int)(aSize * 0.55f + 3.0f);
                }
                SexyVector2 aDelta = new SexyVector2(aX - tx, aY - ty);
                LightningBand.EdgeDist(aDelta, aBand.mLeft, out _, out float aDist1);
                LightningBand.EdgeDist(aDelta, aBand.mRight, out _, out float aDist2);
                float aLimit = aMargin + (float)aRadius;
                if (aDist1 > aLimit && aDist2 > aLimit)
                {
                    continue;
                }
                thePegs.Add(anObj);
                theWeights.Add(aDist1 < aDist2 ? 1.0f - aDist1 / aLimit : 1.0f - aDist2 / aLimit);
            }
        }

        private class PegDistInfo
        {
            public PhysObj mObj;

            public float mRadius;

            public float mT1;

            public float mT2;

            public float mDist1;

            public float mDist2;
        }

        /// <summary>
        /// Electrobolt: lightning arcs from the hit peg towards the orb, hitting pegs in a widening
        /// band (radius 0..50), fewer per ring as it widens, nearest first.
        /// </summary>
        public void DoChainLightning(Ball theBall, PhysObj thePeg)
        {
            float aMinRadius = 0.0f;
            float aMaxRadius = 50.0f;
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            mBoard.mSoundMgr.AddSound(Res.SOUND_POWERUP_LIGHTNING_HIT);
            int aTotalDelay = 0x1e;
            LightningBand aBand = new LightningBand(aX, aY, (float)mLightningOrb.mX, mLightningOrb.mY);
            List<PegDistInfo> aCandidates = new List<PegDistInfo>(mList394.Count);
            foreach (PhysObj anObj in mList394)
            {
                PegInfo aPeg = anObj.mPegInfo;
                if (aPeg.mHit || !anObj.IsVisible() ||
                    !(aPeg.mType == 1 || aPeg.mType == 3 || aPeg.mType == 4 || aPeg.mType == 2))
                {
                    continue;
                }
                float tx = anObj.GetXPos();
                float ty = anObj.GetYPos();
                Poly aBrick = null;
                int anIndex = 0;
                if (anObj.GetClass() == 6)
                {
                    aBrick = (Poly)anObj;
                    aBrick.EnsureLines();
                    anIndex = aBrick.mLines.Count - 1;
                }
                bool found = false;
                for (; !found && anIndex > -1; anIndex--)
                {
                    float px = tx;
                    float py = ty;
                    if (aBrick != null)
                    {
                        px = aBrick.mLines[anIndex].GetXPos();
                        py = aBrick.mLines[anIndex].GetYPos();
                    }
                    found = aBand.Between(px, py, ty);
                }
                if (!found)
                {
                    continue;
                }
                int aRadius;
                if (aBrick == null)
                {
                    aRadius = aPeg.mType == 1 ? 10 : 0xc;
                }
                else
                {
                    SexyVector2[] aCorners = new SexyVector2[4];
                    if (!((Brick)aBrick).GetCorners(aCorners))
                    {
                        throw new InvalidOperationException("DoChainLightning: gotCorners");
                    }
                    float aSize = new SexyVector2(aCorners[0].x - tx, aCorners[0].y - ty).Magnitude();
                    aRadius = aPeg.mType == 1 ? (int)(aSize * 0.55f) : (int)(aSize * 0.55f + 3.0f);
                }
                SexyVector2 aDelta = new SexyVector2(aX - tx, aY - ty);
                PegDistInfo anInfo = new PegDistInfo();
                LightningBand.EdgeDist(aDelta, aBand.mLeft, out anInfo.mT1, out anInfo.mDist1);
                LightningBand.EdgeDist(aDelta, aBand.mRight, out anInfo.mT2, out anInfo.mDist2);
                float aLimit = aMaxRadius + (float)aRadius;
                if (anInfo.mDist1 > aLimit && anInfo.mDist2 > aLimit)
                {
                    continue;
                }
                anInfo.mObj = anObj;
                anInfo.mRadius = (float)aRadius;
                aCandidates.Add(anInfo);
            }
            List<KeyValuePair<PhysObj, float>> aHits = new List<KeyValuePair<PhysObj, float>>();
            float aRing = aMinRadius;
            int aPrevMax = 0;
            while (aRing <= aMaxRadius)
            {
                int aMax = 100000;
                if (aMinRadius < aRing)
                {
                    double aFrac = 1.0 - (double)((aRing - aMinRadius) / (aMaxRadius - aMinRadius));
                    aMax = (int)Math.Ceiling(Math.Pow((double)(float)aFrac, 2.5) * 15.0 + 2.0);
                    if (aPrevMax <= aHits.Count)
                    {
                        break;
                    }
                }
                aPrevMax = aMax;
                for (int i = 0; i < aCandidates.Count;)
                {
                    PegDistInfo anInfo = aCandidates[i];
                    bool hit = false;
                    if (anInfo.mT1 <= 0.0f || !(anInfo.mT2 < 0.0f))
                    {
                        if (anInfo.mDist1 <= aRing + anInfo.mRadius || anInfo.mDist2 <= aRing + anInfo.mRadius)
                        {
                            hit = true;
                        }
                    }
                    else
                    {
                        hit = true;
                    }
                    if (hit && anInfo.mObj.GetClass() == 6)
                    {
                        Poly aBrick = (Poly)anInfo.mObj;
                        hit = false;
                        for (int j = 0; j < aBrick.mLines.Count; j++)
                        {
                            SexyVector2 aDelta = new SexyVector2(aX - aBrick.mLines[j].GetXPos(), aY - aBrick.mLines[j].GetYPos());
                            LightningBand.EdgeDist(aDelta, aBand.mLeft, out float aT1, out float aD1);
                            LightningBand.EdgeDist(aDelta, aBand.mRight, out float aT2, out float aD2);
                            if ((0.0f < aT1 && aT2 < 0.0f) || aD1 <= aRing + 5.0f || aD2 <= aRing + 5.0f)
                            {
                                hit = true;
                                break;
                            }
                        }
                    }
                    if (!hit)
                    {
                        i++;
                    }
                    else
                    {
                        float dx = aX - anInfo.mObj.GetXPos();
                        float dy = aY - anInfo.mObj.GetYPos();
                        aHits.Add(new KeyValuePair<PhysObj, float>(anInfo.mObj, dx * dx + dy * dy));
                        aCandidates.RemoveAt(i);
                    }
                }
                if (aPrevMax < aHits.Count)
                {
                    break;
                }
                float aDone = (aRing - aMinRadius) / (aMaxRadius - aMinRadius);
                if (aDone < 0.25f)
                {
                    aRing += 0.25f;
                }
                else if (aDone < 0.5f)
                {
                    aRing += 0.5f;
                }
                else
                {
                    aRing += 1.0f;
                }
            }
            if (mBoard.mShakeCount < 0x28)
            {
                mBoard.mShakeCount = 0x28;
            }
            if (aHits.Count == 0)
            {
                return;
            }
            // std::sort on HitPegInfo (by distance): not stable, but distances rarely tie.
            aHits.Sort((a, b) => a.Value.CompareTo(b.Value));
            PhysObj aPrev = thePeg;
            for (int i = 0; i < aHits.Count; i++)
            {
                PhysObj anObj = aHits[i].Key;
                int aDelay = (int)((uint)(aTotalDelay * i) / (uint)aHits.Count);
                mBoard.mSoundMgr.AddSoundPitchDelay(Res.SOUND_PEGSPARK, (float)(mInt188 + -0xc), aDelay, 1);
                PegHit(theBall, anObj, true);
                mBoard.mEffectMgr.AddLightningBolt(theBall, aPrev, anObj, aDelay, theBall.mFireball, true);
                aPrev = anObj;
            }
            mBoard.mEffectMgr.AddLightningBolt(theBall, aPrev, null, aTotalDelay, false, true);
            if (aHits.Count > 0xb)
            {
                DoStyleShot(theBall, 0x17);
            }
            Board aBoard = SexyUtil.GetBoard();
            if (aBoard != null)
            {
                PhysObj aBucket = aBoard.GetBucket();
                if (aBucket != null)
                {
                    mLightningOrb.mX = (int)aBucket.GetXPos();
                    mLightningOrb.mStateCount = 0;
                    mLightningOrb.mState = 4;
                }
            }
        }

        /// <summary>Lucky Spin: shows the slot machine (or picks a result directly in replays / while suspended).</summary>
        public void DoSlotMachine(Ball theBall, PhysObj thePeg)
        {
            if (!mMuteLoops)
            {
                thePeg.mPegInfo.mFlashCount = 0;
            }
            Ball aBall = theBall;
            if (theBall.mIsHatBall)
            {
                foreach (Ball anOther in mBoard.mBalls)
                {
                    if (anOther.mHatBall == theBall)
                    {
                        aBall = anOther;
                        break;
                    }
                }
            }
            bool suspended = mBoard.mSuspendCount > 0;
            if (suspended)
            {
                SetSlotMachineResult(SexyUtil.Rand() % 4);
            }
            if (mMuteLoops || suspended)
            {
                int aResult = GetSlotMachineResult();
                if (aResult != -1)
                {
                    DoSlotMachineResult(aResult);
                    FinishSlotMachine(aBall, thePeg);
                }
            }
            else
            {
                mBoard.mSoundMgr.AddSound(Res.SOUND_POWERUP_07_LUCKYSPIN);
                mBoard.DoSlotMachineDialog(aBall, thePeg);
            }
        }

        public void SetSlotMachineResult(int theResult)
        {
            if (mInt50 == 1)
            {
                mInt1a8 = theResult;
            }
            else
            {
                mInt1ac = theResult;
            }
        }

        public int GetSlotMachineResult()
        {
            return mInt50 == 1 ? mInt1a8 : mInt1ac;
        }

        /// <summary>Applies a slot result: 0 = power-up, 1 = magic hat, 2 = score tripler, 3 = extra ball (shown as hat).</summary>
        public void DoSlotMachineResult(int theResult)
        {
            int aResult = theResult;
            if (theResult == 3)
            {
                aResult = 1;
            }
            int aSound = -1;
            switch (aResult)
            {
            case 0:
                if (!mMuteLoops)
                {
                    aSound = Res.SOUND_TING;
                }
                break;
            case 1:
                aSound = Res.SOUND_FREEBALL;
                mHatCount[mCurPlayer]++;
                if (mHatBall)
                {
                    mHatCount[mCurPlayer]++;
                }
                mHatBall = true;
                CalcCornerDisplay();
                break;
            case 2:
                aSound = Res.SOUND_AAH;
                mTripleScoreCount[mCurPlayer]++;
                if (mTripleScore)
                {
                    mTripleScoreCount[mCurPlayer]++;
                }
                mTripleScore = true;
                CalcCornerDisplay();
                break;
            case 3:
                aSound = Res.SOUND_EXTRABALL;
                break;
            }
            if (aSound != -1)
            {
                mBoard.mSoundMgr.AddSound(aSound);
            }
            SetSlotMachineResult(aResult);
        }

        public void FinishSlotMachine(Ball theBall, PhysObj thePeg)
        {
            string[] aNames =
            {
                SexyAppBase.Loc("Random Powerup"), SexyAppBase.Loc("Magic Hat!"), SexyAppBase.Loc("Score Tripler"), SexyAppBase.Loc("Extra Ball")
            };
            int aResult = GetSlotMachineResult();
            if (aResult == 1)
            {
                if (theBall.mHatBall == null)
                {
                    theBall.SetHat(true, true);
                }
            }
            else if (aResult == 3)
            {
                IncNumBalls(1, 0, false);
                mStats[mCurPlayer].mBucketFreeBalls++;
            }
            if (aResult < 1 || aResult > 2)
            {
                if (aResult == 3)
                {
                    AddStandardText(aNames[3], thePeg.GetXPos(), thePeg.GetYPos(), -1);
                }
                else
                {
                    DoPowerup(theBall, thePeg, GetSlotMachinePowerup(), true);
                }
            }
            else
            {
                int aCount = aResult == 2 ? mTripleScoreCount[mCurPlayer] : mHatCount[mCurPlayer];
                string aText = MakeNextTurnString(aNames[aResult], aCount + 1, false);
                AddStandardText(aText, thePeg.GetXPos(), thePeg.GetYPos(), -1);
            }
        }

        public void DoPowerup(Ball theBall, PhysObj thePeg)
        {
            DoPowerup(theBall, thePeg, 0, false);
        }

        /// <summary>A green peg (or Lucky Spin) grants theType (0 = the current master's power-up).</summary>
        public void DoPowerup(Ball theBall, PhysObj thePeg, int theType, bool fromSlotMachine)
        {
            SoundMgr aSoundMgr = mBoard.mSoundMgr;
            int aType = theType;
            if (theType == 0)
            {
                aType = mPowerupTypes[mCurPlayer];
            }
            bool addTurns = true;
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            int aFadeMusic = 0;
            int aSound = -1;
            string aText = "";
            if (fromSlotMachine)
            {
                if (theType == 3)
                {
                    aText = SexyAppBase.Loc("Multiball!");
                }
                else if (theType == 5)
                {
                    aText = SexyAppBase.Loc("SpaceBlast!");
                }
                else if (theType == 9)
                {
                    aText = SexyAppBase.Loc("FlowerPower!");
                }
                else
                {
                    aText = sPowerupNames[theType];
                }
            }
            int aGreenHits = mInt50;
            int aTurns = 2;
            int aHave = mPowerupCount[aType][mCurPlayer];
            int aNew = aHave == 0 ? 1 : 0;
            mBoard.mCharacterMgr.DoPowerupBlink(aType == 3 || aType == 6 ? 4 : 2);
            switch (aType)
            {
            case 1:
                aTurns = aNew + 3;
                aText = MakeNextTurnString(SexyAppBase.Loc("Super Guide"), aHave + aTurns - 1, true);
                aSound = Res.SOUND_POWERUP_00_GUIDE;
                break;
            case 2:
                aTurns = 3;
                aText = MakeNextTurnString(SexyAppBase.Loc("Flippers"), aHave + aTurns, false);
                aSound = Res.SOUND_POWERUP_04_FLIPPERS;
                break;
            case 3:
                DoMultiball(theBall, thePeg);
                addTurns = false;
                break;
            case 4:
                aTurns = 5;
                aText = MakeNextTurnString(SexyAppBase.Loc("Pyramid"), aHave + aTurns, false);
                if (!mDimLowObjects)
                {
                    mBoard.mEffectMgr.AddPyramid(aX, aY, aHave < 1);
                }
                aSound = Res.SOUND_POWERUP_02_PYRAMID;
                aFadeMusic = 0x46;
                break;
            case 5:
                DoExploder(theBall, thePeg);
                addTurns = false;
                break;
            case 6:
                {
                    bool aTrail = false;
                    if (!theBall.mFlag11C)
                    {
                        aTrail = !theBall.mFlagFE;
                    }
                    mBoard.mEffectMgr.AddSpookyBall(aX, aY, aTrail);
                    theBall.mFlag11C = true;
                    ActivateFreeBallCover(true);
                    aText = SexyAppBase.Loc("SpookyBall!");
                    aTurns = 1;
                    thePeg.mPegInfo.mFlashCount = 0;
                    if (mPowerupCount[6][mCurPlayer] < 1)
                    {
                        aSound = !theBall.mFlagFE ? Res.SOUND_POWERUP_05_SPOOKYBALL : Res.SOUND_SPOOKYBALL3;
                    }
                    else
                    {
                        aSound = Res.SOUND_SPOOKYBALL2;
                    }
                    aFadeMusic = 100;
                    break;
                }
            case 7:
                aTurns = aNew + 1;
                aText = MakeNextTurnString(SexyAppBase.Loc("ZenBall"), aHave + aTurns - 1, true);
                aSound = Res.SOUND_POWERUP_09_ZEN;
                aFadeMusic = 100;
                break;
            case 8:
                aSoundMgr.AddSound(Res.SOUND_POWERUP);
                mFlagE4 = true;
                addTurns = false;
                break;
            case 9:
                DoFeverTron(theBall, thePeg);
                addTurns = false;
                break;
            case 10:
                aTurns = 1;
                break;
            case 11:
                aTurns = 1;
                break;
            case 0xc:
                aTurns = aNew + 1;
                aText = MakeNextTurnString(SexyAppBase.Loc("Fireball"), aHave + aTurns - 1, true);
                aSound = Res.SOUND_POWERUP_08_FIREBALL;
                aFadeMusic = 200;
                break;
            case 0xd:
                DoSlotMachine(theBall, thePeg);
                addTurns = false;
                break;
            case 0xe:
                aTurns = aNew + 1;
                aText = MakeNextTurnString(SexyAppBase.Loc("Electrobolt"), aHave + aTurns - 1, true);
                aSound = Res.SOUND_POWERUP_10_LIGHTNING;
                break;
            }
            bool styleShot = false;
            if (aGreenHits == 2 && !fromSlotMachine)
            {
                DoStyleShot(theBall, 10);
                styleShot = true;
            }
            if (aText.Length != 0 && (!mDimLowObjects || fromSlotMachine))
            {
                if (styleShot)
                {
                    aY = aY - 40.0f;
                }
                FloatingText aFloat = AddStandardText(aText, aX, aY, -1);
                AvoidSides(aFloat);
                aFloat.mColor = 0x88ff88;
                aFloat.mUpdateCnt = -0x23;
                aFloat.mVelY = -0.5f;
                aFloat.mFadeOut = 10;
                aFloat.mDuration = 0x96;
            }
            if (aSound > -1)
            {
                aSoundMgr.AddSound(aSound);
                if (aFadeMusic != 0)
                {
                    aSoundMgr.FadeMusic(aFadeMusic);
                }
            }
            if (addTurns)
            {
                mPowerupCount[aType][mCurPlayer] += aTurns;
                ActivatePowerup(aType, true);
            }
        }

        /// <summary>Lucky Spin's "Random Powerup": one of the powers of the masters unlocked so far.</summary>
        public int GetSlotMachinePowerup()
        {
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            int aPeggleChars = Math.Min(aPlayer.mMaxStage, 10);
            int aNightsChars = Math.Min(aPlayer.mNightsMaxStage + -0xb, SexyUtil.gCharCount);
            if (aNightsChars == 8)
            {
                aNightsChars = aNightsChars + 1;
            }
            int aNumChars = Math.Max(aPeggleChars, aNightsChars);
            List<int> aPowerups = new List<int>(aNumChars);
            for (int i = 0; i < aNumChars; i++)
            {
                CharacterInfo anInfo = mBoard.mCharacterMgr.GetCharacterInfo(i);
                if (anInfo != null && anInfo.mPowerup != 0xd)
                {
                    aPowerups.Add(anInfo.mPowerup);
                }
            }
            if (aPowerups.Count == 0)
            {
                aPowerups.Add(1);
            }
            int aRand = mInt50 == 1 ? mInt30 : mInt2c;
            return aPowerups[aRand % aPowerups.Count];
        }

        public void AddExtremeFeverEffect()
        {
            mBoard.mEffectMgr.EraseAllOfType(0x1f);
            mBoard.mEffectMgr.EraseAllOfType(0x29);
            bool ultra = mGoalPegs.Count == 0 && mNonGoalPegs.Count == 0;
            bool zoomed = mBoard.mZoomActive; // SexyAppBase::Is3DAccelerated() is always true here
            mBoard.mEffectMgr.AddExtremeFever((float)0x145, (float)(zoomed ? 0x32 : 100), ultra, 0);
        }

        /// <summary>Extreme Fever: the last orange peg was hit.</summary>
        public void DoFever()
        {
            if (mDimLowObjects)
            {
                return;
            }
            if (mSlowMoSound != null)
            {
                mBoard.mSoundMgr.AddFadeSound(mSlowMoSound);
                mSlowMoSound = null;
            }
            if (mSighSound != null)
            {
                mBoard.mSoundMgr.AddFadeSound(mSighSound);
                mSighSound = null;
            }
            if (!mMuteLoops && mDuelMode)
            {
                mBoard.mApp.mTwoPlayerFevers[mCurPlayer]++;
            }
            mBoard.mEffectMgr.EraseAllOfType(0x18);
            mInt1c0 = 0;
            mInt1c4 = 0;
            mInt1c8 = mStateCount;
            mBoard.mInterfaceMgr.DoFever();
            mDimLowObjects = true;
            AddExtremeFeverEffect();
            if (mFeverPending)
            {
                mBoard.SetSlowMo(true, 100);
                mBoard.mZoomIn = true;
                mBoard.mSlowMoTargetRate = 500;
                mBoard.mSlowMoRateStep = 1;
                mBoard.mSlowMoHold = 10000;
                mBoard.mFeverCam = true;
            }
            mBoard.mSoundMgr.CancelFade();
            mBoard.mApp.PlayMusic(0, false);
            foreach (PhysObj anObj in mBoard.mPhysObjs)
            {
                if (anObj.mId == "fever")
                {
                    anObj.SetActive(true);
                    anObj.SetMovingPercent(0.5f, false);
                }
                else if (anObj.mId == "freeball" || anObj.mId == "bumperhole" || anObj.mId == "freeballcover")
                {
                    anObj.mCollidable = false;
                    Mover aMover = anObj.mMover;
                    if (aMover == null || anObj.mFlag29)
                    {
                        anObj.SetActive(false);
                    }
                    else
                    {
                        aMover.mType = -0xb;
                        aMover.mRadius1 = 100;
                        aMover.mPhase = 0;
                        aMover.mPeriod = 0x14;
                        aMover.mAnchorX = aMover.mX;
                        aMover.mAnchorY = aMover.mY;
                        anObj.mMoveUpdateCnt = 1;
                        anObj.SetMovingPercent(0.5f, false);
                    }
                }
            }
        }
    }
}
