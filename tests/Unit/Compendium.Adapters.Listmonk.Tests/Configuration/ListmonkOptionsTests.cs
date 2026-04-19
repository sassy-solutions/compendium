// -----------------------------------------------------------------------
// <copyright file="ListmonkOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Listmonk.Configuration;
using FluentAssertions;

namespace Compendium.Adapters.Listmonk.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="ListmonkOptions"/>.
/// </summary>
public class ListmonkOptionsTests
{
    [Fact]
    public void ListmonkOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ListmonkOptions();

        // Assert
        options.BaseUrl.Should().BeEmpty();
        options.Username.Should().BeEmpty();
        options.Password.Should().BeEmpty();
        options.DefaultFromEmail.Should().BeEmpty();
        options.DefaultFromName.Should().BeEmpty();
        options.DefaultListId.Should().BeNull();
        options.TimeoutSeconds.Should().Be(30);
        options.MaxRetries.Should().Be(3);
        options.SkipSslValidation.Should().BeFalse();
    }

    [Fact]
    public void ListmonkOptions_WithCustomValues_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new ListmonkOptions
        {
            BaseUrl = "https://listmonk.example.com",
            Username = "admin",
            Password = "secret-password",
            DefaultFromEmail = "noreply@example.com",
            DefaultFromName = "Example Corp",
            DefaultListId = 42,
            TimeoutSeconds = 60,
            MaxRetries = 5,
            SkipSslValidation = true
        };

        // Assert
        options.BaseUrl.Should().Be("https://listmonk.example.com");
        options.Username.Should().Be("admin");
        options.Password.Should().Be("secret-password");
        options.DefaultFromEmail.Should().Be("noreply@example.com");
        options.DefaultFromName.Should().Be("Example Corp");
        options.DefaultListId.Should().Be(42);
        options.TimeoutSeconds.Should().Be(60);
        options.MaxRetries.Should().Be(5);
        options.SkipSslValidation.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(120)]
    public void ListmonkOptions_TimeoutSeconds_AcceptsValidValues(int timeout)
    {
        // Arrange & Act
        var options = new ListmonkOptions { TimeoutSeconds = timeout };

        // Assert
        options.TimeoutSeconds.Should().Be(timeout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void ListmonkOptions_MaxRetries_AcceptsValidValues(int retries)
    {
        // Arrange & Act
        var options = new ListmonkOptions { MaxRetries = retries };

        // Assert
        options.MaxRetries.Should().Be(retries);
    }

    [Fact]
    public void ListmonkOptions_DefaultListId_CanBeNullOrSet()
    {
        // Arrange
        var optionsWithNull = new ListmonkOptions();
        var optionsWithValue = new ListmonkOptions { DefaultListId = 1 };

        // Assert
        optionsWithNull.DefaultListId.Should().BeNull();
        optionsWithValue.DefaultListId.Should().Be(1);
    }
}
