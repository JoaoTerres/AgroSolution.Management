using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain;
using AgroSolution.Core.Domain.Entities;
using FluentAssertions;

namespace AgroSolution.Core.Tests.Domain;

// ─────────────────────────────────────────────────────────────────────────────
// Property Entity Tests
// ─────────────────────────────────────────────────────────────────────────────
public class PropertyEntityTests
{
    private static readonly Guid ValidProducerId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesProperty()
    {
        // Act
        var property = new Property("Fazenda Boa Esperança", "Sertãozinho - SP", ValidProducerId);

        // Assert
        property.Id.Should().NotBe(Guid.Empty);
        property.Name.Should().Be("Fazenda Boa Esperança");
        property.Location.Should().Be("Sertãozinho - SP");
        property.ProducerId.Should().Be(ValidProducerId);
        property.Plots.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsDomainException()
    {
        // Act
        var act = () => new Property("", "Localização Válida - SP", ValidProducerId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*nome*");
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsDomainException()
    {
        // Act
        var act = () => new Property("   ", "Localização Válida - SP", ValidProducerId);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithEmptyLocation_ThrowsDomainException()
    {
        // Act
        var act = () => new Property("Fazenda Válida", "", ValidProducerId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*localização*");
    }

    [Fact]
    public void Constructor_WithEmptyProducerId_ThrowsDomainException()
    {
        // Act
        var act = () => new Property("Fazenda Válida", "Local Válido - SP", Guid.Empty);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*produtor*");
    }

    [Fact]
    public void AddPlot_WithValidArgs_AddsOnePlot()
    {
        // Arrange
        var property = new Property("Fazenda Verde", "Local - GO", ValidProducerId);

        // Act
        property.AddPlot("Talhão 1", "Soja", 10m);

        // Assert
        property.Plots.Should().HaveCount(1);
        property.Plots.First().Name.Should().Be("Talhão 1");
    }

    [Fact]
    public void AddPlot_MultipleTimes_AddsMultiplePlots()
    {
        // Arrange
        var property = new Property("Fazenda Grande", "Local - MT", ValidProducerId);

        // Act
        property.AddPlot("Talhão A", "Milho", 15m);
        property.AddPlot("Talhão B", "Soja", 20m);
        property.AddPlot("Talhão C", "Algodão", 30m);

        // Assert
        property.Plots.Should().HaveCount(3);
    }

    [Fact]
    public void AddPlot_WithInvalidArgs_ThrowsDomainException()
    {
        // Arrange
        var property = new Property("Fazenda Teste", "Local - RS", ValidProducerId);

        // Act — area zero é inválida
        var act = () => property.AddPlot("Talhão Inválido", "Soja", 0m);

        // Assert
        act.Should().Throw<DomainException>();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Plot Entity Tests
// ─────────────────────────────────────────────────────────────────────────────
public class PlotEntityTests
{
    private static readonly Guid ValidPropertyId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesPlot()
    {
        // Act
        var plot = new Plot(ValidPropertyId, "Talhão Norte", "Café", 12.5m);

        // Assert
        plot.Id.Should().NotBe(Guid.Empty);
        plot.PropertyId.Should().Be(ValidPropertyId);
        plot.Name.Should().Be("Talhão Norte");
        plot.CropType.Should().Be("Café");
        plot.AreaInHectares.Should().Be(12.5m);
    }

    [Fact]
    public void Constructor_WithEmptyPropertyId_ThrowsDomainException()
    {
        // Act
        var act = () => new Plot(Guid.Empty, "Talhão", "Soja", 10m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*propriedade*");
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsDomainException()
    {
        // Act
        var act = () => new Plot(ValidPropertyId, "", "Milho", 10m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*nome*");
    }

    [Fact]
    public void Constructor_WithEmptyCropType_ThrowsDomainException()
    {
        // Act
        var act = () => new Plot(ValidPropertyId, "Talhão Válido", "", 10m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*cultura*");
    }

    [Fact]
    public void Constructor_WithZeroArea_ThrowsDomainException()
    {
        // Act
        var act = () => new Plot(ValidPropertyId, "Talhão Válido", "Soja", 0m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*área*");
    }

    [Fact]
    public void Constructor_WithNegativeArea_ThrowsDomainException()
    {
        // Act
        var act = () => new Plot(ValidPropertyId, "Talhão Válido", "Soja", -5m);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithSmallPositiveArea_Succeeds()
    {
        // Act
        var act = () => new Plot(ValidPropertyId, "Talhão Pequeno", "Hortifruti", 0.001m);

        // Assert
        act.Should().NotThrow();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// IoTData Status Transition Tests
// ─────────────────────────────────────────────────────────────────────────────
public class IoTDataStatusTransitionTests
{
    private static readonly Guid ValidPlotId = Guid.NewGuid();

    private static IoTData MakeIoTData() =>
        new IoTData(ValidPlotId, IoTDeviceType.TemperatureSensor, "{\"value\":25,\"unit\":\"C\"}", DateTime.UtcNow);

    [Fact]
    public void Constructor_SetsStatusToPending()
    {
        // Act
        var data = MakeIoTData();

        // Assert
        data.ProcessingStatus.Should().Be(IoTProcessingStatus.Pending);
        data.Id.Should().NotBe(Guid.Empty);
        data.PlotId.Should().Be(ValidPlotId);
    }

    [Fact]
    public void MarkAsQueued_SetsStatusToQueued_AndSetsQueueId()
    {
        // Arrange
        var data = MakeIoTData();

        // Act
        data.MarkAsQueued("queue-id-001");

        // Assert
        data.ProcessingStatus.Should().Be(IoTProcessingStatus.Queued);
        data.ProcessingQueueId.Should().Be("queue-id-001");
        data.ProcessingStartedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsQueued_WithNullQueueId_ThrowsDomainException()
    {
        // Arrange
        var data = MakeIoTData();

        // Act
        var act = () => data.MarkAsQueued(null!);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsProcessed_SetsStatusToProcessed()
    {
        // Arrange
        var data = MakeIoTData();
        data.MarkAsQueued("queue-abc");

        // Act
        data.MarkAsProcessed();

        // Assert
        data.ProcessingStatus.Should().Be(IoTProcessingStatus.Processed);
        data.ProcessingCompletedAt.Should().NotBeNull();
        data.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_SetsStatusToFailed_AndSetsErrorMessage()
    {
        // Arrange
        var data = MakeIoTData();
        data.MarkAsQueued("queue-xyz");

        // Act
        data.MarkAsFailed("Erro de processamento: JSON inválido");

        // Assert
        data.ProcessingStatus.Should().Be(IoTProcessingStatus.Failed);
        data.ErrorMessage.Should().Be("Erro de processamento: JSON inválido");
        data.ProcessingCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_WithNullMessage_ThrowsDomainException()
    {
        // Arrange
        var data = MakeIoTData();

        // Act
        var act = () => data.MarkAsFailed(null!);

        // Assert
        act.Should().Throw<DomainException>();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Alert Entity Tests
// ─────────────────────────────────────────────────────────────────────────────
public class AlertEntityTests
{
    private static readonly Guid ValidPlotId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesActiveAlert()
    {
        // Act
        var alert = new Alert(ValidPlotId, AlertType.Drought, "Umidade abaixo do limite.");

        // Assert
        alert.Id.Should().NotBe(Guid.Empty);
        alert.PlotId.Should().Be(ValidPlotId);
        alert.Type.Should().Be(AlertType.Drought);
        alert.Message.Should().Be("Umidade abaixo do limite.");
        alert.IsActive.Should().BeTrue();
        alert.ResolvedAt.Should().BeNull();
        alert.TriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithEmptyPlotId_ThrowsDomainException()
    {
        // Act
        var act = () => new Alert(Guid.Empty, AlertType.Drought, "Mensagem válida");

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithNullMessage_ThrowsDomainException()
    {
        // Act
        var act = () => new Alert(ValidPlotId, AlertType.ExtremeHeat, null!);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithInvalidAlertType_ThrowsDomainException()
    {
        // Act
        var act = () => new Alert(ValidPlotId, (AlertType)99, "Mensagem válida");

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Resolve_SetsIsActiveToFalse()
    {
        // Arrange
        var alert = new Alert(ValidPlotId, AlertType.HeavyRain, "Chuva intensa detectada.");

        // Act
        alert.Resolve();

        // Assert
        alert.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Resolve_SetsResolvedAt()
    {
        // Arrange
        var alert = new Alert(ValidPlotId, AlertType.ExtremeHeat, "Temperatura extrema.");

        // Act
        alert.Resolve();

        // Assert
        alert.ResolvedAt.Should().NotBeNull();
        alert.ResolvedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
