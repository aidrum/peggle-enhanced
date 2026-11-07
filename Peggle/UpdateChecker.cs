using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sexy
{
    /// <summary>
    /// Port addition: asks GitHub for the latest Peggle Enhanced release once per launch, in the
    /// background. When its version (the release tag, e.g. "1.2") is higher than this build's
    /// (Directory.Build.props Version), ThunderballApp shows the update prompt on the main menu.
    /// No connection, an error or an equal or lower version: nothing happens.
    /// </summary>
    public static class UpdateChecker
    {
        private const string LATEST_RELEASE_URL = "https://api.github.com/repos/aidrum/peggle-enhanced/releases/latest";

        public const string REPOSITORY_URL = "https://github.com/aidrum/peggle-enhanced";

        public const string RELEASES_PAGE_URL = "https://github.com/aidrum/peggle-enhanced/releases/latest";

        private static int sStarted;

        private static volatile bool sUpdateReady;

        /// <summary>The newer release's version text and page, once sUpdateReady.</summary>
        private static string sNewVersion = "";

        private static string sReleaseUrl = RELEASES_PAGE_URL;

        public static void Start()
        {
            if (Interlocked.Exchange(ref sStarted, 1) != 0)
            {
                return;
            }
            // Scripted debug runs (clicks and screenshots) must not meet a prompt; the web version is always
            // the latest build.
            if (Environment.GetEnvironmentVariable("SEXY_DEBUG_SCRIPT") != null || OperatingSystem.IsBrowser())
            {
                return;
            }
            Task.Run(CheckAsync);
        }

        private static async Task CheckAsync()
        {
            try
            {
                using HttpClient aClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                // GitHub's API refuses requests without a User-Agent.
                aClient.DefaultRequestHeaders.UserAgent.ParseAdd("PeggleEnhanced/" + SexyUtil.GetPortVersionString());
                aClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                string aJson = await aClient.GetStringAsync(LATEST_RELEASE_URL).ConfigureAwait(false);
                using JsonDocument aDoc = JsonDocument.Parse(aJson);
                JsonElement aRoot = aDoc.RootElement;
                string aTag = aRoot.TryGetProperty("tag_name", out JsonElement aTagElem) ? aTagElem.GetString() : null;
                Version aRemote = ParseVersion(aTag);
                Version aLocal = Normalize(typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(1, 0));
                if (aRemote == null || aRemote <= aLocal)
                {
                    return;
                }
                if (aRoot.TryGetProperty("html_url", out JsonElement anUrl) && anUrl.GetString() is string aPage && aPage.StartsWith("https://"))
                {
                    sReleaseUrl = aPage;
                }
                sNewVersion = aTag.Trim().TrimStart('v', 'V');
                sUpdateReady = true;
            }
            catch (Exception e)
            {
                // Offline, timed out, rate limited or an unexpected reply: no check.
                Debug.OutputDebug("Update check skipped: " + e.Message);
            }
        }

        /// <summary>A release tag ("1.2", "v1.2.1") as a version, or null when it isn't one.</summary>
        private static Version ParseVersion(string theTag)
        {
            if (string.IsNullOrWhiteSpace(theTag))
            {
                return null;
            }
            string aText = theTag.Trim().TrimStart('v', 'V');
            int anEnd = 0;
            while (anEnd < aText.Length && (char.IsDigit(aText[anEnd]) || aText[anEnd] == '.'))
            {
                anEnd++;
            }
            aText = aText.Substring(0, anEnd).Trim('.');
            if (aText.Length == 0)
            {
                return null;
            }
            if (!aText.Contains('.'))
            {
                aText += ".0";
            }
            return Version.TryParse(aText, out Version aVersion) ? Normalize(aVersion) : null;
        }

        /// <summary>The version with missing parts as 0, so "1.1" and "1.1.0.0" compare equal.</summary>
        private static Version Normalize(Version theVersion)
        {
            return new Version(theVersion.Major, theVersion.Minor, Math.Max(theVersion.Build, 0), Math.Max(theVersion.Revision, 0));
        }

        /// <summary>True while a newer release is waiting to be shown (TakeUpdate).</summary>
        public static bool HasUpdate => sUpdateReady;

        /// <summary>
        /// True once, when a newer release was found: theNewVersion and theUrl are its version text and
        /// release page. The caller shows the prompt.
        /// </summary>
        public static bool TakeUpdate(out string theNewVersion, out string theUrl)
        {
            theNewVersion = sNewVersion;
            theUrl = sReleaseUrl;
            if (!sUpdateReady)
            {
                return false;
            }
            sUpdateReady = false;
            return true;
        }
    }
}
