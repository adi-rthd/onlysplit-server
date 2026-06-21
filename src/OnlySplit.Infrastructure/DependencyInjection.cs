using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OnlySplit.Application.Activities.Interfaces;
using OnlySplit.Application.Activities.Services;
using OnlySplit.Application.Analytics.Interfaces;
using OnlySplit.Application.Dashboard.Interfaces;
using OnlySplit.Application.Dashboard.Services;
using OnlySplit.Application.Features.Redis;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Auth;
using OnlySplit.Infrastructure.Authentication;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Infrastructure.Payments;
using OnlySplit.Infrastructure.Persistence;
using OnlySplit.Infrastructure.Repositories;
using OnlySplit.Infrastructure.Services;
namespace OnlySplit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddDatabase(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddInfrastructureServices(configuration);

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        services.AddDbContext<OnlySplitDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
            }));

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = BindJwtOptions(configuration);
        services.Configure<JwtOptions>(options =>
        {
            options.Secret = jwtOptions.Secret;
            options.Issuer = jwtOptions.Issuer;
            options.Audience = jwtOptions.Audience;
            options.AccessTokenMinutes = jwtOptions.AccessTokenMinutes;
            options.RefreshTokenDays = jwtOptions.RefreshTokenDays;
        });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var razorpayOptions = BindRazorpayOptions(configuration);
        services.Configure<RazorpayOptions>(options =>
        {
            options.KeyId = razorpayOptions.KeyId;
            options.KeySecret = razorpayOptions.KeySecret;
            options.WebhookSecret = razorpayOptions.WebhookSecret;
            options.Currency = razorpayOptions.Currency;
        });

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IGroupMembershipReader, GroupMembershipReader>();
        services.AddScoped<IRazorpayService, RazorpayService>();
        services.AddScoped<IPaymentVerificationService, PaymentVerificationService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IBasicPageService, BasicPageService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IGroupInvitation, GroupInvitationService>();
        services.AddScoped<IGroupInvitation, GroupInvitationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFriendshipService, FriendshipService>();
        services.AddScoped<IActivityFeedService, ActivityFeedService>();
        services.AddScoped<ISessionService, RedisSessionService>();
        services.AddScoped<IAnalyticsService,AnalyticsService>();
        services.AddScoped<IFileUploadService, FileUploadService>();
        return services;
    }

    private static JwtOptions BindJwtOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(options.Secret) || options.Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must contain at least 32 characters. In production, set Jwt__Secret as an environment variable.");
        }

        return options;
    }

    private static RazorpayOptions BindRazorpayOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection(RazorpayOptions.SectionName).Get<RazorpayOptions>() ?? new RazorpayOptions();
        options.Currency = string.IsNullOrWhiteSpace(options.Currency) ? "INR" : options.Currency;
        return options;
    }
}
