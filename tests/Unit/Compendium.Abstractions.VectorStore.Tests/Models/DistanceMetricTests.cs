// -----------------------------------------------------------------------
// <copyright file="DistanceMetricTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Tests.Models;

public class DistanceMetricTests
{
    [Theory]
    [InlineData(DistanceMetric.Cosine, 0)]
    [InlineData(DistanceMetric.L2, 1)]
    [InlineData(DistanceMetric.InnerProduct, 2)]
    public void DistanceMetric_HasStableNumericValues(DistanceMetric metric, int expected)
    {
        // Act / Assert
        ((int)metric).Should().Be(expected);
    }

    [Fact]
    public void DistanceMetric_HasExactlyThreeMembers()
    {
        // Act
        var values = Enum.GetValues<DistanceMetric>();

        // Assert
        values.Should().HaveCount(3);
        values.Should().BeEquivalentTo(new[] { DistanceMetric.Cosine, DistanceMetric.L2, DistanceMetric.InnerProduct });
    }
}
