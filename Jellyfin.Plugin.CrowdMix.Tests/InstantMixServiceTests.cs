using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Jellyfin.Plugin.CrowdMix.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class InstantMixServiceTests
    {
        private readonly IUserManager _userManager = Substitute.For<IUserManager>();
        private readonly ILibraryManager _libraryManager = Substitute.For<ILibraryManager>();
        private readonly IMusicManager _musicManager = Substitute.For<IMusicManager>();
        private readonly IDtoService _dtoService = Substitute.For<IDtoService>();
        private readonly ILibraryTrackReader _trackReader = Substitute.For<ILibraryTrackReader>();
        private readonly IMixEngine _mixEngine = Substitute.For<IMixEngine>();

        [Fact]
        public void GenerateMix_ReturnsEmptyWhenSeedItemMissing()
        {
            var result = Service().GenerateMix(
                Guid.NewGuid(), null, null, Array.Empty<ItemFields>(), null, null, null, Array.Empty<ImageType>(), Anonymous());

            result.TotalRecordCount.Should().Be(0);
        }

        [Fact]
        public void GenerateMix_MapsCrowdMixToDtos()
        {
            var seed = new Audio { Id = Guid.NewGuid(), Name = "Karma Police", Artists = new[] { "Radiohead" } };
            var match = new Audio { Id = Guid.NewGuid(), Name = "Where Is My Mind", Artists = new[] { "Pixies" } };

            _libraryManager.GetItemById<BaseItem>(seed.Id).Returns(seed);
            _libraryManager.GetItemById<BaseItem>(match.Id).Returns(match);
            _trackReader.ToOwnedTrack(seed, Arg.Any<Jellyfin.Database.Implementations.Entities.User?>())
                .Returns(Owned(seed.Id, "radiohead", "karma police"));
            _trackReader.BuildIndex(Arg.Any<Jellyfin.Database.Implementations.Entities.User?>())
                .Returns(new Dictionary<TrackKey, OwnedTrack>());
            _mixEngine.BuildMixAsync(Arg.Any<OwnedTrack>(), Arg.Any<IReadOnlyDictionary<TrackKey, OwnedTrack>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<Guid> { match.Id });
            _dtoService.GetBaseItemDtos(Arg.Any<IReadOnlyList<BaseItem>>(), Arg.Any<DtoOptions>(), Arg.Any<Jellyfin.Database.Implementations.Entities.User?>())
                .Returns(new List<BaseItemDto> { new BaseItemDto { Id = match.Id } });

            var result = Service().GenerateMix(
                seed.Id, null, 10, Array.Empty<ItemFields>(), null, null, null, Array.Empty<ImageType>(), Anonymous());

            result.Items.Should().ContainSingle().Which.Id.Should().Be(match.Id);
        }

        [Fact]
        public void GenerateMix_FallsBackToNativeWhenNoCrowdMatches()
        {
            var seed = new Audio { Id = Guid.NewGuid(), Name = "Obscure", Artists = new[] { "Nobody" } };
            var nativePick = new Audio { Id = Guid.NewGuid(), Name = "Native", Artists = new[] { "Local" } };

            _libraryManager.GetItemById<BaseItem>(seed.Id).Returns(seed);
            _trackReader.ToOwnedTrack(seed, Arg.Any<Jellyfin.Database.Implementations.Entities.User?>())
                .Returns(Owned(seed.Id, "nobody", "obscure"));
            _trackReader.BuildIndex(Arg.Any<Jellyfin.Database.Implementations.Entities.User?>())
                .Returns(new Dictionary<TrackKey, OwnedTrack>());
            _mixEngine.BuildMixAsync(Arg.Any<OwnedTrack>(), Arg.Any<IReadOnlyDictionary<TrackKey, OwnedTrack>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<Guid>());
            _musicManager.GetInstantMixFromItem(seed, Arg.Any<Jellyfin.Database.Implementations.Entities.User?>(), Arg.Any<DtoOptions>())
                .Returns(new List<BaseItem> { nativePick });
            _dtoService.GetBaseItemDtos(Arg.Any<IReadOnlyList<BaseItem>>(), Arg.Any<DtoOptions>(), Arg.Any<Jellyfin.Database.Implementations.Entities.User?>())
                .Returns(new List<BaseItemDto> { new BaseItemDto { Id = nativePick.Id } });

            var result = Service().GenerateMix(
                seed.Id, null, 10, Array.Empty<ItemFields>(), null, null, null, Array.Empty<ImageType>(), Anonymous());

            result.Items.Should().ContainSingle().Which.Id.Should().Be(nativePick.Id);
            _musicManager.Received(1).GetInstantMixFromItem(seed, Arg.Any<Jellyfin.Database.Implementations.Entities.User?>(), Arg.Any<DtoOptions>());
        }

        private static OwnedTrack Owned(Guid id, string artist, string title)
        {
            return new OwnedTrack(id, new TrackKey(artist, title), Array.Empty<string>(), 0, false);
        }

        private static ClaimsPrincipal Anonymous() => new ClaimsPrincipal(new ClaimsIdentity());

        private InstantMixService Service()
        {
            return new InstantMixService(
                _userManager, _libraryManager, _musicManager, _dtoService, _trackReader, _mixEngine,
                NullLogger<InstantMixService>.Instance);
        }
    }
}
