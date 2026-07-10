using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class TrackKeyTests
    {
        [Fact]
        public void Equality_MatchesByNormalizedValues()
        {
            var a = new TrackKey("radiohead", "creep");
            var b = new TrackKey("radiohead", "creep");

            (a == b).Should().BeTrue();
            (a != b).Should().BeFalse();
            a.Equals(b).Should().BeTrue();
            a.Equals((object)b).Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Equality_DiffersByTitle()
        {
            var a = new TrackKey("radiohead", "creep");
            var b = new TrackKey("radiohead", "no surprises");

            (a == b).Should().BeFalse();
            (a != b).Should().BeTrue();
            a.Equals((object)"not a key").Should().BeFalse();
        }

        [Fact]
        public void NullValues_AreCoercedToEmpty()
        {
            var key = new TrackKey(null!, null!);
            key.Artist.Should().BeEmpty();
            key.Title.Should().BeEmpty();
            key.IsValid.Should().BeFalse();
        }

        [Fact]
        public void ToString_IsHumanReadable()
        {
            new TrackKey("miles davis", "so what").ToString().Should().Be("miles davis - so what");
        }
    }
}
