using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using ReactiveUI;

namespace RedBookPlayer.Models.Discs
{
    public abstract class OpticalDiscBase : ReactiveObject
    {
        #region Public Fields

        /// <summary>
        /// Path to the disc image
        /// </summary>
        public string ImagePath { get; protected set; }

        /// <summary>
        /// Indicate if the disc is ready to be used
        /// </summary>
        public bool Initialized { get; protected set; } = false;

        /// <summary>
        /// Current track number
        /// </summary>
        public abstract int CurrentTrackNumber { get; protected set; }

        /// <summary>
        /// Current track index
        /// </summary>
        public abstract ushort CurrentTrackIndex { get; protected set; }

        /// <summary>
        /// Current track session
        /// </summary>
        public abstract ushort CurrentTrackSession { get; protected set; }

        /// <summary>
        /// Current sector number
        /// </summary>
        public abstract ulong CurrentSector { get; protected set; }

        /// <summary>
        /// Represents the sector starting the section
        /// </summary>
        public ulong SectionStartSector
        {
            get => _sectionStartSector;
            protected set => this.RaiseAndSetIfChanged(ref _sectionStartSector, value);
        }

        /// <summary>
        /// Number of bytes per sector for the current track
        /// </summary>
        public abstract int BytesPerSector { get; }

        /// <summary>
        /// Represents the track type
        /// </summary>
        public TrackType TrackType { get; protected set; }

        /// <summary>
        /// Represents the total tracks on the disc
        /// </summary>
        public int TotalTracks { get; protected set; } = 0;

        /// <summary>
        /// Represents the total indices on the disc
        /// </summary>
        public int TotalIndexes { get; protected set; } = 0;

        /// <summary>
        /// Total sectors in the image
        /// </summary>
        public ulong TotalSectors => _image?.Info.Sectors ?? 0;

        /// <summary>
        /// Represents the time adjustment offset for the disc
        /// </summary>
        public ulong TimeOffset { get; protected set; } = 0;

        /// <summary>
        /// Represents the total playing time for the disc
        /// </summary>
        public ulong TotalTime { get; protected set; } = 0;

        private ulong _sectionStartSector;

        #endregion

        #region Protected State Variables

        /// <summary>
        /// Currently loaded disc image
        /// </summary>
        protected IOpticalMediaImage _image;

        #endregion

        /// <summary>
        /// Initialize the disc with a given image
        /// </summary>
        /// <param name="path">Path of the image</param>
        /// <param name="image">Aaruformat image to load</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public abstract void Init(string path, IOpticalMediaImage image, bool autoPlay);

        #region Helpers

        /// <summary>
        /// Extract a track to WAV
        /// </summary>
        /// <param name="trackNumber">Track number to extract</param>
        /// <param name="outputDirectory">Output path to write data to</param>
        public abstract void ExtractTrackToWav(uint trackNumber, string outputDirectory);

        /// <summary>
        /// Load the desired track, if possible
        /// </summary>
        /// <param name="track">Track number to load</param>
        public abstract void LoadTrack(int track);

        /// <summary>
        /// Load the desired index, if possible
        /// </summary>
        /// <param name="index">Index number to load</param>
        public abstract void LoadIndex(ushort index);

        /// <summary>
        /// Read sector data from the base image starting from the current sector
        /// </summary>
        /// <param name="sectorsToRead">Current number of sectors to read</param>
        /// <returns>Byte array representing the read sectors, if possible</returns>
        public virtual byte[] ReadSectors(uint sectorsToRead) => _image.ReadSectors(CurrentSector, sectorsToRead);

        /// <summary>
        /// Set the total indexes from the current track
        /// </summary>
        public abstract void SetTotalIndexes();

        /// <summary>
        /// Set the current sector
        /// </summary>
        /// <param name="sector">New sector number to use</param>
        public void SetCurrentSector(ulong sector) => CurrentSector = sector;

        #endregion
    }
}