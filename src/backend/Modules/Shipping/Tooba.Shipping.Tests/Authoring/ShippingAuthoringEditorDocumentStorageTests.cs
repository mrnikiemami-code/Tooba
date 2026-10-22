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
/// TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199 — executed PostgreSQL facts for the existing-draft
/// editor document: ONE commit/ONE revision advance for a composite PUT that creates a group with rates
/// and titles, an honest no-op that writes nothing, and a rejected late child that changes nothing.
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
    private static readonly Guid Persian = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveTargetVerified;

    [Fact]
    public async Task One_composite_put_persists_the_whole_tree_with_one_revision_advance_then_noop_and_rejection_change_nothing()
    {
        var owner = ShippingAuthoringOwnerScope.ForStore("tb199-store-" + Guid.NewGuid().ToString("N"));
        Guid draftId;
        Guid retained;

        await using (var createContext = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(createContext);
            var created = await directory.CreateAsync(
                owner,
                "post",
                null,
                [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
                CancellationToken.None);
            Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
            draftId = created.Value.DraftId;
            retained = Assert.Single(created.Value.RegionGroups).GroupId;
        }

        var activeLanguages = new HashSet<Guid> { Persian };

        // ONE composite PUT: new group (with a rate row AND a title), a reorder, method text and settings.
        var document = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Isfahan)],
                    [Row(0, 1000, null, null, null, null, 1, 3)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "شمال")]),
                new ShippingAuthoringDraft.EditorGroupInput(retained, [Node(Tehran)], [], []),
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

        await using (var writeContext = await NewContextAsync())
        {
            var saved = await new ShippingAuthoringDraftDirectory(writeContext).ReplaceEditorDocumentAsync(
                owner,
                draftId,
                expectedRevision: 1,
                document,
                activeLanguages,
                "IRR",
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
            Assert.Equal(2, saved.Value.Revision);
        }

        // A NEW context proves the exact whole winning tree, stable retained ids and revision old+1.
        DateTimeOffset stamp;
        await using (var readContext = await NewContextAsync())
        {
            var read = await new ShippingAuthoringDraftDirectory(readContext).GetAsync(
                owner, draftId, CancellationToken.None);
            Assert.True(read.IsSuccess);

            var draft = read.Value;
            stamp = draft.UpdatedAt;
            Assert.Equal(2, draft.Revision);
            Assert.Equal("پست پیشتاز", Assert.Single(draft.Translations).Name);
            Assert.Equal(ShippingFreightPayerMode.Prepaid, draft.MethodSettings!.FreightPaymentMode);
            Assert.Equal(ShippingAuthoringCalculationMode.Manual, draft.MethodSettings.CalculationMode);

            var ordered = draft.RegionGroups.OrderBy(group => group.Order).ToArray();
            Assert.Equal(2, ordered.Length);
            Assert.Equal(0, ordered[0].Order);
            Assert.Equal(retained, ordered[1].GroupId);
            Assert.NotEqual(retained, ordered[0].GroupId);
            Assert.Equal("IRR", ordered[0].CurrencyCode);
            Assert.Equal(1000, Assert.Single(ordered[0].RateRows).MaxWeightGrams);
            Assert.Equal("شمال", Assert.Single(ordered[0].Titles).Title);
        }

        // An identical document at the current revision writes nothing: revision and timestamp preserved.
        await using (var noOpContext = await NewContextAsync())
        {
            var read = await new ShippingAuthoringDraftDirectory(noOpContext).GetAsync(
                owner, draftId, CancellationToken.None);
            var existing = read.Value;
            var ordered = existing.RegionGroups.OrderBy(group => group.Order).ToArray();
            var rowId = Assert.Single(ordered[0].RateRows).RateRowId;

            var identical = new ShippingAuthoringDraft.EditorDocumentInput(
                [
                    new ShippingAuthoringDraft.EditorGroupInput(
                        ordered[0].GroupId,
                        [Node(Isfahan)],
                        [Row(0, 1000, null, null, null, null, 1, 3, rowId)],
                        [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "شمال")]),
                    new ShippingAuthoringDraft.EditorGroupInput(ordered[1].GroupId, [Node(Tehran)], [], []),
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

            var plan = existing.PreflightEditorDocument(identical, activeLanguages, "IRR");
            var diagnostics =
                $"regions={plan.RegionsChanged}; rows={string.Join(",", plan.RateRows.Select(r => r.HasChanges))}; "
                + $"presentation={plan.Presentation.HasChanges}; titles={string.Join(",", plan.Titles.Select(t => t.Changed))}; "
                + $"settings={plan.Settings?.HasChanges}; type={plan.Type?.HasChanges}; "
                + $"storedCurrency={string.Join(",", ordered.Select(g => g.CurrencyCode ?? "<null>"))}; "
                + $"inputCurrency={string.Join(",", plan.RateRows.Select(r => r.CurrencyCode ?? "<null>"))}";

            var saved = await new ShippingAuthoringDraftDirectory(noOpContext).ReplaceEditorDocumentAsync(
                owner,
                draftId,
                expectedRevision: 2,
                identical,
                activeLanguages,
                "IRR",
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
            Assert.True(!plan.HasChanges, diagnostics);
            Assert.True(saved.Value.Revision == 2, diagnostics + $"; revision={saved.Value.Revision}");
            Assert.Equal(stamp, saved.Value.UpdatedAt);
        }

        // A late-invalid child (reversed interval in a brand-new group) is rejected before any write.
        await using (var rejectContext = await NewContextAsync())
        {
            var invalid = new ShippingAuthoringDraft.EditorDocumentInput(
                [
                    new ShippingAuthoringDraft.EditorGroupInput(
                        null,
                        [Node(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))],
                        [Row(5000, 1000, null, null, null, null, 3, 1)],
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

            var rejected = await new ShippingAuthoringDraftDirectory(rejectContext).ReplaceEditorDocumentAsync(
                owner,
                draftId,
                expectedRevision: 2,
                invalid,
                activeLanguages,
                "IRR",
                CancellationToken.None);
            Assert.True(rejected.IsFailure);
            Assert.Equal(
                ShippingErrorCodes.AuthoringRateConditionInvalid,
                rejected.Errors[0].Code);
        }

        await using (var afterRejectContext = await NewContextAsync())
        {
            var read = await new ShippingAuthoringDraftDirectory(afterRejectContext).GetAsync(
                owner, draftId, CancellationToken.None);
            var draft = read.Value;
            Assert.Equal(2, draft.Revision);
            Assert.Equal(stamp, draft.UpdatedAt);
            var ordered = draft.RegionGroups.OrderBy(group => group.Order).ToArray();
            Assert.Equal(2, ordered.Length);
            Assert.Equal(retained, ordered[1].GroupId);
            Assert.Equal(1000, Assert.Single(ordered[0].RateRows).MaxWeightGrams);
            Assert.Equal("شمال", Assert.Single(ordered[0].Titles).Title);
        }

        // A stale token still wins over an otherwise identical payload.
        await using (var staleContext = await NewContextAsync())
        {
            var stale = await new ShippingAuthoringDraftDirectory(staleContext).ReplaceEditorDocumentAsync(
                owner,
                draftId,
                expectedRevision: 1,
                document,
                activeLanguages,
                "IRR",
                CancellationToken.None);
            Assert.True(stale.IsFailure);
            Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, stale.Errors[0].Code);
        }
    }

    [Fact]
    public async Task A_rejected_last_child_leaves_no_tracker_residue_and_a_valid_retry_succeeds()
    {
        await using var seed = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb199-retry-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var retained = Assert.Single(created.Value.RegionGroups).GroupId;
        var activeLanguages = new HashSet<Guid> { Persian };

        // A row id that belongs to a DIFFERENT group of the same draft is a claim, not a hint.
        Guid foreignRowId;
        await using (var prepare = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(prepare);
            var withRows = await directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                retained,
                expectedRevision: 1,
                [Row(0, 1000, null, null, null, null, 1, 3)],
                "IRR",
                CancellationToken.None);
            Assert.True(withRows.IsSuccess, withRows.IsFailure ? withRows.Errors[0].Code : null);
            foreignRowId = Assert.Single(withRows.Value.RegionGroups.Single().RateRows).RateRowId;

            var second = await directory.ReplaceRegionGroupsAsync(
                owner,
                draftId,
                expectedRevision: 2,
                [
                    new ShippingAuthoringRegionGroupInput(retained, [Node(Tehran)]),
                    new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
                ],
                CancellationToken.None);
            Assert.True(second.IsSuccess, second.IsFailure ? second.Errors[0].Code : null);
        }

        // One context performs a rejected composite PUT: valid earlier edits then a late-invalid child.
        await using var context = await NewContextAsync();
        var before = await new ShippingAuthoringDraftDirectory(context).GetAsync(
            owner, draftId, CancellationToken.None);
        var revision = before.Value.Revision;
        Assert.Equal(3, revision);

        var otherGroup = before.Value.RegionGroups.OrderBy(group => group.Order).Last().GroupId;
        var lateInvalid = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                // Valid earlier edit: retitle the first group and reorder nothing.
                new ShippingAuthoringDraft.EditorGroupInput(
                    before.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId,
                    [Node(Tehran)],
                    [],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "عنوان")]),
                // Late-invalid child inside a brand-new group.
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))],
                    [Row(9000, 9000, null, null, null, null, 5, 2)],
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

        var rejected = await new ShippingAuthoringDraftDirectory(context).ReplaceEditorDocumentAsync(
            owner,
            draftId,
            revision,
            lateInvalid,
            activeLanguages,
            "IRR",
            CancellationToken.None);
        Assert.True(rejected.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringRateConditionInvalid, rejected.Errors[0].Code);

        // The rejected attempt left NOTHING pending in the very context that attempted it.
        Assert.Empty(context.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        Assert.Equal(0, await context.SaveChangesAsync());

        // Retained ids that do not belong to the named group and unknown group ids are rejected.
        var foreignRow = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    before.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId,
                    [Node(Tehran)],
                    [Row(0, 1000, null, null, null, null, 1, 3, Guid.NewGuid())],
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
            owner, draftId, revision, foreignRow, activeLanguages, "IRR", CancellationToken.None);
        Assert.True(foreignRejected.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringRateRowPayloadInvalid, foreignRejected.Errors[0].Code);

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
            owner, draftId, revision, unknownGroup, activeLanguages, "IRR", CancellationToken.None);
        Assert.True(unknownRejected.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringRegionPayloadInvalid, unknownRejected.Errors[0].Code);

        Assert.Empty(context.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        Assert.Equal(0, await context.SaveChangesAsync());

        // A valid retry at the ORIGINAL revision still succeeds from this same context and advances once.
        var valid = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    before.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId,
                    [Node(Tehran)],
                    [],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "عنوان")]),
                new ShippingAuthoringDraft.EditorGroupInput(otherGroup, [Node(Isfahan)], [], []),
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
            owner, draftId, revision, valid, activeLanguages, "IRR", CancellationToken.None);
        Assert.True(retried.IsSuccess, retried.IsFailure ? retried.Errors[0].Code : null);
        Assert.Equal(revision + 1, retried.Value.Revision);

        await using var verification = await NewContextAsync();
        var stored = await new ShippingAuthoringDraftDirectory(verification).GetAsync(owner, draftId, CancellationToken.None);
        Assert.True(stored.IsSuccess);
        Assert.Equal(revision + 1, stored.Value.Revision);
        Assert.Equal("عنوان", stored.Value.RegionGroups
            .OrderBy(group => group.Order).First().Titles.Single().Title);
        Assert.Equal("پست پیشتاز", stored.Value.Translations.Single().Name);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, stored.Value.MethodSettings!.FreightPaymentMode);
        Assert.Equal(ShippingAuthoringCalculationMode.Free, stored.Value.MethodSettings.CalculationMode);
    }

    [Fact]
    public async Task Editor_and_region_writes_race_on_one_root_revision_and_only_the_winner_tree_survives()
    {
        await using var seed = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb199-race-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var retained = Assert.Single(created.Value.RegionGroups).GroupId;
        var activeLanguages = new HashSet<Guid> { Persian };

        var gate = new TwoReaderGate();
        var editorInterceptor = new TwoReaderBarrierInterceptor(gate);
        var regionInterceptor = new TwoReaderBarrierInterceptor(gate);

        await using var editorContext = await NewContextAsync(editorInterceptor);
        await using var regionContext = await NewContextAsync(regionInterceptor);

        var editorDocument = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Isfahan)],
                    [Row(0, 1000, null, null, null, null, 1, 3)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "شمال")]),
                new ShippingAuthoringDraft.EditorGroupInput(retained, [Node(Tehran)], [], []),
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
                owner, draftId, 1, editorDocument, activeLanguages, "IRR", token)),
            RunAsync(1, token => new ShippingAuthoringDraftDirectory(regionContext).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                1,
                [new ShippingAuthoringRegionGroupInput(retained, [Node(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"))])],
                token)));

        // Both participants read the SAME root token 1 and were synchronized BEFORE any write.
        Assert.Equal(2, gate.Arrivals);
        Assert.Equal(1, editorInterceptor.OriginalRevision);
        Assert.Equal(1, regionInterceptor.OriginalRevision);

        var winner = Assert.Single(outcomes, outcome => !outcome.Result.IsFailure);
        var loser = Assert.Single(outcomes, outcome => outcome.Result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, loser.Result.Errors[0].Code);

        await using var verification = await NewContextAsync();
        var stored = await new ShippingAuthoringDraftDirectory(verification).GetAsync(owner, draftId, CancellationToken.None);
        Assert.True(stored.IsSuccess, stored.IsFailure ? stored.Errors[0].Code : null);
        var value = stored.Value;

        // Exactly one revision advance: both writers cannot have committed.
        Assert.Equal(2, value.Revision);

        if (winner.Index == 0)
        {
            // The editor winner's whole tree, and NOTHING of the losing region replacement.
            var ordered = value.RegionGroups.OrderBy(group => group.Order).ToArray();
            Assert.Equal(2, ordered.Length);
            Assert.Equal(retained, ordered[1].GroupId);
            Assert.Equal(1000, Assert.Single(ordered[0].RateRows).MaxWeightGrams);
            Assert.Equal("شمال", Assert.Single(ordered[0].Titles).Title);
            Assert.All(ordered[1].Areas, area => Assert.Equal(Tehran, area.NodeId));
            Assert.Empty(ordered[1].RateRows);
            Assert.Equal("پست پیشتاز", Assert.Single(value.Translations).Name);
            Assert.Equal(ShippingFreightPayerMode.Prepaid, value.MethodSettings!.FreightPaymentMode);
        }
        else
        {
            // The region winner's tree only: one retained group, Isfahan area and no editor side effects
            // (no new group, no rate row, no title, no method text or settings).
            var group = Assert.Single(value.RegionGroups);
            Assert.Equal(retained, group.GroupId);
            Assert.Equal(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Assert.Single(group.Areas).NodeId);
            Assert.Empty(group.RateRows);
            Assert.Empty(group.Titles);
            Assert.Empty(value.Translations);
            Assert.Null(value.MethodSettings);
        }

        // The ACTUAL losing tracker holds nothing a later SaveChanges could publish.
        var loserContext = loser.Index == 0 ? editorContext : regionContext;
        Assert.Equal(0, await loserContext.SaveChangesAsync());
        Assert.Empty(loserContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
    }

    [Fact]
    public async Task A_failed_second_staged_save_or_caller_cancellation_rolls_the_whole_document_back()
    {
        await using var seed = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb199-rollback-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var retained = Assert.Single(created.Value.RegionGroups).GroupId;
        var activeLanguages = new HashSet<Guid> { Persian };

        ShippingAuthoringDraft.EditorDocumentInput Document() => new(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Isfahan)],
                    [Row(0, 1000, null, null, null, null, 1, 3)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(Persian, "شمال")]),
                new ShippingAuthoringDraft.EditorGroupInput(retained, [Node(Tehran)], [], []),
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

        // (1) The SECOND staged SaveChanges fails after the first staged write was already executed.
        var failing = new FailOnSecondSaveInterceptor();
        await using (var failureContext = await NewContextAsync(failing))
        {
            var attempt = new ShippingAuthoringDraftDirectory(failureContext).ReplaceEditorDocumentAsync(
                owner, draftId, 1, Document(), activeLanguages, "IRR", CancellationToken.None);
            await Assert.ThrowsAnyAsync<Exception>(() => attempt);
            Assert.True(failing.SaveAttempts >= 2, $"save attempts={failing.SaveAttempts}");
            Assert.Empty(failureContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        }

        // (2) Caller cancellation at that same boundary, using the caller's real CTS.
        using var cancellation = new CancellationTokenSource();
        var cancelling = new CancelOnSecondSaveInterceptor(cancellation);
        await using (var cancelContext = await NewContextAsync(cancelling))
        {
            var attempt = new ShippingAuthoringDraftDirectory(cancelContext).ReplaceEditorDocumentAsync(
                owner, draftId, 1, Document(), activeLanguages, "IRR", cancellation.Token);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attempt);
            Assert.True(cancelling.SaveAttempts >= 2, $"save attempts={cancelling.SaveAttempts}");
            Assert.Empty(cancelContext.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        }

        // Fresh-context proof: no staged residue and the original single-group tree at revision 1.
        await using (var afterFailure = await NewContextAsync())
        {
            var stored = await new ShippingAuthoringDraftDirectory(afterFailure).GetAsync(owner, draftId, CancellationToken.None);
            Assert.True(stored.IsSuccess);
            Assert.Equal(1, stored.Value.Revision);
            var group = Assert.Single(stored.Value.RegionGroups);
            Assert.Equal(retained, group.GroupId);
            Assert.Equal(Tehran, Assert.Single(group.Areas).NodeId);
            Assert.Empty(group.RateRows);
            Assert.Empty(group.Titles);
            Assert.Empty(stored.Value.Translations);
            Assert.Null(stored.Value.MethodSettings);
            var stagedGroups = await afterFailure.Set<ShippingAuthoringRegionGroup>()
                .AsNoTracking()
                .Where(candidate => candidate.DraftId == draftId)
                .Select(candidate => candidate.Order)
                .ToArrayAsync();
            Assert.Equal([0], stagedGroups);
        }

        // A retry at the ORIGINAL revision succeeds and advances exactly once.
        await using (var retryContext = await NewContextAsync())
        {
            var retried = await new ShippingAuthoringDraftDirectory(retryContext).ReplaceEditorDocumentAsync(
                owner, draftId, 1, Document(), activeLanguages, "IRR", CancellationToken.None);
            Assert.True(retried.IsSuccess, retried.IsFailure ? retried.Errors[0].Code : null);
            Assert.Equal(2, retried.Value.Revision);
        }
    }

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
    /// prepared the draft root with revision 1 and before it wrote anything. It also records the concurrency
    /// token's ORIGINAL value, which is the exact predicate the later UPDATE uses.
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
    /// Fails at the START of the SECOND SaveChanges, i.e. after the staged first write and before commit, so
    /// the whole replacement is proven to roll back with no staged residue.
    /// </summary>
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
                throw new InvalidOperationException("tb199: injected failure after the staged write");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>
    /// Cancels the CALLER token at the start of the second SaveChanges, proving real caller cancellation (not
    /// an injected arbitrary exception) rolls the replacement back and propagates.
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
}
