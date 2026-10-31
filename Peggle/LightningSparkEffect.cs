using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Little electric arcs crackling around an electrified ball or peg
    /// (Sexy::LightningSparkEffect). Each spark is an angle (sign = animation direction) and a
    /// remaining life in frames.
    /// </summary>
    public class LightningSparkEffect
    {
        public int mSparkLife;                                    // 0x00

        public float mScale;                                      // 0x04

        public List<KeyValuePair<float, int>> mSparks = new List<KeyValuePair<float, int>>(); // 0x08

        public LightningSparkEffect()
        {
            mSparkLife = 0x28;
            mScale = 1.0f;
        }

        public int GetSparkCount()
        {
            return mSparks.Count;
        }

        public void SetScale(float theScale)
        {
            mScale = theScale;
        }

        public void Update(bool addSparks)
        {
            mSparkLife = 0x28;
            int i = 0;
            while (i < mSparks.Count)
            {
                KeyValuePair<float, int> spark = mSparks[i];
                spark = new KeyValuePair<float, int>(spark.Key, spark.Value - 1);
                mSparks[i] = spark;
                if (spark.Value < 1)
                {
                    mSparks.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            if (addSparks && SexyUtil.Rand() % 10 == 0 && mSparks.Count < 3)
            {
                float angle = (float)(SexyUtil.Rand() / 2147483647.0) * 3.1415927f * 2.0f;
                mSparks.Add(new KeyValuePair<float, int>(angle, mSparkLife));
                if (SexyUtil.Rand() % 2 == 0)
                {
                    KeyValuePair<float, int> back = mSparks[mSparks.Count - 1];
                    mSparks[mSparks.Count - 1] = new KeyValuePair<float, int>(back.Key * -1.0f, back.Value);
                }
            }
        }

        public void Draw(Graphics g, int theX, int theY)
        {
            Image image = Res.IMAGE_LIGHTNING_BALLSPARK;
            int numCels = image.mNumCols;
            for (int i = 0; i < mSparks.Count; i++)
            {
                KeyValuePair<float, int> spark = mSparks[i];
                int cel = (int)(spark.Value / Math.Ceiling(mSparkLife / (float)numCels));
                if (spark.Key < 0.0f)
                {
                    cel = numCels - cel - 1;
                }
                int celWidth = image.GetWidth() / numCels;
                TRect src = new TRect(celWidth * cel, 0, celWidth, image.GetHeight());
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, 200));
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                Transform transform = new Transform();
                transform.Scale(mScale, mScale);
                // The original truncates the angle to whole radians before rotating.
                transform.RotateRad(Math.Abs((int)spark.Key));
                g.DrawImageTransform(image, transform, src, (float)(theX * SexyUtil.gPhysicalDisplayHeight / 600.0),
                    (float)(theY * SexyUtil.gPhysicalDisplayHeight / 600.0));
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                g.SetColorizeImages(false);
                g.SetColor(SexyUtil.MakeColor(0xffffff));
            }
        }
    }
}
