using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class LastFmResponseParserTests
    {
        private const string SimilarJson = @"{
          ""similartracks"": {
            ""track"": [
              { ""name"": ""Paranoid Android"", ""match"": 1.0, ""artist"": { ""name"": ""Radiohead"" } },
              { ""name"": ""Where Is My Mind?"", ""match"": ""0.327"", ""artist"": { ""name"": ""Pixies"" } },
              { ""name"": ""No Artist"", ""match"": 0.2 }
            ]
          }
        }";

        [Fact]
        public void ParseSimilarTracks_ReadsNameArtistAndNumericMatch()
        {
            var result = LastFmResponseParser.ParseSimilarTracks(SimilarJson);

            result.Should().HaveCount(2, "the entry without an artist is skipped");
            result[0].Key.Should().Be(new TrackKey("radiohead", "paranoid android"));
            result[0].Score.Should().Be(1.0);
        }

        [Fact]
        public void ParseSimilarTracks_ParsesStringMatchValue()
        {
            var result = LastFmResponseParser.ParseSimilarTracks(SimilarJson);
            result[1].Score.Should().BeApproximately(0.327, 1e-6);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{}")]
        [InlineData(@"{""similartracks"":{}}")]
        [InlineData(@"{""similartracks"":{""track"":[]}}")]
        public void ParseSimilarTracks_ReturnsEmptyForMissingShapes(string json)
        {
            LastFmResponseParser.ParseSimilarTracks(json).Should().BeEmpty();
        }

        [Fact]
        public void ParseArtistTopTracks_RankDecaysScoresAndKeysToArtist()
        {
            const string json = @"{
              ""toptracks"": {
                ""track"": [
                  { ""name"": ""Da Funk"" },
                  { ""name"": ""Digital Love"" }
                ]
              }
            }";

            var result = LastFmResponseParser.ParseArtistTopTracks(json, "Daft Punk");

            result.Should().HaveCount(2);
            result[0].Key.Should().Be(new TrackKey("daft punk", "da funk"));
            result[0].Score.Should().BeGreaterThan(result[1].Score, "earlier rank scores higher");
            result[0].Score.Should().BeApproximately(0.5, 1e-6);
        }

        [Theory]
        [InlineData("")]
        [InlineData(@"{""toptracks"":{}}")]
        public void ParseArtistTopTracks_ReturnsEmptyForMissingShapes(string json)
        {
            LastFmResponseParser.ParseArtistTopTracks(json, "Daft Punk").Should().BeEmpty();
        }
    }
}
