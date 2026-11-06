namespace Spottify.Core.Abstractions;

/// <summary>Turns free text into albums and track lists (MusicBrainz in production).</summary>
public interface IMetadataProvider
{
    /// <summary>
    /// Finds albums that contain the requested song, best match first. Never picks for the caller: when several
    /// albums are plausible they are all returned so the user can choose.
    /// </summary>
    Task<IReadOnlyList<AlbumCandidate>> SearchAsync(string query, CancellationToken cancellationToken);

    /// <summary>Returns the album with its track list, or <c>null</c> when the id is unknown.</summary>
    Task<AlbumDetails?> GetAlbumAsync(string releaseGroupId, CancellationToken cancellationToken);
}

/// <param name="ReleaseGroupId">MusicBrainz release group: the album as an abstract work, independent of editions.</param>
/// <param name="Year">
/// Earliest year among the editions the search listed for this song. It can be later than the original release
/// (a search only lists some editions), so treat it as a hint; <see cref="AlbumDetails.Year"/> is the reliable one.
/// </param>
/// <param name="ReleaseType">E.g. <c>Album</c>, <c>Single</c>, <c>Album+Compilation</c>.</param>
/// <param name="MatchedTrackTitle">The song of the request as it is titled on this album.</param>
/// <param name="Score">Match quality from 0 to 100 as reported by the provider.</param>
public sealed record AlbumCandidate(
    string ReleaseGroupId,
    string Artist,
    string AlbumTitle,
    int? Year,
    string ReleaseType,
    string MatchedTrackTitle,
    int Score);

/// <param name="ReleaseId">The concrete edition whose track list is returned.</param>
public sealed record AlbumDetails(
    string ReleaseGroupId,
    string ReleaseId,
    string Artist,
    string AlbumTitle,
    int? Year,
    IReadOnlyList<string> TrackTitles)
{
    public int TrackCount => TrackTitles.Count;
}
