using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Jellyfin.Plugin.CrowdMix.LastFm
{
    /// <summary>
    /// Pure parser for the Last.fm JSON responses this plugin consumes. Kept separate from
    /// the HTTP client so the (branch-heavy) parsing logic is unit-testable without a network.
    /// </summary>
    public static class LastFmResponseParser
    {
        /// <summary>
        /// Parses a <c>track.getSimilar</c> response body into scored similar tracks.
        /// </summary>
        /// <param name="json">The raw JSON response body.</param>
        /// <returns>The parsed similar tracks; empty on any missing/invalid shape.</returns>
        public static IReadOnlyList<SimilarTrack> ParseSimilarTracks(string json)
        {
            var results = new List<SimilarTrack>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return results;
            }

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("similartracks", out var container)
                || !container.TryGetProperty("track", out var tracks)
                || tracks.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            foreach (var track in tracks.EnumerateArray())
            {
                var name = ReadString(track, "name");
                var artist = ReadArtistName(track);
                if (name.Length == 0 || artist.Length == 0)
                {
                    continue;
                }

                var key = TrackNormalizer.ToKey(artist, name);
                results.Add(new SimilarTrack(key, ReadMatch(track)));
            }

            return results;
        }

        /// <summary>
        /// Parses an <c>artist.getTopTracks</c> response into scored tracks. Since these responses
        /// carry playcount rather than a similarity match, the score is a rank-decayed value in (0..1]
        /// so artist-derived candidates rank below true track-level similar tracks.
        /// </summary>
        /// <param name="json">The raw JSON response body.</param>
        /// <param name="artist">The artist these top tracks belong to.</param>
        /// <returns>The parsed tracks; empty on any missing/invalid shape.</returns>
        public static IReadOnlyList<SimilarTrack> ParseArtistTopTracks(string json, string artist)
        {
            var results = new List<SimilarTrack>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return results;
            }

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("toptracks", out var container)
                || !container.TryGetProperty("track", out var tracks)
                || tracks.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            var rank = 0;
            foreach (var track in tracks.EnumerateArray())
            {
                var name = ReadString(track, "name");
                if (name.Length == 0)
                {
                    continue;
                }

                rank++;
                var score = 1d / (1d + rank);
                results.Add(new SimilarTrack(TrackNormalizer.ToKey(artist, name), score));
            }

            return results;
        }

        private static string ReadArtistName(JsonElement track)
        {
            if (!track.TryGetProperty("artist", out var artist))
            {
                return string.Empty;
            }

            // track.getSimilar nests artist as an object; artist.getTopTracks as a string.
            return artist.ValueKind == JsonValueKind.Object
                ? ReadString(artist, "name")
                : (artist.ValueKind == JsonValueKind.String ? artist.GetString() ?? string.Empty : string.Empty);
        }

        private static double ReadMatch(JsonElement track)
        {
            if (!track.TryGetProperty("match", out var match))
            {
                return 0d;
            }

            if (match.ValueKind == JsonValueKind.Number)
            {
                return match.GetDouble();
            }

            if (match.ValueKind == JsonValueKind.String
                && double.TryParse(match.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return 0d;
        }

        private static string ReadString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
