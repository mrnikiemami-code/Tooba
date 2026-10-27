using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Storefront;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-232-R2 — طول‌عمر تراکنشِ خودِ آداپتور و مقاومت پاک‌سازی در برابر خطای دوم.
/// </summary>
/// <remarks>
/// <para>
/// این کلاس فقط دربارهٔ یک چیز است: هر تراکنشی که آداپتور خودش باز می‌کند باید دقیقاً یک‌بار Dispose شود،
/// پاک‌سازیِ تلاش حتی وقتی rollback شکست می‌خورد اجرا شود، و هیچ خطای ثانویه‌ای (rollback/Dispose/بازخوانی)
/// جای خطای اصلی را نگیرد. اثبات با seamهای واقعی EF (interceptor) روی PostgreSQL دورریز انجام می‌شود، نه با
/// خواندن متن منبع و نه با hook تولیدی.
/// </para>
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class StorefrontCapturedSelectionTransactionTests
{
    private static readonly Guid CartId = Guid.Parse("01a17e10-0000-7000-9000-0000000000e1");
    private static readonly Guid OtherCartId = Guid.Parse("01a17e10-0000-7000-9000-0000000000e2");
    private static readonly Guid LineA = Guid.Parse("01a17e10-0000-7000-9000-0000000000a1");
    private static readonly Guid LineB = Guid.Parse("01a17e10-0000-7000-9000-0000000000b2");
    private static readonly Guid OtherLineId = Guid.Parse("01a17e10-0000-7000-9000-0000000000b3");
    private static readonly Guid SellerPartyId = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
    private static readonly Guid OtherSellerPartyId = Guid.Parse("01a17e10-0000-7000-9000-0000000000ab");
    private static readonly Guid Snapshot = Guid.Parse("01a17d0e-0419-7000-b575-f507d2f89abc");
    private static readonly Guid DraftSource = Guid.Parse("01a17d0d-e033-7000-af43-49a1daa28822");
    private static readonly Guid AreaGroupA = Guid.Parse("01a17e10-0000-7000-9000-0000000000c1");
    private static readonly Guid RateRowA = Guid.Parse("01a17e10-0000-7000-9000-0000000000d1");
    private static readonly Guid AreaGroupB = Guid.Parse("01a17e10-0000-7000-9000-0000000000c2");
    private static readonly Guid RateRowB = Guid.Parse("01a17e10-0000-7000-9000-0000000000d2");
    private static readonly Guid CountryId = Guid.Parse("01a17e10-0000-7000-9000-0000000000f1");
    private static readonly Guid ProvinceId = Guid.Parse("01a17e10-0000-7000-9000-0000000000f2");
    private static readonly Guid CityId = Guid.Parse("01a17e10-0000-7000-9000-0000000000f3");
    private static readonly DateOnly ReceivingDate = new(2026, 10, 28);
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    /// <summary>ساخت موفق تراکنش خودش را آزاد می‌کند و context بعدی می‌تواند تراکنش تازهٔ خودش را باز کند.</summary>
    [Fact]
    public async Task A_successful_create_and_replacement_dispose_their_own_transaction()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var probe = new TransactionProbe();

        await using (var db = CreateContext(target.ConnectionString, probe))
        {
            await db.Database.MigrateAsync();
            await ResetAsync(db);

            var draft = NewDraft();
            var groups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
            draft.AddShippingGroups(groups, Now);
            draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);

            await new StorefrontShippingDraftStore(db)
                .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);

            Assert.Equal(1, probe.Disposed);
            Assert.Null(db.Database.CurrentTransaction);

            // همان context می‌تواند عملیات صریح بعدی خودش را بدون هیچ تراکنشِ باقی‌مانده شروع/تمام کند.
            await using (var explicitTransaction = await db.Database.BeginTransactionAsync())
            {
                await explicitTransaction.CommitAsync();
            }

            Assert.Equal(2, probe.Disposed);
            Assert.Null(db.Database.CurrentTransaction);

            var store = new StorefrontShippingDraftStore(db);
            var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
            Assert.Equal(1, intent!.SelectionRevision);
            intent.Replace(
                null,
                8,
                intent.RecipientName,
                intent.ContactMobile,
                intent.ProvinceName,
                intent.CityName,
                intent.PostalAddress,
                intent.PostalCode,
                null,
                "post",
                "پست",
                24_000m,
                ReceivingDate,
                ReceivingDate,
                "09:00-12:00",
                "جایگزینی",
                Now.AddMinutes(3));

            await store.ReplaceCapturedSelectionAsync(
                intent,
                Groups(AreaGroupB, RateRowB, freight: 22_000m),
                isNew: false,
                expectedSelectionRevision: 1,
                CancellationToken.None);

            Assert.Equal(3, probe.Disposed);
            Assert.Null(db.Database.CurrentTransaction);
            Assert.Equal(0, await db.SaveChangesAsync());
        }

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(2, stored.SelectionRevision);
        Assert.Equal(AreaGroupB, Assert.Single(stored.ShippingGroups).AcceptedEvidence!.AreaGroupId);
    }

    /// <summary>
    /// لغو واقعی در مرز نوشتن (بین دو SaveChanges) — نه توکنِ از پیش لغوشده: DB قدیمی، هیچ تلاشِ در انتظار،
    /// توکن اصلی فراخوان روی خطا، و تراکنشِ خودمان Dispose شده است.
    /// </summary>
    [Fact]
    public async Task Cancellation_at_the_write_boundary_keeps_the_caller_token_and_disposes_the_transaction()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var probe = new TransactionProbe();

        await using var db = CreateContext(target.ConnectionString, probe);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await ResetAsync(db, OtherCartId);
        await SeedFirstSelectionAsync(db);
        var otherDraft = await SeedOtherDraftAsync(db);

        var store = new StorefrontShippingDraftStore(db);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        Assert.Equal(1, intent!.SelectionRevision);

        using var cancellation = new CancellationTokenSource();
        probe.CancelOnSaveNumber = 2;
        probe.Cancellation = cancellation;
        probe.ResetCounters();

        var replacement = Groups(AreaGroupB, RateRowB, freight: 90_000m);

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.ReplaceCapturedSelectionAsync(
                intent,
                replacement,
                isNew: false,
                expectedSelectionRevision: 1,
                cancellation.Token));

        // توکن همان توکن فراخوان است، نه یک توکن ساختگی و نه توکن پاک‌سازی.
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.True(cancellation.IsCancellationRequested);
        Assert.True(probe.CancelledAtSaveBoundary);
        Assert.Equal(1, probe.RollbackAttempts);

        // تراکنشِ خودمان آزاد شده و هیچ تلاشی در tracking نمانده است.
        Assert.Equal(1, probe.Disposed);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Empty(db.ChangeTracker.Entries<CartShippingDraftGroup>().Where(IsActive));
        Assert.Empty(db.ChangeTracker.Entries<CartShippingDraftGroupLine>().Where(IsActive));
        Assert.Equal(0, await db.SaveChangesAsync());

        // گراف نامرتبط دست‌نخورده است.
        Assert.Equal(EntityState.Unchanged, db.Entry(otherDraft).State);
        Assert.Equal(1, otherDraft.SelectionRevision);
        Assert.Equal("یادداشت دیگر", otherDraft.CustomerNote);

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, stored.SelectionRevision);
        Assert.Null(stored.CustomerNote);
        Assert.Equal(17_000m, stored.ShippingAmount);
        Assert.Equal(CountryId, stored.DestinationCountryId);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupA, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal([LineA], group.Lines.Select(line => line.CartLineId).ToArray());
    }

    /// <summary>
    /// خطای اصلیِ واقعیِ provider و سپس شکستِ اسکریپت‌شدهٔ rollback: همان نمونهٔ خطای اصلی منتشر می‌شود،
    /// گراف تلاش در tracking نمی‌ماند، گراف نامرتبط دست‌نخورده است و تراکنش Dispose شده است. چون rollback
    /// تأیید نشد، هیچ گرافِ «بازیابی‌شده»ای ساخته نمی‌شود.
    /// </summary>
    [Fact]
    public async Task A_rollback_failure_never_replaces_the_primary_failure_and_still_detaches_the_attempt()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var probe = new TransactionProbe();

        await using var db = CreateContext(target.ConnectionString, probe);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await ResetAsync(db, OtherCartId);
        await SeedFirstSelectionAsync(db);
        var otherDraft = await SeedOtherDraftAsync(db);

        var store = new StorefrontShippingDraftStore(db);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        Assert.Equal(1, intent!.SelectionRevision);

        // شکستِ اصلی واقعی provider: دو گروه با همان خط، ایندکس یکتای خط را می‌شکند.
        var broken = Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineA])
            .Concat(Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineA], seller: OtherSellerPartyId))
            .ToArray();

        probe.FailRollback = true;
        probe.ResetCounters();
        var readsBefore = probe.RecoveryReads;

        var error = await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            store.ReplaceCapturedSelectionAsync(
                intent,
                broken,
                isNew: false,
                expectedSelectionRevision: 1,
                CancellationToken.None));

        // همان نمونهٔ خطای اصلی — نه خطای اسکریپت‌شدهٔ rollback.
        Assert.Same(probe.PrimaryFailure, error);
        Assert.IsNotType<InvalidOperationException>(error);
        Assert.Equal(1, probe.RollbackFaults);
        Assert.Equal(1, probe.RollbackAttempts);

        // rollback تأیید نشد ⇒ هیچ بازخوانیِ بازیابی انجام نشده (گراف ساختگی ساخته نمی‌شود).
        Assert.Equal(readsBefore, probe.RecoveryReads);

        // با این حال detach اجرا شده: هیچ کار فعالی برای SaveChanges بعدی نمی‌ماند.
        Assert.Equal(1, probe.Disposed);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Empty(db.ChangeTracker.Entries<CartShippingDraftGroup>().Where(IsActive));
        Assert.Empty(db.ChangeTracker.Entries<CartShippingDraftGroupLine>().Where(IsActive));
        Assert.Equal(0, await db.SaveChangesAsync());

        // گراف نامرتبط تمیز و دست‌نخورده است.
        Assert.Equal(EntityState.Unchanged, db.Entry(otherDraft).State);
        Assert.Equal("یادداشت دیگر", otherDraft.CustomerNote);
        Assert.Single(otherDraft.ShippingGroups);
    }

    /// <summary>
    /// شکستِ بازخوانیِ بازیابی هم نباید خطای اصلی را جایگزین کند: خطای اصلی منتشر می‌شود و هیچ گراف ساختگی
    /// در tracking نمی‌ماند.
    /// </summary>
    [Fact]
    public async Task A_recovery_read_failure_preserves_the_primary_failure()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var probe = new TransactionProbe();

        await using var db = CreateContext(target.ConnectionString, probe);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var store = new StorefrontShippingDraftStore(db);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        Assert.Equal(1, intent!.SelectionRevision);

        var broken = Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineA])
            .Concat(Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineA], seller: OtherSellerPartyId))
            .ToArray();

        probe.FailRecoveryRead = true;
        probe.ResetCounters();

        var error = await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            store.ReplaceCapturedSelectionAsync(
                intent,
                broken,
                isNew: false,
                expectedSelectionRevision: 1,
                CancellationToken.None));

        Assert.Same(probe.PrimaryFailure, error);
        Assert.Equal(1, probe.RecoveryReadFaults);
        Assert.Equal(1, probe.Disposed);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(0, await db.SaveChangesAsync());
    }

    /// <summary>
    /// تراکنشِ بازِ فراخوان adopt نمی‌شود: نوشتن پیش از هر تغییر رد می‌شود و تراکنشِ فراخوان نه Rollback و نه
    /// Dispose می‌شود و همچنان قابل استفاده است.
    /// </summary>
    [Fact]
    public async Task A_caller_owned_transaction_is_refused_without_being_adopted_or_disposed()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var probe = new TransactionProbe();

        await using var db = CreateContext(target.ConnectionString, probe);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var store = new StorefrontShippingDraftStore(db);
        var intent = await store.GetForCapturedSelectionAsync(CartId, CancellationToken.None);
        Assert.Equal(1, intent!.SelectionRevision);

        probe.ResetCounters();

        await using var callerTransaction = await db.Database.BeginTransactionAsync();
        var callerDisposesAtStart = probe.Disposed;

        var error = await Assert.ThrowsAsync<Tooba.Order.Application.Storefront.StorefrontOrderException>(() =>
            store.ReplaceCapturedSelectionAsync(
                intent,
                Groups(AreaGroupB, RateRowB, freight: 90_000m),
                isNew: false,
                expectedSelectionRevision: 1,
                CancellationToken.None));

        Assert.Equal(
            Tooba.Order.Application.Storefront.Shipping.CapturedPreview.StorefrontCapturedSelectionReasons.PendingWork,
            error.Code);

        // تراکنشِ فراخوان دست‌نخورده و هنوز جاری است: نه Rollback شده و نه Dispose.
        Assert.Same(callerTransaction, db.Database.CurrentTransaction);
        Assert.Equal(callerDisposesAtStart, probe.Disposed);
        Assert.Equal(0, probe.RollbackAttempts);

        // و همچنان کار می‌کند.
        await callerTransaction.CommitAsync();

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, stored.SelectionRevision);
        Assert.Equal(AreaGroupA, Assert.Single(stored.ShippingGroups).AcceptedEvidence!.AreaGroupId);
    }

    // ---------------------------------------------------------------- helpers

    private static bool IsActive<TEntity>(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> entry)
        where TEntity : class =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    private static OrderDbContext CreateContext(string connectionString, params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            OrderDbContext.Schema,
            typeof(OrderDbContext));
        options.AddInterceptors(interceptors);
        return new OrderDbContext(options.Options);
    }

    private static async Task SeedFirstSelectionAsync(OrderDbContext db)
    {
        var draft = NewDraft();
        var groups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
        draft.AddShippingGroups(groups, Now);
        draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);
        await new StorefrontShippingDraftStore(db)
            .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);
    }

    private static async Task<CartShippingDraft> SeedOtherDraftAsync(OrderDbContext db)
    {
        var draft = CartShippingDraft.Create(
            OtherCartId,
            null,
            7,
            "گیرنده دیگر",
            "09120000001",
            "تهران",
            "تهران",
            "خیابان دو",
            "1234567891",
            null,
            "post",
            "پست",
            17_000m,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            "یادداشت دیگر",
            Now);

        var groups = CartShippingGroupPlanner.Plan(
            OtherCartId,
            [new CartShippingGroupCartLine(OtherLineId, OtherSellerPartyId, 1m)],
            [
                new CartShippingGroupRequest(
                    "post",
                    "post",
                    "standard",
                    OriginContext: null,
                    [OtherLineId],
                    SelectedDeliveryDate: ReceivingDate,
                    SelectedDeliveryTimeWindow: "09:00-12:00"),
            ],
            [
                new CartShippingGroupEligibleMethod(
                    "post",
                    "post",
                    "standard",
                    "پست",
                    17_000m,
                    "IRR",
                    2,
                    ReceivingDate,
                    OtherSellerPartyId),
            ],
            selectedDeliveryDate: null,
            selectedDeliveryTimeWindow: null,
            Now,
            () => Guid.NewGuid());

        draft.AddShippingGroups(groups, Now);
        await new StorefrontShippingDraftStore(db)
            .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);
        return draft;
    }

    private static CartShippingDraft NewDraft() =>
        CartShippingDraft.Create(
            CartId,
            null,
            7,
            "گیرنده آزمایشی",
            "09120000000",
            "تهران",
            "تهران",
            "خیابان یک",
            "1234567890",
            null,
            "post",
            "پست",
            17_000m,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            null,
            Now);

    private static IReadOnlyList<CartShippingDraftGroup> Groups(
        Guid areaGroup,
        Guid rateRow,
        decimal freight,
        IReadOnlyList<Guid>? lineIds = null,
        decimal packaging = 2_000m,
        Guid? seller = null)
    {
        var lines = lineIds ?? [LineA];
        var sellerId = seller ?? SellerPartyId;
        var groups = CartShippingGroupPlanner.Plan(
            CartId,
            lines.Select(lineId => new CartShippingGroupCartLine(lineId, sellerId, 1m)).ToArray(),
            [
                new CartShippingGroupRequest(
                    "post",
                    "post",
                    "standard",
                    OriginContext: null,
                    lines,
                    SelectedDeliveryDate: ReceivingDate,
                    SelectedDeliveryTimeWindow: "09:00-12:00"),
            ],
            [
                new CartShippingGroupEligibleMethod(
                    "post",
                    "post",
                    "standard",
                    "پست",
                    freight + packaging,
                    "IRR",
                    2,
                    ReceivingDate,
                    sellerId),
            ],
            selectedDeliveryDate: null,
            selectedDeliveryTimeWindow: null,
            Now,
            () => Guid.NewGuid());

        groups[0].ReplaceAcceptedEvidence(
            ShippingGroupAcceptedEvidence.Create(
                Snapshot,
                DraftSource,
                3,
                1,
                1,
                ShippingGroupOwner.Store("store-alpha"),
                areaGroup,
                rateRow,
                null,
                freight,
                packaging,
                "IRR",
                freight + packaging,
                0m,
                ReceivingDate,
                new TimeOnly(9, 0).Ticks,
                new TimeOnly(17, 0).Ticks,
                "Asia/Tehran",
                2,
                4),
            Now);

        return groups;
    }

    private static async Task ResetAsync(OrderDbContext db, Guid? cartId = null)
    {
        var target = cartId ?? CartId;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_group_lines WHERE shipping_group_id IN (SELECT shipping_group_id FROM \"order\".cart_shipping_groups WHERE cart_id = {target})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_groups WHERE cart_id = {target}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_drafts WHERE cart_id = {target}");
    }
}
