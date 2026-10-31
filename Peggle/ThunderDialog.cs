using System;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Peggle's dialog base (Sexy::ThunderDialog): a Dialog drawn over a dimmed screen with a
    /// striped fill, Thunder buttons, and slide-in/slide-out scrolling.
    /// </summary>
    public class ThunderDialog : Dialog
    {
        public const int NO_SCROLL = -100000;

        public int mScrollTargetY;                                // 0xf8

        public int mNextScrollTargetY;                            // 0xfc

        public int mScrollDX;                                     // 0x100

        public int mScrollDir;                                    // 0x104

        public bool mScrollingOff;                                // 0x108

        public bool mKillOnScrollOff;                             // 0x109

        public bool mFastScrollOff;                               // 0x10a

        public bool mCloseOnButton;                               // 0x10b

        public bool mDrawLinesOnly;                               // 0x10c

        public bool mAutoSizeYesButton;                           // 0x10d

        public int mBounceOffset;                                 // 0x110

        public int mInt114;                                       // 0x114

        public int mLastY;                                        // 0x118

        public int mTipId;                                        // 0x11c

        public bool mAlwaysMarkDirty;                             // 0x120

        public Action<ThunderDialog, Graphics> mDrawCallback;     // 0x124

        public ThunderDialog(int theId, bool isModal, string theHeader, string theLines, string theFooter, int theButtonMode)
            : base(Res.IMAGE_DLG_DIALOGBOX, Res.IMAGE_DLG_BUTTON3, theId, isModal, theHeader, theLines, theFooter, theButtonMode)
        {
            mContentInsets = new Insets(THUNDER_DIALOG_INSET_X, THUNDER_DIALOG_INSET_Y, THUNDER_DIALOG_INSET_W, THUNDER_DIALOG_INSET_H);
            mClip = false;
            SetHeaderFont(SexyUtil.GetFontById((int)ResourceId.FONT_OVERLOAD24GRAD1_ID));
            SetLinesFont(SexyUtil.GetFontById((int)ResourceId.FONT_PLAINOUTLINE_ID));
            mButtonHeight = Res.IMAGE_DLG_BUTTON3.mHeight / 3;
            mYesButton = SexyUtil.MakeButtonCopy(mYesButton);
            mNoButton = SexyUtil.MakeButtonCopy(mNoButton);
            SexyUtil.SetupButton(mYesButton, false);
            SexyUtil.SetupButton(mNoButton, false);
            SetColor(3, SexyUtil.MakeColor(0xffffff));
            SetColor(4, SexyUtil.MakeColor(0xffffff));
            SetColor(1, SexyUtil.MakeColor(0xffffff));
            SetColor(0, SexyUtil.MakeColor(0x63f7ff));
            mDrawLinesOnly = false;
            mAutoSizeYesButton = false;
            mScrollDir = -1;
            mScrollTargetY = NO_SCROLL;
            mNextScrollTargetY = NO_SCROLL;
            mScrollDX = 0;
            mScrollingOff = false;
            mKillOnScrollOff = false;
            mCloseOnButton = false;
            mFastScrollOff = true;
            mSpaceAfterHeader = DIALOG_SAPCE_AFTER_HEADER;
            mInt114 = 0x23;
            mLastY = 0;
            mDrawCallback = null;
            mAlwaysMarkDirty = false;
            mTipId = 0;
        }

        public bool IsScrolling()
        {
            return mScrollTargetY != NO_SCROLL;
        }

        /// <summary>Slides the dialog in from below (theDir &lt;= 0) or above (theDir &gt; 0).</summary>
        public void DoScroll(int theDir)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            app.PlaySample(Res.SOUND_DIALOG_MOVE);
            mScrollDir = theDir;
            if (mY < 0)
            {
                mScrollTargetY = app.mHeight * 100 / 600;
            }
            else
            {
                mScrollTargetY = mY;
            }
            if (mScrollDir < 1)
            {
                mY = app.mHeight * 600 / 600;
            }
            else
            {
                mY = -mHeight;
            }
            mLastY = mY;
            mBounceOffset = 0;
            if (theDir < 1)
            {
                mNextScrollTargetY = NO_SCROLL;
            }
            mKillOnScrollOff = false;
            mScrollingOff = false;
            Resize(mX, mY, mWidth, mHeight);
            mWidgetFlagsMod.mRemoveFlags = 0;
            mMouseVisible = true;
        }

        /// <summary>Slides the dialog off the top of the screen, optionally deleting it afterwards.</summary>
        public void DoScrollOff(bool killAfter)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            app.PlaySample(Res.SOUND_DIALOG_MOVE);
            mScrollDir = -1;
            mScrollTargetY = -mHeight;
            mScrollingOff = true;
            mKillOnScrollOff = killAfter;
            app.KillDialog(mId, false, false);
            mWidgetFlagsMod.mRemoveFlags |= 0x30;
            mMouseVisible = false;
        }

        public void FinishScroll()
        {
            if (mScrollTargetY != NO_SCROLL)
            {
                mY = mScrollTargetY;
            }
        }

        public void CancelScroll()
        {
            mWidgetFlagsMod.mRemoveFlags = 0;
            mMouseVisible = true;
            mScrollDir = -1;
            mScrollTargetY = NO_SCROLL;
            mNextScrollTargetY = NO_SCROLL;
            mScrollDX = 0;
        }

        public int GetLeft()
        {
            return mBackgroundInsets.mLeft + mX + mContentInsets.mLeft;
        }

        public int GetTop()
        {
            return mHeaderFont.GetHeight() + mY + mContentInsets.mTop + mBackgroundInsets.mTop + mSpaceAfterHeader - 0xc;
        }

        public int GetWidth()
        {
            return mWidth - mContentInsets.mLeft - mContentInsets.mRight - mBackgroundInsets.mLeft - mBackgroundInsets.mRight;
        }

        public override void Update()
        {
            base.Update();
            if (mScrollTargetY != NO_SCROLL)
            {
                int target = mScrollTargetY;
                if (mScrollDir > 0)
                {
                    target += mBounceOffset;
                }
                int speed = SexyUtil.ClampRange(Math.Abs(mY - target) / 5, 10, 0x28);
                if (mScrollingOff && mFastScrollOff)
                {
                    speed = 0x28;
                }
                int newY;
                if (mScrollDir < 0)
                {
                    newY = mY - speed;
                    if (newY <= target)
                    {
                        newY = target;
                        mScrollTargetY = NO_SCROLL;
                    }
                }
                else
                {
                    newY = mY + speed;
                    if (target <= newY)
                    {
                        newY = target;
                        if (mBounceOffset < 1)
                        {
                            mScrollTargetY = NO_SCROLL;
                        }
                        else
                        {
                            mScrollDir = -1;
                            mBounceOffset = 0;
                        }
                    }
                }
                mX += mScrollDX;
                Resize(mX, newY, mWidth, mHeight);
                if (mScrollTargetY == NO_SCROLL)
                {
                    mLastY = mY;
                    if (mNextScrollTargetY == NO_SCROLL)
                    {
                        if (mScrollingOff)
                        {
                            mWidgetManager?.RemoveWidget(this);
                            if (mKillOnScrollOff)
                            {
                                GlobalStaticVars.gSexyAppBase.SafeDeleteWidget(this);
                            }
                        }
                    }
                    else
                    {
                        mScrollTargetY = mNextScrollTargetY;
                        mNextScrollTargetY = NO_SCROLL;
                    }
                }
            }
            if (mAlwaysMarkDirty)
            {
                MarkDirty();
            }
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            if (mAutoSizeYesButton && mButtonMode == 3)
            {
                int w = mYesButton.mFont.StringWidth(mYesButton.mLabel) + THUNDER_DIALOG_YES_BUTTON_W;
                if (w < THUNDER_DIALOG_YES_BUTTON_MIN_W)
                {
                    w = THUNDER_DIALOG_YES_BUTTON_MIN_W;
                }
                mYesButton.Resize(mX + mWidth / 2 - w / 2, mYesButton.mY, w, mYesButton.mHeight);
            }
        }

        public override void Draw(Graphics g)
        {
            if (!mDrawLinesOnly)
            {
                int transX = g.mTransX;
                int transY = g.mTransY;
                g.mTransX = 0;
                g.mTransY = 0;
                g.SetColor(SexyUtil.MakeColor(unchecked((int)0xaa000000)));
#if PEGGLE_IPHONE
                GetWindowMargins(out int aMarginL, out int aMarginR);
                Graphics aWide = g.Create();
                aWide.ClearClipRect();
                aWide.FillRect(-aMarginL, 0, GlobalStaticVars.gSexyAppBase.mWidth + aMarginL + aMarginR, SCREEN_HEIGHT);
                g.mTransX = transX;
                g.mTransY = transY;
                if (mWidth >= GlobalStaticVars.gSexyAppBase.mWidth && (aMarginL > 0 || aMarginR > 0))
                {
                    // Port addition (ultrawide): a full-screen dialog's fill and frame reach the window's
                    // edges; its contents stay where they are, centred on the game.
                    aWide = g.Create();
                    aWide.ClearClipRect();
                    aWide.mTransX -= aMarginL;
                    int aWidth = mWidth;
                    mWidth += aMarginL + aMarginR;
                    SexyUtil.DrawDialogFill(aWide, new TRect(0, 0, mWidth, mHeight), 0x120952, 0x170b64);
                    base.Draw(aWide);
                    mWidth = aWidth;
                }
                else
                {
                    SexyUtil.DrawDialogFill(g, new TRect(0, 0, mWidth, mHeight), 0x120952, 0x170b64);
                    base.Draw(g);
                }
#else
                g.FillRect(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
                g.mTransX = transX;
                g.mTransY = transY;
                SexyUtil.DrawDialogFill(g, new TRect(0, 0, mWidth, mHeight), 0x120952, 0x170b64);
                base.Draw(g);
#endif
            }
            else
            {
                int y = mContentInsets.mTop + mBackgroundInsets.mTop;
                g.SetFont(mLinesFont);
                g.SetColor(mColors[1]);
                TRect rect = new TRect(mBackgroundInsets.mLeft + mContentInsets.mLeft + 2, y,
                    mWidth - mContentInsets.mLeft - mContentInsets.mRight - mBackgroundInsets.mLeft - mBackgroundInsets.mRight - 4, 0);
                WriteWordWrapped(g, rect, mDialogLines, mLinesFont.GetLineSpacing() + mLineSpacingOffset, mTextAlign);
            }
            mDrawCallback?.Invoke(this, g);
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
        }

#if PEGGLE_IPHONE
        /// <summary>
        /// Port addition (ultrawide): how far the window reaches past the game (mApp.mWidth, at least
        /// the iPhone 5's 1136) on each side, in game pixels. Zero when it does not.
        /// </summary>
        public static void GetWindowMargins(out int theLeft, out int theRight)
        {
            theLeft = 0;
            theRight = 0;
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            ScreenScales s = app.mScreenScales;
            if (s.mTransX <= 0 || s.mScaleFactor <= 0)
            {
                return;
            }
            theLeft = Math.Max(0, (int)Math.Ceiling(s.mTransX / s.mScaleFactor));
            theRight = Math.Max(0, (int)Math.Ceiling((s.mWidth - s.mTransX) / s.mScaleFactor) - app.mWidth);
        }

        /// <summary>
        /// Port addition (ultrawide): for a full-screen widget (theWidth at least the game's), a copy of g
        /// that may draw past the game; otherwise g itself. Create it after setting colours, as it copies
        /// g's state.
        /// </summary>
        public static Graphics CreateWideGraphics(Graphics g, int theWidth)
        {
            GetWindowMargins(out int aLeft, out int aRight);
            if (theWidth < GlobalStaticVars.gSexyAppBase.mWidth || (aLeft == 0 && aRight == 0))
            {
                return g;
            }
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            return aWide;
        }

        /// <summary>Port addition (ultrawide): theRect widened out to the window's edges for a full-screen widget.</summary>
        public static TRect GetWideRect(int theWidth, TRect theRect)
        {
            GetWindowMargins(out int aLeft, out int aRight);
            if (theWidth >= GlobalStaticVars.gSexyAppBase.mWidth)
            {
                theRect.mX -= aLeft;
                theRect.mWidth += aLeft + aRight;
            }
            return theRect;
        }

        /// <summary>
        /// Port addition (ultrawide): how far a portrait at the screen's left edge moves left so it stays at
        /// the window's corner (0 unless the window is wider than the game).
        /// </summary>
        public static int GetLeftCornerShift()
        {
            GetWindowMargins(out int aLeft, out _);
            return -aLeft;
        }

        /// <summary>Port addition (ultrawide): GetLeftCornerShift for a widget theWidth wide (0 unless it is full-screen).</summary>
        public static int GetLeftCornerShift(int theWidth)
        {
            return theWidth >= GlobalStaticVars.gSexyAppBase.mWidth ? GetLeftCornerShift() : 0;
        }

        /// <summary>Port addition (ultrawide): the same for a portrait at the screen's right edge.</summary>
        public static int GetRightCornerShift()
        {
            GetWindowMargins(out _, out int aRight);
            return aRight;
        }
#endif

        public override void KeyDown(KeyCode theKey)
        {
            base.KeyDown(theKey);
        }
    }
}
