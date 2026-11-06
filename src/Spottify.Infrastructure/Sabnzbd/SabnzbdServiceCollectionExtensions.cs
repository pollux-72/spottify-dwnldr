using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spottify.Core.Abstractions;

namespace Spottify.Infrastructure.Sabnzbd;

public static class SabnzbdServiceCollectionExtensions
{
    /// <summary>
    /// Registers SABnzbd as the <see cref="IDownloader"/>. The returned builder lets callers adjust the HTTP
    /// pipeline, which the tests use to replace the network.
    /// </summary>
    public static IHttpClientBuilder AddSabnzbd(this IServiceCollection services, SabnzbdOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(Options.Create(options));

        return services
            .AddHttpClient<IDownloader, SabnzbdDownloader>(client =>
            {
                client.BaseAddress = options.BaseAddress;

                // The downloader client enforces the timeout itself, for headers and body together.
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SpottifyDwnldr/0.1");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })

            // The request URL contains the API key, and the default HTTP logging writes every URL to the log.
            .RemoveAllLoggers();
    }
}
