using AssignmentSubmissionSystem.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Extensions;


public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"));
        });

        return services;
    }
}
