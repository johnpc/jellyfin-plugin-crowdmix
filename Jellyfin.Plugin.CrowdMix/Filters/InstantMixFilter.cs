using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Jellyfin.Plugin.CrowdMix.Services;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jellyfin.Plugin.CrowdMix.Filters
{
    /// <summary>
    /// Runs after Jellyfin's native Instant Mix action and, when CrowdMix is enabled,
    /// replaces the native <see cref="QueryResult{BaseItemDto}"/> with a crowd-sourced mix.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class InstantMixFilter : IAsyncActionFilter
    {
        private readonly InstantMixService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantMixFilter"/> class.
        /// </summary>
        /// <param name="service">The instant mix orchestrator.</param>
        public InstantMixFilter(InstantMixService service)
        {
            _service = service;
        }

        /// <inheritdoc />
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next().ConfigureAwait(false);

            if (!Plugin.GetConfiguration().Enabled)
            {
                return;
            }

            if (executed.Result is not ObjectResult objectResult
                || objectResult.Value is not QueryResult<BaseItemDto>)
            {
                return;
            }

            if (!TryGetArgument<Guid>(context, "itemId", out var itemId))
            {
                return;
            }

            var replacement = _service.GenerateMix(
                itemId,
                GetNullable<Guid>(context, "userId"),
                GetNullable<int>(context, "limit"),
                GetArray<ItemFields>(context, "fields"),
                GetNullable<bool>(context, "enableImages"),
                GetNullable<bool>(context, "enableUserData"),
                GetNullable<int>(context, "imageTypeLimit"),
                GetArray<ImageType>(context, "enableImageTypes"),
                context.HttpContext.User);

            executed.Result = new ObjectResult(replacement) { StatusCode = objectResult.StatusCode };
        }

        private static bool TryGetArgument<T>(ActionExecutingContext context, string name, out T value)
            where T : struct
        {
            if (context.ActionArguments.TryGetValue(name, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        private static T? GetNullable<T>(ActionExecutingContext context, string name)
            where T : struct
        {
            return context.ActionArguments.TryGetValue(name, out var raw) && raw is T typed ? typed : (T?)null;
        }

        private static T[] GetArray<T>(ActionExecutingContext context, string name)
        {
            return context.ActionArguments.TryGetValue(name, out var raw) && raw is T[] typed
                ? typed
                : Array.Empty<T>();
        }
    }
}
