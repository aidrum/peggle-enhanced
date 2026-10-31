using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Base of everything placed on a Peggle board (Sexy::PhysObj): pegs, bricks, lines,
    /// polygons, balls, holes and emitters. Holds the shared level-file data (image, offsets,
    /// bounciness, movement, peg info) and the axis-aligned bounds used for broad-phase collision.
    /// </summary>
    public class PhysObj : EditValObj
    {
        public const int CLASS_PHYSOBJ = 1;

        public const int CLASS_LINE = 2;

        public const int CLASS_POLY = 3;

        public const int CLASS_BALL = 4;

        public const int CLASS_CIRCLE = 5;

        public const int CLASS_BRICK = 6;

        public const int CLASS_GUN = 7;

        public const int CLASS_HOLE = 8;

        public const int CLASS_EMITTER = 9;

        public const int SHAPE_HOLE = 1;

        public const int SHAPE_BALL = 2;

        public const int SHAPE_CIRCLE = 3;

        public const int SHAPE_LINE = 4;

        public const int SHAPE_POLY = 5;

        public const int SHAPE_EMITTER = 6;

        /// <summary>0 normal, 1 also outline bricks, 2 outlines only (editor debugging).</summary>
        public static int mOutlineMode;

        /// <summary>1 when reading a level file, 2 when syncing a save game (mSyncType).</summary>
        public static int mSyncType;

        public static bool mFastLoad;

        public static int mCurSortId;

        public static Action<Ball, PhysObj> mNotifyCollisionFunc;

        private static readonly string[] sLevelImageNames =
        {
            "bonusbumper", "bonushole", "bumpera", "bumperb", "bumperc", "catchera", "catcherb", "catcherc",
            "catchercover", "cobratreats", "leftbumper", "lobsterclawl", "lobsterclawr", "rightbumper"
        };

        private static readonly ResourceId[] sLevelImageIds =
        {
            ResourceId.IMAGE_LEVEL_BONUSBUMPER_ID, ResourceId.IMAGE_LEVEL_BONUSHOLE_ID, ResourceId.IMAGE_LEVEL_BUMPERA_ID,
            ResourceId.IMAGE_LEVEL_BUMPERB_ID, ResourceId.IMAGE_LEVEL_BUMPERC_ID, ResourceId.IMAGE_LEVEL_CATCHERA_ID,
            ResourceId.IMAGE_LEVEL_CATCHERB_ID, ResourceId.IMAGE_LEVEL_CATCHERC_ID, ResourceId.IMAGE_LEVEL_CATCHERCOVER_ID,
            ResourceId.IMAGE_LEVEL_COBRATREATS_ID, ResourceId.IMAGE_LEVEL_LEFTBUMPER_ID, ResourceId.IMAGE_LEVEL_LOBSTERCLAWL_ID,
            ResourceId.IMAGE_LEVEL_LOBSTERCLAWR_ID, ResourceId.IMAGE_LEVEL_RIGHTBUMPER_ID
        };

        public int mShapeType;                                    // 0x10

        public float mMinX;                                       // 0x14

        public float mMinY;                                       // 0x18

        public float mMaxX;                                       // 0x1c

        public float mMaxY;                                       // 0x20

        public bool mCollidable;                                  // 0x24

        public bool mVisible;                                     // 0x25

        public bool mMoving;                                      // 0x26

        public bool mBackground;                                  // 0x27

        public bool mBaseObject;                                  // 0x28

        public bool mFlag29;                                      // 0x29

        public bool mDrawSort;                                    // 0x2a

        public bool mBallStopReset;                               // 0x2b

        public bool mForeground;                                  // 0x2c

        public bool mDrawFloat;                                   // 0x2d

        public bool mUseFloat;                                    // 0x2e

        public bool mImageCached;                                 // 0x2f

        public bool mFlag30;                                      // 0x30

        public bool mShadow;                                      // 0x31

        public float mBouncy;                                     // 0x34

        public float mRolly;                                      // 0x38

        public float mMaxBounceVelocity;                          // 0x3c

        public uint mValue40;                                     // 0x40

        public uint mColor;                                       // 0x44

        public Mover mMover;                                      // 0x48

        public int mMoveUpdateCnt;                                // 0x4c

        public int mMoveCounter;                                  // 0x50

        public int mSound;                                        // 0x54

        public int mSortId;                                       // 0x58

        public string mId = "";                                   // 0x5c

        public string mLogic = "";                                // 0x60

        public string mImageName = "";                            // 0x64

        public int mSubId;                                        // 0x68

        public Image mImage;                                      // 0x6c

        public float mImageDX;                                    // 0x70

        public float mImageDY;                                    // 0x74

        public float mImageRotation;                              // 0x78

        public bool mOwnsImage;                                   // 0x7c

        public int mDisappearCounter;                             // 0x80

        public int mFlipperFlags;                                 // 0x84

        public PegInfo mPegInfo;                                  // 0x88

        public int mInt8c;                                        // 0x8c

        public int mInt90;                                        // 0x90

        public int mSaveValue;                                    // 0x94

        public bool mSaveFlag;                                    // 0x98

        public bool mFlag99;                                      // 0x99

        public PhysObj()
        {
            mShapeType = 0;
            mCollidable = true;
            mVisible = true;
            mMoving = true;
            mBackground = false;
            mDrawSort = false;
            mBallStopReset = false;
            mShadow = true;
            mImage = null;
            mOwnsImage = false;
            mInt8c = 0;
            mInt90 = 0;
            mSaveFlag = false;
            mSaveValue = 0;
            mFlag99 = false;
            mCurSortId++;
            mSortId = mCurSortId;
            mSubId = 0;
            mImageCached = false;
            mFlag30 = false;
            InitDefaults();
        }

        public static Board GetBoard()
        {
            return ((ThunderballApp)GlobalStaticVars.gSexyAppBase).mBoard;
        }

        public static ImageMgr GetImageMgr()
        {
            return ((ThunderballApp)GlobalStaticVars.gSexyAppBase).GetImageMgr();
        }

        /// <summary>Built-in level art named directly by the level file (GetLevelImage).</summary>
        public static Image GetLevelImage(string theName)
        {
            for (int i = 0; i < sLevelImageNames.Length; i++)
            {
                if (theName == sLevelImageNames[i])
                {
                    return Res.GetImageById(sLevelImageIds[i]);
                }
            }
            return null;
        }

        public static Action<int> mDebugFactoryHook;

        public static PhysObj ClassFactory(int theClass)
        {
            mDebugFactoryHook?.Invoke(theClass);
            switch (theClass)
            {
            case CLASS_LINE:
                return new Line();
            case CLASS_POLY:
                return new Poly();
            case CLASS_BALL:
                return new Ball(false);
            case CLASS_CIRCLE:
                return new Ball(true);
            case CLASS_BRICK:
                return new Brick();
            case CLASS_GUN:
                return new Gun(GetBoard());
            case CLASS_HOLE:
                return new Hole();
            case CLASS_EMITTER:
                return new Emitter();
            default:
                return new PhysObj();
            }
        }

        /// <summary>DataSync_SyncRefCountFactory&lt;PhysObj&gt;: shared, polymorphic object reference.</summary>
        public static PhysObj SyncPhysObj(DataSync theSync, PhysObj theObj)
        {
            return theSync.SyncRefCountFactory(theObj, o => o.GetClass(), ClassFactory);
        }

        public virtual void InitDefaults()
        {
            mId = "";
            mLogic = "";
            mBouncy = 0;
            mRolly = 0;
            mMaxBounceVelocity = 0;
            mMoveUpdateCnt = 0;
            mValue40 = 0;
            mDisappearCounter = 0;
            mMoveCounter = 0;
            mSound = 0;
            mFlipperFlags = 0;
            mColor = 0xffffff;
            mPegInfo = null;
            mMover = null;
            DeleteImage();
            mImage = null;
            mImageName = "";
            mImageDX = 0;
            mImageDY = 0;
            mImageRotation = 0;
            mOwnsImage = false;
            mBaseObject = false;
            mFlag29 = false;
            mForeground = false;
            mDrawFloat = false;
            mUseFloat = false;
            mShadow = true;
        }

        public override void SyncState(DataSync theSync)
        {
            bool reading = theSync.GetReader() != null;
            if (reading)
            {
                InitDefaults();
            }
            bool hasRolly = mRolly != 0.0f;
            bool hasBouncy = mBouncy != 0.0f;
            bool hasPegInfo = mPegInfo != null;
            bool hasMover = mMover != null;
            bool hasMoveUpdateCnt = mMoveUpdateCnt != 0;
            bool hasValue40 = mValue40 != 0;
            bool hasColor = mColor != 0xffffff;
            bool hasImage = mImageName.Length != 0;
            bool hasImageDX = mImageDX != 0.0f;
            bool hasImageDY = mImageDY != 0.0f;
            bool hasImageRotation = mImageRotation != 0.0f;
            bool hasDisappearCounter = mDisappearCounter != 0;
            bool hasId = mId.Length != 0;
            bool hasMoveCounter = mMoveCounter != 0;
            bool hasSound = mSound != 0;
            bool hasLogic = mLogic.Length != 0;
            bool hasMaxBounceVelocity = mMaxBounceVelocity != 0.0f;
            bool hasSubId = mSubId != 0;
            bool hasFlipperFlags = mFlipperFlags != 0;
            theSync.SyncBoolBit(ref hasRolly);
            theSync.SyncBoolBit(ref hasBouncy);
            theSync.SyncBoolBit(ref hasPegInfo);
            theSync.SyncBoolBit(ref hasMover);
            theSync.SyncBoolBit(ref hasMoveUpdateCnt);
            theSync.SyncBoolBit(ref mCollidable);
            theSync.SyncBoolBit(ref mVisible);
            theSync.SyncBoolBit(ref mMoving);
            theSync.EndBit();
            theSync.SyncBoolBit(ref hasValue40);
            theSync.SyncBoolBit(ref hasColor);
            theSync.SyncBoolBit(ref hasImage);
            theSync.SyncBoolBit(ref hasImageDX);
            theSync.SyncBoolBit(ref hasImageDY);
            theSync.SyncBoolBit(ref hasImageRotation);
            theSync.SyncBoolBit(ref mBackground);
            theSync.SyncBoolBit(ref mDrawSort);
            theSync.EndBit();
            theSync.SyncBoolBit(ref hasDisappearCounter);
            theSync.SyncBoolBit(ref hasId);
            theSync.SyncBoolBit(ref hasMoveCounter);
            theSync.SyncBoolBit(ref hasSound);
            theSync.SyncBoolBit(ref mBallStopReset);
            theSync.SyncBoolBit(ref hasLogic);
            theSync.SyncBoolBit(ref mBaseObject);
            theSync.SyncBoolBit(ref hasMaxBounceVelocity);
            theSync.EndBit();
            if (theSync.GetVersion() > 0xe)
            {
                theSync.SyncBoolBit(ref mForeground);
                theSync.SyncBoolBit(ref mFlag29);
                theSync.SyncBoolBit(ref hasSubId);
                theSync.SyncBoolBit(ref hasFlipperFlags);
                theSync.SyncBoolBit(ref mDrawFloat);
                theSync.SyncBoolBit(ref mFlag30);
                if ((reading && mSyncType == 1 && theSync.GetVersion() > 0x4f) || theSync.GetVersion() > 0x54)
                {
                    theSync.SyncBoolBit(ref mShadow);
                }
                theSync.EndBit();
            }
            if (hasRolly)
            {
                theSync.SyncFloat(ref mRolly);
            }
            if (hasBouncy)
            {
                theSync.SyncFloat(ref mBouncy);
            }
            if (hasMoveUpdateCnt)
            {
                theSync.SyncLong(ref mMoveUpdateCnt);
            }
            if (hasValue40)
            {
                theSync.SyncLong(ref mValue40);
            }
            if (hasColor)
            {
                theSync.SyncLong(ref mColor);
            }
            if (hasImage)
            {
                theSync.SyncString(ref mImageName);
            }
            if (hasImageDX)
            {
                theSync.SyncFloat(ref mImageDX);
            }
            if (hasImageDY)
            {
                theSync.SyncFloat(ref mImageDY);
            }
            if (hasImageRotation)
            {
                theSync.SyncFloat(ref mImageRotation);
            }
            if (hasDisappearCounter)
            {
                theSync.SyncLong(ref mDisappearCounter);
            }
            if (hasId)
            {
                theSync.SyncString(ref mId);
            }
            if (hasMoveCounter)
            {
                theSync.SyncLong(ref mMoveCounter);
            }
            if (hasSound)
            {
                theSync.SyncByte(ref mSound);
            }
            if (hasLogic)
            {
                theSync.SyncString(ref mLogic);
            }
            if (hasMaxBounceVelocity)
            {
                theSync.SyncFloat(ref mMaxBounceVelocity);
            }
            if (hasSubId)
            {
                theSync.SyncLong(ref mSubId);
            }
            if (hasFlipperFlags)
            {
                theSync.SyncByte(ref mFlipperFlags);
            }
            if (hasPegInfo)
            {
                if (reading)
                {
                    mPegInfo = new PegInfo();
                }
                mPegInfo.SyncState(theSync);
            }
            if (hasMover)
            {
                mMover = theSync.SyncRefCount(mMover, () => new Mover());
            }
            if (mSyncType == 2)
            {
                bool hasSaveValue = mSaveValue != 0;
                theSync.SyncBoolBit(ref mSaveFlag);
                theSync.SyncBoolBit(ref hasSaveValue);
                theSync.EndBit();
                if (hasSaveValue)
                {
                    theSync.SyncLong(ref mSaveValue);
                }
            }
            if (reading)
            {
                mUseFloat = mDrawFloat;
                if (mMover != null)
                {
                    int type = Math.Abs(mMover.mType);
                    if (type == Mover.MOVE_ROTATE || type == Mover.MOVE_ROTATE_BACK_AND_FORTH || type == Mover.MOVE_ROTATE_AROUND_CIRCLE)
                    {
                        mUseFloat = true;
                    }
                }
                if (!mFastLoad)
                {
                    LoadImage();
                }
            }
        }

        /// <summary>
        /// Resolves mImageName: built-in level art, then a bare name under images/&lt;res&gt;/emitters/,
        /// then a path's file name as an IMAGE_EMITTERS_* resource or a loose emitter image.
        /// </summary>
        public bool LoadImage()
        {
            if (mImageName.Length == 0)
            {
                return false;
            }
            DeleteImage();
            mImage = GetLevelImage(SexyUtil.GetFileName(mImageName, false));
            if (mImage == null)
            {
                ImageMgr imageMgr = GetImageMgr();
                string prefix = GlobalStaticVars.mResPathPrefix;
                if (mImageName.IndexOf('/') == -1 && mImageName.IndexOf('\\') == -1)
                {
                    mImage = imageMgr.GetImage("images/" + prefix + "emitters/" + mImageName, out mImageCached, true);
                }
                else
                {
                    string baseName = mImageName.Substring(mImageName.LastIndexOfAny(new[] { '/', '\\' }) + 1).ToUpperInvariant();
                    if (string.Equals(baseName, "12-3DRAGON", StringComparison.OrdinalIgnoreCase))
                    {
                        baseName = "12_3DRAGON";
                    }
                    ResourceId id = Res.GetIdByStringId("IMAGE_EMITTERS_" + baseName);
                    if (id == ResourceId.RESOURCE_ID_MAX)
                    {
                        mImage = imageMgr.GetImage(mImageName, out mImageCached, true);
                        if (mImage == null)
                        {
                            string fileName = mImageName.Substring(mImageName.LastIndexOfAny(new[] { '/', '\\' }) + 1);
                            mImage = imageMgr.GetImage("images/" + prefix + "emitters/" + fileName, out mImageCached, true);
                        }
                    }
                    else
                    {
                        mImage = Res.GetImageById(id);
                    }
                }
            }
            return mImage != null;
        }

        public virtual void DeleteImage()
        {
            mImage = null;
        }

        public virtual int GetClass()
        {
            return CLASS_PHYSOBJ;
        }

        public virtual uint GetTypeFlags()
        {
            uint flags = (mPegInfo == null || mPegInfo.mType == PegType.PEG_NONE) ? 0x1000u : 1u;
            flags |= (mShapeType == SHAPE_BALL || mShapeType == SHAPE_CIRCLE) ? 2u : 0x2000u;
            flags |= GetClass() == CLASS_BRICK ? 4u : 0x4000u;
            flags |= !mCollidable ? 8u : 0x8000u;
            flags |= !mDrawSort ? 0x10000u : 0x10u;
            if (!mFlag99)
            {
                flags |= 0x20000;
            }
            if (mPegInfo != null && !mPegInfo.mVariable)
            {
                flags |= 0x20;
            }
            if (mPegInfo != null && mPegInfo.mCrumble)
            {
                flags |= 0x40;
            }
            if (mSound != 0)
            {
                flags |= 0x80;
            }
            if (GetClass() == CLASS_EMITTER)
            {
                flags |= 0x40000;
            }
            return flags;
        }

        public bool IsMoving()
        {
            return mMoving;
        }

        public bool IsVisible()
        {
            return mVisible;
        }

        public bool IsCollidable()
        {
            return mCollidable;
        }

        public bool IsBackground()
        {
            return mBackground;
        }

        public bool CheckRectCollision(PhysObj theObj)
        {
            return mMinX <= theObj.mMaxX && theObj.mMinX <= mMaxX && mMinY <= theObj.mMaxY && theObj.mMinY <= mMaxY;
        }

        public bool CheckRectCollision(float theMinX, float theMinY, float theMaxX, float theMaxY)
        {
            return mMinX <= theMaxX && theMinX <= mMaxX && mMinY <= theMaxY && theMinY <= mMaxY;
        }

        public virtual void NotifyCollision(PhysObj theObj)
        {
        }

        public void SyncPosWithMover()
        {
            if (mMover != null)
            {
                mMover.CalcPos(mMoveUpdateCnt, 0.0f);
                SetPos(mMover.mX, mMover.mY);
            }
        }

        public void SetActive(bool active)
        {
            mCollidable = active;
            mVisible = active;
        }

        public virtual void SetActiveWithGrowAnim(bool active)
        {
            SetActive(active);
        }

        public virtual void AddedToGame()
        {
        }

        public virtual SexyVector2 GetCenter()
        {
            return new SexyVector2((mMinX + mMaxX) / 2.0f, (mMinY + mMaxY) / 2.0f);
        }

        public float GetOrigX()
        {
            return mMover == null ? GetXPos() : mMover.mAnchorX;
        }

        public float GetOrigY()
        {
            return mMover == null ? GetYPos() : mMover.mAnchorY;
        }

        /// <summary>Where the object will be <paramref name="theFrames"/> updates from now.</summary>
        public float GetFutureX(int theFrames)
        {
            if (!mMoving || mMover == null)
            {
                return GetXPos();
            }
            float x = mMover.mX;
            float y = mMover.mY;
            mMover.CalcPos(mMoveUpdateCnt + theFrames, 0.0f);
            float futureX = mMover.mX;
            mMover.mX = x;
            mMover.mY = y;
            return futureX;
        }

        public void SetIsPeg(bool isPeg)
        {
            if (isPeg && mPegInfo == null)
            {
                mPegInfo = new PegInfo();
                PegChangedHook(true);
            }
            else if (!isPeg && mPegInfo != null)
            {
                mPegInfo = null;
                PegChangedHook(false);
            }
        }

        public virtual void SetPos(float theX, float theY)
        {
        }

        public virtual void Translate(float theDX, float theDY)
        {
            mMinX += theDX;
            mMaxX += theDX;
            mMinY += theDY;
            mMaxY += theDY;
        }

        public virtual void SetVelocity(float theVX, float theVY)
        {
        }

        public virtual void PegChangedHook(bool isPeg)
        {
        }

        public void SetPegType(int thePegType)
        {
            if (mPegInfo != null)
            {
                mPegInfo.mType = thePegType;
                PegChangedHook(true);
            }
        }

        public void SetMoveUpdateCnt(int theUpdateCnt)
        {
            mMoveUpdateCnt = theUpdateCnt;
            SyncPosWithMover();
        }

        /// <summary>
        /// Moves the object to the point where its path reaches <paramref name="thePercent"/>,
        /// either instantly or by letting it travel there (|percent| &lt; 0 wraps a full cycle).
        /// </summary>
        public void SetMovingPercent(float thePercent, bool instant)
        {
            if (mMover == null)
            {
                return;
            }
            bool negative = thePercent < 0.0f;
            if (negative)
            {
                thePercent *= -1.0f;
            }
            int frames = mMover.GetTimeTillPhase(mMoveUpdateCnt, thePercent);
            if (frames < 1 && negative)
            {
                frames = mMover.mPause1 + mMover.mPause2 + mMover.mPeriod + frames;
            }
            if (frames > 0)
            {
                if (!instant)
                {
                    mMoving = true;
                    mMoveCounter = frames;
                }
                else
                {
                    SetMoveUpdateCnt(mMoveUpdateCnt + frames);
                }
            }
        }

        public virtual void Update()
        {
            if (mDisappearCounter != 0)
            {
                mDisappearCounter--;
                if (mDisappearCounter == 0)
                {
                    SetActive(false);
                }
            }
            mPegInfo?.Update(this);
            if (mMover != null)
            {
                float oldX = mMover.mX;
                float oldY = mMover.mY;
                if (!IsMoving())
                {
                    mMover.CalcPos(mMoveUpdateCnt, 0.0f);
                }
                else
                {
                    mMoveUpdateCnt++;
                    mMover.CalcPos(mMoveUpdateCnt, 0.0f);
                }
                float newX = mMover.mX;
                float newY = mMover.mY;
                SetPos(mMover.mX, mMover.mY);
                SetVelocity(newX - oldX, newY - oldY);
            }
            if (mMoveCounter != 0)
            {
                mMoveCounter--;
                if (mMoveCounter == 0)
                {
                    mMoving = false;
                }
            }
        }

        /// <summary>Positions a moving object between frames while the game runs in slow motion.</summary>
        public virtual void UpdateSlowMo(float theFraction)
        {
            if (mMover != null && IsMoving())
            {
                float x = mMover.mX;
                float y = mMover.mY;
                mMover.CalcPos(mMoveUpdateCnt - 1, theFraction);
                SetPos(mMover.mX, mMover.mY);
                mMover.mX = x;
                mMover.mY = y;
            }
        }

        public virtual void Draw(Graphics g)
        {
        }

        public virtual void DrawShadow(Graphics g)
        {
        }

        public virtual void DrawLight(Graphics g)
        {
            mPegInfo?.DrawLight(this, g);
        }

        /// <summary>
        /// Draws <paramref name="theImage"/> centred on (theX, theY) in 600-high board units plus the
        /// object's image offset, rotated by theRot + mImageRotation or mirrored.
        /// </summary>
        public void DrawImage(Graphics g, Image theImage, float theX, float theY, float theRot, bool useFloat, bool mirror)
        {
            float scale = SexyUtil.gPhysicalDisplayHeight;
#if PEGGLE_IPHONE
            // The source rect drops the last row and column (no edge bleed).
            TRect src = new TRect(0, 0, theImage.mWidth - 1, theImage.mHeight - 1);
#else
            TRect src = new TRect(0, 0, theImage.mWidth, theImage.mHeight);
#endif
            float w2 = src.mWidth / 2.0f;
            float h2 = src.mHeight / 2.0f;
            float x = (float)(((theX + mImageDX) * scale) / 600.0 - w2);
            float y = (float)(((theY + mImageDY) * scale) / 600.0 - h2);
            if (mirror)
            {
                if (useFloat && g.Is3D())
                {
                    Transform transform = new Transform();
                    transform.Scale(-1.0f, 1.0f);
                    g.DrawImageTransformF(theImage, transform, x + w2, y + h2);
                }
                else
                {
                    g.DrawImageMirror(theImage, (int)x, (int)y, true);
                }
                return;
            }
            theRot += mImageRotation;
            if (theRot == 0.0f)
            {
                if (useFloat)
                {
                    g.DrawImageF(theImage, x, y, src);
                }
                else
                {
                    g.DrawImage(theImage, (int)x, (int)y);
                }
                return;
            }
            float rcx = (float)(w2 - (mImageDX * scale) / 600.0);
            float rcy = (float)(h2 - (mImageDY * scale) / 600.0);
            g.DrawImageRotatedF(theImage, x, y, theRot, rcx, rcy, src);
        }

        /// <summary>
        /// Draws mImage partly revealed: <paramref name="theClipType"/> (low byte) picks the edge
        /// the visible part grows from and bit 0x100 inverts <paramref name="thePct"/>.
        /// </summary>
        public void DrawImage(Graphics g, float theX, float theY, float theRot, bool useFloat, int theClipType, float thePct)
        {
            Image image = mImage;
            float scale = SexyUtil.gPhysicalDisplayHeight;
            float w2 = image.mWidth / 2.0f;
            float h2 = image.mHeight / 2.0f;
            float x = (float)(((theX + mImageDX) * scale) / 600.0 - w2);
            float y = (float)(((theY + mImageDY) * scale) / 600.0 - h2);
            float pct = Math.Clamp(thePct, 0.0f, 1.0f);
            if ((theClipType & 0x100) == 0)
            {
                pct = 1.0f - pct;
            }
            theClipType &= 0xff;
            int width = (int)(image.mWidth * pct);
            TRect src = new TRect(0, 0, width, image.mHeight);
            switch (theClipType)
            {
            case 2:
                src.mX = image.mWidth - width;
                x += src.mX;
                break;
            case 3:
                src.mX = image.mWidth - width;
                break;
            case 4:
                x += image.mWidth - width;
                break;
            case 5:
                src.mX = (image.mWidth - width) / 2;
                x += src.mX;
                break;
            case 6:
            {
                int height = (int)(image.mHeight * pct);
                src.mHeight = height;
                src.mWidth = image.mWidth;
                src.mY = image.mHeight - height;
                y += src.mY;
                break;
            }
            case 7:
            {
                int height = (int)(image.mHeight * pct);
                src.mHeight = height;
                src.mWidth = image.mWidth;
                y += image.mHeight - height;
                break;
            }
            case 8:
                src.mWidth = image.mWidth;
                break;
            }
            theRot += mImageRotation;
            if (theRot == 0.0f)
            {
                if (useFloat)
                {
                    g.DrawImageF(image, x, y, src);
                }
                else
                {
                    g.DrawImage(image, (int)x, (int)y, src);
                }
                return;
            }
            float rcx = (float)(w2 - (mImageDX * scale) / 600.0);
            float rcy = (float)(h2 - (mImageDY * scale) / 600.0);
            g.DrawImageRotatedF(image, x, y, theRot, rcx, rcy, src);
        }

        public void DrawImage(Graphics g, float theX, float theY, float theRot, bool useFloat, bool mirror)
        {
            DrawImage(g, mImage, theX, theY, theRot, useFloat, mirror);
        }

        public virtual float GetXPos()
        {
            return (mMinX + mMaxX) / 2.0f;
        }

        public virtual float GetYPos()
        {
            return (mMinY + mMaxY) / 2.0f;
        }

        public virtual bool IsCollidableWith(PhysObj theObj)
        {
            return theObj.mCollidable;
        }
    }
}
