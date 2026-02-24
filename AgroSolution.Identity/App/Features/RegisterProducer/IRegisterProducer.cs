using AgroSolution.Identity.App.Common;
using AgroSolution.Identity.App.DTO;

namespace AgroSolution.Identity.App.Features.RegisterProducer;

public interface IRegisterProducer
{
    Task<Result<Guid>> ExecuteAsync(RegisterProducerDto dto);
}
