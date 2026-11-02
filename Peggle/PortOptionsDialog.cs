using System;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// Port addition: the port's own options, opened from the main-menu Options dialog and laid out
    /// in its two columns, on two pages. The first holds Low Memory Mode (lazy loading), High Quality Pegs,
    /// Random Music and (desktop only) PC Controls;
    /// the second, Theme Settings, holds the Menu Theme choice (left), and the Nights Menu and Main
    /// Menu Theme ones (right); the third, App Icon (Android, or SEXY_APP_ICONS=1 to preview it), the
    /// launcher icons, each a picture with a radio button below. A button left of DONE, in its row, turns
    /// the pages.
    /// </summary>
    public class PortOptionsDialog : ThunderDialog, CheckboxListener
    {
        /// <summary>Ids 0-5 are the Menu Theme radio buttons, in MenuTheme order.</summary>
        public const int PortOptions_MenuTheme = 0;

        public const int PortOptions_LazyLoading = 10;

        public const int PortOptions_HighQualityPegs = 11;

        public const int PortOptions_RandomMusic = 12;

        public const int PortOptions_PCControls = 13;

        /// <summary>Ids 20-21 are the Nights Menu radio buttons: Mobile, then PC.</summary>
        public const int PortOptions_NightsMenu = 20;

        /// <summary>Ids 25-26 are the Main Menu Theme radio buttons: Ambience, then Music.</summary>
        public const int PortOptions_MainMenuMusic = 25;

        public const int PortOptions_Page = 30;

        /// <summary>Ids 40 on are the App Icon radio buttons, in APP_ICONS order.</summary>
        public const int PortOptions_AppIcon = 40;

        public const int PAGE_ICONS = 2;

        /// <summary>A launcher icon: its label, its activity alias (Peggle.Android/AndroidManifest.xml) and its picture.</summary>
        public class AppIcon
        {
            public string mLabel;

            public string mAlias;

            /// <summary>The appicons sheet cel shown for it (tools/build_app_icon_sheet.py); a transparent icon shares its opaque one's.</summary>
            public int mPicture;

            public AppIcon(string theLabel, string theAlias, int thePicture)
            {
                mLabel = theLabel;
                mAlias = theAlias;
                mPicture = thePicture;
            }
        }

        /// <summary>
        /// The launcher icons. Each picture gets its icons' radio buttons stacked below it (PC and Nights have
        /// a Transparent one too, with a legacy-only launcher icon).
        /// </summary>
        public static readonly AppIcon[] APP_ICONS =
        {
            new AppIcon("iOS", "IconIOS", 0),
            new AppIcon("iOS Old", "IconIOSOld", 1),
            new AppIcon("Android", "IconAndroid", 2),
            new AppIcon("PC", "IconPC", 3),
            new AppIcon("Transparent", "IconPCTransparent", 3),
            new AppIcon("Nights", "IconNights", 4),
            new AppIcon("Transparent", "IconNightsTransparent", 4),
            new AppIcon("Play Store", "IconPlayStore", 5),
            new AppIcon("Java", "IconJava", 6),
        };

        /// <summary>The pictures (appicons cels) row by row, each row spread across both columns.</summary>
        private static readonly int[][] APP_ICON_ROWS = { new[] { 0, 1, 2, 5 }, new[] { 3, 4, 6 } };

        private const int APP_ICON_PICTURES = 7;

        /// <summary>The radio buttons are drawn this much smaller than the dialog's, to fit four to a row.</summary>
        private const float APP_ICON_CHECK_SCALE = 0.7f;

        /// <summary>Debug aid: SEXY_APP_ICONS=1 shows the App Icon page where icons can't be switched (it does nothing there).</summary>
        private static readonly bool sDebugAppIcons = System.Environment.GetEnvironmentVariable("SEXY_APP_ICONS") == "1";

        public const int PAGE_GENERAL = 0;

        public const int PAGE_THEME = 1;

        private const int LAY_STACK_BELOW = (int)(LayoutFlags.LAY_SameLeft | LayoutFlags.LAY_Below | LayoutFlags.LAY_SameWidth | LayoutFlags.LAY_SameHeight);

        private const int LAY_BESIDE = (int)(LayoutFlags.LAY_SameTop | LayoutFlags.LAY_Right | LayoutFlags.LAY_SameWidth);

        private static readonly string[] MENU_THEME_LABELS = { "Time-based", "Last Played", "Peggle", "Peggle Nights", "Peggle Extreme", "Peggle WoW" };

        private static readonly string[] NIGHTS_MENU_LABELS = { "Mobile", "PC" };

        private static readonly string[] MAIN_MENU_MUSIC_LABELS = { "Ambience", "Music" };

        public ThunderballApp mApp;

        public int mPage;

        public ThunderCheckbox[] mMenuThemeChecks = new ThunderCheckbox[MenuTheme.MENU_THEME_COUNT];

        public ThunderCheckbox[] mNightsMenuChecks = new ThunderCheckbox[NIGHTS_MENU_LABELS.Length];

        public ThunderCheckbox[] mMainMenuMusicChecks = new ThunderCheckbox[MAIN_MENU_MUSIC_LABELS.Length];

        public ThunderCheckbox mLazyLoadingCheckbox;

        public ThunderCheckbox mHighQualityPegsCheckbox;

        public ThunderCheckbox mRandomMusicCheckbox;

        /// <summary>PC Controls; desktop only (null on Android).</summary>
        public ThunderCheckbox mPCControlsCheckbox;

        public ThunderCheckbox[] mAppIconChecks;

        /// <summary>The appicons sheet: the icons' plain launcher pictures, one square cel each.</summary>
        public Image mAppIconSheet;

        public string mAppIconLabel;

        /// <summary>The icon alias the launcher shows, and the one picked (applied with DONE, after a warning).</summary>
        private string mAppIconCurrent;

        private string mAppIconPicked;

        public ThunderButton mPageButton;

        public ThunderButton mDoneButton;

        public string mMenuThemeLabel;

        public string mNightsMenuLabel;

        public string mMainMenuMusicLabel;

        public string mRestartLabel;

        public PortOptionsDialog(ThunderballApp theApp)
            : base(DialogId.DIALOG_PORT_OPTIONS, true, SexyAppBase.Loc("EXTRA OPTIONS"), "", SexyAppBase.Loc(""), 0)
        {
            mApp = theApp;
            mCloseOnButton = true;
            mMenuThemeLabel = SexyAppBase.Loc("Menu Theme");
            mNightsMenuLabel = SexyAppBase.Loc("Nights Menu");
            mMainMenuMusicLabel = SexyAppBase.Loc("Main Menu Sound");
            mRestartLabel = SexyAppBase.Loc("Takes effect after a restart");
            for (int i = 0; i < mMenuThemeChecks.Length; i++)
            {
                mMenuThemeChecks[i] = SexyUtil.MakeRadioButton(PortOptions_MenuTheme + i, this, SexyAppBase.Loc(MENU_THEME_LABELS[i]), 0);
                mMenuThemeChecks[i].mChecked = i == mApp.mMenuTheme;
                AddWidget(mMenuThemeChecks[i]);
            }
            for (int i = 0; i < mNightsMenuChecks.Length; i++)
            {
                mNightsMenuChecks[i] = SexyUtil.MakeRadioButton(PortOptions_NightsMenu + i, this, SexyAppBase.Loc(NIGHTS_MENU_LABELS[i]), 0);
                mNightsMenuChecks[i].mChecked = (i == 1) == mApp.mNightsMenuPC;
                AddWidget(mNightsMenuChecks[i]);
            }
            for (int i = 0; i < mMainMenuMusicChecks.Length; i++)
            {
                mMainMenuMusicChecks[i] = SexyUtil.MakeRadioButton(PortOptions_MainMenuMusic + i, this, SexyAppBase.Loc(MAIN_MENU_MUSIC_LABELS[i]), 0);
                mMainMenuMusicChecks[i].mChecked = (i == 0) == mApp.mMainMenuAmbience;
                AddWidget(mMainMenuMusicChecks[i]);
            }
            mLazyLoadingCheckbox = SexyUtil.MakeCheckbox2(PortOptions_LazyLoading, this, SexyAppBase.Loc("Low Memory Mode"), 0);
            mLazyLoadingCheckbox.mChecked = mApp.mLazyLoadingOption;
            AddWidget(mLazyLoadingCheckbox);
            mHighQualityPegsCheckbox = SexyUtil.MakeCheckbox2(PortOptions_HighQualityPegs, this, SexyAppBase.Loc("High Quality Pegs"), 0);
            mHighQualityPegsCheckbox.mChecked = PegInfo.mHighQualityPegs;
            AddWidget(mHighQualityPegsCheckbox);
            mRandomMusicCheckbox = SexyUtil.MakeCheckbox2(PortOptions_RandomMusic, this, SexyAppBase.Loc("Random Music"), 0);
            mRandomMusicCheckbox.mChecked = mApp.mRandomMusic;
            AddWidget(mRandomMusicCheckbox);
            if (!Main.IsMobile)
            {
                mPCControlsCheckbox = SexyUtil.MakeCheckbox2(PortOptions_PCControls, this, SexyAppBase.Loc("PC Controls"), 0);
                mPCControlsCheckbox.mChecked = mApp.mPCControls;
                AddWidget(mPCControlsCheckbox);
            }
            if (HasIconPage())
            {
                mAppIconLabel = SexyAppBase.Loc("App Icon");
                mAppIconSheet = mApp.GetImage("images/" + GlobalStaticVars.mResPathPrefix + "appicons", true);
                int aCount = APP_ICONS.Length;
                string[] anAliases = GetAppIconAliases();
                string aCurrent = Main.GetAppIconHandler?.Invoke(anAliases) ?? anAliases[0];
                mAppIconCurrent = aCurrent;
                mAppIconPicked = aCurrent;
                mAppIconChecks = new ThunderCheckbox[aCount];
                for (int i = 0; i < aCount; i++)
                {
                    mAppIconChecks[i] = SexyUtil.MakeRadioButton(PortOptions_AppIcon + i, this, SexyAppBase.Loc(APP_ICONS[i].mLabel), 0);
                    mAppIconChecks[i].mDrawScale = APP_ICON_CHECK_SCALE;
                    mAppIconChecks[i].mChecked = APP_ICONS[i].mAlias == aCurrent;
                    AddWidget(mAppIconChecks[i]);
                }
            }
            mPageButton = SexyUtil.MakeButton(PortOptions_Page, this, "");
            AddWidget(mPageButton);
            // DONE as on the Options dialog: the blue image at the orange button's size.
            Image blue = Res.IMAGE_MM_BUTTON_BLUE;
            int w = Res.IMAGE_BUTTON_ORANGE.mWidth;
            int h = Res.IMAGE_BUTTON_ORANGE.mHeight;
            mDoneButton = new ThunderButton(blue, 1000, this);
            mDoneButton.mOverlayText = SexyAppBase.Loc("DONE");
            mDoneButton.mButtonImage = blue;
            mDoneButton.mComponentImage = blue;
            mDoneButton.mWidth = w;
            mDoneButton.mHeight = h / 2;
            mDoneButton.mNormalRect = new TRect(0, 0, w, h / 2);
            mDoneButton.mOverRect = new TRect(0, 0, w, h / 2);
            mDoneButton.mDownRect = new TRect(0, h / 2, w, h / 2);
            AddWidget(mDoneButton);
            ShowPage(PAGE_GENERAL);
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            base.Dispose();
        }

        /// <summary>Whether there's an App Icon page: where the launcher icon can be switched (Android), or to preview it.</summary>
        private static bool HasIconPage()
        {
            return Main.SetAppIconHandler != null || sDebugAppIcons;
        }

        private static string[] GetAppIconAliases()
        {
            string[] anAliases = new string[APP_ICONS.Length];
            for (int i = 0; i < anAliases.Length; i++)
            {
                anAliases[i] = APP_ICONS[i].mAlias;
            }
            return anAliases;
        }

        /// <summary>The page after thePage (General, Theme, then App Icon where there is one).</summary>
        private int NextPage(int thePage)
        {
            if (thePage == PAGE_GENERAL)
            {
                return PAGE_THEME;
            }
            return thePage == PAGE_THEME && mAppIconChecks != null ? PAGE_ICONS : PAGE_GENERAL;
        }

        /// <summary>Shows one page's options and points the page button at the next page.</summary>
        public void ShowPage(int thePage)
        {
            mPage = thePage;
            bool general = thePage == PAGE_GENERAL;
            bool theme = thePage == PAGE_THEME;
            mLazyLoadingCheckbox.SetVisible(general);
            mHighQualityPegsCheckbox.SetVisible(general);
            mRandomMusicCheckbox.SetVisible(general);
            mPCControlsCheckbox?.SetVisible(general);
            if (mAppIconChecks != null)
            {
                foreach (ThunderCheckbox aCheck in mAppIconChecks)
                {
                    aCheck.SetVisible(thePage == PAGE_ICONS);
                }
            }
            foreach (ThunderCheckbox aCheck in mMenuThemeChecks)
            {
                aCheck.SetVisible(theme);
            }
            foreach (ThunderCheckbox aCheck in mNightsMenuChecks)
            {
                aCheck.SetVisible(theme);
            }
            foreach (ThunderCheckbox aCheck in mMainMenuMusicChecks)
            {
                aCheck.SetVisible(theme);
            }
            int aNext = NextPage(thePage);
            mPageButton.mLabel = aNext == PAGE_THEME ? SexyAppBase.Loc("Theme Settings") :
                aNext == PAGE_ICONS ? SexyAppBase.Loc("App Icon") : SexyAppBase.Loc("General Settings");
            MarkDirty();
        }

        public override int GetPreferredHeight(int theWidth)
        {
            return SCREEN_HEIGHT;
        }

        private int GetLabelHeight()
        {
            return Res.FONT_BUTTON.GetHeight() + 10;
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
#if PEGGLE_IPHONE
            int aGutter = SexyUtil.gGutterWidth;
#else
            int aGutter = 0;
#endif
            int left = GetLeft() + aGutter + OPTIONS_CHECKBOX_LEFT_OFFSET;
            int top = OPTIONS_TOP + GetLabelHeight();
            ThunderCheckbox first = mMenuThemeChecks[0];
            first.Resize(left, top, OPTIONS_CHECKBOX_W, first.mHeight);
            // The Menu Theme column ends above the page button: its rows close up when they wouldn't fit.
            int doneH = mDoneButton.mNormalRect.mHeight;
            int doneY = OPTIONS_DONE_BT_Y + (SCREEN_HEIGHT - OPTIONS_DONE_BT_Y - doneH) / 2;
            int pageH = mPageButton.mNormalRect.mHeight;
            int pageY = doneY + (doneH - pageH) / 2;
            int aCount = mMenuThemeChecks.Length;
            int aStep = System.Math.Min(first.mHeight + 5, (pageY - 5 - top - first.mHeight) / (aCount - 1));
            for (int i = 1; i < aCount; i++)
            {
                mMenuThemeChecks[i].Resize(left, top + aStep * i, OPTIONS_CHECKBOX_W, first.mHeight);
            }
            mNightsMenuChecks[0].Layout(LAY_BESIDE | (int)LayoutFlags.LAY_SameHeight, first, 5, 0, 0, 0);
            for (int i = 1; i < mNightsMenuChecks.Length; i++)
            {
                mNightsMenuChecks[i].Layout(LAY_STACK_BELOW, mNightsMenuChecks[i - 1], 0, 5, 0, 0);
            }
            // Main Menu Theme goes under Nights Menu, below a heading of its own.
            mMainMenuMusicChecks[0].Layout(LAY_STACK_BELOW, mNightsMenuChecks[mNightsMenuChecks.Length - 1], 0, 5 + GetLabelHeight(), 0, 0);
            for (int i = 1; i < mMainMenuMusicChecks.Length; i++)
            {
                mMainMenuMusicChecks[i].Layout(LAY_STACK_BELOW, mMainMenuMusicChecks[i - 1], 0, 5, 0, 0);
            }
            // The first page's option takes the first spot of the left column.
            mLazyLoadingCheckbox.Resize(left, top, OPTIONS_CHECKBOX_W, first.mHeight);
            mHighQualityPegsCheckbox.Layout(LAY_BESIDE | (int)LayoutFlags.LAY_SameHeight, mLazyLoadingCheckbox, 5, 0, 0, 0);
            mRandomMusicCheckbox.Layout(LAY_STACK_BELOW, mHighQualityPegsCheckbox, 0, 5, 0, 0);
            mPCControlsCheckbox?.Layout(LAY_STACK_BELOW, mRandomMusicCheckbox, 0, 5, 0, 0);
            if (mAppIconChecks != null)
            {
                LayoutAppIcons(left, top, pageY - 10, (int)(first.mHeight * APP_ICON_CHECK_SCALE));
            }
            // DONE sits halfway between the Options dialog's spot for it and the bottom, for more room
            // above; the page button goes in its row, in the left column, at its own cel height.
            mDoneButton.Resize(OPTIONS_DONE_BT_X + aGutter * 2, doneY, mDoneButton.mNormalRect.mWidth, doneH);
            mPageButton.Resize(left, pageY, OPTIONS_CHECKBOX_W, pageH);
        }

        /// <summary>Where the App Icon page's pictures go (LayoutAppIcons), and the heading's top.</summary>
        private readonly TRect[] mAppIconPictures = new TRect[APP_ICON_PICTURES];

        private int mAppIconTop;

        private int mAppIconLeft;

        /// <summary>The App Icon page's width: both columns.</summary>
        private static int GetAppIconRowWidth()
        {
            return OPTIONS_CHECKBOX_W * 2 + 5;
        }

        /// <summary>
        /// The App Icon page: the pictures in rows (APP_ICON_ROWS) spread across both columns, each with its
        /// icons' (smaller) radio buttons stacked below it; the pictures are as big as the room down to
        /// theBottom allows, up to most of the narrowest slot's width.
        /// </summary>
        private void LayoutAppIcons(int theLeft, int theTop, int theBottom, int theCheckH)
        {
            int aRowW = GetAppIconRowWidth();
            int[] aStack = new int[APP_ICON_PICTURES];
            foreach (AppIcon anIcon in APP_ICONS)
            {
                aStack[anIcon.mPicture]++;
            }
            int aRows = APP_ICON_ROWS.Length;
            int aButtonsH = 0;
            int aNarrowest = aRowW;
            foreach (int[] aRow in APP_ICON_ROWS)
            {
                int aMost = 0;
                foreach (int aPicture in aRow)
                {
                    aMost = Math.Max(aMost, aStack[aPicture]);
                }
                aButtonsH += 5 + aMost * (theCheckH + 5);
                aNarrowest = Math.Min(aNarrowest, aRowW / aRow.Length);
            }
            const int ROW_GAP = 10;
            int anIconSize = Math.Min(aNarrowest * 3 / 4, (theBottom - theTop - aButtonsH - ROW_GAP * (aRows - 1)) / aRows);
            int y = theTop;
            int[] aStacked = new int[APP_ICON_PICTURES];
            for (int r = 0; r < aRows; r++)
            {
                int[] aRow = APP_ICON_ROWS[r];
                int aCount = aRow.Length;
                int aButtonW = aRowW / aCount;
                int aMost = 0;
                for (int k = 0; k < aCount; k++)
                {
                    int aPicture = aRow[k];
                    int aCenterX = theLeft + aRowW * (2 * k + 1) / (2 * aCount);
                    int aSlotX = aCenterX - aButtonW / 2;
                    mAppIconPictures[aPicture] = new TRect(aCenterX - anIconSize / 2, y, anIconSize, anIconSize);
                    aMost = Math.Max(aMost, aStack[aPicture]);
                    for (int i = 0; i < APP_ICONS.Length; i++)
                    {
                        if (APP_ICONS[i].mPicture == aPicture)
                        {
                            int aY = y + anIconSize + 5 + aStacked[aPicture]++ * (theCheckH + 5);
                            mAppIconChecks[i].Resize(aSlotX + 3, aY, aButtonW - 6, theCheckH);
                        }
                    }
                }
                y += anIconSize + 5 + aMost * (theCheckH + 5) + ROW_GAP;
            }
            mAppIconTop = theTop;
            mAppIconLeft = theLeft;
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            if (mPage == PAGE_ICONS)
            {
                DrawAppIcons(g);
                return;
            }
            if (mPage == PAGE_THEME)
            {
                DrawColumnLabel(g, mMenuThemeLabel, mMenuThemeChecks[0]);
                DrawColumnLabel(g, mNightsMenuLabel, mNightsMenuChecks[0]);
                DrawColumnLabel(g, mMainMenuMusicLabel, mMainMenuMusicChecks[0]);
                return;
            }
            g.SetFont(Res.FONT_SMALL);
            g.SetColor(SexyUtil.MakeColor(0x9090d0));
            Font font = g.GetFont();
            ThunderCheckbox lazy = mLazyLoadingCheckbox;
            g.DrawString(mRestartLabel, lazy.mX + (lazy.mWidth - font.StringWidth(mRestartLabel)) / 2, lazy.mY + lazy.mHeight + font.GetAscent() + 2);
        }

        /// <summary>The App Icon page: its heading, and each icon's picture centred over its radio button.</summary>
        private void DrawAppIcons(Graphics g)
        {
            g.SetFont(Res.FONT_BUTTON);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            SexyUtil.SetButtonFontColor(0xd, 1.0f);
            Font font = g.GetFont();
            int aRowW = GetAppIconRowWidth();
            g.DrawString(mAppIconLabel, mAppIconLeft + (aRowW - font.StringWidth(mAppIconLabel)) / 2,
                mAppIconTop - GetLabelHeight() + font.GetAscent());
            if (mAppIconSheet == null)
            {
                return;
            }
            int aCel = mAppIconSheet.mHeight;
            g.SetColor(Microsoft.Xna.Framework.Color.White);
            for (int i = 0; i < APP_ICON_PICTURES; i++)
            {
                g.DrawImage(mAppIconSheet, mAppIconPictures[i], new TRect(i * aCel, 0, aCel, aCel));
            }
        }

        /// <summary>A column's heading, centred over its first option.</summary>
        private void DrawColumnLabel(Graphics g, string theLabel, ThunderCheckbox theFirst)
        {
            g.SetFont(Res.FONT_BUTTON);
            g.SetColor(SexyUtil.MakeColor(0xffffff));
            SexyUtil.SetButtonFontColor(0xd, 1.0f);
            Font font = g.GetFont();
            int y = theFirst.mY - GetLabelHeight() + font.GetAscent();
            g.DrawString(theLabel, theFirst.mX + (theFirst.mWidth - font.StringWidth(theLabel)) / 2, y);
        }

        public void CheckboxChecked(int theId, bool @checked)
        {
            if (mAppIconChecks != null && theId >= PortOptions_AppIcon && theId < PortOptions_AppIcon + mAppIconChecks.Length)
            {
                for (int i = 0; i < mAppIconChecks.Length; i++)
                {
                    mAppIconChecks[i].mChecked = PortOptions_AppIcon + i == theId;
                }
                // Only picked here: DONE applies it (closing the game, after a warning).
                mAppIconPicked = APP_ICONS[theId - PortOptions_AppIcon].mAlias;
                return;
            }
            if (theId >= PortOptions_MenuTheme && theId < PortOptions_MenuTheme + mMenuThemeChecks.Length)
            {
                for (int i = 0; i < mMenuThemeChecks.Length; i++)
                {
                    mMenuThemeChecks[i].mChecked = PortOptions_MenuTheme + i == theId;
                }
                mApp.SetMenuTheme(theId - PortOptions_MenuTheme);
            }
            else if (theId >= PortOptions_NightsMenu && theId < PortOptions_NightsMenu + mNightsMenuChecks.Length)
            {
                for (int i = 0; i < mNightsMenuChecks.Length; i++)
                {
                    mNightsMenuChecks[i].mChecked = PortOptions_NightsMenu + i == theId;
                }
                mApp.SetNightsMenuPC(theId == PortOptions_NightsMenu + 1);
            }
            else if (theId >= PortOptions_MainMenuMusic && theId < PortOptions_MainMenuMusic + mMainMenuMusicChecks.Length)
            {
                for (int i = 0; i < mMainMenuMusicChecks.Length; i++)
                {
                    mMainMenuMusicChecks[i].mChecked = PortOptions_MainMenuMusic + i == theId;
                }
                mApp.SetMainMenuAmbience(theId == PortOptions_MainMenuMusic);
            }
            else if (theId == PortOptions_LazyLoading)
            {
                mApp.SetLazyLoading(@checked);
            }
            else if (theId == PortOptions_HighQualityPegs)
            {
                mApp.SetHighQualityPegs(@checked);
            }
            else if (theId == PortOptions_RandomMusic)
            {
                mApp.SetRandomMusic(@checked);
            }
            else if (theId == PortOptions_PCControls)
            {
                mApp.SetPCControls(@checked);
            }
        }

        public override void ButtonPress(int theId)
        {
            if (!IsScrolling())
            {
                base.ButtonPress(theId);
            }
        }

        /// <summary>
        /// The confirm dialog's OK: Android switches the launcher to the picked icon's alias (the adaptive icon),
        /// which closes the game; the preview (SEXY_APP_ICONS) just closes the dialog as DONE does.
        /// </summary>
        public void ApplyAppIcon()
        {
            mAppIconCurrent = mAppIconPicked;
            if (Main.SetAppIconHandler != null)
            {
                Main.SetAppIconHandler(mAppIconPicked, GetAppIconAliases());
                return;
            }
            base.ButtonDepress(mDoneButton.mId);
        }

        /// <summary>The confirm dialog's Cancel: the selection goes back to the icon the launcher shows.</summary>
        public void RestoreAppIcon()
        {
            mAppIconPicked = mAppIconCurrent;
            for (int i = 0; i < mAppIconChecks.Length; i++)
            {
                mAppIconChecks[i].mChecked = APP_ICONS[i].mAlias == mAppIconCurrent;
            }
            MarkDirty();
        }

        public override void ButtonDepress(int theId)
        {
            if (IsScrolling())
            {
                return;
            }
            if (theId == mDoneButton.mId && mAppIconChecks != null && mAppIconPicked != mAppIconCurrent)
            {
                // A new icon: warn that the game closes to change it (OK applies it, Cancel restores the selection).
                mApp.DoConfirmAppIconDialog();
                return;
            }
            if (theId == PortOptions_Page)
            {
                ShowPage(NextPage(mPage));
                return;
            }
            base.ButtonDepress(theId);
        }
    }
}
