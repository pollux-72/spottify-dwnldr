namespace Spottify.Core.Abstractions;

/// <summary>Hands an NZB to the downloader (SABnzbd in production) and reports progress. Detailed by TASK-10.</summary>
public interface IDownloader
{
    /// <summary>Submits the NZB and returns the downloader's job id.</summary>
    Task<string> SubmitAsync(Uri nzbUrl, string jobName, CancellationToken cancellationToken);

    Task<DownloadJobStatus> GetStatusAsync(string jobId, CancellationToken cancellationToken);
}

public enum DownloadJobState
{
    Queued,
    Downloading,
    Extracting,
    Completed,
    Failed,
}

/// <param name="OutputPath">Folder with the finished files; only set once the job is <see cref="DownloadJobState.Completed"/>.</param>
public sealed record DownloadJobStatus(DownloadJobState State, string? OutputPath, string? Error);
