using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Layout constants used by the framework layer. Same shape as PlantsVsZombies.NET's Constants
    /// (BackBufferSize is stored portrait-first: X = short side, Y = long side), trimmed to what the
    /// framework needs. Peggle Classic HD renders at the iPad's 1024x768 landscape resolution; the
    /// iPhone edition at 640 high and 960 (3:2) to 1136 (16:9) wide, picked from the screen at launch.
    /// </summary>
    public static class Constants
    {
        public enum LanguageIndex
        {
            en,
            fr,
            de,
            es,
            it
        }

        public static bool Loaded = true;

#if PEGGLE_IPHONE
        public static Point BackBufferSize = new Point(640, 1136);

        /// <summary>The narrowest iPhone layout: the iPhone 4's 3:2 960x640, with no gutters.</summary>
        public const int IPHONE_MIN_SCREEN_WIDTH = 960;

        /// <summary>The widest: the iPhone 5's 1136x640, with 88-pixel gutters.</summary>
        public const int IPHONE_MAX_SCREEN_WIDTH = 1136;

        /// <summary>
        /// Port addition: true when the game's width follows a resizable window (desktop): the app then
        /// switches between the 960 and 1136 layouts as the window changes (ThunderballApp.UpdateLayoutWidth).
        /// </summary>
        public static bool DynamicScreenWidth;

        /// <summary>
        /// Port addition: the game's width for a resizable window (desktop), at its fixed height of 640.
        /// Only the original's two layouts are used: the iPhone 5's 1136 (88-pixel gutters) when the
        /// window is 16:9 or wider, else the iPhone 4's 3:2 960. A layout can't shrink below its own
        /// width without letterboxing (its gutters hold UI), so narrower windows take the 960 one, which
        /// the ultrawide margins extend. The window opens at 16:9 (DefaultWindowWidth), so this starts at
        /// 1136. The SEXY_SCREEN_WIDTH environment variable fixes a native width from 960 to 1136 instead
        /// (debug aid). Call before the game is created.
        /// </summary>
        public static void ChooseScreenWidth()
        {
            DynamicScreenWidth = System.Environment.GetEnvironmentVariable("SEXY_SCREEN_WIDTH") == null;
            ChooseScreenWidth(IPHONE_MAX_SCREEN_WIDTH);
        }

        /// <summary>Port addition: the layout width a resizable window of this size calls for (see ChooseScreenWidth()).</summary>
        public static int GetWindowLayoutWidth(int theWindowWidth, int theWindowHeight)
        {
            return (long)theWindowWidth * BackBufferSize.X >= (long)theWindowHeight * IPHONE_MAX_SCREEN_WIDTH
                ? IPHONE_MAX_SCREEN_WIDTH : IPHONE_MIN_SCREEN_WIDTH;
        }

        /// <summary>Port addition: changes the game's width (the app re-lays itself out; see ThunderballApp.UpdateLayoutWidth).</summary>
        public static void SetScreenWidth(int theWidth)
        {
            BackBufferSize = new Point(BackBufferSize.X, theWidth);
        }

        /// <summary>
        /// Port addition: the game's width for a full screen (Android), from its aspect: 3:2 (960) to the
        /// iPhone 5's 16:9 (1136), as the original took the device's screen size. Narrower screens
        /// letterbox, wider ones get the ultrawide margins. Even, so the gutters are whole pixels.
        /// </summary>
        public static void ChooseScreenWidth(int theScreenWidth, int theScreenHeight)
        {
            int aWidth = IPHONE_MAX_SCREEN_WIDTH;
            if (theScreenWidth > 0 && theScreenHeight > 0)
            {
                // Landscape either way round.
                int aLong = System.Math.Max(theScreenWidth, theScreenHeight);
                int aShort = System.Math.Min(theScreenWidth, theScreenHeight);
                aWidth = (int)System.Math.Round(BackBufferSize.X * (double)aLong / aShort / 2.0) * 2;
            }
            ChooseScreenWidth(System.Math.Clamp(aWidth, IPHONE_MIN_SCREEN_WIDTH, IPHONE_MAX_SCREEN_WIDTH));
        }

        private static void ChooseScreenWidth(int theDefaultWidth)
        {
            int aWidth = theDefaultWidth;
            if (int.TryParse(System.Environment.GetEnvironmentVariable("SEXY_SCREEN_WIDTH"), out int anOverride))
            {
                aWidth = System.Math.Clamp(anOverride & ~1, IPHONE_MIN_SCREEN_WIDTH, IPHONE_MAX_SCREEN_WIDTH);
            }
            BackBufferSize = new Point(BackBufferSize.X, aWidth);
        }
#else
        public static Point BackBufferSize = new Point(768, 1024);
#endif

        public static int ScreenWidth => BackBufferSize.Y;

        /// <summary>The desktop window's starting width: 16:9 on iPhone, the game's in HD.</summary>
#if PEGGLE_IPHONE
        public static int DefaultWindowWidth => System.Math.Max(ScreenWidth, IPHONE_MAX_SCREEN_WIDTH);
#else
        public static int DefaultWindowWidth => ScreenWidth;
#endif

        public static int ScreenHeight => BackBufferSize.X;

        public static float S = 1f;

        public static DisplayOrientation SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;

        private static LanguageIndex mLanguage = LanguageIndex.en;

        public static LanguageIndex Language
        {
            get
            {
                return mLanguage;
            }
            set
            {
                mLanguage = value;
                ContentFS.Language = value.ToString();
            }
        }

        public static float InvertAndScale(float x)
        {
            return x * S;
        }
    }
}
