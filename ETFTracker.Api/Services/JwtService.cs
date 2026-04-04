using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer    = _configuration["Jwt:Issuer"]   ?? "ETFTracker";
        var audience  = _configuration["Jwt:Audience"] ?? "ETFTracker";
        var expiryDays = int.Parse(_configuration["Jwt:ExpiryDays"] ?? "30");

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("userId",          user.Id.ToString()),
            new("email",           user.Email           ?? ""),
            new("name",            $"{user.FirstName} {user.LastName}".Trim()),
            new("avatarUrl",       user.AvatarUrl       ?? ""),
            new("githubUsername",  user.GitHubUsername  ?? ""),
        };

        var token = new JwtSecurityToken(
            issuer:            issuer,
            audience:          audience,
            claims:            claims,
            expires:           DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

