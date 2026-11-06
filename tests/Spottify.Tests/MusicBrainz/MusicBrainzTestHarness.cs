using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.MusicBrainz;

namespace Spottify.Tests.MusicBrainz;

/// <summary>Runs the real MusicBrainz client against a scripted fake server, with controllable time.</summary>
internal sealed class MusicBrainzTestHarness : IAsyncDisposable
{
    private readonly ServiceProvider _services;

    public MusicBrainzTestHarness(Func<HttpRequestMessage, HttpResponseMessage> respond, MusicBrainzOptions? options = null, TimeProvider? time = null)
    {
        Requests = [];
        Time = time ?? TimeProvider.System;

        options ??= new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero, RetryDelay = TimeSpan.Zero };

        var services = new ServiceCollection();
        services.AddSingleton(Time);
        services.AddMusicBrainz(options)
            .ConfigurePrimaryHttpMessageHandler(() => new ScriptedHandler(request =>
            {
                Requests.Add(request.RequestUri!);
                UserAgents.Add(request.Headers.UserAgent.ToString());
                return respond(request);
            }));

        _services = services.BuildServiceProvider();
        Provider = _services.GetRequiredService<IMetadataProvider>();
    }

    public IMetadataProvider Provider { get; }

    public TimeProvider Time { get; }

    /// <summary>Every request that reached the (fake) network, in order.</summary>
    public List<Uri> Requests { get; }

    public List<string> UserAgents { get; } = [];

    public static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MusicBrainz", "Fixtures", name));

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public ValueTask DisposeAsync() => _services.DisposeAsync();

    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
