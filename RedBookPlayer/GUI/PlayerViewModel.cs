using System.Linq;
using Aaru.CommonTypes.Enums;
using ReactiveUI;
using RedBookPlayer.Discs;
using RedBookPlayer.Hardware;

namespace RedBookPlayer.GUI
{
    public class PlayerViewModel : ReactiveObject
    {
        /// <summary>
        /// Player representing the internal state
        /// </summary>
        private Player _player;

        #region Player Status

        public bool Initialized => _player?.Initialized ?? false;

        private bool? _playing;
        public bool? Playing
        {
            get => _playing;
            set => this.RaiseAndSetIfChanged(ref _playing, value);
        }

        private int _volume;
        public int Volume
        {
            get => _volume;
            set => this.RaiseAndSetIfChanged(ref _volume, value);
        }

        private bool _applyDeEmphasis;
        public bool ApplyDeEmphasis
        {
            get => _applyDeEmphasis;
            set => this.RaiseAndSetIfChanged(ref _applyDeEmphasis, value);
        }

        #endregion

        #region Model-Provided Playback Information

        private ulong _currentSector;
        public ulong CurrentSector
        {
            get => _currentSector;
            set => this.RaiseAndSetIfChanged(ref _currentSector, value);
        }

        public int CurrentFrame => (int)(_currentSector / (75 * 60));
        public int CurrentSecond => (int)(_currentSector / 75 % 60);
        public int CurrentMinute => (int)(_currentSector % 75);

        private ulong _totalSectors;
        public ulong TotalSectors
        {
            get => _totalSectors;
            set => this.RaiseAndSetIfChanged(ref _totalSectors, value);
        }

        public int TotalFrames => (int)(_totalSectors / (75 * 60));
        public int TotalSeconds => (int)(_totalSectors / 75 % 60);
        public int TotalMinutes => (int)(_totalSectors % 75);

        #endregion

        #region Disc Flags

        private bool _quadChannel;
        public bool QuadChannel
        {
            get => _quadChannel;
            set => this.RaiseAndSetIfChanged(ref _quadChannel, value);
        }

        private bool _isDataTrack;
        public bool IsDataTrack
        {
            get => _isDataTrack;
            set => this.RaiseAndSetIfChanged(ref _isDataTrack, value);
        }

        private bool _copyAllowed;
        public bool CopyAllowed
        {
            get => _copyAllowed;
            set => this.RaiseAndSetIfChanged(ref _copyAllowed, value);
        }

        private bool _trackHasEmphasis;
        public bool TrackHasEmphasis
        {
            get => _trackHasEmphasis;
            set => this.RaiseAndSetIfChanged(ref _trackHasEmphasis, value);
        }

        private bool _hiddenTrack;
        public bool HiddenTrack
        {
            get => _hiddenTrack;
            set => this.RaiseAndSetIfChanged(ref _hiddenTrack, value);
        }

        #endregion

        /// <summary>
        /// Initialize the view model with a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(string path, bool autoPlay)
        {
            _player = new Player();
            _player.Init(path, autoPlay);

            if(Initialized)
                UpdateModel();
        }

        #region Playback

        /// <summary>
        /// Move to the next playable track
        /// </summary>
        public void NextTrack() => _player?.NextTrack();

        /// <summary>
        /// Move to the previous playable track
        /// </summary>
        public void PreviousTrack() => _player?.PreviousTrack();

        /// <summary>
        /// Move to the next index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void NextIndex(bool changeTrack) => _player?.NextIndex(changeTrack);

        /// <summary>
        /// Move to the previous index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void PreviousIndex(bool changeTrack) => _player?.PreviousIndex(changeTrack);

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
        /// Generate the digit string to be interpreted by the frontend
        /// </summary>
        /// <returns>String representing the digits for the frontend</returns>
        public string GenerateDigitString()
        {
            // If the disc isn't initialized, return all '-' characters
            if(_player?.OpticalDisc == null || !_player.OpticalDisc.Initialized)
                return string.Empty.PadLeft(20, '-');

            // Otherwise, take the current time into account
            ulong sectorTime = _player.GetCurrentSectorTime();

            int[] numbers = new int[]
            {
                _player.OpticalDisc.CurrentTrackNumber + 1,
                _player.OpticalDisc.CurrentTrackIndex,

                (int)(sectorTime / (75 * 60)),
                (int)(sectorTime / 75 % 60),
                (int)(sectorTime % 75),

                _player.OpticalDisc.TotalTracks,
                _player.OpticalDisc.TotalIndexes,

                (int)(_player.OpticalDisc.TotalTime / (75 * 60)),
                (int)(_player.OpticalDisc.TotalTime / 75 % 60),
                (int)(_player.OpticalDisc.TotalTime % 75),
            };

            return string.Join("", numbers.Select(i => i.ToString().PadLeft(2, '0').Substring(0, 2)));
        }

        /// <summary>
        /// Update the UI from the internal player
        /// </summary>
        public void UpdateView()
        {
            if(_player?.Initialized != true)
                return;

            Playing = _player.Playing;
            CurrentSector = _player.GetCurrentSectorTime();
            TotalSectors = _player.OpticalDisc.TotalTime;
            Volume = App.Settings.Volume;

            ApplyDeEmphasis = _player.ApplyDeEmphasis;
            HiddenTrack = _player.OpticalDisc.TimeOffset > 150;

            if(_player.OpticalDisc is CompactDisc compactDisc)
            {
                QuadChannel = compactDisc.QuadChannel;
                IsDataTrack = compactDisc.IsDataTrack;
                CopyAllowed = compactDisc.CopyAllowed;
                TrackHasEmphasis = compactDisc.TrackHasEmphasis;
            }
            else
            {
                QuadChannel = false;
                IsDataTrack = _player.OpticalDisc.TrackType != TrackType.Audio;
                CopyAllowed = false;
                TrackHasEmphasis = false;
            }
        }

        /// <summary>
        /// Update the internal player from the UI
        /// </summary>
        public void UpdateModel()
        {
            if(_player?.Initialized != true)
                return;

            _player.SetPlayingState(Playing);
            App.Settings.Volume = Volume;
            _player.SetDeEmphasis(ApplyDeEmphasis);
        }

        #endregion
    }
}