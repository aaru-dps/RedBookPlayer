using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decoders.CD;
using Aaru.Helpers;
using ReactiveUI;
using static Aaru.Decoders.CD.FullTOC;

namespace RedBookPlayer.Common.Discs
{
    public class CompactDisc : OpticalDisc, IReactiveObject
    {
        #region Public Fields

        /// <inheritdoc/>
        public override int CurrentTrackNumber
        {
            get => _currentTrackNumber;
            protected set
            {
                // Unset image means we can't do anything
                if(_image == null)
                    return;

                // Cache the value and the current track number
                int cachedValue = value;
                int cachedTrackNumber;

                // Check if we're incrementing or decrementing the track
                bool increment = cachedValue >= _currentTrackNumber;

                do
                {
                    // If we're over the last track, wrap around
                    if(cachedValue > _image.Tracks.Max(t => t.TrackSequence))
                    {
                        cachedValue = (int)_image.Tracks.Min(t => t.TrackSequence);
                        if(cachedValue == 0 && !_loadHiddenTracks)
                            cachedValue++;
                    }

                    // If we're under the first track and we're not loading hidden tracks, wrap around
                    else if(cachedValue < 1 && !_loadHiddenTracks)
                    {
                        cachedValue = (int)_image.Tracks.Max(t => t.TrackSequence);
                    }

                    // If we're under the first valid track, wrap around
                    else if(cachedValue < _image.Tracks.Min(t => t.TrackSequence))
                    {
                        cachedValue = (int)_image.Tracks.Max(t => t.TrackSequence);
                    }

                    cachedTrackNumber = cachedValue;

                    // Cache the current track for easy access
                    Track track = GetTrack(cachedTrackNumber);

                    // Set track flags from subchannel data, if possible
                    SetTrackFlags(track);

                    // If the track is playable, just return
                    if(TrackType == TrackType.Audio || _loadDataTracks)
                        break;

                    // If we're not playing the track, skip
                    if(increment)
                        cachedValue++;
                    else
                        cachedValue--;
                }
                while(cachedValue != _currentTrackNumber);

                this.RaiseAndSetIfChanged(ref _currentTrackNumber, cachedTrackNumber);

                TotalIndexes = GetTrack(_currentTrackNumber).Indexes.Keys.Max();
                CurrentTrackIndex = GetTrack(_currentTrackNumber).Indexes.Keys.Min();
            }
        }

        /// <inheritdoc/>
        public override ushort CurrentTrackIndex
        {
            get => _currentTrackIndex;
            protected set
            {
                // Unset image means we can't do anything
                if(_image == null)
                    return;

                // Cache the current track for easy access
                Track track = GetTrack(CurrentTrackNumber);

                // Ensure that the value is valid, wrapping around if necessary
                ushort fixedValue = value;
                if(value > track.Indexes.Keys.Max())
                    fixedValue = track.Indexes.Keys.Min();
                else if(value < track.Indexes.Keys.Min())
                    fixedValue = track.Indexes.Keys.Max();

                this.RaiseAndSetIfChanged(ref _currentTrackIndex, fixedValue);

                // Set new index-specific data
                SectionStartSector = (ulong)track.Indexes[CurrentTrackIndex];
                TotalTime = track.TrackEndSector - track.TrackStartSector;
            }
        }

        /// <inheritdoc/>
        public override ulong CurrentSector
        {
            get => _currentSector;
            protected set
            {
                // Unset image means we can't do anything
                if(_image == null)
                    return;

                // Cache the current track for easy access
                Track track = GetTrack(CurrentTrackNumber);

                this.RaiseAndSetIfChanged(ref _currentSector, value);

                if((CurrentTrackNumber < _image.Tracks.Count - 1 && CurrentSector >= GetTrack(CurrentTrackNumber + 1).TrackStartSector)
                    || (CurrentTrackNumber > 0 && CurrentSector < track.TrackStartSector))
                {
                    foreach(Track trackData in _image.Tracks.ToArray().Reverse())
                    {
                        if(CurrentSector >= trackData.TrackStartSector)
                        {
                            CurrentTrackNumber = (int)trackData.TrackSequence;
                            break;
                        }
                    }
                }

                foreach((ushort key, int i) in track.Indexes.Reverse())
                {
                    if((int)CurrentSector >= i)
                    {
                        CurrentTrackIndex = key;
                        return;
                    }
                }

                CurrentTrackIndex = 0;
            }
        }

        /// <inheritdoc/>
        public override int BytesPerSector => GetTrack(CurrentTrackNumber).TrackRawBytesPerSector;

        /// <summary>
        /// Represents the 4CH flag
        /// </summary>
        public bool QuadChannel
        {
            get => _quadChannel;
            private set => this.RaiseAndSetIfChanged(ref _quadChannel, value);
        }

        /// <summary>
        /// Represents the DATA flag
        /// </summary>
        public bool IsDataTrack
        {
            get => _isDataTrack;
            private set => this.RaiseAndSetIfChanged(ref _isDataTrack, value);
        }

        /// <summary>
        /// Represents the DCP flag
        /// </summary>
        public bool CopyAllowed
        {
            get => _copyAllowed;
            private set => this.RaiseAndSetIfChanged(ref _copyAllowed, value);
        }

        /// <summary>
        /// Represents the PRE flag
        /// </summary>
        public bool TrackHasEmphasis
        {
            get => _trackHasEmphasis;
            private set => this.RaiseAndSetIfChanged(ref _trackHasEmphasis, value);
        }

        private bool _quadChannel;
        private bool _isDataTrack;
        private bool _copyAllowed;
        private bool _trackHasEmphasis;

        #endregion

        #region Private State Variables

        /// <summary>
        /// Current track number
        /// </summary>
        private int _currentTrackNumber = 0;

        /// <summary>
        /// Current track index
        /// </summary>
        private ushort _currentTrackIndex = 0;

        /// <summary>
        /// Current sector number
        /// </summary>
        private ulong _currentSector = 0;

        /// <summary>
        /// Indicate if a TOC should be generated if missing
        /// </summary>
        private readonly bool _generateMissingToc = false;

        /// <summary>
        /// Indicate if data tracks should be loaded
        /// </summary>
        private bool _loadDataTracks = false;

        /// <summary>
        /// Indicate if hidden tracks should be loaded
        /// </summary>
        private bool _loadHiddenTracks = false;

        /// <summary>
        /// Current disc table of contents
        /// </summary>
        private CDFullTOC _toc;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="generateMissingToc">Generate a TOC if the disc is missing one</param>
        /// <param name="loadHiddenTracks">Load hidden tracks for playback</param>
        /// <param name="loadDataTracks">Load data tracks for playback</param>
        public CompactDisc(bool generateMissingToc, bool loadHiddenTracks, bool loadDataTracks)
        {
            _generateMissingToc = generateMissingToc;
            _loadHiddenTracks = loadHiddenTracks;
            _loadDataTracks = loadDataTracks;
        }

        /// <inheritdoc/>
        public override void Init(IOpticalMediaImage image, bool autoPlay)
        {
            // If the image is null, we can't do anything
            if(image == null)
                return;

            // Set the current disc image
            _image = image;

            // Attempt to load the TOC
            if(!LoadTOC())
                return;

            // Load the first track
            LoadFirstTrack();

            // Reset total indexes if not in autoplay
            if(!autoPlay)
                TotalIndexes = 0;

            // Set the internal disc state
            TotalTracks = (int)_image.Tracks.Max(t => t.TrackSequence);
            TrackDataDescriptor firstTrack = _toc.TrackDescriptors.First(d => d.ADR == 1 && d.POINT == 1);
            TimeOffset = (ulong)((firstTrack.PMIN * 60 * 75) + (firstTrack.PSEC * 75) + firstTrack.PFRAME);
            TotalTime = TimeOffset + _image.Tracks.Last().TrackEndSector;

            // Mark the disc as ready
            Initialized = true;
        }

        #region Seeking

        /// <inheritdoc/>
        public override void NextTrack()
        {
            if(_image == null)
                return;

            CurrentTrackNumber++;
            LoadTrack(CurrentTrackNumber);
        }

        /// <inheritdoc/>
        public override void PreviousTrack()
        {
            if(_image == null)
                return;

            CurrentTrackNumber--;
            LoadTrack(CurrentTrackNumber);
        }

        /// <inheritdoc/>
        public override bool NextIndex(bool changeTrack)
        {
            if(_image == null)
                return false;

            // Cache the current track for easy access
            Track track = GetTrack(CurrentTrackNumber);

            // If the index is greater than the highest index, change tracks if needed
            if(CurrentTrackIndex + 1 > track.Indexes.Keys.Max())
            {
                if(changeTrack)
                {
                    NextTrack();
                    CurrentSector = (ulong)GetTrack(CurrentTrackNumber).Indexes.Values.Min();
                    return true;
                }
            }

            // If the next index has an invalid offset, change tracks if needed
            else if(track.Indexes[(ushort)(CurrentTrackIndex + 1)] < 0)
            {
                if(changeTrack)
                {
                    NextTrack();
                    CurrentSector = (ulong)GetTrack(CurrentTrackNumber).Indexes.Values.Min();
                    return true;
                }
            }

            // Otherwise, just move to the next index
            else
            {
                CurrentSector = (ulong)track.Indexes[++CurrentTrackIndex];
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool PreviousIndex(bool changeTrack)
        {
            if(_image == null)
                return false;

            // Cache the current track for easy access
            Track track = GetTrack(CurrentTrackNumber);

            // If the index is less than the lowest index, change tracks if needed
            if(CurrentTrackIndex - 1 < track.Indexes.Keys.Min())
            {
                if(changeTrack)
                {
                    PreviousTrack();
                    CurrentSector = (ulong)GetTrack(CurrentTrackNumber).Indexes.Values.Max();
                    return true;
                }
            }

            // If the previous index has an invalid offset, change tracks if needed
            else if (track.Indexes[(ushort)(CurrentTrackIndex - 1)] < 0)
            {
                if(changeTrack)
                {
                    PreviousTrack();
                    CurrentSector = (ulong)GetTrack(CurrentTrackNumber).Indexes.Values.Max();
                    return true;
                }
            }
            
            // Otherwise, just move to the previous index
            else
            {
                CurrentSector = (ulong)track.Indexes[--CurrentTrackIndex];
            }

            return false;
        }

        #endregion

        #region Helpers

        /// <inheritdoc/>
        public override void LoadFirstTrack()
        {
            CurrentTrackNumber = 1;
            LoadTrack(CurrentTrackNumber);
        }

        /// <summary>
        /// Set the value for loading data tracks
        /// </summary>
        /// <param name="load">True to enable loading data tracks, false otherwise</param>
        public void SetLoadDataTracks(bool load) => _loadDataTracks = load;

        /// <summary>
        /// Set the value for loading hidden tracks
        /// </summary>
        /// <param name="load">True to enable loading hidden tracks, false otherwise</param>
        public void SetLoadHiddenTracks(bool load) => _loadHiddenTracks = load;

        /// <inheritdoc/>
        public override void SetTotalIndexes()
        {
            if(_image == null)
                return;

            TotalIndexes = GetTrack(CurrentTrackNumber).Indexes.Keys.Max();
        }

        /// <inheritdoc/>
        protected override void LoadTrack(int trackNumber)
        {
            if(_image == null)
                return;

            // If the track number is invalid, just return
            if(trackNumber < _image.Tracks.Min(t => t.TrackSequence) || trackNumber > _image.Tracks.Max(t => t.TrackSequence))
                return;

            // Cache the current track for easy access
            Track track = GetTrack(trackNumber);

            // Select the first index that has a sector offset greater than or equal to 0
            CurrentSector = (ulong)(track?.Indexes.OrderBy(kvp => kvp.Key).First(kvp => kvp.Value >= 0).Value ?? 0);
        }

        /// <summary>
        /// Get the track with the given sequence value, if possible
        /// </summary>
        /// <param name="trackNumber">Track number to retrieve</param>
        /// <returns>Track object for the requested sequence, null on error</returns>
        private Track GetTrack(int trackNumber)
        {
            try
            {
                return _image.Tracks.FirstOrDefault(t => t.TrackSequence == trackNumber);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Load TOC for the current disc image
        /// </summary>
        /// <returns>True if the TOC could be loaded, false otherwise</returns>
        private bool LoadTOC()
        {
            // If the image is invalide, we can't load or generate a TOC
            if(_image == null)
                return false;

            if(_image.Info.ReadableMediaTags?.Contains(MediaTagType.CD_FullTOC) != true)
            {
                // Only generate the TOC if we have it set
                if(!_generateMissingToc)
                {
                    Console.WriteLine("Full TOC not found");
                    return false;
                }

                Console.WriteLine("Attempting to generate TOC");

                // Get the list of tracks and flags to create the TOC with
                List<Track> tracks = _image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence).ToList();
                Dictionary<byte, byte> trackFlags = new Dictionary<byte, byte>();
                foreach(Track track in tracks)
                {
                    byte[] singleTrackFlags = _image.ReadSectorTag(track.TrackStartSector + 1, SectorTagType.CdTrackFlags);
                    if(singleTrackFlags != null)
                        trackFlags.Add((byte)track.TrackStartSector, singleTrackFlags[0]);
                }

                try
                {
                    _toc = Create(tracks, trackFlags);
                    Console.WriteLine(Prettify(_toc));
                    return true;
                }
                catch
                {
                    Console.WriteLine("Full TOC not found or generated");
                    return false;
                }
            }

            byte[] tocBytes = _image.ReadDiskTag(MediaTagType.CD_FullTOC);
            if(tocBytes == null || tocBytes.Length == 0)
            {
                Console.WriteLine("Error reading TOC from disc image");
                return false;
            }

            if(Swapping.Swap(BitConverter.ToUInt16(tocBytes, 0)) + 2 != tocBytes.Length)
            {
                byte[] tmp = new byte[tocBytes.Length + 2];
                Array.Copy(tocBytes, 0, tmp, 2, tocBytes.Length);
                tmp[0] = (byte)((tocBytes.Length & 0xFF00) >> 8);
                tmp[1] = (byte)(tocBytes.Length & 0xFF);
                tocBytes = tmp;
            }

            var nullableToc = Decode(tocBytes);
            if(nullableToc == null)
            {
                Console.WriteLine("Error decoding TOC");
                return false;
            }

            _toc = nullableToc.Value;
            Console.WriteLine(Prettify(_toc));
            return true;
        }

        /// <summary>
        /// Set default track flags for the current track
        /// </summary>
        /// <param name="track">Track object to read from</param>
        private void SetDefaultTrackFlags(Track track)
        {
            TrackType = track.TrackType;
            QuadChannel = false;
            IsDataTrack = track.TrackType != TrackType.Audio;
            CopyAllowed = false;
            TrackHasEmphasis = false;
        }

        /// <summary>
        /// Set track flags from the current track
        /// </summary>
        /// <param name="track">Track object to read from</param>
        private void SetTrackFlags(Track track)
        {
            try
            {
                // Get the track descriptor from the TOC
                TrackDataDescriptor descriptor = _toc.TrackDescriptors.First(d => d.POINT == track.TrackSequence);

                // Set the track flags from TOC data
                byte flags = (byte)(descriptor.CONTROL & 0x0D);
                TrackHasEmphasis = (flags & (byte)TocControl.TwoChanPreEmph) == (byte)TocControl.TwoChanPreEmph;
                CopyAllowed = (flags & (byte)TocControl.CopyPermissionMask) == (byte)TocControl.CopyPermissionMask;
                IsDataTrack = (flags & (byte)TocControl.DataTrack) == (byte)TocControl.DataTrack;
                QuadChannel = (flags & (byte)TocControl.FourChanNoPreEmph) == (byte)TocControl.FourChanNoPreEmph;

                TrackType = IsDataTrack ? TrackType.Data : TrackType.Audio;

                return;
            }
            catch(Exception)
            {
                SetDefaultTrackFlags(track);
            }
        }

        #endregion
    }
}
