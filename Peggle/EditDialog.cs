using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Single-line text entry dialog (Sexy::EditDialog). Used by the replay save/edit dialogs
    /// (ids 30/31) and the high score entry; a header, a prompt and one EditWidget.
    /// </summary>
    public class EditDialog : ThunderDialog, EditListener
    {
        // 0x128: EditListener sub-object

        public EditWidget mEditWidget;                            // 0x12c

        public bool mAlphaNumericOnly;                            // 0x130

        public string mPrompt = "";                               // 0x134

        public bool mNeedFocus;                                   // 0x138

        public int mEditOffsetY;                                  // 0x13c

        // 0005a664
        public EditDialog(int theId, string theHeader, string thePrompt, bool alphaNumericOnly)
            : base(theId, true, theHeader, "", "", 2)
        {
            mPrompt = thePrompt;
            mEditWidget = SexyUtil.CreateEditWidget(0, this, false);
            mEditWidget.mMaxChars = alphaNumericOnly ? 0xc : 0x20;
            mAlphaNumericOnly = alphaNumericOnly;
            mNeedFocus = true;
            mScrollTargetY = 0;                                   // 0xf8 (sic: the original stores 0 here)
            mEditOffsetY = 0;
        }

        // 0005aa1c
        public override void Dispose()
        {
            mEditWidget?.Dispose();
            base.Dispose();
        }

        // 0005ab7c
        public void SetString(string theString)
        {
            mEditWidget.SetText(theString);
        }

        // 0005aba0
        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            mEditWidget.Resize(GetLeft() + EDIT_DIALOG_OFFSET_X, GetTop() + EDIT_DIALOG_OFFSET_Y + mEditOffsetY,
                GetWidth() + EDIT_DIALOG_OFFSET_W, mEditWidget.mHeight);
        }

        // 0005ac60
        public override int GetPreferredHeight(int theWidth)
        {
            return EDIT_DIALOG_H;
        }

        // 0005ac78
        public override void AddedToManager(WidgetManager theWidgetManager)
        {
            base.AddedToManager(theWidgetManager);
            theWidgetManager.AddWidget(mEditWidget);
        }

        // 0005acb0
        public override void RemovedFromManager(WidgetManager theWidgetManager)
        {
            base.RemovedFromManager(theWidgetManager);
            theWidgetManager.RemoveWidget(mEditWidget);
        }

        // 0005ace8
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            DrawEditBox(g, mEditWidget, 0);
#if PEGGLE_IPHONE
            // The iPhone draws the prompt above the box (HD never shows it).
            g.SetFont(Res.FONT_PLAINOUTLINE);
            WriteCenteredLine(g, EDIT_DIALOG_PROMT_TEXT_Y - DIALOG_SAVE_REPLAY_EDIT_EXT_CHAR_OFFSET_FIX, mPrompt);
#endif
            g.SetFont(Res.FONT_PLAINOUTLINE);
        }

        /// <summary>
        /// Sexy::DrawEditBox(Graphics*, EditWidget*, int) @ 0010bc44, as the original computes it
        /// (relative to the Graphics translation). SexyUtil.DrawEditBox takes a dialog instead.
        /// </summary>
        internal static void DrawEditBox(Graphics g, EditWidget theWidget, int theOffsetY)
        {
            int x = (int)(theWidget.mX - (float)g.mTransX - DIALOG_EDIT_BOX_PAD_X);
            int y = (int)(theWidget.mY - (float)g.mTransY - DIALOG_EDIT_BOX_PAD_Y - DIALOG_SAVE_REPLAY_EDIT_EXT_CHAR_OFFSET_FIX - theOffsetY);
            g.DrawImageBox(new TRect(x, y, theWidget.mWidth + DIALOG_EDIT_BOX_PAD_W, theWidget.mHeight + DIALOG_EDIT_BOX_PAD_H), Res.IMAGE_DLG_TEXTBOX);
        }

        // 0005ad34
        public string GetString()
        {
            return mEditWidget.mString;
        }

        // 0005ad50
        public void EditWidgetText(int theId, string theString)
        {
            ButtonDepress(mYesButton.mId);
        }

        // 0005ad9c
        public bool AllowChar(int theId, char theChar)
        {
            if (!mAlphaNumericOnly)
            {
                return true;
            }
            return isalnum(theChar) || theChar == ' ';
        }

        public bool AllowText(int theId, string theText)
        {
            return true;
        }

        public bool AllowClear(int theId)
        {
            return true;
        }

        // 0005ae2c
        public override void Update()
        {
            if (mNeedFocus && !IsScrolling())
            {
                mWidgetManager.SetFocus(mEditWidget);
                mNeedFocus = false;
            }
            base.Update();
        }

        /// <summary>Sexy::isalnum(wchar_t) @ 0010bd60: ASCII letters/digits plus Latin-1 0xc0..0xff.</summary>
        private static bool isalnum(char theChar)
        {
            return (theChar >= '0' && theChar <= '9') ||
                   (theChar >= 'a' && theChar <= 'z') ||
                   (theChar >= 'A' && theChar <= 'Z') ||
                   (theChar >= 0xc0 && theChar <= 0xff);
        }
    }
}
