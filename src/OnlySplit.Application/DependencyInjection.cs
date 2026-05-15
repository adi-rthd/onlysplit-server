using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OnlySplit.Application.Features.Auth;

namespace OnlySplit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<SignupRequestValidator>();
        return services;
    }
}
