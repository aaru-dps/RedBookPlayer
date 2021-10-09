using System.Runtime.InteropServices;
using ReactiveUI;

namespace RedBookPlayer.Models.Audio
{
    public class SoundOutput : ReactiveObject
    {
        #region Public Fields

        /// <summary>
        /// Indicate if the output is ready to be used
        /// </summary>
        public bool Initialized
        {
            get => _initialized;
            private set => this.RaiseAndSetIfChanged(ref _initialized, value);
        }

        /// <summary>
        /// Indicates the current player state
        /// </summary>
        public PlayerState PlayerState
        {
            get => _playerState;
            private set => this.RaiseAndSetIfChanged(ref _playerState, value);
        }

        /// <summary>
        /// Current playback volume
        /// </summary>
        public int Volume
        {
            get => _volume;
            private set
            {
                int tempVolume = value;
                if(value > 100)
                    tempVolume = 100;
                else if(value < 0)
                    tempVolume = 0;

                this.RaiseAndSetIfChanged(ref _volume, tempVolume);
            }
        }

        private bool _initialized;
        private PlayerState _playerState;
        private int _volume;

        #endregion

        #region Private State Variables

        /// <summary>
        /// Data provider for sound output
        /// </summary>
        private PlayerSource _source;

        /// <summary>
        /// Sound output instance
        /// </summary>
        private IAudioBackend _soundOut;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="defaultVolume">Default volume between 0 and 100 to use when starting playback</param>
        public SoundOutput(int defaultVolume = 100) => Volume = defaultVolume;

        /// <summary>
        /// Initialize the output with a given image
        /// </summary>
        /// <param name="read">ReadFunction to use during decoding</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(PlayerSource.ReadFunction read, bool autoPlay)
        {
            // Reset initialization
            Initialized = false;

            // Setup the audio output
            SetupAudio(read);

            // Initialize playback, if necessary
            if(autoPlay)
                _soundOut.Play();

            // Mark the output as ready
            Initialized = true;
            PlayerState = PlayerState.Stopped;

            // Begin loading data
            _source.Start();
        }

        /// <summary>
        /// Reset the current internal state
        /// </summary>
        public void Reset()
        {
            _soundOut.Stop();
            Initialized = false;
            PlayerState = PlayerState.NoDisc;
        }

        #region Playback

        /// <summary>
        /// Start audio playback
        /// </summary>
        public void Play()
        {
            if(_soundOut.GetPlayerState() != PlayerState.Playing)
                _soundOut.Play();

            PlayerState = PlayerState.Playing;
        }

        /// <summary>
        /// Pause audio playback
        /// </summary>
        public void Pause()
        {
            if(_soundOut.GetPlayerState() != PlayerState.Paused)
                _soundOut.Pause();

            PlayerState = PlayerState.Paused;
        }

        /// <summary>
        /// Stop audio playback
        /// </summary>
        public void Stop()
        {
            if(_soundOut.GetPlayerState() != PlayerState.Stopped)
                _soundOut.Stop();

            PlayerState = PlayerState.Stopped;
        }

        /// <summary>
        /// Eject the currently loaded disc
        /// </summary>
        public void Eject() => Reset();

        #endregion

        #region Helpers

        /// <summary>
        /// Set the value for the volume
        /// </summary>
        /// <param name="volume">New volume value</param>
        public void SetVolume(int volume)
        {
            Volume = volume;
            _soundOut?.SetVolume((float)Volume / 100);
        }

        /// <summary>
        /// Sets or resets the audio playback objects
        /// </summary>
        /// <param name="read">ReadFunction to use during decoding</param>
        private void SetupAudio(PlayerSource.ReadFunction read)
        {
            if(_source == null)
            {
                _source = new PlayerSource(read);
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    _soundOut = new Linux.AudioBackend(_source);
                else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _soundOut = new Windows.AudioBackend(_source);
            }
            else
            {
                _soundOut.Stop();
            }
        }

        #endregion
    }
}