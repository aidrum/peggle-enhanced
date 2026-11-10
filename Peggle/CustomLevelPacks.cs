using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sexy
{
    /// <summary>
    /// Port addition: Peggle Nights level packs (.pak, as PeggleEdit makes them) dropped into a levelpacks
    /// folder (sFolders, set by each platform head). Their stages are rows of the Nights DLC page of Quick
    /// Play and Duel, under its own stages (StageMgr: BonusPage.mHostsCustomLevels, IsCustomStage); they play
    /// as Nights levels and are always open.
    ///
    /// A pack holds a stage config (a .cfg at its root: Stage groups with Name and Level keys; the challenge
    /// Pages are ignored), levels/&lt;name&gt;.dat, the backgrounds levels/&lt;name&gt;.png (PC, 800x600),
    /// levels/cached_thumbnails/&lt;name&gt;.png and the level art it names (images/...). Everything is PC
    /// resolution and scaled at load like the other PC levels (SexyUtil.GetPCImage).
    ///
    /// Each pack's files are added to ContentFS under levelpacks/&lt;key&gt;/, and each level gets a game
    /// level name "pak-&lt;key&gt;-&lt;level&gt;" whose level file and PC background are aliased to the pack's
    /// (levels/pak-...dat, levels/pc/pak-...png), so Board loads them like any level. The pack key is the
    /// pak's file name, so progress (cleared/aced ribbons, high scores, saved games and replays, all stored
    /// by level name) follows the file, and a removed pack's entries are simply never looked up: the
    /// saved-game prompt and the replay list already skip levels that don't exist.
    /// </summary>
    public static class CustomLevelPacks
    {
        public const string LEVEL_PREFIX = "pak-";

        private const string FILES_PREFIX = "levelpacks/";

        /// <summary>The folders searched for .pak files, set by the platform head before the game starts.</summary>
        public static readonly List<string> sFolders = new List<string>();

        public class CustomStage
        {
            public string mName = "";

            public List<CustomLevel> mLevels = new List<CustomLevel>();
        }

        public class CustomLevel
        {
            public Pack mPack;

            /// <summary>The level's name in the pack (levels/&lt;mPackName&gt;.dat).</summary>
            public string mPackName;

            /// <summary>The game's level name (LevelInfo.mName).</summary>
            public string mName;

            public string mDisplayName;

            public int mAceScore;

            /// <summary>The ContentFS path of its thumbnail (or background, when it has no thumbnail), or null.</summary>
            public string mThumbnailPath;
        }

        public class Pack
        {
            public string mKey;

            public string mFileName;

            public PakFile mPak;

            public List<CustomStage> mStages = new List<CustomStage>();

            /// <summary>The ContentFS path of one of its files.</summary>
            public string GetFilePath(string theEntry)
            {
                return FILES_PREFIX + mKey + "/" + theEntry;
            }
        }

        private static readonly List<Pack> sPacks = new List<Pack>();

        private static readonly Dictionary<string, CustomLevel> sLevels = new Dictionary<string, CustomLevel>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Image> sThumbnails = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        private static bool sLoaded;

        /// <summary>The pack of the level being played (Board.LoadLevel2), whose art its level objects use first.</summary>
        private static Pack sActivePack;

        private static readonly string[] IMAGE_EXTS = { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

        public static IReadOnlyList<Pack> GetPacks()
        {
            Load();
            return sPacks;
        }

        /// <summary>Finds the packs and reads their stage configs (once).</summary>
        public static void Load()
        {
            if (sLoaded)
            {
                return;
            }
            sLoaded = true;
            List<string> aFiles = new List<string>();
            foreach (string aFolder in sFolders)
            {
                try
                {
                    if (Directory.Exists(aFolder))
                    {
                        foreach (string aFile in Directory.GetFiles(aFolder))
                        {
                            if (string.Equals(Path.GetExtension(aFile), ".pak", StringComparison.OrdinalIgnoreCase))
                            {
                                aFiles.Add(aFile);
                            }
                        }
                    }
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                }
            }
            aFiles.Sort((a, b) => string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
            HashSet<string> aKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string aFile in aFiles)
            {
                try
                {
                    LoadPack(aFile, aKeys);
                }
                catch (Exception e)
                {
                    // A broken pack is left out; the rest still load.
                    Console.WriteLine("Level pack " + aFile + " skipped: " + e.Message);
                }
            }
        }

        /// <summary>Letters, digits and '_' only, so the name works as a level name (no '.', '/' or '-').</summary>
        private static string MakeSafe(string theName)
        {
            StringBuilder aName = new StringBuilder();
            foreach (char c in theName)
            {
                aName.Append(c < 0x80 && char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
            }
            return aName.Length == 0 ? "_" : aName.ToString();
        }

        private static string MakeUnique(string theName, Func<string, bool> theTaken)
        {
            string aName = theName;
            for (int i = 2; theTaken(aName); i++)
            {
                aName = theName + "_" + i;
            }
            return aName;
        }

        private static void LoadPack(string theFile, HashSet<string> theKeys)
        {
            PakFile aPak = PakFile.Open(theFile);
            if (aPak == null)
            {
                Console.WriteLine("Level pack " + theFile + " isn't a pak file.");
                return;
            }
            string aFileName = Path.GetFileNameWithoutExtension(theFile);
            string aCfg = FindConfig(aPak, aFileName);
            if (aCfg == null)
            {
                Console.WriteLine("Level pack " + theFile + " has no stage config.");
                return;
            }
            Pack aPack = new Pack();
            aPack.mPak = aPak;
            aPack.mFileName = aFileName;
            aPack.mKey = MakeUnique(MakeSafe(aFileName), k => theKeys.Contains(k));
            foreach (string anEntry in aPak.GetNames())
            {
                string aName = anEntry;
                ContentFS.AddVirtualFile(aPack.GetFilePath(aName), () => aPak.Read(aName));
            }
            ReadConfig(aPack, aPack.GetFilePath(aCfg));
            if (aPack.mStages.Count == 0)
            {
                ContentFS.RemoveVirtualFiles(FILES_PREFIX + aPack.mKey + "/");
                Console.WriteLine("Level pack " + theFile + " has no levels.");
                return;
            }
            theKeys.Add(aPack.mKey);
            sPacks.Add(aPack);
        }

        /// <summary>The pack's stage config: a .cfg at its root, the one named like the pak if there are several.</summary>
        private static string FindConfig(PakFile thePak, string theFileName)
        {
            string aFound = null;
            foreach (string anEntry in thePak.GetNames())
            {
                if (anEntry.IndexOf('/') >= 0 || !anEntry.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (string.Equals(Path.GetFileNameWithoutExtension(anEntry), theFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return anEntry;
                }
                // stages.cfg is what PeggleEdit packs and the PC DLC use; otherwise the first.
                if (aFound == null || string.Equals(anEntry, "stages.cfg", StringComparison.OrdinalIgnoreCase))
                {
                    aFound = anEntry;
                }
            }
            return aFound;
        }

        /// <summary>
        /// Reads the Stage groups (Name, Level) at the top of the config; the challenge Pages, trophies and
        /// anything else are skipped. A stage of more than five levels is split into rows of five.
        /// </summary>
        private static void ReadConfig(Pack thePack, string thePath)
        {
            ConfigParser aParser = new ConfigParser(false);
            if (!aParser.Open(thePath, false))
            {
                return;
            }
            List<CustomStage> aStages = new List<CustomStage>();
            CustomStage aStage = null;
            HashSet<string> aLevelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (aParser.ReadNext())
            {
                ConfigParser.ItemType aType = aParser.GetType();
                if (aType == ConfigParser.ItemType.GroupStart)
                {
                    aStage = null;
                    if (aParser.GetGroupDepth() == 1 && string.Equals(aParser.GetGroupType(), "Stage", StringComparison.OrdinalIgnoreCase))
                    {
                        aStage = new CustomStage();
                        aStages.Add(aStage);
                    }
                }
                else if (aType == ConfigParser.ItemType.GroupEnd)
                {
                    aStage = null;
                }
                else if (aType == ConfigParser.ItemType.KeyValue && aStage != null && aParser.GetGroupDepth() == 1)
                {
                    StringParser aVal = aParser.GetValParser();
                    if (aParser.KeyIs("Name"))
                    {
                        aStage.mName = aVal.ReadString();
                    }
                    else if (aParser.KeyIs("Level"))
                    {
                        CustomLevel aLevel = ReadLevel(thePack, aVal, aLevelNames);
                        if (aLevel != null)
                        {
                            aStage.mLevels.Add(aLevel);
                        }
                    }
                }
            }
            if (aParser.GetError().Length > 0)
            {
                Console.WriteLine("Level pack " + thePack.mFileName + ": " + aParser.GetError());
            }
            foreach (CustomStage aRead in aStages)
            {
                string aName = aRead.mName.Trim().Length > 0 ? aRead.mName.Trim() : thePack.mFileName;
                for (int i = 0; i < aRead.mLevels.Count; i += 5)
                {
                    CustomStage aRow = new CustomStage();
                    aRow.mName = aName;
                    aRow.mLevels.AddRange(aRead.mLevels.GetRange(i, Math.Min(5, aRead.mLevels.Count - i)));
                    thePack.mStages.Add(aRow);
                    foreach (CustomLevel aLevel in aRow.mLevels)
                    {
                        sLevels[aLevel.mName] = aLevel;
                    }
                }
            }
        }

        /// <summary>A "Level: name, display name, ace score[, ...]" line; null when the pack has no such level file.</summary>
        private static CustomLevel ReadLevel(Pack thePack, StringParser theVal, HashSet<string> theNames)
        {
            string aPackName = theVal.ReadString().Trim();
            if (aPackName.Length == 0)
            {
                return null;
            }
            string aDisplayName = aPackName;
            int anAceScore = 300000;
            if (theVal.CheckNextChar(','))
            {
                aDisplayName = theVal.ReadString();
                if (theVal.CheckNextChar(','))
                {
                    anAceScore = theVal.ReadInt(300000);
                }
            }
            string aDat = thePack.mPak.FindName("levels/" + aPackName + ".dat");
            if (aDat == null)
            {
                Console.WriteLine("Level pack " + thePack.mFileName + ": level file not found: " + aPackName);
                return null;
            }
            CustomLevel aLevel = new CustomLevel();
            aLevel.mPack = thePack;
            aLevel.mPackName = aPackName;
            aLevel.mDisplayName = aDisplayName;
            aLevel.mAceScore = anAceScore > 0 ? anAceScore : 300000;
            aLevel.mName = MakeUnique(LEVEL_PREFIX + thePack.mKey + "-" + MakeSafe(aPackName), n => theNames.Contains(n));
            theNames.Add(aLevel.mName);
            // The level file and its PC background, under the names Board looks for.
            ContentFS.AddVirtualFile("levels/" + aLevel.mName + ".dat", () => thePack.mPak.Read(aDat));
            string aBackground = FindImage(thePack.mPak, "levels/" + aPackName);
            if (aBackground != null)
            {
                ContentFS.AddVirtualFile("levels/pc/" + aLevel.mName + Path.GetExtension(aBackground), () => thePack.mPak.Read(aBackground));
            }
            string aThumbnail = FindImage(thePack.mPak, "levels/cached_thumbnails/" + aPackName) ?? aBackground;
            aLevel.mThumbnailPath = aThumbnail != null ? thePack.GetFilePath(aThumbnail) : null;
            return aLevel;
        }

        /// <summary>The pak entry of an image named with or without its extension, or null.</summary>
        private static string FindImage(PakFile thePak, string theName)
        {
            string aFound = thePak.FindName(theName);
            if (aFound != null && Array.IndexOf(IMAGE_EXTS, Path.GetExtension(aFound).ToLowerInvariant()) >= 0)
            {
                return aFound;
            }
            foreach (string anExt in IMAGE_EXTS)
            {
                aFound = thePak.FindName(theName + anExt);
                if (aFound != null)
                {
                    return aFound;
                }
            }
            return null;
        }

        /// <summary>Adds the packs' stages to the stage list (after the Nights DLC stages), in pack (file name) order, and returns how many.</summary>
        public static int AddStages(StageMgr theStageMgr)
        {
            Load();
            int aCount = 0;
            foreach (Pack aPack in sPacks)
            {
                foreach (CustomStage aCustom in aPack.mStages)
                {
                    StageInfo aStage = new StageInfo();
                    aStage.mCustomName = aCustom.mName;
                    theStageMgr.mStages.Add(aStage);
                    foreach (CustomLevel aCustomLevel in aCustom.mLevels)
                    {
                        LevelInfo aLevel = new LevelInfo();
                        aLevel.mName = aCustomLevel.mName;
                        aLevel.mDisplayName = aCustomLevel.mDisplayName;
                        aLevel.mAceScore = aCustomLevel.mAceScore;
                        aLevel.mStage = theStageMgr.mStages.Count - 1;
                        aLevel.mLevel = aStage.mLevels.Count;
                        aStage.mLevels.Add(aLevel);
                        theStageMgr.mLevelMap[aLevel.mName] = aLevel;
                    }
                    aCount++;
                }
            }
            return aCount;
        }

        /// <summary>The custom level with this game level name (or level file path), or null.</summary>
        public static CustomLevel GetLevel(string theLevelName)
        {
            if (string.IsNullOrEmpty(theLevelName))
            {
                return null;
            }
            string aName = SexyUtil.GetFileName(theLevelName, true);
            if (!aName.StartsWith(LEVEL_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return sLevels.TryGetValue(aName, out CustomLevel aLevel) ? aLevel : null;
        }

        /// <summary>Board.LoadLevel2: the level (a level file path) about to be loaded, so its pack's art is used.</summary>
        public static void SetActiveLevel(string theLevelFile)
        {
            sActivePack = GetLevel(theLevelFile)?.mPack;
        }

        /// <summary>
        /// The ContentFS path of an image a level object names ("images\\foo.png") in the pack of the level
        /// being played, or null. As on the PC, where a pak's files are mounted over the game's, the pack's
        /// own file comes first.
        /// </summary>
        public static string GetActivePackImage(string theFileName)
        {
            Pack aPack = sActivePack;
            if (aPack == null || string.IsNullOrEmpty(theFileName))
            {
                return null;
            }
            string anEntry = FindImage(aPack.mPak, ContentFS.Normalize(theFileName));
            return anEntry != null ? aPack.GetFilePath(anEntry) : null;
        }

        /// <summary>A custom level's thumbnail at theWidth x theHeight (loaded once), or null when it has none.</summary>
        public static Image GetThumbnail(LevelInfo theLevel, int theWidth, int theHeight)
        {
            CustomLevel aLevel = theLevel != null ? GetLevel(theLevel.mName) : null;
            if (aLevel == null || aLevel.mThumbnailPath == null)
            {
                return null;
            }
            if (!sThumbnails.TryGetValue(aLevel.mName, out Image anImage))
            {
                try
                {
                    anImage = SexyUtil.LoadSizedImage(aLevel.mThumbnailPath, theWidth, theHeight);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Level pack thumbnail " + aLevel.mThumbnailPath + ": " + e.Message);
                    anImage = null;
                }
                sThumbnails[aLevel.mName] = anImage;
            }
            return anImage;
        }
    }
}
