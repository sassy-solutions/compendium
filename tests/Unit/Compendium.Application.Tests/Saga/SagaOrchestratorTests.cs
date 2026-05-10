// -----------------------------------------------------------------------
// <copyright file="SagaOrchestratorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS0618 // SagaOrchestrator and friends are obsolete; we still cover them.

using Compendium.Application.Saga;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Application.Tests.Saga;

/// <summary>
/// Unit tests for the obsolete <see cref="SagaOrchestrator"/> class.
/// </summary>
public class SagaOrchestratorTests
{
    public sealed class FakeSagaData
    {
        public string Name { get; init; } = string.Empty;
    }

    public sealed class FakeSaga : ISaga<FakeSagaData>
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public SagaStatus Status { get; init; } = SagaStatus.NotStarted;

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; init; }

        public IReadOnlyList<SagaStep> Steps { get; init; } = Array.Empty<SagaStep>();

        public FakeSagaData Data { get; init; } = new();
    }

    [Fact]
    public void Constructor_WhenAnyArgIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = Substitute.For<ISagaRepository>();
        var exec = Substitute.For<ISagaStepExecutor>();
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act / Assert
        ((Action)(() => new SagaOrchestrator(null!, exec, sp))).Should().Throw<ArgumentNullException>().WithParameterName("repository");
        ((Action)(() => new SagaOrchestrator(repo, null!, sp))).Should().Throw<ArgumentNullException>().WithParameterName("stepExecutor");
        ((Action)(() => new SagaOrchestrator(repo, exec, null!))).Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public async Task StartSagaAsync_WhenFactoryNotRegistered_ReturnsNotFoundFailure()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();
        var orchestrator = new SagaOrchestrator(
            Substitute.For<ISagaRepository>(),
            Substitute.For<ISagaStepExecutor>(),
            sp);

        // Act
        var result = await orchestrator.StartSagaAsync<FakeSaga, FakeSagaData>(
            new FakeSagaData { Name = "x" },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SagaFactory.NotFound");
    }

    [Fact]
    public async Task StartSagaAsync_WhenFactoryReturnsSaga_PersistsAndReturnsId()
    {
        // Arrange
        var saga = new FakeSaga();
        var repo = Substitute.For<ISagaRepository>();
        var factory = Substitute.For<ISagaFactory<FakeSaga, FakeSagaData>>();
        factory.Create(Arg.Any<FakeSagaData>()).Returns(saga);

        var sp = new ServiceCollection()
            .AddSingleton(factory)
            .BuildServiceProvider();

        var orchestrator = new SagaOrchestrator(repo, Substitute.For<ISagaStepExecutor>(), sp);

        // Act
        var result = await orchestrator.StartSagaAsync<FakeSaga, FakeSagaData>(
            new FakeSagaData(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(saga.Id);
        await repo.Received(1).SaveAsync(saga, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartSagaAsync_WhenFactoryThrows_ReturnsFailure()
    {
        // Arrange
        var factory = Substitute.For<ISagaFactory<FakeSaga, FakeSagaData>>();
        factory.Create(Arg.Any<FakeSagaData>()).Returns(_ => throw new InvalidOperationException("boom"));

        var sp = new ServiceCollection().AddSingleton(factory).BuildServiceProvider();
        var orchestrator = new SagaOrchestrator(
            Substitute.For<ISagaRepository>(),
            Substitute.For<ISagaStepExecutor>(),
            sp);

        // Act
        var result = await orchestrator.StartSagaAsync<FakeSaga, FakeSagaData>(
            new FakeSagaData(),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Saga.StartFailed");
        // Reflection wraps inner exceptions in TargetInvocationException; the obsolete
        // SagaOrchestrator surfaces the wrapper's message, not the inner one.
        result.Error.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenSagaNotFound_ReturnsFailure()
    {
        // Arrange
        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<ISaga>(Error.NotFound("Saga.NotFound", "missing"))));

        var orchestrator = new SagaOrchestrator(
            repo,
            Substitute.For<ISagaStepExecutor>(),
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.ExecuteStepAsync(Guid.NewGuid(), "step", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Saga.NotFound");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenStepNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var saga = new FakeSaga { Steps = new[] { new SagaStep { Id = Guid.NewGuid(), Name = "other", Status = SagaStepStatus.Pending } } };
        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var orchestrator = new SagaOrchestrator(
            repo,
            Substitute.For<ISagaStepExecutor>(),
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.ExecuteStepAsync(saga.Id, "missing", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Step.NotFound");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenStepNotPending_ReturnsConflict()
    {
        // Arrange
        var saga = new FakeSaga { Steps = new[] { new SagaStep { Id = Guid.NewGuid(), Name = "s1", Status = SagaStepStatus.Completed } } };
        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var orchestrator = new SagaOrchestrator(
            repo,
            Substitute.For<ISagaStepExecutor>(),
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.ExecuteStepAsync(saga.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Step.InvalidStatus");
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenExecutionSucceeds_PersistsCompletedStatus()
    {
        // Arrange
        var step = new SagaStep { Id = Guid.NewGuid(), Name = "s1", Status = SagaStepStatus.Pending };
        var saga = new FakeSaga { Steps = new[] { step } };
        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var executor = Substitute.For<ISagaStepExecutor>();
        executor.ExecuteAsync(saga, step, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var orchestrator = new SagaOrchestrator(
            repo,
            executor,
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.ExecuteStepAsync(saga.Id, "s1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateStepStatusAsync(saga.Id, step.Id, SagaStepStatus.Completed, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenExecutionFails_PersistsFailureAndReturnsExecutionError()
    {
        // Arrange
        var step = new SagaStep { Id = Guid.NewGuid(), Name = "s1", Status = SagaStepStatus.Pending };
        var saga = new FakeSaga { Steps = new[] { step } };
        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var executor = Substitute.For<ISagaStepExecutor>();
        executor.ExecuteAsync(saga, step, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Step.Bad", "broke"))));

        var orchestrator = new SagaOrchestrator(
            repo,
            executor,
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.ExecuteStepAsync(saga.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Step.Bad");
        await repo.Received(1).UpdateStepStatusAsync(saga.Id, step.Id, SagaStepStatus.Failed, "broke", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenExecutorThrows_ReturnsExecutionFailure()
    {
        // Arrange
        var step = new SagaStep { Id = Guid.NewGuid(), Name = "s1", Status = SagaStepStatus.Pending };
        var saga = new FakeSaga { Steps = new[] { step } };
        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var executor = Substitute.For<ISagaStepExecutor>();
        executor.ExecuteAsync(saga, step, Arg.Any<CancellationToken>())
            .Returns<Task<Result>>(_ => throw new InvalidOperationException("crash"));

        var orchestrator = new SagaOrchestrator(
            repo,
            executor,
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.ExecuteStepAsync(saga.Id, "s1", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Saga.ExecutionFailed");
    }

    [Fact]
    public async Task CompensateAsync_WhenSagaNotFound_ReturnsFailure()
    {
        // Arrange
        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<ISaga>(Error.NotFound("X", "y"))));

        var orchestrator = new SagaOrchestrator(
            repo,
            Substitute.For<ISagaStepExecutor>(),
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.CompensateAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CompensateAsync_WhenAllStepsCompensate_MarksSagaCompensated()
    {
        // Arrange
        var step1 = new SagaStep { Id = Guid.NewGuid(), Name = "s1", Status = SagaStepStatus.Completed, Order = 1 };
        var step2 = new SagaStep { Id = Guid.NewGuid(), Name = "s2", Status = SagaStepStatus.Completed, Order = 2 };
        var saga = new FakeSaga { Steps = new[] { step1, step2 } };

        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var executor = Substitute.For<ISagaStepExecutor>();
        executor.CompensateAsync(saga, Arg.Any<SagaStep>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var orchestrator = new SagaOrchestrator(
            repo,
            executor,
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.CompensateAsync(saga.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateSagaStatusAsync(saga.Id, SagaStatus.Compensating, Arg.Any<CancellationToken>());
        await repo.Received(1).UpdateSagaStatusAsync(saga.Id, SagaStatus.Compensated, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_WhenStepCompensationFails_ReturnsFailureAndPersistsFailedStep()
    {
        // Arrange
        var step = new SagaStep { Id = Guid.NewGuid(), Name = "s1", Status = SagaStepStatus.Completed };
        var saga = new FakeSaga { Steps = new[] { step } };

        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var executor = Substitute.For<ISagaStepExecutor>();
        executor.CompensateAsync(saga, step, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Comp.Bad", "no"))));

        var orchestrator = new SagaOrchestrator(
            repo,
            executor,
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.CompensateAsync(saga.Id, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comp.Bad");
        await repo.Received(1).UpdateStepStatusAsync(saga.Id, step.Id, SagaStepStatus.Failed, "no", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_WhenExecutorThrows_ReturnsCompensationFailure()
    {
        // Arrange
        var step = new SagaStep { Id = Guid.NewGuid(), Name = "s1", Status = SagaStepStatus.Completed };
        var saga = new FakeSaga { Steps = new[] { step } };

        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var executor = Substitute.For<ISagaStepExecutor>();
        executor.CompensateAsync(saga, step, Arg.Any<CancellationToken>())
            .Returns<Task<Result>>(_ => throw new InvalidOperationException("crashed"));

        var orchestrator = new SagaOrchestrator(
            repo,
            executor,
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.CompensateAsync(saga.Id, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Saga.CompensationFailed");
    }

    [Fact]
    public async Task GetSagaAsync_DelegatesToRepository()
    {
        // Arrange
        var saga = new FakeSaga();
        var repo = Substitute.For<ISagaRepository>();
        repo.GetByIdAsync(saga.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<ISaga>(saga)));

        var orchestrator = new SagaOrchestrator(
            repo,
            Substitute.For<ISagaStepExecutor>(),
            new ServiceCollection().BuildServiceProvider());

        // Act
        var result = await orchestrator.GetSagaAsync(saga.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(saga);
    }
}

#pragma warning restore CS0618
