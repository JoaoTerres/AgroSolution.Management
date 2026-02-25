using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AgroSolution.Identity.Infra.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public (string Token, int ExpiresIn) GenerateToken(Guid producerId, string name, string email)
    {
        var secret  = configuration["Jwt:SecretKey"]!;
        var issuer  = configuration["Jwt:Issuer"]!;
        var audience = configuration["Jwt:Audience"]!;
        var hours   = int.Parse(configuration["Jwt:ExpirationInHours"] ?? "24");

        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry  = DateTime.UtcNow.AddHours(hours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   producerId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name,  name),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier,     producerId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiry,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresIn   = (int)(expiry - DateTime.UtcNow).TotalSeconds;

        return (tokenString, expiresIn);
    }
}
