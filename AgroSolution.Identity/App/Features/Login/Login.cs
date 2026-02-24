using AgroSolution.Identity.App.Common;
using AgroSolution.Identity.App.DTO;
using AgroSolution.Identity.Domain.Interfaces;
using AgroSolution.Identity.Infra.Services;

namespace AgroSolution.Identity.App.Features.Login;

public class Login(
    IProducerRepository repository,
    IPasswordHasher     passwordHasher,
    IJwtTokenService    jwtTokenService) : ILogin
{
    public async Task<Result<TokenResponseDto>> ExecuteAsync(LoginDto dto)
    {
        if (dto is null)
            return Result<TokenResponseDto>.Fail("Credenciais são obrigatórias.");

        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Result<TokenResponseDto>.Fail("E-mail e senha são obrigatórios.");

        var producer = await repository.GetByEmailAsync(dto.Email);
        if (producer is null || !passwordHasher.Verify(dto.Password, producer.PasswordHash))
            return Result<TokenResponseDto>.Fail("E-mail ou senha inválidos.");

        var (token, expiresIn) = jwtTokenService.GenerateToken(producer.Id, producer.Name, producer.Email);

        return Result<TokenResponseDto>.Ok(new TokenResponseDto(
            AccessToken:  token,
            TokenType:    "Bearer",
            ExpiresIn:    expiresIn,
            ProducerId:   producer.Id,
            ProducerName: producer.Name,
            Email:        producer.Email));
    }
}
