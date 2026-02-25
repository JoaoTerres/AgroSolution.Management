namespace AgroSolution.Identity.Infra.Services;

public interface IJwtTokenService
{
    /// <summary>Returns (token, expiresInSeconds).</summary>
    (string Token, int ExpiresIn) GenerateToken(Guid producerId, string name, string email);
}
