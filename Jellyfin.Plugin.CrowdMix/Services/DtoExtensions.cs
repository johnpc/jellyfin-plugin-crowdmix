using System.Collections.Generic;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Helpers for shaping <see cref="DtoOptions"/> from the query parameters the native
    /// Instant Mix endpoint received, so CrowdMix returns DTOs identical in shape to the
    /// native response.
    /// </summary>
    public static class DtoExtensions
    {
        /// <summary>
        /// Applies image/user-data options passed to the Instant Mix endpoint.
        /// </summary>
        /// <param name="dtoOptions">The options to mutate.</param>
        /// <param name="enableImages">Whether images are enabled.</param>
        /// <param name="enableUserData">Whether user data is enabled.</param>
        /// <param name="imageTypeLimit">The image type limit.</param>
        /// <param name="enableImageTypes">The enabled image types.</param>
        /// <returns>The mutated options.</returns>
        public static DtoOptions AddAdditionalDtoOptions(
            this DtoOptions dtoOptions,
            bool? enableImages,
            bool? enableUserData,
            int? imageTypeLimit,
            IReadOnlyList<ImageType> enableImageTypes)
        {
            dtoOptions.EnableImages = enableImages ?? true;

            if (imageTypeLimit.HasValue)
            {
                dtoOptions.ImageTypeLimit = imageTypeLimit.Value;
            }

            if (enableUserData.HasValue)
            {
                dtoOptions.EnableUserData = enableUserData.Value;
            }

            if (enableImageTypes.Count != 0)
            {
                dtoOptions.ImageTypes = enableImageTypes;
            }

            return dtoOptions;
        }
    }
}
