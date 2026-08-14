using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using AssignmentSubmissionSystem.Api.Models.Enums;
using AssignmentSubmissionSystem.Api.Services;
using AssignmentSubmissionSystem.UnitTests.Helpers;
using FluentAssertions;

namespace AssignmentSubmissionSystem.UnitTests.Services;

public class SubmissionServiceTests
{
    private static async Task<(AppDbContext db, Assignment assignment, User teacher, User student)> SeedPublishedAssignmentAsync(DateTime? deadline = null)
    {
        var db = TestDbContextFactory.Create();

        var cls = new SchoolClass { Name = "Grade 10 - A" };
        var subject = new Subject { Name = "Math" };
        var teacher = new User { FullName = "Teacher", Email = "t@x.com", PasswordHash = "x", Role = Role.Teacher };
        var student = new User { FullName = "Student", Email = "s@x.com", PasswordHash = "x", Role = Role.Student };

        var assignment = new Assignment
        {
            Title = "HW1",
            Description = "..",
            MaxMarks = 100,
            Deadline = deadline ?? DateTime.UtcNow.AddDays(7),
            Status = AssignmentStatus.Published,
            TeacherId = teacher.Id,
            SchoolClassId = cls.Id,
            SubjectId = subject.Id,
            SchoolClass = cls,
            Subject = subject,
            Teacher = teacher
        };

        db.AddRange(cls, subject, teacher, student, assignment);
        db.StudentEnrollments.Add(new StudentEnrollment { StudentId = student.Id, SchoolClassId = cls.Id });
        await db.SaveChangesAsync();

        return (db, assignment, teacher, student);
    }

    [Fact]
    public async Task SubmitAsync_Should_Throw_Forbidden_When_Student_Not_Enrolled_In_Class()
    {
        var (db, assignment, _, _) = await SeedPublishedAssignmentAsync();
        var strangerStudent = new User { FullName = "Stranger", Email = "stranger@x.com", PasswordHash = "x", Role = Role.Student };
        db.Users.Add(strangerStudent);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(strangerStudent.Id, Role.Student);
        var sut = new SubmissionService(db, currentUser);

        var act = () => sut.SubmitAsync(assignment.Id, new CreateSubmissionDto { AnswerText = "my answer" });
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*not enrolled*");
    }

    [Fact]
    public async Task SubmitAsync_Should_Throw_Forbidden_When_Assignment_Still_Draft()
    {
        var (db, assignment, _, student) = await SeedPublishedAssignmentAsync();
        assignment.Status = AssignmentStatus.Draft;
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(student.Id, Role.Student);
        var sut = new SubmissionService(db, currentUser);

        var act = () => sut.SubmitAsync(assignment.Id, new CreateSubmissionDto { AnswerText = "answer" });
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*not published*");
    }

    [Fact]
    public async Task SubmitAsync_Should_Mark_Late_When_Submitted_After_Deadline()
    {
        var (db, assignment, _, student) = await SeedPublishedAssignmentAsync(deadline: DateTime.UtcNow.AddMinutes(-10));
        var currentUser = MockCurrentUser.As(student.Id, Role.Student);
        var sut = new SubmissionService(db, currentUser);

        var result = await sut.SubmitAsync(assignment.Id, new CreateSubmissionDto { AnswerText = "answer" });

        result.Status.Should().Be(SubmissionStatus.Late);
    }

    [Fact]
    public async Task SubmitAsync_Should_Mark_OnTime_When_Submitted_Before_Deadline()
    {
        var (db, assignment, _, student) = await SeedPublishedAssignmentAsync(deadline: DateTime.UtcNow.AddDays(1));
        var currentUser = MockCurrentUser.As(student.Id, Role.Student);
        var sut = new SubmissionService(db, currentUser);

        var result = await sut.SubmitAsync(assignment.Id, new CreateSubmissionDto { AnswerText = "answer" });

        result.Status.Should().Be(SubmissionStatus.Submitted);
    }

    [Fact]
    public async Task SubmitAsync_Should_Throw_When_Student_Already_Submitted()
    {
        var (db, assignment, _, student) = await SeedPublishedAssignmentAsync();
        db.Submissions.Add(new Submission { AssignmentId = assignment.Id, StudentId = student.Id, AnswerText = "first try" });
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(student.Id, Role.Student);
        var sut = new SubmissionService(db, currentUser);

        var act = () => sut.SubmitAsync(assignment.Id, new CreateSubmissionDto { AnswerText = "second try" });
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already submitted*");
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Submission_Already_Graded()
    {
        var (db, assignment, teacher, student) = await SeedPublishedAssignmentAsync();
        var submission = new Submission { AssignmentId = assignment.Id, StudentId = student.Id, AnswerText = "answer", Status = SubmissionStatus.Graded, Marks = 90, GradedByTeacherId = teacher.Id, GradedAt = DateTime.UtcNow };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(student.Id, Role.Student);
        var sut = new SubmissionService(db, currentUser);

        var act = () => sut.UpdateAsync(submission.Id, new CreateSubmissionDto { AnswerText = "trying to change graded answer" });
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been graded*");
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Deadline_Has_Passed()
    {
        var (db, assignment, _, student) = await SeedPublishedAssignmentAsync(deadline: DateTime.UtcNow.AddMinutes(-5));
        var submission = new Submission { AssignmentId = assignment.Id, StudentId = student.Id, AnswerText = "answer", Status = SubmissionStatus.Late };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(student.Id, Role.Student);
        var sut = new SubmissionService(db, currentUser);

        var act = () => sut.UpdateAsync(submission.Id, new CreateSubmissionDto { AnswerText = "updated answer" });
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*deadline has passed*");
    }

    [Fact]
    public async Task GradeAsync_Should_Throw_Forbidden_When_Caller_Is_Not_The_Assignment_Owner()
    {
        var (db, assignment, _, student) = await SeedPublishedAssignmentAsync();
        var otherTeacher = new User { FullName = "Other Teacher", Email = "other@x.com", PasswordHash = "x", Role = Role.Teacher };
        db.Users.Add(otherTeacher);
        var submission = new Submission { AssignmentId = assignment.Id, StudentId = student.Id, AnswerText = "answer" };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(otherTeacher.Id, Role.Teacher);
        var sut = new SubmissionService(db, currentUser);

        var act = () => sut.GradeAsync(submission.Id, new GradeSubmissionDto { Marks = 80 });
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GradeAsync_Should_Throw_When_Marks_Exceed_Assignment_MaxMarks()
    {
        var (db, assignment, teacher, student) = await SeedPublishedAssignmentAsync(); // MaxMarks = 100
        var submission = new Submission { AssignmentId = assignment.Id, StudentId = student.Id, AnswerText = "answer" };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(teacher.Id, Role.Teacher);
        var sut = new SubmissionService(db, currentUser);

        var act = () => sut.GradeAsync(submission.Id, new GradeSubmissionDto { Marks = 150 });
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot exceed*");
    }

    [Fact]
    public async Task GradeAsync_Should_Set_All_Grading_Fields_On_Success()
    {
        var (db, assignment, teacher, student) = await SeedPublishedAssignmentAsync();
        var submission = new Submission { AssignmentId = assignment.Id, StudentId = student.Id, AnswerText = "answer" };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        var currentUser = MockCurrentUser.As(teacher.Id, Role.Teacher);
        var sut = new SubmissionService(db, currentUser);

        var result = await sut.GradeAsync(submission.Id, new GradeSubmissionDto { Marks = 88, Feedback = "Good work" });

        result.Status.Should().Be(SubmissionStatus.Graded);
        result.Marks.Should().Be(88);
        result.Feedback.Should().Be("Good work");
        result.GradedAt.Should().NotBeNull();
    }
}
