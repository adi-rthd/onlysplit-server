using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using OnlySplit.API.Hubs;
using OnlySplit.Application.Interfaces;
using OnlySplit.Shared.Responses;

namespace OnlySplit.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddApiValidation();
        services.AddSwaggerDocumentation();
        services.AddRealtime();
        services.AddCorsPolicy(configuration);
        services.AddRateLimiting();
        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

        return services;
    }

    private static IServiceCollection AddApiValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .SelectMany(entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Invalid request value."
                        : error.ErrorMessage))
                    .ToArray();

                return new BadRequestObjectResult(ApiResponse<object>.Fail("Validation failed.", errors));
            };
        });

        return services;
    }

    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OnlySplit API",
                Version = "v1",
                Description = "Realtime expense splitting, settlement, and Razorpay payments backend."
            });

            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token as: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", null, null)] = []
            });
        });

        return services;
    }

    private static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = false;
            options.MaximumReceiveMessageSize = 64 * 1024;
            options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(45);
        });

        return services;
    }

    private static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("OnlySplitCors", builder =>
            {
                if (allowedOrigins.Length == 0)
                {
                    builder.AllowAnyOrigin();
                }
                else
                {
                    builder.WithOrigins(allowedOrigins).AllowCredentials();
                }

                builder.AllowAnyHeader().AllowAnyMethod();
            });
        });

        return services;
    }

    private static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("api", httpContext =>
            {
                var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                    ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous"
                    : httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}
