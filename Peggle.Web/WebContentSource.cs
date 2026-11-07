using System;
using System.Collections.Generic;
using System.IO;
using nkast.Wasm.XHR;
using Sexy;

namespace Peggle.Web
{
    /// <summary>
    /// The game data as files on the web server. A browser can't list a folder, so Content/files.txt lists
    /// them (distribution/build_web.sh): one game path per line, under Content/, or "path&lt;TAB&gt;url" when
    /// the file is elsewhere (the Pages site keeps the builds' files in one shared folder, see
    /// distribution/make_pages.py). Files are fetched when they're opened, with a synchronous request like
    /// the ones KNI's TitleContainer makes (which can't be used itself: it drops the ".." of those urls), and
    /// cached by the browser.
    /// </summary>
    internal sealed class WebContentSource : ContentSource
    {
        public const string FILE_LIST = "files.txt";

        private readonly Dictionary<string, string> mUrls = new Dictionary<string, string>(StringComparer.Ordinal);

        public override IEnumerable<string> EnumerateFiles()
        {
            string aList;
            using (Stream aStream = Download(ContentFS.Root + "/" + FILE_LIST))
            using (StreamReader aReader = new StreamReader(aStream))
            {
                aList = aReader.ReadToEnd();
            }
            mUrls.Clear();
            List<string> aPaths = new List<string>();
            foreach (string aLine in aList.Split('\n'))
            {
                string[] aFields = aLine.TrimEnd('\r').Split('\t');
                string aPath = aFields[0].Trim();
                if (aPath.Length == 0)
                {
                    continue;
                }
                if (aFields.Length > 1 && aFields[1].Length > 0)
                {
                    mUrls[aPath] = aFields[1];
                }
                aPaths.Add(aPath);
            }
            return aPaths;
        }

        public override Stream OpenRead(string theRelPath)
        {
            if (!mUrls.TryGetValue(theRelPath, out string anUrl))
            {
                // Some names need escaping in a URL (the levels' "%", spaces).
                string[] aParts = theRelPath.Split('/');
                for (int i = 0; i < aParts.Length; i++)
                {
                    aParts[i] = Uri.EscapeDataString(aParts[i]);
                }
                anUrl = ContentFS.Root + "/" + string.Join("/", aParts);
            }
            try
            {
                return Download(anUrl);
            }
            catch (IOException e)
            {
                throw new FileNotFoundException("Can't download " + theRelPath + ": " + e.Message, theRelPath, e);
            }
        }

        /// <summary>
        /// Fetches a file synchronously. A page's synchronous requests can't ask for binary data, so the bytes
        /// come as the low byte of each character (the x-user-defined charset).
        /// </summary>
        private static Stream Download(string theUrl)
        {
            XMLHttpRequest aRequest = new XMLHttpRequest();
            aRequest.Open("GET", theUrl, false);
            aRequest.OverrideMimeType("text/plain; charset=x-user-defined");
            aRequest.Send();
            if (aRequest.Status != 200)
            {
                throw new IOException("HTTP " + aRequest.Status + " for " + theUrl);
            }
            string aText = aRequest.ResponseText;
            byte[] aData = new byte[aText.Length];
            for (int i = 0; i < aText.Length; i++)
            {
                aData[i] = (byte)aText[i];
            }
            return new MemoryStream(aData, false);
        }
    }
}
