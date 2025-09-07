using MoneyBoard.Application;
using MoneyBoard.Infrastructure;
using MoneyBoard.WebApi.Extensions;
using MoneyBoard.WebApi.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using System.Security.Cryptography.X509Certificates;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Setup Serilog logging
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .CreateBootstrapLogger();

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    // Add Controllers
    builder.Services.AddControllers();

    // Determine allowed origins based on environment
    var allowedOrigins = builder.Environment.IsDevelopment()
        ? new[] { "*" } // Allow all in development
        : new[] { "https://smart-loan-tracker.vercel.app" };

    // Add CORS policy
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", policyBuilder =>
        {
            if (allowedOrigins.Contains("*"))
            {
                // Development: allow all (no credentials)
                policyBuilder.AllowAnyOrigin()
                             .AllowAnyMethod()
                             .AllowAnyHeader();
            }
            else
            {
                // Production: restrict to frontend, allow credentials
                policyBuilder.WithOrigins(allowedOrigins)
                             .AllowAnyMethod()
                             .AllowAnyHeader()
                             .AllowCredentials();
            }
        });
    });

    // Register application + infrastructure services
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApiServices(builder.Configuration);

    // Configure Data Protection for production
    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddDataProtection()
            .SetApplicationName("MoneyBoard");
    }

    var app = builder.Build();

    // Configure forwarded headers (important for Render.com / reverse proxies)
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        RequireHeaderSymmetry = false,
        ForwardLimit = 2
    });

    // Middleware
    app.UseMiddleware<CorsLoggingMiddleware>();
    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseSerilogRequestLogging();

    // Swagger only in Development
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Order is important!
    app.UseCors("AllowSpecificOrigins");
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}