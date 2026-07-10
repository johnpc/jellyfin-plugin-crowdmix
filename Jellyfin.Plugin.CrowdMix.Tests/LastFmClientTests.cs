using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class LastFmClientTests
    {
        [Fact]
        public async Task GetSimilarTracksAsync_ParsesResponseAndBuildsRequest()
        {
            var handler = new StubHandler(_ => Json(@"{""similartracks"":{""track"":[
                {""name"":""Paranoid Android"",""match"":0.9,""artist"":{""name"":""Radiohead""}}]}}"));
            var client = Client(handler);

            var result = await client.GetSimilarTracksAsync(new TrackKey("radiohead", "karma police"), 20, CancellationToken.None);

            result.Should().ContainSingle().Which.Key.Should().Be(new TrackKey("radiohead", "paranoid android"));
            handler.LastUrl.Should().Contain("method=track.getsimilar")
                .And.Contain("api_key=test-key")
                .And.Contain("artist=radiohead")
                .And.Contain("karma police");
        }

        [Fact]
        public async Task GetSimilarTracksAsync_ReturnsEmptyForInvalidSeed()
        {
            var client = Client(new StubHandler(_ => Json("{}")));
            (await client.GetSimilarTracksAsync(new TrackKey(string.Empty, string.Empty), 10, CancellationToken.None))
                .Should().BeEmpty();
        }

        [Fact]
        public async Task GetSimilarTracksAsync_ReturnsEmptyOnHttpError()
        {
            var client = Client(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));
            (await client.GetSimilarTracksAsync(new TrackKey("a", "b"), 10, CancellationToken.None)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetSimilarTracksAsync_ReturnsEmptyOnNetworkException()
        {
            var client = Client(new StubHandler(_ => throw new HttpRequestException("boom")));
            (await client.GetSimilarTracksAsync(new TrackKey("a", "b"), 10, CancellationToken.None)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetSimilarArtistTracksAsync_FollowsSimilarArtistsThenTopTracks()
        {
            var handler = new StubHandler(request =>
            {
                var url = request.RequestUri!.ToString();
                if (url.Contains("artist.getsimilar", StringComparison.Ordinal))
                {
                    return Json(@"{""similarartists"":{""artist"":[{""name"":""Pixies""}]}}");
                }

                return Json(@"{""toptracks"":{""track"":[{""name"":""Where Is My Mind?""}]}}");
            });
            var client = Client(handler);

            var result = await client.GetSimilarArtistTracksAsync("radiohead", 20, CancellationToken.None);

            result.Should().ContainSingle().Which.Key.Should().Be(new TrackKey("pixies", "where is my mind"));
        }

        [Fact]
        public async Task GetSimilarArtistTracksAsync_ReturnsEmptyForBlankArtist()
        {
            var client = Client(new StubHandler(_ => Json("{}")));
            (await client.GetSimilarArtistTracksAsync("   ", 10, CancellationToken.None)).Should().BeEmpty();
        }

        private static HttpResponseMessage Json(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }

        private static LastFmClient Client(StubHandler handler)
        {
            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));
            return new LastFmClient(factory, NullLogger<LastFmClient>.Instance, () => "test-key");
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            public string? LastUrl { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastUrl = request.RequestUri!.ToString();
                return Task.FromResult(_responder(request));
            }
        }
    }
}
