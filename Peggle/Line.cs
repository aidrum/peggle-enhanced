using System;

namespace Sexy
{
    /// <summary>
    /// A solid line segment (Sexy::Line), used on its own for rods and as the edges of polygons
    /// and bricks. Provides the swept-circle tests the ball uses: penetration, edge and vertex.
    /// </summary>
    public class Line : PhysObj
    {
        /// <summary>Lets a ball sliding exactly along a horizontal edge collide once (set by the ball).</summary>
        public static bool mAllowKissCollision;

        public float mDX;                                         // 0x9c

        public float mDY;                                         // 0xa0

        public float mX1;                                         // 0xa4

        public float mX2;                                         // 0xa8

        public float mY1;                                         // 0xac

        public float mY2;                                         // 0xb0

        public float mVelX;                                       // 0xb4

        public float mVelY;                                       // 0xb8

        public SexyVector2 mNormal;                               // 0xbc

        public float mD;                                          // 0xc4

        public bool mIsVertical;                                  // 0xc8

        public bool mOneSided;                                    // 0xc9

        public Line()
        {
            mShapeType = SHAPE_LINE;
            mOneSided = false;
            mVelX = 0;
            mVelY = 0;
        }

        public Line(float x1, float y1, float x2, float y2) : this()
        {
            Init(x1, y1, x2, y2);
        }

        public void Init(float x1, float y1, float x2, float y2)
        {
            mX1 = x1;
            mX2 = x2;
            mY1 = y1;
            mY2 = y2;
            mDX = mX2 - mX1;
            mDY = mY2 - mY1;
            mMinX = mX1;
            mMaxX = mX2;
            mMinY = mY1;
            mMaxY = mY2;
            if (mMaxX < mMinX)
            {
                (mMinX, mMaxX) = (mMaxX, mMinX);
            }
            if (mMaxY < mMinY)
            {
                (mMinY, mMaxY) = (mMaxY, mMinY);
            }
            mIsVertical = Math.Abs(mY1 - mY2) > Math.Abs(mX1 - mX2);
            mNormal = new SexyVector2(mY1 - mY2, mX2 - mX1).Normalize();
            mD = -new SexyVector2(mX1, mY1).Dot(mNormal);
        }

        public override void SyncState(DataSync theSync)
        {
            base.SyncState(theSync);
            bool hasVelX = mVelX != 0.0f;
            bool hasVelY = mVelY != 0.0f;
            theSync.SyncBoolBit(ref hasVelX);
            theSync.SyncBoolBit(ref hasVelY);
            theSync.SyncBoolBit(ref mOneSided);
            theSync.EndBit();
            theSync.SyncFloat(ref mX1);
            theSync.SyncFloat(ref mY1);
            theSync.SyncFloat(ref mX2);
            theSync.SyncFloat(ref mY2);
            if (hasVelX)
            {
                theSync.SyncFloat(ref mVelX);
            }
            if (hasVelY)
            {
                theSync.SyncFloat(ref mVelY);
            }
            if (mSyncType == 3)
            {
                theSync.SyncFloat(ref mDX);
                theSync.SyncFloat(ref mDY);
                theSync.SyncFloat(ref mNormal.x);
                theSync.SyncFloat(ref mNormal.y);
                theSync.SyncBoolBit(ref mIsVertical);
                theSync.EndBit();
                SetPos(mX1, mY1);
            }
            else if (theSync.GetReader() != null)
            {
                Init(mX1, mY1, mX2, mY2);
            }
        }

        public override void Translate(float theDX, float theDY)
        {
            SetPos(mX1 + theDX, mY1 + theDY);
        }

        public override void SetPos(float theX, float theY)
        {
            mX1 = theX;
            mY1 = theY;
            mX2 = mX1 + mDX;
            mY2 = mY1 + mDY;
            mD = -new SexyVector2(theX, theY).Dot(mNormal);
            mMinX = mX1;
            mMaxX = mX2;
            mMinY = mY1;
            mMaxY = mY2;
            if (mMaxX < mMinX)
            {
                (mMinX, mMaxX) = (mMaxX, mMinX);
            }
            if (mMaxY < mMinY)
            {
                (mMinY, mMaxY) = (mMaxY, mMinY);
            }
        }

        public override void SetVelocity(float theVX, float theVY)
        {
            mVelX = theVX;
            mVelY = theVY;
        }

        /// <summary>The line's own motion projected onto the contact normal.</summary>
        public void CalcEdgeHitVelocity(SexyVector2 theNormal, out SexyVector2 theHitVel)
        {
            float d = new SexyVector2(mVelX, mVelY).Dot(theNormal);
            theHitVel = theNormal * d;
        }

        public void CalcVertexHitVelocity(SexyVector2 theNormal, out SexyVector2 theHitVel)
        {
            float d = new SexyVector2(mVelX, mVelY).Dot(theNormal);
            theHitVel = theNormal * d;
        }

        /// <summary>Earliest time (0..theTime) a circle moving by theVel touches the point (theVX, theVY).</summary>
        public bool CheckVertexTimeCollision(SexyVector2 thePos, SexyVector2 theVel, float theRadius, ref float theTime, ref SexyVector2 theHitPos,
            float theVX, float theVY, ref SexyVector2 theNormal, ref SexyVector2 theHitVel)
        {
            SexyVector2 d = new SexyVector2(thePos.x - theVX, thePos.y - theVY);
            float r = theRadius - 0.01f;
            float a = theVel.Dot(theVel);
            float b = d.Dot(theVel) * 2.0f;
            float c = d.Dot(d) - r * r;
            float disc = b * b - a * 4.0f * c;
            if (disc < 0.0f)
            {
                return false;
            }
            disc = (float)Math.Sqrt(disc);
            float t = (-0.0f - b - disc) / (a * 2.0f);
            if (t < 0.0f || !(t < theTime))
            {
                return false;
            }
            t -= 0.01f / theVel.Magnitude();
            theHitPos = thePos + theVel * t;
            if (!(t > 0.0f))
            {
                t = 0.0f;
            }
            theTime = t;
            theNormal = (theHitPos - new SexyVector2(theVX, theVY)).Normalize();
            CalcVertexHitVelocity(theNormal, out theHitVel);
            return true;
        }

        /// <summary>Earliest time a moving circle touches the segment's face.</summary>
        public bool CheckEdgeTimeCollision(SexyVector2 thePos, SexyVector2 theVel, float theRadius, ref SexyVector2 theHitPos,
            ref SexyVector2 theNormal, ref SexyVector2 theHitVel, ref float theTime)
        {
            SexyVector2 n = mNormal;
            float d = mD;
            if (!mOneSided && (thePos - new SexyVector2(mX1, mY1)).Dot(n) < 0.0f)
            {
                n = n * -1.0f;
                d *= -1.0f;
            }
            float vn = theVel.Dot(n);
            if (vn > 0.2f)
            {
                return false;
            }
            if (vn == 0.0f)
            {
                if (!mAllowKissCollision || n.y != 0.0f)
                {
                    return false;
                }
                SexyVector2 p = thePos - n * theRadius;
                bool inRange = mIsVertical ? (mMinY <= p.y && p.y <= mMaxY) : (mMinX <= p.x && p.x <= mMaxX);
                if (inRange)
                {
                    float dist = thePos.Dot(n) + d;
                    if (theRadius < Math.Abs(dist))
                    {
                        return false;
                    }
                    theTime = 0.0f;
                    theHitPos = thePos;
                    theNormal = n;
                    theHitVel = new SexyVector2(0.0f, 0.0f);
                    mAllowKissCollision = false;
                    return true;
                }
            }
            float t = ((theRadius - 0.01f) - d - thePos.Dot(n)) / vn;
            if (0.0f <= t && t < theTime)
            {
                SexyVector2 c = thePos + theVel * t;
                SexyVector2 p = c - n * theRadius;
                bool inRange = mIsVertical ? (mMinY <= p.y && p.y <= mMaxY) : (mMinX <= p.x && p.x <= mMaxX);
                if (inRange)
                {
                    theTime = t;
                    theHitPos = c + n * 0.01f;
                    theNormal = n;
                    CalcEdgeHitVelocity(theNormal, out theHitVel);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Pushes a circle already overlapping the segment back out along the face or from the nearer end.</summary>
        public bool CheckPenetration(SexyVector2 thePos, float theRadius, ref SexyVector2 theHitPos, ref SexyVector2 theNormal, ref SexyVector2 theHitVel)
        {
            float r = theRadius - 0.01f;
            float dist = thePos.Dot(mNormal) + mD;
            if (r < Math.Abs(dist))
            {
                return false;
            }
            SexyVector2 hit = new SexyVector2();
            int count;
            if (mIsVertical)
            {
                if (thePos.y + theRadius < mMinY || mMaxY < thePos.y - theRadius)
                {
                    return false;
                }
                count = CircleIntersect(thePos.x, thePos.y, r, ref hit, out _, false);
            }
            else
            {
                if (thePos.x + theRadius < mMinX || mMaxX < thePos.x - theRadius)
                {
                    return false;
                }
                count = CircleIntersect(thePos.x, thePos.y, r, ref hit, out _, false);
            }
            if (count == 0)
            {
                return false;
            }
            if (count == 1)
            {
                float d1 = Sq(mX1 - thePos.x) + Sq(mY1 - thePos.y);
                float d2 = Sq(mX2 - thePos.x) + Sq(mY2 - thePos.y);
                SexyVector2 vertex;
                SexyVector2 off;
                if (d1 < d2)
                {
                    vertex = new SexyVector2(mX1, mY1);
                    off = vertex - hit;
                    if (off.x == 0.0f && off.y == 0.0f)
                    {
                        off = new SexyVector2(mX1 - mX2, mY1 - mY2).Normalize() * 0.01f;
                    }
                }
                else
                {
                    vertex = new SexyVector2(mX2, mY2);
                    off = vertex - hit;
                    if (off.x == 0.0f && off.y == 0.0f)
                    {
                        off = new SexyVector2(mX2 - mX1, mY2 - mY1).Normalize() * 0.01f;
                    }
                }
                theHitPos = thePos + off;
                theNormal = (theHitPos - vertex).Normalize();
                theHitPos = theHitPos + theNormal * 0.01f;
                CalcVertexHitVelocity(theNormal, out theHitVel);
                return true;
            }
            theNormal = mNormal;
            if (!mOneSided && dist < 0.0f)
            {
                theNormal = theNormal * -1.0f;
                dist *= -1.0f;
            }
            theHitPos = thePos + theNormal * (theRadius - dist);
            CalcEdgeHitVelocity(theNormal, out theHitVel);
            return true;
        }

        private static float Sq(float v)
        {
            return v * v;
        }

        /// <summary>Intersections of the segment with a circle; returns how many lie on the segment.</summary>
        public int CircleIntersect(float cx, float cy, float r, ref SexyVector2 theHit1, out SexyVector2 theHit2, bool wantSecond)
        {
            theHit2 = new SexyVector2();
            SexyVector2 f = new SexyVector2(mX1 - cx, mY1 - cy);
            SexyVector2 d = new SexyVector2(mX2 - mX1, mY2 - mY1);
            float a = d.Dot(d);
            float b = f.Dot(d) * 2.0f;
            float c = f.Dot(f) - r * r;
            float disc = b * b - a * 4.0f * c;
            if (disc < 0.0f)
            {
                return 0;
            }
            disc = (float)Math.Sqrt(disc);
            float t1 = (-0.0f - b + disc) / (a * 2.0f);
            float t2 = (-0.0f - b - disc) / (a * 2.0f);
            int count = 0;
            SexyVector2 start = new SexyVector2(f.x + cx, f.y + cy);
            if (0.0f <= t1 && t1 <= 1.0f)
            {
                theHit1 = start + d * t1;
                count++;
            }
            if (0.0f <= t2 && t2 <= 1.0f)
            {
                if (count == 0)
                {
                    theHit1 = start + d * t2;
                }
                else if (wantSecond)
                {
                    theHit2 = start + d * t2;
                }
                count++;
            }
            return count;
        }

        /// <summary>
        /// Full swept test against this segment: penetration first, then face, then the first end
        /// (and the second end when <paramref name="checkSecondVertex"/>; shared corners skip it).
        /// </summary>
        public bool CheckCollision(SexyVector2 thePos, SexyVector2 theVel, float theRadius, ref float theTime, ref SexyVector2 theHitPos,
            ref SexyVector2 theNormal, ref SexyVector2 theHitVel, bool checkSecondVertex)
        {
            if (thePos.x + theRadius < mMinX - 20.0f || mMaxX + 20.0f < thePos.x - theRadius
                || thePos.y + theRadius < mMinY - 20.0f || mMaxY + 20.0f < thePos.y - theRadius)
            {
                return false;
            }
            if (CheckPenetration(thePos, theRadius, ref theHitPos, ref theNormal, ref theHitVel))
            {
                theTime = 0.0f;
                return true;
            }
            bool edge = CheckEdgeTimeCollision(thePos, theVel, theRadius, ref theHitPos, ref theNormal, ref theHitVel, ref theTime);
            bool vertex = CheckVertexTimeCollision(thePos, theVel, theRadius, ref theTime, ref theHitPos, mX1, mY1, ref theNormal, ref theHitVel);
            bool result = vertex || edge;
            if (checkSecondVertex && CheckVertexTimeCollision(thePos, theVel, theRadius, ref theTime, ref theHitPos, mX2, mY2, ref theNormal, ref theHitVel))
            {
                result = true;
            }
            return result;
        }

        public float GetDistanceFromPoint(float theX, float theY)
        {
            bool inRange = mIsVertical ? (mMinY <= theY && theY <= mMaxY) : (mMinX <= theX && theX <= mMaxX);
            if (!inRange)
            {
                float d1 = Sq(theX - mX1) + Sq(theY - mY1);
                float d2 = Sq(theX - mX2) + Sq(theY - mY2);
                return (float)Math.Sqrt(d1 < d2 ? d1 : d2);
            }
            return Math.Abs(new SexyVector2(theX, theY).Dot(mNormal) + mD);
        }

        private static int CalcClipCode(float x, float y, TRect theRect)
        {
            int code = 0;
            if (x < theRect.mX)
            {
                code |= 1;
            }
            else if (x > theRect.mX + theRect.mWidth)
            {
                code |= 2;
            }
            if (y < theRect.mY)
            {
                code |= 4;
            }
            else if (y > theRect.mY + theRect.mHeight)
            {
                code |= 8;
            }
            return code;
        }

        private static void CalcMinMax(SexyVector2[] thePoints, SexyVector2 theAxis, out float theMin, out float theMax)
        {
            theMin = theMax = thePoints[0].Dot(theAxis);
            for (int i = 1; i < thePoints.Length; i++)
            {
                float v = thePoints[i].Dot(theAxis);
                theMin = Math.Min(theMin, v);
                theMax = Math.Max(theMax, v);
            }
        }

        /// <summary>Separating-axis test of the segment against a rectangle.</summary>
        public bool IsPartlyInsideRect(TRect theRect)
        {
            int c1 = CalcClipCode(mX1, mY1, theRect);
            int c2 = CalcClipCode(mX2, mY2, theRect);
            if (c1 == 0 || c2 == 0)
            {
                return true;
            }
            if ((c1 & c2) != 0)
            {
                return false;
            }
            SexyVector2[] rect =
            {
                new SexyVector2(theRect.mX, theRect.mY), new SexyVector2(theRect.mX + theRect.mWidth, theRect.mY),
                new SexyVector2(theRect.mX, theRect.mY + theRect.mHeight), new SexyVector2(theRect.mX + theRect.mWidth, theRect.mY + theRect.mHeight)
            };
            SexyVector2[] seg = { new SexyVector2(mX1, mY1), new SexyVector2(mX2, mY2) };
            SexyVector2[] axes = { new SexyVector2(1.0f, 0.0f), new SexyVector2(0.0f, 1.0f), new SexyVector2(mY1 - mY2, mX2 - mX1) };
            foreach (SexyVector2 axis in axes)
            {
                CalcMinMax(rect, axis, out float min1, out float max1);
                CalcMinMax(seg, axis, out float min2, out float max2);
                if (max2 < min1 || max1 < min2)
                {
                    return false;
                }
            }
            return true;
        }

        public override void Draw(Graphics g)
        {
            g.SetColor(SexyUtil.MakeColor((int)mColor));
            float scale = SexyUtil.gPhysicalDisplayHeight;
            g.DrawLine((int)((mX1 * scale) / 600.0), (int)((mY1 * scale) / 600.0), (int)((mX2 * scale) / 600.0), (int)((mY2 * scale) / 600.0));
        }

        public override int GetClass()
        {
            return CLASS_LINE;
        }

        public override float GetXPos()
        {
            return mX1;
        }

        public override float GetYPos()
        {
            return mY1;
        }
    }
}
