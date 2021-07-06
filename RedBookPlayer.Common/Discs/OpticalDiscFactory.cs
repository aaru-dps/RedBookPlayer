using System.IO;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Metadata;
using Aaru.DiscImages;
using Aaru.Filters;

namespace RedBookPlayer.Common.Discs
{
    public static class OpticalDiscFactory
    {
        /// <summary>
        /// Generate an OpticalDisc from an input path
        /// </summary>
        /// <param name="path">Path to load the image from</param>
        /// <param name="generateMissingToc">Generate a TOC if the disc is missing one [CompactDisc only]</param>
        /// <param name="loadHiddenTracks">Load hidden tracks for playback [CompactDisc only]</param>
        /// <param name="loadDataTracks">Load data tracks for playback [CompactDisc only]</param>
        /// <param name="autoPlay">True if the image should be playable immediately, false otherwise</param>
        /// <returns>Instantiated OpticalDisc, if possible</returns>
        public static OpticalDisc GenerateFromPath(string path, bool generateMissingToc, bool loadHiddenTracks, bool loadDataTracks, bool autoPlay)
        {
            try
            {
                // Validate the image exists
                if(string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return null;

                // Load the disc image to memory
                // TODO: Assumes Aaruformat right now for all
                var image = new AaruFormat();
                var filter = new ZZZNoFilter();
                filter.Open(path);
                image.Open(filter);

                // Generate and instantiate the disc
                return GenerateFromImage(image, generateMissingToc, loadHiddenTracks, loadDataTracks, autoPlay);
            }
            catch
            {
                // All errors mean an invalid image in some way
                return null;
            }
        }

        /// <summary>
        /// Generate an OpticalDisc from an input IOpticalMediaImage
        /// </summary>
        /// <param name="image">IOpticalMediaImage to create from</param>
        /// <param name="generateMissingToc">Generate a TOC if the disc is missing one [CompactDisc only]</param>
        /// <param name="loadHiddenTracks">Load hidden tracks for playback [CompactDisc only]</param>
        /// <param name="loadDataTracks">Load data tracks for playback [CompactDisc only]</param>
        /// <param name="autoPlay">True if the image should be playable immediately, false otherwise</param>
        /// <returns>Instantiated OpticalDisc, if possible</returns>
        public static OpticalDisc GenerateFromImage(IOpticalMediaImage image, bool generateMissingToc, bool loadHiddenTracks, bool loadDataTracks, bool autoPlay)
        {
            // If the image is not usable, we don't do anything
            if(!IsUsableImage(image))
                return null;

            // Create the output object
            OpticalDisc opticalDisc;

            // Create the proper disc type
            switch(GetMediaType(image))
            {
                case "Compact Disc":
                case "GD":
                    opticalDisc = new CompactDisc(generateMissingToc, loadHiddenTracks, loadDataTracks);
                    break;
                default:
                    opticalDisc = null;
                    break;
            }

            // Null image means we don't do anything
            if(opticalDisc == null)
                return opticalDisc;

            // Instantiate the disc and return
            opticalDisc.Init(image, autoPlay);
            return opticalDisc;
        }

        /// <summary>
        /// Gets the human-readable media type from an image
        /// </summary>
        /// <param name="image">Media image to check</param>
        /// <returns>Type from the image, empty string on error</returns>
        /// <remarks>TODO: Can we be more granular with sub types?</remarks>
        private static string GetMediaType(IOpticalMediaImage image)
        {
            // Null image means we don't do anything
            if(image == null)
                return string.Empty;

            (string type, string _) = MediaType.MediaTypeToString(image.Info.MediaType);
            return type;
        }

        /// <summary>
        /// Indicates if the image is considered "usable" or not
        /// </summary>
        /// <param name="image">Aaruformat image file</param>
        /// <returns>True if the image is playble, false otherwise</returns>
        private static bool IsUsableImage(IOpticalMediaImage image)
        {
            // Invalid images can't be used
            if(image == null)
                return false;

            // Determine based on media type
            return GetMediaType(image) switch
            {
                "Compact Disc" => true,
                "GD" => true, // Requires TOC generation
                _ => false,
            };
        }
    }
}
