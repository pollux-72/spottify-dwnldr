using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Spottify.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> create migrations without starting the host.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SpottifyDbContext>
{
    public SpottifyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SpottifyDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new SpottifyDbContext(options);
    }
}
