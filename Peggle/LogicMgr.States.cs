using System;
using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    public partial class LogicMgr
    {
        public const int STATE_CHARACTER_DIALOG = 9;

        /// <summary>When every ball has stopped (or a tap forced it), starts popping the hit pegs or ends the shot.</summary>
        public void CheckBallStop()
        {
            if (mInt0c != 0)
            {
                return;
            }
            if (!mFlagE3)
            {
                foreach (Ball aBall in mBoard.mBalls)
                {
                    if (!aBall.mIsPeg && aBall.mStuckCounter < 0x32)
                    {
                        return;
                    }
                }
            }
            else
            {
                mFlagE3 = false;
            }
            foreach (Ball aBall in mBoard.mBalls)
            {
                aBall.mStuckCounter = 0;
            }
            if (mList39c.Count != 0)
            {
                mInt14 = mInt188 - mList39c.Count;
                mInt10 = 0;
                RemoveHitPegs();
            }
            else
            {
                mBoard.KillAllBalls();
            }
        }

        /// <summary>Every other frame, moves the free-ball movers' radius one step towards mFreeBallRadius.</summary>
        public void UpdateFreeBallRadius()
        {
            if (mBoard.mGameUpdateCnt % 2 == 0)
            {
                return;
            }
            bool moving = false;
            foreach (PhysObj anObj in mBoard.mPhysObjs)
            {
                if ((anObj.mId == "freeballcover" || anObj.mId == "freeball" || anObj.mId == "bumperhole") && anObj.mMover != null)
                {
                    Mover aMover = anObj.mMover;
                    float aRadius = (float)aMover.mRadius1;
                    if (aRadius != (float)mFreeBallRadius && aMover.mPeriod > 0)
                    {
                        moving = true;
                        float aNew;
                        if (aRadius <= (float)mFreeBallRadius)
                        {
                            aNew = (float)Math.Min((int)(aRadius + 1.0f), mFreeBallRadius);
                        }
                        else
                        {
                            aNew = (float)Math.Max((int)(aRadius - 1.0f), mFreeBallRadius);
                        }
                        aMover.mRadius1 = (int)aNew;
                    }
                }
            }
            if (!moving)
            {
                mFreeBallRadius = 0;
            }
        }

        /// <summary>A Spooky Ball that falls off the bottom wraps around to the top.</summary>
        public void UpdateWrapAroundBall()
        {
            foreach (Ball aBall in new List<Ball>(mBoard.mBalls))
            {
                float aTop = aBall.mY - aBall.mRadius;
                if (aBall.mFlag11C && aTop > (float)mBoard.mBoardRect.mHeight)
                {
                    DoWrapAround(aBall);
                }
            }
        }

        /// <summary>Lets the intro texts finish: erases ones still waiting, fast-forwards the rest.</summary>
        public void FinishInitLevelText()
        {
            if (mTurnTextId != 0)
            {
                FloatingText aText = mBoard.mFloatingTextMgr.GetTextById(mTurnTextId);
                if (aText != null)
                {
                    if (aText.mUpdateCnt < 0)
                    {
                        mBoard.mFloatingTextMgr.EraseText(aText.mId);
                    }
                    else if (aText.mUpdateCnt < 1000)
                    {
                        aText.mUpdateCnt = 1000;
                    }
                }
                mTurnTextId = 0;
            }
            if (mInt3c != 0)
            {
                FloatingText aText = mBoard.mFloatingTextMgr.GetTextById(mInt3c);
                if (aText != null)
                {
                    if (aText.mUpdateCnt < 0)
                    {
                        mBoard.mFloatingTextMgr.EraseText(aText.mId);
                    }
                    else if (aText.mUpdateCnt < 1000)
                    {
                        aText.mUpdateCnt = 1000;
                    }
                }
                mInt3c = 0;
            }
        }

        public void FinishInitLevel()
        {
            FinishInitLevelText();
            int aMode = mBoard.mGameMode;
            if (aMode == GameMode.GAMEMODE_ADVENTURE || aMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS || aMode == GameMode.GAMEMODE_QUICKPLAY)
            {
                // The original picks a level-intro text here, but this build never sets one.
                if (mBoard.mApp.OnMaxLevel())
                {
                    mBoard.mApp.GetMaxLevel();
                }
            }
            BeginTurn(false);
        }

        public void CheckDoCharacterDialog()
        {
            bool scrollIn = !mFlag158;
            mFlag158 = false;
            if (mBoard.mTrophyCharacter)
            {
                FinishInitLevel();
                return;
            }
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            if (aPlayer != null)
            {
                int aMode = mBoard.mGameMode;
                if (aMode == GameMode.GAMEMODE_ADVENTURE || aMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
                {
                    // Port: a bonus Adventure has no Master stage: its levels have the character select once
                    // it has been beaten, and the stage intro before.
                    if (ThunderballApp.mBonusAdventure != null ? ThunderballApp.BonusMastersFixed(aPlayer) : mInt1b8 < aPlayer.mLastAdventureStage)
                    {
                        mBoard.DoStageDialog(scrollIn);
                    }
                    else
                    {
                        mBoard.DoCharacterDialog(scrollIn);
                    }
                }
                else if (aMode == GameMode.GAMEMODE_QUICKPLAY || aMode == GameMode.GAMEMODE_DUEL || aMode == GameMode.GAMEMODE_CHALLENGE)
                {
                    mBoard.DoCharacterDialog(scrollIn);
                }
            }
            SetState(STATE_CHARACTER_DIALOG);
        }

        /// <summary>The level intro: pegs appear one by one, then the duel rules (once) and the character dialog.</summary>
        public void UpdateInitLevel()
        {
            if (mBoard.mShowSpaceCracks && mStateCount <= 0x1d)
            {
                return;
            }
            int aShown = 0;
            foreach (PhysObj anObj in mList394)
            {
                if (aShown >= 1)
                {
                    break;
                }
                PegInfo aPeg = anObj.mPegInfo;
                if (!anObj.mVisible && anObj.mCollidable)
                {
                    anObj.mVisible = true;
                    if (aPeg.mType != 1)
                    {
                        aPeg.DoFlash(1);
                    }
                    aShown++;
                }
            }
            if (aShown != 0)
            {
                return;
            }
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            if (!mDuelMode || mFlag21 || aPlayer == null || !aPlayer.mMusicEnabled)
            {
                if (mBoard.mApp.mDialogMap.Count == 0)
                {
                    if (mInt28 < 1)
                    {
                        CheckDoCharacterDialog();
                    }
                    else
                    {
                        mInt28--;
                    }
                }
                return;
            }
            mFlag21 = true;
            ThunderDialog aDialog = (ThunderDialog)mBoard.mApp.DoDialog(DialogId.DIALOG_GAME_TIP, false, SexyAppBase.Loc("Duel Rules"), "", SexyAppBase.Loc("Continue"), 3);
            mBoard.mShowingGameTip = true;
            aDialog.mContentInsets.mBottom = aDialog.mContentInsets.mBottom - DIALOG_DUEL_TUTORIAL_BUTTON_EXTRA_OFFSET_Y;
            aDialog.mCloseOnButton = true;
            aDialog.mTextAlign = 0;
            aDialog.mDrawLinesOnly = true;
            aDialog.mDialogLines += SexyAppBase.Loc("Duel Rules\n\n");
            aDialog.mDialogLines += SexyAppBase.Loc("Players ^00FF00^take turns^oldclr^ shooting the ball.\n\n");
            aDialog.mDialogLines += SexyAppBase.Loc("The goal is to ^00FF00^score more points^oldclr^ than your opponent.\n\n");
            aDialog.mDialogLines += SexyAppBase.Loc("You are ^FF4444^penalized^oldclr^ if you ^00FF00^fail to hit an orange peg^oldclr^ on a shot!");
            aDialog.Resize(LOGICMGR_DUEL_RULES_X + mBoard.mX, LOGICMGR_DUEL_RULES_Y, LOGICMGR_DUEL_RULES_W, LOGICMGR_DUEL_RULES_H);
            aDialog.mAutoSizeYesButton = true;
            mInt28 = 0xf;
            if (!mFlag158)
            {
                aDialog.DoScroll(1);
            }
            else
            {
                mFlag158 = false;
            }
        }

        public void UpdateCharacterDialog()
        {
            if (mStateCount > 1 && mBoard.mInterfaceMgr.mBallsSettled)
            {
                if (!mBoard.mShowingGameTip)
                {
                    FinishInitLevel();
                }
                else if (mStateCount > 199 && mTurnTextId != 0)
                {
                    FinishInitLevelText();
                }
            }
        }

        /// <summary>While aiming: early-level hints that flash the pegs to aim at, and starting the AI's turn.</summary>
        public void UpdatePreShot()
        {
            int aMaxLevel = mBoard.mApp.GetMaxLevel();
            int aShots = mStats[0].mShots;
            if ((mStateCount == 0x32 && aShots == 0) || (mStateCount == 0x8c && aShots > 0))
            {
                bool onMax = mBoard.mApp.OnMaxLevel();
                int aBlinks = 2;
                int aType = 2;
                if (!onMax || aShots != 0)
                {
                    if (aMaxLevel == 0 || aMaxLevel == 1)
                    {
                        aBlinks = 5;
                    }
                }
                else if (aMaxLevel == 0 || aMaxLevel == 1)
                {
                    aBlinks = 5;
                }
                else if (aMaxLevel == 2 || (aMaxLevel % 5 == 0 && aMaxLevel > 0 && aMaxLevel < 0x1a))
                {
                    aType = 4;
                    aBlinks = 5;
                }
                else if (aMaxLevel == 3)
                {
                    aType = 3;
                    aBlinks = 5;
                }
                if (aType == 2 && mGoalPegs.Count > 10 && ((aMaxLevel != 0 && aMaxLevel != 1) || aShots > 0))
                {
                    aType = 0;
                }
                foreach (PhysObj anObj in mList394)
                {
                    if (anObj.mPegInfo != null && anObj.mPegInfo.mType == aType && anObj.mVisible)
                    {
                        anObj.mPegInfo.DoFlashBlink(aBlinks);
                    }
                }
            }
            if (mStateCount == 0x4b && mGoalPegs.Count < 7)
            {
                foreach (PhysObj anObj in mGoalPegs)
                {
                    if (anObj.mPegInfo != null && !anObj.mPegInfo.mHit)
                    {
                        anObj.mPegInfo.DoFlash(1);
                    }
                }
            }
            if (mGuideCount > 1)
            {
                mGuideCount++;
                if (mGuideCount == 0x1e)
                {
                    mGuideCount = 0;
#if !PEGGLE_IPHONE
                    mBoard.mReplayButtonState = 1;
#endif
                }
            }
            int aDelay = 0x96;
            if (mBoard.mReplayWriters.Count == 0)
            {
                aDelay = 100;
            }
            AIMgr anAI = mBoard.mAIMgr;
            if (mIsComputer[mCurPlayer] && aDelay <= mStateCount && !anAI.mThinking)
            {
                anAI.mDelayFrames = 100;
                anAI.mFramesPerUpdate = 1;
                AIMgr.mSkillLevel = mComputerSkill[mCurPlayer];
                anAI.BeginThinking(false, 0.0f, 0, 0);
            }
        }
    }
}
