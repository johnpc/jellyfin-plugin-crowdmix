namespace Jellyfin.Plugin.CrowdMix.LastFm
{
    /// <summary>
    /// A single similar-track recommendation returned by the Last.fm crowd signal,
    /// carrying the match score Last.fm assigns (0..1, higher is more similar).
    /// </summary>
    public sealed class SimilarTrack
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimilarTrack"/> class.
        /// </summary>
        /// <param name="key">The normalized identity of the recommended track.</param>
        /// <param name="score">The Last.fm match score (0..1).</param>
        public SimilarTrack(TrackKey key, double score)
        {
            Key = key;
            Score = score;
        }

        /// <summary>
        /// Gets the normalized identity of the recommended track.
        /// </summary>
        public TrackKey Key { get; }

        /// <summary>
        /// Gets the Last.fm match score (0..1, higher is more similar).
        /// </summary>
        public double Score { get; }
    }
}
