using System;
using Microsoft.Xna.Framework.Media;

namespace Sexy
{
    /// <summary>
    /// The audio backend under StreamMusicInterface: plays one track at a time. A track is an
    /// opaque handle from Open(). Platforms can replace the MonoGame one (see
    /// StreamMusicInterface.PlatformPlayer).
    /// </summary>
    public abstract class MusicPlayer
    {
        /// <summary>Opens a track (null on failure).</summary>
        public abstract object Open(string theFullPath);

        /// <summary>Gets a track ready so a later Play() starts without delay, and keeps it ready.</summary>
        public virtual void Preload(object theTrack)
        {
        }

        public abstract void Play(object theTrack, bool loop);

        public abstract void Stop();

        public abstract void Pause();

        public abstract void Resume();

        public abstract void SetVolume(float theVolume);

        /// <summary>True once the current track has ended (or nothing is playing).</summary>
        public abstract bool IsStopped();

        public abstract void Close(object theTrack);

        /// <summary>Called with StreamMusicInterface.Update (100 times a second).</summary>
        public virtual void Update()
        {
        }
    }
}
