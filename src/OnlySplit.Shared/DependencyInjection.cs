using Microsoft.Extensions.DependencyInjection;

namespace OnlySplit.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddShared(this IServiceCollection services) => services;
}
