using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// A player profile (Sexy::PlayerInfo): progress, unlocked trophies, cleared/aced levels,
    /// options and statistics. Peggle and Peggle Nights keep separate copies of the adventure
    /// fields; the accessors pick one using ThunderballApp.mGameLevelPack.
    /// Saved as "userdata/user%d.dat"; the roster (name/id) lives in users.dat (ProfileMgr).
    /// Port addition: version 0x1b appends the bonus levels' (Nights bonus, Extreme) cleared/aced sets,
    /// 0x1c the bonus Adventures' progress (Extreme, WoW; BonusAdventureInfo). While one of those is
    /// played (ThunderballApp.mBonusAdventure) the accessors use its record instead.
    /// </summary>
    public class PlayerInfo
    {
        public const int DETAILS_VERSION = 0x1d;

        public string mName = "";                                 // 0x04

        public int mUseOrder;                                     // 0x08

        public int mId;                                           // 0x0c

        public int mMaxStage;                                     // 0x10

        public int mMaxLevel;                                     // 0x14

        public int mLastStageStage;                               // 0x18

        public int mLastStageLevel;                               // 0x1c

        public int mLastQuickPlayLevel;                           // 0x20

        public int mLastQuickPlayStage;                           // 0x24

        public int mCurCharacter;                                 // 0x28

        public int mCurCharacter2;                                // 0x2c

        public int mAdventureWins;                                // 0x30

        public bool mComputerPlayer;                              // 0x34

        public int mComputerSkill;                                // 0x38

        public int mDuelRounds;                                   // 0x3c

        public int mLeaveGameWarnings;                            // 0x40

        public bool mColorblind;                                  // 0x48

        public bool mLeftHanded;                                  // 0x49

        public bool mNeedShowStory;                               // 0x4a

        public uint mTipFlags;                                    // 0x4c

        public int mTotalTipCount;                                // 0x50

        public int mOtherTipCount;                                // 0x54

        public int[] mTipCounts = new int[10];                    // 0x58

        public string mUnused80 = "";                             // 0x80

        public string mPlayer2Name = "";                          // 0x84

        public string mWideName = "";                             // 0x88

        public bool mDirty;                                       // 0x8c

        public bool mSoundEnabled;                                // 0x8d

        public bool mMusicEnabled;                                // 0x8e

        public bool mFlag8f;                                      // 0x8f

        public List<int> mAdventureHighScores = new List<int>();  // 0x90 (multiset, ascending)

        public SortedSet<int> mPeggleTrophySet = new SortedSet<int>();                  // 0xa8

        public SortedDictionary<int, int> mTrophyTopScores = new SortedDictionary<int, int>(); // 0xc0

        public int mPortraitsShown;                               // 0xd8 (Nights masters revealed on the ProgressScreen)

        public List<int> mTrophyWins = new List<int> { 0, 0, 0, 0 };             // 0xdc

        public List<int> mTrophyWinsDisplayed = new List<int> { 0, 0, 0, 0 };    // 0xe8

        public int mLastTrophyWon;                                // 0xf4

        public SortedSet<string> mNightsClearedLevelSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase); // 0xf8

        public SortedSet<string> mNightsAcedLevelSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);    // 0x110

        public SortedSet<int> mNightsTrophySet = new SortedSet<int>();                  // 0x128

        public bool mShownAceAllStory;                            // 0x140

        public int mStoryShownLevel;                              // 0x144

        public int mBadgeCount;                                   // 0x148

        public SortedSet<string> mPeggleClearedLevelSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase); // 0x14c

        public SortedSet<string> mPeggleAcedLevelSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);    // 0x164

        public bool mNeedUpdateAcedLevels;                        // 0x17c

        /// <summary>Port addition: the bonus levels' own cleared/aced sets, kept apart so they don't count toward Peggle's or Nights'.</summary>
        public SortedSet<string> mBonusClearedLevelSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        public SortedSet<string> mBonusAcedLevelSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Port addition: the main-menu decals this player has seen (Peggle WoW's player +0xa4 name list): a
        /// decal not in it yet makes its entrance (the WoW banner burning in) and is added. Saved from version 0x1d.
        /// </summary>
        public SortedSet<string> mSeenDecals = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>Port addition: the bonus Adventures' progress, by BonusPage.mAdventureKey.</summary>
        public SortedDictionary<string, BonusAdventureInfo> mBonusAdventures = new SortedDictionary<string, BonusAdventureInfo>(StringComparer.Ordinal);

        public int mByte180;                                      // 0x180

        public int mByte184;                                      // 0x184

        public GameStats mPeggleAdventureStats = new GameStats(); // 0x188

        public GameStats mTotalStats = new GameStats();           // 0x204

        public int mPeggleAdventureScore;                         // 0x280

        public int mPeggleAdventureLevelReplay;                   // 0x284

        public int mPeggleAdventureNumRetries;                    // 0x288

        public int mNightsMaxStage;                               // 0x28c

        public int mNightsMaxLevel;                               // 0x290

        public int mNightsLastStageStage;                         // 0x294

        public int mNightsLastStageLevel;                         // 0x298

        public int mNightsAdventureWins;                          // 0x29c

        public int mNightsAdventureScore;                         // 0x2a0

        public int mNightsAdventureLevelReplay;                   // 0x2a4

        public int mNightsAdventureNumRetries;                    // 0x2a8

        public bool mNightsNeedShowStory;                         // 0x2ac

        public int mInt2b0;                                       // 0x2b0

        public GameStats mNightsAdventureStats = new GameStats(); // 0x2b4

        public bool mNightsTrialSeen;                             // 0x330

        public bool mNightsTrialIntroSeen;                        // 0x331

        public int mFirstAdventureStage;                          // 0x334

        public int mLastAdventureStage;                           // 0x338

        public int mNumAdventureStages;                           // 0x33c

        public int mInt340;                                       // 0x340

        public string mVersionString = "";                        // 0x344

        public string mWString348 = "";                           // 0x348

        private static bool IsNights => ThunderballApp.mGameLevelPack == 1;

        /// <summary>Port addition: the bonus Adventure being played, or null.</summary>
        private static BonusPage Bonus => ThunderballApp.mBonusAdventure;

        /// <summary>Port addition: a bonus Adventure's progress record, made on first use.</summary>
        public BonusAdventureInfo GetBonusAdventure(BonusPage thePage)
        {
            if (!mBonusAdventures.TryGetValue(thePage.mAdventureKey, out BonusAdventureInfo anInfo))
            {
                anInfo = new BonusAdventureInfo();
                mBonusAdventures[thePage.mAdventureKey] = anInfo;
            }
            return anInfo;
        }

        private BonusAdventureInfo CurBonus => GetBonusAdventure(Bonus);

        public PlayerInfo()
        {
            Reset(false);
        }

        public PlayerInfo Clone()
        {
            PlayerInfo p = (PlayerInfo)MemberwiseClone();
            p.mTipCounts = (int[])mTipCounts.Clone();
            p.mAdventureHighScores = new List<int>(mAdventureHighScores);
            p.mPeggleTrophySet = new SortedSet<int>(mPeggleTrophySet);
            p.mTrophyTopScores = new SortedDictionary<int, int>(mTrophyTopScores);
            p.mTrophyWins = new List<int>(mTrophyWins);
            p.mTrophyWinsDisplayed = new List<int>(mTrophyWinsDisplayed);
            p.mNightsClearedLevelSet = new SortedSet<string>(mNightsClearedLevelSet, StringComparer.OrdinalIgnoreCase);
            p.mNightsAcedLevelSet = new SortedSet<string>(mNightsAcedLevelSet, StringComparer.OrdinalIgnoreCase);
            p.mNightsTrophySet = new SortedSet<int>(mNightsTrophySet);
            p.mPeggleClearedLevelSet = new SortedSet<string>(mPeggleClearedLevelSet, StringComparer.OrdinalIgnoreCase);
            p.mPeggleAcedLevelSet = new SortedSet<string>(mPeggleAcedLevelSet, StringComparer.OrdinalIgnoreCase);
            p.mBonusClearedLevelSet = new SortedSet<string>(mBonusClearedLevelSet, StringComparer.OrdinalIgnoreCase);
            p.mBonusAcedLevelSet = new SortedSet<string>(mBonusAcedLevelSet, StringComparer.OrdinalIgnoreCase);
            p.mSeenDecals = new SortedSet<string>(mSeenDecals, StringComparer.Ordinal);
            p.mPeggleAdventureStats = new GameStats();
            p.mPeggleAdventureStats.Add(mPeggleAdventureStats);
            p.mTotalStats = new GameStats();
            p.mTotalStats.Add(mTotalStats);
            p.mNightsAdventureStats = new GameStats();
            p.mNightsAdventureStats.Add(mNightsAdventureStats);
            p.mBonusAdventures = new SortedDictionary<string, BonusAdventureInfo>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, BonusAdventureInfo> anEntry in mBonusAdventures)
            {
                p.mBonusAdventures[anEntry.Key] = anEntry.Value.Clone();
            }
            return p;
        }

        public void Reset(bool keepCollections)
        {
            mDirty = true;
            mSoundEnabled = true;
            mMusicEnabled = true;
            mComputerPlayer = true;
            mComputerSkill = -1;
            mDuelRounds = 0;
            mColorblind = false;
            mLeftHanded = false;
            mNeedShowStory = true;
            mFlag8f = false;
            mTipFlags = 0;
            mTotalTipCount = 0;
            mOtherTipCount = 0;
            Array.Clear(mTipCounts, 0, mTipCounts.Length);
            mMaxStage = 0;
            mMaxLevel = 0;
            mLastStageStage = -1;
            mLastStageLevel = 0;
            mCurCharacter = 0;
            mCurCharacter2 = 1;
            mUnused80 = "";
            mPlayer2Name = "";
            mLastQuickPlayLevel = 0;
            mPortraitsShown = 0;
            mTrophyWins = new List<int> { 0, 0, 0, 0 };
            mTrophyWinsDisplayed = new List<int> { 0, 0, 0, 0 };
            mByte180 = 0;
            mByte184 = 0;
            mAdventureWins = 0;
            mLeaveGameWarnings = 0;
            mLastQuickPlayStage = 0;
            mPeggleAdventureNumRetries = 0;
            mPeggleAdventureStats.Reset();
            mPeggleAdventureScore = 0;
            mPeggleAdventureLevelReplay = 0;
            mNeedUpdateAcedLevels = false;
            if (!keepCollections)
            {
                mPeggleTrophySet.Clear();
                mTrophyTopScores.Clear();
                mPeggleClearedLevelSet.Clear();
                mAdventureHighScores.Clear();
                mTotalStats.Reset();
                mNightsTrophySet.Clear();
                mNightsClearedLevelSet.Clear();
                mNightsAcedLevelSet.Clear();
                mPeggleAcedLevelSet.Clear();
                mShownAceAllStory = false;
                mInt340 = 0;
                mVersionString = SexyUtil.GetBundleShortVersionString();
                mWString348 = SexyAppBase.Loc("");
                mBonusClearedLevelSet.Clear();
                mBonusAcedLevelSet.Clear();
            }
            mBonusAdventures.Clear();
            mSeenDecals.Clear();
            mStoryShownLevel = 0;
            mBadgeCount = 0;
            mNightsMaxStage = 11;
            mNightsMaxLevel = 0;
            mNightsLastStageStage = 10;
            mNightsLastStageLevel = 0;
            mNightsAdventureWins = 0;
            mNightsAdventureScore = 0;
            mNightsAdventureLevelReplay = 0;
            mNightsAdventureNumRetries = 0;
            mNightsNeedShowStory = true;
            mLastTrophyWon = 0;
            mInt2b0 = 15;
            mNightsAdventureStats.Reset();
            mNightsTrialSeen = false;
            mNightsTrialIntroSeen = false;
        }

        public void DeleteSavedGames()
        {
            DeleteSavedGame(GameMode.GAMEMODE_ADVENTURE, -1);
            DeleteSavedGame(GameMode.GAMEMODE_ADVENTURE_NIGHTS, -1);
            foreach (BonusPage aPage in StageMgr.BONUS_PAGES)
            {
                if (aPage.HasAdventure)
                {
                    GlobalStaticVars.gSexyAppBase.EraseFile(GetBonusSavedGameName(aPage));
                }
            }
            DeleteSavedGame(GameMode.GAMEMODE_QUICKPLAY, -1);
            DeleteSavedGame(GameMode.GAMEMODE_CHALLENGE, 0);
            DeleteSavedGame(GameMode.GAMEMODE_CHALLENGE, 1);
            DeleteSavedGame(GameMode.GAMEMODE_DUEL, -1);
        }

        public void RestartAdventure()
        {
            SetmAdventureScore(0);
            SetmAdventureLevelReplay(0);
            SetmAdventureNumRetries(0);
            SetmLastStageStage(mFirstAdventureStage);
            SetmLastStageLevel(0);
            SetmNeedShowStory(true);
            GetmCurAdventureStats().Reset();
            mDirty = true;
        }

        public void SetmAdventureScore(int v)
        {
            if (Bonus != null)
            {
                CurBonus.mScore = v;
            }
            else if (IsNights)
            {
                mNightsAdventureScore = v;
            }
            else
            {
                mPeggleAdventureScore = v;
            }
        }

        public void SetmAdventureLevelReplay(int v)
        {
            if (Bonus != null)
            {
                CurBonus.mLevelReplay = v;
            }
            else if (IsNights)
            {
                mNightsAdventureLevelReplay = v;
            }
            else
            {
                mPeggleAdventureLevelReplay = v;
            }
        }

        public void SetmAdventureNumRetries(int v)
        {
            if (Bonus != null)
            {
                CurBonus.mNumRetries = v;
            }
            else if (IsNights)
            {
                mNightsAdventureNumRetries = v;
            }
            else
            {
                mPeggleAdventureNumRetries = v;
            }
        }

        public void SetmLastStageStage(int v)
        {
            if (Bonus != null)
            {
                CurBonus.mLastStageStage = v - Bonus.mFirstStage;
            }
            else if (IsNights)
            {
                mNightsLastStageStage = v;
            }
            else
            {
                mLastStageStage = v;
            }
        }

        public void SetmLastStageLevel(int v)
        {
            if (Bonus != null)
            {
                CurBonus.mLastStageLevel = v;
            }
            else if (IsNights)
            {
                mNightsLastStageLevel = v;
            }
            else
            {
                mLastStageLevel = v;
            }
        }

        public void SetmNeedShowStory(bool v)
        {
            if (Bonus != null)
            {
                CurBonus.mNeedShowStory = v;
            }
            else if (IsNights)
            {
                mNightsNeedShowStory = v;
            }
            else
            {
                mNeedShowStory = v;
            }
        }

        public GameStats GetmCurAdventureStats()
        {
            return Bonus != null ? CurBonus.mStats : IsNights ? mNightsAdventureStats : mPeggleAdventureStats;
        }

        public void SyncSummary(DataSync theSync)
        {
            theSync.SyncString(ref mName);
            theSync.SyncLong(ref mUseOrder);
            theSync.SyncLong(ref mId);
        }

        private static void SyncStringSet(DataSync s, SortedSet<string> set)
        {
            s.SyncSet(set, (ds, v) => ds.SyncString(v), () => "");
        }

        private static void SyncIntSet(DataSync s, ICollection<int> set)
        {
            s.SyncSet(set, (ds, v) => ds.SyncLong(v), () => 0);
        }

        public void SyncDetails(DataSync theSync)
        {
            if (theSync.IsReading)
            {
                Reset(false);
            }
            int version = DETAILS_VERSION;
            theSync.SyncLong(ref version);
            if (version <= 0x12 || version > DETAILS_VERSION)
            {
                return;
            }
            theSync.SyncLong(ref mMaxStage);
            theSync.SyncLong(ref mMaxLevel);
            theSync.SyncLong(ref mLastQuickPlayStage);
            theSync.SyncLong(ref mLastStageStage);
            theSync.SyncLong(ref mLastStageLevel);
            theSync.SyncLong(ref mCurCharacter);
            theSync.SyncLong(ref mCurCharacter2);
            theSync.SyncString(ref mPlayer2Name);
            theSync.SyncLong(ref mLastQuickPlayLevel);
            SyncIntSet(theSync, mPeggleTrophySet);
            // multiset<int>: kept sorted ascending
            theSync.SyncSet(mAdventureHighScores, (ds, v) => ds.SyncLong(v), () => 0);
            if (theSync.IsReading)
            {
                mAdventureHighScores.Sort();
            }
            theSync.SyncMap(mTrophyTopScores, (ds, k) => ds.SyncLong(k), (ds, v) => ds.SyncLong(v), () => 0, () => 0);
            theSync.SyncWString(ref mWideName);
            theSync.SyncBoolBit(ref mSoundEnabled);
            theSync.SyncBoolBit(ref mComputerPlayer);
            theSync.SyncBoolBit(ref mMusicEnabled);
            theSync.SyncBoolBit(ref mColorblind);
            theSync.SyncBoolBit(ref mNeedShowStory);
            theSync.SyncBoolBit(ref mFlag8f);
            theSync.SyncBoolBit(ref mLeftHanded);
            bool unused = true;
            theSync.SyncBoolBit(ref unused);
            theSync.EndBit();
            theSync.SyncLong(ref mComputerSkill);
            theSync.SyncByte(ref mDuelRounds);
            theSync.SyncByte(ref mByte180);
            theSync.SyncByte(ref mByte184);
            SyncStringSet(theSync, mPeggleClearedLevelSet);
            theSync.SyncLong(ref mAdventureWins);
            theSync.SyncByte(ref mLeaveGameWarnings);
            theSync.SyncLong(ref mTipFlags);
            theSync.SyncLong(ref mPeggleAdventureScore);
            theSync.SyncLong(ref mPeggleAdventureLevelReplay);
            theSync.SyncLong(ref mPeggleAdventureNumRetries);
            mPeggleAdventureStats.SyncState(theSync);
            mTotalStats.SyncState(theSync);
            if (version > 0x14)
            {
                theSync.SyncShort(ref mTotalTipCount);
                for (int i = 0; i < 10; i++)
                {
                    theSync.SyncByte(ref mTipCounts[i]);
                }
            }
            if (version > 0x15)
            {
                theSync.SyncShort(ref mOtherTipCount);
            }
            if (version > 0x16)
            {
                theSync.SyncShort(ref mStoryShownLevel);
            }
            if (version < 0x18)
            {
                mBadgeCount = mMaxStage;
            }
            else
            {
                theSync.SyncShort(ref mBadgeCount);
            }
            if (version < 0x19)
            {
                mPortraitsShown = 0;
                mTrophyWins = new List<int> { 0, 0, 0, 0 };
                mTrophyWinsDisplayed = new List<int> { 0, 0, 0, 0 };
                mPeggleAcedLevelSet.Clear();
                mNeedUpdateAcedLevels = true;
                mNightsClearedLevelSet.Clear();
                mNightsAcedLevelSet.Clear();
                mShownAceAllStory = false;
                mInt340 = 0;
                mVersionString = SexyUtil.GetBundleShortVersionString();
                mWString348 = SexyAppBase.Loc("");
            }
            else
            {
                theSync.SyncLong(ref mNightsMaxStage);
                theSync.SyncLong(ref mNightsMaxLevel);
                theSync.SyncLong(ref mNightsLastStageStage);
                theSync.SyncLong(ref mNightsLastStageLevel);
                theSync.SyncLong(ref mNightsAdventureWins);
                theSync.SyncLong(ref mNightsAdventureScore);
                theSync.SyncLong(ref mNightsAdventureLevelReplay);
                theSync.SyncLong(ref mNightsAdventureNumRetries);
                theSync.SyncBool(ref mNightsNeedShowStory);
                theSync.SyncLong(ref mInt2b0);
                theSync.SyncLong(ref mPortraitsShown);
                SyncIntSet(theSync, mNightsTrophySet);
                theSync.SyncIntList(mTrophyWins);
                theSync.SyncIntList(mTrophyWinsDisplayed);
                SyncStringSet(theSync, mPeggleAcedLevelSet);
                theSync.SyncBool(ref mNeedUpdateAcedLevels);
                SyncStringSet(theSync, mNightsClearedLevelSet);
                SyncStringSet(theSync, mNightsAcedLevelSet);
                theSync.SyncBool(ref mShownAceAllStory);
                theSync.SyncLong(ref mInt340);
                theSync.SyncWString(ref mVersionString);
                theSync.SyncWString(ref mWString348);
                theSync.SyncBool(ref mNightsTrialSeen);
                theSync.SyncBool(ref mNightsTrialIntroSeen);
                theSync.SyncLong(ref mLastTrophyWon);
            }
            if (version >= 0x1a)
            {
                if (version == 0x1a)
                {
                    // Pre-release 0x1a profiles also stored an Extreme unlock count, now unused.
                    int anUnused = 0;
                    theSync.SyncLong(ref anUnused);
                }
                SyncStringSet(theSync, mBonusClearedLevelSet);
                SyncStringSet(theSync, mBonusAcedLevelSet);
            }
            if (version >= 0x1c)
            {
                int aCount = mBonusAdventures.Count;
                theSync.SyncLong(ref aCount);
                if (theSync.IsReading)
                {
                    for (int i = 0; i < aCount; i++)
                    {
                        string aKey = "";
                        theSync.SyncString(ref aKey);
                        BonusAdventureInfo anInfo = new BonusAdventureInfo();
                        anInfo.SyncState(theSync);
                        mBonusAdventures[aKey] = anInfo;
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, BonusAdventureInfo> anEntry in mBonusAdventures)
                    {
                        string aKey = anEntry.Key;
                        theSync.SyncString(ref aKey);
                        anEntry.Value.SyncState(theSync);
                    }
                }
            }
            if (version >= 0x1d)
            {
                SyncStringSet(theSync, mSeenDecals);
            }
        }

        private string DetailsFileName => SexyUtil.GetDocumentsDir() + "userdata/user" + mId + ".dat";

        public void LoadDetails()
        {
            Buffer buffer = new Buffer();
            if (!GlobalStaticVars.gSexyAppBase.ReadBufferFromFile(DetailsFileName, ref buffer, false))
            {
                return;
            }
            try
            {
                DataReader reader = new DataReader();
                reader.OpenMemory(buffer.GetDataPtr(), buffer.GetDataLen(), false);
                SyncDetails(new DataSync(reader));
            }
            catch (DataReaderException)
            {
                Reset(false);
            }
        }

        public void SaveDetails()
        {
            mDirty = false;
            DataWriter writer = new DataWriter();
            writer.OpenMemory(0x20);
            SyncDetails(new DataSync(writer));
            Common.MkDir(SexyUtil.GetDocumentsDir() + "userdata");
            GlobalStaticVars.gSexyAppBase.WriteBytesToFile(DetailsFileName, writer.GetDataPtr(), writer.GetDataLen());
        }

        public void SaveIfDirty()
        {
            if (mDirty)
            {
                SaveDetails();
            }
        }

        public string GetSavedGameName(int theGameMode, int theLevelPack)
        {
            string dir = SexyUtil.GetDocumentsDir();
            if (Bonus != null && (theGameMode == GameMode.GAMEMODE_ADVENTURE || theGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS))
            {
                // Port addition: each bonus Adventure keeps its own saved game.
                return GetBonusSavedGameName(Bonus);
            }
            switch (theGameMode)
            {
            case GameMode.GAMEMODE_ADVENTURE:
                return dir + "userdata/stage" + mId + ".sav";
            case GameMode.GAMEMODE_QUICKPLAY:
                return dir + "userdata/arcade" + mId + ".sav";
            case GameMode.GAMEMODE_DUEL:
                return dir + "userdata/duel" + mId + ".sav";
            case GameMode.GAMEMODE_CHALLENGE:
                return dir + (theLevelPack == 1 ? "userdata/trophynights" : "userdata/trophy") + mId + ".sav";
            case GameMode.GAMEMODE_ADVENTURE_NIGHTS:
                return dir + "userdata/stagenights" + mId + ".sav";
            default:
                return "";
            }
        }

        /// <summary>Port addition: a bonus Adventure's saved game, e.g. "userdata/stageextreme1.sav".</summary>
        public string GetBonusSavedGameName(BonusPage thePage)
        {
            return SexyUtil.GetDocumentsDir() + "userdata/stage" + thePage.mAdventureKey + mId + ".sav";
        }

        public void DeleteUserFiles()
        {
            GlobalStaticVars.gSexyAppBase.EraseFile(DetailsFileName);
            DeleteSavedGames();
        }

        public void DeleteSavedGame(int theGameMode, int theLevelPack)
        {
            string name = GetSavedGameName(theGameMode, theLevelPack);
            if (!string.IsNullOrEmpty(name))
            {
                GlobalStaticVars.gSexyAppBase.EraseFile(name);
            }
        }

        /// <summary>Port debug aid (not in the original): SEXY_UNLOCK_ALL=1 opens every level for testing.</summary>
        public static readonly bool gDebugUnlockAll = System.Environment.GetEnvironmentVariable("SEXY_UNLOCK_ALL") == "1";

        public bool IsLevelUnlocked(int theStage, int theLevel)
        {
            if (gDebugUnlockAll)
            {
                return true;
            }
            if (StageMgr.IsBonusStage(theStage))
            {
                // Port addition: a bonus page with its own Adventure (Extreme, WoW) unlocks as that
                // Adventure is played, like Peggle's and Nights' levels; the others are always open.
                BonusPage aPage = StageMgr.GetBonusPage(theStage);
                if (aPage == null || !aPage.HasAdventure)
                {
                    return true;
                }
                BonusAdventureInfo anInfo = GetBonusAdventure(aPage);
                int aStage = theStage - aPage.mFirstStage;
                if (anInfo.mMaxStage != aStage)
                {
                    return aStage < anInfo.mMaxStage;
                }
                return theLevel < anInfo.mMaxLevel;
            }
            if (theStage < 11)
            {
                if (mMaxStage < theStage)
                {
                    return false;
                }
                if (theStage < mMaxStage)
                {
                    return true;
                }
                return theLevel < mMaxLevel;
            }
            if (mNightsMaxStage < theStage)
            {
                return false;
            }
            if (theStage < mNightsMaxStage)
            {
                return true;
            }
            return theLevel < mNightsMaxLevel;
        }

        public void SetPlayer2Name(string theName)
        {
            if (mPlayer2Name != theName)
            {
                mPlayer2Name = theName;
                mDirty = true;
            }
        }

        public void SetColorblind(bool v)
        {
            if (v != mColorblind)
            {
                mColorblind = v;
                mDirty = true;
            }
        }

        /// <summary>Port addition: marks the decal seen; true (and dirty) if it wasn't yet (Peggle WoW vf70's new-decal check).</summary>
        public bool SeeDecal(string theName)
        {
            if (!mSeenDecals.Add(theName))
            {
                return false;
            }
            mDirty = true;
            return true;
        }

        public void SetLeftHanded(bool v)
        {
            if (v != mLeftHanded)
            {
                mLeftHanded = v;
                mDirty = true;
            }
        }

        public void SetSelCharacter(int theChar)
        {
            if (mCurCharacter != theChar)
            {
                mCurCharacter = theChar;
                mDirty = true;
            }
        }

        public void SetSelCharacters(int theChar1, int theChar2)
        {
            SetSelCharacter(theChar1);
            if (mCurCharacter2 != theChar2)
            {
                mCurCharacter2 = theChar2;
                mDirty = true;
            }
        }

        public void SetComputerPlayer(bool v)
        {
            if (mComputerPlayer != v)
            {
                mComputerPlayer = v;
                mDirty = true;
            }
        }

        public void SetComputerSkill(int v)
        {
            if (mComputerSkill != v)
            {
                mComputerSkill = v;
                mDirty = true;
            }
        }

        public void CheckTrophyTopScore(int theTrophyId, int theScore, bool lowerIsBetter)
        {
            if (theScore <= 0)
            {
                return;
            }
            if (!mTrophyTopScores.TryGetValue(theTrophyId, out int current))
            {
                mTrophyTopScores[theTrophyId] = theScore;
                current = theScore;
            }
            if ((lowerIsBetter && theScore < current) || (!lowerIsBetter && current < theScore))
            {
                mTrophyTopScores[theTrophyId] = theScore;
                mDirty = true;
            }
        }

        public int GetTrophyTopScore(int theTrophyId)
        {
            return mTrophyTopScores.TryGetValue(theTrophyId, out int v) ? v : 0;
        }

        public void IncTip(int theTip)
        {
            mTotalTipCount++;
            if (theTip < 0 || theTip > 9)
            {
                mOtherTipCount++;
            }
            else
            {
                mTipCounts[theTip]++;
            }
            mDirty = true;
        }

        public int GetMaxLevel()
        {
            return GetmMaxStage() * 5 + GetmMaxLevel();
        }

        public int GetmMaxStage()
        {
            return Bonus != null ? Bonus.mFirstStage + CurBonus.mMaxStage : IsNights ? mNightsMaxStage : mMaxStage;
        }

        public int GetmMaxLevel()
        {
            return Bonus != null ? CurBonus.mMaxLevel : IsNights ? mNightsMaxLevel : mMaxLevel;
        }

        public int GetAdventureEfficiencyBonus()
        {
            int bonus = GetmAdventureNumRetries() * -200000 + 10000000;
            return bonus < 100000 ? 100000 : bonus;
        }

        public int GetmAdventureNumRetries()
        {
            return Bonus != null ? CurBonus.mNumRetries : IsNights ? mNightsAdventureNumRetries : mPeggleAdventureNumRetries;
        }

        public void AwardTrophy(int theTrophyId)
        {
            if (GetmTrophySet().Add(theTrophyId))
            {
                mDirty = true;
            }
        }

        public SortedSet<int> GetmTrophySet()
        {
            return IsNights ? mNightsTrophySet : mPeggleTrophySet;
        }

        /// <summary>The trophy set for the current level pack (as used by ThunderballApp).</summary>
        public SortedSet<int> mTrophySet => GetmTrophySet();

        public SortedSet<string> mClearedLevelSet => GetmClearedLevelSet();

        public SortedSet<string> mAcedLevelSet => GetmAcedLevelSet();

        public void AddTotalStats(GameStats theStats, int theGameMode)
        {
            if (theGameMode == GameMode.GAMEMODE_ADVENTURE || theGameMode == GameMode.GAMEMODE_ADVENTURE_NIGHTS)
            {
                GetmCurAdventureStats().Add(theStats);
            }
            mTotalStats.Add(theStats);
            mDirty = true;
        }

        public bool JustBeatAdventure()
        {
            return GetmAdventureWinCount() > 0 && GetmLastStageStage() == mLastAdventureStage + 1 && GetmLastStageLevel() == 0;
        }

        public int GetmAdventureWinCount()
        {
            return Bonus != null ? CurBonus.mWins : IsNights ? mNightsAdventureWins : mAdventureWins;
        }

        public int GetmLastStageStage()
        {
            return Bonus != null ? Bonus.mFirstStage + CurBonus.mLastStageStage : IsNights ? mNightsLastStageStage : mLastStageStage;
        }

        public int GetmLastStageLevel()
        {
            return Bonus != null ? CurBonus.mLastStageLevel : IsNights ? mNightsLastStageLevel : mLastStageLevel;
        }

        /// <summary>Awards the four Peggle Nights trophies (adventure, 60 trophies, 60 cleared, 60 aced).</summary>
        public int RefreshTrophyWins()
        {
            int newWins = 0;
            if (mTrophyWins[0] == 0 && mNightsAdventureWins > 0)
            {
                mTrophyWins[0] = 1;
                newWins = 1;
                mLastTrophyWon = 0;
            }
            if (mTrophyWins[1] == 0 && mNightsTrophySet.Count > 0x3b)
            {
                mTrophyWins[1] = 1;
                newWins++;
                mLastTrophyWon = 1;
            }
            if (mTrophyWins[3] == 0 && mNightsAcedLevelSet.Count > 0x3b)
            {
                mTrophyWins[3] = 1;
                newWins++;
                mLastTrophyWon = 3;
            }
            if (mTrophyWins[2] == 0 && mNightsClearedLevelSet.Count > 0x3b)
            {
                mTrophyWins[2] = 1;
                newWins++;
                mLastTrophyWon = 2;
            }
            if (newWins != 0)
            {
                SaveDetails();
            }
            return newWins;
        }

        public static Image TrophyToImage(int theTrophy)
        {
            switch (theTrophy)
            {
            case 0:
                return Res.IMAGE_TR_NIGHTS_ADV;
            case 1:
                return Res.IMAGE_TR_NIGHTS_MASTER3;
            case 2:
                return Res.IMAGE_TR_NIGHTS_MASTER2;
            case 3:
                return Res.IMAGE_TR_NIGHTS_MASTER1;
            default:
                return null;
            }
        }

        public int GetNextTrophyToDisplay()
        {
            for (int i = 0; i < mTrophyWins.Count; i++)
            {
                if (mTrophyWins[i] != 0 && mTrophyWinsDisplayed[i] == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetNumTrophiesWon(bool displayed)
        {
            int count = 0;
            for (int i = 0; i < mTrophyWins.Count; i++)
            {
                if ((displayed ? mTrophyWinsDisplayed[i] : mTrophyWins[i]) != 0)
                {
                    count++;
                }
            }
            return count;
        }

        public int GetmAdventureLevelReplay()
        {
            return Bonus != null ? CurBonus.mLevelReplay : IsNights ? mNightsAdventureLevelReplay : mPeggleAdventureLevelReplay;
        }

        public void SubmitAdventureHighScore(int theScore)
        {
            int idx = mAdventureHighScores.BinarySearch(theScore);
            if (idx < 0)
            {
                idx = ~idx;
            }
            else
            {
                while (idx < mAdventureHighScores.Count && mAdventureHighScores[idx] == theScore)
                {
                    idx++;
                }
            }
            mAdventureHighScores.Insert(idx, theScore);
            if (mAdventureHighScores.Count < 11)
            {
                mDirty = true;
            }
            else
            {
                if (mAdventureHighScores[0] != theScore)
                {
                    mDirty = true;
                }
                mAdventureHighScores.RemoveAt(0);
            }
        }

        /// <returns>true if the tip had not been shown before.</returns>
        public bool SetTip(int theTip)
        {
            uint bit = 1u << (theTip & 0xff);
            bool isNew = (mTipFlags & bit) == 0;
            if (isNew)
            {
                mTipFlags |= bit;
                mDirty = true;
            }
            return isNew;
        }

        public bool IsTipSet(int theTip)
        {
            return (mTipFlags & (1u << (theTip & 0xff))) != 0;
        }

        public void SetBadgeCount(int theCount)
        {
            mBadgeCount = theCount;
            mDirty = true;
        }

        public void SetmMaxStage(int v)
        {
            if (Bonus != null)
            {
                CurBonus.mMaxStage = v - Bonus.mFirstStage;
            }
            else if (IsNights)
            {
                mNightsMaxStage = v;
            }
            else
            {
                mMaxStage = v;
            }
        }

        public void SetmMaxLevel(int v)
        {
            if (Bonus != null)
            {
                CurBonus.mMaxLevel = v;
            }
            else if (IsNights)
            {
                mNightsMaxLevel = v;
            }
            else
            {
                mMaxLevel = v;
            }
        }

        public void SetmAdventureWinCount(int v)
        {
            if (Bonus != null)
            {
                CurBonus.mWins = v;
            }
            else if (IsNights)
            {
                mNightsAdventureWins = v;
            }
            else
            {
                mAdventureWins = v;
            }
        }

        public int GetmAdventureScore()
        {
            return Bonus != null ? CurBonus.mScore : IsNights ? mNightsAdventureScore : mPeggleAdventureScore;
        }

        public bool GetmNeedShowStory()
        {
            return Bonus != null ? CurBonus.mNeedShowStory : IsNights ? mNightsNeedShowStory : mNeedShowStory;
        }

        public SortedSet<string> GetmClearedLevelSet()
        {
            return IsNights ? mNightsClearedLevelSet : mPeggleClearedLevelSet;
        }

        public SortedSet<string> GetmAcedLevelSet()
        {
            return IsNights ? mNightsAcedLevelSet : mPeggleAcedLevelSet;
        }

        /// <summary>Port addition: the cleared set a level belongs to (the bonus levels have their own).</summary>
        public SortedSet<string> GetClearedLevelSet(string theLevel, StageMgr theStageMgr)
        {
            return theStageMgr.IsBonusLevel(theLevel) ? mBonusClearedLevelSet : GetmClearedLevelSet();
        }

        public SortedSet<string> GetAcedLevelSet(string theLevel, StageMgr theStageMgr)
        {
            return theStageMgr.IsBonusLevel(theLevel) ? mBonusAcedLevelSet : GetmAcedLevelSet();
        }

        /// <summary>
        /// One-time migration for old profiles: marks Peggle levels as aced when the player's top
        /// high score beats the level's ace score.
        /// </summary>
        public void UpdatePeggleAcedLevels(ThunderballApp theApp)
        {
            if (!mNeedUpdateAcedLevels)
            {
                return;
            }
            mNeedUpdateAcedLevels = false;
            for (int stage = 0; stage < 11; stage++)
            {
                StageInfo stageInfo = theApp.mStageMgr.GetStageInfo(stage);
                for (int level = 0; level < 5; level++)
                {
                    LevelInfo levelInfo = stageInfo?.GetLevelInfo(level);
                    if (levelInfo == null)
                    {
                        continue;
                    }
                    List<HighScoreEntry> scores = theApp.mHighScoreMgr.GetScores(levelInfo.mName, false, levelInfo.mAceScore);
                    if (scores.Count > 0 && levelInfo.mAceScore < scores[0].mScore && mPeggleAcedLevelSet.Add(levelInfo.mName))
                    {
                        mDirty = true;
                    }
                }
            }
        }

        public bool CheckCharacterUnlocked(int theCharacter)
        {
            if (gDebugUnlockAll)
            {
                return true;
            }
            for (int stage = 0; stage <= mMaxStage; stage++)
            {
                if (SexyUtil.GetCharacterForStage(stage, 0) == theCharacter)
                {
                    return true;
                }
            }
            for (int stage = 0; stage <= mNightsMaxStage - 11; stage++)
            {
                if (SexyUtil.GetCharacterForStage(stage, 1) == theCharacter)
                {
                    return true;
                }
            }
            // Port addition: the masters reached in the WoW Adventure (Splork) are unlocked everywhere too.
            foreach (BonusPage aPage in StageMgr.BONUS_PAGES)
            {
                if (aPage.mAdventureKey == StageMgr.ADVENTURE_WOW && aPage.mStageCharacters != null &&
                    mBonusAdventures.TryGetValue(aPage.mAdventureKey, out BonusAdventureInfo anInfo))
                {
                    for (int stage = 0; stage <= anInfo.mMaxStage && stage < aPage.mStageCharacters.Length; stage++)
                    {
                        if (aPage.mStageCharacters[stage] == theCharacter)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Port addition: a bonus Adventure's progress (Peggle Extreme, Peggle WoW), the fields Peggle and
    /// Nights keep in PlayerInfo. Stages count from the Adventure's first (BonusPage.mFirstStage), so
    /// saves don't depend on where the page's stages land.
    /// </summary>
    public class BonusAdventureInfo
    {
        public int mMaxStage;

        public int mMaxLevel;

        public int mLastStageStage = -1;

        public int mLastStageLevel;

        public int mWins;

        public int mScore;

        public int mLevelReplay;

        public int mNumRetries;

        public bool mNeedShowStory = true;

        public GameStats mStats = new GameStats();

        public BonusAdventureInfo Clone()
        {
            BonusAdventureInfo anInfo = (BonusAdventureInfo)MemberwiseClone();
            anInfo.mStats = new GameStats();
            anInfo.mStats.Add(mStats);
            return anInfo;
        }

        public void SyncState(DataSync theSync)
        {
            theSync.SyncLong(ref mMaxStage);
            theSync.SyncLong(ref mMaxLevel);
            theSync.SyncLong(ref mLastStageStage);
            theSync.SyncLong(ref mLastStageLevel);
            theSync.SyncLong(ref mWins);
            theSync.SyncLong(ref mScore);
            theSync.SyncLong(ref mLevelReplay);
            theSync.SyncLong(ref mNumRetries);
            theSync.SyncBool(ref mNeedShowStory);
            mStats.SyncState(theSync);
        }
    }
}
