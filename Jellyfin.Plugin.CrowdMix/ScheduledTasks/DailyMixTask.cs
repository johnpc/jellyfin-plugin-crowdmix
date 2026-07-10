using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CrowdMix.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CrowdMix.ScheduledTasks
{
    /// <summary>
    /// Thin orchestrator that rebuilds each user's daily mix on a schedule. All logic lives in
    /// <see cref="DailyMixService"/>; this class only wires it into Jellyfin's task system.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class DailyMixTask : IScheduledTask
    {
        private readonly DailyMixService _dailyMixService;
        private readonly ILogger<DailyMixTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DailyMixTask"/> class.
        /// </summary>
        /// <param name="dailyMixService">The daily mix service.</param>
        /// <param name="logger">The logger.</param>
        public DailyMixTask(DailyMixService dailyMixService, ILogger<DailyMixTask> logger)
        {
            _dailyMixService = dailyMixService;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "CrowdMix Daily Mix";

        /// <inheritdoc />
        public string Key => "CrowdMixDailyMix";

        /// <inheritdoc />
        public string Description => "Rebuilds each user's CrowdMix Daily playlist from their listening history.";

        /// <inheritdoc />
        public string Category => "CrowdMix";

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (!Plugin.GetConfiguration().EnableDailyMix)
            {
                _logger.LogInformation("CrowdMix: daily mix disabled, skipping.");
                progress.Report(100);
                return;
            }

            progress.Report(0);
            await _dailyMixService.RunAsync(cancellationToken).ConfigureAwait(false);
            progress.Report(100);
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.DailyTrigger,
                    TimeOfDayTicks = TimeSpan.FromHours(5).Ticks,
                },
            };
        }
    }
}
