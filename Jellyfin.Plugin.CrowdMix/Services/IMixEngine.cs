using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CrowdMix.LastFm;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Produces an ordered list of owned track ids for an instant mix around a seed track,
    /// using the crowd signal plus local re-ranking. Backend-agnostic and free of Jellyfin
    /// entity types so it can be unit-tested end-to-end.
    /// </summary>
    public interface IMixEngine
    {
        /// <summary>
        /// Builds a ranked instant mix for a seed track.
        /// </summary>
        /// <param name="seed">The owned seed track.</param>
        /// <param name="ownedByKey">All owned tracks indexed by normalized key.</param>
        /// <param name="limit">The desired mix size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Ordered owned track ids; empty when the crowd signal yields no owned matches.</returns>
        Task<IReadOnlyList<Guid>> BuildMixAsync(
            OwnedTrack seed,
            IReadOnlyDictionary<TrackKey, OwnedTrack> ownedByKey,
            int limit,
            CancellationToken cancellationToken);
    }
}
