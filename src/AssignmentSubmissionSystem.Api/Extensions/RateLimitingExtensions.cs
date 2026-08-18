using System.Threading.RateLimiting;

namespace AssignmentSubmissionSystem.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string LoginPolicy = "LoginPolicy";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(LoginPolicy, httpContext =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 6,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }
                );
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                await context.HttpContext.Response.WriteAsync(
                            """{"status":429,"title":"Too many login attempts. Please wait a minute and try again."}""",
                            cancellationToken
                );
            };
        });

        return services;
    }
}
