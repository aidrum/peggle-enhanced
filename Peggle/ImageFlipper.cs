using System;

namespace Sexy
{
    /// <summary>
    /// Animates one image turning over into another (Sexy::ImageFlipper): a 16x16 textured mesh
    /// rotated about the vertical axis with perspective, while moving and resizing from the first
    /// rect to the second. Used by ThumbnailDialog to flip a level thumbnail open into the dialog.
    /// </summary>
    public class ImageFlipper
    {
        public const int FLIP_FORWARD = 0;

        public const int FLIP_BACKWARD = 1;

        public Image mImage1;                                     // 0x00

        public Image mImage2;                                     // 0x04

        public TRect mRect1;                                      // 0x08

        public TRect mRect2;                                      // 0x18

        public int mDuration;                                     // 0x28

        public int mCount;                                        // 0x2c

        public int mDirection;                                    // 0x30

        public SexyVector2 mDelta;                                // 0x34

        public SexyVector2 mOffset;                               // 0x3c

        public ImageFlipper()
        {
            mImage1 = null;
            mImage2 = null;
            mDuration = 0;
            mCount = 0;
            mDirection = 0;
        }

        public float GetPercentComplete()
        {
            if (mDuration == 0)
            {
                return 100.0f;
            }
            return (float)mCount * 100.0f / (float)mDuration;
        }

        public void StartFlip(Image theImage1, TRect theRect1, Image theImage2, TRect theRect2, int theDirection, int theDuration)
        {
            mImage1 = theImage1;
            mImage2 = theImage2;
            mRect1 = theRect1;
            mRect2 = theRect2;
            mDirection = theDirection;
            mDuration = theDuration;
            mCount = 0;
            mDelta = new SexyVector2((float)((mRect2.mX + mRect2.mWidth / 2) - (mRect1.mX + mRect1.mWidth / 2)),
                (float)((mRect2.mY + mRect2.mHeight / 2) - (mRect1.mY + mRect1.mHeight / 2)));
            mOffset = mDelta;
        }

        public void Update()
        {
            if (mCount < mDuration)
            {
                mCount++;
            }
            float aDist = mDelta.Magnitude();
            SexyVector2 aDir = aDist > 0.0f ? mDelta / aDist : mDelta;
            mOffset = aDir * (aDist * (float)mCount / (float)mDuration);
        }

        /// <summary>Projects a point on the turning card onto the screen (Project2D, focal length theFocal).</summary>
        private static TriVertex Project2D(float theX, float theY, float theZ, float theFocal, float theU, float theV)
        {
            return new TriVertex(theX * theFocal / theZ, theY * theFocal / theZ, theU, theV);
        }

        public void Draw(Graphics g)
        {
            if (mImage1 == null || mImage2 == null)
            {
                return;
            }
            float anAngle = (float)mCount * 180.0f / (float)mDuration;
            bool isBack = ((int)anAngle + 90) / 180 % 2 != 0;
            Image anImage = isBack ? mImage2 : mImage1;
            if (mDirection == FLIP_BACKWARD)
            {
                anAngle = 180.0f - anAngle;
            }
            float aRad = SexyUtil.DegToRad(anAngle);
            float aCos = (float)Math.Cos(aRad);
            float aSin = (float)Math.Sin(aRad);
            float[] aYs = new float[17];
            float[] aXs = new float[17];
            for (int i = 0; i < 16; i++)
            {
                aYs[i] = (float)(anImage.mHeight * i / 16);
                aXs[i] = (float)(anImage.mWidth * i / 16);
            }
            aXs[16] = (float)anImage.mWidth;
            aYs[16] = (float)anImage.mHeight;
            float t = (float)mCount / (float)mDuration;
            float aWidth = SexyUtil.LinearCombineF(t, (float)mRect2.mWidth, (float)mRect1.mWidth);
            float aHeight = SexyUtil.LinearCombineF(t, (float)mRect2.mHeight, (float)mRect1.mHeight);
            float aScaleX = aWidth / (float)anImage.mWidth;
            float aScaleY = aHeight / (float)anImage.mHeight;
            float aFocal = 800.0f;
            int aTransX = (int)(mOffset.x + (float)mRect1.mX + (float)(mRect1.mWidth / 2));
            int aTransY = (int)(mOffset.y + (float)mRect1.mY + (float)(mRect1.mHeight / 2));
            g.Translate(aTransX, aTransY);
            for (int aRow = 0; aRow < 16; aRow++)
            {
                for (int aCol = 0; aCol < 16; aCol++)
                {
                    float x0 = (aXs[aCol] - (float)(anImage.mWidth / 2)) * aScaleX;
                    float y0 = (aYs[aRow] - (float)(anImage.mHeight / 2)) * aScaleY;
                    float x1 = (aXs[aCol + 1] - (float)(anImage.mWidth / 2)) * aScaleX;
                    float y1 = (aYs[aRow + 1] - (float)(anImage.mHeight / 2)) * aScaleY;
                    float u0, u1;
                    if ((!isBack || mDirection != FLIP_FORWARD) && (isBack || mDirection != FLIP_BACKWARD))
                    {
                        u0 = aXs[aCol] / (float)anImage.mWidth;
                        u1 = aXs[aCol + 1] / (float)anImage.mWidth;
                    }
                    else
                    {
                        u0 = 1.0f - aXs[aCol] / (float)anImage.mWidth;
                        u1 = 1.0f - aXs[aCol + 1] / (float)anImage.mWidth;
                    }
                    float v0 = aYs[aRow] / (float)anImage.mHeight;
                    float v1 = aYs[aRow + 1] / (float)anImage.mHeight;
                    TriVertex a = Project2D(x0 * aCos, y0, aFocal - x0 * aSin, aFocal, u0, v0);
                    TriVertex b = Project2D(x1 * aCos, y0, aFocal - x1 * aSin, aFocal, u1, v0);
                    TriVertex c = Project2D(x0 * aCos, y1, aFocal - x0 * aSin, aFocal, u0, v1);
                    TriVertex d = Project2D(x1 * aCos, y1, aFocal - x1 * aSin, aFocal, u1, v1);
                    g.DrawTriangleTex(anImage, a, b, c);
                    g.DrawTriangleTex(anImage, c, b, d);
                }
            }
            g.Translate(-aTransX, -aTransY);
        }
    }
}
