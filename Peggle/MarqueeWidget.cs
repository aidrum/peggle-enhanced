using static Sexy.Layout;
namespace Sexy
{
    /// <summary>
    /// A text pod that scrolls its word-wrapped text up one line at a time (Sexy::MarqueeWidget),
    /// pausing on each line; used for the score pods beside the board.
    /// </summary>
    public class MarqueeWidget : Widget
    {
        public bool mPaused;                                      // 0x78

        public int mRepeats;                                      // 0x7c

        public int mRepeatsLeft;                                  // 0x80

        public Font mFont;                                        // 0x84

        public MemoryImage mTextImage;                            // 0x88

        public int mScrollDelay;                                  // 0x8c

        public int mLinePause;                                    // 0x90

        public int mScrollPos;                                    // 0x94

        public int mScrollCounter;                                // 0x98

        public int mPauseCounter;                                 // 0x9c

        public int mLineSpacing;                                  // 0xa0

        public int mStartPause;                                   // 0xa4

        public int mEndPause;                                     // 0xa8

        public int mTextHeight;                                   // 0xac

        public int mLineHeight;                                   // 0xb0

        public MarqueeWidget(Font theFont, int theStartPause, int theScrollDelay, int theLinePause, int theEndPause, int theLineSpacing)
        {
            mFont = theFont;
            mScrollDelay = theScrollDelay;
            mLinePause = theLinePause;
            mStartPause = theStartPause;
            mEndPause = theEndPause;
            mTextImage = null;
            if (theLineSpacing == -1)
            {
                theLineSpacing = mFont.GetHeight();
            }
            mLineSpacing = theLineSpacing;
            mLineHeight = theLineSpacing;
            mRepeats = -1;
            mScrollPos = 0;
            mPauseCounter = mStartPause;
            mScrollCounter = mScrollDelay;
            mPaused = false;
            mRepeatsLeft = mRepeats;
        }

        public override void Dispose()
        {
            mTextImage?.Dispose();
            base.Dispose();
        }

        public void Pause()
        {
            mPaused = true;
        }

        public void Resume()
        {
            mPaused = false;
        }

        public void SetRepeats(int theRepeats)
        {
            mRepeats = theRepeats;
        }

        public void SetText(string theText, int theJustification)
        {
            Graphics aMeasure = Graphics.GetNew();
            aMeasure.SetFont(mFont);
            aMeasure.SetClipRect(0, 0, 0, 0);
            int aHeight = aMeasure.WriteWordWrapped(new TRect(0, 0, mWidth, 0), theText, mLineSpacing, -1);
            aMeasure.PrepareForReuse();
            if (mTextImage != null && aHeight != mTextImage.mHeight)
            {
                mTextImage.Dispose();
                mTextImage = null;
                mScrollPos = 0;
                mScrollDelay = 3;
                mLinePause = 0x1e;
                mStartPause = 0xf0;
                mEndPause = 0xf0;
                mPauseCounter = mStartPause;
                mScrollCounter = mScrollDelay;
            }
            if (mTextImage == null)
            {
                mTextImage = new MemoryImage();
                mTextImage.Create(mWidth, aHeight);
            }
            mTextHeight = mLineHeight * (mTextImage.mHeight / mLineHeight);
            bool inFrame = Graphics.IsSpriteBatchBegun();
            Graphics g = Graphics.GetNew(mTextImage);
            g.SetFont(mFont);
            g.SetColor(Microsoft.Xna.Framework.Color.White);
            g.WriteWordWrapped(new TRect(0, 0, mWidth, 0), theText, mLineSpacing, theJustification);
            g.EndOffscreen(inFrame);
        }

        public new void Reset()
        {
            mRepeatsLeft = mRepeats;
        }

        public override void Update()
        {
            if (mPaused)
            {
                return;
            }
            if (mScrollPos == mTextHeight - mLineHeight && mRepeatsLeft == 0)
            {
                return;
            }
            if (mPauseCounter > 0 && --mPauseCounter > 0)
            {
                return;
            }
            if (mScrollCounter > 0)
            {
                if (--mScrollCounter != 0)
                {
                    return;
                }
                mScrollPos++;
                mScrollCounter = mScrollDelay;
            }
            if (mScrollPos == mTextHeight)
            {
                mScrollPos = 0;
                mPauseCounter = mStartPause;
            }
            else if (mScrollPos == mTextHeight - mLineHeight)
            {
                mPauseCounter = mEndPause;
                if (mRepeatsLeft > 0)
                {
                    mRepeatsLeft--;
                }
            }
            else if (mScrollPos % mLineHeight == 0)
            {
                mPauseCounter = mLinePause;
            }
        }

        public override void Draw(Graphics g)
        {
            if (mTextImage == null)
            {
                return;
            }
            int aY = mTextHeight - mTextImage.mHeight;
            g.mClipRect.mY = BOARD_SCORE_POD_CLIP_OFFSET_Y + BOARD_SCORE_POD_Y;
#if !PEGGLE_IPHONE
            g.mClipRect.mHeight = BOARD_SCORE_POD_CLIP_HEIGHT;
#endif
            if (mHeight < mTextHeight)
            {
                g.Translate(0, -mScrollPos);
                g.DrawImage(mTextImage, 0, aY);
                g.DrawImage(mTextImage, 0, aY + mTextHeight);
                g.Translate(0, mScrollPos);
            }
            else
            {
                g.DrawImage(mTextImage, 0, aY);
            }
        }
    }
}
