using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Avalonia.Threading;
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

        #region Playback Passthrough

        /// <summary>
        /// Currently selected disc
        /// </summary>
        public int CurrentDisc
        {
            get => _currentDisc;
            private set
            {
                int temp = value;
                if (temp < 0)
                    temp = _numberOfDiscs - 1;
                else if (temp >= _numberOfDiscs)
                    temp = 0;
                
                this.RaiseAndSetIfChanged(ref _currentDisc, temp);
            }
        }

        /// <summary>
        /// Indicates how to deal with multiple discs
        /// </summary>
        public DiscHandling DiscHandling
        {
            get => _discHandling;
            private set => this.RaiseAndSetIfChanged(ref _discHandling, value);
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
        /// Indicate if hidden tracks should be loaded
        /// </summary>
        public bool LoadHiddenTracks
        {
            get => _loadHiddenTracks;
            private set => this.RaiseAndSetIfChanged(ref _loadHiddenTracks, value);
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
        /// Indicates how tracks on different session should be handled
        /// </summary>
        public SessionHandling SessionHandling
        {
            get => _sessionHandling;
            private set => this.RaiseAndSetIfChanged(ref _sessionHandling, value);
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
        /// Should invoke playback mode changes
        /// </summary>
        private bool ShouldInvokePlaybackModes
        {
            get => _shouldInvokePlaybackModes;
            set => this.RaiseAndSetIfChanged(ref _shouldInvokePlaybackModes, value);
        }

        private bool _initialized;
        private int _numberOfDiscs;
        private int _currentDisc;
        private DiscHandling _discHandling;
        private bool _loadHiddenTracks;
        private DataPlayback _dataPlayback;
        private RepeatMode _repeatMode;
        private SessionHandling _sessionHandling;
        private bool _applyDeEmphasis;
        private bool _shouldInvokePlaybackModes;

        #endregion

        #region OpticalDisc Passthrough

        /// <summary>
        /// Path to the disc image
        /// </summary>
        public string ImagePath
        {
            get => _imagePath;
            private set => this.RaiseAndSetIfChanged(ref _imagePath, value);
        }

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
        public int TotalTracks => _opticalDiscs[CurrentDisc]?.TotalTracks ?? 0;

        /// <summary>
        /// Represents the total indices on the disc
        /// </summary>
        public int TotalIndexes => _opticalDiscs[CurrentDisc]?.TotalIndexes ?? 0;

        /// <summary>
        /// Total sectors in the image
        /// </summary>
        public ulong TotalSectors => _opticalDiscs[CurrentDisc]?.TotalSectors ?? 0;

        /// <summary>
        /// Represents the time adjustment offset for the disc
        /// </summary>
        public ulong TimeOffset => _opticalDiscs[CurrentDisc]?.TimeOffset ?? 0;

        /// <summary>
        /// Represents the total playing time for the disc
        /// </summary>
        public ulong TotalTime => _opticalDiscs[CurrentDisc]?.TotalTime ?? 0;

        private string _imagePath;
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
        /// Current playback volume
        /// </summary>
        public int Volume
        {
            get => _volume;
            private set => this.RaiseAndSetIfChanged(ref _volume, value);
        }

        private PlayerState _playerState;
        private int _volume;

        #endregion

        #region Private State Variables

        /// <summary>
        /// Sound output handling class
        /// </summary>
        private readonly SoundOutput _soundOutput;

        /// <summary>
        /// OpticalDisc objects
        /// </summary>
        private OpticalDiscBase[] _opticalDiscs;

        /// <summary>
        /// List of available tracks organized by disc
        /// </summary>
        private Dictionary<int, List<int>> _availableTrackList;

        /// <summary>
        /// Current track playback order
        /// </summary>
        private List<KeyValuePair<int, int>> _trackPlaybackOrder;

        /// <summary>
        /// Current track in playback order list
        /// </summary>
        private int _currentTrackInOrder;

        /// <summary>
        /// Last volume for mute toggling
        /// </summary>
        private int? _lastVolume = null;

        /// <summary>
        /// Filtering stage for audio output
        /// </summary>
        private FilterStage _filterStage;

        /// <summary>
        /// Current position in the sector for reading
        /// </summary>
        private int _currentSectorReadPosition = 0;

        /// <summary>
        /// Lock object for reading track data
        /// </summary>
        private readonly object _readingImage = new object();

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="numberOfDiscs">Number of discs to allow loading</param>
        /// <param name="defaultVolume">Default volume between 0 and 100 to use when starting playback</param>
        public Player(int numberOfDiscs, int defaultVolume)
        {
            Initialized = false;

            if (numberOfDiscs <= 0)
                numberOfDiscs = 1;

            _numberOfDiscs = numberOfDiscs;
            _opticalDiscs = new OpticalDiscBase[numberOfDiscs];
            _currentDisc = 0;

            _filterStage = new FilterStage();
            _soundOutput = new SoundOutput(defaultVolume);

            _availableTrackList = new Dictionary<int, List<int>>();
            for(int i = 0; i < _numberOfDiscs; i++)
            {
                _availableTrackList.Add(i, new List<int>());
            }

            _trackPlaybackOrder = new List<KeyValuePair<int, int>>();
            _currentTrackInOrder = 0;

            PropertyChanged += HandlePlaybackModes;
        }

        /// <summary>
        /// Initializes player from a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="playerOptions">Options to pass to the player</param>
        /// <param name="opticalDiscOptions">Options to pass to the optical disc factory</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(string path, PlayerOptions playerOptions, OpticalDiscOptions opticalDiscOptions, bool autoPlay)
        {
            // Reset initialization
            Initialized = false;

            // Set player options
            DataPlayback = playerOptions.DataPlayback;
            DiscHandling = playerOptions.DiscHandling;
            LoadHiddenTracks = playerOptions.LoadHiddenTracks;
            RepeatMode = playerOptions.RepeatMode;
            SessionHandling = playerOptions.SessionHandling;

            // Initalize the disc
            _opticalDiscs[CurrentDisc] = OpticalDiscFactory.GenerateFromPath(path, opticalDiscOptions, autoPlay);
            if(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized)
                return;

            // Add event handling for the optical disc
            _opticalDiscs[CurrentDisc].PropertyChanged += OpticalDiscStateChanged;

            // Setup de-emphasis filters
            _filterStage.SetupFilters();

            // Initialize the sound output
            _soundOutput.Init(ProviderRead, autoPlay);
            if(_soundOutput == null || !_soundOutput.Initialized)
                return;

            // Add event handling for the sound output
            _soundOutput.PropertyChanged += SoundOutputStateChanged;

            // Load in the track list for the current disc
            LoadTrackList();

            // Mark the player as ready
            Initialized = true;

            // Force a refresh of the state information
            OpticalDiscStateChanged(this, null);
            SoundOutputStateChanged(this, null);
        }

        /// <summary>
        /// Load the track list into the track dictionary for the current disc
        /// </summary>
        private void LoadTrackList()
        {
            OpticalDiscBase opticalDisc = _opticalDiscs[CurrentDisc];

            // If the disc exists, add it to the dictionary
            if(_opticalDiscs[CurrentDisc] != null)
            {
                if(opticalDisc is CompactDisc compactDisc)
                    _availableTrackList[CurrentDisc] = compactDisc.Tracks.Select(t => (int)t.TrackSequence).OrderBy(s => s).ToList();
                else
                    _availableTrackList[CurrentDisc] = Enumerable.Range(1, opticalDisc.TotalTracks).ToList();
            }

            // If the disc is null, then make sure it's removed
            else
            {
                _availableTrackList[CurrentDisc] = new List<int>();
            }

            // Repopulate the playback order
            _trackPlaybackOrder = new List<KeyValuePair<int, int>>();
            if(DiscHandling == DiscHandling.SingleDisc)
            {
                List<int> availableTracks = _availableTrackList[CurrentDisc];
                if(availableTracks != null && availableTracks.Count > 0)
                    _trackPlaybackOrder.AddRange(availableTracks.Select(t => new KeyValuePair<int, int>(CurrentDisc, t)));
            }
            else if(DiscHandling == DiscHandling.MultiDisc)
            {
                for(int i = 0; i < _numberOfDiscs; i++)
                {
                    List<int> availableTracks = _availableTrackList[i];
                    if(availableTracks != null && availableTracks.Count > 0)
                        _trackPlaybackOrder.AddRange(availableTracks.Select(t => new KeyValuePair<int, int>(i, t)));
                }
            }

            // Try to get back to the last loaded track
            int currentFoundTrack = 0;
            if(_trackPlaybackOrder == null || _trackPlaybackOrder.Count == 0)
            {
                currentFoundTrack = 0;
            }
            else if(_trackPlaybackOrder.Any(kvp => kvp.Key == CurrentDisc))
            {
                currentFoundTrack = _trackPlaybackOrder.FindIndex(kvp => kvp.Key == CurrentDisc && kvp.Value == CurrentTrackNumber);
                if(currentFoundTrack == -1)
                    currentFoundTrack = _trackPlaybackOrder.Where(kvp => kvp.Key == CurrentDisc).Min(kvp => kvp.Value);
            }
            else
            {
                int lowestDiscNumber = _trackPlaybackOrder.Min(kvp => kvp.Key);
                currentFoundTrack = _trackPlaybackOrder.Where(kvp => kvp.Key == lowestDiscNumber).Min(kvp => kvp.Value);
            }

            _currentTrackInOrder = currentFoundTrack;
        }

        #region Playback (UI)

        /// <summary>
        /// Begin playback
        /// </summary>
        public void Play()
        {
            if(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized)
                return;
            else if(_soundOutput == null)
                return;
            else if(_soundOutput.PlayerState != PlayerState.Paused && _soundOutput.PlayerState != PlayerState.Stopped)
                return;

            _soundOutput.Play();
            _opticalDiscs[CurrentDisc].SetTotalIndexes();
            PlayerState = PlayerState.Playing;
        }

        /// <summary>
        /// Pause current playback
        /// </summary>
        public void Pause()
        {
            if(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized)
                return;
            else if(_soundOutput == null)
                return;
            else if(_soundOutput.PlayerState != PlayerState.Playing)
                return;

            _soundOutput.Pause();
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
        /// Shuffle the current track order
        /// </summary>
        public void ShuffleTracks()
        {
            List<KeyValuePair<int, int>> newPlaybackOrder = new List<KeyValuePair<int, int>>();
            Random random = new Random();

            while(_trackPlaybackOrder.Count > 0)
            {
                int next = random.Next(0, _trackPlaybackOrder.Count - 1);
                newPlaybackOrder.Add(_trackPlaybackOrder[next]);
                _trackPlaybackOrder.RemoveAt(next);
            }

            _trackPlaybackOrder = newPlaybackOrder;
        }

        /// <summary>
        /// Stop current playback
        /// </summary>
        public void Stop()
        {
            if(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized)
                return;
            else if(_soundOutput == null)
                return;
            else if(_soundOutput.PlayerState != PlayerState.Playing && _soundOutput.PlayerState != PlayerState.Paused)
               return;

            _soundOutput.Stop();
            CurrentTrackNumber = 0;
            SelectTrack(1);
            PlayerState = PlayerState.Stopped;
        }

        /// <summary>
        /// Eject the currently loaded disc
        /// </summary>
        public void Eject()
        {
            if(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized)
                return;
            else if(_soundOutput == null)
                return;

            Stop();
            _opticalDiscs[CurrentDisc] = null;
            LoadTrackList();

            // Only de-initialize the player if all discs are ejected
            if(_opticalDiscs.All(d => d == null || !d.Initialized))
            {
                _soundOutput.Eject();
                PlayerState = PlayerState.NoDisc;
                Initialized = false;
            }
            else
            {
                PlayerState = PlayerState.Stopped;
            }
        }

        /// <summary>
        /// Move to the next disc
        /// </summary>
        public void NextDisc() => SelectDisc(CurrentDisc + 1);

        /// <summary>
        /// Move to the previous disc
        /// </summary>
        public void PreviousDisc() => SelectDisc(CurrentDisc - 1);

        /// <summary>
        /// Move to the next playable track
        /// </summary>
        /// <remarks>TODO: This should follow the track playback order</remarks>
        public void NextTrack() => SelectTrack(CurrentTrackNumber + 1);

        /// <summary>
        /// Move to the previous playable track
        /// </summary>
        /// <remarks>TODO: This should follow the track playback order</remarks>
        public void PreviousTrack() => SelectTrack(CurrentTrackNumber - 1);

        /// <summary>
        /// Move to the next index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void NextIndex(bool changeTrack) => SelectIndex((ushort)(CurrentTrackIndex + 1), changeTrack);

        /// <summary>
        /// Move to the previous index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void PreviousIndex(bool changeTrack) => SelectIndex((ushort)(CurrentTrackIndex - 1), changeTrack);

        /// <summary>
        /// Fast-forward playback by 75 sectors
        /// </summary>
        public void FastForward()
        {
            if(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized)
                return;

            _opticalDiscs[CurrentDisc].SetCurrentSector(_opticalDiscs[CurrentDisc].CurrentSector + 75);
        }

        /// <summary>
        /// Rewind playback by 75 sectors
        /// </summary>
        public void Rewind()
        {
            if(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized)
                return;

            _opticalDiscs[CurrentDisc].SetCurrentSector(_opticalDiscs[CurrentDisc].CurrentSector - 75);
        }

        #endregion

        #region Playback (Internal)

        /// <summary>
        /// Fill the current byte buffer with playable data
        /// </summary>
        /// <param name="buffer">Buffer to load data into</param>
        /// <param name="offset">Offset in the buffer to load at</param>
        /// <param name="count">Number of bytes to load</param>
        /// <returns>Number of bytes read</returns>
        public int ProviderRead(byte[] buffer, int offset, int count)
        {
            // If we have an unreadable amount
            if (count <= 0)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            // If we have an unreadable track, just return
            if(_opticalDiscs[CurrentDisc].BytesPerSector <= 0)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            // Determine how many sectors we can read
            DetermineReadAmount(count, out ulong sectorsToRead, out ulong zeroSectorsAmount);

            // Get data to return
            byte[] audioDataSegment = ReadData(count, sectorsToRead, zeroSectorsAmount);
            if(audioDataSegment == null)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            // Write out the audio data to the buffer
            Array.Copy(audioDataSegment, 0, buffer, offset, count);

            // Set the read position in the sector for easier access
            _currentSectorReadPosition += count;
            if(_currentSectorReadPosition >= _opticalDiscs[CurrentDisc].BytesPerSector)
            {
                ulong newSectorValue = _opticalDiscs[CurrentDisc].CurrentSector + (ulong)(_currentSectorReadPosition / _opticalDiscs[CurrentDisc].BytesPerSector);
                if(newSectorValue >= _opticalDiscs[CurrentDisc].TotalSectors)
                {
                    ShouldInvokePlaybackModes = true;
                }
                else if(RepeatMode == RepeatMode.Single && _opticalDiscs[CurrentDisc] is CompactDisc compactDisc)
                {
                    ulong trackEndSector = compactDisc.GetTrack(CurrentTrackNumber).TrackEndSector;
                    if (newSectorValue > trackEndSector)
                    {
                        ShouldInvokePlaybackModes = true;
                    }
                    else
                    {
                        _opticalDiscs[CurrentDisc].SetCurrentSector(newSectorValue);
                        _currentSectorReadPosition %= _opticalDiscs[CurrentDisc].BytesPerSector;
                    }
                }
                else
                {
                    _opticalDiscs[CurrentDisc].SetCurrentSector(newSectorValue);
                    _currentSectorReadPosition %= _opticalDiscs[CurrentDisc].BytesPerSector;
                }
            }

            return count;
        }

        /// <summary>
        /// Select a disc by number
        /// </summary>
        /// <param name="discNumber">Disc number to attempt to load</param>
        /// <remarks>TODO: This needs to reset the pointer in the track playback order</remarks>
        public void SelectDisc(int discNumber)
        {
            PlayerState wasPlaying = PlayerState;
            if (wasPlaying == PlayerState.Playing)
                Stop();

            _currentSectorReadPosition = 0;

            CurrentDisc = discNumber;
            if (_opticalDiscs[CurrentDisc] != null && _opticalDiscs[CurrentDisc].Initialized)
            {
                Initialized = true;
                OpticalDiscStateChanged(this, null);
                SoundOutputStateChanged(this, null);

                if(wasPlaying == PlayerState.Playing)
                    Play();
            }
            else
            {
                PlayerState = PlayerState.NoDisc;
                Initialized = false;
            }
        }

        /// <summary>
        /// Select a disc by number
        /// </summary>
        /// <param name="index">Track index to attempt to load</param>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void SelectIndex(ushort index, bool changeTrack)
        {
            PlayerState wasPlaying = PlayerState;
            if (wasPlaying == PlayerState.Playing)
                Pause();

            // CompactDisc needs special handling of track wraparound
            if (_opticalDiscs[CurrentDisc] is CompactDisc compactDisc)
            {
                // Cache the current track for easy access
                Track track = compactDisc.GetTrack(CurrentTrackNumber);
                if(track == null)
                    return;

                // Check if we're incrementing or decrementing the track
                bool increment = (short)index >= (short)CurrentTrackIndex;

                // If the index is greater than the highest index, change tracks if needed
                if((short)index > (short)track.Indexes.Keys.Max())
                {
                    if(changeTrack)
                        NextTrack();
                }

                // If the index is less than the lowest index, change tracks if needed
                else if((short)index < (short)track.Indexes.Keys.Min())
                {
                    if(changeTrack)
                    {
                        PreviousTrack();
                        compactDisc.SetCurrentSector((ulong)compactDisc.GetTrack(CurrentTrackNumber).Indexes.Values.Max());
                    }
                }

                // If the next index has an invalid offset, change tracks if needed
                else if(track.Indexes[index] < 0)
                {
                    if(changeTrack)
                    {
                        if(increment)
                        {
                            NextTrack();
                        }
                        else
                        {
                            PreviousTrack();
                            compactDisc.SetCurrentSector((ulong)compactDisc.GetTrack(CurrentTrackNumber).Indexes.Values.Min());
                        }
                    }
                }

                // Otherwise, just move to the next index
                else
                {
                    compactDisc.SetCurrentSector((ulong)track.Indexes[index]);
                }
            }
            else
            {
                // TODO: Fill in for non-CD media
            }

            if(wasPlaying == PlayerState.Playing)
                Play();
        }

        /// <summary>
        /// Select a track by number
        /// </summary>
        /// <param name="trackNumber">Track number to attempt to load</param>
        /// <returns>True if the track was changed, false otherwise</returns>
        /// <remarks>TODO: This needs to reset the pointer in the track playback order</remarks>
        public bool SelectTrack(int trackNumber)
        {
            if(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized)
                return false;

            PlayerState wasPlaying = PlayerState;
            if(wasPlaying == PlayerState.Playing)
                Pause();

            // CompactDisc needs special handling of track wraparound
            if (_opticalDiscs[CurrentDisc] is CompactDisc compactDisc)
            {
                // Cache the value and the current track number
                int cachedValue = trackNumber;
                int cachedTrackNumber;

                // Take care of disc switching first
                if(DiscHandling == DiscHandling.MultiDisc)
                {
                    if(trackNumber > (int)compactDisc.Tracks.Max(t => t.TrackSequence))
                    {
                        do
                        {
                            NextDisc();
                        }
                        while(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized);

                        if(wasPlaying == PlayerState.Playing)
                            Play();

                        return true;
                    }
                    else if((trackNumber < 1 && !LoadHiddenTracks) || (trackNumber < (int)compactDisc.Tracks.Min(t => t.TrackSequence)))
                    {
                        do
                        {
                            PreviousDisc();
                        }
                        while(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized);

                        SelectTrack(-1);
                        if(wasPlaying == PlayerState.Playing)
                            Play();

                        return true;
                    }
                }

                // If we have an invalid current track number, set it to the minimum
                if(!compactDisc.Tracks.Any(t => t.TrackSequence == _currentTrackNumber))
                    _currentTrackNumber = (int)compactDisc.Tracks.Min(t => t.TrackSequence);

                // Check if we're incrementing or decrementing the track
                bool increment = cachedValue >= _currentTrackNumber;
                
                do
                {
                    // If we're over the last track, wrap around
                    if(cachedValue > compactDisc.Tracks.Max(t => t.TrackSequence))
                    {
                        cachedValue = (int)compactDisc.Tracks.Min(t => t.TrackSequence);
                        if(cachedValue == 0 && !LoadHiddenTracks)
                            cachedValue++;
                    }

                    // If we're under the first track and we're not loading hidden tracks, wrap around
                    else if(cachedValue < 1 && !LoadHiddenTracks)
                    {
                        cachedValue = (int)compactDisc.Tracks.Max(t => t.TrackSequence);
                    }

                    // If we're under the first valid track, wrap around
                    else if(cachedValue < compactDisc.Tracks.Min(t => t.TrackSequence))
                    {
                        cachedValue = (int)compactDisc.Tracks.Max(t => t.TrackSequence);
                    }

                    cachedTrackNumber = cachedValue;

                    // Cache the current track for easy access
                    Track track = compactDisc.GetTrack(cachedTrackNumber);
                    if(track == null)
                        return false;

                    // If the track is playable, just return
                    if((track.TrackType == TrackType.Audio || DataPlayback != DataPlayback.Skip)
                        && (SessionHandling == SessionHandling.AllSessions || track.TrackSession == 1))
                    {
                        break;
                    }

                    // If we're not playing the track, skip
                    if(increment)
                        cachedValue++;
                    else
                        cachedValue--;
                }
                while(cachedValue != _currentTrackNumber);

                // Load the now-valid value
                compactDisc.LoadTrack(cachedTrackNumber);
                ApplyDeEmphasis = compactDisc.TrackHasEmphasis;
            }
            else
            {
                if(trackNumber >= _opticalDiscs[CurrentDisc].TotalTracks)
                {
                    if(DiscHandling == DiscHandling.MultiDisc)
                    {
                        do
                        {
                            NextDisc();
                        }
                        while(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized);
                    }

                    trackNumber = 1;
                }
                else if(trackNumber < 1)
                {
                    if(DiscHandling == DiscHandling.MultiDisc)
                    {
                        do
                        {
                            PreviousDisc();
                        }
                        while(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized);
                        trackNumber = 1;
                    }

                    trackNumber = _opticalDiscs[CurrentDisc].TotalTracks - 1;
                }
                
                _opticalDiscs[CurrentDisc].LoadTrack(trackNumber);
            }

            if(wasPlaying == PlayerState.Playing)
                Play();

            return true;
        }

        /// <summary>
        /// Select a track in the relative track list by number
        /// </summary>
        /// <param name="relativeTrackNumber">Relative track number to attempt to load</param>
        public void SelectRelativeTrack(int relativeTrackNumber)
        {
            if(_trackPlaybackOrder == null || _trackPlaybackOrder.Count == 0)
                return;

            PlayerState wasPlaying = PlayerState;
            if(wasPlaying == PlayerState.Playing)
                Pause();

            if(relativeTrackNumber < 0)
                relativeTrackNumber = _trackPlaybackOrder.Count - 1;
            else if(relativeTrackNumber >= _trackPlaybackOrder.Count)
                relativeTrackNumber = 0;

            do
            {
                _currentTrackInOrder = relativeTrackNumber;
                KeyValuePair<int, int> discTrackPair = _trackPlaybackOrder[relativeTrackNumber];
                SelectDisc(discTrackPair.Key);
                if(SelectTrack(discTrackPair.Value))
                    break;
            }
            while(true);

            if(wasPlaying == PlayerState.Playing)
                Play();
        }

        /// <summary>
        /// Determine the number of real and zero sectors to read
        /// </summary>
        /// <param name="count">Number of requested bytes to read</param>
        /// <param name="sectorsToRead">Number of sectors to read</param>
        /// <param name="zeroSectorsAmount">Number of zeroed sectors to concatenate</param>
        private void DetermineReadAmount(int count, out ulong sectorsToRead, out ulong zeroSectorsAmount)
        {
            // Attempt to read 10 more sectors than requested
            sectorsToRead = ((ulong)count / (ulong)_opticalDiscs[CurrentDisc].BytesPerSector) + 10;
            zeroSectorsAmount = 0;

            // Avoid overreads by padding with 0-byte data at the end
            if(_opticalDiscs[CurrentDisc].CurrentSector + sectorsToRead > _opticalDiscs[CurrentDisc].TotalSectors)
            {
                ulong oldSectorsToRead = sectorsToRead;
                sectorsToRead = _opticalDiscs[CurrentDisc].TotalSectors - _opticalDiscs[CurrentDisc].CurrentSector;

                int tempZeroSectorCount = (int)(oldSectorsToRead - sectorsToRead);
                zeroSectorsAmount = (ulong)(tempZeroSectorCount < 0 ? 0 : tempZeroSectorCount);
            }
        }

        /// <summary>
        /// Read the requested amount of data from an input
        /// </summary>
        /// <param name="count">Number of bytes to load</param>
        /// <param name="sectorsToRead">Number of sectors to read</param>
        /// <param name="zeroSectorsAmount">Number of zeroed sectors to concatenate</param>
        /// <returns>The requested amount of data, if possible</returns>
        private byte[] ReadData(int count, ulong sectorsToRead, ulong zeroSectorsAmount)
        {
            // If the amount of zeroes being asked for is the same as the sectors, return null
            if (sectorsToRead == zeroSectorsAmount)
                return null;

            // Create padding data for overreads
            byte[] zeroSectors = new byte[(int)zeroSectorsAmount * _opticalDiscs[CurrentDisc].BytesPerSector];
            byte[] audioData;

            // Attempt to read the required number of sectors
            var readSectorTask = Task.Run(() =>
            {
                lock(_readingImage)
                {
                    try
                    {
                        if(_opticalDiscs[CurrentDisc] is CompactDisc compactDisc)
                            return compactDisc.ReadSectors((uint)sectorsToRead, DataPlayback).Concat(zeroSectors).ToArray();
                        else
                            return _opticalDiscs[CurrentDisc].ReadSectors((uint)sectorsToRead).Concat(zeroSectors).ToArray();
                    }
                    catch { }

                    return zeroSectors;
                }
            });

            // Wait 100ms at longest for the read to occur
            if(readSectorTask.Wait(TimeSpan.FromMilliseconds(100)))
                audioData = readSectorTask.Result;
            else
                return null;

            // Load only the requested audio segment
            byte[] audioDataSegment = new byte[count];
            int copyAmount = Math.Min(count, audioData.Length - _currentSectorReadPosition);
            if(Math.Max(0, copyAmount) == 0)
                return null;

            Array.Copy(audioData, _currentSectorReadPosition, audioDataSegment, 0, copyAmount);

            // Apply de-emphasis filtering, only if enabled
            if(ApplyDeEmphasis)
                _filterStage.ProcessAudioData(audioDataSegment);

            return audioDataSegment;
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
        /// <param name="applyDeEmphasis"></param>
        private void SetDeEmphasis(bool applyDeEmphasis) => ApplyDeEmphasis = applyDeEmphasis;

        #endregion

        #region Extraction

        /// <summary>
        /// Extract a single track from the image to WAV
        /// </summary>
        /// <param name="trackNumber"></param>
        /// <param name="outputDirectory">Output path to write data to</param>
        public void ExtractSingleTrackToWav(uint trackNumber, string outputDirectory)
        {
            OpticalDiscBase opticalDisc = _opticalDiscs[CurrentDisc];
            if(opticalDisc == null || !opticalDisc.Initialized)
                return;

            if(opticalDisc is CompactDisc compactDisc)
            {
                // Get the track with that value, if possible
                Track track = compactDisc.Tracks.FirstOrDefault(t => t.TrackSequence == trackNumber);

                // If the track isn't valid, we can't do anything
                if(track == null || !(DataPlayback != DataPlayback.Skip || track.TrackType == TrackType.Audio))
                    return;

                // Extract the track if it's valid
                compactDisc.ExtractTrackToWav(trackNumber, outputDirectory, DataPlayback);
            }
            else
            {
                opticalDisc?.ExtractTrackToWav(trackNumber, outputDirectory);
            }
        }

        /// <summary>
        /// Extract all tracks from the image to WAV
        /// </summary>
        /// <param name="outputDirectory">Output path to write data to</param>
        public void ExtractAllTracksToWav(string outputDirectory)
        {
            OpticalDiscBase opticalDisc = _opticalDiscs[CurrentDisc];
            if(opticalDisc == null || !opticalDisc.Initialized)
                return;

            if(opticalDisc is CompactDisc compactDisc)
            {
                foreach(Track track in compactDisc.Tracks)
                {
                    ExtractSingleTrackToWav(track.TrackSequence, outputDirectory);
                }
            }
            else
            {
                for(uint i = 0; i < opticalDisc.TotalTracks; i++)
                {
                    ExtractSingleTrackToWav(i, outputDirectory);
                }
            }
        }

        #endregion

        #region Setters

        /// <summary>
        /// Set data playback method [CompactDisc only]
        /// </summary>
        /// <param name="dataPlayback">New playback value</param>
        public void SetDataPlayback(DataPlayback dataPlayback) => DataPlayback = dataPlayback;

        /// <summary>
        /// Set disc handling method
        /// </summary>
        /// <param name="discHandling">New playback value</param>
        public void SetDiscHandling(DiscHandling discHandling)
        {
            DiscHandling = discHandling;
            LoadTrackList();
        }

        /// <summary>
        /// Set the value for loading hidden tracks [CompactDisc only]
        /// </summary>
        /// <param name="loadHiddenTracks">True to enable loading hidden tracks, false otherwise</param>
        public void SetLoadHiddenTracks(bool loadHiddenTracks) => LoadHiddenTracks = loadHiddenTracks;

        /// <summary>
        /// Set repeat mode
        /// </summary>
        /// <param name="repeatMode">New repeat mode value</param>
        public void SetRepeatMode(RepeatMode repeatMode) => RepeatMode = repeatMode;

        /// <summary>
        /// Set the value for session handling [CompactDisc only]
        /// </summary>
        /// <param name="sessionHandling">New session handling value</param>
        public void SetSessionHandling(SessionHandling sessionHandling) => SessionHandling = sessionHandling;

        #endregion

        #region State Change Event Handlers

        /// <summary>
        /// Handle special playback modes if we get flagged to
        /// </summary>
        private async void HandlePlaybackModes(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName != nameof(ShouldInvokePlaybackModes))
                return;

            // Always stop before doing anything else
            PlayerState wasPlaying = PlayerState;
            await Dispatcher.UIThread.InvokeAsync(Stop);

            switch(RepeatMode)
            {
                case RepeatMode.None:
                    // No-op
                    break;
                case RepeatMode.Single:
                    _opticalDiscs[CurrentDisc].LoadTrack(CurrentTrackNumber);
                    break;
                case RepeatMode.All when DiscHandling == DiscHandling.SingleDisc:
                    SelectTrack(1);
                    break;
                case RepeatMode.All when DiscHandling == DiscHandling.MultiDisc:
                    do
                    {
                        NextDisc();
                    }
                    while(_opticalDiscs[CurrentDisc] == null || !_opticalDiscs[CurrentDisc].Initialized);

                    SelectTrack(1);
                    break;
            }

            _shouldInvokePlaybackModes = false;
            if(wasPlaying == PlayerState.Playing)
                await Dispatcher.UIThread.InvokeAsync(Play);
        }

        /// <summary>
        /// Update the player from the current OpticalDisc
        /// </summary>
        private void OpticalDiscStateChanged(object sender, PropertyChangedEventArgs e)
        {
            ImagePath = _opticalDiscs[CurrentDisc].ImagePath;
            CurrentTrackNumber = _opticalDiscs[CurrentDisc].CurrentTrackNumber;
            CurrentTrackIndex = _opticalDiscs[CurrentDisc].CurrentTrackIndex;
            CurrentSector = _opticalDiscs[CurrentDisc].CurrentSector;
            SectionStartSector = _opticalDiscs[CurrentDisc].SectionStartSector;

            HiddenTrack = TimeOffset > 150;

            if(_opticalDiscs[CurrentDisc] is CompactDisc compactDisc)
            {
                QuadChannel = compactDisc.QuadChannel;
                IsDataTrack = compactDisc.IsDataTrack;
                CopyAllowed = compactDisc.CopyAllowed;
                TrackHasEmphasis = compactDisc.TrackHasEmphasis;
            }
            else
            {
                QuadChannel = false;
                IsDataTrack = _opticalDiscs[CurrentDisc].TrackType != TrackType.Audio;
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
            Volume = _soundOutput.Volume;
        }

        #endregion
    }
}