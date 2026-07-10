using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CrowdMix.LastFm;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Default <see cref="IMixEngine"/>: candidate generation from the Last.fm crowd signal
    /// (track-level similarity, widened by similar-artist top tracks when sparse), then
    /// hybrid re-ranking against the owned library.
    /// </summary>
    public sealed class MixEngine : IMixEngine
    {
        private const int ArtistWidenThreshold = 8;

        private readonly ILastFmClient _client;
        private readonly MixReranker _reranker;

        /// <summary>
        /// Initializes a new instance of the <see cref="MixEngine"/> class.
        /// </summary>
        /// <param name="client">The Last.fm crowd-signal client.</param>
        /// <param name="reranker">The hybrid re-ranker.</param>
        public MixEngine(ILastFmClient client, MixReranker reranker)
        {
            _client = client;
            _reranker = reranker;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Guid>> BuildMixAsync(
            OwnedTrack seed,
            IReadOnlyDictionary<TrackKey, OwnedTrack> ownedByKey,
            int limit,
            CancellationToken cancellationToken)
        {
            if (seed == null || !seed.Key.IsValid || limit <= 0)
            {
                return Array.Empty<Guid>();
            }

            // Request generously: many similar tracks won't be in the user's library.
            var fetch = Math.Max(limit * 4, 40);
            var candidates = new List<SimilarTrack>(
                await _client.GetSimilarTracksAsync(seed.Key, fetch, cancellationToken).ConfigureAwait(false));

            if (CountOwnedMatches(candidates, ownedByKey, seed.Id) < ArtistWidenThreshold)
            {
                candidates.AddRange(
                    await _client.GetSimilarArtistTracksAsync(seed.Key.Artist, fetch, cancellationToken).ConfigureAwait(false));
            }

            return _reranker.Rank(candidates, ownedByKey, seed.Genres, seed.Id, limit);
        }

        private static int CountOwnedMatches(
            IEnumerable<SimilarTrack> candidates,
            IReadOnlyDictionary<TrackKey, OwnedTrack> ownedByKey,
            Guid seedId)
        {
            var seen = new HashSet<Guid>();
            foreach (var candidate in candidates)
            {
                if (ownedByKey.TryGetValue(candidate.Key, out var owned) && owned.Id != seedId)
                {
                    seen.Add(owned.Id);
                }
            }

            return seen.Count;
        }
    }
}
