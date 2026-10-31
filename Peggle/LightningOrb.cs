using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// The lightning rod that rides the free-ball bucket while a lightning ball is in play
    /// (Sexy::LightningOrb): it rises out of the bucket, pulses with rings and sparks, crackles,
    /// then sinks back.
    /// </summary>
    public class LightningOrb
    {
        public int mState;                                        // 0x00

        public int mBaseX;                                        // 0x04

        public float mBaseY;                                      // 0x08

        public int mX;                                            // 0x0c

        public float mY;                                          // 0x10

        public float mVelY;                                       // 0x14

        public float mTopY;                                       // 0x18

        public float mRestY;                                      // 0x1c

        public int mRingLife;                                     // 0x20

        public LogicMgr mLogicMgr;                                // 0x24

        public LightningSparkEffect mSparks = new LightningSparkEffect(); // 0x28

        public int mStateCount;                                   // 0x3c

        public int mPulseCount;                                   // 0x40

        public int mRingCounter;                                  // 0x44

        public int mPulsePeriod;                                  // 0x48

        public int mRingInterval;                                 // 0x4c

        public List<int> mRings = new List<int>();                // 0x50

        public LightningOrb(LogicMgr theLogicMgr)
        {
            mRestY = 0;
            mTopY = 0;
            mY = 1000.0f;
            mLogicMgr = theLogicMgr;
            Reset();
        }

        public void Reset()
        {
            mStateCount = 0;
            mState = 0;
            mBaseX = 0;
            mBaseY = 0;
            mVelY = 0;
            mX = 0;
            mRingCounter = 0;
            mRestY = 586.0f;
            mTopY = 512.0f;
            mY = mRestY;
        }

        public void Update()
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null || aBoard.mFlagBA)
            {
                return;
            }
            PhysObj aBucket = aBoard.GetBucket();
            if (aBucket == null)
            {
                return;
            }
            mPulsePeriod = 50;
            mRingInterval = 25;
            mRingLife = 90;
            for (int i = 0; i < mRings.Count;)
            {
                mRings[i]--;
                if (mRings[i] < 1)
                {
                    mRings.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            if (mState == 1 || mState == 2)
            {
                if (mRingCounter % mRingInterval == 0)
                {
                    mRings.Add(mRingLife);
                }
                mRingCounter++;
            }
            mSparks.SetScale(0.8f);
            mSparks.Update(mState == 1 || mState == 2);

            switch (mState)
            {
                case 0:
                    aBoard = PhysObj.GetBoard();
                    if (aBoard != null && aBoard.IsLightningBallActive())
                    {
                        mRingCounter = 0;
                        mState = 1;
                    }
                    break;
                case 1:
                    aBoard = PhysObj.GetBoard();
                    if (aBoard == null || !aBoard.IsLightningBallActive())
                    {
                        mVelY = 0;
                        mState = 7;
                        mStateCount = 0;
                    }
                    else
                    {
                        mVelY = -1.0f;
                        mY = Math.Max(mTopY, mY + mVelY);
                        if (mY == mTopY)
                        {
                            mStateCount = 0;
                            mPulseCount = 0;
                            mVelY = 0;
                            mState = 2;
                        }
                    }
                    syncPosWithBucket();
                    mBaseX = mX;
                    mBaseY = mY;
                    break;
                case 2:
                    aBoard = PhysObj.GetBoard();
                    if (aBoard == null || !aBoard.IsLightningBallActive())
                    {
                        mVelY = 0;
                        mState = 7;
                        mStateCount = 0;
                    }
                    else
                    {
                        mY = mBaseY + SexyUtil.PulseVal(mPulseCount, mPulsePeriod) * 5.0f;
                        syncPosWithBucket();
                        mPulseCount++;
                    }
                    mBaseX = mX;
                    break;
                case 3:
                    mBaseX = mX;
                    mY = mBaseY + SexyUtil.PulseVal(mPulseCount, mPulsePeriod) * 5.0f;
                    mPulseCount++;
                    if (mStateCount > 30)
                    {
                        mStateCount = 0;
                        mState = 4;
                    }
                    break;
                case 4:
                    mX = mBaseX + SexyUtil.Rand() % 5 - 2;
                    mY = mBaseY + SexyUtil.Rand() % 5 - 2.0f;
                    if (mLogicMgr.mBoard != null)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Math.Abs(SexyUtil.GetRandFloat());
                            float anAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 360);
                            float aCos = (float)Math.Cos(anAngle) * 0.7f;
                            float aSin = (float)Math.Sin(anAngle) * 0.7f;
                            float aSpeed = SexyUtil.Rand() % 1000 / 1000.0f * 1.5f + 0.4f;
                            Effect anEffect = mLogicMgr.mBoard.mEffectMgr.AddEffect(Effect.TYPE_LIGHTNING_CRACKLE,
                                mBaseX + aCos * 20.0f, mBaseY + 0.0f + aSin * 20.0f, 0, false);
                            anEffect.mUpdateCnt = 0;
                            anEffect.mColor = SexyUtil.Rand() % 2 == 0 ? 0x8888ff : 0xff88ff;
                            anEffect.mVelX = aCos * aSpeed;
                            anEffect.mVelY = (float)(aSin * aSpeed + -0.1);
                            anEffect.mDuration = 0x82;
                            anEffect.mGravity = 0.001f;
                        }
                    }
                    if (mStateCount > 50)
                    {
                        mStateCount = 0;
                        mState = mLogicMgr.mDimLowObjects ? 9 : 5;
                    }
                    break;
                case 5:
                    if (!mLogicMgr.mDimLowObjects)
                    {
                        if (mLogicMgr.mBoard != null)
                        {
                            int aDist = Math.Abs((int)(aBucket.GetXPos() - mX));
                            int aFutureDist = Math.Abs((int)(aBucket.GetFutureX(50) - mX));
                            if (mStateCount > 15 && aDist > 100 && aFutureDist > 100)
                            {
                                mStateCount = 0;
                                mState = 6;
                            }
                        }
                    }
                    else
                    {
                        mStateCount = 0;
                        mState = 9;
                    }
                    break;
                case 6:
                    if (!mLogicMgr.mDimLowObjects)
                    {
                        if (mY < mRestY)
                        {
                            mVelY += 0.04f;
                            mY = Math.Min(mRestY, mY + mVelY);
                        }
                        else
                        {
                            mVelY = 0;
                            mState = 0;
                        }
                    }
                    else
                    {
                        mStateCount = 0;
                        mState = 9;
                    }
                    break;
                case 7:
                    if (mStateCount > 60)
                    {
                        mStateCount = 0;
                        mState = 8;
                    }
                    syncPosWithBucket();
                    break;
                case 8:
                    aBoard = PhysObj.GetBoard();
                    if (aBoard != null && aBoard.IsLightningBallActive())
                    {
                        mVelY = 0;
                        mState = 1;
                        mStateCount = 0;
                    }
                    else
                    {
                        mVelY = 1.0f;
                        mY = Math.Min(mRestY, mY + mVelY);
                        if (mY == mRestY)
                        {
                            mStateCount = 0;
                            mVelY = 0;
                            mPulseCount = 0;
                            mBaseY = mY;
                            mState = 0;
                        }
                    }
                    syncPosWithBucket();
                    break;
                case 9:
                    if (mStateCount == 100)
                    {
                        mY = mRestY;
                        mState = 0;
                    }
                    break;
            }
            mStateCount++;
        }

        public void syncPosWithBucket()
        {
            mX = (int)GetPosX(0, 0, null);
        }

        public void Draw(Graphics g)
        {
            if (mY >= 600.0f)
            {
                return;
            }
            if (PhysObj.GetBoard().GetBucket() == null)
            {
                return;
            }
            Image anImage = Res.IMAGE_LIGHTNING_ROD_ON;
            if (mState == 4 || mState == 2 || mState == 1)
            {
                anImage = Res.IMAGE_LIGHTNING_ROD_ZAP;
            }
            else if (mState >= 5 && mState <= 9)
            {
                anImage = Res.IMAGE_LIGHTNING_ROD;
            }
            drawRings(g, Res.IMAGE_LIGHTNING_ROD_RING_BACK, 0x28);
            if (mState == 9)
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, 0xff - (int)(mStateCount * 255.0f / 100.0f)));
            }
            int aH = SexyUtil.gPhysicalDisplayHeight;
            g.DrawImage(anImage, (int)((double)(mX * aH) / 600.0 - anImage.GetWidth() / 2),
                                 (int)((double)(mY * aH) / 600.0 - anImage.GetHeight() / 2));
            mSparks.Draw(g, mX, (int)(mY + -2.0f));
            if (mState == 9)
            {
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                g.SetColorizeImages(false);
            }
            drawRings(g, Res.IMAGE_LIGHTNING_ROD_RING_FRONT, 0x28);
        }

        public void drawRings(Graphics g, Image theImage, int theOffsetY)
        {
            g.SetColorizeImages(true);
            g.SetDrawMode(1);
            int aH = SexyUtil.gPhysicalDisplayHeight;
            for (int i = 0; i < mRings.Count; i++)
            {
                float t = 1.0f - mRings[i] / (float)mRingLife;
                float anAlpha = 205.0f;
                if (t < 0.4f)
                {
                    anAlpha = t / 0.4f * 205.0f;
                }
                if (t > 0.6f)
                {
                    anAlpha *= 1.0f - (t - 0.6f) / 0.39999998f;
                }
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)anAlpha));
                float aRise = 44.0f;
                int aWidth = (int)(theImage.GetWidth() * (t * 0.4f + 0.45f));
                int aHeight = (int)(theImage.GetHeight() * (t * 0.4f + 0.45f));
                g.DrawImage(theImage,
                    (int)((mBaseX - aWidth / 2 + 0.0f) * aH / 600.0f),
                    (int)((mY - aHeight / 2 + 0.0f + theOffsetY - t * aRise) * aH / 600.0f),
                    (int)((double)(aH * aWidth) / 600.0), (int)((double)(aHeight * aH) / 600.0));
            }
            g.SetDrawMode(0);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.SetColorizeImages(false);
        }

        /// <summary>
        /// The rod's x on the bucket: the bucket's x plus a sideways lean that follows the
        /// bucket's back-and-forth motion (theExtra + 32 at the ends of its travel).
        /// </summary>
        public static float GetPosX(int theFrames, int theExtra, PhysObj theBucket)
        {
            if (theBucket == null)
            {
                theBucket = PhysObj.GetBoard().GetBucket();
            }
            if (theBucket == null)
            {
                return 0.0f;
            }
            float aPhase = theBucket.mMoveUpdateCnt % theBucket.mMover.mPeriod / (float)theBucket.mMover.mPeriod;
            float aQ1 = 0.25f - aPhase;
            float aQ3 = 0.75f - aPhase;
            float aLean;
            if (Math.Abs(aQ1) <= 0.25f)
            {
                aLean = (1.0f - Math.Abs(aQ1) / 0.25f) * -1.0f;
            }
            else if (Math.Abs(aQ3) <= 0.25f)
            {
                aLean = 1.0f - Math.Abs(aQ3) / 0.25f;
            }
            else
            {
                aLean = 0.0f;
            }
            return theBucket.GetXPos() + aLean * (theExtra + 0x20);
        }
    }
}
