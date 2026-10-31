using System;
using System.Collections.Generic;
using System.Linq;

namespace Sexy
{
    /// <summary>
    /// The computer opponent and the ZenBall/helper aim search (Sexy::AIMgr). BeginThinking copies
    /// the level into a private CollisionMgr, launches a fan of AIBalls at candidate angles, and
    /// simulates them a few frames per update. When the time is up it picks the best-scoring ball
    /// and swings the launcher to its angle while the character's eyes wander over the board.
    /// </summary>
    public class AIMgr
    {
        /// <summary>Opponent skill: below 0 is easy, 10 or more is ZenBall-strength.</summary>
        public static int mSkillLevel;

        public Board mBoard;                                      // 0x04

        public CollisionMgr mCollisionMgr;                        // 0x08

        /// <summary>Objects updated every think frame: movers shared by all balls, and the balls.</summary>
        public List<PhysObj> mMovingObjs = new List<PhysObj>();  // 0x0c

        public List<PhysObj> mStaticObjs = new List<PhysObj>();  // 0x14

        /// <summary>The free ball bucket and its cover, whose radius tracks LogicMgr's.</summary>
        public List<PhysObj> mFreeBallObjs = new List<PhysObj>(); // 0x1c

        public List<Ball> mBalls = new List<Ball>();              // 0x24

        public AIBall mBestBall;                                  // 0x2c

        public AIBall mPrevBestBall;                              // 0x30

        /// <summary>The ball fired at the starting angle (modes 1 and 2), for comparison.</summary>
        public AIBall mAimBall;                                   // 0x34

        public AIBall mPartnerBall;                               // 0x38

        public bool mShowPath;                                    // 0x3c

        public bool mThinking;                                    // 0x3d

        /// <summary>Simulate everything in one update instead of spreading it out.</summary>
        public bool mFastThink;                                   // 0x3e

        public int mThinkFrames;                                  // 0x40

        public int mFramesPerUpdate;                              // 0x44

        public int mThinkCount;                                   // 0x48

        public int mEyeDir;                                       // 0x4c

        /// <summary>Frames to wait after thinking before the shot is taken.</summary>
        public int mDelayFrames;                                  // 0x50

        public float mAngle;                                      // 0x54

        public bool mHasPowerup1;                                 // 0x58

        public bool mHasFireball;                                 // 0x59

        public bool mHasLightning;                                // 0x5a

        /// <summary>0 computer turn, 1 ZenBall, 2 stealth helper.</summary>
        public int mMode;                                         // 0x5c

        public float mMinAngleDeg;                                // 0x60

        public float mMaxAngleDeg;                                // 0x64

        public float mMinAngle;                                   // 0x68

        public float mMaxAngle;                                   // 0x6c

        public SexyVector2 mEyePos;                               // 0x70

        public SexyVector2 mEyeTarget;                            // 0x78

        public int mUpdateCnt;                                    // 0x80

        public int mFreeBallRadius;                               // 0x84

        public AIMgr(Board theBoard)
        {
            mBoard = theBoard;
            mCollisionMgr = new CollisionMgr();
            mShowPath = false;
            mFramesPerUpdate = 1;
            mDelayFrames = 0x1e;
            Clear();
        }

        public void Clear()
        {
            mThinking = false;
            mFastThink = false;
            mThinkCount = 0;
            if (mBestBall != null)
            {
                mPrevBestBall = mBestBall;
            }
            mBestBall = null;
            mAimBall = null;
            mPartnerBall = null;
            mBalls.Clear();
            mMovingObjs.Clear();
            mStaticObjs.Clear();
            mFreeBallObjs.Clear();
            mCollisionMgr.Clear();
        }

        /// <summary>The launch angle aimed at a peg, nudged for the ball's drop over distance.</summary>
        public float GetAngleForPeg(PhysObj thePeg)
        {
            Gun aGun = mBoard.mGun;
            float aGunX = aGun.mCenterX;
            float aGunY = aGun.mCenterY;
            float aX = thePeg.GetXPos();
            float aY = thePeg.GetYPos();
            float anAngle = (float)Math.Atan2(aGunY - aY, aX - aGunX);
            SexyVector2 aDir = new SexyVector2(aX - aGunX, aY - aGunY);
            float anAdjust = (float)Math.Abs((double)(float)Math.Atan2(aDir.x, aDir.y));
            anAdjust = anAdjust * (aDir.MagnitudeSquared() * 1.3e-06f);
            if (anAdjust > 1.0f)
            {
                anAdjust = 1.0f;
            }
            if (aDir.x < 0.0f)
            {
                anAdjust *= -1.0f;
            }
            anAngle += anAdjust;
            return KeepAngleInRange(anAngle);
        }

        public float KeepAngleInRange(float theAngle)
        {
            float anAngle = SexyUtil.GetCanonicalAngleRad(theAngle);
            if (anAngle <= 1.5707964f || anAngle >= 4.712389f)
            {
                if (anAngle > 1.5707964f)
                {
                    anAngle -= 6.2831855f;
                }
                if (anAngle > mMinAngle)
                {
                    anAngle = mMinAngle;
                }
            }
            else if (anAngle < mMaxAngle)
            {
                anAngle = mMaxAngle;
            }
            return anAngle;
        }

        /// <summary>No good shot: aim at the densest cluster of orange pegs (or keep the player's aim).</summary>
        public void CalcDefAngle()
        {
            LogicMgr aLogicMgr = mBoard.mLogicMgr;
            if (mMode != 0)
            {
                mAngle = aLogicMgr.mGunAngle;
                return;
            }
            PhysObj aBest = null;
            int aBestCount = 0;
            foreach (PhysObj aPeg in aLogicMgr.mGoalPegs)
            {
                float aX = aPeg.GetXPos();
                float aY = aPeg.GetYPos();
                int aCount = 0;
                foreach (PhysObj anOther in aLogicMgr.mGoalPegs)
                {
                    float dx = anOther.GetXPos() - aX;
                    float dy = anOther.GetYPos() - aY;
                    if (dx * dx + dy * dy < 10000.0f)
                    {
                        aCount++;
                    }
                }
                if (aBestCount < aCount)
                {
                    aBest = aPeg;
                    aBestCount = aCount;
                }
            }
            if (aBest != null)
            {
                mAngle = GetAngleForPeg(aBest);
            }
        }

        public bool GetEyePos(ref SexyVector2 thePos)
        {
            if (!mThinking || mThinkCount <= mThinkFrames)
            {
                return false;
            }
            thePos = mEyePos;
            return true;
        }

        /// <summary>Grows or shrinks the copied free ball bucket toward LogicMgr's radius.</summary>
        public void UpdateFreeBallRadius(int theFrames)
        {
            int aStep = theFrames / 2;
            if (mUpdateCnt % 2 == 1)
            {
                aStep++;
            }
            mUpdateCnt += theFrames;
            if (aStep <= 0)
            {
                return;
            }
            bool changed = false;
            foreach (PhysObj anObj in mFreeBallObjs)
            {
                Mover aMover = anObj.mMover;
                float aRadius = (float)aMover.mRadius1;
                float aTarget = (float)mFreeBallRadius;
                if (aRadius != aTarget)
                {
                    changed = true;
                    float aNew;
                    if (aRadius <= aTarget)
                    {
                        aNew = (float)Math.Min((int)(aRadius + (float)aStep), mFreeBallRadius);
                    }
                    else
                    {
                        aNew = (float)Math.Max((int)(aRadius - (float)aStep), mFreeBallRadius);
                    }
                    aMover.mRadius1 = (int)aNew;
                }
            }
            if (!changed)
            {
                mFreeBallRadius = 0;
            }
        }

        public void BeginThinking(bool resume, float theAngle, int theMode, int theExtraFrames)
        {
            PhysObj.mSyncType = 3;
            PhysObj.mFastLoad = true;
            mEyePos = mBoard.mCharacterMgr.GetLastLookPos();
            mEyePos.y = 400.0f;
            mEyeDir = mEyePos.x <= 325.0f ? 1 : -1;
            mMode = theMode;
            BeginThinking2(resume, theAngle, theExtraFrames);
            PhysObj.mFastLoad = false;
        }

        public void BeginThinking2(bool resume, float theAngle, int theExtraFrames)
        {
            LogicMgr aLogicMgr = mBoard.mLogicMgr;
            mUpdateCnt = mBoard.mGameUpdateCnt + 1;
            mFreeBallRadius = aLogicMgr.mFreeBallRadius;
            float aResumeAngle = 0.0f;
            if (resume)
            {
                if (mBestBall == null)
                {
                    return;
                }
                aResumeAngle = mBestBall.mAngle;
            }
            Clear();
            mThinking = true;
            if (!aLogicMgr.mZenThinking)
            {
                mThinkFrames = 500;
                if (mMode == 2)
                {
                    mThinkFrames = 250;
                }
            }
            else
            {
                mThinkFrames = 500;
            }
            int aPlayer = aLogicMgr.mCurPlayer;
            AIBall.mShowPath = true;
            AIBall.mPowerup = aLogicMgr.mPowerupTypes[aPlayer];
            AIBall.mHadPowerup = aLogicMgr.mPowerupCount[AIBall.mPowerup][aPlayer] > 0;
            mHasPowerup1 = aLogicMgr.mPowerupCount[1][aPlayer] > 0;
            mHasFireball = aLogicMgr.mPowerupCount[12][aPlayer] > 0;
            mHasLightning = aLogicMgr.mPowerupCount[14][aPlayer] > 0;
            bool hasTimeBomb = aLogicMgr.mPowerupCount[11][aPlayer] > 0;
            float aConstraint = SexyUtil.GetAngleConstraint();
            mMinAngleDeg = 90.0f - aConstraint;
            mMaxAngleDeg = aConstraint + 90.0f;
            mMinAngle = (mMinAngleDeg * 3.1415927f) / 180.0f;
            mMaxAngle = (mMaxAngleDeg * 3.1415927f) / 180.0f;

            // Copy the level into the AI's world.
            DataSync aSync = new DataSync();
            aSync.StartWriteMemory(0x20);
            aSync.SetVersion(0x57);
            aSync.SyncList(mBoard.mPhysObjs, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            aSync.SyncPointers();
            DataSync aReadSync = new DataSync();
            DataWriter aWriter = aSync.GetWriter();
            aReadSync.StartReadMemory(aWriter.GetDataPtr(), aWriter.GetDataLen(), false);
            aReadSync.SetVersion(0x57);
            aReadSync.SyncList(mMovingObjs, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);

            bool hasPowerupPeg = false;
            foreach (PhysObj aPeg in aLogicMgr.mNonGoalPegs)
            {
                if (aPeg.mPegInfo.mType == PegType.PEG_POWERUP)
                {
                    hasPowerupPeg = true;
                }
            }
            if (hasTimeBomb || (hasPowerupPeg && AIBall.mPowerup == 11))
            {
                mThinkFrames = 1000;
            }
            int aMoveFrames;
            if (!aLogicMgr.mZenThinking)
            {
                aMoveFrames = mThinkFrames / mFramesPerUpdate + -1 + mDelayFrames;
            }
            else
            {
                aMoveFrames = mDelayFrames + -1;
            }
            if (theExtraFrames > 0)
            {
                aMoveFrames = theExtraFrames;
            }
            if (mDelayFrames > 0)
            {
                aMoveFrames++;
            }
            if (aMoveFrames < 0 || resume)
            {
                aMoveFrames = 0;
            }

            // Sort the copies: objects with logic get one copy per ball, the rest are shared.
            aSync.StartWriteMemory(0x20);
            aSync.SetVersion(0x57);
            List<PhysObj> aPerBallObjs = new List<PhysObj>();
            List<object> aSharedMovers = new List<object>();
            for (int i = 0; i < mMovingObjs.Count;)
            {
                PhysObj anObj = mMovingObjs[i];
                if (anObj.mShapeType == PhysObj.SHAPE_BALL || anObj.mFlag30)
                {
                    mMovingObjs.RemoveAt(i);
                    continue;
                }
                anObj.mFlag30 = true;
                if (!string.IsNullOrEmpty(anObj.mLogic))
                {
                    Mover aMover = anObj.mMover;
                    while (aMover != null)
                    {
                        aMover = aMover.mSubMover;
                        if (aMover != null)
                        {
                            aSharedMovers.Add(aMover);
                            aSync.AddRefCount(aMover);
                        }
                    }
                    aPerBallObjs.Add(anObj);
                    mMovingObjs.RemoveAt(i);
                    continue;
                }
                if (anObj.mMover == null)
                {
                    mStaticObjs.Add(anObj);
                    mMovingObjs.RemoveAt(i);
                }
                else
                {
                    if (anObj.mId == "freeballcover" || anObj.mId == "freeball" || anObj.mId == "bumperhole")
                    {
                        mFreeBallObjs.Add(anObj);
                    }
                    if (anObj.mMoving && aMoveFrames > 0)
                    {
                        anObj.SetMoveUpdateCnt(anObj.mMoveUpdateCnt + aMoveFrames);
                    }
                    i++;
                }
                mCollisionMgr.AddObj(anObj);
            }
            if (mFreeBallRadius != 0)
            {
                UpdateFreeBallRadius(aMoveFrames);
            }
            aSync.SyncList(aPerBallObjs, (ds, o) => PhysObj.SyncPhysObj(ds, o), () => null);
            aSync.SyncPointers();

            // Launch position and speed, as Gun.Fire would.
            Gun aGun = mBoard.mGun;
            int aNumAngles = 0x1e;
            float aSpread = 360.0f - (mMaxAngleDeg - mMinAngleDeg);
            float aSpeed = aGun.mFireSpeed;
            float aBallRadius = aGun.mBall != null ? aGun.mBall.mRadius : Ball.mDefRadius;
            int aRadius = (int)aBallRadius;
            float aGunX = aGun.mCenterX;
            float aGunY = aGun.mCenterY;
            float aStartX = aGunX + aGun.mBallOffset + aGun.mOffset + (float)aRadius + (float)Layout.GUN_BALL_PADDING;
#if PEGGLE_IPHONE
            float aStartY = aGunY + 1.0f;
#else
            float aStartY = aGunY + 2.0f;
#endif
            int aGoalPegCount = aLogicMgr.mGoalPegs.Count;
            mAngle = aGun.mAngle;
            mFastThink = resume;
            int anAimIndex = -1;
            List<float> anAngles = new List<float>();
            int aStartTime = 0;
            int anEndTime = 0;
            int aTimeStep = 1;
            if (mMode != 0)
            {
                // Search tightly around the given angle.
                int aHalfCount = 0x78;
                if (mMode == 2)
                {
                    PlayerInfo aPlayerInfo = mBoard.mApp.mPlayerInfo;
                    bool isBeginner = false;
                    if (aPlayerInfo.mLastStageLevel < 4)
                    {
                        isBeginner = aPlayerInfo.mLastStageStage == -1 || aPlayerInfo.mLastStageStage == 0;
                    }
                    aHalfCount = isBeginner ? 3 : 2;
                }
                const float aStep = 0.001f;
                for (int i = -aHalfCount; i <= aHalfCount; i++)
                {
                    if (i == 0)
                    {
                        anAimIndex = anAngles.Count;
                    }
                    anAngles.Add(KeepAngleInRange(theAngle + (float)i * aStep));
                }
            }
            else if (!mHasLightning)
            {
                if (!resume)
                {
                    int aGoalCount = aLogicMgr.mGoalPegs.Count;
                    if (aGoalCount < 10 || mSkillLevel < 0)
                    {
                        int aSpreadCount = 0;
                        if (aGoalCount == 1)
                        {
                            aSpreadCount = 5;
                        }
                        else if (aGoalCount == 2)
                        {
                            aSpreadCount = 2;
                        }
                        else if (aGoalCount == 3)
                        {
                            aSpreadCount = 1;
                        }
                        foreach (PhysObj aPeg in aLogicMgr.mGoalPegs)
                        {
                            float aPegAngle = GetAngleForPeg(aPeg);
                            anAngles.Add(aPegAngle);
                            for (int j = 0; j < aSpreadCount; j++)
                            {
                                anAngles.Add(KeepAngleInRange(aPegAngle + (float)j * -0.01f));
                                anAngles.Add(KeepAngleInRange(aPegAngle + (float)j * 0.01f));
                            }
                        }
                        if (aSpreadCount == 0)
                        {
                            aNumAngles -= aLogicMgr.mGoalPegs.Count;
                        }
                    }
                    foreach (PhysObj aPeg in aLogicMgr.mNonGoalPegs)
                    {
                        if (aPeg.mPegInfo.mType == PegType.PEG_POWERUP)
                        {
                            anAngles.Add(GetAngleForPeg(aPeg));
                        }
                    }
                    int aStepInt = 0;
                    if (aNumAngles > 0)
                    {
                        aSpread = aSpread / (float)aNumAngles;
                        aStepInt = (int)(float)Math.Ceiling(aSpread);
                    }
                    if (aStepInt < 1)
                    {
                        aStepInt = 1;
                    }
                    if (mSkillLevel >= 0)
                    {
                        for (int i = 0; i < aNumAngles; i++)
                        {
                            float aDeg = mMinAngleDeg - (float)i * aSpread;
                            aDeg = aDeg - (float)(SexyUtil.Rand() % aStepInt);
                            aDeg = aDeg + (float)(SexyUtil.Rand() % 1000) / 1000.0f;
                            anAngles.Add(KeepAngleInRange(SexyUtil.DegToRad(aDeg)));
                        }
                    }
                }
                else
                {
                    anAngles.Add(aResumeAngle);
                }
            }
            else
            {
                // Lightning: aim at each unhit peg, top to bottom.
                PhysObj aBucket = mBoard.GetBucket();
                if (aBucket != null && aBucket.mMover != null)
                {
                    anEndTime = aBucket.mMover.mPeriod / 2;
                }
                else
                {
                    anEndTime = 2000;
                }
                aTimeStep = (anEndTime - aStartTime) / 2;
                List<PhysObj> aPegs = new List<PhysObj>(aLogicMgr.mList394.Count);
                foreach (PhysObj anObj in aLogicMgr.mList394)
                {
                    if (anObj != null && anObj.mShapeType == PhysObj.SHAPE_CIRCLE && !anObj.mPegInfo.mHit)
                    {
                        aPegs.Add(anObj);
                    }
                }
                aPegs = aPegs.OrderBy(p => p.GetYPos()).ToList();
                int aPegCount = Math.Min(0xf, aPegs.Count);
                int aSpreadCount = 0;
                if (aPegCount < 10 || mSkillLevel < 0)
                {
                    aSpreadCount = 1;
                    if (aPegCount < 7)
                    {
                        aSpreadCount = 2;
                    }
                    if (aPegCount < 4)
                    {
                        aSpreadCount = 3;
                    }
                }
                for (int i = 0; i < aPegCount; i++)
                {
                    float aPegAngle = GetAngleForPeg(aPegs[i]);
                    if (aPegAngle == KeepAngleInRange(aPegAngle))
                    {
                        for (int j = 0; j <= aSpreadCount; j++)
                        {
                            if (j == 0)
                            {
                                anAngles.Add(aPegAngle);
                            }
                            else
                            {
                                anAngles.Add(KeepAngleInRange(aPegAngle + (float)j * -0.02f));
                                anAngles.Add(KeepAngleInRange(aPegAngle + (float)j * 0.02f));
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < anAngles.Count; i++)
            {
                // The time offsets are never applied, so lightning launches identical balls.
                for (int t = aStartTime; t <= anEndTime; t += aTimeStep)
                {
                    float anAngle = anAngles[i];
                    float aX = aStartX;
                    float aY = aStartY;
                    SexyUtil.RotateXY(ref aX, ref aY, aGunX, aGunY, anAngle);
                    AIBall aBall = new AIBall();
                    if (i == anAimIndex && mMode != 0)
                    {
                        mAimBall = aBall;
                    }
                    aBall.mAngle = anAngle;
                    aBall.mGoalPegsLeft = aGoalPegCount;
                    if (mHasLightning)
                    {
                        aBall.SetElectrified(true, 0);
                    }
                    if (mHasFireball)
                    {
                        aBall.SetFireball(true);
                    }
                    if (hasTimeBomb)
                    {
                        aBall.mBombTime = 0;
                    }
                    aBall.SetPos(aX, aY);
                    aBall.SetVelocity(aSpeed * (float)Math.Cos(anAngle), -(aSpeed * (float)Math.Sin(anAngle)));
                    mBalls.Add(aBall);
                    mMovingObjs.Add(aBall);
                    mCollisionMgr.AddObj(aBall);
                    if (aPerBallObjs.Count != 0)
                    {
                        aBall.SyncObjects(aSync, aSharedMovers);
                        foreach (PhysObj anObj in aBall.mObjects)
                        {
                            if (anObj.mMoving && aMoveFrames > 0)
                            {
                                anObj.SetMoveUpdateCnt(anObj.mMoveUpdateCnt + aMoveFrames);
                            }
                        }
                    }
                }
            }
            mThinkFrames = anEndTime + mThinkFrames;
        }

        public void NotifyCollision(PhysObj theBall, PhysObj theObj)
        {
            theBall.NotifyCollision(theObj);
        }

        public void UpdateOneFrame()
        {
            mThinkCount++;
            AIBall.mThinkCount = mThinkCount;
            AIBall.mAIMgr = this;
            if (mFreeBallRadius != 0)
            {
                UpdateFreeBallRadius(1);
            }
            foreach (PhysObj anObj in mMovingObjs.ToArray())
            {
                anObj.Update();
            }
            mCollisionMgr.Update(true);
            foreach (Ball aBall in mBalls.ToArray())
            {
                AIBall anAIBall = (AIBall)aBall;
                if (anAIBall.mDone == 0)
                {
                    if (!aBall.mInCollideUpdate)
                    {
                        aBall.DoNonCollideUpdate();
                    }
                }
                else if (anAIBall.mDone == 1)
                {
                    mCollisionMgr.RemoveObj(aBall);
                    anAIBall.mDone = 2;
                }
            }
            foreach (CollisionInfo anInfo in mCollisionMgr.mCollisionInfo.ToArray())
            {
                NotifyCollision(anInfo.mBall, anInfo.mObj);
            }
        }

        public void Update()
        {
            if (!mThinking)
            {
                return;
            }
            if (mThinkCount < mThinkFrames)
            {
                int aFrames = mThinkFrames - mThinkCount;
                if (mFramesPerUpdate < aFrames && !mFastThink)
                {
                    aFrames = mFramesPerUpdate;
                }
                for (int i = 0; i < aFrames; i++)
                {
                    UpdateOneFrame();
                }
                mEyePos.x += (float)(mEyeDir * 5);
                if (mEyePos.x != 0.0f)
                {
                    if (mEyePos.x > 650.0f)
                    {
                        mEyePos.x = 650.0f;
                        mEyeDir = -1;
                    }
                    else if (mEyePos.x < 0.0f)
                    {
                        mEyePos.x = 0.0f;
                        mEyeDir = 1;
                    }
                }
            }
            else if (!mBoard.mFlagBA)
            {
                mThinkCount++;
            }
            if (mBoard.mFlagBA)
            {
                return;
            }
            if (mThinkCount == mThinkFrames)
            {
                ChooseShot();
            }
            else if (mThinkFrames < mThinkCount)
            {
                if (mThinkCount - mThinkFrames > 0x13)
                {
                    LogicMgr aLogicMgr = mBoard.mLogicMgr;
                    Gun aGun = mBoard.mGun;
                    if (aGun.mAngle != mAngle)
                    {
                        float aTarget = mAngle;
                        float aCur = aGun.mAngle;
                        if (aTarget < 1.5707964f)
                        {
                            aTarget += 6.2831855f;
                        }
                        if (aCur < 1.5707964f)
                        {
                            aCur += 6.2831855f;
                        }
                        float aStep = SexyUtil.ClampRange((float)((double)Math.Abs(aTarget - aCur) * 0.10000000149011612), 0.02f, 0.4f);
                        if (aCur < aTarget)
                        {
                            aCur += aStep;
                            if (aTarget <= aCur)
                            {
                                aCur = SexyUtil.GetCanonicalAngleRad(aTarget);
                            }
                        }
                        else
                        {
                            aCur -= aStep;
                            if (aCur <= aTarget)
                            {
                                aCur = SexyUtil.GetCanonicalAngleRad(aTarget);
                            }
                        }
                        aGun.SetAngle(aCur, true);
                        aLogicMgr.mGunAngle = aCur;
                    }
                }
                MoveXToY(ref mEyePos.x, mEyeTarget.x, 10);
                MoveXToY(ref mEyePos.y, mEyeTarget.y, 10);
            }
            if (mThinkFrames + mDelayFrames <= mThinkCount)
            {
                LogicMgr aLogicMgr = mBoard.mLogicMgr;
                mBoard.mGun.SetAngle(mAngle, true);
                aLogicMgr.mGunAngle = mAngle;
                mThinking = false;
                aLogicMgr.mZenThinking = false;
                if (aLogicMgr.mState != 10)
                {
                    mBoard.mLogicMgr.mFlagD8 = true;
                }
            }
        }

        private static void MoveXToY(ref float theX, float theY, int theStep)
        {
            if (theX < theY)
            {
                theX += (float)theStep;
                if (theY < theX)
                {
                    theX = theY;
                }
            }
            else if (theX > theY)
            {
                theX -= (float)theStep;
                if (theX < theY)
                {
                    theX = theY;
                }
            }
        }

        /// <summary>Thinking time is up: rank the simulated shots and take the best one.</summary>
        private void ChooseShot()
        {
            LogicMgr aLogicMgr = mBoard.mLogicMgr;
            bool isDuel = aLogicMgr.mDuelMode;
            Gun aGun = mBoard.mGun;
            AIBall aBest = null;
            int aBestScore = 0;
            bool aMustScore = aLogicMgr.mFlag18;
            bool aBestGoalFirst = false;
            bool aBestGoalEarly = false;
            bool aBestGoalSoon = false;
            List<AIBall> aCandidates = new List<AIBall>();
            foreach (Ball aBallBase in mBalls)
            {
                AIBall aBall = (AIBall)aBallBase;
                if (aBall.mIsMultiballChild)
                {
                    continue;
                }
                bool hitOtherFirst = aBall.mPegHitTimes[0] != aBall.mHitTimes[0];
                bool goalFirst = aBall.mPegHitTimes[0] == aBall.mGoalHitTimes[0];
                bool goalEarly = false;
                if (aBall.mGoalHitTimes[0] <= aBall.mPegHitTimes[1])
                {
                    goalEarly = aBall.mGoalHitTimes[0] < aBall.mPegHitTimes[0] + 0x19;
                }
                bool powerupEarly = false;
                if (aBall.mPowerupHitTime >= 0 && aBall.mPowerupHitTime <= aBall.mPegHitTimes[1])
                {
                    powerupEarly = aBall.mPowerupHitTime < aBall.mPegHitTimes[0] + 0x14;
                }
                bool goalSoon = false;
                if (aBall.mPegHitTimes[2] == -1 || aBall.mGoalHitTimes[0] <= aBall.mPegHitTimes[2] + 0x19)
                {
                    goalSoon = aBall.mGoalHitTimes[0] - aBall.mPegHitTimes[0] < 0x96;
                }
                if (mHasFireball && aBall.mGoalHitTimes[0] < aBall.mHitTimes[0] + 200 && aBall.mGoalHitTimes[0] >= 0)
                {
                    goalEarly = true;
                    goalFirst = true;
                    aBall.mGoalPegsHit = 1;
                }
                if (aMustScore || (AIBall.mPowerup == 8 && powerupEarly))
                {
                    goalEarly = true;
                    goalFirst = true;
                    aBall.mGoalPegsHit = 1;
                }
                if (mHasPowerup1)
                {
                    hitOtherFirst = false;
                    if (!goalFirst)
                    {
                        goalFirst = aBall.mGoalHitTimes[0] <= aBall.mPegHitTimes[1] && aBall.mGoalHitTimes[0] < aBall.mPegHitTimes[0] + 0x32;
                    }
                }
                if (aBall.mScore < aBall.mBombScore)
                {
                    aBall.mScore = aBall.mBombScore;
                    aBall.mGoalPegsLeft = aBall.mBombGoalPegsLeft;
                    if (aBall.mGoalPegsHit < 1)
                    {
                        aBall.mGoalPegsHit = 1;
                    }
                }
                else
                {
                    aBall.mBombTime = -1;
                }
                if (aBall.mGoalPegsLeft == 0 && aBall.mGoalPegsHit > 0 && !aLogicMgr.mClearLevelMode)
                {
                    // Clearing the level is worth a lot, more so when it wins a duel.
                    uint aBonus = 25000;
                    if (isDuel && aLogicMgr.mScore[1 - aLogicMgr.mCurPlayer] < aLogicMgr.mScore[aLogicMgr.mCurPlayer] + 10000)
                    {
                        aBonus = 100000;
                    }
                    if (mSkillLevel < 10 && !goalFirst && !goalEarly && (mSkillLevel == 0 || !goalSoon))
                    {
                        aBonus /= 20;
                    }
                    aBall.mScore = (int)aBonus + aBall.mScore;
                }
                else if (!isDuel && aBest != null && mSkillLevel > 9 && aBall.mGoalPegsHit < aBest.mGoalPegsHit)
                {
                    if (aBall.mScore < 8000 || (float)aBall.mScore < (float)aBest.mScore * 1.5f)
                    {
                        if (aBall.mScore < 25000)
                        {
                            aBall.mScore = 0;
                        }
                    }
                }
                if (mSkillLevel < 10)
                {
                    bool aDouble = !hitOtherFirst;
                    if (goalFirst)
                    {
                        aBall.mScore *= 20;
                    }
                    else if (goalEarly)
                    {
                        aBall.mScore *= 10;
                    }
                    else if (goalSoon)
                    {
                        aBall.mScore *= 3;
                    }
                    else
                    {
                        aDouble = false;
                    }
                    if (aDouble)
                    {
                        aBall.mScore <<= 1;
                    }
                    else if (mSkillLevel < 0)
                    {
                        aBall.mScore /= 5;
                    }
                    if (mSkillLevel < 0 && goalFirst)
                    {
                        aBall.mScore *= 10;
                    }
                }
                if (isDuel && aBall.mGoalPegsHit == 0)
                {
                    aBall.mScore = 0;
                }
                if (mMode == 2 && aBall.mScore > 0)
                {
                    // The stealth helper must agree with the guide the player can see.
                    int aMaxLen = aGun.GetMaxGuideLength();
                    int aGuideHit = aGun.mGuideHitIdx;
                    if (aGuideHit < 0)
                    {
                        aGuideHit = aMaxLen + 1;
                    }
                    int aFirstHit = aBall.mHitTimes[0];
                    if (aMaxLen < aGuideHit && aFirstHit <= aMaxLen)
                    {
                        if (!aBall.mHitMovingFirst)
                        {
                            aBall.mScore = 0;
                        }
                    }
                    else if (aGuideHit <= aMaxLen && Math.Abs(aFirstHit - aGuideHit) > 5)
                    {
                        bool aMismatch = true;
                        if (!aGun.mGuideHitMoving)
                        {
                            if (aBall.mHitMovingFirst && aFirstHit <= aGuideHit)
                            {
                                aMismatch = false;
                            }
                        }
                        else if (!aBall.mHitMovingFirst)
                        {
                            aMismatch = aFirstHit < aGuideHit;
                        }
                        else
                        {
                            aMismatch = false;
                        }
                        if (aMismatch)
                        {
                            aBall.mScore = 0;
                        }
                    }
                    if (Math.Abs(aBall.mHoleTime - aGun.mGuideHoleIdx) > 5 && aBall.mHoleTime <= aGuideHit)
                    {
                        aBall.mScore = 0;
                    }
                }
                if (goalFirst && !hitOtherFirst && mSkillLevel < 0 && aBall.mScore > 500)
                {
                    aCandidates.Add(aBall);
                }
                if (aBestScore < aBall.mScore)
                {
                    aBestScore = aBall.mScore;
                    aBest = aBall;
                    aBestGoalFirst = goalFirst;
                    aBestGoalEarly = goalEarly;
                    aBestGoalSoon = goalSoon;
                }
            }
            int aBombTime = -1;
            const int aChance = 100;
            if (aCandidates.Count != 0 && mSkillLevel < 0)
            {
                // Easy opponents settle for any decent shot.
                if (SexyUtil.Rand() % 100 < aChance)
                {
                    aBest = aCandidates[SexyUtil.Rand() % aCandidates.Count];
                }
            }
            if (aBest == null)
            {
                mPartnerBall = null;
                mBestBall = null;
                CalcDefAngle();
            }
            else
            {
                mAngle = aBest.mAngle;
                mBestBall = aBest;
                mPartnerBall = aBest.mPartner;
                if (aBest.mBombTime > 0)
                {
                    aBombTime = aBest.mBombTime;
                }
            }
            bool aJitter = false;
            if (mSkillLevel < 1)
            {
                aJitter = aBombTime < 0;
            }
            if (mSkillLevel < 10 && !aBestGoalFirst && !aBestGoalEarly && !aBestGoalSoon)
            {
                aJitter = true;
            }
            if (!aJitter && !mHasPowerup1 && mSkillLevel < 10 && SexyUtil.Rand() % 100 < 0x19)
            {
                aJitter = true;
            }
            if (aJitter)
            {
                float aDelta = mSkillLevel < 0 ? 0.001f : 0.0001f;
                if (SexyUtil.Rand() % 2 != 0)
                {
                    aDelta *= -1.0f;
                }
                if (mHasPowerup1 && SexyUtil.Rand() % 100 < 0x46)
                {
                    aDelta = 0.0f;
                }
                mAngle += aDelta;
            }
            mEyePos = mBoard.mCharacterMgr.GetLastLookPos();
            float anAngle = mAngle;
            mEyeTarget = new SexyVector2(aGun.mCenterX + (float)Math.Cos(anAngle) * 200.0f,
                aGun.mCenterY - (float)Math.Sin(anAngle) * 200.0f);
            if (aBombTime >= 0)
            {
                aLogicMgr.mInt1a0 = aBombTime;
            }
        }

        public void Draw(Graphics g)
        {
            if (!mShowPath || mBestBall == null)
            {
                return;
            }
            g.SetColor(SexyUtil.MakeColor(0xffff));
            foreach (SexyVector2 aPoint in mBestBall.mPath)
            {
                g.FillRect((int)aPoint.x, (int)aPoint.y, 1, 1);
            }
            if (mPartnerBall != null)
            {
                g.SetColor(SexyUtil.MakeColor(0xffff));
                foreach (SexyVector2 aPoint in mPartnerBall.mPath)
                {
                    g.FillRect((int)aPoint.x, (int)aPoint.y, 1, 1);
                }
            }
        }
    }
}
