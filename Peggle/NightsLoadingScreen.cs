using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Port addition: the PC Peggle Nights intro (PeggleNights.exe Sexy::LoadingScreen, its 3D-accelerated
    /// path: states 0, 3 and 4), in place of the iPad/iPhone Nights loading scene. After the PopCap logo,
    /// a sunset over a daytime hill dims to night: the sun sets, white stars come out, the bright layers
    /// fade off the dark ones, the moon rises, the Peggle logo drops, "Nights" fades in under it and the
    /// glow lights up once the ball hits; fireflies drift and pop when tapped.
    /// The moon, ball, sun and the loading bar (frame, lights, phrase, Play button, as the mobile
    /// screen has them) are this edition's art; the rest is the PC's (nights_loading,
    /// tools/build_wow_assets.py). Coordinates are the PC's 800x600, scaled by H / 600 and centred.
    /// RE notes: D:/peggle-re-nights (decomp, scripts); the WoW screen (WoWLoadingScreen) is based on this one.
    /// </summary>
    public class NightsLoadingScreen : LoadingScreen
    {
        private static readonly Random sRand = new Random();

        /// <summary>The star field's spots (PC 0x6c86f0, as WoW's); the last two are the table's empty entries.</summary>
        private static readonly int[] STAR_XY =
        {
            313, 55, 475, 68, 758, 129, 724, 30, 709, 149, 678, 193, 636, 127, 624, 23, 616, 26, 594, 99,
            583, 65, 515, 47, 512, 102, 509, 82, 505, 37, 505, 125, 504, 30, 475, 74, 443, 58, 438, 38,
            348, 36, 314, 92, 312, 36, 270, 126, 249, 66, 154, 104, 0, 0, 0, 0,
        };

        private static readonly int[] STAR_PERIODS = { 91, 173, 137 };

        private PCSheet mSheet;

        private LoadingFireflies mFlies;

        /// <summary>The sun images this screen loaded (the Nights theme's groups don't have them).</summary>
        private bool mLoadedSunGlow;

        private bool mLoadedSunHead;

        /// <summary>PC SOUND_LOGO_BUZZ_ON: the logo's lights switching on as the ball hits it.</summary>
        private int mBuzzSound = -1;

        /// <summary>The ball's hit, when the loading thread hasn't made SOUND_PEGHIT_LOW yet (this screen's copy).</summary>
        private int mHitSound = -1;

        /// <summary>0 PopCap logo, 3 then 4 the scene (PC 0x2f0..0x2f8; 7-11 are the unaccelerated path).</summary>
        private int mState;

        private int mPrevState;

        private int mStateCounter;

        private int mStateDuration = 200;

        private float mPCDisplayPct;                              // 0xa4

        /// <summary>The moon's rise (PC 0xa8).</summary>
        private float mMoonPct;                                   // 0xa8

        /// <summary>min(1, display * 1.45): when fireflies come, and how far the hills have moved (PC 0xac).</summary>
        private float mFireflyGate;                               // 0xac

        private int mLogoCounter2;                                // 0x2e8

        private int mLogoX = 116;                                 // 0x2fc

        private int mLogoY = 106;                                 // 0x300

        /// <summary>When the ball hit the logo, for the glow (PC 0x548; 0 = not yet).</summary>
        private int mGlowStart;                                   // 0x548

        private float mStarAlpha;

        /// <summary>Where Draw put "Nights" this frame (PC y; int.MinValue = not shown).</summary>
        private int mNightsTextY = int.MinValue;

        /// <summary>The logo's y for its glow this frame (PC y; int.MinValue = no glow).</summary>
        private int mGlowLogoY = int.MinValue;

        private readonly CurvedVal mLogoDropCurve = new CurvedVal("b+-1,1,0.01,1.25,~###9~###       {J}1V kPZRD");  // 0x308

        private readonly CurvedVal mLogoScaleCurve = new CurvedVal("b+0.75,1,0.01,1.25,####         ~~Tob");        // 0x398

        private readonly CurvedVal mNightsCurve = new CurvedVal("b+0,1,0.01,3,####         ~~###");                 // 0x428

        private readonly CurvedVal mGlowCurve = new CurvedVal("b+0,1,0.01,0.8,####L####>a###Ya###@#### ]####Bn###Yn###6####  U####>R###  aR###"); // 0x4b8

        private static int H => SexyUtil.gPhysicalDisplayHeight;

        /// <summary>
        /// Port: the PC window's frame hid the scene's edges, where its layers stop (the forest spans PC x
        /// 14..787, and it starts 24 px above the bottom); the scene is zoomed this much about its top centre
        /// so they don't show. The loading bar, phrase and Play button are the edition's and not zoomed.
        /// </summary>
        private const float SCENE_ZOOM = 1.045f;

        private static float S => H / 600.0f * SCENE_ZOOM;

        /// <summary>The x of the PC screen's left edge: the 800-wide scene is centred.</summary>
        private int OX => (mWidth - (int)(800 * S)) / 2;

        public NightsLoadingScreen(ThunderballApp theApp) : base(theApp)
        {
            mSheet = PCSheet.Load("images/pc/nights_loading", S);
            if (mSheet != null)
            {
                mFlies = new LoadingFireflies(mApp, mSheet.mImage, mSheet["FLY0"], mSheet["FLY1"]);
            }
            mBuzzSound = mApp.mSoundManager.LoadSound("sounds/nights_logo_buzz_on.wav");
            // The sun is the Peggle loading screen's, which the Nights theme doesn't load.
            ResourceManager rm = mApp.mResourceManager;
            if (Res.IMAGE_LOAD_SUNGLOW_LOAD == null)
            {
                Res.IMAGE_LOAD_SUNGLOW_LOAD = rm.LoadImage("IMAGE_LOAD_SUNGLOW_LOAD");
                mLoadedSunGlow = true;
            }
            if (Res.IMAGE_LOAD_SUNHEAD == null)
            {
                Res.IMAGE_LOAD_SUNHEAD = rm.LoadImage("IMAGE_LOAD_SUNHEAD");
                mLoadedSunHead = true;
            }
        }

        public override void Dispose()
        {
            mFlies?.Dispose();
            mFlies = null;
            mSheet?.Dispose();
            mSheet = null;
            foreach (int aSound in new[] { mBuzzSound, mHitSound })
            {
                if (aSound >= 0)
                {
                    mApp.mSoundManager.ReleaseSound((uint)aSound);
                }
            }
            mBuzzSound = mHitSound = -1;
            ResourceManager rm = mApp.mResourceManager;
            if (mLoadedSunGlow)
            {
                rm.DeleteImage("IMAGE_LOAD_SUNGLOW_LOAD");
                Res.IMAGE_LOAD_SUNGLOW_LOAD = null;
            }
            if (mLoadedSunHead)
            {
                rm.DeleteImage("IMAGE_LOAD_SUNHEAD");
                Res.IMAGE_LOAD_SUNHEAD = null;
            }
            base.Dispose();
        }

        private float T => mUpdateCnt / 100.0f;

        private bool InScene => mState >= 3 && mState <= 4;

        // ---------------------------------------------------------------- Update (PC vf22 0x4bc540)

        public override void Update()
        {
            WidgetUpdate();
            int aCount = mUpdateCnt;
            if (mStateDuration != -1 && mStateCounter >= mStateDuration)
            {
                switch (mState)
                {
                case 0:
                    mStateDuration = 100;
                    mState = 3;
                    break;
                case 3:
                    mState = 4;
                    mStateDuration = 200;
                    break;
                case 4:
                    mStateDuration = 1000;
                    break;
                }
            }
            if (mState != mPrevState)
            {
                mStateCounter = 0;
                if (mState == 3)
                {
                    mPCDisplayPct = 0;
                    mMoonPct = 0;
                }
                mPrevState = mState;
            }
            else
            {
                mStateCounter++;
            }
            if (mDoneFlashCounter != 0)
            {
                mDoneFlashCounter++;
            }
            if (aCount == 1)
            {
                // The PC plays its opening piece here; the port's music has the Nights opening, then
                // loops the Nights ambience.
                mApp.PlayOpeningMusic(2 + ThunderballApp.NIGHTS_SONG_OFFSET, ThunderballApp.NIGHTS_AMBIENCE_SONG);
            }
            UpdatePCBall();
            UpdateTrail();
            if (mBallBounceCounter > 0 && ++mBallBounceCounter > 30)
            {
                mBallBounceCounter = 0;
            }
            float aTarget = mApp.mLoadingThreadCompleted ? 1.0f : 0.9999f;
            float aTimed = (aCount - 250) * 0.001f;
            if (!mApp.mLoadingThreadCompleted && aTimed > 0.99f)
            {
                aTimed = 0.99f;
            }
            aTarget = Math.Max(0, Math.Min(aTimed, aTarget));
            if (mLoadingPct < aTarget)
            {
                mLoadingPct = Math.Min(mLoadingPct + 0.00126f, aTarget);
                mNumLights = (int)(mLoadingPct * 25);
                if (mLoadingPct >= 1.0f)
                {
                    mDoneFlashCounter = 1;
                    FinishLoading();
                    mPlayButton.SetVisible(InScene);
                }
            }
            mPCDisplayPct = Math.Min(1.0f, mPCDisplayPct + 0.00115f);
            if (mPCDisplayPct > 0.62f)
            {
                mLogoCounter2++;
            }
            if (mPCDisplayPct > 0.8f)
            {
                mMoonPct = Math.Min(1.0f, mMoonPct + 0.0027f);
            }
            if (InScene && aCount > 0x500)
            {
                mPCDisplayPct = 1.0f;
                if (mBallState == 0)
                {
                    DoBallBounce();
                    mGlowStart = 0;
                }
            }
            mFireflyGate = Math.Min(mPCDisplayPct * 1.45f, 1.0f);
            mFlies?.Update(this, InScene && mFireflyGate == 1.0f, OX, S);
            mPhraseCounter++;
            if (mPhraseCounter == 300)
            {
                LoadNextPhrase();
            }
            if (mGlowStart > 0 && aCount == mGlowStart && mBuzzSound >= 0)
            {
                // PC: the logo's lights buzz on the tick the ball hits it.
                mApp.PlaySample(mBuzzSound);
            }
            MarkDirty();
        }

        /// <summary>The ball drop on the logo (PC FUN_004bc410): bounces at y 150 and starts the logo's glow.</summary>
        private void UpdatePCBall()
        {
            if (mBallState <= 0 || mBallState >= 3)
            {
                return;
            }
            mBallVelY += 0.2f;
            mBallX += mBallVelX;
            mBallY += mBallVelY;
            if (mBallY >= 150.0f && mBallState == 1)
            {
                if (mGlowStart == 0)
                {
                    mGlowStart = mUpdateCnt;
                }
                mBallBounceCounter = 1;
                mBallState = 2;
                mBallVelY *= -0.75f;
                if (Res.SOUND_PEGHIT_LOW >= 0)
                {
                    mApp.PlaySample(Res.SOUND_PEGHIT_LOW);
                }
                else
                {
                    if (mHitSound < 0)
                    {
                        mHitSound = mApp.mSoundManager.LoadSound("sounds/peghit_low.wav");
                    }
                    if (mHitSound >= 0)
                    {
                        mApp.PlaySample(mHitSound);
                    }
                }
            }
            else if (mBallX > 840 && mBallX > 860)
            {
                mBallState = 3;
            }
            if (mBallX < 840)
            {
                for (int i = -7; i < 8; i += 2)
                {
                    float anAngle = (float)Math.Atan2(mBallVelY, mBallVelX);
                    double aPerp = anAngle + Math.PI / 2;
                    TrailParticle p = new TrailParticle();
                    p.mX = (float)(Math.Cos(aPerp) * 4.3 * i + mBallX + sRand.Next(30) - 15.0);
                    p.mY = (float)(Math.Sin(aPerp) * 4.3 * i + mBallY + sRand.Next(30) - 15.0);
                    p.mLife = 0xff;
                    p.mVelX = (float)(Math.Cos(aPerp) * -0.071 * i);
                    p.mVelY = (float)(Math.Sin(aPerp) * -0.071 * i);
                    p.mClockwise = i > 0;
                    p.mAngle = anAngle;
                    mParticles.Add(p);
                }
            }
        }

        /// <summary>The ball's trail wobbles out (as the WoW screen's).</summary>
        private void UpdateTrail()
        {
            for (int i = 0; i < mParticles.Count; i++)
            {
                TrailParticle p = mParticles[i];
                p.mLife -= 6;
                if (p.mLife < 1)
                {
                    mParticles[i] = mParticles[mParticles.Count - 1];
                    mParticles.RemoveAt(mParticles.Count - 1);
                    i--;
                    continue;
                }
                float aWobble = (0.25f - (p.mVelY * p.mVelY + p.mVelX * p.mVelX)) * 3.0f;
                p.mY = (float)(Math.Sin(p.mAngle) * aWobble + p.mVelY + p.mY);
                p.mX = (float)(Math.Cos(p.mAngle) * aWobble + p.mVelX + p.mX);
                mParticles[i] = p;
            }
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            // PC vf54: a tap on a firefly pops it.
            if (mFlies != null && mFlies.Pop((x - OX) / S, y / S))
            {
                return;
            }
            base.MouseDown(x, y, theClickCount);
        }

        // ---------------------------------------------------------------- Draw helpers

        private int PX(float x) => OX + (int)(x * S);

        private int PY(float y) => (int)(y * S);

        private void Cel(Graphics g, string theCel, float x, float y)
        {
            g.DrawImage(mSheet.mImage, PX(x), PY(y), mSheet[theCel]);
        }

        /// <summary>
        /// Port: the scene is the PC screen in a centred 4:3 box, unclipped; on iPhone the backdrop spreads
        /// over the window: the sky, sunset and house hill zoom, the hills stretch, the forest splits.
        /// </summary>
        private readonly PCScene mScene = new PCScene();

        /// <summary>The forest's outer PC px on each side that go to the window's edges on iPhone.</summary>
        private const int FOREST_SIDE_W = 315;

        /// <summary>A far layer (sky, sunset, house hill) at PC (x, y), PC size theW x theH.</summary>
        private void FarCel(Graphics g, string theCel, float x, float y, float theW, float theH)
        {
            g.DrawImage(mSheet.mImage, mScene.Far(x, y, theW * S, theH * S), mSheet[theCel]);
        }

        private void FarCel(Graphics g, string theCel, float x, float y)
        {
            TRect aSrc = mSheet[theCel];
            g.DrawImage(mSheet.mImage, mScene.Far(x, y, aSrc.mWidth, aSrc.mHeight), aSrc);
        }

        private void MidCel(Graphics g, string theCel, float x, float y)
        {
            TRect aSrc = mSheet[theCel];
            g.DrawImage(mSheet.mImage, mScene.Mid(x, y, aSrc.mWidth, aSrc.mHeight), aSrc);
        }

        /// <summary>An edition image centred on PC (x, y), zoomed with the scene.</summary>
        private void DrawCentred(Graphics g, Image theImage, float x, float y)
        {
            float w = theImage.mWidth * SCENE_ZOOM;
            float h = theImage.mHeight * SCENE_ZOOM;
            g.DrawImage(theImage, new TRect((int)(OX + x * S - w / 2), (int)(y * S - h / 2), (int)w, (int)h), new TRect(0, 0, theImage.mWidth, theImage.mHeight));
        }

        // ---------------------------------------------------------------- Draw (PC vf26 0x4bf030, states 3-4)

        public override void Draw(Graphics theG)
        {
            mScene.Update(mWidth, mHeight, SCENE_ZOOM);
            mNightsTextY = int.MinValue;
            mGlowLogoY = int.MinValue;
            // The scene reaches the window's margins (iPhone); the loading bar and phrase are the edition's.
            Graphics g = PCScene.Wide(theG);
            g.SetColor(new Color(0, 0, 0));
            g.FillRect(mScene.mWindowX, 0, mScene.mWindowW, mHeight);
            g.SetColor(new Color(255, 255, 255));
            if (mSheet == null || !InScene)
            {
                DeferOverlay(0);
                return;
            }
            // The span the sky (10..810), the sunset (0..800) and the house hill (0..820) all cover.
            mScene.SetFarSpan(10, 800);
            float t = T;
            int aLogoY = mLogoY;
            float aLogoScale = 0.5f;
            if (mLogoCounter2 > 0)
            {
                aLogoY = (int)(mLogoDropCurve.GetOutVal(mLogoCounter2 / 100.0) * 400 + aLogoY);
                aLogoScale = (float)mLogoScaleCurve.GetOutVal(mLogoCounter2 / 100.0);
            }
            float pct = mPCDisplayPct;
            // The night sky, under the sunset that fades and darkens off it.
            FarCel(g, "BACK", 10, -30);
            int aSunsetAlpha = Math.Max(0, Math.Min(255, (int)(384 - pct * 512)));
            int aGray = (int)(255 - pct * 255);
            g.SetColorizeImages(true);
            g.SetColor(new Color(aGray, aGray, aGray, aSunsetAlpha));
            FarCel(g, "BACKBRIGHT", 0, 0, 800, 600);
            g.SetColorizeImages(false);
            DrawStars(g);
            int aSunY = (int)(pct * 1000 - 214);
            float aMoon = 1 - (float)Math.Pow(mMoonPct, 0.4);
            int aMoonY = (int)(aMoon * 220 - 314);
            if (pct < 0.6f)
            {
                g.SetColorizeImages(true);
                g.SetColor(new Color(aGray, aGray, aGray));
                // The sun glow (the PC's turns by an angle that stays 0) and head at the mobile loading
                // screen's spots: the glow's sun sits off its image's centre, at its pivot (0x18a, 0x17f).
                Image aGlow = Res.IMAGE_LOAD_SUNGLOW_LOAD;
                g.DrawImage(aGlow, new TRect(PX(-36), PY(aSunY), (int)(aGlow.mWidth * SCENE_ZOOM), (int)(aGlow.mHeight * SCENE_ZOOM)), new TRect(0, 0, aGlow.mWidth, aGlow.mHeight));
                Image aHead = Res.IMAGE_LOAD_SUNHEAD;
                g.DrawImage(aHead, new TRect(PX(0x11b), PY(aSunY + 0x133), (int)(aHead.mWidth * SCENE_ZOOM), (int)(aHead.mHeight * SCENE_ZOOM)), new TRect(0, 0, aHead.mWidth, aHead.mHeight));
                g.SetColorizeImages(false);
            }
            else if (aMoon != 1)
            {
                Image aMoonImage = Res.IMAGE_LOAD_NIGHTS_MOON;
                DrawCentred(g, aMoonImage, 0x100 + 259 / 2.0f, aMoonY + 0x148 + 260 / 2.0f);
            }
            if (mLogoCounter2 > 0 && mLogoCounter2 < 0x76)
            {
                DrawLogoScaled(g, aLogoScale, aLogoY);
            }
            int b = mBallBounceCounter;
            if (b > 0 && b < 10)
            {
                int d = (int)(8 - b * 1.5);
                mLogoY += d;
                aLogoY += d;
            }
            // The daytime hill with the house, fading and darkening like the sunset.
            g.SetColorizeImages(true);
            g.SetColor(new Color(aGray, aGray, aGray, aSunsetAlpha));
            FarCel(g, "BACKTUTE", 0, 200);
            g.SetColorizeImages(false);
            // The hills and the forest: their bright copies fade off the dark ones as they move.
            int aBrightAlpha = Math.Max(0, (int)(255 - pct * 254));
            float aHillY = mFireflyGate * 65 + 250;
            MidCel(g, "HILL2", 0, aHillY);
            g.SetColorizeImages(true);
            g.SetColor(new Color(255, 255, 255, aBrightAlpha));
            MidCel(g, "HILL2BRIGHT", 0, aHillY);
            g.SetColorizeImages(false);
            int aForestY = (int)(mFireflyGate * 110 - 119);
            mScene.DrawFront(g, mSheet.mImage, mSheet["HILL"], 0xe, aForestY, FOREST_SIDE_W);
            g.SetColorizeImages(true);
            g.SetColor(new Color(255, 255, 255, aBrightAlpha));
            mScene.DrawFront(g, mSheet.mImage, mSheet["HILLBRIGHT"], 0xe, aForestY, FOREST_SIDE_W);
            g.SetColorizeImages(false);
            g.SetColor(new Color(255, 255, 255));
            // This edition's loading bar, as on its own Nights screen.
            theG.Translate(SexyUtil.gGutterWidth, 0);
            theG.DrawImage(Res.IMAGE_LOAD_NIGHTS_LOADINGBAR, LOADING_BAR_X, LOADING_NIGHTS_LOAD_LIGHT_OFFSET + LOADING_BAR_Y);
            DrawBarOverlay(theG);
#if PEGGLE_IPHONE
            if (mLoadingDone)
            {
                theG.SetColorizeImages(true);
                theG.SetColor(SexyUtil.MakeColor(0));
                theG.DrawImage(Res.IMAGE_LOAD_TM, (int)(H * 0x23e / 600.0 + LOADING_LEFT_OFFSET), (int)(H * 0x136 / 600.0));
                theG.SetColorizeImages(false);
                theG.SetColor(new Color(255, 255, 255));
            }
#endif
            theG.Translate(-SexyUtil.gGutterWidth, 0);
            if (mLogoCounter2 >= 0x76)
            {
                DrawLogoScaled(g, aLogoScale, aLogoY);
                if (mBallBounceCounter != 0)
                {
                    g.SetColorizeImages(true);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    float aGlow = SexyUtil.InterpValF(mBallBounceCounter, 5, 25, 0.0f, 1.0f, 0.0f);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(aGlow * 128)));
                    Cel(g, "LOGO", mLogoX, aLogoY);
                    g.SetColorizeImages(false);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                }
                // "Nights" fades in under the logo after tick 900, drawn in DrawOverlay (over the Play button).
                mNightsTextY = aLogoY + 0xbd;
            }
            // The logo's glow goes over "Nights", so it's drawn in DrawOverlay too.
            mGlowLogoY = mGlowStart != 0 ? aLogoY : int.MinValue;
            DrawPhrase(theG);
            DeferOverlay(0);
        }

        /// <summary>The Peggle logo stretched by theScale about its middle.</summary>
        private void DrawLogoScaled(Graphics g, float theScale, int theLogoY)
        {
            TRect aSrc = mSheet["LOGO"];
            int aW = (int)Math.Round(aSrc.mWidth / S);
            int aH = (int)Math.Round(aSrc.mHeight / S);
            int w = (int)(aW * theScale);
            int h = (int)(aH * theScale);
            int x = (aW - w) / 2 + mLogoX;
            int y = (aH - h) / 2 + theLogoY;
            g.DrawImage(mSheet.mImage, new TRect(PX(x), PY(y), (int)(w * S), (int)(h * S)), aSrc);
        }

        /// <summary>PC FUN_004becf0: three twinkling white stars once the sky is darkening.</summary>
        private void DrawStars(Graphics g)
        {
            if (mPCDisplayPct <= 0.45f)
            {
                return;
            }
            mStarAlpha = Math.Min(1.0f, mStarAlpha + 0.03f);
            foreach (int aPeriod in STAR_PERIODS)
            {
                int i = ((mUpdateCnt / aPeriod) * 0x471) % 28;
                float f = (mUpdateCnt % aPeriod) / (float)aPeriod * 2;
                if (f > 1.0f)
                {
                    f = 2 - f;
                }
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(f * 150 * mStarAlpha)));
                g.SetColorizeImages(true);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                Cel(g, "STAR", STAR_XY[i * 2] - 5, STAR_XY[i * 2 + 1] - 5);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                g.SetColorizeImages(false);
            }
            g.SetColor(new Color(255, 255, 255));
        }

        // ---------------------------------------------------------------- DrawOverlay (PC vf44 0x4be660)

        public override void DrawOverlay(Graphics g)
        {
            if (mState == 0)
            {
                int a = 255;
                if (mUpdateCnt < 50)
                {
                    a = mStateCounter * 255 / 50;
                }
                else if (mUpdateCnt > 150)
                {
                    a = (200 - mStateCounter) * 255 / 50;
                }
                g.SetColor(SexyUtil.MakeColor(0xffffff, Math.Max(0, Math.Min(255, a))));
                g.SetColorizeImages(true);
                Image aPopCap = Res.IMAGE_LOAD_POPCAP;
                g.DrawImage(aPopCap, (mWidth - aPopCap.mWidth) / 2, (mHeight - aPopCap.mHeight) / 2);
                g.SetColorizeImages(false);
                g.SetColor(new Color(255, 255, 255));
                return;
            }
            if (mDoneFlashCounter > 0 && mDoneFlashCounter < 50)
            {
                float f = (mDoneFlashCounter < 25 ? mDoneFlashCounter : 50 - mDoneFlashCounter) / 25.0f * 100.0f;
                Graphics g2 = g.Create();
                g2.SetColorizeImages(true);
                g2.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                g2.SetColor(new Color((int)f, (int)f, (int)f));
                g2.Translate(mPlayButton.mX, mPlayButton.mY);
                mPlayButton.Draw(g2);
            }
            Graphics gs = PCScene.Wide(g);
            if (mNightsTextY != int.MinValue && mUpdateCnt > 900)
            {
                // Port: over the Play button (the PC's bar has no button there).
                float a = (float)mNightsCurve.GetOutVal((mUpdateCnt - 900) / 100.0);
                gs.SetColorizeImages(true);
                gs.SetColor(SexyUtil.MakeColor(0xffffff, (int)(a * 255)));
                Cel(gs, "NIGHTS", mLogoX + 0x77, mNightsTextY);
                gs.SetColorizeImages(false);
                gs.SetColor(new Color(255, 255, 255));
            }
            if (mGlowLogoY != int.MinValue)
            {
                // The PC draws the glow after "Nights" (0x4bf801), so it lights the word up too.
                int a = (int)(mGlowCurve.GetOutVal((mUpdateCnt - mGlowStart) / 100.0) * 255);
                if (a > 0)
                {
                    gs.SetColorizeImages(true);
                    gs.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    gs.SetColor(SexyUtil.MakeColor(0xffffff, a));
                    Cel(gs, "GLOW", mLogoX + 0x1c, mGlowLogoY + 0x22);
                    gs.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                    gs.SetColorizeImages(false);
                    gs.SetColor(new Color(255, 255, 255));
                }
            }
            if (mBallState > 0 && mBallState < 3)
            {
                DrawCentred(gs, Res.IMAGE_LOAD_NIGHTS_BALL, mBallX, mBallY);
            }
            mFlies?.Draw(gs, OX, S);
            DrawTrail(gs);
            mFlies?.DrawSparks(gs, OX, S);
        }

        /// <summary>PC FUN_004bde30's trail half: small additive specks behind the ball.</summary>
        private void DrawTrail(Graphics g)
        {
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            int aSize = Math.Max(1, (int)S);
            foreach (TrailParticle p in mParticles)
            {
                g.SetColor(new Color(sRand.Next(128), sRand.Next(128), sRand.Next(128), p.mLife));
                g.FillRect(PX(p.mX - 1), PY(p.mY - 1), (sRand.Next(2) + 1) * aSize, (sRand.Next(2) + 1) * aSize);
                if (sRand.Next(255) < p.mLife)
                {
                    g.SetColor(new Color(2, 2, 1));
                    g.FillRect(PX(p.mX - 4), PY(p.mY - 4), (int)(8 * S), (int)(8 * S));
                }
            }
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
            g.SetColor(new Color(255, 255, 255));
        }
    }
}
