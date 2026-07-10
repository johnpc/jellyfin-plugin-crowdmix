using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CrowdMix.LastFm
{
    /// <summary>
    /// HTTP-backed <see cref="ILastFmClient"/>. Thin: it builds request URLs, performs the GET,
    /// and delegates all parsing to <see cref="LastFmResponseParser"/>. Network/parse failures
    /// degrade to an empty result so the mix can fall back to the native Jellyfin behavior.
    /// </summary>
    public sealed class LastFmClient : ILastFmClient
    {
        private const string ApiRoot = "https://ws.audioscrobbler.com/2.0/";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LastFmClient> _logger;
        private readonly Func<string> _apiKeyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LastFmClient"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The Jellyfin-provided HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="apiKeyProvider">Supplies the current Last.fm API key from configuration.</param>
        public LastFmClient(
            IHttpClientFactory httpClientFactory,
            ILogger<LastFmClient> logger,
            Func<string> apiKeyProvider)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiKeyProvider = apiKeyProvider;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SimilarTrack>> GetSimilarTracksAsync(TrackKey seed, int limit, CancellationToken cancellationToken)
        {
            if (!seed.IsValid)
            {
                return Array.Empty<SimilarTrack>();
            }

            var url = BuildUrl("track.getsimilar", limit, ("artist", seed.Artist), ("track", seed.Title));
            var body = await GetAsync(url, cancellationToken).ConfigureAwait(false);
            return body == null ? Array.Empty<SimilarTrack>() : LastFmResponseParser.ParseSimilarTracks(body);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SimilarTrack>> GetSimilarArtistTracksAsync(string artist, int limit, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(artist))
            {
                return Array.Empty<SimilarTrack>();
            }

            var artistsUrl = BuildUrl("artist.getsimilar", limit, ("artist", artist));
            var artistsBody = await GetAsync(artistsUrl, cancellationToken).ConfigureAwait(false);
            if (artistsBody == null)
            {
                return Array.Empty<SimilarTrack>();
            }

            var results = new List<SimilarTrack>();
            foreach (var similarArtist in ParseSimilarArtistNames(artistsBody))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var topUrl = BuildUrl("artist.gettoptracks", 5, ("artist", similarArtist));
                var topBody = await GetAsync(topUrl, cancellationToken).ConfigureAwait(false);
                if (topBody != null)
                {
                    results.AddRange(LastFmResponseParser.ParseArtistTopTracks(topBody, similarArtist));
                }
            }

            return results;
        }

        private static IEnumerable<string> ParseSimilarArtistNames(string json)
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("similarartists", out var container)
                || !container.TryGetProperty("artist", out var artists)
                || artists.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var artist in artists.EnumerateArray())
            {
                if (artist.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                {
                    var value = name.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        yield return value;
                    }
                }
            }
        }

        private string BuildUrl(string method, int limit, params (string Key, string Value)[] parameters)
        {
            var builder = new System.Text.StringBuilder(ApiRoot);
            builder.Append("?method=").Append(method);
            foreach (var (key, value) in parameters)
            {
                builder.Append('&').Append(key).Append('=').Append(Uri.EscapeDataString(value));
            }

            builder.Append("&autocorrect=1&format=json&limit=").Append(limit.ToString(CultureInfo.InvariantCulture));
            builder.Append("&api_key=").Append(Uri.EscapeDataString(_apiKeyProvider()));
            return builder.ToString();
        }

        private async Task<string?> GetAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CrowdMix");
                using var response = await client.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CrowdMix: Last.fm returned {Status} for a request.", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "CrowdMix: Last.fm request failed.");
                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }
    }
}
