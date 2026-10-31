using System;

namespace Sexy
{
    /// <summary>A single particle owned by an Emitter; all curves are read from the emitter by life fraction.</summary>
    public class GameParticle
    {
        public float mX;                                          // 0x00
        public float mY;                                          // 0x04
        public float mAccelX;                                     // 0x08
        public float mAccelY;                                     // 0x0c
        public Image mImage;                                      // 0x10
        public int mUpdateCnt;                                    // 0x14
        public int mLife;                                         // 0x18
        public Emitter mEmitter;                                  // 0x1c
        public float mVelX;                                       // 0x20
        public float mVelY;                                       // 0x24
        public int mFadeOutTime;                                  // 0x28
        public int mFadeInTime;                                   // 0x2c
        public float mRotation;                                   // 0x30
        public float mScale;                                      // 0x34
        public float mOffsetX;                                    // 0x38
        public float mOffsetY;                                    // 0x3c
        public bool mOrientToMotion;                              // 0x40
        public float mWobbleRate;                                 // 0x44
        public int mWobbleRadius;                                 // 0x48
        public float mWobblePhase;                                // 0x4c
        public Emitter mOwner;                                    // 0x64

        public GameParticle(Emitter theEmitter)
        {
            mEmitter = theEmitter;
            mImage = null;
            mX = 0;
            mY = 0;
            mAccelX = 0;
            mAccelY = 0;
            mScale = 0;
            mLife = 0;
            mOffsetX = 0;
            mOffsetY = 0;
            mUpdateCnt = 0;
            mRotation = 0;
            mFadeOutTime = -1;
            mOrientToMotion = false;
            mOwner = null;
        }

        public bool Update()
        {
            mUpdateCnt++;
            float t = (float)((double)mUpdateCnt / (double)mLife);
            mRotation = (float)(mRotation + mEmitter.mParticleSpin.GetOutVal(t) / 180.0 * 3.1415927410125732);
            mX = (float)(mX + mEmitter.mParticleVelX.GetOutVal(t) + mVelX);
            mY = (float)(mY + mEmitter.mParticleVelY.GetOutVal(t) + mVelY);
            mVelX += mAccelX;
            mVelY += mAccelY;
            float aScaleDelta = (float)mEmitter.mParticleScaleDelta.GetOutVal(t);
            if (aScaleDelta != 0.0f)
            {
                mScale += aScaleDelta;
                if (mScale <= 0.0f)
                {
                    mUpdateCnt = mLife;
                }
            }
            return mUpdateCnt < mLife;
        }

        public void Draw(Graphics g, int theOffsetX, int theOffsetY)
        {
            float t = (float)((double)mUpdateCnt / (double)mLife);
            float anAlpha = (float)mEmitter.mParticleAlpha.GetOutVal(t);
            if (mFadeInTime > 0 && mUpdateCnt < mFadeInTime)
            {
                anAlpha = (float)((double)((float)mUpdateCnt / (float)mFadeInTime) * mEmitter.mParticleAlpha.GetOutVal(t));
            }
            else if (mFadeOutTime > -1 && mFadeOutTime <= mUpdateCnt)
            {
                anAlpha = (float)(mEmitter.mParticleAlpha.GetOutVal(t) * (double)(1.0f - (float)(mUpdateCnt - mFadeOutTime) / (float)(mLife - mFadeOutTime)));
            }

            double aRed = mEmitter.mParticleRed.GetOutVal(t);
            double aGreen = mEmitter.mParticleGreen.GetOutVal(t);
            double aBlue = mEmitter.mParticleBlue.GetOutVal(t);
            int aColor = (int)(aBlue * 255.0) + (int)(aGreen * 255.0) * 0x100 + (int)(aRed * 255.0) * 0x10000;
            bool colorize = anAlpha != 1.0f || aColor != 0xffffff;
            if (colorize)
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(aColor, (int)(Math.Min(Math.Max(anAlpha, 0.0f), 1.0f) * 255.0f)));
            }

            float aX = (float)(mEmitter.mParticleOffsetX.GetOutVal(t) + theOffsetX + mX);
            float aY = (float)(mEmitter.mParticleOffsetY.GetOutVal(t) + theOffsetY + mY);
            float aWobbleX = 0.0f;
            float aWobbleY = 0.0f;
            if (mWobbleRate != 0.0f)
            {
                int aCount = 1;
                for (int i = 1; i <= aCount; i++)
                {
                    double aTime = mUpdateCnt + i * 0.800000011920929;
                    aTime -= mLife * ((int)aTime / mLife);
                    double aPhase = aTime / mLife * mWobbleRate;
                    float aRadius = (float)(mLife - aTime) / (float)mLife * mWobbleRadius;
                    aWobbleX = (float)(aWobbleX + aRadius * Math.Cos((aPhase + mWobblePhase) * 3.1415927410125732 * 2.0));
                    aWobbleY = (float)(aWobbleY + aRadius * Math.Sin((aPhase + mWobblePhase) * 3.1415927410125732 * 2.0));
                    aX += aWobbleX;
                    aY += aWobbleY;
                }
            }

            float aRot = (float)(mEmitter.mParticleRotation.GetOutVal(t) / 180.0 * 3.1415927410125732 + mRotation);
            float aDisplayH = SexyUtil.gPhysicalDisplayHeight;
            if (mImage == null)
            {
                g.FillRect((int)(aX * aDisplayH / 600.0f), (int)(aY * aDisplayH / 600.0f), 3, 3);
            }
            else
            {
                float aScale = (float)(mEmitter.mParticleScale.GetOutVal(t) + mScale);
                if (aRot != 0.0f || mOrientToMotion)
                {
                    if (mOrientToMotion)
                    {
                        float aDX = (float)(mEmitter.mParticleVelX.GetOutVal(t) + mVelX + aWobbleX);
                        float aDY = (float)(mEmitter.mParticleVelY.GetOutVal(t) + mVelY + aWobbleY);
                        if (aDX != 0.0f)
                        {
                            aRot = (float)(aRot + Math.Atan(aDX / aDY) + (aDY < 0.0f ? 3.1415927f : 0.0f));
                        }
                    }
                    SexyTransform2D aTrans = new SexyTransform2D();
                    if (mOffsetX != 0.0f || mOffsetY != 0.0f)
                    {
                        aTrans.Translate(mOffsetX, mOffsetY);
                    }
                    aTrans.Scale(aScale, aScale);
                    aTrans.RotateRad(aRot);
                    int aWidth = mImage.GetWidth();
                    int aHeight = mImage.GetHeight();
#if PEGGLE_IPHONE
                    // The source rect drops the last row and column (no edge bleed).
                    g.DrawImageMatrix(mImage, aTrans, new TRect(0, 0, mImage.GetWidth() - 1, mImage.GetHeight() - 1),
                        (float)(aX * aDisplayH / 600.0 - aWidth / 2.0f),
                        (float)(aY * aDisplayH / 600.0 - aHeight / 2.0f));
#else
                    g.DrawImageMatrix(mImage, aTrans,
                        (float)(aX * aDisplayH / 600.0 - aWidth / 2.0f),
                        (float)(aY * aDisplayH / 600.0 - aHeight / 2.0f));
#endif
                }
                else
                {
                    int aWidth = (int)(mImage.GetWidth() * aScale);
                    int aHeight = (int)(mImage.GetHeight() * aScale);
#if PEGGLE_IPHONE
                    g.DrawImage(mImage, new TRect((int)((double)(aX * aDisplayH) / 600.0 - aWidth / 2),
                        (int)((double)(aY * aDisplayH) / 600.0 - aHeight / 2), aWidth, aHeight),
                        new TRect(0, 0, mImage.GetWidth() - 1, mImage.GetHeight() - 1));
#else
                    g.DrawImage(mImage,
                        (int)((double)(aX * aDisplayH) / 600.0 - aWidth / 2),
                        (int)((double)(aY * aDisplayH) / 600.0 - aHeight / 2),
                        aWidth, aHeight);
#endif
                }
            }

            if (colorize)
            {
                g.SetColorizeImages(false);
                g.SetColor(SexyUtil.MakeColor(-1));
            }
        }
    }
}
