using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>A spark left behind the bouncing loading-screen ball (Sexy::TrailParticle, 0x1c bytes).</summary>
    public struct TrailParticle
    {
        public float mX;                                          // 0x00

        public float mY;                                          // 0x04

        public int mLife;                                         // 0x08

        public float mVelX;                                       // 0x0c

        public float mVelY;                                       // 0x10

        public bool mClockwise;                                   // 0x14

        public float mAngle;                                      // 0x18
    }

    /// <summary>
    /// The title/loading screen (Sexy::LoadingScreen): PopCap logo fade, Peggle (or Nights) logo
    /// drop, a time-based loading bar of 25 lights, the rotating sun, the bouncing ball with its
    /// spark trail, rotating loading phrases and the "Play Now" button.
    /// Coordinates are authored for 800x600 and scaled by gPhysicalDisplayHeight / 600.
    /// </summary>
    public class LoadingScreen : Widget, ButtonListener
    {
        private static readonly Random sRandom = new Random();

        private static int aTickStart;

        private static bool needCalc = true;

        private static float a;

        private static float b;

        private static float c;

        public ThunderballApp mApp;                               // 0x7c

        public ThunderButton mPlayButton;                         // 0x80

        public float mLoadingPct;                                 // 0x84

        public float mSunRotation;                                // 0x88

        public int mSunCounter;                                   // 0x8c

        public int mDoneFlashCounter;                             // 0x90

        public float mDisplayPct;                                 // 0x94

        public int mStartGameCountdown;                           // 0x98

        public int mBallState;                                    // 0x9c (0 none, 1 falling, 2 bounced, 3 gone)

        public int mBallBounceCounter;                            // 0xa0

        public float mBallX;                                      // 0xa4

        public float mBallY;                                      // 0xa8

        public float mBallVelX;                                   // 0xac

        public float mBallVelY;                                   // 0xb0

        public bool mMusicStarted;                                // 0xb4

        public List<TrailParticle> mParticles = new List<TrailParticle>(); // 0xb8

        public bool mLoadingDone;                                 // 0xc4

        public string mPhrase = "";                               // 0xc8

        public int[] mPhraseOrder = new int[0x1b];                // 0xcc

        public int mPhraseIndex;                                  // 0x138

        public int mPhraseCounter;                                // 0x13c

        public bool mExitThread;                                  // 0x144

        public int mLogoCounter;                                  // 0x148

        public bool mDisplayPartnerLogo;                          // 0x14c

        public int mNumLights;                                    // 0x150

        /// <summary>Port addition: the menu theme the screen was made with (the app's may change as it scrolls off).</summary>
        public bool mNightsMode;

        /// <summary>
        /// Port addition: the Peggle Extreme look (Menu Theme Last Played after an Extreme level): the day
        /// screen with the Extreme sun face, and the ball blows up on the logo instead of bouncing off.
        /// </summary>
        public bool mExtreme;

        /// <summary>Port addition: the ball has blown up on the logo: the blast mark and EXTREME label show.</summary>
        public bool mExtremeHit;

        /// <summary>Port addition: the Extreme art (tools/build_extreme_assets.py), PC art scaled by H / 600.</summary>
        public Image mExtremeImage;

        public int mExtremeSound = -1;

        /// <summary>Port addition: HD's Extreme logo, without the "HD" badge (peggleloader_nohd); null on the iPhone.</summary>
        public Image mExtremeLogo;

        /// <summary>The day logo: the Extreme one when there is one.</summary>
        private Image LogoImage => mExtremeLogo ?? Res.IMAGE_LOAD_LOGO;

        /// <summary>Port addition: the cels of mExtremeImage at PC (800x600) scale.</summary>
        private static readonly TRect EXTREME_LABEL_RECT = new TRect(0, 0, 572, 195);

        private static readonly TRect EXTREME_SUNHEAD_RECT = new TRect(0, 225, 150, 150);

        private static readonly TRect EXTREME_BLASTMARK_RECT = new TRect(225, 225, 203, 80);

        /// <summary>Port addition: where the PC Extreme screen puts the label and the blast mark (800x600 coordinates).</summary>
        private const int EXTREME_LABEL_X = 122;

        private const int EXTREME_LABEL_Y = 286;

        private const int EXTREME_BLASTMARK_X = 286;

        private const int EXTREME_BLASTMARK_Y = 196;

        private static int H => SexyUtil.gPhysicalDisplayHeight;

        public LoadingScreen(ThunderballApp theApp)
        {
            mApp = theApp;
            mNightsMode = mApp.mNightsMode;
            if (mApp.GetMenuThemeExtreme())
            {
                mExtremeImage = SexyUtil.LoadPCImage("images/pc/loading_extreme");
                mExtremeSound = mApp.mSoundManager.LoadSound("sounds/explode_extreme.wav");
                mExtreme = mExtremeImage != null;
#if !PEGGLE_IPHONE
                if (mExtreme)
                {
                    mExtremeLogo = mApp.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "loading/peggleloader_nohd", true);
                }
#endif
            }
            mLoadingPct = 0;
            mSunRotation = 0;
            mSunCounter = 0;
            mDisplayPct = 0;
            mDoneFlashCounter = 0;
            mStartGameCountdown = 0;
            mBallState = 0;
            mBallBounceCounter = 0;
            mMusicStarted = false;
            mLogoCounter = 0;
            mLoadingDone = false;
            Image playNow = Res.IMAGE_LOAD_PLAYNOW;
            mPlayButton = new ThunderButton(playNow, 0, this);
            int w = playNow.mWidth;
            int h = playNow.mHeight;
            mPlayButton.mButtonImage = playNow;
            mPlayButton.mComponentImage = null;
            mPlayButton.mWidth = w;
            mPlayButton.mHeight = h / 2;
            mPlayButton.mNormalRect = new TRect(0, 0, w, h / 2);
            mPlayButton.mOverRect = new TRect(0, h / 2, w, h / 2);
            PlacePlayButton();
            mPlayButton.SetVisible(false);
            AddWidget(mPlayButton);
            mDisplayPartnerLogo = mApp.GetBoolean("DisplayPartnerLogo", false);
            mNumLights = 0;
            for (int i = 0; i < 0x1b; i++)
            {
                mPhraseOrder[i] = i;
            }
            for (int i = 0x1b - 1; i > 0; i--)
            {
                int j = sRandom.Next(i + 1);
                int t = mPhraseOrder[i];
                mPhraseOrder[i] = mPhraseOrder[j];
                mPhraseOrder[j] = t;
            }
            mPhraseIndex = -1;
            LoadNextPhrase();
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
#if PEGGLE_IPHONE
            mApp.UnloadLoadingBarResources(mNightsMode);
#else
            ResourceManager rm = mApp.mResourceManager;
            rm.DeleteImage("IMAGE_LOAD_POPCAP");
            rm.DeleteImage("IMAGE_LOAD_TM");
            rm.DeleteImage("IMAGE_LOAD_OVERLAY");
            rm.DeleteImage("IMAGE_LOAD_LIGHTS");
            rm.DeleteImage("IMAGE_LOAD_PLAYNOW");
            if (!mNightsMode)
            {
                rm.DeleteImage("IMAGE_LOAD_BACK");
                rm.DeleteImage("IMAGE_LOAD_HILL");
                rm.DeleteImage("IMAGE_LOAD_LOGO");
                rm.DeleteImage("IMAGE_LOAD_SHADOW");
                rm.DeleteImage("IMAGE_LOAD_SUNHEAD");
                rm.DeleteImage("IMAGE_LOAD_BALL");
            }
            else
            {
                rm.DeleteImage("IMAGE_LOAD_NIGHTS_BACK");
                rm.DeleteImage("IMAGE_LOAD_NIGHTS_HILL");
                rm.DeleteImage("IMAGE_LOAD_NIGHTS_LOGO");
                rm.DeleteImage("IMAGE_LOAD_NIGHTS_LOGO_OVERLAY");
                rm.DeleteImage("IMAGE_LOAD_NIGHTS_MOON");
                rm.DeleteImage("IMAGE_LOAD_NIGHTS_BALL");
                rm.DeleteImage("IMAGE_LOAD_NIGHTS_LOADINGBAR");
            }
#endif
            if (mExtremeImage != null)
            {
                mExtremeImage.Dispose();
                mExtremeImage = null;
            }
            if (mExtremeLogo != null)
            {
                mExtremeLogo.Dispose();
                mExtremeLogo = null;
            }
            if (mExtremeSound >= 0)
            {
                mApp.mSoundManager.ReleaseSound((uint)mExtremeSound);
                mExtremeSound = -1;
            }
            mParticles.Clear();
            base.Dispose();
        }

        private static string GetPhrase(int theIndex)
        {
            switch (theIndex)
            {
            case 0: return SexyAppBase.Loc("Arranging the pegs...");
            case 1: return SexyAppBase.Loc("Preparing the rainbows...");
            case 2: return SexyAppBase.Loc("Adjusting Ode to Joy volume...");
            case 3: return SexyAppBase.Loc("Doing Peggle Master stuff...");
            case 4: return SexyAppBase.Loc("Loading Peggle Balls...");
            case 5: return SexyAppBase.Loc("Coming up with witty sayings...");
            case 6: return SexyAppBase.Loc("Calculating sparkles...");
            case 7: return SexyAppBase.Loc("Fine-tuning bounciness...");
            case 8: return SexyAppBase.Loc("Cranking awesomeness...");
            case 9: return SexyAppBase.Loc("Sending good vibes...");
            case 10: return SexyAppBase.Loc("Generating flow...");
            case 11: return SexyAppBase.Loc("Maximizing fun factors...");
            case 12: return SexyAppBase.Loc("Hoping this works...");
            case 13: return SexyAppBase.Loc("Getting coffee...");
            case 14: return SexyAppBase.Loc("Thinking about it...");
            case 15: return SexyAppBase.Loc("Pushing electrons...");
            case 16: return SexyAppBase.Loc("Polishing claws...");
            case 17: return SexyAppBase.Loc("Summoning Zen...");
            case 18: return SexyAppBase.Loc("Seeding random number generator...");
            case 19: return SexyAppBase.Loc("Bioforming blasmatrons...");
            case 20: return SexyAppBase.Loc("Playing with yarn...");
            case 21: return SexyAppBase.Loc("Busting rad moves...");
            case 22: return SexyAppBase.Loc("Printing diplomas...");
            case 23: return SexyAppBase.Loc("Making lights blink...");
            case 24: return SexyAppBase.Loc("Doing computery stuff...");
            case 25: return SexyAppBase.Loc("Enacting syllogisms...");
            case 26: return SexyAppBase.Loc("Bathing in sunshine...");
            default: return "";
            }
        }

        public void LoadNextPhrase()
        {
            mPhraseCounter = 0;
            mPhraseIndex++;
            if (mPhraseIndex == 0x1b)
            {
                mPhraseIndex = 0;
            }
            mPhrase = GetPhrase(mPhraseOrder[mPhraseIndex]);
        }

        public void DoBallBounce()
        {
            mBallState = 1;
            mBallX = 205.0f;
            mBallY = -20.0f;
            mBallVelX = 4.5f;
            mBallVelY = 0;
            mBallBounceCounter = 0;
            mExtremeHit = false;
        }

        public void UpdateBall()
        {
            if (mBallState <= 0 || mBallState >= 3)
            {
                return;
            }
            mBallVelY += 0.2f;
            mBallX += mBallVelX;
            mBallY += mBallVelY;
            if (mBallY >= 170.0f && mBallState == 1 && mExtreme)
            {
                // Port addition: the Extreme ball blows up on the logo, leaving the blast mark and the label.
                mBallBounceCounter = 1;
                mBallState = 3;
                mExtremeHit = true;
                if (mExtremeSound >= 0)
                {
                    mApp.PlaySample(mExtremeSound);
                }
            }
            else if (mBallY >= 170.0f && mBallState == 1)
            {
                mBallBounceCounter = 1;
                mBallState = 2;
                mBallVelY *= -0.75f;
                mBallVelX *= 1.0f;
                if (Res.SOUND_PEGHIT_LOW >= 0)
                {
                    mApp.PlaySample(Res.SOUND_PEGHIT_LOW);
                }
            }
            else if (mBallX > 840 && mBallX > 860)
            {
                mBallState = 3;
            }
            if (mBallX < 840)
            {
                SpawnParticles();
            }
            MarkDirty();
        }

        public void SpawnParticles()
        {
            for (int i = LOADING_SPAWN_PARTICLES_START; i <= LOADING_SPAWN_PARTICLES_END; i++)
            {
                float angle = (float)Math.Atan2(mBallVelY, mBallVelX);
                TrailParticle p = new TrailParticle();
                p.mX = (float)(mBallX + 4.300000190734863 * Math.Cos(angle + 1.5707963705062866) * i + sRandom.Next() % 0x1e - 15.0);
                p.mY = (float)(mBallY + 4.300000190734863 * Math.Sin(angle + 1.5707963705062866) * i + sRandom.Next() % 0x1e - 15.0);
                p.mLife = 0xff;
                p.mVelX = (float)(-0.07100000232458115 * Math.Cos(angle + 1.5707963705062866) * i);
                p.mVelY = (float)(-0.07100000232458115 * Math.Sin(angle + 1.5707963705062866) * i);
                p.mClockwise = i > 0;
                p.mAngle = angle;
                mParticles.Add(p);
            }
        }

        public override void Update()
        {
            base.Update();
            uint elapsed = 0;
            if (aTickStart == 0)
            {
                aTickStart = Environment.TickCount;
            }
            else
            {
                elapsed = (uint)(Environment.TickCount - aTickStart);
            }
            float pct = elapsed / 9885.0f;
            if (pct > 50.0f)
            {
                pct = 0.0f;
            }
            if (mDoneFlashCounter != 0)
            {
                mDoneFlashCounter++;
            }
            if (mUpdateCnt == 1 && !mMusicStarted)
            {
                mApp.PlayMusic(2, false);
            }
            UpdateBall();
            UpdateParticles();
            if (mBallBounceCounter > 0)
            {
                mBallBounceCounter++;
                if (mBallBounceCounter > 0x1e)
                {
                    mBallBounceCounter = 0;
                }
                MarkDirty();
            }
            if (pct > 0.0f)
            {
                mLogoCounter++;
            }
            if (!mApp.mLoadingThreadCompleted)
            {
                if (pct >= 1.0f)
                {
                    pct = 0.9999f;
                }
            }
            else
            {
                pct = 1.0f;
            }
            if (pct < 0.0f)
            {
                pct = 0.0f;
            }
            if (mLoadingPct < pct)
            {
                mLoadingPct += 0.04f;
                if (pct < mLoadingPct)
                {
                    mLoadingPct = pct;
                }
                mNumLights = (int)(mLoadingPct * 25.0f);
                MarkDirty();
                if (mLoadingPct >= 1.0f)
                {
                    mDoneFlashCounter = 1;
                    FinishLoading();
                }
            }
            if (mDisplayPct < mLoadingPct)
            {
                mDisplayPct = mLoadingPct;
                MarkDirty();
            }
            if (mDisplayPct >= 1.0f && mUpdateCnt > 0x578)
            {
                mDisplayPct = 1.0f;
                if (mBallState == 0)
                {
                    DoBallBounce();
                }
            }
            if (mDisplayPct >= 1.0f)
            {
                mSunRotation += 0.2f;
                mSunCounter++;
                MarkDirty();
            }
            if (mStartGameCountdown > 0 && --mStartGameCountdown == 0)
            {
                UnloadMainMenuGroups();
                mApp.StartAdventureGame(GameMode.GAMEMODE_ADVENTURE);
            }
            if (mUpdateCnt < 0x227 || mParticles.Count != 0)
            {
                MarkDirty();
            }
            mPhraseCounter++;
            if (mPhraseCounter == 300)
            {
                LoadNextPhrase();
            }
        }

        /// <summary>Port addition: Widget's own Update, for WoWLoadingScreen, which replaces this screen's.</summary>
        protected void WidgetUpdate()
        {
            base.Update();
        }

        /// <summary>
        /// Loading has finished: unpacks the atlases the menus need and shows the Play button.
        /// Port: split out of Update so WoWLoadingScreen can share it.
        /// </summary>
        protected void FinishLoading()
        {
            ResourceManager rm = mApp.mResourceManager;
            if (!mApp.mLazyResourceLoading)
            {
                // Port: every group the loading thread loaded, not just these three atlases.
                mApp.UnpackLoadingThreadGroups();
            }
            Res.UnpackDialogAtlasImages(rm);
            Res.UnpackDialog_locAtlasImages(rm);
#if PEGGLE_IPHONE
            if (!mNightsMode)
            {
                Res.UnpackMainMenuAtlasImages(rm);
            }
            else
            {
                Res.UnpackMainMenuNightsAtlasImages(rm);
            }
            Res.UnpackInterfaceAtlasImages(rm);
            Res.UnpackFever_locAtlasImages(rm);
            Res.UnpackLuckySpin_locAtlasImages(rm);
            Res.UnpackMAINMENUCOMMONAtlasImages(rm);
#else
            Res.UnpackDIALOG8888AtlasImages(rm);
            Res.UnpackDIALOG88882AtlasImages(rm);
            Res.UnpackDIALOG8888_locAtlasImages(rm);
            Res.UnpackMainMenuCommonAtlasImages(rm);
            Res.UnpackMainMenuCommon_locAtlasImages(rm);
            if (!mNightsMode)
            {
                Res.UnpackMainMenuAtlasImages(rm);
                Res.UnpackMainMenu_locAtlasImages(rm);
            }
            else
            {
                Res.UnpackMainMenuNightsAtlasImages(rm);
                Res.UnpackMainMenuNights_locAtlasImages(rm);
            }
            Res.UnpackFever_locAtlasImages(rm);
#endif
            // (The original can jump straight into a level or replay given on the command
            // line; not used by this port.)
            mPlayButton.SetVisible(true);
            mLoadingDone = true;
        }

        private void UnloadMainMenuGroups()
        {
            if (!mApp.mLazyResourceLoading)
            {
                return;
            }
            ResourceManager rm = mApp.mResourceManager;
            if (rm.IsGroupLoaded("MainMenuCommon"))
            {
                rm.DeleteResources("MainMenuCommon");
                rm.DeleteResources(mNightsMode ? "MainMenuNights" : "MainMenuPeggle");
            }
        }

        public void UpdateParticles()
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
                float speed2 = p.mVelY * p.mVelY + p.mVelX * p.mVelX;
                float wobble = (0.25f - speed2) * 3.0f;
                p.mY = (float)(p.mY + (double)p.mVelY + wobble * Math.Sin(p.mAngle));
                p.mX = (float)(p.mX + (double)p.mVelX + wobble * Math.Cos(p.mAngle));
                float r = p.mClockwise ? 0.015f : -0.015f;
                double cos = Math.Cos(r);
                double sin = Math.Sin(r);
                float vx = p.mVelX;
                float vy = p.mVelY;
                p.mVelX = (float)(cos * vx + sin * vy);
                p.mVelY = (float)(sin * -1.0 * vx + cos * vy);
                mParticles[i] = p;
            }
        }

        public void DrawParticles(Graphics g)
        {
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            foreach (TrailParticle p in mParticles)
            {
                g.SetColor(new Color(sRandom.Next() % 0x80, sRandom.Next() % 0x80, sRandom.Next() % 0x80, p.mLife));
                g.FillRect((int)(p.mX * H / 600.0 - 1.0 + LOADING_PARTICLE_TRAIL_OFFSET_X), (int)(p.mY * H / 600.0f - 1.0),
                    sRandom.Next() % 2 + 1, sRandom.Next() % 2 + 1);
            }
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
        }

        /// <summary>The original drew a pixel of each of 50 images in turn to force texture uploads.</summary>
        public void PreflightNextImage(Graphics g)
        {
        }

        public void DrawLight(Graphics g, int theLight)
        {
            int cel;
            switch (theLight)
            {
            case 0:
                cel = 4;
                break;
            case 1:
                cel = 3;
                break;
            case 0x17:
                cel = 1;
                break;
            case 0x18:
                cel = 0;
                break;
            default:
                cel = 2;
                break;
            }
            Image lights = Res.IMAGE_LOAD_LIGHTS;
            TRect src = lights.GetCelRect(cel);
            int y = !mNightsMode ? LOADING_LIGHT_Y : LOADING_NIGHTS_LOAD_LIGHT_OFFSET + LOADING_LIGHT_Y;
#if PEGGLE_IPHONE
            int x = (int)((int)(theLight * 13.9f + 224.0f) * H / 600.0 + LOADING_LEFT_OFFSET);
#else
            int x = (int)((int)(theLight * LOAD_BAR_X_END + 224.0f) * H / 600.0 + LOADING_LEFT_OFFSET);
#endif
            g.DrawImage(lights, x, y, src);
        }

        public override void DrawOverlay(Graphics g)
        {
            if (mDoneFlashCounter > 0 && mDoneFlashCounter < 0x32)
            {
                float t = (mDoneFlashCounter < 0x19 ? mDoneFlashCounter : 0x32 - mDoneFlashCounter) / 25.0f * 100.0f;
                Graphics g2 = g.Create();
                g2.SetColorizeImages(true);
                g2.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                int v = (int)t;
                g2.SetColor(new Color(v, v, v));
                g2.Translate(mPlayButton.mX, mPlayButton.mY);
                mPlayButton.Draw(g2);
            }
            g.Translate(SexyUtil.gGutterWidth, 0);
            if (mExtremeHit)
            {
                // Port addition: the EXTREME label goes over the Play button.
                g.DrawImage(mExtremeImage, (int)(EXTREME_LABEL_X * H / 600.0 + LOADING_LEFT_OFFSET),
                    (int)(EXTREME_LABEL_Y * H / 600.0), SexyUtil.ScalePCRect(EXTREME_LABEL_RECT));
            }
            if (mDoneFlashCounter == 0)
            {
                if (mUpdateCnt < 0xa0)
                {
                    g.SetColor(SexyUtil.MakeColor(0, (0xa0 - mUpdateCnt) * 0xff / 100));
                    g.FillRect(0, 0, mWidth, mHeight);
                }
                if (mUpdateCnt < 0x80)
                {
                    g.SetColor(SexyUtil.MakeColor(0xffffff, mUpdateCnt * -2 + 0xff));
                    g.SetColorizeImages(true);
                    Image popcap = Res.IMAGE_LOAD_POPCAP;
                    g.DrawImage(popcap, (mWidth - popcap.mWidth) / 2 - SexyUtil.gGutterWidth, (mHeight - popcap.mHeight) / 2);
                    g.SetColorizeImages(false);
                }
            }
            g.Translate(-SexyUtil.gGutterWidth, 0);
        }

#if PEGGLE_IPHONE
        /// <summary>The iPhone build has no Nights logo shift.</summary>
        private const int LOADING_NIGHTS_LOGO_OFFSET_X = 0;
#endif

        protected virtual void PlacePlayButton()
        {
            if (!mNightsMode)
            {
                mPlayButton.Move(LOADING_START_BUTTON_X + SexyUtil.gGutterWidth, LOADING_START_BUTTON_Y);
            }
            else
            {
                mPlayButton.Move(LOADING_START_BUTTON_X + SexyUtil.gGutterWidth, LOADING_START_BUTTON_Y + LOADING_NIGHTS_LOAD_LIGHT_OFFSET);
            }
        }

#if PEGGLE_IPHONE
        /// <summary>
        /// Port addition: after the game's width changed (ThunderballApp.UpdateLayoutWidth), fills the new
        /// width and moves the Play button for the new gutter. Everything else reads the gutter as it draws.
        /// </summary>
        public void UpdateLayoutWidth()
        {
            Resize(mX, mY, mApp.mWidth, mApp.mHeight);
            PlacePlayButton();
        }
#endif

        /// <summary>
        /// x for a full-width backdrop layer: 0 in HD; on the iPhone it is centred on the screen,
        /// in the gutter-translated coordinates the screen draws in.
        /// </summary>
        private int BackdropX(Image theImage)
        {
#if PEGGLE_IPHONE
            return (mWidth - theImage.mWidth) / 2 - SexyUtil.gGutterWidth;
#else
            return 0;
#endif
        }

        public override void Draw(Graphics g)
        {
#if PEGGLE_IPHONE
            if (SexyUtil.gGutterWidth != 0)
            {
                Res.IMAGE_LOAD_NIGHTS_FRONT_TOP = Res.IMAGE_LOAD_NIGHTS_FRONT_TOP_WIDE;
                Res.IMAGE_LOAD_NIGHTS_FRONT = Res.IMAGE_LOAD_NIGHTS_FRONT_WIDE;
            }
#endif
            PreflightNextImage(g);
            g.Translate(SexyUtil.gGutterWidth, 0);
            if (mDoneFlashCounter == 0 && mUpdateCnt < 0xa0)
            {
                g.Translate(-SexyUtil.gGutterWidth, 0);
                DeferOverlay(0);
                return;
            }
            bool nights = mNightsMode;
            Image back = nights ? Res.IMAGE_LOAD_NIGHTS_BACK : Res.IMAGE_LOAD_BACK;
#if PEGGLE_IPHONE
            // Port addition (ultrawide): the backdrop layers cover a window wider than the game; the
            // farthest one (Peggle or Nights) zooms from its centre.
            int aScreenX = (mWidth - mApp.mWidth) / 2 - SexyUtil.gGutterWidth;
            Ultrawide.DrawBack(g, back, aScreenX, 0);
#else
            g.DrawImage(back, BackdropX(back), 0);
#endif
            int sunX = -0x24;
            int sunY = (int)((1.0f - mDisplayPct) * 500.0f + -214.0f);
            if (!nights)
            {
                g.DrawImageRotated(Res.IMAGE_LOAD_SUNGLOW_LOAD, (int)(sunX * H / 600.0 + LOADING_LEFT_OFFSET), (int)(sunY * H / 600.0),
                    MathHelper.ToRadians(mSunRotation), (int)(H * 0x18a / 600.0), (int)(H * 0x17f / 600.0));
                int headX = (int)(H * 0x11b / 600.0 + LOADING_LEFT_OFFSET);
                int headY = (int)((sunY + 0x133) * H / 600.0);
                if (mExtreme)
                {
                    // Centred where this edition's own sun head goes.
                    TRect src = SexyUtil.ScalePCRect(EXTREME_SUNHEAD_RECT);
                    g.DrawImage(mExtremeImage, headX + (Res.IMAGE_LOAD_SUNHEAD.mWidth - src.mWidth) / 2,
                        headY + (Res.IMAGE_LOAD_SUNHEAD.mHeight - src.mHeight) / 2, src);
                }
                else
                {
                    g.DrawImage(Res.IMAGE_LOAD_SUNHEAD, headX, headY);
                }
            }
            else
            {
                g.DrawImage(Res.IMAGE_LOAD_NIGHTS_MOON, (int)((0x11b - LOADING_NIGHTS_LOGO_OFFSET_X) * H / 600.0),
                    (int)((sunY + 0xe6 - LOADING_NIGHTS_LOGO_OFFSET) * H / 600.0));
            }
            int logoX = 0xaa;
            float logoY = 0x8c;
            int dropEnd = 0x32;
            int bounceEnd = 0x46;
            if (needCalc)
            {
                // y(t) = a t^2 + b t + c passing through y(50) = 80 and y(70) = 140 from c = 490.
                needCalc = false;
                c = 0x1ea;
                SolveLogoCurve(0x9c4, 0x32, 0x50 - c, 0x1324, 0x46, 0x8c - c, out a, out b);
            }
            if (nights)
            {
#if PEGGLE_IPHONE
                Ultrawide.DrawFrontTop(g, Res.IMAGE_LOAD_NIGHTS_FRONT_TOP, aScreenX, 0);
#else
                g.DrawImage(Res.IMAGE_LOAD_NIGHTS_FRONT_TOP, BackdropX(Res.IMAGE_LOAD_NIGHTS_FRONT_TOP), 0);
#endif
            }
            float t = mLogoCounter;
            float curveY = a * t * t + b * t + c;
            if (mLogoCounter < dropEnd)
            {
                DrawLogo(g, logoX, curveY);
            }
            float bounce = 0.0f;
            if (mBallBounceCounter > 0 && mBallBounceCounter < 10)
            {
                bounce = 8.0f - mBallBounceCounter * 1.5f;
                logoY = bounce + logoY;
            }
            if (!nights)
            {
                Image hill = Res.IMAGE_LOAD_HILL;
                g.DrawImage(hill, BackdropX(hill), mHeight - hill.mHeight);
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0x101010, (int)(mDisplayPct * 120.0f * mDisplayPct)));
                g.DrawImage(Res.IMAGE_LOAD_SHADOW, (int)(H * 0xa2 / 600.0 + LOADING_LEFT_OFFSET), (int)((bounce + 0x122) * H / 600.0));
                g.SetColorizeImages(false);
            }
            else
            {
                Image hill = Res.IMAGE_LOAD_NIGHTS_HILL;
#if PEGGLE_IPHONE
                Ultrawide.DrawMid(g, hill, aScreenX, mHeight - hill.mHeight);
#else
                g.DrawImage(hill, BackdropX(hill), mHeight - hill.mHeight);
#endif
#if PEGGLE_IPHONE
                Ultrawide.DrawFrontBottom(g, Res.IMAGE_LOAD_NIGHTS_FRONT, aScreenX, Res.IMAGE_LOAD_NIGHTS_FRONT_TOP.mHeight);
#else
                g.DrawImage(Res.IMAGE_LOAD_NIGHTS_FRONT, 0, 0x203);
#endif
                g.DrawImage(Res.IMAGE_LOAD_NIGHTS_LOADINGBAR, LOADING_BAR_X, LOADING_NIGHTS_LOAD_LIGHT_OFFSET + LOADING_BAR_Y);
            }
            if (mLogoCounter < dropEnd || bounceEnd <= mLogoCounter)
            {
                if (bounceEnd <= mLogoCounter)
                {
                    if (!nights)
                    {
                        int x = (int)(logoX * H / 600.0 + LOADING_LEFT_OFFSET);
                        int y = (int)(logoY * H / 600.0);
                        g.DrawImage(LogoImage, x, y);
                        if (mBallBounceCounter != 0)
                        {
                            g.SetColorizeImages(true);
                            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                            float glow = SexyUtil.InterpValF(mBallBounceCounter, 5, 0x19, 0.0f, 1.0f, 0.0f);
                            g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(glow * 128.0f)));
                            g.DrawImage(LogoImage, x, y);
                            g.SetColorizeImages(false);
                            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                        }
                        if (mExtremeHit)
                        {
                            // The blast mark is on the logo, so it moves with the logo's bump.
                            g.DrawImage(mExtremeImage, (int)(EXTREME_BLASTMARK_X * H / 600.0 + LOADING_LEFT_OFFSET),
                                (int)((EXTREME_BLASTMARK_Y - 0x8c + logoY) * H / 600.0), SexyUtil.ScalePCRect(EXTREME_BLASTMARK_RECT));
                        }
                    }
                    else
                    {
                        int x = (int)((logoX - LOADING_NIGHTS_LOGO_OFFSET_X) * H / 600.0);
                        int y = (int)(logoY * H / 600.0 - LOADING_NIGHTS_LOGO_OFFSET);
                        g.DrawImage(Res.IMAGE_LOAD_NIGHTS_LOGO, x, y);
                        if (mBallState > 1)
                        {
                            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                            g.DrawImage(Res.IMAGE_LOAD_NIGHTS_LOGO_OVERLAY, x, y);
                            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                        }
                    }
                }
            }
            else
            {
                DrawLogo(g, logoX, curveY);
            }
            DrawBarOverlay(g);
            DrawParticles(g);
            if (mBallState > 0 && mBallState != 3)
            {
                Image ball = nights ? Res.IMAGE_LOAD_NIGHTS_BALL : Res.IMAGE_LOAD_BALL;
                float bx = (float)(mBallX * H / 600.0 + LOADING_LEFT_OFFSET);
                float by = mBallY * H / 600.0f;
                g.DrawImageF(ball, bx - ball.mWidth / 2.0f, by - ball.mHeight / 2.0f);
            }
#if PEGGLE_IPHONE
            if (mLoadingDone)
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0));
                g.DrawImage(Res.IMAGE_LOAD_TM, (int)(H * 0x23e / 600.0 + LOADING_LEFT_OFFSET), (int)(H * 0x136 / 600.0));
                g.SetColorizeImages(false);
            }
#endif
            g.Translate(-SexyUtil.gGutterWidth, 0);
            DrawPhrase(g);
            DeferOverlay(0);
        }

        /// <summary>
        /// The loading bar's frame and lights, in the gutter-translated coordinates Draw uses.
        /// Port: split out of Draw so NightsLoadingScreen draws this edition's bar too.
        /// </summary>
        protected void DrawBarOverlay(Graphics g)
        {
            g.DrawImage(Res.IMAGE_LOAD_OVERLAY, LOADING_BAR_X, mNightsMode ? LOADING_NIGHTS_LOAD_LIGHT_OFFSET + LOADING_BAR_Y : LOADING_BAR_Y);
#if PEGGLE_IPHONE
            for (int i = 0; i < mNumLights; i++)
            {
                DrawLight(g, i);
            }
#else
            if (mNumLights < 0x19)
            {
                for (int i = 0; i < mNumLights; i++)
                {
                    DrawLight(g, i);
                }
            }
#endif
        }

        /// <summary>The loading phrase under the bar until the Play button shows (untranslated coordinates).</summary>
        protected void DrawPhrase(Graphics g)
        {
            if (mPhrase.Length > 0 && !mPlayButton.mVisible)
            {
                g.SetFont(Res.FONT_OVERLOAD16OUTLINE);
                g.SetColor(SexyUtil.MakeColor(0xffec91));
                int w = g.GetFont().StringWidth(mPhrase);
                g.DrawString(mPhrase, (mWidth - w) / 2, LOADING_BAR_TEXT_Y + LOADING_NIGHTS_LOAD_LIGHT_OFFSET);
            }
        }

        private void DrawLogo(Graphics g, int theX, float theCurveY)
        {
            if (!mNightsMode)
            {
                g.DrawImage(LogoImage, (int)(theX * H / 600.0 + LOADING_LEFT_OFFSET), (int)(theCurveY * H / 600.0f));
            }
            else
            {
                g.DrawImage(Res.IMAGE_LOAD_NIGHTS_LOGO, (int)((theX - LOADING_NIGHTS_LOGO_OFFSET_X) * H / 600.0),
                    (int)(theCurveY * H / 600.0 - LOADING_NIGHTS_LOGO_OFFSET));
            }
        }

        /// <summary>Solve2Eq: a1*x + b1*y = c1, a2*x + b2*y = c2.</summary>
        private static void SolveLogoCurve(float a1, float b1, float c1, float a2, float b2, float c2, out float x, out float y)
        {
            float det = a1 * b2 - a2 * b1;
            x = (c1 * b2 - c2 * b1) / det;
            y = (a1 * c2 - a2 * c1) / det;
        }

        public override void KeyChar(SexyChar theChar)
        {
            if (theChar.value_type == 'b')
            {
                DoBallBounce();
            }
        }

        public void ButtonPress(int theId)
        {
            mApp.PlaySample(Res.SOUND_BUTTON1);
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
        }

        public void ButtonDepress(int theId)
        {
            if (mApp.mPlayerInfo == null)
            {
                mPlayButton.SetDisabled(true);
                mApp.DoCreateUserDialog();
            }
            else if (mApp.mRestoreLocation == 1)
            {
                if (!mApp.RestoreGame())
                {
                    mApp.ShowMainMenu();
                }
                else
                {
                    UnloadMainMenuGroups();
                }
            }
            else
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

        public override void MouseDown(int x, int y, int theClickCount)
        {
            if (mPlayButton.mVisible)
            {
                ButtonDepress(0);
            }
        }
    }
}
