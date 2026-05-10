// -----------------------------------------------------------------------
// <copyright file="CorrelationIdTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Tests.Observability;

/// <summary>
/// Unit tests for <see cref="CorrelationIdProvider"/> and <see cref="CorrelationIdScope"/>.
/// Validates correlation ID lifecycle, async-local isolation, and scope semantics.
/// </summary>
public sealed class CorrelationIdTests
{
    [Fact]
    public void GetCorrelationId_WhenNoneSet_GeneratesNewCorrelationId()
    {
        // Arrange
        var provider = new CorrelationIdProvider();

        // Act
        var correlationId = provider.GetCorrelationId();

        // Assert
        correlationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public void GetCorrelationId_WhenAlreadySet_ReturnsSameValue()
    {
        // Arrange
        var provider = new CorrelationIdProvider();
        provider.SetCorrelationId("custom-id");

        // Act
        var first = provider.GetCorrelationId();
        var second = provider.GetCorrelationId();

        // Assert
        first.Should().Be("custom-id");
        second.Should().Be("custom-id");
    }

    [Fact]
    public void SetCorrelationId_WithValidId_StoresIt()
    {
        // Arrange
        var provider = new CorrelationIdProvider();

        // Act
        provider.SetCorrelationId("trace-123");

        // Assert
        provider.GetCorrelationId().Should().Be("trace-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetCorrelationId_WithInvalidId_ThrowsArgumentException(string? invalid)
    {
        // Arrange
        var provider = new CorrelationIdProvider();

        // Act
        var act = () => provider.SetCorrelationId(invalid!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Correlation ID cannot be null or empty*");
    }

    [Fact]
    public void GenerateCorrelationId_ProducesNewIdAndSetsItAsCurrent()
    {
        // Arrange
        var provider = new CorrelationIdProvider();

        // Act
        var generated = provider.GenerateCorrelationId();
        var current = provider.GetCorrelationId();

        // Assert
        generated.Should().NotBeNullOrEmpty();
        current.Should().Be(generated);
        Guid.TryParse(generated, out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateCorrelationId_TwoCalls_ProduceDifferentValues()
    {
        // Arrange
        var provider = new CorrelationIdProvider();

        // Act
        var first = provider.GenerateCorrelationId();
        var second = provider.GenerateCorrelationId();

        // Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public async Task CorrelationId_IsIsolatedAcrossAsyncFlows()
    {
        // Arrange
        var provider = new CorrelationIdProvider();
        provider.SetCorrelationId("outer");

        string? innerCorrelationId = null;

        // Act
        await Task.Run(() =>
        {
            provider.SetCorrelationId("inner");
            innerCorrelationId = provider.GetCorrelationId();
        });

        // Assert
        innerCorrelationId.Should().Be("inner");
        provider.GetCorrelationId().Should().Be("outer");
    }

    [Fact]
    public void CorrelationIdScope_Constructor_NullProvider_Throws()
    {
        // Arrange / Act
        var act = () => new CorrelationIdScope(null!, "id");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("provider");
    }

    [Fact]
    public void CorrelationIdScope_SetsCorrelationIdForScope()
    {
        // Arrange
        var provider = new CorrelationIdProvider();
        provider.SetCorrelationId("outer-id");

        // Act
        using (new CorrelationIdScope(provider, "scoped-id"))
        {
            provider.GetCorrelationId().Should().Be("scoped-id");
        }

        // Assert — restored after disposal
        provider.GetCorrelationId().Should().Be("outer-id");
    }

    [Fact]
    public void CorrelationIdScope_Dispose_RestoresPreviousId()
    {
        // Arrange
        var provider = new CorrelationIdProvider();
        provider.SetCorrelationId("first");
        var scope = new CorrelationIdScope(provider, "second");

        // Act
        scope.Dispose();

        // Assert
        provider.GetCorrelationId().Should().Be("first");
    }

    [Fact]
    public void CorrelationIdScope_NestedScopes_RestorePreviousIdsCorrectly()
    {
        // Arrange
        var provider = new CorrelationIdProvider();
        provider.SetCorrelationId("L0");

        // Act / Assert
        using (new CorrelationIdScope(provider, "L1"))
        {
            provider.GetCorrelationId().Should().Be("L1");
            using (new CorrelationIdScope(provider, "L2"))
            {
                provider.GetCorrelationId().Should().Be("L2");
            }

            provider.GetCorrelationId().Should().Be("L1");
        }

        provider.GetCorrelationId().Should().Be("L0");
    }
}
