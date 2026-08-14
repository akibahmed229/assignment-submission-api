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

        string adminPwd = config["Seed:AdminPassword"] ?? "Admin123";
        string teacherPwd = config["Seed:TeacherPassword"] ?? "Teacher123";
        string studentPwd = config["Seed:StudentPassword"] ?? "Student123";

        db.Users.AddRange(
            new User { FullName = "System Admin", Email = "admin@assignmentsystem.local", Role = Role.Admin, PasswordHash = hasher.Hash(adminPwd) },
            new User { FullName = "Demo Teacher", Email = "teacher@assignmentsystem.local", Role = Role.Teacher, PasswordHash = hasher.Hash(teacherPwd) },
            new User { FullName = "Demo Student", Email = "student@assignmentsystem.local", Role = Role.Student, PasswordHash = hasher.Hash(studentPwd) }
        );

        await db.SaveChangesAsync();
    }
}
