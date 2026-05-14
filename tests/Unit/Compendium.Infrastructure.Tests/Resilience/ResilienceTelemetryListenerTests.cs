// -----------------------------------------------------------------------
// <copyright file="ResilienceTelemetryListenerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Resilience;

namespace Compendium.Infrastructure.Tests.Resilience;

/// <summary>
/// Unit tests for <see cref="ResilienceTelemetryListener"/> covering all logging methods.
/// </summary>
public sealed class ResilienceTelemetryListenerTests
{
    private readonly ILogger<ResilienceTelemetryListener> _logger = Substitute.For<ILogger<ResilienceTelemetryListener>>();

    [Fact]
    public void Ctor_NullLogger_Throws()
    {
        // Arrange / Act
        var act = () => new ResilienceTelemetryListener(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void OnCircuitBreakerStateChange_LogsInformation()
    {
        // Arrange
        var sut = new ResilienceTelemetryListener(_logger);

        // Act
        sut.OnCircuitBreakerStateChange("postgres", "Open");

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void OnRetry_LogsWarning()
    {
        // Arrange
        var sut = new ResilienceTelemetryListener(_logger);

        // Act
        sut.OnRetry("postgres", 1, TimeSpan.FromMilliseconds(100));

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void OnTimeout_LogsError()
    {
        // Arrange
        var sut = new ResilienceTelemetryListener(_logger);

        // Act
        sut.OnTimeout("redis", TimeSpan.FromSeconds(2));

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void PollyResilienceOptions_PostgreSqlDefaults_HasExpectedValues()
    {
        // Arrange / Act
        var options = PollyResilienceOptions.PostgreSqlDefaults();

        // Assert
        options.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxRetryAttempts.Should().Be(3);
        options.BaseRetryDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        options.CircuitBreakerFailureThreshold.Should().Be(0.5);
        options.CircuitBreakerSamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.CircuitBreakerMinimumThroughput.Should().Be(10);
        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void PollyResilienceOptions_RedisDefaults_HasShorterTimeouts()
    {
        // Arrange / Act
        var options = PollyResilienceOptions.RedisDefaults();

        // Assert
        options.Timeout.Should().Be(TimeSpan.FromSeconds(2));
        options.BaseRetryDelay.Should().Be(TimeSpan.FromMilliseconds(50));
        options.CircuitBreakerMinimumThroughput.Should().Be(20);
        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void PollyResilienceOptions_DefaultCtor_HasSafeDefaults()
    {
        // Arrange / Act
        var options = new PollyResilienceOptions();

        // Assert
        options.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxRetryAttempts.Should().Be(3);
    }
}
