using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Jellyfin.Plugin.CrowdMix.Services;
using Reqnroll;

namespace Jellyfin.Plugin.CrowdMix.AcceptanceTests
{
    [Binding]
    public sealed class InstantMixSteps
    {
        private readonly Dictionary<TrackKey, OwnedTrack> _library = new();
        private readonly List<SimilarTrack> _crowd = new();
        private OwnedTrack? _seed;
        private int _maxPerArtist;
        private IReadOnlyList<Guid> _mix = Array.Empty<Guid>();

        [Given("a library containing \"(.*)\" and \"(.*)\"")]
        public void GivenALibraryContaining(string seed, string other)
        {
            _seed = Add(seed);
            Add(other);
        }

        [Given("the crowd considers \"(.*)\" similar to the seed")]
        public void GivenTheCrowdConsidersSimilar(string track)
        {
            _crowd.Add(new SimilarTrack(KeyOf(track), 0.5));
        }

        [Given("a library with three tracks by \"(.*)\" all similar to the seed")]
        public void GivenThreeTracksBySameArtist(string artist)
        {
            _seed = Add($"{artist} - Seed Song");
            for (var i = 1; i <= 3; i++)
            {
                var track = Add($"{artist} - Track {i}");
                _crowd.Add(new SimilarTrack(track.Key, 0.9 - (i * 0.01)));
            }
        }

        [Given("a max of (.*) tracks per artist")]
        public void GivenAMaxTracksPerArtist(int max)
        {
            _maxPerArtist = max;
        }

        [When("I request an instant mix seeded by \"(.*)\"")]
        public void WhenIRequestAnInstantMix(string seed)
        {
            _seed ??= Add(seed);
            var reranker = new MixReranker(new MixWeights { MaxTracksPerArtist = _maxPerArtist });
            var engine = new MixEngine(new StubClient(_crowd), reranker);
            _mix = engine.BuildMixAsync(_seed, _library, 20, CancellationToken.None).GetAwaiter().GetResult();
        }

        [Then("the mix contains \"(.*)\"")]
        public void ThenTheMixContains(string track)
        {
            _mix.Should().Contain(_library[KeyOf(track)].Id);
        }

        [Then("the mix excludes the seed track")]
        public void ThenTheMixExcludesTheSeed()
        {
            _mix.Should().NotContain(_seed!.Id);
        }

        [Then("no more than (.*) tracks by \"(.*)\" appear in the mix")]
        public void ThenNoMoreThanTracksByArtist(int max, string artist)
        {
            var normalizedArtist = TrackNormalizer.NormalizeArtist(artist);
            var count = _mix
                .Select(id => _library.Values.First(t => t.Id == id))
                .Count(t => t.Key.Artist == normalizedArtist);
            count.Should().BeLessThanOrEqualTo(max);
        }

        private OwnedTrack Add(string track)
        {
            var key = KeyOf(track);
            var owned = new OwnedTrack(Guid.NewGuid(), key, Array.Empty<string>(), 0, false);
            _library[key] = owned;
            return owned;
        }

        private static TrackKey KeyOf(string track)
        {
            var parts = track.Split(" - ", 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2
                ? TrackNormalizer.ToKey(parts[0], parts[1])
                : TrackNormalizer.ToKey(track, track);
        }

        private sealed class StubClient : ILastFmClient
        {
            private readonly IReadOnlyList<SimilarTrack> _similar;

            public StubClient(IReadOnlyList<SimilarTrack> similar)
            {
                _similar = similar;
            }

            public System.Threading.Tasks.Task<IReadOnlyList<SimilarTrack>> GetSimilarTracksAsync(TrackKey seed, int limit, CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult(_similar);
            }

            public System.Threading.Tasks.Task<IReadOnlyList<SimilarTrack>> GetSimilarArtistTracksAsync(string artist, int limit, CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult<IReadOnlyList<SimilarTrack>>(Array.Empty<SimilarTrack>());
            }
        }
    }
}
