namespace Spottify.Core.Abstractions;

/// <summary>Hands an NZB to the downloader (SABnzbd in production) and reports progress.</summary>
public interface IDownloader
{
    /// <summary>
    /// Queues the NZB and returns the downloader's job id. Failures are reported as <see cref="DownloaderException"/>
    /// or <see cref="DownloaderAuthenticationException"/>.
    /// </summary>
    /// <param name="nzbUrl">Link to the NZB. The downloader fetches it itself.</param>
    /// <param name="jobName">Name shown by the downloader and used for the download folder.</param>
    Task<string> SubmitAsync(Uri nzbUrl, string jobName, CancellationToken cancellationToken);

    /// <summary>
    /// Current state of a job. A job the downloader does not know any more (for example removed by hand) is
    /// reported as <see cref="DownloadJobState.Failed"/>, not as an exception.
    /// </summary>
    Task<DownloadJobStatus> GetStatusAsync(string jobId, CancellationToken cancellationToken);
}

public enum DownloadJobState
{
    /// <summary>Waiting for a free slot, paused, or still being fetched from the indexer.</summary>
    Queued,

    Downloading,

    /// <summary>The download is complete and is being verified, repaired, unpacked or moved.</summary>
    Extracting,

    Completed,

    Failed,
}

/// <param name="State">Where the job is.</param>
/// <param name="OutputPath">Folder with the finished files as this service sees it; only set when <see cref="DownloadJobState.Completed"/>.</param>
/// <param name="Error">Why the job failed; only set when <see cref="DownloadJobState.Failed"/>.</param>
public sealed record DownloadJobStatus(DownloadJobState State, string? OutputPath, string? Error);
