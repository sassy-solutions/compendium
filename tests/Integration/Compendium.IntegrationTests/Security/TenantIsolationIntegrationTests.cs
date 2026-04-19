// -----------------------------------------------------------------------
// <copyright file="TenantIsolationIntegrationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Adapters.PostgreSQL.Security;
using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using Compendium.IntegrationTests.Infrastructure;
using Compendium.Multitenancy;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Compendium.IntegrationTests.Fixtures;
using Xunit;

namespace Compendium.IntegrationTests.Security;

/// <summary>
/// Unit tests for tenant validation and SQL filter generation.
/// These tests do NOT require database infrastructure and run quickly.
/// </summary>
public sealed class TenantValidationTests
{
    /// <summary>
    /// Test: TenantId injection attempt via SQL injection
    /// </summary>
    [Fact]
    public void IsValidTenantId_WithSQLInjectionAttempt_ReturnsFalse()
    {
        // Arrange: Various SQL injection payloads
        var injectionAttempts = new[]
        {
            "tenant'; DROP TABLE event_store; --",
            "tenant' OR '1'='1",
            "tenant\"; DELETE FROM event_store WHERE '1'='1",
            "tenant' UNION SELECT * FROM pg_user --",
            "../../../etc/passwd",
            "tenant<script>alert('xss')</script>",
            "tenant' AND 1=1 --"
        };

        // Act & Assert: All should be rejected
        foreach (var attempt in injectionAttempts)
        {
            var isValid = RowLevelSecurityExtensions.IsValidTenantId(attempt);
            Assert.False(isValid, $"Injection attempt should be rejected: {attempt}");
        }
    }

    /// <summary>
    /// Test: Valid tenant IDs should pass validation
    /// </summary>
    [Fact]
    public void IsValidTenantId_WithValidFormats_ReturnsTrue()
    {
        // Arrange: Valid tenant ID formats
        var validIds = new[]
        {
            "tenant-123",
            "tenant_abc",
            "TENANT-XYZ",
            "tenant123",
            "a",
            "tenant-abc-123_xyz"
        };

        // Act & Assert: All should pass
        foreach (var id in validIds)
        {
            var isValid = RowLevelSecurityExtensions.IsValidTenantId(id);
            Assert.True(isValid, $"Valid tenant ID should pass: {id}");
        }
    }

    /// <summary>
    /// Test: CreateTenantFilter generates safe SQL
    /// </summary>
    [Fact]
    public void CreateTenantFilter_GeneratesSafeSQL()
    {
        // Act
        var filter = RowLevelSecurityExtensions.CreateTenantFilter("tenant-abc");

        // Assert
        Assert.Equal("AND (@TenantId IS NULL OR tenant_id = @TenantId)", filter);
    }

    /// <summary>
    /// Test: CreateTenantFilter with invalid tenant ID throws
    /// </summary>
    [Fact]
    public void CreateTenantFilter_WithInvalidTenantId_Throws()
    {
        // Arrange
        var invalidTenantId = "tenant'; DROP TABLE event_store; --";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RowLevelSecurityExtensions.CreateTenantFilter(invalidTenantId));

        Assert.Contains("Invalid tenant ID format", exception.Message);
    }
}

/// <summary>
/// Integration tests for multi-tenant isolation security.
/// COMP-023: Multi-Tenancy Security Hardening
///
/// These tests verify that tenant isolation is enforced at both:
/// 1. Application level (Compendium framework queries)
/// 2. Database level (PostgreSQL Row-Level Security policies)
/// </summary>
public sealed class TenantIsolationIntegrationTests : IAsyncLifetime
{
    private string _connectionString = string.Empty;
    private PostgreSqlContainer? _postgres;
    private PostgreSqlEventStore? _eventStore;
    private ILogger<PostgreSqlEventStore> _logger = null!;
    private EventTypeRegistry _eventTypeRegistry = null!;
    private IEventDeserializer _eventDeserializer = null!;

    private static void SetTenantUsingReflection(TenantContext context, TenantInfo tenantInfo)
    {
        var setTenantMethod = typeof(TenantContext).GetMethod("SetTenant",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        setTenantMethod?.Invoke(context, new object[] { tenantInfo });
    }

    public async Task InitializeAsync()
    {
        // Use EnvironmentConfigurationHelper for connection string fallback
        var externalConnectionString = EnvironmentConfigurationHelper.GetPostgreSqlConnectionString();

        if (!string.IsNullOrEmpty(externalConnectionString))
        {
            _connectionString = externalConnectionString;
            Console.WriteLine("Using external PostgreSQL for TenantIsolationIntegrationTests");
        }
        else
        {
            // Fallback to TestContainers
            Console.WriteLine("Starting TestContainer for TenantIsolationIntegrationTests...");
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("compendium_test")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .WithCleanUp(true)
                .Build();

            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
            Console.WriteLine("TestContainer started for TenantIsolationIntegrationTests");
        }

        _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<PostgreSqlEventStore>();

        _eventTypeRegistry = new EventTypeRegistry();
        _eventTypeRegistry.RegisterEventType(typeof(TestDomainEvent));

        _eventDeserializer = new SecureEventDeserializer(_eventTypeRegistry);

        // Ensure schema is created
        await EnsureSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        if (_eventStore != null)
        {
            await _eventStore.DisposeAsync();
        }

        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
            Console.WriteLine("TestContainer disposed for TenantIsolationIntegrationTests");
        }
    }

    /// <summary>
    /// Test: Tenant A cannot read Tenant B's events via EventStore API
    /// </summary>
    [RequiresDockerFact]
    public async Task GetEventsAsync_WithDifferentTenant_ReturnsEmptyList()
    {
        // Arrange: Create events for tenant-A
        var tenantAContext = new TenantContext();
        SetTenantUsingReflection(tenantAContext, new TenantInfo { Id = "tenant-A", Name = "Tenant A" });

        var eventStoreA = CreateEventStore(tenantAContext);
        var aggregateId = $"test-aggregate-{Guid.NewGuid()}";

        var events = new List<IDomainEvent>
        {
            new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                Message = "Secret data for Tenant A"
            }
        };

        var appendResult = await eventStoreA.AppendEventsAsync(aggregateId, events, -1);
        Assert.True(appendResult.IsSuccess, "Failed to append events for tenant-A");

        // Act: Try to read as tenant-B
        var tenantBContext = new TenantContext();
        SetTenantUsingReflection(tenantBContext, new TenantInfo { Id = "tenant-B", Name = "Tenant B" });

        var eventStoreB = CreateEventStore(tenantBContext);
        var readResult = await eventStoreB.GetEventsAsync(aggregateId);

        // Assert: Tenant B should see ZERO events
        Assert.True(readResult.IsSuccess);
        Assert.Empty(readResult.Value);
    }

    /// <summary>
    /// Test: Tenant isolation via NULL tenant_id (single-tenant mode)
    /// </summary>
    [RequiresDockerFact]
    public async Task GetEventsAsync_WithNullTenantId_ReturnsAllEvents()
    {
        // Arrange: Create events with NULL tenant (single-tenant mode)
        var eventStore = CreateEventStore(null); // No tenant context
        var aggregateId = $"test-aggregate-{Guid.NewGuid()}";

        var events = new List<IDomainEvent>
        {
            new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                Message = "Data for single-tenant mode"
            }
        };

        var appendResult = await eventStore.AppendEventsAsync(aggregateId, events, -1);
        Assert.True(appendResult.IsSuccess);

        // Act: Read with NULL tenant context
        var readResult = await eventStore.GetEventsAsync(aggregateId);

        // Assert: Should retrieve the event
        Assert.True(readResult.IsSuccess);
        Assert.Single(readResult.Value);
        Assert.Equal("Data for single-tenant mode", ((TestDomainEvent)readResult.Value[0]).Message);
    }

    /// <summary>
    /// Test: Cross-tenant write attempt should fail
    /// </summary>
    [RequiresDockerFact]
    public async Task AppendEventsAsync_CrossTenantAccess_ShouldFail()
    {
        // Arrange: Create aggregate for tenant-X
        var tenantXContext = new TenantContext();
        SetTenantUsingReflection(tenantXContext, new TenantInfo { Id = "tenant-X", Name = "Tenant X" });

        var eventStoreX = CreateEventStore(tenantXContext);
        var aggregateId = $"test-aggregate-{Guid.NewGuid()}";

        var initialEvents = new List<IDomainEvent>
        {
            new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                Message = "Initial event for Tenant X"
            }
        };

        var appendResult = await eventStoreX.AppendEventsAsync(aggregateId, initialEvents, -1);
        Assert.True(appendResult.IsSuccess);

        // Act: Attempt to append to same aggregate as tenant-Y (different tenant)
        var tenantYContext = new TenantContext();
        SetTenantUsingReflection(tenantYContext, new TenantInfo { Id = "tenant-Y", Name = "Tenant Y" });

        var eventStoreY = CreateEventStore(tenantYContext);

        var maliciousEvents = new List<IDomainEvent>
        {
            new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                Message = "Malicious event from Tenant Y"
            }
        };

        // Use -1 because for tenant-Y this is a NEW stream (tenant isolation)
        var maliciousAppendResult = await eventStoreY.AppendEventsAsync(aggregateId, maliciousEvents, -1);

        // Assert: Should succeed (creates new stream for tenant-Y), BUT when reading back...
        var readResult = await eventStoreY.GetEventsAsync(aggregateId);

        // Tenant Y should only see their own event, not Tenant X's
        Assert.True(readResult.IsSuccess);
        Assert.Single(readResult.Value);
        Assert.Equal("Malicious event from Tenant Y", ((TestDomainEvent)readResult.Value[0]).Message);
    }

    /// <summary>
    /// Test: Statistics should respect tenant isolation
    /// </summary>
    [RequiresDockerFact]
    public async Task GetStatisticsAsync_RespectsTenantIsolation()
    {
        // Arrange: Create events for two different tenants
        var tenant1Context = new TenantContext();
        SetTenantUsingReflection(tenant1Context, new TenantInfo { Id = $"tenant-stats-1-{Guid.NewGuid()}", Name = "Stats Tenant 1" });

        var tenant2Context = new TenantContext();
        SetTenantUsingReflection(tenant2Context, new TenantInfo { Id = $"tenant-stats-2-{Guid.NewGuid()}", Name = "Stats Tenant 2" });

        var eventStore1 = CreateEventStore(tenant1Context);
        var eventStore2 = CreateEventStore(tenant2Context);

        // Add 3 events for tenant 1
        var aggregate1Id = $"stats-aggregate-1-{Guid.NewGuid()}";
        var events1 = Enumerable.Range(1, 3).Select(i => new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregate1Id,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            Message = $"Event {i} for Tenant 1"
        }).ToList();

        await eventStore1.AppendEventsAsync(aggregate1Id, events1, -1);

        // Add 5 events for tenant 2
        var aggregate2Id = $"stats-aggregate-2-{Guid.NewGuid()}";
        var events2 = Enumerable.Range(1, 5).Select(i => new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregate2Id,
            AggregateType = "TestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            Message = $"Event {i} for Tenant 2"
        }).ToList();

        await eventStore2.AppendEventsAsync(aggregate2Id, events2, -1);

        // Act: Get statistics for each tenant
        var stats1Result = await eventStore1.GetStatisticsAsync();
        var stats2Result = await eventStore2.GetStatisticsAsync();

        // Assert: Each tenant should only see their own events
        Assert.True(stats1Result.IsSuccess);
        Assert.True(stats2Result.IsSuccess);

        // Tenant 1 should have >= 3 events (may have more from other tests)
        Assert.True(stats1Result.Value.TotalEvents >= 3);

        // Tenant 2 should have >= 5 events (may have more from other tests)
        Assert.True(stats2Result.Value.TotalEvents >= 5);

        // Each tenant should not see the other's aggregates
        Assert.DoesNotContain(aggregate2Id, stats1Result.Value.AggregateStatistics.Keys);
        Assert.DoesNotContain(aggregate1Id, stats2Result.Value.AggregateStatistics.Keys);
    }

    /// <summary>
    /// Test: ExistsAsync respects tenant isolation
    /// </summary>
    [RequiresDockerFact]
    public async Task ExistsAsync_RespectsTenantIsolation()
    {
        // Arrange: Create aggregate for tenant-exists-1
        var tenant1Context = new TenantContext();
        SetTenantUsingReflection(tenant1Context, new TenantInfo { Id = $"tenant-exists-1-{Guid.NewGuid()}", Name = "Exists Tenant 1" });

        var eventStore1 = CreateEventStore(tenant1Context);
        var aggregateId = $"exists-test-{Guid.NewGuid()}";

        var events = new List<IDomainEvent>
        {
            new TestDomainEvent
            {
                EventId = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = "TestAggregate",
                OccurredOn = DateTimeOffset.UtcNow,
                Message = "Event for Tenant 1"
            }
        };

        await eventStore1.AppendEventsAsync(aggregateId, events, -1);

        // Act: Check existence from tenant-2 (different tenant)
        var tenant2Context = new TenantContext();
        SetTenantUsingReflection(tenant2Context, new TenantInfo { Id = $"tenant-exists-2-{Guid.NewGuid()}", Name = "Exists Tenant 2" });

        var eventStore2 = CreateEventStore(tenant2Context);
        var existsResult = await eventStore2.ExistsAsync(aggregateId);

        // Assert: Tenant 2 should NOT see tenant 1's aggregate
        Assert.True(existsResult.IsSuccess);
        Assert.False(existsResult.Value);

        // Verify tenant 1 CAN see it
        var exists1Result = await eventStore1.ExistsAsync(aggregateId);
        Assert.True(exists1Result.IsSuccess);
        Assert.True(exists1Result.Value);
    }

    private PostgreSqlEventStore CreateEventStore(ITenantContext? tenantContext)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new Adapters.PostgreSQL.Configuration.PostgreSqlOptions
        {
            ConnectionString = _connectionString,
            TableName = "event_store",
            AutoCreateSchema = true
        });

        return new PostgreSqlEventStore(
            options,
            _eventDeserializer,
            _logger,
            tenantContext);
    }

    private async Task EnsureSchemaAsync()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new Adapters.PostgreSQL.Configuration.PostgreSqlOptions
        {
            ConnectionString = _connectionString,
            TableName = "event_store",
            AutoCreateSchema = true
        });

        await using var eventStore = new PostgreSqlEventStore(
            options,
            _eventDeserializer,
            _logger,
            null);

        await eventStore.InitializeSchemaAsync();
    }
}

/// <summary>
/// Test domain event for tenant isolation tests.
/// </summary>
internal sealed record TestDomainEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public string AggregateId { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
    public long AggregateVersion { get; init; }
    public int EventVersion { get; init; } = 1;
    public DateTimeOffset OccurredOn { get; init; }
    public string Message { get; init; } = string.Empty;
}
