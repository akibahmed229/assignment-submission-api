using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}

public class AuthService(AppDbContext db, IPasswordHasher hasher, IJwtService jwt) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email is already registered.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = hasher.Hash(dto.Password),
            Role = dto.Role
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new AuthResponseDto(user.Id, user.FullName, user.Email, user.Role, jwt.GenerateToken(user));
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // Same message for "no such user" and "wrong password" -- don't
        // leak which one it was, that's a user-enumeration vector.
        if (user is null || !user.IsActive || !hasher.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthResponseDto(user.Id, user.FullName, user.Email, user.Role, jwt.GenerateToken(user));
    }

}
