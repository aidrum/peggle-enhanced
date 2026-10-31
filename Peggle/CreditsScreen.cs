using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Credits screen (Sexy::CreditsScreen, id 44): a full-screen ThunderDialog with Master Hu,
    /// "Hu Made It" and the scrolling CreditsWidget, plus a blinking BACK button.
    /// </summary>
    public class CreditsScreen : ThunderDialog
    {
        public ThunderballApp mApp;                               // 0x128

        public ThunderButton mBackButton;                         // 0x12c

        public CreditsWidget mCreditsWidget;                      // 0x130

        public ThunderScrollWidget mScrollWidget;                 // 0x134

        // 0019c540
        public CreditsScreen(ThunderballApp theApp)
            : base(DialogId.DIALOG_CREDITS, true, "", "", "", 0)
        {
            mApp = theApp;
            mCreditsWidget = new CreditsWidget(theApp);
            mScrollWidget = new ThunderScrollWidget();
            AddWidget(mScrollWidget);
            mScrollWidget.AddWidget(mCreditsWidget);
            Image orange = Res.IMAGE_BUTTON_ORANGE;
            int w = orange.mWidth;
            int h = orange.mHeight;
            mBackButton = new ThunderButton(orange, 0, this);
            mBackButton.mOverlayText = SexyAppBase.Loc("BACK"); // 0x138
            mBackButton.mButtonImage = orange;                  // 0x88
            mBackButton.mComponentImage = orange;               // 0xf8
            mBackButton.mWidth = w;
            mBackButton.mHeight = h / 2;
            mBackButton.mNormalRect = new TRect(0, 0, w, h / 2);
            mBackButton.mOverRect = new TRect(0, 0, w, h / 2);
            mBackButton.mDownRect = new TRect(0, h / 2, w, h / 2);
            AddWidget(mBackButton);
            mBackButton.Blink(1000, true);
        }

        // 0019cbd0
        public override void Dispose()
        {
            RemoveAllWidgets(true, true);
            base.Dispose();
        }

        // 0019ccf0
        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int top = mContentInsets.mTop;
            int scrollY = CREDITS_SCR_SCROLL_WIDGET_Y;
            int scrollH = CREDITS_SCR_SCROLL_WIDGET_H;
            int x = CREDITS_SCR_SCROLL_WIDGET_X + mContentInsets.mLeft;
            int w = mWidth - CREDITS_SCR_SCROLL_WIDGET_W - x;
            mCreditsWidget.Resize(0, 0, w, mCreditsWidget.GetPreferredHeight());
            mScrollWidget.Resize(x, scrollY + top, w, scrollH - (scrollY + top));
            mBackButton.Move(CREDITS_SCR_DONE_BTN_X, CREDITS_SCR_DONE_BTN_Y);
        }

        // 0019ce40
        public override void ButtonDepress(int theId)
        {
            mApp.DoToMenu();
        }

        // 0019ce78
        public override void Draw(Graphics g)
        {
            base.Draw(g);
#if !PEGGLE_IPHONE
            g.DrawImage(Res.IMAGE_HU_LARGE, CREDITS_SCR_HU_IMG_X, CREDITS_SCR_HU_IMG_Y);
#endif
            TRect rect = mScrollWidget.GetRect();
            rect.Inflate(6, 6);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0, 0x7f));
            SexyUtil.MyDrawImageBox(g, rect, Res.IMAGE_DLG_TEXTRECT);
            g.SetColorizeImages(false);
            g.SetColor(SexyUtil.MakeColor(0x63f7ff));
            g.SetFont(mHeaderFont);
            int y = mBackgroundInsets.mTop + mContentInsets.mTop;
            y = mHeaderFont.GetAscent() - mHeaderFont.GetAscentPadding() + y;
            string text = SexyAppBase.Loc("Hu Made It");
#if PEGGLE_IPHONE
            // Left-aligned, with Hu drawn last (over the text box).
            g.DrawString(text, CREDITS_SCR_HU_TEXT_X, y);
            // Port addition (ultrawide): Hu stays in the window's corner when it is wider than the game.
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            aWide.DrawImage(Res.IMAGE_HU_LARGE, CREDITS_SCR_HU_IMG_X + GetLeftCornerShift(mWidth), CREDITS_SCR_HU_IMG_Y);
#else
            g.DrawString(text, CREDITS_SCR_HU_TEXT_X - mHeaderFont.StringWidth(SexyAppBase.Loc("Hu Made It")) / 2, y);
#endif
        }
    }
}
