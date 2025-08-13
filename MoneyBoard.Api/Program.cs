using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using MoneyBoard.Application;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Validators;
using MoneyBoard.Infrastructure;
using MoneyBoard.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Register validators
builder.Services.AddScoped<IValidator<RegisterDto>, AuthValidator.RegisterValidator>();
builder.Services.AddScoped<IValidator<LoginDto>, AuthValidator.LoginValidator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Application & Infrastructure services
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();