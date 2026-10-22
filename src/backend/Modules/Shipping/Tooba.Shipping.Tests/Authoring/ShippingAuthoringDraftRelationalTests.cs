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
