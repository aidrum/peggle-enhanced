using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The level-select details popup (Sexy::LevelDetailsDialog): level thumbnail, ribbons, ace
    /// score and the top five high scores (or a mode description in Duel and Challenge).
    /// </summary>
    public class LevelDetailsDialog : ThumbnailDialog
    {
        private static int aStringWidth;

        public List<HighScoreEntry> mScores;                      // 0x1b4

        public bool mCleared;                                     // 0x1b8

        public bool mAced;                                        // 0x1b9

        public string mAceScore = "";                             // 0x1bc

        public string mAceLabel = "";                             // 0x1c0

        public ThunderballApp mApp;                               // 0x1c4

        public int mStage;                                        // 0x1c8

        public int mLevel;                                        // 0x1cc

        public LevelDetailsDialog(ThunderballApp theApp, ThumbnailDialogListener theListener)
            : base(DialogId.DIALOG_LEVEL_DETAILS, theListener)
        {
            mApp = theApp;
            mScores = null;
        }

        public override void DrawContent(Graphics g)
        {
            bool isDuel = mApp.mGameMode == GameMode.GAMEMODE_DUEL;
            bool isChallenge = mApp.mGameMode == GameMode.GAMEMODE_CHALLENGE;
            bool isQuickPlay = !isDuel && !isChallenge;
            if (!isQuickPlay)
            {
                mAceScore = "";
            }
            base.DrawContent(g);
            SexyUtil.DrawLevelThumbnail(g, mStage, mLevel, mThumbRect);
            DrawThumbFrame(g);
            if (mAceScore.Length != 0)
            {
                g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
                g.SetColor(SexyUtil.MakeColor(0x88ff88, 0xff));
#if PEGGLE_IPHONE
                g.DrawString(mAceScore, mThumbRect.mX + (mThumbRect.mWidth - g.GetFont().StringWidth(mAceScore)) / 2,
                    mThumbRect.mY + mThumbRect.mHeight + g.GetFont().GetLineSpacing());
#else
                g.DrawString(mAceLabel, mThumbRect.mX + (mThumbRect.mWidth - g.GetFont().StringWidth(mAceLabel)) / 2,
                    mThumbRect.mY + mThumbRect.mHeight + g.GetFont().GetLineSpacing());
                g.DrawString(mAceScore, mThumbRect.mX + (mThumbRect.mWidth - g.GetFont().StringWidth(mAceScore)) / 2,
                    g.GetFont().GetLineSpacing() + mThumbRect.mY + mThumbRect.mHeight + Res.FONT_OVERLOAD12OUTLINE.GetMaxCharHeight());
#endif
            }
            int aRibbons = 1;
            if (mCleared)
            {
                g.DrawImage(Res.IMAGE_TINYBLUERIBBON, mThumbRect.mX + mThumbRect.mWidth - Res.IMAGE_TINYBLUERIBBON.mWidth,
                    mThumbRect.mY + mThumbRect.mHeight - Res.IMAGE_TINYBLUERIBBON.mHeight);
                aRibbons++;
            }
            if (mAced)
            {
                g.DrawImage(Res.IMAGE_TINYACEDRIBBON, mThumbRect.mX + mThumbRect.mWidth - Res.IMAGE_TINYACEDRIBBON.mWidth * aRibbons,
                    mThumbRect.mY + mThumbRect.mHeight - Res.IMAGE_TINYACEDRIBBON.mHeight);
            }
            if (!isQuickPlay)
            {
                g.SetColor(SexyUtil.MakeColor(0x88ff88, 0xff));
                g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
                string aText;
                if (!isDuel)
                {
                    aText = SexyAppBase.Loc("Pick a level for the challenge!");
                }
                else
                {
                    aText = SexyAppBase.Loc("Face off against a friend or the computer in this two player game mode!");
                }
                TRect aRect = mContentRect;
                aRect.Offset(-10, 0x1e);
                WriteWordWrapped(g, aRect, aText, -1, 0);
                return;
            }
            g.SetFont(Res.FONT_OVERLOAD10);
            if (mScores == null)
            {
                return;
            }
            int y = mContentRect.mY + LEVEL_DETAILS_DLG_HIGHSCORE_CAPTION_OFFSET_Y;
            int aRankX = mContentRect.mX;
            int aNameX = aRankX + LEVEL_DETAILS_DLG_HIGHSCORE_TOPFIVE_NAME_OFFSET_X;
            int aScoreX = aNameX + LEVEL_DETAILS_DLG_HIGHSCORE_TOPFIVE_SCORE_OFFSET_X;
            g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
            g.SetColor(SexyUtil.MakeColor(0x88ff88, 0xff));
            string aCaption = SexyAppBase.Loc("Top Five Scores");
            if (aStringWidth < 1)
            {
                aStringWidth = g.GetFont().StringWidth(aCaption);
            }
            g.DrawString(aCaption, mContentRect.mX + (mContentRect.mWidth / 2 - aStringWidth / 2), y);
            y += LEVEL_DETAILS_DLG_HIGHSCORE_TOPFIVE_GAP_Y;
            g.SetFont(SexyUtil.GetFontById(4));
            int aLineHeight = g.GetFont().GetHeight();
            int aRank = 1;
            Graphics aClipG = Graphics.GetNew(g);
            aClipG.ClipRect(0, 0, aScoreX - LEVEL_DETAILS_DLG_HIGHSCORE_TOPFIVE_RIGHT_CLIP_X, mHeight);
            foreach (HighScoreEntry anEntry in mScores)
            {
                if (aRank > 5)
                {
                    break;
                }
                int aHue = aRank * 10 % 0x100;
                if (aHue > 0x80)
                {
                    aHue = 0x100 - aHue;
                }
                int aColor = GlobalStaticVars.gSexyAppBase.HSLToRGB(aHue, 0xff, 0x80);
                g.SetFont(SexyUtil.GetFontById(4));
                g.SetColor(SexyUtil.MakeColor(aColor, 0xff));
                g.DrawString(aRank + ".", aRankX, y);
                aClipG.SetColor(SexyUtil.MakeColor(aColor, 0xff));
                aClipG.DrawString(SexyUtil.GetLocalizedPlayerName(anEntry.mName), aNameX, y);
                string aScore = SexyUtil.CommaSeperate(anEntry.mScore);
                g.SetFont(Res.FONT_OVERLOAD10);
                int aScoreWidth = g.GetFont().StringWidth(aScore);
                g.SetColor(SexyUtil.MakeColor(aColor, 0xff));
                g.DrawString(aScore, aScoreX - aScoreWidth, y);
                y += aLineHeight;
                aRank++;
            }
            aClipG.PrepareForReuse();
        }
    }
}
