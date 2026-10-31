using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The Adventure story screen shown before each stage (Sexy::StoryScreen): the master's portrait
    /// and a few words of wisdom, then the stage text typed out. In Nights the next character fades in
    /// with a dream wobble. After the last stage (or a trophy story) it is the win screen instead:
    /// scrolling credits over fireworks.
    /// </summary>
    public class StoryScreen : Widget, ButtonListener
    {
        // 0x78: ButtonListener sub-object

        public ThunderballApp mApp;                               // 0x7c

        public ThunderButton mOkButton;                           // 0x80

        public HyperlinkWidget mContinueLink;                     // 0x84

        public TypingTextWidget mTypingText;                      // 0x88

        public ScrollWidget mScrollWidget;                        // 0x8c

        public StageInfo mStageInfo;                              // 0x90

        public int mStage;                                        // 0x94

        public int mOkButtonX;                                    // 0x98

        public int mOkButtonY;                                    // 0x9c

        public bool mIsWin;                                       // 0xa0

        public int mStoryIndex;                                   // 0xa4

        public int mIntA8;                                        // 0xa8

        public int mIntAC;                                        // 0xac

        public bool mAutoScroll;                                  // 0xb0

        public bool mDrawFireworks;                               // 0xb1

        public float mRocketX;                                    // 0xb4

        public float mRocketY;                                    // 0xb8

        public float mRocketVelX;                                 // 0xbc

        public float mRocketVelY;                                 // 0xc0

        public int mRocketLife;                                   // 0xc4

        public int mRocketDelay;                                  // 0xc8

        public List<Particle> mParticles = new List<Particle>();  // 0xcc

        public bool mTypingDone;                                  // 0xd8

        public bool mIsTrophyStory;                               // 0xd9

        public bool mIsNights;                                    // 0xda

        public int mTypingDelay;                                  // 0xdc

        public float mCreditsScroll;                              // 0xe0

        public int mIntE4;                                        // 0xe4

        public int mIntEC;                                        // 0xec

        public int mDreamFade;                                    // 0xf0

        public int mDreamStep;                                    // 0xf4

        public CurvedVal mDreamAlpha = new CurvedVal();           // 0xf8

        public CurvedVal mDreamAmp = new CurvedVal();             // 0x16c

        public CurvedVal mDreamPhase = new CurvedVal();           // 0x1e0

        public MemoryImage mDreamImage;                           // 0x254

        public CurvedVal mTransitionAlpha = new CurvedVal();      // 0x258

        public CurvedVal mTransitionAmp = new CurvedVal();        // 0x2cc

        public CurvedVal mTransitionPhase = new CurvedVal();      // 0x340

        public bool mInDreamTransition;                           // 0x3b4

        public int mDreamStartTime;                               // 0x3b8

        public AreaParticleEffect mAreaEffect = new AreaParticleEffect(); // 0x3bc

        /// <summary>How long the board's own dream-in waits: the transition fade's length.</summary>
        public double mDreamTransitionPos => mTransitionAlpha.mInMax;

        /// <summary>
        /// Port addition: the Nights stage whose story has no dream (the one before the Master stage).
        /// A bonus Adventure (WoW) has no Master stage, so every stage dreams.
        /// </summary>
        private int NoDreamStage => ThunderballApp.mBonusAdventure != null ? -1 : mApp.mPlayerInfo.mLastAdventureStage - 1;

        /// <summary>
        /// Whether this stage's story dreams the master into the next character. Port: never in the WoW
        /// Adventure, whose masters are shown in their WoW portraits straight away.
        /// </summary>
        private bool Dreams => mStage != NoDreamStage && !SexyUtil.IsWoWAdventure;

        /// <summary>Port addition: the Nights stage that shows Bjorn (the Master stage); none in a bonus Adventure.</summary>
        private int BjornStage => ThunderballApp.mBonusAdventure != null ? -1 : mApp.mPlayerInfo.mLastAdventureStage;

        private static readonly string[] gNumberWords =
        {
            "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve"
        };

        public StoryScreen(ThunderballApp theApp, bool isTrophyStory, bool isNights)
        {
            mApp = theApp;
            mApp.UnloadMainMenuResources();
            mApp.UnloadGameResources();
            if (mApp.mLazyResourceLoading)
            {
                mApp.LoadCharSelectResources();
                if (!mApp.mResourceManager.IsGroupLoaded("CharInGame"))
                {
                    if (!mApp.mResourceManager.LoadResources("CharInGame"))
                    {
                        mApp.ShowResourceError(true);
                    }
                    if (!Res.ExtractCharInGameResources(mApp.mResourceManager, true))
                    {
                        mApp.ShowResourceError(true);
                    }
                }
            }
            mIsTrophyStory = isTrophyStory;
            mIsNights = isNights;
            mRocketLife = -1;
            mDrawFireworks = true;
            mRocketDelay = 200;

            int w = Res.IMAGE_MM_BUTTON_BLUE.mWidth;
            int h = Res.IMAGE_MM_BUTTON_BLUE.mHeight;
            mOkButton = new ThunderButton(Res.IMAGE_MM_BUTTON_BLUE, 0, this);
            mOkButton.mOverlayText = SexyAppBase.Loc("NEXT");
            mOkButton.mButtonImage = Res.IMAGE_MM_BUTTON_BLUE;
            mOkButton.mComponentImage = Res.IMAGE_MM_BUTTON_BLUE;
            mOkButton.mWidth = w;
            mOkButton.mHeight = h / 2;
            mOkButton.mNormalRect = new TRect(0, 0, w, h / 2);
            mOkButton.mOverRect = new TRect(0, 0, w, h / 2);
            mOkButton.mDownRect = new TRect(0, h / 2, w, h / 2);

            mContinueLink = new HyperlinkWidget(0, this);
            mContinueLink.SetFont(SexyUtil.GetFontById(8));
            mContinueLink.mColor = SexyUtil.MakeColor(0xffff88);
            mContinueLink.mOverColor = SexyUtil.MakeColor(0x88ff88);
            mContinueLink.mDoFinger = true;
            mContinueLink.mLabel = SexyAppBase.Loc("Continue");
            mContinueLink.mUnderlineSize = 0;
            mContinueLink.mUnderlineOffset = 0;
            int aLinkWidth = mContinueLink.mFont.StringWidth(mContinueLink.mLabel) + mContinueLink.mFont.GetDecorationOffset() * 2;
            mContinueLink.Resize(30, (int)(SexyUtil.gPhysicalDisplayHeight * 600 / 600.0 + -31.0), aLinkWidth,
                mContinueLink.mFont.GetHeight() + 18);
            mContinueLink.SetVisible(false);

#if PEGGLE_IPHONE
            mOkButtonX = STORY_OK_BT_X + SexyUtil.gGutterWidth * 2;
#else
            mOkButtonX = STORY_OK_BT_X;
#endif
            mOkButtonY = STORY_OK_BT_Y;
            mOkButton.Move(mOkButtonX, mOkButtonY);
            AddWidget(mOkButton);
            AddWidget(mContinueLink);

            mAreaEffect.mAdditive = true;
            mAreaEffect.mAlpha = 1.0f;
            mDreamAlpha.SetCurve("b+0,1,0.01,2,#/k@         ~~###");
            mDreamAmp.SetCurve("b+0,1,0.01,2,~###       ^=oz^  C#H}8");
            mDreamPhase.SetCurve("b+0,1,0.01,2,#0ik         ~~###");
            mTransitionAlpha.SetCurve("b+0,1,0.01,1,~###   ?~###      M####7####");
            mTransitionAmp.SetCurve("b+0,1,0.01,1,#)9*   H7.c%      X~b$h");
            mTransitionPhase.SetCurve("b+0,1,0.01,1,~xDn   :bnTt      g#F+7");
            for (int anId = 0x92; anId <= 0x97; anId++)
            {
                mAreaEffect.AddParticleImageId(anId); // IMAGE_CHR_NIGHTS_PARTICLE1..6
            }
            mAreaEffect.mSpawnRate.SetCurve("b+0,1,0,1,.### %g5cC R~###     IPjAg  &#C<v");
            mAreaEffect.mPos = new SexyVector2(0f, 0f);
            mAreaEffect.mWidth = 0;
            mAreaEffect.mHeight = 0;
            mAreaEffect.mDuration = 200;
            mAreaEffect.mDensity = BitConverter.Int64BitsToDouble(0x3f0f75104d551d69);
            mAreaEffect.mVelXRange = 200;
            mAreaEffect.mVelYRange = 200;
            mAreaEffect.mEdgeX = 5;
            mAreaEffect.mEdgeY = 5;
            mAreaEffect.mParticleAlpha.SetCurve("b+0,1,0.01,0.7,f###    Y~###     G####");
            mAreaEffect.mScale.SetCurve("b+0,1,0.01,0.7,####    `i###     AM###");
            mInDreamTransition = false;
            mDreamStartTime = 0;
            mDreamImage = null;
            mStageInfo = null;
            mStage = 0;
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (aPlayer != null)
            {
                mStage = aPlayer.GetmLastStageStage();
            }
            mStage = Math.Max(mStage, aPlayer.mFirstAdventureStage);
            if (mStage == -1)
            {
                mStage++;
            }
            mIntAC = 0;
            mIntA8 = mIsNights ? 900 : 0;
            InitText();
            if (mIsWin)
            {
                mContinueLink.mUnderlineSize = 2;
#if PEGGLE_IPHONE
                mContinueLink.Move(SexyUtil.gGutterWidth + (STORY_CONTINUE_BT_X - aLinkWidth / 2), STORY_CONTINUE_BT_Y);
#else
                mContinueLink.Move(STORY_CONTINUE_BT_X - aLinkWidth / 2, STORY_CONTINUE_BT_Y);
#endif
            }
        }

        public void InitText()
        {
#if PEGGLE_IPHONE
            if (SexyUtil.gGutterWidth != 0)
            {
                Res.IMAGE_STORY_WIN_NIGHTS = Res.IMAGE_STORY_WIN_NIGHTS_WIDE;
                Res.IMAGE_STORY_WIN = Res.IMAGE_STORY_WIN_WIDE;
            }
#endif
            mTypingText = null;
            mScrollWidget = null;
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            int aFirst = aPlayer.mFirstAdventureStage;
            int aLast = aPlayer.mLastAdventureStage;
            mStageInfo = mApp.mStageMgr.GetStageInfo(mIsTrophyStory ? aLast : SexyUtil.ClampRange(mStage, aFirst, aLast));
            mIsWin = mStage == aLast + 1 || mIsTrophyStory;
            if (!mIsWin)
            {
                // Port: the chapter intro plays the menu theme of the adventure being played (Peggle
                // or Nights), whatever the main menu's look.
                mApp.PlayMenuMusic(ThunderballApp.mGameLevelPack == 1);
            }
            else
            {
                if (ThunderballApp.mGameLevelPack == 1)
                {
                    if (!mApp.mResourceManager.LoadResources("StoryNights"))
                    {
                        mApp.ShowResourceError(true);
                    }
                    if (!Res.ExtractStoryNightsResources(mApp.mResourceManager, false))
                    {
                        mApp.ShowResourceError(true);
                    }
                }
                else
                {
                    if (!mApp.mResourceManager.LoadResources("Story"))
                    {
                        mApp.ShowResourceError(true);
                    }
                    if (!Res.ExtractStoryResources(mApp.mResourceManager, false))
                    {
                        mApp.ShowResourceError(true);
                    }
                }
                mApp.PlayVictoryTrack();
            }
            mTypingDone = false;
            if (ThunderballApp.mGameLevelPack == 1)
            {
                mIntE4 = 0;
                mDreamFade = 0;
                mDreamStep = 0;
                mIntEC = 0;
            }
            mTypingDelay = 20;
            mOkButton.SetVisible(false);
            mOkButton.mX = (0 - mOkButton.mWidth) - STORY_OK_BT_START_OFFSCREEN_OFFSET;
            mCreditsScroll = 0;
            mStoryIndex = 0;
            mAutoScroll = true;
            if (!mIsWin)
            {
                int aChar = SexyUtil.GetAdventureCharacter(mStage - aFirst, ThunderballApp.mGameLevelPack);
                List<StoryData> aMaster = mStageInfo.mStoryText[0];
                if (aMaster.Count != 0 && aMaster[0].mParam >= 0)
                {
                    aChar = aMaster[0].mParam;
                }
                int aNumChars = ThunderballApp.mGameLevelPack == 0 ? 10 : 11;
                aChar %= aNumChars;
                mTypingText = new TypingTextWidget(this);
                if (ThunderballApp.mGameLevelPack == 1)
                {
                    // Port addition: a bonus Adventure's stage (WoW) may have a second pair of texts
                    // (StageDialog 4 and 5) for once the Adventure has been beaten.
                    int aText = 1;
                    if (ThunderballApp.mBonusAdventure != null && aPlayer != null && aPlayer.GetmAdventureWinCount() > 0 &&
                        mStageInfo.mStoryText[3].Count > 0)
                    {
                        aText = 3;
                    }
                    mTypingText.mText = mStageInfo.mStoryText[aText][0].mText;
                    mTypingText.mText2 = mStageInfo.mStoryText[aText + 1].Count > 0 ? mStageInfo.mStoryText[aText + 1][0].mText : "";
                }
                else
                {
                    mStoryIndex = 1;
                    // Port: the Extreme Adventure always tells its first playthrough's story (its stages
                    // have no later ones; their lists hold its credits instead).
                    bool anExtreme = ThunderballApp.mBonusAdventure?.mAdventureKey == StageMgr.ADVENTURE_EXTREME;
                    if (aPlayer != null && !anExtreme)
                    {
                        if (aPlayer.GetmAdventureWinCount() < 4)
                        {
                            mStoryIndex += aPlayer.GetmAdventureWinCount();
                        }
                        else
                        {
                            mStoryIndex += SexyUtil.Rand() % 3 + 1;
                        }
                    }
                    if (mStageInfo.mStoryText[mStoryIndex].Count == 0)
                    {
                        mStoryIndex = 1;
                    }
                    mTypingText.mText = mStageInfo.mStoryText[mStoryIndex][0].mText;
                }
                mTypingText.mColor = SexyUtil.MakeColor(SexyUtil.GetColorForCharacter(aChar, ThunderballApp.mGameLevelPack, false));
                if (aChar == 5)
                {
                    mTypingText.mColor = SexyUtil.MakeColor(0xffac32);
                }
                mTypingText.mFont = Res.FONT_OVERLOAD12OUTLINE;
                int aWidth = STORY_TEXT_W;
                int aHeight = STORY_TEXT_H;
                mTypingText.Resize(new TRect(0, 0, aWidth, mTypingText.GetPreferredHeight(aWidth, -1)));
                if (aHeight < mTypingText.mHeight)
                {
                    mScrollWidget = new ScrollWidget();
                    mScrollWidget.EnableIndicators(Res.IMAGE_DLG_SCROLL_INDICATOR);
                    mScrollWidget.Resize(new TRect(STORY_TEXT_X + SexyUtil.gGutterWidth * 2, STORY_TEXT_Y, aWidth, aHeight));
                    mScrollWidget.AddWidget(mTypingText);
                    AddWidget(mScrollWidget);
                }
                else
                {
                    mTypingText.Move(STORY_TEXT_X + SexyUtil.gGutterWidth * 2, STORY_TEXT_Y);
                    AddWidget(mTypingText);
                }
                if (ThunderballApp.mGameLevelPack == 1)
                {
                    Image aCharImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_CHR_CHARACTER01_ID);
                    int aCharW = aCharImage.mWidth;
                    int aCharH = aCharImage.mHeight;
                    TRect aRect = mScrollWidget == null ? mTypingText.GetRect() : mScrollWidget.GetRect();
                    mAreaEffect.mPos = new SexyVector2((aRect.mX - aCharW) / 2 + aCharW / 2, aRect.mY + aCharH / 2);
                    mAreaEffect.mWidth = aCharW;
                    mAreaEffect.mHeight = aCharH;
                }
            }
            mContinueLink.SetVisible(mIsWin);
        }

        public override void Dispose()
        {
            mDreamImage = null;
            if (mApp.mLazyResourceLoading)
            {
                mApp.mResourceManager.DeleteResources("CharSelect");
                if (mApp.mBoard == null)
                {
                    mApp.mResourceManager.DeleteResources("CharInGame");
                }
            }
            if (mIsWin)
            {
#if PEGGLE_IPHONE
                // InitText swapped in the wide images, so both pointers are cleared.
                if (Res.IMAGE_STORY_WIN != null || Res.IMAGE_STORY_WIN_WIDE != null)
                {
                    mApp.mResourceManager.DeleteResources("Story");
                    Res.IMAGE_STORY_WIN = null;
                    Res.IMAGE_STORY_WIN_WIDE = null;
                }
                if (Res.IMAGE_STORY_WIN_NIGHTS != null || Res.IMAGE_STORY_WIN_NIGHTS_WIDE != null)
                {
                    mApp.mResourceManager.DeleteResources("StoryNights");
                    Res.IMAGE_STORY_WIN_NIGHTS = null;
                    Res.IMAGE_STORY_WIN_NIGHTS_WIDE = null;
                }
#else
                if (Res.IMAGE_STORY_WIN != null)
                {
                    mApp.mResourceManager.DeleteResources("Story");
                    Res.IMAGE_STORY_WIN = null;
                }
                if (Res.IMAGE_STORY_WIN_NIGHTS != null)
                {
                    mApp.mResourceManager.DeleteResources("StoryNights");
                    Res.IMAGE_STORY_WIN_NIGHTS = null;
                }
#endif
            }
            RemoveAllWidgets(true, false);
            mParticles.Clear();
            base.Dispose();
        }

        private static int GetParticleDuration()
        {
            return SexyUtil.Rand() % 30 + 70;
        }

        public void SpawnFireworks()
        {
            int aColor = 0xff5511;
            int aPick = SexyUtil.Rand() % 3;
            if (aPick == 1)
            {
                aColor = 0x3355ff;
            }
            else if (aPick == 0)
            {
                aColor = 0x55ff22;
            }
            // Each spark draws its random numbers in order: x jitter, y jitter, angle, speed.
            for (int i = 0; i < 45; i++)
            {
                AddSpark(5, () => 6.283f * (SexyUtil.Rand() % 100) / 100.0f, () => 0.2f * (SexyUtil.Rand() % 8 + 2), aColor, 0);
            }
            for (int i = 0; i < 90; i++)
            {
                AddSpark(5, () => 6.283f * (SexyUtil.Rand() % 100) / 100.0f, () => 0.2f * (SexyUtil.Rand() % 8 + 2), aColor, 2);
            }
            for (int i = 0; i < 45; i++)
            {
                AddSpark(4, () => 6.283f * (SexyUtil.Rand() % 100) / 100.0f, () => 0.1f * (SexyUtil.Rand() % 10 + 10), aColor, 3);
            }
            if (SexyUtil.Rand() % 2 == 0)
            {
                // A "smiley": two eyes and a mouth.
                for (int i = 0; i < 22; i++)
                {
                    AddSpark(4, () => 2.2f + 0.4f * (SexyUtil.Rand() % 100) / 100.0f, () => 0.2f * (SexyUtil.Rand() % 2 + 4), aColor, 4);
                }
                for (int i = 0; i < 45; i++)
                {
                    AddSpark(4, () => -2.2f - 0.4f * (SexyUtil.Rand() % 100) / 100.0f, () => 0.2f * (SexyUtil.Rand() % 2 + 4), aColor, 4);
                }
                for (int i = 0; i < 67; i++)
                {
                    AddSpark(5, () => -1.0f + 2.0f * (SexyUtil.Rand() % 100) / 100.0f, () => 0.2f * (SexyUtil.Rand() % 1 + 5), aColor, 4);
                }
            }
            else
            {
                // A heart.
                for (int i = 0; i < 142; i++)
                {
                    Particle p = new Particle();
                    p.mX = mRocketX + SexyUtil.Rand() % 6 - 3.0f;
                    p.mY = mRocketY + SexyUtil.Rand() % 6 - 3.0f;
                    float anAngle = 0.0f + 6.283f * (SexyUtil.Rand() % 100) / 100.0f;
                    float aSpeed = (float)(0.949999988079071 * (1.0 + Math.Cos(anAngle)));
                    p.mVelX = (float)(aSpeed * Math.Sin(anAngle));
                    p.mVelY = (float)(aSpeed * Math.Cos(anAngle));
                    if (p.mVelY > 0.0f)
                    {
                        p.mVelX = (float)(p.mVelX * (1.0 - Math.Cos(anAngle)));
                    }
                    p.mColor = aColor;
                    p.mVelY -= 0.75f;
                    p.mType = 4;
                    p.mDuration = GetParticleDuration();
                    mParticles.Add(p);
                }
            }
            if (mUpdateCnt > 20 && mDrawFireworks)
            {
                mApp.PlaySample(Res.SOUND_FIREWORKPOP);
            }
        }

        private void AddSpark(int theJitter, Func<float> theAngle, Func<float> theSpeed, int theColor, int theType)
        {
            Particle p = new Particle();
            p.mX = mRocketX + SexyUtil.Rand() % (theJitter * 2) - (float)theJitter;
            p.mY = mRocketY + SexyUtil.Rand() % (theJitter * 2) - (float)theJitter;
            float anAngle = theAngle();
            float aSpeed = theSpeed();
            p.mVelX = (float)(aSpeed * Math.Sin(anAngle));
            p.mVelY = (float)(aSpeed * Math.Cos(anAngle));
            p.mType = theType;
            p.mColor = theColor;
            p.mDuration = GetParticleDuration();
            mParticles.Add(p);
        }

        public void UpdateWin()
        {
            if (mRocketDelay < 1)
            {
                if (mRocketLife > 0)
                {
                    mRocketLife--;
                    mRocketX += mRocketVelX;
                    mRocketY += mRocketVelY;
                    if (mRocketLife == 0)
                    {
                        SpawnFireworks();
                        mRocketDelay = SexyUtil.Rand() % 250 + 1;
                    }
                }
            }
            else
            {
                mRocketDelay--;
                if (mRocketDelay == 0)
                {
                    // Launch a rocket from the bottom of the left or right half.
                    int aSide = SexyUtil.Rand() % 2;
                    mRocketY = mApp.mHeight;
                    int aHalf = mApp.mWidth / 2;
                    mRocketX = SexyUtil.Rand() % (aHalf * 3 / 4) + aHalf / 4;
                    if (aSide != 0)
                    {
                        mRocketX += mApp.mWidth / 2;
                    }
                    mRocketVelX = (10 - SexyUtil.Rand() % 20) * 0.12f;
                    mRocketVelY = -4.0f;
#if PEGGLE_IPHONE
                    mRocketLife = SexyUtil.Rand() % 0x28 + 0x19;
#else
                    int aMinLife = ROCKET_MIN_LIFE;
                    mRocketLife = SexyUtil.Rand() % ROCKET_LIFE_MODIFIER + aMinLife;
#endif
                }
            }
            UpdateParticles();
            mCreditsScroll += 0.25f;
            if (STORY_CREDITS_LENGTH < mCreditsScroll)
            {
                mCreditsScroll = 0;
            }
            MarkDirty();
        }

        public void UpdateParticles()
        {
            if (mRocketLife > 0)
            {
                // Rocket exhaust.
                for (int i = 0; i < 3; i++)
                {
                    Particle p = new Particle();
#if PEGGLE_IPHONE
                    p.mX = mRocketX + SexyUtil.Rand() % 3;
#else
                    p.mX = mRocketX + SexyUtil.Rand() % 3 - mRocketVelX * 2.0f;
#endif
                    p.mY = mRocketY + SexyUtil.Rand() % 3 - 1.0f;
                    p.mDuration = 30;
                    p.mType = 1;
                    float anAngle = 6.283f * (SexyUtil.Rand() % 100) / 100.0f;
                    float aSpeed = 0.03f * (SexyUtil.Rand() % 10);
                    p.mVelX = (float)(aSpeed * Math.Sin(anAngle));
                    p.mVelY = (float)(aSpeed * Math.Cos(anAngle));
                    mParticles.Add(p);
                }
            }
            for (int i = 0; i < mParticles.Count; i++)
            {
                Particle p = mParticles[i];
                if (p.mUpdateCnt < p.mDuration)
                {
                    p.Update();
                }
                else
                {
                    mParticles[i] = mParticles[mParticles.Count - 1];
                    mParticles.RemoveAt(mParticles.Count - 1);
                    i--;
                }
            }
        }

        private void FinishDreamCurves()
        {
            mDreamAlpha.mInVal = mDreamAlpha.mInMax;
            mDreamAmp.mInVal = mDreamAmp.mInMax;
            mDreamPhase.mInVal = mDreamPhase.mInMax;
        }

        public void DoUpdate()
        {
            if (mIsWin)
            {
                UpdateWin();
                return;
            }
            if (ThunderballApp.mGameLevelPack == 1)
            {
                if (mDreamStep == 2 && Dreams)
                {
                    mAreaEffect.Update();
                }
                mWidgetManager.BringToFront(this);
            }
            if (mTypingDone)
            {
                return;
            }
            if (mTypingDelay != 0)
            {
                mTypingDelay--;
                if (mTypingDelay == 0)
                {
                    mTypingText.mDelay = 20;
                    MarkDirty();
                    if (ThunderballApp.mGameLevelPack == 1)
                    {
                        mDreamFade = 1;
                        mIntEC = 20;
                        mDreamStep++;
                        if (mDreamStep == 2 && Dreams)
                        {
                            mApp.PlaySample(Res.SOUND_STAGE_DREAMY_IN);
                        }
                        if (mDreamStep == 2 && !Dreams)
                        {
                            mDreamFade = 0;
                            FinishDreamCurves();
                        }
                    }
                }
                return;
            }
            if (ThunderballApp.mGameLevelPack == 1)
            {
                if (mDreamStep > 1 && mDreamAlpha.IncInVal() && mDreamAmp.IncInVal() && mDreamPhase.IncInVal())
                {
                    MarkDirty();
                    return;
                }
                if (mDreamFade != 0)
                {
                    MarkDirty();
                    mDreamFade++;
                    if (mDreamFade == 20)
                    {
                        mDreamFade = 0;
                    }
                }
            }
            if (!mTypingText.IsDone())
            {
                if (mAutoScroll && mScrollWidget != null)
                {
                    int aLineHeight = mTypingText.mFont.GetHeight();
                    mScrollWidget.ScrollRectIntoView(new TRect(0, mTypingText.mLineCount * aLineHeight, mTypingText.mWidth, aLineHeight), false);
                }
            }
            else
            {
                mTypingDone = true;
                mOkButton.SetVisible(true);
                mOkButton.Blink(1000, true);
                mApp.PlaySample(Res.SOUND_DIALOG_MOVE);
                mAutoScroll = false;
            }
        }

        public override void Update()
        {
            base.Update();
            if (ThunderballApp.mGameLevelPack == 1 && mInDreamTransition)
            {
                return;
            }
            if (mOkButton.mVisible)
            {
                // Slide the NEXT button in from the left.
                if (mOkButton.mY != mOkButtonY)
                {
                    mOkButton.mY -= 15;
                    if (mOkButton.mY < mOkButtonY)
                    {
                        mOkButton.mY = mOkButtonY;
                    }
                }
                if (mOkButton.mX != mOkButtonX)
                {
                    mOkButton.mX += 30;
                    if (mOkButtonX < mOkButton.mX)
                    {
                        mOkButton.mX = mOkButtonX;
                    }
                }
                MarkDirty();
            }
            for (int i = 0; i < 1; i++)
            {
                DoUpdate();
            }
        }

        public void DrawCharacterPlusFrame(Graphics g, int theStage, int theX, int theY)
        {
            int aStage = theStage;
            if (theStage < 0 || theStage > 10)
            {
                aStage = 11;
            }
            int aBase = aStage == 10 ? (int)ResourceId.IMAGE_CHR_NIGHTS_CHARACTER01_ID : (int)ResourceId.IMAGE_CHR_CHARACTER01_ID;
            Image anImage = SexyUtil.GetImageById(SexyUtil.GetAdventureCharacter(aStage, ThunderballApp.mGameLevelPack) + aBase);
            g.DrawImage(anImage, theX, theY);
            g.DrawImageBox(new TRect(theX - STORY_SCREEN_CHAR_FRAME_GAP, theY - STORY_SCREEN_CHAR_FRAME_GAP,
                anImage.mWidth + STORY_SCREEN_CHAR_FRAME_GAP * 2, anImage.mHeight + STORY_SCREEN_CHAR_FRAME_GAP * 2), Res.IMAGE_DLG_BLUEFRAME);
        }

        /// <summary>Port addition: a portrait image (the WoW Adventure's) in the story's frame.</summary>
        private void DrawPortraitPlusFrame(Graphics g, Image theImage, int theX, int theY)
        {
            g.DrawImage(theImage, theX, theY);
            g.DrawImageBox(new TRect(theX - STORY_SCREEN_CHAR_FRAME_GAP, theY - STORY_SCREEN_CHAR_FRAME_GAP,
                theImage.mWidth + STORY_SCREEN_CHAR_FRAME_GAP * 2, theImage.mHeight + STORY_SCREEN_CHAR_FRAME_GAP * 2), Res.IMAGE_DLG_BLUEFRAME);
        }

        public void DrawNewCharacterPlusFrame(Graphics g, int theStage, int theX, int theY, bool theDream)
        {
            int aStage = theStage;
            if (theStage < 0 || theStage > 10)
            {
                aStage = 10;
            }
            Image anImage = SexyUtil.GetImageById(SexyUtil.GetAdventureCharacter(aStage, ThunderballApp.mGameLevelPack) + (int)ResourceId.IMAGE_CHR_NIGHTS_CHARACTER01_ID);
            if (!theDream)
            {
                g.DrawImage(anImage, theX, theY);
            }
            else
            {
                MiscEffectUtil.DrawDreamImage(g, anImage, 25.0, 20.0, mDreamAlpha.mInVal, theX, theY, mDreamAmp, mDreamPhase, 2, 2.5f);
            }
            g.DrawImageBox(new TRect(theX - STORY_SCREEN_CHAR_FRAME_GAP, theY - STORY_SCREEN_CHAR_FRAME_GAP,
                anImage.mWidth + STORY_SCREEN_CHAR_FRAME_GAP * 2, anImage.mHeight + STORY_SCREEN_CHAR_FRAME_GAP * 2), Res.IMAGE_DLG_BLUEFRAME);
        }

        /// <summary>Port addition: the PC Peggle Extreme ending (images/pc/theend_extreme, 800x600), loaded once.</summary>
        private static Image sExtremeWinImage;

        /// <summary>
        /// Port addition: the Extreme Adventure's win screen is the PC Peggle Extreme one (PeggleExtreme.exe
        /// 0x48b320): its 4:3 art scaled to the screen's height and centred (black round it, on iPhone too),
        /// the diploma's text area filled purple (two rects, clear of the turret at its lower left), and the
        /// credits clipped to its narrow window. Returns that window, or an empty rect when not Extreme.
        /// </summary>
        private TRect DrawExtremeWinBack(Graphics g)
        {
            if (ThunderballApp.mBonusAdventure?.mAdventureKey != StageMgr.ADVENTURE_EXTREME)
            {
                return new TRect(0, 0, 0, 0);
            }
            sExtremeWinImage ??= SexyUtil.LoadPCImage("images/pc/theend_extreme");
            if (sExtremeWinImage == null)
            {
                return new TRect(0, 0, 0, 0);
            }
            float S = SexyUtil.gPhysicalDisplayHeight / 600.0f;
            int ox = (mWidth - sExtremeWinImage.mWidth) / 2;
            TRect PC(int x, int y, int w, int h) => new TRect(ox + (int)(x * S), (int)(y * S), (int)Math.Ceiling(w * S), (int)Math.Ceiling(h * S));
#if PEGGLE_IPHONE
            Graphics aWideG = ThunderDialog.CreateWideGraphics(g, mWidth);
            aWideG.SetColor(new Color(0, 0, 0));
            aWideG.FillRect(ThunderDialog.GetWideRect(mWidth, new TRect(0, 0, mWidth, mHeight)));
#else
            g.SetColor(new Color(0, 0, 0));
            g.FillRect(0, 0, mWidth, mHeight);
#endif
            g.SetColor(Color.White);
            g.DrawImage(sExtremeWinImage, ox, 0);
            // The PC's text window (0x118, 0x75, 0x10a, 0x13c), EXTREME_WIN_BOX_INSET PC px narrower on each side;
            // DrawWin draws no box over it (the PC filled it purple, as the art already is).
            return PC(0x118 + EXTREME_WIN_BOX_INSET, 0x75, 0x10a - EXTREME_WIN_BOX_INSET * 2, 0x13c);
        }

        /// <summary>Port addition: how much narrower (PC px, each side) the Extreme ending's text box is than the PC's window.</summary>
        private const int EXTREME_WIN_BOX_INSET = 4;

        public void DrawWin(Graphics g)
        {
            TRect aRect = DrawExtremeWinBack(g);
            bool anExtreme = aRect.mWidth > 0;
            if (!anExtreme)
            {
                Image aWinImage = ThunderballApp.mGameLevelPack == 1 ? Res.IMAGE_STORY_WIN_NIGHTS : Res.IMAGE_STORY_WIN;
                g.DrawImage(aWinImage, (mWidth - aWinImage.mWidth) / 2, 0);
            }
            if (mStageInfo == null)
            {
                return;
            }
            Graphics aG = g.Create();
            if (!anExtreme)
            {
                // Port: the Extreme ending has no box (its diploma's text area is already purple).
                aRect = new TRect(STORY_CONTENTS_X + SexyUtil.gGutterWidth, STORY_CONTENTS_Y, STORY_CONTENTS_W, STORY_CONTENTS_H);
                g.SetColor(new Color(0, 0, 0x5e, 0x9b));
                g.FillRect(aRect);
                g.SetColor(Color.White);
            }
            aG.ClipRect(aRect);

            // Which credits roll: more trophies/aces unlock longer endings.
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            int anEnding = 0;
            if (ThunderballApp.mBonusAdventure != null)
            {
                anEnding = Math.Max(ThunderballApp.mBonusAdventure.mCreditsEnding, 0);
            }
            else if (ThunderballApp.mGameLevelPack == 1)
            {
                if (aPlayer != null)
                {
                    anEnding = Math.Max(aPlayer.GetNumTrophiesWon(false) - 1, 0);
                }
            }
            else if (aPlayer != null)
            {
                if (aPlayer.GetmTrophySet().Count >= TrophyMgr.GetNumTrophiesCurGame())
                {
                    anEnding = 1;
                    if (aPlayer.mClearedLevelSet.Count >= StageMgr.GetNumLevelsCurGame())
                    {
                        anEnding = 2;
                    }
                }
                if (aPlayer.mAcedLevelSet.Count >= StageMgr.GetNumLevelsCurGame() && mApp.mShowAceAllStory)
                {
                    anEnding = 3;
                }
            }
            if (mStageInfo.mStoryText[anEnding].Count == 0)
            {
                anEnding = 0;
            }
            List<StoryData> aCredits = mStageInfo.mStoryText[anEnding];
            int y = (int)(SexyUtil.gPhysicalDisplayHeight * 455 / 600.0 - mCreditsScroll);
            int aCenterX = aRect.mX + aRect.mWidth / 2;
            int aPad = 0;
            // Entry 0 is the stage's StageDialog of the same number, which shares the list. Port: the
            // bonus Adventure's credits (Extreme's Credit 3) have no StageDialog ahead of them
            // (BonusPage.mCreditsEnding).
            for (int i = ThunderballApp.mBonusAdventure != null ? 0 : 1; i < aCredits.Count; i++)
            {
                StoryData aLine = aCredits[i];
                int aType = aLine.mParam;
                if (aType == 2)
                {
                    y += 20;
                    continue;
                }
                if (aType == 1)
                {
                    aG.SetFont(SexyUtil.GetFontById(8));
                    aG.SetColor(SexyUtil.MakeColor(0xffff00));
                }
                else if (aType == 0)
                {
                    aG.SetFont(SexyUtil.GetFontById(8));
                    aG.SetColor(SexyUtil.MakeColor(0xffff88));
                }
                else
                {
                    aG.SetFont(SexyUtil.GetFontById(0x14));
                    aG.SetColor(SexyUtil.MakeColor(0xffffaa));
                }
                aPad = 0;
                if (aLine.mText.Length > 0 && aLine.mText[0] == '$')
                {
                    // A trophy picture in the credits.
                    int aPadTop = STORY_TROPHY_PAD_TOP;
                    TRect aCelRect;
                    if (ThunderballApp.mGameLevelPack == 1)
                    {
                        int aCel = 0;
                        if (aPlayer != null)
                        {
                            if (aPlayer.GetNumTrophiesWon(true) == 4)
                            {
                                aCel = SexyUtil.Rand() % 4;
                            }
                            else
                            {
                                aCel = aPlayer.mLastTrophyWon;
                            }
                        }
                        aCelRect = Res.IMAGE_STORY_TROPHIES_NIGHTS.GetCelRect(aCel);
                        aG.DrawImageCel(Res.IMAGE_STORY_TROPHIES_NIGHTS, aCenterX - aCelRect.mWidth / 2, aPadTop + y, aCel);
                    }
                    else
                    {
                        aCelRect = Res.IMAGE_STORY_TROPHIES.GetCelRect(anEnding);
                        aG.DrawImageCel(Res.IMAGE_STORY_TROPHIES, aCenterX - aCelRect.mWidth / 2, aPadTop + y, anEnding);
                    }
                    y = aCelRect.mHeight + aPadTop * 2 + y;
                    continue;
                }
                if (aLine.mText.Length > 0 && aLine.mText[0] == '@')
                {
                    aLine.mText = aPlayer.mName;
                }
                if (i != 0)
                {
                    y = aPad + aG.GetFont().GetHeight() + y;
                }
                if (aRect.mY + aRect.mHeight + aG.GetFont().GetHeight() < y)
                {
                    break;
                }
                if (y >= aRect.mY)
                {
                    int aTextW = aG.GetFont().StringWidth(aLine.mText);
                    int aRoom = aRect.mWidth - 8;
                    if (anExtreme && aTextW > aRoom)
                    {
                        // Port: the Extreme diploma is narrow: a line too wide for it is shrunk about its middle.
                        float aShrink = aRoom / (float)aTextW;
                        aG.SetScale(aShrink, aShrink, aCenterX, y);
                        aG.DrawString(aLine.mText, aCenterX - aTextW / 2, y);
                        aG.SetScale(1.0f, 1.0f, 0, 0);
                    }
                    else
                    {
                        aG.DrawString(aLine.mText, aCenterX - aTextW / 2, y);
                    }
                }
            }
            mDrawFireworks = true; // Graphics::Is3D()
            if (mDrawFireworks)
            {
                DrawFireworks(g);
            }
        }

        public void DrawFireworks(Graphics g)
        {
            g.SetColorizeImages(true);
            for (int i = 0; i < mParticles.Count; i++)
            {
                Particle p = mParticles[i];
                float aLife = (float)p.mUpdateCnt / (float)p.mDuration;
                if (p.mType == 1)
                {
                    int aCel = (int)(aLife * 30.0f);
                    g.SetColor(new Color(0, 0, 0, 40 - aCel));
                    Image anImage = Res.IMAGE_STORY_ROCKETFIRE;
#if PEGGLE_IPHONE
                    g.DrawImageCel(anImage, (int)(p.mX - 4.0f), (int)(p.mY - 4.0f), aCel % anImage.mNumCols, aCel / anImage.mNumCols);
#else
                    float aSize = (float)(anImage.mWidth / anImage.mNumCols) * 0.4f;
                    g.DrawImageCel(anImage, new TRect((int)(p.mX - 4.0f), (int)(p.mY - 4.0f), (int)aSize, (int)aSize), aCel);
#endif
                }
                else if (p.mType == 0)
                {
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    g.SetColor(SexyUtil.MakeColor(p.mColor, 120));
                    int aCel = (int)(aLife * 14.0f);
                    g.DrawImageCel(Res.IMAGE_STORY_BIGSPARKLE, new TRect((int)(p.mX - 15.0f), (int)(p.mY - 15.0f), 30, 30), aCel);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                }
                else if (p.mType == 2 || p.mType == 4)
                {
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    int anAlpha = (int)(255.0f - aLife * 255.0f);
                    g.SetColor(SexyUtil.MakeColor(p.mColor, anAlpha));
                    g.DrawImage(Res.IMAGE_STORY_NUKESTREAK, (int)(p.mX - 6.0f), (int)(p.mY - 6.0f), 13, 13);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                }
                else if (p.mType == 3)
                {
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    int anAlpha = (int)(255.0f - aLife * 255.0f);
                    g.SetColor(SexyUtil.MakeColor(p.mColor, anAlpha));
                    Transform aTrans = new Transform();
                    aTrans.Scale(0.75f, 0.75f);
                    aTrans.RotateRad((float)(Math.Atan2(p.mVelX, p.mVelY) + 3.1414999961853027));
                    g.DrawImageTransform(Res.IMAGE_STORY_FIRESTREAK, aTrans, p.mX, p.mY);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                }
            }
            if (mRocketLife >= 0 && mRocketDelay == 0)
            {
                g.SetColor(Color.White);
                Transform aTrans = new Transform();
                aTrans.Scale(0.75f, 0.75f);
                aTrans.RotateRad((float)(Math.Atan2(mRocketVelX, mRocketVelY) + 3.1414999961853027));
                g.DrawImageTransform(Res.IMAGE_STORY_ROCKET, aTrans, mRocketX, mRocketY);
            }
            g.SetColorizeImages(false);
        }

        public void StartDreamTransition()
        {
            if (mDreamImage == null)
            {
                // Snapshot the whole screen; Draw wobbles and fades it out over the new board.
                MemoryImage anImage = new MemoryImage();
                anImage.Create(GlobalStaticVars.gSexyAppBase.mWidgetManager.mWidth, GlobalStaticVars.gSexyAppBase.mWidgetManager.mHeight);
                bool inFrame = Graphics.IsSpriteBatchBegun();
                Graphics aG = Graphics.GetNew(anImage);
                GlobalStaticVars.gSexyAppBase.mWidgetManager.DrawWidgetsTo(aG);
                aG.EndOffscreen(inFrame);
                mDreamStartTime = GlobalStaticVars.gSexyAppBase.mUpdateCount;
                mDreamImage = anImage;
                mContinueLink.SetVisible(false);
                mOkButton.SetVisible(false);
            }
            mInDreamTransition = true;
            mApp.PlaySample(Res.SOUND_STAGE_DREAMY_OUT);
        }

        public override void Draw(Graphics g)
        {
            if (mIsWin)
            {
                DrawWin(g);
                return;
            }
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (ThunderballApp.mGameLevelPack == 1 && mInDreamTransition)
            {
                if (mDreamImage != null)
                {
                    double aTime = (GlobalStaticVars.gSexyAppBase.mUpdateCount - mDreamStartTime) / 100.0;
                    if (aTime <= mTransitionAlpha.mInMax)
                    {
                        double anAlpha = mTransitionAlpha.GetOutVal(aTime);
                        g.SetColorizeImages(true);
                        g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(anAlpha * 255.0)));
                        MiscEffectUtil.DrawDreamImage(g, mDreamImage, 15.0, 120.0, aTime, 0, 0, mTransitionAmp, mTransitionPhase, 2, 6.0f);
                        g.SetColor(SexyUtil.MakeColor(0xffffff));
                        g.SetColorizeImages(false);
                    }
                }
                return;
            }

            g.SetColor(SexyUtil.MakeColor(0x80));
            g.FillRect(0, 0, mWidth, 20);
            g.FillRect(0, mHeight - 20, mWidth, 20);
            g.FillRect(0, 0, 20, mHeight);
            g.FillRect(mWidth - 20, 0, 20, mHeight);
            TRect aDialogRect = new TRect(0, 0, mWidth, mHeight);
#if PEGGLE_IPHONE
            // Port addition (ultrawide): the fill and frame reach the window's edges, and the master's
            // portrait stays in the window's corner.
            TRect aWideRect = ThunderDialog.GetWideRect(mWidth, aDialogRect);
            Graphics aWideG = ThunderDialog.CreateWideGraphics(g, mWidth);
            SexyUtil.DrawDialogFill(aWideG, aWideRect, 0x120952, 0x170b64);
            aWideG.DrawImageBox(aWideRect, Res.IMAGE_DLG_DIALOGBOX);
            int aPortraitShift = ThunderDialog.GetLeftCornerShift();
            Graphics aPortraitG = ThunderDialog.CreateWideGraphics(g, mWidth);
#else
            SexyUtil.DrawDialogFill(g, aDialogRect, 0x120952, 0x170b64);
            int aPortraitShift = 0;
            Graphics aPortraitG = g;
#endif
            if (ThunderballApp.mGameLevelPack == 0)
            {
                // The master's portrait behind the dialog frame.
                int aChar = SexyUtil.GetAdventureCharacter(mStage - aPlayer.mFirstAdventureStage, ThunderballApp.mGameLevelPack);
                if (aChar < 0 || aChar > 9)
                {
                    aChar = 0;
                }
#if !PEGGLE_IPHONE
                TRect anOldClip = g.GetClipRect();
                g.SetClipRect(new TRect(CHAR_DIALOG_CHAR_SEL_CLIP_X, 0, SCREEN_WIDTH - CHAR_DIALOG_CHAR_SEL_CLIP_X, mHeight));
#endif
                Image aPortrait = SexyUtil.GetImageById((int)ResourceId.IMAGE_CHARSEL_01_BJORN_LARGE_ID + aChar);
                float aScale = CHARACTER_IMAGE_STORY_SCREEN_SCALE[aChar];
                if (aScale == 1.0f)
                {
                    aPortraitG.DrawImage(aPortrait, CHARACTER_IMAGE_STORY_SCREEN_OFFSET_X[aChar] + aPortraitShift, CHARACTER_IMAGE_STORY_SCREEN_OFFSET_Y[aChar]);
                }
                else
                {
                    aPortraitG.DrawImage(aPortrait, CHARACTER_IMAGE_STORY_SCREEN_OFFSET_X[aChar] + aPortraitShift, CHARACTER_IMAGE_STORY_SCREEN_OFFSET_Y[aChar],
                        (int)(aPortrait.mWidth * aScale), (int)(aPortrait.mHeight * aScale));
                }
#if !PEGGLE_IPHONE
                g.SetClipRect(anOldClip);
#endif
            }
#if !PEGGLE_IPHONE
            g.DrawImageBox(aDialogRect, Res.IMAGE_DLG_DIALOGBOX);
#endif
            if (mStageInfo == null)
            {
                return;
            }
            g.SetColor(SexyUtil.MakeColor(0xddddff));
#if PEGGLE_IPHONE
            g.SetFont(SexyUtil.GetFontById(0x10));
#else
            g.SetFont(SexyUtil.GetFontById(0xb));
#endif
            int aStageNum = mStage - aPlayer.mFirstAdventureStage;
            Font aFont = g.GetFont();
            if (aStageNum < 0 || aStageNum > 11)
            {
                WriteCenteredLine(g, aFont.GetHeight() - aFont.GetDecorationOffset(),
                    CFormat.StrFormat(SexyAppBase.Loc("Stage %d"), aStageNum + 1));
            }
            else
            {
#if PEGGLE_IPHONE
                WriteCenteredLine(g, aFont.GetHeight() - aFont.GetDecorationOffset(),
#else
                WriteCenteredLine(g, aFont.GetHeight() - aFont.GetDecorationOffset() + IPAD_HEADER_OFFSET,
#endif
                    CFormat.StrFormat(SexyAppBase.Loc("Stage %S"), SexyAppBase.Loc(gNumberWords[aStageNum])));
            }
#if PEGGLE_IPHONE
            TRect aMasterRect = new TRect(DIALOG_STORY_SCREEN_MASTER_TEXT_OFFSET_X + SexyUtil.gGutterWidth * 2, DIALOG_STORY_SCREEN_MASTER_TEXT_OFFSET_Y, 0, 0);
#else
            TRect aMasterRect = new TRect(DIALOG_STORY_SCREEN_MASTER_TEXT_OFFSET_X, DIALOG_STORY_SCREEN_MASTER_TEXT_OFFSET_Y, 0, 0);
#endif
            aMasterRect.mWidth = mWidth - aMasterRect.mX - STORY_MASTER_TEXT_PAD_X;
            TRect aTextRect = mScrollWidget == null ? mTypingText.GetRect() : mScrollWidget.GetRect();
            if (ThunderballApp.mGameLevelPack == 1)
            {
                // The current master, then the next character fading in through a dream.
                Image aFrameImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_CHR_CHARACTER01_ID);
                int aX = (aTextRect.mX - aFrameImage.mWidth) / 2;
                int aY = aTextRect.mY;
                // Port: in the WoW Adventure, just the master's WoW portrait, fading in.
                Image aWoWPortrait = SexyUtil.GetWoWPortrait(SexyUtil.GetAdventureCharacter(mStage - aPlayer.mFirstAdventureStage, ThunderballApp.mGameLevelPack));
                int aPortraits = aWoWPortrait != null ? 1 : 2;
                for (int i = 0; i < aPortraits && i < mDreamStep; i++)
                {
                    int anAlpha = 0xff;
                    if (mDreamFade > 0 && i == mDreamStep - 1)
                    {
                        anAlpha = mDreamFade * 0xff / 20;
                    }
                    g.SetColorizeImages(true);
                    if (aWoWPortrait != null)
                    {
                        g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                        DrawPortraitPlusFrame(g, aWoWPortrait, aX, aY);
                    }
                    else if (i == 0)
                    {
                        g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                        DrawCharacterPlusFrame(g, mStage == BjornStage ? 0 : mStage - aPlayer.mFirstAdventureStage, aX, aY);
                    }
                    else
                    {
                        g.SetColor(SexyUtil.MakeColor(0xffffff, (int)((double)mDreamAlpha * 255.0)));
                        g.SetClipRect(aX, aY, Res.IMAGE_CHR_CHARACTER01.GetWidth(), Res.IMAGE_CHR_CHARACTER01.GetHeight());
                        DrawNewCharacterPlusFrame(g, mStage - aPlayer.mFirstAdventureStage, aX, aY, mStage != NoDreamStage);
                        g.ClearClipRect();
                    }
                    g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                    g.SetColorizeImages(false);
                }
                mAreaEffect.Draw(g);
                mAreaEffect.DrawOverlay(g);
            }
            g.SetColor(SexyUtil.MakeColor(0, 0x80));
            g.SetColorizeImages(true);
            aTextRect = aTextRect.Inflate(8, 8);
            SexyUtil.MyDrawImageBox(g, aTextRect, Res.IMAGE_DLG_TEXTRECT);
            g.SetColorizeImages(false);
            g.SetColor(SexyUtil.MakeColor(0xffffff, 0xff));
            g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
            string aMasterText;
            if (ThunderballApp.mGameLevelPack == 1 || mStoryIndex == 1)
            {
                aMasterText = mStageInfo.mStoryText[0][0].mText;
            }
            else
            {
                aMasterText = SexyAppBase.Loc("A few choice words of\nPeggle Wisdom");
            }
            g.WriteWordWrapped(aMasterRect, aMasterText, g.GetFont().GetMaxCharHeight() * 3 / 2, 0);
        }

        public void ButtonPress(int theId)
        {
        }

        public void ButtonPress(int theId, int theClickCount)
        {
        }

        public void ButtonDepress(int theId)
        {
            if (!mIsWin)
            {
                mApp.mPlayerInfo.SetmAdventureLevelReplay(0);
                mApp.ShowBoard(true, true, false, false);
            }
            else if (ThunderballApp.mBonusAdventure != null)
            {
                // Port addition: a bonus Adventure's win has no trophy room or progress screen to go on to.
                mApp.ShowMainMenu();
            }
            else if (ThunderballApp.mGameLevelPack == 1)
            {
                mApp.ShowProgressScreen(2);
            }
            else
            {
                if (mApp.mShowAceAllStory)
                {
                    mApp.mShowAceAllStory = false;
                }
                mApp.ShowTrophyRoom(1);
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

        public void StopTyping()
        {
            if (mTypingText != null)
            {
                mTypingText.Finish();
            }
            mTypingDelay = 0;
            MarkDirty();
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            if (theClickCount > 0)
            {
                StopTyping();
                if (ThunderballApp.mGameLevelPack == 1 && theClickCount > 0)
                {
                    // Skip straight to the end of the dream.
                    mIntE4 = 10000000;
                    mTypingDelay = 0;
                    mDreamFade = 0;
                    if (mDreamStep < 2 && Dreams)
                    {
                        mApp.PlaySample(Res.SOUND_STAGE_DREAMY_IN);
                    }
                    mDreamStep = 2;
                    FinishDreamCurves();
                    mAreaEffect.mSpawnRate.mOutMin = 0;
                    mAreaEffect.mSpawnRate.mOutMax = 0;
                    MarkDirty();
                }
            }
        }

        public override void KeyChar(SexyChar theChar)
        {
        }

        public override void KeyDown(KeyCode theKey)
        {
        }
    }
}
