using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sexy
{
    /// <summary>
    /// Development aid: a scripted run driven by the SEXY_DEBUG_SCRIPT environment variable, e.g.
    /// "shot:300:a.png;click:400:512,600;drag:450:800,300,200,300;key:500:b;quit:600". Steps fire on the given update count;
    /// the game keeps updating while unfocused. Not used in normal play.
    /// </summary>
    public static class DebugScript
    {
        private struct Step
        {
            public int mFrame;

            public string mKind;

            public string mArg;
        }

        private static readonly List<Step> sSteps = new List<Step>();

        public static bool Active { get; private set; }

        public static int Frame { get; private set; }

        private static string sPendingShot;

        /// <summary>The pad a pad step set (Main feeds it to the game in place of the real one); null before any.</summary>
        public static GamePadState? Pad { get; private set; }

        /// <summary>A shot is waiting for the next draw; Main holds updates until then, so shots on consecutive frames all get taken.</summary>
        public static bool ShotPending => sPendingShot != null;

        private static int[] sDrag;

        private static int sDragStep = -1;

        private const int DRAG_FRAMES = 10;

        public static void Init()
        {
            string script = Environment.GetEnvironmentVariable("SEXY_DEBUG_SCRIPT");
            if (string.IsNullOrEmpty(script))
            {
                return;
            }
            Active = true;
            foreach (string part in script.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] f = part.Split(':', 3);
                sSteps.Add(new Step { mKind = f[0], mFrame = int.Parse(f[1]), mArg = f.Length > 2 ? f[2] : "" });
            }
        }

        /// <summary>Runs the steps due this update; returns false when the game should exit.</summary>
        public static bool Update(SexyAppBase theApp)
        {
            if (!Active)
            {
                return true;
            }
            Frame++;
            if (Frame == 1)
            {
                ScreenScales sc = theApp.mScreenScales;
                GamePadCapabilities aPad = GamePad.GetCapabilities(PlayerIndex.One);
                Console.WriteLine($"[debug] pad {(aPad.IsConnected ? aPad.DisplayName : "none")}");
                Console.WriteLine($"[debug] app {theApp.mWidth}x{theApp.mHeight} scale {sc.mScaleFactor} trans {sc.mTransX},{sc.mTransY} screen {sc.mWidth}x{sc.mHeight} bb {GlobalStaticVars.g.GraphicsDevice.PresentationParameters.BackBufferWidth}x{GlobalStaticVars.g.GraphicsDevice.PresentationParameters.BackBufferHeight}");
            }
            if (sDragStep >= 0)
            {
                sDragStep++;
                int dx = sDrag[0] + (sDrag[2] - sDrag[0]) * sDragStep / DRAG_FRAMES;
                int dy = sDrag[1] + (sDrag[3] - sDrag[1]) * sDragStep / DRAG_FRAMES;
                if (sDragStep < DRAG_FRAMES)
                {
                    theApp.mWidgetManager.MouseDrag(dx, dy);
                }
                else
                {
                    theApp.mWidgetManager.MouseDrag(sDrag[2], sDrag[3]);
                    theApp.mWidgetManager.MouseUp(sDrag[2], sDrag[3], 1);
                    sDragStep = -1;
                }
            }
            foreach (Step s in sSteps)
            {
                if (s.mFrame != Frame)
                {
                    continue;
                }
                switch (s.mKind)
                {
                case "click":
                {
                    string[] xy = s.mArg.Split(',');
                    int x = int.Parse(xy[0]);
                    int y = int.Parse(xy[1]);
                    theApp.mWidgetManager.MouseMove(x, y);
                    theApp.mWidgetManager.MouseDown(x, y, 1);
                    theApp.mWidgetManager.MouseUp(x, y, 1);
                    break;
                }
                case "dclick":
                {
                    // dclick:F:x,y - a double click (second press has click count 2).
                    string[] xy = s.mArg.Split(',');
                    int x = int.Parse(xy[0]);
                    int y = int.Parse(xy[1]);
                    theApp.mWidgetManager.MouseMove(x, y);
                    theApp.mWidgetManager.MouseDown(x, y, 1);
                    theApp.mWidgetManager.MouseUp(x, y, 1);
                    theApp.mWidgetManager.MouseDown(x, y, 2);
                    theApp.mWidgetManager.MouseUp(x, y, 2);
                    break;
                }
                case "move":
                {
                    // move:F:x,y - hover without pressing.
                    string[] xy = s.mArg.Split(',');
                    theApp.mWidgetManager.MouseMove(int.Parse(xy[0]), int.Parse(xy[1]));
                    break;
                }
                case "drag":
                {
                    // drag:F:x1,y1,x2,y2 - press, move over DRAG_FRAMES updates, release.
                    sDrag = Array.ConvertAll(s.mArg.Split(','), int.Parse);
                    theApp.mWidgetManager.MouseMove(sDrag[0], sDrag[1]);
                    theApp.mWidgetManager.MouseDown(sDrag[0], sDrag[1], 1);
                    sDragStep = 0;
                    break;
                }
                case "key":
                    foreach (char c in s.mArg)
                    {
                        theApp.mWidgetManager.KeyChar(new SexyChar(c));
                    }
                    break;
                case "enter":
                    theApp.mWidgetManager.KeyDown(KeyCode.Return);
                    theApp.mWidgetManager.KeyUp(KeyCode.Return);
                    break;
                case "shot":
                    sPendingShot = s.mArg;
                    break;
                case "pad":
                    // pad:F:lx,ly,held... - the pad from this update on: the left stick and the buttons held
                    // (a b x y lb rb lt rt start back up down left right rup rdown), e.g. "pad:300:0.5,0,a"; "pad:F:0,0" lets go.
                    Pad = ParsePad(s.mArg);
                    break;
                case "size":
                {
                    // size:F:w,h - resize the backbuffer (e.g. to check letterboxing).
                    string[] wh = s.mArg.Split(',');
                    Main.graphics.PreferredBackBufferWidth = int.Parse(wh[0]);
                    Main.graphics.PreferredBackBufferHeight = int.Parse(wh[1]);
                    Main.graphics.GraphicsDeviceManager.ApplyChanges();
                    theApp.mScreenScales.Init(int.Parse(wh[0]), int.Parse(wh[1]), Constants.ScreenWidth, Constants.ScreenHeight);
                    Graphics.Resized();
                    break;
                }
                case "dump":
                    Dump(theApp.mWidgetManager);
                    break;
                case "blur":
                    // blur:F: / focus:F: - the window losing and regaining focus (Main.OnDeactivated/OnActivated).
                    theApp.LostFocus();
                    theApp.AppEnteredBackground();
                    break;
                case "focus":
                    theApp.GotFocus();
                    break;
                case "quit":
                    return false;
                }
            }
            return true;
        }

        private static GamePadState ParsePad(string theArg)
        {
            string[] f = theArg.Split(',');
            Buttons aButtons = 0;
            bool lt = false, rt = false, up = false, down = false, left = false, right = false;
            float aScroll = 0f;
            for (int i = 2; i < f.Length; i++)
            {
                switch (f[i])
                {
                case "a": aButtons |= Buttons.A; break;
                case "b": aButtons |= Buttons.B; break;
                case "x": aButtons |= Buttons.X; break;
                case "y": aButtons |= Buttons.Y; break;
                case "lb": aButtons |= Buttons.LeftShoulder; break;
                case "rb": aButtons |= Buttons.RightShoulder; break;
                case "start": aButtons |= Buttons.Start; break;
                case "back": aButtons |= Buttons.Back; break;
                case "lt": lt = true; break;
                case "rt": rt = true; break;
                case "up": up = true; break;
                case "down": down = true; break;
                case "left": left = true; break;
                case "right": right = true; break;
                case "rup": aScroll = 1f; break;
                case "rdown": aScroll = -1f; break;
                }
            }
            ButtonState Held(bool theHeld) => theHeld ? ButtonState.Pressed : ButtonState.Released;
            return new GamePadState(
                new GamePadThumbSticks(new Vector2(float.Parse(f[0]), float.Parse(f[1])), new Vector2(0f, aScroll)),
                new GamePadTriggers(lt ? 1f : 0f, rt ? 1f : 0f),
                new GamePadButtons(aButtons),
                new GamePadDPad(Held(up), Held(down), Held(left), Held(right)));
        }

        /// <summary>Prints the widget tree and the manager's modal/focus/capture state.</summary>
        private static void Dump(WidgetManager theManager)
        {
            Console.WriteLine($"[dump] frame {Frame} modal={Name(theManager.mBaseModalWidget)} focus={Name(theManager.mFocusWidget)} over={Name(theManager.mOverWidget)} lastDown={Name(theManager.mLastDownWidget)} downButtons={theManager.mDownButtons} flags={theManager.mWidgetFlags:x} belowModal+={theManager.mBelowModalFlagsMod.mAddFlags:x} -={theManager.mBelowModalFlagsMod.mRemoveFlags:x}");
            foreach (Widget w in theManager.mWidgets)
            {
                DumpWidget(w, 1);
            }
        }

        private static void DumpWidget(Widget theWidget, int theDepth)
        {
            string aLabel = theWidget is ButtonWidget aButton && !string.IsNullOrEmpty(aButton.mLabel) ? $" \"{aButton.mLabel}\" id={aButton.mId}" : "";
            Console.WriteLine($"{new string(' ', theDepth * 2)}{Name(theWidget)}{aLabel} @{theWidget.mX},{theWidget.mY} {theWidget.mWidth}x{theWidget.mHeight} vis={theWidget.mVisible} mouse={theWidget.mMouseVisible} dis={theWidget.mDisabled} flags={theWidget.mWidgetFlagsMod.mAddFlags:x}/{theWidget.mWidgetFlagsMod.mRemoveFlags:x}");
            foreach (Widget w in theWidget.mWidgets)
            {
                DumpWidget(w, theDepth + 1);
            }
        }

        private static string Name(Widget theWidget)
        {
            return theWidget == null ? "null" : theWidget.GetType().Name;
        }

        /// <summary>Called after a frame is drawn: saves a pending screenshot of the back buffer.</summary>
        public static void AfterDraw(GraphicsDevice theDevice)
        {
            if (sPendingShot == null)
            {
                return;
            }
            int w = theDevice.PresentationParameters.BackBufferWidth;
            int h = theDevice.PresentationParameters.BackBufferHeight;
            Microsoft.Xna.Framework.Color[] data = new Microsoft.Xna.Framework.Color[w * h];
            theDevice.GetBackBufferData(data);
            using (Texture2D tex = new Texture2D(theDevice, w, h))
            {
                tex.SetData(data);
                using (Stream s = File.Create(sPendingShot))
                {
                    tex.SaveAsPng(s, w, h);
                }
            }
            sPendingShot = null;
        }
    }
}
