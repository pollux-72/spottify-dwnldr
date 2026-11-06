namespace Spottify.Core.Domain;

/// <summary>Lifecycle of a <see cref="DownloadRequest"/>, from the user's chat message to the imported album.</summary>
public enum RequestStatus
{
    /// <summary>The user asked for a song or album; nothing has been resolved yet.</summary>
    Requested,

    /// <summary>The request was mapped to a concrete album via the metadata provider.</summary>
    Resolved,

    /// <summary>The indexer is being searched for a matching release.</summary>
    Searching,

    /// <summary>No acceptable release was found. Nothing doubtful is downloaded.</summary>
    NotFound,

    /// <summary>A release was chosen and handed to the downloader.</summary>
    Downloading,

    /// <summary>The download finished and the album is in the library.</summary>
    Imported,

    /// <summary>A step failed; see <see cref="DownloadRequest.ErrorMessage"/>.</summary>
    Failed,
}
