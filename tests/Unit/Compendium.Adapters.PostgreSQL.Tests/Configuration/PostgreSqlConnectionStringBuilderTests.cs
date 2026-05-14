// -----------------------------------------------------------------------
// <copyright file="PostgreSqlConnectionStringBuilderTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using FluentAssertions;
using Npgsql;

namespace Compendium.Adapters.PostgreSQL.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="PostgreSqlConnectionStringBuilder"/>.
/// </summary>
public class PostgreSqlConnectionStringBuilderTests
{
    private const string SampleConnectionString = "Host=localhost;Username=u;Password=p;Database=d";

    [Fact]
    public void BuildConnectionString_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        Action act = () => PostgreSqlConnectionStringBuilder.BuildConnectionString(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildConnectionString_WhenConnectionStringMissing_ThrowsArgumentException(string? connectionString)
    {
        // Arrange
        var options = new PostgreSqlOptions { ConnectionString = connectionString! };

        // Act
        Action act = () => PostgreSqlConnectionStringBuilder.BuildConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*ConnectionString*");
    }

    [Fact]
    public void BuildConnectionString_WhenValidOptions_AppliesPoolingParameters()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            EnablePooling = true,
            MinimumPoolSize = 10,
            MaximumPoolSize = 50,
            ConnectionIdleLifetime = 120,
            ConnectionLifetime = 600,
            ConnectionTimeout = 15,
            CommandTimeout = 45,
            Keepalive = 20,
        };

        // Act
        var connectionString = PostgreSqlConnectionStringBuilder.BuildConnectionString(options);

        // Assert
        var parsed = new NpgsqlConnectionStringBuilder(connectionString);
        parsed.Pooling.Should().BeTrue();
        parsed.MinPoolSize.Should().Be(10);
        parsed.MaxPoolSize.Should().Be(50);
        parsed.ConnectionIdleLifetime.Should().Be(120);
        parsed.ConnectionLifetime.Should().Be(600);
        parsed.Timeout.Should().Be(15);
        parsed.CommandTimeout.Should().Be(45);
        parsed.KeepAlive.Should().Be(20);
    }

    [Fact]
    public void BuildConnectionString_WhenKeepaliveIsZero_DoesNotApplyKeepalive()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            Keepalive = 0,
        };

        // Act
        var connectionString = PostgreSqlConnectionStringBuilder.BuildConnectionString(options);

        // Assert
        var parsed = new NpgsqlConnectionStringBuilder(connectionString);
        parsed.KeepAlive.Should().Be(0);
    }

    [Fact]
    public void BuildConnectionString_WhenPoolingDisabled_SetsPoolingFalse()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            EnablePooling = false,
        };

        // Act
        var connectionString = PostgreSqlConnectionStringBuilder.BuildConnectionString(options);

        // Assert
        var parsed = new NpgsqlConnectionStringBuilder(connectionString);
        parsed.Pooling.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenOptionsIsNull_ReturnsFailure()
    {
        // Arrange / Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(null!);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("PostgreSqlOptions cannot be null");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenConnectionStringMissing_ReturnsFailure(string? connectionString)
    {
        // Arrange
        var options = new PostgreSqlOptions { ConnectionString = connectionString! };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("ConnectionString is required");
    }

    [Fact]
    public void Validate_WhenMinimumPoolSizeNegative_ReturnsFailure()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            MinimumPoolSize = -1,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("MinimumPoolSize cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WhenMaximumPoolSizeNotPositive_ReturnsFailure(int maximumPoolSize)
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            MaximumPoolSize = maximumPoolSize,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("MaximumPoolSize must be greater than 0");
    }

    [Fact]
    public void Validate_WhenMinimumGreaterThanMaximum_ReturnsFailure()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            MinimumPoolSize = 100,
            MaximumPoolSize = 50,
            MaxPoolSize = 100,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("MinimumPoolSize cannot be greater than MaximumPoolSize");
    }

    [Fact]
    public void Validate_WhenAppMaxLessThanNpgsqlMax_ReturnsFailure()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            MinimumPoolSize = 10,
            MaximumPoolSize = 200,
            MaxPoolSize = 100, // less than MaximumPoolSize
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("Application MaxPoolSize");
        errorMessage.Should().Contain("Npgsql MaximumPoolSize");
    }

    [Fact]
    public void Validate_WhenConnectionIdleLifetimeNegative_ReturnsFailure()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            ConnectionIdleLifetime = -1,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("ConnectionIdleLifetime cannot be negative");
    }

    [Fact]
    public void Validate_WhenConnectionLifetimeNegative_ReturnsFailure()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            ConnectionLifetime = -10,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("ConnectionLifetime cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenConnectionTimeoutNotPositive_ReturnsFailure(int connectionTimeout)
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            ConnectionTimeout = connectionTimeout,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("ConnectionTimeout must be greater than 0");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenCommandTimeoutNotPositive_ReturnsFailure(int commandTimeout)
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            CommandTimeout = commandTimeout,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("CommandTimeout must be greater than 0");
    }

    [Fact]
    public void Validate_WhenKeepaliveNegative_ReturnsFailure()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
            Keepalive = -1,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Keepalive cannot be negative");
    }

    [Fact]
    public void Validate_WhenConnectionStringInvalid_ReturnsFailure()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            // Use a string that NpgsqlConnectionStringBuilder cannot parse
            ConnectionString = "this_is_not_a_valid_connection_string!!!",
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().StartWith("Invalid connection string:");
    }

    [Fact]
    public void Validate_WhenAllValid_ReturnsSuccess()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = SampleConnectionString,
        };

        // Act
        var (isValid, errorMessage) = PostgreSqlConnectionStringBuilder.Validate(options);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void GetRecommendedProductionOptions_ReturnsExpectedDefaults()
    {
        // Arrange / Act
        var options = PostgreSqlConnectionStringBuilder.GetRecommendedProductionOptions();

        // Assert
        options.MaxPoolSize.Should().Be(200);
        options.CommandTimeout.Should().Be(60);
        options.MinimumPoolSize.Should().Be(50);
        options.MaximumPoolSize.Should().Be(200);
        options.ConnectionIdleLifetime.Should().Be(900);
        options.ConnectionLifetime.Should().Be(3600);
        options.ConnectionTimeout.Should().Be(30);
        options.Keepalive.Should().Be(30);
        options.EnablePooling.Should().BeTrue();
        options.AutoCreateSchema.Should().BeFalse();
        options.BatchSize.Should().Be(1000);
    }

    [Fact]
    public void GetConservativeOptions_ReturnsExpectedDefaults()
    {
        // Arrange / Act
        var options = PostgreSqlConnectionStringBuilder.GetConservativeOptions();

        // Assert
        options.MaxPoolSize.Should().Be(100);
        options.CommandTimeout.Should().Be(60);
        options.MinimumPoolSize.Should().Be(25);
        options.MaximumPoolSize.Should().Be(100);
        options.ConnectionIdleLifetime.Should().Be(600);
        options.ConnectionLifetime.Should().Be(1800);
        options.ConnectionTimeout.Should().Be(30);
        options.Keepalive.Should().Be(60);
        options.EnablePooling.Should().BeTrue();
        options.AutoCreateSchema.Should().BeFalse();
        options.BatchSize.Should().Be(500);
    }
}
