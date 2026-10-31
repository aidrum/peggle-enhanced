using System;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Port addition: the Peggle WoW cutscene (PeggleWoW.exe Sexy::IntroScreen, 0x4d1a40-0x4d28c0, shown by
    /// ThunderballApp 0x42dc50 when the adventure is started from the lounge at a stage's beginning, before
    /// the board; ThunderballApp.StartAdventureGame here). The WoW main menu's scene (the
    /// InstituteLounge, WoWMenuScene) plays in a window, titled "An Outlandish Adventure", while its master
    /// speaks two lines in turn: Bjorn the first time through, Splork (standing in for him) once the adventure
    /// has been won. The window keeps its place in the PC screen (800x600 scaled to the display height and
    /// centred, PCScene). Port: the PC frame of dialog-box art is replaced by this edition's story-screen
    /// container (the dialog fill and dialog box round the whole screen, as StoryScreen draws it).
    /// A click shows the Continue button (sliding in from the left); a click or Continue in the first part
    /// skips to the second, Continue in the second ends it, as does the second part running out. Then the
    /// adventure goes straight on to the board, as on the PC (the story screen only comes after a finished
    /// stage, so stage 1's StageDialog text is never shown, as in the PC game).
    /// Art: images/pc/wow_mainmenu (the scene) and images/pc/wow_cutscene (tools/build_wow_assets.py).
    /// </summary>
    public class WoWCutscene : Widget, ButtonListener
    {
        /// <summary>The scene's window in the PC screen (the IntroScreen Draw's clip rect).</summary>
        private const int WINDOW_X = 0x78;

        private const int WINDOW_Y = 78;

        private const int WINDOW_W = 800 - 0xf0;

        private const int WINDOW_H = 434;

        /// <summary>Where the lounge's origin is drawn (IntroScreen this+0xc20/0xc24).</summary>
        private const int SCENE_X = 0x56;

        private const int SCENE_Y = 0xf;

        /// <summary>The lounge's speech spot (its this+0x874/0x878): Say puts the bubble's arrow tip here.</summary>
        private const int SPEECH_X = 262;

        private const int SPEECH_Y = 202;

        /// <summary>How long each part lasts, in updates (IntroScreen vf22).</summary>
        private static readonly int[] PART_LENGTH = { 0, 400, 500 };

#if PEGGLE_IPHONE
/// <summary>Port: how much smaller the speech bubble is drawn (see SetPart).</summary>
        private const float BUBBLE_SCALE = 0.6f;
#else
/// <summary>Port: how much smaller the speech bubble is drawn (see SetPart).</summary>
        private const float BUBBLE_SCALE = 0.8f;
#endif

        /// <summary>Where the Continue button stops (its x is centred on 400).</summary>
        private const int BUTTON_Y = 0x21a;

        public ThunderballApp mApp;

        private readonly WoWMenuScene mScene;

        private readonly PCScene mPC = new PCScene();

        private Image mSelectorFrame;

        private Image[] mSpeechImages;

        private readonly SpeechBubble[] mBubbles = new SpeechBubble[3];

        private readonly bool mSplork;

        /// <summary>The part playing, 1 or 2 (PC this+0xc04), and its update count (+0xc08).</summary>
        private int mPart;

        private int mPartCount;

        private readonly ThunderButton mContinueButton;

        private bool mFinished;

        private static float S => SexyUtil.gPhysicalDisplayHeight / 600.0f;

        public WoWCutscene(ThunderballApp theApp)
        {
            mApp = theApp;
            // vf71: Splork stands in for Bjorn once the adventure has been won.
            mSplork = mApp.mPlayerInfo != null && mApp.mPlayerInfo.GetmAdventureWinCount() > 0;
            mScene = new WoWMenuScene(mSplork);
            mScene.mShowQuestMarker = false;
            float aScale = S;
            mSelectorFrame = SexyUtil.LoadPCImage("images/pc/wow_cutscene/selectorframe", aScale);
            string[] aPieces = { "br", "bl", "b", "tl", "t", "tr", "l", "r", "c", "bpl", "lp", "rp", "tp" };
            mSpeechImages = new Image[aPieces.Length];
            for (int i = 0; i < aPieces.Length; i++)
            {
                mSpeechImages[i] = SexyUtil.LoadPCImage("images/pc/wow_cutscene/speech_" + aPieces[i], aScale);
            }

            Image aBlue = Res.IMAGE_MM_BUTTON_BLUE;
            int w = aBlue.mWidth;
            int h = aBlue.mHeight;
            mContinueButton = new ThunderButton(aBlue, 0, this);
            mContinueButton.mOverlayText = SexyAppBase.Loc("Continue");
            mContinueButton.mButtonImage = aBlue;
            mContinueButton.mComponentImage = aBlue;
            mContinueButton.mWidth = w;
            mContinueButton.mHeight = h / 2;
            mContinueButton.mNormalRect = new TRect(0, 0, w, h / 2);
            mContinueButton.mOverRect = new TRect(0, 0, w, h / 2);
            mContinueButton.mDownRect = new TRect(0, h / 2, w, h / 2);
            mContinueButton.SetVisible(false);
            AddWidget(mContinueButton);

            mApp.PlayMenuMusic(true);
            SetPart(1);
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            mScene.Dispose();
            mSelectorFrame?.Dispose();
            mSelectorFrame = null;
            if (mSpeechImages != null)
            {
                foreach (Image anImage in mSpeechImages)
                {
                    anImage?.Dispose();
                }
                mSpeechImages = null;
            }
            base.Dispose();
        }

        private int PX(float x) => mPC.PX(x);

        private int PY(float y) => mPC.PY(y);

        /// <summary>The Continue button's resting place: centred on the PC screen, in its bottom band.</summary>
        private int ButtonRestX => PX(400) - mContinueButton.mWidth / 2;

        private int ButtonRestY => PY(BUTTON_Y + 18) - mContinueButton.mHeight / 2;

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            mPC.Update(mWidth, mHeight);
            if (mContinueButton.mVisible)
            {
                mContinueButton.Move(ButtonRestX, ButtonRestY);
            }
        }

        /// <summary>
        /// FUN_004d2300: starts a part, with its line said in a speech bubble (Bjorn's, or Splork's once the
        /// adventure has been won), the bubble's arrow 20 (Splork) or 80 (Bjorn) px right of the lounge's
        /// speech spot and 15 down.
        /// </summary>
        private void SetPart(int thePart)
        {
            if (thePart == mPart)
            {
                return;
            }
            mPart = thePart;
            mPartCount = 0;
            string aLine;
            if (thePart == 1)
            {
                aLine = mSplork ? "Returned! This is excelling!" : "I can't wait to visit Azeroth!";
            }
            else
            {
                aLine = mSplork ? "Alltimes good to redo quests for larger experiences!" : "I clicked \"Accept!\" for every quest in the tour package!";
            }
            foreach (SpeechBubble anOld in mBubbles)
            {
                anOld?.Hide(0);
            }
            if (mSpeechImages == null || Array.IndexOf(mSpeechImages, null) >= 0)
            {
                return;
            }
            // The lounge's Say (0x43c3e0): the bubble's arrow at its bottom left, 36 px in, text in gold in
            // resource 0x47, FONT_PLAIN2 (Italian Old Style 13). The cutscene narrows it to 200 px. Port: no
            // negative text padding (the edition bubble's thick edges allow it; the PC pieces' thin ones don't).
            float aScale = S;
            SpeechBubble aBubble = new SpeechBubble(mSpeechImages);
            aBubble.mOverlapX = 0;
            aBubble.mOverlapY = 0;
            aBubble.mArrowTipX = (int)(10 * aScale);
            aBubble.mArrowTipY = (int)(-10 * aScale);
            aBubble.mShowSound = Res.SOUND_SPEECH_BUBBLE_POPUP;
            aBubble.mLineSpacing = Res.FONT_PLAIN2.GetLineSpacing();
            aBubble.mPadX = 0;
            aBubble.mPadY = 0;
            aBubble.SetTextMaxWidth((int)(200 * aScale));
            aBubble.SetText(SexyAppBase.Loc(aLine), Res.FONT_PLAIN2, SexyUtil.MakeColor(0xffd255));
            // Port: this edition's FONT_PLAIN2 is larger than the PC's, so the bubble (text and all) is drawn
            // at BUBBLE_SCALE, keeping its arrow tip in place.
            aBubble.mDrawScale = BUBBLE_SCALE;
            int aShiftX = mSplork ? 20 : 80;
            aBubble.Move((int)((SPEECH_X + aShiftX) * aScale), (int)((SPEECH_Y + 15) * aScale), 7, (int)(36 * aScale), 0);
            aBubble.Show(0);
            mBubbles[thePart] = aBubble;
        }

        public override void Update()
        {
            base.Update();
            mScene.Update();
            foreach (SpeechBubble aBubble in mBubbles)
            {
                aBubble?.Update();
            }
            // The Continue button slides in from the left, 30 PC px an update.
            if (mContinueButton.mVisible && mContinueButton.mX != ButtonRestX)
            {
                int x = Math.Min(ButtonRestX, mContinueButton.mX + (int)(30 * S));
                mContinueButton.Move(x, ButtonRestY);
            }
            mPartCount++;
            if (mPart == 1 && mPartCount == PART_LENGTH[1])
            {
                SetPart(2);
            }
            else if (mPart == 2 && mPartCount == PART_LENGTH[2])
            {
                Finish();
            }
            MarkDirty();
        }

        public override void Draw(Graphics theG)
        {
            mPC.Update(mWidth, mHeight);
            DrawContainer(theG);
            Graphics g = PCScene.Wide(theG);

            // The scene in its window, the speech bubble over it.
            float aScale = S;
            Graphics aSceneG = g.Create();
            aSceneG.ClipRect(PX(WINDOW_X), PY(WINDOW_Y), (int)(WINDOW_W * aScale), (int)(WINDOW_H * aScale));
            aSceneG.SetColor(new Color(0, 0, 0));
            aSceneG.FillRect(PX(WINDOW_X), PY(WINDOW_Y), (int)(WINDOW_W * aScale), (int)(WINDOW_H * aScale));
            aSceneG.SetColor(Color.White);
            mScene.DrawAt(aSceneG, PX(SCENE_X), PY(SCENE_Y), aScale);
            foreach (SpeechBubble aBubble in mBubbles)
            {
                aBubble?.Draw(aSceneG, PX(SCENE_X), PY(SCENE_Y));
            }

            g.SetColor(Color.White);
            if (mSelectorFrame != null)
            {
                g.DrawImageBox(new TRect(PX(114), PY(70), (int)(572 * aScale), (int)(450 * aScale)), mSelectorFrame);
            }
            g.SetFont(Res.FONT_OVERLOAD24OUTLINE);
            g.SetColor(Color.White);
            string aTitle = SexyAppBase.Loc("An Outlandish Adventure");
            g.DrawString(aTitle, PX(400) - g.GetFont().StringWidth(aTitle) / 2, PY(50));
        }

        /// <summary>
        /// Port: the container round the scene (on the PC a frame of dialog-box art, FUN_004d1c10): the story
        /// screen's, its dark border, dialog fill and dialog box over the whole screen (on iPhone reaching the
        /// window's edges).
        /// </summary>
        private void DrawContainer(Graphics g)
        {
            g.SetColor(SexyUtil.MakeColor(0x80));
            g.FillRect(0, 0, mWidth, 20);
            g.FillRect(0, mHeight - 20, mWidth, 20);
            g.FillRect(0, 0, 20, mHeight);
            g.FillRect(mWidth - 20, 0, 20, mHeight);
            TRect aDialogRect = new TRect(0, 0, mWidth, mHeight);
#if PEGGLE_IPHONE
            TRect aWideRect = ThunderDialog.GetWideRect(mWidth, aDialogRect);
            Graphics aWideG = ThunderDialog.CreateWideGraphics(g, mWidth);
            SexyUtil.DrawDialogFill(aWideG, aWideRect, 0x120952, 0x170b64);
            aWideG.DrawImageBox(aWideRect, Res.IMAGE_DLG_DIALOGBOX);
#else
            SexyUtil.DrawDialogFill(g, aDialogRect, 0x120952, 0x170b64);
            g.DrawImageBox(aDialogRect, Res.IMAGE_DLG_DIALOGBOX);
#endif
            g.SetColor(Color.White);
        }

        /// <summary>vf54: a click shows the Continue button; with it shown, a click in the first part skips to the second.</summary>
        public override void MouseDown(int x, int y, int theClickCount)
        {
            base.MouseDown(x, y, theClickCount);
            if (!mContinueButton.mVisible)
            {
                mContinueButton.Move(-100 - mContinueButton.mWidth, ButtonRestY);
                mContinueButton.SetVisible(true);
                return;
            }
            if (mPart == 1)
            {
                SetPart(2);
            }
        }

        /// <summary>The cutscene is over: straight on to the board (0x42c9a0), as on the PC.</summary>
        private void Finish()
        {
            if (mFinished)
            {
                return;
            }
            mFinished = true;
            // As the story screen would, the stage's story counts as seen, and the board starts afresh.
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (aPlayer != null)
            {
                aPlayer.SetmNeedShowStory(false);
                aPlayer.SetmAdventureLevelReplay(0);
                aPlayer.mDirty = true;
            }
            mApp.ShowBoard(true, true, false, false);
        }

        public void ButtonPress(int theId)
        {
        }

        public void ButtonPress(int theId, int theClickCount)
        {
        }

        /// <summary>vf2: Continue skips the first part, and ends the second.</summary>
        public void ButtonDepress(int theId)
        {
            if (mPart == 1)
            {
                SetPart(2);
            }
            else
            {
                Finish();
            }
        }

        public void ButtonDownTick(int theId)
        {
        }

        public void ButtonMouseEnter(int theId)
        {
        }

        public void ButtonMouseLeave(int theId)
        {
        }

        public void ButtonMouseMove(int theId, int theX, int theY)
        {
        }
    }
}
