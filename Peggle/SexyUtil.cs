using System;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>Peggle's ButtonFontColor enum: index of a 3-layer colour set for FONT_BUTTON.</summary>
    public static class ButtonFontColor
    {
        public const int NONE = 0x11;
    }

    /// <summary>
    /// Free functions from the Sexy namespace in Peggle (ThunderCommon.cpp, SexyCommon, Res helpers).
    /// Named after the original; the class is split across several files.
    /// </summary>
    public static partial class SexyUtil
    {
        private static char CommaChar = ',';

        public static MTRand gCommonMTRand = new MTRand();

        /// <summary>The physical display height; many coordinates are authored for 600 high.</summary>
        public static int gPhysicalDisplayHeight = 768;

        /// <summary>
        /// iPhone: the blank strip on each side when the 960-wide layout is centred on a wider screen
        /// (88 on the iPhone 5's 1136). Always 0 in HD.
        /// </summary>
        public static int gGutterWidth;

        /// <summary>SexyAppBase::mLayoutScaleFactor: 1 for the iPad art, 2 for the iPhone's 960x640 art.</summary>
        public static int mLayoutScaleFactor = 1;

        /// <summary>iPhone: mResoultionType, the index into PATH_RESOLUTION_PREFIX (0 480x320, 1 1024x768, 2 960x640).</summary>
        public static int mResoultionType;

        /// <summary>File-static const in the binary (Sexy::gCharCount).</summary>
        public static readonly int gCharCount = 11;

        private static readonly int[] aPeggleCharacterArray = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 11 };

        private static readonly int[] aNightsCharacterArray = { 0, 1, 5, 2, 3, 4, 6, 8, 7, 9, 10, 11 };

        private static readonly int[] aStageArray = { 0, 1, 3, 4, 5, 2, 6, 8, 7, 9, 10, 11 };

        public static readonly string[] powerup_names =
        {
            "None", "Guide", "Flippers", "Multiball", "Pyramid", "SpaceBlast", "SpookyBall", "ZenBall", "ShotExtender",
            "FlowerPower", "Nudge", "TimeBomb", "Fireball", "LuckySpin", "Chainlightning"
        };

        public static readonly string[] styleshot_names =
        {
            "LongShot", "FlipperManiac", "ExtremeSlide", "OrangeAttack", "CoolClear", "MultiballMadness", "HatTrick",
            "HotStuff", "FreeballFrenzy", "SkillShot", "DoublePowerup", "BucketFever", "LuckyBounce", "MadSkills",
            "CrazyMadSkills", "ExtraLongShot", "PyramidShot", "DuelShot", "DuelShotBonus", "MegaShot", "PyramidEyeShot",
            "DoubleLongShot", "WallShot", "ShockItToMe"
        };

        /// <summary>Sexy::Color(int): 0xAARRGGBB, an alpha of 0 meaning opaque.</summary>
        public static Color MakeColor(int theColor)
        {
            int a = (theColor >> 24) & 0xff;
            if (a == 0)
            {
                a = 0xff;
            }
            return new Color((theColor >> 16) & 0xff, (theColor >> 8) & 0xff, theColor & 0xff, a);
        }

        /// <summary>Sexy::Color(int theColor, int theAlpha)</summary>
        public static Color MakeColor(int theColor, int theAlpha)
        {
            return new Color((theColor >> 16) & 0xff, (theColor >> 8) & 0xff, theColor & 0xff, ClampRange(theAlpha, 0, 255));
        }

        /// <summary>MakeColor(double, double, double, double)</summary>
        public static Color MakeColor(double r, double g, double b, double a)
        {
            return new Color((int)(r * 255.0), (int)(g * 255.0), (int)(b * 255.0), (int)(a * 255.0));
        }

        public static int ClampRange(int theValue, int theMin, int theMax)
        {
            if (theValue < theMin)
            {
                return theMin;
            }
            return theValue > theMax ? theMax : theValue;
        }

        /// <summary>Modulo that is never negative.</summary>
        public static int AceMod(int theValue, int theMod)
        {
            if (theValue < 0)
            {
                int m = -theValue % theMod;
                return m == 0 ? 0 : theMod - m;
            }
            return theValue % theMod;
        }

        public static float ClampRange(float theValue, float theMin, float theMax)
        {
            if (theValue < theMin)
            {
                return theMin;
            }
            return theValue > theMax ? theMax : theValue;
        }

        public static void SetCommaSeparator(char theChar)
        {
            CommaChar = theChar;
        }

        public static string CommaSeperate(int theValue)
        {
            if (theValue == 0)
            {
                return "0";
            }
            bool negative = theValue < 0;
            string digits = Math.Abs((long)theValue).ToString();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int lead = digits.Length % 3;
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (i - lead) % 3 == 0)
                {
                    sb.Append(CommaChar);
                }
                sb.Append(digits[i]);
            }
            return negative ? "-" + sb : sb.ToString();
        }

        /// <summary>
        /// The original returns the iOS Documents directory; here save data is stored relative to
        /// SexyAppBase.applicationStoragePath, so the prefix is empty.
        /// </summary>
        public static string GetDocumentsDir()
        {
            return "";
        }

        /// <summary>
        /// Port addition: the port's version, the game assembly's (Directory.Build.props &lt;Version&gt;): major.minor,
        /// plus the build and revision numbers when they're set.
        /// </summary>
        public static string GetPortVersionString()
        {
            Version aVersion = typeof(SexyUtil).Assembly.GetName().Version ?? new Version(1, 0);
            string aText = aVersion.Major + "." + aVersion.Minor;
            if (aVersion.Build > 0 || aVersion.Revision > 0)
            {
                aText += "." + aVersion.Build;
            }
            if (aVersion.Revision > 0)
            {
                aText += "." + aVersion.Revision;
            }
            return aText;
        }

        public static string GetBundleShortVersionString()
        {
#if PEGGLE_IPHONE
            return "1.7.2";
#else
            return "2.0.9";
#endif
        }

        /// <summary>CFBundleVersion ("4840.14.07.16.01" on iPad, "4824.14.07.16.01" on iPhone) read as an integer.</summary>
        public static int GetBundleVersionInteger()
        {
#if PEGGLE_IPHONE
            return 4824;
#else
            return 4840;
#endif
        }

        public static int Rand()
        {
            return (int)gCommonMTRand.Next();
        }

        public static float GetRandFloat()
        {
            int r = Rand() % 1000 - 500;
            return r / 500.0f;
        }

        public static int GetCharacterForStage(int theStage, int theLevelPack)
        {
            if (theStage < 0)
            {
                return 0;
            }
            if (theLevelPack == 0)
            {
                return theStage < 11 ? aPeggleCharacterArray[theStage] : 0;
            }
            if (theLevelPack == 1)
            {
                return aNightsCharacterArray[Math.Min(theStage, 11)];
            }
            return 0;
        }

        /// <summary>
        /// Port addition: the master of an Adventure stage (counted from the Adventure's first), as
        /// GetCharacterForStage, except in a bonus Adventure, whose page lists its own masters.
        /// </summary>
        public static int GetAdventureCharacter(int theStage, int theLevelPack)
        {
            BonusPage aPage = ThunderballApp.mBonusAdventure;
            if (aPage != null && aPage.mStageCharacters != null)
            {
                return aPage.mStageCharacters[ClampRange(theStage, 0, aPage.mStageCharacters.Length - 1)];
            }
            return GetCharacterForStage(theStage, theLevelPack);
        }

        /// <summary>Port addition: whether the Peggle WoW bonus Adventure is being played.</summary>
        public static bool IsWoWAdventure => ThunderballApp.mBonusAdventure?.mAdventureKey == StageMgr.ADVENTURE_WOW;

        /// <summary>Port addition: Bjorn's and Splork's WoW portraits, loaded once.</summary>
        private static Image[] sWoWPortraits;

        /// <summary>
        /// Port addition: in the WoW Adventure, Bjorn's (character 0) or Splork's (3) portrait from the PC WoW
        /// edition (images/pc/wow_portraits), at the size of this edition's Nights portraits; null for anyone
        /// else, outside that Adventure, or when the file is missing.
        /// </summary>
        public static Image GetWoWPortrait(int theCharacter)
        {
            int anIndex = theCharacter == 0 ? 0 : theCharacter == 3 ? 1 : -1;
            if (anIndex < 0 || !IsWoWAdventure)
            {
                return null;
            }
            sWoWPortraits ??= new Image[2];
            if (sWoWPortraits[anIndex] == null)
            {
                ImageLibImage anImage = ImageLib.GetImage("images/pc/wow_portraits/" + (anIndex == 0 ? "bjorn" : "splork"), false);
                if (anImage != null)
                {
                    Image aSize = GetImageById((int)ResourceId.IMAGE_CHR_NIGHTS_CHARACTER01_ID);
                    sWoWPortraits[anIndex] = LoadResizedImage(anImage, aSize.GetWidth(), aSize.GetHeight());
                }
            }
            return sWoWPortraits[anIndex];
        }

        public static int GetStageForCharacter(int theCharacter)
        {
            return aStageArray[ClampRange(theCharacter, 0, 11)];
        }

        public static int GetColorForCharacter(int theCharacter, int theLevelPack, bool unused)
        {
            switch (theCharacter)
            {
            case 0: return 0x65aaf5;
            case 1: return 0x9bd54d;
            case 2: return 0xf3c13e;
            case 3: return 0xcb43ff;
            case 4: return 0x28e5ff;
            case 5: return 0x7c2c45;
            case 6: return 0xfeb0e0;
            case 7: return 0xf55e57;
            case 8: return 0xfed811;
            case 9: return 0x938f8e;
            case 10: return 0xad3efd;
            default: return 0xffffff;
            }
        }

        private static Image sBonusThumbnails;

        /// <summary>Port addition: loads a PC image and resamples it to theWidth x theHeight.</summary>
        private static Image LoadResizedImage(ImageLibImage theImage, int theWidth, int theHeight)
        {
            ImageFileLoader.Premultiply(theImage.mBits);
            if (theImage.mWidth != theWidth || theImage.mHeight != theHeight)
            {
                theImage = ImageLib.Resize(theImage, theWidth, theHeight);
            }
            return theImage.CreateImage();
        }

        /// <summary>
        /// Port addition: the bonus levels (Nights bonus, Extreme) ship the PC's 800x600-scale art
        /// (tools/import_bonus_levels.py). When an image is missing, this looks for a PC one instead:
        /// a background "levels/&lt;res&gt;/name" as levels/ds/name (a Peggle Dual Shot screen,
        /// LoadDSBackground) or else levels/pc/name, and any other "images/.../name"
        /// (level art, emitter particles) as images/pc/name, since the game finds those by file
        /// name. It is scaled as the board is (display height / 600), so it comes out the size of
        /// this edition's own art. Null when there is no such file.
        /// </summary>
        public static Image GetPCImage(string theFileName)
        {
            string aName = ContentFS.Normalize(theFileName);
            string aLevelsPrefix = "levels/" + GlobalStaticVars.mResPathPrefix;
            string aPath;
            if (aName.StartsWith(aLevelsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string aLevel = aName.Substring(aLevelsPrefix.Length);
                Image aDSImage = LoadDSBackground("levels/ds/" + aLevel);
                if (aDSImage != null)
                {
                    return aDSImage;
                }
                aPath = "levels/pc/" + aLevel;
            }
            else if (aName.StartsWith("images/", StringComparison.OrdinalIgnoreCase) && !aName.StartsWith("images/pc/", StringComparison.OrdinalIgnoreCase))
            {
                aPath = "images/pc/" + aName.Substring(aName.LastIndexOf('/') + 1);
            }
            else
            {
                return null;
            }
            return LoadPCImage(aPath);
        }

        /// <summary>
        /// Port addition: board units per Peggle Dual Shot background pixel, and the board point at the
        /// background's top-left: the DS screen's (3, 4.5), less the one column added on its left.
        /// </summary>
        private const float DS_SCALE = 2.53f;

        private const float DS_ORIGIN_X = 3.0f - DS_SCALE;

        private const float DS_ORIGIN_Y = 4.5f;

        /// <summary>
        /// Port addition: a Peggle Dual Shot level background (levels/ds/, from
        /// tools/import_dualshot_levels.py) scaled up nearest-neighbour to this edition's background
        /// size, so it is placed like the edition's own backgrounds. The file is the DS screen (256x192)
        /// expanded to the whole board at the same pixel size: a column on the left and rows below,
        /// painted in by hand. Pixel (i, j) shows board point (0.47 + 2.53 i, 4.5 + 2.53 j); anything the
        /// file doesn't reach is left transparent. Null when there is no such file.
        /// </summary>
        public static Image LoadDSBackground(string thePath)
        {
            ImageLibImage aSource = ImageLib.GetImage(thePath, false);
            if (aSource == null)
            {
                return null;
            }
            int aWidth = BACKGROUND_IMAGE_WIDTH;
            int aHeight = BACKGROUND_IMAGE_HEIGHT;
            // The board area an edition background covers: centred on (0x145, 0x118) at 1.28 pixels
            // per board unit in HD (Board.LoadLevel2).
            float aBoardW = 827 / 1.28f;
            float aBoardH = 696 / 1.28f;
            float aLeft = 0x145 - aBoardW / 2;
            float aTop = 0x118 - aBoardH / 2;
            int[] aCols = new int[aWidth];
            for (int x = 0; x < aWidth; x++)
            {
                aCols[x] = (int)Math.Floor((aLeft + (x + 0.5f) * aBoardW / aWidth - DS_ORIGIN_X) / DS_SCALE);
            }
            ImageLibImage anImage = new ImageLibImage(aWidth, aHeight);
            for (int y = 0; y < aHeight; y++)
            {
                int aRow = (int)Math.Floor((aTop + (y + 0.5f) * aBoardH / aHeight - DS_ORIGIN_Y) / DS_SCALE);
                if (aRow < 0 || aRow >= aSource.mHeight)
                {
                    continue;
                }
                for (int x = 0; x < aWidth; x++)
                {
                    if (aCols[x] >= 0 && aCols[x] < aSource.mWidth)
                    {
                        anImage.mBits[y * aWidth + x] = aSource.mBits[aRow * aSource.mWidth + aCols[x]];
                    }
                }
            }
            return LoadResizedImage(anImage, aWidth, aHeight);
        }

        /// <summary>Port addition: loads an image file resampled by theScale. Null when missing.</summary>
        public static Image LoadScaledImage(string thePath, double theScale)
        {
            ImageLibImage anImage = ImageLib.GetImage(thePath, false);
            if (anImage == null)
            {
                return null;
            }
            return LoadResizedImage(anImage, (int)Math.Round(anImage.mWidth * theScale), (int)Math.Round(anImage.mHeight * theScale));
        }

        /// <summary>Port addition: a cel rect of a PC sheet loaded with LoadPCImage, scaled as the sheet was (display height / 600).</summary>
        public static TRect ScalePCRect(TRect theRect)
        {
            return ScalePCRect(theRect, gPhysicalDisplayHeight / 600.0);
        }

        /// <summary>Port addition: a cel rect of a PC sheet loaded with LoadPCImage(path, theScale).</summary>
        public static TRect ScalePCRect(TRect theRect, double aScale)
        {
            return new TRect((int)Math.Round(theRect.mX * aScale), (int)Math.Round(theRect.mY * aScale),
                (int)Math.Round(theRect.mWidth * aScale), (int)Math.Round(theRect.mHeight * aScale));
        }

        /// <summary>Port addition: loads a PC (800x600-scale) image file, scaled by display height / 600. Null when missing.</summary>
        public static Image LoadPCImage(string thePath)
        {
            return LoadPCImage(thePath, gPhysicalDisplayHeight / 600.0);
        }

        /// <summary>Port addition: loads a PC (800x600-scale) image file resampled by aScale. Null when missing.</summary>
        public static Image LoadPCImage(string thePath, double aScale)
        {
            ImageLibImage anImage = ImageLib.GetImage(thePath, false);
            if (anImage == null)
            {
                return null;
            }
            return LoadResizedImage(anImage, (int)Math.Ceiling(anImage.mWidth * aScale - 0.01), (int)Math.Ceiling(anImage.mHeight * aScale - 0.01));
        }

        /// <summary>
        /// Port addition: every bonus level's thumbnail, five to a row and a row per bonus stage, at
        /// the size of this edition's thumbnail cels (tools/import_bonus_levels.py).
        /// </summary>
        public static Image GetBonusThumbnails()
        {
            if (sBonusThumbnails == null)
            {
                sBonusThumbnails = GlobalStaticVars.gSexyAppBase.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "levelselect/levelthumbs_bonus", true);
                if (sBonusThumbnails != null)
                {
                    TRect aCel = Res.IMAGE_LS_THUMBNAILS_PEGGLE.GetCelRect(0);
                    sBonusThumbnails.mNumCols = 5;
                    sBonusThumbnails.mNumRows = sBonusThumbnails.mHeight / aCel.mHeight;
                }
            }
            return sBonusThumbnails;
        }

        /// <summary>Draws a level's thumbnail cel; stages 0-10 are Peggle, 11+ Nights, each split over two strips (port: 24+ bonus).</summary>
        public static void DrawLevelThumbnail(Graphics g, int theStage, int theLevel, TRect theRect)
        {
            Image anImage;
            int anIndex;
            if (StageMgr.IsBonusStage(theStage))
            {
                Image aSheet = GetBonusThumbnails();
                if (aSheet != null)
                {
                    g.DrawImageCel(aSheet, theRect, theLevel, theStage - StageMgr.BONUS_FIRST_STAGE);
                }
                return;
            }
#if PEGGLE_IPHONE
            // The iPhone art fits each episode's thumbnails on one strip.
            if (theStage < 0xb)
            {
                anImage = Res.IMAGE_LS_THUMBNAILS_PEGGLE;
            }
            else
            {
                theStage -= 0xb;
                anImage = Res.IMAGE_LS_THUMBNAILS_NIGHTS;
            }
            anIndex = theStage * 5 + theLevel;
#else
            if (theStage < 0xb)
            {
                int aCount = Res.IMAGE_LS_THUMBNAILS_PEGGLE.mNumCols * Res.IMAGE_LS_THUMBNAILS_PEGGLE.mNumRows;
                anIndex = theStage * 5 + theLevel;
                if (aCount - 1 < anIndex)
                {
                    anImage = Res.IMAGE_LS_THUMBNAILS_PEGGLE2;
                    anIndex -= aCount;
                }
                else
                {
                    anImage = Res.IMAGE_LS_THUMBNAILS_PEGGLE;
                }
            }
            else
            {
                int aCount = Res.IMAGE_LS_THUMBNAILS_NIGHTS.mNumCols * Res.IMAGE_LS_THUMBNAILS_NIGHTS.mNumRows;
                anIndex = (theStage - 0xb) * 5 + theLevel;
                if (aCount - 1 < anIndex)
                {
                    anImage = Res.IMAGE_LS_THUMBNAILS_NIGHTS2;
                    anIndex -= aCount;
                }
                else
                {
                    anImage = Res.IMAGE_LS_THUMBNAILS_NIGHTS;
                }
            }
#endif
            g.DrawImageCel(anImage, theRect, anIndex % anImage.mNumCols, anIndex / anImage.mNumCols);
        }

        public static int GetColorForStage(int theStage, int theLevelPack, bool forcePeggle)
        {
            if (forcePeggle || theLevelPack == 0)
            {
                switch (theStage)
                {
                case 0: return 0x65aaf5;
                case 1: return 0x9bd54d;
                case 2: return 0xf3c13e;
                case 3: return 0xcb43ff;
                case 4: return 0x28e5ff;
                case 5: return 0x7c2c45;
                case 6: return 0xfeb0e0;
                case 7: return 0xf55e57;
                case 8: return 0xfed811;
                case 9: return 0xdff7f9;
                case 10: return 0xad3efd;
                default: return 0xffffff;
                }
            }
            switch (theStage)
            {
            case 0: return 0x65aaf5;
            case 1: return 0x9bd54d;
            case 2: return 0x7c2c45;
            case 3: return 0xf3c13e;
            case 4: return 0xcb43ff;
            case 5: return 0x28e5ff;
            case 6: return 0xfeb0e0;
            case 7: return 0xfed811;
            case 8: return 0xf55e57;
            case 9: return 0xdff7f9;
            case 10: return 0xad3efd;
            default: return 0xffffff;
            }
        }

        private static readonly int[] sStyleShotScores =
        {
            25000, 25000, 50000, 50000, 50000, 25000, 10000, 25000, 100000, 5000, 25000, 25000,
            25000, 25000, 100000, 50000, 10000, 0, 0, 0, 10000, 25000, 25000, 25000
        };

        private static readonly int[] sStyleShotScoresDuel =
        {
            5000, 5000, 5000, 5000, 5000, 5000, 2500, 5000, 10000, 2500, 2500, 5000,
            2500, 5000, 10000, 10000, 5000, 2500, 2500, 2500, 5000, 10000, 5000, 5000
        };

        public static int GetStyleShotScore(int theStyleShot, int theAlt, int unused)
        {
            return GetStyleShotScore(theStyleShot, theAlt != 0);
        }

        public static int GetStyleShotScore(int theStyleShot, bool theAlt)
        {
            if (theStyleShot < 0 || theStyleShot >= sStyleShotScores.Length)
            {
                return 0;
            }
            return theAlt ? sStyleShotScoresDuel[theStyleShot] : sStyleShotScores[theStyleShot];
        }

        public static float PulseVal(int theTick, int thePeriod)
        {
            int v = theTick % (thePeriod * 2);
            if (thePeriod < v)
            {
                v = thePeriod * 2 - v;
            }
            return v / (float)thePeriod;
        }

        /// <summary>Triangle wave like PulseVal, held at 0 for the first <paramref name="theDelay"/> ticks of each cycle.</summary>
        public static float PulseVal(int theTick, int thePeriod, int theDelay)
        {
            int total = theDelay + thePeriod * 2;
            int v = theTick % total;
            if (v < theDelay)
            {
                return 0.0f;
            }
            v -= theDelay;
            if (v > thePeriod)
            {
                v = thePeriod * 2 - v;
            }
            return v / (float)thePeriod;
        }

        /// <summary>Half-angle of the launcher's swing, in degrees.</summary>
        /// <summary>
        /// Port: the iPhone build's limit (83.5) for both editions; the HD build used 85.5, which
        /// keeps the launcher 2 degrees lower.
        /// </summary>
        public static float GetAngleConstraint()
        {
            return 83.5f;
        }

        public static float DegToRad(float theDeg)
        {
            return (theDeg * 3.1415927f) / 180.0f;
        }

        public static float RadToDeg(float theRad)
        {
            return (theRad * 180.0f) / 3.1415927f;
        }

        /// <summary>Wraps an angle into [0, 360).</summary>
        public static float GetCanonicalAngleDeg(float theAngle)
        {
            if (theAngle >= 0.0f && theAngle < 360.0f)
            {
                return theAngle;
            }
            if (theAngle < 0.0f)
            {
                return 360.0f - (-theAngle % 360.0f);
            }
            return theAngle % 360.0f;
        }

        /// <summary>Wraps an angle into [0, 2pi).</summary>
        public static float GetCanonicalAngleRad(float theAngle)
        {
            const float TWO_PI = 6.2831855f;
            if (theAngle >= 0.0f && theAngle < TWO_PI)
            {
                return theAngle;
            }
            if (theAngle < 0.0f)
            {
                return TWO_PI - (-theAngle % TWO_PI);
            }
            return theAngle % TWO_PI;
        }

        /// <summary>The file name part of a path, optionally without its extension (Sexy::GetFileName).</summary>
        public static string GetFileName(string thePath, bool noExtension)
        {
            int lastSlash = Math.Max(thePath.LastIndexOf('\\'), thePath.LastIndexOf('/'));
            if (noExtension)
            {
                int lastDot = thePath.LastIndexOf('.');
                if (lastDot > lastSlash)
                {
                    return thePath.Substring(lastSlash + 1, lastDot - lastSlash - 1);
                }
            }
            return lastSlash == -1 ? thePath : thePath.Substring(lastSlash + 1);
        }

        /// <summary>Modulo that wraps negative values into [0, theMod] (MyMod in the original).</summary>
        public static int MyMod(int theValue, int theMod)
        {
            if (theValue < 0)
            {
                return theMod - (-theValue) % theMod;
            }
            return theValue % theMod;
        }

        /// <summary>Rotates (x, y) by <paramref name="theAngle"/> radians around (cx, cy), clockwise on screen.</summary>
        public static void RotateXY(ref float x, ref float y, float cx, float cy, float theAngle)
        {
            float dx = x - cx;
            float dy = y - cy;
            float c = (float)Math.Cos(theAngle);
            float s = (float)Math.Sin(theAngle);
            x = dx * c + cx + dy * s;
            y = dy * c + cy - dx * s;
        }

        /// <summary>Sexy::dist&lt;float&gt;.</summary>
        public static float Dist(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        /// <summary>t * a + (1 - t) * b.</summary>
        public static float LinearCombineF(float t, float a, float b)
        {
            return t * a + (1.0f - t) * b;
        }

        /// <summary>
        /// Draws theImage (or its theSrcRect) stretched to a circle of radius theRadius + theExtra
        /// centred at (theX, theY); unstretched when the source already has that diameter.
        /// </summary>
        public static void DrawCircle(Graphics g, float theX, float theY, float theRadius, Image theImage, TRect? theSrcRect, int theExtra)
        {
            TRect aSrc = theSrcRect ?? new TRect(0, 0, theImage.GetWidth(), theImage.GetHeight());
            float aRadius = theRadius + theExtra;
            if (aSrc.mWidth == aRadius * 2.0f && aSrc.mHeight == aRadius * 2.0f)
            {
                if (g.Is3D())
                {
                    g.DrawImageF(theImage, theX - aSrc.mWidth / 2.0f, theY - aSrc.mHeight / 2.0f, aSrc);
                    return;
                }
                g.DrawImage(theImage, (int)(theX - aSrc.mWidth / 2.0f), (int)(theY - aSrc.mHeight / 2.0f), aSrc);
                return;
            }
            g.DrawImage(theImage, new TRect((int)(theX - aRadius), (int)(theY - aRadius), (int)(aRadius * 2.0f), (int)(aRadius * 2.0f)), aSrc);
        }

        public static void DrawEllipse(Graphics g, float theX, float theY, float theRadiusX, float theRadiusY, Image theImage)
        {
            TRect aSrc = new TRect(0, 0, theImage.GetWidth(), theImage.GetHeight());
            TRect aDest = new TRect((int)(theX - theRadiusX), (int)(theY - theRadiusY), (int)(theRadiusX * 2.0f), (int)(theRadiusY * 2.0f));
            bool wasColorized = g.GetColorizeImages();
            g.SetFastStretch(true);
            g.SetColorizeImages(true);
            g.DrawImage(theImage, aDest, aSrc);
            g.SetColorizeImages(wasColorized);
        }

        public static float InterpValF(int theTick, int theDuration, float theStart, float theEnd)
        {
            if (theTick < theDuration)
            {
                float t = theTick / (float)theDuration;
                return t * theEnd + (1.0f - t) * theStart;
            }
            return theEnd;
        }

        public static float InterpValF(int theTick, int theDuration1, int theDuration2, float theStart, float theMid, float theEnd)
        {
            if (theTick < theDuration1)
            {
                float t = theTick / (float)theDuration1;
                return t * theMid + (1.0f - t) * theStart;
            }
            if (theTick < theDuration2 + theDuration1)
            {
                float t = (theTick - theDuration1) / (float)theDuration2;
                return t * theEnd + (1.0f - t) * theMid;
            }
            return theEnd;
        }

        public static string GetLocalizedPlayerName(string theName)
        {
            if (string.IsNullOrEmpty(theName))
            {
                return "";
            }
            switch (theName)
            {
            case "Computer":
            case "Player2":
            case "Player 2":
            case "Player1":
            case "Player 1":
                return SexyAppBase.Loc(theName);
            default:
                return theName;
            }
        }

        public static void DrawStringWithShadow(Graphics g, string theString, int theX, int theY, int theShadowColor, int theShadowOffset, int unused = 0)
        {
            Color color = g.GetColor();
            g.SetColor(MakeColor(theShadowColor));
            g.DrawString(theString, theShadowOffset + theX, theShadowOffset + theY);
            g.SetColor(color);
            g.DrawString(theString, theX, theY);
        }

        /// <summary>Nine-slice draw of an image split into a 3x3 grid.</summary>
        public static void MyDrawImageBox(Graphics g, TRect theDest, Image theImage)
        {
            TRect src = new TRect(0, 0, theImage.mWidth, theImage.mHeight);
            int cw = src.mWidth / 3;
            int ch = src.mHeight / 3;
            int mw = src.mWidth - cw * 2;
            int mh = src.mHeight - ch * 2;
            int sx = src.mX;
            int sy = src.mY;
            g.DrawImage(theImage, theDest.mX, theDest.mY, new TRect(sx, sy, cw, ch));
            g.DrawImage(theImage, theDest.mX + theDest.mWidth - cw, theDest.mY, new TRect(sx + cw + mw, sy, cw, ch));
            g.DrawImage(theImage, theDest.mX, theDest.mY + theDest.mHeight - ch, new TRect(sx, sy + ch + mh, cw, ch));
            g.DrawImage(theImage, theDest.mX + theDest.mWidth - cw, theDest.mY + theDest.mHeight - ch, new TRect(sx + cw + mw, sy + ch + mh, cw, ch));
            g.DrawImage(theImage, new TRect(theDest.mX, theDest.mY + ch, cw, theDest.mHeight - ch * 2), new TRect(sx, sy + ch, cw, mh));
            g.DrawImage(theImage, new TRect(theDest.mX + theDest.mWidth - cw, theDest.mY + ch, cw, theDest.mHeight - ch * 2), new TRect(sx + cw + mw, sy + ch, cw, mh));
            g.DrawImage(theImage, new TRect(theDest.mX + cw, theDest.mY, theDest.mWidth - cw * 2, ch), new TRect(sx + cw, sy, mw, ch));
            g.DrawImage(theImage, new TRect(theDest.mX + cw, theDest.mY + theDest.mHeight - ch, theDest.mWidth - cw * 2, ch), new TRect(sx + cw, sy + ch + mh, mw, ch));
            g.DrawImage(theImage, new TRect(theDest.mX + cw, theDest.mY + ch, theDest.mWidth - cw * 2, theDest.mHeight - ch * 2), new TRect(sx + cw, sy + ch, mw, mh));
        }

        /// <summary>The striped fill behind a ThunderDialog.</summary>
        public static void DrawDialogFill(Graphics g, TRect theRect, int theStripeColor, int theFillColor)
        {
            TRect r = new TRect(DIALOG_FILL_MARGIN_LEFT + theRect.mX, DIALOG_FILL_MARGIN_TOP + theRect.mY,
                theRect.mWidth - DIALOG_FILL_MARGIN_LEFT - DIALOG_FILL_MARGIN_RIGHT,
                theRect.mHeight - DIALOG_FILL_MARGIN_TOP - DIALOG_FILL_MARGIN_BOTTOM);
            g.SetColor(MakeColor(theFillColor));
            g.FillRect(r);
            g.SetColor(MakeColor(theStripeColor));
            for (int y = DIALOG_FILL_STRIPES_OFFSET_Y + r.mY; y < r.mY + r.mHeight; y += DIALOG_FILL_STRIPES_SPACING_Y)
            {
                g.FillRect(r.mX, y, r.mWidth, DIALOG_FILL_STRIPES_THICKNESS);
            }
        }

        public static Board GetBoard()
        {
            return ((ThunderballApp)GlobalStaticVars.gSexyAppBase).mBoard;
        }

        public static int ScreenToBoardX(int theX)
        {
            Board aBoard = GetBoard();
            return aBoard != null ? theX - aBoard.mBoardRect.mX : theX;
        }

        public static int ScreenToBoardY(int theY)
        {
            Board aBoard = GetBoard();
            return aBoard != null ? theY - aBoard.mBoardRect.mY : theY;
        }

        /// <summary>The last touch position in 600-high board coordinates.</summary>
        public static int MouseToBoardX()
        {
            return ScreenToBoardX((int)((double)GlobalStaticVars.gSexyAppBase.mWidgetManager.mLastMouseX * 600.0 / (double)gPhysicalDisplayHeight));
        }

        public static int MouseToBoardY()
        {
            return ScreenToBoardY((int)((double)GlobalStaticVars.gSexyAppBase.mWidgetManager.mLastMouseY * 600.0 / (double)gPhysicalDisplayHeight));
        }

        /// <summary>No-op on iOS.</summary>
        public static void SetAppTitle(string theTitle)
        {
        }

        private static TRect srcRectFF;

        private static TRect srcRectFire;

        /// <summary>Draws the frame around the board, with the fast-forward and fire buttons in its corners.</summary>
        public static void DrawScreenFrame(Graphics g)
        {
#if PEGGLE_IPHONE
            // The iPhone frame sits flush and has no fast-forward/fire corners (the dial widget has those).
            g.DrawImage(Res.IMAGE_SCREENFRAMELEFT, 0, 0);
            g.DrawImage(Res.IMAGE_SCREENFRAMETOP, Res.IMAGE_SCREENFRAMELEFT.mWidth, 0);
            g.DrawImage(Res.IMAGE_SCREENFRAMEBOTTOM, Res.IMAGE_SCREENFRAMELEFT.mWidth,
                Res.IMAGE_SCREENFRAMELEFT.mHeight - Res.IMAGE_SCREENFRAMEBOTTOM.mHeight);
            g.DrawImage(Res.IMAGE_SCREENFRAMERIGHT, Res.IMAGE_SCREENFRAMETOP.mWidth + Res.IMAGE_SCREENFRAMELEFT.mWidth, 0);
#else
            g.DrawImage(Res.IMAGE_SCREENFRAMELEFT, -10, -10);
            g.DrawImage(Res.IMAGE_SCREENFRAMETOP, Res.IMAGE_SCREENFRAMELEFT.mWidth, -10);
            g.DrawImage(Res.IMAGE_SCREENFRAMEBOTTOM, Res.IMAGE_SCREENFRAMELEFT.mWidth + -10,
                Res.IMAGE_SCREENFRAMELEFT.mHeight - Res.IMAGE_SCREENFRAMEBOTTOM.mHeight + -10);
            g.DrawImage(Res.IMAGE_SCREENFRAMERIGHT, Res.IMAGE_SCREENFRAMETOP.mWidth + Res.IMAGE_SCREENFRAMELEFT.mWidth + -10, -10);
            srcRectFF = Res.IMAGE_SCREENFRAMEFF.GetCelRect(0);
            srcRectFire = Res.IMAGE_SCREENFRAMEFIRE.GetCelRect(0);
            Board aBoard = ((ThunderballApp)GlobalStaticVars.gSexyAppBase).mBoard;
            if (aBoard.mFireButton.mIsDown)
            {
                srcRectFire = Res.IMAGE_SCREENFRAMEFIRE.GetCelRect(1);
            }
            if (aBoard.mFFwdPressed)
            {
                srcRectFF = Res.IMAGE_SCREENFRAMEFF.GetCelRect(1);
            }
            g.DrawImage(Res.IMAGE_SCREENFRAMEFF, 0, 0, srcRectFF);
            g.DrawImage(Res.IMAGE_SCREENFRAMEFIRE, 0x400 - Res.IMAGE_SCREENFRAMEFIRE.mWidth,
                0x300 - Res.IMAGE_SCREENFRAMEFIRE.mHeight / 2, srcRectFire);
#endif
        }

        /// <summary>Centres a dialog on screen, at least wide enough for its header.</summary>
        public static void PositionDialog(Dialog theDialog, int theWidth, bool unused1, int unused2)
        {
            int minWidth = theDialog.mHeaderFont.StringWidth(theDialog.mDialogHeader) + theDialog.mContentInsets.mLeft + theDialog.mContentInsets.mRight +
                theDialog.mBackgroundInsets.mLeft + theDialog.mBackgroundInsets.mRight + 20;
            int width = Math.Max(theWidth, minWidth);
            int height = theDialog.GetPreferredHeight(width);
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            theDialog.Resize(app.mWidth / 2 - width / 2, app.mHeight / 2 - height / 2, width, height);
        }

        public static Font GetFontById(int theId)
        {
            return Res.GetFontById((ResourceId)theId);
        }

        public static Image GetImageById(int theId)
        {
            return Res.GetImageById((ResourceId)theId);
        }

        public static int GetSoundById(int theId)
        {
            return Res.GetSoundById((ResourceId)theId);
        }
    }
}
