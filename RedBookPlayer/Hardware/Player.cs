using System;
using System.IO;
using Aaru.DiscImages;
using Aaru.Filters;
using RedBookPlayer.Discs;

namespace RedBookPlayer.Hardware
{
    public class Player
    {
        #region Public Fields

        /// <summary>
        /// Indicate if the player is ready to be used
        /// </summary>
        public bool Initialized { get; private set; } = false;

        /// <summary>
        /// OpticalDisc object
        /// </summary>
        public OpticalDisc OpticalDisc { get; private set; }

        /// <summary>
        /// Indicate if the disc is playing
        /// </summary>
        public bool? Playing
        {
            get => _soundOutput?.Playing;
            set
            {
                if(OpticalDisc == null || !OpticalDisc.Initialized)
                    return;

                // If the playing state has not changed, do nothing
                if(value == _soundOutput?.Playing)
                    return;

                if(value == true)
                {
                    _soundOutput.Play();
                    OpticalDisc.SetTotalIndexes();
                }
                else if(value == false)
                {
                    _soundOutput.Stop();
                }
                else
                {
                    _soundOutput.Stop();
                    OpticalDisc.LoadFirstTrack();
                }
            }
        }

        /// <summary>
        /// Indicate the current playback volume
        /// </summary>
        public int Volume
        {
            get => _soundOutput?.Volume ?? 100;
            set
            {
                if(_soundOutput != null)
                    _soundOutput.Volume = value;
            }
        }

        /// <summary>
        /// Indicates if de-emphasis should be applied
        /// </summary>
        public bool ApplyDeEmphasis
        {
            get => _soundOutput?.ApplyDeEmphasis ?? false;
            set => _soundOutput?.SetDeEmphasis(value);
        }

        #endregion

        #region Private State Variables

        /// <summary>
        /// Sound output handling class
        /// </summary>
        public SoundOutput _soundOutput;

        #endregion

        /// <summary>
        /// Create a new Player from a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        /// <param name="defaultVolume">Default volume between 0 and 100 to use when starting playback</param>
        public Player(string path, bool autoPlay = false, int defaultVolume = 100)
        {
            // Set the internal state for initialization
            Initialized = false;
            _soundOutput = new SoundOutput();
            _soundOutput.ApplyDeEmphasis = false;
            OpticalDisc = null;

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
                OpticalDisc = OpticalDiscFactory.GenerateFromImage(image, autoPlay);
            }
            catch
            {
                // All errors mean an invalid image in some way
                return;
            }

            // Initialize the sound output
            _soundOutput.Init(OpticalDisc, autoPlay, defaultVolume);
            if(_soundOutput == null || !_soundOutput.Initialized)
                return;

            // Mark the player as ready
            Initialized = true;
        }

        #region Playback

        /// <summary>
        /// Move to the next playable track
        /// </summary>
        public void NextTrack()
        {
            if(OpticalDisc == null || !OpticalDisc.Initialized)
                return;

            bool? wasPlaying = Playing;
            if(wasPlaying == true) Playing = false;

            OpticalDisc.NextTrack();
            if(OpticalDisc is CompactDisc compactDisc)
                _soundOutput.ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            if(wasPlaying == true) Playing = true;
        }

        /// <summary>
        /// Move to the previous playable track
        /// </summary>
        public void PreviousTrack()
        {
            if(OpticalDisc == null || !OpticalDisc.Initialized)
                return;

            bool? wasPlaying = Playing;
            if(wasPlaying == true) Playing = false;

            OpticalDisc.PreviousTrack();
            if(OpticalDisc is CompactDisc compactDisc)
                _soundOutput.ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            if(wasPlaying == true) Playing = true;
        }

        /// <summary>
        /// Move to the next index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void NextIndex(bool changeTrack)
        {
            if(OpticalDisc == null || !OpticalDisc.Initialized)
                return;

            bool? wasPlaying = Playing;
            if(wasPlaying == true) Playing = false;

            OpticalDisc.NextIndex(changeTrack);
            if(OpticalDisc is CompactDisc compactDisc)
                _soundOutput.ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            if(wasPlaying == true) Playing = true;
        }

        /// <summary>
        /// Move to the previous index
        /// </summary>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void PreviousIndex(bool changeTrack)
        {
            if(OpticalDisc == null || !OpticalDisc.Initialized)
                return;

            bool? wasPlaying = Playing;
            if(wasPlaying == true) Playing = false;

            OpticalDisc.PreviousIndex(changeTrack);
            if(OpticalDisc is CompactDisc compactDisc)
                _soundOutput.ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

            if(wasPlaying == true) Playing = true;
        }

        /// <summary>
        /// Fast-forward playback by 75 sectors, if possible
        /// </summary>
        public void FastForward()
        {
            if(OpticalDisc == null || !OpticalDisc.Initialized)
                return;

            OpticalDisc.CurrentSector = Math.Min(OpticalDisc.TotalSectors, OpticalDisc.CurrentSector + 75);
        }

        /// <summary>
        /// Rewind playback by 75 sectors, if possible
        /// </summary>
        public void Rewind()
        {
            if(OpticalDisc == null || !OpticalDisc.Initialized)
                return;

            if(OpticalDisc.CurrentSector >= 75)
                OpticalDisc.CurrentSector -= 75;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Get current sector time, accounting for offsets
        /// </summary>
        /// <returns>ulong representing the current sector time</returns>
        public ulong GetCurrentSectorTime()
        {
            ulong sectorTime = OpticalDisc.CurrentSector;
            if (OpticalDisc.SectionStartSector != 0)
                sectorTime -= OpticalDisc.SectionStartSector;
            else
                sectorTime += OpticalDisc.TimeOffset;

            return sectorTime;
        }

        /// <summary>
        /// Set if de-emphasis should be applied
        /// </summary>
        /// <param name="apply">True to enable, false to disable</param>
        public void SetDeEmphasis(bool apply) => _soundOutput?.SetDeEmphasis(apply);

        #endregion
    }
}