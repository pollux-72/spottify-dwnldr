namespace Spottify.Core.Abstractions;

/// <summary>Searches a Usenet indexer for album releases (Newznab API in production).</summary>
public interface IIndexer
{
    /// <summary>
    /// Returns the releases the indexer lists for the album, in the indexer's order. Judging which one is the right
    /// release is a separate step. Failures are reported as <see cref="IndexerException"/> or one of its subtypes.
    /// </summary>
    Task<IReadOnlyList<IndexerRelease>> SearchAsync(string artist, string album, CancellationToken cancellationToken);
}

/// <param name="Title">The release name as posted, e.g. <c>Daft.Punk-Discovery-2001-FLAC</c>.</param>
/// <param name="SizeBytes">Size of the release, or 0 when the indexer does not say.</param>
/// <param name="PublishedAt">When the release was published; the age of a release is derived from it.</param>
/// <param name="Category">Newznab category id such as <c>3040</c> (Audio/Lossless) or <c>3010</c> (Audio/MP3).</param>
/// <param name="NzbUrl">Download link of the NZB. It usually contains the account's API key, so do not log it.</param>
public sealed record IndexerRelease(
    string Title,
    long SizeBytes,
    DateTimeOffset PublishedAt,
    string Category,
    Uri NzbUrl);
