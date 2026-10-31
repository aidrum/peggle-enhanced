using System;

namespace Sexy
{
    public /*internal*/ static partial class GlobalStaticVars
    {
        public static void initialize(Main main)
        {
            initializeReusables();
            GlobalStaticVars.g = Main.graphics;
            GlobalStaticVars.g.Init();
            GlobalStaticVars.gSexyAppBase = Main.AppFactory(main);
            GlobalStaticVars.gSexyAppBase.Init();
            GlobalStaticVars.gSexyAppBase.Start();
        }

        public static void initializeReusables() 
        {
            //LazyReusableObjectHelper<Graphics>.Initialize(20);
            Graphics.PreAllocateMemory();
        } 

        public static void shutdown()
        {
            GlobalStaticVars.gSexyAppBase.Shutdown();
            GlobalStaticVars.gSexyAppBase.Dispose();
        }

        internal static string GetResourceDir()
        {
            return "";
        }

        internal static string CommaSeperate_(int theDispPoints)
        {
            if (theDispPoints == 0)
            {
                return "0";
            }
            return string.Format("{0:#,#}", theDispPoints);
        }

        internal static string GetDocumentsDir()
        {
            return "docs/";
        }

        public static Graphics g;

        public static SexyAppBase gSexyAppBase;

        /// <summary>
        /// Resolution sub-folder of the art, as in Peggle's mResPathPrefix: "1024x768/" (iPad) for HD,
        /// "960x640/" for the iPhone edition (set by ThunderballApp from the screen size, as the original does).
        /// </summary>
        public static string mResPathPrefix = "1024x768/";

        internal static int gProfileVersion = 14;

        public static bool gIsPartnerBuild = false;

        public static int gSlowMoCounter = 0;

        public static bool gSlowMo = false;

        public static bool gFastMo = false;

        public static bool gLowFramerate = false;

        public static string gGetCurrentLevelName;

        public static bool gAppCloseRequest;

        public static bool gAppHasUsedCheatKeys;

        public struct CGPoint
        {
            public float x;

            public float y;
        }

        public enum Phase
        {
            TOUCH_BEGAN,
            TOUCH_MOVED,
            TOUCH_STATIONARY,
            TOUCH_ENDED,
            TOUCH_CANCELLED
        }

        public struct Touch
        {
            public GlobalStaticVars.CGPoint location;
        }
    }
}
