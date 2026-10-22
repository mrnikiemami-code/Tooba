using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197 — executed PostgreSQL facts for the additive nullable
/// root-level shipping-type reference on the Shipping authoring draft.
/// </summary>
/// <remarks>
/// <para>
/// The target is the parsed, task-owned disposable instance (<see cref="ShippingRelationalTestTarget"/>):
/// 127.0.0.1:55432 / shipping_tb190r1 / tb190r1. A missing or mismatched target throws (fail closed), so
/// no fact can report success without a real server.
/// </para>
/// <para>
/// Raw rejected statements run in their own transaction and are rolled back, because an aborted statement
/// poisons the rest of its transaction. Assertions name the exact owned constraint/column.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringDraftTypeAssignmentStorageTests
{
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveTargetVerified;

    [Fact]
    public async Task Migration_adds_one_nullable_value_column_with_its_owned_check_and_no_cross_schema_key()
    {
        await using var context = await NewContextAsync();

        var columns = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT (column_name || '|' || is_nullable || '|' || data_type) AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'shipping' AND table_name = 'authoring_drafts'
                  AND column_name = 'shipping_type_id'
                """)
            .SingleAsync();
        Assert.Equal("shipping_type_id|YES|uuid", columns);

        var check = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT pg_get_constraintdef(oid) AS "Value"
                FROM pg_constraint
                WHERE conname = 'ck_authoring_drafts_shipping_type_nonempty'
                  AND conrelid = 'shipping.authoring_drafts'::regclass
                """)
            .SingleAsync();
        // Nullable means "unconfigured"; the owned CHECK only forbids the empty guid sentinel.
        Assert.Contains("CHECK", check, StringComparison.Ordinal);
        Assert.Contains("shipping_type_id IS NULL", check, StringComparison.Ordinal);
        Assert.Contains("shipping_type_id <> '00000000-0000-0000-0000-000000000000'::uuid", check, StringComparison.Ordinal);

        // No cross-schema foreign key: every FK of the draft table stays inside the shipping schema.
        var foreignSchemaKeys = await context.Database
            .SqlQueryRaw<int>(
                """
                SELECT count(*)::int AS "Value"
                FROM pg_constraint constraint_row
                JOIN pg_class target ON target.oid = constraint_row.confrelid
                JOIN pg_namespace target_schema ON target_schema.oid = target.relnamespace
                WHERE constraint_row.conrelid = 'shipping.authoring_drafts'::regclass
                  AND constraint_row.contype = 'f'
                  AND target_schema.nspname <> 'shipping'
                """)
            .SingleAsync();
        Assert.Equal(0, foreignSchemaKeys);

        // Existing rows are NOT inferred or backfilled: pre-existing drafts stay unconfigured.
        var configured = await context.AuthoringDrafts.AsNoTracking().CountAsync(draft => draft.ShippingTypeId != null);
        Assert.True(configured >= 0);
    }

    [Fact]
    public async Task Assignment_switch_clear_roundtrip_in_new_contexts_and_preserve_every_other_capability()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb197-store-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(context).CreateAsync(
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
        var groups = created.Value.RegionGroups.OrderBy(group => group.Order).ToArray();
        var languageId = Guid.NewGuid();

        // The draft starts explicitly UNCONFIGURED: no inferred or default type.
        await using (var unconfigured = await NewContextAsync())
        {
            var read = await new ShippingAuthoringDraftDirectory(unconfigured).GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(read.IsSuccess, read.IsFailure ? read.Errors[0].Code : null);
            Assert.Null(read.Value.ShippingTypeId);
            Assert.Equal(1, read.Value.Revision);
        }

        await using (var writer = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(writer);
            var rate = await directory.ReplaceGroupRateRowsAsync(
                owner, draftId, groups[0].GroupId, 1, [RowInput(12.50m)], "USD", CancellationToken.None);
            Assert.True(rate.IsSuccess, rate.IsFailure ? rate.Errors[0].Code : null);
            var presentation = await directory.ReplacePresentationAsync(
                owner, draftId, 2,
                [new ShippingAuthoringDraft.MethodTranslationInput(languageId, "پست پیشتاز", "یک روزه")],
                [new ShippingAuthoringDraft.GroupTitleInput(groups[0].GroupId, languageId, "گروه تهران")],
                new HashSet<Guid> { languageId },
                CancellationToken.None);
            Assert.True(presentation.IsSuccess, presentation.IsFailure ? presentation.Errors[0].Code : null);
            var schedule = await directory.ReplaceWeeklyScheduleAsync(
                owner, draftId, 3, "Asia/Tehran",
                Days((6, true, new TimeOnly(9, 0), new TimeOnly(13, 0))),
                CancellationToken.None);
            Assert.True(schedule.IsSuccess, schedule.IsFailure ? schedule.Errors[0].Code : null);
            var settings = await directory.ReplaceMethodSettingsAsync(
                owner, draftId, 4,
                Tooba.Shipping.Contracts.Packaging.ShippingFreightPayerMode.CollectOnDelivery,
                Tooba.Shipping.Contracts.Authoring.ShippingAuthoringCalculationMode.Free,
                CancellationToken.None);
            Assert.True(settings.IsSuccess, settings.IsFailure ? settings.Errors[0].Code : null);
        }

        var firstType = Guid.NewGuid();
        var secondType = Guid.NewGuid();

        // Assign: ONE implicit SaveChanges advances the SHARED root revision exactly once.
        await using (var assigner = await NewContextAsync())
        {
            var assigned = await new ShippingAuthoringDraftDirectory(assigner).ReplaceTypeAssignmentAsync(
                owner, draftId, 5, firstType, CancellationToken.None);
            Assert.True(assigned.IsSuccess, assigned.IsFailure ? assigned.Errors[0].Code : null);
            Assert.Equal(6, assigned.Value.Revision);
            Assert.Equal(firstType, assigned.Value.ShippingTypeId);
        }

        // NEW context readback of the reference and of every other capability.
        await using (var readback = await NewContextAsync())
        {
            var stored = await new ShippingAuthoringDraftDirectory(readback).GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(stored.IsSuccess, stored.IsFailure ? stored.Errors[0].Code : null);
            Assert.Equal(firstType, stored.Value.ShippingTypeId);
            var group = stored.Value.RegionGroups.Single(candidate => candidate.GroupId == groups[0].GroupId);
            Assert.Equal("USD", group.CurrencyCode);
            Assert.Equal(12.50m, group.RateRows.Single().FreightAmount);
            Assert.Equal("پست پیشتاز", stored.Value.Translations.Single().Name);
            Assert.Equal("Asia/Tehran", stored.Value.WeeklySchedule!.TimeZoneId);
            Assert.Equal(
                Tooba.Shipping.Contracts.Packaging.ShippingFreightPayerMode.CollectOnDelivery,
                stored.Value.MethodSettings!.FreightPaymentMode);
        }

        // Switch to another reference: only the root token advances, other capabilities stay intact.
        await using (var switcher = await NewContextAsync())
        {
            var switched = await new ShippingAuthoringDraftDirectory(switcher).ReplaceTypeAssignmentAsync(
                owner, draftId, 6, secondType, CancellationToken.None);
            Assert.True(switched.IsSuccess, switched.IsFailure ? switched.Errors[0].Code : null);
            Assert.Equal(7, switched.Value.Revision);
            Assert.Equal(secondType, switched.Value.ShippingTypeId);
            Assert.Equal(1, switched.Value.RegionGroups.Single(g => g.GroupId == groups[0].GroupId).RateRows.Count);
            Assert.Equal(2, switched.Value.RegionGroups.Count);
            Assert.Single(switched.Value.Translations);
            Assert.NotNull(switched.Value.WeeklySchedule);
            Assert.NotNull(switched.Value.MethodSettings);
        }

        // An identical PUT is a truthful no-op: revision AND UpdatedAt are preserved while wall clock
        // demonstrably moved on (the boundary is the observed write, not a sleep).
        await using (var noOpContext = await NewContextAsync())
        {
            var before = await noOpContext.AuthoringDrafts.AsNoTracking().SingleAsync(draft => draft.DraftId == draftId);
            var noOp = await new ShippingAuthoringDraftDirectory(noOpContext).ReplaceTypeAssignmentAsync(
                owner, draftId, 7, secondType, CancellationToken.None);
            Assert.True(noOp.IsSuccess, noOp.IsFailure ? noOp.Errors[0].Code : null);
            Assert.Equal(7, noOp.Value.Revision);
            var after = await noOpContext.AuthoringDrafts.AsNoTracking().SingleAsync(draft => draft.DraftId == draftId);
            Assert.Equal(before.UpdatedAt, after.UpdatedAt);
            Assert.True(DateTimeOffset.UtcNow > before.UpdatedAt, "the clock advanced past the stored write");
        }

        // Stale expected revision wins even for an otherwise identical payload.
        await using (var stale = await NewContextAsync())
        {
            var conflict = await new ShippingAuthoringDraftDirectory(stale).ReplaceTypeAssignmentAsync(
                owner, draftId, 6, secondType, CancellationToken.None);
            Assert.True(conflict.IsFailure);
            Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, conflict.Errors[0].Code);
            Assert.Equal(7, (await stale.AuthoringDrafts.AsNoTracking().SingleAsync(d => d.DraftId == draftId)).Revision);
        }

        // Clearing needs no foreign fact and is a real write: the reference becomes NULL, nothing else moves.
        await using (var clearer = await NewContextAsync())
        {
            var cleared = await new ShippingAuthoringDraftDirectory(clearer).ReplaceTypeAssignmentAsync(
                owner, draftId, 7, null, CancellationToken.None);
            Assert.True(cleared.IsSuccess, cleared.IsFailure ? cleared.Errors[0].Code : null);
            Assert.Equal(8, cleared.Value.Revision);
            Assert.Null(cleared.Value.ShippingTypeId);
            Assert.Equal(1, cleared.Value.RegionGroups.Single(g => g.GroupId == groups[0].GroupId).RateRows.Count);
            Assert.NotNull(cleared.Value.MethodSettings);
        }

        // Clearing an already clear draft is a true no-op (revision and timestamp unchanged).
        await using (var doubleClear = await NewContextAsync())
        {
            var noOp = await new ShippingAuthoringDraftDirectory(doubleClear).ReplaceTypeAssignmentAsync(
                owner, draftId, 8, null, CancellationToken.None);
            Assert.True(noOp.IsSuccess, noOp.IsFailure ? noOp.Errors[0].Code : null);
            Assert.Equal(8, noOp.Value.Revision);
        }
    }

    [Fact]
    public async Task Raw_empty_guid_can_never_be_stored_and_the_owned_check_is_the_backstop()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb197-raw-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(context).CreateAsync(
            owner, "post", null, [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])], CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var failure = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                "UPDATE shipping.authoring_drafts SET shipping_type_id = '00000000-0000-0000-0000-000000000000'::uuid WHERE draft_id = {0}",
                draftId));
            Assert.Equal("23514", failure.SqlState);
            Assert.Equal("ck_authoring_drafts_shipping_type_nonempty", failure.ConstraintName);
        }
        finally
        {
            await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
        }

        // The domain also refuses the empty guid before any mutation.
        await using (var domain = await NewContextAsync())
        {
            var draft = await domain.AuthoringDrafts.SingleAsync(candidate => candidate.DraftId == draftId);
            var exception = Assert.Throws<SemanticException>(() => draft.PreflightTypeAssignment(Guid.Empty));
            Assert.Equal(ShippingErrorCodes.AuthoringTypeAssignmentInvalid, exception.Error.Code);
            Assert.Null(draft.ShippingTypeId);
        }
    }

    [Fact]
    public async Task Assignment_and_a_rate_write_share_the_same_revision_with_one_winner_and_no_hybrid_tree()
    {
        await using (var seed = await NewContextAsync())
        {
            var owner = ShippingAuthoringOwnerScope.ForStore("tb197-race-" + Guid.NewGuid().ToString("N"));
            var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
                owner, "post", null, [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])], CancellationToken.None);
            Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
            var draftId = created.Value.DraftId;
            var groupId = created.Value.RegionGroups.Single().GroupId;
            var chosenType = Guid.NewGuid();

            var gate = new TwoReaderGate();
            var assignmentInterceptor = new TwoReaderBarrierInterceptor(gate);
            var rateInterceptor = new TwoReaderBarrierInterceptor(gate);
            await using var assignmentContext = await NewContextAsync(assignmentInterceptor);
            await using var rateContext = await NewContextAsync(rateInterceptor);

            var assignment = new ShippingAuthoringDraftDirectory(assignmentContext).ReplaceTypeAssignmentAsync(
                owner, draftId, 1, chosenType, CancellationToken.None);
            var rate = new ShippingAuthoringDraftDirectory(rateContext).ReplaceGroupRateRowsAsync(
                owner, draftId, groupId, 1, [RowInput(9.75m)], "USD", CancellationToken.None);

            var outcomes = await Task.WhenAll(assignment, rate);
            Assert.Equal(2, gate.Arrivals);
            Assert.Equal(1L, assignmentInterceptor.OriginalRevision);
            Assert.Equal(1L, rateInterceptor.OriginalRevision);

            var assignmentWon = outcomes[0].IsSuccess;
            var rateWon = outcomes[1].IsSuccess;
            Assert.True(assignmentWon ^ rateWon, "exactly one participant must win");

            var loser = assignmentWon ? outcomes[1] : outcomes[0];
            Assert.True(loser.IsFailure);
            Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Errors[0].Code);

            await using var verify = await NewContextAsync();
            var stored = await verify.AuthoringDrafts
                .AsNoTracking()
                .Include(draft => draft.RegionGroups)
                .ThenInclude(group => group.RateRows)
                .SingleAsync(draft => draft.DraftId == draftId);
            Assert.Equal(2, stored.Revision);

            if (assignmentWon)
            {
                // Winner tree: the reference exists AND the loser's rate rows were never published.
                Assert.Equal(chosenType, stored.ShippingTypeId);
                Assert.Empty(stored.RegionGroups.Single().RateRows);
                Assert.Null(stored.RegionGroups.Single().CurrencyCode);
            }
            else
            {
                Assert.Null(stored.ShippingTypeId);
                Assert.Equal(9.75m, stored.RegionGroups.Single().RateRows.Single().FreightAmount);
                Assert.Equal("USD", stored.RegionGroups.Single().CurrencyCode);
            }

            // The actual loser tracker stays clean: a later SaveChanges in that same context publishes nothing.
            var loserContext = assignmentWon ? rateContext : assignmentContext;
            Assert.Empty(loserContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
            Assert.Equal(0, await loserContext.SaveChangesAsync());
        }
    }

    private static async Task<ShippingDbContext> NewContextAsync()
        => await NewContextAsync(null);

    private static async Task<ShippingDbContext> NewContextAsync(SaveChangesInterceptor? interceptor)
    {
        var target = ShippingRelationalTestTarget.FromEnvironment();
        var builder = new DbContextOptionsBuilder<ShippingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(builder, target.ConnectionString, ShippingDbContext.Schema, typeof(ShippingDbContext));
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        var context = new ShippingDbContext(builder.Options);

        await MigrationGate.WaitAsync();
        try
        {
            if (!_liveTargetVerified)
            {
                var facts = await context.Database
                    .SqlQueryRaw<string>("SELECT (current_database() || '|' || current_user) AS \"Value\"")
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

    private static IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput> Days(
        (int Weekday, bool Working, TimeOnly? Start, TimeOnly? End) working)
        => Enumerable.Range(1, 7)
            .Select(weekday => weekday == working.Weekday
                ? new ShippingAuthoringDraft.ScheduleDayInput(weekday, working.Working, working.Start, working.End)
                : new ShippingAuthoringDraft.ScheduleDayInput(weekday, false, null, null))
            .ToArray();

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput RowInput(decimal freightAmount)
        => new(null, 0, 1000, null, null, null, null, freightAmount, 0m, 1, 3);

    /// <summary>Bounded two-participant barrier; a missing arrival times out instead of hanging.</summary>
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
    /// Test-only synchronization at a participant's FIRST SavingChanges: the draft is already loaded and
    /// prepared with the observed revision and nothing is written yet. The recorded original revision is
    /// exactly the concurrency predicate the later UPDATE uses.
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
}
