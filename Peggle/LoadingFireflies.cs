using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Port addition: the loading screen's fireflies and their sparks, as the PC Peggle Nights and Peggle
    /// WoW loading screens have them (PeggleWoW.exe LoadingScreen; Nights shares the code with other
    /// frames). Up to five fireflies loop round the screen on a fixed curve, shy away from the mouse and
    /// drop still sparks; tapping one pops it (FairyPop and a burst of 30 sparks).
    /// Positions are the PC's 800x600, mapped to the screen by an x offset and a scale.
    /// RE notes: D:/peggle-re-wow/notes_loading.md.
    /// </summary>
    public class LoadingFireflies
    {
        private struct Firefly
        {
            public float mPhase;                                  // 0x00

            public float mPushX;                                  // 0x04

            public float mPushY;                                  // 0x08

            public float mX;                                      // 0x0c

            public float mY;                                      // 0x10

            public float mAlpha;                                  // 0x14
        }

        private static readonly Random sRand = new Random();

        private readonly ThunderballApp mApp;

        private readonly Image mImage;

        private readonly TRect mFly0;

        private readonly TRect mFly1;

        private int mPopSound = -1;

        private readonly List<Firefly> mFireflies = new List<Firefly>();          // 0xf0

        private readonly List<TrailParticle> mSparks = new List<TrailParticle>(); // 0xd0

        /// <summary>theImage holds the two frames (theFly0, theFly1), already at the screen's scale.</summary>
        public LoadingFireflies(ThunderballApp theApp, Image theImage, TRect theFly0, TRect theFly1)
        {
            mApp = theApp;
            mImage = theImage;
            mFly0 = theFly0;
            mFly1 = theFly1;
            mPopSound = mApp.mSoundManager.LoadSound("sounds/wow_fairypop.wav");
        }

        public void Dispose()
        {
            if (mPopSound >= 0)
            {
                mApp.mSoundManager.ReleaseSound((uint)mPopSound);
                mPopSound = -1;
            }
        }

        /// <summary>
        /// One update: the sparks fall, a new firefly may come (a 1 in 100 chance while theCanSpawn and
        /// there are fewer than five), and the fireflies move, shying from the mouse when theWidget's
        /// manager has one; theOffX/theScale map the PC screen onto theWidget.
        /// </summary>
        public void Update(Widget theWidget, bool theCanSpawn, float theOffX, float theScale)
        {
            for (int i = 0; i < mSparks.Count;)
            {
                TrailParticle p = mSparks[i];
                if (--p.mLife < 1)
                {
                    mSparks[i] = mSparks[mSparks.Count - 1];
                    mSparks.RemoveAt(mSparks.Count - 1);
                    continue;
                }
                p.mY += p.mVelY;
                p.mX += p.mVelX;
                p.mVelY += 0.0002f;
                mSparks[i] = p;
                i++;
            }
            if (theCanSpawn && mFireflies.Count < 5 && sRand.Next(100) == 0)
            {
                Firefly aFly = new Firefly();
                aFly.mPhase = sRand.Next(1000) * 0.001f;
                mFireflies.Add(aFly);
            }
            WidgetManager aManager = theWidget.mWidgetManager;
            bool aMouse = aManager != null && aManager.mLastMouseX != WidgetManager.NO_TOUCH_MOUSE_POS.mX;
            float aMouseX = aMouse ? (aManager.mLastMouseX - theWidget.mX - theOffX) / theScale : 0;
            float aMouseY = aMouse ? (aManager.mLastMouseY - theWidget.mY) / theScale : 0;
            for (int i = 0; i < mFireflies.Count; i++)
            {
                Firefly f = mFireflies[i];
                f.mPhase += 4e-05f;
                if (f.mPhase > 1.0f)
                {
                    f.mPhase -= 1.0f;
                }
                float p = (float)(f.mPhase * Math.PI * 2);
                f.mX = (float)(Math.Cos(p * 4) * 280 + 400 + Math.Cos(p * 3) * 70);
                f.mY = (float)(300 - Math.Sin(p * 3) * 240 - Math.Sin(p * 4) * 53.175);
                f.mPushX *= 0.99f;
                f.mPushY *= 0.99f;
                f.mAlpha = Math.Min(1.0f, f.mAlpha + 0.01f);
                if (aMouse)
                {
                    float dx = f.mPushX + f.mX - aMouseX;
                    float dy = f.mPushY + f.mY - aMouseY;
                    float d2 = dx * dx + dy * dy;
                    if (d2 < 10000 && d2 > 1)
                    {
                        f.mPushX += dx * 100 / d2;
                        f.mPushY += dy * 100 / d2;
                    }
                }
                if (sRand.Next(6) == 0)
                {
                    AddSparks((int)(f.mPushX + f.mX), (int)(f.mPushY + f.mY), false);
                }
                mFireflies[i] = f;
            }
        }

        /// <summary>PC FUN_004be670: a still spark, or (theBurst) 30 flying ones.</summary>
        private void AddSparks(int theX, int theY, bool theBurst)
        {
            if (!theBurst)
            {
                mSparks.Add(new TrailParticle { mX = theX, mY = theY, mLife = 0xff });
                return;
            }
            for (int i = 0; i < 30; i++)
            {
                float anAngle = (float)(sRand.Next(1000) * 0.002 * Math.PI);
                float aSpeed = sRand.Next(1000) * 0.001f;
                mSparks.Add(new TrailParticle
                {
                    mX = theX, mY = theY, mLife = 0xff,
                    mVelX = (float)Math.Sin(anAngle) * aSpeed, mVelY = (float)Math.Cos(anAngle) * aSpeed,
                });
            }
        }

        /// <summary>PC vf54: pops the firefly near (x,y) (PC coordinates), if any; true when one popped.</summary>
        public bool Pop(float x, float y)
        {
            for (int i = 0; i < mFireflies.Count; i++)
            {
                Firefly f = mFireflies[i];
                float dx = f.mPushX + f.mX - x;
                float dy = f.mPushY + f.mY - y;
                if (dx * dx + dy * dy < 600)
                {
                    if (mPopSound >= 0)
                    {
                        mApp.PlaySample(mPopSound);
                    }
                    AddSparks((int)(f.mPushX + f.mX), (int)(f.mPushY + f.mY), true);
                    mFireflies.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>The fireflies, flickering between their two frames.</summary>
        public void Draw(Graphics g, float theOffX, float theScale)
        {
            foreach (Firefly f in mFireflies)
            {
                TRect aCel = sRand.Next(2) == 0 ? mFly0 : mFly1;
                g.SetColorizeImages(true);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(f.mAlpha * 255)));
                float aHalf = (int)(aCel.mWidth / theScale) / 2;
                g.DrawImage(mImage, (int)(theOffX + (int)(f.mPushX + f.mX - aHalf) * theScale), (int)((int)(f.mPushY + f.mY - aHalf) * theScale), aCel);
                g.SetColorizeImages(false);
            }
            g.SetColor(new Color(255, 255, 255));
        }

        /// <summary>PC FUN_004befe0's spark half: small dim red specks, drawn additively.</summary>
        public void DrawSparks(Graphics g, float theOffX, float theScale)
        {
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
            int aSize = Math.Max(1, (int)theScale);
            foreach (TrailParticle p in mSparks)
            {
                g.SetColor(new Color(sRand.Next(32) + 32, sRand.Next(32), sRand.Next(32), Math.Max(0, Math.Min(255, p.mLife))));
                g.FillRect((int)(theOffX + (p.mX - 1) * theScale), (int)((p.mY - 1) * theScale), (sRand.Next(2) + 1) * aSize, (sRand.Next(2) + 1) * aSize);
            }
            g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
            g.SetColor(new Color(255, 255, 255));
        }
    }
}
