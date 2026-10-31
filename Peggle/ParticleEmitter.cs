using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>A particle of a ParticleEmitter (Peggle WoW, 0x80 bytes; defaults from FUN_0043fec0).</summary>
    public class EmitterParticle
    {
        public float mX;                                          // 0x00

        public float mY;                                          // 0x04

        public float mVelX;                                       // 0x08

        public float mVelY;                                       // 0x0c

        public float mStartX;                                     // 0x10

        public float mStartY;                                     // 0x14

        public float mAccelX;                                     // 0x18

        public float mAccelY;                                     // 0x1c

        public int mAge;                                          // 0x20

        public int mLife;                                         // 0x24

        /// <summary>The image cel, or -1 for the whole image.</summary>
        public int mCel = -1;                                     // 0x28

        public int mCelOffset;                                    // 0x2c

        public float mCelSpeed;                                   // 0x30

        /// <summary>The wobble's starting angle, in turns.</summary>
        public float mPhase;                                      // 0x34

        public float mAlpha = 1.0f;                               // 0x38

        public float mAngle;                                      // 0x3c

        public float mAngleVel;                                   // 0x40

        public float mScaleX = 1.0f;                              // 0x44

        public float mScaleY = 1.0f;                              // 0x48

        public float mScaleVelX;                                  // 0x4c

        public float mScaleVelY;                                  // 0x50

        /// <summary>Colours: mColor1, fading to mColor2 by mColorTime1, then to mColor3 by mColorTime2 (-1 = none).</summary>
        public int mColor1 = 0xffffff;                            // 0x54

        public int mColor2;                                       // 0x58

        public int mColorTime1 = -1;                              // 0x5c

        public int mColor3;                                       // 0x60

        public int mColorTime2 = -1;                              // 0x64

        public bool mAdditive;                                    // 0x68

        /// <summary>Updates before the acceleration starts (-1: at once).</summary>
        public int mDelay = -1;                                   // 0x6c

        public ParticleEmitter mEmitter;                          // 0x70

        public EmitterParticle Clone()
        {
            return (EmitterParticle)MemberwiseClone();
        }

        /// <summary>FUN_0043ff30: one update; false once the particle is dead.</summary>
        public bool Update()
        {
            mAge++;
            mAngle += mAngleVel;
            // (Particles following a path, which also animate their cels, aren't used by the port.)
            mX += mVelX;
            mY += mVelY;
            if (mDelay > 0)
            {
                mDelay--;
            }
            else
            {
                mVelX += mAccelX;
                mVelY += mAccelY;
            }
            if (mScaleVelX != 0)
            {
                mScaleX += mScaleVelX;
                if (mScaleX <= 0)
                {
                    mAge = mLife;
                }
            }
            if (mScaleVelY != 0)
            {
                mScaleY += mScaleVelY;
                if (mScaleY <= 0)
                {
                    mAge = mLife;
                    return false;
                }
            }
            return mAge < mLife;
        }
    }

    /// <summary>
    /// Port of Peggle WoW's Sexy::ParticleEmitter (0x10c bytes; ctor 0x43dcf0, Update vf2 0x43dfc0,
    /// Spawn vf3 0x43e290, Draw vf1 0x43e3f0): particles spawned from a template, moved by optional
    /// curves, faded in/out, coloured and drawn as an image (cel) or a rect. Positions are in the
    /// emitter's own space (the PC's 800x600 for the WoW screens); Draw maps them to the screen.
    /// </summary>
    public class ParticleEmitter
    {
        public float mTransX;                                     // 0x04

        public float mTransY;                                     // 0x08

        /// <summary>Extra fading copies drawn behind each particle (echoes).</summary>
        public int mEchoCount;                                    // 0x0c

        public float mEchoScale;                                  // 0x10

        public int mEchoStep = 1;                                 // 0x14

        public float mRateRand;                                   // 0x18

        public int mFadeInTime = -1;                              // 0x1c

        public int mFadeOutTime = -1;                             // 0x20

        public bool mOrderedRemove;                               // 0x24

        /// <summary>The wobble counts down the particle's life instead of up.</summary>
        public bool mWobbleReverse;                               // 0x25

        /// <summary>
        /// Wobble: each particle is drawn on a circle around its position, mWobbleRate turns per life
        /// (starting at its mPhase), of radius mWobbleAmp (times mWobbleCurve) shrinking to 0 over its life.
        /// </summary>
        public float mWobbleRate;                                 // 0x28

        public int mWobbleAmp;                                    // 0x2c

        public float mX;                                          // 0x30

        public float mY;                                          // 0x34

        public bool mRotateByVel;                                 // 0x38

        public Image mImage;                                      // 0x3c

        /// <summary>Port: the image's area in a sheet, and its cel grid.</summary>
        public TRect mImageRect;

        public int mImageCols = 1;

        public int mImageRows = 1;

        public CurvedVal mXCurve;                                 // 0x40

        public CurvedVal mYCurve;                                 // 0x44

        public CurvedVal mYAddCurve;                              // 0x48

        public CurvedVal mXAddCurve;                              // 0x4c

        public CurvedVal mAlphaCurve;                             // 0x50

        public CurvedVal mScaleCurve;                             // 0x54

        public CurvedVal mWobbleCurve;                            // 0x58

        public float mRate;                                       // 0x5c

        public float mSpawned;                                    // 0x60

        public float mSpawnAccum;                                 // 0x64

        /// <summary>Called with each new particle (the PC's IPartInitializer at 0x68).</summary>
        public Action<ParticleEmitter, EmitterParticle> mInitPart;  // 0x68

        public EmitterParticle mTemplate = new EmitterParticle { mLife = 100 }; // 0x6c

        public List<EmitterParticle> mParticles = new List<EmitterParticle>(); // 0xf0

        private static readonly Random sRand = new Random();

        /// <summary>vf3: a new particle (a copy of the template, or a default one) at the emitter's position.</summary>
        public EmitterParticle Spawn(bool theFromTemplate)
        {
            EmitterParticle p = theFromTemplate ? mTemplate.Clone() : new EmitterParticle();
            p.mAge = 0;
            p.mEmitter = this;
            p.mX = mX;
            p.mY = mY;
            mParticles.Add(p);
            mInitPart?.Invoke(this, p);
            return p;
        }

        /// <summary>FUN_0043dfa0: runs 100 updates, so the emitter starts full.</summary>
        public void Prewarm()
        {
            for (int i = 0; i < 100; i++)
            {
                Update(true);
            }
        }

        /// <summary>vf2: spawns at mRate per update (when theSpawn), moves the particles and drops dead ones.</summary>
        public virtual void Update(bool theSpawn)
        {
            mSpawnAccum += mRate;
            if (mRateRand != 0)
            {
                mSpawnAccum += sRand.Next(10000) / 10000.0f * mRateRand;
            }
            while (mSpawned < mSpawnAccum)
            {
                if (theSpawn)
                {
                    Spawn(true);
                }
                mSpawned++;
            }
            for (int i = 0; i < mParticles.Count;)
            {
                EmitterParticle p = mParticles[i];
                double t = p.mAge / 100.0;
                if (mXCurve != null)
                {
                    p.mX = (float)(p.mStartX + mX + mXCurve.GetOutVal(t));
                }
                if (mYCurve != null)
                {
                    p.mY = (float)(p.mStartY + mY + mYCurve.GetOutVal(t));
                }
                if (mXAddCurve != null)
                {
                    p.mX += (float)mXAddCurve.GetOutVal(t);
                }
                if (mYAddCurve != null)
                {
                    p.mY += (float)mYAddCurve.GetOutVal(t);
                }
                if (p.Update())
                {
                    i++;
                }
                else if (mOrderedRemove)
                {
                    mParticles.RemoveAt(i);
                }
                else
                {
                    mParticles[i] = mParticles[mParticles.Count - 1];
                    mParticles.RemoveAt(mParticles.Count - 1);
                }
            }
        }

        private static int LerpColor(int theFrom, int theTo, float t)
        {
            int r = (int)(((theFrom >> 16) & 0xff) + (((theTo >> 16) & 0xff) - ((theFrom >> 16) & 0xff)) * t);
            int g = (int)(((theFrom >> 8) & 0xff) + (((theTo >> 8) & 0xff) - ((theFrom >> 8) & 0xff)) * t);
            int b = (int)((theFrom & 0xff) + ((theTo & 0xff) - (theFrom & 0xff)) * t);
            return (r << 16) | (g << 8) | b;
        }

        private static int ColorAt(EmitterParticle p, int theAge)
        {
            if (p.mColorTime2 >= 1 && theAge < p.mColorTime2 && p.mColorTime1 >= 1 && theAge > p.mColorTime1)
            {
                return LerpColor(p.mColor2, p.mColor3, (theAge - p.mColorTime1) / (float)(p.mColorTime2 - p.mColorTime1));
            }
            if (p.mColorTime1 > 0)
            {
                return p.mColorTime1 <= theAge ? p.mColor2 : LerpColor(p.mColor1, p.mColor2, theAge / (float)p.mColorTime1);
            }
            if (p.mColorTime2 >= 1 && theAge >= p.mColorTime2)
            {
                return p.mColor3;
            }
            return p.mColor1;
        }

        /// <summary>
        /// vf1: draws the particles, each at (theOffX + x * theScale, theOffY + y * theScale). The image is
        /// already at the screen's scale, so only positions are scaled.
        /// </summary>
        public void Draw(Graphics g, float theOffX, float theOffY, float theScale)
        {
            Transform aTransform = new Transform();
            foreach (EmitterParticle p in mParticles)
            {
                for (int anEcho = 0; anEcho <= mEchoCount; anEcho++)
                {
                    int anAge = p.mAge - mEchoStep * anEcho;
                    if (anAge < 0)
                    {
                        continue;
                    }
                    float anAlpha = p.mAlpha;
                    if (mAlphaCurve != null)
                    {
                        anAlpha = (float)(mAlphaCurve.GetOutVal(anAge / 100.0) * p.mAlpha);
                    }
                    anAlpha *= 1.0f - anEcho / (float)(mEchoCount + 1);
                    if (mFadeInTime > 0 && anAge < mFadeInTime)
                    {
                        anAlpha = anAge * anAlpha / mFadeInTime;
                    }
                    else if (mFadeOutTime >= 0 && anAge >= mFadeOutTime)
                    {
                        anAlpha *= 1.0f - (anAge - mFadeOutTime) / (float)(p.mLife - mFadeOutTime);
                    }
                    if (anAlpha == 0)
                    {
                        continue;
                    }
                    int aColor = ColorAt(p, anAge);
                    bool aColorize = anAlpha != 1.0f || aColor != 0xffffff;
                    if (aColorize)
                    {
                        g.SetColorizeImages(true);
                        g.SetColor(SexyUtil.MakeColor(aColor, (int)(anAlpha * 255)));
                    }
                    g.SetDrawMode(p.mAdditive ? Graphics.DrawMode.DRAWMODE_ADDITIVE : Graphics.DrawMode.DRAWMODE_NORMAL);
                    float aPosX = p.mX;
                    float aPosY = p.mY;
                    if (mWobbleRate != 0 && p.mLife > 0)
                    {
                        float t = mWobbleReverse ? p.mLife - anAge : anAge;
                        float aFrac = t - ((int)t / p.mLife) * p.mLife;
                        double aTurn = aFrac / p.mLife * mWobbleRate;
                        float anAmp = mWobbleAmp;
                        if (mWobbleCurve != null)
                        {
                            anAmp = (float)(mWobbleCurve.GetOutVal(anAge / 100.0) * anAmp);
                        }
                        anAmp *= (p.mLife - aFrac) / p.mLife;
                        double anAngle2 = (p.mPhase + aTurn) * Math.PI * 2;
                        aPosX += (float)(Math.Cos(anAngle2) * anAmp) + p.mVelX;
                        aPosY += (float)(Math.Sin(anAngle2) * anAmp) + p.mVelY;
                    }
                    float x = theOffX + aPosX * theScale;
                    float y = theOffY + aPosY * theScale;
                    if (mImage == null)
                    {
                        g.SetColor(SexyUtil.MakeColor(aColor, (int)(anAlpha * 255)));
                        g.FillRect((int)(x - p.mScaleX * theScale / 2), (int)(y - p.mScaleY * theScale / 2),
                            Math.Max(1, (int)(p.mScaleX * theScale)), Math.Max(1, (int)(p.mScaleY * theScale)));
                    }
                    else
                    {
                        float sx = p.mScaleX;
                        float sy = p.mScaleY;
                        if (anEcho != 0 && mEchoScale != 0)
                        {
                            sx += mEchoScale;
                            sy += mEchoScale;
                        }
                        if (mScaleCurve != null)
                        {
                            float c = (float)mScaleCurve.GetOutVal(anAge / 100.0);
                            sx *= c;
                            sy *= c;
                        }
                        aTransform.Reset();
                        if (mTransX != 0 || mTransY != 0)
                        {
                            aTransform.Translate(mTransX * theScale, mTransY * theScale);
                        }
                        aTransform.Scale(sx, sy);
                        float anAngle = mRotateByVel ? (float)Math.Atan2(p.mVelY, p.mVelX) : p.mAngle;
                        if (anAngle != 0)
                        {
                            aTransform.RotateRad(anAngle);
                        }
                        TRect aSrc = mImageRect;
                        if (p.mCel >= 0)
                        {
                            int aCelW = mImageRect.mWidth / mImageCols;
                            int aCelH = mImageRect.mHeight / mImageRows;
                            aSrc = new TRect(mImageRect.mX + (p.mCel % mImageCols) * aCelW, mImageRect.mY + (p.mCel / mImageCols) * aCelH, aCelW, aCelH);
                        }
                        g.DrawImageTransformF(mImage, aTransform, aSrc, x, y);
                    }
                    if (aColorize)
                    {
                        g.SetColorizeImages(false);
                        g.SetColor(new Color(255, 255, 255));
                    }
                }
            }
            g.SetColor(new Color(255, 255, 255));
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
        }
    }

    /// <summary>
    /// Port of Peggle WoW's dragon fire (an unnamed object in LoadingScreen at +0x9dc; init FUN_00440270,
    /// update FUN_004404d0, draw FUN_00440c40): fire particles poured from mRect at mIntensity per update,
    /// each with a dark copy behind it, shedding sparks; FIREBALLLOOP plays while the intensity is up.
    /// </summary>
    public class FlameEffect
    {
        public TRect mRect = new TRect(0, 0, 1, 1);               // 0x04

        public int mFrame;                                        // 0x14

        public int mSpawned;                                      // 0x18

        public float mSpawnAccum;                                 // 0x1c

        public float mIntensity = 0.5f;                           // 0x20

        private SoundInstance mSound;                             // 0x24

        private bool mSoundFading;                                // 0x28

        private float mSoundVolume;                               // 0x2c

        private int mSparksSpawned;                               // 0x30

        private float mSparkAccum;                                // 0x34

        /// <summary>The fire, its dark underlay and the sparks.</summary>
        public ParticleEmitter mFire = new ParticleEmitter();      // 0x38

        public ParticleEmitter mSmoke = new ParticleEmitter();     // 0x144

        public ParticleEmitter mSparks = new ParticleEmitter();    // 0x250

        private static readonly CurvedVal sSizeCurve = new CurvedVal("b+0,1,0,1,#### D/2}4   @~###    -,?D, 5####");

        private static readonly Random sRand = new Random();

        public FlameEffect(Image theImage, TRect theFlameStrip, TRect theSpark)
        {
            mFire.mImage = theImage;
            mFire.mImageRect = theFlameStrip;
            mFire.mImageCols = 2;
            mFire.mImageRows = 2;
            mFire.mFadeOutTime = 50;
            mFire.mYAddCurve = new CurvedVal("b+0.5,4,0,1,~###     C~### 5####   K####");
            mFire.mScaleCurve = new CurvedVal("b;1,1.5,0,1,####       Y=[Z[tU3zYcj)ScXbjUZ7`###");
            mFire.mTemplate.mAlpha = 0.7f;
            mFire.mTemplate.mColor1 = 0xff910a;
            mFire.mTemplate.mAdditive = true;
            mSmoke.mTemplate = mFire.mTemplate.Clone();
            mSmoke.mTemplate.mAdditive = false;
            mSmoke.mImage = theImage;
            mSmoke.mImageRect = theFlameStrip;
            mSmoke.mImageCols = 2;
            mSmoke.mImageRows = 2;
            mSmoke.mFadeOutTime = 55;
            mSmoke.mYAddCurve = new CurvedVal("b+0.5,4,0,1,~###     C~### 5####   K####");
            mSmoke.mScaleCurve = new CurvedVal("b;1,1.5,0,1,####       Y=[Z[tU3zYcj)ScXbjUZ7`###");
            mSparks.mTemplate.mLife = 50;
            mSparks.mTemplate.mScaleX = 0.25f;
            mSparks.mTemplate.mScaleY = 0.25f;
            mSparks.mTemplate.mScaleVelX = -0.0005f;
            mSparks.mTemplate.mScaleVelY = -0.0005f;
            mSparks.mTemplate.mColor1 = 0xff0000;
            mSparks.mTemplate.mColor2 = 0xdd0000;
            mSparks.mTemplate.mColorTime1 = 180;
            mSparks.mImage = theImage;
            mSparks.mImageRect = theSpark;
            mSparks.mFadeOutTime = 25;
        }

        private static float Rand01()
        {
            return sRand.Next(10000) / 10000.0f;
        }

        public void Update(ThunderballApp theApp)
        {
            UpdateSound(theApp);
            mSpawnAccum += mIntensity;
            mFire.Update(true);
            mSmoke.Update(true);
            mSparks.Update(true);
            while (mSpawned < mSpawnAccum)
            {
                EmitterParticle p = mFire.Spawn(true);
                p.mX = mRect.mX + Rand01() * (mRect.mX + mRect.mWidth - mRect.mX);
                p.mY = mRect.mY + Rand01() * (mRect.mY + mRect.mHeight - mRect.mY);
                p.mVelX = Rand01() * 1.28f - 0.8f;
                p.mVelY = Rand01() * 2 + 0.5f;
                float aSize = (float)(sSizeCurve.GetOutVal(sRand.Next(0x7fff) / 32767.0) * 0.4 + 0.4);
                p.mScaleX = aSize;
                p.mScaleY = aSize;
                float aShrink = (Rand01() * 0.035f + 0.015f) * aSize;
                p.mScaleVelX = aShrink;
                p.mScaleVelY = aShrink;
                if (Rand01() <= 0.15f)
                {
                    p.mColor1 = 0xdd5a0a;
                    p.mAdditive = false;
                }
                p.mCelSpeed = 0.1f;
                p.mCelOffset = p.mCel = sRand.Next(3);
                float aDir = p.mVelX > 0 ? 1.0f : -1.0f;
                p.mAccelX = Rand01() * 0.01f * aDir;
                p.mAccelY = Rand01() * 0.01f + 0.01f;
                // Its dark copy, a little bigger and up-left, drawn behind.
                EmitterParticle q = mSmoke.Spawn(true);
                int anIndex = mSmoke.mParticles.Count - 1;
                q = p.Clone();
                q.mEmitter = mSmoke;
                mSmoke.mParticles[anIndex] = q;
                q.mScaleX = q.mScaleY = q.mScaleX * 1.1f;
                q.mAdditive = false;
                q.mColor1 = 0;
                q.mColorTime1 = -1;
                q.mColorTime2 = -1;
                q.mX -= 1;
                q.mY -= 1;
                mSpawned++;
            }
            foreach (EmitterParticle p in mFire.mParticles)
            {
                mSparkAccum += p.mScaleX * 0.01f * p.mScaleY;
                if (mSparksSpawned < (int)mSparkAccum)
                {
                    mSparksSpawned++;
                    EmitterParticle s = mSparks.Spawn(true);
                    s.mX = (Rand01() * 30 - 15) * p.mScaleX + p.mX;
                    s.mY = (Rand01() * 30 - 15) * p.mScaleY + p.mY;
                    s.mVelX = Rand01() * 1.8f - 0.9f;
                    s.mVelY = Rand01() * 1.8f - 0.9f;
                }
            }
            mFrame++;
        }

        /// <summary>FIREBALLLOOP fades in while the fire burns and out (then stops) when it doesn't.</summary>
        private void UpdateSound(ThunderballApp theApp)
        {
            if (mIntensity > 0)
            {
                mSoundFading = false;
                if (mSound == null && Res.SOUND_FIREBALLLOOP >= 0)
                {
                    mSound = theApp.mSoundManager.GetSoundInstance((uint)Res.SOUND_FIREBALLLOOP);
                    if (mSound != null)
                    {
                        if (mSoundVolume == 0)
                        {
                            mSoundVolume = (float)mSound.GetVolume();
                        }
                        mSound.Play(true, true);
                    }
                }
            }
            else if (mSound != null)
            {
                mSoundFading = true;
            }
            if (mSound == null)
            {
                return;
            }
            if (mSoundFading)
            {
                double aVolume = Math.Max(0, mSound.GetVolume() - 0.05);
                mSound.SetVolume(aVolume);
                if (mSound.GetVolume() == 0)
                {
                    mSoundFading = false;
                    mSound.Release();
                    mSound = null;
                }
            }
            else
            {
                mSound.SetVolume(Math.Min(mSoundVolume, mSound.GetVolume() + 0.1));
            }
        }

        /// <summary>Stops the loop (the screen is going away).</summary>
        public void StopSound()
        {
            if (mSound != null)
            {
                mSound.Stop();
                mSound.Release();
                mSound = null;
            }
        }

        public void Draw(Graphics g, float theOffX, float theOffY, float theScale)
        {
            mSmoke.Draw(g, theOffX, theOffY, theScale);
            mSparks.Draw(g, theOffX, theOffY, theScale);
            mFire.Draw(g, theOffX, theOffY, theScale);
        }
    }
}
