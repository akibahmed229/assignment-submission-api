using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using AssignmentSubmissionSystem.Api.Models.Enums;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Services;

public interface IAssignmentService
{
    Task<AssignmentResponseDto> CreateAsync(CreateAssignmentDto dto);
    Task<AssignmentResponseDto> PublishAsync(Guid id);
    Task<AssignmentResponseDto> GetByIdAsync(Guid id);
    Task<List<AssignmentResponseDto>> GetMineAsync();
    Task DeleteAsync(Guid id);
}

public class AssignmentService(AppDbContext db, ICurrentUserService currentUser) : IAssignmentService
{
    private static AssignmentResponseDto ToDto(Assignment a) => new(
        a.Id,
        a.Title,
        a.Description,
        a.MaxMarks,
        a.Deadline,
        a.Status,
        a.TeacherId,
        a.SchoolClassId,
        a.SubjectId,
        a.CreatedAt);

    private async Task<Assignment> GetOwnedByCurrentTeacherAsync(Guid id)
    {
        var entity = await db.Assignments.FindAsync(id)
            ?? throw new NotFoundException($"Assignment {id} was not found.");

        if (currentUser.Role != Role.Admin && entity.TeacherId != currentUser.UserId)
            throw new ForbiddenAccessException($"You can only manage assignments you created.");

        return entity;
    }

    private async Task EnsureViewableAsync(Assignment entity)
    {
        if (currentUser.Role == Role.Admin) return;
        if (currentUser.Role == Role.Teacher && entity.TeacherId == currentUser.UserId) return;

        if (currentUser.Role == Role.Student)
        {
            var enrolled = await db.StudentEnrollments.AnyAsync(se =>
                    se.StudentId == currentUser.UserId && se.SchoolClassId == entity.SchoolClassId);

            if (enrolled && entity.Status == AssignmentStatus.Published) return;
        }

        throw new ForbiddenAccessException("You do not have access to this assignment.");
    }

    public async Task<AssignmentResponseDto> CreateAsync(CreateAssignmentDto dto)
    {
        // This is the check [Authorize(Roles = "Teacher")] CANNOT do --
        // that attribute only confirms "some teacher" is calling. This
        // confirms THIS teacher is actually assigned to teach THIS
        // subject for THIS class, via the TeacherAssignment table.
        var isAssigned = await db.TeacherAssignments.AnyAsync(ta =>
                ta.TeacherId == currentUser.UserId &&
                ta.SchoolClassId == dto.SchoolClassId &&
                ta.SubjectId == dto.SubjectId);
        if (!isAssigned)
            throw new ForbiddenAccessException("You are not assigned to teach this subject for this class.");

        var entity = new Assignment
        {
            Title = dto.Title,
            Description = dto.Description,
            Deadline = dto.Deadline,
            MaxMarks = dto.MaxMarks,
            TeacherId = currentUser.UserId,
            SchoolClassId = dto.SchoolClassId,
            SubjectId = dto.SubjectId
        };

        db.Assignments.Add(entity);
        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<AssignmentResponseDto> PublishAsync(Guid id)
    {
        var entity = await GetOwnedByCurrentTeacherAsync(id);

        entity.Status = AssignmentStatus.Published;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();


        return ToDto(entity);
    }

    public async Task<AssignmentResponseDto> GetByIdAsync(Guid id)
    {
        var entity = await db.Assignments.FindAsync(id)
            ?? throw new NotFoundException($"Assignment {id} was not found.");

        await EnsureViewableAsync(entity);

        return ToDto(entity);
    }

    public async Task<List<AssignmentResponseDto>> GetMineAsync()
    {
        // Same endpoint, different result shape per role -- this is the
        // "role-based visibility" the brief describes, done at the query
        // level instead of filtering a full list in memory.
        IQueryable<Assignment> query = currentUser.Role switch
        {
            Role.Admin => db.Assignments,
            Role.Teacher => db.Assignments.Where(a => a.TeacherId == currentUser.UserId),
            Role.Student => db.Assignments.Where(a =>
                    a.Status == AssignmentStatus.Published &&
                    db.StudentEnrollments.Any(se => se.StudentId == currentUser.UserId && se.SchoolClassId == a.SchoolClassId)),
            _ => db.Assignments.Where(a => false)
        };

        var assignments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

        return assignments.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetOwnedByCurrentTeacherAsync(id);

        db.Assignments.Remove(entity);
        await db.SaveChangesAsync();
    }
}
