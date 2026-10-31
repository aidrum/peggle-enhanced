using System;
using System.Collections.Generic;
using System.IO;

namespace Sexy
{
    public class LevelStats
    {
        public int mNumWins;                                      // 0x00

        public int mNumLosses;                                    // 0x04

        public int mWinScore;                                     // 0x08

        public int mLossScore;                                    // 0x0c

        public int[] mCounts = new int[4];                        // 0x10

        public int mLossPegsLeft;                                 // 0x20

        public uint mBestScore;                                   // 0x24

        public int mBestScoreCharacter;                           // 0x28

        public void Clear()
        {
            mNumWins = 0;
            mNumLosses = 0;
            mWinScore = 0;
            mLossScore = 0;
            mLossPegsLeft = 0;
            mBestScore = 0;
            mBestScoreCharacter = 0;
            Array.Clear(mCounts, 0, 4);
        }

        public void SyncState(DataSync theSync)
        {
            theSync.SyncLong(ref mNumWins);
            theSync.SyncLong(ref mNumLosses);
            theSync.SyncLong(ref mWinScore);
            theSync.SyncLong(ref mLossScore);
            theSync.SyncLong(ref mLossPegsLeft);
            theSync.SyncLong(ref mBestScore);
            theSync.SyncLong(ref mBestScoreCharacter);
            for (int i = 0; i < 4; i++)
            {
                theSync.SyncLong(ref mCounts[i]);
            }
        }

        public void Add(LevelStats theStats)
        {
            mNumWins += theStats.mNumWins;
            mNumLosses += theStats.mNumLosses;
            mWinScore += theStats.mWinScore;
            mLossScore += theStats.mLossScore;
            mLossPegsLeft += theStats.mLossPegsLeft;
            for (int i = 0; i < 4; i++)
            {
                mCounts[i] += theStats.mCounts[i];
            }
            if (mBestScore < theStats.mBestScore)
            {
                mBestScore = theStats.mBestScore;
                mBestScoreCharacter = theStats.mBestScoreCharacter;
            }
        }
    }

    public class TrophyStats
    {
        public int mNumWins;                                      // 0x0

        public int mNumLosses;                                    // 0x4

        public void SyncState(DataSync theSync)
        {
            theSync.SyncLong(ref mNumWins);
            theSync.SyncLong(ref mNumLosses);
        }
    }

    /// <summary>
    /// Anonymous play statistics (Sexy::StatsMgr) kept in userdata/stat_&lt;name&gt;_&lt;time&gt;.dat:
    /// per-level and per-trophy counters and character usage/win/loss counts.
    /// </summary>
    public class StatsMgr
    {
        public SortedDictionary<string, LevelStats> mLevelStats = new SortedDictionary<string, LevelStats>(StringComparer.OrdinalIgnoreCase);// 0x000

        public SortedDictionary<string, LevelStats> mStageLevelStats = new SortedDictionary<string, LevelStats>(StringComparer.OrdinalIgnoreCase);// 0x018

        public string mPlayerName = "";                           // 0x030

        public uint mStartTime;                                   // 0x034

        public int[] mCharacterCount = new int[12];               // 0x038

        public int[] mTrophyCharacterCount = new int[12];         // 0x068

        public int[] mCharacterWinCount = new int[16];            // 0x098

        public int[] mStageCharacterWinCount = new int[16];       // 0x0d8

        public int[] mCharacterLossCount = new int[16];           // 0x118

        public int[] mStageCharacterLossCount = new int[16];      // 0x158

        public int mNumGames;                                     // 0x198

        public SortedDictionary<int, TrophyStats> mTrophyStats = new SortedDictionary<int, TrophyStats>();// 0x19c

        public StatsMgr()
        {
            Clear();
        }

        public void Clear()
        {
            mLevelStats.Clear();
            mStageLevelStats.Clear();
            mStartTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            mPlayerName = "";
            mNumGames = 0;
            Array.Clear(mCharacterCount, 0, 12);
            Array.Clear(mTrophyCharacterCount, 0, 12);
            Array.Clear(mCharacterWinCount, 0, 16);
            Array.Clear(mCharacterLossCount, 0, 16);
            Array.Clear(mStageCharacterWinCount, 0, 16);
            Array.Clear(mStageCharacterLossCount, 0, 16);
        }

        private static void SyncLevelMap(DataSync theSync, SortedDictionary<string, LevelStats> theMap)
        {
            theSync.SyncMap(theMap, (ds, k) => ds.SyncString(k), (ds, v) =>
            {
                v.SyncState(ds);
                return v;
            }, () => "", () => new LevelStats());
        }

        public void SyncState(DataSync theSync)
        {
            int version = 3;
            theSync.SyncLong(ref version);
            theSync.SetVersion(version);
            theSync.SyncLong(ref mStartTime);
            theSync.SyncString(ref mPlayerName);
            theSync.SyncLong(ref mNumGames);
            for (int i = 0; i < 12; i++)
            {
                theSync.SyncLong(ref mCharacterCount[i]);
            }
            for (int i = 0; i < 16; i++)
            {
                theSync.SyncLong(ref mCharacterWinCount[i]);
                theSync.SyncLong(ref mCharacterLossCount[i]);
            }
            SyncLevelMap(theSync, mLevelStats);
            if (theSync.GetVersion() > 1)
            {
                theSync.SyncMap(mTrophyStats, (ds, k) => ds.SyncLong(k), (ds, v) =>
                {
                    v.SyncState(ds);
                    return v;
                }, () => 0, () => new TrophyStats());
            }
            if (theSync.GetVersion() > 2)
            {
                for (int i = 0; i < 12; i++)
                {
                    theSync.SyncLong(ref mTrophyCharacterCount[i]);
                }
                for (int i = 0; i < 16; i++)
                {
                    theSync.SyncLong(ref mStageCharacterWinCount[i]);
                    theSync.SyncLong(ref mStageCharacterLossCount[i]);
                }
                SyncLevelMap(theSync, mStageLevelStats);
            }
        }

        /// <summary>Loads theFileName, or with null the first userdata/stat_*.dat found.</summary>
        public bool Load(string theFileName)
        {
            Clear();
            string fileName = theFileName;
            if (fileName == null)
            {
                string dir = Path.Combine(GlobalStaticVars.gSexyAppBase.applicationStoragePath, SexyUtil.GetDocumentsDir() + "userdata");
                string[] found = Directory.Exists(dir) ? Directory.GetFiles(dir, "stat_*.dat") : Array.Empty<string>();
                if (found.Length == 0)
                {
                    return false;
                }
                fileName = SexyUtil.GetDocumentsDir() + "userdata/" + Path.GetFileName(found[0]);
            }
            Buffer buffer = new Buffer();
            if (!GlobalStaticVars.gSexyAppBase.ReadBufferFromFile(fileName, ref buffer, false))
            {
                return false;
            }
            try
            {
                DataReader reader = new DataReader();
                reader.OpenMemory(buffer.GetDataPtr(), buffer.GetDataLen(), false);
                DataSync sync = new DataSync(reader);
                SyncState(sync);
                sync.SyncPointers();
            }
            catch (DataReaderException)
            {
                Clear();
                return false;
            }
            return true;
        }

        public bool Save(string theFileName)
        {
            Common.MkDir(SexyUtil.GetDocumentsDir() + "userdata");
            string fileName = theFileName;
            if (fileName == null)
            {
                string name = "";
                foreach (char c in mPlayerName)
                {
                    if (c < 0x80 && char.IsLetterOrDigit(c))
                    {
                        name += c;
                    }
                }
                fileName = SexyUtil.GetDocumentsDir() + CFormat.StrFormat("userdata/stat_%s_%x.dat", name, mStartTime);
            }
            DataWriter writer = new DataWriter();
            writer.OpenMemory(0x20);
            DataSync sync = new DataSync(writer);
            SyncState(sync);
            sync.SyncPointers();
            return GlobalStaticVars.gSexyAppBase.WriteBytesToFile(fileName, writer.GetDataPtr(), writer.GetDataLen());
        }

        public LevelStats GetStatsForLevel(string theLevel)
        {
            if (!mLevelStats.TryGetValue(theLevel, out LevelStats stats))
            {
                stats = new LevelStats();
                mLevelStats[theLevel] = stats;
            }
            return stats;
        }

        public LevelStats GetStageStatsForLevel(string theLevel)
        {
            if (!mStageLevelStats.TryGetValue(theLevel, out LevelStats stats))
            {
                stats = new LevelStats();
                mStageLevelStats[theLevel] = stats;
            }
            return stats;
        }

        public TrophyStats GetStatsForTrophy(int theTrophy)
        {
            if (!mTrophyStats.TryGetValue(theTrophy, out TrophyStats stats))
            {
                stats = new TrophyStats();
                mTrophyStats[theTrophy] = stats;
            }
            return stats;
        }

        public void IncCharacterCount(int theChar)
        {
            if (theChar >= 0 && theChar < 12)
            {
                mCharacterCount[theChar]++;
            }
        }

        public void IncTrophyCharacterCount(int theChar)
        {
            if (theChar >= 0 && theChar < 12)
            {
                mTrophyCharacterCount[theChar]++;
            }
        }

        public void IncCharacterWinCount(int theChar)
        {
            if (theChar >= 0 && theChar < 16)
            {
                mCharacterWinCount[theChar]++;
            }
        }

        public void IncCharacterLossCount(int theChar)
        {
            if (theChar >= 0 && theChar < 16)
            {
                mCharacterLossCount[theChar]++;
            }
        }

        public void IncStageCharacterWinCount(int theChar)
        {
            if (theChar >= 0 && theChar < 16)
            {
                mStageCharacterWinCount[theChar]++;
            }
        }

        public void IncStageCharacterLossCount(int theChar)
        {
            if (theChar >= 0 && theChar < 16)
            {
                mStageCharacterLossCount[theChar]++;
            }
        }
    }
}
