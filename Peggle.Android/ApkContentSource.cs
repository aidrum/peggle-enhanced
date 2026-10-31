using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Android.Content;
using Android.Content.Res;
using Sexy;

namespace Peggle.Android
{
    /// <summary>
    /// Reads the game data straight from the APK's "Content" assets, so nothing is copied out on
    /// first launch. Assets stored uncompressed (png/jpg/ogg, see the csproj) are read in place
    /// through a seekable window onto the APK file; the rest (xml, wav, levels) are inflated into
    /// memory when opened.
    /// </summary>
    public class ApkContentSource : ContentSource
    {
        public const string AssetRoot = "Content";

        private readonly AssetManager mAssets;

        private readonly string mApkPath;

        // Relative path -> uncompressed size, for the assets the APK stores without compression.
        private readonly Dictionary<string, long> mStored = new Dictionary<string, long>(StringComparer.Ordinal);

        public ApkContentSource(Context theContext)
        {
            mAssets = theContext.Assets;
            mApkPath = theContext.ApplicationInfo.SourceDir;
        }

        public override IEnumerable<string> EnumerateFiles()
        {
            // The zip's central directory lists every asset in one read, where AssetManager.List
            // would take a call per folder.
            const string prefix = "assets/" + AssetRoot + "/";
            List<string> aFiles = new List<string>();
            lock (mStored)
            {
                mStored.Clear();
                using (ZipArchive aZip = ZipFile.OpenRead(mApkPath))
                {
                    foreach (ZipArchiveEntry anEntry in aZip.Entries)
                    {
                        string aName = anEntry.FullName;
                        if (!aName.StartsWith(prefix, StringComparison.Ordinal) || aName.EndsWith("/"))
                        {
                            continue;
                        }
                        string aRel = aName.Substring(prefix.Length);
                        aFiles.Add(aRel);
                        // A stored entry's compressed and uncompressed sizes are equal.
                        if (anEntry.CompressedLength == anEntry.Length)
                        {
                            mStored[aRel] = anEntry.Length;
                        }
                    }
                }
            }
            return aFiles;
        }

        public override Stream OpenRead(string theRelPath)
        {
            string anAssetPath = AssetRoot + "/" + theRelPath;
            bool isStored;
            lock (mStored)
            {
                isStored = mStored.ContainsKey(theRelPath);
            }
            if (isStored)
            {
                try
                {
                    using (AssetFileDescriptor aDesc = mAssets.OpenFd(anAssetPath))
                    {
                        FileStream anApk = new FileStream(mApkPath, FileMode.Open, FileAccess.Read, FileShare.Read, 0x4000);
                        return new SubStream(anApk, aDesc.StartOffset, aDesc.Length);
                    }
                }
                catch (Java.IO.FileNotFoundException)
                {
                    // Compressed after all (OpenFd only works on stored assets): read it below.
                }
            }
            using (Stream anInput = mAssets.Open(anAssetPath))
            {
                MemoryStream aCopy = new MemoryStream();
                anInput.CopyTo(aCopy);
                aCopy.Position = 0;
                return aCopy;
            }
        }

        /// <summary>A read-only, seekable view of [theOffset, theOffset + theLength) of a stream it owns.</summary>
        private sealed class SubStream : Stream
        {
            private readonly Stream mBase;

            private readonly long mOffset;

            private readonly long mLength;

            private long mPosition;

            public SubStream(Stream theBase, long theOffset, long theLength)
            {
                mBase = theBase;
                mOffset = theOffset;
                mLength = theLength;
                mBase.Position = theOffset;
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length => mLength;

            public override long Position
            {
                get => mPosition;
                set => Seek(value, SeekOrigin.Begin);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                long aLeft = mLength - mPosition;
                if (aLeft <= 0)
                {
                    return 0;
                }
                if (count > aLeft)
                {
                    count = (int)aLeft;
                }
                if (mBase.Position != mOffset + mPosition)
                {
                    mBase.Position = mOffset + mPosition;
                }
                int aRead = mBase.Read(buffer, offset, count);
                mPosition += aRead;
                return aRead;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long aTarget = origin == SeekOrigin.Begin ? offset : origin == SeekOrigin.Current ? mPosition + offset : mLength + offset;
                if (aTarget < 0)
                {
                    throw new IOException("Seek before the start of the stream.");
                }
                mPosition = aTarget;
                return mPosition;
            }

            public override void Flush()
            {
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    mBase.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
