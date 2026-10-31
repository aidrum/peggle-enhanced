using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>A firework/rocket-trail spark on the story screens (Sexy::Particle, 0x20 bytes).</summary>
    public class Particle
    {
        public float mX;                                          // 0x00

        public float mY;                                          // 0x04

        public float mVelX;                                       // 0x08

        public float mVelY;                                       // 0x0c

        public int mUpdateCnt;                                    // 0x10

        public int mDuration;                                     // 0x14

        public int mType;                                         // 0x18

        public int mColor;                                        // 0x1c

        public void Update()
        {
            mUpdateCnt++;
            mX += mVelX;
            mY += mVelY;
            mVelY += 0.01f;
        }
    }

    /// <summary>
    /// The story text that types itself out one character at a time (Sexy::TypingTextWidget).
    /// Nights stories type two strings back to back (mText then mText2).
    /// </summary>
    public class TypingTextWidget : Widget
    {
        public StoryScreen mStoryScreen;                          // 0x78

        public string mText = "";                                 // 0x7c

        public string mText2 = "";                                // 0x80

        public Color mColor = Color.White;                        // 0x84

        public Font mFont;                                        // 0x94

        public int mNumChars;                                     // 0x98

        public int mDelay;                                        // 0x9c

        public int mLineCount;                                    // 0xa0

        public TypingTextWidget(StoryScreen theStoryScreen)
        {
            mStoryScreen = theStoryScreen;
        }

        public bool IsDone()
        {
            return (uint)mNumChars >= (uint)(mText.Length + mText2.Length);
        }

        public void Finish()
        {
            mNumChars = 10000000;
            mDelay = 0;
        }

        public override void Draw(Graphics g)
        {
            if (mText.Length == 0)
            {
                mLineCount = 0;
                return;
            }
            if (ThunderballApp.mGameLevelPack == 0 || !mStoryScreen.mInDreamTransition)
            {
                g.SetFont(mFont);
                g.SetColor(mColor);
                int aMaxWidth = 0;
                int aLastWidth = 0;
                g.WriteWordWrapped(new TRect(0, 4, mWidth, mHeight), mText + mText2, -1, -1, ref aMaxWidth, mNumChars,
                    ref aLastWidth, ref mLineCount, false);
            }
        }

        private static int GetCharDelay(char theChar)
        {
            switch (theChar)
            {
                case '!':
                case '?':
                    return 20;
                case ',':
                case '.':
                    return 10;
                default:
                    return 2;
            }
        }

        public override void Update()
        {
            if (mDelay != 0)
            {
                mDelay--;
                return;
            }
            if (ThunderballApp.mGameLevelPack == 1)
            {
                if (!IsDone())
                {
                    mNumChars++;
                    mStoryScreen.mApp.PlaySample(Res.SOUND_TYPING);
                }
                int aBase = 0;
                for (int i = 0; i < 2; i++)
                {
                    string aText = i == 0 ? mText : mText2;
                    if (aText.Length == 0)
                    {
                        return;
                    }
                    int aPos = mNumChars - aBase - 1;
                    if (aPos >= 0 && aPos < aText.Length)
                    {
                        mDelay = GetCharDelay(aText[aPos]);
                        if (aPos == aText.Length - 1)
                        {
                            // Pause between the two halves (and before the dream).
                            mStoryScreen.mTypingDelay = 50;
                            mDelay = 50;
                        }
                        return;
                    }
                    aBase += aText.Length;
                }
            }
            else if (!IsDone())
            {
                mNumChars++;
                mStoryScreen.mApp.PlaySample(Res.SOUND_TYPING);
                int aPos = mNumChars - 1;
                if (aPos >= 0 && aPos < mText.Length)
                {
                    mDelay = GetCharDelay(mText[aPos]);
                }
            }
        }

        public int GetPreferredHeight(int theWidth, int theMaxChars)
        {
            Graphics g = Graphics.GetNew();
            g.SetFont(mFont);
            int aMaxWidth = 0;
            return g.GetWordWrappedHeight(theWidth, mText + mText2, -1, ref aMaxWidth, theMaxChars) + 4;
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            mStoryScreen.MouseDown(x, y, theClickCount);
        }
    }

    /// <summary>One AreaParticleEffect particle (Sexy::ParticleData, 0x20 bytes).</summary>
    public class ParticleData
    {
        public float mX;                                          // 0x00

        public float mY;                                          // 0x04

        public float mVelX;                                       // 0x08

        public float mVelY;                                       // 0x0c

        public int mUpdateCnt;                                    // 0x10

        public Image mImage;                                      // 0x14

        public int mColor;                                        // 0x18

        public float mGravity;                                    // 0x1c

        /// <summary>Port addition: which of the effect's images it got (an index into mImageIds), for mCelsOnly.</summary>
        public int mImageIndex;
    }

    /// <summary>
    /// Sparkles spawned over a rectangle (or an ellipse when mCircular) with a spawn-rate curve over
    /// the effect's life and scale/alpha curves over each particle's life (Sexy::AreaParticleEffect).
    /// Used around the dreaming character on the Nights story screen.
    /// </summary>
    public class AreaParticleEffect
    {
        public List<ParticleData> mParticles = new List<ParticleData>(); // 0x00

        public SexyVector2 mPos;                                  // 0x0c

        public bool mCircular;                                    // 0x14

        public bool mAdditive;                                    // 0x15

        public int mDuration;                                     // 0x18

        public int mWidth;                                        // 0x1c

        public int mHeight;                                       // 0x20

        public int mEdgeX;                                        // 0x24

        public int mEdgeY;                                        // 0x28

        public int mVelXRange;                                    // 0x2c

        public int mVelYRange;                                    // 0x30

        public double mDensity;                                   // 0x34

        public float mAlpha;                                      // 0x3c

        public CurvedVal mSpawnRate = new CurvedVal();            // 0x40

        public CurvedVal mRadius = new CurvedVal();               // 0xb4

        public CurvedVal mScale = new CurvedVal();                // 0x128

        public CurvedVal mParticleAlpha = new CurvedVal();        // 0x19c

        public List<int> mImageIds = new List<int>();             // 0x210

        /// <summary>
        /// Port addition: mImageIds are indices into the caller's own cels (ParticleData.mImageIndex), not
        /// resource ids, and the caller draws the particles (the WoW menu banner's sparkles, WoWMenuScene).
        /// </summary>
        public bool mCelsOnly;

        public double mSpawnAccum;                                // 0x21c

        public int mInt224;                                       // 0x224

        public int mUpdateCnt;                                    // 0x228

        public AreaParticleEffect()
        {
            Reset();
        }

        public void Reset()
        {
            mParticles.Clear();
            mImageIds.Clear();
            mSpawnRate.SetCurve("b+0,1,0,1,####J~###yyl4>        [####");
            mRadius.SetCurve("b+0,1,0,1,#3b;    n~###     3#@Tt");
            mScale.SetCurve("b+0,1,0.01,0.4,####    `i###     AM###");
            mParticleAlpha.SetCurve("b+0,1,0.01,0.4,f###    Y~###     G####");
            mInt224 = 0;
            mDuration = 0;
            mUpdateCnt = 0;
            mCircular = false;
            mPos = new SexyVector2(0f, 0f);
            mVelXRange = 0;
            mVelYRange = 0;
            mSpawnAccum = 0.0;
            mDensity = BitConverter.Int64BitsToDouble(0x3f1f75104d551d69);
            mAdditive = false;
            mAlpha = 1.0f;
            mEdgeX = -1;
            mEdgeY = -1;
        }

        public void AddParticleImageId(int theId)
        {
            mImageIds.Add(theId);
        }

        public bool Update()
        {
            int aPrevCount = (int)mSpawnAccum;
            double aRate = (double)(mWidth * mHeight) * mDensity * (mCircular ? 1.5 : 1.0);
            double aTime = (double)mUpdateCnt / (double)mDuration;
            mSpawnAccum += mSpawnRate.GetOutVal(aTime) * aRate;
            for (int n = aPrevCount; n < (int)mSpawnAccum; n++)
            {
                ParticleData p = new ParticleData();
                mParticles.Add(p);
                p.mUpdateCnt = 0;
                if (mCircular)
                {
                    float aRadius = (float)(mRadius.GetOutVal(SexyUtil.Rand() / 2147483647.0) * mWidth) - mWidth / 2.0f;
                    float anAngle = (float)(SexyUtil.Rand() / 2147483647.0 * 3.1415927410125732 * 2.0);
                    p.mX = (float)(mPos.x + aRadius * Math.Cos(anAngle));
                    p.mY = (float)(mPos.y + aRadius * Math.Sin(anAngle) * mHeight / mWidth);
                }
                else
                {
                    p.mX = mPos.x + SexyUtil.Rand() % mWidth - mWidth / 2.0f;
                    p.mY = mPos.y + SexyUtil.Rand() % mHeight - mHeight / 2.0f;
                    if (mEdgeX >= 0 && mEdgeY >= 0)
                    {
                        // Pin the particle to within mEdgeX/mEdgeY of a random edge.
                        if (SexyUtil.Rand() % 2 == 0)
                        {
                            if (SexyUtil.Rand() % 2 == 0)
                            {
                                p.mX = mPos.x + mWidth / 2 - SexyUtil.Rand() % mEdgeX;
                            }
                            else
                            {
                                p.mX = mPos.x - mWidth / 2 + SexyUtil.Rand() % mEdgeX;
                            }
                        }
                        else if (SexyUtil.Rand() % 2 == 0)
                        {
                            p.mY = mPos.y + mHeight / 2 - SexyUtil.Rand() % mEdgeY;
                        }
                        else
                        {
                            p.mY = mPos.y - mHeight / 2 + SexyUtil.Rand() % mEdgeY;
                        }
                    }
                }
                p.mImageIndex = (int)((uint)SexyUtil.Rand() % (uint)mImageIds.Count);
                p.mImage = mCelsOnly ? null : SexyUtil.GetImageById(mImageIds[p.mImageIndex]);
                p.mVelX = mVelXRange == 0 ? 0f : (SexyUtil.Rand() % mVelXRange - mVelXRange / 2) * 0.001f;
                p.mVelY = mVelYRange == 0 ? 0f : (SexyUtil.Rand() % mVelYRange - mVelYRange / 2) * 0.001f;
                SexyUtil.Rand(); // picks between two identical colours
                p.mColor = 0xffffff;
                p.mGravity = 0f;
            }
            for (int i = mParticles.Count - 1; i >= 0; i--)
            {
                ParticleData p = mParticles[i];
                p.mUpdateCnt++;
                if (mScale.mInMax < p.mUpdateCnt / 100.0f)
                {
                    if (mParticles.Count > 1)
                    {
                        mParticles[i] = mParticles[mParticles.Count - 1];
                    }
                    mParticles.RemoveAt(mParticles.Count - 1);
                }
                else
                {
                    p.mVelY += p.mGravity;
                    p.mX += p.mVelX;
                    p.mY += p.mVelY;
                }
            }
            mUpdateCnt++;
            return aTime < mSpawnRate.mInMax || mParticles.Count != 0;
        }

        public void Draw(Graphics g)
        {
            mInt224 = Math.Max(0, mInt224);
        }

        public void DrawOverlay(Graphics g)
        {
            for (int i = 0; i < mParticles.Count; i++)
            {
                g.SetColorizeImages(true);
                if (mAdditive)
                {
                    g.SetDrawMode(Graphics.DrawMode.DRAWMODE_ADDITIVE);
                }
                ParticleData p = mParticles[i];
                double aScale = mScale.GetOutVal(p.mUpdateCnt / 100.0f);
                double anAlpha = mParticleAlpha.GetOutVal(p.mUpdateCnt / 100.0f);
                g.SetColor(SexyUtil.MakeColor(p.mColor, SexyUtil.ClampRange((int)(mAlpha * anAlpha * 255.0), 0, 0xff)));
                Transform aTrans = new Transform();
                aTrans.Scale((float)aScale, (float)aScale);
                g.DrawImageTransform(p.mImage, aTrans, p.mX, p.mY);
                g.SetDrawMode(Graphics.DrawMode.DRAWMODE_NORMAL);
                g.SetColor(Color.White);
                g.SetColorizeImages(false);
            }
        }
    }

    /// <summary>Shared effect helpers (Sexy::MiscEffectUtil).</summary>
    public static class MiscEffectUtil
    {
        /// <summary>
        /// Draws theImage as horizontal strips of theStripHeight pixels, each shifted sideways by a sine
        /// wave: the "dream" wobble. Amplitude is theAmplitude * theAmpCurve(theTime) and the phase is
        /// thePhase * thePhaseCurve(theTime); theFrequency is the number of wave periods over the image.
        /// </summary>
        public static void DrawDreamImage(Graphics g, Image theImage, double thePhase, double theAmplitude, double theTime,
            int theX, int theY, CurvedVal theAmpCurve, CurvedVal thePhaseCurve, int theStripHeight, float theFrequency)
        {
            double aWidth = theImage.GetWidth();
            double aHeight = theImage.GetHeight();
            int aNumStrips = (int)(aHeight * 1.0 / theStripHeight);
            double aPhase = thePhase * thePhaseCurve.GetOutVal(theTime);
            double anAmp = theAmplitude * theAmpCurve.GetOutVal(theTime);
            TriVertex[] aVerts = new TriVertex[aNumStrips * 2 * 3];
            for (int i = 0; i < aNumStrips; i++)
            {
                int anOffset = (int)(Math.Sin((double)(theFrequency * 3.1415927f) * 2.0 * i / aNumStrips + aPhase) * anAmp);
                int t = theStripHeight == 1 ? i : i * 2;
                float aLeft = (float)(anOffset + theX - anAmp);
                float aRight = (float)(anOffset + theX + aWidth + anAmp);
                float aTop = theY + theStripHeight * i;
                float aBottom = theY + theStripHeight * (i + 1);
                float aTopV = (float)(theStripHeight * i / aHeight);
                float aBottomV = (float)(theStripHeight * (i + 1) / aHeight);
                aVerts[t * 3] = new TriVertex(aLeft, aTop, 0f, aTopV);
                aVerts[t * 3 + 1] = new TriVertex(aRight, aTop, 1f, aTopV);
                aVerts[t * 3 + 2] = new TriVertex(aLeft, aBottom, 0f, aBottomV);
                if (theStripHeight != 1)
                {
                    aVerts[t * 3 + 3] = new TriVertex(aRight, aTop, 1f, aTopV);
                    aVerts[t * 3 + 4] = new TriVertex(aRight, aBottom, 1f, aBottomV);
                    aVerts[t * 3 + 5] = new TriVertex(aLeft, aBottom, 0f, aBottomV);
                }
            }
            // The original draws 2 * aNumStrips triangles; with one-pixel strips the second half is
            // left zeroed (degenerate), so only the filled ones are drawn here.
            int aNumTris = theStripHeight == 1 ? aNumStrips : aNumStrips * 2;
            for (int i = 0; i < aNumTris; i++)
            {
                g.DrawTriangleTex(theImage, aVerts[i * 3], aVerts[i * 3 + 1], aVerts[i * 3 + 2]);
            }
        }
    }
}
