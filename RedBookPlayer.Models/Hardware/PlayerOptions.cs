namespace RedBookPlayer.Models.Discs
{
    public class PlayerOptions
    {
        /// <summary>
        /// Indicate how data tracks should be handled
        /// </summary>
        public DataPlayback DataPlayback { get; set; } = DataPlayback.Skip;

        /// <summary>
        /// Indicate if hidden tracks should be loaded
        /// </summary>
        public bool LoadHiddenTracks { get; set; } = false;

        /// <summary>
        /// Indicates the repeat mode
        /// </summary>
        public RepeatMode RepeatMode { get; set; } = RepeatMode.None;

        /// <summary>
        /// Indicates how tracks on different session should be handled
        /// </summary>
        public SessionHandling SessionHandling { get; set; } = SessionHandling.AllSessions;
    }
}