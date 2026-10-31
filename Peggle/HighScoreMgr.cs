using System;
using System.Collections.Generic;

namespace Sexy
{
    public class HighScoreEntry
    {
        public string mName = "";                                 // 0x0

        public int mScore;                                        // 0x4

        public bool mIsNew;                                       // 0x8

        public void SyncState(DataSync theSync)
        {
            theSync.SyncString(ref mName);
            theSync.SyncLong(ref mScore);
        }
    }

    /// <summary>
    /// Per-level top-10 tables (Sexy::HighScoreMgr), one map for Peggle and one for Nights, saved
    /// in userdata/highscores.dat. Empty tables are filled with made-up default scores.
    /// </summary>
    public class HighScoreMgr
    {
        private static readonly string[] names =
        {
            "Andy", "Anthony", "Ben", "Bill", "Brian", "Amanda", "Chad", "Dave", "Del", "Ed", "Jimmy Goggle",
            "Eric", "Hans", "Isaac", "Jason", "James", "Jeff", "Joe", "John", "Josh", "Megan", "Kathy",
            "Dan", "Mark", "Matt", "Max", "Shawn", "Stephen", "Sukhbir", "Tysen", "Walter", "Wes"
        };

        private static readonly Random sRandom = new Random();

        public SortedDictionary<string, List<HighScoreEntry>> mScoreMap = new SortedDictionary<string, List<HighScoreEntry>>(StringComparer.OrdinalIgnoreCase); // 0x04

        public SortedDictionary<string, List<HighScoreEntry>> mNightsScoreMap = new SortedDictionary<string, List<HighScoreEntry>>(StringComparer.OrdinalIgnoreCase); // 0x1c

        private static string FileName => SexyUtil.GetDocumentsDir() + "userdata/highscores.dat";

        private static void SyncScoreMap(DataSync theSync, SortedDictionary<string, List<HighScoreEntry>> theMap)
        {
            theSync.SyncMap(theMap, (ds, k) => ds.SyncString(k), (ds, list) =>
            {
                ds.SyncList(list, (ds2, e) =>
                {
                    e.SyncState(ds2);
                    return e;
                }, () => new HighScoreEntry());
                return list;
            }, () => "", () => new List<HighScoreEntry>());
        }

        public void SyncState(DataSync theSync)
        {
            int version = 3;
            theSync.SyncLong(ref version);
            theSync.SetVersion(version);
            if (version > 1)
            {
                SyncScoreMap(theSync, mScoreMap);
            }
            if (version > 2)
            {
                SyncScoreMap(theSync, mNightsScoreMap);
            }
        }

        public bool Load()
        {
            mScoreMap.Clear();
            mNightsScoreMap.Clear();
            Buffer buffer = new Buffer();
            if (!GlobalStaticVars.gSexyAppBase.ReadBufferFromFile(FileName, ref buffer, false))
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
                mScoreMap.Clear();
                mNightsScoreMap.Clear();
            }
            return true;
        }

        public bool Save()
        {
            Common.MkDir(SexyUtil.GetDocumentsDir() + "userdata");
            DataWriter writer = new DataWriter();
            writer.OpenMemory(0x20);
            DataSync sync = new DataSync(writer);
            SyncState(sync);
            sync.SyncPointers();
            return GlobalStaticVars.gSexyAppBase.WriteBytesToFile(FileName, writer.GetDataPtr(), writer.GetDataLen());
        }

        public void CreateDefHighScores(List<HighScoreEntry> theList, string theLevelName, int theAceScore)
        {
            bool adventure = theLevelName == "_adventure";
            theList.Clear();
            int score = 300000;
            if (theAceScore == -1)
            {
                if (adventure)
                {
                    score = 20000000;
                }
            }
            else
            {
                score = theAceScore;
            }
            for (int i = 0; i < 10; i++)
            {
                HighScoreEntry entry = new HighScoreEntry();
                entry.mName = names[sRandom.Next() % 0x20];
                entry.mScore = score;
                theList.Add(entry);
                if (theAceScore == -1)
                {
                    if (!adventure)
                    {
                        score -= 10000;
                    }
                    else
                    {
                        score -= i == 0 ? 3000000 : 1500000;
                    }
                }
                else if (score >= 400000)
                {
                    score = Math.Max(score - (i == 0 ? 100000 : 25000), 50000);
                }
                else if (score >= 300000)
                {
                    score = Math.Max(score - (i == 0 ? 75000 : 25000), 50000);
                }
                else
                {
                    score = Math.Max(score - (i == 0 ? 50000 : 25000), 50000);
                }
            }
        }

        public SortedDictionary<string, List<HighScoreEntry>> GetScoreMap()
        {
            return ThunderballApp.mGameLevelPack == 1 ? mNightsScoreMap : mScoreMap;
        }

        private List<HighScoreEntry> GetList(string theLevelName)
        {
            SortedDictionary<string, List<HighScoreEntry>> map = GetScoreMap();
            if (!map.TryGetValue(theLevelName, out List<HighScoreEntry> list))
            {
                list = new List<HighScoreEntry>();
                map[theLevelName] = list;
            }
            return list;
        }

        /// <summary>
        /// Returns true if theScore makes the table; when doSubmit is set the entry is inserted
        /// (marked new) and the file saved. reloadFirst re-reads the file before submitting.
        /// </summary>
        public bool Submit(string theLevelName, string thePlayerName, int theScore, bool doSubmit, bool reloadFirst, int theAceScore)
        {
            if (theScore < 1)
            {
                return false;
            }
            List<HighScoreEntry> list = GetList(theLevelName);
            if (list.Count == 0)
            {
                CreateDefHighScores(list, theLevelName, theAceScore);
            }
            foreach (HighScoreEntry e in list)
            {
                e.mIsNew = false;
            }
            int pos = 0;
            while (pos < list.Count && list[pos].mScore >= theScore)
            {
                pos++;
            }
            if (pos == list.Count && list.Count > 9)
            {
                return false;
            }
            if (doSubmit)
            {
                if (reloadFirst)
                {
                    Load();
                    return Submit(theLevelName, thePlayerName, theScore, true, false, theAceScore);
                }
                HighScoreEntry entry = new HighScoreEntry();
                entry.mName = thePlayerName;
                entry.mScore = theScore;
                entry.mIsNew = true;
                list.Insert(pos, entry);
                while (list.Count > 10)
                {
                    list.RemoveAt(list.Count - 1);
                }
                Save();
            }
            return true;
        }

        /// <summary>The table for a level (created with defaults if missing); null if requireNew and nothing is new.</summary>
        public List<HighScoreEntry> GetScores(string theLevelName, bool requireNew, int theAceScore)
        {
            if (string.IsNullOrEmpty(theLevelName))
            {
                return null;
            }
            List<HighScoreEntry> list = GetList(theLevelName);
            if (list.Count == 0)
            {
                CreateDefHighScores(list, theLevelName, theAceScore);
            }
            if (requireNew && !list.Exists(e => e.mIsNew))
            {
                return null;
            }
            return list;
        }
    }

    /// <summary>Caches images loaded by file name and owns images queued for deletion (Sexy::ImageMgr).</summary>
    public class ImageMgr
    {
        public SortedDictionary<string, Image> mImageMap = new SortedDictionary<string, Image>(StringComparer.OrdinalIgnoreCase); // 0x04

        public HashSet<Image> mDeleteSet = new HashSet<Image>();  // 0x1c

        public void Clear()
        {
            foreach (Image image in mImageMap.Values)
            {
                image?.Dispose();
            }
            foreach (Image image in mDeleteSet)
            {
                image?.Dispose();
            }
            mImageMap.Clear();
            mDeleteSet.Clear();
        }

        public Image GetImage(string theFileName, out bool wasCached, bool loadIfMissing)
        {
            if (!mImageMap.TryGetValue(theFileName, out Image image))
            {
                mImageMap[theFileName] = null;
            }
            if (image == null)
            {
                if (loadIfMissing)
                {
                    image = GlobalStaticVars.gSexyAppBase.GetImage(theFileName, true) ?? SexyUtil.GetPCImage(theFileName);
                    mImageMap[theFileName] = image;
                }
                wasCached = false;
            }
            else
            {
                wasCached = true;
            }
            return image;
        }

        public Image GetImage(string theFileName)
        {
            return GetImage(theFileName, out _, true);
        }

        public void AddDeleteImage(Image theImage)
        {
            if (theImage != null)
            {
                mDeleteSet.Add(theImage);
            }
        }
    }
}
