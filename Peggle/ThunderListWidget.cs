using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Peggle's list box (Sexy::ThunderListWidget): Sexy's ListWidget without a scrollbar (it lives
    /// inside a ThunderScrollWidget), with a fixed item height and the selected/hilited row drawn
    /// over a tinted IMAGE_DLG_GROUPRECT box. Linked lists (mParentList/mChild) share rows as columns.
    /// </summary>
    public class ThunderListWidget : Widget
    {
        public const int COLOR_BKG = 0;

        public const int COLOR_OUTLINE = 1;

        public const int COLOR_TEXT = 2;

        public const int COLOR_HILITE = 3;

        public const int COLOR_SELECT = 4;

        public const int COLOR_SELECT_TEXT = 5;

        public const int JUSTIFY_LEFT = 0;

        public const int JUSTIFY_CENTER = 1;

        public const int JUSTIFY_RIGHT = 2;

        public static readonly int[,] gInitialListWidgetColors =
        {
            { 255, 255, 255 },
            { 255, 255, 255 },
            { 0, 0, 0 },
            { 0, 192, 0 },
            { 0, 0, 128 },
            { 255, 255, 255 }
        };

        public int mId;                                           // 0x78

        public Font mFont;                                        // 0x7c

        public int mJustify;                                      // 0x80

        public List<string> mLines = new List<string>();          // 0x84

        public List<Color> mLineColors = new List<Color>();       // 0x90

        public double mPosition;                                  // 0x9c

        public double mPageSize;                                  // 0xa4

        public int mHiliteIdx;                                    // 0xac

        public int mSelectIdx;                                    // 0xb0

        public ListListener mListListener;                        // 0xb4

        public ThunderListWidget mParentList;                     // 0xb8

        public ThunderListWidget mChild;                          // 0xbc

        public bool mSortFromChild;                               // 0xc0

        public bool mDrawOutline;                                 // 0xc1

        public int mMaxNumericPlaces;                             // 0xc4

        public int mItemHeight;                                   // 0xc8

        public bool mDrawSelectWhenHilited;                       // 0xcc

        public bool mDoFingerWhenHilited;                         // 0xcd

        public ThunderListWidget(int theId, Font theFont, ListListener theListListener)
        {
            mJustify = JUSTIFY_LEFT;
            mHiliteIdx = -1;
            mSelectIdx = -1;
            mItemHeight = theFont == null ? -1 : theFont.GetHeight();
            SetColors(gInitialListWidgetColors, 6);
            mId = theId;
            mFont = theFont;
            mListListener = theListListener;
            mParentList = null;
            mChild = null;
            mPosition = 0.0;
            mPageSize = 0.0;
            mSortFromChild = false;
            mDrawOutline = true;
            mMaxNumericPlaces = 0;
            mDrawSelectWhenHilited = false;
            mDoFingerWhenHilited = true;
        }

        private int GetItemHeight()
        {
            return mItemHeight == -1 ? mFont.GetHeight() : mItemHeight;
        }

        private ThunderListWidget GetFirstInChain()
        {
            ThunderListWidget list = this;
            while (list.mParentList != null)
            {
                list = list.mParentList;
            }
            return list;
        }

        public override void RemovedFromManager(WidgetManager theManager)
        {
            base.RemovedFromManager(theManager);
            mListListener?.ListClosed(mId);
        }

        public virtual string GetSortKey(int theIdx)
        {
            string key = mLines[theIdx];
            while (key.Length < mMaxNumericPlaces)
            {
                key = "0" + key;
            }
            if (mSortFromChild)
            {
                return mChild.GetSortKey(theIdx) + key;
            }
            if (mChild == null)
            {
                return key;
            }
            return key + mChild.GetSortKey(theIdx);
        }

        public virtual void Sort(bool ascending)
        {
            int count = mLines.Count;
            int[] map = new int[count];
            string[] keys = new string[count];
            for (int i = 0; i < count; i++)
            {
                map[i] = i;
                keys[i] = GetSortKey(i);
            }
            for (int i = 1; i < count; i++)
            {
                for (int j = 0; j < count - i; j++)
                {
                    int cmp = string.CompareOrdinal(keys[j], keys[j + 1]);
                    if ((ascending && cmp > 0) || (!ascending && cmp < 0))
                    {
                        int swapIdx = map[j];
                        map[j] = map[j + 1];
                        map[j + 1] = swapIdx;
                        string swapKey = keys[j];
                        keys[j] = keys[j + 1];
                        keys[j + 1] = swapKey;
                    }
                }
            }
            for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
            {
                List<string> lines = new List<string>();
                List<Color> colors = new List<Color>();
                for (int i = 0; i < count; i++)
                {
                    lines.Add(list.mLines[map[i]]);
                    colors.Add(list.mLineColors[map[i]]);
                }
                list.mLines = lines;
                list.mLineColors = colors;
                list.MarkDirty();
            }
        }

        public virtual string GetStringAt(int theIdx)
        {
            return mLines[theIdx];
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            double pageSize = 1.0;
            int itemHeight = GetItemHeight();
            if (itemHeight + 8 < mHeight)
            {
                pageSize = (mHeight - 8.0) / itemHeight;
            }
            mPageSize = pageSize;
        }

        public virtual int AddLine(string theLine, bool alphabetical)
        {
            int idx = -1;
            bool inserted = false;
            if (alphabetical)
            {
                for (int i = 0; i < mLines.Count; i++)
                {
                    if (string.CompareOrdinal(theLine, mLines[i]) < 0)
                    {
                        idx = i;
                        for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
                        {
                            list.mLines.Insert(i, list == this ? theLine : "-");
                            list.mLineColors.Insert(i, mColors[COLOR_TEXT]);
                            list.MarkDirty();
                        }
                        inserted = true;
                        break;
                    }
                }
            }
            if (!inserted)
            {
                idx = mLines.Count;
                for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
                {
                    list.mLines.Add(list == this ? theLine : "-");
                    list.mLineColors.Add(mColors[COLOR_TEXT]);
                    list.MarkDirty();
                }
            }
            return idx;
        }

        public virtual void SetLine(int theIdx, string theString)
        {
            mLines[theIdx] = theString;
            MarkDirty();
        }

        public virtual int GetLineCount()
        {
            return mLines.Count;
        }

        public virtual int GetLineIdx(string theLine)
        {
            for (int i = 0; i < mLines.Count; i++)
            {
                if (string.CompareOrdinal(mLines[i], theLine) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public virtual void SetColor(string theLine, Color theColor)
        {
            SetLineColor(GetLineIdx(theLine), theColor);
        }

        public virtual void SetLineColor(int theIdx, Color theColor)
        {
            if (theIdx < 0 || theIdx >= mLines.Count)
            {
                return;
            }
            for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
            {
                list.mLineColors[theIdx] = theColor;
                list.MarkDirty();
            }
        }

        public virtual void RemoveLine(int theIdx)
        {
            if (theIdx == -1)
            {
                return;
            }
            for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
            {
                list.mLines.RemoveAt(theIdx);
                list.mLineColors.RemoveAt(theIdx);
                list.MarkDirty();
            }
        }

        public virtual void RemoveAll()
        {
            for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
            {
                list.mLines.Clear();
                list.mLineColors.Clear();
                list.mSelectIdx = -1;
                list.mHiliteIdx = -1;
                list.MarkDirty();
            }
        }

        public virtual int GetOptimalWidth()
        {
            int width = 0;
            for (int i = 0; i < mLines.Count; i++)
            {
                width = Math.Max(width, mFont.StringWidth(mLines[i]));
            }
            return width + 0x10;
        }

        public virtual int GetOptimalHeight()
        {
            return GetItemHeight() * mLines.Count + 8;
        }

        public override void OrderInManagerChanged()
        {
            base.OrderInManagerChanged();
            if (mChild != null)
            {
                GlobalStaticVars.gSexyAppBase.mWidgetManager.PutInfront(mChild, this);
            }
        }

        public override void Draw(Graphics g)
        {
            g.SetColor(mColors[COLOR_BKG]);
            g.FillRect(0, 0, mWidth, mHeight);
            Graphics textG = Graphics.GetNew(g);
            textG.ClipRect(4, 4, mWidth - 8, mHeight - 8);
            Graphics boxG = Graphics.GetNew(g);
            boxG.ClipRect(0, 4, mWidth, mHeight - 8);
            textG.SetFont(mFont);
            int first = (int)mPosition;
            int last = Math.Min(mLines.Count - 1, (int)mPosition + (int)mPageSize + 1);
            int itemHeight;
            int textOffsetY;
            if (mItemHeight == -1)
            {
                itemHeight = mFont.GetHeight();
                textOffsetY = 0;
            }
            else
            {
                itemHeight = mItemHeight;
                textOffsetY = (mItemHeight - mFont.GetHeight()) / 2;
            }
            for (int i = first; i <= last; i++)
            {
                int y = (int)((i - mPosition) * itemHeight) + 4;
                if (i == mSelectIdx || (i == mHiliteIdx && mDrawSelectWhenHilited))
                {
                    boxG.SetColor(mColors[COLOR_SELECT]);
                    boxG.SetColorizeImages(true);
                    SexyUtil.MyDrawImageBox(boxG, new TRect(0, y, mWidth, itemHeight), Res.IMAGE_DLG_GROUPRECT);
                    boxG.SetColorizeImages(false);
                }
                if (i == mHiliteIdx)
                {
                    textG.SetColor(mColors[COLOR_HILITE]);
                }
                else if (i == mSelectIdx && mColors.Count > 5)
                {
                    textG.SetColor(mColors[COLOR_SELECT_TEXT]);
                }
                else
                {
                    textG.SetColor(mLineColors[i]);
                }
                string line = mLines[i];
                int x;
                if (mJustify == JUSTIFY_CENTER)
                {
                    x = (mWidth - mFont.StringWidth(line)) / 2;
                }
                else if (mJustify == JUSTIFY_LEFT)
                {
                    x = 4;
                }
                else
                {
                    x = mWidth - mFont.StringWidth(line) - 4;
                }
                textG.DrawString(line, x, textOffsetY + y + mFont.GetAscent());
            }
            if (mDrawOutline)
            {
                g.SetColor(mColors[COLOR_OUTLINE]);
                g.DrawRect(0, 0, mWidth - 1, mHeight - 1);
            }
            boxG.PrepareForReuse();
            textG.PrepareForReuse();
        }

        public virtual void ScrollPosition(int theId, double thePosition)
        {
            mChild?.ScrollPosition(theId, thePosition);
            mPosition = thePosition;
            MarkDirty();
        }

        public virtual void SetHilite(int theHiliteIdx, bool notifyListener)
        {
            int oldIdx = mHiliteIdx;
            mHiliteIdx = theHiliteIdx;
            if (oldIdx != mHiliteIdx && notifyListener && mListListener != null)
            {
                mListListener.ListHiliteChanged(mId, oldIdx, mHiliteIdx);
            }
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            int idx = (int)((y - 4) / (double)GetItemHeight() + mPosition);
            if (idx < 0 || idx >= mLines.Count)
            {
                idx = -1;
            }
            if (idx != mHiliteIdx)
            {
                for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
                {
                    list.SetHilite(idx, true);
                    list.MarkDirty();
                }
            }
            if (mHiliteIdx != -1 && mListListener != null)
            {
                mListListener.ListClicked(mId, mHiliteIdx, theClickCount);
            }
        }

        public override void MouseLeave()
        {
            for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
            {
                list.SetHilite(-1, true);
                list.MarkDirty();
            }
        }

        public virtual void SetSelect(int theSelectIdx)
        {
            for (ThunderListWidget list = GetFirstInChain(); list != null; list = list.mChild)
            {
                list.mSelectIdx = theSelectIdx;
                list.MarkDirty();
            }
        }
    }
}
