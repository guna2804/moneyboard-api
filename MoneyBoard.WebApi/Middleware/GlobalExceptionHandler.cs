using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MoneyBoard.WebApi.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;

            _logger.LogError(ex,
                "Unhandled exception. TraceId: {TraceId}, Path: {Path}",
                traceId, context.Request.Path);

            var problemDetails = new ProblemDetails
            {
                Type = "https://moneyboard.com/errors/unhandled",
                Title = "An unexpected error occurred.",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = ex.ToString(),
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = traceId }
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problemDetails.Status ?? 500;
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}