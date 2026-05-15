using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;

namespace OnlySplit.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    OnlySplitDbContext context,
    IServiceProvider services,
    IConfiguration configuration) : IDatabaseInitializer
{
    public async Task InitializeAsync(bool isDevelopment, CancellationToken cancellationToken = default)
    {
        if (configuration.GetValue("Database:RunMigrationsOnStartup", false))
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        if (isDevelopment)
        {
            await DatabaseSeeder.SeedDevelopmentDataAsync(services, cancellationToken);
        }
    }
}
