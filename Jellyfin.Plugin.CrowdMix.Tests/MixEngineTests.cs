using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Jellyfin.Plugin.CrowdMix.Services;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class MixEngineTests
    {
        [Fact]
        public async Task BuildMixAsync_ReturnsOwnedMatchesFromTrackSimilarity()
        {
            var seed = Owned("radiohead", "karma police");
            var match = Owned("pixies", "where is my mind");
            var index = Index(seed, match);

            var client = new FakeClient
            {
                SimilarTracks = new[] { new SimilarTrack(match.Key, 0.4) },
            };
            var engine = new MixEngine(client, new MixReranker(new MixWeights { MaxTracksPerArtist = 0 }));

            var mix = await engine.BuildMixAsync(seed, index, 10, CancellationToken.None);

            mix.Should().ContainSingle().Which.Should().Be(match.Id);
            client.SimilarCalls.Should().Be(1);
        }

        [Fact]
        public async Task BuildMixAsync_WidensToSimilarArtistsWhenTrackMatchesSparse()
        {
            var seed = Owned("obscure band", "deep cut");
            var artistMatch = Owned("adjacent band", "hit");
            var index = Index(seed, artistMatch);

            var client = new FakeClient
            {
                SimilarTracks = Array.Empty<SimilarTrack>(),
                ArtistTracks = new[] { new SimilarTrack(artistMatch.Key, 0.3) },
            };
            var engine = new MixEngine(client, new MixReranker(new MixWeights { MaxTracksPerArtist = 0 }));

            var mix = await engine.BuildMixAsync(seed, index, 10, CancellationToken.None);

            mix.Should().Contain(artistMatch.Id);
            client.ArtistCalls.Should().Be(1, "sparse track results trigger the artist-widen fallback");
        }

        [Fact]
        public async Task BuildMixAsync_SkipsWidenWhenEnoughTrackMatches()
        {
            var seed = Owned("big artist", "big song");
            var matches = Enumerable.Range(0, 9).Select(i => Owned("artist" + i, "song" + i)).ToArray();
            var index = Index(new[] { seed }.Concat(matches).ToArray());

            var client = new FakeClient
            {
                SimilarTracks = matches.Select(m => new SimilarTrack(m.Key, 0.5)).ToArray(),
            };
            var engine = new MixEngine(client, new MixReranker(new MixWeights { MaxTracksPerArtist = 0 }));

            await engine.BuildMixAsync(seed, index, 20, CancellationToken.None);

            client.ArtistCalls.Should().Be(0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task BuildMixAsync_ReturnsEmptyForNonPositiveLimit(int limit)
        {
            var seed = Owned("a", "b");
            var engine = new MixEngine(new FakeClient(), new MixReranker(new MixWeights()));

            (await engine.BuildMixAsync(seed, Index(seed), limit, CancellationToken.None)).Should().BeEmpty();
        }

        [Fact]
        public async Task BuildMixAsync_ReturnsEmptyForInvalidSeed()
        {
            var invalid = new OwnedTrack(Guid.NewGuid(), new TrackKey(string.Empty, string.Empty), Array.Empty<string>(), 0, false);
            var engine = new MixEngine(new FakeClient(), new MixReranker(new MixWeights()));

            (await engine.BuildMixAsync(invalid, Index(invalid), 10, CancellationToken.None)).Should().BeEmpty();
        }

        private static OwnedTrack Owned(string artist, string title)
        {
            return new OwnedTrack(Guid.NewGuid(), new TrackKey(artist, title), Array.Empty<string>(), 0, false);
        }

        private static IReadOnlyDictionary<TrackKey, OwnedTrack> Index(params OwnedTrack[] tracks)
        {
            return tracks.ToDictionary(t => t.Key);
        }

        private sealed class FakeClient : ILastFmClient
        {
            public IReadOnlyList<SimilarTrack> SimilarTracks { get; set; } = Array.Empty<SimilarTrack>();

            public IReadOnlyList<SimilarTrack> ArtistTracks { get; set; } = Array.Empty<SimilarTrack>();

            public int SimilarCalls { get; private set; }

            public int ArtistCalls { get; private set; }

            public Task<IReadOnlyList<SimilarTrack>> GetSimilarTracksAsync(TrackKey seed, int limit, CancellationToken cancellationToken)
            {
                SimilarCalls++;
                return Task.FromResult(SimilarTracks);
            }

            public Task<IReadOnlyList<SimilarTrack>> GetSimilarArtistTracksAsync(string artist, int limit, CancellationToken cancellationToken)
            {
                ArtistCalls++;
                return Task.FromResult(ArtistTracks);
            }
        }
    }
}
