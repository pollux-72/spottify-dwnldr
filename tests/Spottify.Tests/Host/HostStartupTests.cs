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

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "spottify-host-" + Guid.NewGuid().ToString("N"));

    public HostStartupTests() => Environment.SetEnvironmentVariable(Variable, _directory);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Variable, null);
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
}
