using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Spottify.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Registers the SQLite-backed <see cref="SpottifyDbContext"/> for the given database file.</summary>
    public static IServiceCollection AddSpottifyPersistence(this IServiceCollection services, string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        return services.AddDbContext<SpottifyDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));
    }

    /// <summary>Applies pending migrations. Called on startup so a fresh volume gets its schema automatically.</summary>
    public static async Task MigrateSpottifyDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SpottifyDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
