using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneyBoard.Domain.Repositories;
using MoneyBoard.Infrastructure.Data;

namespace MoneyBoard.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var envConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

            var connectionString = !string.IsNullOrEmpty(envConnectionString)
                ? envConnectionString
                : configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention());

            services.AddScoped<ILoanRepository, LoanRepository>();
            services.AddScoped<IRepaymentRepository, RepaymentRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

            return services;
        }
    }
}