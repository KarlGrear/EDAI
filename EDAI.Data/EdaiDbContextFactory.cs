using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EDAI.Data;

// Used exclusively by EF Core tooling (dotnet ef migrations).
// Not registered in the DI container.
public sealed class EdaiDbContextFactory : IDesignTimeDbContextFactory<EdaiDbContext>
{
    public EdaiDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "EDAI.db");
        var options = new DbContextOptionsBuilder<EdaiDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        return new EdaiDbContext(options);
    }
}
