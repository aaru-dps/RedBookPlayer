using System;
using System.Linq;
using System.Threading.Tasks;
using CSCore.SoundOut;
using NWaves.Audio;
using NWaves.Filters.BiQuad;
using ReactiveUI;
using RedBookPlayer.Common.Discs;

namespace RedBookPlayer.Common.Hardware
{
    public class SoundOutput : ReactiveObject
    {
        #region Public Fields

        /// <summary>
        /// Indicate if the output is ready to be used
        /// </summary>
        public bool Initialized { get; private set; } = false;

        /// <summary>
        /// Indicate if the output is playing
        /// </summary>
        public bool Playing
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

        private bool _playing;
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
        private OpticalDisc _opticalDisc;

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
        /// Initialize the output with a given image
        /// </summary>
        /// <param name="opticalDisc">OpticalDisc to load from</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        /// <param name="defaultVolume">Default volume between 0 and 100 to use when starting playback</param>
        public void Init(OpticalDisc opticalDisc, bool autoPlay = false, int defaultVolume = 100)
        {
            // If we have an unusable disc, just return
            if(opticalDisc == null || !opticalDisc.Initialized)
                return;

            // Save a reference to the disc
            _opticalDisc = opticalDisc;

            // Set the initial playback volume
            Volume = defaultVolume;

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
            if (_opticalDisc.BytesPerSector <= 0)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            // Determine how many sectors we can read
            ulong sectorsToRead;
            ulong zeroSectorsAmount;
            do
            {
                // Attempt to read 2 more sectors than requested
                sectorsToRead = ((ulong)count / (ulong)_opticalDisc.BytesPerSector) + 2;
                zeroSectorsAmount = 0;

                // Avoid overreads by padding with 0-byte data at the end
                if(_opticalDisc.CurrentSector + sectorsToRead > _opticalDisc.TotalSectors)
                {
                    ulong oldSectorsToRead = sectorsToRead;
                    sectorsToRead = _opticalDisc.TotalSectors - _opticalDisc.CurrentSector;

                    int tempZeroSectorCount = (int)(oldSectorsToRead - sectorsToRead);
                    zeroSectorsAmount = (ulong)(tempZeroSectorCount < 0 ? 0 : tempZeroSectorCount);
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
                    for(int i = 0; i < 4; i++)
                    {
                        try
                        {
                            return _opticalDisc.ReadSectors((uint)sectorsToRead).Concat(zeroSectors).ToArray();
                        }
                        catch(ArgumentOutOfRangeException)
                        {
                            _opticalDisc.LoadFirstTrack();
                        }
                    }

                    return zeroSectors;
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
            int copyAmount = Math.Min(count, audioData.Length - _currentSectorReadPosition);
            if(Math.Max(0, copyAmount) == 0)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            Array.Copy(audioData, _currentSectorReadPosition, audioDataSegment, 0, copyAmount);

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
            if (_soundOut.PlaybackState != PlaybackState.Playing)
                _soundOut.Play();

            Playing = _soundOut.PlaybackState == PlaybackState.Playing;
        }

        /// <summary>
        /// Pause audio playback
        /// </summary>
        public void Pause()
        {
            if(_soundOut.PlaybackState != PlaybackState.Paused)
                _soundOut.Pause();

            Playing = _soundOut.PlaybackState == PlaybackState.Playing;
        }

        /// <summary>
        /// Stop audio playback
        /// </summary>
        public void Stop()
        {
            if(_soundOut.PlaybackState != PlaybackState.Stopped)
                _soundOut.Stop();

            Playing = _soundOut.PlaybackState == PlaybackState.Playing;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Set de-emphasis status
        /// </summary>
        /// <param name="apply"></param>
        public void SetDeEmphasis(bool apply) => ApplyDeEmphasis = apply;

        /// <summary>
        /// Set the value for the volume
        /// </summary>
        /// <param name="volume">New volume value</param>
        public void SetVolume(int volume) => Volume = volume;

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
