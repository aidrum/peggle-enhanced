using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The replay control strip beside the board after a shot (Sexy::ReplayDialog, id 22): Play
    /// (replay the last shot; held, it toggles stepping through every shot), Save and Stop/Done.
    /// </summary>
    public class ReplayDialog : ThunderDialog, DialogListener
    {
        public const int REPLAY_BUTTON_PLAY = 0;

        public const int REPLAY_BUTTON_SAVE = 1;

        public const int REPLAY_BUTTON_DONE = 2;

        public const int DIALOG_SAVE_REPLAY = 30;

        // 0x128: DialogListener sub-object

        public Board mBoard;                                      // 0x12c

        public ThunderButton mPlayButton;                         // 0x130

        public ThunderButton mFFwdButton;                         // 0x134 (the Save button)

        public ThunderButton mDoneButton;                         // 0x138

        /// <summary>Which recorded shot plays next when stepping through all of them, or -1.</summary>
        public int mReplayIndex;                                  // 0x13c

        public bool mFlag140;                                     // 0x140

        public bool mMultiple;                                    // 0x141

        public bool mIsReplay;                                    // 0x142

        public ReplayDialog(Board theBoard) : base(DialogId.DIALOG_REPLAY, true, "", "", "", 0)
        {
            mDialogListener = this;
            mPriority = 0;                                        // 0x40
            mBoard = theBoard;
            int[] anImageIds = { (int)ResourceId.IMAGE_IP_PLAYBUTTON_ID, (int)ResourceId.IMAGE_IP_SAVEBUTTON_ID, (int)ResourceId.IMAGE_IP_STOPBUTTON_ID };
            ThunderButton[] aButtons = new ThunderButton[3];
            for (int i = 0; i < 3; i++)
            {
                aButtons[i] = SexyUtil.MakeEmbeddedButton(i, this, SexyUtil.GetImageById(anImageIds[i]), false);
#if !PEGGLE_IPHONE
                // HD draws them in the Ballotron slot (InterfaceMgr.DrawReplayCtrls); the iPhone buttons draw themselves.
                aButtons[i].SetDraw(false);
#endif
                AddWidget(aButtons[i]);
            }
            mPlayButton = aButtons[0];
            mFFwdButton = aButtons[1];
            mDoneButton = aButtons[2];
            mFlag140 = false;
            mReplayIndex = -1;
            mCloseOnButton = false;                               // 0x10a
            mIsReplay = false;
            SetMode(false, false, true);
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        public void SetMode(bool isReplay, bool multiple, bool reset)
        {
            mIsReplay = isReplay;
            mFlag140 = false;
            if (reset || mReplayIndex < 0 || mBoard.mReplayWriters.Count <= mReplayIndex)
            {
                mReplayIndex = -1;
            }
            mMultiple = multiple;
            if (mMultiple)
            {
                if (mBoard.mReplayWriters.Count < 2)
                {
                    mMultiple = false;
                }
                else if (mReplayIndex < 0 || mBoard.mReplayWriters.Count <= mReplayIndex)
                {
                    mReplayIndex = mBoard.mReplayWriters.Count - 1;
                }
            }
            // A replay can't be saved while it is itself being replayed.
            mFFwdButton.SetVisible(!(isReplay && !mBoard.mReplayStarted && !mMultiple));
        }

        public void SetMouseVisibility(bool visible)
        {
            mPlayButton.mMouseVisible = visible;
            mFFwdButton.mMouseVisible = visible;
            mDoneButton.mMouseVisible = visible;
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            mPlayButton.Move(REPLAY_BUTTON_X, REPLAY_BUTTON1_Y);
            mFFwdButton.Move(REPLAY_BUTTON_X, REPLAY_BUTTON2_Y);
            mDoneButton.Move(REPLAY_BUTTON_X, REPLAY_BUTTON3_Y);
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
        }

        public override int GetPreferredHeight(int theWidth)
        {
#if PEGGLE_IPHONE
            return Res.IMAGE_IP_REPLAYFRAME.mHeight;
#else
            return REPLAY_DIALOG_H;
#endif
        }

        /// <summary>Holding the right button over the board fast-forwards a replay.</summary>
        public bool IsFast()
        {
            return mBoard.mIsOver && GlobalStaticVars.gSexyAppBase.mWidgetManager.IsRightButtonDown();
        }

        public void BlinkDoneButton()
        {
            mDoneButton.Blink(10, true);
        }

        public void StopBlinkingButtons()
        {
            mDoneButton.mBlinkCount = 0;
        }

        public void DialogButtonPress(int theDialogId, int theButtonId)
        {
        }

        public void DialogButtonDepress(int theDialogId, int theButtonId)
        {
            if (theDialogId != DIALOG_SAVE_REPLAY)
            {
                return;
            }
            ThunderballApp anApp = mBoard.mApp;
            if (theButtonId == 1000)
            {
                EditDialog aDialog = anApp.GetDialog(DIALOG_SAVE_REPLAY) as EditDialog;
                if (aDialog != null)
                {
                    string aDescription = aDialog.GetString();
                    int anId = anApp.mNextReplayId++;
                    mBoard.SaveReplayFile(anId, aDescription);
                    anApp.RegistryWriteInteger("NextReplayId", anApp.mNextReplayId);
                }
            }
            anApp.DoScrollOff(theDialogId);
            mBoard.Pause(false);
        }

        public override void ButtonDepress(int theId)
        {
#if PEGGLE_IPHONE
            if (IsScrolling())
#else
            if (IsScrolling() || mBoard.mZoomFrame > 0)
#endif
            {
                return;
            }
            if (theId == REPLAY_BUTTON_DONE)
            {
                if (!mBoard.mIsReplay)
                {
                    mBoard.EndReplay();
                    mBoard.mInputEnabled = false;
#if PEGGLE_IPHONE
                    mBoard.mInstantReplayButton.SetDisabled(false);
#endif
                    mBoard.mLeftScorePod.SetVisible(true);
                    mBoard.mRightScorePod.SetVisible(true);
#if !PEGGLE_IPHONE
                    mBoard.mReplayButtonState = 1;
#endif
                }
                else
                {
                    mBoard.mApp.DoReplayFileDialog(true);
                }
            }
            else if (theId == REPLAY_BUTTON_SAVE)
            {
                mBoard.Pause(true);
                EditDialog aDialog = new EditDialog(DIALOG_SAVE_REPLAY, SexyAppBase.Loc("Save Replay"),
                    SexyAppBase.Loc("Enter a description for this replay."), false);
                aDialog.mDialogListener = this;
                aDialog.SetString("");
                aDialog.mYesButton.mLabel = SexyAppBase.Loc("OK");
                aDialog.mNoButton.mLabel = SexyAppBase.Loc("Cancel");
                aDialog.mEditOffsetY = DIALOG_SAVE_REPLAY_EDIT_EXTRA_OFFSET_Y;
                aDialog.mContentInsets.mBottom /= 2;
#if PEGGLE_IPHONE
                SexyUtil.PositionDialog(aDialog, GlobalStaticVars.gSexyAppBase.mWidth, false, -1);
#else
                SexyUtil.PositionDialog(aDialog, SCREEN_WIDTH, false, -1);
                aDialog.mHeight += 0x32;
#endif
                mBoard.mApp.AddDialog(aDialog.mId, aDialog);
            }
            else if (theId == REPLAY_BUTTON_PLAY)
            {
                if (!mPlayButton.mTouchCancelled)
                {
                    if (mBoard.mLogicMgr.mDimLowObjects)
                    {
                        mBoard.PlayMusic();
                    }
                    mBoard.ShowReplay(mReplayIndex, false);
                }
                else
                {
                    // A long press toggles between the last shot and stepping through every shot.
                    SetMode(mIsReplay, !mMultiple, false);
                    mBoard.PositionReplayDialog();
                }
            }
        }

        public override void Draw(Graphics g)
        {
#if PEGGLE_IPHONE
            if (mBoard.mZoomFrame < 10)
            {
                g.DrawImage(Res.IMAGE_IP_REPLAYFRAME, 0, 0);
            }
#endif
        }

#if PEGGLE_IPHONE
        // iPhone: pressing Stop starts slow motion until the touch lifts (see Update).
        public override void ButtonPress(int theId)
        {
            base.ButtonPress(theId);
            if (theId == REPLAY_BUTTON_DONE)
            {
                mFlag140 = true;
                mBoard.SetSlowMo(true, 0xfa);
            }
        }
#endif

        public override void Update()
        {
            base.Update();
#if PEGGLE_IPHONE
            // Slow motion while a touch is held on the board.
            if (!IsScrolling() && mWidgetManager != null)
            {
                if (!mBoard.mIsOver || !mWidgetManager.IsLeftButtonDown())
                {
                    if (mFlag140)
                    {
                        mFlag140 = false;
                        mBoard.SetSlowMo(false, 0xfa);
                    }
                }
                else if (!mFlag140)
                {
                    mFlag140 = true;
                    mBoard.SetSlowMo(true, 0xfa);
                }
            }
#else
            IsScrolling();
#endif
        }
    }
}
