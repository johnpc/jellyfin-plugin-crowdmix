using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Jellyfin.Plugin.CrowdMix.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class DailyMixServiceTests
    {
        private readonly IUserManager _userManager = Substitute.For<IUserManager>();
        private readonly ILibraryManager _libraryManager = Substitute.For<ILibraryManager>();
        private readonly IPlaylistManager _playlistManager = Substitute.For<IPlaylistManager>();
        private readonly ILibraryTrackReader _trackReader = Substitute.For<ILibraryTrackReader>();
        private readonly IMixEngine _mixEngine = Substitute.For<IMixEngine>();

        [Fact]
        public async Task RunAsync_CreatesPlaylistFromMostPlayedSeeds()
        {
            var user = NewUser();
            _userManager.GetUsers().Returns(new[] { user });

            var seed = Owned("radiohead", "karma police", playCount: 12);
            var match = Owned("pixies", "where is my mind", playCount: 0);
            _trackReader.BuildIndex(user).Returns(new Dictionary<TrackKey, OwnedTrack>
            {
                [seed.Key] = seed,
                [match.Key] = match,
            });
            _mixEngine.BuildMixAsync(seed, Arg.Any<IReadOnlyDictionary<TrackKey, OwnedTrack>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<Guid> { match.Id });
            _playlistManager.GetPlaylists(user.Id).Returns(Array.Empty<Playlist>());
            _playlistManager.CreatePlaylist(Arg.Any<PlaylistCreationRequest>())
                .Returns(Task.FromResult(new PlaylistCreationResult(Guid.NewGuid().ToString())));

            await Service().RunAsync(CancellationToken.None);

            await _playlistManager.Received(1).CreatePlaylist(
                Arg.Is<PlaylistCreationRequest>(r => r.Name == "CrowdMix Daily" && r.ItemIdList.Count == 1));
        }

        [Fact]
        public async Task RunAsync_SkipsUserWithNoPlayHistory()
        {
            var user = NewUser();
            _userManager.GetUsers().Returns(new[] { user });
            _trackReader.BuildIndex(user).Returns(new Dictionary<TrackKey, OwnedTrack>
            {
                [new TrackKey("a", "b")] = Owned("a", "b", playCount: 0),
            });

            await Service().RunAsync(CancellationToken.None);

            await _playlistManager.DidNotReceive().CreatePlaylist(Arg.Any<PlaylistCreationRequest>());
        }

        private static OwnedTrack Owned(string artist, string title, int playCount)
        {
            return new OwnedTrack(Guid.NewGuid(), new TrackKey(artist, title), Array.Empty<string>(), playCount, false);
        }

        private static User NewUser() => new User("tester", "Default", "Default") { Id = Guid.NewGuid() };

        private DailyMixService Service()
        {
            return new DailyMixService(
                _userManager, _libraryManager, _playlistManager, _trackReader, _mixEngine,
                NullLogger<DailyMixService>.Instance);
        }
    }
}
