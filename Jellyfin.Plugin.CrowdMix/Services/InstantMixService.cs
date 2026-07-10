using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.CrowdMix.LastFm;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Orchestrates a CrowdMix instant mix for the injected Instant Mix endpoint: resolves the seed,
    /// runs the crowd-signal engine against the owned library, and builds the DTO result — falling
    /// back to the native mix when CrowdMix yields nothing.
    /// </summary>
    public sealed class InstantMixService
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IMusicManager _musicManager;
        private readonly IDtoService _dtoService;
        private readonly ILibraryTrackReader _trackReader;
        private readonly IMixEngine _mixEngine;
        private readonly ILogger<InstantMixService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantMixService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="musicManager">The music manager (native fallback).</param>
        /// <param name="dtoService">The DTO service.</param>
        /// <param name="trackReader">The owned-library reader.</param>
        /// <param name="mixEngine">The crowd-signal mix engine.</param>
        /// <param name="logger">The logger.</param>
        public InstantMixService(
            IUserManager userManager,
            ILibraryManager libraryManager,
            IMusicManager musicManager,
            IDtoService dtoService,
            ILibraryTrackReader trackReader,
            IMixEngine mixEngine,
            ILogger<InstantMixService> logger)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _musicManager = musicManager;
            _dtoService = dtoService;
            _trackReader = trackReader;
            _mixEngine = mixEngine;
            _logger = logger;
        }

        /// <summary>
        /// Generates an instant mix, matching the native endpoint's result shape.
        /// </summary>
        /// <param name="itemId">The seed item id.</param>
        /// <param name="userId">The requesting user id.</param>
        /// <param name="limit">The requested mix size.</param>
        /// <param name="fields">The requested DTO fields.</param>
        /// <param name="enableImages">Whether images are enabled.</param>
        /// <param name="enableUserData">Whether user data is enabled.</param>
        /// <param name="imageTypeLimit">The image type limit.</param>
        /// <param name="enableImageTypes">The enabled image types.</param>
        /// <param name="currentUser">The current claims principal.</param>
        /// <returns>The mix result.</returns>
        public QueryResult<BaseItemDto> GenerateMix(
            Guid itemId,
            Guid? userId,
            int? limit,
            ItemFields[] fields,
            bool? enableImages,
            bool? enableUserData,
            int? imageTypeLimit,
            ImageType[] enableImageTypes,
            ClaimsPrincipal currentUser)
        {
            var user = userId.HasValue && userId.Value != Guid.Empty ? _userManager.GetUserById(userId.Value) : null;
            var item = _libraryManager.GetItemById<BaseItem>(itemId);
            if (item == null)
            {
                return new QueryResult<BaseItemDto>(0, 0, Array.Empty<BaseItemDto>());
            }

            var config = Plugin.GetConfiguration();
            var size = limit ?? config.MixSize;
            var dtoOptions = new DtoOptions { Fields = fields }
                .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

            var ordered = ResolveOwnedMix(item, user, size);
            if (ordered.Count == 0)
            {
                return NativeFallback(item, user, size, dtoOptions, config.FallbackToNative);
            }

            var items = ordered
                .Select(id => _libraryManager.GetItemById<BaseItem>(id))
                .Where(track => track != null)
                .Select(track => track!)
                .ToList();

            return new QueryResult<BaseItemDto>(0, items.Count, _dtoService.GetBaseItemDtos(items, dtoOptions, user));
        }

        private static IEnumerable<Audio> SeedTracks(BaseItem item)
        {
            if (item is Audio audio)
            {
                yield return audio;
            }
            else if (item is MusicAlbum album)
            {
                foreach (var child in album.Children.OfType<Audio>())
                {
                    yield return child;
                }
            }
        }

        private IReadOnlyList<Guid> ResolveOwnedMix(BaseItem item, User? user, int size)
        {
            var seeds = SeedTracks(item).ToList();
            if (seeds.Count == 0)
            {
                return Array.Empty<Guid>();
            }

            var index = _trackReader.BuildIndex(user);
            var ordered = new List<Guid>();
            var seen = new HashSet<Guid>();
            foreach (var seed in seeds)
            {
                var owned = _trackReader.ToOwnedTrack(seed, user);
                var mix = _mixEngine.BuildMixAsync(owned, index, size, CancellationToken.None)
                    .GetAwaiter().GetResult();
                foreach (var id in mix)
                {
                    if (seen.Add(id))
                    {
                        ordered.Add(id);
                    }
                }
            }

            return ordered.Count > size ? ordered.Take(size).ToList() : ordered;
        }

        private QueryResult<BaseItemDto> NativeFallback(
            BaseItem item, User? user, int size, DtoOptions dtoOptions, bool fallbackEnabled)
        {
            if (!fallbackEnabled)
            {
                return new QueryResult<BaseItemDto>(0, 0, Array.Empty<BaseItemDto>());
            }

            _logger.LogInformation("CrowdMix: no owned crowd matches, falling back to native Instant Mix.");
            var native = _musicManager.GetInstantMixFromItem(item, user, dtoOptions).ToList();
            if (native.Count > size)
            {
                native = native.Take(size).ToList();
            }

            return new QueryResult<BaseItemDto>(0, native.Count, _dtoService.GetBaseItemDtos(native, dtoOptions, user));
        }
    }
}
