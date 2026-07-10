namespace Jellyfin.Plugin.CrowdMix.Services
{
    /// <summary>
    /// Tunable weights for the hybrid re-ranking score. Mirrors the real
    /// "candidate generation then learned re-rank" architecture: the crowd signal
    /// generates candidates, and these weights fuse the re-ranking signals.
    /// </summary>
    public sealed class MixWeights
    {
        /// <summary>
        /// Gets or sets the weight applied to the Last.fm crowd similarity score.
        /// </summary>
        public double CrowdSimilarity { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the weight applied to genre/tag overlap with the seed track.
        /// </summary>
        public double TagOverlap { get; set; } = 0.4;

        /// <summary>
        /// Gets or sets the weight applied to the user's affinity (play history) for a candidate.
        /// </summary>
        public double PlayAffinity { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the flat bonus added when the user favorited a candidate.
        /// </summary>
        public double FavoriteBonus { get; set; } = 0.25;

        /// <summary>
        /// Gets or sets the maximum number of tracks by any single artist allowed in a mix.
        /// Last.fm ranks same-artist tracks near 1.0, which would otherwise front-load the mix
        /// with the seed artist. Zero or negative disables the cap.
        /// </summary>
        public int MaxTracksPerArtist { get; set; } = 2;

        /// <summary>
        /// Gets the default weights.
        /// </summary>
        /// <returns>A new instance with default weights.</returns>
        public static MixWeights Default() => new MixWeights();
    }
}
