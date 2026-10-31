using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Peggle's standard button (Sexy::ThunderButton): a DialogButton with a layered FONT_BUTTON
    /// colour set, click sound, blinking/pulsing highlight, fade alpha, and an optional caption
    /// drawn over the coloured main-menu buttons.
    /// </summary>
    public class ThunderButton : DialogButton
    {
        public int mClickSound;                                   // 0x10c

        public int mUnk110;                                       // 0x110

        public int mUnk118;                                       // 0x118

        public bool mDraw;                                        // 0x11c

        public int mBlinkCount;                                   // 0x120

        public bool mTouchCancelled;                              // 0x124

        public bool mPulseBlink;                                  // 0x125

        public int mBlinkPeriod;                                  // 0x128

        public int mOverlayIndex;                                 // 0x12c

        public bool mUnk130;                                      // 0x130

        public bool mHighlightOnDown;                             // 0x131

        public int mFontColor;                                    // 0x134

        public string mOverlayText = "";                          // 0x138

        public bool mHasOverlay;                                  // 0x13c

        public float mFadeAlpha;                                  // 0x140

        public ThunderButton(Image theImage, int theId, ButtonListener theListener) : base(theImage, theId, theListener)
        {
            mHasOverlay = false;
            if (theImage == Res.IMAGE_BUTTON_ORANGE && theImage != null)
            {
                mOverlayIndex = BUTTON_ORANGE_INDEX;
                mHasOverlay = true;
            }
            else if (theImage == Res.IMAGE_MM_NIGHTS_BUTTON_PURPLE && theImage != null)
            {
                mOverlayIndex = BUTTON_PURPLE_INDEX;
                mHasOverlay = true;
            }
            else if (theImage == Res.IMAGE_MM_BUTTON_BLUE && theImage != null)
            {
                mOverlayIndex = BUTTON_BLUE_INDEX;
                mHasOverlay = true;
            }
            mUnk118 = -1000;
            mClickSound = -1;
            mUnk110 = -1;
            mDraw = true;
            mOverAlphaSpeed = 0.08;
            mOverAlphaFadeInSpeed = 0.2;
            mFontColor = ButtonFontColor.NONE;
            mTouchCancelled = false;
            mBlinkCount = 0;
            mPulseBlink = true;
            mBlinkPeriod = 120;
            mUnk130 = false;
            mHighlightOnDown = false;
            mFadeAlpha = 1.0f;
            mTranslateY = 0;
            mTranslateX = 0;
        }

        public void SetDraw(bool theDraw)
        {
            mDraw = theDraw;
        }

        public void SetFadeAlpha(float theAlpha)
        {
            mFadeAlpha = theAlpha;
        }

        public void SetClickSound(int theSound)
        {
            mClickSound = theSound;
        }

        public void Blink(int theCount, bool startOn)
        {
            mBlinkCount = theCount * mBlinkPeriod - 1;
            if (!startOn)
            {
                mBlinkCount += mBlinkPeriod / 2;
            }
        }

        public void StopBlink()
        {
            mBlinkCount = 0;
        }

        public override bool IsPointVisible(int x, int y)
        {
            return true;
        }

        public override void MouseEnter()
        {
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            if (mClickSound >= 0)
            {
                GlobalStaticVars.gSexyAppBase.PlaySample(mClickSound);
            }
            base.MouseDown(x, y, theClickCount);
        }

        public override void MouseUp(int x, int y, int theClickCount)
        {
            mTouchCancelled = theClickCount < 0;
            base.MouseUp(x, y, theClickCount);
        }

        public override void Update()
        {
            base.Update();
            if (mBlinkCount != 0)
            {
                mBlinkCount--;
                MarkDirty();
            }
        }

        public override void Draw(Graphics g)
        {
            if (!mDraw)
            {
                return;
            }
            g.SetColorizeImages(true);
            g.SetColor(new Color(255, 255, 255, (int)(mFadeAlpha * 255.0f)));
            if (mFontColor != ButtonFontColor.NONE)
            {
                if (mIsDown && mIsOver)
                {
                    SexyUtil.SetButtonFontColor(mFontColor + 2, mFadeAlpha);
                }
                else if (!mIsOver && !mIsDown)
                {
                    SexyUtil.SetButtonFontColor(mFontColor, mFadeAlpha);
                }
                else
                {
                    SexyUtil.SetButtonFontColor(mFontColor + 1, mFadeAlpha);
                }
            }
            if (mBlinkCount == 0 || (!mPulseBlink && mBlinkCount / (mBlinkPeriod / 2) % 2 == 0) || mIsDown || mIsOver)
            {
                base.Draw(g);
                if (mHighlightOnDown && mIsDown && mIsOver)
                {
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0x404040, (int)(mFadeAlpha * 255.0f)));
                    base.Draw(g);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                    g.SetColorizeImages(false);
                }
            }
            else
            {
                bool wasOver = mIsOver;
                double overAlpha = mOverAlpha;
                if (!mPulseBlink)
                {
                    mIsOver = true;
                    mOverAlpha = 0.0;
                    base.Draw(g);
                }
                else
                {
                    int hold = 30;
                    int cycle = mBlinkPeriod + hold;
                    int half = mBlinkPeriod / 2;
                    int t = mBlinkCount % cycle;
                    if (t < half)
                    {
                        mOverAlpha = t / (float)half;
                    }
                    else if (t < half + hold)
                    {
                        mOverAlpha = 1.0;
                    }
                    else
                    {
                        mOverAlpha = (cycle - t) / (float)half;
                    }
                    mOverAlpha *= mOverAlpha;
                    mOverAlpha *= 0.699999988079071;
                    mOverAlpha *= mFadeAlpha;
                    mIsOver = false;
                    TRect down = mDownRect;
                    mDownRect = mOverRect;
                    mOverRect = down;
                    base.Draw(g);
                    down = mDownRect;
                    mDownRect = mOverRect;
                    mOverRect = down;
                }
                mIsOver = wasOver;
                mOverAlpha = overAlpha;
            }
            if (mHasOverlay && mFadeAlpha == 1.0f)
            {
                g.SetScale(0.8f, 0.8f, mWidth / 2, mHeight / 2);
                g.SetColorizeImages(true);
                if (mOverlayIndex == BUTTON_ORANGE_INDEX)
                {
                    g.SetFont(Res.FONT_OVERLOAD24GRAD_ORANGE);
                    g.SetColor(SexyUtil.MakeColor(0xfef5b8));
                }
                else if (mOverlayIndex == BUTTON_PURPLE_INDEX)
                {
                    g.SetFont(Res.FONT_OVERLOAD24GRAD_PURPLE);
                    g.SetColor(SexyUtil.MakeColor(0xffddf6));
                }
                else if (mOverlayIndex == BUTTON_BLUE_INDEX)
                {
                    g.SetFont(Res.FONT_OVERLOAD24GRAD_BLUE);
                    g.SetColor(SexyUtil.MakeColor(0x01e8ff));
                }
                else
                {
                    g.SetColorizeImages(false);
                    return;
                }
                int x = (mWidth - g.GetFont().StringWidth(mOverlayText)) / 2;
                int y = (g.GetFont().GetCenteredHeight() + mHeight) / 2;
                if (mOverlayText.Length < 13)
                {
                    g.DrawString(mOverlayText, x, THUNDER_BUTTON_SINGLE_LINE_TEXT_OFFSET_Y + y);
                }
                else
                {
#if PEGGLE_IPHONE
                    WriteWordWrapped(g, new TRect(0, THUNDER_BUTTON_MULTI_LINE_TEXT_OFFSET_Y, (int)(mWidth * 1.1), mHeight), mOverlayText, -1, 0);
#else
                    WriteWordWrapped(g, new TRect(0, THUNDER_BUTTON_MULTI_LINE_TEXT_OFFSET_Y, mWidth, mHeight), mOverlayText, -1, 0);
#endif
                }
                g.SetScale(1.0f);
            }
            g.SetColorizeImages(false);
        }
    }

    /// <summary>A button that draws a single image tinted by its state (Sexy::SimpleButton).</summary>
    public class SimpleButton : ButtonWidget
    {
        public Image mImage;                                      // 0xf8

        public int mNormalColor;                                  // 0xfc

        public int mOverColor;                                    // 0x100

        public int mDownColor;                                    // 0x104

        public SimpleButton(Image theImage, int theId, ButtonListener theListener) : base(theId, theListener)
        {
            mDoFinger = true;
            mImage = theImage;
            mOverColor = 0x88ff88;
            mDownColor = 0x88ff88;
            mNormalColor = 0xffff00;
        }

        public override void Draw(Graphics g)
        {
            int x = (mWidth - mImage.mWidth) / 2;
            int y = (mHeight - mImage.mHeight) / 2;
            if (mIsDown && mIsOver)
            {
                g.SetColor(SexyUtil.MakeColor(mDownColor));
                x++;
                y++;
            }
            else if (mIsOver)
            {
                g.SetColor(SexyUtil.MakeColor(mOverColor));
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(mNormalColor));
            }
            g.SetColorizeImages(true);
            g.DrawImage(mImage, x, y);
            g.SetColorizeImages(false);
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            base.MouseDown(x, y, theClickCount);
            GlobalStaticVars.gSexyAppBase.PlaySample(Res.SOUND_BUTTON1);
            MarkDirty();
        }

        public override void MouseUp(int x, int y, int theBtnNum, int theClickCount)
        {
            base.MouseUp(x, y, theBtnNum, theClickCount);
            MarkDirty();
        }

        public override void MouseEnter()
        {
            base.MouseEnter();
            MarkDirty();
        }

        public override void MouseLeave()
        {
            base.MouseLeave();
            MarkDirty();
        }
    }

    /// <summary>Peggle's checkbox / radio button with a FONT_BUTTON caption (Sexy::ThunderCheckbox).</summary>
    public class ThunderCheckbox : Checkbox
    {
        public Font mFont;                                        // 0xdc

        public string mLabel = "";                                // 0xe0

        public int mClickSound;                                   // 0xe4

        public bool mIsRadio;                                     // 0xe8

        public int mTextOffsetX;                                  // 0xec

        public int mTextOffsetY;                                  // 0xf0

        public int mStyle;                                        // 0xf4

        public TRect mOverUncheckedRect = default(TRect);         // 0xf8

        public TRect mOverCheckedRect = default(TRect);           // 0x108

        public double mOverAlpha;                                 // 0x118

        public double mOverAlphaSpeed;                            // 0x120

        public double mOverAlphaFadeInSpeed;                      // 0x128

        public float mFadeAlpha;                                  // 0x130

        /// <summary>
        /// Port addition: drawn (box and caption) at this scale, so a small checkbox keeps the art's proportions:
        /// the art is laid out for the widget's size divided by it. Its touch area is the widget's own rect.
        /// </summary>
        public float mDrawScale = 1;

        public ThunderCheckbox(Image theUncheckedImage, Image theCheckedImage, int theId, CheckboxListener theListener)
            : base(theUncheckedImage, theCheckedImage, theId, theListener)
        {
            mClickSound = -1;
            mIsRadio = false;
            mFont = Res.FONT_BUTTON;
            mTextOffsetY = 0;
            mTextOffsetX = 0;
            mOverAlpha = 0.0;
            mOverAlphaSpeed = 0.08;
            mOverAlphaFadeInSpeed = 0.2;
            mStyle = 0;
            mFadeAlpha = 1.0f;
        }

        public void SetFadeAlpha(float theAlpha)
        {
            mFadeAlpha = theAlpha;
            MarkDirty();
        }

        public void SetClickSound(int theSound)
        {
            mClickSound = theSound;
        }

        public override void Draw(Graphics g)
        {
            if (mDrawScale == 1)
            {
                DrawScaled(g, mWidth, mHeight);
                return;
            }
            // Port: shrunk about the widget's corner (its translation), as the board's zoom scales its drawing.
            XnaTransform2D aTrans = new XnaTransform2D();
            aTrans.Translate(-g.mTransX, -g.mTransY);
            aTrans.Scale(mDrawScale, mDrawScale);
            aTrans.Translate(g.mTransX, g.mTransY);
            Graphics.PushTransform(ref aTrans, true);
            // The clip rect is scaled along with the drawing, so it's widened to match (about the same corner).
            TRect anOldClip = g.mClipRect;
            g.mClipRect = new TRect(anOldClip.mX, anOldClip.mY, (int)System.Math.Ceiling(anOldClip.mWidth / mDrawScale),
                (int)System.Math.Ceiling(anOldClip.mHeight / mDrawScale));
            DrawScaled(g, (int)System.Math.Round(mWidth / mDrawScale), (int)System.Math.Round(mHeight / mDrawScale));
            g.mClipRect = anOldClip;
            Graphics.PopTransform();
        }

        private void DrawScaled(Graphics g, int mWidth, int mHeight)
        {
            TRect dest = new TRect(0, 0, mWidth, mHeight);
            if (mStyle == 1)
            {
                if (mChecked || (mIsDown && mIsOver))
                {
                    SexyUtil.SetButtonFontColor(0x10, mFadeAlpha);
                }
                else if (!mIsOver && !mIsDown)
                {
                    SexyUtil.SetButtonFontColor(0xd, mFadeAlpha);
                }
                else
                {
                    SexyUtil.SetButtonFontColor(0xf, mFadeAlpha);
                }
            }
            else if (mIsOver)
            {
                SexyUtil.SetButtonFontColor(0xe, mFadeAlpha);
            }
            else
            {
                SexyUtil.SetButtonFontColor(0xd, mFadeAlpha);
            }
            TRect normal = mChecked ? mCheckedRect : mUncheckedRect;
            TRect over = mChecked ? mOverCheckedRect : mOverUncheckedRect;
            g.SetColorizeImages(true);
            g.SetColor(new Color(255, 255, 255, (int)(mFadeAlpha * 255.0f)));
            if (mOverAlpha <= 0.0 || over.mHeight < 1)
            {
                if (mIsOver && over.mHeight >= 1)
                {
                    g.DrawImageBox(over, dest, mUncheckedImage);
                }
                else
                {
                    g.DrawImageBox(normal, dest, mUncheckedImage);
                }
            }
            else
            {
                if (mOverAlpha < 1.0)
                {
                    g.DrawImageBox(normal, dest, mUncheckedImage);
                }
                g.SetColor(new Color(255, 255, 255, (int)(mOverAlpha * 255.0 * mFadeAlpha)));
                g.DrawImageBox(over, dest, mUncheckedImage);
            }
            g.SetColorizeImages(false);
            g.SetFont(mFont);
            int textWidth = g.GetFont().StringWidth(mLabel);
            int x = mStyle == 1 ? (mWidth - textWidth) / 2 + mTextOffsetX : THUNDER_BUTTON_POS_X + (mWidth - textWidth) / 2;
            int y = mFont.GetAscent() + (mHeight - mFont.GetHeight()) / 2 + mTextOffsetY;
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.DrawString(mLabel, x, y);
        }

        public override void MouseEnter()
        {
            base.MouseEnter();
            if (mOverAlphaFadeInSpeed == 0.0 && mOverAlpha > 0.0)
            {
                mOverAlpha = 0.0;
            }
            MarkDirty();
        }

        public override void MouseLeave()
        {
            base.MouseLeave();
            if (mOverAlphaSpeed == 0.0 && mOverAlpha > 0.0)
            {
                mOverAlpha = 0.0;
            }
            else if (mOverAlphaSpeed > 0.0 && mOverAlpha == 0.0)
            {
                mOverAlpha = 1.0;
            }
            MarkDirty();
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            if (!mIsRadio || !mChecked)
            {
                base.MouseDown(x, y, theClickCount);
            }
            if (mClickSound >= 0)
            {
                GlobalStaticVars.gSexyAppBase.PlaySample(mClickSound);
            }
        }

        public override void MouseUp(int x, int y, int theClickCount)
        {
            base.MouseUp(x, y, theClickCount);
            MarkDirty();
        }

        public override void Update()
        {
            base.Update();
            if (!mIsDown && !mIsOver && mOverAlpha > 0.0)
            {
                if (mOverAlphaSpeed > 0.0)
                {
                    mOverAlpha -= mOverAlphaSpeed;
                    if (mOverAlpha < 0.0)
                    {
                        mOverAlpha = 0.0;
                    }
                }
                else
                {
                    mOverAlpha = 0.0;
                }
                MarkDirty();
            }
            else if (mIsOver && mOverAlphaFadeInSpeed > 0.0 && mOverAlpha < 1.0)
            {
                mOverAlpha += mOverAlphaFadeInSpeed;
                if (mOverAlpha > 1.0)
                {
                    mOverAlpha = 1.0;
                }
                MarkDirty();
            }
        }
    }
}
