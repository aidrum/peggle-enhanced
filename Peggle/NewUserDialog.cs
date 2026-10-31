using System;
using System.Text;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Create/rename profile dialog (Sexy::NewUserDialog). On first launch it is drawn as the
    /// Peggle Institute certificate with today's date; once a name is accepted the "approved"
    /// stamp slams down and the main menu opens.
    /// </summary>
    public class NewUserDialog : ThunderDialog, EditListener
    {
        public ThunderballApp mApp;                               // 0x12c

        public EditWidget mNameWidget;                            // 0x130

        public bool mCertificate;                                 // 0x134

        public string mDateString = "";                           // 0x138

        public int mStampCounter;                                 // 0x13c

        public bool mNeedFocus;                                   // 0x140

        public bool mShowApproval;                                // 0x141

        public int mFocusDelay;                                   // 0x144

        public NewUserDialog(ThunderballApp theApp, bool isRename, bool isCertificate, bool canCancel)
            : base(isRename ? DialogId.DIALOG_RENAME_USER : DialogId.DIALOG_CREATE_USER, true,
                isRename ? SexyAppBase.Loc("Rename User") : SexyAppBase.Loc("New User"),
                SexyAppBase.Loc("Please enter your name."), "", 2)
        {
            mApp = theApp;
            mCertificate = isCertificate;
            mWidth = mCertificate ? Res.IMAGE_CERTIFICATE.mWidth : NEWUSER_NOCERTIFICATE_W;
            mNeedFocus = false;
            mFocusDelay = 2;
            mStampCounter = 0;
            mShowApproval = false;
            mNameWidget = SexyUtil.CreateEditWidget(0, this, mCertificate);
            mNameWidget.mAllowEmptyReturn = true;
            if (mCertificate)
            {
                DateTime now = DateTime.Now;
                string format = SexyAppBase.Loc("m/d/y");
                StringBuilder sb = new StringBuilder();
                foreach (char c in format)
                {
                    switch (char.ToLowerInvariant(c))
                    {
                    case 'y':
                        sb.Append(now.Year);
                        break;
                    case 'm':
                        sb.Append(now.Month);
                        break;
                    case 'd':
                        sb.Append(now.Day);
                        break;
                    default:
                        sb.Append(c);
                        break;
                    }
                }
                mDateString = sb.ToString();
                mNameWidget.SetFont(SexyUtil.GetFontById((int)ResourceId.FONT_PLAIN_ID));
                mNameWidget.SetColor(2, SexyUtil.MakeColor(0x60));
                mNameWidget.SetColor(3, SexyUtil.MakeColor(0x80));
                mNameWidget.SetColor(4, SexyUtil.MakeColor(0xffffff));
            }
            mNameWidget.mMaxChars = 7;
            mNameWidget.mAutoCapitalization = 1;
            mNoButton.SetVisible(false);
            mYesButton.SetVisible(false);
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            mNameWidget?.Dispose();
            base.Dispose();
        }

        public override int GetPreferredHeight(int theWidth)
        {
            if (!mCertificate)
            {
                return base.GetPreferredHeight(theWidth) + NEWUSER_NOCERTIFICATE_PADDING_H;
            }
            return Res.IMAGE_CERTIFICATE.mHeight;
        }

        public override void AddedToManager(WidgetManager theWidgetManager)
        {
            base.AddedToManager(theWidgetManager);
            theWidgetManager.AddWidget(mNameWidget);
            mNeedFocus = true;
        }

        public override void RemovedFromManager(WidgetManager theWidgetManager)
        {
            base.RemovedFromManager(theWidgetManager);
            theWidgetManager.RemoveWidget(mNameWidget);
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            if (!mCertificate)
            {
                mNameWidget.Resize(NEWUSER_NOCERTIFICATE_NAME_EDIT_X + mX, NEWUSER_NOCERTIFICATE_NAME_EDIT_Y + mY,
                    mWidth - NEWUSER_NOCERTIFICATE_NAME_EDIT_W, NEWUSER_NOCERTIFICATE_NAME_EDIT_H);
            }
            else
            {
#if PEGGLE_IPHONE
                mNameWidget.Resize(SexyUtil.gGutterWidth + NEWUSER_CERTIFICATE_NAME_EDIT_X, NEWUSER_CERTIFICATE_NAME_EDIT_Y,
                    NEWUSER_CERTIFICATE_NAME_EDIT_W, NEWUSER_CERTIFICATE_NAME_EDIT_H);
#else
                mNameWidget.Resize(NEWUSER_CERTIFICATE_NAME_EDIT_X, NEWUSER_CERTIFICATE_NAME_EDIT_Y + mY,
                    NEWUSER_CERTIFICATE_NAME_EDIT_W, NEWUSER_CERTIFICATE_NAME_EDIT_H);
#endif
            }
        }

        public override void Update()
        {
#if !PEGGLE_IPHONE
            if (mCertificate)
            {
                mNameWidget.mY = NEWUSER_CERTIFICATE_NAME_EDIT_Y + mY;
            }
#endif
            if (mNeedFocus && !IsScrolling())
            {
                if (mFocusDelay == 0)
                {
                    mWidgetManager?.SetFocus(mNameWidget);
                    mNeedFocus = false;
                }
                else
                {
                    mFocusDelay--;
                }
            }
            base.Update();
            if (mShowApproval && mStampCounter < 1)
            {
                mStampCounter = 1;
                mApp.PlaySample(Res.SOUND_STAMP);
            }
            if (mStampCounter != 0)
            {
                mStampCounter++;
                if (mStampCounter == 0x82)
                {
                    if (mApp.mPlayerInfo == null)
                    {
                        mStampCounter = 0;
                    }
                    else
                    {
                        mApp.KillDialog(DialogId.DIALOG_USER);
                        mApp.DoScrollOff(mId);
                        if (mApp.mMainMenu == null)
                        {
                            mApp.ShowMainMenu();
                        }
                        else
                        {
                            mWidgetManager?.SetFocus(mApp.mMainMenu);
                        }
                    }
                }
            }
        }

        public override void Draw(Graphics g)
        {
            if (!mCertificate)
            {
                base.Draw(g);
#if PEGGLE_IPHONE
                EditDialog.DrawEditBox(g, mNameWidget, 0);
#else
                SexyUtil.DrawEditBox(g, mNameWidget, this);
#endif
                return;
            }
            g.DrawImage(Res.IMAGE_CERTIFICATE, 0, 0);
            g.SetColor(SexyUtil.MakeColor(0x80));
            g.SetFont(SexyUtil.GetFontById((int)ResourceId.FONT_PLAIN_ID));
            g.DrawString(mDateString, NEWUSER_CERTIFICATE_DATE_X - g.GetFont().StringWidth(mDateString) / 2, NEWUSER_CERTIFICATE_DATE_Y);
            if (mStampCounter > 0)
            {
                DeferOverlay(0);
            }
        }

        public override void DrawOverlay(Graphics g)
        {
            if (mStampCounter <= 0)
            {
                return;
            }
            const int growTime = 0x14;
            float scale = 1.0f;
            if (mStampCounter < growTime)
            {
                scale = (1.0f - mStampCounter / (float)growTime) * 2.0f + 1.0f;
            }
            Image stamp = mApp.mNightsMode ? Res.IMAGE_MM_NIGHTS_APPROVEDSTAMP : Res.IMAGE_MM_APPROVEDSTAMP;
            int w = (int)(stamp.mWidth * scale);
            int h = (int)(stamp.mHeight * scale);
            g.DrawImage(stamp, new TRect(NEWUSER_APPROVED_STAMP_X - w / 2, NEWUSER_APPROVED_STAMP_Y - h / 2, w, h), new TRect(0, 0, stamp.mWidth, stamp.mHeight));
        }

        public void EditWidgetText(int theId, string theString)
        {
            mApp.DialogButtonDepress(mId, 1000);
        }

        public bool AllowChar(int theId, char theChar)
        {
            if (theChar == '^')
            {
                return false;
            }
            return (theChar < 0x80 && char.IsLetterOrDigit(theChar)) || theChar == ' ';
        }

        public bool AllowText(int theId, string theText)
        {
            return true;
        }

        public bool AllowClear(int theId)
        {
            return true;
        }

        /// <summary>The typed name with runs of spaces collapsed and no leading/trailing space.</summary>
        public string GetName()
        {
            char prev = ' ';
            StringBuilder sb = new StringBuilder();
            foreach (char c in mNameWidget.mString)
            {
                if (c == ' ')
                {
                    if (prev != ' ')
                    {
                        sb.Append(' ');
                    }
                }
                else
                {
                    sb.Append(c);
                }
                prev = c;
            }
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
            {
                sb.Length--;
            }
            return sb.ToString();
        }

        public void SetName(string theName)
        {
            mNameWidget.SetText(theName);
        }

        public void DoApproved()
        {
            mShowApproval = true;
        }
    }
}
