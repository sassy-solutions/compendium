// -----------------------------------------------------------------------
// <copyright file="PostgresProcessManagerStateReloadTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.Sagas;
using Compendium.Application.Sagas.ProcessManagers;
using Compendium.Core.Results;
using Compendium.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Compendium.IntegrationTests.EndToEnd.Scenarios;

/// <summary>
/// Validates the typed-state reload contract added in preview.5: a saga whose state was
/// persisted by <see cref="PostgresProcessManagerRepository.SaveAsync"/> can be reloaded
/// via <c>GetByIdAsync&lt;TState&gt;</c> with its full domain state intact. This is the
/// foundation for idempotent saga step retries — without it, resumption can't tell what
/// external work has already been done.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "Saga")]
public sealed class PostgresProcessManagerStateReloadTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _pg;
    private PostgresProcessManagerRepository _repository = null!;

    public PostgresProcessManagerStateReloadTests(PostgreSqlFixture pg)
    {
        _pg = pg;
    }

    public async Task InitializeAsync()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        var options = Options.Create(new PostgreSqlOptions { ConnectionString = _pg.ConnectionString });
        _repository = new PostgresProcessManagerRepository(options);
        await _repository.InitializeAsync();
        await _pg.CleanTableAsync("process_manager_steps");
        await _pg.CleanTableAsync("process_managers");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [RequiresDockerFact]
    public async Task GetByIdAsyncTyped_AfterSave_RestoresStateUntouched()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        var pm = ProvisionOrgProcessManager.Create("acme", "admin@acme.test");
        pm.State.ZitadelOrgId = "zit-001";
        pm.State.K8sNamespace = "acme-prod";
        pm.State.DbSchemaCreated = true;

        var save = await _repository.SaveAsync(pm);
        save.IsSuccess.Should().BeTrue();

        var loaded = await _repository.GetByIdAsync<ProvisionOrgState>(pm.Id);

        loaded.IsSuccess.Should().BeTrue();
        loaded.Value.Id.Should().Be(pm.Id);
        loaded.Value.State.OrgName.Should().Be("acme");
        loaded.Value.State.AdminEmail.Should().Be("admin@acme.test");
        loaded.Value.State.ZitadelOrgId.Should().Be("zit-001");
        loaded.Value.State.K8sNamespace.Should().Be("acme-prod");
        loaded.Value.State.DbSchemaCreated.Should().BeTrue();
        loaded.Value.Steps.Should().HaveCount(3);
    }

    [RequiresDockerFact]
    public async Task GetByIdAsyncTyped_NotFound_ReturnsNotFoundError()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        var loaded = await _repository.GetByIdAsync<ProvisionOrgState>(Guid.NewGuid());

        loaded.IsFailure.Should().BeTrue();
        loaded.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [RequiresDockerFact]
    public async Task GetByIdAsyncTyped_StateWritesAfterLoadDoNotEscapeBack()
    {
        // The reload returns a snapshot; mutating it without calling SaveAsync must not
        // round-trip into the database. This confirms the rehydrated instance is decoupled
        // from the persisted row — the orchestrator stays the only writer.
        if (!_pg.IsAvailable)
        {
            return;
        }

        var pm = ProvisionOrgProcessManager.Create("widget", "owner@widget.test");
        await _repository.SaveAsync(pm);

        var firstLoad = await _repository.GetByIdAsync<ProvisionOrgState>(pm.Id);
        firstLoad.Value.State.ZitadelOrgId = "leaked";

        var secondLoad = await _repository.GetByIdAsync<ProvisionOrgState>(pm.Id);
        secondLoad.Value.State.ZitadelOrgId.Should().BeNull();
    }

    [RequiresDockerFact]
    public async Task GetByIdAsyncTyped_AfterCrashAndResume_StateRestored()
    {
        // Simulates the saga reuse case: step 1 completes and persists state, the host
        // crashes, a new repository instance reloads — the typed state must let step 2
        // detect that step 1's external work is already done and skip duplicates.
        if (!_pg.IsAvailable)
        {
            return;
        }

        var pm = ProvisionOrgProcessManager.Create("foo", "bar@foo.test");
        pm.State.ZitadelOrgId = "zit-foo-001";
        pm.State.AdminUserId = "user-007";
        await _repository.SaveAsync(pm);

        // Fresh repository instance — mirrors a process restart.
        var freshRepo = new PostgresProcessManagerRepository(
            Options.Create(new PostgreSqlOptions { ConnectionString = _pg.ConnectionString }));

        var resumed = await freshRepo.GetByIdAsync<ProvisionOrgState>(pm.Id);
        resumed.IsSuccess.Should().BeTrue();
        resumed.Value.State.ZitadelOrgId.Should().Be("zit-foo-001");
        resumed.Value.State.AdminUserId.Should().Be("user-007");
    }

    private sealed class ProvisionOrgState
    {
        public string OrgName { get; set; } = string.Empty;

        public string AdminEmail { get; set; } = string.Empty;

        public string? ZitadelOrgId { get; set; }

        public string? AdminUserId { get; set; }

        public string? K8sNamespace { get; set; }

        public bool DbSchemaCreated { get; set; }
    }

    private sealed class ProvisionOrgProcessManager : ProcessManager<ProvisionOrgState>
    {
        private ProvisionOrgProcessManager(Guid id, ProvisionOrgState state)
            : base(id, state, new[] { "ProvisionZitadel", "CreateDbSchema", "CreateK8sNamespace" })
        {
        }

        public static ProvisionOrgProcessManager Create(string orgName, string adminEmail) =>
            new(
                Guid.NewGuid(),
                new ProvisionOrgState { OrgName = orgName, AdminEmail = adminEmail });
    }
}
