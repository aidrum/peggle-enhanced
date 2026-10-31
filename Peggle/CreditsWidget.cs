using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// The scrolling credits text (Sexy::CreditsWidget): the lines of stage 10's second story
    /// text after the first " " marker line, headings (param 1) in yellow.
    /// </summary>
    public class CreditsWidget : Widget
    {
        public ThunderballApp mApp;                               // 0x78

        public StageInfo mStageInfo;                              // 0x7c

        // 0019c104
        public CreditsWidget(ThunderballApp theApp)
        {
            mApp = theApp;
            mStageInfo = mApp.mStageMgr.GetStageInfo(10);
        }

        // 0019c31c
        public int GetPreferredHeight()
        {
            int lineH = Res.FONT_OVERLOAD16OUTLINE.mHeight;
            List<StoryData> lines = mStageInfo.mStoryText[1];     // stage + 0x14
            int h = Res.FONT_OVERLOAD16OUTLINE.mHeight;
            bool started = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (started)
                {
                    h += lineH - 2;
                }
                else if (lines[i].mText == " ")
                {
                    started = true;
                }
            }
            return h;
        }

        // 0019c3c4
        public override void Draw(Graphics g)
        {
            List<StoryData> lines = mStageInfo.mStoryText[1];     // stage + 0x14
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
            int y = g.GetFont().mHeight;
            int step = Res.FONT_OVERLOAD16OUTLINE.mHeight - 2;
            bool started = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!started)
                {
                    if (lines[i].mText == " ")
                    {
                        started = true;
                    }
                }
                else
                {
                    int color = lines[i].mParam == 1 ? 0xffff00 : 0xffff88;
                    g.SetColor(SexyUtil.MakeColor(color));
                    WriteCenteredLine(g, y, lines[i].mText);
                    y += step;
                }
            }
        }
    }
}
