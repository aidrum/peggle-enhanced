namespace Sexy
{
    /// <summary>
    /// Two-cel image button with a slowly pulsing highlight (Sexy::GameButton): the normal cel is
    /// drawn with the highlight cel faded over it, or just the highlight cel while pressed.
    /// </summary>
    public class GameButton : ButtonWidget
    {
        public Image mImage;                                      // 0xf8

        public bool mVertical;                                    // 0xfc

        public TRect mNormalCelRect;                              // 0x100

        public TRect mDownCelRect;                                // 0x110

        public new int mUpdateCnt;                                // 0x120

        public float mGlow;                                       // 0x124

        public bool mGlowRising;                                  // 0x128

        public GameButton(Image theImage, int theId, ButtonListener theListener, bool isVertical) : base(theId, theListener)
        {
            mDoFinger = true;
            mImage = theImage;
            mVertical = isVertical;
            mUpdateCnt = 0;
            mGlow = 0.0f;
            mGlowRising = true;
            if (!isVertical)
            {
                mNormalCelRect = new TRect(0, 0, mImage.mWidth / 2, mImage.mHeight);
                mDownCelRect = new TRect(mImage.mWidth / 2, 0, mImage.mWidth / 2, mImage.mHeight);
            }
            else
            {
                mNormalCelRect = new TRect(0, 0, mImage.mWidth, mImage.mHeight / 2);
                mDownCelRect = new TRect(0, mImage.mHeight / 2, mImage.mWidth, mImage.mHeight / 2);
            }
        }

        public override void Dispose()
        {
            mImage = null;
            base.Dispose();
        }

        public override void Draw(Graphics g)
        {
            if (!IsButtonDown())
            {
                g.DrawImage(mImage, 0, 0, mNormalCelRect);
                g.SetColor(SexyUtil.MakeColor(0xffffff, (int)(mGlow * 255.0f)));
                g.SetColorizeImages(true);
                g.DrawImage(mImage, 0, 0, mDownCelRect);
                g.SetColorizeImages(false);
            }
            else
            {
                g.DrawImage(mImage, 0, 0, mDownCelRect);
            }
        }

        public override void Update()
        {
            base.Update();
            mUpdateCnt++;
            if (!IsButtonDown())
            {
                if (!mGlowRising)
                {
                    mGlow -= 0.009f;
                    if (mGlow <= 0.0f)
                    {
                        mGlow = 0.0f;
                        mGlowRising = true;
                    }
                }
                else
                {
                    mGlow += 0.009f;
                    if (0.9f <= mGlow)
                    {
                        mGlow = 0.9f;
                        mGlowRising = false;
                    }
                }
            }
            else
            {
                mGlow = 0.0f;
                mGlowRising = true;
            }
        }
    }
}
