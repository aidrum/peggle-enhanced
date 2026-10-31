using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Port addition: the Peggle WoW edition's loading screen (PeggleWoW.exe Sexy::LoadingScreen, its
    /// hardware-accelerated path), shown for the Menu Theme Last Played after a WoW level. A dark world
    /// brightens over ~9 s while the sun sets; the dragon flies in, Bjorn gets scorched, the Peggle logo
    /// drops and the WoW logo and "EDITION" follow; fireflies drift (and pop when tapped), then the ball
    /// drops on the logo. The original's "Click to Play" is this edition's Play button.
    /// Coordinates are the PC's 800x600, scaled by H / 600 and centred; the art is wow_loading
    /// (tools/build_wow_assets.py). RE notes: D:\peggle-re-wow\notes_loading.md.
    /// </summary>
    public class WoWLoadingScreen : LoadingScreen
    {
        private static readonly Random sRand = new Random();

        /// <summary>The star field's spots (PC 0x6b68f0); the last two are the table's empty entries.</summary>
        private static readonly int[] STAR_XY =
        {
            313, 55, 475, 68, 758, 129, 724, 30, 709, 149, 678, 193, 636, 127, 624, 23, 616, 26, 594, 99,
            583, 65, 515, 47, 512, 102, 509, 82, 505, 37, 505, 125, 504, 30, 475, 74, 443, 58, 438, 38,
            348, 36, 314, 92, 312, 36, 270, 126, 249, 66, 154, 104, 0, 0, 0, 0,
        };

        private static readonly int[] STAR_PERIODS = { 91, 173, 137 };

        /// <summary>
        /// How far (PC px) the ground hill and Bjorn sit lower than on the PC, whose window frame hid the
        /// screen's bottom 34 px; here the whole 800x600 shows, so they move down into that strip.
        /// </summary>
        private const int GROUND_DROP_Y = 20;

        private PCSheet mSheet;

        /// <summary>
        /// The Peggle logo: this edition's own, without the "HD" badge (iPad peggleloader_nohd, iPhone
        /// peggleloader), instead of the PC's; it is already at the edition's scale.
        /// </summary>
        private Image mLogoImage;

        /// <summary>The Play button art shrunk to the PC's "Click to Play" width (227 PC px), to fit the smaller WoW bar.</summary>
        private Image mPlayImage;

        /// <summary>The fireflies and their sparks (PC 0xd0, 0xf0).</summary>
        private LoadingFireflies mFlies;

        /// <summary>0 PopCap logo, 1 fading in, 2 the scene (PC 0x2f0..0x2f8).</summary>
        private int mState;

        private int mStateCounter;

        private int mStateDuration = 200;

        private float mWoWDisplayPct;                             // 0xa4

        private float mFireflyGate;                               // 0xac

        private float mBallFade;                                  // 0x2dc

        private int mLogoCounter2;                                // 0x2e8

        private float mLogoX = 181;                               // 0x2fc

        private float mLogoY = 140;                               // 0x300

        private float mStarAlpha;                                 // 0xd48

        private readonly CurvedVal mLogoDropCurve = new CurvedVal("b+-1,1,0.01,1.25,~###9~###       {J}1V kPZRD");          // 0x308

        private readonly CurvedVal mLogoScaleCurve = new CurvedVal("b+0.75,1,0.01,1.25,####         ~~Tob");                // 0x398

        private readonly CurvedVal mParallaxCurve = new CurvedVal("b+-1,0,0.01,8,~###  V~log       J#GT&");                 // 0x428

        private readonly CurvedVal mDragonAlphaCurve = new CurvedVal("b+0,1,0.01,8.5,####         O####Q~####~H,}");       // 0x4b8

        private readonly CurvedVal mDragonScaleCurve = new CurvedVal("b+0,1,0.01,8.5,P###         OP###Q~H,}");            // 0x548

        private readonly CurvedVal mFlameCurve = new CurvedVal("b+0,2.5,0.01,9.5,####         7#####~f5bj~v-$##H,}");       // 0x5d8

        private readonly CurvedVal mDragonXCurve = new CurvedVal("b+0,1.25,0.01,8.5,P###         FP###ZyH,}");             // 0x668

        private readonly CurvedVal mDragonYCurve = new CurvedVal("b+0,140,0.01,8.5,####         :####6U8uG7~###:Jhsd(FLIn"); // 0x6f8

        private readonly CurvedVal mHillScaleCurve = new CurvedVal("b+0,1,0,8,~rd@         ~#EuU");                         // 0x788

        private readonly CurvedVal mBrightCurve = new CurvedVal("b+0,255,0,9,~###         ,~###u#H`J");                    // 0x818

        private readonly CurvedVal mWoWLogoCurve = new CurvedVal("b+-80,30,0.01,0.3,####      u~###   ,e###");             // 0x8a8

        private readonly CurvedVal mEditionCurve = new CurvedVal("b+0,1,0,9,####        |#### %~###");                     // 0x938

        /// <summary>The dragon's fire intensity (PC 0x9fc).</summary>
        private float mFlameIntensity;

        /// <summary>The dragon's fire (PC this+0x9dc).</summary>
        private FlameEffect mFlame;


        private static int H => SexyUtil.gPhysicalDisplayHeight;

        private static float S => H / 600.0f;

        /// <summary>The x of the PC screen's left edge: the 800-wide scene is centred.</summary>
        private int OX => (mWidth - (int)(800 * S)) / 2;

        public WoWLoadingScreen(ThunderballApp theApp) : base(theApp)
        {
            mSheet = PCSheet.Load("images/pc/wow_loading");
            Image aPlayNow = Res.IMAGE_LOAD_PLAYNOW;
            mPlayImage = SexyUtil.LoadScaledImage("images/" + GlobalStaticVars.mResPathPrefix + "loading/playnow", 227 * S / aPlayNow.mWidth);
            if (mPlayImage != null)
            {
                int w = mPlayImage.mWidth;
                int h = mPlayImage.mHeight;
                mPlayButton.mButtonImage = mPlayImage;
                mPlayButton.mWidth = w;
                mPlayButton.mHeight = h / 2;
                mPlayButton.mNormalRect = new TRect(0, 0, w, h / 2);
                mPlayButton.mOverRect = new TRect(0, h / 2, w, h / 2);
            }
            if (mSheet != null)
            {
                mFlies = new LoadingFireflies(mApp, mSheet.mImage, mSheet["FLY0"], mSheet["FLY1"]);
                mFlame = new FlameEffect(mSheet.mImage, mSheet["FLAME_STRIP"], mSheet["SPARK"]);
                mFlame.mRect = new TRect(408, 134, 10, 10);
            }
#if PEGGLE_IPHONE
            mLogoImage = mApp.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "loading/peggleloader", true);
#else
            mLogoImage = mApp.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "loading/peggleloader_nohd", true);
#endif
        }

        public override void Dispose()
        {
            mFlame?.StopSound();
            mSheet?.Dispose();
            mSheet = null;
            mLogoImage?.Dispose();
            mLogoImage = null;
            mFlies?.Dispose();
            mFlies = null;
            // The button still points at mPlayImage until base.Dispose removes it.
            base.Dispose();
            mPlayImage?.Dispose();
            mPlayImage = null;
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            PlacePlayButton();
        }

        /// <summary>Port: the loading bar (frame, lights, Play button) is BAR_DROP PC px lower than the PC's, with no "Loading..." under it.</summary>
        private const int BAR_DROP = 30;

        /// <summary>Centred where the PC's "Click to Play" button was (288,504, 227x33), lowered with the bar.</summary>
        protected override void PlacePlayButton()
        {
            mPlayButton.Move(OX + (int)(401 * S) - mPlayButton.mWidth / 2, (int)((520 + BAR_DROP) * S) - mPlayButton.mHeight / 2);
        }

        private float T => mUpdateCnt / 100.0f;

        // ---------------------------------------------------------------- Update

        public override void Update()
        {
            WidgetUpdate();
            int aCount = mUpdateCnt;
            mFlameIntensity = aCount > 1300 && MouseInRect(392, 78, 40, 80) ? 3.2f : (float)mFlameCurve.GetOutVal(T);
            if (mFlame != null)
            {
                mFlame.mIntensity = mFlameIntensity;
                mFlame.Update(mApp);
            }
            if (mStateDuration != -1 && mStateCounter >= mStateDuration)
            {
                switch (mState)
                {
                case 0:
                    mStateDuration = 100;
                    mState = 1;
                    break;
                case 1:
                    mState = 2;
                    mStateDuration = 200;
                    break;
                case 2:
                    mStateDuration = 1000;
                    break;
                }
            }
            if (mState != mPrevState)
            {
                mStateCounter = 0;
                if (mState == 1)
                {
                    mWoWDisplayPct = 0;
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
                // The original plays the moonlight piece here instead of the loading music; the port
                // plays it as a song, then loops the opening ambience.
                mApp.PlayOpeningMusic(ThunderballApp.WOW_OPENING_SONG, ThunderballApp.WOW_OPENING_AMBIENCE_SONG);
            }
            UpdateBall();
            UpdateParticles();
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
                    mPlayButton.SetVisible(mState != 0);
                }
            }
            mWoWDisplayPct = Math.Min(1.0f, mWoWDisplayPct + 0.00115f);
            if (mWoWDisplayPct > 0.42f)
            {
                mLogoCounter2++;
            }
            if (mState != 0 && aCount > 0x500)
            {
                mWoWDisplayPct = 1.0f;
                if (mBallState == 0)
                {
                    DoBallBounce();
                }
            }
            float aBallFade = 1.0f;
            if (mBallState > 1)
            {
                mBallFade = Math.Min(1.0f, mBallFade + 0.025f);
                aBallFade = mBallFade;
            }
            mFireflyGate = Math.Min(mWoWDisplayPct * 1.45f, aBallFade);
            mFlies?.Update(this, mState != 0 && mFireflyGate == 1.0f, OX, S);
            MarkDirty();
        }

        private int mPrevState;

        private bool MouseInRect(int theX, int theY, int theW, int theH)
        {
            if (mWidgetManager == null)
            {
                return false;
            }
            float x = (mWidgetManager.mLastMouseX - mX - OX) / S;
            float y = (mWidgetManager.mLastMouseY - mY) / S;
            return x >= theX && x < theX + theW && y >= theY && y < theY + theH;
        }

        /// <summary>The ball drop on the logo (PC FUN_004bd730): Peggle's, bouncing at y 160.</summary>
        private new void UpdateBall()
        {
            if (mBallState <= 0 || mBallState >= 3)
            {
                return;
            }
            mBallVelY += 0.2f;
            mBallX += mBallVelX;
            mBallY += mBallVelY;
            if (mBallY > 160.0f && mBallState == 1)
            {
                mBallBounceCounter = 1;
                mBallState = 2;
                mBallVelY *= -0.75f;
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
                SpawnTrail();
            }
        }

        private void SpawnTrail()
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

        /// <summary>PC FUN_004be780 (trail half): the ball's trail wobbles out (no turning, unlike Peggle's).</summary>
        private new void UpdateParticles()
        {
            for (int i = 0; i < mParticles.Count; i++)
            {
                TrailParticle p = mParticles[i];
                p.mLife -= 6;
                if (p.mLife < 1)
                {
                    mParticles[i] = mParticles[mParticles.Count - 1];
                    mParticles.RemoveAt(mParticles.Count - 1);
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

        private void CelF(Graphics g, string theCel, float x, float y)
        {
            g.DrawImageF(mSheet.mImage, OX + x * S, y * S, mSheet[theCel]);
        }

        /// <summary>
        /// Port: the scene is the PC screen in a centred 4:3 box, unclipped; on iPhone the two backdrop
        /// layers (sky, canyon) zoom about its centre to cover the window.
        /// </summary>
        private readonly PCScene mScene = new PCScene();

        /// <summary>
        /// A backdrop layer as wide as the PC screen, at its left edge (zoomed on iPhone). With
        /// theCoverBottom, a layer ending above the screen's bottom has its last row stretched down to it
        /// (the PC's frame hid that strip).
        /// </summary>
        private void Wide(Graphics g, string theCel, float y, bool theCoverBottom = false)
        {
            TRect aSrc = mSheet[theCel];
            TRect aDest = mScene.Far(0, y, aSrc.mWidth, aSrc.mHeight);
            int aX = aDest.mX;
            int aW = aDest.mWidth;
            g.DrawImage(mSheet.mImage, aDest, aSrc);
            int aBottom = aDest.mY + aDest.mHeight;
            if (theCoverBottom && aBottom < mHeight)
            {
                // Overlaps the layer by a row, so no seam shows.
                g.DrawImage(mSheet.mImage, new TRect(aX, aBottom - 1, aW, mHeight - aBottom + 1), new TRect(aSrc.mX, aSrc.mY + aSrc.mHeight - 1, aSrc.mWidth, 1));
            }
        }

        /// <summary>Sexy DrawImageTransformF with a scale: theCel centred on (x,y), scaled.</summary>
        private void CelScaled(Graphics g, string theCel, float theScale, float x, float y)
        {
            TRect aSrc = mSheet[theCel];
            float w = aSrc.mWidth * theScale;
            float h = aSrc.mHeight * theScale;
            g.DrawImage(mSheet.mImage, new TRect((int)(OX + x * S - w / 2), (int)(y * S - h / 2), (int)w, (int)h), aSrc);
        }

        private int CelW(string theCel) => (int)Math.Round(mSheet[theCel].mWidth / S);

        private int CelH(string theCel) => (int)Math.Round(mSheet[theCel].mHeight / S);

        // ---------------------------------------------------------------- Draw (PC vf26, hardware path)

        public override void Draw(Graphics theG)
        {
            if (mSheet == null || mLogoImage == null)
            {
                return;
            }
            mScene.Update(mWidth, mHeight);
            mScene.SetFarSpan(0, 800);
            Graphics g = PCScene.Wide(theG);
            float t = T;
            int aLogoY = (int)mLogoY;
            float aLogoScale = 0.5f;
            if (mLogoCounter2 > 0)
            {
                aLogoY = (int)(mLogoDropCurve.GetOutVal(mLogoCounter2 / 100.0) * 400 + aLogoY);
                aLogoScale = (float)mLogoScaleCurve.GetOutVal(mLogoCounter2 / 100.0);
            }
            g.SetColor(new Color(0, 0, 0));
            g.FillRect(mScene.mWindowX, 0, mScene.mWindowW, mHeight);
            g.SetColor(new Color(255, 255, 255));
            if (mState != 0)
            {
                DrawScene(g, t, ref aLogoY, aLogoScale);
            }
            if (mState != 0)
            {
                if (mLoadingPct < 1.0f)
                {
                    for (int i = 0; i < mNumLights; i++)
                    {
                        DrawWoWLight(g, i);
                    }
                }
            }
            DeferOverlay(0);
            if (mUpdateCnt > 1400)
            {
                float a = Math.Min(1.0f, (mUpdateCnt - 1400) / 50.0f);
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(a * 255)));
                Cel(g, "TM", (int)(mLogoX + 406), (int)(mLogoY + 172));
                g.SetColorizeImages(false);
                g.SetColor(new Color(255, 255, 255));
            }
        }

        private void DrawScene(Graphics g, float t, ref int aLogoY, float aLogoScale)
        {
            float aBright = (float)mBrightCurve.GetOutVal(t);
            float aPar = (float)mParallaxCurve.GetOutVal(t);
            float pct = mWoWDisplayPct;
            Wide(g, "BACK", -57 - aPar * 40);
            int v = (int)Math.Max(0, 255 - pct * 300);
            if (aBright > 0)
            {
                g.SetColorizeImages(true);
                g.SetColor(new Color(v, v, v, (int)aBright));
                Wide(g, "BACKBRIGHT", 0);
            }
            g.SetColorizeImages(false);
            DrawStars(g);
            int aSunY = (int)(pct * 1000 - 214);
            if (pct < 0.6f)
            {
                g.SetColorizeImages(true);
                int aGray = (int)(255 - pct * 255);
                g.SetColor(new Color(aGray, aGray, aGray));
                // Drawn rotated by the sun angle (PC 0x98), which the WoW screen never changes from 0.
                Cel(g, "SUNGLOW", -6, aSunY);
                g.SetColorizeImages(false);
            }
            if (mLogoCounter2 > 0 && mLogoCounter2 < 0x76)
            {
                DrawLogoScaled(g, aLogoScale, aLogoY);
            }
            Wide(g, "HILL3", aPar * 140 + 198, true);
            g.SetColorizeImages(true);
            int aAlpha = (int)aBright;
            int aGray2 = (int)(255 - pct * 255);
            g.SetColor(new Color(aGray2, aGray2, aGray2, Math.Max(0, Math.Min(255, aAlpha))));
            Wide(g, "HILL3BRIGHT", (int)(aPar * 140 + 197), true);
            g.SetColorizeImages(false);
            int b = mBallBounceCounter;
            if (b > 0 && b < 10)
            {
                int d = (int)(8 - b * 1.5);
                aLogoY += d;
                mLogoY += d;
            }
            bool aLate = mUpdateCnt > 930;
            float aHillScale = (float)mHillScaleCurve.GetOutVal(t);
            CelScaled(g, "HILL2", aHillScale * 0.6f + 0.4f, 400, aPar * 122 + 652 + GROUND_DROP_Y);
            g.SetColorizeImages(true);
            g.SetColor(new Color(aGray2, aGray2, aGray2, Math.Max(0, Math.Min(255, aAlpha))));
            CelScaled(g, "HILL2BRIGHT", aHillScale * 0.6f + 0.4f, 400, aPar * 122 + 652 + GROUND_DROP_Y);
            g.SetColorizeImages(false);
            g.SetColor(new Color(255, 255, 255));
            if (!aLate)
            {
                CelScaled(g, "BJORN", aHillScale * 0.65f + 0.5f, 398, aPar * 62 + 536 + GROUND_DROP_Y);
            }
            DrawDragon(g, t);
            if (aLate)
            {
                float f = Math.Min(1.0f, (mUpdateCnt - 930) / 20.0f);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(f * 255)));
                g.SetColorizeImages(true);
                CelF(g, "BJORN_SCORCHED", 370, 376 - aPar * 62 + GROUND_DROP_Y);
                int c = mUpdateCnt;
                if ((c > 1100 && c < 1115) || (c > 1150 && c < 1165))
                {
                    Cel(g, "BJORN_EYES_CLOSED", 406, 449 + GROUND_DROP_Y);
                }
                g.SetColorizeImages(false);
                g.SetColor(new Color(255, 255, 255));
            }
            if (mLogoCounter2 >= 0x76)
            {
                int c = mUpdateCnt;
                if (c >= 820 && c <= 840)
                {
                    Cel(g, "WOWLOGO", 285, (int)(mWoWLogoCurve.GetOutVal((c - 820) / 100.0) + 296));
                }
                DrawLogoScaled(g, aLogoScale, aLogoY);
                if (mBallBounceCounter != 0)
                {
                    g.SetColorizeImages(true);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    float aGlow = SexyUtil.InterpValF(mBallBounceCounter, 5, 25, 0.0f, 1.0f, 0.0f);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(aGlow * 128)));
                    g.DrawImage(mLogoImage, PX((int)mLogoX), PY(aLogoY));
                    g.SetColorizeImages(false);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                }
                if (c > 840)
                {
                    Cel(g, "WOWLOGO", 285, (int)(mWoWLogoCurve.GetOutVal((c - 820) / 100.0) + 296));
                    float e = (float)mEditionCurve.GetOutVal(t);
                    if (e > 0)
                    {
                        if (e < 1.0f)
                        {
                            g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(e * 255)));
                            g.SetColorizeImages(true);
                        }
                        Cel(g, "WOW_EDITION_TEXT", 315, 382);
                        g.SetColorizeImages(false);
                        g.SetColor(new Color(255, 255, 255));
                    }
                }
            }
            g.SetColorizeImages(true);
            int aBarGray = (int)(255 - pct * 60);
            g.SetColor(new Color(aBarGray, aBarGray, aBarGray));
            CelF(g, "OVERLAY", 272, 493 + BAR_DROP);
            g.SetColorizeImages(false);
            g.SetColor(new Color(255, 255, 255));
        }

        /// <summary>The Peggle logo stretched by theScale about its middle (PC: DrawImage with a dest rect).</summary>
        private void DrawLogoScaled(Graphics g, float theScale, int theLogoY)
        {
            int aW = (int)Math.Round(mLogoImage.mWidth / S);
            int aH = (int)Math.Round(mLogoImage.mHeight / S);
            int w = (int)(aW * theScale);
            int h = (int)(aH * theScale);
            int x = (int)((aW - w) / 2 + mLogoX);
            int y = (aH - h) / 2 + theLogoY;
            g.DrawImage(mLogoImage, new TRect(PX(x), PY(y), (int)(w * S), (int)(h * S)), new TRect(0, 0, mLogoImage.mWidth, mLogoImage.mHeight));
        }

        /// <summary>The dragon flying in (scaled by Graphics' scale about (1,1), as the PC draws it).</summary>
        private void DrawDragon(Graphics g, float t)
        {
            float ds = (float)mDragonScaleCurve.GetOutVal(t);
            float dx = (float)mDragonXCurve.GetOutVal(t);
            float dy = (float)mDragonYCurve.GetOutVal(t);
            float da = (float)mDragonAlphaCurve.GetOutVal(t);
            if (da != 1.0f)
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(da * 255)));
            }
            int aW = CelW("DRAGON");
            int aH = CelH("DRAGON");
            int x = (int)(dx + 397 - aW * ds * 0.5f);
            int y = (int)(dy + 91 - aH * ds * 0.5f);
            // Graphics::SetScale(ds, ds, 1, 1): positions scale about (1,1) too.
            float aX = 1 + (x - 1) * ds;
            float aY = 1 + (y - 1) * ds;
            TRect aSrc = mSheet["DRAGON"];
            if (ds > 0)
            {
                g.DrawImage(mSheet.mImage, new TRect((int)(OX + aX * S), (int)(aY * S), (int)(aSrc.mWidth * ds), (int)(aSrc.mHeight * ds)), aSrc);
            }
            if (da != 1.0f)
            {
                g.SetColor(new Color(255, 255, 255));
                g.SetColorizeImages(false);
            }
            mFlame.mRect = new TRect(408, 134, 10, 10);
            mFlame.Draw(g, OX, 0, S);
            if (t >= mDragonScaleCurve.mInMax)
            {
                Cel(g, "DRAGON_HEAD", 398, 100);
            }
        }

        /// <summary>PC FUN_004bfcb0: three twinkling red stars once the sky is mostly dark.</summary>
        private void DrawStars(Graphics g)
        {
            if (mWoWDisplayPct <= 0.75f)
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
                g.SetColor(SexyUtil.MakeColor(0xff0000, (int)(f * 150 * mStarAlpha)));
                g.SetColorizeImages(true);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                Cel(g, "STAR", STAR_XY[i * 2] - 5, STAR_XY[i * 2 + 1] - 5);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                g.SetColorizeImages(false);
            }
            g.SetColor(new Color(255, 255, 255));
        }

        private void DrawWoWLight(Graphics g, int theLight)
        {
            int aCel;
            switch (theLight)
            {
            case 0:
                aCel = 4;
                break;
            case 1:
                aCel = 3;
                break;
            case 0x17:
                aCel = 1;
                break;
            case 0x18:
                aCel = 0;
                break;
            default:
                aCel = 2;
                break;
            }
            TRect aLights = mSheet["LIGHTS"];
            int aCelW = aLights.mWidth / 5;
            g.DrawImage(mSheet.mImage, PX(theLight * 9 + 0x11f), PY(0x1fa + BAR_DROP), new TRect(aLights.mX + aCel * aCelW, aLights.mY, aCelW, aLights.mHeight));
        }

        // ---------------------------------------------------------------- DrawOverlay (PC vf44)

        public override void DrawOverlay(Graphics theG)
        {
            if (mSheet == null)
            {
                return;
            }
            Graphics g = PCScene.Wide(theG);
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
            }
            else
            {
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
                if (mBallState > 0)
                {
                    TRect aBall = mSheet["BALL"];
                    g.DrawImageF(mSheet.mImage, OX + mBallX * S - aBall.mWidth / 2.0f, mBallY * S - aBall.mHeight / 2.0f, aBall);
                }
                mFlies?.Draw(g, OX, S);
                DrawParticles(g);
            }
            if (mState == 1)
            {
                g.SetColor(new Color(0, 0, 0, 255 - mStateCounter * 255 / mStateDuration));
                g.FillRect(mScene.mWindowX, 0, mScene.mWindowW, mHeight);
                g.SetColor(new Color(255, 255, 255));
            }
        }

        /// <summary>PC FUN_004befe0: the trail and sparks as small additive specks.</summary>
        private void DrawParticles(Graphics g)
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
            mFlies?.DrawSparks(g, OX, S);
        }
    }
}
