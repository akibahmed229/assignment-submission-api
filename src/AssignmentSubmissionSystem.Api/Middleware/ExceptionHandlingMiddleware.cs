using System.Net;
using System.Text.Json;
using AssignmentSubmissionSystem.Api.Exceptions;

namespace AssignmentSubmissionSystem.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, message) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, ex.Message),
                ForbiddenAccessException => (HttpStatusCode.Forbidden, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            if (status == HttpStatusCode.InternalServerError)
                logger.LogError(ex, "Unhandled exception");
            else
                logger.LogWarning("Handled exception: {Message}", ex.Message);

            // Clear existing response headers (except CORS) if response hasn't started
            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)status;

                await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = (int)status, message }));
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = (int)status, message }));
        }
    }
}
