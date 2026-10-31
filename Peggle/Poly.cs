using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// A polygon built from Line edges around a centre (Sexy::Poly). Rotating movers re-rotate
    /// the points lazily; closed polygons also push out balls found inside them; "movetoggle"
    /// polygons flip between two movement phases when hit.
    /// </summary>
    public class Poly : PhysObj
    {
        public static bool mAllowDim;

        public List<SexyVector2> mPoints = new List<SexyVector2>();      // 0x9c (relative to the centre)

        public List<SexyVector2> mLineOffsets = new List<SexyVector2>(); // 0xa8

        public List<Line> mLines = new List<Line>();                      // 0xb4

        public float mCenterX;                                    // 0xc0

        public float mCenterY;                                    // 0xc4

        public float mRotation;                                   // 0xc8

        public float mRotVel;                                     // 0xcc

        public float mScale;                                      // 0xd0

        public float mDrawRotation;                               // 0xd4

        public bool mIsRect;                                      // 0xd8

        public bool mConvex;                                      // 0xd9

        public bool mClosed;                                      // 0xda

        public bool mTileImage;                                   // 0xdb

        public bool mFlipped;                                     // 0xdc

        public int mNormalDir;                                    // 0xe0

        public int mGrowCounter;                                  // 0xe4

        public int mGrowFlags;                                    // 0xe8

        public int mHighlightCounter;                             // 0xec

        public bool mGrowHighlight;                               // 0xf0

        public bool mNeedRotInit;                                 // 0xf1

        public bool mNeedPosInit;                                 // 0xf2

        public bool mStoppedRotating;                             // 0xf3

        public Poly()
        {
            InitPolyDefaults();
            mShapeType = SHAPE_POLY;
            mCenterY = 0;
            mCenterX = 0;
            mIsRect = false;
            mConvex = true;
            mClosed = true;
        }

        private void InitPolyDefaults()
        {
            mNormalDir = 1;
            mRotation = 0;
            mRotVel = 0;
            mDrawRotation = 0;
            mGrowCounter = 0;
            mGrowFlags = 0;
            mHighlightCounter = 0;
            mTileImage = false;
            mFlipped = false;
            mGrowHighlight = true;
            mNeedRotInit = false;
            mNeedPosInit = false;
            mStoppedRotating = false;
            mScale = 1.0f;
        }

        public void InitFromRect(float theX, float theY, float theWidth, float theHeight)
        {
            mPoints.Clear();
            float hw = (theWidth - 1.0f) / 2.0f;
            float hh = (theHeight - 1.0f) / 2.0f;
            AddPoint(-hw, -hh);
            AddPoint(hw, -hh);
            AddPoint(hw, hh);
            AddPoint(-hw, hh);
            AddPoint(-hw, -hh);
            mCenterX = theX + (theWidth - 1.0f) / 2.0f;
            mCenterY = theY + (theHeight - 1.0f) / 2.0f;
            mIsRect = true;
            InitFromPoints();
        }

        protected void SyncCommon(DataSync theSync, bool syncPoints)
        {
            base.SyncState(theSync);
            bool reading = theSync.GetReader() != null;
            if (reading)
            {
                InitPolyDefaults();
            }
            bool hasMover = mMover != null;
            bool hasNormalDir = mNormalDir != 1;
            bool hasRotation = mRotation != 0.0f && hasMover;
            bool hasRotVel = mRotVel != 0.0f;
            bool hasCenter = !hasMover;
            bool hasScale = mScale != 1.0f && mSyncType == 1;
            bool hasGrowCounter = mGrowCounter != 0;
            bool hasGrowFlags = mGrowFlags != 0;
            bool hasHighlight = mHighlightCounter != 0;
            theSync.SyncBoolBit(ref mIsRect);
            theSync.SyncBoolBit(ref hasNormalDir);
            theSync.SyncBoolBit(ref hasRotation);
            theSync.SyncBoolBit(ref hasRotVel);
            theSync.SyncBoolBit(ref hasCenter);
            theSync.SyncBoolBit(ref hasScale);
            theSync.SyncBoolBit(ref mTileImage);
            theSync.SyncBoolBit(ref mFlipped);
            theSync.EndBit();
            if (theSync.GetVersion() > 0x22)
            {
                theSync.SyncBoolBit(ref hasGrowCounter);
                theSync.SyncBoolBit(ref hasGrowFlags);
                theSync.SyncBoolBit(ref hasHighlight);
                theSync.SyncBoolBit(ref mGrowHighlight);
                theSync.EndBit();
            }
            if (hasRotation)
            {
                theSync.SyncFloat(ref mRotation);
            }
            if (hasRotVel)
            {
                theSync.SyncFloat(ref mRotVel);
            }
            if (hasScale)
            {
                theSync.SyncFloat(ref mScale);
            }
            if (hasNormalDir)
            {
                theSync.SyncSByte(ref mNormalDir);
            }
            if (hasCenter)
            {
                theSync.SyncFloat(ref mCenterX);
                theSync.SyncFloat(ref mCenterY);
            }
            if (reading && hasMover)
            {
                mRotation = mMover.mRotation;
                mCenterX = mMover.mX;
                mCenterY = mMover.mY;
            }
            if (reading)
            {
                mDrawRotation = mRotation;
            }
            if (syncPoints)
            {
                SyncVectorList(theSync, mPoints);
                if (mSyncType == 3)
                {
                    if (theSync.GetWriter() != null)
                    {
                        EnsureLines();
                    }
                    theSync.SyncFloat(ref mMinX);
                    theSync.SyncFloat(ref mMaxX);
                    theSync.SyncFloat(ref mMinY);
                    theSync.SyncFloat(ref mMaxY);
                    SyncVectorList(theSync, mLineOffsets);
                    theSync.SyncList(mLines, (ds, l) => (Line)ds.SyncRefCount(l, () => new Line()), () => null);
                }
                else if (reading)
                {
                    InitFromPoints();
                }
            }
            if (hasGrowCounter)
            {
                theSync.SyncByte(ref mGrowCounter);
            }
            if (hasGrowFlags)
            {
                theSync.SyncLong(ref mGrowFlags);
            }
            if (hasHighlight)
            {
                theSync.SyncShort(ref mHighlightCounter);
            }
        }

        public static void SyncVectorList(DataSync theSync, List<SexyVector2> theList)
        {
            theSync.SyncList(theList, (ds, v) =>
            {
                ds.SyncFloat(ref v.x);
                ds.SyncFloat(ref v.y);
                return v;
            }, () => new SexyVector2());
        }

        /// <summary>Applies any rotation or move deferred by Update/SetPos before the lines are used.</summary>
        public void EnsureLines()
        {
            if (mNeedRotInit)
            {
                InitRotatedFromPoints(mRotation);
            }
            else if (mNeedPosInit)
            {
                ActualSetPos(mCenterX, mCenterY);
            }
        }

        public void InitFromPoints()
        {
            if (mLines.Count != mPoints.Count - 1)
            {
                mLines.Clear();
                mLineOffsets.Clear();
                for (int i = 0; i < mPoints.Count - 1; i++)
                {
                    Line line = new Line(0.0f, 0.0f, 0.0f, 0.0f);
                    mLines.Add(line);
                    line.mOneSided = mNormalDir != 0;
                    mLineOffsets.Add(new SexyVector2(0.0f, 0.0f));
                }
            }
            if (mRotation == 0.0f)
            {
                for (int i = 0; i < mPoints.Count - 1; i++)
                {
                    SexyVector2 p1 = mPoints[i];
                    SexyVector2 p2 = mPoints[i + 1];
                    Line line = mLines[i];
                    line.Init(p1.x + mCenterX, p1.y + mCenterY, p2.x + mCenterX, p2.y + mCenterY);
                    mLineOffsets[i] = new SexyVector2(line.mX1 - mCenterX, line.mY1 - mCenterY);
                }
                CalcBoundingBox();
            }
            else
            {
                InitRotatedFromPoints(mRotation);
            }
            CheckConvex();
        }

        public override void SyncState(DataSync theSync)
        {
            SyncCommon(theSync, true);
        }

        public void AddPoint(float theX, float theY)
        {
            mPoints.Add(new SexyVector2(theX, theY));
        }

        /// <summary>Bubbles (and a sound) puffing from a "movetoggle" polygon's leading edge.</summary>
        public void AddBubbles(int theSound)
        {
            Board board = GetBoard();
            if (mFlag30 || board == null)
            {
                return;
            }
            if (theSound >= 0)
            {
                board.mSoundMgr.AddSound(theSound);
            }
            float x = mFlipped ? mMaxX : mMinX;
            float y = mCenterY;
            int count = SexyUtil.Rand() % 3 + 2;
            for (int i = 0; i < count; i++)
            {
                int dx = SexyUtil.Rand() % 0x15;
                int dy = SexyUtil.Rand() % 0x15;
                Effect bubble = board.mEffectMgr.AddBubble((int)x + dx - 10, (int)y + dy - 10);
                bubble.mUpdateCnt = -SexyUtil.Rand() % 0x14;
            }
        }

        public override void NotifyCollision(PhysObj theObj)
        {
            if (mLogic.Length != 0 && mLogic == "movetoggle" && !IsMoving() && mMover != null && mMover.mPeriod > 0)
            {
                if (mMoveUpdateCnt % mMover.mPeriod < 10)
                {
                    if (mFlipped)
                    {
                        mFlipped = false;
                        mImageDX *= -1.0f;
                    }
                    SetMovingPercent(0.5f, false);
                    AddBubbles(Res.SOUND_BUBBLES);
                }
                else
                {
                    if (!mFlipped)
                    {
                        mFlipped = true;
                        mImageDX *= -1.0f;
                    }
                    SetMovingPercent(0.0f, false);
                    AddBubbles(Res.SOUND_BUBBLES);
                }
            }
        }

        public void CalcBoundingBox()
        {
            mMinY = 1e12f;
            mMinX = 1e12f;
            mMaxY = -mMinX;
            mMaxX = -mMinX;
            foreach (Line line in mLines)
            {
                if (line.mMinX < mMinX)
                {
                    mMinX = line.mMinX;
                }
                if (mMaxX < line.mMaxX)
                {
                    mMaxX = line.mMaxX;
                }
                if (line.mMinY < mMinY)
                {
                    mMinY = line.mMinY;
                }
                if (mMaxY < line.mMaxY)
                {
                    mMaxY = line.mMaxY;
                }
            }
        }

        public void InitRotatedFromPoints(float theRotation)
        {
            if (mPoints.Count < 2)
            {
                return;
            }
            mNeedRotInit = false;
            mNeedPosInit = false;
            float c = (float)Math.Cos(theRotation);
            float s = (float)Math.Sin(theRotation);
            float px = mPoints[0].x * c + mPoints[0].y * s;
            float py = mPoints[0].y * c - mPoints[0].x * s;
            for (int i = 0; i < mPoints.Count - 1; i++)
            {
                SexyVector2 p = mPoints[i + 1];
                float nx = p.x * c + p.y * s;
                float ny = p.y * c - p.x * s;
                Line line = mLines[i];
                line.Init(px + mCenterX, py + mCenterY, nx + mCenterX, ny + mCenterY);
                mLineOffsets[i] = new SexyVector2(line.mX1 - mCenterX, line.mY1 - mCenterY);
                px = nx;
                py = ny;
            }
            CalcBoundingBox();
        }

        /// <summary>Marks the polygon convex if it is closed and every turn has the same sign.</summary>
        public void CheckConvex()
        {
            if (mPoints.Count < 3)
            {
                return;
            }
            mClosed = mPoints[mPoints.Count - 1] == mPoints[0];
            if (!mClosed)
            {
                mConvex = false;
                return;
            }
            mConvex = true;
            float sign = 0.0f;
            SexyVector2 prev = mPoints[0];
            SexyVector2 prevEdge = mPoints[0] - mPoints[mPoints.Count - 2];
            for (int i = 1; i < mPoints.Count; i++)
            {
                SexyVector2 p = mPoints[i];
                SexyVector2 edge = p - prev;
                float cross = prevEdge.x * edge.y - prevEdge.y * edge.x;
                if ((sign < 0.0f && cross > 0.0f) || (sign > 0.0f && cross < 0.0f))
                {
                    mConvex = false;
                    return;
                }
                if (sign == 0.0f && cross != 0.0f)
                {
                    sign = cross;
                }
                prev = p;
                prevEdge = edge;
            }
        }

        /// <summary>Only refreshes the bounds now; the lines are rotated when next needed.</summary>
        public void DelayedInitRotatedFromPoints(float theRotation)
        {
            mMinY = 1e12f;
            mMinX = 1e12f;
            mMaxY = -mMinX;
            mMaxX = -mMinX;
            float c = (float)Math.Cos(theRotation);
            float s = (float)Math.Sin(theRotation);
            for (int i = 0; i < mPoints.Count; i++)
            {
                float x = mPoints[i].x * c + mPoints[i].y * s + mCenterX;
                float y = (mPoints[i].y * c - mPoints[i].x * s) + mCenterY;
                if (x < mMinX)
                {
                    mMinX = x;
                }
                if (mMaxX < x)
                {
                    mMaxX = x;
                }
                if (y < mMinY)
                {
                    mMinY = y;
                }
                if (mMaxY < y)
                {
                    mMaxY = y;
                }
            }
            mNeedRotInit = true;
        }

        public void SetNormalDir(int theDir)
        {
            if (theDir == -mNormalDir)
            {
                ReverseLines();
            }
            else if (theDir == 0)
            {
                foreach (Line line in mLines)
                {
                    line.mOneSided = false;
                }
            }
            mNormalDir = theDir;
        }

        public void ReverseLines()
        {
            mPoints.Reverse();
            InitFromPoints();
        }

        /// <summary>
        /// Tests every edge; closed polygons (normal dir 1) also detect a ball that ended up inside
        /// and push it out through the nearest edge. Spinning polygons add their surface speed at
        /// the hit point to <paramref name="theSpinVel"/>.
        /// </summary>
        public bool CheckCollision(SexyVector2 thePos, SexyVector2 theVel, float theRadius, ref float theTime, ref SexyVector2 theHitPos,
            ref SexyVector2 theNormal, ref SexyVector2 theHitVel, ref SexyVector2 theSpinVel)
        {
            if (thePos.x + theRadius < mMinX - 20.0f || mMaxX + 20.0f < thePos.x - theRadius
                || thePos.y + theRadius < mMinY - 20.0f || mMaxY + 20.0f < thePos.y - theRadius)
            {
                return false;
            }
            EnsureLines();
            bool convex = mConvex;
            bool checkInside = mNormalDir == 1 && mClosed;
            float bestDist = 1e7f;
            Line bestLine = null;
            Line hitLine = null;
            int crossings = 0;
            foreach (Line line in mLines)
            {
                if (line.CheckCollision(thePos, theVel, theRadius, ref theTime, ref theHitPos, ref theNormal, ref theHitVel, false))
                {
                    hitLine = line;
                }
                if (checkInside)
                {
                    float dist = -(thePos - new SexyVector2(line.mX1, line.mY1)).Dot(line.mNormal);
                    if (!convex)
                    {
                        if (((line.mY2 != line.mY1 && line.mY1 <= thePos.y && thePos.y < line.mY2) || (line.mY2 <= thePos.y && thePos.y < line.mY1))
                            && thePos.x < ((thePos.y - line.mY1) * (line.mX2 - line.mX1)) / (line.mY2 - line.mY1) + line.mX1)
                        {
                            crossings++;
                        }
                        if (dist > 0.0f && dist < bestDist)
                        {
                            bestLine = line;
                            bestDist = dist;
                        }
                    }
                    else if (dist < bestDist)
                    {
                        bestLine = line;
                        bestDist = dist;
                    }
                }
            }
            if (convex && bestDist > 0.0f)
            {
                crossings = 1;
            }
            if ((crossings & 1) != 0 && bestLine != null && theTime != 0.0f)
            {
                theTime = 0.0f;
                theHitPos = thePos + bestLine.mNormal * (bestDist + theRadius);
                bestLine.CalcEdgeHitVelocity(bestLine.mNormal, out theHitVel);
                hitLine = bestLine;
                theNormal = bestLine.mNormal;
            }
            if (hitLine == null)
            {
                return false;
            }
            if (mRotVel != 0.0f)
            {
                float dx = theHitPos.x - mCenterX;
                float dy = theHitPos.y - mCenterY;
                theSpinVel.x += dy * mRotVel;
                theSpinVel.y += (-0.0f - dx) * mRotVel;
            }
            return true;
        }

        public override void Translate(float theDX, float theDY)
        {
            SetPos(mCenterX + theDX, mCenterY + theDY);
        }

        public void ActualSetPos(float theX, float theY)
        {
            mNeedPosInit = false;
            float dx = theX - mCenterX;
            float dy = theY - mCenterY;
            mCenterX = theX;
            mCenterY = theY;
            mMinX += dx;
            mMaxX += dx;
            mMinY += dy;
            mMaxY += dy;
            for (int i = 0; i < mLines.Count; i++)
            {
                SexyVector2 offset = mLineOffsets[i];
                mLines[i].SetPos(mCenterX + offset.x, mCenterY + offset.y);
            }
        }

        /// <summary>Moves the bounds now and the lines lazily (EnsureLines).</summary>
        public override void SetPos(float theX, float theY)
        {
            float oldX = mCenterX;
            float oldY = mCenterY;
            mCenterX = theX;
            mCenterY = theY;
            mMinX += theX - oldX;
            mMaxX += theX - oldX;
            mMinY += theY - oldY;
            mMaxY += theY - oldY;
            mNeedPosInit = true;
        }

        public override void SetVelocity(float theVX, float theVY)
        {
            foreach (Line line in mLines)
            {
                line.SetVelocity(theVX, theVY);
            }
        }

        public void SetRotation(float theRotation)
        {
            if (mRotation != theRotation)
            {
                float rot = SexyUtil.GetCanonicalAngleRad(theRotation);
                mDrawRotation = rot;
                mRotation = rot;
                if (mMover != null)
                {
                    mMover.mBaseRotation = mRotation;
                }
                InitRotatedFromPoints(mRotation);
            }
        }

        public void SetScale(float theScale)
        {
            if (theScale > 0.0f)
            {
                float oldScale = mScale;
                for (int i = 0; i < mPoints.Count; i++)
                {
                    mPoints[i] = mPoints[i] * (theScale / oldScale);
                }
                mScale = theScale;
                InitFromPoints();
            }
        }

        public void MoveCenterTo(float theX, float theY)
        {
            float dx = theX - mCenterX;
            float dy = theY - mCenterY;
            for (int i = 0; i < mPoints.Count; i++)
            {
                mPoints[i] = new SexyVector2(mPoints[i].x - dx, mPoints[i].y - dy);
            }
            mCenterX += dx;
            mCenterY += dy;
            mImageDX -= dx;
            mImageDY -= dy;
            mMover?.Translate(dx, dy);
            InitFromPoints();
        }

        public void Close()
        {
            if (mPoints.Count < 2)
            {
                return;
            }
            SexyVector2 front = mPoints[0];
            SexyVector2 back = mPoints[mPoints.Count - 1];
            float dsq = (front.x - back.x) * (front.x - back.x) + (front.y - back.y) * (front.y - back.y);
            if (dsq < 100.0f)
            {
                mPoints[0] = back;
            }
            else
            {
                mPoints.Add(front);
            }
            InitFromPoints();
        }

        /// <summary>An unrotated rectangle's screen rect (in device pixels), for fills and tiling.</summary>
        public bool GetStandardRect(out TRect theRect)
        {
            theRect = new TRect();
            if (!mIsRect || mLines.Count != 4 || mRotation != 0.0f)
            {
                return false;
            }
            EnsureLines();
            Line first = mLines[0];
            Line third = mLines[mLines.Count - 2];
            float scale = SexyUtil.gPhysicalDisplayHeight;
            theRect = new TRect((int)((first.mX1 * scale) / 600.0), (int)((first.mY1 * scale) / 600.0),
                (int)(((third.mX1 - first.mX1) * scale) / 600.0), (int)(((third.mY1 - first.mY1) * scale) / 600.0));
            return true;
        }

        public override void SetActiveWithGrowAnim(bool active)
        {
            bool wasVisible = false;
            if (mVisible)
            {
                wasVisible = true;
                if (mGrowCounter != 0)
                {
                    wasVisible = (mGrowFlags & 0x100) == 0;
                }
            }
            SetActive(active);
            if (wasVisible != mVisible)
            {
                if (active)
                {
                    mGrowFlags &= ~0x100;
                }
                else
                {
                    mGrowFlags |= 0x100;
                    mVisible = true;
                }
                mGrowCounter = 0x14;
            }
        }

        public void DrawPoly(Graphics g, int theFillColor, int theOutlineColor)
        {
            EnsureLines();
            float scale = SexyUtil.gPhysicalDisplayHeight;
            if (theFillColor != 0)
            {
                g.SetColor(SexyUtil.MakeColor(theFillColor));
                if (GetStandardRect(out TRect rect))
                {
                    g.FillRect(rect);
                }
                else if (mLines.Count > 1)
                {
                    int count = Math.Min(100, mLines.Count);
                    TPoint[] points = new TPoint[count];
                    for (int i = 0; i < count; i++)
                    {
                        Line line = mLines[i];
                        points[i] = new TPoint((int)((line.mX1 * scale) / 600.0), (int)((line.mY1 * scale) / 600.0));
                    }
                    g.PolyFill(points, count);
                }
            }
            if (theOutlineColor != 0)
            {
                g.SetColor(SexyUtil.MakeColor(theOutlineColor));
                foreach (Line line in mLines)
                {
                    g.DrawLine((int)((line.mX1 * scale) / 600.0), (int)((line.mY1 * scale) / 600.0),
                        (int)((line.mX2 * scale) / 600.0), (int)((line.mY2 * scale) / 600.0));
                }
            }
        }

        public override void DrawShadow(Graphics g)
        {
            if (mShadow && mPegInfo != null && mImage != null && mOutlineMode != 2 && g.Is3D())
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0, 0xff));
                DrawImage(g, mCenterX - 3.0f, mCenterY + 3.0f, mDrawRotation, true, mFlipped);
                g.SetColorizeImages(false);
            }
        }

        public override void DrawLight(Graphics g)
        {
            base.DrawLight(g);
        }

        public override void Draw(Graphics g)
        {
            if (mImage == null || mOutlineMode == 2)
            {
                DrawPoly(g, (int)mValue40, (int)mColor);
                return;
            }
            if (mTileImage && GetStandardRect(out TRect rect))
            {
                // The rect is already in device pixels, yet the original scales it again here.
                float scale = SexyUtil.gPhysicalDisplayHeight;
                for (int x = 0; x < rect.mWidth; x += mImage.mWidth)
                {
                    int w = mImage.mWidth;
                    if (rect.mWidth <= x + w)
                    {
                        w = rect.mWidth - x;
                    }
                    for (int y = 0; y < rect.mHeight; y += mImage.mHeight)
                    {
                        int h = mImage.mHeight;
                        if (rect.mHeight <= y + h)
                        {
                            h = rect.mHeight - y;
                        }
                        g.DrawImage(mImage, (int)(scale * (rect.mX + x) / 600.0), (int)(scale * (rect.mY + y) / 600.0),
                            new TRect(0, 0, (int)(w * scale / 600.0), (int)(h * scale / 600.0)));
                    }
                }
            }
            else
            {
                bool useFloat = false;
                if (!mBackground)
                {
                    useFloat = true;
                    if (!g.Is3D())
                    {
                        useFloat = mDrawFloat;
                    }
                }
                if (mGrowCounter != 0 && mGrowFlags != 0)
                {
                    DrawImage(g, mCenterX, mCenterY, mDrawRotation, useFloat, mGrowFlags, mGrowCounter / 20.0f);
                }
                else
                {
                    bool dim = false;
                    if (mAllowDim && mImage != null)
                    {
                        Board board = GetBoard();
                        if (board != null && board.mLogicMgr != null && board.mLogicMgr.mDimLowObjects && mId != "flipper")
                        {
                            dim = mCenterY + mImage.mHeight >= 530.0f;
                        }
                    }
                    if (dim)
                    {
                        g.SetColorizeImages(true);
                        g.SetColor(SexyUtil.MakeColor(0xffffff, 0xf0));
                    }
                    DrawImage(g, mCenterX, mCenterY, mDrawRotation, useFloat, mFlipped);
                    if (dim)
                    {
                        g.SetColorizeImages(false);
                        g.SetColor(SexyUtil.MakeColor(0xffffff));
                    }
                    if (mHighlightCounter > 0 && (mHighlightCounter / 0x14) % 2 == 0)
                    {
                        g.SetColor(SexyUtil.MakeColor(0x808080));
                        g.SetColorizeImages(true);
                        g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                        DrawImage(g, mCenterX, mCenterY, mDrawRotation, useFloat, mFlipped);
                        g.SetColorizeImages(false);
                        g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                    }
                }
            }
            if (mOutlineMode == 1)
            {
                DrawPoly(g, 0, 0xffffff);
            }
        }

        public override void Update()
        {
            base.Update();
            if (mMover != null)
            {
                float rot = mMover.mRotation;
                if (rot == mRotation)
                {
                    if (mRotVel != 0.0f)
                    {
                        mStoppedRotating = true;
                    }
                    mRotVel = 0.0f;
                }
                else
                {
                    mRotVel = rot - mRotation;
                    while (mRotVel > 3.1415927f)
                    {
                        mRotVel -= 6.2831855f;
                    }
                    while (mRotVel < -3.1415927f)
                    {
                        mRotVel += 6.2831855f;
                    }
                    mDrawRotation = rot;
                    mRotation = rot;
                    DelayedInitRotatedFromPoints(mRotation);
                }
            }
            if (mLogic.Length != 0 && mLogic == "movetoggle" && mMoveCounter == 0x28)
            {
                mFlipped = !mFlipped;
                mImageDX = -0.0f - mImageDX;
                AddBubbles(Res.SOUND_FLIP);
            }
            if (mGrowCounter > 0)
            {
                mGrowCounter--;
                if ((mGrowFlags & 0x100) == 0)
                {
                    if ((mGrowFlags & 0x1000) != 0 && mGrowHighlight)
                    {
                        mHighlightCounter = 100;
                    }
                }
                else
                {
                    mVisible = mGrowCounter != 0;
                }
            }
            if (mHighlightCounter > 0)
            {
                mHighlightCounter--;
            }
        }

        public override void UpdateSlowMo(float theFraction)
        {
            if (mMover != null)
            {
                float rot = mMover.mRotation;
                base.UpdateSlowMo(theFraction);
                float newRot = mMover.mRotation;
                mMover.mRotation = rot;
                mDrawRotation = newRot;
                if (newRot != mRotation)
                {
                    InitRotatedFromPoints(newRot);
                }
            }
        }

        /// <summary>Even-odd ray test against the edges.</summary>
        public bool PointInside(float theX, float theY)
        {
            EnsureLines();
            int crossings = 0;
            foreach (Line line in mLines)
            {
                float y1 = line.mY1;
                float y2 = line.mY2;
                if (((y2 != y1 && y1 <= theY && theY < y2) || (y2 <= theY && theY < y1))
                    && theX < ((theY - y1) * (line.mX2 - line.mX1)) / (y2 - y1) + line.mX1)
                {
                    crossings++;
                }
            }
            return (crossings & 1) != 0;
        }

        public override SexyVector2 GetCenter()
        {
            return new SexyVector2(mCenterX, mCenterY);
        }

        public override int GetClass()
        {
            return CLASS_POLY;
        }

        public override float GetXPos()
        {
            return mCenterX;
        }

        public override float GetYPos()
        {
            return mCenterY;
        }
    }
}
