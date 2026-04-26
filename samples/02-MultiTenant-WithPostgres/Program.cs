// MultiTenant + PostgreSQL sample.
//
// Two tenants share a single event_store table; isolation is enforced by the
// PostgreSqlEventStore writing the current TenantId on every event row.
//
// Run: docker compose up -d  →  dotnet run

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.DependencyInjection;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Abstractions.EventSourcing;
using Compendium.Core.Domain.Events;
using Compendium.Core.Domain.Primitives;
using Compendium.Core.EventSourcing;
using Compendium.Core.Results;
using Compendium.Multitenancy;
using Compendium.Multitenancy.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MultiTenant.WithPostgres;

// ── Domain ──────────────────────────────────────────────────────────────────

public sealed class OrderPlaced : DomainEventBase
{
    public OrderPlaced(string orderId, string customerId, decimal totalAmount, long version)
        : base(orderId, nameof(Order), version)
    {
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }

    public string CustomerId { get; }
    public decimal TotalAmount { get; }
}

public sealed class Order : AggregateRoot<Guid>
{
    private Order(Guid id) : base(id) { }

    public static Order Place(Guid id, string customerId, decimal totalAmount)
    {
        var order = new Order(id);
        order.AddDomainEvent(new OrderPlaced(id.ToString(), customerId, totalAmount, order.Version + 1));
        order.IncrementVersion();
        return order;
    }
}

// ── Composition root ────────────────────────────────────────────────────────

public static class Program
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=compendium_sample;Username=compendium;Password=compendium";

    public static async Task<int> Main()
    {
        Console.WriteLine("=== Compendium MultiTenant + PostgreSQL ===\n");

        if (!await IsPostgresReachableAsync())
        {
            Console.Error.WriteLine($"""
                ✗ Could not connect to PostgreSQL at {ConnectionString}.

                Start the bundled container:
                    docker compose up -d

                Then re-run:
                    dotnet run
                """);
            return 1;
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCompendiumMultitenancy();
        services.AddPostgreSqlEventStore(options =>
        {
            options.ConnectionString = ConnectionString;
            options.AutoCreateSchema = true;
        });

        await using var provider = services.BuildServiceProvider();

        // Whitelist domain events so the secure deserializer can re-hydrate them.
        var registry = provider.GetRequiredService<IEventTypeRegistry>();
        registry.RegisterEventType(typeof(OrderPlaced));

        // Initialize the event_store table on the very first run.
        var concrete = provider.GetRequiredService<PostgreSqlEventStore>();
        var schemaResult = await concrete.InitializeSchemaAsync();
        if (schemaResult.IsFailure)
        {
            Console.Error.WriteLine($"✗ Schema init failed: {schemaResult.Error.Code} - {schemaResult.Error.Message}");
            return 1;
        }

        var tenantSetter = provider.GetRequiredService<ITenantContextSetter>();
        var eventStore = provider.GetRequiredService<IEventStore>();

        var tenantA = new TenantInfo { Id = "acme", Name = "Acme Corp" };
        var tenantB = new TenantInfo { Id = "globex", Name = "Globex" };

        // Place one order under each tenant.
        var acmeOrderId = await PlaceOrderForTenant(tenantSetter, eventStore, tenantA, "cust-a-1", 19.99m);
        var globexOrderId = await PlaceOrderForTenant(tenantSetter, eventStore, tenantB, "cust-b-1", 42.50m);

        Console.WriteLine();

        // Read each tenant's order back, scoped by tenant context.
        await ReadOrderForTenant(tenantSetter, eventStore, tenantA, acmeOrderId);
        await ReadOrderForTenant(tenantSetter, eventStore, tenantB, globexOrderId);

        Console.WriteLine();

        // Cross-tenant read attempt: switching to Globex but asking for Acme's
        // aggregate returns nothing — isolation is enforced at the row level.
        await ReadOrderForTenant(tenantSetter, eventStore, tenantB, acmeOrderId, label: "(cross-tenant — expected: empty)");

        Console.WriteLine("\nDone.");
        return 0;
    }

    private static async Task<Guid> PlaceOrderForTenant(
        ITenantContextSetter tenantSetter,
        IEventStore eventStore,
        TenantInfo tenant,
        string customerId,
        decimal totalAmount)
    {
        tenantSetter.SetTenant(tenant);

        var order = Order.Place(Guid.NewGuid(), customerId, totalAmount);
        var events = order.GetUncommittedEvents();

        var append = await eventStore.AppendEventsAsync(
            aggregateId: order.Id.ToString(),
            events: events,
            expectedVersion: 0);

        if (append.IsFailure)
        {
            Console.Error.WriteLine($"  ✗ Append for tenant '{tenant.Id}' failed: {append.Error.Code} - {append.Error.Message}");
            return order.Id;
        }

        Console.WriteLine($"  ✓ Tenant {tenant.Id,-7} placed order {order.Id} (customer={customerId}, total={totalAmount})");
        return order.Id;
    }

    private static async Task ReadOrderForTenant(
        ITenantContextSetter tenantSetter,
        IEventStore eventStore,
        TenantInfo tenant,
        Guid orderId,
        string? label = null)
    {
        tenantSetter.SetTenant(tenant);

        var read = await eventStore.GetEventsAsync(orderId.ToString());
        var marker = label ?? "";
        if (read.IsFailure)
        {
            Console.WriteLine($"  • Tenant {tenant.Id,-7} read {orderId} → error: {read.Error.Code} {marker}");
            return;
        }

        Console.WriteLine($"  • Tenant {tenant.Id,-7} read {orderId} → {read.Value!.Count} event(s) {marker}");
        foreach (var @event in read.Value!)
        {
            Console.WriteLine($"      - {@event.GetType().Name} v{@event.AggregateVersion} @ {@event.OccurredOn:O}");
        }
    }

    private static async Task<bool> IsPostgresReachableAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await connection.OpenAsync(cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
