namespace RedBookPlayer.Models.Discs
{
    public class OpticalDiscOptions
    {
        #region CompactDisc

        /// <summary>
        /// Indicate if a TOC should be generated if missing
        /// </summary>
        public bool GenerateMissingToc { get; set; } = false;

        #endregion
    }
}