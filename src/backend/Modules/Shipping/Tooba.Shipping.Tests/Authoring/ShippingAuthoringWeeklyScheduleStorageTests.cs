using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
/// TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193 — executed PostgreSQL facts for the NEW owner-scoped
/// weekly schedule storage.
/// </summary>
/// <remarks>
/// <para>
/// The target is the parsed, task-owned disposable instance (<see cref="ShippingRelationalTestTarget"/>):
/// 127.0.0.1:55432 / shipping_tb190r1 / tb190r1. A missing or mismatched target throws (fail closed).
/// </para>
/// <para>
/// Every rejected raw statement runs in its OWN transaction and is rolled back, because an aborted
/// statement poisons the rest of its transaction. Assertions name the exact SQLSTATE and the exact owned
/// constraint, so "something threw" is never accepted as proof.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringWeeklyScheduleStorageTests
{
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private const int Monday = 1;
    private const int Friday = 5;
    private const int Saturday = 6;

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveTargetVerified;

    [Fact]
    public async Task Full_week_roundtrips_through_a_new_context_and_survives_unrelated_capability_writes()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb193-week-" + Guid.NewGuid().ToString("N"));
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

        var directory = new ShippingAuthoringDraftDirectory(context);
        var rate = await directory.ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            groups[0].GroupId,
            1,
            [RowInput(null, 0, 1000, 12.50m)],
            "USD",
            CancellationToken.None);
        Assert.True(rate.IsSuccess, rate.IsFailure ? rate.Errors[0].Code : null);
        var presentation = await directory.ReplacePresentationAsync(
            owner,
            draftId,
            2,
            [new ShippingAuthoringDraft.MethodTranslationInput(languageId, "ارسال سریع", "یک روزه")],
            [new ShippingAuthoringDraft.GroupTitleInput(groups[0].GroupId, languageId, "گروه تهران")],
            new HashSet<Guid> { languageId },
            CancellationToken.None);
        Assert.True(presentation.IsSuccess, presentation.IsFailure ? presentation.Errors[0].Code : null);

        var configured = await directory.ReplaceWeeklyScheduleAsync(
            owner,
            draftId,
            3,
            "Asia/Tehran",
            Days(
                (Saturday, true, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                (Friday, false, new TimeOnly(10, 0), new TimeOnly(12, 0))),
            CancellationToken.None);
        Assert.True(configured.IsSuccess, configured.IsFailure ? configured.Errors[0].Code : null);
        Assert.Equal(4, configured.Value.Revision);

        // NEW context readback: the seven day keys are distinct and the closed day keeps its pair.
        await using (var readback = await NewContextAsync())
        {
            var stored = await new ShippingAuthoringDraftDirectory(readback)
                .GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(stored.IsSuccess, stored.IsFailure ? stored.Errors[0].Code : null);
            var schedule = stored.Value.WeeklySchedule;
            Assert.NotNull(schedule);
            Assert.Equal("Asia/Tehran", schedule!.TimeZoneId);
            Assert.Equal(7, schedule.Days.Select(day => day.Weekday).Distinct().Count());
            var friday = schedule.Days.Single(day => day.Weekday == Friday);
            Assert.False(friday.IsWorking);
            Assert.Equal(new TimeOnly(10, 0), friday.StartTime);
            Assert.Equal(new TimeOnly(12, 0), friday.EndTime);
            var saturday = schedule.Days.Single(day => day.Weekday == Saturday);
            Assert.True(saturday.IsWorking);
            Assert.Equal(new TimeOnly(9, 0), saturday.StartTime);
        }

        // An unrelated group reorder + rate edit preserves the ROOT schedule byte-for-byte.
        await using (var editor = await NewContextAsync())
        {
            var editorDirectory = new ShippingAuthoringDraftDirectory(editor);
            var reordered = await editorDirectory.ReplaceRegionGroupsAsync(
                owner,
                draftId,
                4,
                [
                    new ShippingAuthoringRegionGroupInput(groups[1].GroupId, [Node(Isfahan)]),
                    new ShippingAuthoringRegionGroupInput(groups[0].GroupId, [Node(Tehran)]),
                ],
                CancellationToken.None);
            Assert.True(reordered.IsSuccess, reordered.IsFailure ? reordered.Errors[0].Code : null);
        }

        await using (var edited = await NewContextAsync())
        {
            var schedule = await edited.AuthoringDrafts
                .AsNoTracking()
                .Include(draft => draft.WeeklySchedule)
                .ThenInclude(week => week!.Days)
                .SingleAsync(draft => draft.DraftId == draftId);
            Assert.Equal("Asia/Tehran", schedule.WeeklySchedule!.TimeZoneId);
            Assert.Equal(7, schedule.WeeklySchedule.Days.Count);
        }

        // A schedule-only update preserves translations, rate amounts/currency and the geography snapshot.
        await using (var scheduleWriter = await NewContextAsync())
        {
            var updated = await new ShippingAuthoringDraftDirectory(scheduleWriter).ReplaceWeeklyScheduleAsync(
                owner,
                draftId,
                5,
                "Asia/Tehran",
                Days(
                    (Saturday, true, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                    (Friday, false, new TimeOnly(10, 0), new TimeOnly(12, 0)),
                    (Monday, true, new TimeOnly(8, 0), new TimeOnly(16, 0))),
                CancellationToken.None);
            Assert.True(updated.IsSuccess, updated.IsFailure ? updated.Errors[0].Code : null);
        }

        await using (var final = await NewContextAsync())
        {
            var stored = await new ShippingAuthoringDraftDirectory(final)
                .GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(stored.IsSuccess, stored.IsFailure ? stored.Errors[0].Code : null);
            var draft = stored.Value;
            Assert.Equal("ارسال سریع", draft.Translations.Single().Name);
            var group = draft.RegionGroups.Single(item => item.GroupId == groups[0].GroupId);
            Assert.Equal("USD", group.CurrencyCode);
            Assert.Equal(12.50m, group.RateRows.Single().FreightAmount);
            Assert.Equal("گروه تهران", group.Titles.Single().Title);
            Assert.Equal(new TimeOnly(8, 0), draft.WeeklySchedule!.Days.Single(day => day.Weekday == Monday).StartTime);
        }
    }

    [Fact]
    public async Task Raw_storage_rejects_weekday_pairs_ranges_flags_duplicates_and_foreign_or_blank_timezone()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb193-raw-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(context).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;

        // A committed header proves the owned table accepts a real configured week.
        await using (var seed = await NewContextAsync())
        {
            await using var transaction = await seed.Database.BeginTransactionAsync();
            await ExecuteAsync(
                seed,
                transaction,
                "INSERT INTO shipping.authoring_weekly_schedules (draft_id, time_zone_id, updated_at) "
                + "VALUES (@draft, 'Asia/Tehran', now())",
                ("draft", draftId));
            await ExecuteAsync(
                seed,
                transaction,
                "INSERT INTO shipping.authoring_weekly_schedule_days (draft_id, weekday, is_working, start_time, end_time) "
                + "VALUES (@draft, 6, TRUE, '09:00', '13:00')",
                ("draft", draftId));
            await transaction.CommitAsync();
        }

        // Duplicate (draft, weekday) is the composite primary key.
        await AssertRejectedAsync(
            context,
            "23505",
            "pk_authoring_weekly_schedule_days",
            "INSERT INTO shipping.authoring_weekly_schedule_days (draft_id, weekday, is_working, start_time, end_time) "
            + "VALUES (@draft, 6, TRUE, '09:00', '13:00')",
            ("draft", draftId));

        // A weekday outside the ISO 1..7 identity space is refused by the owned CHECK.
        await AssertRejectedAsync(
            context,
            "23514",
            "ck_authoring_weekly_schedule_days_weekday_range",
            "INSERT INTO shipping.authoring_weekly_schedule_days (draft_id, weekday, is_working, start_time, end_time) "
            + "VALUES (@draft, 8, FALSE, NULL, NULL)",
            ("draft", draftId));

        // A half-stored pair is not a remembered pair.
        await AssertRejectedAsync(
            context,
            "23514",
            "ck_authoring_weekly_schedule_days_hours_paired",
            "INSERT INTO shipping.authoring_weekly_schedule_days (draft_id, weekday, is_working, start_time, end_time) "
            + "VALUES (@draft, 2, FALSE, '09:00', NULL)",
            ("draft", draftId));

        // Equal/inverted/overnight intervals are rejected rather than reversed or extended.
        await AssertRejectedAsync(
            context,
            "23514",
            "ck_authoring_weekly_schedule_days_range",
            "INSERT INTO shipping.authoring_weekly_schedule_days (draft_id, weekday, is_working, start_time, end_time) "
            + "VALUES (@draft, 3, TRUE, '17:00', '09:00')",
            ("draft", draftId));

        // A working day without an explicit interval cannot exist in storage.
        await AssertRejectedAsync(
            context,
            "23514",
            "ck_authoring_weekly_schedule_days_working_hours",
            "INSERT INTO shipping.authoring_weekly_schedule_days (draft_id, weekday, is_working, start_time, end_time) "
            + "VALUES (@draft, 4, TRUE, NULL, NULL)",
            ("draft", draftId));

        // The day table has its own FK to the schedule header, not to a foreign module or a bare draft.
        await AssertRejectedAsync(
            context,
            "23503",
            "fk_authoring_weekly_schedule_days_schedule",
            "INSERT INTO shipping.authoring_weekly_schedule_days (draft_id, weekday, is_working, start_time, end_time) "
            + "VALUES (@other, 1, FALSE, NULL, NULL)",
            ("other", Guid.NewGuid()));

        // A blank timezone is refused; resolvability stays a canonical preflight rule, not a fake catalog.
        await AssertRejectedAsync(
            context,
            "23514",
            "ck_authoring_weekly_schedules_timezone_length",
            "INSERT INTO shipping.authoring_weekly_schedules (draft_id, time_zone_id, updated_at) "
            + "VALUES (@other, '   ', now())",
            ("other", Guid.NewGuid()));

        // One schedule per draft/method: the draft key is the real relational invariant.
        await AssertRejectedAsync(
            context,
            "23505",
            "pk_authoring_weekly_schedules",
            "INSERT INTO shipping.authoring_weekly_schedules (draft_id, time_zone_id, updated_at) "
            + "VALUES (@draft, 'Asia/Tehran', now())",
            ("draft", draftId));
    }

    [Fact]
    public async Task Concurrent_schedule_and_rate_writes_leave_exactly_the_winner_state_and_one_canonical_conflict()
    {
        await using var seed = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb193-race-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var groupId = created.Value.RegionGroups.Single().GroupId;

        // Deterministic two-reader barrier (the established TB-190-R2/TB-191-R1/TB-192-R1 pattern): each
        // participant is intercepted at its FIRST SavingChanges, i.e. AFTER it read the draft root with
        // revision 1 and staged its own rows, and BEFORE either has written anything. A bare BEGIN holds no
        // row lock here, so neither participant waits on the other while holding a lock.
        var gate = new TwoReaderGate();
        var scheduleInterceptor = new TwoReaderBarrierInterceptor(gate);
        var rateInterceptor = new TwoReaderBarrierInterceptor(gate);

        await using var scheduleContext = await NewContextAsync(scheduleInterceptor);
        await using var rateContext = await NewContextAsync(rateInterceptor);

        async Task<(int Index, Tooba.BuildingBlocks.Results.Result<ShippingAuthoringDraft> Result)> RunAsync(
            int index,
            Func<CancellationToken, Task<Tooba.BuildingBlocks.Results.Result<ShippingAuthoringDraft>>> run)
        {
            try
            {
                return (index, await run(CancellationToken.None));
            }
            catch (Exception exception)
            {
                throw new Xunit.Sdk.XunitException(
                    $"participant {index} threw {exception.GetType().Name}: {exception.Message}");
            }
        }

        var outcomes = await Task.WhenAll(
            RunAsync(0, token => new ShippingAuthoringDraftDirectory(scheduleContext).ReplaceWeeklyScheduleAsync(
                owner,
                draftId,
                1,
                "Asia/Tehran",
                Days((Saturday, true, new TimeOnly(9, 0), new TimeOnly(13, 0))),
                token)),
            RunAsync(1, token => new ShippingAuthoringDraftDirectory(rateContext).ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groupId,
                1,
                [RowInput(null, 0, 1000, 12.50m)],
                "USD",
                token)));

        // Both really arrived with the ORIGINAL root token 1 loaded, and the barrier really synchronized them.
        Assert.Equal(2, gate.Arrivals);
        Assert.Equal(1, scheduleInterceptor.OriginalRevision);
        Assert.Equal(1, rateInterceptor.OriginalRevision);

        // Exactly one winner and exactly one loser: the shared root revision admits a single commit.
        var winner = Assert.Single(outcomes, outcome => !outcome.Result.IsFailure);
        var loser = Assert.Single(outcomes, outcome => outcome.Result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Result.Errors[0].Code);

        await using var verification = await NewContextAsync();
        var stored = await new ShippingAuthoringDraftDirectory(verification)
            .GetAsync(owner, draftId, CancellationToken.None);
        Assert.True(stored.IsSuccess, stored.IsFailure ? stored.Errors[0].Code : null);
        var draftValue = stored.Value;

        // The revision advanced EXACTLY once, so both writers cannot have committed.
        Assert.Equal(2, draftValue.Revision);
        var storedGroup = draftValue.RegionGroups.Single();
        Assert.Equal(groupId, storedGroup.GroupId);

        // The persisted state is exactly the WINNER's capability and the absence of the loser's, asserted
        // explicitly in both directions instead of validating whichever capability happens to exist.
        if (winner.Index == 0)
        {
            var schedule = Assert.IsType<ShippingAuthoringWeeklySchedule>(draftValue.WeeklySchedule);
            Assert.Equal("Asia/Tehran", schedule.TimeZoneId);
            Assert.Equal(7, schedule.Days.Count);
            var saturday = schedule.Days.Single(day => day.Weekday == Saturday);
            Assert.True(saturday.IsWorking);
            Assert.Equal(new TimeOnly(9, 0), saturday.StartTime);
            Assert.Equal(new TimeOnly(13, 0), saturday.EndTime);
            Assert.All(
                schedule.Days.Where(day => day.Weekday != Saturday),
                day => Assert.False(day.IsWorking));

            // The losing rate write left nothing behind: no row and no currency.
            Assert.Empty(storedGroup.RateRows);
            Assert.Null(storedGroup.CurrencyCode);
        }
        else
        {
            // The losing schedule write left nothing behind: no header, so the draft stays unconfigured.
            Assert.Null(draftValue.WeeklySchedule);

            var storedRow = Assert.Single(storedGroup.RateRows);
            Assert.Equal(12.50m, storedRow.FreightAmount);
            Assert.Equal("USD", storedGroup.CurrencyCode);
        }

        // No hybrid: the day rows exist only when the schedule write actually won.
        var dayRowCount = await verification.Set<ShippingAuthoringScheduleDay>()
            .AsNoTracking()
            .CountAsync(day => day.DraftId == draftId);
        Assert.Equal(winner.Index == 0 ? 7 : 0, dayRowCount);
        var rateRowCount = await verification.Set<ShippingAuthoringRateRow>()
            .AsNoTracking()
            .CountAsync(row => row.GroupId == groupId);
        Assert.Equal(winner.Index == 0 ? 0 : 1, rateRowCount);

        // The ACTUAL losing context holds no pending edit that a later SaveChanges could publish.
        var loserContext = loser.Index == 0 ? scheduleContext : rateContext;
        Assert.Empty(loserContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));

        // And the loser re-reads the winner's state, never its own rejected write.
        var reread = await new ShippingAuthoringDraftDirectory(loserContext)
            .GetAsync(owner, draftId, CancellationToken.None);
        Assert.True(reread.IsSuccess, reread.IsFailure ? reread.Errors[0].Code : null);
        Assert.Equal(2, reread.Value.Revision);
        Assert.Equal(
            draftValue.WeeklySchedule is not null,
            reread.Value.WeeklySchedule is not null);
        Assert.Equal(
            storedGroup.RateRows.Count,
            reread.Value.RegionGroups.Single().RateRows.Count);
    }

    private static IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput> Days(
        params (int Weekday, bool IsWorking, TimeOnly? Start, TimeOnly? End)[] overrides)
    {
        var byWeekday = overrides.ToDictionary(item => item.Weekday);
        return Enumerable.Range(1, 7)
            .Select(weekday => byWeekday.TryGetValue(weekday, out var day)
                ? new ShippingAuthoringDraft.ScheduleDayInput(weekday, day.IsWorking, day.Start, day.End)
                : new ShippingAuthoringDraft.ScheduleDayInput(weekday, false, null, null))
            .ToArray();
    }

    private static async Task AssertRejectedAsync(
        ShippingDbContext context,
        string sqlState,
        string constraint,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        var connection = await OpenAsync(context);
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = BuildCommand(connection, transaction, sql, parameters);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(sqlState, exception.SqlState);
        Assert.Equal(constraint, exception.ConstraintName);
        await transaction.RollbackAsync();
    }

    private static async Task ExecuteAsync(
        ShippingDbContext context,
        IDbContextTransaction transaction,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await using var command = BuildCommand(
            connection,
            (NpgsqlTransaction)transaction.GetDbTransaction(),
            sql,
            parameters);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private static async Task<NpgsqlConnection> OpenAsync(ShippingDbContext context)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    private static NpgsqlCommand BuildCommand(
        NpgsqlConnection connection,
        DbTransaction? transaction,
        string sql,
        (string Name, object? Value)[] parameters)
    {
        var command = new NpgsqlCommand(sql, connection, (NpgsqlTransaction?)transaction);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    private static async Task<ShippingDbContext> NewContextAsync()
        => await NewContextAsync(null);

    private static async Task<ShippingDbContext> NewContextAsync(SaveChangesInterceptor? interceptor)
    {
        // Parsed, test-owned target: exact loopback host/port/database/role. A missing or mismatched
        // configuration throws instead of skipping or faking the relational proof.
        var target = ShippingRelationalTestTarget.FromEnvironment();
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
    /// Test-only synchronization point at the FIRST SavingChanges of a participant, i.e. after that
    /// participant read and prepared the draft root with revision 1 and before it wrote anything. It also
    /// records the concurrency token's ORIGINAL value, which is the exact predicate the later UPDATE uses.
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

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput RowInput(
        Guid? rateRowId,
        long? minWeightGrams,
        long? maxWeightGrams,
        decimal freightAmount,
        decimal packagingAmount = 0m,
        int minDeliveryDays = 1,
        int maxDeliveryDays = 3)
        => new(
            rateRowId,
            minWeightGrams,
            maxWeightGrams,
            null,
            null,
            null,
            null,
            freightAmount,
            packagingAmount,
            minDeliveryDays,
            maxDeliveryDays);
}
