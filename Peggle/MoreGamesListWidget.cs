using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// The More Games list (Sexy::MoreGamesListWidget): one banner image per game, stacked 2px apart and
    /// centred; tapping a banner opens its link.
    /// </summary>
    public class MoreGamesListWidget : Widget, INavCells
    {
        public class GameInfo
        {
            public Image mImage;                                  // 0x00

            public string mLink = "";                             // 0x04
        }

        public ThunderballApp mApp;                               // 0x78

        public List<GameInfo> mGames = new List<GameInfo>();      // 0x7c

        // 00198204
        public MoreGamesListWidget(ThunderballApp theApp)
        {
            mApp = theApp;
        }

        // 0019840c
        public override void Dispose()
        {
            foreach (GameInfo aGame in mGames)
            {
                aGame.mImage?.Dispose();
            }
            mGames.Clear();
            base.Dispose();
        }

        // 00198598
        public int GetPreferredHeight()
        {
            int aHeight = 0;
            foreach (GameInfo aGame in mGames)
            {
                aHeight += aGame.mImage.mHeight;
            }
            return (mGames.Count - 1) * 2 + aHeight;
        }

        // 001985fc
        public override void Draw(Graphics g)
        {
            int y = 0;
            foreach (GameInfo aGame in mGames)
            {
                g.DrawImage(aGame.mImage, mWidth / 2 - aGame.mImage.mWidth / 2, y);
                y += aGame.mImage.mHeight + 2;
            }
        }

        // 00198698
        public override void MouseUp(int x, int y, int theClickCount)
        {
            int aTop = 0;
            foreach (GameInfo aGame in mGames)
            {
                if (aTop <= y && y < aGame.mImage.mHeight + aTop)
                {
                    mApp.OpenURL(aGame.mLink);
                    return;
                }
                aTop += aGame.mImage.mHeight + 2;
            }
        }

        // 00198748
        public void AddRow(Image theImage, string theLink)
        {
            mGames.Add(new GameInfo { mImage = theImage, mLink = theLink });
        }

        // ---------------------------------------------------------------- port addition: menu navigation

        /// <summary>Port addition (menu navigation): each game banner.</summary>
        public int GetNavCellCount()
        {
            return mGames.Count;
        }

        public TRect GetNavCellRect(int theCell)
        {
            int y = 0;
            for (int i = 0; i < theCell; i++)
            {
                y += mGames[i].mImage.mHeight + 2;
            }
            Image anImage = mGames[theCell].mImage;
            return new TRect(mWidth / 2 - anImage.mWidth / 2, y, anImage.mWidth, anImage.mHeight);
        }

        /// <summary>A on a banner: a click on it.</summary>
        public void NavCellActivate(int theCell)
        {
            TRect aRect = GetNavCellRect(theCell);
            MouseUp(aRect.mX + aRect.mWidth / 2, aRect.mY + aRect.mHeight / 2, 1);
        }

        public override bool IsNavigable()
        {
            return true;
        }
    }
}
