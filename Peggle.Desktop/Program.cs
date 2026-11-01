using System;
using System.IO;
using Sexy;

namespace Peggle.Desktop
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--test-levels")
            {
                TestLevels(args[1]);
                return;
            }
            if (OperatingSystem.IsMacOS())
            {
                UseAppBundleContent();
            }
            // Debug aid: SEXY_STORAGE_DIR points saves/profiles at another folder (scripted test runs).
            string storage = Environment.GetEnvironmentVariable("SEXY_STORAGE_DIR") ?? GetDefaultStorageDir();
            Directory.CreateDirectory(storage);
            Sexy.Main.AppFactory = game => new ThunderballApp(game);
            using (Sexy.Main game = new Sexy.Main())
            {
                game.applicationStoragePath = storage;
#if PEGGLE_IPHONE
                game.Window.Title = "Peggle";
#else
                game.Window.Title = "Peggle HD";
#endif
                game.Run();
            }
        }

        /// <summary>The per-edition folder name used under the OS's application data folder.</summary>
#if PEGGLE_IPHONE
        private const string SAVE_FOLDER_EDITION = "iPhone";
#else
        private const string SAVE_FOLDER_EDITION = "HD";
#endif

        /// <summary>
        /// Where saves live when SEXY_STORAGE_DIR isn't set:
        /// <list type="bullet">
        /// <item>Windows: next to the executable, like the original PC release.</item>
        /// <item>macOS: ~/Library/Application Support/Peggle Enhanced/&lt;edition&gt;, as a .app bundle is read-only
        /// (and may run from a randomized, translocated path).</item>
        /// <item>Linux: $XDG_DATA_HOME (~/.local/share)/Peggle Enhanced/&lt;edition&gt;, as the install folder may be
        /// read-only (an AppImage always is). Saves already next to the executable keep being used.</item>
        /// </list>
        /// </summary>
        private static string GetDefaultStorageDir()
        {
            if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support", "Peggle Enhanced", SAVE_FOLDER_EDITION);
            }
            if (OperatingSystem.IsLinux() && !HasSaves(AppContext.BaseDirectory))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
                    Environment.SpecialFolderOption.Create), "Peggle Enhanced", SAVE_FOLDER_EDITION);
            }
            ImportOldSaves(AppContext.BaseDirectory);
            return AppContext.BaseDirectory;
        }

        /// <summary>
        /// In a macOS .app the game data lives in Contents/Resources/Content (codesign rejects data folders such
        /// as en.lproj next to the executable in Contents/MacOS); a plain build keeps it next to the executable.
        /// </summary>
        private static void UseAppBundleContent()
        {
            string aBundleContent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "Content"));
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Content")) && Directory.Exists(aBundleContent))
            {
                ContentFS.Root = aBundleContent;
            }
        }

        /// <summary>Whether a folder holds the game's saves (registry and/or profiles).</summary>
        private static bool HasSaves(string theDir)
        {
            return File.Exists(Path.Combine(theDir, "registry.txt")) || Directory.Exists(Path.Combine(theDir, "userdata"));
        }

        /// <summary>
        /// Earlier builds saved to %AppData%\PeggleClassic. If the install folder has no saves yet,
        /// those are copied over (the old folder is left as it was).
        /// </summary>
        private static void ImportOldSaves(string theDir)
        {
            string oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PeggleClassic");
            if (!Directory.Exists(oldDir) || HasSaves(theDir))
            {
                return;
            }
            try
            {
                foreach (string file in Directory.GetFiles(oldDir, "*", SearchOption.AllDirectories))
                {
                    string target = Path.Combine(theDir, Path.GetRelativePath(oldDir, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(file, target, false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not import saves from " + oldDir + ": " + e.Message);
            }
        }

        /// <summary>Debug aid: parses every level file in a folder and reports object counts or errors.</summary>
        private static void TestLevels(string theDir)
        {
            PhysObj.mFastLoad = true;
            int lastClass = 0;
            PhysObj.mDebugFactoryHook = c => lastClass = c;
            int ok = 0;
            foreach (string file in Directory.GetFiles(theDir, "*.dat"))
            {
                try
                {
                    var objs = new System.Collections.Generic.List<PhysObj>();
                    Board.LoadLevelBase(File.ReadAllBytes(file), objs);
                    var counts = new System.Collections.Generic.SortedDictionary<string, int>();
                    foreach (PhysObj obj in objs)
                    {
                        string key = obj.GetType().Name;
                        counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
                    }
                    Console.WriteLine(Path.GetFileName(file) + ": " + string.Join(", ", counts));
                    ok++;
                }
                catch (Exception e)
                {
                    Console.WriteLine(Path.GetFileName(file) + ": FAILED (last class " + lastClass + ") " + e.GetType().Name + " " + e.Message);
                }
            }
            Console.WriteLine(ok + " ok");
        }
    }
}
