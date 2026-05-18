// -----------------------------------------------------------------------
// <copyright file="PresignedActionTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Tests.Models;

public class PresignedActionTests
{
    [Fact]
    public void PresignedAction_Get_HasValueZero()
    {
        // Arrange
        var action = PresignedAction.Get;

        // Act
        var value = (int)action;

        // Assert
        value.Should().Be(0);
    }

    [Fact]
    public void PresignedAction_Put_HasValueOne()
    {
        // Arrange
        var action = PresignedAction.Put;

        // Act
        var value = (int)action;

        // Assert
        value.Should().Be(1);
    }

    [Theory]
    [InlineData(PresignedAction.Get, "Get")]
    [InlineData(PresignedAction.Put, "Put")]
    public void PresignedAction_ToString_ReturnsName(PresignedAction action, string expected)
    {
        // Act
        var actual = action.ToString();

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void PresignedAction_DefinedValues_AreExactlyGetAndPut()
    {
        // Act
        var values = Enum.GetValues<PresignedAction>();

        // Assert
        values.Should().BeEquivalentTo(new[] { PresignedAction.Get, PresignedAction.Put });
    }
}
