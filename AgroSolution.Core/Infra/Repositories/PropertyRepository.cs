using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroSolution.Core.Infra.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly ManagementDbContext _context;

    public PropertyRepository(ManagementDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Property property)
    {
        await _context.Properties.AddAsync(property);
    }

    public async Task<Property?> GetByIdAsync(Guid id)
    {
        return await _context.Properties
            .Include(p => p.Plots)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Property>> GetByProducerIdAsync(Guid producerId)
    {
        return await _context.Properties
            .Include(p => p.Plots)
            .Where(p => p.ProducerId == producerId)
            .ToListAsync();
    }

    public void Update(Property property)
    {
        _context.Properties.Update(property);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}