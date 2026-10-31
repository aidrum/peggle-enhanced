namespace Sexy
{
    /// <summary>
    /// Port addition: the fireplace of the PC Peggle Nights main menu (InstituteLounge draw, 0x439aad-0x439d9b),
    /// drawn over the Nights Menu option's PC art (nights_pc). The PC draws the firelight glow and then ten
    /// flames, additively; their alpha and size flicker by fixed integer hashes of the lounge's frame
    /// counter (+0xa0, one per update), and each flame keeps its spot on the logs. nights_pc is the PC
    /// lounge background (mainmenu.jp2) redrawn at another scale and offset, so the PC's lounge
    /// coordinates are mapped onto it.
    /// </summary>
    public class NightsFireplace
    {
#if PEGGLE_IPHONE
        /// <summary>nights_pc pixels per PC lounge pixel, and where the lounge's top-left falls in nights_pc.</summary>
        private const float LOUNGE_SCALE = 0.998f;
        private const float LOUNGE_X = 338.8f;
        private const float LOUNGE_Y = -19.4f;
#else
        private const float LOUNGE_SCALE = 1.149f;
        private const float LOUNGE_X = 156.4f;
        private const float LOUNGE_Y = -22.7f;
#endif

        private static readonly string[] FLAME_CELS = { "FLAME_01", "FLAME_02", "FLAME_03", "FLAME_04" };

        private PCSheet mSheet;

        /// <summary>The fireplace, or null when its sheet is missing.</summary>
        public static NightsFireplace Load()
        {
            PCSheet aSheet = PCSheet.Load("images/pc/nights_fire", LOUNGE_SCALE);
            return aSheet == null ? null : new NightsFireplace { mSheet = aSheet };
        }

        /// <summary>
        /// Draws the fire over nights_pc, whose top-left is at (theX, theY) zoomed by theZoom; theCounter
        /// is the menu's update count.
        /// </summary>
        public void Draw(Graphics g, float theX, float theY, float theZoom, int theCounter)
        {
            int c = theCounter;
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            g.SetColorizeImages(true);
            // The firelight (alpha 174-215) at lounge (0x40, 0xe0).
            int anAlpha = ((c / 16) * 0x529 + (c / 13) * 0x305) % 0x2a + 0xae;
            g.SetColor(new Microsoft.Xna.Framework.Color(255, 255, 255, anAlpha));
            DrawCel(g, "FIRELIGHT", 0x40, 0xe0, -1, -1, theX, theY, theZoom);
            int aFlame = 7;
            int aSizeY = 0x70;
            int aSizeX = 0x20;
            int aBlink = 0x5d0;
            int aPosY = 0xa25;
            int aPosX = 0x4ad;
            for (; aBlink < 0x706; aBlink += 0x1f)
            {
                anAlpha = ((c / 7) * 0x529 + aBlink + (c / 6) * 0x305) % 0x35 + 0xc9;
                g.SetColor(new Microsoft.Xna.Framework.Color(255, 255, 255, anAlpha));
                string aCel = FLAME_CELS[aFlame & 3];
                float aScaleX = ((((aFlame + (c / 3) * 3) / 18) * (aSizeX + 200)) % 11 + 0x46) / 75.0f;
                float aScaleY = (((((c / 2) * 7 + aSizeY) / 16) * aSizeX) % 7 + 0x17) / 26.0f;
                // The cel sizes at PC scale (the sheet is resampled to nights_pc's).
                TRect aSrc = mSheet[aCel];
                float w = aSrc.mWidth / LOUNGE_SCALE;
                float h = aSrc.mHeight / LOUNGE_SCALE;
                int aW = (int)(w * aScaleX);
                int aH = (int)(h * aScaleY);
                // Centred on a spot on the logs, its foot a fifth of its height below it.
                int x = (int)((aPosX % 0x49 + 0x1e8) - w * 0.5f * aScaleX);
                int y = (int)((aPosY % 0x2f + 0x1a4) - h * 0.8f * aScaleY);
                DrawCel(g, aCel, x, y, aW, aH, theX, theY, theZoom);
                aFlame++;
                aSizeY += 0x10;
                aSizeX += 3;
                aPosY += 0x173;
                aPosX += 0xab;
            }
            g.SetColorizeImages(false);
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
        }

        /// <summary>A cel at lounge (x, y), lounge size theW x theH (-1: its own), mapped onto nights_pc.</summary>
        private void DrawCel(Graphics g, string theCel, int x, int y, int theW, int theH, float theX, float theY, float theZoom)
        {
            TRect aSrc = mSheet[theCel];
            float aScale = LOUNGE_SCALE * theZoom;
            float aLeft = theX + (LOUNGE_X + x * LOUNGE_SCALE) * theZoom;
            float aTop = theY + (LOUNGE_Y + y * LOUNGE_SCALE) * theZoom;
            float aW = theW < 0 ? aSrc.mWidth * theZoom : theW * aScale;
            float aH = theH < 0 ? aSrc.mHeight * theZoom : theH * aScale;
            g.DrawImage(mSheet.mImage, new TRect((int)aLeft, (int)aTop, (int)(aW + 0.5f), (int)(aH + 0.5f)), aSrc);
        }
    }
}
