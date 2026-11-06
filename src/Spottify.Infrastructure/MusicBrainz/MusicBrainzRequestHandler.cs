using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Spottify.Infrastructure.MusicBrainz;

/// <summary>
/// Spaces requests at least <see cref="MusicBrainzOptions.MinRequestInterval"/> apart. MusicBrainz allows about
/// one request per second and blocks clients that exceed it. Registered as a singleton because
/// <c>HttpClientFactory</c> recreates message handlers every couple of minutes, which would reset the timing.
/// </summary>
internal sealed class MusicBrainzThrottle(IOptions<MusicBrainzOptions> options, TimeProvider timeProvider) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long? _lastRequestTimestamp;

    public async Task WaitForTurnAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_lastRequestTimestamp is { } last)
            {
                var wait = options.Value.MinRequestInterval - timeProvider.GetElapsedTime(last);
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, timeProvider, cancellationToken).ConfigureAwait(false);
                }
            }

            _lastRequestTimestamp = timeProvider.GetTimestamp();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}

/// <summary>
/// Makes every request to MusicBrainz polite: identifies the client, waits for its turn at the
/// <see cref="MusicBrainzThrottle"/> and retries when the server asks us to slow down or is briefly unavailable.
/// </summary>
internal sealed class MusicBrainzRequestHandler(
    IOptions<MusicBrainzOptions> options,
    MusicBrainzThrottle throttle,
    TimeProvider timeProvider) : DelegatingHandler
{
    private readonly MusicBrainzOptions _options = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            await throttle.WaitForTurnAsync(cancellationToken).ConfigureAwait(false);

            // A request message can be sent only once, so every attempt gets a copy.
            using var attemptRequest = CopyOf(request);
            var response = await base.SendAsync(attemptRequest, cancellationToken).ConfigureAwait(false);

            if (!ShouldRetry(response.StatusCode) || attempt >= _options.MaxRetries)
            {
                return response;
            }

            var delay = RetryDelayFor(response, attempt);
            response.Dispose();
            await Task.Delay(delay, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool ShouldRetry(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout;

    private TimeSpan RetryDelayFor(HttpResponseMessage response, int attempt)
    {
        // Our own backoff is the floor: the live service answers "busy" with "Retry-After: 0",
        // and retrying instantly would only hammer a server that just asked for relief.
        var backoff = _options.RetryDelay * (attempt + 1);

        var retryAfter = response.Headers.RetryAfter;
        var requested = retryAfter?.Delta
            ?? (retryAfter?.Date is { } date ? date - timeProvider.GetUtcNow() : TimeSpan.Zero);

        return requested > backoff ? requested : backoff;
    }

    private HttpRequestMessage CopyOf(HttpRequestMessage original)
    {
        var copy = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
        {
            copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        copy.Headers.UserAgent.Clear();
        copy.Headers.UserAgent.ParseAdd(_options.UserAgent);
        copy.Headers.Accept.Clear();
        copy.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return copy;
    }
}
