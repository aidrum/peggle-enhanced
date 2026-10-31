using System;
using static Sexy.Layout;

namespace Sexy
{
    public partial class CharacterMgr
    {
        private static bool sCalcEyePosRecursive;

        /// <summary>
        /// The "image" key: loads characters/1024x768/&lt;name&gt; with theFrames vertical frames, and for
        /// every frame cuts the eye area (where &lt;name&gt;mask is not opaque) out of the colour image into
        /// one packed image, so the eyes can be drawn between the body and that cut-out. &lt;name&gt;mask2
        /// bounds how far the pupils may travel (see FindEyePos).
        /// </summary>
        private void LoadCharacterImage(ConfigParser theParser, CharacterInfo theInfo, string thePath, int theFrames)
        {
            string aMaskPath = thePath + "mask";
            string aMask2Path = thePath + "mask2";
            ImageLibImage aColorBits = ImageLib.GetImage(thePath, true);
            Image anImage = GlobalStaticVars.gSexyAppBase.GetImage(thePath, true);
            ImageLibImage aMask = ImageLib.GetImage(aMaskPath, true);
            ImageLibImage aMask2 = ImageLib.GetImage(aMask2Path, true);
            if (anImage == null)
            {
                theParser.Error("Image not found: " + thePath, false, true);
                return;
            }
            if (aMask == null)
            {
                theParser.Error("Image not found: " + aMaskPath, false, true);
                return;
            }
            if (aMask.mWidth != anImage.mWidth || aMask.mHeight != anImage.mHeight)
            {
                theParser.Error("Mask size must be the same as image size", false, true);
            }
            if (aMask2 != null && (aMask2.mWidth != anImage.mWidth || aMask2.mHeight != anImage.mHeight))
            {
                theParser.Error("Mask2 size must be the same as image size", false, true);
            }
            if (theFrames < 2)
            {
                mFrameCountdown = 0;
            }
            else
            {
                mFrameCountdown = theFrames + 1;
                mNumFrames = theFrames;
            }
            mImageMgr.AddDeleteImage(anImage);
            int aFrameHeight = anImage.mHeight / theFrames;
            int aMaxWidth = 0;
            int aMaxHeight = 0;
            int aFirst = theInfo.mImages.Count;
            for (int i = 0; i < theFrames; i++)
            {
                CharacterImage aFrame = new CharacterImage();
                theInfo.mImages.Add(aFrame);
                TRect aFrameRect = new TRect(0, aFrameHeight * i, anImage.mWidth, aFrameHeight);
                aFrame.mSrcImage = anImage;
                aFrame.mFrameRect = aFrameRect;
                TRect aBorder = FindAlphaBorder(aMask, aFrameRect);
                if (aBorder.mWidth < 1 || aBorder.mHeight < 1)
                {
                    theParser.Error(aMaskPath + " messed.", false, true);
                }
                aFrame.mBorderRect = aBorder;
                TRect aCut = aBorder;
                if (aCut.mX > 0)
                {
                    aCut.mX--;
                    aCut.mWidth++;
                }
                if (aCut.mX + aCut.mWidth < aFrameRect.mX + aFrameRect.mWidth)
                {
                    aCut.mWidth++;
                }
                if (aCut.mY > 0)
                {
                    aCut.mY--;
                    aCut.mHeight++;
                }
                if (aCut.mY + aCut.mHeight < aFrameRect.mY + aFrameRect.mHeight)
                {
                    aCut.mHeight++;
                }
                if (aMask2 != null)
                {
                    TRect aBorder2 = FindAlphaBorder(aMask2, aFrameRect);
                    if (aBorder2.mWidth < 1 || aBorder2.mHeight < 1)
                    {
                        theParser.Error(aMask2Path + " messed.", false, true);
                    }
                    aFrame.mMask2Image = CopyImage(aMask2, aBorder2);
                    aFrame.mMask2Pos = new TPoint(aBorder2.mX, aBorder2.mY - aFrameRect.mY);
                    aFrame.mMask2Rect = new TRect(0, 0, aBorder2.mWidth, aBorder2.mHeight);
                }
                aMaxWidth = Math.Max(aCut.mWidth, aMaxWidth);
                aMaxHeight = Math.Max(aCut.mHeight, aMaxHeight);
                aFrame.mCutRect = aCut;
                aFrame.mDrawOffset = new TPoint(aFrame.mBorderRect.mX, aFrame.mBorderRect.mY - aFrameRect.mY);
            }
            ImageLibImage aPacked = new ImageLibImage(aMaxWidth, theFrames * aMaxHeight);
            for (int i = 0; i < theFrames; i++)
            {
                CharacterImage aFrame = theInfo.mImages[aFirst + i];
                CutoutImageRectFromAlpha(aColorBits, aMask, aFrame.mCutRect, aPacked, aFrame.mCutRect.mX, aFrame.mCutRect.mY, 0, i * aMaxHeight);
                // A snapshot per frame, as the original builds a new Image from the bits each iteration.
                aFrame.mImage = aPacked.CreateImage();
                aFrame.mCutRect = new TRect(aFrame.mBorderRect.mX - aFrame.mCutRect.mX,
                    i * aMaxHeight + aFrame.mBorderRect.mY - aFrame.mCutRect.mY,
                    aFrame.mBorderRect.mWidth, aFrame.mBorderRect.mHeight);
            }
        }

        /// <summary>FindAlphaBorder: bounds of the pixels inside theRect that are not fully opaque.</summary>
        private static TRect FindAlphaBorder(ImageLibImage theImage, TRect theRect)
        {
            int aPitch = theImage.mWidth;
            int aWidth = theRect.mWidth;
            int aHeight = theRect.mHeight;
            uint[] aBits = theImage.GetBits();
            int aRow = theRect.mY * aPitch + theRect.mX;
            int aLeft = aWidth;
            int aTop = aHeight;
            int aRight = 0;
            int aBottom = 0;
            for (int y = 0; y < aHeight; y++)
            {
                bool found = false;
                for (int x = 0; x < aWidth; x++)
                {
                    if ((aBits[aRow + x] & 0xff000000) != 0xff000000)
                    {
                        if (!found)
                        {
                            aLeft = Math.Min(x, aLeft);
                            found = true;
                        }
                        else
                        {
                            aRight = Math.Max(x, aRight);
                        }
                    }
                }
                if (!found)
                {
                    if (aTop != aHeight)
                    {
                        break;
                    }
                }
                else
                {
                    if (aTop == aHeight)
                    {
                        aTop = y;
                    }
                    aBottom = y;
                }
                aRow += aPitch;
            }
            return new TRect(aLeft + theRect.mX, aTop + theRect.mY, aRight - aLeft + 1, aBottom - aTop + 1);
        }

        private static ImageLibImage CopyImage(ImageLibImage theImage, TRect theRect)
        {
            ImageLibImage aCopy = new ImageLibImage(theRect.mWidth, theRect.mHeight);
            for (int y = 0; y < theRect.mHeight; y++)
            {
                Array.Copy(theImage.mBits, (theRect.mY + y) * theImage.mWidth + theRect.mX, aCopy.mBits, y * aCopy.mWidth, theRect.mWidth);
            }
            return aCopy;
        }

        /// <summary>
        /// Copies theRect of theImage into theDest at (theDestX, theDestY), keeping only pixels where
        /// theAlpha (read at theAlphaX, theAlphaY) is not transparent, then premultiplies all of theDest.
        /// </summary>
        private static void CutoutImageRectFromAlpha(ImageLibImage theImage, ImageLibImage theAlpha, TRect theRect, ImageLibImage theDest,
            int theAlphaX, int theAlphaY, int theDestX, int theDestY)
        {
            uint[] aSrc = theImage.mBits;
            uint[] aMask = theAlpha.mBits;
            uint[] aDst = theDest.mBits;
            for (int y = 0; y < theRect.mHeight; y++)
            {
                int aMaskRow = (theAlphaY + y) * theAlpha.mWidth + theAlphaX;
                int aSrcRow = (theRect.mY + y) * theImage.mWidth + theRect.mX;
                int aDstRow = (theDestY + y) * theDest.mWidth + theDestX;
                for (int x = 0; x < theRect.mWidth; x++)
                {
                    uint aMaskA = aMask[aMaskRow + x] & 0xff000000;
                    if (aMaskA == 0)
                    {
                        aDst[aDstRow + x] = 0;
                    }
                    else
                    {
                        uint aPixel = aSrc[aSrcRow + x];
                        uint aSrcA = aPixel & 0xff000000;
                        if (aSrcA < 0xff000000)
                        {
                            if (aSrcA != 0)
                            {
                                aDst[aDstRow + x] = ((aPixel >> 24) * (aMaskA >> 8)) & 0xff000000 | aPixel & 0xffffff;
                            }
                        }
                        else
                        {
                            aDst[aDstRow + x] = aMaskA | aPixel & 0xffffff;
                        }
                    }
                }
            }
            for (int i = 0; i < aDst.Length; i++)
            {
                uint p = aDst[i];
                uint a = p >> 24;
                aDst[i] = (a << 24) | ((a * ((p >> 16) & 0xff) / 0xff) << 16) | ((a * ((p >> 8) & 0xff) / 0xff) << 8) | (a * (p & 0xff) / 0xff);
            }
        }

        /// <summary>
        /// FindEyePos: walks from the eye's rest position towards theLookPos until mask2 turns opaque,
        /// and returns the pupil centre that far along (at least 5 pixels). theSide limits the walk
        /// to the left (1) or right (2) half of the mask when the character has two eyes.
        /// </summary>
        private static SexyVector2 FindEyePos(ImageLibImage theMask2, Image theEyeImage, SexyVector2 theEyeOffset, SexyVector2 theEyeCenter,
            SexyVector2 theCharPos, SexyVector2 theLookPos, SexyVector2 theDefault, bool theNoWalk, TRect theMask2Rect, TPoint theMask2Pos, int theSide)
        {
            SexyVector2 anEyePos = theCharPos + theEyeOffset;
#if PEGGLE_IPHONE
            // The iPhone version has no downward bias and no per-eye half limit (theSide is unused).
            SexyVector2 aDir = (theLookPos - theCharPos - theEyeCenter).Normalize();
            theSide = 0;
#else
            SexyVector2 aDir = ((theLookPos + new SexyVector2(0.0f, 250.0f)) - theCharPos - theEyeCenter).Normalize();
#endif
            if (!(Math.Abs((double)aDir.x) > 0.1f) && !(Math.Abs((double)aDir.y) > 0.1f))
            {
                return theDefault;
            }
            SexyVector2 aStart = theEyeOffset;
            aStart.x = aStart.x - theMask2Pos.mX + theMask2Rect.mX;
            aStart.y = aStart.y - theMask2Pos.mY + theMask2Rect.mY;
            int aLeft = theMask2Rect.mX;
            int aTop = theMask2Rect.mY;
            int aRight = aLeft + theMask2Rect.mWidth;
            int aBottom = aTop + theMask2Rect.mHeight;
            if (theSide == 1)
            {
                aRight = theMask2Rect.mX + theMask2Rect.mWidth / 2;
            }
            if (theSide == 2)
            {
                aLeft = theMask2Rect.mX + theMask2Rect.mWidth / 2;
                aRight = theMask2Rect.mX + theMask2Rect.mWidth;
            }
            int aDist = 0;
            if (!theNoWalk)
            {
                uint[] aBits = theMask2.GetBits();
                while (true)
                {
                    SexyVector2 aPos = aStart + aDir * (float)aDist;
                    int px = (int)aPos.x;
                    int py = (int)aPos.y;
                    if (px < aLeft || py < aTop || px >= aRight || py >= aBottom ||
                        (aBits[px + py * theMask2.mWidth] & 0xff000000) >= 0x64000000)
                    {
                        break;
                    }
                    aDist++;
                }
            }
#if PEGGLE_IPHONE
            aDist = aDist - theEyeImage.mWidth / 2 - 1;
            if (aDist < 0)
            {
                aDist = 0;
            }
#else
            aDist = aDist - (theEyeImage.mWidth / 4 + theEyeImage.mHeight / 4) - 1;
            if (aDist < 5)
            {
                aDist = 5;
            }
#endif
            return anEyePos + aDir * (float)aDist;
        }

        /// <summary>ConvergeVectors: moves theCur towards theTarget by half the distance, clamped to 0.5..3.</summary>
        private static void ConvergeVectors(ref SexyVector2 theCur, SexyVector2 theTarget)
        {
            SexyVector2 aDelta = theTarget - theCur;
            float aMagSq = aDelta.MagnitudeSquared();
            if (aMagSq < 1.0f)
            {
                theCur = theTarget;
            }
            else
            {
                float aStep = SexyUtil.ClampRange((float)Math.Sqrt(aMagSq) / 2.0f, 0.5f, 3.0f);
                theCur += aDelta.Normalize() * aStep;
            }
        }

        public void CalcEyePos(bool snap)
        {
            if (mCharacters.Count == 0)
            {
                return;
            }
            CharacterInfo anInfo = mCharacters[mCurCharacter];
            CharacterImage anImage = anInfo.GetCurImage();
            if (anImage == null)
            {
                return;
            }
            TRect aFrameRect = anImage.mFrameRect;
            float aHeight = SexyUtil.gPhysicalDisplayHeight;
            float aCharX = (float)((double)(anInfo.mPosX * aHeight) / 600.0 - aFrameRect.mWidth / 2 + anImage.mCharOffX);
            float aCharY = (float)((double)(anInfo.mPosY * aHeight) / 600.0 - aFrameRect.mHeight / 2 + anImage.mCharOffY + CHAR_MGR_CHAR_POS_OFFSET_Y);
            SexyVector2 anEye1 = anImage.mEye1;
            SexyVector2 anEye2 = anImage.mEye2;
            SexyVector2 aCenter = new SexyVector2((anEye1.x + anEye2.x) / 2.0f, (anEye1.y + anEye2.y) / 2.0f);
            SexyVector2 aCharPos = new SexyVector2(aCharX, aCharY);
            if (anEye1.x >= 0.0f)
            {
                mEyePos1 = FindEyePos(anImage.mMask2Image, anInfo.mEyeImage, anEye1, aCenter, aCharPos, mLookPos, mEyePos1, mCrossEyeFix,
                    anImage.mMask2Rect, anImage.mMask2Pos, anEye2.x < 0.0f ? 0 : 1);
            }
            if (anEye2.x >= 0.0f)
            {
                mEyePos2 = FindEyePos(anImage.mMask2Image, anInfo.mEyeImage2, anEye2, aCenter, aCharPos, mLookPos, mEyePos2, mCrossEyeFix,
                    anImage.mMask2Rect, anImage.mMask2Pos, 2);
            }
            if (!snap)
            {
                ConvergeVectors(ref mCurEyePos1, mEyePos1);
                ConvergeVectors(ref mCurEyePos2, mEyePos2);
            }
            else
            {
                int n = anInfo.mImages.Count;
                mTweenCounter = (n < 3 || anInfo.mCurFrame == 0 || anInfo.mCurFrame == n - 1) ? 0x14 : anInfo.mTweenSpeed;
                mCurEyePos1 = mEyePos1;
                mCurEyePos2 = mEyePos2;
            }
            if (anInfo.mImages.Count > 1 && mTweenCounter == 0 && !sCalcEyePosRecursive)
            {
                int aCount = anInfo.mImages.Count;
                int aCurFrame = anInfo.mCurFrame;
                int aTurnLine = (int)((double)(aCharX + (float)(aFrameRect.mWidth / 2)) * 600.0 / (double)aHeight);
                if (!snap)
                {
                    aTurnLine += anInfo.mFlag60 ? anInfo.mTurnLine : -anInfo.mTurnLine;
                }
                int aNewFrame = aCurFrame;
                if ((float)aTurnLine <= mLookPos.x)
                {
                    if (aCurFrame < aCount - 1)
                    {
                        aNewFrame = aCurFrame + 1;
                    }
                }
                else if (aCurFrame > 0)
                {
                    aNewFrame = aCurFrame - 1;
                }
                anInfo.mCurFrame = aNewFrame;
                if (aNewFrame != aCurFrame)
                {
                    anInfo.mFlag60 = mLookPos.x < (float)aTurnLine;
                    sCalcEyePosRecursive = true;
                    CalcEyePos(true);
                    sCalcEyePosRecursive = false;
                }
            }
        }

        /// <summary>Renders the three launcher cels into mShooterImage.</summary>
        public void CreateShooterImage(int theColor)
        {
            mAppQuality = 0; // SexyAppBase+0x188
            bool isNew = mShooterImage == null;
            if (isNew)
            {
                mShooterImage = new MemoryImage();
                mShooterImage.Create(CHAR_MGR_SHOOTER_X, CHAR_MGR_SHOOTER_Y);
            }
            mShooterImage.mNumRows = 3;
            Graphics g = Graphics.GetNew(mShooterImage);
            if (!isNew)
            {
                mShooterImage.Clear();
            }
            int aHeight = SexyUtil.gPhysicalDisplayHeight;
#if PEGGLE_IPHONE
            g.DrawImageCel(Res.IMAGE_IP_SHOOTER, (int)((double)(aHeight * 10) / 600.0), (int)((double)(aHeight * 3) / 600.0), 0);
            g.DrawImageCel(Res.IMAGE_IP_SHOOTER, (int)((double)(aHeight * 10) / 600.0), (int)((double)(aHeight * 0x43) / 600.0), 1);
            g.DrawImageCel(Res.IMAGE_IP_SHOOTER, (int)((double)(aHeight * 10) / 600.0), (int)((double)(aHeight * 0x83) / 600.0), 2);
#else
            g.DrawImageCel(Res.IMAGE_IP_SHOOTER, (int)((double)(aHeight * CHAR_MGR_SHOOTER_CELL_0_X) / 600.0), (int)((double)(CHAR_MGR_SHOOTER_CELL_0_Y * aHeight) / 600.0), 0);
            g.DrawImageCel(Res.IMAGE_IP_SHOOTER, (int)((double)(aHeight * CHAR_MGR_SHOOTER_CELL_1_X) / 600.0), (int)((double)(CHAR_MGR_SHOOTER_CELL_1_Y * aHeight) / 600.0), 1);
            g.DrawImageCel(Res.IMAGE_IP_SHOOTER, (int)((double)(aHeight * CHAR_MGR_SHOOTER_CELL_2_X) / 600.0), (int)((double)(CHAR_MGR_SHOOTER_CELL_2_Y * aHeight) / 600.0), 2);
#endif
            g.SetRenderTarget(null);
            g.PrepareForReuse();
        }

        public static void DoCharMove(int theX, int theY, bool unused)
        {
        }

        /// <summary>Keeps the pupils centred when the look target sits on the character's own face.</summary>
        public void CheckCrossEyeFix()
        {
            CharacterInfo anInfo = mCharacters[mCurCharacter];
            CharacterImage anImage = anInfo.GetCurImage();
            if (anImage == null)
            {
                return;
            }
            float aCharX = anInfo.mPosX - (float)(anImage.mFrameRect.mWidth / 2) + (float)anImage.mCharOffX;
            float aCharY = anInfo.mPosY - (float)(anImage.mFrameRect.mHeight / 2) + (float)anImage.mCharOffY;
            int aLeft = (int)(anImage.mEye1.x - 12.0f + aCharX);
            int aRight = (int)(anImage.mEye2.x + 12.0f + aCharX);
            int aTop = (int)(anImage.mEye1.y - 12.0f + aCharY);
            int aBottom = (int)((float)(aTop + 0x16) + aCharY);
            if (!(mLookPos.y > aCharY) && !(mLookPos.x < aCharX - 37.0f) && !(mLookPos.x > aCharX + 37.0f))
            {
                mCrossEyeFix = true;
                return;
            }
            mCrossEyeFix = !(mLookPos.x < (float)aLeft) && !(mLookPos.x > (float)aRight) &&
                !(mLookPos.y < (float)aTop) && !(mLookPos.y > (float)aBottom);
        }

        /// <summary>Picks what the eyes follow: the ball in flight, the aim point, or the AI's target.</summary>
        public void CalcLookPos(Board theBoard, bool force)
        {
            LogicMgr aLogic = theBoard.mLogicMgr;
            if (aLogic.mState == 10)
            {
                return;
            }
            SexyVector2 aLook = new SexyVector2((float)SexyUtil.MouseToBoardX(), (float)SexyUtil.MouseToBoardY());
            bool checkCrossEye = false;
            bool haveAIEye = false;
            if (theBoard.mBalls.Count != 0 && aLogic.mState == LogicMgr.STATE_SHOOTING)
            {
                Ball aBall = theBoard.mBalls[0];
                aLook = new SexyVector2(aBall.mX, aBall.mY);
            }
            else if (aLogic.mState == 2 || aLogic.mState == 3 || aLogic.mState == 4)
            {
                aLook = aLogic.mEyeTarget;
            }
            else
            {
                if (aLogic.mState == LogicMgr.STATE_AIMING && theBoard.mGun.mBall != null)
                {
                    Gun aGun = theBoard.mGun;
                    SexyVector2 aCenter = new SexyVector2(aGun.mCenterX, aGun.mCenterY);
                    aLook = (new SexyVector2(aGun.mBallX, aGun.mBallY) - aCenter) * 4.0f;
                    aLook += aCenter;
                    goto done;
                }
                checkCrossEye = true;
                if (aLogic.mMuteLoops && !force)
                {
                    return;
                }
                if (aLogic.mIsComputer[aLogic.mCurPlayer])
                {
                    if (theBoard.mAIMgr.GetEyePos(ref aLook))
                    {
                        haveAIEye = true;
                    }
                    else if (!force)
                    {
                        return;
                    }
                }
            }
        done:
            mCrossEyeFix = false;
            if (checkCrossEye)
            {
                if (theBoard.mGameMode == GameMode.GAMEMODE_DEMO && !haveAIEye)
                {
                    mCrossEyeFix = true;
                }
                else
                {
                    CheckCrossEyeFix();
                }
            }
            mLookPos.x = aLook.x + (float)theBoard.mBoardRect.mX;
            mLookPos.y = aLook.y + (float)theBoard.mBoardRect.mY;
        }

        /// <summary>Whether the current player's power-up is live, and its remaining count for the banner.</summary>
        public void CalcPowerupActive(Board theBoard)
        {
            LogicMgr aLogic = theBoard.mLogicMgr;
            int aPlayer = aLogic.mCurPlayer;
            int aType = aLogic.mPowerupTypes[aPlayer];
            bool active = false;
            int aCount = -1;
            switch (aType)
            {
            case 1:
            case 0xe:
                aCount = aLogic.mPowerupCount[aType][aPlayer];
                if (aLogic.mState != LogicMgr.STATE_AIMING && aCount > 0)
                {
                    aCount--;
                }
                active = aCount > 0;
                break;
            case 2:
            case 4:
            case 6:
                aCount = aLogic.mPowerupCount[aType][aPlayer];
                active = aCount > 0;
                break;
            case 3:
            case 5:
            case 9:
                active = aLogic.mFlag1C;
                break;
            case 7:
            case 0xc:
                if (aLogic.mCornerText.Length != 0)
                {
                    active = true;
                }
                aCount = aLogic.mPowerupCount[aType][aPlayer];
                if (aLogic.mState != LogicMgr.STATE_AIMING && aCount > 0)
                {
                    aCount--;
                }
                break;
            case 0xd:
                {
                    int[] aTypes = { 1, 4, 2 };
                    for (int i = 0; i < 3; i++)
                    {
                        int c = aLogic.mPowerupCount[aTypes[i]][aPlayer];
                        if (c > 0)
                        {
                            if (aTypes[i] == 1 && aLogic.mState != LogicMgr.STATE_AIMING)
                            {
                                c--;
                            }
                            // As in the original: aCount starts at -1, so only a 0 is ever replaced.
                            if (aCount == 0 || c < aCount)
                            {
                                aCount = c;
                            }
                        }
                    }
                    active = aCount > 0;
                }
                break;
            }
            mPowerupActive = active;
            if (aCount != mPowerupCount[aPlayer])
            {
                if (mPowerupCount[aPlayer] != -2)
                {
                    DoPowerupCountBlink();
                }
                mPowerupCount[aPlayer] = aCount;
            }
        }

        public void DoPowerupCountBlink()
        {
            mPowerupCountBlink = 0xef;
        }

        public void DoPowerupBlink(int theCount)
        {
            mPowerupBlink = theCount * 0x28 + -1;
        }

        /// <summary>Draws the launcher backdrop, the launcher and the loaded ball.</summary>
        public void DrawBack(Graphics gBoard, Graphics g, Board theBoard)
        {
            if (theBoard == null)
            {
                return;
            }
            CalcPowerupActive(theBoard);
            PlayerInfo aPlayer = theBoard.mApp.mPlayerInfo;
            SexyUtil.ClampRange(theBoard.mInterfaceMgr.mStageNum - aPlayer.mFirstAdventureStage, 0, 10);
            SexyUtil.ClampRange(mCurCharacter, 0, 10);
            Image aBackImage = SexyUtil.GetImageById(theBoard.mInterfaceMgr.mShooterBackImageId);
#if PEGGLE_IPHONE
            if (aBackImage != null)
            {
                g.DrawImage(aBackImage, (BOARD_W - aBackImage.mWidth) / 2, 0);
            }
#else
            g.DrawImage(aBackImage, (BOARD_W - aBackImage.mWidth) / 2, CHAR_MGR_SHOOTER_BACK_IMG_Y);
#endif
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            g.SetColorizeImages(true);
            Gun aGun = theBoard.mGun;
            int aCel = 0;
            if (theBoard.mEndLevelDialog.mWidgetManager == null || !theBoard.mEndLevelDialog.mVisible)
            {
                LogicMgr aLogic = theBoard.mLogicMgr;
                if (aLogic.mFlagD9 || aLogic.mFlagD8)
                {
                    aCel = 1;
                }
                else if (aLogic.mState == LogicMgr.STATE_SHOOTING && aLogic.mStateCount < 4 &&
                    (!aLogic.mMuteLoops || theBoard.mReplayPauseCount < 10))
                {
                    aCel = 2;
                }
                float aGunX = theBoard.mGun.mCenterX + 0.0f;
                float aGunY = theBoard.mGun.mCenterY + 0.0f;
#if PEGGLE_IPHONE
                // Port: also when it hasn't been made yet. InterfaceMgr.SyncStageNum only makes it when the stage
                // changes, so a replay started on the interface's current stage (e.g. first thing after launch)
                // had none and crashed here.
                if (mAppQuality != 0 || mShooterImage == null) // SexyAppBase+0x188 is always 0 here
#else
                if (mAppQuality != 0 && !theBoard.mPeggleZoom) // SexyAppBase+0x188 is always 0 here
#endif
                {
                    int aChar = GetCurCharacter();
                    if (aChar < 0 || aChar > 10)
                    {
                        CreateShooterImage(SexyUtil.GetColorForStage(theBoard.mInterfaceMgr.mStageNum - aPlayer.mFirstAdventureStage, ThunderballApp.mGameLevelPack, false));
                    }
                    else
                    {
                        CreateShooterImage(SexyUtil.GetColorForCharacter(aChar, ThunderballApp.mGameLevelPack, false));
                    }
                }
#if PEGGLE_IPHONE
                Image aShooter = mShooterImage;
#else
                Image aShooter = Res.IMAGE_IP_SHOOTER;
#endif
                int aCelHeight = aShooter.mHeight / 3;
                TRect aSrcRect = new TRect(0, aCelHeight * aCel, aShooter.mWidth, aCelHeight - 1);
                Transform aTransform = new Transform();
                aTransform.Translate(0.0f, (float)(CHAR_MGR_SHOOTER_TRANS_OFFEST_Y + 1));
                aTransform.RotateRad(theBoard.mGun.mAngle + 1.5707964f);
                float aH = SexyUtil.gPhysicalDisplayHeight;
                aTransform.Translate((float)((double)((aGunX + theBoard.mBoardRect.mX) * aH) / 600.0),
                    (float)((double)((aGunY + theBoard.mBoardRect.mY) * aH) / 600.0));
                g.DrawImageTransform(aShooter, aTransform, aSrcRect, 0.0f, 0.0f);
            }
            if (aGun.mBall != null && aCel != 1)
            {
                int aGuideCount = theBoard.mLogicMgr.mGuideCount;
                if (aGuideCount == 0)
                {
                    aGun.mBall.Draw(gBoard);
                }
                else if (aGuideCount > 0x19)
                {
                    float aRadius = aGun.mBall != null ? aGun.mBall.mRadius : Ball.mDefRadius;
                    float aDist = aRadius + aGun.mBallOffset + aGun.mOffset;
                    float x = aGun.mCenterX + aDist + (float)((aGuideCount + -0x19) * 2) - 10.0f;
                    float y = aGun.mCenterY;
                    SexyUtil.RotateXY(ref x, ref y, aGun.mCenterX, aGun.mCenterY, aGun.mAngle);
                    float aH = SexyUtil.gPhysicalDisplayHeight;
                    float dx = (x - aGun.mBallX) * aH / 600.0f;
                    float dy = (y - aGun.mBallY) * aH / 600.0f;
                    gBoard.Translate((int)dx, (int)dy);
                    aGun.mBall.Draw(gBoard);
                    gBoard.Translate((int)(-0.0f - dx), (int)(-0.0f - dy));
                }
            }
        }

        /// <summary>TransformClipRect: under a 3D transform the clip rect is meaningless, so it is opened up.</summary>
        private static void TransformClipRect(Graphics g)
        {
            if (g.Is3D() && !Graphics.IsTransformStackEmpty())
            {
                g.mClipRect = new TRect(-100000, -100000, 200000, 200000);
            }
        }

        /// <summary>Draws the current master: body, eyes (or yin-yang eyes) and the cut-out over the eyes.</summary>
        public void Draw(Graphics g, Board theBoard)
        {
            mDrawn = false;
            if (theBoard != null &&
                (theBoard.mShowingGameTip ||
                 (!theBoard.mTrophyCharacter && theBoard.mLogicMgr.mState == 8 &&
                  ((theBoard.mGameMode != GameMode.GAMEMODE_ADVENTURE && theBoard.mGameMode != GameMode.GAMEMODE_ADVENTURE_NIGHTS) ||
                   (ThunderballApp.mBonusAdventure == null ? theBoard.mApp.mCurStage == theBoard.mApp.mPlayerInfo.mLastAdventureStage
                       : ThunderballApp.BonusCharacterSelect(theBoard.mApp.mPlayerInfo)) ||
                   theBoard.mLevelPlayCount < 2) &&
                  theBoard.mGameMode != GameMode.GAMEMODE_DEMO)))
            {
                return;
            }
            mDrawn = true;
            CharacterInfo anInfo = mCharacters[mCurCharacter];
            CharacterImage anImage = anInfo.GetCurImage();
            if (anImage == null || anImage.mSrcImage == null || anImage.mImage == null)
            {
                return;
            }
            TRect aFrameRect = anImage.mFrameRect;
            TRect aCutRect = anImage.mCutRect;
            float aH = SexyUtil.gPhysicalDisplayHeight;
            float aX = (float)((double)(anInfo.mPosX * aH) / 600.0 - aFrameRect.mWidth / 2 + anImage.mCharOffX + CHAR_MGR_CHAR_IMG_OFFSET_X);
            float aY = (float)((double)(anInfo.mPosY * aH) / 600.0 - aFrameRect.mHeight / 2 + anImage.mCharOffY + CHAR_MGR_CHAR_IMG_OFFSET_Y + 0.0);
            float anEye1X = mCurEyePos1.x + (float)CHAR_MGR_CHAR_IMG_OFFSET_X;
            float anEye1Y = mCurEyePos1.y + (float)CHAR_MGR_EYE_OFFSET_Y + 0.0f;
            float anEye2X = mCurEyePos2.x + (float)CHAR_MGR_CHAR_IMG_OFFSET_X;
            float anEye2Y = mCurEyePos2.y + (float)CHAR_MGR_EYE_OFFSET_Y + 0.0f;
            Graphics gClip = g.Create();
            if (mMoveCount == 0)
            {
                gClip.ClipRect(CHAR_MGR_CLIP_RECT_X, CHAR_MGR_CLIP_RECT_Y, CHAR_MGR_CLIP_RECT_W, CHAR_MGR_CLIP_RECT_H);
            }
            else
            {
                float t = (float)mMoveCount / (float)mMoveTime;
                float aNewX = t * (mMoveTo.x - (float)(aFrameRect.mWidth / 2)) + (1.0f - t) * aX;
                float aNewY = t * (mMoveTo.y - (float)(aFrameRect.mHeight / 2)) + (1.0f - t) * aY;
                float dx = aNewX - aX;
                float dy = aNewY - aY;
                anEye1X += dx;
                anEye2X += dx;
                anEye1Y += dy;
                anEye2Y += dy;
                aY = aNewY;
                aX = aNewX;
            }
            TransformClipRect(gClip);
            gClip.DrawImage(anImage.mSrcImage, (int)aX, (int)(aY + 0.0f), aFrameRect);
            aX += (float)anImage.mDrawOffset.mX;
            aY += (float)anImage.mDrawOffset.mY;
            gClip.ClipRect((int)aX, (int)aY, aCutRect.mWidth, aCutRect.mHeight);
            TransformClipRect(gClip);
            if (!mYinYangEye || mYinYangAlpha < 1.0f)
            {
                if (anImage.mEye1.x >= 0.0f)
                {
                    Image anEye = anInfo.mEyeImage;
                    g.DrawImageF(anEye, anEye1X - (float)anEye.mWidth / 2.0f, anEye1Y - (float)anEye.mHeight / 2.0f);
                }
                if (anImage.mEye2.x >= 0.0f)
                {
                    Image anEye = anInfo.mEyeImage2;
                    g.DrawImageF(anEye, anEye2X - (float)anEye.mWidth / 2.0f, anEye2Y - (float)anEye.mHeight / 2.0f);
                }
            }
            if (mYinYangEye || mYinYangAlpha > 0.0f)
            {
                if (mYinYangAlpha < 1.0f)
                {
                    gClip.SetColorizeImages(true);
                    gClip.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mYinYangAlpha * 255.0f)));
                }
                float aRot = 0.0f;
                if (theBoard != null)
                {
                    aRot = (float)theBoard.mGameUpdateCnt * 0.05f;
                }
                Image aYinYang = Res.IMAGE_CHR_YINYANGEYE;
                if (anImage.mEye1.x >= 0.0f)
                {
                    gClip.DrawImageRotatedF(aYinYang, anEye1X - (float)aYinYang.mWidth / 2.0f, anEye1Y - (float)aYinYang.mHeight / 2.0f, (double)(-aRot));
                }
                if (anImage.mEye2.x >= 0.0f)
                {
                    gClip.DrawImageRotatedF(aYinYang, anEye2X - (float)aYinYang.mWidth / 2.0f, anEye2Y - (float)aYinYang.mHeight / 2.0f, (double)(-aRot));
                }
                gClip.SetColorizeImages(false);
            }
            gClip.DrawImage(anImage.mImage, (int)aX, (int)aY, aCutRect);
        }

        /// <summary>Draws the power-up banner and its remaining-count number.</summary>
        public void DrawFront(Graphics gBoard, Graphics g, Board theBoard)
        {
            if (!mDrawn || mMoveCount != 0)
            {
                return;
            }
            LogicMgr aLogic = theBoard.mLogicMgr;
            int aPlayer = aLogic.mCurPlayer;
            int aType = aLogic.mPowerupTypes[aPlayer];
            bool blinkOff = false;
            if (mPowerupBlink != 0)
            {
                blinkOff = (mPowerupBlink / 0x14) % 2 == 0;
            }
            if (aType == 0xc || aType == 7)
            {
                blinkOff = (mUpdateCnt / 0x1e) % 2 == 0;
            }
            bool countBlinkOff = false;
            if (mPowerupCountBlink != 0)
            {
                countBlinkOff = (mPowerupCountBlink / 0x1e) % 2 == 0;
            }
#if !PEGGLE_IPHONE
            // (The iPhone has no power-up banner; only the count is drawn.)
            int aChar = SexyUtil.ClampRange(mCurCharacter, 0, 10);
            Image aBanners = Res.IMAGE_IP_BANNERS;
            int aCelWidth = aBanners.mWidth / aBanners.mNumCols;
            int aCelHeight = aBanners.mHeight / aBanners.mNumRows;
            if (!blinkOff && mPowerupActive)
            {
                g.DrawImage(aBanners, 0x1b6, 1, new TRect(aCelWidth, aCelHeight * aChar, aCelWidth, aCelHeight));
            }
            else
            {
                g.DrawImage(aBanners, 0x1b6, 1, new TRect(0, aCelHeight * aChar, aCelWidth, aCelHeight));
            }
#endif
            int aCount = mPowerupCount[aPlayer];
            if (aCount > 0)
            {
                if (aCount > 99)
                {
                    aCount = 99;
                }
                if (!countBlinkOff && aCount < 99)
                {
                    g.SetFont(Res.FONT_OVERLOAD10OUTLINE);
                    g.SetColor(SexyUtil.MakeColor(0xffffff));
                    string aText = CFormat.StrFormat("%d", aCount);
#if PEGGLE_IPHONE
                    int aH0 = SexyUtil.gPhysicalDisplayHeight;
                    g.DrawString(aText, (int)((double)(aH0 * 400) / 600.0 - g.GetFont().StringWidth(aText) / 2), (int)((double)(aH0 << 5) / 600.0));
                    return;
#endif
                    g.SetScale(0.8f, 0.8f, (float)(g.GetFont().StringWidth(aText) / 2), (float)(g.GetFont().GetCenteredHeight() / 2));
                    int aH = SexyUtil.gPhysicalDisplayHeight;
                    g.DrawString(aText, (int)((double)(aH * 499) / 600.0 - g.GetFont().StringWidth(aText) / 2), (int)((double)(aH * 0x2f) / 600.0));
                    g.SetScale(1.0f, 1.0f, (float)(g.GetFont().StringWidth(aText) / 2), (float)(g.GetFont().GetCenteredHeight() / 2));
                }
            }
        }

        public void Update()
        {
            if (mTweenCounter != 0)
            {
                mTweenCounter--;
            }
            if (mPowerupCountBlink != 0)
            {
                mPowerupCountBlink--;
            }
            if (mPowerupBlink != 0)
            {
                mPowerupBlink--;
            }
            mUpdateCnt++;
            if (!mYinYangEye)
            {
                if (mYinYangAlpha > 0.0f)
                {
                    mYinYangAlpha = SexyUtil.ClampRange(mYinYangAlpha - 0.1f, 0.0f, 1.0f);
                }
            }
            else if (mYinYangAlpha < 1.0f)
            {
                mYinYangAlpha = SexyUtil.ClampRange(mYinYangAlpha + 0.1f, 0.0f, 1.0f);
            }
            if (mMoveCount == 0)
            {
                CalcEyePos(false);
            }
            else
            {
                mMoveCount--;
                if (mMoveTime + -5 < mMoveCount)
                {
                    CharacterInfo anInfo = mCharacters[mCurCharacter];
                    anInfo.mCurFrame = anInfo.mImages.Count - 1;
                    mTweenCounter = 1;
                    CalcEyePos(true);
                    mTweenCounter = 0;
                }
                else
                {
                    CalcEyePos(false);
                }
            }
        }

        /// <summary>While paused, turns the character to face front.</summary>
        public void UpdatePaused()
        {
            CharacterInfo anInfo = mCharacters[mCurCharacter];
            if (anInfo.mCurFrame != 0 && anInfo.mCurFrame != anInfo.mImages.Count - 1)
            {
                anInfo.mCurFrame = anInfo.mFlag60 ? 0 : anInfo.mImages.Count - 1;
                mTweenCounter = 1;
                CalcEyePos(true);
                mTweenCounter = 0;
                SexyUtil.GetBoard()?.MarkDirty();
            }
        }

        public void UpdateSlowMo()
        {
            if (mTweenCounter != 0)
            {
                mTweenCounter--;
                if (mTweenCounter == 0)
                {
                    CalcEyePos(false);
                }
            }
        }

        public bool CharMoving()
        {
            return mMoveCount != 0;
        }

        public SexyVector2 GetLastLookPos()
        {
            return mLookPos;
        }
    }
}
