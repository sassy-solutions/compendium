// -----------------------------------------------------------------------
// <copyright file="PostgresProcessManagerRepositoryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.Sagas;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Adapters.PostgreSQL.Tests.Sagas;

/// <summary>
/// Unit tests for the constructor and validation surface of <see cref="PostgresProcessManagerRepository"/>.
/// CRUD against a real PostgreSQL instance is covered in
/// <c>tests/Integration/Compendium.IntegrationTests/EndToEnd/Scenarios/PostgresProcessManagerStateReloadTests.cs</c>.
/// </summary>
public class PostgresProcessManagerRepositoryTests
{
    private const string ValidConnectionString = "Host=localhost;Database=db;Username=u;Password=p";

    private static IOptions<PostgreSqlOptions> Options(PostgreSqlOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);

    [Fact]
    public void Ctor_WhenOptionsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        Action act = () => _ = new PostgresProcessManagerRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenOptionsValueNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var stub = Substitute.For<IOptions<PostgreSqlOptions>>();
        stub.Value.Returns((PostgreSqlOptions?)null);

        // Act
        Action act = () => _ = new PostgresProcessManagerRepository(stub);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*connection string*");
    }

    [Fact]
    public void Ctor_WhenValidArguments_DoesNotThrow()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });

        // Act
        Action act = () => _ = new PostgresProcessManagerRepository(options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SaveAsync_WhenProcessManagerNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.SaveAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenDatabaseUnavailable_ReturnsLoadFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProcessManager.LoadFailed");
    }

    [Fact]
    public async Task GetByIdAsync_Generic_WhenDatabaseUnavailable_ReturnsLoadFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetByIdAsync<DummyState>(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProcessManager.LoadFailed");
    }

    [Fact]
    public async Task SaveAsync_WhenDatabaseUnavailable_ReturnsSaveFailure()
    {
        // Arrange
        var sut = CreateSut();
        var pm = CreateFakeProcessManager();

        // Act
        var result = await sut.SaveAsync(pm, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProcessManager.SaveFailed");
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenDatabaseUnavailable_ReturnsUpdateStatusFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStatusAsync(Guid.NewGuid(), SagaStatus.Completed, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProcessManager.UpdateStatusFailed");
    }

    [Fact]
    public async Task UpdateStepStatusAsync_WhenDatabaseUnavailable_ReturnsUpdateStepStatusFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStepStatusAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SagaStepStatus.Completed,
            errorMessage: null,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProcessManager.UpdateStepStatusFailed");
    }

    [Fact]
    public async Task InitializeAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.InitializeAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    private static PostgresProcessManagerRepository CreateSut()
    {
        var options = Options(new PostgreSqlOptions
        {
            // Connect to a closed port so calls fail fast in the catch branch.
            ConnectionString = "Host=127.0.0.1;Port=1;Database=db;Username=u;Password=p;Timeout=1;Command Timeout=1",
        });
        return new PostgresProcessManagerRepository(
            options,
            tenantContext: null,
            logger: Substitute.For<ILogger<PostgresProcessManagerRepository>>());
    }

    private static IProcessManager CreateFakeProcessManager()
    {
        var pm = Substitute.For<IProcessManager>();
        pm.Id.Returns(Guid.NewGuid());
        pm.Status.Returns(SagaStatus.NotStarted);
        pm.CreatedAt.Returns(DateTime.UtcNow);
        pm.CompletedAt.Returns((DateTime?)null);
        pm.Steps.Returns(new List<SagaStep>().AsReadOnly());
        return pm;
    }

    /// <summary>State stub used to exercise the generic <see cref="IProcessManagerRepository.GetByIdAsync{TState}"/> overload.</summary>
    public sealed class DummyState
    {
        public string Value { get; set; } = string.Empty;
    }
}
