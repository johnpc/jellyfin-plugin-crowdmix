using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.CrowdMix.LastFm;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Reads the user's owned audio library and projects it into the normalized-key index
    /// the mix engine works against.
    /// </summary>
    public interface ILibraryTrackReader
    {
        /// <summary>
        /// Builds an index of all owned audio tracks keyed by normalized (artist, title),
        /// carrying per-user play/favorite data for re-ranking.
        /// </summary>
        /// <param name="user">The user whose play data to load (null uses no personalization).</param>
        /// <returns>The owned-track index. On key collisions the more-played track wins.</returns>
        IReadOnlyDictionary<TrackKey, OwnedTrack> BuildIndex(User? user);

        /// <summary>
        /// Projects a single <see cref="Audio"/> entity into an <see cref="OwnedTrack"/>.
        /// </summary>
        /// <param name="audio">The audio entity.</param>
        /// <param name="user">The user whose play data to load.</param>
        /// <returns>The projected owned track.</returns>
        OwnedTrack ToOwnedTrack(Audio audio, User? user);
    }
}
