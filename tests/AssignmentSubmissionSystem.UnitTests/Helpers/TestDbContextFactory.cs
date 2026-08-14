using AssignmentSubmissionSystem.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.UnitTests.Helpers;

/// <summary>
/// Every test gets its own isolated in-memory database (unique name per
/// call via Guid) so tests never see each other's data and can run in
/// parallel safely. Still a real AppDbContext -- your OnModelCreating
/// config (unique indexes, FK rules) still applies, just backed by
/// memory instead of Postgres.
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
