// -----------------------------------------------------------------------
// <copyright file="SagaStatusTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;

namespace Compendium.Abstractions.Tests.Sagas.Common;

public class SagaStatusTests
{
    [Theory]
    [InlineData(SagaStatus.NotStarted, "NotStarted")]
    [InlineData(SagaStatus.InProgress, "InProgress")]
    [InlineData(SagaStatus.Completed, "Completed")]
    [InlineData(SagaStatus.Failed, "Failed")]
    [InlineData(SagaStatus.Compensating, "Compensating")]
    [InlineData(SagaStatus.Compensated, "Compensated")]
    public void SagaStatus_ToString_RoundTripsThroughEnumName(SagaStatus status, string expectedName)
    {
        // Act
        var rendered = status.ToString();

        // Assert
        rendered.Should().Be(expectedName);
        Enum.TryParse<SagaStatus>(rendered, out var parsed).Should().BeTrue();
        parsed.Should().Be(status);
    }

    [Fact]
    public void SagaStatus_DeclaresExactlySixMembers()
    {
        // Arrange / Act
        var values = Enum.GetValues<SagaStatus>();

        // Assert — locking the contract; new states must be added consciously
        values.Should().HaveCount(6);
        values.Should().Contain(new[]
        {
            SagaStatus.NotStarted,
            SagaStatus.InProgress,
            SagaStatus.Completed,
            SagaStatus.Failed,
            SagaStatus.Compensating,
            SagaStatus.Compensated,
        });
    }

    [Fact]
    public void SagaStatus_DefaultValue_IsNotStarted()
    {
        // Arrange / Act
        var defaultStatus = default(SagaStatus);

        // Assert — every newly-constructed saga should start in NotStarted
        defaultStatus.Should().Be(SagaStatus.NotStarted);
    }
}
