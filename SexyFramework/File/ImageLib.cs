using System;
using Microsoft.Xna.Framework.Graphics;

namespace Sexy
{
    /// <summary>
    /// A CPU-side image (ImageLib::Image): width, height and straight-alpha 32-bit pixels. The
    /// channel order is SurfaceFormat.Color's (ABGR in a uint); alpha is the top byte as in the
    /// original's ARGB layout, which is all the pixel code that uses these images relies on.
    /// </summary>
    public class ImageLibImage
    {
        public int mWidth;                                        // 0x04

        public int mHeight;                                       // 0x08

        public uint[] mBits;                                      // 0x0c

        public ImageLibImage()
        {
        }

        public ImageLibImage(int theWidth, int theHeight)
        {
            mWidth = theWidth;
            mHeight = theHeight;
            mBits = new uint[theWidth * theHeight];
        }

        public uint[] GetBits()
        {
            return mBits;
        }

        /// <summary>Uploads the pixels as-is (they must already be premultiplied) into a drawable image.</summary>
        public Image CreateImage()
        {
            Texture2D aTexture = new Texture2D(GlobalStaticVars.g.GraphicsDevice, mWidth, mHeight, false, SurfaceFormat.Color);
            aTexture.SetData((uint[])mBits.Clone());
            return new Image(aTexture);
        }
    }

    public static class ImageLib
    {
        /// <summary>ImageLib::GetImage: loads a colour image and its "_" alpha mask without premultiplying.</summary>
        public static ImageLibImage GetImage(string theFileName, bool lookForAlphaImage)
        {
            uint[] aBits = ImageFileLoader.LoadBits(GlobalStaticVars.g.GraphicsDevice, theFileName, out int aWidth, out int aHeight);
            if (aBits == null)
            {
                return null;
            }
            ImageLibImage anImage = new ImageLibImage();
            anImage.mWidth = aWidth;
            anImage.mHeight = aHeight;
            anImage.mBits = aBits;
            return anImage;
        }

        /// <summary>
        /// Port addition: resamples an image to theWidth x theHeight with a Catmull-Rom filter (widened
        /// when shrinking), one axis at a time. The pixels should be premultiplied.
        /// </summary>
        public static ImageLibImage Resize(ImageLibImage theImage, int theWidth, int theHeight)
        {
            int aSrcW = theImage.mWidth;
            int aSrcH = theImage.mHeight;
            float[] aSrc = new float[aSrcW * aSrcH * 4];
            for (int i = 0; i < aSrcW * aSrcH; i++)
            {
                uint p = theImage.mBits[i];
                aSrc[i * 4] = p & 0xFF;
                aSrc[i * 4 + 1] = (p >> 8) & 0xFF;
                aSrc[i * 4 + 2] = (p >> 16) & 0xFF;
                aSrc[i * 4 + 3] = p >> 24;
            }

            ComputeWeights(aSrcW, theWidth, out int[] aStartX, out float[][] aWeightsX);
            float[] aRows = new float[theWidth * aSrcH * 4];
            for (int y = 0; y < aSrcH; y++)
            {
                for (int x = 0; x < theWidth; x++)
                {
                    float[] w = aWeightsX[x];
                    int o = (y * theWidth + x) * 4;
                    for (int k = 0; k < w.Length; k++)
                    {
                        int s = (y * aSrcW + Math.Clamp(aStartX[x] + k, 0, aSrcW - 1)) * 4;
                        for (int c = 0; c < 4; c++)
                        {
                            aRows[o + c] += aSrc[s + c] * w[k];
                        }
                    }
                }
            }

            ComputeWeights(aSrcH, theHeight, out int[] aStartY, out float[][] aWeightsY);
            ImageLibImage aResult = new ImageLibImage(theWidth, theHeight);
            float[] aPixel = new float[4];
            for (int y = 0; y < theHeight; y++)
            {
                float[] w = aWeightsY[y];
                for (int x = 0; x < theWidth; x++)
                {
                    Array.Clear(aPixel);
                    for (int k = 0; k < w.Length; k++)
                    {
                        int s = (Math.Clamp(aStartY[y] + k, 0, aSrcH - 1) * theWidth + x) * 4;
                        for (int c = 0; c < 4; c++)
                        {
                            aPixel[c] += aRows[s + c] * w[k];
                        }
                    }
                    uint a = (uint)Math.Clamp((int)(aPixel[3] + 0.5f), 0, 255);
                    uint r = (uint)Math.Clamp((int)(aPixel[0] + 0.5f), 0, (int)a);
                    uint g = (uint)Math.Clamp((int)(aPixel[1] + 0.5f), 0, (int)a);
                    uint b = (uint)Math.Clamp((int)(aPixel[2] + 0.5f), 0, (int)a);
                    aResult.mBits[y * theWidth + x] = (a << 24) | (b << 16) | (g << 8) | r;
                }
            }
            return aResult;
        }

        private static void ComputeWeights(int theSrcLen, int theDstLen, out int[] theStarts, out float[][] theWeights)
        {
            double aScale = (double)theDstLen / theSrcLen;
            double aFilterScale = Math.Min(aScale, 1.0);
            double aSupport = 2.0 / aFilterScale;
            theStarts = new int[theDstLen];
            theWeights = new float[theDstLen][];
            for (int i = 0; i < theDstLen; i++)
            {
                double aCenter = (i + 0.5) / aScale - 0.5;
                int aFirst = (int)Math.Floor(aCenter - aSupport) + 1;
                int aLast = (int)Math.Floor(aCenter + aSupport);
                float[] w = new float[aLast - aFirst + 1];
                double aSum = 0.0;
                for (int j = aFirst; j <= aLast; j++)
                {
                    double v = CatmullRom((j - aCenter) * aFilterScale);
                    w[j - aFirst] = (float)v;
                    aSum += v;
                }
                for (int k = 0; k < w.Length; k++)
                {
                    w[k] = (float)(w[k] / aSum);
                }
                theStarts[i] = aFirst;
                theWeights[i] = w;
            }
        }

        private static double CatmullRom(double x)
        {
            x = Math.Abs(x);
            if (x < 1.0)
            {
                return (1.5 * x - 2.5) * x * x + 1.0;
            }
            if (x < 2.0)
            {
                return ((-0.5 * x + 2.5) * x - 4.0) * x + 2.0;
            }
            return 0.0;
        }
    }
}
