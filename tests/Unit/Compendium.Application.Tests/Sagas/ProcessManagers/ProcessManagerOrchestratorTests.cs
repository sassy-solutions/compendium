// -----------------------------------------------------------------------
// <copyright file="ProcessManagerOrchestratorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Application.Sagas.ProcessManagers;

namespace Compendium.Application.Tests.Sagas.ProcessManagers;

/// <summary>
/// Unit tests for the <see cref="ProcessManagerOrchestrator"/> class.
/// </summary>
public class ProcessManagerOrchestratorTests
{
    public sealed class FakeState
    {
    }

    private sealed class FakeProcessManager : ProcessManager<FakeState>
    {
        public FakeProcessManager(Guid id, IEnumerable<string> stepNames)
            : base(id, new FakeState(), stepNames)
        {
        }
    }

    [Fact]
    public void Constructor_WhenAnyArgIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = Substitute.For<IProcessManagerRepository>();
        var exec = Substitute.For<IProcessManagerStepExecutor>();

        // Act / Assert
        ((Action)(() => new ProcessManagerOrchestrator(null!, exec))).Should().Throw<ArgumentNullException>().WithParameterName("repository");
        ((Action)(() => new ProcessManagerOrchestrator(repo, null!))).Should().Throw<ArgumentNullException>().WithParameterName("stepExecutor");
    }

    [Fact]
    public async Task StartAsync_WhenProcessManagerIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var orch = CreateOrchestrator(out _, out _);

        // Act
        var act = async () => await orch.StartAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("processManager");
    }

    [Fact]
    public async Task StartAsync_WhenSaveSucceeds_ReturnsId()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        repo.SaveAsync(pm, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await orch.StartAsync(pm, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(pm.Id);
    }

    [Fact]
    public async Task StartAsync_WhenSaveFails_PropagatesFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        repo.SaveAsync(pm, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Persist.Bad", "fail"))));

        // Act
        var result = await orch.StartAsync(pm, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Persist.Bad");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ExecuteStepAsync_WhenStepNameMissing_ReturnsValidationFailure(string? stepName)
    {
        // Arrange
        var orch = CreateOrchestrator(out _, out _);

        // Act
        var result = await orch.ExecuteStepAsync(Guid.NewGuid(), stepName!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProcessManager.StepNameMissing");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenLookupFails_PropagatesFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IProcessManager>(Error.NotFound("X", "y"))));

        // Act
        var result = await orch.ExecuteStepAsync(Guid.NewGuid(), "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("X");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenStepNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "other" });
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProcessManager.StepNotFound");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenStepNotPending_ReturnsConflict()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        // Mark step as completed up-front
        pm.TransitionStep(pm.Steps[0].Id, SagaStepStatus.Completed);

        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProcessManager.StepInvalidStatus");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenInProgressMarkFails_ReturnsThatFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, SagaStatus.InProgress, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Persist.Down", "down"))));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Persist.Down");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenExecutionFails_MarksStepFailedAndReturnsExecError()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, SagaStatus.InProgress, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Failed, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        exec.ExecuteAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Step.Bad", "no"))));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Step.Bad");
        await repo.Received(1).UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Failed, "no", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenMarkFailedItselfFails_ReturnsPersistenceFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, SagaStatus.InProgress, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Failed, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Persist.Down", "no"))));
        exec.ExecuteAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Step.Bad", "no"))));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Persist.Down");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenAllStepsCompleteAfter_MarksSagaCompleted()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, Arg.Any<SagaStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), Arg.Any<SagaStepStatus>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                // Simulate the in-memory transition so the all-completed check can pass.
                var stepId = (Guid)call[1];
                var status = (SagaStepStatus)call[2];
                var errMsg = (string?)call[3];
                pm.TransitionStep(stepId, status, errMsg);
                return Task.FromResult(Result.Success());
            });
        exec.ExecuteAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "s1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateStatusAsync(pm.Id, SagaStatus.Completed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenMarkCompletedFails_ReturnsPersistenceFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1", "s2" });
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, SagaStatus.InProgress, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Completed, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Persist.Step.Down", "x"))));
        exec.ExecuteAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Persist.Step.Down");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenRefreshFailsAfterCompletion_PropagatesFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1", "s2" });

        var lookups = 0;
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                lookups++;
                return lookups == 1
                    ? Task.FromResult(Result.Success<IProcessManager>(pm))
                    : Task.FromResult(Result.Failure<IProcessManager>(Error.Failure("Refresh.Down", "x")));
            });
        repo.UpdateStatusAsync(pm.Id, SagaStatus.InProgress, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Completed, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        exec.ExecuteAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Refresh.Down");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenStepIsNotLastAndFinishCalled_DoesNotComplete()
    {
        // Arrange — two-step saga; complete one, the second is still pending → no auto-complete.
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1", "s2" });

        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, Arg.Any<SagaStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), Arg.Any<SagaStepStatus>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                pm.TransitionStep((Guid)call[1], (SagaStepStatus)call[2], (string?)call[3]);
                return Task.FromResult(Result.Success());
            });
        exec.ExecuteAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await orch.ExecuteStepAsync(pm.Id, "s1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await repo.DidNotReceive().UpdateStatusAsync(pm.Id, SagaStatus.Completed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_WhenLookupFails_PropagatesFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IProcessManager>(Error.NotFound("Pm.NotFound", "x"))));

        // Act
        var result = await orch.CompensateAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Pm.NotFound");
    }

    [Fact]
    public async Task CompensateAsync_WhenMarkCompensatingFails_ReturnsThatFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, SagaStatus.Compensating, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Persist.Bad", "no"))));

        // Act
        var result = await orch.CompensateAsync(pm.Id, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Persist.Bad");
    }

    [Fact]
    public async Task CompensateAsync_WhenAllCompensationsSucceed_MarksCompensated()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1", "s2" });
        // Mark both completed.
        pm.TransitionStep(pm.Steps[0].Id, SagaStepStatus.Completed);
        pm.TransitionStep(pm.Steps[1].Id, SagaStepStatus.Completed);

        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, Arg.Any<SagaStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Compensated, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        exec.CompensateAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await orch.CompensateAsync(pm.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateStatusAsync(pm.Id, SagaStatus.Compensated, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_WhenStepCompensationFails_ReturnsExecError()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        pm.TransitionStep(pm.Steps[0].Id, SagaStepStatus.Completed);

        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, SagaStatus.Compensating, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Failed, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        exec.CompensateAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Comp.Bad", "no"))));

        // Act
        var result = await orch.CompensateAsync(pm.Id, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comp.Bad");
    }

    [Fact]
    public async Task CompensateAsync_WhenMarkStepFailedItselfFails_ReturnsPersistenceFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        pm.TransitionStep(pm.Steps[0].Id, SagaStepStatus.Completed);

        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, SagaStatus.Compensating, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Failed, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Persist.Down", "down"))));
        exec.CompensateAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Comp.Bad", "no"))));

        // Act
        var result = await orch.CompensateAsync(pm.Id, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Persist.Down");
    }

    [Fact]
    public async Task CompensateAsync_WhenMarkStepCompensatedFails_ReturnsPersistenceFailure()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out var exec);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        pm.TransitionStep(pm.Steps[0].Id, SagaStepStatus.Completed);

        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));
        repo.UpdateStatusAsync(pm.Id, SagaStatus.Compensating, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        repo.UpdateStepStatusAsync(pm.Id, Arg.Any<Guid>(), SagaStepStatus.Compensated, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Persist.Down", "down"))));
        exec.CompensateAsync(pm, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await orch.CompensateAsync(pm.Id, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Persist.Down");
    }

    [Fact]
    public async Task GetAsync_DelegatesToRepository()
    {
        // Arrange
        var orch = CreateOrchestrator(out var repo, out _);
        var pm = new FakeProcessManager(Guid.NewGuid(), new[] { "s1" });
        repo.GetByIdAsync(pm.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IProcessManager>(pm)));

        // Act
        var result = await orch.GetAsync(pm.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(pm);
    }

    private static ProcessManagerOrchestrator CreateOrchestrator(
        out IProcessManagerRepository repo,
        out IProcessManagerStepExecutor exec)
    {
        repo = Substitute.For<IProcessManagerRepository>();
        exec = Substitute.For<IProcessManagerStepExecutor>();
        return new ProcessManagerOrchestrator(repo, exec);
    }
}
