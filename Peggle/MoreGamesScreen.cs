using System;
using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    /// <summary>
    /// The More Games screen (Sexy::MoreGamesScreen, id 44): a full-screen ThunderDialog with Warren,
    /// a blurb and a scrolling list of game banners read from net_content/more_games.xml,
    /// plus a BACK button.
    /// </summary>
    public class MoreGamesScreen : ThunderDialog, INavHandler
    {
        public ThunderballApp mApp;                               // 0x128

        public ThunderButton mBackButton;                         // 0x12c

        public MoreGamesListWidget mListWidget;                   // 0x130

        public ThunderScrollWidget mScrollWidget;                 // 0x134

        public string mLexText = "";                              // 0x138

        // 0019888c
        public MoreGamesScreen(ThunderballApp theApp)
            : base(DialogId.DIALOG_CREDITS, true, SexyAppBase.Loc("More Games!"), "", "", 0)
        {
            mApp = theApp;
            mListWidget = new MoreGamesListWidget(theApp);
            mScrollWidget = new ThunderScrollWidget();
            AddWidget(mScrollWidget);
            mScrollWidget.AddWidget(mListWidget);
            Image orange = Res.IMAGE_BUTTON_ORANGE;
            int w = orange.mWidth;
            int h = orange.mHeight;
            mBackButton = new ThunderButton(orange, 0, this);
            mBackButton.mOverlayText = SexyAppBase.Loc("BACK"); // 0x138
            mBackButton.mButtonImage = orange;                  // 0x88
            mBackButton.mComponentImage = orange;               // 0xf8
            mBackButton.mWidth = w;
            mBackButton.mHeight = h / 2;
            mBackButton.mNormalRect = new TRect(0, 0, w, h / 2);
            mBackButton.mOverRect = new TRect(0, 0, w, h / 2);
            mBackButton.mDownRect = new TRect(0, h / 2, w, h / 2);
            AddWidget(mBackButton);
            LoadGames();
        }

        // 00198eb8
        public void LoadGames()
        {
            // Port: the original read net_content/<res>/ per language; the port's banners are identical
            // across versions and untranslated, so they live once at the content root.
            string aDir = "net_content/";
            XMLParser aParser = new XMLParser();
            if (!aParser.OpenFile(aDir + "more_games.xml"))
            {
                return;
            }
            string aText = "";
            XMLElement anElement = new XMLElement();
            while (aParser.NextElement(ref anElement))
            {
                if (anElement.mType == XMLElementType.TYPE_START)
                {
                    aText = "";
                    if (anElement.mValue == "entry")
                    {
                        anElement.mAttributes.TryGetValue("image", out string anImageName);
                        anElement.mAttributes.TryGetValue("link", out string aLink);
                        if (!string.IsNullOrEmpty(anImageName) && !string.IsNullOrEmpty(aLink))
                        {
                            Image anImage = mApp.GetImage(aDir + anImageName, false);
                            if (anImage != null)
                            {
                                mListWidget.AddRow(anImage, GetPortLink(anImageName, aLink));
                            }
                        }
                    }
                }
                else if (anElement.mType == XMLElementType.TYPE_END)
                {
                    if (anElement.mValue == "lexText")
                    {
                        mLexText = aText;
                    }
                    aText = "";
                }
                else if (anElement.mType == XMLElementType.TYPE_ELEMENT)
                {
                    aText += anElement.mValue;
                }
            }
        }

        /// <summary>
        /// Port addition: Some games have different links depending on their platform. This function
        /// returns the appropiate link for defined games.
        /// </summary>
        private static string GetPortLink(string theImageName, string theLink)
        {
            string aName = theImageName.ToLowerInvariant();
            string aWeb;
            string aMobile;
            if (aName.Contains("chuzzle2"))
            {
                aWeb = "https://store.steampowered.com/app/4157840/Chuzzle_2/";
                aMobile = "https://play.google.com/store/apps/details?id=com.raptisoft.Chuzzle2";
            }
            else { 
                return theLink;
            }
            if (OperatingSystem.IsAndroid())
            {
                return aMobile;
            }
            else
            {
                return aWeb;
            }
        }

        // 00199828
        public override void Dispose()
        {
            RemoveAllWidgets(true, true);
            base.Dispose();
        }

        // 0019997c
        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int aGap = DIALOG_MORE_GAMES_GAP_X;
            int x = CHARACTER_IMAGE_STORY_SCREEN_OFFSET_X[7] + Res.IMAGE_WAREN_LARGE.mWidth + aGap;
            int y = GetTop() + Res.FONT_PLAINOUTLINE.GetHeight() * 3;
            int w = mWidth - x - aGap;
            mListWidget.Resize(0, 0, w, mListWidget.GetPreferredHeight());
            mScrollWidget.Resize(x, y, w, mHeight - y - mBackgroundInsets.mBottom);
            mBackButton.Move(MOREGAMES_BACK_BUTTON_X, MOREGAMES_BACK_BUTTON_Y);
        }

        // 00199ae4
        public override void ButtonDepress(int theId)
        {
            mApp.DoToMenu();
        }

        // 00199b1c
        public override void Draw(Graphics g)
        {
            base.Draw(g);
#if PEGGLE_IPHONE
            // Port addition (ultrawide): Warren stays in the window's corner when it is wider than the game.
            Graphics aWide = g.Create();
            aWide.ClearClipRect();
            aWide.DrawImage(Res.IMAGE_WAREN_LARGE, CHARACTER_IMAGE_STORY_SCREEN_OFFSET_X[7] + GetLeftCornerShift(), CHARACTER_IMAGE_STORY_SCREEN_OFFSET_Y[7]);
#else
            g.DrawImage(Res.IMAGE_WAREN_LARGE, CHARACTER_IMAGE_STORY_SCREEN_OFFSET_X[7], CHARACTER_IMAGE_STORY_SCREEN_OFFSET_Y[7]);
#endif
            g.SetFont(Res.FONT_PLAINOUTLINE);
            g.SetColor(Color.White);
            g.WriteWordWrapped(new TRect(mScrollWidget.mX, GetTop(), mScrollWidget.mWidth, Res.FONT_PLAINOUTLINE.GetHeight() * 2), mLexText, -1, 0);
            TRect aRect = mScrollWidget.GetRect();
            aRect.Inflate(6, 6);
            g.SetColorizeImages(true);
            g.SetColor(SexyUtil.MakeColor(0, 0x7f));
            SexyUtil.MyDrawImageBox(g, aRect, Res.IMAGE_DLG_TEXTRECT);
            g.SetColorizeImages(false);
        }

        // ---------------------------------------------------------------- port addition: menu navigation

        /// <summary>Port addition (menu navigation): B is the back button.</summary>
        bool INavHandler.NavAction(NavAction theAction)
        {
            if (theAction == NavAction.Back)
            {
                MenuNav.Press(mBackButton);
                return true;
            }
            return false;
        }
    }
}
