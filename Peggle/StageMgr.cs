using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>A line of stage story / credits text (Sexy::StoryData).</summary>
    public class StoryData
    {
        public string mText = "";                                 // 0x0

        public int mParam = -1;                                   // 0x4
    }

    /// <summary>One level of a stage (Sexy::LevelInfo); mName is the level file in levels/.</summary>
    public class LevelInfo
    {
        public int mLastUseTime;                                  // 0x08

        public int mLevel;                                        // 0x0c

        public int mStage;                                        // 0x10

        public int mAceScore;                                     // 0x14

        public string mName = "";                                 // 0x18

        public string mDisplayName = "";                          // 0x1c

        public string mDialogText = "";                           // 0x20

        public string mDialogTitle = "";                          // 0x24

        public bool mExcludeFromRandom;                           // 0x28

        public LevelInfo()
        {
            mStage = 0;
            mLevel = 0;
            mLastUseTime = -1000;
            mExcludeFromRandom = false;
        }
    }

    /// <summary>
    /// Port addition: a Quick Play page of bonus levels from the PC games, read from its own cfg
    /// after stages.cfg. Its stages follow the ones before it.
    /// </summary>
    public class BonusPage
    {
        public string mConfig;

        /// <summary>The level pack it plays as (music, high scores, colours): 0 Peggle, 1 Nights.</summary>
        public int mLevelPack;

        /// <summary>Its logo in the level screen's arrow sheet (GameArrowButton.LOGO_*).</summary>
        public int mLogoCel;

        /// <summary>The colour of its thumbnail outlines.</summary>
        public int mOutlineColor;

        public int mFirstStage;

        public int mNumStages;

        /// <summary>
        /// The page's own Adventure (Select Game &gt; More), or null: the key its progress and saved game
        /// are stored under. A page with an Adventure unlocks its levels as that Adventure is played;
        /// the others are always open.
        /// </summary>
        public string mAdventureKey;

        /// <summary>Its Adventure's master for each stage (character ids, as GetCharacterForStage returns).</summary>
        public int[] mStageCharacters;

        /// <summary>
        /// The credits its Adventure's win rolls (the last stage's Credit number - 1), or -1 for none:
        /// the win then goes back to the main menu.
        /// </summary>
        public int mCreditsEnding;

        /// <summary>
        /// How many of its stages, from the first, its Adventure plays (-1: all). The stages after
        /// them are Quick Play levels only, always open (the WoW addon levels under the WoW page).
        /// </summary>
        public int mAdventureStageCount;

        /// <summary>
        /// True for a page shown on the Peggle Quick Play page, under Peggle's stages, instead of on a
        /// page of its own (the web demo levels). It isn't one of the paged ones (GetNumBonusPages).
        /// </summary>
        public bool mOnPeggleRows;

        public BonusPage(string theConfig, int theLevelPack, int theLogoCel, int theOutlineColor, string theAdventureKey = null,
            int[] theStageCharacters = null, int theCreditsEnding = -1, int theAdventureStageCount = -1)
        {
            mAdventureStageCount = theAdventureStageCount;
            mCreditsEnding = theCreditsEnding;
            mConfig = theConfig;
            mLevelPack = theLevelPack;
            mLogoCel = theLogoCel;
            mOutlineColor = theOutlineColor;
            mAdventureKey = theAdventureKey;
            mStageCharacters = theStageCharacters;
        }

        public bool HasAdventure => mAdventureKey != null;

        /// <summary>The number of stages its Adventure plays.</summary>
        public int AdventureStages => mAdventureStageCount < 0 ? mNumStages : Math.Min(mAdventureStageCount, mNumStages);

        /// <summary>True for a stage its Adventure plays (and unlocks).</summary>
        public bool IsAdventureStage(int theStage)
        {
            return HasAdventure && theStage >= mFirstStage && theStage < mFirstStage + AdventureStages;
        }

        /// <summary>The Adventure's last stage (its final one is an ordinary stage, not a Master stage).</summary>
        public int mLastStage => mFirstStage + AdventureStages - 1;
    }

    /// <summary>A stage of five levels plus its story texts (Sexy::StageInfo).</summary>
    public class StageInfo
    {
        public const int NUM_STORY_TEXTS = 5;

        public List<StoryData>[] mStoryText = new List<StoryData>[NUM_STORY_TEXTS]; // 0x08

        public List<LevelInfo> mLevels = new List<LevelInfo>();  // 0x44

        public StageInfo()
        {
            for (int i = 0; i < NUM_STORY_TEXTS; i++)
            {
                mStoryText[i] = new List<StoryData>();
            }
        }

        public LevelInfo GetLevelInfo(int theLevel)
        {
            if (theLevel >= 0 && theLevel < mLevels.Count)
            {
                return mLevels[theLevel];
            }
            return null;
        }
    }

    /// <summary>
    /// The stage/level catalogue read from levels/stages.cfg (Sexy::StageMgr): stages 0-10 are
    /// Peggle, 11-22 Peggle Nights. Also owns the loading-screen tips and random level picking.
    /// Stage 23 holds the challenge-only Tilt level. Port addition: the bonus pages (BONUS_PAGES)
    /// follow as stages 24+, the web demo levels (shown on the Peggle page) last.
    /// </summary>
    public class StageMgr
    {
        /// <summary>Port addition: index of the first bonus stage.</summary>
        public const int BONUS_FIRST_STAGE = 0x18;

        /// <summary>Port addition: the bonus Quick Play pages, in page (and stage) order.</summary>
        public static readonly BonusPage[] BONUS_PAGES =
        {
            new BonusPage("levels/stages_nightsdlc.cfg", 1, GameArrowButton.LOGO_NIGHTSDLC, 0xff55e9),
            // Peggle Dual Shot (DS) exclusives: no Adventure, so always open; they play as Peggle levels.
            new BonusPage("levels/stages_dualshot.cfg", 0, GameArrowButton.LOGO_DUALSHOT, 0x9ee02a),
            // Extreme: Bjorn hosts both stages; its win rolls Credit 3 ("Well Done!": Credit 1 promises
            // challenges and Credit 2 the full game, which don't apply here). WoW: Bjorn, then Splork
            // (Space Blast); it has no credits.
            new BonusPage("levels/stages_extreme.cfg", 0, GameArrowButton.LOGO_EXTREME, 0xff8a2a, ADVENTURE_EXTREME, new[] { 0, 0 }, 2),
            // Its Adventure is the first two stages; the WoW addon levels follow, outside it.
            new BonusPage("levels/stages_wow.cfg", 1, GameArrowButton.LOGO_WOW, 0xf0b030, ADVENTURE_WOW, new[] { 0, 3 }, -1, 2),
            // The Peggle web demo's own levels: under Peggle's stages on the Peggle page, always open.
            // Last, so the other pages' stage numbers don't move.
            new BonusPage("levels/stages_demo.cfg", 0, -1, 0xeac76a) { mOnPeggleRows = true },
        };

        /// <summary>Port addition: the bonus Adventures' keys (BonusPage.mAdventureKey).</summary>
        public const string ADVENTURE_EXTREME = "extreme";

        public const string ADVENTURE_WOW = "wow";

        /// <summary>Port addition: the loaded bonus page with this Adventure key, or null.</summary>
        public static BonusPage GetAdventurePage(string theKey)
        {
            foreach (BonusPage aPage in sBonusPages)
            {
                if (aPage.mAdventureKey == theKey && aPage.mNumStages > 0)
                {
                    return aPage;
                }
            }
            return null;
        }

        public ThunderballApp mApp;                               // 0x04

        public int mUseTime;                                      // 0x08

        public List<StageInfo> mStages = new List<StageInfo>();  // 0x0c

        public SortedDictionary<string, LevelInfo> mLevelMap = new SortedDictionary<string, LevelInfo>(StringComparer.OrdinalIgnoreCase); // 0x18

        public List<string> mTips = new List<string>();          // 0x30

        public StageMgr(ThunderballApp theApp)
        {
            mApp = theApp;
        }

        public int GetNumLevels()
        {
            return mLevelMap.Count;
        }

        public int GetNumStages()
        {
            return mStages.Count;
        }

        public void Clear()
        {
            mUseTime = 0;
            mStages.Clear();
            mLevelMap.Clear();
            mTips.Clear();
        }

        public StageInfo GetStageInfo(int theStage)
        {
            if (theStage >= 0 && theStage < mStages.Count)
            {
                return mStages[theStage];
            }
            return null;
        }

        /// <summary>Port addition: the number of bonus stages loaded.</summary>
        public int GetNumBonusStages()
        {
            return Math.Max(0, mStages.Count - BONUS_FIRST_STAGE);
        }

        public int GetNumBonusLevels()
        {
            int aCount = 0;
            for (int i = BONUS_FIRST_STAGE; i < mStages.Count; i++)
            {
                aCount += mStages[i].mLevels.Count;
            }
            return aCount;
        }

        private static readonly List<BonusPage> sBonusPages = new List<BonusPage>();

        private static BonusPage sPeggleRowsPage;

        /// <summary>Port addition: the loaded bonus stages shown on the Peggle page (the web demo levels), or null.</summary>
        public static BonusPage GetPeggleRowsPage()
        {
            return sPeggleRowsPage;
        }

        /// <summary>Port addition: the bonus pages that were loaded, in page order (not the Peggle page's rows).</summary>
        public static int GetNumBonusPages()
        {
            return sBonusPages.Count;
        }

        public static BonusPage GetBonusPageAt(int theIndex)
        {
            return sBonusPages[theIndex];
        }

        /// <summary>Port addition: the index of the bonus page a stage is on, or -1 (also for the Peggle page's rows).</summary>
        public static int GetBonusPageIndex(int theStage)
        {
            for (int i = 0; i < sBonusPages.Count; i++)
            {
                BonusPage aPage = sBonusPages[i];
                if (theStage >= aPage.mFirstStage && theStage < aPage.mFirstStage + aPage.mNumStages)
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool IsBonusStage(int theStage)
        {
            return theStage >= BONUS_FIRST_STAGE;
        }

        /// <summary>
        /// Port addition: a stage's number as its pack shows it, from 1: Peggle's stages, Nights' (from
        /// stage 11) and each bonus page's (Nights bonus, Extreme, WoW) counted from their own first stage.
        /// </summary>
        public static int GetStageNumberInPack(int theStage)
        {
            BonusPage aPage = GetBonusPage(theStage);
            if (aPage != null)
            {
                return theStage - aPage.mFirstStage + 1;
            }
            return theStage > 10 ? theStage - 10 : theStage + 1;
        }

        /// <summary>Port addition: the bonus page a stage is on (or the Peggle page's rows), or null.</summary>
        public static BonusPage GetBonusPage(int theStage)
        {
            int anIndex = GetBonusPageIndex(theStage);
            if (anIndex >= 0)
            {
                return sBonusPages[anIndex];
            }
            BonusPage aRows = sPeggleRowsPage;
            return aRows != null && theStage >= aRows.mFirstStage && theStage < aRows.mFirstStage + aRows.mNumStages ? aRows : null;
        }

        /// <summary>Port addition: true for a bonus level name.</summary>
        public bool IsBonusLevel(string theName)
        {
            LevelInfo aLevel = GetLevelInfoByName(theName);
            return aLevel != null && IsBonusStage(aLevel.mStage);
        }

        /// <summary>Port addition: the level pack a stage plays as; a bonus page sets its own.</summary>
        public static int GetLevelPackForStage(int theStage)
        {
            BonusPage aPage = GetBonusPage(theStage);
            if (aPage != null)
            {
                return aPage.mLevelPack;
            }
            return theStage < 0xb ? 0 : 1;
        }

        public static int GetNumLevelsCurGame()
        {
            return ThunderballApp.mGameLevelPack == 1 ? 0x3c : 0x37;
        }

        public string GetTip(uint theIndex)
        {
            if (mTips.Count == 0)
            {
                return "";
            }
            return mTips[(int)(theIndex % (uint)mTips.Count)];
        }

        public void ResetLastUseTimes()
        {
            mUseTime = 0;
            foreach (StageInfo stage in mStages)
            {
                foreach (LevelInfo level in stage.mLevels)
                {
                    level.mLastUseTime = -1000;
                }
            }
        }

        private void CollectRandomLevels(List<LevelInfo> theList, int theFirstStage, int theEndStage, int theMaxStage, int theMaxLevel,
            int theCurStage, int theCurLevel, bool excludeFlagged, int theMinAge)
        {
            for (int s = theFirstStage; s < theEndStage && s < mStages.Count; s++)
            {
                StageInfo stage = mStages[s];
                for (int l = 0; l < stage.mLevels.Count; l++)
                {
                    LevelInfo level = stage.mLevels[l];
                    if (s == theCurStage && l == theCurLevel)
                    {
                        continue;
                    }
                    if (theMaxStage < s || (s == theMaxStage && theMaxLevel <= l))
                    {
                        break;
                    }
                    if ((!excludeFlagged || !level.mExcludeFromRandom) && (theMinAge < 1 || theMinAge <= mUseTime - level.mLastUseTime))
                    {
                        theList.Add(level);
                    }
                }
            }
        }

        /// <summary>
        /// Picks a random unlocked level other than the current one (in/out params). Peggle stages
        /// are limited by (theMaxStage, theMaxLevel), Nights stages by (theNightsMaxStage,
        /// theNightsMaxLevel). theMinAge &gt; 0 avoids recently used levels. Port: includeBonus adds
        /// the bonus pages' levels (always unlocked; with curPackOnly, those of pages playing as the
        /// current pack), for Quick Play's Random Level; challenges and the demo keep the original pool.
        /// </summary>
        public void GetRandomLevel(int theMaxStage, int theMaxLevel, int theNightsMaxStage, int theNightsMaxLevel, ref int theStage, ref int theLevel,
            bool excludeFlagged, int theMinAge, bool retry, bool curPackOnly, bool includeBonus = false)
        {
            List<LevelInfo> levels = new List<LevelInfo>();
            if (!curPackOnly || ThunderballApp.mGameLevelPack == 0)
            {
                CollectRandomLevels(levels, 0, 0xb, theMaxStage, theMaxLevel, theStage, theLevel, excludeFlagged, theMinAge);
            }
            if (!curPackOnly || ThunderballApp.mGameLevelPack == 1)
            {
                CollectRandomLevels(levels, 0xb, 0x17, theNightsMaxStage, theNightsMaxLevel, theStage, theLevel, excludeFlagged, theMinAge);
            }
            if (includeBonus)
            {
                for (int s = BONUS_FIRST_STAGE; s < mStages.Count; s++)
                {
                    if (!curPackOnly || GetLevelPackForStage(s) == ThunderballApp.mGameLevelPack)
                    {
                        // A page with its own Adventure only offers the levels that Adventure has unlocked
                        // (the stages outside it are always open).
                        int aMaxStage = int.MaxValue;
                        int aMaxLevel = 0;
                        BonusPage aPage = GetBonusPage(s);
                        PlayerInfo aPlayer = mApp?.mPlayerInfo;
                        if (aPage != null && aPage.IsAdventureStage(s) && aPlayer != null && !PlayerInfo.gDebugUnlockAll)
                        {
                            BonusAdventureInfo anInfo = aPlayer.GetBonusAdventure(aPage);
                            aMaxStage = aPage.mFirstStage + anInfo.mMaxStage;
                            aMaxLevel = anInfo.mMaxLevel;
                        }
                        CollectRandomLevels(levels, s, s + 1, aMaxStage, aMaxLevel, theStage, theLevel, excludeFlagged, theMinAge);
                    }
                }
            }
            if (levels.Count > 0)
            {
                LevelInfo level = levels[SexyUtil.Rand() % levels.Count];
                if (theMinAge > 0)
                {
                    MarkLastUseTime(level);
                }
                theStage = level.mStage;
                theLevel = level.mLevel;
            }
            else if (retry)
            {
                ResetLastUseTimes();
                GetRandomLevel(theMaxStage, theMaxLevel, theNightsMaxStage, theNightsMaxLevel, ref theStage, ref theLevel, excludeFlagged, theMinAge, false, false, includeBonus);
            }
        }

        public void MarkLastUseTime(LevelInfo theLevel)
        {
            mUseTime++;
            theLevel.mLastUseTime = mUseTime;
        }

        public void GetRandomLevel(PlayerInfo thePlayer, ref int theStage, ref int theLevel, bool excludeFlagged, int theMinAge, bool unused, bool curPackOnly,
            bool includeBonus = false)
        {
            GetRandomLevel(thePlayer.mMaxStage, thePlayer.mMaxLevel, thePlayer.mNightsMaxStage, thePlayer.mNightsMaxLevel, ref theStage, ref theLevel,
                excludeFlagged, theMinAge, true, curPackOnly, includeBonus);
        }

        public LevelInfo GetRandomLevel(PlayerInfo thePlayer, bool excludeFlagged, int theMinAge, bool curPackOnly)
        {
            int stage = -1;
            int level = -1;
            GetRandomLevel(thePlayer, ref stage, ref level, excludeFlagged, theMinAge, true, curPackOnly);
            return GetStageInfo(stage)?.GetLevelInfo(level);
        }

        public LevelInfo GetLevelInfoByName(string theName)
        {
            return theName != null && mLevelMap.TryGetValue(theName, out LevelInfo info) ? info : null;
        }

        private void ReadStageConfig(ConfigParser theParser, StageInfo theStage)
        {
            StringParser val = theParser.GetValParser();
            if (theParser.KeyIs("Level"))
            {
                LevelInfo level = new LevelInfo();
                theStage.mLevels.Add(level);
                level.mName = val.ReadString();
                level.mLevel = theStage.mLevels.Count - 1;
                level.mStage = mStages.Count - 1;
                level.mAceScore = 300000;
                if (!val.CheckNextChar(','))
                {
                    level.mDisplayName = level.mName;
                }
                else
                {
                    level.mDisplayName = val.ReadString();
                    if (val.CheckNextChar(','))
                    {
                        level.mAceScore = val.ReadInt();
                    }
                }
                if (GlobalStaticVars.gSexyAppBase != null && !ContentFS.Exists("levels/" + level.mName + ".dat"))
                {
                    theParser.Error("Level file not found: " + level.mName, false, true);
                }
                mLevelMap[level.mName] = level;
            }
            else if (theParser.KeyIs("Dialog"))
            {
                int num = val.ReadInt() - 1;
                val.CheckNextChar(',');
                if (num < 0 || num >= theStage.mLevels.Count)
                {
                    theParser.Error("Invalid level num.", false, true);
                    return;
                }
                LevelInfo level = theStage.GetLevelInfo(num);
                level.mDialogText = val.ReadHTMLString();
                if (val.CheckNextChar(','))
                {
                    level.mDialogTitle = val.ReadString(false, true);
                }
            }
            else if (theParser.KeyIs("StageDialog"))
            {
                int num = val.ReadInt() - 1;
                val.CheckNextChar(',');
                if (num < 0 || num >= StageInfo.NUM_STORY_TEXTS)
                {
                    theParser.Error("Invalid text num.", false, true);
                    return;
                }
                List<StoryData> texts = theStage.mStoryText[num];
                if (texts.Count < 1)
                {
                    texts.Add(new StoryData());
                }
                StoryData data = texts[0];
                data.mText += val.ReadHTMLString();
                if (val.CheckNextChar(','))
                {
                    data.mParam = val.ReadInt(-1);
                }
            }
            else if (theParser.KeyIs("Credit"))
            {
                int num = val.ReadInt() - 1;
                val.CheckNextChar(',');
                if (num < 0 || num >= StageInfo.NUM_STORY_TEXTS)
                {
                    theParser.Error("Invalid text num.", false, true);
                    return;
                }
                StoryData data = new StoryData();
                theStage.mStoryText[num].Add(data);
                data.mText = val.ReadHTMLString();
                if (val.CheckNextChar(','))
                {
                    data.mParam = val.ReadInt(-1);
                }
            }
            else
            {
                theParser.ErrorUnexpectedKey();
            }
        }

        private void ReadKeyVal(ConfigParser theParser)
        {
            StringParser val = theParser.GetValParser();
            if (theParser.KeyIs("ExcludeRandStages"))
            {
                do
                {
                    if (val.EndOfString())
                    {
                        break;
                    }
                    int stageNum = val.ReadInt() - 1;
                    StageInfo stage = GetStageInfo(stageNum);
                    if (stage == null)
                    {
                        theParser.Error(CFormat.StrFormat("Stage not found: %d", stageNum), false, true);
                        return;
                    }
                    foreach (LevelInfo level in stage.mLevels)
                    {
                        level.mExcludeFromRandom = true;
                    }
                }
                while (val.CheckNextChar(','));
            }
            else if (theParser.KeyIs("ExcludeRandLevels") || theParser.KeyIs("IncludeRandLevels"))
            {
                bool exclude = theParser.KeyIs("ExcludeRandLevels");
                while (!val.EndOfString())
                {
                    string name = val.ReadString(true, true);
                    LevelInfo level = GetLevelInfoByName(name);
                    if (level == null)
                    {
                        theParser.Error("Level not found: " + name, false, true);
                        return;
                    }
                    level.mExcludeFromRandom = exclude;
                    if (!val.CheckNextChar(','))
                    {
                        break;
                    }
                }
            }
            else if (theParser.KeyIs("Tip"))
            {
                mTips.Add(val.ReadString());
            }
            else
            {
                theParser.ErrorUnexpectedKey();
            }
        }

        public void ReadConfig(string theFileName)
        {
            Clear();
            ReadConfigFile(theFileName);
        }

        private void ReadConfigFile(string theFileName)
        {
            ConfigParser parser = new ConfigParser(true);
            parser.Open(theFileName, true);
            bool inStage = false;
            while (parser.ReadNext())
            {
                ConfigParser.ItemType type = parser.GetType();
                if (type == ConfigParser.ItemType.GroupStart)
                {
                    if (parser.GroupTypeIs("Stage", 1))
                    {
                        inStage = true;
                        mStages.Add(new StageInfo());
                    }
                    else
                    {
                        parser.ErrorUnexpectedGroupType();
                    }
                }
                else if (type == ConfigParser.ItemType.KeyValue)
                {
                    if (!inStage)
                    {
                        ReadKeyVal(parser);
                    }
                    else
                    {
                        ReadStageConfig(parser, mStages[mStages.Count - 1]);
                    }
                }
                else
                {
                    inStage = false;
                }
            }
        }

        public void Load(string theFileName)
        {
            ReadConfig(theFileName ?? "levels/stages.cfg");
            // Port addition: the bonus pages, after the Peggle, Nights and Tilt stages.
            bool aLoadBonus = theFileName == null && mStages.Count == BONUS_FIRST_STAGE;
            sBonusPages.Clear();
            sPeggleRowsPage = null;
            foreach (BonusPage aPage in BONUS_PAGES)
            {
                aPage.mFirstStage = mStages.Count;
                aPage.mNumStages = 0;
                if (aLoadBonus && ContentFS.Exists(aPage.mConfig))
                {
                    ReadConfigFile(aPage.mConfig);
                    aPage.mNumStages = mStages.Count - aPage.mFirstStage;
                    if (aPage.mOnPeggleRows)
                    {
                        sPeggleRowsPage = aPage.mNumStages > 0 ? aPage : null;
                    }
                    else
                    {
                        sBonusPages.Add(aPage);
                    }
                }
            }
        }
    }
}
