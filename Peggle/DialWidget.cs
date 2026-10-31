#if PEGGLE_IPHONE
using System;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// iPhone only (Sexy::DialWidget): the control strip beside the board, with the aiming wheel and the
    /// fire button. HD draws its dial inside InterfaceMgr and handles it in Board's touch code.
    /// Board places it with SetPadLayout (right of the board, or left for left-handed play).
    /// </summary>
    public class DialWidget : Widget
    {
        public int mLastY;                                        // 0x7c

        public int mDelta;                                        // 0x80

        public int mDialCel;                                      // 0x84

        public bool mDown;                                        // 0x88

        public Board mBoard;                                      // 0x8c

        public TRect mDialRect;                                   // 0x90

        public TRect mFireRect;                                   // 0xa0

        // 000785b0
        public DialWidget(Board theBoard)
        {
            mBoard = theBoard;
            mDown = false;
            mDelta = 0;
            mLastY = 0;
            mDialCel = 0;
            mFireRect = new TRect(BOARD_FIRE_BUTTON_RECT_X, BOARD_FIRE_BUTTON_RECT_Y, BOARD_FIRE_BUTTON_RECT_W, BOARD_FIRE_BUTTON_RECT_H);
            mDialRect = new TRect(BOARD_DIAL_RECT_X, BOARD_DIAL_RECT_Y, BOARD_DIAL_RECT_W, BOARD_DIAL_RECT_H);
        }

        // 000787c8
        public override void Draw(Graphics g)
        {
            if (mBoard.mDreamTransitionCounter < 1)
            {
                DeferOverlay(0);
            }
        }

        // 000787f4
        public override void DrawOverlay(Graphics g)
        {
            if (mBoard.mZoom != 0.0f || mBoard.mZoomFrame != 0)
            {
                g.SetColor(Microsoft.Xna.Framework.Color.Black);
                g.FillRect(0, 0, SCREEN_WIDTH - BOARD_W, SCREEN_HEIGHT);
                g.SetColor(Microsoft.Xna.Framework.Color.White);
            }
            int aChar = mBoard.mCharacterMgr.GetCurCharacter();
            InterfaceMgr anInterface = mBoard.mInterfaceMgr;
            if (!mBoard.mRestored || aChar < 0 || aChar > 10)
            {
                g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForStage(anInterface.mStageNum - mBoard.mApp.mPlayerInfo.mFirstAdventureStage, ThunderballApp.mGameLevelPack, false)));
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForCharacter(aChar, ThunderballApp.mGameLevelPack, false)));
            }
            Image aFrame = SexyUtil.GetImageById(anInterface.mWheelFrameImageId);
            if (aFrame != null)
            {
                g.DrawImage(aFrame, 0, 0);
            }
            g.SetColorizeImages(false);
            g.DrawImageCel(Res.IMAGE_INT_WHEEL, BOARD_INT_WHEEL_X, BOARD_INT_WHEEL_Y, mDialCel);
            g.DrawImage(Res.IMAGE_INT_WHEEL_OVERLAY, BOARD_INT_WHEEL_OVERLAY_X, BOARD_INT_WHEEL_OVERLAY_Y);
            int x = BOARD_FIRE_BUTTON_X;
            int y = BOARD_FIRE_BUTTON_Y;
            int aState = mBoard.mLogicMgr.mState;
            // Glows while held down with the touch still over the fire button.
            if (!mIsDown || mBoard.mGameMode == GameMode.GAMEMODE_DEMO ||
                (aState != LogicMgr.STATE_AIMING && aState != LogicMgr.STATE_ZEN_SHOT) || mDown ||
                mFireRect.mHeight <= mWidgetManager.mLastMouseY)
            {
                g.DrawImage(Res.IMAGE_INT_FIRE_BUTTON, x, y);
            }
            else
            {
                g.DrawImage(Res.IMAGE_INT_FIRE_BUTTON_GLOW, x, y);
            }
            bool aFlip = false;
            if (aState != LogicMgr.STATE_AIMING)
            {
                aFlip = mBoard.mLogicMgr.mPowerupCount[2][mBoard.mLogicMgr.mCurPlayer] > 0;
            }
            g.DrawImage(aFlip ? Res.IMAGE_INT_FIRE_BUTTON_FLIP_TEXT : Res.IMAGE_INT_FIRE_BUTTON_FIRE_TEXT, x, y);
            if (mBoard.mApp.IsLeftHanded())
            {
                // Left-handed: the ball-o-tron frame is drawn again, clipped to this strip's width
                // (Graphics+0x24 is the clip rect's width, not the translation).
                int aClipW = g.mClipRect.mWidth;
                g.mClipRect.mWidth = SCREEN_WIDTH - BOARD_W;
                int aX = SexyUtil.gGutterWidth + INTERFACE_BALLOTRON_OFFSET_X + (SCREEN_WIDTH - BOARD_W) - 1;
                g.DrawImage(SexyUtil.GetImageById(anInterface.mBallFrameImageId), aX, 0);
                g.DrawImage(Res.IMAGE_IP_BALLOTRON_BADGE, aX, INTERFACE_BALLOTRON_BADGE_OFFSET_Y);
                g.mClipRect.mWidth = aClipW;
            }
        }

        // Desktop input: the original is touch-only, so mouse input is fed through the touch handlers.
        public override void MouseDown(int x, int y, int theBtnNum, int theClickCount)
        {
            if (theBtnNum < 0 || theBtnNum == 2 || mBoard.IsPCRightButton(theBtnNum))
            {
                return;
            }
            _Touch aTouch = default(_Touch);
            aTouch.location = new CGPoint(x, y);
            aTouch.previousLocation = aTouch.location;
            aTouch.tapCount = theClickCount;
            TouchBegan(aTouch);
        }

        public override void MouseDrag(int x, int y)
        {
            _Touch aTouch = default(_Touch);
            aTouch.location = new CGPoint(x, y);
            aTouch.previousLocation = aTouch.location;
            TouchMoved(aTouch);
        }

        public override void MouseUp(int x, int y, int theBtnNum, int theClickCount)
        {
            if (theBtnNum < 0 || theBtnNum == 2 || mBoard.IsPCRightButton(theBtnNum))
            {
                return;
            }
            _Touch aTouch = default(_Touch);
            aTouch.location = new CGPoint(x, y);
            aTouch.previousLocation = aTouch.location;
            aTouch.tapCount = theClickCount;
            TouchEnded(aTouch);
        }

        /// <summary>Port addition (PC Controls): the mouse wheel over the strip turns the gun as it does over the board.</summary>
        public override void MouseWheel(int theDelta)
        {
            if (mBoard.UsePCControls())
            {
                mBoard.MouseWheel(theDelta);
            }
        }

        // 00078c68
        public override void TouchBegan(_Touch theTouch)
        {
            int x = (int)theTouch.location.X;
            int y = (int)theTouch.location.Y;
            if (!mBoard.mInputEnabled)
            {
                return;
            }
            if (mFireRect.Contains(x, y) && mBoard.mGameMode != GameMode.GAMEMODE_DEMO)
            {
                PressButton();
            }
            else if (mDialRect.Contains(x, y))
            {
                mLastY = y;
                mDelta = 0;
                mDown = true;
            }
        }

        // 00078d3c
        public void PressButton()
        {
            mBoard.mLogicMgr.MouseDown(0, 0, 1, false, false);
        }

        // 00078d88
        public void ReleaseButton()
        {
            mBoard.mLogicMgr.MouseUp(0, 0, 1, false);
        }

        // 00078dcc
        public override void TouchMoved(_Touch theTouch)
        {
            int x = (int)theTouch.location.X;
            int y = (int)theTouch.location.Y;
            if (!mDialRect.Contains(x, y))
            {
                return;
            }
            if (mLastY > -1)
            {
                mDelta = mLastY - y;
                mBoard.mLogicMgr.MouseWheel(mBoard.mPeggleZoom ? mDelta * 0.2f : mDelta);
                if (Math.Abs(mDelta) < 4)
                {
                    mDelta = 0;
                }
            }
            mLastY = y;
        }

        // 00078eb8
        public override void TouchEnded(_Touch theTouch)
        {
            mLastY = -1;
            mDown = false;
            ReleaseButton();
        }

        // 00078ee0
        public override void Update()
        {
            if (!mBoard.mInputEnabled)
            {
                mLastY = -1;
                return;
            }
            if (!mDown && mDelta != 0)
            {
                mBoard.mLogicMgr.MouseWheel(mBoard.mPeggleZoom ? mDelta * 0.2f : mDelta);
                mDelta = mDelta * 0x5e / 100;
            }
            int anAngle = (int)SexyUtil.RadToDeg(mBoard.mGun.mAngle);
            if (anAngle > 0xb4)
            {
                anAngle = 0x168 - anAngle;
            }
            if (anAngle < 0)
            {
                anAngle = -anAngle;
            }
            mDialCel = anAngle % Res.IMAGE_INT_WHEEL.mNumCols;
        }

        // 00079024
        public void Reset()
        {
            mDown = false;
            mDelta = 0;
            mLastY = 0;
            mDialCel = 0;
        }
    }
}
#endif
