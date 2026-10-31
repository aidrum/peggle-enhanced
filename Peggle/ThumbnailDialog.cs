using Microsoft.Xna.Framework;
using static Sexy.Layout;

namespace Sexy
{
    public interface ThumbnailDialogListener
    {
        void PlayPressed();

        void Closed();

        void GetThumbnailInfo(out Image theImage, out TRect theRect);
    }

    /// <summary>
    /// Details popup that flips open out of a thumbnail (Sexy::ThumbnailDialog): a framed picture
    /// with a caption, a content area for subclasses, and PLAY / BACK buttons.
    /// </summary>
    public class ThumbnailDialog : ThunderDialog
    {
        public ThumbnailDialogListener mListener;                 // 0x128

        public TRect mThumbRect;                                  // 0x12c

        public TRect mContentRect;                                // 0x13c

        public TRect mDialogRect;                                 // 0x14c

        public int mCaptionWidth;                                 // 0x15c

        public string mCaption = "";                              // 0x160

        public ThunderButton mPlayButton;                         // 0x164

        public ThunderButton mBackButton;                         // 0x168

        public ImageFlipper mFlipper = new ImageFlipper();        // 0x16c

        public MemoryImage mPicture;                              // 0x1b0

        public ThumbnailDialog(int theId, ThumbnailDialogListener theListener)
            : base(theId, true, "", "", "", 0)
        {
            int w = Res.IMAGE_MM_BUTTON_BLUE.mWidth;
            int h = Res.IMAGE_MM_BUTTON_BLUE.mHeight;
            mPlayButton = MakeButton(Res.IMAGE_MM_BUTTON_BLUE, 1000, SexyAppBase.Loc("PLAY"), w, h);
            mBackButton = MakeButton(Res.IMAGE_BUTTON_ORANGE, 1001, SexyAppBase.Loc("BACK"), w, h);
            mPlayButton.SetVisible(false);
            mBackButton.SetVisible(false);
            mCaptionWidth = 0;
            mListener = theListener;
            mPicture = null;
            mAutoSizeYesButton = true;
            AddWidget(mPlayButton);
            AddWidget(mBackButton);
        }

        private ThunderButton MakeButton(Image theImage, int theId, string theLabel, int w, int h)
        {
            ThunderButton aButton = new ThunderButton(theImage, theId, this);
            aButton.mOverlayText = theLabel;
            aButton.mButtonImage = theImage;
            aButton.mComponentImage = theImage;
            aButton.mWidth = w;
            aButton.mHeight = h / 2;
            aButton.mNormalRect = new TRect(0, 0, w, h / 2);
            aButton.mOverRect = new TRect(0, 0, w, h / 2);
            aButton.mDownRect = new TRect(0, h / 2, w, h / 2);
            return aButton;
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, false);
            mPicture?.Dispose();
            mPicture = null;
            base.Dispose();
        }

        public void SetCaption(string theCaption)
        {
            mCaption = theCaption;
            mCaptionWidth = Res.FONT_OVERLOAD16.StringWidth(theCaption);
        }

        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            int anInsetX = THUMBNAIL_DIALOG_INSET_X;
            int anInsetY = THUMBNAIL_DIALOG_INSET_Y;
            TRect aCelRect = Res.IMAGE_LS_THUMBNAILS_PEGGLE.GetCelRect(0);
            mDialogRect = new TRect(0, 0, theWidth, theHeight);
#if PEGGLE_IPHONE
            mThumbRect = new TRect(mDialogRect.mX + anInsetX,
                Res.FONT_OVERLOAD16.GetHeight() + mDialogRect.mY + anInsetY + THUMBNAIL_DIALOG_RECT_OFFSET_Y,
                aCelRect.mWidth, aCelRect.mHeight);
#else
            mThumbRect = new TRect(mDialogRect.mX + anInsetX - IPAD_THUMB_FRAME_OFFSET_X,
                Res.FONT_OVERLOAD16.GetHeight() + mDialogRect.mY + anInsetY + THUMBNAIL_DIALOG_RECT_OFFSET_Y,
                aCelRect.mWidth - IPAD_THUMB_FRAME_OFFSET_W, aCelRect.mHeight - IPAD_THUMB_FRAME_OFFSET_H);
#endif
            int aLeft = mThumbRect.mX + mThumbRect.mWidth + THUMBNAIL_DIALOG_CONTENT_X;
            int aRight = mDialogRect.mX + mDialogRect.mWidth - THUMBNAIL_DIALOG_CONTENT_X;
            mContentRect = new TRect(aLeft, mThumbRect.mY, aRight - aLeft, mThumbRect.mHeight);
            int aButtonY = THUMBNAIL_DIALOG_BUTTON_Y;
            int aButtonInset = THUMBNAIL_DIALOG_BUTTON_INSET_X;
            mPlayButton.Move(mDialogRect.mX + mDialogRect.mWidth - mPlayButton.mWidth - aButtonInset, aButtonY);
            mBackButton.Move(mDialogRect.mX + aButtonInset, aButtonY);
        }

        public override void ButtonDepress(int theId)
        {
            if (theId == 1000)
            {
                mListener.PlayPressed();
            }
            else
            {
                Hide();
            }
        }

        public void Hide()
        {
            mListener.GetThumbnailInfo(out Image anImage, out TRect aRect);
            mPlayButton.SetVisible(false);
            mBackButton.SetVisible(false);
            mFlipper.StartFlip(mPicture, mPictureRect, anImage, aRect, ImageFlipper.FLIP_BACKWARD, 0x19);
        }

        /// <summary>
        /// Port addition (ultrawide): the card's background and frame (mDialogRect widened out to the
        /// window's edges on iPhone; the contents stay in mDialogRect). Also the flipper's picture.
        /// </summary>
        public TRect mPictureRect;

        /// <summary>Port addition: set while GetPicture renders into mPicture, which is already the wide size.</summary>
        private bool mDrawingPicture;

        public virtual void DrawContent(Graphics g)
        {
#if PEGGLE_IPHONE
            // (Graphics.Create drops an offscreen target, so the picture is drawn into directly.)
            Graphics aWide = mDrawingPicture ? g : CreateWideGraphics(g, mWidth);
            aWide.SetColor(new Color(0x14, 0x18, 0x32));
            aWide.FillRect(mPictureRect);
            SexyUtil.MyDrawImageBox(aWide, mPictureRect, Res.IMAGE_DLG_SELECTORFRAME);
#else
            g.SetColor(new Color(0x14, 0x18, 0x32));
            g.FillRect(mDialogRect);
            SexyUtil.MyDrawImageBox(g, mDialogRect, Res.IMAGE_DLG_SELECTORFRAME);
#endif
            g.SetColor(new Color(0x39, 0xa5, 0x42));
            g.SetFont(Res.FONT_OVERLOAD16);
            g.DrawString(mCaption, mWidth / 2 - mCaptionWidth / 2, mThumbRect.mY - g.GetFont().GetMaxCharHeight() / 2);
        }

        public override void Draw(Graphics g)
        {
            int aMaxAlpha = 0x82;
            int anAlpha = (int)(mFlipper.GetPercentComplete() / 100.0f * (float)aMaxAlpha);
            if (mFlipper.mDirection == ImageFlipper.FLIP_BACKWARD)
            {
                anAlpha = aMaxAlpha - anAlpha;
            }
#if PEGGLE_IPHONE
            // Port addition (ultrawide): the dimming and the turning card may reach past the game.
            Graphics aWide = CreateWideGraphics(g, mWidth);
            TRect aDimRect = GetWideRect(mWidth, new TRect(0, 0, mWidth, mHeight));
#else
            Graphics aWide = g;
            TRect aDimRect = new TRect(0, 0, mWidth, mHeight);
#endif
            aWide.SetColor(new Color(0, 0, 0, anAlpha));
            aWide.FillRect(aDimRect);
            if (mFlipper.GetPercentComplete() == 100.0f)
            {
                DrawContent(g);
            }
            else
            {
                mFlipper.Draw(aWide);
            }
        }

        public void DrawThumbFrame(Graphics g)
        {
            g.SetColorizeImages(true);
            g.SetColor(new Color(0x14, 0x18, 0x32));
            g.DrawImage(Res.IMAGE_DLG_THUMBCORNER_TL, mThumbRect.mX, mThumbRect.mY);
            g.DrawImage(Res.IMAGE_DLG_THUMBCORNER_TR, mThumbRect.mX + mThumbRect.mWidth - Res.IMAGE_DLG_THUMBCORNER_TR.mWidth, mThumbRect.mY);
            g.DrawImage(Res.IMAGE_DLG_THUMBCORNER_BL, mThumbRect.mX, mThumbRect.mY + mThumbRect.mHeight - Res.IMAGE_DLG_THUMBCORNER_BL.mHeight);
            g.DrawImage(Res.IMAGE_DLG_THUMBCORNER_BR, mThumbRect.mX + mThumbRect.mWidth - Res.IMAGE_DLG_THUMBCORNER_BR.mWidth,
                mThumbRect.mY + mThumbRect.mHeight - Res.IMAGE_DLG_THUMBCORNER_BR.mHeight);
            g.SetColorizeImages(false);
            g.DrawImage(Res.IMAGE_DLG_THUMBFRAME, mThumbRect.mX, mThumbRect.mY);
        }

        /// <summary>Renders the finished dialog into mPicture so the flipper can turn it over.</summary>
        public void GetPicture()
        {
#if PEGGLE_IPHONE
            mPictureRect = GetWideRect(mWidth, mDialogRect);
#else
            mPictureRect = mDialogRect;
#endif
            if (mPicture != null && (mPicture.mWidth != mPictureRect.mWidth || mPicture.mHeight != mPictureRect.mHeight))
            {
                mPicture.Dispose();
                mPicture = null;
            }
            if (mPicture == null)
            {
                mPicture = new MemoryImage();
                mPicture.Create(mPictureRect.mWidth, mPictureRect.mHeight);
            }
            else
            {
                mPicture.Clear();
            }
            bool inFrame = Graphics.IsSpriteBatchBegun();
            Graphics g = Graphics.GetNew(mPicture);
            g.Translate(-mPictureRect.mX, -mPictureRect.mY);
            mDrawingPicture = true;
            DrawContent(g);
            mDrawingPicture = false;
            g.EndOffscreen(inFrame);
        }

        public override void Update()
        {
            mFlipper.Update();
            if (mFlipper.GetPercentComplete() == 100.0f)
            {
                if (mFlipper.mDirection == ImageFlipper.FLIP_FORWARD)
                {
                    mPlayButton.SetVisible(true);
                    mBackButton.SetVisible(true);
                }
                else
                {
                    mListener.Closed();
                }
            }
            MarkDirty();
        }

        public bool IsAnimating()
        {
            return mFlipper.GetPercentComplete() != 100.0f;
        }

        public void Show()
        {
            mListener.GetThumbnailInfo(out Image anImage, out TRect aRect);
            GetPicture();
            mFlipper.StartFlip(anImage, aRect, mPicture, mPictureRect, ImageFlipper.FLIP_FORWARD, 0x19);
        }
    }
}
