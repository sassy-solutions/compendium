// -----------------------------------------------------------------------
// <copyright file="SnapshotTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Primitives;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.Tests.EventSourcing;

public class SnapshotTests : IDisposable
{
    private readonly InMemorySnapshotStore _snapshotStore;
    private readonly TenantContext _tenantContext;

    public SnapshotTests()
    {
        _tenantContext = new TenantContext();
        _snapshotStore = new InMemorySnapshotStore(NullLogger<InMemorySnapshotStore>.Instance, _tenantContext);
    }

    [Fact]
    public async Task InMemorySnapshotStore_Should_Save_And_Retrieve_Snapshots()
    {
        // Arrange
        var aggregate = new TestAggregate("test-id");
        aggregate.DoSomething("test action");

        // Act
        var saveResult = await _snapshotStore.SaveSnapshotAsync("test-id", aggregate, 1);
        var loadResult = await _snapshotStore.GetLatestSnapshotAsync<TestAggregate>("test-id");

        // Assert
        saveResult.IsSuccess.Should().BeTrue();
        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value.State.Id.Should().Be("test-id");
        loadResult.Value.Version.Should().Be(1);
    }

    [Fact]
    public async Task InMemorySnapshotStore_Should_Handle_Not_Found()
    {
        // Act
        var loadResult = await _snapshotStore.GetLatestSnapshotAsync<TestAggregate>("non-existent");

        // Assert
        loadResult.IsSuccess.Should().BeFalse();
        loadResult.Error.Code.Should().Be("SnapshotStore.NotFound");
    }

    [Fact]
    public async Task InMemorySnapshotStore_Should_Update_With_Newer_Version()
    {
        // Arrange
        var aggregate = new TestAggregate("test-id");
        aggregate.DoSomething("action 1");

        // Act - Save initial snapshot
        await _snapshotStore.SaveSnapshotAsync("test-id", aggregate, 1);

        // Try to save older version - should be skipped
        await _snapshotStore.SaveSnapshotAsync("test-id", aggregate, 0);

        // Save newer version - should update
        aggregate.DoSomething("action 2");
        await _snapshotStore.SaveSnapshotAsync("test-id", aggregate, 2);

        var loadResult = await _snapshotStore.GetLatestSnapshotAsync<TestAggregate>("test-id");

        // Assert
        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value.Version.Should().Be(2);
    }

    [Fact]
    public async Task InMemorySnapshotStore_Should_Support_Multi_Tenancy()
    {
        // Arrange
        var tenant1 = new TenantInfo { Id = "tenant1", Name = "Tenant 1" };
        var tenant2 = new TenantInfo { Id = "tenant2", Name = "Tenant 2" };

        var data1 = "tenant1 data";
        var data2 = "tenant2 data";

        // Act
        // Save snapshot for tenant 1
        using (var scope1 = new TenantScope(_tenantContext, tenant1))
        {
            var result1 = await _snapshotStore.SaveSnapshotAsync("shared-id", data1, 1);
            result1.IsSuccess.Should().BeTrue();
        }

        // Save snapshot for tenant 2
        using (var scope2 = new TenantScope(_tenantContext, tenant2))
        {
            var result2 = await _snapshotStore.SaveSnapshotAsync("shared-id", data2, 1);
            result2.IsSuccess.Should().BeTrue();
        }

        // Load snapshots for each tenant
        string? loadedData1 = null;
        string? loadedData2 = null;

        using (var scope1 = new TenantScope(_tenantContext, tenant1))
        {
            var result1 = await _snapshotStore.GetLatestSnapshotAsync<string>("shared-id");
            result1.IsSuccess.Should().BeTrue();
            loadedData1 = result1.Value.State;
        }

        using (var scope2 = new TenantScope(_tenantContext, tenant2))
        {
            var result2 = await _snapshotStore.GetLatestSnapshotAsync<string>("shared-id");
            result2.IsSuccess.Should().BeTrue();
            loadedData2 = result2.Value.State;
        }

        // Assert
        loadedData1.Should().Be("tenant1 data");
        loadedData2.Should().Be("tenant2 data");
    }

    [Fact]
    public void NoSnapshotStrategy_Should_Never_Take_Snapshots()
    {
        // Arrange
        var strategy = new NoSnapshotStrategy();

        // Act & Assert
        strategy.ShouldTakeSnapshot("any-id", 1, 1).Should().BeFalse();
        strategy.ShouldTakeSnapshot("any-id", 100, 1000).Should().BeFalse();
        strategy.ShouldTakeSnapshot("any-id", 0, 0).Should().BeFalse();
    }

    [Fact]
    public void IntervalSnapshotStrategy_Should_Take_Snapshots_At_Intervals()
    {
        // Arrange
        var strategy = new IntervalSnapshotStrategy(10);

        // Act & Assert
        strategy.ShouldTakeSnapshot("any-id", 1, 5).Should().BeFalse();
        strategy.ShouldTakeSnapshot("any-id", 1, 10).Should().BeTrue();
        strategy.ShouldTakeSnapshot("any-id", 1, 15).Should().BeFalse();
        strategy.ShouldTakeSnapshot("any-id", 1, 20).Should().BeTrue();
        strategy.ShouldTakeSnapshot("any-id", 1, 0).Should().BeFalse();
    }

    [Fact]
    public async Task SnapshotStore_Should_Return_Statistics()
    {
        // Arrange
        var aggregate = new TestAggregate("test-id");
        await _snapshotStore.SaveSnapshotAsync("test-id", aggregate, 1);

        // Act
        var statsResult = await _snapshotStore.GetStatisticsAsync();

        // Assert
        statsResult.IsSuccess.Should().BeTrue();
        statsResult.Value.TotalSnapshots.Should().Be(1);
        statsResult.Value.OldestSnapshot.Should().NotBeNull();
        statsResult.Value.NewestSnapshot.Should().NotBeNull();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _snapshotStore?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

// Test helper class
public class TestAggregate : AggregateRoot<string>
{
    private readonly List<string> _actions = new();

    public TestAggregate(string id) : base(id)
    {
    }

    public IReadOnlyList<string> Actions => _actions.AsReadOnly();

    public void DoSomething(string action)
    {
        _actions.Add(action);
    }
}
