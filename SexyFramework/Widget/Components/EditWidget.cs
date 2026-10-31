using System;
using System.Text;
using Microsoft.Xna.Framework;

namespace Sexy
{
    public interface EditListener
    {
        void EditWidgetText(int theId, string theString);

        bool AllowChar(int theId, char theChar);

        bool AllowText(int theId, string theText);

        bool AllowClear(int theId);
    }

    /// <summary>Callbacks from the text-entry manager (Sexy::EditManagerDelegate).</summary>
    public interface EditManagerDelegate
    {
        void EditingEnded();

        bool ShouldChangeCharacters(int theStart, int theLength, string theReplacement);

        bool ShouldClear();
    }

    /// <summary>
    /// Text entry (Sexy::EditManager). On iOS this drove a native UITextField over the EditWidget;
    /// here it is a keyboard-fed buffer that the focused EditWidget draws itself.
    /// </summary>
    public class EditManager
    {
        private static EditManager sManager;

        private readonly StringBuilder mText = new StringBuilder();

        private EditManagerDelegate mDelegate;

        public bool mEditing;

        public TRect mBounds;

        public static EditManager GetManager()
        {
            return sManager ?? (sManager = new EditManager());
        }

        public void SetText(string theText)
        {
            mText.Clear();
            mText.Append(theText ?? "");
        }

        public string GetText()
        {
            return mText.ToString();
        }

        public int GetTextLength()
        {
            return mText.Length;
        }

        public void SetDelegate(EditManagerDelegate theDelegate)
        {
            mDelegate = theDelegate;
        }

        public void SetBounds(int x, int y, int w, int h, bool animate)
        {
            mBounds = new TRect(x, y, w, h);
        }

        /// <summary>
        /// Platform text prompt (e.g. the Android soft keyboard dialog): called with the current text
        /// and a callback that receives the entered text, or null if cancelled. The callback may run
        /// on any thread; the result is applied on the game thread by <see cref="UpdatePrompt"/>.
        /// </summary>
        public static Action<string, Action<string>> TextPromptHandler;

        private readonly object mPromptLock = new object();

        private bool mPromptOpen;

        private bool mPromptDone;

        private string mPromptResult;

        public void BeginEditing()
        {
            mEditing = true;
            ShowPrompt();
        }

        /// <summary>Opens the platform text prompt, as the iOS keyboard appeared when editing began.</summary>
        public void ShowPrompt()
        {
            if (TextPromptHandler == null || mPromptOpen)
            {
                return;
            }
            mPromptOpen = true;
            TextPromptHandler(GetText(), result =>
            {
                lock (mPromptLock)
                {
                    mPromptResult = result;
                    mPromptDone = true;
                }
            });
        }

        /// <summary>
        /// Applies a finished prompt: the text replaces the field (through the same filters as typed
        /// characters), then editing ends as when the iOS keyboard's Done key was pressed.
        /// </summary>
        public void UpdatePrompt()
        {
            string result;
            lock (mPromptLock)
            {
                if (!mPromptDone)
                {
                    return;
                }
                mPromptDone = false;
                result = mPromptResult;
                mPromptResult = null;
            }
            mPromptOpen = false;
            if (result == null || !mEditing)
            {
                return;
            }
            if (mDelegate == null || mDelegate.ShouldClear())
            {
                mText.Clear();
                foreach (char c in result)
                {
                    HandleChar(c);
                }
            }
            HandleChar('\r');
        }

        public void EndEditing()
        {
            mEditing = false;
        }

        /// <summary>Feeds a typed character (or backspace/enter) from the focused EditWidget.</summary>
        public void HandleChar(char theChar)
        {
            if (!mEditing)
            {
                return;
            }
            if (theChar == '\r' || theChar == '\n')
            {
                mEditing = false;
                mDelegate?.EditingEnded();
                return;
            }
            if (theChar == '\b')
            {
                if (mText.Length > 0 && (mDelegate == null || mDelegate.ShouldChangeCharacters(mText.Length - 1, 1, "")))
                {
                    mText.Remove(mText.Length - 1, 1);
                }
                return;
            }
            if (theChar < ' ')
            {
                return;
            }
            string s = theChar.ToString();
            if (mDelegate == null || mDelegate.ShouldChangeCharacters(mText.Length, 0, s))
            {
                mText.Append(s);
            }
        }
    }

    /// <summary>A single-line text field (Sexy::EditWidget).</summary>
    public class EditWidget : Widget, EditManagerDelegate
    {
        public int mId;                                           // 0x7c

        public string mString = "";                               // 0x80

        public string mPasswordDisplayString = "";                // 0x84

        public Font mFont;                                        // 0x88

        public EditListener mEditListener;                        // 0x8c

        public int mMaxChars;                                     // 0x90

        public char mPasswordChar;                                // 0x94

        public bool mEditing;                                     // 0x98

        public bool mAllowEmptyReturn;                            // 0x99

        public int mKeyboardType;                                 // 0x9c

        public int mAutoCapitalization;                           // 0xa0

        public int mBorderStyle;                                  // 0xa4

        public int mReturnKeyType;                                // 0xa8

        public TRect mEditBounds;                                 // 0xac

        private static readonly int[,] gEditWidgetColors =
        {
            { 255, 255, 255 },
            { 255, 255, 255 },
            { 0, 0, 0 },
            { 0, 0, 0 },
            { 255, 255, 255 }
        };

        public EditWidget(int theId, EditListener theListener)
        {
            mId = theId;
            mEditListener = theListener;
            mFont = null;
            mMaxChars = -1;
            mPasswordChar = '\0';
            mEditing = false;
            mString = "";
            mAllowEmptyReturn = false;
            SetColors(gEditWidgetColors, 5);
        }

        public virtual void SetText(string theText)
        {
            mString = theText ?? "";
            if (mHasFocus)
            {
                EditManager.GetManager().SetText(mString);
            }
            MarkDirty();
        }

        public string GetDisplayString()
        {
            string text = mEditing ? EditManager.GetManager().GetText() : mString;
            if (mPasswordChar == '\0')
            {
                return text;
            }
            if (mPasswordDisplayString.Length != text.Length)
            {
                mPasswordDisplayString = new string(mPasswordChar, text.Length);
            }
            return mPasswordDisplayString;
        }

        public override bool WantsFocus()
        {
            return true;
        }

        public virtual void SetFont(Font theFont)
        {
            mFont = theFont?.Duplicate();
        }

        public override void Draw(Graphics g)
        {
            // The original draws the field only when not editing (a native text field covers it
            // otherwise); here the field is drawn in both states, with a caret while editing.
            string text = GetDisplayString();
            g.SetColor(GetColor(0));
            g.FillRect(0, 0, mWidth, mHeight);
            if (mFont != null)
            {
                g.SetFont(mFont);
                g.SetColor(GetColor(mEditing ? 3 : 2));
                int y = mFont.GetAscent() + (mHeight - mFont.GetHeight()) / 2;
                g.DrawString(text, 4, y);
                if (mEditing && (mUpdateCnt / 30) % 2 == 0)
                {
                    int x = 4 + mFont.StringWidth(text);
                    g.FillRect(x + 1, y - mFont.GetAscent(), 2, mFont.GetHeight());
                }
            }
            g.SetColor(GetColor(1));
            g.DrawRect(0, 0, mWidth - 1, mHeight - 1);
        }

        public override void Update()
        {
            base.Update();
            if (mEditing)
            {
                EditManager.GetManager().UpdatePrompt();
                MarkDirty();
            }
        }

        public override void MouseDown(int x, int y, int theBtnNum, int theClickCount)
        {
            base.MouseDown(x, y, theBtnNum, theClickCount);
            // With a platform prompt (Android), tapping the field reopens it, like tapping a UITextField.
            if (EditManager.TextPromptHandler != null)
            {
                if (!mEditing)
                {
                    GotFocus();
                }
                else
                {
                    EditManager.GetManager().ShowPrompt();
                }
            }
        }

        public override void GotFocus()
        {
            base.GotFocus();
            mEditing = true;
            mEditBounds = new TRect(mX, mY, mWidth, mHeight);
            EditManager manager = EditManager.GetManager();
            manager.SetBounds(mEditBounds.mX, mEditBounds.mY, mEditBounds.mWidth, mEditBounds.mHeight, true);
            manager.SetText(mString);
            manager.SetDelegate(this);
            manager.BeginEditing();
        }

        public override void LostFocus()
        {
            EditManager.GetManager().EndEditing();
            mEditing = false;
            base.LostFocus();
        }

        public override void KeyChar(SexyChar theChar)
        {
            EditManager.GetManager().HandleChar(theChar.value_type);
            MarkDirty();
        }

        public override void KeyDown(KeyCode theKey)
        {
            if (theKey == KeyCode.Back)
            {
                EditManager.GetManager().HandleChar('\b');
            }
            else if (theKey == KeyCode.Return)
            {
                EditManager.GetManager().HandleChar('\r');
            }
            MarkDirty();
        }

        public void EditingEnded()
        {
            mEditing = false;
            mString = EditManager.GetManager().GetText();
            mEditListener?.EditWidgetText(mId, mString);
        }

        public bool ShouldChangeCharacters(int theStart, int theLength, string theReplacement)
        {
            EditManager manager = EditManager.GetManager();
            if (mMaxChars > 0 && manager.GetTextLength() >= mMaxChars && theLength == 0)
            {
                return false;
            }
            string old = manager.GetText();
            string text = old.Substring(0, theStart) + theReplacement;
            if (theStart + theLength < old.Length)
            {
                text += old.Substring(theStart + theLength);
            }
            if (mEditListener != null)
            {
                foreach (char c in theReplacement)
                {
                    if (!mEditListener.AllowChar(mId, c))
                    {
                        return false;
                    }
                }
                if (!mEditListener.AllowText(mId, text))
                {
                    return false;
                }
            }
            mString = text;
            return true;
        }

        public bool ShouldClear()
        {
            bool clear = mEditListener == null || mEditListener.AllowClear(mId);
            if (clear)
            {
                mString = "";
            }
            return clear;
        }
    }
}
