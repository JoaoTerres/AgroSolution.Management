namespace AgroSolution.Core.App.DTO;

public record PlotResponseDto(
    Guid Id, 
    string Name, 
    string CropType, 
    decimal Area);