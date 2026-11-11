using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The saved-replay browser (Sexy::ReplayFileDialog, "Show Replay"): a scrolling list of the
    /// replay files in userdata/replays with Edit (rename) and Delete buttons; Play loads the
    /// selected file into a (possibly new) Board and starts it in replay mode.
    /// </summary>
    public class ReplayFileDialog : ThunderDialog, EditListener, DialogListener, INavHandler
    {
        public const int DIALOG_DELETE_REPLAY = 29;
        public const int DIALOG_EDIT_REPLAY = 31;
        public const int DIALOG_REPLAY_FILE_ERROR = 32;
        public const int DIALOG_REPLAY_FILE_CONFIRM = 33;

        // 0x128: EditListener sub-object, 0x12c: DialogListener sub-object

        public ThunderballApp mApp;                               // 0x130

        public Board mBoard;                                      // 0x134

        public ThunderButton mEditButton;                         // 0x138

        public ThunderButton mDeleteButton;                       // 0x13c

        public ReplayThumbnailsWidget mThumbnails;                // 0x140

        public ThunderScrollWidget mScrollWidget;                 // 0x144

        /// <param name="theFresh">iPhone: false reopens the list where it was left (scroll and selection).</param>
        public ReplayFileDialog(ThunderballApp theApp, bool theFresh = true)
            : base(DialogId.DIALOG_REPLAY_FILE, true, SexyAppBase.Loc("Show Replay"), "", "", 2)
        {
#if PEGGLE_IPHONE
            ReplayThumbnailsWidget.ROW_HEIGHT = REPLAY_FILE_THUMB_H + REPLAY_FILE_CAPTION_H + REPLAY_FILE_THUMB_GAP_W * 2;
#else
            ReplayThumbnailsWidget.ROW_HEIGHT = REPLAY_FILE_THUMB_H + REPLAY_FILE_CAPTION_H + REPLAY_FILE_THUMB_GAP_W * 2 + REPLAY_FILE_THUMB_H_IPAD_OFFSET;
#endif
            ReplayThumbnailsWidget.ROW_WIDTH = REPLAY_FILE_THUMB_W * 5 + REPLAY_FILE_THUMB_INSET_W * 2 + REPLAY_FILE_THUMB_GAP_W * 4;
            mCloseOnButton = true;
            mApp = theApp;
            mBoard = mApp.mBoard;
            mApp.UnloadMainMenuResources();
            mApp.LoadQuickplayResources();
            mScrollWidget = new ThunderScrollWidget();
            mThumbnails = new ReplayThumbnailsWidget(mApp);
            mScrollWidget.AddWidget(mThumbnails);
            mEditButton = SexyUtil.MakeButton(0, this, SexyAppBase.Loc("Edit"));
            mDeleteButton = SexyUtil.MakeButton(1, this, SexyAppBase.Loc("Delete"));
            mThumbnails.LoadReplayInfo();
#if PEGGLE_IPHONE
            if (!theFresh)
            {
                mThumbnails.mSelected = mApp.mReplayListSelected;
                mScrollWidget.SetScrollOffset(new CGPoint(0, mApp.mReplayListScrollY), false);
            }
#endif
            mYesButton.mLabel = SexyAppBase.Loc("Play");
            mNoButton.mLabel = SexyAppBase.Loc("Main Menu");
            AddWidget(mScrollWidget);
            AddWidget(mDeleteButton);
            AddWidget(mEditButton);
        }

        public override void Dispose()
        {
            mApp.UnloadQuickplayResources();
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int aWidth = mWidth - REPLAY_FILE_DIALOG_OFFSET_W;
            mEditButton.Layout(0x1103, mYesButton, 0, 0, 0, 0);
            mNoButton.Layout(0x4403, mYesButton, 0, 0, 0, 0);
            mDeleteButton.Layout(0x1103, mNoButton, 0, 0, 0, 0);
            mThumbnails.Resize(0, 0, aWidth, ReplayThumbnailsWidget.GetPreferredHeight(mThumbnails));
            mScrollWidget.Resize(REPLAY_FILE_DIALOG_X, REPLAY_FILE_DIALOG_Y, aWidth, REPLAY_FILE_SCROLL_H);
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return SCREEN_HEIGHT;
        }

        public void FinishDeleteFile()
        {
            if (Common.DeleteFile(mThumbnails.mFiles[mThumbnails.mSelected].mPath))
            {
                mThumbnails.RemoveSelectedItem();
            }
        }

        public bool FinishRenameFile()
        {
            EditDialog aDialog = mApp.GetDialog(DIALOG_EDIT_REPLAY) as EditDialog;
            if (aDialog == null)
            {
                return true;
            }
            ReplayThumbnailsWidget.FileInfo anInfo = mThumbnails.mFiles[mThumbnails.mSelected];
            string anOldCaption = anInfo.mCaption;
            string aNewCaption = aDialog.GetString();
            if (aNewCaption.Length == 0 || string.Equals(anOldCaption, aNewCaption, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (!mApp.ChangeReplayCaption(anInfo.mPath, aNewCaption))
            {
                return false;
            }
            anInfo.mCaption = aNewCaption;
            return true;
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            TRect aRect = mScrollWidget.GetRect();
            aRect.Inflate(6, 6);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0, 0x7f));
            SexyUtil.MyDrawImageBox(g, aRect, Res.IMAGE_DLG_TEXTRECT);
            g.SetColorizeImages(false);
        }

        public void EditWidgetText(int theId, string theString)
        {
            ButtonDepress(1000);
        }

        public bool AllowChar(int theId, char theChar)
        {
            return true;
        }

        public bool AllowText(int theId, string theText)
        {
            return true;
        }

        public bool AllowClear(int theId)
        {
            return true;
        }

        public void DialogButtonPress(int theDialogId, int theButtonId)
        {
        }

        public void DialogButtonDepress(int theDialogId, int theButtonId)
        {
            bool aClose = true;
            if (theDialogId == DIALOG_REPLAY_FILE_CONFIRM)
            {
                if (theButtonId == 1000 && HandleYes(true))
                {
                    mApp.KillDialog(DIALOG_REPLAY_FILE_CONFIRM);
                    KillDialog();
                    return;
                }
            }
            else if (theDialogId == DIALOG_EDIT_REPLAY)
            {
                if (theButtonId == 1000)
                {
                    aClose = FinishRenameFile();
                }
            }
            else if (theDialogId == DIALOG_DELETE_REPLAY && theButtonId == 1000)
            {
                FinishDeleteFile();
            }
            if (aClose)
            {
                mApp.DoScrollOff(theDialogId);
            }
        }

        /// <summary>Loads the selected replay into the board (creating one if needed) and starts it.</summary>
        public bool HandleYes(bool theConfirmed)
        {
            if (mThumbnails.mSelected < 0)
            {
                return false;
            }
#if PEGGLE_IPHONE
            mApp.mReplayListScrollY = mThumbnails.mY;
            mApp.mReplayListSelected = mThumbnails.mSelected;
#endif
            if (mBoard == null)
            {
                mBoard = new Board(mApp);
                mBoard.Resize(0, 0, mWidth, mHeight);
                mWidgetManager.AddWidget(mBoard);
                mBoard.SetVisible(false);
                mBoard.Reset();
                mApp.mBoard = mBoard;
            }
            bool aLoaded = false;
            string aPath = mThumbnails.mFiles[mThumbnails.mSelected].mPath;
            if (mApp.FileExists(aPath))
            {
                aLoaded = LoadFile(aPath);
            }
            if (!aLoaded)
            {
                Dialog aDialog = mApp.DoDialogScroll(DIALOG_REPLAY_FILE_ERROR, true, SexyAppBase.Loc("File Error"),
                    SexyAppBase.Loc("Unable to open replay file."), SexyAppBase.Loc("Ok"), 3);
                RepositionDialog(aDialog);
                return false;
            }
            PhysObj.GetImageMgr()?.Clear();
            mBoard.mReplayStarted = false;
            GlobalStaticVars.gSexyAppBase.mWidgetManager.RemoveWidget(mBoard.mReplayDialog);
            mBoard.mReplayDialog.SetMode(mBoard.mIsReplay, mBoard.mReplayWriters.Count > 1, true);
            mBoard.mLeftScorePod.Resume();
            mBoard.mRightScorePod.Resume();
            mBoard.mLeftScorePod.SetRepeats(0);
            mBoard.mRightScorePod.SetRepeats(0);
            mBoard.mLeftScorePod.SetVisible(true);
            mBoard.mRightScorePod.SetVisible(true);
            mBoard.mRestored = true;
            mBoard.ShowReplay(0, false);
            mBoard.mReplayDialog.mReplayIndex = 0;
            if (mApp.mBoard.mLogicMgr != null)
            {
                mApp.SetLevelPack(StageMgr.GetLevelPackForStage(mApp.mBoard.mLogicMgr.mInt1b8));
            }
            mBoard.Pause(false);
            mBoard.PlayMusic();
            if (!mBoard.mVisible)
            {
                ThunderballApp aBoardApp = mBoard.mApp;
                aBoardApp.CheckScrollOff(aBoardApp.mMainMenu, 1, true);
                aBoardApp.CleanupScreens(false, true);
                aBoardApp.ScrollOn(mBoard);
                mBoard.SetVisible(true);
            }
#if !PEGGLE_IPHONE
            mBoard.mReplayButtonState = 2;
#endif
            return true;
        }

        public void KillDialog()
        {
            GlobalStaticVars.gSexyAppBase.KillDialog(mId);
            if (mBoard != null)
            {
                mBoard.mWidgetManager?.SetFocus(mBoard);
                mBoard.Pause(false);
            }
        }

        public bool LoadFile(string theFileName)
        {
            if (mBoard == null)
            {
                return false;
            }
            return mBoard.LoadReplayFile(theFileName);
        }

        public void RepositionDialog(Dialog theDialog)
        {
            int y = mEditButton.mY - theDialog.mHeight;
            if (y < 10)
            {
                y = 10;
            }
            if (y < mY)
            {
                y = mY;
            }
            theDialog.Move(theDialog.mX, y);
            ((ThunderDialog)theDialog).DoScroll(1);
        }

        public override void ButtonDepress(int theId)
        {
            if (theId == 0)
            {
                if (mThumbnails.mSelected != -1)
                {
                    EditDialog aDialog = new EditDialog(DIALOG_EDIT_REPLAY, SexyAppBase.Loc("Edit Replay"),
                        SexyAppBase.Loc("Enter a description for this replay."), false);
                    aDialog.mYesButton.mLabel = SexyAppBase.Loc("OK");
                    aDialog.mNoButton.mLabel = SexyAppBase.Loc("Cancel");
                    aDialog.mDialogListener = this;
                    aDialog.SetString(mThumbnails.mFiles[mThumbnails.mSelected].mCaption);
                    aDialog.mEditOffsetY = DIALOG_SAVE_REPLAY_EDIT_EXTRA_OFFSET_Y;
                    aDialog.mContentInsets.mBottom /= 2;
#if PEGGLE_IPHONE
                    SexyUtil.PositionDialog(aDialog, mApp.mWidth, false, -1);
#else
                    SexyUtil.PositionDialog(aDialog, SCREEN_WIDTH, false, -1);
#endif
                    mApp.AddDialog(aDialog.mId, aDialog);
                }
            }
            else if (theId == 1000 || theId == 1001)
            {
                bool aClose = true;
                if (theId == 1000)
                {
                    aClose = HandleYes(false);
                }
                if (aClose)
                {
                    KillDialog();
                }
                if (theId == 1001)
                {
                    mApp.ShowMainMenu();
                }
            }
            else if (theId == 1 && mThumbnails.mSelected != -1)
            {
                Dialog aDialog = mApp.DoDialogScroll(DIALOG_DELETE_REPLAY, true, SexyAppBase.Loc("Delete Replay?"),
                    SexyAppBase.Loc("Are you sure you want to delete the replay?"), "", 1);
                aDialog.mYesButton.mLabel = SexyAppBase.Loc("Delete");
                aDialog.mNoButton.mLabel = SexyAppBase.Loc("Cancel");
                aDialog.mDialogListener = this;
                RepositionDialog(aDialog);
            }
        }

        // ---------------------------------------------------------------- port addition: menu navigation

        /// <summary>Port addition (menu navigation): Play is selected first.</summary>
        NavTarget INavHandler.NavDefault()
        {
            return new NavTarget(mYesButton);
        }

        /// <summary>
        /// Port addition (menu navigation): up and down pick the replay, left and right go round the buttons (Edit,
        /// Delete, Play, Main Menu), and B or Start is Main Menu.
        /// </summary>
        bool INavHandler.NavAction(NavAction theAction)
        {
            switch (theAction)
            {
                case NavAction.Up:
                case NavAction.Down:
                {
                    int aCount = mThumbnails.mFiles.Count;
                    if (aCount == 0)
                    {
                        return true;
                    }
                    int aRow = Math.Clamp(mThumbnails.mSelected + (theAction == NavAction.Up ? -1 : 1), 0, aCount - 1);
                    if (aRow != mThumbnails.mSelected)
                    {
                        mApp.PlaySample(Res.SOUND_BUTTON1);
                        mThumbnails.MouseUp(mThumbnails.mWidth / 2, aRow * (REPLAY_FILE_ROW_GAP_W + ReplayThumbnailsWidget.ROW_HEIGHT) + ReplayThumbnailsWidget.ROW_HEIGHT / 2, 1);
                    }
                    return true;
                }
                case NavAction.Left:
                case NavAction.Right:
                {
                    List<NavTarget> aButtons = new List<NavTarget>();
                    foreach (Widget aButton in new Widget[] { mEditButton, mDeleteButton, mYesButton, mNoButton })
                    {
                        if (MenuNav.IsTarget(aButton))
                        {
                            aButtons.Add(new NavTarget(aButton));
                        }
                    }
                    if (aButtons.Count == 0)
                    {
                        return true;
                    }
                    // Reading order on the screen: upper left, upper right, bottom left, bottom right (then round again).
                    aButtons.Sort((a, b) =>
                    {
                        TRect aRectA = MenuNav.GetRect(a);
                        TRect aRectB = MenuNav.GetRect(b);
                        if (Math.Abs(aRectA.mY - aRectB.mY) > aRectA.mHeight / 2)
                        {
                            return aRectA.mY.CompareTo(aRectB.mY);
                        }
                        return aRectA.mX.CompareTo(aRectB.mX);
                    });
                    int anIndex = aButtons.IndexOf(MenuNav.Selected);
                    anIndex = anIndex < 0 ? 0 : (anIndex + (theAction == NavAction.Left ? aButtons.Count - 1 : 1)) % aButtons.Count;
                    MenuNav.Select(aButtons[anIndex]);
                    return true;
                }
                case NavAction.Back:
                case NavAction.Start:
                    MenuNav.Press(mNoButton);
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The list of saved replays in the ReplayFileDialog (Sexy::ReplayThumbnailsWidget): one row
    /// per file with the level thumbnail, caption, level description and save date.
    /// </summary>
    public class ReplayThumbnailsWidget : Widget
    {
        public static int ROW_HEIGHT;

        public static int ROW_WIDTH;

        public class FileInfo
        {
            public string mCaption = "";                          // 0x00

            public string mPath = "";                             // 0x04

            public string mLevelDesc = "";                        // 0x08

            public string mDate = "";                             // 0x0c

            public int mStage;                                    // 0x10

            public int mLevel;                                    // 0x14

            public int mTime;                                     // 0x18
        }

        public ThunderballApp mApp;                               // 0x78

        public List<FileInfo> mFiles = new List<FileInfo>();      // 0x7c

        public int mSelected;                                     // 0x88

        public ReplayThumbnailsWidget(ThunderballApp theApp)
        {
            mApp = theApp;
            mSelected = -1;
        }

        public void RemoveSelectedItem()
        {
            if (mSelected == -1)
            {
                return;
            }
            mFiles.RemoveAt(mSelected);
            Resize(0, 0, mWidth, GetPreferredHeight(this));
            ((ScrollWidget)mParent).ClientSizeChanged();
            mSelected = Math.Min(mSelected, mFiles.Count - 1);
        }

        public static int GetPreferredHeight(ReplayThumbnailsWidget theWidget)
        {
            if (theWidget.mFiles.Count == 0)
            {
                return 0;
            }
            return (REPLAY_FILE_ROW_GAP_W + ROW_HEIGHT) * theWidget.mFiles.Count - REPLAY_FILE_ROW_GAP_W;
        }

        public override void Draw(Graphics g)
        {
            mApp.LoadQuickplayResources();
            Widget aParent = (Widget)mParent;
            TRect aVisible = new TRect(0, -mY, mWidth, aParent.mHeight);
            int aTextOffsetX = REPLAY_FILE_THUMB_W + REPLAY_FILE_THUMBNAIL_GAP_EXTRA_W + REPLAY_FILE_THUMB_GAP_W;
            for (int i = 0; i < mFiles.Count; i++)
            {
                FileInfo anInfo = mFiles[i];
                bool isSelected = i == mSelected;
                TRect aRowRect = new TRect(0, i * (REPLAY_FILE_ROW_GAP_W + ROW_HEIGHT), mWidth, ROW_HEIGHT);
                if (!aRowRect.Intersects(aVisible))
                {
                    continue;
                }
                g.SetColorizeImages(true);
                int aPack = 0;
                int aStage = anInfo.mStage;
                if (aStage > 10)
                {
                    aPack = 1;
                    aStage -= 0xb;
                }
                g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForStage(aStage, aPack, false), 0x7f));
                SexyUtil.MyDrawImageBox(g, aRowRect, Res.IMAGE_DLG_GROUPRECT);
                TRect anInner = aRowRect;
                anInner.Inflate(-3, -3);
                if (isSelected)
                {
                    g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForStage(aStage, aPack, false), 200));
                    SexyUtil.MyDrawImageBox(g, anInner, Res.IMAGE_DLG_GROUPRECT);
                }
                g.SetColorizeImages(false);
                // Port addition: the Extreme, WoW and Dual Shot levels use their logo from the game arrows.
                BonusPage aBonusPage = StageMgr.GetBonusPage(anInfo.mStage);
                int aBonusLogo = aBonusPage != null ? aBonusPage.mLogoCel : -1;
                if (aBonusLogo == GameArrowButton.LOGO_EXTREME || aBonusLogo == GameArrowButton.LOGO_WOW || aBonusLogo == GameArrowButton.LOGO_DUALSHOT)
                {
                    TRect aSrc = GameArrowButton.GetLogoRect(aBonusLogo);
                    g.DrawImage(GameArrowButton.GetSheet(), anInner.mX + anInner.mWidth - aSrc.mWidth, anInner.mY + (anInner.mHeight - aSrc.mHeight) / 2, aSrc);
                }
                else
                {
                    // Port: the other bonus levels by the game they play as (the web demo's: Peggle).
                    bool isPeggle = aBonusPage != null ? aBonusPage.mLevelPack == 0 : anInfo.mStage < 0xb;
                    Image aLogo = isPeggle ? Res.IMAGE_DLG_REPLAY_LOGO_PEGGLE : Res.IMAGE_DLG_REPLAY_LOGO_NIGHTS;
                    g.DrawImage(aLogo, anInner.mX + anInner.mWidth - aLogo.mWidth, anInner.mY + (anInner.mHeight - aLogo.mHeight) / 2);
                }

                int x = aRowRect.mX + REPLAY_FILE_THUMB_INSET_W;
#if PEGGLE_IPHONE
                int y = aRowRect.mY + 4;
#else
                int y = aRowRect.mY + REPLAY_THUMB_Y_OFFSET;
#endif
                SexyUtil.DrawLevelThumbnail(g, anInfo.mStage, anInfo.mLevel, new TRect(x, y, REPLAY_FILE_THUMB_W, REPLAY_FILE_THUMB_H));
                if (!isSelected)
                {
                    g.SetColor(SexyUtil.MakeColor(0x808080));
                    g.DrawRect(x, y, REPLAY_FILE_THUMB_W - 1, REPLAY_FILE_THUMB_H - 1);
                }
                else
                {
                    g.SetColor(SexyUtil.MakeColor(0xffff00));
                    g.DrawRect(x, y, REPLAY_FILE_THUMB_W - 1, REPLAY_FILE_THUMB_H - 1);
                    g.DrawRect(x - 1, y - 1, REPLAY_FILE_THUMB_W + 1, REPLAY_FILE_THUMB_H + 1);
                }

                // Caption, shortened with "..." to fit.
                g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                int aLineSpacing = g.GetFont().GetLineSpacing();
                string aText = "";
                for (int aCut = 0; aCut < anInfo.mCaption.Length; aCut++)
                {
                    aText = anInfo.mCaption.Substring(0, anInfo.mCaption.Length - aCut);
                    if (g.GetFont().StringWidth(aText) <= REPLAY_FILE_THUMBNAIL_MAX_STR_W)
                    {
                        if (aCut > 0)
                        {
                            aText = aText.Substring(0, Math.Max(0, aText.Length - 3)) + "...";
                        }
                        break;
                    }
                }
#if PEGGLE_IPHONE
                g.DrawString(aText, x + aTextOffsetX, DIALOG_REPLAY_FILE_DETAILS_OFFSET_Y + y + g.GetFont().GetHeight());
#else
                g.DrawString(aText, x + aTextOffsetX, DIALOG_REPLAY_FILE_DETAILS_OFFSET_Y + y + aLineSpacing / 2);
#endif

                // Level description, split after its last "x:" prefix onto two lines.
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                g.SetFont(Res.FONT_BUTTON);
                SexyUtil.SetButtonFontColor(3, 1.0f);
                int anAscent = g.GetFont().GetAscent();
                string aDesc = anInfo.mLevelDesc;
#if PEGGLE_IPHONE
                // One line, centred on the thumbnail.
                g.DrawString(aDesc, x + aTextOffsetX, y + DIALOG_REPLAY_FILE_DETAILS_OFFSET_Y + (REPLAY_FILE_THUMB_H + anAscent) / 2);
#else
                int aColon = aDesc.IndexOf(':');
                int aSecondColon = aColon < 0 ? -1 : aDesc.IndexOf(':', aColon + 1);
                int aSplit = (aSecondColon != -1 ? aSecondColon : aColon) + 2;
                aSplit = Math.Clamp(aSplit, 0, aDesc.Length);
                string aLine1 = aDesc.Substring(0, aSplit);
                string aLine2 = aDesc.Substring(aSplit);
                g.DrawString(aLine1, x + aTextOffsetX, REPLAY_FILE_THUMB_H / 3 + anAscent / 2 + y + DIALOG_REPLAY_FILE_DETAILS_OFFSET_Y);
                g.DrawString(aLine2, x + aTextOffsetX, y + DIALOG_REPLAY_FILE_DETAILS_OFFSET_Y + REPLAY_FILE_THUMB_H * 2 / 3 + anAscent / 2 - 5);
#endif

                g.SetFont(Res.FONT_SMALL);
                g.SetColor(isSelected ? SexyUtil.MakeColor(0) : SexyUtil.MakeColor(0xffffff));
                g.DrawString(anInfo.mDate, x + aTextOffsetX, anInner.mY + anInner.mHeight - Res.FONT_SMALL.GetDescent());
            }
        }

        public override void MouseUp(int x, int y, int theClickCount)
        {
            if (x <= REPLAY_FILE_THUMB_INSET_W)
            {
                return;
            }
            int aRow = y / (REPLAY_FILE_ROW_GAP_W + ROW_HEIGHT);
            if ((uint)aRow < (uint)mFiles.Count)
            {
                mSelected = aRow;
                ((ScrollWidget)mParent).ScrollRectIntoView(new TRect(0, mSelected * (REPLAY_FILE_ROW_GAP_W + ROW_HEIGHT), mWidth, ROW_HEIGHT), true);
            }
        }

        /// <summary>Reads a replay file's header into theInfo: caption, save time, level and description.</summary>
        public bool GetReplayDetails(string theFileName, FileInfo theInfo)
        {
            theInfo.mPath = SexyUtil.GetDocumentsDir() + "userdata/replays/" + theFileName;
            Buffer aBuffer = new Buffer();
            if (!mApp.ReadBufferFromFile(theInfo.mPath, ref aBuffer, true))
            {
                return false;
            }
            DataReader aReader = new DataReader();
            aReader.OpenMemory(aBuffer.GetDataPtr(), aBuffer.GetDataLen(), false);
            int aVersion = aReader.ReadLong();
            if (aVersion < 0x54)
            {
                string aFullPath = Path.Combine(GlobalStaticVars.gSexyAppBase.applicationStoragePath, theInfo.mPath);
                theInfo.mTime = (int)new DateTimeOffset(File.GetLastWriteTimeUtc(aFullPath)).ToUnixTimeSeconds();
                theInfo.mCaption = SexyUtil.GetFileName(theFileName, true);
                if (aVersion >= 0x32 && Board.HasNightsReplayHeader(theFileName, aVersion))
                {
                    // Port addition: the Peggle Nights/WoW header's extra long.
                    aReader.ReadLong();
                }
            }
            else
            {
                theInfo.mCaption = aReader.ReadWString();
                theInfo.mTime = aReader.ReadLong();
            }
            DateTime aTime = DateTimeOffset.FromUnixTimeSeconds(theInfo.mTime).LocalDateTime;
            theInfo.mDate = aTime.ToString("dddd MMMM dd, yyyy hh:mmtt", CultureInfo.InvariantCulture);
            if ((aReader.ReadShort() & 0xffff) != 1)
            {
                return false;
            }
            int aLen = aReader.ReadLong();
            if (!aReader.CanReadBytes(aLen))
            {
                return false;
            }
            int aReplayVersion = aReader.ReadLong();
            bool aValid = aReader.ReadBool();
            if (!aValid || aReplayVersion > 0x57)
            {
                return false;
            }
            aReader.ReadLong();
            aReader.ReadLong();
            aReader.ReadLong();
            aReader.ReadLong();
            int aChallengeId = aReader.ReadLong();
            bool isDuel = false;
            string aLevelName = aReader.ReadString();
            if (aReplayVersion > 0x33)
            {
                isDuel = aReader.ReadBit();
                aReader.EndBit();
            }
            LevelInfo aLevel = mApp.mStageMgr.GetLevelInfoByName(aLevelName);
            if (aLevel == null)
            {
                return false;
            }
            theInfo.mStage = aLevel.mStage;
            theInfo.mLevel = aLevel.mLevel;
            if (aChallengeId != 0)
            {
                TrophyInfo aTrophy = mApp.mTrophyMgr.GetTrophyInfoById(aChallengeId);
                if (aTrophy == null)
                {
                    return false;
                }
                theInfo.mLevelDesc = SexyAppBase.Loc("Challenge: ") + aTrophy.mName;
                return true;
            }
            string aCustomStage = mApp.mStageMgr.GetStageInfo(aLevel.mStage)?.mCustomName;
            if (aCustomStage != null)
            {
                // Port addition: a custom level pack's level goes by its stage's name, as on the level screen.
                theInfo.mLevelDesc = (isDuel ? SexyAppBase.Loc("Duel: ") : "") + aCustomStage + ": " + aLevel.mDisplayName;
                return true;
            }
            int aStageNum = StageMgr.GetStageNumberInPack(aLevel.mStage);
            string aFormat = isDuel ? SexyAppBase.Loc("Duel: Stage %d-%d: %S") : SexyAppBase.Loc("Stage %d-%d: %S");
            theInfo.mLevelDesc = CFormat.StrFormat(aFormat.Replace("%S", "%s"), aStageNum, aLevel.mLevel + 1, aLevel.mDisplayName);
            return true;
        }

        public void LoadReplayInfo()
        {
            mFiles.Clear();
            string aDir = Path.Combine(GlobalStaticVars.gSexyAppBase.applicationStoragePath, SexyUtil.GetDocumentsDir() + "userdata/replays");
            if (Directory.Exists(aDir))
            {
                foreach (string aFile in Directory.GetFiles(aDir, "*.peg*"))
                {
                    FileInfo anInfo = new FileInfo();
                    mFiles.Add(anInfo);
                    GetReplayDetails(Path.GetFileName(aFile), anInfo);
                }
            }
            // FileInfo::operator< sorts newest first.
            mFiles.Sort((a, b) => b.mTime.CompareTo(a.mTime));
            if (mSelected == -1)
            {
                mSelected = mFiles.Count == 0 ? -1 : 0;
            }
        }
    }
}
