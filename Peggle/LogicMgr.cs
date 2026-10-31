using System;
using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The game rules (Sexy::LogicMgr): turn/shot state machine, scoring, style shots, power-ups,
    /// fever, goal pegs, free-ball bucket, and the input that aims and fires the launcher.
    /// Field names without a known meaning carry their original offset.
    /// </summary>
    public partial class LogicMgr
    {
        public const int STATE_NONE = 0;

        public const int STATE_AIMING = 1;

        public const int STATE_SHOOTING = 2;

        /// <summary>LogicMgr::PegHitInfo (0x10 bytes).</summary>
        public class PegHitInfo
        {
            public int mInt00;                                    // 0x00

            public int mInt04;                                    // 0x04

            public float mX;                                      // 0x08

            public float mY;                                      // 0x0c

            public PegHitInfo SyncState(DataSync theSync)
            {
                theSync.SyncLong(ref mInt00);
                theSync.SyncLong(ref mInt04);
                theSync.SyncFloat(ref mX);
                theSync.SyncFloat(ref mY);
                return this;
            }
        }

        /// <summary>LogicMgr::ClickInfo (8 bytes): a recorded input event for replays.</summary>
        public class ClickInfo
        {
            public int mTime;                                     // 0x00

            public bool mFlag4;                                   // 0x04

            public bool mFlag5;                                   // 0x05

            public ClickInfo()
            {
            }

            public ClickInfo(int theTime, bool theFlag4, bool theFlag5)
            {
                mTime = theTime;
                mFlag4 = theFlag4;
                mFlag5 = theFlag5;
            }

            public ClickInfo SyncState(DataSync theSync)
            {
                theSync.SyncLong(ref mTime);
                theSync.SyncBoolBit(ref mFlag4);
                theSync.SyncBoolBit(ref mFlag5);
                theSync.EndBit();
                return this;
            }
        }

        /// <summary>LogicMgr::BallPosInfo (0x14 bytes): a ball position sample recorded for replays.</summary>
        public class BallPosInfo : IComparable<BallPosInfo>
        {
            public int mTime;                                     // 0x00

            public uint mId;                                      // 0x04

            public float mX;                                      // 0x08

            public float mY;                                      // 0x0c

            public bool mWritten;                                 // 0x10

            public int CompareTo(BallPosInfo theOther)
            {
                if (mTime < theOther.mTime)
                {
                    return -1;
                }
                if (theOther.mTime < mTime)
                {
                    return 1;
                }
                return mId.CompareTo(theOther.mId);
            }
        }

        public int mState;                                        // 0x04

        public int mStateCount;                                   // 0x08

        public int mInt0c;                                        // 0x0c

        public int mInt10;                                        // 0x10

        public int mInt14;                                        // 0x14

        public bool mFlag18;                                      // 0x18

        public bool mFlag19;                                      // 0x19

        public bool mGotExtraBall;                                // 0x1a

        public bool mFlag1B;                                      // 0x1b

        public bool mFlag1C;                                      // 0x1c

        public bool mFlag1D;                                      // 0x1d

        public bool mFlag1E;                                      // 0x1e

        public bool mFlag1F;                                      // 0x1f

        public bool mFlag20;                                      // 0x20

        public bool mFlag21;                                      // 0x21

        public int mInt24;                                        // 0x24

        public int mInt28;                                        // 0x28

        public int mInt2c;                                        // 0x2c

        public int mInt30;                                        // 0x30

        public int mTurnTextId;                                   // 0x34 (FloatingText id)

        public int mInt38;                                        // 0x38

        public int mInt3c;                                        // 0x3c

        public int mInt40;                                        // 0x40

        public int mInt44;                                        // 0x44

        public int mInt48;                                        // 0x48

        public int mInt4c;                                        // 0x4c

        public int mInt50;                                        // 0x50

        public bool mWearHat;                                     // 0x54

        public bool mSuperGuide;                                  // 0x55

        public int[] mTripleScoreCount = new int[2];              // 0x58

        public int[] mHatCount = new int[2];                      // 0x60

        public bool mTripleScore;                                 // 0x68

        public bool mHatBall;                                     // 0x69

        public int mInt6c;                                        // 0x6c

        public bool mZenThinking;                                      // 0x70

        public LightningOrb mLightningOrb;                        // 0x74

        public int mLastStyleShotTime;                            // 0x78

        public float mWheelMove;                                  // 0x7c

        public int[] mComputerSkill = new int[2];                 // 0x80 (per player)

        public string mCornerText = "";                           // 0x88 (current power-up label)

        public int mMouseX;                                       // 0x8c

        public int mMouseY;                                       // 0x90

        public float mWheelMoveRad;                               // 0x94

        public int mInt98;                                        // 0x98

        public int mInt9c;                                        // 0x9c

        public bool mFlagA0;                                      // 0xa0

        public bool mFlagA1;                                      // 0xa1

        public bool mFlagA2;                                      // 0xa2

        public SoundInstance mSlowMoSound;                        // 0xa4

        public SoundInstance mSighSound;                          // 0xa8

        public List<PegHitInfo> mPegHits = new List<PegHitInfo>(); // 0xac

        public List<ClickInfo> mClicks = new List<ClickInfo>();   // 0xb8

        public List<BallPosInfo> mBallPositions = new List<BallPosInfo>(); // 0xc0

        public float mGunAngle;                                   // 0xcc

        public float mFloatD0;                                    // 0xd0

        public int mIntD4;                                        // 0xd4

        public bool mFlagD8;                                      // 0xd8

        public bool mFlagD9;                                      // 0xd9

        public bool mFlagDA;                                      // 0xda (stealth helper shot active)

        public int mIntDC;                                        // 0xdc

        public bool mFlagE0;                                      // 0xe0

        public bool mMuteLoops;                                   // 0xe1

        public bool mDimLowObjects;                               // 0xe2

        public bool mFlagE3;                                      // 0xe3

        public bool mFlagE4;                                      // 0xe4

        public bool mFeverPending;                                      // 0xe5

        public bool mSlowMoSoundPlaying;                                      // 0xe6

        public bool mClearLevelMode;                              // 0xe7

        public int[] mStyleShotsLeft = new int[GameStats.NUM_STYLE_SHOTS]; // 0xe8

        public List<int> mStyleShotCounts = new List<int>();     // 0x148

        public bool mLevelCleared;                                     // 0x154

        public bool mFlag155;                                     // 0x155

        public bool mFlag156;                                     // 0x156

        public bool mFlag157;                                     // 0x157

        public bool mFlag158;                                     // 0x158

        public bool mFlag159;                                     // 0x159

        public Board mBoard;                                      // 0x15c

        public int mShotScore;                                    // 0x160

        public int mInt164;                                       // 0x164

        public int mStyleScore;                                   // 0x168

        public int mInt16c;                                       // 0x16c

        public int mInt170;                                       // 0x170

        public int mInt174;                                       // 0x174

        public int mInt178;                                       // 0x178

        public int mInt17c;                                       // 0x17c

        public bool mDuelMode;                                    // 0x180

        public int mCurPlayer;                                    // 0x184

        public int mInt188;                                       // 0x188

        public int mNumGoalPegs;                                       // 0x18c

        public int mFreeBallRadius;                               // 0x190

        public int mGuideCount;                                   // 0x194

        public int mInt198;                                       // 0x198

        public int mInt19c;                                       // 0x19c

        public int mInt1a0;                                       // 0x1a0

        public int mInt1a4;                                       // 0x1a4

        public int mInt1a8;                                       // 0x1a8

        public int mInt1ac;                                       // 0x1ac

        public int mInt1b0;                                       // 0x1b0

        public int mInt1b4;                                       // 0x1b4

        public int mInt1b8;                                       // 0x1b8

        public int mInt1bc;                                       // 0x1bc

        public int mInt1c0;                                       // 0x1c0

        public int mInt1c4;                                       // 0x1c4

        public int mInt1c8;                                       // 0x1c8

        public PhysObj mObj1cc;                                   // 0x1cc

        public int[] mScore = new int[2];                         // 0x1d0

        public int[] mBallsLeft = new int[2];                     // 0x1d8

        public int[] mCharacters = new int[2];                    // 0x1e0

        public string[] mPlayerNames = { "", "" };                // 0x1e8

        public int[] mPowerupTypes = new int[2];                  // 0x1f0

        public int[] mPowerupTypesActive = new int[2];            // 0x1f8 (permanent power-up per player)

        public int[][] mPowerupCount = MakePowerupCount();        // 0x200 [type][player]

        public bool[] mIsComputer = new bool[2];                  // 0x278

        public GameStats[] mStats = { new GameStats(), new GameStats() }; // 0x27c

        public int mInt374;                                       // 0x374

        public int mBucketBonusCount;                             // 0x378

        public int[] mInt37c = new int[2];                        // 0x37c

        public SexyVector2 mVec384;                               // 0x384

        public SexyVector2 mEyeTarget;                            // 0x38c

        public List<PhysObj> mList394 = new List<PhysObj>();     // 0x394

        public List<PhysObj> mList39c = new List<PhysObj>();     // 0x39c

        public HashSet<PhysObj> mGoalPegs = new HashSet<PhysObj>();     // 0x3a4

        public HashSet<PhysObj> mNonGoalPegs = new HashSet<PhysObj>();  // 0x3bc

        public LevelStats mLevelStats = new LevelStats();         // 0x3d4

        public PhysObj mObj400;                                   // 0x400

        public bool mFlag404;                                     // 0x404

        private static int[][] MakePowerupCount()
        {
            int[][] aCounts = new int[15][];
            for (int i = 0; i < 15; i++)
            {
                aCounts[i] = new int[2];
            }
            return aCounts;
        }

        private static float GetBaseMouseWheelMove()
        {
            return 0.1f;
        }

        public LogicMgr(Board theBoard)
        {
            mBoard = theBoard;
            mPowerupTypesActive[1] = 0;
            mPowerupTypesActive[0] = 0;
            mWearHat = false;
            mFlag21 = false;
            mSlowMoSound = null;
            mSighSound = null;
            mLightningOrb = new LightningOrb(this);
            Clear(true, false);
        }

        public void Clear(bool clearAll, bool keepObjects)
        {
            SetState(STATE_NONE);
            mStateCount = 0;
            mLightningOrb.Reset();
            mObj400 = null;
            mStyleShotCounts.Clear();
            for (int i = 0; i < 0x18; i++)
            {
                mStyleShotCounts.Add(0);
            }
            Ball.mGravity = Ball.mDefaultGravity;
            mInt0c = 0;
            mInt10 = 0;
            mInt14 = 0;
            mFlag18 = false;
            mInt16c = 0;
            mFlag19 = false;
            mGotExtraBall = false;
            mFlag1B = false;
            mFlag1C = false;
            mFlag159 = false;
            mFlag1D = false;
            mFlag1E = false;
            mFlag20 = false;
            mFlag1F = false;
            mFlag21 = false;
            mTripleScore = false;
            mHatBall = false;
            mFlag158 = false;
            mInt24 = 0;
            mInt50 = 0;
            mInt2c = 0;
            mInt30 = 0;
            mTurnTextId = 0;
            mInt38 = 0;
            mInt3c = 0;
            mInt44 = 0;
            mInt40 = 0;
            mInt48 = 0;
            mInt4c = 0;
            mInt17c = 0;
            mInt28 = 0;
            mCornerText = "";
            mInt9c = 0;
            mInt98 = 0;
            mFlagA0 = false;
            mFlagA2 = false;
            mFlagA1 = false;
            mWheelMove = GetBaseMouseWheelMove();
            mWheelMoveRad = SexyUtil.DegToRad(mWheelMove);
            mMouseX = 400;
            mMouseY = 300;
            mGunAngle = 0;
            mFloatD0 = 0;
            mIntD4 = 0;
            mFlagD8 = false;
            mFlagD9 = false;
            mIntDC = 0;
            mFlagE0 = false;
            mMuteLoops = false;
            mDimLowObjects = false;
            mFlagE3 = false;
            mFlagE4 = false;
            mFeverPending = false;
            mSlowMoSoundPlaying = false;
            mFlag157 = false;
            mLevelCleared = false;
            mFlag155 = false;
            mFlag156 = false;
            mInt1c8 = 0;
            mShotScore = 0;
            mInt164 = 0;
            mStyleScore = 0;
            mInt170 = 0;
            mInt174 = 0;
            mInt178 = 0;
            mCurPlayer = 0;
            mInt188 = 0;
            mFreeBallRadius = 0;
            mGuideCount = 0;
            mInt198 = 0;
            mInt19c = 0;
            mInt1a0 = 0;
            mInt1a4 = 0;
            mInt1a8 = -1;
            mInt1ac = -1;
            mInt1b0 = 0;
            mInt1b4 = 0;
            mInt1b8 = 0;
            mInt1c0 = 0;
            mInt1c4 = 0;
            mInt1bc = 0;
            mInt374 = 0;
            mBucketBonusCount = 0;
            mVec384 = new SexyVector2(0, 0);
            mEyeTarget = new SexyVector2(0, 0);
            KillSlowMoSound();
            KillSighSound();
            if (!keepObjects)
            {
                mObj1cc = null;
                mList394.Clear();
                mGoalPegs.Clear();
                mNonGoalPegs.Clear();
            }
            mList39c.Clear();
            mLevelStats.Clear();
            mClicks.Clear();
            mBallPositions.Clear();
            mPegHits.Clear();
            for (int i = 0; i < 0xf; i++)
            {
                mPowerupCount[i][0] = 0;
                mPowerupCount[i][1] = 0;
            }
            for (int i = 0; i < 2; i++)
            {
                mTripleScoreCount[i] = 0;
                mHatCount[i] = 0;
                mStats[i].Reset();
            }
            if (!clearAll)
            {
                mInt37c[0] = mScore[0];
                mInt37c[1] = mScore[1];
            }
            else
            {
                for (int i = 0; i < 0x18; i++)
                {
                    mStyleShotsLeft[i] = 0;
                }
                mClearLevelMode = false;
                mDuelMode = false;
                mScore[1] = 0;
                mScore[0] = 0;
                mInt37c[1] = 0;
                mInt37c[0] = 0;
                mBallsLeft[1] = 0;
                mBallsLeft[0] = 0;
                mCharacters[1] = 0;
                mCharacters[0] = 0;
                mPlayerNames[0] = SexyAppBase.Loc("Player1");
                mPlayerNames[1] = SexyAppBase.Loc("Player2");
                mPowerupTypes[1] = 1;
                mPowerupTypes[0] = 1;
                mIsComputer[1] = false;
                mIsComputer[0] = false;
                mComputerSkill[1] = 0;
                mComputerSkill[0] = 0;
                mNumGoalPegs = 0;
            }
            mFlag404 = false;
            mInt6c = -1;
            mZenThinking = false;
        }

        public void KillSlowMoSound()
        {
            if (mSlowMoSound != null)
            {
                mSlowMoSound.Release();
                mSlowMoSound = null;
            }
        }

        public void KillSighSound()
        {
            if (mSighSound != null)
            {
                mBoard.mSoundMgr.AddFadeSound(mSighSound);
                mSighSound = null;
            }
        }

        public void SyncState(DataSync theSync)
        {
            if (theSync.IsReading)
            {
                mLightningOrb.Reset();
                Ball.mGravity = Ball.mDefaultGravity;
            }
            theSync.SyncLong(ref mState);
            theSync.SyncLong(ref mStateCount);
            theSync.SyncLong(ref mInt0c);
            theSync.SyncBoolBit(ref mFlag18);
            theSync.SyncBoolBit(ref mFlag19);
            theSync.SyncBoolBit(ref mFlag1C);
            theSync.SyncBoolBit(ref mDuelMode);
            theSync.SyncBoolBit(ref mFlagD8);
            theSync.SyncBoolBit(ref mDimLowObjects);
            theSync.SyncBoolBit(ref mIsComputer[0]);
            theSync.SyncBoolBit(ref mIsComputer[1]);
            theSync.EndBit();
            theSync.SyncBoolBit(ref mFlagE0);
            theSync.SyncBoolBit(ref mFlag1D);
            theSync.SyncBoolBit(ref mFlag1E);
            theSync.SyncBoolBit(ref mFlagE4);
            theSync.SyncBoolBit(ref mFeverPending);
            theSync.SyncBoolBit(ref mClearLevelMode);
            theSync.SyncBoolBit(ref mFlag20);
            theSync.SyncBoolBit(ref mFlag157);
            theSync.EndBit();
            bool has1c0 = mInt1c0 > 0;
            bool has1c4 = mInt1c4 > 0;
            bool has1c8 = mInt1c8 > 0;
            if (theSync.GetVersion() < 0x26)
            {
                mInt1c0 = 0;
                mInt1c4 = 0;
                mInt1c8 = 0;
            }
            else
            {
                theSync.SyncBoolBit(ref mLevelCleared);
                theSync.SyncBoolBit(ref has1c0);
                theSync.SyncBoolBit(ref has1c4);
                theSync.SyncBoolBit(ref has1c8);
                theSync.SyncBoolBit(ref mGotExtraBall);
                theSync.SyncBoolBit(ref mFlag1B);
                theSync.SyncBoolBit(ref mTripleScore);
                theSync.SyncBoolBit(ref mHatBall);
                theSync.EndBit();
                if (has1c0)
                {
                    theSync.SyncLong(ref mInt1c0);
                }
                if (has1c4)
                {
                    theSync.SyncLong(ref mInt1c4);
                }
                if (has1c8)
                {
                    theSync.SyncLong(ref mInt1c8);
                }
            }
            theSync.SyncLong(ref mInt24);
            theSync.SyncFloat(ref mGunAngle);
            theSync.SyncLong(ref mShotScore);
            theSync.SyncLong(ref mInt164);
            theSync.SyncLong(ref mInt170);
            theSync.SyncLong(ref mInt178);
            theSync.SyncLong(ref mCurPlayer);
            theSync.SyncLong(ref mInt188);
            if (theSync.GetVersion() < 0xe)
            {
                int aDummy = 0;
                theSync.SyncLong(ref aDummy);
            }
            if (theSync.GetVersion() < 0x14)
            {
                mFeverPending = true;
            }
            theSync.SyncLong(ref mNumGoalPegs);
            theSync.SyncLong(ref mFreeBallRadius);
            if (theSync.GetVersion() > 2)
            {
                theSync.SyncLong(ref mInt2c);
                theSync.SyncLong(ref mInt30);
                theSync.SyncLong(ref mTurnTextId);
                theSync.SyncLong(ref mInt38);
            }
            if (theSync.GetVersion() > 4)
            {
                theSync.SyncLong(ref mGuideCount);
            }
            if (theSync.GetVersion() > 7)
            {
                theSync.SyncLong(ref mCharacters[0]);
                theSync.SyncLong(ref mCharacters[1]);
            }
            if (theSync.GetVersion() > 8)
            {
                theSync.SyncLong(ref mInt3c);
                if (theSync.GetVersion() < 0xd)
                {
                    int aDummy = 0;
                    theSync.SyncLong(ref aDummy);
                }
            }
            if (theSync.GetVersion() > 9)
            {
                theSync.SyncLong(ref mComputerSkill[0]);
                theSync.SyncLong(ref mComputerSkill[1]);
            }
            if (theSync.GetVersion() > 10)
            {
                theSync.SyncLong(ref mInt198);
                theSync.SyncLong(ref mInt19c);
                theSync.SyncLong(ref mInt1a0);
                theSync.SyncLong(ref mInt1a4);
                theSync.SyncLong(ref mInt1a8);
                if (theSync.GetVersion() < 0x2e)
                {
                    mInt1ac = -1;
                }
                else
                {
                    theSync.SyncLong(ref mInt1ac);
                }
            }
            if (theSync.GetVersion() < 0xd)
            {
                mInt10 = 0;
                mInt14 = 0;
            }
            else
            {
                theSync.SyncLong(ref mInt10);
                theSync.SyncLong(ref mInt14);
            }
            if (theSync.GetVersion() < 0x11)
            {
                mInt1b0 = 0;
            }
            else
            {
                theSync.SyncLong(ref mInt1b0);
            }
            if (theSync.GetVersion() < 0x14)
            {
                mInt1b4 = 0;
            }
            else
            {
                theSync.SyncLong(ref mInt1b4);
            }
            if (theSync.GetVersion() < 0x17)
            {
                mCornerText = "";
            }
            else
            {
                theSync.SyncWString(ref mCornerText);
            }
            if (theSync.GetVersion() < 0x1e)
            {
                mInt44 = 0;
                mPegHits.Clear();
            }
            else
            {
                theSync.SyncLong(ref mInt44);
                theSync.SyncLong(ref mInt48);
                theSync.SyncList(mPegHits, (ds, p) => p.SyncState(ds), () => new PegHitInfo());
            }
            if (theSync.GetVersion() > 0x54)
            {
                LightningOrb anOrb = mLightningOrb;
                theSync.SyncLong(ref anOrb.mState);
                theSync.SyncLong(ref anOrb.mBaseX);
                theSync.SyncFloat(ref anOrb.mBaseY);
                theSync.SyncLong(ref anOrb.mX);
                theSync.SyncFloat(ref anOrb.mY);
                theSync.SyncFloat(ref anOrb.mVelY);
                theSync.SyncFloat(ref anOrb.mTopY);
                theSync.SyncFloat(ref anOrb.mRestY);
                theSync.SyncLong(ref anOrb.mStateCount);
                theSync.SyncLong(ref anOrb.mPulseCount);
                theSync.SyncLong(ref anOrb.mRingCounter);
                theSync.SyncIntList(anOrb.mRings);
                theSync.SyncBoolBit(ref mZenThinking);
                theSync.SyncLong(ref mInt6c);
            }
            if (theSync.GetVersion() < 0x1f)
            {
                mInt4c = 0;
            }
            else
            {
                theSync.SyncLong(ref mInt4c);
            }
            if (theSync.GetVersion() < 0x24)
            {
                mInt1b8 = 10;
            }
            else
            {
                theSync.SyncLong(ref mInt1b8);
            }
            if (theSync.GetVersion() < 0x28)
            {
                for (int i = 0; i < 2; i++)
                {
                    mTripleScoreCount[i] = 0;
                    mHatCount[i] = 0;
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    theSync.SyncByte(ref mTripleScoreCount[i]);
                    theSync.SyncByte(ref mHatCount[i]);
                }
            }
            if (theSync.GetVersion() > 0x29)
            {
                theSync.SyncLong(ref mInt1bc);
                mStats[0].SyncState(theSync);
                if (mDuelMode)
                {
                    mStats[1].SyncState(theSync);
                }
            }
            if (theSync.GetVersion() < 0x2b)
            {
                mInt17c = 0;
            }
            else
            {
                theSync.SyncShort(ref mInt17c);
            }
            if (theSync.GetVersion() < 0x2e)
            {
                mInt50 = mFlag1C ? 1 : 0;
            }
            else
            {
                theSync.SyncByte(ref mInt50);
            }
            if (theSync.GetVersion() < 0x2f)
            {
                mSlowMoSoundPlaying = false;
                mSuperGuide = false;
                mFlag155 = false;
                mFlag156 = false;
            }
            else
            {
                bool hasD0 = mFloatD0 != 0.0f;
                theSync.SyncBoolBit(ref hasD0);
                theSync.SyncBoolBit(ref mSlowMoSoundPlaying);
                theSync.SyncBoolBit(ref mSuperGuide);
                theSync.SyncBoolBit(ref mFlagA0);
                theSync.SyncBoolBit(ref mFlagA2);
                theSync.SyncBoolBit(ref mFlagA1);
                theSync.SyncBoolBit(ref mFlag155);
                theSync.SyncBoolBit(ref mFlag156);
                theSync.EndBit();
                if (hasD0)
                {
                    theSync.SyncFloat(ref mFloatD0);
                }
            }
            if (theSync.GetVersion() > 0x32)
            {
                theSync.SyncLong(ref mInt98);
                theSync.SyncLong(ref mInt9c);
            }
            if (theSync.GetVersion() < 0x37)
            {
                mInt374 = 0;
            }
            else
            {
                int aDummy = 0;
                theSync.SyncLong(ref aDummy);
                theSync.SyncLong(ref mInt374);
            }
            if (theSync.GetVersion() > 0x37)
            {
                int aDummy = 0;
                theSync.SyncLong(ref aDummy);
            }
            if (theSync.GetVersion() < 0x3b)
            {
                mInt37c[0] = 0;
                mInt37c[1] = 0;
            }
            else
            {
                theSync.SyncLong(ref mInt37c[0]);
                theSync.SyncLong(ref mInt37c[1]);
            }
            if (theSync.GetVersion() < 0x3c)
            {
                mInt40 = 0;
                mStyleScore = 0;
            }
            else
            {
                theSync.SyncLong(ref mInt40);
                theSync.SyncLong(ref mStyleScore);
            }
            if (theSync.GetVersion() < 0x3e)
            {
                mLastStyleShotTime = -1000;
            }
            else
            {
                theSync.SyncLong(ref mLastStyleShotTime);
            }
            if (theSync.GetVersion() > 0x3f)
            {
                theSync.SyncBoolBit(ref mFlag159);
                theSync.EndBit();
            }
            if (theSync.GetVersion() > 0x41)
            {
                theSync.SyncLong(ref mBucketBonusCount);
            }
            if (theSync.GetVersion() > 0x54)
            {
                int aCount = 0x18;
                theSync.SyncLong(ref aCount);
                for (int i = 0; i < aCount; i++)
                {
                    theSync.SyncLong(ref mStyleShotsLeft[i]);
                }
            }
            if (theSync.GetVersion() > 0x54)
            {
                theSync.SyncIntList(mStyleShotCounts);
                theSync.SyncLong(ref mInt16c);
            }
            mObj1cc = PhysObj.SyncPhysObj(theSync, mObj1cc);
            for (int i = 0; i < 2; i++)
            {
                theSync.SyncLong(ref mScore[i]);
                theSync.SyncLong(ref mBallsLeft[i]);
                theSync.SyncString(ref mPlayerNames[i]);
                theSync.SyncLong(ref mPowerupTypes[i]);
                theSync.SyncLong(ref mPowerupTypesActive[i]);
                int aNumPowerups = 0xf;
                if (theSync.GetVersion() < 3)
                {
                    aNumPowerups = 7;
                }
                else if (theSync.GetVersion() < 0x55)
                {
                    aNumPowerups = 0xe;
                }
                for (int j = 0; j < aNumPowerups; j++)
                {
                    theSync.SyncLong(ref mPowerupCount[j][i]);
                }
            }
            theSync.SyncFloat(ref mVec384.x);
            theSync.SyncFloat(ref mVec384.y);
            theSync.SyncFloat(ref mEyeTarget.x);
            theSync.SyncFloat(ref mEyeTarget.y);
            theSync.SyncList(mList394, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            theSync.SyncList(mList39c, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            theSync.SyncSet(mGoalPegs, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            theSync.SyncSet(mNonGoalPegs, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            if (theSync.IsReading && mMuteLoops)
            {
                mInt1a8 = -1;
                mInt1ac = -1;
            }
        }

        /// <summary>Replays: the click list, then the recorded ball positions grouped by ball id.</summary>
        public void SyncClickTimes(DataSync theSync)
        {
            theSync.SyncList(mClicks, (ds, c) => c.SyncState(ds), () => new ClickInfo());
            if (theSync.GetWriter() == null)
            {
                mBallPositions.Clear();
                DataReader aReader = theSync.GetReader();
                if (aReader.CanReadBytes(1))
                {
                    theSync.SyncLong(ref mInt1a8);
                    if (theSync.GetVersion() > 0x2d)
                    {
                        theSync.SyncLong(ref mInt1ac);
                    }
                    if (theSync.GetVersion() > 0x17)
                    {
                        int aSeqId = -1;
                        if (theSync.GetVersion() < 0x1d)
                        {
                            aSeqId = 0;
                        }
                        SortedSet<BallPosInfo> aSet = new SortedSet<BallPosInfo>();
                        while (true)
                        {
                            int aCount = aReader.ReadLong();
                            if (aCount == 0)
                            {
                                break;
                            }
                            int anId = aReader.ReadLong();
                            int aTime = aReader.ReadLong();
                            if (aSeqId > -1)
                            {
                                anId = aSeqId;
                                aSeqId++;
                            }
                            for (int i = 0; i < aCount; i++)
                            {
                                BallPosInfo anInfo = new BallPosInfo();
                                anInfo.mTime = aTime;
                                aTime++;
                                anInfo.mId = (uint)anId;
                                anInfo.mX = aReader.ReadFloat();
                                anInfo.mY = aReader.ReadFloat();
                                aSet.Add(anInfo);
                            }
                        }
                        mBallPositions.Clear();
                        foreach (BallPosInfo anInfo in aSet.Reverse())
                        {
                            mBallPositions.Add(anInfo);
                        }
                    }
                }
            }
            else
            {
                DataWriter aWriter = theSync.GetWriter();
                theSync.SyncLong(ref mInt1a8);
                theSync.SyncLong(ref mInt1ac);
                for (int i = 0; i < mBallPositions.Count; i++)
                {
                    BallPosInfo anInfo = mBallPositions[i];
                    if (!anInfo.mWritten)
                    {
                        int aPos = aWriter.GetPos();
                        uint anId = anInfo.mId;
                        aWriter.WriteLong(0);
                        aWriter.WriteLong((int)anId);
                        aWriter.WriteLong(anInfo.mTime);
                        int aCount = 0;
                        for (int j = i; j < mBallPositions.Count; j++)
                        {
                            BallPosInfo anOther = mBallPositions[j];
                            if (!anOther.mWritten && anOther.mId == anId)
                            {
                                anOther.mWritten = true;
                                aWriter.WriteFloat(anOther.mX);
                                aWriter.WriteFloat(anOther.mY);
                                aCount++;
                            }
                        }
                        aWriter.SetLong(aCount, aPos);
                    }
                }
                aWriter.WriteLong(0);
            }
        }

        public void WriteClickTimes()
        {
            if (mBoard.mReplayWriters.Count != 0)
            {
                DataWriter aWriter = mBoard.mReplayWriters[mBoard.mReplayWriters.Count - 1];
                if (aWriter != null)
                {
                    DataSync aSync = new DataSync(aWriter);
                    SyncClickTimes(aSync);
                }
            }
        }

        public void SetState(int theState)
        {
            mState = theState;
            mStateCount = 0;
        }

        public void IncScore(int theScore, bool settle)
        {
            int anInc = theScore;
            if (theScore < 0 && theScore + mScore[mCurPlayer] < 0)
            {
                anInc = -mScore[mCurPlayer];
            }
            mScore[mCurPlayer] += anInc;
            if (mCurPlayer == 0)
            {
                mBoard.mGameId += anInc; // Board+0xe4: player one's running score
            }
            if (settle)
            {
                mBoard.mInterfaceMgr.SettleDown();
            }
        }

        /// <summary>Adds to the shot score; crossing 25k/75k/125k awards an extra ball (or a duel bonus).</summary>
        public void IncShotScore(int theScore)
        {
            int aPrev = mShotScore;
            mShotScore += theScore;
            if (!ClearedLevel(false))
            {
                int[] aThresholds = { 25000, 75000, 125000 };
                for (int i = 0; i < 3; i++)
                {
                    if (aThresholds[i] <= mShotScore && aPrev < aThresholds[i])
                    {
                        mBoard.mInterfaceMgr.mShotMeterScore = mShotScore;
                        mBoard.mInterfaceMgr.DoShotMeterBlink();
                        bool allowFreeBall = true;
                        if (mBoard.mTrophyInfo != null)
                        {
                            allowFreeBall = !mBoard.mTrophyInfo.mNoFreeballs;
                        }
                        if (!mDuelMode)
                        {
                            if (allowFreeBall)
                            {
                                IncNumBalls(1, 0, false);
                            }
                        }
                        else
                        {
                            DoStyleShot(100.0f, 88.0f, 0x13, null);
                        }
                        if (allowFreeBall)
                        {
                            mBoard.mSoundMgr.AddSound(Res.SOUND_EXTRABALL + i);
                            mStats[mCurPlayer].mFreeBalls[i]++;
                            mGotExtraBall = true;
                        }
                    }
                }
            }
        }

        public bool ClearedLevel(bool theCheckTrophy)
        {
            if (mBoard.mGameMode == GameMode.GAMEMODE_CHALLENGE &&
                (!theCheckTrophy || !mBoard.mTrophyInfo.mNoForceWin) && !mDuelMode &&
                mBoard.IsCurTrophyBeaten(false))
            {
                return true;
            }
            if (!mClearLevelMode)
            {
                return mGoalPegs.Count == 0;
            }
            return mGoalPegs.Count == 0 && mNonGoalPegs.Count == 0;
        }

        private static float sLastStyleX;

        private static float sLastStyleY;

        private static int sRepeatCount;

        public void DoStyleShot(float theX, float theY, int theStyleShot, PhysObj theObj)
        {
            if (mStyleShotsLeft[theStyleShot] == 0 && (theStyleShot == 0x15 || theStyleShot == 0xf) && mStyleShotsLeft[0] > 0)
            {
                mStyleShotsLeft[0]--;
            }
            else if (mStyleShotsLeft[theStyleShot] > 0)
            {
                mStyleShotsLeft[theStyleShot]--;
            }
            mStyleShotCounts[theStyleShot]++;
            if (mStateCount - mLastStyleShotTime < 0x14 && Math.Abs((int)(sLastStyleX - theX)) < 0x14 &&
                Math.Abs((int)(sLastStyleY - theY)) < 0x14)
            {
                sRepeatCount++;
                theY -= (float)(sRepeatCount * 0x32);
                if (sLastStyleX <= 325.0f)
                {
                    theX += (float)(sRepeatCount * 0x32);
                }
                else
                {
                    theX -= (float)(sRepeatCount * 0x32);
                }
            }
            else
            {
                sLastStyleX = theX;
                sLastStyleY = theY;
                sRepeatCount = 0;
            }
            mLastStyleShotTime = mStateCount;
            string[] aLines = { "", "" };
            bool big = false;
            bool jimmy = false;
            int aScore = SexyUtil.GetStyleShotScore(theStyleShot, mDuelMode ? 1 : 0, 0);
            if (mDuelMode && theStyleShot == 0x11)
            {
                aScore = mBucketBonusCount * SexyUtil.GetStyleShotScore(0x12, mDuelMode ? 1 : 0, 0) + aScore;
                mStats[mCurPlayer].mStyleShots[0x12] += mBucketBonusCount;
                mBucketBonusCount++;
            }
            switch (theStyleShot)
            {
            case 0:
                aLines[0] = SexyAppBase.Loc("Long Shot!");
                break;
            case 1:
                aLines[0] = SexyAppBase.Loc("Flipper Maniac!");
                break;
            case 2:
                aLines[0] = SexyAppBase.Loc("Extreme Slide!");
                jimmy = true;
                break;
            case 3:
                aLines[0] = SexyAppBase.Loc("Orange Attack!");
                big = true;
                jimmy = true;
                break;
            case 4:
                aLines[0] = SexyAppBase.Loc("Cool Clear!");
                big = true;
                break;
            case 5:
                aLines[0] = SexyAppBase.Loc("Multiball Madness!");
                big = true;
                break;
            case 6:
                aLines[0] = SexyAppBase.Loc("Hat Trick!");
                big = true;
                break;
            case 7:
                aLines[0] = SexyAppBase.Loc("Hot Stuff!");
                big = true;
                break;
            case 8:
                aLines[0] = SexyAppBase.Loc("Free Ball Frenzy!");
                big = true;
                jimmy = true;
                break;
            case 9:
                aLines[0] = SexyAppBase.Loc("Free Ball Skills!");
                break;
            case 10:
                switch (mPowerupTypes[mCurPlayer] - 1)
                {
                case 0:
                    aLines[0] = SexyAppBase.Loc("Double Guide!");
                    break;
                case 1:
                    aLines[0] = SexyAppBase.Loc("Flip Out!");
                    break;
                case 2:
                    aLines[0] = SexyAppBase.Loc("Triple Play!");
                    break;
                case 3:
                    aLines[0] = SexyAppBase.Loc("Pyramid Pileup!");
                    break;
                case 4:
                    aLines[0] = SexyAppBase.Loc("Big Bang!");
                    break;
                case 5:
                    aLines[0] = SexyAppBase.Loc("Spooktacular!");
                    break;
                case 6:
                    aLines[0] = SexyAppBase.Loc("Zen Maniac!");
                    break;
                case 8:
                    aLines[0] = SexyAppBase.Loc("Petal to the Metal!");
                    break;
                case 0xb:
                    aLines[0] = SexyAppBase.Loc("Great Balls of Fire!");
                    break;
                case 0xc:
                    aLines[0] = SexyAppBase.Loc("Spin Again!");
                    break;
                case 0xd:
                    aLines[0] = SexyAppBase.Loc("Power Surge!");
                    break;
                }
                big = true;
                jimmy = true;
                break;
            case 0xb:
                aLines[0] = SexyAppBase.Loc("Kick the Bucket!");
                big = true;
                break;
            case 0xc:
                aLines[0] = SexyAppBase.Loc("Lucky Bounce!");
                big = true;
                break;
            case 0xd:
                aLines[0] = SexyAppBase.Loc("Mad Skillz!");
                big = true;
                jimmy = true;
                break;
            case 0xe:
                aLines[0] = SexyAppBase.Loc("Crazy Mad Skillz!");
                big = true;
                jimmy = true;
                break;
            case 0xf:
                aLines[0] = SexyAppBase.Loc("Super Long Shot!");
                big = true;
                jimmy = true;
                break;
            case 0x10:
                aLines[0] = SexyAppBase.Loc("Pyramid Pickup!");
                break;
            case 0x11:
                aLines[0] = SexyAppBase.Loc("Bucket Bonus!");
                break;
            case 0x13:
                aLines[0] = SexyAppBase.Loc("Mega Shot Bonus!");
                break;
            case 0x14:
                aLines[0] = SexyAppBase.Loc("Eye of the Pyramid!");
                break;
            case 0x15:
                aLines[0] = SexyAppBase.Loc("Double Long Shot!");
                big = true;
                jimmy = true;
                break;
            case 0x16:
                aLines[0] = SexyAppBase.Loc("Off the Wall!");
                break;
            case 0x17:
                aLines[0] = SexyAppBase.Loc("Shock It to Me");
                break;
            default:
                return;
            }
            if (jimmy && ((theStyleShot == 10 && mInt2c % 100 < 10) || (theStyleShot != 10 && mInt2c % 100 < 0x14)))
            {
                DoJimmy(false);
            }
            if (aScore > 0)
            {
                aLines[1] = CFormat.StrFormat("__10k__+%d", aScore);
                mBoard.mInterfaceMgr.AddScoreUpdateDelay(0);
                mStyleScore += aScore;
            }
            mStats[mCurPlayer].mStyleShots[theStyleShot]++;
            float aFirstX = 0;
            for (int i = 0; i < 2; i++)
            {
                if (aLines[i].Length == 0)
                {
                    continue;
                }
                int aSize = i == 0 ? 8 : 6;
                FloatingText aText = AddStandardText(aLines[i], theX, theY, aSize);
                if (i == 0)
                {
                    AvoidSides(aText);
                    aFirstX = aText.mX;
                }
                else
                {
                    int aWide = Res.FONT_OVERLOAD16OUTLINE.StringWidth(aLines[0]);
                    int aNarrow = Res.FONT_OVERLOAD12OUTLINE.StringWidth(aLines[0]);
                    aText.mX = aFirstX + (float)((aWide - aNarrow) / 2);
                }
                aText.mLayer = 2;
                aText.mDuration = big ? 0xfa : 200;
                aText.mVelY = 0;
                if (big)
                {
                    aText.mSpinUntil = 10;
                    aText.mRotationVel = 0.3f;
                }
                // The original skips the scale-in only when big and not 3D accelerated; this port is always 3D.
                aText.AddScale(0.5f, 0);
                aText.AddScale(2.0f, 10);
                aText.AddScale(1.0f, 0x1e);
                aText.mFadeOut = 0x96;
                aText.mColor = 0x80ff80;
                aText.mUpdateCnt = i * -0xf;
                mBoard.mSoundMgr.AddSound(Res.SOUND_SKILLSHOT);
                theY += (float)LOGICMGR_STYLESHOT_SCORE_OFFSET_Y;
            }
        }

        public void IncNumBalls(int theNum, int theType, bool toBucket)
        {
            mBallsLeft[mCurPlayer] += theNum;
            if (theNum > 0)
            {
                if (toBucket)
                {
                    mBoard.mInterfaceMgr.AddBottomBall(theType);
                }
                else
                {
                    mBoard.mInterfaceMgr.AddTopBalls(theNum);
                }
            }
            if (mBallsLeft[mCurPlayer] < 0)
            {
                mBallsLeft[mCurPlayer] = 0;
            }
        }

        /// <summary>
        /// Aims at the last touch: an angle towards it plus a distance-based lead, refined by testing
        /// angles ±0.1 rad for the trajectory that best passes through the touch point.
        /// </summary>
        public void CalcGunAngle(bool applyToGun)
        {
            if (mState == 10 || mFlagD8)
            {
                return;
            }
            Gun aGun = mBoard.mGun;
            mFlag1F = false;
            if (aGun == null)
            {
                mGunAngle = 0;
                return;
            }
            float cx = aGun.mCenterX;
            float cy = aGun.mCenterY;
            float mx = (float)SexyUtil.ScreenToBoardX(mMouseX);
            float my = (float)SexyUtil.ScreenToBoardY(mMouseY);
            float anAngle = (float)Math.Atan2(cy - my, mx - cx);
            SexyVector2 aDelta = new SexyVector2(mx - cx, my - cy);
            float aLead = (float)Math.Abs((double)(float)Math.Atan2(aDelta.x, aDelta.y));
            aLead = aLead * (aDelta.MagnitudeSquared() * 1.3e-06f);
            if (aLead > 1.0f)
            {
                aLead = 1.0f;
            }
            if (aDelta.x < 0.0f)
            {
                aLead = aLead * -1.0f;
            }
            anAngle = anAngle + aLead;
            float aBestAngle = anAngle;
            float aBestErr = 1000000.0f;
            float aStartX = cx + aGun.mBallOffset + aGun.mOffset + Ball.mDefRadius;
            float aStartY = cy + 1.0f;
            float aDist = (float)Math.Sqrt((double)((mx - aStartX) * (mx - aStartX) + (my - aStartY) * (my - aStartY)));
            if (aDist > aGun.mBallOffset + aGun.mOffset + Ball.mDefRadius + 10.0f)
            {
                for (int i = -10; i < 10; i++)
                {
                    float aTry = anAngle + (float)i * 0.01f;
                    float px = aStartX;
                    float py = aStartY;
                    SexyUtil.RotateXY(ref px, ref py, cx, cy, aTry);
                    float dx = mx - px;
                    float dy = my - py;
                    float aSpeed = aGun.mFireSpeed;
                    float aCos = (float)Math.Cos(aTry);
                    float anErr;
                    if (Math.Abs((double)aCos) < 0.0010000000474974513)
                    {
                        anErr = (float)(dx == 0.0f ? 0 : 1000000);
                    }
                    else
                    {
                        float t = dx / (aSpeed * aCos);
                        if (t < 0.0f)
                        {
                            anErr = 1000000.0f;
                        }
                        else
                        {
                            float y = (-0.0f - aSpeed) * t * (float)Math.Sin(aTry) + 0.5f * Ball.mGravity * (t * t);
                            anErr = (float)Math.Abs((double)(y - dy));
                        }
                    }
                    if (anErr < aBestErr)
                    {
                        aBestAngle = aTry;
                        aBestErr = anErr;
                    }
                }
            }
            SetGunAngle(aBestAngle);
            if (applyToGun)
            {
                aGun.SetAngle(mGunAngle, false);
            }
        }

        /// <summary>Normalises the angle and keeps it out of the forbidden band around straight up (see GetAngleConstraint).</summary>
        public void SetGunAngle(float theAngle)
        {
            mGunAngle = SexyUtil.GetCanonicalAngleRad(theAngle);
            float aConstraint = SexyUtil.GetAngleConstraint();
            float aLow = ((90.0f - aConstraint) * 3.1415927f) / 180.0f;
            float aHigh = ((aConstraint + 90.0f) * 3.1415927f) / 180.0f;
            if (aLow < mGunAngle && mGunAngle < aHigh)
            {
                if (mGunAngle <= (aLow + aHigh) / 2.0f)
                {
                    mGunAngle = aLow;
                }
                else
                {
                    mGunAngle = aHigh;
                }
            }
        }

        public void AvoidSides(FloatingText theText)
        {
            int aMargin = theText.mWidth + 10;
            if (theText.mX < (float)aMargin)
            {
                theText.mX = (float)aMargin;
            }
            if (theText.mX > (float)(700 - aMargin))
            {
                theText.mX = (float)(700 - aMargin);
            }
        }

        public FloatingText AddStandardText(string theText, float theX, float theY, int theSize)
        {
            FloatingText aText = mBoard.mFloatingTextMgr.AddText();
            aText.mFontId = theSize < 0 ? 6 : theSize;
            aText.mX = theX;
            aText.mY = theY;
            aText.mVelY = -0.1f;
            aText.mColor = 0xffff00;
            aText.mShadowColor = 0;
            aText.mJustifyY = 0;
            aText.mJustifyX = 0;
            aText.mDuration = 100;
            aText.mLayer = 1;
            aText.SetText(theText);
            return aText;
        }
    }
}
