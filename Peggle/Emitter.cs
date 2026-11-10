using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>Level particle emitter (PhysObj class 9, shape 6).</summary>
    public class Emitter : PhysObj
    {
        public float mX;                                          // 0x9c
        public float mY;                                          // 0xa0
        public float mVelX;                                       // 0xa4
        public float mVelY;                                       // 0xa8
        public float mEmitAccum;                                  // 0xac
        public int mWidth;                                        // 0xb0
        public int mHeight;                                       // 0xb4
        public bool mEllipse;                                     // 0xb8
        public int mLastParticle;                                 // 0xbc
        public int mMaxParticles;                                 // 0xc0
        public int mPeakParticles;                                // 0xc4
        public string mName = "";                                 // 0xc8
        public string mParticleImageName = "";                    // 0xcc
        public Image mParticleImage;                              // 0xd0
        public CurvedVal mEmitRate = new CurvedVal();             // 0xd4
        public CurvedVal mEmitArea = new CurvedVal();             // 0x148
        public CurvedVal mParticleAlpha = new CurvedVal();        // 0x1bc
        public CurvedVal mParticleScale = new CurvedVal();        // 0x230
        public CurvedVal mParticleScaleDelta = new CurvedVal();   // 0x2a4
        public CurvedVal mParticleRed = new CurvedVal();          // 0x318
        public CurvedVal mParticleGreen = new CurvedVal();        // 0x38c
        public CurvedVal mParticleBlue = new CurvedVal();         // 0x400
        public CurvedVal mParticleRotation = new CurvedVal();     // 0x474
        public CurvedVal mParticleSpin = new CurvedVal();         // 0x4e8
        public CurvedVal mParticleVelX = new CurvedVal();         // 0x55c
        public CurvedVal mParticleVelY = new CurvedVal();         // 0x5d0
        public CurvedVal mParticleOffsetX = new CurvedVal();      // 0x644
        public CurvedVal mParticleOffsetY = new CurvedVal();      // 0x6b8
        public float mRandRotation;                               // 0x72c
        public float mEmitRateVar;                                // 0x730
        public int mPreloadFrames;                                // 0x734
        public int mPreloadLeft;                                  // 0x738
        public bool mStopped;                                     // 0x73c
        public bool mStartActive;                                 // 0x73d
        public bool mEmitRateRepeat;                              // 0x73e
        public bool mLocalCoords;                                 // 0x73f
        public bool mAdditive;                                    // 0x740
        public bool mOrientToMotion;                              // 0x741
        public bool mUseArea;                                     // 0x742
        public float mWobbleRate;                                 // 0x744
        public float mScaleVar;                                   // 0x748
        public float mFadeOutTime;                                // 0x74c
        public float mFadeInTime;                                 // 0x750
        public float mLife;                                       // 0x754
        public float mEmitScale2D;                                // 0x758
        public float mEmitScale3D;                                // 0x75c
        public float mLaunchSpeed;                                // 0x760
        public float mLaunchSpeedVar;                             // 0x764
        public float mLaunchAccel;                                // 0x768
        public float mLaunchAngle;                                // 0x76c
        public float mLaunchAngleVar;                             // 0x770
        public float mVelXVar;                                    // 0x774
        public float mVelYVar;                                    // 0x778
        public float mAccelX;                                     // 0x77c
        public float mAccelY;                                     // 0x780
        public int mUpdateCnt;                                    // 0x784
        public List<GameParticle> mParticles = new List<GameParticle>(); // 0x788

        public Emitter()
        {
            mShapeType = SHAPE_EMITTER;
            mEllipse = false;
            mX = 0;
            mY = 0;
            mVelX = 0;
            mVelY = 0;
            mUpdateCnt = 0;
            mPeakParticles = 0;
            mParticleImageName = "images/game/feverglowstar";
            mEmitRate.SetConstant(0.10000000149011612);
            mEmitRateVar = 0;
            mParticleScale.SetConstant(1.0);
            mScaleVar = 0;
            mParticleScaleDelta.SetConstant(0.0);
            mParticleAlpha.SetConstant(1.0);
            mFadeInTime = 0.1f;
            mFadeOutTime = 0.5f;
            mLife = 0.75f;
            mEmitArea.SetConstant(1.0);
            mParticleRed.SetConstant(1.0);
            mParticleGreen.SetConstant(1.0);
            mParticleBlue.SetConstant(1.0);
            mParticleRotation.SetConstant(0.0);
            mRandRotation = 0;
            mParticleSpin.SetConstant(0.0);
            mPreloadFrames = 100;
            mPreloadLeft = mPreloadFrames;
            mStopped = false;
            mStartActive = true;
            mAccelY = 0;
            mLocalCoords = false;
            mAdditive = true;
            mOrientToMotion = false;
            mEmitRateRepeat = true;
            mEmitRate.SetMode(CurvedVal.MODE_REPEAT);
            foreach (CurvedVal aCurve in GetCurves())
            {
                aCurve.mKeepDataStr = true;
            }
            mUseArea = true;
            mWobbleRate = 0;
            mEmitScale2D = 0;
            mEmitScale3D = 1.0f;
            mLaunchSpeed = 0;
            mLaunchSpeedVar = 0;
            mLaunchAccel = 0;
            mLaunchAngle = 0;
            mLaunchAngleVar = 0;
            mParticleOffsetX.SetConstant(0.0);
            mParticleOffsetY.SetConstant(0.0);
            mParticleVelX.SetConstant(0.0);
            mVelXVar = 0;
            mParticleVelY.SetConstant(0.0);
            mVelYVar = 0;
            mAccelX = 0;
            mEmitAccum = 0;
            mLastParticle = -1;
            mParticleImage = null;
            mMaxParticles = 0;
            SetMaxParticles(100);
            SetSize(150, 150);
            SetPos(300.0f, 200.0f);
        }

        private IEnumerable<CurvedVal> GetCurves()
        {
            yield return mEmitRate;
            yield return mEmitArea;
            yield return mParticleAlpha;
            yield return mParticleScale;
            yield return mParticleRed;
            yield return mParticleGreen;
            yield return mParticleBlue;
            yield return mParticleRotation;
            yield return mParticleSpin;
            yield return mParticleVelX;
            yield return mParticleVelY;
            yield return mParticleOffsetX;
            yield return mParticleOffsetY;
            yield return mParticleScaleDelta;
        }

        public void SetMaxParticles(int theMax)
        {
            if (mMaxParticles == theMax)
            {
                return;
            }
            if (mMaxParticles < theMax)
            {
                for (int i = mMaxParticles; i < theMax; i++)
                {
                    mParticles.Add(new GameParticle(this));
                }
            }
            else
            {
                mParticles.RemoveRange(theMax, mParticles.Count - theMax);
            }
            mMaxParticles = theMax;
            mLastParticle = Math.Min(mLastParticle, mMaxParticles - 1);
        }

        private void SyncCurve(DataSync theSync, CurvedVal theCurve)
        {
            bool isLinear = false;
            if (theSync.GetReader() == null)
            {
                isLinear = theCurve.IsLinear();
            }
            theSync.SyncBool(ref isLinear);
            if (!isLinear)
            {
                theSync.SyncString(ref theCurve.mDataStr);
                if (theSync.GetReader() != null)
                {
                    theCurve.SetCurve(theCurve.mDataStr);
                }
            }
            else
            {
                float aValue = 0;
                if (theSync.GetReader() == null)
                {
                    aValue = (float)theCurve.GetOutVal();
                    theSync.SyncFloat(ref aValue);
                }
                else
                {
                    theSync.SyncFloat(ref aValue);
                    theCurve.SetConstant(aValue);
                }
            }
        }

        public override void SyncState(DataSync theSync)
        {
            base.SyncState(theSync);
            int aVersion = 2;
            if (theSync.GetReader() != null)
            {
                mParticleImage = null;
                mLastParticle = -1;
            }
            theSync.SyncLong(ref aVersion);

            bool hasMover = mMover != null;
            bool hasPos = !hasMover;
            bool hasVel = true;
            if (mVelX == 0.0f)
            {
                hasVel = mVelY != 0.0f;
            }
            bool hasScale = true;
            if (mParticleScale.GetOutVal() == 1.0 && mParticleScale.IsLinear() &&
                mParticleScaleDelta.GetOutVal() == 0.0 && mParticleScaleDelta.IsLinear())
            {
                hasScale = mScaleVar != 0.0f;
            }
            bool hasAlpha = true;
            if (mParticleAlpha.GetOutVal() == 1.0)
            {
                hasAlpha = !mParticleAlpha.IsLinear();
            }
            bool hasColor = true;
            if (mParticleRed.GetOutVal() == 1.0 && mParticleRed.IsLinear() &&
                mParticleGreen.GetOutVal() == 1.0 && mParticleGreen.IsLinear() &&
                mParticleBlue.GetOutVal() == 1.0)
            {
                hasColor = !mParticleBlue.IsLinear();
            }
            bool hasVelCurve = true;
            if (mParticleVelX.GetOutVal() == 0.0 && mParticleVelX.IsLinear() && mVelXVar == 0.0f &&
                mParticleVelY.GetOutVal() == 0.0 && mParticleVelY.IsLinear() && mVelYVar == 0.0f &&
                mAccelX == 0.0f)
            {
                hasVelCurve = mAccelY != 0.0f;
            }
            bool hasLaunch = true;
            if (mLaunchSpeed == 0.0f && mLaunchSpeedVar == 0.0f && mLaunchAccel == 0.0f && mLaunchAngle == 0.0f)
            {
                hasLaunch = mLaunchAngleVar != 0.0f;
            }
            bool hasRot = true;
            if (mParticleRotation.IsLinear() && mParticleRotation.GetOutVal() == 0.0 &&
                mParticleSpin.IsLinear() && mParticleSpin.GetOutVal() == 0.0)
            {
                hasRot = mRandRotation != 0.0f;
            }
            bool hasOffset = true;
            if (mParticleOffsetX.IsLinear() && mParticleOffsetX.GetOutVal() == 0.0 && mParticleOffsetY.IsLinear())
            {
                hasOffset = mParticleOffsetY.GetOutVal() != 0.0;
            }

            theSync.SyncBoolBit(ref mEllipse);
            theSync.SyncBoolBit(ref mLocalCoords);
            theSync.SyncBoolBit(ref mAdditive);
            theSync.SyncBoolBit(ref mOrientToMotion);
            theSync.SyncBoolBit(ref mUseArea);
            theSync.SyncBoolBit(ref hasPos);
            theSync.SyncBoolBit(ref hasVel);
            theSync.SyncBoolBit(ref hasScale);
            theSync.SyncBoolBit(ref hasColor);
            theSync.SyncBoolBit(ref hasAlpha);
            theSync.SyncBoolBit(ref hasVelCurve);
            theSync.SyncBoolBit(ref hasLaunch);
            theSync.SyncBoolBit(ref hasRot);
            theSync.SyncBoolBit(ref hasOffset);
            theSync.SyncBoolBit(ref mStartActive);
            theSync.EndBit();
            theSync.SyncString(ref mParticleImageName);
            theSync.SyncLong(ref mWidth);
            theSync.SyncLong(ref mHeight);

            if (aVersion > 1)
            {
                theSync.SyncLong(ref mPreloadFrames);
                theSync.SyncFloat(ref mEmitScale3D);
                theSync.SyncString(ref mName);
                theSync.SyncBool(ref mEmitRateRepeat);
                if (!hasOffset)
                {
                    mParticleOffsetX.SetConstant(0.0);
                    mParticleOffsetY.SetConstant(0.0);
                }
                else
                {
                    SyncCurve(theSync, mParticleOffsetX);
                    SyncCurve(theSync, mParticleOffsetY);
                }
            }

            if (!hasPos)
            {
                if (theSync.GetReader() != null && hasMover)
                {
                    mX = mMover.mX;
                    mY = mMover.mY;
                }
            }
            else
            {
                theSync.SyncFloat(ref mX);
                theSync.SyncFloat(ref mY);
            }

            theSync.SyncString(ref mParticleImageName);
            theSync.SyncFloat(ref mEmitRateVar);
            theSync.SyncFloat(ref mEmitScale2D);
            theSync.SyncFloat(ref mWobbleRate);
            int aMaxParticles = mMaxParticles;
            theSync.SyncLong(ref aMaxParticles);
            if (theSync.GetReader() != null)
            {
                SetMaxParticles(aMaxParticles);
            }
            theSync.SyncFloat(ref mFadeOutTime);
            theSync.SyncFloat(ref mFadeInTime);
            theSync.SyncFloat(ref mLife);
            SyncCurve(theSync, mEmitRate);
            SyncCurve(theSync, mEmitArea);

            if (!hasRot)
            {
                mParticleRotation.SetConstant(0.0);
                mParticleSpin.SetConstant(0.0);
                mRandRotation = 0;
            }
            else
            {
                SyncCurve(theSync, mParticleRotation);
                SyncCurve(theSync, mParticleSpin);
                theSync.SyncFloat(ref mRandRotation);
            }

            if (!hasScale)
            {
                mParticleScale.SetConstant(1.0);
                mParticleScaleDelta.SetConstant(0.0);
                mScaleVar = 0;
            }
            else
            {
                SyncCurve(theSync, mParticleScale);
                SyncCurve(theSync, mParticleScaleDelta);
                theSync.SyncFloat(ref mScaleVar);
            }

            if (!hasColor)
            {
                mParticleRed.SetConstant(1.0);
                mParticleGreen.SetConstant(1.0);
                mParticleBlue.SetConstant(1.0);
            }
            else
            {
                SyncCurve(theSync, mParticleRed);
                SyncCurve(theSync, mParticleGreen);
                SyncCurve(theSync, mParticleBlue);
            }

            if (!hasAlpha)
            {
                mParticleAlpha.SetConstant(1.0);
            }
            else
            {
                SyncCurve(theSync, mParticleAlpha);
            }

            if (!hasVelCurve)
            {
                mParticleVelX.SetConstant(0.0);
                mVelXVar = 0;
                mParticleVelY.SetConstant(0.0);
                mVelYVar = 0;
                mAccelX = 0;
                mAccelY = 0;
            }
            else
            {
                SyncCurve(theSync, mParticleVelX);
                SyncCurve(theSync, mParticleVelY);
                theSync.SyncFloat(ref mVelXVar);
                theSync.SyncFloat(ref mVelYVar);
                theSync.SyncFloat(ref mAccelX);
                theSync.SyncFloat(ref mAccelY);
            }

            if (!hasLaunch)
            {
                mLaunchSpeed = 0;
                mLaunchSpeedVar = 0;
                mLaunchAccel = 0;
                mLaunchAngle = 0;
                mLaunchAngleVar = 0;
            }
            else
            {
                theSync.SyncFloat(ref mLaunchSpeed);
                theSync.SyncFloat(ref mLaunchSpeedVar);
                theSync.SyncFloat(ref mLaunchAccel);
                theSync.SyncFloat(ref mLaunchAngle);
                theSync.SyncFloat(ref mLaunchAngleVar);
            }

            if (!hasVel)
            {
                mVelY = 0;
                mVelX = 0;
            }
            else
            {
                theSync.SyncFloat(ref mVelX);
                theSync.SyncFloat(ref mVelY);
            }

            SetSize(mWidth, mHeight);
            if (theSync.GetReader() != null)
            {
                if (aVersion < 2)
                {
                    mStartActive = true;
                }
                mPreloadLeft = mPreloadFrames;
                mStopped = !mStartActive;
                mEmitRate.SetMode(mEmitRateRepeat ? 1 : 0);
            }
        }

        public override void Translate(float theDX, float theDY)
        {
            mX += theDX;
            mY += theDY;
            mMinX += theDX;
            mMaxX += theDX;
            mMinY += theDY;
            mMaxY += theDY;
        }

        public override void SetPos(float theX, float theY)
        {
            Translate(theX - mX, theY - mY);
        }

        public void SetSize(int theWidth, int theHeight)
        {
            mWidth = theWidth;
            mHeight = theHeight;
            mMinX = mX - mWidth / 2.0f;
            mMaxX = mX + mWidth / 2.0f;
            mMinY = mY - mHeight / 2.0f;
            mMaxY = mY + mHeight / 2.0f;
        }

        public override void SetVelocity(float theVX, float theVY)
        {
            mVelX = theVX;
            mVelY = theVY;
        }

        private float GetEmitScale()
        {
            return GlobalStaticVars.gSexyAppBase.Is3DAccelerated() ? mEmitScale3D : mEmitScale2D;
        }

        public override void Update()
        {
            base.Update();
            if (!mStopped && mPreloadLeft > 0)
            {
                int aCount = (int)(mPreloadLeft * GetEmitScale());
                for (int i = 0; i < aCount; i++)
                {
                    UpdateSingle();
                }
                mPreloadLeft = 0;
            }
            UpdateSingle();
        }

        private static double RandFraction()
        {
            return SexyUtil.Rand() / 2147483647.0;
        }

        private void UpdateSingle()
        {
            int aCount = (int)mEmitAccum;
            if (mStopped)
            {
                UpdateParticles();
                return;
            }

            double aRate = mEmitRate.GetOutVal();
            double aRand = SexyUtil.Rand();
            float aRateVar = mEmitRateVar;
            double anAreaMult = mEllipse ? 1.33 : 1.0;
            mEmitRate.IncInVal();
            float aScale = GetEmitScale();
            double anAmount = (aRate + aRand / 2147483647.0 * aRateVar) * anAreaMult * aScale;
            mEmitAccum = (float)(mEmitAccum + anAmount);

            while (aCount < (int)mEmitAccum)
            {
                GameParticle aParticle = CreateParticle();
                if (aParticle == null)
                {
                    break;
                }
                if (!mUseArea)
                {
                    if (!mLocalCoords)
                    {
                        aParticle.mX = GetXPos();
                        aParticle.mY = GetYPos();
                    }
                    else
                    {
                        aParticle.mX = 0;
                        aParticle.mY = 0;
                    }
                }
                else if (!mEllipse)
                {
                    float aSpread = 1.0f;
                    if (!mEmitArea.IsLinear() || mEmitArea.mOutMin != 1.0)
                    {
                        aSpread = (float)mEmitArea.GetOutVal(RandFraction());
                    }
                    aParticle.mX = (float)(mWidth / 2) + aSpread * (float)(SexyUtil.Rand() % mWidth - mWidth / 2);
                    aParticle.mY = (float)(mHeight / 2) + aSpread * (float)(SexyUtil.Rand() % mHeight - mHeight / 2);
                    if (!mLocalCoords)
                    {
                        aParticle.mX += GetXPos() - (float)(mWidth / 2);
                        aParticle.mY += GetYPos() - (float)(mHeight / 2);
                    }
                    else
                    {
                        aParticle.mX -= (float)(mWidth / 2);
                        aParticle.mY -= (float)(mHeight / 2);
                    }
                }
                else
                {
                    double aRadius;
                    if (!mEmitArea.IsLinear() || mEmitArea.mOutMin != 1.0)
                    {
                        double aMin = mEmitArea.mOutMin;
                        double aMax = mEmitArea.mOutMax;
                        double aVal = mEmitArea.GetOutVal(RandFraction());
                        aRadius = (aMin + (aMax - aMin) * aVal) * mWidth - (double)(mWidth / 2.0f);
                    }
                    else
                    {
                        aRadius = RandFraction() * mWidth - (double)(mWidth / 2.0f);
                    }
                    float aR = (float)aRadius;
                    float anAngle = (float)(RandFraction() * 3.1415927410125732 * 2.0);
                    aParticle.mX = (float)(aR * Math.Cos(anAngle) * mWidth / mHeight);
                    aParticle.mY = (float)(aR * Math.Sin(anAngle) * mHeight / mWidth);
                    if (!mLocalCoords)
                    {
                        aParticle.mX += GetXPos();
                        aParticle.mY += GetYPos();
                    }
                }
                aCount++;
            }
            UpdateParticles();
            mUpdateCnt++;
        }

        private GameParticle CreateParticle()
        {
            if (mLastParticle + 1 >= mParticles.Count)
            {
                return null;
            }
            if (mParticleImage == null)
            {
                ImageMgr anImageMgr = GetImageMgr();
                // Port addition: a custom level pack's own particle image (CustomLevelPacks), before the
                // name loses its folder.
                if (CustomLevelPacks.GetActivePackImage(mParticleImageName) != null)
                {
                    mParticleImage = anImageMgr.GetImage(mParticleImageName, out mImageCached, true);
                }
            }
            if (mParticleImage == null)
            {
                ImageMgr anImageMgr = GetImageMgr();
                mParticleImageName = mParticleImageName.Substring(mParticleImageName.LastIndexOfAny(new[] { '/', '\\' }) + 1);
                if (mParticleImageName.IndexOf('/') == -1 && mParticleImageName.IndexOf('\\') == -1)
                {
                    mParticleImage = anImageMgr.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "emitters/" + mParticleImageName, out mImageCached, true);
                }
                else
                {
                    string aBaseName = mParticleImageName.Substring(mParticleImageName.LastIndexOfAny(new[] { '/', '\\' }) + 1).ToUpperInvariant();
                    if (string.Equals(aBaseName, "12-3DRAGON", StringComparison.OrdinalIgnoreCase))
                    {
                        aBaseName = "12_3DRAGON";
                    }
                    ResourceId anId = Res.GetIdByStringId("IMAGE_EMITTERS_" + aBaseName);
                    if (anId == ResourceId.RESOURCE_ID_MAX)
                    {
                        mParticleImage = anImageMgr.GetImage(mParticleImageName, out mImageCached, true);
                    }
                    else
                    {
                        mParticleImage = Res.GetImageById(anId);
                    }
                }
            }
            if (mParticleImage == null)
            {
                return null;
            }

            GameParticle aParticle = mParticles[mLastParticle + 1];
            mPeakParticles = Math.Max(mLastParticle + 1, mPeakParticles);
            mLastParticle++;
            aParticle.mOwner = this;
            aParticle.mFadeOutTime = (int)(mFadeOutTime * 100.0f);
            aParticle.mFadeInTime = (int)(mFadeInTime * 100.0f);
            aParticle.mLife = (int)(mLife * 100.0f);
            aParticle.mOrientToMotion = mOrientToMotion;
            aParticle.mUpdateCnt = 0;
            aParticle.mScale = (float)(RandFraction() * mScaleVar);
            aParticle.mRotation = (float)(RandFraction() * mRandRotation / 180.0 * 3.1415927410125732);
            aParticle.mWobbleRate = mWobbleRate;
            if (aParticle.mWobbleRate != 0.0f)
            {
                aParticle.mWobbleRadius = (mWidth + mHeight) / 4;
                aParticle.mWobblePhase = (float)RandFraction();
            }
            aParticle.mVelX = (float)(RandFraction() * mVelXVar);
            aParticle.mVelY = (float)(RandFraction() * mVelYVar);
            aParticle.mAccelX = mAccelX;
            aParticle.mAccelY = mAccelY;
            if (mLaunchSpeed != 0.0f || mLaunchSpeedVar != 0.0f || mLaunchAccel != 0.0f)
            {
                float aSpeed = (float)(mLaunchSpeed + RandFraction() * mLaunchSpeedVar);
                double aBaseAngle = mLaunchAngle / 180.0 * 3.1415927410125732;
                float anAngle = (float)(aBaseAngle + RandFraction() * mLaunchAngleVar / 180.0 * 3.1415927410125732);
                aParticle.mVelX = (float)(aParticle.mVelX + aSpeed * Math.Cos(anAngle));
                aParticle.mVelY = (float)(aParticle.mVelY + aSpeed * Math.Sin(anAngle));
                aParticle.mAccelX = (float)(aParticle.mAccelX + mLaunchAccel * Math.Cos(anAngle));
                aParticle.mAccelY = (float)(aParticle.mAccelY + mLaunchAccel * Math.Sin(anAngle));
            }
            aParticle.mImage = mParticleImage;
            return aParticle;
        }

        private void UpdateParticles()
        {
            int i = 0;
            while (i <= mLastParticle)
            {
                if (!mParticles[i].Update())
                {
                    GameParticle aDead = mParticles[i];
                    mParticles[i] = mParticles[mLastParticle];
                    mParticles[mLastParticle] = aDead;
                    mLastParticle--;
                }
                else
                {
                    i++;
                }
            }
        }

        public override void Draw(Graphics g)
        {
            if (mOutlineMode == 1)
            {
                g.SetColor(SexyUtil.MakeColor(0xffffff));
                DrawCircle(g, (int)GetXPos(), (int)GetYPos(), 10, 10);
            }
            if (GlobalStaticVars.gSexyAppBase.Is3DAccelerated() || mEmitScale2D != 0.0f)
            {
                // The original draws the editor's outline and particle counts whenever the outline colour
                // (mColor, default white) is set. Its own levels all store 0 for emitters; a PC level
                // that doesn't (the Nights bonus level Peggle 3D) showed "N particles / (N peak)" during
                // play. Port: only while editing a level, where the overlay belongs.
                if (Board.mLevelEditing)
                {
                    DrawEmitter(g, (int)mValue40, (int)mColor);
                }
                if (mAdditive)
                {
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                }
                DrawParticles(g);
                g.SetColorizeImages(false);
                g.SetColor(SexyUtil.MakeColor(-1));
                if (mAdditive)
                {
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                }
            }
        }

        /// <summary>Global DrawEllipse(g, x, y, rx, ry, thickness, segments) as an outline.</summary>
        private static void DrawCircle(Graphics g, int theX, int theY, int theRadiusX, int theRadiusY)
        {
            const int aSegments = 0x20;
            for (int i = 0; i < aSegments; i++)
            {
                double a0 = i * Math.PI * 2.0 / aSegments;
                double a1 = (i + 1) * Math.PI * 2.0 / aSegments;
                g.DrawLine(theX + (int)(Math.Cos(a0) * theRadiusX), theY + (int)(Math.Sin(a0) * theRadiusY),
                           theX + (int)(Math.Cos(a1) * theRadiusX), theY + (int)(Math.Sin(a1) * theRadiusY));
            }
        }

        public void DrawEmitter(Graphics g, int theValue, int theColor)
        {
            if (theColor == 0)
            {
                return;
            }
            int aX = (int)mX;
            int aY = (int)mY;
            int aWidth = mWidth;
            int aHeight = mHeight;
            if (!mEllipse)
            {
                int aLeft = aX - aWidth / 2;
                int aTop = aY - aHeight / 2;
                g.SetColor(SexyUtil.MakeColor(theColor));
                for (int i = 0; i < 3; i++)
                {
                    g.DrawRect(aLeft + i, aTop + i, aWidth - i * 2, aHeight - i * 2);
                }
                g.SetColor(SexyUtil.MakeColor(0));
                g.DrawRect(aLeft - 1, aTop - 1, aWidth + 2, aHeight + 2);
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(theColor));
                DrawCircle(g, aX, aY, (int)(aWidth / 2.0f), (int)(aHeight / 2.0f));
            }
            g.SetFont(SexyUtil.GetFontById(0x12));
            g.SetColor(SexyUtil.MakeColor(-1));
            g.DrawString(string.Format("{0} particles", mLastParticle + 1), aX + aWidth / 2 + 0xf, aY);
            g.DrawString(string.Format("({0} peak)", mPeakParticles), aX + aWidth / 2 + 0xf, aY + 0xf);
        }

        private void DrawParticles(Graphics g)
        {
            for (int i = 0; i <= mLastParticle; i++)
            {
                if (!mLocalCoords)
                {
                    mParticles[i].Draw(g, 0, 0);
                }
                else
                {
                    mParticles[i].Draw(g, (int)GetXPos(), (int)GetYPos());
                }
            }
        }

        public override void DrawLight(Graphics g)
        {
            base.DrawLight(g);
        }

        public void Trigger()
        {
            mStopped = mStartActive;
            if (!mStopped)
            {
                mEmitRate.SetInVal(0.0);
            }
        }

        public override int GetClass()
        {
            return CLASS_EMITTER;
        }

        public override float GetXPos()
        {
            return mX;
        }

        public override float GetYPos()
        {
            return mY;
        }
    }
}
