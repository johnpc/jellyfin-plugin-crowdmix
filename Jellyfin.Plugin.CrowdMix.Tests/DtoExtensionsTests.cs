using FluentAssertions;
using Jellyfin.Plugin.CrowdMix.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.CrowdMix.Tests
{
    public class DtoExtensionsTests
    {
        [Fact]
        public void AddAdditionalDtoOptions_AppliesAllProvidedValues()
        {
            var options = new DtoOptions()
                .AddAdditionalDtoOptions(
                    enableImages: false,
                    enableUserData: true,
                    imageTypeLimit: 3,
                    enableImageTypes: new[] { ImageType.Primary, ImageType.Backdrop });

            options.EnableImages.Should().BeFalse();
            options.EnableUserData.Should().BeTrue();
            options.ImageTypeLimit.Should().Be(3);
            options.ImageTypes.Should().Contain(ImageType.Primary);
        }

        [Fact]
        public void AddAdditionalDtoOptions_DefaultsImagesOnWhenNull()
        {
            var options = new DtoOptions()
                .AddAdditionalDtoOptions(null, null, null, System.Array.Empty<ImageType>());

            options.EnableImages.Should().BeTrue();
        }
    }
}
