using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Sexy
{
    /// <summary>Port addition (controller/keyboard): what a menu input asks for.</summary>
    public enum NavAction
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Confirm,
        Back,
        Start,
        PageLeft,
        PageRight
    }

    /// <summary>
    /// Port addition (controller/keyboard): one thing the menu selection can be on: a widget (a button, a
    /// checkbox, a slider), or one cell of a widget made of several (a thumbnail grid, a list; INavCells).
    /// </summary>
    public readonly struct NavTarget : IEquatable<NavTarget>
    {
        public readonly Widget mWidget;

        /// <summary>The cell of an INavCells widget, -1 for the whole widget.</summary>
        public readonly int mCell;

        public NavTarget(Widget theWidget, int theCell = -1)
        {
            mWidget = theWidget;
            mCell = theCell;
        }

        public static readonly NavTarget None = default;

        public bool IsNone => mWidget == null;

        public bool Equals(NavTarget other) => mWidget == other.mWidget && mCell == other.mCell;

        public override bool Equals(object obj) => obj is NavTarget t && Equals(t);

        public override int GetHashCode() => HashCode.Combine(mWidget, mCell);

        public static bool operator ==(NavTarget a, NavTarget b) => a.Equals(b);

        public static bool operator !=(NavTarget a, NavTarget b) => !a.Equals(b);
    }

    /// <summary>
    /// Port addition (controller/keyboard): a widget whose parts are picked one by one (thumbnail grids, lists).
    /// Each cell is a selection target; a cell inside a ScrollWidget is scrolled into view when selected.
    /// </summary>
    public interface INavCells
    {
        int GetNavCellCount();

        /// <summary>A cell's rectangle in the widget's coordinates; an empty one for a cell that can't be picked.</summary>
        TRect GetNavCellRect(int theCell);

        /// <summary>A/Enter on the cell (normally what a click on it does).</summary>
        void NavCellActivate(int theCell);
    }

    /// <summary>
    /// Port addition (controller/keyboard): a screen or dialog that steers the selection itself. It gets each action
    /// first while the selection is inside it (or there is none); returning true means it handled it.
    /// </summary>
    public interface INavHandler
    {
        /// <summary>The selection when the screen comes up, or None for the first button.</summary>
        NavTarget NavDefault() => NavTarget.None;

        bool NavAction(NavAction theAction) => false;

        /// <summary>The selection moved (MenuNav.Selected).</summary>
        void NavSelectionChanged()
        {
        }
    }

    /// <summary>
    /// Port addition (controller/keyboard): menu selection for the pad and the keyboard. The D-pad or left stick (arrows
    /// or WASD) moves a highlight between the buttons of the screen or dialog on top; A (Space, Z or Enter) presses
    /// what's selected, B and Back (Escape) go back (SexyAppBase.BackButtonPress, which asks MenuNav.Back first), LB/RB
    /// (Q/E) turn pages and Start does the screen's Start action (else Enter, for text prompts). Selecting works
    /// spatially unless the screen steers it (INavHandler). The mouse or a touch hides the highlight until the next pad
    /// or key input, and the first such input only brings it back (except A when there is a single button to press).
    /// The game says when the menus have the input (SexyAppBase.WantsMenuNav); during play the board takes the pad.
    /// </summary>
    public static class MenuNav
    {
        private const double REPEAT_DELAY = 0.4;

        private const double REPEAT_INTERVAL = 0.12;

        private const float STICK_DEADZONE = 0.5f;

        /// <summary>How much more the stick has to lean the other way to switch from up/down to left/right, or back.</summary>
        private const float STICK_AXIS_SWITCH = 1.5f;

        /// <summary>True from a pad or key input until the mouse or a finger is used.</summary>
        public static bool Active { get; private set; }

        private static NavTarget sSelected;

        /// <summary>Earlier selections, most recent last: when a dialog closes, the button that opened it is selected again.</summary>
        private static readonly List<NavTarget> sHistory = new List<NavTarget>();

        private static List<NavTarget> sTargets = new List<NavTarget>();

        private static NavAction sPadDir;

        private static NavAction sKeyDir;

        private static NavAction sHeldDir;

        private static double sRepeatTime;

        private static double sTime;

        private static bool sShowing;

        public static NavTarget Selected => sSelected;

        /// <summary>True while theWidget is highlighted (it may draw its own highlight; Widget.DrawsNavHighlight).</summary>
        public static bool IsHighlighted(Widget theWidget)
        {
            return sShowing && sSelected.mWidget == theWidget && theWidget != null;
        }

        /// <summary>0..1, the highlight's pulse.</summary>
        public static float Pulse => (float)(0.5 + 0.5 * Math.Sin(sTime * Math.PI * 2.0));

        /// <summary>The mouse moved or clicked, or a finger touched: the highlight goes until the next pad or key input.</summary>
        public static void Cancel()
        {
            Active = false;
            sShowing = false;
        }

        /// <summary>Selects theTarget (a screen's own steering), scrolling a cell into view.</summary>
        public static void Select(NavTarget theTarget)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            if (theTarget.IsNone || theTarget == sSelected)
            {
                return;
            }
            sSelected = theTarget;
            if (theTarget.mCell >= 0 && theTarget.mWidget.mParent is ScrollWidget aScroll && theTarget.mWidget is INavCells aCells)
            {
                TRect aRect = aCells.GetNavCellRect(theTarget.mCell);
                aScroll.ScrollRectIntoView(aRect, true);
            }
            GetHandler(app)?.NavSelectionChanged();
        }

        public static void Select(Widget theWidget)
        {
            Select(new NavTarget(theWidget));
        }

        /// <summary>True when theTarget can be selected now (it is on the screen on top).</summary>
        public static bool IsTarget(NavTarget theTarget)
        {
            return !theTarget.IsNone && sTargets.Contains(theTarget);
        }

        public static bool IsTarget(Widget theWidget)
        {
            return IsTarget(new NavTarget(theWidget));
        }

        /// <summary>The targets inside theContainer (all of them for null), as of this update.</summary>
        public static List<NavTarget> GetTargets(Widget theContainer = null)
        {
            List<NavTarget> aList = new List<NavTarget>();
            foreach (NavTarget aTarget in sTargets)
            {
                if (theContainer == null || BelongsTo(aTarget.mWidget, theContainer))
                {
                    aList.Add(aTarget);
                }
            }
            return aList;
        }

        // ---------------------------------------------------------------- input

        /// <summary>The pad, while the menus have it (the game didn't take it this update).</summary>
        public static void UpdatePad(SexyAppBase theApp, GamePadState thePad, GamePadState thePrevPad)
        {
            if (!thePad.IsConnected)
            {
                sPadDir = NavAction.None;
                return;
            }
            sPadDir = GetPadDir(thePad);
            if (Pressed(thePad.Buttons.A, thePrevPad.Buttons.A))
            {
                Do(theApp, NavAction.Confirm);
            }
            if (Pressed(thePad.Buttons.Start, thePrevPad.Buttons.Start))
            {
                Do(theApp, NavAction.Start);
            }
            if (Pressed(thePad.Buttons.LeftShoulder, thePrevPad.Buttons.LeftShoulder))
            {
                Do(theApp, NavAction.PageLeft);
            }
            if (Pressed(thePad.Buttons.RightShoulder, thePrevPad.Buttons.RightShoulder))
            {
                Do(theApp, NavAction.PageRight);
            }
            // The right stick scrolls the text of the dialog on top (credits, help, lists).
            float aScroll = thePad.ThumbSticks.Right.Y;
            if (Math.Abs(aScroll) > 0.3f && theApp.WantsMenuNav())
            {
                ScrollWidget aScrollWidget = FindScrollWidget(GetTopDialog(theApp));
                if (aScrollWidget != null)
                {
                    int aDelta = (int)(-aScroll * Math.Abs(aScroll) * theApp.mHeight / 40);
                    CGPoint anOffset = aScrollWidget.GetScrollOffset();
                    aScrollWidget.ScrollRectIntoView(new TRect((int)-anOffset.x, (int)-anOffset.y + aDelta, aScrollWidget.mWidth, aScrollWidget.mHeight), false);
                }
            }
        }

        private static ScrollWidget FindScrollWidget(WidgetContainer theContainer)
        {
            if (theContainer == null)
            {
                return null;
            }
            foreach (Widget aWidget in theContainer.mWidgets)
            {
                if (!aWidget.mVisible)
                {
                    continue;
                }
                if (aWidget is ScrollWidget aScroll)
                {
                    return aScroll;
                }
                ScrollWidget aFound = FindScrollWidget(aWidget);
                if (aFound != null)
                {
                    return aFound;
                }
            }
            return null;
        }

        /// <summary>The game has the pad this update: just note the direction held, so it doesn't move the menu later.</summary>
        public static void PadTaken(GamePadState thePad)
        {
            sPadDir = thePad.IsConnected ? GetPadDir(thePad) : NavAction.None;
        }

        /// <summary>True when the keyboard drives the menus (not while text is being typed).</summary>
        public static bool WantsKeyboard(SexyAppBase theApp)
        {
            return theApp.WantsMenuNav() && !(theApp.mWidgetManager.mFocusWidget is EditWidget);
        }

        /// <summary>The keys held this update (arrows/WASD move, with repeat, in Update).</summary>
        public static void UpdateKeys(SexyAppBase theApp, KeyboardState theKeys)
        {
            sKeyDir = NavAction.None;
            if (!WantsKeyboard(theApp))
            {
                return;
            }
            if (theKeys.IsKeyDown(Keys.Up) || theKeys.IsKeyDown(Keys.W))
            {
                sKeyDir = NavAction.Up;
            }
            else if (theKeys.IsKeyDown(Keys.Down) || theKeys.IsKeyDown(Keys.S))
            {
                sKeyDir = NavAction.Down;
            }
            else if (theKeys.IsKeyDown(Keys.Left) || theKeys.IsKeyDown(Keys.A))
            {
                sKeyDir = NavAction.Left;
            }
            else if (theKeys.IsKeyDown(Keys.Right) || theKeys.IsKeyDown(Keys.D))
            {
                sKeyDir = NavAction.Right;
            }
        }

        /// <summary>A key went down; true when it is a menu key, which isn't passed on to the widgets.</summary>
        public static bool KeyDown(SexyAppBase theApp, Keys theKey)
        {
            if (!WantsKeyboard(theApp))
            {
                return false;
            }
            switch (theKey)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.W:
                case Keys.A:
                case Keys.S:
                case Keys.D:
                    return true;
                case Keys.Space:
                case Keys.Z:
                case Keys.Enter:
                    Do(theApp, NavAction.Confirm);
                    return true;
                case Keys.Q:
                    Do(theApp, NavAction.PageLeft);
                    return true;
                case Keys.E:
                    Do(theApp, NavAction.PageRight);
                    return true;
            }
            return false;
        }

        /// <summary>Once an update, after the input: keeps the selection on the screen on top and repeats held directions.</summary>
        public static void Update(SexyAppBase theApp, double theSeconds)
        {
            sTime += theSeconds;
            NavAction aDir = sPadDir != NavAction.None ? sPadDir : sKeyDir;
            if (!theApp.WantsMenuNav())
            {
                sShowing = false;
                sHeldDir = aDir;
                return;
            }
            if (Active)
            {
                Validate(theApp);
            }
            sShowing = Active && !sSelected.IsNone;
            // The stick slides a selected slider smoothly (the D-pad and keys still step it a tenth).
            if (sShowing && sStickX != 0 && sPadDir == aDir && (aDir == NavAction.Left || aDir == NavAction.Right) &&
                sSelected.mCell < 0 && sSelected.mWidget is Slider aSlider)
            {
                if (sSliding != aSlider)
                {
                    EndSlide();
                    sSliding = aSlider;
                }
                // Dragging, as with the mouse: the volume test sound waits for the release.
                aSlider.mDragging = true;
                double aVal = Math.Clamp(aSlider.mVal + sStickX * Math.Abs(sStickX) * SLIDE_SPEED * theSeconds, 0.0, 1.0);
                if (aVal != aSlider.mVal)
                {
                    aSlider.SetValue(aVal);
                    aSlider.mListener.SliderVal(aSlider.mId, aSlider.mVal);
                }
                sHeldDir = aDir;
                return;
            }
            EndSlide();
            if (aDir != sHeldDir)
            {
                sHeldDir = aDir;
                if (aDir != NavAction.None)
                {
                    sRepeatTime = REPEAT_DELAY;
                    Do(theApp, aDir);
                }
            }
            else if (aDir != NavAction.None)
            {
                sRepeatTime -= theSeconds;
                if (sRepeatTime <= 0)
                {
                    sRepeatTime += REPEAT_INTERVAL;
                    Do(theApp, aDir);
                }
            }
        }

        /// <summary>The stick let go of the slider: the drag ends and the value is reported once more (the volume test sound).</summary>
        private static void EndSlide()
        {
            if (sSliding == null)
            {
                return;
            }
            Slider aSlider = sSliding;
            sSliding = null;
            aSlider.mDragging = false;
            if (aSlider.mWidgetManager != null)
            {
                aSlider.mListener.SliderVal(aSlider.mId, aSlider.mVal);
            }
        }

        /// <summary>
        /// B, Back or Escape (from SexyAppBase.BackButtonPress): the screen's own back action, else the top dialog's
        /// Cancel/No button or its footer button, else its only button. False leaves it to the game.
        /// </summary>
        public static bool Back(SexyAppBase theApp)
        {
            if (!theApp.WantsMenuNav())
            {
                return false;
            }
            Validate(theApp);
            INavHandler aHandler = GetHandler(theApp);
            if (aHandler != null && aHandler.NavAction(NavAction.Back))
            {
                return true;
            }
            Dialog aDialog = GetTopDialog(theApp);
            if (aDialog == null)
            {
                return false;
            }
            if ((aDialog.mButtonMode == 1 || aDialog.mButtonMode == 2) && aDialog.mNoButton != null && IsTarget(aDialog.mNoButton))
            {
                Activate(theApp, new NavTarget(aDialog.mNoButton));
                return true;
            }
            // A footer button (Back, Cancel, OK, Main Menu) is the dialog's way out.
            if (aDialog.mButtonMode == 3 && aDialog.mYesButton != null && IsTarget(aDialog.mYesButton))
            {
                Activate(theApp, new NavTarget(aDialog.mYesButton));
                return true;
            }
            List<NavTarget> aTargets = GetTargets(aDialog);
            if (aTargets.Count == 1 && aTargets[0].mCell < 0 && aTargets[0].mWidget is ButtonWidget)
            {
                Activate(theApp, aTargets[0]);
                return true;
            }
            return false;
        }

        private static void Do(SexyAppBase theApp, NavAction theAction)
        {
            if (!theApp.WantsMenuNav())
            {
                return;
            }
            bool wasActive = Active;
            Active = true;
            Validate(theApp);
            sShowing = !sSelected.IsNone;
            // The first input after the mouse only brings the highlight back, unless A has a lone button to press.
            if (!wasActive && !sSelected.IsNone && (IsDirection(theAction) || (theAction == NavAction.Confirm && sTargets.Count > 1)))
            {
                return;
            }
            INavHandler aHandler = GetHandler(theApp);
            if (aHandler != null && aHandler.NavAction(theAction))
            {
                return;
            }
            switch (theAction)
            {
                case NavAction.Up:
                case NavAction.Down:
                case NavAction.Left:
                case NavAction.Right:
                    if (sSelected.IsNone)
                    {
                        return;
                    }
                    if (sSelected.mCell < 0 && sSelected.mWidget.NavAdjust(theAction))
                    {
                        return;
                    }
                    Select(FindInDirection(sSelected, theAction, sTargets));
                    break;
                case NavAction.Confirm:
                    if (!sSelected.IsNone)
                    {
                        Activate(theApp, sSelected);
                    }
                    else
                    {
                        // Nothing to select: a tap on the screen (story pages, the Lucky Spin, ...).
                        Click(theApp, theApp.mWidth / 2, theApp.mHeight / 2);
                    }
                    break;
                case NavAction.Start:
                    if (!theApp.NavStart())
                    {
                        // Enter confirms the text prompts, which have no OK button.
                        theApp.mWidgetManager.KeyDown(KeyCode.Return);
                        theApp.mWidgetManager.KeyUp(KeyCode.Return);
                    }
                    break;
            }
        }

        private static bool IsDirection(NavAction theAction)
        {
            return theAction == NavAction.Up || theAction == NavAction.Down || theAction == NavAction.Left || theAction == NavAction.Right;
        }

        private static bool Pressed(ButtonState theNow, ButtonState thePrev)
        {
            return theNow == ButtonState.Pressed && thePrev == ButtonState.Released;
        }

        /// <summary>The left stick's x when it gave the pad's direction, else 0 (the D-pad, or nothing held).</summary>
        private static float sStickX;

        /// <summary>The slider the stick is sliding smoothly, let go (as a mouse drag ends) when the stick is.</summary>
        private static Slider sSliding;

        /// <summary>Slider travel a second with the stick fully over.</summary>
        private const float SLIDE_SPEED = 0.9f;

        private static NavAction GetPadDir(GamePadState thePad)
        {
            sStickX = 0;
            if (thePad.DPad.Up == ButtonState.Pressed)
            {
                return NavAction.Up;
            }
            if (thePad.DPad.Down == ButtonState.Pressed)
            {
                return NavAction.Down;
            }
            if (thePad.DPad.Left == ButtonState.Pressed)
            {
                return NavAction.Left;
            }
            if (thePad.DPad.Right == ButtonState.Pressed)
            {
                return NavAction.Right;
            }
            Vector2 aStick = thePad.ThumbSticks.Left;
            if (aStick.Length() < STICK_DEADZONE)
            {
                return NavAction.None;
            }
            sStickX = aStick.X;
            // Hysteresis: a stick held up/down (or left/right) has to lean clearly the other way to switch, so a push
            // a little off the axis doesn't flick to the other one.
            bool wasVertical = sPadDir == NavAction.Up || sPadDir == NavAction.Down;
            bool wasHorizontal = sPadDir == NavAction.Left || sPadDir == NavAction.Right;
            bool horizontal;
            if (wasVertical)
            {
                horizontal = Math.Abs(aStick.X) > Math.Abs(aStick.Y) * STICK_AXIS_SWITCH;
            }
            else if (wasHorizontal)
            {
                horizontal = Math.Abs(aStick.Y) * STICK_AXIS_SWITCH <= Math.Abs(aStick.X);
            }
            else
            {
                horizontal = Math.Abs(aStick.X) > Math.Abs(aStick.Y);
            }
            if (horizontal)
            {
                return aStick.X > 0 ? NavAction.Right : NavAction.Left;
            }
            return aStick.Y > 0 ? NavAction.Up : NavAction.Down;
        }

        // ---------------------------------------------------------------- pressing

        /// <summary>Presses theTarget: a cell's own action, else a click (a tap on phones) in the middle of the widget.</summary>
        public static void Activate(SexyAppBase theApp, NavTarget theTarget)
        {
            if (theTarget.IsNone)
            {
                return;
            }
            if (theTarget.mCell >= 0 && theTarget.mWidget is INavCells aCells)
            {
                aCells.NavCellActivate(theTarget.mCell);
                return;
            }
            Widget aWidget = theTarget.mWidget;
            if (aWidget.NavActivate())
            {
                return;
            }
            TRect aRect = GetVisibleRect(aWidget);
            Click(theApp, aRect.mX + aRect.mWidth / 2, aRect.mY + aRect.mHeight / 2);
        }

        public static void Activate(Widget theWidget)
        {
            Activate(GlobalStaticVars.gSexyAppBase, new NavTarget(theWidget));
        }

        /// <summary>
        /// Presses a button for a screen's back or Start action: a click when it can be reached, else straight to its
        /// listener (it may be moving). False when it is hidden or disabled.
        /// </summary>
        public static bool Press(ButtonWidget theButton)
        {
            if (theButton == null || !theButton.mVisible || theButton.mDisabled || theButton.mWidgetManager == null)
            {
                return false;
            }
            if (IsTarget(theButton))
            {
                Activate(theButton);
            }
            else
            {
                theButton.mButtonListener?.ButtonDepress(theButton.mId);
            }
            return true;
        }

        /// <summary>A click at (x, y): a touch on phones and tablets, the left button elsewhere, so screens see what they always do.</summary>
        public static void Click(SexyAppBase theApp, int x, int y)
        {
            if (Main.IsMobile)
            {
                _Touch aTouch = default(_Touch);
                aTouch.location = new CGPoint(x, y);
                aTouch.previousLocation = aTouch.location;
                aTouch.tapCount = 1;
                aTouch.timestamp = sTime;
                theApp.TouchBegan(aTouch);
                theApp.TouchEnded(aTouch);
            }
            else
            {
                WidgetManager aManager = theApp.mWidgetManager;
                aManager.MouseDown(x, y, 1);
                aManager.MouseUp(x, y, 1);
                // The pointer isn't really there: no hover look left on the button.
                aManager.MousePosition(WidgetManager.NO_TOUCH_MOUSE_POS.mX, WidgetManager.NO_TOUCH_MOUSE_POS.mY);
            }
        }

        // ---------------------------------------------------------------- targets

        private static void Validate(SexyAppBase theApp)
        {
            sTargets = CollectTargets(theApp);
            if (!sSelected.IsNone && !sTargets.Contains(sSelected))
            {
                Remember(sSelected);
                sSelected = NavTarget.None;
            }
            if (sSelected.IsNone && Active)
            {
                NavTarget aPick = NavTarget.None;
                for (int i = sHistory.Count - 1; i >= 0; i--)
                {
                    if (sTargets.Contains(sHistory[i]))
                    {
                        aPick = sHistory[i];
                        sHistory.RemoveAt(i);
                        break;
                    }
                }
                if (aPick.IsNone)
                {
                    aPick = PickDefault(theApp);
                }
                Select(aPick);
            }
        }

        private static void Remember(NavTarget theTarget)
        {
            sHistory.Remove(theTarget);
            sHistory.Add(theTarget);
            if (sHistory.Count > 16)
            {
                sHistory.RemoveAt(0);
            }
        }

        private static NavTarget PickDefault(SexyAppBase theApp)
        {
            if (sTargets.Count == 0)
            {
                return NavTarget.None;
            }
            // The handler on top, whatever the selection is (there is none now).
            INavHandler aHandler = GetHandler(theApp);
            if (aHandler != null)
            {
                NavTarget aDefault = aHandler.NavDefault();
                if (IsTarget(aDefault))
                {
                    return aDefault;
                }
            }
            // A dialog's Yes/OK button, unless it is the footer (its way out, like Back or Main Menu).
            Dialog aDialog = GetTopDialog(theApp);
            Widget aFooter = null;
            if (aDialog != null && aDialog.mYesButton != null && IsTarget(aDialog.mYesButton))
            {
                if (aDialog.mButtonMode != 3)
                {
                    return new NavTarget(aDialog.mYesButton);
                }
                aFooter = aDialog.mYesButton;
            }
            // Else the first in reading order.
            NavTarget aBest = NavTarget.None;
            TRect aBestRect = default(TRect);
            foreach (NavTarget aTarget in sTargets)
            {
                if (aTarget.mWidget == aFooter && sTargets.Count > 1)
                {
                    continue;
                }
                TRect aRect = GetRect(aTarget);
                if (aBest.IsNone || aRect.mY + 10 < aBestRect.mY || (Math.Abs(aRect.mY - aBestRect.mY) <= 10 && aRect.mX < aBestRect.mX))
                {
                    aBest = aTarget;
                    aBestRect = aRect;
                }
            }
            return aBest;
        }

        private static List<NavTarget> CollectTargets(SexyAppBase theApp)
        {
            List<NavTarget> aList = new List<NavTarget>();
            Collect(theApp.mWidgetManager, theApp.mWidgetManager, aList);
            return aList;
        }

        private static void Collect(WidgetManager theManager, WidgetContainer theContainer, List<NavTarget> theList)
        {
            foreach (Widget aWidget in theContainer.mWidgets)
            {
                if (!aWidget.mVisible || aWidget.mNavIgnore)
                {
                    continue;
                }
                if (aWidget.IsNavigable() && IsReachable(theManager, aWidget))
                {
                    if (aWidget is INavCells aCells)
                    {
                        int aCount = aCells.GetNavCellCount();
                        for (int i = 0; i < aCount; i++)
                        {
                            TRect aRect = aCells.GetNavCellRect(i);
                            if (aRect.mWidth > 0 && aRect.mHeight > 0)
                            {
                                theList.Add(new NavTarget(aWidget, i));
                            }
                        }
                    }
                    else
                    {
                        theList.Add(new NavTarget(aWidget));
                    }
                }
                Collect(theManager, aWidget, theList);
            }
        }

        /// <summary>True when a click in the middle of the widget's visible part would reach it (not hidden or under a modal dialog).</summary>
        private static bool IsReachable(WidgetManager theManager, Widget theWidget)
        {
            TRect aRect = GetVisibleRect(theWidget);
            if (aRect.mWidth <= 0 || aRect.mHeight <= 0)
            {
                return false;
            }
            Widget aHit = theManager.GetWidgetAt(aRect.mX + aRect.mWidth / 2, aRect.mY + aRect.mHeight / 2);
            if (aHit == null)
            {
                return false;
            }
            // A scroll view takes the hit for its contents (it passes clicks on itself), so a hit on one holding the
            // widget counts too.
            return IsInside(aHit, theWidget) || (aHit is ScrollWidget && IsInside(theWidget, aHit));
        }

        /// <summary>The widget's rectangle on the screen, cut to the scroll views it is in.</summary>
        private static TRect GetVisibleRect(Widget theWidget)
        {
            CGPoint aPos = theWidget.GetAbsPos();
            TRect aRect = new TRect((int)aPos.x, (int)aPos.y, theWidget.mWidth, theWidget.mHeight);
            for (WidgetContainer aParent = theWidget.mParent; aParent is Widget aWidget; aParent = aWidget.mParent)
            {
                if (aWidget is ScrollWidget)
                {
                    CGPoint aParentPos = aWidget.GetAbsPos();
                    aRect = aRect.Intersection(new TRect((int)aParentPos.x, (int)aParentPos.y, aWidget.mWidth, aWidget.mHeight));
                }
            }
            return aRect;
        }

        /// <summary>A target's rectangle on the screen (a cell may be scrolled out of view).</summary>
        public static TRect GetRect(NavTarget theTarget)
        {
            CGPoint aPos = theTarget.mWidget.GetAbsPos();
            if (theTarget.mCell >= 0 && theTarget.mWidget is INavCells aCells)
            {
                TRect aCell = aCells.GetNavCellRect(theTarget.mCell);
                return new TRect((int)aPos.x + aCell.mX, (int)aPos.y + aCell.mY, aCell.mWidth, aCell.mHeight);
            }
            return new TRect((int)aPos.x, (int)aPos.y, theTarget.mWidget.mWidth, theTarget.mWidget.mHeight);
        }

        /// <summary>
        /// True when theWidget is part of theContainer: inside it, or one of a dialog's Yes/No buttons, which Dialog
        /// adds to the widget manager beside the dialog rather than inside it.
        /// </summary>
        public static bool BelongsTo(Widget theWidget, Widget theContainer)
        {
            if (IsInside(theWidget, theContainer))
            {
                return true;
            }
            return theContainer is Dialog aDialog &&
                ((aDialog.mYesButton != null && IsInside(theWidget, aDialog.mYesButton)) ||
                 (aDialog.mNoButton != null && IsInside(theWidget, aDialog.mNoButton)));
        }

        public static bool IsInside(Widget theWidget, Widget theContainer)
        {
            for (WidgetContainer aCur = theWidget; aCur != null; aCur = aCur.mParent)
            {
                if (aCur == theContainer)
                {
                    return true;
                }
                if (aCur is WidgetManager)
                {
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// The nearest target from theFrom in theDir, among theTargets: ahead of it, scored by the gap plus twice the
        /// sideways offset; ones far off to the side (more than three times further sideways than ahead) don't count.
        /// </summary>
        public static NavTarget FindInDirection(NavTarget theFrom, NavAction theDir, IEnumerable<NavTarget> theTargets)
        {
            TRect aFrom = GetRect(theFrom);
            float aFromX = aFrom.mX + aFrom.mWidth / 2f;
            float aFromY = aFrom.mY + aFrom.mHeight / 2f;
            NavTarget aBest = NavTarget.None;
            float aBestScore = float.MaxValue;
            foreach (NavTarget aTarget in theTargets)
            {
                if (aTarget == theFrom)
                {
                    continue;
                }
                TRect aRect = GetRect(aTarget);
                float aX = aRect.mX + aRect.mWidth / 2f;
                float aY = aRect.mY + aRect.mHeight / 2f;
                float anAhead;
                float aGap;
                float aSide;
                bool anOverlap;
                switch (theDir)
                {
                    case NavAction.Right:
                        anAhead = aX - aFromX;
                        aGap = aRect.mX - (aFrom.mX + aFrom.mWidth);
                        aSide = Math.Abs(aY - aFromY);
                        anOverlap = aRect.mY < aFrom.mY + aFrom.mHeight && aFrom.mY < aRect.mY + aRect.mHeight;
                        break;
                    case NavAction.Left:
                        anAhead = aFromX - aX;
                        aGap = aFrom.mX - (aRect.mX + aRect.mWidth);
                        aSide = Math.Abs(aY - aFromY);
                        anOverlap = aRect.mY < aFrom.mY + aFrom.mHeight && aFrom.mY < aRect.mY + aRect.mHeight;
                        break;
                    case NavAction.Down:
                        anAhead = aY - aFromY;
                        aGap = aRect.mY - (aFrom.mY + aFrom.mHeight);
                        aSide = Math.Abs(aX - aFromX);
                        anOverlap = aRect.mX < aFrom.mX + aFrom.mWidth && aFrom.mX < aRect.mX + aRect.mWidth;
                        break;
                    case NavAction.Up:
                        anAhead = aFromY - aY;
                        aGap = aFrom.mY - (aRect.mY + aRect.mHeight);
                        aSide = Math.Abs(aX - aFromX);
                        anOverlap = aRect.mX < aFrom.mX + aFrom.mWidth && aFrom.mX < aRect.mX + aRect.mWidth;
                        break;
                    default:
                        return NavTarget.None;
                }
                if (anAhead <= 0.5f || (!anOverlap && aSide > anAhead * 3))
                {
                    continue;
                }
                float aScore = Math.Max(0, aGap) + aSide * (anOverlap ? 0.5f : 2f);
                if (aScore < aBestScore)
                {
                    aBestScore = aScore;
                    aBest = aTarget;
                }
            }
            return aBest;
        }

        /// <summary>The handler on top (scanning down to the modal dialog) that holds the selection, or any if there is none.</summary>
        private static INavHandler GetHandler(SexyAppBase theApp)
        {
            WidgetManager aManager = theApp.mWidgetManager;
            for (LinkedListNode<Widget> aNode = aManager.mWidgets.Last; aNode != null; aNode = aNode.Previous)
            {
                Widget aWidget = aNode.Value;
                if (aWidget.mVisible && aWidget is INavHandler aHandler && (sSelected.IsNone || BelongsTo(sSelected.mWidget, aWidget)))
                {
                    return aHandler;
                }
                if (aWidget == aManager.mBaseModalWidget)
                {
                    break;
                }
            }
            return null;
        }

        /// <summary>The dialog on top that takes clicks (not one scrolling off), down to the modal one.</summary>
        public static Dialog GetTopDialog(SexyAppBase theApp)
        {
            WidgetManager aManager = theApp.mWidgetManager;
            for (LinkedListNode<Widget> aNode = aManager.mWidgets.Last; aNode != null; aNode = aNode.Previous)
            {
                Widget aWidget = aNode.Value;
                if (aWidget.mVisible && aWidget.mMouseVisible && aWidget is Dialog aDialog)
                {
                    return aDialog;
                }
                if (aWidget == aManager.mBaseModalWidget)
                {
                    break;
                }
            }
            return null;
        }

        // ---------------------------------------------------------------- drawing

        /// <summary>A pulsing frame round the selection, for widgets that don't draw their own (Widget.DrawsNavHighlight).</summary>
        public static void Draw(SexyAppBase theApp)
        {
            if (!sShowing || sSelected.IsNone || sSelected.mWidget.mWidgetManager == null)
            {
                return;
            }
            if (sSelected.mCell < 0 && sSelected.mWidget.DrawsNavHighlight)
            {
                return;
            }
            TRect aRect = GetRect(sSelected);
            int aUnit = Math.Max(2, theApp.mHeight / 200);
            aRect = aRect.Inflate(aUnit, aUnit);
            Graphics g = Graphics.GetNew();
            // Cut to the scroll views the selection is in, so a cell scrolling into view doesn't draw outside them.
            TRect aClip = new TRect(-theApp.mWidth, -theApp.mHeight, theApp.mWidth * 3, theApp.mHeight * 3);
            for (WidgetContainer aParent = sSelected.mWidget.mParent; aParent is Widget aWidget; aParent = aWidget.mParent)
            {
                if (aWidget is ScrollWidget)
                {
                    CGPoint aPos = aWidget.GetAbsPos();
                    aClip = aClip.Intersection(new TRect((int)aPos.x - aUnit * 2, (int)aPos.y - aUnit * 2, aWidget.mWidth + aUnit * 4, aWidget.mHeight + aUnit * 4));
                }
            }
            g.SetClipRect(aClip);
            int anAlpha = 140 + (int)(115 * Pulse);
            g.SetColor(new Color(255, 230, 80, anAlpha));
            g.FillRect(aRect.mX, aRect.mY, aRect.mWidth, aUnit);
            g.FillRect(aRect.mX, aRect.mY + aRect.mHeight - aUnit, aRect.mWidth, aUnit);
            g.FillRect(aRect.mX, aRect.mY + aUnit, aUnit, aRect.mHeight - aUnit * 2);
            g.FillRect(aRect.mX + aRect.mWidth - aUnit, aRect.mY + aUnit, aUnit, aRect.mHeight - aUnit * 2);
            g.PrepareForReuse();
        }
    }
}
