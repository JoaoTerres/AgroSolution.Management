namespace AgroSolution.Core.App.DTO;

public record PropertyResponseDto(
    Guid Id, 
    string Name, 
    string Location, 
    List<PlotResponseDto> Plots);