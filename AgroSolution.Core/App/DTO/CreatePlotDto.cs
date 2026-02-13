namespace AgroSolution.Core.App.DTO;

public record CreatePlotDto(
    Guid PropertyId, 
    string Name, 
    string CropType, 
    decimal Area);