using Microsoft.Extensions.DependencyInjection;

namespace MoneyBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services;
}