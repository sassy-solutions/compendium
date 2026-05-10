// -----------------------------------------------------------------------
// <copyright file="ProcessManagerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Application.Sagas.ProcessManagers;

namespace Compendium.Application.Tests.Sagas.ProcessManagers;

/// <summary>
/// Unit tests for the <see cref="ProcessManager{TState}"/> base class.
/// </summary>
public class ProcessManagerTests
{
    public sealed class FakeState
    {
        public string Order { get; init; } = string.Empty;
    }

    private sealed class FakeProcessManager : ProcessManager<FakeState>
    {
        public FakeProcessManager(Guid id, FakeState state, IEnumerable<string> stepNames)
            : base(id, state, stepNames)
        {
        }
    }

    [Fact]
    public void Constructor_WhenStateIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new FakeProcessManager(Guid.NewGuid(), null!, new[] { "a" });

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("state");
    }

    [Fact]
    public void Constructor_WhenStepNamesIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new FakeProcessManager(Guid.NewGuid(), new FakeState(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("stepNames");
    }

    [Fact]
    public void Constructor_WhenNoSteps_ThrowsArgumentException()
    {
        // Arrange / Act
        var act = () => new FakeProcessManager(Guid.NewGuid(), new FakeState(), Array.Empty<string>());

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("stepNames");
    }

    [Fact]
    public void Constructor_AssignsStepsInOrderAndPendingStatus()
    {
        // Arrange / Act
        var pm = new FakeProcessManager(Guid.NewGuid(), new FakeState(), new[] { "a", "b", "c" });

        // Assert
        pm.Steps.Should().HaveCount(3);
        pm.Steps[0].Name.Should().Be("a");
        pm.Steps[0].Order.Should().Be(1);
        pm.Steps[1].Name.Should().Be("b");
        pm.Steps[1].Order.Should().Be(2);
        pm.Steps[2].Name.Should().Be("c");
        pm.Steps[2].Order.Should().Be(3);
        pm.Steps.Should().OnlyContain(s => s.Status == SagaStepStatus.Pending);
        pm.Status.Should().Be(SagaStatus.NotStarted);
        pm.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void TransitionTo_Completed_SetsCompletedAt()
    {
        // Arrange
        var pm = new FakeProcessManager(Guid.NewGuid(), new FakeState(), new[] { "s1" });
        var before = DateTime.UtcNow;

        // Act
        pm.TransitionTo(SagaStatus.Completed);

        // Assert
        pm.Status.Should().Be(SagaStatus.Completed);
        pm.CompletedAt.Should().NotBeNull();
        pm.CompletedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void TransitionTo_Compensated_SetsCompletedAt()
    {
        // Arrange
        var pm = new FakeProcessManager(Guid.NewGuid(), new FakeState(), new[] { "s1" });

        // Act
        pm.TransitionTo(SagaStatus.Compensated);

        // Assert
        pm.Status.Should().Be(SagaStatus.Compensated);
        pm.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void TransitionTo_OtherStatus_DoesNotSetCompletedAt()
    {
        // Arrange
        var pm = new FakeProcessManager(Guid.NewGuid(), new FakeState(), new[] { "s1" });

        // Act
        pm.TransitionTo(SagaStatus.InProgress);

        // Assert
        pm.Status.Should().Be(SagaStatus.InProgress);
        pm.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void TransitionStep_WhenStepIdUnknown_NoOp()
    {
        // Arrange
        var pm = new FakeProcessManager(Guid.NewGuid(), new FakeState(), new[] { "s1" });
        var snapshot = pm.Steps[0];

        // Act
        pm.TransitionStep(Guid.NewGuid(), SagaStepStatus.Completed);

        // Assert
        pm.Steps[0].Should().BeEquivalentTo(snapshot);
    }

    [Fact]
    public void TransitionStep_ToCompleted_SetsExecutedAt()
    {
        // Arrange
        var pm = new FakeProcessManager(Guid.NewGuid(), new FakeState(), new[] { "s1" });
        var stepId = pm.Steps[0].Id;

        // Act
        pm.TransitionStep(stepId, SagaStepStatus.Completed);

        // Assert
        pm.Steps[0].Status.Should().Be(SagaStepStatus.Completed);
        pm.Steps[0].ExecutedAt.Should().NotBeNull();
        pm.Steps[0].CompensatedAt.Should().BeNull();
    }

    [Fact]
    public void TransitionStep_ToCompensated_SetsCompensatedAt()
    {
        // Arrange
        var pm = new FakeProcessManager(Guid.NewGuid(), new FakeState(), new[] { "s1" });
        var stepId = pm.Steps[0].Id;

        // Act
        pm.TransitionStep(stepId, SagaStepStatus.Compensated);

        // Assert
        pm.Steps[0].Status.Should().Be(SagaStepStatus.Compensated);
        pm.Steps[0].CompensatedAt.Should().NotBeNull();
    }

    [Fact]
    public void TransitionStep_ToFailed_StoresErrorMessage()
    {
        // Arrange
        var pm = new FakeProcessManager(Guid.NewGuid(), new FakeState(), new[] { "s1" });
        var stepId = pm.Steps[0].Id;

        // Act
        pm.TransitionStep(stepId, SagaStepStatus.Failed, "boom");

        // Assert
        pm.Steps[0].Status.Should().Be(SagaStepStatus.Failed);
        pm.Steps[0].ErrorMessage.Should().Be("boom");
    }
}
