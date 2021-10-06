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
}