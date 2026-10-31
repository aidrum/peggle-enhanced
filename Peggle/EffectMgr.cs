using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Owns the board's effects (Sexy::EffectMgr) in five lists by draw priority (-1..3), and
    /// builds the composite effects (blasts, fever, Jimmy, spooky balls...).
    /// </summary>
    public class EffectMgr
    {
        public List<Effect>[] mEffects = new List<Effect>[5];    // 0x04

        private static readonly float[] BUBBLE_SPEEDS = { 0.67f, 0.83f, 1.0f, 1.07f, 0.76f, 0.93f };

        private static readonly int[] ULTRA_EXTREME_FEVER_KERNING = { 0, 0, -2, -2, 10, -2, -4, 2, -4, -7, -4, 3, 4, -2, 0, -2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        private static readonly int[] EXTREME_FEVER_KERNING = { -2, -4, 2, -4, -7, -4, 3, 4, -2, 0, -2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public EffectMgr()
        {
            for (int i = 0; i < 5; i++)
            {
                mEffects[i] = new List<Effect>();
            }
        }

        public void SyncState(DataSync theSync)
        {
            if (theSync.GetVersion() < 0x35)
            {
                theSync.SyncList(mEffects[0], (ds, e) => { e.SyncState(ds); return e; }, () => new Effect());
                Clear();
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    theSync.SyncList(mEffects[i], (ds, e) => { e.SyncState(ds); return e; }, () => new Effect());
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < 5; i++)
            {
                mEffects[i].Clear();
            }
        }

        public int PriorityToList(int thePriority)
        {
            return thePriority + 1;
        }

        public int ClampPriority(int thePriority)
        {
            return SexyUtil.ClampRange(thePriority, -1, 3);
        }

        /// <summary>Moves theEffect to another priority list; returns the effect in its new place.</summary>
        public Effect SetPriority(Effect theEffect, int thePriority)
        {
            thePriority = ClampPriority(thePriority);
            int aCurrent = ClampPriority(theEffect.GetPriority());
            if (aCurrent == thePriority)
            {
                return theEffect;
            }
            List<Effect> aFrom = mEffects[PriorityToList(aCurrent)];
            List<Effect> aTo = mEffects[PriorityToList(thePriority)];
            Effect aMoved = new Effect(theEffect);
            aTo.Add(aMoved);
            aMoved.SetPriorityPrv(thePriority);
            aFrom.Remove(theEffect);
            return aMoved;
        }

        /// <summary>Adds an effect (duration 100) at the back of its priority list, or the front when atFront.</summary>
        public Effect AddEffect(int theType, float theX, float theY, int thePriority = 0, bool atFront = false)
        {
            thePriority = ClampPriority(thePriority);
            List<Effect> aList = mEffects[PriorityToList(thePriority)];
            Effect anEffect = new Effect();
            if (atFront)
            {
                aList.Insert(0, anEffect);
            }
            else
            {
                aList.Add(anEffect);
            }
            anEffect.mType = theType;
            anEffect.mX = theX;
            anEffect.mY = theY;
            anEffect.SetPriorityPrv(thePriority);
            anEffect.mDuration = 100;
            return anEffect;
        }

        /// <summary>A point on ring theRing of the space-blast crack pattern at spoke theIndex, rippling with theTime.</summary>
        public SexyVector2 SetCrackPoint(int theIndex, int theRing, int theTime)
        {
            float aPhase = theTime * 3.1415927f / 20.0f + SexyUtil.Rand() % 4 / 100.0f;
            float aRadius = 0.065f * theRing;
            float anOffset = theRing % 2 == 1 ? -0.5f : 0.0f;
            float anAngle = aPhase + (anOffset + theIndex) * 3.1415927f * 0.025f;
            return new SexyVector2((float)(aRadius * Math.Sin(anAngle)), (float)(aRadius * Math.Cos(anAngle)));
        }

        /// <summary>
        /// A lightning bolt from theFrom to theTo (or the lightning rod when null), with optional
        /// crackles along it; fiery bolts are orange and throw embers at the target.
        /// </summary>
        public Effect AddLightningBolt(PhysObj theSource, PhysObj theFrom, PhysObj theTo, int theDelay, bool isFire, bool withSparks)
        {
            float aToX;
            float aToY;
            if (theTo == null)
            {
                LightningOrb anOrb = PhysObj.GetBoard().mLogicMgr.mLightningOrb;
                aToX = anOrb.mX;
                aToY = anOrb.mY;
            }
            else
            {
                aToX = theTo.GetXPos();
                aToY = theTo.GetYPos();
            }
            GlobalStaticVars.gSexyAppBase.Is3DAccelerated();
            if (withSparks)
            {
                for (int i = 0; i < 10; i++)
                {
                    float t = Math.Abs(SexyUtil.GetRandFloat());
                    float aFromX = theFrom.GetXPos();
                    float aFromY = theFrom.GetYPos();
                    float anAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x168);
                    float aCos = (float)Math.Cos(anAngle);
                    float aSin = (float)Math.Sin(anAngle);
                    float aSpeed = SexyUtil.Rand() % 1000 / 1000.0f * 1.5f + 0.4f;
                    Effect aCrackle = AddEffect(Effect.TYPE_LIGHTNING_CRACKLE, t * aFromX + (1.0f - t) * aToX + aCos * 20.0f,
                        t * aFromY + (1.0f - t) * aToY + aSin * 20.0f, 0, false);
                    aCrackle.mUpdateCnt = isFire ? 0 : -theDelay;
                    aCrackle.mColor = isFire ? 0xff6622 : 0x8888ff;
                    aCrackle.mVelX = aCos * aSpeed;
                    aCrackle.mVelY = (float)(aSin * aSpeed + -0.1);
                    aCrackle.mDuration = 0x82;
                    aCrackle.mGravity = 0.001f;
                }
            }
            Effect aBolt = AddEffect(Effect.TYPE_LIGHTNING_BOLT, theFrom.GetXPos(), theFrom.GetYPos(), 0, false);
            aBolt.mUpdateCnt = 0;
            aBolt.mVelX = 0;
            aBolt.mVelY = 0;
            aBolt.mColor = isFire ? 0xff6622 : unchecked((int)0xff6666ff);
            aBolt.mDuration = isFire ? 0x1e : 0x2d;
            aBolt.mGravity = 0;
            aBolt.mFlags = 1;
            aBolt.mAlpha = theTo == null ? 1 : 0;
            aBolt.mObj1 = theFrom;
            aBolt.mObj2 = theTo;
            aBolt.mObj3 = theSource;
            if (isFire)
            {
                for (int i = 0; i < 0x14; i++)
                {
                    float anAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x168);
                    float aCos = (float)Math.Cos(anAngle);
                    float aSin = (float)Math.Sin(anAngle);
                    float aDist = SexyUtil.Rand() % 10000;
                    Effect anEmber = AddEffect(Effect.TYPE_EMBER, aToX + aDist * 0.001f * aCos, aToY + aDist * 0.001f * aSin, 0, false);
                    anEmber.mVelY = (-0.0f - SexyUtil.Rand() % 1000 / 1000.0f) * 1.0f;
                    anEmber.mDuration = 0x23;
                    anEmber.mUpdateCnt = -(SexyUtil.Rand() % 0x23);
                }
            }
            return aBolt;
        }

        public Effect AddRibbon(float theX, float theY, int theFlags, int theDelay)
        {
            Effect anEffect = AddEffect(Effect.TYPE_RIBBON, theX, theY, 0, false);
            anEffect.mDuration = 0;
            anEffect.mUpdateCnt = -theDelay;
            anEffect.mFlags = theFlags;
            anEffect.mAlpha = 0;
            return anEffect;
        }

        public Effect AddMasterBadge(float theX, float theY, int theDelay)
        {
            Effect anEffect = AddEffect(Effect.TYPE_MASTER_BADGE, theX, theY, 0, false);
            anEffect.mDuration = 0;
            anEffect.mUpdateCnt = -theDelay;
            return anEffect;
        }

        public Effect AddCoinFlip(float theX, float theY, bool theSide)
        {
            Effect anEffect = AddEffect(Effect.TYPE_COIN_FLIP, theX, theY, 0, false);
            anEffect.mDuration = 0xfa;
            anEffect.mValue = SexyUtil.Rand() % 0x168;
            anEffect.mColor = theSide ? 1 : 0;
            return anEffect;
        }

        public void AddFlowerSparkles(float theX, float theY)
        {
            for (int i = 0; i < 0x32; i++)
            {
                float anAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x168);
                float aCos = (float)Math.Cos(anAngle);
                float aSin = (float)Math.Sin(anAngle);
                float aDist = SexyUtil.Rand() % 0x14;
                float aSpeed = SexyUtil.Rand() % 1000 / 1000.0f * 0.5f + 0.25f;
                Effect anEffect = AddEffect(Effect.TYPE_FLOWER_SPARKLE, theX + aCos * aDist, theY + aDist * aSin, 1, false);
                anEffect.mDuration = SexyUtil.Rand() % 0x1e + 0x32;
                anEffect.mVelX = aCos * aSpeed;
                anEffect.mVelY = aSin * aSpeed + -0.15f;
                anEffect.mGravity = Ball.mGravity * 0.15f;
            }
        }

        private static void ParseKerning(string theList, int[] theKerning)
        {
            int anIdx = 0;
            foreach (string aToken in theList.Split(','))
            {
                string aTrimmed = aToken.Trim();
                if (aTrimmed.Length != 0 && anIdx < theKerning.Length)
                {
                    int.TryParse(aTrimmed, out int aValue);
                    theKerning[anIdx++] = aValue;
                }
            }
        }

        /// <summary>
        /// Spells "ultra extreme fever" (or "extreme fever") as one effect per letter, each line
        /// sliding in from alternate sides after theDelay updates.
        /// </summary>
        public void AddExtremeFever(float theX, float theY, bool isUltra, int theDelay)
        {
            if (PhysObj.GetBoard() == null)
            {
                return;
            }
            int aLetterWidth = 0x4e;
#if PEGGLE_IPHONE
            int aLineHeight = (int)(52800.0 / SexyUtil.gPhysicalDisplayHeight);
#else
            int aLineHeight = (int)(Layout.EFFECT_MGR_ADD_EXTREMEFEVER_AHEIGHT * 600.0 / SexyUtil.gPhysicalDisplayHeight);
#endif
            int aDuration = 300;
            theY += 20.0f;
            string aLetters = SexyAppBase.Loc("ultraexmfv");
            string[] aWords = { "", "", "" };
            string aDelimiter = SexyAppBase.Loc("UEF_STRING_DELIMITER_ID");
            if (aDelimiter == "UEF_STRING_DELIMITER_ID")
            {
                aDelimiter = "\n";
            }
            string aText = SexyAppBase.Loc("ultra\nextreme\nfever");
            string aKerningList = SexyAppBase.Loc("ULTRA_EXTREME_FEVER_KERNING");
            if (!isUltra)
            {
                aText = SexyAppBase.Loc("extreme\nfever");
                aKerningList = SexyAppBase.Loc("EXTREME_FEVER_KERNING");
            }
            int aWordIdx = isUltra ? 0 : 1;
            foreach (string aToken in aText.Split(aDelimiter.ToCharArray()))
            {
                if (aWordIdx == 3)
                {
                    break;
                }
                string aTrimmed = aToken.Trim();
                if (aTrimmed.Length != 0)
                {
                    aWords[aWordIdx++] = aTrimmed;
                }
            }
            int[] aKerning = (int[])(isUltra ? ULTRA_EXTREME_FEVER_KERNING : EXTREME_FEVER_KERNING).Clone();
            ParseKerning(aKerningList, aKerning);

            int[] aSpacing = { -3, 0, 3 };
            int[] aLineOffsetX = { 0x14, 0, -0x16 };
            int aY = (int)theY;
            int aTimeOffset = 0;
            int aKernIdx = 0;
            for (int aLine = isUltra ? 0 : 1; aLine < 3; aLine++)
            {
                string aWord = aWords[aLine];
                int aX = (int)((theX - aWord.Length * aLetterWidth / 2) + aLetterWidth / 2 + aLineOffsetX[aLine]);
                for (int i = 0; i < aWord.Length; i++)
                {
                    Effect anEffect = AddEffect(Effect.TYPE_EXTREME_FEVER, aX, (float)((double)(SexyUtil.gPhysicalDisplayHeight * aY) / 600.0), 3, false);
                    anEffect.mFlags = aLetters.IndexOf(aWord[i]);
                    anEffect.mDuration = aDuration - aTimeOffset;
                    anEffect.mNoSlowMo = true;
                    anEffect.mUpdateCnt = -theDelay - aTimeOffset;
                    if (aLine < 1)
                    {
                        anEffect.mAlpha = 1;
                        aTimeOffset += 0x14;
                    }
                    else
                    {
                        anEffect.mValue = aLine == 1 ? -500 : 500;
                        anEffect.mAngle = aLine == 1 ? 0x14 : 10;
                    }
                    if (aLine == 1 && i == 0)
                    {
                        anEffect.mAlpha = 2;
                    }
                    else if (aLine == 2 && i == aWord.Length - 1)
                    {
                        anEffect.mAlpha = 3;
                    }
                    int aKern = i + aKernIdx < aKerning.Length ? aKerning[i + aKernIdx] : 0;
                    anEffect.mX += aKern;
                    aX = aSpacing[aLine] + aLetterWidth + aKern + aX;
                }
                aKernIdx += aWord.Length;
                aTimeOffset += 0xf;
                aY += aLineHeight;
            }
        }

        public Effect AddBubble(float theX, float theY)
        {
            Effect anEffect = AddEffect(Effect.TYPE_BUBBLE, theX, theY, 0, false);
            anEffect.mVelY = -0.0f - BUBBLE_SPEEDS[SexyUtil.Rand() % 6];
            anEffect.mValue = SexyUtil.Rand() % 4;
            if (anEffect.mValue == 3)
            {
                anEffect.mValue = SexyUtil.Rand() % 4;
            }
            anEffect.mDuration = 0;
            return anEffect;
        }

        public void AddFeverScoreText()
        {
            Effect anEffect = AddEffect(Effect.TYPE_FEVER_SCORE_TEXT, 400.0f, 300.0f, 0, false);
            anEffect.mDuration = 100000;
            anEffect.mFlags = 3;
        }

        private void AddFeverStreaks(Board theBoard, float theX, float theY, int theCount, float theSpeedRange, float theSpeedMin)
        {
            for (int i = 0; i < theCount; i++)
            {
                float anAngle = SexyUtil.Rand() % 1000 * 3.1415927f / 500.0f;
                float aSpeed = SexyUtil.Rand() % 1000 * theSpeedRange / 1000.0f + theSpeedMin;
                float aDist = SexyUtil.Rand() % 10;
                Effect anEffect = theBoard.mEffectMgr.AddEffect(Effect.TYPE_STREAK, (float)(theX + aDist * Math.Sin(anAngle)),
                    (float)(theY + aDist * Math.Cos(anAngle)), 0, false);
                anEffect.mColor = GlobalStaticVars.gSexyAppBase.HSLToRGB(SexyUtil.Rand() % 0xff, 0xff, 0x40);
                anEffect.mFlags = 1;
                anEffect.mFlags |= 2;
                anEffect.mVelX = (float)(aSpeed * Math.Sin(anAngle));
                anEffect.mVelY = (float)(aSpeed * Math.Cos(anAngle) + -1.0);
                anEffect.mDuration = 0x2d;
                anEffect.mGravity = Ball.mGravity * 1.0f;
            }
        }

        private void AddFeverCrackles(float theX, float theY, int theCount, float theSpeedRange)
        {
            for (int i = 0; i < theCount; i++)
            {
                float anAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x168);
                float aCos = (float)Math.Cos(anAngle);
                float aSin = (float)Math.Sin(anAngle);
                float aDist = SexyUtil.Rand() % 0x14;
                float aSpeed = SexyUtil.Rand() % 1000 / 1000.0f * theSpeedRange + 0.1f;
                Effect anEffect = AddEffect(Effect.TYPE_FEVER_CRACKLE, theX + aCos * aDist, theY + aDist * aSin, 0, false);
                anEffect.mUpdateCnt = 0;
                anEffect.mVelX = aCos * aSpeed;
                anEffect.mVelY = aSin * aSpeed + -1.0f;
                anEffect.mFlags = SexyUtil.Rand() % 0xff;
                anEffect.mAlpha = SexyUtil.Rand() % 0x2d + 3;
                anEffect.mDuration = anEffect.mAlpha + 0x1e;
                anEffect.mGravity = Ball.mGravity;
            }
        }

        /// <summary>The big rainbow burst when the last orange peg is hit.</summary>
        public void AddFeverBlast(float theX, float theY)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            aBoard.mShakeCount = 0x14;
            AddFeverStreaks(aBoard, theX, theY, 0x50, 2.5f, 1.5f);
            AddFeverCrackles(theX, theY, 0x96, 4.0f);
        }

        public void AddFeverPointsBlast(float theX, float theY)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            aBoard.mShakeCount = 0;
            AddFeverStreaks(aBoard, theX, theY, Layout.EFFECT_MGR_NUM_STREAKS, 1.25f, 0.75f);
            AddFeverCrackles(theX, theY, Layout.EFFECT_MGR_NUM_CRACKLES, 2.0f);
        }

        /// <summary>Muzzle flash, smoke and flecks when the launcher fires at theAngle.</summary>
        public void AddCannonBlast(float theX, float theY, float theAngle)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            float aY = theY + 4.0f;
            for (int i = 0; i < 0x14; i++)
            {
                float aSpeed = SexyUtil.Rand() % 1000 * 1.5f / 1000.0f + 0.3f;
                float anAngle = theAngle + 1.5707964f + (1.0f - aSpeed * 0.2f) * 0.5f * (1.0f - SexyUtil.Rand() % 1000 / 500.0f);
                float aDist = SexyUtil.Rand() % 10;
                double aSin = Math.Sin(anAngle);
                double aJitterX = SexyUtil.Rand() % 10;
                double aCos = Math.Cos(anAngle);
                double aJitterY = SexyUtil.Rand() % 10;
                Effect anEffect = aBoard.mEffectMgr.AddEffect(Effect.TYPE_STREAK, (float)((theX + aDist * aSin + aJitterX) - 5.0),
                    (float)((aY + aDist * aCos + aJitterY) - 5.0), 2, false);
                anEffect.mColor = 0x994411;
                anEffect.mFlags = 1;
                anEffect.mFlags |= 2;
                anEffect.mVelX = (float)(aSpeed * Math.Sin(anAngle));
                anEffect.mVelY = (float)(aSpeed * Math.Cos(anAngle) + -0.0);
                anEffect.mDuration = SexyUtil.Rand() % 0x1e + 5;
                anEffect.mGravity = Ball.mGravity * 0.1f;
            }
            for (int i = 0; i < 0x11; i++)
            {
                float anAngle = theAngle + 1.5707964f + (1.0f - SexyUtil.Rand() % 1000 / 500.0f) * 0.7f;
                float aSin = (float)Math.Sin(anAngle);
                float aCos = (float)Math.Cos(anAngle);
                float aDist = SexyUtil.Rand() % 0x14 + -10;
                float aSpeed = SexyUtil.Rand() % 1000 / 1000.0f * 1.5f + 0.3f;
                float aJitterX = SexyUtil.Rand() % 10;
                float aJitterY = SexyUtil.Rand() % 10;
                Effect anEffect = AddEffect(Effect.TYPE_SMOKE, (theX + aSin * aDist + aJitterX) - 5.0f, (aY + aCos * aDist + aJitterY) - 5.0f, 2, false);
                anEffect.mFlags = 1;
                anEffect.mColor = 0xaaaaaa;
                anEffect.mVelX = aSin * aSpeed;
                anEffect.mVelY = (float)(aCos * aSpeed + -0.2);
                anEffect.mDuration = (int)((SexyUtil.Rand() % 10 + 0x1e) - aSpeed * 8.0f);
                anEffect.mGravity = Ball.mGravity * 0.1f;
            }
            for (int i = 0; i < 0x11; i++)
            {
                float anAngle = theAngle + 1.5707964f + (SexyUtil.Rand() % 1000 / -500.0f + 1.0f) * 0.4f;
                float aSin = (float)Math.Sin(anAngle);
                float aCos = (float)Math.Cos(anAngle);
                float aDist = SexyUtil.Rand() % 0x14 + -10;
                float aRand = SexyUtil.Rand() % 1000 / 1000.0f;
                float aSpeed = aRand + aRand + 0.1f;
                float aX = theX + aSin * aDist + SexyUtil.Rand() % 10 - 5.0f;
                float aFleckY = aY + aCos * aDist + SexyUtil.Rand() % 10 - 5.0f;
                Effect anEffect = AddEffect(Effect.TYPE_FLECK, aX, aFleckY, 3, false);
                anEffect.mFlags = SexyUtil.Rand() % 2;
                anEffect.mAlpha = SexyUtil.Rand() % 0x10;
                int aShade = SexyUtil.Rand() % 3;
                if (aShade == 1)
                {
                    anEffect.mColor = 0x808080;
                }
                else if (aShade == 0)
                {
                    anEffect.mColor = 0x404040;
                }
                else
                {
                    anEffect.mColor = 0xf0f0f0;
                }
                anEffect.mVelX = aSin * aSpeed;
                anEffect.mVelY = (float)(aCos * aSpeed + -0.2);
                anEffect.mDuration = (int)((SexyUtil.Rand() % 10 + 0x1e) - aSpeed * 4.0f);
                anEffect.mGravity = Ball.mGravity * 0.1f;
            }
        }

        /// <summary>Sparks converging on (theX, theY) when Master Hu's zen shot triggers.</summary>
        public void AddZenPowerup(float theX, float theY)
        {
            if (PhysObj.GetBoard() == null)
            {
                return;
            }
            for (int i = 0; i < 0x32; i++)
            {
                float anAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x168);
                float aCos = (float)Math.Cos(anAngle);
                float aSin = (float)Math.Sin(anAngle);
                float aDist = SexyUtil.Rand() % 0x14 + 0x96;
                float aSpeed = SexyUtil.Rand() % 1000 / 1000.0f * 1.0f + 8.5f;
                Effect anEffect = AddEffect(Effect.TYPE_FEVER_SPARK, theX + aCos * aDist, theY + aDist * aSin, 3, false);
                anEffect.mVelX = (-0.0f - aCos) * aSpeed;
                anEffect.mVelY = (-0.0f - aSin) * aSpeed;
                anEffect.mGravity = Ball.mGravity * 0.0f;
                anEffect.mDuration = 0x7d;
                anEffect.mAlpha = SexyUtil.Rand() % 2 + 1;
                anEffect.mColor = 0xffff88;
                anEffect.mUpdateCnt = -1;
                anEffect.mValue = 1;
            }
        }

        /// <summary>Kat Tut's space blast: cracks, burst, bubble, halo and 25 stars, after theDelay.</summary>
        public void AddSpaceBlast(float theX, float theY, int theDelay)
        {
            Effect anEffect = AddEffect(Effect.TYPE_SPACE_CRACKS, theX, theY, -1, true);
            anEffect.mUpdateCnt = -theDelay;
            anEffect.mDuration = 0x28;
            anEffect = AddEffect(Effect.TYPE_SPACE_BURST, theX, theY, 0, false);
            anEffect.mUpdateCnt = -theDelay;
            anEffect.mDuration = 0x1e;
            anEffect = AddEffect(Effect.TYPE_SPACE_BUBBLE, theX, theY, 0, false);
            anEffect.mUpdateCnt = -5 - theDelay;
            anEffect.mDuration = 0x28;
            anEffect = AddEffect(Effect.TYPE_SPACE_HALO, theX, theY, 0, false);
            anEffect.mUpdateCnt = -5 - theDelay;
            anEffect.mDuration = 0x28;
            for (int i = 0; i < 0x19; i++)
            {
                float anAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x168);
                float aCos = (float)Math.Cos(anAngle);
                float aSin = (float)Math.Sin(anAngle);
                float aDist = SexyUtil.Rand() % 0x3c;
                float aSpeed = SexyUtil.Rand() % 1000 / 1000.0f * 2.5f + 0.75f;
                Effect aStar = AddEffect(Effect.TYPE_SPACE_STAR, theX + aCos * aDist, theY + aDist * aSin, 0, false);
                aStar.mUpdateCnt = -theDelay;
                aStar.mDuration = 0x3c;
                aStar.mVelX = aCos * aSpeed;
                aStar.mVelY = aSin * aSpeed + -0.25f;
                aStar.mGravity = Ball.mGravity * 0.15f;
            }
        }

        public Effect AddRay(float theX, float theY)
        {
            Effect anEffect = AddEffect(Effect.TYPE_FEVER_RAY, theX, theY, 1, false);
            anEffect.mDuration = 200;
            anEffect.mValue = SexyUtil.Rand() % 100 + 0x16a;
            anEffect.mAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x15 + -10);
            anEffect.mAngleVel = anEffect.mAngle * 0.01f;
            return anEffect;
        }

        private static readonly int[][] FEVER_HOLE_STAR_COLORS =
        {
            new[] { 0x66ff, 0x4444ff, 0x8888ff, 0xff00, 0xbbff00, 0x44ff, 0xff2200, 0xcc5515, 0x88aa30 },
            new[] { 0x66ff, 0x4444ff, 0x8888ff, 0xff00, 0xbbff00, 0x44ff, 0xff2200, 0xbb4415, 0x999930 }
        };

        /// <summary>Rays, stars and streaks bursting out of a fever bucket hole (theType 0..2 = size).</summary>
        public void AddFeverHoleEffect(float theX, float theY, int theType)
        {
            int aRays;
            int aRayDuration;
            float aDensity;
            int aRayLength;
            float aSpeedScale;
            int aMaxLife;
            if (theType == 1)
            {
                aRays = 8;
                aRayDuration = 0x78;
                aDensity = 0.8f;
                aRayLength = 0xf0;
                aSpeedScale = 0.8f;
                aMaxLife = 0x3c;
            }
            else if (theType == 0)
            {
                aRays = 4;
                aRayDuration = 100;
                aDensity = 0.5f;
                aRayLength = 0xa0;
                aSpeedScale = 0.7f;
                aMaxLife = 0x32;
            }
            else
            {
                aRays = 0xc;
                aRayDuration = 0x8c;
                aDensity = 1.1f;
                aRayLength = 300;
                aSpeedScale = 1.0f;
                aMaxLife = 0x46;
            }
            for (int i = 0; i < aRays; i++)
            {
                Effect aRay = AddRay(theX, theY - -20.0f);
                aRay.mUpdateCnt = -SexyUtil.Rand() % 5;
                aRay.mDuration = aRayDuration;
                aRay.mValue = SexyUtil.Rand() % 100 + aRayLength;
                aRay.mFlags = theType;
                if (aRay.mAngleVel > 0.001f || aRay.mAngleVel < -0.001f)
                {
                    aRay.mAngleVel *= 0.5f;
                }
            }
            for (int i = 0; i < (int)(aRayDuration * aDensity / 1.0f); i++)
            {
                int aDelay = SexyUtil.Rand() % (aRayDuration - 0x1e) + 0x1e;
                int aLife = (int)((aRayDuration - aDelay) / 1.2f);
                if (aLife <= 10)
                {
                    continue;
                }
                if (aLife > aMaxLife * 1.5f)
                {
                    aLife = (int)(aMaxLife * 1.5f);
                }
                Effect aStar = AddFeverHoleParticle(Effect.TYPE_FEVER_STAR, theX, theY, aDelay, aSpeedScale, 1.5f);
                aStar.mDuration = aLife;
                aStar.mFlags = 3 - theType;
                aStar.mColor = FEVER_HOLE_STAR_COLORS[0][SexyUtil.Rand() % 3 + theType * 3];
                if (SexyUtil.Rand() % 4 == 0)
                {
                    aStar.mColor = 0xffffff;
                }
                else
                {
                    aStar.mFlags = 0;
                }
            }
            for (int i = 0; i < (int)(aRayDuration * aDensity / 1.0f); i++)
            {
                int aDelay = SexyUtil.Rand() % (aRayDuration - 0x1e) + 0x1e;
                int aLife = (aRayDuration - aDelay) / 2;
                if (aLife <= 10)
                {
                    continue;
                }
                if (aMaxLife < aLife)
                {
                    aLife = aMaxLife;
                }
                Effect aStreak = AddFeverHoleParticle(Effect.TYPE_STREAK, theX, theY, aDelay, aSpeedScale, 1.0f);
                aStreak.mDuration = aLife;
                aStreak.mFlags = 3;
                aStreak.mColor = FEVER_HOLE_STAR_COLORS[1][SexyUtil.Rand() % 3 + theType * 3];
            }
        }

        private Effect AddFeverHoleParticle(int theType, float theX, float theY, int theDelay, float theSpeedScale, float theGravity)
        {
            float anAngle = SexyUtil.DegToRad((SexyUtil.Rand() % 1000 / 500.0f - 1.0f) * 15.0f + 90.0f);
            float aCos = (float)Math.Cos(anAngle);
            float aSin = (float)Math.Sin(anAngle);
            float aSpeed = theSpeedScale * 7.0f + (SexyUtil.Rand() % 1000 / 500.0f - 1.0f) * 1.0f;
            Effect anEffect = AddEffect(theType, theX + aCos * 60.0f, theY, 1, false);
            anEffect.mUpdateCnt = -theDelay;
            anEffect.mGravity = Ball.mGravity * theGravity;
            anEffect.mAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x168);
            anEffect.mAngleVel = SexyUtil.Rand() % 1000 / 1000.0f * 0.1f + 0.02f;
            anEffect.mVelX = aCos * aSpeed;
            anEffect.mVelY = (-0.0f - aSin) * aSpeed;
            if (anEffect.mVelX > 0.0f)
            {
                anEffect.mAngleVel *= -1.0f;
            }
            anEffect.mValue = SexyUtil.Rand() % 0x1e + 0x14;
            return anEffect;
        }

        public void AddFeverRainbow(int theDuration)
        {
            Effect anEffect = AddEffect(Effect.TYPE_FEVER_RAINBOW, 325.0f, 500.0f, 0, false);
            anEffect.mDuration = theDuration;
        }

        /// <summary>Ends the open-ended fever rainbow, sparks and score text over the next few seconds.</summary>
        public void FinishFeverRainbow()
        {
            int aRainbowTime = 100;
            int aSparkTime = 0x50;
            int aTextTime = 0x3c;
            for (int i = 0; i < 5; i++)
            {
                foreach (Effect anEffect in mEffects[i])
                {
                    if (anEffect.mType == Effect.TYPE_FEVER_RAINBOW)
                    {
                        anEffect.mDuration = anEffect.mUpdateCnt + aRainbowTime;
                    }
                    else if (anEffect.mType == Effect.TYPE_RAINBOW_SPARK)
                    {
                        anEffect.mDuration = anEffect.mUpdateCnt + aSparkTime;
                    }
                    else if (anEffect.mType == Effect.TYPE_FEVER_SCORE_TEXT)
                    {
                        anEffect.mDuration = anEffect.mUpdateCnt + aTextTime;
                    }
                }
            }
        }

        /// <summary>A beam from (theX, theY) to the pyramid bumper (image "bumperc").</summary>
        public void AddPyramid(float theX, float theY, bool unused)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            foreach (PhysObj anObj in aBoard.mPhysObjs)
            {
                if (!string.IsNullOrEmpty(anObj.mImageName) && anObj.mImageName == "bumperc")
                {
                    Effect anEffect = AddEffect(Effect.TYPE_PYRAMID2, theX, theY, 2, false);
                    anEffect.mDuration = 0x78;
                    anEffect.mObj1 = anObj;
                    return;
                }
            }
        }

        /// <summary>The glow and motes over the free-ball bucket, after theDelay.</summary>
        public void AddCatcherGlow(int theDelay, bool withOrbs)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            PhysObj aBucket = null;
            foreach (PhysObj anObj in aBoard.mPhysObjs)
            {
                aBucket = anObj;
                if (anObj.mShapeType == PhysObj.SHAPE_HOLE && anObj.mId == "freeball")
                {
                    break;
                }
            }
            if (aBucket == null)
            {
                return;
            }
            Effect aGlow = AddEffect(Effect.TYPE_CATCHER_GLOW, aBucket.GetXPos(), aBucket.GetYPos(), 0, false);
            aGlow.mUpdateCnt = -theDelay;
            aGlow.mDuration = 0x5a;
            aGlow.mObj1 = aBucket;
            if (withOrbs)
            {
                aGlow.mFlags = 1;
            }
            for (int i = 0; i < 0x14; i++)
            {
                int aDX = (int)((0.5f - SexyUtil.Rand() % 1000 / 1000.0f) * 60.0f);
                Effect aMote = AddEffect(Effect.TYPE_CATCHER_MOTE, aDX, 0x14 - SexyUtil.Rand() % 0x28, 0, false);
                aMote.mVelX = aDX * 0.01f;
                aMote.mVelY = (SexyUtil.Rand() % 0x32 + 0x32) * 0.01f;
                aMote.mUpdateCnt = -theDelay;
                aMote.mDuration = SexyUtil.Rand() % 0x32 + 0x46;
                aMote.mObj1 = aBucket;
                aMote.mFlags = SexyUtil.Rand() % 3 + 1;
                aMote.mAlpha = SexyUtil.Rand() % 100;
            }
        }

        /// <summary>Jimmy Lightning popping up in a bottom corner (behind and in front of the board).</summary>
        public void AddJimmy(bool onRight, int thePhrase, int theDelay)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return;
            }
            float aX = onRight ? aBoard.mBoardRect.mWidth + -0x28 : 0x28 - Res.IMAGE_JIMMY.mWidth;
            float aY = aBoard.mBoardRect.mHeight + -3;
            int aPhrase = thePhrase < 0 ? SexyUtil.Rand() : thePhrase;
            aPhrase %= 0xff;
            // Port addition: Peggle Extreme's levels have the Heavy, Peggle WoW's their own phrases.
            BonusPage aPage = aBoard.mApp.GetLevelBonusPage(aBoard.mLevelName);
            int aGame = aPage == null ? Effect.JIMMY_PEGGLE
                : aPage.mAdventureKey == StageMgr.ADVENTURE_EXTREME ? Effect.JIMMY_EXTREME
                : aPage.mAdventureKey == StageMgr.ADVENTURE_WOW ? Effect.JIMMY_WOW
                : Effect.JIMMY_PEGGLE;
            Effect anEffect = AddEffect(Effect.TYPE_JIMMY, aX, aY, -1, false);
            anEffect.mJimmyGame = aGame;
            anEffect.mFlags = onRight ? 1 : 0;
            anEffect.mDuration = 0x96;
            anEffect.mColor = aPhrase;
            anEffect.mUpdateCnt = -theDelay;
            anEffect.mNoSlowMo = true;
            anEffect = AddEffect(Effect.TYPE_JIMMY, aX, aY, 2, false);
            anEffect.mJimmyGame = aGame;
            anEffect.mFlags = onRight ? 1 : 0;
            anEffect.mDuration = 0x96;
            anEffect.mColor = aPhrase;
            anEffect.mUpdateCnt = -theDelay;
            anEffect.mNoSlowMo = true;
        }

        /// <summary>The poof and three ghosts of a spooky ball; withTrail adds the eyes/trail tracker.</summary>
        public void AddSpookyBall(float theX, float theY, bool withTrail)
        {
            for (int i = 0; i < 4; i++)
            {
                Effect aPoof = AddEffect(Effect.TYPE_SPOOKY_POOF, theX, theY, 0, false);
                switch (i)
                {
                    case 0: aPoof.mVelY = -0.3f; break;
                    case 1: aPoof.mVelY = 0.3f; break;
                    case 2: aPoof.mVelX = 0.3f; break;
                    case 3: aPoof.mVelX = -0.3f; break;
                }
                aPoof.mFlags = i;
                aPoof.mDuration = 0x28;
            }
            float aBaseAngle = SexyUtil.DegToRad(SexyUtil.Rand() % 0x168);
            for (int i = 0; i < 3; i++)
            {
                Effect aGhost = AddEffect(Effect.TYPE_SPOOKY_GHOST, theX, theY, 2, false);
                aGhost.mDuration = 100;
                aGhost.mAngle = aBaseAngle + i * 6.2831855f / 3.0f;
                aGhost.mUpdateCnt = -5;
                aGhost.mColor = 5;
            }
            if (withTrail)
            {
                Effect aTrail = AddEffect(Effect.TYPE_SPOOKY_EYES, theX, theY, 2, false);
                aTrail.mDuration = 10000;
            }
        }

        /// <summary>A ghost rising where a spooky ball wraps from the bottom to the top.</summary>
        public void AddSpookyBallWrap(float theX, float theY, float theVelX, float theVelY)
        {
            Effect aGhost = AddEffect(Effect.TYPE_SPOOKY_GHOST, theX, theY, 2, false);
            aGhost.mDuration = 0x50;
            aGhost.mAngle = 1.5707964f;
            aGhost.mFlags = theX < 325.0f ? 1 : 0;
        }

        /// <summary>Flashes the "lbump" overlay for a level bumper hit (one effect per bumper, restarted on re-hit).</summary>
        public Effect AddLevelBumper(PhysObj theBumper)
        {
            Board aBoard = PhysObj.GetBoard();
            if (aBoard == null)
            {
                return null;
            }
            int aPriority = 0;
            Effect anEffect = null;
            foreach (Effect anExisting in mEffects[PriorityToList(0)])
            {
                if (anExisting.mType == Effect.TYPE_LEVEL_BUMPER && anExisting.mObj1 == theBumper)
                {
                    anEffect = anExisting;
                    break;
                }
            }
            bool isNew = false;
            if (anEffect == null)
            {
                PhysObj anOverlay0 = null;
                PhysObj anOverlay1 = null;
                foreach (PhysObj anObj in aBoard.mPhysObjs)
                {
                    if (!string.IsNullOrEmpty(anObj.mId) && anObj.mId == "lbump")
                    {
                        if (anObj.mSubId == 0)
                        {
                            anOverlay0 = anObj;
                        }
                        else
                        {
                            anOverlay1 = anObj;
                        }
                    }
                }
                SexyVector2 aCenter = theBumper.GetCenter();
                anEffect = AddEffect(Effect.TYPE_LEVEL_BUMPER, aCenter.x, aCenter.y, aPriority, false);
                anEffect.mDuration = 0x28;
                anEffect.mObj1 = theBumper;
                anEffect.mObj2 = anOverlay0;
                anEffect.mObj3 = anOverlay1;
                anEffect.mFlags = theBumper.mSubId;
                isNew = true;
            }
            if (isNew || anEffect.mUpdateCnt > 0x23)
            {
                aBoard.mSoundMgr.AddSound(Res.GetSoundById(ResourceId.SOUND_RICHOCHET_ID));
            }
            anEffect.mUpdateCnt = 0;
            return anEffect;
        }

        public Effect GetEffectByType(int theType)
        {
            for (int i = 0; i < 5; i++)
            {
                foreach (Effect anEffect in mEffects[i])
                {
                    if (anEffect.mType == theType)
                    {
                        return anEffect;
                    }
                }
            }
            return null;
        }

        public void GetEffectsByType(List<Effect> theEffects, int theType)
        {
            for (int i = 0; i < 5; i++)
            {
                foreach (Effect anEffect in mEffects[i])
                {
                    if (anEffect.mType == theType)
                    {
                        theEffects.Add(anEffect);
                    }
                }
            }
        }

        public int EraseAllOfType(int theType)
        {
            int aCount = 0;
            for (int i = 0; i < 5; i++)
            {
                aCount += mEffects[i].RemoveAll(e => e.mType == theType);
            }
            return aCount;
        }

        /// <summary>Draws the effects of one priority (started ones only).</summary>
        public void Draw(Graphics g, int thePriority)
        {
            if (thePriority != ClampPriority(thePriority))
            {
                return;
            }
            List<Effect> aList = mEffects[PriorityToList(thePriority)];
            for (int i = 0; i < aList.Count; i++)
            {
                if (aList[i].mUpdateCnt > -1)
                {
                    aList[i].Draw(g);
                }
            }
        }

        /// <summary>
        /// aList.IndexOf(theEffect), checking theHint first: an effect's update usually leaves it where it
        /// was, and a full scan per effect made updating the thousands of fever trails quadratic.
        /// </summary>
        private static int FindEffect(List<Effect> aList, Effect theEffect, int theHint)
        {
            if (theHint < aList.Count && aList[theHint] == theEffect)
            {
                return theHint;
            }
            return aList.IndexOf(theEffect);
        }

        public void Update(bool slowMoTick)
        {
            for (int i = 0; i < 5; i++)
            {
                List<Effect> aList = mEffects[i];
                for (int j = 0; j < aList.Count;)
                {
                    Effect anEffect = aList[j];
                    if (!anEffect.Update(slowMoTick))
                    {
                        int anIdx = FindEffect(aList, anEffect, j);
                        if (anIdx >= 0)
                        {
                            aList.RemoveAt(anIdx);
                            if (anIdx < j)
                            {
                                j--;
                            }
                        }
                    }
                    else
                    {
                        j = FindEffect(aList, anEffect, j) + 1;
                    }
                }
            }
        }

        public void UpdateSlowMo(float theFraction)
        {
            for (int i = 0; i < 5; i++)
            {
                List<Effect> aList = mEffects[i];
                for (int j = 0; j < aList.Count;)
                {
                    Effect anEffect = aList[j];
                    if (!anEffect.UpdateSlowMo(theFraction))
                    {
                        int anIdx = FindEffect(aList, anEffect, j);
                        if (anIdx >= 0)
                        {
                            aList.RemoveAt(anIdx);
                            if (anIdx < j)
                            {
                                j--;
                            }
                        }
                    }
                    else
                    {
                        j = FindEffect(aList, anEffect, j) + 1;
                    }
                }
            }
        }
    }
}
