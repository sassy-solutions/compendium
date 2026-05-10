// -----------------------------------------------------------------------
// <copyright file="RowLevelSecurityExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Security;
using FluentAssertions;
using Npgsql;

namespace Compendium.Adapters.PostgreSQL.Tests.Security;

/// <summary>
/// Unit tests for <see cref="RowLevelSecurityExtensions"/>.
/// Only the pure-validation surface is unit-tested here; the methods that
/// execute SQL via an open <see cref="NpgsqlConnection"/> are covered by
/// the integration suite.
/// </summary>
public class RowLevelSecurityExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidTenantId_WhenNullOrWhitespace_ReturnsFalse(string? tenantId)
    {
        // Arrange / Act
        var actual = RowLevelSecurityExtensions.IsValidTenantId(tenantId);

        // Assert
        actual.Should().BeFalse();
    }

    [Theory]
    [InlineData("tenant-1")]
    [InlineData("tenant_42")]
    [InlineData("Tenant-ABC-123")]
    [InlineData("a")]
    [InlineData("0123456789")]
    public void IsValidTenantId_WhenWellFormed_ReturnsTrue(string tenantId)
    {
        // Arrange / Act
        var actual = RowLevelSecurityExtensions.IsValidTenantId(tenantId);

        // Assert
        actual.Should().BeTrue();
    }

    [Theory]
    [InlineData("tenant 1")] // space
    [InlineData("tenant;DROP TABLE")] // SQL injection attempt
    [InlineData("tenant'--")] // single quote
    [InlineData("tenant/with/slash")]
    [InlineData("tenant.dot")] // dot is not allowed in tenant id
    [InlineData("tenant@domain")]
    public void IsValidTenantId_WhenContainsForbiddenCharacters_ReturnsFalse(string tenantId)
    {
        // Arrange / Act
        var actual = RowLevelSecurityExtensions.IsValidTenantId(tenantId);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void IsValidTenantId_WhenLengthExceeds255_ReturnsFalse()
    {
        // Arrange
        var tenantId = new string('a', 256);

        // Act
        var actual = RowLevelSecurityExtensions.IsValidTenantId(tenantId);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void IsValidTenantId_WhenLengthExactly255_ReturnsTrue()
    {
        // Arrange
        var tenantId = new string('a', 255);

        // Act
        var actual = RowLevelSecurityExtensions.IsValidTenantId(tenantId);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void CreateTenantFilter_WhenTenantIdValid_ReturnsParameterizedFragment()
    {
        // Arrange
        const string tenantId = "tenant-1";

        // Act
        var filter = RowLevelSecurityExtensions.CreateTenantFilter(tenantId);

        // Assert
        filter.Should().Be("AND (@TenantId IS NULL OR tenant_id = @TenantId)");
    }

    [Fact]
    public void CreateTenantFilter_WhenTenantIdNullOrWhitespace_ReturnsDefaultFilter()
    {
        // Arrange / Act
        var nullFilter = RowLevelSecurityExtensions.CreateTenantFilter(null);
        var emptyFilter = RowLevelSecurityExtensions.CreateTenantFilter("");
        var whitespaceFilter = RowLevelSecurityExtensions.CreateTenantFilter("   ");

        // Assert
        nullFilter.Should().Be("AND (@TenantId IS NULL OR tenant_id = @TenantId)");
        emptyFilter.Should().Be("AND (@TenantId IS NULL OR tenant_id = @TenantId)");
        whitespaceFilter.Should().Be("AND (@TenantId IS NULL OR tenant_id = @TenantId)");
    }

    [Fact]
    public void CreateTenantFilter_WhenCustomColumnName_UsesProvidedColumn()
    {
        // Arrange
        const string tenantId = "tenant-2";

        // Act
        var filter = RowLevelSecurityExtensions.CreateTenantFilter(tenantId, "events.tenant_id");

        // Assert
        filter.Should().Be("AND (@TenantId IS NULL OR events.tenant_id = @TenantId)");
    }

    [Theory]
    [InlineData("tenant; DROP TABLE")]
    [InlineData("'; --")]
    [InlineData("tenant.with.spaces and bad")]
    public void CreateTenantFilter_WhenTenantIdInvalid_ThrowsArgumentException(string tenantId)
    {
        // Arrange / Act
        Action act = () => RowLevelSecurityExtensions.CreateTenantFilter(tenantId);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Invalid tenant ID format*");
    }

    [Theory]
    [InlineData("tenant id")] // space
    [InlineData("tenant;id")]
    [InlineData("tenant'name")]
    [InlineData("tenant\"name")]
    public void CreateTenantFilter_WhenColumnNameInvalid_ThrowsArgumentException(string columnName)
    {
        // Arrange / Act
        Action act = () => RowLevelSecurityExtensions.CreateTenantFilter("tenant-1", columnName);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Invalid column name*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateTenantFilter_WhenColumnNameNullOrWhitespace_ThrowsArgumentException(string? columnName)
    {
        // Arrange / Act
        Action act = () => RowLevelSecurityExtensions.CreateTenantFilter("tenant-1", columnName!);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Invalid column name*");
    }

    [Fact]
    public async Task SetTenantContextAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        // Arrange
        NpgsqlConnection? connection = null;

        // Act
        Func<Task> act = async () => await RowLevelSecurityExtensions.SetTenantContextAsync(
            connection!,
            "tenant-1",
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetTenantContextAsync_WhenTenantIdEmpty_ReturnsValidationFailure(string? tenantId)
    {
        // Arrange — use an unopened NpgsqlConnection. The method exits before it touches it.
        using var connection = new NpgsqlConnection("Host=localhost");

        // Act
        var result = await RowLevelSecurityExtensions.SetTenantContextAsync(
            connection,
            tenantId!,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RLS.InvalidTenantId");
    }

    [Theory]
    [InlineData("bad tenant")] // space
    [InlineData("tenant';--")]
    [InlineData("tenant/with/slashes")]
    public async Task SetTenantContextAsync_WhenTenantIdInvalidFormat_ReturnsValidationFailure(string tenantId)
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost");

        // Act
        var result = await RowLevelSecurityExtensions.SetTenantContextAsync(
            connection,
            tenantId,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RLS.InvalidTenantIdFormat");
        result.Error.Message.Should().Contain(tenantId);
    }

    [Fact]
    public async Task GetTenantContextAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        // Arrange
        NpgsqlConnection? connection = null;

        // Act
        Func<Task> act = async () => await RowLevelSecurityExtensions.GetTenantContextAsync(
            connection!,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ClearTenantContextAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        // Arrange
        NpgsqlConnection? connection = null;

        // Act
        Func<Task> act = async () => await RowLevelSecurityExtensions.ClearTenantContextAsync(
            connection!,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetTenantContextAsync_WhenConnectionNotOpened_ReturnsFailureFromCatchBranch()
    {
        // Arrange — A closed connection produces an InvalidOperationException, exercising
        // the generic exception handler in the method.
        using var connection = new NpgsqlConnection("Host=localhost");

        // Act
        var result = await RowLevelSecurityExtensions.GetTenantContextAsync(
            connection,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RLS.GetContextFailed");
    }

    [Fact]
    public async Task ClearTenantContextAsync_WhenConnectionNotOpened_ReturnsFailureFromCatchBranch()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost");

        // Act
        var result = await RowLevelSecurityExtensions.ClearTenantContextAsync(
            connection,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RLS.ClearContextFailed");
    }

    [Fact]
    public async Task SetTenantContextAsync_WhenConnectionNotOpened_ReturnsFailureFromCatchBranch()
    {
        // Arrange
        using var connection = new NpgsqlConnection("Host=localhost");

        // Act
        var result = await RowLevelSecurityExtensions.SetTenantContextAsync(
            connection,
            "tenant-1",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RLS.SetContextFailed");
    }
}
