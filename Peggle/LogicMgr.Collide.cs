using System;
using System.Collections.Generic;

namespace Sexy
{
    public partial class LogicMgr
    {
        public const int STATE_POST_SHOT = 3;

        /// <summary>Processes the collisions the CollisionMgr gathered this frame: pegs, holes, walls, flippers and sounds.</summary>
        public void CheckCollisions()
        {
            List<CollisionInfo> aCollisions = mBoard.mCollisionMgr.mCollisionInfo;
            if (mDimLowObjects && mBoard.mSlowMo && mBoard.mSlowMoTargetRate == 0)
            {
                bool allSettled = true;
                foreach (Ball aBall in mBoard.mBalls)
                {
                    if (aBall.mY < 400.0f)
                    {
                        allSettled = false;
                        break;
                    }
                    float aSpeedSq = aBall.mVelY * aBall.mVelY + aBall.mVelX * aBall.mVelX;
                    if (!(aSpeedSq <= 9.0f))
                    {
                        allSettled = false;
                        break;
                    }
                }
                if (allSettled && mBoard.mSlowMo && mBoard.mSlowMoTargetRate == 0)
                {
                    mBoard.mSlowMoTargetRate = 1000;
                    mBoard.mSlowMoRateStep = 1;
                }
            }
            for (int i = 0; i < aCollisions.Count; i++)
            {
                CollisionInfo anInfo = aCollisions[i];
                Ball aBall = anInfo.mBall;
                PhysObj anObj = anInfo.mObj;
                // Objects with logic (e.g. "movetoggle") are told about the hit (the original tests mLogic,
                // offset 0x60, not the id).
                if (anObj.mLogic.Length != 0)
                {
                    anObj.NotifyCollision(aBall);
                }
                if (anObj.mId == "flipper")
                {
                    aBall.mFlagFC = true;
                    if (KILLBALL_TIME3 < mInt38)
                    {
                        mInt38 = KILLBALL_TIME3;
                    }
                }
                else if (anObj.mId == "wall")
                {
                    if (aBall.mValue140 < 1)
                    {
                        aBall.mValue140 = mStateCount;
                    }
                    else if (mStateCount - aBall.mValue140 > 9)
                    {
                        aBall.mValue144++;
                    }
                }
                else if (anObj.mId == "freeballcover")
                {
                    if (aBall.mFireball)
                    {
                        aBall.mFlagFD = true;
                        bool covered = false;
                        if (mBoard.mTrophyInfo != null)
                        {
                            covered = mBoard.mTrophyInfo.mFreeballCovered;
                        }
                        if (!covered)
                        {
                            anObj.SetActiveWithGrowAnim(false);
                        }
                        mBoard.mSoundMgr.AddSound(Res.SOUND_FIREBALLBOUNCE);
                    }
                }
                else if (anObj.mId == "lbump")
                {
                    mBoard.mEffectMgr.AddLevelBumper(anObj);
                }
                if (anObj.mShapeType == PhysObj.SHAPE_HOLE)
                {
                    HoleHit((Hole)anObj, aBall);
                }
                else if (anObj.mPegInfo == null)
                {
                    if (anObj.mId == "bumperhole")
                    {
                        aBall.mFlag124 = true;
                    }
                    else if (anObj.mId == "freeball")
                    {
                        if (aBall.mFireball)
                        {
                            mBoard.mSoundMgr.AddSound(Res.SOUND_FIREBALLBOUNCE);
                        }
                        if (!(aBall.mX <= 15.0f) && aBall.mX < (float)(mBoard.mBoardRect.mWidth + -0xf))
                        {
                            if (aBall.mValue138 < 1)
                            {
                                aBall.mValue138 = mStateCount;
                            }
                            else if (mStateCount - aBall.mValue138 > 9)
                            {
                                aBall.mValue13C++;
                            }
                        }
                    }
                }
                else
                {
                    PegHit(aBall, anObj, false);
                }
                if (anObj.mSound != 0 && (aBall.mBounceCount > 4 || anObj.mShapeType == PhysObj.SHAPE_HOLE))
                {
                    int aSound = -1;
                    if (anObj.mSound == 4)
                    {
                        aSound = !aBall.mFireball ? Res.SOUND_RICHOCHET : Res.SOUND_FIREBALLBOUNCE;
                    }
                    if (aSound > -1)
                    {
                        mBoard.mSoundMgr.AddSound(aSound);
                    }
                }
                mFlagA0 = true;
            }
            aCollisions.Clear();
        }

        /// <summary>The crowd sighs when the last orange peg is narrowly missed.</summary>
        public void DoFeverMissed()
        {
            if (mSighSound == null)
            {
                mSighSound = mBoard.mApp.mSoundManager.GetSoundInstance((uint)Res.SOUND_SIGH);
            }
            if (mSighSound != null)
            {
                mSighSound.Play(false, false);
            }
        }

        /// <summary>Slow motion as the ball nears the last orange peg.</summary>
        public void DoFeverSlow()
        {
            mBoard.SetSlowMo(true, 100);
            mBoard.mSlowMoHold = 100;
            mBoard.mZoomIn = true;
            mBoard.mFeverCam = true;
            mBoard.mSoundMgr.FadeMusic(0x4b);
            if (mSighSound != null)
            {
                mBoard.mSoundMgr.AddFadeSound(mSighSound);
                mSighSound = null;
            }
            mBoard.mSoundMgr.AddSound(Res.SOUND_SLOWMO);
            if (mBoard.mBalls.Count == 1 && !mSlowMoSoundPlaying)
            {
                Ball aBall = mBoard.mBalls[0];
                if (1.0f <= aBall.mVelX * aBall.mVelX + aBall.mVelY * aBall.mVelY)
                {
                    mSlowMoSoundPlaying = true;
                }
            }
        }

        /// <summary>After a shot: pops the hit pegs one at a time while the shot-score text counts up.</summary>
        public void RemoveHitPegs()
        {
            if (mList39c.Count == 0 && mInt10 <= 0)
            {
                return;
            }
            int aLeft = mList39c.Count;
            if (mInt10 < 1)
            {
                PhysObj aPeg = mList39c[0];
                mVec384 = new SexyVector2(aPeg.GetXPos(), aPeg.GetYPos());
                if (aPeg.mVisible)
                {
                    aPeg.mPegInfo.DoFlash(1);
                    aPeg.mDisappearCounter = 4;
                }
                mList39c.RemoveAt(0);
                aLeft--;
            }
            else
            {
                aLeft = mInt10 + aLeft;
                mInt10--;
            }
            if (aLeft > 0)
            {
                mInt0c = SexyUtil.ClampRange(8 - aLeft / 10, 1, 5);
            }
            mBoard.mSoundMgr.AddSound(Res.SOUND_PEGPOP);
            mInt14++;
            if (mInt3c != 0)
            {
                FloatingText aScoreText = mBoard.mFloatingTextMgr.GetTextById(mInt3c);
                FloatingText aCountText = mBoard.mFloatingTextMgr.GetTextById(mInt178);
                FloatingText aThirdText = mBoard.mFloatingTextMgr.GetTextById(mInt40);
                if (aScoreText != null)
                {
                    int aCount = mInt188 - mList39c.Count - mInt10;
                    if (mFlag18)
                    {
                        aCount = mInt14;
                        if (mInt188 < aCount)
                        {
                            aCount = mInt188;
                        }
                    }
                    if (aCount < 1)
                    {
                        aCount = 1;
                    }
                    if (!mFlag18 && mDuelMode)
                    {
                        aScoreText.mUpdateCnt = -1000;
                        if (aCountText != null)
                        {
                            aCountText.mUpdateCnt = -1000;
                        }
                        if (aThirdText != null)
                        {
                            aThirdText.mUpdateCnt = -1000;
                        }
                    }
                    else
                    {
                        int aBonus = mShotScore - mInt164 * mInt188;
                        if (aBonus < 0)
                        {
                            aBonus = 0;
                        }
                        if (!mDimLowObjects)
                        {
                            aScoreText.SetText(SexyUtil.CommaSeperate(mInt164 * aCount + aBonus));
                            if (aCountText != null)
                            {
                                string aFormat = aCount == 1 ? SexyAppBase.Loc("%s x %d Peg") : SexyAppBase.Loc("%s x %d Pegs");
                                aCountText.SetText(CFormat.StrFormat(aFormat, SexyUtil.CommaSeperate(mInt164), aCount));
                            }
                        }
                    }
                }
            }
            if (KILLBALL_TIME3 < mInt38)
            {
                mInt38 = KILLBALL_TIME3;
            }
            foreach (Ball aBall in mBoard.mBalls)
            {
                aBall.mStuckCounter = 0;
            }
        }
    }
}
