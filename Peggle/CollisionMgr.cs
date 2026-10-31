using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>A ball paired with something it may hit; ordered like the original std::set&lt;CollisionPair&gt;.</summary>
    public struct CollisionPair
    {
        public PhysObj mObj1;
        public PhysObj mObj2;

        public CollisionPair(PhysObj theObj1, PhysObj theObj2)
        {
            if (theObj1.mShapeType == PhysObj.SHAPE_BALL)
            {
                if (theObj2.mShapeType == PhysObj.SHAPE_BALL && theObj1.mSortId >= theObj2.mSortId)
                {
                    mObj1 = theObj2;
                    mObj2 = theObj1;
                }
                else
                {
                    mObj1 = theObj1;
                    mObj2 = theObj2;
                }
            }
            else
            {
                mObj1 = theObj2;
                mObj2 = theObj1;
            }
        }
    }

    public class CollisionPairComparer : IComparer<CollisionPair>
    {
        public static readonly CollisionPairComparer Instance = new CollisionPairComparer();

        private static bool Less(CollisionPair a, CollisionPair b)
        {
            if (a.mObj1 == b.mObj1)
            {
                if (a.mObj2.mShapeType == b.mObj2.mShapeType)
                {
                    return a.mObj2.mSortId < b.mObj2.mSortId;
                }
                return a.mObj2.mShapeType < b.mObj2.mShapeType;
            }
            return a.mObj1.mSortId < b.mObj1.mSortId;
        }

        public int Compare(CollisionPair a, CollisionPair b)
        {
            if (Less(a, b))
            {
                return -1;
            }
            if (Less(b, a))
            {
                return 1;
            }
            return 0;
        }
    }

    public struct CollisionInfo
    {
        public Ball mBall;
        public PhysObj mObj;

        public CollisionInfo(Ball theBall, PhysObj theObj)
        {
            mBall = theBall;
            mObj = theObj;
        }
    }

    /// <summary>Sweep-and-prune broad phase over object bounding boxes, plus the per-ball path used for aim prediction.</summary>
    public class CollisionMgr
    {
        public struct EndPoint
        {
            public PhysObj mObj;
            public int mAxis;   // 0 = x (mMinX/mMaxX), 1 = y (mMinY/mMaxY)
            public bool mIsMin;

            public EndPoint(PhysObj theObj, int theAxis, bool isMin)
            {
                mObj = theObj;
                mAxis = theAxis;
                mIsMin = isMin;
            }

            public float Value
            {
                get
                {
                    if (mAxis == 0)
                    {
                        return mIsMin ? mObj.mMinX : mObj.mMaxX;
                    }
                    return mIsMin ? mObj.mMinY : mObj.mMaxY;
                }
            }

            public static bool operator <(EndPoint a, EndPoint b)
            {
                return a.Value < b.Value;
            }

            public static bool operator >(EndPoint a, EndPoint b)
            {
                return a.Value > b.Value;
            }
        }

        public bool mHoleOut;                                                     // 0x00
        public List<EndPoint> mXEndPoints = new List<EndPoint>();                 // 0x04
        public List<EndPoint> mYEndPoints = new List<EndPoint>();                 // 0x10
        public SortedSet<CollisionPair> mPairs = new SortedSet<CollisionPair>(CollisionPairComparer.Instance); // 0x1c
        public List<PhysObj> mCollideList = new List<PhysObj>();                  // 0x38
        public List<CollisionInfo> mCollisionInfo = new List<CollisionInfo>();    // 0x44
        public float mBallMinX;                                                   // 0x50
        public float mBallMaxX;                                                   // 0x54
        public float mBallMinY;                                                   // 0x58
        public float mBallMaxY;                                                   // 0x5c
        public List<PhysObj> mBallCandidates = new List<PhysObj>();               // 0x60
        public bool mIgnorePegs;                                                  // 0x6c
        public SortedSet<CollisionPair> mBallPairs = new SortedSet<CollisionPair>(CollisionPairComparer.Instance); // 0x70

        private static CollisionMgr mNotifyCollisionData;

        public CollisionMgr()
        {
            mHoleOut = false;
            mIgnorePegs = false;
        }

        public void Clear()
        {
            mPairs.Clear();
            mXEndPoints.Clear();
            mYEndPoints.Clear();
        }

        private static int LowerBound(List<EndPoint> theList, EndPoint theValue)
        {
            int lo = 0;
            int hi = theList.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (theList[mid] < theValue)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }
            return lo;
        }

        private void AddObj(PhysObj theObj, int theAxis, List<EndPoint> theList)
        {
            SortEndPoints(mXEndPoints);
            SortEndPoints(mYEndPoints);
            EndPoint aMin = new EndPoint(theObj, theAxis, true);
            EndPoint aMax = new EndPoint(theObj, theAxis, false);
            theList.Insert(LowerBound(theList, aMin), aMin);
            theList.Insert(LowerBound(theList, aMax), aMax);
        }

        private static bool CanPair(PhysObj theBall, PhysObj theOther)
        {
            return theOther.mShapeType != PhysObj.SHAPE_BALL ||
                   (((Ball)theOther).mPlaySounds && ((Ball)theBall).mPlaySounds);
        }

        private void SortEndPoints(List<EndPoint> theList)
        {
            int aSize = theList.Count;
            for (int i = 1; i < aSize; i++)
            {
                for (int j = i; j > 0; j--)
                {
                    EndPoint e1 = theList[j - 1];
                    EndPoint e2 = theList[j];
                    if (e1 < e2)
                    {
                        break;
                    }
                    if (!e1.mIsMin)
                    {
                        if (e2.mIsMin)
                        {
                            PhysObj a = e1.mObj;
                            PhysObj b = e2.mObj;
                            if (a.mShapeType == PhysObj.SHAPE_BALL)
                            {
                                if (CanPair(a, b) && a.CheckRectCollision(b))
                                {
                                    mPairs.Add(new CollisionPair(a, b));
                                }
                            }
                            else if (b.mShapeType == PhysObj.SHAPE_BALL && a.CheckRectCollision(b))
                            {
                                mPairs.Add(new CollisionPair(a, b));
                            }
                        }
                    }
                    else if (!e2.mIsMin)
                    {
                        PhysObj a = e1.mObj;
                        PhysObj b = e2.mObj;
                        if (a.mShapeType == PhysObj.SHAPE_BALL)
                        {
                            if (CanPair(a, b))
                            {
                                mPairs.Remove(new CollisionPair(a, b));
                            }
                        }
                        else if (b.mShapeType == PhysObj.SHAPE_BALL)
                        {
                            mPairs.Remove(new CollisionPair(a, b));
                        }
                    }
                    theList[j - 1] = e2;
                    theList[j] = e1;
                }
            }
        }

        private static void RemoveObj(PhysObj theObj, List<EndPoint> theList)
        {
            int i = 0;
            while (i < theList.Count)
            {
                if (theList[i].mObj == theObj)
                {
                    theList.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public void AddObj(PhysObj theObj)
        {
            foreach (EndPoint anEndPoint in mXEndPoints)
            {
                if (!anEndPoint.mIsMin)
                {
                    continue;
                }
                PhysObj anOther = anEndPoint.mObj;
                if (theObj.mShapeType == PhysObj.SHAPE_BALL)
                {
                    if (CanPair(theObj, anOther) && theObj.CheckRectCollision(anOther))
                    {
                        mPairs.Add(new CollisionPair(theObj, anOther));
                    }
                }
                else if (anOther.mShapeType == PhysObj.SHAPE_BALL && theObj.CheckRectCollision(anOther))
                {
                    mPairs.Add(new CollisionPair(theObj, anOther));
                }
            }
            AddObj(theObj, 0, mXEndPoints);
            AddObj(theObj, 1, mYEndPoints);
        }

        public void RemoveObj(PhysObj theObj)
        {
            RemoveObj(theObj, mXEndPoints);
            RemoveObj(theObj, mYEndPoints);
            mPairs.RemoveWhere(p => p.mObj1 == theObj || p.mObj2 == theObj);
        }

        private static void NotifyCollision(Ball theBall, PhysObj theObj)
        {
            if (theBall.mShapeType != PhysObj.SHAPE_BALL && theObj.mShapeType != PhysObj.SHAPE_BALL)
            {
                return;
            }
            CollisionMgr aMgr = mNotifyCollisionData;
            if (theBall.mShapeType == PhysObj.SHAPE_BALL)
            {
                aMgr.mCollisionInfo.Add(new CollisionInfo(theBall, theObj));
            }
            else
            {
                aMgr.mCollisionInfo.Add(new CollisionInfo((Ball)theObj, theBall));
            }
        }

        /// <summary>Smallest pair strictly after <paramref name="theCur"/> (iterator ++ that survives erasure).</summary>
        private bool NextPair(CollisionPair theCur, bool first, out CollisionPair theNext)
        {
            if (first)
            {
                if (mPairs.Count == 0)
                {
                    theNext = default(CollisionPair);
                    return false;
                }
                theNext = mPairs.Min;
                return true;
            }
            if (mPairs.Count == 0 || CollisionPairComparer.Instance.Compare(theCur, mPairs.Max) >= 0)
            {
                theNext = default(CollisionPair);
                return false;
            }
            foreach (CollisionPair aPair in mPairs.GetViewBetween(theCur, mPairs.Max))
            {
                if (CollisionPairComparer.Instance.Compare(aPair, theCur) > 0)
                {
                    theNext = aPair;
                    return true;
                }
            }
            theNext = default(CollisionPair);
            return false;
        }

        public void Update(bool collectCollisions)
        {
            mCollideList.Clear();
            if (collectCollisions)
            {
                PhysObj.mNotifyCollisionFunc = NotifyCollision;
                mNotifyCollisionData = this;
                mCollisionInfo.Clear();
            }
            SortEndPoints(mXEndPoints);
            SortEndPoints(mYEndPoints);

            Ball aCurBall = null;
            bool hasPair = NextPair(default(CollisionPair), true, out CollisionPair aPair);
            for (; hasPair; hasPair = NextPair(aPair, false, out aPair))
            {
                Ball aBall = (Ball)aPair.mObj1;
                PhysObj anObj = aPair.mObj2;
                if (aBall != aCurBall)
                {
                    if (aCurBall != null)
                    {
                        aCurBall.DoCollideUpdate(mCollideList, null);
                        mCollideList.Clear();
                    }
                    aCurBall = aBall;
                }
                if (aBall.IsCollidableWith(anObj))
                {
                    int aShape = anObj.mShapeType;
                    if (aShape == PhysObj.SHAPE_HOLE)
                    {
                        ((Hole)anObj).CheckCollision(aBall);
                    }
                    else if (aShape - 3 >= 0 && aShape - 3 < 3)
                    {
                        mCollideList.Add(anObj);
                    }
                    else if (aShape == PhysObj.SHAPE_BALL)
                    {
                        aBall.CheckBallCollision((Ball)anObj);
                    }
                }
            }
            if (aCurBall != null)
            {
                aCurBall.DoCollideUpdate(mCollideList, null);
                mCollideList.Clear();
            }
            PhysObj.mNotifyCollisionFunc = null;
        }

        public void BeginUpdateOneBall(Ball theBall)
        {
            float aMinX = theBall.mMinX - 75.0f;
            float aMaxX = theBall.mMaxX + 75.0f;
            float aMinY = theBall.mMinY - 75.0f;
            float aMaxY = theBall.mMaxY + 75.0f;
            mBallCandidates.Clear();
            mBallMinX = aMinX;
            mBallMaxX = aMaxX;
            mBallMinY = aMinY;
            mBallMaxY = aMaxY;
            foreach (EndPoint anEndPoint in mXEndPoints)
            {
                if (anEndPoint.mIsMin)
                {
                    continue;
                }
                PhysObj anObj = anEndPoint.mObj;
                if (mIgnorePegs && anObj.mPegInfo != null)
                {
                    continue;
                }
                if (anObj.CheckRectCollision(aMinX, aMinY, aMaxX, aMaxY))
                {
                    mBallCandidates.Add(anObj);
                }
            }
        }

        /// <summary>Steps one ball against its cached neighbourhood; true if it collided or entered a moving hole.</summary>
        public bool UpdateOneBall(Ball theBall, SexyVector2[] theHolePos)
        {
            bool result = false;
            mCollideList.Clear();
            if (mBallMaxX < theBall.mMaxX || theBall.mMinX < mBallMinX || theBall.mMinY < mBallMinY || mBallMaxY < theBall.mMaxY)
            {
                BeginUpdateOneBall(theBall);
            }

            foreach (PhysObj anObj in mBallCandidates)
            {
                if (!anObj.IsCollidable() || !anObj.CheckRectCollision(theBall))
                {
                    continue;
                }
                int aShape = anObj.mShapeType;
                if (aShape == PhysObj.SHAPE_HOLE)
                {
                    Hole aHole = (Hole)anObj;
                    if (aHole.mTarget != null && aHole.CheckCollision(theBall))
                    {
                        mHoleOut = true;
                        theBall.SetHoleOut(aHole.mTarget, 0);
                        if ((aHole.mMover != null && aHole.IsMoving()) ||
                            (aHole.mTarget.mMover != null && aHole.mTarget.IsMoving()))
                        {
                            result = true;
                            if (theHolePos != null)
                            {
                                theHolePos[0] = new SexyVector2(theBall.mX, theBall.mY);
                            }
                        }
                    }
                }
                else if (aShape - 4 >= 0 && aShape - 4 < 2)
                {
                    mBallPairs.Add(new CollisionPair(theBall, anObj));
                }
                else if (aShape == PhysObj.SHAPE_CIRCLE)
                {
                    mBallPairs.Add(new CollisionPair(theBall, anObj));
                }
            }

            foreach (CollisionPair aPair in mBallPairs)
            {
                mCollideList.Add(aPair.mObj2);
            }
            if (mCollideList.Count != 0 && theBall.DoCollideUpdate(mCollideList, theHolePos))
            {
                result = true;
            }
            if (!theBall.mInCollideUpdate)
            {
                theBall.DoNonCollideUpdate();
            }
            mBallPairs.Clear();
            return result;
        }
    }
}
