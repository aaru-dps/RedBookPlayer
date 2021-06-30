using System;
using System.IO;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.DiscImages;
using Aaru.Filters;
using RedBookPlayer.Discs;
using RedBookPlayer.GUI;

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
        /// Indicate if the disc is playing
        /// </summary>
        public bool Playing => _soundOutput?.Playing ?? false;

        /// <summary>
        /// Indicates if de-emphasis should be applied
        /// </summary>
        public bool ApplyDeEmphasis => _soundOutput?.ApplyDeEmphasis ?? false;

        #endregion

        #region Private State Variables

        /// <summary>
        /// OpticalDisc object
        /// </summary>
        private OpticalDisc _opticalDisc;

        /// <summary>
        /// Sound output handling class
        /// </summary>
        public SoundOutput _soundOutput;

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
            _soundOutput = new SoundOutput();
            _soundOutput.ApplyDeEmphasis = false;
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

            // Initialize the sound output
            _soundOutput.Init(_opticalDisc, autoPlay);
            if(_soundOutput == null || !_soundOutput.Initialized)
                return;

            // Mark the player as ready
            Initialized = true;
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
                _soundOutput.Play();
                _opticalDisc.SetTotalIndexes();
            }
            else
            {
                _soundOutput.Stop();
            }
        }

        /// <summary>
        /// Stop the current audio playback
        /// </summary>
        public void Stop()
        {
            if(_opticalDisc == null || !_opticalDisc.Initialized)
                return;

            _soundOutput.Stop();
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
                _soundOutput.ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

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
                _soundOutput.ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

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
                _soundOutput.ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

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
                _soundOutput.ApplyDeEmphasis = compactDisc.TrackHasEmphasis;

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
        public void ToggleDeEmphasis(bool enable) => _soundOutput?.ToggleDeEmphasis(enable);

        /// <summary>
        /// Update the data context for the frontend
        /// </summary>
        /// <param name="dataContext">Data context to be updated</param>
        public void UpdateDataContext(PlayerViewModel dataContext)
        {
            if(!Initialized || dataContext == null)
                return;

            dataContext.HiddenTrack = _opticalDisc.TimeOffset > 150;
            dataContext.ApplyDeEmphasis = _soundOutput.ApplyDeEmphasis;

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

        #endregion
    }
}