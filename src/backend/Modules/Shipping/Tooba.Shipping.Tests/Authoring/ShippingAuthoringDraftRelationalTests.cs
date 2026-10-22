using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
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
/// The connection string comes from <c>TOOBA_TB190R1_POSTGRES</c> and must name the task-owned disposable
/// instance exactly (<c>127.0.0.1:55432</c>, database <c>shipping_tb190r1</c>, role <c>tb190r1</c>), parsed
/// with <see cref="ShippingRelationalTestTarget"/> before any connection is opened. When the variable is
/// absent or names anything else these facts <b>fail closed</b>: they throw, they are never reported as
/// passed, and nothing is skipped. Running the whole suite therefore requires the isolated database to be
/// provisioned first, and the suite must not be executed after cleanup.
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
    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveTargetVerified;

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

        // Deterministic two-reader barrier: the interceptor arrives at the FIRST SavingChanges of each
        // participant, i.e. after that participant has read and prepared revision 1 and BEFORE either has
        // written anything. A bare BEGIN holds no row locks, so no participant waits while holding one.
        var gate = new TwoReaderGate();
        var interceptorA = new TwoReaderBarrierInterceptor(gate);
        var interceptorB = new TwoReaderBarrierInterceptor(gate);

        await using var contextA = await NewContextAsync(interceptorA);
        await using var contextB = await NewContextAsync(interceptorB);

        async Task<(int Index, Result<ShippingAuthoringDraft> Result)> RunAsync(
            int index,
            ShippingDbContext context,
            Func<CancellationToken, Task<Result<ShippingAuthoringDraft>>> run)
        {
            try
            {
                return (index, await run(CancellationToken.None));
            }
            catch (Exception exception)
            {
                throw new Xunit.Sdk.XunitException($"participant {index} threw {exception.GetType().Name}: {exception.Message}");
            }
        }

        var outcomes = await Task.WhenAll(
            RunAsync(0, contextA, token => new ShippingAuthoringDraftDirectory(contextA).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)])],
                token)),
            RunAsync(1, contextB, token => new ShippingAuthoringDraftDirectory(contextB).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [
                    new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
                    new ShippingAuthoringRegionGroupInput(null, [Node(Qom)]),
                ],
                token)));

        // Both really arrived with revision 1 loaded, and the barrier really synchronized them.
        Assert.Equal(2, gate.Arrivals);
        Assert.Equal(1, interceptorA.OriginalRevision);
        Assert.Equal(1, interceptorB.OriginalRevision);

        var winner = Assert.Single(outcomes, outcome => !outcome.Result.IsFailure);
        var loser = Assert.Single(outcomes, outcome => outcome.Result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Result.Errors[0].Code);

        await using var verification = await NewContextAsync();
        var stored = await verification.AuthoringDrafts
            .AsNoTracking()
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.Areas)
            .SingleAsync(draft => draft.DraftId == draftId);

        // The persisted tree is the WINNER's tree, whichever participant won, with revision advanced once.
        Assert.Equal(2, stored.Revision);
        Assert.Equal(
            Project(winner.Result.Value.RegionGroups),
            Project(stored.RegionGroups
                .OrderBy(group => group.Order)
                .Select(group => (
                    group.GroupId,
                    group.Order,
                    (IReadOnlyList<Guid>)group.Areas.Select(area => area.NodeId).OrderBy(id => id).ToArray()))));        await AssertNoStagedOrdersAsync(verification, draftId, includeDraftRevision: false);

        // The ACTUAL losing context holds no pending edit that a later SaveChanges could publish.
        var loserContext = loser.Index == 0 ? contextA : contextB;
        Assert.Empty(loserContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));

        // And its tree is still readable as the winner's, never a hybrid.
        var reread = await loserContext.AuthoringDrafts
            .AsNoTracking()
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.Areas)
            .SingleAsync(draft => draft.DraftId == draftId);
        Assert.Equal(2, reread.Revision);
    }

    [Fact]
    public async Task Caller_cancellation_after_the_first_stage_rolls_back_and_propagates()
    {
        await using var seedContext = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb190r1-cancel-" + Guid.NewGuid().ToString("N"));
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

        // The caller token is cancelled at the START of the SECOND SaveChanges, i.e. after the staged orders
        // and node moves were already written inside the replacement transaction and before the commit.
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CancelOnSecondSaveInterceptor(cancellation);
        await using var context = await NewContextAsync(interceptor);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ShippingAuthoringDraftDirectory(context).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [
                    new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)]),
                    new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
                ],
                cancellation.Token));

        Assert.Equal(2, interceptor.SaveAttempts);
        Assert.True(cancellation.IsCancellationRequested);

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

    private static string Project(
        IEnumerable<(Guid GroupId, int Order, IReadOnlyList<Guid> Nodes)> groups)
        => string.Join(
            "|",
            groups
                .OrderBy(group => group.Order)
                .Select(group => $"{group.GroupId}@{group.Order}[{string.Join(",", group.Nodes)}]"));

    private static string Project(IReadOnlyList<ShippingAuthoringRegionGroup> groups)
        => Project(groups.Select(group => (
            group.GroupId,
            group.Order,
            (IReadOnlyList<Guid>)group.Areas.Select(area => area.NodeId).OrderBy(id => id).ToArray())));

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

    [Fact]
    public async Task Rate_rows_roundtrip_bind_the_currency_and_share_the_root_revision_through_npgsql()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb191-rates-" + Guid.NewGuid().ToString("N"));
        var directory = new ShippingAuthoringDraftDirectory(context);

        var created = await directory.CreateAsync(
            owner,
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
            ],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var first = created.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId;
        var second = created.Value.RegionGroups.OrderBy(group => group.Order).Last().GroupId;

        // Two alternative rows on three axes; the amounts are canonical two-decimal values, not rounded.
        var saved = await directory.ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            first,
            1,
            [
                RowInput(null, 0, 1000, 15000.55m, 2000.25m, minDeliveryDays: 1, maxDeliveryDays: 2),
                RowInput(null, 1000, 5000, 30000m, 0m, 2, 4, minItemCount: 1, maxItemCount: 10,
                    minMerchandiseAmount: 100000m, maxMerchandiseAmount: 500000m),
            ],
            "IRR",
            CancellationToken.None);
        Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        Assert.Equal(2, saved.Value.Revision);
        Assert.Equal("IRR", saved.Value.FindRegionGroup(first)!.CurrencyCode);

        var storedRows = await StoredRowsAsync(context, first);
        Assert.Equal(2, storedRows.Count);
        var unconditioned = storedRows.Single(row => row.MinWeightGrams == 0);
        Assert.Equal(1000, unconditioned.MaxWeightGrams);
        Assert.Null(unconditioned.MinItemCount);
        Assert.Null(unconditioned.MinMerchandiseAmount);
        Assert.Equal(15000.55m, unconditioned.FreightAmount);
        Assert.Equal(2000.25m, unconditioned.PackagingAmount);
        Assert.Equal(1, unconditioned.MinDeliveryDays);
        Assert.Equal(2, unconditioned.MaxDeliveryDays);
        var conditioned = storedRows.Single(row => row.MinWeightGrams == 1000);
        Assert.Equal(1, conditioned.MinItemCount);
        Assert.Equal(10, conditioned.MaxItemCount);
        Assert.Equal(100000m, conditioned.MinMerchandiseAmount);
        Assert.Equal(500000m, conditioned.MaxMerchandiseAmount);

        // The currency binds only on the group that got rows; the untouched group keeps nothing.
        Assert.Equal("IRR", await StoredCurrencyAsync(context, first));
        Assert.Null(await StoredCurrencyAsync(context, second));
        Assert.Empty(await StoredRowsAsync(context, second));

        // A retained row id is real identity: same rate row survives the replacement of its values.
        var kept = conditioned.RateRowId;
        var replaced = await directory.ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            first,
            2,
            [
                RowInput(kept, 1000, 5000, 31000m, 0m, 3, 5),
                RowInput(null, 5000, 9000, 45000m, 1000m, 5, 7),
            ],
            "IRR",
            CancellationToken.None);
        Assert.True(replaced.IsSuccess, replaced.IsFailure ? replaced.Errors[0].Code : null);
        Assert.Equal(3, replaced.Value.Revision);
        var afterReplace = await StoredRowsAsync(context, first);
        Assert.Equal(2, afterReplace.Count);
        Assert.Contains(afterReplace, row => row.RateRowId == kept);
        Assert.Equal(31000m, afterReplace.Single(row => row.RateRowId == kept).FreightAmount);
        Assert.Equal(3, afterReplace.Single(row => row.RateRowId == kept).MinDeliveryDays);

        // Removing every row clears the rows but never unbinds the stored currency.
        var cleared = await directory.ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            first,
            3,
            [],
            "IRR",
            CancellationToken.None);
        Assert.True(cleared.IsSuccess, cleared.IsFailure ? cleared.Errors[0].Code : null);
        Assert.Equal(4, cleared.Value.Revision);
        Assert.Empty(await StoredRowsAsync(context, first));
        Assert.Equal("IRR", await StoredCurrencyAsync(context, first));

        // A different trusted currency is a configuration mismatch: fail closed, nothing written.
        var mismatch = Assert.Throws<SemanticException>(() => directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                first,
                4,
                [RowInput(null, 0, 1000, 1000m, 0m)],
                "USD",
                CancellationToken.None)
            .GetAwaiter()
            .GetResult());
        Assert.Equal(ShippingErrorCodes.AuthoringRateCurrencyMismatch, mismatch.Error.Code);
        Assert.Empty(await StoredRowsAsync(context, first));
        Assert.Equal("IRR", await StoredCurrencyAsync(context, first));
        Assert.Equal(4, await StoredRevisionAsync(context, draftId));
    }

    [Fact]
    public async Task The_rate_row_write_is_one_atomic_boundary_and_a_failed_save_publishes_nothing()
    {
        await using var seedContext = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb191-atomic-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seedContext).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess);
        var draftId = created.Value.DraftId;
        var groupId = created.Value.RegionGroups.Single().GroupId;
        var baseline = await new ShippingAuthoringDraftDirectory(seedContext).ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            groupId,
            1,
            [RowInput(null, 0, 1000, 9000m, 0m)],
            "IRR",
            CancellationToken.None);
        Assert.True(baseline.IsSuccess, baseline.IsFailure ? baseline.Errors[0].Code : null);
        Assert.Equal(2, baseline.Value.Revision);

        // A provider failure inside the single SaveChanges must leave the previous rows and revision intact,
        // because rows and the root revision share one implicit transaction.
        var failure = new FailOnNextSaveInterceptor();
        await using var failing = await NewContextAsync(failure);
        failure.Arm();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ShippingAuthoringDraftDirectory(failing).ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groupId,
                2,
                [
                    RowInput(null, 0, 500, 1000m, 0m),
                    RowInput(null, 500, 1000, 2000m, 0m),
                ],
                "IRR",
                CancellationToken.None));
        Assert.Equal(1, failure.SaveAttempts);

        await using var verification = await NewContextAsync();
        var stored = await StoredRowsAsync(verification, groupId);
        Assert.Single(stored);
        Assert.Equal(9000m, stored[0].FreightAmount);
        Assert.Equal(2, await StoredRevisionAsync(verification, draftId));
        Assert.Empty(failing.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));

        // Nothing half-written: the same replacement from the untouched revision still succeeds once.
        var retried = await new ShippingAuthoringDraftDirectory(verification).ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            groupId,
            2,
            [
                RowInput(null, 0, 500, 1000m, 0m),
                RowInput(null, 500, 1000, 2000m, 0m),
            ],
            "IRR",
            CancellationToken.None);
        Assert.True(retried.IsSuccess, retried.IsFailure ? retried.Errors[0].Code : null);
        Assert.Equal(3, retried.Value.Revision);
        Assert.Equal(2, (await StoredRowsAsync(verification, groupId)).Count);
    }

    [Fact]
    public async Task A_same_revision_rate_row_race_leaves_exactly_one_winner_and_only_its_rows()
    {
        await using var seedContext = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb191-race-" + Guid.NewGuid().ToString("N"));
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

        // Both participants read revision 1 (root revision and rows), then meet at their single SavingChanges.
        var gate = new TwoReaderGate();
        await using var contextA = await NewContextAsync(new TwoReaderBarrierInterceptor(gate));
        await using var contextB = await NewContextAsync(new TwoReaderBarrierInterceptor(gate));

        async Task<(int Index, Result<ShippingAuthoringDraft> Result)> RunAsync(
            int index,
            ShippingDbContext context,
            Guid groupId,
            ShippingAuthoringDraft.ShippingAuthoringRateRowInput[] rows)
        {
            try
            {
                return (index, await new ShippingAuthoringDraftDirectory(context).ReplaceGroupRateRowsAsync(
                    owner,
                    draftId,
                    groupId,
                    1,
                    rows,
                    "IRR",
                    CancellationToken.None));
            }
            catch (Exception exception)
            {
                throw new Xunit.Sdk.XunitException($"participant {index} threw {exception.GetType().Name}: {exception.Message}");
            }
        }

        var outcomes = await Task.WhenAll(
            RunAsync(0, contextA, first, [RowInput(null, 0, 1000, 1111m, 0m)]),
            RunAsync(1, contextB, first, [RowInput(null, 2000, 3000, 2222m, 0m)]));

        Assert.Equal(2, gate.Arrivals);

        var winner = Assert.Single(outcomes, outcome => !outcome.Result.IsFailure);
        var loser = Assert.Single(outcomes, outcome => outcome.Result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Result.Errors[0].Code);

        await using var verification = await NewContextAsync();
        Assert.Equal(2, await StoredRevisionAsync(verification, draftId));
        var stored = await StoredRowsAsync(verification, first);
        Assert.Single(stored);
        Assert.Equal(
            winner.Index == 0 ? 1111m : 2222m,
            stored[0].FreightAmount);
        var losingContext = loser.Index == 0 ? contextA : contextB;
        Assert.Empty(losingContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        Assert.Empty(await StoredRowsAsync(verification, second));
    }

    [Fact]
    public async Task Retained_group_rate_rows_survive_a_region_reorder_and_are_deleted_with_their_group()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb191-cascade-" + Guid.NewGuid().ToString("N"));
        var directory = new ShippingAuthoringDraftDirectory(context);

        var created = await directory.CreateAsync(
            owner,
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
            ],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var first = created.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId;
        var second = created.Value.RegionGroups.OrderBy(group => group.Order).Last().GroupId;

        var saved = await directory.ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            first,
            1,
            [RowInput(null, 0, 1000, 12000m, 500m), RowInput(null, 1000, 4000, 24000m, 0m)],
            "IRR",
            CancellationToken.None);
        Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        var rowIds = (await StoredRowsAsync(context, first)).Select(row => row.RateRowId).OrderBy(id => id).ToArray();

        // Reordering the tree keeps the group identity, so its rows and currency are untouched.
        var reordered = await directory.ReplaceRegionGroupsAsync(
            owner,
            draftId,
            2,
            [
                new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
            ],
            CancellationToken.None);
        Assert.True(reordered.IsSuccess, reordered.IsFailure ? reordered.Errors[0].Code : null);
        Assert.Equal(3, reordered.Value.Revision);
        Assert.Equal(1, reordered.Value.FindRegionGroup(first)!.Order);
        Assert.Equal(
            rowIds,
            (await StoredRowsAsync(context, first)).Select(row => row.RateRowId).OrderBy(id => id).ToArray());
        Assert.Equal("IRR", await StoredCurrencyAsync(context, first));

        // Deleting the owning group removes exactly its own dependent rows.
        var dropped = await directory.ReplaceRegionGroupsAsync(
            owner,
            draftId,
            3,
            [new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)])],
            CancellationToken.None);
        Assert.True(dropped.IsSuccess, dropped.IsFailure ? dropped.Errors[0].Code : null);
        Assert.Equal(4, dropped.Value.Revision);
        Assert.Empty(await StoredRowsAsync(context, first));
        Assert.Empty(await StoredRowsAsync(context, second));
        Assert.Equal(0, await context.Set<ShippingAuthoringRateRow>().AsNoTracking()
            .CountAsync(row => row.GroupId == first));
        await AssertNoStagedOrdersAsync(context, draftId, includeDraftRevision: false);
    }

    private static Task<List<ShippingAuthoringRateRow>> StoredRowsAsync(ShippingDbContext context, Guid groupId)
        => context.Set<ShippingAuthoringRateRow>()
            .AsNoTracking()
            .Where(row => row.GroupId == groupId)
            .OrderBy(row => row.MinWeightGrams)
            .ThenBy(row => row.FreightAmount)
            .ToListAsync();

    private static Task<string?> StoredCurrencyAsync(ShippingDbContext context, Guid groupId)
        => context.Set<ShippingAuthoringRegionGroup>()
            .AsNoTracking()
            .Where(group => group.GroupId == groupId)
            .Select(group => group.CurrencyCode)
            .SingleAsync();

    private static Task<long> StoredRevisionAsync(ShippingDbContext context, Guid draftId)
        => context.AuthoringDrafts
            .AsNoTracking()
            .Where(draft => draft.DraftId == draftId)
            .Select(draft => draft.Revision)
            .SingleAsync();

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput RowInput(
        Guid? rateRowId,
        long? minWeightGrams,
        long? maxWeightGrams,
        decimal freightAmount,
        decimal packagingAmount = 0m,
        int minDeliveryDays = 1,
        int maxDeliveryDays = 3,
        int? minItemCount = null,
        int? maxItemCount = null,
        decimal? minMerchandiseAmount = null,
        decimal? maxMerchandiseAmount = null)
        => new(
            rateRowId,
            minWeightGrams,
            maxWeightGrams,
            minItemCount,
            maxItemCount,
            minMerchandiseAmount,
            maxMerchandiseAmount,
            freightAmount,
            packagingAmount,
            minDeliveryDays,
            maxDeliveryDays);

    [Fact]
    public async Task A_same_revision_rate_write_racing_a_region_edit_leaves_exactly_the_winner_state()
    {
        await using var seed = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb191r1-xop-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
            owner,
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
            ],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var first = created.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId;
        var second = created.Value.RegionGroups.OrderBy(group => group.Order).Last().GroupId;
        var initialTree = Project(created.Value.RegionGroups);

        // Two DIFFERENT operations share the root revision: a whole rate-row replacement on the first
        // group and a region-tree replacement that reorders and edits selections. Both arrive after
        // loading revision 1 and before either writes; a bare BEGIN holds no row lock.
        var gate = new TwoReaderGate();
        await using var contextA = await NewContextAsync(new TwoReaderBarrierInterceptor(gate));
        await using var contextB = await NewContextAsync(new TwoReaderBarrierInterceptor(gate));

        async Task<(int Index, Result<ShippingAuthoringDraft> Result)> RunAsync(
            int index,
            ShippingDbContext context,
            Func<CancellationToken, Task<Result<ShippingAuthoringDraft>>> run)
        {
            try
            {
                return (index, await run(CancellationToken.None));
            }
            catch (Exception exception)
            {
                throw new Xunit.Sdk.XunitException($"participant {index} threw {exception.GetType().Name}: {exception.Message}");
            }
        }

        var outcomes = await Task.WhenAll(
            RunAsync(0, contextA, token => new ShippingAuthoringDraftDirectory(contextA).ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                first,
                1,
                [RowInput(null, 0, 1000, 1111m, 0m)],
                "IRR",
                token)),
            RunAsync(1, contextB, token => new ShippingAuthoringDraftDirectory(contextB).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [
                    new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan), Node(Qom)]),
                    new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
                ],
                token)));

        Assert.Equal(2, gate.Arrivals);

        var winner = Assert.Single(outcomes, outcome => !outcome.Result.IsFailure);
        var loser = Assert.Single(outcomes, outcome => outcome.Result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Result.Errors[0].Code);

        await using var verification = await NewContextAsync();
        var stored = await verification.AuthoringDrafts
            .AsNoTracking()
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.Areas)
            .SingleAsync(draft => draft.DraftId == draftId);
        Assert.Equal(2, stored.Revision);

        // The stored tree is the WINNER's tree, and the losing operation left nothing behind.
        var storedRows = await StoredRowsAsync(verification, first);
        if (winner.Index == 0)
        {
            Assert.Equal(initialTree, Project(stored.RegionGroups));
            Assert.Single(storedRows);
            Assert.Equal(1111m, storedRows[0].FreightAmount);
            Assert.Equal("IRR", await StoredCurrencyAsync(verification, first));
        }
        else
        {
            Assert.NotEqual(initialTree, Project(stored.RegionGroups));
            var ordered = stored.RegionGroups.OrderBy(group => group.Order).ToArray();
            Assert.Equal([0, 1], ordered.Select(group => group.Order).ToArray());
            Assert.Equal(second, ordered[0].GroupId);
            Assert.Equal(
                new[] { Isfahan, Qom }.OrderBy(id => id).ToArray(),
                ordered[0].Areas.Select(area => area.NodeId).OrderBy(id => id).ToArray());
            Assert.Equal(first, ordered[1].GroupId);
            Assert.Equal(Tehran, ordered[1].Areas.Single().NodeId);
            Assert.Empty(storedRows);
            Assert.Null(await StoredCurrencyAsync(verification, first));
        }

        var losingContext = loser.Index == 0 ? contextA : contextB;
        Assert.Empty(losingContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        await AssertNoStagedOrdersAsync(verification, draftId, includeDraftRevision: false);
    }

    [Fact]
    public async Task A_same_revision_rate_write_racing_a_region_removal_never_leaves_orphan_rows()
    {
        await using var seed = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb191r1-xdel-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
            owner,
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
            ],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var first = created.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId;
        var second = created.Value.RegionGroups.OrderBy(group => group.Order).Last().GroupId;

        var gate = new TwoReaderGate();
        await using var contextA = await NewContextAsync(new TwoReaderBarrierInterceptor(gate));
        await using var contextB = await NewContextAsync(new TwoReaderBarrierInterceptor(gate));

        async Task<(int Index, Result<ShippingAuthoringDraft> Result)> RunAsync(
            int index,
            ShippingDbContext context,
            Func<CancellationToken, Task<Result<ShippingAuthoringDraft>>> run)
        {
            try
            {
                return (index, await run(CancellationToken.None));
            }
            catch (Exception exception)
            {
                throw new Xunit.Sdk.XunitException($"participant {index} threw {exception.GetType().Name}: {exception.Message}");
            }
        }

        // A writes rates to the FIRST group; B removes exactly that group and keeps only the second.
        var outcomes = await Task.WhenAll(
            RunAsync(0, contextA, token => new ShippingAuthoringDraftDirectory(contextA).ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                first,
                1,
                [RowInput(null, 0, 1000, 4321m, 0m)],
                "IRR",
                token)),
            RunAsync(1, contextB, token => new ShippingAuthoringDraftDirectory(contextB).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)])],
                token)));

        Assert.Equal(2, gate.Arrivals);
        var winner = Assert.Single(outcomes, outcome => !outcome.Result.IsFailure);
        var loser = Assert.Single(outcomes, outcome => outcome.Result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Result.Errors[0].Code);

        await using var verification = await NewContextAsync();
        var stored = await verification.AuthoringDrafts
            .AsNoTracking()
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.Areas)
            .SingleAsync(draft => draft.DraftId == draftId);
        Assert.Equal(2, stored.Revision);

        var rows = await verification.Set<ShippingAuthoringRateRow>()
            .AsNoTracking()
            .Where(row => row.GroupId == first || row.GroupId == second)
            .ToListAsync();

        if (winner.Index == 0)
        {
            // The rate write won: the group survives with its row, and the removal never happened.
            Assert.Equal([first, second], stored.RegionGroups.OrderBy(g => g.Order).Select(g => g.GroupId).ToArray());
            Assert.Single(rows);
            Assert.Equal(first, rows[0].GroupId);
            Assert.Equal(4321m, rows[0].FreightAmount);
        }
        else
        {
            // The removal won: the group is gone and NO orphan rate row survives it.
            Assert.Equal([second], stored.RegionGroups.Select(g => g.GroupId).ToArray());
            Assert.Empty(rows);
        }

        var losingContext = loser.Index == 0 ? contextA : contextB;
        Assert.Empty(losingContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        await AssertNoStagedOrdersAsync(verification, draftId, includeDraftRevision: false);
    }

    [Fact]
    public async Task Raw_storage_rejects_every_shape_the_domain_rejects_and_accepts_the_valid_boundaries()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb191r1-raw-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(context).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var groupId = created.Value.RegionGroups.Single().GroupId;

        // R191-01: each axis with min=NULL and a positive max is UNKNOWN in the old predicate and was
        // accepted by PostgreSQL. Every attempt below is parameterized raw SQL that bypasses the
        // aggregate and transport entirely, runs in its own rolled-back transaction, and asserts the
        // exact owned constraint identity (SQLSTATE 23514), not merely "something threw".
        await AssertRawRejectedAsync(context, groupId,
            "ck_authoring_rate_rows_weight_interval", new RawRow(null, 500, null, null, null, null));
        await AssertRawRejectedAsync(context, groupId,
            "ck_authoring_rate_rows_count_interval", new RawRow(null, null, null, 5, null, null));
        await AssertRawRejectedAsync(context, groupId,
            "ck_authoring_rate_rows_merchandise_interval", new RawRow(null, null, null, null, null, 1000m));

        // A minimum merchandise amount above the canonical cap with an unbounded upper limit: the old
        // amount_within_max ignored the minimum. Either merchandise guard may fire first; both are owned.
        await AssertRawRejectedAsync(context, groupId,
            ["ck_authoring_rate_rows_merchandise_interval", "ck_authoring_rate_rows_amount_within_max"],
            new RawRow(null, null, null, null, 2_000_000_000m, null));
        await AssertRawRejectedAsync(context, groupId,
            ["ck_authoring_rate_rows_merchandise_interval", "ck_authoring_rate_rows_amount_within_max"],
            new RawRow(null, null, null, null, 0m, 2_000_000_000m));

        // Re-inverted and equal bounds stay rejected, and the count minimum stays positive.
        await AssertRawRejectedAsync(context, groupId,
            "ck_authoring_rate_rows_weight_interval", new RawRow(5000, 5000, null, null, null, null));
        await AssertRawRejectedAsync(context, groupId,
            "ck_authoring_rate_rows_count_interval", new RawRow(null, null, 0, 5, null, null));

        // Malformed currency codes that the old length+upper() guard accepted.
        foreach (var malformed in new[] { "123", "A$!", "irr", "IR ", " I", "IR" })
        {
            await AssertCurrencyRejectedAsync(context, groupId, malformed);
        }

        // Valid: both bounds NULL, min-only, an adjacent bounded pair, the at-limit minimum amount with no
        // upper bound, an unconditioned count and a canonical currency code all persist.
        var accepted = new[]
        {
            new RawRow(null, null, null, null, null, null),
            new RawRow(0L, null, null, null, null, null),
            new RawRow(0L, 1000L, null, null, null, null),
            new RawRow(1000L, 5000L, 1, 4, 5000m, 10000m),
            new RawRow(null, null, 1, null, 1_000_000_000m, null),
        };
        foreach (var row in accepted)
        {
            await AssertRawAcceptedAsync(context, groupId, row);
        }

        await AssertCurrencyAcceptedAsync(context, groupId, "IRR");
        Assert.Equal("IRR", await StoredCurrencyAsync(context, groupId));

        // The at-limit minimum amount is stored verbatim; nothing was silently rounded into another rule.
        Assert.Contains(
            await StoredRowsAsync(context, groupId),
            row => row.MinMerchandiseAmount == 1_000_000_000m && row.MaxMerchandiseAmount is null);
    }

    private sealed record RawRow(
        long? MinWeightGrams,
        long? MaxWeightGrams,
        int? MinItemCount,
        int? MaxItemCount,
        decimal? MinMerchandiseAmount,
        decimal? MaxMerchandiseAmount);

    private static async Task AssertRawRejectedAsync(
        ShippingDbContext context,
        Guid groupId,
        string expectedConstraint,
        RawRow row)
        => await AssertRawRejectedAsync(context, groupId, [expectedConstraint], row);

    private static async Task AssertRawRejectedAsync(
        ShippingDbContext context,
        Guid groupId,
        IReadOnlyList<string> expectedConstraints,
        RawRow row)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        // An aborted statement poisons its transaction, so every attempt owns and rolls back its own.
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = BuildInsert(connection, transaction, groupId, row);
        var failure = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", failure.SqlState);
        Assert.Contains(failure.ConstraintName, expectedConstraints);
        await transaction.RollbackAsync();
    }

    private static async Task AssertRawAcceptedAsync(ShippingDbContext context, Guid groupId, RawRow row)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = BuildInsert(connection, null, groupId, row);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private static NpgsqlCommand BuildInsert(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid groupId,
        RawRow row)
    {
        var command = new NpgsqlCommand(
            """
            INSERT INTO shipping.authoring_rate_rows
                (rate_row_id, group_id, min_weight_grams, max_weight_grams, min_item_count, max_item_count,
                 min_merchandise_amount, max_merchandise_amount, freight_amount, packaging_amount,
                 min_delivery_days, max_delivery_days)
            VALUES (@id, @group, @minWeight, @maxWeight, @minCount, @maxCount, @minAmount, @maxAmount,
                    1000.00, 0.00, 1, 2)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("group", groupId);
        command.Parameters.AddWithValue("minWeight", (object?)row.MinWeightGrams ?? DBNull.Value);
        command.Parameters.AddWithValue("maxWeight", (object?)row.MaxWeightGrams ?? DBNull.Value);
        command.Parameters.AddWithValue("minCount", (object?)row.MinItemCount ?? DBNull.Value);
        command.Parameters.AddWithValue("maxCount", (object?)row.MaxItemCount ?? DBNull.Value);
        command.Parameters.AddWithValue("minAmount", (object?)row.MinMerchandiseAmount ?? DBNull.Value);
        command.Parameters.AddWithValue("maxAmount", (object?)row.MaxMerchandiseAmount ?? DBNull.Value);
        return command;
    }

    private static async Task AssertCurrencyRejectedAsync(ShippingDbContext context, Guid groupId, string code)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand(
            "UPDATE shipping.authoring_region_groups SET currency_code = @code WHERE group_id = @group",
            connection,
            transaction);
        command.Parameters.AddWithValue("code", code);
        command.Parameters.AddWithValue("group", groupId);
        var failure = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", failure.SqlState);
        Assert.Equal("ck_authoring_region_groups_currency_canonical", failure.ConstraintName);
        await transaction.RollbackAsync();
    }

    private static async Task AssertCurrencyAcceptedAsync(ShippingDbContext context, Guid groupId, string code)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = new NpgsqlCommand(
            "UPDATE shipping.authoring_region_groups SET currency_code = @code WHERE group_id = @group",
            connection);
        command.Parameters.AddWithValue("code", code);
        command.Parameters.AddWithValue("group", groupId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
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
        // Parsed, test-owned target: exact loopback host, exact task-only port, database and role. A missing
        // or mismatched configuration throws (fail closed) instead of skipping or faking relational proof.
        var target = ShippingRelationalTestTarget.FromEnvironment();

        // The production module configuration is used verbatim: same snake_case convention, same
        // schema-owned migration history table, same migrations assembly. A plain UseNpgsql would query
        // an unmapped PascalCase schema and prove nothing about the real mapping.
        var builder = new DbContextOptionsBuilder<ShippingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder,
            target.ConnectionString,
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
            // Prove the live target before any write: the parsed string says where we meant to go, this
            // says where we actually landed.
            if (!_liveTargetVerified)
            {
                var facts = await context.Database
                    .SqlQueryRaw<string>(
                        "SELECT (current_database() || '|' || current_user) AS \"Value\"")
                    .SingleAsync();
                var parts = facts.Split('|');
                Assert.Equal(ShippingRelationalTestTarget.ExpectedDatabase, parts[0]);
                Assert.Equal(ShippingRelationalTestTarget.ExpectedUser, parts[1]);
                _liveTargetVerified = true;
            }

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

    /// <summary>
    /// Bounded two-participant barrier. Arrivals are counted; the last one releases both. A participant that
    /// never arrives is bounded by a timeout so a failure cannot hang the suite, and a bare open transaction
    /// holds no row lock, so nobody waits while holding a conflicting lock.
    /// </summary>
    private sealed class TwoReaderGate
    {
        private const int Participants = 2;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);
        private readonly TaskCompletionSource _bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;

        public int Arrivals => Volatile.Read(ref _arrivals);

        public async Task ArriveAsync()
        {
            if (Interlocked.Increment(ref _arrivals) == Participants)
            {
                _bothArrived.TrySetResult();
            }

            await _bothArrived.Task.WaitAsync(Timeout);
        }
    }

    /// <summary>
    /// Test-only synchronization point at the FIRST SavingChanges of a replacement participant, i.e. after
    /// that participant read and prepared revision 1 and before it wrote anything. It also records the
    /// concurrency token's ORIGINAL value, which is the exact predicate the later UPDATE uses.
    /// </summary>
    private sealed class TwoReaderBarrierInterceptor(TwoReaderGate gate) : SaveChangesInterceptor
    {
        private bool _arrived;

        public long OriginalRevision { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!_arrived)
            {
                _arrived = true;
                var entry = eventData.Context!.ChangeTracker
                    .Entries<ShippingAuthoringDraft>()
                    .Single(candidate => candidate.State == EntityState.Modified);
                OriginalRevision = (long)entry.OriginalValues[nameof(ShippingAuthoringDraft.Revision)]!;
                await gate.ArriveAsync();
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>
    /// Cancels the caller token at the START of the SECOND SaveChanges, i.e. after the staged write and
    /// before the commit, so caller cancellation (not an injected arbitrary exception) is proven to roll the
    /// replacement back and propagate.
    /// </summary>
    private sealed class CancelOnSecondSaveInterceptor(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        public int SaveAttempts { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (SaveAttempts >= 2 && !cancellation.IsCancellationRequested)
            {
                cancellation.Cancel();
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class FailOnSecondSaveInterceptor : SaveChangesInterceptor
    {        public int SaveAttempts { get; private set; }

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

    private sealed class FailOnNextSaveInterceptor : SaveChangesInterceptor
    {
        private bool _armed;

        public int SaveAttempts { get; private set; }

        public void Arm() => _armed = true;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (_armed)
            {
                _armed = false;
                throw new InvalidOperationException("tb191: injected failure inside the single SaveChanges");
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
