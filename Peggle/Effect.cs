using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// One board effect (Sexy::Effect): a small record (position, velocity, timers and a few
    /// type-specific values) whose drawing and updating is chosen by mType.
    /// </summary>
    public class Effect : ISyncable
    {
        public const int TYPE_EXPLOSION = 1;
        public const int TYPE_SPARKLE = 2;
        public const int TYPE_SPARKLE_FEVER = 4;
        public const int TYPE_FIREBALL_FIRE = 5;
        public const int TYPE_STREAK = 6;
        public const int TYPE_RIBBON = 7;
        public const int TYPE_COIN_FLIP = 8;
        public const int TYPE_BUBBLE = 9;
        public const int TYPE_SPACE_CRACKS = 10;
        public const int TYPE_SPACE_HALO = 11;
        public const int TYPE_SPACE_BURST = 12;
        public const int TYPE_SPACE_BUBBLE = 13;
        public const int TYPE_SPACE_STAR = 14;
        public const int TYPE_FLOWER_SPARKLE = 15;
        public const int TYPE_FEVER_RAY = 16;
        public const int TYPE_FEVER_STAR = 17;
        public const int TYPE_FEVER_SPARK = 18;
        public const int TYPE_FEVER_TRAIL = 19;
        public const int TYPE_FEVER_CRACKLE = 20;
        public const int TYPE_FEVER_SCORE_TEXT = 21;
        public const int TYPE_LIGHTNING_CRACKLE = 22;
        public const int TYPE_LIGHTNING_BOLT = 23;
        public const int TYPE_PYRAMID = 24;
        public const int TYPE_PYRAMID2 = 25;
        public const int TYPE_FEVER_RAINBOW = 26;
        public const int TYPE_CATCHER_GLOW = 28;
        public const int TYPE_CATCHER_MOTE = 29;
        public const int TYPE_EXTREME_FEVER = 31;
        public const int TYPE_HAT = 32;
        public const int TYPE_JIMMY = 33;
        public const int TYPE_BALL_HOLE = 34;
        public const int TYPE_MASTER_BADGE = 35;
        public const int TYPE_SPOOKY_POOF = 36;
        public const int TYPE_SPOOKY_GHOST = 37;
        public const int TYPE_SPOOKY_EYES = 38;
        public const int TYPE_SPOOKY_SPARKLE = 39;
        public const int TYPE_RAINBOW_SPARK = 40;
        public const int TYPE_EMBER = 41;
        public const int TYPE_SMOKE = 42;
        public const int TYPE_FLECK = 43;
        public const int TYPE_FLAME_TRAIL = 44;
        public const int TYPE_FIREBALL_EMBER = 46;
        public const int TYPE_LEVEL_BUMPER = 47;
        public const int TYPE_STYLE_SPIN = 48;
        public const int TYPE_FEVER_NUKE = 49;
        public const int TYPE_NUKE_STREAK = 50;

        public int mPriority;                                     // 0x04

        public int mType;                                         // 0x08

        public int mUpdateCnt;                                    // 0x0c

        /// <summary>Lifetime in updates; 0 = until removed.</summary>
        public int mDuration;                                     // 0x10

        public float mX;                                          // 0x14

        public float mY;                                          // 0x18

        public float mVelX;                                       // 0x1c

        public float mVelY;                                       // 0x20

        /// <summary>Added to mVelY each update (capped at 10) when positive.</summary>
        public float mGravity;                                    // 0x24

        public float mAngle;                                      // 0x28

        public float mAngleVel;                                   // 0x2c

        public int mValue;                                        // 0x30

        public int mColor;                                        // 0x34

        public int mFlags;                                        // 0x38

        public int mAlpha;                                        // 0x3c

        /// <summary>Updates at full speed during slow motion.</summary>
        public bool mNoSlowMo;                                    // 0x40

        /// <summary>Port addition: whose corner Jimmy this is (TYPE_JIMMY): JIMMY_PEGGLE, JIMMY_EXTREME or JIMMY_WOW.</summary>
        public int mJimmyGame;

        public const int JIMMY_PEGGLE = 0;

        /// <summary>Peggle Extreme's levels: the Heavy (jimmy_extreme) with his own sound, Jimmy's phrases.</summary>
        public const int JIMMY_EXTREME = 1;

        /// <summary>Peggle WoW's levels: Jimmy, with WoW's phrases (two lines where they have a line break).</summary>
        public const int JIMMY_WOW = 2;

        /// <summary>Port addition: the Heavy (images/pc/Heavy, PC art at the display's scale), loaded once.</summary>
        private static Image sExtremeJimmy;

        /// <summary>Port addition: the Heavy's SOUND_GOODSHOT (sounds/goodshot_extreme.wav), loaded once; -2 before then.</summary>
        private static int sExtremeJimmySound = -2;

        /// <summary>The corner image: the Heavy in Extreme's levels (when it loads), else Jimmy.</summary>
        private Image GetJimmyImage()
        {
            if (mJimmyGame == JIMMY_EXTREME)
            {
                sExtremeJimmy ??= SexyUtil.LoadPCImage("images/pc/Heavy");
                if (sExtremeJimmy != null)
                {
                    return sExtremeJimmy;
                }
            }
            return Res.IMAGE_JIMMY;
        }

        public PhysObj mObj1;                                     // 0x44

        public PhysObj mObj2;                                     // 0x48

        public PhysObj mObj3;                                     // 0x4c

        public Effect()
        {
            Reset();
        }

        public Effect(Effect theEffect)
        {
            CopyFrom(theEffect);
        }

        public void Reset()
        {
            mType = 0;
            mUpdateCnt = 0;
            mDuration = 0;
            mY = 0;
            mX = 0;
            mVelY = 0;
            mVelX = 0;
            mValue = 0;
            mGravity = 0;
            mColor = 0;
            mAngle = 0;
            mAngleVel = 0;
            mPriority = 0;
            mFlags = 0;
            mAlpha = 0;
            mNoSlowMo = false;
        }

        public void CopyFrom(Effect theEffect)
        {
            mType = theEffect.mType;
            mUpdateCnt = theEffect.mUpdateCnt;
            mDuration = theEffect.mDuration;
            mX = theEffect.mX;
            mY = theEffect.mY;
            mVelX = theEffect.mVelX;
            mVelY = theEffect.mVelY;
            mGravity = theEffect.mGravity;
            mAngle = theEffect.mAngle;
            mAngleVel = theEffect.mAngleVel;
            mValue = theEffect.mValue;
            mColor = theEffect.mColor;
            mPriority = theEffect.mPriority;
            mFlags = theEffect.mFlags;
            mAlpha = theEffect.mAlpha;
            mNoSlowMo = theEffect.mNoSlowMo;
            mObj1 = theEffect.mObj1;
            mObj2 = theEffect.mObj2;
            mObj3 = theEffect.mObj3;
        }

        public void SetPriorityPrv(int thePriority)
        {
            mPriority = thePriority;
        }

        public int GetPriority()
        {
            return mPriority;
        }

        private static readonly Transform sScratchTrans = new Transform();

        /// <summary>
        /// A reset, shared Transform for draw code that uses one only until its draw call returns; fever
        /// sparks, trails and streaks number in the thousands, and a new Transform each was GC churn.
        /// </summary>
        private static Transform ScratchTransform()
        {
            sScratchTrans.Reset();
            return sScratchTrans;
        }

        private static float BoardToScreen(float theValue)
        {
            return (float)(theValue * SexyUtil.gPhysicalDisplayHeight / 600.0);
        }

        /// <summary>Scales the RGB of the current colour in place (the original edits Graphics' colour fields).</summary>
        private static void ScaleColor(Graphics g, float theScale)
        {
            Color c = g.GetColor();
            g.SetColor(new Color((int)(c.R * theScale), (int)(c.G * theScale), (int)(c.B * theScale), (int)c.A));
        }

        private static PhysObj SyncObj(DataSync theSync, PhysObj theObj)
        {
            return theSync.SyncRefCount(theObj, () => (PhysObj)new Ball(false));
        }

        public void SyncState(DataSync theSync)
        {
            if (theSync.GetReader() != null)
            {
                Reset();
            }
            byte[] aType = BitConverter.GetBytes(mType);
            theSync.SyncBytes(aType, 4);
            mType = BitConverter.ToInt32(aType, 0);
            theSync.SyncLong(ref mUpdateCnt);
            theSync.SyncLong(ref mDuration);
            theSync.SyncFloat(ref mX);
            theSync.SyncFloat(ref mY);
            if (theSync.GetVersion() < 0x11)
            {
                theSync.SyncFloat(ref mVelX);
                theSync.SyncFloat(ref mVelY);
                theSync.SyncShort(ref mValue);
                return;
            }
            bool hasVelX = mVelX != 0.0f;
            bool hasVelY = mVelY != 0.0f;
            bool hasValue = mValue != 0;
            bool hasGravity = mGravity != 0.0f;
            bool hasColor = mColor != 0;
            bool hasAngle = mAngle != 0.0f;
            bool hasAngleVel = mAngleVel != 0.0f;
            bool hasFlags = mFlags != 0;
            bool hasObj1 = mObj1 != null;
            bool hasObj2 = mObj2 != null;
            bool hasObj3 = mObj3 != null;
            bool hasAlpha = mAlpha != 0;
            theSync.SyncBoolBit(ref hasVelX);
            theSync.SyncBoolBit(ref hasVelY);
            theSync.SyncBoolBit(ref hasValue);
            theSync.SyncBoolBit(ref hasGravity);
            theSync.SyncBoolBit(ref hasColor);
            theSync.SyncBoolBit(ref hasAngle);
            theSync.SyncBoolBit(ref hasAngleVel);
            theSync.SyncBoolBit(ref hasFlags);
            theSync.EndBit();
            if (theSync.GetVersion() > 0x2b)
            {
                theSync.SyncBoolBit(ref hasObj1);
                theSync.SyncBoolBit(ref mNoSlowMo);
                theSync.SyncBoolBit(ref hasObj2);
                theSync.SyncBoolBit(ref hasAlpha);
                theSync.SyncBoolBit(ref hasObj3);
                theSync.EndBit();
            }
            if (hasVelX)
            {
                theSync.SyncFloat(ref mVelX);
            }
            if (hasVelY)
            {
                theSync.SyncFloat(ref mVelY);
            }
            if (hasValue)
            {
                theSync.SyncLong(ref mValue);
            }
            if (hasGravity)
            {
                theSync.SyncFloat(ref mGravity);
            }
            if (hasColor)
            {
                theSync.SyncLong(ref mColor);
            }
            if (hasAngle)
            {
                theSync.SyncFloat(ref mAngle);
            }
            if (hasAngleVel)
            {
                theSync.SyncFloat(ref mAngleVel);
            }
            if (hasFlags)
            {
                theSync.SyncByte(ref mFlags);
            }
            if (hasObj1)
            {
                mObj1 = SyncObj(theSync, mObj1);
            }
            if (hasObj2)
            {
                mObj2 = SyncObj(theSync, mObj2);
            }
            if (hasObj3)
            {
                mObj3 = SyncObj(theSync, mObj3);
            }
            if (hasAlpha)
            {
                theSync.SyncByte(ref mAlpha);
            }
            if (theSync.GetVersion() > 0x22)
            {
                theSync.SyncSByte(ref mPriority);
            }
        }

        public void DrawExplosion(Graphics g)
        {
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0xff0000, 0x96 - mUpdateCnt * 0x96 / 100));
            SexyUtil.DrawCircle(g, mX, mY, mValue, Res.IMAGE_CIRCLE, null, 0);
            g.SetColorizeImages(false);
        }

        public void DrawSparkle(Graphics g)
        {
            Image anImage = Res.IMAGE_SPARKLE;
            if (mType == TYPE_SPARKLE_FEVER)
            {
                return;
            }
            float aX = mX - anImage.GetCelWidth() / 2;
            float aY = mY - anImage.GetCelHeight() / 2;
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            int aColor = 0xffffff;
            if (mValue == 1)
            {
                aColor = GlobalStaticVars.gSexyAppBase.HSLToRGB((int)(mUpdateCnt * 5.0f) % 0xff, 0xff, 0x80);
            }
            if (mDuration < mUpdateCnt + 10 || mAlpha != 0)
            {
                g.SetColor(SexyUtil.MakeColor(aColor, mAlpha * SexyUtil.ClampRange(mDuration - mUpdateCnt, 0, 10) / 10));
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(aColor));
            }
            int aCel = (mUpdateCnt / 2 + mFlags) % anImage.mNumCols;
            if (mVelX > 0.0f)
            {
                aCel = anImage.mNumCols - 1 - aCel;
            }
            g.DrawImageF(anImage, BoardToScreen(aX), BoardToScreen(aY), anImage.GetCelRect(aCel));
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        public void DrawFeverCrackle(Graphics g)
        {
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            Image anImage = Res.IMAGE_SPACESTAR;
            g.SetColor(SexyUtil.MakeColor(mColor));
            int aSize = (int)((mDuration - mUpdateCnt) * 0.35f);
            if (mUpdateCnt < mAlpha)
            {
                aSize = 2;
            }
            g.DrawImage(anImage, (int)((double)(mX * SexyUtil.gPhysicalDisplayHeight) / 600.0 - aSize),
                (int)((double)(mY * SexyUtil.gPhysicalDisplayHeight) / 600.0 - aSize), aSize * 2, aSize * 2);
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawLightningCrackle(Graphics g)
        {
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            Image anImage = Res.IMAGE_SPACESTAR;
            g.SetColor(SexyUtil.MakeColor(mColor));
            int aSize = (int)((mDuration - mUpdateCnt) * 0.05f);
            int aH = SexyUtil.gPhysicalDisplayHeight;
            g.DrawImage(anImage, (int)((mX - aSize) * aH / 600.0f), (int)((mY - aSize) * aH / 600.0f),
                (int)((double)(aH * aSize * 2) / 600.0), (int)((double)(aSize * 2 * aH) / 600.0));
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        /// <summary>A bolt from mObj1 to mObj2 (or to the lightning rod), tiled from the front/back strips.</summary>
        public void DrawLightningBolt(Graphics g)
        {
            g.SetDrawMode(1);
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null || mObj1 == null || (mObj2 == null && mAlpha == 0))
            {
                return;
            }
            int aH = SexyUtil.gPhysicalDisplayHeight;
            mX = mObj1.GetXPos() * aH / 600.0f;
            mY = mObj1.GetYPos() * aH / 600.0f;
            float aToX;
            float aToY;
            if (mObj2 == null)
            {
                LightningOrb anOrb = aBoard.mLogicMgr.mLightningOrb;
                if (anOrb == null)
                {
                    return;
                }
                aToX = (float)((double)(aH * anOrb.mX) / 600.0);
                aToY = anOrb.mY * aH;
            }
            else
            {
                aToX = mObj2.GetXPos() * aH / 600.0f;
                aToY = mObj2.GetYPos() * aH;
            }
            aToY /= 600.0f;
            float aDX = mX - aToX;
            float aDY = mY - aToY;
            float aLen = (float)Math.Sqrt(aDX * aDX + aDY * aDY);
            Image anImage = aLen > Res.IMAGE_LIGHTNING_FRONT_SMALL.GetHeight() ? Res.IMAGE_LIGHTNING_FRONT : Res.IMAGE_LIGHTNING_FRONT_SMALL;
            TRect aCelRect = anImage.GetCelRect(mUpdateCnt / 4 % anImage.mNumCols);
            int aSegs = (int)Math.Ceiling(aLen / anImage.GetHeight());
            aDX /= aSegs;
            aDY /= aSegs;
            aLen /= aSegs;
            for (int i = 0; i < aSegs; i++)
            {
                g.SetColorizeImages(true);
                float anAngle = (float)(Math.Atan2(aDX, aDY) + 3.141592025756836);
                if (g.Is3D())
                {
                    Transform aBackTrans = new Transform();
                    aBackTrans.Scale(0.5f, aLen / Res.IMAGE_LIGHTNING_BACK.GetHeight());
                    aBackTrans.RotateRad(anAngle);
                    g.SetColor(SexyUtil.MakeColor(mColor));
                    g.DrawImageTransform(Res.IMAGE_LIGHTNING_BACK, aBackTrans,
                        mX + (-0.5f - i) * aDX, mY + (-0.5f - i) * aDY);
                    g.SetColor(SexyUtil.MakeColor(0xffffff));
                }
                Transform aTrans = new Transform();
                aTrans.Scale(0.5f * aLen / anImage.GetHeight() + 0.5f, aLen / anImage.GetHeight());
                aTrans.RotateRad(anAngle);
                g.SetColorizeImages(false);
                g.DrawImageTransform(anImage, aTrans, aCelRect, mX + (-0.5f - i) * aDX, mY + (-0.5f - i) * aDY);
            }
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.SetColorizeImages(false);
        }

        public void DrawFireballFire(Graphics g)
        {
            if (!g.Is3D())
            {
                return;
            }
            Image anImage = Res.IMAGE_FIREBALLFIRE;
            float aX = (float)((double)(mX * SexyUtil.gPhysicalDisplayHeight) / 600.0 - anImage.GetCelWidth() / 2);
            float aY = (float)((double)(mY * SexyUtil.gPhysicalDisplayHeight) / 600.0 - anImage.GetCelHeight() / 2);
            g.SetColor(new Color(0x14, 0x14, 0x14));
            int aCel = mUpdateCnt / 2;
            if (aCel < anImage.mNumRows * anImage.mNumCols)
            {
                g.DrawImageRotatedF(anImage, aX, aY, SexyUtil.DegToRad(mValue + mUpdateCnt), anImage.GetCelRect(aCel));
            }
        }

        public void DrawStreak(Graphics g)
        {
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            int aColor = (mFlags & 1) == 0 ? 0xff9944 : mColor;
            int anAlpha = 0xff;
            int aFade = (mFlags & 2) == 0 ? 10 : 0x14;
            int aLeft = mDuration - mUpdateCnt;
            if (aLeft < aFade)
            {
                anAlpha = aLeft * 0xff / aFade;
            }
            g.SetColor(SexyUtil.MakeColor(aColor, anAlpha));
            Image anImage = Res.IMAGE_STREAK;
            float anAngle = (float)Math.Atan2(mVelX, mVelY) + 3.1415927f;
            float aSpeed = (float)Math.Sqrt(mVelX * mVelX + mVelY * mVelY);
            Transform aTrans = ScratchTransform();
            if (g.Is3D())
            {
                if ((mFlags & 2) != 0)
                {
                    aTrans.Scale(0.65f, 0.15f + aSpeed * 0.3f);
                }
                else
                {
                    aTrans.Scale(0.65f, 0.3f + aSpeed * 0.5f);
                }
            }
            aTrans.RotateRad(anAngle);
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        /// <summary>The "Level Complete"/"Aced" ribbon: zooms in, holds, then flies off to the corner.</summary>
        public void DrawRibbon(Graphics g)
        {
            Transform aTrans = new Transform();
            int aGrowTime = 0x14;
            int aSettleTime = 0x1e;
#if PEGGLE_IPHONE
            // Half the HD scales throughout.
            float aStartScale = 0.1f;
            float aGrowScale = 1.5f;
            float aX = mX;
            float aY = mY;
            float aScale = mAlpha == 0xfe ? 0.85f : 1.0f;
            float anExitScale = 1.0f;
            float aSmallScale = 0.85f;
#else
            float aStartScale = 0.2f;
            float aGrowScale = 3.0f;
            float aX = mX;
            float aY = mY;
            float aScale = mAlpha == 0xfe ? 1.7f : 2.0f;
            float anExitScale = 2.0f;
            float aSmallScale = 1.7f;
#endif
            int aSmallX = 0xaf;
            int aSmallY = 0x118;
            Image anImage;
            int anExitX;
            bool isAced = mFlags > 0;
            bool shrinkFirst = false;
            if (isAced)
            {
                anImage = Res.IMAGE_DLG_ACEDRIBBON;
                anExitX = -50;
                shrinkFirst = mAlpha == 0xff;
            }
            else
            {
                anImage = Res.IMAGE_BLUERIBBON;
                anExitX = 0x2ad;
            }

            if (mAlpha < 2 || mAlpha > 0xfd)
            {
                if (mUpdateCnt < aGrowTime)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, mUpdateCnt * 0xff / aGrowTime));
                    if (!g.Is3D())
                    {
                        aTrans.Scale(aScale, aScale);
                    }
                    else
                    {
                        float s = aStartScale + aGrowScale * mUpdateCnt / aGrowTime;
                        aTrans.Scale(s, s);
                    }
                }
                else if (mUpdateCnt < aSettleTime)
                {
                    if (!g.Is3D())
                    {
                        aTrans.Scale(aScale, aScale);
                    }
                    else
                    {
                        float s = (aStartScale + aGrowScale) -
                                  ((aStartScale + aGrowScale) - aScale) * (mUpdateCnt - aGrowTime) / (aSettleTime - aGrowTime);
                        aTrans.Scale(s, s);
                    }
                }
                else if (mDuration < 1 || mUpdateCnt <= mDuration - 0x14)
                {
                    aTrans.Scale(aScale, aScale);
                }
                else
                {
                    if (mDuration <= mUpdateCnt)
                    {
                        return;
                    }
                    if (shrinkFirst)
                    {
                        mX = aSmallX;
                        mY = aSmallY;
                        anExitScale = aSmallScale;
                    }
                    float t = (mDuration - mUpdateCnt) / 20.0f;
                    aX = SexyUtil.LinearCombineF(t, mX, anExitX);
                    aY = SexyUtil.LinearCombineF(t, mY, -16.0f);
                    float s = SexyUtil.LinearCombineF(t, anExitScale, 0.1f);
                    aTrans.Scale(s, s);
                }
            }
            else if (!g.Is3D() || mAlpha < mUpdateCnt)
            {
                aX = aSmallX;
                aY = aSmallY;
                aTrans.Scale(aSmallScale, aSmallScale);
            }
            else
            {
                float t = (mAlpha - mUpdateCnt) / (float)mAlpha;
                aX = SexyUtil.LinearCombineF(t, mX, aSmallX);
                aY = SexyUtil.LinearCombineF(t, mY, aSmallY);
                float s = SexyUtil.LinearCombineF(t, aScale, aSmallScale);
                aTrans.Scale(s, s);
            }
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(aX), BoardToScreen(aY));
            g.SetColorizeImages(false);
        }

        public void DrawMasterBadge(Graphics g)
        {
            Transform aTrans = new Transform();
            int aFadeTime = 5;
            int aGrowTime = 0x14;
            int aSettleTime = 0x1e;
#if PEGGLE_IPHONE
            float aStartScale = 0.1f;
            float aGrowScale = 1.5f;
#else
            float aStartScale = 0.2f;
            float aGrowScale = 3.0f;
#endif
            float aX = mX;
            float aY = mY;
            if (mUpdateCnt < aGrowTime)
            {
                if (mUpdateCnt < aFadeTime)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, mUpdateCnt * 0xff / aFadeTime));
                }
                if (g.Is3D())
                {
                    float s = aStartScale + aGrowScale * mUpdateCnt / aGrowTime;
                    aTrans.Scale(s, s);
                }
            }
            else if (mUpdateCnt < aSettleTime)
            {
                if (g.Is3D())
                {
                    float s = (aStartScale + aGrowScale) - ((aStartScale + aGrowScale) - 1.0f) * (mUpdateCnt - aGrowTime) / (aSettleTime - aGrowTime);
                    aTrans.Scale(s, s);
                }
            }
#if !PEGGLE_IPHONE
            else
            {
                aTrans.Scale(2.0f, 2.0f);
            }
#endif
            g.DrawImageTransform(Res.IMAGE_MASTERBADGE, aTrans, BoardToScreen(aX), BoardToScreen(aY));
            g.SetColorizeImages(false);
        }

        public void DrawBubble(Graphics g)
        {
            g.SetDrawMode(1);
            Image anImage = Res.IMAGE_BUBBLES;
            TRect aRect = anImage.GetCelRect(mValue);
            int aJitter = 0;
            if (mUpdateCnt % 3 == 0)
            {
                aJitter = SexyUtil.Rand() % 2;
            }
            int aH = SexyUtil.gPhysicalDisplayHeight;
            g.DrawImage(anImage, (int)((double)(mX * aH) / 600.0 - aRect.mWidth / 2 + aJitter),
                (int)((double)(mY * aH) / 600.0 - aRect.mHeight / 2), aRect);
            g.SetDrawMode(0);
        }

        /// <summary>
        /// The Space Blast shockwave: a burst glow plus 80 triangles of the board image
        /// (Board::mCrackImage) pushed outwards, like the board itself shattering.
        /// </summary>
        public void DrawSpaceCracks(Graphics g)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null || !g.Is3D() || !aBoard.mShowSpaceCracks)
            {
                return;
            }
            float aScale = 543.0f;
#if PEGGLE_IPHONE
            float aU = (mX + -2.0f) / Layout.BACKGROUND_IMAGE_WIDTH;
            float aV = (mY + -12.5f) / Layout.BACKGROUND_IMAGE_HEIGHT;
#else
            float aU = (mX + -2.0f) / 827.0f;
            float aV = (mY + -12.5f) / 696.0f;
#endif
            float aFade = (mDuration - mUpdateCnt) / (mDuration * 0.7f);
            if (aFade > 0.85f)
            {
                aFade = 0.85f;
            }
            g.SetColor(SexyUtil.MakeColor(0x8040ff, (int)(aFade * 255.0f)));
            g.SetColorizeImages(true);
            g.SetDrawMode(1);
            int aH = SexyUtil.gPhysicalDisplayHeight;
            g.DrawImage(Res.IMAGE_SPACEBURST, (int)((mX - 120.0f) * aH / 600.0f), (int)((mY - 120.0f) * aH / 600.0f),
                (int)((double)(aH * 0xf0) / 600.0), (int)((double)(aH * 0xf0) / 600.0));
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
            if (aBoard.mCrackImage == null)
            {
                return;
            }
            g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(aFade * 255.0f)));
            g.SetColorizeImages(true);
            EffectMgr anEffectMgr = aBoard.mEffectMgr;
            for (int i = 0; i < 0x50; i++)
            {
                SexyVector2 aP1 = anEffectMgr.SetCrackPoint(i - 2, 1, mUpdateCnt);
                SexyVector2 aP2 = anEffectMgr.SetCrackPoint(i + 2, 1, mUpdateCnt);
                SexyVector2 aP4 = anEffectMgr.SetCrackPoint(i, 2, mUpdateCnt);
                TriVertex v1 = new TriVertex(aP1.x, aP1.y, aU + aP1.x / 1.19f, aV + aP1.y);
                TriVertex v2 = new TriVertex(aP2.x, aP2.y, aU + aP2.x / 1.19f, aV + aP2.y);
                TriVertex v3 = new TriVertex(0.0f, 0.0f, aU, aV);
                TriVertex v4 = new TriVertex(aP4.x, aP4.y, aU + aP4.x / 1.19f, aV + aP4.y);
                v1.x *= aScale;
                v1.y *= aScale;
                v2.x *= aScale;
                v2.y *= aScale;
                v4.x *= aScale;
                v4.y *= aScale;
                SexyVector2 aC1 = new SexyVector2((v1.x + v2.x + v3.x) / 3.0f, (v1.y + v2.y + v3.y) / 3.0f);
                SexyVector2 aC2 = new SexyVector2((v1.x + v2.x + v4.x) / 3.0f, (v1.y + v2.y + v4.y) / 3.0f);
                v1.x += mX;
                v1.y += mY;
                v2.x += mX;
                v2.y += mY;
                v3.x += mX;
                v3.y += mY;
                v4.x += mX;
                v4.y += mY;
                int aSpread = Math.Min(mUpdateCnt, (mDuration - mUpdateCnt) / 3 + mDuration / 4);
                if (aSpread < 1)
                {
                    aSpread = 1;
                }
                float f = (SexyUtil.Rand() % aSpread + aSpread * 8) / (mDuration * 9.0f);
                v1.x = (v1.x + f * 2.75f * aC1.x + f * aC2.x) * aH / 600.0f;
                v1.y = (v1.y + f * 2.75f * aC1.y + f * aC2.y) * aH / 600.0f;
                v2.x = (v2.x + f * 2.75f * aC1.x + f * aC2.x) * aH / 600.0f;
                v2.y = (v2.y + f * 2.75f * aC1.y + f * aC2.y) * aH / 600.0f;
                v3.x = (v3.x + f * 2.75f * aC1.x + f * 0.5f * aC2.x) * aH / 600.0f;
                v3.y = (v3.y + f * 2.75f * aC1.y + f * 0.5f * aC2.y) * aH / 600.0f;
                g.DrawTriangleTex(aBoard.mCrackImage, v1, v2, v3);
                v4.x = (v4.x + f * 2.75f * aC2.x) * aH / 600.0f;
                v4.y = (v4.y + f * 2.75f * aC2.y) * aH / 600.0f;
                g.DrawTriangleTex(aBoard.mCrackImage, v1, v2, v4);
            }
            g.SetColorizeImages(false);
        }

        public void DrawSpaceHalo(Graphics g)
        {
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            Image anImage = Res.IMAGE_SPACEHALO;
            Transform aTrans = new Transform();
            float aFade = 1.0f;
            if (mDuration - 0x1e < mUpdateCnt)
            {
                aFade = (mDuration - mUpdateCnt) / 30.0f;
            }
            aFade *= 0.8f;
            int aGreen = 0xff;
            if (mUpdateCnt > 10)
            {
                aGreen = 0xff - (mUpdateCnt - 10) * 0xff / 0x28;
                if (aGreen < 0)
                {
                    aGreen = 0;
                }
            }
            g.SetColor(new Color(0xff, aGreen, 0xff));
            ScaleColor(g, aFade);
            float s = mUpdateCnt / 15.0f + 0.5f;
            aTrans.Scale(s, s);
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawSpaceBurst(Graphics g)
        {
            g.SetColorizeImages(true);
            Image anImage = Res.IMAGE_SPACEBURST;
            Transform aTrans = new Transform();
            float aFade = 1.0f;
            if (mDuration - 10 < mUpdateCnt)
            {
                aFade = (mDuration - mUpdateCnt) / 10.0f;
            }
            aFade *= 0.8f;
            int aGreen = 0xff;
            int aBlue = 0;
            if (mUpdateCnt > 0)
            {
                aGreen = Math.Min(mUpdateCnt, 10) * -10 + 0xff;
                if (aGreen < 0)
                {
                    aGreen = 0;
                }
            }
            if (mUpdateCnt > 10)
            {
                aBlue = (mUpdateCnt - 10) * 10;
                aGreen = (mUpdateCnt - 10) * 3 + aGreen;
                if (aBlue > 0xff)
                {
                    aBlue = 0xff;
                }
                if (aGreen > 0xff)
                {
                    aGreen = 0xff;
                }
            }
            g.SetColor(new Color(0xff, aGreen, aBlue, (int)(aFade * 255.0f)));
            float aWidth = anImage.GetWidth();
            float aScale;
            if (mUpdateCnt < 10)
            {
                aScale = mUpdateCnt * (200.0f / aWidth - 1.0f) / 10.0f + 1.0f;
            }
            else
            {
                aScale = 200.0f / aWidth + (mUpdateCnt - 10) * 0.01f;
            }
            if (aScale != 1.0f)
            {
                aTrans.Scale(aScale, aScale);
            }
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            g.SetColorizeImages(false);
        }

        public void DrawSpaceBubble(Graphics g)
        {
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            Image anImage = Res.IMAGE_SPACEBUBBLE;
            Transform aTrans = new Transform();
            float aFade = 1.0f;
            if (mDuration - 0x1e < mUpdateCnt)
            {
                aFade = (mDuration - mUpdateCnt) / 30.0f;
            }
            g.SetColor(SexyUtil.MakeColor(0x4400ff));
            ScaleColor(g, aFade);
            float aScale = mUpdateCnt < 0x28 ? mUpdateCnt * 1.5f / 40.0f + 0.2f : 1.7f;
            if (aScale != 1.0f)
            {
                aTrans.Scale(aScale, aScale);
            }
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawSpaceStar(Graphics g)
        {
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            Image anImage = Res.IMAGE_SPACESTAR;
            Transform aTrans = new Transform();
            float aFade = 1.0f;
            int aRed = 0xff;
            int aGreen = 0x80;
            if (mDuration - 0x14 < mUpdateCnt)
            {
                aFade = (mDuration - mUpdateCnt) / 20.0f;
            }
            int aGreenStart = mDuration - 0x1e;
            int aRedStart = mDuration - 0x14;
            if (aGreenStart < mUpdateCnt)
            {
                aGreen = (mUpdateCnt - aGreenStart) * -10 + 0x80;
                if (aGreen < 0)
                {
                    aGreen = 0;
                }
            }
            if (aRedStart < mUpdateCnt)
            {
                aRed = (mUpdateCnt - aRedStart) * -10 + 0xff;
                if (aRed < 0)
                {
                    aRed = 0;
                }
            }
            g.SetColor(new Color(aRed, aGreen, 0xff));
            ScaleColor(g, aFade);
            aTrans.Scale(0.4f, 0.4f);
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawFlowerSparkle(Graphics g)
        {
            g.SetColorizeImages(true);
            int anAlpha = 0xff;
            int aLeft = mDuration - mUpdateCnt;
            if (aLeft < 0x14)
            {
                anAlpha = aLeft * 0xff / 0x14;
            }
            g.SetColor(SexyUtil.MakeColor(0x403010, anAlpha));
            g.SetDrawMode(1);
            int aH = SexyUtil.gPhysicalDisplayHeight;
            g.FillRect((int)(mX * aH / 600.0f - 1.0f), (int)(mY * aH / 600.0f - 1.0f), 2, 2);
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawRainbowSpark(Graphics g)
        {
            if (!g.Is3D())
            {
                return;
            }
            g.SetColor(SexyUtil.MakeColor(mColor));
            g.SetDrawMode(1);
            int aPhase = (mUpdateCnt + mValue) % 0x1e;
            if (aPhase > 0xf)
            {
                aPhase = 0x1e - aPhase;
            }
            ScaleColor(g, aPhase * 0x19 / 0xf / 255.0f);
            int aH = SexyUtil.gPhysicalDisplayHeight;
            g.FillRect((int)(mX * aH / 600.0f - 1.0f), (int)(mY * aH / 600.0f - 1.0f), 2, 2);
            g.SetDrawMode(0);
        }

        /// <summary>The free-ball coin toss after a total miss, with its "Total Miss!" caption.</summary>
        public void DrawCoinFlip(Graphics g)
        {
            int aLeft = mDuration - mUpdateCnt;
            if (aLeft < 0)
            {
                return;
            }
            int anAlpha = 0xff;
            if (mUpdateCnt < 10)
            {
                anAlpha = mUpdateCnt * 0xff / 10;
            }
            else if (aLeft < 0x14)
            {
                anAlpha = aLeft * 0xff / 0x14;
            }
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
            Image aHeads = Res.IMAGE_COINHEADS;
            int aH = SexyUtil.gPhysicalDisplayHeight;
            int aX = (int)(mX * aH / 600.0f);
            int aY = (int)((double)(mY * aH) / 600.0 + Layout.EFFECT_MGR_COIN_OFFSET_Y);
            int aHilight = -1;
            int aSide;
            Transform aTrans = new Transform();
            if (mValue < 0)
            {
                aHilight = mUpdateCnt + mValue;
                aSide = mColor == 0 ? 1 : 0;
            }
            else
            {
                float aFlip = Math.Abs((float)Math.Cos(SexyUtil.DegToRad(mValue % 0xb4)));
                aSide = (mValue + 0x5a) / 0xb4 % 2;
                aTrans.Scale(1.0f, aFlip);
            }
            Image aCoin = aSide == 0 ? Res.IMAGE_COINHEADS : Res.IMAGE_COINTAILS;
            g.DrawImageTransform(aCoin, aTrans, aX, aY);
            if (aHilight > -1 && aHilight / 0xf % 2 == 0 && aHilight < 0x5a)
            {
                g.DrawImageCel(Res.IMAGE_COINHILIGHT, Layout.EFFECT_MGR_COIN_X + (aX - aHeads.mWidth / 2),
                    Layout.EFFECT_MGR_COIN_Y + (aY - aHeads.mHeight / 2), aSide);
            }
            g.SetColorizeImages(false);
            if (mDuration - 0x96 < mUpdateCnt)
            {
                anAlpha = ((mDuration - 0x8c) - mUpdateCnt) * 0xff / 10;
            }
            if (anAlpha > -1)
            {
                string aText = SexyAppBase.Loc("Total Miss!");
                g.SetColor(SexyUtil.MakeColor(0xffff00, anAlpha));
                g.SetFont(Res.FONT_OVERLOAD24GRAD2);
                g.DrawString(aText, (int)((double)(mX * aH) / 600.0 - g.GetFont().StringWidth(aText) / 2),
                    aY - Layout.EFFECT_MGR_TOTAL_MISS_OFFSET_Y);
            }
        }

        public void DrawFeverRay(Graphics g)
        {
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            float aFade = 1.0f;
            if (mUpdateCnt < 0x14)
            {
                aFade = mUpdateCnt / 20.0f;
            }
            else if (mDuration - 0x50 < mUpdateCnt)
            {
                aFade = (mDuration - mUpdateCnt) / 80.0f;
            }
            if (mFlags == 1)
            {
                g.SetColor(SexyUtil.MakeColor(0x52210));
            }
            else if (mFlags == 0)
            {
                g.SetColor(SexyUtil.MakeColor(0x111122));
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(0x332210));
            }
            ScaleColor(g, aFade);
            Image anImage = Res.IMAGE_FEVERRAY;
            Transform aTrans = ScratchTransform();
            float aScaleY = mValue / (float)anImage.mHeight + mUpdateCnt * 0.004f;
            float aHalfHeight = anImage.mHeight / 2.0f;
            aTrans.Scale(1.0f, aScaleY);
            aTrans.Translate(0.0f, -0.0f - aHalfHeight * aScaleY);
            aTrans.RotateRad(mAngle + mAngleVel * mUpdateCnt);
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        public void DrawFeverStar(Graphics g)
        {
            Image anImage = Res.IMAGE_FEVERGLOWSTAR;
            if (mFlags == 3)
            {
                anImage = Res.IMAGE_FEVERSTAR_BLUE;
            }
            else if (mFlags == 2)
            {
                anImage = Res.IMAGE_FEVERSTAR_GREEN;
            }
            else if (mFlags == 1)
            {
                anImage = Res.IMAGE_FEVERSTAR_RED;
            }
            Transform aTrans = ScratchTransform();
            if (mAngleVel != 0.0f)
            {
                aTrans.RotateRad(mAngle + mAngleVel * mUpdateCnt);
            }
            // The original falls back to pre-scaled star images when not 3D-accelerated.
            float aScale = mValue / (float)anImage.mWidth;
            aTrans.Scale(aScale, aScale);
            int aFadeTime = 0x32;
            if (mFlags == 0)
            {
                g.SetColorizeImages(true);
                g.SetDrawMode(1);
                g.SetColor(SexyUtil.MakeColor(mColor));
                if (mDuration - aFadeTime < mUpdateCnt)
                {
                    ScaleColor(g, (mDuration - mUpdateCnt) / (float)aFadeTime);
                }
            }
            else if (mDuration - 0x32 < mUpdateCnt)
            {
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (mDuration - mUpdateCnt) * 0xff / aFadeTime));
            }
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        public void DrawFeverSpark(Graphics g)
        {
            if (mAlpha == 0)
            {
                g.SetColor(SexyUtil.MakeColor(GlobalStaticVars.gSexyAppBase.HSLToRGB((int)(mUpdateCnt * 2.0f) % 0xff, 0xff, 0x80)));
            }
            else
            {
                int anAlpha = (int)(Math.Sqrt(mVelY * mVelY + mVelX * mVelX) * 30.0 + 16.0);
                if (anAlpha > 0xff)
                {
                    anAlpha = 0xff;
                }
                g.SetColor(SexyUtil.MakeColor(mColor, anAlpha));
            }
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            if (mAlpha == 0)
            {
                int aPeriod = 0x1e;
                int aPhase = (mFlags + mColor) % 0x1e;
                if (aPhase > aPeriod / 2)
                {
                    aPhase = aPeriod - aPhase;
                }
                ScaleColor(g, aPhase * 0x4b / (aPeriod / 2) / 255.0f);
            }
            if (mAlpha == 0 || !g.Is3D())
            {
                int aH = SexyUtil.gPhysicalDisplayHeight;
                g.FillRect((int)((double)(mX * aH) / 600.0 - mValue), (int)((double)(mY * aH) / 600.0 - mValue), mValue * 2, mValue * 2);
            }
            else
            {
                Image anImage = Res.IMAGE_STREAK;
                float anAngle = (float)Math.Atan2(mVelX, mVelY) + 3.1415927f;
                float aSpeed = (float)Math.Sqrt(mVelX * mVelX + mVelY * mVelY);
                Transform aTrans = ScratchTransform();
                aTrans.Scale(0.35f, 0.05f + aSpeed * 0.3f);
                aTrans.RotateRad(anAngle);
                g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            }
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawFeverTrail(Graphics g)
        {
            Image anImage = Res.IMAGE_FEVERTRAIL;
            if ((mFlags & 4) != 0)
            {
                anImage = Res.IMAGE_SPACESTAR;
            }
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            int aColor = mColor;
            if ((mFlags & 1) != 0)
            {
                aColor = GlobalStaticVars.gSexyAppBase.HSLToRGB((int)(mUpdateCnt * 2.0f) % 0xff, 0xff, 0x80);
            }
            if ((mFlags & 4) == 0)
            {
                g.SetColor(SexyUtil.MakeColor(aColor));
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(aColor, 0x60));
            }
            if (mDuration < mUpdateCnt + 10)
            {
                ScaleColor(g, SexyUtil.ClampRange(mDuration - mUpdateCnt, 0, 10) / 10.0f);
            }
            if ((mFlags & 4) == 0)
            {
                Transform aTrans = ScratchTransform();
                if (!g.Is3D())
                {
                    g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
                }
                else
                {
                    float aScale = mUpdateCnt * -0.004f + 1.1f;
                    if (aScale <= 0.0f)
                    {
                        return;
                    }
                    aTrans.Scale(aScale, aScale);
                    g.DrawImageTransformF(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
                }
            }
            else
            {
                int aH = SexyUtil.gPhysicalDisplayHeight;
                g.DrawImage(anImage, (int)((double)(mX * aH) / 600.0 - mValue), (int)((double)(mY * aH) / 600.0 - mValue), mValue * 2, mValue * 2);
            }
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        /// <summary>The fever score plaque swinging in from the left and out to the right.</summary>
        public void DrawFeverScoreText(Graphics g)
        {
            float anAngle = 0.0f;
            if (mUpdateCnt < 20.0f)
            {
                anAngle = (1.0f - mUpdateCnt / 20.0f) * -1.5707964f;
            }
            else if (mUpdateCnt > mDuration - 20.0f)
            {
                anAngle = (1.0f - (mDuration - mUpdateCnt) / 20.0f) * 1.5707964f;
            }
            int aH = SexyUtil.gPhysicalDisplayHeight;
            g.SetDrawMode(1);
            Image anImage = Res.IMAGE_FEVERSCORE;
            g.DrawImageRotated(anImage, (int)((double)(aH * 0x145) / 600.0 - anImage.mWidth / 2), (int)((double)(aH * 0x69) / 600.0),
                anAngle, (int)((double)(aH * 0xe1) / 600.0), (int)((double)(aH * 0x1a4) / 600.0),
                new TRect(0, 0, anImage.mWidth, anImage.mHeight));
            g.SetDrawMode(0);
            anImage = Res.IMAGE_FEVERSCORETOP;
            g.DrawImageRotated(anImage, (int)((double)(aH * 0x145) / 600.0 - anImage.mWidth / 2), (int)((double)(aH * 0x69) / 600.0),
                anAngle, (int)((double)(aH * 0xe1) / 600.0), (int)((double)(aH * 0x1a4) / 600.0),
                new TRect(0, 0, anImage.mWidth, anImage.mHeight));
        }

        /// <summary>Draws the peg mObj1 slid towards the effect position (the pyramid peg rising into place).</summary>
        public void DrawPyramid(Graphics g)
        {
            float anOrigX = mObj1.GetXPos();
            float anOrigY = mObj1.GetYPos();
            float aX = SexyUtil.InterpValF(mUpdateCnt, mDuration - 5, mX, anOrigX);
            float aY = SexyUtil.InterpValF(mUpdateCnt, mDuration - 5, mY, anOrigY);
            mObj1.SetPos(aX, aY);
            bool wasVisible = mObj1.mVisible;
            mObj1.mVisible = true;
            mObj1.Draw(g);
            mObj1.mVisible = wasVisible;
            mObj1.SetPos(anOrigX, anOrigY);
        }

        /// <summary>A beam from the effect position to the pyramid peg mObj1.</summary>
        public void DrawPyramid2(Graphics g)
        {
            if (mObj1 == null)
            {
                return;
            }
            float aToX = mObj1.GetXPos();
            float aToY = mObj1.GetYPos();
            float aDist = SexyUtil.Dist(mX, mY, aToX, aToY);
            aDist *= SexyUtil.InterpValF(mUpdateCnt, 5, 0.2f, 1.0f);
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            float aFade = 1.0f;
            if (mUpdateCnt < 0x14)
            {
                aFade = mUpdateCnt / 20.0f;
            }
            else if (mDuration - 0x50 < mUpdateCnt)
            {
                aFade = (mDuration - mUpdateCnt) / 80.0f;
            }
            int aRed = 0x88;
            if (mUpdateCnt > 3000)
            {
                aRed = SexyUtil.ClampRange((int)(0x88 + (mUpdateCnt - 3000) * 6.0f), 0, 0xff);
            }
            g.SetColor(new Color(aRed, 0xff, 0x88, (int)(aFade * 255.0f)));
            Image anImage = Res.IMAGE_PYRAMIDRAY;
            Transform aTrans = new Transform();
            float aHalfWidth = anImage.mWidth / 2.0f;
            float aHalfHeight = anImage.mHeight / 2.0f;
            if (g.Is3D())
            {
                float aScaleY = aDist / anImage.mHeight;
                float aScaleX = SexyUtil.InterpValF(mUpdateCnt, 0x14, 0.5f, 1.0f) * 1.0f;
                aTrans.Scale(aScaleX, aScaleY);
                aHalfHeight *= aScaleY;
                aHalfWidth *= aScaleX;
            }
            aTrans.Translate(0.0f, -0.0f - aHalfHeight);
            float anAngle = (float)Math.Atan2(mY - aToY, aToX - mX);
            aTrans.RotateRad(anAngle + SexyUtil.DegToRad(-90.0f));
            g.DrawImageTransform(anImage, aTrans, BoardToScreen(mX), BoardToScreen(mY));
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        /// <summary>
        /// The Extreme Fever rainbow: a textured band between two half-ellipses around (mX, mY)
        /// that sweeps in from the left end and, at the end, fades out from the left.
        /// </summary>
        public void DrawFeverRainbow(Graphics g)
        {
            if (!g.Is3D())
            {
                return;
            }
            int aH = SexyUtil.gPhysicalDisplayHeight;
            float aCX = mX * aH / 600.0f;
            float aCY = mY * aH / 600.0f;
            float aRadiusY = (float)((double)(aH * 0x15e) / 600.0);
            float aRadiusX = (float)((double)(aH * 0x172) / 600.0);
            float aWidth = (float)((double)(aH * 100) / 600.0);
            float aFade = 1.0f;
            float anAlpha = 255.0f;
            if (mDuration - 0x28 < mUpdateCnt)
            {
                anAlpha = (mDuration - mUpdateCnt) * 255.0f / 40.0f;
                aFade = anAlpha / 255.0f;
                aWidth *= aFade;
            }
            InitRainbowArc();
            SexyVector2[][] anArcs = sRainbowArcs;
            int aCount = sRainbowCos.Length;
            aRadiusX += aWidth / 2.0f;
            aRadiusY += aWidth / 2.0f;
            for (int i = 0; i < 2; i++)
            {
                for (int k = 0; k < aCount; k++)
                {
                    anArcs[i][k] = new SexyVector2(aCX + aRadiusX * sRainbowCos[k], aCY - aRadiusY * sRainbowSin[k]);
                }
                if (aCount != 0)
                {
                    anArcs[i][aCount - 1].y = anArcs[i][0].y;
                }
                aRadiusX -= aWidth;
                aRadiusY -= aWidth;
            }

            int aLastIdx = SexyUtil.ClampRange((int)(mUpdateCnt * 1.5f), 0, aCount - 1);
            int aFirstIdx = 0;
            if (mDuration > 0 && mDuration - 0x28 < mUpdateCnt)
            {
                aFirstIdx = (aCount - 1) * (0x28 - (mDuration - mUpdateCnt)) / 0x28;
                if (aFirstIdx < 0)
                {
                    aFirstIdx = 0;
                }
                aLastIdx = aCount - 1;
            }
            g.SetColorizeImages(true);
            for (int aPass = 0; aPass < 2; aPass++)
            {
                if (aPass == 1)
                {
                    g.SetDrawMode(1);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, 0x32));
                }
                g.SetDrawMode(0);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)anAlpha));
                int aTexPeriod = 0x32;
                int aTexOffset = mUpdateCnt / 2 % aTexPeriod;
                float u = (float)((aTexOffset + 0.5) / aTexPeriod);
                int aTris = 0;
                for (int k = aFirstIdx; k < aLastIdx; k++)
                {
                    SexyVector2 aOuter0 = anArcs[0][k];
                    SexyVector2 anInner0 = anArcs[1][k];
                    SexyVector2 aOuter1 = anArcs[0][k + 1];
                    SexyVector2 anInner1 = anArcs[1][k + 1];
                    TriVertex v1 = new TriVertex(aOuter0.x, aOuter0.y, u, 0.0f);
                    TriVertex v2 = new TriVertex(anInner0.x, anInner0.y, u, 1.0f);
                    TriVertex v3 = new TriVertex(aOuter1.x, aOuter1.y, u, 0.0f);
                    TriVertex v4 = new TriVertex(anInner0.x, anInner0.y, u, 1.0f);
                    TriVertex v5 = new TriVertex(aOuter1.x, aOuter1.y, u, 0.0f);
                    TriVertex v6 = new TriVertex(anInner1.x, anInner1.y, u, 1.0f);
                    if (aFade < 1.0f)
                    {
                        float t1 = 1.0f - (1.0f - aFade) * (1.0f - (1.0f - (aLastIdx - (k + 1)) / (float)(aLastIdx - aFirstIdx)));
                        float t0 = 1.0f - (1.0f - aFade) * (1.0f - (1.0f - (aLastIdx - k) / (float)(aLastIdx - aFirstIdx)));
                        v1.y = t0 * (aOuter0.y - anInner0.y) + anInner0.y;
                        v1.x = t0 * (aOuter0.x - anInner0.x) + anInner0.x;
                        v3.y = t1 * (aOuter1.y - anInner1.y) + anInner1.y;
                        v3.x = t1 * (aOuter1.x - anInner1.x) + anInner1.x;
                        v5.y = t1 * (aOuter1.y - anInner1.y) + anInner1.y;
                        v5.x = t1 * (aOuter1.x - anInner1.x) + anInner1.x;
                        float aBase = aPass == 1 ? 50.0f : anAlpha;
                        g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(aBase * t1 * t1 * t1 * t1)));
                        g.DrawTriangleTex(Res.IMAGE_FEVERRAINBOW, v1, v2, v3);
                        g.DrawTriangleTex(Res.IMAGE_FEVERRAINBOW, v4, v5, v6);
                        continue;
                    }
                    // One colour for the whole band: queue its triangles and submit them in one call.
                    sRainbowTris[aTris, 0] = v1;
                    sRainbowTris[aTris, 1] = v2;
                    sRainbowTris[aTris, 2] = v3;
                    sRainbowTris[aTris + 1, 0] = v4;
                    sRainbowTris[aTris + 1, 1] = v5;
                    sRainbowTris[aTris + 1, 2] = v6;
                    aTris += 2;
                }
                if (aTris != 0)
                {
                    g.DrawTrianglesTex(Res.IMAGE_FEVERRAINBOW, sRainbowTris, aTris);
                }
            }
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        // The rainbow's arc angles never change, so their cos/sin are computed once; the arc points
        // and triangles are rebuilt into these buffers each frame instead of new lists.
        private static float[] sRainbowCos;

        private static float[] sRainbowSin;

        private static SexyVector2[][] sRainbowArcs;

        private static TriVertex[,] sRainbowTris;

        private static void InitRainbowArc()
        {
            if (sRainbowCos != null)
            {
                return;
            }
            List<float> anAngles = new List<float>();
            float aStep = 0.025f;
            float aStart = SexyUtil.DegToRad(0.0f);
            float anEnd = 3.1415927f - aStart;
            for (float a = aStart; a < anEnd; a += aStep)
            {
                anAngles.Add(a);
            }
            int aCount = anAngles.Count;
            sRainbowCos = new float[aCount];
            sRainbowSin = new float[aCount];
            for (int k = 0; k < aCount; k++)
            {
                sRainbowCos[k] = (float)Math.Cos(anAngles[k]);
                sRainbowSin[k] = (float)Math.Sin(anAngles[k]);
            }
            sRainbowArcs = new[] { new SexyVector2[aCount], new SexyVector2[aCount] };
            sRainbowTris = new TriVertex[Math.Max(aCount - 1, 1) * 2, 3];
        }

        /// <summary>Where an "ultra extreme fever" letter is drawn: sliding in by mValue until mAngle, out over the last 10 updates.</summary>
        public void GetExtremeFeverPos(out float theX, out float theY)
        {
            theX = mX;
            theY = mY;
            if (mValue != 0)
            {
                if (mAngle <= mUpdateCnt)
                {
                    if (mDuration - 10 < mUpdateCnt)
                    {
                        theX += mValue * (10 - (mDuration - mUpdateCnt)) / 10;
                    }
                }
                else
                {
                    theX += mValue * (mAngle - mUpdateCnt) / mAngle;
                }
            }
            if (mAlpha == 1 && mDuration - 10 < mUpdateCnt)
            {
                theY -= (10 - (mDuration - mUpdateCnt)) * 200 / 10;
            }
        }

        private static string sIndexListText;

        private static int[] sIndexList;

        /// <summary>ParseIndexList, reusing the last result while the list text is unchanged (it is re-read every frame per letter).</summary>
        private static int[] GetIndexList(string theList)
        {
            if (sIndexList == null || theList != sIndexListText)
            {
                sIndexList = ParseIndexList(theList);
                sIndexListText = theList;
            }
            return sIndexList;
        }

        private static int[] ParseIndexList(string theList)
        {
            List<int> aList = new List<int>();
            foreach (string aToken in theList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int.TryParse(aToken.Trim(), out int aValue);
                aList.Add(aValue);
            }
            return aList.ToArray();
        }

        /// <summary>One letter of "ultra extreme fever" (cel mFlags), with its glow and, for mAlpha 2/3, a flame streak.</summary>
        public void DrawExtremeFever(Graphics g)
        {
            Transform aTrans = ScratchTransform();
            float aScale = 1.0f;
            if (mAlpha == 1 && mUpdateCnt < 0xf)
            {
#if PEGGLE_IPHONE
                // The "EXTREME FEVER" text zooms in from 4x.
                aScale += (0xf - mUpdateCnt) * 3.0f / 15.0f;
#else
                aScale += (0xf - mUpdateCnt) * 0.0f / 15.0f;
#endif
            }
            if (aScale != 1.0f)
            {
                aTrans.Scale(aScale, aScale);
            }
            GetExtremeFeverPos(out float aPosX, out float aPosY);
            int aH = SexyUtil.gPhysicalDisplayHeight;
            if (mAlpha == 2 || mAlpha == 3)
            {
                int aTrailTime = 0x14;
                int aTrailStart = (int)(mAngle - 0.0f);
                if (mUpdateCnt < aTrailStart + 0x14)
                {
                    Image aTrail = Res.IMAGE_FLAMETRAIL;
                    float aX = aPosX;
                    float aY = aPosY + -15.0f;
                    bool mirror = mAlpha == 2;
                    TRect aRect = new TRect(0, 0, aTrail.mWidth, aTrail.mHeight);
                    if (mirror)
                    {
                        aX -= aTrail.mWidth;
                    }
                    aX += 0.0f;
                    if (aTrailStart <= mUpdateCnt)
                    {
                        aRect.mWidth -= (mUpdateCnt - aTrailStart) * aRect.mWidth / aTrailTime;
                        int aCut = aTrail.mWidth - aRect.mWidth;
                        aRect.mX += aCut;
                        if (mirror)
                        {
                            aX += aCut;
                        }
                    }
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffa0));
                    g.DrawImageMirror(aTrail, (int)(aX * aH / 600.0f), (int)(aY * aH / 600.0f), aRect, mirror);
                    int aJitterX = 10;
                    int aJitterY = 0;
                    int aDX = SexyUtil.Rand() % (aJitterX * 2) - aJitterX;
                    int aDY = 0;
                    if (aJitterY > 0)
                    {
                        aDY = SexyUtil.Rand() % (aJitterY * 2) - aJitterY;
                    }
                    g.SetDrawMode(1);
                    g.SetColor(SexyUtil.MakeColor(0xa06030));
                    g.DrawImageMirror(aTrail, (int)((aX + aDX) * aH / 600.0f), (int)((double)((aY + aDY) * aH) / 600.0), aRect, mirror);
                    g.SetDrawMode(0);
                    g.SetColorizeImages(false);
                }
            }

            int anAlpha = mColor == 0 ? 0xff : mColor;
            g.SetColorizeImages(true);
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            bool noFeverPegs = aBoard.mLogicMgr.mGoalPegs.Count == 0 && aBoard.mLogicMgr.mNonGoalPegs.Count == 0;
            string anIndexList = noFeverPegs
                ? GlobalStaticVars.gSexyAppBase.GetString("UEF_GLOW_IMAGE_INDEX", "0")
                : GlobalStaticVars.gSexyAppBase.GetString("UEF_EXTREME_GLOW_IMAGE_INDEX", "0");
            int[] anIndices = GetIndexList(anIndexList);
            for (int aPass = 0; aPass < 2; aPass++)
            {
                TRect aRect = new TRect();
                Image anImage;
                if (aPass == 0)
                {
                    g.SetDrawMode(1);
                    anImage = Res.IMAGE_XFEVERGLOW;
                    // The original indexes the parsed list with mFlags unchecked.
                    int aCel = mFlags >= 0 && mFlags < anIndices.Length ? anIndices[mFlags] : 0;
                    aRect = anImage.GetCelRect(aCel);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                }
                else
                {
                    g.SetDrawMode(0);
                    anImage = Res.IMAGE_XFEVER;
                    aRect = anImage.GetCelRect(mFlags);
                    aRect.mHeight -= 1;
                    g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                }
                g.DrawImageTransform(anImage, aTrans, aRect, aPosX * aH / 600.0f, (float)((double)(aPosY * aH) / 600.0 + 30.0));
            }
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        /// <summary>The glow over the free-ball catcher, with two counter-rotating orbs when mFlags is set.</summary>
        public void DrawCatcherGlow(Graphics g)
        {
            if (mObj1 == null)
            {
                return;
            }
            float aX = mObj1.GetXPos() + 0.0f;
            float aY = mObj1.GetYPos() + -57.0f;
            int anAlpha = 0xff;
            if (mUpdateCnt < 0x14)
            {
                anAlpha = mUpdateCnt * 0xff / 0x14;
            }
            else if (mDuration - 0x1e < mUpdateCnt)
            {
                anAlpha = (mDuration - mUpdateCnt) * 0xff / 0x1e;
            }
            anAlpha = (int)(anAlpha * 0.9f);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0xffff88, anAlpha));
            Transform aTrans = new Transform();
            g.DrawImageTransform(Res.IMAGE_CATCHERGLOW, aTrans, BoardToScreen(aX), BoardToScreen(aY));
            g.SetDrawMode(1);
            if (mFlags != 0)
            {
                if (g.Is3D())
                {
                    aTrans.RotateRad(mUpdateCnt * 6.2831855f / mDuration + 3.1415927f);
                }
                g.DrawImageTransform(Res.IMAGE_PYRAMIDBALL, aTrans, BoardToScreen(aX), BoardToScreen(aY + 45.0f));
                aTrans.Reset();
                aTrans.Scale(0.66f, 0.66f);
                if (g.Is3D())
                {
                    aTrans.RotateRad(3.1415927f - mUpdateCnt * 6.2831855f / mDuration);
                }
                g.DrawImageTransform(Res.IMAGE_PYRAMIDBALL, aTrans, BoardToScreen(aX), BoardToScreen(aY + 45.0f));
            }
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        /// <summary>A mote circling the catcher (mObj1) on a wobbly orbit seeded by mAlpha; mFlags is its size.</summary>
        public void DrawCatcherMote(Graphics g)
        {
            if (mObj1 == null)
            {
                return;
            }
            float aX = mObj1.GetXPos() + mX;
            float aY = mObj1.GetYPos() - mY;
            float t = mUpdateCnt / (float)mDuration;
            float aSpeed = 9.0f - (mAlpha % 0xd + 5) / 18.0f;
            float aPhase = mAlpha % 0x11 / 17.0f;
            float aRadius = (mAlpha + 7) % 0xb / 0.7f;
            double anAngle = t * 0.44f * aSpeed + aPhase * 6.2831855f;
            aX = (float)(aX + aRadius * Math.Sin(anAngle));
            aY = (float)(aY + aRadius * Math.Cos(anAngle));
            int aMaxAlpha = (mFlags - 1) * -100 + 0xff;
            int anAlpha;
            if (mUpdateCnt < 0x14)
            {
                anAlpha = aMaxAlpha * mUpdateCnt / 0x14;
            }
            else
            {
                anAlpha = aMaxAlpha;
                if (mDuration - 0x1e < mUpdateCnt)
                {
                    anAlpha = aMaxAlpha * (mDuration - mUpdateCnt) / 0x1e;
                }
            }
            anAlpha = (int)(anAlpha * 0.9f);
            g.SetColorizeImages(true);
            g.SetDrawMode(1);
            g.SetColor(SexyUtil.MakeColor(0xffff88, anAlpha));
            int aH = SexyUtil.gPhysicalDisplayHeight;
            g.DrawImage(Res.IMAGE_SPACESTAR, (int)((double)(aX * aH) / 600.0 - mFlags), (int)((double)(aY * aH) / 600.0 - mFlags), mFlags * 2, mFlags * 2);
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }

        /// <summary>A hat knocked off a ball, tilting with its sideways speed.</summary>
        public void DrawHat(Graphics g)
        {
            float anAngle = SexyUtil.ClampRange(mVelX / 3.0f, -1.0f, 1.0f) * 0.7853982f;
            Image anImage = mFlags == 0 ? Res.IMAGE_HAT : Res.IMAGE_MAGICHAT;
            int aH = SexyUtil.gPhysicalDisplayHeight;
            float aX = (float)((double)(mX * aH) / 600.0 - anImage.mWidth / 2);
            float aY = (float)((double)(mY * aH) / 600.0 - anImage.mHeight / 2 - (double)(aH * 0x14) / 600.0);
            g.DrawImageRotatedF(anImage, aX, aY, anAngle);
        }

        private static readonly string[] JIMMY_PHRASES =
        {
            "Extreme!", "Tubular!", "Radical!", "Awesome!", "Insane!", "Freak Out!",
            "Whoa...", "Wicked!", "Mad Skills!", "Excellent!", "Rock On!", "Toasty!"
        };

        /// <summary>Port addition: Peggle WoW's corner Jimmy phrases (PeggleWoW.exe 0x45e997: phrase % 10).</summary>
        private static readonly string[] WOW_JIMMY_PHRASES =
        {
            "FROST\nSHOCK!", "FEVER IS\n4 FITE!", "AWESUMZ!\nLOL!", "LEEEERR\nRROOOYYY!!!", "Bubble\nHearth!",
            "MORE DOTS!", "QQ MOAR!", "lol wut?", "WTB Fever,\nPST", "Peggle Master\nLFG"
        };

        /// <summary>Jimmy Lightning popping in from the board edge (mFlags = side) with a speech bubble.</summary>
        public void DrawJimmy(Graphics g)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            int aFadeTime = 0x14;
            float aSlide;
            if (mUpdateCnt < 0x14)
            {
                aSlide = mUpdateCnt / 20.0f;
            }
            else if (mDuration - 0x14 < mUpdateCnt)
            {
                aSlide = (mDuration - mUpdateCnt) / 20.0f;
            }
            else
            {
                aSlide = 1.0f;
            }
            int aDir = mFlags == 0 ? 1 : -1;
            Image aJimmy = GetJimmyImage();
            Image aSpeech = Res.IMAGE_JIMMYSPEECH;
            int aSlideX = (int)(aJimmy.mWidth * aDir * aSlide);
            int aSlideY = (int)(-aJimmy.mHeight * aSlide);
            int aH = SexyUtil.gPhysicalDisplayHeight;
            TRect aClip = aBoard.mBoardRect;
            aClip.mX = 0;
            aClip.mY = 0;
            aClip.mWidth = (int)((double)(aClip.mWidth * aH) / 600.0);
            aClip.mHeight = (int)((double)(aClip.mHeight * aH) / 600.0);
            int aY = aClip.mHeight + aSlideY;
            int aX = mFlags == 0 ? -aJimmy.mWidth : aClip.mWidth;
            aX += aSlideX;
            Graphics aClipG = g.Create();
            if (mPriority > 0)
            {
                aClip.mHeight -= Layout.EFFECT_MGR_JIMMY_OFFSET_Y;
                if (mFlags == 0)
                {
                    aClip.mX += Layout.EFFECT_MGR_JIMMY_OFFSET_X;
                }
                else
                {
                    aClip.mWidth -= Layout.EFFECT_MGR_JIMMY_OFFSET_X;
                }
            }
            aClipG.ClipRect(aClip);
            aClipG.DrawImageMirror(aJimmy, aX, aY, mFlags != 0);

            int anAlpha = 0xff;
            int aFadeIn = 1;
            int anEnd = mDuration - aFadeTime;
            if (mUpdateCnt <= aFadeTime)
            {
                return;
            }
            if (aFadeTime < mUpdateCnt && mUpdateCnt < aFadeTime + aFadeIn)
            {
                anAlpha = 0xff - (aFadeTime + aFadeIn - mUpdateCnt) * 0xff / aFadeIn;
            }
            else if (anEnd - aFadeIn < mUpdateCnt)
            {
                anAlpha = (anEnd - mUpdateCnt) * 0xff / aFadeIn;
                if (anAlpha < 0)
                {
                    return;
                }
            }
            int aBubbleX = aX + (mFlags == 0 ? aJimmy.mWidth : -aSpeech.mWidth);
            int aBubbleY = aY - aSpeech.mHeight;
            g.SetColorizeImages(true);
            g.SetColor(new Color(0xff, 0xff, 0xff, anAlpha));
            g.DrawImageMirror(aSpeech, aBubbleX, aBubbleY, mFlags != 0);
            g.SetColorizeImages(false);
            g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
            g.SetFont(Res.GetFontById(ResourceId.FONT_OVERLOAD16OUTLINE_ID));
            if (mJimmyGame == JIMMY_WOW)
            {
                DrawWoWJimmyText(g, WOW_JIMMY_PHRASES[mColor % WOW_JIMMY_PHRASES.Length], aBubbleX, aBubbleY, aSpeech);
                return;
            }
            string aText = SexyAppBase.Loc(JIMMY_PHRASES[mColor % 0xc]);
            int aWidth = g.GetFont().StringWidth(aText);
            int anAscent = g.GetFont().GetAscent();
#if PEGGLE_IPHONE
            g.DrawString(aText, aBubbleX + (aSpeech.mWidth - aWidth) / 2,
                aBubbleY + (aSpeech.mHeight - anAscent) / 2 + g.GetFont().GetAscent());
#else
            g.DrawString(aText, aBubbleX + (aSpeech.mWidth - aWidth) / 2 + Layout.EFFECT_MGR_JIMMY_SPEECH_OFFSET_X,
                Layout.EFFECT_MGR_JIMMY_SPEECH_OFFSET_Y + aBubbleY + (aSpeech.mHeight - anAscent) / 2 + g.GetFont().GetAscent());
#endif
        }

        /// <summary>
        /// Port addition: a WoW Jimmy phrase in the bubble (PeggleWoW.exe 0x45e9e8): one line as Jimmy's are, or
        /// two lines split at the line break, each centred, their tops at (bubble height - 1.85 / 0.15 font
        /// heights) / 2 from the bubble's top.
        /// </summary>
        private static void DrawWoWJimmyText(Graphics g, string theText, int theBubbleX, int theBubbleY, Image theSpeech)
        {
            Font aFont = g.GetFont();
#if PEGGLE_IPHONE
            int anOffsetX = 0;
            int anOffsetY = 0;
#else
            int anOffsetX = Layout.EFFECT_MGR_JIMMY_SPEECH_OFFSET_X;
            int anOffsetY = Layout.EFFECT_MGR_JIMMY_SPEECH_OFFSET_Y;
#endif
            int aBreak = theText.IndexOf('\n');
            if (aBreak < 0)
            {
                g.DrawString(theText, theBubbleX + (theSpeech.mWidth - aFont.StringWidth(theText)) / 2 + anOffsetX,
                    anOffsetY + theBubbleY + (theSpeech.mHeight - aFont.GetAscent()) / 2 + aFont.GetAscent());
                return;
            }
            string[] aLines = { theText.Substring(0, aBreak), theText.Substring(aBreak + 1) };
            double[] aTops = { 1.85, 0.15 };
            for (int i = 0; i < 2; i++)
            {
                int y = theBubbleY + (int)((theSpeech.mHeight - aFont.GetHeight() * aTops[i]) * 0.5) + aFont.GetAscent() + anOffsetY;
                g.DrawString(aLines[i], theBubbleX + (theSpeech.mWidth - aFont.StringWidth(aLines[i])) / 2 + anOffsetX, y);
            }
        }

        /// <summary>The spinning trail of stars for a style shot, curling towards (mFlags, mAlpha).</summary>
        public void DrawStyleSpin(Graphics g)
        {
            if (PhysObj.GetBoard() == null)
            {
                return;
            }
            g.SetColorizeImages(true);
            g.SetDrawMode(1);
            int aAge = mDuration - mUpdateCnt < mUpdateCnt ? mDuration - mUpdateCnt : mUpdateCnt;
            int anAlpha = aAge * 4 + 0x50;
            if (mUpdateCnt > 0x14)
            {
                anAlpha = (int)(aAge / 20.0f * 160.0f * (aAge / 20.0f));
            }
            g.SetColor(new Color(100, 0xff, 0x9b, SexyUtil.ClampRange(anAlpha, 0, 255)));
            float aDY = (aAge + 1) * (mY - mAlpha) / 14.0f;
            float aDX = ((aAge + 1) * (mX - mFlags) / 14.0f + (mX - mFlags) * 2.0f) / 3.0f;
            int aX = (int)(mFlags + aDX);
            int aY = (int)(mAlpha + aDY);
            int aCount = aAge < 0xf ? aAge : 0xf;
            int aH = SexyUtil.gPhysicalDisplayHeight;
            for (int i = 0; i < aCount; i++)
            {
                int aSize = (int)((0xf - i) / 6.0f + 1.0f);
                float aPull = i / 14.0f * (i / 14.0f);
                if (mDuration / 4 < mUpdateCnt)
                {
                    aPull = aPull * (mDuration * 0.5f - mUpdateCnt) * 0.05f;
                }
                g.DrawImage(Res.IMAGE_SPACESTAR,
                    (int)((double)(aX * aH) / 600.0 - aSize + i * mVelX * 0.075f - aDX * aPull * 0.3f),
                    (int)((double)(aY * aH) / 600.0 - aSize + i * mVelY * 0.075f - aDY * aPull * 0.3f),
                    aSize * 2, aSize * 2);
            }
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        /// <summary>A ball (mObj1) shrinking into a hole, with its hat if it has one.</summary>
        public void DrawBallHole(Graphics g)
        {
            if (mObj1 == null || mObj2 == null)
            {
                return;
            }
            Ball aBall = (Ball)mObj1;
            float t = mUpdateCnt / (float)mDuration;
            float aScale = (1.0f - t) * 0.9f + 0.1f;
            SexyVector2 aPos = new SexyVector2(mX, mY);
            Transform aTrans = new Transform();
            if (aScale != 1.0f)
            {
                aTrans.Scale(aScale, aScale);
            }
            if (aBall.mHasHat)
            {
                float aHatDX = (aBall.mHatX - aBall.mX) * (1.0f - t);
                float aHatDY = (aBall.mHatY - aBall.mY - 20.0f) * (1.0f - t);
                Image aHat = aBall.mHatBall != null ? Res.IMAGE_MAGICHAT : Res.IMAGE_HAT;
                if (!g.Is3D())
                {
                    g.DrawImageTransform(aHat, aTrans, BoardToScreen(aPos.x + aHatDX), BoardToScreen(aPos.y + aHatDY));
                }
                else
                {
                    float anAngle = SexyUtil.ClampRange(mVelX / 3.0f, -1.0f, 1.0f) * 0.7853982f;
                    Transform aHatTrans = new Transform(aTrans);
                    aHatTrans.RotateRad(anAngle);
                    g.DrawImageTransform(aHat, aHatTrans, BoardToScreen(aPos.x + aHatDX), BoardToScreen(aPos.y + aHatDY));
                }
            }
            if (!aBall.mFireball)
            {
                g.DrawImageTransformF(Res.IMAGE_BALL, aTrans, BoardToScreen(aPos.x), BoardToScreen(aPos.y));
            }
            else
            {
                float anAngle = 0.0f;
                Board aBoard = PhysObj.GetBoard();
                if (aBoard != null)
                {
                    anAngle = aBoard.mGameUpdateCnt * 0.05f;
                }
                if (g.Is3D())
                {
                    aTrans.RotateRad(anAngle);
                }
                g.DrawImageTransformF(Res.IMAGE_FIREBALL, aTrans, BoardToScreen(aPos.x), BoardToScreen(aPos.y));
            }
        }

        public void DrawSpookyPoof(Graphics g)
        {
            Transform aTrans = new Transform();
            Image anImage;
            int aCel;
            switch (mFlags)
            {
                case 2: anImage = Res.IMAGE_SPOOKYPOOF2; aCel = 0; break;
                case 1: anImage = Res.IMAGE_SPOOKYPOOF1; aCel = 1; break;
                case 0: anImage = Res.IMAGE_SPOOKYPOOF1; aCel = 0; break;
                default: anImage = Res.IMAGE_SPOOKYPOOF2; aCel = 1; break;
            }
            TRect aRect = anImage.GetCelRect(aCel);
            g.SetColorizeImages(true);
            int anAlpha = 0xff;
            if (mDuration - 0x28 < mUpdateCnt)
            {
                anAlpha = (mDuration - mUpdateCnt) * 0xff / 0x28;
                if (anAlpha < 1)
                {
                    return;
                }
            }
            g.SetColor(SexyUtil.MakeColor(0x44ff44, anAlpha));
            float aScale = mUpdateCnt / 10.0f + 0.2f;
            aTrans.Scale(aScale, aScale);
            g.DrawImageTransform(anImage, aTrans, aRect, BoardToScreen(mX), BoardToScreen(mY));
            g.SetColorizeImages(false);
        }

        /// <summary>A ghost spiralling out from (mX, mY), starting after mColor updates.</summary>
        public void DrawSpookyGhost(Graphics g)
        {
            Transform aTrans = new Transform();
            int aStart = mColor;
            int aFadeOut = 0x46;
            if (mUpdateCnt < aStart)
            {
                return;
            }
            float aTurn = mFlags != 0 ? -0.03f : 0.03f;
            float aSpeed = 1.5f;
            float anAngle = mAngle + mUpdateCnt * aTurn;
            float aX = mX + mUpdateCnt * 1.5f * (float)Math.Cos(anAngle);
            float aY = mY + mUpdateCnt * aSpeed * (float)Math.Sin(anAngle);
            float aPrevX = mX + (mUpdateCnt - 1) * aSpeed * (float)Math.Cos(anAngle - aTurn);
            float aPrevY = mY + (mUpdateCnt - 1) * aSpeed * (float)Math.Sin(anAngle - aTurn);
            float aRot = (float)Math.Atan2(aPrevY - aY, aX - aPrevX) - 1.5707964f;
            aTrans.RotateRad(aRot);
            g.SetColorizeImages(true);
            int anAlpha = 0xff;
            if (mUpdateCnt < aStart + 10)
            {
                anAlpha = (mUpdateCnt - aStart) * 0xff / 10;
                if (anAlpha < 1)
                {
                    return;
                }
            }
            else if (mDuration - aFadeOut < mUpdateCnt)
            {
                anAlpha = (mDuration - mUpdateCnt) * 0xff / aFadeOut;
                if (anAlpha < 1)
                {
                    return;
                }
            }
            g.SetColor(SexyUtil.MakeColor(0xaaffaa, anAlpha));
            g.DrawImageTransform(Res.IMAGE_SPOOKYGHOST, aTrans, BoardToScreen(aX), BoardToScreen(aY));
            g.SetColorizeImages(false);
        }

        public void DrawSpookySparkle(Graphics g)
        {
            int aStart = 0;
            int aFadeOut = 0x28;
            g.SetColorizeImages(true);
            int anAlpha = 10;
            if (mUpdateCnt < aStart + 10)
            {
                anAlpha = (mUpdateCnt - aStart) * 10 / 10;
                if (anAlpha < 1)
                {
                    return;
                }
            }
            else if (mDuration - aFadeOut < mUpdateCnt)
            {
                anAlpha = (mDuration - mUpdateCnt) * 10 / aFadeOut;
                if (anAlpha < 1)
                {
                    return;
                }
            }
            int aH = SexyUtil.gPhysicalDisplayHeight;
            if (mFlags == 0)
            {
                Image anImage = Res.IMAGE_SPOOKYTRAIL;
                g.SetColor(SexyUtil.MakeColor(0x60ff20, anAlpha));
                g.DrawImage(anImage, (int)((double)(mX * aH) / 600.0 - anImage.mWidth / 2), (int)((double)(mY * aH) / 600.0 - anImage.mHeight / 2));
            }
            else
            {
                Image anImage = Res.IMAGE_SPACESTAR;
                g.SetDrawMode(1);
                g.SetColor(SexyUtil.MakeColor(0x808020, anAlpha * 4));
                g.DrawImage(anImage, (int)((double)(mX * aH) / 600.0 - anImage.mWidth / 2), (int)((double)(mY * aH) / 600.0 - anImage.mHeight / 2));
                g.SetDrawMode(0);
            }
            g.SetColorizeImages(false);
        }

        /// <summary>Glowing eyes on every spooky ball; the ball is redrawn with the clip top at the board's top.</summary>
        public void DrawSpookyEyes(Graphics g)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null || aBoard.mBalls.Count == 0)
            {
                return;
            }
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            foreach (Ball aBall in aBoard.mBalls)
            {
                if (aBall.mFlagFE || aBall.mFlag11C)
                {
                    Transform aTrans = new Transform();
                    int aClipY = g.mClipRect.mY;
                    g.mClipRect.mY = aBoard.mBoardRect.mY;
                    aBall.Draw(g);
                    g.SetDrawMode(1);
                    g.DrawImageTransform(Res.IMAGE_SPOOKYEYES, aTrans, BoardToScreen(aBall.mDrawX), BoardToScreen(aBall.mDrawY));
                    g.SetDrawMode(0);
                    g.mClipRect.mY = aClipY;
                }
            }
        }

        public void DrawEmber(Graphics g)
        {
            Image anImage = Res.IMAGE_EMBER;
            if (anImage.mAnimInfo != null && mUpdateCnt >= anImage.mAnimInfo.mTotalAnimTime)
            {
                return;
            }
            TRect aRect = anImage.GetCelRect(anImage.GetAnimCel(mUpdateCnt));
            Transform aTrans = new Transform();
            g.SetColorizeImages(true);
            g.SetDrawMode(1);
            g.SetColor(SexyUtil.MakeColor(0x806060, 0x80));
            if (!g.Is3D())
            {
                g.DrawImageTransform(anImage, aTrans, aRect, BoardToScreen(mX), BoardToScreen(mY));
            }
            else
            {
                g.DrawImageTransformF(anImage, aTrans, aRect, BoardToScreen(mX), BoardToScreen(mY));
            }
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawFireballEmber(Graphics g)
        {
            Image anImage = Res.IMAGE_EMBER;
            int aCel = (int)(mUpdateCnt * 0.15f);
            if (aCel >= anImage.mNumCols)
            {
                return;
            }
            Transform aTrans = new Transform();
            if (mAngle != 0.0f)
            {
                aTrans.RotateRad(mAngle);
            }
            TRect aRect = anImage.GetCelRect(aCel);
            g.SetColorizeImages(true);
            g.SetDrawMode(1);
            g.SetColor(SexyUtil.MakeColor(0xa08080, 0xff));
            g.DrawImageTransform(anImage, aTrans, aRect, BoardToScreen(mX), BoardToScreen(mY));
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawSmoke(Graphics g)
        {
            Image anImage = Res.IMAGE_SMOKE;
            int aCel = mUpdateCnt / 3;
            if (aCel >= anImage.mNumCols)
            {
                return;
            }
            TRect aRect = anImage.GetCelRect(aCel);
            Transform aTrans = new Transform();
            float aScale = mFlags != 0 ? 0.45f : 1.0f;
            if (aScale != 1.0f)
            {
                aTrans.Scale(aScale, aScale);
            }
            g.SetColorizeImages(true);
            if (mFlags == 0)
            {
                g.SetColor(SexyUtil.MakeColor(0x303030, 0x30));
            }
            else
            {
                g.SetColor(SexyUtil.MakeColor(mColor, (mDuration - mUpdateCnt) * 0x40 / mDuration));
            }
            g.DrawImageTransformF(anImage, aTrans, aRect, BoardToScreen(mX), BoardToScreen(mY));
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        public void DrawFleck(Graphics g)
        {
            Image anImage = Res.IMAGE_FLECK;
            int aCel = (mUpdateCnt + mAlpha) / 0xc % 4;
            if (mFlags != 0)
            {
                aCel = 3 - aCel;
            }
            TRect aRect = anImage.GetCelRect(aCel);
            Transform aTrans = new Transform();
            aTrans.Scale(0.33f, 0.33f);
            int anAlpha = 0x40;
            if (mDuration - mUpdateCnt < 10)
            {
                anAlpha = (mDuration - mUpdateCnt) * 0x40 / 10;
            }
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(mColor, anAlpha));
            g.DrawImageTransformF(anImage, aTrans, aRect, BoardToScreen(mX), BoardToScreen(mY));
            g.SetColorizeImages(false);
        }

        public void DrawFlameTrail(Graphics g)
        {
        }

        /// <summary>Draws mObj2's image over the bumper mObj1, fading out at the end.</summary>
        public void DrawLevelBumper(Graphics g)
        {
            if (mObj1 == null)
            {
                return;
            }
            Image anImage = mObj2?.mImage;
            int anAlpha = 200;
            if (mUpdateCnt > -1 && mDuration - 10 < mUpdateCnt)
            {
                anAlpha = (mDuration - mUpdateCnt) * 200 / 10;
            }
            if (anAlpha != 0xff)
            {
                if (anAlpha < 1)
                {
                    return;
                }
                if (anAlpha < 0xff)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                }
            }
            SexyVector2 aCenter = mObj1.GetCenter();
            Transform aTrans = new Transform();
            if (anImage != null)
            {
                g.DrawImageTransform(anImage, aTrans, BoardToScreen(aCenter.x), BoardToScreen(aCenter.y));
            }
            g.SetColorizeImages(false);
        }

        public void DrawFeverNuke(Graphics g)
        {
        }

        public void DrawNukeStreak(Graphics g)
        {
        }

        public void Draw(Graphics g)
        {
            switch (mType)
            {
                case TYPE_EXPLOSION: DrawExplosion(g); break;
                case TYPE_SPARKLE: DrawSparkle(g); break;
                case TYPE_SPARKLE_FEVER: DrawSparkle(g); break;
                case TYPE_FIREBALL_FIRE: DrawFireballFire(g); break;
                case TYPE_STREAK: DrawStreak(g); break;
                case TYPE_RIBBON: DrawRibbon(g); break;
                case TYPE_COIN_FLIP: DrawCoinFlip(g); break;
                case TYPE_BUBBLE: DrawBubble(g); break;
                case TYPE_SPACE_CRACKS: DrawSpaceCracks(g); break;
                case TYPE_SPACE_HALO: DrawSpaceHalo(g); break;
                case TYPE_SPACE_BURST: DrawSpaceBurst(g); break;
                case TYPE_SPACE_BUBBLE: DrawSpaceBubble(g); break;
                case TYPE_SPACE_STAR: DrawSpaceStar(g); break;
                case TYPE_FLOWER_SPARKLE: DrawFlowerSparkle(g); break;
                case TYPE_FEVER_RAY: DrawFeverRay(g); break;
                case TYPE_FEVER_STAR: DrawFeverStar(g); break;
                case TYPE_FEVER_SPARK: DrawFeverSpark(g); break;
                case TYPE_FEVER_TRAIL: DrawFeverTrail(g); break;
                case TYPE_FEVER_CRACKLE: DrawFeverCrackle(g); break;
                case TYPE_FEVER_SCORE_TEXT: DrawFeverScoreText(g); break;
                case TYPE_LIGHTNING_CRACKLE: DrawLightningCrackle(g); break;
                case TYPE_LIGHTNING_BOLT: DrawLightningBolt(g); break;
                case TYPE_PYRAMID: DrawPyramid(g); break;
                case TYPE_PYRAMID2: DrawPyramid2(g); break;
                case TYPE_FEVER_RAINBOW: DrawFeverRainbow(g); break;
                case TYPE_CATCHER_GLOW: DrawCatcherGlow(g); break;
                case TYPE_CATCHER_MOTE: DrawCatcherMote(g); break;
                case TYPE_EXTREME_FEVER: DrawExtremeFever(g); break;
                case TYPE_HAT: DrawHat(g); break;
                case TYPE_JIMMY: DrawJimmy(g); break;
                case TYPE_BALL_HOLE: DrawBallHole(g); break;
                case TYPE_MASTER_BADGE: DrawMasterBadge(g); break;
                case TYPE_SPOOKY_POOF: DrawSpookyPoof(g); break;
                case TYPE_SPOOKY_GHOST: DrawSpookyGhost(g); break;
                case TYPE_SPOOKY_EYES: DrawSpookyEyes(g); break;
                case TYPE_SPOOKY_SPARKLE: DrawSpookySparkle(g); break;
                case TYPE_RAINBOW_SPARK: DrawRainbowSpark(g); break;
                case TYPE_EMBER: DrawEmber(g); break;
                case TYPE_SMOKE: DrawSmoke(g); break;
                case TYPE_FLECK: DrawFleck(g); break;
                case TYPE_FLAME_TRAIL: DrawFlameTrail(g); break;
                case TYPE_FIREBALL_EMBER: DrawFireballEmber(g); break;
                case TYPE_LEVEL_BUMPER: DrawLevelBumper(g); break;
                case TYPE_STYLE_SPIN: DrawStyleSpin(g); break;
                case TYPE_FEVER_NUKE: DrawFeverNuke(g); break;
                case TYPE_NUKE_STREAK: DrawNukeStreak(g); break;
            }
        }

        /// <summary>Returns false once the effect has run its course.</summary>
        public bool Update(bool slowMoTick)
        {
            if (slowMoTick && mNoSlowMo)
            {
                return true;
            }
            mUpdateCnt++;
            if (mUpdateCnt < 0)
            {
                if (mType == TYPE_EXTREME_FEVER)
                {
                    UpdateExtremeFever();
                }
                return true;
            }
            if (mDuration < mUpdateCnt && mDuration != 0)
            {
                return false;
            }
            switch (mType)
            {
                case TYPE_SPARKLE_FEVER: UpdateSparkleFever(); break;
                case TYPE_RIBBON: UpdateRibbon(); break;
                case TYPE_COIN_FLIP: UpdateCoin(); break;
                case TYPE_BUBBLE: UpdateBubble(); break;
                case TYPE_SPACE_STAR: UpdateSpaceStar(); break;
                case TYPE_FLOWER_SPARKLE: UpdateFlowerSparkle(); break;
                case TYPE_FEVER_STAR: UpdateFeverStar(); break;
                case TYPE_FEVER_SPARK:
                    if (!slowMoTick)
                    {
                        UpdateFeverSpark();
                    }
                    break;
                case TYPE_FEVER_CRACKLE: UpdateFeverCrackle(); break;
                case TYPE_FEVER_SCORE_TEXT: UpdateFeverScoreText(); break;
                case TYPE_PYRAMID: UpdatePyramid(); break;
                case TYPE_CATCHER_MOTE: UpdateCatcherMote(); break;
                case TYPE_EXTREME_FEVER: UpdateExtremeFever(); break;
                case TYPE_HAT: UpdateHat(); break;
                case TYPE_JIMMY: UpdateJimmy(); break;
                case TYPE_BALL_HOLE:
                    UpdateBallHole();
                    return true;
                case TYPE_SPOOKY_EYES: UpdateSpookyTrail(); break;
                case TYPE_STYLE_SPIN: UpdateStyleSpin(); break;
                case TYPE_NUKE_STREAK: UpdateNukeStreak(); break;
            }
            if (slowMoTick)
            {
                return true;
            }
            mX += mVelX;
            mY += mVelY;
            if (mGravity > 0.0f)
            {
                mVelY += mGravity;
                if (mVelY > 10.0f)
                {
                    mVelY = 10.0f;
                }
            }
            return true;
        }

        public bool UpdateSlowMo(float theFraction)
        {
            if (mNoSlowMo)
            {
                return Update(false);
            }
            if (mUpdateCnt < 0)
            {
                return true;
            }
            if (mType == TYPE_FEVER_SPARK)
            {
                UpdateFeverSpark();
            }
            mX += mVelX * theFraction;
            mY += mVelY * theFraction;
            if (mGravity > 0.0f)
            {
                mVelY += mGravity * theFraction;
                if (mVelY > 10.0f)
                {
                    mVelY = 10.0f;
                }
            }
            return true;
        }

        /// <summary>Spins the coin; once fewer than 150 updates remain it stops on the chosen side (mColor).</summary>
        public void UpdateCoin()
        {
            if (mValue < 0)
            {
                return;
            }
            int aPrev = mValue;
            mValue = (mValue + 9) % 0x168;
            if (mValue >= 0x58 && mValue <= 0x5c)
            {
                mValue = 0x5d;
            }
            else if (mValue > 0x10b && mValue < 0x111)
            {
                mValue = 0x111;
            }
            if (mDuration - mUpdateCnt < 0x96)
            {
                if (mColor == 1)
                {
                    if (mValue < aPrev)
                    {
                        mValue = -mUpdateCnt;
                    }
                }
                else if (aPrev < 0xb5 && mValue > 0xb3)
                {
                    mValue = -mUpdateCnt;
                }
            }
        }

        public void UpdateBubble()
        {
            if (mY < -20.0f)
            {
                mDuration = mUpdateCnt;
            }
        }

        public void UpdateFeverStar()
        {
            if (mX < 0.0f || mX > 650.0f)
            {
                mVelX *= -1.0f;
            }
            if (mY > 630.0f)
            {
                mDuration = mUpdateCnt;
            }
        }

        public void UpdateSpaceStar()
        {
            mVelX -= (0x32 - SexyUtil.Rand() % 100) / 100.0f;
            mVelX *= 0.95f;
            mVelY *= 0.95f;
        }

        public void UpdateFeverCrackle()
        {
            mColor = GlobalStaticVars.gSexyAppBase.HSLToRGB(mFlags % 0xff, 0xff, 0x80);
            if (!GlobalStaticVars.gSexyAppBase.Is3DAccelerated() || mUpdateCnt != mAlpha)
            {
                return;
            }
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            if (aBoard.mLogicMgr.mGoalPegs.Count == 0 && aBoard.mLogicMgr.mNonGoalPegs.Count == 0)
            {
                Effect anEffect = aBoard.mEffectMgr.AddEffect(TYPE_FEVER_TRAIL, mX + SexyUtil.Rand() % 2, mY + SexyUtil.Rand() % 2,
                    mY < 150.0f ? 2 : 0, false);
                anEffect.mDuration = 0x19;
                anEffect.mFlags |= 2;
                anEffect.mFlags |= 4;
                anEffect.mVelX = (1.0f - SexyUtil.Rand() % 200 / 100.0f) * 3.6f + mVelX * 1.0f;
                anEffect.mVelY = (1.0f - SexyUtil.Rand() % 200 / 100.0f) * 3.6f + mVelY * 1.0f;
                anEffect.mValue = 4;
                anEffect.mGravity = mGravity;
                anEffect.mColor = mColor;
                anEffect.mUpdateCnt = -1;
            }
        }

        /// <summary>Fever sparks (mAlpha 1/2 = spin direction) swirl around a random live ball.</summary>
        public void UpdateFeverSpark()
        {
            mFlags++;
            if (mAlpha == 0)
            {
                return;
            }
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null || aBoard.mFlagBA)
            {
                return;
            }
            if (aBoard.mBalls.Count == 0)
            {
                mVelX *= 0.95f;
                mVelY *= 0.95f;
                return;
            }
            Ball aBall = aBoard.mBalls[SexyUtil.Rand() % aBoard.mBalls.Count];
            float aDY = mY - aBall.GetY();
            float aDX = mX - aBall.GetX();
            if (aDY == 0.0f && aDX == 0.0f)
            {
                return;
            }
            float aDistSq = aDY * aDY + aDX * aDX;
            if (aDistSq < 4.0f)
            {
                aDistSq = 4.0f;
            }
            if (aDistSq < 1000.0f)
            {
                mVelX += aDX * 2.0f / aDistSq;
                mVelY += aDY * 2.0f / aDistSq;
                float aTurn = mAlpha == 1 ? -0.04f : 0.04f;
                float aVelX = mVelX;
                mVelX = (float)(mVelX * Math.Cos(aTurn) + mVelY * Math.Sin(aTurn));
                mVelY = (float)(mVelY * Math.Cos(aTurn) - aVelX * Math.Sin(aTurn));
            }
            else
            {
                mVelX *= 0.95f;
                mVelY *= 0.95f;
            }
        }

        public void UpdateFlowerSparkle()
        {
            mVelX *= 0.97f;
            mVelY *= 0.97f;
        }

        /// <summary>A fever sparkle leaving a rainbow trail and homing in on one of the balls.</summary>
        public void UpdateSparkleFever()
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            int aSize = 0x3c / (mUpdateCnt + 10);
            if (aSize < 1)
            {
                aSize = 1;
            }
            Effect aTrail = aBoard.mEffectMgr.AddEffect(TYPE_FEVER_TRAIL, mX, mY, mY < 150.0f ? 2 : 0, false);
            aTrail.mDuration = 0x19;
            aTrail.mFlags |= 2;
            aTrail.mFlags |= 1;
            aTrail.mFlags |= 4;
            aTrail.mVelX = (1.0f - SexyUtil.Rand() % 200 / 100.0f) * 0.1f + mVelX * 0.25f;
            aTrail.mVelY = (1.0f - SexyUtil.Rand() % 200 / 100.0f) * 0.1f + mVelY * 0.25f;
            aTrail.mValue = aSize + 4;
            aTrail.mColor = GlobalStaticVars.gSexyAppBase.HSLToRGB(mUpdateCnt % 0xff, 0xff, 0x80);
            aTrail.mUpdateCnt = -1;
            if (aBoard.mBalls.Count != 0 && mUpdateCnt > 0x13)
            {
                Ball aBall = aBoard.mBalls[mFlags % aBoard.mBalls.Count];
                float aDX = aBall.mX - mX;
                float aDY = aBall.mY - mY;
                float aDist = (float)Math.Pow(aDX * aDX + aDY * aDY, 0.6);
                if (aDist > 0.0f)
                {
                    mVelX *= 0.95f;
                    mVelY *= 0.95f;
                    mVelX += aDX * 1.7f / aDist;
                    mVelY += aDY * 1.7f / aDist;
                    float aSpeedSq = mVelX * mVelX + mVelY * mVelY;
                    if (aSpeedSq < 6.0f && aSpeedSq > 0.0f)
                    {
                        mVelX = mVelX * 6.0f / aSpeedSq;
                        mVelY = mVelY * 6.0f / aSpeedSq;
                    }
                }
            }
        }

        public void UpdateCatcherMote()
        {
            mVelY *= 0.99f;
        }

        public void UpdatePyramid()
        {
            if (mFlags != 0 && mUpdateCnt == mDuration && mObj1 != null)
            {
                mObj1.mVisible = true;
            }
        }

        /// <summary>At the end of the ribbon flags the board; with mFlags > 1 it re-launches itself as further end rewards.</summary>
        public void UpdateRibbon()
        {
            if (mUpdateCnt > 0 && mUpdateCnt == mDuration)
            {
                Board aBoard = PhysObj.GetBoard();
                if (aBoard != null)
                {
                    if (mFlags == 1)
                    {
                        aBoard.mFlagC0 = true;
                    }
                    else
                    {
                        aBoard.mFlagBF = true;
                    }
                }
            }
            if ((mUpdateCnt > 0x50 || mDuration > 0) && mFlags > 1)
            {
                mFlags--;
                Effect aReward = PhysObj.GetBoard().mLogicMgr.AddEndReward(2, true);
                mAlpha = 0x14;
                mUpdateCnt = 0;
                if (aReward != null)
                {
                    aReward.mAlpha = 0xfe;
                }
            }
        }

        /// <summary>Particles for the fever letters; empty in this build.</summary>
        public void AddExtremeFeverParticles(int theType, float theX, float theY, int theChance, int theCount)
        {
        }

        /// <summary>
        /// Drives an "ultra extreme fever" letter: its sounds, and fading while a ball passes
        /// behind it (in zoomed board coordinates).
        /// </summary>
        public void UpdateExtremeFever()
        {
            if (GlobalStaticVars.gSexyAppBase.mUpdateCount % 4 == 0)
            {
                mY += -1.0f;
            }
            if (mUpdateCnt < 0)
            {
                return;
            }
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            if (mAlpha == 1)
            {
                if (mUpdateCnt == 0xf)
                {
                    aBoard.mShakeCount = 10;
                    aBoard.mSoundMgr.AddSound(Res.SOUND_FEVERHIT);
                }
                else if (mUpdateCnt == 0)
                {
                    aBoard.mSoundMgr.AddSound(Res.SOUND_WHISTLE);
                }
            }
            else if (mAlpha > 1 && mUpdateCnt == 0x14)
            {
                aBoard.mSoundMgr.AddSound(Res.SOUND_WOOSH);
            }
            GetExtremeFeverPos(out float aPosX, out float aPosY);
            bool aBallBehind = false;
            if (mUpdateCnt > 0x31)
            {
                bool zoomed = false;
                SexyTransform2D aZoom = new SexyTransform2D();
                if (GlobalStaticVars.gSexyAppBase.Is3DAccelerated() && aBoard.mZoom > 0.0f)
                {
                    zoomed = true;
                    float aScale = aBoard.mZoom * 1.0f + 1.0f;
                    int aH = SexyUtil.gPhysicalDisplayHeight;
                    aZoom.Translate((float)-(aBoard.mZoomX * aH / 600.0), (float)-(aBoard.mZoomY * aH / 600.0));
                    aZoom.Scale(aScale, aScale);
                    aZoom.Translate((float)(aBoard.mZoomX * aH / 600.0), (float)(aBoard.mZoomY * aH / 600.0));
                }
                int aBoardX = aBoard.mBoardRect.mX;
                int aBoardY = aBoard.mBoardRect.mY;
                TRect aRect = new TRect((int)(aPosX - 40 + aBoardX), (int)(aPosY - 40 + aBoardY), 0x50, 0x50);
                foreach (Ball aBall in aBoard.mBalls)
                {
                    SexyVector2 aPos = new SexyVector2(aBall.mX + aBoardX, aBall.mY + aBoardY);
                    if (zoomed)
                    {
                        aPos = aZoom * aPos;
                    }
                    if (aRect.Contains((int)aPos.x, (int)aPos.y))
                    {
                        aBallBehind = true;
                        break;
                    }
                }
            }
            if (GlobalStaticVars.gSexyAppBase.Is3DAccelerated())
            {
                if (mDuration - mUpdateCnt < 0x15)
                {
                    AddExtremeFeverParticles(TYPE_SMOKE, aPosX, aPosY, 100, 1);
                }
                else if (mUpdateCnt < 0x14 && mAlpha == 0)
                {
                    AddExtremeFeverParticles(TYPE_FLAME_TRAIL, aPosX, aPosY, 100, 8);
                }
                if (!aBallBehind && mDuration - mUpdateCnt > 0x27 && mUpdateCnt > 0xe)
                {
                    AddExtremeFeverParticles(TYPE_EMBER, aPosX, aPosY, 0x32, 1);
                }
            }
            if (mDuration - mUpdateCnt == 0x14 && mAlpha > 1)
            {
                aBoard.mSoundMgr.AddSound(Res.SOUND_WOOSH);
            }
            if (!aBallBehind)
            {
                if (mColor > 0 && mColor < 0xff)
                {
                    mColor += 5;
                    if (mColor > 0xff)
                    {
                        mColor = 0;
                    }
                }
            }
            else
            {
                if (mColor == 0)
                {
                    mColor = 0xff;
                }
                mColor -= 5;
                mColor = SexyUtil.ClampRange(mColor, 100, 0xff);
            }
        }

        public void UpdateHat()
        {
            if (mY > 630.0f)
            {
                mDuration = mUpdateCnt;
            }
        }

        /// <summary>Pulls the ball-in-hole effect towards the hole (mObj2).</summary>
        public void UpdateBallHole()
        {
            if (mObj1 == null || mObj2 == null)
            {
                return;
            }
            SexyVector2 aCenter = mObj2.GetCenter();
            SexyVector2 aPull = (aCenter - new SexyVector2(mX, mY)) * 0.2f;
            mVelX += aPull.x;
            mVelY += aPull.y;
            mVelX *= 0.7f;
            mVelY *= 0.7f;
            mX += mVelX;
            mY += mVelY;
        }

        /// <summary>Jimmy ducks away early when a ball comes near his corner.</summary>
        public void UpdateJimmy()
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            if (mUpdateCnt == 1)
            {
                if (mJimmyGame == JIMMY_EXTREME)
                {
                    // Port: the Heavy says his line instead (PeggleExtreme.exe 0x45b0dc: SOUND_GOODSHOT, interval 100).
                    if (sExtremeJimmySound == -2)
                    {
                        sExtremeJimmySound = aBoard.mApp.mSoundManager.LoadSound("sounds/goodshot_extreme.wav");
                    }
                    if (sExtremeJimmySound >= 0)
                    {
                        aBoard.mSoundMgr.AddSound(sExtremeJimmySound, 0.0f, 0, 0, 100, -1.0f);
                    }
                }
                else
                {
                    aBoard.mSoundMgr.AddSound(Res.SOUND_POWERUP_01_MULTIBALL);
                }
            }
            if (mUpdateCnt >= mDuration - 0x14)
            {
                return;
            }
            TRect aBoardRect = aBoard.mBoardRect;
            Image aJimmy = GetJimmyImage();
            // Port: Jimmy's corner (a ball in it sends him away) is in board units, which are 600-high; the image
            // is at the display's scale, so its size is scaled back as the PC's (already 800x600) needs no scaling.
            // Unscaled it was up to 28% too big (HD), so he left again as soon as he appeared.
            double aToBoard = 600.0 / SexyUtil.gPhysicalDisplayHeight;
            TRect aRect = new TRect(0, 0, (int)(aJimmy.mWidth * aToBoard) + 100, (int)(aJimmy.mHeight * aToBoard) + 0x32);
            if (mFlags != 0)
            {
                aRect.mX = aBoardRect.mWidth - aRect.mWidth;
            }
            aRect.mY = aBoardRect.mHeight - aRect.mHeight;
            foreach (Ball aBall in aBoard.mBalls)
            {
                if (aRect.Contains((int)aBall.mX, (int)aBall.mY))
                {
                    mUpdateCnt = mDuration - mUpdateCnt;
                    if (mUpdateCnt < mDuration - 0x14)
                    {
                        mUpdateCnt = mDuration - 0x14;
                    }
                    return;
                }
            }
        }

        /// <summary>Rotates the spin point around (mFlags, mAlpha), tightening over time; mVel holds the last step.</summary>
        public void UpdateStyleSpin()
        {
            if (PhysObj.GetBoard() == null)
            {
                return;
            }
            mX -= mVelX;
            mY -= mVelY;
            float anAngle = 0.295f;
            if (mUpdateCnt > 0x12)
            {
                anAngle = 0.295f / (mUpdateCnt - 0x12);
            }
            float aDX = mX - mFlags;
            float aDY = mY - mAlpha;
            mVelX = mX;
            mVelY = mY;
            mX = (float)(mFlags + aDX * Math.Cos(anAngle) + aDY * Math.Sin(anAngle));
            mY = (float)((mAlpha - aDX * Math.Sin(anAngle)) + aDY * Math.Cos(anAngle));
            mVelX -= mX;
            mVelY -= mY;
        }

        /// <summary>Spooky balls shed sparkles; an open-ended trail (duration 10000) ends once none are left.</summary>
        public void UpdateSpookyTrail()
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            bool anyBall = false;
            foreach (Ball aBall in aBoard.mBalls)
            {
                if (aBall.mFlag11C || aBall.mFlagFE)
                {
                    Effect anEffect = aBoard.mEffectMgr.AddEffect(TYPE_SPOOKY_SPARKLE, (aBall.mX - 6.0f) + SexyUtil.Rand() % 0xc,
                        (aBall.mY - 6.0f) + SexyUtil.Rand() % 0xc, 0, true);
                    anEffect.mVelX = (SexyUtil.Rand() % 5 - 2) / 10.0f;
                    anEffect.mVelY = (SexyUtil.Rand() % 5 - 2) / 10.0f;
                    anEffect.mDuration = 0x32;
                    anEffect.mFlags = SexyUtil.Rand() % 2;
                    anyBall = true;
                }
            }
            if (!anyBall && mDuration == 10000)
            {
                mDuration = mUpdateCnt + 1;
            }
        }

        public void UpdateFeverScoreText()
        {
        }

        public void UpdateNukeStreak()
        {
        }
    }
}
