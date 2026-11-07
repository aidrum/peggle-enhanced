using System;
using System.Collections.Generic;
using System.IO;

namespace Sexy
{
	/// <summary>Where the game data comes from: loose files under ContentFS.Root, or (Android) the APK's assets.</summary>
	public abstract class ContentSource
	{
		/// <summary>Every file, as a '/'-separated path relative to the content root.</summary>
		public abstract IEnumerable<string> EnumerateFiles();

		/// <summary>Opens a file by the relative path EnumerateFiles gave for it.</summary>
		public abstract Stream OpenRead(string theRelPath);
	}

	/// <summary>The data as ordinary files in the ContentFS.Root folder (desktop).</summary>
	public class FileContentSource : ContentSource
	{
		public override IEnumerable<string> EnumerateFiles()
		{
			string aRoot = ContentFS.Root;
			if (!Directory.Exists(aRoot))
			{
				yield break;
			}
			int rootLen = aRoot.Length + 1;
			foreach (string file in Directory.EnumerateFiles(aRoot, "*", SearchOption.AllDirectories))
			{
				yield return file.Substring(rootLen).Replace('\\', '/');
			}
		}

		public override Stream OpenRead(string theRelPath)
		{
			return System.IO.File.OpenRead(Path.Combine(ContentFS.Root, theRelPath));
		}
	}

	/// <summary>
	/// Resolves game resource paths against the converted iOS bundle in Content/.
	/// Mirrors NSBundle lookup: "&lt;lang&gt;.lproj/&lt;path&gt;" wins over "&lt;path&gt;".
	/// Lookups are case-insensitive (the iOS data references files with inconsistent case)
	/// and know about the extension changes made by tools/convert_assets.py
	/// (.tex/.txz -> .png, sound .caf -> .wav, music .caf -> .ogg).
	/// Resolved paths are "Root/&lt;relative path&gt;"; they need not exist on disk (Android reads the
	/// APK's assets), so open them with OpenResolved rather than System.IO.
	/// </summary>
	public static class ContentFS
	{
		public static string Language = "en";

		private static string mRoot;

		private static ContentSource mSource;

		private static volatile Dictionary<string, string> mIndex;

		private static readonly object mIndexLock = new object();

		private static readonly string[] ImageExts = { ".png", ".jpg", ".gif" };

		private static readonly string[] SoundExts = { ".wav", ".ogg" };

		public static string Root
		{
			get
			{
				if (mRoot == null)
				{
					mRoot = Path.Combine(AppContext.BaseDirectory, "Content");
				}
				return mRoot;
			}
			set
			{
				mRoot = value;
				mIndex = null;
			}
		}

		/// <summary>Where the files are read from; loose files under Root unless a platform sets another.</summary>
		public static ContentSource Source
		{
			get
			{
				if (mSource == null)
				{
					mSource = new FileContentSource();
				}
				return mSource;
			}
			set
			{
				mSource = value;
				mIndex = null;
			}
		}

		private static Dictionary<string, string> GetIndex()
		{
			Dictionary<string, string> anIndex = mIndex;
			if (anIndex != null)
			{
				return anIndex;
			}
			// The loading thread and the main thread both look files up.
			lock (mIndexLock)
			{
				if (mIndex == null)
				{
					anIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					foreach (string rel in Source.EnumerateFiles())
					{
						anIndex[rel] = Root + "/" + rel;
					}
					mIndex = anIndex;
				}
				return mIndex;
			}
		}

		public static string Normalize(string thePath)
		{
			string p = thePath.Replace('\\', '/');
			while (p.StartsWith("./"))
			{
				p = p.Substring(2);
			}
			if (p.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
			{
				p = p.Substring(8);
			}
			return p.TrimStart('/');
		}

		private static string Lookup(string rel)
		{
			Dictionary<string, string> anIndex = GetIndex();
			// Port: More Games content (net_content/) is not localized; it lives at the content root.
			if (!rel.StartsWith("net_content/", StringComparison.OrdinalIgnoreCase)
				&& anIndex.TryGetValue(Language + ".lproj/" + rel, out string loc))
			{
				return loc;
			}
			if (anIndex.TryGetValue(rel, out string full))
			{
				return full;
			}
			return null;
		}

		/// <summary>Returns the absolute path of a resource, or null if it does not exist.</summary>
		public static string Resolve(string thePath)
		{
			string rel = Normalize(thePath);
			string found = Lookup(rel);
			if (found != null)
			{
				return found;
			}
			string ext = Path.GetExtension(rel).ToLowerInvariant();
			string stem = ext.Length > 0 ? rel.Substring(0, rel.Length - ext.Length) : rel;
			string[] candidates;
			switch (ext)
			{
			case ".tex":
			case ".txz":
				candidates = ImageExts;
				break;
			case ".caf":
				candidates = SoundExts;
				break;
			case "":
				candidates = new[] { ".png", ".jpg", ".gif", ".wav", ".ogg", ".txt" };
				break;
			default:
				candidates = Array.Empty<string>();
				break;
			}
			foreach (string c in candidates)
			{
				found = Lookup(stem + c);
				if (found != null)
				{
					return found;
				}
			}
			return null;
		}

		/// <summary>Like <see cref="Resolve"/> but only matches image files.</summary>
		public static string ResolveImage(string thePath)
		{
			string found = Resolve(thePath);
			if (found == null)
			{
				return null;
			}
			string ext = Path.GetExtension(found).ToLowerInvariant();
			return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp" ? found : null;
		}

		public static bool Exists(string thePath)
		{
			return Resolve(thePath) != null;
		}

		/// <summary>True when the resource resolves to a file inside a "&lt;lang&gt;.lproj" folder.</summary>
		public static bool IsLocalized(string thePath)
		{
			string full = Resolve(thePath);
			if (full == null)
			{
				return false;
			}
			string rel = full.Substring(Root.Length + 1).Replace('\\', '/');
			int slash = rel.IndexOf('/');
			return slash > 0 && rel.Substring(0, slash).EndsWith(".lproj", StringComparison.OrdinalIgnoreCase);
		}

		public static Stream OpenRead(string thePath)
		{
			string full = Resolve(thePath);
			if (full == null)
			{
				throw new FileNotFoundException("Resource not found: " + thePath);
			}
			return OpenResolved(full);
		}

		public static byte[] ReadAllBytes(string thePath)
		{
			string full = Resolve(thePath);
			return full == null ? null : ReadResolvedBytes(full);
		}

		/// <summary>Opens a path returned by Resolve.</summary>
		public static Stream OpenResolved(string theFullPath)
		{
			if (!theFullPath.StartsWith(Root + "/", StringComparison.Ordinal))
			{
				throw new FileNotFoundException("Not a resolved content path: " + theFullPath);
			}
			return Source.OpenRead(theFullPath.Substring(Root.Length + 1));
		}

		/// <summary>Reads a whole file by a path returned by Resolve.</summary>
		public static byte[] ReadResolvedBytes(string theFullPath)
		{
			using (Stream aStream = OpenResolved(theFullPath))
			{
				if (aStream.CanSeek)
				{
					byte[] aData = new byte[aStream.Length];
					aStream.ReadExactly(aData, 0, aData.Length);
					return aData;
				}
				MemoryStream aCopy = new MemoryStream();
				aStream.CopyTo(aCopy);
				return aCopy.ToArray();
			}
		}
	}
}
