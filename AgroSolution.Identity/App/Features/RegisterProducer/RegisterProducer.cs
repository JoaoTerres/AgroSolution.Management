using AgroSolution.Identity.App.Common;
using AgroSolution.Identity.App.DTO;
using AgroSolution.Identity.Domain.Entities;
using AgroSolution.Identity.Domain.Interfaces;
using AgroSolution.Identity.Infra.Services;

namespace AgroSolution.Identity.App.Features.RegisterProducer;

public class RegisterProducer(
    IProducerRepository repository,
    IPasswordHasher     passwordHasher) : IRegisterProducer
{
    public async Task<Result<Guid>> ExecuteAsync(RegisterProducerDto dto)
    {
        if (dto is null)
            return Result<Guid>.Fail("Dados de cadastro são obrigatórios.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<Guid>.Fail("O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<Guid>.Fail("O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return Result<Guid>.Fail("A senha deve ter no mínimo 6 caracteres.");

        if (await repository.ExistsByEmailAsync(dto.Email))
            return Result<Guid>.Fail("Já existe um produtor cadastrado com este e-mail.");

        var hash     = passwordHasher.Hash(dto.Password);
        var producer = new Producer(dto.Name, dto.Email, hash);

        await repository.AddAsync(producer);
        await repository.SaveChangesAsync();

        return Result<Guid>.Ok(producer.Id);
    }
}
