using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219 — executed PostgreSQL facts for the versioned
/// selected-capture pointer and its Contracts-only reader.
/// </summary>
/// <remarks>
/// <para>
/// Target: the task-owned disposable instance from <see cref="ShippingCaptureRelationalTestTarget"/>
/// (127.0.0.1:15432 / shipping_tb214 / tb214), the same isolation family as TB-214. The older 55432 target and
/// the shared 5432 database are never used; a missing target throws so no fact can pass without a server.
/// </para>
/// <para>
/// Selection is publication <b>intent</b> only: no readiness, eligibility, activation or quote claim.
/// </para>
/// </remarks>
public sealed class ShippingSelectedCapturedRevisionStorageTests
{
    private static readonly Guid Persian = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid English = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CityNode = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ShippingType = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _schemaReady;

    [Fact]
    public async Task The_pointer_table_unique_owner_binding_index_and_version_constraints_exist()
    {
        await using var context = await NewContextAsync();

        var table = await context.Database
            .SqlQueryRaw<string>(
                "SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'shipping' AND table_name = 'selected_captured_revisions'")
            .SingleAsync();
        Assert.Equal("selected_captured_revisions", table);

        // NULLS NOT DISTINCT: a nullable owner/option column can never bypass the one-pointer rule.
        var index = await context.Database
            .SqlQueryRaw<string>(
                "SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'shipping' AND indexname = 'ux_selected_captured_revisions_owner_binding'")
            .SingleAsync();
        Assert.Contains("UNIQUE", index, StringComparison.Ordinal);
        Assert.Contains("NULLS NOT DISTINCT", index, StringComparison.Ordinal);
        Assert.Contains("binding_service_key", index, StringComparison.Ordinal);
        Assert.Contains("binding_option_key", index, StringComparison.Ordinal);
        Assert.DoesNotContain("destination", index, StringComparison.Ordinal);

        var checks = await context.Database
            .SqlQueryRaw<string>(
                "SELECT conname AS \"Value\" FROM pg_constraint WHERE conrelid = 'shipping.selected_captured_revisions'::regclass AND contype = 'c' ORDER BY conname")
            .ToListAsync();
        Assert.Contains("ck_selected_captured_revisions_owner_coherent", checks);
        Assert.Contains("ck_selected_captured_revisions_version_positive", checks);
        Assert.Contains("ck_selected_captured_revisions_service_key_canonical", checks);
        Assert.Contains("ck_selected_captured_revisions_option_key_canonical", checks);
        Assert.Contains("ck_selected_captured_revisions_selected_snapshot_not_empty", checks);
    }

    [Fact]
    public async Task First_selection_reads_exact_provenance_and_a_later_draft_edit_changes_nothing_selected()
    {
        var (owner, tenant, draftId, revision, snapshotId, payload) = await SeedCapturedDraftAsync();

        await using (var write = await NewContextAsync())
        {
            var selected = await Directory(write, tenant).SelectAsync(
                owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 0, CancellationToken.None);

            Assert.True(selected.IsSuccess, selected.IsFailure ? selected.Errors[0].Code : null);
            Assert.True(selected.Value.Exists);
            Assert.Equal(ShippingSelectedCaptureState.Selected, selected.Value.State);
            Assert.Equal(1, selected.Value.SelectionVersion);
            Assert.Equal(snapshotId, selected.Value.SnapshotId);
            Assert.Equal(draftId, selected.Value.DraftId);
            Assert.Equal(revision, selected.Value.SourceRevision);
        }

        // The draft advances: selection is an explicit pointer, so a later edit changes NOTHING selected.
        await using (var edit = await NewContextAsync())
        {
            var current = (await new ShippingAuthoringDraftDirectory(edit).GetAsync(owner, draftId, CancellationToken.None)).Value;
            var saved = await new ShippingAuthoringDraftDirectory(edit).ReplaceMethodSettingsAsync(
                owner,
                draftId,
                current.Revision,
                Tooba.Shipping.Contracts.Packaging.ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual,
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
            Assert.True(saved.Value.Revision > revision);
        }

        await using (var read = await NewContextAsync())
        {
            var result = await Directory(read, tenant).ReadAsync(
                new ShippingSelectedCapturedRevisionRequest(
                    ShippingCapturedQuoteOwnerKind.Store, tenant, null, payload.ServiceCode, payload.OptionCode),
                CancellationToken.None);

            Assert.Equal(ShippingSelectedCaptureVerdict.Selected, result.Verdict);
            Assert.Equal(ShippingSelectedCaptureReadState.Selected, result.State);
            Assert.Equal(1, result.SelectionVersion);
            // EXACTLY the old immutable capture, never the newer draft revision.
            Assert.Equal(snapshotId, result.SnapshotId);
            Assert.Equal(revision, result.SourceRevision);
            Assert.Equal(payload.FormatVersion, result.FormatVersion);
            Assert.Equal(payload.ShippingTypeId, result.ShippingTypeId);
            Assert.Equal(2, result.Translations.Count);
            Assert.Equal(English, result.Translations[1].LanguageId);
            Assert.Null(result.Translations[1].Description);
        }
    }

    [Fact]
    public async Task Selecting_a_newer_capture_then_rolling_back_to_an_older_existing_one_advances_the_version()
    {
        var (owner, tenant, draftId, firstRevision, firstSnapshot, payload) = await SeedCapturedDraftAsync();
        var secondRevision = await AdvanceAndCaptureAsync(owner, draftId, firstRevision);
        Assert.True(secondRevision.Revision > firstRevision);

        await using var context = await NewContextAsync();
        var directory = Directory(context, tenant);

        var first = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, firstRevision, 0, CancellationToken.None);
        Assert.True(first.IsSuccess, first.IsFailure ? first.Errors[0].Code : null);
        Assert.Equal(1, first.Value.SelectionVersion);

        var second = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, secondRevision.Revision, 1, CancellationToken.None);
        Assert.True(second.IsSuccess, second.IsFailure ? second.Errors[0].Code : null);
        Assert.Equal(2, second.Value.SelectionVersion);
        Assert.Equal(secondRevision.SnapshotId, second.Value.SnapshotId);
        Assert.Equal(secondRevision.Revision, second.Value.SourceRevision);

        // Rollback = deliberately selecting the OLDER existing capture at the CURRENT pointer version.
        var rollback = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, firstRevision, 2, CancellationToken.None);
        Assert.True(rollback.IsSuccess, rollback.IsFailure ? rollback.Errors[0].Code : null);
        Assert.Equal(3, rollback.Value.SelectionVersion);
        Assert.Equal(firstSnapshot, rollback.Value.SnapshotId);
        Assert.Equal(firstRevision, rollback.Value.SourceRevision);
    }

    [Fact]
    public async Task Same_target_at_the_current_version_is_a_noop_and_a_stale_version_always_conflicts()
    {
        var (owner, tenant, draftId, revision, snapshotId, payload) = await SeedCapturedDraftAsync();

        await using var context = await NewContextAsync();
        var directory = Directory(context, tenant);

        var created = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 0, CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var stored = await context.SelectedCapturedRevisions.AsNoTracking().SingleAsync(row => row.OwnerTenantId == tenant);
        var storedAt = stored.UpdatedAt;

        // Same target at the CURRENT expected version: truthful no-op that preserves version AND time.
        var noop = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 1, CancellationToken.None);
        Assert.True(noop.IsSuccess, noop.IsFailure ? noop.Errors[0].Code : null);
        Assert.Equal(1, noop.Value.SelectionVersion);
        Assert.Equal(snapshotId, noop.Value.SnapshotId);
        var afterNoop = await context.SelectedCapturedRevisions.AsNoTracking().SingleAsync(row => row.OwnerTenantId == tenant);
        Assert.Equal(storedAt, afterNoop.UpdatedAt);

        // Stale expected version conflicts even when the target is identical; nothing is written.
        var stale = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 7, CancellationToken.None);
        Assert.True(stale.IsFailure);
        Assert.Equal(ShippingErrorCodes.SelectedCaptureConflict, stale.FirstError.Code);
        Assert.Equal(1, (await context.SelectedCapturedRevisions.AsNoTracking().SingleAsync(row => row.OwnerTenantId == tenant)).SelectionVersion);

        // A pointer that does not exist yet (an existing capture of a different method) is creatable only
        // from version 0; expected version 1 conflicts and writes nothing.
        var (otherOwner, otherTenant, otherDraft, otherRevision, _, otherPayload) =
            await SeedCapturedDraftAsync("express");
        await using var otherContext = await NewContextAsync();
        var otherDirectory = Directory(otherContext, otherTenant);
        var absentWrongVersion = await otherDirectory.SelectAsync(
            otherOwner, otherPayload.ServiceCode, otherPayload.OptionCode, otherDraft, otherRevision, 1,
            CancellationToken.None);
        Assert.True(absentWrongVersion.IsFailure);
        Assert.Equal(ShippingErrorCodes.SelectedCaptureConflict, absentWrongVersion.FirstError.Code);
        Assert.Equal(0, await CountPointersAsync(otherTenant));
        Assert.NotEqual(tenant, otherTenant);
    }

    [Fact]
    public async Task Clear_keeps_the_row_and_version_and_a_pre_clear_version_cannot_resurrect_the_target()
    {
        var (owner, tenant, draftId, revision, _, payload) = await SeedCapturedDraftAsync();

        await using var context = await NewContextAsync();
        var directory = Directory(context, tenant);

        var created = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 0, CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);

        var cleared = await directory.ClearAsync(
            owner, payload.ServiceCode, payload.OptionCode, 1, CancellationToken.None);
        Assert.True(cleared.IsSuccess, cleared.IsFailure ? cleared.Errors[0].Code : null);
        Assert.True(cleared.Value.Exists);
        Assert.Equal(ShippingSelectedCaptureState.Cleared, cleared.Value.State);
        Assert.Equal(2, cleared.Value.SelectionVersion);
        Assert.Null(cleared.Value.SnapshotId);

        // The row is retained (no delete, no version reset to 0 => no ABA).
        Assert.Equal(1, await CountPointersAsync(tenant));

        // A version from BEFORE the clear cannot bring the old target back.
        var resurrect = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 1, CancellationToken.None);
        Assert.True(resurrect.IsFailure);
        Assert.Equal(ShippingErrorCodes.SelectedCaptureConflict, resurrect.FirstError.Code);

        // Clearing an already-cleared pointer at the current version is a no-op.
        var clearedAgain = await directory.ClearAsync(
            owner, payload.ServiceCode, payload.OptionCode, 2, CancellationToken.None);
        Assert.True(clearedAgain.IsSuccess, clearedAgain.IsFailure ? clearedAgain.Errors[0].Code : null);
        Assert.Equal(2, clearedAgain.Value.SelectionVersion);
        Assert.Equal(ShippingSelectedCaptureState.Cleared, clearedAgain.Value.State);

        // An explicit new selection at the new version succeeds and advances once more.
        var reselected = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 2, CancellationToken.None);
        Assert.True(reselected.IsSuccess, reselected.IsFailure ? reselected.Errors[0].Code : null);
        Assert.Equal(3, reselected.Value.SelectionVersion);
        Assert.Equal(ShippingSelectedCaptureState.Selected, reselected.Value.State);

        // Clearing an ABSENT pointer at expected 0 returns absent without inserting anything.
        var otherOwner = ShippingAuthoringOwnerScope.ForStore("tb219-absent-" + Guid.NewGuid().ToString("N"));
        var absent = await directory.ClearAsync(
            otherOwner, payload.ServiceCode, payload.OptionCode, 0, CancellationToken.None);
        Assert.True(absent.IsSuccess, absent.IsFailure ? absent.Errors[0].Code : null);
        Assert.False(absent.Value.Exists);
        Assert.Equal(1, await CountPointersAsync(tenant));
    }

    [Fact]
    public async Task Foreign_missing_or_mismatched_captures_cannot_be_selected_and_owners_stay_separate()
    {
        var (owner, tenant, draftId, revision, snapshotId, payload) = await SeedCapturedDraftAsync();
        var other = await SeedCapturedDraftAsync();

        await using var context = await NewContextAsync();
        var directory = Directory(context, tenant);

        // A capture of ANOTHER owner is not selectable through this owner's scope.
        var foreign = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, other.DraftId, other.Revision, 0, CancellationToken.None);
        Assert.True(foreign.IsFailure);
        Assert.Equal(ShippingErrorCodes.SelectedCaptureSnapshotUnavailable, foreign.FirstError.Code);

        // A revision that was never captured cannot be selected, and NOTHING is captured here.
        var missingRevision = await directory.SelectAsync(
            owner, payload.ServiceCode, payload.OptionCode, draftId, revision + 99, 0, CancellationToken.None);
        Assert.True(missingRevision.IsFailure);
        Assert.Equal(ShippingErrorCodes.SelectedCaptureSnapshotUnavailable, missingRevision.FirstError.Code);
        Assert.Equal(0, await context.AuthoringRevisionSnapshots.CountAsync(row => row.SourceRevision == revision + 99));

        // A caller-declared binding that the verified payload does not have is refused.
        var wrongBinding = await directory.SelectAsync(
            owner, "another-service", payload.OptionCode, draftId, revision, 0, CancellationToken.None);
        Assert.True(wrongBinding.IsFailure);
        Assert.Equal(ShippingErrorCodes.SelectedCaptureBindingMismatch, wrongBinding.FirstError.Code);
        Assert.Equal(0, await CountPointersAsync(tenant));

        // Case/space variation of the SAME binding is the SAME pointer: it cannot create a second row.
        var same = await directory.SelectAsync(
            owner, "  POST ", null, draftId, revision, 0, CancellationToken.None);
        Assert.True(same.IsSuccess, same.IsFailure ? same.Errors[0].Code : null);
        var again = await directory.SelectAsync(
            owner, "Post", null, draftId, revision, 1, CancellationToken.None);
        Assert.True(again.IsSuccess, again.IsFailure ? again.Errors[0].Code : null);
        Assert.Equal(1, again.Value.SelectionVersion);
        Assert.Equal(1, await CountPointersAsync(tenant));
        Assert.Equal("post", (await context.SelectedCapturedRevisions.AsNoTracking().SingleAsync(row => row.OwnerTenantId == tenant)).BindingServiceKey);

        // A genuinely independent owner keeps its own pointer.
        var otherDirectory = Directory(context, other.Tenant);
        var otherSelected = await otherDirectory.SelectAsync(
            other.Owner, other.Payload.ServiceCode, other.Payload.OptionCode, other.DraftId, other.Revision, 0,
            CancellationToken.None);
        Assert.True(otherSelected.IsSuccess, otherSelected.IsFailure ? otherSelected.Errors[0].Code : null);
        Assert.Equal(2, await context.SelectedCapturedRevisions.CountAsync(row => row.OwnerTenantId == tenant || row.OwnerTenantId == other.Tenant));

        // Each owner reads ONLY its own selection.
        var mine = await directory.ReadAsync(
            new ShippingSelectedCapturedRevisionRequest(
                ShippingCapturedQuoteOwnerKind.Store, tenant, null, payload.ServiceCode, payload.OptionCode),
            CancellationToken.None);
        var theirs = await otherDirectory.ReadAsync(
            new ShippingSelectedCapturedRevisionRequest(
                ShippingCapturedQuoteOwnerKind.Store, other.Tenant, null, other.Payload.ServiceCode, other.Payload.OptionCode),
            CancellationToken.None);
        Assert.Equal(snapshotId, mine.SnapshotId);
        Assert.Equal(other.SnapshotId, theirs.SnapshotId);
        Assert.NotEqual(mine.SnapshotId, theirs.SnapshotId);
    }

    [Fact]
    public async Task Two_independent_contexts_at_the_same_expected_version_produce_exactly_one_winner()
    {
        var (owner, tenant, draftId, revision, _, payload) = await SeedCapturedDraftAsync();

        await using (var seed = await NewContextAsync())
        {
            var created = await Directory(seed, tenant).SelectAsync(
                owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 0, CancellationToken.None);
            Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        }

        // Both writers believe the pointer is at version 1 and each targets a DIFFERENT newer capture, so
        // each attempts a real change (no lawful no-op): exactly one may commit.
        var newer = await AdvanceAndCaptureAsync(owner, draftId, revision);
        var newest = await AdvanceAndCaptureAsync(owner, draftId, newer.Revision);
        await using var first = await NewContextAsync();
        await using var second = await NewContextAsync();

        var results = await Task.WhenAll(
            Directory(first, tenant).SelectAsync(
                owner, payload.ServiceCode, payload.OptionCode, draftId, newer.Revision, 1, CancellationToken.None),
            Directory(second, tenant).SelectAsync(
                owner, payload.ServiceCode, payload.OptionCode, draftId, newest.Revision, 1, CancellationToken.None));

        Assert.Single(results, result => result.IsSuccess);
        var loser = results.Single(result => result.IsFailure);
        Assert.Equal(ShippingErrorCodes.SelectedCaptureConflict, loser.FirstError.Code);

        // Exactly one row, ONE version advanced once (1 -> 2), and no hybrid: the winner's target is the
        // whole stored state.
        var stored = await first.SelectedCapturedRevisions.AsNoTracking().SingleAsync(row => row.OwnerTenantId == tenant);
        Assert.Equal(2, stored.SelectionVersion);
        Assert.Equal(results.Single(result => result.IsSuccess).Value.SnapshotId, stored.SelectedSnapshotId);
        Assert.Equal(1, await CountPointersAsync(tenant));
    }

    [Fact]
    public async Task A_failed_or_cancelled_pointer_save_leaves_no_pending_entry_and_preserves_callers_work()
    {
        var (owner, tenant, draftId, revision, _, payload) = await SeedCapturedDraftAsync();

        // 1) A sentinel fault at the write boundary: same instance propagates, nothing stays pending.
        await using (var context = await NewContextAsync(new ThrowSentinelOnSaveInterceptor()))
        {
            var sentinelInterceptor = new ThrowSentinelOnSaveInterceptor();
            await using var faultedContext = await NewContextAsync(sentinelInterceptor);
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => Directory(faultedContext, tenant)
                .SelectAsync(owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 0, CancellationToken.None));
            Assert.Same(sentinelInterceptor.Sentinel, thrown);
            Assert.Empty(faultedContext.SelectedCapturedRevisions.Local);
            Assert.Equal(0, await faultedContext.SaveChangesAsync());
        }

        Assert.Equal(0, await CountPointersAsync(tenant));

        // 2) Real caller cancellation at the same boundary, with the caller's own token identity.
        using (var cancellation = new CancellationTokenSource())
        {
            await using var context = await NewContextAsync(new CancelOnSaveInterceptor(cancellation));
            var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Directory(context, tenant)
                .SelectAsync(owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 0, cancellation.Token));
            Assert.Equal(cancellation.Token, cancelled.CancellationToken);
            Assert.Empty(context.SelectedCapturedRevisions.Local);
            Assert.Equal(0, await context.SaveChangesAsync());
        }

        Assert.Equal(0, await CountPointersAsync(tenant));

        // 3) Unrelated unsaved caller work is REFUSED before any write and stays tracked/Modified.
        await using (var dirty = await NewContextAsync())
        {
            var draft = await dirty.AuthoringDrafts.SingleAsync(row => row.DraftId == draftId);
            dirty.Entry(draft).Property(row => row.ServiceCode).CurrentValue = "post-edited";
            dirty.Entry(draft).Property(row => row.ServiceCode).IsModified = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => Directory(dirty, tenant)
                .SelectAsync(owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 0, CancellationToken.None));

            // The caller's edit is neither flushed nor cleared.
            Assert.Equal(EntityState.Modified, dirty.Entry(draft).State);
            Assert.Empty(dirty.SelectedCapturedRevisions.Local);
        }

        Assert.Equal(0, await CountPointersAsync(tenant));
    }

    [Fact]
    public async Task The_contracts_reader_is_selected_only_never_latest_and_fails_closed_on_scope_and_token()
    {
        var (owner, tenant, draftId, revision, _, payload) = await SeedCapturedDraftAsync();

        await using (var write = await NewContextAsync())
        {
            var created = await Directory(write, tenant).SelectAsync(
                owner, payload.ServiceCode, payload.OptionCode, draftId, revision, 0, CancellationToken.None);
            Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        }

        await using (var read = await NewContextAsync())
        {
            var directory = Directory(read, tenant);

            // Absent for a binding that was never selected, even though captures exist for the owner.
            var absent = await directory.ReadAsync(
                new ShippingSelectedCapturedRevisionRequest(
                    ShippingCapturedQuoteOwnerKind.Store, tenant, null, payload.ServiceCode, "another-option"),
                CancellationToken.None);
            Assert.Equal(ShippingSelectedCaptureVerdict.Absent, absent.Verdict);
            Assert.Equal(ShippingSelectedCaptureReadState.Absent, absent.State);
            Assert.Null(absent.SelectionVersion);
            Assert.Null(absent.SnapshotId);

            // Blank binding is an invalid request, not a "latest" read.
            var invalid = await directory.ReadAsync(
                new ShippingSelectedCapturedRevisionRequest(
                    ShippingCapturedQuoteOwnerKind.Store, tenant, null, "   ", null),
                CancellationToken.None);
            Assert.Equal(ShippingSelectedCaptureVerdict.InvalidRequest, invalid.Verdict);
            Assert.Equal(ShippingSelectedCaptureVerdictCodes.InvalidRequest, invalid.ReasonCode);

            // A tenant/kind that does not match the trusted context fails BEFORE any pointer read.
            var scopeMismatch = await directory.ReadAsync(
                new ShippingSelectedCapturedRevisionRequest(
                    ShippingCapturedQuoteOwnerKind.Store, "other-tenant", null, payload.ServiceCode, payload.OptionCode),
                CancellationToken.None);
            Assert.Equal(ShippingSelectedCaptureVerdict.OwnerScopeUnavailable, scopeMismatch.Verdict);
            Assert.Equal(ShippingSelectedCaptureVerdictCodes.OwnerScopeUnavailable, scopeMismatch.ReasonCode);

            // A cleared pointer reads as Cleared: no fallback to newest snapshot/config/another owner.
            var cleared = await directory.ClearAsync(
                owner, payload.ServiceCode, payload.OptionCode, 1, CancellationToken.None);
            Assert.True(cleared.IsSuccess, cleared.IsFailure ? cleared.Errors[0].Code : null);
            var afterClear = await directory.ReadAsync(
                new ShippingSelectedCapturedRevisionRequest(
                    ShippingCapturedQuoteOwnerKind.Store, tenant, null, payload.ServiceCode, payload.OptionCode),
                CancellationToken.None);
            Assert.Equal(ShippingSelectedCaptureVerdict.Cleared, afterClear.Verdict);
            Assert.Equal(ShippingSelectedCaptureReadState.Cleared, afterClear.State);
            Assert.Null(afterClear.SnapshotId);
            Assert.Empty(afterClear.Translations);
            Assert.Equal(2, afterClear.SelectionVersion);
        }

        // Provider failures and caller cancellation propagate untouched.
        var disposed = await NewContextAsync();
        await disposed.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => Directory(disposed, tenant).ReadAsync(
            new ShippingSelectedCapturedRevisionRequest(
                ShippingCapturedQuoteOwnerKind.Store, tenant, null, payload.ServiceCode, payload.OptionCode),
            CancellationToken.None));

        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            await using var context = await NewContextAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Directory(context, tenant).ReadAsync(
                new ShippingSelectedCapturedRevisionRequest(
                    ShippingCapturedQuoteOwnerKind.Store, tenant, null, payload.ServiceCode, payload.OptionCode),
                cancellation.Token));
        }
    }

    // ------------------------------------------------------------------ fixtures

    private static ShippingSelectedCapturedRevisionDirectory Directory(
        ShippingDbContext context,
        string? tenantId) =>
        new(
            context,
            new ShippingAuthoringDraftDirectory(context),
            new StubEdition(ToobaEdition.SingleStore),
            new StubTenant(tenantId));

    private static async Task<int> CountPointersAsync(string tenant)
    {
        await using var context = await NewContextAsync();
        return await context.SelectedCapturedRevisions.CountAsync(row => row.OwnerTenantId == tenant);
    }

    private static async Task<(
        ShippingAuthoringOwnerScope Owner,
        string Tenant,
        Guid DraftId,
        long Revision,
        Guid SnapshotId,
        ShippingAuthoringRevisionPayload Payload)> SeedCapturedDraftAsync(string serviceCode = "post")
    {
        var tenant = "tb219-store-" + Guid.NewGuid().ToString("N");
        var owner = ShippingAuthoringOwnerScope.ForStore(tenant);
        Guid draftId;
        long revision;

        await using (var context = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(context);
            var created = await directory.CreateAsync(
                owner,
                serviceCode,
                null,
                [new ShippingAuthoringRegionGroupInput(null, [new ShippingDestinationNode(ShippingDestinationNodeKind.City, CityNode)])],
                CancellationToken.None);
            Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
            draftId = created.Value.DraftId;
        }

        await using (var rates = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(rates);
            var current = (await directory.GetAsync(owner, draftId, CancellationToken.None)).Value;
            var groupId = current.RegionGroups.OrderBy(group => group.Order).First().GroupId;
            var saved = await directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                groupId,
                current.Revision,
                [new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                    null, 0, null, 1, null, null, null, 120_000.00m, 5_000.00m, 1, 3)],
                "IRT",
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        }

        await using (var presentation = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(presentation);
            var current = (await directory.GetAsync(owner, draftId, CancellationToken.None)).Value;
            var groupId = current.RegionGroups.OrderBy(group => group.Order).First().GroupId;
            var saved = await directory.ReplacePresentationAsync(
                owner,
                draftId,
                current.Revision,
                [
                    new ShippingAuthoringDraft.MethodTranslationInput(Persian, "پست پیشتاز", "توضیح فارسی"),
                    new ShippingAuthoringDraft.MethodTranslationInput(English, "Express post", null),
                ],
                [new ShippingAuthoringDraft.GroupTitleInput(groupId, Persian, "منطقه")],
                new HashSet<Guid> { Persian, English },
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        }

        await using (var type = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(type);
            var current = (await directory.GetAsync(owner, draftId, CancellationToken.None)).Value;
            var saved = await directory.ReplaceTypeAssignmentAsync(
                owner,
                draftId,
                current.Revision,
                ShippingType,
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
            revision = saved.Value.Revision;
        }

        await using (var capture = await NewContextAsync())
        {
            var captured = await new ShippingAuthoringDraftDirectory(capture)
                .CaptureRevisionAsync(owner, draftId, revision, CancellationToken.None);
            Assert.True(captured.IsSuccess, captured.IsFailure ? captured.Errors[0].Code : null);
            return (owner, tenant, draftId, revision, captured.Value.SnapshotId, captured.Value.ReadPayload());
        }
    }

    private static async Task<(long Revision, Guid SnapshotId)> AdvanceAndCaptureAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        long currentRevision)
    {
        long next;
        await using (var edit = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(edit);
            var current = (await directory.GetAsync(owner, draftId, CancellationToken.None)).Value;
            var saved = await directory.ReplaceMethodSettingsAsync(
                owner,
                draftId,
                current.Revision,
                current.Revision % 2 == 0
                    ? Tooba.Shipping.Contracts.Packaging.ShippingFreightPayerMode.CollectOnDelivery
                    : Tooba.Shipping.Contracts.Packaging.ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual,
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
            next = saved.Value.Revision;
            Assert.True(next > currentRevision);
        }

        await using (var capture = await NewContextAsync())
        {
            var captured = await new ShippingAuthoringDraftDirectory(capture)
                .CaptureRevisionAsync(owner, draftId, next, CancellationToken.None);
            Assert.True(captured.IsSuccess, captured.IsFailure ? captured.Errors[0].Code : null);
            return (next, captured.Value.SnapshotId);
        }
    }

    private static async Task<ShippingDbContext> NewContextAsync(params IInterceptor[] interceptors)
    {
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

    private sealed class ThrowSentinelOnSaveInterceptor : SaveChangesInterceptor
    {
        public InvalidOperationException Sentinel { get; } = new("tb219 sentinel: pointer write refused");

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

    private sealed class CancelOnSaveInterceptor(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        private bool _cancelled;

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
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class StubEdition(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current => new(edition, "test");
    }

    private sealed class StubTenant(string? tenantId) : ICurrentTenant
    {
        public TenantContext? Current => tenantId is null
            ? null
            : new TenantContext(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference("test"),
                null,
                null,
                null,
                "localhost",
                null);
    }}
