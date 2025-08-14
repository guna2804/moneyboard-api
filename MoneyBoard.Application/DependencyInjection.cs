using Microsoft.Extensions.DependencyInjection;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Application.Services;

namespace MoneyBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBCryptService, BCryptAdapterService>();
        services.AddScoped<ITokenService, TokenService>();
        return services;
    }
}