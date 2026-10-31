using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Port of Peggle Classic HD's Sexy::CoreAudioMusicInterface on top of a MusicPlayer
    /// (MonoGame's MediaPlayer unless the platform sets PlatformPlayer). One track plays at a
    /// time; switching tracks fades the current one out and fades the pending one in once it has
    /// stopped. Volume ramps advance in Update(), which the app calls 100 times a second like the
    /// original.
    /// </summary>
    public class StreamMusicInterface : MusicInterface
    {
        /// <summary>Optional platform backend, set before the app is created.</summary>
        public static MusicPlayer PlatformPlayer;

        private readonly MusicPlayer mPlayer;

        private readonly Dictionary<int, string> mPaths = new Dictionary<int, string>();

        private readonly Dictionary<int, object> mSongs = new Dictionary<int, object>();

        private bool mEnabled;

        private int mCurrent = -1;

        private bool mCurrentLoop;

        private int mPending = -1;

        private bool mPendingLoop;

        private float mPendingFadeIn;

        private int mRequestedId = -1;

        private double mVolume;

        private double mQueueVolume;

        private double mVolumeStep;

        private double mVolumeTarget;

        private float mDefaultFadeIn = 0.1f;

        private float mDefaultFadeOut = 0.5f;

        public StreamMusicInterface()
        {
            mEnabled = true;
            mPlayer = PlatformPlayer ?? new VorbisMusicPlayer();
        }

        public override void Enable(bool enable)
        {
            mEnabled = enable;
            if (!enable)
            {
                Stop();
            }
        }

        public override bool LoadMusic(int theSongId, string theFileName)
        {
            string full = ContentFS.Resolve(theFileName);
            if (full == null)
            {
                return false;
            }
            mPaths[theSongId] = full;
            return true;
        }

        private object GetSong(int theSongId)
        {
            if (mSongs.TryGetValue(theSongId, out object song))
            {
                return song;
            }
            if (!mPaths.TryGetValue(theSongId, out string path))
            {
                return null;
            }
            try
            {
                song = mPlayer.Open(path);
            }
            catch (Exception e)
            {
                Debug.OutputDebug("Music load failed: " + path + " " + e.Message);
                song = null;
            }
            mSongs[theSongId] = song;
            return song;
        }

        public override void PreloadMusic(int theSongId)
        {
            object song = GetSong(theSongId);
            if (song != null)
            {
                mPlayer.Preload(song);
            }
        }

        public override void SetDefaultFadeIn(float fadeInSeconds)
        {
            mDefaultFadeIn = fadeInSeconds;
        }

        public override void SetDefaultFadeOut(float fadeOutSeconds)
        {
            mDefaultFadeOut = fadeOutSeconds;
        }

        public override void PlayMusic(int theSongId)
        {
            PlayMusic(theSongId, 0, mDefaultFadeOut, mDefaultFadeIn, false);
        }

        public override void PlayMusic(int theSongId, bool loop)
        {
            PlayMusic(theSongId, 0, mDefaultFadeOut, mDefaultFadeIn, loop);
        }

        public override void PlayMusic(int theSongid, int offset, float fadeOutSeconds, float fadeInSeconds, bool loop)
        {
            mRequestedId = theSongid;
            isStopped = false;
            if (!mEnabled || !mPaths.ContainsKey(theSongid))
            {
                return;
            }
            if (theSongid == mCurrent  && theSongid != 13)
            {
                return;
            }
            if (mCurrent == -1)
            {
                FadeIn(theSongid, fadeInSeconds, loop);
            }
            else
            {
                mPending = theSongid;
                mPendingLoop = loop;
                mPendingFadeIn = fadeInSeconds;
                FadeOut(fadeOutSeconds);
            }
        }

        private void SetVolumeTarget(double theTarget, double theSeconds)
        {
            mVolumeTarget = theTarget;
            mVolumeStep = theSeconds > 0.0 ? (mVolumeTarget - mQueueVolume) / theSeconds / 100.0 : mVolumeTarget - mQueueVolume;
        }

        private void SetQueueVolume(double theVolume)
        {
            mQueueVolume = theVolume;
            if (mCurrent != -1)
            {
                mPlayer.SetVolume((float)Math.Clamp(mQueueVolume, 0.0, 1.0));
            }
        }

        private void FadeOut(float theSeconds)
        {
            if (mQueueVolume > 0.0 && theSeconds > 0.0)
            {
                SetVolumeTarget(0.0, theSeconds);
                return;
            }
            Stop();
        }

        private void FadeIn(int theSongId, float theSeconds, bool loop)
        {
            SetQueueVolume(0.0);
            SetVolumeTarget(mVolume, theSeconds);
            Start(theSongId, loop);
        }

        private void Start(int theSongId, bool loop)
        {
            object song = GetSong(theSongId);
            if (song == null)
            {
                return;
            }
            mCurrent = theSongId;
            mCurrentLoop = loop;
            mPlayer.SetVolume((float)Math.Clamp(mQueueVolume, 0.0, 1.0));
            mPlayer.Play(song, loop);
        }

        private void Stop()
        {
            mCurrent = -1;
            mVolumeStep = 0.0;
            mPlayer.Stop();
        }

        public override void StopMusic()
        {
            StopMusic(mDefaultFadeOut);
        }

        public override void StopMusic(float fadeOutSeconds)
        {
            mRequestedId = -1;
            mPending = -1;
            isStopped = true;
            if (mCurrent != -1)
            {
                FadeOut(fadeOutSeconds);
            }
        }

        public override void PauseMusic()
        {
            if (mCurrent != -1)
            {
                mPlayer.Pause();
            }
        }

        public override void ResumeMusic()
        {
            if (mCurrent != -1)
            {
                mPlayer.Resume();
            }
        }

        public override void UnloadMusic(int theSongId)
        {
            if (mSongs.TryGetValue(theSongId, out object song))
            {
                if (mCurrent == theSongId)
                {
                    Stop();
                }
                if (song != null)
                {
                    mPlayer.Close(song);
                }
                mSongs.Remove(theSongId);
            }
        }

        public override void UnloadAllMusic()
        {
            Stop();
            foreach (object song in mSongs.Values)
            {
                if (song != null)
                {
                    mPlayer.Close(song);
                }
            }
            mSongs.Clear();
        }

        public override bool IsPlaying(int theSongId)
        {
            return mCurrent != -1 && mCurrent == theSongId;
        }

        public override void SetVolume(float theVolume)
        {
            mVolume = theVolume;
            SetQueueVolume(mVolume);
        }

        public override float GetVolume()
        {
            return (float)mVolume;
        }

        public override void Update()
        {
            if (mEnabled && mCurrent != -1)
            {
                if (mVolumeStep > 0.0)
                {
                    if (mQueueVolume + mVolumeStep > mVolumeTarget)
                    {
                        mVolumeStep = 0.0;
                        SetQueueVolume(mVolumeTarget);
                    }
                    else
                    {
                        SetQueueVolume(mQueueVolume + mVolumeStep);
                    }
                }
                else if (mVolumeStep < 0.0)
                {
                    if (mQueueVolume + mVolumeStep < mVolumeTarget)
                    {
                        mVolumeStep = 0.0;
                        Stop();
                    }
                    else
                    {
                        SetQueueVolume(mQueueVolume + mVolumeStep);
                    }
                }
                if (mCurrent != -1 && !mCurrentLoop && mPlayer.IsStopped())
                {
                    mCurrent = -1;
                }
            }
            if (mEnabled && mCurrent == -1 && mPending != -1)
            {
                int pending = mPending;
                mPending = -1;
                FadeIn(pending, mPendingFadeIn, mPendingLoop);
            }
        }
    }
}
