using System;
using System.Collections.Generic;

namespace Sexy
{
    public partial class LogicMgr
    {
        /// <summary>The computer's flipper play: taps when a ball is low over a flipper, then releases a little later.</summary>
        public void CheckDoFlippers()
        {
            if (mInt19c < 0)
            {
                if (mStateCount - mInt198 <= 0x31)
                {
                    return;
                }
                bool flip = false;
                bool hold = false;
                int aTop = 400 - mBoard.mBoardRect.mY;
                int aBottom = 0x21c - mBoard.mBoardRect.mY;
                int aLeft = 0xa0 - mBoard.mBoardRect.mX;
                int aRight = 0x280 - mBoard.mBoardRect.mX;
                foreach (Ball aBall in mBoard.mBalls)
                {
                    if (!(aBall.mY > (float)aTop) || !(aBall.mY < (float)aBottom))
                    {
                        continue;
                    }
                    if (!(aBall.mX > (float)aRight) && !(aBall.mX < (float)aLeft))
                    {
                        continue;
                    }
                    int aSpeed = (int)(aBall.mVelX * aBall.mVelX + aBall.mVelY * aBall.mVelY);
                    int aChance;
                    if (aSpeed < 0xb)
                    {
                        if (aSpeed < 6)
                        {
                            aChance = mInt198 > -1 ? 2 : 10;
                        }
                        else
                        {
                            aChance = mInt198 > -1 ? 5 : 0x14;
                        }
                    }
                    else
                    {
                        aChance = mInt198 > -1 ? 10 : 0x28;
                    }
                    if (SexyUtil.Rand() % 100 <= aChance)
                    {
                        if ((float)aLeft <= aBall.mX)
                        {
                            if ((float)(aRight + 0x28) < aBall.mX || -0.2f <= aBall.mVelX)
                            {
                                hold = true;
                            }
                        }
                        else if (aBall.mX < (float)(aLeft + -0x28) || aBall.mVelX <= 0.2f)
                        {
                            hold = true;
                        }
                        flip = true;
                        break;
                    }
                }
                if (flip)
                {
                    mInt198 = mStateCount;
                    MouseDown(0, 0, 1, false, true);
                    mInt19c = mStateCount + 5;
                    if (hold)
                    {
                        int aRand = SexyUtil.Rand() % 100;
                        if (aRand < 10)
                        {
                            mInt19c += 100;
                        }
                        else if (aRand < 0x28)
                        {
                            mInt19c += 0x32;
                        }
                    }
                }
            }
            else if (mInt19c <= mStateCount)
            {
                MouseUp(0, 0, 1, true);
                mInt19c = -1;
                mInt198 = mStateCount;
            }
        }

        /// <summary>Fever sparkles trailing a ball every 10 units it travels (rainbow during fever, gold for Zen).</summary>
        public void AddFeverSparks(Ball theBall, bool isZen)
        {
            SexyVector2 aVel = new SexyVector2(theBall.mVelX, theBall.mVelY);
            float aSpeed = aVel.Magnitude();
            if (!(aSpeed > 0.0f))
            {
                return;
            }
            theBall.mSparkDist = theBall.mSparkDist + aSpeed;
            SexyVector2 aDir = aVel / aSpeed;
            SexyVector2 aPos = new SexyVector2(theBall.mX, theBall.mY);
            float aSpacing = 10.0f;
            aPos -= aDir * theBall.mSparkDist;
            aDir = aDir * aSpacing;
            float aBackAngle = (float)Math.Atan2(theBall.mVelY, theBall.mVelX) + 3.1415927f;
            bool accelerated = true; // SexyAppBase::Is3DAccelerated()
            while (theBall.mSparkDist >= aSpacing)
            {
                theBall.mSparkDist = theBall.mSparkDist - aSpacing;
                Effect aSpark = mBoard.mEffectMgr.AddEffect(0x13, aPos.x, aPos.y, 150.0f <= aPos.y ? 0 : 2, false);
                aPos += aDir;
                aSpark.mDuration = accelerated ? 0x96 : 0x4b;
                aSpark.mFlags = aSpark.mFlags | 2;
                if (!isZen)
                {
                    aSpark.mFlags = aSpark.mFlags | 1;
                    aSpark.mColor = GlobalStaticVars.gSexyAppBase.HSLToRGB(mStateCount % 0xff, 0xff, 0x80);
                }
                else
                {
                    aSpark.mColor = 0;
                }
                aSpark.mUpdateCnt = -1;
                int aBursts = (int)((float)(SexyUtil.Rand() % 1000) / 1000.0f * 2.0f + 1.0f);
                int aDrifts = (int)((float)(SexyUtil.Rand() % 1000) / 1000.0f * 10.0f + 0.0f);
                if (isZen)
                {
                    aBursts = 0;
                    aDrifts = aDrifts / 8;
                }
                int aCount = aBursts + aDrifts;
                for (int i = 0; i < aCount; i++)
                {
                    Effect aBit = mBoard.mEffectMgr.AddEffect(0x12, aSpark.mX, aSpark.mY, 3, false);
                    aBit.mGravity = Ball.mGravity * 0.0f;
                    aBit.mDuration = aSpark.mDuration + 0x19;
                    if (!isZen)
                    {
                        aBit.mFlags = aBit.mFlags | 1;
                        aBit.mColor = SexyUtil.Rand() % 0xffffff;
                    }
                    else
                    {
                        aBit.mAlpha = SexyUtil.Rand() % 2 + 1;
                        aBit.mColor = 0xffff88;
                    }
                    int aJitter = 4;
                    aBit.mX = aBit.mX + (float)(SexyUtil.Rand() % (aJitter * 2 + 1) - aJitter);
                    aBit.mY = aBit.mY + (float)(SexyUtil.Rand() % (aJitter * 2 + 1) - aJitter);
                    aBit.mAngleVel = ((float)(SexyUtil.Rand() % 1000) / 500.0f - 1.0f) * 0.0f;
                    aBit.mAngle = SexyUtil.DegToRad((float)(SexyUtil.Rand() % 0x168)) * 1.0f;
                    aBit.mUpdateCnt = -1;
                    aBit.mValue = 1;
                    if (aBursts <= i)
                    {
                        float aBitSpeed = (float)(SexyUtil.Rand() % 1000) / 1000.0f * 1.0f;
                        float aSpread = 0.3f;
                        if (SexyUtil.Rand() % 2 != 0)
                        {
                            aSpread = -0.3f;
                        }
                        aBit.mVelX = (float)Math.Cos(aBackAngle + aSpread) * aBitSpeed;
                        aBit.mVelY = (float)Math.Sin(aBackAngle + aSpread) * aBitSpeed;
                        aBit.mDuration = SexyUtil.Rand() % 0x50 + 0x50;
                    }
                }
            }
        }

        /// <summary>
        /// Per frame while balls are in play: records (or, in replays, plays back) ball positions,
        /// draws ball trails (fireball, fever sparkles, slide sparks) and checks for the shot's end.
        /// </summary>
        public bool UpdateShotBalls()
        {
            if (mBoard.mBalls.Count == 0)
            {
                return false;
            }
            foreach (Ball aBall in mBoard.mBalls)
            {
                if (!mMuteLoops)
                {
                    if (mBallPositions.Count < 60000)
                    {
                        BallPosInfo anInfo = new BallPosInfo();
                        mBallPositions.Add(anInfo);
                        anInfo.mId = (uint)aBall.mId;
                        anInfo.mTime = mStateCount;
                        anInfo.mX = aBall.mX;
                        anInfo.mY = aBall.mY;
                    }
                }
                else
                {
                    while (mBallPositions.Count != 0 && mBallPositions[mBallPositions.Count - 1].mTime < mStateCount)
                    {
                        mBallPositions.RemoveAt(mBallPositions.Count - 1);
                    }
                    for (int i = mBallPositions.Count - 1; i >= 0; i--)
                    {
                        BallPosInfo anInfo = mBallPositions[i];
                        if (mStateCount < anInfo.mTime)
                        {
                            break;
                        }
                        if (anInfo.mId == (uint)aBall.mId)
                        {
                            aBall.SetPos(anInfo.mX, anInfo.mY);
                            if (!aBall.mFlagFB)
                            {
                                aBall.mFlagFB = true;
                            }
                            else if (!aBall.mHasCollided)
                            {
                                aBall.SetVelocity(aBall.mX - aBall.mPrevX, aBall.mY - aBall.mPrevY);
                            }
                            break;
                        }
                    }
                }
                if (aBall.mHoleOutCounter != 0)
                {
                    continue;
                }
                if (aBall.mValue138 > 0 && aBall.mY < (float)aBall.mValue148)
                {
                    aBall.mValue148 = (int)aBall.mY;
                }
                if (aBall.mFireball)
                {
                    if (true) // SexyAppBase::Is3DAccelerated()
                    {
                        Effect aFlame = mBoard.mEffectMgr.AddEffect(5, aBall.mX, aBall.mY, 0, false);
                        aFlame.mDuration = 0x3c;
                        aFlame.mValue = SexyUtil.Rand() % 0x168;
                    }
                }
                else if (mDimLowObjects || mSlowMoSoundPlaying)
                {
                    AddFeverSparks(aBall, false);
                }
                else if (mSuperGuide)
                {
                    AddFeverSparks(aBall, true);
                }
                else if (aBall.mCount10C != 0 && aBall.mValue110 > 1 && aBall.mHasCollided)
                {
                    int aEvery = SexyUtil.ClampRange(3 - aBall.mValue110 / 4, 1, 3);
                    if (mStateCount % aEvery == 0)
                    {
                        Effect aSpark = mBoard.mEffectMgr.AddEffect(6, aBall.mX, aBall.mY, 0, false);
                        aSpark.mDuration = 0x19;
                        aSpark.mGravity = Ball.mGravity * 0.25f;
                        float aScale = 0.25f;
                        float aSpeed = (float)Math.Sqrt(aBall.mVelX * aBall.mVelX + aBall.mVelY * aBall.mVelY);
                        float aJitter = 0.3f;
                        float aPush = (float)(SexyUtil.Rand() % 1000) / 1000.0f * 0.55f;
                        aSpark.mVelX = (-0.0f - aBall.mVelX) + aJitter * (1.0f - (float)(SexyUtil.Rand() % 1000) / 500.0f) +
                            aBall.mLastNormal.x * aSpeed * aPush;
                        aSpark.mVelY = (-0.0f - aBall.mVelY) + aJitter * (1.0f - (float)(SexyUtil.Rand() % 1000) / 500.0f) +
                            aBall.mLastNormal.y * aSpeed * aPush;
                        aSpark.mVelX = aSpark.mVelX * aScale;
                        aSpark.mVelY = aSpark.mVelY * aScale;
                        float aSparkSpeedSq = aSpark.mVelX * aSpark.mVelX + aSpark.mVelY * aSpark.mVelY;
                        if (aSparkSpeedSq > 3.0f)
                        {
                            aSpark.mVelX = aSpark.mVelX * 3.0f / aSparkSpeedSq;
                            aSpark.mVelY = aSpark.mVelY * 3.0f / aSparkSpeedSq;
                        }
                        aSpark.mX = aSpark.mX + (aSpark.mVelX * 2.0f - aBall.mLastNormal.x * 4.5f);
                        aSpark.mY = aSpark.mY + (aSpark.mVelY * 2.0f - aBall.mLastNormal.y * 4.5f);
                    }
                }
            }
            UpdateWrapAroundBall();
            Ball aFirst = mBoard.mBalls[0];
            if (0.0f < aFirst.mX && aFirst.mX < 640.0f && 0.0f < aFirst.mY && aFirst.mY < 520.0f)
            {
                mEyeTarget = new SexyVector2(aFirst.mX, aFirst.mY);
            }
            CheckBallStop();
            return true;
        }
    }
}
