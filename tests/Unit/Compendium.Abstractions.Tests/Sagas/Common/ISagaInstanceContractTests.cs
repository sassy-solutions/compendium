// -----------------------------------------------------------------------
// <copyright file="ISagaInstanceContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;

namespace Compendium.Abstractions.Tests.Sagas.Common;

public class ISagaInstanceContractTests
{
    [Fact]
    public void ISagaInstance_Substitute_ExposesAllRequiredProperties()
    {
        // Arrange
        var saga = Substitute.For<ISagaInstance>();
        var id = Guid.NewGuid();
        var createdAt = DateTime.Parse("2026-05-10T00:00:00Z").ToUniversalTime();
        var completedAt = DateTime.Parse("2026-05-10T01:00:00Z").ToUniversalTime();
        saga.Id.Returns(id);
        saga.Status.Returns(SagaStatus.Completed);
        saga.CreatedAt.Returns(createdAt);
        saga.CompletedAt.Returns(completedAt);

        // Act / Assert
        saga.Id.Should().Be(id);
        saga.Status.Should().Be(SagaStatus.Completed);
        saga.CreatedAt.Should().Be(createdAt);
        saga.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void ISagaInstance_Substitute_CompletedAt_CanBeNullForInProgressSaga()
    {
        // Arrange
        var saga = Substitute.For<ISagaInstance>();
        saga.Status.Returns(SagaStatus.InProgress);
        saga.CompletedAt.Returns((DateTime?)null);

        // Act / Assert
        saga.Status.Should().Be(SagaStatus.InProgress);
        saga.CompletedAt.Should().BeNull();
    }
}
