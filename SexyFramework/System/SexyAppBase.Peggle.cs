using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Sexy
{
    /// <summary>
    /// Parts of Peggle Classic HD's Sexy::SexyAppBase that the PlantsVsZombies.NET framework layer
    /// does not provide: a persistent registry (CFPreferences on iOS), the classic
    /// DoDialog/NewDialog pair and properties loading.
    /// </summary>
    public partial class SexyAppBase
    {
        /// <summary>Run-time string localisation table (SexyAppBase + 0x150 in the original).</summary>
        public PopLoc mPopLoc = new PopLoc();

        /// <summary>Shortcut for gSexyAppBase->mPopLoc.GetString(...), used by all UI text.</summary>
        public static string Loc(string theString)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            return app != null ? app.mPopLoc.GetString(theString) : theString;
        }

        private const string RegistryFileName = "registry.txt";

        private readonly Dictionary<string, object> mRegistry = new Dictionary<string, object>(StringComparer.Ordinal);

        private bool mRegistryLoaded;

        private string RegistryPath => Path.Combine(applicationStoragePath, RegistryFileName);

        private void LoadRegistryFile()
        {
            if (mRegistryLoaded)
            {
                return;
            }
            mRegistryLoaded = true;
            try
            {
                if (!System.IO.File.Exists(RegistryPath))
                {
                    return;
                }
                foreach (string line in System.IO.File.ReadAllLines(RegistryPath, Encoding.UTF8))
                {
                    // name \t type \t value  (i = int, s = base64 utf-8 string, b = base64 bytes)
                    string[] parts = line.Split('\t');
                    if (parts.Length != 3)
                    {
                        continue;
                    }
                    switch (parts[1])
                    {
                    case "i":
                        mRegistry[parts[0]] = int.Parse(parts[2], CultureInfo.InvariantCulture);
                        break;
                    case "s":
                        mRegistry[parts[0]] = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                        break;
                    case "b":
                        mRegistry[parts[0]] = Convert.FromBase64String(parts[2]);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.OutputDebug("Registry load failed: " + e.Message);
            }
        }

        private bool SaveRegistryFile()
        {
            try
            {
                Directory.CreateDirectory(applicationStoragePath);
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, object> kv in mRegistry)
                {
                    switch (kv.Value)
                    {
                    case int i:
                        sb.Append(kv.Key).Append("\ti\t").Append(i.ToString(CultureInfo.InvariantCulture)).Append('\n');
                        break;
                    case string s:
                        sb.Append(kv.Key).Append("\ts\t").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(s))).Append('\n');
                        break;
                    case byte[] b:
                        sb.Append(kv.Key).Append("\tb\t").Append(Convert.ToBase64String(b)).Append('\n');
                        break;
                    }
                }
                string tmp = RegistryPath + ".tmp";
                System.IO.File.WriteAllText(tmp, sb.ToString(), Encoding.UTF8);
                System.IO.File.Move(tmp, RegistryPath, true);
                return true;
            }
            catch (Exception e)
            {
                Debug.OutputDebug("Registry save failed: " + e.Message);
                return false;
            }
        }

        public bool RegistryReadInteger(string theValueName, ref int theValue)
        {
            LoadRegistryFile();
            if (mRegistry.TryGetValue(theValueName, out object v) && v is int i)
            {
                theValue = i;
                return true;
            }
            return false;
        }

        public bool RegistryReadString(string theValueName, ref string theString)
        {
            LoadRegistryFile();
            if (mRegistry.TryGetValue(theValueName, out object v) && v is string s)
            {
                theString = s;
                return true;
            }
            return false;
        }

        public bool RegistryReadBoolean(string theValueName, ref bool theValue)
        {
            int v = 0;
            if (!RegistryReadInteger(theValueName, ref v))
            {
                return false;
            }
            theValue = v != 0;
            return true;
        }

        public bool RegistryReadData(string theValueName, ref byte[] theData)
        {
            LoadRegistryFile();
            if (mRegistry.TryGetValue(theValueName, out object v) && v is byte[] b)
            {
                theData = b;
                return true;
            }
            return false;
        }

        public bool RegistryWriteInteger(string theValueName, int theValue)
        {
            LoadRegistryFile();
            mRegistry[theValueName] = theValue;
            return SaveRegistryFile();
        }

        public bool RegistryWriteBoolean(string theValueName, bool theValue)
        {
            return RegistryWriteInteger(theValueName, theValue ? 1 : 0);
        }

        public bool RegistryWriteString(string theValueName, string theString)
        {
            LoadRegistryFile();
            mRegistry[theValueName] = theString ?? "";
            return SaveRegistryFile();
        }

        public bool RegistryWriteData(string theValueName, byte[] theData)
        {
            LoadRegistryFile();
            mRegistry[theValueName] = theData;
            return SaveRegistryFile();
        }

        public bool RegistryEraseValue(string theValueName)
        {
            LoadRegistryFile();
            if (mRegistry.Remove(theValueName))
            {
                return SaveRegistryFile();
            }
            return false;
        }

        /// <summary>Port of SexyAppBase::ReadFromRegistry: volumes and mute state.</summary>
        public virtual void ReadFromRegistry()
        {
            mReadFromRegistry = true;
            int v = 0;
            if (RegistryReadInteger("MusicVolume", ref v))
            {
                SetMusicVolume(v / 100.0);
            }
            if (RegistryReadInteger("SfxVolume", ref v))
            {
                SetSfxVolume(v / 100.0);
            }
            if (RegistryReadInteger("Muted", ref v))
            {
                mMuteCount = v;
            }
            RegistryWriteInteger("InProgress", 1);
        }

        /// <summary>Creates the dialog object for DoDialog; games override this (ThunderballApp::NewDialog).</summary>
        public virtual Dialog NewDialog(int theDialogId, bool isModal, string theDialogHeader, string theDialogLines, string theDialogFooter, int theButtonMode)
        {
            return new Dialog(null, null, theDialogId, isModal, theDialogHeader, theDialogLines, theDialogFooter, theButtonMode);
        }

        public virtual Dialog DoDialog(int theDialogId, bool isModal, string theDialogHeader, string theDialogLines, string theDialogFooter, int theButtonMode)
        {
            KillDialog(theDialogId);
            Dialog dialog = NewDialog(theDialogId, isModal, theDialogHeader, theDialogLines, theDialogFooter, theButtonMode);
            AddDialog(theDialogId, dialog);
            return dialog;
        }

        /// <summary>Loads "properties/default.xml" (strings, ints, bools...) into the property maps.</summary>
        public bool LoadProperties(string theFileName, bool required, bool checkSig)
        {
            if (!ContentFS.Exists(theFileName))
            {
                if (required)
                {
                    Debug.OutputDebug("Unable to open properties file: " + theFileName);
                    return false;
                }
                return true;
            }
            PropertiesParser parser = new PropertiesParser(this);
            bool ok = parser.ParsePropertiesFile(theFileName);
            if (!ok)
            {
                Debug.OutputDebug(parser.GetErrorText());
            }
            return ok;
        }

        public bool GetBoolean(string theId, bool theDefault)
        {
            return mBoolProperties.TryGetValue(theId, out bool value) ? value : theDefault;
        }

        public bool LoadProperties()
        {
            return LoadProperties("properties/default.xml", true, false);
        }

        /// <summary>
        /// Port: fills mPopLoc for ContentFS.Language. The original's call sites give PopLoc a string
        /// id along with the English text; the port's keep only the text, so the English table maps
        /// text to id and the current language's table maps id to the translation.
        /// </summary>
        public void LoadPopLoc()
        {
            mPopLoc.mStrings.Clear();
            if (ContentFS.Language == "en")
            {
                return;
            }
            List<KeyValuePair<int, string>> english = PopLoc.ReadStringTable("en.lproj/properties/default.xml");
            Dictionary<int, string> local = new Dictionary<int, string>();
            foreach (KeyValuePair<int, string> entry in PopLoc.ReadStringTable(ContentFS.Language + ".lproj/properties/default.xml"))
            {
                local[entry.Key] = entry.Value;
            }
            foreach (KeyValuePair<int, string> entry in english)
            {
                // The same English text can have several ids; the first one with a usable translation wins.
                if (entry.Value.Length > 0 && !mPopLoc.mStrings.ContainsKey(entry.Value)
                    && local.TryGetValue(entry.Key, out string translated) && PopLoc.SameFormatSpecs(entry.Value, translated))
                {
                    mPopLoc.mStrings.Add(entry.Value, translated);
                }
            }
        }

        public bool IsMuted()
        {
            return mMuteCount > 0;
        }
    }

    /// <summary>
    /// Peggle's run-time string localisation (Sexy::PopLoc). Strings in the game code are written
    /// in English and passed through GetString, which returns the localised version when one is
    /// available. English is the identity mapping.
    /// </summary>
    public class PopLoc
    {
        public Dictionary<string, string> mStrings = new Dictionary<string, string>(StringComparer.Ordinal);

        public string GetString(string theString)
        {
            return mStrings.TryGetValue(theString, out string localized) ? localized : theString;
        }

        private static readonly Regex StringEntryRegex = new Regex("<String id=\"(\\d+)\">(.*?)</String>", RegexOptions.Singleline);

        private static readonly Regex EntityRegex = new Regex("&(#x[0-9a-fA-F]+|#[0-9]+|[a-zA-Z]+);");

        private static readonly Regex FormatSpecRegex = new Regex("%[-+ #0]*[0-9]*(\\.[0-9]+)?[a-zA-Z%]");

        /// <summary>Reads the &lt;String id="n"&gt; entries of a properties file, in file order.</summary>
        public static List<KeyValuePair<int, string>> ReadStringTable(string theFileName)
        {
            List<KeyValuePair<int, string>> table = new List<KeyValuePair<int, string>>();
            byte[] data = ContentFS.ReadAllBytes(theFileName);
            if (data == null)
            {
                return table;
            }
            string text = Encoding.UTF8.GetString(data);
            foreach (Match match in StringEntryRegex.Matches(text))
            {
                table.Add(new KeyValuePair<int, string>(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), DecodeEntities(match.Groups[2].Value)));
            }
            return table;
        }

        private static string DecodeEntities(string theString)
        {
            return EntityRegex.Replace(theString, m =>
            {
                string name = m.Groups[1].Value;
                switch (name)
                {
                case "nbsp": return " ";
                case "cr": return "\n";
                case "amp": return "&";
                case "lt": return "<";
                case "gt": return ">";
                case "quot": return "\"";
                case "apos": return "'";
                }
                if (name.StartsWith("#x"))
                {
                    return char.ConvertFromUtf32(int.Parse(name.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                if (name.StartsWith("#"))
                {
                    return char.ConvertFromUtf32(int.Parse(name.Substring(1), CultureInfo.InvariantCulture));
                }
                return m.Value;
            });
        }

        /// <summary>
        /// True when both strings use the same printf specifiers in the same order (a few shipped
        /// translations mangle them, e.g. "%d %  en plus", which would break the formatting).
        /// </summary>
        public static bool SameFormatSpecs(string theEnglish, string theTranslated)
        {
            MatchCollection a = FormatSpecRegex.Matches(theEnglish);
            MatchCollection b = FormatSpecRegex.Matches(theTranslated);
            if (a.Count != b.Count)
            {
                return false;
            }
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Value != b[i].Value)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
