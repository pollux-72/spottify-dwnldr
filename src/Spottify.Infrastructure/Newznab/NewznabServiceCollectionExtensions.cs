using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spottify.Core.Abstractions;

namespace Spottify.Infrastructure.Newznab;

public static class NewznabServiceCollectionExtensions
{
    /// <summary>
    /// Registers a Newznab indexer as the <see cref="IIndexer"/>. The returned builder lets callers adjust the HTTP
    /// pipeline, which the tests use to replace the network.
    /// </summary>
    public static IHttpClientBuilder AddNewznabIndexer(this IServiceCollection services, NewznabOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(Options.Create(options));

        return services
            .AddHttpClient<IIndexer, NewznabIndexer>(client =>
            {
                client.BaseAddress = options.BaseAddress;
                client.Timeout = options.RequestTimeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SpottifyDwnldr/0.1");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/xml, text/xml, application/rss+xml");
            })

            // The request URL contains the API key, and the default HTTP logging writes every URL to the log.
            .RemoveAllLoggers();
    }
}
