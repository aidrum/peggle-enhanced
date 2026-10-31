using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// ScrollWidget with Peggle's scroll indicator and tinted top/bottom fade shadows
    /// (Sexy::ThunderScrollWidget).
    /// </summary>
    public class ThunderScrollWidget : ScrollWidget
    {
        public int mShadowColor;                                  // 0x13c

        public bool mDrawTopShadow;                               // 0x140

        public bool mDrawBottomShadow;                            // 0x141

        public ThunderScrollWidget()
        {
            mShadowColor = 0x100840;
            mDrawTopShadow = true;
            mDrawBottomShadow = true;
            EnableIndicators(Res.IMAGE_DLG_SCROLL_INDICATOR);
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            DeferOverlay(0);
        }

        public override void DrawOverlay(Graphics g)
        {
            TRect oldClip = g.GetClipRect();
            g.SetClipRect(0, 0, mWidth, mHeight);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(mShadowColor));
            WidgetContainer parent = mParent;
            Image top = Res.IMAGE_DLG_SCROLLSHADOW_TOP;
            if (mDrawTopShadow)
            {
                g.DrawImage(top, parent.mX, parent.mY - THUNDER_SCROLL_WIDGET_CORNER_ALPHA_OFFSET_Y, mWidth, top.mHeight);
            }
            if (mDrawBottomShadow)
            {
                Image bottom = Res.IMAGE_DLG_SCROLLSHADOW_BOTTOM;
                // The original sizes the bottom shadow by the top image's height.
                g.DrawImage(bottom, parent.mX, THUNDER_SCROLL_WIDGET_CORNER_ALPHA_OFFSET_Y + parent.mY + mHeight - bottom.mHeight,
                    mWidth, top.mHeight);
            }
            g.SetColorizeImages(false);
            g.SetClipRect(oldClip);
        }
    }
}
