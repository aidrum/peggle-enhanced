using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.JSInterop;

namespace Peggle.Web
{
    /// <summary>
    /// The saves (registry, profiles, stats, replays) are ordinary files in the WebAssembly runtime's in-memory
    /// file system under <see cref="Dir"/>. They're kept in the browser's IndexedDB as one zip
    /// (wwwroot/js/peggle.js): restored before the game starts, and stored again whenever a file changes.
    /// </summary>
    internal static class WebSaves
    {
        public const string Dir = "/saves";

        private const double POLL_SECONDS = 2.0;

        private static string sFingerprint = "";

        private static long sLastPoll;

        public static void Restore(byte[] theZip)
        {
            Directory.CreateDirectory(Dir);
            if (theZip != null && theZip.Length > 0)
            {
                try
                {
                    using (MemoryStream aStream = new MemoryStream(theZip))
                    using (ZipArchive anArchive = new ZipArchive(aStream, ZipArchiveMode.Read))
                    {
                        anArchive.ExtractToDirectory(Dir, true);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not restore the saves: " + e.Message);
                }
            }
            sFingerprint = Fingerprint();
        }

        /// <summary>Stores the saves every few seconds if anything changed.</summary>
        public static void Poll(IJSInProcessRuntime theJs)
        {
            if (Stopwatch.GetElapsedTime(sLastPoll).TotalSeconds < POLL_SECONDS)
            {
                return;
            }
            sLastPoll = Stopwatch.GetTimestamp();
            Flush(theJs);
        }

        public static void Flush(IJSInProcessRuntime theJs)
        {
            try
            {
                string aFingerprint = Fingerprint();
                if (aFingerprint == sFingerprint)
                {
                    return;
                }
                using (MemoryStream aStream = new MemoryStream())
                {
                    using (ZipArchive anArchive = new ZipArchive(aStream, ZipArchiveMode.Create, true))
                    {
                        foreach (string aFile in Directory.EnumerateFiles(Dir, "*", SearchOption.AllDirectories))
                        {
                            anArchive.CreateEntryFromFile(aFile, Path.GetRelativePath(Dir, aFile).Replace('\\', '/'), CompressionLevel.Fastest);
                        }
                    }
                    theJs.InvokeVoid("peggleWeb.storeSaves", aStream.ToArray());
                }
                sFingerprint = aFingerprint;
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not store the saves: " + e.Message);
            }
        }

        /// <summary>Every file's path, size and time, which change whenever a file is written.</summary>
        private static string Fingerprint()
        {
            StringBuilder aText = new StringBuilder();
            foreach (string aFile in Directory.EnumerateFiles(Dir, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                FileInfo anInfo = new FileInfo(aFile);
                aText.Append(aFile).Append('|').Append(anInfo.Length).Append('|').Append(anInfo.LastWriteTimeUtc.Ticks).Append('\n');
            }
            return aText.ToString();
        }
    }
}
