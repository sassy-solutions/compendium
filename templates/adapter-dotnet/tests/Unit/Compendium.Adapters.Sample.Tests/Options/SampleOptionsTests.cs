// -----------------------------------------------------------------------
// <copyright file="SampleOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Compendium.Adapters.Sample.Options;

namespace Compendium.Adapters.Sample.Tests.Options;

/// <summary>
/// Demonstrates the convention every adapter test follows :
/// <list type="bullet">
///   <item>file copyright header</item>
///   <item>class named <c>{SUT}Tests</c></item>
///   <item>method named <c>{Method}_{Scenario}_{Expected}</c></item>
///   <item>explicit <c>// Arrange / // Act / // Assert</c> comments</item>
///   <item>FluentAssertions only — never <c>Assert.*</c></item>
/// </list>
/// </summary>
public class SampleOptionsTests
{
    [Fact]
    public void SampleOptions_Defaults_AreSensible()
    {
        // Arrange / Act
        var options = new SampleOptions();

        // Assert
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.BaseUrl.Should().BeEmpty();
        options.ApiKey.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "key", false)]
    [InlineData("   ", "key", false)]
    [InlineData("not-a-url", "key", false)]
    [InlineData("https://api.example.com", "", false)]
    [InlineData("https://api.example.com", "valid-key", true)]
    public void SampleOptions_DataAnnotations_ValidateAsExpected(
        string baseUrl,
        string apiKey,
        bool expectedValid)
    {
        // Arrange
        var options = new SampleOptions { BaseUrl = baseUrl, ApiKey = apiKey };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var actual = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        // Assert
        actual.Should().Be(expectedValid);
    }

    [Fact]
    public void SampleOptions_SectionName_IsCanonical()
    {
        // Assert
        SampleOptions.SectionName.Should().Be("Compendium:Adapters:Sample");
    }
}
