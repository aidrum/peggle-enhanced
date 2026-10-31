using System;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The Lucky Spin wheel (Sexy::SlotMachineDialog): a pointer spins over four sectors (triple
    /// score, wizard hat, random power-up, extra ball) and slows to a stop while a ring of 40 rim
    /// lights chases it; the winning sector blinks, then the result goes to LogicMgr.
    /// </summary>
    public class SlotMachineDialog : ThunderDialog
    {
#if PEGGLE_IPHONE
        // The iPhone's light positions (480x320 units, scaled by mLayoutScaleFactor).
        private static readonly int[] LIGHTS_X = { 65, 54, 45, 37, 28, 19, 14, 9, 5, 3, 2, 3, 5, 8, 13, 20, 28, 37, 46, 54,
            65, 75, 84, 92, 101, 109, 116, 121, 124, 128, 128, 127, 125, 121, 116, 109, 101, 94, 84, 75 };

        private static readonly int[] LIGHTS_Y = { 2, 2, 4, 8, 13, 19, 28, 36, 46, 55, 64, 74, 84, 92, 101, 109, 115, 121, 125, 127,
            128, 127, 124, 122, 116, 109, 102, 93, 84, 74, 64, 55, 46, 36, 28, 19, 13, 9, 5, 3 };
#else
        private static readonly int[] LIGHTS_X = { 168, 145, 122, 100, 80, 60, 46, 33, 25, 19, 17, 19, 25, 33, 46, 60, 79, 100, 122, 145,
            168, 192, 216, 237, 257, 277, 291, 303, 312, 318, 320, 318, 312, 303, 291, 276, 258, 237, 215, 192 };

        private static readonly int[] LIGHTS_Y = { 15, 17, 23, 31, 44, 58, 77, 97, 120, 142, 166, 189, 213, 235, 255, 274, 289, 302, 311, 317,
            319, 317, 312, 302, 290, 275, 256, 236, 214, 191, 167, 144, 120, 98, 78, 58, 44, 32, 22, 17 };
#endif

        /// <summary>mLayoutScaleFactor: 1 for the iPad (1024x768) art, 2 for the iPhone's 960x640.</summary>
        private static int LAYOUT_SCALE_FACTOR => SexyUtil.mLayoutScaleFactor;

        public ThunderballApp mApp;                               // 0x128

        public Board mBoard;                                      // 0x12c

        public int mCurLight;                                     // 0x130 (sector under the pointer)

        public int mDoneCount;                                    // 0x134

        public Ball mBall;                                        // 0x138

        public PhysObj mHole;                                     // 0x13c

        public float mAngle;                                      // 0x140

        public float mSpeed;                                      // 0x144

        public int mRimLight;                                     // 0x148

        public int[] mLights = new int[0x28];                     // 0x14c

        public int mDingCount;                                    // 0x1ec

        public int mRandomCel;                                    // 0x1f0

        public int mCoastTime;                                    // 0x1f4

        public int mStartDelay;                                   // 0x1f8

        public SlotMachineDialog(Board theBoard)
            : base(DialogId.DIALOG_SLOT_MACHINE, false, SexyAppBase.Loc("Lucky Spin!"), "", "", 0)
        {
            mBoard = theBoard;
            mApp = theBoard.mApp;
        }

        public void Init(Ball theBall, PhysObj theHole)
        {
            mBall = theBall;
            mHole = theHole;
            mDoneCount = 0;
            mWidth = Res.IMAGE_SPIN_SPINNER.mWidth;
            mHeight = Res.IMAGE_SPIN_SPINNER.mHeight;
            mRimLight = 0;
            mCurLight = 0;
            mRandomCel = 0;
            switch (mBoard.mLogicMgr.GetSlotMachinePowerup())
            {
            case 1: mRandomCel = 0; break;
            case 2: mRandomCel = 4; break;
            case 3: mRandomCel = 1; break;
            case 4: mRandomCel = 2; break;
            case 5: mRandomCel = 3; break;
            case 6: mRandomCel = 5; break;
            case 7: mRandomCel = 8; break;
            case 9: mRandomCel = 6; break;
            case 0xc: mRandomCel = 7; break;
            case 0xe: mRandomCel = 9; break;
            }
            mAngle = SexyUtil.DegToRad((float)(SexyUtil.Rand() % 0x168));
            mSpeed = 0.15f;
            mCoastTime = SexyUtil.Rand() % 100;
            for (int i = 0; i < 0x28; i++)
            {
                mLights[i] = 0;
            }
            mDingCount = 0;
            mStartDelay = 0x28;
            CalcCurLight();
        }

        /// <summary>Works out the sector under the pointer and lights the rim (or blinks the winning sector).</summary>
        public void CalcCurLight()
        {
            float[] aBounds = { SexyUtil.DegToRad(45.0f), SexyUtil.DegToRad(135.0f), SexyUtil.DegToRad(225.0f), SexyUtil.DegToRad(315.0f) };
            int i = 0;
            while (i < 4 && !(mAngle < aBounds[i]))
            {
                i++;
            }
            mCurLight = i % 4;
            if (mDoneCount == 0)
            {
                mRimLight = SexyUtil.AceMod((int)((mAngle - SexyUtil.DegToRad(-5.0f)) * 20.0f / 3.1415927f), 0x28);
                mLights[mRimLight] = 0x1e;
                return;
            }
            for (int j = 0; j < 0x28; j++)
            {
                mLights[j] = 0;
            }
            if (mDoneCount / 0x14 % 2 == 1)
            {
                if (mCurLight < 1)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        mLights[j] = 0x1e;
                    }
                    for (int j = 0x23; j < 0x28; j++)
                    {
                        mLights[j] = 0x1e;
                    }
                }
                else
                {
                    for (int j = mCurLight * 10 - 5; j <= mCurLight * 10 + 5; j++)
                    {
                        mLights[j] = 0x1e;
                    }
                }
            }
        }

        public void SyncState(DataSync theSync)
        {
            theSync.SyncLong(ref mCurLight);
            theSync.SyncLong(ref mDoneCount);
            mBall = (Ball)PhysObj.SyncPhysObj(theSync, mBall);
            mHole = PhysObj.SyncPhysObj(theSync, mHole);
            theSync.SyncFloat(ref mAngle);
            theSync.SyncFloat(ref mSpeed);
            theSync.SyncLong(ref mRimLight);
            for (int i = 0; i < 0x28; i++)
            {
                theSync.SyncLong(ref mLights[i]);
            }
            theSync.SyncLong(ref mDingCount);
            theSync.SyncLong(ref mRandomCel);
            theSync.SyncLong(ref mCoastTime);
            theSync.SyncLong(ref mStartDelay);
            theSync.SyncLong(ref mScrollTargetY);
            theSync.SyncLong(ref mScrollDir);
            theSync.SyncLong(ref mX);
            theSync.SyncLong(ref mY);
            theSync.SyncLong(ref mWidth);
            theSync.SyncLong(ref mHeight);
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return Res.IMAGE_SPIN_SPINNER.mHeight;
        }

        public override void Draw(Graphics g)
        {
            if (mBoard.mPauseCount >= 1)
            {
                return;
            }
            g.DrawImage(Res.IMAGE_SPIN_SPINNER, 0, 0);
            int aLit = mCurLight;
            if ((mDoneCount > 0 && mDoneCount / 0x14 % 2 == 0) || mStartDelay > 0)
            {
                aLit = -1;
            }
            switch (aLit)
            {
            case 0:
                g.DrawImage(Res.IMAGE_SPIN_TRIPLEON, SLOT_MACHINE_SPIN_POS1_X, SLOT_MACHINE_SPIN_POS1_Y);
                break;
            case 1:
                g.DrawImage(Res.IMAGE_SPIN_HATON, SLOT_MACHINE_SPIN_POS2_X, SLOT_MACHINE_SPIN_POS2_Y);
                break;
            case 2:
                g.DrawImage(Res.IMAGE_SPIN_RANDOMON, SLOT_MACHINE_SPIN_POS3_X, SLOT_MACHINE_SPIN_POS3_Y);
                break;
            case 3:
                g.DrawImage(Res.IMAGE_SPIN_EXTRAON, SLOT_MACHINE_SPIN_POS4_X, SLOT_MACHINE_SPIN_POS4_Y);
                break;
            }
            Image aRandomText = aLit == 2 ? Res.IMAGE_SPIN_RANDOMTEXTON : Res.IMAGE_SPIN_RANDOMTEXT;
            g.DrawImageCel(aRandomText, SLOT_MACHINE_RAND_IMG_X, SLOT_MACHINE_RAND_IMG_Y, mRandomCel);
            g.SetColorizeImages(true);
            for (int i = 0; i < 0x28; i++)
            {
                if (mLights[i] == 0)
                {
                    continue;
                }
                g.SetColor(SexyUtil.MakeColor(0xffffff, mLights[i] * 0xff / 0x1e));
                if (i == 5 || i == 0xf || i == 0x19 || i == 0x23)
                {
                    // The four sector boundaries use the long light, flipped to suit each side.
                    bool aMirror = i == 0x19 || i == 0x23;
                    int aCel = (i == 0xf || i == 0x19) ? 1 : 0;
#if PEGGLE_IPHONE
                    g.DrawImageMirror(Res.IMAGE_SPIN_RIMLIGHT2, LIGHTS_X[i] * LAYOUT_SCALE_FACTOR,
                        LIGHTS_Y[i] * LAYOUT_SCALE_FACTOR, Res.IMAGE_SPIN_RIMLIGHT2.GetCelRect(aCel), aMirror);
#else
                    g.DrawImageMirror(Res.IMAGE_SPIN_RIMLIGHT2, LIGHTS_X[i] * LAYOUT_SCALE_FACTOR + IPAD_LIGHTS_OFFSET,
                        LIGHTS_Y[i] * LAYOUT_SCALE_FACTOR + IPAD_LIGHTS_OFFSET, Res.IMAGE_SPIN_RIMLIGHT2.GetCelRect(aCel), aMirror);
#endif
                }
                else
                {
#if PEGGLE_IPHONE
                    g.DrawImage(Res.IMAGE_SPIN_RIMLIGHT, LIGHTS_X[i] * LAYOUT_SCALE_FACTOR, LIGHTS_Y[i] * LAYOUT_SCALE_FACTOR);
#else
                    g.DrawImage(Res.IMAGE_SPIN_RIMLIGHT, LIGHTS_X[i] * LAYOUT_SCALE_FACTOR - 0xd, LIGHTS_Y[i] * LAYOUT_SCALE_FACTOR - 0xd);
#endif
                }
            }
            g.SetColorizeImages(false);
            g.DrawImageRotatedF(Res.IMAGE_SPIN_ARROW, (float)SLOT_MACHINE_ARROW_X, (float)SLOT_MACHINE_ARROW_Y, (double)mAngle,
                (float)SLOT_MACHINE_ARROW_ROT_X, (float)SLOT_MACHINE_ARROW_ROT_Y);
        }

        /// <summary>A tap while spinning skips straight to the result; while paused it unpauses.</summary>
        public override void MouseDown(int x, int y, int theClickCount)
        {
            if (mBoard.mPauseCount != 0)
            {
                mBoard.Pause(false);
                return;
            }
            if (mDoneCount == 0 && mSpeed > 0.0f)
            {
                while (mSpeed > 0.0f)
                {
                    UpdatePointer(false);
                }
                for (int i = 0; i < 0x28; i++)
                {
                    mLights[i] = 0;
                }
            }
        }

        /// <summary>Turns the pointer, dings on each sector change and slows it down; reports the result when it stops.</summary>
        public void UpdatePointer(bool playSounds)
        {
            int anOldRim = mRimLight;
            bool aDelaying = mStartDelay >= 1;
            if (!aDelaying)
            {
                mAngle -= mSpeed;
            }
            else
            {
                mStartDelay--;
                mAngle -= 0.0f;
            }
            CalcCurLight();
            if (mAngle > 6.2831855f)
            {
                mAngle -= 6.2831855f;
            }
            else if (mAngle < 0.0f)
            {
                mAngle += 6.2831855f;
            }
            if (aDelaying)
            {
                return;
            }
            if (mCoastTime > 0)
            {
                mCoastTime--;
            }
            if ((mRimLight - 5) % 10 != 0)
            {
                if ((mRimLight + 4) % 10 == 0 && anOldRim != mRimLight)
                {
                    mBoard.mSoundMgr.AddSoundPitch(Res.SOUND_DING, 0.0f);
                    mDingCount = (mDingCount + 1) % 4;
                    if (mCoastTime == 0)
                    {
                        mSpeed -= 0.0016f;
                    }
                }
                else if (mCoastTime == 0)
                {
                    mSpeed -= 0.0008f;
                }
            }
            if (mSpeed <= 0.0f)
            {
                int[] aResults = { 2, 1, 0, 3 };
                mBoard.mLogicMgr.DoSlotMachineResult(aResults[mCurLight]);
                mSpeed = 0.0f;
                mDoneCount = 1;
            }
        }

        public override void Update()
        {
            if (mBoard.mPauseCount != 0)
            {
                return;
            }
            int aSteps = 1;
            if (mBoard.mRightClickSpeedup && mWidgetManager != null && mWidgetManager.IsRightButtonDown())
            {
                aSteps = 4;
            }
            for (int s = 0; s < aSteps; s++)
            {
                base.Update();
                MarkDirty();
                for (int i = 0; i < 0x28; i++)
                {
                    if (mLights[i] != 0)
                    {
                        mLights[i]--;
                    }
                }
                if (mDoneCount < 1)
                {
                    UpdatePointer(true);
                    continue;
                }
                mDoneCount++;
                if (mDoneCount == 0x78)
                {
                    mScrollTargetY = 600;
                    mScrollDir = 1;
                }
                if (mDoneCount > 0x8c)
                {
                    mBoard.RemoveSlotMachineDialog();
                    if (mBall == null || mHole == null)
                    {
                        return;
                    }
                    mBoard.mLogicMgr.FinishSlotMachine(mBall, mHole);
                    return;
                }
                CalcCurLight();
            }
        }
    }
}
