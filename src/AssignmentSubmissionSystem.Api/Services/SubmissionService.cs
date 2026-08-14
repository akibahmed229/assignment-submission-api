using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using AssignmentSubmissionSystem.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Services;

public interface ISubmissionService
{

    Task<SubmissionResponseDto> SubmitAsync(Guid assignmentId, CreateSubmissionDto dto);
    Task<SubmissionResponseDto> UpdateAsync(Guid submissionId, CreateSubmissionDto dto);
    Task<SubmissionResponseDto> GradeAsync(Guid submissionId, GradeSubmissionDto dto);
    Task<List<SubmissionResponseDto>> GetForAssignmentAsync(Guid assignmentId);
    Task<List<SubmissionResponseDto>> GetMineAsync();
}

public class SubmissionService(AppDbContext db, ICurrentUserService currentUser) : ISubmissionService
{
    private static SubmissionResponseDto ToDto(Submission s) => new(
       s.Id,
       s.AssignmentId,
       s.StudentId,
       s.AnswerText,
       s.SubmittedAt,
       s.Status,
       s.Marks,
       s.Feedback,
       s.GradedAt);

    public async Task<SubmissionResponseDto> SubmitAsync(Guid assignmentId, CreateSubmissionDto dto)
    {
        var assignment = await db.Assignments.FindAsync(assignmentId)
            ?? throw new NotFoundException($"Assignment {assignmentId} was not found.");
        if (assignment.Status != AssignmentStatus.Published)
            throw new ForbiddenAccessException("This assignment is not published yet.");

        var enrolled = await db.StudentEnrollments.AnyAsync(se =>
                se.StudentId == currentUser.UserId &&
                se.SchoolClassId == assignment.SchoolClassId);
        if (!enrolled)
            throw new ForbiddenAccessException("You are not enrolled in this assignment's class.");

        var alreadySubmitted = await db.Submissions.AnyAsync(s =>
                s.AssignmentId == assignmentId &&
                s.StudentId == currentUser.UserId);
        if (alreadySubmitted)
            throw new InvalidOperationException("You have already submitted this assignment. Use update instead.");

        var entity = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = currentUser.UserId,
            AnswerText = dto.AnswerText,
            SubmittedAt = DateTime.UtcNow,
            // Late submissions are still accepted, just flagged -- whether
            // to hard-block them instead is a product decision, not a
            // technical one; easy to change to a thrown exception if the
            // brief expects a hard cutoff.
            Status = DateTime.UtcNow > assignment.Deadline ? SubmissionStatus.Late : SubmissionStatus.Submitted
        };

        db.Submissions.Add(entity);
        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<SubmissionResponseDto> UpdateAsync(Guid submissionId, CreateSubmissionDto dto)
    {
        var entity = await db.Submissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.Id == submissionId) ??
            throw new NotFoundException($"Submission {submissionId} was not found.");

        if (entity.StudentId != currentUser.UserId)
            throw new ForbiddenAccessException("You can only update your own submission.");

        if (entity.Status == SubmissionStatus.Graded)
            throw new InvalidOperationException("This submission has already been graded and can no longer be edited.");

        if (DateTime.UtcNow > entity.Assignment.Deadline)
            throw new InvalidOperationException("The deadline has passed -- this submission can no longer be updated.");

        entity.AnswerText = dto.AnswerText;
        entity.SubmittedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<SubmissionResponseDto> GradeAsync(Guid submissionId, GradeSubmissionDto dto)
    {
        var entity = await db.Submissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.Id == submissionId) ??
            throw new NotFoundException($"Submission {submissionId} was not found.");

        if (currentUser.Role != Role.Admin && entity.Assignment.TeacherId != currentUser.UserId)
            throw new ForbiddenAccessException("You can only grade submissions for assignments you created.");

        if (dto.Marks > entity.Assignment.MaxMarks)
            throw new InvalidOperationException($"Marks cannot exceed the assignment's maximum of {entity.Assignment.MaxMarks}.");

        entity.Marks = dto.Marks;
        entity.Feedback = dto.Feedback;
        entity.Status = SubmissionStatus.Graded;
        entity.GradedByTeacherId = currentUser.UserId;
        entity.GradedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<List<SubmissionResponseDto>> GetForAssignmentAsync(Guid assignmentId)
    {
        var assignment = await db.Assignments.FindAsync(assignmentId) ??
            throw new NotFoundException($"Assignment {assignmentId} was not found.");

        if (currentUser.Role != Role.Admin && assignment.TeacherId != currentUser.UserId)
            throw new ForbiddenAccessException("You can only view submissions for assignments you created.");


        var submissions = await db.Submissions
            .Where(s => s.AssignmentId == assignmentId)
            .OrderBy(s => s.SubmittedAt)
            .ToListAsync();

        return submissions.Select(ToDto).ToList();
    }

    public async Task<List<SubmissionResponseDto>> GetMineAsync()
    {
        var submissions = await db.Submissions
            .Where(s => s.StudentId == currentUser.UserId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return submissions.Select(ToDto).ToList();
    }

}
