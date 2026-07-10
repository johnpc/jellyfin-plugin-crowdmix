using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.Configuration;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void Defaults_AreSensible()
        {
            var config = new PluginConfiguration();

            config.Enabled.Should().BeTrue();
            config.FallbackToNative.Should().BeTrue();
            config.MixSize.Should().Be(50);
            config.MaxTracksPerArtist.Should().Be(2);
            config.LastFmApiKey.Should().BeEmpty();
        }

        [Fact]
        public void ToWeights_MapsEveryField()
        {
            var config = new PluginConfiguration
            {
                CrowdSimilarityWeight = 1.5,
                TagOverlapWeight = 0.6,
                PlayAffinityWeight = 0.2,
                FavoriteBonusWeight = 0.1,
                MaxTracksPerArtist = 4,
            };

            var weights = config.ToWeights();

            weights.CrowdSimilarity.Should().Be(1.5);
            weights.TagOverlap.Should().Be(0.6);
            weights.PlayAffinity.Should().Be(0.2);
            weights.FavoriteBonus.Should().Be(0.1);
            weights.MaxTracksPerArtist.Should().Be(4);
        }

        [Fact]
        public void MixWeightsDefault_MatchesParameterlessConstructor()
        {
            var defaults = Jellyfin.Plugin.CrowdMix.Services.MixWeights.Default();
            defaults.CrowdSimilarity.Should().Be(1.0);
            defaults.MaxTracksPerArtist.Should().Be(2);
        }
    }
}
