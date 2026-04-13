using AgroSolution.Core.App.Features.GetByIdPlot;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AgroSolution.Core.Tests.Features;

public class GetByIdPlotTests
{
    private readonly Mock<IPropertyRepository> _repositoryMock = new();
    private readonly IGetByIdPlot _sut;

    public GetByIdPlotTests()
    {
        _sut = new GetByIdPlot(_repositoryMock.Object);
    }

    private static Plot MakePlot() =>
        new Plot(Guid.NewGuid(), "Talhão Teste", "Soja", 45.5m);

    [Fact]
    public async Task ExecuteAsync_WithValidPlotId_ReturnsOkWithDto()
    {
        // Arrange
        var plot = MakePlot();
        _repositoryMock
            .Setup(r => r.GetPlotByIdAsync(plot.Id))
            .ReturnsAsync(plot);

        // Act
        var result = await _sut.ExecuteAsync(plot.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlotNotFound_ReturnsFail()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPlotByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Plot?)null);

        // Act
        var result = await _sut.ExecuteAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Talhão não encontrado");
    }

    [Fact]
    public async Task ExecuteAsync_MapsAllFieldsCorrectly()
    {
        // Arrange
        var plot = MakePlot();
        _repositoryMock
            .Setup(r => r.GetPlotByIdAsync(plot.Id))
            .ReturnsAsync(plot);

        // Act
        var result = await _sut.ExecuteAsync(plot.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(plot.Id);
        result.Data.Name.Should().Be(plot.Name);
        result.Data.CropType.Should().Be(plot.CropType);
        result.Data.Area.Should().Be(plot.AreaInHectares);
    }
}
