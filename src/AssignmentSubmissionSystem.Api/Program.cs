using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Extensions;
using AssignmentSubmissionSystem.Api.Middleware;
using AssignmentSubmissionSystem.Api.Services;
using DotNetEnv;
using Serilog;
using System.Text.Json.Serialization;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services
    .AddPersistence(builder.Configuration)
    .AddApplicationServices()
    .AddJwtAuthentication(builder.Configuration)
    .AddFrontendCors()
    .AddRateLimiting()
    .AddSwaggerWithJwt();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// --- Migrate + seed on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await DbSeeder.SeedAsync(db, hasher, config);
}

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Assignment API v1");
        c.RoutePrefix = "swagger";
    });
}

// --- Middleware pipeline: order is load-bearing, kept explicit and inline
// on purpose rather than hidden inside an extension method. ---

app.UseCors(CorsExtensions.FrontendPolicy);   // must run early so CORS headers attach even if downstream code throws
app.UseRateLimiter();                          // before auth -- reject excess requests before spending effort validating a JWT

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
