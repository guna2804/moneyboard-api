using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MoneyBoard.WebApi.Middleware
{
    public class CorsLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorsLoggingMiddleware> _logger;

        public CorsLoggingMiddleware(RequestDelegate next, ILogger<CorsLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == "OPTIONS")
            {
                _logger.LogInformation("CORS preflight request: {Method} {Path} from {Origin}",
                    context.Request.Method, context.Request.Path, context.Request.Headers["Origin"]);
            }

            await _next(context);

            if (context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
            {
                _logger.LogInformation("CORS headers added to response: {Origin} for {Path}",
                    context.Response.Headers["Access-Control-Allow-Origin"], context.Request.Path);
            }
        }
    }
}