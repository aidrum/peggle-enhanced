using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// The board's sound queue (Sexy::SoundMgr): plays samples now or after a delay, drops repeats
    /// of the same sample within a minimum interval, and fades out looping sounds it is handed.
    /// </summary>
    public class SoundMgr
    {
        public class SoundDesc : ISyncable
        {
            public int mInterval;                                 // 0x00

            public int mSound;                                    // 0x04

            public int mPan;                                      // 0x08

            public float mPitch;                                  // 0x0c

            public float mVolume;                                 // 0x10

            public void SyncState(DataSync theSync)
            {
                theSync.SyncLong(ref mInterval);
                theSync.SyncLong(ref mSound);
                theSync.SyncLong(ref mPan);
                theSync.SyncFloat(ref mPitch);
                if (theSync.GetVersion() > 0x30)
                {
                    theSync.SyncFloat(ref mVolume);
                }
            }
        }

        private class SoundDescComparer : IComparer<SoundDesc>
        {
            public static readonly SoundDescComparer Instance = new SoundDescComparer();

            public int Compare(SoundDesc a, SoundDesc b)
            {
                if (a.mSound != b.mSound)
                {
                    return a.mSound < b.mSound ? -1 : 1;
                }
                if (a.mPitch != b.mPitch)
                {
                    return a.mPitch < b.mPitch ? -1 : 1;
                }
                if (a.mPan != b.mPan)
                {
                    return a.mPan < b.mPan ? -1 : 1;
                }
                return 0;
            }
        }

        /// <summary>Delayed sounds keyed by the update count they play at (a multimap; kept sorted, stable).</summary>
        public List<KeyValuePair<int, SoundDesc>> mSoundMap = new List<KeyValuePair<int, SoundDesc>>(); // 0x04

        public int mUpdateCnt;                                    // 0x1c

        public float mMusicVolume;                                // 0x20

        public float mMusicFadeDelta;                             // 0x24

        public bool mMusicPaused;                                 // 0x28

        public int mMusicFadeDelay;                               // 0x2c

        /// <summary>The app update count each sample was last played at.</summary>
        public SortedDictionary<SoundDesc, int> mLastPlayed = new SortedDictionary<SoundDesc, int>(SoundDescComparer.Instance); // 0x30

        public List<SoundInstance> mFadeSounds = new List<SoundInstance>(); // 0x48

        public SoundMgr()
        {
            Clear();
            mMusicPaused = false;
            mMusicVolume = 1.0f;
            mMusicFadeDelta = 0.0f;
        }

        public void Clear()
        {
            mSoundMap.Clear();
            mLastPlayed.Clear();
            mUpdateCnt = 0;
            KillAllSounds();
        }

        public void SyncState(DataSync theSync)
        {
            theSync.SyncList(mSoundMap, (ds, kv) =>
            {
                int aKey = kv.Key;
                ds.SyncLong(ref aKey);
                kv.Value.SyncState(ds);
                return new KeyValuePair<int, SoundDesc>(aKey, kv.Value);
            }, () => new KeyValuePair<int, SoundDesc>(0, new SoundDesc()));
            if (theSync.GetVersion() > 0x2f)
            {
                theSync.SyncLong(ref mUpdateCnt);
            }
            if (theSync.GetReader() != null)
            {
                mLastPlayed.Clear();
            }
        }

        public void PauseMusic(bool pause)
        {
            if (mMusicPaused != pause)
            {
                mMusicPaused = pause;
                if (pause)
                {
                    GlobalStaticVars.gSexyAppBase.mMusicInterface.PauseMusic();
                }
                else
                {
                    GlobalStaticVars.gSexyAppBase.mMusicInterface.ResumeMusic();
                }
            }
        }

        public void FadeMusic(int theDelay)
        {
            SexyAppBase anApp = GlobalStaticVars.gSexyAppBase;
            if (anApp.mMusicVolume > 0.0 && anApp.mSfxVolume > 0.0)
            {
                if (mMusicFadeDelay < theDelay)
                {
                    mMusicFadeDelay = theDelay;
                }
                mMusicFadeDelta = -0.03f;
            }
        }

        public void CancelFade()
        {
            mMusicFadeDelay = 0;
            mMusicFadeDelta = 0.0f;
            mMusicVolume = 1.0f;
        }

        public void PlaySample(SoundDesc theDesc)
        {
            SexyAppBase anApp = GlobalStaticVars.gSexyAppBase;
            mLastPlayed.TryGetValue(theDesc, out int aLast);
            if (aLast != 0 && anApp.mUpdateCount - aLast < theDesc.mInterval)
            {
                return;
            }
            mLastPlayed[theDesc] = anApp.mUpdateCount;
            if (theDesc.mSound < 0)
            {
                return;
            }
            SoundInstance anInst = anApp.mSoundManager.GetSoundInstance((uint)theDesc.mSound);
            if (anInst != null)
            {
                if (theDesc.mPitch != 0.0f)
                {
                    anInst.AdjustPitch(theDesc.mPitch);
                }
                if (theDesc.mPan != 0)
                {
                    anInst.SetPan(theDesc.mPan);
                }
                if (theDesc.mVolume >= 0.0f)
                {
                    anInst.SetVolume(theDesc.mVolume);
                }
                anInst.Play(false, true);
            }
        }

        /// <summary>Plays theSound (after theDelay updates, if non-zero) unless it played within theInterval updates.</summary>
        public void AddSound(int theSound, float thePitch, int thePan, int theDelay, int theInterval, float theVolume)
        {
            if (theVolume == 0.0f)
            {
                return;
            }
            SoundDesc aDesc = new SoundDesc();
            aDesc.mInterval = theInterval;
            aDesc.mSound = theSound;
            aDesc.mPan = thePan;
            aDesc.mPitch = thePitch;
            aDesc.mVolume = theVolume;
            if (theDelay == 0)
            {
                PlaySample(aDesc);
            }
            else
            {
                int aKey = mUpdateCnt + theDelay;
                int anIdx = mSoundMap.Count;
                while (anIdx > 0 && mSoundMap[anIdx - 1].Key > aKey)
                {
                    anIdx--;
                }
                mSoundMap.Insert(anIdx, new KeyValuePair<int, SoundDesc>(aKey, aDesc));
            }
        }

        public void AddSound(int theSound, int theInterval = 0)
        {
            AddSound(theSound, 0.0f, 0, 0, theInterval, -1.0f);
        }

        public void AddSoundPitch(int theSound, float thePitch, int theInterval = 0)
        {
            AddSound(theSound, thePitch, 0, 0, theInterval, -1.0f);
        }

        public void AddSoundDelay(int theSound, int theDelay, int theInterval = 0)
        {
            AddSound(theSound, 0.0f, 0, theDelay, theInterval, -1.0f);
        }

        public void AddSoundPitchDelay(int theSound, float thePitch, int theDelay, int theInterval = 0)
        {
            AddSound(theSound, thePitch, 0, theDelay, theInterval, -1.0f);
        }

        /// <summary>Takes ownership of a (looping) sound and fades it out.</summary>
        public void AddFadeSound(SoundInstance theSound)
        {
            mFadeSounds.Add(theSound);
        }

        public void Update()
        {
            mUpdateCnt++;
            for (int i = 0; i < mSoundMap.Count;)
            {
                if (mUpdateCnt < mSoundMap[i].Key)
                {
                    i++;
                }
                else
                {
                    SoundDesc aDesc = mSoundMap[i].Value;
                    PlaySample(aDesc);
                    mSoundMap.RemoveAt(i);
                }
            }
            UpdateSounds();
        }

        public void UpdateSounds()
        {
            if (mMusicFadeDelay > 0 && --mMusicFadeDelay == 0)
            {
                mMusicFadeDelta = 0.01f;
            }
            if (mMusicFadeDelta != 0.0f)
            {
                mMusicVolume += mMusicFadeDelta;
                if (mMusicVolume < 0.4f)
                {
                    mMusicVolume = 0.4f;
                }
                else if (mMusicVolume > 1.0f)
                {
                    mMusicVolume = 1.0f;
                }
                // The original applies no volume change here on iOS.
            }

            for (int i = 0; i < mFadeSounds.Count;)
            {
                SoundInstance anInst = mFadeSounds[i];
                if (anInst == null)
                {
                    i++;
                    continue;
                }
                if (anInst.IsPlaying())
                {
                    double aVolume = anInst.GetVolume();
                    if ((float)(aVolume - 0.05000000074505806) > 0.0f)
                    {
                        anInst.SetVolume((float)(aVolume - 0.05000000074505806));
                        i++;
                        continue;
                    }
                }
                anInst.Release();
                mFadeSounds.RemoveAt(i);
            }
        }

        public void KillAllSounds()
        {
            mSoundMap.Clear();
            foreach (SoundInstance anInst in mFadeSounds)
            {
                anInst.Release();
            }
            mFadeSounds.Clear();
        }
    }
}
