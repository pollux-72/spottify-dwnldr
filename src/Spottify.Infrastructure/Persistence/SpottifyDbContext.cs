using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spottify.Core.Domain;

namespace Spottify.Infrastructure.Persistence;

public sealed class SpottifyDbContext(DbContextOptions<SpottifyDbContext> options) : DbContext(options)
{
    public DbSet<DownloadRequest> DownloadRequests => Set<DownloadRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var request = modelBuilder.Entity<DownloadRequest>();

        request.ToTable("download_requests");
        request.HasKey(r => r.Id);

        request.Property(r => r.QueryText).IsRequired();

        // Stored as text so the database stays readable and reordering the enum cannot corrupt rows.
        request.Property(r => r.Status).HasConversion<string>().IsRequired();

        // SQLite cannot sort or compare DateTimeOffset natively, so store Unix milliseconds.
        var unixMillis = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        request.Property(r => r.CreatedAt).HasConversion(unixMillis);
        request.Property(r => r.UpdatedAt).HasConversion(unixMillis);

        request.HasIndex(r => r.Status);

        // Duplicate detection: one open request per album. The workflow (TASK-13) decides what "open" means,
        // so this index is deliberately not unique.
        request.HasIndex(r => r.MusicBrainzReleaseGroupId);
    }
}
