using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Port addition: the PC Peggle Nights ending (PeggleNights.exe Sexy::CreditsScreen, 0x4db3d0-0x4df2f0),
    /// shown by the trophy room (ProgressScreen state 7) once a newly won trophy has been revealed and
    /// Continue pressed: the credits room, its CONGRATULATIONS! curtain rising, the spotlight and the
    /// trophy on its pedestal, and the credits played on the room's dot-matrix screen from the PC credit
    /// script (levels/credits_nights.txt, the PC stages.cfg's Credit: lines), with ticker tape falling.
    /// The PC screen is 800x600 scaled to the display height and centred; the PC window's blue frame hid
    /// its edges, so the scene is zoomed in (SCENE_ZOOM) instead. iPhone: the trophy room's curtains
    /// (TrophyNights_curtain) frame the scene at its 4:3 edges in place of the PC's side curtains, where
    /// the trophy room closed them. Art: images/pc/nights_ending (tools/build_nights_ending_assets.py).
    /// </summary>
    public class NightsEndingScreen : Widget, ButtonListener
    {
        /// <summary>The PC window's blue frame (IMAGE_DLG_BLUEFRAME) covered this many PC px of each edge.</summary>
        public const int BORDER_PC = 11;

        /// <summary>The scene is zoomed about its centre so the strip the blue frame hid doesn't show.</summary>
        public const float SCENE_ZOOM = 600.0f / (600 - BORDER_PC * 2);

        private const string ART = "images/pc/nights_ending/";

        /// <summary>The dot-matrix screen: its first light (PC px), its size in lights, one light per 4 PC px.</summary>
        private const int LED_X = 228;

        private const int LED_Y = 51;

        private const int LED_COLS = 125;

        private const int LED_ROWS = 129;

        private const int LED_PITCH = 4;

        /// <summary>Where the Continue button stops (its x is centred on 400), as the PC link's (0x215).</summary>
        private const int BUTTON_Y = 0x215;

        public ThunderballApp mApp;

        private readonly int mTrophy;

        private readonly int mTrophyCount;

        private float S;

        private float OX;

        private float OY;

        private readonly NightsCurtains mCurtains = new NightsCurtains();

        private readonly NightsEndingArt.SpriteSet mArt = new NightsEndingArt.SpriteSet();

        private NightsEndingArt.Sprite mRoom;

        private NightsEndingArt.Sprite mLight;

        private NightsEndingArt.Sprite mPedestal;

        private NightsEndingArt.Sprite mTrophyImage;

        private Point mTrophyPos;

        private readonly NightsEndingArt.Sprite[] mTickers = new NightsEndingArt.Sprite[2];

        /// <summary>Update count (PC this+0x120).</summary>
        private int mCount;

        /// <summary>The curtain rising (+0x1c0), the spotlight coming on (+0x250), the room lighting up (+0x2e0).</summary>
        private readonly CurvedVal mCurtainCurve = new CurvedVal("b+0,1,0.01,1.5,#&z(         ~~`m0");

        private readonly CurvedVal mLightCurve = new CurvedVal("b+0,1,0.01,1.2,####        n#####~###^y###S~###");

        private readonly CurvedVal mRoomCurve = new CurvedVal("b+0,1,0.01,2,####  .####      _~### 7~###");

        /// <summary>The ticker tape's sideways drift (+0x128).</summary>
        private readonly CurvedVal mDriftCurve = new CurvedVal("b+-0.05,0.5,0.01,10,+###84eNIL6524?<kHo81}&&L:+<j7C7UX;R3y6<P###4R###>I###FL@:JPT###?P###NX###MH###YO###kA###]S###WM###DT###]D###7H###9;###?J###S+###");

        private readonly CreditPlayer mCredits;

        private readonly List<Ticker> mTickerTape = new List<Ticker>();

        /// <summary>Ticker tape to make: 0.08 a frame (+0xb0), the running total (+0xb8).</summary>
        private double mTickerTotal;

        private readonly ThunderButton mContinueButton;

        private static int sCurtainsSound = -2;

        private static int sSpotlightSound = -2;

        private static int sFanfareSound = -2;

        private class Ticker
        {
            public int mAge;

            public double mX;

            public int mLife;

            public double mScale;

            public double mAngle;

            public int mImage;

            public Color mColor;

            /// <summary>Drawn behind the pedestal (+0x3c), else in front of everything.</summary>
            public bool mBehind;
        }

        public NightsEndingScreen(ThunderballApp theApp, int theTrophy, int theTrophyCount)
        {
            mApp = theApp;
            mTrophy = theTrophy;
            mTrophyCount = theTrophyCount;
            float aScale = SceneScale;
            mArt.mScale = aScale;
            mRoom = mArt.Get("creditroom");
            mLight = mArt.Get("light");
            mPedestal = mArt.Get("creditpedistal");
            string aTrophyName;
            switch (theTrophy)
            {
            case 0: aTrophyName = "adventure"; mTrophyPos = new Point(100, 0x195); break;
            case 1: aTrophyName = "allclear"; mTrophyPos = new Point(99, 0x17a); break;
            case 2: aTrophyName = "challenge"; mTrophyPos = new Point(0x5b, 0x184); break;
            case 3: aTrophyName = "ace"; mTrophyPos = new Point(0x85, 0x1a7); break;
            default: aTrophyName = "beethoven"; mTrophyPos = new Point(0x6a, 0x14d); break;
            }
            mTrophyImage = mArt.Get(aTrophyName);
            for (int i = 0; i < 2; i++)
            {
                mTickers[i] = mArt.Get("ticker" + (i + 1));
            }
            mCurtains.Load(aScale);
            if (sCurtainsSound == -2)
            {
                sCurtainsSound = mApp.mSoundManager.LoadSound("sounds/nights_ending_curtains.wav");
                sSpotlightSound = mApp.mSoundManager.LoadSound("sounds/nights_ending_spotlight.wav");
                sFanfareSound = mApp.mSoundManager.LoadSound("sounds/nights_ending_fanfare.wav");
            }

            string aName = mApp.mPlayerInfo != null ? mApp.mPlayerInfo.mName : "";
            mCredits = new CreditPlayer(theTrophy, theTrophyCount, aName);
            mCredits.Build();

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
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            mArt.Dispose();
            mCurtains.Dispose();
            mCredits.Clear();
            NightsEndingArt.ReleaseBits();
            base.Dispose();
        }

        /// <summary>Display px per PC px.</summary>
        public static float SceneScale => SexyUtil.gPhysicalDisplayHeight / 600.0f * SCENE_ZOOM;

        /// <summary>The PC screen's scale and top-left in a theWidth x theHeight screen, centred.</summary>
        public static void GetScene(int theWidth, int theHeight, out float theScale, out float theX, out float theY)
        {
            theScale = theHeight / 600.0f * SCENE_ZOOM;
            theX = (theWidth - 800 * theScale) / 2;
            theY = (theHeight - 600 * theScale) / 2;
        }

        private int PX(float x) => (int)Math.Round(OX + x * S);

        private int PY(float y) => (int)Math.Round(OY + y * S);

        private int ButtonRestX => PX(400) - mContinueButton.mWidth / 2;

        private int ButtonRestY => PY(BUTTON_Y + 18) - mContinueButton.mHeight / 2;

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            GetScene(mWidth, mHeight, out S, out OX, out OY);
            if (mContinueButton.mVisible)
            {
                mContinueButton.Move(ButtonRestX, ButtonRestY);
            }
        }

        /// <summary>vf22 (FUN_004dd400).</summary>
        public override void Update()
        {
            base.Update();
            MarkDirty();
            if (mCount == 700)
            {
                mApp.PlayMusic(ThunderballApp.NIGHTS_CREDITS_SONG, true);
            }
            if (mCount == 80)
            {
                PlaySound(sCurtainsSound);
            }
            else if (mCount > 100)
            {
                if (!mCurtainCurve.HasBeenTriggered())
                {
                    mCurtainCurve.IncInVal();
                }
                else if (!mLightCurve.HasBeenTriggered())
                {
                    if (mCount == 0x14c)
                    {
                        PlaySound(sSpotlightSound);
                    }
                    mLightCurve.IncInVal();
                }
                else if (!mRoomCurve.HasBeenTriggered())
                {
                    if (mCount == 0x19a)
                    {
                        PlaySound(sFanfareSound);
                    }
                    mRoomCurve.IncInVal();
                    mCredits.Update();
                }
                else
                {
                    mDriftCurve.IncInVal();
                    mCredits.Update();
                    if (mCredits.IsEmpty())
                    {
                        mCredits.Build();
                    }
                    UpdateTickerTape();
                }
            }
            // The Continue button comes in from the left, 30 PC px an update.
            if (mContinueButton.mVisible && mContinueButton.mX != ButtonRestX)
            {
                mContinueButton.Move(Math.Min(ButtonRestX, mContinueButton.mX + (int)(30 * S)), ButtonRestY);
            }
            mCount++;
        }

        private void PlaySound(int theSound)
        {
            if (theSound >= 0)
            {
                mApp.PlaySample(theSound);
            }
        }

        /// <summary>FUN_004dd5b0: new ticker tape, and the old drifting down until it runs out.</summary>
        private void UpdateTickerTape()
        {
            int aBefore = (int)mTickerTotal;
            mTickerTotal += 0.08;
            for (int i = aBefore; i < (int)mTickerTotal; i++)
            {
                Ticker aTicker = new Ticker();
                aTicker.mX = SexyUtil.Rand() % 900 - 100.0;
                aTicker.mImage = SexyUtil.Rand() % 2;
                aTicker.mLife = SexyUtil.Rand() % 300 + 500;
                double r = RandUnit();
                aTicker.mScale = r * 0.3 + 0.4;
                aTicker.mAngle = RandUnit() * Math.PI * 2;
                aTicker.mBehind = r < 0.5;
                int c = SexyUtil.Rand() % 55 + 200;
                switch (SexyUtil.Rand() % 6)
                {
                case 0: aTicker.mColor = new Color(c, 0, 0); break;
                case 1: aTicker.mColor = new Color(0, c, 0); break;
                case 2: aTicker.mColor = new Color(0, 0, c); break;
                case 3: aTicker.mColor = new Color(0, c, c); break;
                case 4: aTicker.mColor = new Color(c, c, 0); break;
                default: aTicker.mColor = new Color(c, 0, c); break;
                }
                mTickerTape.Add(aTicker);
            }
            for (int i = 0; i < mTickerTape.Count;)
            {
                Ticker aTicker = mTickerTape[i];
                if (aTicker.mAge == aTicker.mLife)
                {
                    mTickerTape.RemoveAt(i);
                    continue;
                }
                aTicker.mX += mDriftCurve.GetOutVal(mDriftCurve.GetInVal() + (double)aTicker.mAge / aTicker.mLife * 3);
                aTicker.mAge++;
                i++;
            }
        }

        private static double RandUnit()
        {
            return (SexyUtil.Rand() & 0x7fff) / 32767.0;
        }

        /// <summary>vf26.</summary>
        public override void Draw(Graphics theG)
        {
            GetScene(mWidth, mHeight, out S, out OX, out OY);
            Graphics g = PCScene.Wide(theG);
            int aMarginL = 0;
            int aMarginR = 0;
#if PEGGLE_IPHONE
            ThunderDialog.GetWindowMargins(out aMarginL, out aMarginR);
#endif
            g.SetColor(Color.Black);
            g.FillRect(-aMarginL, 0, mWidth + aMarginL + aMarginR, mHeight);

            // The room, dark until it lights up.
            double aRoom = mRoomCurve.GetOutVal();
            SetGrey(g, aRoom, 205, 50);
            DrawPC(g, mRoom, 0, 0);
            g.SetColorizeImages(false);
            g.SetColor(Color.White);

            Graphics aScreenG = g.Create();
            aScreenG.ClipRect(PX(LED_X), PY(LED_Y), (int)Math.Round(LED_COLS * LED_PITCH * S), (int)Math.Round(LED_ROWS * LED_PITCH * S));
            mCredits.Draw(aScreenG, this);

            double aLight = mLightCurve.GetOutVal();
            if (mCurtainCurve.HasBeenTriggered())
            {
                if (aLight != 0 && aLight != 1)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(aLight * 255)));
                }
                DrawPC(g, mLight, 5, 0);
                g.SetColor(Color.White);
                g.SetColorizeImages(false);
            }
            DrawTickerTape(g, true);

            SetGrey(g, aLight, 165, 90);
            DrawPC(g, mPedestal, 0x54, 0x210);
            DrawPC(g, mTrophyImage, mTrophyPos.X, mTrophyPos.Y);
            g.SetColorizeImages(false);
            g.SetColor(Color.White);

            mCurtains.DrawDrop(g, mWidth, mHeight, mCurtainCurve.HasBeenTriggered() ? -1 : mCurtainCurve.GetOutVal());
#if PEGGLE_IPHONE
            // On any window: they cover the 4:3 scene's sides.
            NightsCurtains.GetSceneSides(mWidth, mHeight, out int aLeft, out int aRight);
            NightsCurtains.DrawSides(g, mHeight, aLeft, aRight);
#else
            mCurtains.DrawPCSides(g, mWidth, mHeight);
#endif
            DrawTickerTape(g, false);
        }

        /// <summary>A grey (theScale * theValue + theBase) tint, unless theValue is 1.</summary>
        private static void SetGrey(Graphics g, double theValue, int theScale, int theBase)
        {
            if (theValue == 1)
            {
                return;
            }
            int c = (int)(theValue * theScale + theBase);
            g.SetColorizeImages(true);
            g.SetColor(new Color(c, c, c));
        }

        private void DrawPC(Graphics g, NightsEndingArt.Sprite theSprite, float x, float y)
        {
            theSprite?.Draw(g, PX(x), PY(y));
        }

        /// <summary>FUN_004ddc50: each piece of ticker tape on its layer, spun, scaled and tinted, falling from -50 to 620.</summary>
        private void DrawTickerTape(Graphics g, bool theBehind)
        {
            g.SetColorizeImages(true);
            foreach (Ticker aTicker in mTickerTape)
            {
                NightsEndingArt.Sprite aSprite = mTickers[aTicker.mImage];
                if (aTicker.mBehind != theBehind || aSprite == null)
                {
                    continue;
                }
                // The tape's cels play back and forth (its AnimInfo: ping-pong, 5 updates a cel).
                const int aCels = 7;
                int aStep = aTicker.mAge / 5 % (aCels * 2 - 2);
                int aCel = aStep < aCels ? aStep : aCels * 2 - 2 - aStep;
                int aCelW = aSprite.mRect.mWidth / aCels;
                Transform aTrans = new Transform();
                aTrans.RotateRad((float)aTicker.mAngle);
                aTrans.Scale((float)aTicker.mScale, (float)aTicker.mScale);
                g.SetColor(aTicker.mColor);
                double y = -50 + 670.0 * aTicker.mAge / aTicker.mLife;
                g.DrawImageTransformF(aSprite.mImage, aTrans, new TRect(aSprite.mRect.mX + aCel * aCelW, aSprite.mRect.mY, aCelW, aSprite.mRect.mHeight), OX + (float)aTicker.mX * S, OY + (float)y * S);
            }
            g.SetColor(Color.White);
            g.SetColorizeImages(false);
        }

        /// <summary>vf54: a click (once the screen has been up a moment) shows the Continue button.</summary>
        public override void MouseDown(int x, int y, int theClickCount)
        {
            base.MouseDown(x, y, theClickCount);
            if (theClickCount == 1 && mCount > 0x4b && !mContinueButton.mVisible)
            {
                mContinueButton.Move(-100 - mContinueButton.mWidth, ButtonRestY);
                mContinueButton.SetVisible(true);
            }
        }

        public void ButtonPress(int theId)
        {
        }

        public void ButtonPress(int theId, int theClickCount)
        {
        }

        /// <summary>vf2: Continue goes back to the main menu (0x42f750).</summary>
        public void ButtonDepress(int theId)
        {
            if (theId == 0)
            {
                mApp.ShowMainMenu();
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

        /// <summary>
        /// The credits (FUN_004dba30 builds them, FUN_004de440 plays them, FUN_004dea70 draws them): the
        /// credit script's pages are drawn into pictures one pixel per light of the dot-matrix screen, each
        /// shown from its start time, sliding and fading in along its curves, held for its pause, then
        /// gone. When the last one has gone they're built and played again.
        /// </summary>
        private class CreditPlayer
        {
            private class Page
            {
                public int mWidth;

                public int mHeight;

                public uint[] mBits;
            }

            private class CreditEvent
            {
                public Page mPage;

                public int mStart;

                public int mPause;

                public int mOffsetX;

                public int mOffsetY;

                public int mFrameRate;

                public float mXMult;

                public float mYMult;

                public string mCurveX;

                public string mCurveY;

                public string mCurveAlpha;
            }

            private class CreditAnim
            {
                public CreditEvent mEvent;

                public CurvedVal mX;

                public CurvedVal mY;

                public CurvedVal mAlpha;

                public int mPause;

                public Image mLights;

                public float mLightsScale;
            }

            private static List<List<string>> sPages;

            private static readonly Dictionary<string, PixelFont> sFonts = new Dictionary<string, PixelFont>();

            private readonly int mTrophy;

            private readonly int mTrophyCount;

            private readonly string mUserName;

            private readonly List<CreditEvent> mEvents = new List<CreditEvent>();

            private readonly List<CreditAnim> mAnims = new List<CreditAnim>();

            private readonly Dictionary<string, Page> mImages = new Dictionary<string, Page>();

            private int mTime;

            public CreditPlayer(int theTrophy, int theTrophyCount, string theUserName)
            {
                mTrophy = theTrophy;
                mTrophyCount = theTrophyCount;
                mUserName = theUserName;
            }

            public bool IsEmpty()
            {
                return mEvents.Count == 0 && mAnims.Count == 0;
            }

            public void Clear()
            {
                foreach (CreditAnim anAnim in mAnims)
                {
                    anAnim.mLights?.Dispose();
                }
                mAnims.Clear();
                mEvents.Clear();
                mImages.Clear();
            }

            /// <summary>The credit script, split into pages at "newPage" (StageMgr's Credit list).</summary>
            private static List<List<string>> GetPages()
            {
                if (sPages != null)
                {
                    return sPages;
                }
                sPages = new List<List<string>>();
                byte[] aData = ContentFS.ReadAllBytes("levels/credits_nights.txt");
                if (aData == null)
                {
                    return sPages;
                }
                List<string> aPage = null;
                foreach (string aRawLine in Encoding.UTF8.GetString(aData).Split('\n'))
                {
                    string aLine = aRawLine.TrimEnd('\r');
                    if (aLine == "newPage")
                    {
                        aPage = new List<string>();
                        sPages.Add(aPage);
                    }
                    else
                    {
                        aPage?.Add(aLine);
                    }
                }
                return sPages;
            }

            private static PixelFont GetFont(string theName)
            {
                if (!sFonts.TryGetValue(theName, out PixelFont aFont))
                {
                    aFont = PixelFont.Load("font_" + theName);
                    sFonts[theName] = aFont;
                }
                return aFont;
            }

            private Page GetImage(string thePath)
            {
                // The script names the PC files (images/credits/name).
                string aName = thePath.Substring(thePath.LastIndexOf('/') + 1).ToLowerInvariant();
                if (!mImages.TryGetValue(aName, out Page aPage))
                {
                    uint[] aBits = NightsEndingArt.GetBits(aName, out int w, out int h);
                    aPage = aBits == null ? null : new Page { mWidth = w, mHeight = h, mBits = aBits };
                    mImages[aName] = aPage;
                }
                return aPage;
            }

            /// <summary>
            /// FUN_004dba30: starts over (time 0) and draws every page that's for this trophy and trophy
            /// count into a picture, scheduled at its start time. The settings carry from page to page.
            /// </summary>
            public void Build()
            {
                Clear();
                mTime = 0;
                int aLineHeight = 0;
                int aImageW = 0x7d;
                int aImageH = 0x81;
                float aXMult = 1;
                float aYMult = 1;
                Color aColor = Color.White;
                PixelFont aFont = null;
                int anOffsetX = 0;
                int anOffsetY = 0;
                int aTimeStart = 0;
                int aKeyframe = 0;
                int aTrophyId = -1;
                int aTrophyCount = -1;
                int aPause = 0;
                bool aCentered = true;
                string aCurveX = TransInCurve("none", "");
                string aCurveY = TransInCurve("none", "");
                string aCurveAlpha = TransInCurve("full", "");
                List<List<string>> aPages = GetPages();
                foreach (List<string> aLines in aPages)
                {
                    int aFrameRate = 0;
                    int y = 0;
                    int i = 0;
                    // Each page starts in mini7 (a new Graphics, given FONT_MINI7); the line height carries over.
                    aFont = GetFont("mini7");
                    Page aPage = new Page { mWidth = aImageW, mHeight = aImageH, mBits = new uint[aImageW * aImageH] };
                    Page aCanvas = aPage;
                    if (aImageH > 0)
                    {
                        do
                        {
                            if (i >= aLines.Count)
                            {
                                break;
                            }
                            for (; i < aLines.Count; i++)
                            {
                                string aLine = aLines[i];
                                int anEq = aLine.IndexOf('=');
                                if (anEq < 0)
                                {
                                    break;
                                }
                                bool anIncluded = (aTrophyId == mTrophy || aTrophyId == -1) && (aTrophyCount == mTrophyCount || aTrophyCount == -1);
                                string aKey = aLine.Substring(0, anEq);
                                string aValue = aLine.Substring(anEq + 1);
                                if (aKey == "trophyId")
                                {
                                    aTrophyId = AToL(aValue);
                                    continue;
                                }
                                if (aTrophyId != mTrophy && aTrophyId != -1)
                                {
                                    continue;
                                }
                                switch (aKey)
                                {
                                case "color":
                                    if (uint.TryParse(aValue.StartsWith("0x") ? aValue.Substring(2) : aValue, NumberStyles.HexNumber, null, out uint aHex))
                                    {
                                        int anAlpha = (int)(aHex >> 24);
                                        aColor = new Color((int)(aHex >> 16) & 0xff, (int)(aHex >> 8) & 0xff, (int)aHex & 0xff, anAlpha == 0 ? 255 : anAlpha);
                                    }
                                    break;
                                case "font":
                                {
                                    // The line height is the font's, less a pixel for most (five for arcade).
                                    int anAdjust;
                                    switch (aValue)
                                    {
                                    case "mini7": case "silk": anAdjust = 0; break;
                                    case "arcade": anAdjust = -5; break;
                                    case "mini7bold": case "mini76": case "silkscreen7": case "argontight9": anAdjust = -1; break;
                                    default: anAdjust = int.MinValue; break;
                                    }
                                    if (anAdjust != int.MinValue)
                                    {
                                        aFont = GetFont(aValue);
                                        aLineHeight = (aFont != null ? aFont.mHeight : 0) + anAdjust;
                                    }
                                    break;
                                }
                                case "timeStart": aTimeStart = AToL(aValue); break;
                                case "trophyCount": aTrophyCount = AToL(aValue); break;
                                case "timeStartKeyframe": aTimeStart = AToL(aValue) + aKeyframe; break;
                                case "keyframeAdd":
                                    if (anIncluded)
                                    {
                                        aKeyframe += AToL(aValue);
                                    }
                                    break;
                                case "transInXMult": aXMult = (float)AToF(aValue); break;
                                case "transInYMult": aYMult = (float)AToF(aValue); break;
                                case "centered": aCentered = AToL(aValue) == 1; break;
                                case "timePause": aPause = AToL(aValue); break;
                                case "offsetX": anOffsetX = AToL(aValue); break;
                                case "frameRate": aFrameRate = AToL(aValue); break;
                                case "offsetY": anOffsetY = AToL(aValue); break;
                                case "addOffsetY": anOffsetY += AToL(aValue); break;
                                case "imageWidth": aImageW = AToL(aValue); break;
                                case "imageHeight": aImageH = AToL(aValue); break;
                                case "transInX": aCurveX = TransInCurve(aValue, aValue); break;
                                case "transInY": aCurveY = TransInCurve(aValue, aValue); break;
                                case "transInAlpha": aCurveAlpha = TransInCurve(aValue, aValue); break;
                                case "image":
                                {
                                    Page anImage = GetImage(aValue);
                                    if (anImage != null)
                                    {
                                        BlitImage(aCanvas, anImage, aCanvas.mWidth / 2 - anImage.mWidth / 2, y);
                                        y += anImage.mHeight;
                                    }
                                    break;
                                }
                                case "imageOnly":
                                {
                                    Page anImage = GetImage(aValue);
                                    if (anImage != null)
                                    {
                                        aPage = anImage;
                                    }
                                    break;
                                }
                                case "blank": y += AToL(aValue); break;
                                }
                            }
                            if (i < aLines.Count)
                            {
                                // A line of text, its top at the pen.
                                string aText = aLines[i].Replace("%user%", mUserName);
                                if (aFont != null)
                                {
                                    int x = aCentered ? aCanvas.mWidth / 2 - aFont.StringWidth(aText) / 2 : 0;
                                    aFont.DrawString(aCanvas, aText, x, y, aColor);
                                }
                                y += aLineHeight;
                                i++;
                            }
                        }
                        while (y < aImageH);
                    }
                    if ((aTrophyId == mTrophy || aTrophyId == -1) && (aTrophyCount == mTrophyCount || aTrophyCount == -1))
                    {
                        mEvents.Add(new CreditEvent
                        {
                            mPage = aPage,
                            mStart = mTime + aTimeStart,
                            mPause = aPause,
                            mOffsetX = anOffsetX,
                            mOffsetY = anOffsetY,
                            mFrameRate = aFrameRate,
                            mXMult = aXMult,
                            mYMult = aYMult,
                            mCurveX = aCurveX,
                            mCurveY = aCurveY,
                            mCurveAlpha = aCurveAlpha
                        });
                    }
                }
            }

            /// <summary>FUN_004dcd00: the transIn presets; anything else is a curve of its own.</summary>
            private static string TransInCurve(string theName, string theRaw)
            {
                switch (theName)
                {
                case "fromLeft": return "b+-1,1,0.01,1,####         ~P###";
                case "toLeft": return "b+-1,1,0.01,1,P###         ~####";
                case "fromRight": return "b+-1,1,0.01,1,~###         ~P###";
                case "toRight": return "b+-1,1,0.01,1,P###         ~~###";
                case "none": case "emptyNone": return "b#0,0,0.01,0.01,~         ~~";
                case "full": case "emptyFull": return "b#1,1,0.01,0.01,~         ~~";
                default: return theRaw;
                }
            }

            private static CurvedVal MakeCurve(string theCurve)
            {
                CurvedVal aCurve = new CurvedVal();
                if (string.IsNullOrEmpty(theCurve))
                {
                    aCurve.SetConstant(0);
                }
                else
                {
                    aCurve.SetCurve(theCurve);
                }
                return aCurve;
            }

            /// <summary>FUN_004de440: starts what's due, moves along what's playing, and drops what's done.</summary>
            public void Update()
            {
                for (int i = 0; i < mEvents.Count;)
                {
                    CreditEvent anEvent = mEvents[i];
                    if (anEvent.mStart > mTime)
                    {
                        i++;
                        continue;
                    }
                    mAnims.Add(new CreditAnim
                    {
                        mEvent = anEvent,
                        mX = MakeCurve(anEvent.mCurveX),
                        mY = MakeCurve(anEvent.mCurveY),
                        mAlpha = MakeCurve(anEvent.mCurveAlpha),
                        mPause = anEvent.mPause
                    });
                    mEvents.RemoveAt(i);
                }
                for (int i = 0; i < mAnims.Count;)
                {
                    CreditAnim anAnim = mAnims[i];
                    if (!anAnim.mX.HasBeenTriggered() || !anAnim.mY.HasBeenTriggered() || !anAnim.mAlpha.HasBeenTriggered())
                    {
                        anAnim.mX.IncInVal();
                        anAnim.mY.IncInVal();
                        anAnim.mAlpha.IncInVal();
                    }
                    else if (anAnim.mPause > 0)
                    {
                        anAnim.mPause--;
                    }
                    else
                    {
                        anAnim.mLights?.Dispose();
                        mAnims.RemoveAt(i);
                        continue;
                    }
                    i++;
                }
                mTime++;
            }

            /// <summary>FUN_004dea70: each picture on the screen, its position snapped to the lights.</summary>
            public void Draw(Graphics g, NightsEndingScreen theScreen)
            {
                float aScale = theScreen.S;
                foreach (CreditAnim anAnim in mAnims)
                {
                    CreditEvent anEvent = anAnim.mEvent;
                    if (anEvent.mPage == null || anEvent.mFrameRate >= 1)
                    {
                        // A picture with a frame rate is drawn by its cels; these have only the one, so the PC draws nothing.
                        continue;
                    }
                    if (anAnim.mLights == null || anAnim.mLightsScale != aScale)
                    {
                        anAnim.mLights?.Dispose();
                        anAnim.mLights = MakeLights(anEvent.mPage, aScale);
                        anAnim.mLightsScale = aScale;
                    }
                    int x = (int)(anEvent.mXMult * anAnim.mX.GetOutVal() * LED_COLS * LED_PITCH);
                    int y = (int)(anEvent.mYMult * anAnim.mY.GetOutVal() * LED_ROWS * LED_PITCH);
                    x = x / LED_PITCH * LED_PITCH + anEvent.mOffsetX * LED_PITCH + LED_X;
                    y = y / LED_PITCH * LED_PITCH + anEvent.mOffsetY * LED_PITCH + LED_Y;
                    double anAlpha = anAnim.mAlpha.GetOutVal();
                    if (anAlpha != 1)
                    {
                        g.SetColorizeImages(true);
                        g.SetColor(SexyUtil.MakeColor(0xffffff, Math.Clamp((int)(anAlpha * 255), 0, 255)));
                    }
                    g.DrawImage(anAnim.mLights, theScreen.PX(x), theScreen.PY(y));
                    g.SetColor(Color.White);
                    g.SetColorizeImages(false);
                }
            }

            /// <summary>
            /// FUN_004df2f0: a picture as the screen shows it, each pixel a light half the pitch across with
            /// dark between, drawn straight at the display's scale.
            /// </summary>
            private static Image MakeLights(Page thePage, float theScale)
            {
                int w = Math.Max(1, (int)Math.Ceiling(thePage.mWidth * LED_PITCH * theScale));
                int h = Math.Max(1, (int)Math.Ceiling(thePage.mHeight * LED_PITCH * theScale));
                uint[] aBits = new uint[w * h];
                for (int j = 0; j < thePage.mHeight; j++)
                {
                    int y0 = (int)Math.Round(j * LED_PITCH * theScale);
                    int y1 = Math.Min(h, Math.Max(y0 + 1, (int)Math.Round((j * LED_PITCH + LED_PITCH / 2) * theScale)));
                    for (int i = 0; i < thePage.mWidth; i++)
                    {
                        uint p = thePage.mBits[j * thePage.mWidth + i];
                        uint a = p >> 24;
                        if (a == 0)
                        {
                            continue;
                        }
                        // Premultiplied, as the textures are.
                        uint r = (p & 0xff) * a / 255;
                        uint gr = ((p >> 8) & 0xff) * a / 255;
                        uint b = ((p >> 16) & 0xff) * a / 255;
                        uint q = (a << 24) | (b << 16) | (gr << 8) | r;
                        int x0 = (int)Math.Round(i * LED_PITCH * theScale);
                        int x1 = Math.Min(w, Math.Max(x0 + 1, (int)Math.Round((i * LED_PITCH + LED_PITCH / 2) * theScale)));
                        for (int y = y0; y < y1; y++)
                        {
                            for (int x = x0; x < x1; x++)
                            {
                                aBits[y * w + x] = q;
                            }
                        }
                    }
                }
                ImageLibImage anImage = new ImageLibImage();
                anImage.mWidth = w;
                anImage.mHeight = h;
                anImage.mBits = aBits;
                return anImage.CreateImage();
            }

            private static void BlitImage(Page theDest, Page theSrc, int theX, int theY)
            {
                for (int j = 0; j < theSrc.mHeight; j++)
                {
                    for (int i = 0; i < theSrc.mWidth; i++)
                    {
                        uint p = theSrc.mBits[j * theSrc.mWidth + i];
                        Blend(theDest, theX + i, theY + j, (int)(p & 0xff), (int)((p >> 8) & 0xff), (int)((p >> 16) & 0xff), (int)(p >> 24));
                    }
                }
            }

            /// <summary>Straight-alpha "over" into a page.</summary>
            private static void Blend(Page thePage, int x, int y, int r, int g, int b, int a)
            {
                if (a <= 0 || x < 0 || y < 0 || x >= thePage.mWidth || y >= thePage.mHeight)
                {
                    return;
                }
                int anIndex = y * thePage.mWidth + x;
                uint d = thePage.mBits[anIndex];
                int da = (int)(d >> 24);
                if (a >= 255 || da == 0)
                {
                    thePage.mBits[anIndex] = (uint)((a << 24) | (b << 16) | (g << 8) | r);
                    return;
                }
                int aDstPart = da * (255 - a) / 255;
                int oa = a + aDstPart;
                int or = (r * a + (int)(d & 0xff) * aDstPart) / oa;
                int og = (g * a + (int)((d >> 8) & 0xff) * aDstPart) / oa;
                int ob = (b * a + (int)((d >> 16) & 0xff) * aDstPart) / oa;
                thePage.mBits[anIndex] = (uint)((oa << 24) | (ob << 16) | (og << 8) | or);
            }

            /// <summary>C atol: the leading integer, 0 if none.</summary>
            private static int AToL(string theValue)
            {
                string s = theValue.Trim();
                int n = 0;
                while (n < s.Length && (char.IsDigit(s[n]) || (n == 0 && (s[n] == '-' || s[n] == '+'))))
                {
                    n++;
                }
                int.TryParse(s.Substring(0, n), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int aResult);
                return aResult;
            }

            private static double AToF(string theValue)
            {
                double.TryParse(theValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double aResult);
                return aResult;
            }

            /// <summary>
            /// A PC pixel font (fonts/*.txt, converted by tools/build_nights_ending_assets.py), drawn into the
            /// pages: each glyph at the pen plus its offset, the pen at the line's top left,
            /// tinted.
            /// </summary>
            private class PixelFont
            {
                public int mAscent;

                /// <summary>mini7's ascent: where a page's first baseline is.</summary>
                private const int MINI7_ASCENT = 6;

                public int mHeight;

                private readonly Dictionary<int, int[]> mChars = new Dictionary<int, int[]>();

                private readonly Dictionary<int, int> mKerning = new Dictionary<int, int>();

                /// <summary>The glyphs (the font's cel in the credits sheet).</summary>
                private uint[] mSheet;

                private int mSheetW;

                private int mSheetH;

                public static PixelFont Load(string theName)
                {
                    byte[] aData = ContentFS.ReadAllBytes(ART + theName + ".txt");
                    uint[] aSheet = NightsEndingArt.GetBits(theName, out int w, out int h);
                    if (aData == null || aSheet == null)
                    {
                        return null;
                    }
                    PixelFont aFont = new PixelFont { mSheet = aSheet, mSheetW = w, mSheetH = h };
                    string[] aLines = Encoding.UTF8.GetString(aData).Split('\n');
                    string[] aHeader = aLines[0].Split(' ');
                    aFont.mAscent = int.Parse(aHeader[0], CultureInfo.InvariantCulture);
                    aFont.mHeight = int.Parse(aHeader[1], CultureInfo.InvariantCulture);
                    for (int i = 1; i < aLines.Length; i++)
                    {
                        string[] f = aLines[i].Trim().Split(' ');
                        if (f[0] == "c" && f.Length == 9)
                        {
                            int[] v = new int[7];
                            for (int k = 0; k < 7; k++)
                            {
                                v[k] = int.Parse(f[k + 2], CultureInfo.InvariantCulture);
                            }
                            aFont.mChars[int.Parse(f[1], CultureInfo.InvariantCulture)] = v;
                        }
                        else if (f[0] == "k" && f.Length == 4)
                        {
                            aFont.mKerning[(int.Parse(f[1], CultureInfo.InvariantCulture) << 16) | int.Parse(f[2], CultureInfo.InvariantCulture)] =
                                int.Parse(f[3], CultureInfo.InvariantCulture);
                        }
                    }
                    return aFont;
                }

                private int Kerning(char thePrev, char theChar)
                {
                    return mKerning.TryGetValue((thePrev << 16) | theChar, out int k) ? k : 0;
                }

                public int StringWidth(string theText)
                {
                    int w = 0;
                    for (int i = 0; i < theText.Length; i++)
                    {
                        if (mChars.TryGetValue(theText[i], out int[] c))
                        {
                            w += c[6];
                        }
                        if (i > 0)
                        {
                            w += Kerning(theText[i - 1], theText[i]);
                        }
                    }
                    return w;
                }

                public void DrawString(Page thePage, string theText, int theX, int theY, Color theColor)
                {
                    int x = theX;
                    for (int i = 0; i < theText.Length; i++)
                    {
                        if (i > 0)
                        {
                            x += Kerning(theText[i - 1], theText[i]);
                        }
                        if (!mChars.TryGetValue(theText[i], out int[] c))
                        {
                            continue;
                        }
                        int aLeft = x + c[4];
                        // The PC's y is the baseline (y - ascent + offset). Port: each page's pen starts a mini7
                        // ascent lower, or a page's first line would be above the page; every font then shares
                        // the baseline, as on the PC (the headings sit higher than the mini7 lines under them).
                        int aTop = theY + MINI7_ASCENT - mAscent + c[5];
                        for (int j = 0; j < c[3]; j++)
                        {
                            for (int k = 0; k < c[2]; k++)
                            {
                                int sx = c[0] + k;
                                int sy = c[1] + j;
                                if (sx >= mSheetW || sy >= mSheetH)
                                {
                                    continue;
                                }
                                int a = (int)(mSheet[sy * mSheetW + sx] >> 24) * theColor.A / 255;
                                Blend(thePage, aLeft + k, aTop + j, theColor.R, theColor.G, theColor.B, a);
                            }
                        }
                        x += c[6];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Port addition: the curtains of the Nights ending, shared by the trophy room (ProgressScreen state 7)
    /// and the credits room (NightsEndingScreen), placed as NightsEndingScreen places the PC screen. The
    /// PC's CONGRATULATIONS! curtain (IMAGE_TROPHY_CURTAIN) drops to PC y 10 - 635 * its curve, between
    /// its side curtains (IMAGE_TROPHY_CURTAIN_L/R). iPhone: the trophy room's own curtains
    /// (TrophyNights_curtain) stand in for the side curtains, in two halves that close in to
    /// the 4:3 edges.
    /// </summary>
    public class NightsCurtains
    {
        /// <summary>
        /// TrophyNights_curtain: its opening's edges at their widest (the bottom; the edges aren't straight),
        /// so a half placed by them covers up to that line at every height, and its middle.
        /// </summary>
        private const int SIDES_OPEN_LEFT = 1145;

        private const int SIDES_OPEN_RIGHT = 2269;

        private const int SIDES_MID = 1707;

        private readonly NightsEndingArt.SpriteSet mArt = new NightsEndingArt.SpriteSet();

        private NightsEndingArt.Sprite mDrop;

        private NightsEndingArt.Sprite mLeft;

        private NightsEndingArt.Sprite mRight;

        private static Image sSides;

        public void Load(float theScale)
        {
            mArt.mScale = theScale;
            mDrop = mArt.Get("curtain");
#if !PEGGLE_IPHONE
            mLeft = mArt.Get("curtainl");
            mRight = mArt.Get("curtainr");
#endif
        }

        public void Dispose()
        {
            mArt.Dispose();
            mDrop = mLeft = mRight = null;
        }

        /// <summary>The CONGRATULATIONS! curtain at theCurve (0 down, 1 up); none when theCurve is negative.</summary>
        public void DrawDrop(Graphics g, int theWidth, int theHeight, double theCurve)
        {
            if (mDrop == null || theCurve < 0)
            {
                return;
            }
            NightsEndingScreen.GetScene(theWidth, theHeight, out float s, out float ox, out float oy);
            mDrop.Draw(g, (int)Math.Round(ox), (int)Math.Round(oy + (10 - theCurve * 635) * s));
        }

        /// <summary>The PC's side curtains, at the PC screen's edges.</summary>
        public void DrawPCSides(Graphics g, int theWidth, int theHeight)
        {
            if (mLeft == null || mRight == null)
            {
                return;
            }
            NightsEndingScreen.GetScene(theWidth, theHeight, out float s, out float ox, out float oy);
            mLeft.Draw(g, (int)Math.Round(ox), (int)Math.Round(oy));
            mRight.Draw(g, (int)Math.Round(ox + 800 * s) - mRight.mRect.mWidth, (int)Math.Round(oy));
        }

        /// <summary>iPhone: the trophy room's curtains, in halves whose inner edges are at theLeftEdge and theRightEdge.</summary>
        public static void DrawSides(Graphics g, int theHeight, int theLeftEdge, int theRightEdge)
        {
            if (sSides == null)
            {
                sSides = GlobalStaticVars.gSexyAppBase.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "TrophyNights_curtain", true);
                if (sSides == null)
                {
                    return;
                }
            }
            int y = (theHeight - sSides.mHeight) / 2;
            g.DrawImage(sSides, theLeftEdge - SIDES_OPEN_LEFT, y, new TRect(0, 0, SIDES_MID, sSides.mHeight));
            g.DrawImage(sSides, theRightEdge - (SIDES_OPEN_RIGHT - SIDES_MID), y, new TRect(SIDES_MID, 0, sSides.mWidth - SIDES_MID, sSides.mHeight));
        }

#if PEGGLE_IPHONE
        private const float SIDES_MIN_EXTRA_WIDTH = 5.0f;

        /// <summary>
        /// iPhone: whether the trophy room's curtains are shown: only on a window wider than the game's 16:9,
        /// by more than SIDES_MIN_EXTRA_WIDTH game pixels (nearly 16:9 screens don't get them).
        /// </summary>
        public static bool ShowSides()
        {
            ThunderDialog.GetWindowMargins(out int aLeft, out int aRight);
            if (aLeft == 0 && aRight == 0)
            {
                return false;
            }
            // The margins are rounded up per side, so measure the extra width unrounded.
            SexyAppBase anApp = GlobalStaticVars.gSexyAppBase;
            ScreenScales aScales = anApp.mScreenScales;
            return aScales.mWidth / aScales.mScaleFactor - anApp.mWidth > SIDES_MIN_EXTRA_WIDTH;
        }
#endif

        /// <summary>The 4:3 scene's edges (the PC screen less the blue frame's strip) in a theWidth x theHeight screen.</summary>
        public static void GetSceneSides(int theWidth, int theHeight, out int theLeft, out int theRight)
        {
            NightsEndingScreen.GetScene(theWidth, theHeight, out float s, out float ox, out _);
            theLeft = (int)Math.Round(ox + NightsEndingScreen.BORDER_PC * s);
            theRight = (int)Math.Round(ox + (800 - NightsEndingScreen.BORDER_PC) * s);
        }

        /// <summary>iPhone: the inner edges of the trophy room's curtains where ProgressScreen draws them open, centred on the screen.</summary>
        public static void GetOpenSides(int theScreenWidth, int theOffsetX, out int theLeft, out int theRight)
        {
            int aWidth = sSides != null ? sSides.mWidth : 3414;
            int x = (theScreenWidth - aWidth) / 2 - theOffsetX;
            theLeft = x + SIDES_OPEN_LEFT;
            theRight = x + SIDES_OPEN_RIGHT;
        }
    }

    /// <summary>
    /// Port addition: the Nights ending's art, packed into sprite sheets by tools/build_nights_ending_assets.py
    /// and listed in images/pc/nights_ending/sheets.txt ("name sheet x y w h", PC px).
    /// </summary>
    public static class NightsEndingArt
    {
        private const string ART = "images/pc/nights_ending/";

        private static Dictionary<string, KeyValuePair<string, TRect>> sCels;

        private static readonly Dictionary<string, ImageLibImage> sBits = new Dictionary<string, ImageLibImage>();

        /// <summary>A cel of a resampled sheet.</summary>
        public class Sprite
        {
            public Image mImage;

            public TRect mRect;

            public void Draw(Graphics g, int x, int y)
            {
                g.DrawImage(mImage, x, y, mRect);
            }
        }

        /// <summary>Sheets resampled to mScale, loaded as their cels are asked for, until Dispose.</summary>
        public class SpriteSet
        {
            public float mScale = 1;

            private readonly Dictionary<string, Image> mSheets = new Dictionary<string, Image>();

            public Sprite Get(string theName)
            {
                if (!GetCel(theName, out string aSheet, out TRect aRect))
                {
                    return null;
                }
                if (!mSheets.TryGetValue(aSheet, out Image anImage))
                {
                    anImage = SexyUtil.LoadPCImage(ART + aSheet, mScale);
                    mSheets[aSheet] = anImage;
                }
                return anImage == null ? null : new Sprite { mImage = anImage, mRect = SexyUtil.ScalePCRect(aRect, mScale) };
            }

            public void Dispose()
            {
                foreach (Image anImage in mSheets.Values)
                {
                    anImage?.Dispose();
                }
                mSheets.Clear();
            }
        }

        public static bool GetCel(string theName, out string theSheet, out TRect theRect)
        {
            if (sCels == null)
            {
                sCels = new Dictionary<string, KeyValuePair<string, TRect>>();
                byte[] aData = ContentFS.ReadAllBytes(ART + "sheets.txt");
                if (aData != null)
                {
                    foreach (string aLine in Encoding.UTF8.GetString(aData).Split('\n'))
                    {
                        string[] f = aLine.Trim().Split(' ');
                        if (f.Length == 6)
                        {
                            sCels[f[0]] = new KeyValuePair<string, TRect>(f[1], new TRect(int.Parse(f[2], CultureInfo.InvariantCulture),
                                int.Parse(f[3], CultureInfo.InvariantCulture), int.Parse(f[4], CultureInfo.InvariantCulture), int.Parse(f[5], CultureInfo.InvariantCulture)));
                        }
                    }
                }
            }
            if (sCels.TryGetValue(theName, out KeyValuePair<string, TRect> aCel))
            {
                theSheet = aCel.Key;
                theRect = aCel.Value;
                return true;
            }
            theSheet = null;
            theRect = new TRect();
            return false;
        }

        /// <summary>A cel's pixels at PC size (straight alpha), for drawing into the credit pages; null if missing.</summary>
        public static uint[] GetBits(string theName, out int theWidth, out int theHeight)
        {
            theWidth = 0;
            theHeight = 0;
            if (!GetCel(theName, out string aSheet, out TRect aRect))
            {
                return null;
            }
            if (!sBits.TryGetValue(aSheet, out ImageLibImage anImage))
            {
                anImage = ImageLib.GetImage(ART + aSheet, false);
                sBits[aSheet] = anImage;
            }
            if (anImage == null)
            {
                return null;
            }
            uint[] aBits = new uint[aRect.mWidth * aRect.mHeight];
            for (int y = 0; y < aRect.mHeight; y++)
            {
                Array.Copy(anImage.mBits, (aRect.mY + y) * anImage.mWidth + aRect.mX, aBits, y * aRect.mWidth, aRect.mWidth);
            }
            theWidth = aRect.mWidth;
            theHeight = aRect.mHeight;
            return aBits;
        }

        /// <summary>Lets the sheets' pixels go (they're kept while the ending is up).</summary>
        public static void ReleaseBits()
        {
            sBits.Clear();
        }
    }
}
