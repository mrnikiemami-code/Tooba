using System.Globalization;
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
/// TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199 / R1 — executed PostgreSQL facts for the existing-draft
/// editor document: ONE commit/ONE revision advance for a composite PUT, an honest no-op that writes
/// nothing, a rejected late child that changes nothing, the cross-capability race, and rollback/caller
/// cancellation. Every whole-tree claim is made against ONE structural snapshot of every authored root
/// scalar and every own child row.
/// </summary>
/// <remarks>
/// The target is the parsed, task-owned disposable instance (<see cref="ShippingRelationalTestTarget"/>):
/// 127.0.0.1:55432 / shipping_tb190r1 / tb190r1. A missing or mismatched target throws (fail closed), so
/// no fact can report success without a real server.
/// </remarks>
public sealed class ShippingAuthoringEditorDocumentStorageTests
{
    private static readonly Guid Tehran = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid Isfahan = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid Shiraz = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Persian = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid English = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TypeDefinition = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly HashSet<Guid> ActiveLanguages = [Persian, English];

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveTargetVerified;

    [Fact]
    public async Task A_composite_put_advances_once_and_an_identical_document_preserves_the_whole_tree_and_stamp()
    {
        var seeded = await SeedCompleteAsync("tb199-noop");
        var revision = seeded.Revision;

        // A deliberately OLD persisted stamp makes the timestamp boundary non-vacuous: a no-op that
        // rewrote UpdatedAt from "now" could not silently pass.
        await using (var ageing = await NewContextAsync())
        {
            var oldStamp = DateTimeOffset.UtcNow.AddDays(-3);
            var affected = await ageing.Database.ExecuteSqlRawAsync(
                "UPDATE shipping.authoring_drafts SET updated_at = {0} WHERE draft_id = {1}",
                oldStamp,
                seeded.DraftId);
            Assert.Equal(1, affected);
        }

        var baseline = await SnapshotAsync(seeded.DraftId);
        Assert.Contains(seeded.DraftId.ToString(), baseline, StringComparison.Ordinal);

        // ONE composite PUT: a brand-new group with a nested rate row AND a title, a reorder of the
        // retained groups, a presentation patch, method settings and an explicit type selection.
        var document = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Shiraz)],
                    [Row(0, 1500, null, null, null, null, 2, 4)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "جنوب")]),
                new ShippingAuthoringDraft.EditorGroupInput(seeded.Groups[1], [Node(Isfahan)], [], []),
                new ShippingAuthoringDraft.EditorGroupInput(seeded.Groups[0], [Node(Tehran)], [], []),
            ],
            [new ShippingAuthoringDraft.MethodTranslationInput(Persian, "پست ویژه", "توضیح تازه")],
            false,
            null,
            null,
            true,
            ShippingFreightPayerMode.CollectOnDelivery,
            ShippingAuthoringCalculationMode.CarrierApi,
            true,
            TypeDefinition);

        ShippingAuthoringDraft saved;
        await using (var writeContext = await NewContextAsync())
        {
            var result = await new ShippingAuthoringDraftDirectory(writeContext).ReplaceEditorDocumentAsync(
                seeded.Owner,
                seeded.DraftId,
                revision,
                document,
                ActiveLanguages,
                "IRR",
                CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null);
            saved = result.Value;
            Assert.Equal(revision + 1, saved.Revision);
        }

        // The complete winning tree, not selected fields: revision advanced exactly once and the whole
        // returned aggregate matches a fresh AsNoTracking read of every own row.
        var afterSave = await SnapshotAsync(seeded.DraftId);
        Assert.Equal(await SnapshotAsync(seeded.DraftId), afterSave);
        Assert.Equal(Snapshot(saved), afterSave);

        // The brand-new group really carries its nested row and title, and the retained groups keep their
        // identities in the requested order.
        var ordered = saved.RegionGroups.OrderBy(group => group.Order).ToArray();
        Assert.Equal(3, ordered.Length);
        Assert.Equal(Shiraz, Assert.Single(ordered[0].Areas).NodeId);
        Assert.Equal(1500, Assert.Single(ordered[0].RateRows).MaxWeightGrams);
        Assert.Equal("جنوب", Assert.Single(ordered[0].Titles).Title);
        Assert.Equal(seeded.Groups[1], ordered[1].GroupId);
        Assert.Equal(seeded.Groups[0], ordered[2].GroupId);

        var stamp = await ReadStampAsync(seeded.DraftId);
        Assert.Equal(revision + 1, stamp.Revision);

        // The SAME non-empty document at the current revision (schedule/settings preserved because they
        // are omitted) is an honest no-op: no write at all, so revision and UpdatedAt stay bit-identical.
        var identical = DocumentFrom(saved, includeOptionalSections: false);
        await using (var noOpContext = await NewContextAsync())
        {
            var result = await new ShippingAuthoringDraftDirectory(noOpContext).ReplaceEditorDocumentAsync(
                seeded.Owner,
                seeded.DraftId,
                revision + 1,
                identical,
                ActiveLanguages,
                "IRR",
                CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null);
            Assert.Equal(revision + 1, result.Value.Revision);
        }

        Assert.Equal(afterSave, await SnapshotAsync(seeded.DraftId));
        var noOpStamp = await ReadStampAsync(seeded.DraftId);
        Assert.Equal(stamp.UpdatedAt, noOpStamp.UpdatedAt);
        Assert.Equal(stamp.Revision, noOpStamp.Revision);

        // A stale token still wins over an otherwise identical document.
        await using (var staleContext = await NewContextAsync())
        {
            var stale = await new ShippingAuthoringDraftDirectory(staleContext).ReplaceEditorDocumentAsync(
                seeded.Owner,
                seeded.DraftId,
                revision,
                identical,
                ActiveLanguages,
                "IRR",
                CancellationToken.None);
            Assert.True(stale.IsFailure);
            Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, stale.Errors[0].Code);
        }

        Assert.Equal(afterSave, await SnapshotAsync(seeded.DraftId));
    }

    [Fact]
    public async Task A_late_invalid_child_leaves_the_same_aggregate_and_context_untouched_and_retries_in_place()
    {
        var seeded = await SeedCompleteAsync("tb199-late");
        var baseline = await SnapshotAsync(seeded.DraftId);
        var revision = seeded.Revision;

        await using var context = await NewContextAsync();
        var loaded = await new ShippingAuthoringDraftDirectory(context).GetAsync(
            seeded.Owner, seeded.DraftId, CancellationToken.None);
        Assert.True(loaded.IsSuccess);
        Assert.Equal(revision, loaded.Value.Revision);
        var valueBefore = loaded.Value;

        // Valid earlier edits then a LATE-invalid child in a brand-new group.
        var lateInvalid = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    seeded.Groups[0],
                    [Node(Tehran)],
                    [],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(English, "capital")]),
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"))],
                    [Row(9000, 9000, null, null, null, null, 5, 2)],
                    []),
            ],
            [new ShippingAuthoringDraft.MethodTranslationInput(Persian, "نام پذیرفته‌نشده", null)],
            false,
            null,
            null,
            true,
            ShippingFreightPayerMode.Prepaid,
            ShippingAuthoringCalculationMode.Free,
            true,
            null);

        var rejected = await new ShippingAuthoringDraftDirectory(context).ReplaceEditorDocumentAsync(
            seeded.Owner,
            seeded.DraftId,
            revision,
            lateInvalid,
            ActiveLanguages,
            "IRR",
            CancellationToken.None);
        Assert.True(rejected.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringRateConditionInvalid, rejected.Errors[0].Code);

        // The SAME loaded aggregate is value-identical, nothing is pending, and a later SaveChanges that
        // would publish a half-applied edit writes zero rows.
        Assert.Equal(revision, valueBefore.Revision);
        Assert.Equal(seeded.Groups.Length, valueBefore.RegionGroups.Count);
        Assert.All(context.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
        Assert.Equal(0, await context.SaveChangesAsync());
        Assert.Equal(baseline, await SnapshotAsync(seeded.DraftId));

        // A retained row id that is NOT a row of the named group is rejected, not silently re-parented.
        var foreignRowId = await FirstRowIdAsync(seeded.DraftId, seeded.Groups[1]);
        var foreignRow = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    seeded.Groups[0],
                    [Node(Tehran)],
                    [Row(0, 1000, null, null, null, null, 1, 3, foreignRowId)],
                    []),
            ],
            [],
            false,
            null,
            null,
            false,
            null,
            null,
            false,
            null);
        var foreignRejected = await new ShippingAuthoringDraftDirectory(context).ReplaceEditorDocumentAsync(
            seeded.Owner, seeded.DraftId, revision, foreignRow, ActiveLanguages, "IRR", CancellationToken.None);
        Assert.True(foreignRejected.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringRateRowPayloadInvalid, foreignRejected.Errors[0].Code);

        // An unknown group id is a claim too.
        var unknownGroup = new ShippingAuthoringDraft.EditorDocumentInput(
            [new ShippingAuthoringDraft.EditorGroupInput(Guid.NewGuid(), [Node(Tehran)], [], [])],
            [],
            false,
            null,
            null,
            false,
            null,
            null,
            false,
            null);
        var unknownRejected = await new ShippingAuthoringDraftDirectory(context).ReplaceEditorDocumentAsync(
            seeded.Owner, seeded.DraftId, revision, unknownGroup, ActiveLanguages, "IRR", CancellationToken.None);
        Assert.True(unknownRejected.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringRegionPayloadInvalid, unknownRejected.Errors[0].Code);

        Assert.All(context.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
        Assert.Equal(0, await context.SaveChangesAsync());
        Assert.Equal(baseline, await SnapshotAsync(seeded.DraftId));

        // A valid retry at the ORIGINAL revision in this SAME context succeeds once, with the complete
        // expected tree.
        var valid = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    seeded.Groups[0],
                    [Node(Tehran)],
                    [],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "پایتخت")]),
                new ShippingAuthoringDraft.EditorGroupInput(seeded.Groups[1], [Node(Isfahan)], [], []),
            ],
            [new ShippingAuthoringDraft.MethodTranslationInput(Persian, "پست پیشتاز", null)],
            false,
            null,
            null,
            true,
            ShippingFreightPayerMode.CollectOnDelivery,
            ShippingAuthoringCalculationMode.Free,
            false,
            null);
        var retried = await new ShippingAuthoringDraftDirectory(context).ReplaceEditorDocumentAsync(
            seeded.Owner, seeded.DraftId, revision, valid, ActiveLanguages, "IRR", CancellationToken.None);
        Assert.True(retried.IsSuccess, retried.IsFailure ? retried.Errors[0].Code : null);
        Assert.Equal(revision + 1, retried.Value.Revision);
        Assert.Equal(Snapshot(retried.Value), await SnapshotAsync(seeded.DraftId));
    }

    [Fact]
    public async Task Editor_and_region_writes_race_on_one_root_revision_and_only_the_winner_tree_survives()
    {
        var seeded = await SeedCompleteAsync("tb199-race");
        var baseline = await SnapshotAsync(seeded.DraftId);

        var gate = new TwoReaderGate();
        var editorInterceptor = new TwoReaderBarrierInterceptor(gate);
        var regionInterceptor = new TwoReaderBarrierInterceptor(gate);

        await using var editorContext = await NewContextAsync(editorInterceptor);
        await using var regionContext = await NewContextAsync(regionInterceptor);

        var editorDocument = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Shiraz)],
                    [Row(0, 1000, null, null, null, null, 1, 3)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "شمال")]),
                new ShippingAuthoringDraft.EditorGroupInput(seeded.Groups[0], [Node(Tehran)], [], []),
                new ShippingAuthoringDraft.EditorGroupInput(seeded.Groups[1], [Node(Isfahan)], [], []),
            ],
            [new ShippingAuthoringDraft.MethodTranslationInput(Persian, "پست پیشتاز", null)],
            false,
            null,
            null,
            false,
            null,
            null,
            false,
            null);

        var replacementNode = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

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
            RunAsync(0, token => new ShippingAuthoringDraftDirectory(editorContext).ReplaceEditorDocumentAsync(
                seeded.Owner, seeded.DraftId, seeded.Revision, editorDocument, ActiveLanguages, "IRR", token)),
            RunAsync(1, token => new ShippingAuthoringDraftDirectory(regionContext).ReplaceRegionGroupsAsync(
                seeded.Owner,
                seeded.DraftId,
                seeded.Revision,
                [
                    new ShippingAuthoringRegionGroupInput(seeded.Groups[0], [Node(Tehran)]),
                    new ShippingAuthoringRegionGroupInput(seeded.Groups[1], [Node(replacementNode)]),
                ],
                token)));

        // Both participants read the SAME root token and were synchronized BEFORE any write.
        Assert.Equal(2, gate.Arrivals);
        Assert.Equal(seeded.Revision, editorInterceptor.OriginalRevision);
        Assert.Equal(seeded.Revision, regionInterceptor.OriginalRevision);

        var winner = Assert.Single(outcomes, outcome => !outcome.Result.IsFailure);
        var loser = Assert.Single(outcomes, outcome => outcome.Result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Result.Errors[0].Code);
        Assert.Equal(seeded.Revision + 1, winner.Result.Value.Revision);

        // The complete winning proposal — schedule, type, unchanged languages, every row and selection —
        // equals a fresh snapshot of every persisted row, with no hybrid from the loser.
        var winning = await SnapshotAsync(seeded.DraftId);
        Assert.Equal(Snapshot(winner.Result.Value), winning);
        Assert.NotEqual(baseline, winning);

        // The ACTUAL loser tracker is clean BEFORE its later SaveChanges that must publish nothing.
        var loserContext = loser.Index == 0 ? editorContext : regionContext;
        var loserDirectory = new ShippingAuthoringDraftDirectory(loserContext);
        Assert.All(loserContext.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
        Assert.Equal(0, await loserContext.SaveChangesAsync());

        // Re-reading through the loser context (and a fresh one) returns the winning tree, never a partial
        // write of its own rejected proposal.
        var loserRead = await loserDirectory.GetAsync(seeded.Owner, seeded.DraftId, CancellationToken.None);
        Assert.True(loserRead.IsSuccess, loserRead.IsFailure ? loserRead.Errors[0].Code : null);
        Assert.Equal(Snapshot(loserRead.Value), winning);

        if (winner.Index == 0)
        {
            // The editor winner's new group really exists and the region loser's node never arrived.
            Assert.DoesNotContain(SnapshotOfNode(replacementNode), winning, StringComparison.Ordinal);
        }
        else
        {
            // The region winner's replacement node arrived and the editor's new group never did.
            Assert.Contains(SnapshotOfNode(replacementNode), winning, StringComparison.Ordinal);
            Assert.DoesNotContain(SnapshotOfNode(Shiraz), winning, StringComparison.Ordinal);
        }

        Assert.Equal(winning, await SnapshotAsync(seeded.DraftId));
    }

    [Fact]
    public async Task A_failed_second_staged_save_and_caller_cancellation_roll_the_whole_document_back()
    {
        var seeded = await SeedCompleteAsync("tb199-rollback");
        var baseline = await SnapshotAsync(seeded.DraftId);
        var revision = seeded.Revision;

        ShippingAuthoringDraft.EditorDocumentInput Document() => new(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Shiraz)],
                    [Row(0, 1000, null, null, null, null, 1, 3)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "شمال")]),
                new ShippingAuthoringDraft.EditorGroupInput(seeded.Groups[0], [Node(Tehran)], [], []),
                new ShippingAuthoringDraft.EditorGroupInput(seeded.Groups[1], [Node(Isfahan)], [], []),
            ],
            [new ShippingAuthoringDraft.MethodTranslationInput(Persian, "پست پیشتاز", null)],
            false,
            null,
            null,
            true,
            ShippingFreightPayerMode.Prepaid,
            ShippingAuthoringCalculationMode.Manual,
            false,
            null);

        // (1) The SECOND staged SaveChanges fails after the FIRST one completed, with ONE named sentinel.
        var sentinel = new SentinelFailureException("tb199: injected failure after the staged write");
        var failing = new FailOnSecondSaveInterceptor(sentinel);
        await using (var failureContext = await NewContextAsync(failing))
        {
            var thrown = await Assert.ThrowsAnyAsync<Exception>(() =>
                new ShippingAuthoringDraftDirectory(failureContext).ReplaceEditorDocumentAsync(
                    seeded.Owner, seeded.DraftId, revision, Document(), ActiveLanguages, "IRR", CancellationToken.None));

            // The exact sentinel instance (unwrapped if the provider wrapped it) — not "any exception".
            var actual = thrown;
            while (actual is not SentinelFailureException && actual.InnerException is not null)
            {
                actual = actual.InnerException;
            }

            Assert.Same(sentinel, actual);
            Assert.Equal(2, failing.Attempts);
            Assert.Equal(1, failing.Completed);
            Assert.All(failureContext.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
        }

        // Immediately after the failure — before any cancellation attempt — the whole tree is the baseline.
        Assert.Equal(baseline, await SnapshotAsync(seeded.DraftId));

        // (2) Caller cancellation at the SAME boundary, with the caller's real CTS.
        using var cancellation = new CancellationTokenSource();
        var cancelling = new CancelOnSecondSaveInterceptor(cancellation);
        var expectedToken = cancellation.Token;
        string expected;
        await using (var cancelContext = await NewContextAsync(cancelling))
        {
            var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new ShippingAuthoringDraftDirectory(cancelContext).ReplaceEditorDocumentAsync(
                    seeded.Owner, seeded.DraftId, revision, Document(), ActiveLanguages, "IRR", expectedToken));

            Assert.True(cancellation.IsCancellationRequested);
            Assert.Equal(expectedToken, cancelled.CancellationToken);
            Assert.Equal(2, cancelling.Attempts);
            Assert.Equal(1, cancelling.Completed);
            Assert.All(cancelContext.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
            Assert.Equal(0, await cancelContext.SaveChangesAsync());
            Assert.Equal(baseline, await SnapshotAsync(seeded.DraftId));

            // Retrying in the SAME writing context at the ORIGINAL revision succeeds exactly once.
            var retried = await new ShippingAuthoringDraftDirectory(cancelContext).ReplaceEditorDocumentAsync(
                seeded.Owner, seeded.DraftId, revision, Document(), ActiveLanguages, "IRR", CancellationToken.None);
            Assert.True(retried.IsSuccess, retried.IsFailure ? retried.Errors[0].Code : null);
            Assert.Equal(revision + 1, retried.Value.Revision);
            expected = Snapshot(retried.Value);
            Assert.Equal(expected, await SnapshotAsync(seeded.DraftId));
        }

        // Only the ONE successful retry is stored: no duplicate group/row residue from the failed or
        // cancelled attempts, no extra revision advance, and exactly one new Shiraz group.
        var stored = await SnapshotAsync(seeded.DraftId);
        Assert.Equal(expected, stored);
        Assert.Equal(revision + 1, (await ReadStampAsync(seeded.DraftId)).Revision);
        Assert.Equal(
            1,
            stored.Split('\n').Count(line => line.Contains(SnapshotOfNode(Shiraz).TrimStart('|'), StringComparison.Ordinal)));
    }

    // ---------- complete seeding ----------

    private sealed record Seeded(ShippingAuthoringOwnerScope Owner, Guid DraftId, Guid[] Groups, long Revision);

    /// <summary>
    /// Seeds a COMPLETE nontrivial authored graph through the canonical directory API: two groups with
    /// distinct selections, three rate rows with distinct amounts, two languages with distinct descriptions,
    /// a group title, a seven-day schedule, both method settings and a type selection.
    /// </summary>
    private static async Task<Seeded> SeedCompleteAsync(string tag)
    {
        var owner = ShippingAuthoringOwnerScope.ForStore(tag + "-" + Guid.NewGuid().ToString("N"));
        Guid draftId;
        Guid[] groups;
        long revision;

        await using (var context = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(context);
            var created = await directory.CreateAsync(
                owner, "post", null, [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])], CancellationToken.None);
            Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
            draftId = created.Value.DraftId;
            var first = Assert.Single(created.Value.RegionGroups).GroupId;

            var second = await directory.ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [
                    new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
                    new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
                ],
                CancellationToken.None);
            Assert.True(second.IsSuccess, second.IsFailure ? second.Errors[0].Code : null);
            groups = second.Value.RegionGroups.OrderBy(group => group.Order).Select(group => group.GroupId).ToArray();
            Assert.Equal(2, groups.Length);

            var rowsA = await directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groups[0],
                2,
                [Row(0, 1000, null, null, null, null, 1, 3, freight: 25000m, packaging: 5000m),
                 Row(1001, 5000, null, null, null, null, 2, 5, freight: 41000m, packaging: 7500m)],
                "IRR",
                CancellationToken.None);
            Assert.True(rowsA.IsSuccess, rowsA.IsFailure ? rowsA.Errors[0].Code : null);

            var rowsB = await directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groups[1],
                3,
                [Row(0, 3000, null, null, null, null, 3, 6, freight: 18000m, packaging: 2500m)],
                "IRR",
                CancellationToken.None);
            Assert.True(rowsB.IsSuccess, rowsB.IsFailure ? rowsB.Errors[0].Code : null);

            var presentation = await directory.ReplacePresentationAsync(
                owner,
                draftId,
                4,
                [
                    new ShippingAuthoringDraft.MethodTranslationInput(Persian, "پست پیشتاز", "ارسال سریع"),
                    new ShippingAuthoringDraft.MethodTranslationInput(English, "Express post", "fast delivery"),
                ],
                [new ShippingAuthoringDraft.GroupTitleInput(groups[0], Persian, "پایتخت")],
                ActiveLanguages,
                CancellationToken.None);
            Assert.True(presentation.IsSuccess, presentation.IsFailure ? presentation.Errors[0].Code : null);

            var schedule = await directory.ReplaceWeeklyScheduleAsync(
                owner,
                draftId,
                5,
                "Asia/Tehran",
                Days((6, true, new TimeOnly(9, 0), new TimeOnly(13, 0)), (1, true, new TimeOnly(8, 0), new TimeOnly(12, 0))),
                CancellationToken.None);
            Assert.True(schedule.IsSuccess, schedule.IsFailure ? schedule.Errors[0].Code : null);

            var settings = await directory.ReplaceMethodSettingsAsync(
                owner,
                draftId,
                6,
                ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual,
                CancellationToken.None);
            Assert.True(settings.IsSuccess, settings.IsFailure ? settings.Errors[0].Code : null);

            var type = await directory.ReplaceTypeAssignmentAsync(
                owner, draftId, 7, TypeDefinition, CancellationToken.None);
            Assert.True(type.IsSuccess, type.IsFailure ? type.Errors[0].Code : null);
            revision = type.Value.Revision;
            Assert.Equal(8, revision);
        }

        return new Seeded(owner, draftId, groups, revision);
    }

    private static IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput> Days(
        params (int Weekday, bool IsWorking, TimeOnly Start, TimeOnly End)[] overrides)
    {
        var byWeekday = overrides.ToDictionary(item => item.Weekday);
        return Enumerable.Range(1, 7)
            .Select(weekday => byWeekday.TryGetValue(weekday, out var day)
                ? new ShippingAuthoringDraft.ScheduleDayInput(weekday, day.IsWorking, day.Start, day.End)
                : new ShippingAuthoringDraft.ScheduleDayInput(weekday, false, null, null))
            .ToArray();
    }

    /// <summary>Rebuilds the editor document that reproduces the stored draft exactly (identity ids included).</summary>
    private static ShippingAuthoringDraft.EditorDocumentInput DocumentFrom(
        ShippingAuthoringDraft draft,
        bool includeOptionalSections)
    {
        var groups = draft.RegionGroups
            .OrderBy(group => group.Order)
            .Select(group => new ShippingAuthoringDraft.EditorGroupInput(
                group.GroupId,
                group.Areas.OrderBy(area => area.NodeId).Select(area => area.Node).ToArray(),
                group.RateRows
                    .OrderBy(row => row.RateRowId)
                    .Select(row => new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                        row.RateRowId,
                        row.MinWeightGrams,
                        row.MaxWeightGrams,
                        row.MinItemCount,
                        row.MaxItemCount,
                        row.MinMerchandiseAmount,
                        row.MaxMerchandiseAmount,
                        row.FreightAmount,
                        row.PackagingAmount,
                        row.MinDeliveryDays,
                        row.MaxDeliveryDays))
                    .ToArray(),
                group.Titles
                    .OrderBy(title => title.LanguageId)
                    .Select(title => new ShippingAuthoringDraft.EditorGroupTitleInput(title.LanguageId, title.Title))
                    .ToArray()))
            .ToArray();

        var translations = draft.Translations
            .OrderBy(translation => translation.LanguageId)
            .Select(translation => new ShippingAuthoringDraft.MethodTranslationInput(
                translation.LanguageId,
                translation.Name,
                translation.Description))
            .ToArray();

        var schedule = includeOptionalSections ? draft.WeeklySchedule : null;
        var settings = includeOptionalSections ? draft.MethodSettings : null;
        var scheduleDays = schedule is null
            ? null
            : schedule.Days
                .OrderBy(day => day.Weekday)
                .Select(day => new ShippingAuthoringDraft.ScheduleDayInput(
                    day.Weekday, day.IsWorking, day.StartTime, day.EndTime))
                .ToArray();

        return new ShippingAuthoringDraft.EditorDocumentInput(
            groups,
            translations,
            schedule is not null,
            schedule?.TimeZoneId,
            scheduleDays,
            settings is not null,
            settings?.FreightPaymentMode,
            settings?.CalculationMode,
            includeOptionalSections,
            includeOptionalSections ? draft.ShippingTypeId : null);
    }

    // ---------- complete structural snapshot ----------

    /// <summary>
    /// ONE deterministic structural snapshot of EVERY authored root scalar and ALL own child rows, built
    /// from a fresh <c>AsNoTracking</c> context. Null and empty are distinct tokens, so an absent section can
    /// never be confused with an empty one, and two snapshots are equal only when every value matches.
    /// </summary>
    private static async Task<string> SnapshotAsync(Guid draftId)
    {
        await using var context = await NewContextAsync();
        var draft = await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId);
        var lines = new List<string>
        {
            $"root|{draft.DraftId}|{draft.OwnerKind}|{Tok(draft.OwnerTenantId)}|{Tok(draft.OwnerSellerPartyId)}"
            + $"|{draft.ServiceCode}|{Tok(draft.OptionCode)}|{Tok(draft.ShippingTypeId)}|{draft.Revision}"
            + $"|{Ts(draft.CreatedAt)}|{Ts(draft.UpdatedAt)}",
        };

        var groups = await context.Set<ShippingAuthoringRegionGroup>()
            .AsNoTracking().Where(group => group.DraftId == draftId).ToListAsync();
        var areas = await context.Set<ShippingAuthoringSelectedArea>()
            .AsNoTracking().Where(area => area.DraftId == draftId).ToListAsync();
        var groupIds = groups.Select(group => group.GroupId).ToArray();
        var rows = await context.Set<ShippingAuthoringRateRow>()
            .AsNoTracking().Where(row => groupIds.Contains(row.GroupId)).ToListAsync();
        var titles = await context.Set<ShippingAuthoringRegionGroupTitle>()
            .AsNoTracking().Where(title => title.DraftId == draftId).ToListAsync();
        var translations = await context.Set<ShippingAuthoringDraftTranslation>()
            .AsNoTracking().Where(translation => translation.DraftId == draftId).ToListAsync();
        var schedule = await context.Set<ShippingAuthoringWeeklySchedule>()
            .AsNoTracking().SingleOrDefaultAsync(item => item.DraftId == draftId);
        var days = await context.Set<ShippingAuthoringScheduleDay>()
            .AsNoTracking().Where(day => day.DraftId == draftId).ToListAsync();
        var settings = await context.Set<ShippingAuthoringMethodSettings>()
            .AsNoTracking().SingleOrDefaultAsync(item => item.DraftId == draftId);

        foreach (var group in groups.OrderBy(group => group.Order))
        {
            lines.Add($"group|{group.GroupId}|{group.Order}|{Tok(group.CurrencyCode)}");
            foreach (var area in areas.Where(area => area.GroupId == group.GroupId).OrderBy(area => area.NodeId))
            {
                lines.Add($"area|{area.SelectedAreaId}|{area.GroupId}|{area.NodeKind}|{area.NodeId}");
            }

            foreach (var row in rows.Where(row => row.GroupId == group.GroupId).OrderBy(row => row.RateRowId))
            {
                lines.Add(
                    $"row|{row.RateRowId}|{row.GroupId}|{Num(row.MinWeightGrams)}|{Num(row.MaxWeightGrams)}"
                    + $"|{Num(row.MinItemCount)}|{Num(row.MaxItemCount)}|{Num(row.MinMerchandiseAmount)}"
                    + $"|{Num(row.MaxMerchandiseAmount)}|{Num(row.FreightAmount)}|{Num(row.PackagingAmount)}"
                    + $"|{row.MinDeliveryDays}|{row.MaxDeliveryDays}");
            }

            foreach (var title in titles.Where(title => title.GroupId == group.GroupId).OrderBy(title => title.LanguageId))
            {
                lines.Add($"title|{title.GroupId}|{title.LanguageId}|{title.Title}");
            }
        }

        foreach (var translation in translations.OrderBy(translation => translation.LanguageId))
        {
            lines.Add($"translation|{translation.LanguageId}|{translation.Name}|{Tok(translation.Description)}");
        }

        lines.Add(schedule is null
            ? "schedule|<none>"
            : $"schedule|{schedule.TimeZoneId}|{Ts(schedule.UpdatedAt)}");
        foreach (var day in days.OrderBy(day => day.Weekday))
        {
            lines.Add(
                $"day|{day.Weekday}|{day.IsWorking}|{Tok(day.StartTime)}|{Tok(day.EndTime)}");
        }

        lines.Add(settings is null
            ? "settings|<none>"
            : $"settings|{settings.FreightPaymentMode}|{settings.CalculationMode}|{Ts(settings.UpdatedAt)}");

        return string.Join('\n', lines);
    }

    /// <summary>The same canonical snapshot, built from an in-memory saved aggregate (no second read).</summary>
    private static string Snapshot(ShippingAuthoringDraft draft)
    {
        var lines = new List<string>
        {
            $"root|{draft.DraftId}|{draft.OwnerKind}|{Tok(draft.OwnerTenantId)}|{Tok(draft.OwnerSellerPartyId)}"
            + $"|{draft.ServiceCode}|{Tok(draft.OptionCode)}|{Tok(draft.ShippingTypeId)}|{draft.Revision}"
            + $"|{Ts(draft.CreatedAt)}|{Ts(draft.UpdatedAt)}",
        };

        foreach (var group in draft.RegionGroups.OrderBy(group => group.Order))
        {
            lines.Add($"group|{group.GroupId}|{group.Order}|{Tok(group.CurrencyCode)}");
            foreach (var area in group.Areas.OrderBy(area => area.NodeId))
            {
                lines.Add($"area|{area.SelectedAreaId}|{group.GroupId}|{area.NodeKind}|{area.NodeId}");
            }

            foreach (var row in group.RateRows.OrderBy(row => row.RateRowId))
            {
                lines.Add(
                    $"row|{row.RateRowId}|{group.GroupId}|{Num(row.MinWeightGrams)}|{Num(row.MaxWeightGrams)}"
                    + $"|{Num(row.MinItemCount)}|{Num(row.MaxItemCount)}|{Num(row.MinMerchandiseAmount)}"
                    + $"|{Num(row.MaxMerchandiseAmount)}|{Num(row.FreightAmount)}|{Num(row.PackagingAmount)}"
                    + $"|{row.MinDeliveryDays}|{row.MaxDeliveryDays}");
            }

            foreach (var title in group.Titles.OrderBy(title => title.LanguageId))
            {
                lines.Add($"title|{group.GroupId}|{title.LanguageId}|{title.Title}");
            }
        }

        foreach (var translation in draft.Translations.OrderBy(translation => translation.LanguageId))
        {
            lines.Add($"translation|{translation.LanguageId}|{translation.Name}|{Tok(translation.Description)}");
        }

        lines.Add(draft.WeeklySchedule is { } schedule
            ? $"schedule|{schedule.TimeZoneId}|{Ts(schedule.UpdatedAt)}"
            : "schedule|<none>");
        var savedDays = draft.WeeklySchedule is { } savedSchedule
            ? savedSchedule.Days.OrderBy(day => day.Weekday).ToArray()
            : Array.Empty<ShippingAuthoringScheduleDay>();
        foreach (var day in savedDays)
        {
            lines.Add($"day|{day.Weekday}|{day.IsWorking}|{Tok(day.StartTime)}|{Tok(day.EndTime)}");
        }

        lines.Add(draft.MethodSettings is { } settings
            ? $"settings|{settings.FreightPaymentMode}|{settings.CalculationMode}|{Ts(settings.UpdatedAt)}"
            : "settings|<none>");

        return string.Join('\n', lines);
    }

    private static string Tok(object? value) => value switch
    {
        null => "<null>",
        TimeOnly time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>",
    };

    /// <summary>
    /// PostgreSQL stores microsecond precision, so a round-tripped timestamp is normalized to
    /// microseconds before comparison; anything coarser would silently hide a real second-level change.
    /// </summary>
    private static string Ts(DateTimeOffset value)
        => new DateTimeOffset(
                value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond),
                value.Offset)
            .ToString("O", CultureInfo.InvariantCulture);

    private static string Num(decimal? value)
        => value is null ? "<null>" : value.Value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Num(long? value)
        => value is null ? "<null>" : value.Value.ToString(CultureInfo.InvariantCulture);

    private static string Num(int? value)
        => value is null ? "<null>" : value.Value.ToString(CultureInfo.InvariantCulture);

    private static string SnapshotOfNode(Guid nodeId) => $"|{nodeId}";

    private static async Task<(long Revision, DateTimeOffset UpdatedAt)> ReadStampAsync(Guid draftId)
    {
        await using var context = await NewContextAsync();
        var draft = await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId);
        return (draft.Revision, draft.UpdatedAt);
    }

    private static async Task<Guid> FirstRowIdAsync(Guid draftId, Guid groupId)
    {
        await using var context = await NewContextAsync();
        return await context.Set<ShippingAuthoringRateRow>()
            .AsNoTracking()
            .Where(row => row.GroupId == groupId)
            .Select(row => row.RateRowId)
            .FirstAsync();
    }

    // ---------- helpers ----------

    private static ShippingDestinationNode Node(Guid id)
        => new(ShippingDestinationNodeKind.AdministrativeArea, id);

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput Row(
        long? minWeight,
        long? maxWeight,
        int? minCount,
        int? maxCount,
        decimal? minAmount,
        decimal? maxAmount,
        int? minDays,
        int? maxDays,
        Guid? rateRowId = null,
        decimal? freight = 25000m,
        decimal? packaging = 5000m)
        => new(rateRowId, minWeight, maxWeight, minCount, maxCount, minAmount, maxAmount, freight, packaging, minDays, maxDays);

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

    /// <summary>Named test-only failure identity: the proof may not accept "any exception".</summary>
    private sealed class SentinelFailureException(string message) : Exception(message);

    /// <summary>
    /// Bounded two-participant barrier (the established TB-190-R2/191/192/193/194 pattern). Arrivals are
    /// counted; the last one releases both. A participant that never arrives is bounded by a timeout so a
    /// failure cannot hang the suite, and a bare open transaction holds no row lock.
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
    /// Test-only synchronization point at the FIRST SavingChanges of a participant, i.e. after it read and
    /// prepared the draft root with the original revision and before it wrote anything. It also records the
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
    /// Fails the SECOND SaveChanges with the ONE named sentinel instance, after the first one completed, so
    /// the whole replacement is proven to roll back with no staged residue.
    /// </summary>
    private sealed class FailOnSecondSaveInterceptor(Exception sentinel) : SaveChangesInterceptor
    {
        public int Attempts { get; private set; }

        public int Completed { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Attempts++;
            if (Attempts >= 2)
            {
                throw sentinel;
            }

            var outcome = await base.SavingChangesAsync(eventData, result, cancellationToken);
            Completed++;
            return outcome;
        }
    }

    /// <summary>
    /// Cancels the CALLER token at the start of the second SaveChanges, proving real caller cancellation (not
    /// an arbitrary exception) rolls the replacement back and propagates the caller's own token.
    /// </summary>
    private sealed class CancelOnSecondSaveInterceptor(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        public int Attempts { get; private set; }

        public int Completed { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Attempts++;
            if (Attempts >= 2)
            {
                // Cancel the CALLER's own token and then surface it from the interceptor, so the second
                // save genuinely does not complete (it is not merely rejected later at commit).
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            var outcome = await base.SavingChangesAsync(eventData, result, cancellationToken);
            Completed++;
            return outcome;
        }
    }
}
