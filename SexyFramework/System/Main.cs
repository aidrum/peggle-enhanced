using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Sexy
{
    /// <summary>
    /// MonoGame host for a SexyApp game. Adapted from PlantsVsZombies.NET's Lawn_PCGL Main:
    /// the WP7 GamerServices, IronPython modding and JSON config handling are removed, and the
    /// concrete app is supplied by the game through <see cref="AppFactory"/>.
    /// </summary>
    public class Main : Game
    {
        public static Graphics graphics;

        public static Func<Main, SexyAppBase> AppFactory;

        /// <summary>Full-screen platforms (Android, iOS): the screen's size in pixels, for the backbuffer.</summary>
        public static Point? NativeScreenSize;

        /// <summary>
        /// Port: phones and tablets (Android, iOS). Full screen, touch only, lazy loading and no PC Controls;
        /// desktops get a resizable window with mouse, keyboard and text input.
        /// </summary>
        public static bool IsMobile => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>Set by the platform (Android's back button); handled as a back press on the next update.</summary>
        public static volatile bool BackRequested;

        /// <summary>
        /// Set by a platform host to close the app itself. On Android, Game.Exit only moves the task to
        /// the back, so the still-running game kept exiting every time it was reopened.
        /// </summary>
        public static Action ExitHandler;

        /// <summary>
        /// Set by a platform host that opens web links itself (Android starts a view intent). Without
        /// one, SexyAppBase.OpenURL hands the link to the desktop's default browser.
        /// </summary>
        public static Action<string> OpenUrlHandler;

        /// <summary>
        /// Port addition (Android): switches the launcher icon to the alias named first (one of the second's,
        /// the app's icon aliases, which are all others disabled). Null where the platform can't. Android ends
        /// the app's task when its launcher alias changes, so the game quits (saving) to apply it.
        /// </summary>
        public static Action<string, string[]> SetAppIconHandler;

        /// <summary>Port addition (Android): which of the given icon aliases the launcher shows.</summary>
        public static Func<string[], string> GetAppIconHandler;

        public static bool LOW_MEMORY_DEVICE { get; private set; }

        public static bool DO_LOW_MEMORY_OPTIONS { get; private set; }

        public static bool IsInTrialMode => false;

        private static UI_ORIENTATION orientationUsed;

        private static bool newOrientation;

        private static bool wantToSuppressDraw;

        private MouseState previousMouseState;

        // UIKit-style tap counting (UITouch.tapCount): the game toggles its zoom on a double tap.
        private const double MULTI_TAP_SECONDS = 0.35;

        private const float MULTI_TAP_DISTANCE = 40f;

        private int mTapCount;

        private double mLastTapTime = double.NegativeInfinity;

        private CGPoint mLastTapPos;

        private int mMouseClickCount = 1;

        private KeyboardState previousKeyboardState;

        private GamePadState previousGamepadState;

        public string applicationStoragePath;

        public Main()
        {
            Main.graphics = Graphics.GetNew(this);
            Main.graphics.IsFullScreen = IsMobile;
#if PEGGLE_IPHONE
            // A full screen (Android, iOS) lays out at its own aspect, as the original did on the device; a
            // resizable window switches between the 960 and 1136 layouts (see ChooseScreenWidth).
            if (NativeScreenSize is Point aScreen)
            {
                Constants.ChooseScreenWidth(aScreen.X, aScreen.Y);
            }
            else
            {
                Constants.ChooseScreenWidth();
            }
#endif
            // A backbuffer of the game's size gets letterboxed by MonoGame Android through the
            // viewport alone, which is lost whenever a render target is unset; there the backbuffer
            // takes the whole screen and ScreenScales letterboxes, as it does in a resized window.
            Main.graphics.PreferredBackBufferWidth = NativeScreenSize?.X ?? Constants.DefaultWindowWidth;
            Main.graphics.PreferredBackBufferHeight = NativeScreenSize?.Y ?? Constants.ScreenHeight;
            GraphicsState.mGraphicsDeviceManager.DeviceCreated += graphics_DeviceCreated;
            // The game renders into offscreen images mid-frame (e.g. floating texts build their
            // image lazily in Draw). With the default DiscardContents, switching back to the
            // backbuffer clears everything drawn so far, which flashes black for a frame.
            GraphicsState.mGraphicsDeviceManager.PreparingDeviceSettings += (sender, e) =>
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            base.TargetElapsedTime = TimeSpan.FromSeconds(0.01);
            base.Exiting += Main_Exiting;
            Window.AllowUserResizing = true;
            base.Window.ClientSizeChanged += OnResize;
            IsMouseVisible = true;
            if (!IsMobile)
            {
                HookTextInput();
            }
            DebugScript.Init();
        }

        /// <summary>
        /// Kept out of the constructor: GameWindow.TextInput only exists in the desktop MonoGame
        /// builds, so referencing it inline would fail when the constructor is compiled on Android/iOS.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HookTextInput()
        {
            Window.TextInput += OnTextInput;
        }

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            // Backspace/Enter arrive as KeyDown; only printable characters are typed text.
            if (e.Character >= ' ' && GlobalStaticVars.gSexyAppBase?.mWidgetManager != null)
            {
                GlobalStaticVars.gSexyAppBase.mWidgetManager.KeyChar(new SexyChar(e.Character));
            }
        }

        public void OnResize(object sender, EventArgs e)
        {
            Rectangle bounds = Window.ClientBounds;
            if (GlobalStaticVars.gSexyAppBase != null && bounds.Width > 0 && bounds.Height > 0)
            {
                MatchBackBufferToWindow(bounds);
                GlobalStaticVars.gSexyAppBase.mScreenScales.Init(bounds.Width, bounds.Height, Constants.ScreenWidth, Constants.ScreenHeight);
                Graphics.Resized();
            }
        }

        /// <summary>
        /// On phones and tablets the backbuffer must be exactly the surface size. Otherwise MonoGame letterboxes it
        /// through the viewport, which is lost whenever a render target is unset, so the picture shrank
        /// and later jumped (black bands at the top when the surface was smaller than the display).
        /// </summary>
        private void MatchBackBufferToWindow(Rectangle theBounds)
        {
            if (!IsMobile || theBounds.Width <= 0 || theBounds.Height <= 0)
            {
                return;
            }
            PresentationParameters pp = base.GraphicsDevice.PresentationParameters;
            if (pp.BackBufferWidth != theBounds.Width || pp.BackBufferHeight != theBounds.Height)
            {
                Main.graphics.PreferredBackBufferWidth = theBounds.Width;
                Main.graphics.PreferredBackBufferHeight = theBounds.Height;
                Main.graphics.GraphicsDeviceManager.ApplyChanges();
            }
            base.GraphicsDevice.Viewport = new Viewport(0, 0, theBounds.Width, theBounds.Height);
        }

        private Point mWindowedSize;

        /// <summary>
        /// Switches between a window and borderless full screen at the desktop resolution (no mode change),
        /// restoring the window size on the way back.
        /// </summary>
        public void ToggleFullscreen()
        {
            GraphicsDeviceManager aManager = Main.graphics.GraphicsDeviceManager;
            if (!aManager.IsFullScreen)
            {
                mWindowedSize = new Point(Window.ClientBounds.Width, Window.ClientBounds.Height);
                DisplayMode aMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                aManager.HardwareModeSwitch = false;
                aManager.PreferredBackBufferWidth = aMode.Width;
                aManager.PreferredBackBufferHeight = aMode.Height;
                aManager.IsFullScreen = true;
            }
            else
            {
                aManager.IsFullScreen = false;
                if (mWindowedSize.X > 0 && mWindowedSize.Y > 0)
                {
                    aManager.PreferredBackBufferWidth = mWindowedSize.X;
                    aManager.PreferredBackBufferHeight = mWindowedSize.Y;
                }
            }
            aManager.ApplyChanges();
            OnResize(null, null);
        }

        private void graphics_DeviceCreated(object sender, EventArgs e)
        {
            base.GraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
        }

        private bool mExiting;

        private void ExitGame()
        {
            if (ExitHandler == null)
            {
                base.Exit();
                return;
            }
            if (mExiting)
            {
                return;
            }
            mExiting = true;
            // Save and shut down as the Exiting event would, then let the host close the app.
            GlobalStaticVars.gSexyAppBase?.AppExit();
            ExitHandler();
        }

        private void Main_Exiting(object sender, ExitingEventArgs e)
        {
            GlobalStaticVars.gSexyAppBase?.AppExit();
        }

        internal string FetchApplicationStoragePath()
        {
            return applicationStoragePath ?? AppContext.BaseDirectory;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            GraphicsState.Init();
            GlobalStaticVars.initialize(this);
            MatchBackBufferToWindow(Window.ClientBounds);
            GlobalStaticVars.gSexyAppBase.mScreenScales.Init(Window.ClientBounds.Width, Window.ClientBounds.Height, Constants.ScreenWidth, Constants.ScreenHeight);
            Graphics.Resized();
            GlobalStaticVars.gSexyAppBase.StartLoadingThread();
        }


        protected override void Update(GameTime gameTime)
        {
            if (!base.IsActive && !DebugScript.Active)
            {
                return;
            }
            if (DebugScript.ShotPending)
            {
                base.Update(gameTime);
                return;
            }
            if (!DebugScript.Update(GlobalStaticVars.gSexyAppBase))
            {
                ExitGame();
                return;
            }
            if (GlobalStaticVars.gSexyAppBase.WantsToExit)
            {
                ExitGame();
                return;
            }
            // A scripted run drives all input itself; the real mouse would otherwise click into it.
            if (!DebugScript.Active)
            {
                HandleInput(gameTime);
            }
            else if (DebugScript.Pad is GamePadState aPad)
            {
                HandleGamePad(GlobalStaticVars.gSexyAppBase, aPad, gameTime);
            }
            long aStart = System.Diagnostics.Stopwatch.GetTimestamp();
            GlobalStaticVars.gSexyAppBase.UpdateApp();
            mPerfUpdateTicks += System.Diagnostics.Stopwatch.GetTimestamp() - aStart;
            base.Update(gameTime);
        }

        public static void SuppressNextDraw()
        {
            Main.wantToSuppressDraw = true;
        }

        public static void NeedToSetUpOrientationMatrix(UI_ORIENTATION orientation)
        {
            Main.orientationUsed = orientation;
            Main.newOrientation = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            Main.newOrientation = false;
            if (Main.wantToSuppressDraw)
            {
                Main.wantToSuppressDraw = false;
                return;
            }
            lock (ResourceManager.DrawLocker)
            {
                base.GraphicsDevice.Clear(Color.Black);
                long aStart = System.Diagnostics.Stopwatch.GetTimestamp();
                GlobalStaticVars.gSexyAppBase.DrawGame(gameTime);
                base.Draw(gameTime);
                if (gPerf)
                {
                    PerfFrame(System.Diagnostics.Stopwatch.GetTimestamp() - aStart);
                }
                DebugScript.AfterDraw(base.GraphicsDevice);
            }
        }

        public void HandleInput(GameTime gameTime)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            ScreenScales s = app.mScreenScales;
            // Mobile input is touch only (below); polling the mouse there too would double taps.
            MouseState msstate = IsMobile ? previousMouseState : Mouse.GetState();
            CGPoint mouse = s.InvMapTouch(new CGPoint(msstate.Position.X, msstate.Position.Y));
            int mx = (int)mouse.x;
            int my = (int)mouse.y;
            if (previousMouseState.Position != msstate.Position)
            {
                GamePadCursor.Hide(app);
                app.mWidgetManager.MouseMove(mx, my);
            }
            if (msstate.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                // Click count 3 means the middle button to WidgetManager, so mouse clicks cycle 1,2,1,2...
                mMouseClickCount = CountTap(mouse, gameTime.TotalGameTime.TotalSeconds);
                if (mMouseClickCount > 2)
                {
                    mMouseClickCount = mTapCount = 1;
                }
                app.mWidgetManager.MouseDown(mx, my, mMouseClickCount);
            }
            else if (msstate.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed)
            {
                app.mWidgetManager.MouseUp(mx, my, mMouseClickCount);
            }
            if (msstate.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                app.mWidgetManager.MouseDown(mx, my, -1);
            }
            else if (msstate.RightButton == ButtonState.Released && previousMouseState.RightButton == ButtonState.Pressed)
            {
                app.mWidgetManager.MouseUp(mx, my, -1);
            }
            // Click count 3 is the middle button to WidgetManager (Widget.MouseDown passes it as button 2).
            if (msstate.MiddleButton == ButtonState.Pressed && previousMouseState.MiddleButton == ButtonState.Released)
            {
                app.mWidgetManager.MouseDown(mx, my, 3);
            }
            else if (msstate.MiddleButton == ButtonState.Released && previousMouseState.MiddleButton == ButtonState.Pressed)
            {
                app.mWidgetManager.MouseUp(mx, my, 3);
            }
            if (msstate.ScrollWheelValue != previousMouseState.ScrollWheelValue)
            {
                app.mWidgetManager.MouseWheel(msstate.ScrollWheelValue - previousMouseState.ScrollWheelValue);
            }
            GamePadState pad = GamePad.GetState(PlayerIndex.One);
            KeyboardState keys = Keyboard.GetState();
            if ((keys.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape)) || BackRequested)
            {
                BackRequested = false;
                app.BackButtonPress();
            }
            HandleGamePad(app, pad, gameTime);
            bool began = false;
            foreach (TouchLocation touchLocation in TouchPanel.GetState())
            {
                _Touch touch = default(_Touch);
                touch.location.mX = s.InvMapTouchX(touchLocation.Position.X);
                touch.location.mY = s.InvMapTouchY(touchLocation.Position.Y);
                if (touchLocation.TryGetPreviousLocation(out TouchLocation prev))
                {
                    touch.previousLocation = s.InvMapTouch(new CGPoint(prev.Position.X, prev.Position.Y));
                }
                else
                {
                    touch.previousLocation = touch.location;
                }
                touch.timestamp = gameTime.TotalGameTime.TotalSeconds;
                if (touchLocation.State == TouchLocationState.Pressed && !began)
                {
                    mTouchTapCount = CountTap(touch.location, touch.timestamp);
                }
                touch.tapCount = mTouchTapCount;
                if (touchLocation.State == TouchLocationState.Pressed && !began)
                {
                    GamePadCursor.Hide(app);
                    app.TouchBegan(touch);
                    began = true;
                }
                else if (touchLocation.State == TouchLocationState.Moved)
                {
                    app.TouchMoved(touch);
                }
                else if (touchLocation.State == TouchLocationState.Released)
                {
                    app.TouchEnded(touch);
                }
                else if (touchLocation.State == TouchLocationState.Invalid)
                {
                    app.TouchesCanceled();
                }
            }
            // Port addition: Alt+Enter toggles full screen on desktops; the Enter press isn't passed on.
            bool aAltEnter = !IsMobile && (keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt)) &&
                keys.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter);
            if (aAltEnter)
            {
                ToggleFullscreen();
            }
            foreach (Keys k in keys.GetPressedKeys())
            {
                if (previousKeyboardState.IsKeyUp(k) && !(aAltEnter && k == Keys.Enter))
                {
                    app.mWidgetManager.KeyDown((KeyCode)k);
                }
            }
            foreach (Keys k in previousKeyboardState.GetPressedKeys())
            {
                if (keys.IsKeyUp(k))
                {
                    app.mWidgetManager.KeyUp((KeyCode)k);
                }
            }
            previousMouseState = msstate;
            previousKeyboardState = keys;
        }

        private int mTouchTapCount = 1;

        /// <summary>
        /// Port addition (controller): Back and B go back (Android's back button); then the game takes the pad if it
        /// wants it (the board), or else it moves the pointer (GamePadCursor).
        /// </summary>
        private void HandleGamePad(SexyAppBase theApp, GamePadState thePad, GameTime theTime)
        {
            if ((thePad.Buttons.Back == ButtonState.Pressed && previousGamepadState.Buttons.Back == ButtonState.Released) ||
                (thePad.Buttons.B == ButtonState.Pressed && previousGamepadState.Buttons.B == ButtonState.Released))
            {
                theApp.BackButtonPress();
            }
            if (theApp.GamePadUpdate(thePad, previousGamepadState))
            {
                GamePadCursor.Hide(theApp);
            }
            else
            {
                GamePadCursor.Update(theApp, thePad, previousGamepadState, theTime.ElapsedGameTime.TotalSeconds, theTime.TotalGameTime.TotalSeconds);
            }
            previousGamepadState = thePad;
        }

        // SEXY_PERF=1 (port debug aid): logs average update/draw CPU time and GPU draw calls once a second.
        private static readonly bool gPerf = Environment.GetEnvironmentVariable("SEXY_PERF") == "1";

        private long mPerfUpdateTicks;

        private long mPerfDrawTicks;

        private long mPerfDrawCalls;

        private long mPerfPrimitives;

        private long mPerfMaxDrawTicks;

        private int mPerfFrames;

        private int mPerfGCs;

        private void PerfFrame(long theDrawTicks)
        {
            GraphicsMetrics aMetrics = base.GraphicsDevice.Metrics;
            mPerfDrawTicks += theDrawTicks;
            mPerfMaxDrawTicks = Math.Max(mPerfMaxDrawTicks, theDrawTicks);
            mPerfDrawCalls += aMetrics.DrawCount;
            mPerfPrimitives += aMetrics.PrimitiveCount;
            if (++mPerfFrames < 60)
            {
                return;
            }
            double aMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency / mPerfFrames;
            int aGCs = GC.CollectionCount(0);
            Console.WriteLine($"[perf] update {mPerfUpdateTicks * aMs:F2}ms draw {mPerfDrawTicks * aMs:F2}ms (max {mPerfMaxDrawTicks * aMs * mPerfFrames:F2}) " +
                $"calls {mPerfDrawCalls / mPerfFrames} prims {mPerfPrimitives / mPerfFrames} gc0 {aGCs - mPerfGCs} alloc {GC.GetTotalAllocatedBytes() / 1024}K f{DebugScript.Frame}");
            mPerfGCs = aGCs;
            mPerfUpdateTicks = mPerfDrawTicks = mPerfDrawCalls = mPerfPrimitives = mPerfMaxDrawTicks = 0;
            mPerfFrames = 0;
        }

        private int CountTap(CGPoint thePos, double theTime)
        {
            float dx = thePos.X - mLastTapPos.X;
            float dy = thePos.Y - mLastTapPos.Y;
            if (theTime - mLastTapTime <= MULTI_TAP_SECONDS && dx * dx + dy * dy <= MULTI_TAP_DISTANCE * MULTI_TAP_DISTANCE)
            {
                mTapCount++;
            }
            else
            {
                mTapCount = 1;
            }
            mLastTapTime = theTime;
            mLastTapPos = thePos;
            return mTapCount;
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            // Scripted runs ignore real focus changes (their blur/focus steps simulate them).
            if (app != null && !DebugScript.Active)
            {
                app.GotFocus();
                if (!app.mMusicInterface.isStopped)
                {
                    app.mMusicInterface.ResumeMusic();
                }
            }
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            // Scripted runs ignore real focus changes (their blur/focus steps simulate them).
            if (app != null && !DebugScript.Active)
            {
                app.LostFocus();
                if (!app.mMusicInterface.isStopped)
                {
                    app.mMusicInterface.PauseMusic();
                }
                app.AppEnteredBackground();
            }
            base.OnDeactivated(sender, args);
        }
    }
}
