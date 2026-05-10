// -----------------------------------------------------------------------
// <copyright file="SagaStepTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;

namespace Compendium.Abstractions.Tests.Sagas.Common;

public class SagaStepTests
{
    [Fact]
    public void SagaStep_DefaultConstruction_HasEmptyDefaults()
    {
        // Arrange / Act
        var step = new SagaStep();

        // Assert
        step.Id.Should().Be(Guid.Empty);
        step.Name.Should().Be(string.Empty);
        step.Status.Should().Be(SagaStepStatus.Pending);
        step.ExecutedAt.Should().BeNull();
        step.CompensatedAt.Should().BeNull();
        step.ErrorMessage.Should().BeNull();
        step.Order.Should().Be(0);
    }

    [Fact]
    public void SagaStep_InitAllProperties_PersistsValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var executed = DateTime.Parse("2026-05-10T12:00:00Z").ToUniversalTime();
        var compensated = DateTime.Parse("2026-05-10T13:00:00Z").ToUniversalTime();

        // Act
        var step = new SagaStep
        {
            Id = id,
            Name = "ReserveInventory",
            Status = SagaStepStatus.Completed,
            ExecutedAt = executed,
            CompensatedAt = compensated,
            ErrorMessage = "boom",
            Order = 3,
        };

        // Assert
        step.Id.Should().Be(id);
        step.Name.Should().Be("ReserveInventory");
        step.Status.Should().Be(SagaStepStatus.Completed);
        step.ExecutedAt.Should().Be(executed);
        step.CompensatedAt.Should().Be(compensated);
        step.ErrorMessage.Should().Be("boom");
        step.Order.Should().Be(3);
    }

    [Theory]
    [InlineData(SagaStepStatus.Pending)]
    [InlineData(SagaStepStatus.Executing)]
    [InlineData(SagaStepStatus.Completed)]
    [InlineData(SagaStepStatus.Failed)]
    [InlineData(SagaStepStatus.Compensating)]
    [InlineData(SagaStepStatus.Compensated)]
    public void SagaStep_AcceptsAllStatuses(SagaStepStatus status)
    {
        // Arrange / Act
        var step = new SagaStep { Status = status };

        // Assert
        step.Status.Should().Be(status);
    }

    [Fact]
    public void SagaStep_NullableTimestamps_AcceptNull()
    {
        // Arrange / Act
        var step = new SagaStep
        {
            ExecutedAt = null,
            CompensatedAt = null,
            ErrorMessage = null,
        };

        // Assert
        step.ExecutedAt.Should().BeNull();
        step.CompensatedAt.Should().BeNull();
        step.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SagaStep_OrderSupportsLargePositiveValues()
    {
        // Arrange / Act
        var step = new SagaStep { Order = int.MaxValue };

        // Assert
        step.Order.Should().Be(int.MaxValue);
    }
}
