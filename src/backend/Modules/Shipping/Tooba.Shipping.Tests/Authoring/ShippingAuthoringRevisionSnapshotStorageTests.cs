using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Persistence;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214 — executed PostgreSQL facts for the immutable
/// revision capture (staging primitive; no publication, no pointer, no activation).
/// </summary>
/// <remarks>
/// <para>
/// The target is the parsed, task-owned disposable instance (<see cref="ShippingRelationalTestTarget"/>):
/// 127.0.0.1:55432 / shipping_tb190r1 / tb190r1. A missing or mismatched target throws (fail closed), so a
/// relational fact can never pass without a real server and the shared 5432 database is never touched.
/// </para>
/// <para>
/// The migration is applied by the test-owned migrator on that disposable instance only; the Host is never
/// restarted and no shared schema is mutated.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringRevisionSnapshotStorageTests
{
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;

    [Fact]
    public async Task Capture_is_a_complete_immutable_copy_and_a_later_edit_creates_a_new_revision()
    {
        var owner = ShippingAuthoringOwnerScope.ForStore("tb214-store-" + Guid.NewGuid().ToString("N"));
        Guid draftId;
        long firstRevision;

        await using (var seed = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(seed);
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
            draftId = created.Value.DraftId;
            firstRevision = created.Value.Revision;
        }

        await using (var writer = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(writer);
            var group = (await directory.GetAsync(owner, draftId, CancellationToken.None)).Value
                .RegionGroups.OrderBy(candidate => candidate.Order).First();
            var rates = await directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                group.GroupId,
                firstRevision,
                [
                    new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                        null, 0, 5000, 1, 10, null, null, 120_000.00m, 5_000.00m, 1, 3),
                    new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                        null, 5000, null, 11, null, null, null, 180_000.50m, 9_500.75m, 2, 5),
                ],
                "IRT",
                CancellationToken.None);
            Assert.True(rates.IsSuccess, rates.IsFailure ? rates.Errors[0].Code : null);
        }

        Guid capturedId;
        DateTimeOffset capturedAt;
        string capturedPayload;
        long capturedRevision;

        await using (var capture = await NewContextAsync())
        {
            var result = await new ShippingAuthoringDraftDirectory(capture)
                .CaptureRevisionAsync(owner, draftId, firstRevision + 1, CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null);
            capturedId = result.Value.SnapshotId;
            capturedAt = result.Value.CapturedAt;
            capturedPayload = result.Value.Payload;
            capturedRevision = result.Value.SourceRevision;

            // Two rows in the SAME area keep DIFFERENT packaging fees in the captured body.
            var group = result.Value.ReadPayload().Groups.Single();
            Assert.Equal(2, group.RateRows.Count);
            Assert.Equal(5_000.00m, group.RateRows.Single(row => row.MinWeightGrams == 0).PackagingAmount);
            Assert.Equal(9_500.75m, group.RateRows.Single(row => row.MinWeightGrams == 5000).PackagingAmount);
        }

        // Capture the SAME revision again: idempotent, one row, identical identity/time/content.
        await using (var again = await NewContextAsync())
        {
            var result = await new ShippingAuthoringDraftDirectory(again)
                .CaptureRevisionAsync(owner, draftId, capturedRevision, CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null);
            Assert.Equal(capturedId, result.Value.SnapshotId);
            Assert.Equal(capturedAt, result.Value.CapturedAt);
            Assert.Equal(capturedPayload, result.Value.Payload);
            Assert.Equal(1, await again.AuthoringRevisionSnapshots.CountAsync());
        }

        // A stale expected revision is refused with the canonical conflict and creates NO row.
        await using (var stale = await NewContextAsync())
        {
            var result = await new ShippingAuthoringDraftDirectory(stale)
                .CaptureRevisionAsync(owner, draftId, capturedRevision - 1, CancellationToken.None);
            Assert.True(result.IsFailure);
            Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, result.Errors[0].Code);
            Assert.Equal(1, await stale.AuthoringRevisionSnapshots.CountAsync());
        }

        // A FOREIGN owner gets the same safe not-found and creates NO row.
        await using (var foreign = await NewContextAsync())
        {
            var result = await new ShippingAuthoringDraftDirectory(foreign).CaptureRevisionAsync(
                ShippingAuthoringOwnerScope.ForStore("tb214-foreign-" + Guid.NewGuid().ToString("N")),
                draftId,
                capturedRevision,
                CancellationToken.None);
            Assert.True(result.IsFailure);
            Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, result.Errors[0].Code);
            Assert.Equal(1, await foreign.AuthoringRevisionSnapshots.CountAsync());
        }

        // Editing the draft afterwards leaves the OLD capture untouched and a new capture differs.
        await using (var edit = await NewContextAsync())
        {
            var directory = new ShippingAuthoringDraftDirectory(edit);
            var current = await directory.GetAsync(owner, draftId, CancellationToken.None);
            var group = current.Value.RegionGroups.OrderBy(candidate => candidate.Order).First();
            var saved = await directory.ReplaceGroupRateRowsAsync(
                owner,
                draftId,
                group.GroupId,
                current.Value.Revision,
                [new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(
                    null, 0, 900, 1, 2, null, null, 1_000.00m, 10.00m, 1, 2)],
                "IRT",
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        }

        await using (var verify = await NewContextAsync())
        {
            var stored = await verify.AuthoringRevisionSnapshots.AsNoTracking().SingleAsync();
            Assert.Equal(capturedId, stored.SnapshotId);
            Assert.Equal(capturedAt, stored.CapturedAt);
            Assert.Equal(capturedPayload, stored.Payload);
            Assert.Equal(capturedRevision, stored.SourceRevision);

            var next = await new ShippingAuthoringDraftDirectory(verify)
                .CaptureRevisionAsync(owner, draftId, capturedRevision + 1, CancellationToken.None);
            Assert.True(next.IsSuccess, next.IsFailure ? next.Errors[0].Code : null);
            Assert.NotEqual(capturedId, next.Value.SnapshotId);
            Assert.NotEqual(capturedPayload, next.Value.Payload);
            Assert.Equal(2, await verify.AuthoringRevisionSnapshots.CountAsync());
        }
    }

    private static async Task<ShippingDbContext> NewContextAsync()
    {
        var target = ShippingRelationalTestTarget.FromEnvironment();
        var builder = new DbContextOptionsBuilder<ShippingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder,
            target.ConnectionString,
            ShippingDbContext.Schema,
            typeof(ShippingDbContext));

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
        => new(ShippingDestinationNodeKind.City, id);
}
