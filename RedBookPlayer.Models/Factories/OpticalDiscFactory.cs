using System.IO;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Metadata;
using Aaru.DiscImages;
using Aaru.Filters;
using RedBookPlayer.Models.Discs;

namespace RedBookPlayer.Models.Factories
{
    public static class OpticalDiscFactory
    {
        /// <summary>
        /// Generate an OpticalDisc from an input path
        /// </summary>
        /// <param name="path">Path to load the image from</param>
        /// <param name="options">Options to pass to the optical disc factory</param>
        /// <param name="autoPlay">True if the image should be playable immediately, false otherwise</param>
        /// <returns>Instantiated OpticalDisc, if possible</returns>
        public static OpticalDiscBase GenerateFromPath(string path, OpticalDiscOptions options, bool autoPlay)
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
                return GenerateFromImage(image, options, autoPlay);
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
        /// <param name="options">Options to pass to the optical disc factory</param>
        /// <param name="autoPlay">True if the image should be playable immediately, false otherwise</param>
        /// <returns>Instantiated OpticalDisc, if possible</returns>
        public static OpticalDiscBase GenerateFromImage(IOpticalMediaImage image, OpticalDiscOptions options, bool autoPlay)
        {
            // If the image is not usable, we don't do anything
            if(!IsUsableImage(image))
                return null;

            // Create the output object
            OpticalDiscBase opticalDisc;

            // Create the proper disc type
            switch(GetMediaType(image))
            {
                case "Compact Disc":
                case "GD":
                    opticalDisc = new CompactDisc(options);
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
