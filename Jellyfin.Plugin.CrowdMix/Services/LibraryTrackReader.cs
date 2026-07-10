using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.CrowdMix.LastFm;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Default <see cref="ILibraryTrackReader"/> backed by Jellyfin's library and user-data managers.
    /// </summary>
    public sealed class LibraryTrackReader : ILibraryTrackReader
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryTrackReader"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataManager">The user data manager.</param>
        public LibraryTrackReader(ILibraryManager libraryManager, IUserDataManager userDataManager)
        {
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<TrackKey, OwnedTrack> BuildIndex(User? user)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Audio },
                MediaTypes = new[] { MediaType.Audio },
                Recursive = true,
                EnableTotalRecordCount = false,
            };

            var index = new Dictionary<TrackKey, OwnedTrack>();
            foreach (var item in _libraryManager.GetItemList(query))
            {
                if (item is not Audio audio)
                {
                    continue;
                }

                var owned = ToOwnedTrack(audio, user);
                if (!owned.Key.IsValid)
                {
                    continue;
                }

                // On collision, keep the more-played instance so re-ranking uses the best signal.
                if (!index.TryGetValue(owned.Key, out var existing) || owned.PlayCount > existing.PlayCount)
                {
                    index[owned.Key] = owned;
                }
            }

            return index;
        }

        /// <inheritdoc />
        public OwnedTrack ToOwnedTrack(Audio audio, User? user)
        {
            var artist = PrimaryArtist(audio);
            var key = TrackNormalizer.ToKey(artist, audio.Name);

            var playCount = 0;
            var isFavorite = false;
            if (user != null)
            {
                var data = _userDataManager.GetUserData(user, audio);
                if (data != null)
                {
                    playCount = data.PlayCount;
                    isFavorite = data.IsFavorite;
                }
            }

            var genres = audio.Genres ?? Array.Empty<string>();
            return new OwnedTrack(audio.Id, key, genres, playCount, isFavorite);
        }

        private static string? PrimaryArtist(Audio audio)
        {
            if (audio.Artists.Count > 0)
            {
                return audio.Artists[0];
            }

            return audio.AlbumArtists.Count > 0 ? audio.AlbumArtists[0] : null;
        }
    }
}
