using System;

namespace Jellyfin.Plugin.CrowdMix.LastFm
{
    /// <summary>
    /// A normalized (artist, title) identity used to match tracks across Jellyfin and Last.fm.
    /// Values are expected to be pre-normalized (see <see cref="TrackNormalizer"/>), so default
    /// value-equality is a correct catalog match.
    /// </summary>
    public readonly struct TrackKey : IEquatable<TrackKey>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackKey"/> struct.
        /// </summary>
        /// <param name="artist">The normalized artist name.</param>
        /// <param name="title">The normalized track title.</param>
        public TrackKey(string artist, string title)
        {
            Artist = artist ?? string.Empty;
            Title = title ?? string.Empty;
        }

        /// <summary>
        /// Gets the normalized artist name.
        /// </summary>
        public string Artist { get; }

        /// <summary>
        /// Gets the normalized track title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets a value indicating whether this key has both an artist and a title.
        /// </summary>
        public bool IsValid => Artist.Length > 0 && Title.Length > 0;

        /// <summary>
        /// Determines whether two keys are equal.
        /// </summary>
        /// <param name="left">The left key.</param>
        /// <param name="right">The right key.</param>
        /// <returns><c>true</c> if the keys are equal.</returns>
        public static bool operator ==(TrackKey left, TrackKey right) => left.Equals(right);

        /// <summary>
        /// Determines whether two keys are not equal.
        /// </summary>
        /// <param name="left">The left key.</param>
        /// <param name="right">The right key.</param>
        /// <returns><c>true</c> if the keys are not equal.</returns>
        public static bool operator !=(TrackKey left, TrackKey right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(TrackKey other)
        {
            return string.Equals(Artist, other.Artist, StringComparison.Ordinal)
                && string.Equals(Title, other.Title, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is TrackKey other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Artist, Title);

        /// <inheritdoc />
        public override string ToString() => $"{Artist} - {Title}";
    }
}
