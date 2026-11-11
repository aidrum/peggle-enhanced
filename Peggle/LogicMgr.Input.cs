using System;
using System.Collections.Generic;

namespace Sexy
{
    public partial class LogicMgr
    {
        public bool MouseMove(int x, int y)
        {
            if (mState != STATE_NONE && mIntD4 == 0 && !mIsComputer[mCurPlayer])
            {
                double aH = (double)SexyUtil.gPhysicalDisplayHeight;
                mMouseX = (int)((double)x * 600.0 / aH);
                mMouseY = (int)((double)y * 600.0 / aH);
                mMouseFracX = 0;
                mMouseFracY = 0;
                mWheelMove = GetBaseMouseWheelMove();
                mBoard.mGun.mFirstHitDeflect = 1000.0f;
                mFlag1F = true;
            }
            return true;
        }

        public bool MouseDrag(int x, int y)
        {
            double aH = (double)SexyUtil.gPhysicalDisplayHeight;
            int aX = (int)((double)x * 600.0 / aH);
            int aY = (int)((double)y * 600.0 / aH);
            // Port: a leftover sub-pixel part (MouseDragFine) counts as a move, so it's dropped.
            if (mIntD4 == 0 && !mIsComputer[mCurPlayer] && (aX != mMouseX || aY != mMouseY || mMouseFracX != 0 || mMouseFracY != 0))
            {
                mMouseX = aX;
                mMouseY = aY;
                mMouseFracX = 0;
                mMouseFracY = 0;
                mWheelMove = GetBaseMouseWheelMove();
                mBoard.mGun.mFirstHitDeflect = 1000.0f;
                mFlag1F = true;
            }
            return true;
        }

        /// <summary>
        /// Port addition (PC Controls, zoomed in): MouseDrag at a fractional display point, keeping the
        /// sub-pixel part of the PC position (mMouseFracX/Y) so aiming isn't locked to the PC pixel grid.
        /// </summary>
        public bool MouseDragFine(double x, double y)
        {
            double aH = (double)SexyUtil.gPhysicalDisplayHeight;
            double aPX = x * 600.0 / aH;
            double aPY = y * 600.0 / aH;
            int aX = (int)Math.Floor(aPX);
            int aY = (int)Math.Floor(aPY);
            float aFracX = (float)(aPX - aX);
            float aFracY = (float)(aPY - aY);
            if (mIntD4 == 0 && !mIsComputer[mCurPlayer] &&
                (aX != mMouseX || aY != mMouseY || aFracX != mMouseFracX || aFracY != mMouseFracY))
            {
                mMouseX = aX;
                mMouseY = aY;
                mMouseFracX = aFracX;
                mMouseFracY = aFracY;
                mWheelMove = GetBaseMouseWheelMove();
                mBoard.mGun.mFirstHitDeflect = 1000.0f;
                mFlag1F = true;
            }
            return true;
        }

        /// <summary>
        /// A tap/click: fires, flips the flippers, speeds up a level intro, triggers a time bomb, or
        /// dismisses the end-of-level reward. theButton &lt; 0 is a right click / second finger.
        /// </summary>
        public bool MouseDown(int x, int y, int theButton, bool fromReplay, bool isAI)
        {
            double aH = (double)SexyUtil.gPhysicalDisplayHeight;
            mMouseX = (int)((double)x * 600.0 / aH);
            mMouseY = (int)((double)y * 600.0 / aH);
            mMouseFracX = 0;
            mMouseFracY = 0;
            bool ai = isAI;
            if (theButton < 0 && mState != STATE_INIT_LEVEL)
            {
                CalcCornerDisplay();
                return false;
            }
            if (mMuteLoops && !fromReplay)
            {
                return true;
            }
            if (mMuteLoops)
            {
                ai = true;
            }
            if (mState != STATE_LEVEL_DONE || !mFlag155 || mStateCount > 99)
            {
                bool recorded = false;
                if (!mMuteLoops && mState == STATE_SHOOTING)
                {
                    int aTime = mStateCount;
                    if (ai)
                    {
                        aTime = aTime + -1;
                    }
                    mClicks.Add(new ClickInfo(aTime, false, theButton < 0));
                    recorded = true;
                }
                if (theButton > 0)
                {
                    bool handled = true;
                    if (mState == STATE_AIMING && !mIsComputer[mCurPlayer])
                    {
                        if (mGuideCount == 0)
                        {
                            mFlagD9 = true;
#if !PEGGLE_IPHONE
                            mBoard.mReplayButtonState = 0;
#endif
                        }
                    }
                    else if (mState == STATE_SHOOTING && mFlag1D)
                    {
                        mFlag1D = KILLBALL_TIME1 < mInt38;
                        mFlagE3 = true;
                    }
                    else if (mState == STATE_SHOOTING)
                    {
                        if (!mBoard.mSlowMo)
                        {
                            if (!mIsComputer[mCurPlayer] || ai)
                            {
                                handled = false;
                            }
                            else if (mClicks.Count != 0 && recorded)
                            {
                                mClicks.RemoveAt(mClicks.Count - 1);
                            }
                        }
                        else
                        {
                            handled = false;
                            if (!mMuteLoops)
                            {
                                mFeverPending = false;
                                if (mBoard.mSlowMoHold != 0)
                                {
                                    mBoard.mSlowMoHold = 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (mClicks.Count != 0 && recorded)
                        {
                            mClicks.RemoveAt(mClicks.Count - 1);
                        }
                        SpeedTransition();
                    }
                    if (handled)
                    {
                        return true;
                    }
                    if (mFlag20)
                    {
                        mFlag20 = false;
                        PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
                        if (!mMuteLoops && aPlayer != null && aPlayer.mDuelRounds < 100)
                        {
                            aPlayer.mDuelRounds++;
                            aPlayer.mDirty = true;
                        }
                    }
                    mFlag1E = true;
                }
                if (mPowerupCount[2][mCurPlayer] < 1)
                {
                    if (mState == STATE_SHOOTING && mPowerupCount[11][mCurPlayer] > 0)
                    {
                        DoTimeBomb();
                    }
                    else if (mState == STATE_SHOOTING)
                    {
                        if (mBoard.mApp.GetMaxLevel() == 0 && theButton > 0 && !mDimLowObjects && !mBoard.mSlowMo)
                        {
                            if (mBoard.mFloatingTextMgr.GetTextById(mTurnTextId) == null)
                            {
                                Gun aGun = mBoard.mGun;
                                FloatingText aText = AddStandardText(SexyAppBase.Loc("You can only fire ^00FF00^one ball^oldclr^ at a time!^oldclr^"),
                                    aGun.mCenterX, aGun.mCenterY + 130.0f, 6);
                                aText.mColor = 0xffffff;
                                aText.mVelY = 0;
                                aText.mDuration = 200;
                                aText.mFadeIn = 10;
                                aText.mFadeOut = 0x1e;
                                aText.mBackShade = true;
                                mTurnTextId = aText.mId;
                            }
                            mBoard.mSoundMgr.AddSound(Res.SOUND_COINSPIN_NO);
                            return true;
                        }
                    }
                }
                else
                {
                    FlipperClick(true);
                }
                return true;
            }
            // End of level: a tap skips the reward animations and moves on.
            if (ThunderballApp.mGameLevelPack == 1)
            {
                mStateCount = 100;
                mBoard.mFloatingTextMgr.EraseText(mTurnTextId);
                mTurnTextId = 0;
                List<Effect> aRibbons = new List<Effect>();
                mBoard.mEffectMgr.GetEffectsByType(aRibbons, 7);
                if (aRibbons.Count != 0)
                {
                    if (aRibbons.Count == 2)
                    {
                        for (int i = 0; i < aRibbons.Count; i++)
                        {
                            aRibbons[i] = mBoard.mEffectMgr.SetPriority(aRibbons[i], 2);
                            aRibbons[i].mDuration = aRibbons[i].mUpdateCnt + 0x14;
                            if (aRibbons[i].mFlags == 1)
                            {
                                aRibbons[i].mAlpha = -1;
                            }
                        }
                    }
                    else if (aRibbons.Count == 1)
                    {
                        aRibbons[0] = mBoard.mEffectMgr.SetPriority(aRibbons[0], 2);
                        aRibbons[0].mDuration = aRibbons[0].mUpdateCnt + 0x14;
                    }
                    bool skipStory = mBoard.mApp.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS &&
                        mBoard.mApp.mPlayerInfo.JustBeatAdventure() && mBoard.mApp.mPlayerInfo.mNightsAdventureWins == 1;
                    if (!skipStory && mBoard.mEndLevelDialog != null && mBoard.mEndLevelDialog.mTrophyWon)
                    {
                        mBoard.mApp.ShowStoryScreen(true, false);
                    }
                }
                Effect aBadge = mBoard.mEffectMgr.GetEffectByType(0x23);
                if (aBadge != null)
                {
                    aBadge.mDuration = aBadge.mUpdateCnt + 0x14;
                    if (mBoard.mEndLevelDialog == null || !mBoard.mEndLevelDialog.mTrophyWon)
                    {
                        mBoard.mApp.ShowTrophyScreen(false);
                    }
                    else
                    {
                        mBoard.mApp.ShowStoryScreen(true, false);
                    }
                }
            }
            else
            {
                mStateCount = 100;
                mBoard.mFloatingTextMgr.EraseText(mTurnTextId);
                mTurnTextId = 0;
                bool tryTrophy = true;
                if (mBoard.mApp.mGameMode == GameMode.GAMEMODE_ADVENTURE)
                {
                    tryTrophy = !mBoard.mApp.mPlayerInfo.JustBeatAdventure() || mBoard.mApp.mPlayerInfo.mAdventureWins != 1;
                }
                if (tryTrophy && mBoard.mApp.TryShowNewTrophy())
                {
                    return true;
                }
                Effect aRibbon = mBoard.mEffectMgr.GetEffectByType(7);
                if (aRibbon != null)
                {
                    aRibbon = mBoard.mEffectMgr.SetPriority(aRibbon, 2);
                    aRibbon.mDuration = aRibbon.mUpdateCnt + 0x14;
                }
                Effect aBadge = mBoard.mEffectMgr.GetEffectByType(0x23);
                if (aBadge != null)
                {
                    aBadge.mDuration = aBadge.mUpdateCnt + 0x14;
                    mBoard.mApp.ShowTrophyScreen(false);
                }
            }
            return true;
        }

        /// <summary>Tapping during the level intro: shows every peg at once and settles the interface.</summary>
        public void SpeedTransition()
        {
            if (mState != STATE_INIT_LEVEL)
            {
                return;
            }
            mFlag158 = true;
            foreach (PhysObj anObj in mList394)
            {
                if (anObj.mCollidable)
                {
                    anObj.mVisible = true;
                    anObj.mPegInfo.mFlashCount = 0;
                }
            }
            if (mTurnTextId != 0)
            {
                mBoard.mFloatingTextMgr.EraseText(mTurnTextId);
                mTurnTextId = 0;
            }
            if (mInt3c != 0)
            {
                mBoard.mFloatingTextMgr.EraseText(mInt3c);
                mInt3c = 0;
            }
            if (mInt40 != 0)
            {
                mBoard.mFloatingTextMgr.EraseText(mInt40);
                mInt40 = 0;
            }
            if (mInt178 != 0)
            {
                mBoard.mFloatingTextMgr.EraseText(mInt178);
                mInt178 = 0;
            }
            mBoard.mInterfaceMgr.SettleDown();
            mBoard.mInterfaceMgr.SetNumBalls(mBallsLeft[mCurPlayer]);
        }

        public bool MouseUp(int x, int y, int theButton, bool isAI)
        {
            bool ai = isAI;
            if (mMuteLoops)
            {
                ai = true;
            }
            bool recorded = false;
            if (!mMuteLoops && mState == STATE_SHOOTING)
            {
                mClicks.Add(new ClickInfo(mStateCount, true, theButton < 0));
                recorded = true;
            }
            bool wasDown = mFlag1E;
            if (theButton > 0)
            {
                mFlag1E = false;
            }
            if (!mIsComputer[mCurPlayer] || ai)
            {
                if (mPowerupCount[2][mCurPlayer] > 0 && theButton > 0 && (theButton < 0 || wasDown))
                {
                    FlipperClick(false);
                }
                CalcCornerDisplay();
            }
            else if (recorded && mClicks.Count != 0)
            {
                mClicks.RemoveAt(mClicks.Count - 1);
            }
            return true;
        }

        /// <summary>Fine aiming with the wheel; finer still while a Super Guide line hits something close.</summary>
        public bool MouseWheel(float theDelta)
        {
            if (mIsComputer[mCurPlayer] || mState != STATE_AIMING)
            {
                return true;
            }
            float aMove = GetBaseMouseWheelMove();
            Gun aGun = mBoard.mGun;
            if (aGun.mDoBouncyGuide && aGun.mFirstHitDeflect <= 1.0f && Math.Abs((double)theDelta) <= 1.0)
            {
                aMove = SexyUtil.ClampRange(mWheelMove * (0.02f / aGun.mFirstHitDeflect), 0.01f, 0.1f);
            }
            mWheelMove = aMove;
            SetGunAngle(mGunAngle + theDelta * (aMove * 0.017453292f));
            mIntD4 = 0x1e;
            aGun.mNeedFirstHit = true;
            return true;
        }

        public bool KeyChar(char theChar)
        {
            return false;
        }

        public bool KeyDown(KeyCode theKey)
        {
            return false;
        }
    }
}
