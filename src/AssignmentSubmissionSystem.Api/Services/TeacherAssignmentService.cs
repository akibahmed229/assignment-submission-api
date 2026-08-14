using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using AssignmentSubmissionSystem.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Services;

public interface ITeacherAssignmentService
{
    Task<TeacherAssignmentResponseDto> CreateAsync(CreateTeacherAssignmentDto dto);
    Task<List<TeacherAssignmentResponseDto>> GetAllAsync();
    Task<List<TeacherAssignmentResponseDto>> GetMineAsync(Guid teacherId);
    Task DeleteAsync(Guid id);
}

public class TeacherAssignmentService(AppDbContext db) : ITeacherAssignmentService
{
    public async Task<TeacherAssignmentResponseDto> CreateAsync(CreateTeacherAssignmentDto dto)
    {
        var teacher = await db.Users.FindAsync(dto.TeacherId) ??
            throw new NotFoundException($"User {dto.TeacherId} was not found.");
        if (teacher.Role != Role.Teacher)
            throw new InvalidOperationException($"User {teacher.Email} is not a Teacher.");

        var schoolClass = await db.SchoolClasses.FindAsync(dto.SchoolClassId) ??
            throw new NotFoundException($"Class {dto.SchoolClassId} was not found.");

        var subject = await db.Subjects.FindAsync(dto.SubjectId) ??
            throw new NotFoundException($"Subject {dto.SubjectId} was not found.");

        var alreadyAssigned = await db.TeacherAssignments.AnyAsync(ta =>
                ta.TeacherId == dto.TeacherId &&
                ta.SchoolClassId == dto.SchoolClassId &&
                ta.SubjectId == dto.SubjectId);
        if (alreadyAssigned)
            throw new InvalidOperationException("This teacher is already assigned to this subject for this class.");

        // Note: this AnyAsync check is a friendly-error convenience, not the
        // real guard -- the unique index from AppDbContext is what actually
        // prevents duplicates under concurrent requests. If two identical
        // requests land at the same instant, this check can pass for both,
        // and the second SaveChangesAsync will throw a DbUpdateException
        // instead. Worth catching and mapping to a nicer message later;
        // skipped here to keep this pass focused.

        var entity = new TeacherAssignment { TeacherId = teacher.Id, SchoolClassId = schoolClass.Id, SubjectId = subject.Id };

        db.TeacherAssignments.Add(entity);
        await db.SaveChangesAsync();

        return new TeacherAssignmentResponseDto(
            entity.Id, teacher.Id, teacher.FullName, schoolClass.Id, schoolClass.Name, subject.Id, subject.Name);
    }

    public async Task<List<TeacherAssignmentResponseDto>> GetAllAsync()
    {
        return await db.TeacherAssignments
            .Include(ta => ta.Teacher)
            .Include(ta => ta.SchoolClass)
            .Include(ta => ta.Subject)
            .Select(ta => new TeacherAssignmentResponseDto
                    (ta.Id, ta.TeacherId, ta.Teacher.FullName, ta.SchoolClassId, ta.SchoolClass.Name, ta.SubjectId, ta.Subject.Name))
            .ToListAsync();

        // This .Select DOES translate to SQL fine -- unlike ToDto above,
        // this is a `new DTO(...)` constructor call with plain property
        // access, not an arbitrary method invocation. EF can turn that
        // straight into a SQL JOIN + column projection.
    }

    public async Task<List<TeacherAssignmentResponseDto>> GetMineAsync(Guid teacherId)
    {
        return await db.TeacherAssignments
            .Where(ta => ta.TeacherId == teacherId)
            .Include(ta => ta.Teacher)
            .Include(ta => ta.SchoolClass)
            .Include(ta => ta.Subject)
            .Select(ta => new TeacherAssignmentResponseDto(
            ta.Id, ta.TeacherId, ta.Teacher.FullName, ta.SchoolClassId, ta.SchoolClass.Name, ta.SubjectId, ta.Subject.Name))
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.TeacherAssignments.FindAsync(id)
        ?? throw new NotFoundException($"Teacher assignment {id} was not found.");

        db.TeacherAssignments.Remove(entity);
        await db.SaveChangesAsync();
    }
}
