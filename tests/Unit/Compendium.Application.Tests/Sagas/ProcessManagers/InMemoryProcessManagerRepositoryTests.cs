// -----------------------------------------------------------------------
// <copyright file="InMemoryProcessManagerRepositoryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Application.Sagas.ProcessManagers;

namespace Compendium.Application.Tests.Sagas.ProcessManagers;

/// <summary>
/// Unit tests for the <see cref="InMemoryProcessManagerRepository"/> class.
/// </summary>
public class InMemoryProcessManagerRepositoryTests
{
    public sealed class FakeState
    {
    }

    public sealed class OtherState
    {
    }

    private sealed class FakeProcessManager : ProcessManager<FakeState>
    {
        public FakeProcessManager(Guid id)
            : base(id, new FakeState(), new[] { "s1", "s2" })
        {
        }
    }

    [Fact]
    public async Task SaveAsync_WhenProcessManagerIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();

        // Act
        var act = async () => await repo.SaveAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("processManager");
    }

    [Fact]
    public async Task SaveAsync_StoresProcessManager()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();
        var pm = new FakeProcessManager(Guid.NewGuid());

        // Act
        var save = await repo.SaveAsync(pm, CancellationToken.None);
        var get = await repo.GetByIdAsync(pm.Id, CancellationToken.None);

        // Assert
        save.IsSuccess.Should().BeTrue();
        get.IsSuccess.Should().BeTrue();
        get.Value.Should().BeSameAs(pm);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProcessManager.NotFound");
    }

    [Fact]
    public async Task GetByIdAsyncTState_WhenNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();

        // Act
        var result = await repo.GetByIdAsync<FakeState>(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProcessManager.NotFound");
    }

    [Fact]
    public async Task GetByIdAsyncTState_WhenStateMatches_ReturnsTypedProcessManager()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();
        var pm = new FakeProcessManager(Guid.NewGuid());
        await repo.SaveAsync(pm, CancellationToken.None);

        // Act
        var result = await repo.GetByIdAsync<FakeState>(pm.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(pm.Id);
    }

    [Fact]
    public async Task GetByIdAsyncTState_WhenStateMismatch_ReturnsConflictFailure()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();
        var pm = new FakeProcessManager(Guid.NewGuid());
        await repo.SaveAsync(pm, CancellationToken.None);

        // Act
        var result = await repo.GetByIdAsync<OtherState>(pm.Id, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProcessManager.StateTypeMismatch");
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();

        // Act
        var result = await repo.UpdateStatusAsync(Guid.NewGuid(), SagaStatus.Completed, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProcessManager.NotFound");
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenFound_TransitionsStatus()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();
        var pm = new FakeProcessManager(Guid.NewGuid());
        await repo.SaveAsync(pm, CancellationToken.None);

        // Act
        var result = await repo.UpdateStatusAsync(pm.Id, SagaStatus.Completed, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pm.Status.Should().Be(SagaStatus.Completed);
    }

    [Fact]
    public async Task UpdateStepStatusAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();

        // Act
        var result = await repo.UpdateStepStatusAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SagaStepStatus.Completed,
            errorMessage: null,
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProcessManager.NotFound");
    }

    [Fact]
    public async Task UpdateStepStatusAsync_WhenFound_UpdatesStep()
    {
        // Arrange
        var repo = new InMemoryProcessManagerRepository();
        var pm = new FakeProcessManager(Guid.NewGuid());
        await repo.SaveAsync(pm, CancellationToken.None);
        var stepId = pm.Steps[0].Id;

        // Act
        var result = await repo.UpdateStepStatusAsync(
            pm.Id,
            stepId,
            SagaStepStatus.Completed,
            errorMessage: null,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pm.Steps[0].Status.Should().Be(SagaStepStatus.Completed);
    }
}
