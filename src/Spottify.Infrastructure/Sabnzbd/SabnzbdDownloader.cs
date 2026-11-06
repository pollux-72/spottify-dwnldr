using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.Http;

namespace Spottify.Infrastructure.Sabnzbd;

/// <summary><see cref="IDownloader"/> for SABnzbd, using its HTTP API with JSON output.</summary>
/// <remarks>
/// The API key travels as a query parameter, which is how SABnzbd works. Therefore this client's HTTP logging is
/// switched off at registration, and no message built here contains the request URL.
/// </remarks>
internal sealed partial class SabnzbdDownloader(HttpClient http, IOptions<SabnzbdOptions> options) : IDownloader
{
    private const int MaxJobNameLength = 200;

    public async Task<string> SubmitAsync(Uri nzbUrl, string jobName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nzbUrl);
        if (nzbUrl.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("The NZB link must be an http or https address.", nameof(nzbUrl));
        }

        using var answer = await CallAsync(
            "addurl",
            [("name", nzbUrl.AbsoluteUri), ("nzbname", CleanName(jobName)), ("cat", options.Value.Category)],
            cancellationToken).ConfigureAwait(false);

        // {"status": true, "nzo_ids": ["SABnzbd_nzo_kyt1f0"]}
        if (answer.RootElement.TryGetProperty("nzo_ids", out var ids)
            && ids.ValueKind == JsonValueKind.Array
            && ids.GetArrayLength() > 0
            && ids[0] is { ValueKind: JsonValueKind.String } first
            && first.GetString() is { Length: > 0 } id
            && JobId().IsMatch(id))
        {
            return id;
        }

        throw new DownloaderException("SABnzbd accepted the request but returned no job id.");
    }

    public async Task<DownloadJobStatus> GetStatusAsync(string jobId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        if (!JobId().IsMatch(jobId))
        {
            throw new ArgumentException("Not a SABnzbd job id.", nameof(jobId));
        }

        // A job moves from the queue to the history when it finishes downloading. If it is in neither list it may
        // have moved between the two calls, so look once more before declaring it unknown.
        for (var pass = 0; pass < 2; pass++)
        {
            if (await FindInQueueAsync(jobId, cancellationToken).ConfigureAwait(false) is { } queued)
            {
                return queued;
            }

            if (await FindInHistoryAsync(jobId, cancellationToken).ConfigureAwait(false) is { } finished)
            {
                return finished;
            }
        }

        return new DownloadJobStatus(DownloadJobState.Failed, null, "SABnzbd does not know this job. It may have been removed there.");
    }

    private async Task<DownloadJobStatus?> FindInQueueAsync(string jobId, CancellationToken cancellationToken)
    {
        using var answer = await CallAsync("queue", [("nzo_ids", jobId)], cancellationToken).ConfigureAwait(false);

        if (FindSlot(answer, "queue", jobId) is not { } slot)
        {
            return null;
        }

        // Queue states: Downloading, Queued, Paused, Fetching, Grabbing, Propagating, Checking, ...
        // Post-processing steps (Verifying, Repairing, Extracting, Moving, Running) also appear while still listed.
        var state = Text(slot, "status") switch
        {
            "Downloading" => DownloadJobState.Downloading,
            "Verifying" or "Repairing" or "Extracting" or "Moving" or "Running" or "QuickCheck" => DownloadJobState.Extracting,
            _ => DownloadJobState.Queued, // also states this client does not know: they are not final
        };

        return new DownloadJobStatus(state, null, null);
    }

    private async Task<DownloadJobStatus?> FindInHistoryAsync(string jobId, CancellationToken cancellationToken)
    {
        using var answer = await CallAsync("history", [("nzo_ids", jobId)], cancellationToken).ConfigureAwait(false);

        if (FindSlot(answer, "history", jobId) is not { } slot)
        {
            return null;
        }

        switch (Text(slot, "status"))
        {
            case "Completed":
                // "storage" is where the finished files are. "path" is the working folder of the job, which does not
                // hold the result, so it is not used as a fallback.
                var folder = Text(slot, "storage");

                return string.IsNullOrEmpty(folder)
                    ? new DownloadJobStatus(DownloadJobState.Failed, null, "SABnzbd reports the job as completed but gives no folder.")
                    : new DownloadJobStatus(DownloadJobState.Completed, MapToLocalPath(folder), null);

            case "Failed":
                var reason = Text(slot, "fail_message");
                return new DownloadJobStatus(
                    DownloadJobState.Failed,
                    null,
                    string.IsNullOrWhiteSpace(reason) ? "SABnzbd reports the download as failed." : reason);

            default:
                // Verifying, Repairing, Extracting, Moving, Running ... are steps after the download.
                return new DownloadJobStatus(DownloadJobState.Extracting, null, null);
        }
    }

    /// <summary>
    /// The slot of the job, or <c>null</c> when the list is valid and the job is not in it. An answer that does not
    /// have the expected shape is an error: treating it as "empty" would make a faulty answer look like a removed job.
    /// </summary>
    private static JsonElement? FindSlot(JsonDocument answer, string listName, string jobId)
    {
        if (!answer.RootElement.TryGetProperty(listName, out var list)
            || list.ValueKind != JsonValueKind.Object
            || !list.TryGetProperty("slots", out var slots)
            || slots.ValueKind != JsonValueKind.Array)
        {
            throw new DownloaderException($"SABnzbd sent an unexpected answer for the {listName}.");
        }

        foreach (var slot in slots.EnumerateArray())
        {
            if (slot.ValueKind == JsonValueKind.Object && Text(slot, "nzo_id") == jobId)
            {
                return slot;
            }
        }

        return null;
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>Translates a folder reported by SABnzbd into the path this service uses.</summary>
    private string MapToLocalPath(string reported)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.RemotePath) || string.IsNullOrWhiteSpace(settings.LocalPath))
        {
            return reported;
        }

        // SABnzbd may run on Windows, so compare with one kind of separator.
        var remote = settings.RemotePath.Replace('\\', '/').TrimEnd('/');
        var path = reported.Replace('\\', '/');

        if (path.Equals(remote, StringComparison.OrdinalIgnoreCase))
        {
            return settings.LocalPath;
        }

        return path.StartsWith(remote + "/", StringComparison.OrdinalIgnoreCase)
            ? settings.LocalPath.TrimEnd('/', '\\') + "/" + path[(remote.Length + 1)..]
            : reported;
    }

    /// <summary>Calls the API and returns the parsed JSON. The caller disposes the document.</summary>
    private async Task<JsonDocument> CallAsync(string mode, (string Name, string Value)[] parameters, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var url = "api?output=json"
            + $"&mode={Uri.EscapeDataString(mode)}"
            + string.Concat(parameters.Select(p => $"&{p.Name}={Uri.EscapeDataString(p.Value)}"))
            + $"&apikey={Uri.EscapeDataString(settings.ApiKey)}";

        // One timeout for the whole exchange, headers and body (see the Newznab client for why).
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.RequestTimeout);

        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            await using var body = await LimitedBody.ReadAsync(
                response, settings.MaxResponseBytes, () => TooLarge(settings.MaxResponseBytes), timeout.Token).ConfigureAwait(false);

            var document = TryParse(body);

            try
            {
                // A refused key can come as HTTP 401/403 with or without an error text, or as
                // {"status": false, "error": "API Key Incorrect"} with HTTP 200. The status counts first.
                var errorText = ErrorText(document);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    throw new DownloaderAuthenticationException(
                        $"SABnzbd refused the API key (HTTP {(int)response.StatusCode})" + (errorText is null ? "." : $": {errorText}"));
                }

                if (errorText is not null)
                {
                    throw ErrorFor(errorText);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new DownloaderException($"SABnzbd answered HTTP {(int)response.StatusCode}.");
                }

                return document is { RootElement.ValueKind: JsonValueKind.Object }
                    ? document
                    : throw new DownloaderException("The SABnzbd answer is not a JSON object.");
            }
            catch
            {
                document?.Dispose();
                throw;
            }
        }
        catch (HttpRequestException exception)
        {
            // Keep the HTTP stack's text out of the message and let callers read the inner exception if needed.
            throw new DownloaderException("SABnzbd could not be reached.", exception);
        }
        catch (IOException exception)
        {
            // The connection broke while the body was being read.
            throw new DownloaderException("The connection to SABnzbd was interrupted.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DownloaderException("SABnzbd did not answer in time.", exception);
        }
    }

    private static string? ErrorText(JsonDocument? document) =>
        document is { RootElement.ValueKind: JsonValueKind.Object }
        && document.RootElement.TryGetProperty("error", out var error)
        && error.ValueKind == JsonValueKind.String
            ? error.GetString()
            : null;

    private static JsonDocument? TryParse(Stream body)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DownloaderException ErrorFor(string message) =>
        message.Contains("API Key", StringComparison.OrdinalIgnoreCase) || message.Contains("authentication", StringComparison.OrdinalIgnoreCase)
            ? new DownloaderAuthenticationException($"SABnzbd refused the API key: {message}")
            : new DownloaderException($"SABnzbd reported an error: {message}");

    private static DownloaderException TooLarge(long maxBytes) =>
        new(string.Create(CultureInfo.InvariantCulture, $"The SABnzbd answer is larger than the allowed {maxBytes} bytes."));

    // The name becomes a folder name in SABnzbd, which cleans it up itself; this only keeps control characters
    // and absurd lengths out of the request.
    private static string CleanName(string name)
    {
        var cleaned = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return cleaned.Length > MaxJobNameLength ? cleaned[..MaxJobNameLength] : cleaned;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,100}$")]
    private static partial Regex JobId();
}
