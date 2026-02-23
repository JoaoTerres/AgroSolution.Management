using System;
using System.Threading.Tasks;
using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.ReceiveIoTData;
using AgroSolution.Core.App.Validation;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace AgroSolution.Core.Tests.Features;

public class ReceiveIoTDataTests
{
    private readonly Mock<IIoTDataRepository> _iotDataRepositoryMock;
    private readonly Mock<IDeviceRepository> _deviceRepositoryMock;
    private readonly IoTDeviceValidatorFactory _validatorFactory;
    private readonly IReceiveIoTData _receiveIoTData;

    private readonly Guid _testPlotId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    private readonly string _testDeviceId = "agri-sensor-node-042";

    public ReceiveIoTDataTests()
    {
        _iotDataRepositoryMock = new Mock<IIoTDataRepository>();
        _deviceRepositoryMock = new Mock<IDeviceRepository>();
        _validatorFactory = new IoTDeviceValidatorFactory();

        _iotDataRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<IoTData>()))
            .ReturnsAsync(true);

        _receiveIoTData = new ReceiveIoTData(
            _iotDataRepositoryMock.Object,
            _validatorFactory,
            _deviceRepositoryMock.Object);
    }

    #region Temperature Sensor Tests

    [Fact]
    public async Task ExecuteAsync_WithValidTemperatureData_ShouldSucceed()
    {
        // Arrange
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = "{\"value\": 25.5, \"unit\": \"C\"}",
            DeviceTimestamp = DateTime.UtcNow
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.PlotId.Should().Be(_testPlotId);
        result.Data.DeviceType.Should().Be(IoTDeviceType.TemperatureSensor);
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTemperatureValue_ShouldFail()
    {
        // Arrange - temperature out of range (-60 to 60)
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = "{\"value\": 150, \"unit\": \"C\"}",
            DeviceTimestamp = DateTime.UtcNow
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Formato de dados inválido");
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingTemperatureValue_ShouldFail()
    {
        // Arrange
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = "{\"unit\": \"C\"}",
            DeviceTimestamp = DateTime.UtcNow
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Never);
    }

    #endregion

    #region Humidity Sensor Tests

    [Fact]
    public async Task ExecuteAsync_WithValidHumidityData_ShouldSucceed()
    {
        // Arrange
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.HumiditySensor,
            RawData = "{\"value\": 65.5, \"unit\": \"%\"}",
            DeviceTimestamp = DateTime.UtcNow
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.DeviceType.Should().Be(IoTDeviceType.HumiditySensor);
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithHumidityOutOfRange_ShouldFail()
    {
        // Arrange - humidity > 100
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.HumiditySensor,
            RawData = "{\"value\": 150, \"unit\": \"%\"}",
            DeviceTimestamp = DateTime.UtcNow
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Never);
    }

    #endregion

    #region Precipitation Sensor Tests

    [Fact]
    public async Task ExecuteAsync_WithValidPrecipitationData_ShouldSucceed()
    {
        // Arrange
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.PrecipitationSensor,
            RawData = "{\"value\": 12.4, \"unit\": \"mm\"}",
            DeviceTimestamp = DateTime.UtcNow
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.DeviceType.Should().Be(IoTDeviceType.PrecipitationSensor);
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNegativePrecipitation_ShouldFail()
    {
        // Arrange
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.PrecipitationSensor,
            RawData = "{\"value\": -5, \"unit\": \"mm\"}",
            DeviceTimestamp = DateTime.UtcNow
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Never);
    }

    #endregion

    #region Weather Station Tests

    [Fact]
    public async Task ExecuteAsync_WithValidWeatherStationData_ShouldSucceed()
    {
        // Arrange
        _deviceRepositoryMock
            .Setup(x => x.GetPlotIdByDeviceAsync(_testDeviceId))
            .ReturnsAsync(_testPlotId);

        var weatherStationJson = @"{
            ""device_id"": ""agri-sensor-node-042"",
            ""timestamp"": ""2026-02-18T14:30:00Z"",
            ""location"": { ""lat"": -21.1704, ""lon"": -47.8103 },
            ""telemetry"": {
                ""temperature_air"": 28.5,
                ""humidity_air"": 62.0,
                ""pressure"": 1013.2,
                ""precipitation_mm"": 12.4,
                ""wind_speed_kmh"": 5.2,
                ""soil_moisture_1"": 45.2
            },
            ""device_status"": { ""battery_voltage"": 3.7, ""rssi"": -72 }
        }";

        var dto = new ReceiveIoTDataDto
        {
            DeviceType = IoTDeviceType.WeatherStationNode,
            RawData = weatherStationJson
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.PlotId.Should().Be(_testPlotId);
        result.Data.DeviceType.Should().Be(IoTDeviceType.WeatherStationNode);
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithWeatherStationAndUnknownDeviceId_ShouldFail()
    {
        // Arrange
        _deviceRepositoryMock
            .Setup(x => x.GetPlotIdByDeviceAsync(It.IsAny<string>()))
            .ReturnsAsync((Guid?)null);

        var weatherStationJson = @"{
            ""device_id"": ""unknown-device"",
            ""telemetry"": {
                ""temperature_air"": 28.5,
                ""humidity_air"": 62.0,
                ""pressure"": 1013.2,
                ""precipitation_mm"": 12.4,
                ""wind_speed_kmh"": 5.2,
                ""soil_moisture_1"": 45.2
            }
        }";

        var dto = new ReceiveIoTDataDto
        {
            DeviceType = IoTDeviceType.WeatherStationNode,
            RawData = weatherStationJson
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Dispositivo não encontrado");
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithWeatherStationHumidityOutOfRange_ShouldFail()
    {
        // Arrange
        var weatherStationJson = @"{
            ""device_id"": ""agri-sensor-node-042"",
            ""telemetry"": {
                ""temperature_air"": 28.5,
                ""humidity_air"": 150,
                ""pressure"": 1013.2,
                ""precipitation_mm"": 12.4,
                ""wind_speed_kmh"": 5.2,
                ""soil_moisture_1"": 45.2
            }
        }";

        var dto = new ReceiveIoTDataDto
        {
            DeviceType = IoTDeviceType.WeatherStationNode,
            RawData = weatherStationJson
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithWeatherStationMissingTelemetry_ShouldFail()
    {
        // Arrange
        var weatherStationJson = @"{
            ""device_id"": ""agri-sensor-node-042""
        }";

        var dto = new ReceiveIoTDataDto
        {
            DeviceType = IoTDeviceType.WeatherStationNode,
            RawData = weatherStationJson
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Never);
    }

    #endregion

    #region Common Validation Tests

    [Fact]
    public async Task ExecuteAsync_WithNullDto_ShouldFail()
    {
        // Act
        var result = await _receiveIoTData.ExecuteAsync(null);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inválidos");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyRawData_ShouldFail()
    {
        // Arrange
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = string.Empty
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("obrigatórios");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJson_ShouldFail()
    {
        // Arrange
        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = "{ invalid json"
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        _iotDataRepositoryMock.Verify(x => x.AddAsync(It.IsAny<IoTData>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithRepositoryFailure_ShouldFail()
    {
        // Arrange
        _iotDataRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<IoTData>()))
            .ReturnsAsync(false);

        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = "{\"value\": 25.5, \"unit\": \"C\"}"
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("persistir");
    }

    #endregion

    #region Device Lookup Tests

    [Fact]
    public async Task ExecuteAsync_WithDeviceIdLookup_ShouldFindPlotAndSucceed()
    {
        // Arrange
        _deviceRepositoryMock
            .Setup(x => x.GetPlotIdByDeviceAsync(_testDeviceId))
            .ReturnsAsync(_testPlotId);

        var dto = new ReceiveIoTDataDto
        {
            DeviceId = _testDeviceId,
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = "{\"value\": 22.3, \"unit\": \"C\"}"
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.PlotId.Should().Be(_testPlotId);
        _deviceRepositoryMock.Verify(x => x.GetPlotIdByDeviceAsync(_testDeviceId), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithPlotIdProvidedAndDeviceIdInJson_ShouldUsePlotId()
    {
        // Arrange - PlotId in DTO takes precedence over deviceId
        _deviceRepositoryMock
            .Setup(x => x.GetPlotIdByDeviceAsync(It.IsAny<string>()))
            .ReturnsAsync(Guid.NewGuid());

        var dto = new ReceiveIoTDataDto
        {
            PlotId = _testPlotId,
            DeviceId = "some-device",
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = "{\"value\": 22.3, \"unit\": \"C\"}"
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.PlotId.Should().Be(_testPlotId);
        _deviceRepositoryMock.Verify(x => x.GetPlotIdByDeviceAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutPlotIdAndWithoutDeviceId_ShouldFail()
    {
        // Arrange
        var dto = new ReceiveIoTDataDto
        {
            DeviceType = IoTDeviceType.TemperatureSensor,
            RawData = "{\"value\": 22.3, \"unit\": \"C\"}"
        };

        // Act
        var result = await _receiveIoTData.ExecuteAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion
}
