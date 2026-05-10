// -----------------------------------------------------------------------
// <copyright file="SagaStepStatusTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;

namespace Compendium.Abstractions.Tests.Sagas.Common;

public class SagaStepStatusTests
{
    [Theory]
    [InlineData(SagaStepStatus.Pending, "Pending")]
    [InlineData(SagaStepStatus.Executing, "Executing")]
    [InlineData(SagaStepStatus.Completed, "Completed")]
    [InlineData(SagaStepStatus.Failed, "Failed")]
    [InlineData(SagaStepStatus.Compensating, "Compensating")]
    [InlineData(SagaStepStatus.Compensated, "Compensated")]
    public void SagaStepStatus_ToString_RoundTripsThroughEnumName(SagaStepStatus status, string expectedName)
    {
        // Act
        var rendered = status.ToString();

        // Assert
        rendered.Should().Be(expectedName);
        Enum.TryParse<SagaStepStatus>(rendered, out var parsed).Should().BeTrue();
        parsed.Should().Be(status);
    }

    [Fact]
    public void SagaStepStatus_DeclaresExactlySixMembers()
    {
        // Arrange / Act
        var values = Enum.GetValues<SagaStepStatus>();

        // Assert
        values.Should().HaveCount(6);
        values.Should().Contain(new[]
        {
            SagaStepStatus.Pending,
            SagaStepStatus.Executing,
            SagaStepStatus.Completed,
            SagaStepStatus.Failed,
            SagaStepStatus.Compensating,
            SagaStepStatus.Compensated,
        });
    }

    [Fact]
    public void SagaStepStatus_DefaultValue_IsPending()
    {
        // Arrange / Act
        var defaultStatus = default(SagaStepStatus);

        // Assert — fresh steps must start in Pending
        defaultStatus.Should().Be(SagaStepStatus.Pending);
    }
}
