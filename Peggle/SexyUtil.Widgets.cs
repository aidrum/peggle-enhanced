using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    public static partial class SexyUtil
    {
        /// <summary>
        /// SetButtonFontColor's static table: 17 colour sets of 3 layer colours (fill, highlight,
        /// outline) for FONT_BUTTON. Recovered from the guarded initialiser at 0x109c28.
        /// </summary>
        private static Color[] sButtonFontColors;

        private static Color[] GetButtonFontColors()
        {
            if (sButtonFontColors == null)
            {
                sButtonFontColors = new[]
                {
                    MakeColor(0.34, 0.03, 0.0, 0.8), MakeColor(0xffba02), MakeColor(0.99, 0.37, 0.0, 1.0),
                    MakeColor(0.3, 0.0, 0.05, 0.8), MakeColor(0xfac841), MakeColor(1.0, 0.69, 0.0, 1.0),
                    MakeColor(0.77, 0.08, 0.0, 0.8), MakeColor(0xf6fd42), MakeColor(0.96, 0.7, 0.08, 1.0),
                    MakeColor(0x821c01), MakeColor(0xfff3a3), MakeColor(0xffcc00),
                    MakeColor(0xb32702), MakeColor(0xfffada), MakeColor(0xffe400),
                    MakeColor(0xe55e04), MakeColor(0xffffff), MakeColor(0xfffeac),
                    MakeColor(0.06, 0.37, 0.08, 0.8), MakeColor(0xd3f65c), MakeColor(0.04, 0.87, 0.16, 1.0),
                    MakeColor(0.06, 0.37, 0.08, 0.8), MakeColor(0xd3f65c), MakeColor(0.04, 0.87, 0.16, 1.0),
                    MakeColor(0.32, 0.05, 0.45, 0.8), MakeColor(0xf3b1fd), MakeColor(0.87, 0.33, 1.0, 1.0),
                    MakeColor(0.33, 0.06, 0.32, 0.8), MakeColor(0xf5b3ff), MakeColor(0.82, 0.32, 0.92, 1.0),
                    MakeColor(0.36, 0.12, 0.33, 1.0), MakeColor(0xfb54ea), MakeColor(0.8, 0.17, 0.72, 1.0),
                    MakeColor(0.38, 0.01, 0.34, 1.0), MakeColor(0xfe94e7), MakeColor(0.88, 0.55, 0.94, 1.0),
                    MakeColor(0.9, 0.2, 0.8, 1.0), MakeColor(0xfdb6f7), MakeColor(1.0, 0.6, 1.0, 1.0),
                    MakeColor(0x1333d8), MakeColor(0x65f3f8), MakeColor(0x4cc4f7),
                    MakeColor(0x1a3bd6), MakeColor(0xffffff), MakeColor(0xd2f7ff),
                    MakeColor(0x0a5dcc), MakeColor(0xc2f6ff), MakeColor(0x7ffaff),
                    MakeColor(0x006a29), MakeColor(0xfffde4), MakeColor(0xf6ff00),
                };
            }
            return sButtonFontColors;
        }

        /// <summary>Recolours FONT_BUTTON's layers; layer 2 (the glow) uses an additive colour.</summary>
        public static void SetButtonFontColor(int theColor, float theAlpha)
        {
            if (!(Res.FONT_BUTTON is ImageFont font))
            {
                return;
            }
            Color[] table = GetButtonFontColors();
            int i = 0;
            foreach (FontLayer layer in font.mFontData.mFontLayerList)
            {
                int idx = theColor * 3 + i;
                Color c = idx >= 0 && idx < table.Length ? table[idx] : Color.White;
                if (i == 2)
                {
                    layer.mColorMult.A = 0;
                    int a = 0x5a;
                    if (theAlpha != 1.0f)
                    {
                        a = (int)(a * theAlpha);
                    }
                    layer.mColorAdd = new Color(c.R, c.G, c.B, (byte)a);
                }
                else
                {
                    int a = c.A;
                    if (theAlpha != 1.0f)
                    {
                        a = (int)(a * theAlpha);
                    }
                    layer.mColorMult = new Color(c.R, c.G, c.B, (byte)a);
                }
                i++;
            }
        }

        /// <summary>Splits a 3-cel button strip (vertical, or horizontal) into normal/over/down rects.</summary>
        public static void SetupButton(DialogButton theButton, bool horizontal)
        {
            if (theButton == null)
            {
                return;
            }
            int w = theButton.mComponentImage.mWidth;
            int h = theButton.mComponentImage.mHeight;
            if (horizontal)
            {
                w /= 3;
                theButton.mNormalRect = new TRect(0, 0, w, h);
                theButton.mOverRect = new TRect(w, 0, w, h);
                theButton.mDownRect = new TRect(w * 2, 0, w, h);
            }
            else
            {
                h /= 3;
                theButton.mNormalRect = new TRect(0, 0, w, h);
                theButton.mOverRect = new TRect(0, h, w, h);
                theButton.mDownRect = new TRect(0, h * 2, w, h);
            }
            theButton.SetFont(Res.FONT_BUTTON);
            theButton.SetColor(0, SexyUtil.MakeColor(0xffffff));
            theButton.SetColor(1, SexyUtil.MakeColor(0xffffff));
        }

        public static ThunderButton MakeButton(int theId, ButtonListener theListener, string theLabel, Image theImage = null)
        {
            if (theImage == null)
            {
                theImage = Res.IMAGE_DLG_BUTTON3;
            }
            ThunderButton button = new ThunderButton(theImage, theId, theListener);
            button.mLabel = theLabel;
            button.mWidth = button.mComponentImage.mWidth;
            button.mHeight = button.mComponentImage.mHeight / 3;
            button.mFontColor = 3;
            button.SetClickSound(Res.SOUND_BUTTON1);
            SetupButton(button, false);
            return button;
        }

        /// <summary>A 2-cel image button (normal + down), optionally a toggle that keeps the down look.</summary>
        public static ThunderButton MakeEmbeddedButton(int theId, ButtonListener theListener, Image theImage, bool isToggle)
        {
            int w = theImage.mWidth;
            int h = theImage.mHeight;
            if (theImage.mNumCols < 2)
            {
                h /= 2;
            }
            else
            {
                w /= 2;
            }
            if (isToggle && theImage.mNumRows == 1 && theImage.mNumCols == 1)
            {
                w = theImage.mWidth;
                h = theImage.mHeight;
            }
            ThunderButton button = new ThunderButton(theImage, theId, theListener);
            button.mWidth = w;
            button.mHeight = h;
            button.mFontColor = ButtonFontColor.NONE;
            button.SetClickSound(Res.SOUND_BUTTON1);
            button.mNormalRect = new TRect(0, 0, w, h);
            if (h == theImage.mHeight)
            {
                button.mOverRect = new TRect(0, 0, w, h);
                button.mDownRect = new TRect(w, 0, w, h);
            }
            else
            {
                button.mOverRect = new TRect(0, 0, w, h);
                button.mDownRect = new TRect(0, h, w, h);
            }
            if (isToggle)
            {
                button.mHighlightOnDown = true;
                button.mDownRect = button.mOverRect;
            }
            return button;
        }

        public static SimpleButton MakeSimpleButton(int theId, ButtonListener theListener, Image theImage, int theExtraWidth, int theExtraHeight)
        {
            SimpleButton button = new SimpleButton(theImage, theId, theListener);
            button.mWidth = theImage.mWidth + theExtraWidth;
            button.mHeight = theImage.mHeight + theExtraHeight;
            return button;
        }

        /// <summary>Style 0: 4-cel IMAGE_DLG_BUTTON1 check box; style 1: 3-cel IMAGE_DLG_BUTTON2 toggle.</summary>
        public static ThunderCheckbox MakeCheckbox(int theId, CheckboxListener theListener, int theStyle)
        {
            ThunderCheckbox checkbox = null;
            int w = 0;
            int h = 0;
            if (theStyle == 0)
            {
                Image image = Res.IMAGE_DLG_BUTTON1;
                w = image.mWidth;
                h = image.mHeight / 4;
                checkbox = new ThunderCheckbox(image, image, theId, theListener);
                checkbox.mUncheckedRect = new TRect(0, 0, w, h);
                checkbox.mOverUncheckedRect = new TRect(0, h, w, h);
                checkbox.mCheckedRect = new TRect(0, h * 2, w, h);
                checkbox.mOverCheckedRect = new TRect(0, h * 3, w, h);
            }
            else if (theStyle == 1)
            {
                Image image = Res.IMAGE_DLG_BUTTON2;
                w = image.mWidth;
                h = image.mHeight / 3;
                checkbox = new ThunderCheckbox(image, image, theId, theListener);
                checkbox.mUncheckedRect = new TRect(0, 0, w, h);
                checkbox.mOverUncheckedRect = new TRect(0, h, w, h);
                checkbox.mCheckedRect = new TRect(0, h * 2, w, h);
            }
            checkbox.mStyle = theStyle;
            checkbox.Resize(0, 0, w, h);
            checkbox.mHasTransparencies = true;
            checkbox.mHasAlpha = true;
            checkbox.SetClickSound(Res.SOUND_BUTTON1);
            return checkbox;
        }

        public static ThunderCheckbox MakeCheckbox2(int theId, CheckboxListener theListener, string theLabel, int theStyle)
        {
            ThunderCheckbox checkbox = MakeCheckbox(theId, theListener, theStyle);
            checkbox.mLabel = theLabel;
            checkbox.mWidth = checkbox.mFont.StringWidth(theLabel);
            checkbox.mWidth += theStyle == 0 ? 0x3c : 0x1e;
            return checkbox;
        }

        public static ThunderListWidget MakeListWidget(int theId, ListListener theListener)
        {
            ThunderListWidget list = new ThunderListWidget(theId, GetFontById(6), theListener);
            list.SetColor(ThunderListWidget.COLOR_BKG, new Color(0, 0, 0, 0));
            list.SetColor(ThunderListWidget.COLOR_OUTLINE, MakeColor(0x800000));
            list.SetColor(ThunderListWidget.COLOR_TEXT, MakeColor(0xffff50));
            list.SetColor(ThunderListWidget.COLOR_HILITE, MakeColor(0xffffff));
            list.SetColor(ThunderListWidget.COLOR_SELECT, MakeColor(0x43f4));
            list.mDrawOutline = false;
            list.mItemHeight = DIALOG_LIST_ITEM_HEIGHT;
            return list;
        }

        public static ThunderCheckbox MakeRadioButton(int theId, CheckboxListener theListener, string theLabel, int theStyle)
        {
            ThunderCheckbox checkbox = MakeCheckbox2(theId, theListener, theLabel, theStyle);
            checkbox.mIsRadio = true;
            return checkbox;
        }

        public static void SetupEditWidget(EditWidget theWidget, bool isCertificate)
        {
            theWidget.SetFont(Res.FONT_PLAIN);
            theWidget.SetColor(0, new Color(0, 0, 0, 0));
            theWidget.SetColor(1, new Color(0, 0, 0, 0));
            theWidget.SetColor(2, MakeColor(0));
            theWidget.SetColor(3, MakeColor(0x80));
            theWidget.SetColor(4, MakeColor(0xffffff));
            theWidget.mReturnKeyType = 9;
            theWidget.mHeight = DIALOG_EDIT_ITEM_HEIGHT;
            if (!isCertificate)
            {
                theWidget.mHeight += DIALOG_SAVE_REPLAY_EDIT_EXT_CHAR_OFFSET_FIX;
            }
            theWidget.mAutoCapitalization = 1;
        }

        public static EditWidget CreateEditWidget(int theId, EditListener theListener, bool isCertificate)
        {
            EditWidget widget = new EditWidget(theId, theListener);
            SetupEditWidget(widget, isCertificate);
            return widget;
        }

        /// <summary>Draws the text-box frame around an EditWidget, in theDialog's coordinates.</summary>
        public static void DrawEditBox(Graphics g, EditWidget theWidget, Widget theDialog)
        {
            int x = theWidget.mX - theDialog.mX - DIALOG_EDIT_BOX_PAD_X;
            int y = theWidget.mY - theDialog.mY - DIALOG_EDIT_BOX_PAD_Y - DIALOG_SAVE_REPLAY_EDIT_EXT_CHAR_OFFSET_FIX + 10;
            g.DrawImageBox(new TRect(x, y, theWidget.mWidth + DIALOG_EDIT_BOX_PAD_W, theWidget.mHeight + DIALOG_EDIT_BOX_PAD_H), Res.IMAGE_DLG_TEXTBOX);
        }

        /// <summary>MakeButtonCopy: replaces a framework DialogButton with an equivalent ThunderButton.</summary>
        public static DialogButton MakeButtonCopy(DialogButton theButton)
        {
            if (theButton == null)
            {
                return null;
            }
            ThunderButton copy = MakeButton(theButton.mId, theButton.mButtonListener, theButton.mLabel, null);
            copy.Resize(theButton.mX, theButton.mY, theButton.mWidth, theButton.mHeight);
            theButton.Dispose();
            return copy;
        }
    }
}
