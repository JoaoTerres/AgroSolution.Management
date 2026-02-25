using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.AlertEngine;
using AgroSolution.Core.App.Features.GetAlerts;
using AgroSolution.Core.App.Features.GetIoTDataByRange;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AgroSolution.Core.Tests.Features;

// ─────────────────────────────────────────────────────────────────────────────
// AlertEngineService Tests
// ─────────────────────────────────────────────────────────────────────────────
public class AlertEngineServiceTests
{
    private readonly Mock<IIoTDataRepository>  _iotRepoMock  = new();
    private readonly Mock<IAlertRepository>    _alertRepoMock = new();
    private readonly AlertEngineService        _sut;

    private readonly Guid _plotId = Guid.NewGuid();

    public AlertEngineServiceTests()
    {
        _sut = new AlertEngineService(
            _iotRepoMock.Object,
            _alertRepoMock.Object,
            NullLogger<AlertEngineService>.Instance);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IoTData MakeHumidityData(Guid plotId, float humidityValue, DateTime? at = null)
    {
        var d = new IoTData(
            plotId,
            IoTDeviceType.HumiditySensor,
            $"{{\"value\":{humidityValue},\"unit\":\"%\"}}",
            at ?? DateTime.UtcNow);
        d.MarkAsQueued("test-queue");
        d.MarkAsProcessed();
        return d;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_TemperatureDevice_SkipsDroughtEvaluation()
    {
        // Arrange — no repo calls expected for non-humidity device type
        _iotRepoMock
            .Setup(r => r.GetByPlotIdAndDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Empty<IoTData>());

        // Act
        await _sut.EvaluateAsync(_plotId, IoTDeviceType.TemperatureSensor);

        // Assert — no IoT repo calls for temperature-only data
        _iotRepoMock.Verify(
            r => r.GetByPlotIdAndDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_InsufficientReadings_DoesNotCreateAlert()
    {
        // Arrange — only 1 reading (below minimum of 2)
        var readings = new[] { MakeHumidityData(_plotId, 10f) };

        _iotRepoMock
            .Setup(r => r.GetByPlotIdAndDateRangeAsync(_plotId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        _alertRepoMock
            .Setup(r => r.GetActiveByPlotIdAndTypeAsync(_plotId, AlertType.Drought))
            .ReturnsAsync((Alert?)null);

        // Act
        await _sut.EvaluateAsync(_plotId, IoTDeviceType.HumiditySensor);

        // Assert
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_AllReadingsBelowThreshold_CreatesAlert()
    {
        // Arrange — 3 readings all < 30%
        var readings = new[]
        {
            MakeHumidityData(_plotId, 10f),
            MakeHumidityData(_plotId, 12f),
            MakeHumidityData(_plotId, 15f)
        };

        _iotRepoMock
            .Setup(r => r.GetByPlotIdAndDateRangeAsync(_plotId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        _alertRepoMock
            .Setup(r => r.GetActiveByPlotIdAndTypeAsync(_plotId, AlertType.Drought))
            .ReturnsAsync((Alert?)null);

        _alertRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Alert>()))
            .ReturnsAsync(true);

        // Act
        await _sut.EvaluateAsync(_plotId, IoTDeviceType.HumiditySensor);

        // Assert
        _alertRepoMock.Verify(r => r.AddAsync(
            It.Is<Alert>(a => a.Type == AlertType.Drought && a.PlotId == _plotId && a.IsActive)),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_ReadingsMixed_DoesNotCreateAlert()
    {
        // Arrange — one reading above threshold → not all below 30%
        var readings = new[]
        {
            MakeHumidityData(_plotId, 10f),
            MakeHumidityData(_plotId, 45f)  // above 30%
        };

        _iotRepoMock
            .Setup(r => r.GetByPlotIdAndDateRangeAsync(_plotId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        _alertRepoMock
            .Setup(r => r.GetActiveByPlotIdAndTypeAsync(_plotId, AlertType.Drought))
            .ReturnsAsync((Alert?)null);

        // Act
        await _sut.EvaluateAsync(_plotId, IoTDeviceType.HumiditySensor);

        // Assert
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_ActiveAlertExists_DoesNotCreateDuplicate()
    {
        // Arrange — active alert already exists
        var readings = new[]
        {
            MakeHumidityData(_plotId, 5f),
            MakeHumidityData(_plotId, 8f)
        };

        var existingAlert = new Alert(_plotId, AlertType.Drought, "existing drought");

        _iotRepoMock
            .Setup(r => r.GetByPlotIdAndDateRangeAsync(_plotId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        _alertRepoMock
            .Setup(r => r.GetActiveByPlotIdAndTypeAsync(_plotId, AlertType.Drought))
            .ReturnsAsync(existingAlert);

        // Act
        await _sut.EvaluateAsync(_plotId, IoTDeviceType.HumiditySensor);

        // Assert — no new alert added
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_ActiveAlert_ReadingsNowAboveThreshold_ResolvesAlert()
    {
        // Arrange — existing active alert, but readings now normal
        var readings = new[]
        {
            MakeHumidityData(_plotId, 60f),
            MakeHumidityData(_plotId, 65f)
        };

        var existingAlert = new Alert(_plotId, AlertType.Drought, "drought alert");

        _iotRepoMock
            .Setup(r => r.GetByPlotIdAndDateRangeAsync(_plotId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        _alertRepoMock
            .Setup(r => r.GetActiveByPlotIdAndTypeAsync(_plotId, AlertType.Drought))
            .ReturnsAsync(existingAlert);

        _alertRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Alert>()))
            .ReturnsAsync(true);

        // Act
        await _sut.EvaluateAsync(_plotId, IoTDeviceType.HumiditySensor);

        // Assert — alert resolved
        _alertRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Alert>(a => !a.IsActive && a.ResolvedAt.HasValue)),
            Times.Once);
        _alertRepoMock.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GetAlerts Use Case Tests
// ─────────────────────────────────────────────────────────────────────────────
public class GetAlertsTests
{
    private readonly Mock<IAlertRepository> _repoMock = new();
    private readonly IGetAlerts _sut;

    private readonly Guid _plotId = Guid.NewGuid();

    public GetAlertsTests() => _sut = new GetAlerts(_repoMock.Object);

    [Fact]
    public async Task ExecuteAsync_EmptyGuid_ReturnsFail()
    {
        var result = await _sut.ExecuteAsync(Guid.Empty);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_NoAlerts_ReturnsEmptyList()
    {
        _repoMock
            .Setup(r => r.GetByPlotIdAsync(_plotId))
            .ReturnsAsync(Enumerable.Empty<Alert>());

        var result = await _sut.ExecuteAsync(_plotId);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithAlerts_ReturnsMappedDtos()
    {
        var alerts = new[]
        {
            new Alert(_plotId, AlertType.Drought, "seca detectada"),
            new Alert(_plotId, AlertType.ExtremeHeat, "calor extremo")
        };

        _repoMock
            .Setup(r => r.GetByPlotIdAsync(_plotId))
            .ReturnsAsync(alerts);

        var result = await _sut.ExecuteAsync(_plotId);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(a => a.Type).Should().Contain(["Drought", "ExtremeHeat"]);
        result.Data!.All(a => a.PlotId == _plotId).Should().BeTrue();
        result.Data!.All(a => a.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ResolvedAlert_MapsResolvedAtCorrectly()
    {
        var alert = new Alert(_plotId, AlertType.Drought, "seca");
        alert.Resolve();

        _repoMock
            .Setup(r => r.GetByPlotIdAsync(_plotId))
            .ReturnsAsync([alert]);

        var result = await _sut.ExecuteAsync(_plotId);

        result.Success.Should().BeTrue();
        var dto = result.Data!.Single();
        dto.IsActive.Should().BeFalse();
        dto.ResolvedAt.Should().NotBeNull();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GetIoTDataByRange Use Case Tests
// ─────────────────────────────────────────────────────────────────────────────
public class GetIoTDataByRangeTests
{
    private readonly Mock<IIoTDataRepository> _repoMock = new();
    private readonly IGetIoTDataByRange _sut;

    private readonly Guid _plotId = Guid.NewGuid();

    public GetIoTDataByRangeTests() => _sut = new GetIoTDataByRange(_repoMock.Object);

    [Fact]
    public async Task ExecuteAsync_EmptyPlotId_ReturnsFail()
    {
        var result = await _sut.ExecuteAsync(Guid.Empty, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_FromAfterTo_ReturnsFail()
    {
        var result = await _sut.ExecuteAsync(_plotId, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("anterior");
    }

    [Fact]
    public async Task ExecuteAsync_FromEqualTo_ReturnsFail()
    {
        var now = DateTime.UtcNow;
        var result = await _sut.ExecuteAsync(_plotId, now, now);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_RangeExceeds90Days_ReturnsFail()
    {
        var from = DateTime.UtcNow.AddDays(-91);
        var to   = DateTime.UtcNow;

        var result = await _sut.ExecuteAsync(_plotId, from, to);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("90 dias");
    }

    [Fact]
    public async Task ExecuteAsync_ValidRange_ReturnsOkWithMappedDtos()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to   = DateTime.UtcNow;

        var data = new[]
        {
            new IoTData(_plotId, IoTDeviceType.HumiditySensor, "{\"value\":42}", DateTime.UtcNow.AddHours(-2)),
            new IoTData(_plotId, IoTDeviceType.TemperatureSensor, "{\"value\":28,\"unit\":\"C\"}", DateTime.UtcNow.AddHours(-1))
        };

        _repoMock
            .Setup(r => r.GetByPlotIdAndDateRangeAsync(_plotId, from, to))
            .ReturnsAsync(data);

        var result = await _sut.ExecuteAsync(_plotId, from, to);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.All(d => d.PlotId == _plotId).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_NoDataInRange_ReturnsEmptyList()
    {
        _repoMock
            .Setup(r => r.GetByPlotIdAndDateRangeAsync(_plotId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Empty<IoTData>());

        var result = await _sut.ExecuteAsync(_plotId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
