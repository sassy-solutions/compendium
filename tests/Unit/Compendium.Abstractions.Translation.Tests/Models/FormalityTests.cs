// -----------------------------------------------------------------------
// <copyright file="FormalityTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation.Tests.Models;

public class FormalityTests
{
    [Fact]
    public void Formality_Default_HasValueZero()
    {
        // Act / Assert
        ((int)Formality.Default).Should().Be(0);
    }

    [Theory]
    [InlineData(Formality.Default, 0)]
    [InlineData(Formality.More, 1)]
    [InlineData(Formality.Less, 2)]
    [InlineData(Formality.PreferMore, 3)]
    [InlineData(Formality.PreferLess, 4)]
    public void Formality_AllMembers_HaveStableOrdinals(Formality value, int expected)
    {
        // Act / Assert
        ((int)value).Should().Be(expected);
    }

    [Fact]
    public void Formality_ExposesExpectedMembers()
    {
        // Act
        var names = Enum.GetNames<Formality>();

        // Assert
        names.Should().BeEquivalentTo(new[]
        {
            nameof(Formality.Default),
            nameof(Formality.More),
            nameof(Formality.Less),
            nameof(Formality.PreferMore),
            nameof(Formality.PreferLess),
        });
    }
}
