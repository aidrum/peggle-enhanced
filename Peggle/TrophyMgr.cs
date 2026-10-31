using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>A challenge opponent: character id (-2 none, 11 random) and AI difficulty.</summary>
    public class OpponentInfo
    {
        public int mCharacterId;                                  // 0x0

        public int mDifficulty;                                   // 0x4
    }

    /// <summary>One challenge ("trophy") from levels/trophy.cfg (Sexy::TrophyInfo).</summary>
    public class TrophyInfo
    {
        public int mId;                                           // 0x008

        public string mName = "";                                 // 0x00c

        public string mDesc = "";                                 // 0x010

        public string mSmallDesc = "";                            // 0x014

        public string mInGameDesc = "";                           // 0x018

        public string mThumbnailOverride = "";                    // 0x01c

        public string mBackgroundOverride = "";                   // 0x020

        public bool mFlag24;                                      // 0x024

        public int mBalls;                                        // 0x028

        public int mNumOrange;                                    // 0x02c

        public int mLives;                                        // 0x030

        public int mScoreReq;                                     // 0x034

        public int mScoreCeil;                                    // 0x038

        public bool mScoreReset;                                  // 0x03c

        public bool mClearLevel;                                  // 0x03d

        public int mStyleScoreReq;                                // 0x040

        public int mUniqueStyleShotsReq;                          // 0x044

        public bool mFlag48;                                      // 0x048

        public int mLevelPack;                                    // 0x04c

        public bool mNoFreeballs;                                 // 0x050

        public bool mNoGreens;                                    // 0x051

        public bool mNoForceWin;                                  // 0x052

        public bool mNoEndOnWin;                                  // 0x053

        public bool mFreeballCovered;                             // 0x054

        public int mScorePenaltyForNoOrange;                      // 0x058

        public int[] mStyleShotsReq = new int[24];                // 0x05c

        public int[] mPowerups = new int[15];                     // 0x0bc

        public int mMagicHat;                                     // 0x0f8

        public float mGravityMod;                                 // 0x0fc

        public float mProjSpeedMod;                               // 0x100

        public int mTripleScore;                                  // 0x104

        public List<string> mLevels = new List<string>();        // 0x108

        public List<OpponentInfo> mOpponents = new List<OpponentInfo>(); // 0x114

        public List<int> mCharacters = new List<int>();          // 0x120

        public TrophyInfo()
        {
            mScoreReset = true;
            mGravityMod = 1.0f;
            mProjSpeedMod = 1.0f;
            mLives = 0;
            mUniqueStyleShotsReq = -1;
        }

        public OpponentInfo GetOpponentInfo(int theIndex)
        {
            if (mOpponents.Count == 0 || theIndex < 0)
            {
                return null;
            }
            return theIndex < mOpponents.Count ? mOpponents[theIndex] : mOpponents[mOpponents.Count - 1];
        }

        public int GetCharacterId(int theIndex)
        {
            if (mCharacters.Count == 0 || theIndex < 0)
            {
                return 0;
            }
            return theIndex < mCharacters.Count ? mCharacters[theIndex] : mCharacters[mCharacters.Count - 1];
        }
    }

    /// <summary>A page of the trophy room (Sexy::TrophyPage).</summary>
    public class TrophyPage
    {
        public string mName = "";                                 // 0x0

        public string mDesc = "";                                 // 0x4

        public string mSmallDesc = "";                            // 0x8

        public List<TrophyInfo> mTrophies = new List<TrophyInfo>(); // 0xc
    }

    /// <summary>Reads levels/trophy.cfg: pages of challenges, Peggle pages 0-7 then Nights (Sexy::TrophyMgr).</summary>
    public class TrophyMgr
    {
        public ThunderballApp mApp;                               // 0x04

        public List<TrophyPage> mPages = new List<TrophyPage>(); // 0x08

        public SortedDictionary<int, TrophyInfo> mTrophyMap = new SortedDictionary<int, TrophyInfo>(); // 0x14

        public TrophyMgr(ThunderballApp theApp)
        {
            mApp = theApp;
            Clear();
        }

        public void Clear()
        {
            mPages.Clear();
            mTrophyMap.Clear();
        }

        public int GetNumTrophies()
        {
            return mTrophyMap.Count;
        }

        public static int GetStyleShotByName(string theName)
        {
            for (int i = 0; i < 24; i++)
            {
                if (string.Equals(theName, SexyUtil.styleshot_names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return 24;
        }

        public static int GetPowerupByName(string theName)
        {
            for (int i = 0; i < 15; i++)
            {
                if (string.Equals(theName, SexyUtil.powerup_names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return 15;
        }

        private int ParseCharacter(ConfigParser theParser, string theName)
        {
            if (string.Equals(theName, "None", StringComparison.OrdinalIgnoreCase))
            {
                return -2;
            }
            if (string.Equals(theName, "Rand", StringComparison.OrdinalIgnoreCase))
            {
                return 11;
            }
            int id = mApp.mCharacterMgr.GetCharacterIdByName(theName);
            if (id < 0)
            {
                theParser.Error("Character not found: " + theName, false, true);
            }
            return id;
        }

        private static List<string> ReadStringList(StringParser theParser)
        {
            List<string> list = new List<string>();
            while (!theParser.EndOfString())
            {
                list.Add(theParser.ReadString(false, true));
                if (!theParser.CheckNextChar(','))
                {
                    break;
                }
            }
            return list;
        }

        private void ReadTrophyConfig(ConfigParser theParser, TrophyInfo theTrophy)
        {
            StringParser val = theParser.GetValParser();
            if (theParser.KeyIs("Levels"))
            {
                theTrophy.mLevels = ReadStringList(val);
                foreach (string level in theTrophy.mLevels)
                {
                    if (level.StartsWith("rand", StringComparison.OrdinalIgnoreCase) || string.Equals(level, "pick", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (GlobalStaticVars.gSexyAppBase != null && !ContentFS.Exists("levels/" + level + ".dat"))
                    {
                        theParser.Error("Level file not found: " + level, false, true);
                    }
                }
            }
            else if (theParser.KeyIs("StyleShots"))
            {
                foreach (string name in ReadStringList(val))
                {
                    int shot = GetStyleShotByName(name);
                    if (shot == 24)
                    {
                        theParser.Error("Style Shot not found: " + name, false, true);
                    }
                    else
                    {
                        theTrophy.mStyleShotsReq[shot]++;
                    }
                }
            }
            else if (theParser.KeyIs("Powerups"))
            {
                foreach (string entry in ReadStringList(val))
                {
                    string name = entry;
                    int count = 1;
                    int eq = name.IndexOf('=');
                    if (eq != -1)
                    {
                        string num = name.Substring(eq + 1).Trim();
                        try
                        {
                            count = Convert.ToInt32(num, 16);
                            name = name.Substring(0, eq);
                        }
                        catch (FormatException)
                        {
                            count = 1;
                        }
                    }
                    if (name == "MagicHat")
                    {
                        theTrophy.mMagicHat = count;
                    }
                    else if (name == "TripleScore")
                    {
                        theTrophy.mTripleScore = count;
                    }
                    else
                    {
                        int powerup = GetPowerupByName(name);
                        if (powerup == 15)
                        {
                            theParser.Error("Powerup not found: " + name, false, true);
                        }
                        else
                        {
                            theTrophy.mPowerups[powerup] += count + 1;
                        }
                    }
                }
            }
            else if (theParser.KeyIs("Characters"))
            {
                foreach (string name in ReadStringList(val))
                {
                    theTrophy.mCharacters.Add(ParseCharacter(theParser, name));
                }
            }
            else if (theParser.KeyIs("Opponents"))
            {
                foreach (string name in ReadStringList(val))
                {
                    OpponentInfo opp = new OpponentInfo();
                    opp.mCharacterId = ParseCharacter(theParser, name);
                    opp.mDifficulty = 0;
                    theTrophy.mOpponents.Add(opp);
                }
            }
            else if (theParser.KeyIs("OpponentDifficulty"))
            {
                List<int> list = new List<int>();
                while (!val.EndOfString())
                {
                    list.Add(val.ReadInt());
                    if (!val.CheckNextChar(','))
                    {
                        break;
                    }
                }
                if (list.Count != theTrophy.mOpponents.Count)
                {
                    theParser.Error("OpponentDifficulty must have same number of entries as the number of opponents.  And be specified second.", false, true);
                }
                for (int i = 0; i < list.Count && i < theTrophy.mOpponents.Count; i++)
                {
                    theTrophy.mOpponents[i].mDifficulty = list[i];
                }
            }
            else if (theParser.KeyIs("Id"))
            {
                theTrophy.mId = val.ReadInt();
            }
            else if (theParser.KeyIs("Balls"))
            {
                theTrophy.mBalls = val.ReadInt();
            }
            else if (theParser.KeyIs("NumOrange"))
            {
                theTrophy.mNumOrange = val.ReadInt();
            }
            else if (theParser.KeyIs("Lives"))
            {
                theTrophy.mLives = val.ReadInt();
            }
            else if (theParser.KeyIs("ScoreReset"))
            {
                theTrophy.mScoreReset = val.ReadBool();
            }
            else if (theParser.KeyIs("ClearLevel"))
            {
                theTrophy.mClearLevel = val.ReadBool();
            }
            else if (theParser.KeyIs("NoGreens"))
            {
                theTrophy.mNoGreens = val.ReadBool();
            }
            else if (theParser.KeyIs("FreeballCovered"))
            {
                theTrophy.mFreeballCovered = val.ReadBool();
            }
            else if (theParser.KeyIs("ScorePenaltyForNoOrange"))
            {
                theTrophy.mScorePenaltyForNoOrange = val.ReadInt();
            }
            else if (theParser.KeyIs("ThumbnailOverride"))
            {
                theTrophy.mThumbnailOverride = val.ReadString();
            }
            else if (theParser.KeyIs("BackgroundOverride"))
            {
                theTrophy.mBackgroundOverride = val.ReadString();
            }
            else if (theParser.KeyIs("LevelPackNights"))
            {
                theTrophy.mLevelPack = val.ReadInt();
            }
            else if (theParser.KeyIs("ScoreReq"))
            {
                theTrophy.mScoreReq = val.ReadInt();
                if (theTrophy.mScoreReq > 0)
                {
                    theTrophy.mScoreReset = false;
                }
            }
            else if (theParser.KeyIs("ScoreCeil"))
            {
                theTrophy.mScoreCeil = val.ReadInt();
                if (theTrophy.mScoreCeil > 0)
                {
                    theTrophy.mScoreReset = false;
                }
            }
            else if (theParser.KeyIs("NoFreeballs"))
            {
                theTrophy.mNoFreeballs = val.ReadBool();
            }
            else if (theParser.KeyIs("NoForceWin"))
            {
                theTrophy.mNoForceWin = val.ReadBool();
            }
            else if (theParser.KeyIs("NoEndOnWin"))
            {
                theTrophy.mNoEndOnWin = val.ReadBool();
            }
            else if (theParser.KeyIs("GravityMod"))
            {
                theTrophy.mGravityMod = val.ReadFloat(1.0f);
            }
            else if (theParser.KeyIs("ProjSpeedMod"))
            {
                theTrophy.mProjSpeedMod = val.ReadFloat(1.0f);
            }
            else if (theParser.KeyIs("StyleScoreReq"))
            {
                theTrophy.mStyleScoreReq = val.ReadInt();
            }
            else if (theParser.KeyIs("UniqueStyleShotsReq"))
            {
                theTrophy.mUniqueStyleShotsReq = val.ReadInt();
            }
            else if (theParser.KeyIs("Desc"))
            {
                theTrophy.mDesc = val.ReadString();
            }
            else if (theParser.KeyIs("InGameDesc"))
            {
                theTrophy.mInGameDesc = val.ReadString();
            }
            else if (theParser.KeyIs("SmallDesc"))
            {
                theTrophy.mSmallDesc = val.ReadString();
            }
            else
            {
                theParser.ErrorUnexpectedKey();
            }
        }

        private void ReadTrophyPage(ConfigParser theParser)
        {
            TrophyPage page = new TrophyPage();
            mPages.Add(page);
            page.mName = theParser.GetGroupName();
            bool inTrophy = false;
            while (theParser.ReadNext())
            {
                ConfigParser.ItemType type = theParser.GetType();
                if (type == ConfigParser.ItemType.GroupStart)
                {
                    if (theParser.GroupTypeIs("Trophy", 2))
                    {
                        inTrophy = true;
                        TrophyInfo trophy = new TrophyInfo();
                        page.mTrophies.Add(trophy);
                        trophy.mName = theParser.GetGroupName();
                        trophy.mLevelPack = mPages.Count > 8 ? 1 : 0;
                    }
                    else
                    {
                        theParser.ErrorUnexpectedGroupType();
                    }
                }
                else if (type == ConfigParser.ItemType.KeyValue)
                {
                    if (inTrophy)
                    {
                        ReadTrophyConfig(theParser, page.mTrophies[page.mTrophies.Count - 1]);
                    }
                    else if (theParser.KeyIs("Desc"))
                    {
                        page.mDesc = theParser.GetValParser().ReadString();
                    }
                    else if (theParser.KeyIs("SmallDesc"))
                    {
                        page.mSmallDesc = theParser.GetValParser().ReadString();
                    }
                    else
                    {
                        theParser.ErrorUnexpectedKey();
                    }
                }
                else
                {
                    if (!inTrophy)
                    {
                        break;
                    }
                    inTrophy = false;
                    TrophyInfo trophy = page.mTrophies[page.mTrophies.Count - 1];
                    if (trophy.mId == 0)
                    {
                        theParser.Error("Must specifty trophy id.", false, true);
                    }
                    if (mTrophyMap.ContainsKey(trophy.mId))
                    {
                        theParser.Error(CFormat.StrFormat("Id already exists: %d", trophy.mId), false, true);
                    }
                    else
                    {
                        mTrophyMap[trophy.mId] = trophy;
                    }
                    if (string.IsNullOrEmpty(trophy.mSmallDesc))
                    {
                        trophy.mSmallDesc = page.mSmallDesc;
                    }
                }
            }
        }

        public void ReadConfig(string theFileName)
        {
            Clear();
            ConfigParser parser = new ConfigParser(true);
            parser.Open(theFileName, true);
            while (parser.ReadNext())
            {
                if (parser.GetType() == ConfigParser.ItemType.GroupStart)
                {
                    if (parser.GroupTypeIs("Page", 1))
                    {
                        ReadTrophyPage(parser);
                    }
                    else
                    {
                        parser.ErrorUnexpectedGroupType();
                    }
                }
                else
                {
                    parser.Error("Unexpected stuff", false, true);
                }
            }
        }

        public void Load(string theFileName)
        {
            ReadConfig(theFileName ?? "levels/trophy.cfg");
        }

        public TrophyPage GetTrophyPage(int thePage)
        {
            if (thePage >= 0 && thePage < mPages.Count)
            {
                return mPages[thePage];
            }
            return null;
        }

        public TrophyInfo GetTrophyInfoById(int theId)
        {
            return mTrophyMap.TryGetValue(theId, out TrophyInfo info) ? info : null;
        }

        public static int GetNumPagesCurGame()
        {
            return ThunderballApp.mGameLevelPack == 1 ? 0xc : 8;
        }

        public static int GetNumTrophiesCurGame()
        {
            return ThunderballApp.mGameLevelPack == 1 ? 0x3c : 0x28;
        }
    }
}
