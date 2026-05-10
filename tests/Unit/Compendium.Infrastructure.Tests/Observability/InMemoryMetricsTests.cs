// -----------------------------------------------------------------------
// <copyright file="InMemoryMetricsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Tests.Observability;

/// <summary>
/// Unit tests for <see cref="InMemoryMetrics"/> covering all metric kinds and validation.
/// </summary>
public sealed class InMemoryMetricsTests
{
    private readonly ILogger<InMemoryMetrics> _logger = Substitute.For<ILogger<InMemoryMetrics>>();
    private readonly InMemoryMetrics _sut;

    public InMemoryMetricsTests()
    {
        // Arrange
        _sut = new InMemoryMetrics(_logger);
    }

    [Fact]
    public void Ctor_WithNullLogger_Throws()
    {
        // Arrange / Act
        var act = () => new InMemoryMetrics(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void IncrementCounter_WithValidName_RecordsCounter()
    {
        // Arrange / Act
        _sut.IncrementCounter("hits", 1);
        _sut.IncrementCounter("hits", 2);

        // Assert
        var metrics = _sut.GetAllMetrics();
        metrics.Should().ContainKey("hits");
        metrics["hits"].Type.Should().Be(MetricType.Counter);
        metrics["hits"].Value.Should().Be(3);
    }

    [Fact]
    public void IncrementCounter_WithDifferentTags_KeepsSeparateEntries()
    {
        // Arrange / Act
        _sut.IncrementCounter("hits", 1, new KeyValuePair<string, object?>("k", "a"));
        _sut.IncrementCounter("hits", 1, new KeyValuePair<string, object?>("k", "b"));

        // Assert
        _sut.GetAllMetrics().Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IncrementCounter_WithEmptyName_Throws(string invalidName)
    {
        // Arrange / Act
        var act = () => _sut.IncrementCounter(invalidName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void RecordValue_WithValidName_RecordsGauge()
    {
        // Arrange / Act
        _sut.RecordValue("temp", 42.5);

        // Assert
        var metrics = _sut.GetAllMetrics();
        metrics["temp"].Type.Should().Be(MetricType.Gauge);
        metrics["temp"].Value.Should().Be(42.5);
    }

    [Fact]
    public void RecordValue_TwiceWithSameKey_OverridesValue()
    {
        // Arrange / Act
        _sut.RecordValue("g", 1);
        _sut.RecordValue("g", 7);

        // Assert
        _sut.GetAllMetrics()["g"].Value.Should().Be(7);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RecordValue_WithEmptyName_Throws(string invalid)
    {
        // Arrange / Act
        var act = () => _sut.RecordValue(invalid, 0);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordDuration_WithValidName_RecordsHistogram()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(250);

        // Act
        _sut.RecordDuration("op", duration);

        // Assert
        var metrics = _sut.GetAllMetrics();
        metrics["op"].Type.Should().Be(MetricType.Histogram);
        metrics["op"].Value.Should().Be(250);
    }

    [Fact]
    public void RecordDuration_TwoCalls_AveragesValues()
    {
        // Arrange / Act
        _sut.RecordDuration("op", TimeSpan.FromMilliseconds(100));
        _sut.RecordDuration("op", TimeSpan.FromMilliseconds(200));

        // Assert — implementation does running average
        _sut.GetAllMetrics()["op"].Value.Should().Be(150);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordDuration_WithEmptyName_Throws(string invalid)
    {
        // Arrange / Act
        var act = () => _sut.RecordDuration(invalid, TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartTimer_ReturnsDisposableThatRecordsDuration()
    {
        // Arrange / Act
        using (_sut.StartTimer("scoped-op"))
        {
            // some work
        }

        // Assert
        _sut.GetAllMetrics().Should().ContainKey("scoped-op");
        _sut.GetAllMetrics()["scoped-op"].Type.Should().Be(MetricType.Histogram);
    }

    [Fact]
    public void RecordEvent_RecordsCounterAndDuration()
    {
        // Arrange / Act
        _sut.RecordEvent("OrderCreated", "order-1", "Order", 12.5);

        // Assert
        var metrics = _sut.GetAllMetrics();
        metrics.Keys.Should().Contain(k => k.StartsWith("events.total"));
        metrics.Keys.Should().Contain(k => k.StartsWith("events.processing.duration"));
    }

    [Fact]
    public void RecordProjectionRebuild_RecordsCounterAndDuration()
    {
        // Arrange / Act
        _sut.RecordProjectionRebuild("OrderProjection", 100);

        // Assert
        var metrics = _sut.GetAllMetrics();
        metrics.Keys.Should().Contain(k => k.StartsWith("projections.rebuilds.total"));
        metrics.Keys.Should().Contain(k => k.StartsWith("projections.rebuild.duration"));
    }

    [Fact]
    public void RecordCircuitBreakerTrip_IncrementsCounter()
    {
        // Arrange / Act
        _sut.RecordCircuitBreakerTrip("redis");

        // Assert
        _sut.GetAllMetrics().Keys.Should().Contain(k => k.StartsWith("circuitbreaker.trips.total"));
    }

    [Fact]
    public void RecordEncryptionOperation_RecordsDuration()
    {
        // Arrange / Act
        _sut.RecordEncryptionOperation("encrypt", 5);

        // Assert
        _sut.GetAllMetrics().Keys.Should().Contain(k => k.StartsWith("encryption.operation.duration"));
    }

    [Fact]
    public void RecordConnectionSemaphoreWait_RecordsDuration()
    {
        // Arrange / Act
        _sut.RecordConnectionSemaphoreWait(15, "select");

        // Assert
        _sut.GetAllMetrics().Keys.Should().Contain(k => k.StartsWith("connection.semaphore.wait"));
    }

    [Fact]
    public void RecordConnectionAcquisition_RecordsDuration()
    {
        // Arrange / Act
        _sut.RecordConnectionAcquisition(2, "insert");

        // Assert
        _sut.GetAllMetrics().Keys.Should().Contain(k => k.StartsWith("connection.acquisition.duration"));
    }

    [Fact]
    public void RecordQueryExecution_RecordsDuration()
    {
        // Arrange / Act
        _sut.RecordQueryExecution("select", 7, "list_users");

        // Assert
        _sut.GetAllMetrics().Keys.Should().Contain(k => k.StartsWith("database.query.duration"));
    }

    [Fact]
    public void RecordActiveConnections_RecordsGauge()
    {
        // Arrange / Act
        _sut.RecordActiveConnections(8);

        // Assert
        _sut.GetAllMetrics().Should().ContainKey("connection.active");
        _sut.GetAllMetrics()["connection.active"].Value.Should().Be(8);
    }

    [Fact]
    public void RecordSemaphoreQueueLength_RecordsGauge()
    {
        // Arrange / Act
        _sut.RecordSemaphoreQueueLength(3);

        // Assert
        _sut.GetAllMetrics().Should().ContainKey("connection.semaphore.queue");
        _sut.GetAllMetrics()["connection.semaphore.queue"].Value.Should().Be(3);
    }

    [Fact]
    public void RecordConnectionError_IncrementsCounter()
    {
        // Arrange / Act
        _sut.RecordConnectionError("timeout", "select");

        // Assert
        _sut.GetAllMetrics().Keys.Should().Contain(k => k.StartsWith("connection.errors.total"));
    }

    [Fact]
    public void GetAllMetrics_ReturnsImmutableSnapshot()
    {
        // Arrange
        _sut.IncrementCounter("a");
        var snapshot1 = _sut.GetAllMetrics();

        // Act
        _sut.IncrementCounter("b");
        var snapshot2 = _sut.GetAllMetrics();

        // Assert
        snapshot1.Should().HaveCount(1);
        snapshot2.Should().HaveCount(2);
    }
}
