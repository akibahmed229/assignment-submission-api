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
        var secret = config["Jwt:Secret"]!;
        var claims = new List<Claim> {
            new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Email, user.Email),
            new (ClaimTypes.Role, user.Role.ToString()) // [Authorize(Roles = "Admin")] reads this claim
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpiryMinutes"]!)),
                signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
