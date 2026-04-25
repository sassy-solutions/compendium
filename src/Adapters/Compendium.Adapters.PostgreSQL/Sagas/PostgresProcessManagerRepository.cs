// -----------------------------------------------------------------------
// <copyright file="PostgresProcessManagerRepository.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Text.Json;
using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Core.Results;
using Compendium.Multitenancy;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Compendium.Adapters.PostgreSQL.Sagas;

/// <summary>
/// PostgreSQL implementation of <see cref="IProcessManagerRepository"/>. Persists
/// process-manager metadata, a JSON snapshot of state (best-effort, optional), and the
/// per-step status table. Multi-tenant aware via <see cref="ITenantContext"/>.
/// </summary>
/// <remarks>
/// Schema is created on first use via <see cref="InitializeAsync"/>. In production you
/// likely want to manage migrations externally; the auto-init is provided for parity
/// with the rest of <c>Compendium.Adapters.PostgreSQL</c>.
/// </remarks>
public sealed class PostgresProcessManagerRepository : IProcessManagerRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    private readonly string _connectionString;
    private readonly ITenantContext? _tenantContext;
    private readonly ILogger<PostgresProcessManagerRepository>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresProcessManagerRepository"/> class.
    /// </summary>
    /// <param name="options">PostgreSQL configuration options.</param>
    /// <param name="tenantContext">Optional tenant context for multi-tenant isolation.</param>
    /// <param name="logger">Optional logger.</param>
    public PostgresProcessManagerRepository(
        IOptions<PostgreSqlOptions> options,
        ITenantContext? tenantContext = null,
        ILogger<PostgresProcessManagerRepository>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _connectionString = options.Value?.ConnectionString
            ?? throw new InvalidOperationException("PostgreSQL connection string is not configured");
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates the schema if it does not yet exist. Safe to call many times.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS process_managers (
                id              UUID PRIMARY KEY,
                status          VARCHAR(32) NOT NULL,
                version         INTEGER NOT NULL DEFAULT 0,
                state_json      JSONB,
                correlation_id  UUID,
                tenant_id       VARCHAR(255),
                created_at      TIMESTAMPTZ NOT NULL,
                completed_at    TIMESTAMPTZ
            );

            CREATE INDEX IF NOT EXISTS ix_process_managers_status
                ON process_managers (status)
                WHERE status IN ('InProgress', 'Compensating');

            CREATE INDEX IF NOT EXISTS ix_process_managers_tenant
                ON process_managers (tenant_id);

            CREATE TABLE IF NOT EXISTS process_manager_steps (
                id                  UUID PRIMARY KEY,
                process_manager_id  UUID NOT NULL REFERENCES process_managers(id) ON DELETE CASCADE,
                name                VARCHAR(255) NOT NULL,
                step_order          INTEGER NOT NULL,
                status              VARCHAR(32) NOT NULL,
                executed_at         TIMESTAMPTZ,
                compensated_at      TIMESTAMPTZ,
                error_message       TEXT
            );

            CREATE INDEX IF NOT EXISTS ix_pm_steps_pm_id
                ON process_manager_steps (process_manager_id);
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        _logger?.LogDebug("Process manager schema initialized.");
    }

    /// <inheritdoc />
    public async Task<Result<IProcessManager>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sagaSql = """
            SELECT id, status, created_at, completed_at
            FROM process_managers
            WHERE id = @Id AND (@TenantId IS NULL OR tenant_id = @TenantId)
            LIMIT 1;
            """;

        const string stepsSql = """
            SELECT id AS Id, name AS Name, step_order AS Order, status AS Status,
                   executed_at AS ExecutedAt, compensated_at AS CompensatedAt,
                   error_message AS ErrorMessage
            FROM process_manager_steps
            WHERE process_manager_id = @Id
            ORDER BY step_order;
            """;

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var saga = await connection.QuerySingleOrDefaultAsync<SagaRow>(
                new CommandDefinition(sagaSql, new { Id = id, TenantId = _tenantContext?.TenantId }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (saga is null)
            {
                return Result.Failure<IProcessManager>(
                    Error.NotFound("ProcessManager.NotFound", $"Process manager {id} not found."));
            }

            var stepRows = (await connection.QueryAsync<StepRow>(
                new CommandDefinition(stepsSql, new { Id = id }, cancellationToken: cancellationToken))
                .ConfigureAwait(false)).ToList();

            var steps = stepRows.Select(r => new SagaStep
            {
                Id = r.Id,
                Name = r.Name,
                Order = r.Order,
                Status = Enum.Parse<SagaStepStatus>(r.Status, ignoreCase: true),
                ExecutedAt = r.ExecutedAt,
                CompensatedAt = r.CompensatedAt,
                ErrorMessage = r.ErrorMessage,
            });

            var pm = new PersistedProcessManager(
                saga.Id,
                Enum.Parse<SagaStatus>(saga.Status, ignoreCase: true),
                DateTime.SpecifyKind(saga.CreatedAt, DateTimeKind.Utc),
                saga.CompletedAt is null ? null : DateTime.SpecifyKind(saga.CompletedAt.Value, DateTimeKind.Utc),
                steps);

            return Result.Success<IProcessManager>(pm);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load process manager {Id}", id);
            return Result.Failure<IProcessManager>(Error.Failure("ProcessManager.LoadFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(IProcessManager processManager, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processManager);

        const string upsertSagaSql = """
            INSERT INTO process_managers (id, status, version, state_json, tenant_id, created_at, completed_at)
            VALUES (@Id, @Status, 0, @StateJson::jsonb, @TenantId, @CreatedAt, @CompletedAt)
            ON CONFLICT (id) DO UPDATE
              SET status = EXCLUDED.status,
                  state_json = EXCLUDED.state_json,
                  completed_at = EXCLUDED.completed_at,
                  version = process_managers.version + 1;
            """;

        const string upsertStepSql = """
            INSERT INTO process_manager_steps (id, process_manager_id, name, step_order, status, executed_at, compensated_at, error_message)
            VALUES (@Id, @ProcessManagerId, @Name, @Order, @Status, @ExecutedAt, @CompensatedAt, @ErrorMessage)
            ON CONFLICT (id) DO UPDATE
              SET status = EXCLUDED.status,
                  executed_at = EXCLUDED.executed_at,
                  compensated_at = EXCLUDED.compensated_at,
                  error_message = EXCLUDED.error_message;
            """;

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            // Best-effort state serialization. If the saga doesn't expose typed state, persist null.
            var stateJson = TrySerializeState(processManager);

            await connection.ExecuteAsync(new CommandDefinition(
                upsertSagaSql,
                new
                {
                    processManager.Id,
                    Status = processManager.Status.ToString(),
                    StateJson = stateJson,
                    TenantId = _tenantContext?.TenantId,
                    processManager.CreatedAt,
                    processManager.CompletedAt,
                },
                tx,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            foreach (var step in processManager.Steps)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    upsertStepSql,
                    new
                    {
                        step.Id,
                        ProcessManagerId = processManager.Id,
                        step.Name,
                        step.Order,
                        Status = step.Status.ToString(),
                        step.ExecutedAt,
                        step.CompensatedAt,
                        step.ErrorMessage,
                    },
                    tx,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save process manager {Id}", processManager.Id);
            return Result.Failure(Error.Failure("ProcessManager.SaveFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateStatusAsync(Guid id, SagaStatus status, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE process_managers
            SET status = @Status,
                completed_at = CASE WHEN @Status IN ('Completed', 'Compensated') THEN NOW() ELSE completed_at END,
                version = version + 1
            WHERE id = @Id;
            """;

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var rows = await connection.ExecuteAsync(new CommandDefinition(
                sql, new { Id = id, Status = status.ToString() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (rows == 0)
            {
                return Result.Failure(Error.NotFound("ProcessManager.NotFound", $"Process manager {id} not found."));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("ProcessManager.UpdateStatusFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateStepStatusAsync(
        Guid processManagerId,
        Guid stepId,
        SagaStepStatus status,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE process_manager_steps
            SET status = @Status,
                executed_at = CASE WHEN @Status = 'Completed' THEN NOW() ELSE executed_at END,
                compensated_at = CASE WHEN @Status = 'Compensated' THEN NOW() ELSE compensated_at END,
                error_message = @ErrorMessage
            WHERE id = @StepId AND process_manager_id = @ProcessManagerId;
            """;

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var rows = await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { ProcessManagerId = processManagerId, StepId = stepId, Status = status.ToString(), ErrorMessage = errorMessage },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (rows == 0)
            {
                return Result.Failure(Error.NotFound(
                    "ProcessManager.StepNotFound",
                    $"Step {stepId} not found in process manager {processManagerId}."));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("ProcessManager.UpdateStepStatusFailed", ex.Message));
        }
    }

    private static string? TrySerializeState(IProcessManager processManager)
    {
        // Reflection over IProcessManager<TState>.State — we don't depend on a concrete
        // generic type so callers can use any state shape. Failures degrade to null.
        try
        {
            var stateProp = processManager.GetType().GetProperty("State");
            var state = stateProp?.GetValue(processManager);
            return state is null ? null : JsonSerializer.Serialize(state, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private sealed record SagaRow
    {
        public Guid Id { get; init; }

        public string Status { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }

        public DateTime? CompletedAt { get; init; }
    }

    private sealed record StepRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int Order { get; init; }

        public string Status { get; init; } = string.Empty;

        public DateTime? ExecutedAt { get; init; }

        public DateTime? CompensatedAt { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
