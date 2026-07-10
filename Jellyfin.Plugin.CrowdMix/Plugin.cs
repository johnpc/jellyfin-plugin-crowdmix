using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.CrowdMix.Configuration;
using Jellyfin.Plugin.CrowdMix.Filters;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CrowdMix
{
    /// <summary>
    /// The main plugin class for CrowdMix.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly ILogger<Plugin> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="serviceProvider">The application service provider.</param>
        /// <param name="actionProvider">The action descriptor collection provider.</param>
        /// <param name="lifetime">The host application lifetime.</param>
        /// <param name="logger">The logger.</param>
        public Plugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer,
            IServiceProvider serviceProvider,
            IActionDescriptorCollectionProvider actionProvider,
            IHostApplicationLifetime lifetime,
            ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _logger = logger;

            // Actions are registered before plugins finish loading, so hook once the app has started.
            lifetime.ApplicationStarted.Register(() => InjectFilters(actionProvider, serviceProvider));
        }

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "CrowdMix";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("fd98574b-53f4-473d-81e1-ca86cce168bd");

        /// <inheritdoc />
        public override string Description =>
            "A better Instant Mix powered by real listening-crowd similarity (Last.fm), not just audio.";

        /// <summary>
        /// Gets the current plugin configuration, or defaults if the plugin is not loaded.
        /// </summary>
        /// <returns>The active configuration.</returns>
        public static PluginConfiguration GetConfiguration()
        {
            return Instance?.Configuration ?? new PluginConfiguration();
        }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
                },
            };
        }

        private void InjectFilters(IActionDescriptorCollectionProvider provider, IServiceProvider serviceProvider)
        {
            var count = provider.AddDynamicFilter<InstantMixFilter>(serviceProvider, action =>
                action.ControllerTypeInfo.FullName == "Jellyfin.Api.Controllers.InstantMixController"
                && (action.MethodInfo.Name == "GetInstantMixFromItem"
                    || action.MethodInfo.Name == "GetInstantMixFromAlbum"
                    || action.MethodInfo.Name == "GetInstantMixFromSong"));

            _logger.LogInformation("CrowdMix: attached to {Count} Instant Mix action(s).", count);
        }
    }
}
