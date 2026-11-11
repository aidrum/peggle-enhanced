using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>An object that serialises itself in both directions through a DataSync.</summary>
    public interface ISyncable
    {
        void SyncState(DataSync theSync);
    }

    /// <summary>
    /// Bidirectional serialiser (Sexy::DataSync from PopCap's AceLib). With a reader the Sync*
    /// calls fill the referenced values; with a writer they write them, so one SyncState method
    /// serves both loading and saving.
    ///
    /// Shared objects (RefCount) are written once: the first reference writes 1 followed by the
    /// object's state, later references write its id (2, 3, ...); 0 is null.
    /// </summary>
    public class DataSync
    {
        private DataReader mReader;                               // 0x04

        private DataWriter mWriter;                               // 0x08

        private readonly DataReader mOwnReader = new DataReader(); // 0x0c

        private readonly DataWriter mOwnWriter = new DataWriter(); // 0x2c

        private int mVersion;

        private bool mCanSyncPointers;                            // 0x4c

        private readonly Dictionary<object, int> mRefCountToId = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);

        private readonly Dictionary<int, object> mIdToRefCount = new Dictionary<int, object>();

        public DataSync()
        {
        }

        public DataSync(DataReader theReader)
        {
            mReader = theReader;
            ResetPointerTable();
        }

        public DataSync(DataWriter theWriter)
        {
            mWriter = theWriter;
            ResetPointerTable();
        }

        public DataReader GetReader()
        {
            return mReader;
        }

        public DataWriter GetWriter()
        {
            return mWriter;
        }

        public bool IsReading => mReader != null;

        public int GetVersion()
        {
            return mVersion;
        }

        public void SetVersion(int theVersion)
        {
            mVersion = theVersion;
        }

        /// <summary>
        /// Port addition: the stream was written by a PC game (a PC replay; Board.mReplayFromPC). The iOS editions
        /// moved some version gates and dropped some fields after branching from PC Peggle Nights (version 0x52),
        /// so the classes read those the PC way (PCVersionAbove).
        /// </summary>
        public bool mPCFormat;

        /// <summary>Port addition: the PC games' gate (version > theVersion) when reading a PC stream, else false.</summary>
        public bool PCVersionAbove(int theVersion)
        {
            return mPCFormat && mVersion > theVersion;
        }

        public bool GetCanSyncPointers()
        {
            return mCanSyncPointers;
        }

        public void SetCanSyncPointers(bool theCanSync)
        {
            mCanSyncPointers = theCanSync;
        }

        public void ResetPointerTable()
        {
            mRefCountToId.Clear();
            mIdToRefCount.Clear();
        }

        public DataReader StartReadMemory(byte[] theData, int theLength, bool takeOwnership)
        {
            mReader = null;
            mWriter = null;
            mOwnReader.Close();
            mOwnWriter.Close();
            mCanSyncPointers = true;
            ResetPointerTable();
            mReader = mOwnReader;
            mOwnReader.OpenMemory(theData, theLength, takeOwnership);
            return mOwnReader;
        }

        public DataWriter StartWriteMemory(int theReserve = 0x20)
        {
            mReader = null;
            mWriter = null;
            mOwnReader.Close();
            mOwnWriter.Close();
            mCanSyncPointers = true;
            ResetPointerTable();
            mWriter = mOwnWriter;
            mOwnWriter.OpenMemory(theReserve);
            return mOwnWriter;
        }

        public void SyncBytes(byte[] theData, int theCount)
        {
            if (mReader != null)
            {
                mReader.ReadBytes(theData, 0, theCount);
            }
            else
            {
                mWriter.WriteBytes(theData, 0, theCount);
            }
        }

        public void SyncLong(ref int theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadLong();
            }
            else
            {
                mWriter.WriteLong(theValue);
            }
        }

        public void SyncLong(ref uint theValue)
        {
            if (mReader != null)
            {
                theValue = (uint)mReader.ReadLong();
            }
            else
            {
                mWriter.WriteLong((int)theValue);
            }
        }

        public int SyncLong(int theValue)
        {
            SyncLong(ref theValue);
            return theValue;
        }

        public void SyncShort(ref int theValue)
        {
            if (mReader != null)
            {
                theValue = (ushort)mReader.ReadShort();
            }
            else
            {
                mWriter.WriteShort((short)theValue);
            }
        }

        public void SyncSShort(ref int theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadShort();
            }
            else
            {
                mWriter.WriteShort((short)theValue);
            }
        }

        public void SyncByte(ref int theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadByte();
            }
            else
            {
                mWriter.WriteByte((byte)theValue);
            }
        }

        public void SyncSByte(ref int theValue)
        {
            if (mReader != null)
            {
                theValue = (sbyte)mReader.ReadByte();
            }
            else
            {
                mWriter.WriteByte((byte)theValue);
            }
        }

        public void EndBit()
        {
            if (mReader != null)
            {
                mReader.EndBit();
            }
            else
            {
                mWriter.EndBit();
            }
        }

        public void SyncBool(ref bool theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadBool();
            }
            else
            {
                mWriter.WriteBool(theValue);
            }
        }

        public void SyncBoolBit(ref bool theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadBit();
            }
            else
            {
                mWriter.WriteBit(theValue);
            }
        }

        public void SyncFloat(ref float theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadFloat();
            }
            else
            {
                mWriter.WriteFloat(theValue);
            }
        }

        public void SyncDouble(ref double theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadDouble();
            }
            else
            {
                mWriter.WriteDouble(theValue);
            }
        }

        /// <summary>std::string</summary>
        public void SyncString(ref string theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadString();
            }
            else
            {
                mWriter.WriteString(theValue);
            }
        }

        /// <summary>std::wstring</summary>
        public void SyncWString(ref string theValue)
        {
            if (mReader != null)
            {
                theValue = mReader.ReadWString();
            }
            else
            {
                mWriter.WriteWString(theValue);
            }
        }

        /// <summary>Registers a shared object; returns 1 if newly added, else its existing id.</summary>
        public int AddRefCount(object theObject)
        {
            if (theObject == null)
            {
                return 0;
            }
            if (mRefCountToId.TryGetValue(theObject, out int id))
            {
                return id;
            }
            // The original takes the map size after inserting, so the first object is id 3.
            id = mIdToRefCount.Count + 3;
            mRefCountToId[theObject] = id;
            mIdToRefCount[id] = theObject;
            return 1;
        }

        public object GetRefCount(int theId)
        {
            if (mIdToRefCount.TryGetValue(theId, out object obj))
            {
                return obj;
            }
            throw new DataReaderException();
        }

        /// <summary>DataSync_SyncRefCount&lt;T&gt;: syncs a shared object reference.</summary>
        public T SyncRefCount<T>(T theObject, Func<T> theFactory) where T : class, ISyncable
        {
            if (mReader == null)
            {
                int id = AddRefCount(theObject);
                mWriter.WriteLong(id);
                if (id == 1)
                {
                    theObject.SyncState(this);
                }
                return theObject;
            }
            int readId = mReader.ReadLong();
            if (readId == 0)
            {
                return null;
            }
            if (readId == 1)
            {
                T obj = theFactory();
                AddRefCount(obj);
                obj.SyncState(this);
                return obj;
            }
            return (T)GetRefCount(readId);
        }

        /// <summary>
        /// DataSync_SyncRefCountFactory&lt;T&gt;: like SyncRefCount but polymorphic; the class id
        /// returned by <paramref name="theGetClass"/> is written so the reader can construct
        /// the right subclass.
        /// </summary>
        public T SyncRefCountFactory<T>(T theObject, Func<T, int> theGetClass, Func<int, T> theFactory) where T : class, ISyncable
        {
            if (mReader == null)
            {
                int id = AddRefCount(theObject);
                mWriter.WriteLong(id);
                if (id == 1)
                {
                    mWriter.WriteLong(theGetClass(theObject));
                    theObject.SyncState(this);
                }
                return theObject;
            }
            int readId = mReader.ReadLong();
            if (readId == 0)
            {
                return null;
            }
            if (readId == 1)
            {
                int cls = mReader.ReadLong();
                T obj = theFactory(cls);
                AddRefCount(obj);
                obj.SyncState(this);
                return obj;
            }
            return (T)GetRefCount(readId);
        }

        public void SyncIntList(List<int> theList)
        {
            SyncList(theList, (ds, v) => ds.SyncLong(v), () => 0);
        }

        public void SyncStringList(List<string> theList)
        {
            SyncList(theList, (ds, v) => { ds.SyncString(ref v); return v; }, () => "");
        }

        /// <summary>std::set / std::multiset: count then elements.</summary>
        public void SyncSet<T>(ICollection<T> theSet, Func<DataSync, T, T> theSyncElement, Func<T> theNew)
        {
            int count = SyncCount(theSet.Count);
            if (mReader != null)
            {
                theSet.Clear();
                for (int i = 0; i < count; i++)
                {
                    theSet.Add(theSyncElement(this, theNew()));
                }
            }
            else
            {
                foreach (T item in theSet)
                {
                    theSyncElement(this, item);
                }
            }
        }

        /// <summary>std::map: count then (key, value) pairs.</summary>
        public void SyncMap<K, V>(IDictionary<K, V> theMap, Func<DataSync, K, K> theSyncKey, Func<DataSync, V, V> theSyncValue, Func<K> theNewKey, Func<V> theNewValue)
        {
            int count = SyncCount(theMap.Count);
            if (mReader != null)
            {
                theMap.Clear();
                for (int i = 0; i < count; i++)
                {
                    K key = theSyncKey(this, theNewKey());
                    V value = theSyncValue(this, theNewValue());
                    theMap[key] = value;
                }
            }
            else
            {
                foreach (KeyValuePair<K, V> kv in theMap)
                {
                    theSyncKey(this, kv.Key);
                    theSyncValue(this, kv.Value);
                }
            }
        }

        public string SyncString(string theValue)
        {
            SyncString(ref theValue);
            return theValue;
        }

        public void SyncPointers()
        {
            ResetPointerTable();
        }

        private int SyncCount(int theCount)
        {
            SyncLong(ref theCount);
            if (mReader != null && (theCount > 100000 || theCount < 0))
            {
                throw new DataReaderException();
            }
            return theCount;
        }

        /// <summary>DataSync_SyncSTLList/Vector with a per-element sync function.</summary>
        public void SyncList<T>(List<T> theList, Func<DataSync, T, T> theSyncElement, Func<T> theNew)
        {
            int count = SyncCount(theList.Count);
            if (mReader != null)
            {
                theList.Clear();
                for (int i = 0; i < count; i++)
                {
                    theList.Add(theSyncElement(this, theNew()));
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    theSyncElement(this, theList[i]);
                }
            }
        }
    }
}
