using System;

namespace Sexy
{
    /// <summary>
    /// A brick peg (Sexy::Brick): a Poly generated from arc parameters (inner radius, width,
    /// sweep, angle) whose edges can each be curved or straight, drawn as a textured triangle
    /// strip mapped across the brick image.
    /// </summary>
    public class Brick : Poly
    {
        /// <summary>Cleared by Board.LoadLevel around LoadLevel2; nothing in this build reads it.</summary>
        public static bool mGenImageOnLoad = true;

        public const int BRICK_CURVED = 0;

        public const int BRICK_STRAIGHT_INNER = 1;

        public const int BRICK_SHIFTED_INNER = 2;

        public const int BRICK_STRAIGHT_OUTER = 3;

        public const int BRICK_SHIFTED_OUTER = 4;

        public const int BRICK_STRAIGHT = 5;

        private static readonly ResourceId[] NormalIds =
        {
            ResourceId.IMAGE_BRICK0_ID, ResourceId.IMAGE_BRICK1_ID, ResourceId.IMAGE_BRICK2_ID, ResourceId.IMAGE_BRICK3_ID,
            ResourceId.IMAGE_BRICK4_ID, ResourceId.IMAGE_BRICK5_ID, ResourceId.IMAGE_BRICK6_ID, ResourceId.IMAGE_BRICK7_ID
        };

        private static readonly ResourceId[] ColorBlindIds =
        {
            ResourceId.IMAGE_BRICKCB0_ID, ResourceId.IMAGE_BRICKCB1_ID, ResourceId.IMAGE_BRICKCB2_ID, ResourceId.IMAGE_BRICKCB3_ID,
            ResourceId.IMAGE_BRICKCB4_ID, ResourceId.IMAGE_BRICKCB5_ID, ResourceId.IMAGE_BRICKCB6_ID, ResourceId.IMAGE_BRICKCB7_ID
        };

        public float mInnerRadius;                                // 0xf4

        public float mSweepAngle;                                 // 0xf8 (degrees)

        public float mLeftAngle;                                  // 0xfc (degrees, straight bricks)

        public float mRightAngle;                                 // 0x100 (degrees, straight bricks)

        public float mWidth;                                      // 0x104

        public float mAngle;                                      // 0x108 (degrees)

        public float mOffsetX;                                    // 0x10c

        public float mOffsetY;                                    // 0x110

        public int mCurvePoints;                                  // 0x114

        public int mBrickType;                                    // 0x118

        public int[] mCorners = new int[4];                       // 0x11c

        public bool mTextureFlip;                                 // 0x12c

        public bool mAnchorLeft;                                  // 0x12d

        public bool mAnchorRight;                                 // 0x12e

        public TriVertex[] mTexVerts = new TriVertex[30];         // 0x130

        public TriVertex[] mDrawVerts = new TriVertex[30];        // 0x388

        public float mTexMinX;                                    // 0x5e0

        public float mTexMinY;                                    // 0x5e4

        public float mTexWidth;                                   // 0x5e8

        public float mTexHeight;                                  // 0x5ec

        public float mLastDrawX;                                  // 0x5f0

        public float mLastDrawY;                                  // 0x5f4

        public int mNumVerts;                                     // 0x5f8

        public float mLastDrawRotation;                           // 0x5fc

        public bool mLastZoomed;                                  // 0x600

        public Brick()
        {
            mInnerRadius = 35.0f;
            mSweepAngle = 30.0f;
            mWidth = 20.0f;
            mAngle = -90.0f;
            mLeftAngle = 0;
            mRightAngle = 0;
            mOffsetX = 0;
            mOffsetY = 0;
            mAnchorLeft = false;
            mAnchorRight = false;
            mTextureFlip = false;
            mCurvePoints = 2;
            mBrickType = BRICK_CURVED;
            mNumVerts = 0;
            mLastDrawX = -1.0f;
            mLastDrawY = -1.0f;
            mLastDrawRotation = -32767.0f;
            mLastZoomed = false;
        }

        public override void PegChangedHook(bool isPeg)
        {
        }

        public override void SyncState(DataSync theSync)
        {
            int defaultCurvePoints = 1;
            SyncCommon(theSync, mSyncType == 3);
            if (theSync.GetReader() != null)
            {
                mBrickType = BRICK_CURVED;
                mCurvePoints = defaultCurvePoints;
                mWidth = 20.0f;
                mOffsetY = 0;
                mOffsetX = 0;
            }
            bool hasType = mBrickType != BRICK_CURVED;
            bool hasCurvePoints = false;
            if (mCurvePoints != 2)
            {
                hasCurvePoints = mBrickType != BRICK_STRAIGHT;
            }
            bool hasLeftAngle = mLeftAngle != 0.0f;
            bool hasRightAngle = mRightAngle != 0.0f;
            bool hasSweep = mBrickType != BRICK_STRAIGHT;
            bool hasWidth = mWidth != 20.0f;
            bool hasOffsetX = mOffsetX != 0.0f;
            bool hasOffsetY = mOffsetY != 0.0f;
            theSync.SyncBoolBit(ref mAnchorLeft);
            theSync.SyncBoolBit(ref mAnchorRight);
            theSync.SyncBoolBit(ref hasType);
            theSync.SyncBoolBit(ref hasCurvePoints);
            theSync.SyncBoolBit(ref hasSweep);
            theSync.SyncBoolBit(ref hasLeftAngle);
            theSync.SyncBoolBit(ref hasRightAngle);
            theSync.SyncBoolBit(ref hasWidth);
            theSync.EndBit();
            theSync.SyncBoolBit(ref hasOffsetX);
            theSync.SyncBoolBit(ref hasOffsetY);
            theSync.SyncBoolBit(ref mTextureFlip);
            theSync.EndBit();
            if (hasOffsetX)
            {
                theSync.SyncFloat(ref mOffsetX);
            }
            if (hasOffsetY)
            {
                theSync.SyncFloat(ref mOffsetY);
            }
            if (hasType)
            {
                theSync.SyncByte(ref mBrickType);
            }
            if (hasCurvePoints)
            {
                theSync.SyncByte(ref mCurvePoints);
            }
            if (hasLeftAngle)
            {
                theSync.SyncFloat(ref mLeftAngle);
            }
            if (hasRightAngle)
            {
                theSync.SyncFloat(ref mRightAngle);
            }
            if (hasRightAngle)
            {
                // The original syncs the right angle twice.
                theSync.SyncFloat(ref mRightAngle);
            }
            if (hasSweep)
            {
                theSync.SyncFloat(ref mSweepAngle);
            }
            if (hasWidth)
            {
                theSync.SyncFloat(ref mWidth);
            }
            theSync.SyncFloat(ref mInnerRadius);
            theSync.SyncFloat(ref mAngle);
            for (int i = 0; i < 4; i++)
            {
                theSync.SyncByte(ref mCorners[i]);
            }
            if (theSync.GetReader() != null && mSyncType != 3)
            {
                InitFromParams();
            }
        }

        private static SexyVector2 ArcPoint(float theRadius, float theAngle)
        {
            return new SexyVector2(theRadius * (float)Math.Cos(theAngle), (-0.0f - theRadius) * (float)Math.Sin(theAngle));
        }

        private static void SnapEnds(ref SexyVector2 p1, ref SexyVector2 p2)
        {
            if (Math.Abs(p1.x - p2.x) < 0.01f)
            {
                p2.x = p1.x;
            }
            else if (Math.Abs(p1.y - p2.y) < 0.01f)
            {
                p2.y = p1.y;
            }
        }

        /// <summary>
        /// Builds the outline: the inner edge from the high-angle end to the low one, then the
        /// outer edge back, each curved (mCurvePoints intermediate points) or straight per type,
        /// centred on the midpoint of the two edges plus the offset.
        /// </summary>
        public void InitFromParams()
        {
            int type = mBrickType;
            bool curvedInner = type != BRICK_STRAIGHT_INNER && type != BRICK_SHIFTED_INNER;
            bool curvedOuter = type != BRICK_STRAIGHT_OUTER && type != BRICK_SHIFTED_OUTER;
            if (type == BRICK_STRAIGHT)
            {
                if (mInnerRadius > 600.0f)
                {
                    mInnerRadius = 600.0f;
                }
                curvedInner = false;
                curvedOuter = false;
            }
            int segs = Math.Max(mCurvePoints, 0);
            SexyVector2 p1 = new SexyVector2();
            SexyVector2 p2 = new SexyVector2();
            int count = 5;
            if (curvedInner)
            {
                count = segs + 5;
            }
            if (curvedOuter)
            {
                count = segs + count;
            }
            float rIn = mInnerRadius;
            float rOut = mInnerRadius + mWidth;
            float aMid = SexyUtil.DegToRad(mAngle);
            float aLo = SexyUtil.DegToRad(mAngle - mSweepAngle / 2.0f);
            float aHi = SexyUtil.DegToRad(mAngle + mSweepAngle / 2.0f);
            int idx = 0;
            mPoints.Clear();
            for (int i = 0; i < count; i++)
            {
                mPoints.Add(new SexyVector2());
            }
            SexyVector2 innerMid;
            SexyVector2 outerMid;
            if (curvedInner)
            {
                mCorners[3] = idx;
                mPoints[idx++] = ArcPoint(rIn, aHi);
                for (int i = 0; i < segs; i++)
                {
                    float t = (i + 1) / (float)(segs + 1);
                    float a = t * aLo + (1.0f - t) * aHi;
                    mPoints[idx++] = ArcPoint(rIn, a);
                }
                mCorners[0] = idx;
                mPoints[idx++] = ArcPoint(rIn, aLo);
                innerMid = (mPoints[(segs + 1) / 2] + mPoints[(segs + 2) / 2]) / 2.0f;
            }
            else
            {
                if (type == BRICK_SHIFTED_INNER)
                {
                    SexyVector2 dir = new SexyVector2((float)Math.Cos(aMid), -0.0f - (float)Math.Sin(aMid));
                    p1 = ArcPoint(rOut, aLo) - dir * mWidth;
                    p2 = ArcPoint(rOut, aHi) - dir * mWidth;
                }
                else if (type == BRICK_STRAIGHT)
                {
                    SexyVector2 v = ArcPoint(rIn, aMid);
                    SexyVector2 h = new SexyVector2((mInnerRadius * (float)Math.Sin(aMid)) / 2.0f, (mInnerRadius * (float)Math.Cos(aMid)) / 2.0f);
                    p1 = v + h;
                    p2 = v - h;
                }
                else
                {
                    p1 = ArcPoint(rIn, aLo);
                    p2 = ArcPoint(rIn, aHi);
                }
                SnapEnds(ref p1, ref p2);
                mCorners[3] = idx;
                mPoints[idx++] = p2;
                mCorners[0] = 1;
                mPoints[idx++] = p1;
                innerMid = (p1 + p2) / 2.0f;
            }
            if (curvedOuter)
            {
                int start = idx;
                mCorners[1] = idx;
                mPoints[idx++] = ArcPoint(rOut, aLo);
                for (int i = 0; i < segs; i++)
                {
                    float t = (i + 1) / (float)(segs + 1);
                    float a = (1.0f - t) * aLo + t * aHi;
                    mPoints[idx++] = ArcPoint(rOut, a);
                }
                mCorners[2] = idx;
                mPoints[idx++] = ArcPoint(rOut, aHi);
                outerMid = (mPoints[start + (segs + 1) / 2] + mPoints[start + (segs + 2) / 2]) / 2.0f;
            }
            else
            {
                if (type == BRICK_SHIFTED_OUTER)
                {
                    SexyVector2 dir = new SexyVector2((float)Math.Cos(aMid), -0.0f - (float)Math.Sin(aMid));
                    p1 = ArcPoint(rIn, aLo) + dir * mWidth;
                    p2 = ArcPoint(rIn, aHi) + dir * mWidth;
                }
                else if (type == BRICK_STRAIGHT)
                {
                    float a1 = aMid + SexyUtil.DegToRad(mLeftAngle);
                    float a2 = aMid + SexyUtil.DegToRad(mRightAngle);
                    p1 = p1 + ArcPoint(mWidth, a1);
                    p2 = p2 + ArcPoint(mWidth, a2);
                }
                else
                {
                    p1 = ArcPoint(rOut, aLo);
                    p2 = ArcPoint(rOut, aHi);
                }
                SnapEnds(ref p1, ref p2);
                mCorners[1] = idx;
                mPoints[idx++] = p1;
                mCorners[2] = idx;
                mPoints[idx++] = p2;
                outerMid = (p1 + p2) / 2.0f;
            }
            mPoints[idx] = mPoints[0];
            SexyVector2 center = (innerMid + outerMid) / 2.0f;
            center.x += mOffsetX;
            center.y += mOffsetY;
            for (int i = 0; i < mPoints.Count; i++)
            {
                mPoints[i] = mPoints[i] - center;
            }
            InitFromPoints();
        }

        /// <summary>World positions of the four corner points (inner-low, outer-low, inner-high, outer-high order of mCorners).</summary>
        public bool GetCorners(SexyVector2[] theCorners)
        {
            EnsureLines();
            for (int i = 0; i < 4; i++)
            {
                int idx = mCorners[i];
                if (idx >= mLines.Count)
                {
                    return false;
                }
                Line line = mLines[idx];
                theCorners[i] = new SexyVector2(line.mX1, line.mY1);
            }
            return true;
        }

        public Image GetBrickImage(int theIndex)
        {
            if (mPegInfo == null || theIndex < 0 || theIndex >= NormalIds.Length)
            {
                return null;
            }
            return Res.GetImageById(PegInfo.mColorblind ? ColorBlindIds[theIndex] : NormalIds[theIndex]);
        }

        /// <summary>
        /// Builds the texture strip: pairs of points walked along the inner and outer edges, with
        /// u running across the image and v picking its top or bottom row.
        /// </summary>
        public void ConfigureBrick(int theIndex)
        {
            Image image = GetBrickImage(theIndex);
            if (image == null)
            {
                return;
            }
            float w = image.mWidth;
            float h = image.mHeight;
            float uHi = (w - 1.0f) / w;
            float uLo = 1.0f / w;
            float vHi = (h - 1.0f) / h;
            float vLo = 1.0f / h;
            int n1 = Math.Abs(mCorners[3] - mCorners[0]) + 1;
            int n2 = Math.Abs(mCorners[2] - mCorners[1]) + 1;
            int dir1 = mCorners[3] <= mCorners[0] ? -1 : 1;
            int dir2 = mCorners[2] <= mCorners[1] ? -1 : 1;
            int i1 = mCorners[0];
            int i2 = mCorners[1];
            int n = Math.Max(n1, n2);
            mNumVerts = 0;
            float vInner = vLo;
            float vOuter = vHi;
            if (mTextureFlip)
            {
                (vInner, vOuter) = (vOuter, vInner);
            }
            for (int k = 0; k < n; k++)
            {
                if (n2 < n1)
                {
                    i1 = mCorners[0] + k * dir1;
                    i2 = mCorners[1] + (n2 * k * dir2) / (n - 1);
                }
                else if (n1 < n2)
                {
                    i1 = mCorners[0] + (n1 * k * dir1) / n;
                    i2 = mCorners[1] + k * dir2;
                }
                else
                {
                    i1 = mCorners[0] + k * dir1;
                    i2 = mCorners[1] + k * dir2;
                }
                SexyVector2 pa = mPoints[i1];
                SexyVector2 pb = mPoints[i2];
                float ta = (float)(Math.Abs((double)(i1 - mCorners[0])) / (n1 - 1));
                float tb = (float)(Math.Abs((double)(i2 - mCorners[1])) / (n2 - 1));
                float ua = ta * uHi + (1.0f - ta) * uLo;
                float ub = tb * uHi + (1.0f - tb) * uLo;
                mTexVerts[mNumVerts++] = new TriVertex(pa.x, pa.y, ua, vInner);
                mTexVerts[mNumVerts++] = new TriVertex(pb.x, pb.y, ub, vOuter);
            }
            mTexMinX = 32767.0f;
            mTexMinY = mTexMinX;
            mTexWidth = -0.0f - mTexMinX;
            mTexHeight = mTexWidth;
            for (int i = 0; i < mNumVerts; i++)
            {
                mTexMinX = Math.Min(mTexMinX, mTexVerts[i].x);
                mTexMinY = Math.Min(mTexMinY, mTexVerts[i].y);
                mTexWidth = Math.Max(mTexWidth, mTexVerts[i].x);
                mTexHeight = Math.Max(mTexHeight, mTexVerts[i].y);
            }
            mImageDX = (mTexMinX + mTexWidth) / 2.0f;
            mImageDY = (mTexMinY + mTexHeight) / 2.0f;
            mTexMinX -= 1.0f;
            mTexMinY -= 1.0f;
            mTexWidth -= mTexMinX;
            mTexHeight -= mTexMinY;
        }

        /// <summary>Draws the strip, re-transforming the cached vertices only when the brick moved or turned.</summary>
        public void DrawBrick(Graphics g, int theIndex)
        {
            Board board = GetBoard();
            bool zoomed = board != null && board.mZoom > 0.0f && board.mZoomActive;
            if (mNumVerts == 0)
            {
                ConfigureBrick(theIndex);
                for (int i = 0; i < mNumVerts; i++)
                {
                    mDrawVerts[i].u = mTexVerts[i].u;
                    mDrawVerts[i].v = mTexVerts[i].v;
                }
            }
            float hw = (mTexWidth + 3.0f) / 2.0f;
            float hh = (mTexHeight + 3.0f) / 2.0f;
            float x0 = (mCenterX + mImageDX) - hw;
            float y0 = (mCenterY + mImageDY) - hh;
            Image image = GetBrickImage(theIndex);
            bool linearBlend = g.GetLinearBlend();
            g.SetLinearBlend(true);
            float dx = Math.Abs(x0 - mLastDrawX);
            float dy = Math.Abs(y0 - mLastDrawY);
            bool skip = !mLastZoomed && !zoomed && dx < 1.0f && dy < 1.0f && mDrawRotation == 0.0f && !mStoppedRotating;
            if (!skip)
            {
                mStoppedRotating = false;
                float scale = SexyUtil.gPhysicalDisplayHeight;
                if (mDrawRotation != 0.0f)
                {
                    mLastDrawRotation = mDrawRotation;
                    float ox = hw - mImageDX;
                    float oy = hh - mImageDY;
                    float c = (float)Math.Cos(mDrawRotation);
                    float s = (float)Math.Sin(mDrawRotation);
                    for (int i = 0; i < mNumVerts; i++)
                    {
                        float px = (mTexVerts[i].x - mTexMinX) - ox;
                        float py = (mTexVerts[i].y - mTexMinY) - oy;
                        mDrawVerts[i].x = (float)((x0 + ox + px * c + py * s) * scale / 600.0);
                        mDrawVerts[i].y = (float)((y0 + ((oy + py * c) - px * s)) * scale / 600.0);
                    }
                }
                else
                {
                    for (int i = 0; i < mNumVerts; i++)
                    {
                        mDrawVerts[i].x = (float)(((mTexVerts[i].x - mTexMinX) + x0) * scale / 600.0);
                        mDrawVerts[i].y = (float)(((mTexVerts[i].y - mTexMinY) + y0) * scale / 600.0);
                    }
                }
                mLastDrawX = x0;
                mLastDrawY = y0;
            }
            mLastZoomed = zoomed;
            // While the board zooms the original also runs each vertex through the transform stack;
            // the port's zoom applies the board transform when drawing instead.
            for (int i = 0; i < mNumVerts - 2; i++)
            {
                g.DrawTriangleTex(image, mDrawVerts[i], mDrawVerts[i + 1], mDrawVerts[i + 2]);
            }
            g.SetLinearBlend(linearBlend);
        }

        public override void DrawShadow(Graphics g)
        {
        }

        public override void Draw(Graphics g)
        {
            if (mPegInfo == null)
            {
                base.Draw(g);
            }
            else
            {
                mPegInfo.DrawBrick(this, g, false);
            }
        }

        public override int GetClass()
        {
            return CLASS_BRICK;
        }
    }
}
