using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.Sabnzbd;

namespace Spottify.Tests.Sabnzbd;

/// <summary>
/// Runs the real SABnzbd client against a scripted fake server. The JSON answers used with it were written from the
/// SABnzbd API documentation, not recorded from a running SABnzbd.
/// </summary>
internal sealed class SabnzbdHarness : IAsyncDisposable
{
    public const string ApiKey = "SABKEY123";

    public SabnzbdHarness(Func<HttpRequestMessage, HttpResponseMessage> respond, SabnzbdOptions? options = null, ILoggerProvider? loggers = null)
        : this((request, _) => Task.FromResult(respond(request)), options, loggers)
    {
    }

    public SabnzbdHarness(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond, SabnzbdOptions? options = null, ILoggerProvider? loggers = null)
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

        services.AddSabnzbd(options ?? Options())
            .ConfigurePrimaryHttpMessageHandler(() => new Scripted((request, cancellationToken) =>
            {
                Requests.Add(request.RequestUri!);
                return respond(request, cancellationToken);
            }));

        Services = services.BuildServiceProvider();
        Downloader = Services.GetRequiredService<IDownloader>();
    }

    public ServiceProvider Services { get; }

    public IDownloader Downloader { get; }

    public List<Uri> Requests { get; } = [];

    public static SabnzbdOptions Options(string baseUrl = "http://sabnzbd:8080/") =>
        new() { BaseUrl = baseUrl, ApiKey = ApiKey };

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>Answers each API call by its <c>mode</c> with the matching JSON.</summary>
    public static Func<HttpRequestMessage, HttpResponseMessage> ByMode(Func<string, string> jsonFor) =>
        request => Json(jsonFor(Mode(request.RequestUri!)));

    public static string Mode(Uri uri) =>
        System.Web.HttpUtility.ParseQueryString(uri.Query)["mode"] ?? string.Empty;

    public static string Query(Uri uri, string name) =>
        System.Web.HttpUtility.ParseQueryString(uri.Query)[name] ?? string.Empty;

    public static string Queue(string id, string status) =>
        $$$"""{"queue":{"status":"Downloading","slots":[{"nzo_id":"other_job","status":"Downloading"},{"nzo_id":"{{{id}}}","status":"{{{status}}}","filename":"Daft.Punk","percentage":"42","cat":"music"}]}}""";

    public const string EmptyQueue = """{"queue":{"status":"Idle","slots":[]}}""";

    public const string EmptyHistory = """{"history":{"slots":[]}}""";

    public static string History(string id, string status, string storage = "", string failMessage = "") =>
        $$$"""{"history":{"slots":[{"nzo_id":"other_job","status":"Completed","storage":"/x"},{"nzo_id":"{{{id}}}","name":"Daft.Punk","status":"{{{status}}}","storage":"{{{storage}}}","fail_message":"{{{failMessage}}}","category":"music"}]}}""";

    public ValueTask DisposeAsync() => Services.DisposeAsync();

    private sealed class Scripted(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}
