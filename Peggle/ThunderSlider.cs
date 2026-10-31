using System;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Labelled volume slider (Sexy::ThunderSlider): a Slider whose orb runs along a lit track drawn
    /// over the IMAGE_DLG_SLIDER plate, with the label centred on the plate's button area.
    /// </summary>
    public class ThunderSlider : Slider
    {
        private static readonly Color aDisabledColor = new Color(100, 100, 100);

        public string mLabel = "";                                // 0xa4

        public ThunderSlider(string theLabel, int theId, SliderListener theListener)
            : base(null, Res.IMAGE_DLG_SLIDERORB, theId, theListener)
        {
            mLabel = theLabel;
            mClip = false;
        }

        public override void Draw(Graphics g)
        {
            int x = THUNDER_BUTTON_SLIDER_X;
            int y = THUNDER_BUTTON_SLIDER_Y;
            if (mDisabled)
            {
                g.SetColorizeImages(true);
                g.SetColor(aDisabledColor);
            }
            g.DrawImage(Res.IMAGE_DLG_SLIDER, x, y);
            g.SetFont(Res.FONT_BUTTON);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            SexyUtil.SetButtonFontColor(0xd, 1.0f);
            Font font = g.GetFont();
            int textX = THUNDER_BUTTON_TEXT_OFFSET_X + x;
            y = font.GetAscent() + (Res.IMAGE_DLG_SLIDER.mHeight - font.GetHeight()) / 2 + y;
            g.DrawString(mLabel, textX - font.StringWidth(mLabel) / 2, y);
            if (mDisabled)
            {
                g.SetColor(aDisabledColor);
            }
            Image hilight = Res.IMAGE_DLG_SLIDERHILIGHT;
            int halfThumb = mThumbImage.mWidth / 2;
            int litWidth = Math.Clamp((int)((hilight.mWidth - halfThumb * 2) * mVal + halfThumb), 0, hilight.mWidth);
            g.DrawImage(hilight, THUNDER_BUTTON_SLIDER_OFFSET_X, THUNDER_BUTTON_SLIDER_OFFSET_Y,
                new TRect(0, 0, litWidth, hilight.mHeight));
            base.Draw(g);
            if (mDisabled)
            {
                g.SetColorizeImages(false);
            }
        }
    }
}
