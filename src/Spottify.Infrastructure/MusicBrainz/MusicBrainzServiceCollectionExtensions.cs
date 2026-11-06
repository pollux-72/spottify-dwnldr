using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Spottify.Core.Abstractions;

namespace Spottify.Infrastructure.MusicBrainz;

public static class MusicBrainzServiceCollectionExtensions
{
    /// <summary>
    /// Registers MusicBrainz as the <see cref="IMetadataProvider"/>. The returned builder lets callers
    /// adjust the HTTP pipeline, which the tests use to replace the network.
    /// </summary>
    public static IHttpClientBuilder AddMusicBrainz(this IServiceCollection services, MusicBrainzOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<MusicBrainzThrottle>();
        services.AddTransient<MusicBrainzRequestHandler>();

        return services
            .AddHttpClient<IMetadataProvider, MusicBrainzClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<MusicBrainzRequestHandler>();
    }
}
