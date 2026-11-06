using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.Sabnzbd;

namespace Spottify.Tests.Sabnzbd;

/// <summary>
/// Manual check against your own SABnzbd. It runs only with <c>SPOTTIFY_LIVE_TESTS=1</c> and the SABnzbd settings in the
/// environment (<c>SPOTTIFY_Spottify__Sabnzbd__BaseUrl</c> and <c>SPOTTIFY_Spottify__Sabnzbd__ApiKey</c>), see README.
/// It starts no download: it only asks for a job that cannot exist, which proves address, key and API path.
/// </summary>
public sealed class SabnzbdLiveTests
{
    [LiveFact]
    public async Task The_real_sabnzbd_accepts_the_key_and_reports_an_unknown_job_as_failed()
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables("SPOTTIFY_").Build();
        var options = configuration.GetSection(SabnzbdOptions.SectionName).Get<SabnzbdOptions>()
            ?? throw new InvalidOperationException("Set SPOTTIFY_Spottify__Sabnzbd__BaseUrl and SPOTTIFY_Spottify__Sabnzbd__ApiKey.");

        var services = new ServiceCollection();
        services.AddSabnzbd(options);
        await using var provider = services.BuildServiceProvider();
        var downloader = provider.GetRequiredService<IDownloader>();

        var status = await downloader.GetStatusAsync("SABnzbd_nzo_doesnotexist", CancellationToken.None);

        Assert.Equal(DownloadJobState.Failed, status.State);
        Assert.Contains("does not know", status.Error, StringComparison.Ordinal);
    }
}
