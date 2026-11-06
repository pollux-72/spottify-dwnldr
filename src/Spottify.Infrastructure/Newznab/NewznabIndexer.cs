using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.Http;

namespace Spottify.Infrastructure.Newznab;

/// <summary><see cref="IIndexer"/> for any Newznab compatible Usenet indexer.</summary>
/// <remarks>
/// The API key travels as a query parameter, which is how Newznab works. Therefore this client's HTTP logging is
/// switched off at registration, and no message built here contains the request URL.
/// </remarks>
internal sealed class NewznabIndexer(HttpClient http, IOptions<NewznabOptions> options) : IIndexer
{
    public async Task<IReadOnlyList<IndexerRelease>> SearchAsync(string artist, string album, CancellationToken cancellationToken)
    {
        var text = SearchText.Build(artist, album);
        if (text.Length == 0)
        {
            return [];
        }

        var settings = options.Value;
        var url = "api?t=search"
            + $"&q={Uri.EscapeDataString(text)}"
            + $"&cat={Uri.EscapeDataString(settings.Categories)}"
            + $"&limit={settings.Limit}"
            + $"&apikey={Uri.EscapeDataString(settings.ApiKey)}";

        // One timeout for the whole exchange, headers and body. The HttpClient has none of its own because with
        // streaming reads its timeout would end as soon as the headers arrive.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.RequestTimeout);

        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            await using var body = await LimitedBody.ReadAsync(
                response, settings.MaxResponseBytes, () => TooLarge(settings.MaxResponseBytes), timeout.Token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return NewznabResponseParser.Parse(body);
            }

            throw NewznabResponseParser.TryParseError(body) ?? response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    new IndexerAuthenticationException($"The indexer refused the API key (HTTP {(int)response.StatusCode})."),
                HttpStatusCode.TooManyRequests =>
                    new IndexerLimitReachedException("The indexer's request limit is reached (HTTP 429)."),
                _ => new IndexerException($"The indexer answered HTTP {(int)response.StatusCode}."),
            };
        }
        catch (HttpRequestException exception)
        {
            // The exception text of the HTTP stack can name the host, never the query, but keep it out of the
            // message anyway and let callers read the inner exception if they need it.
            throw new IndexerException("The indexer could not be reached.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IndexerException("The indexer did not answer in time.", exception);
        }
    }

    private static IndexerException TooLarge(long maxBytes) =>
        new(string.Create(CultureInfo.InvariantCulture, $"The indexer answer is larger than the allowed {maxBytes} bytes."));
}

/// <summary>Builds the search text. Indexers match words, so punctuation only gets in the way.</summary>
internal static class SearchText
{
    private const int MaxLength = 200;

    public static string Build(string? artist, string? album)
    {
        var text = new StringBuilder();
        var pendingSpace = false;

        foreach (var c in $"{artist} {album}")
        {
            if (char.IsLetterOrDigit(c))
            {
                if (pendingSpace && text.Length > 0)
                {
                    text.Append(' ');
                }

                pendingSpace = false;
                text.Append(c);
            }
            else
            {
                pendingSpace = true;
            }

            if (text.Length >= MaxLength)
            {
                break;
            }
        }

        return text.ToString();
    }
}
