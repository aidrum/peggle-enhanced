using System;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using NVorbis;

namespace Sexy
{
    /// <summary>
    /// Music player implementation on top of NVorbis and DynamicSoundEffectInstance.
    /// Uses a dedicated background thread for decoding and buffer submission so music 
    /// continues playing seamlessly even if the main game thread freezes during screen transitions.
    /// </summary>
    public class VorbisMusicPlayer : MusicPlayer, IDisposable
    {
        internal sealed class VorbisTrack : IDisposable
        {
            public string Path;
            public VorbisReader Reader;
            public DynamicSoundEffectInstance Instance;
            public bool Loop;
            public bool Finished;
            public float[] FloatBuffer;

            public VorbisTrack(string path)
            {
                Path = path;
            }

            public bool EnsureLoaded()
            {
                if (Reader != null && Instance != null && !Instance.IsDisposed)
                {
                    return true;
                }
                try
                {
                    if (Reader == null)
                    {
                        Reader = new VorbisReader(ContentFS.OpenResolved(Path), true);
                    }
                    if (Instance == null || Instance.IsDisposed)
                    {
                        var channels = Reader.Channels == 2 ? AudioChannels.Stereo : AudioChannels.Mono;
                        Instance = new DynamicSoundEffectInstance(Reader.SampleRate, channels);
                    }
                    if (FloatBuffer == null)
                    {
                        // 4096 samples per channel = ~85-93ms of audio per buffer
                        int bufferSamples = Reader.Channels * 4096;
                        FloatBuffer = new float[bufferSamples];
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Debug.OutputDebug("VorbisTrack load failed: " + Path + " " + e.Message);
                    Dispose();
                    return false;
                }
            }

            public void Dispose()
            {
                if (Instance != null)
                {
                    try
                    {
                        if (!Instance.IsDisposed)
                        {
                            Instance.Stop();
                            Instance.Dispose();
                        }
                    }
                    catch { }
                    Instance = null;
                }
                if (Reader != null)
                {
                    try { Reader.Dispose(); } catch { }
                    Reader = null;
                }
                FloatBuffer = null;
            }
        }

        private const int TARGET_BUFFER_COUNT = 8; // ~750ms of queued audio headroom
        private readonly object mLock = new object();
        private readonly AutoResetEvent mWakeEvent = new AutoResetEvent(false);
        private readonly Thread mStreamingThread;
        private volatile bool mIsRunning = true;

        private VorbisTrack mCurrent;
        private float mVolume = 1f;

        public VorbisMusicPlayer()
        {
            mStreamingThread = new Thread(StreamingThreadProc)
            {
                IsBackground = true,
                Name = "VorbisAudioStreamingThread",
                Priority = ThreadPriority.Highest
            };
            mStreamingThread.Start();
        }

        ~VorbisMusicPlayer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            mIsRunning = false;
            mWakeEvent.Set();
            Stop();
        }

        /// <summary>
        /// Dedicated background streaming loop running independently of the game thread.
        /// </summary>
        private void StreamingThreadProc()
        {
            while (mIsRunning)
            {
                try
                {
                    mWakeEvent.WaitOne(10);
                    if (!mIsRunning)
                    {
                        break;
                    }

                    lock (mLock)
                    {
                        if (mCurrent != null && mCurrent.EnsureLoaded())
                        {
                            if (mCurrent.Instance != null && !mCurrent.Instance.IsDisposed && mCurrent.Instance.State == SoundState.Playing)
                            {
                                FillBuffers(mCurrent, TARGET_BUFFER_COUNT);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.OutputDebug("Vorbis audio thread error: " + e.Message);
                }
            }
        }

        public override object Open(string theFullPath)
        {
            // theFullPath comes from ContentFS.Resolve, so the file exists (possibly only inside the APK).
            if (string.IsNullOrEmpty(theFullPath))
            {
                return null;
            }
            var track = new VorbisTrack(theFullPath);
            if (!track.EnsureLoaded())
            {
                return null;
            }
            return track;
        }

        public override void Preload(object theTrack)
        {
            if (theTrack is VorbisTrack track)
            {
                lock (mLock)
                {
                    track.EnsureLoaded();
                }
            }
        }

        private static void SeekToStart(VorbisReader reader)
        {
            try
            {
                if (reader != null && reader.SamplePosition != 0)
                {
                    reader.SamplePosition = 0;
                }
            }
            catch
            {
                // Ignore seek error if reader is already at start or seeking is not supported
            }
        }

        public override void Play(object theTrack, bool loop)
        {
            if (!(theTrack is VorbisTrack track))
            {
                return;
            }

            lock (mLock)
            {
                if (mCurrent != null && mCurrent != track)
                {
                    Stop();
                }

                if (!track.EnsureLoaded())
                {
                    return;
                }

                mCurrent = track;
                track.Loop = loop;
                track.Instance.Pitch = 0f;
                track.Finished = false;
                SeekToStart(track.Reader);

                if (track.Instance.State != SoundState.Stopped)
                {
                    track.Instance.Stop();
                }

                track.Instance.BufferNeeded -= OnBufferNeeded;
                track.Instance.BufferNeeded += OnBufferNeeded;

                // Pre-fill initial buffers
                FillBuffers(track, TARGET_BUFFER_COUNT);

                track.Instance.Volume = mVolume;
                track.Instance.Play();

                mWakeEvent.Set();
            }
        }

        private void OnBufferNeeded(object sender, EventArgs e)
        {
            mWakeEvent.Set();
        }

        private void FillBuffers(VorbisTrack track, int targetCount)
        {
            if (track.Instance == null || track.Instance.IsDisposed || track.Reader == null)
            {
                return;
            }

            while (track.Instance.PendingBufferCount < targetCount && !track.Finished)
            {
                int totalRead = 0;
                while (totalRead < track.FloatBuffer.Length)
                {
                    int read = track.Reader.ReadSamples(track.FloatBuffer, totalRead, track.FloatBuffer.Length - totalRead);
                    if (read == 0)
                    {
                        if (track.Loop)
                        {
                            SeekToStart(track.Reader);
                            int loopedRead = track.Reader.ReadSamples(track.FloatBuffer, totalRead, track.FloatBuffer.Length - totalRead);
                            if (loopedRead == 0)
                            {
                                track.Finished = true;
                                break;
                            }
                            totalRead += loopedRead;
                        }
                        else
                        {
                            track.Finished = true;
                            break;
                        }
                    }
                    else
                    {
                        totalRead += read;
                    }
                }

                if (totalRead > 0)
                {
                    // Allocate a fresh byte array per buffer submission to prevent driver buffer corruption
                    byte[] pcmBuffer = new byte[totalRead * 2];
                    for (int i = 0; i < totalRead; i++)
                    {
                        short pcm = (short)Math.Clamp((int)(track.FloatBuffer[i] * 32767f), -32768, 32767);
                        pcmBuffer[i * 2] = (byte)(pcm & 0xFF);
                        pcmBuffer[i * 2 + 1] = (byte)(pcm >> 8);
                    }
                    track.Instance.SubmitBuffer(pcmBuffer, 0, totalRead * 2);
                }
            }
        }

        public override void Stop()
        {
            lock (mLock)
            {
                if (mCurrent != null)
                {
                    if (mCurrent.Instance != null && !mCurrent.Instance.IsDisposed)
                    {
                        mCurrent.Instance.BufferNeeded -= OnBufferNeeded;
                        mCurrent.Instance.Stop();
                    }
                    if (mCurrent.Reader != null)
                    {
                        SeekToStart(mCurrent.Reader);
                    }
                    mCurrent = null;
                }
            }
        }

        public override void Pause()
        {
            lock (mLock)
            {
                if (mCurrent?.Instance != null && !mCurrent.Instance.IsDisposed && mCurrent.Instance.State == SoundState.Playing)
                {
                    mCurrent.Instance.Pause();
                }
            }
        }

        public override void Resume()
        {
            lock (mLock)
            {
                if (mCurrent?.Instance != null && !mCurrent.Instance.IsDisposed && mCurrent.Instance.State == SoundState.Paused)
                {
                    mCurrent.Instance.Resume();
                    mWakeEvent.Set();
                }
            }
        }

        public override void SetVolume(float theVolume)
        {
            lock (mLock)
            {
                mVolume = Math.Clamp(theVolume, 0f, 1f);
                if (mCurrent?.Instance != null && !mCurrent.Instance.IsDisposed)
                {
                    mCurrent.Instance.Volume = mVolume;
                }
            }
        }

        public override bool IsStopped()
        {
            lock (mLock)
            {
                if (mCurrent == null)
                {
                    return true;
                }
                if (mCurrent.Instance == null || mCurrent.Instance.IsDisposed)
                {
                    return true;
                }
                 if (mCurrent.Instance.PendingBufferCount != 0)
                {
                    return false;
                }
                return mCurrent.Finished || mCurrent.Instance.State == SoundState.Stopped;
            }
        }

        public override void Close(object theTrack)
        {
            if (theTrack is VorbisTrack track)
            {
                lock (mLock)
                {
                    if (mCurrent == track)
                    {
                        Stop();
                    }
                    track.Dispose();
                }
            }
        }
    }
}