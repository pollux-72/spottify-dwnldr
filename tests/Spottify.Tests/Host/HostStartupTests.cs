using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Spottify.Tests.Host;

/// <summary>
/// Starts the real host. Configuration is passed through the SPOTTIFY_ environment variable, the same way
/// the Docker container receives it, so this also covers the environment variable mapping.
/// </summary>
[Collection("Environment variables")]
public sealed class HostStartupTests : IDisposable
{
    private const string Variable = "SPOTTIFY_Spottify__DataDirectory";
    private const string FileNameVariable = "SPOTTIFY_Spottify__DatabaseFileName";
    private const string MusicBrainzUrlVariable = "SPOTTIFY_Spottify__MusicBrainz__BaseUrl";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "spottify-host-" + Guid.NewGuid().ToString("N"));

    public HostStartupTests() => Environment.SetEnvironmentVariable(Variable, _directory);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Variable, null);
        Environment.SetEnvironmentVariable(FileNameVariable, null);
        Environment.SetEnvironmentVariable(MusicBrainzUrlVariable, null);
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task Host_creates_the_database_on_startup_and_reports_healthy()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(File.Exists(Path.Combine(_directory, "spottify.db")));
    }

    [Fact]
    public async Task Invalid_settings_stop_the_host_before_anything_is_created_on_disk()
    {
        Environment.SetEnvironmentVariable(FileNameVariable, "../escaped.db");

        await using var factory = new WebApplicationFactory<Program>();

        var failure = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(failure);
        Assert.Contains("DatabaseFileName", Flatten(failure), StringComparison.Ordinal);
        Assert.False(Directory.Exists(_directory), "the data directory must not be created for invalid settings");
    }

    [Fact]
    public async Task The_host_provides_the_musicbrainz_metadata_provider()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        Assert.NotNull(factory.Services.GetService(typeof(Spottify.Core.Abstractions.IMetadataProvider)));
    }

    [Fact]
    public async Task An_invalid_musicbrainz_url_stops_the_host_at_startup()
    {
        Environment.SetEnvironmentVariable(MusicBrainzUrlVariable, "not a url");

        await using var factory = new WebApplicationFactory<Program>();

        var failure = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(failure);
        Assert.Contains("BaseUrl", Flatten(failure), StringComparison.Ordinal);
    }

    private static string Flatten(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            messages.Add(current.Message);
            if (current.InnerException is null)
            {
                break;
            }
        }

        return string.Join(" | ", messages);
    }
}
