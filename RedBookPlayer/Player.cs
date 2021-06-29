using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aaru.CommonTypes.Enums;
using Aaru.DiscImages;
using Aaru.Filters;
using CSCore.SoundOut;
using NWaves.Audio;
using NWaves.Filters.BiQuad;
using RedBookPlayer.Discs;

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
        /// Indicates if de-emphasis should be applied
        /// </summary>
        public bool ApplyDeEmphasis { get; private set; } = false;

        /// <summary>
        /// Indicate if the disc is playing
        /// </summary>
        public bool Playing => _soundOut.PlaybackState == PlaybackState.Playing;

        #endregion

        #region Private State Variables

        /// <summary>
        /// OpticalDisc object
        /// </summary>
        private OpticalDisc _opticalDisc;

        /// <summary>
        /// Current position in the sector
        /// </summary>
        private int _currentSectorReadPosition = 0;

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
        /// Initialize the player with a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(string path, bool autoPlay = false)
        {
            // Reset the internal state for initialization
            Initialized = false;
            ApplyDeEmphasis = false;
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
                _opticalDisc = OpticalDiscFactory.GenerateFromImage(image, App.Settings.AutoPlay);
            }
            catch
            {
                // All errors mean an invalid image in some way
                return;
            }

            // If we have an unusable disc, just return
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            // Enable de-emphasis for CDs, if necessary
            if(_opticalDisc is CompactDisc compactDisc)
                ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            // Setup de-emphasis filters
            SetupFilters();

            // Setup the audio output
            SetupAudio();

            // Initialize playback, if necessary
            if(autoPlay)
                _soundOut.Play();

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
            _soundOut.Volume = (float)App.Settings.Volume / 100;

            // Determine how many sectors we can read
            ulong sectorsToRead;
            ulong zeroSectorsAmount;
            do
            {
                // Attempt to read 2 more sectors than requested
                sectorsToRead = ((ulong)(count / _opticalDisc.BytesPerSector)) + 2;
                zeroSectorsAmount = 0;

                // Avoid overreads by padding with 0-byte data at the end
                if(_opticalDisc.CurrentSector + sectorsToRead > _opticalDisc.TotalSectors)
                {
                    ulong oldSectorsToRead = sectorsToRead;
                    sectorsToRead = _opticalDisc.TotalSectors - _opticalDisc.CurrentSector;
                    zeroSectorsAmount = oldSectorsToRead - sectorsToRead;
                }

                // TODO: Figure out when this value could be negative
                if(sectorsToRead <= 0)
                {
                    _opticalDisc.LoadFirstTrack();
                    _currentSectorReadPosition = 0;
                }
            } while(sectorsToRead <= 0);

            // Create padding data for overreads
            byte[] zeroSectors = new byte[(int)zeroSectorsAmount * _opticalDisc.BytesPerSector];
            byte[] audioData;

            // Attempt to read the required number of sectors
            var readSectorTask = Task.Run(() =>
            {
                lock(_readingImage)
                {
                    try
                    {
                        return _opticalDisc.ReadSectors((uint)sectorsToRead).Concat(zeroSectors).ToArray();
                    }
                    catch(ArgumentOutOfRangeException)
                    {
                        _opticalDisc.LoadFirstTrack();
                        return _opticalDisc.ReadSectors((uint)sectorsToRead).Concat(zeroSectors).ToArray();
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
            if(_currentSectorReadPosition >= _opticalDisc.BytesPerSector)
            {
                _opticalDisc.CurrentSector += (ulong)(_currentSectorReadPosition / _opticalDisc.BytesPerSector);
                _currentSectorReadPosition %= _opticalDisc.BytesPerSector;
            }

            return count;
        }

        #region Playback

        /// <summary>
        /// Toggle audio playback
        /// </summary>
        /// <param name="start">True to start playback, false to pause</param>
        public void TogglePlayPause(bool start)
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            if(start)
            {
                _soundOut.Play();
                _opticalDisc.SetTotalIndexes();
            }
            else
            {
                _soundOut.Stop();
            }
        }

        /// <summary>
        /// Stop the current audio playback
        /// </summary>
        public void Stop()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            _soundOut.Stop();
            _opticalDisc.LoadFirstTrack();
        }

        /// <summary>
        /// Move to the next playable track
        /// </summary>
        public void NextTrack()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            bool wasPlaying = Playing;
            if(wasPlaying) TogglePlayPause(false);

            _opticalDisc.NextTrack();
            if(_opticalDisc is CompactDisc compactDisc)
                ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            if(wasPlaying) TogglePlayPause(true);
        }

        /// <summary>
        /// Move to the previous playable track
        /// </summary>
        public void PreviousTrack()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            bool wasPlaying = Playing;
            if(wasPlaying) TogglePlayPause(false);

            _opticalDisc.PreviousTrack();
            if(_opticalDisc is CompactDisc compactDisc)
                ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            if(wasPlaying) TogglePlayPause(true);
        }

        /// <summary>
        /// Move to the next index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void NextIndex(bool changeTrack)
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            bool wasPlaying = Playing;
            if(wasPlaying) TogglePlayPause(false);

            _opticalDisc.NextIndex(changeTrack);
            if(_opticalDisc is CompactDisc compactDisc)
                ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            if(wasPlaying) TogglePlayPause(true);
        }

        /// <summary>
        /// Move to the previous index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void PreviousIndex(bool changeTrack)
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            bool wasPlaying = Playing;
            if(wasPlaying) TogglePlayPause(false);

            _opticalDisc.PreviousIndex(changeTrack);
            if(_opticalDisc is CompactDisc compactDisc)
                ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            if(wasPlaying) TogglePlayPause(true);
        }

        /// <summary>
        /// Fast-forward playback by 75 sectors, if possible
        /// </summary>
        public void FastForward()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            _opticalDisc.CurrentSector = Math.Min(_opticalDisc.TotalSectors, _opticalDisc.CurrentSector + 75);
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
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Generate the digit string to be interpreted by the frontend
        /// </summary>
        /// <returns>String representing the digits for the frontend</returns>
        public string GenerateDigitString()
        {
            // If the disc isn't initialized, return all '-' characters
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return string.Empty.PadLeft(20, '-');

            // Otherwise, take the current time into account
            ulong sectorTime = _opticalDisc.CurrentSector;
            if(_opticalDisc.SectionStartSector != 0)
                sectorTime -= _opticalDisc.SectionStartSector;
            else
                sectorTime += _opticalDisc.TimeOffset;

            int[] numbers = new int[]
            {
                _opticalDisc.CurrentTrackNumber + 1,
                _opticalDisc.CurrentTrackIndex,

                (int)(sectorTime / (75 * 60)),
                (int)(sectorTime / 75 % 60),
                (int)(sectorTime % 75),

                _opticalDisc.TotalTracks,
                _opticalDisc.TotalIndexes,

                (int)(_opticalDisc.TotalTime / (75 * 60)),
                (int)(_opticalDisc.TotalTime / 75 % 60),
                (int)(_opticalDisc.TotalTime % 75),
            };

            return string.Join("", numbers.Select(i => i.ToString().PadLeft(2, '0').Substring(0, 2)));
        }

        /// <summary>
        /// Toggle de-emphasis processing
        /// </summary>
        /// <param name="enable">True to apply de-emphasis, false otherwise</param>
        public void ToggleDeEmphasis(bool enable) => ApplyDeEmphasis = enable;

        /// <summary>
        /// Update the data context for the frontend
        /// </summary>
        /// <param name="dataContext">Data context to be updated</param>
        public void UpdateDataContext(PlayerViewModel dataContext)
        {
            if(!Initialized || dataContext == null)
                return;

            dataContext.HiddenTrack = _opticalDisc.TimeOffset > 150;
            dataContext.ApplyDeEmphasis = ApplyDeEmphasis;

            if(_opticalDisc is CompactDisc compactDisc)
            {
                dataContext.QuadChannel = compactDisc.QuadChannel;
                dataContext.IsDataTrack = compactDisc.IsDataTrack;
                dataContext.CopyAllowed = compactDisc.CopyAllowed;
                dataContext.TrackHasEmphasis = compactDisc.TrackHasEmphasis;
            }
            else
            {
                dataContext.QuadChannel = false;
                dataContext.IsDataTrack = _opticalDisc.TrackType != TrackType.Audio;
                dataContext.CopyAllowed = false;
                dataContext.TrackHasEmphasis = false;
            }
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