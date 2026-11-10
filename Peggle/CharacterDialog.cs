using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The pre-level character picker (Sexy::CharacterDialog). In single-player modes it shows the
    /// character grid with the chosen character's portrait, power and description. In Duel it first
    /// asks friend/computer and difficulty, then player one, then player two. Holding the dialog
    /// fades it out so the board can be seen behind it.
    /// </summary>
    public class CharacterDialog : ThunderDialog, CheckboxListener, INavHandler
    {
        public const int CHARACTER_RANDOM = 0xb;

        public static string[] gPowerupNames = new string[12];

        private static readonly int[] CHARACTER_IMAGE_DUEL_OFFSET_XS = { -50, -58, 0, 22, -18, -54, -50, -54, -40, -4, -54, 100 };

        private static readonly int[] CHARACTER_IMAGE_DUEL_OFFSET_YS = { 100, 134, 80, 100, 144, 60, 110, -35, 89, 40, 79, 145 };

        public ThunderballApp mApp;                               // 0x12c

        public Board mBoard;                                      // 0x130

        public CharacterMgr mCharacterMgr;                        // 0x134

        public PlayerInfo mPlayerInfo;                            // 0x138

        public ThunderCheckbox mFriendCheck;                      // 0x13c

        public ThunderCheckbox mComputerCheck;                    // 0x140

        public ThunderCheckbox[] mSkillChecks = new ThunderCheckbox[4]; // 0x144 (easy, normal, hard, master)

        public float mFade;                                       // 0x154

        public int mSelIdx;                                       // 0x158

        public int[] mSelCharacters = new int[2];                 // 0x15c

        public ThunderButton[] mCharButtons = new ThunderButton[12]; // 0x164

        public ThunderButton mNextButton;                         // 0x198

        public ThunderButton mDoneButton;                         // 0x19c

        public bool mDuel;                                        // 0x1a0

        public bool mDuelChallenge;                               // 0x1a1

        public CharacterDialog(Board theBoard)
            : base(DialogId.DIALOG_CHARACTER, false, "", "", SexyAppBase.Loc(""), 0)
        {
            gPowerupNames[0] = SexyAppBase.Loc("Super Guide");
            gPowerupNames[1] = SexyAppBase.Loc("Multiball");
            gPowerupNames[2] = SexyAppBase.Loc("Pyramid");
            gPowerupNames[3] = SexyAppBase.Loc("Space Blast");
            gPowerupNames[4] = SexyAppBase.Loc("Flippers");
            gPowerupNames[5] = SexyAppBase.Loc("Spooky Ball");
            gPowerupNames[6] = SexyAppBase.Loc("Flower Power");
            gPowerupNames[7] = SexyAppBase.Loc("Lucky Spin");
            gPowerupNames[8] = SexyAppBase.Loc("Fireball");
            gPowerupNames[9] = SexyAppBase.Loc("Zen Ball");
            gPowerupNames[10] = SexyAppBase.Loc("Electrobolt");
            gPowerupNames[11] = SexyAppBase.Loc("Random");
            mFade = 1.0f;
            mSelIdx = 0;
            mBoard = theBoard;
            mApp = theBoard.mApp;
            if (mApp.mStoryScreen == null)
            {
                mApp.mResourceManager.DeleteResources("CharInGame");
            }
            mApp.LoadCharSelectResources();
            mCharacterMgr = mApp.mCharacterMgr;
            mPlayerInfo = mApp.mPlayerInfo;
            mDuel = mBoard.mLogicMgr.mDuelMode;
            mDuelChallenge = mDuel && mBoard.mGameMode == GameMode.GAMEMODE_CHALLENGE;
            if (!mDuel || mDuelChallenge)
            {
                mDialogHeader = SexyAppBase.Loc("Choose Character");
            }
            else
            {
                mDialogHeader = SexyAppBase.Loc("Select Opponent");
            }
            int w = Res.IMAGE_MM_BUTTON_BLUE.mWidth;
            int h = Res.IMAGE_MM_BUTTON_BLUE.mHeight;
            mNextButton = MakeNavButton(1000, SexyAppBase.Loc("NEXT"), w, h);
            mDoneButton = MakeNavButton(0x44c, SexyAppBase.Loc("DONE"), w, h);
            if (!mDuel)
            {
                mNextButton.SetVisible(false);
                mDoneButton.SetVisible(true);
            }
            else
            {
                mNextButton.SetVisible(true);
                mDoneButton.SetVisible(false);
            }
            // Port addition (menu navigation): A on a character or option presses these, so they are never selected.
            mNextButton.mNavIgnore = true;
            mDoneButton.mNavIgnore = true;
            AddWidget(mNextButton);
            AddWidget(mDoneButton);
            mWidth = SCREEN_WIDTH;
            TRect aCel = Res.IMAGE_CHR_CHARACTERS.GetCelRect(0);
            int aSpaceX = CHAR_DIALOG_CHAR_BUTTON_SPACE_X;
            int aSpaceY = CHAR_DIALOG_CHAR_BUTTON_SPACE_Y;
            int aRows = 2;
            int aCols = 7;
            int anIndex = 0;
            int x = 0;
            int y = CHAR_DIALOG_CHAR_BUTTON_Y;
            for (int aRow = 0; aRow < aRows; aRow++)
            {
#if PEGGLE_IPHONE
                x = SexyUtil.gGutterWidth + 0xe;
#else
                x = 0x19;
#endif
                for (int aCol = 0; aCol < aCols; aCol++)
                {
                    ThunderButton aButton = SexyUtil.MakeButton(anIndex, this, "", null);
                    mCharButtons[anIndex] = aButton;
                    aButton.Resize(x, y, aCel.mWidth, aCel.mHeight);
                    aButton.SetDraw(false);
                    // Port addition (menu navigation): the picked character's own border shows the selection.
                    aButton.mNavNoHighlight = true;
                    if (anIndex < 0xb && mPlayerInfo != null && !mPlayerInfo.CheckCharacterUnlocked(anIndex))
                    {
                        aButton.SetDisabled(true);
                    }
                    if (mDuel && !mDuelChallenge)
                    {
                        aButton.SetVisible(false);
                    }
                    AddWidget(aButton);
                    anIndex++;
                    x = aCel.mWidth + aSpaceX + x;
                }
                y = aCel.mHeight + aSpaceY + y;
                aCols = 5;
            }
            int aNextX = x + ((aSpaceX + aCel.mWidth * 2) - mNextButton.mNormalRect.mWidth) / 2 + CHAR_DIALOG_DUEL_NEXT_BUTTON_EXTRA_OFFSET_X;
            mNextButton.Resize(aNextX, CHAR_DIALOG_NEXT_BUTTON_Y, mNextButton.mNormalRect.mWidth, mNextButton.mNormalRect.mHeight);
            mDoneButton.Resize(mNextButton.mX, mNextButton.mY, mNextButton.mWidth, mNextButton.mHeight);
            mNextButton.Blink(1000, true);
            mDoneButton.Blink(1000, true);
            mFriendCheck = SexyUtil.MakeRadioButton(0, this, SexyAppBase.Loc("Play a Friend"), 0);
            mComputerCheck = SexyUtil.MakeRadioButton(1, this, SexyAppBase.Loc("Play Computer"), 0);
            mSkillChecks[0] = SexyUtil.MakeRadioButton(2, this, SexyAppBase.Loc("Easy"), 1);
            mSkillChecks[1] = SexyUtil.MakeRadioButton(3, this, SexyAppBase.Loc("Normal"), 1);
            mSkillChecks[2] = SexyUtil.MakeRadioButton(4, this, SexyAppBase.Loc("Hard"), 1);
            mSkillChecks[3] = SexyUtil.MakeRadioButton(5, this, SexyAppBase.Loc("Master"), 1);
#if PEGGLE_IPHONE
            int aCheckGutter = SexyUtil.gGutterWidth;
#else
            int aCheckGutter = 0;
#endif
            mFriendCheck.Resize(CHAR_DIALOG_OPP_CHECK_X + aCheckGutter, CHAR_DIALOG_OPP_CHECK1_Y, CHAR_DIALOG_OPP_CHECK_W, mFriendCheck.mHeight);
            mComputerCheck.Resize(CHAR_DIALOG_OPP_CHECK_X + aCheckGutter, CHAR_DIALOG_OPP_CHECK2_Y, CHAR_DIALOG_OPP_CHECK_W, mComputerCheck.mHeight);
            int aCheckY = CHAR_DIALOG_DIFF_CHECK_Y;
            for (int i = 0; i < 4; i++)
            {
                mSkillChecks[i].Resize(CHAR_DIALOG_DIFF_CHECK_X + aCheckGutter, aCheckY, CHAR_DIALOG_DIFF_CHECK_W, mSkillChecks[i].mHeight);
                aCheckY += CHAR_DIALOG_DIFF_CHECK_GAP_Y;
            }
            AddWidget(mFriendCheck);
            AddWidget(mComputerCheck);
            for (int i = 0; i < 4; i++)
            {
                AddWidget(mSkillChecks[i]);
            }
            if (!mDuel || mDuelChallenge)
            {
                mFriendCheck.SetVisible(false);
                mComputerCheck.SetVisible(false);
                for (int i = 0; i < 4; i++)
                {
                    mSkillChecks[i].SetVisible(false);
                }
            }
            mSelCharacters[1] = -1;
            mSelCharacters[0] = -1;
            if (!mDuelChallenge)
            {
                if (mPlayerInfo != null)
                {
                    if (!mPlayerInfo.mComputerPlayer)
                    {
                        mFriendCheck.mChecked = true;
                    }
                    else
                    {
                        mComputerCheck.mChecked = true;
                    }
                    CheckSkill(mPlayerInfo.mComputerSkill);
                    SyncDifficultyVisibility();
                    int[] aSaved = { mPlayerInfo.mCurCharacter, mPlayerInfo.mCurCharacter2 };
                    for (int i = 0; i < 2; i++)
                    {
                        mSelCharacters[i] = CanSelectCharacter(aSaved[i]) ? aSaved[i] : 0;
                    }
                }
            }
            else
            {
                int anOpponent = 0;
                int aSkill = 0;
                if (mBoard.mTrophyInfo != null)
                {
                    OpponentInfo anInfo = mBoard.mTrophyInfo.GetOpponentInfo(mBoard.mTrophyLevelIdx);
                    if (anInfo != null)
                    {
                        aSkill = anInfo.mDifficulty;
                        anOpponent = anInfo.mCharacterId;
                    }
                }
                mComputerCheck.mChecked = true;
                if (aSkill == -1)
                {
                    mSkillChecks[0].mChecked = true;
                }
                else if (aSkill == 0)
                {
                    mSkillChecks[1].mChecked = true;
                }
                else if (aSkill == 1)
                {
                    mSkillChecks[2].mChecked = true;
                }
                else
                {
                    mSkillChecks[3].mChecked = true;
                }
                mFriendCheck.mDisabled = true;
                mComputerCheck.mDisabled = true;
                for (int i = 0; i < 4; i++)
                {
                    mSkillChecks[i].SetDisabled(true);
                }
                if (mPlayerInfo != null)
                {
                    mSelCharacters[0] = mPlayerInfo.mCurCharacter;
#if PEGGLE_IPHONE
                    if (!CanSelectCharacter(mSelCharacters[0]))
                    {
                        mSelCharacters[0] = 0;
                    }
#endif
                }
                mSelCharacters[1] = anOpponent;
                mNextButton.SetVisible(false);
                mDoneButton.SetVisible(true);
            }
        }

        /// <summary>Checks the difficulty box for a skill level: -1 easy, 1 hard, 2 master, anything else normal.</summary>
        private void CheckSkill(int theSkill)
        {
            if (theSkill == -1)
            {
                mSkillChecks[0].mChecked = true;
            }
            else if (theSkill == 1)
            {
                mSkillChecks[2].mChecked = true;
            }
            else if (theSkill == 2)
            {
                mSkillChecks[3].mChecked = true;
            }
            else
            {
                mSkillChecks[1].mChecked = true;
            }
        }

        private ThunderButton MakeNavButton(int theId, string theLabel, int w, int h)
        {
            ThunderButton aButton = new ThunderButton(Res.IMAGE_MM_BUTTON_BLUE, theId, this);
            aButton.mOverlayText = theLabel;
            aButton.mButtonImage = Res.IMAGE_MM_BUTTON_BLUE;
            aButton.mComponentImage = Res.IMAGE_MM_BUTTON_BLUE;
            aButton.mWidth = w;
            aButton.mHeight = h / 2;
            aButton.mNormalRect = new TRect(0, 0, w, h / 2);
            aButton.mOverRect = new TRect(0, 0, w, h / 2);
            aButton.mDownRect = new TRect(0, h / 2, w, h / 2);
            return aButton;
        }

        public void SyncDifficultyVisibility()
        {
            if (mDuel)
            {
                bool aShow = mComputerCheck.mChecked;
                for (int i = 0; i < 4; i++)
                {
                    mSkillChecks[i].mVisible = aShow;
                }
            }
        }

        public bool CanSelectCharacter(int theCharacter)
        {
            if (theCharacter == CHARACTER_RANDOM)
            {
                return true;
            }
            return mApp.mPlayerInfo != null && mApp.mPlayerInfo.CheckCharacterUnlocked(theCharacter);
        }

        public override void Dispose()
        {
            mApp.UnloadCharSelectResources();
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return SCREEN_HEIGHT;
        }

#if PEGGLE_IPHONE
        // The iPhone draws the portraits unclipped, with the offsets from the layout table.
        // Port addition (ultrawide): they stay in the window's corners when it is wider than the game.
        public void DrawLeftCharacter(Graphics g, int theCharacter)
        {
            Image anImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_CHARSEL_01_BJORN_LARGE_ID + theCharacter);
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            aWide.DrawImage(anImage, CHARACTER_IMAGE_DUEL_OFFSET_X_ALL[theCharacter] + GetLeftCornerShift(), CHARACTER_IMAGE_DUEL_OFFSET_Y_ALL[theCharacter]);
        }

        public void DrawRightCharacter(Graphics g, int theCharacter)
        {
            Image anImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_CHARSEL_01_BJORN_LARGE_ID + theCharacter);
            int x = mWidth - anImage.mWidth - CHARACTER_IMAGE_DUEL_OFFSET_X_ALL[theCharacter] + GetRightCornerShift();
            int y = CHARACTER_IMAGE_DUEL_OFFSET_Y_ALL[theCharacter];
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            if (theCharacter < 0 || theCharacter > 10)
            {
                aWide.DrawImage(anImage, x, y);
            }
            else
            {
                aWide.DrawImageMirror(anImage, x, y, true);
            }
        }
#else
        public void DrawLeftCharacter(Graphics g, int theCharacter)
        {
            Image anImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_CHARSEL_01_BJORN_LARGE_ID + theCharacter);
            TRect anOldClip = g.mClipRect;
            g.SetClipRect(new TRect(CHAR_DIALOG_CHAR_SEL_CLIP_X, 0, SCREEN_WIDTH - CHAR_DIALOG_CHAR_SEL_CLIP_X, mHeight));
            g.DrawImage(anImage, CHARACTER_IMAGE_DUEL_OFFSET_XS[theCharacter], CHARACTER_IMAGE_DUEL_OFFSET_YS[theCharacter]);
            g.SetClipRect(anOldClip);
        }

        public void DrawRightCharacter(Graphics g, int theCharacter)
        {
            Image anImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_CHARSEL_01_BJORN_LARGE_ID + theCharacter);
            TRect anOldClip = g.mClipRect;
            g.SetClipRect(new TRect(CHAR_DIALOG_CHAR_SEL_CLIP_X, 0, SCREEN_WIDTH + CHAR_DIALOG_CHAR_SEL_CLIP_X * -2, mHeight));
            int x = mWidth - anImage.mWidth - CHARACTER_IMAGE_DUEL_OFFSET_XS[theCharacter];
            int y = CHARACTER_IMAGE_DUEL_OFFSET_YS[theCharacter];
            if (theCharacter < 0 || theCharacter > 10)
            {
                g.DrawImage(anImage, x, y);
            }
            else
            {
                g.DrawImageMirror(anImage, x, y, true);
            }
            g.SetClipRect(anOldClip);
        }
#endif

        /// <summary>The selected character's portrait, name, power (in the character's colour) and description.</summary>
        public void DrawCharacterInfo(Graphics g, int theCharacter)
        {
            if (theCharacter < 0 || theCharacter >= 0xc)
            {
                return;
            }
            int aChar = SexyUtil.ClampRange(theCharacter, 0, 0xb);
            int anAlpha = (int)(mFade * 255.0f);
            g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
            g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
            string aName;
            string aDesc;
            if (aChar == CHARACTER_RANDOM)
            {
                aName = SexyAppBase.Loc("Random");
                aDesc = SexyAppBase.Loc("Choose a random character.");
            }
            else
            {
                aName = mCharacterMgr.GetCharacterName(aChar);
                aDesc = mCharacterMgr.GetCharacterDesc(aChar);
            }
            DrawLeftCharacter(g, aChar);
            TRect aRect = new TRect(CHAR_DIALOG_INFO_RECT_X, CHAR_DIALOG_INFO_RECT_Y, CHAR_DIALOG_INFO_RECT_W, CHAR_DIALOG_INFO_RECT_H);
            int x = aRect.mX;
            int y = aRect.mY;
            g.DrawString(aName, x, y);
            g.SetFont(Res.FONT_OVERLOAD12OUTLINE);
            y += g.GetFont().GetMaxCharHeight();
            string aPower = gPowerupNames[aChar];
            if (aChar < 0xb)
            {
                g.SetColor(SexyUtil.MakeColor(SexyUtil.GetColorForCharacter(aChar, ThunderballApp.mGameLevelPack, false), anAlpha));
                g.DrawString(aPower, x, y);
                g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
            }
            aRect.mY = y + CHAR_DIALOG_INFO_END_GAP_Y;
            WriteWordWrapped(g, aRect, aDesc, -1, -1);
        }

        private static int ToArgb(int theRgb, int theAlpha)
        {
            return (theAlpha << 24) | (theRgb & 0xffffff);
        }

        public override void Draw(Graphics g)
        {
            TRect aRect = new TRect(0, 0, mWidth, mHeight);
#if PEGGLE_IPHONE
            // Port addition (ultrawide): the fill, frame and shading reach the window's edges.
            TRect aWideRect = GetWideRect(mWidth, aRect);
            Graphics aWideG = CreateWideGraphics(g, mWidth);
#else
            TRect aWideRect = aRect;
            Graphics aWideG = g;
#endif
            if (mFade < 1.0f)
            {
                SexyUtil.DrawDialogFill(aWideG, aWideRect, ToArgb(0x120952, (int)(mFade * 64.0f)), ToArgb(0x170b64, (int)(mFade * 255.0f)));
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mFade * 255.0f)));
            }
            else
            {
                SexyUtil.DrawDialogFill(aWideG, aWideRect, 0x120952, 0x170b64);
                g.SetColor(Color.White);
            }
#if PEGGLE_IPHONE
            // The frame and header go first, under the portraits.
            aWideG = CreateWideGraphics(g, mWidth);
            aWideG.DrawImageBox(aWideRect, Res.IMAGE_DLG_DIALOGBOX);
            g.SetFont(SexyUtil.GetFontById(0xb));
            WriteCenteredLine(g, DIALOG_HEADER_FONT_OFFSET_Y, mDialogHeader);
#endif
            if (!mDuel)
            {
                DrawCharacterInfo(g, mSelCharacters[0]);
            }
            else
            {
                DrawLeftCharacter(g, mSelCharacters[0]);
                DrawRightCharacter(g, mSelCharacters[1]);
                g.DrawImage(Res.IMAGE_16_VS, CHAR_DIALOG_CHAR_SEL_VS_X, CHAR_DIALOG_CHAR_SEL_VS_Y);
            }
            int y = CHAR_DIALOG_LINE_FILL_Y;
#if PEGGLE_IPHONE
            aWideG = CreateWideGraphics(g, mWidth);
#endif
            aWideG.SetColor(SexyUtil.MakeColor(0, (int)(mFade * 80.0f)));
            aWideG.FillRect(aWideRect.mX, y, aWideRect.mWidth, CHAR_DIALOG_LINE_FILL_H);
            int[] aLineAlphas = { 70, 30, 10 };
            for (int i = 0; i < 3; i++)
            {
                y -= CHAR_DIALOG_LINE_LINE_H;
                aWideG.SetColor(SexyUtil.MakeColor(0, (int)(mFade * (float)aLineAlphas[i])));
                aWideG.FillRect(aWideRect.mX, y, aWideRect.mWidth, CHAR_DIALOG_LINE_LINE_H);
            }
#if !PEGGLE_IPHONE
            g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mFade * 255.0f)));
            g.DrawImageBox(aRect, Res.IMAGE_DLG_DIALOGBOX);
            g.SetFont(SexyUtil.GetFontById(0xb));
            WriteCenteredLine(g, DIALOG_HEADER_FONT_OFFSET_Y, mDialogHeader);
#endif
            for (int i = 0; i < 0xc; i++)
            {
                ThunderButton aButton = mCharButtons[i];
                if (!aButton.mVisible)
                {
                    continue;
                }
                Image anImage = !aButton.mDisabled ? Res.IMAGE_CHR_CHARACTERS : Res.IMAGE_CHR_CHARACTERSHADOWS;
                int x = aButton.mX;
                int by = aButton.mY;
                g.DrawImage(anImage, x, by, anImage.GetCelRect(i));
                if (mSelCharacters[mSelIdx] == i)
                {
                    g.DrawImageBox(new TRect(x, by, aButton.mWidth, aButton.mHeight), Res.IMAGE_CHARSEL_13_YELLOWFRAME);
                }
            }
            g.SetColorizeImages(false);
            DeferOverlay(0);
        }

        public override void DrawOverlay(Graphics g)
        {
            if (mFade < 1.0f)
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mFade * 10.0f)));
            }
            if (mDuel && !mDuelChallenge)
            {
                for (int i = 0; i < 0xc; i++)
                {
                    ThunderButton aButton = mCharButtons[i];
                    if (!aButton.mVisible)
                    {
                        continue;
                    }
                    if (mSelCharacters[0] == i)
                    {
                        g.DrawImage(Res.IMAGE_CHARSEL_14_ONE, CHAR_DIALOG_CHAR_SEL_1_X + aButton.mX, CHAR_DIALOG_CHAR_SEL_1_Y + aButton.mY);
                    }
                    if (mSelIdx > 0 && mSelCharacters[1] == i)
                    {
                        g.DrawImage(Res.IMAGE_CHARSEL_15_TWO, CHAR_DIALOG_CHAR_SEL_2_X + aButton.mX, CHAR_DIALOG_CHAR_SEL_2_Y + aButton.mY);
                    }
                }
            }
            g.SetColorizeImages(false);
        }

        /// <summary>Fades to a quarter while held (to peek at the board) and back up on release.</summary>
        public override void Update()
        {
            float anOldFade = mFade;
            if (!mIsDown)
            {
                mFade = Math.Min(1.0f, mFade + 0.05f);
            }
            else
            {
                mFade = Math.Max(0.25f, mFade - 0.05f);
            }
            if (anOldFade != mFade)
            {
                float aButtonFade = (float)Math.Pow(mFade, 2.0);
                List<Widget> aWidgets = new List<Widget>();
                aWidgets.Add(mNextButton);
                aWidgets.Add(mDoneButton);
                mNextButton.SetFadeAlpha(aButtonFade);
                mDoneButton.SetFadeAlpha(aButtonFade);
                aWidgets.Add(mFriendCheck);
                aWidgets.Add(mComputerCheck);
                mFriendCheck.SetFadeAlpha(aButtonFade);
                mComputerCheck.SetFadeAlpha(aButtonFade);
                for (int i = 0; i < 4; i++)
                {
                    aWidgets.Add(mSkillChecks[i]);
                    mSkillChecks[i].SetFadeAlpha(aButtonFade);
                }
                int anAlpha = (int)(mFade * 255.0f);
                foreach (Widget aWidget in aWidgets)
                {
                    if (aWidget == null)
                    {
                        continue;
                    }
                    for (int i = 0; i < aWidget.mColors.Count; i++)
                    {
                        Color aColor = aWidget.mColors[i];
                        aColor.A = (byte)anAlpha;
                        aWidget.mColors[i] = aColor;
                    }
                }
            }
            base.Update();
        }

        /// <summary>Stores the choices in the player profile and applies them to the board.</summary>
        public void SaveDetails()
        {
            if (mSelCharacters[0] == -1 || (mDuel && mSelCharacters[1] == -1) || mPlayerInfo == null)
            {
                return;
            }
            if (!mDuel || mDuelChallenge)
            {
                mPlayerInfo.SetSelCharacter(mSelCharacters[0]);
            }
            else
            {
                mPlayerInfo.SetComputerPlayer(mComputerCheck.mChecked);
                if (mSkillChecks[0].mChecked)
                {
                    mPlayerInfo.SetComputerSkill(-1);
                }
                else if (mSkillChecks[1].mChecked)
                {
                    mPlayerInfo.SetComputerSkill(0);
                }
                else if (mSkillChecks[2].mChecked)
                {
                    mPlayerInfo.SetComputerSkill(1);
                }
                else
                {
                    mPlayerInfo.SetComputerSkill(2);
                }
                if (!mPlayerInfo.mComputerPlayer)
                {
                    mPlayerInfo.SetPlayer2Name(SexyAppBase.Loc("Player 2"));
                }
                else
                {
                    mPlayerInfo.SetPlayer2Name(SexyAppBase.Loc("Computer"));
                }
                mPlayerInfo.SetSelCharacters(mSelCharacters[0], mSelCharacters[1]);
            }
            mBoard.SyncCharacterOptions(true);
        }

        public void PlayCharacterSound(int theCharacter)
        {
            int aSound = -1;
            if (theCharacter < 0 || theCharacter > 10)
            {
                if (theCharacter == CHARACTER_RANDOM)
                {
                    aSound = Res.GetSoundById(ResourceId.SOUND_LEVELDONE_ID);
                }
            }
            else
            {
                aSound = Res.GetSoundById((ResourceId)((int)ResourceId.SOUND_POWERUP_00_GUIDE_ID + theCharacter));
            }
            mApp.mSoundManager.StopAllSounds();
            if (aSound != -1)
            {
                mApp.PlaySample(aSound);
            }
        }

        public override void ButtonPress(int theId, int theClickCount)
        {
            base.ButtonPress(theId, theClickCount);
            if (IsScrolling())
            {
                return;
            }
            if ((theId > -1 && theId < 0xc && CanSelectCharacter(theId)) || theId == CHARACTER_RANDOM)
            {
                mSelCharacters[mSelIdx] = theId;
            }
            PlayCharacterSound(theId);
        }

        public override void ButtonDepress(int theId)
        {
            if (IsScrolling() || (theId != 1000 && theId != 0x44c))
            {
                return;
            }
            WidgetManager aManager = mWidgetManager;
            if (mDuel)
            {
                if (!mCharButtons[0].mVisible)
                {
                    WidgetMover aMover = mApp.mWidgetMover;
                    aMover.ScrollOff(mFriendCheck, 1, false);
                    aMover.ScrollOff(mComputerCheck, 1, false);
                    for (int i = 0; i < 4; i++)
                    {
                        aMover.ScrollOff(mSkillChecks[i], 1, false);
                    }
                    for (int i = 0; i < 0xc; i++)
                    {
                        mCharButtons[i].SetVisible(true);
                        aMover.ScrollOn(mCharButtons[i], 1);
                    }
                    mDialogHeader = SexyAppBase.Loc("Select Player One");
                    return;
                }
                if (mSelIdx == 0 && !mDuelChallenge)
                {
                    mDialogHeader = SexyAppBase.Loc("Select Player Two");
                    mNextButton.SetVisible(false);
                    mDoneButton.SetVisible(true);
                    mSelIdx = 1;
                    return;
                }
            }
            SaveDetails();
            mBoard.mShowingGameTip = false;
            mApp.KillDialog(this);
            mApp.UnloadCharSelectResources();
            ResourceManager aResMgr = mApp.mResourceManager;
            if (!aResMgr.IsGroupLoaded("CharInGame"))
            {
                if (!aResMgr.LoadResources("CharInGame"))
                {
                    mApp.ShowResourceError(true);
                }
                if (!Res.ExtractCharInGameResources(aResMgr, true))
                {
                    mApp.ShowResourceError(true);
                }
            }
            aManager?.SetFocus(mBoard);
#if PEGGLE_IPHONE
            // From here the interface takes the chosen character's colours.
            mBoard.mRestored = true;
#endif
            mBoard.mInterfaceMgr.UpdateInterfaceColor();
        }

        public void CheckboxChecked(int theId, bool isChecked)
        {
            if ((uint)theId < 2)
            {
                (theId == 0 ? mComputerCheck : mFriendCheck).mChecked = false;
                SyncDifficultyVisibility();
            }
            else if (theId > 1 && theId < 6)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i + 2 != theId)
                    {
                        mSkillChecks[i].mChecked = false;
                    }
                }
            }
        }

        // ---------------------------------------------------------------- port addition: menu navigation

        /// <summary>Port addition (menu navigation): the character picked now is selected first.</summary>
        NavTarget INavHandler.NavDefault()
        {
            int aChar = mSelCharacters[mSelIdx];
            if (aChar >= 0 && aChar < mCharButtons.Length && mCharButtons[aChar] != null && MenuNav.IsTarget(mCharButtons[aChar]))
            {
                return new NavTarget(mCharButtons[aChar]);
            }
            // The duel options page: the option already checked (selecting one checks it).
            foreach (ThunderCheckbox aCheck in new[] { mComputerCheck, mFriendCheck })
            {
                if (aCheck.mChecked && MenuNav.IsTarget(aCheck))
                {
                    return new NavTarget(aCheck);
                }
            }
            return NavTarget.None;
        }

        /// <summary>Port addition (menu navigation): the character button selected, or -1.</summary>
        private int GetNavCharacter()
        {
            Widget aSel = MenuNav.Selected.mWidget;
            return aSel == null ? -1 : System.Array.IndexOf(mCharButtons, aSel as ThunderButton);
        }

        /// <summary>
        /// Port addition (menu navigation): highlighting a character picks it, as a click on it does; highlighting one of
        /// the duel options (Play a Friend / Play Computer, the difficulty) checks it.
        /// </summary>
        void INavHandler.NavSelectionChanged()
        {
            int aChar = GetNavCharacter();
            if (aChar >= 0 && mSelCharacters[mSelIdx] != aChar)
            {
                ButtonPress(aChar, 1);
            }
            else if (MenuNav.Selected.mWidget is ThunderCheckbox aCheck && !aCheck.mChecked && MenuNav.IsInside(aCheck, this))
            {
                MenuNav.Activate(aCheck);
            }
        }

        /// <summary>Port addition (menu navigation): A is DONE (NEXT when choosing the duel opponent comes next).</summary>
        bool INavHandler.NavAction(NavAction theAction)
        {
            if (theAction == NavAction.Confirm)
            {
                MenuNav.Press(mDoneButton.mVisible ? mDoneButton : mNextButton);
                return true;
            }
            return false;
        }
    }
}
