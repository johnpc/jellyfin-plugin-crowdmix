using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class TrackNormalizerTests
    {
        [Theory]
        [InlineData("The Beatles", "the beatles")]
        [InlineData("BEYONCÉ", "beyonce")]
        [InlineData("Björk", "bjork")]
        [InlineData("  Radiohead  ", "radiohead")]
        public void NormalizeArtist_FoldsCaseAccentsAndWhitespace(string input, string expected)
        {
            TrackNormalizer.NormalizeArtist(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("Daft Punk feat. Pharrell", "daft punk")]
        [InlineData("Jay-Z ft. Alicia Keys", "jay z")]
        [InlineData("Calvin Harris featuring Rihanna", "calvin harris")]
        public void NormalizeArtist_DropsFeaturedCredits(string input, string expected)
        {
            TrackNormalizer.NormalizeArtist(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("Karma Police - Remastered 2017", "karma police")]
        [InlineData("Bohemian Rhapsody (Remastered)", "bohemian rhapsody")]
        [InlineData("Creep - Radio Edit", "creep")]
        [InlineData("So What - Live", "so what")]
        public void NormalizeTitle_StripsVersionNoise(string input, string expected)
        {
            TrackNormalizer.NormalizeTitle(input).Should().Be(expected);
        }

        [Fact]
        public void NormalizeTitle_DropsFeaturedCredit()
        {
            TrackNormalizer.NormalizeTitle("Umbrella feat. Jay-Z").Should().Be("umbrella");
        }

        [Fact]
        public void ToKey_ProducesValidKeyForRealTrack()
        {
            var key = TrackNormalizer.ToKey("Miles Davis", "So What");
            key.Artist.Should().Be("miles davis");
            key.Title.Should().Be("so what");
            key.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("Artist", "")]
        public void ToKey_IsInvalidWhenPartMissing(string? artist, string? title)
        {
            TrackNormalizer.ToKey(artist, title).IsValid.Should().BeFalse();
        }
    }
}
