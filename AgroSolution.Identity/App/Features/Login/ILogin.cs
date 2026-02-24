using AgroSolution.Identity.App.Common;
using AgroSolution.Identity.App.DTO;

namespace AgroSolution.Identity.App.Features.Login;

public interface ILogin
{
    Task<Result<TokenResponseDto>> ExecuteAsync(LoginDto dto);
}
