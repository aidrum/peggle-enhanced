using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// "Peggle Wisdom" popup shown before Adventure levels (Sexy::StageDialog): the level's story
    /// text beside the stage master's portrait, or a level icon. On the first two levels of the
    /// adventure it starts with a tutorial page (wheel/fire, then double-tap zoom) that scrolls up
    /// to the story page when Next is pressed.
    /// </summary>
    public class StageDialog : ThunderDialog
    {
        public ThunderballApp mApp;                               // 0x128

        public Board mBoard;                                      // 0x12c

        public int mTextHeight;                                   // 0x130

        public bool mShowIcon;                                    // 0x134

        public int mPage;                                         // 0x138

        public int mScrollTime;                                   // 0x13c

        public int mScrollCount;                                  // 0x140

        public StageDialog(Board theBoard)
            : base(DialogId.DIALOG_STAGE, false, SexyAppBase.Loc("Peggle Wisdom"), "", SexyAppBase.Loc("Ok"), 3)
        {
            mCloseOnButton = true;
            mDrawLinesOnly = true;
            mBoard = theBoard;
            mApp = mBoard.mApp;
            mShowIcon = true;
            mWidth = mApp.mWidth - STAGE_DIALOG_OFFSET_W;
            mAutoSizeYesButton = true;
            LevelInfo aLevel = mApp.GetCurLevelInfo();
            if (aLevel != null && aLevel.mDialogTitle.Length != 0)
            {
                mDialogHeader = aLevel.mDialogTitle;
            }
            mScrollTime = 0;
            mScrollCount = 0;
            if (aLevel != null && aLevel.mStage == mApp.mPlayerInfo.mFirstAdventureStage && aLevel.mLevel < 2)
            {
                mPage = 1;
                mYesButton.mLabel = SexyAppBase.Loc("Next");
            }
            else
            {
                mPage = 0;
            }
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return STAGE_DIALOG_H;
        }

#if PEGGLE_IPHONE
        // The iPhone frame is as thick at the top and bottom as at the sides.
        private const int STAGE_DIALOG_ICON_FRAME_H = 0; // unused: FrameRect uses the width
#endif

        private static TRect FrameRect(int theX, int theY, int theW, int theH, int theFrameH)
        {
#if PEGGLE_IPHONE
            theFrameH = STAGE_DIALOG_ICON_FRAME_W;
#endif
            return new TRect(theX - STAGE_DIALOG_ICON_FRAME_W, theY - theFrameH, theW + STAGE_DIALOG_ICON_FRAME_W * 2, theH + theFrameH * 2);
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            int aStage = mApp.mCurStage - mApp.mPlayerInfo.mFirstAdventureStage;
            LevelInfo aLevel = mApp.GetCurLevelInfo();
            int anOffset = mPage * mHeight;
            if (mScrollCount != mScrollTime)
            {
                anOffset = (int)((float)anOffset + (1.0f - (float)mScrollCount / (float)mScrollTime) * (float)mHeight);
            }
            g.PushState();
            g.mClipRect = new TRect(0, STAGE_DIALOG_CLIP_Y, SCREEN_WIDTH, SCREEN_HEIGHT);
            g.Translate(0, -anOffset);
            if (aLevel.mStage == mApp.mPlayerInfo.mFirstAdventureStage && aLevel.mLevel < 2)
            {
                int aPageY = mHeight;
                string aText;
                Image anIcon;
                if (aLevel.mLevel == 0)
                {
                    aText = SexyAppBase.Loc("\nUse the ^64D8FD^WHEEL^oldclr^ to adjust your ^FFE078^AIM.^oldclr^\n\nPress ^FC920A^FIRE^oldclr^ to shoot the ball.");
                    anIcon = Res.IMAGE_DLG_INSTRUCTIONS_WHEEL;
                }
                else
                {
                    aText = SexyAppBase.Loc("^FC920A^PRECISION!^oldclr^\n\n^64D8FD^DOUBLE TAP^oldclr^ the screen to ^FFE078^ZOOM IN!^oldclr^");
                    anIcon = Res.IMAGE_DLG_INSTRUCTIONS_ZOOM;
                }
                int w = STAGE_DIALOG_ICON_CLIP_W;
                int h = STAGE_DIALOG_ICON_CLIP_H;
                int x = STAGE_DIALOG_ICON_CLIP_X;
                int y = STAGE_DIALOG_ICON_CLIP_Y + aPageY;
                g.DrawImage(anIcon, x, y, w, h);
                g.DrawImageBox(FrameRect(x, y, w, h, STAGE_DIALOG_ICON_FRAME_H), Res.IMAGE_DLG_SELECTORFRAME);
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                g.SetFont(Res.FONT_PLAINOUTLINE);
                WriteWordWrapped(g, new TRect(STAGE_DIALOG_TEXT_RECT_X, aPageY + STAGE_DIALOG_TEXT_RECT_Y, STAGE_DIALOG_TEXT_RECT_W, STAGE_DIALOG_TEXT_RECT_H), aText, -1, -1);
            }
            int aChar = SexyUtil.GetAdventureCharacter(aStage, ThunderballApp.mGameLevelPack);
            // Port: the WoW Adventure's masters have their WoW portraits.
            Image aPortrait = SexyUtil.GetWoWPortrait(aChar) ??
                SexyUtil.GetImageById(ThunderballApp.mGameLevelPack == 0 ? (int)ResourceId.IMAGE_CHR_CHARACTER01_ID + aChar : (int)ResourceId.IMAGE_CHR_NIGHTS_CHARACTER01_ID + aChar);
            TRect aTextRect = new TRect(STAGE_DIALOG_TEXT_RECT_X, STAGE_DIALOG_TEXT_RECT_Y, STAGE_DIALOG_TEXT_RECT_W, STAGE_DIALOG_TEXT_RECT_H);
            int anIconLevels = ThunderballApp.mGameLevelPack != 0 ? 3 : 4;
            bool aLevelIcon = aLevel != null && aLevel.mLevel < anIconLevels && aLevel.mStage == mApp.mPlayerInfo.mFirstAdventureStage;
            if (mShowIcon && !aLevelIcon)
            {
                int x = STAGE_DIALOG_ICON_CLIP_X;
                int y = STAGE_DIALOG_ICON_CLIP_Y;
                g.DrawImage(aPortrait, x, y);
                g.DrawImageBox(FrameRect(x, y, aPortrait.mWidth, aPortrait.mHeight, STAGE_DIALOG_ICON_FRAME_H), Res.IMAGE_DLG_SELECTORFRAME);
            }
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.SetFont(Res.FONT_PLAINOUTLINE);
            mTextHeight = WriteWordWrapped(g, aTextRect, aLevel.mDialogText, -1, -1);
            if (aLevelIcon)
            {
                int anIndex = aLevel.mLevel;
                if (ThunderballApp.mGameLevelPack == 1 && anIndex > 0)
                {
                    anIndex++;
                }
                Image anIcon = SexyUtil.GetImageById((int)ResourceId.IMAGE_DLG_INSTRUCTIONS_ID + anIndex);
                int x = STAGE_DIALOG_ICON_CLIP_X;
                int y = STAGE_DIALOG_ICON_CLIP_Y;
                g.DrawImage(anIcon, x, y);
                g.DrawImageBox(FrameRect(x, y, anIcon.mWidth, anIcon.mHeight, STAGE_DIALOG_ICON_FRAME_W), Res.IMAGE_DLG_SELECTORFRAME);
            }
            g.Translate(0, anOffset);
            g.PopState();
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
        }

        public override void KeyChar(SexyChar theChar)
        {
        }

        public override void Update()
        {
            base.Update();
            if (mScrollCount < mScrollTime)
            {
                mScrollCount++;
                if (mScrollCount == mScrollTime)
                {
                    mYesButton.SetVisible(true);
                }
            }
        }

        public override void ButtonDepress(int theId)
        {
            WidgetManager aManager = mWidgetManager;
            if (theId != 1000)
            {
                return;
            }
            if (mPage == 0)
            {
                base.ButtonDepress(theId);
                mBoard.mShowingGameTip = false;
                mBoard.mCharMoveCount = 0x14;
                CharacterMgr.DoCharMove(mX + 0x3c, mY + 0x6e, true);
                aManager?.SetFocus(mBoard);
            }
            else
            {
                mPage--;
                mYesButton.SetVisible(false);
                mYesButton.mLabel = SexyAppBase.Loc("Ok");
                mScrollCount = 0;
                mScrollTime = 0x19;
            }
        }
    }
}
