using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Jellyfin.Plugin.CrowdMix.LastFm;
using Jellyfin.Plugin.CrowdMix.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CrowdMix.Registration
{
    /// <summary>
    /// Registers CrowdMix services into Jellyfin's DI container at startup.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class CrowdMixRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddHttpClient("CrowdMix", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin-CrowdMix/1.0 (+https://github.com/johnpc/jellyfin-plugin-crowdmix)");
            });

            serviceCollection.AddSingleton<ILastFmClient>(sp => new LastFmClient(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<LastFmClient>>(),
                () => Plugin.GetConfiguration().LastFmApiKey));

            serviceCollection.AddSingleton<IMixEngine>(sp => new MixEngine(
                sp.GetRequiredService<ILastFmClient>(),
                new MixReranker(Plugin.GetConfiguration().ToWeights())));

            serviceCollection.AddSingleton<ILibraryTrackReader, LibraryTrackReader>();
            serviceCollection.AddSingleton<InstantMixService>();
            serviceCollection.AddSingleton<DailyMixService>();
        }
    }
}
