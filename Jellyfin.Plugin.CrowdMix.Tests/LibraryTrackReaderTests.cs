using System;
using System.Collections.Generic;
using FluentAssertions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Jellyfin.Plugin.CrowdMix.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class LibraryTrackReaderTests
    {
        private readonly ILibraryManager _libraryManager = Substitute.For<ILibraryManager>();
        private readonly IUserDataManager _userDataManager = Substitute.For<IUserDataManager>();

        [Fact]
        public void ToOwnedTrack_ProjectsArtistTitleGenresAndUserData()
        {
            var user = NewUser();
            var audio = new Audio
            {
                Id = Guid.NewGuid(),
                Name = "Karma Police",
                Artists = new[] { "Radiohead" },
                Genres = new[] { "Alternative" },
            };
            _userDataManager.GetUserData(user, audio).Returns(new UserItemData
            {
                Key = "k",
                PlayCount = 7,
                IsFavorite = true,
            });

            var owned = Reader().ToOwnedTrack(audio, user);

            owned.Key.Should().Be(new TrackKey("radiohead", "karma police"));
            owned.Genres.Should().Contain("Alternative");
            owned.PlayCount.Should().Be(7);
            owned.IsFavorite.Should().BeTrue();
        }

        [Fact]
        public void ToOwnedTrack_FallsBackToAlbumArtistAndNoUser()
        {
            var audio = new Audio
            {
                Id = Guid.NewGuid(),
                Name = "So What",
                AlbumArtists = new[] { "Miles Davis" },
            };

            var owned = Reader().ToOwnedTrack(audio, null);

            owned.Key.Should().Be(new TrackKey("miles davis", "so what"));
            owned.PlayCount.Should().Be(0);
            owned.IsFavorite.Should().BeFalse();
        }

        [Fact]
        public void BuildIndex_KeepsMorePlayedOnKeyCollisionAndSkipsInvalid()
        {
            var user = NewUser();
            var quiet = new Audio { Id = Guid.NewGuid(), Name = "Creep", Artists = new[] { "Radiohead" } };
            var loud = new Audio { Id = Guid.NewGuid(), Name = "Creep", Artists = new[] { "Radiohead" } };
            var untitled = new Audio { Id = Guid.NewGuid(), Name = string.Empty, Artists = new[] { "Ghost" } };

            _userDataManager.GetUserData(user, quiet).Returns(Data(1));
            _userDataManager.GetUserData(user, loud).Returns(Data(9));
            _userDataManager.GetUserData(user, untitled).Returns(Data(0));
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { quiet, loud, untitled });

            var index = Reader().BuildIndex(user);

            index.Should().HaveCount(1);
            index[new TrackKey("radiohead", "creep")].Id.Should().Be(loud.Id, "the more-played instance wins");
        }

        private static UserItemData Data(int playCount) => new UserItemData { Key = Guid.NewGuid().ToString(), PlayCount = playCount };

        private static User NewUser() => new User("tester", "Default", "Default");

        private LibraryTrackReader Reader() => new LibraryTrackReader(_libraryManager, _userDataManager);
    }
}
