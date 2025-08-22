using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Application.Mappings;
using MoneyBoard.Application.Validators;
using System.Reflection;
using System.Text;

namespace MoneyBoard.WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddFluentValidationAutoValidation();
            services.AddFluentValidationClientsideAdapters();

            // Validators
            services.AddScoped<IValidator<RegisterDto>, AuthValidator.RegisterValidator>();
            services.AddScoped<IValidator<LoginDto>, AuthValidator.LoginValidator>();

            // Auth Service
            services.AddScoped<IAuthService, AuthService>();

            // JWT Authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config["Jwt:Issuer"],
                        ValidAudience = config["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
                    };
                });

            // Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            var mappingConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(Assembly.GetAssembly(typeof(BaseMappingProfile)));
            });

            services.AddSingleton(mappingConfig.CreateMapper());
            return services;
        }
    }
}