using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Port addition: the Peggle WoW main menu's animated scene (PeggleWoW.exe Sexy::InstituteLounge, its
    /// hardware path, for a player who hasn't finished WoW's adventure): the portal and the Peggle land
    /// through it with its sun, the portal's vortex (MainMenuEffect), the torch fire, Bjorn with his
    /// quest marker, the sparkles running round his outline and the rune lines over the arch. Only the
    /// picture: the main menu's buttons and links stay the mobile ones. With theSplork (the stage cutscene,
    /// WoWCutscene, once the adventure has been won) Splork stands there instead, with his flames and
    /// lightning, as on the main menu once the player has won it, and the banner the adventure's win hangs
    /// (trophy 0's decals; StartBannerEntrance burns it in the first time). Not ported: the other trophies
    /// and decals, the Arthas easter egg, the hover effects (glow, speech bubbles, clicking the master).
    /// The PC scene (605x539 at 8,5 of 800x600) is scaled to fit the area it is drawn in, at its top-left; its art is wow_mainmenu (tools/build_wow_assets.py), resampled to that scale.
    /// RE notes: D:/peggle-re-wow/notes_menu.md.
    /// </summary>
    public class WoWMenuScene
    {
        private const int SCENE_X = 8;

        private const int SCENE_Y = 5;

        private const int SCENE_W = 605;

        private const int SCENE_H = 539;

        /// <summary>Splork's x (PC this+0xa0); he is drawn at y 70, his flames and lightning at (x + 107, 176).</summary>
        private const float SPLORK_X = -5;

        /// <summary>Bjorn's spot (PC this+0xa4/0xa8).</summary>
        private const float BJORN_X = 0;

        private const float BJORN_Y = 151;

        /// <summary>Where the vortex's emitters are drawn from, the portal's middle (MainMenuEffect draw, 0x402af0).</summary>
        private const float VORTEX_X = 380;

        private const float VORTEX_Y = 334;

        /// <summary>The rune lines on Bjorn's armour (setup 0x437d00).</summary>
        private static readonly Vector2[] RUNE_POS = { new Vector2(67, 298), new Vector2(48, 343), new Vector2(119, 360), new Vector2(45, 395) };

        private static readonly Random sRand = new Random();

        private PCSheet mSheet;

        /// <summary>The frame counter (PC this+0xb0).</summary>
        private int mCount;

        private readonly ParticleEmitter mVortexFlames = new ParticleEmitter();   // MainMenuEffect +0x04

        private readonly ParticleEmitter mVortexClouds = new ParticleEmitter();   // MainMenuEffect +0x110

        private readonly ParticleEmitter mVortexTrail = new ParticleEmitter();    // MainMenuEffect +0x21c

        private readonly ShapeParticleEmitter mOutline = new ShapeParticleEmitter(); // +0x3a0

        private readonly List<ShapeParticleEmitter> mRunes = new List<ShapeParticleEmitter>(); // +0x854

        private readonly ParticleEmitter mSplorkFlames = new ParticleEmitter();   // +0x184

        private readonly ParticleEmitter mSplorkLightning = new ParticleEmitter(); // +0x290

        /// <summary>Splork stands in for Bjorn (PC vf71: the player has won the adventure).</summary>
        public readonly bool mSplork;

        /// <summary>
        /// The banner (vf70 0x43b897, trophy 0, won with the adventure, so shown with Splork): the pole decal
        /// (IMAGE_MM_TABARD) and the waving banner decal (IMAGE_MM_TABARD_STRIP, 4x4 cels at 0.2 a frame), drawn
        /// at their rects' (324,162) and (361,196) less their (7,19) and (7,26) offsets.
        /// </summary>
        private const int TABARD_X = 317;

        private const int TABARD_Y = 143;

        private const int TABARD_STRIP_X = 354;

        private const int TABARD_STRIP_Y = 170;

        /// <summary>The waving banner's cel size at PC scale (tabard_strip is 496 x 900, 4 x 4 cels).</summary>
        private const int TABARD_CEL_W = 124;

        private const int TABARD_CEL_H = 225;

        /// <summary>The banner's rect (vf70: its hover and sparkle area), 108 x 184 at (361,196).</summary>
        private const int TABARD_RECT_X = 361;

        private const int TABARD_RECT_Y = 196;

        private const int TABARD_RECT_W = 108;

        private const int TABARD_RECT_H = 184;

        /// <summary>How long the banner burns in (PC this+0x180).</summary>
        private const int BURN_LENGTH = 100;

        /// <summary>The burn's update count while the banner burns in, else -1 (PC this+0xe8).</summary>
        private int mBurn = -1;

        /// <summary>
        /// The burning edge's flames (PC E5, this+0x6c0): fills along a short line that follows the edge.
        /// (Its twin E4, +0x530, black, never spawns: its fill rate only ever decays from 0.)
        /// </summary>
        private readonly ShapeParticleEmitter mBurnFlames = new ShapeParticleEmitter();

        /// <summary>The sparkles over the banner when it first appears (the strip decal's +0x1c effect).</summary>
        private AreaParticleEffect mBannerSparkles;

        /// <summary>How much of the banner shows while it burns in (Draw, times 210).</summary>
        private static readonly CurvedVal sBurnClipCurve = new CurvedVal("b+0,1,0,1,#### ;####       z~P##j~P##");

        /// <summary>Where the flames ride (this+0xf0, set each Update; times 210).</summary>
        private static readonly CurvedVal sBurnEdgeCurve = new CurvedVal("b+0,1,0,1,####        %|P## |~P##");

        /// <summary>The flames' fill rate (0x6f47a8).</summary>
        private static readonly CurvedVal sBurnRateCurve = new CurvedVal("b+0,1,0,1,#### %~###     )~yD? `IjN3  9####");

        /// <summary>The flames' line length and offset (0x6f4718).</summary>
        private static readonly CurvedVal sBurnSpreadCurve = new CurvedVal("b+0.1,1,0,1,####   &~###      {~###");

        private static readonly string[] SPARKLE_CELS = { "TROPHY_PARTICLE1", "TROPHY_PARTICLE2", "TROPHY_PARTICLE3", "TROPHY_PARTICLE4", "TROPHY_PARTICLE5", "TROPHY_PARTICLE6" };

        /// <summary>The quest marker over the master (PC this+0x86c; the cutscene hides it).</summary>
        public bool mShowQuestMarker = true;

        /// <summary>
        /// Bjorn's quest marker (0x43dba0): a yellow "!" while the adventure hasn't been started (PC player
        /// +0x24, the last stage played, is -1), a grey "?" once it has. Splork's is always a blue "!".
        /// </summary>
        public bool mQuestStarted;

        private static float Rand01()
        {
            return sRand.Next(10000) / 10000.0f;
        }

        private static float Rand1000()
        {
            return sRand.Next(1000) / 1000.0f;
        }

        public WoWMenuScene() : this(false)
        {
        }

        public WoWMenuScene(bool theSplork)
        {
            mSplork = theSplork;
            Dictionary<string, float[]> aShapes = LoadShapes("images/pc/wow_mainmenu_shapes.txt");
            // MainMenuEffect (0x402710): the portal's vortex, particles spiralling in on (334,380).
            mVortexFlames.mRate = 0.14f;
            mVortexFlames.mWobbleRate = 0.2f;
            mVortexFlames.mWobbleAmp = 170;
            mVortexFlames.mTemplate.mLife = 100;
            mVortexFlames.mTemplate.mAdditive = true;
            mVortexFlames.mAlphaCurve = new CurvedVal("b+0,0.3,0,1,#########   &~### j~### V~###   [####");
            mVortexFlames.mScaleCurve = new CurvedVal("b+0,0.5,0,1,~####~###         ~#Fu:");
            mVortexFlames.mInitPart = (e, p) => p.mPhase = Rand01();
            mVortexClouds.mRate = 1.14f;
            mVortexClouds.mWobbleRate = 0.2f;
            mVortexClouds.mWobbleAmp = 200;
            mVortexClouds.mTemplate.mLife = 100;
            mVortexClouds.mTemplate.mAdditive = true;
            mVortexClouds.mAlphaCurve = new CurvedVal("b+0,0.2,0,1,#### h#### o}### 7~### V~###   [####");
            mVortexClouds.mScaleCurve = new CurvedVal("b+0,0.5,0,1,~####~###         ~#Fu:");
            mVortexClouds.mInitPart = (e, p) => p.mPhase = Rand01();
            mVortexTrail.mRate = 0.003f;
            mVortexTrail.mRateRand = 0.01f;
            mVortexTrail.mWobbleRate = 2;
            mVortexTrail.mWobbleAmp = 210;
            mVortexTrail.mWobbleCurve = new CurvedVal("b+0,1,0,8,~###  *S### `Pn[*     Y####_####");
            mVortexTrail.mTemplate.mLife = 1000;
            mVortexTrail.mTemplate.mAdditive = true;
            mVortexTrail.mTemplate.mScaleX = mVortexTrail.mTemplate.mScaleY = 2.3f;
            mVortexTrail.mEchoCount = 50;
            mVortexTrail.mEchoStep = 3;
            mVortexTrail.mEchoScale = 0.1f;
            mVortexTrail.mAlphaCurve = new CurvedVal("b+0,0.08,0,9,#########X#### H~###G~###Opkt>    g%tu;  D####");
            mVortexTrail.mInitPart = (e, p) =>
            {
                p.mPhase = Rand01();
                p.mLife = (int)(Rand01() * 1000 + 1000);
                p.mScaleX = p.mScaleY = Rand01() * 0.8f + 0.7f;
            };
            // InstituteLounge setup (0x437d00): the sparkles round Bjorn's outline...
            mOutline.mFadeOutTime = 10;
            mOutline.mTemplate.mLife = 200;
            mOutline.mTemplate.mAdditive = true;
            mOutline.mTemplate.mAlpha = 0.4f;
            mOutline.mScaleCurve = new CurvedVal("b+0.1,0.8,0,2,#7)1     t~P##    -#O6R");
            mOutline.SetSpeed(0.3f);
            mOutline.mInitPart = InitOutlinePart;
            mOutline.mX = BJORN_X + 4;
            mOutline.mY = BJORN_Y + 314;
            AddShape(mOutline, aShapes, "BJORN_OUTLINE");
            // ...and the four rune lines traced over the arch.
            for (int i = 0; i < RUNE_POS.Length; i++)
            {
                ShapeParticleEmitter aRune = new ShapeParticleEmitter();
                aRune.mX = RUNE_POS[i].X;
                aRune.mY = RUNE_POS[i].Y;
                aRune.mTraceRate = 4.3f;
                aRune.mFadeInTime = 40;
                aRune.mFadeOutTime = 50;
                aRune.SetSpeed(6.8f);
                aRune.mDelayMin = 200;
                aRune.mDelayMax = 400;
                aRune.NewDelay();
                aRune.mScaleCurve = new CurvedVal("b+0,1,0,1,####    r~###     /####");
                aRune.mTemplate.mLife = 100;
                aRune.mTemplate.mAdditive = true;
                aRune.mTemplate.mAlpha = 0.2f;
                aRune.mInitPart = InitRunePart;
                AddShape(aRune, aShapes, "RUNE" + i);
                mRunes.Add(aRune);
            }
            mVortexFlames.Prewarm();
            mVortexClouds.Prewarm();
            // Splork's flames and lightning (setup 0x437d00), bursting out from his hands.
            mSplorkFlames.mRate = 0.35f;
            mSplorkFlames.mRateRand = 0.1f;
            mSplorkFlames.mFadeOutTime = 50;
            mSplorkFlames.mScaleCurve = new CurvedVal("b+0,1,0,1,~zTM  @~n[*       aHAi9");
            mSplorkFlames.mTemplate.mLife = 140;
            mSplorkFlames.mTemplate.mAdditive = true;
            mSplorkFlames.mInitPart = InitSplorkFlamePart;
            mSplorkLightning.mRate = 0.04f;
            mSplorkLightning.mRateRand = 0.02f;
            mSplorkLightning.mRotateByVel = true;
            mSplorkLightning.mFadeOutTime = 54;
            mSplorkLightning.mScaleCurve = new CurvedVal("b+0,1,0,1,~###         ~#?fD");
            mSplorkLightning.mTemplate.mLife = 65;
            mSplorkLightning.mTemplate.mAdditive = true;
            mSplorkLightning.mInitPart = InitSplorkLightningPart;
            // The banner's burning-edge flames (setup 0x437d00, E5).
            mBurnFlames.mTemplate.mVelX = 0.14f;
            mBurnFlames.mTemplate.mVelY = -1.64f;
            mBurnFlames.mTemplate.mLife = 30;
            mBurnFlames.mTemplate.mAdditive = true;
            mBurnFlames.mTemplate.mAccelX = 0;
            mBurnFlames.mTemplate.mAccelY = 0.0001f;
            mBurnFlames.mTemplate.mAlpha = 0.3f;
            mBurnFlames.mFadeInTime = 10;
            mBurnFlames.mFadeOutTime = 15;
            mBurnFlames.mImageCols = 2;
            mBurnFlames.mImageRows = 2;
            mBurnFlames.mInitPart = InitBurnFlamePart;
            mBurnFlames.SetSpeed(0.05f);
            if (mSplork)
            {
                mSplorkFlames.Prewarm();
                mSplorkLightning.Prewarm();
            }
        }

        private static Dictionary<string, float[]> LoadShapes(string thePath)
        {
            Dictionary<string, float[]> aShapes = new Dictionary<string, float[]>();
            if (!ContentFS.Exists(thePath))
            {
                return aShapes;
            }
            foreach (string aLine in Encoding.UTF8.GetString(ContentFS.ReadAllBytes(thePath)).Split('\n'))
            {
                string[] aParts = aLine.Trim().Split(' ');
                if (aParts.Length < 3)
                {
                    continue;
                }
                float[] aPoints = new float[aParts.Length - 1];
                for (int i = 1; i < aParts.Length; i++)
                {
                    aPoints[i - 1] = float.Parse(aParts[i], System.Globalization.CultureInfo.InvariantCulture);
                }
                aShapes[aParts[0]] = aPoints;
            }
            return aShapes;
        }

        private static void AddShape(ShapeParticleEmitter theEmitter, Dictionary<string, float[]> theShapes, string theName)
        {
            theEmitter.ClearShape();
            if (theShapes.TryGetValue(theName, out float[] aPoints))
            {
                for (int i = 0; i + 1 < aPoints.Length; i += 2)
                {
                    theEmitter.AddPoint(aPoints[i], aPoints[i + 1]);
                }
            }
        }

        /// <summary>InitPart (0x43d5b0) for the banner's flames: red or yellow, a random flame cel, living 15-24 or 50-64 updates.</summary>
        private static void InitBurnFlamePart(ParticleEmitter theEmitter, EmitterParticle p)
        {
            p.mColor1 = sRand.Next(2) != 0 ? 0xff4c57 : 0xf1e057;
            p.mCelSpeed = 0.1f;
            p.mCelOffset = p.mCel = sRand.Next(3);
            p.mLife = sRand.Next(10) < 5 ? 50 + sRand.Next(15) : 15 + sRand.Next(10);
        }

        /// <summary>
        /// The banner's entrance, the first time this player's menu shows it (vf70's new decals): it burns in
        /// from the top, flames riding its edge, with sparkles over it.
        /// </summary>
        public void StartBannerEntrance()
        {
            if (!mSplork)
            {
                return;
            }
            mBurn = 0;
            AreaParticleEffect anEffect = new AreaParticleEffect();
            anEffect.mCelsOnly = true;
            for (int i = 0; i < SPARKLE_CELS.Length; i++)
            {
                anEffect.AddParticleImageId(i); // IMAGE_TROPHY_PARTICLE1..6
            }
            anEffect.mSpawnRate.SetCurve("b+0,1,0,1,####  K[5|U   ^gl4>   v#C<v");
            anEffect.mPos = new SexyVector2(TABARD_RECT_X + TABARD_RECT_W / 2, TABARD_RECT_Y + TABARD_RECT_H / 2);
            anEffect.mWidth = TABARD_RECT_W;
            anEffect.mHeight = TABARD_RECT_H;
            anEffect.mCircular = false;
            anEffect.mAdditive = true;
            anEffect.mAlpha = 1.0f;
            anEffect.mDuration = 400;
            anEffect.mDensity = 0.1f * 8e-05;
            mBannerSparkles = anEffect;
        }

        /// <summary>InitPart (0x43d5b0) for the outline: blue to pink sparkles, drifting slowly to a stop.</summary>
        private static void InitOutlinePart(ParticleEmitter theEmitter, EmitterParticle p)
        {
            switch (sRand.Next(3))
            {
            case 0:
                p.mColor1 = 0x6666ff;
                p.mColor2 = 0xff0066;
                break;
            case 1:
                p.mColor1 = 0x2288ff;
                p.mColor2 = 0xff0066;
                break;
            default:
                p.mColor1 = 0x0000ff;
                p.mColor2 = 0;
                break;
            }
            p.mColorTime1 = 100;
            float aScale = sRand.Next(4) == 0 ? Rand01() * 1.5f + Rand01() * 0.2f : Rand01() * 1.9f + Rand01() * 0.1f;
            p.mScaleX = p.mScaleY = aScale;
            p.mVelX = Rand01() * 0.15f - 0.075f;
            p.mVelY = Rand01() * 0.15f - 0.075f;
            p.mAccelX = -p.mVelX * 0.0075f;
            p.mAccelY = -p.mVelY * 0.0075f;
        }

        /// <summary>InitPart (0x43d5b0) for Splork's flames: cyan, flung out fast in any direction and slowing.</summary>
        private static void InitSplorkFlamePart(ParticleEmitter theEmitter, EmitterParticle p)
        {
            float anAngle = (float)(Rand1000() * Math.PI * 2);
            float anAccel = Rand1000() * 0.005f - 0.03f;
            float aSpeed = Rand1000() * 0.1f + 1.8f;
            float aCos = (float)Math.Cos(anAngle);
            float aSin = (float)Math.Sin(anAngle);
            p.mColor1 = 0x42fdf5;
            p.mVelX = aCos * aSpeed;
            p.mVelY = aSin * aSpeed;
            p.mAccelX = aCos * anAccel;
            p.mAccelY = aSin * anAccel;
            p.mAlpha = 0.6f;
            p.mScaleX = p.mScaleY = 2;
        }

        /// <summary>InitPart for Splork's lightning: bolts sent off in any direction, speeding up.</summary>
        private static void InitSplorkLightningPart(ParticleEmitter theEmitter, EmitterParticle p)
        {
            float anAngle = (float)(Rand1000() * Math.PI * 2);
            float anAccel = Rand1000() * 0.01f + 0.02f;
            float aSpeed = Rand1000() * 0.1f + 0.5f;
            float aCos = (float)Math.Cos(anAngle);
            float aSin = (float)Math.Sin(anAngle);
            p.mColor1 = 0x6b8cd2;
            p.mVelX = aCos * aSpeed;
            p.mVelY = aSin * aSpeed;
            p.mAccelX = aCos * anAccel;
            p.mAccelY = aSin * anAccel;
            p.mAlpha = 1;
            p.mScaleX = p.mScaleY = 1;
        }

        /// <summary>InitPart for the runes: pink, then orange, then pale yellow, at a random size and angle.</summary>
        private static void InitRunePart(ParticleEmitter theEmitter, EmitterParticle p)
        {
            p.mColor1 = 0xff7e98;
            p.mColorTime1 = 30;
            p.mColor2 = 0xff8d23;
            p.mColorTime2 = 60;
            p.mColor3 = 0xfffca5;
            p.mScaleX = p.mScaleY = 0.25f + Rand01() * 0.25f;
            p.mAngle = (float)(Rand01() * Math.PI * 2);
        }

        public void Dispose()
        {
            mSheet?.Dispose();
            mSheet = null;
            mFrame?.Dispose();
            mFrame = null;
        }

        /// <summary>Port addition: the stage cutscene's selector frame, resampled to mFrameScale.</summary>
        private Image mFrame;

        private float mFrameScale;

        /// <summary>How far the frame reaches out past the scene (PC px), as the cutscene's does past its window.</summary>
        private const int FRAME_OUT_X = 6;

        private const int FRAME_OUT_Y = 8;

        /// <summary>The room left round the frame (PC px).</summary>
        private const int FRAME_GAP = 6;

        /// <summary>The frame art's border width (PC px).</summary>
        private const int FRAME_BORDER = 12;

        /// <summary>Port addition: where the last DrawFramed's frame border starts on the right (display px).</summary>
        public int mFrameInnerRight = int.MaxValue;

        /// <summary>
        /// Port addition (the main menu): the scene framed as the stage cutscene's is, fitted with its frame in
        /// (theX, theY, theW, theH) at its top-left: clipped to its own rect, in the cutscene's selector frame
        /// (wow_cutscene/selectorframe).
        /// </summary>
        public void DrawFramed(Graphics g, int theX, int theY, int theW, int theH)
        {
            double aFit = FitScale(theW, theH);
            int aMarginX = (int)((FRAME_OUT_X + FRAME_GAP) * aFit);
            int aMarginY = (int)((FRAME_OUT_Y + FRAME_GAP) * aFit);
            float F = (float)FitScale(Math.Max(1, theW - aMarginX * 2), Math.Max(1, theH - aMarginY * 2));
            int x = theX + aMarginX;
            int y = theY + aMarginY;
            int w = (int)(SCENE_W * F);
            int h = (int)(SCENE_H * F);
            mFrameInnerRight = x + w + (int)((FRAME_OUT_X - FRAME_BORDER) * F);
            Graphics aSceneG = g.Create();
            aSceneG.ClipRect(x, y, w, h);
            aSceneG.SetColor(new Color(0, 0, 0));
            aSceneG.FillRect(x, y, w, h);
            aSceneG.SetColor(Color.White);
            DrawAt(aSceneG, x - SCENE_X * F, y - SCENE_Y * F, F);
            if (mFrame == null || Math.Abs(mFrameScale - F) > 0.001f)
            {
                mFrame?.Dispose();
                mFrame = SexyUtil.LoadPCImage("images/pc/wow_cutscene/selectorframe", F);
                mFrameScale = F;
            }
            if (mFrame != null)
            {
                int anOutX = (int)(FRAME_OUT_X * F);
                int anOutY = (int)(FRAME_OUT_Y * F);
                g.SetColor(Color.White);
                g.DrawImageBox(new TRect(x - anOutX, y - anOutY, w + anOutX * 2, h + anOutY * 2), mFrame);
            }
        }

        /// <summary>The lounge's Update (0x43a140, hardware path).</summary>
        public void Update()
        {
            if (mSplork)
            {
                mSplorkFlames.Update(true);
                mSplorkLightning.Update(true);
                UpdateBanner();
            }
            mVortexFlames.Update(true);
            mVortexClouds.Update(true);
            mVortexTrail.Update(true);
            mOutline.Update(true);
            foreach (ShapeParticleEmitter aRune in mRunes)
            {
                aRune.Update(true);
            }
            if (mCount % 35 == 0)
            {
                mOutline.Fill(true, 100, 200, 0.5f);
            }
            mCount++;
        }

        /// <summary>
        /// The banner's burn-in (lounge Update 0x43a3c4-0x43a840): the flames' line moves down with the burning
        /// edge, filling with flames at a rate that rises and falls; then the sparkles.
        /// </summary>
        private void UpdateBanner()
        {
            mBurnFlames.mFillMinStep = 10;
            mBurnFlames.mFillMaxStep = 13;
            if (mBurn >= 0)
            {
                float t = mBurn / (float)BURN_LENGTH;
                float aSpread = (float)sBurnSpreadCurve.GetOutVal(t);
                float anOffset = (1 - aSpread) * 50;
                int anEdge = (int)(sBurnEdgeCurve.GetOutVal(t) * 210);
                mBurnFlames.mX = TABARD_STRIP_X - 0x1c + anOffset;
                mBurnFlames.mY = TABARD_STRIP_Y - 0x28 + anOffset + anEdge;
                mBurn++;
                mBurnFlames.ClearShape();
                mBurnFlames.AddPoint(50 * aSpread, 50 * aSpread);
                mBurnFlames.AddPoint(120 * aSpread, 0);
                if (mBurn >= BURN_LENGTH)
                {
                    mBurn = -1;
                    mBurnFlames.mFillRate = 0;
                    mBurnFlames.mFillRand = 0;
                }
                else
                {
                    float aRate = (float)sBurnRateCurve.GetOutVal(t);
                    mBurnFlames.mFillRate = aRate * 0.97f;
                    mBurnFlames.mFillRand = aRate * 0.25f;
                }
            }
            mBurnFlames.Update(true);
            if (mBannerSparkles != null && !mBannerSparkles.Update())
            {
                mBannerSparkles = null;
            }
        }

        /// <summary>
        /// The banner (the lounge Draw's decal loop, 0x43904c): the pole, then the waving banner, cut off below
        /// its burning edge while it burns in; then the flames.
        /// </summary>
        private void DrawBanner(Graphics g, float ox, float oy, float F)
        {
            g.DrawImage(mSheet.mImage, (int)(ox + TABARD_X * F), (int)(oy + TABARD_Y * F), mSheet["TABARD"]);
            // Each cel's rect is the PC one (124 x 225) scaled, so the rounding doesn't add up row by row
            // (dividing the scaled strip by 4 shifted the later frames' banner by a few pixels).
            TRect aStrip = mSheet["TABARD_STRIP"];
            int aFrame = (int)(mCount * 0.2f) % 16;
            int aCelX = (int)Math.Round(aFrame % 4 * TABARD_CEL_W * F);
            int aCelY = (int)Math.Round(aFrame / 4 * TABARD_CEL_H * F);
            int aCelW = (int)Math.Round(TABARD_CEL_W * F);
            int aCelH = (int)Math.Round(TABARD_CEL_H * F);
            TRect aSrc = new TRect(aStrip.mX + aCelX, aStrip.mY + aCelY, aCelW, aCelH);
            int x = (int)(ox + TABARD_STRIP_X * F);
            int y = (int)(oy + TABARD_STRIP_Y * F);
            if (mBurn >= 0)
            {
                int aShown = (int)(sBurnClipCurve.GetOutVal(mBurn / (float)BURN_LENGTH) * 210 * F);
                Graphics aClipG = g.Create();
                aClipG.ClipRect(x, y, aCelW, aShown);
                aClipG.DrawImage(mSheet.mImage, x, y, aSrc);
            }
            else
            {
                g.DrawImage(mSheet.mImage, x, y, aSrc);
            }
            mBurnFlames.Draw(g, ox, oy, F);
        }

        /// <summary>The banner's sparkles (the decal effect's DrawOverlay, 0x436710), from the sheet's trophy particles.</summary>
        private void DrawBannerSparkles(Graphics g, float ox, float oy, float F)
        {
            AreaParticleEffect anEffect = mBannerSparkles;
            if (anEffect == null)
            {
                return;
            }
            g.SetColorizeImages(true);
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            foreach (ParticleData p in anEffect.mParticles)
            {
                double aScale = anEffect.mScale.GetOutVal(p.mUpdateCnt / 100.0f);
                double anAlpha = anEffect.mParticleAlpha.GetOutVal(p.mUpdateCnt / 100.0f);
                g.SetColor(SexyUtil.MakeColor(p.mColor, SexyUtil.ClampRange((int)(anEffect.mAlpha * anAlpha * 255.0), 0, 0xff)));
                Transform aTrans = new Transform();
                aTrans.Scale((float)aScale, (float)aScale);
                g.DrawImageTransform(mSheet.mImage, aTrans, mSheet[SPARKLE_CELS[p.mImageIndex]], ox + p.mX * F, oy + p.mY * F);
            }
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
            g.SetColorizeImages(false);
            g.SetColor(Color.White);
        }

        /// <summary>The scale that fits the PC scene in theW x theH.</summary>
        private static double FitScale(int theW, int theH)
        {
            return Math.Min(theW / (double)SCENE_W, theH / (double)SCENE_H);
        }

        /// <summary>Draws the scene fitted in (theX, theY, theW, theH), at its top-left corner.</summary>
        public void Draw(Graphics g, int theX, int theY, int theW, int theH)
        {
            float F = (float)FitScale(theW, theH);
            DrawAt(g, theX - SCENE_X * F, theY - SCENE_Y * F, F);
        }

        /// <summary>
        /// Draws the scene with the lounge's origin (PC 0,0) at (ox, oy) and theScale display px per PC px (the
        /// PC lounge Draw's translation; the cutscene draws it at 86,15 of its screen).
        /// </summary>
        public void DrawAt(Graphics g, float ox, float oy, float theScale)
        {
            if (mSheet == null || Math.Abs(mSheet.mScale - theScale) > 0.001)
            {
                // The art is resampled to the drawing scale (again when the area changes size).
                mSheet?.Dispose();
                mSheet = PCSheet.Load("images/pc/wow_mainmenu", theScale);
                if (mSheet == null)
                {
                    return;
                }
            }
            float F = (float)mSheet.mScale;
            Image anImage = mSheet.mImage;
            void Cel(string theCel, float x, float y)
            {
                g.DrawImage(anImage, (int)(ox + x * F), (int)(oy + y * F), mSheet[theCel]);
            }

            Cel("PORTAL_ART", 0x127, 0xdb);
            // The sun through the portal: its glow turns and pulses.
            Transform aTransform = new Transform();
            aTransform.Reset();
            aTransform.Translate(1 * F, 4 * F);
            aTransform.RotateRad((float)((mCount % 800) / -800.0 * Math.PI * 2));
            float aPulse = (float)(Math.Sin((mCount % 200) / 200.0 * Math.PI * 2) * 0.1 + 0.75);
            aTransform.Scale(aPulse, aPulse);
            g.DrawImageTransform(anImage, aTransform, mSheet["SMALL_SUN_GLOW"], ox + 388 * F, oy + 300 * F);
            Cel("SMALL_SUN_HEAD", 0x170, 0x118);
            Cel("BACKGROUND", SCENE_X, SCENE_Y);
            SetVortexImages();
            mVortexClouds.Draw(g, ox + VORTEX_X * F, oy + VORTEX_Y * F, F);
            mVortexFlames.Draw(g, ox + VORTEX_X * F, oy + VORTEX_Y * F, F);
            mVortexTrail.Draw(g, ox + VORTEX_X * F, oy + VORTEX_Y * F, F);
            DrawTorchFire(g, ox, oy, F);
            Cel("TORCH_POT", 0x12, 0x1a8);
            if (mSplork)
            {
                DrawBanner(g, ox, oy, F);
                // Splork (vf71 shown): his lightning and flames, then him. Not ported: his quest marker,
                // which only the main menu (not the cutscene) shows.
                mSplorkLightning.Draw(g, ox + (SPLORK_X + 107) * F, oy + 176 * F, F);
                mSplorkFlames.Draw(g, ox + (SPLORK_X + 107) * F, oy + 176 * F, F);
                Cel("SPLORK", (int)SPLORK_X, 70);
                if (mShowQuestMarker)
                {
                    Cel("QUEST_EMARK_BLUE", 0x9c, 0x50);
                }
                DrawBannerSparkles(g, ox, oy, F);
                Cel("TREE_MASK", 8, 0x1b8);
                return;
            }
            mOutline.Draw(g, ox, oy, F);
            Cel("BJORN", BJORN_X, BJORN_Y);
            if (mShowQuestMarker)
            {
                // The quest marker (0x43dba0): a "!" for an adventure not yet started, a "?" for one under way.
                if (mQuestStarted)
                {
                    Cel("QUEST_QMARK_GRAY", 200, 0x32);
                }
                else
                {
                    Cel("QUEST_EMARK_YELLOW", 0xd2, 0x32);
                }
            }
            foreach (ShapeParticleEmitter aRune in mRunes)
            {
                aRune.Draw(g, ox, oy, F);
            }
            Cel("BJORN_HAIR", BJORN_X + 178, BJORN_Y + 106);
            Cel("TREE_MASK", 8, 0x1b8);
        }

        /// <summary>Points the emitters at their cels in the (possibly reloaded) sheet.</summary>
        private void SetVortexImages()
        {
            SetImage(mVortexFlames, "FLAME_PARTICLE");
            SetImage(mVortexClouds, "CLOUD_PART");
            SetImage(mVortexTrail, "FLAME_PARTICLE");
            SetImage(mOutline, "BJORN_PART1");
            SetImage(mSplorkFlames, "FLAME_PARTICLE");
            SetImage(mSplorkLightning, "LIGHTNING");
            SetImage(mBurnFlames, "FLAME_STRIP");
            foreach (ShapeParticleEmitter aRune in mRunes)
            {
                SetImage(aRune, "FLAME_01");
            }
        }

        private void SetImage(ParticleEmitter theEmitter, string theCel)
        {
            theEmitter.mImage = mSheet.mImage;
            theEmitter.mImageRect = mSheet[theCel];
        }

        /// <summary>
        /// The fire over the torch pot (in the lounge's Draw): seven flame images over (36..68, 400..447),
        /// each flickering in size and brightness with the frame count, drawn additively.
        /// </summary>
        private void DrawTorchFire(Graphics g, float ox, float oy, float F)
        {
            string[] aFlames = { "FLAME_01", "FLAME_02", "FLAME_03", "FLAME_04" };
            int c = mCount;
            int a3c = 0x29a;
            int a40 = 0xf2;
            int a44 = 0x359f;
            int a48 = 0x972;
            int a4c = 0x18b7;
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            g.SetColorizeImages(true);
            for (int k = 0; a48 < 0xa4b; k++)
            {
                int anAlpha = ((c / 7) * 0x529 + a48 + (c / 6) * 0x305) % 0x35 + 0xc9;
                g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                string aCel = aFlames[k & 3];
                int r1 = a4c % 0x21;
                int r2 = a44 % 0x2f;
                float sy = (((c / 3 * 3 + k) / 18 * (a40 + 0x50)) % 11 + 100) / 75.0f;
                float sx = (((c / 2 + a3c) * 7 / 21 * a40) % 7 + 29) / 26.0f;
                TRect aSrc = mSheet[aCel];
                float aW = aSrc.mWidth / F;
                float aH = aSrc.mHeight / F;
                float x = r1 + 0x24 - aW * sx * 0.5f;
                float y = r2 + 0x190 - aH * sy * 0.8f;
                g.DrawImage(mSheet.mImage, new TRect((int)(ox + x * F), (int)(oy + y * F), (int)(aW * sx * F), (int)(aH * sy * F)), aSrc);
                a4c += 0xab;
                a44 += 0x173;
                a3c += 0x12;
                a40 += 3;
                a48 += 0x1f;
            }
            g.SetColorizeImages(false);
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
            g.SetColor(new Color(255, 255, 255));
        }
    }
}
