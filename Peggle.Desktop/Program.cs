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
            if (args.Length >= 2 && args[0] == "--dump-level")
            {
                DumpLevel(args[1]);
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

        /// <summary>Debug aid: lists each object of a level file with its position, brick shape, flags and mover.</summary>
        private static void DumpLevel(string theFile)
        {
            PhysObj.mFastLoad = true;
            var objs = new System.Collections.Generic.List<PhysObj>();
            Board.LoadLevelBase(File.ReadAllBytes(theFile), objs);
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (PhysObj obj in objs)
            {
                string anExtra = "";
                if (obj is Brick aBrick)
                {
                    anExtra = string.Format(inv, " type {0} angle {1} sweep {2} inner {3} width {4} curve {5} rot {6} rotvel {7} mover {8} center {9},{10} offset {11},{12} anchorL {13} anchorR {14} tflip {15} corners {16}", aBrick.mBrickType, aBrick.mAngle, aBrick.mSweepAngle, aBrick.mInnerRadius, aBrick.mWidth, aBrick.mCurvePoints, aBrick.mRotation, aBrick.mRotVel, aBrick.mMover != null, aBrick.mCenterX, aBrick.mCenterY, aBrick.mOffsetX, aBrick.mOffsetY, aBrick.mAnchorLeft, aBrick.mAnchorRight, aBrick.mTextureFlip, string.Join(".", aBrick.mCorners));
                }
                anExtra += string.Format(inv, " | flags col{0} vis{1} mov{2} bg{3} base{4} f29{5} sort{6} stop{7} fg{8} dfloat{9} f30{10} shadow{11} sub{12} flip{13} bouncy{14} rolly{15} img'{16}' logic'{17}' id'{18}' peg{19}",
                    obj.mCollidable ? 1 : 0, obj.mVisible ? 1 : 0, obj.mMoving ? 1 : 0, obj.mBackground ? 1 : 0, obj.mBaseObject ? 1 : 0, obj.mFlag29 ? 1 : 0, obj.mDrawSort ? 1 : 0, obj.mBallStopReset ? 1 : 0,
                    obj.mForeground ? 1 : 0, obj.mDrawFloat ? 1 : 0, obj.mFlag30 ? 1 : 0, obj.mShadow ? 1 : 0, obj.mSubId, obj.mFlipperFlags, obj.mBouncy, obj.mRolly, obj.mImageName, obj.mLogic, obj.mId, obj.mPegInfo != null ? 1 : 0);
                if (obj is Poly aPoly)
                {
                    aPoly.EnsureLines();
                    var aPts = new System.Collections.Generic.List<string>();
                    foreach (Line aLine in aPoly.mLines)
                    {
                        aPts.Add(string.Format(inv, "{0},{1}", aLine.mX1, aLine.mY1));
                    }
                    anExtra += " | lines " + string.Join(";", aPts);
                }
                Mover m = obj.mMover;
                if (m != null)
                {
                    anExtra += string.Format(inv, " | mover type {0} anchor {1},{2} period {3} toff {4} r1 {5} phase {6} moverot {7} r2 {8} p1 {9}/{10} p2 {11}/{12} maxang {13} rot {14} baserot {15} pos {16},{17}",
                        m.mType, m.mAnchorX, m.mAnchorY, m.mPeriod, m.mTimeOffset, m.mRadius1, m.mPhase, m.mMoveRotation, m.mRadius2, m.mPause1, m.mPhase1, m.mPause2, m.mPhase2, m.mMaxAngle, m.mRotation, m.mBaseRotation, m.mX, m.mY);
                }
                Console.WriteLine(obj.GetType().Name + " " + obj.GetXPos().ToString(inv) + " " + obj.GetYPos().ToString(inv) + anExtra);
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
