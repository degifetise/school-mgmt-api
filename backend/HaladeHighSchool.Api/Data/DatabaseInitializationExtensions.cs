using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Data;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");

        await db.Database.MigrateAsync(cancellationToken);
        await SchoolMasterDataSeeder.SeedAsync(db, logger, cancellationToken);
        await DbSeeder.SeedAsync(services, cancellationToken);
    }
}
