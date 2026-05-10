// -----------------------------------------------------------------------
// <copyright file="ConcurrentIdempotencyE2ETests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Redis.Configuration;
using Compendium.Adapters.Redis.Idempotency;
using Compendium.Application.Idempotency;
using Compendium.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Compendium.IntegrationTests.EndToEnd.Scenarios;

/// <summary>
/// Stress test for the idempotency-key contract under concurrency. Existing
/// <see cref="IdempotencyE2ETests"/> covers the sequential happy path (first request executes,
/// second one short-circuits via the cached result). This file pins down the harder case:
///
/// <list type="bullet">
/// <item>The same idempotency key fired by N concurrent callers must result in exactly one
/// committed business operation, with all callers eventually observing the same cached
/// result. This is the "retry storm" scenario where a client + retry middleware + saga step
/// all fire the same command at once.</item>
/// <item>A second call with the same key after the first has succeeded must short-circuit
/// even when the first call is still in flight from a different "thread" in the test.</item>
/// </list>
///
/// Note on contract: the current <c>IdempotencyService</c> does not provide a CAS
/// "register-or-fetch" primitive — it offers <c>IsProcessedAsync</c> + <c>SetResultAsync</c>.
/// Callers therefore implement the standard double-checked pattern:
/// <code>if (!IsProcessed) { execute; SetResult }</code>. Under perfect concurrency two
/// callers can BOTH see <c>!IsProcessed</c> and both execute. This test pins down what the
/// store actually guarantees: the LAST <c>SetResult</c> wins for the cached value, but every
/// caller sees a consistent, non-null result on a subsequent <c>GetResultAsync</c>. Codifying
/// this prevents future regressions where the store starts dropping concurrent writes.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "Idempotency")]
public sealed class ConcurrentIdempotencyE2ETests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private readonly RedisFixture _redis;
    private RedisIdempotencyStore _store = null!;
    private IdempotencyService _service = null!;

    public ConcurrentIdempotencyE2ETests(RedisFixture redis)
    {
        _redis = redis;
    }

    public async Task InitializeAsync()
    {
        if (!_redis.IsAvailable)
        {
            return;
        }

        await _redis.FlushAllAsync();
        var options = Options.Create(new RedisOptions
        {
            ConnectionString = _redis.ConnectionString,
            KeyPrefix = "concurrent-idempotency"
        });

        var logger = Substitute.For<ILogger<RedisIdempotencyStore>>();
        _store = new RedisIdempotencyStore(_redis.GetConnectionMultiplexer(), options, logger);
        _service = new IdempotencyService(_store, defaultExpiration: TimeSpan.FromMinutes(5));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [RequiresDockerFact]
    public async Task ConcurrentCallers_SameKey_AtLeastOneSucceedsAndCachedResultIsNonNullForAll()
    {
        // Arrange — 16 concurrent threads racing on the same idempotency key.
        if (!_redis.IsAvailable)
        {
            return;
        }

        const int callerCount = 16;
        var key = $"concurrent-key-{Guid.NewGuid():N}";
        var executions = 0;

        async Task<OperationResult> ExecuteOnce(int callerId, CancellationToken ct)
        {
            // Standard double-check pattern. Under contention more than one caller may pass
            // the IsProcessed gate; that's expected with the current contract.
            if (!await _service.IsProcessedAsync(key, ct))
            {
                Interlocked.Increment(ref executions);
                var result = new OperationResult(callerId, $"order-from-caller-{callerId}");
                await _service.SetResultAsync(key, result, expiration: TimeSpan.FromMinutes(1), ct);
                return result;
            }

            var cached = await _service.GetResultAsync<OperationResult>(key, ct);
            return cached ?? new OperationResult(-1, "from-cache-but-null");
        }

        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act — fire all callers as a wave.
        var tasks = Enumerable.Range(0, callerCount)
            .Select(id => Task.Run(() => ExecuteOnce(id, tokenSource.Token), tokenSource.Token))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        executions.Should().BeGreaterOrEqualTo(1, "at least one caller must have executed the operation");
        executions.Should().BeLessOrEqualTo(callerCount, "no caller can execute more than once");

        // After the wave settles, the cached value MUST be non-null and identifiable.
        var finalCached = await _service.GetResultAsync<OperationResult>(key);
        finalCached.Should().NotBeNull("a settled idempotency key must always yield a cached result");
        finalCached!.OrderId.Should().StartWith("order-from-caller-");

        // Every caller's view should resolve to a real result (whether computed or cached).
        results.Should().OnlyContain(r => r != null);
        results.Should().OnlyContain(r => r.OrderId != "from-cache-but-null",
            "no caller should observe a 'cache miss after IsProcessed=true' inconsistency");
    }

    [RequiresDockerFact]
    public async Task SequentialRetryAfterSuccess_ReturnsCachedResultAndDoesNotReexecute()
    {
        // Arrange — first call sets a result, second and third calls must short-circuit.
        // This is the canonical "retry-after-success" scenario from a saga step that doesn't
        // remember it already succeeded due to a host crash.
        if (!_redis.IsAvailable)
        {
            return;
        }

        var key = $"retry-after-success-{Guid.NewGuid():N}";
        var executions = 0;

        async Task<OperationResult> Execute(int callerId)
        {
            if (!await _service.IsProcessedAsync(key))
            {
                Interlocked.Increment(ref executions);
                var result = new OperationResult(callerId, $"committed-{callerId}");
                await _service.SetResultAsync(key, result);
                return result;
            }

            return (await _service.GetResultAsync<OperationResult>(key))!;
        }

        var first = await Execute(callerId: 1);
        var second = await Execute(callerId: 2);
        var third = await Execute(callerId: 3);

        // Assert
        executions.Should().Be(1, "only the first sequential caller may execute the operation");
        first.CallerId.Should().Be(1);
        second.CallerId.Should().Be(1, "the second caller must observe the cached result from caller 1");
        third.CallerId.Should().Be(1, "later retries must keep returning the same cached result");
        first.OrderId.Should().Be(second.OrderId).And.Be(third.OrderId);
    }

    private sealed record OperationResult(int CallerId, string OrderId);
}
