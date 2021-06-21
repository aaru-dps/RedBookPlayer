using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.DiscImages;
using Aaru.Helpers;
using CSCore.SoundOut;
using NWaves.Audio;
using NWaves.Filters.BiQuad;
using static Aaru.Decoders.CD.FullTOC;

namespace RedBookPlayer
{
    public class Player
    {
        #region Public Fields

        /// <summary>
        /// Indicate if the player is ready to be used
        /// </summary>
        public bool Initialized { get; private set; } = false;

        /// <summary>
        /// Currently loaded disc image
        /// </summary>
        public AaruFormat Image { get; private set; }

        /// <summary>
        /// Current track number
        /// </summary>
        public int CurrentTrack
        {
            get => _currentTrack;
            private set
            {
                // Unset image means we can't do anything
                if(Image == null)
                    return;

                // If the value is the same, don't do anything
                if(value == _currentTrack)
                    return;

                // Check if we're incrementing or decrementing the track
                bool increment = value > _currentTrack;

                // Ensure that the value is valid, wrapping around if necessary
                if(value >= Image.Tracks.Count)
                    _currentTrack = 0;
                else if(value < 0)
                    _currentTrack = Image.Tracks.Count - 1;
                else
                    _currentTrack = value;

                // Cache the current track for easy access
                Track track = Image.Tracks[CurrentTrack];

                // Set new track-specific data
                byte[] flagsData = Image.ReadSectorTag(track.TrackSequence, SectorTagType.CdTrackFlags);
                ApplyDeEmphasis = ((CdFlags)flagsData[0]).HasFlag(CdFlags.PreEmphasis);

                try
                {
                    byte[] subchannel = Image.ReadSectorTag(track.TrackStartSector, SectorTagType.CdSectorSubchannel);

                    if(!ApplyDeEmphasis)
                        ApplyDeEmphasis = (subchannel[3] & 0b01000000) != 0;

                    CopyAllowed = (subchannel[2] & 0b01000000) != 0;
                    TrackType = (subchannel[1] & 0b01000000) != 0 ? Aaru.CommonTypes.Enums.TrackType.Data : Aaru.CommonTypes.Enums.TrackType.Audio;
                }
                catch(ArgumentException)
                {
                    TrackType = track.TrackType;
                }

                TrackHasEmphasis = ApplyDeEmphasis;

                TotalIndexes = track.Indexes.Keys.Max();
                CurrentIndex = track.Indexes.Keys.Min();

                // If we're not playing data tracks, skip
                if(!App.Settings.PlayDataTracks && TrackType != Aaru.CommonTypes.Enums.TrackType.Audio)
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
        public ushort CurrentIndex
        {
            get => _currentIndex;
            private set
            {
                // Unset image means we can't do anything
                if(Image == null)
                    return;

                // If the value is the same, don't do anything
                if(value == _currentIndex)
                    return;

                // Cache the current track for easy access
                Track track = Image.Tracks[CurrentTrack];

                // Ensure that the value is valid, wrapping around if necessary
                if(value > track.Indexes.Keys.Max())
                    _currentIndex = 0;
                else if(value < 0)
                    _currentIndex = track.Indexes.Keys.Max();
                else
                    _currentIndex = value;

                // Set new index-specific data
                SectionStartSector = (ulong)track.Indexes[CurrentIndex];
                TotalTime = track.TrackEndSector - track.TrackStartSector;
            }
        }

        /// <summary>
        /// Current sector number
        /// </summary>
        public ulong CurrentSector
        {
            get => _currentSector;
            private set
            {
                // Unset image means we can't do anything
                if(Image == null)
                    return;

                // If the value is the same, don't do anything
                if(value == _currentSector)
                    return;

                // Cache the current track for easy access
                Track track = Image.Tracks[CurrentTrack];

                _currentSector = value;

                if((CurrentTrack < Image.Tracks.Count - 1 && CurrentSector >= Image.Tracks[CurrentTrack + 1].TrackStartSector)
                        || (CurrentTrack > 0 && CurrentSector < track.TrackStartSector))
                {
                    foreach(Track trackData in Image.Tracks.ToArray().Reverse())
                    {
                        if(CurrentSector >= trackData.TrackStartSector)
                        {
                            CurrentTrack = (int)trackData.TrackSequence - 1;
                            break;
                        }
                    }
                }

                foreach((ushort key, int i) in track.Indexes.Reverse())
                {
                    if((int)CurrentSector >= i)
                    {
                        CurrentIndex = key;
                        return;
                    }
                }

                CurrentIndex = 0;
            }
        }

        /// <summary>
        /// Represents the pre-emphasis flag
        /// </summary>
        public bool TrackHasEmphasis { get; private set; } = false;

        /// <summary>
        /// Indicates if de-emphasis should be applied
        /// </summary>
        public bool ApplyDeEmphasis { get; private set; } = false;

        /// <summary>
        /// Represents the copy allowed flag
        /// </summary>
        public bool CopyAllowed { get; private set; } = false;

        /// <summary>
        /// Represents the track type
        /// </summary>
        public TrackType? TrackType { get; private set; }

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
        /// Represents the time adjustment offset for the disc
        /// </summary>
        public ulong TimeOffset { get; private set; } = 0;

        /// <summary>
        /// Represents the total playing time for the disc
        /// </summary>
        public ulong TotalTime { get; private set; } = 0;

        /// <summary>
        /// Represents the current play volume between 0 and 100
        /// </summary>
        public int Volume
        {
            get => _volume;
            set
            {
                if(value >= 0 &&
                   value <= 100)
                    _volume = value;
            }
        }

        #endregion

        #region Private State Variables

        /// <summary>
        /// Current track number
        /// </summary>
        private int _currentTrack = 0;

        /// <summary>
        /// Current track index
        /// </summary>
        private ushort _currentIndex = 0;

        /// <summary>
        /// Current sector number
        /// </summary>
        private ulong _currentSector = 0;

        /// <summary>
        /// Current position in the sector
        /// </summary>
        private int _currentSectorReadPosition = 0;

        /// <summary>
        /// Current play volume between 0 and 100
        /// </summary>
        private int _volume = 100;

        /// <summary>
        /// Current disc table of contents
        /// </summary>
        private CDFullTOC _toc;

        /// <summary>
        /// Data provider for sound output
        /// </summary>
        private PlayerSource _source;

        /// <summary>
        /// Sound output instance
        /// </summary>
        private ALSoundOut _soundOut;

        /// <summary>
        /// Left channel de-emphasis filter
        /// </summary>
        private BiQuadFilter _deEmphasisFilterLeft;

        /// <summary>
        /// Right channel de-emphasis filter
        /// </summary>
        private BiQuadFilter _deEmphasisFilterRight;

        /// <summary>
        /// Lock object for reading track data
        /// </summary>
        private readonly object _readingImage = new object();

        #endregion

        /// <summary>
        /// Initialize the player with a given image
        /// </summary>
        /// <param name="image">Aaruformat image to load for playback</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public async void Init(AaruFormat image, bool autoPlay = false)
        {
            // If the image is null, we can't do anything
            if(image == null)
                return;

            // Set the current disc image
            Image = image;

            // Attempt to load the TOC
            if(!await LoadTOC())
                return;

            // Setup the de-emphasis filters
            SetupFilters();

            // Setup the audio output
            SetupAudio();

            // Load the first track
            CurrentTrack = 0;
            LoadTrack(0);

            // Initialize playback, if necessary
            if(autoPlay)
                _soundOut.Play();
            else
                TotalIndexes = 0;

            // Set the internal disc state
            TotalTracks = image.Tracks.Count;
            TrackDataDescriptor firstTrack = _toc.TrackDescriptors.First(d => d.ADR == 1 && d.POINT == 1);
            TimeOffset = (ulong)((firstTrack.PMIN * 60 * 75) + (firstTrack.PSEC * 75) + firstTrack.PFRAME);
            TotalTime = TimeOffset + image.Tracks.Last().TrackEndSector;

            // Set the output volume from settings
            Volume = App.Settings.Volume;

            // Mark the player as ready
            Initialized = true;

            // Begin loading data
            _source.Start();
        }

        /// <summary>
        /// Fill the current byte buffer with playable data
        /// </summary>
        /// <param name="buffer">Buffer to load data into</param>
        /// <param name="offset">Offset in the buffer to load at</param>
        /// <param name="count">Number of bytes to load</param>
        /// <returns>Number of bytes read</returns>
        public int ProviderRead(byte[] buffer, int offset, int count)
        {
            // Set the current volume
            _soundOut.Volume = (float)Volume / 100;

            // Determine how many sectors we can read
            ulong sectorsToRead;
            ulong zeroSectorsAmount;
            do
            {
                // Attempt to read 2 more sectors than requested
                sectorsToRead = ((ulong)count / 2352) + 2;
                zeroSectorsAmount = 0;

                // Avoid overreads by padding with 0-byte data at the end
                if(CurrentSector + sectorsToRead > Image.Info.Sectors)
                {
                    ulong oldSectorsToRead = sectorsToRead;
                    sectorsToRead = Image.Info.Sectors - CurrentSector;
                    zeroSectorsAmount = oldSectorsToRead - sectorsToRead;
                }

                // TODO: Figure out when this value could be negative
                if(sectorsToRead <= 0)
                {
                    LoadTrack(0);
                    _currentSectorReadPosition = 0;
                }
            } while(sectorsToRead <= 0);

            // Create padding data for overreads
            byte[] zeroSectors = new byte[zeroSectorsAmount * 2352];
            byte[] audioData;

            // Attempt to read the required number of sectors
            var readSectorTask = Task.Run(() =>
            {
                lock(_readingImage)
                {
                    try
                    {
                        return Image.ReadSectors(CurrentSector, (uint)sectorsToRead).Concat(zeroSectors).ToArray();
                    }
                    catch(ArgumentOutOfRangeException)
                    {
                        LoadTrack(0);
                        return Image.ReadSectors(CurrentSector, (uint)sectorsToRead).Concat(zeroSectors).ToArray();
                    }
                }
            });

            // Wait 100ms at longest for the read to occur
            if(readSectorTask.Wait(TimeSpan.FromMilliseconds(100)))
            {
                audioData = readSectorTask.Result;
            }
            else
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            // Load only the requested audio segment
            byte[] audioDataSegment = new byte[count];
            Array.Copy(audioData, _currentSectorReadPosition, audioDataSegment, 0, Math.Min(count, audioData.Length - _currentSectorReadPosition));

            // Apply de-emphasis filtering, only if enabled
            if(ApplyDeEmphasis)
            {
                float[][] floatAudioData = new float[2][];
                floatAudioData[0] = new float[audioDataSegment.Length / 4];
                floatAudioData[1] = new float[audioDataSegment.Length / 4];
                ByteConverter.ToFloats16Bit(audioDataSegment, floatAudioData);

                for(int i = 0; i < floatAudioData[0].Length; i++)
                {
                    floatAudioData[0][i] = _deEmphasisFilterLeft.Process(floatAudioData[0][i]);
                    floatAudioData[1][i] = _deEmphasisFilterRight.Process(floatAudioData[1][i]);
                }

                ByteConverter.FromFloats16Bit(floatAudioData, audioDataSegment);
            }

            // Write out the audio data to the buffer
            Array.Copy(audioDataSegment, 0, buffer, offset, count);

            // Set the read position in the sector for easier access
            _currentSectorReadPosition += count;
            if(_currentSectorReadPosition >= 2352)
            {
                CurrentSector += (ulong)_currentSectorReadPosition / 2352;
                _currentSectorReadPosition %= 2352;
            }

            return count;
        }

        #region Player Controls

        /// <summary>
        /// Start audio playback
        /// </summary>
        public void Play()
        {
            if(Image == null)
                return;

            _soundOut.Play();
            TotalIndexes = Image.Tracks[CurrentTrack].Indexes.Keys.Max();
        }

        /// <summary>
        /// Pause the current audio playback
        /// </summary>
        public void Pause()
        {
            if(Image == null)
                return;

            _soundOut.Stop();
        }

        /// <summary>
        /// Stop the current audio playback
        /// </summary>
        public void Stop()
        {
            if(Image == null)
                return;

            _soundOut.Stop();
            LoadTrack(CurrentTrack);
        }

        /// <summary>
        /// Try to move to the next track, wrapping around if necessary
        /// </summary>
        public void NextTrack()
        {
            if(Image == null)
                return;

            CurrentTrack++;
            LoadTrack(CurrentTrack);
        }

        /// <summary>
        /// Try to move to the previous track, wrapping around if necessary
        /// </summary>
        public void PreviousTrack()
        {
            if(Image == null)
                return;

            if(CurrentSector < (ulong)Image.Tracks[CurrentTrack].Indexes[1] + 75)
            {
                if(App.Settings.AllowSkipHiddenTrack && CurrentTrack == 0 && CurrentSector >= 75)
                    CurrentSector = 0;
                else
                    CurrentTrack--;
            }

            LoadTrack(CurrentTrack);
        }

        /// <summary>
        /// Try to move to the next track index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void NextIndex(bool changeTrack)
        {
            if(Image == null)
                return;

            if(CurrentIndex + 1 > Image.Tracks[CurrentTrack].Indexes.Keys.Max())
            {
                if(changeTrack)
                {
                    NextTrack();
                    CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes.Values.Min();
                }
            }
            else
            {
                CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[++CurrentIndex];
            }
        }

        /// <summary>
        /// Try to move to the previous track index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void PreviousIndex(bool changeTrack)
        {
            if(Image == null)
                return;

            if(CurrentIndex - 1 < Image.Tracks[CurrentTrack].Indexes.Keys.Min())
            {
                if(changeTrack)
                {
                    PreviousTrack();
                    CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes.Values.Max();
                }
            }
            else
            {
                CurrentSector = (ulong)Image.Tracks[CurrentTrack].Indexes[--CurrentIndex];
            }
        }

        /// <summary>
        /// Fast-forward playback by 75 sectors, if possible
        /// </summary>
        public void FastForward()
        {
            if(Image == null)
                return;

            CurrentSector = Math.Min(Image.Info.Sectors - 1, CurrentSector + 75);
        }

        /// <summary>
        /// Rewind playback by 75 sectors, if possible
        /// </summary>
        public void Rewind()
        {
            if(Image == null)
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
        /// Generate a CDFullTOC object from the current image
        /// </summary>
        /// <returns>CDFullTOC object, if possible</returns>
        /// <remarks>Copied from <see cref="Aaru.DiscImages.CloneCd"/></remarks>
        private bool GenerateTOC()
        {
            // Invalid image means we can't generate anything
            if(Image == null)
                return false;

            _toc = new CDFullTOC();
            Dictionary<byte, byte> _trackFlags = new Dictionary<byte, byte>();
            Dictionary<byte, byte> sessionEndingTrack = new Dictionary<byte, byte>();
            _toc.FirstCompleteSession = byte.MaxValue;
            _toc.LastCompleteSession = byte.MinValue;
            List<TrackDataDescriptor> trackDescriptors = new List<TrackDataDescriptor>();
            byte currentTrack = 0;

            foreach(Track track in Image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence))
            {
                byte[] trackFlags = Image.ReadSectorTag(track.TrackStartSector + 1, SectorTagType.CdTrackFlags);
                if(trackFlags != null)
                    _trackFlags.Add((byte)track.TrackStartSector, trackFlags[0]);
            }

            foreach(Track track in Image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence))
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

            foreach(Track track in Image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence))
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
                        LbaToMsf(Image.Tracks.OrderBy(t => t.TrackSession).ThenBy(t => t.TrackSequence).Last().
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
                        LbaToMsf(Image.Tracks.FirstOrDefault(t => t.TrackSequence == endingTrackNumber)?.TrackEndSector ??
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
        private async Task<bool> LoadTOC()
        {
            if(await Task.Run(() => Image.Info.ReadableMediaTags?.Contains(MediaTagType.CD_FullTOC)) != true)
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

            byte[] tocBytes = await Task.Run(() => Image.ReadDiskTag(MediaTagType.CD_FullTOC));
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

            var nullableToc = await Task.Run(() => Decode(tocBytes));
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
        /// Load the track for a given track number, if possible
        /// </summary>
        /// <param name="index">Track number to load</param>
        private void LoadTrack(int index)
        {
            // Save if audio is currently playing
            bool oldRun = _source.Run;

            // Stop playback if necessary
            _source.Stop();

            // If it is a valid index, seek to the first, non-negative sectored index for the track
            if(index >= 0 && index < Image.Tracks.Count)
            {
                ushort firstIndex = Image.Tracks[index].Indexes.Keys.Min();
                int firstSector = Image.Tracks[index].Indexes[firstIndex];
                CurrentSector = (ulong)(firstSector >= 0 ? firstSector : Image.Tracks[index].Indexes[1]);
            }

            // Reset the playing state
            _source.Run = oldRun;
        }

        /// <summary>
        /// Sets or resets the de-emphasis filters
        /// </summary>
        private void SetupFilters()
        {
            if(_deEmphasisFilterLeft == null)
            {
                _deEmphasisFilterLeft = new DeEmphasisFilter();
                _deEmphasisFilterRight = new DeEmphasisFilter();
            }
            else
            {
                _deEmphasisFilterLeft.Reset();
                _deEmphasisFilterRight.Reset();
            }
        }

        /// <summary>
        /// Sets or resets the audio playback objects
        /// </summary>
        private void SetupAudio()
        {
            if(_source == null)
            {
                _source = new PlayerSource(ProviderRead);
                _soundOut = new ALSoundOut(100);
                _soundOut.Initialize(_source);
            }
            else
            {
                _soundOut.Stop();
            }
        }

        #endregion
    }
}