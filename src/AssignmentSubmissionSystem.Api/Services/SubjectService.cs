using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Services;

public interface ISubjectService
{
    Task<SubjectResponseDto> CreateAsync(CreateSubjectDto dto);
    Task<List<SubjectResponseDto>> GetAllAsync();
    Task<SubjectResponseDto> GetByIdAsync(Guid id);
    Task DeleteAsync(Guid id);
}

public class SubjectService(AppDbContext db) : ISubjectService
{
    private static SubjectResponseDto ToDto(Subject s) => new(s.Id, s.Name, s.Code, s.CreatedAt);

    public async Task<SubjectResponseDto> CreateAsync(CreateSubjectDto dto)
    {
        var entity = new Subject { Name = dto.Name, Code = dto.Code };

        db.Subjects.Add(entity);
        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<List<SubjectResponseDto>> GetAllAsync()
    {
        var subject = await db.Subjects.OrderBy(s => s.Name).ToListAsync();

        return subject.Select(ToDto).ToList();
    }

    public async Task<SubjectResponseDto> GetByIdAsync(Guid id)
    {
        var entity = await db.Subjects.FindAsync(id)
            ?? throw new NotFoundException($"Subject {id} was not found.");

        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.Subjects.FindAsync(id)
            ?? throw new NotFoundException($"Subject {id} was not found.");

        db.Subjects.Remove(entity);
        await db.SaveChangesAsync();
    }
}
