using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.CrowdMix.LastFm;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Builds a per-user "daily mix" playlist by seeding the crowd-signal engine from the user's
    /// most-played tracks, then materializing the ranked result into a Jellyfin playlist.
    /// </summary>
    public sealed class DailyMixService
    {
        private const string PlaylistName = "CrowdMix Daily";
        private const int SeedCount = 5;

        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IPlaylistManager _playlistManager;
        private readonly ILibraryTrackReader _trackReader;
        private readonly IMixEngine _mixEngine;
        private readonly ILogger<DailyMixService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DailyMixService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="playlistManager">The playlist manager.</param>
        /// <param name="trackReader">The owned-library reader.</param>
        /// <param name="mixEngine">The crowd-signal mix engine.</param>
        /// <param name="logger">The logger.</param>
        public DailyMixService(
            IUserManager userManager,
            ILibraryManager libraryManager,
            IPlaylistManager playlistManager,
            ILibraryTrackReader trackReader,
            IMixEngine mixEngine,
            ILogger<DailyMixService> logger)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _playlistManager = playlistManager;
            _trackReader = trackReader;
            _mixEngine = mixEngine;
            _logger = logger;
        }

        /// <summary>
        /// Rebuilds the daily mix playlist for every user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var size = Plugin.GetConfiguration().DailyMixSize;
            foreach (var user in _userManager.GetUsers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await BuildForUserAsync(user, size, cancellationToken).ConfigureAwait(false);
            }
        }

        private static List<OwnedTrack> TopSeeds(IReadOnlyDictionary<TrackKey, OwnedTrack> index)
        {
            return index.Values
                .Where(track => track.PlayCount > 0 || track.IsFavorite)
                .OrderByDescending(track => track.IsFavorite)
                .ThenByDescending(track => track.PlayCount)
                .Take(SeedCount)
                .ToList();
        }

        private async Task BuildForUserAsync(User user, int size, CancellationToken cancellationToken)
        {
            var index = _trackReader.BuildIndex(user);
            var seeds = TopSeeds(index);
            if (seeds.Count == 0)
            {
                _logger.LogInformation("CrowdMix: no play history for {User}, skipping daily mix.", user.Username);
                return;
            }

            var ordered = new List<Guid>();
            var seen = new HashSet<Guid>();
            foreach (var seed in seeds)
            {
                var mix = await _mixEngine.BuildMixAsync(seed, index, size, cancellationToken).ConfigureAwait(false);
                foreach (var id in mix.Where(id => seen.Add(id)))
                {
                    ordered.Add(id);
                }
            }

            if (ordered.Count == 0)
            {
                return;
            }

            var final = ordered.Count > size ? ordered.Take(size).ToList() : ordered;
            await ReplacePlaylistAsync(user, final).ConfigureAwait(false);
            _logger.LogInformation("CrowdMix: built {Count}-track daily mix for {User}.", final.Count, user.Username);
        }

        private async Task ReplacePlaylistAsync(User user, IReadOnlyList<Guid> itemIds)
        {
            foreach (var existing in ExistingDailyMixes(user))
            {
                _libraryManager.DeleteItem(existing, new DeleteOptions { DeleteFileLocation = true }, true);
            }

            await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
            {
                Name = PlaylistName,
                ItemIdList = itemIds.ToArray(),
                UserId = user.Id,
                MediaType = MediaType.Audio,
            }).ConfigureAwait(false);
        }

        private List<BaseItem> ExistingDailyMixes(User user)
        {
            return _playlistManager.GetPlaylists(user.Id)
                .Where(playlist => string.Equals(playlist.Name, PlaylistName, StringComparison.Ordinal))
                .Cast<BaseItem>()
                .ToList();
        }
    }
}
