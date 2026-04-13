using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.GetProperties;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AgroSolution.Core.Tests.Features;

public class GetPropertiesTests
{
    private readonly Mock<IPropertyRepository> _repositoryMock = new();
    private readonly IGetProperties _sut;

    private readonly Guid _producerId = Guid.NewGuid();

    public GetPropertiesTests()
    {
        _sut = new GetProperties(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyGuid_ReturnsFail()
    {
        // Act
        var result = await _sut.ExecuteAsync(Guid.Empty);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        _repositoryMock.Verify(r => r.GetByProducerIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoProperties_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(_producerId))
            .ReturnsAsync(Enumerable.Empty<Property>());

        // Act
        var result = await _sut.ExecuteAsync(_producerId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithProperties_ReturnsMappedDtos()
    {
        // Arrange
        var properties = new[]
        {
            new Property("Fazenda Alpha", "Local Alpha - SP", _producerId),
            new Property("Fazenda Beta",  "Local Beta - MG",  _producerId)
        };

        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(_producerId))
            .ReturnsAsync(properties);

        // Act
        var result = await _sut.ExecuteAsync(_producerId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(p => p.Name).Should().Contain(["Fazenda Alpha", "Fazenda Beta"]);
        result.Data!.Select(p => p.Location).Should().Contain(["Local Alpha - SP", "Local Beta - MG"]);
    }

    [Fact]
    public async Task ExecuteAsync_PropertyWithPlots_MapsPlotDtos()
    {
        // Arrange
        var property = new Property("Fazenda com Talhões", "Interior - GO", _producerId);
        property.AddPlot("Talhão 1", "Soja", 30m);
        property.AddPlot("Talhão 2", "Milho", 20m);

        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(_producerId))
            .ReturnsAsync([property]);

        // Act
        var result = await _sut.ExecuteAsync(_producerId);

        // Assert
        result.Success.Should().BeTrue();
        var dto = result.Data!.Single();
        dto.Plots.Should().HaveCount(2);
        dto.Plots.Select(p => p.Name).Should().Contain(["Talhão 1", "Talhão 2"]);
        dto.Plots.Select(p => p.CropType).Should().Contain(["Soja", "Milho"]);
    }

    [Fact]
    public async Task ExecuteAsync_PropertyWithNoPlots_ReturnsEmptyPlotsList()
    {
        // Arrange
        var property = new Property("Fazenda Sem Talhões", "Local - PR", _producerId);

        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(_producerId))
            .ReturnsAsync([property]);

        // Act
        var result = await _sut.ExecuteAsync(_producerId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Single().Plots.Should().BeEmpty();
    }
}
