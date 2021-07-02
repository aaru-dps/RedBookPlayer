using ReactiveUI;

namespace RedBookPlayer.GUI
{
    public class PlayerViewModel : ReactiveObject
    {
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
    }
}