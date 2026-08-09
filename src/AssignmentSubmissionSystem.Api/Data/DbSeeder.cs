using AssignmentSubmissionSystem.Api.Models.Entities;
using AssignmentSubmissionSystem.Api.Models.Enums;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Data;

public class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, IConfiguration config)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync()) return; // already seeded

        db.Users.AddRange(
            new User { FullName = "System Admin", Email = "admin@assignmentsystem.local", Role = Role.Admin, PasswordHash = hasher.Hash(config["Seed:AdminPassword"]!.ToString()) },
            new User { FullName = "Demo Teacher", Email = "teacher@assignmentsystem.local", Role = Role.Teacher, PasswordHash = hasher.Hash(config["Seed:TeacherPassword"]!.ToString()) },
            new User { FullName = "Demo Student", Email = "student@assignmentsystem.local", Role = Role.Student, PasswordHash = hasher.Hash(config["Seed:StudentPassword"]!.ToString()) }
        );

        await db.SaveChangesAsync();
    }
}
