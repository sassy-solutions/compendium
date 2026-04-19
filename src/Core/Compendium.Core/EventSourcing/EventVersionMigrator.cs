// -----------------------------------------------------------------------
// <copyright file="EventVersionMigrator.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Compendium.Core.Domain.Events;
using Compendium.Core.Results;

namespace Compendium.Core.EventSourcing;

/// <summary>
/// Manages event version migrations using registered upcasters.
/// Applies a chain of upcasters to migrate events from old versions to the latest schema.
/// </summary>
/// <remarks>
/// The EventVersionMigrator handles:
/// - Registration of upcasters for specific event type and version combinations
/// - Automatic chaining of multiple upcasters (V1 -> V2 -> V3)
/// - Detection of missing upcasters in the migration chain
/// - Thread-safe upcaster management
///
/// Example usage:
/// <code>
/// var migrator = new EventVersionMigrator();
/// migrator.RegisterUpcaster(new ConfigurationCreatedV1ToV2Upcaster());
/// migrator.RegisterUpcaster(new ConfigurationCreatedV2ToV3Upcaster());
///
/// // Automatically applies V1->V2->V3 migration
/// var result = migrator.MigrateToLatest(oldV1Event);
/// </code>
/// </remarks>
public sealed class EventVersionMigrator : IEventVersionMigrator
{
    private readonly ConcurrentDictionary<(Type EventType, int Version), IEventUpcaster> _upcasters = new();
    private readonly ConcurrentDictionary<Type, int> _latestVersions = new();
    private readonly Action<string>? _logWarning;
    private readonly Action<string>? _logInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventVersionMigrator"/> class.
    /// </summary>
    /// <param name="logWarning">Optional warning logger action.</param>
    /// <param name="logInfo">Optional info logger action.</param>
    public EventVersionMigrator(
        Action<string>? logWarning = null,
        Action<string>? logInfo = null)
    {
        _logWarning = logWarning;
        _logInfo = logInfo;
    }

    /// <inheritdoc />
    public void RegisterUpcaster(IEventUpcaster upcaster)
    {
        ArgumentNullException.ThrowIfNull(upcaster);

        var key = (upcaster.SourceEventType, upcaster.SourceVersion);

        if (!_upcasters.TryAdd(key, upcaster))
        {
            _logWarning?.Invoke(
                $"Upcaster for {upcaster.SourceEventType.Name} V{upcaster.SourceVersion}->V{upcaster.TargetVersion} was already registered. Replacing.");
            _upcasters[key] = upcaster;
        }

        // Update latest known version for this event type
        _latestVersions.AddOrUpdate(
            upcaster.TargetEventType,
            upcaster.TargetVersion,
            (_, current) => Math.Max(current, upcaster.TargetVersion));

        _logInfo?.Invoke(
            $"Registered upcaster: {upcaster.SourceEventType.Name} V{upcaster.SourceVersion} -> {upcaster.TargetEventType.Name} V{upcaster.TargetVersion}");
    }

    /// <inheritdoc />
    public Result<IDomainEvent> MigrateToLatest(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var currentEvent = domainEvent;
        var currentVersion = domainEvent.EventVersion;
        var startingEventType = domainEvent.GetType();

        // Apply upcasters in sequence until no more are available
        var migrationsApplied = 0;
        const int maxMigrations = 100; // Prevent infinite loops

        while (migrationsApplied < maxMigrations)
        {
            var key = (currentEvent.GetType(), currentVersion);

            // Check if there's an upcaster for the current event type and version
            if (!_upcasters.TryGetValue(key, out var upcaster))
            {
                // No more upcasters available - we're at the latest version
                break;
            }

            try
            {
                var previousType = currentEvent.GetType();
                var previousVersion = currentVersion;

                currentEvent = upcaster.Upcast(currentEvent);
                currentVersion = currentEvent.EventVersion;
                migrationsApplied++;

                _logInfo?.Invoke(
                    $"Applied upcaster: {previousType.Name} V{previousVersion} -> {currentEvent.GetType().Name} V{currentVersion}");
            }
            catch (Exception ex)
            {
                return Result.Failure<IDomainEvent>(
                    Error.Failure(
                        "EventVersionMigrator.UpcastFailed",
                        $"Failed to upcast {currentEvent.GetType().Name} V{currentVersion}: {ex.Message}"));
            }
        }

        if (migrationsApplied >= maxMigrations)
        {
            return Result.Failure<IDomainEvent>(
                Error.Failure(
                    "EventVersionMigrator.TooManyMigrations",
                    $"Migration exceeded maximum of {maxMigrations} upcasts. Possible circular migration detected."));
        }

        if (migrationsApplied > 0)
        {
            _logInfo?.Invoke(
                $"Successfully migrated {startingEventType.Name} from V{domainEvent.EventVersion} to {currentEvent.GetType().Name} V{currentVersion} ({migrationsApplied} migrations applied)");
        }

        return Result.Success(currentEvent);
    }

    /// <inheritdoc />
    public int? GetLatestVersion(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return _latestVersions.TryGetValue(eventType, out var version) ? version : null;
    }

    /// <inheritdoc />
    public bool HasUpcasterForVersion(Type eventType, int version)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return _upcasters.ContainsKey((eventType, version));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IEventUpcaster> GetRegisteredUpcasters()
    {
        return _upcasters.Values.ToList().AsReadOnly();
    }
}

/// <summary>
/// Interface for the event version migrator service.
/// </summary>
public interface IEventVersionMigrator
{
    /// <summary>
    /// Registers an upcaster for migrating events from one version to another.
    /// </summary>
    /// <param name="upcaster">The upcaster to register.</param>
    void RegisterUpcaster(IEventUpcaster upcaster);

    /// <summary>
    /// Migrates an event to the latest known version by applying a chain of upcasters.
    /// </summary>
    /// <param name="domainEvent">The event to migrate.</param>
    /// <returns>The migrated event at the latest version, or an error if migration fails.</returns>
    Result<IDomainEvent> MigrateToLatest(IDomainEvent domainEvent);

    /// <summary>
    /// Gets the latest known version for an event type.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns>The latest version number, or null if no upcasters are registered for this type.</returns>
    int? GetLatestVersion(Type eventType);

    /// <summary>
    /// Checks if an upcaster is registered for a specific event type and version.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="version">The version number.</param>
    /// <returns>True if an upcaster is registered, false otherwise.</returns>
    bool HasUpcasterForVersion(Type eventType, int version);

    /// <summary>
    /// Gets all registered upcasters.
    /// </summary>
    /// <returns>A read-only collection of registered upcasters.</returns>
    IReadOnlyCollection<IEventUpcaster> GetRegisteredUpcasters();
}
