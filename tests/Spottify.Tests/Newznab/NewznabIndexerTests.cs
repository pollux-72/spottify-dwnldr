using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.Newznab;
using Spottify.Tests.Support;

namespace Spottify.Tests.Newznab;

public sealed class NewznabIndexerTests
{
    private const string ApiKey = "SECRETKEY123";

    private static NewznabOptions Options(string baseUrl = "https://indexer.example/") =>
        new() { BaseUrl = baseUrl, ApiKey = ApiKey };

    private static HttpResponseMessage Xml(string text, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(text, Encoding.UTF8, "application/xml") };

    private static string Fixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Newznab", "Fixtures", "search-discovery.xml"));

    [Fact]
    public async Task A_search_returns_the_releases_of_the_feed()
    {
        await using var harness = new IndexerHarness(_ => Xml(Fixture()));

        var releases = await harness.Indexer.SearchAsync("Daft Punk", "Discovery", CancellationToken.None);

        Assert.Equal(4, releases.Count);
        Assert.Equal("Daft.Punk-Discovery-2001-FLAC", releases[0].Title);
    }

    [Fact]
    public async Task The_request_asks_for_audio_with_a_cleaned_up_query_and_the_api_key()
    {
        await using var harness = new IndexerHarness(_ => Xml(Fixture()));

        await harness.Indexer.SearchAsync("AC/DC", "Back in Black!", CancellationToken.None);

        var request = Assert.Single(harness.Requests);
        Assert.Equal("/api", request.AbsolutePath);
        Assert.Contains("t=search", request.Query, StringComparison.Ordinal);
        Assert.Contains("q=AC%20DC%20Back%20in%20Black", request.Query, StringComparison.Ordinal);
        Assert.Contains("cat=3000", request.Query, StringComparison.Ordinal);
        Assert.Contains("limit=50", request.Query, StringComparison.Ordinal);
        Assert.Contains("apikey=" + ApiKey, request.Query, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://indexer.example", "/api")]
    [InlineData("https://indexer.example/", "/api")]
    [InlineData("https://example.org/newznab", "/newznab/api")]
    [InlineData("https://example.org/newznab/", "/newznab/api")]
    public async Task The_api_is_found_below_the_base_url_with_or_without_slash(string baseUrl, string expectedPath)
    {
        await using var harness = new IndexerHarness(_ => Xml(Fixture()), Options(baseUrl));

        await harness.Indexer.SearchAsync("A", "B", CancellationToken.None);

        Assert.Equal(expectedPath, harness.Requests[0].AbsolutePath);
    }

    [Fact]
    public async Task Configured_categories_and_limit_are_used()
    {
        var options = Options();
        options.Categories = "3010,3040";
        options.Limit = 10;
        await using var harness = new IndexerHarness(_ => Xml(Fixture()), options);

        await harness.Indexer.SearchAsync("A", "B", CancellationToken.None);

        Assert.Contains("cat=3010%2C3040", harness.Requests[0].Query, StringComparison.Ordinal);
        Assert.Contains("limit=10", harness.Requests[0].Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_search_without_any_words_makes_no_request()
    {
        await using var harness = new IndexerHarness(_ => Xml(Fixture()));

        var releases = await harness.Indexer.SearchAsync("  ", "?!", CancellationToken.None);

        Assert.Empty(releases);
        Assert.Empty(harness.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(IndexerAuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(IndexerAuthenticationException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(IndexerLimitReachedException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(IndexerException))]
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(IndexerException))]
    public async Task Http_errors_become_the_matching_indexer_exception(HttpStatusCode status, Type expected)
    {
        await using var harness = new IndexerHarness(_ => Xml("<html>no</html>", status));

        var failure = await Assert.ThrowsAnyAsync<IndexerException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));

        Assert.Equal(expected, failure.GetType());
        AssertNoKey(failure);
    }

    [Fact]
    public async Task An_error_document_with_a_failing_status_still_reports_the_indexer_error_code()
    {
        await using var harness = new IndexerHarness(_ =>
            Xml("""<error code="500" description="Request limit reached"/>""", HttpStatusCode.BadRequest));

        var failure = await Assert.ThrowsAsync<IndexerLimitReachedException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));

        Assert.Contains("Request limit reached", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_error_document_with_status_200_is_an_error_too()
    {
        await using var harness = new IndexerHarness(_ => Xml("""<error code="100" description="Incorrect user credentials"/>"""));

        await Assert.ThrowsAsync<IndexerAuthenticationException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));
    }

    [Fact]
    public async Task A_network_failure_is_an_indexer_exception_without_the_key()
    {
        await using var harness = new IndexerHarness((_, _) => throw new HttpRequestException("Connection refused"));

        var failure = await Assert.ThrowsAsync<IndexerException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));

        Assert.IsType<HttpRequestException>(failure.InnerException);
        AssertNoKey(failure);
    }

    [Fact]
    public async Task A_connection_that_breaks_while_the_body_is_read_is_an_indexer_exception()
    {
        await using var harness = new IndexerHarness(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new BrokenConnectionContent() });

        var failure = await Assert.ThrowsAsync<IndexerException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));

        Assert.IsAssignableFrom<IOException>(failure.InnerException);
        AssertNoKey(failure);
    }

    [Fact]
    public async Task A_slow_indexer_is_an_indexer_exception()
    {
        var options = Options();
        options.RequestTimeout = TimeSpan.FromSeconds(1);
        await using var harness = new IndexerHarness(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Xml(Fixture());
        }, options);

        var failure = await Assert.ThrowsAsync<IndexerException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));

        Assert.Contains("did not answer in time", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancelling_the_call_is_not_reported_as_an_indexer_failure()
    {
        await using var harness = new IndexerHarness(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Xml(Fixture());
        });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Indexer.SearchAsync("A", "B", cancellation.Token));
    }

    [Fact]
    public async Task The_api_key_never_reaches_the_log_not_even_at_trace_level()
    {
        var log = new CapturingLoggerProvider();
        await using var harness = new IndexerHarness(_ => Xml(Fixture()), loggers: log);

        await harness.Indexer.SearchAsync("Daft Punk", "Discovery", CancellationToken.None);

        // Make sure the capture works at all, otherwise "no key in the log" proves nothing.
        harness.Services.GetRequiredService<ILoggerFactory>().CreateLogger("probe").LogInformation("probe");
        Assert.Contains(log.Messages, m => m == "probe");

        Assert.DoesNotContain(log.Messages, m => m.Contains(ApiKey, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(IndexerAuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(IndexerAuthenticationException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(IndexerLimitReachedException))]
    public async Task A_garbled_body_on_a_failing_status_still_gives_the_exception_for_that_status(HttpStatusCode status, Type expected)
    {
        await using var harness = new IndexerHarness(_ => Xml("<html><body>Forbidden <br></body>", status));

        var failure = await Assert.ThrowsAnyAsync<IndexerException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));

        Assert.Equal(expected, failure.GetType());
    }

    [Fact]
    public async Task An_answer_that_announces_more_than_the_limit_is_refused_without_reading_it()
    {
        var options = Options();
        options.MaxResponseBytes = 1024;
        await using var harness = new IndexerHarness(_ => Xml(new string('x', 5_000)), options);

        var failure = await Assert.ThrowsAsync<IndexerException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));

        Assert.Contains("larger than the allowed 1024 bytes", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_answer_of_unknown_length_is_cut_off_at_the_limit()
    {
        var options = Options();
        options.MaxResponseBytes = 1024;
        var content = new GeneratedContent(bytes: 10_000_000);
        await using var harness = new IndexerHarness(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content }, options);

        var failure = await Assert.ThrowsAsync<IndexerException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None));

        Assert.Contains("larger than the allowed", failure.Message, StringComparison.Ordinal);
        Assert.True(content.BytesWritten < 1_000_000, "the client must stop reading early, not consume everything");
    }

    [Fact]
    public async Task A_body_that_stalls_after_the_headers_times_out()
    {
        var options = Options();
        options.RequestTimeout = TimeSpan.FromSeconds(1);
        await using var harness = new IndexerHarness(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new GeneratedContent(bytes: -1) }, options);

        var failure = await Assert.ThrowsAsync<IndexerException>(
            () => harness.Indexer.SearchAsync("A", "B", CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15)));

        Assert.Contains("did not answer in time", failure.Message, StringComparison.Ordinal);
    }

    private static void AssertNoKey(Exception exception) =>
        Assert.DoesNotContain(ApiKey, exception.Message, StringComparison.Ordinal);

    [Theory]
    [InlineData("Daft Punk", "Discovery", "Daft Punk Discovery")]
    [InlineData("AC/DC", "Back in Black", "AC DC Back in Black")]
    [InlineData("Beyoncé", "I Am... Sasha Fierce", "Beyoncé I Am Sasha Fierce")]
    [InlineData("  Sigur   Rós ", "( )", "Sigur Rós")]
    [InlineData("!!!", "???", "")]
    public void Search_text_keeps_words_and_drops_punctuation(string artist, string album, string expected) =>
        Assert.Equal(expected, SearchText.Build(artist, album));

    [Fact]
    public void Search_text_is_limited_in_length() =>
        Assert.True(SearchText.Build(new string('a', 500), "b").Length <= 200);

    private sealed class IndexerHarness : IAsyncDisposable
    {
        public IndexerHarness(Func<HttpRequestMessage, HttpResponseMessage> respond, NewznabOptions? options = null, ILoggerProvider? loggers = null)
            : this((request, _) => Task.FromResult(respond(request)), options, loggers)
        {
        }

        public IndexerHarness(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond, NewznabOptions? options = null, ILoggerProvider? loggers = null)
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                if (loggers is not null)
                {
                    builder.AddProvider(loggers);
                }
            });

            services.AddNewznabIndexer(options ?? Options())
                .ConfigurePrimaryHttpMessageHandler(() => new Scripted((request, cancellationToken) =>
                {
                    Requests.Add(request.RequestUri!);
                    return respond(request, cancellationToken);
                }));

            Services = services.BuildServiceProvider();
            Indexer = Services.GetRequiredService<IIndexer>();
        }

        public ServiceProvider Services { get; }

        public IIndexer Indexer { get; }

        public List<Uri> Requests { get; } = [];

        public ValueTask DisposeAsync() => Services.DisposeAsync();

        private sealed class Scripted(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                respond(request, cancellationToken);
        }
    }
}
