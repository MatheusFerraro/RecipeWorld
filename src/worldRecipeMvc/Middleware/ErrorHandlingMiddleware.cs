using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace worldRecipeMvc.Middleware
{
    /// <summary>
    /// Converts unhandled exceptions on API routes into RFC 7807 ProblemDetails JSON.
    /// Non-API requests rethrow so the outer exception handler (developer page in
    /// Development, /Home/Error in Production) can render an HTML error page.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                if (context.Response.HasStarted || !context.Request.Path.StartsWithSegments("/api"))
                {
                    // Let the outer exception handler render the HTML error page
                    throw;
                }

                await WriteProblemDetailsAsync(context, ex);
            }
        }

        private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
        {
            context.Response.Clear();
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var problem = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = "An error occurred while processing your request.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Instance = context.Request.Path
            };

            if (_env.IsDevelopment())
            {
                problem.Detail = exception.ToString();
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }
}
