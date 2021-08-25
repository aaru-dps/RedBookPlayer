using System;
using System.Linq;
using System.Threading.Tasks;
using CSCore.SoundOut;
using NWaves.Audio;
using NWaves.Filters.BiQuad;
using ReactiveUI;
using RedBookPlayer.Models.Discs;

namespace RedBookPlayer.Models.Hardware
{
    public class SoundOutput : ReactiveObject
    {
        #region Public Fields

        /// <summary>
        /// Indicate if the output is ready to be used
        /// </summary>
        public bool Initialized
        {
            get => _initialized;
            private set => this.RaiseAndSetIfChanged(ref _initialized, value);
        }

        /// <summary>
        /// Indicates the current player state
        /// </summary>
        public PlayerState PlayerState
        {
            get => _playerState;
            private set => this.RaiseAndSetIfChanged(ref _playerState, value);
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
            private set
            {
                int tempVolume = value;
                if(value > 100)
                    tempVolume = 100;
                else if(value < 0)
                    tempVolume = 0;

                this.RaiseAndSetIfChanged(ref _volume, tempVolume);
            }
        }

        private bool _initialized;
        private PlayerState _playerState;
        private bool _applyDeEmphasis;
        private int _volume;

        #endregion

        #region Private State Variables

        /// <summary>
        /// OpticalDisc from the parent player for easy access
        /// </summary>
        /// <remarks>
        /// TODO: Can we remove the need for a local reference to OpticalDisc?
        /// </remarks>
        private OpticalDiscBase _opticalDisc;

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
        /// Current position in the sector
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
        /// <param name="defaultVolume">Default volume between 0 and 100 to use when starting playback</param>
        public SoundOutput(int defaultVolume = 100) => Volume = defaultVolume;

        /// <summary>
        /// Initialize the output with a given image
        /// </summary>
        /// <param name="opticalDisc">OpticalDisc to load from</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(OpticalDiscBase opticalDisc, bool autoPlay)
        {
            // If we have an unusable disc, just return
            if(opticalDisc == null || !opticalDisc.Initialized)
                return;

            // Save a reference to the disc
            _opticalDisc = opticalDisc;

            // Enable de-emphasis for CDs, if necessary
            if(opticalDisc is CompactDisc compactDisc)
                ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            // Setup de-emphasis filters
            SetupFilters();

            // Setup the audio output
            SetupAudio();

            // Initialize playback, if necessary
            if(autoPlay)
                _soundOut.Play();

            // Mark the output as ready
            Initialized = true;
            PlayerState = PlayerState.Stopped;

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

            // If we have an unreadable track, just return
            if(_opticalDisc.BytesPerSector <= 0)
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
            if(_currentSectorReadPosition >= _opticalDisc.BytesPerSector)
            {
                int currentTrack = _opticalDisc.CurrentTrackNumber;
                _opticalDisc.SetCurrentSector(_opticalDisc.CurrentSector + (ulong)(_currentSectorReadPosition / _opticalDisc.BytesPerSector));
                _currentSectorReadPosition %= _opticalDisc.BytesPerSector;
            }

            return count;
        }

        #region Playback

        /// <summary>
        /// Start audio playback
        /// </summary>
        public void Play()
        {
            if(_soundOut.PlaybackState != PlaybackState.Playing)
                _soundOut.Play();

            PlayerState = PlayerState.Playing;
        }

        /// <summary>
        /// Pause audio playback
        /// </summary>
        public void Pause()
        {
            if(_soundOut.PlaybackState != PlaybackState.Paused)
                _soundOut.Pause();

            PlayerState = PlayerState.Paused;
        }

        /// <summary>
        /// Stop audio playback
        /// </summary>
        public void Stop()
        {
            if(_soundOut.PlaybackState != PlaybackState.Stopped)
                _soundOut.Stop();

            PlayerState = PlayerState.Stopped;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Set de-emphasis status
        /// </summary>
        /// <param name="apply">New de-emphasis status</param>
        public void SetDeEmphasis(bool apply) => ApplyDeEmphasis = apply;

        /// <summary>
        /// Set the value for the volume
        /// </summary>
        /// <param name="volume">New volume value</param>
        public void SetVolume(int volume) => Volume = volume;

        /// <summary>
        /// Determine the number of real and zero sectors to read
        /// </summary>
        /// <param name="count">Number of requested bytes to read</param>
        /// <param name="sectorsToRead">Number of sectors to read</param>
        /// <param name="zeroSectorsAmount">Number of zeroed sectors to concatenate</param>
        private void DetermineReadAmount(int count, out ulong sectorsToRead, out ulong zeroSectorsAmount)
        {
            // Attempt to read 5 more sectors than requested
            sectorsToRead = ((ulong)count / (ulong)_opticalDisc.BytesPerSector) + 5;
            zeroSectorsAmount = 0;

            // Avoid overreads by padding with 0-byte data at the end
            if(_opticalDisc.CurrentSector + sectorsToRead > _opticalDisc.TotalSectors)
            {
                ulong oldSectorsToRead = sectorsToRead;
                sectorsToRead = _opticalDisc.TotalSectors - _opticalDisc.CurrentSector;

                int tempZeroSectorCount = (int)(oldSectorsToRead - sectorsToRead);
                zeroSectorsAmount = (ulong)(tempZeroSectorCount < 0 ? 0 : tempZeroSectorCount);
            }
        }

        /// <summary>
        /// Process de-emphasis of audio data
        /// </summary>
        /// <param name="audioData">Audio data to process</param>
        private void ProcessDeEmphasis(byte[] audioData)
        {
            float[][] floatAudioData = new float[2][];
            floatAudioData[0] = new float[audioData.Length / 4];
            floatAudioData[1] = new float[audioData.Length / 4];
            ByteConverter.ToFloats16Bit(audioData, floatAudioData);

            for(int i = 0; i < floatAudioData[0].Length; i++)
            {
                floatAudioData[0][i] = _deEmphasisFilterLeft.Process(floatAudioData[0][i]);
                floatAudioData[1][i] = _deEmphasisFilterRight.Process(floatAudioData[1][i]);
            }

            ByteConverter.FromFloats16Bit(floatAudioData, audioData);
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
            // Create padding data for overreads
            byte[] zeroSectors = new byte[(int)zeroSectorsAmount * _opticalDisc.BytesPerSector];
            byte[] audioData;

            // Attempt to read the required number of sectors
            var readSectorTask = Task.Run(() =>
            {
                lock(_readingImage)
                {
                    for(int i = 0; i < 4; i++)
                    {
                        try
                        {
                            return _opticalDisc.ReadSectors((uint)sectorsToRead).Concat(zeroSectors).ToArray();
                        }
                        catch { }
                    }

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
                ProcessDeEmphasis(audioDataSegment);

            return audioDataSegment;
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