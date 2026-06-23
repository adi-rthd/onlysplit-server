using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
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

    /// <summary>
    /// Configures the upload directory and static file serving for uploaded files.
    /// Creates required subdirectories (avatars, proofs) and verifies write access.
    /// Fails startup if the directory is not writable.
    /// </summary>
    public static WebApplication UseUploadStaticFiles(this WebApplication app)
    {
        var uploadPath = app.Configuration.GetValue<string>("FileStorage:UploadPath") ?? "/app/uploads";

        // Resolve to absolute path
        if (!Path.IsPathRooted(uploadPath))
        {
            uploadPath = Path.GetFullPath(uploadPath, Directory.GetCurrentDirectory());
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("OnlySplit.Uploads");

        // Ensure upload directories exist
        var avatarsDir = Path.Combine(uploadPath, "avatars");
        var proofsDir = Path.Combine(uploadPath, "proofs");

        try
        {
            Directory.CreateDirectory(avatarsDir);
            Directory.CreateDirectory(proofsDir);
            logger.LogInformation("Upload directories ensured at {UploadPath}", uploadPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create upload directories at '{uploadPath}': {ex.Message}", ex);
        }

        // Verify the upload directory is writable
        try
        {
            var testFile = Path.Combine(uploadPath, ".write-test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Upload directory '{uploadPath}' is not writable: {ex.Message}", ex);
        }

        // Configure static files for /uploads path (directory browsing is disabled by default)
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadPath),
            RequestPath = "/uploads"
        });

        logger.LogInformation("Static file middleware configured for /uploads serving from {UploadPath}", uploadPath);

        return app;
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

        // Static files for uploads — BEFORE UseAuthentication so uploaded files are publicly accessible
        app.UseUploadStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapControllers().RequireRateLimiting("api");
        app.MapHub<ActivityHub>("/hubs/activity").RequireCors("OnlySplitCors");
        app.MapHub<GroupHub>("/hubs/groups").RequireCors("OnlySplitCors");
        app.MapHub<PaymentHub>("/hubs/payments").RequireCors("OnlySplitCors");

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OnlySplit.API" }));

        return app;
    }
}
