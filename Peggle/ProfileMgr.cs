using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// The profile roster (Sexy::ProfileMgr): a case-insensitive name → PlayerInfo map whose
    /// summaries are saved to "userdata/users.dat". Profile details load on demand.
    /// </summary>
    public class ProfileMgr
    {
        private const int gProfileVersion = 1;

        private const int MAX_PROFILES = 200;

        private readonly SortedDictionary<string, PlayerInfo> mProfileMap = new SortedDictionary<string, PlayerInfo>(StringComparer.OrdinalIgnoreCase); // 0x04

        private int mNextProfileId;                               // 0x1c

        private int mNextProfileUseSeq;                           // 0x20

        public ProfileMgr()
        {
            Clear();
        }

        public SortedDictionary<string, PlayerInfo> GetProfileMap()
        {
            return mProfileMap;
        }

        public int GetNumProfiles()
        {
            return mProfileMap.Count;
        }

        public void Clear()
        {
            mProfileMap.Clear();
            mNextProfileId = 1;
            mNextProfileUseSeq = 1;
        }

        public PlayerInfo GetAnyProfile()
        {
            if (mProfileMap.Count == 0)
            {
                return null;
            }
            PlayerInfo p = null;
            foreach (PlayerInfo info in mProfileMap.Values)
            {
                p = info;
                break;
            }
            p.LoadDetails();
            p.mUseOrder = mNextProfileUseSeq++;
            return p;
        }

        public void SyncState(DataSync theSync)
        {
            int version = gProfileVersion;
            theSync.SyncLong(ref version);
            theSync.SetVersion(version);
            if (version > gProfileVersion)
            {
                return;
            }
            if (!theSync.IsReading)
            {
                theSync.GetWriter().WriteShort((short)mProfileMap.Count);
                foreach (PlayerInfo info in mProfileMap.Values)
                {
                    info.SyncSummary(theSync);
                }
                return;
            }
            mProfileMap.Clear();
            int count = (ushort)theSync.GetReader().ReadShort();
            int maxUse = 0;
            int maxId = 0;
            for (int i = 0; i < count; i++)
            {
                PlayerInfo info = new PlayerInfo();
                info.SyncSummary(theSync);
                if (maxUse < info.mUseOrder)
                {
                    maxUse = info.mUseOrder;
                }
                if (maxId < info.mId)
                {
                    maxId = info.mId;
                }
                mProfileMap[info.mName] = info;
            }
            mNextProfileId = maxId + 1;
            mNextProfileUseSeq = maxUse + 1;
        }

        private static string UsersFileName => SexyUtil.GetDocumentsDir() + "userdata/users.dat";

        public void Load()
        {
            Buffer buffer = new Buffer();
            if (!GlobalStaticVars.gSexyAppBase.ReadBufferFromFile(UsersFileName, ref buffer, false))
            {
                return;
            }
            try
            {
                DataReader reader = new DataReader();
                reader.OpenMemory(buffer.GetDataPtr(), buffer.GetDataLen(), false);
                SyncState(new DataSync(reader));
            }
            catch (DataReaderException)
            {
                Clear();
            }
        }

        public void Save()
        {
            DataWriter writer = new DataWriter();
            writer.OpenMemory(0x20);
            SyncState(new DataSync(writer));
            Common.MkDir(SexyUtil.GetDocumentsDir() + "userdata");
            GlobalStaticVars.gSexyAppBase.WriteBytesToFile(UsersFileName, writer.GetDataPtr(), writer.GetDataLen());
        }

        public bool DeleteProfile(string theName)
        {
            if (!mProfileMap.TryGetValue(theName, out PlayerInfo info))
            {
                return false;
            }
            info.DeleteUserFiles();
            mProfileMap.Remove(theName);
            return true;
        }

        public bool RenameProfile(string theOldName, string theNewName)
        {
            if (!mProfileMap.TryGetValue(theOldName, out PlayerInfo info))
            {
                return false;
            }
            if (string.Equals(theOldName, theNewName, StringComparison.OrdinalIgnoreCase))
            {
                info.mName = theNewName;
                return true;
            }
            if (mProfileMap.ContainsKey(theNewName))
            {
                return false;
            }
            mProfileMap.Remove(theOldName);
            mProfileMap[theNewName] = info;
            info.mName = theNewName;
            return true;
        }

        public void DeleteOldestProfile()
        {
            if (mProfileMap.Count == 0)
            {
                return;
            }
            PlayerInfo oldest = null;
            foreach (PlayerInfo info in mProfileMap.Values)
            {
                if (oldest == null || info.mUseOrder < oldest.mUseOrder)
                {
                    oldest = info;
                }
            }
            oldest.DeleteUserFiles();
            mProfileMap.Remove(oldest.mName);
        }

        public void DeleteOldProfiles()
        {
            while (mProfileMap.Count > MAX_PROFILES)
            {
                DeleteOldestProfile();
            }
        }

        public PlayerInfo GetProfile(string theName)
        {
            if (!mProfileMap.TryGetValue(theName, out PlayerInfo info))
            {
                return null;
            }
            info.LoadDetails();
            info.mUseOrder = mNextProfileUseSeq++;
            return info;
        }

        public PlayerInfo AddProfile(string theName)
        {
            if (mProfileMap.ContainsKey(theName))
            {
                return null;
            }
            PlayerInfo info = new PlayerInfo();
            info.mName = theName;
            info.mId = mNextProfileId++;
            info.mUseOrder = mNextProfileUseSeq++;
            mProfileMap[theName] = info;
            DeleteOldProfiles();
            return info;
        }
    }
}
