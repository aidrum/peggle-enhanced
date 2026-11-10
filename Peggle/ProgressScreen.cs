using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The Peggle Nights trophy room / progress screen (Sexy::ProgressScreen): portraits of the
    /// masters revealed so far, the four Nights trophies, idle speech bubbles, and the spotlight
    /// sequences that reveal a newly beaten master or a newly won trophy.
    /// </summary>
    public class ProgressScreen : Widget, ButtonListener, INavHandler
    {
#if PEGGLE_IPHONE
        // The iPhone's GetPortraitSpeechInfo offsets (literals there).
        private const float PROGRESS_PORTRAIT_BJORN_SPEECH_X = 9, PROGRESS_PORTRAIT_BJORN_SPEECH_Y = -1;
        private const float PROGRESS_PORTRAIT_JIMMY_SPEECH_X = -7, PROGRESS_PORTRAIT_JIMMY_SPEECH_Y = -3;
        private const float PROGRESS_PORTRAIT_TUT_SPEECH_X = 10, PROGRESS_PORTRAIT_TUT_SPEECH_Y = -15;
        private const float PROGRESS_PORTRAIT_SPLORK_SPEECH_X = 2, PROGRESS_PORTRAIT_SPLORK_SPEECH_Y = -2;
        private const float PROGRESS_PORTRAIT_CLAUDE_SPEECH_X = -10, PROGRESS_PORTRAIT_CLAUDE_SPEECH_Y = 0;
        private const float PROGRESS_PORTRAIT_RENFIELD_SPEECH_X = 8, PROGRESS_PORTRAIT_RENFIELD_SPEECH_Y = -7;
        private const float PROGRESS_PORTRAIT_TULA_SPEECH_X = 4, PROGRESS_PORTRAIT_TULA_SPEECH_Y = -8;
        private const float PROGRESS_PORTRAIT_WARREN_SPEECH_X = -10, PROGRESS_PORTRAIT_WARREN_SPEECH_Y = 3;
        private const float PROGRESS_PORTRAIT_CINDER_SPEECH_X = 10, PROGRESS_PORTRAIT_CINDER_SPEECH_Y = 0;
        private const float PROGRESS_PORTRAIT_HU_SPEECH_X = -8, PROGRESS_PORTRAIT_HU_SPEECH_Y = -3;
        private const float PROGRESS_PORTRAIT_KULAH_SPEECH_X = -10, PROGRESS_PORTRAIT_KULAH_SPEECH_Y = 5;
#endif
        // ECloseTo: where the Continue/Back link goes.
        public const int CLOSE_TO_ADVENTURE_SCREEN = 0;
        public const int CLOSE_TO_STORY = 1;
        public const int CLOSE_TO_MAIN_MENU = 2;
        public const int CLOSE_TO_MAIN_MENU_TROPHY = 4;

        // EState.
        public const int STATE_NONE = 0;
        public const int STATE_IDLE_LINK = 1;
        public const int STATE_IDLE = 2;
        public const int STATE_SEEK_PORTRAIT = 3;
        public const int STATE_SHOW_PORTRAIT = 4;
        public const int STATE_SEEK_TROPHY = 5;
        public const int STATE_SHOW_TROPHY = 6;
        public const int STATE_7 = 7;

        // 0x78: ButtonListener sub-object

        public ThunderballApp mApp;                               // 0x7c

        public HyperlinkWidget mOkLink;                           // 0x80

        public bool mShake;                                       // 0x84

        public List<int> mIdleStages = new List<int>();           // 0x88

        public List<int>[] mIdleLines = new List<int>[11];        // 0x94

        public List<KeyValuePair<int, string>> mTrophyLines = new List<KeyValuePair<int, string>>(); // 0xa0

        public int mHoverTrophy = -1;                             // 0xac

        public HelixEffect mHelix = new HelixEffect();            // 0xb0

        public int mTrophy = -1;                                  // 0xcc

        public int mStateDuration = 100;                          // 0xd8

        public CurvedVal mCurveDC = new CurvedVal();              // 0xdc

        public int mIdleSpeechTimer;                              // 0x150

        public List<TrophySparkle> mSparkles = new List<TrophySparkle>(); // 0x154

        public CurvedVal mSparkleScale = new CurvedVal();         // 0x160

        public CurvedVal mSparkleAlpha = new CurvedVal();         // 0x1d4

        public int mState;                                        // 0x254

        public int mCloseTo;                                      // 0x258

        public CurvedVal mTrophyRise = new CurvedVal();           // 0x25c

        public List<float> mTrophyGlow = new List<float> { 0f, 0f, 0f, 0f }; // 0x2d0

        public AreaParticleEffect mParticles = new AreaParticleEffect(); // 0x2dc

        public SpotlightEffect mSpotlight = new SpotlightEffect(); // 0x508

        public SpeechBubble[] mBubbles = new SpeechBubble[11];    // 0x710 (one per stage)

        /// <summary>Port addition: state 7's curtains (NightsCurtains), and whether it has gone on to the credits room.</summary>
        private readonly NightsCurtains mEndingCurtains = new NightsCurtains();

        private bool mEndingShown;

        private static int sApplauseSound = -2;

#if PEGGLE_IPHONE
        /// <summary>Port addition: how long (updates) the trophy room's curtains take to close in state 7.</summary>
        private const int ENDING_SIDES_CLOSE_TIME = 50;
#endif

        public List<SpeechBubble> mOldBubbles = new List<SpeechBubble>(); // 0x71c (hiding, then deleted)

        public class TrophySparkle
        {
            public SexyVector2 mPos;

            public int mAge;
        }

#if PEGGLE_IPHONE
        // The iPhone's tables (480x320 units, scaled by mLayoutScaleFactor when read).
        private static readonly int[] PROGRESS_SPEECH_BUBBLE_XS = { 60, 344, 67, 122, 359, 117, 167, 337, 133, 405, 416 };
        private static readonly int[] PROGRESS_SPEECH_BUBBLE_YS = { 184, 248, 268, 82, 81, 170, 178, 171, 255, 174, 262 };
        private static readonly int[] PROGRESS_PORTRAIT_MIDXS = { 63, 344, 67, 122, 359, 117, 167, 337, 133, 405, 416 };
        private static readonly int[] PROGRESS_PORTRAIT_MIDYS = { 184, 248, 268, 82, 81, 170, 178, 171, 255, 174, 262 };
        private static readonly int[] PROGRESS_PORTRAIT_WS = { 33, 41, 27, 30, 35, 22, 31, 27, 32, 35, 27 };
        private static readonly int[] PROGRESS_PORTRAIT_HS = { 47, 51, 51, 54, 62, 43, 41, 51, 41, 55, 50 };
        private static readonly bool[] PROGRESS_PORTRAIT_CIRCULAR = { false, false, true, false, false, true, false, true, false, false, true };

        // Per trophy: centre x, y, particle w, h, [4], [5], draw x, y, hover x, y.
        private static readonly int[][] PROGRESS_TROPHY_DATA =
        {
            new[] { 240, 186, 87, 97, 240, 186, 197, 133, 199, 133 }, // ADVWIN
            new[] { 292, 50, 39, 78, 292, 50, 273, 12, 273, 12 },     // ALLCLEAR
            new[] { 240, 35, 71, 67, 240, 35, 205, 2, 216, 5 },       // ALLTROPHIES
            new[] { 195, 52, 33, 64, 195, 52, 175, 25, 181, 29 }      // ALLACED
        };

        private static int LAYOUT_SCALE_FACTOR => SexyUtil.mLayoutScaleFactor;
#else
        private static readonly int[] PROGRESS_SPEECH_BUBBLE_XS = { 90, 765, 98, 231, 804, 216, 340, 752, 260, 910, 942 };
        private static readonly int[] PROGRESS_SPEECH_BUBBLE_YS = { 451, 590, 643, 203, 199, 418, 436, 425, 619, 431, 647 };
        private static readonly int[] PROGRESS_PORTRAIT_MIDXS = { 86, 763, 78, 225, 798, 214, 333, 750, 256, 912, 938 };
        private static readonly int[] PROGRESS_PORTRAIT_MIDYS = { 440, 596, 638, 191, 193, 404, 431, 418, 615, 421, 634 };
        private static readonly int[] PROGRESS_PORTRAIT_WS = { 79, 93, 27, 72, 83, 55, 74, 67, 78, 66, 66 };
        private static readonly int[] PROGRESS_PORTRAIT_HS = { 129, 121, 121, 132, 150, 105, 98, 124, 102, 132, 122 };

        // Per trophy: centre x, y, particle w, h, [4], [5], draw x, y, hover x, y.
        private static readonly int[][] PROGRESS_TROPHY_DATA =
        {
            new[] { 515, 410, 209, 233, 540, 490, 412, 330, 412, 330 }, // ADVWIN
            new[] { 638, 123, 94, 187, 638, 123, 585, 30, 585, 30 },    // ALLCLEAR
            new[] { 514, 97, 170, 182, 514, 97, 430, 5, 430, 5 },       // ALLTROPHIES
            new[] { 390, 140, 79, 154, 390, 140, 358, 62, 358, 62 }     // ALLACED
        };

        private const int LAYOUT_SCALE_FACTOR = 1;
#endif

        private const int NIGHTS_TROPHY_ROOM_SPEECH_BUBBLE_PAD_X = -24;
        private const int NIGHTS_TROPHY_ROOM_SPEECH_BUBBLE_PAD_Y = -21;

        // 13 sparkle points per trophy (x, y in 600-high units), CheckAddRandomTrophySparkle.
        private static readonly int[] gSparkleCoordsRaw =
        {
            4, 28, 13, 2, 140, 29, 134, 10, 107, 23, 90, 66, 59, 68, 93, 100, 99, 41, 57, 60, 45, 41, 86, 24, 45, 103,
            21, 2, 63, 22, 2, 28, 51, 58, 8, 105, 10, 60, 28, 34, 51, 27, 21, 14, 22, 82, 40, 54, 21, 2, 63, 22,
            1, 6, 42, 13, 71, 57, 30, 85, 111, 23, 76, 28, 66, 87, 33, 52, 70, 57, 1, 6, 42, 13, 71, 57, 30, 85,
            4, 17, 54, 27, 31, 7, 21, 0, 52, 42, 18, 87, 33, 37, 35, 61, 15, 14, 48, 27, 4, 17, 54, 27, 31, 7
        };

        private static int[] gSparkleCoords;

        public ProgressScreen(ThunderballApp theApp, int theCloseTo)
        {
            for (int i = 0; i < mIdleLines.Length; i++)
            {
                mIdleLines[i] = new List<int>();
            }
            mTrophy = -1;
            mCurveDC.SetCurve("b+0,1,0.01,1.5,~###        D#>E]##### ]#P##");
            mApp = theApp;
            if (!mApp.mResourceManager.LoadResources("TrophyRoomNights"))
            {
                mApp.ShowResourceError(true);
            }
            if (!Res.ExtractTrophyRoomNightsResources(mApp.mResourceManager, true))
            {
                mApp.ShowResourceError(true);
            }
            mUpdateCnt = 0;
            mState = STATE_NONE;
            mHoverTrophy = -1;
            mIdleSpeechTimer = SexyUtil.Rand() % 1000 + 150;
            mCloseTo = theCloseTo;
            mStateDuration = 100;
            mShake = false;

            mOkLink = new HyperlinkWidget(0, this);
            mOkLink.SetFont(SexyUtil.GetFontById(8));
            mOkLink.mColor = SexyUtil.MakeColor(0xffff88);
            mOkLink.mOverColor = SexyUtil.MakeColor(0x88ff88);
            mOkLink.mDoFinger = true;
            mOkLink.mLabel = mCloseTo == CLOSE_TO_ADVENTURE_SCREEN ? SexyAppBase.Loc("Back") : SexyAppBase.Loc("Continue");
            mOkLink.mUnderlineSize = PROGRESS_OK_BT_UNDERLINE_SIZE;
            mOkLink.mUnderlineOffset = mOkLink.mFont.GetDescent() - mOkLink.mUnderlineSize;
            int aWidth = mOkLink.mFont.StringWidth(mOkLink.mLabel) + 8;
            mOkLink.Resize(PROGRESS_OK_BT_X - aWidth / 2, PROGRESS_OK_BT_Y, aWidth, PROGRESS_OK_BT_PAD_H + mOkLink.mFont.GetHeight());
            AddWidget(mOkLink);
            mOkLink.SetVisible(false);

            bool aStarted = false;
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (aPlayer != null)
            {
                if (mTrophy == -1)
                {
                    mTrophy = aPlayer.GetNextTrophyToDisplay();
                }
                aPlayer.RefreshTrophyWins();
                if (aPlayer.mPortraitsShown < aPlayer.GetmMaxStage() - 11 && mApp.mPlayerInfo.mPortraitsShown < 11)
                {
                    SetState(STATE_SEEK_PORTRAIT);
                    mUpdateCnt = -150;
                    aStarted = true;
                }
                else
                {
                    mTrophy = aPlayer.GetNextTrophyToDisplay();
                    if (mTrophy != -1)
                    {
                        SetState(STATE_SEEK_TROPHY);
                        mUpdateCnt = -150;
                        aStarted = true;
                    }
                }
            }
            if (!aStarted)
            {
                SetState(STATE_IDLE);
            }
        }

        public int GetCloseTo()
        {
            return mCloseTo;
        }

        public static void GetTrophyCenterParticleData(int theTrophy, out int theX, out int theY, out int theA, out int theB, out int theW, out int theH)
        {
            if (theTrophy >= 0 && theTrophy < 4)
            {
                int[] d = PROGRESS_TROPHY_DATA[theTrophy];
                theX = d[0] * LAYOUT_SCALE_FACTOR;
                theY = d[1] * LAYOUT_SCALE_FACTOR;
                theW = d[2] * LAYOUT_SCALE_FACTOR;
                theH = d[3] * LAYOUT_SCALE_FACTOR;
                theA = d[4] * LAYOUT_SCALE_FACTOR;
                theB = d[5] * LAYOUT_SCALE_FACTOR;
            }
            else
            {
                theX = 0;
                theY = 0;
                theW = 50 * LAYOUT_SCALE_FACTOR;
                theH = 50 * LAYOUT_SCALE_FACTOR;
                theA = 0;
                theB = 0;
            }
        }

        public static void GetTrophyDrawLoc(int theTrophy, out int theX, out int theY, out int theHoverX, out int theHoverY, out int theW, out int theH)
        {
            if (theTrophy >= 0 && theTrophy < 4)
            {
                int[] d = PROGRESS_TROPHY_DATA[theTrophy];
                theX = d[6] * LAYOUT_SCALE_FACTOR;
                theY = d[7] * LAYOUT_SCALE_FACTOR;
                theHoverX = d[8] * LAYOUT_SCALE_FACTOR;
                theHoverY = d[9] * LAYOUT_SCALE_FACTOR;
                theW = d[2] * LAYOUT_SCALE_FACTOR;
                theH = d[3] * LAYOUT_SCALE_FACTOR;
            }
            else
            {
                theX = 0;
                theY = 0;
                theHoverX = 0;
                theHoverY = 0;
                theW = 50 * LAYOUT_SCALE_FACTOR;
                theH = 50 * LAYOUT_SCALE_FACTOR;
            }
        }

        public static void GetPortraitCenterLoc(int theStage, out int theBubbleX, out int theBubbleY, out int theMidX, out int theMidY, out int theW, out int theH, out bool theCircular)
        {
            int aChar = SexyUtil.GetCharacterForStage(theStage, 1);
            theMidX = 0;
            theMidY = 0;
            theW = 10;
            theH = 10;
            theCircular = false;
            if (aChar >= 0 && aChar <= 10)
            {
                theBubbleX = PROGRESS_SPEECH_BUBBLE_XS[aChar];
                theBubbleY = PROGRESS_SPEECH_BUBBLE_YS[aChar];
                theMidX = PROGRESS_PORTRAIT_MIDXS[aChar];
                theMidY = PROGRESS_PORTRAIT_MIDYS[aChar];
                theW = PROGRESS_PORTRAIT_WS[aChar];
                theH = PROGRESS_PORTRAIT_HS[aChar];
#if PEGGLE_IPHONE
                theCircular = PROGRESS_PORTRAIT_CIRCULAR[aChar];
#endif
            }
            else
            {
                theBubbleX = -100;
                theBubbleY = -100;
            }
            theBubbleX *= LAYOUT_SCALE_FACTOR;
            theBubbleY *= LAYOUT_SCALE_FACTOR;
            theMidX *= LAYOUT_SCALE_FACTOR;
            theMidY *= LAYOUT_SCALE_FACTOR;
            theW *= LAYOUT_SCALE_FACTOR;
            theH *= LAYOUT_SCALE_FACTOR;
        }

        public static float GetPortraitExtraScaling(int theStage)
        {
            switch (SexyUtil.GetCharacterForStage(theStage, 1))
            {
            case 0:
            case 1:
            case 3:
            case 4:
            case 9:
                return 0.6f;
            case 2:
            case 5:
            case 6:
            case 7:
            case 8:
            case 10:
                return 0.5f;
            default:
                return 0f;
            }
        }

        public static void GetPortraitSpeechInfo(int theStage, out int theX, out int theY, out int thePlacement)
        {
            GetPortraitCenterLoc(theStage, out theX, out theY, out _, out _, out _, out _, out _);
            float dx;
            float dy;
            switch (SexyUtil.GetCharacterForStage(theStage, 1))
            {
            case 0: dx = PROGRESS_PORTRAIT_BJORN_SPEECH_X; dy = PROGRESS_PORTRAIT_BJORN_SPEECH_Y; thePlacement = 2; break;
            case 1: dx = PROGRESS_PORTRAIT_JIMMY_SPEECH_X; dy = PROGRESS_PORTRAIT_JIMMY_SPEECH_Y; thePlacement = 4; break;
            case 2: dx = PROGRESS_PORTRAIT_TUT_SPEECH_X; dy = PROGRESS_PORTRAIT_TUT_SPEECH_Y; thePlacement = 7; break;
            case 3: dx = PROGRESS_PORTRAIT_SPLORK_SPEECH_X; dy = PROGRESS_PORTRAIT_SPLORK_SPEECH_Y; thePlacement = 1; break;
            case 4: dx = PROGRESS_PORTRAIT_CLAUDE_SPEECH_X; dy = PROGRESS_PORTRAIT_CLAUDE_SPEECH_Y; thePlacement = 3; break;
            case 5: dx = PROGRESS_PORTRAIT_RENFIELD_SPEECH_X; dy = PROGRESS_PORTRAIT_RENFIELD_SPEECH_Y; thePlacement = 2; break;
            case 6: dx = PROGRESS_PORTRAIT_TULA_SPEECH_X; dy = PROGRESS_PORTRAIT_TULA_SPEECH_Y; thePlacement = 2; break;
            case 7: dx = PROGRESS_PORTRAIT_WARREN_SPEECH_X; dy = PROGRESS_PORTRAIT_WARREN_SPEECH_Y; thePlacement = 4; break;
            case 8: dx = PROGRESS_PORTRAIT_CINDER_SPEECH_X; dy = PROGRESS_PORTRAIT_CINDER_SPEECH_Y; thePlacement = 7; break;
            case 9: dx = PROGRESS_PORTRAIT_HU_SPEECH_X; dy = PROGRESS_PORTRAIT_HU_SPEECH_Y; thePlacement = 4; break;
            case 10: dx = PROGRESS_PORTRAIT_KULAH_SPEECH_X; dy = PROGRESS_PORTRAIT_KULAH_SPEECH_Y; thePlacement = 4; break;
            default:
                thePlacement = 0;
                return;
            }
            theX = (int)(theX + dx);
            theY = (int)(theY + dy);
        }

        private SexyVector2 PortraitCenter(int theStage)
        {
            GetPortraitCenterLoc(theStage, out int x, out int y, out _, out _, out _, out _, out _);
            return new SexyVector2(x, y);
        }

        public void SetState(int theState)
        {
            mState = theState;
            mUpdateCnt = 0;
            mShake = false;
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            switch (theState)
            {
            case STATE_IDLE:
                mOkLink.SetVisible(true);
                break;
            case STATE_SEEK_PORTRAIT:
                mApp.StopMusic();
                mApp.PlaySample(Res.SOUND_DRUMROLL);
                if (aPlayer.GetmMaxStage() - 11 != aPlayer.mPortraitsShown)
                {
                    SexyVector2 aTarget = PortraitCenter(aPlayer.mPortraitsShown);
                    float aScale = GetPortraitExtraScaling(aPlayer.mPortraitsShown);
                    if (!mSpotlight.IsPaused())
                    {
                        mSpotlight.Start(aTarget, aScale);
                    }
                    else
                    {
                        mSpotlight.Seek(aTarget, aScale);
                    }
                }
                break;
            case STATE_SHOW_PORTRAIT:
            {
                mApp.PlaySample(Res.SOUND_TROPHY_PHOTO_IN);
                mStateDuration = 200;
                GetPortraitCenterLoc(aPlayer.mPortraitsShown, out _, out _, out int aMidX, out int aMidY, out int aW, out int aH, out bool aCircular);
                mParticles.Reset();
                for (int i = 0; i < 6; i++)
                {
                    mParticles.AddParticleImageId((int)ResourceId.IMAGE_TR_NIGHTS_PARTICLE1_ID + i);
                }
                mParticles.mSpawnRate.SetCurve("b+0,1,0,1,#### %d6)1 @~###   pGmD-   m#M:'");
                mParticles.mPos = new SexyVector2(aMidX, aMidY);
                mParticles.mAlpha = 1.0f;
                mParticles.mWidth = aW;
                mParticles.mHeight = aH;
                mParticles.mCircular = aCircular;
                mParticles.mDuration = mStateDuration;
                mParticles.mAdditive = true;
                MarkDirty();
                break;
            }
            case STATE_7:
                mEndingCurtains.Load(NightsEndingScreen.SceneScale);
                break;
            case STATE_SEEK_TROPHY:
                mCloseTo = CLOSE_TO_MAIN_MENU_TROPHY;
                mApp.StopMusic();
                mApp.PlaySample(Res.SOUND_DRUMROLL);
                showNextTrophy(false);
                break;
            case STATE_SHOW_TROPHY:
            {
                mTrophy = aPlayer.GetNextTrophyToDisplay();
                GetTrophyCenterParticleData(mTrophy, out int aX, out int aY, out _, out _, out int aW, out int aH);
                mStateDuration = 400;
                mCloseTo = CLOSE_TO_MAIN_MENU_TROPHY;
                mParticles.Reset();
                for (int i = 0; i < 6; i++)
                {
                    mParticles.AddParticleImageId((int)ResourceId.IMAGE_TR_NIGHTS_PARTICLE1_ID + i);
                }
                mParticles.mPos = new SexyVector2(aX, aY);
                mParticles.mWidth = aW;
                mParticles.mHeight = aH;
                mParticles.mCircular = false;
                mParticles.mAdditive = true;
                mParticles.mAlpha = 1.0f;
                mParticles.mDuration = mStateDuration;
                if (mTrophy == 0)
                {
                    mParticles.mSpawnRate.SetCurve("b+0,0.5,0,1,#### xSdx9 =~### (~###   -PmD-  Z#M:'");
                }
                else
                {
                    mParticles.mSpawnRate.SetCurve("b+0,0.5,0,1,#### `Z5GC H~Ws|B~###  5]l#s z1rED  I#GAY");
                }
                MarkDirty();
                break;
            }
            }
        }

        public override void Dispose()
        {
            mApp.mResourceManager.DeleteResources("TrophyRoomNights");
            mEndingCurtains.Dispose();
            for (int i = 0; i < mBubbles.Length; i++)
            {
                mBubbles[i] = null;
            }
            mOldBubbles.Clear();
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        private static void Shuffle<T>(List<T> theList)
        {
            // std::random_shuffle
            for (int i = 1; i < theList.Count; i++)
            {
                int j = SexyUtil.Rand() % (i + 1);
                T aTemp = theList[i];
                theList[i] = theList[j];
                theList[j] = aTemp;
            }
        }

        public bool TryAddIdleSpeech()
        {
            int aHover = -1;
            for (int i = 0; i < 4; i++)
            {
                if (1.0f <= mTrophyGlow[i])
                {
                    if (i != mHoverTrophy)
                    {
                        mIdleSpeechTimer = Math.Min(mIdleSpeechTimer, 10);
                    }
                    aHover = i;
                    break;
                }
            }
            if (aHover == -1)
            {
                mHoverTrophy = -1;
            }
            for (int i = 0; i < mBubbles.Length; i++)
            {
                if (mBubbles[i] != null)
                {
                    return false;
                }
            }
            bool aShown = false;
            bool aHovered = false;
            mIdleSpeechTimer--;
            if (mIdleSpeechTimer < 1)
            {
                if (aHover == -1)
                {
                    // A random revealed master says one of their idle lines.
                    if (mIdleStages.Count == 0)
                    {
                        for (int i = 0; i < 11; i++)
                        {
                            mIdleStages.Add(i);
                        }
                        Shuffle(mIdleStages);
                    }
                    CharacterInfo anInfo = null;
                    int aStage = 0;
                    bool aFound = false;
                    while (anInfo == null && mIdleStages.Count != 0)
                    {
                        aStage = mIdleStages[mIdleStages.Count - 1];
                        mIdleStages.RemoveAt(mIdleStages.Count - 1);
                        anInfo = mApp.mCharacterMgr.GetCharacterInfo(SexyUtil.GetCharacterForStage(aStage, ThunderballApp.mGameLevelPack));
                        if (anInfo != null && mApp.mPlayerInfo.GetmMaxStage() - 11 <= aStage)
                        {
                            anInfo = null;
                        }
                        if (anInfo == null)
                        {
                            continue;
                        }
                        List<int> aLines = mIdleLines[aStage];
                        if (aLines.Count == 0)
                        {
                            for (int i = 0; i < anInfo.mTrophyIdle.Count; i++)
                            {
                                aLines.Add(i);
                            }
                            Shuffle(aLines);
                        }
                        aFound = aLines.Count != 0;
                        break;
                    }
                    if (aFound)
                    {
                        List<int> aLines = mIdleLines[aStage];
                        ShowSpeech(aStage, anInfo.mTrophyIdle[aLines[aLines.Count - 1]], 400, 0, true);
                        aLines.RemoveAt(aLines.Count - 1);
                        aShown = true;
                    }
                }
                else
                {
                    // Hovering a trophy: masters comment on it.
                    aHovered = true;
                    if ((aHover != mHoverTrophy || mTrophyLines.Count == 0) && (aHover != mHoverTrophy || aHover != 0))
                    {
                        mHoverTrophy = aHover;
                        mTrophyLines.Clear();
                        getTrophyWinDialogue(aHover, mTrophyLines);
                        Shuffle(mTrophyLines);
                    }
                    aShown = mTrophyLines.Count != 0;
                    if (aShown)
                    {
                        KeyValuePair<int, string> aLine = mTrophyLines[mTrophyLines.Count - 1];
                        ShowSpeech(SexyUtil.GetStageForCharacter(aLine.Key), aLine.Value, 400, 0, true);
                        mTrophyLines.RemoveAt(mTrophyLines.Count - 1);
                    }
                }
            }
            if (aShown || aHovered)
            {
#if PEGGLE_IPHONE
                mIdleSpeechTimer = SexyUtil.Rand() % 800 + 750;
#else
                mIdleSpeechTimer = SexyUtil.Rand() % 200 + 10;
#endif
            }
            return aShown;
        }

        public void getTrophyWinDialogue(int theTrophy, List<KeyValuePair<int, string>> theList)
        {
            theList.Clear();
            for (int i = 0; i < 11; i++)
            {
                CharacterInfo anInfo = mApp.mCharacterMgr.GetCharacterInfo(i);
                if (anInfo == null)
                {
                    continue;
                }
                string aLine;
                switch (theTrophy)
                {
                case 0: aLine = anInfo.mTrophyWinAdventure; break;
                case 1: aLine = anInfo.mTrophyWinAllClear; break;
                case 2: aLine = anInfo.mTrophyWinChallenge; break;
                case 3: aLine = anInfo.mTrophyWinAce; break;
                default: aLine = ""; break;
                }
                if (!string.IsNullOrEmpty(aLine))
                {
                    theList.Add(new KeyValuePair<int, string>(i, aLine));
                }
            }
        }

        public SpeechBubble ShowSpeech(int theStage, string theText, int theExpireTime, int theDelay, bool unused)
        {
            SpeechBubble anOld = mBubbles[theStage];
            if (anOld != null)
            {
                if (!anOld.Hide(0))
                {
                    return null;
                }
                mOldBubbles.Add(anOld);
                mBubbles[theStage] = null;
            }
            GetPortraitSpeechInfo(theStage, out int x, out int y, out int aPlacement);
            SpeechBubble aBubble = new SpeechBubble();
            aBubble.SetFontPadding(NIGHTS_TROPHY_ROOM_SPEECH_BUBBLE_PAD_X, NIGHTS_TROPHY_ROOM_SPEECH_BUBBLE_PAD_Y);
            aBubble.SetTextMaxWidth(PROGRESS_SPEECH_TEXT_MAX_W);
            aBubble.SetFontJustification(0);
            aBubble.SetText(theText, SexyUtil.GetFontById(0x19), SexyUtil.MakeColor(0x494a8f, 0xff));
            aBubble.Move(x, y, aPlacement, 0, 0);
            aBubble.Show(theDelay);
            aBubble.SetExpireTime(theExpireTime);
            aBubble.SetMinTextHeight(PROGRESS_SPEECH_TEXT_MIN_H);
            foreach (SpeechBubble anOther in mBubbles)
            {
                if (anOther != null && !anOther.IsHidden())
                {
                    anOther.Hide(0);
                }
            }
            mBubbles[theStage] = aBubble;
            return aBubble;
        }

        public override void Update()
        {
            mTrophyRise.SetCurve("b+0,1,0.01,1,####   o;]d* tV3HE    <~P##");
            if (mApp.mStoryScreen == null)
            {
                mWidgetManager.SetFocus(this);
            }
            int aNumUpdates = 1;
            if (GlobalStaticVars.gSexyAppBase.mWidgetManager.IsRightButtonDown() && mState != STATE_NONE && mState != STATE_IDLE_LINK && mState != STATE_IDLE)
            {
                aNumUpdates = 4; // debug fast-forward
            }
            for (int i = 0; i < aNumUpdates; i++)
            {
                DoSingleUpdate();
            }
        }

        public void DoSingleUpdate()
        {
            if (mEndingShown)
            {
                return;
            }
            bool aHelixSpawn = false;
            mSpotlight.Update();
            base.Update();
            mParticles.Update();
            MarkDirty();
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            UpdateTrophySparkles();
            switch (mState)
            {
            case STATE_IDLE_LINK:
            case STATE_IDLE:
                if (mState == STATE_IDLE_LINK && mUpdateCnt > 20)
                {
                    mOkLink.SetVisible(true);
                }
                if (mApp.mAdventureScreen == null)
                {
                    TryAddIdleSpeech();
                    CheckAddRandomTrophySparkle();
                    for (int i = 0; i < mTrophyGlow.Count; i++)
                    {
                        GetTrophyDrawLoc(i, out _, out _, out int aHoverX, out int aHoverY, out int aW, out int aH);
                        TRect aRect = new TRect(aHoverX, aHoverY, aW, aH);
                        WidgetManager aWM = mWidgetManager;
                        if (aPlayer == null || aPlayer.mTrophyWinsDisplayed[i] == 0 || !aRect.Contains(aWM.mLastMouseX, aWM.mLastMouseY))
                        {
                            mTrophyGlow[i] = Math.Max(mTrophyGlow[i] - 0.02f, 0.0f);
                        }
                        else
                        {
                            mTrophyGlow[i] = Math.Min(mTrophyGlow[i] + 0.02f, 1.0f);
                        }
                    }
                }
                break;
            case STATE_SEEK_PORTRAIT:
                if (0.7f < mSpotlight.GetSpotlightSeekPct())
                {
                    SetState(STATE_SHOW_PORTRAIT);
                }
                break;
            case STATE_SHOW_PORTRAIT:
                MarkDirty();
                if (mSpotlight.IsPaused() && mStateDuration < mUpdateCnt)
                {
                    if (aPlayer == null)
                    {
                        SetState(STATE_IDLE_LINK);
                        break;
                    }
                    if (aPlayer.mPortraitsShown < aPlayer.GetmMaxStage() - 11)
                    {
                        CharacterInfo anInfo = mApp.mCharacterMgr.GetCharacterInfo(SexyUtil.GetCharacterForStage(aPlayer.mPortraitsShown, ThunderballApp.mGameLevelPack));
                        ShowSpeech(aPlayer.mPortraitsShown, anInfo.mTrophyWin, -1, 0, true);
                    }
                    aPlayer.mPortraitsShown++;
                    aPlayer.SaveDetails();
                    if (aPlayer.mPortraitsShown < aPlayer.GetmMaxStage() - 11 && aPlayer.mPortraitsShown < 11)
                    {
                        SetState(STATE_SEEK_PORTRAIT);
                        mSpotlight.Seek(PortraitCenter(aPlayer.mPortraitsShown), GetPortraitExtraScaling(aPlayer.mPortraitsShown));
                        break;
                    }
                    if (aPlayer.GetNextTrophyToDisplay() != -1)
                    {
                        SetState(STATE_SEEK_TROPHY);
                        showNextTrophy(true);
                        break;
                    }
                    if (mSpotlight.IsPaused())
                    {
                        mSpotlight.Finish();
                    }
                    mApp.PlayProgressMusic();
                    SetState(STATE_IDLE_LINK);
                }
                break;
            case STATE_SEEK_TROPHY:
                if (0.8f < mSpotlight.GetSpotlightSeekPct())
                {
                    SetState(STATE_SHOW_TROPHY);
                }
                break;
            case STATE_SHOW_TROPHY:
                MarkDirty();
                if (aPlayer != null)
                {
                    int aNext = aPlayer.GetNextTrophyToDisplay();
                    // Port: the iPhone had no intro, award or applause sounds here; they play as on the PC.
                    if (mUpdateCnt == 2)
                    {
                        mApp.PlaySample(Res.SOUND_TROPHY_WIN_INTRO);
                    }
                    if ((aNext == 0 && mUpdateCnt == 270) || (aNext != 0 && mUpdateCnt == 200))
                    {
                        mApp.PlaySample(Res.SOUND_AWARD);
                    }
                    else if ((aNext == 0 && mUpdateCnt == 305) || (aNext != 0 && mUpdateCnt == 230))
                    {
                        mApp.PlaySample(Res.SOUND_APPLAUSE);
                    }
                    else if (mUpdateCnt == 375)
                    {
                        ShowTrophyWinChorus(aNext);
                    }
                    if (mSpotlight.IsPaused())
                    {
                        if (mStateDuration < mUpdateCnt)
                        {
                            if (aNext != -1)
                            {
                                aPlayer.mTrophyWinsDisplayed[aNext] = 1;
                                aPlayer.SaveDetails();
                            }
                            if (aPlayer.GetNextTrophyToDisplay() == -1)
                            {
                                if (mSpotlight.IsPaused())
                                {
                                    mSpotlight.Finish();
                                }
                                SetState(STATE_IDLE_LINK);
                            }
                            else
                            {
                                SetState(STATE_SEEK_TROPHY);
                                showNextTrophy(true);
                            }
                        }
                        else if (mStateDuration > 50 && aPlayer.GetNextTrophyToDisplay() == 0)
                        {
                            aHelixSpawn = true;
                        }
                    }
                }
                break;
            case STATE_7:
                // Port: the PC's ending (the iOS kept the state but nothing reaches it). The curtain
                // closes to applause while the Continue link goes off to the right, then the credits room.
                mApp.StopMusic();
                if (mUpdateCnt == 20)
                {
                    if (sApplauseSound == -2)
                    {
                        sApplauseSound = mApp.mSoundManager.LoadSound("sounds/nights_ending_applause.wav");
                    }
                    if (sApplauseSound >= 0)
                    {
                        mApp.PlaySample(sApplauseSound);
                    }
                }
                if (mOkLink.mX < mWidth + 100)
                {
                    mOkLink.Move(mOkLink.mX + (int)(30 * SexyUtil.gPhysicalDisplayHeight / 600.0f), mOkLink.mY);
                }
#if PEGGLE_IPHONE
                // The trophy room's curtains close in to the 4:3 edges first, so no gap shows beside the
                // CONGRATULATIONS! curtain as it drops.
                if (mUpdateCnt < ENDING_SIDES_CLOSE_TIME)
                {
                    break;
                }
#endif
                mCurveDC.IncInVal();
                if (mCurveDC.HasBeenTriggered())
                {
                    mEndingShown = true;
                    mApp.ShowNightsEnding(mTrophy, aPlayer != null ? aPlayer.GetNumTrophiesWon(false) : 0);
                    return;
                }
                break;
            }

            for (int i = 0; i < mBubbles.Length; i++)
            {
                if (mBubbles[i] != null)
                {
                    mBubbles[i].Update();
                    if (mBubbles[i].IsHidden() && !mBubbles[i].IsStatePending())
                    {
                        mBubbles[i] = null;
                    }
                }
            }
            int j = 0;
            while (j < mOldBubbles.Count)
            {
                SpeechBubble aBubble = mOldBubbles[j];
                aBubble.Update();
                if (aBubble.IsHidden() && !aBubble.IsStatePending())
                {
                    mOldBubbles.RemoveAt(j);
                }
                else
                {
                    aBubble.Hide(0);
                    j++;
                }
            }
            mHelix.Update(aHelixSpawn);
        }

        /// <summary>Every master congratulates the player at once, without overlapping bubbles.</summary>
        private void ShowTrophyWinChorus(int theTrophy)
        {
            List<KeyValuePair<int, string>> aLines = new List<KeyValuePair<int, string>>();
            getTrophyWinDialogue(theTrophy, aLines);
            Shuffle(aLines);
            int aDelay = 100;
            List<SpeechBubble> aPlaced = new List<SpeechBubble>(new SpeechBubble[aLines.Count]);
            if (theTrophy == 0)
            {
                int i = 0;
                while (i < aLines.Count)
                {
                    if (aLines[i].Key == 11)
                    {
                        i++;
                    }
                    else
                    {
                        aLines.RemoveAt(i);
                    }
                }
            }
            else
            {
                // Lay the bubbles out once to drop any that would overlap an earlier one.
                int i = 0;
                while (i < aLines.Count)
                {
                    SpeechBubble aBubble = ShowSpeech(SexyUtil.GetStageForCharacter(aLines[i].Key), aLines[i].Value, -1, 0, false);
                    bool anOverlap = false;
                    if (aBubble != null)
                    {
                        TRect anExtents = aBubble.GetExtents();
                        foreach (SpeechBubble anOther in aPlaced)
                        {
                            if (anOther != null && anOther.GetExtents().Intersects(anExtents))
                            {
                                anOverlap = true;
                                break;
                            }
                        }
                    }
                    if (aBubble == null || anOverlap)
                    {
                        aLines.RemoveAt(i);
                    }
                    else
                    {
                        aPlaced.Add(aBubble);
                        i++;
                    }
                }
                for (int k = 0; k < mBubbles.Length; k++)
                {
                    mBubbles[k] = null;
                }
            }
            for (int i = 0; i < aLines.Count; i++)
            {
                ShowSpeech(SexyUtil.GetStageForCharacter(aLines[i].Key), aLines[i].Value, -1, aDelay, true);
                aDelay = SexyUtil.Rand() % 15 + 20 + aDelay;
            }
        }

        public void UpdateTrophySparkles()
        {
            int i = 0;
            while (i < mSparkles.Count)
            {
                mSparkles[i].mAge++;
                if (mSparkles[i].mAge < 60)
                {
                    i++;
                }
                else
                {
                    mSparkles.RemoveAt(i);
                }
            }
        }

        public void CheckAddRandomTrophySparkle()
        {
            if (gSparkleCoords == null)
            {
                gSparkleCoords = new int[gSparkleCoordsRaw.Length];
                for (int i = 0; i < gSparkleCoordsRaw.Length; i++)
                {
                    gSparkleCoords[i] = (int)((double)(SexyUtil.gPhysicalDisplayHeight * gSparkleCoordsRaw[i]) / 600.0);
                }
            }
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (aPlayer == null)
            {
                return;
            }
            const int aNumPoints = 13;
            int aNumWon = 0;
            for (int i = 0; i < aPlayer.mTrophyWins.Count; i++)
            {
                if (aPlayer.mTrophyWins[i] != 0)
                {
                    aNumWon++;
                }
            }
            if (mSparkles.Count >= 10 || SexyUtil.Rand() % 150 >= aNumWon)
            {
                return;
            }
            int aStartTrophy = SexyUtil.Rand() % 4;
            int aTrophy = aStartTrophy;
            do
            {
                if (aPlayer.mTrophyWins[aTrophy] != 0)
                {
                    GetTrophyDrawLoc(aTrophy, out int aX, out int aY, out _, out _, out _, out _);
                    int aStart = SexyUtil.Rand() % aNumPoints;
                    int aPoint = (aStart + 1) % aNumPoints;
                    do
                    {
                        float aPX = gSparkleCoords[aTrophy * 26 + aPoint * 2] + aX;
                        float aPY = gSparkleCoords[aTrophy * 26 + aPoint * 2 + 1] + aY;
                        bool aFree = true;
                        foreach (TrophySparkle aSparkle in mSparkles)
                        {
                            if (aPX == aSparkle.mPos.x && aPY == aSparkle.mPos.y)
                            {
                                aFree = false;
                                break;
                            }
                        }
                        if (aFree)
                        {
                            mSparkles.Add(new TrophySparkle { mPos = new SexyVector2(aPX, aPY), mAge = 0 });
                            return;
                        }
                        aPoint = (aPoint + 1) % aNumPoints;
                    }
                    while (aPoint != aStart);
                }
                aTrophy = (aTrophy + 1) % 4;
            }
            while (aTrophy != aStartTrophy);
        }

        public void showNextTrophy(bool theSeek)
        {
            mTrophy = mApp.mPlayerInfo.GetNextTrophyToDisplay();
            GetTrophyCenterParticleData(mTrophy, out int aX, out int aY, out _, out _, out _, out _);
            SexyVector2 aTarget = new SexyVector2(aX, aY);
            float aScale = mTrophy == 0 ? 0.75f : 0.5f;
            if (!theSeek)
            {
                mSpotlight.Start(aTarget, aScale);
            }
            else
            {
                mSpotlight.Seek(aTarget, aScale);
            }
        }

        public override void Draw(Graphics g)
        {
#if PEGGLE_IPHONE
            // The trophy room art spreads over the gutters.
            g.PushState();
            g.ClearClipRect();
#endif
            int aShakeX = 0;
            int aShakeY = 0;
            if (mShake)
            {
                aShakeX = SexyUtil.Rand() % PROGRESS_SHAKE_EQ_A - PROGRESS_SHAKE_EQ_B;
                aShakeY = SexyUtil.Rand() % PROGRESS_SHAKE_EQ_A - PROGRESS_SHAKE_EQ_B;
                g.Translate(aShakeX, aShakeY);
            }
            g.SetColor(SexyUtil.MakeColor(0));
            g.FillRect(new TRect(0, 0, mWidth, mHeight));
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            int aTime = Math.Max(0, mUpdateCnt);
#if PEGGLE_IPHONE
            g.DrawImage(Res.IMAGE_TR_NIGHTS_TROPHYROOM_SCREEN, (mWidth - Res.IMAGE_TR_NIGHTS_TROPHYROOM_SCREEN.mWidth) / 2 - SexyUtil.gGutterWidth, 0);
#else
            g.DrawImage(Res.IMAGE_TR_NIGHTS_TROPHYROOM_SCREEN, 0, 0);
#endif
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (aPlayer != null)
            {
                if (mState == STATE_SHOW_PORTRAIT)
                {
                    float aFade = (float)aTime / mStateDuration;
                    for (int i = 0; i < Math.Min(aPlayer.mPortraitsShown + 1, SexyUtil.gCharCount); i++)
                    {
                        DrawPortraitPost(g, i, i == aPlayer.mPortraitsShown ? aFade : 1.0f);
                    }
                }
                else
                {
                    for (int i = 0; i < Math.Min(aPlayer.mPortraitsShown, SexyUtil.gCharCount); i++)
                    {
                        DrawPortraitPost(g, i, 1.0f);
                    }
                }
            }
            mParticles.Draw(g);
            mHelix.Draw(g, false);
            if (aPlayer != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (aPlayer.mTrophyWinsDisplayed[i] != 0)
                    {
                        DrawTrophy(g, i, false);
                    }
                }
                if (mState == STATE_SHOW_TROPHY)
                {
                    mTrophy = aPlayer.GetNextTrophyToDisplay();
                    if (mTrophy != -1)
                    {
                        DrawTrophy(g, mTrophy, true);
                    }
                }
            }
            mHelix.Draw(g, true);
            mSpotlight.Draw(g, mState == STATE_SHOW_TROPHY || mState == STATE_SEEK_TROPHY);
            mParticles.DrawOverlay(g);
            DrawTrophySparkles(g);
            foreach (SpeechBubble aBubble in mBubbles)
            {
                aBubble?.Draw(g, 0, 0);
            }
            foreach (SpeechBubble aBubble in mOldBubbles)
            {
                aBubble.Draw(g, 0, 0);
            }
            if (aShakeX != 0 || aShakeY != 0)
            {
                g.Translate(-aShakeX, -aShakeY);
            }
            DeferOverlay(0);
#if PEGGLE_IPHONE
            g.PopState();
#endif
        }

        public void DrawPortraitPost(Graphics g, int theStage, float theAlpha)
        {
            int aChar = SexyUtil.GetCharacterForStage(theStage, ThunderballApp.mGameLevelPack);
            Image anImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_TR_NIGHTS_PORTRAIT_01_ID + aChar);
            int x = 0;
            int y = 0;
            switch (aChar)
            {
            case 0: x = PROGRESS_PORTRAIT_BJORN_X; y = PROGRESS_PORTRAIT_BJORN_Y; break;
            case 1: x = PROGRESS_PORTRAIT_JIMMY_X; y = PROGRESS_PORTRAIT_JIMMY_Y; break;
            case 2: x = PROGRESS_PORTRAIT_TUT_X; y = PROGRESS_PORTRAIT_TUT_Y; break;
            case 3: x = PROGRESS_PORTRAIT_SPLORK_X; y = PROGRESS_PORTRAIT_SPLORK_Y; break;
            case 4: x = PROGRESS_PORTRAIT_CLAUDE_X; y = PROGRESS_PORTRAIT_CLAUDE_Y; break;
            case 5: x = PROGRESS_PORTRAIT_RENFIELD_X; y = PROGRESS_PORTRAIT_RENFIELD_Y; break;
            case 6: x = PROGRESS_PORTRAIT_TULA_X; y = PROGRESS_PORTRAIT_TULA_Y; break;
            case 7: x = PROGRESS_PORTRAIT_WARREN_X; y = PROGRESS_PORTRAIT_WARREN_Y; break;
            case 8: x = PROGRESS_PORTRAIT_CINDER_X; y = PROGRESS_PORTRAIT_CINDER_Y; break;
            case 9: x = PROGRESS_PORTRAIT_HU_X; y = PROGRESS_PORTRAIT_HU_Y; break;
            case 10: x = PROGRESS_PORTRAIT_KULAH_X; y = PROGRESS_PORTRAIT_KULAH_Y; break;
            }
            if (theAlpha < 1.0f)
            {
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(theAlpha * 255.0f)));
                g.SetColorizeImages(true);
            }
            g.DrawImage(anImage, x, y);
            if (theAlpha < 1.0f)
            {
                g.SetColorizeImages(false);
                g.SetColor(SexyUtil.MakeColor(0xffffff));
            }
        }

        public void DrawTrophy(Graphics g, int theTrophy, bool theRevealing)
        {
            Image anImage = PlayerInfo.TrophyToImage(theTrophy);
            GetTrophyDrawLoc(theTrophy, out int aX, out int aY, out _, out _, out _, out _);
            if (!theRevealing)
            {
                g.DrawImage(anImage, aX, aY);
                if (0.0f < mTrophyGlow[theTrophy])
                {
                    g.SetColorizeImages(true);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    g.SetColor(SexyUtil.MakeColor(0xffcc55, (int)(mTrophyGlow[theTrophy] * 100.0f)));
                    g.DrawImage(anImage, aX, aY);
                    g.SetColor(Color.White);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                    g.SetColorizeImages(false);
                }
            }
            else if ((uint)(theTrophy - 1) < 3)
            {
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mUpdateCnt * 255.0f / mStateDuration)));
                g.SetColorizeImages(true);
                g.DrawImage(anImage, aX, aY);
                g.SetColorizeImages(false);
                g.SetColor(SexyUtil.MakeColor(0xffffff));
            }
            else if (theTrophy == 0)
            {
                // The adventure trophy rises out of a glowing hole in the floor.
                const int aFadeTime = 20;
                float aScale = 1.0f;
                mShake = false;
                if (mUpdateCnt < aFadeTime)
                {
                    aScale = (float)mUpdateCnt / aFadeTime;
                }
                else if (mStateDuration - aFadeTime < mUpdateCnt)
                {
                    aScale = (float)(mStateDuration - mUpdateCnt) / aFadeTime;
                }
                else if (60 < mUpdateCnt && mUpdateCnt < mStateDuration - 70)
                {
                    mShake = true;
                }
                Image aHole = Res.IMAGE_TR_NIGHTS_HOLE;
                Image aGlow = Res.IMAGE_CATCHERGLOW;
                int aHoleW = (int)(aHole.GetWidth() * aScale);
                int aHoleH = (int)(aHole.GetHeight() * aScale);
                int aGlowW = (int)(aGlow.GetWidth() * aScale);
                int aGlowH = (int)(aGlow.GetHeight() * Math.Pow(aScale, 2.0));
                aGlowW = (int)(aGlowW * 1.32);
                aGlowH = (int)(aGlowH * 1.5);
                int aHoleX = PROGRESS_TROPHY_HOLE_X;
                int aHoleY = PROGRESS_TROPHY_HOLE_Y;
                g.DrawImage(aHole, aHoleX - aHoleW / 2, aHoleY - aHoleH / 2, aHoleW, aHoleH);
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffff00, 0xff));
                g.DrawImage(aGlow, aHoleX + 1 - aGlowW / 2, (int)(aHoleY - aGlowH * 0.9f), aGlowW, aGlowH);
                g.SetColor(Color.White);
                g.SetColorizeImages(false);
                int aW = anImage.GetWidth();
                int aH = anImage.GetHeight();
                int aFloorY = PROGRESS_TROPHY_HOLE_Y + Res.IMAGE_TR_NIGHTS_TABLE_BOTTOM.GetHeight();
                double aRise = (double)(mUpdateCnt - aFadeTime - 100) / (double)(mStateDuration - aFadeTime - 110);
                aRise = Math.Min(1.0, Math.Max(0.0, aRise));
                aRise = mTrophyRise.GetOutVal(aRise);
                int aDrawY = (int)(aY + (aFloorY - aY) * (1.0 - aRise));
                if (aDrawY < aFloorY)
                {
                    int aVisH = Math.Min(aH, aFloorY - aDrawY);
                    g.DrawImageBox(new TRect(0, 0, aW, aVisH), new TRect(aX, aDrawY, aW, aVisH), anImage);
                }
                g.DrawImage(Res.IMAGE_TR_NIGHTS_TABLE_BOTTOM, PROGRESS_TABLE_BOTTOM_X, PROGRESS_TABLE_BOTTOM_Y);
            }
        }

        public void DrawTrophySparkles(Graphics g)
        {
            mSparkleScale.SetCurve("b+0,1,0.01,1,####    iz###     8####");
            mSparkleAlpha.SetCurve("b+0,1,0.01,1,####y~###        %~### &####");
            g.SetColorizeImages(true);
            Image anImage = Res.IMAGE_TR_NIGHTS_SPARKLE;
            foreach (TrophySparkle aSparkle in mSparkles)
            {
                double aScale = mSparkleScale.GetOutVal(aSparkle.mAge / 60.0f);
                double anAlpha = mSparkleAlpha.GetOutVal(aSparkle.mAge / 60.0f);
                int w = anImage.GetWidth();
                int h = anImage.GetHeight();
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(anAlpha * 255.0)));
                g.DrawImage(anImage, (int)(aSparkle.mPos.x - w * aScale / 2.0), (int)(aSparkle.mPos.y - h * aScale / 2.0),
                    (int)(w * aScale), (int)(h * aScale));
            }
            g.SetColorizeImages(false);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
        }

        public override void DrawOverlay(Graphics g)
        {
            if (mState == STATE_7)
            {
                foreach (SpeechBubble aBubble in mBubbles)
                {
                    aBubble?.Hide(0);
                }
                DrawEndingCurtains(g);
                return;
            }
#if PEGGLE_IPHONE
            DrawCurtain(g);
#endif
        }

        /// <summary>
        /// Port: state 7's curtains (the PC's vf44): the CONGRATULATIONS! curtain drops over the 4:3 middle.
        /// HD: between the PC's side curtains, as on the PC. iPhone: the trophy room's own curtains first close
        /// in to the 4:3 edges (ENDING_SIDES_CLOSE_TIME), on any window (they cover the 4:3 scene's sides),
        /// then it drops; they stay there for the credits room (NightsEndingScreen).
        /// </summary>
        private void DrawEndingCurtains(Graphics g)
        {
            double aCurve = mCurveDC.GetOutVal();
#if PEGGLE_IPHONE
            // In the screen's coordinates (the trophy room sits at the gutter).
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            aWide.SetColorizeImages(false);
            aWide.mTransX -= SexyUtil.gGutterWidth;
            mEndingCurtains.DrawDrop(aWide, mWidth, mHeight, aCurve);
            NightsCurtains.GetOpenSides(mWidth, 0, out int aLeft, out int aRight);
            if (!NightsCurtains.ShowSides())
            {
                // Not shown in the trophy room on this window: they come in from its edges.
                ThunderDialog.GetWindowMargins(out int aMarginL, out int aMarginR);
                aLeft = Math.Min(aLeft, -aMarginL);
                aRight = Math.Max(aRight, mWidth + aMarginR);
            }
            NightsCurtains.GetSceneSides(mWidth, mHeight, out int aLeft43, out int aRight43);
            float t = Math.Min(1.0f, (float)mUpdateCnt / ENDING_SIDES_CLOSE_TIME);
            float aClosed = 1 - (1 - t) * (1 - t);
            NightsCurtains.DrawSides(aWide, mHeight, (int)(aLeft + (aLeft43 - aLeft) * aClosed), (int)(aRight + (aRight43 - aRight) * aClosed));
#else
            mEndingCurtains.DrawDrop(g, mWidth, mHeight, aCurve);
            mEndingCurtains.DrawPCSides(g, mWidth, mHeight);
#endif
        }

#if PEGGLE_IPHONE
        /// <summary>
        /// Port addition (ultrawide): on a window wider than the game's 16:9 (NightsCurtains.ShowSides), the
        /// curtain (open in the middle) is drawn over the whole room, centred on the screen, to frame it out
        /// to the window's edges. It's only drawn, so clicks and hovers reach the room as before.
        /// </summary>
        private void DrawCurtain(Graphics g)
        {
            if (!NightsCurtains.ShowSides())
            {
                return;
            }
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            aWide.SetColorizeImages(false);
            aWide.mTransX -= SexyUtil.gGutterWidth;
            NightsCurtains.GetOpenSides(mWidth, 0, out int aLeft, out int aRight);
            NightsCurtains.DrawSides(aWide, mHeight, aLeft, aRight);
        }
#endif

        public override void MouseDown(int x, int y, int theClickCount)
        {
            if (mUpdateCnt > 5)
            {
                mOkLink.SetVisible(true);
            }
            for (int i = mBubbles.Length - 1; i >= 0; i--)
            {
                if (mBubbles[i] != null && mBubbles[i].GetExtents().Contains(x, y))
                {
                    mBubbles[i].Hide(0);
                    mOldBubbles.Add(mBubbles[i]);
                    mBubbles[i] = null;
                    return;
                }
            }
        }

        public void ButtonPress(int theId)
        {
        }

        public void ButtonPress(int theId, int theClickCount)
        {
        }

        public void ButtonDepress(int theId)
        {
            if (mApp.mAdventureScreen != null)
            {
                return;
            }
            if (theId == 0)
            {
                if (mState == STATE_7)
                {
                    return;
                }
                PlayerInfo aPlayer = mApp.mPlayerInfo;
                if (aPlayer != null)
                {
                    // Skip whatever reveal is pending.
                    aPlayer.mTrophyWinsDisplayed = new List<int>(aPlayer.mTrophyWins);
                    aPlayer.mPortraitsShown = aPlayer.GetmMaxStage() - 11;
                }
                for (int i = 0; i < mBubbles.Length; i++)
                {
                    if (mBubbles[i] != null)
                    {
                        mBubbles[i].Hide(0);
                        mOldBubbles.Add(mBubbles[i]);
                        mBubbles[i] = null;
                    }
                }
                switch (mCloseTo)
                {
                case CLOSE_TO_STORY:
                case -1:
                    mApp.EndProgressScreen();
                    break;
                case CLOSE_TO_ADVENTURE_SCREEN:
                    mApp.ShowAdventureScreen(true);
                    break;
                case CLOSE_TO_MAIN_MENU:
                    mApp.ShowMainMenu();
                    break;
                case CLOSE_TO_MAIN_MENU_TROPHY:
                    // Port: as on the PC, a newly won trophy closes the curtain on to the credits room
                    // (the iOS went back to the main menu).
                    SetState(STATE_7);
                    break;
                }
            }
            else if (theId == 100)
            {
                dbgPrevChar();
            }
            else if (theId == 101)
            {
                dbgNextChar();
            }
            else if (theId == 102)
            {
                SetState(STATE_SHOW_PORTRAIT);
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

        public void dbgPrevChar()
        {
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            for (int i = aPlayer.mTrophyWinsDisplayed.Count - 1; i >= 0; i--)
            {
                if (aPlayer.mTrophyWinsDisplayed[i] != 0)
                {
                    aPlayer.mTrophyWinsDisplayed[i] = 0;
                    aPlayer.mTrophyWins[i] = 0;
                    return;
                }
                if (aPlayer.mTrophyWins[i] != 0)
                {
                    aPlayer.mTrophyWins[i] = 0;
                }
            }
            aPlayer.SetmMaxStage(Math.Max(0, aPlayer.GetmMaxStage() - 1));
            aPlayer.mPortraitsShown = aPlayer.GetmMaxStage() - 11;
        }

        public void dbgNextChar()
        {
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (aPlayer.GetmMaxStage() - 11 < 11)
            {
                aPlayer.SetmMaxStage(aPlayer.GetmMaxStage() + 1);
                SetState(STATE_SEEK_PORTRAIT);
            }
            else
            {
                for (int i = 0; i < aPlayer.mTrophyWins.Count; i++)
                {
                    if (aPlayer.mTrophyWins[i] == 0)
                    {
                        aPlayer.mTrophyWins[i] = 1;
                        break;
                    }
                }
                if (aPlayer.GetNextTrophyToDisplay() != -1)
                {
                    SetState(STATE_SEEK_TROPHY);
                }
            }
        }

        public override void KeyChar(SexyChar theChar)
        {
        }

        // ---------------------------------------------------------------- port addition: menu navigation

        /// <summary>Port addition (menu navigation): B is the OK link, once it is shown.</summary>
        bool INavHandler.NavAction(NavAction theAction)
        {
            if (theAction == NavAction.Back)
            {
                if (MenuNav.IsTarget(mOkLink))
                {
                    MenuNav.Activate(mOkLink);
                }
                return true;
            }
            return false;
        }
    }
}
