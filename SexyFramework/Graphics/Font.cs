using System;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// SexyApp font base class, as in Peggle Classic HD (Sexy::Font). Replaces the
    /// SpriteFont-oriented Font interface of PlantsVsZombies.NET: Peggle draws all text with
    /// bitmap <see cref="ImageFont"/>s.
    /// </summary>
    public abstract class Font : IDisposable
    {
        public int mAscent;

        public int mAscentPadding;

        public int mHeight;

        public int mLineSpacingOffset;

        public int mMaxCharHeight = -1;

        public int mCenterOffset;

        protected Font()
        {
        }

        protected Font(Font theFont)
        {
            mAscent = theFont.mAscent;
            mAscentPadding = theFont.mAscentPadding;
            mHeight = theFont.mHeight;
            mLineSpacingOffset = theFont.mLineSpacingOffset;
            mMaxCharHeight = theFont.mMaxCharHeight;
            mCenterOffset = theFont.mCenterOffset;
        }

        public virtual void Dispose()
        {
        }

        public virtual int GetAscent()
        {
            return mAscent;
        }

        public virtual int GetAscentPadding()
        {
            return mAscentPadding;
        }

        public virtual int GetDescent()
        {
            return mHeight - mAscent;
        }

        public virtual int GetHeight()
        {
            return mHeight;
        }

        public virtual int GetLineSpacingOffset()
        {
            return mLineSpacingOffset;
        }

        public virtual int GetLineSpacing()
        {
            return mHeight + mLineSpacingOffset;
        }

        public virtual int GetMaxCharHeight()
        {
            return mMaxCharHeight;
        }

        public virtual int GetCenteredHeight()
        {
            int h = mMaxCharHeight - ((mMaxCharHeight + mCenterOffset) - mAscent);
            if (h < 1)
            {
                h = mMaxCharHeight;
            }
            return h / 2;
        }

        public virtual int GetDecorationOffset()
        {
            return (int)(mMaxCharHeight / 2f + 0.5f) - GetCenteredHeight();
        }

        public virtual int StringWidth(string theString)
        {
            return 0;
        }

        public virtual int CharWidth(char theChar)
        {
            return StringWidth(theChar.ToString());
        }

        public virtual int CharWidthKern(char theChar, char thePrevChar)
        {
            return CharWidth(theChar);
        }

        public virtual void DrawString(Graphics g, int theX, int theY, string theString, Color theColor, TRect theClipRect)
        {
        }

        public abstract Font Duplicate();
    }
}
