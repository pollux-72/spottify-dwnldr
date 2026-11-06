namespace Spottify.Core.Abstractions;

/// <summary>Turns free text into albums and track lists (MusicBrainz in production). Detailed by TASK-4.</summary>
public interface IMetadataProvider
{
    Task<IReadOnlyList<AlbumCandidate>> SearchAsync(string query, CancellationToken cancellationToken);

    Task<AlbumDetails?> GetAlbumAsync(string releaseGroupId, CancellationToken cancellationToken);
}

public sealed record AlbumCandidate(
    string ReleaseGroupId,
    string Artist,
    string AlbumTitle,
    int? Year);

public sealed record AlbumDetails(
    string ReleaseGroupId,
    string ReleaseId,
    string Artist,
    string AlbumTitle,
    int? Year,
    IReadOnlyList<string> TrackTitles);
