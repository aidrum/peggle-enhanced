using System;
using System.Collections.Generic;
using System.Linq;

namespace Sexy
{
    /// <summary>
    /// A simulated shot used by the computer opponent and ZenBall (Sexy::AIBall). It flies through
    /// the AIMgr's private copy of the level, records when it hits things, and keeps a heuristic
    /// score that AIMgr uses to pick the best angle. Pegs are never removed; hits live in mHitPegs.
    /// </summary>
    public class AIBall : Ball
    {
        public static int mThinkCount;

        public static AIMgr mAIMgr;

        public static bool mShowPath;

        /// <summary>The current player's character power (LogicMgr.mPowerupTypes).</summary>
        public static int mPowerup;

        /// <summary>Whether that power is already active when the shot starts.</summary>
        public static bool mHadPowerup;

        public float mAngle;                                      // 0x160

        public int mGoalPegsLeft;                                 // 0x164

        /// <summary>Heuristic value AIMgr ranks shots by.</summary>
        public int mScore;                                        // 0x168

        /// <summary>Points the shot would actually earn.</summary>
        public int mPoints;                                       // 0x16c

        public int mGoalPegsHit;                                  // 0x170

        public int mPurplePoints;                                 // 0x174

        /// <summary>Think frames of the first two collisions with anything.</summary>
        public int[] mHitTimes = { -1, -1 };                      // 0x178

        /// <summary>Think frames of the first three peg hits.</summary>
        public int[] mPegHitTimes = { -1, -1, -1 };               // 0x180

        /// <summary>Think frames of the first two orange peg hits.</summary>
        public int[] mGoalHitTimes = { -1, -1 };                  // 0x18c

        public int mPowerupHitTime;                               // 0x194

        public int mNumHits;                                      // 0x198

        public int mNumPegHits;                                   // 0x19c

        public int mNumGoalHits;                                  // 0x1a0

        public int mHoleTime;                                     // 0x1a4

        /// <summary>Pegs this shot has hit, with the hit frame (crumbling pegs count hits down from 0).</summary>
        public Dictionary<PegInfo, int> mHitPegs = new Dictionary<PegInfo, int>(); // 0x1a8

        public List<SexyVector2> mPath = new List<SexyVector2>(); // 0x1c0

        /// <summary>0 flying, 1 finished (remove from collisions), 2 removed.</summary>
        public int mDone;                                         // 0x1cc

        public bool mHitFreeBallCover;                            // 0x1cd

        public int mBombScore;                                    // 0x1d0

        public int mBombTime;                                     // 0x1d4

        public int mBombGoalPegsLeft;                             // 0x1d8

        public bool mHitMovingFirst;                              // 0x1dc

        public bool mGotFreeBall;                                 // 0x1dd

        /// <summary>This ball's own copies of the level objects that have logic attached.</summary>
        public List<PhysObj> mObjects = new List<PhysObj>();     // 0x1e0

        public List<bool> mColliding = new List<bool>();          // 0x1e8

        /// <summary>The other half of a multiball split.</summary>
        public AIBall mPartner;                                   // 0x1fc

        public bool mIsMultiballChild;                            // 0x200

        public AIBall() : base(false)
        {
            mPlaySounds = false;
            mGoalPegsHit = 0;
            mDone = 0;
            mGoalPegsLeft = 0;
            mNumHits = 0;
            mNumPegHits = 0;
            mNumGoalHits = 0;
            mBombScore = -1;
            mBombTime = -1;
            mBombGoalPegsLeft = 100;
            mPurplePoints = 0;
            mHoleTime = -1;
            mPartner = null;
            mIsMultiballChild = false;
            mHitFreeBallCover = false;
            mHitMovingFirst = false;
            mGotFreeBall = false;
        }

        public void CopyAttributes(AIBall theBall)
        {
            mAngle = theBall.mAngle;
            mScore = theBall.mScore;
            mPoints = theBall.mPoints;
            mHitPegs = new Dictionary<PegInfo, int>(theBall.mHitPegs);
            mObjects = new List<PhysObj>(theBall.mObjects);
            mGoalPegsHit = theBall.mGoalPegsHit;
            ResetColliding();
        }

        private void ResetColliding()
        {
            mColliding.Clear();
            for (int i = 0; i < mObjects.Count; i++)
            {
                mColliding.Add(false);
            }
        }

        /// <summary>
        /// Gives this ball its own copies of the logic objects written to <paramref name="theSync"/>.
        /// The shared sub-movers are registered first, in the same order, so their ids resolve.
        /// </summary>
        public void SyncObjects(DataSync theSync, List<object> theRefCounts)
        {
            DataSync aSync = new DataSync();
            DataWriter aWriter = theSync.GetWriter();
            aSync.StartReadMemory(aWriter.GetDataPtr(), aWriter.GetDataLen(), false);
            aSync.SetVersion(0x57);
            foreach (object aRefCount in theRefCounts)
            {
                aSync.AddRefCount(aRefCount);
            }
            aSync.SyncList(mObjects, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            ResetColliding();
        }

        public override bool IsCollidableWith(PhysObj theObj)
        {
            if (mPartner != null && theObj.mShapeType == SHAPE_BALL)
            {
                return theObj == mPartner;
            }
            if (!base.IsCollidableWith(theObj))
            {
                return false;
            }
            PegInfo aPeg = theObj.mPegInfo;
            if (aPeg == null)
            {
                return true;
            }
            bool spooky = aPeg.mType == PegType.PEG_POWERUP && mPowerup == 3;
            if (!spooky && !aPeg.mCrumble && !mFireball)
            {
                return true;
            }
            if (!mHitPegs.TryGetValue(aPeg, out int aHitTime))
            {
                return true;
            }
            if (spooky || mFireball)
            {
                return false;
            }
            return aHitTime < 0 || mThinkCount - aHitTime < 0x61;
        }

        public void DoMultiball(PhysObj thePeg)
        {
            LogicMgr aLogicMgr = mAIMgr.mBoard.mLogicMgr;
            float aVelX = 0.5f;
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            float aDiff = aX - mX;
            if (aDiff < 0.0f)
            {
                aVelX *= -1.0f;
            }
            else if (aDiff == 0.0f && aLogicMgr.mInt2c % 2 == 0)
            {
                aVelX *= -1.0f;
            }
            AIBall aBall = new AIBall();
            aBall.CopyAttributes(this);
            aBall.SetPos(aX, aY);
            aBall.SetVelocity(aVelX, -3.0f);
            mAIMgr.mBalls.Add(aBall);
            mAIMgr.mMovingObjs.Add(aBall);
            mAIMgr.mCollisionMgr.AddObj(aBall);
            aBall.mIsMultiballChild = true;
            aBall.mPlaySounds = true;
            aBall.mPartner = this;
            mPlaySounds = true;
            mPartner = aBall;
        }

        private static float DistSqr(PhysObj theObj, float theX, float theY)
        {
            float dx = theObj.GetXPos() - theX;
            float dy = theObj.GetYPos() - theY;
            return dx * dx + dy * dy;
        }

        public void DoExploder(PhysObj thePeg)
        {
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            const float aRadiusSq = 10000.0f;
            foreach (PhysObj anObj in mAIMgr.mBoard.mLogicMgr.mList394.ToArray())
            {
                if (!anObj.mPegInfo.mHit && anObj.IsVisible() && DistSqr(anObj, aX, aY) <= aRadiusSq)
                {
                    NotifyCollision(anObj);
                }
            }
        }

        /// <summary>Lights the nearest fifth of the remaining orange pegs.</summary>
        public void DoFeverTron(PhysObj thePeg)
        {
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            LogicMgr aLogicMgr = mAIMgr.mBoard.mLogicMgr;
            List<KeyValuePair<float, PhysObj>> aByDist = new List<KeyValuePair<float, PhysObj>>();
            foreach (PhysObj anObj in aLogicMgr.mList394)
            {
                if (!anObj.mPegInfo.mHit && anObj.IsVisible() && anObj.mPegInfo.mType == PegType.PEG_GOAL
                    && !mHitPegs.ContainsKey(anObj.mPegInfo))
                {
                    aByDist.Add(new KeyValuePair<float, PhysObj>(DistSqr(anObj, aX, aY), anObj));
                }
            }
            int aCount = (int)Math.Ceiling(aLogicMgr.mGoalPegs.Count * 0.2f);
            int i = 0;
            foreach (KeyValuePair<float, PhysObj> anEntry in aByDist.OrderBy(e => e.Key))
            {
                if (i >= aCount)
                {
                    break;
                }
                NotifyCollision(anEntry.Value);
                i++;
            }
        }

        /// <summary>Scores what the time bomb would take out if it blew up here (checked every 20 frames).</summary>
        public void DoTimeBomb()
        {
            float aX = mX;
            float aY = mY;
            const float aRadiusSq = 6400.0f;
            int aGoalPegsLeft = mGoalPegsLeft;
            int aHits = mHitPegs.Count;
            int aScore = mScore;
            int aPoints = mPoints;
            bool hitGoal = mGoalPegsHit > 0;
            LogicMgr aLogicMgr = mAIMgr.mBoard.mLogicMgr;
            foreach (PhysObj anObj in aLogicMgr.mList394)
            {
                PegInfo aPeg = anObj.mPegInfo;
                if (aPeg.mHit || !anObj.IsVisible() || mHitPegs.ContainsKey(aPeg))
                {
                    continue;
                }
                if (DistSqr(anObj, aX, aY) <= aRadiusSq)
                {
                    int aMult = aLogicMgr.CalcScoreMult(aGoalPegsLeft);
                    int aBase = 10;
                    if (aPeg.mType == PegType.PEG_SCORE)
                    {
                        aBase = 500;
                    }
                    else if (aPeg.mType == PegType.PEG_GOAL)
                    {
                        aBase = 100;
                        aGoalPegsLeft--;
                        hitGoal = true;
                    }
                    aPoints = aBase * aMult + aPoints;
                    aScore = aBase * aMult * aHits + aPoints + aScore;
                    aHits++;
                }
            }
            if (hitGoal && mBombScore <= aScore)
            {
                mBombScore = aScore;
                mBombTime = mThinkCount;
                mBombGoalPegsLeft = aGoalPegsLeft;
            }
        }

        public override void NotifyCollision(PhysObj theObj)
        {
            LogicMgr aLogicMgr = mAIMgr.mBoard.mLogicMgr;
            bool isComputer = aLogicMgr.mIsComputer[aLogicMgr.mCurPlayer];
            if (mNumHits < 2)
            {
                if (mNumHits == 0 && theObj.mMover != null && theObj.mMoving)
                {
                    mHitMovingFirst = true;
                }
                mHitTimes[mNumHits++] = mThinkCount;
            }
            if (!string.IsNullOrEmpty(theObj.mLogic))
            {
                theObj.NotifyCollision(this);
            }
            if (theObj.mShapeType == SHAPE_POLY)
            {
                if (theObj.mId == "freeball")
                {
                    if (AIMgr.mSkillLevel < 10)
                    {
                        if (mFireball)
                        {
                            mScore += 5000;
                        }
                        else
                        {
                            mDone = 1;
                        }
                    }
                    else if (mGoalPegsLeft == 1)
                    {
                        mScore -= 15000;
                    }
                    if (mGotFreeBall)
                    {
                        mDone = 1;
                    }
                }
                else if (theObj.mId == "flipper")
                {
                    mDone = 1;
                }
                else if (theObj.mId == "freeballcover")
                {
                    if (!mHitFreeBallCover)
                    {
                        mHitFreeBallCover = true;
                        if (mFireball)
                        {
                            mScore += 5000;
                        }
                    }
                    else
                    {
                        mDone = 1;
                    }
                }
                else if (theObj.mId == "bumperhole" && AIMgr.mSkillLevel < 0)
                {
                    mDone = 1;
                }
                else if (theObj.mId == "bumperhole" && AIMgr.mSkillLevel > 9 && mGoalPegsLeft < 3
                    && aLogicMgr.mDuelMode && isComputer)
                {
                    mScore = (int)((float)mScore * 0.1f);
                }
            }
            if (theObj.mShapeType == SHAPE_HOLE)
            {
                Hole aHole = (Hole)theObj;
                if (aHole.mTarget == null)
                {
                    mDone = 1;
                }
                else
                {
                    if (mHoleTime == -1)
                    {
                        mHoleTime = mThinkCount;
                    }
                    SetHoleOut(aHole.mTarget, aHole.mValueA0);
                }
                if (theObj.mId == "freeball" && AIMgr.mSkillLevel > 9 && !aLogicMgr.mDuelMode && mScore > 0 && !mGotFreeBall)
                {
                    mScore += 2500;
                }
                return;
            }
            PegInfo aPeg = theObj.mPegInfo;
            if (aPeg == null || aPeg.mHit)
            {
                return;
            }
            if (mNumPegHits < 3)
            {
                mPegHitTimes[mNumPegHits++] = mThinkCount;
            }
            if (!aPeg.mCrumble)
            {
                if (mHitPegs.ContainsKey(aPeg))
                {
                    return;
                }
                mHitPegs[aPeg] = mThinkCount;
            }
            else
            {
                if (!mHitPegs.TryGetValue(aPeg, out int aCount))
                {
                    aCount = 0;
                }
                if (aCount < 1)
                {
                    aCount--;
                    if (aCount == -3)
                    {
                        aCount = mThinkCount;
                    }
                }
                mHitPegs[aPeg] = aCount;
                if (aCount != -1)
                {
                    return;
                }
            }
            if (aPeg.mType == PegType.PEG_GOAL && mNumGoalHits < 2)
            {
                mGoalHitTimes[mNumGoalHits++] = mThinkCount;
            }
            if (aPeg.mType == PegType.PEG_POWERUP && mPowerupHitTime == -1)
            {
                mPowerupHitTime = mThinkCount;
            }
            bool applyMult = true;
            int aBase;
            switch (aPeg.mType)
            {
            case PegType.PEG_NORMAL:
                aBase = 10;
                break;
            case PegType.PEG_GOAL:
                aBase = 100;
                break;
            case PegType.PEG_SCORE:
                aBase = 500;
                break;
            case PegType.PEG_POWERUP:
                if (!isComputer)
                {
                    aBase = 50;
                }
                else
                {
                    switch (mPowerup)
                    {
                    case 1:
                    case 6:
                    case 8:
                    case 12:
                        aBase = 300;
                        break;
                    case 2:
                    case 4:
                        aBase = 300;
                        if (!mHadPowerup)
                        {
                            mGotFreeBall = true;
                        }
                        break;
                    case 7:
                        aBase = AIMgr.mSkillLevel < 11 ? 300 : 10;
                        break;
                    case 13:
                        aBase = 5000;
                        applyMult = false;
                        break;
                    default:
                        aBase = 100;
                        break;
                    }
                }
                break;
            default:
                aBase = 10;
                break;
            }
            if (isComputer)
            {
                if (mHitPegs.Count == 1 && mGoalPegsLeft == 1 && aPeg.mType == PegType.PEG_GOAL)
                {
                    aBase = (int)((double)aBase * 1.5);
                }
                aBase = (int)((float)(aBase * 10) / ((float)(uint)mHitPegs.Count * 1.0f + 2.0f));
                if (mGoalPegsLeft < 3)
                {
                    aBase = (int)((float)(aBase * 10) / ((float)(uint)(mHitPegs.Count + mGoalPegsLeft) * 1.0f + 2.0f));
                }
                if (mHitPegs.Count == 1 && aPeg.mType == PegType.PEG_GOAL && aLogicMgr.mDuelMode)
                {
                    aBase <<= 2;
                }
            }
            if (applyMult)
            {
                aBase *= aLogicMgr.CalcScoreMult(mGoalPegsLeft);
            }
            if (aBase > 10000 && aPeg.mType == PegType.PEG_SCORE)
            {
                aBase = 10000;
            }
            if (aPeg.mType == PegType.PEG_POWERUP)
            {
                switch (mPowerup)
                {
                case 3:
                    DoMultiball(theObj);
                    break;
                case 5:
                    DoExploder(theObj);
                    break;
                case 9:
                    DoFeverTron(theObj);
                    break;
                case 11:
                    mBombTime = 0;
                    break;
                }
            }
            else if (aPeg.mType == PegType.PEG_SCORE)
            {
                mPurplePoints = aBase;
            }
            mPoints += aBase;
            int aDelta = aBase * (mHitPegs.Count - 1) + mPoints;
            mScore += aDelta;
            if (aPeg.mType == PegType.PEG_GOAL)
            {
                mGoalPegsHit++;
                mGoalPegsLeft--;
                if (mIsMultiballChild && mPartner != null)
                {
                    mPartner.mGoalPegsHit++;
                    mPartner.mGoalPegsLeft--;
                }
            }
            if (mIsMultiballChild && mPartner != null)
            {
                mPartner.mPoints += aBase;
                mPartner.mScore += aDelta;
            }
        }

        public override void Update()
        {
            if (mDone != 0)
            {
                return;
            }
            if (mBombTime >= 0 && mThinkCount % 20 == 0)
            {
                DoTimeBomb();
            }
            mStuckCounter = 0;
            base.Update();
            if (mShowPath)
            {
                mPath.Add(new SexyVector2(mX, mY));
            }
            if (mThinkCount > 500 && mBombTime < 0)
            {
                mDone = 1;
            }
            if (!mIsMultiballChild)
            {
                foreach (PhysObj anObj in mObjects)
                {
                    anObj.Update();
                }
            }
            CollisionMgr aCollisionMgr = mAIMgr.mCollisionMgr;
            for (int i = 0; i < mObjects.Count; i++)
            {
                PhysObj anObj = mObjects[i];
                bool colliding = CheckRectCollision(anObj) && IsCollidableWith(anObj);
                if (colliding != mColliding[i])
                {
                    if (!colliding)
                    {
                        aCollisionMgr.mPairs.Remove(new CollisionPair(this, anObj));
                    }
                    else
                    {
                        aCollisionMgr.mPairs.Add(new CollisionPair(this, anObj));
                    }
                    mColliding[i] = colliding;
                }
            }
        }
    }
}
