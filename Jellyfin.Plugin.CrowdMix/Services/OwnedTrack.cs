using System;
using System.Collections.Generic;
using Jellyfin.Plugin.CrowdMix.LastFm;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// A track the user actually owns in their Jellyfin library, reduced to just the fields
    /// CrowdMix needs to match against the crowd signal and re-rank. Decoupled from Jellyfin's
    /// <c>BaseItem</c> so the matching/ranking logic is unit-testable.
    /// </summary>
    public sealed class OwnedTrack
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OwnedTrack"/> class.
        /// </summary>
        /// <param name="id">The Jellyfin item id.</param>
        /// <param name="key">The normalized (artist, title) identity.</param>
        /// <param name="genres">The track's genre/tag set (may be empty).</param>
        /// <param name="playCount">The user's play count for the track.</param>
        /// <param name="isFavorite">Whether the user favorited the track.</param>
        public OwnedTrack(Guid id, TrackKey key, IReadOnlyCollection<string> genres, int playCount, bool isFavorite)
        {
            Id = id;
            Key = key;
            Genres = genres;
            PlayCount = playCount;
            IsFavorite = isFavorite;
        }

        /// <summary>
        /// Gets the Jellyfin item id.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the normalized identity of the track.
        /// </summary>
        public TrackKey Key { get; }

        /// <summary>
        /// Gets the track's genre/tag set.
        /// </summary>
        public IReadOnlyCollection<string> Genres { get; }

        /// <summary>
        /// Gets the user's play count for the track.
        /// </summary>
        public int PlayCount { get; }

        /// <summary>
        /// Gets a value indicating whether the user favorited the track.
        /// </summary>
        public bool IsFavorite { get; }
    }
}
