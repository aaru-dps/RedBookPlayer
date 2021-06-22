using System;
using System.Linq;
using System.Threading.Tasks;
using CSCore.SoundOut;
using NWaves.Audio;
using NWaves.Filters.BiQuad;

namespace RedBookPlayer
{
    public class Player
    {
        #region Public Fields

        /// <summary>
        /// Indicate if the player is ready to be used
        /// </summary>
        public bool Initialized { get; private set; } = false;

        #endregion

        #region Private State Variables

        /// <summary>
        /// Current position in the sector
        /// </summary>
        private int _currentSectorReadPosition = 0;

        /// <summary>
        /// PlaybableDisc object
        /// </summary>
        private PlayableDisc _playableDisc;

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
        /// <param name="disc">Initialized disc image</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(PlayableDisc disc, bool autoPlay = false)
        {
            // If the disc is not initalized, we can't do anything
            if(!disc.Initialized)
                return;

            // Set the internal reference to the disc
            _playableDisc = disc;

            // Setup the de-emphasis filters
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
                sectorsToRead = ((ulong)count / 2352) + 2;
                zeroSectorsAmount = 0;

                // Avoid overreads by padding with 0-byte data at the end
                if(_playableDisc.CurrentSector + sectorsToRead > _playableDisc.TotalSectors)
                {
                    ulong oldSectorsToRead = sectorsToRead;
                    sectorsToRead = _playableDisc.TotalSectors - _playableDisc.CurrentSector;
                    zeroSectorsAmount = oldSectorsToRead - sectorsToRead;
                }

                // TODO: Figure out when this value could be negative
                if(sectorsToRead <= 0)
                {
                    _playableDisc.LoadFirstTrack();
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
                        return _playableDisc.ReadSectors((uint)sectorsToRead).Concat(zeroSectors).ToArray();
                    }
                    catch(ArgumentOutOfRangeException)
                    {
                        _playableDisc.LoadFirstTrack();
                        return _playableDisc.ReadSectors((uint)sectorsToRead).Concat(zeroSectors).ToArray();
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
            if(_playableDisc.ApplyDeEmphasis)
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
                _playableDisc.CurrentSector += (ulong)_currentSectorReadPosition / 2352;
                _currentSectorReadPosition %= 2352;
            }

            return count;
        }

        #region Playback

        /// <summary>
        /// Start audio playback
        /// </summary>
        public void Play()
        {
            if(!_playableDisc.Initialized)
                return;

            _soundOut.Play();
            _playableDisc.SetTotalIndexes();
        }

        /// <summary>
        /// Pause the current audio playback
        /// </summary>
        public void Pause()
        {
            if(!_playableDisc.Initialized)
                return;

            _soundOut.Stop();
        }

        /// <summary>
        /// Stop the current audio playback
        /// </summary>
        public void Stop()
        {
            if(!_playableDisc.Initialized)
                return;

            _soundOut.Stop();
            _playableDisc.LoadFirstTrack();
        }

        #endregion

        #region Helpers

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