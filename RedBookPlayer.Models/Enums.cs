namespace RedBookPlayer.Models
{
    /// <summary>
    /// Determine how to handle data tracks
    /// </summary>
    /// TODO: Add extra flags/options for sessions (only first session, all sessions)
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
}
