using System.ComponentModel;
using Aaru.CommonTypes.Enums;
using ReactiveUI;
using RedBookPlayer.Models.Discs;
using RedBookPlayer.Models.Factories;

namespace RedBookPlayer.Models.Hardware
{
    public class Player : ReactiveObject
    {
        /// <summary>
        /// Indicate if the player is ready to be used
        /// </summary>
        public bool Initialized
        {
            get => _initialized;
            private set => this.RaiseAndSetIfChanged(ref _initialized, value);
        }

        private bool _initialized;

        #region OpticalDisc Passthrough

        /// <summary>
        /// Current track number
        /// </summary>
        public int CurrentTrackNumber
        {
            get => _currentTrackNumber;
            private set => this.RaiseAndSetIfChanged(ref _currentTrackNumber, value);
        }

        /// <summary>
        /// Current track index
        /// </summary>
        public ushort CurrentTrackIndex
        {
            get => _currentTrackIndex;
            private set => this.RaiseAndSetIfChanged(ref _currentTrackIndex, value);
        }

        /// <summary>
        /// Current track session
        /// </summary>
        public ushort CurrentTrackSession
        {
            get => _currentTrackSession;
            private set => this.RaiseAndSetIfChanged(ref _currentTrackSession, value);
        }

        /// <summary>
        /// Current sector number
        /// </summary>
        public ulong CurrentSector
        {
            get => _currentSector;
            private set => this.RaiseAndSetIfChanged(ref _currentSector, value);
        }

        /// <summary>
        /// Represents the sector starting the section
        /// </summary>
        public ulong SectionStartSector
        {
            get => _sectionStartSector;
            protected set => this.RaiseAndSetIfChanged(ref _sectionStartSector, value);
        }

        /// <summary>
        /// Represents if the disc has a hidden track
        /// </summary>
        public bool HiddenTrack
        {
            get => _hasHiddenTrack;
            private set => this.RaiseAndSetIfChanged(ref _hasHiddenTrack, value);
        }

        /// <summary>
        /// Represents the 4CH flag [CompactDisc only]
        /// </summary>
        public bool QuadChannel
        {
            get => _quadChannel;
            private set => this.RaiseAndSetIfChanged(ref _quadChannel, value);
        }

        /// <summary>
        /// Represents the DATA flag [CompactDisc only]
        /// </summary>
        public bool IsDataTrack
        {
            get => _isDataTrack;
            private set => this.RaiseAndSetIfChanged(ref _isDataTrack, value);
        }

        /// <summary>
        /// Represents the DCP flag [CompactDisc only]
        /// </summary>
        public bool CopyAllowed
        {
            get => _copyAllowed;
            private set => this.RaiseAndSetIfChanged(ref _copyAllowed, value);
        }

        /// <summary>
        /// Represents the PRE flag [CompactDisc only]
        /// </summary>
        public bool TrackHasEmphasis
        {
            get => _trackHasEmphasis;
            private set => this.RaiseAndSetIfChanged(ref _trackHasEmphasis, value);
        }

        /// <summary>
        /// Represents the total tracks on the disc
        /// </summary>
        public int TotalTracks => _opticalDisc?.TotalTracks ?? 0;

        /// <summary>
        /// Represents the total indices on the disc
        /// </summary>
        public int TotalIndexes => _opticalDisc?.TotalIndexes ?? 0;

        /// <summary>
        /// Total sectors in the image
        /// </summary>
        public ulong TotalSectors => _opticalDisc?.TotalSectors ?? 0;

        /// <summary>
        /// Represents the time adjustment offset for the disc
        /// </summary>
        public ulong TimeOffset => _opticalDisc?.TimeOffset ?? 0;

        /// <summary>
        /// Represents the total playing time for the disc
        /// </summary>
        public ulong TotalTime => _opticalDisc?.TotalTime ?? 0;

        private int _currentTrackNumber;
        private ushort _currentTrackIndex;
        private ushort _currentTrackSession;
        private ulong _currentSector;
        private ulong _sectionStartSector;

        private bool _hasHiddenTrack;
        private bool _quadChannel;
        private bool _isDataTrack;
        private bool _copyAllowed;
        private bool _trackHasEmphasis;

        #endregion

        #region SoundOutput Passthrough

        /// <summary>
        /// Indicates the current player state
        /// </summary>
        public PlayerState PlayerState
        {
            get => _playerState;
            private set => this.RaiseAndSetIfChanged(ref _playerState, value);
        }

        /// <summary>
        /// Indicates how to handle playback of data tracks
        /// </summary>
        public DataPlayback DataPlayback
        {
            get => _dataPlayback;
            private set => this.RaiseAndSetIfChanged(ref _dataPlayback, value);
        }

        /// <summary>
        /// Indicates the repeat mode
        /// </summary>
        public RepeatMode RepeatMode
        {
            get => _repeatMode;
            private set => this.RaiseAndSetIfChanged(ref _repeatMode, value);
        }

        /// <summary>
        /// Indicates if de-emphasis should be applied
        /// </summary>
        public bool ApplyDeEmphasis
        {
            get => _applyDeEmphasis;
            private set => this.RaiseAndSetIfChanged(ref _applyDeEmphasis, value);
        }

        /// <summary>
        /// Current playback volume
        /// </summary>
        public int Volume
        {
            get => _volume;
            private set => this.RaiseAndSetIfChanged(ref _volume, value);
        }

        private PlayerState _playerState;
        private DataPlayback _dataPlayback;
        private RepeatMode _repeatMode;
        private bool _applyDeEmphasis;
        private int _volume;

        #endregion

        #region Private State Variables

        /// <summary>
        /// Sound output handling class
        /// </summary>
        private readonly SoundOutput _soundOutput;

        /// <summary>
        /// OpticalDisc object
        /// </summary>
        private OpticalDiscBase _opticalDisc;

        /// <summary>
        /// Last volume for mute toggling
        /// </summary>
        private int? _lastVolume = null;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="defaultVolume">Default volume between 0 and 100 to use when starting playback</param>
        public Player(int defaultVolume)
        {
            Initialized = false;
            _soundOutput = new SoundOutput(defaultVolume);
            _soundOutput.SetDeEmphasis(false);
        }

        /// <summary>
        /// Initializes player from a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="options">Options to pass to the optical disc factory</param>
        /// <param name="repeatMode">RepeatMode for sound output</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(string path, OpticalDiscOptions options, RepeatMode repeatMode, bool autoPlay)
        {
            // Reset initialization
            Initialized = false;

            // Initalize the disc
            _opticalDisc = OpticalDiscFactory.GenerateFromPath(path, options, autoPlay);
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            // Add event handling for the optical disc
            _opticalDisc.PropertyChanged += OpticalDiscStateChanged;

            // Initialize the sound output
            _soundOutput.Init(_opticalDisc, repeatMode, autoPlay);
            if(_soundOutput == null || !_soundOutput.Initialized)
                return;

            // Add event handling for the sound output
            _soundOutput.PropertyChanged += SoundOutputStateChanged;

            // Mark the player as ready
            Initialized = true;

            // Force a refresh of the state information
            OpticalDiscStateChanged(this, null);
            SoundOutputStateChanged(this, null);
        }

        #region Playback

        /// <summary>
        /// Begin playback
        /// </summary>
        public void Play()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;
            else if(_soundOutput == null)
                return;
            else if(_soundOutput.PlayerState != PlayerState.Paused && _soundOutput.PlayerState != PlayerState.Stopped)
                return;

            _soundOutput.Play();
            _opticalDisc.SetTotalIndexes();
            PlayerState = PlayerState.Playing;
        }

        /// <summary>
        /// Pause current playback
        /// </summary>
        public void Pause()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;
            else if(_soundOutput == null)
                return;
            else if(_soundOutput.PlayerState != PlayerState.Playing)
                return;

            _soundOutput?.Pause();
            PlayerState = PlayerState.Paused;
        }

        /// <summary>
        /// Toggle current playback
        /// </summary>
        public void TogglePlayback()
        {
            switch(PlayerState)
            {
                case PlayerState.NoDisc:
                    break;
                case PlayerState.Stopped:
                    Play();
                    break;
                case PlayerState.Paused:
                    Play();
                    break;
                case PlayerState.Playing:
                    Pause();
                    break;
            }
        }

        /// <summary>
        /// Stop current playback
        /// </summary>
        public void Stop()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;
            else if(_soundOutput == null)
                return;
            else if(_soundOutput.PlayerState != PlayerState.Playing && _soundOutput.PlayerState != PlayerState.Paused)
                return;

            _soundOutput.Stop();
            _opticalDisc.LoadFirstTrack();
            PlayerState = PlayerState.Stopped;
        }

        /// <summary>
        /// Eject the currently loaded disc
        /// </summary>
        public void Eject()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;
            else if(_soundOutput == null)
                return;

            Stop();
            _soundOutput.Eject();
            _opticalDisc = null;
            PlayerState = PlayerState.NoDisc;
            Initialized = false;
        }

        /// <summary>
        /// Move to the next playable track
        /// </summary>
        public void NextTrack()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            PlayerState wasPlaying = PlayerState;
            if(wasPlaying == PlayerState.Playing)
                Pause();

            _opticalDisc.NextTrack();
            if(_opticalDisc is CompactDisc compactDisc)
                _soundOutput.SetDeEmphasis(compactDisc.TrackHasEmphasis);

            if(wasPlaying == PlayerState.Playing)
                Play();
        }

        /// <summary>
        /// Move to the previous playable track
        /// </summary>
        public void PreviousTrack()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            PlayerState wasPlaying = PlayerState;
            if(wasPlaying == PlayerState.Playing)
                Pause();

            _opticalDisc.PreviousTrack();
            if(_opticalDisc is CompactDisc compactDisc)
                _soundOutput.SetDeEmphasis(compactDisc.TrackHasEmphasis);

            if(wasPlaying == PlayerState.Playing)
                Play();
        }

        /// <summary>
        /// Move to the next index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void NextIndex(bool changeTrack)
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            PlayerState wasPlaying = PlayerState;
            if(wasPlaying == PlayerState.Playing)
                Pause();

            _opticalDisc.NextIndex(changeTrack);
            if(_opticalDisc is CompactDisc compactDisc)
                _soundOutput.SetDeEmphasis(compactDisc.TrackHasEmphasis);

            if(wasPlaying == PlayerState.Playing)
                Play();
        }

        /// <summary>
        /// Move to the previous index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void PreviousIndex(bool changeTrack)
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            PlayerState wasPlaying = PlayerState;
            if(wasPlaying == PlayerState.Playing)
                Pause();

            _opticalDisc.PreviousIndex(changeTrack);
            if(_opticalDisc is CompactDisc compactDisc)
                _soundOutput.SetDeEmphasis(compactDisc.TrackHasEmphasis);

            if(wasPlaying == PlayerState.Playing)
                Play();
        }

        /// <summary>
        /// Fast-forward playback by 75 sectors
        /// </summary>
        public void FastForward()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            _opticalDisc.SetCurrentSector(_opticalDisc.CurrentSector + 75);
        }

        /// <summary>
        /// Rewind playback by 75 sectors
        /// </summary>
        public void Rewind()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            _opticalDisc.SetCurrentSector(_opticalDisc.CurrentSector - 75);
        }

        #endregion

        #region Volume

        /// <summary>
        /// Increment the volume value
        /// </summary>
        public void VolumeUp() => SetVolume(Volume + 1);

        /// <summary>
        /// Decrement the volume value
        /// </summary>
        public void VolumeDown() => SetVolume(Volume + 1);

        /// <summary>
        /// Set the value for the volume
        /// </summary>
        /// <param name="volume">New volume value</param>
        public void SetVolume(int volume) => _soundOutput?.SetVolume(volume);

        /// <summary>
        /// Temporarily mute playback
        /// </summary>
        public void ToggleMute()
        {
            if(_lastVolume == null)
            {
                _lastVolume = Volume;
                SetVolume(0);
            }
            else
            {
                SetVolume(_lastVolume.Value);
                _lastVolume = null;
            }
        }

        #endregion

        #region Emphasis

        /// <summary>
        /// Enable de-emphasis
        /// </summary>
        public void EnableDeEmphasis() => SetDeEmphasis(true);

        /// <summary>
        /// Disable de-emphasis
        /// </summary>
        public void DisableDeEmphasis() => SetDeEmphasis(false);

        /// <summary>
        /// Toggle de-emphasis
        /// </summary>
        public void ToggleDeEmphasis() => SetDeEmphasis(!ApplyDeEmphasis);

        /// <summary>
        /// Set de-emphasis status
        /// </summary>
        /// <param name="apply"></param>
        private void SetDeEmphasis(bool apply) => _soundOutput?.SetDeEmphasis(apply);

        #endregion

        #region Helpers

        /// <summary>
        /// Extract a single track from the image to WAV
        /// </summary>
        /// <param name="trackNumber"></param>
        /// <param name="outputDirectory">Output path to write data to</param>
        public void ExtractSingleTrackToWav(uint trackNumber, string outputDirectory) => _opticalDisc?.ExtractTrackToWav(trackNumber, outputDirectory);

        /// <summary>
        /// Extract all tracks from the image to WAV
        /// <param name="outputDirectory">Output path to write data to</param>
        public void ExtractAllTracksToWav(string outputDirectory) => _opticalDisc?.ExtractAllTracksToWav(outputDirectory);

        /// <summary>
        /// Set data playback method [CompactDisc only]
        /// </summary>
        /// <param name="dataPlayback">New playback value</param>
        public void SetDataPlayback(DataPlayback dataPlayback)
        {
            if(_opticalDisc is CompactDisc compactDisc)
                compactDisc.DataPlayback = dataPlayback;
        }

        /// <summary>
        /// Set the value for loading hidden tracks [CompactDisc only]
        /// </summary>
        /// <param name="load">True to enable loading hidden tracks, false otherwise</param>
        public void SetLoadHiddenTracks(bool load)
        {
            if(_opticalDisc is CompactDisc compactDisc)
                compactDisc.LoadHiddenTracks = load;
        }

        /// <summary>
        /// Set repeat mode
        /// </summary>
        /// <param name="repeatMode">New repeat mode value</param>
        public void SetRepeatMode(RepeatMode repeatMode) => _soundOutput?.SetRepeatMode(repeatMode);

        /// <summary>
        /// Set the value for session handling [CompactDisc only]
        /// </summary>
        /// <param name="sessionHandling">New session handling value</param>
        public void SetSessionHandling(SessionHandling sessionHandling)
        {
            if(_opticalDisc is CompactDisc compactDisc)
                compactDisc.SessionHandling = sessionHandling;
        }

        /// <summary>
        /// Update the player from the current OpticalDisc
        /// </summary>
        private void OpticalDiscStateChanged(object sender, PropertyChangedEventArgs e)
        {
            CurrentTrackNumber = _opticalDisc.CurrentTrackNumber;
            CurrentTrackIndex = _opticalDisc.CurrentTrackIndex;
            CurrentSector = _opticalDisc.CurrentSector;
            SectionStartSector = _opticalDisc.SectionStartSector;

            HiddenTrack = TimeOffset > 150;

            if(_opticalDisc is CompactDisc compactDisc)
            {
                QuadChannel = compactDisc.QuadChannel;
                IsDataTrack = compactDisc.IsDataTrack;
                CopyAllowed = compactDisc.CopyAllowed;
                TrackHasEmphasis = compactDisc.TrackHasEmphasis;
            }
            else
            {
                QuadChannel = false;
                IsDataTrack = _opticalDisc.TrackType != TrackType.Audio;
                CopyAllowed = false;
                TrackHasEmphasis = false;
            }
        }

        /// <summary>
        /// Update the player from the current SoundOutput
        /// </summary>
        private void SoundOutputStateChanged(object sender, PropertyChangedEventArgs e)
        {
            PlayerState = _soundOutput.PlayerState;
            RepeatMode = _soundOutput.RepeatMode;
            ApplyDeEmphasis = _soundOutput.ApplyDeEmphasis;
            Volume = _soundOutput.Volume;
        }

        #endregion
    }
}