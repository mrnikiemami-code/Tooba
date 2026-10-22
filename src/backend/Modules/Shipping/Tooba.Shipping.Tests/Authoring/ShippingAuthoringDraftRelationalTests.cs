using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Persistence;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1 — R190-03 proof on a real PostgreSQL database.
/// </summary>
/// <remarks>
/// <para>
/// The connection string comes from <c>TOOBA_TB190R1_POSTGRES</c> and must point at the task-owned
/// disposable instance (127.0.0.1:55432, database <c>shipping_tb190r1</c>). Without it every fact is
/// skipped: no shared :5432 / tooba_alpha server is ever touched, and no test pretends to be relational
/// when it is not.
/// </para>
/// <para>
/// These tests execute the real <see cref="ShippingAuthoringDraftDirectory"/>, the real
/// <see cref="ShippingDbContext"/> over Npgsql, and the real module migrations. Only the Fulfillment
/// method validator and the destination resolver are labelled fixtures, because the catalog and the
/// Geography authority are not the subject of this repair.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringDraftRelationalTests
{
    private const string ConnectionVariable = "TOOBA_TB190R1_POSTGRES";
    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;

    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Qom = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public async Task Reorder_delete_shift_insert_before_and_node_move_preserve_identity_and_final_uniqueness()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb190r1-" + Guid.NewGuid().ToString("N"));
        var directory = new ShippingAuthoringDraftDirectory(context);

        var created = await directory.CreateAsync(
            owner,
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Qom)]),
            ],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);

        var draftId = created.Value.DraftId;
        var groups = created.Value.RegionGroups.OrderBy(group => group.Order).ToArray();
        var groupA = groups[0].GroupId;
        var groupB = groups[1].GroupId;
        var groupC = groups[2].GroupId;
        var tehranAreaRowId = await AreaRowIdAsync(context, draftId, Tehran);

        // 1) Pure swap: A@0,B@1,C@2 -> C@0,B@1,A@2. With an immediate UNIQUE(draft_id, group_order) there
        //    is no legal first ordinary UPDATE unless the write is staged inside one transaction.
        var swapped = await directory.ReplaceRegionGroupsAsync(
            owner,
            draftId,
            1,
            [
                new ShippingAuthoringRegionGroupInput(groupC, [Node(Qom)]),
                new ShippingAuthoringRegionGroupInput(groupB, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(groupA, [Node(Tehran)]),
            ],
            CancellationToken.None);
        Assert.True(swapped.IsSuccess, swapped.IsFailure ? swapped.Errors[0].Code : null);
        Assert.Equal(2, swapped.Value.Revision);
        Assert.Equal(
            [groupC, groupB, groupA],
            swapped.Value.RegionGroups.OrderBy(group => group.Order).Select(group => group.GroupId).ToArray());
        Assert.Equal([0, 1, 2], swapped.Value.RegionGroups.OrderBy(group => group.Order).Select(group => group.Order).ToArray());
        await AssertNoStagedOrdersAsync(context, draftId);

        // 2) Delete the first group: the later groups shift 1->0, 2->1 with stable identities.
        var shifted = await directory.ReplaceRegionGroupsAsync(
            owner,
            draftId,
            2,
            [
                new ShippingAuthoringRegionGroupInput(groupB, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(groupA, [Node(Tehran)]),
            ],
            CancellationToken.None);
        Assert.True(shifted.IsSuccess, shifted.IsFailure ? shifted.Errors[0].Code : null);
        Assert.Equal(
            [groupB, groupA],
            shifted.Value.RegionGroups.OrderBy(group => group.Order).Select(group => group.GroupId).ToArray());
        Assert.Equal([0, 1], shifted.Value.RegionGroups.OrderBy(group => group.Order).Select(group => group.Order).ToArray());

        // 3) Insert a brand-new group BEFORE the retained ones.
        var inserted = await directory.ReplaceRegionGroupsAsync(
            owner,
            draftId,
            3,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Qom)]),
                new ShippingAuthoringRegionGroupInput(groupB, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(groupA, [Node(Tehran)]),
            ],
            CancellationToken.None);
        Assert.True(inserted.IsSuccess, inserted.IsFailure ? inserted.Errors[0].Code : null);
        var ordered = inserted.Value.RegionGroups.OrderBy(group => group.Order).ToArray();
        Assert.Equal([0, 1, 2], ordered.Select(group => group.Order).ToArray());
        Assert.Equal(groupB, ordered[1].GroupId);
        Assert.Equal(groupA, ordered[2].GroupId);
        Assert.DoesNotContain(ordered[0].GroupId, new[] { groupA, groupB, groupC });
        await AssertNoStagedOrdersAsync(context, draftId);

        // 4) Move an existing selected node between retained groups: the SAME area row is reparented, so
        //    the draft-wide (draft_id, node_kind, node_id) uniqueness is never transiently violated.
        var nodeMove = await directory.ReplaceRegionGroupsAsync(
            owner,
            draftId,
            4,
            [
                new ShippingAuthoringRegionGroupInput(ordered[0].GroupId, [Node(Qom)]),
                new ShippingAuthoringRegionGroupInput(groupB, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(groupA, []),
            ],
            CancellationToken.None);
        Assert.True(nodeMove.IsSuccess, nodeMove.IsFailure ? nodeMove.Errors[0].Code : null);
        var finalGroups = nodeMove.Value.RegionGroups.OrderBy(group => group.Order).ToArray();
        Assert.Equal([0, 1, 2], finalGroups.Select(group => group.Order).ToArray());
        Assert.Equal(groupB, finalGroups[1].GroupId);
        Assert.Equal(Tehran, finalGroups[1].Areas.Single().NodeId);
        Assert.Equal(groupA, finalGroups[2].GroupId);
        Assert.Empty(finalGroups[2].Areas);

        // Row identity survived the move: same selected-area row, new owning group.
        var movedRow = await context.Set<ShippingAuthoringSelectedArea>()
            .AsNoTracking()
            .SingleAsync(area => area.DraftId == draftId && area.NodeId == Tehran);
        Assert.Equal(tehranAreaRowId, movedRow.SelectedAreaId);
        Assert.Equal(groupB, movedRow.GroupId);

        // Draft-wide uniqueness survives every step.
        var duplicateNodes = await context.Set<ShippingAuthoringSelectedArea>()
            .AsNoTracking()
            .Where(area => area.DraftId == draftId)
            .GroupBy(area => new { area.NodeKind, area.NodeId })
            .CountAsync(group => group.Count() > 1);
        Assert.Equal(0, duplicateNodes);
        await AssertNoStagedOrdersAsync(context, draftId);
    }

    [Fact]
    public async Task A_same_revision_race_leaves_exactly_one_winner_and_no_partial_group_or_area_writes()
    {
        await using var seedContext = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb190r1-race-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seedContext).CreateAsync(
            owner,
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
            ],
            CancellationToken.None);
        Assert.True(created.IsSuccess);
        var draftId = created.Value.DraftId;
        var first = created.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId;
        var second = created.Value.RegionGroups.OrderBy(group => group.Order).Last().GroupId;

        await using var contextA = await NewContextAsync();
        await using var contextB = await NewContextAsync();

        var results = await Task.WhenAll(
            new ShippingAuthoringDraftDirectory(contextA).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)])],
                CancellationToken.None),
            new ShippingAuthoringDraftDirectory(contextB).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [
                    new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
                    new ShippingAuthoringRegionGroupInput(null, [Node(Qom)]),
                ],
                CancellationToken.None));

        Assert.Single(results, result => !result.IsFailure);
        var loser = Assert.Single(results, result => result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Errors[0].Code);

        await using var verification = await NewContextAsync();
        var stored = await verification.AuthoringDrafts
            .AsNoTracking()
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.Areas)
            .SingleAsync(draft => draft.DraftId == draftId);
        Assert.Equal(2, stored.Revision);
        Assert.Single(stored.RegionGroups);
        Assert.Equal(Isfahan, stored.RegionGroups.Single().Areas.Single().NodeId);
        Assert.DoesNotContain(stored.RegionGroups, group => group.Areas.Any(area => area.NodeId == Qom));
        Assert.Equal(0, stored.RegionGroups.Single().Order);
        await AssertNoStagedOrdersAsync(verification, draftId, includeDraftRevision: false);

        // The losing scoped context holds no pending edit that a later SaveChanges could publish.
        Assert.Empty(contextB.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
    }

    [Fact]
    public async Task A_failure_after_the_first_stage_rolls_the_whole_replacement_back()
    {
        await using var seedContext = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb190r1-rollback-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seedContext).CreateAsync(
            owner,
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
            ],
            CancellationToken.None);
        Assert.True(created.IsSuccess);
        var draftId = created.Value.DraftId;
        var first = created.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId;
        var second = created.Value.RegionGroups.OrderBy(group => group.Order).Last().GroupId;

        // A test-owned interceptor fails the SECOND SaveChanges, i.e. after the staged orders/area moves
        // were already written inside the replacement transaction. Only a real one-transaction boundary
        // can leave the previous tree intact here.
        var failure = new FailOnSecondSaveInterceptor();
        await using var context = await NewContextAsync(failure);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ShippingAuthoringDraftDirectory(context).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [
                    new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)]),
                    new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
                ],
                CancellationToken.None));
        Assert.Equal(2, failure.SaveAttempts);

        await using var verification = await NewContextAsync();
        var stored = await verification.AuthoringDrafts
            .AsNoTracking()
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.Areas)
            .SingleAsync(draft => draft.DraftId == draftId);
        Assert.Equal(1, stored.Revision);
        Assert.Equal(
            [first, second],
            stored.RegionGroups.OrderBy(group => group.Order).Select(group => group.GroupId).ToArray());
        Assert.Equal([0, 1], stored.RegionGroups.OrderBy(group => group.Order).Select(group => group.Order).ToArray());
        Assert.Equal(Tehran, stored.RegionGroups.Single(group => group.GroupId == first).Areas.Single().NodeId);
        await AssertNoStagedOrdersAsync(verification, draftId, includeDraftRevision: false);

        // Nothing half-written: a valid replacement from the untouched revision still succeeds.
        var retried = await new ShippingAuthoringDraftDirectory(verification).ReplaceRegionGroupsAsync(
            owner,
            draftId,
            1,
            [
                new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
            ],
            CancellationToken.None);
        Assert.True(retried.IsSuccess, retried.IsFailure ? retried.Errors[0].Code : null);
        Assert.Equal(2, retried.Value.Revision);
    }

    [Fact]
    public async Task The_real_handler_creates_replaces_and_refuses_a_stale_revision_through_npgsql()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb190r1-handler-" + Guid.NewGuid().ToString("N"));
        var directory = new ShippingAuthoringDraftDirectory(context);
        var create = new CreateShippingAuthoringDraftHandler(directory, new FakeResolver(), new FakeValidator());

        var created = await create.Handle(
            new CreateShippingAuthoringDraftCommand(
                owner,
                new ShippingAuthoringDraftCreateRequest(
                    "post",
                    null,
                    [new ShippingAuthoringRegionGroupWrite(null, [Area(Tehran)])])),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);

        var replace = new ReplaceShippingAuthoringDraftRegionsHandler(directory, new FakeResolver(), new FakeValidator());
        var replaced = await replace.Handle(
            new ReplaceShippingAuthoringDraftRegionsCommand(
                owner,
                created.Value.DraftId,
                new ShippingAuthoringDraftReplaceRegionsRequest(
                    1,
                    [new ShippingAuthoringRegionGroupWrite(null, [Area(Isfahan)])])),
            CancellationToken.None);
        Assert.True(replaced.IsSuccess, replaced.IsFailure ? replaced.Errors[0].Code : null);
        Assert.Equal(2, replaced.Value.Revision);
        Assert.Equal(Isfahan, replaced.Value.RegionGroups.Single().Areas.Single().Id);

        // Stale retry -> canonical 409, stored revision untouched.
        var stale = await replace.Handle(
            new ReplaceShippingAuthoringDraftRegionsCommand(
                owner,
                created.Value.DraftId,
                new ShippingAuthoringDraftReplaceRegionsRequest(
                    1,
                    [new ShippingAuthoringRegionGroupWrite(null, [Area(Tehran)])])),
            CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, stale.Errors[0].Code);

        await using var verification = await NewContextAsync();
        Assert.Equal(
            2,
            await verification.AuthoringDrafts
                .AsNoTracking()
                .Where(draft => draft.DraftId == created.Value.DraftId)
                .Select(draft => draft.Revision)
                .SingleAsync());
    }

    private static Task<Guid> AreaRowIdAsync(ShippingDbContext context, Guid draftId, Guid nodeId)
        => context.Set<ShippingAuthoringSelectedArea>()
            .AsNoTracking()
            .Where(area => area.DraftId == draftId && area.NodeId == nodeId)
            .Select(area => area.SelectedAreaId)
            .SingleAsync();

    private static async Task AssertNoStagedOrdersAsync(
        ShippingDbContext context,
        Guid draftId,
        bool includeDraftRevision = true)
    {
        var hasStaged = await context.Set<ShippingAuthoringRegionGroup>()
            .AsNoTracking()
            .AnyAsync(group => group.DraftId == draftId && group.Order >= ShippingAuthoringDraft.StagedOrderBase);
        Assert.False(hasStaged, "a staged temporary order leaked into the committed tree");

        if (includeDraftRevision)
        {
            var revision = await context.AuthoringDrafts
                .AsNoTracking()
                .Where(draft => draft.DraftId == draftId)
                .Select(draft => draft.Revision)
                .SingleAsync();
            Assert.True(revision >= 2);
        }
    }

    private static async Task<ShippingDbContext> NewContextAsync(SaveChangesInterceptor? interceptor = null)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{ConnectionVariable} must point at the disposable R190-03 PostgreSQL instance; refusing to fake relational proof.");
        }

        // Fail closed: this suite only ever runs against the task-owned loopback instance.
        Assert.Contains("127.0.0.1", connectionString!, StringComparison.Ordinal);
        Assert.Contains("shipping_tb190r1", connectionString!, StringComparison.Ordinal);
        Assert.DoesNotContain("tooba_alpha", connectionString!, StringComparison.OrdinalIgnoreCase);

        // The production module configuration is used verbatim: same snake_case convention, same
        // schema-owned migration history table, same migrations assembly. A plain UseNpgsql would query
        // an unmapped PascalCase schema and prove nothing about the real mapping.
        var builder = new DbContextOptionsBuilder<ShippingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder,
            connectionString!,
            ShippingDbContext.Schema,
            typeof(ShippingDbContext));
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        var context = new ShippingDbContext(builder.Options);

        await MigrationGate.WaitAsync();
        try
        {
            if (!_migrated)
            {
                await context.GetService<IMigrator>().MigrateAsync();
                _migrated = true;
            }
        }
        finally
        {
            MigrationGate.Release();
        }

        return context;
    }

    private static ShippingDestinationNode Node(Guid id)
        => new(ShippingDestinationNodeKind.AdministrativeArea, id);

    private static ShippingAuthoringSelectedAreaWrite Area(Guid administrativeAreaId)
        => new(ShippingAuthoringAreaKind.AdministrativeArea, Iran, administrativeAreaId);

    private sealed class FailOnSecondSaveInterceptor : SaveChangesInterceptor
    {
        public int SaveAttempts { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (SaveAttempts >= 2)
            {
                throw new InvalidOperationException("tb190r1: injected failure after the staged write");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class FakeValidator : IShippingMethodOptionValidator
    {
        public Task<Result<bool>> ValidateAsync(string serviceCode, string? optionCode, CancellationToken cancellationToken)
            => Task.FromResult(string.Equals(serviceCode, "post", StringComparison.Ordinal)
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(new SemanticError(ShippingErrorCodes.ServiceOptionUnknown)));
    }

    private sealed class FakeResolver : IShippingAuthoringDestinationResolver
    {
        public Task<Result<IReadOnlyList<ShippingDestinationSelectionPath>>> ResolveAsync(
            IReadOnlyList<ShippingAuthoringSelectedAreaWrite> selections,
            CancellationToken cancellationToken)
            => Task.FromResult(Result<IReadOnlyList<ShippingDestinationSelectionPath>>.Success(
                selections
                    .Select(selection => ShippingDestinationSelectionPath.FromChain(
                        [
                            new ShippingDestinationNode(ShippingDestinationNodeKind.Country, selection.CountryId),
                            new ShippingDestinationNode(
                                ShippingDestinationNodeKind.AdministrativeArea,
                                selection.AdministrativeAreaId!.Value),
                        ]))
                    .ToArray()));
    }
}
