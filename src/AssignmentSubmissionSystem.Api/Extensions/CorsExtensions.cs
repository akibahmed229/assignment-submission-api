namespace AssignmentSubmissionSystem.Api.Extensions;

public static class CorsExtensions
{
    public const string FrontendPolicy = "Fronted";

    public static IServiceCollection AddFrontedCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(FrontendPolicy, policy =>
            {
                policy.WithOrigins(
                        "http://localhost:3000",
                        "https://assignment-submission-frontend-six.vercel.app")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}
