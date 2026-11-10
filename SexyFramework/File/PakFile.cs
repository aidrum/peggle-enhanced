using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sexy
{
	/// <summary>
	/// Port addition: a PopCap .pak archive (Sexy::PakInterface's format), as the PC games and their level
	/// packs use. Every byte is XORed with 0xF7. Header: u32 magic 0xBAC04AC0, u32 version 0; then a
	/// directory of entries, each a flags byte (0x80 ends the list), a name (u8 length + text), a u32 size
	/// and a u64 file time; the files' data follow one after another in directory order.
	/// The directory is read when the pak is opened; file data is read on demand.
	/// </summary>
	public class PakFile
	{
		private const byte XOR_KEY = 0xF7;

		private const uint MAGIC = 0xBAC04AC0;

		private const byte FLAG_END = 0x80;

		private struct Entry
		{
			public string mName;

			public long mOffset;

			public int mSize;
		}

		public readonly string mPath;

		private readonly Dictionary<string, Entry> mEntries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

		private readonly List<string> mNames = new List<string>();

		private readonly object mLock = new object();

		private PakFile(string thePath)
		{
			mPath = thePath;
		}

		/// <summary>Normalizes an entry name: '/' separators, no leading "./" or '/'.</summary>
		public static string NormalizeName(string theName)
		{
			string aName = theName.Replace('\\', '/');
			while (aName.StartsWith("./", StringComparison.Ordinal))
			{
				aName = aName.Substring(2);
			}
			return aName.TrimStart('/');
		}

		/// <summary>Opens a pak and reads its directory. Null when the file isn't a readable pak.</summary>
		public static PakFile Open(string thePath)
		{
			try
			{
				PakFile aPak = new PakFile(thePath);
				using (FileStream aStream = File.OpenRead(thePath))
				{
					long aLength = aStream.Length;
					XorReader aReader = new XorReader(aStream);
					if (aReader.ReadU32() != MAGIC || aReader.ReadU32() != 0)
					{
						return null;
					}
					List<Entry> anEntries = new List<Entry>();
					while (true)
					{
						byte aFlags = aReader.ReadByte();
						if ((aFlags & FLAG_END) != 0)
						{
							break;
						}
						int aNameLen = aReader.ReadByte();
						byte[] aNameBytes = aReader.ReadBytes(aNameLen);
						int aSize = (int)aReader.ReadU32();
						aReader.ReadBytes(8);
						if (aSize < 0)
						{
							return null;
						}
						anEntries.Add(new Entry { mName = NormalizeName(Encoding.Latin1.GetString(aNameBytes)), mSize = aSize });
					}
					long anOffset = aReader.mPosition;
					foreach (Entry anEntry in anEntries)
					{
						Entry aPlaced = anEntry;
						aPlaced.mOffset = anOffset;
						anOffset += anEntry.mSize;
						if (anOffset > aLength)
						{
							return null;
						}
						if (!aPak.mEntries.ContainsKey(aPlaced.mName))
						{
							aPak.mNames.Add(aPlaced.mName);
						}
						aPak.mEntries[aPlaced.mName] = aPlaced;
					}
				}
				return aPak;
			}
			catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is EndOfStreamException)
			{
				return null;
			}
		}

		/// <summary>The entries' names ('/' separators), in directory order.</summary>
		public IReadOnlyList<string> GetNames()
		{
			return mNames;
		}

		public bool Contains(string theName)
		{
			return mEntries.ContainsKey(NormalizeName(theName));
		}

		/// <summary>The entry's name as the pak spells it, or null.</summary>
		public string FindName(string theName)
		{
			return mEntries.TryGetValue(NormalizeName(theName), out Entry anEntry) ? anEntry.mName : null;
		}

		/// <summary>An entry's data, or null when there is no such entry or it can't be read.</summary>
		public byte[] Read(string theName)
		{
			if (!mEntries.TryGetValue(NormalizeName(theName), out Entry anEntry))
			{
				return null;
			}
			try
			{
				byte[] aData = new byte[anEntry.mSize];
				lock (mLock)
				{
					using (FileStream aStream = File.OpenRead(mPath))
					{
						aStream.Seek(anEntry.mOffset, SeekOrigin.Begin);
						aStream.ReadExactly(aData, 0, aData.Length);
					}
				}
				for (int i = 0; i < aData.Length; i++)
				{
					aData[i] ^= XOR_KEY;
				}
				return aData;
			}
			catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is EndOfStreamException)
			{
				return null;
			}
		}

		/// <summary>Reads the XORed directory from the start of the file.</summary>
		private class XorReader
		{
			private readonly Stream mStream;

			public long mPosition;

			public XorReader(Stream theStream)
			{
				mStream = theStream;
			}

			public byte ReadByte()
			{
				int b = mStream.ReadByte();
				if (b < 0)
				{
					throw new EndOfStreamException();
				}
				mPosition++;
				return (byte)(b ^ XOR_KEY);
			}

			public byte[] ReadBytes(int theCount)
			{
				byte[] aData = new byte[theCount];
				mStream.ReadExactly(aData, 0, theCount);
				mPosition += theCount;
				for (int i = 0; i < theCount; i++)
				{
					aData[i] ^= XOR_KEY;
				}
				return aData;
			}

			public uint ReadU32()
			{
				byte[] b = ReadBytes(4);
				return (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
			}
		}
	}
}
