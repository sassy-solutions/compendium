// -----------------------------------------------------------------------
// <copyright file="TokenBucketRateLimiterTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Resilience;

namespace Compendium.Infrastructure.Tests.Resilience;

/// <summary>
/// Unit tests for <see cref="TokenBucketRateLimiter"/> covering happy path, exhaustion,
/// validation failures, and the simple <see cref="Result"/> overload.
/// </summary>
public sealed class TokenBucketRateLimiterTests
{
    private readonly ILogger<TokenBucketRateLimiter> _logger = Substitute.For<ILogger<TokenBucketRateLimiter>>();

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        // Arrange / Act
        var act = () => new TokenBucketRateLimiter(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("options");
    }

    [Fact]
    public void Ctor_NullLogger_Throws()
    {
        // Arrange
        var options = new RateLimitOptions();

        // Act
        var act = () => new TokenBucketRateLimiter(options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task IsAllowedAsync_FreshBucket_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut(maxRequests: 10);

        // Act
        var allowed = await sut.IsAllowedAsync("user-1");

        // Assert
        allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsAllowedAsync_EmptyKey_ReturnsFalse(string invalid)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var allowed = await sut.IsAllowedAsync(invalid);

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_BucketExhausted_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut(maxRequests: 2);

        // Act
        var first = await sut.IsAllowedAsync("user-1");
        var second = await sut.IsAllowedAsync("user-1");
        var third = await sut.IsAllowedAsync("user-1");

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
        third.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsyncT_WithinLimit_RunsOperation()
    {
        // Arrange
        var sut = CreateSut(maxRequests: 5);

        // Act
        var result = await sut.ExecuteAsync<int>("user-1", () => Task.FromResult(Result.Success(42)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsyncT_WhenLimitExceeded_ReturnsTooManyRequestsFailure()
    {
        // Arrange
        var sut = CreateSut(maxRequests: 1);

        // Act
        await sut.ExecuteAsync<int>("user-1", () => Task.FromResult(Result.Success(1)));
        var rejected = await sut.ExecuteAsync<int>("user-1", () => Task.FromResult(Result.Success(2)));

        // Assert
        rejected.IsFailure.Should().BeTrue();
        rejected.Error.Code.Should().Be("RateLimit.Exceeded");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsyncT_EmptyKey_ReturnsValidationFailure(string invalid)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync<int>(invalid, () => Task.FromResult(Result.Success(0)));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RateLimit.KeyEmpty");
    }

    [Fact]
    public async Task ExecuteAsyncT_NullOperation_Throws()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.ExecuteAsync<int>("k", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_NonGeneric_DelegatesToGenericOverload()
    {
        // Arrange
        var sut = CreateSut(maxRequests: 2);

        // Act
        var ok = await sut.ExecuteAsync("user-x", () => Task.FromResult(Result.Success()));
        var ok2 = await sut.ExecuteAsync("user-x", () => Task.FromResult(Result.Success()));
        var rejected = await sut.ExecuteAsync("user-x", () => Task.FromResult(Result.Success()));

        // Assert
        ok.IsSuccess.Should().BeTrue();
        ok2.IsSuccess.Should().BeTrue();
        rejected.IsFailure.Should().BeTrue();
        rejected.Error.Code.Should().Be("RateLimit.Exceeded");
    }

    [Fact]
    public async Task ExecuteAsync_NonGeneric_OperationFailure_PropagatesError()
    {
        // Arrange
        var sut = CreateSut(maxRequests: 5);

        // Act
        var result = await sut.ExecuteAsync(
            "user-x",
            () => Task.FromResult(Result.Failure(Error.Failure("op.fail", "boom"))));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("op.fail");
    }

    [Fact]
    public async Task IsAllowedAsync_WithDifferentKeys_BucketsAreIndependent()
    {
        // Arrange
        var sut = CreateSut(maxRequests: 1);

        // Act
        var aFirst = await sut.IsAllowedAsync("a");
        var bFirst = await sut.IsAllowedAsync("b");
        var aSecond = await sut.IsAllowedAsync("a");

        // Assert
        aFirst.Should().BeTrue();
        bFirst.Should().BeTrue();
        aSecond.Should().BeFalse();
    }

    [Fact]
    public void RateLimitOptions_Defaults_HaveSensibleValues()
    {
        // Arrange / Act
        var options = new RateLimitOptions();

        // Assert
        options.MaxRequests.Should().Be(100);
        options.Window.Should().Be(TimeSpan.FromMinutes(1));
        options.RefillInterval.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Refill_AfterRefillInterval_RestoresTokens()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            MaxRequests = 4,
            Window = TimeSpan.FromMilliseconds(40),
            RefillInterval = TimeSpan.FromMilliseconds(10),
        };
        var sut = new TokenBucketRateLimiter(options, _logger);

        // Drain the bucket
        for (var i = 0; i < 4; i++)
        {
            await sut.IsAllowedAsync("k");
        }

        (await sut.IsAllowedAsync("k")).Should().BeFalse();

        // Act
        await Task.Delay(50);
        var allowedAfterRefill = await sut.IsAllowedAsync("k");

        // Assert
        allowedAfterRefill.Should().BeTrue();
    }

    private TokenBucketRateLimiter CreateSut(int maxRequests = 100)
    {
        var options = new RateLimitOptions
        {
            MaxRequests = maxRequests,
            Window = TimeSpan.FromSeconds(60),
            RefillInterval = TimeSpan.FromSeconds(1),
        };
        return new TokenBucketRateLimiter(options, _logger);
    }
}
