namespace RedBookPlayer.Models.Discs
{
    public class OpticalDiscOptions
    {
        #region CompactDisc

        /// <summary>
        /// Indicate how data tracks should be handled
        /// </summary>
        public DataPlayback DataPlayback { get; set; } = DataPlayback.Skip;

        /// <summary>
        /// Indicate if a TOC should be generated if missing
        /// </summary>
        public bool GenerateMissingToc { get; set; } = false;

        /// <summary>
        /// Indicate if hidden tracks should be loaded
        /// </summary>
        public bool LoadHiddenTracks { get; set; } = false;

        /// <summary>
        /// Indicates how tracks on different session should be handled
        /// </summary>
        public SessionHandling SessionHandling { get; set; } = SessionHandling.AllSessions;

        #endregion
    }
}
