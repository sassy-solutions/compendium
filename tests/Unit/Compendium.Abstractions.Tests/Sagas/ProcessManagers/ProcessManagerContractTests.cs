// -----------------------------------------------------------------------
// <copyright file="ProcessManagerContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;

namespace Compendium.Abstractions.Tests.Sagas.ProcessManagers;

public class ProcessManagerContractTests
{
    public sealed class FakeState
    {
        public string? Marker { get; init; }
    }

    [Fact]
    public void IProcessManager_DerivesFromISagaInstance()
    {
        // Arrange / Act / Assert
        typeof(ISagaInstance).IsAssignableFrom(typeof(IProcessManager)).Should().BeTrue();
    }

    [Fact]
    public void IProcessManager_Substitute_ExposesStepsAndSagaProperties()
    {
        // Arrange
        var pm = Substitute.For<IProcessManager>();
        var steps = new List<SagaStep>
        {
            new() { Id = Guid.NewGuid(), Name = "step-1", Order = 1 },
            new() { Id = Guid.NewGuid(), Name = "step-2", Order = 2 },
        };
        pm.Steps.Returns(steps);
        pm.Status.Returns(SagaStatus.InProgress);

        // Act / Assert
        pm.Steps.Should().HaveCount(2);
        pm.Steps[0].Name.Should().Be("step-1");
        pm.Status.Should().Be(SagaStatus.InProgress);
    }

    [Fact]
    public void IProcessManagerOfTState_Substitute_ExposesStronglyTypedState()
    {
        // Arrange
        var pm = Substitute.For<IProcessManager<FakeState>>();
        var state = new FakeState { Marker = "alpha" };
        pm.State.Returns(state);
        pm.Steps.Returns(Array.Empty<SagaStep>());
        pm.Status.Returns(SagaStatus.NotStarted);

        // Act / Assert
        pm.State.Should().BeSameAs(state);
        pm.State.Marker.Should().Be("alpha");
    }

    [Fact]
    public async Task IProcessManagerOrchestrator_Substitute_StartAsync_ReturnsId()
    {
        // Arrange
        var orch = Substitute.For<IProcessManagerOrchestrator>();
        var pm = Substitute.For<IProcessManager>();
        var id = Guid.NewGuid();
        orch.StartAsync(pm, Arg.Any<CancellationToken>()).Returns(Result.Success(id));

        // Act
        var result = await orch.StartAsync(pm, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
    }

    [Fact]
    public async Task IProcessManagerOrchestrator_Substitute_ExecuteStepAsync_ReturnsConfiguredResult()
    {
        // Arrange
        var orch = Substitute.For<IProcessManagerOrchestrator>();
        var pmId = Guid.NewGuid();
        orch.ExecuteStepAsync(pmId, "step-1", Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await orch.ExecuteStepAsync(pmId, "step-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IProcessManagerOrchestrator_Substitute_CompensateAsync_ReturnsConfiguredResult()
    {
        // Arrange
        var orch = Substitute.For<IProcessManagerOrchestrator>();
        var pmId = Guid.NewGuid();
        orch.CompensateAsync(pmId, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await orch.CompensateAsync(pmId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IProcessManagerOrchestrator_Substitute_GetAsync_ReturnsConfiguredProcessManager()
    {
        // Arrange
        var orch = Substitute.For<IProcessManagerOrchestrator>();
        var pmId = Guid.NewGuid();
        var pm = Substitute.For<IProcessManager>();
        orch.GetAsync(pmId, Arg.Any<CancellationToken>()).Returns(Result.Success(pm));

        // Act
        var result = await orch.GetAsync(pmId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(pm);
    }

    [Fact]
    public async Task IProcessManagerRepository_Substitute_GetByIdAsync_ReturnsProcessManager()
    {
        // Arrange
        var repo = Substitute.For<IProcessManagerRepository>();
        var id = Guid.NewGuid();
        var pm = Substitute.For<IProcessManager>();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Success(pm));

        // Act
        var result = await repo.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(pm);
    }

    [Fact]
    public async Task IProcessManagerRepository_Substitute_GetByIdAsyncOfTState_ReturnsTypedProcessManager()
    {
        // Arrange
        var repo = Substitute.For<IProcessManagerRepository>();
        var id = Guid.NewGuid();
        var pm = Substitute.For<IProcessManager<FakeState>>();
        repo.GetByIdAsync<FakeState>(id, Arg.Any<CancellationToken>()).Returns(Result.Success(pm));

        // Act
        var result = await repo.GetByIdAsync<FakeState>(id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(pm);
    }

    [Fact]
    public async Task IProcessManagerRepository_Substitute_SaveAsync_ReturnsSuccess()
    {
        // Arrange
        var repo = Substitute.For<IProcessManagerRepository>();
        var pm = Substitute.For<IProcessManager>();
        repo.SaveAsync(pm, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await repo.SaveAsync(pm, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IProcessManagerRepository_Substitute_UpdateStatusAsync_ReturnsSuccess()
    {
        // Arrange
        var repo = Substitute.For<IProcessManagerRepository>();
        var id = Guid.NewGuid();
        repo.UpdateStatusAsync(id, SagaStatus.Completed, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await repo.UpdateStatusAsync(id, SagaStatus.Completed, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateStatusAsync(id, SagaStatus.Completed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IProcessManagerRepository_Substitute_UpdateStepStatusAsync_PropagatesAllArguments()
    {
        // Arrange
        var repo = Substitute.For<IProcessManagerRepository>();
        var pmId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        repo.UpdateStepStatusAsync(pmId, stepId, SagaStepStatus.Failed, "boom", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await repo.UpdateStepStatusAsync(pmId, stepId, SagaStepStatus.Failed, "boom", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateStepStatusAsync(pmId, stepId, SagaStepStatus.Failed, "boom", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IProcessManagerRepository_Substitute_UpdateStepStatusAsync_AcceptsNullErrorMessage()
    {
        // Arrange
        var repo = Substitute.For<IProcessManagerRepository>();
        var pmId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        repo.UpdateStepStatusAsync(pmId, stepId, SagaStepStatus.Completed, null, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await repo.UpdateStepStatusAsync(pmId, stepId, SagaStepStatus.Completed, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IProcessManagerStepExecutor_Substitute_ExecuteAsync_ReturnsConfiguredResult()
    {
        // Arrange
        var executor = Substitute.For<IProcessManagerStepExecutor>();
        var pm = Substitute.For<IProcessManager>();
        var step = new SagaStep { Id = Guid.NewGuid(), Name = "step-1" };
        executor.ExecuteAsync(pm, step, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await executor.ExecuteAsync(pm, step, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IProcessManagerStepExecutor_Substitute_CompensateAsync_PropagatesFailure()
    {
        // Arrange
        var executor = Substitute.For<IProcessManagerStepExecutor>();
        var pm = Substitute.For<IProcessManager>();
        var step = new SagaStep { Id = Guid.NewGuid(), Name = "step-1" };
        var error = Error.Failure("step.compensation_failed", "boom");
        executor.CompensateAsync(pm, step, Arg.Any<CancellationToken>()).Returns(Result.Failure(error));

        // Act
        var result = await executor.CompensateAsync(pm, step, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("step.compensation_failed");
    }
}
