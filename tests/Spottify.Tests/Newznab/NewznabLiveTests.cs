using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.Newznab;

namespace Spottify.Tests.Newznab;

/// <summary>
/// Manual check against the real indexer. It runs only with <c>SPOTTIFY_LIVE_TESTS=1</c> and the indexer settings in
/// the environment (<c>SPOTTIFY_Spottify__Indexer__BaseUrl</c> and <c>SPOTTIFY_Spottify__Indexer__ApiKey</c>), see README.
/// It spends one API request of the account and prints no secrets.
/// </summary>
public sealed class NewznabLiveTests
{
    [LiveFact]
    public async Task The_real_indexer_answers_an_album_search_with_usable_releases()
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables("SPOTTIFY_").Build();
        var options = configuration.GetSection(NewznabOptions.SectionName).Get<NewznabOptions>()
            ?? throw new InvalidOperationException("Set SPOTTIFY_Spottify__Indexer__BaseUrl and SPOTTIFY_Spottify__Indexer__ApiKey.");

        var services = new ServiceCollection();
        services.AddNewznabIndexer(options);
        await using var provider = services.BuildServiceProvider();
        var indexer = provider.GetRequiredService<IIndexer>();

        var releases = await indexer.SearchAsync("Daft Punk", "Discovery", CancellationToken.None);

        // A result list may be empty for an unusual indexer, but a working API call must not fail
        // and every release it returns must be complete.
        Assert.All(releases, release =>
        {
            Assert.False(string.IsNullOrWhiteSpace(release.Title));
            Assert.True(release.NzbUrl.IsAbsoluteUri);
        });
    }
}
