using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decoders.CD;
using Aaru.Helpers;
using static Aaru.Decoders.CD.FullTOC;

namespace RedBookPlayer.Discs
{
    public class CompactDisc : OpticalDisc
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
                int cachedTrackNumber = _currentTrackNumber;

                // Check if we're incrementing or decrementing the track
                bool increment = cachedValue >= _currentTrackNumber;

                do
                {
                    // Ensure that the value is valid, wrapping around if necessary
                    if(cachedValue >= _image.Tracks.Count)
                        cachedValue = 0;
                    else if(cachedValue < 0)
                        cachedValue = _image.Tracks.Count - 1;

                    _currentTrackNumber = cachedValue;

                    // Cache the current track for easy access
                    Track track = _image.Tracks[_currentTrackNumber];

                    // Set track flags from subchannel data, if possible
                    SetTrackFlags(track);

                    TotalIndexes = track.Indexes.Keys.Max();
                    CurrentTrackIndex = track.Indexes.Keys.Min();

                    // If the track is playable, just return
                    if(TrackType == TrackType.Audio || App.Settings.PlayDataTracks)
                        return;

                    // If we're not playing the track, skip
                    if(increment)
                        cachedValue++;
                    else
                        cachedValue--;
                }
                while(cachedValue != cachedTrackNumber);
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
                Track track = _image.Tracks[CurrentTrackNumber];

                // Ensure that the value is valid, wrapping around if necessary
                if(value > track.Indexes.Keys.Max())
                    _currentTrackIndex = 0;
                else if(value < 0)
                    _currentTrackIndex = track.Indexes.Keys.Max();
                else
                    _currentTrackIndex = value;

                // Set new index-specific data
                SectionStartSector = (ulong)track.Indexes[CurrentTrackIndex];
                TotalTime = track.TrackEndSector - track.TrackStartSector;
            }
        }

        /// <inheritdoc/>
        public override ulong CurrentSector
        {
            get => _currentSector;
            set
            {
                // Unset image means we can't do anything
                if(_image == null)
                    return;

                // Cache the current track for easy access
                Track track = _image.Tracks[CurrentTrackNumber];

                _currentSector = value;

                if((CurrentTrackNumber < _image.Tracks.Count - 1 && CurrentSector >= _image.Tracks[CurrentTrackNumber + 1].TrackStartSector)
                    || (CurrentTrackNumber > 0 && CurrentSector < track.TrackStartSector))
                {
                    foreach(Track trackData in _image.Tracks.ToArray().Reverse())
                    {
                        if(CurrentSector >= trackData.TrackStartSector)
                        {
                            CurrentTrackNumber = (int)trackData.TrackSequence - 1;
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

        /// <summary>
        /// Represents the 4CH flag
        /// </summary>
        public bool QuadChannel { get; private set; } = false;

        /// <summary>
        /// Represents the DATA flag
        /// </summary>
        public bool IsDataTrack => TrackType != TrackType.Audio;

        /// <summary>
        /// Represents the DCP flag
        /// </summary>
        public bool CopyAllowed { get; private set; } = false;

        /// <summary>
        /// Represents the PRE flag
        /// </summary>
        public bool TrackHasEmphasis { get; private set; } = false;

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
        /// Current disc table of contents
        /// </summary>
        private CDFullTOC _toc;

        #endregion

        /// <inheritdoc/>
        public override void Init(IOpticalMediaImage image, bool autoPlay = false)
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
            TotalTracks = _image.Tracks.Count;
            TrackDataDescriptor firstTrack = _toc.TrackDescriptors.First(d => d.ADR == 1 && d.POINT == 1);
            TimeOffset = (ulong)((firstTrack.PMIN * 60 * 75) + (firstTrack.PSEC * 75) + firstTrack.PFRAME);
            TotalTime = TimeOffset + _image.Tracks.Last().TrackEndSector;

            // Mark the disc as ready
            Initialized = true;
        }

        #region Seeking

        /// <inheritdoc/>
        public override bool NextIndex(bool changeTrack)
        {
            if(_image == null)
                return false;

            if(CurrentTrackIndex + 1 > _image.Tracks[CurrentTrackNumber].Indexes.Keys.Max())
            {
                if(changeTrack)
                {
                    NextTrack();
                    CurrentSector = (ulong)_image.Tracks[CurrentTrackNumber].Indexes.Values.Min();
                    return true;
                }
            }
            else
            {
                CurrentSector = (ulong)_image.Tracks[CurrentTrackNumber].Indexes[++CurrentTrackIndex];
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool PreviousIndex(bool changeTrack)
        {
            if(_image == null)
                return false;

            if(CurrentTrackIndex - 1 < _image.Tracks[CurrentTrackNumber].Indexes.Keys.Min())
            {
                if(changeTrack)
                {
                    PreviousTrack();
                    CurrentSector = (ulong)_image.Tracks[CurrentTrackNumber].Indexes.Values.Max();
                    return true;
                }
            }
            else
            {
                CurrentSector = (ulong)_image.Tracks[CurrentTrackNumber].Indexes[--CurrentTrackIndex];
            }

            return false;
        }

        #endregion

        #region Helpers

        /// <inheritdoc/>
        public override void LoadFirstTrack()
        {
            CurrentTrackNumber = 0;
            LoadTrack(CurrentTrackNumber);
        }

        /// <inheritdoc/>
        public override void SetTotalIndexes()
        {
            if(_image == null)
                return;

            TotalIndexes = _image.Tracks[CurrentTrackNumber].Indexes.Keys.Max();
        }

        /// <inheritdoc/>
        protected override void LoadTrack(int track)
        {
            if(_image == null)
                return;

            if(track < 0 || track >= _image.Tracks.Count)
                return;

            ushort firstIndex = _image.Tracks[track].Indexes.Keys.Min();
            int firstSector = _image.Tracks[track].Indexes[firstIndex];
            CurrentSector = (ulong)(firstSector >= 0 ? firstSector : _image.Tracks[track].Indexes[1]);
        }

        /// <summary>
        /// Generate a CDFullTOC object from the current image
        /// </summary>
        /// <returns>CDFullTOC object, if possible</returns>
        /// <remarks>Copied from <see cref="Aaru.DiscImages.CloneCd"/></remarks>
        private bool GenerateTOC()
        {
            // Invalid image means we can't generate anything
            if(_image == null)
                return false;

            _toc = new CDFullTOC();
            Dictionary<byte, byte> _trackFlags = new Dictionary<byte, byte>();
            Dictionary<byte, byte> sessionEndingTrack = new Dictionary<byte, byte>();
            _toc.FirstCompleteSession = byte.MaxValue;
            _toc.LastCompleteSession = byte.MinValue;
            List<TrackDataDescriptor> trackDescriptors = new List<TrackDataDescriptor>();
            byte currentTrack = 0;

            foreach(Track track in _image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence))
            {
                byte[] trackFlags = _image.ReadSectorTag(track.TrackStartSector + 1, SectorTagType.CdTrackFlags);
                if(trackFlags != null)
                    _trackFlags.Add((byte)track.TrackStartSector, trackFlags[0]);
            }

            foreach(Track track in _image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence))
            {
                if(track.TrackSession < _toc.FirstCompleteSession)
                    _toc.FirstCompleteSession = (byte)track.TrackSession;

                if(track.TrackSession <= _toc.LastCompleteSession)
                {
                    currentTrack = (byte)track.TrackSequence;

                    continue;
                }

                if(_toc.LastCompleteSession > 0)
                    sessionEndingTrack.Add(_toc.LastCompleteSession, currentTrack);

                _toc.LastCompleteSession = (byte)track.TrackSession;
            }

            byte currentSession = 0;

            foreach(Track track in _image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence))
            {
                _trackFlags.TryGetValue((byte)track.TrackSequence, out byte trackControl);

                if(trackControl == 0 &&
                   track.TrackType != Aaru.CommonTypes.Enums.TrackType.Audio)
                    trackControl = (byte)CdFlags.DataTrack;

                // Lead-Out
                if(track.TrackSession > currentSession &&
                   currentSession != 0)
                {
                    (byte minute, byte second, byte frame) leadoutAmsf = LbaToMsf(track.TrackStartSector - 150);

                    (byte minute, byte second, byte frame) leadoutPmsf =
                        LbaToMsf(_image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence).Last().
                                        TrackStartSector);

                    // Lead-out
                    trackDescriptors.Add(new TrackDataDescriptor
                    {
                        SessionNumber = currentSession,
                        POINT = 0xB0,
                        ADR = 5,
                        CONTROL = 0,
                        HOUR = 0,
                        Min = leadoutAmsf.minute,
                        Sec = leadoutAmsf.second,
                        Frame = leadoutAmsf.frame,
                        PHOUR = 2,
                        PMIN = leadoutPmsf.minute,
                        PSEC = leadoutPmsf.second,
                        PFRAME = leadoutPmsf.frame
                    });

                    // This seems to be constant? It should not exist on CD-ROM but CloneCD creates them anyway
                    // Format seems like ATIP, but ATIP should not be as 0xC0 in TOC...
                    //trackDescriptors.Add(new TrackDataDescriptor
                    //{
                    //    SessionNumber = currentSession,
                    //    POINT = 0xC0,
                    //    ADR = 5,
                    //    CONTROL = 0,
                    //    Min = 128,
                    //    PMIN = 97,
                    //    PSEC = 25
                    //});
                }

                // Lead-in
                if(track.TrackSession > currentSession)
                {
                    currentSession = (byte)track.TrackSession;
                    sessionEndingTrack.TryGetValue(currentSession, out byte endingTrackNumber);

                    (byte minute, byte second, byte frame) leadinPmsf =
                        LbaToMsf(_image.Tracks.FirstOrDefault(t => t.TrackSequence == endingTrackNumber)?.TrackEndSector ??
                                 0 + 1);

                    // Starting track
                    trackDescriptors.Add(new TrackDataDescriptor
                    {
                        SessionNumber = currentSession,
                        POINT = 0xA0,
                        ADR = 1,
                        CONTROL = trackControl,
                        PMIN = (byte)track.TrackSequence
                    });

                    // Ending track
                    trackDescriptors.Add(new TrackDataDescriptor
                    {
                        SessionNumber = currentSession,
                        POINT = 0xA1,
                        ADR = 1,
                        CONTROL = trackControl,
                        PMIN = endingTrackNumber
                    });

                    // Lead-out start
                    trackDescriptors.Add(new TrackDataDescriptor
                    {
                        SessionNumber = currentSession,
                        POINT = 0xA2,
                        ADR = 1,
                        CONTROL = trackControl,
                        PHOUR = 0,
                        PMIN = leadinPmsf.minute,
                        PSEC = leadinPmsf.second,
                        PFRAME = leadinPmsf.frame
                    });
                }

                (byte minute, byte second, byte frame) pmsf = LbaToMsf(track.TrackStartSector);

                // Track
                trackDescriptors.Add(new TrackDataDescriptor
                {
                    SessionNumber = (byte)track.TrackSession,
                    POINT = (byte)track.TrackSequence,
                    ADR = 1,
                    CONTROL = trackControl,
                    PHOUR = 0,
                    PMIN = pmsf.minute,
                    PSEC = pmsf.second,
                    PFRAME = pmsf.frame
                });
            }

            _toc.TrackDescriptors = trackDescriptors.ToArray();
            return true;
        }

        /// <summary>
        /// Convert the sector to LBA values
        /// </summary>
        /// <param name="sector">Sector to convert</param>
        /// <returns>LBA values for the sector number</returns>
        /// <remarks>Copied from <see cref="Aaru.DiscImages.CloneCd"/></remarks>
        private (byte minute, byte second, byte frame) LbaToMsf(ulong sector) =>
            ((byte)((sector + 150) / 75 / 60), (byte)((sector + 150) / 75 % 60), (byte)((sector + 150) % 75));

        /// <summary>
        /// Load TOC for the current disc image
        /// </summary>
        /// <returns>True if the TOC could be loaded, false otherwise</returns>
        private bool LoadTOC()
        {
            if(_image.Info.ReadableMediaTags?.Contains(MediaTagType.CD_FullTOC) != true)
            {
                // Only generate the TOC if we have it set
                if(!App.Settings.GenerateMissingTOC)
                {
                    Console.WriteLine("Full TOC not found");
                    return false;
                }

                Console.WriteLine("Attempting to generate TOC");
                if(GenerateTOC())
                {
                    Console.WriteLine(Prettify(_toc));
                    return true;
                }
                else
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
            QuadChannel = false;
            TrackType = track.TrackType;
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
                TrackType = (flags & (byte)TocControl.DataTrack) == (byte)TocControl.DataTrack ? TrackType.Data : TrackType.Audio;
                QuadChannel = (flags & (byte)TocControl.FourChanNoPreEmph) == (byte)TocControl.FourChanNoPreEmph;

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
