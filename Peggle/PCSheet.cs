using System;
using System.Collections.Generic;
using System.Text;

namespace Sexy
{
    /// <summary>
    /// Port addition: a sheet of PC (800x600-scale) art made by a tools/build_*_assets.py script: the image
    /// (images/pc/name.png), resampled at load by display height / 600 like the other PC art, and its
    /// index (name.txt, "NAME x y w h" per cel at PC scale), so cel rects aren't repeated in code.
    /// </summary>
    public class PCSheet
    {
        public Image mImage;

        /// <summary>What the PC art was resampled by.</summary>
        public double mScale;

        private readonly Dictionary<string, TRect> mCels = new Dictionary<string, TRect>();

        /// <summary>The sheet at thePath (no extension), or null when its image or index is missing.</summary>
        public static PCSheet Load(string thePath)
        {
            return Load(thePath, SexyUtil.gPhysicalDisplayHeight / 600.0);
        }

        /// <summary>The sheet at thePath resampled by theScale instead of display height / 600.</summary>
        public static PCSheet Load(string thePath, double theScale)
        {
            if (!ContentFS.Exists(thePath + ".txt"))
            {
                return null;
            }
            Image anImage = SexyUtil.LoadPCImage(thePath, theScale);
            if (anImage == null)
            {
                return null;
            }
            PCSheet aSheet = new PCSheet();
            aSheet.mImage = anImage;
            aSheet.mScale = theScale;
            string aText = Encoding.UTF8.GetString(ContentFS.ReadAllBytes(thePath + ".txt"));
            foreach (string aLine in aText.Split('\n'))
            {
                string[] aParts = aLine.Trim().Split(' ');
                if (aParts.Length == 5)
                {
                    aSheet.mCels[aParts[0]] = SexyUtil.ScalePCRect(new TRect(int.Parse(aParts[1]), int.Parse(aParts[2]), int.Parse(aParts[3]), int.Parse(aParts[4])), theScale);
                }
            }
            return aSheet;
        }

        /// <summary>A cel's rect in mImage (scaled).</summary>
        public TRect this[string theCel]
        {
            get { return mCels[theCel]; }
        }

        public void Dispose()
        {
            mImage?.Dispose();
            mImage = null;
        }
    }
}
