using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.CreateProperty;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AgroSolution.Core.Tests.Features;

public class CreatePropertyTests
{
    private readonly Mock<IPropertyRepository> _repositoryMock = new();
    private readonly ICreateProperty _sut;

    private readonly Guid _producerId = Guid.NewGuid();

    public CreatePropertyTests()
    {
        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Enumerable.Empty<Property>());

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Property>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _sut = new CreateProperty(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidDto_ReturnsOkWithGuid()
    {
        // Arrange
        var dto = new CreatePropertyDto("Fazenda São João", "Ribeirão Preto - SP");

        // Act
        var result = await _sut.ExecuteAsync(dto, _producerId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingProperties_Succeeds()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(_producerId))
            .ReturnsAsync(Enumerable.Empty<Property>());

        var dto = new CreatePropertyDto("Fazenda Nova", "Campinas - SP");

        // Act
        var result = await _sut.ExecuteAsync(dto, _producerId);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateNameExactMatch_ReturnsFail()
    {
        // Arrange
        var existingProperty = new Property("Fazenda Santa Maria", "Uberlândia - MG", _producerId);
        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(_producerId))
            .ReturnsAsync([existingProperty]);

        var dto = new CreatePropertyDto("Fazenda Santa Maria", "Outro Local - MG");

        // Act
        var result = await _sut.ExecuteAsync(dto, _producerId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("nome");
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateNameCaseInsensitive_ReturnsFail()
    {
        // Arrange
        var existingProperty = new Property("Fazenda Santa Maria", "Uberlândia - MG", _producerId);
        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(_producerId))
            .ReturnsAsync([existingProperty]);

        var dto = new CreatePropertyDto("FAZENDA SANTA MARIA", "Outro Local - MG");

        // Act
        var result = await _sut.ExecuteAsync(dto, _producerId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCallSaveChanges_WhenDuplicate()
    {
        // Arrange
        var existingProperty = new Property("Fazenda Duplicada", "Local - SP", _producerId);
        _repositoryMock
            .Setup(r => r.GetByProducerIdAsync(_producerId))
            .ReturnsAsync([existingProperty]);

        var dto = new CreatePropertyDto("Fazenda Duplicada", "Outro Local - SP");

        // Act
        await _sut.ExecuteAsync(dto, _producerId);

        // Assert
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Property>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CallsSaveChangesOnce_OnSuccess()
    {
        // Arrange
        var dto = new CreatePropertyDto("Fazenda Boa Vista", "Goiânia - GO");

        // Act
        await _sut.ExecuteAsync(dto, _producerId);

        // Assert
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CallsAddAsync_OnSuccess()
    {
        // Arrange
        var dto = new CreatePropertyDto("Fazenda Progresso", "Mato Grosso - MT");

        // Act
        await _sut.ExecuteAsync(dto, _producerId);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Property>(p =>
            p.Name == dto.Name &&
            p.Location == dto.Location &&
            p.ProducerId == _producerId)),
            Times.Once);
    }
}
