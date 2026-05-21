using OnlySplit.Application;
using OnlySplit.API.Extensions;
using OnlySplit.Infrastructure;
using OnlySplit.Shared;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddShared()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"]
        ?? throw new InvalidOperationException(
            "Redis connection string is missing."
        );

    return ConnectionMultiplexer.Connect(connectionString);
});

var app = builder.Build();

await app.BootstrapDatabaseAsync();

app.UseOnlySplitPipeline();

await app.RunAsync();