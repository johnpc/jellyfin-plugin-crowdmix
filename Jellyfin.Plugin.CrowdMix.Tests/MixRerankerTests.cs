using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Jellyfin.Plugin.CrowdMix.Services;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class MixRerankerTests
    {
        private static readonly Guid SeedId = Guid.NewGuid();

        [Fact]
        public void Rank_OrdersByCrowdScoreThenPersonalization()
        {
            var low = Owned("artist a", "low", playCount: 0);
            var high = Owned("artist b", "high", playCount: 0);
            var owned = Index(low, high);

            var candidates = new[]
            {
                new SimilarTrack(low.Key, 0.2),
                new SimilarTrack(high.Key, 0.9),
            };

            var ranked = new MixReranker(Weights()).Rank(candidates, owned, Array.Empty<string>(), SeedId, 10);

            ranked.Should().Equal(high.Id, low.Id);
        }

        [Fact]
        public void Rank_PlayAffinityBreaksCrowdTies()
        {
            var played = Owned("artist a", "played", playCount: 10);
            var unplayed = Owned("artist b", "unplayed", playCount: 0);
            var owned = Index(played, unplayed);

            var candidates = new[]
            {
                new SimilarTrack(played.Key, 0.5),
                new SimilarTrack(unplayed.Key, 0.5),
            };

            var ranked = new MixReranker(Weights()).Rank(candidates, owned, Array.Empty<string>(), SeedId, 10);

            ranked.First().Should().Be(played.Id);
        }

        [Fact]
        public void Rank_ExcludesSeedAndUnownedCandidates()
        {
            var seed = new OwnedTrack(SeedId, new TrackKey("seed", "seed"), Array.Empty<string>(), 0, false);
            var owned = Index(seed);
            var candidates = new[]
            {
                new SimilarTrack(seed.Key, 1.0),
                new SimilarTrack(new TrackKey("unowned", "ghost"), 0.9),
            };

            new MixReranker(Weights()).Rank(candidates, owned, Array.Empty<string>(), SeedId, 10).Should().BeEmpty();
        }

        [Fact]
        public void Rank_EnforcesMaxTracksPerArtist()
        {
            var a1 = Owned("popular", "one", 0);
            var a2 = Owned("popular", "two", 0);
            var a3 = Owned("popular", "three", 0);
            var other = Owned("other", "song", 0);
            var owned = Index(a1, a2, a3, other);

            var candidates = new[]
            {
                new SimilarTrack(a1.Key, 0.99),
                new SimilarTrack(a2.Key, 0.98),
                new SimilarTrack(a3.Key, 0.97),
                new SimilarTrack(other.Key, 0.5),
            };

            var weights = Weights();
            weights.MaxTracksPerArtist = 2;

            var ranked = new MixReranker(weights).Rank(candidates, owned, Array.Empty<string>(), SeedId, 10);

            ranked.Should().HaveCount(3);
            ranked.Count(id => id == a3.Id).Should().Be(0, "the third 'popular' track is capped out");
            ranked.Should().Contain(other.Id);
        }

        [Fact]
        public void Rank_ZeroCapDisablesArtistLimit()
        {
            var a1 = Owned("popular", "one", 0);
            var a2 = Owned("popular", "two", 0);
            var a3 = Owned("popular", "three", 0);
            var owned = Index(a1, a2, a3);

            var candidates = owned.Values.Select(t => new SimilarTrack(t.Key, 0.5)).ToArray();
            var weights = Weights();
            weights.MaxTracksPerArtist = 0;

            new MixReranker(weights).Rank(candidates, owned, Array.Empty<string>(), SeedId, 10)
                .Should().HaveCount(3);
        }

        [Fact]
        public void Rank_TagOverlapAndFavoriteRaiseScore()
        {
            var favWithTags = new OwnedTrack(Guid.NewGuid(), new TrackKey("a", "fav"), new[] { "jazz" }, 0, true);
            var plain = Owned("b", "plain", 0);
            var owned = Index(favWithTags, plain);

            var candidates = new[]
            {
                new SimilarTrack(favWithTags.Key, 0.3),
                new SimilarTrack(plain.Key, 0.3),
            };

            var ranked = new MixReranker(Weights()).Rank(candidates, owned, new[] { "jazz" }, SeedId, 10);
            ranked.First().Should().Be(favWithTags.Id);
        }

        [Fact]
        public void Rank_NegativeLimitYieldsEmpty()
        {
            var track = Owned("a", "x", 0);
            var owned = Index(track);
            var candidates = new[] { new SimilarTrack(track.Key, 0.5) };

            new MixReranker(Weights()).Rank(candidates, owned, Array.Empty<string>(), SeedId, -1).Should().BeEmpty();
        }

        private static MixWeights Weights() => new MixWeights { MaxTracksPerArtist = 2 };

        private static OwnedTrack Owned(string artist, string title, int playCount)
        {
            return new OwnedTrack(Guid.NewGuid(), new TrackKey(artist, title), Array.Empty<string>(), playCount, false);
        }

        private static IReadOnlyDictionary<TrackKey, OwnedTrack> Index(params OwnedTrack[] tracks)
        {
            return tracks.ToDictionary(t => t.Key);
        }
    }
}
