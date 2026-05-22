using OnlySplit.Application;
using OnlySplit.API.Extensions;
using OnlySplit.Infrastructure;
using OnlySplit.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddShared()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

await app.BootstrapDatabaseAsync();
app.UseOnlySplitPipeline();

await app.RunAsync();
