using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;

namespace AgroSolution.Core.App.Features.CreateProperty;

public interface ICreateProperty
{
    Task<Result<Guid>> ExecuteAsync(CreatePropertyDto dto, Guid producerId);
}