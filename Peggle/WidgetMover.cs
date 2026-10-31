using System.Collections.Generic;

namespace Sexy
{
    /// <summary>Screen transition directions (Sexy::WidgetScroll).</summary>
    public static class WidgetScroll
    {
        public const int SCROLL_NONE = 0;

        public const int SCROLL_LEFT = 1;

        public const int SCROLL_RIGHT = 2;

        public const int SCROLL_UP = 3;

        public const int SCROLL_DOWN = 4;

        public const int SCROLL_FADE = 5;
    }

    /// <summary>One widget sliding on or off screen (Sexy::MovingWidget).</summary>
    public class MovingWidget
    {
        public Widget mWidget;                                    // 0x00

        public int mFramesLeft;                                   // 0x04

        public bool mRemoveWhenDone;                              // 0x08

        public float mStartX;                                     // 0x0c

        public float mStartY;                                     // 0x10

        public float mEndX;                                       // 0x14

        public float mEndY;                                       // 0x18

        public int mScroll;                                       // 0x1c

        public int mUpdateCnt;                                    // 0x20

        private void Remove()
        {
            if (mRemoveWhenDone)
            {
                GlobalStaticVars.gSexyAppBase.mWidgetManager.RemoveWidget(mWidget);
                GlobalStaticVars.gSexyAppBase.SafeDeleteWidget(mWidget);
            }
        }

        /// <returns>false once the move has finished and the entry should be dropped.</returns>
        public bool Update()
        {
            mUpdateCnt++;
            mFramesLeft--;
            if (mScroll == WidgetScroll.SCROLL_FADE)
            {
                GlobalStaticVars.gSexyAppBase.mWidgetManager.BringToFront(mWidget);
                mWidget.MarkDirty();
                if (mUpdateCnt > 99)
                {
                    Remove();
                    return false;
                }
            }
            else
            {
                float t = mFramesLeft / 7.0f;
                float x = mStartX * t + mEndX * (1.0f - t);
                float y = mStartY * t + mEndY * (1.0f - t);
                mWidget.Move((int)x, (int)y);
                if (mFramesLeft == 0)
                {
                    Remove();
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Slides screens on and off over 7 frames (Sexy::WidgetMover). Scrolled-off widgets stop
    /// receiving updates and input and can be removed and deleted when the move finishes.
    /// </summary>
    public class WidgetMover
    {
        private const int WIDGETFLAGS_UPDATE = 1;

        private const int WIDGETFLAGS_ALLOW_MOUSE = 0x10;

        private const int WIDGETFLAGS_ALLOW_FOCUS = 0x20;

        public LinkedList<MovingWidget> mWidgets = new LinkedList<MovingWidget>();  // 0x04

        public MovingWidget GetWidgetData(Widget theWidget, bool create)
        {
            foreach (MovingWidget mw in mWidgets)
            {
                if (mw.mWidget == theWidget)
                {
                    return mw;
                }
            }
            if (!create)
            {
                return null;
            }
            MovingWidget data = new MovingWidget();
            data.mWidget = theWidget;
            mWidgets.AddLast(data);
            return data;
        }

        public void ScrollOn(Widget theWidget, int theScroll)
        {
            MovingWidget data = GetWidgetData(theWidget, true);
            int x = theWidget.mX;
            int y = theWidget.mY;
            int w = theWidget.mWidth;
            int h = theWidget.mHeight;
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            data.mFramesLeft = 7;
            data.mEndX = x;
            data.mEndY = y;
            data.mRemoveWhenDone = false;
            data.mScroll = theScroll;
            switch (theScroll)
            {
            case WidgetScroll.SCROLL_NONE:
            case WidgetScroll.SCROLL_FADE:
                data.mStartX = x;
                data.mStartY = y;
                break;
            case WidgetScroll.SCROLL_LEFT:
                data.mStartX = app.mWidth;
                data.mStartY = y;
                break;
            case WidgetScroll.SCROLL_RIGHT:
                data.mStartX = -w;
                data.mStartY = y;
                break;
            case WidgetScroll.SCROLL_UP:
                data.mStartX = x;
                data.mStartY = -h;
                break;
            case WidgetScroll.SCROLL_DOWN:
                data.mStartX = x;
                data.mStartY = app.mHeight;
                break;
            }
            theWidget.mWidgetFlagsMod.mRemoveFlags = 0;
            theWidget.mMouseVisible = true;
        }

        public void ScrollOff(Widget theWidget, int theScroll, bool removeWhenDone)
        {
            MovingWidget data = GetWidgetData(theWidget, true);
            int x = theWidget.mX;
            int y = theWidget.mY;
            int w = theWidget.mWidth;
            int h = theWidget.mHeight;
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            data.mFramesLeft = 7;
            data.mStartX = x;
            data.mStartY = y;
            data.mRemoveWhenDone = removeWhenDone;
            data.mScroll = theScroll;
            switch (theScroll)
            {
            case WidgetScroll.SCROLL_NONE:
                data.mEndX = x;
                data.mEndY = y;
                break;
            case WidgetScroll.SCROLL_LEFT:
                data.mEndX = -w;
                data.mEndY = y;
                break;
            case WidgetScroll.SCROLL_RIGHT:
                data.mEndX = app.mWidth;
                data.mEndY = y;
                break;
            case WidgetScroll.SCROLL_UP:
                data.mEndX = x;
                data.mEndY = app.mHeight;
                break;
            case WidgetScroll.SCROLL_DOWN:
                data.mEndX = x;
                data.mEndY = -h;
                break;
            }
            theWidget.mWidgetFlagsMod.mRemoveFlags |= WIDGETFLAGS_UPDATE | WIDGETFLAGS_ALLOW_MOUSE | WIDGETFLAGS_ALLOW_FOCUS;
            theWidget.mMouseVisible = false;
        }

        public void Update()
        {
            LinkedListNode<MovingWidget> node = mWidgets.First;
            while (node != null)
            {
                LinkedListNode<MovingWidget> next = node.Next;
                if (!node.Value.Update())
                {
                    mWidgets.Remove(node);
                }
                node = next;
            }
        }

        public void Clear()
        {
            foreach (MovingWidget mw in mWidgets)
            {
                if (mw.mRemoveWhenDone)
                {
                    GlobalStaticVars.gSexyAppBase.mWidgetManager.RemoveWidget(mw.mWidget);
                    GlobalStaticVars.gSexyAppBase.SafeDeleteWidget(mw.mWidget);
                }
            }
            mWidgets.Clear();
        }

        public bool WillRemove(Widget theWidget)
        {
            MovingWidget data = GetWidgetData(theWidget, false);
            return data != null && data.mRemoveWhenDone;
        }

        /// <summary>Port: true while a widget is scrolling off to be removed.</summary>
        public bool IsRemovingAny()
        {
            foreach (MovingWidget mw in mWidgets)
            {
                if (mw.mRemoveWhenDone)
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveWidget(Widget theWidget)
        {
            MovingWidget data = GetWidgetData(theWidget, false);
            if (data != null)
            {
                mWidgets.Remove(data);
            }
        }
    }
}
