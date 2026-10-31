using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// The ball launcher (Sexy::Gun): a one-sided triangle that rotates about its centre and holds
    /// the loaded ball at its nose. Also simulates the aim guide and the "bouncy" extended guide.
    /// </summary>
    public class Gun : Poly
    {
        private static PhysObj mTryFindCollisionPeg;

        public Board mBoard;                                      // 0xf4

        public int mUpdateCnt;                                    // 0xf8

        public float mAngle;                                      // 0xfc

        public float mTargetAngle;                                // 0x100

        public float mBallX;                                      // 0x104

        public float mBallY;                                      // 0x108

        public float mFireSpeed;                                  // 0x10c

        public float mNoseLength;                                 // 0x110

        public float mHalfWidth;                                  // 0x114

        public float mOffset;                                     // 0x118

        public float mBallOffset;                                 // 0x11c

        public SexyVector2 mFirstHitDir;                          // 0x120

        public float mFirstHitDeflect;                            // 0x128

        public bool mNeedFirstHit;                                // 0x12c

        public float mLastAngle;                                  // 0x130

        public float mStableAngle;                                // 0x134

        public int mStableTime;                                   // 0x138

        public int mStableCount;                                  // 0x13c

        public bool mDoBouncyGuide;                               // 0x140

        public bool mGuideValid;                                  // 0x141

        public int mGuideHitIdx;                                  // 0x144

        public int mGuideHoleIdx;                                 // 0x148

        public bool mGuideHitMoving;                              // 0x14c

        public bool mFireball;                                    // 0x14d

        public bool mSuperGuide;                                  // 0x14e

        public SexyVector2 mGuideHitPos;                          // 0x150

        public float mGuideHitAngle;                              // 0x158

        public float mLastFireAngle;                              // 0x15c

        public float mLastFireX;                                  // 0x160

        public float mLastFireY;                                  // 0x164

        public float mLastFireSpeed;                              // 0x168

        public Ball mBall;                                        // 0x16c

        public Ball mGuideBall;                                   // 0x170

        public List<SexyVector2> mGuidePoints = new List<SexyVector2>(); // 0x174

        /// <summary>Recent aim changes as (angle delta, update count) (0x180).</summary>
        public LinkedList<KeyValuePair<float, int>> mAngleHistory = new LinkedList<KeyValuePair<float, int>>();

        public Gun(Board theBoard)
        {
            mBoard = theBoard;
            mUpdateCnt = 0;
            mCenterX = 0;
            mCenterY = 0;
            mAngle = 0;
            mNormalDir = 1;
            mGuideHitIdx = -1;
            mGuideHoleIdx = -1;
            mFireSpeed = 4.0f;
            mLastAngle = 0;
            mStableAngle = 0;
            mStableTime = 0;
            mStableCount = 0;
            mDoBouncyGuide = false;
            mGuideValid = false;
            mFireball = false;
            mLastFireAngle = 0;
            mLastFireX = 0;
            mLastFireY = 0;
            mLastFireSpeed = 0;
            mNoseLength = 30.0f;
            mHalfWidth = 10.0f;
            mOffset = 60.0f;
            mBallOffset = 26.0f;
            mFirstHitDeflect = 1000.0f;
            mNeedFirstHit = true;
            mGuideHitAngle = 0;
            mGuideBall = new Ball(false);
            CalcPoints();
        }

        private static float CalcAngleDiff(float theAngle1, float theAngle2)
        {
            while (theAngle2 > 6.2831855f)
            {
                theAngle2 -= 6.2831855f;
            }
            while (theAngle1 > theAngle2)
            {
                theAngle1 -= 6.2831855f;
            }
            float aDiff = theAngle2 - theAngle1;
            if (aDiff > 3.1415927f)
            {
                aDiff = -(6.2831855f - aDiff);
            }
            return aDiff;
        }

        private static int sign(float theValue)
        {
            if (theValue > 0)
            {
                return 1;
            }
            if (theValue < 0)
            {
                return -1;
            }
            return 0;
        }

        private float GetBallRadius()
        {
            return mBall != null ? mBall.mRadius : Ball.mDefRadius;
        }

        public void CalcPoints()
        {
            mConvex = true;
            mClosed = true;
            if (mLines.Count != 3)
            {
                mLines.Clear();
                for (int i = 0; i < 3; i++)
                {
                    mLines.Add(new Line(0, 0, 0, 0));
                    mLines[mLines.Count - 1].mOneSided = true;
                    mLineOffsets.Add(new SexyVector2(0, 0));
                }
            }

            float aBallDist = GetBallRadius() + mBallOffset + Layout.GUN_BALL_PADDING;
            float[] aPts = new float[8];
            aPts[0] = mCenterX + mNoseLength;
            aPts[1] = mCenterY;
            aPts[2] = mCenterX;
            aPts[3] = mCenterY - mHalfWidth;
            aPts[4] = mCenterX;
            aPts[5] = mCenterY + mHalfWidth;
            aPts[6] = mCenterX + aBallDist;
#if PEGGLE_IPHONE
            aPts[7] = mCenterY + 1.0f;
#else
            aPts[7] = mCenterY + 2.0f;
#endif
            for (int i = 0; i < 4; i++)
            {
                aPts[i * 2] += mOffset;
                SexyUtil.RotateXY(ref aPts[i * 2], ref aPts[i * 2 + 1], mCenterX, mCenterY, mAngle);
            }

            for (int i = 0; i < 3; i++)
            {
                Line aLine = mLines[i];
                int aNext = (i + 1) % 3;
                aLine.Init(aPts[i * 2], aPts[i * 2 + 1], aPts[aNext * 2], aPts[aNext * 2 + 1]);
                mLineOffsets[i] = new SexyVector2(aLine.mX1 - mCenterX, aLine.mY1 - mCenterY);
            }

            mBallX = aPts[6];
            mBallY = aPts[7];
            mBall?.SetAbsPos(mBallX, mBallY);
            CalcBoundingBox();
        }

        public void Clear()
        {
            mBall = null;
            mGuidePoints.Clear();
            mAngleHistory.Clear();
            mUpdateCnt = 0;
            mDoBouncyGuide = false;
            mFireball = false;
            mSuperGuide = false;
            mFirstHitDeflect = 1000.0f;
            mNeedFirstHit = true;
        }

        public override void SyncState(DataSync theSync)
        {
            SyncCommon(theSync, false);
            if (theSync.GetReader() != null)
            {
                mUpdateCnt = 0;
            }
            bool hasUpdateCnt = mUpdateCnt != 0;
            bool hasLastFire = mLastFireSpeed != 0.0f;
            theSync.SyncBoolBit(ref hasUpdateCnt);
            theSync.SyncBoolBit(ref mDoBouncyGuide);
            theSync.SyncBoolBit(ref hasLastFire);
            theSync.SyncBoolBit(ref mGuideValid);
            theSync.SyncBoolBit(ref mFireball);
            theSync.SyncBoolBit(ref mSuperGuide);
            theSync.EndBit();
            if (hasUpdateCnt)
            {
                theSync.SyncLong(ref mUpdateCnt);
            }
            if (hasLastFire)
            {
                theSync.SyncFloat(ref mLastFireAngle);
                theSync.SyncFloat(ref mLastFireX);
                theSync.SyncFloat(ref mLastFireY);
                theSync.SyncFloat(ref mLastFireSpeed);
            }
            theSync.SyncFloat(ref mAngle);
            mBall = theSync.SyncRefCount(mBall, () => new Ball(false));
            if (theSync.GetReader() != null)
            {
                CalcPoints();
                mTargetAngle = mAngle;
            }
        }

        public void SetDoBouncyGuide(bool doBouncy)
        {
            mDoBouncyGuide = doBouncy;
            mGuidePoints.Clear();
        }

        public float CalcAngularVelocity()
        {
            if (mAngleHistory.Count == 0)
            {
                return 0.0f;
            }
            while (mAngleHistory.Count != 0 && mUpdateCnt - mAngleHistory.First.Value.Value > 20)
            {
                mAngleHistory.RemoveFirst();
            }
            if (mAngleHistory.Count == 0)
            {
                return 0.0f;
            }
            float aSum = 0.0f;
            foreach (KeyValuePair<float, int> anEntry in mAngleHistory)
            {
                aSum += anEntry.Key;
            }
            int aTime = mUpdateCnt - mAngleHistory.First.Value.Value;
            if (aTime < 1)
            {
                aTime = 1;
            }
            return aSum / aTime;
        }

        public void UpdateCommon()
        {
            if (mAngle == mLastAngle)
            {
                mStableCount++;
                if (mStableCount > 29)
                {
                    mStableAngle = mAngle;
                    mStableTime = mUpdateCnt;
                }
            }
            else
            {
                mLastAngle = mAngle;
                mStableCount = 0;
            }
        }

        /// <summary>Simulates the shot with the guide ball, recording its path and first hit.</summary>
        public void UpdateBouncyGuide()
        {
            mGuidePoints.Clear();
            mGuideHitIdx = -1;
            mGuideHoleIdx = -1;
            float aStartX = mBallX;
            float aStartY = mBallY;
            float aRadius = GetBallRadius();
            Ball aBall = mGuideBall;
            aBall.SetPos(aStartX, aStartY);
            aBall.mRadius = aRadius;
            aBall.mPlaySounds = false;
            aBall.SetVelocity(mFireSpeed * (float)Math.Cos(mAngle), -(mFireSpeed * (float)Math.Sin(mAngle)));

            int aHits = 0;
            int aMaxSteps = mSuperGuide ? 300 : 200;
            int aExtraSteps = 0;
            CollisionMgr aMgr = mBoard.mCollisionMgr;
            aMgr.mHoleOut = false;
            aMgr.mIgnorePegs = mBall != null && mBall.mFireball;
            if (aMgr.mIgnorePegs)
            {
                aBall.SetFireballRadius();
            }
            PhysObj.mNotifyCollisionFunc = null;
            aMgr.BeginUpdateOneBall(aBall);
            mGuideHitMoving = false;
            int aLimitY = mBoard.mBoardRect.mHeight + 20;
            int aMaxHits = 1;
            SexyVector2[] aHitPos = new SexyVector2[1];
            for (int i = 0; i < aMaxSteps; i++)
            {
                aBall.Update();
                aHitPos[0] = new SexyVector2();
                if (aMgr.UpdateOneBall(aBall, aHitPos))
                {
                    if (aHits == 0)
                    {
                        mGuideHitIdx = i;
                        mGuideHitPos = aHitPos[0];
                        bool aMoving = false;
                        foreach (PhysObj anObj in aMgr.mCollideList)
                        {
                            if (anObj.mMover != null && anObj.IsMoving())
                            {
                                aMoving = true;
                                break;
                            }
                        }
                        if (mNeedFirstHit)
                        {
                            SexyVector2 aDir = new SexyVector2(aBall.mVelX, aBall.mVelY).Normalize();
                            if (mFirstHitDeflect >= 1000.0f)
                            {
                                mFirstHitDeflect = 1.0f;
                            }
                            else
                            {
                                mFirstHitDeflect = Math.Abs(aDir.Dot(mFirstHitDir));
                                mFirstHitDeflect = SexyUtil.ClampRange((float)Math.Acos(mFirstHitDeflect) / 1.5707964f, 0.0001f, 1.0f);
                            }
                            mFirstHitDir = aDir;
                            mNeedFirstHit = false;
                        }
                        mGuideHitAngle = (float)Math.Atan2(aBall.mVelY, aBall.mVelX);
                        if (aMoving)
                        {
                            mGuideHitMoving = true;
                            if (aExtraSteps == 0 || aExtraSteps > 5)
                            {
                                aExtraSteps = 5;
                            }
                        }
                    }
                    aHits++;
                    if (aHits == aMaxHits && aExtraSteps == 0)
                    {
                        aExtraSteps = mDoBouncyGuide ? 100 : 1;
                    }
                }
                if (mGuideHoleIdx == -1 && aMgr.mHoleOut)
                {
                    mGuideHoleIdx = i;
                }
                if (aHits > aMaxHits)
                {
                    break;
                }
                if (aBall.mY > aLimitY)
                {
                    break;
                }
                mGuidePoints.Add(new SexyVector2(aBall.mX, aBall.mY));
                if (aExtraSteps != 0 && --aExtraSteps == 0)
                {
                    break;
                }
            }
            mGuideValid = !aMgr.mHoleOut;
            aMgr.mIgnorePegs = false;
        }

        private static void FIndCollisionPegNotify(PhysObj theBall, PhysObj theObj)
        {
            if (theObj.mPegInfo != null && !theObj.mPegInfo.mHit && theObj.IsVisible())
            {
                mTryFindCollisionPeg = theObj;
            }
        }

        /// <summary>Returns the first unhit, visible peg the current aim would strike (within 120 frames).</summary>
        public PhysObj TryFindCollisionPeg()
        {
            float aStartX = mBallX;
            float aStartY = mBallY;
            float aRadius = GetBallRadius();
            Ball aBall = new Ball(false);
            aBall.SetPos(aStartX, aStartY);
            aBall.mRadius = aRadius;
            aBall.mPlaySounds = false;
            mTryFindCollisionPeg = null;
            aBall.SetVelocity(mFireSpeed * (float)Math.Cos(mAngle), -(mFireSpeed * (float)Math.Sin(mAngle)));
            CollisionMgr aMgr = mBoard.mCollisionMgr;
            PhysObj.mNotifyCollisionFunc = FIndCollisionPegNotify;
            aMgr.BeginUpdateOneBall(aBall);
            SexyVector2[] aHitPos = new SexyVector2[1];
            PhysObj aResult = null;
            for (int i = 0; ; i++)
            {
                if (i >= 120)
                {
                    PhysObj.mNotifyCollisionFunc = null;
                    break;
                }
                aBall.Update();
                aHitPos[0] = new SexyVector2();
                aMgr.UpdateOneBall(aBall, aHitPos);
                if (mTryFindCollisionPeg != null)
                {
                    aResult = mTryFindCollisionPeg;
                    break;
                }
            }
            mTryFindCollisionPeg = null;
            return aResult;
        }

        public override void Update()
        {
            base.Update();
            mBall?.UpdateEffects();
            mUpdateCnt++;
            float aVelX = 0.0f;
            float aVelY = 0.0f;
            if (mLines.Count != 0)
            {
                aVelX = mLines[0].mVelX;
                aVelY = mLines[0].mVelY;
            }
            float anAngVel = CalcAngularVelocity();
            if (mBall != null)
            {
                mBall.SetVelocity(aVelX + anAngVel * 10.0f * (float)Math.Cos(mAngle + 1.5707964f),
                                  aVelY - anAngVel * 10.0f * (float)Math.Sin(mAngle + 1.5707964f));
            }
            mRotVel = anAngVel;
            if (!Board.mSlowMoPending)
            {
                UpdateCommon();
            }

            if (mAngle != mTargetAngle)
            {
                float aTarget = mTargetAngle;
                float aCur = mAngle;
                if (aTarget < 1.5707964f)
                {
                    aTarget += 6.2831855f;
                }
                if (aCur < 1.5707964f)
                {
                    aCur += 6.2831855f;
                }
                if (aCur < aTarget)
                {
                    mAngle += 0.2f;
                    if (aTarget <= aCur + 0.2f)
                    {
                        mTargetAngle = SexyUtil.GetCanonicalAngleRad(mTargetAngle);
                        mAngle = mTargetAngle;
                    }
                }
                else
                {
                    mAngle -= 0.2f;
                    if (aCur - 0.2f <= aTarget)
                    {
                        mTargetAngle = SexyUtil.GetCanonicalAngleRad(mTargetAngle);
                        mAngle = mTargetAngle;
                    }
                }
                CalcPoints();
            }

            if (!Board.mLevelEditing && mBoard.mLogicMgr.mState == LogicMgr.STATE_AIMING)
            {
                UpdateBouncyGuide();
            }

            if (mLastFireSpeed != 0.0f &&
                (mBoard.mLogicMgr.mState != LogicMgr.STATE_SHOOTING || mBoard.mLogicMgr.mStateCount > 150))
            {
                mLastFireSpeed = 0.0f;
            }
        }

        public override void UpdateSlowMo(float theFraction)
        {
            base.UpdateSlowMo(theFraction);
            UpdateCommon();
        }

        /// <summary>Aims the gun; instant snaps (and resets the stable-angle timer), otherwise it turns at 0.2 rad/frame.</summary>
        public bool SetAngle(float theAngle, bool instant)
        {
            if (theAngle == mAngle)
            {
                return false;
            }
            if (!instant)
            {
                if (theAngle == mTargetAngle)
                {
                    return true;
                }
                float aDiff = CalcAngleDiff(mAngle, theAngle);
                if (mAngleHistory.Count != 0 && sign(mAngleHistory.Last.Value.Key) == sign(aDiff))
                {
                    mAngleHistory.Clear();
                }
                mAngleHistory.AddLast(new KeyValuePair<float, int>(aDiff, mUpdateCnt));
                mTargetAngle = theAngle;
            }
            else
            {
                mAngleHistory.Clear();
                mStableTime = mUpdateCnt - 1000;
                mAngle = theAngle;
                mTargetAngle = theAngle;
                CalcPoints();
            }
            return true;
        }

        public void Reload(Ball theBall)
        {
            mBall = theBall;
            mBall.mIsPeg = true;
            mFireball = false;
            CalcPoints();
        }

        public void SetElectrified(bool on)
        {
            if (mBall != null)
            {
                GlobalStaticVars.gSexyAppBase.PlaySample(Res.SOUND_LIGHTNING_START);
                mBall.SetElectrified(on, 0x28);
            }
        }

        public void SetFireball(bool on)
        {
            if (mBall != null)
            {
                mBall.SetFireball(on);
                mFireball = on;
                CalcPoints();
            }
        }

        /// <summary>
        /// Releases the loaded ball. With useStable, a shot within 10 frames of the aim settling
        /// uses the settled angle (so a flick while clicking doesn't spoil the shot).
        /// </summary>
        public Ball Fire(bool useStable)
        {
            if (mBall == null)
            {
                return null;
            }
            float anAngle = mAngle;
            if (useStable)
            {
                int aTime = mUpdateCnt - mStableTime;
                if (aTime > -1 && aTime < 10)
                {
                    mAngle = mStableAngle;
                    CalcPoints();
                    if (mDoBouncyGuide)
                    {
                        UpdateBouncyGuide();
                    }
                }
            }
            Ball aBall = mBall;
            mBall = null;
            mLastFireAngle = mAngle;
            mLastFireX = mBallX;
            mLastFireY = mBallY;
            mLastFireSpeed = mFireSpeed;
            aBall.mIsPeg = false;
            aBall.SetVelocity(mFireSpeed * (float)Math.Cos(mAngle), -(mFireSpeed * (float)Math.Sin(mAngle)));
            if (mAngle != anAngle)
            {
                mAngle = anAngle;
                CalcPoints();
            }
            return aBall;
        }

        public bool CanFire()
        {
            return mBall != null;
        }

        private static void DrawGuideTriangle(Graphics g, float theX1, float theY1, float theX2, float theY2, float theX3, float theY3)
        {
            Transform aTrans = new Transform();
            aTrans.RotateRad((float)Math.Atan2(theY1 - theY2, theX2 - theX1));
            g.DrawImageTransform(Res.IMAGE_GUIDEARROW, aTrans,
                (float)(theX3 * SexyUtil.gPhysicalDisplayHeight / 600.0), (float)(theY3 * SexyUtil.gPhysicalDisplayHeight / 600.0));
        }

        private static void DrawGuideLine(Graphics g, float theX1, float theY1, float theX2, float theY2, float theX3, float theY3)
        {
            Transform aTrans = new Transform();
            aTrans.RotateRad((float)Math.Atan2(theY1 - theY2, theX2 - theX1));
            g.DrawImageTransformF(Res.IMAGE_GUIDERECT, aTrans,
                (float)(theX3 * SexyUtil.gPhysicalDisplayHeight / 600.0), (float)(theY3 * SexyUtil.gPhysicalDisplayHeight / 600.0));
        }

        /// <summary>
        /// Draws the dotted aim guide along the ballistic arc, every 4 frames from theStart up to
        /// theLength, snapping dots onto the simulated path; ends with an arrow when isEnd, or with
        /// the hit marker once past theHitIdx.
        /// </summary>
        public void DoDrawGuide(Graphics g, float theX, float theY, float theAngle, float theSpeed, int theHitIdx, int theLength, int theStart, bool isEnd)
        {
            bool drawHit = true;
            SexyVector2 aPos = new SexyVector2(theX, theY);
            SexyVector2 aVel = new SexyVector2(theSpeed * (float)Math.Cos(theAngle), -theSpeed * (float)Math.Sin(theAngle));
            SexyVector2 anAccel = new SexyVector2(0.0f, Ball.mGravity);
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            if (mFireball)
            {
                g.SetColor(SexyUtil.MakeColor(0x903010));
            }
            else if (mBoard.IsLightningBallActive())
            {
                g.SetColor(SexyUtil.MakeColor(0x8c389a));
            }
            else if (mSuperGuide || mBoard.mLogicMgr.mSuperGuide)
            {
                g.SetColor(SexyUtil.MakeColor(0xaaaa60));
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(0x109090));
            }

            bool aHitReached = theHitIdx == -1;
            bool aDoHit = false;
            if (theStart < 1)
            {
                theStart = 1;
            }
            int i = 2;
            SexyVector2 aCur = aPos + aVel * i + anAccel * 0.5f * i * i;
            while (i < theLength)
            {
                i += 4;
                SexyVector2 aPrev = aCur;
                SexyVector2 aNext = aPos + aVel * i + anAccel * 0.5f * i * i;
                SexyVector2 aDot = (i >= 3 && i < mGuidePoints.Count + 3) ? mGuidePoints[i - 3] : aNext;
                aCur = aNext;
                bool aLast = i >= theLength && isEnd;
                int aHitLimit = aLast ? theHitIdx + 2 : theHitIdx;
                if (!aHitReached && i > aHitLimit)
                {
                    aHitReached = true;
                    aDoHit = true;
                }
                if (i < theStart)
                {
                    continue;
                }
                if (aLast)
                {
                    if (!aDoHit)
                    {
                        DrawGuideTriangle(g, aPrev.x, aPrev.y, aCur.x, aCur.y, aDot.x, aDot.y);
                    }
                }
                else if (!aDoHit)
                {
                    DrawGuideLine(g, aPrev.x, aPrev.y, aCur.x, aCur.y, aDot.x, aDot.y);
                }

                if (aDoHit)
                {
                    aDoHit = false;
                    if (drawHit)
                    {
                        float aScale = SexyUtil.gPhysicalDisplayHeight;
                        if (mDoBouncyGuide && mGuidePoints.Count != 0)
                        {
                            Transform aTrans = new Transform();
                            aTrans.RotateRad(4.712389f - mGuideHitAngle);
                            float aHitX = (float)(mGuideHitPos.x * aScale / 600.0);
                            float aHitY = (float)(mGuideHitPos.y * aScale / 600.0);
                            g.DrawImageTransformF(Res.IMAGE_ALPHABALL, aTrans, aHitX, aHitY);
                            g.DrawImageTransformF(Res.IMAGE_ALPHABALL, aTrans, aHitX, aHitY);
                        }
                        else
                        {
                            g.PushState();
                            g.SetDrawMode(0);
                            g.SetColorizeImages(true);
                            g.SetColor(SexyUtil.MakeColor(0xffffff));
                            Transform aTrans = new Transform();
                            aTrans.RotateRad((float)Math.Atan2(aPrev.y - aCur.y, aCur.x - aPrev.x));
                            g.DrawImageTransformF(Res.IMAGE_GUIDEHIT, aTrans,
                                (float)(mGuideHitPos.x * aScale / 600.0), (float)(mGuideHitPos.y * aScale / 600.0));
                            g.PopState();
                        }
                        break;
                    }
                }
            }
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public int GetMaxGuideLength()
        {
            return mDoBouncyGuide ? 80 : 50;
        }

        /// <summary>
        /// Port addition: a step between guide points longer than this is a teleport (a ball moves at most
        /// 15 a step, see Ball.Update), where the ribbon breaks.
        /// </summary>
        private const float GUIDE_TELEPORT_JUMP = 30.0f;

        /// <summary>Draws the simulated path past the regular guide as a tapered textured ribbon.</summary>
        public void DrawBouncyGuide(Graphics g)
        {
            if (mGuidePoints.Count == 0)
            {
                return;
            }
            int aMaxLen = GetMaxGuideLength();
            int aBegin = 0;
            int anEnd = mGuidePoints.Count;
            LogicMgr aLogic = mBoard.mLogicMgr;
            int aState = aLogic.mState;
            int aSize = mGuidePoints.Count;
            int aStart = 1;
            if (mGuideHitIdx < 0)
            {
                aStart = aMaxLen + 1;
            }
            else if (mGuideHitIdx > 1)
            {
                aStart = mGuideHitIdx;
                if (aMaxLen + 1 < aStart)
                {
                    aStart = aMaxLen + 1;
                }
            }

            if (aState == LogicMgr.STATE_AIMING || aState == 10)
            {
                int aGrow = aLogic.mGuideCount;
                if (aGrow > 0)
                {
                    if (aGrow < 11)
                    {
                        return;
                    }
                    int aShown = (aGrow - 10) * 5;
                    if (aSize < aShown)
                    {
                        aShown = aSize;
                    }
                    anEnd -= aSize - aShown;
                }
            }
            else if (aState == LogicMgr.STATE_SHOOTING && mGuideValid)
            {
                if (mLastFireSpeed == 0.0f)
                {
                    return;
                }
                int aFade = aLogic.mStateCount - 10;
                if (aStart < aFade)
                {
                    aStart = aFade;
                }
            }
            else
            {
                return;
            }

            if (aStart > 0)
            {
                if (aSize <= aStart)
                {
                    return;
                }
                aBegin += aStart;
            }

            if (mFireball)
            {
                g.SetColor(SexyUtil.MakeColor(0xaa4020));
            }
            else if (mSuperGuide)
            {
                int anAlpha = 0xff;
                if (aLogic.mState == LogicMgr.STATE_SHOOTING && aLogic.mStateCount > 10)
                {
                    anAlpha = (10 - aLogic.mStateCount) * 0xff / 10;
                    if (anAlpha < 1)
                    {
                        return;
                    }
                }
                g.SetColor(SexyUtil.MakeColor(0xffff60, anAlpha));
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(0x208888, 0xff));
            }

            float aPrevX = mGuidePoints[0].x;
            float aPrevY = mGuidePoints[0].y;
            g.SetDrawMode(1);
            int anIdx = 0;
            float aPrevPerpX = 0.0f;
            float aPrevPerpY = 0.0f;
            if (GlobalStaticVars.gSexyAppBase.Is3DAccelerated())
            {
                aPrevX = mGuidePoints[aBegin].x;
                aPrevY = mGuidePoints[aBegin].y;
            }
            g.PushState();
            g.SetColorizeImages(true);
            // The framework applies the transform stack to textured triangles itself, so only the
            // untransformed path of the original is needed here.
            float aScale = SexyUtil.gPhysicalDisplayHeight / 600.0f;
            for (int it = aBegin; it < anEnd; it++, anIdx++)
            {
                float aX = mGuidePoints[it].x;
                float aY = mGuidePoints[it].y;
                float aDX = aPrevX - aX;
                float aDY = aPrevY - aY;
                if (aDX * aDX + aDY * aDY > GUIDE_TELEPORT_JUMP * GUIDE_TELEPORT_JUMP)
                {
                    // Port fix: the guide ball went through a teleport hole. The original
                    // drew a ribbon across the jump; it restarts at the exit instead, as it
                    // starts at the cannon.
                    aPrevX = aX;
                    aPrevY = aY;
                    aPrevPerpX = 0.0f;
                    aPrevPerpY = 0.0f;
                    continue;
                }
                if (aDX != 0.0f && aDY != 0.0f)
                {
                    float aWidth = 1.0f;
                    if (aSize - aStart - anIdx < 6)
                    {
                        aWidth = (float)Math.Sqrt(Math.Abs((aSize - aStart - anIdx) / 6.0f));
                    }
                    aWidth *= 7.0f;
                    float aLen = (float)Math.Sqrt(aDX * aDX + aDY * aDY);
                    aDX = aWidth * aDX / aLen;
                    aDY = aWidth * aDY / aLen;
                }
                float aPerpX = -aDY;
                float aPerpY = aDX;
                SexyVector2 aP1 = new SexyVector2(aX + aPerpX, aY + aPerpY);
                SexyVector2 aP2 = new SexyVector2(aX - aPerpX, aY - aPerpY);
                SexyVector2 aP3 = new SexyVector2(aPrevX + aPrevPerpX, aPrevY + aPrevPerpY);
                SexyVector2 aP4 = new SexyVector2(aPrevX - aPrevPerpX, aPrevY - aPrevPerpY);
                if (aPrevPerpX == 0.0f && aPrevPerpY == 0.0f)
                {
                    aP3 = new SexyVector2(aPrevX + aPerpX, aPrevY + aPerpY);
                    aP4 = new SexyVector2(aPrevX + aPerpX * -1.0f, aPrevY - aPerpY);
                }
                aPrevPerpX = aPerpX;
                aPrevPerpY = aPerpY;
                TriVertex aV3 = new TriVertex(aP3.x * aScale, aP3.y * aScale, 0.5f, 0.0f);
                TriVertex aV4 = new TriVertex(aP4.x * aScale, aP4.y * aScale, 0.5f, 1.0f);
                TriVertex aV1 = new TriVertex(aP1.x * aScale, aP1.y * aScale, 0.5f, 0.0f);
                TriVertex aV2 = new TriVertex(aP2.x * aScale, aP2.y * aScale, 0.5f, 1.0f);
                g.DrawTriangleTex(Res.IMAGE_GUIDERECT2, aV3, aV4, aV1);
                g.DrawTriangleTex(Res.IMAGE_GUIDERECT2, aV4, aV1, aV2);
                aPrevX = aX;
                aPrevY = aY;
            }
            g.SetColorizeImages(false);
            g.PopState();
            g.SetDrawMode(0);
        }

        public bool NeedDrawMouseClick()
        {
            if ((mDoBouncyGuide || mGuideHitIdx > -1) && mGuideHitIdx <= GetMaxGuideLength())
            {
                return false;
            }
            return true;
        }

        public void DrawGuide(Graphics g, bool drawAim)
        {
            int aGrow = mBoard.mLogicMgr.mGuideCount;
            int aMaxLen = GetMaxGuideLength();
            int aLen = aMaxLen;
            if (drawAim)
            {
                if (aGrow > 10)
                {
                    aGrow = Math.Min((aGrow - 10) * 4, aLen);
                    DoDrawGuide(g, mBallX, mBallY, mAngle, mFireSpeed, mGuideHitIdx, aGrow, 2, aGrow >= aMaxLen);
                }
                else if (aGrow == 0)
                {
                    DoDrawGuide(g, mBallX, mBallY, mAngle, mFireSpeed, mGuideHitIdx, aLen, 2, true);
                }
            }

            if (mDoBouncyGuide && aGrow == 0)
            {
                LogicMgr aLogic = mBoard.mLogicMgr;
                int aStart = aLogic.mStateCount - 6;
                if (aLogic.mState == LogicMgr.STATE_SHOOTING && aStart < 100 && mGuideValid)
                {
                    if (aStart < 0)
                    {
                        aStart = 0;
                    }
                    bool isEnd = false;
                    int aHitIdx = mGuideHitIdx;
                    if (aHitIdx < 0 || aHitIdx >= aLen)
                    {
                        isEnd = true;
                    }
                    else
                    {
                        aLen = aHitIdx;
                    }
                    if (aStart < aLen && mLastFireSpeed != 0.0f)
                    {
                        DoDrawGuide(g, mLastFireX, mLastFireY, mLastFireAngle, mLastFireSpeed, mGuideHitIdx, aLen, aStart, isEnd);
                    }
                }
                DrawBouncyGuide(g);
            }
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
        }

        public override int GetClass()
        {
            return CLASS_GUN;
        }
    }
}
