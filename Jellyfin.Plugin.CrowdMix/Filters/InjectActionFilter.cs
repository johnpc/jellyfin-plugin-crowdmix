using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CrowdMix.Filters
{
    /// <summary>
    /// Dynamically attaches an MVC action filter to already-registered controller actions.
    /// This is how CrowdMix intercepts Jellyfin's built-in Instant Mix endpoint without the
    /// server having any awareness of the plugin. (Approach adapted from arnesacnussem's
    /// Meilisearch plugin, via BetterMix.)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class InjectActionFilter
    {
        /// <summary>
        /// Attaches a filter of type <typeparamref name="T"/> to every action matching a predicate.
        /// </summary>
        /// <typeparam name="T">The filter type to instantiate per matched action.</typeparam>
        /// <param name="provider">The action descriptor collection provider.</param>
        /// <param name="serviceProvider">The DI container used to construct the filter.</param>
        /// <param name="matcher">Predicate selecting which controller actions to hook.</param>
        /// <returns>The number of actions the filter was attached to.</returns>
        public static int AddDynamicFilter<T>(
            this IActionDescriptorCollectionProvider provider,
            IServiceProvider serviceProvider,
            Func<ControllerActionDescriptor, bool> matcher)
            where T : IFilterMetadata
        {
            var targets = provider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(matcher)
                .ToArray();

            foreach (var action in targets)
            {
                var filter = ActivatorUtilities.CreateInstance<T>(serviceProvider);
                action.FilterDescriptors.Add(new FilterDescriptor(filter, FilterScope.Global));
            }

            return targets.Length;
        }
    }
}
