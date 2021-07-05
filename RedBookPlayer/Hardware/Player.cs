using System;
using System.ComponentModel;
using System.IO;
using Aaru.CommonTypes.Enums;
using Aaru.DiscImages;
using Aaru.Filters;
using ReactiveUI;
using RedBookPlayer.Discs;

namespace RedBookPlayer.Hardware
{
    public class Player : ReactiveObject
    {
        /// <summary>
        /// Indicate if the player is ready to be used
        /// </summary>
        public bool Initialized { get; private set; } = false;

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
        public int TotalTracks => _opticalDisc.TotalTracks;

        /// <summary>
        /// Represents the total indices on the disc
        /// </summary>
        public int TotalIndexes => _opticalDisc.TotalIndexes;

        /// <summary>
        /// Total sectors in the image
        /// </summary>
        public ulong TotalSectors => _opticalDisc.TotalSectors;

        /// <summary>
        /// Represents the time adjustment offset for the disc
        /// </summary>
        public ulong TimeOffset => _opticalDisc.TimeOffset;

        /// <summary>
        /// Represents the total playing time for the disc
        /// </summary>
        public ulong TotalTime => _opticalDisc.TotalTime;

        private int _currentTrackNumber;
        private ushort _currentTrackIndex;
        private ulong _currentSector;

        private bool _hasHiddenTrack;
        private bool _quadChannel;
        private bool _isDataTrack;
        private bool _copyAllowed;
        private bool _trackHasEmphasis;

        #endregion

        #region SoundOutput Passthrough

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
            set => this.RaiseAndSetIfChanged(ref _volume, value);
        }

        private bool? _playing;
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
        private readonly OpticalDisc _opticalDisc;

        #endregion

        /// <summary>
        /// Create a new Player from a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        /// <param name="defaultVolume">Default volume between 0 and 100 to use when starting playback</param>
        public Player(string path, bool autoPlay = false, int defaultVolume = 100)
        {
            // Set the internal state for initialization
            Initialized = false;
            _soundOutput = new SoundOutput();
            _soundOutput.SetDeEmphasis(false);
            _opticalDisc = null;

            try
            {
                // Validate the image exists
                if(string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return;

                // Load the disc image to memory
                var image = new AaruFormat();
                var filter = new ZZZNoFilter();
                filter.Open(path);
                image.Open(filter);

                // Generate and instantiate the disc
                _opticalDisc = OpticalDiscFactory.GenerateFromImage(image, autoPlay);
            }
            catch
            {
                // All errors mean an invalid image in some way
                return;
            }

            // Add event handling for the optical disc
            if(_opticalDisc != null)
                _opticalDisc.PropertyChanged += OpticalDiscStateChanged;

            // Initialize the sound output
            _soundOutput.Init(_opticalDisc, autoPlay, defaultVolume);
            if(_soundOutput == null || !_soundOutput.Initialized)
                return;

            // Add event handling for the sound output
            _soundOutput.PropertyChanged += SoundOutputStateChanged;

            // Mark the player as ready
            Initialized = true;
            SetDiscInformation();
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
            else if(_soundOutput.Playing)
                return;

            _soundOutput.Play();
            _opticalDisc.SetTotalIndexes();
            Playing = true;
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
            else if(!_soundOutput.Playing)
                return;

            _soundOutput?.Stop();
            Playing = false;
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
            else if(!_soundOutput.Playing)
                return;

            _soundOutput?.Stop();
            _opticalDisc.LoadFirstTrack();
            Playing = null;
        }

        /// <summary>
        /// Move to the next playable track
        /// </summary>
        public void NextTrack()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            bool? wasPlaying = Playing;
            if(wasPlaying == true) Pause();

            _opticalDisc.NextTrack();
            if(_opticalDisc is CompactDisc compactDisc)
                _soundOutput.SetDeEmphasis(compactDisc.TrackHasEmphasis);

            SetDiscInformation();

            if(wasPlaying == true) Play();
        }

        /// <summary>
        /// Move to the previous playable track
        /// </summary>
        public void PreviousTrack()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            bool? wasPlaying = Playing;
            if(wasPlaying == true) Pause();

            _opticalDisc.PreviousTrack();
            if(_opticalDisc is CompactDisc compactDisc)
                _soundOutput.SetDeEmphasis(compactDisc.TrackHasEmphasis);

            SetDiscInformation();

            if(wasPlaying == true) Play();
        }

        /// <summary>
        /// Move to the next index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void NextIndex(bool changeTrack)
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            bool? wasPlaying = Playing;
            if(wasPlaying == true) Pause();

            _opticalDisc.NextIndex(changeTrack);
            if(_opticalDisc is CompactDisc compactDisc)
                _soundOutput.SetDeEmphasis(compactDisc.TrackHasEmphasis);

            SetDiscInformation();

            if(wasPlaying == true) Play();
        }

        /// <summary>
        /// Move to the previous index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void PreviousIndex(bool changeTrack)
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            bool? wasPlaying = Playing;
            if(wasPlaying == true) Pause();

            _opticalDisc.PreviousIndex(changeTrack);
            if(_opticalDisc is CompactDisc compactDisc)
                _soundOutput.SetDeEmphasis(compactDisc.TrackHasEmphasis);

            SetDiscInformation();

            if(wasPlaying == true) Play();
        }

        /// <summary>
        /// Fast-forward playback by 75 sectors, if possible
        /// </summary>
        public void FastForward()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            _opticalDisc.CurrentSector = Math.Min(_opticalDisc.TotalSectors, _opticalDisc.CurrentSector + 75);
            SetDiscInformation();
        }

        /// <summary>
        /// Rewind playback by 75 sectors, if possible
        /// </summary>
        public void Rewind()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            if(_opticalDisc.CurrentSector >= 75)
                _opticalDisc.CurrentSector -= 75;

            SetDiscInformation();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Get current sector time, accounting for offsets
        /// </summary>
        /// <returns>ulong representing the current sector time</returns>
        public ulong GetCurrentSectorTime()
        {
            ulong sectorTime = _opticalDisc.CurrentSector;
            if (_opticalDisc.SectionStartSector != 0)
                sectorTime -= _opticalDisc.SectionStartSector;
            else
                sectorTime += _opticalDisc.TimeOffset;

            return sectorTime;
        }

        /// <summary>
        /// Set de-emphasis status
        /// </summary>
        /// <param name="apply"></param>
        public void SetDeEmphasis(bool apply) => _soundOutput?.SetDeEmphasis(apply);

        /// <summary>
        /// Update the player from the current OpticalDisc
        /// </summary>
        private void OpticalDiscStateChanged(object sender, PropertyChangedEventArgs e) => SetDiscInformation();

        /// <summary>
        /// Update the player from the current SoundOutput
        /// </summary>
        private void SoundOutputStateChanged(object sender, PropertyChangedEventArgs e)
        {
            Playing = _soundOutput.Playing;
            ApplyDeEmphasis = _soundOutput.ApplyDeEmphasis;
            //Volume = _soundOutput.Volume;
        }

        /// <summary>
        /// Set all current disc information
        /// </summary>
        private void SetDiscInformation()
        {
            CurrentTrackNumber = _opticalDisc.CurrentTrackNumber;
            CurrentTrackIndex = _opticalDisc.CurrentTrackIndex;
            CurrentSector = _opticalDisc.CurrentSector;

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

        #endregion
    }
}