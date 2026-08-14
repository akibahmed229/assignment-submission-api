using AssignmentSubmissionSystem.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // --- User ---
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        // --- SchoolClass / Subject: plain lookup tables, nothing special ---
        builder.Entity<SchoolClass>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
        });

        builder.Entity<Subject>(entity =>
        {
            entity.Property(s => s.Name).IsRequired().HasMaxLength(150);
            entity.Property(s => s.Code).HasMaxLength(20);
        });

        // --- TeacherAssignment: the "Admin assigns teachers to subjects/classes" join table ---
        // Answers: "which teacher teaches which subject, in which class?"
        builder.Entity<TeacherAssignment>(entity =>
        {
            // Same teacher can't be assigned to the same subject+class twice.
            // This is a DB-level guarantee, not just an app-level check --
            // even a bug in your service code can't create a duplicate row.
            entity.HasIndex(ta => new { ta.TeacherId, ta.SchoolClassId, ta.SubjectId }).IsUnique();

            entity.HasOne(ta => ta.Teacher)
                .WithMany()
                .HasForeignKey(ta => ta.TeacherId)
                .OnDelete(DeleteBehavior.Restrict); // don't cascade-delete a User and silently wipe their teaching assignments

            entity.HasOne(ta => ta.SchoolClass)
                .WithMany(c => c.TeacherAssignments)
                .HasForeignKey(ta => ta.SchoolClassId)
                .OnDelete(DeleteBehavior.Cascade); // deleting a class does clean up its assignments

            entity.HasOne(ta => ta.Subject)
                .WithMany(s => s.TeacherAssignments)
                .HasForeignKey(ta => ta.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- StudentEnrollment: the "student belongs to a class" join table ---
        // Answers: "which student belongs to which class?"
        builder.Entity<StudentEnrollment>(entity =>
        {
            // A student is only enrolled in a given class once.
            entity.HasIndex(se => new { se.StudentId, se.SchoolClassId }).IsUnique();

            entity.HasOne(se => se.Student)
                .WithMany()
                .HasForeignKey(se => se.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(se => se.SchoolClass)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(se => se.SchoolClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Assignment ---
        // An assignment a teacher has created for a specific class and subject.
        // Assignment.TeacherId says "who specifically created this piece of coursework"
        builder.Entity<Assignment>(entity =>
        {
            entity.Property(a => a.Title).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Description).IsRequired();
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.SchoolClass)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict); // don't let a class deletion cascade-wipe historical assignments

            entity.HasOne(a => a.Subject)
                .WithMany(s => s.Assignments)
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Submission ---
        // A student's answer to a specific assignment, plus its grading state.
        builder.Entity<Submission>(entity =>
        {
            entity.Property(s => s.AnswerText).IsRequired();
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            // A student can only submit once per assignment (your DTO/service
            // layer decides whether a re-submit before the deadline updates
            // this row or is rejected -- either way, no duplicate rows).
            entity.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();

            entity.HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade); // deleting an assignment does clean up its submissions

            entity.HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // GradedByTeacherId is a bare Guid? with no navigation property
            // (see Submission.cs) specifically to avoid EF needing to
            // disambiguate two separate FK paths to User from this entity.
            // No config needed here for that reason.
        });

        base.OnModelCreating(builder);
    }
}
