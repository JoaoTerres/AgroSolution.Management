using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.AddPlot;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AgroSolution.Core.Tests.Features;

public class AddPlotTests
{
    private readonly Mock<IPropertyRepository> _repositoryMock = new();
    private readonly IAddPlot _sut;

    private readonly Guid _producerId = Guid.NewGuid();

    public AddPlotTests()
    {
        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _sut = new AddPlot(_repositoryMock.Object);
    }

    private Property MakeProperty() =>
        new Property("Fazenda Teste", "Localização Teste - SP", _producerId);

    [Fact]
    public async Task ExecuteAsync_WithValidDto_ReturnsOkWithPlotGuid()
    {
        // Arrange
        var property = MakeProperty();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(property);

        var dto = new CreatePlotDto(property.Id, "Talhão Norte", "Soja", 25.5m);

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPropertyNotFound_ReturnsFail()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Property?)null);

        var dto = new CreatePlotDto(Guid.NewGuid(), "Talhão Sul", "Milho", 10m);

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("propriedade");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPropertyNotFound_DoesNotCallSaveChanges()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Property?)null);

        var dto = new CreatePlotDto(Guid.NewGuid(), "Talhão Leste", "Café", 5m);

        // Act
        await _sut.ExecuteAsync(dto);

        // Assert
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidDto_CallsSaveChangesOnce()
    {
        // Arrange
        var property = MakeProperty();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(property);

        var dto = new CreatePlotDto(property.Id, "Talhão Oeste", "Cana", 100m);

        // Act
        await _sut.ExecuteAsync(dto);

        // Assert
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PlotIsAddedToProperty()
    {
        // Arrange
        var property = MakeProperty();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(property);

        var dto = new CreatePlotDto(property.Id, "Talhão Central", "Algodão", 50m);

        // Act
        await _sut.ExecuteAsync(dto);

        // Assert
        property.Plots.Should().HaveCount(1);
        property.Plots.First().Name.Should().Be("Talhão Central");
        property.Plots.First().CropType.Should().Be("Algodão");
        property.Plots.First().AreaInHectares.Should().Be(50m);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsLastPlotId()
    {
        // Arrange
        var property = MakeProperty();
        property.AddPlot("Talhão Antigo", "Soja", 10m); // talhão pré-existente

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(property);

        var dto = new CreatePlotDto(property.Id, "Talhão Novo", "Milho", 20m);

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(property.Plots.Last().Id);
        property.Plots.Should().HaveCount(2);
    }
}
