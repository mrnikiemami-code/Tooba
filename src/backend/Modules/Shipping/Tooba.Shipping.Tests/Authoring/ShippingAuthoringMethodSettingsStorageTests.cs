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
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194 — executed PostgreSQL facts for the NEW method-root settings
/// storage (freight payment timing + calculation mode as two independent selections).
/// </summary>
/// <remarks>
/// <para>
/// The target is the parsed, task-owned disposable instance (<see cref="ShippingRelationalTestTarget"/>):
/// 127.0.0.1:55432 / shipping_tb190r1 / tb190r1. A missing or mismatched target throws (fail closed).
/// </para>
/// <para>
/// Every rejected raw statement runs in its OWN transaction and is rolled back, because an aborted
/// statement poisons the rest of its transaction. Assertions name the exact SQLSTATE and the exact owned
/// constraint/column, so "something threw" is never accepted as proof.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringMethodSettingsStorageTests
{
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private const int Saturday = 6;
    private const int Monday = 1;

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveTargetVerified;

    [Fact]
    public async Task Settings_roundtrip_through_a_new_context_and_preserve_every_other_capability()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb194-store-" + Guid.NewGuid().ToString("N"));
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

        // Absent settings are explicitly UNCONFIGURED, never an implicit Prepaid/Manual row.
        await using (var unconfigured = await NewContextAsync())
        {
            var read = await new ShippingAuthoringDraftDirectory(unconfigured)
                .GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(read.IsSuccess, read.IsFailure ? read.Errors[0].Code : null);
            Assert.Null(read.Value.MethodSettings);
            Assert.Equal(1, read.Value.Revision);
        }

        await using (var writer = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(writer);
            var rate = await directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groups[0].GroupId,
                1,
                [RowInput(12.50m)],
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
            var schedule = await directory.ReplaceWeeklyScheduleAsync(
                owner,
                draftId,
                3,
                "Asia/Tehran",
                Days((Saturday, true, new TimeOnly(9, 0), new TimeOnly(13, 0))),
                CancellationToken.None);
            Assert.True(schedule.IsSuccess, schedule.IsFailure ? schedule.Errors[0].Code : null);

            // The first settings write creates exactly one row and advances the SAME root revision once.
            var settings = await directory.ReplaceMethodSettingsAsync(
                owner,
                draftId,
                4,
                ShippingFreightPayerMode.CollectOnDelivery,
                ShippingAuthoringCalculationMode.Free,
                CancellationToken.None);
            Assert.True(settings.IsSuccess, settings.IsFailure ? settings.Errors[0].Code : null);
            Assert.Equal(5, settings.Value.Revision);

            // An identical PUT is a truthful no-op: revision and both selections are preserved.
            var noOp = await directory.ReplaceMethodSettingsAsync(
                owner,
                draftId,
                5,
                ShippingFreightPayerMode.CollectOnDelivery,
                ShippingAuthoringCalculationMode.Free,
                CancellationToken.None);
            Assert.True(noOp.IsSuccess, noOp.IsFailure ? noOp.Errors[0].Code : null);
            Assert.Equal(5, noOp.Value.Revision);
        }

        // NEW context readback of both independent values.
        await using (var readback = await NewContextAsync())
        {
            var stored = await new ShippingAuthoringDraftDirectory(readback)
                .GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(stored.IsSuccess, stored.IsFailure ? stored.Errors[0].Code : null);
            var settings = Assert.IsType<ShippingAuthoringMethodSettings>(stored.Value.MethodSettings);
            Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, settings.FreightPaymentMode);
            Assert.Equal(ShippingAuthoringCalculationMode.Free, settings.CalculationMode);
        }

        // Switching ONLY the freight axis preserves rates/currency/text/geography/schedule.
        await using (var switcher = await NewContextAsync())
        {
            var switched = await new ShippingAuthoringDraftDirectory(switcher).ReplaceMethodSettingsAsync(
                owner,
                draftId,
                5,
                ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Free,
                CancellationToken.None);
            Assert.True(switched.IsSuccess, switched.IsFailure ? switched.Errors[0].Code : null);
            Assert.Equal(6, switched.Value.Revision);
            Assert.Equal(ShippingFreightPayerMode.Prepaid, switched.Value.MethodSettings!.FreightPaymentMode);
            Assert.Equal(ShippingAuthoringCalculationMode.Free, switched.Value.MethodSettings.CalculationMode);
        }

        // A subsequent EXISTING-capability write (region reorder) preserves the settings row in return.
        await using (var editor = await NewContextAsync())
        {
            var reordered = await new ShippingAuthoringDraftDirectory(editor).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                6,
                [
                    new ShippingAuthoringRegionGroupInput(groups[1].GroupId, [Node(Isfahan)]),
                    new ShippingAuthoringRegionGroupInput(groups[0].GroupId, [Node(Tehran)]),
                ],
                CancellationToken.None);
            Assert.True(reordered.IsSuccess, reordered.IsFailure ? reordered.Errors[0].Code : null);
        }

        await using (var final = await NewContextAsync())
        {
            var stored = await new ShippingAuthoringDraftDirectory(final)
                .GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(stored.IsSuccess, stored.IsFailure ? stored.Errors[0].Code : null);
            var draft = stored.Value;
            Assert.Equal(7, draft.Revision);
            Assert.Equal(ShippingFreightPayerMode.Prepaid, draft.MethodSettings!.FreightPaymentMode);
            Assert.Equal(ShippingAuthoringCalculationMode.Free, draft.MethodSettings.CalculationMode);
            var group = draft.RegionGroups.Single(item => item.GroupId == groups[0].GroupId);
            Assert.Equal("USD", group.CurrencyCode);
            Assert.Equal(12.50m, group.RateRows.Single().FreightAmount);
            Assert.Equal("ارسال سریع", draft.Translations.Single().Name);
            Assert.Equal("Asia/Tehran", draft.WeeklySchedule!.TimeZoneId);
            Assert.Equal(7, draft.WeeklySchedule.Days.Count);
        }
    }

    [Fact]
    public async Task Raw_storage_rejects_null_unknown_duplicate_and_foreign_rows()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb194-raw-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(context).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;

        // A committed row proves the owned table accepts a real pair of independent selections.
        await using (var seed = await NewContextAsync())
        {
            await using var transaction = await seed.Database.BeginTransactionAsync();
            await ExecuteAsync(
                seed,
                transaction,
                "INSERT INTO shipping.authoring_method_settings "
                + "(draft_id, freight_payment_mode, calculation_mode, updated_at) VALUES (@draft, 1, 2, now())",
                ("draft", draftId));
            await transaction.CommitAsync();
        }

        // SQL NULL cannot bypass the required freight choice.
        var nullFailure = await CaptureRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_method_settings "
            + "(draft_id, freight_payment_mode, calculation_mode, updated_at) VALUES (@other, NULL, 0, now())",
            ("other", Guid.NewGuid()));
        Assert.Equal("23502", nullFailure.SqlState);
        Assert.Equal("freight_payment_mode", nullFailure.ColumnName);

        // NULL is equally impossible for the calculation choice.
        var nullCalculation = await CaptureRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_method_settings "
            + "(draft_id, freight_payment_mode, calculation_mode, updated_at) VALUES (@other, 0, NULL, now())",
            ("other", Guid.NewGuid()));
        Assert.Equal("23502", nullCalculation.SqlState);
        Assert.Equal("calculation_mode", nullCalculation.ColumnName);

        // An unknown freight value is refused by the owned CHECK instead of being coerced to Prepaid.
        await AssertRejectedAsync(
            context,
            "23514",
            "ck_authoring_method_settings_freight_mode",
            "INSERT INTO shipping.authoring_method_settings "
            + "(draft_id, freight_payment_mode, calculation_mode, updated_at) VALUES (@other, 7, 0, now())",
            ("other", Guid.NewGuid()));

        // An unknown calculation value is refused by the owned CHECK instead of being coerced to Manual.
        await AssertRejectedAsync(
            context,
            "23514",
            "ck_authoring_method_settings_calculation_mode",
            "INSERT INTO shipping.authoring_method_settings "
            + "(draft_id, freight_payment_mode, calculation_mode, updated_at) VALUES (@other, 0, 9, now())",
            ("other", Guid.NewGuid()));

        // One settings row per draft/method: the draft key is the real relational invariant.
        await AssertRejectedAsync(
            context,
            "23505",
            "pk_authoring_method_settings",
            "INSERT INTO shipping.authoring_method_settings "
            + "(draft_id, freight_payment_mode, calculation_mode, updated_at) VALUES (@draft, 0, 0, now())",
            ("draft", draftId));

        // The row can never point at a draft that does not exist.
        await AssertRejectedAsync(
            context,
            "23503",
            "fk_authoring_method_settings_draft",
            "INSERT INTO shipping.authoring_method_settings "
            + "(draft_id, freight_payment_mode, calculation_mode, updated_at) VALUES (@other, 0, 0, now())",
            ("other", Guid.NewGuid()));
    }

    [Fact]
    public async Task Concurrent_settings_and_rate_writes_leave_exactly_the_winner_state_and_one_canonical_conflict()
    {
        await using var seed = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb194-race-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var groupId = created.Value.RegionGroups.Single().GroupId;

        // Deterministic two-reader barrier (the established TB-190-R2/TB-191-R1/TB-192-R1/TB-193 pattern):
        // each participant is intercepted at its FIRST SavingChanges, i.e. AFTER it read the draft root with
        // revision 1 and staged its own rows, and BEFORE either has written anything. A bare BEGIN holds no
        // row lock here, so neither participant waits on the other while holding a lock.
        var gate = new TwoReaderGate();
        var settingsInterceptor = new TwoReaderBarrierInterceptor(gate);
        var rateInterceptor = new TwoReaderBarrierInterceptor(gate);

        await using var settingsContext = await NewContextAsync(settingsInterceptor);
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
            RunAsync(0, token => new ShippingAuthoringDraftDirectory(settingsContext).ReplaceMethodSettingsAsync(
                owner,
                draftId,
                1,
                ShippingFreightPayerMode.CollectOnDelivery,
                ShippingAuthoringCalculationMode.Free,
                token)),
            RunAsync(1, token => new ShippingAuthoringDraftDirectory(rateContext).ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groupId,
                1,
                [RowInput(12.50m)],
                "USD",
                token)));

        // Both really arrived with the ORIGINAL root token 1 loaded, and the barrier really synchronized them.
        Assert.Equal(2, gate.Arrivals);
        Assert.Equal(1, settingsInterceptor.OriginalRevision);
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

        // The persisted state is exactly the WINNER's capability and the absence of the loser's, asserted
        // explicitly in both directions instead of validating whichever capability happens to exist.
        if (winner.Index == 0)
        {
            var settings = Assert.IsType<ShippingAuthoringMethodSettings>(draftValue.MethodSettings);
            Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, settings.FreightPaymentMode);
            Assert.Equal(ShippingAuthoringCalculationMode.Free, settings.CalculationMode);

            // The losing rate write left nothing behind: no row and no currency.
            Assert.Empty(storedGroup.RateRows);
            Assert.Null(storedGroup.CurrencyCode);
        }
        else
        {
            // The losing settings write left nothing behind, so the draft stays UNCONFIGURED.
            Assert.Null(draftValue.MethodSettings);

            var storedRow = Assert.Single(storedGroup.RateRows);
            Assert.Equal(12.50m, storedRow.FreightAmount);
            Assert.Equal("USD", storedGroup.CurrencyCode);
        }

        // No hybrid: the settings row exists only when the settings write actually won.
        var settingsRowCount = await verification.Set<ShippingAuthoringMethodSettings>()
            .AsNoTracking()
            .CountAsync(settings => settings.DraftId == draftId);
        Assert.Equal(winner.Index == 0 ? 1 : 0, settingsRowCount);
        var rateRowCount = await verification.Set<ShippingAuthoringRateRow>()
            .AsNoTracking()
            .CountAsync(row => row.GroupId == groupId);
        Assert.Equal(winner.Index == 0 ? 0 : 1, rateRowCount);

        // The ACTUAL losing context holds no pending edit that a later SaveChanges could publish.
        var loserContext = loser.Index == 0 ? settingsContext : rateContext;
        Assert.Empty(loserContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));

        // And the loser re-reads the winner's state, never its own rejected write.
        var reread = await new ShippingAuthoringDraftDirectory(loserContext)
            .GetAsync(owner, draftId, CancellationToken.None);
        Assert.True(reread.IsSuccess, reread.IsFailure ? reread.Errors[0].Code : null);
        Assert.Equal(2, reread.Value.Revision);
        Assert.Equal(draftValue.MethodSettings is not null, reread.Value.MethodSettings is not null);
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
        var exception = await CaptureRejectedAsync(context, sql, parameters);
        Assert.Equal(sqlState, exception.SqlState);
        Assert.Equal(constraint, exception.ConstraintName);
    }

    private static async Task<PostgresException> CaptureRejectedAsync(
        ShippingDbContext context,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        var connection = await OpenAsync(context);
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = BuildCommand(connection, transaction, sql, parameters);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        await transaction.RollbackAsync();
        return exception;
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

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput RowInput(decimal freightAmount)
        => new(
            null,
            0,
            1000,
            null,
            null,
            null,
            null,
            freightAmount,
            0m,
            1,
            3);
}
