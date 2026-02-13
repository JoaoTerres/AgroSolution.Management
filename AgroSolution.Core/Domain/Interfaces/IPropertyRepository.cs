using AgroSolution.Core.Domain.Entities;

namespace AgroSolution.Core.Domain.Interfaces;

public interface IPropertyRepository
{
    Task AddAsync(Property property);
    Task<Property?> GetByIdAsync(Guid id);
    Task<IEnumerable<Property>> GetByProducerIdAsync(Guid producerId);
    void Update(Property property);
    Task SaveChangesAsync();
}