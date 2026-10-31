using System;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The Peggle trophy room (Sexy::TrophyRoom): the ten stage badges and three trophies on the
    /// shelves, plus the ace-all trophy. Newly earned badges fade in one by one with a ring of
    /// stars; trophies rise out of the shelf with fever rays and a halo.
    /// </summary>
    public class TrophyRoom : ThunderDialog
    {
        // DESTINATION: where the Continue/Back link goes.
        public const int DESTINATION_ADVENTURE_SCREEN = 0;
        public const int DESTINATION_MAIN_MENU = 1;
        public const int DESTINATION_BOARD = 2;

        // 0x78: ButtonListener sub-object

        public ThunderballApp mApp;                               // 0x128

        public HyperlinkWidget mContinueLink;                     // 0x12c

        public int mTrophyEffectCount;                            // 0x130

        public int mTrophyEffectDuration;                         // 0x134

        public int mBadgeEffectCount;                             // 0x138

        public int mBadgeEffectDuration;                          // 0x13c

        public bool mShowingAced;                                 // 0x140

        public bool mAllAced;                                     // 0x141

        public int mCurBadge;                                     // 0x144 (-1 = nothing being revealed)

        public int mNumBadges;                                    // 0x148

        public bool mFirstBadge;                                  // 0x14c

        public int mDelay;                                        // 0x150

        public int mDestination;                                  // 0x154

        // Index 13 is the ace-all trophy.
#if PEGGLE_IPHONE
        // The iPhone's BadgeLocations table (480x320 units, scaled by mLayoutScaleFactor).
        private static readonly int[] BADGE_LOCATION_X = { 25, 23, 103, 73, 84, 27, 185, 90, 21, 201, 260, 129, 190, 202 };
        private static readonly int[] BADGE_LOCATION_Y = { 118, 255, 15, 222, 53, 90, 233, 268, 0, 0, 120, 122, 88, 210 };
        private static readonly int[] BADGE_LOCATION_XFLIP = { 353, 389, 280, 346, 274, 398, -1, 290, 351, -1, -1, -1, -1, -1 };
        private const int TROPHY_FADE_IN_DURATION = 0xfa;
        private const int TROPHY_TOP_COUNT = 0xaf;
#else
        private static readonly int[] BADGE_LOCATION_X = { 14, 5, 200, 130, 152, 12, 390, 172, 3, 436, 565, 245, 390, 427 };
        private static readonly int[] BADGE_LOCATION_Y = { 292, 626, 40, 538, 130, 225, 568, 654, 2, 10, 290, 280, 205, 513 };
        private static readonly int[] BADGE_LOCATION_XFLIP = { 793, 880, 618, 786, 605, 900, -1, 645, 800, -1, -1, -1, -1, -1 };
#endif
        private const int ACED_INDEX = 13;

        /// <summary>mLayoutScaleFactor: 1 for the iPad (1024x768) art, 2 for the iPhone's 960x640.</summary>
        private static int LAYOUT_SCALE_FACTOR => SexyUtil.mLayoutScaleFactor;

        public TrophyRoom(ThunderballApp theApp, int theDestination)
            : base(DialogId.DIALOG_TROPHY_ROOM, true, SexyAppBase.Loc(""), "", SexyAppBase.Loc(""), 0)
        {
            mApp = theApp;
            if (!mApp.mResourceManager.LoadResources("TrophyRoom"))
            {
                mApp.ShowResourceError(true);
            }
            if (!Res.ExtractTrophyRoomResources(mApp.mResourceManager, true))
            {
                mApp.ShowResourceError(true);
            }

            mContinueLink = new HyperlinkWidget(3000, this);
            mContinueLink.SetFont(SexyUtil.GetFontById(8));
            mContinueLink.mColor = SexyUtil.MakeColor(0xffff88);
            mContinueLink.mOverColor = SexyUtil.MakeColor(0x88ff88);
            mContinueLink.mDoFinger = true;
#if PEGGLE_IPHONE
            // Reached from the Stats screen (there is no Trophies button on the iPhone main menu).
            mContinueLink.mLabel = theDestination != DESTINATION_ADVENTURE_SCREEN ? SexyAppBase.Loc("Continue") : SexyAppBase.Loc("Back");
#else
            mContinueLink.mLabel = theDestination != DESTINATION_MAIN_MENU ? SexyAppBase.Loc("Continue") : SexyAppBase.Loc("Back");
#endif
            mContinueLink.mUnderlineSize = TROPHY_ROOM_UNDERLINE_H;
            mContinueLink.mUnderlineOffset = mContinueLink.mFont.GetDescent() - mContinueLink.mUnderlineSize;
            int aWidth = mContinueLink.mFont.StringWidth(mContinueLink.mLabel) + 8;
            mContinueLink.Resize((SCREEN_WIDTH - aWidth) / 2, TROPHY_ROOM_CONT_BUTTON_Y, aWidth, mContinueLink.mFont.GetHeight() + TROPHY_ROOM_CONT_BUTTON_H);
            AddWidget(mContinueLink);

            mDestination = theDestination;
            mTrophyEffectCount = 0;
            mTrophyEffectDuration = 0;
            mBadgeEffectCount = 0;
            mBadgeEffectDuration = 0;

            // One badge per beaten stage, then the all-trophies and all-levels trophies.
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            mNumBadges = aPlayer.mMaxStage;
            if (aPlayer.GetmTrophySet().Count >= TrophyMgr.GetNumTrophiesCurGame())
            {
                mNumBadges++;
                if (aPlayer.mClearedLevelSet.Count >= StageMgr.GetNumLevelsCurGame())
                {
                    mNumBadges++;
                }
            }
            if (aPlayer.mBadgeCount == mNumBadges)
            {
                mContinueLink.SetVisible(true);
                mCurBadge = -1;
            }
            else
            {
                mCurBadge = aPlayer.mBadgeCount;
                aPlayer.SetBadgeCount(mNumBadges);
                mContinueLink.SetVisible(false);
            }
            mFirstBadge = true;
            mDelay = 0;
#if PEGGLE_IPHONE
            mAllAced = aPlayer.mAcedLevelSet.Count >= StageMgr.GetNumLevelsCurGame();
#else
            mAllAced = aPlayer.mAcedLevelSet.Count >= StageMgr.GetNumLevelsCurGame() || aPlayer.mShownAceAllStory;
#endif
            mShowingAced = false;
            if (mAllAced && !aPlayer.mShownAceAllStory)
            {
                mContinueLink.SetVisible(false);
            }
        }

        public override void Dispose()
        {
            mApp.mResourceManager.DeleteResources("TrophyRoom");
            base.Dispose();
        }

        public void LeaveRoom()
        {
            if (mDestination == DESTINATION_MAIN_MENU)
            {
                GlobalStaticVars.gSexyAppBase.KillDialog(this);
                mApp.DoToMenu();
            }
            else if (mDestination == DESTINATION_BOARD)
            {
                if (mApp.mPlayerInfo.GetmNeedShowStory())
                {
                    mApp.ShowStoryScreen(false, false);
                }
                else
                {
                    mApp.ShowBoard(false, true, false, false);
                }
            }
            else if (mDestination == DESTINATION_ADVENTURE_SCREEN)
            {
                mApp.ShowAdventureScreen(false);
            }
        }

        public override void ButtonDepress(int theId)
        {
            if (theId == 3000)
            {
                LeaveRoom();
            }
            else
            {
                base.ButtonDepress(theId);
            }
        }

        public void StartTrophyEffect()
        {
            mTrophyEffectCount = 0;
            mTrophyEffectDuration = TROPHY_FADE_IN_DURATION;
        }

        public void StartBadgeEffect()
        {
            mBadgeEffectCount = 0;
            mBadgeEffectDuration = 250;
        }

        public void DrawTrophyEffect(Graphics g)
        {
            Image anImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_TR_BADGE00_ID + mCurBadge);
            int aScale = LAYOUT_SCALE_FACTOR;
            int aX = BADGE_LOCATION_X[mCurBadge] * aScale;
            int aY = BADGE_LOCATION_Y[mCurBadge] * aScale;
            int aBaseY = (int)((float)(aY + anImage.mHeight - TROPHY_ROOM_HALO_OFFSET_Y) - (float)aScale * 0.75f * (float)mTrophyEffectCount);
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            g.SetColorizeImages(true);
            int aWidth = anImage.mWidth + TROPHY_ROOM_TROPHY_OFFSET_W;
            int aCenterX = aX + aWidth / 2 - TROPHY_ROOM_TROPHY_OFFSET_W;
            float aRadius = aWidth / 2;
            int aTopCount = TROPHY_TOP_COUNT;

            // Spinning halo under the trophy.
            if (g.Is3D())
            {
                int anAlpha = 0xff;
                if (aTopCount < mTrophyEffectCount)
                {
                    anAlpha = 0xff - (mTrophyEffectCount - aTopCount) * 0xf;
                }
                if (anAlpha > 0)
                {
                    g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                    Transform aTrans = new Transform();
                    aTrans.RotateRad((float)mTrophyEffectCount / -12f);
                    aTrans.Scale(1f, 0.12f);
                    g.DrawImageTransform(Res.IMAGE_STORY_TROPHYREVEAL, aTrans, (float)aCenterX, (float)(TROPHY_ROOM_TROPHYREVEAL_OFFSET_Y + aBaseY));
                }
            }

            // Fever rays sweeping back and forth.
            int aRayAlpha = 0xff;
            if (aTopCount < mTrophyEffectCount)
            {
                aRayAlpha = 0xff - (mTrophyEffectCount - aTopCount) * 0xf;
            }
            if (aRayAlpha > 0)
            {
                g.SetColor(SexyUtil.MakeColor(0x101010, aRayAlpha));
                int aNumRays = 15;
                for (int i = 0; i < aNumRays; i++)
                {
                    int aRayX = (int)((double)aCenterX + (double)aRadius * Math.Sin((double)((float)mTrophyEffectCount / 120f + (float)i * 6.2831855f / (float)aNumRays)));
                    if (g.Is3D())
                    {
                        Image aRay = Res.IMAGE_FEVERRAY;
                        g.DrawImageRotated(aRay, TROPHY_ROOM_FEVERRAY_X + aRayX - aRay.mWidth / 2, TROPHY_ROOM_FEVERRAY_Y + (aBaseY - aRay.mHeight), (double)(aCenterX - aRayX) * 0.005);
                    }
                }
            }

            // Ring of stars orbiting the trophy, flying apart once it is fully up.
            g.SetColorizeImages(true);
            int aNumStars = 150;
            for (int i = 0; i < aNumStars; i++)
            {
                int anAlpha = 0xff;
                if (aTopCount < mTrophyEffectCount)
                {
                    anAlpha -= (mTrophyEffectCount - aTopCount) * 0xff / (i * 13 % 98 + 2);
                }
                if (anAlpha <= 0)
                {
                    continue;
                }
                g.SetColor(SexyUtil.MakeColor(anImage == Res.IMAGE_TR_BADGE10_TROPHY_STAGES ? 0x404080 : 0x808040, anAlpha));
                float anAngle = (float)mTrophyEffectCount / 12f + (float)i * 6.2831855f / (float)aNumStars;
                int aStarX = (int)((double)aCenterX + (double)aRadius * Math.Sin((double)anAngle));
                int aStarY = (int)((double)aBaseY - Math.Cos((double)anAngle) * 8.0);
                if (aBaseY - 15 <= aStarY)
                {
                    int aDrawY = (int)((float)aStarY + (float)(i * 0x55b % 0x3f + i * 0x139 % 0x25) / 3f - 5f);
                    if (aTopCount < mTrophyEffectCount)
                    {
                        aStarX = (int)((float)aStarX + (float)(mTrophyEffectCount - aTopCount) * 0.1f * (float)(i * 0x25 % 0x33) * (i % 2 != 0 ? -1f : 1f));
                        aDrawY = (int)((float)aDrawY + (float)(mTrophyEffectCount - aTopCount) * 0.1f * (float)(i * 0x33 % 0x25) * (i % 4 / 2 != 0 ? -1f : 1f));
                    }
                    g.DrawImage(Res.IMAGE_SPACESTAR, aStarX, aDrawY, TROPHY_ROOM_SPACE_STAR_W, TROPHY_ROOM_SPACE_STAR_W);
                }
            }
            g.SetColorizeImages(false);
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
        }

        public void DrawBadgeEffect(Graphics g)
        {
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            g.SetColorizeImages(true);
            int anIndex = mShowingAced ? ACED_INDEX : mCurBadge;
            Image anImage = mShowingAced ? Res.IMAGE_DLG_TROPHY_ACED : SexyUtil.GetImageById((int)ResourceId.IMAGE_TR_BADGE00_ID + mCurBadge);
            int aWidth = anImage.mWidth;
            int aHeight = anImage.mHeight;
            int aScale = LAYOUT_SCALE_FACTOR;
            int aX = BADGE_LOCATION_X[anIndex] * aScale;
            int aY = BADGE_LOCATION_Y[anIndex] * aScale;
            int aFlipX = BADGE_LOCATION_XFLIP[anIndex] * aScale;
            int aCopies = aFlipX < 0 ? 1 : 2;
            for (int aCopy = 0; aCopy < aCopies; aCopy++)
            {
                int aCenterX = (aCopy == 0 ? aX : aFlipX) + aWidth / 2;
                int aCenterY = aY + aHeight / 2;
                float aRadius = Math.Max(aWidth, aHeight) / 5;
                int aStartFly = 0xaf;
                g.SetColorizeImages(true);
                int aNumStars = 300;
                for (int i = 0; i < aNumStars; i++)
                {
                    int anAlpha = 0xff;
                    if (aStartFly < mBadgeEffectCount)
                    {
                        anAlpha -= (mBadgeEffectCount - aStartFly) * 0xff / (i * 13 % 98 + 2);
                    }
                    if (anAlpha <= 0)
                    {
                        continue;
                    }
                    g.SetColor(SexyUtil.MakeColor(0x404080, anAlpha));
                    float anAngle = (float)mBadgeEffectCount / 12f + (float)i * 6.2831855f / (float)aNumStars;
                    float aFly = (float)(mBadgeEffectCount - aStartFly) * 0.1f;
                    int aDX = (int)((float)(int)((double)aRadius * Math.Cos((double)anAngle)) + aFly * (float)(i * 0x25 % 0x33) * (i % 2 != 0 ? -1f : 1f));
                    int aDY = (int)((float)(int)((double)aRadius * Math.Sin((double)anAngle)) + aFly * (float)(i * 0x33 % 0x25) * (i % 4 / 2 != 0 ? -1f : 1f));
                    g.DrawImage(Res.IMAGE_SPACESTAR, aCenterX + aDX - 5, aCenterY + aDY - 5, TROPHY_ROOM_SPACE_STAR_W, TROPHY_ROOM_SPACE_STAR_W);
                }
            }
            g.SetColorizeImages(false);
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
        }

        public override void Draw(Graphics g)
        {
            int aScale = LAYOUT_SCALE_FACTOR;
            int aNumShown = mCurBadge < 0 ? mNumBadges : mCurBadge;
#if PEGGLE_IPHONE
            g.DrawImage(Res.IMAGE_TROPHYROOM, (mWidth - Res.IMAGE_TROPHYROOM.mWidth) / 2 - SexyUtil.gGutterWidth, 0);
#else
            g.DrawImage(Res.IMAGE_TROPHYROOM, 0, 0);
#endif
            if (mNumBadges < 11)
            {
                g.DrawImage(Res.IMAGE_DLG_TROPHY_COVER, TROPHY_ROOM_TROPHY_COVER_X, TROPHY_ROOM_TROPHY_COVER_Y);
            }
            for (int i = 0; i < aNumShown; i++)
            {
                Image aBadge = SexyUtil.GetImageById((int)ResourceId.IMAGE_TR_BADGE00_ID + i);
                g.DrawImage(aBadge, BADGE_LOCATION_X[i] * aScale, aScale * BADGE_LOCATION_Y[i]);
                if (BADGE_LOCATION_XFLIP[i] * aScale >= 0)
                {
                    g.DrawImageMirror(aBadge, BADGE_LOCATION_XFLIP[i] * aScale, aScale * BADGE_LOCATION_Y[i], true);
                }
            }
            if (mApp.mPlayerInfo.mShownAceAllStory)
            {
                g.DrawImage(Res.IMAGE_DLG_TROPHY_ACED, BADGE_LOCATION_X[ACED_INDEX] * aScale, BADGE_LOCATION_Y[ACED_INDEX] * aScale);
            }

            if (mDelay != 0 || (mCurBadge < 0 && !mShowingAced))
            {
                return;
            }
            if (mTrophyEffectCount < mTrophyEffectDuration)
            {
                // The trophy rises out of the shelf.
                Image anImage = SexyUtil.GetImageById((int)ResourceId.IMAGE_TR_BADGE00_ID + mCurBadge);
                int aX = aScale * BADGE_LOCATION_X[mCurBadge];
                int aY = aScale * BADGE_LOCATION_Y[mCurBadge];
                int aHidden = (int)((float)anImage.mHeight + (float)aScale * -0.75f * (float)mTrophyEffectCount);
                Graphics gClip = g.Create();
                gClip.ClipRect(new TRect(aX, aY + aHidden, anImage.mWidth, anImage.mHeight - aHidden));
                gClip.DrawImage(anImage, aX, aY);
                DrawTrophyEffect(g);
            }
            else if (mBadgeEffectCount <= mBadgeEffectDuration)
            {
                int aFadeStart = 0x86;
                int anIndex = mShowingAced ? ACED_INDEX : mCurBadge;
                Image anImage = mShowingAced ? Res.IMAGE_DLG_TROPHY_ACED : SexyUtil.GetImageById((int)ResourceId.IMAGE_TR_BADGE00_ID + mCurBadge);
                int aX = BADGE_LOCATION_X[anIndex] * aScale;
                int aY = BADGE_LOCATION_Y[anIndex] * aScale;
                int aFlipX = BADGE_LOCATION_XFLIP[anIndex] * aScale;
                g.SetColorizeImages(true);
                int anAlpha = SexyUtil.ClampRange((int)((float)(mBadgeEffectCount - aFadeStart) * 2.5f), 0, 0xff);
                g.SetColor(SexyUtil.MakeColor(0xffffff, anAlpha));
                g.DrawImage(anImage, aX, aY);
                if (aFlipX >= 0)
                {
                    g.DrawImageMirror(anImage, aFlipX, aY, true);
                }
                g.SetColorizeImages(false);
                DrawBadgeEffect(g);
            }
        }

        public override void Update()
        {
            if (IsScrolling())
            {
                return;
            }
            base.Update(); // Widget::Update; ThunderDialog only adds scrolling, which IsScrolling ruled out
            if (mDelay != 0)
            {
                mDelay--;
                return;
            }
            PlayerInfo aPlayer = mApp.mPlayerInfo;
            if (mTrophyEffectCount < mTrophyEffectDuration)
            {
                mTrophyEffectCount++;
                MarkDirty();
                if (mTrophyEffectCount == 160)
                {
                    mApp.PlaySample(Res.SOUND_AWARD);
                }
                else if (mTrophyEffectCount == 10)
                {
                    mApp.PlaySample(Res.SOUND_APPLAUSE);
                }
            }
            else if (mBadgeEffectCount < mBadgeEffectDuration)
            {
                mBadgeEffectCount++;
                if (!mShowingAced)
                {
                    if (mBadgeEffectCount == 20)
                    {
                        mApp.PlaySample(Res.SOUND_APPLAUSE);
                    }
                }
                else if (mBadgeEffectCount == 160)
                {
                    mApp.PlaySample(Res.SOUND_AWARD);
                }
                else if (mBadgeEffectCount == 10)
                {
                    mApp.PlaySample(Res.SOUND_APPLAUSE);
                }
            }
            else if (mCurBadge < 0)
            {
                if (mAllAced && !aPlayer.mShownAceAllStory)
                {
                    if (!mShowingAced)
                    {
                        mDelay = 100;
                        mShowingAced = true;
                        StartBadgeEffect();
                    }
                    else
                    {
                        mShowingAced = false;
                        aPlayer.mShownAceAllStory = true;
                        mContinueLink.SetVisible(true);
                    }
                }
            }
            else
            {
                mDelay = 100;
                if (mFirstBadge)
                {
                    mFirstBadge = false;
                }
                else
                {
                    mCurBadge++;
                }
                if (mCurBadge < mNumBadges)
                {
                    if (mCurBadge < 10)
                    {
                        StartBadgeEffect();
                    }
                    else
                    {
                        StartTrophyEffect();
                    }
                }
                else
                {
                    mCurBadge = -1;
                    if (!mAllAced || aPlayer.mShownAceAllStory)
                    {
                        mContinueLink.SetVisible(true);
                    }
                }
            }
        }
    }
}
