using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CrowdMix.Configuration
{
    /// <summary>
    /// Plugin configuration for CrowdMix.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            LastFmApiKey = string.Empty;
            Enabled = true;
            FallbackToNative = true;
            MixSize = 50;
            CrowdSimilarityWeight = 1.0;
            TagOverlapWeight = 0.4;
            PlayAffinityWeight = 0.3;
            FavoriteBonusWeight = 0.25;
            MaxTracksPerArtist = 2;
            EnableDailyMix = false;
            DailyMixSize = 30;
        }

        /// <summary>
        /// Gets or sets the Last.fm API key used to read the crowd signal.
        /// </summary>
        public string LastFmApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether CrowdMix overrides the native Instant Mix.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to keep the native mix result when CrowdMix
        /// produces no owned matches (e.g. the seed is unknown to Last.fm).
        /// </summary>
        public bool FallbackToNative { get; set; }

        /// <summary>
        /// Gets or sets the target number of tracks in an instant mix.
        /// </summary>
        public int MixSize { get; set; }

        /// <summary>
        /// Gets or sets the weight applied to the Last.fm crowd similarity score.
        /// </summary>
        public double CrowdSimilarityWeight { get; set; }

        /// <summary>
        /// Gets or sets the weight applied to genre/tag overlap with the seed.
        /// </summary>
        public double TagOverlapWeight { get; set; }

        /// <summary>
        /// Gets or sets the weight applied to the user's play affinity for a candidate.
        /// </summary>
        public double PlayAffinityWeight { get; set; }

        /// <summary>
        /// Gets or sets the flat bonus added when the user favorited a candidate.
        /// </summary>
        public double FavoriteBonusWeight { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tracks by a single artist in a mix (0 disables the cap).
        /// </summary>
        public int MaxTracksPerArtist { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the per-user daily mix task is enabled.
        /// </summary>
        public bool EnableDailyMix { get; set; }

        /// <summary>
        /// Gets or sets the target number of tracks in a daily mix.
        /// </summary>
        public int DailyMixSize { get; set; }

        /// <summary>
        /// Builds the re-ranking weights from the configured values.
        /// </summary>
        /// <returns>The mix weights.</returns>
        public Services.MixWeights ToWeights()
        {
            return new Services.MixWeights
            {
                CrowdSimilarity = CrowdSimilarityWeight,
                TagOverlap = TagOverlapWeight,
                PlayAffinity = PlayAffinityWeight,
                FavoriteBonus = FavoriteBonusWeight,
                MaxTracksPerArtist = MaxTracksPerArtist,
            };
        }
    }
}
