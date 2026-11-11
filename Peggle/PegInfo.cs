using System;

namespace Sexy
{
    public static class PegType
    {
        public const int PEG_NONE = 0;

        public const int PEG_NORMAL = 1;

        public const int PEG_GOAL = 2;

        public const int PEG_SCORE = 3;

        public const int PEG_POWERUP = 4;
    }

    /// <summary>
    /// Peg state attached to a level object (Sexy::PegInfo): its colour/type, hit state, the
    /// crumble-away counter, the flash/pulse effects and the electric sparks.
    /// </summary>
    public class PegInfo : EditValObj
    {
        public static bool mColorblind;

        public static bool mFullSized;

        /// <summary>Port addition: the High Quality Pegs option: pegs always use the large images the zoom uses.</summary>
        public static bool mHighQualityPegs;

        /// <summary>
        /// Port addition: the PC Peg Size option. The PC games draw a peg's 20x20 cel (ballpeg.jpg) unscaled at
        /// (x - r, y - r) (PegInfo draw, 0x49e1b0 in PeggleNights.exe), so a radius-10 peg is 20 board units across,
        /// as wide as its hitbox. The mobile cels are ~24 board units across (HD 31 px at 1.28 px per unit, iPhone
        /// 26 px at 1.067). With this on, the mobile art is drawn at the PC size. The hitbox is the level's radius
        /// on every platform (same level files and collision code), so it doesn't change.
        /// </summary>
        public static bool mPCSized
        {
            get
            {
                // While a replay is watched (a replay file, or an instant replay with a restore point to go back
                // to), the mode it was recorded in, so its physics (the fireball radius) and art match.
                Board aBoard = SexyUtil.GetBoard();
                if (aBoard != null && (aBoard.mIsReplay || aBoard.mReplayRestorePoint != null))
                {
                    return aBoard.mReplayPCSized;
                }
                return mPCSizedOption;
            }
        }

        /// <summary>The PC Peg Size option as set (saved as "PCPegSize"); mPCSized is what applies right now.</summary>
        public static bool mPCSizedOption;

        /// <summary>The PC peg cel's size in board units.</summary>
        public const float PC_PEG_SIZE = 20.0f;

        private static readonly ResourceId[] NormalIds =
        {
            ResourceId.IMAGE_BALLPEG0_ID, ResourceId.IMAGE_BALLPEG1_ID, ResourceId.IMAGE_BALLPEG2_ID, ResourceId.IMAGE_BALLPEG3_ID,
            ResourceId.IMAGE_BALLPEG4_ID, ResourceId.IMAGE_BALLPEG5_ID, ResourceId.IMAGE_BALLPEG6_ID, ResourceId.IMAGE_BALLPEG7_ID
        };

        private static readonly ResourceId[] ColorBlindIds =
        {
            ResourceId.IMAGE_BALLPEGCB0_ID, ResourceId.IMAGE_BALLPEGCB1_ID, ResourceId.IMAGE_BALLPEGCB2_ID, ResourceId.IMAGE_BALLPEGCB3_ID,
            ResourceId.IMAGE_BALLPEGCB4_ID, ResourceId.IMAGE_BALLPEGCB5_ID, ResourceId.IMAGE_BALLPEGCB6_ID, ResourceId.IMAGE_BALLPEGCB7_ID
        };

        private static readonly ResourceId[] LargeNormalIds =
        {
            ResourceId.IMAGE_BALLPEG0_LARGE_ID, ResourceId.IMAGE_BALLPEG1_LARGE_ID, ResourceId.IMAGE_BALLPEG2_LARGE_ID, ResourceId.IMAGE_BALLPEG3_LARGE_ID,
            ResourceId.IMAGE_BALLPEG4_LARGE_ID, ResourceId.IMAGE_BALLPEG5_LARGE_ID, ResourceId.IMAGE_BALLPEG6_LARGE_ID, ResourceId.IMAGE_BALLPEG7_LARGE_ID
        };

        private static readonly ResourceId[] LargeColorBlindIds =
        {
            ResourceId.IMAGE_BALLPEGCB0_LARGE_ID, ResourceId.IMAGE_BALLPEGCB1_LARGE_ID, ResourceId.IMAGE_BALLPEGCB2_LARGE_ID, ResourceId.IMAGE_BALLPEGCB3_LARGE_ID,
            ResourceId.IMAGE_BALLPEGCB4_LARGE_ID, ResourceId.IMAGE_BALLPEGCB5_LARGE_ID, ResourceId.IMAGE_BALLPEGCB6_LARGE_ID, ResourceId.IMAGE_BALLPEGCB7_LARGE_ID
        };

        public int mType;                                         // 0x10

        public bool mHit;                                         // 0x14

        public bool mVariable;                                    // 0x15

        public bool mCrumble;                                     // 0x16

        public bool mShowFlower;                                  // 0x17

        public int mHitCount;                                     // 0x18

        public int mCrumbleCount;                                 // 0x1c

        public int mFlashCount;                                   // 0x20

        public int mFlashType;                                    // 0x24

        public float mGlow;                                       // 0x28

        public float mGlowTarget;                                 // 0x2c

        public LightningSparkEffect mSparks;                      // 0x30

        public PegInfo()
        {
            mType = PegType.PEG_NORMAL;
            mHit = false;
            mVariable = true;
            mCrumble = false;
            mShowFlower = false;
            mHitCount = 0;
            mFlashCount = 0;
            mFlashType = 0;
            mCrumbleCount = 0;
            mGlow = 0;
            mGlowTarget = 0;
            mSparks = null;
        }

        public void Reset()
        {
            mHitCount = 0;
            mFlashCount = 0;
            mFlashType = 0;
            mCrumbleCount = 0;
            mGlow = 0;
            mGlowTarget = 0;
        }

        public void SetHit(bool hit)
        {
            mHit = hit;
            mHitCount = hit ? 1 : 0;
        }

        public override void SyncState(DataSync theSync)
        {
            if (theSync.GetReader() != null)
            {
                mHitCount = 0;
                mFlashType = 0;
                mFlashCount = 0;
                mCrumbleCount = 0;
            }
            bool hasHitCount = mHitCount != 0;
            bool hasFlashCount = mFlashCount != 0;
            bool hasFlashType = mFlashType != 0;
            bool hasCrumbleCount = mCrumbleCount != 0;
            theSync.SyncByte(ref mType);
            theSync.SyncBoolBit(ref mHit);
            theSync.SyncBoolBit(ref mVariable);
            theSync.SyncBoolBit(ref hasHitCount);
            theSync.SyncBoolBit(ref mCrumble);
            theSync.SyncBoolBit(ref hasFlashCount);
            theSync.SyncBoolBit(ref hasFlashType);
            theSync.SyncBoolBit(ref mShowFlower);
            theSync.SyncBoolBit(ref hasCrumbleCount);
            theSync.EndBit();
            if (hasHitCount)
            {
                theSync.SyncLong(ref mHitCount);
            }
            if (hasFlashCount)
            {
                theSync.SyncLong(ref mFlashCount);
            }
            if (hasFlashType)
            {
                theSync.SyncByte(ref mFlashType);
            }
            if (hasCrumbleCount)
            {
                theSync.SyncByte(ref mCrumbleCount);
            }
        }

        public void DoFlash(int theFlashType)
        {
            mFlashCount = 1;
            mFlashType = theFlashType;
        }

        public void DoFlashBlink(int theBlinks)
        {
            mFlashCount = theBlinks * 0x32 - 1;
            mFlashType = 10;
        }

        /// <summary>Additive expanding ring from the IMAGE_BIGPULSE* strips (id 0x1a + type).</summary>
        public void DrawBigPulse(Graphics g, int theType, float theX, float theY, int theFrame, int theDirection)
        {
            Image image = Res.GetImageById((ResourceId)((int)ResourceId.IMAGE_01_PULSE_NORMAL_ID + theType));
            int cel;
            if (image.mAnimInfo == null)
            {
                cel = theFrame / 2;
                if (image.mNumCols <= cel)
                {
                    return;
                }
            }
            else
            {
                if (image.mAnimInfo.mTotalAnimTime < theFrame)
                {
                    return;
                }
                cel = image.GetAnimCel(theFrame);
            }
            if (theDirection == 2)
            {
                cel = image.mNumCols - cel - 1;
            }
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            g.DrawImageCel(image, (int)(theX - image.GetCelWidth() / 2), (int)(theY - image.GetCelHeight() / 2), cel);
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
        }

        /// <summary>Colour-blind symbols are not drawn on iOS (empty in the original).</summary>
        public void DrawColorblind(Graphics g, PhysObj theObj)
        {
        }

        public Image GetPegImage(int theIndex, bool large)
        {
            if (large)
            {
                return Res.GetImageById(mColorblind ? LargeColorBlindIds[theIndex] : LargeNormalIds[theIndex]);
            }
            return Res.GetImageById(mColorblind ? ColorBlindIds[theIndex] : NormalIds[theIndex]);
        }

        public void DrawBall(Ball theBall, Graphics g, bool glowPass)
        {
            int index = mType - 1;
            if (mHit)
            {
                index = mType + 3;
            }
            float scale = SexyUtil.gPhysicalDisplayHeight / 600.0f;
            if (mPCSized)
            {
                // Port addition: the edition's art (large under the zoom or High Quality Pegs) at the PC's size.
                Image image = GetPegImage(index, mFullSized);
                int size = (int)(PC_PEG_SIZE * scale + 0.5f);
                int left = (int)Math.Floor(theBall.mX * scale - size / 2.0f + 0.5f);
                int top = (int)Math.Floor(theBall.mY * scale - size / 2.0f + 0.5f);
                g.DrawImage(image, new TRect(left, top, size, size), new TRect(0, 0, image.mWidth, image.mHeight));
            }
            else if (!mFullSized)
            {
                Image image = GetPegImage(index, mFullSized);
                float x = (float)(theBall.mX * scale - image.mWidth / 2);
                float y = (float)(theBall.mY * scale - image.mHeight / 2);
                if (theBall.mVelX == 0.0f && theBall.mVelY == 0.0f)
                {
                    g.DrawImage(image, (int)(x + 0.5f), (int)(y + 0.5f));
                }
                else
                {
                    g.DrawImageF(image, x, y);
                }
            }
            else
            {
                // Full-sized pegs are drawn as two textured triangles at the small peg's size.
                Image large = GetPegImage(index, true);
                Image small = GetPegImage(index, false);
                float w = small.mWidth;
                float h = small.mHeight;
                float cx = theBall.mX * scale;
                float cy = theBall.mY * scale;
                float left = cx - w / 2.0f;
                float top = cy - h / 2.0f;
                g.DrawImage(large, new TRect((int)left, (int)top, (int)w, (int)h), new TRect(0, 0, large.mWidth, large.mHeight));
            }
            if (mFlashType == 10 && mFlashCount != 0 && !mHit)
            {
                mHit = true;
                g.SetColorizeImages(true);
                float pulse = SexyUtil.PulseVal(mFlashCount, 0x14, 10);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(pulse * 200.0f + 0.0f)));
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                g.SetColorizeImages(true);
                DrawBall(theBall, g, false);
                g.SetColorizeImages(false);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                mHit = false;
            }
            else if (!glowPass && mGlow != 0.0f)
            {
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mGlow * 255.0f)));
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                g.SetColorizeImages(true);
                DrawBall(theBall, g, true);
                g.SetColorizeImages(false);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
            }
            if (!glowPass && mSparks != null)
            {
                mSparks.Draw(g, (int)(theBall.mX + 0.0f), (int)(theBall.mY + 0.0f));
            }
        }

        /// <summary>
        /// Port addition (PC Peg Size): the PC peg shadow (Ball::DrawShadow, 0x493f10 in PeggleNights.exe). PC draws
        /// IMAGE_PEGSHADOW, a black disc 20 units across (a circle of the peg's radius when it isn't 10), at alpha
        /// 0x90, centred 3 units left of and 3 units below the peg. Here the peg's own texture is drawn as that
        /// silhouette (colorized black).
        /// </summary>
        public void DrawShadow(Ball theBall, Graphics g)
        {
            int index = mType - 1;
            if (mHit)
            {
                index = mType + 3;
            }
            if (index < 0 || index >= 8)
            {
                return;
            }
            float scale = SexyUtil.gPhysicalDisplayHeight / 600.0f;
            Image image = GetPegImage(index, mFullSized);
            int size = (int)(theBall.mRadius * 2.0f * scale + 0.5f);
            int left = (int)Math.Floor((theBall.mX - PC_SHADOW_OFFSET) * scale - size / 2.0f + 0.5f);
            int top = (int)Math.Floor((theBall.mY + PC_SHADOW_OFFSET) * scale - size / 2.0f + 0.5f);
            Microsoft.Xna.Framework.Color aColor = g.GetColor();
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0x000000, 0x90));
            g.DrawImage(image, new TRect(left, top, size, size), new TRect(0, 0, image.mWidth, image.mHeight));
            g.SetColorizeImages(false);
            g.SetColor(aColor);
        }

        /// <summary>The PC peg shadow's offset in board units: left and down.</summary>
        public const float PC_SHADOW_OFFSET = 3.0f;

        public void DrawBrick(Brick theBrick, Graphics g, bool glowPass)
        {
            int index = mType - 1;
            if (index >= 4 || index < 0)
            {
                return;
            }
            if (mHit)
            {
                index = mType + 3;
            }
            if (PhysObj.mOutlineMode != 2)
            {
                theBrick.DrawBrick(g, index);
                if (mFlashType == 10 && mFlashCount != 0 && !mHit)
                {
                    mHit = true;
                    g.SetColorizeImages(true);
                    float pulse = SexyUtil.PulseVal(mFlashCount, 0x14, 10);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(pulse * 200.0f + 0.0f)));
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    g.SetColorizeImages(true);
                    DrawBrick(theBrick, g, false);
                    g.SetColorizeImages(false);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                    mHit = false;
                }
                else if (!glowPass && mGlow != 0.0f)
                {
                    g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mGlow * 255.0f)));
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    g.SetColorizeImages(true);
                    DrawBrick(theBrick, g, true);
                    g.SetColorizeImages(false);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                }
            }
            if (PhysObj.mOutlineMode == 1)
            {
                theBrick.DrawPoly(g, 0, 0xffffff);
            }
            if (!glowPass && mSparks != null)
            {
                mSparks.Draw(g, (int)(theBrick.mCenterX + 0.0f), (int)(theBrick.mCenterY + 0.0f));
            }
        }

        public void DrawLight(PhysObj theObj, Graphics g)
        {
            if (theObj.mVisible && mColorblind && (mType == PegType.PEG_POWERUP || mType == PegType.PEG_SCORE))
            {
                DrawColorblind(g, theObj);
            }
            float scale = SexyUtil.gPhysicalDisplayHeight / 600.0f;
            if (mHitCount != 0)
            {
                SexyVector2 center = theObj.GetCenter();
                if (mShowFlower)
                {
                    const int fadeStart = 0x50;
                    const int fadeEnd = 100;
                    if (mHitCount < fadeEnd)
                    {
                        Transform transform = new Transform();
                        float flowerScale = 1.0f;
                        if (mHitCount < 10)
                        {
                            flowerScale = (mHitCount * 1.5f) / 10.0f + 0.5f;
                        }
                        else if (mHitCount < 0x14)
                        {
                            flowerScale = (0x14 - mHitCount) / 10.0f + 1.0f;
                        }
                        if (fadeStart < mHitCount)
                        {
                            int alpha = ((fadeEnd - mHitCount) * 0xff) / (fadeEnd - fadeStart);
                            g.SetColorizeImages(true);
                            g.SetColor(new Microsoft.Xna.Framework.Color(0xff, 0xff, 0xff, alpha));
                        }
                        if (flowerScale != 1.0f)
                        {
                            transform.Scale(flowerScale, flowerScale);
                        }
                        g.DrawImageTransform(Res.IMAGE_FLOWER, transform, center.x * scale, center.y * scale);
                        g.SetColorizeImages(false);
                    }
                }
            }
            if (mFlashCount != 0 && mFlashType < 10)
            {
                int frame = mFlashCount;
                SexyVector2 center = theObj.GetCenter();
                float x = center.x * scale;
                DrawBigPulse(g, mType - 1, x, center.y * scale, frame, mFlashType);
            }
        }

        public void IncCrumble()
        {
            if (mCrumbleCount < 3)
            {
                mCrumbleCount++;
            }
        }

        public void Update(PhysObj theObj)
        {
            if (mHitCount != 0)
            {
                mHitCount++;
                if (mCrumble && mCrumbleCount > 2 && mCrumbleCount < 100)
                {
                    mCrumbleCount++;
                    if (mCrumbleCount == 100)
                    {
                        theObj.SetActive(false);
                    }
                }
            }
            if (mFlashCount != 0)
            {
                if (mFlashType == 10)
                {
                    mFlashCount--;
                    if (mFlashCount < 1)
                    {
                        mFlashType = 0;
                        mFlashCount = 0;
                    }
                }
                else
                {
                    mFlashCount++;
                    if (mFlashCount > 100)
                    {
                        mFlashCount = 0;
                    }
                }
            }
            if (mGlowTarget == 2.0f)
            {
                if (mSparks == null)
                {
                    mSparks = new LightningSparkEffect();
                    mSparks.SetScale(0.5f);
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
            if (mGlow < mGlowTarget)
            {
                mGlow = Math.Min(1.0f, Math.Min(mGlowTarget, mGlow + 0.04f));
            }
            else if (mGlowTarget < mGlow)
            {
                mGlow = Math.Max(0.0f, Math.Max(mGlowTarget, mGlow - 0.04f));
            }
        }
    }
}
