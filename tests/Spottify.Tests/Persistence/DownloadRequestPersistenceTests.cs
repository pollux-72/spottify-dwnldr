using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spottify.Core.Domain;
using Spottify.Infrastructure.Persistence;

namespace Spottify.Tests.Persistence;

public sealed class DownloadRequestPersistenceTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "spottify-tests-" + Guid.NewGuid().ToString("N"));
    private ServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _services = new ServiceCollection()
            .AddSpottifyPersistence(Path.Combine(_directory, "test.db"))
            .BuildServiceProvider();

        await _services.MigrateSpottifyDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();

        // SQLite pools connections and keeps the file locked on Windows until the pool is cleared.
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Migrations_create_the_download_requests_table()
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SpottifyDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Empty(await db.DownloadRequests.ToListAsync());
    }

    [Fact]
    public async Task Database_path_with_a_semicolon_is_not_parsed_as_connection_string_syntax()
    {
        var folder = Path.Combine(_directory, "semi;colon");
        Directory.CreateDirectory(folder);
        var databasePath = Path.Combine(folder, "state.db");

        await using var services = new ServiceCollection().AddSpottifyPersistence(databasePath).BuildServiceProvider();
        await services.MigrateSpottifyDatabaseAsync();

        Assert.True(File.Exists(databasePath));
    }

    [Fact]
    public async Task Request_survives_a_roundtrip_with_all_fields()
    {
        var id = Guid.NewGuid();
        var created = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SpottifyDbContext>();
            db.DownloadRequests.Add(new DownloadRequest
            {
                Id = id,
                QueryText = "Daft Punk - One More Time",
                Status = RequestStatus.Downloading,
                Artist = "Daft Punk",
                AlbumTitle = "Discovery",
                MusicBrainzReleaseGroupId = "rg-1",
                MusicBrainzReleaseId = "r-1",
                SelectedReleaseTitle = "Daft.Punk-Discovery-2001-FLAC",
                SelectedNzbUrl = "https://indexer.example/getnzb/abc",
                DownloaderJobId = "SABnzbd_nzo_x",
                ErrorMessage = null,
                CreatedAt = created,
                UpdatedAt = created.AddMinutes(5),
            });
            await db.SaveChangesAsync();
        }

        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SpottifyDbContext>();
            var loaded = await db.DownloadRequests.SingleAsync(r => r.Id == id);

            Assert.Equal("Daft Punk - One More Time", loaded.QueryText);
            Assert.Equal(RequestStatus.Downloading, loaded.Status);
            Assert.Equal("rg-1", loaded.MusicBrainzReleaseGroupId);
            Assert.Equal("SABnzbd_nzo_x", loaded.DownloaderJobId);
            Assert.Equal(created, loaded.CreatedAt);
            Assert.Equal(created.AddMinutes(5), loaded.UpdatedAt);
        }
    }

    [Fact]
    public async Task Status_is_stored_as_readable_text()
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SpottifyDbContext>();
        db.DownloadRequests.Add(new DownloadRequest { Id = Guid.NewGuid(), QueryText = "x", Status = RequestStatus.NotFound });
        await db.SaveChangesAsync();

        var stored = await db.Database
            .SqlQueryRaw<string>("SELECT \"Status\" AS \"Value\" FROM download_requests")
            .SingleAsync();

        Assert.Equal("NotFound", stored);
    }

    [Fact]
    public async Task Requests_can_be_ordered_and_filtered_by_time_and_status()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SpottifyDbContext>();
        db.DownloadRequests.AddRange(
            new DownloadRequest { Id = Guid.NewGuid(), QueryText = "late", CreatedAt = start.AddDays(2), Status = RequestStatus.Failed },
            new DownloadRequest { Id = Guid.NewGuid(), QueryText = "early", CreatedAt = start, Status = RequestStatus.Failed },
            new DownloadRequest { Id = Guid.NewGuid(), QueryText = "other", CreatedAt = start.AddDays(1), Status = RequestStatus.Imported });
        await db.SaveChangesAsync();

        var failed = await db.DownloadRequests
            .Where(r => r.Status == RequestStatus.Failed)
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.QueryText)
            .ToListAsync();

        Assert.Equal(["early", "late"], failed);
    }
}
