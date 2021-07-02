using ReactiveUI;

namespace RedBookPlayer.GUI
{
    public class PlayerViewModel : ReactiveObject
    {
        #region Player Status

        private bool _playing;
        public bool Playing
        {
            get => _playing;
            set => this.RaiseAndSetIfChanged(ref _playing, value);
        }

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

        private int _volume;
        public int Volume
        {
            get => _volume;
            set => this.RaiseAndSetIfChanged(ref _volume, value);
        }

        #endregion

        #region Disc Flags

        private bool _applyDeEmphasis;
        public bool ApplyDeEmphasis
        {
            get => _applyDeEmphasis;
            set => this.RaiseAndSetIfChanged(ref _applyDeEmphasis, value);
        }

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
    }
}