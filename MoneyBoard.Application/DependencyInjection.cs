using Microsoft.Extensions.DependencyInjection;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Application.Services;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBCryptService, BCryptAdapterService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ILoanService, LoanService>();
        return services;
    }
}
