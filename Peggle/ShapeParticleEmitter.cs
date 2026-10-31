using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Port of Peggle WoW's Sexy::ShapeParticleEmitter (0x190 bytes; ctor 0x43eb10, AddPoint 0x43ed40,
    /// ClearShape 0x43eca0, PosAt 0x43f220, Update vf2 0x43f3b0, Fill 0x43f720): particles along a path
    /// (a polyline from the emitter's position through the points added). It traces the path, dropping
    /// mTraceRate particles per update as a cursor moves mSpeed along it and resting a random
    /// mDelayMin..mDelayMax updates after each pass (unless closed), and/or fills it with particles
    /// spaced along its whole length (Fill).
    /// </summary>
    public class ShapeParticleEmitter : ParticleEmitter
    {
        /// <summary>The path's segments (deltas) and their lengths.</summary>
        private readonly List<Vector2> mSegments = new List<Vector2>(); // 0x110

        private readonly List<float> mLengths = new List<float>();       // 0x120

        public bool mClosed;                                      // 0x10c

        public double mSpeed;                                     // 0x138

        public double mCursor;                                    // 0x140

        public double mTotalLength;                               // 0x148

        public float mTraceRate;                                  // 0x154

        public float mTraceRand;                                  // 0x158

        private float mTraceAccum;                                // 0x15c

        private float mTraceSpawned;                              // 0x160

        public int mDelayMin;                                     // 0x164

        public int mDelayMax;                                     // 0x168

        public int mDelay;                                        // 0x16c

        /// <summary>Fills per update (vf2's second part): mFillRate plus up to mFillRand more, each a Fill along the path.</summary>
        public float mFillRate;                                   // 0x170

        public float mFillRand;                                   // 0x174

        private float mFillAccum;                                 // 0x178

        private int mFillCount;                                   // 0x17c

        /// <summary>Fill's arguments (its first flag is unused; the second is theNormal).</summary>
        public bool mFillNormal;                                  // 0x181

        public float mFillMinStep = 1;                            // 0x184

        public float mFillMaxStep;                                // 0x188

        public bool mFillOffset;                                  // 0x18c

        private static readonly Random sShapeRand = new Random();

        private static float Rand01()
        {
            return sShapeRand.Next(10000) / 10000.0f;
        }

        /// <summary>FUN_0043fdc0.</summary>
        public void SetSpeed(float theSpeed)
        {
            mSpeed = theSpeed;
            mCursor = 0;
        }

        /// <summary>FUN_0043f350: a new rest between passes.</summary>
        public void NewDelay()
        {
            mDelay = (int)(mDelayMin + Rand01() * (mDelayMax - mDelayMin));
        }

        /// <summary>FUN_0043eca0.</summary>
        public void ClearShape()
        {
            mSegments.Clear();
            mLengths.Clear();
            mTotalLength = 0;
        }

        /// <summary>FUN_0043ed40: extends the path to (x,y); the first point joins it to the emitter's position.</summary>
        public void AddPoint(float x, float y)
        {
            float anEndX = 0;
            float anEndY = 0;
            foreach (Vector2 aSeg in mSegments)
            {
                anEndX += aSeg.X;
                anEndY += aSeg.Y;
            }
            Vector2 aNew = new Vector2(x - anEndX, y - anEndY);
            mSegments.Add(aNew);
            float aLen = aNew.Length();
            mLengths.Add(aLen);
            mTotalLength += aLen;
        }

        /// <summary>FUN_0043f220: the point theDist along the path, relative to the emitter.</summary>
        public Vector2 PosAt(double theDist)
        {
            Vector2 aPos = Vector2.Zero;
            if (mSegments.Count == 0 || theDist > mTotalLength)
            {
                return aPos;
            }
            int i = 0;
            while (true)
            {
                float aLen = mLengths[i];
                if (theDist <= aLen)
                {
                    float t = aLen <= 0 ? 0 : (float)(theDist / aLen);
                    return aPos + mSegments[i] * t;
                }
                aPos += mSegments[i];
                theDist -= aLen;
                i = (i + 1) % mSegments.Count;
                if (i == 0)
                {
                    aPos = Vector2.Zero;
                }
            }
        }

        /// <summary>vf2: traces the path (when mTraceRate is set), then updates the particles.</summary>
        public override void Update(bool theSpawn)
        {
            double aStartCursor = mCursor;
            if (mSegments.Count != 0 && (mTraceRate > 0 || mTraceRand > 0))
            {
                double aStart = mCursor;
                if (mDelay < 1)
                {
                    float aPrevAccum = mTraceAccum;
                    mTraceAccum += mTraceRate;
                    if (mTraceRand > 0)
                    {
                        mTraceAccum += Rand01() * mTraceRand;
                    }
                    double aNext = mCursor + mSpeed;
                    if (aNext <= mTotalLength || mClosed)
                    {
                        if (aNext > mTotalLength)
                        {
                            aNext -= mTotalLength;
                        }
                        while (mTraceSpawned < mTraceAccum)
                        {
                            mTraceSpawned++;
                            double aDist = ((mTraceSpawned - aPrevAccum) / mTraceRate * mSpeed + aStart) % mTotalLength;
                            if (theSpawn)
                            {
                                EmitterParticle p = Spawn(true);
                                Vector2 aPos = PosAt(aDist);
                                p.mX += aPos.X;
                                p.mY += aPos.Y;
                            }
                        }
                    }
                    else
                    {
                        aNext = 0;
                        NewDelay();
                    }
                    mCursor = aNext;
                }
                else
                {
                    mCursor = 0;
                    mDelay--;
                    aStartCursor = 0;
                }
            }
            if (mSegments.Count != 0)
            {
                // The fills, each a step further along the path from where this update's tracing began.
                mFillAccum += mFillRate;
                if (mFillRand != 0)
                {
                    mFillAccum += Rand01() * mFillRand;
                }
                if (mFillCount < mFillAccum)
                {
                    mCursor = aStartCursor;
                    while (mFillCount < mFillAccum)
                    {
                        mCursor += mSpeed;
                        if (mCursor > mTotalLength)
                        {
                            mCursor -= mTotalLength;
                        }
                        if (theSpawn)
                        {
                            Fill(mFillNormal, mFillMinStep, mFillMaxStep, mFillOffset ? 1 : 0);
                        }
                        mFillCount++;
                    }
                }
            }
            base.Update(theSpawn);
        }

        /// <summary>
        /// FUN_0043f720: particles all along the path, theMinStep..theMaxStep apart. With theNormal, their
        /// velocity and acceleration turn across the path (keeping their size and signs), and they start
        /// theOffset of a segment out from it.
        /// </summary>
        public void Fill(bool theNormal, float theMinStep, float theMaxStep, float theOffset)
        {
            if (mSegments.Count == 0 || mTotalLength == 0)
            {
                return;
            }
            float aStepRange = theMaxStep - theMinStep;
            double aDist = mCursor + (theMaxStep > 0 ? Rand01() * aStepRange + theMinStep : 0);
            double aTravelled = 0;
            int i = 0;
            Vector2 aBase = Vector2.Zero;
            bool aHaveSpeed = false;
            float aSpeed = 0;
            float anAccel = 0;
            while (true)
            {
                float aLen = mLengths[i];
                if (aLen < aDist)
                {
                    aBase += mSegments[i];
                    aDist -= aLen;
                    i = (i + 1) % mSegments.Count;
                    if (i == 0)
                    {
                        aBase = Vector2.Zero;
                    }
                    continue;
                }
                float t = aLen > 0 ? (float)(aDist / aLen) : 0;
                Vector2 aSeg = mSegments[i];
                EmitterParticle p = Spawn(true);
                if (!aHaveSpeed)
                {
                    aHaveSpeed = true;
                    aSpeed = (float)Math.Sqrt(p.mVelX * p.mVelX + p.mVelY * p.mVelY);
                    anAccel = (float)Math.Sqrt(p.mAccelX * p.mAccelX + p.mAccelY * p.mAccelY);
                }
                p.mX = aBase.X + aSeg.X * t;
                p.mY = aBase.Y + aSeg.Y * t;
                if (theNormal && (aSpeed != 0 || anAccel != 0))
                {
                    Vector2 aNormal = new Vector2(aSeg.Y, -aSeg.X);
                    if (p.mVelX != 0 || p.mVelY != 0)
                    {
                        p.mVelX = (p.mVelX >= 0 ? 1 : -1) * aNormal.X * aSpeed;
                        p.mVelY = (p.mVelY >= 0 ? 1 : -1) * aNormal.Y * aSpeed;
                    }
                    if (p.mAccelX != 0 || p.mAccelY != 0)
                    {
                        p.mAccelX = (p.mAccelX >= 0 ? 1 : -1) * aNormal.X * anAccel;
                        p.mAccelY = (p.mAccelY >= 0 ? 1 : -1) * aNormal.Y * anAccel;
                    }
                    if (theOffset != 0)
                    {
                        p.mX += theOffset * aNormal.X;
                        p.mY += theOffset * aNormal.Y;
                    }
                }
                p.mX += mX;
                p.mY += mY;
                float aStep = theMaxStep > 0 ? Rand01() * aStepRange + theMinStep : theMinStep;
                if (aTravelled < mTotalLength && mTotalLength <= aTravelled + aStep)
                {
                    return;
                }
                aTravelled += aStep;
                aDist += aStep;
            }
        }

        /// <summary>vf1: drawn only once it has a path.</summary>
        public new void Draw(Graphics g, float theOffX, float theOffY, float theScale)
        {
            if (mSegments.Count != 0)
            {
                base.Draw(g, theOffX, theOffY, theScale);
            }
        }
    }
}
