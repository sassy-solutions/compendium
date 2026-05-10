// -----------------------------------------------------------------------
// <copyright file="RedisOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Redis.Configuration;
using FluentAssertions;

namespace Compendium.Adapters.Redis.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="RedisOptions"/>.
/// </summary>
public class RedisOptionsTests
{
    [Fact]
    public void RedisOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new RedisOptions();

        // Assert
        options.ConnectionString.Should().Be("localhost:6379");
        options.DefaultDatabase.Should().Be(0);
        options.ConnectTimeout.Should().Be(5000);
        options.CommandTimeout.Should().Be(5000);
        options.RetryCount.Should().Be(3);
        options.RetryDelayMs.Should().Be(1000);
        options.ValidateConnectionString.Should().BeTrue();
        options.KeyPrefix.Should().Be("compendium");
    }

    [Fact]
    public void RedisOptions_SectionName_IsCompendiumRedis()
    {
        // Arrange & Act
        var sectionName = RedisOptions.SectionName;

        // Assert
        sectionName.Should().Be("Compendium:Redis");
    }

    [Fact]
    public void RedisOptions_AllProperties_AreMutable()
    {
        // Arrange
        var options = new RedisOptions();

        // Act
        options.ConnectionString = "redis.example.com:6380";
        options.DefaultDatabase = 5;
        options.ConnectTimeout = 10_000;
        options.CommandTimeout = 7_500;
        options.RetryCount = 7;
        options.RetryDelayMs = 250;
        options.ValidateConnectionString = false;
        options.KeyPrefix = "myapp";

        // Assert
        options.ConnectionString.Should().Be("redis.example.com:6380");
        options.DefaultDatabase.Should().Be(5);
        options.ConnectTimeout.Should().Be(10_000);
        options.CommandTimeout.Should().Be(7_500);
        options.RetryCount.Should().Be(7);
        options.RetryDelayMs.Should().Be(250);
        options.ValidateConnectionString.Should().BeFalse();
        options.KeyPrefix.Should().Be("myapp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("custom-prefix")]
    [InlineData("a:b:c")]
    public void RedisOptions_KeyPrefix_AcceptsAnyString(string prefix)
    {
        // Arrange
        var options = new RedisOptions();

        // Act
        options.KeyPrefix = prefix;

        // Assert
        options.KeyPrefix.Should().Be(prefix);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(-1)]
    public void RedisOptions_DefaultDatabase_AcceptsAnyInt(int db)
    {
        // Arrange
        var options = new RedisOptions();

        // Act
        options.DefaultDatabase = db;

        // Assert
        options.DefaultDatabase.Should().Be(db);
    }
}
