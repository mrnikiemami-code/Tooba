using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
/// TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R1 — executed PostgreSQL facts for the immutable
/// revision capture (staging primitive; no publication, no pointer, no activation).
/// </summary>
/// <remarks>
/// <para>
/// Target: the task-owned disposable instance from <see cref="ShippingCaptureRelationalTestTarget"/>
/// (127.0.0.1:15432 / shipping_tb214 / tb214). The older 55432 family target and the shared 5432 database are
/// never used. A missing target throws, so no fact passes without a real server.
/// </para>
/// <para>
/// Lock/transaction/concurrency facts run against real PostgreSQL; no in-memory provider is used as a
/// substitute. Barriers are bounded (≤10s) and commands are bounded (≤90s).
/// </para>
/// </remarks>
public sealed class ShippingAuthoringRevisionSnapshotStorageTests
{
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Persian = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid English = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _schemaReady;

    [Fact]
    public async Task The_capture_table_and_its_exact_unique_index_exist_after_the_real_migration_chain()
    {
        await using var context = await NewContextAsync();

        var table = await context.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'shipping' AND table_name = 'authoring_revision_snapshots'")
            .SingleAsync();
        Assert.Equal("authoring_revision_snapshots", table);

        var index = await context.Database
            .SqlQueryRaw<string>("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'shipping' AND indexname = 'ux_authoring_revision_snapshots_owner_revision'")
            .SingleAsync();
        Assert.Contains("UNIQUE", index, StringComparison.Ordinal);
        Assert.Contains("NULLS NOT DISTINCT", index, StringComparison.Ordinal);
        Assert.Contains("source_revision", index, StringComparison.Ordinal);

        var payloadType = await context.Database
            .SqlQueryRaw<string>("SELECT data_type AS \"Value\" FROM information_schema.columns WHERE table_schema = 'shipping' AND table_name = 'authoring_revision_snapshots' AND column_name = 'payload'")
            .SingleAsync();
        Assert.Equal("jsonb", payloadType);
    }

    [Fact]
    public async Task A_capture_is_the_complete_graph_and_stays_immutable_after_a_later_edit()
    {
        var (owner, draftId, firstRevision, groups) = await SeedAsync();

        Guid capturedId;
        DateTimeOffset capturedAt;
        string capturedPayload;
        long capturedRevision;

        await using (var capture = await NewContextAsync())
        {
            var result = await new ShippingAuthoringDraftDirectory(capture)
                .CaptureRevisionAsync(owner, draftId, firstRevision, CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null);
            capturedId = result.Value.SnapshotId;
            capturedAt = result.Value.CapturedAt;
            capturedPayload = result.Value.Payload;
            capturedRevision = result.Value.SourceRevision;

            // BOTH seeded groups survive: the capture is the whole graph, not one group.
            var payload = result.Value.ReadPayload();
            Assert.Equal(2, payload.Groups.Count);
            var intended = payload.Groups.Single(group => group.GroupId == groups[0]);
            Assert.Equal(2, intended.RateRows.Count);
            Assert.Equal(5_000.00m, intended.RateRows.Single(row => row.MinWeightGrams == 0).PackagingAmount);
            Assert.Equal(9_500.75m, intended.RateRows.Single(row => row.MinWeightGrams == 5000).PackagingAmount);
            Assert.Equal(180_000.50m, intended.RateRows.Single(row => row.MinWeightGrams == 5000).FreightAmount);
            Assert.Equal(5_000_000.75m, intended.RateRows.Single(row => row.MinWeightGrams == 0).MaxMerchandiseAmount);
            Assert.Null(intended.RateRows.Single(row => row.MinWeightGrams == 5000).MaxWeightGrams);
            Assert.Single(intended.Areas);
            Assert.Equal(Tehran, intended.Areas[0].NodeId);
            Assert.Equal(2, intended.Titles.Count);
            Assert.Equal(2, payload.Translations.Count);
            Assert.Null(payload.Translations.Single(t => t.LanguageId == English).Description);
            var week = Assert.IsType<ShippingAuthoringRevisionWeek>(payload.WeeklySchedule);
            Assert.Equal(7, week.Days.Count);
            Assert.Equal(TimeOnly.FromTimeSpan(new TimeSpan(0, 9, 15, 30, 250)), week.Days.Single(d => d.Weekday == 1).StartTime);
            Assert.False(week.Days.Single(d => d.Weekday == 2).IsWorking);
            Assert.NotNull(week.Days.Single(d => d.Weekday == 2).StartTime);
            var settings = Assert.IsType<ShippingAuthoringRevisionSettings>(payload.MethodSettings);
            Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, settings.FreightPaymentMode);
            Assert.Equal(ShippingAuthoringCalculationMode.Manual, settings.CalculationMode);
            Assert.NotNull(payload.ShippingTypeId);
        }

        // The FIRST answer is the provider-persisted row: a fresh independent read must match it exactly.
        await using (var fresh = await NewContextAsync())
        {
            var stored = await fresh.AuthoringRevisionSnapshots.AsNoTracking().SingleAsync(row => row.SnapshotId == capturedId);
            Assert.Equal(capturedAt, stored.CapturedAt);
            Assert.Equal(capturedPayload, stored.Payload);
            Assert.Equal(capturedRevision, stored.SourceRevision);
        }

        // Re-capturing the SAME revision is idempotent: same identity/time/content, still one row.
        await using (var again = await NewContextAsync())
        {
            var result = await new ShippingAuthoringDraftDirectory(again)
                .CaptureRevisionAsync(owner, draftId, capturedRevision, CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null);
            Assert.Equal(capturedId, result.Value.SnapshotId);
            Assert.Equal(capturedAt, result.Value.CapturedAt);
            Assert.Equal(capturedPayload, result.Value.Payload);
            Assert.Equal(1, await again.AuthoringRevisionSnapshots.CountAsync(row => row.DraftId == draftId));
        }

        // The draft advances: the historical reader still returns the OLD capture, never the new revision.
        long advancedRevision;
        await using (var edit = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(edit);
            var saved = await directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groups[0],
                capturedRevision,
                [new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                    null, 0, 900, 1, 2, null, null, 1_000.00m, 10.00m, 1, 2)],
                "IRT",
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
            advancedRevision = saved.Value.Revision;
        }

        await using (var history = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(history);
            var old = await directory.GetCapturedRevisionAsync(owner, draftId, capturedRevision, CancellationToken.None);
            Assert.True(old.IsSuccess, old.IsFailure ? old.Errors[0].Code : null);
            Assert.Equal(capturedId, old.Value.SnapshotId);
            Assert.Equal(capturedPayload, old.Value.Payload);
            Assert.Equal(2, old.Value.ReadPayload().Groups.Single(g => g.GroupId == groups[0]).RateRows.Count);

            // No latest/current fallback: the advanced revision has its own capture, and an un-captured
            // revision is a safe not-found.
            var notCaptured = await directory.GetCapturedRevisionAsync(
                owner, draftId, advancedRevision + 100, CancellationToken.None);
            Assert.True(notCaptured.IsFailure);
            Assert.Equal(ShippingErrorCodes.AuthoringRevisionSnapshotNotFound, notCaptured.Errors[0].Code);

            // A foreign owner is the same safe not-found, never another owner's row.
            var foreign = await directory.GetCapturedRevisionAsync(
                ShippingAuthoringOwnerScope.ForStore("tb214-foreign-" + Guid.NewGuid().ToString("N")),
                draftId,
                capturedRevision,
                CancellationToken.None);
            Assert.True(foreign.IsFailure);
            Assert.Equal(ShippingErrorCodes.AuthoringRevisionSnapshotNotFound, foreign.Errors[0].Code);
        }
    }

    [Fact]
    public async Task Stale_tracked_copies_never_leak_into_a_capture_and_dirty_work_is_never_flushed()
    {
        var (owner, draftId, firstRevision, groups) = await SeedAsync();

        // Context A materialises the OLD graph and keeps it TRACKED (the "stale identity map" case).
        await using var staleA = await NewContextAsync();
        var staleDraft = await staleA.AuthoringDrafts
            .Include(candidate => candidate.RegionGroups)
            .ThenInclude(group => group.RateRows)
            .SingleAsync(candidate => candidate.DraftId == draftId);
        Assert.Equal(2, staleDraft.RegionGroups.Single(group => group.GroupId == groups[0]).RateRows.Count);
        Assert.Same(staleDraft, staleA.AuthoringDrafts.Local.Single(candidate => candidate.DraftId == draftId));

        // Context B advances the SAME draft: revision + replacement children.
        long newRevision;
        await using (var writer = await NewContextAsync())
        {
            var saved = await new ShippingAuthoringDraftDirectory(writer).ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groups[0],
                firstRevision,
                [new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                    null, 0, 700, 1, 3, null, null, 77_000.00m, 1_111.00m, 3, 4)],
                "IRT",
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
            newRevision = saved.Value.Revision;
        }

        // The successful capture runs through the ACTUAL stale context A (not a fresh context): the captured
        // facts must be B's COMPLETE graph, never a hybrid of A's stale tracked root/children and the new
        // revision, and A's stale tracked instances must survive the successful capture unchanged.
        var staleChildBefore = staleDraft.RegionGroups.Single(group => group.GroupId == groups[0]);
        var staleRootRevisionBefore = staleDraft.Revision;
        var captured = await new ShippingAuthoringDraftDirectory(staleA)
            .CaptureRevisionAsync(owner, draftId, newRevision, CancellationToken.None);
        Assert.True(captured.IsSuccess, captured.IsFailure ? captured.Errors[0].Code : null);
        var payload = captured.Value.ReadPayload();
        Assert.Equal(newRevision, payload.SourceRevision);
        var capturedGroup = payload.Groups.Single(group => group.GroupId == groups[0]);
        Assert.Single(capturedGroup.RateRows);
        Assert.Equal(1_111.00m, capturedGroup.RateRows[0].PackagingAmount);
        Assert.Equal(700L, capturedGroup.RateRows[0].MaxWeightGrams);

        // The stale caller's tracked instances were neither detached nor refreshed and nothing of its own work
        // was flushed: same object identities, same old revision, same two children.
        Assert.Same(staleDraft, staleA.AuthoringDrafts.Local.Single(candidate => candidate.DraftId == draftId));
        Assert.Equal(staleRootRevisionBefore, staleDraft.Revision);
        var staleChildAfter = staleA.AuthoringDrafts.Local
            .Single(candidate => candidate.DraftId == draftId)
            .RegionGroups
            .Single(group => group.GroupId == groups[0]);
        Assert.Same(staleChildBefore, staleChildAfter);
        Assert.Equal(2, staleChildAfter.RateRows.Count);
        Assert.False(staleA.ChangeTracker.HasChanges());

        // A capture through the SAME context that still holds the OLD graph (its token is now stale) is the
        // canonical conflict, adds no row, and does not clear or detach that caller's tracked work.
        var before = await CountSnapshotsAsync(draftId);
        var staleCapture = await new ShippingAuthoringDraftDirectory(staleA)
            .CaptureRevisionAsync(owner, draftId, firstRevision, CancellationToken.None);
        Assert.True(staleCapture.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, staleCapture.Errors[0].Code);
        Assert.NotNull(staleA.AuthoringDrafts.Local.SingleOrDefault(draft => draft.DraftId == draftId));
        Assert.Same(staleDraft, staleA.AuthoringDrafts.Local.SingleOrDefault(draft => draft.DraftId == draftId));
        Assert.Equal(before, await CountSnapshotsAsync(draftId));

        // A caller with UNSAVED work: capture must refuse, flush nothing, clear nothing, and add no snapshot.
        await using (var dirty = await NewContextAsync())
        {
            var loaded = await new ShippingAuthoringDraftDirectory(dirty).GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(loaded.IsSuccess);
            var trackedRoot = await dirty.AuthoringDrafts.SingleAsync(draft => draft.DraftId == draftId);
            dirty.Entry(trackedRoot).Property(nameof(ShippingAuthoringDraft.ServiceCode)).CurrentValue = "post-dirty";
            Assert.True(dirty.ChangeTracker.HasChanges());

            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new ShippingAuthoringDraftDirectory(dirty).CaptureRevisionAsync(
                    owner, draftId, newRevision, CancellationToken.None));
            Assert.Contains("clean change tracker", thrown.Message, StringComparison.Ordinal);

            // Nothing was flushed, nothing was cleared, and the caller's own edit is still pending.
            Assert.True(dirty.ChangeTracker.HasChanges());
            Assert.Equal("post-dirty", dirty.Entry(trackedRoot).Property(nameof(ShippingAuthoringDraft.ServiceCode)).CurrentValue);
            Assert.Equal(newRevision, trackedRoot.Revision);
            Assert.False(dirty.Entry(trackedRoot).Property(nameof(ShippingAuthoringDraft.Revision)).IsModified);
        }

        Assert.Equal(before, await CountSnapshotsAsync(draftId));
    }

    [Fact]
    public async Task Two_real_contexts_capturing_the_same_revision_store_exactly_one_row()
    {
        var (owner, draftId, firstRevision, _) = await SeedAsync();
        var revision = firstRevision;

        await using var first = await NewContextAsync();
        await using var second = await NewContextAsync();

        // A truly concurrent rendezvous: BOTH captures are started on the thread pool first and each signals
        // the bounded gate INSIDE its Task; the arrival count is asserted, not the ignored barrier return.
        using var gate = new Barrier(2);
        var arrivals = 0;
        var results = await Task.WhenAll(
            Task.Run(() => CaptureAtGateAsync(first, gate, revision)),
            Task.Run(() => CaptureAtGateAsync(second, gate, revision)));

        Assert.Equal(2, Volatile.Read(ref arrivals));
        Assert.All(results, result => Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null));
        Assert.Equal(results[0].Value.SnapshotId, results[1].Value.SnapshotId);
        Assert.Equal(results[0].Value.CapturedAt, results[1].Value.CapturedAt);
        Assert.Equal(results[0].Value.Payload, results[1].Value.Payload);
        Assert.Equal(1, await CountSnapshotsAsync(draftId));

        async Task<Tooba.BuildingBlocks.Results.Result<ShippingAuthoringRevisionSnapshot>> CaptureAtGateAsync(
            ShippingDbContext context,
            Barrier rendezvous,
            long expected)
        {
            // Bounded: a rendezvous that does not get its second arrival fails instead of hanging the test.
            Assert.True(rendezvous.SignalAndWait(TimeSpan.FromSeconds(10)));
            Interlocked.Increment(ref arrivals);
            return await new ShippingAuthoringDraftDirectory(context)
                .CaptureRevisionAsync(owner, draftId, expected, CancellationToken.None);
        }
    }

    [Fact]
    public async Task A_capture_racing_a_replacement_is_either_the_complete_old_or_new_graph_never_a_hybrid()
    {
        var (owner, draftId, firstRevision, groups) = await SeedAsync();

        await using var captureContext = await NewContextAsync();
        await using var editContext = await NewContextAsync();

        using var barrier = new Barrier(2);
        var captureTask = Task.Run(async () =>
        {
            barrier.SignalAndWait(TimeSpan.FromSeconds(10));
            return await new ShippingAuthoringDraftDirectory(captureContext)
                .CaptureRevisionAsync(owner, draftId, firstRevision, CancellationToken.None);
        });
        var editTask = Task.Run(async () =>
        {
            barrier.SignalAndWait(TimeSpan.FromSeconds(10));
            return await new ShippingAuthoringDraftDirectory(editContext).ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groups[0],
                firstRevision,
                [new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                    null, 0, 400, 1, 4, null, null, 33_000.00m, 222.00m, 6, 9)],
                "IRT",
                CancellationToken.None);
        });

        var capture = await captureTask.WaitAsync(TimeSpan.FromSeconds(90));
        var edit = await editTask.WaitAsync(TimeSpan.FromSeconds(90));

        // Either order is legitimate; the capture must be a CONSISTENT revision, never a hybrid. An explicit
        // revision can only be captured while the draft still sits at it, so a capture that lost the race is
        // the canonical conflict rather than a mixture of old and new children.
        if (capture.IsSuccess)
        {
            var payload = capture.Value.ReadPayload();
            Assert.Equal(firstRevision, payload.SourceRevision);
            var capturedGroup = payload.Groups.Single(group => group.GroupId == groups[0]);
            Assert.Equal(2, capturedGroup.RateRows.Count);
            Assert.Equal(5_000.00m, capturedGroup.RateRows.Single(row => row.MinWeightGrams == 0).PackagingAmount);
        }
        else
        {
            Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, capture.Errors[0].Code);
        }

        Assert.True(edit.IsSuccess || edit.Errors[0].Code == ShippingErrorCodes.AuthoringDraftRevisionConflict);
    }

    [Fact]
    public async Task A_failure_before_the_capture_insert_propagates_and_publishes_nothing_later()
    {
        var (owner, draftId, firstRevision, _) = await SeedAsync();
        var before = await CountSnapshotsAsync(draftId);

        // A caller that merely PRELOADED (tracked, unmodified) instances: HasChanges() is false, so capture is
        // allowed to start, and those instances must survive the failure untouched.
        var interceptor = new ThrowSentinelOnSaveInterceptor();
        await using var context = await NewContextAsync(interceptor);
        var preloadedDraft = await context.AuthoringDrafts
            .Include(candidate => candidate.RegionGroups)
            .SingleAsync(candidate => candidate.DraftId == draftId);
        var preloadedGroups = preloadedDraft.RegionGroups.ToArray();
        Assert.False(context.ChangeTracker.HasChanges());

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ShippingAuthoringDraftDirectory(context).CaptureRevisionAsync(
                owner, draftId, firstRevision, CancellationToken.None));

        // The SAME sentinel instance propagates (no wrapping, no swallowing).
        Assert.Same(interceptor.Sentinel, thrown);

        // THIS method's attempted snapshot entry is gone: nothing can be published by a later SaveChanges.
        Assert.Empty(context.AuthoringRevisionSnapshots.Local);
        Assert.Equal(0, await context.SaveChangesAsync());

        // The caller's preloaded instances are still tracked, same identities, and nothing was written.
        Assert.Same(preloadedDraft, context.AuthoringDrafts.Local.Single(candidate => candidate.DraftId == draftId));
        Assert.Equal(
            preloadedGroups,
            context.AuthoringDrafts.Local.Single(candidate => candidate.DraftId == draftId).RegionGroups.ToArray());
        Assert.Equal(before, await CountSnapshotsAsync(draftId));
    }

    [Fact]
    public async Task A_real_caller_cancellation_at_the_capture_write_boundary_writes_no_row()
    {
        var (owner, draftId, firstRevision, _) = await SeedAsync();
        var before = await CountSnapshotsAsync(draftId);

        using var cancellation = new CancellationTokenSource();
        await using var context = await NewContextAsync(new CancelOnSaveInterceptor(cancellation));

        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ShippingAuthoringDraftDirectory(context).CaptureRevisionAsync(
                owner, draftId, firstRevision, cancellation.Token));

        // The caller's own token identity propagates; nothing was retried with another token.
        Assert.Equal(cancellation.Token, cancelled.CancellationToken);
        Assert.Empty(context.AuthoringRevisionSnapshots.Local);
        Assert.Equal(0, await context.SaveChangesAsync());
        Assert.Equal(before, await CountSnapshotsAsync(draftId));
    }

    [Fact]
    public async Task A_real_unique_violation_returns_the_independent_winner_and_leaves_no_current_transaction()
    {
        var (owner, draftId, firstRevision, _) = await SeedAsync();
        var before = await CountSnapshotsAsync(draftId);

        // An INDEPENDENT task-owned winner for the same owner/draft/revision is committed by another context
        // AFTER the idempotent pre-check but BEFORE this capture's INSERT, so the capture truly hits SQLSTATE
        // 23505 on ux_authoring_revision_snapshots_owner_revision and the REAL recovery catch executes.
        var interceptor = new InsertIndependentWinnerOnSaveInterceptor(owner, draftId);
        await using var context = await NewContextAsync(interceptor);

        var result = await new ShippingAuthoringDraftDirectory(context).CaptureRevisionAsync(
            owner, draftId, firstRevision, CancellationToken.None);

        Assert.True(interceptor.Injected, "the winner must have been injected before the capture insert");
        Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null);
        Assert.Equal(interceptor.WinnerSnapshotId, result.Value.SnapshotId);

        // The recovery path disposed ITS OWN transaction before reading the winner.
        Assert.Null(context.Database.CurrentTransaction);
        Assert.Empty(context.AuthoringRevisionSnapshots.Local);
        Assert.Equal(0, await context.SaveChangesAsync());
        Assert.Equal(before + 1, await CountSnapshotsAsync(draftId));
    }

    // ------------------------------------------------------------------ fixtures

    private static async Task<(
        ShippingAuthoringOwnerScope Owner,
        Guid DraftId,
        long Revision,
        IReadOnlyList<Guid> Groups)> SeedAsync()
    {
        var owner = ShippingAuthoringOwnerScope.ForStore("tb214-store-" + Guid.NewGuid().ToString("N"));
        await using var context = await NewContextAsync();
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
        var groups = created.Value.RegionGroups.OrderBy(group => group.Order).Select(group => group.GroupId).ToArray();

        var rates = await directory.ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            groups[0],
            created.Value.Revision,
            [
                new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                    null, 0, 5000, 1, 10, 100_000.25m, 5_000_000.75m, 120_000.00m, 5_000.00m, 1, 3),
                new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                    null, 5000, null, 11, null, null, null, 180_000.50m, 9_500.75m, 2, 5),
            ],
            "IRT",
            CancellationToken.None);
        Assert.True(rates.IsSuccess, rates.IsFailure ? rates.Errors[0].Code : null);

        await using (var presentation = await NewContextAsync())
        {
            var saved = await new ShippingAuthoringDraftDirectory(presentation).ReplacePresentationAsync(
                owner,
                draftId,
                rates.Value.Revision,
                [
                    new ShippingAuthoringDraft.MethodTranslationInput(Persian, "پست پیشتاز", "توضیح فارسی"),
                    new ShippingAuthoringDraft.MethodTranslationInput(English, "Express post", null),
                ],
                [
                    new ShippingAuthoringDraft.GroupTitleInput(groups[0], Persian, "منطقهٔ تهران"),
                    new ShippingAuthoringDraft.GroupTitleInput(groups[0], English, "Tehran area"),
                ],
                new HashSet<Guid> { Persian, English },
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        }

        await using (var schedule = await NewContextAsync())
        {
            var current = (await new ShippingAuthoringDraftDirectory(schedule).GetAsync(owner, draftId, CancellationToken.None)).Value;
            var saved = await new ShippingAuthoringDraftDirectory(schedule).ReplaceWeeklyScheduleAsync(
                owner,
                draftId,
                current.Revision,
                "Asia/Tehran",
                [
                    new ShippingAuthoringDraft.ScheduleDayInput(1, true, TimeOnly.FromTimeSpan(new TimeSpan(0, 9, 15, 30, 250)), new TimeOnly(12, 45)),
                    new ShippingAuthoringDraft.ScheduleDayInput(2, false, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(3, true, new TimeOnly(8, 0), new TimeOnly(16, 30)),
                    new ShippingAuthoringDraft.ScheduleDayInput(4, false, null, null),
                    new ShippingAuthoringDraft.ScheduleDayInput(5, true, new TimeOnly(10, 0), new TimeOnly(18, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(6, true, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(7, false, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                ],
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        }

        await using (var settings = await NewContextAsync())
        {
            var current = (await new ShippingAuthoringDraftDirectory(settings).GetAsync(owner, draftId, CancellationToken.None)).Value;
            var saved = await new ShippingAuthoringDraftDirectory(settings).ReplaceMethodSettingsAsync(
                owner,
                draftId,
                current.Revision,
                ShippingFreightPayerMode.CollectOnDelivery,
                ShippingAuthoringCalculationMode.Manual,
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        }

        await using (var type = await NewContextAsync())
        {
            var current = (await new ShippingAuthoringDraftDirectory(type).GetAsync(owner, draftId, CancellationToken.None)).Value;
            var saved = await new ShippingAuthoringDraftDirectory(type).ReplaceTypeAssignmentAsync(
                owner,
                draftId,
                current.Revision,
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
            return (owner, draftId, saved.Value.Revision, groups);
        }
    }

    private static async Task<int> CountSnapshotsAsync(Guid draftId)
    {
        await using var context = await NewContextAsync();
        return await context.AuthoringRevisionSnapshots.CountAsync(row => row.DraftId == draftId);
    }

    private static async Task<ShippingDbContext> NewContextAsync(params IInterceptor[] interceptors)
    {
        // Parsed, test-owned target asserted against live server facts BEFORE any DDL.
        var target = ShippingCaptureRelationalTestTarget.FromEnvironment();
        await EnsureSchemaAsync(target);

        var builder = new DbContextOptionsBuilder<ShippingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder,
            target.ConnectionString,
            ShippingDbContext.Schema,
            typeof(ShippingDbContext));
        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new ShippingDbContext(builder.Options);
    }

    /// <summary>
    /// یک‌بار (در هر اجرا) واقعیت هویت سرور و زنجیرهٔ واقعی migration را روی همان هدفِ
    /// در اختیار تسک ثابت می‌کند؛ context این بررسی هیچ interceptor آزمایشی ندارد.
    /// </summary>
    /// <param name="target">هدف رابطه‌ایِ در اختیار تسک.</param>
    private static async Task EnsureSchemaAsync(ShippingCaptureRelationalTestTarget target)
    {
        await MigrationGate.WaitAsync();
        try
        {
            if (_schemaReady)
            {
                return;
            }

            var options = new DbContextOptionsBuilder<ShippingDbContext>();
            ToobaNpgsql.ConfigureModuleContext(
                options,
                target.ConnectionString,
                ShippingDbContext.Schema,
                typeof(ShippingDbContext));
            await using var context = new ShippingDbContext(options.Options);

            // TCP (not a socket) plus the exact identity this task owns: never the shared database.
            var facts = await context.Database
                .SqlQueryRaw<string>(
                    "SELECT (current_database() || '|' || current_user || '|' || coalesce(host(inet_server_addr()), 'socket')) AS \"Value\"")
                .SingleAsync();
            var parts = facts.Split('|');
            Assert.Equal(ShippingCaptureRelationalTestTarget.ExpectedDatabase, parts[0]);
            Assert.Equal(ShippingCaptureRelationalTestTarget.ExpectedUser, parts[1]);
            Assert.NotEqual("socket", parts[2]);

            await context.GetService<IMigrator>().MigrateAsync();
            _schemaReady = true;
        }
        finally
        {
            MigrationGate.Release();
        }
    }

    private static ShippingDestinationNode Node(Guid id)
        => new(ShippingDestinationNodeKind.City, id);

    /// <summary>
    /// یک استثنای شاهدِ یکتا پیش از نوشتنِ واقعی پرتاب می‌کند تا ثابت شود همان نمونه propagate می‌شود؛
    /// فراخوانی‌های بعدی عبور می‌کنند تا «SaveChanges بعدی چیزی منتشر نمی‌کند» واقعاً سنجیده شود.
    /// </summary>
    private sealed class ThrowSentinelOnSaveInterceptor : SaveChangesInterceptor
    {
        public InvalidOperationException Sentinel { get; } = new("tb214 sentinel: capture write refused");

        private bool _thrown;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_thrown)
            {
                return ValueTask.FromResult(result);
            }

            _thrown = true;
            throw Sentinel;
        }
    }

    /// <summary>
    /// در مرزِ نوشتنِ capture، توکنِ واقعیِ فراخوان را یک‌بار لغو می‌کند و همان
    /// OperationCanceledException را با هویتِ همان توکن پرتاب می‌کند.
    /// </summary>
    private sealed class CancelOnSaveInterceptor : SaveChangesInterceptor
    {
        private readonly CancellationTokenSource _cancellation;

        private bool _cancelled;

        public CancelOnSaveInterceptor(CancellationTokenSource cancellation) => _cancellation = cancellation;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_cancelled)
            {
                return ValueTask.FromResult(result);
            }

            _cancelled = true;
            _cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// پس از بررسی idempotent و پیش از INSERTِ capture، یک برندهٔ مستقل برای همان
    /// (owner, draft, revision) در تراکنشِ جدا می‌نویسد؛ سپس INSERTِ خودِ capture با نقضِ واقعیِ
    /// قید یکتا (23505) مواجه می‌شود و مسیرِ بازیابیِ واقعی اجرا می‌گردد.
    /// </summary>
    private sealed class InsertIndependentWinnerOnSaveInterceptor : SaveChangesInterceptor
    {
        private readonly ShippingAuthoringOwnerScope _owner;
        private readonly Guid _draftId;

        public InsertIndependentWinnerOnSaveInterceptor(ShippingAuthoringOwnerScope owner, Guid draftId)
        {
            _owner = owner;
            _draftId = draftId;
        }

        public bool Injected { get; private set; }

        public Guid WinnerSnapshotId { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Injected)
            {
                return result;
            }

            // A separate context/connection = a separate committed transaction: the capture's own transaction
            // only holds the draft root lock, so this winner insert is not blocked by it.
            await using var winner = await NewContextAsync();
            var draft = await winner.AuthoringDrafts
                .Include(candidate => candidate.RegionGroups)
                .ThenInclude(group => group.Areas)
                .Include(candidate => candidate.RegionGroups)
                .ThenInclude(group => group.RateRows)
                .Include(candidate => candidate.RegionGroups)
                .ThenInclude(group => group.Titles)
                .Include(candidate => candidate.Translations)
                .Include(candidate => candidate.MethodSettings)
                .Include(candidate => candidate.WeeklySchedule)
                .ThenInclude(schedule => schedule!.Days)
                .SingleAsync(candidate => candidate.DraftId == _draftId, cancellationToken);

            var independent = ShippingAuthoringRevisionSnapshot.Capture(draft, DateTimeOffset.UtcNow);
            winner.AuthoringRevisionSnapshots.Add(independent);
            await winner.SaveChangesAsync(cancellationToken);

            WinnerSnapshotId = independent.SnapshotId;
            Injected = true;
            return result;
        }
    }
}
