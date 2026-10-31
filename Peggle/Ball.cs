using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// The ball (Sexy::Ball). The same class is also the round peg (constructed with isPeg):
    /// pegs are static circles the ball bounces off. The ball integrates gravity, sweeps against
    /// lines, polygons and pegs up to 20 times per frame, tracks being stuck, and carries the
    /// fireball, electric and magic-hat power-up states.
    /// </summary>
    public class Ball : PhysObj
    {
        public static float mDefRadius = 6.0f;

        public static float mGravity = 0.05f;

        /// <summary>PhysObj::mDefaultGravity (gravity is reset to this between levels).</summary>
        public static float mDefaultGravity = 0.05f;

        public int mStuckCounter;                                 // 0x9c

        public new int mId;                                       // 0xa0

        public float mX;                                          // 0xa4

        public float mY;                                          // 0xa8

        public float mPrevX;                                      // 0xac

        public float mPrevY;                                      // 0xb0

        public float mVelX;                                       // 0xb4

        public float mVelY;                                       // 0xb8

        public float mHatX;                                       // 0xbc

        public float mHatY;                                       // 0xc0

        /// <summary>Stuck-check samples; index 5 aliases mDrawX/mDrawY as in the original layout.</summary>
        public SexyVector2[] mTrail = new SexyVector2[5];         // 0xc4

        public float mDrawX;                                      // 0xec

        public float mDrawY;                                      // 0xf0

        public float mRadius;                                     // 0xf4

        public bool mIsPeg;                                       // 0xf8

        public bool mInCollideUpdate;                             // 0xf9

        public bool mHasCollided;                                 // 0xfa

        public bool mFlagFC;                                      // 0xfc

        public bool mFlagFD;                                      // 0xfd

        public bool mFlagFE;                                      // 0xfe

        public int mBounceCount;                                  // 0x100

        public int mCount104;                                     // 0x104

        public int mHoleOutCounter;                               // 0x108

        public int mCount10C;                                     // 0x10c

        public int mValue110;                                     // 0x110

        public int mValue114;                                     // 0x114

        public PhysObj mHoleOutObj;                               // 0x118

        public bool mFlag11C;                                     // 0x11c

        public bool mFireball;                                    // 0x11d

        public bool mElectrified;                                 // 0x11e

        public LightningSparkEffect mSparks;                      // 0x120

        public bool mFlag124;                                     // 0x124

        public bool mPlaySounds;                                  // 0x125

        public bool mJiggled;                                     // 0x126

        public bool mCollidedThisFrame;                           // 0x127

        public SexyVector2 mLastNormal;                           // 0x128

        public float mImpactSpeed;                                // 0x130

        public float mSparkDist;                                   // 0x134 (fever spark trail distance)

        public int mValue138;                                     // 0x138

        public int mValue13C;                                     // 0x13c

        public int mValue140;                                     // 0x140

        public int mValue144;                                     // 0x144

        public int mValue148;                                     // 0x148

        public SoundInstance mFireballSound;                      // 0x14c

        public SoundInstance mLightningSound;                     // 0x150

        public Ball mHatBall;                                     // 0x154

        public bool mIsHatBall;                                   // 0x158

        public bool mHasHat;                                      // 0x159

        public int mLightningSoundDelay;                          // 0x15c

        public Ball(bool isPeg)
        {
            mId = mSortId;
            mShapeType = isPeg ? SHAPE_CIRCLE : SHAPE_BALL;
            mIsPeg = isPeg;
            mStuckCounter = 0;
            mRadius = mDefRadius;
            mPrevY = 0;
            mPrevX = 0;
            mHoleOutCounter = 0;
            mCount10C = 0;
            mValue110 = 0;
            mValue114 = -1000;
            mSparks = null;
            mBounceCount = 1000000;
            mCount104 = 1000000;
            mFlag11C = false;
            mPlaySounds = true;
            mFireball = false;
            mElectrified = false;
            mJiggled = false;
            mCollidedThisFrame = false;
            mHasCollided = false;
            mFlagFB = false;
            mFlagFC = false;
            mFlagFD = false;
            mFlagFE = false;
            mIsHatBall = false;
            mHasHat = false;
            mFlag124 = false;
            mImpactSpeed = 0;
            mSparkDist = 0;
            mValue138 = 0;
            mValue13C = 0;
            mValue140 = 0;
            mValue144 = 0;
            mLightningSoundDelay = 0;
            mValue148 = 10000;
            mFireballSound = null;
            SetPos(0, 0);
            SetVelocity(0, 0);
        }

        public bool mFlagFB;                                      // 0xfb

        public float GetX()
        {
            return mX;
        }

        public float GetY()
        {
            return mY;
        }

        public float GetRadius()
        {
            return mRadius;
        }

        private SexyVector2 GetTrail(int theIdx)
        {
            return theIdx == 5 ? new SexyVector2(mDrawX, mDrawY) : mTrail[theIdx];
        }

        private void SetTrail(int theIdx, SexyVector2 theValue)
        {
            if (theIdx == 5)
            {
                mDrawX = theValue.x;
                mDrawY = theValue.y;
            }
            else
            {
                mTrail[theIdx] = theValue;
            }
        }

        public void StopFireballSound()
        {
            Board board = GetBoard();
            if (mFireballSound != null && board != null)
            {
#if PEGGLE_IPHONE
                if (!board.mSavingInBackground)
                {
                    board.mSoundMgr.AddFadeSound(mFireballSound);
                }
#else
                board.mSoundMgr.AddFadeSound(mFireballSound);
#endif
                mFireballSound = null;
            }
        }

        public void StopLightningSound()
        {
            Board board = GetBoard();
            if (mLightningSound != null && board != null)
            {
#if PEGGLE_IPHONE
                if (!board.mSavingInBackground)
                {
                    board.mSoundMgr.AddFadeSound(mLightningSound);
                }
#else
                board.mSoundMgr.AddFadeSound(mLightningSound);
#endif
                mLightningSound = null;
            }
            mLightningSoundDelay = 0;
        }

        public override void SyncState(DataSync theSync)
        {
            base.SyncState(theSync);
            bool reading = theSync.GetReader() != null;
            if (reading)
            {
                mBounceCount = 1000000;
                mCount104 = 1000000;
                mFlag11C = false;
                mHoleOutCounter = 0;
                mCount10C = 0;
                mValue114 = -1000;
                mValue110 = 0;
                mHoleOutObj = null;
                mHasCollided = false;
                mFlagFB = false;
                mImpactSpeed = 0;
            }
            bool hasPos = mMover == null;
            bool hasBounceCount = mBounceCount < 1000000;
            bool hasCount104 = mCount104 < 1000000;
            bool hasHoleOut = mHoleOutCounter != 0;
            bool hasHoleOutObj = mHoleOutObj != null;
            bool hasCount10C = mCount10C != 0;
            bool hasValue114 = mValue114 != -1000;
            bool hasValue110 = mValue110 != 0;
            theSync.SyncBoolBit(ref mIsPeg);
            theSync.SyncBoolBit(ref hasPos);
            theSync.SyncBoolBit(ref hasBounceCount);
            theSync.SyncBoolBit(ref mFlag11C);
            theSync.SyncBoolBit(ref mPlaySounds);
            theSync.SyncBoolBit(ref hasHoleOut);
            theSync.SyncBoolBit(ref hasHoleOutObj);
            theSync.SyncBoolBit(ref mFireball);
            theSync.EndBit();
            bool newFormat = (reading && mSyncType == 1 && theSync.GetVersion() > 0x51) || theSync.GetVersion() > 0x54;
            if (newFormat)
            {
                theSync.SyncBoolBit(ref hasCount104);
                theSync.EndBit();
            }
            if (!hasPos)
            {
                if (mMover == null && reading)
                {
                    throw new DataReaderException();
                }
                mX = mMover.mX;
                mY = mMover.mY;
            }
            else
            {
                theSync.SyncFloat(ref mX);
                theSync.SyncFloat(ref mY);
            }
            theSync.SyncFloat(ref mRadius);
            if (mShapeType == SHAPE_BALL)
            {
                theSync.SyncFloat(ref mPrevX);
                theSync.SyncFloat(ref mPrevY);
                theSync.SyncFloat(ref mVelX);
                theSync.SyncFloat(ref mVelY);
                theSync.SyncFloat(ref mDrawX);
                theSync.SyncFloat(ref mDrawY);
                theSync.SyncLong(ref mStuckCounter);
                theSync.SyncFloat(ref mTrail[0].x);
                theSync.SyncFloat(ref mTrail[0].y);
                if (theSync.GetVersion() > 0xb)
                {
                    for (int i = 1; i < 5; i++)
                    {
                        theSync.SyncFloat(ref mTrail[i].x);
                        theSync.SyncFloat(ref mTrail[i].y);
                    }
                }
                if (theSync.GetVersion() > 0xc)
                {
                    theSync.SyncBoolBit(ref mJiggled);
                    theSync.SyncBoolBit(ref mCollidedThisFrame);
                    theSync.SyncBoolBit(ref hasCount10C);
                    theSync.SyncBoolBit(ref hasValue114);
                    theSync.SyncBoolBit(ref hasValue110);
                    theSync.SyncBoolBit(ref mHasCollided);
                    theSync.SyncBoolBit(ref mFlagFC);
                    theSync.SyncBoolBit(ref mFlagFD);
                    theSync.EndBit();
                }
                if (theSync.GetVersion() > 0x17)
                {
                    theSync.SyncLong(ref mId);
                }
                bool hasValue138 = mValue138 != 0;
                if (theSync.GetVersion() > 0x1d)
                {
                    theSync.SyncBoolBit(ref mFlagFE);
                    theSync.SyncBoolBit(ref mHasHat);
                    theSync.SyncBoolBit(ref mIsHatBall);
                    theSync.SyncBoolBit(ref hasValue138);
                    theSync.SyncBoolBit(ref mFlag124);
                    if (theSync.GetVersion() > 0x54)
                    {
                        theSync.SyncBoolBit(ref mElectrified);
                    }
                    theSync.EndBit();
                }
                if (hasBounceCount)
                {
                    theSync.SyncLong(ref mBounceCount);
                }
                if (newFormat && hasCount104)
                {
                    theSync.SyncLong(ref mCount104);
                }
                if (hasHoleOut)
                {
                    theSync.SyncLong(ref mHoleOutCounter);
                }
                if (hasHoleOutObj)
                {
                    mHoleOutObj = SyncPhysObj(theSync, mHoleOutObj);
                }
                if (hasCount10C)
                {
                    theSync.SyncLong(ref mCount10C);
                }
                if (hasValue114)
                {
                    theSync.SyncLong(ref mValue114);
                }
                if (hasValue110)
                {
                    theSync.SyncLong(ref mValue110);
                }
                if (hasValue138)
                {
                    theSync.SyncLong(ref mValue138);
                    theSync.SyncLong(ref mValue13C);
                    if (theSync.GetVersion() > 0x3e)
                    {
                        theSync.SyncLong(ref mValue148);
                    }
                }
                if (mHasHat)
                {
                    mHatBall = theSync.SyncRefCount(mHatBall, () => new Ball(false));
                }
                if (theSync.GetVersion() > 0x4a)
                {
                    theSync.SyncLong(ref mValue140);
                    theSync.SyncLong(ref mValue144);
                }
            }
            if (reading)
            {
                mMinX = mX - mRadius;
                mMaxX = mX + mRadius;
                mMinY = mY - mRadius;
                mMaxY = mY + mRadius;
                mHatX = mX;
                mHatY = mY;
                if (mFireball)
                {
                    StartFireballSound();
                }
                if (mElectrified)
                {
                    StartLightningSound();
                }
            }
        }

        public void StartFireballSound()
        {
            Board board = GetBoard();
            if (board == null)
            {
                return;
            }
#if PEGGLE_IPHONE
            // (The iPhone plays the loops in replays too.)
            if (true)
#else
            if (board.mApp.mGameMode != GameMode.GAMEMODE_REPLAY && !board.mLogicMgr.mMuteLoops)
#endif
            {
                if (mFireballSound == null && mPlaySounds)
                {
                    mFireballSound = board.mApp.mSoundManager.GetSoundInstance((uint)Res.SOUND_FIREBALLLOOP);
                }
                if (mFireballSound != null && !mFireballSound.IsPlaying())
                {
                    mFireballSound.Play(true);
                }
            }
        }

        public void StartLightningSound()
        {
            Board board = GetBoard();
            if (board == null)
            {
                return;
            }
#if PEGGLE_IPHONE
            // (The iPhone plays the loops in replays too.)
            if (true)
#else
            if (board.mApp.mGameMode != GameMode.GAMEMODE_REPLAY && !board.mLogicMgr.mMuteLoops)
#endif
            {
                if (mLightningSound == null && mPlaySounds)
                {
                    mLightningSound = board.mApp.mSoundManager.GetSoundInstance((uint)Res.SOUND_LIGHTNING_LOOP);
                }
                if (mLightningSound != null && !mLightningSound.IsPlaying())
                {
                    mLightningSound.Play(true, true);
                }
            }
        }

        /// <summary>Teleports without leaving a motion trail: previous/draw/hat positions all jump too.</summary>
        public void SetAbsPos(float theX, float theY)
        {
            SetPos(theX, theY);
            mPrevX = mX;
            mPrevY = mY;
            mDrawX = mX;
            mDrawY = mY;
            mHatX = mX;
            mHatY = mY;
        }

        /// <summary>Hides the ball for <paramref name="theDelay"/> frames before it re-emerges at <paramref name="theObj"/>.</summary>
        public void SetHoleOut(PhysObj theObj, int theDelay)
        {
            if (theDelay < 1)
            {
                SetAbsPos(theObj.GetXPos(), theObj.GetYPos());
            }
            else
            {
                SetActive(false);
                mHoleOutCounter = theDelay;
                mHoleOutObj = theObj;
            }
        }

        public override void AddedToGame()
        {
            base.AddedToGame();
            mPrevX = mX;
            mPrevY = mY;
            mDrawX = mX;
            mDrawY = mY;
        }

        public override SexyVector2 GetCenter()
        {
            return new SexyVector2(mX, mY);
        }

        public override void SetPos(float theX, float theY)
        {
            mX = theX;
            mY = theY;
            mMinX = mX - mRadius;
            mMaxX = mX + mRadius;
            mMinY = mY - mRadius;
            mMaxY = mY + mRadius;
        }

        public override void SetVelocity(float theVX, float theVY)
        {
            mVelX = theVX;
            mVelY = theVY;
        }

        public override void Translate(float theDX, float theDY)
        {
            mX += theDX;
            mY += theDY;
            mMinX += theDX;
            mMaxX += theDX;
            mMinY += theDY;
            mMaxY += theDY;
        }

        public void SetRadius(float theRadius)
        {
            mRadius = theRadius;
            SyncBounds();
        }

        public void SyncBounds()
        {
            mMinX = mX - mRadius;
            mMaxX = mX + mRadius;
            mMinY = mY - mRadius;
            mMaxY = mY + mRadius;
        }

        public void SetFireball(bool on)
        {
            mFireball = on;
            if (on)
            {
                SetFireballRadius();
                StartFireballSound();
            }
            else
            {
                StopFireballSound();
                mRadius = mDefRadius;
            }
        }

        public void SetFireballRadius()
        {
            mRadius = 10.0f;
        }

        public void SetElectrified(bool on, int theSoundDelay)
        {
            mElectrified = on;
            if (on)
            {
                mLightningSoundDelay = theSoundDelay;
                if (mLightningSoundDelay == 0)
                {
                    StartLightningSound();
                }
            }
            else
            {
                StopLightningSound();
            }
        }

        /// <summary>
        /// Gives the ball the magic hat (Lord of the Hats power): a big invisible hat ball that
        /// follows above it and catches pegs too.
        /// </summary>
        public void SetHat(bool on, bool withHatBall)
        {
            mHasHat = on;
            if (withHatBall && on && mHatBall == null)
            {
                mHatBall = new Ball(false);
                mHatBall.mIsHatBall = true;
                mHatBall.mFireball = true;
                mHatBall.mCollidable = false;
                mHatBall.mRadius = 20.0f;
                GetBoard()?.AddObj(mHatBall);
            }
            if (!on)
            {
                if (mHatBall != null)
                {
                    Board board = GetBoard();
                    if (board != null)
                    {
                        board.RemoveObj(mHatBall);
                        mHatBall = null;
                    }
                }
            }
            else
            {
                const float swing = 200.0f;
                mHatX = mX <= 325.0f ? mX + swing : mX - swing;
                mHatY = mY - swing;
            }
        }

        public bool CheckBallCollision(Ball theBall)
        {
            float r1 = mRadius;
            float r2 = theBall.mRadius;
            float dx = mX - theBall.mX;
            float dy = mY - theBall.mY;
            float r = (r1 + r2) - 0.01f;
            bool hit = dx * dx + dy * dy <= r * r;
            if (hit)
            {
                DoBallCollision(theBall);
            }
            return hit;
        }

        public void DoBallCollision(Ball theBall)
        {
            if (!mIsPeg)
            {
                if (!theBall.mIsPeg)
                {
                    DoBallBallCollision(theBall);
                }
                else
                {
                    DoBallPegCollision(theBall);
                }
            }
            else if (!theBall.mIsPeg)
            {
                theBall.DoBallPegCollision(this);
            }
        }

        /// <summary>Swept circle-vs-peg test (called on the peg) for a ball at thePos moving by theVel.</summary>
        public bool CheckCollision(SexyVector2 thePos, SexyVector2 theVel, float theRadius, ref float theTime, ref SexyVector2 theHitPos)
        {
            float r = theRadius + (mRadius - 0.01f);
            float rsq = r * r;
            SexyVector2 c = new SexyVector2(mX, mY);
            SexyVector2 d = thePos - c;
            if (d.Dot(d) < rsq)
            {
                theTime = 0.0f;
                theHitPos = thePos;
                return true;
            }
            float a = theVel.Dot(theVel);
            float b = d.Dot(theVel) * 2.0f;
            float cc = (thePos.Dot(thePos) + c.Dot(c)) - thePos.Dot(c) * 2.0f - rsq;
            float disc = b * b - a * 4.0f * cc;
            if (disc < 0.0f)
            {
                return false;
            }
            disc = (float)Math.Sqrt(disc);
            float t = ((-0.0f - b) - disc) / (a * 2.0f);
            if (t < 0.0f)
            {
                t = ((-0.0f - b) + disc) / (a * 2.0f);
            }
            if (t < 0.0f || !(t < theTime))
            {
                return false;
            }
            theTime = t;
            theHitPos = thePos + theVel * t;
            return true;
        }

        /// <summary>Equal-mass bounce between two game balls, then separates them evenly.</summary>
        public void DoBallBallCollision(Ball theBall)
        {
            SexyVector2 n = new SexyVector2(theBall.mX - mX, theBall.mY - mY).Normalize();
            SexyVector2 v1 = new SexyVector2(mVelX, mVelY);
            SexyVector2 v2 = new SexyVector2(theBall.mVelX, theBall.mVelY);
            float m1 = 1.0f;
            float m2 = 1.0f;
            float j = n.Dot(v1 - v2) * 0.95f;
            j *= 2.0f;
            theBall.mImpactSpeed = Math.Abs(j);
            mImpactSpeed = Math.Abs(j);
            float a = (m1 * j) / (m1 + m2);
            float b = ((-0.0f - a) * m2) / m1;
            SexyVector2 nv1 = v1 + n * b;
            SexyVector2 nv2 = v2 + n * a;
            float r1 = mRadius;
            float r2 = theBall.mRadius;
            theBall.mVelX = nv2.x;
            theBall.mVelY = nv2.y;
            mVelX = nv1.x;
            mVelY = nv1.y;
            float dx = mX - theBall.mX;
            float dy = mY - theBall.mY;
            float overlap = (r1 + r2) - (float)Math.Sqrt(dx * dx + dy * dy);
            if (overlap > 0.0f)
            {
                SexyVector2 off = n * overlap / 2.0f;
                theBall.Translate(off.x, off.y);
                Translate(-0.0f - off.x, -0.0f - off.y);
            }
            mNotifyCollisionFunc?.Invoke(this, theBall);
        }

        /// <summary>Bounce off a peg treated as an immovable mass; fireballs pass through pegs.</summary>
        public void DoBallPegCollision(Ball thePeg)
        {
            float dx = mX - thePeg.mX;
            float dy = mY - thePeg.mY;
            float distSq = dx * dx + dy * dy;
            float r1 = mRadius;
            float r2 = thePeg.mRadius;
            SexyVector2 n = new SexyVector2(thePeg.mX - mX, thePeg.mY - mY).Normalize();
            SexyVector2 v1 = new SexyVector2(mVelX, mVelY);
            SexyVector2 v2 = new SexyVector2(thePeg.mVelX, thePeg.mVelY);
            mLastNormal = -n;
            float m1 = 1.0f;
            float m2 = 1e12f;
            float bounce = thePeg.mBouncy;
            float rolly = thePeg.mRolly;
            if (bounce == 0.0f)
            {
                bounce = 0.9f;
            }
            if (rolly == 0.0f)
            {
                rolly = 1.0f;
            }
            float j = n.Dot(v1 - v2) * bounce;
            if (Math.Abs((double)j) > rolly)
            {
                j *= 2.0f;
            }
            else
            {
                mBounceCount = 0;
            }
            mImpactSpeed = Math.Abs(j);
            if (!mFireball || thePeg.mPegInfo == null)
            {
                float a = (m1 * j) / (m1 + m2);
                float b = ((-0.0f - a) * m2) / m1;
                SexyVector2 nv1 = v1 + n * b;
                mVelX = nv1.x;
                mVelY = nv1.y;
                float overlap = (r1 + r2) - (float)Math.Sqrt(distSq);
                SexyVector2 push = n * overlap;
                Translate(-0.0f - push.x, -0.0f - push.y);
                if (thePeg.mBallStopReset)
                {
                    mStuckCounter = 0;
                }
            }
            mNotifyCollisionFunc?.Invoke(this, thePeg);
        }

        /// <summary>
        /// Response to hitting a line or polygon edge: bounce (reflect, scale by the surface's
        /// bounciness, add its push) or, for slow impacts, slide along it.
        /// </summary>
        public void DoLineCollision(ref SexyVector2 theVel, PhysObj theObj, SexyVector2 theNormal, SexyVector2 theHitPos,
            SexyVector2 theHitVel, ref SexyVector2 theSpinVel)
        {
            mLastNormal = theNormal;
            if (mFireball && theObj.mPegInfo != null)
            {
                mNotifyCollisionFunc?.Invoke(this, theObj);
                return;
            }
            SetPos(theHitPos.x, theHitPos.y);
            SexyVector2 rel = theVel - theHitVel - theSpinVel;
            float vn = -0.0f - rel.Dot(theNormal);
            if (vn < 0.0f)
            {
                mImpactSpeed = 0.0f;
                return;
            }
            mImpactSpeed = vn;
            float bounce = theObj.mBouncy;
            float rolly = theObj.mRolly;
            if (bounce == 0.0f)
            {
                bounce = 0.82f;
            }
            if (rolly == 0.0f)
            {
                rolly = 1.4f;
            }
            if (Math.Abs((double)theNormal.x) > 0.99)
            {
                vn *= 5.0f;
            }
            if (vn > rolly)
            {
                if (theVel.Dot(theNormal) < 0.0f)
                {
                    SexyVector2 proj = theNormal * theNormal.Dot(theVel);
                    theVel = (theVel - proj * 2.0f) * bounce;
                }
                SexyVector2 push = (theHitVel + theSpinVel) * (bounce + 1.0f);
                if (push.Dot(theNormal) > 0.0f)
                {
                    Translate(push.x, push.y);
                }
                theVel = theVel + push;
                mVelX = theVel.x;
                mVelY = theVel.y;
                float maxVel = theObj.mMaxBounceVelocity;
                if (maxVel > 0.0f && maxVel * maxVel < mVelX * mVelX + mVelY * mVelY)
                {
                    SexyVector2 v = new SexyVector2(mVelX, mVelY).Normalize() * maxVel;
                    mVelX = v.x;
                    mVelY = v.y;
                }
            }
            else
            {
                mBounceCount = 0;
                SexyVector2 tangent = theVel - theNormal * theNormal.Dot(theVel);
                if (theSpinVel.Dot(theNormal) > 0.0f)
                {
                    theSpinVel = theNormal * theSpinVel.Dot(theNormal);
                }
                else
                {
                    theSpinVel = new SexyVector2(0.0f, 0.0f);
                }
                mVelX = tangent.x + theHitVel.x + theSpinVel.x;
                mVelY = tangent.y + theHitVel.y + theSpinVel.y;
            }
            if (theObj.mBallStopReset)
            {
                mStuckCounter = 0;
            }
            mNotifyCollisionFunc?.Invoke(this, theObj);
        }

        /// <summary>
        /// Moves the ball through one frame against <paramref name="theObjs"/>, resolving up to
        /// 20 contacts by consuming the remaining fraction of the step after each.
        /// </summary>
        public bool DoCollideUpdate(List<PhysObj> theObjs, SexyVector2[] theFirstHitPos)
        {
            if (mIsPeg || (!mCollidable && !mIsHatBall))
            {
                return false;
            }
            bool collided = false;
            mInCollideUpdate = true;
            float remaining = 1.0f;
            float t = 1.0f;
            Line.mAllowKissCollision = true;
            for (int iter = 0; iter < 0x14; iter++)
            {
                PhysObj hitObj = null;
                SexyVector2 pos = new SexyVector2(mX, mY);
                SexyVector2 vel = new SexyVector2(mVelX, mVelY);
                SexyVector2 hitPos = new SexyVector2();
                SexyVector2 hitVel = new SexyVector2();
                SexyVector2 spinVel = new SexyVector2();
                SexyVector2 normal = new SexyVector2();
                for (int i = 0; i < theObjs.Count; i++)
                {
                    PhysObj obj = theObjs[i];
                    if (obj.mShapeType == SHAPE_LINE)
                    {
                        if (((Line)obj).CheckCollision(pos, vel, mRadius, ref t, ref hitPos, ref normal, ref hitVel, true))
                        {
                            spinVel = new SexyVector2(0.0f, 0.0f);
                            hitObj = obj;
                        }
                    }
                    else if (obj.mShapeType == SHAPE_POLY)
                    {
                        if (((Poly)obj).CheckCollision(pos, vel, mRadius, ref t, ref hitPos, ref normal, ref hitVel, ref spinVel))
                        {
                            hitObj = obj;
                        }
                    }
                    else if (obj.mShapeType == SHAPE_CIRCLE)
                    {
                        if (((Ball)obj).CheckCollision(pos, vel, mRadius, ref t, ref hitPos))
                        {
                            hitObj = obj;
                        }
                    }
                }
                if (hitObj == null)
                {
                    mX += mVelX * remaining;
                    mY += mVelY * remaining;
                    break;
                }
                if (!mFireball || hitObj.mPegInfo == null)
                {
                    mHasCollided = true;
                    mCollidedThisFrame = true;
                    collided = true;
                    if (theFirstHitPos != null && iter == 0)
                    {
                        theFirstHitPos[0] = hitPos;
                    }
                    if (hitObj.mShapeType == SHAPE_CIRCLE)
                    {
                        SetPos(hitPos.x, hitPos.y);
                        DoBallPegCollision((Ball)hitObj);
                    }
                    else
                    {
                        DoLineCollision(ref vel, hitObj, normal, hitPos, hitVel, ref spinVel);
                    }
                    mDrawX = mX;
                    mDrawY = mY;
                    t = remaining - t;
                    remaining = t;
                }
                else
                {
                    mNotifyCollisionFunc?.Invoke(this, hitObj);
                    theObjs.Remove(hitObj);
                }
            }
            return collided;
        }

        /// <summary>
        /// Will this ball, moving at its velocity, touch theObj (polys and pegs only) within theMaxTime
        /// velocity steps, with theExtra added to its radius?
        /// </summary>
        public bool CheckCollision(PhysObj theObj, float theMaxTime, float theExtra)
        {
            SexyVector2 pos = new SexyVector2(mX, mY);
            SexyVector2 vel = new SexyVector2(mVelX, mVelY);
            SexyVector2 hitPos = new SexyVector2();
            SexyVector2 normal = new SexyVector2();
            SexyVector2 hitVel = new SexyVector2();
            SexyVector2 spinVel = new SexyVector2();
            float radius = mRadius + theExtra;
            float t = theMaxTime;
            if (theObj.mShapeType == SHAPE_POLY)
            {
                if (((Poly)theObj).CheckCollision(pos, vel, radius, ref t, ref hitPos, ref normal, ref hitVel, ref spinVel))
                {
                    return true;
                }
            }
            else if (theObj.mShapeType == SHAPE_CIRCLE && ((Ball)theObj).CheckCollision(pos, vel, radius, ref t, ref hitPos))
            {
                return true;
            }
            return false;
        }

        public void DoNonCollideUpdate()
        {
            Translate(mVelX, mVelY);
        }

        public override bool IsCollidableWith(PhysObj theObj)
        {
            if (mIsHatBall)
            {
                if (mMoveUpdateCnt < 0xb)
                {
                    return false;
                }
                return theObj.mPegInfo != null && !theObj.mPegInfo.mHit && theObj.mCollidable;
            }
            if (mIsPeg && theObj.mShapeType != SHAPE_BALL)
            {
                return false;
            }
            return mCollidable && theObj.IsCollidable();
        }

        public void UpdateLightningSound()
        {
            if (mLightningSoundDelay > 0)
            {
                mLightningSoundDelay--;
                if (mLightningSoundDelay == 0)
                {
                    StartLightningSound();
                }
            }
        }

        public void UpdateEffects()
        {
            UpdateLightningSound();
            if (mElectrified)
            {
                if (mSparks == null)
                {
                    mSparks = new LightningSparkEffect();
                }
                mSparks.Update(true);
            }
            else if (mSparks != null)
            {
                if (mSparks.GetSparkCount() < 1)
                {
                    mSparks = null;
                }
                else
                {
                    mSparks.Update(false);
                }
            }
        }

        /// <summary>
        /// Per-frame step before collision: stuck detection (samples every 10 frames), hole-out
        /// countdown, gravity, hat following, speed cap (15) and swept bounds.
        /// </summary>
        public override void Update()
        {
            UpdateEffects();
            if (mIsHatBall)
            {
                mMoveUpdateCnt++;
                return;
            }
            if (mIsPeg)
            {
                mPrevX = mX;
                mPrevY = mY;
                base.Update();
                mInCollideUpdate = true;
                return;
            }
            base.Update();
            mBounceCount++;
            if (mCount10C != 0)
            {
                mCount10C--;
                if (mCount10C == 0)
                {
                    mValue110 = 0;
                }
            }
            mStuckCounter++;
            if (mStuckCounter % 10 == 0)
            {
                int idx = Math.Clamp(mStuckCounter / 10, 1, 6) - 1;
                SetTrail(idx, new SexyVector2(mX, mY));
                bool jiggled = mJiggled;
                mJiggled = false;
                for (int i = 0; i < idx; i++)
                {
                    SexyVector2 p = GetTrail(i);
                    float d = (mX - p.x) * (mX - p.x) + (mY - p.y) * (mY - p.y);
                    if (d >= 4.0f)
                    {
                        mStuckCounter = 0;
                        break;
                    }
                    if (d >= 0.040000003f && !jiggled)
                    {
                        jiggled = true;
                        mJiggled = true;
                        mStuckCounter -= 5;
                    }
                }
            }
            mPrevX = mX;
            mPrevY = mY;
            if (mHoleOutCounter > 0)
            {
                mStuckCounter = 0;
                mHoleOutCounter--;
                if (mHoleOutCounter != 0)
                {
                    mInCollideUpdate = true;
                    return;
                }
                SetActive(true);
                if (mHoleOutObj != null)
                {
                    SetAbsPos(mHoleOutObj.GetXPos(), mHoleOutObj.GetYPos());
                    mHoleOutObj = null;
                }
            }
            mVelY += mGravity;
            if (mHasHat)
            {
                if (mY + 2.0f < mHatY)
                {
                    mHatY = mY + 2.0f;
                }
                // The hat closes a quarter of the gap each update (the original computes the magnitude
                // but divides by 4.0).
                SexyVector2 d = new SexyVector2(mX - mHatX, mY - mHatY);
                d = d / 4.0f;
                mHatX += d.x;
                mHatY += d.y;
                mHatBall?.SetPos(mHatX + 0.0f, mHatY - 21.0f);
            }
            const float maxSpeed = 15.0f;
            float vx = mVelX;
            float vy = mVelY;
            if (vx * vx + vy * vy > 225.0f)
            {
                SexyVector2 v = new SexyVector2(vx, vy).Normalize() * maxSpeed;
                mVelX = v.x;
                vx = v.x;
                mVelY = v.y;
                vy = v.y;
            }
            mMinX = (mX - mRadius) + (vx < 0.0f ? vx : 0.0f);
            mMaxX = mX + mRadius + (vx > 0.0f ? vx : 0.0f);
            mMinY = (mY - mRadius) + (vy < 0.0f ? vy : 0.0f);
            mMaxY = mY + mRadius + (vy > 0.0f ? vy : 0.0f);
            mDrawX = mX;
            mDrawY = mY;
            mInCollideUpdate = false;
            mHasCollided = false;
        }

        /// <summary>Interpolates the drawn position between frames during slow motion.</summary>
        public override void UpdateSlowMo(float theFraction)
        {
            base.UpdateSlowMo(theFraction);
            if (!mIsPeg)
            {
                mDrawX = mPrevX * (1.0f - theFraction) + mX * theFraction;
                mDrawY = mPrevY * (1.0f - theFraction) + mY * theFraction;
            }
        }

        public override void DrawShadow(Graphics g)
        {
        }

        public void DrawHat(Graphics g)
        {
            float hx = (mDrawX + mHatX) - mX;
            float hy = (mDrawY + mHatY) - mY;
            float rot = Math.Clamp(mVelX / 3.0f, -1.0f, 1.0f) * 0.7853982f;
            Image image = mHatBall != null ? Res.IMAGE_MAGICHAT : Res.IMAGE_HAT;
            float scale = SexyUtil.gPhysicalDisplayHeight;
            float x = (float)((hx * scale) / 600.0 - image.mWidth / 2);
            float y = (float)((hy * scale) / 600.0 - image.mHeight / 2 - 20.0);
            g.DrawImageRotatedF(image, x, y, rot);
        }

        public override void Draw(Graphics g)
        {
            float scale = SexyUtil.gPhysicalDisplayHeight;
            if (mShapeType == SHAPE_CIRCLE)
            {
                if (mPegInfo == null)
                {
                    if (mImage != null)
                    {
                        DrawImage(g, mX, mY, 0.0f, g.Is3D(), false);
                    }
                }
                else
                {
                    mPegInfo.DrawBall(this, g, false);
                }
                return;
            }
            if (mIsHatBall)
            {
                return;
            }
            float x;
            float y;
            if (!mIsPeg)
            {
                x = mDrawX;
                y = mDrawY;
            }
            else
            {
                x = mX;
                y = mY;
            }
            if (mRadius != mDefRadius)
            {
                if (mFireball)
                {
                    if (mHasHat)
                    {
                        DrawHat(g);
                    }
                    x -= mRadius + 5.0f;
                    y -= mRadius + 5.0f;
                    float rot = 0.0f;
                    Board board = GetBoard();
                    if (board != null)
                    {
                        rot = board.mGameUpdateCnt * 0.05f;
                    }
                    g.DrawImageRotatedF(Res.IMAGE_FIREBALL, (float)((x * scale) / 600.0), (float)((y * scale) / 600.0), rot);
                }
            }
            else
            {
                if (mHasHat)
                {
                    DrawHat(g);
                }
                Image ball = Res.IMAGE_BALL;
                float dx = (float)((x * scale) / 600.0 - ball.mWidth / 2);
                float dy = (float)((y * scale) / 600.0 - ball.mHeight / 2);
                bool colorize = g.GetColorizeImages();
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0, 0x60));
                g.DrawImageF(Res.IMAGE_BALLSHADOW, dx - 2.0f, dy + 2.0f);
                g.SetColorizeImages(false);
                g.DrawImageF(ball, dx, dy);
                if (mElectrified)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, 100));
                    Board board = GetBoard();
                    if (board != null)
                    {
                        Image spark = Res.IMAGE_LIGHTNING_SPARK;
                        for (int i = 0; i < 6; i++)
                        {
                            float a = (board.mGameUpdateCnt + i * 0x55b) * -0.00125f;
                            int frame = ((i * 3 + 7) * board.mGameUpdateCnt) / 0x32 % 0x18;
                            if (frame < 4)
                            {
                                TRect cel = spark.GetCelRect(frame);
                                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                                g.SetColor(SexyUtil.MakeColor(0xffffff, 200));
                                double sx = (x * scale) / 600.0 - cel.mWidth / 2 + (scale * 7) / 600.0 + Math.Sin(a) * 9.0;
                                double sy = (y * scale) / 600.0 - cel.mHeight / 2 + (scale * 7) / 600.0 + Math.Cos(a) * 9.0;
                                g.DrawImageRotatedF(spark, (float)sx, (float)sy, a, cel);
                                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                            }
                        }
                    }
                    g.SetColorizeImages(colorize);
                }
            }
            if (mSparks != null)
            {
                mSparks.Draw(g, (int)(mDrawX + 0.0f), (int)(mDrawY + 0.0f));
                mSparks.SetScale(mFireball ? 0.65f : 0.4f);
            }
        }

        public override int GetClass()
        {
            return mShapeType != SHAPE_BALL ? CLASS_CIRCLE : CLASS_BALL;
        }

        public override float GetXPos()
        {
            return mX;
        }

        public override float GetYPos()
        {
            return mY;
        }
    }
}
