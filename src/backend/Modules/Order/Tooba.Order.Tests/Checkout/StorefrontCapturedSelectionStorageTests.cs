using Microsoft.EntityFrameworkCore;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Infrastructure.Storefront;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232 — اثبات رابطه‌ای نوشتن اتمیک انتخاب capture‌شده روی
/// همان پایگاه‌دادهٔ دورانداختنیِ متعلق به این تسک (127.0.0.1:15433/order_tb231/tb231).
/// </summary>
/// <remarks>
/// <para>
/// همهٔ حقایق با <see cref="StorefrontShippingDraftStore.ReplaceCapturedSelectionAsync"/> واقعی اجرا
/// می‌شوند: تراکنش، CAS نسخهٔ انتخاب، حذف/درج گروه‌ها و شاهد ۲۳۱ ذخیره‌شده. هیچ fixture درون‌حافظه‌ای
/// جانشین ذخیره‌سازی نمی‌شود.
/// </para>
/// <para>
/// این اثبات هیچ سفارش/رزرو/پرداختی نمی‌سازد و هیچ ردیفی بیرون از سبدهای همین تست نمی‌نویسد.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedSelectionStorageTests
{
    private static readonly Guid CartId = Guid.Parse("01a17e00-0000-7000-9000-0000000000c1");
    private static readonly Guid SellerPartyId = Guid.Parse("01a17e00-0000-7000-9000-0000000000aa");
    private static readonly Guid LineA = Guid.Parse("01a17e00-0000-7000-9000-0000000000a1");
    private static readonly Guid LineB = Guid.Parse("01a17e00-0000-7000-9000-0000000000b2");
    private static readonly Guid Snapshot = Guid.Parse("01a17e00-0000-7000-9000-0000000000e1");
    private static readonly Guid DraftSource = Guid.Parse("01a17e00-0000-7000-9000-0000000000d1");
    private static readonly Guid AreaGroupA = Guid.Parse("01a17e00-0000-7000-9000-0000000000f1");
    private static readonly Guid RateRowA = Guid.Parse("01a17e00-0000-7000-9000-0000000000f2");
    private static readonly Guid AreaGroupB = Guid.Parse("01a17e00-0000-7000-9000-0000000000f3");
    private static readonly Guid RateRowB = Guid.Parse("01a17e00-0000-7000-9000-0000000000f4");
    private static readonly Guid ShippingTypeId = Guid.Parse("01a1785e-01f3-7000-b50d-d6372e756f89");
    private static readonly Guid CountryId = Guid.Parse("01a17e00-0000-7000-9000-0000000000c9");
    private static readonly Guid ProvinceId = Guid.Parse("01a17e00-0000-7000-9000-0000000000ca");
    private static readonly Guid CityId = Guid.Parse("01a17e00-0000-7000-9000-0000000000cb");
    private static readonly DateOnly ReceivingDate = new(2026, 10, 29);
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task First_selection_persists_revision_destination_and_compact_evidence_atomically()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using (var write = CreateContext(target.ConnectionString))
        {
            await write.Database.MigrateAsync();
            await ResetAsync(write);

            var draft = NewDraft();
            var groups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
            draft.AddShippingGroups(groups, Now);
            draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);

            await new StorefrontShippingDraftStore(write)
                .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);
        }

        await using var read = CreateContext(target.ConnectionString);
        var stored = await read.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

        Assert.Equal(1, stored.SelectionRevision);
        Assert.Equal(CountryId, stored.DestinationCountryId);
        Assert.Equal(ProvinceId, stored.DestinationAdministrativeAreaId);
        Assert.Equal(CityId, stored.DestinationCityId);
        Assert.Null(stored.DestinationServiceAreaId);

        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal([LineA], group.Lines.Select(line => line.CartLineId).ToArray());
        var evidence = group.AcceptedEvidence;
        Assert.NotNull(evidence);
        Assert.Equal(Snapshot, evidence!.SnapshotId);
        Assert.Equal(DraftSource, evidence.DraftId);
        Assert.Equal(3, evidence.SourceRevision);
        Assert.Equal(1, evidence.FormatVersion);
        Assert.Equal(1, evidence.SelectionVersion);
        Assert.Equal(ShippingGroupOwnerKind.Store, evidence.Owner.Kind);
        Assert.Equal("store-alpha", evidence.Owner.TenantKey);
        Assert.Equal(AreaGroupA, evidence.AreaGroupId);
        Assert.Equal(RateRowA, evidence.RateRowId);
        Assert.Equal(15_000m, evidence.FreightAmount);
        Assert.Equal(2_000m, evidence.PackagingAmount);
        Assert.Equal(17_000m, evidence.PayableAtCheckoutAmount);
        Assert.Equal(0m, evidence.CarrierCollectAmount);
        Assert.Equal("IRR", evidence.Currency);
        Assert.Equal(ReceivingDate, evidence.ReceivingDate);
        Assert.Equal(new TimeOnly(9, 0).Ticks, evidence.ReceivingOpenTicks);
        Assert.Equal(new TimeOnly(17, 0).Ticks, evidence.ReceivingCloseTicks);
        Assert.Equal("Asia/Tehran", evidence.ReceivingTimeZone);

        // ستون خام مستقل از نگاشت EF همان نسخه را نشان می‌دهد.
        var raw = await read.Database
            .SqlQuery<string>($"SELECT selection_revision::text AS \"Value\" FROM \"order\".cart_shipping_drafts WHERE cart_id = {CartId}")
            .SingleAsync();        Assert.Equal("1", raw);
    }

    [Fact]
    public async Task Zero_expected_revision_on_an_existing_selection_is_a_conflict_and_writes_nothing()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var loaded = await LoadAsync(db);
        var replacement = Groups(AreaGroupB, RateRowB, freight: 90_000m);
        loaded.Replace(
            null,
            7,
            loaded.RecipientName,
            loaded.ContactMobile,
            loaded.ProvinceName,
            loaded.CityName,
            loaded.PostalAddress,
            loaded.PostalCode,
            null,
            "post",
            "پست",
            17_000m,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            null,
            Now.AddMinutes(1));

        var store = new StorefrontShippingDraftStore(db);
        var conflict = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            store.ReplaceCapturedSelectionAsync(loaded, replacement, isNew: false, expectedSelectionRevision: 0, CancellationToken.None));

        Assert.Equal(StorefrontShippingSelectionConflict.Stale, conflict.Code);

        // حالت ذخیره‌شده دست‌نخورده است: همان نسخه، همان گروه، همان شاهد.
        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(1, stored.SelectionRevision);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupA, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RateRowA, group.AcceptedEvidence!.RateRowId);
        Assert.Equal([LineA], group.Lines.Select(line => line.CartLineId).ToArray());
    }

    [Fact]
    public async Task Second_lawful_selection_replaces_every_group_and_increments_the_revision()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var loaded = await LoadAsync(db);
        var replacement = Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineB]);
        loaded.Replace(
            null,
            8,
            loaded.RecipientName,
            loaded.ContactMobile,
            loaded.ProvinceName,
            loaded.CityName,
            loaded.PostalAddress,
            loaded.PostalCode,
            null,
            "post",
            "پست",
            92_000m,
            ReceivingDate,
            ReceivingDate,
            "09:00-12:00",
            null,
            Now.AddMinutes(2));
        loaded.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now.AddMinutes(2));

        await new StorefrontShippingDraftStore(db)
            .ReplaceCapturedSelectionAsync(loaded, replacement, isNew: false, expectedSelectionRevision: 1, CancellationToken.None);

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

        Assert.Equal(2, stored.SelectionRevision);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupB, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RateRowB, group.AcceptedEvidence!.RateRowId);
        Assert.Equal(90_000m, group.AcceptedEvidence!.FreightAmount);
        Assert.Equal(92_000m, group.AcceptedEvidence!.PayableAtCheckoutAmount);
        Assert.Equal([LineB], group.Lines.Select(line => line.CartLineId).ToArray());
        Assert.Equal(92_000m, stored.ShippingAmount);
    }

    [Fact]
    public async Task A_new_selection_with_a_nonzero_expected_revision_is_refused_before_any_insert()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);

        var draft = NewDraft();
        var groups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
        draft.AddShippingGroups(groups, Now);

        var conflict = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            new StorefrontShippingDraftStore(db)
                .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 1, CancellationToken.None));

        Assert.Equal(StorefrontShippingSelectionConflict.Stale, conflict.Code);

        await using var verify = CreateContext(target.ConnectionString);
        Assert.False(await verify.ShippingDrafts.AnyAsync(x => x.CartId == CartId));
    }

    [Fact]
    public async Task A_concurrently_advanced_selection_is_a_conflict_even_with_the_same_expected_revision()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var first = CreateContext(target.ConnectionString);
        await first.Database.MigrateAsync();
        await ResetAsync(first);
        await SeedFirstSelectionAsync(first);

        // دو خواندن مستقل از همان نسخهٔ ۱: دقیقاً همان چیزی که دو تب هم‌زمان می‌بینند.
        await using var second = CreateContext(target.ConnectionString);
        var winner = await LoadAsync(first);
        var loser = await LoadAsync(second);
        Assert.Equal(1, winner.SelectionRevision);
        Assert.Equal(1, loser.SelectionRevision);

        var winnerGroups = Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineB]);
        winner.Replace(null, 8, winner.RecipientName, winner.ContactMobile, winner.ProvinceName, winner.CityName,
            winner.PostalAddress, winner.PostalCode, null, "post", "پست", 92_000m, ReceivingDate, ReceivingDate,
            "09:00-12:00", null, Now.AddMinutes(3));
        await new StorefrontShippingDraftStore(first)
            .ReplaceCapturedSelectionAsync(winner, winnerGroups, isNew: false, expectedSelectionRevision: 1, CancellationToken.None);

        var loserGroups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
        loser.Replace(null, 8, loser.RecipientName, loser.ContactMobile, loser.ProvinceName, loser.CityName,
            loser.PostalAddress, loser.PostalCode, null, "post", "پست", 17_000m, ReceivingDate, ReceivingDate,
            "09:00-12:00", null, Now.AddMinutes(4));

        // همان expectedRevision = ۱، ولی ردیف در این فاصله به ۲ رسیده است: توکن هم‌زمانی EF می‌گیرد.
        var conflict = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            new StorefrontShippingDraftStore(second)
                .ReplaceCapturedSelectionAsync(loser, loserGroups, isNew: false, expectedSelectionRevision: 1, CancellationToken.None));
        Assert.Equal(StorefrontShippingSelectionConflict.Stale, conflict.Code);

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);
        Assert.Equal(2, stored.SelectionRevision);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupB, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal([LineB], group.Lines.Select(line => line.CartLineId).ToArray());
    }

    /// <summary>
    /// شکست <b>بعد از</b> مرحلهٔ حذف و <b>داخل</b> مرحلهٔ درج: تراکنش کامل برمی‌گردد، انتخاب قدیمی سالم
    /// می‌ماند و هیچ گروه تلاش‌شده‌ای در tracking نمی‌ماند تا SaveChanges بعدی آن را جاودانه کند.
    /// </summary>
    [Fact]
    public async Task A_failure_between_the_delete_and_insert_stages_rolls_back_to_the_old_selection()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();

        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();
        await ResetAsync(db);
        await SeedFirstSelectionAsync(db);

        var loaded = await LoadAsync(db);
        Assert.Equal(1, loaded.SelectionRevision);

        // دو گروه با همان روش و همان خط سبد: حذف مرحلهٔ اول انجام می‌شود و درج مرحلهٔ دوم روی
        // ایندکس یکتای خط سبد می‌شکند — یعنی شکست دقیقاً بین دو مرحله، نه پیش از حذف.
        var broken = Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineA])
            .Concat(Groups(AreaGroupB, RateRowB, freight: 90_000m, lineIds: [LineA]))
            .ToArray();

        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            new StorefrontShippingDraftStore(db)
                .ReplaceCapturedSelectionAsync(loaded, broken, isNew: false, expectedSelectionRevision: 1, CancellationToken.None));

        // هیچ گروه/خط اضافه یا حذف‌شده‌ای در tracking نمی‌ماند و نسخهٔ انتخاب به مقدار قبلی برگشته است.
        var trackedGroups = db.ChangeTracker.Entries<CartShippingDraftGroup>()
            .Select(e => $"{e.Entity.ShippingGroupId:N}:{e.State}")
            .ToArray();
        Assert.True(
            trackedGroups.Length == 1 && trackedGroups[0].EndsWith(":Unchanged", StringComparison.Ordinal),
            "tracked groups=" + string.Join(",", trackedGroups));
        Assert.All(
            db.ChangeTracker.Entries<CartShippingDraftGroupLine>(),
            e => Assert.Equal(EntityState.Unchanged, e.State));
        Assert.Equal(1, db.Entry(loaded).CurrentValues.GetValue<long>(nameof(CartShippingDraft.SelectionRevision)));
        Assert.Equal(1, db.Entry(loaded).CurrentValues.GetValue<long>(nameof(CartShippingDraft.SelectionRevision)));

        // SaveChanges بعدی هیچ بازمانده‌ای را نمی‌نویسد.
        await db.SaveChangesAsync();

        await using var verify = CreateContext(target.ConnectionString);
        var stored = await verify.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

        Assert.Equal(1, stored.SelectionRevision);
        var group = Assert.Single(stored.ShippingGroups);
        Assert.Equal(AreaGroupA, group.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RateRowA, group.AcceptedEvidence!.RateRowId);
        Assert.Equal(15_000m, group.AcceptedEvidence!.FreightAmount);
        Assert.Equal([LineA], group.Lines.Select(x => x.CartLineId).ToArray());
    }

    // ---------------------------------------------------------------- helpers
    private static async Task SeedFirstSelectionAsync(OrderDbContext db)
    {
        var draft = NewDraft();
        var groups = Groups(AreaGroupA, RateRowA, freight: 15_000m);
        draft.AddShippingGroups(groups, Now);
        draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);
        await new StorefrontShippingDraftStore(db)
            .ReplaceCapturedSelectionAsync(draft, groups, isNew: true, expectedSelectionRevision: 0, CancellationToken.None);
    }

    private static async Task<CartShippingDraft> LoadAsync(OrderDbContext db) =>
        await db.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

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
        decimal packaging = 2_000m)
    {
        var lines = lineIds ?? [LineA];
        var groups = CartShippingGroupPlanner.Plan(
            CartId,
            lines.Select(line => new CartShippingGroupCartLine(line, SellerPartyId, 1m)).ToArray(),
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
                    SellerPartyId),
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
                ShippingTypeId,
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

    private static async Task ResetAsync(OrderDbContext db)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_group_lines WHERE shipping_group_id IN (SELECT shipping_group_id FROM \"order\".cart_shipping_groups WHERE cart_id = {CartId})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_groups WHERE cart_id = {CartId}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"order\".cart_shipping_drafts WHERE cart_id = {CartId}");
    }

    private static OrderDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }
}
