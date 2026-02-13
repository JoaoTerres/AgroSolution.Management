using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;

namespace AgroSolution.Core.App.Features.CreateProperty;

public class CreateProperty(IPropertyRepository repository) : ICreateProperty
{
    public async Task<Result<Guid>> ExecuteAsync(CreatePropertyDto dto, Guid producerId)
    {
        var properties = await repository.GetByProducerIdAsync(producerId);
        if (properties.Any(p => p.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
            return Result<Guid>.Fail("Você já possui uma propriedade cadastrada com este nome.");

        var property = new Property(dto.Name, dto.Location, producerId);
        
        await repository.AddAsync(property);
        await repository.SaveChangesAsync();

        return Result<Guid>.Ok(property.Id);
    }
}