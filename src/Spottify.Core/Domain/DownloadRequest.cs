namespace Spottify.Core.Domain;

/// <summary>
/// One user request ("Artist - Title") and everything the pipeline learns about it on the way
/// to an imported album. This is the persistent state that lets the workflow survive restarts.
/// </summary>
public sealed class DownloadRequest
{
    public Guid Id { get; set; }

    /// <summary>The text exactly as the user entered it.</summary>
    public required string QueryText { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Requested;

    public string? Artist { get; set; }

    public string? AlbumTitle { get; set; }

    /// <summary>MusicBrainz release group (the album as an abstract work). Used to detect duplicate requests.</summary>
    public string? MusicBrainzReleaseGroupId { get; set; }

    /// <summary>MusicBrainz release (a concrete edition with a track list).</summary>
    public string? MusicBrainzReleaseId { get; set; }

    /// <summary>Title of the indexer release that was chosen.</summary>
    public string? SelectedReleaseTitle { get; set; }

    /// <summary>NZB link of the chosen release.</summary>
    public string? SelectedNzbUrl { get; set; }

    /// <summary>Job id returned by the downloader (SABnzbd).</summary>
    public string? DownloaderJobId { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
