using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.Sabnzbd;
using Spottify.Tests.Support;
using static Spottify.Tests.Sabnzbd.SabnzbdHarness;

namespace Spottify.Tests.Sabnzbd;

public sealed class SabnzbdDownloaderTests
{
    private const string JobId = "SABnzbd_nzo_kyt1f0";
    private static readonly Uri Nzb = new("https://indexer.example/getnzb/abc.nzb?r=INDEXERKEY");

    private static string SubmitOk() => $$"""{"status":true,"nzo_ids":["{{JobId}}"]}""";

    // ---- submit

    [Fact]
    public async Task Submit_sends_the_nzb_link_a_job_name_and_the_music_category_and_returns_the_job_id()
    {
        await using var harness = new SabnzbdHarness(_ => Json(SubmitOk()));

        var id = await harness.Downloader.SubmitAsync(Nzb, "Daft.Punk-Discovery-2001-FLAC", CancellationToken.None);

        Assert.Equal(JobId, id);
        var request = Assert.Single(harness.Requests);
        Assert.Equal("/api", request.AbsolutePath);
        Assert.Equal("addurl", Mode(request));
        Assert.Equal(Nzb.AbsoluteUri, Query(request, "name"));
        Assert.Equal("Daft.Punk-Discovery-2001-FLAC", Query(request, "nzbname"));
        Assert.Equal("music", Query(request, "cat"));
        Assert.Equal("json", Query(request, "output"));
        Assert.Equal(ApiKey, Query(request, "apikey"));
    }

    [Fact]
    public async Task The_category_comes_from_the_configuration()
    {
        var options = Options();
        options.Category = "audio";
        await using var harness = new SabnzbdHarness(_ => Json(SubmitOk()), options);

        await harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None);

        Assert.Equal("audio", Query(harness.Requests[0], "cat"));
    }

    [Fact]
    public async Task Job_names_lose_control_characters_and_are_shortened()
    {
        await using var harness = new SabnzbdHarness(_ => Json(SubmitOk()));

        await harness.Downloader.SubmitAsync(Nzb, "  Daft\r\nPunk\0 " + new string('a', 500), CancellationToken.None);

        var name = Query(harness.Requests[0], "nzbname");
        Assert.StartsWith("DaftPunk", name, StringComparison.Ordinal);
        Assert.Equal(200, name.Length);
    }

    [Theory]
    [InlineData("ftp://indexer.example/a.nzb")]
    [InlineData("file:///etc/passwd")]
    public async Task Only_http_links_are_handed_to_sabnzbd(string link)
    {
        await using var harness = new SabnzbdHarness(_ => Json(SubmitOk()));

        await Assert.ThrowsAsync<ArgumentException>(
            () => harness.Downloader.SubmitAsync(new Uri(link), "x", CancellationToken.None));

        Assert.Empty(harness.Requests);
    }

    [Fact]
    public async Task A_rejected_api_key_is_an_authentication_error_even_with_status_200()
    {
        await using var harness = new SabnzbdHarness(_ => Json("""{"status":false,"error":"API Key Incorrect"}"""));

        var failure = await Assert.ThrowsAsync<DownloaderAuthenticationException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));

        AssertNoKeys(failure);
    }

    [Fact]
    public async Task Other_errors_keep_the_message_of_sabnzbd()
    {
        await using var harness = new SabnzbdHarness(_ => Json("""{"status":false,"error":"Not a valid NZB"}"""));

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));

        Assert.IsNotType<DownloaderAuthenticationException>(failure);
        Assert.Contains("Not a valid NZB", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"status":true,"nzo_ids":[]}""")]
    [InlineData("""{"status":true}""")]
    [InlineData("""{"status":true,"nzo_ids":["../../etc/passwd"]}""")]
    [InlineData("""{"status":true,"nzo_ids":[42]}""")]
    public async Task An_answer_without_a_usable_job_id_is_an_error(string json)
    {
        await using var harness = new SabnzbdHarness(_ => Json(json));

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));

        Assert.Contains("no job id", failure.Message, StringComparison.Ordinal);
    }

    // ---- status

    [Theory]
    [InlineData("Downloading", DownloadJobState.Downloading)]
    [InlineData("Queued", DownloadJobState.Queued)]
    [InlineData("Paused", DownloadJobState.Queued)]
    [InlineData("Fetching", DownloadJobState.Queued)]
    [InlineData("Propagating", DownloadJobState.Queued)]
    [InlineData("Grabbing", DownloadJobState.Queued)]
    [InlineData("Verifying", DownloadJobState.Extracting)]
    [InlineData("Repairing", DownloadJobState.Extracting)]
    [InlineData("Extracting", DownloadJobState.Extracting)]
    [InlineData("Moving", DownloadJobState.Extracting)]
    [InlineData("Running", DownloadJobState.Extracting)]
    [InlineData("SomethingNew", DownloadJobState.Queued)]
    public async Task Queue_states_map_to_job_states(string sabState, DownloadJobState expected)
    {
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? Queue(JobId, sabState) : EmptyHistory));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(expected, status.State);
        Assert.Null(status.OutputPath);
        Assert.Null(status.Error);
    }

    [Fact]
    public async Task The_status_request_asks_for_this_job_only()
    {
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? Queue(JobId, "Downloading") : EmptyHistory));

        await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        var request = Assert.Single(harness.Requests);
        Assert.Equal("queue", Mode(request));
        Assert.Equal(JobId, Query(request, "nzo_ids"));
    }

    [Fact]
    public async Task A_finished_job_is_found_in_the_history_with_its_folder()
    {
        await using var harness = new SabnzbdHarness(ByMode(mode =>
            mode == "queue" ? EmptyQueue : History(JobId, "Completed", "/downloads/complete/music/Daft.Punk")));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(DownloadJobState.Completed, status.State);
        Assert.Equal("/downloads/complete/music/Daft.Punk", status.OutputPath);
        Assert.Null(status.Error);
    }

    [Fact]
    public async Task A_completed_job_without_a_folder_is_reported_as_failed()
    {
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? EmptyQueue : History(JobId, "Completed")));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(DownloadJobState.Failed, status.State);
        Assert.Contains("no folder", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_working_folder_path_is_never_used_as_the_finished_folder()
    {
        // "path" is where SABnzbd works on the job; only "storage" holds the finished files.
        const string history = """{"history":{"slots":[{"nzo_id":"SABnzbd_nzo_kyt1f0","status":"Completed","path":"/downloads/incomplete/X"}]}}""";
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? EmptyQueue : history));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(DownloadJobState.Failed, status.State);
        Assert.Null(status.OutputPath);
        Assert.Contains("no folder", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_job_carries_the_reason_of_sabnzbd()
    {
        await using var harness = new SabnzbdHarness(ByMode(mode =>
            mode == "queue" ? EmptyQueue : History(JobId, "Failed", failMessage: "Aborted, cannot be completed")));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(DownloadJobState.Failed, status.State);
        Assert.Equal("Aborted, cannot be completed", status.Error);
        Assert.Null(status.OutputPath);
    }

    [Fact]
    public async Task A_failed_job_without_a_reason_still_has_an_error_text()
    {
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? EmptyQueue : History(JobId, "Failed")));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(status.Error));
    }

    [Theory]
    [InlineData("Verifying")]
    [InlineData("Repairing")]
    [InlineData("Extracting")]
    [InlineData("Moving")]
    [InlineData("Running")]
    public async Task Post_processing_steps_in_the_history_are_not_final(string sabState)
    {
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? EmptyQueue : History(JobId, sabState)));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(DownloadJobState.Extracting, status.State);
    }

    [Fact]
    public async Task A_job_that_sabnzbd_does_not_know_is_reported_as_failed_not_thrown()
    {
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? EmptyQueue : EmptyHistory));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(DownloadJobState.Failed, status.State);
        Assert.Contains("does not know this job", status.Error, StringComparison.Ordinal);
        Assert.Equal(4, harness.Requests.Count); // queue and history, twice
    }

    [Fact]
    public async Task A_job_that_moves_from_queue_to_history_between_the_calls_is_not_lost()
    {
        var queueCalls = 0;
        await using var harness = new SabnzbdHarness(ByMode(mode =>
            mode == "queue"
                ? (++queueCalls == 1 ? Queue(JobId, "Downloading") : EmptyQueue)
                : History(JobId, "Completed", "/downloads/complete/music/X")));

        // First call: still in the queue.
        Assert.Equal(DownloadJobState.Downloading, (await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None)).State);

        // Second call: gone from the queue, present in the history.
        Assert.Equal(DownloadJobState.Completed, (await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None)).State);
    }

    [Fact]
    public async Task A_job_missing_in_the_first_pass_but_present_in_the_second_is_found()
    {
        var historyCalls = 0;
        await using var harness = new SabnzbdHarness(ByMode(mode =>
            mode == "queue"
                ? EmptyQueue
                : (++historyCalls == 1 ? EmptyHistory : History(JobId, "Completed", "/downloads/x"))));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(DownloadJobState.Completed, status.State);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task A_blank_job_id_is_rejected_before_any_request(string id)
    {
        await using var harness = new SabnzbdHarness(_ => Json("{}"));

        await Assert.ThrowsAnyAsync<ArgumentException>(() => harness.Downloader.GetStatusAsync(id, CancellationToken.None));

        Assert.Empty(harness.Requests);
    }

    [Theory]
    [InlineData("a&mode=delete")]
    [InlineData("../x")]
    [InlineData("a b")]
    public async Task A_job_id_that_could_change_the_request_is_rejected(string id)
    {
        await using var harness = new SabnzbdHarness(_ => Json("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => harness.Downloader.GetStatusAsync(id, CancellationToken.None));

        Assert.Empty(harness.Requests);
    }

    // ---- folder mapping

    [Theory]
    [InlineData("/downloads", "/data/downloads", "/downloads/complete/music/X", "/data/downloads/complete/music/X")]
    [InlineData("/downloads/", "/data/downloads/", "/downloads/complete/music/X", "/data/downloads/complete/music/X")]
    [InlineData("/downloads", "/data/downloads", "/downloads", "/data/downloads")]
    [InlineData("/downloads", "/data/downloads", "/downloadsX/complete/X", "/downloadsX/complete/X")] // not the same folder
    [InlineData("/downloads", "/data/downloads", "/other/complete/X", "/other/complete/X")]
    [InlineData("C:\\Downloads", "/mnt/dl", "C:\\Downloads\\complete\\X", "/mnt/dl/complete/X")]
    public async Task Folders_are_translated_to_the_path_this_service_uses(string remote, string local, string reported, string expected)
    {
        var options = Options();
        options.RemotePath = remote;
        options.LocalPath = local;
        await using var harness = new SabnzbdHarness(
            ByMode(mode => mode == "queue" ? EmptyQueue : History(JobId, "Completed", reported.Replace("\\", "\\\\", StringComparison.Ordinal))),
            options);

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal(expected, status.OutputPath);
    }

    [Fact]
    public async Task Without_a_mapping_the_folder_is_passed_on_unchanged()
    {
        await using var harness = new SabnzbdHarness(ByMode(mode =>
            mode == "queue" ? EmptyQueue : History(JobId, "Completed", "/downloads/complete/music/X")));

        var status = await harness.Downloader.GetStatusAsync(JobId, CancellationToken.None);

        Assert.Equal("/downloads/complete/music/X", status.OutputPath);
    }

    // ---- failures of the connection

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(DownloaderAuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(DownloaderAuthenticationException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(DownloaderException))]
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(DownloaderException))]
    public async Task Http_errors_become_the_matching_downloader_exception(HttpStatusCode status, Type expected)
    {
        await using var harness = new SabnzbdHarness(_ => Json("<html>no</html>", status));

        var failure = await Assert.ThrowsAnyAsync<DownloaderException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));

        Assert.Equal(expected, failure.GetType());
        AssertNoKeys(failure);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, """{"status":false,"error":"Forbidden"}""")]
    [InlineData(HttpStatusCode.Forbidden, """{"status":false,"error":"Not allowed"}""")]
    [InlineData(HttpStatusCode.Forbidden, """{"error":"Something completely different"}""")]
    [InlineData(HttpStatusCode.Forbidden, "")]
    public async Task The_http_status_decides_that_a_key_was_refused_whatever_the_error_text_says(HttpStatusCode status, string body)
    {
        await using var harness = new SabnzbdHarness(_ => Json(body, status));

        var failure = await Assert.ThrowsAsync<DownloaderAuthenticationException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));

        AssertNoKeys(failure);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"queue":null}""")]
    [InlineData("""{"queue":{}}""")]
    [InlineData("""{"queue":{"slots":"none"}}""")]
    [InlineData("""{"queue":[]}""")]
    [InlineData("""{"history":{"slots":[]}}""")] // the wrong list for this call
    public async Task A_queue_answer_with_an_unexpected_shape_is_an_error_not_an_unknown_job(string queueAnswer)
    {
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? queueAnswer : EmptyHistory));

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.GetStatusAsync(JobId, CancellationToken.None));

        Assert.Contains("unexpected answer", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"history":null}""")]
    [InlineData("""{"history":{"noofslots":0}}""")]
    [InlineData("""{"history":{"slots":{}}}""")]
    public async Task A_history_answer_with_an_unexpected_shape_is_an_error_not_an_unknown_job(string historyAnswer)
    {
        await using var harness = new SabnzbdHarness(ByMode(mode => mode == "queue" ? EmptyQueue : historyAnswer));

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.GetStatusAsync(JobId, CancellationToken.None));

        Assert.Contains("unexpected answer", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_connection_that_breaks_while_the_body_is_read_is_a_downloader_exception()
    {
        await using var harness = new SabnzbdHarness(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new BrokenConnectionContent() });

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.GetStatusAsync(JobId, CancellationToken.None));

        Assert.IsAssignableFrom<IOException>(failure.InnerException);
        AssertNoKeys(failure);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("[1,2,3]")]
    [InlineData("\"text\"")]
    public async Task Answers_that_are_not_a_json_object_are_errors(string body)
    {
        await using var harness = new SabnzbdHarness(_ => Json(body));

        await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));
    }

    [Fact]
    public async Task A_network_failure_is_a_downloader_exception_without_the_key()
    {
        await using var harness = new SabnzbdHarness((_, _) => throw new HttpRequestException("Connection refused"));

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));

        Assert.IsType<HttpRequestException>(failure.InnerException);
        AssertNoKeys(failure);
    }

    [Fact]
    public async Task A_slow_sabnzbd_is_a_downloader_exception()
    {
        var options = Options();
        options.RequestTimeout = TimeSpan.FromSeconds(1);
        await using var harness = new SabnzbdHarness(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Json("{}");
        }, options);

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));

        Assert.Contains("did not answer in time", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_body_that_stalls_after_the_headers_times_out()
    {
        var options = Options();
        options.RequestTimeout = TimeSpan.FromSeconds(1);
        await using var harness = new SabnzbdHarness(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new GeneratedContent(bytes: -1) }, options);

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15)));

        Assert.Contains("did not answer in time", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_oversized_answer_is_cut_off_at_the_limit()
    {
        var options = Options();
        options.MaxResponseBytes = 1024;
        var content = new GeneratedContent(bytes: 10_000_000);
        await using var harness = new SabnzbdHarness(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content }, options);

        var failure = await Assert.ThrowsAsync<DownloaderException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None));

        Assert.Contains("larger than the allowed", failure.Message, StringComparison.Ordinal);
        Assert.True(content.BytesWritten < 1_000_000);
    }

    [Fact]
    public async Task Cancelling_the_call_is_not_reported_as_a_downloader_failure()
    {
        await using var harness = new SabnzbdHarness(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Json("{}");
        });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Downloader.SubmitAsync(Nzb, "x", cancellation.Token));
    }

    [Fact]
    public async Task The_api_keys_never_reach_the_log_not_even_at_trace_level()
    {
        var log = new CapturingLoggerProvider();
        await using var harness = new SabnzbdHarness(_ => Json(SubmitOk()), loggers: log);

        await harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None);

        // Prove the capture works at all, otherwise "no key in the log" says nothing.
        harness.Services.GetRequiredService<ILoggerFactory>().CreateLogger("probe").LogInformation("probe");
        Assert.Contains(log.Messages, m => m == "probe");

        Assert.DoesNotContain(log.Messages, m => m.Contains(ApiKey, StringComparison.Ordinal));
        Assert.DoesNotContain(log.Messages, m => m.Contains("INDEXERKEY", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("http://sabnzbd:8080", "/api")]
    [InlineData("http://sabnzbd:8080/", "/api")]
    [InlineData("http://192.168.1.5:8080/sabnzbd", "/sabnzbd/api")]
    public async Task The_api_is_found_below_the_base_url_with_or_without_slash(string baseUrl, string expectedPath)
    {
        await using var harness = new SabnzbdHarness(_ => Json(SubmitOk()), Options(baseUrl));

        await harness.Downloader.SubmitAsync(Nzb, "x", CancellationToken.None);

        Assert.Equal(expectedPath, harness.Requests[0].AbsolutePath);
    }

    private static void AssertNoKeys(Exception exception)
    {
        Assert.DoesNotContain(ApiKey, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("INDEXERKEY", exception.Message, StringComparison.Ordinal);
    }
}
