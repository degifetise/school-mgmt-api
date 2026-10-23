using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

        try
        {
            await db.Database.MigrateAsync(cancellationToken);
            await SchoolMasterDataSeeder.SeedAsync(db, logger, cancellationToken);
            await DbSeeder.SeedAsync(services, cancellationToken);
        }
        catch (Exception exception) when (IsDatabaseUnavailable(exception))
        {
            logger.LogWarning(
                exception,
                "Skipping database migration/seeding due to offline environment. The API will continue starting without database initialization.");
        }
    }

    private static bool IsDatabaseUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or SocketException
                || current is NpgsqlException { IsTransient: true })
            {
                return true;
            }
        }

        return false;
    }
}
