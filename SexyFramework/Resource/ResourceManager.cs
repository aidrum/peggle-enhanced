using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sexy
{
    public class ResourceManagerException : Exception
    {
        public ResourceManagerException(string theMessage) : base(theMessage)
        {
        }
    }

    /// <summary>
    /// Port of Peggle Classic HD's Sexy::ResourceManager. The iOS build does not parse
    /// resources.xml at run time; ReadAllMaps loads the precompiled binary maps in
    /// "&lt;lang&gt;.lproj/data_res/1024x768/" (images, sounds, fonts, atlases, atlas entries).
    /// Resources are grouped by their XML &lt;resources id&gt; and loaded per group.
    /// </summary>
    public class ResourceManager
    {
        public static object DrawLocker = new object();

        private const uint RES_MAGIC = 0xFFAABBCC;

        public enum ResType
        {
            ResType_Image,
            ResType_Sound,
            ResType_Font,
            ResType_Atlas,
            ResType_AtlasImage
        }

        public class BaseRes
        {
            public ResType mType;

            public string mId;

            public string mResGroup;

            public string mPath;

            public bool mFromProgram;

            public virtual void DeleteResource()
            {
            }
        }

        public class ImageRes : BaseRes
        {
            public Image mImage;

            public string mAlphaImage = "";

            public string mAlphaGridImage = "";

            public string mVariant = "";

            public bool mPalletize;

            public bool mA4R4G4B4;

            public bool mA8R8G8B8;

            public bool mR5G6B5;

            public bool mA1R5G5B5;

            public bool mDDSurface;

            public bool mPurgeBits;

            public bool mMinimizeSubdivisions;

            public bool mAutoFindAlpha;

            public int mRows = 1;

            public int mCols = 1;

            public uint mAlphaColor = 0xFFFFFF;

            public AnimInfo mAnimInfo = new AnimInfo();

            public ImageRes()
            {
                mType = ResType.ResType_Image;
            }

            public override void DeleteResource()
            {
                mImage?.Dispose();
                mImage = null;
            }
        }

        public class AtlasRes : ImageRes
        {
            public List<AtlasImageRes> mEntries = new List<AtlasImageRes>();

            public AtlasRes()
            {
                mType = ResType.ResType_Atlas;
            }
        }

        public class AtlasImageRes : BaseRes
        {
            public int mX;

            public int mY;

            public int mWidth;

            public int mHeight;

            public int mRows = 1;

            public int mCols = 1;

            public AnimType mAnimType;

            public int mFrameDelay;

            public int mBeginDelay;

            public int mEndDelay;

            public List<int> mPerFrameDelay = new List<int>();

            public List<int> mFrameMap = new List<int>();

            public AtlasRes mParent;

            public AtlasImageRes()
            {
                mType = ResType.ResType_AtlasImage;
            }
        }

        public class SoundRes : BaseRes
        {
            public int mSoundId = -1;

            public double mVolume = -1.0;

            public int mPanning;

            public SoundRes()
            {
                mType = ResType.ResType_Sound;
            }

            public override void DeleteResource()
            {
                if (mSoundId >= 0)
                {
                    GlobalStaticVars.gSexyAppBase.mSoundManager.ReleaseSound((uint)mSoundId);
                }
                mSoundId = -1;
            }
        }

        public class FontRes : BaseRes
        {
            public Font mFont;

            public Image mImage;

            public string mImagePath = "";

            public string mTags = "";

            public bool mSysFont;

            public bool mBold;

            public bool mItalic;

            public bool mShadow;

            public bool mUnderline;

            public int mSize;

            public FontRes()
            {
                mType = ResType.ResType_Font;
            }

            public override void DeleteResource()
            {
                mFont?.Dispose();
                mFont = null;
                mImage?.Dispose();
                mImage = null;
            }
        }

        public Dictionary<string, BaseRes> mImageMap = new Dictionary<string, BaseRes>();

        public Dictionary<string, BaseRes> mSoundMap = new Dictionary<string, BaseRes>();

        public Dictionary<string, BaseRes> mFontMap = new Dictionary<string, BaseRes>();

        public Dictionary<string, BaseRes> mAtlasMap = new Dictionary<string, BaseRes>();

        public Dictionary<string, BaseRes> mAtlasImageMap = new Dictionary<string, BaseRes>();

        public string mError = "";

        public bool mHasFailed;

        public SexyAppBase mApp;

        public string mCurResGroup = "";

        public bool mAllowMissingProgramResources;

        public Dictionary<string, List<BaseRes>> mResGroupMap = new Dictionary<string, List<BaseRes>>(StringComparer.OrdinalIgnoreCase);

        public List<BaseRes> mCurResGroupList;

        public int mCurResGroupListItr;

        public HashSet<string> mLoadedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Port: groups unloaded with DeleteResourcesLater. They count as unloaded, but their
        /// resources stay until DoDeferredDeletes (loading one again just keeps it).
        /// </summary>
        public HashSet<string> mDeferredDeletes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ResourceManager(SexyAppBase theApp)
        {
            mApp = theApp;
            mHasFailed = false;
            mAllowMissingProgramResources = false;
            mCurResGroupList = null;
        }

        public void Dispose()
        {
            DeleteMap(mImageMap);
            DeleteMap(mSoundMap);
            DeleteMap(mFontMap);
            DeleteMap(mAtlasMap);
            mAtlasImageMap.Clear();
        }

        private void DeleteMap(Dictionary<string, BaseRes> theMap)
        {
            foreach (BaseRes res in theMap.Values)
            {
                res.DeleteResource();
            }
            theMap.Clear();
        }

        public static string GetResourceDir()
        {
            return "";
        }

        public bool IsGroupLoaded(string theGroup)
        {
            return mLoadedGroups.Contains(theGroup) && !mDeferredDeletes.Contains(theGroup);
        }

        public void DeleteResourcesLater(string theGroup)
        {
            mDeferredDeletes.Add(theGroup);
        }

        public void DoDeferredDeletes()
        {
            if (mDeferredDeletes.Count == 0)
            {
                return;
            }
            string[] aGroups = new string[mDeferredDeletes.Count];
            mDeferredDeletes.CopyTo(aGroups);
            foreach (string aGroup in aGroups)
            {
                DeleteResources(aGroup);
            }
        }

        public void DeleteResources(Dictionary<string, BaseRes> theMap, string theGroup)
        {
            foreach (BaseRes res in theMap.Values)
            {
                if (string.IsNullOrEmpty(theGroup) || res.mResGroup == theGroup)
                {
                    res.DeleteResource();
                }
            }
        }

        public void DeleteResources(string theGroup)
        {
            DeleteResources(mImageMap, theGroup);
            DeleteResources(mSoundMap, theGroup);
            DeleteResources(mFontMap, theGroup);
            DeleteResources(mAtlasMap, theGroup);
            mLoadedGroups.Remove(theGroup);
            mDeferredDeletes.Remove(theGroup);
        }

        public string GetErrorText()
        {
            return mError;
        }

        public bool HadError()
        {
            return mHasFailed;
        }

        public bool Fail(string theErrorText)
        {
            if (!mHasFailed)
            {
                mHasFailed = true;
                mError = theErrorText;
            }
            Debug.OutputDebug("ResourceManager: " + theErrorText);
            return false;
        }

        #region Binary resource maps

        private static string ReadStringRes(BinaryReader r)
        {
            int len = r.ReadUInt16();
            return Encoding.Latin1.GetString(r.ReadBytes(len));
        }

        private static List<int> ReadVectorRes(BinaryReader r)
        {
            int n = r.ReadInt32();
            List<int> v = new List<int>(n);
            for (int i = 0; i < n; i++)
            {
                v.Add(r.ReadInt32());
            }
            return v;
        }

        private static void ReadImageFields(BinaryReader r, ImageRes res)
        {
            res.mAlphaImage = ReadStringRes(r);
            res.mAlphaGridImage = ReadStringRes(r);
            res.mVariant = ReadStringRes(r);
            res.mPalletize = r.ReadByte() != 0;
            res.mA4R4G4B4 = r.ReadByte() != 0;
            res.mA8R8G8B8 = r.ReadByte() != 0;
            res.mR5G6B5 = r.ReadByte() != 0;
            res.mA1R5G5B5 = r.ReadByte() != 0;
            res.mDDSurface = r.ReadByte() != 0;
            res.mPurgeBits = r.ReadByte() != 0;
            res.mMinimizeSubdivisions = r.ReadByte() != 0;
            res.mAutoFindAlpha = r.ReadByte() != 0;
            res.mRows = r.ReadInt32();
            res.mCols = r.ReadInt32();
            res.mAlphaColor = r.ReadUInt32();
            // AnimInfo: type, frame delay, cel count, two std::vectors (pointers, meaningless on disk), total time.
            byte[] anim = r.ReadBytes(0x28);
            res.mAnimInfo.mAnimType = (AnimType)BitConverter.ToInt32(anim, 0);
            res.mAnimInfo.mFrameDelay = BitConverter.ToInt32(anim, 4);
            res.mAnimInfo.mNumCels = BitConverter.ToInt32(anim, 8);
            res.mAnimInfo.mTotalAnimTime = BitConverter.ToInt32(anim, 0x24);
        }

        public void ReadRes(Dictionary<string, BaseRes> theMap, string thePath)
        {
            byte[] data = ContentFS.ReadAllBytes(thePath);
            if (data == null)
            {
                return;
            }
            using (BinaryReader r = new BinaryReader(new MemoryStream(data)))
            {
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    ResType type = (ResType)r.ReadInt32();
                    string id = ReadStringRes(r);
                    string group = ReadStringRes(r);
                    string path = ReadStringRes(r);
                    bool fromProgram = (r.ReadByte() & 1) != 0;
                    BaseRes res;
                    switch (type)
                    {
                    case ResType.ResType_Image:
                    {
                        ImageRes imageRes = new ImageRes();
                        ReadImageFields(r, imageRes);
                        res = imageRes;
                        break;
                    }
                    case ResType.ResType_Atlas:
                    {
                        AtlasRes atlasRes = new AtlasRes();
                        ReadImageFields(r, atlasRes);
                        res = atlasRes;
                        break;
                    }
                    case ResType.ResType_Sound:
                    {
                        SoundRes soundRes = new SoundRes();
                        soundRes.mSoundId = r.ReadInt32();
                        soundRes.mVolume = r.ReadDouble();
                        soundRes.mPanning = r.ReadInt32();
                        res = soundRes;
                        break;
                    }
                    case ResType.ResType_Font:
                    {
                        FontRes fontRes = new FontRes();
                        fontRes.mImagePath = ReadStringRes(r);
                        fontRes.mTags = ReadStringRes(r);
                        fontRes.mSysFont = r.ReadByte() != 0;
                        fontRes.mBold = r.ReadByte() != 0;
                        fontRes.mItalic = r.ReadByte() != 0;
                        fontRes.mShadow = r.ReadByte() != 0;
                        fontRes.mUnderline = r.ReadByte() != 0;
                        fontRes.mSize = r.ReadInt32();
                        res = fontRes;
                        break;
                    }
                    case ResType.ResType_AtlasImage:
                    {
                        AtlasImageRes atlasImageRes = new AtlasImageRes();
                        atlasImageRes.mX = r.ReadInt32();
                        atlasImageRes.mY = r.ReadInt32();
                        atlasImageRes.mWidth = r.ReadInt32();
                        atlasImageRes.mHeight = r.ReadInt32();
                        atlasImageRes.mRows = r.ReadInt32();
                        atlasImageRes.mCols = r.ReadInt32();
                        atlasImageRes.mAnimType = (AnimType)r.ReadInt32();
                        atlasImageRes.mFrameDelay = r.ReadInt32();
                        atlasImageRes.mBeginDelay = r.ReadInt32();
                        atlasImageRes.mEndDelay = r.ReadInt32();
                        atlasImageRes.mPerFrameDelay = ReadVectorRes(r);
                        atlasImageRes.mFrameMap = ReadVectorRes(r);
                        string parentId = ReadStringRes(r);
                        if (mAtlasMap.TryGetValue(parentId, out BaseRes parent))
                        {
                            atlasImageRes.mParent = (AtlasRes)parent;
                            atlasImageRes.mParent.mEntries.Add(atlasImageRes);
                        }
                        else
                        {
                            Debug.OutputDebug("ERROR: no parent Atlas for res " + id);
                        }
                        // Atlas entries are not loaded on their own: the font byte is uninitialised
                        // in the shipped data, so the flag is ignored for them.
                        fromProgram = false;
                        res = atlasImageRes;
                        break;
                    }
                    default:
                        throw new InvalidDataException("Unknown resource type " + (int)type + " in " + thePath);
                    }
                    res.mType = type;
                    res.mId = id;
                    res.mResGroup = group;
                    res.mPath = path;
                    res.mFromProgram = fromProgram;
                    uint magic = r.ReadUInt32();
                    if (magic != RES_MAGIC)
                    {
                        Debug.OutputDebug("___________ERROR: magic not read correctly " + id);
                    }
                    InsertToMap(theMap, res);
                }
            }
        }

        private void InsertToMap(Dictionary<string, BaseRes> theMap, BaseRes theRes)
        {
            if (theMap.ContainsKey(theRes.mId))
            {
                Debug.OutputDebug("___________ERROR: resource already defined " + theRes.mId);
                return;
            }
            theMap.Add(theRes.mId, theRes);
            if (theRes.mType == ResType.ResType_AtlasImage)
            {
                return;
            }
            if (!mResGroupMap.TryGetValue(theRes.mResGroup, out List<BaseRes> list))
            {
                list = new List<BaseRes>();
                mResGroupMap.Add(theRes.mResGroup, list);
            }
            list.Add(theRes);
        }

        public void ReadAllMaps()
        {
            string dir = "data_res/" + GlobalStaticVars.mResPathPrefix;
            ReadRes(mImageMap, dir + "imageMap.dat");
            ReadRes(mSoundMap, dir + "soundMap.dat");
            ReadRes(mFontMap, dir + "fontMap.dat");
            ReadRes(mAtlasMap, dir + "atlasMap.dat");
            ReadRes(mAtlasImageMap, dir + "atlasImageMap.dat");
        }

        /// <summary>
        /// Port: after ContentFS.Language changed, re-reads the maps and drops the images whose file
        /// is localized (or whose atlas layout changed), then loads them again for the groups that
        /// are loaded. Sounds and fonts are the same in every language and are kept. Returns the
        /// groups whose images were reloaded, so their Res fields can be extracted again.
        /// </summary>
        public List<string> ReloadLocalizedImages()
        {
            DoDeferredDeletes();
            ResourceManager aNewMaps = new ResourceManager(mApp);
            aNewMaps.ReadAllMaps();
            HashSet<AtlasRes> aMovedAtlases = new HashSet<AtlasRes>();
            foreach (KeyValuePair<string, BaseRes> entry in mAtlasImageMap)
            {
                AtlasImageRes res = (AtlasImageRes)entry.Value;
                if (!aNewMaps.mAtlasImageMap.TryGetValue(entry.Key, out BaseRes newBase))
                {
                    continue;
                }
                AtlasImageRes newRes = (AtlasImageRes)newBase;
                if (res.mX != newRes.mX || res.mY != newRes.mY || res.mWidth != newRes.mWidth || res.mHeight != newRes.mHeight)
                {
                    res.mX = newRes.mX;
                    res.mY = newRes.mY;
                    res.mWidth = newRes.mWidth;
                    res.mHeight = newRes.mHeight;
                    if (res.mParent != null)
                    {
                        aMovedAtlases.Add(res.mParent);
                    }
                }
            }
            List<string> aGroups = new List<string>();
            foreach (Dictionary<string, BaseRes> map in new[] { mImageMap, mAtlasMap })
            {
                Dictionary<string, BaseRes> newMap = map == mImageMap ? aNewMaps.mImageMap : aNewMaps.mAtlasMap;
                foreach (BaseRes baseRes in map.Values)
                {
                    ImageRes res = (ImageRes)baseRes;
                    bool changed = ContentFS.IsLocalized(res.mPath) || (res is AtlasRes atlas && aMovedAtlases.Contains(atlas));
                    if (newMap.TryGetValue(res.mId, out BaseRes newRes) && newRes.mPath != res.mPath)
                    {
                        // The iPhone maps name the .lproj folder in the path.
                        res.mPath = newRes.mPath;
                        changed = true;
                    }
                    if (changed && res.mImage != null)
                    {
                        res.DeleteResource();
                        if (!aGroups.Contains(res.mResGroup))
                        {
                            aGroups.Add(res.mResGroup);
                        }
                    }
                }
            }
            foreach (string aGroup in aGroups)
            {
                StartLoadResources(aGroup);
                while (LoadNextResource())
                {
                }
            }
            return aGroups;
        }

        #endregion

        #region Loading

        public virtual bool DoLoadImage(ImageRes theRes)
        {
            if (string.IsNullOrEmpty(theRes.mPath))
            {
                return Fail("Image has no path: " + theRes.mId);
            }
            Image image;
            if (theRes is AtlasRes atlasRes)
            {
                List<Microsoft.Xna.Framework.Rectangle> rects = new List<Microsoft.Xna.Framework.Rectangle>();
                foreach (AtlasImageRes entry in atlasRes.mEntries)
                {
                    rects.Add(new Microsoft.Xna.Framework.Rectangle(entry.mX, entry.mY, entry.mWidth, entry.mHeight));
                }
                Microsoft.Xna.Framework.Graphics.Texture2D texture = ImageFileLoader.LoadAtlas(GlobalStaticVars.g.GraphicsDevice, theRes.mPath, rects);
                image = texture == null ? null : new Image(texture);
                if (texture != null)
                {
                    texture.Name = theRes.mPath;
                }
            }
            else
            {
                image = mApp.GetImage(theRes.mPath, true);
            }
            if (image == null)
            {
                return Fail("Failed to load image: " + theRes.mPath);
            }
            theRes.mImage = image;
            if (theRes.mAnimInfo.mAnimType != AnimType.AnimType_None)
            {
                image.mAnimInfo = new AnimInfo(theRes.mAnimInfo);
            }
            image.mNumRows = theRes.mRows;
            image.mNumCols = theRes.mCols;
            ResourceLoadedHook(theRes);
            return true;
        }

        public virtual bool DoLoadSound(SoundRes theRes)
        {
            int soundId = mApp.mSoundManager.GetFreeSoundId();
            if (soundId < 0)
            {
                return Fail("Out of free sound ids");
            }
            if (!mApp.mSoundManager.LoadSound((uint)soundId, GetResourceDir() + theRes.mPath))
            {
                return Fail("Failed to load sound: " + theRes.mPath);
            }
            if (theRes.mVolume >= 0.0)
            {
                mApp.mSoundManager.SetBaseVolume((uint)soundId, theRes.mVolume);
            }
            if (theRes.mPanning != 0)
            {
                mApp.mSoundManager.SetBasePan((uint)soundId, theRes.mPanning);
            }
            theRes.mSoundId = soundId;
            ResourceLoadedHook(theRes);
            return true;
        }

        public virtual bool DoLoadFont(FontRes theRes)
        {
            if (theRes.mSysFont)
            {
                return Fail("SysFont not supported: " + theRes.mId);
            }
            ImageFont font;
            if (!string.IsNullOrEmpty(theRes.mImagePath))
            {
                Image image = mApp.GetImage(theRes.mImagePath, true);
                if (image == null)
                {
                    return Fail("Failed to load image: " + theRes.mImagePath);
                }
                theRes.mImage = image;
                font = new ImageFont(image, GetResourceDir() + theRes.mPath);
            }
            else if (theRes.mPath.StartsWith("!ref:"))
            {
                Font refFont = GetFont(theRes.mPath.Substring(5));
                if (refFont == null)
                {
                    return Fail("Ref font not found: " + theRes.mPath.Substring(5));
                }
                font = (ImageFont)refFont.Duplicate();
            }
            else
            {
                font = new ImageFont(mApp, GetResourceDir() + theRes.mPath);
            }
            if (font.mFontData == null || !font.mFontData.mInitialized)
            {
                font.Dispose();
                return Fail("Failed to load font: " + theRes.mPath);
            }
            if (!string.IsNullOrEmpty(theRes.mTags))
            {
                foreach (string tag in theRes.mTags.Split(new[] { ',', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    font.AddTag(tag);
                }
                font.Prepare();
            }
            theRes.mFont = font;
            ResourceLoadedHook(theRes);
            return true;
        }

        public virtual void ResourceLoadedHook(BaseRes theRes)
        {
        }

        public void StartLoadResources(string theGroup)
        {
            mError = "";
            mHasFailed = false;
            mCurResGroup = theGroup;
            if (!mResGroupMap.TryGetValue(theGroup, out mCurResGroupList))
            {
                mCurResGroupList = new List<BaseRes>();
                mResGroupMap[theGroup] = mCurResGroupList;
            }
            mCurResGroupListItr = 0;
        }

        public virtual bool LoadNextResource()
        {
            if (HadError() || mCurResGroupList == null)
            {
                return false;
            }
            while (mCurResGroupListItr < mCurResGroupList.Count)
            {
                BaseRes res = mCurResGroupList[mCurResGroupListItr++];
                if (res.mFromProgram)
                {
                    continue;
                }
                switch (res.mType)
                {
                case ResType.ResType_Image:
                case ResType.ResType_Atlas:
                {
                    ImageRes imageRes = (ImageRes)res;
                    if (imageRes.mImage == null)
                    {
                        return DoLoadImage(imageRes);
                    }
                    break;
                }
                case ResType.ResType_Sound:
                {
                    SoundRes soundRes = (SoundRes)res;
                    if (soundRes.mSoundId == -1)
                    {
                        return DoLoadSound(soundRes);
                    }
                    break;
                }
                case ResType.ResType_Font:
                {
                    FontRes fontRes = (FontRes)res;
                    if (fontRes.mFont == null)
                    {
                        return DoLoadFont(fontRes);
                    }
                    break;
                }
                }
            }
            return false;
        }

        public bool LoadResources(string theGroup)
        {
            if (mDeferredDeletes.Remove(theGroup) && mLoadedGroups.Contains(theGroup))
            {
                return true;
            }
            mError = "";
            mHasFailed = false;
            StartLoadResources(theGroup);
            while (LoadNextResource())
            {
            }
            SexyAppBase.mUpdateAllowInputTimeStamp = true;
            if (!HadError())
            {
                mLoadedGroups.Add(theGroup);
                return true;
            }
            return false;
        }

        public void SetGroupAsLoaded(string theGroup)
        {
            mDeferredDeletes.Remove(theGroup);
            mLoadedGroups.Add(theGroup);
        }

        public int GetNumResources(string theGroup, Dictionary<string, BaseRes> theMap)
        {
            int count = 0;
            foreach (BaseRes res in theMap.Values)
            {
                if ((string.IsNullOrEmpty(theGroup) || string.Equals(res.mResGroup, theGroup, StringComparison.OrdinalIgnoreCase)) && !res.mFromProgram)
                {
                    count++;
                }
            }
            return count;
        }

        public int GetNumImages(string theGroup)
        {
            return GetNumResources(theGroup, mImageMap);
        }

        public int GetNumSounds(string theGroup)
        {
            return GetNumResources(theGroup, mSoundMap);
        }

        public int GetNumFonts(string theGroup)
        {
            return GetNumResources(theGroup, mFontMap);
        }

        public int GetNumAtlas(string theGroup)
        {
            return GetNumResources(theGroup, mAtlasMap);
        }

        public int GetNumAtlasEntry(string theGroup)
        {
            return GetNumResources(theGroup, mAtlasImageMap);
        }

        public int GetNumResources(string theGroup)
        {
            return GetNumImages(theGroup) + GetNumSounds(theGroup) + GetNumFonts(theGroup) + GetNumAtlas(theGroup);
        }

        #endregion

        #region Lookup

        private ResourceManagerException Throw(string theMessage)
        {
            Fail(theMessage);
            return new ResourceManagerException(GetErrorText());
        }

        public Image GetImage(string theId)
        {
            if (mImageMap.TryGetValue(theId, out BaseRes res))
            {
                return ((ImageRes)res).mImage;
            }
            return null;
        }

        /// <summary>Port addition: loads one image of a group (if it isn't loaded) and returns it, or null if it fails.</summary>
        public Image LoadImage(string theId)
        {
            if (!mImageMap.TryGetValue(theId, out BaseRes res))
            {
                return null;
            }
            ImageRes imageRes = (ImageRes)res;
            if (imageRes.mImage == null)
            {
                DoLoadImage(imageRes);
            }
            return imageRes.mImage;
        }

        public Image GetImageThrow(string theId)
        {
            if (mImageMap.TryGetValue(theId, out BaseRes res))
            {
                Image image = ((ImageRes)res).mImage;
                if (image != null || (mAllowMissingProgramResources && res.mFromProgram))
                {
                    return image;
                }
            }
            throw Throw("Image resource not found: " + theId);
        }

        public Image GetAtlasThrow(string theId)
        {
            if (mAtlasMap.TryGetValue(theId, out BaseRes res))
            {
                Image image = ((ImageRes)res).mImage;
                if (image != null || (mAllowMissingProgramResources && res.mFromProgram))
                {
                    return image;
                }
            }
            throw Throw("Atlas resource not found: " + theId);
        }

        /// <summary>Creates a new sub-image of the (already loaded) parent atlas.</summary>
        public Image GetAtlasImageThrow(string theId)
        {
            if (mAtlasImageMap.TryGetValue(theId, out BaseRes baseRes))
            {
                AtlasImageRes res = (AtlasImageRes)baseRes;
                Image parent = res.mParent?.mImage;
                if (parent == null)
                {
                    throw Throw("Atlas not loaded for image: " + theId);
                }
                Image image = new Image(parent, res.mX, res.mY, res.mWidth, res.mHeight);
                image.mNumRows = res.mRows;
                image.mNumCols = res.mCols;
                if (res.mAnimType != AnimType.AnimType_None)
                {
                    int numCels = Math.Max(res.mRows, res.mCols);
                    image.mAnimInfo = new AnimInfo();
                    image.mAnimInfo.mAnimType = res.mAnimType;
                    image.mAnimInfo.mFrameDelay = res.mFrameDelay;
                    image.mAnimInfo.mPerFrameDelay = new List<int>(res.mPerFrameDelay);
                    // Peggle copies the per-frame delays into the frame map as well (original behaviour).
                    image.mAnimInfo.mFrameMap = new List<int>(res.mPerFrameDelay);
                    image.mAnimInfo.Compute(numCels, res.mBeginDelay, res.mEndDelay);
                }
                return image;
            }
            throw Throw("Image resource not found: " + theId);
        }

        public int GetSoundThrow(string theId)
        {
            if (mSoundMap.TryGetValue(theId, out BaseRes res))
            {
                SoundRes soundRes = (SoundRes)res;
                if (soundRes.mSoundId != -1 || (mAllowMissingProgramResources && res.mFromProgram))
                {
                    return soundRes.mSoundId;
                }
            }
            throw Throw("Sound resource not found: " + theId);
        }

        public Font GetFont(string theId)
        {
            if (mFontMap.TryGetValue(theId, out BaseRes res))
            {
                return ((FontRes)res).mFont;
            }
            return null;
        }

        public Font GetFontThrow(string theId)
        {
            if (mFontMap.TryGetValue(theId, out BaseRes res))
            {
                FontRes fontRes = (FontRes)res;
                if (fontRes.mFont != null || (mAllowMissingProgramResources && res.mFromProgram))
                {
                    return fontRes.mFont;
                }
            }
            throw Throw("Font resource not found: " + theId);
        }

        public bool ReplaceImage(string theId, Image theImage)
        {
            if (mImageMap.TryGetValue(theId, out BaseRes res))
            {
                ImageRes imageRes = (ImageRes)res;
                imageRes.mImage?.Dispose();
                imageRes.mImage = theImage;
                return true;
            }
            return false;
        }

        public void DeleteImage(string theId)
        {
            ReplaceImage(theId, null);
        }

        #endregion
    }
}
