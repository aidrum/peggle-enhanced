using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Sexy
{
    /// <summary>
    /// Port addition (controller): a pointer for the screens that only take the mouse or touch. The left stick
    /// or the D-pad moves it, A clicks where it points (a touch on phones and tablets, the left button elsewhere,
    /// so screens see what they always do), the right stick scrolls and Start presses Enter. It shows from the first pad input until
    /// the mouse or a finger is used, and while the game drives the pad itself (SexyAppBase.GamePadUpdate).
    /// </summary>
    public static class GamePadCursor
    {
        /// <summary>Screen heights a second at full stick (the stick's travel is squared, for fine moves).</summary>
        private const float SPEED = 1.1f;

        /// <summary>D-pad speed, in screen heights a second.</summary>
        private const float DPAD_SPEED = 0.5f;

        /// <summary>Wheel notches a second at full right stick (WidgetManager.MouseWheel takes 120 a notch).</summary>
        private const float WHEEL_NOTCHES = 12f;

        private const int WHEEL_NOTCH = 120;

        private static bool sVisible;

        private static bool sDown;

        private static float sX = float.NaN;

        private static float sY;

        private static float sWheel;

        public static bool Visible => sVisible;

        /// <summary>Hides the pointer (the mouse moved, a finger touched, or the game took the pad), releasing A first.</summary>
        public static void Hide(SexyAppBase theApp)
        {
            if (sDown)
            {
                Release(theApp);
            }
            sVisible = false;
        }

        public static void Update(SexyAppBase theApp, GamePadState thePad, GamePadState thePrevPad, double theSeconds, double theTime)
        {
            if (!thePad.IsConnected)
            {
                Hide(theApp);
                return;
            }
            if (float.IsNaN(sX))
            {
                sX = theApp.mWidth / 2f;
                sY = theApp.mHeight / 2f;
            }
            // Start is Enter, which confirms the name prompt (it has no OK button) and other text dialogs.
            if (thePad.Buttons.Start == ButtonState.Pressed && thePrevPad.Buttons.Start == ButtonState.Released)
            {
                theApp.mWidgetManager.KeyDown(KeyCode.Return);
                theApp.mWidgetManager.KeyUp(KeyCode.Return);
            }
            Vector2 aStick = thePad.ThumbSticks.Left;
            float aStep = (float)(theApp.mHeight * theSeconds);
            float aMoveX = aStick.X * Math.Abs(aStick.X) * SPEED * aStep;
            float aMoveY = -aStick.Y * Math.Abs(aStick.Y) * SPEED * aStep;
            aMoveX += ((thePad.DPad.Right == ButtonState.Pressed ? 1 : 0) - (thePad.DPad.Left == ButtonState.Pressed ? 1 : 0)) * DPAD_SPEED * aStep;
            aMoveY += ((thePad.DPad.Down == ButtonState.Pressed ? 1 : 0) - (thePad.DPad.Up == ButtonState.Pressed ? 1 : 0)) * DPAD_SPEED * aStep;
            bool aPressed = thePad.Buttons.A == ButtonState.Pressed && thePrevPad.Buttons.A == ButtonState.Released;
            float aScroll = thePad.ThumbSticks.Right.Y;
            if (!sVisible)
            {
                // The first input only brings the pointer up where it was; a press doesn't click yet.
                if (aMoveX != 0 || aMoveY != 0 || aPressed || aScroll != 0)
                {
                    sVisible = true;
                    sWheel = 0;
                    theApp.mWidgetManager.MouseMove((int)sX, (int)sY);
                }
                return;
            }
            if (aMoveX != 0 || aMoveY != 0)
            {
                sX = Math.Clamp(sX + aMoveX, 0, theApp.mWidth - 1);
                sY = Math.Clamp(sY + aMoveY, 0, theApp.mHeight - 1);
                if (sDown && Main.IsMobile)
                {
                    theApp.TouchMoved(MakeTouch(theTime));
                }
                else
                {
                    theApp.mWidgetManager.MouseMove((int)sX, (int)sY);
                }
            }
            if (aPressed && !sDown)
            {
                sDown = true;
                if (Main.IsMobile)
                {
                    theApp.TouchBegan(MakeTouch(theTime));
                }
                else
                {
                    theApp.mWidgetManager.MouseDown((int)sX, (int)sY, 1);
                }
            }
            else if (sDown && thePad.Buttons.A == ButtonState.Released)
            {
                Release(theApp);
            }
            sWheel += aScroll * Math.Abs(aScroll) * WHEEL_NOTCHES * (float)theSeconds;
            int aNotches = (int)sWheel;
            if (aNotches != 0)
            {
                sWheel -= aNotches;
                theApp.mWidgetManager.MouseWheel(aNotches * WHEEL_NOTCH);
            }
        }

        private static void Release(SexyAppBase theApp)
        {
            sDown = false;
            if (Main.IsMobile)
            {
                theApp.TouchEnded(MakeTouch(0));
            }
            else
            {
                theApp.mWidgetManager.MouseUp((int)sX, (int)sY, 1);
            }
        }

        private static _Touch MakeTouch(double theTime)
        {
            _Touch aTouch = default(_Touch);
            aTouch.location = new CGPoint(sX, sY);
            aTouch.previousLocation = aTouch.location;
            aTouch.tapCount = 1;
            aTouch.timestamp = theTime;
            return aTouch;
        }

        /// <summary>An arrow, white edged with black, its tip at the pointer; a 768-high screen draws it 32 high.</summary>
        public static void Draw(SexyAppBase theApp)
        {
            if (!sVisible)
            {
                return;
            }
            int aUnit = Math.Max(1, (theApp.mHeight + 383) / 384);
            int aX = (int)sX;
            int aY = (int)sY;
            Graphics g = Graphics.GetNew();
            g.SetColor(Color.Black);
            for (int i = 0; i < 16; i++)
            {
                g.FillRect(aX - aUnit, aY + (i - 1) * aUnit, (ArrowWidth(i) + 2) * aUnit, aUnit * 3);
            }
            g.SetColor(Color.White);
            for (int i = 0; i < 16; i++)
            {
                g.FillRect(aX, aY + i * aUnit, ArrowWidth(i) * aUnit, aUnit);
            }
            g.PrepareForReuse();
        }

        /// <summary>The arrow's width on row theRow: it widens to row 11, then narrows to a short tail.</summary>
        private static int ArrowWidth(int theRow)
        {
            return theRow < 12 ? theRow * 2 / 3 + 1 : 3;
        }
    }
}
