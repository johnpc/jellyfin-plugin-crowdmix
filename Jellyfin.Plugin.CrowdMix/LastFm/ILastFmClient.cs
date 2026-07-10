using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.CrowdMix.LastFm
{
    /// <summary>
    /// Reads the Last.fm crowd signal: tracks that a large listening population plays
    /// alongside a given seed. This is the collaborative-filtering pillar that pure
    /// audio-embedding approaches can only approximate.
    /// </summary>
    public interface ILastFmClient
    {
        /// <summary>
        /// Gets tracks similar to the seed track, ordered by descending match score.
        /// </summary>
        /// <param name="seed">The normalized seed track key.</param>
        /// <param name="limit">The maximum number of results to request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Similar tracks; empty if the seed is unknown to Last.fm.</returns>
        Task<IReadOnlyList<SimilarTrack>> GetSimilarTracksAsync(TrackKey seed, int limit, CancellationToken cancellationToken);

        /// <summary>
        /// Gets top tracks for artists similar to the seed's artist. Used to widen the
        /// candidate pool when track-level similarity is sparse.
        /// </summary>
        /// <param name="artist">The normalized artist name.</param>
        /// <param name="limit">The maximum number of results to request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Similar tracks derived from similar artists.</returns>
        Task<IReadOnlyList<SimilarTrack>> GetSimilarArtistTracksAsync(string artist, int limit, CancellationToken cancellationToken);
    }
}
