using System;
using System.Collections.Generic;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The board's frame and HUD (Sexy::InterfaceMgr): the ball tube (Ballotron) with its dial and
    /// replay covers, the fever meter and multiplier bars, the free-ball shot meter, the balls-left
    /// counter and the score marquees. Also loads the per-character interface image group.
    /// </summary>
    public class InterfaceMgr
    {
        public class BallInfo
        {
            public float mY;                                      // 0x0

            public float mVelY;                                   // 0x4

            public BallInfo SyncState(DataSync theSync)
            {
                theSync.SyncFloat(ref mY);
                theSync.SyncFloat(ref mVelY);
                return this;
            }
        }

        public const int BALLOTRON_BALLS = 0;

        public const int BALLOTRON_DIAL = 1;

        public const int BALLOTRON_REPLAY = 2;

        public ThunderballApp mApp;                               // 0x04

        public Board mBoard;                                      // 0x08

        public int mFFwdCover;                                    // 0x0c

        public int mBallotronMode;                                // 0x10

        public int mBallotronState;                               // 0x14

        public int mBallotronCount;                               // 0x18

        public int mFeverStart;                                   // 0x1c

        public int mFeverBlink;                                   // 0x20

        public int mFeverLight;                                   // 0x24

        public int mFeverLightCount;                              // 0x28

        public int mFeverBlinkPos;                                // 0x2c

        public int mFeverCount;                                   // 0x30

        public int mShotMeterDisp;                                // 0x34

        public int mShotMeterTarget;                              // 0x38

        public int mShotMeterScore;                               // 0x3c

        public int mShotMeterBlink;                               // 0x40

        public int mLoadCount;                                    // 0x44

        public int mBallOffset;                                   // 0x48

        public int mBottomBallDelay;                              // 0x4c

        public int mTopScore;                                     // 0x50

        public int mAdventureScore;                               // 0x54

        public int mBallCountBlink;                               // 0x58

        public string mTopScoreName = "";                         // 0x5c

        public int mStageDisp;                                    // 0x60

        public int mLevelDisp;                                    // 0x64

        public int[] mDispScore = new int[2];                     // 0x68

        public int mScoreUpdateDelay;                             // 0x70

        public string[] mPodText = { "", "" };                    // 0x74

        public int[] mPodTextCount = new int[2];                  // 0x7c

        public int[] mScoreBlink = new int[2];                    // 0x84

        public int mVisibleBallsTarget;                           // 0x8c

        public int mVisibleBalls;                                 // 0x90

        public int mButtonCover;                                  // 0x94

        public int mStageNum;                                     // 0x98

        public int mFeverMultBlink;                               // 0x9c

        public int[] mFeverLights = new int[0x19];                // 0xa0

        public bool mBallsSettled;                                // 0x104

        public bool mShotMeterFull;                               // 0x105

        public bool mMarqueesDirty;                               // 0x106

        public string mResGroup = "";                             // 0x108

        public int mBallImageId;                                  // 0x10c

        public int mBallFrameImageId;                             // 0x110

        public int mFeverMeterFrameImageId;                       // 0x114

        public int mLeftWindowFrameImageId;                       // 0x118

        public int mRightWindowFrameImageId;                      // 0x11c

        public int mShooterBackImageId;                           // 0x120

        public int mWheelFrameImageId;                            // 0x124

#if PEGGLE_IPHONE
        // iPhone: the gutter sidebars beside the 960-wide layout (IP group images).
        public int mLeftBarImageId;                               // 0x118 (iPhone)

        public int mRightBarImageId;                              // 0x11c (iPhone)

        public int mLeftHandedBarImageId;                         // 0x120 (iPhone)
#endif

        public List<BallInfo> mBalls = new List<BallInfo>();      // 0x128

        public int mUpdateCnt;                                    // 0x134

        private static readonly int[] FEVER_GLOW_BITS = { 0, 1, 3, 7, 6, 4 };

        private static int xpos1 = INTERFACE_MGR_SHOT_MTR_CLIPL_X;

        private static int xpos2 = INTERFACE_MGR_SHOT_MTR_CLIPR_X;

#if !PEGGLE_IPHONE
        /// <summary>
        /// Port addition (HD left-handed): how far the fever meter moves right and the Ball-o-tron left. Each
        /// frame's body is 100 px wide against the screen edge (fever 0-99, Ball-o-tron 924-1023), so a shift
        /// of BOARD_W - 100 swaps them. (928, centring the tubes where the other's were, looked worse.)
        /// </summary>
        public static int LEFT_HANDED_SHIFT_X = 924;

        /// <summary>Port addition (HD left-handed): the rows of the frames that swap; the top (with the fast-forward and replay buttons) and the bottom stay.</summary>
        public static int LEFT_HANDED_STRIP_Y = 80;

        public static int LEFT_HANDED_STRIP_BOTTOM_Y = 651;

        /// <summary>Port addition: whether the HD interface is swapped for left-handed play.</summary>
        public bool IsLeftHanded()
        {
            return mApp.IsLeftHanded();
        }

        /// <summary>
        /// Port addition: the Ball-o-tron mode the board asks for (Board.mReplayButtonState); with PC Controls
        /// the wheel isn't used, so the balls show instead.
        /// </summary>
        private int GetWantedBallotronMode()
        {
            int aMode = mBoard.mReplayButtonState;
            return aMode == BALLOTRON_DIAL && mBoard.UsePCControls() ? BALLOTRON_BALLS : aMode;
        }

        /// <summary>Port addition (HD left-handed): the x shift of the Ball-o-tron and its contents (0 right-handed).</summary>
        public int GetBallotronShift()
        {
            return IsLeftHanded() ? -LEFT_HANDED_SHIFT_X : 0;
        }

        /// <summary>Port addition (HD left-handed): the x shift of the fever meter and its lights (0 right-handed).</summary>
        public int GetFeverShift()
        {
            return IsLeftHanded() ? LEFT_HANDED_SHIFT_X : 0;
        }
#endif

        public InterfaceMgr(Board theBoard)
        {
            mBoard = theBoard;
            mApp = theBoard.mApp;
            mButtonCover = 10;
            mMarqueesDirty = true;
            mFFwdCover = 10;
            mStageNum = -1;
            mResGroup = "";
            mBallImageId = -1;
            mBallFrameImageId = -1;
            mFeverMeterFrameImageId = -1;
            mLeftWindowFrameImageId = -1;
            mRightWindowFrameImageId = -1;
            mShooterBackImageId = -1;
            mWheelFrameImageId = -1;
            Clear();
        }

        private static int GetMaxVisibleBalls()
        {
            return 0x19;
        }

        private static int GetBallSpacing()
        {
#if PEGGLE_IPHONE
            return 0x14;
#else
            return BALL_SPACING;
#endif
        }

        private static Image GetBallImage()
        {
            return Res.IMAGE_INT_BALL;
        }

        public int GetNumBalls()
        {
            return mBalls.Count;
        }

        public int GetBallBottom()
        {
            int aBottom = mBallOffset + 0x19a + mVisibleBalls * 3;
            if (mLoadCount != 0)
            {
                if (mLoadCount < 0x15)
                {
                    aBottom += mLoadCount * 2;
                }
                else if (mLoadCount < 0x1e)
                {
                    aBottom += (0x1e - mLoadCount) * 2;
                }
            }
            return aBottom;
        }

        public int GetTopBallY()
        {
            if (mBalls.Count != 0 && mBalls.Count <= GetMaxVisibleBalls())
            {
                return (int)mBalls[mBalls.Count - 1].mY;
            }
            return -1;
        }

        public int GetNumVisibleBalls()
        {
            return Math.Min(GetMaxVisibleBalls(), mBalls.Count);
        }

        public void Clear()
        {
#if !PEGGLE_IPHONE
            mUpdateCnt = 0;
            mBallotronMode = GetWantedBallotronMode();
            mBallotronState = 0;
            mBallotronCount = -1;
#endif
            mFeverStart = -1;
            mFeverBlink = 0;
            mFeverLight = -1;
            mFeverLightCount = 0;
            mFeverBlinkPos = -1;
            mShotMeterDisp = 0;
            mShotMeterTarget = 0;
            mShotMeterScore = 0;
            mShotMeterBlink = 0;
            mLoadCount = 0;
            mBallOffset = 0;
            mBottomBallDelay = 0;
            mTopScore = 0;
            mTopScoreName = "";
            mBallsSettled = true;
            mShotMeterFull = false;
            mDispScore[1] = 0;
            mDispScore[0] = 0;
            mScoreUpdateDelay = 0;
            mPodText[1] = "";
            mPodText[0] = "";
            mPodTextCount[1] = 0;
            mPodTextCount[0] = 0;
            mVisibleBalls = 0;
            mVisibleBallsTarget = 0;
            mBallCountBlink = 0;
            mFeverCount = 0;
            mAdventureScore = 0;
            mFeverMultBlink = 0;
            mScoreBlink[1] = 0;
            mScoreBlink[0] = 0;
            mBalls.Clear();
        }

        public void Dispose()
        {
            if (mResGroup.Length != 0)
            {
                mApp.mResourceManager.DeleteResources(mResGroup);
            }
        }

        public void SyncState(DataSync theSync)
        {
            if (theSync.GetReader() != null)
            {
                for (int i = 0; i < 0x19; i++)
                {
                    mFeverLights[i] = 0;
                }
            }
            if (theSync.GetVersion() <= 4)
            {
                return;
            }
            theSync.SyncBoolBit(ref mBallsSettled);
            theSync.SyncBoolBit(ref mShotMeterFull);
            theSync.EndBit();
            theSync.SyncLong(ref mFeverStart);
            theSync.SyncLong(ref mFeverBlink);
            theSync.SyncLong(ref mTopScore);
            theSync.SyncLong(ref mShotMeterDisp);
            theSync.SyncLong(ref mShotMeterTarget);
            theSync.SyncLong(ref mShotMeterBlink);
            theSync.SyncLong(ref mLoadCount);
            theSync.SyncLong(ref mBallOffset);
            theSync.SyncLong(ref mBottomBallDelay);
            theSync.SyncList(mBalls, (ds, b) => b.SyncState(ds), () => new BallInfo());
            if (theSync.GetVersion() > 6)
            {
                theSync.SyncLong(ref mFeverLight);
                theSync.SyncLong(ref mFeverLightCount);
                theSync.SyncLong(ref mFeverBlinkPos);
            }
            if (theSync.GetVersion() > 7)
            {
                theSync.SyncString(ref mTopScoreName);
                theSync.SyncLong(ref mStageDisp);
                theSync.SyncLong(ref mLevelDisp);
            }
            if (theSync.GetVersion() > 8)
            {
                theSync.SyncLong(ref mDispScore[0]);
                theSync.SyncLong(ref mDispScore[1]);
                theSync.SyncLong(ref mScoreUpdateDelay);
            }
            if (theSync.GetVersion() > 0x1a)
            {
                for (int i = 0; i < 2; i++)
                {
                    theSync.SyncLong(ref mPodTextCount[i]);
                    theSync.SyncString(ref mPodText[i]);
                }
                theSync.SyncLong(ref mVisibleBallsTarget);
                theSync.SyncLong(ref mVisibleBalls);
            }
            if (theSync.GetVersion() < 0x22)
            {
                mFeverCount = 0;
            }
            else
            {
                theSync.SyncLong(ref mFeverCount);
            }
            if (theSync.GetVersion() < 0x29)
            {
                mScoreBlink[1] = 0;
                mScoreBlink[0] = 0;
            }
            else
            {
                theSync.SyncShort(ref mScoreBlink[0]);
                theSync.SyncShort(ref mScoreBlink[1]);
            }
            if (theSync.GetVersion() < 0x2a)
            {
                mAdventureScore = 0;
            }
            else
            {
                theSync.SyncLong(ref mAdventureScore);
            }
            if (theSync.GetVersion() < 0x36)
            {
                mShotMeterScore = mShotMeterDisp;
            }
            else
            {
                theSync.SyncLong(ref mShotMeterScore);
            }
            mMarqueesDirty = true;
        }

        /// <summary>Works out the stage being played and recolours the launcher to match it (or the character).</summary>
        public void SyncStageNum()
        {
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            LevelInfo aLevel = mBoard.mApp.mStageMgr.GetLevelInfoByName(SexyUtil.GetFileName(mBoard.mLevelName, true));
            int aStage = aLevel == null ? aPlayer.mFirstAdventureStage : aLevel.mStage;
            if (aLevel != null && StageMgr.IsBonusStage(aStage))
            {
                // Port addition: a bonus page's stages take the colours of its game's first stages.
                aStage = aStage - StageMgr.GetBonusPage(aStage).mFirstStage + aPlayer.mFirstAdventureStage;
            }
            if (ThunderballApp.mBonusAdventure != null)
            {
                // Port addition: a bonus Adventure's stages are all ordinary ones (no Master stage).
                aStage = SexyUtil.ClampRange(aStage, aPlayer.mFirstAdventureStage, aPlayer.mLastAdventureStage);
            }
            else if (aStage == aPlayer.mLastAdventureStage)
            {
                aStage = aPlayer.mLastAdventureStage - 1;
            }
            else
            {
                aStage = SexyUtil.ClampRange(aStage, aPlayer.mFirstAdventureStage, aPlayer.mLastAdventureStage - 1);
            }
            if (aStage == mStageNum)
            {
                UpdateInterfaceColor();
                return;
            }
            mStageNum = aStage;
            UpdateInterfaceColor();
            int aChar = mBoard.mCharacterMgr.GetCurCharacter();
            if (!mBoard.mRestored || aChar < 0 || aChar > 10)
            {
                mBoard.mCharacterMgr.CreateShooterImage(SexyUtil.GetColorForStage(mStageNum - aPlayer.mFirstAdventureStage, ThunderballApp.mGameLevelPack, false));
            }
            else
            {
                mBoard.mCharacterMgr.CreateShooterImage(SexyUtil.GetColorForCharacter(aChar, ThunderballApp.mGameLevelPack, false));
            }
        }

        /// <summary>Loads the interface image group for the current character ("IP" or "IP_n") and looks up its frame images.</summary>
        public void UpdateInterfaceColor()
        {
            int aChar = mBoard.mCharacterMgr.GetCurCharacter();
            string aGroup = aChar < 1 ? "IP" : string.Format("IP_{0}", aChar);
            ResourceManager aResMgr = mApp.mResourceManager;
            if (aResMgr.IsGroupLoaded(aGroup))
            {
                return;
            }
            if (mResGroup.Length != 0)
            {
                aResMgr.DeleteResources(mResGroup);
            }
            if (!aResMgr.LoadResources(aGroup))
            {
                mApp.ShowResourceError(true);
            }
            if (!Res.ExtractResourcesByName(aResMgr, mResGroup.Length != 0 ? aGroup : aGroup, true))
            {
                mApp.ShowResourceError(true);
            }
            mResGroup = aGroup;
            mBallImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_B");
            mBallFrameImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_BALLOFRAME");
            mFeverMeterFrameImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_FEVERMETER_FRAME");
            mLeftWindowFrameImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_LEFT_WINDOW_FRAME");
            mRightWindowFrameImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_RIGHT_WINDOW_FRAME");
            mShooterBackImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_SHOOTERBACK");
            mWheelFrameImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_WHEEL_FRAME");
#if PEGGLE_IPHONE
            mLeftBarImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_LEFT_BAR");
            mRightBarImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_RIGHT_BAR");
            mLeftHandedBarImageId = (int)Res.GetIdByStringId("IMAGE_" + aGroup + "_LEFT_HANDED_BAR");
#endif
        }

#if PEGGLE_IPHONE
        /// <summary>
        /// iPhone: fills the gutters on either side of the 960-wide layout with the character's bars.
        /// Left-handed play puts the board on the right, so the left bar is drawn at the screen's edge.
        /// Port addition: on a window wider than the game (1136) each bar is split. The outer
        /// SIDEBAR_OUTER_W pixels move out to the window's edge, the rest stays against the game, and
        /// the gap between is filled with a plain strip of the interface art (ULTRAWIDE_FILL_SRC)
        /// stretched across it. At the game's own width the two parts meet and nothing changes.
        /// Port addition (3:2 to 16:9): with gutters narrower than the bars, each bar keeps its inner edge
        /// against the game and its outer part runs off the screen's edge.
        /// </summary>
        public void DrawSidebars(Graphics g)
        {
            Graphics g2 = g.Create();
            g2.ClearClipRect();
            Image aLeftBar = SexyUtil.GetImageById(mApp.IsLeftHanded() ? mLeftHandedBarImageId : mLeftBarImageId);
            Image aRightBar = SexyUtil.GetImageById(mRightBarImageId);
            // Screen x (0..mApp.mWidth is the game) to g's coordinates; the bars sit at the game's
            // edges in both handednesses.
            int aOrigin = -g.mTransX;
            int aGameRight = mApp.mWidth;
            int aWindowLeft = 0;
            int aWindowRight = aGameRight;
            ScreenScales s = mApp.mScreenScales;
            if (s.mTransX > 0 && s.mScaleFactor > 0)
            {
                aWindowLeft = (int)Math.Floor(-s.mTransX / s.mScaleFactor);
                aWindowRight = (int)Math.Ceiling((s.mWidth - s.mTransX) / s.mScaleFactor);
            }
            // The 960-wide layout's edges: the bars end there. Port: with PC Controls the wheel strip is gone and
            // the board centred, so they reach the board's edges (right-handed bars, ThunderballApp.IsLeftHanded).
            int aLeftEdge = SexyUtil.gGutterWidth;
            int aRightEdge = aGameRight - SexyUtil.gGutterWidth;
            if (mBoard.UsePCControls())
            {
                aLeftEdge = mBoard.mX;
                aRightEdge = mBoard.mX + mBoard.mWidth;
            }
            if (aLeftEdge <= aWindowLeft && aRightEdge >= aWindowRight)
            {
                return;
            }
            int aBarW = aLeftBar.mWidth;
            int aOuterW = SIDEBAR_OUTER_W;
            int aInnerW = aBarW - aOuterW;
            int aHeight = aLeftBar.mHeight;
            // Split a bar only when its side has more room than the whole bar.
            bool aSplitLeft = aLeftEdge - aWindowLeft > aBarW;
            bool aSplitRight = aWindowRight - aRightEdge > aBarW;
            // Gaps between each bar's outer part (at the window's edge) and inner part (at the game).
            int aLeftGapX = aWindowLeft + aOuterW;
            int aLeftGapW = aLeftEdge - aInnerW - aLeftGapX;
            int aRightGapX = aRightEdge + aInnerW;
            int aRightGapW = aWindowRight - aOuterW - aRightGapX;
            Image anAtlas = SexyUtil.GetImageById((int)Res.GetIdByStringId("IMAGE_" + mResGroup));
            if (anAtlas != null)
            {
                // Atlas row 1 is the art's top row (row 0 is the edge padding), so the strip starts at y = -1.
                // Each fill reaches 1 pixel under the bar parts on both sides, so no seam shows.
                if (aSplitLeft)
                {
                    g2.DrawImage(anAtlas, new TRect(aOrigin + aLeftGapX - 1, -1, aLeftGapW + 2, 642), ULTRAWIDE_FILL_SRC);
                }
                if (aSplitRight)
                {
                    g2.DrawImage(anAtlas, new TRect(aOrigin + aRightGapX - 1, -1, aRightGapW + 2, 642), ULTRAWIDE_FILL_SRC);
                }
            }
            // Left bar: outer part at the window's left edge, inner part against the game.
            if (aSplitLeft)
            {
                g2.DrawImage(aLeftBar, new TRect(aOrigin + aWindowLeft, 0, aOuterW, aHeight), new TRect(0, 0, aOuterW, aHeight));
                g2.DrawImage(aLeftBar, new TRect(aOrigin + aLeftEdge - aInnerW, 0, aInnerW, aHeight), new TRect(aOuterW, 0, aInnerW, aHeight));
            }
            else
            {
                g2.DrawImage(aLeftBar, aOrigin + aLeftEdge - aBarW, 0);
            }
            // Right bar, mirrored: inner part against the game, outer part at the window's right edge.
            if (aSplitRight)
            {
                g2.DrawImage(aRightBar, new TRect(aOrigin + aRightEdge, 0, aInnerW, aHeight), new TRect(0, 0, aInnerW, aHeight));
                g2.DrawImage(aRightBar, new TRect(aOrigin + aWindowRight - aOuterW, 0, aOuterW, aHeight), new TRect(aInnerW, 0, aOuterW, aHeight));
            }
            else
            {
                g2.DrawImage(aRightBar, aOrigin + aRightEdge, 0);
            }
        }

        /// <summary>Port addition: how much of each bar (from the screen's edge) moves out to the window's edge.</summary>
        private const int SIDEBAR_OUTER_W = 63;

        /// <summary>Port addition: the plain stretch of interface art (inside the left-handed bar) used to fill wide windows.</summary>
        private static readonly TRect ULTRAWIDE_FILL_SRC = new TRect(507, 0, 3, 642);
#endif

        public void LoadGun()
        {
            if (mBalls.Count != 0)
            {
                mBallsSettled = false;
                mLoadCount = 1;
            }
            else
            {
                mBoard.mLogicMgr.mGuideCount = 2;
            }
        }

        public void SetNumBalls(int theNum)
        {
            while (mBalls.Count > theNum)
            {
                mBalls.RemoveAt(mBalls.Count - 1);
            }
            while (mBalls.Count < theNum)
            {
                mBalls.Add(new BallInfo());
            }
            int aVisible = Math.Min(theNum, 0xf);
            mVisibleBalls = aVisible;
            mVisibleBallsTarget = aVisible;
            mBallOffset = 0;
            int y = GetBallBottom();
            int aSpacing = GetBallSpacing();
            for (int i = 0; i < mBalls.Count; i++)
            {
                mBalls[i].mY = (float)y;
                mBalls[i].mVelY = 0.0f;
                y -= aSpacing;
            }
            mBallsSettled = true;
        }

        public void AddTopBalls(int theNum)
        {
            mBallsSettled = false;
            int aDelay = ((9 - mBalls.Count) + theNum) * 4 + 0x14;
            if (aDelay > -1 && theNum < 3)
            {
                mBoard.mSoundMgr.AddSoundDelay(Res.SOUND_ADD_BALL, aDelay);
            }
            int aSpacing = GetBallSpacing();
            int y = 100;
            for (int i = 0; i < theNum; i++)
            {
                BallInfo aBall = new BallInfo();
                mBalls.Add(aBall);
                aBall.mY = (float)y;
                aBall.mVelY = 0.0f;
                y -= aSpacing;
            }
        }

        /// <summary>Pushes a ball in at the bottom of the tube (now, or after theDelay frames).</summary>
        public void AddBottomBall(int theDelay)
        {
            mBallsSettled = false;
            if (mBottomBallDelay != 0)
            {
                mBottomBallDelay = 0;
                AddBottomBall(0);
            }
            if (theDelay != 0)
            {
                mBottomBallDelay = theDelay;
                return;
            }
            int aSpacing = GetBallSpacing();
            BallInfo aBall = new BallInfo();
            mBalls.Insert(0, aBall);
            aBall.mVelY = 0.0f;
            mBallOffset += aSpacing;
            aBall.mY = (float)GetBallBottom();
            int aVisible = GetNumVisibleBalls();
            for (int i = 0; i < aVisible; i++)
            {
                mBalls[i].mVelY = -1.0f;
            }
        }

        public void AddScoreUpdateDelay(int theDelay)
        {
            if (mScoreUpdateDelay < theDelay)
            {
                mScoreUpdateDelay = theDelay;
            }
        }

        private static int ScaleY(int theValue)
        {
            return (int)((double)(SexyUtil.gPhysicalDisplayHeight * theValue) / 600.0);
        }

        public void DrawScoreBlink(Graphics g)
        {
            if (mScoreBlink[0] > 0 && mScoreBlink[0] / 0xf % 2 == 0)
            {
                g.DrawImage(Res.IMAGE_INT_SCOREHILIGHT, ScaleY(0x71), ScaleY(2));
            }
            if (mScoreBlink[1] > 0 && mScoreBlink[1] / 0xf % 2 == 0)
            {
                g.DrawImage(Res.IMAGE_INT_SCOREHILIGHT, ScaleY(0x1ec), ScaleY(2));
            }
        }

        public void DrawOnePlayer(Graphics g)
        {
            DrawScoreBlink(g);
        }

        public void DrawStageMode(Graphics g)
        {
            DrawScoreBlink(g);
        }

        public void DrawTrophyMode(Graphics g)
        {
            DrawScoreBlink(g);
        }

        /// <summary>Duel: highlights the score pod of the player whose turn it is.</summary>
        public void DrawTwoPlayer(Graphics g)
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            bool hide = false;
            if (aLogic.mState == LogicMgr.STATE_AIMING)
            {
                if (aLogic.mStateCount < 0x50 && aLogic.mStateCount / 0xf % 2 == 0)
                {
                    hide = true;
                }
            }
            else
            {
                DrawScoreBlink(g);
            }
            if (!hide)
            {
                if (aLogic.mCurPlayer == 0)
                {
#if PEGGLE_IPHONE
                    g.DrawImage(Res.IMAGE_INT_SCOREHILIGHT, INTERFACE_LEFT_SCORE_POD_DRAW_X, 0);
                }
                else
                {
                    g.DrawImage(Res.IMAGE_INT_SCOREHILIGHT, INTERFACE_RIGHT_SCORE_POD_DRAW_X, 0);
#else
                    g.DrawImage(Res.IMAGE_INT_SCOREHILIGHT, INTERFACE_LEFT_SCORE_POD_DRAW_X, INTERFACE_LEFT_SCORE_POD_DRAW_Y);
                }
                else
                {
                    g.DrawImage(Res.IMAGE_INT_SCOREHILIGHT, INTERFACE_RIGHT_SCORE_POD_DRAW_X, INTERFACE_RIGHT_SCORE_POD_DRAW_Y);
#endif
                }
            }
        }

        /// <summary>The 25-light fever meter on the left, its multiplier bars and the fever glow.</summary>
        public void DrawFeverLights(Graphics g)
        {
            int aLit = 0x19 - mBoard.mLogicMgr.mGoalPegs.Count;
            int aLimit = 1000;
            int aCount = aLit;
            if (mFeverBlink != 0 && mFeverBlinkPos != -1 && mFeverBlinkPos < aLit && mFeverBlink / 0x1e % 2 == 0 && mFeverCount == 0)
            {
                aCount = mFeverBlinkPos;
            }
            int aRunner = mFeverLight;
            if (mFeverLightCount < 0 || mFeverBlink > 0)
            {
                aRunner = -1;
            }
            if (mFeverStart != -1)
            {
                aLimit = mFeverStart;
            }
            if (mFeverBlink > 0)
            {
                aLimit = mFeverBlinkPos;
            }
            int x = INTERFACE_MGR_FEVER_LIGHTS_X;
#if PEGGLE_IPHONE
            // The iPhone stacks the lights upward from y 509 (600-unit space), 14 apart.
            int y600 = 0x1fd;
#else
            int y = INTERFACE_MGR_FEVER_LIGHTS_Y;
#endif
            for (int i = 0; i < aCount && i < 0x19; i++)
            {
#if PEGGLE_IPHONE
                Image anImage = i == 0 ? Res.IMAGE_INT_FEVERMETERBOTTOM : (i == 0x18 ? Res.IMAGE_INT_FEVERMETERTOP : Res.IMAGE_INT_FEVERMETER);
                int y = ScaleY(y600);
                y600 -= 0xe;
                g.DrawImage(anImage, x, y);
#else
                int anOverlap = INTERFACE_MGR_FEVER_LIGHTS_OVERLAP_MID_Y;
                Image anImage;
                if (i == 0)
                {
                    anOverlap = INTERFACE_MGR_FEVER_LIGHTS_OVERLAP_BTM_Y;
                    anImage = Res.IMAGE_INT_FEVERMETERBOTTOM;
                }
                else if (i == 0x18)
                {
                    anImage = Res.IMAGE_INT_FEVERMETERTOP;
                }
                else
                {
                    anImage = Res.IMAGE_INT_FEVERMETER;
                }
                y -= anImage.mHeight;
                g.DrawImage(anImage, x, y);
#endif
                bool aGlow = i == aRunner || aLimit <= i;
                int aBright = 0xaa;
                if (mFeverCount != 0 && mFeverLights[i] != 0)
                {
                    aGlow = true;
                    aBright = mFeverLights[i] * 0xaa / 0x14;
                }
                if (aGlow)
                {
                    g.SetColorizeImages(true);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                    g.SetColor(new Microsoft.Xna.Framework.Color(aBright, aBright, aBright));
                    g.DrawImage(anImage, x, y);
                    g.SetColorizeImages(false);
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                }
#if !PEGGLE_IPHONE
                y += anOverlap;
#endif
            }
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
            g.SetColorizeImages(false);
            int aMultX = INTERFACE_MGR_FEVER_LIGHTS_MULT_X;
            int[] aMultY = { INTERFACE_MGR_FEVER_LIGHTS_MULT1_Y, INTERFACE_MGR_FEVER_LIGHTS_MULT2_Y, INTERFACE_MGR_FEVER_LIGHTS_MULT3_Y, INTERFACE_MGR_FEVER_LIGHTS_MULT4_Y };
            int[] aMultOrg = { INTERFACE_MGR_FEVER_LIGHTS_MULT_ORG1_Y, INTERFACE_MGR_FEVER_LIGHTS_MULT_ORG2_Y, INTERFACE_MGR_FEVER_LIGHTS_MULT_ORG3_Y, INTERFACE_MGR_FEVER_LIGHTS_MULT_ORG4_Y };
            for (int i = 0; i < 4; i++)
            {
                g.DrawImageCel(Res.IMAGE_INT_FEVERBARS, aMultX, aMultY[i], i);
                if (aMultOrg[i] <= aLit)
                {
                    bool aShow = true;
                    if (mFeverCount == 0)
                    {
                        if (i == 3 || aLit < aMultOrg[i + 1])
                        {
                            aShow = mFeverMultBlink / 0xf % 2 == 0;
                        }
                    }
                    else
                    {
                        aShow = i == 0 || i == 3;
                        if (mFeverCount / 0x14 % 2 != 0)
                        {
                            aShow = !aShow;
                        }
                    }
                    if (aShow)
                    {
                        g.DrawImageCel(Res.IMAGE_INT_FEVERBARLITES, INTERFACE_MGR_FEVER_LIGHTS_BAR_X + aMultX, INTERFACE_MGR_FEVER_LIGHTS_BAR_Y + aMultY[i], i);
                    }
                }
            }
            if (mFeverCount != 0)
            {
                int aBright = 0x96;
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                g.SetColorizeImages(true);
                g.SetColor(new Microsoft.Xna.Framework.Color(aBright, aBright, aBright));
#if PEGGLE_IPHONE
                int gx = 0x2f7;
                int gy = 0x7e;
#else
                int gx = INTERFACE_MGR_FEVER_GLOW_X;
                int gy = INTERFACE_MGR_FEVER_GLOW_Y;
#endif
                int aBits = FEVER_GLOW_BITS[(mFeverCount / 10 + 5) % 6];
                Image[] aGlows = { Res.IMAGE_INT_FEVERGLOW1, Res.IMAGE_INT_FEVERGLOW2, Res.IMAGE_INT_FEVERGLOW3 };
                for (int b = 2; b >= 0; b--)
                {
                    if ((aBits & (1 << b)) != 0)
                    {
                        Image aGlow = aGlows[b];
                        g.DrawImage(aGlow, (int)((double)(gx * SexyUtil.gPhysicalDisplayHeight) / 600.0 - (double)(aGlow.mWidth / 2)),
                            (int)((double)(gy * SexyUtil.gPhysicalDisplayHeight) / 600.0 - (double)(aGlow.mHeight / 2)));
                    }
                }
                aBright = aBits * 0xff / 7;
                g.SetColor(new Microsoft.Xna.Framework.Color(aBright * 0xff / 0xff, (aBright << 7) / 0xff, aBright * 0x50 / 0xff));
#if PEGGLE_IPHONE
                g.DrawImage(Res.IMAGE_INT_FEVERGLOW, ScaleY(0x2aa), ScaleY(0x31));
#else
                g.DrawImage(Res.IMAGE_INT_FEVERGLOW, ScaleY(INTERFACE_MGR_FEVER_ALPHA_GLOW_X), ScaleY(INTERFACE_MGR_FEVER_ALPHA_GLOW_Y));
#endif
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                g.SetColorizeImages(false);
            }
        }

        /// <summary>The free-ball meters either side of the Ballotron, filling in three 25k/50k/50k tiers.</summary>
        public void DrawShotMeter(Graphics g)
        {
            bool aBlinkOff = false;
            if (mShotMeterBlink != 0)
            {
                aBlinkOff = mShotMeterBlink / 0x1e % 2 == 1 && mShotMeterBlink < 0x8c;
            }
            TRect aClipL = new TRect(xpos1, INTERFACE_MGR_SHOT_MTR_CLIP_Y, INTERFACE_MGR_SHOT_MTR_CLIP_W, SCREEN_HEIGHT);
            TRect aClipR = new TRect(xpos2, INTERFACE_MGR_SHOT_MTR_CLIP_Y, INTERFACE_MGR_SHOT_MTR_CLIP_W, SCREEN_HEIGHT);
            Image aMeter = Res.IMAGE_INT_FREEBALLMETER;
            int aValue = mShotMeterDisp;
            int aRange = 25000;
            int aFullCel = -1;
            TRect aFillRect = default(TRect);
            if (aValue < 0x1e849)
            {
                if (aValue < 0x124f9)
                {
                    if (aValue < 0x61a9)
                    {
                        aFillRect = aMeter.GetCelRect(0);
                    }
                    else
                    {
                        aFullCel = 0;
                        aRange = 50000;
                        aValue -= 25000;
                        aFillRect = aMeter.GetCelRect(1);
                    }
                }
                else
                {
                    aFullCel = 1;
                    aRange = 50000;
                    aValue -= 75000;
                    aFillRect = aMeter.GetCelRect(2);
                }
            }
            else
            {
                aFullCel = 2;
                aValue = 0;
            }
            if (aFullCel > -1)
            {
                if (aBlinkOff)
                {
                    aFullCel = -1;
                }
                if (aFullCel > -1)
                {
                    TRect aFullRect = aMeter.GetCelRect(aFullCel);
                    g.SetClipRect(aClipL);
                    g.DrawImage(aMeter, xpos1, INTERFACE_MGR_FREE_BALL_MTR_Y, aFullRect);
                    g.SetClipRect(aClipR);
                    g.DrawImageMirror(aMeter, xpos2, INTERFACE_MGR_FREE_BALL_MTR_Y, aFullRect, true);
                }
            }
            int aFill = aValue * aMeter.mHeight / aRange;
            if (aFill > 0)
            {
                if (aMeter.mHeight < aFill)
                {
                    aFill = aMeter.mHeight;
                }
#if PEGGLE_IPHONE
                int aTop = INTERFACE_MGR_SHOT_MTR_BAR_Y + aMeter.mHeight - aFill;
#else
                int aTop = INTERFACE_MGR_FREE_BALL_MTR_Y + aMeter.mHeight - aFill;
#endif
                aFillRect.mY = aMeter.mHeight - aFill;
                aFillRect.mHeight = aFill;
                g.SetClipRect(aClipL);
                g.DrawImage(aMeter, xpos1, aTop, aFillRect);
                g.SetClipRect(aClipR);
                g.DrawImage(aMeter, xpos2, aTop, aFillRect);
                Image aCap = Res.IMAGE_INT_FREEBALLMETERTOP;
#if PEGGLE_IPHONE
                int aCapH = aCap.mHeight;
#else
                int aCapH = aFill < aCap.mHeight ? aFill : aCap.mHeight;
#endif
                TRect aCapRect = new TRect(aFillRect.mX, 0, aFillRect.mWidth, aCapH);
                g.SetClipRect(aClipL);
                g.DrawImage(aCap, xpos1, aTop - aCapH, aCapRect);
                g.SetClipRect(aClipR);
                g.DrawImage(aCap, xpos2, aTop - aCapH, aCapRect);
            }
            g.ClearClipRect();
#if PEGGLE_IPHONE
            g.DrawImage(Res.IMAGE_IP_BALL_METER_TOP, INTERFACE_BALLOTRON_OFFSET_X - 1, INTERFACE_MGR_FREE_BALL_MTR_Y - 1);
            g.DrawImage(Res.IMAGE_IP_BALL_METER_BOTTOM, INTERFACE_BALLOTRON_OFFSET_X - 1, INTERFACE_MGR_FREE_BALL_MTR_BTM_Y);
#endif
        }

        public int GetBallX()
        {
#if PEGGLE_IPHONE
            return 0x1e;
#else
            return INTERFACE_MGR_GETBALL_X;
#endif
        }

        /// <summary>The plunger, the queued balls in the tube and the glass over them.</summary>
        public void DrawBalls(Graphics g)
        {
            Image aBallImage = GetBallImage();
            int aBallX = GetBallX();
            Graphics aClipG = Graphics.GetNew(g);
            aClipG.SetClipRect(new TRect(INTERFACE_MGR_BALLS_CLIP_X, INTERFACE_MGR_BALLS_CLIP_Y, INTERFACE_MGR_BALLS_CLIP_W, INTERFACE_MGR_BALLS_CLIP_H));
            aClipG.DrawImage(Res.IMAGE_INT_PLUNGER, ScaleY(aBallX - 2), ScaleY(GetBallBottom() + 0x11));
            int aVisible = GetNumVisibleBalls();
            for (int i = 0; i < aVisible; i++)
            {
                int x = ScaleY(aBallX);
                aClipG.DrawImage(aBallImage, x, (int)(mBalls[i].mY * (float)SexyUtil.gPhysicalDisplayHeight / 600.0f));
            }
#if PEGGLE_IPHONE
            g.DrawImage(Res.IMAGE_INT_BALLGLASS, ScaleY(3), ScaleY(0x5c));
#else
            aClipG.DrawImage(Res.IMAGE_INT_BALLGLASS, ScaleY(INTERFACE_MGR_DRAWBALLS_X), ScaleY(INTERFACE_MGR_DRAWBALLS_Y));
#endif
            aClipG.PrepareForReuse();
        }

        /// <summary>A cover sliding shut from both sides over a button, theCount of 10 being fully closed.</summary>
        private static void DrawSlidingCover(Graphics g, Image theCover, int theX, int theY, int theCount)
        {
            if (theCount < 10)
            {
                int aLeftW = theCover.mWidth / 2;
                int aRightW = theCover.mWidth - aLeftW;
                float t = (float)theCount / 10.0f;
                aLeftW = (int)((float)aLeftW * t);
                aRightW = (int)((float)aRightW * t);
                g.DrawImage(theCover, theX, theY, new TRect(0, 0, aLeftW, theCover.mHeight));
                g.DrawImage(theCover, theX + theCover.mWidth - aRightW, theY, new TRect(theCover.mWidth - aRightW, 0, aRightW, theCover.mHeight));
            }
            else
            {
                g.DrawImage(theCover, theX, theY);
            }
        }

        /// <summary>Draws the frame, the Ballotron contents, the menu button and its covers, and the fever meter.</summary>
#if PEGGLE_IPHONE
        /// <summary>
        /// iPhone: the Ball-o-tron and fever meter frames, the shooter orb, the ball tube, the menu and
        /// instant replay buttons (with their sliding cover) and the fever meter.
        /// </summary>
        public void DrawBack(Graphics g)
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            int aX = INTERFACE_BALLOTRON_OFFSET_X - 1;
            Image aBallFrame = SexyUtil.GetImageById(mBallFrameImageId);
            g.DrawImage(aBallFrame, aX, 0);
            g.DrawImage(Res.IMAGE_IP_BALLOTRON_BADGE, aX, INTERFACE_BALLOTRON_BADGE_OFFSET_Y);
            Image aLeftFrame = SexyUtil.GetImageById(mLeftWindowFrameImageId);
            if (aLeftFrame != null)
            {
                g.DrawImage(aLeftFrame, aX + aBallFrame.mWidth, 0);
            }
            g.DrawImage(Res.IMAGE_IP_SHOOTERORB, INTERFACE_MGR_SHOOTER_ORB_OFFSET_X + (BOARD_W - Res.IMAGE_IP_SHOOTERORB.mWidth) / 2, 0);
            Image aFeverFrame = SexyUtil.GetImageById(mFeverMeterFrameImageId);
            g.DrawImage(aFeverFrame, BOARD_W - aFeverFrame.mWidth, 0);
            g.DrawImage(Res.IMAGE_IP_FEVERMETER_BADGE, BOARD_W - Res.IMAGE_IP_FEVERMETER_BADGE.mWidth, INTERFACE_FEVERMETER_BADGE_OFFSET_Y);
            Image aRightFrame = SexyUtil.GetImageById(mRightWindowFrameImageId);
            if (aRightFrame != null)
            {
                g.DrawImage(aRightFrame, BOARD_W - aFeverFrame.mWidth - aRightFrame.mWidth, 0);
            }
            g.DrawImageRotated(SexyUtil.GetImageById(mBallImageId), aBallFrame.mWidth + aX, SCREEN_HEIGHT, 1.5707963705062866, 0, 0);
            DrawBalls(g);
            if (mBoard.mPeggleZoom)
            {
                mBoard.mMenuButton.mIsDown = false;
                mBoard.mInstantReplayButton.mIsDown = false;
            }
            DrawButtonImage(g, mBoard.mMenuButton, false);
            DrawButtonImage(g, mBoard.mInstantReplayButton, false);
            if (mButtonCover > 0)
            {
                DrawSlidingCover(g, Res.IMAGE_INT_BUTTONCOVER, INTERFACE_MGR_BTN_COVER_X + 1, INTERFACE_MGR_BTN_COVER_Y + 1, mButtonCover);
            }
            DrawFeverLights(g);
            if (!aLogic.mDuelMode)
            {
                if (mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE || mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
                {
                    DrawStageMode(g);
                }
                else if (mBoard.mGameMode == GameMode.GAMEMODE_CHALLENGE)
                {
                    DrawTrophyMode(g);
                }
                else
                {
                    DrawOnePlayer(g);
                }
            }
            else
            {
                DrawTwoPlayer(g);
            }
        }
#else
        public void DrawBack(Graphics g)
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            // The left frame (BALLOFRAME) holds the fever meter, the right one (FEVERMETER_FRAME) the Ball-o-tron.
            Image aBallFrame = SexyUtil.GetImageById(mBallFrameImageId);
            g.DrawImage(aBallFrame, 0, 0);
            Image aFeverFrame = SexyUtil.GetImageById(mFeverMeterFrameImageId);
            g.DrawImage(aFeverFrame, BOARD_W - aFeverFrame.mWidth, 0);
            int aFeverShift = GetFeverShift();
            int aBallotronShift = GetBallotronShift();
            if (IsLeftHanded())
            {
                // Port addition (left-handed): each frame's middle rows are drawn over the other side's frame,
                // whose own edge (cut the same way) shows past them. The tops and bottoms stay as they are.
                TRect aStrip = new TRect(0, LEFT_HANDED_STRIP_Y, aFeverFrame.mWidth, LEFT_HANDED_STRIP_BOTTOM_Y - LEFT_HANDED_STRIP_Y);
                g.DrawImage(aFeverFrame, BOARD_W - aFeverFrame.mWidth + aBallotronShift, LEFT_HANDED_STRIP_Y, aStrip);
                aStrip.mWidth = aBallFrame.mWidth;
                g.DrawImage(aBallFrame, aFeverShift, LEFT_HANDED_STRIP_Y, aStrip);
            }
            g.DrawImage(Res.IMAGE_IP_FEVERMETER_BADGE, aFeverShift, INTERFACE_BALLOTRON_BADGE_OFFSET_Y + 0x14);
            g.DrawImage(SexyUtil.GetImageById(mLeftWindowFrameImageId), aBallFrame.mWidth, 0);
            g.DrawImage(Res.IMAGE_IP_BALLOTRON_BADGE, BOARD_W - Res.IMAGE_IP_FEVERMETER_BADGE.mWidth + aBallotronShift, INTERFACE_FEVERMETER_BADGE_OFFSET_Y + 0x14);
            Image aRightFrame = SexyUtil.GetImageById(mRightWindowFrameImageId);
            g.DrawImage(aRightFrame, BOARD_W - aFeverFrame.mWidth - aRightFrame.mWidth, 0);
            g.DrawImage(Res.IMAGE_IP_SHOOTERORB, (BOARD_W - Res.IMAGE_IP_SHOOTERORB.mWidth) / 2, 0);
            g.DrawImageRotated(SexyUtil.GetImageById(mBallImageId), aBallFrame.mWidth - 0x2a, SCREEN_HEIGHT, 1.5707963705062866, 0, 0);
            // Replay controls aren't shifted here: their buttons sit where Board.PositionReplayDialog put them.
            g.Translate(aBallotronShift, 0);
            if (mBallotronMode == BALLOTRON_BALLS)
            {
                DrawBalls(g);
            }
            else if (mBallotronMode == BALLOTRON_DIAL)
            {
                DrawDial(g);
            }
            g.Translate(-aBallotronShift, 0);
            if (mBallotronMode == BALLOTRON_REPLAY)
            {
                DrawReplayCtrls(g);
            }
            if (mBoard.mPeggleZoom)
            {
                mBoard.mMenuButton.mIsDown = false;
                mBoard.mReplayPressed = false;
            }
            DrawButtonImage(g, mBoard.mMenuButton, false);
            if (mButtonCover > 0)
            {
                DrawSlidingCover(g, Res.IMAGE_INT_BUTTONCOVER, INTERFACE_MGR_BTN_COVER_X - 2, INTERFACE_MGR_BTN_COVER_Y - 2, mButtonCover);
            }
            if ((mBoard.mInterfaceMgr.mFFwdCover > 0 && !mBoard.mFeverCam) || mBallotronMode == BALLOTRON_REPLAY)
            {
                int aCount = mBallotronMode == BALLOTRON_REPLAY ? 10 : mBoard.mInterfaceMgr.mFFwdCover;
                DrawSlidingCover(g, Res.IMAGE_INT_BUTTONCOVER, BOARD_FFWD_RECT_COVER_OFFSET_X + mBoard.mFFwdRect.mX,
                    BOARD_FFWD_RECT_COVER_OFFSET_Y + mBoard.mFFwdRect.mY, aCount);
            }
            g.Translate(aFeverShift, 0);
            DrawFeverLights(g);
            g.Translate(-aFeverShift, 0);
            if (!aLogic.mDuelMode)
            {
                if (mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE || mBoard.mGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
                {
                    DrawStageMode(g);
                }
                else if (mBoard.mGameMode == GameMode.GAMEMODE_CHALLENGE)
                {
                    DrawTrophyMode(g);
                }
                else
                {
                    DrawOnePlayer(g);
                }
            }
            else
            {
                DrawTwoPlayer(g);
            }
        }
#endif

#if !PEGGLE_IPHONE
        /// <summary>The aiming dial shown in the Ballotron slot (the wheel scrolls with the gun angle).</summary>
        public void DrawDial(Graphics g)
        {
            if (mBoard.mDreamTransitionCounter >= 1)
            {
                return;
            }
            TRect anOldClip = g.mClipRect;
            g.SetClipRect(BOARD_DIAL_RECT_X, BOARD_DIAL_RECT_Y, Res.IMAGE_INT_WHEELSHADOW.mWidth, Res.IMAGE_INT_WHEELSHADOW.mHeight);
            int aPos = mBoard.mDialObj.mDialPos + 0x14a;
            int aWheelH = Res.IMAGE_INT_WHEEL.mHeight;
            g.DrawImageCel(Res.IMAGE_INT_WHEEL, BOARD_DIAL_RECT_X, BOARD_DIAL_RECT_Y + aPos, 0);
            g.DrawImageCel(Res.IMAGE_INT_WHEEL, BOARD_DIAL_RECT_X, BOARD_DIAL_RECT_Y - aWheelH + aPos, 0);
            g.DrawImageCel(Res.IMAGE_INT_WHEEL, BOARD_DIAL_RECT_X, BOARD_DIAL_RECT_Y + aWheelH * -2 + aPos, 0);
            g.DrawImageCel(Res.IMAGE_INT_WHEEL, BOARD_DIAL_RECT_X, BOARD_DIAL_RECT_Y + aWheelH * -3 + aPos, 0);
            g.DrawImageCel(Res.IMAGE_INT_WHEELSHADOW, BOARD_DIAL_RECT_X, BOARD_DIAL_RECT_Y, 0);
            // Port: restored as saved (absolute); SetClipRect would add the translation again (left-handed shift).
            g.mClipRect = anOldClip;
        }
#endif

#if !PEGGLE_IPHONE
        /// <summary>The replay controls drawn into the Ballotron slot while watching a replay.</summary>
        public void DrawReplayCtrls(Graphics g)
        {
            g.DrawImage(Res.IMAGE_INT_REPLAY_BGD, BOARD_DIAL_RECT_X + GetBallotronShift(), BOARD_DIAL_RECT_Y);
            ReplayDialog aDialog = mBoard.mReplayDialog;
            if (aDialog == null || aDialog.mPlayButton == null)
            {
                return;
            }
            DrawReplayButton(g, aDialog, aDialog.mPlayButton);
            if (aDialog.mFFwdButton.mVisible)
            {
                DrawReplayButton(g, aDialog, aDialog.mFFwdButton);
            }
            DrawReplayButton(g, aDialog, aDialog.mDoneButton);
        }
#endif

#if !PEGGLE_IPHONE
        private static void DrawReplayButton(Graphics g, ReplayDialog theDialog, ThunderButton theButton)
        {
            g.DrawImageCel(theButton.mComponentImage, theDialog.mX + theButton.mX, theDialog.mY + theButton.mY, theButton.mIsDown ? 1 : 0);
        }
#endif

        /// <summary>Draws a button that is normally hidden (mDraw false) at its own position.</summary>
        private static void DrawButtonImage(Graphics g, ThunderButton theButton, bool forceOver)
        {
            int x = theButton.mX;
            int y = theButton.mY;
            g.Translate(x, y);
            theButton.SetDraw(true);
            if (forceOver)
            {
                bool wasOver = theButton.mIsOver;
                theButton.mIsOver = true;
                theButton.mOverAlpha = 0.0;
                theButton.Draw(g);
                theButton.mIsOver = wasOver;
            }
            else
            {
                theButton.Draw(g);
            }
            theButton.SetDraw(false);
            g.Translate(-x, -y);
        }

        /// <summary>Shot meter, balls-left counter, the free-ball light-up and the Ballotron cover.</summary>
        public void DrawFront(Graphics g)
        {
#if PEGGLE_IPHONE
            DrawFrontContents(g);
#else
            // Port addition (left-handed): all of it belongs to the Ball-o-tron, so it moves with it.
            int aShift = GetBallotronShift();
            g.Translate(aShift, 0);
            DrawFrontContents(g);
            g.Translate(-aShift, 0);
#endif
        }

        private void DrawFrontContents(Graphics g)
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
#if PEGGLE_IPHONE
            DrawShotMeter(g);
#else
            if (mBallotronMode == BALLOTRON_BALLS)
            {
                DrawShotMeter(g);
            }
#endif
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
            g.SetColorizeImages(false);
            g.SetColor(SexyUtil.MakeColor(0xffff00));
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            if ((mBallCountBlink == 0 || mBallCountBlink / 0x1e % 2 == 0) && mShotMeterBlink < 1)
            {
                int aBalls = aLogic.mBallsLeft[aLogic.mCurPlayer];
                g.SetColor(new Microsoft.Xna.Framework.Color(0xff, 0xff, 0));
                g.SetFont(Res.FONT_SCORE_DISP);
                string aText = aBalls.ToString();
                int aWidth = g.GetFont().StringWidth(aText);
                g.DrawString(aText, INTERFACE_MGR_NUM_BALL_X - aWidth / 2, INTERFACE_MGR_NUM_BALL_Y + g.GetFont().GetCenteredHeight());
            }
            if (mShotMeterBlink != 0)
            {
                int aPeriod = 0xf;
                int aFrame = mShotMeterBlink / 0xf;
                int aFlashes = 4;
                int aFlashLen = 5;
                bool canFreeBall = mBoard.mTrophyInfo == null || !mBoard.mTrophyInfo.mNoFreeballs;
                Image anImage;
                if (!canFreeBall || aFrame / 4 % 2 == 1)
                {
                    anImage = Res.IMAGE_INT_FBPOINTS;
                }
                else
                {
                    anImage = Res.IMAGE_INT_FBFREEBALL;
                }
                int aColor;
                int aTier;
                if (mShotMeterScore < 75000)
                {
                    aColor = 0x88ff88;
                    aTier = 0;
                }
                else if (mShotMeterScore < 0x1e848)
                {
                    aColor = 0xff44ff;
                    aTier = 1;
                }
                else
                {
                    aColor = 0xffff44;
                    aTier = 2;
                }
                int aFadeStart = 0x3c;
                int aFadePos = aFrame - aFlashLen * aFlashes;
                int aCol;
                if (!canFreeBall || aFadePos < 0)
                {
                    aCol = aFrame % 2;
                }
                else
                {
                    aFadePos = mShotMeterBlink - aFlashes * aFlashLen * aPeriod;
                    anImage = Res.IMAGE_INT_FBFREEBALL;
                    aCol = 1;
                }
                if (aLogic.mDuelMode)
                {
                    anImage = Res.IMAGE_INT_FBPOINTS;
                }
                Transform aTransform = new Transform();
                int anAlpha = 0xff;
                if (mShotMeterBlink < 10)
                {
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, mShotMeterBlink * 0xff / 10));
                }
                else if (aFadeStart < aFadePos)
                {
                    anAlpha = 0xff - (aFadePos - aFadeStart) * 0xff / 0x14;
                    if (anAlpha < 0)
                    {
                        return;
                    }
                    g.SetColorizeImages(true);
                    g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                }
#if PEGGLE_IPHONE
                float cx = (float)(0x27 * SexyUtil.gPhysicalDisplayHeight / 600.0);
                float cy = (float)(0x7d * SexyUtil.gPhysicalDisplayHeight / 600.0);
#else
                float cx = (float)INTERFACE_MGR_FREEBALL_CENTER_X;
                float cy = (float)INTERFACE_MGR_FREEBALL_CENTER_Y;
#endif
                g.DrawImageTransform(Res.IMAGE_INT_FBLIGHTUP, aTransform, Res.IMAGE_INT_FBLIGHTUP.GetCelRect(aTier), cx, cy);
                g.DrawImageTransform(anImage, aTransform, anImage.GetCelRect(aCol, aTier), cx, cy);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                g.SetColorizeImages(true);
                anAlpha = anAlpha * anAlpha / 0xff;
                anAlpha = aCol == 0 ? anAlpha << 7 : anAlpha * 0xff;
                anAlpha /= 0xff;
                g.SetColor(SexyUtil.MakeColor(aColor, anAlpha));
#if PEGGLE_IPHONE
                g.DrawImage(Res.IMAGE_INT_FBGLOW, 0, ScaleY(0x14));
#else
                g.DrawImage(Res.IMAGE_INT_FBGLOW, INTERFACE_MGR_FREEBALL_GLOW_X, INTERFACE_MGR_FREEBALL_GLOW_Y);
#endif
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                g.SetColorizeImages(false);
            }
#if !PEGGLE_IPHONE
            DrawBallotronCover(g);
#endif
        }

#if !PEGGLE_IPHONE
        /// <summary>The two halves that close over the Ballotron while it switches between balls, dial and replay.</summary>
        public void DrawBallotronCover(Graphics g)
        {
            if (mBallotronCount < 0)
            {
                return;
            }
            TRect anOldClip = g.mClipRect;
            g.SetClipRect(new TRect(BOARD_BALLOTRON_COVER_RECT_X, BOARD_BALLOTRON_COVER_RECT_Y, BOARD_BALLOTRON_COVER_RECT_W, BOARD_BALLOTRON_COVER_RECT_H));
            int x = BOARD_BALLOTRON_COVER_RECT_X;
            int y = BOARD_BALLOTRON_COVER_RECT_Y;
            Image aLeft = Res.IMAGE_INT_BALLOTRON_COVER_LEFT;
            Image aRight = Res.IMAGE_INT_BALLOTRON_COVER_RIGHT;
            if (!((float)mBallotronCount < INTERFACE_MGR_BALLOTRON_COVER_ANIM_SPEED))
            {
                g.DrawImage(aLeft, x, y);
                g.DrawImage(aRight, BOARD_DIAL_RECT_W + x - aRight.mWidth, y);
            }
            else
            {
                float t = (float)mBallotronCount / INTERFACE_MGR_BALLOTRON_COVER_ANIM_SPEED;
                TRect aLeftRect = aLeft.GetCelRect(0);
                int aCut = (int)((float)aLeftRect.mWidth - (float)aLeftRect.mWidth * t);
                aLeftRect.mX += aCut;
                aLeftRect.mWidth -= aCut;
                g.DrawImage(aLeft, x, y, aLeftRect);
                TRect aRightRect = aRight.GetCelRect(0);
                aRightRect.mWidth = (int)((float)aRightRect.mWidth * t);
                x = BOARD_BALLOTRON_COVER_RECT_W + BOARD_BALLOTRON_COVER_RECT_X;
                g.DrawImage(aRight, x - aRightRect.mWidth, y, aRightRect);
            }
            // Port: restored as saved (absolute); SetClipRect would add the translation again (left-handed shift).
            g.mClipRect = anOldClip;
        }
#endif

        public void BeginShot()
        {
            mFeverStart = 0x19 - mBoard.mLogicMgr.mGoalPegs.Count;
            mShotMeterBlink = 0;
            mShotMeterFull = false;
            mFeverLight = -1;
        }

        /// <summary>Rewrites the two score marquees for the current mode.</summary>
        public void UpdateMarquees()
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            TrophyInfo aTrophy = mBoard.mTrophyInfo;
            if (aLogic.mDuelMode)
            {
                int aBalls0 = aLogic.mBallsLeft[0];
                int aBalls1 = aLogic.mBallsLeft[1];
                string aLeft;
                if (aTrophy == null)
                {
                    aLeft = aLogic.mPlayerNames[0];
                    aLeft += "\n" + string.Format(SexyAppBase.Loc("Balls %d").Replace("%d", "{0}"), aBalls0);
                }
                else
                {
                    aLeft = GetChallengeTitle(aTrophy);
                    aLeft += "\n" + GetLivesText(aTrophy, aBalls0);
                }
                string aRight = aLogic.mPlayerNames[1];
                aRight += "\n" + string.Format(SexyAppBase.Loc("Balls %d").Replace("%d", "{0}"), aBalls1);
                aLeft += "\n" + SexyUtil.CommaSeperate(mDispScore[0]);
                aRight += "\n" + SexyUtil.CommaSeperate(mDispScore[1]);
                mBoard.mLeftScorePod.SetText(aLeft, 0);
                mBoard.mRightScorePod.SetText(aRight, 0);
                mMarqueesDirty = false;
                return;
            }
            int aMode = mBoard.mGameMode;
            if (aMode == GameMode.GAMEMODE_ADVENTURE || aMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                string aStageText = string.Format(SexyAppBase.Loc("Stage: %d-%d").Replace("%d-%d", "{0}-{1}"),
                    mStageDisp - mApp.mPlayerInfo.mFirstAdventureStage, mLevelDisp);
                mBoard.mLeftScorePod.SetText(aStageText + "\n" + SexyUtil.CommaSeperate(mDispScore[0]), 0);
                string aLevelName = SexyUtil.GetFileName(mBoard.mLevelName, true);
                if (aLevelName.Length == 0)
                {
                    return;
                }
                LevelInfo aLevel = mApp.mStageMgr.GetLevelInfoByName(aLevelName);
                string aRight;
                if (aLevel == null)
                {
                    aRight = SexyAppBase.Loc("Total Score") + "\n" + SexyUtil.CommaSeperate(mAdventureScore);
                }
                else
                {
                    aRight = SexyAppBase.Loc("Ace Score\n") + SexyUtil.CommaSeperate(aLevel.mAceScore) + "\n" +
                        SexyAppBase.Loc("Total Score") + "\n" + SexyUtil.CommaSeperate(mAdventureScore);
                }
                mBoard.mRightScorePod.SetText(aRight, 0);
                mMarqueesDirty = false;
                return;
            }
            if (aMode == GameMode.GAMEMODE_CHALLENGE)
            {
                string aLeft = SexyAppBase.Loc("Challenge Score\n") + SexyUtil.CommaSeperate(mDispScore[0]);
                if (aTrophy.mLives < 2)
                {
                    if (aTrophy.mStyleScoreReq == 0)
                    {
                        if (aTrophy.mUniqueStyleShotsReq > -1)
                        {
                            aLeft += string.Format(SexyAppBase.Loc("\nUnique Style Shots\n%d").Replace("%d", "{0}"), aLogic.GetUniqueStyleShots());
                        }
                    }
                    else
                    {
                        aLeft += SexyAppBase.Loc("\nStyle Shot Points\n") + SexyUtil.CommaSeperate(aLogic.mInt16c);
                    }
                }
                else
                {
                    aLeft += "\n" + GetLivesText(aTrophy, 0);
                }
                mBoard.mLeftScorePod.SetText(aLeft, 0);
                string aRight = GetChallengeTitle(aTrophy);
                if (aTrophy.mSmallDesc.Length != 0)
                {
                    aRight += "\n" + aTrophy.mSmallDesc;
                }
                mBoard.mRightScorePod.SetText(aRight, 0);
                mMarqueesDirty = false;
                return;
            }
            string aTitle = aMode == GameMode.GAMEMODE_DEMO ? SexyAppBase.Loc("Demo\n") : SexyAppBase.Loc("Quick Play\n");
            mBoard.mLeftScorePod.SetText(aTitle + SexyUtil.CommaSeperate(mDispScore[0]), 0);
            if (mTopScore > 0)
            {
                string aLevelName = SexyUtil.GetFileName(mBoard.mLevelName, true);
                if (aLevelName.Length == 0)
                {
                    return;
                }
                LevelInfo aLevel = mApp.mStageMgr.GetLevelInfoByName(aLevelName);
                string aRight = SexyAppBase.Loc("Top Score\n") + mTopScoreName + "\n" + SexyUtil.CommaSeperate(mTopScore) + "\n" +
                    SexyAppBase.Loc("Ace Score\n") + SexyUtil.CommaSeperate(aLevel != null ? aLevel.mAceScore : 0);
                mBoard.mRightScorePod.SetText(aRight, 0);
            }
            mMarqueesDirty = false;
        }

        private string GetChallengeTitle(TrophyInfo theTrophy)
        {
            if (theTrophy.mLevels.Count == 1)
            {
                return SexyAppBase.Loc("Challenge");
            }
            return string.Format(SexyAppBase.Loc("Chal. %d/%d").Replace("%d/%d", "{0}/{1}"), mBoard.mTrophyLevelIdx + 1, theTrophy.mLevels.Count);
        }

        private string GetLivesText(TrophyInfo theTrophy, int theBalls)
        {
            if (theTrophy.mLives < 2)
            {
                return string.Format(SexyAppBase.Loc("Balls %d").Replace("%d", "{0}"), theBalls);
            }
            if (mBoard.mTrophyLives == 1)
            {
                return SexyAppBase.Loc("Last Life");
            }
            return string.Format(SexyAppBase.Loc("Lives: %d").Replace("%d", "{0}"), mBoard.mTrophyLives);
        }

        public void EndShot()
        {
            mFeverStart = -1;
            mFeverLight = -1;
            mShotMeterTarget = 0;
        }

        public void DoShotMeterBlink()
        {
            mShotMeterBlink = 1;
        }

        public void DoFeverBlink()
        {
            if (mFeverStart != -1)
            {
                mFeverBlink = 1;
                mFeverBlinkPos = mFeverStart;
            }
        }

        /// <summary>Refreshes the adventure total and the level's top score shown on the right marquee.</summary>
        public void UpdateHighScore(bool unused)
        {
            PlayerInfo aPlayer = mBoard.mApp.mPlayerInfo;
            mAdventureScore = aPlayer == null ? 0 : aPlayer.GetmAdventureScore();
            mTopScore = 0;
            mTopScoreName = "";
            string aLevelName = SexyUtil.GetFileName(mBoard.mLevelName, true);
            if (aLevelName.Length == 0)
            {
                return;
            }
            List<HighScoreEntry> aScores = mBoard.mApp.mHighScoreMgr.GetScores(aLevelName, false, -1);
            if (aScores != null && aScores.Count != 0)
            {
                mTopScore = aScores[0].mScore;
                mTopScoreName = aScores[0].mName;
            }
        }

        public void InitLevel()
        {
            UpdateHighScore(false);
            mStageDisp = mBoard.mApp.mCurStage + 1;
            mLevelDisp = mBoard.mApp.mCurLevel + 1;
        }

        /// <summary>Feeds the shot score into the free-ball meter, stopping at each tier, and animates it towards its target.</summary>
        public void UpdateShotMeter()
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            if (aLogic.mState == LogicMgr.STATE_SHOOTING && !aLogic.mDimLowObjects)
            {
                if (aLogic.mShotScore >= 0x1e848 && mShotMeterDisp < 0x1e848)
                {
                    mShotMeterDisp = 0x1e848;
                    mShotMeterTarget = 0x1e848;
                    mShotMeterFull = true;
                }
                else if (aLogic.mShotScore >= 75000 && mShotMeterDisp < 75000)
                {
                    mShotMeterDisp = 75000;
                    mShotMeterTarget = 75000;
                    mShotMeterFull = true;
                }
                else if (aLogic.mShotScore >= 25000 && mShotMeterDisp < 25000)
                {
                    mShotMeterDisp = 25000;
                    mShotMeterTarget = 25000;
                    mShotMeterFull = true;
                }
                else
                {
                    mShotMeterTarget = aLogic.mShotScore;
                }
                if (mShotMeterScore < aLogic.mShotScore)
                {
                    mShotMeterScore = aLogic.mShotScore;
                }
            }
            if (mShotMeterBlink != 0)
            {
                mShotMeterBlink++;
                if (mShotMeterBlink > 399)
                {
                    if (mShotMeterDisp == 0)
                    {
                        mShotMeterScore = 0;
                    }
                    mShotMeterBlink = 0;
                }
            }
            if (mShotMeterDisp == mShotMeterTarget)
            {
                return;
            }
            if (mShotMeterDisp < mShotMeterTarget)
            {
                int aDiff = mShotMeterTarget - mShotMeterDisp;
                if (aDiff > 100000)
                {
                    mShotMeterDisp += 5000;
                }
                else if (aDiff > 25000)
                {
                    mShotMeterDisp += 1000;
                }
                else if (aDiff > 1000)
                {
                    mShotMeterDisp += 200;
                }
                else if (aDiff > 100)
                {
                    mShotMeterDisp += 100;
                }
                else
                {
                    mShotMeterDisp += 10;
                }
                if (mShotMeterTarget < mShotMeterDisp)
                {
                    mShotMeterDisp = mShotMeterTarget;
                }
            }
            else if (mShotMeterTarget < mShotMeterDisp)
            {
                int aDiff = mShotMeterDisp - mShotMeterTarget;
                if (aDiff > 100000)
                {
                    mShotMeterDisp -= 5000;
                }
                if (aDiff > 25000)
                {
                    mShotMeterDisp -= 1000;
                }
                else
                {
                    mShotMeterDisp -= 200;
                }
                if (mShotMeterDisp < mShotMeterTarget)
                {
                    mShotMeterDisp = mShotMeterTarget;
                }
            }
            if (mShotMeterDisp == 0)
            {
                if (mShotMeterBlink == 0)
                {
                    mShotMeterScore = 0;
                }
                mShotMeterFull = false;
            }
            else if (mShotMeterDisp > 24999)
            {
                mShotMeterFull = true;
            }
        }

        /// <summary>Ball physics in the tube: falling, stacking with a little bounce, and loading the gun from the top.</summary>
        public void UpdateBalls()
        {
            if (mBalls.Count == 0 || mBallsSettled)
            {
                if (mLoadCount != 0)
                {
                    mBoard.mLogicMgr.mGuideCount = 2;
                    mLoadCount = 0;
                }
                return;
            }
            if (mBallOffset != 0)
            {
                mBallOffset -= 3;
                if (mBallOffset < 0)
                {
                    mBallOffset = 0;
                }
            }
            int aVisible = GetNumVisibleBalls();
            if (mLoadCount != 0)
            {
                mLoadCount++;
                if (mLoadCount == 0x14)
                {
                    float aVel = -15.0f;
                    for (int i = aVisible - 1; i >= 0; i--)
                    {
                        mBalls[i].mVelY = aVel;
                        aVel = -1.5f;
                    }
                }
            }
            int aBottom = GetBallBottom();
            int aSpacing = GetBallSpacing();
            int aTarget = Math.Min(mBalls.Count, 0xf);
            if (mLoadCount > 0x13 && aTarget > 0)
            {
                aTarget--;
            }
            if (aTarget == 0)
            {
                mVisibleBallsTarget = 0;
            }
            bool aMoving = false;
            BallInfo aFirst = mBalls[0];
            aFirst.mY += aFirst.mVelY;
            if (aFirst.mY <= (float)aBottom)
            {
                aMoving = aFirst.mY < (float)aBottom;
                if (aMoving)
                {
                    aFirst.mVelY += 0.15f;
                }
            }
            else
            {
                aFirst.mY = (float)aBottom;
                if (Math.Abs(aFirst.mVelY) >= 1.0f)
                {
                    aFirst.mVelY *= -0.35f;
                }
                else
                {
                    aFirst.mVelY = 0.0f;
                }
                if (mVisibleBallsTarget < 1)
                {
                    mVisibleBallsTarget = 1;
                }
            }
            for (int i = 1; i < aVisible; i++)
            {
                BallInfo aBall = mBalls[i];
                BallInfo aBelow = mBalls[i - 1];
                float aGap = aBelow.mY - (aBall.mY + aBall.mVelY) - (float)aSpacing;
                if (aGap >= 0.0f)
                {
                    if (!aMoving && aGap >= 5.0f)
                    {
                        aMoving = true;
                    }
                    aBall.mVelY += 0.15f;
                    aBall.mY += aBall.mVelY;
                }
                else
                {
                    aBall.mY = aBelow.mY - (float)aSpacing;
                    float anImpulse = (Math.Abs(aBall.mVelY) + Math.Abs(aBelow.mVelY)) * 0.375f;
                    if (anImpulse < 1.0f)
                    {
                        anImpulse = 0.0f;
                    }
                    if (aBall.mVelY > 0.0f)
                    {
                        aBall.mVelY = 0.0f;
                    }
                    if (aBelow.mVelY < 0.0f)
                    {
                        aBelow.mVelY = 0.0f;
                    }
                    aBall.mVelY -= anImpulse;
                    aBelow.mVelY += anImpulse;
                    if (!aMoving && mVisibleBallsTarget < i + 1)
                    {
                        mVisibleBallsTarget = i + 1;
                    }
                }
            }
            if (aTarget < mVisibleBallsTarget)
            {
                mVisibleBallsTarget = aTarget;
            }
            else if (mVisibleBallsTarget > 0 && aTarget - mVisibleBallsTarget > 3)
            {
                mVisibleBallsTarget = aTarget;
            }
            if ((mLoadCount > 0x14 && mBalls[mBalls.Count - 1].mY < 130.0f) || mLoadCount > 0x3c)
            {
                mLoadCount = 0;
                mBalls.RemoveAt(mBalls.Count - 1);
                if (mBalls.Count >= GetMaxVisibleBalls())
                {
                    BallInfo aNext = mBalls[GetMaxVisibleBalls() - 1];
                    aNext.mY = 130.0f;
                    aNext.mVelY = 0.0f;
                }
                mBoard.mLogicMgr.mGuideCount = 2;
            }
            if (mBallOffset == 0 && mLoadCount == 0 && mBottomBallDelay == 0)
            {
                mBallsSettled = true;
                int aCount = GetNumVisibleBalls();
                for (int i = 0; i < aCount; i++)
                {
                    if (mBalls[i].mVelY != 0.0f)
                    {
                        mBallsSettled = false;
                    }
                }
            }
            if (mVisibleBalls != mVisibleBallsTarget)
            {
                if (mVisibleBalls < mVisibleBallsTarget)
                {
                    mVisibleBalls++;
                    if (mVisibleBalls == mVisibleBallsTarget && aTarget < mVisibleBalls)
                    {
                        mVisibleBallsTarget = aTarget;
                    }
                }
                else
                {
                    mVisibleBalls--;
                }
                mBallsSettled = false;
            }
        }

        /// <summary>When player 1's displayed score passes the level's top score, it takes over the top-score line.</summary>
        public void CheckHighScoreSync()
        {
            if (mTopScore > 0 && mTopScore < mDispScore[0] && mBoard.mGameMode != GameMode.GAMEMODE_DEMO)
            {
                mTopScore = mDispScore[0];
                if (mApp.mPlayerInfo != null)
                {
                    mTopScoreName = mApp.mPlayerInfo.mName;
                }
            }
        }

        /// <summary>Counts the displayed scores towards the real ones.</summary>
        public void UpdateScoreDisp()
        {
            if (mScoreUpdateDelay != 0)
            {
                mScoreUpdateDelay--;
                return;
            }
            LogicMgr aLogic = mBoard.mLogicMgr;
            for (int i = 0; i < 2; i++)
            {
                int aDisp = mDispScore[i];
                int aScore = aLogic.mScore[i];
                if (aDisp == aScore)
                {
                    continue;
                }
                if (aDisp < aScore)
                {
                    if (aLogic.mState == LogicMgr.STATE_LEVEL_DONE)
                    {
                        aDisp += 5000;
                    }
                    else
                    {
                        int aDiff = aScore - aDisp;
                        if (aDiff > 100000)
                        {
                            aDisp += 5000;
                        }
                        else if (aDiff > 10000)
                        {
                            aDisp += 1000;
                        }
                        else if (aDiff > 1000)
                        {
                            aDisp += 200;
                        }
                        else if (aDiff > 100)
                        {
                            aDisp += 100;
                        }
                        else
                        {
                            aDisp += 10;
                        }
                    }
                    if (aScore < aDisp)
                    {
                        aDisp = aScore;
                    }
                }
                else if (aScore < aDisp)
                {
                    int aDiff = aDisp - aScore;
                    if (aDiff > 100000)
                    {
                        aDisp -= 5000;
                    }
                    if (aDiff > 10000)
                    {
                        aDisp -= 1000;
                    }
                    else if (aDiff > 1000)
                    {
                        aDisp -= 200;
                    }
                    else if (aDiff > 100)
                    {
                        aDisp -= 100;
                    }
                    else
                    {
                        aDisp -= 10;
                    }
                    if (aDisp < aScore)
                    {
                        aDisp = aScore;
                    }
                }
                mDispScore[i] = aDisp;
                if (i == 0)
                {
                    CheckHighScoreSync();
                }
                if (aDisp == aScore)
                {
                    mMarqueesDirty = true;
                }
            }
        }

        public bool IsScoreAccurate()
        {
            for (int i = 0; i < 2; i++)
            {
                if (mDispScore[i] != mBoard.mLogicMgr.mScore[i])
                {
                    return false;
                }
            }
            return true;
        }

        public void SettleDown()
        {
            LogicMgr aLogic = mBoard.mLogicMgr;
            mDispScore[0] = aLogic.mScore[0];
            mDispScore[1] = aLogic.mScore[1];
            CheckHighScoreSync();
        }

        public void DoFeverMultBlink()
        {
            mFeverMultBlink = 1;
        }

        public void DoFever()
        {
            mFeverCount = 1;
            for (int i = 0; i < 0x19; i++)
            {
                mFeverLights[i] = 0;
            }
        }

        public void Update()
        {
#if !PEGGLE_IPHONE
            if (mUpdateCnt < int.MaxValue)
            {
                mUpdateCnt++;
            }
            else
            {
                mUpdateCnt = 0;
            }
#endif
            for (int i = 0; i < 2; i++)
            {
                if (mPodTextCount[i] != 0)
                {
                    mPodTextCount[i]++;
                    if (mPodTextCount[i] > 300)
                    {
                        mPodTextCount[i] = 0;
                    }
                }
                if (mScoreBlink[i] != 0)
                {
                    mScoreBlink[i]++;
                    if (mScoreBlink[i] > 0x77)
                    {
                        mScoreBlink[i] = 0;
                    }
                }
            }
            UpdateScoreDisp();
            if (mBottomBallDelay != 0)
            {
                mBottomBallDelay--;
                if (mBottomBallDelay == 0)
                {
                    AddBottomBall(0);
                }
            }
            UpdateShotMeter();
            UpdateBalls();
#if !PEGGLE_IPHONE
            UpdateDial();
#endif
            if (mFeverBlink != 0)
            {
                mFeverBlink++;
                if (mFeverBlink > 0xb3)
                {
                    mFeverBlink = 0;
                }
            }
            if (mFeverMultBlink != 0)
            {
                mFeverMultBlink++;
                if (mFeverMultBlink > 0x78)
                {
                    mFeverMultBlink = 0;
                }
            }
            if (mFeverStart != -1)
            {
                mFeverLightCount++;
                if (mFeverLightCount == 10)
                {
                    mFeverLightCount = 0;
                    int aLit = 0x19 - mBoard.mLogicMgr.mGoalPegs.Count;
                    if (mFeverStart < aLit)
                    {
                        if (mFeverLight == -1)
                        {
                            mFeverLight = mFeverStart;
                        }
                        else
                        {
                            mFeverLight++;
                        }
                        if (aLit <= mFeverLight)
                        {
                            mFeverLightCount = -0x1e;
                            mFeverLight = mFeverStart;
                        }
                    }
                    else
                    {
                        mFeverLight = -1;
                    }
                }
            }
            LogicMgr aLogic = mBoard.mLogicMgr;
#if PEGGLE_IPHONE
            // The instant replay button's cover opens while it is usable, and it blinks once open.
            if (!mBoard.mInstantReplayButton.mDisabled)
            {
                if (mButtonCover > 0)
                {
                    mButtonCover--;
                    if (mButtonCover == 0)
                    {
                        mBoard.mInstantReplayButton.Blink(aLogic.mDimLowObjects ? 8 : 1, false);
                    }
                }
            }
            else if (mButtonCover < 10)
            {
                mButtonCover++;
            }
#else
            bool aiming = aLogic.mState == LogicMgr.STATE_AIMING;
            bool feverIdle = aLogic.mDimLowObjects && mBoard.mZoomFrame == 0;
            if (aiming || feverIdle)
            {
                if (mFFwdCover > 0)
                {
                    mFFwdCover--;
                }
            }
            else if (mFFwdCover < 10)
            {
                mFFwdCover++;
            }
            if (aiming || feverIdle || aLogic.mState == LogicMgr.STATE_LEVEL_DONE)
            {
                if (mButtonCover > 0 && mBoard.mReplayButtonState != BALLOTRON_REPLAY)
                {
                    mButtonCover--;
                }
            }
            else if (mButtonCover < 10)
            {
                mButtonCover++;
            }
            if (mUpdateCnt % 5 == 0)
            {
                UpdateBallotron();
            }
#endif
            if (mBallCountBlink != 0)
            {
                mBallCountBlink--;
            }
            if (mFeverCount != 0)
            {
                mFeverCount++;
                int aPhase = mFeverCount / 10 % 6;
                for (int i = 0; i < 0x19; i++)
                {
                    if (aPhase == i % 6)
                    {
                        mFeverLights[i] = 0x14;
                    }
                    else if (mFeverLights[i] > 0)
                    {
                        mFeverLights[i]--;
                    }
                }
            }
            if (mMarqueesDirty)
            {
                UpdateMarquees();
            }
        }

#if !PEGGLE_IPHONE
        /// <summary>Turns the dial's drag into gun rotation and turns the gun angle back into the dial's position.</summary>
        public void UpdateDial()
        {
            Board.DialObj aDial = mBoard.mDialObj;
            if (mBallotronMode == BALLOTRON_DIAL)
            {
                if (!aDial.mDown && aDial.mDelta != 0)
                {
                    if (!mBoard.mPeggleZoom)
                    {
                        mBoard.mLogicMgr.MouseWheel((float)aDial.mDelta);
                    }
                    else
                    {
                        mBoard.mLogicMgr.MouseWheel((float)aDial.mDelta * 0.2f);
                    }
                    aDial.mDelta = aDial.mDelta * 0x5e / 100;
                }
                int anAngle = (int)SexyUtil.RadToDeg(mBoard.mGun.mAngle);
                if (anAngle < 0x97)
                {
                    if (anAngle > 0)
                    {
                        anAngle = -anAngle;
                    }
                }
                else
                {
                    anAngle = 0x168 - anAngle;
                }
                aDial.mDialPos = anAngle << 1;
            }
            else
            {
                aDial.mLastY = -1;
            }
        }
#endif

#if !PEGGLE_IPHONE
        /// <summary>Closes the Ballotron cover, swaps its contents to the board's requested mode, then opens it again.</summary>
        public void UpdateBallotron()
        {
            if (mBallotronMode == GetWantedBallotronMode() && mBallotronCount < 0)
            {
                return;
            }
            if (mBallotronState == 0)
            {
                mBallotronState = 1;
                mBallotronCount = 0;
            }
            else if (mBallotronState == 1)
            {
                if ((float)mBallotronCount < INTERFACE_MGR_BALLOTRON_COVER_ANIM_SPEED)
                {
                    mBallotronCount++;
                }
                else
                {
                    mBallotronState = 2;
                    mBallotronMode = GetWantedBallotronMode();
                }
            }
            else if (mBallotronState == 2)
            {
                mBallotronState = 3;
            }
            else if (mBallotronState == 3)
            {
                if (mBallotronCount == 0)
                {
                    mBallotronState = 0;
                    mBallotronCount = -1;
                }
                else
                {
                    mBallotronCount--;
                }
            }
        }
#endif

        public static void UpdatePaused()
        {
        }
    }
}
