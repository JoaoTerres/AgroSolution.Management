using AgroSolution.Identity.Domain.Entities;

namespace AgroSolution.Identity.Domain.Interfaces;

public interface IProducerRepository
{
    Task AddAsync(Producer producer);
    Task<Producer?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task SaveChangesAsync();
}
