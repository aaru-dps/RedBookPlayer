using CSCore.SoundOut;

namespace RedBookPlayer.Models.Audio.Windows
{
    public class AudioBackend : IAudioBackend
    {
        /// <summary>
        /// Sound output instance
        /// </summary>
        private readonly ALSoundOut _soundOut;

        public AudioBackend() { }

        public AudioBackend(PlayerSource source)
        {
            _soundOut = new ALSoundOut(100);
            _soundOut.Initialize(source);
        }

        #region IAudioBackend Implementation

        /// <inheritdoc/>
        public void Pause() => _soundOut.Pause();

        /// <inheritdoc/>
        public void Play() => _soundOut.Play();

        /// <inheritdoc/>
        public void Stop() => _soundOut.Stop();

        /// <inheritdoc/>
        public PlayerState GetPlayerState()
        {
            return (_soundOut?.PlaybackState) switch
            {
                PlaybackState.Paused => PlayerState.Paused,
                PlaybackState.Playing => PlayerState.Playing,
                PlaybackState.Stopped => PlayerState.Stopped,
                _ => PlayerState.NoDisc,
            };
        }

        /// <inheritdoc/>
        public void SetVolume(float volume)
        {
            if(_soundOut != null)
                _soundOut.Volume = volume;
        }

        #endregion
    }
}