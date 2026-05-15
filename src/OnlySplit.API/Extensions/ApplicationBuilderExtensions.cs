using Microsoft.AspNetCore.HttpOverrides;
using OnlySplit.API.Hubs;
using OnlySplit.API.Middleware;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task BootstrapDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await initializer.InitializeAsync(app.Environment.IsDevelopment());
    }

    public static WebApplication UseOnlySplitPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseMiddleware<ExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "OnlySplit API v1");
                options.RoutePrefix = "swagger";
            });
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors("OnlySplitCors");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapControllers().RequireRateLimiting("api");
        app.MapHub<ActivityHub>("/hubs/activity");
        app.MapHub<GroupHub>("/hubs/groups");
        app.MapHub<PaymentHub>("/hubs/payments");

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OnlySplit.API" }));

        return app;
    }
}
