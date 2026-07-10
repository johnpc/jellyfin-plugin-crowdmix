using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.CrowdMix.LastFm;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Fuses the crowd-similarity signal with local personalization signals (tag overlap,
    /// play affinity, favorites) to order candidate tracks. Pure and deterministic so it is
    /// fully unit-testable — this is where CrowdMix beats a pure audio-embedding mix for
    /// the individual user.
    /// </summary>
    public sealed class MixReranker
    {
        private readonly MixWeights _weights;

        /// <summary>
        /// Initializes a new instance of the <see cref="MixReranker"/> class.
        /// </summary>
        /// <param name="weights">The scoring weights.</param>
        public MixReranker(MixWeights weights)
        {
            _weights = weights;
        }

        /// <summary>
        /// Ranks owned tracks that match the crowd candidates, best first.
        /// </summary>
        /// <param name="candidates">Scored candidates from the crowd signal (may contain duplicates).</param>
        /// <param name="ownedByKey">Owned tracks indexed by their normalized key.</param>
        /// <param name="seedGenres">The seed track's genres, for tag-overlap scoring.</param>
        /// <param name="excludeSeedId">The seed track id to exclude from results.</param>
        /// <param name="limit">The maximum number of results to return.</param>
        /// <returns>Ordered owned track ids.</returns>
        public IReadOnlyList<Guid> Rank(
            IEnumerable<SimilarTrack> candidates,
            IReadOnlyDictionary<TrackKey, OwnedTrack> ownedByKey,
            IReadOnlyCollection<string> seedGenres,
            Guid excludeSeedId,
            int limit)
        {
            var seedGenreSet = new HashSet<string>(seedGenres, StringComparer.OrdinalIgnoreCase);
            var best = new Dictionary<Guid, double>();
            var artistById = new Dictionary<Guid, string>();

            foreach (var candidate in candidates)
            {
                if (!ownedByKey.TryGetValue(candidate.Key, out var owned) || owned.Id == excludeSeedId)
                {
                    continue;
                }

                var score = Score(candidate, owned, seedGenreSet);
                if (!best.TryGetValue(owned.Id, out var existing) || score > existing)
                {
                    best[owned.Id] = score;
                    artistById[owned.Id] = owned.Key.Artist;
                }
            }

            return SelectWithArtistCap(best, artistById, limit);
        }

        private static double TagOverlap(IReadOnlyCollection<string> genres, HashSet<string> seedGenreSet)
        {
            if (seedGenreSet.Count == 0 || genres.Count == 0)
            {
                return 0d;
            }

            var matches = genres.Count(seedGenreSet.Contains);
            return (double)matches / seedGenreSet.Count;
        }

        private static double PlayAffinity(int playCount)
        {
            // Saturating curve: repeated plays help but never dominate the crowd signal.
            return playCount <= 0 ? 0d : Math.Log(1 + playCount) / Math.Log(11);
        }

        private List<Guid> SelectWithArtistCap(
            Dictionary<Guid, double> scoreById,
            Dictionary<Guid, string> artistById,
            int limit)
        {
            var cap = _weights.MaxTracksPerArtist;
            var result = new List<Guid>();
            var perArtist = new Dictionary<string, int>(StringComparer.Ordinal);
            var take = limit < 0 ? 0 : limit;

            foreach (var pair in scoreById.OrderByDescending(entry => entry.Value))
            {
                if (result.Count >= take)
                {
                    break;
                }

                var artist = artistById[pair.Key];
                if (cap > 0 && artist.Length > 0)
                {
                    perArtist.TryGetValue(artist, out var used);
                    if (used >= cap)
                    {
                        continue;
                    }

                    perArtist[artist] = used + 1;
                }

                result.Add(pair.Key);
            }

            return result;
        }

        private double Score(SimilarTrack candidate, OwnedTrack owned, HashSet<string> seedGenreSet)
        {
            var score = _weights.CrowdSimilarity * candidate.Score;
            score += _weights.TagOverlap * TagOverlap(owned.Genres, seedGenreSet);
            score += _weights.PlayAffinity * PlayAffinity(owned.PlayCount);
            if (owned.IsFavorite)
            {
                score += _weights.FavoriteBonus;
            }

            return score;
        }
    }
}
