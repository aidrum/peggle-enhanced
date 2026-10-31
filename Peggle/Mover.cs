using System;

namespace Sexy
{
    /// <summary>
    /// Base of the level objects' editable/serialisable state (Sexy::EditValObj). The original
    /// binds fields to level-editor values; the editor is not part of the shipped game, so only
    /// the serialisation contract remains.
    /// </summary>
    public abstract class EditValObj : ISyncable
    {
        /// <summary>Level-editor value block; always null outside the editor.</summary>
        public object mEditVal;                                   // 0x08

        public object GetEditVal()
        {
            return mEditVal;
        }

        public abstract void SyncState(DataSync theSync);
    }

    /// <summary>
    /// Movement path of a level object (Sexy::Mover): circles, ellipses, figure-eights, pendulums,
    /// rotations and linear sweeps around an anchor, with optional pauses and a sub-mover that the
    /// anchor itself follows.
    /// </summary>
    public class Mover : EditValObj
    {
        public const int MOVE_NONE = 0;

        public const int MOVE_VERTICAL = 1;

        public const int MOVE_HORIZONTAL = 2;

        public const int MOVE_CIRCLE = 3;

        public const int MOVE_HORIZONTAL_INFINITY = 4;

        public const int MOVE_VERTICAL_INFINITY = 5;

        public const int MOVE_HORIZONTAL_ARC = 6;

        public const int MOVE_VERTICAL_ARC = 7;

        public const int MOVE_ROTATE = 8;

        public const int MOVE_ROTATE_BACK_AND_FORTH = 9;

        public const int MOVE_VERTICAL_WRAP = 11;

        public const int MOVE_HORIZONTAL_WRAP = 12;

        public const int MOVE_ROTATE_AROUND_CIRCLE = 13;

        public const int MOVE_ROTATE_AROUND_CIRCLE_BACK_AND_FORTH = 14;

        public const int MOVE_TORNADO = 15;

        public int mType;                                         // 0x10 (sign = direction)

        public int mTimeOffset;                                   // 0x14

        public int mRadius1;                                      // 0x18

        public int mPeriod;                                       // 0x1c

        public float mPhase;                                      // 0x20

        public float mMoveRotation;                               // 0x24

        public int mRadius2;                                      // 0x28

        public int mPause1;                                       // 0x2c

        public int mPhase1;                                       // 0x30 (percent)

        public int mPause2;                                       // 0x34

        public int mPhase2;                                       // 0x38 (percent)

        public float mMaxAngle;                                   // 0x3c

        public float mPostDelayPhase;                             // 0x40

        public float mBaseRotation;                               // 0x44

        public Mover mSubMover;                                   // 0x48

        public float mSubMoverX;                                  // 0x4c

        public float mSubMoverY;                                  // 0x50

        public float mX;                                          // 0x54

        public float mY;                                          // 0x58

        public float mAnchorX;                                    // 0x5c

        public float mAnchorY;                                    // 0x60

        public float mRotation;                                   // 0x64

        public Mover()
        {
            InitDefaults();
        }

        public void InitDefaults()
        {
            mType = 0;
            mTimeOffset = 0;
            mRadius1 = 0;
            mPeriod = 200;
            mPhase = 0;
            mMoveRotation = 0;
            mRadius2 = 0;
            mPause1 = 0;
            mPause2 = 0;
            mPhase1 = 0;
            mPhase2 = 0;
            mPostDelayPhase = 0;
            mMaxAngle = 0;
            mRotation = 0;
            mBaseRotation = 0;
            mY = 0;
            mX = 0;
            mAnchorY = 0;
            mAnchorX = 0;
            mSubMover = null;
        }

        public override void SyncState(DataSync theSync)
        {
            if (theSync.GetReader() != null)
            {
                InitDefaults();
            }
            theSync.SyncSByte(ref mType);
            theSync.SyncFloat(ref mAnchorX);
            theSync.SyncFloat(ref mAnchorY);
            theSync.SyncShort(ref mPeriod);
            bool hasTimeOffset = mTimeOffset != 0;
            bool hasRadius1 = mRadius1 != 0;
            bool hasPhase = mPhase != 0.0f;
            bool hasMoveRotation = mMoveRotation != 0.0f;
            bool hasRadius2 = mRadius2 != 0;
            bool hasPause1 = mPause1 != 0;
            bool hasPause2 = mPause2 != 0;
            bool hasPhase1 = mPhase1 != 0;
            bool hasPhase2 = mPhase2 != 0;
            bool hasPostDelayPhase = mPostDelayPhase != 0.0f;
            bool hasMaxAngle = mMaxAngle != 0.0f;
            bool hasRotation = mRotation != 0.0f;
            bool hasSubMover = mSubMover != null;
            bool hasPos = true;
            if (mAnchorX == mX)
            {
                hasPos = mAnchorY != mY;
            }
            bool hasBaseRotation = mBaseRotation != 0.0f;
            theSync.SyncBoolBit(ref hasTimeOffset);
            theSync.SyncBoolBit(ref hasRadius1);
            theSync.SyncBoolBit(ref hasPhase);
            theSync.SyncBoolBit(ref hasMoveRotation);
            theSync.SyncBoolBit(ref hasRadius2);
            theSync.SyncBoolBit(ref hasPause1);
            theSync.SyncBoolBit(ref hasPause2);
            theSync.SyncBoolBit(ref hasPhase1);
            theSync.EndBit();
            theSync.SyncBoolBit(ref hasPhase2);
            theSync.SyncBoolBit(ref hasPostDelayPhase);
            theSync.SyncBoolBit(ref hasMaxAngle);
            theSync.SyncBoolBit(ref hasRotation);
            theSync.SyncBoolBit(ref hasSubMover);
            theSync.SyncBoolBit(ref hasPos);
            theSync.SyncBoolBit(ref hasBaseRotation);
            theSync.EndBit();
            if (hasTimeOffset)
            {
                theSync.SyncSShort(ref mTimeOffset);
            }
            if (hasRadius1)
            {
                theSync.SyncSShort(ref mRadius1);
            }
            if (hasPhase)
            {
                theSync.SyncFloat(ref mPhase);
            }
            if (hasMoveRotation)
            {
                theSync.SyncFloat(ref mMoveRotation);
            }
            if (hasRadius2)
            {
                theSync.SyncSShort(ref mRadius2);
            }
            if (hasPause1)
            {
                theSync.SyncShort(ref mPause1);
            }
            if (hasPause2)
            {
                theSync.SyncShort(ref mPause2);
            }
            if (hasPhase1)
            {
                theSync.SyncSByte(ref mPhase1);
            }
            if (hasPhase2)
            {
                theSync.SyncSByte(ref mPhase2);
            }
            if (hasPostDelayPhase)
            {
                theSync.SyncFloat(ref mPostDelayPhase);
            }
            if (hasMaxAngle)
            {
                theSync.SyncFloat(ref mMaxAngle);
            }
            if (hasRotation)
            {
                theSync.SyncFloat(ref mRotation);
            }
            if (hasBaseRotation)
            {
                theSync.SyncFloat(ref mBaseRotation);
            }
            if (hasSubMover)
            {
                theSync.SyncFloat(ref mSubMoverX);
                theSync.SyncFloat(ref mSubMoverY);
                mSubMover = theSync.SyncRefCount(mSubMover, () => new Mover());
            }
            if (!hasPos)
            {
                mX = mAnchorX;
                mY = mAnchorY;
            }
            else
            {
                theSync.SyncFloat(ref mX);
                theSync.SyncFloat(ref mY);
            }
        }

        public void SetPos(float theX, float theY)
        {
            Translate(theX - mAnchorX, theY - mAnchorY);
        }

        public void Translate(float theDX, float theDY)
        {
            mX += theDX;
            mY += theDY;
            mAnchorX += theDX;
            mAnchorY += theDY;
        }

        /// <summary>Frames from <paramref name="theUpdateCnt"/> until the path reaches <paramref name="thePhase"/> (0-1).</summary>
        public int GetTimeTillPhase(int theUpdateCnt, float thePhase)
        {
            if (mPause1 < 1)
            {
                if (mPeriod < 1)
                {
                    return 0;
                }
                return SexyUtil.MyMod((int)(thePhase * mPeriod) - (theUpdateCnt + mTimeOffset), mPeriod);
            }
            int total = mPeriod + mPause1 + mPause2;
            if (mPeriod < 1 || total < 1)
            {
                return 0;
            }
            int target = (int)(thePhase * mPeriod);
            if (thePhase > mPhase1)
            {
                target += mPause1;
            }
            if (thePhase > mPhase2)
            {
                target += mPause2;
            }
            return SexyUtil.MyMod(target - (theUpdateCnt + mTimeOffset), total);
        }

        /// <summary>Position along the path (0-1) at <paramref name="theUpdateCnt"/>, holding still during pauses.</summary>
        public float GetMovePos(int theUpdateCnt)
        {
            float pos;
            int period;
            if (mPause1 < 1)
            {
                period = mPeriod;
                pos = (theUpdateCnt + mTimeOffset) + mPhase * period;
            }
            else
            {
                int total = mPeriod + mPause1 + mPause2;
                int t = theUpdateCnt + mTimeOffset;
                if (total > 0)
                {
                    int pause1At = (mPeriod * mPhase1) / 100;
                    int pause1 = mPause1;
                    int pause2At = pause1At + pause1 + (mPeriod * (mPhase2 - mPhase1)) / 100;
                    int pause2 = mPause2;
                    int m = (theUpdateCnt + mTimeOffset + (int)(mPostDelayPhase * total)) % total;
                    t = m;
                    if (pause1At < m)
                    {
                        t = pause1At;
                        if (pause1At + pause1 < m)
                        {
                            if (pause2At < m)
                            {
                                if (pause2At + pause2 < m)
                                {
                                    t = m - mPause1 - mPause2;
                                }
                                else
                                {
                                    t = pause2At - mPause1;
                                }
                            }
                            else
                            {
                                t = m - mPause1;
                            }
                        }
                    }
                }
                period = mPeriod;
                pos = t + mPhase * period;
            }
            int ipos = (int)pos;
            ipos -= (ipos / period) * period;
            return ipos / (float)period;
        }

        /// <summary>Computes mX/mY/mRotation for frame <paramref name="theUpdateCnt"/> plus a sub-frame offset.</summary>
        public void CalcPos(int theUpdateCnt, float theFraction)
        {
            if (mSubMover != null)
            {
                Translate(mSubMover.mX - mSubMoverX, mSubMover.mY - mSubMoverY);
                mSubMoverX = mSubMover.mX;
                mSubMoverY = mSubMover.mY;
            }
            if (mType == 0 || mPeriod < 1)
            {
                mX = mAnchorX;
                mY = mAnchorY;
                mRotation = mBaseRotation;
                return;
            }
            int dir = mType < 0 ? -1 : 1;
            float movePos = GetMovePos(theUpdateCnt) + theFraction / mPeriod;
            float radiusX = mRadius1;
            float radiusY = mRadius2 != 0 ? mRadius2 : radiusX;
            const float TWO_PI = 6.2831855f;
            switch (Math.Abs(mType))
            {
            case MOVE_VERTICAL:
                mX = mAnchorX;
                mY = mAnchorY - radiusY * (float)Math.Sin(movePos * TWO_PI * dir);
                mRotation = mBaseRotation;
                break;
            case MOVE_HORIZONTAL:
                mX = mAnchorX + radiusX * (float)Math.Cos(movePos * TWO_PI * dir);
                mY = mAnchorY;
                mRotation = mBaseRotation;
                break;
            case MOVE_CIRCLE:
            {
                float a = movePos * TWO_PI * dir;
                mX = mAnchorX + radiusX * (float)Math.Cos(a);
                mY = mAnchorY - radiusY * (float)Math.Sin(a);
                mRotation = mBaseRotation;
                break;
            }
            case MOVE_HORIZONTAL_INFINITY:
            {
                float a = movePos * TWO_PI * dir;
                mX = mAnchorX + radiusX * (float)Math.Cos(a);
                mY = mAnchorY - (radiusY * (float)Math.Sin(a * 2.0f)) / 2.0f;
                mRotation = mBaseRotation;
                break;
            }
            case MOVE_VERTICAL_INFINITY:
            {
                float a = movePos * TWO_PI * dir;
                mX = mAnchorX + radiusX * (float)Math.Sin(a * 2.0f);
                mY = mAnchorY - radiusY * (float)Math.Sin(a);
                mRotation = mBaseRotation;
                break;
            }
            case MOVE_HORIZONTAL_ARC:
            {
                float maxAngle = mMaxAngle == 0.0f ? 3.1415927f : mMaxAngle;
                float a = (maxAngle * ((float)Math.Sin(movePos * TWO_PI - 1.5707964f) + 1.0f)) / 2.0f;
                mX = mAnchorX + radiusX * (float)Math.Cos(a);
                mY = mAnchorY - radiusY * (float)Math.Sin(a);
                mRotation = mBaseRotation;
                break;
            }
            case MOVE_VERTICAL_ARC:
            {
                float maxAngle = mMaxAngle == 0.0f ? 3.1415927f : mMaxAngle;
                float a = (maxAngle * ((float)Math.Sin(movePos * TWO_PI - 1.5707964f) + 1.0f)) / 2.0f;
                mX = mAnchorX + radiusX * (float)Math.Sin(a);
                mY = mAnchorY - radiusY * (float)Math.Cos(a);
                mRotation = mBaseRotation;
                break;
            }
            case MOVE_ROTATE:
                if (mMaxAngle == 0.0f)
                {
                    mRotation = movePos * TWO_PI * dir + mBaseRotation;
                }
                else
                {
                    mRotation = mMaxAngle * dir + mBaseRotation;
                }
                mX = mAnchorX;
                mY = mAnchorY;
                break;
            case MOVE_ROTATE_BACK_AND_FORTH:
            {
                float maxAngle = mMaxAngle == 0.0f ? 3.1415927f : mMaxAngle;
                mRotation = (maxAngle * (float)Math.Sin(movePos * TWO_PI) + mMoveRotation) * dir + mBaseRotation;
                mX = mAnchorX;
                mY = mAnchorY;
                break;
            }
            case MOVE_VERTICAL_WRAP:
                mX = mAnchorX;
                mY = mAnchorY - radiusY * (movePos - (int)movePos) * dir;
                mRotation = mBaseRotation;
                break;
            case MOVE_HORIZONTAL_WRAP:
                mX = mAnchorX + radiusX * (movePos - (int)movePos) * dir;
                mY = mAnchorY;
                mRotation = mBaseRotation;
                break;
            case MOVE_ROTATE_AROUND_CIRCLE:
            {
                float a = movePos * TWO_PI * dir;
                mX = mAnchorX + radiusX * (float)Math.Cos(a);
                mY = mAnchorY - radiusY * (float)Math.Sin(a);
                if (mRadius2 == 0 || mRadius1 == 0)
                {
                    mRotation = a + mBaseRotation;
                }
                else
                {
                    float tx = mAnchorX + radiusX * (float)Math.Cos(a + 1.5707964f);
                    float ty = mAnchorY - radiusY * (float)Math.Sin(a + 1.5707964f);
                    mRotation = (float)Math.Atan2(mAnchorY - ty, tx - mAnchorX) + mBaseRotation;
                }
                break;
            }
            case MOVE_ROTATE_AROUND_CIRCLE_BACK_AND_FORTH:
            {
                float maxAngle = mMaxAngle == 0.0f ? 3.1415927f : mMaxAngle;
                float a = mPhase * TWO_PI * dir + (maxAngle * (float)Math.Sin((GetMovePos(theUpdateCnt) - mPhase) * TWO_PI) + mMoveRotation) * dir;
                mX = mAnchorX + radiusX * (float)Math.Cos(a);
                mY = mAnchorY - radiusY * (float)Math.Sin(a);
                if (mRadius2 == 0 || mRadius1 == 0)
                {
                    mRotation = a + mBaseRotation;
                }
                else
                {
                    float tx = mAnchorX + radiusX * (float)Math.Cos(a + 1.5707964f);
                    float ty = mAnchorY - radiusY * (float)Math.Sin(a + 1.5707964f);
                    mRotation = (float)Math.Atan2(mAnchorY - ty, tx - mAnchorX) + mBaseRotation;
                }
                break;
            }
            case MOVE_TORNADO:
            {
                float a = movePos * TWO_PI * dir;
                mX = mAnchorX + radiusX * 0.8f * (float)Math.Cos(a * 4.0f) + radiusX * 0.2f * (float)Math.Cos(a * 3.0f);
                mY = mAnchorY - radiusY * 0.8f * (float)Math.Sin(a * 3.0f) + radiusY * -0.0f * (float)Math.Cos(a * 2.0f)
                    - radiusY * 0.17725f * (float)Math.Sin(a * 4.0f);
                break;
            }
            }
            if (mMoveRotation != 0.0f)
            {
                SexyUtil.RotateXY(ref mX, ref mY, mAnchorX, mAnchorY, mMoveRotation);
            }
        }

        /// <summary>Editor-only path preview (Sexy::Mover::DrawMovement).</summary>
        public void DrawMovement(Graphics g)
        {
            if (mType > 0 && mPeriod > 0)
            {
                float x = mX;
                float y = mY;
                int pause1 = mPause1;
                mPause1 = 0;
                int[] xs = new int[11];
                int[] ys = new int[11];
                for (int i = 0; i < 10; i++)
                {
                    CalcPos((mPeriod * i) / 10, 0.0f);
                    xs[i] = (int)mX;
                    ys[i] = (int)mY;
                }
                xs[10] = xs[0];
                ys[10] = ys[0];
                for (int i = 0; i < 10; i++)
                {
                    g.DrawLine(xs[i], ys[i], xs[i + 1], ys[i + 1]);
                }
                mPause1 = pause1;
                mX = x;
                mY = y;
            }
        }
    }
}
