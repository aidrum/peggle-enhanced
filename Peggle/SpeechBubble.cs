using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// A nine-slice speech bubble with an arrow, pre-rendered into a MemoryImage, that pops in and
    /// fades/bounces out (Sexy::SpeechBubble). Used by the Nights ProgressScreen and TrophyRoom.
    /// </summary>
    public class SpeechBubble
    {
#if PEGGLE_IPHONE
        // The iPhone nine-slice pieces meet edge to edge.
        private const int PROGRESS_SPEECH_IPAD_OFFSET_HORZ = 0;
        private const int PROGRESS_SPEECH_IPAD_OFFSET_VERT = 0;
#endif
        public const int STATE_HIDDEN = 0;
        public const int STATE_SHOWING = 1;
        public const int STATE_SHOWN = 2;
        public const int STATE_HIDING = 3;

        // EArrowPlacement: 0 none, 1/2 left (top/bottom), 3/4 right, 5/6 top (left/right),
        // 7/8 bottom (left/right), 9 = keep the current placement (Move only).
        public const int ARROW_NONE = 0;
        public const int ARROW_KEEP = 9;

        // mImages indices.
        private const int BR = 0, BL = 1, B = 2, TL = 3, T = 4, TR = 5, L = 6, R = 7, C = 8, BP = 9, LP = 10, RP = 11, TP = 12;

        public CurvedVal mShowAlpha = new CurvedVal();            // 0x000

        public CurvedVal mShowScale = new CurvedVal();            // 0x074

        public CurvedVal mShowOffsetY = new CurvedVal();          // 0x0e8

        public CurvedVal mHideAlpha = new CurvedVal();            // 0x15c

        public CurvedVal mHideScale = new CurvedVal();            // 0x1d0

        public CurvedVal mHideOffsetY = new CurvedVal();          // 0x244

        public Image[] mImages = new Image[13];                   // 0x2b8

        public SexyVector2 mAttachPoint;                          // 0x2c4

        public int mArrowPlacement;                               // 0x2cc

        public int mShowSound = -1;                               // 0x2d0

        public MemoryImage mImage;                                // 0x2d4

        public bool mDirty;                                       // 0x2d8

        public int mExpireTime = -1;                              // 0x2dc

        public int mPendingState;                                 // 0x2e0

        public int mPendingDelay;                                 // 0x2e4

        public int mTextWidth;                                    // 0x2e8

        public int mTextHeight;                                   // 0x2ec

        public int mMinTextWidth;                                 // 0x2f0

        public int mMinTextHeight;                                // 0x2f4

        public int mTextMaxWidth = 200;                           // 0x2f8

        public int mArrowOffsetX;                                 // 0x2fc

        public int mArrowOffsetY;                                 // 0x300

        public int mJustification = -1;                           // 0x304

        public int mX;                                            // 0x308

        public int mY;                                            // 0x30c

        public int mState;                                        // 0x310

        public string mText = "";                                 // 0x314

        public int mLineSpacing;                                  // 0x318

        public Font mFont;                                        // 0x31c

        public Color mColor;                                      // 0x320

        public bool mKeepImage;                                   // 0x330

        public int mPadX = -5;                                    // 0x334

        public int mPadY = -5;                                    // 0x338

        /// <summary>
        /// Port addition: how far the nine-slice's inner pieces overlap (the edition's own art), and where a
        /// top/bottom arrow's tip sits from the bubble's corner (Layout's PROGRESS_SPEECH_* values). The PC
        /// pieces (WoWCutscene) meet edge to edge, with their own arrow spot.
        /// </summary>
        public int mOverlapX = PROGRESS_SPEECH_IPAD_OFFSET_HORZ;

        public int mOverlapY = PROGRESS_SPEECH_IPAD_OFFSET_VERT;

        public int mArrowTipX = PROGRESS_SPEECH_ATTACH_POINT_BOTTOM_X;

        public int mArrowTipY = PROGRESS_SPEECH_ATTACH_POINT_BOTTOM_Y;

        /// <summary>Port addition: the scale the finished bubble is drawn at, about its arrow tip (WoWCutscene).</summary>
        public float mDrawScale = 1;

        public SpeechBubble() : this(null)
        {
        }

        /// <summary>
        /// Port: theImages (or null for the edition's Nights bubble) are the 13 pieces in mImages order:
        /// BR, BL, B, TL, T, TR, L, R, C, BP (the bottom arrow), LP, RP, TP.
        /// </summary>
        public SpeechBubble(Image[] theImages)
        {
            mLineSpacing = Res.FONT_STAGE.GetMaxCharHeight();
            InitCurves();
            if (theImages != null)
            {
                theImages.CopyTo(mImages, 0);
                return;
            }
            mImages[BR] = Res.IMAGE_TR_NIGHTS_SPEECH_BR;
            mImages[BL] = Res.IMAGE_TR_NIGHTS_SPEECH_BL;
            mImages[B] = Res.IMAGE_TR_NIGHTS_SPEECH_B;
            mImages[TL] = Res.IMAGE_TR_NIGHTS_SPEECH_TL;
            mImages[T] = Res.IMAGE_TR_NIGHTS_SPEECH_T;
            mImages[TR] = Res.IMAGE_TR_NIGHTS_SPEECH_TR;
            mImages[L] = Res.IMAGE_TR_NIGHTS_SPEECH_L;
            mImages[R] = Res.IMAGE_TR_NIGHTS_SPEECH_R;
            mImages[C] = Res.IMAGE_TR_NIGHTS_SPEECH_C;
            mImages[BP] = Res.IMAGE_TR_NIGHTS_SPEECH_BP;
            mImages[LP] = Res.IMAGE_TR_NIGHTS_SPEECH_LP;
            mImages[RP] = Res.IMAGE_TR_NIGHTS_SPEECH_RP;
            mImages[TP] = Res.IMAGE_TR_NIGHTS_SPEECH_TP;
        }

        private void InitCurves()
        {
            mShowScale.SetCurve("b+0,1,0.01,0.15,;1^<         ~~###");
            mShowOffsetY.SetCurve("b+-50,50,0.01,0.15,####        )Y#jG xPP##");
            mShowAlpha.SetCurve("b+0,1,0.01,0.15,####         ~~###");
            mHideScale.SetCurve("b+0,1,0.01,0.1,~###         ~(@[*");
            mHideOffsetY.SetCurve("b+0,20,0.01,0.1,#%Yr         ~~ccC");
            mHideAlpha.SetCurve("b+0,1,0.01,0.1,~###         ~####");
        }

        public bool IsHidden()
        {
            return mState == STATE_HIDDEN;
        }

        public bool IsStatePending()
        {
            return mPendingDelay > 0;
        }

        public void SetFontJustification(int theJustification)
        {
            mJustification = theJustification;
            refreshTextBounds();
        }

        public void SetExpireTime(int theTime)
        {
            mExpireTime = theTime;
        }

        public void SetMinTextHeight(int theHeight)
        {
            mMinTextHeight = theHeight;
            refreshTextBounds();
        }

        public void SetTextMaxWidth(int theWidth)
        {
            mTextMaxWidth = theWidth;
            refreshTextBounds();
        }

        public void SetFontPadding(int thePadX, int thePadY)
        {
            mPadX = thePadX;
            mPadY = thePadY;
            refreshTextBounds();
        }

        public void SetArrowPlacement(int thePlacement, int theOffsetX, int theOffsetY)
        {
            mArrowPlacement = thePlacement;
            mArrowOffsetX = theOffsetX;
            mArrowOffsetY = theOffsetY;
            refreshAttachPoint();
        }

        private void ensureMins()
        {
            mTextWidth = Math.Max(mMinTextWidth, Math.Max(mImages[C].GetWidth(), mTextWidth));
            mTextHeight = Math.Max(mMinTextHeight, mTextHeight);
        }

        /// <summary>Where the arrow tip sits, relative to the bubble image's top-left.</summary>
        private void refreshAttachPoint()
        {
            ensureMins();
            int aTLH = mImages[TL].GetHeight();
            int aTRH = mImages[TR].GetHeight();
            int aTLW = mImages[TL].GetWidth();
            int aLPH = mImages[LP].GetHeight();
            int aRPW = mImages[RP].GetWidth();
            int aTPW = mImages[TP].GetWidth();
            int aLW = mImages[L].GetWidth();
            int aBPH = mImages[BP].GetHeight();
            int aBPW = mImages[BP].GetWidth();
            int aBLW = mImages[BL].GetWidth();
            switch (mArrowPlacement)
            {
            case 0:
                mAttachPoint = new SexyVector2(0f, 0f);
                break;
            case 1:
                mAttachPoint = new SexyVector2(PROGRESS_SPEECH_ATTACH_POINT_LEFT_X, PROGRESS_SPEECH_ATTACH_POINT_LEFT_Y + aTLH + aLPH);
                break;
            case 2:
                mAttachPoint = new SexyVector2(PROGRESS_SPEECH_ATTACH_POINT_LEFT_X, aTLH + mTextHeight + aLPH - PROGRESS_SPEECH_ATTACH_POINT_LEFT_Y);
                break;
            case 3:
                mAttachPoint = new SexyVector2(PROGRESS_SPEECH_ATTACH_POINT_RIGHT_X + aLW + aRPW + mTextWidth, PROGRESS_SPEECH_ATTACH_POINT_RIGHT_Y + aTLH + aTRH);
                break;
            case 4:
                mAttachPoint = new SexyVector2(PROGRESS_SPEECH_ATTACH_POINT_RIGHT_X + aLW + aRPW + mTextWidth, aTLH + mTextHeight + aTRH - PROGRESS_SPEECH_ATTACH_POINT_RIGHT_Y);
                break;
            case 5:
                mAttachPoint = new SexyVector2(mArrowTipX + (aTLW - aTPW), mArrowTipY);
                break;
            case 6:
                mAttachPoint = new SexyVector2(aTLW - aTPW + mTextWidth - mArrowTipX, mArrowTipY);
                break;
            case 7:
                mAttachPoint = new SexyVector2(mArrowTipX + (aBLW - aBPW), aTLH + aBPH + mTextHeight - mArrowTipY);
                break;
            case 8:
                mAttachPoint = new SexyVector2(aBLW - aBPW + mTextWidth - mArrowTipX, aTLH + aBPH + mTextHeight - mArrowTipY);
                break;
            }
            mAttachPoint.x += mArrowOffsetX;
            mAttachPoint.y += mArrowOffsetY;
            mDirty = true;
        }

        public TRect GetExtents()
        {
            return new TRect(mX, mY, mImages[TL].GetWidth() + mTextWidth + mImages[BR].GetWidth(),
                mImages[TL].GetHeight() + mTextHeight + mImages[BR].GetHeight());
        }

        /// <summary>Positions the bubble so its arrow tip is at (theX, theY).</summary>
        public void Move(int theX, int theY, int thePlacement = ARROW_KEEP, int theOffsetX = 0, int theOffsetY = 0)
        {
            if (thePlacement != ARROW_KEEP || theOffsetX != 0 || theOffsetY != 0)
            {
                SetArrowPlacement(thePlacement, theOffsetX, theOffsetY);
            }
            mX = (int)(theX - mAttachPoint.x);
            mY = (int)(theY - mAttachPoint.y);
        }

        public void Draw(Graphics g, int theOffsetX, int theOffsetY)
        {
            if (mState == STATE_HIDDEN || mImage == null)
            {
                return;
            }
            bool hiding = mState == STATE_HIDING;
            int x = mX + theOffsetX;
            int y = (int)(mY + theOffsetY + (double)(hiding ? mHideOffsetY : mShowOffsetY));
            if (mArrowPlacement == 1 || mArrowPlacement == 2)
            {
                x -= mImages[LP].GetWidth();
            }
            g.SetColor(SexyUtil.MakeColor(0xffffff, (int)((double)(hiding ? mHideAlpha : mShowAlpha) * 255.0)));
            g.SetColorizeImages(true);
            double aScale = hiding ? mHideScale : mShowScale;
            int anArrowY = 0;
            if (mArrowPlacement == 5 || mArrowPlacement == 6)
            {
                anArrowY = -mImages[TP].GetHeight();
            }
            double w = mImage.GetWidth();
            double h = mImage.GetHeight();
            double aDrawX = x + w * (1.0 - aScale) / 2.0;
            double aDrawY = anArrowY + y + h * (1.0 - aScale) / 2.0;
            if (mDrawScale != 1)
            {
                // Port: scaled about the arrow tip, so the tip stays where Move put it.
                double aTipX = mX + theOffsetX + mAttachPoint.x;
                double aTipY = mY + theOffsetY + mAttachPoint.y;
                aDrawX = aTipX + (aDrawX - aTipX) * mDrawScale;
                aDrawY = aTipY + (aDrawY - aTipY) * mDrawScale;
                aScale *= mDrawScale;
            }
            g.DrawImage(mImage, (int)aDrawX, (int)aDrawY, (int)(w * aScale), (int)(h * aScale));
            g.SetColorizeImages(false);
            g.SetColor(Color.White);
        }

        public void RefreshImage()
        {
            if (mKeepImage && mImage != null)
            {
                return;
            }
            mDirty = false;
            int anOffX = 0;
            int anOffY = 0;
            mImage = null;
            int aY = anOffY;
            ensureMins();
            int anExtraW = 0;
            int anExtraH = 0;
            switch (mArrowPlacement)
            {
            case 1:
            case 2:
                anOffX = mImages[LP].GetWidth();
                anExtraW = mImages[LP].GetWidth();
                break;
            case 3:
            case 4:
                anExtraW = mImages[RP].GetWidth();
                break;
            case 5:
            case 6:
                anOffY = mImages[TP].GetHeight();
                anExtraH = mImages[TP].GetHeight();
                aY += anExtraH;
                break;
            case 7:
            case 8:
                anExtraH = mImages[BP].GetHeight();
                break;
            }
            TRect anExtents = GetExtents();
            MemoryImage anImage = new MemoryImage();
            anImage.Create(anExtents.mWidth + anExtraW, anExtraH + anExtents.mHeight);
            mImage = anImage;
            bool inFrame = Graphics.IsSpriteBatchBegun();
            Graphics g = Graphics.GetNew(anImage);

            // Nine-slice body; the inner edges overlap by the iPad offsets.
            int x = anOffX;
            g.DrawImage(mImages[TL], x, aY);
            x = mImages[TL].GetWidth() - mOverlapX + x;
            g.DrawImage(mImages[T], x, aY, mOverlapX + mTextWidth, mImages[T].GetHeight());
            x += mTextWidth;
            g.DrawImage(mImages[TR], x, aY);
            aY = mImages[TL].GetHeight() - mOverlapY + aY;
            x = anOffX;
            g.DrawImage(mImages[L], x, aY, mImages[L].GetWidth(), mTextHeight);
            x = mImages[L].GetWidth() - mOverlapX + x;
            g.DrawImage(mImages[C], x, aY, mOverlapX + mTextWidth, mTextHeight);
            x += mTextWidth;
            g.DrawImage(mImages[R], x, aY, mImages[R].GetWidth(), mTextHeight);
            aY = mTextHeight - mOverlapY + aY;
            x = anOffX;
            g.DrawImage(mImages[BL], x, aY);
            x = mImages[BL].GetWidth() - mOverlapX + x;
            g.DrawImage(mImages[B], x, aY, mOverlapX + mTextWidth, mImages[B].GetHeight());
            x += mTextWidth;
            g.DrawImage(mImages[BR], x, aY);

            // The arrow; side arrows are kept within the bubble's straight edge.
            int aShiftX = 0;
            int aShiftY = 0;
            int aTRH = mImages[TR].GetHeight();
            switch (mArrowPlacement)
            {
            case 5:
            case 6:
                g.DrawImage(mImages[TP], (int)(anOffX + mAttachPoint.x), (int)(anOffY + mAttachPoint.y));
                break;
            case 3:
            case 4:
            {
                int anArrowY = (int)(anOffY + mAttachPoint.y - mImages[RP].GetHeight());
                int aMax = mArrowPlacement == 3 ? anOffY + aTRH + mTextHeight - 25 : anOffY + aTRH + mTextHeight;
                int aClamped = SexyUtil.ClampRange(anArrowY, anOffY + aTRH - 5, aMax);
                g.DrawImage(mImages[RP], (int)(anOffX + mAttachPoint.x - mImages[RP].GetWidth()), aClamped);
                aShiftY = aClamped - anArrowY;
                break;
            }
            case 7:
            case 8:
                g.DrawImage(mImages[BP], (int)(anOffX + mAttachPoint.x), (int)(anOffY - mImages[BP].GetHeight() + mAttachPoint.y));
                break;
            case 1:
            case 2:
            {
                int anArrowY = (int)(anOffY + mAttachPoint.y - mImages[LP].GetHeight());
                int aMax = mArrowPlacement == 1 ? anOffY + aTRH + mTextHeight - 25 : anOffY + aTRH + mTextHeight;
                int aClamped = SexyUtil.ClampRange(anArrowY, anOffY + aTRH - 5, aMax);
                g.DrawImage(mImages[LP], (int)(anOffX + mAttachPoint.x), aClamped);
                aShiftY = aClamped - anArrowY;
                break;
            }
            }
            mAttachPoint.x += aShiftX;
            mAttachPoint.y += aShiftY;
            mX -= aShiftX;
            mY -= aShiftY;

            g.SetColor(mColor);
            g.SetFont(mFont);
            int aCenterX = anOffX + mImages[TL].GetWidth() + mTextWidth / 2;
            int aCenterY = anOffY + mImages[TL].GetHeight() + mTextHeight / 2;
            TRect aTextRect = new TRect(aCenterX - mTextWidth / 2 + mPadX, aCenterY - mTextHeight / 2 + mPadY,
                mTextWidth + mPadX * -2, mTextHeight + mPadY * -2);
            if (mText.Length != 0)
            {
                g.WriteWordWrapped(aTextRect, mText, mLineSpacing, mJustification);
            }
            g.EndOffscreen(inFrame);
        }

        public void SetText(string theText, Font theFont, Color theColor)
        {
            mText = theText;
            mFont = theFont;
            mColor = theColor;
            refreshTextBounds();
        }

        private void refreshTextBounds()
        {
            if (mText.Length != 0)
            {
                mTextWidth = mFont.StringWidth(mText);
                mTextWidth = mTextMaxWidth;
                Graphics g = Graphics.GetNew();
                g.SetFont(mFont);
                int aMaxWidth = 0;
                int aHeight = g.GetWordWrappedHeight(mTextWidth + mPadX * -2, mText, mLineSpacing, ref aMaxWidth, -1);
                mTextHeight = mPadY * 2 + aHeight;
            }
            refreshAttachPoint();
        }

        public bool Show(int theDelay)
        {
            int aState = mState;
            if (aState == STATE_HIDDEN)
            {
                setState(STATE_SHOWING, theDelay);
            }
            return aState == STATE_HIDDEN;
        }

        public bool Hide(int theDelay)
        {
            int aState = mState;
            if (aState == STATE_SHOWN)
            {
                setState(STATE_HIDING, theDelay);
            }
            return aState == STATE_SHOWN;
        }

        private void setState(int theState, int theDelay)
        {
            if (theDelay != 0)
            {
                mPendingState = theState;
                mPendingDelay = theDelay;
                return;
            }
            mPendingDelay = 0;
            if (theState == mState)
            {
                return;
            }
            mState = theState;
            if (theState == STATE_SHOWING)
            {
                if (mShowSound != -1)
                {
                    GlobalStaticVars.gSexyAppBase.PlaySample(mShowSound);
                }
                mShowScale.SetInVal(0.0);
                mShowOffsetY.SetInVal(0.0);
                mShowAlpha.SetInVal(0.0);
            }
            else if (theState == STATE_HIDING)
            {
                mHideScale.SetInVal(0.0);
                mHideOffsetY.SetInVal(0.0);
                mHideAlpha.SetInVal(0.0);
            }
        }

        public void Update()
        {
            if (mImage == null || mDirty)
            {
                RefreshImage();
            }
            if (mPendingDelay > 0)
            {
                mPendingDelay--;
                if (mPendingDelay == 0)
                {
                    setState(mPendingState, 0);
                }
            }
            switch (mState)
            {
            case STATE_SHOWING:
                mShowScale.IncInVal();
                mShowOffsetY.IncInVal();
                mShowAlpha.IncInVal();
                if (mShowScale.IsFinished())
                {
                    setState(STATE_SHOWN, 0);
                }
                break;
            case STATE_SHOWN:
                if (mExpireTime > 0)
                {
                    mExpireTime--;
                }
                if (mExpireTime == 0)
                {
                    Hide(0);
                }
                break;
            case STATE_HIDING:
                mHideScale.IncInVal();
                mHideOffsetY.IncInVal();
                mHideAlpha.IncInVal();
                if (mHideScale.IsFinished())
                {
                    setState(STATE_HIDDEN, 0);
                }
                break;
            }
        }
    }

    /// <summary>
    /// Golden sparks spiralling around the Nights progress screen's trophy column
    /// (Sexy::HelixEffect). Drawn in two passes so sparks pass behind and in front.
    /// </summary>
    public class HelixEffect
    {
        public class ParticleInfo
        {
            public int mAge;                                      // 0x00

            public int mLife;                                     // 0x04

            public int mPhase;                                    // 0x08

            public int mPeriod;                                   // 0x0c

            public bool mFlip;                                    // 0x10
        }

        public List<ParticleInfo> mParticles = new List<ParticleInfo>(); // 0x00

        public int mSpawnDelay;                                   // 0x0c

        public int mInt14;                                        // 0x14

        public int mUpdateCnt;                                    // 0x18

        public void Update(bool theSpawn)
        {
            mInt14 += SexyUtil.Rand() % 3 + 1;
            mSpawnDelay--;
            if (mSpawnDelay < 1)
            {
                if (theSpawn)
                {
                    ParticleInfo p = new ParticleInfo();
                    p.mAge = 0;
                    p.mPeriod = 100;
                    p.mLife = SexyUtil.Rand() % 50 + 100;
                    p.mPhase = SexyUtil.Rand() % p.mPeriod;
                    p.mFlip = SexyUtil.Rand() % 2 == 0;
                    mParticles.Add(p);
                }
                mSpawnDelay = SexyUtil.Rand() % 10 + 5;
            }
            int i = 0;
            while (i < mParticles.Count)
            {
                if (mParticles[i].mAge < mParticles[i].mLife)
                {
                    mParticles[i].mAge++;
                    i++;
                }
                else
                {
                    mParticles.RemoveAt(i);
                }
            }
            mUpdateCnt++;
        }

        public void Draw(Graphics g, bool theFront)
        {
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            g.SetColorizeImages(true);
            for (int i = 0; i < mParticles.Count; i++)
            {
                ParticleInfo p = mParticles[i];
                Image anImage = Res.IMAGE_TR_NIGHTS_PARTICLE_WHITE_MED_ROUND;
                float aW = anImage.GetWidth() * 0.22f;
                float aH = anImage.GetHeight() * 0.22f;
                float anAge = p.mAge;
                float anAlpha = Math.Min(1.0f, (p.mLife - anAge) / 60.0f);
                // A trail of 55 shrinking, fading dots back along the spiral.
                for (int j = 0; j < 55; j++)
                {
                    float x = 455.0f;
                    float y = anAge / p.mPeriod * -150.0f + 420.0f;
                    float aT = 1.0f - (anAge + p.mPhase) / (p.mPeriod * 0.66f);
                    int aQuarter = (int)((aT + 100.0f) * 2.0f) % 4;
                    bool visible = theFront ? (aQuarter == 3 || aQuarter == 0) : (aQuarter == 2 || aQuarter == 1);
                    if (!visible)
                    {
                        continue;
                    }
                    if (!p.mFlip)
                    {
                        x += (float)Math.Sin(aT * 3.1415927f) * 80.0f;
                        y += (float)Math.Cos(aT * 3.1415927f) * -10.0f;
                    }
                    else
                    {
#if PEGGLE_IPHONE
                        x += (float)Math.Cos(aT * 3.1415927f + 1.5707964f) * -80.0f;
#else
                        x += (float)Math.Cos(aT * 3.1415927f + 1.5707964f) * 0.0f;
#endif
                        y += (float)Math.Sin(aT * 3.1415927f + 1.5707964f) * 10.0f;
                    }
                    g.SetColor(SexyUtil.MakeColor(0xaaaa00, (int)(anAlpha * 200.0f)));
                    float aScreenH = SexyUtil.gPhysicalDisplayHeight;
#if PEGGLE_IPHONE
                    g.DrawImage(anImage, (int)(x * aScreenH / 600.0 - aW / 2.0), (int)(y * aScreenH / 600.0 - aH / 2.0), (int)aW, (int)aH);
#else
                    g.DrawImage(anImage, (int)(x * aScreenH / 600.0 - 80.0), (int)(y * aScreenH / 600.0 - aH / 2.0), (int)aW, (int)aH);
#endif
                    anAlpha -= 0.02f;
                    aW -= 0.15f;
                    aH -= 0.15f;
                    anAge -= 0.8f;
                    if (anAge < 0.0f || anAlpha <= 0.0f || aW <= 0.0f)
                    {
                        break;
                    }
                }
            }
            g.SetColorizeImages(false);
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
        }
    }

    /// <summary>
    /// The darkened screen with a spotlight hole that wanders, slows, then seeks a target
    /// (Sexy::SpotlightEffect). Used to reveal a trophy on the Nights progress screen.
    /// </summary>
    public class SpotlightEffect
    {
#if PEGGLE_IPHONE
        private static float PROGRESS_SCREEN_SPOTLIGHT_STEP_SIZE => SPOTLIGHT_STEP_SIZE;
#endif
        public const int STATE_OFF = 0;
        public const int STATE_SEARCHING = 1;
        public const int STATE_SLOWING = 2;
        public const int STATE_SEEKING = 3;
        public const int STATE_PAUSED = 4;
        public const int STATE_FINISHING = 5;

        public int mState;                                        // 0x00

        public int mAge;                                          // 0x04

        public int mStateTime;                                    // 0x08

        public int mSearchTime;                                   // 0x0c

        public float mScale;                                      // 0x10

        public float mTargetScale;                                // 0x14

        public float mStartScale;                                 // 0x18

        public float mSearchPos;                                  // 0x1c

        public SexyVector2 mPos;                                  // 0x20

        public SexyVector2 mStartPos;                             // 0x28

        public SexyVector2 mTarget;                               // 0x30

        public CurvedVal mFade = new CurvedVal();                 // 0x38

        public CurvedVal mSearchX = new CurvedVal();              // 0xac

        public CurvedVal mSearchY = new CurvedVal();              // 0x120

        public CurvedVal mSeek = new CurvedVal();                 // 0x194

        public SpotlightEffect()
        {
            Reset();
        }

        public void Reset()
        {
            mFade.SetCurve("b+0,1,0.01,0.5,####         ~g###");
            mSearchX.SetCurve("b+-1,1,0.01,1.5,P5aD  P~###     #####  PPc?9");
            mSearchY.SetCurve("b+-1,1,0.01,1.5,P71r :~###  O####  Q~###  O#### :Pe>k");
            mSeek.SetCurve("b+0,1,0.01,1,####         ~~###");
            mSearchPos = 0;
            mAge = 0;
            setState(STATE_OFF);
            mPos = new SexyVector2(SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2);
            mScale = 0;
            mTargetScale = 0;
            mStartScale = 0;
        }

        private void setState(int theState)
        {
            mStateTime = 0;
            mSearchTime = 0;
            mState = theState;
            if (theState == STATE_SEEKING)
            {
                mStartPos = mPos;
            }
            else if (theState == STATE_SEARCHING)
            {
                mSearchPos += SexyUtil.Rand() % 150;
                mSearchTime = SexyUtil.Rand() % 25 + 170;
            }
        }

        private void UpdateSearchPos()
        {
            float aPos = mSearchPos % 150.0f;
            mPos.x = (float)(SCREEN_WIDTH / 2 + mSearchX.GetOutVal(aPos / 100.0f) * PROGRESS_SPOTLIGHT_SEARCH_W);
            mPos.y = (float)(SCREEN_HEIGHT / 2 + mSearchY.GetOutVal(aPos / 100.0f) * PROGRESS_SPOTLIGHT_SEARCH_H + PROGRESS_SPOTLIGHT_OFFSET_Y);
        }

        public void Update()
        {
            if (mState == STATE_SEARCHING)
            {
                mSearchPos += 1.0f;
                UpdateSearchPos();
                float aDist = new SexyVector2(mPos.x - mTarget.x, mPos.y - mTarget.y).Magnitude();
                if (mSearchTime <= mAge && 200.0f < aDist)
                {
                    setState(STATE_SLOWING);
                }
            }
            else if (mState == STATE_SLOWING)
            {
                if (mStateTime < 25)
                {
                    mSearchPos += 1.0f - mStateTime / 25.0f;
                }
                UpdateSearchPos();
                if (mStateTime > 30)
                {
                    setState(STATE_SEEKING);
                }
            }
            else if (mState == STATE_SEEKING)
            {
                SexyVector2 aDelta = new SexyVector2(mTarget.x - mStartPos.x, mTarget.y - mStartPos.y);
                float aDist = aDelta.Magnitude();
                double aTime = (mStateTime / 100.0f) / (aDist / PROGRESS_SCREEN_SPOTLIGHT_STEP_SIZE);
                if (mTargetScale - mStartScale != 0.0f)
                {
                    mScale = (float)(mStartScale + (mTargetScale - mStartScale) * mSeek.GetOutVal(aTime));
                }
#if PEGGLE_IPHONE
                // The move itself uses an unscaled 500 (the scale above uses the scaled SPOTLIGHT_STEP_SIZE).
                float aFrac = (float)mSeek.GetOutVal((mStateTime / 100.0f) / (aDist / 500.0));
#else
                float aFrac = (float)mSeek.GetOutVal(aTime);
#endif
                mPos.x = mStartPos.x + aDelta.x * aFrac;
                mPos.y = mStartPos.y + aDelta.y * aFrac;
                if (1.0f <= GetSpotlightSeekPct())
                {
                    mStartScale = mScale;
                    setState(STATE_PAUSED);
                }
            }
            mStateTime++;
            mAge++;
        }

        public float GetSpotlightSeekPct()
        {
            if (mState != STATE_SEEKING)
            {
                return 0f;
            }
            return (float)((mStateTime / 100.0f) / mSeek.mInMax);
        }

        public void Seek(SexyVector2 theTarget, float theScale)
        {
            mTarget = theTarget;
            mTargetScale = theScale;
            setState(STATE_SEEKING);
        }

        public void Start(SexyVector2 theTarget, float theScale)
        {
            Reset();
            mTarget = theTarget;
            mTargetScale = theScale;
            setState(STATE_SEARCHING);
        }

        public bool IsPaused()
        {
            return mState == STATE_PAUSED;
        }

        public void Finish()
        {
            setState(STATE_FINISHING);
        }

        public void Draw(Graphics g, bool theWide)
        {
            Image anImage = Res.IMAGE_TR_NIGHTS_SPOTLIGHT;
            int w = anImage.GetWidth();
            int h = anImage.GetHeight();
#if PEGGLE_IPHONE
            // Wider on trophies, and grown by the current scale.
            if (theWide)
            {
                w = (int)(w * 1.5);
                h = (int)(h * 1.5);
            }
            if (mScale > 0.0f)
            {
                float aScale = mScale + 1.0f;
                w = (int)(w * aScale);
                h = (int)(h * aScale);
            }
#endif
            if (mState == STATE_FINISHING)
            {
                double aTime = mStateTime / 100.0f;
                if (mFade.mInMax <= aTime)
                {
                    return;
                }
                g.SetColor(SexyUtil.MakeColor(0, (int)(mFade.GetOutVal(mFade.mInMax - aTime) * 255.0)));
            }
            else
            {
                if (mState == STATE_OFF)
                {
                    return;
                }
                g.SetColor(SexyUtil.MakeColor(0, (int)(mFade.GetOutVal(mAge / 100.0f) * 255.0)));
            }
            g.SetColorizeImages(true);
            int aLeft = (int)(mPos.x - w / 2);
            int aRight = aLeft + w;
            int aTop = (int)(mPos.y - h / 2);
            int aBottom = aTop + h;
#if PEGGLE_IPHONE
            g.FillRect(-SexyUtil.gGutterWidth, 0, aLeft + SexyUtil.gGutterWidth, SCREEN_HEIGHT);
#else
            g.FillRect(0, 0, aLeft, SCREEN_HEIGHT);
#endif
            g.FillRect(aRight, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
            g.FillRect(aLeft, 0, w, aTop);
            g.FillRect(aLeft, aBottom, w, SCREEN_HEIGHT);
            g.DrawImage(anImage, aLeft, aTop, w, h);
            g.SetColorizeImages(false);
        }
    }
}
