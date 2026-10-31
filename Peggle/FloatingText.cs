using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// A floating caption on the board (Sexy::FloatingText): multi-line, localized text that can
    /// fade, blink, scale and rotate along keyframes, follow an object, and point an arrow.
    /// Scaled or rotated text is pre-rendered into an image.
    /// </summary>
    public class FloatingText : ISyncable
    {
        public class ScaleInfo : ISyncable
        {
            public float mScale;                                  // 0x00

            public int mTime;                                     // 0x04

            public ScaleInfo()
            {
            }

            public ScaleInfo(float theScale, int theTime)
            {
                mScale = theScale;
                mTime = theTime;
            }

            public void SyncState(DataSync theSync)
            {
                theSync.SyncFloat(ref mScale);
                theSync.SyncLong(ref mTime);
            }
        }

        public class PosInfo : ISyncable
        {
            public SexyVector2 mPos;                              // 0x00

            public int mTime;                                     // 0x08

            public PosInfo()
            {
            }

            public PosInfo(SexyVector2 thePos, int theTime)
            {
                mPos = thePos;
                mTime = theTime;
            }

            public void SyncState(DataSync theSync)
            {
                theSync.SyncFloat(ref mPos.x);
                theSync.SyncFloat(ref mPos.y);
                theSync.SyncLong(ref mTime);
            }
        }

        public MemoryImage mTextImage;                            // 0x04

        public List<string> mLines = new List<string>();          // 0x08

        public List<string> mSexyLines = new List<string>();      // 0x14

        public List<int> mLineWidths = new List<int>();           // 0x20

        public List<int> mSexyLineWidths = new List<int>();       // 0x2c

        public List<ScaleInfo> mScales = new List<ScaleInfo>();   // 0x38

        public List<PosInfo> mPositions = new List<PosInfo>();    // 0x44

        public int mHeight;                                       // 0x50

        public int mSexyHeight;                                   // 0x54

        public int mWidth;                                        // 0x58

        public int mSexyWidth;                                    // 0x5c

        /// <summary>-1 left, 0 centre, 1 right.</summary>
        public int mJustifyX;                                     // 0x60

        /// <summary>-1 top, 0 centre, 1 bottom.</summary>
        public int mJustifyY;                                     // 0x64

        public int mId;                                           // 0x68

        public int mFontId;                                       // 0x6c

        public int mColor;                                        // 0x70

        public int mShadowColor;                                  // 0x74

        public int mDuration;                                     // 0x78

        public int mUpdateCnt;                                    // 0x7c

        public int mBlinkCnt;                                     // 0x80

        public int mFadeIn;                                       // 0x84

        public int mFadeOut;                                      // 0x88

        public int mLayer;                                        // 0x8c

        public float mX;                                          // 0x90

        public float mY;                                          // 0x94

        public float mVelX;                                       // 0x98

        public float mVelY;                                       // 0x9c

        public float mScale;                                      // 0xa0

        public float mRotation;                                   // 0xa4

        public float mRotationVel;                                // 0xa8

        public bool mBackShade;                                   // 0xac

        /// <summary>The text contains ^RRGGBB^ colour codes.</summary>
        public bool mColorCodes;                                  // 0xad

        /// <summary>Keep spinning until this update count.</summary>
        public int mSpinUntil;                                    // 0xb0

        /// <summary>0 none, 1 up, 2 down, 3 custom.</summary>
        public int mArrowType;                                    // 0xb4

        public int mArrowX;                                       // 0xb8

        public int mArrowY;                                       // 0xbc

        public int mArrowDir;                                     // 0xc0

        public PhysObj mRefObj;                                   // 0xc4

        public float mRefX;                                       // 0xc8

        public float mRefY;                                       // 0xcc

        public bool mRenderingImage;                              // 0xd0

        /// <summary>The ascent offset is in screen pixels rather than board units.</summary>
        public bool mScreenAscent;                                // 0xd1

        public FloatingText()
        {
            mTextImage = null;
            Reset();
        }

        public FloatingText(FloatingText theText)
        {
            mTextImage = theText.mTextImage;
            mLines = new List<string>(theText.mLines);
            mSexyLines = new List<string>(theText.mSexyLines);
            mLineWidths = new List<int>(theText.mLineWidths);
            mSexyLineWidths = new List<int>(theText.mSexyLineWidths);
            mScales = new List<ScaleInfo>(theText.mScales);
            mPositions = new List<PosInfo>(theText.mPositions);
            mHeight = theText.mHeight;
            mSexyHeight = theText.mSexyHeight;
            mWidth = theText.mWidth;
            mSexyWidth = theText.mSexyWidth;
            mJustifyX = theText.mJustifyX;
            mJustifyY = theText.mJustifyY;
            mId = theText.mId;
            mFontId = theText.mFontId;
            mColor = theText.mColor;
            mShadowColor = theText.mShadowColor;
            mDuration = theText.mDuration;
            mUpdateCnt = theText.mUpdateCnt;
            mBlinkCnt = theText.mBlinkCnt;
            mFadeIn = theText.mFadeIn;
            mFadeOut = theText.mFadeOut;
            mLayer = theText.mLayer;
            mX = theText.mX;
            mY = theText.mY;
            mVelX = theText.mVelX;
            mVelY = theText.mVelY;
            mScale = theText.mScale;
            mRotation = theText.mRotation;
            mRotationVel = theText.mRotationVel;
            mBackShade = theText.mBackShade;
            mColorCodes = theText.mColorCodes;
            mSpinUntil = theText.mSpinUntil;
            mArrowType = theText.mArrowType;
            mArrowX = theText.mArrowX;
            mArrowY = theText.mArrowY;
            mArrowDir = theText.mArrowDir;
            mRefObj = theText.mRefObj;
            mRefX = theText.mRefX;
            mRefY = theText.mRefY;
            mRenderingImage = theText.mRenderingImage;
            mScreenAscent = theText.mScreenAscent;
        }

        public void Reset()
        {
            mTextImage?.Dispose();
            mTextImage = null;
            mSexyLines.Clear();
            mLines.Clear();
            mLineWidths.Clear();
            mSexyLineWidths.Clear();
            mScales.Clear();
            mPositions.Clear();
            mRenderingImage = false;
            mId = 0;
            mHeight = 0;
            mSexyHeight = 0;
            mWidth = 0;
            mSexyWidth = 0;
            mJustifyX = 0;
            mJustifyY = 0;
            mFontId = 0;
            mColor = 0;
            mShadowColor = 0;
            mDuration = 0;
            mUpdateCnt = 0;
            mX = 0;
            mY = 0;
            mVelX = 0;
            mVelY = 0;
            mBlinkCnt = 0;
            mFadeIn = 0;
            mFadeOut = 0;
            mArrowType = 0;
            mScale = 1.0f;
            mLayer = 0;
            mRotation = 0;
            mRotationVel = 0;
            mSpinUntil = 0;
            mBackShade = false;
            mColorCodes = false;
            mRefObj = null;
            mRefX = 0;
            mRefY = 0;
            mArrowX = 0;
            mArrowY = 0;
            mArrowDir = 0;
            mScreenAscent = false;
        }

        public void SyncState(DataSync theSync)
        {
            if (theSync.GetReader() != null)
            {
                Reset();
            }
            theSync.SyncStringList(mLines);
            theSync.SyncIntList(mLineWidths);
            theSync.SyncLong(ref mHeight);
            theSync.SyncLong(ref mWidth);
            theSync.SyncSByte(ref mJustifyX);
            theSync.SyncSByte(ref mJustifyY);
            theSync.SyncLong(ref mId);
            theSync.SyncLong(ref mFontId);
            theSync.SyncLong(ref mColor);
            theSync.SyncLong(ref mShadowColor);
            theSync.SyncLong(ref mDuration);
            theSync.SyncLong(ref mUpdateCnt);
            theSync.SyncLong(ref mBlinkCnt);
            theSync.SyncFloat(ref mX);
            theSync.SyncFloat(ref mY);
            theSync.SyncFloat(ref mVelX);
            theSync.SyncFloat(ref mVelY);
            if (theSync.GetVersion() > 0xf && theSync.GetVersion() < 0x1a)
            {
                int anUnused = 0;
                theSync.SyncLong(ref anUnused);
            }
            if (theSync.GetVersion() > 0x13)
            {
                theSync.SyncList(mScales, (ds, s) => { s.SyncState(ds); return s; }, () => new ScaleInfo());
                theSync.SyncList(mPositions, (ds, p) => { p.SyncState(ds); return p; }, () => new PosInfo());
                theSync.SyncFloat(ref mScale);
            }
            bool hasFadeIn = mFadeIn != 0;
            bool hasFadeOut = mFadeOut != 0;
            bool hasRotationVel = mRotationVel != 0.0f;
            bool hasArrow = mArrowType != 0;
            bool hasRefObj = mRefObj != null;
            bool hasCustomArrow = mArrowType == 3;
            if (theSync.GetVersion() > 0x19)
            {
                theSync.SyncBoolBit(ref hasFadeIn);
                theSync.SyncBoolBit(ref hasFadeOut);
                theSync.SyncBoolBit(ref hasRotationVel);
                theSync.SyncBoolBit(ref mBackShade);
                theSync.SyncBoolBit(ref mColorCodes);
                theSync.SyncBoolBit(ref hasArrow);
                theSync.SyncBoolBit(ref hasRefObj);
                theSync.SyncBoolBit(ref hasCustomArrow);
                theSync.EndBit();
                if (hasFadeIn)
                {
                    theSync.SyncLong(ref mFadeIn);
                }
                if (hasFadeOut)
                {
                    theSync.SyncLong(ref mFadeOut);
                }
                if (hasArrow)
                {
                    theSync.SyncByte(ref mArrowType);
                }
                if (hasRefObj)
                {
                    mRefObj = theSync.SyncRefCount(mRefObj, () => (PhysObj)new Ball(false));
                    theSync.SyncFloat(ref mRefX);
                    theSync.SyncFloat(ref mRefY);
                }
                if (hasCustomArrow)
                {
                    theSync.SyncLong(ref mArrowX);
                    theSync.SyncLong(ref mArrowY);
                    theSync.SyncLong(ref mArrowDir);
                }
            }
            if (theSync.GetVersion() > 0x1f)
            {
                theSync.SyncLong(ref mLayer);
            }
            if (theSync.GetVersion() > 0x22 && hasRotationVel)
            {
                theSync.SyncFloat(ref mRotation);
                theSync.SyncFloat(ref mRotationVel);
                theSync.SyncLong(ref mSpinUntil);
            }
            if (theSync.GetVersion() > 0x55)
            {
                theSync.SyncBoolBit(ref mScreenAscent);
            }
            if (theSync.GetReader() != null)
            {
                if (mFontId < 0 || mFontId >= 0x1a)
                {
                    mFontId = 0x12;
                }
                string aText = "";
                for (int i = 0; i < mLines.Count; i++)
                {
                    if (i != 0)
                    {
                        aText += "\n";
                    }
                    aText += mLines[i];
                }
                SetText(aText);
            }
        }

        private static Font GetFont(int theFontId)
        {
            return Res.GetFontById((ResourceId)theFontId);
        }

        private static int MeasureColored(Graphics g, string theLine)
        {
            return g.WriteString(theLine, 0, 0, -1, -1, false, 0, -1, -1);
        }

        /// <summary>Sets the text (lines split on '\n', each localized), measuring its lines.</summary>
        public void SetText(string theText)
        {
            mTextImage?.Dispose();
            mTextImage = null;
            mSexyLines.Clear();
            mSexyLineWidths.Clear();
            mLines.Clear();
            mLineWidths.Clear();
            Font aFont = GetFont(mFontId);
            int aMaxWidth = 0;
            int aMaxSexyWidth = 0;
            Graphics g = Graphics.GetNew();
            g.SetFont(aFont);
            int aStart = 0;
            bool done = false;
            bool handledPattern = false;
            if (theText.IndexOf('^') != -1)
            {
                mColorCodes = true;
            }
            while (!done)
            {
                int aPos = theText.IndexOf('\n', aStart);
                if (aPos == -1)
                {
                    done = true;
                    mLines.Add(aStart == 0 ? theText : theText.Substring(aStart));
                }
                else
                {
                    mLines.Add(theText.Substring(aStart, aPos - aStart));
                }
                mSexyLines.Add(LocalizeString(mLines[mLines.Count - 1], ref handledPattern));
                int aWidth;
                int aSexyWidth;
                if (!mColorCodes)
                {
                    aWidth = aFont.StringWidth(mLines[mLines.Count - 1]);
                    aSexyWidth = aFont.StringWidth(mSexyLines[mSexyLines.Count - 1]);
                }
                else
                {
                    aWidth = MeasureColored(g, mLines[mLines.Count - 1]);
                    aSexyWidth = MeasureColored(g, mSexyLines[mSexyLines.Count - 1]);
                }
                mLineWidths.Add(aWidth);
                mSexyLineWidths.Add(aSexyWidth);
                if (aMaxWidth < mLineWidths[mLineWidths.Count - 1])
                {
                    aMaxWidth = mLineWidths[mLineWidths.Count - 1];
                }
                if (aMaxSexyWidth < mSexyLineWidths[mSexyLineWidths.Count - 1])
                {
                    aMaxSexyWidth = mSexyLineWidths[mSexyLineWidths.Count - 1];
                }
                aStart = aPos + 1;
            }
            mWidth = aMaxWidth;
            mSexyWidth = aMaxSexyWidth;
            mHeight = mLines.Count * aFont.GetAscent();
            mSexyHeight = mSexyLines.Count * aFont.GetAscent();
            g.PrepareForReuse();
            if (!handledPattern)
            {
                string aSexyText = theText;
                ThunderballApp anApp = GlobalStaticVars.gSexyAppBase as ThunderballApp;
                if (anApp.mFloatingTextPatMgr.HandlePattern(ref aSexyText))
                {
                    SetSexyText(aSexyText);
                }
                else if (GlobalStaticVars.gSexyAppBase.mPopLoc.mStrings.TryGetValue(theText, out string aLocalized))
                {
                    // The original sets the localized text and then immediately the raw text.
                    SetSexyText(aLocalized);
                    SetSexyText(theText);
                }
            }
        }

        /// <summary>Looks the line up in the string table, else runs it through the pattern manager.</summary>
        public static string LocalizeString(string theString, ref bool handledPattern)
        {
            if (GlobalStaticVars.gSexyAppBase.mPopLoc.mStrings.TryGetValue(theString, out string aLocalized))
            {
                return aLocalized;
            }
            string aSexyString = theString;
            ThunderballApp anApp = GlobalStaticVars.gSexyAppBase as ThunderballApp;
            bool handled = anApp.mFloatingTextPatMgr.HandlePattern(ref aSexyString);
            handledPattern = handledPattern | handled;
            return aSexyString;
        }

        public void SetSexyText(string theText)
        {
            mSexyLines.Clear();
            mSexyLineWidths.Clear();
            Font aFont = GetFont(mFontId);
            int aMaxWidth = 0;
            Graphics g = Graphics.GetNew();
            g.SetFont(aFont);
            int aStart = 0;
            bool done = false;
            if (theText.IndexOf('^') != -1)
            {
                mColorCodes = true;
            }
            while (!done)
            {
                int aPos = theText.IndexOf('\n', aStart);
                if (aPos == -1)
                {
                    done = true;
                    mSexyLines.Add(aStart == 0 ? theText : theText.Substring(aStart));
                }
                else
                {
                    mSexyLines.Add(theText.Substring(aStart, aPos - aStart));
                }
                int aWidth = !mColorCodes ? aFont.StringWidth(mSexyLines[mSexyLines.Count - 1]) : MeasureColored(g, mSexyLines[mSexyLines.Count - 1]);
                mSexyLineWidths.Add(aWidth);
                if (aMaxWidth < aWidth)
                {
                    aMaxWidth = aWidth;
                }
                aStart = aPos + 1;
            }
            mSexyWidth = aMaxWidth;
            mSexyHeight = mSexyLines.Count * aFont.GetAscent();
            g.PrepareForReuse();
        }

        /// <summary>Adds a scale keyframe; a keyframe at time 0 also sets the current scale.</summary>
        public void AddScale(float theScale, int theTime)
        {
            mScales.Add(new ScaleInfo(theScale, theTime));
            if (theTime == 0)
            {
                mScale = theScale;
            }
        }

        /// <summary>Adds a move keyframe theTime updates after the previous one (or now).</summary>
        public void MoveTo(float theX, float theY, int theTime)
        {
            if (mPositions.Count == 0)
            {
                mPositions.Add(new PosInfo(new SexyVector2(mX, mY), mUpdateCnt < 1 ? 0 : mUpdateCnt));
            }
            int aLast = mPositions[mPositions.Count - 1].mTime;
            if (aLast < mUpdateCnt)
            {
                aLast = mUpdateCnt;
            }
            mPositions.Add(new PosInfo(new SexyVector2(theX, theY), aLast + theTime));
        }

        public void HoldPos(int theTime)
        {
            if (mPositions.Count == 0)
            {
                mPositions.Add(new PosInfo(new SexyVector2(mX, mY), mUpdateCnt < 1 ? 0 : mUpdateCnt));
            }
            PosInfo aLast = mPositions[mPositions.Count - 1];
            MoveTo(aLast.mPos.x, aLast.mPos.y, theTime);
        }

        public void SetRefObj(PhysObj theObj)
        {
            mRefObj = theObj;
            if (theObj != null)
            {
                mRefX = theObj.GetXPos();
                mRefY = theObj.GetYPos();
            }
        }

        /// <summary>Renders the text unscaled into an image so it can be scaled/rotated as a sprite.</summary>
        public void CreateTextImage()
        {
            mRenderingImage = true;
            mTextImage?.Dispose();
            mTextImage = null;
            Font aFont = GetFont(mFontId);
            int aPadWidth = aFont.StringWidth("W") * 2;
            int aPadHeight = aFont.GetAscent() + 10;
            MemoryImage anImage = new MemoryImage();
            int aHeight = mSexyHeight + aPadHeight;
            if ((aHeight & 1) != 0)
            {
                aHeight++;
            }
            anImage.Create(mSexyWidth + aPadWidth, aHeight);
            Graphics g = Graphics.GetNew(anImage);
            int aBlink = mBlinkCnt;
            float aX = mX;
            float aY = mY;
            int aJustifyY = mJustifyY;
            float aScale = mScale;
            float aRotation = mRotation;
            int aFadeIn = mFadeIn;
            mBlinkCnt = 0;
            mY = 0;
            mX = 0;
            mJustifyY = 0;
            mScale = 1.0f;
            mRotation = 0;
            mFadeIn = 0;
            if (mJustifyX == 0)
            {
                mX = anImage.mWidth / 2;
            }
            else if (mJustifyX == 1)
            {
                mX = anImage.mWidth - aPadWidth / 2;
            }
            else
            {
                mX = aPadWidth / 2;
            }
            mY = anImage.mHeight - (mSexyHeight + 5);
            Draw(g);
            g.SetRenderTarget(null);
            g.PrepareForReuse();
            mBlinkCnt = aBlink;
            mX = aX;
            mY = aY;
            mJustifyY = aJustifyY;
            mScale = aScale;
            mRotation = aRotation;
            mFadeIn = aFadeIn;
            mTextImage = anImage;
            mRenderingImage = false;
        }

        public void Draw(Graphics g)
        {
            if (mUpdateCnt < 0)
            {
                return;
            }
            if (mBlinkCnt != 0 && mBlinkCnt / 0x1e % 2 == 0)
            {
                return;
            }
            int anAlpha = 0xff;
            if (mFadeIn > 0 && mUpdateCnt < mFadeIn)
            {
                anAlpha = mUpdateCnt * 0xff / mFadeIn;
            }
            else if (mFadeOut > 0 && mDuration - mFadeOut < mUpdateCnt)
            {
                anAlpha = 0xff - (mUpdateCnt - (mDuration - mFadeOut)) * 0xff / mFadeOut;
                if (anAlpha < 1)
                {
                    return;
                }
                if (anAlpha > 0xff)
                {
                    anAlpha = 0xff;
                }
            }
            int aH = SexyUtil.gPhysicalDisplayHeight;
            if (mScale != 1.0f || mRotation != 0.0f || mTextImage != null)
            {
                if (mTextImage == null)
                {
                    CreateTextImage();
                }
                Transform aTrans = new Transform();
                if (mScale != 1.0f)
                {
                    aTrans.Scale(mScale, mScale);
                }
                if (mRotation != 0.0f && (g.Is3D() || mScale == 1.0f))
                {
                    aTrans.RotateRad(mRotation);
                }
                float aX = mX * aH / 600.0f;
                float aY = mY * aH / 600.0f;
                if (mJustifyX == -1)
                {
                    aX = (float)((double)(mX * aH) / 600.0 + mTextImage.mWidth / 2);
                }
                else if (mJustifyX == 1)
                {
                    aX = (float)((double)(mX * aH) / 600.0 - mTextImage.mWidth / 2);
                }
                if (mJustifyY == -1)
                {
                    aY = (float)((double)(mY * aH) / 600.0 + mTextImage.mHeight / 2);
                }
                else if (mJustifyY == 1)
                {
                    aY = (float)((double)(mY * aH) / 600.0 - mTextImage.mHeight / 2);
                }
                if (anAlpha != 0xff)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                }
                g.DrawImageTransform(mTextImage, aTrans, aX, aY);
                g.SetColorizeImages(false);
                return;
            }

            Font aFont = GetFont(mFontId);
            g.SetFont(aFont);
            float aDrawX = mX;
            float aDrawY = mY;
            if (!mScreenAscent)
            {
                aDrawY += aFont.GetAscent();
            }
            else
            {
                aDrawY = (float)(aDrawY + aFont.GetAscent() * 600.0 / aH);
            }
            if (mJustifyY == 0)
            {
                aDrawY -= mSexyHeight / 2;
            }
            if (mBackShade)
            {
                int aShadeX;
                if (mJustifyX == 0)
                {
                    aShadeX = (int)((double)(aDrawX * aH) / 600.0 - mSexyWidth / 2);
                }
                else if (mJustifyX == 1)
                {
                    aShadeX = (int)((double)(aDrawX * aH) / 600.0 - mSexyWidth);
                }
                else
                {
                    aShadeX = (int)(aDrawX * aH / 600.0f);
                }
                g.SetColor(SexyUtil.MakeColor(0, anAlpha * 0x7d / 0xff));
                g.SetColorizeImages(true);
                int aPad = Layout.FLOAT_TEXT_MGR_BACK_SHADE_PAD;
                TRect aRect = new TRect(aShadeX - aPad, (int)((double)(aDrawY * aH) / 600.0 - aFont.GetAscent()),
                    mSexyWidth + aPad * 2, mSexyHeight + aPad);
                g.DrawImageBox(aRect.Inflate(2, 2), Res.IMAGE_DLG_TEXTRECT);
                g.SetColorizeImages(false);
            }
            int aColor = mColor;
            if (!mRenderingImage)
            {
                aDrawY = aDrawY * aH / 600.0f;
            }
            for (int i = 0; i < mSexyLines.Count; i++)
            {
                float aLineX;
                if (mJustifyX == 0)
                {
                    double aHalf = !mRenderingImage ? mSexyLineWidths[i] / 2 * 600.0 / aH : mSexyLineWidths[i] / 2;
                    aLineX = (float)(aDrawX - aHalf);
                }
                else if (mJustifyX == 1)
                {
                    double aFull = !mRenderingImage ? mSexyLineWidths[i] * 600.0 / aH : mSexyLineWidths[i];
                    aLineX = (float)(aDrawX - aFull);
                }
                else
                {
                    aLineX = aDrawX;
                }
                if (mShadowColor != 0)
                {
                    g.SetColor(SexyUtil.MakeColor(mShadowColor, anAlpha));
                    if (!mColorCodes)
                    {
                        g.DrawString(mSexyLines[i], (int)(aLineX + 2.0f), (int)(aDrawY + 2.0f));
                    }
                    else
                    {
                        g.DrawStringColor(mSexyLines[i], (int)(aLineX + 2.0f), (int)(aDrawY + 2.0f), -1);
                    }
                }
                g.SetColor(SexyUtil.MakeColor(aColor, anAlpha));
                if (!mColorCodes)
                {
                    if (!mRenderingImage)
                    {
                        g.DrawString(mSexyLines[i], (int)(aLineX * aH / 600.0f), (int)aDrawY);
                    }
                    else
                    {
                        g.DrawString(mSexyLines[i], (int)aLineX, (int)aDrawY);
                    }
                }
                else
                {
                    float aX = aLineX;
                    if (!mRenderingImage)
                    {
                        aX = aLineX * aH / 600.0f;
                    }
                    g.DrawStringColor(mSexyLines[i], (int)aX, (int)aDrawY, mColor);
                    Color aCurrent = g.GetColor();
                    aColor = (aCurrent.R << 16) | (aCurrent.G << 8) | aCurrent.B;
                    if (aCurrent.PackedValue == 0xffffffff)
                    {
                        aColor = 0xffffff;
                    }
                }
                aDrawY += aFont.GetAscent();
            }
            if (mArrowType != 0)
            {
                if (anAlpha != 0xff)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                }
                int anArrowY = (int)((double)(aDrawY * aH) / 600.0 - aFont.GetAscent());
                int anArrowX;
                if (mJustifyX == 0)
                {
                    anArrowX = (int)((double)(aDrawX * aH) / 600.0 - mSexyWidth / 2);
                }
                else if (mJustifyX == 1)
                {
                    anArrowX = (int)((double)(aDrawX * aH) / 600.0 - mSexyWidth);
                }
                else
                {
                    anArrowX = (int)(aDrawX * aH / 600.0f);
                }
                Image anArrow = Res.IMAGE_TIPARROW;
                if (mArrowType == 2)
                {
#if PEGGLE_IPHONE
                    g.DrawImageCel(anArrow, (anArrowX - anArrow.mWidth / 2 / 2) + -6, anArrowY + mSexyHeight + -7, 1);
                }
                else if (mArrowType == 1)
                {
                    g.DrawImageCel(anArrow, (anArrowX - anArrow.mWidth / 2 / 2) + -6, anArrowY + -7, 0);
#else
                    g.DrawImageCel(anArrow, (anArrowX - anArrow.mWidth / 2 / 2) + -6, anArrowY + mSexyHeight + -7, Layout.FLOATING_TEXT_MGR_ARROW_DOWN);
                }
                else if (mArrowType == 1)
                {
                    g.DrawImageCel(anArrow, Layout.FLOATINGTEXTMGR_IPAD_OFFSET + (anArrowX - anArrow.mWidth / 2 / 2) + -6, anArrowY + -7, Layout.FLOATING_TEXT_MGR_ARROW_UP);
#endif
                }
                g.SetColorizeImages(false);
            }
        }

        /// <summary>Interpolates the scale between the keyframes around the current time.</summary>
        public void UpdateScale()
        {
            int aPrevTime = 0;
            float aPrevScale = 1.0f;
            int i;
            for (i = 0; i < mScales.Count; i++)
            {
                ScaleInfo anInfo = mScales[i];
                if (mUpdateCnt < anInfo.mTime)
                {
                    if (anInfo.mTime - aPrevTime > 0)
                    {
                        float t = (mUpdateCnt - aPrevTime) / (float)(anInfo.mTime - aPrevTime);
                        mScale = t * anInfo.mScale + (1.0f - t) * aPrevScale;
                    }
                    break;
                }
                aPrevScale = anInfo.mScale;
                aPrevTime = anInfo.mTime;
            }
            if (i == mScales.Count)
            {
                mScale = aPrevScale;
            }
        }

        /// <summary>Interpolates the position between the keyframes around the current time; drifts by mVel after the last.</summary>
        public void UpdatePos()
        {
            int aPrevTime = 0;
            PosInfo aPrev = mPositions[0];
            int i;
            for (i = 1; i < mPositions.Count; i++)
            {
                PosInfo anInfo = mPositions[i];
                if (mUpdateCnt <= anInfo.mTime)
                {
                    int aSpan = anInfo.mTime - aPrevTime;
                    if (aSpan > 0)
                    {
                        if (anInfo.mPos != aPrev.mPos)
                        {
                            float t = (mUpdateCnt - aPrevTime) / (float)aSpan;
                            SexyVector2 aPos = anInfo.mPos * t + aPrev.mPos * (1.0f - t);
                            mX = aPos.x;
                            mY = aPos.y;
                        }
                        else
                        {
                            mX = anInfo.mPos.x;
                            mY = anInfo.mPos.y;
                        }
                    }
                    break;
                }
                aPrevTime = anInfo.mTime;
                aPrev = anInfo;
            }
            if (i == mPositions.Count)
            {
                mX += mVelX;
                mY += mVelY;
            }
        }

        /// <summary>Returns false once the text's duration has passed.</summary>
        public bool Update()
        {
            mUpdateCnt++;
            if (mUpdateCnt < 0)
            {
                return true;
            }
            if (mDuration < mUpdateCnt && mDuration != 0)
            {
                return false;
            }
            if (mRefObj != null)
            {
                float aX = mRefObj.GetXPos();
                float aY = mRefObj.GetYPos();
                mX += aX - mRefX;
                mY += aY - mRefY;
                mRefX = aX;
                mRefY = aY;
            }
            if (mBlinkCnt != 0)
            {
                mBlinkCnt--;
            }
            if (mScales.Count != 0)
            {
                UpdateScale();
            }
            if (mUpdateCnt < mSpinUntil || mRotation != 0.0f)
            {
                UpdateRotation();
            }
            if (mPositions.Count < 2)
            {
                mX += mVelX;
                mY += mVelY;
            }
            else
            {
                UpdatePos();
            }
            return true;
        }

        /// <summary>Spins by mRotationVel, wrapping while spinning and settling to 0 once the spin time has passed.</summary>
        public void UpdateRotation()
        {
            mRotation += mRotationVel;
            if (mRotationVel <= 0.0f)
            {
                if (mRotation < 0.0f)
                {
                    if (mUpdateCnt < mSpinUntil)
                    {
                        mRotation += 6.2831855f;
                    }
                    else
                    {
                        mRotation = 0;
                    }
                }
            }
            else if (mRotation > 6.2831855f)
            {
                if (mUpdateCnt < mSpinUntil)
                {
                    mRotation -= 6.2831855f;
                }
                else
                {
                    mRotation = 0;
                }
            }
        }
    }
}
