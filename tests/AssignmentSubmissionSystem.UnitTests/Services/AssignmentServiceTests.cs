using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using AssignmentSubmissionSystem.Api.Models.Enums;
using AssignmentSubmissionSystem.Api.Services;
using AssignmentSubmissionSystem.UnitTests.Helpers;
using FluentAssertions;

namespace AssignmentSubmissionSystem.UnitTests.Services;

public class AssignmentServiceTests
{
    private static async Task<(AppDbContext db, SchoolClass cls, Subject subject, User teacher, User student)> SeedBaseDataAsync()
    {
        var db = TestDbContextFactory.Create();

        var cls = new SchoolClass { Name = "Grade 10 - A" };
        var subject = new Subject { Name = "Mathematics" };
        var teacher = new User { FullName = "Mr. Karim", Email = "teacher@x.com", PasswordHash = "x", Role = Role.Teacher };
        var student = new User { FullName = "Akib", Email = "student@x.com", PasswordHash = "x", Role = Role.Student };

        db.AddRange(cls, subject, teacher, student);
        await db.SaveChangesAsync();

        return (db, cls, subject, teacher, student);
    }

    [Fact]
    public async Task CreateAsync_Should_Throw_Forbidden_When_Teacher_Not_Assigned()
    {
        var (db, cls, subject, teacher, _) = await SeedBaseDataAsync();
        // Deliberately NOT adding a TeacherAssignment row -- this is the case under test.
        var currentUser = MockCurrentUser.As(teacher.Id, Role.Teacher);
        var sut = new AssignmentService(db, currentUser);

        var dto = new CreateAssignmentDto { Title = "HW1", Description = "...", Deadline = DateTime.UtcNow.AddDays(7), MaxMarks = 100, SchoolClassId = cls.Id, SubjectId = subject.Id };

        var act = () => sut.CreateAsync(dto);
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*not assigned*");
    }

    [Fact]
    public async Task CreateAsync_Should_Succeed_When_Teacher_Is_Assigned()
    {
        var (db, cls, subject, teacher, _) = await SeedBaseDataAsync();
        db.TeacherAssignments.Add(new TeacherAssignment { TeacherId = teacher.Id, SchoolClassId = cls.Id, SubjectId = subject.Id });
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(teacher.Id, Role.Teacher);
        var sut = new AssignmentService(db, currentUser);

        var dto = new CreateAssignmentDto { Title = "HW1", Description = "...", Deadline = DateTime.UtcNow.AddDays(7), MaxMarks = 100, SchoolClassId = cls.Id, SubjectId = subject.Id };
        var result = await sut.CreateAsync(dto);

        result.Status.Should().Be(AssignmentStatus.Draft); // new assignments start as Draft, not visible to students
        result.TeacherId.Should().Be(teacher.Id);           // taken from the current user, not trusted from the DTO
    }

    [Fact]
    public async Task PublishAsync_Should_Throw_Forbidden_When_Caller_Is_Not_The_Owning_Teacher()
    {
        var (db, cls, subject, teacher, _) = await SeedBaseDataAsync();
        var otherTeacher = new User { FullName = "Other", Email = "other@x.com", PasswordHash = "x", Role = Role.Teacher };
        db.Users.Add(otherTeacher);

        var assignment = new Assignment { Title = "HW1", Description = "...", Deadline = DateTime.UtcNow.AddDays(7), MaxMarks = 100, TeacherId = teacher.Id, SchoolClassId = cls.Id, SubjectId = subject.Id };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(otherTeacher.Id, Role.Teacher); // NOT the creator
        var sut = new AssignmentService(db, currentUser);

        var act = () => sut.PublishAsync(assignment.Id);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task PublishAsync_Should_Succeed_When_Caller_Is_Admin_Even_If_Not_Creator()
    {
        var (db, cls, subject, teacher, _) = await SeedBaseDataAsync();
        var admin = new User { FullName = "Admin", Email = "admin@x.com", PasswordHash = "x", Role = Role.Admin };
        db.Users.Add(admin);

        var assignment = new Assignment { Title = "HW1", Description = "...", Deadline = DateTime.UtcNow.AddDays(7), MaxMarks = 100, TeacherId = teacher.Id, SchoolClassId = cls.Id, SubjectId = subject.Id };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(admin.Id, Role.Admin);
        var sut = new AssignmentService(db, currentUser);

        var result = await sut.PublishAsync(assignment.Id);
        result.Status.Should().Be(AssignmentStatus.Published);
    }

    [Fact]
    public async Task GetMineAsync_As_Student_Should_Only_Return_Published_Assignments_For_Enrolled_Classes()
    {
        var (db, cls, subject, teacher, student) = await SeedBaseDataAsync();

        var otherClass = new SchoolClass { Name = "Grade 10 - B" };
        db.SchoolClasses.Add(otherClass);

        db.StudentEnrollments.Add(new StudentEnrollment { StudentId = student.Id, SchoolClassId = cls.Id });

        db.Assignments.AddRange(
            new Assignment { Title = "Visible: published, enrolled class", Description = "..", Deadline = DateTime.UtcNow.AddDays(1), MaxMarks = 10, TeacherId = teacher.Id, SchoolClassId = cls.Id, SubjectId = subject.Id, Status = AssignmentStatus.Published },
            new Assignment { Title = "Hidden: still draft", Description = "..", Deadline = DateTime.UtcNow.AddDays(1), MaxMarks = 10, TeacherId = teacher.Id, SchoolClassId = cls.Id, SubjectId = subject.Id, Status = AssignmentStatus.Draft },
            new Assignment { Title = "Hidden: published but different class", Description = "..", Deadline = DateTime.UtcNow.AddDays(1), MaxMarks = 10, TeacherId = teacher.Id, SchoolClassId = otherClass.Id, SubjectId = subject.Id, Status = AssignmentStatus.Published }
        );
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(student.Id, Role.Student);
        var sut = new AssignmentService(db, currentUser);

        var result = await sut.GetMineAsync();

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Visible: published, enrolled class");
    }

    [Fact]
    public async Task DeleteAsync_Should_Throw_Forbidden_When_Teacher_Does_Not_Own_The_Assignment()
    {
        var (db, cls, subject, teacher, _) = await SeedBaseDataAsync();
        var otherTeacher = new User { FullName = "Other", Email = "other@x.com", PasswordHash = "x", Role = Role.Teacher };
        db.Users.Add(otherTeacher);

        var assignment = new Assignment { Title = "HW1", Description = "..", Deadline = DateTime.UtcNow.AddDays(1), MaxMarks = 10, TeacherId = teacher.Id, SchoolClassId = cls.Id, SubjectId = subject.Id };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(otherTeacher.Id, Role.Teacher);
        var sut = new AssignmentService(db, currentUser);

        var act = () => sut.DeleteAsync(assignment.Id);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
