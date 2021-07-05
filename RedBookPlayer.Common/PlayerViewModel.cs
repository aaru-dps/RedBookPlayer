using System.ComponentModel;
using ReactiveUI;
using RedBookPlayer.Common.Hardware;

namespace RedBookPlayer.Common
{
    public class PlayerViewModel : ReactiveObject
    {
        /// <summary>
        /// Player representing the internal state
        /// </summary>
        private Player _player;

        /// <summary>
        /// Last volume for mute toggling
        /// </summary>
        private int? _lastVolume = null;

        #region Player Passthrough

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
        public int TotalTracks => _player.TotalTracks;

        /// <summary>
        /// Represents the total indices on the disc
        /// </summary>
        public int TotalIndexes => _player.TotalIndexes;

        /// <summary>
        /// Total sectors in the image
        /// </summary>
        public ulong TotalSectors => _player.TotalSectors;

        /// <summary>
        /// Represents the time adjustment offset for the disc
        /// </summary>
        public ulong TimeOffset => _player.TimeOffset;

        /// <summary>
        /// Represents the total playing time for the disc
        /// </summary>
        public ulong TotalTime => _player.TotalTime;

        private int _currentTrackNumber;
        private ushort _currentTrackIndex;
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
        /// Indicate if the model is ready to be used
        /// </summary>
        public bool Initialized => _player?.Initialized ?? false;

        /// <summary>
        /// Indicate if the output is playing
        /// </summary>
        public bool? Playing
        {
            get => _playing;
            private set => this.RaiseAndSetIfChanged(ref _playing, value);
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

        private bool? _playing;
        private bool _applyDeEmphasis;
        private int _volume;

        #endregion

        #endregion

        /// <summary>
        /// Initialize the view model with a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="generateMissingToc">Generate a TOC if the disc is missing one [CompactDisc only]</param>
        /// <param name="loadDataTracks">Load data tracks for playback [CompactDisc only]</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        /// <param name="defaultVolume">Default volume between 0 and 100 to use when starting playback</param>
        public void Init(string path, bool generateMissingToc, bool loadDataTracks, bool autoPlay, int defaultVolume)
        {
            // Stop current playback, if necessary
            if(Playing != null) Playing = null;

            // Create and attempt to initialize new Player
            _player = new Player(path, generateMissingToc, loadDataTracks, autoPlay, defaultVolume);
            if(Initialized)
            {
                _player.PropertyChanged += PlayerStateChanged;
                PlayerStateChanged(this, null);
            }
        }

        #region Playback

        /// <summary>
        /// Begin playback
        /// </summary>
        public void Play() => _player?.Play();

        /// <summary>
        /// Pause current playback
        /// </summary>
        public void Pause() => _player?.Pause();

        /// <summary>
        /// Stop current playback
        /// </summary>
        public void Stop() => _player?.Stop();

        /// <summary>
        /// Move to the next playable track
        /// </summary>
        public void NextTrack() => _player?.NextTrack();

        /// <summary>
        /// Move to the previous playable track
        /// </summary>
        /// <param name="playHiddenTrack">True to play the hidden track, if it exists</param>
        public void PreviousTrack(bool playHiddenTrack) => _player?.PreviousTrack(playHiddenTrack);

        /// <summary>
        /// Move to the next index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void NextIndex(bool changeTrack) => _player?.NextIndex(changeTrack);

        /// <summary>
        /// Move to the previous index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        /// <param name="playHiddenTrack">True to play the hidden track, if it exists</param>
        public void PreviousIndex(bool changeTrack, bool playHiddenTrack) => _player?.PreviousIndex(changeTrack, playHiddenTrack);

        /// <summary>
        /// Fast-forward playback by 75 sectors, if possible
        /// </summary>
        public void FastForward() => _player?.FastForward();

        /// <summary>
        /// Rewind playback by 75 sectors, if possible
        /// </summary>
        public void Rewind() => _player?.Rewind();

        #endregion

        #region Helpers

        /// <summary>
        /// Set de-emphasis status
        /// </summary>
        /// <param name="apply"></param>
        public void SetDeEmphasis(bool apply) => _player?.SetDeEmphasis(apply);

        /// <summary>
        /// Set the value for loading data tracks [CompactDisc only]
        /// </summary>
        /// <param name="load">True to enable loading data tracks, false otherwise</param>
        public void SetLoadDataTracks(bool load) => _player?.SetLoadDataTracks(load);

        /// <summary>
        /// Set the value for the volume
        /// </summary>
        /// <param name="volume">New volume value</param>
        public void SetVolume(int volume) => _player?.SetVolume(volume);

        /// <summary>
        /// Temporarily mute playback
        /// </summary>
        public void ToggleMute()
        {
            if(_lastVolume == null)
            {
                _lastVolume = Volume;
                _player?.SetVolume(0);
            }
            else
            {
                _player?.SetVolume(_lastVolume.Value);
                _lastVolume = null;
            }
        }

        /// <summary>
        /// Update the view-model from the Player
        /// </summary>
        private void PlayerStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if(_player?.Initialized != true)
                return;

            CurrentTrackNumber = _player.CurrentTrackNumber;
            CurrentTrackIndex = _player.CurrentTrackIndex;
            CurrentSector = _player.CurrentSector;
            SectionStartSector = _player.SectionStartSector;

            HiddenTrack = _player.HiddenTrack;

            QuadChannel = _player.QuadChannel;
            IsDataTrack = _player.IsDataTrack;
            CopyAllowed = _player.CopyAllowed;
            TrackHasEmphasis = _player.TrackHasEmphasis;

            Playing = _player.Playing;
            ApplyDeEmphasis = _player.ApplyDeEmphasis;
            Volume = _player.Volume;
        }

        #endregion
    }
}