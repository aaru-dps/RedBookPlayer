using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using static Aaru.Decoders.CD.FullTOC;

namespace RedBookPlayer
{
    public class PlayableDisc
    {
        #region Public Fields

        /// <summary>
        /// Indicate if the disc is ready to be used
        /// </summary>
        public bool Initialized { get; private set; } = false;

        /// <summary>
        /// Current track number
        /// </summary>
        public int CurrentTrackNumber
        {
            get => _currentTrackNumber;
            set
            {
                // Unset image means we can't do anything
                if(_image == null)
                    return;

                // Check if we're incrementing or decrementing the track
                bool increment = value >= _currentTrackNumber;

                // Ensure that the value is valid, wrapping around if necessary
                if(value >= _image.Tracks.Count)
                    _currentTrackNumber = 0;
                else if(value < 0)
                    _currentTrackNumber = _image.Tracks.Count - 1;
                else
                    _currentTrackNumber = value;

                // Cache the current track for easy access
                Track track = _image.Tracks[CurrentTrackNumber];

                // Set track flags from subchannel data, if possible
                SetTrackFlags(track);

                ApplyDeEmphasis = TrackHasEmphasis;

                TotalIndexes = track.Indexes.Keys.Max();
                CurrentTrackIndex = track.Indexes.Keys.Min();

                // If we're not playing data tracks, skip
                if(!App.Settings.PlayDataTracks && TrackType != TrackType.Audio)
                {
                    if(increment)
                        NextTrack();
                    else
                        PreviousTrack();
                }
            }
        }

        /// <summary>
        /// Current track index
        /// </summary>
        public ushort CurrentTrackIndex
        {
            get => _currentTrackIndex;
            set
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

        /// <summary>
        /// Current sector number
        /// </summary>
        public ulong CurrentSector
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
        /// Represents the PRE flag
        /// </summary>
        public bool TrackHasEmphasis { get; private set; } = false;

        /// <summary>
        /// Indicates if de-emphasis should be applied
        /// </summary>
        public bool ApplyDeEmphasis { get; private set; } = false;

        /// <summary>
        /// Represents the DCP flag
        /// </summary>
        public bool CopyAllowed { get; private set; } = false;

        /// <summary>
        /// Represents the track type
        /// </summary>
        public TrackType TrackType { get; private set; }

        /// <summary>
        /// Represents the 4CH flag
        /// </summary>
        public bool QuadChannel { get; private set; } = false;

        /// <summary>
        /// Represents the sector starting the section
        /// </summary>
        public ulong SectionStartSector { get; private set; }

        /// <summary>
        /// Represents the total tracks on the disc
        /// </summary>
        public int TotalTracks { get; private set; } = 0;

        /// <summary>
        /// Represents the total indices on the disc
        /// </summary>
        public int TotalIndexes { get; private set; } = 0;

        /// <summary>
        /// Total sectors in the image
        /// </summary>
        public ulong TotalSectors => _image.Info.Sectors;

        /// <summary>
        /// Represents the time adjustment offset for the disc
        /// </summary>
        public ulong TimeOffset { get; private set; } = 0;

        /// <summary>
        /// Represents the total playing time for the disc
        /// </summary>
        public ulong TotalTime { get; private set; } = 0;

        #endregion

        #region Private State Variables

        /// <summary>
        /// Currently loaded disc image
        /// </summary>
        private IOpticalMediaImage _image;

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

        /// <summary>
        /// Initialize the disc with a given image
        /// </summary>
        /// <param name="image">Aaruformat image to load</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(IOpticalMediaImage image, bool autoPlay = false)
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

        /// <summary>
        /// Try to move to the next track, wrapping around if necessary
        /// </summary>
        public void NextTrack()
        {
            if(_image == null)
                return;

            CurrentTrackNumber++;
            LoadTrack(CurrentTrackNumber);
        }

        /// <summary>
        /// Try to move to the previous track, wrapping around if necessary
        /// </summary>
        public void PreviousTrack()
        {
            if(_image == null)
                return;

            if(CurrentSector < (ulong)_image.Tracks[CurrentTrackNumber].Indexes[1] + 75)
            {
                if(App.Settings.AllowSkipHiddenTrack && CurrentTrackNumber == 0 && CurrentSector >= 75)
                    CurrentSector = 0;
                else
                    CurrentTrackNumber--;
            }
            else
                CurrentTrackNumber--;

            LoadTrack(CurrentTrackNumber);
        }

        /// <summary>
        /// Try to move to the next track index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        /// <returns>True if the track was changed, false otherwise</returns>
        public bool NextIndex(bool changeTrack)
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

        /// <summary>
        /// Try to move to the previous track index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        /// <returns>True if the track was changed, false otherwise</returns>
        public bool PreviousIndex(bool changeTrack)
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

        /// <summary>
        /// Fast-forward playback by 75 sectors, if possible
        /// </summary>
        public void FastForward()
        {
            if(_image == null)
                return;

            CurrentSector = Math.Min(_image.Info.Sectors - 1, CurrentSector + 75);
        }

        /// <summary>
        /// Rewind playback by 75 sectors, if possible
        /// </summary>
        public void Rewind()
        {
            if(_image == null)
                return;

            if(CurrentSector >= 75)
                CurrentSector -= 75;
        }

        /// <summary>
        /// Toggle de-emphasis processing
        /// </summary>
        /// <param name="enable">True to apply de-emphasis, false otherwise</param>
        public void ToggleDeEmphasis(bool enable) => ApplyDeEmphasis = enable;

        #endregion

        #region Helpers

        /// <summary>
        /// Load the first valid track in the image
        /// </summary>
        public void LoadFirstTrack()
        {
            CurrentTrackNumber = 0;
            LoadTrack(CurrentTrackNumber);
        }

        /// <summary>
        /// Read sector data from the base image starting from the current sector
        /// </summary>
        /// <param name="sectorsToRead">Current number of sectors to read</param>
        /// <returns>Byte array representing the read sectors, if possible</returns>
        public byte[] ReadSectors(uint sectorsToRead) => _image.ReadSectors(CurrentSector, sectorsToRead);

        /// <summary>
        /// Set the total indexes from the current track
        /// </summary>
        public void SetTotalIndexes()
        {
            if(_image == null)
                return;

            TotalIndexes = _image.Tracks[CurrentTrackNumber].Indexes.Keys.Max();
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
        /// Load the desired track, if possible
        /// </summary>
        /// <param name="track">Track number to load</param>
        private void LoadTrack(int track)
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
                ulong currentSector = track.TrackStartSector;
                for (int i = 0; i < 16; i++)
                {
                    // Try to read the subchannel
                    byte[] subBuf = _image.ReadSectorTag(track.TrackStartSector, SectorTagType.CdSectorSubchannel);
                    if(subBuf == null || subBuf.Length < 4)
                        return;

                    // Check the expected track, if possible
                    int adr = subBuf[0] & 0x0F;
                    if(adr == 1)
                    {
                        if(subBuf[1] > track.TrackSequence)
                        {
                            currentSector--;
                            continue;
                        }
                        else if(subBuf[1] < track.TrackSequence)
                        {
                            currentSector++;
                            continue;
                        }
                    }

                    // Set the track flags from subchannel data
                    int control = (subBuf[0] & 0xF0) / 16;
                    switch((control & 0xC) / 4)
                    {
                        case 0:
                            QuadChannel = false;
                            TrackType = TrackType.Audio;
                            TrackHasEmphasis = (control & 0x01) == 1;
                            break;
                        case 1:
                            QuadChannel = false;
                            TrackType = TrackType.Data;
                            TrackHasEmphasis = false;
                            break;
                        case 2:
                            QuadChannel = true;
                            TrackType = TrackType.Audio;
                            TrackHasEmphasis = (control & 0x01) == 1;
                            break;
                        default:
                            QuadChannel = false;
                            TrackType = track.TrackType;
                            TrackHasEmphasis = false;
                            break;
                    }

                    CopyAllowed = (control & 0x02) > 0;
                    return;
                }

                // If we didn't find subchannel data, assume defaults
                SetDefaultTrackFlags(track);
            }
            catch(Exception)
            {
                SetDefaultTrackFlags(track);
            }
        }

        #endregion
    }
}
