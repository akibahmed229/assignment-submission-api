using System.ComponentModel.DataAnnotations;
using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Services;

public interface ISchoolClassService
{
    Task<SchoolClassResponseDto> CreateAsync(CreateSchoolClassDto dto);
    Task<List<SchoolClassResponseDto>> GetAllAsync();
    Task<SchoolClassResponseDto> GetByIdAsync(Guid id);
    Task DeleteAsync(Guid id);
}

public class SchoolClassService(AppDbContext db) : ISchoolClassService
{
    private static SchoolClassResponseDto ToDto(SchoolClass c) => new(c.Id, c.Name, c.CreatedAt);

    public async Task<SchoolClassResponseDto> CreateAsync(CreateSchoolClassDto dto)
    {
        var entity = new SchoolClass { Name = dto.Name };

        db.SchoolClasses.Add(entity);
        await db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<List<SchoolClassResponseDto>> GetAllAsync()
    {
        // Materialize first, THEN map -- ToDto is a plain C# method, and EF
        // can't translate an arbitrary method call into SQL. Calling
        // .Select(ToDto) directly on the IQueryable throws at runtime.
        var classes = await db.SchoolClasses.OrderBy(c => c.Name).ToListAsync();

        return classes.Select(ToDto).ToList();
    }

    public async Task<SchoolClassResponseDto> GetByIdAsync(Guid id)
    {
        var entity = await db.SchoolClasses.FindAsync(id) ??
            throw new NotFoundException($"Class {id} was not found.");

        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.SchoolClasses.FindAsync(id) ??
            throw new NotFoundException($"Class {id} was not found.");

        db.SchoolClasses.Remove(entity);
        await db.SaveChangesAsync();
    }

}
