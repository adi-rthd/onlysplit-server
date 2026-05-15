using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OnlySplit.Infrastructure.Database;

public sealed class OnlySplitDbContextFactory : IDesignTimeDbContextFactory<OnlySplitDbContext>
{
    public OnlySplitDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configurationBasePath = ResolveConfigurationBasePath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configurationBasePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            "Host=localhost;Port=5432;Database=onlysplit;Username=onlysplit;Password=onlysplit";

        var optionsBuilder = new DbContextOptionsBuilder<OnlySplitDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new OnlySplitDbContext(optionsBuilder.Options);
    }

    private static string ResolveConfigurationBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            currentDirectory,
            Path.Combine(currentDirectory, "src", "OnlySplit.API"),
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "OnlySplit.API")),
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "..", "OnlySplit.API"))
        };

        return candidates.FirstOrDefault(candidate => File.Exists(Path.Combine(candidate, "appsettings.json")))
            ?? currentDirectory;
    }
}
