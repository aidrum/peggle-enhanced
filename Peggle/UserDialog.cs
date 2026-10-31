using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// "Who are you?" profile picker (Sexy::UserDialog): a scrolling list of player names with
    /// Rename / Delete / Create New User buttons and OK/Cancel.
    /// </summary>
    public class UserDialog : ThunderDialog, ListListener
    {
        public ThunderballApp mApp;                               // 0x12c

        public ThunderListWidget mUserList;                       // 0x130

        public ThunderButton mRenameButton;                       // 0x134

        public ThunderButton mDeleteButton;                       // 0x138

        public ThunderButton mCreateButton;                       // 0x13c

        public ThunderScrollWidget mScrollWidget;                 // 0x140

        public bool mHasCreateLine;                               // 0x144

        public UserDialog(ThunderballApp theApp)
            : base(DialogId.DIALOG_USER, true, SexyAppBase.Loc(""), "", "", 2)
        {
            mApp = theApp;
            mCloseOnButton = true;
            mHasCreateLine = false;
            mUserList = SexyUtil.MakeListWidget(0, this);
            mUserList.mJustify = ThunderListWidget.JUSTIFY_CENTER;
            mRenameButton = SexyUtil.MakeButton(0, this, SexyAppBase.Loc("Rename"));
            mDeleteButton = SexyUtil.MakeButton(1, this, SexyAppBase.Loc("Delete"));
            mCreateButton = SexyUtil.MakeButton(2, this, SexyAppBase.Loc("Create New User"));
            mYesButton.mLabel = SexyAppBase.Loc("OK");
            mNoButton.mLabel = SexyAppBase.Loc("Cancel");
            if (mHasCreateLine)
            {
                mUserList.AddLine(SexyAppBase.Loc("(Create a New User)"), false);
            }
            PlayerInfo current = theApp.mPlayerInfo;
            if (current != null)
            {
                mUserList.SetSelect(mUserList.AddLine(current.mName, false));
            }
            foreach (KeyValuePair<string, PlayerInfo> entry in theApp.mProfileMgr.GetProfileMap())
            {
                if (current == null || entry.Value.mName != current.mName)
                {
                    mUserList.AddLine(entry.Value.mName, false);
                }
            }
            mScrollWidget = new ThunderScrollWidget();
            mScrollWidget.AddWidget(mUserList);
            AddWidget(mScrollWidget);
            AddWidget(mRenameButton);
            AddWidget(mDeleteButton);
            AddWidget(mCreateButton);
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int listWidth = USER_DIALOG_W;
            int btnWidth = USER_DIALOG_BTN_W;
            int btnX = USER_DIALOG_BTN_X;
            mScrollWidget.Resize(USER_DIALOG_X, USER_DIALOG_Y, listWidth, USER_DIALOG_H);
            mUserList.Resize(0, 0, listWidth, mUserList.GetOptimalHeight());
            mYesButton.Resize(btnX, mYesButton.mY, btnWidth, mYesButton.mHeight);
            mNoButton.Resize(btnX + USER_DIALOG_NO_BTN_X, mNoButton.mY, btnWidth, mNoButton.mHeight);
            mRenameButton.Layout((int)(LayoutFlags.LAY_SameLeft | LayoutFlags.LAY_Above | LayoutFlags.LAY_SameWidth | LayoutFlags.LAY_SameHeight),
                mYesButton, -mX, -5 - mY, 0, 0);
            mDeleteButton.Layout((int)(LayoutFlags.LAY_SameLeft | LayoutFlags.LAY_Above | LayoutFlags.LAY_SameWidth | LayoutFlags.LAY_SameHeight),
                mNoButton, -mX, -5 - mY, 0, 0);
            mCreateButton.Layout((int)(LayoutFlags.LAY_SameLeft | LayoutFlags.LAY_Above | LayoutFlags.LAY_SameHeight),
                mRenameButton, 0, -5, 0, 0);
            mCreateButton.Layout((int)LayoutFlags.LAY_GrowToRight, mDeleteButton, 0, 0, 0, 0);
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return SCREEN_HEIGHT;
        }

        public override void AddedToManager(WidgetManager theWidgetManager)
        {
            base.AddedToManager(theWidgetManager);
            theWidgetManager.SetFocus(mUserList);
        }

        public override void RemovedFromManager(WidgetManager theWidgetManager)
        {
            base.RemovedFromManager(theWidgetManager);
        }

        /// <summary>The selected profile name, or "" when nothing valid is selected.</summary>
        public string GetSelName()
        {
            int idx = mUserList.mSelectIdx;
            if (idx < 0 || idx >= mUserList.GetLineCount())
            {
                return "";
            }
            return mUserList.GetStringAt(idx);
        }

        public void FinishDeleteUser()
        {
            int idx = mUserList.mSelectIdx;
            mUserList.RemoveLine(mUserList.mSelectIdx);
            idx--;
            int minIdx = GetMinListIndex();
            if (idx < minIdx)
            {
                idx = minIdx;
            }
            if (minIdx < mUserList.GetLineCount())
            {
                mUserList.SetSelect(idx);
            }
            mUserList.Resize(0, 0, mUserList.mWidth, mUserList.GetOptimalHeight());
            mScrollWidget.ClientSizeChanged();
        }

        public void FinishRenameUser(string theName)
        {
            int idx = mUserList.mSelectIdx;
            if (GetMinListIndex() <= idx)
            {
                mUserList.SetLine(idx, theName);
            }
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            TRect box = mScrollWidget.GetRect();
            box.Inflate(6, 6);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0x7f));
            SexyUtil.MyDrawImageBox(g, box, Res.IMAGE_DLG_TEXTRECT);
            g.SetColorizeImages(false);
            g.SetColor(SexyUtil.MakeColor(0x63f7ff));
            g.SetFont(mHeaderFont);
            int y = mBackgroundInsets.mTop + mContentInsets.mTop + mHeaderFont.GetAscent() - mHeaderFont.GetAscentPadding();
            string title = SexyAppBase.Loc("HU ARE YOU?");
#if PEGGLE_IPHONE
            g.DrawString(title, USER_DIALOG_HU_STRING_X, y);
#else
            g.DrawString(title, USER_DIALOG_HU_STRING_X - mHeaderFont.StringWidth(title) / 2, y);
#endif
#if PEGGLE_IPHONE
            // Port addition (ultrawide): Hu stays in the window's corner when it is wider than the game.
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            aWide.DrawImage(Res.IMAGE_HU_LARGE, USER_DIALOG_HU_IMG_X + GetLeftCornerShift(mWidth), 0);
#else
            g.DrawImage(Res.IMAGE_HU_LARGE, USER_DIALOG_HU_IMG_X, 0);
#endif
        }

        public void ListClicked(int theId, int theIdx, int theClickCount)
        {
            if (theIdx == 0 && mHasCreateLine)
            {
                mApp.PlaySample(Res.SOUND_BUTTON1);
                mApp.DoCreateUserDialog();
                return;
            }
            mUserList.SetSelect(theIdx);
            if (theClickCount == 2)
            {
                mApp.FinishUserDialog(true);
            }
        }

        public void ListClosed(int theId)
        {
        }

        public void ListHiliteChanged(int theId, int theOldIdx, int theNewIdx)
        {
        }

        public override void ButtonDepress(int theId)
        {
            base.ButtonDepress(theId);
            string name = GetSelName();
            if (theId == 0)
            {
                if (name.Length != 0)
                {
                    mApp.DoRenameUserDialog(name);
                }
            }
            else if (theId == 2)
            {
                mApp.DoCreateUserDialog();
            }
            else if (theId == 1)
            {
                if (name.Length != 0)
                {
                    mApp.DoConfirmDeleteUserDialog(name);
                }
            }
        }

        public int GetMinListIndex()
        {
            return mHasCreateLine ? 1 : 0;
        }
    }
}
