namespace Spottify.Core.Abstractions;

/// <summary>Searches a Usenet indexer for album releases (Newznab API in production). Detailed by TASK-9.</summary>
public interface IIndexer
{
    Task<IReadOnlyList<IndexerRelease>> SearchAsync(string artist, string album, CancellationToken cancellationToken);
}

public sealed record IndexerRelease(
    string Title,
    long SizeBytes,
    DateTimeOffset PublishedAt,
    string Category,
    Uri NzbUrl);
