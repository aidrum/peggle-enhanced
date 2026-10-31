using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>One glyph of a font layer (Sexy::CharData, 0x24 bytes in the binary .dat fonts).</summary>
    public struct CharData
    {
        public char mChar;

        public TRect mImageRect;

        public TPoint mOffset;

        public int mWidth;

        public int mOrder;
    }

    /// <summary>Kerning pair: key is (first char &lt;&lt; 16) | second char.</summary>
    public struct KerningInfo
    {
        public uint mKey;

        public short mOffset;
    }

    public class FontLayer
    {
        public FontData mFontData;

        public string mLayerName = "";

        public List<string> mRequiredTags = new List<string>();

        public List<string> mExcludedTags = new List<string>();

        public CharData[] mCharData = Array.Empty<CharData>();

        public KerningInfo[] mKerningData = Array.Empty<KerningInfo>();

        public Color mColorMult = Color.White;

        public Color mColorAdd = new Color(0, 0, 0, 0);

        public Image mImage;

        public string mImagePath = "";

        public int mDrawMode = -1;

        public TPoint mOffset;

        public int mSpacing;

        public int mMinPointSize = -1;

        public int mMaxPointSize = -1;

        public int mPointSize;

        public int mAscent;

        public int mAscentPadding;

        public int mHeight;

        public int mDefaultHeight;

        public int mLineSpacingOffset;

        public int mBaseOrder;

        public bool mUseAlphaCorrection = true;

        public int mMaxCharHeight = -1;

        public int mCenterOffset;

        public FontLayer(FontData theFontData)
        {
            mFontData = theFontData;
        }

        public bool GetCharData(char theChar, out CharData theData)
        {
            int lo = 0;
            int hi = mCharData.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                char c = mCharData[mid].mChar;
                if (c == theChar)
                {
                    theData = mCharData[mid];
                    return true;
                }
                if (theChar < c)
                {
                    hi = mid - 1;
                }
                else
                {
                    lo = mid + 1;
                }
            }
            theData = default(CharData);
            return false;
        }

        public bool LookupKerningInfo(char theChar, char theSecondChar, out short theOffset)
        {
            uint key = ((uint)theSecondChar << 16) | theChar;
            int lo = 0;
            int hi = mKerningData.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                uint k = mKerningData[mid].mKey;
                if (k == key)
                {
                    theOffset = mKerningData[mid].mOffset;
                    return true;
                }
                if (key < k)
                {
                    hi = mid - 1;
                }
                else
                {
                    lo = mid + 1;
                }
            }
            theOffset = 0;
            return false;
        }
    }

    public class FontData
    {
        private const uint FONT_MAGIC = 0xFF0000BB;

        public bool mInitialized;

        public int mRefCount;

        public SexyAppBase mApp;

        public int mDefaultPointSize;

        /// <summary>Sorted (from, to) character remapping pairs.</summary>
        public KeyValuePair<char, char>[] mCharMap = Array.Empty<KeyValuePair<char, char>>();

        public List<FontLayer> mFontLayerList = new List<FontLayer>();

        public Dictionary<string, FontLayer> mFontLayerMap = new Dictionary<string, FontLayer>();

        public string mSourceFile = "";

        public void Ref()
        {
            mRefCount++;
        }

        public void DeRef()
        {
            mRefCount--;
        }

        private static string ReadString(BinaryReader r)
        {
            int len = r.ReadUInt16();
            return Encoding.Latin1.GetString(r.ReadBytes(len));
        }

        private static List<string> ReadStringVector(BinaryReader r)
        {
            int n = r.ReadUInt16();
            List<string> v = new List<string>(n);
            for (int i = 0; i < n; i++)
            {
                v.Add(ReadString(r));
            }
            return v;
        }

        private static Color ReadColor(BinaryReader r)
        {
            int red = r.ReadInt32();
            int green = r.ReadInt32();
            int blue = r.ReadInt32();
            int alpha = r.ReadInt32();
            return new Color(red, green, blue, alpha);
        }

        /// <summary>
        /// Port of FontData::LoadBinary: reads the compiled ".dat" font produced by PopCap's
        /// font tool (all fonts shipped with Peggle Classic HD use it).
        /// </summary>
        public bool LoadBinary(SexyAppBase theApp, string thePath)
        {
            mApp = theApp;
            mSourceFile = thePath;
            byte[] data = ContentFS.ReadAllBytes(thePath);
            bool ok = false;
            if (data != null)
            {
                using (BinaryReader r = new BinaryReader(new MemoryStream(data)))
                {
                    if (r.ReadUInt32() == FONT_MAGIC)
                    {
                        mDefaultPointSize = r.ReadInt32();
                        int mapCount = r.ReadInt32();
                        mCharMap = new KeyValuePair<char, char>[mapCount];
                        for (int i = 0; i < mapCount; i++)
                        {
                            char from = (char)r.ReadUInt16();
                            char to = (char)r.ReadUInt16();
                            mCharMap[i] = new KeyValuePair<char, char>(from, to);
                        }
                        int layerCount = r.ReadUInt16();
                        for (int l = 0; l < layerCount; l++)
                        {
                            FontLayer layer = new FontLayer(this);
                            mFontLayerList.Add(layer);
                            layer.mLayerName = ReadString(r);
                            mFontLayerMap[layer.mLayerName] = layer;
                            layer.mRequiredTags = ReadStringVector(r);
                            layer.mExcludedTags = ReadStringVector(r);
                            int numChars = r.ReadUInt16();
                            layer.mCharData = new CharData[numChars];
                            for (int c = 0; c < numChars; c++)
                            {
                                CharData cd;
                                cd.mChar = (char)r.ReadUInt16();
                                r.ReadUInt16();
                                cd.mImageRect = new TRect(r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
                                cd.mOffset = new TPoint(r.ReadInt32(), r.ReadInt32());
                                cd.mWidth = r.ReadInt32();
                                cd.mOrder = r.ReadInt32();
                                layer.mCharData[c] = cd;
                            }
                            int numKern = r.ReadUInt16();
                            layer.mKerningData = new KerningInfo[numKern];
                            for (int k = 0; k < numKern; k++)
                            {
                                KerningInfo ki;
                                ki.mKey = r.ReadUInt32();
                                ki.mOffset = r.ReadInt16();
                                r.ReadUInt16();
                                layer.mKerningData[k] = ki;
                            }
                            layer.mColorMult = ReadColor(r);
                            layer.mColorAdd = ReadColor(r);
                            layer.mImagePath = GlobalStaticVars.GetResourceDir() + ReadString(r);
                            layer.mImage = theApp.GetImage(layer.mImagePath, true);
                            layer.mDrawMode = r.ReadInt32();
                            layer.mOffset = new TPoint(r.ReadInt32(), r.ReadInt32());
                            layer.mSpacing = r.ReadInt32();
                            layer.mMinPointSize = r.ReadInt32();
                            layer.mMaxPointSize = r.ReadInt32();
                            layer.mPointSize = r.ReadInt32();
                            layer.mAscent = r.ReadInt32();
                            layer.mAscentPadding = r.ReadInt32();
                            layer.mHeight = r.ReadInt32();
                            layer.mDefaultHeight = r.ReadInt32();
                            layer.mLineSpacingOffset = r.ReadInt32();
                            layer.mBaseOrder = r.ReadInt32();
                            layer.mUseAlphaCorrection = r.ReadByte() != 0;
                            layer.mMaxCharHeight = r.ReadInt32();
                            layer.mCenterOffset = r.ReadInt32();
                        }
                        ok = r.ReadUInt32() == FONT_MAGIC;
                    }
                }
            }
            mInitialized = ok;
            return ok;
        }
    }

    public class ActiveFontLayer
    {
        public FontLayer mBaseFontLayer;

        public Image mScaledImage;

        public bool mOwnsImage;

        public TRect GetRectForChar(char theChar)
        {
            if (mBaseFontLayer.GetCharData(theChar, out CharData cd))
            {
                return cd.mImageRect;
            }
            return new TRect(0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Bitmap font (Sexy::ImageFont) as implemented in Peggle Classic HD. Only unscaled drawing
    /// is supported, like the original (scaled layers print "UNSUPPORTED").
    /// </summary>
    public class ImageFont : Font
    {
        private struct RenderCommand
        {
            public Image mImage;

            public int mX;

            public int mY;

            public TRect mSrc;

            public int mDrawMode;

            public Color mColor;

            public int mNext;
        }

        private static readonly object sRenderLock = new object();

        private static readonly RenderCommand[] sRenderCommandPool = new RenderCommand[4096];

        private static readonly int[] sRenderHead = new int[256];

        private static readonly int[] sRenderTail = new int[256];

        public FontData mFontData;

        public int mPointSize;

        public List<string> mTagVector = new List<string>();

        public bool mActivateAllLayers;

        public bool mActiveListValid;

        public List<ActiveFontLayer> mActiveLayerList = new List<ActiveFontLayer>();

        public double mScale = 1.0;

        public bool mForceScaledImagesWhite;

        public ImageFont(SexyAppBase theApp, string theFontDescFileName)
        {
            mFontData = new FontData();
            mFontData.Ref();
            string ext = Path.GetExtension(theFontDescFileName).ToLowerInvariant();
            if (ext == ".dat")
            {
                mFontData.LoadBinary(theApp, theFontDescFileName);
            }
            else
            {
                Debug.OutputDebug("ImageFont: only binary .dat fonts are supported: " + theFontDescFileName);
            }
            mPointSize = mFontData.mDefaultPointSize;
            mActivateAllLayers = false;
            GenerateActiveFontLayers();
            mActiveListValid = true;
            mForceScaledImagesWhite = false;
        }

        public ImageFont(Image theFontImage, string theFontDescFileName)
            : this(GlobalStaticVars.gSexyAppBase, theFontDescFileName)
        {
        }

        public ImageFont(ImageFont theImageFont) : base(theImageFont)
        {
            mFontData = theImageFont.mFontData;
            mFontData.Ref();
            mPointSize = theImageFont.mPointSize;
            mTagVector = new List<string>(theImageFont.mTagVector);
            mActivateAllLayers = theImageFont.mActivateAllLayers;
            mScale = theImageFont.mScale;
            mForceScaledImagesWhite = theImageFont.mForceScaledImagesWhite;
            GenerateActiveFontLayers();
            mActiveListValid = true;
        }

        public override void Dispose()
        {
            mFontData?.DeRef();
        }

        private static int RoundScaled(double v)
        {
            return (int)(v < 0.0 ? v - 0.501 : v + 0.501);
        }

        public virtual void GenerateActiveFontLayers()
        {
            if (!mFontData.mInitialized)
            {
                return;
            }
            mActiveLayerList.Clear();
            mAscent = 0;
            mAscentPadding = 0;
            mHeight = 0;
            mLineSpacingOffset = 0;
            mMaxCharHeight = -1;
            mCenterOffset = 0;
            bool first = true;
            foreach (FontLayer layer in mFontData.mFontLayerList)
            {
                if (mPointSize < layer.mMinPointSize || (mPointSize > layer.mMaxPointSize && layer.mMaxPointSize != -1))
                {
                    continue;
                }
                bool active = true;
                foreach (string tag in layer.mRequiredTags)
                {
                    if (!mTagVector.Contains(tag))
                    {
                        active = false;
                    }
                }
                foreach (string tag in mTagVector)
                {
                    if (layer.mExcludedTags.Contains(tag))
                    {
                        active = false;
                    }
                }
                if (!mActivateAllLayers && !active)
                {
                    continue;
                }
                ActiveFontLayer activeLayer = new ActiveFontLayer();
                activeLayer.mBaseFontLayer = layer;
                mActiveLayerList.Add(activeLayer);
                double scale = mScale;
                if (scale != 1.0 || (layer.mPointSize != 0 && mPointSize != layer.mPointSize))
                {
                    Debug.OutputDebug("**** UNSUPPORTED ****");
                }
                else
                {
                    activeLayer.mScaledImage = layer.mImage;
                    activeLayer.mOwnsImage = false;
                }
                int ascent = RoundScaled(layer.mAscent * scale);
                if (mAscent < ascent)
                {
                    mAscent = ascent;
                }
                int height = RoundScaled((layer.mHeight == 0 ? layer.mDefaultHeight : layer.mHeight) * scale);
                if (mHeight < height)
                {
                    mHeight = height;
                }
                int ascentPadding = RoundScaled(layer.mAscentPadding * scale);
                if (first || ascentPadding < mAscentPadding)
                {
                    mAscentPadding = ascentPadding;
                }
                int lineSpacingOffset = RoundScaled(layer.mLineSpacingOffset * scale);
                if (first || mLineSpacingOffset < lineSpacingOffset)
                {
                    mLineSpacingOffset = lineSpacingOffset;
                }
                int maxCharHeight = RoundScaled(layer.mMaxCharHeight * scale);
                if (first || mMaxCharHeight < maxCharHeight)
                {
                    mMaxCharHeight = maxCharHeight;
                    mCenterOffset = RoundScaled(layer.mCenterOffset * scale);
                }
                first = false;
            }
        }

        public char GetMappedChar(char theChar)
        {
            KeyValuePair<char, char>[] map = mFontData.mCharMap;
            int lo = 0;
            int hi = map.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (map[mid].Key == theChar)
                {
                    return map[mid].Value;
                }
                if (theChar < map[mid].Key)
                {
                    hi = mid - 1;
                }
                else
                {
                    lo = mid + 1;
                }
            }
            return theChar;
        }

        public override int StringWidth(string theString)
        {
            int width = 0;
            char prev = '\0';
            for (int i = 0; i < theString.Length; i++)
            {
                char c = theString[i];
                width += CharWidthKern(c, prev);
                prev = c;
            }
            return width;
        }

        public override int CharWidthKern(char theChar, char thePrevChar)
        {
            Prepare();
            int maxWidth = 0;
            double pointSize = mPointSize * mScale;
            theChar = GetMappedChar(theChar);
            if (thePrevChar != '\0')
            {
                thePrevChar = GetMappedChar(thePrevChar);
            }
            foreach (ActiveFontLayer active in mActiveLayerList)
            {
                FontLayer layer = active.mBaseFontLayer;
                if (!layer.GetCharData(theChar, out CharData cd))
                {
                    continue;
                }
                int layerPointSize = layer.mPointSize;
                int charWidth;
                int spacing;
                if (layerPointSize == 0)
                {
                    charWidth = RoundScaled(cd.mWidth * mScale);
                    spacing = 0;
                    if (thePrevChar != '\0')
                    {
                        spacing = layer.mSpacing;
                        if (layer.LookupKerningInfo(theChar, thePrevChar, out short kern))
                        {
                            spacing = (int)(spacing + kern * mScale);
                        }
                    }
                }
                else
                {
                    charWidth = RoundScaled(cd.mWidth * pointSize / layerPointSize);
                    spacing = 0;
                    if (thePrevChar != '\0')
                    {
                        spacing = layer.mSpacing;
                        if (layer.LookupKerningInfo(theChar, thePrevChar, out short kern))
                        {
                            spacing = (int)(spacing + kern * pointSize / layerPointSize);
                        }
                    }
                }
                if (maxWidth < charWidth + spacing)
                {
                    maxWidth = charWidth + spacing;
                }
            }
            return maxWidth;
        }

        public override int CharWidth(char theChar)
        {
            return CharWidthKern(theChar, '\0');
        }

        public override void DrawString(Graphics g, int theX, int theY, string theString, Color theColor, TRect theClipRect)
        {
            DrawStringEx(g, theX, theY, theString, theColor, theClipRect, null, out _);
        }

        public void DrawStringEx(Graphics g, int theX, int theY, string theString, Color theColor, TRect theClipRect, List<TRect> theDrawnAreas, out int theWidth)
        {
            lock (sRenderLock)
            {
                Array.Clear(sRenderHead, 0, sRenderHead.Length);
                Array.Clear(sRenderTail, 0, sRenderTail.Length);
                theDrawnAreas?.Clear();
                if (!mFontData.mInitialized)
                {
                    theWidth = 0;
                    return;
                }
                Prepare();
                bool colorizeImages = g.GetColorizeImages();
                g.SetColorizeImages(true);
                int curX = theX;
                int poolIdx = 0;
                for (int i = 0; i < theString.Length; i++)
                {
                    char c = GetMappedChar(theString[i]);
                    char next = '\0';
                    if (i < theString.Length - 1)
                    {
                        next = GetMappedChar(theString[i + 1]);
                    }
                    int maxXPos = curX;
                    foreach (ActiveFontLayer active in mActiveLayerList)
                    {
                        FontLayer layer = active.mBaseFontLayer;
                        if (!layer.GetCharData(c, out CharData cd))
                        {
                            continue;
                        }
                        int layerXPos = curX;
                        double scale = mScale;
                        if (layer.mPointSize != 0)
                        {
                            scale *= (double)mPointSize / layer.mPointSize;
                        }
                        int imageX;
                        int imageY;
                        int charWidth;
                        int spacing;
                        if (scale == 1.0)
                        {
                            imageX = curX + layer.mOffset.mX + cd.mOffset.mX;
                            imageY = theY - (layer.mAscent - layer.mOffset.mY - cd.mOffset.mY);
                            charWidth = cd.mWidth;
                            spacing = 0;
                            if (next != '\0')
                            {
                                spacing = layer.mSpacing;
                                if (layer.LookupKerningInfo(next, c, out short kern))
                                {
                                    spacing += kern;
                                }
                            }
                        }
                        else
                        {
                            imageX = (int)Math.Floor((layer.mOffset.mX + cd.mOffset.mX) * scale) + curX;
                            imageY = theY - (int)Math.Floor((layer.mAscent - layer.mOffset.mY - cd.mOffset.mY) * scale);
                            charWidth = (int)(cd.mWidth * scale);
                            spacing = 0;
                            if (next != '\0')
                            {
                                spacing = layer.mSpacing;
                                if (layer.LookupKerningInfo(next, c, out short kern))
                                {
                                    spacing += (int)(kern * scale);
                                }
                            }
                        }
                        Color color = new Color(
                            Math.Min(layer.mColorMult.R * theColor.R / 255 + layer.mColorAdd.R, 255),
                            Math.Min(layer.mColorMult.G * theColor.G / 255 + layer.mColorAdd.G, 255),
                            Math.Min(layer.mColorMult.B * theColor.B / 255 + layer.mColorAdd.B, 255),
                            Math.Min(layer.mColorMult.A * theColor.A / 255 + layer.mColorAdd.A, 255));
                        int order = layer.mBaseOrder + cd.mOrder;
                        if (poolIdx >= sRenderCommandPool.Length)
                        {
                            break;
                        }
                        ref RenderCommand cmd = ref sRenderCommandPool[poolIdx];
                        cmd.mImage = active.mScaledImage;
                        cmd.mColor = color;
                        cmd.mX = imageX;
                        cmd.mY = imageY;
                        cmd.mSrc = active.GetRectForChar(c);
                        cmd.mDrawMode = layer.mDrawMode;
                        cmd.mNext = 0;
                        int bucket = Math.Min(Math.Max(order + 128, 0), 255);
                        // Lists are 1-based so that 0 means "empty".
                        if (sRenderTail[bucket] == 0)
                        {
                            sRenderTail[bucket] = poolIdx + 1;
                            sRenderHead[bucket] = poolIdx + 1;
                        }
                        else
                        {
                            sRenderCommandPool[sRenderTail[bucket] - 1].mNext = poolIdx + 1;
                            sRenderTail[bucket] = poolIdx + 1;
                        }
                        poolIdx++;
                        theDrawnAreas?.Add(new TRect(imageX, imageY, cmd.mSrc.mWidth, cmd.mSrc.mHeight));
                        layerXPos += charWidth + spacing;
                        if (layerXPos > maxXPos)
                        {
                            maxXPos = layerXPos;
                        }
                    }
                    curX = maxXPos;
                }
                theWidth = curX - theX;
                Color origColor = g.GetColor();
                for (int bucket = 0; bucket < 256; bucket++)
                {
                    for (int idx = sRenderHead[bucket]; idx != 0; idx = sRenderCommandPool[idx - 1].mNext)
                    {
                        ref RenderCommand cmd = ref sRenderCommandPool[idx - 1];
                        Graphics.DrawMode oldDrawMode = g.GetDrawMode();
                        if (cmd.mDrawMode != -1)
                        {
                            g.SetDrawMode(cmd.mDrawMode);
                        }
                        g.SetColor(cmd.mColor);
                        if (cmd.mImage != null)
                        {
                            g.DrawImage(cmd.mImage, cmd.mX, cmd.mY, cmd.mSrc);
                        }
                        g.SetDrawMode(oldDrawMode);
                    }
                }
                g.SetColor(origColor);
                g.SetColorizeImages(colorizeImages);
            }
        }

        public override Font Duplicate()
        {
            return new ImageFont(this);
        }

        public virtual void SetPointSize(int thePointSize)
        {
            mPointSize = thePointSize;
            mActiveListValid = false;
        }

        public virtual int GetPointSize()
        {
            return mPointSize;
        }

        public virtual void SetScale(double theScale)
        {
            mScale = theScale;
            mActiveListValid = false;
        }

        public virtual int GetDefaultPointSize()
        {
            return mFontData.mDefaultPointSize;
        }

        public virtual bool AddTag(string theTagName)
        {
            if (HasTag(theTagName))
            {
                return false;
            }
            mTagVector.Add(theTagName.ToUpperInvariant());
            mActiveListValid = false;
            return true;
        }

        public virtual bool RemoveTag(string theTagName)
        {
            bool removed = mTagVector.Remove(theTagName.ToUpperInvariant());
            if (removed)
            {
                mActiveListValid = false;
            }
            return removed;
        }

        public virtual bool HasTag(string theTagName)
        {
            return mTagVector.Contains(theTagName.ToUpperInvariant());
        }

        public virtual void Prepare()
        {
            if (!mActiveListValid)
            {
                GenerateActiveFontLayers();
                mActiveListValid = true;
            }
        }
    }
}
