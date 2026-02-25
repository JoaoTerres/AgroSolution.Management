using AgroSolution.Identity.Domain.Entities;
using AgroSolution.Identity.Domain.Interfaces;
using AgroSolution.Identity.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroSolution.Identity.Infra.Repositories;

public class ProducerRepository(IdentityDbContext context) : IProducerRepository
{
    public async Task AddAsync(Producer producer)
        => await context.Producers.AddAsync(producer);

    public async Task<Producer?> GetByEmailAsync(string email)
        => await context.Producers
            .FirstOrDefaultAsync(p => p.Email == email.ToLowerInvariant());

    public async Task<bool> ExistsByEmailAsync(string email)
        => await context.Producers
            .AnyAsync(p => p.Email == email.ToLowerInvariant());

    public async Task SaveChangesAsync()
        => await context.SaveChangesAsync();
}
