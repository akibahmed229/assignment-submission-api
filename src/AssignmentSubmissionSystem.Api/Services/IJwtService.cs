using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssignmentSubmissionSystem.Api.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AssignmentSubmissionSystem.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}

public class JwtService(IConfiguration config) : IJwtService
{
    public string GenerateToken(User user)
    {
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Safe parse with fallback to 60 minutes
        double expiryMinutes = double.TryParse(config["Jwt:ExpiryMinutes"], out var minutes)
            ? minutes
            : 60;

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
