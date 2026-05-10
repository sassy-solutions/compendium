// -----------------------------------------------------------------------
// <copyright file="PostgreSqlOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using FluentAssertions;

namespace Compendium.Adapters.PostgreSQL.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="PostgreSqlOptions"/>.
/// </summary>
public class PostgreSqlOptionsTests
{
    [Fact]
    public void PostgreSqlOptions_Defaults_AreSetToProductionValues()
    {
        // Arrange / Act
        var options = new PostgreSqlOptions();

        // Assert
        options.ConnectionString.Should().BeEmpty();
        options.MaxPoolSize.Should().Be(200);
        options.CommandTimeout.Should().Be(60);
        options.TableName.Should().Be("event_store");
        options.AutoCreateSchema.Should().BeFalse();
        options.BatchSize.Should().Be(1000);
        options.MinimumPoolSize.Should().Be(50);
        options.MaximumPoolSize.Should().Be(200);
        options.ConnectionIdleLifetime.Should().Be(900);
        options.ConnectionLifetime.Should().Be(3600);
        options.ConnectionTimeout.Should().Be(30);
        options.Keepalive.Should().Be(30);
        options.EnablePooling.Should().BeTrue();
    }

    [Fact]
    public void PostgreSqlOptions_SectionName_HasExpectedValue()
    {
        // Arrange / Act
        var sectionName = PostgreSqlOptions.SectionName;

        // Assert
        sectionName.Should().Be("Compendium:EventStore");
    }

    [Fact]
    public void PostgreSqlOptions_SettersAndGetters_RoundTripValues()
    {
        // Arrange
        var options = new PostgreSqlOptions();

        // Act
        options.ConnectionString = "Host=localhost;Database=db;";
        options.MaxPoolSize = 500;
        options.CommandTimeout = 120;
        options.TableName = "events";
        options.AutoCreateSchema = true;
        options.BatchSize = 250;
        options.MinimumPoolSize = 10;
        options.MaximumPoolSize = 100;
        options.ConnectionIdleLifetime = 60;
        options.ConnectionLifetime = 600;
        options.ConnectionTimeout = 5;
        options.Keepalive = 0;
        options.EnablePooling = false;

        // Assert
        options.ConnectionString.Should().Be("Host=localhost;Database=db;");
        options.MaxPoolSize.Should().Be(500);
        options.CommandTimeout.Should().Be(120);
        options.TableName.Should().Be("events");
        options.AutoCreateSchema.Should().BeTrue();
        options.BatchSize.Should().Be(250);
        options.MinimumPoolSize.Should().Be(10);
        options.MaximumPoolSize.Should().Be(100);
        options.ConnectionIdleLifetime.Should().Be(60);
        options.ConnectionLifetime.Should().Be(600);
        options.ConnectionTimeout.Should().Be(5);
        options.Keepalive.Should().Be(0);
        options.EnablePooling.Should().BeFalse();
    }
}
