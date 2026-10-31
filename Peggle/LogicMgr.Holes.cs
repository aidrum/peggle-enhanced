using System;
using System.Collections.Generic;

namespace Sexy
{
    public partial class LogicMgr
    {
        public static int mSpookyCollisions;

        public static List<PegInfo> mSpookyIgnoreList = new List<PegInfo>();

        /// <summary>Jimmy Lightning pops in to comment on a great shot (once per shot unless forced).</summary>
        public void DoJimmy(bool force)
        {
            if (mBoard.mLogicMgr.mDimLowObjects || (mFlag156 && !force))
            {
                return;
            }
            if (!(mBoard.mZoom <= 0.0f))
            {
                return;
            }
            float aMinX = 100000.0f;
            float aMaxX = -100000.0f;
            foreach (Ball aBall in mBoard.mBalls)
            {
                if (aBall.mX < aMinX)
                {
                    aMinX = aBall.mX;
                }
                if (aBall.mX > aMaxX)
                {
                    aMaxX = aBall.mX;
                }
            }
            if ((200.0f <= aMinX || aMaxX <= 400.0f) && mBoard.mEffectMgr.GetEffectByType(0x21) == null)
            {
                mBoard.mEffectMgr.AddJimmy(aMinX < (float)(mBoard.mBoardRect.mWidth / 2), mInt30, 0x1e);
                if (!force)
                {
                    mFlag156 = true;
                }
            }
        }

        /// <summary>Records the peg hit and checks the long-shot style shots between the first hits.</summary>
        public void CheckPegHitSkillShot(Ball theBall, PhysObj thePeg)
        {
            PegHitInfo aHit = new PegHitInfo();
            mPegHits.Add(aHit);
            aHit.mInt00 = mStateCount;
            aHit.mX = theBall.mX;
            aHit.mY = theBall.mY;
            aHit.mInt04 = thePeg.mPegInfo.mType;
            if (mDimLowObjects && aHit.mInt04 == 4 && mPowerupTypes[mCurPlayer] == 3)
            {
                return;
            }
            if (mPegHits.Count == 1)
            {
                PegHitInfo aFirst = mPegHits[0];
                if (aFirst.mInt04 != 1 && theBall.mValue140 > 0 && mStateCount - theBall.mValue140 > 0x28)
                {
                    DoStyleShot(theBall, 0x16);
                }
            }
            else if (mPegHits.Count == 2)
            {
                PegHitInfo a = mPegHits[0];
                PegHitInfo b = mPegHits[1];
                if (a.mInt04 != 1 && b.mInt04 != 1)
                {
                    CheckLongShot(theBall, a, b);
                }
            }
            else if (mPegHits.Count == 3)
            {
                PegHitInfo a = mPegHits[0];
                PegHitInfo b = mPegHits[1];
                PegHitInfo c = mPegHits[2];
                if (a.mInt04 == 1 || b.mInt04 != 1 || c.mInt04 == 1)
                {
                    if (a.mInt04 != 1 && b.mInt04 != 1 && c.mInt04 != 1)
                    {
                        float aDistAB = DistSq(a.mX, a.mY, b.mX, b.mY);
                        float aDistCB = DistSq(c.mX, c.mY, b.mX, b.mY);
                        if (!(aDistAB <= (float)40000) && (float)40000 < aDistCB)
                        {
                            DoStyleShot(theBall, 0x15);
                        }
                    }
                }
                else if (c.mInt00 - b.mInt00 < 10)
                {
                    CheckLongShot(theBall, a, b);
                }
            }
        }

        private static float DistSq(float x1, float y1, float x2, float y2)
        {
            return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
        }

        private void CheckLongShot(Ball theBall, PegHitInfo a, PegHitInfo b)
        {
            float aDist = DistSq(a.mX, a.mY, b.mX, b.mY);
            if (aDist < (float)0x31704)
            {
                if ((float)40000 < aDist)
                {
                    DoStyleShot(theBall, 0);
                }
            }
            else
            {
                DoStyleShot(theBall, 0xf);
            }
        }

        /// <summary>The ball went into the free-ball bucket: a free ball (or "Total Miss" in duels) and bucket style shots.</summary>
        public void FreeBallHit(Hole theHole, Ball theBall)
        {
            int aWindow = 0x32;
            FloatingText aText = null;
            float aX = theHole.GetXPos();
            float aY = theHole.GetYPos() - 10.0f;
            float aStyleY = aY + -90.0f;
            PhysObj aRefObj = theHole;
            if (theBall.mValue13C < 1)
            {
                aWindow = 0x96;
            }
            if (mBoard.mBoardRect.mHeight - theBall.mValue148 > 0x7d)
            {
                aWindow = (int)((float)aWindow * 0.4f);
            }
            bool noFreeBalls = false;
            if (mBoard.mTrophyInfo != null)
            {
                noFreeBalls = mBoard.mTrophyInfo.mNoFreeballs;
            }
            if (!noFreeBalls && mInt188 == 0 && mDuelMode)
            {
                aY = aY + -20.0f;
                aText = AddStandardText(SexyAppBase.Loc("Total Miss\nNo Free Ball"), aX, aY, -1);
                aText.mDuration = 200;
                aRefObj = null;
            }
            else
            {
                int aGlowDelay = 10;
                if (mBoard.mEffectMgr.EraseAllOfType(0x1c) != 0)
                {
                    aGlowDelay = 0;
                    aRefObj = null;
                }
                mBoard.mEffectMgr.AddCatcherGlow(aGlowDelay, theBall.mFlag124);
                if (mInt44 != 0)
                {
                    mBoard.mFloatingTextMgr.EraseText(mInt44);
                    mInt44 = 0;
                }
                SoundMgr aSoundMgr = mBoard.mSoundMgr;
                bool skillful = false;
                bool showText = true;
                if (mInt188 == 1 && theBall.mId == 0)
                {
                    skillful = true;
                }
                else if (mPegHits.Count == 2 && theBall.mId == 0)
                {
                    PegHitInfo a = mPegHits[0];
                    PegHitInfo b = mPegHits[1];
                    if (b.mInt00 - a.mInt00 < 0x14 && mStateCount - b.mInt00 < 0x32)
                    {
                        skillful = true;
                    }
                }
                bool streak = false;
                int aStreak = Math.Abs(mInt374);
                if (skillful || (theBall.mId == 0 && mInt188 > 0))
                {
                    mInt374 = aStreak + 1;
                    streak = true;
                }
                if (streak && mInt374 % 10 == 0)
                {
                    DoStyleShot(aX, aStyleY, 0xe, aRefObj);
                    showText = false;
                }
                else if (theBall.mHatBall != null && mInt188 > 0)
                {
                    DoStyleShot(aX, aStyleY, 6, aRefObj);
                    showText = false;
                }
                else if (theBall.mFlagFD && mPowerupCount[12][mCurPlayer] > 0)
                {
                    DoStyleShot(aX, aStyleY, 7, aRefObj);
                    showText = false;
                }
                else if (mFlag19)
                {
                    DoStyleShot(aX, aStyleY, 8, aRefObj);
                    showText = false;
                }
                else if (theBall.mValue138 > 0 && mInt188 > 0 && mStateCount - theBall.mValue138 > aWindow)
                {
                    DoStyleShot(aX, aStyleY, 0xc, aRefObj);
                    showText = false;
                }
                else if (streak && mInt374 == 5)
                {
                    DoStyleShot(aX, aStyleY, 0xd, aRefObj);
                    showText = false;
                }
                else if (skillful)
                {
                    DoStyleShot(aX, aStyleY, 9, aRefObj);
                    showText = false;
                }
                if (mDuelMode)
                {
                    DoStyleShot(aX, aStyleY, 0x11, aRefObj);
                    showText = false;
                }
                if (theBall.mFlag124)
                {
                    DoStyleShot(aX, aStyleY, 0x10, aRefObj);
                }
                else if (mPowerupCount[4][mCurPlayer] > 0 && mInt188 > 0)
                {
                    DoStyleShot(aX, aStyleY, 0x14, aRefObj);
                }
                if (noFreeBalls)
                {
                    return;
                }
                if (showText)
                {
                    if (!skillful)
                    {
                        aSoundMgr.AddSound(Res.SOUND_FREEBALL);
                    }
                    aText = AddStandardText(SexyAppBase.Loc("Free Ball!"), aX, aY, 6);
                    mInt44 = aText.mId;
                }
                mFlag19 = true;
                mStats[mCurPlayer].mBucketFreeBalls++;
                if (!mDuelMode)
                {
                    IncNumBalls(1, 0, false);
                }
            }
            if (aText != null)
            {
                aText.mLayer = -1;
                aText.mVelY = -1.0f;
                aText.mFadeOut = 10;
                aText.AddScale(0.2f, 0);
                aText.AddScale(1.0f, 10);
                aText.mDuration = 100;
                aText.SetRefObj(aRefObj);
            }
            mEyeTarget = new SexyVector2(aX, aY - 40.0f);
        }

        /// <summary>A ball dropped into a hole: teleport holes, the free-ball bucket, and the fever slots.</summary>
        public void HoleHit(Hole theHole, Ball theBall)
        {
            IncShotScore(theHole.mValue);
            if (theHole.mTarget != null)
            {
                Effect anEffect = mBoard.mEffectMgr.AddEffect(0x22, theBall.mX, theBall.mY, 0, false);
                anEffect.mObj1 = theBall;
                anEffect.mObj2 = theHole;
                anEffect.mVelX = theBall.mVelX;
                anEffect.mVelY = theBall.mVelY;
                anEffect.mDuration = 10;
                theBall.SetHoleOut(theHole.mTarget, theHole.mValueA0);
                PlayPortalSound();
                return;
            }
            if (theHole.mId == "freeball")
            {
                FreeBallHit(theHole, theBall);
            }
            else if (theHole.mId == "fever")
            {
                int aValue = theHole.mValue;
                if (mDuelMode)
                {
                    aValue = aValue << 1;
                }
                int aTier;
                if (aValue < 0x2711)
                {
                    aTier = 0;
                }
                else if (aValue < 0xc351)
                {
                    aTier = 1;
                }
                else
                {
                    aTier = 2;
                }
                if (aTier == 2)
                {
                    mBoard.mSoundMgr.AddSound(Res.SOUND_FIREWORKS2);
                }
                else if (aTier < 2)
                {
                    mBoard.mSoundMgr.AddSound(Res.SOUND_FIREWORKS1);
                }
                mBoard.mEffectMgr.AddFeverHoleEffect(theHole.mX, theHole.mY + 0.0f, aTier);
                if (theBall.mFlag11C)
                {
                    DoWrapAround(theBall);
                    return;
                }
            }
            mBoard.RemoveObj(theBall);
        }

        /// <summary>
        /// Spooky Ball: the ball re-enters at the top. Up to nine trial balls are simulated from there
        /// and the starting velocity that would hit the most pegs is kept.
        /// </summary>
        public void DoWrapAround(Ball theBall)
        {
            SoundMgr aSoundMgr = mBoard.mSoundMgr;
            if (!theBall.mFlagFE)
            {
                if (mPowerupCount[6][mCurPlayer] == 2)
                {
                    aSoundMgr.AddSound(Res.SOUND_SPOOKYBALL3);
                }
                else
                {
                    aSoundMgr.AddSound(Res.SOUND_SPOOKYBALL2);
                }
            }
            else
            {
                aSoundMgr.AddSound(Res.SOUND_SPOOKYBALL4);
            }
            theBall.mFlagFE = true;
            mPowerupCount[6][mCurPlayer]--;
            if (mPowerupCount[6][mCurPlayer] < 1)
            {
                mPowerupCount[6][mCurPlayer] = 0;
                theBall.mFlag11C = false;
                bool covered = false;
                if (mBoard.mTrophyInfo != null)
                {
                    covered = mBoard.mTrophyInfo.mFreeballCovered;
                }
                ActivateFreeBallCover(covered);
            }
            SexyVector2 aVel = new SexyVector2(theBall.mVelX, theBall.mVelY);
            float anOldX = theBall.mX;
            float anOldY = theBall.mY;
            float aSpeed = aVel.Magnitude();
            if (aSpeed > 3.0f)
            {
                aVel = aVel * 3.0f / aSpeed;
                theBall.SetVelocity(aVel.x, aVel.y);
            }
            float aTryVX = theBall.mVelX;
            float aTryVY = theBall.mVelY;
            int aBest = 0;
            CollisionMgr aCollisionMgr = mBoard.mCollisionMgr;
            for (int i = 0; i <= 8; i++)
            {
                mSpookyCollisions = 0;
                mSpookyIgnoreList.Clear();
                Ball aTrial = new Ball(false);
                aTrial.SetPos(theBall.mX, 10.0f);
                aTrial.SetRadius(theBall.GetRadius());
                aTrial.SetVelocity(theBall.mVelX, theBall.mVelY);
                if (i != 0)
                {
                    float aJitter = (float)(SexyUtil.Rand() % 2000) / -1000.0f + 1.0f;
                    aTrial.mVelX = aTrial.mVelX + aJitter;
                    aTryVX = aTrial.mVelX;
                    aTryVY = aTrial.mVelY;
                }
                aTrial.mPlaySounds = false;
                int aSteps = i == 0 ? 400 : 0x15e;
                aCollisionMgr.mIgnorePegs = theBall.mFireball;
                PhysObj.mNotifyCollisionFunc = NotifySpookyCollision;
                aCollisionMgr.BeginUpdateOneBall(aTrial);
                int aBottom = mBoard.mBoardRect.mHeight + 0x14;
                for (int j = 0; j < aSteps; j++)
                {
                    aTrial.Update();
                    aCollisionMgr.UpdateOneBall(aTrial, null);
                    if (aTrial.mY > (float)aBottom)
                    {
                        break;
                    }
                }
                if (aBest < mSpookyCollisions)
                {
                    theBall.mVelX = aTryVX;
                    theBall.mVelY = aTryVY;
                    aBest = mSpookyCollisions;
                }
            }
            theBall.SetAbsPos(theBall.mX, 10.0f);
            theBall.mValue138 = 0;
            theBall.mValue13C = 0;
            mBoard.mEffectMgr.AddSpookyBallWrap(theBall.mX, theBall.mY, anOldX, anOldY);
            mBoard.ClearZoom();
        }

        /// <summary>Counts distinct unhit pegs a trial Spooky Ball touches.</summary>
        public static void NotifySpookyCollision(PhysObj theBall, PhysObj theObj)
        {
            if (theObj == null || theBall == null)
            {
                return;
            }
            int aClass = theObj.GetClass();
            if (aClass != 5 && aClass != 6)
            {
                return;
            }
            PegInfo aPeg = theObj.mPegInfo;
            if (aPeg != null && !aPeg.mHit && !mSpookyIgnoreList.Contains(aPeg))
            {
                mSpookyIgnoreList.Add(aPeg);
                mSpookyCollisions++;
            }
        }
    }
}
