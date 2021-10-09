using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decoders.CD;
using Aaru.Helpers;
using CSCore.Codecs.WAV;
using ReactiveUI;
using static Aaru.Decoders.CD.FullTOC;

namespace RedBookPlayer.Models.Discs
{
    public class CompactDisc : OpticalDiscBase, IReactiveObject
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

                // Invalid value means we can't do anything
                if(value > _image.Tracks.Max(t => t.TrackSequence))
                    return;
                else if(value < _image.Tracks.Min(t => t.TrackSequence))
                    return;

                // Cache the current track for easy access
                Track track = GetTrack(value);
                if(track == null)
                    return;

                // Set all track flags and values
                SetTrackFlags(track);
                TotalIndexes = track.Indexes.Keys.Max();
                CurrentTrackIndex = track.Indexes.Keys.Min();
                CurrentTrackSession = track.TrackSession;

                // Mark the track as changed
                this.RaiseAndSetIfChanged(ref _currentTrackNumber, value);
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
                if(track == null)
                    return;

                // Invalid value means we can't do anything
                if(value > track.Indexes.Keys.Max())
                    return;
                else if(value < track.Indexes.Keys.Min())
                    return;

                this.RaiseAndSetIfChanged(ref _currentTrackIndex, value);

                // Set new index-specific data
                SectionStartSector = (ulong)track.Indexes[CurrentTrackIndex];
                TotalTime = track.TrackEndSector - track.TrackStartSector;
            }
        }

        /// <inheritdoc/>
        public override ushort CurrentTrackSession
        {
            get => _currentTrackSession;
            protected set => this.RaiseAndSetIfChanged(ref _currentTrackSession, value);
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

                // Invalid value means we can't do anything
                if(value > _image.Info.Sectors)
                    return;
                else if(value < 0)
                    return;

                // Cache the current track for easy access
                Track track = GetTrack(CurrentTrackNumber);
                if(track == null)
                    return;

                this.RaiseAndSetIfChanged(ref _currentSector, value);

                // If the current sector is outside of the last known track, seek to the right one
                if(CurrentSector < track.TrackStartSector || CurrentSector > track.TrackEndSector)
                {
                    track = _image.Tracks.Last(t => CurrentSector >= t.TrackStartSector);
                    CurrentTrackNumber = (int)track.TrackSequence;
                }

                // Set the new index, if necessary
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
        public override int BytesPerSector => GetTrack(CurrentTrackNumber)?.TrackRawBytesPerSector ?? 0;

        /// <summary>
        /// Readonly list of all tracks in the image
        /// </summary>
        public List<Track> Tracks => _image?.Tracks;

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
        /// Current track session
        /// </summary>
        private ushort _currentTrackSession = 0;

        /// <summary>
        /// Current sector number
        /// </summary>
        private ulong _currentSector = 0;

        /// <summary>
        /// Indicate if a TOC should be generated if missing
        /// </summary>
        private readonly bool _generateMissingToc = false;

        /// <summary>
        /// Current disc table of contents
        /// </summary>
        private CDFullTOC _toc;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">Set of options for a new disc</param>
        public CompactDisc(OpticalDiscOptions options) => _generateMissingToc = options.GenerateMissingToc;

        /// <inheritdoc/>
        public override void Init(string path, IOpticalMediaImage image, bool autoPlay)
        {
            // If the image is null, we can't do anything
            if(image == null)
                return;

            // Set the current disc image
            ImagePath = path;
            _image = image;

            // Attempt to load the TOC
            if(!LoadTOC())
                return;

            // Load the first track by default
            CurrentTrackNumber = 1;
            LoadTrack(CurrentTrackNumber);

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

        #region Helpers

        /// <inheritdoc/>
        public override void ExtractTrackToWav(uint trackNumber, string outputDirectory) => ExtractTrackToWav(trackNumber, outputDirectory, DataPlayback.Skip);

        /// <summary>
        /// Extract a track to WAV
        /// </summary>
        /// <param name="trackNumber">Track number to extract</param>
        /// <param name="outputDirectory">Output path to write data to</param>
        /// <param name="dataPlayback">DataPlayback value indicating how to handle data tracks</param>
        public void ExtractTrackToWav(uint trackNumber, string outputDirectory, DataPlayback dataPlayback)
        {
            if(_image == null)
                return;

            // Get the track with that value, if possible
            Track track = _image.Tracks.FirstOrDefault(t => t.TrackSequence == trackNumber);
            if(track == null)
                return;

            // Get the number of sectors to read
            uint length = (uint)(track.TrackEndSector - track.TrackStartSector);

            // Read in the track data to a buffer
            byte[] buffer = ReadSectors(track.TrackStartSector, length, dataPlayback);

            // Build the WAV output
            string filename = Path.Combine(outputDirectory, $"Track {trackNumber.ToString().PadLeft(2, '0')}.wav");
            using(WaveWriter waveWriter = new WaveWriter(filename, new CSCore.WaveFormat()))
            {
                // TODO: This should also apply de-emphasis as on playback
                // Should this be configurable? Match the de-emphasis status?

                // Write out to the file
                waveWriter.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Get the track with the given sequence value, if possible
        /// </summary>
        /// <param name="trackNumber">Track number to retrieve</param>
        /// <returns>Track object for the requested sequence, null on error</returns>
        public Track GetTrack(int trackNumber)
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

        /// <inheritdoc/>
        public override void LoadTrack(int trackNumber)
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

            // Load and debug output
            uint sectorCount = (uint)(track.TrackEndSector - track.TrackStartSector);
            byte[] trackData = ReadSectors(sectorCount);
            Console.WriteLine($"DEBUG: Track {trackNumber} - {sectorCount} sectors / {trackData.Length} bytes");
        }

        /// <inheritdoc/>
        public override void LoadIndex(ushort index)
        {
            if(_image == null)
                return;

            // Cache the current track for easy access
            Track track = GetTrack(CurrentTrackNumber);
            if(track == null)
                return;

            // If the index is invalid, just return
            if(index < track.Indexes.Keys.Min() || index > track.Indexes.Keys.Max())
                return;

            // Select the first index that has a sector offset greater than or equal to 0
            CurrentSector = (ulong)track.Indexes[index];
        }

        /// <inheritdoc/>
        public override byte[] ReadSectors(uint sectorsToRead) => ReadSectors(CurrentSector, sectorsToRead, DataPlayback.Skip);

        /// <summary>
        /// Read sector data from the base image starting from the specified sector
        /// </summary>
        /// <param name="sectorsToRead">Current number of sectors to read</param>
        /// <param name="dataPlayback">DataPlayback value indicating how to handle data tracks</param>
        /// <returns>Byte array representing the read sectors, if possible</returns>
        public byte[] ReadSectors(uint sectorsToRead, DataPlayback dataPlayback) => ReadSectors(CurrentSector, sectorsToRead, dataPlayback);

        /// <summary>
        /// Read subchannel data from the base image starting from the specified sector
        /// </summary>
        /// <param name="sectorsToRead">Current number of sectors to read</param>
        /// <returns>Byte array representing the read subchannels, if possible</returns>
        public byte[] ReadSubchannels(uint sectorsToRead) => ReadSubchannels(CurrentSector, sectorsToRead);

        /// <summary>
        /// Read sector data from the base image starting from the specified sector
        /// </summary>
        /// <param name="startSector">Sector to start at for reading</param>
        /// <param name="sectorsToRead">Current number of sectors to read</param>
        /// <param name="dataPlayback">DataPlayback value indicating how to handle data tracks</param>
        /// <returns>Byte array representing the read sectors, if possible</returns>
        /// <remarks>Should be a multiple of 96 bytes</remarks>
        private byte[] ReadSectors(ulong startSector, uint sectorsToRead, DataPlayback dataPlayback)
        {
            if(TrackType == TrackType.Audio || dataPlayback == DataPlayback.Play)
            {
                return _image.ReadSectors(startSector, sectorsToRead);
            }
            else if(dataPlayback == DataPlayback.Blank)
            {
                byte[] sectors = _image.ReadSectors(startSector, sectorsToRead);
                Array.Clear(sectors, 0, sectors.Length);
                return sectors;
            }
            else
            {
                return new byte[0];
            }
        }

        /// <summary>
        /// Read subchannel data from the base image starting from the specified sector
        /// </summary>
        /// <param name="startSector">Sector to start at for reading</param>
        /// <param name="sectorsToRead">Current number of sectors to read</param>
        /// <returns>Byte array representing the read subchannels, if possible</returns>
        /// <remarks>Should be a multiple of 96 bytes</remarks>
        private byte[] ReadSubchannels(ulong startSector, uint sectorsToRead)
            => _image.ReadSectorsTag(startSector, sectorsToRead, SectorTagType.CdSectorSubchannel);

        /// <inheritdoc/>
        public override void SetTotalIndexes()
        {
            if(_image == null)
                return;

            TotalIndexes = GetTrack(CurrentTrackNumber)?.Indexes.Keys.Max() ?? 0;
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