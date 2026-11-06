using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Spottify.Infrastructure.MusicBrainz;
using static Spottify.Tests.MusicBrainz.MusicBrainzTestHarness;

namespace Spottify.Tests.MusicBrainz;

public sealed class MusicBrainzRequestHandlerTests
{
    // One album hit, so a search is answered by a single request. An empty answer would make the client
    // run its fallback search and double every request count below.
    private const string OneAlbumHit = """
        {"recordings":[{"title":"t","score":100,"artist-credit":[{"name":"A","joinphrase":""}],
          "releases":[{"id":"r","title":"T","status":"Official","date":"2000",
            "artist-credit":[{"name":"A","joinphrase":""}],
            "release-group":{"id":"g","title":"T","primary-type":"Album","secondary-types":[]}}]}]}
        """;

    [Fact]
    public async Task Consecutive_requests_are_spaced_by_the_minimum_interval()
    {
        var time = new FakeTimeProvider();
        var stamps = new List<DateTimeOffset>();
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.FromSeconds(1) };
        await using var harness = new MusicBrainzTestHarness(_ =>
        {
            stamps.Add(time.GetUtcNow());
            return Json(OneAlbumHit);
        }, options, time);

        var work = Task.WhenAll(
            harness.Provider.SearchAsync("A - one", CancellationToken.None),
            harness.Provider.SearchAsync("A - two", CancellationToken.None),
            harness.Provider.SearchAsync("A - three", CancellationToken.None));
        await RunUntilDoneAsync(work, time);

        Assert.Equal(3, stamps.Count);
        Assert.True(stamps[1] - stamps[0] >= TimeSpan.FromSeconds(1));
        Assert.True(stamps[2] - stamps[1] >= TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task First_request_is_not_delayed()
    {
        var time = new FakeTimeProvider();
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.FromSeconds(1) };
        await using var harness = new MusicBrainzTestHarness(_ => Json(OneAlbumHit), options, time);

        // The fake clock never advances, so this only completes if nothing waits.
        await harness.Provider.SearchAsync("A - one", CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_503_is_retried_after_the_delay_the_server_asks_for()
    {
        var time = new FakeTimeProvider();
        var attempts = 0;
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero };
        await using var harness = new MusicBrainzTestHarness(_ =>
        {
            attempts++;
            if (attempts > 1)
            {
                return Json(OneAlbumHit);
            }

            var busy = Json("{}", HttpStatusCode.ServiceUnavailable);
            busy.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
            return busy;
        }, options, time);

        var start = time.GetUtcNow();
        var work = harness.Provider.SearchAsync("A - B", CancellationToken.None);
        await RunUntilDoneAsync(work, time);

        Assert.Equal(2, attempts);
        Assert.True(time.GetUtcNow() - start >= TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task A_retry_after_of_zero_still_backs_off_by_the_configured_delay()
    {
        // The live service answers "busy" with "Retry-After: 0".
        var time = new FakeTimeProvider();
        var attempts = 0;
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero, RetryDelay = TimeSpan.FromSeconds(2) };
        await using var harness = new MusicBrainzTestHarness(_ =>
        {
            attempts++;
            if (attempts > 1)
            {
                return Json(OneAlbumHit);
            }

            var busy = Json("{}", HttpStatusCode.ServiceUnavailable);
            busy.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
            return busy;
        }, options, time);

        var start = time.GetUtcNow();
        await RunUntilDoneAsync(harness.Provider.SearchAsync("A - B", CancellationToken.None), time);

        Assert.True(time.GetUtcNow() - start >= TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Retries_stop_after_the_configured_number_and_the_error_is_reported()
    {
        var time = new FakeTimeProvider();
        var attempts = 0;
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero, MaxRetries = 2, RetryDelay = TimeSpan.FromSeconds(1) };
        await using var harness = new MusicBrainzTestHarness(_ =>
        {
            attempts++;
            return Json("{}", HttpStatusCode.ServiceUnavailable);
        }, options, time);

        var work = harness.Provider.SearchAsync("A - B", CancellationToken.None);
        await Assert.ThrowsAsync<HttpRequestException>(() => RunUntilDoneAsync(work, time));

        Assert.Equal(3, attempts); // first try plus two retries
    }

    [Fact]
    public async Task Rate_limit_answers_429_are_retried_too()
    {
        var time = new FakeTimeProvider();
        var attempts = 0;
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero, RetryDelay = TimeSpan.FromSeconds(1) };
        await using var harness = new MusicBrainzTestHarness(_ =>
        {
            attempts++;
            return attempts == 1 ? Json("{}", HttpStatusCode.TooManyRequests) : Json(OneAlbumHit);
        }, options, time);

        await RunUntilDoneAsync(harness.Provider.SearchAsync("A - B", CancellationToken.None), time);

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Client_errors_other_than_429_are_not_retried()
    {
        var attempts = 0;
        await using var harness = new MusicBrainzTestHarness(_ =>
        {
            attempts++;
            return Json("{}", HttpStatusCode.BadRequest);
        });

        await Assert.ThrowsAsync<HttpRequestException>(() => harness.Provider.SearchAsync("A - B", CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task An_attempt_that_never_answers_times_out_and_is_retried_then_reported_as_an_http_error()
    {
        var attempts = 0;
        var options = new MusicBrainzOptions
        {
            MinRequestInterval = TimeSpan.Zero,
            RetryDelay = TimeSpan.Zero,
            MaxRetries = 1,
            RequestTimeout = TimeSpan.FromSeconds(1),
        };
        await using var harness = new MusicBrainzTestHarness(async (_, cancellationToken) =>
        {
            attempts++;
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Json("{}");
        }, options);

        // The per-attempt timeout runs in real time, so this takes about two seconds.
        var failure = await Assert.ThrowsAsync<HttpRequestException>(() => harness.Provider.SearchAsync("A - B", CancellationToken.None));

        Assert.Equal(2, attempts);
        Assert.Contains("did not answer", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancelling_the_call_is_not_mistaken_for_a_timeout()
    {
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero, MaxRetries = 3 };
        await using var harness = new MusicBrainzTestHarness(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Json("{}");
        }, options);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.Provider.SearchAsync("A - B", cancellation.Token));

        Assert.Single(harness.Requests); // no retry after the caller gave up
    }

    [Fact]
    public async Task A_huge_retry_after_is_capped()
    {
        var time = new FakeTimeProvider();
        var attempts = 0;
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero, MaxRetryDelay = TimeSpan.FromSeconds(5) };
        await using var harness = new MusicBrainzTestHarness(_ =>
        {
            attempts++;
            if (attempts > 1)
            {
                return Json(OneAlbumHit);
            }

            var busy = Json("{}", HttpStatusCode.ServiceUnavailable);
            busy.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromHours(1));
            return busy;
        }, options, time);

        var start = time.GetUtcNow();
        await RunUntilDoneAsync(harness.Provider.SearchAsync("A - B", CancellationToken.None), time);

        Assert.InRange(time.GetUtcNow() - start, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task A_retry_budget_beyond_thirty_seconds_is_not_cut_off_by_the_client()
    {
        // Backoff of 20 s, then 40 s: well over the 30 s the whole pipeline used to be allowed.
        var time = new FakeTimeProvider();
        var attempts = 0;
        var options = new MusicBrainzOptions
        {
            MinRequestInterval = TimeSpan.Zero,
            RetryDelay = TimeSpan.FromSeconds(20),
            MaxRetryDelay = TimeSpan.FromMinutes(5),
        };
        await using var harness = new MusicBrainzTestHarness(_ =>
            ++attempts < 3 ? Json("{}", HttpStatusCode.ServiceUnavailable) : Json(OneAlbumHit), options, time);

        var start = time.GetUtcNow();
        await RunUntilDoneAsync(harness.Provider.SearchAsync("A - B", CancellationToken.None), time);

        Assert.Equal(3, attempts);
        Assert.True(time.GetUtcNow() - start >= TimeSpan.FromSeconds(60));

        var client = harness.Services.GetRequiredService<IHttpClientFactory>().CreateClient("IMetadataProvider");
        Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);
    }

    /// <summary>Advances the fake clock until the work finishes, so waits in the code under test do not block.</summary>
    private static async Task RunUntilDoneAsync(Task work, FakeTimeProvider time)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!work.IsCompleted)
        {
            timeout.Token.ThrowIfCancellationRequested();
            time.Advance(TimeSpan.FromMilliseconds(250));
            await Task.Delay(1, timeout.Token);
        }

        await work;
    }
}
