using System;
using System.Globalization;
using System.Text;

namespace Jellyfin.Plugin.CrowdMix.LastFm
{
    /// <summary>
    /// Normalizes artist and title strings so that trivially-different spellings
    /// (case, accents, "feat." credits, remaster/version suffixes) match the same catalog entry.
    /// This is the crux of mapping a local library to a crowd-signal catalog.
    /// </summary>
    public static class TrackNormalizer
    {
        private static readonly string[] TitleNoise =
        {
            " - remaster", " - remastered", "(remaster", "(remastered",
            " - single version", " - album version", " - radio edit",
            " - mono", " - stereo", " - live", "(live", " - deluxe",
            " - bonus track", "(bonus track", " - original mix",
        };

        private static readonly string[] FeatMarkers =
        {
            " feat.", " feat ", " ft.", " ft ", " featuring ",
        };

        /// <summary>
        /// Builds a normalized <see cref="TrackKey"/> from raw artist and title values.
        /// </summary>
        /// <param name="artist">The raw artist name.</param>
        /// <param name="title">The raw track title.</param>
        /// <returns>A normalized key.</returns>
        public static TrackKey ToKey(string? artist, string? title)
        {
            return new TrackKey(NormalizeArtist(artist), NormalizeTitle(title));
        }

        /// <summary>
        /// Normalizes an artist name: primary artist only, accents and case folded.
        /// </summary>
        /// <param name="artist">The raw artist name.</param>
        /// <returns>The normalized artist.</returns>
        public static string NormalizeArtist(string? artist)
        {
            var value = StripFeat(artist ?? string.Empty);
            return Fold(value);
        }

        /// <summary>
        /// Normalizes a track title: version/remaster noise and featured-artist credits removed.
        /// </summary>
        /// <param name="title">The raw track title.</param>
        /// <returns>The normalized title.</returns>
        public static string NormalizeTitle(string? title)
        {
            var value = (title ?? string.Empty).ToLowerInvariant();
            foreach (var noise in TitleNoise)
            {
                var idx = value.IndexOf(noise, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    value = value.Substring(0, idx);
                }
            }

            value = StripFeat(value);
            return Fold(value);
        }

        private static string StripFeat(string value)
        {
            var lower = value.ToLowerInvariant();
            foreach (var marker in FeatMarkers)
            {
                var idx = lower.IndexOf(marker, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    return value.Substring(0, idx);
                }
            }

            return value;
        }

        private static string Fold(string value)
        {
            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            var lastWasSpace = false;

            foreach (var ch in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    lastWasSpace = false;
                }
                else if (!lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }
            }

            return builder.ToString().Trim();
        }
    }
}
