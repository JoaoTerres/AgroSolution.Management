using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;

namespace AgroSolution.Core.App.Features.GetProperties;

public interface IGetProperties
{
    Task<Result<IEnumerable<PropertyResponseDto>>> ExecuteAsync(Guid producerId);
}