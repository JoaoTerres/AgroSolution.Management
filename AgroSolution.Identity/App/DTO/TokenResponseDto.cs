namespace AgroSolution.Identity.App.DTO;

public record TokenResponseDto(
    string AccessToken,
    string TokenType,
    int    ExpiresIn,
    Guid   ProducerId,
    string ProducerName,
    string Email
);
