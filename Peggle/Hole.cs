using System;

namespace Sexy
{
    /// <summary>
    /// A catch zone (Sexy::Hole): free-ball buckets, Fever score buckets ("fever" id with a
    /// value), teleporter mouths and other rectangular or elliptical triggers. A ball inside it
    /// is reported through PhysObj.mNotifyCollisionFunc.
    /// </summary>
    public class Hole : PhysObj
    {
        public PhysObj mTarget;                                   // 0x9c

        public int mValueA0;                                      // 0xa0

        public int mFlashCount;                                   // 0xa4

        public int mValue;                                        // 0xa8

        public float mX;                                          // 0xac

        public float mY;                                          // 0xb0

        public float mVelX;                                       // 0xb4

        public float mVelY;                                       // 0xb8

        public int mWidth;                                        // 0xbc

        public int mHeight;                                       // 0xc0

        public bool mEllipse;                                     // 0xc4

        public int mUpdateCnt;                                    // 0xc8

        public float mFloatCC;                                    // 0xcc

        public Hole()
        {
            mTarget = null;
            mShapeType = SHAPE_HOLE;
            mFlashCount = 0;
            mEllipse = false;
            mX = 0;
            mY = 0;
            mVelX = 0;
            mVelY = 0;
            mValue = 0;
            mValueA0 = 0;
            mUpdateCnt = 0;
            mFloatCC = -1.0f;
            SetSize(100, 0x14);
        }

        public override void SyncState(DataSync theSync)
        {
            base.SyncState(theSync);
            if (theSync.GetReader() != null)
            {
                mFlashCount = 0;
                mValue = 0;
                mValueA0 = 0;
                mTarget = null;
                mFloatCC = -1.0f;
            }
            bool hasMover = mMover != null;
            bool hasFlash = mFlashCount != 0;
            bool hasPos = !hasMover;
            bool hasValue = mValue != 0;
            bool hasTarget = mTarget != null;
            bool hasValueA0 = mValueA0 != 0;
            bool hasVel = true;
            if (mVelX == 0.0f)
            {
                hasVel = mVelY != 0.0f;
            }
            theSync.SyncBoolBit(ref mEllipse);
            theSync.SyncBoolBit(ref hasFlash);
            theSync.SyncBoolBit(ref hasPos);
            theSync.SyncBoolBit(ref hasValue);
            theSync.SyncBoolBit(ref hasTarget);
            theSync.SyncBoolBit(ref hasValueA0);
            theSync.SyncBoolBit(ref hasVel);
            theSync.EndBit();
            theSync.SyncLong(ref mWidth);
            theSync.SyncLong(ref mHeight);
            if (hasFlash)
            {
                theSync.SyncShort(ref mFlashCount);
            }
            if (hasValue)
            {
                theSync.SyncLong(ref mValue);
            }
            if (hasValueA0)
            {
                theSync.SyncLong(ref mValueA0);
            }
            if (hasTarget)
            {
                mTarget = SyncPhysObj(theSync, mTarget);
            }
            if (!hasPos)
            {
                if (theSync.GetReader() != null && hasMover)
                {
                    mX = mMover.mX;
                    mY = mMover.mY;
                }
            }
            else
            {
                theSync.SyncFloat(ref mX);
                theSync.SyncFloat(ref mY);
            }
            if (!hasVel)
            {
                mVelY = 0;
                mVelX = 0;
            }
            else
            {
                theSync.SyncFloat(ref mVelX);
                theSync.SyncFloat(ref mVelY);
            }
            SetSize(mWidth, mHeight);
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

        public override void SetPos(float theX, float theY)
        {
            Translate(theX - mX, theY - mY);
        }

        public virtual void SetSize(int theWidth, int theHeight)
        {
            mWidth = theWidth;
            mHeight = theHeight;
            mMinX = mX - mWidth / 2.0f;
            mMaxX = mX + mWidth / 2.0f;
            mMinY = mY - mHeight / 2.0f;
            mMaxY = mY + mHeight / 2.0f;
        }

        /// <summary>True when the ball's centre is inside the box (or the ellipse).</summary>
        public bool CheckCollision(Ball theBall)
        {
            float hw = mWidth / 2.0f;
            float hh = mHeight / 2.0f;
            float bx = theBall.mX;
            float by = theBall.mY;
            if (bx < mX - hw || mX + hw < bx || by < mY - hh || mY + hh < by)
            {
                return false;
            }
            if (mEllipse && ((bx - mX) * (bx - mX)) / (hw * hw) + ((by - mY) * (by - mY)) / (hh * hh) > 1.0f)
            {
                return false;
            }
            if (mNotifyCollisionFunc != null)
            {
                mNotifyCollisionFunc(theBall, this);
                SetFlashCount(100);
            }
            return true;
        }

        public void SetFlashCount(int theCount)
        {
            mFlashCount = theCount;
        }

        public override void SetVelocity(float theVX, float theVY)
        {
            mVelX = theVX;
            mVelY = theVY;
        }

        public override void Update()
        {
            base.Update();
            if (mFlashCount != 0)
            {
                mFlashCount--;
            }
            mUpdateCnt++;
        }

        private static void DrawEllipse(Graphics g, float cx, float cy, float rx, float ry, Image theImage)
        {
            TRect src = new TRect(0, 0, theImage.mWidth, theImage.mHeight);
            TRect dest = new TRect((int)(cx - rx), (int)(cy - ry), (int)(rx * 2.0f), (int)(ry * 2.0f));
            bool colorize = g.GetColorizeImages();
            g.SetFastStretch(true);
            g.SetColorizeImages(true);
            g.DrawImage(theImage, dest, src);
            g.SetColorizeImages(colorize);
        }

        /// <summary>Plain outlined box or ellipse, used when the hole has no image (and in outline mode).</summary>
        public void DrawHole(Graphics g, int theFillColor, int theOutlineColor)
        {
            bool flash = mFlashCount > 0 && (mFlashCount / 0xf) % 2 == 0;
            int fill = theFillColor;
            int outline = theOutlineColor;
            if (flash)
            {
                (fill, outline) = (outline, fill);
            }
            float scale = SexyUtil.gPhysicalDisplayHeight;
            int x = (int)((mX * scale) / 600.0);
            int y = (int)((mY * scale) / 600.0);
            int w = (int)(mWidth * SexyUtil.gPhysicalDisplayHeight / 600.0);
            int h = (int)(mHeight * SexyUtil.gPhysicalDisplayHeight / 600.0);
            if (!mEllipse)
            {
                int left = x - w / 2;
                int top = y - h / 2;
                g.SetColor(SexyUtil.MakeColor(fill));
                g.FillRect(left, top, w, h);
                g.SetColor(SexyUtil.MakeColor(outline));
                for (int i = 0; i < 3; i++)
                {
                    g.DrawRect(left + i, top + i, w - i * 2, h - i * 2);
                }
                g.SetColor(SexyUtil.MakeColor(0));
                g.DrawRect(left - 1, top - 1, w + 2, h + 2);
            }
            else
            {
                float rx = w / 2.0f;
                float ry = h / 2.0f;
                g.SetColor(SexyUtil.MakeColor(0));
                DrawEllipse(g, x, y, rx + 1.0f, ry + 1.0f, Res.IMAGE_CIRCLE);
                g.SetColor(SexyUtil.MakeColor(outline));
                DrawEllipse(g, x, y, rx, ry, Res.IMAGE_CIRCLE);
                g.SetColor(SexyUtil.MakeColor(fill));
                DrawEllipse(g, x, y, rx - 3.0f, ry - 3.0f, Res.IMAGE_CIRCLE);
            }
            if (mValue > 0)
            {
                Font font = Res.FONT_PLAIN;
                g.SetFont(font);
                g.SetColor(SexyUtil.MakeColor(outline));
                string text = mValue.ToString();
                int textY = y - font.GetHeight() / 2 + font.GetAscent();
                SexyUtil.DrawStringWithShadow(g, text, x - font.StringWidth(text) / 2, textY, 0, 2, 2);
            }
        }

        /// <summary>
        /// Draws the hole's image; Fever buckets add the halo and score plate, and glow when a
        /// ball has just dropped in.
        /// </summary>
        public override void Draw(Graphics g)
        {
            if (mImage == null || mOutlineMode == 2)
            {
                DrawHole(g, (int)mValue40, (int)mColor);
                return;
            }
            bool flash = mFlashCount > 0 && (mFlashCount / 0xf) % 2 == 0;
            Image lightImage = null;
            int haloOffset = 0;
            float scale = SexyUtil.gPhysicalDisplayHeight;
            if (mSubId != 0 && mId == "fever")
            {
                switch (mSubId)
                {
                case 1:
                case 5:
                    haloOffset = 0x14;
                    break;
                case 2:
                case 4:
                    haloOffset = 0x14;
                    break;
                case 3:
                    haloOffset = 0x19;
                    break;
                }
                ResourceId scoreId;
                switch (mValue)
                {
                case 5000:
                    scoreId = ResourceId.IMAGE_BONUS_09_5000_ID;
                    break;
                case 10000:
                    scoreId = ResourceId.IMAGE_BONUS_08_10000_ID;
                    break;
                case 25000:
                    scoreId = ResourceId.IMAGE_BONUS_07_25000_ID;
                    break;
                case 50000:
                    scoreId = ResourceId.IMAGE_BONUS_06_50000_ID;
                    break;
                case 100000:
                    scoreId = ResourceId.IMAGE_BONUS_05_100000_ID;
                    break;
                default:
                    scoreId = ResourceId.IMAGE_BONUS_08_10000_ID;
                    break;
                }
                Image halo = Res.IMAGE_BONUSHALO;
                g.DrawImage(halo, (int)((mX * scale) / 600.0 - halo.mWidth / 2), (int)(((mY - haloOffset - 40.0f) * scale) / 600.0));
                Image score = Res.GetImageById(scoreId);
                g.DrawImage(score, (int)((mX * scale) / 600.0 - score.mWidth / 2), (int)(((mY - haloOffset - 25.0f) * scale) / 600.0));
                lightImage = Res.GetImageById(scoreId - 5);
            }
            DrawImage(g, mX, mY, 0.0f, g.Is3D(), false);
            if (mOutlineMode == 1)
            {
                DrawHole(g, 0, 0xffffff);
            }
            if (lightImage != null && mFlashCount > 0)
            {
                Image glow = Res.IMAGE_BONUSGLOW;
                switch (mSubId)
                {
                case 1:
                case 5:
                    glow = Res.IMAGE_BONUSGLOW_BLUE;
                    break;
                case 2:
                case 4:
                    glow = Res.IMAGE_BONUSGLOW_GREEN;
                    break;
                }
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                g.DrawImage(glow, (int)((mX * scale) / 600.0 - glow.mWidth / 2), (int)(((mY - 55.0f) * scale) / 600.0));
                if (flash)
                {
                    g.SetColor(SexyUtil.MakeColor(0x808080));
                    g.DrawImage(lightImage, (int)((mX * scale) / 600.0 - lightImage.mWidth / 2), (int)(((mY - haloOffset - 40.0f) * scale) / 600.0));
                }
                g.SetColorizeImages(false);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
            }
        }

        public override void DrawLight(Graphics g)
        {
            base.DrawLight(g);
        }

        public override int GetClass()
        {
            return CLASS_HOLE;
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
