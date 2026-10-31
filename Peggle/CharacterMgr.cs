using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>One animation frame of a Peggle master's portrait (Sexy::CharacterImage).</summary>
    public class CharacterImage
    {
        public Image mSrcImage;                                   // 0x00

        public Image mImage;                                      // 0x04 (frames cut out by alpha mask)

        public ImageLibImage mMask2Image;                         // 0x08

        public TRect mFrameRect = default(TRect);                 // 0x0c

        public TRect mCutRect = default(TRect);                   // 0x1c

        public TRect mMask2Rect = default(TRect);                 // 0x2c

        public TRect mBorderRect = default(TRect);                // 0x3c

        public TPoint mDrawOffset;                                // 0x4c

        public TPoint mMask2Pos;                                  // 0x54

        public SexyVector2 mEye1 = new SexyVector2();             // 0x5c

        public SexyVector2 mEye2 = new SexyVector2();             // 0x64

        public int mCharOffX;                                     // 0x6c

        public int mCharOffY;                                     // 0x70
    }

    /// <summary>A Peggle master from characters/1024x768/characters.cfg (Sexy::CharacterInfo).</summary>
    public class CharacterInfo
    {
        public List<CharacterImage> mImages = new List<CharacterImage>(); // 0x00

        public List<string> mTips = new List<string>();          // 0x0c

        public int mCurFrame;                                     // 0x18

        public Image mEyeImage;                                   // 0x1c

        public Image mEyeImage2;                                  // 0x20

        public string mName;                                      // 0x24

        public string mDesc = "";                                 // 0x28

        public string mTrophyWin = "";                            // 0x2c

        public string mTrophyWinAdventure = "";                   // 0x30

        public string mTrophyWinAce = "";                         // 0x34

        public string mTrophyWinAllClear = "";                    // 0x38

        public string mTrophyWinChallenge = "";                   // 0x3c

        public List<string> mTrophyIdle = new List<string>();    // 0x40

        public float mPosX;                                       // 0x4c

        public float mPosY;                                       // 0x50

        public int mPowerup;                                      // 0x54

        public int mTweenSpeed;                                   // 0x58

        public int mTurnLine;                                     // 0x5c

        public bool mFlag60;                                      // 0x60

        public CharacterInfo()
        {
            mName = SexyAppBase.Loc("Name");
            mPosX = 0;
            mPosY = 0;
            mEyeImage = null;
            mEyeImage2 = null;
            mCurFrame = 0;
            mFlag60 = true;
            mPowerup = 1;
            mTweenSpeed = 6;
            mTurnLine = 0x32;
        }

        public CharacterImage GetCurImage()
        {
            if (mCurFrame >= 0 && mCurFrame < mImages.Count)
            {
                return mImages[mCurFrame];
            }
            return null;
        }
    }

    /// <summary>
    /// The Peggle masters (Sexy::CharacterMgr): their config, the current character and its
    /// animated portrait with eyes that follow the ball. The in-game drawing/eye-tracking half lives
    /// in CharacterMgr.Board.cs.
    /// </summary>
    public partial class CharacterMgr
    {
        public ImageMgr mImageMgr;                                // 0x04

        public MemoryImage mShooterImage;                         // 0x08

        public List<CharacterInfo> mCharacters = new List<CharacterInfo>(); // 0x0c

        public int mCurCharacter;                                 // 0x18

        public int mFrameCountdown;                               // 0x1c

        public int mNumFrames;                                    // 0x20

        public int mTweenCounter;                                 // 0x24

        public int mPowerupCountBlink;                            // 0x28

        public int mPowerupBlink;                                 // 0x2c

        public int[] mPowerupCount = new int[2];                  // 0x30 (per player; -2 = unknown)

        public int mUpdateCnt;                                    // 0x38

        public bool mPowerupActive;                               // 0x3c

        public bool mYinYangEye;                                  // 0x3d

        public float mYinYangAlpha;                               // 0x40

        public int mAppQuality;                                   // 0x44

        public SexyVector2 mEyePos1 = new SexyVector2();          // 0x48

        public SexyVector2 mEyePos2 = new SexyVector2();          // 0x50

        public SexyVector2 mCurEyePos1 = new SexyVector2();       // 0x58

        public SexyVector2 mCurEyePos2 = new SexyVector2();       // 0x60

        public SexyVector2 mLookPos = new SexyVector2();          // 0x68

        public bool mCrossEyeFix;                                 // 0x70

        public int mBaseX;                                        // 0x74

        public int mBaseY;                                        // 0x78

        public SexyVector2 mMoveTo = new SexyVector2();           // 0x7c

        public int mMoveTime;                                     // 0x88

        public int mMoveCount;                                    // 0x8c

        public bool mDrawn;                                       // 0x90

        public CharacterMgr()
        {
            mImageMgr = new ImageMgr();
            mShooterImage = null;
            mAppQuality = 0; // SexyAppBase+0x188
            mCurCharacter = 0;
            mBaseX = 400;
            mBaseY = 0x46;
            mTweenCounter = 0;
            mMoveCount = 0;
            mPowerupBlink = 0;
            mPowerupCountBlink = 0;
            mUpdateCnt = 0;
            mCrossEyeFix = true;
            mYinYangEye = false;
            mYinYangAlpha = 0;
        }

        public void SyncState(DataSync theSync)
        {
            theSync.SyncLong(ref mCurCharacter);
            if (theSync.GetVersion() < 0x27)
            {
                mPowerupBlink = 0;
                mPowerupCountBlink = 0;
            }
            else
            {
                theSync.SyncLong(ref mPowerupBlink);
                theSync.SyncLong(ref mPowerupCountBlink);
            }
            if (theSync.GetVersion() > 0x2f)
            {
                theSync.SyncBoolBit(ref mYinYangEye);
                theSync.EndBit();
            }
            if (theSync.GetVersion() > 0x55)
            {
                theSync.SyncLong(ref mAppQuality);
            }
            if (theSync.IsReading)
            {
                mYinYangAlpha = mYinYangEye ? 1 : 0;
                mPowerupCount[1] = -2;
                mPowerupCount[0] = -2;
                mUpdateCnt = 0;
                SetCurCharacter(mCurCharacter);
            }
        }

        public void SetCurCharacter(int theCharacter)
        {
            if (mCharacters.Count == 0)
            {
                return;
            }
            mUpdateCnt = 0;
            int prev = mCurCharacter;
            mCurCharacter = SexyUtil.AceMod(theCharacter, mCharacters.Count);
            if (mCurCharacter != prev)
            {
                CalcEyePos(true);
            }
        }

        public void Clear(bool clearCharacters)
        {
            mPowerupBlink = 0;
            mPowerupCountBlink = 0;
            mPowerupCount[1] = -2;
            mPowerupCount[0] = -2;
            mYinYangEye = false;
            mYinYangAlpha = 0;
            if (clearCharacters)
            {
                mImageMgr.Clear();
                mCharacters.Clear();
            }
        }

        public void InitDefault()
        {
            Clear(true);
            CharacterInfo info = new CharacterInfo();
            info.mPosX = 320.0f;
            info.mPosY = 40.0f;
            mCharacters.Add(info);
        }

        public void SetYinYangEye(bool on)
        {
            if (mYinYangEye != on)
            {
                mYinYangEye = on;
            }
        }

        public CharacterImage GetCurCharFrame(CharacterInfo theInfo)
        {
            if (theInfo.mImages.Count == 0)
            {
                return null;
            }
            if (mFrameCountdown < 1)
            {
                return theInfo.mImages[theInfo.mImages.Count - 1];
            }
            int idx = theInfo.mImages.Count - mFrameCountdown;
            if (idx >= 0 && idx < theInfo.mImages.Count)
            {
                return theInfo.mImages[idx];
            }
            return null;
        }

        public int ReadPowerupType(ConfigParser theParser, string theName)
        {
            for (int i = 0; i < 15; i++)
            {
                if (string.Equals(theName, SexyUtil.powerup_names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            theParser.Error("Unknown powerup type", false, true);
            return 0;
        }

        private static string CharacterPath(string theName)
        {
            return "characters/" + GlobalStaticVars.mResPathPrefix + theName;
        }

        public void ReaderCharacterConfig(ConfigParser theParser, CharacterInfo theInfo)
        {
            StringParser val = theParser.GetValParser();
            if (theParser.KeyIs("image"))
            {
                string name = val.ReadString();
                int frames = 1;
                if (val.CheckNextChar(','))
                {
                    frames = val.ReadInt();
                    if (frames < 1)
                    {
                        theParser.Error("Invalid number of frames", false, true);
                        frames = 1;
                    }
                }
                LoadCharacterImage(theParser, theInfo, CharacterPath(name), frames);
            }
            else if (theParser.KeyIs("eyeimage"))
            {
                string path1 = CharacterPath(val.ReadString());
                string path2 = path1;
                if (val.CheckNextChar(','))
                {
                    path2 = CharacterPath(val.ReadString());
                }
                theInfo.mEyeImage = mImageMgr.GetImage(path1, out _, true);
                theInfo.mEyeImage2 = mImageMgr.GetImage(path2, out _, true);
                if (theInfo.mEyeImage == null)
                {
                    theParser.Error("Image not found: " + path1, false, true);
                }
                else if (theInfo.mEyeImage2 == null)
                {
                    theParser.Error("Image not found: " + path2, false, true);
                }
            }
            else if (theParser.KeyIs("eyes"))
            {
                if (mFrameCountdown > 0)
                {
                    mFrameCountdown--;
                }
                CharacterImage frame = GetCurCharFrame(theInfo);
                if (frame == null)
                {
                    theParser.Error("You need to specify an image first.", false, true);
                    return;
                }
                frame.mEye1.x = (float)(val.ReadInt() * 0.63);
                val.EnsureNextChar(',');
                frame.mEye1.y = (float)(val.ReadInt() * 0.63);
                val.EnsureNextChar(',');
                frame.mEye2.x = (float)(val.ReadInt() * 0.63);
                val.EnsureNextChar(',');
                frame.mEye2.y = (float)(val.ReadInt() * 0.63);
                if (mFrameCountdown > 0)
                {
                    int offset = frame.mFrameRect.mHeight * (mNumFrames - mFrameCountdown);
                    frame.mEye1.y -= offset;
                    frame.mEye2.y -= offset;
                }
            }
            else if (theParser.KeyIs("pos"))
            {
                theInfo.mPosX = val.ReadInt() + mBaseX;
                val.EnsureNextChar(',');
                theInfo.mPosY = val.ReadInt() + mBaseY;
            }
            else if (theParser.KeyIs("charoff"))
            {
                CharacterImage frame = GetCurCharFrame(theInfo);
                if (frame == null)
                {
                    theParser.Error("You need to specify an image first.", false, true);
                    return;
                }
                frame.mCharOffX = val.ReadInt();
                val.EnsureNextChar(',');
                frame.mCharOffY = val.ReadInt();
            }
            else if (theParser.KeyIs("deleteframe"))
            {
                int frame = val.ReadInt();
                if (frame >= 0 && frame < theInfo.mImages.Count)
                {
                    theInfo.mImages.RemoveAt(frame);
                }
            }
            else if (theParser.KeyIs("Powerup"))
            {
                theInfo.mPowerup = ReadPowerupType(theParser, val.ReadString());
            }
            else if (theParser.KeyIs("tweenspeed"))
            {
                theInfo.mTweenSpeed = val.ReadInt();
            }
            else if (theParser.KeyIs("turnline"))
            {
                theInfo.mTurnLine = val.ReadInt();
            }
            else if (theParser.KeyIs("desc"))
            {
                theInfo.mDesc = val.ReadString();
            }
            else if (theParser.KeyIs("TrophyWin"))
            {
                theInfo.mTrophyWin = val.ReadString();
            }
            else if (theParser.KeyIs("TrophyIdle"))
            {
                theInfo.mTrophyIdle.Add(val.ReadString());
            }
            else if (theParser.KeyIs("TrophyWinAdventure"))
            {
                theInfo.mTrophyWinAdventure = val.ReadString();
            }
            else if (theParser.KeyIs("TrophyWinAce"))
            {
                theInfo.mTrophyWinAce = val.ReadString();
            }
            else if (theParser.KeyIs("TrophyWinAllClear"))
            {
                theInfo.mTrophyWinAllClear = val.ReadString();
            }
            else if (theParser.KeyIs("TrophyWinChallenge"))
            {
                theInfo.mTrophyWinChallenge = val.ReadString();
            }
            else if (theParser.KeyIs("tip"))
            {
                theInfo.mTips.Add(val.ReadString());
            }
            else
            {
                theParser.ErrorUnexpectedKey();
            }
        }

        public void ReadConfig(string theFileName)
        {
            Clear(true);
            CharacterInfo defaults = new CharacterInfo();
            ConfigParser parser = new ConfigParser(true);
            parser.Open(theFileName, true);
            bool inCharacter = false;
            while (parser.ReadNext())
            {
                ConfigParser.ItemType type = parser.GetType();
                if (type == ConfigParser.ItemType.GroupStart)
                {
                    if (parser.GroupTypeIs("Character", 1))
                    {
                        mFrameCountdown = -1;
                        inCharacter = true;
                        if (string.IsNullOrEmpty(parser.GetGroupName()))
                        {
                            parser.Error("Need name for character", false, true);
                        }
                        CharacterInfo info = new CharacterInfo();
                        CopyCharacterInfo(defaults, info);
                        mCharacters.Add(info);
                        info.mName = parser.GetGroupName();
                        info.mPosX += mBaseX;
                        info.mPosY += mBaseY;
                        info.mEyeImage2 = null;
                        info.mEyeImage = null;
                    }
                    else
                    {
                        parser.ErrorUnexpectedGroupType();
                    }
                }
                else if (type == ConfigParser.ItemType.KeyValue)
                {
                    ReaderCharacterConfig(parser, inCharacter ? mCharacters[mCharacters.Count - 1] : defaults);
                }
                else
                {
                    inCharacter = false;
                }
            }
            if (mCurCharacter >= mCharacters.Count)
            {
                mCurCharacter = mCharacters.Count - 1;
            }
            Resync();
        }

        /// <summary>The C++ code pushes a copy of the defaults block; copy the plain fields.</summary>
        private static void CopyCharacterInfo(CharacterInfo theFrom, CharacterInfo theTo)
        {
            theTo.mImages = new List<CharacterImage>(theFrom.mImages);
            theTo.mTips = new List<string>(theFrom.mTips);
            theTo.mCurFrame = theFrom.mCurFrame;
            theTo.mEyeImage = theFrom.mEyeImage;
            theTo.mEyeImage2 = theFrom.mEyeImage2;
            theTo.mName = theFrom.mName;
            theTo.mDesc = theFrom.mDesc;
            theTo.mTrophyWin = theFrom.mTrophyWin;
            theTo.mTrophyWinAdventure = theFrom.mTrophyWinAdventure;
            theTo.mTrophyWinAce = theFrom.mTrophyWinAce;
            theTo.mTrophyWinAllClear = theFrom.mTrophyWinAllClear;
            theTo.mTrophyWinChallenge = theFrom.mTrophyWinChallenge;
            theTo.mTrophyIdle = new List<string>(theFrom.mTrophyIdle);
            theTo.mPosX = theFrom.mPosX;
            theTo.mPosY = theFrom.mPosY;
            theTo.mPowerup = theFrom.mPowerup;
            theTo.mTweenSpeed = theFrom.mTweenSpeed;
            theTo.mTurnLine = theFrom.mTurnLine;
            theTo.mFlag60 = theFrom.mFlag60;
        }

        public void Resync()
        {
            CalcEyePos(true);
        }

        public CharacterInfo GetCharacterInfo(int theCharacter)
        {
            if (theCharacter >= 0 && theCharacter < mCharacters.Count)
            {
                return mCharacters[SexyUtil.AceMod(theCharacter, mCharacters.Count)];
            }
            return null;
        }

        public CharacterInfo GetCharacterInfoSafe(int theCharacter)
        {
            return mCharacters[SexyUtil.AceMod(theCharacter, mCharacters.Count)];
        }

        public string GetCharacterName(int theCharacter)
        {
            return GetCharacterInfo(theCharacter)?.mName ?? "";
        }

        public string GetCharacterDesc(int theCharacter)
        {
            return GetCharacterInfo(theCharacter)?.mDesc ?? "";
        }

        public string GetCharacterTip(int theCharacter, uint theIndex)
        {
            CharacterInfo info = GetCharacterInfo(theCharacter);
            if (info != null && info.mTips.Count > 0)
            {
                return info.mTips[(int)(theIndex % (uint)info.mTips.Count)];
            }
            return "";
        }

        public int GetCharacterIdByName(string theName)
        {
            for (int i = 0; i < mCharacters.Count; i++)
            {
                if (string.Equals(mCharacters[i].mName, theName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Load()
        {
            ReadConfig("characters/" + GlobalStaticVars.mResPathPrefix + "characters.cfg");
        }

        public int GetCurCharacter()
        {
            return mCurCharacter;
        }
    }
}
