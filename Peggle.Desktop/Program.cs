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
            // Saves live next to the executable, like the original PC release.
            // Debug aid: SEXY_STORAGE_DIR points saves/profiles at another folder (scripted test runs).
            string storage = Environment.GetEnvironmentVariable("SEXY_STORAGE_DIR");
            if (storage == null)
            {
                storage = AppContext.BaseDirectory;
                ImportOldSaves(storage);
            }
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

        /// <summary>
        /// Earlier builds saved to %AppData%\PeggleClassic. If the install folder has no saves yet,
        /// those are copied over (the old folder is left as it was).
        /// </summary>
        private static void ImportOldSaves(string theDir)
        {
            string oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PeggleClassic");
            if (!Directory.Exists(oldDir) || File.Exists(Path.Combine(theDir, "registry.txt")) || Directory.Exists(Path.Combine(theDir, "userdata")))
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
