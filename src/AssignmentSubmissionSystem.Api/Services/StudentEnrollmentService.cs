using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using AssignmentSubmissionSystem.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Services;

public interface IStudentEnrollmentService
{
    Task<StudentEnrollmentResponseDto> CreateAsync(CreateStudentEnrollmentDto dto);
    Task<List<StudentEnrollmentResponseDto>> GetByClassAsync(Guid schoolClassId);
    Task DeleteAsync(Guid id);
}

public class StudentEnrollmentService(AppDbContext db) : IStudentEnrollmentService
{
    public async Task<StudentEnrollmentResponseDto> CreateAsync(CreateStudentEnrollmentDto dto)
    {
        var student = await db.Users.FindAsync(dto.StudentId) ??
            throw new NotFoundException($"User {dto.StudentId} was not found.");
        if (student.Role != Role.Student)
            throw new InvalidOperationException($"User {student.Email} is not a Student.");

        var schoolClass = await db.SchoolClasses.FindAsync(dto.SchoolClassId) ??
                        throw new NotFoundException($"Class {dto.SchoolClassId} was not found.");

        var alreadyEnrolled = await db.StudentEnrollments.AnyAsync(se =>
                se.StudentId == dto.StudentId &&
                se.SchoolClassId == dto.SchoolClassId);
        if (alreadyEnrolled)
            throw new InvalidOperationException("This student is already enrolled in this class.");

        var entity = new StudentEnrollment { StudentId = student.Id, SchoolClassId = schoolClass.Id };

        db.StudentEnrollments.Add(entity);
        await db.SaveChangesAsync();

        return new StudentEnrollmentResponseDto(entity.Id, student.Id, student.FullName, schoolClass.Id, schoolClass.Name);
    }

    public async Task<List<StudentEnrollmentResponseDto>> GetByClassAsync(Guid schoolClassId)
    {
        return await db.StudentEnrollments
            .Where(se => se.SchoolClassId == schoolClassId)
            .Select(se => new StudentEnrollmentResponseDto(se.Id, se.StudentId, se.Student.FullName, se.SchoolClassId, se.SchoolClass.Name))
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.StudentEnrollments.FindAsync(id)
               ?? throw new NotFoundException($"Enrollment {id} was not found.");

        db.StudentEnrollments.Remove(entity);
        await db.SaveChangesAsync();
    }
}
