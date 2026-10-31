namespace Sexy
{
    /// <summary>
    /// "Continue Game?" dialog (Sexy::ContinueDialog, id 25). Offers Continue / New Game (or
    /// Continue / Restart Level in Adventure) for a saved game in progress, plus Cancel.
    /// </summary>
    public class ContinueDialog : ThunderDialog
    {
        public ThunderballApp mApp;                               // 0x128

        public ThunderButton mContinueButton;                     // 0x12c

        public ThunderButton mNewGameButton;                      // 0x130

        public ContinueDialog(ThunderballApp theApp, string theText)
            : base(DialogId.DIALOG_CONTINUE, true, SexyAppBase.Loc("Continue Game?"), theText, SexyAppBase.Loc("Cancel"), 3)
        {
            mApp = theApp;
            mTextAlign = -1;                                      // 0xa8
            mCloseOnButton = true;                                // 0x10b
            if (theApp.mGameMode == GameMode.GAMEMODE_ADVENTURE || theApp.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                mContinueButton = SexyUtil.MakeButton(0, this, SexyAppBase.Loc("Continue"));
                mNewGameButton = SexyUtil.MakeButton(1, this, SexyAppBase.Loc("Restart Level"));
            }
            else
            {
                mContinueButton = SexyUtil.MakeButton(0, this, SexyAppBase.Loc("Continue"));
                mNewGameButton = SexyUtil.MakeButton(1, this, SexyAppBase.Loc("New Game"));
            }
        }

        public override void Dispose()
        {
            mContinueButton?.Dispose();
            mNewGameButton?.Dispose();
            base.Dispose();
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return Sexy.Layout.SCREEN_HEIGHT;
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int w = (mWidth - mContentInsets.mLeft - mContentInsets.mRight - mBackgroundInsets.mLeft - mBackgroundInsets.mRight
                     - mButtonSidePadding * 2 - mButtonHorzSpacing) / 2;
            int h = Res.IMAGE_DLG_BUTTON3.mHeight / 3;
            int yesY = mYesButton.mY;
            mYesButton.Resize(mX + mWidth / 2 - w / 2, mYesButton.mY, w, mYesButton.mHeight);
            mContinueButton.Resize(mX + mBackgroundInsets.mLeft + mContentInsets.mLeft + mButtonSidePadding, yesY - h - 5, w, h);
            mNewGameButton.Resize(mContinueButton.mX + w + mButtonHorzSpacing, mContinueButton.mY, w, h);
        }

        public override void AddedToManager(WidgetManager theWidgetManager)
        {
            base.AddedToManager(theWidgetManager);
            theWidgetManager.AddWidget(mContinueButton);
            theWidgetManager.AddWidget(mNewGameButton);
        }

        public override void RemovedFromManager(WidgetManager theWidgetManager)
        {
            base.RemovedFromManager(theWidgetManager);
            theWidgetManager.RemoveWidget(mContinueButton);
            theWidgetManager.RemoveWidget(mNewGameButton);
        }

        public override void ButtonDepress(int theId)
        {
            if (theId == 0)
            {
                mApp.KillDialog(this);
                mApp.FinishContinueDialog(true);
            }
            else if (theId == 1)
            {
                mApp.KillDialog(this);
                mApp.FinishContinueDialog(false);
            }
            else
            {
                mApp.DoScrollOff(mId);
            }
        }
    }
}
