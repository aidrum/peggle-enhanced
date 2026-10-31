using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace Sexy
{
    /// <summary>
    /// Loads SexyApp-style images from Content/. A colour image "dir/name.ext" may come with a
    /// separate greyscale alpha mask "dir/name_.ext" or "dir/_name.ext"; a mask with no colour
    /// image is drawn as white with that alpha (used by fonts and character masks).
    /// The returned texture is premultiplied, as the renderer (inherited from
    /// PlantsVsZombies.NET, whose XNB textures were premultiplied at build time) expects.
    /// Textures decoded from the bundle's .tex/.txz files are already premultiplied; the converter
    /// lists them in Content/premultiplied.txt, and they are uploaded as they are.
    /// </summary>
    public static class ImageFileLoader
    {
        public delegate uint[] Decoder(byte[] theFileData, out int theWidth, out int theHeight);

        /// <summary>
        /// Optional platform image decoder returning straight-alpha pixels packed as SurfaceFormat.Color
        /// (null to fall back). Android sets one that decodes on the CPU: Texture2D.FromStream plus GetData
        /// uploads every image to the GPU and reads it back, which was the slow part of loading screens.
        /// </summary>
        public static Decoder PlatformDecoder;
        private static uint[] ReadPixels(GraphicsDevice theDevice, string theFullPath, out int theWidth, out int theHeight)
        {
            try
            {
                if (PlatformDecoder != null)
                {
                    uint[] decoded = PlatformDecoder(ContentFS.ReadResolvedBytes(theFullPath), out theWidth, out theHeight);
                    if (decoded != null)
                    {
                        return decoded;
                    }
                }
                using (Stream s = ContentFS.OpenResolved(theFullPath))
                using (Texture2D tex = Texture2D.FromStream(theDevice, s))
                {
                    theWidth = tex.Width;
                    theHeight = tex.Height;
                    uint[] pixels = new uint[theWidth * theHeight];
                    tex.GetData(pixels);
                    return pixels;
                }
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException("Can't decode image " + theFullPath, e);
            }
        }

        private static System.Collections.Generic.HashSet<string> sPremultipliedFiles;

        /// <summary>True when the file (an absolute Content path) already holds premultiplied pixels.</summary>
        private static bool IsPremultipliedFile(string theFullPath)
        {
            if (sPremultipliedFiles == null)
            {
                sPremultipliedFiles = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                byte[] list = ContentFS.ReadAllBytes("premultiplied.txt");
                if (list != null)
                {
                    foreach (string rawLine in System.Text.Encoding.UTF8.GetString(list).Split('\n'))
                    {
                        string line = rawLine.TrimEnd('\r');
                        if (line.Length > 0 && line[0] != '#')
                        {
                            sPremultipliedFiles.Add(line.Trim());
                        }
                    }
                }
            }
            if (theFullPath == null || !theFullPath.StartsWith(ContentFS.Root, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return sPremultipliedFiles.Contains(theFullPath.Substring(ContentFS.Root.Length).TrimStart('/', '\\').Replace('\\', '/'));
        }

        public static Texture2D Load(GraphicsDevice theDevice, string thePath)
        {
            uint[] pixels = LoadBits(theDevice, thePath, out int w, out int h, out bool premultiplied);
            if (pixels == null)
            {
                return null;
            }
            if (!premultiplied)
            {
                Premultiply(pixels);
            }
            Texture2D result = new Texture2D(theDevice, w, h, false, SurfaceFormat.Color);
            result.SetData(pixels);
            return result;
        }

        /// <summary>
        /// Loads an atlas page and copies each entry's edge pixels one texel outward into the free gap
        /// around it. Filtering at an entry's edge then reads its own edge instead of the transparent
        /// gap, as it would on a standalone image with clamped addressing; without this, scaling to a
        /// non-native resolution showed thin dark seams where pieces of the HUD meet. Gap pixels that
        /// two entries would both claim are left alone.
        /// </summary>
        public static Texture2D LoadAtlas(GraphicsDevice theDevice, string thePath, System.Collections.Generic.List<Microsoft.Xna.Framework.Rectangle> theRects)
        {
            uint[] pixels = LoadBits(theDevice, thePath, out int w, out int h, out bool premultiplied);
            if (pixels == null)
            {
                return null;
            }
            ExtrudeEdges(pixels, w, h, theRects);
            if (!premultiplied)
            {
                Premultiply(pixels);
            }
            Texture2D result = new Texture2D(theDevice, w, h, false, SurfaceFormat.Color);
            result.SetData(pixels);
            return result;
        }

        private static void ExtrudeEdges(uint[] pixels, int w, int h, System.Collections.Generic.List<Microsoft.Xna.Framework.Rectangle> theRects)
        {
            const int FREE = -1;
            const int CONFLICT = -2;
            int[] owner = new int[w * h];
            Array.Fill(owner, FREE);
            foreach (Microsoft.Xna.Framework.Rectangle r in theRects)
            {
                for (int y = Math.Max(0, r.Top); y < Math.Min(h, r.Bottom); y++)
                {
                    for (int x = Math.Max(0, r.Left); x < Math.Min(w, r.Right); x++)
                    {
                        owner[y * w + x] = int.MaxValue;
                    }
                }
            }
            // Claim the ring of gap pixels around each entry; a pixel two entries want is dropped.
            int[] source = new int[w * h];
            for (int i = 0; i < theRects.Count; i++)
            {
                Microsoft.Xna.Framework.Rectangle r = theRects[i];
                if (r.Width <= 0 || r.Height <= 0)
                {
                    continue;
                }
                for (int y = r.Top - 1; y <= r.Bottom; y++)
                {
                    for (int x = r.Left - 1; x <= r.Right; x++)
                    {
                        if (x < 0 || y < 0 || x >= w || y >= h || (x >= r.Left && x < r.Right && y >= r.Top && y < r.Bottom))
                        {
                            continue;
                        }
                        int p = y * w + x;
                        if (owner[p] == FREE)
                        {
                            owner[p] = i;
                            int sx = Math.Clamp(x, r.Left, r.Right - 1);
                            int sy = Math.Clamp(y, r.Top, r.Bottom - 1);
                            source[p] = sy * w + sx;
                        }
                        else if (owner[p] >= 0 && owner[p] != int.MaxValue && owner[p] != i)
                        {
                            owner[p] = CONFLICT;
                        }
                    }
                }
            }
            for (int p = 0; p < owner.Length; p++)
            {
                if (owner[p] >= 0 && owner[p] != int.MaxValue)
                {
                    pixels[p] = pixels[source[p]];
                }
            }
        }

        /// <summary>
        /// The straight-alpha pixels of an image and its alpha mask (ImageLib::GetImage with
        /// lookForAlphaImage), packed as SurfaceFormat.Color uints. Null when neither file exists.
        /// </summary>
        public static uint[] LoadBits(GraphicsDevice theDevice, string thePath, out int theWidth, out int theHeight)
        {
            uint[] pixels = LoadBits(theDevice, thePath, out theWidth, out theHeight, out bool premultiplied);
            if (pixels != null && premultiplied)
            {
                Unpremultiply(pixels);
            }
            return pixels;
        }

        /// <summary>
        /// Like LoadBits, but leaves already-premultiplied files (see IsPremultipliedFile) as they are and
        /// says so. A file with a separate alpha mask is straight.
        /// </summary>
        private static uint[] LoadBits(GraphicsDevice theDevice, string thePath, out int theWidth, out int theHeight, out bool thePremultiplied)
        {
            thePremultiplied = false;
            theWidth = 0;
            theHeight = 0;
            string colorPath = ContentFS.ResolveImage(thePath);
            string alphaPath = FindAlphaMask(thePath);
            if (colorPath == null && alphaPath == null)
            {
                return null;
            }
            int w;
            int h;
            uint[] pixels;
            if (colorPath != null)
            {
                pixels = ReadPixels(theDevice, colorPath, out w, out h);
            }
            else
            {
                ReadPixels(theDevice, alphaPath, out w, out h);
                pixels = new uint[w * h];
                Array.Fill(pixels, 0xFFFFFFFFu);
            }
            thePremultiplied = colorPath != null && alphaPath == null && IsPremultipliedFile(colorPath);
            if (alphaPath != null)
            {
                uint[] alpha = ReadPixels(theDevice, alphaPath, out int aw, out int ah);
                if (aw == w && ah == h)
                {
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        // SurfaceFormat.Color is ABGR in a uint; the mask's red channel is its alpha.
                        pixels[i] = (pixels[i] & 0x00FFFFFF) | ((alpha[i] & 0xFF) << 24);
                    }
                }
            }
            theWidth = w;
            theHeight = h;
            return pixels;
        }

        public static void Premultiply(uint[] pixels)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                uint p = pixels[i];
                uint a = p >> 24;
                if (a == 255)
                {
                    continue;
                }
                uint r = (p & 0xFF) * a / 255;
                uint g = ((p >> 8) & 0xFF) * a / 255;
                uint b = ((p >> 16) & 0xFF) * a / 255;
                pixels[i] = (a << 24) | (b << 16) | (g << 8) | r;
            }
        }

        public static void Unpremultiply(uint[] pixels)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                uint p = pixels[i];
                uint a = p >> 24;
                if (a == 255 || a == 0)
                {
                    continue;
                }
                uint r = Math.Min(255u, ((p & 0xFF) * 255 + a / 2) / a);
                uint g = Math.Min(255u, (((p >> 8) & 0xFF) * 255 + a / 2) / a);
                uint b = Math.Min(255u, (((p >> 16) & 0xFF) * 255 + a / 2) / a);
                pixels[i] = (a << 24) | (b << 16) | (g << 8) | r;
            }
        }

        public static string FindAlphaMask(string thePath)
        {
            string p = ContentFS.Normalize(thePath);
            string dir = Path.GetDirectoryName(p)?.Replace('\\', '/') ?? "";
            string name = Path.GetFileNameWithoutExtension(p);
            string prefix = dir.Length > 0 ? dir + "/" : "";
            foreach (string candidate in new[] { prefix + name + "_", prefix + "_" + name })
            {
                foreach (string ext in new[] { ".gif", ".png", ".jpg" })
                {
                    string found = ContentFS.ResolveImage(candidate + ext);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }
    }
}
