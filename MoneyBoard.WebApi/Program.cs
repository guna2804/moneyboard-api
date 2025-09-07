using MoneyBoard.Application;
using MoneyBoard.Infrastructure;
using MoneyBoard.WebApi.Extensions;
using MoneyBoard.WebApi.Middleware;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .CreateBootstrapLogger();

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    builder.Services.AddControllers();

    // Determine allowed origins based on environment
    var allowedOrigins = builder.Environment.IsDevelopment()
        ? new[] { "*" } // Allow all in development
        : new[] { "https://smart-loan-tracker.vercel.app" };

    // Add CORS policy to allow specific origins
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", policyBuilder =>
        {
            if (allowedOrigins.Contains("*"))
            {
                policyBuilder.AllowAnyOrigin()
                             .AllowAnyMethod()
                             .AllowAnyHeader()
                             .AllowCredentials();
            }
            else
            {
                policyBuilder.WithOrigins(allowedOrigins)
                             .AllowAnyMethod()
                             .AllowAnyHeader()
                             .AllowCredentials();
            }
        });
    });

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApiServices(builder.Configuration);

    var app = builder.Build();

    app.UseMiddleware<CorsLoggingMiddleware>();
    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowSpecificOrigins");
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