namespace RedBookPlayer.Models
{
    /// <summary>
    /// Determine how to handle data tracks
    /// </summary>
    public enum DataPlayback
    {
        /// <summary>
        /// Skip playing all data tracks
        /// </summary>
        Skip = 0,

        /// <summary>
        /// Play silence for all data tracks
        /// </summary>
        Blank = 1,

        /// <summary>
        /// Play the data from all data tracks
        /// </summary>
        Play = 2,
    }

    /// <summary>
    /// Determine how to handle multiple discs
    /// </summary>
    /// <remarks>Used with both repeat and shuffle</remarks>
    public enum DiscHandling
    {
        /// <summary>
        /// Only deal with tracks on the current disc
        /// </summary>
        SingleDisc = 0,

        /// <summary>
        /// Deal with tracks on all loaded discs
        /// </summary>
        MultiDisc = 1,
    }

    /// <summary>
    /// Current player state
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// No disc is loaded
        /// </summary>
        NoDisc,

        /// <summary>
        /// Disc is loaded, playback is stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Disc is loaded, playback is paused
        /// </summary>
        Paused,

        /// <summary>
        /// Disc is loaded, playback enabled
        /// </summary>
        Playing,
    }

    /// <summary>
    /// Playback repeat mode
    /// </summary>
    public enum RepeatMode
    {
        /// <summary>
        /// No repeat
        /// </summary>
        None,

        /// <summary>
        /// Repeat a single track
        /// </summary>
        Single,

        /// <summary>
        /// Repeat all tracks
        /// </summary>
        All,
    }

    /// <summary>
    /// Determine how to handle different sessions
    /// </summary>
    public enum SessionHandling
    {
        /// <summary>
        /// Allow playing tracks from all sessions
        /// </summary>
        AllSessions = 0,

        /// <summary>
        /// Only play tracks from the first session
        /// </summary>
        FirstSessionOnly = 1,
    }

    /// <summary>
    /// Known set of subchannel instructions
    /// </summary>
    /// <see cref="https://jbum.com/cdg_revealed.html"/>
    public enum SubchannelInstruction : byte
    {
        /// <summary>
        /// Set the screen to a particular color.
        /// </summary>
        MemoryPreset            = 1,

        /// <summary>
        /// Set the border of the screen to a particular color.
        /// </summary>
        BorderPreset            = 2,

        /// <summary>
        /// Load a 12 x 6, 2 color tile and display it normally.
        /// </summary>
        TileBlockNormal         = 6,

        /// <summary>
        /// Scroll the image, filling in the new area with a color.
        /// </summary>
        ScrollPreset            = 20,

        /// <summary>
        /// Scroll the image, rotating the bits back around.
        /// </summary>
        ScrollCopy              = 24,

        /// <summary>
        /// Define a specific color as being transparent.
        /// </summary>
        DefineTransparentColor  = 28,

        /// <summary>
        /// Load in the lower 8 entries of the color table.
        /// </summary>
        LoadColorTableLower     = 30,

        /// <summary>
        /// Load in the upper 8 entries of the color table.
        /// </summary>
        LoadColorTableUpper     = 31,

        /// <summary>
        /// Load a 12 x 6, 2 color tile and display it using the XOR method.
        /// </summary>
        TileBlockXOR            = 38,
    }
}