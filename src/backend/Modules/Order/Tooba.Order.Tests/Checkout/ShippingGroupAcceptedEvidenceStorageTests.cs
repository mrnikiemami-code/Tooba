using Microsoft.EntityFrameworkCore;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1 — اثبات رابطه‌ای شاهد فشرده روی یک پایگاه‌دادهٔ
/// دورانداختنیِ متعلق به همین تسک (127.0.0.1:15433/order_tb231/tb231).
/// </summary>
/// <remarks>
/// همهٔ حقایق با مادی‌ساز واقعی و همان زنجیرهٔ مهاجرت Order اجرا می‌شوند؛ هیچ fixture درون‌حافظه‌ای
/// جانشین ذخیره‌سازی نمی‌شود و هیچ تراکنش بین‌ماژولی ساخته نمی‌شود.
/// </remarks>
public sealed class ShippingGroupAcceptedEvidenceStorageTests
{
    [Fact]
    public async Task Two_groups_on_one_snapshot_persist_distinct_owner_token_money_and_hour_facts()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var checkoutId = Guid.Parse("01a17d0e-1111-7000-9000-000000000001");
        var sellerOrderA = Guid.Parse("01a17d0e-1111-7000-9000-0000000000a1");
        var sellerOrderB = Guid.Parse("01a17d0e-1111-7000-9000-0000000000b2");

        var groups = ShippingGroupAcceptedEvidenceTests.PlannerGroups();
        groups[0].ReplaceAcceptedEvidence(
            ShippingGroupAcceptedEvidenceTests.Evidence(
                ShippingGroupAcceptedEvidenceTests.AreaA,
                ShippingGroupAcceptedEvidenceTests.RowA,
                freight: 120_000m,
                packaging: 5_000m,
                owner: ShippingGroupOwner.Store("Store-Alpha"),
                payable: groups[0].ShippingAmount,
                selection: ShippingGroupAcceptedEvidenceTests.LargeSelectionVersion),
            ShippingGroupAcceptedEvidenceTests.Now);
        groups[1].ReplaceAcceptedEvidence(
            ShippingGroupAcceptedEvidenceTests.Evidence(
                ShippingGroupAcceptedEvidenceTests.AreaB,
                ShippingGroupAcceptedEvidenceTests.RowB,
                freight: 80_000m,
                packaging: 10_000m,
                owner: ShippingGroupOwner.Seller(ShippingGroupAcceptedEvidenceTests.SellerB),
                date: new DateOnly(2026, 10, 31),
                openTicks: ShippingGroupAcceptedEvidenceTests.OpenTicks + 1,
                closeTicks: ShippingGroupAcceptedEvidenceTests.CloseTicks + 1,
                payable: groups[1].ShippingAmount,
                selection: ShippingGroupAcceptedEvidenceTests.LargeSelectionVersion + 1),
            ShippingGroupAcceptedEvidenceTests.Now);

        var orderLineA = ShippingGroupAcceptedEvidenceTests.OrderLineId(1);
        var orderLineB = ShippingGroupAcceptedEvidenceTests.OrderLineId(2);
        var plans = groups.Select(ShippingGroupAcceptedEvidenceTests.Plan).ToArray();

        await using (var write = CreateContext(target.ConnectionString))
        {
            await write.Database.MigrateAsync();
            await ResetAsync(write);
            await InsertOwnersAsync(write, checkoutId, sellerOrderA, sellerOrderB);
            await InsertOrderLineAsync(write, orderLineA, sellerOrderA, ShippingGroupAcceptedEvidenceTests.SellerA);
            await InsertOrderLineAsync(write, orderLineB, sellerOrderB, ShippingGroupAcceptedEvidenceTests.SellerB);

            var materialized = OrderShippingGroupMaterializer.Materialize(
                plans,
                [(orderLineA, ShippingGroupAcceptedEvidenceTests.SellerA),
                 (orderLineB, ShippingGroupAcceptedEvidenceTests.SellerB)],
                new Dictionary<Guid, Guid>
                {
                    [ShippingGroupAcceptedEvidenceTests.SellerA] = sellerOrderA,
                    [ShippingGroupAcceptedEvidenceTests.SellerB] = sellerOrderB,
                },
                new Dictionary<Guid, Guid>
                {
                    [ShippingGroupAcceptedEvidenceTests.CartLineA] = orderLineA,
                    [ShippingGroupAcceptedEvidenceTests.CartLineB] = orderLineB,
                },
                checkoutId,
                ShippingGroupAcceptedEvidenceTests.Now);

            write.AddRange(materialized);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext(target.ConnectionString);
        var stored = await read.OrderShippingGroups
            .Include(x => x.Lines)
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync();

        Assert.Equal(2, stored.Count);
        Assert.All(stored, group => Assert.Equal(ShippingGroupAcceptedEvidenceTests.Snapshot, group.AcceptedEvidence!.SnapshotId));
        Assert.All(stored, group => Assert.Equal("IRR", group.AcceptedEvidence!.Currency));
        Assert.All(stored, group => Assert.Equal("Asia/Tehran", group.AcceptedEvidence!.ReceivingTimeZone));
        Assert.All(stored, group => Assert.Equal(2, group.AcceptedEvidence!.MinimumDeliveryDays));
        Assert.All(stored, group => Assert.Equal(3, group.AcceptedEvidence!.MaximumDeliveryDays));

        // دقت long و ساعت‌های هفت‌رقمی دقیقاً همان مقادیر نوشته‌شده‌اند، نه مقدار کوتاه/گرد‌شده.
        Assert.All(stored, group => Assert.Equal(
            ShippingGroupAcceptedEvidenceTests.LargeSourceRevision, group.AcceptedEvidence!.SourceRevision));

        var first = stored.Single(x => x.SellerPartyId == ShippingGroupAcceptedEvidenceTests.SellerA);
        var second = stored.Single(x => x.SellerPartyId == ShippingGroupAcceptedEvidenceTests.SellerB);

        Assert.Equal(ShippingGroupOwnerKind.Store, first.AcceptedEvidence!.Owner.Kind);
        Assert.Equal("Store-Alpha", first.AcceptedEvidence!.Owner.TenantKey);
        Assert.Null(first.AcceptedEvidence!.Owner.SellerPartyId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.AreaA, first.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.RowA, first.AcceptedEvidence!.RateRowId);
        Assert.Equal(120_000m, first.AcceptedEvidence!.FreightAmount);
        Assert.Equal(5_000m, first.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(125_000m, first.AcceptedEvidence!.PayableAtCheckoutAmount);
        Assert.Equal(0m, first.AcceptedEvidence!.CarrierCollectAmount);
        Assert.Equal(new DateOnly(2026, 10, 29), first.AcceptedEvidence!.ReceivingDate);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.OpenTicks, first.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.CloseTicks, first.AcceptedEvidence!.ReceivingCloseTicks);
        Assert.Equal(new TimeOnly(ShippingGroupAcceptedEvidenceTests.OpenTicks).Hour, new TimeOnly(first.AcceptedEvidence!.ReceivingOpenTicks).Hour);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.LargeSelectionVersion, first.AcceptedEvidence!.SelectionVersion);

        Assert.Equal(ShippingGroupOwnerKind.Seller, second.AcceptedEvidence!.Owner.Kind);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.SellerB, second.AcceptedEvidence!.Owner.SellerPartyId);
        Assert.Null(second.AcceptedEvidence!.Owner.TenantKey);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.AreaB, second.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.RowB, second.AcceptedEvidence!.RateRowId);
        Assert.Equal(80_000m, second.AcceptedEvidence!.FreightAmount);
        Assert.Equal(10_000m, second.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(new DateOnly(2026, 10, 31), second.AcceptedEvidence!.ReceivingDate);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.OpenTicks + 1, second.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.LargeSelectionVersion + 1, second.AcceptedEvidence!.SelectionVersion);

        // مقادیر خام ستون‌ها (مستقل از نگاشت EF) همان دقت را نشان می‌دهند.
        var rawTokens = await read.Database
            .SqlQuery<string>($"SELECT accepted_source_revision::text || '/' || accepted_selection_version::text || '/' || accepted_receiving_open_ticks::text AS \"Value\" FROM \"order\".order_shipping_groups WHERE shipping_group_id = {first.ShippingGroupId}")
            .SingleAsync();
        Assert.Equal(
            $"{ShippingGroupAcceptedEvidenceTests.LargeSourceRevision}/{ShippingGroupAcceptedEvidenceTests.LargeSelectionVersion}/{ShippingGroupAcceptedEvidenceTests.OpenTicks}",
            rawTokens);

        // هویت گروه/عضویت با افزودن شاهد عوض نشده است.
        Assert.Equal(groups[0].ShippingGroupId, first.ShippingGroupId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.OrderLineId(1), first.Lines.Single().OrderLineId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.OrderLineId(2), second.Lines.Single().OrderLineId);
    }

    [Fact]
    public async Task Draft_replacement_with_a_coherent_new_quote_never_rewrites_the_stored_order_facts()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var checkoutId = Guid.Parse("01a17d0e-2222-7000-9000-000000000001");
        var sellerOrderA = Guid.Parse("01a17d0e-2222-7000-9000-0000000000a1");
        var orderLineA = ShippingGroupAcceptedEvidenceTests.OrderLineId(1);

        var groups = ShippingGroupAcceptedEvidenceTests.PlannerGroups()
            .Where(group => group.SellerPartyId == ShippingGroupAcceptedEvidenceTests.SellerA)
            .ToArray();
        var draft = CartShippingDraft.Create(
            ShippingGroupAcceptedEvidenceTests.Cart,
            "hash",
            1,
            "گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "نشانی",
            "1234567890",
            null,
            groups[0].MethodCode,
            groups[0].MethodLabel,
            groups[0].ShippingAmount,
            groups[0].MinimumDeliveryDate,
            groups[0].SelectedDeliveryDate,
            groups[0].SelectedDeliveryTimeWindow,
            null,
            ShippingGroupAcceptedEvidenceTests.Now);

        var accepted = ShippingGroupAcceptedEvidenceTests.Evidence(
            ShippingGroupAcceptedEvidenceTests.AreaA,
            ShippingGroupAcceptedEvidenceTests.RowA,
            freight: 120_000m,
            packaging: 5_000m,
            payable: groups[0].ShippingAmount);
        groups[0].ReplaceAcceptedEvidence(accepted, ShippingGroupAcceptedEvidenceTests.Now);
        draft.AddShippingGroups(groups, ShippingGroupAcceptedEvidenceTests.Now);

        await using (var write = CreateContext(target.ConnectionString))
        {
            await write.Database.MigrateAsync();
            await ResetAsync(write);
            await InsertOwnersAsync(write, checkoutId, sellerOrderA, Guid.Empty);
            await InsertOrderLineAsync(write, orderLineA, sellerOrderA, ShippingGroupAcceptedEvidenceTests.SellerA);

            var materialized = OrderShippingGroupMaterializer.Materialize(
                [ShippingGroupAcceptedEvidenceTests.Plan(groups[0])],
                [(orderLineA, ShippingGroupAcceptedEvidenceTests.SellerA)],
                new Dictionary<Guid, Guid> { [ShippingGroupAcceptedEvidenceTests.SellerA] = sellerOrderA },
                new Dictionary<Guid, Guid> { [ShippingGroupAcceptedEvidenceTests.CartLineA] = orderLineA },
                checkoutId,
                ShippingGroupAcceptedEvidenceTests.Now);

            write.AddRange(materialized);
            write.Add(draft);
            await write.SaveChangesAsync();
        }

        // جایگزینی صریح گروه پیش‌نویس با نقل‌قول تازه و <b>منسجم</b>: مبلغ گروه 157,000 و
        // کرایه 150,000 + بسته‌بندی 7,000 = 157,000 قابل‌پرداخت هنگام تسویه.
        var freshGroups = ShippingGroupAcceptedEvidenceTests.PlannerGroups(standardAmount: 157_000m)
            .Where(group => group.SellerPartyId == ShippingGroupAcceptedEvidenceTests.SellerA)
            .ToArray();
        var replacement = ShippingGroupAcceptedEvidenceTests.Evidence(
            ShippingGroupAcceptedEvidenceTests.AreaB,
            ShippingGroupAcceptedEvidenceTests.RowB,
            freight: 150_000m,
            packaging: 7_000m,
            snapshot: Guid.Parse("01a17d0e-3333-7000-9000-000000000001"),
            payable: freshGroups[0].ShippingAmount,
            selection: 9);
        freshGroups[0].ReplaceAcceptedEvidence(
            replacement, ShippingGroupAcceptedEvidenceTests.Now.AddHours(1));

        await using (var rewrite = CreateContext(target.ConnectionString))
        {
            var tracked = await rewrite.ShippingDrafts
                .Include(x => x.ShippingGroups)
                .SingleAsync(x => x.CartId == ShippingGroupAcceptedEvidenceTests.Cart);

            // مسیر صریح و پشتیبانی‌شدهٔ جایگزینی گروه: ابتدا حذف قطعی، سپس افزودن گروه تازه در یک ذخیرهٔ جداگانه.
            tracked.ClearShippingGroups(ShippingGroupAcceptedEvidenceTests.Now.AddHours(1));
            await rewrite.SaveChangesAsync();

            tracked.AddShippingGroups(freshGroups, ShippingGroupAcceptedEvidenceTests.Now.AddHours(1));
            await rewrite.SaveChangesAsync();
        }

        await using var read = CreateContext(target.ConnectionString);
        var storedOrder = await read.OrderShippingGroups
            .SingleAsync(x => x.CheckoutId == checkoutId);
        var storedDraft = await read.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == ShippingGroupAcceptedEvidenceTests.Cart);

        // سفارشِ ثبت‌شده همان snapshot/مالک/مبلغ/تاریخ/ساعت/نسخهٔ قدیمی را نگه می‌دارد.
        Assert.Equal(accepted.SnapshotId, storedOrder.AcceptedEvidence!.SnapshotId);
        Assert.Equal(ShippingGroupOwnerKind.Store, storedOrder.AcceptedEvidence!.Owner.Kind);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.TenantKey, storedOrder.AcceptedEvidence!.Owner.TenantKey);
        Assert.Equal(120_000m, storedOrder.AcceptedEvidence!.FreightAmount);
        Assert.Equal(5_000m, storedOrder.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(125_000m, storedOrder.AcceptedEvidence!.PayableAtCheckoutAmount);
        Assert.Equal(new DateOnly(2026, 10, 29), storedOrder.AcceptedEvidence!.ReceivingDate);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.OpenTicks, storedOrder.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.LargeSourceRevision, storedOrder.AcceptedEvidence!.SourceRevision);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.LargeSelectionVersion, storedOrder.AcceptedEvidence!.SelectionVersion);

        // پیش‌نویس شاهد تازهٔ منسجم را دارد؛ این فقط پیش‌نویس است، نه تاریخچه.
        var draftGroup = storedDraft.ShippingGroups.Single();
        Assert.Equal(157_000m, draftGroup.ShippingAmount);
        Assert.Equal(replacement.SnapshotId, draftGroup.AcceptedEvidence!.SnapshotId);
        Assert.Equal(150_000m, draftGroup.AcceptedEvidence!.FreightAmount);
        Assert.Equal(7_000m, draftGroup.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(9, draftGroup.AcceptedEvidence!.SelectionVersion);
    }

    [Fact]
    public async Task Legacy_groups_store_null_evidence_and_keep_working()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var checkoutId = Guid.Parse("01a17d0e-4444-7000-9000-000000000001");
        var sellerOrderA = Guid.Parse("01a17d0e-4444-7000-9000-0000000000a1");
        var orderLineA = ShippingGroupAcceptedEvidenceTests.OrderLineId(1);

        var groups = ShippingGroupAcceptedEvidenceTests.PlannerGroups()
            .Where(group => group.SellerPartyId == ShippingGroupAcceptedEvidenceTests.SellerA)
            .ToArray();

        await using (var write = CreateContext(target.ConnectionString))
        {
            await write.Database.MigrateAsync();
            await ResetAsync(write);
            await InsertOwnersAsync(write, checkoutId, sellerOrderA, Guid.Empty);
            await InsertOrderLineAsync(write, orderLineA, sellerOrderA, ShippingGroupAcceptedEvidenceTests.SellerA);

            var materialized = OrderShippingGroupMaterializer.Materialize(
                [ShippingGroupAcceptedEvidenceTests.Plan(groups[0])],
                [(orderLineA, ShippingGroupAcceptedEvidenceTests.SellerA)],
                new Dictionary<Guid, Guid> { [ShippingGroupAcceptedEvidenceTests.SellerA] = sellerOrderA },
                new Dictionary<Guid, Guid> { [ShippingGroupAcceptedEvidenceTests.CartLineA] = orderLineA },
                checkoutId,
                ShippingGroupAcceptedEvidenceTests.Now);

            write.AddRange(materialized);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext(target.ConnectionString);
        var stored = await read.OrderShippingGroups.SingleAsync(x => x.CheckoutId == checkoutId);

        Assert.Null(stored.AcceptedEvidence);
        Assert.Equal(125_000m, stored.ShippingChargeSnapshot);
    }

    [Fact]
    public async Task Persisted_evidence_is_all_or_none_and_shape_checked_by_the_database()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        var checkoutId = Guid.Parse("01a17d0e-5555-7000-9000-000000000001");
        var sellerOrderA = Guid.Parse("01a17d0e-5555-7000-9000-0000000000a1");
        var orderLineA = ShippingGroupAcceptedEvidenceTests.OrderLineId(1);

        var groups = ShippingGroupAcceptedEvidenceTests.PlannerGroups()
            .Where(group => group.SellerPartyId == ShippingGroupAcceptedEvidenceTests.SellerA)
            .ToArray();
        groups[0].ReplaceAcceptedEvidence(
            ShippingGroupAcceptedEvidenceTests.Evidence(
                ShippingGroupAcceptedEvidenceTests.AreaA,
                ShippingGroupAcceptedEvidenceTests.RowA,
                freight: 120_000m,
                packaging: 5_000m,
                payable: groups[0].ShippingAmount),
            ShippingGroupAcceptedEvidenceTests.Now);

        Guid groupId;
        await using (var write = CreateContext(target.ConnectionString))
        {
            await write.Database.MigrateAsync();
            await ResetAsync(write);
            await InsertOwnersAsync(write, checkoutId, sellerOrderA, Guid.Empty);
            await InsertOrderLineAsync(write, orderLineA, sellerOrderA, ShippingGroupAcceptedEvidenceTests.SellerA);

            var materialized = OrderShippingGroupMaterializer.Materialize(
                [ShippingGroupAcceptedEvidenceTests.Plan(groups[0])],
                [(orderLineA, ShippingGroupAcceptedEvidenceTests.SellerA)],
                new Dictionary<Guid, Guid> { [ShippingGroupAcceptedEvidenceTests.SellerA] = sellerOrderA },
                new Dictionary<Guid, Guid> { [ShippingGroupAcceptedEvidenceTests.CartLineA] = orderLineA },
                checkoutId,
                ShippingGroupAcceptedEvidenceTests.Now);

            write.AddRange(materialized);
            await write.SaveChangesAsync();
            groupId = materialized[0].ShippingGroupId;
        }

        await using var db = CreateContext(target.ConnectionString);

        // هیچ ردیف نیمه‌کامل/ناسازگاری در پایگاه‌داده نمی‌ماند؛ CHECKها مستقل از سازندهٔ دامنه.
        await AssertCheckViolationAsync(db, () => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"order\".order_shipping_groups SET accepted_receiving_time_zone = NULL WHERE shipping_group_id = {groupId}"));
        await AssertCheckViolationAsync(db, () => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"order\".order_shipping_groups SET accepted_owner_kind = NULL WHERE shipping_group_id = {groupId}"));
        await AssertCheckViolationAsync(db, () => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"order\".order_shipping_groups SET accepted_owner_kind = 2, accepted_owner_tenant_key = 'store-alpha' WHERE shipping_group_id = {groupId}"));
        await AssertCheckViolationAsync(db, () => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"order\".order_shipping_groups SET accepted_owner_kind = 1, accepted_owner_seller_party_id = {ShippingGroupAcceptedEvidenceTests.SellerA} WHERE shipping_group_id = {groupId}"));
        await AssertCheckViolationAsync(db, () => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"order\".order_shipping_groups SET accepted_freight_amount = 150000.00 WHERE shipping_group_id = {groupId}"));
        await AssertCheckViolationAsync(db, () => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"order\".order_shipping_groups SET accepted_receiving_close_ticks = 324001234567 WHERE shipping_group_id = {groupId}"));
        await AssertCheckViolationAsync(db, () => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"order\".order_shipping_groups SET accepted_format_version = 2 WHERE shipping_group_id = {groupId}"));
        await AssertCheckViolationAsync(db, () => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"order\".order_shipping_groups SET accepted_owner_seller_party_id = {ShippingGroupAcceptedEvidenceTests.SellerA} WHERE shipping_group_id = {groupId}"));

        // ردیف دست‌نخورده باقی مانده است.
        var intact = await db.OrderShippingGroups.SingleAsync(x => x.ShippingGroupId == groupId);
        Assert.NotNull(intact.AcceptedEvidence);
        Assert.Equal(120_000m, intact.AcceptedEvidence!.FreightAmount);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.OpenTicks, intact.AcceptedEvidence!.ReceivingOpenTicks);

        // شاهد کامل هم می‌تواند صریحاً پاک شود (همهٔ ستون‌ها با هم)؛ گروه قدیمی معتبر می‌ماند.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "order".order_shipping_groups
                SET accepted_snapshot_id = NULL,
                    accepted_draft_id = NULL,
                    accepted_source_revision = NULL,
                    accepted_format_version = NULL,
                    accepted_selection_version = NULL,
                    accepted_owner_kind = NULL,
                    accepted_owner_tenant_key = NULL,
                    accepted_owner_seller_party_id = NULL,
                    accepted_area_group_id = NULL,
                    accepted_rate_row_id = NULL,
                    accepted_shipping_type_id = NULL,
                    accepted_freight_amount = NULL,
                    accepted_packaging_amount = NULL,
                    accepted_currency = NULL,
                    accepted_payable_at_checkout_amount = NULL,
                    accepted_carrier_collect_amount = NULL,
                    accepted_receiving_date = NULL,
                    accepted_receiving_open_ticks = NULL,
                    accepted_receiving_close_ticks = NULL,
                    accepted_receiving_time_zone = NULL,
                    accepted_minimum_delivery_days = NULL,
                    accepted_maximum_delivery_days = NULL
              WHERE shipping_group_id = {groupId}
             """);

        db.ChangeTracker.Clear();
        var cleared = await db.OrderShippingGroups.SingleAsync(x => x.ShippingGroupId == groupId);
        Assert.Null(cleared.AcceptedEvidence);
        Assert.Equal(125_000m, cleared.ShippingChargeSnapshot);
    }

    [Fact]
    public async Task Evidence_storage_is_compact_scalars_with_lossless_types_on_both_tables()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();

        foreach (var (table, clrType) in new[]
                 {
                     ("cart_shipping_groups", typeof(CartShippingDraftGroup)),
                     ("order_shipping_groups", typeof(OrderShippingGroup)),
                 })
        {
            // مدل زمان اجرا در برابر ستون‌های واقعی همان جدول: همان مجموعه، نه کمتر و نه بیشتر.
            var modelledColumns = ModelEvidenceColumns(db, clrType);
            Assert.Equal(22, modelledColumns.Count);
            var evidenceColumns = await db.Database
                .SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema = 'order' AND table_name = {table} AND column_name LIKE 'accepted\\_%' ORDER BY column_name")
                .ToListAsync();
            Assert.Equal(22, evidenceColumns.Count);
            Assert.Equal(modelledColumns.OrderBy(x => x, StringComparer.Ordinal), evidenceColumns);

            var structured = await db.Database
                .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema = 'order' AND table_name = {table} AND data_type IN ('json', 'jsonb')")
                .SingleAsync();
            Assert.Equal(0, structured);

            var nullable = await db.Database
                .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema = 'order' AND table_name = {table} AND column_name LIKE 'accepted\\_%' AND is_nullable = 'NO'")
                .SingleAsync();
            Assert.Equal(0, nullable);

            // دقت: بازبینی/نسخه/تیک ساعت باید bigint باشند، نه integer/time/timestamp.
            var bigintColumns = await db.Database
                .SqlQuery<string>($"SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_schema = 'order' AND table_name = {table} AND column_name IN ('accepted_source_revision', 'accepted_selection_version', 'accepted_receiving_open_ticks', 'accepted_receiving_close_ticks') AND data_type = 'bigint'")
                .ToListAsync();
            Assert.Equal(4, bigintColumns.Count);

            var moneyColumns = await db.Database
                .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema = 'order' AND table_name = {table} AND column_name IN ('accepted_freight_amount', 'accepted_packaging_amount', 'accepted_payable_at_checkout_amount', 'accepted_carrier_collect_amount') AND data_type = 'numeric' AND numeric_precision = 18 AND numeric_scale = 2")
                .SingleAsync();
            Assert.Equal(4, moneyColumns);

            var constraints = await db.Database
                .SqlQuery<string>($"SELECT conname::text AS \"Value\" FROM pg_constraint WHERE conrelid = to_regclass('order.' || {table}) AND contype = 'c' ORDER BY conname")
                .ToListAsync();
            Assert.Contains($"ck_{table}_accepted_evidence_complete", constraints);
            Assert.Contains($"ck_{table}_accepted_evidence_shape", constraints);
        }

        var snapshotIndex = await db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname = 'order' AND tablename = 'order_shipping_groups' AND indexname = 'ix_order_shipping_groups_accepted_snapshot_id'")
            .SingleAsync();
        Assert.Equal(1, snapshotIndex);
    }

    private static IReadOnlyList<string> ModelEvidenceColumns(OrderDbContext db, Type clrType)
    {
        var entityType = db.Model.FindEntityType(clrType)
            ?? throw new InvalidOperationException($"{clrType.Name} is not part of the Order model");
        var evidence = entityType.FindNavigation(nameof(OrderShippingGroup.AcceptedEvidence))?.TargetEntityType
            ?? throw new InvalidOperationException($"{clrType.Name} has no owned accepted-evidence type");

        var columns = new List<string>();
        foreach (var property in evidence.GetProperties().Where(x => !x.IsShadowProperty()))
        {
            columns.Add(property.GetColumnName());
        }

        var owner = evidence.FindNavigation(nameof(ShippingGroupAcceptedEvidence.Owner))?.TargetEntityType;
        if (owner is not null)
        {
            foreach (var property in owner.GetProperties().Where(x => !x.IsShadowProperty()))
            {
                columns.Add(property.GetColumnName());
            }
        }

        return columns;
    }

    private static async Task AssertCheckViolationAsync(OrderDbContext db, Func<Task> action)
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(action);
        Assert.True(
            FindSqlState(error) == "23514",
            $"expected a CHECK violation (23514) but got: {error.GetType().Name}: {error.Message}");
    }

    private static string? FindSqlState(Exception? error)
    {
        for (var current = error; current is not null; current = current.InnerException)
        {
            if (current.GetType().GetProperty("SqlState")?.GetValue(current) is string state)
            {
                return state;
            }
        }

        return null;
    }

    private static OrderDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }

    private static async Task ResetAsync(OrderDbContext db)
    {
        foreach (var statement in new[]
        {
            "DELETE FROM \"order\".cart_shipping_group_lines",
            "DELETE FROM \"order\".cart_shipping_groups",
            "DELETE FROM \"order\".cart_shipping_drafts",
            "DELETE FROM \"order\".order_shipping_group_lines",
            "DELETE FROM \"order\".order_shipping_groups",
            "DELETE FROM \"order\".order_lines",
            "DELETE FROM \"order\".seller_orders",
            "DELETE FROM \"order\".checkouts",
        })
        {
            await db.Database.ExecuteSqlRawAsync(statement);
        }
    }

    private static async Task InsertOwnersAsync(
        OrderDbContext db,
        Guid checkoutId,
        Guid sellerOrderA,
        Guid sellerOrderB)
    {
        var now = ShippingGroupAcceptedEvidenceTests.Now;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".checkouts
                 (checkout_id, idempotency_key, cart_id, mode, buyer_party_id, placed_by_user_id, market, currency, channel, created_at, submitted_at)
             VALUES
                 ({checkoutId}, {"tb231"}, {ShippingGroupAcceptedEvidenceTests.Cart}, {"SingleStore"}, {Guid.NewGuid()},
                  {Guid.NewGuid()}, {"IR"}, {"IRR"}, {"Web"}, {now}, {now})
             """);

        await InsertSellerOrderAsync(
            db, sellerOrderA, checkoutId, "TB231-A", ShippingGroupAcceptedEvidenceTests.SellerA);
        if (sellerOrderB != Guid.Empty)
        {
            await InsertSellerOrderAsync(
                db, sellerOrderB, checkoutId, "TB231-B", ShippingGroupAcceptedEvidenceTests.SellerB);
        }
    }

    private static async Task InsertSellerOrderAsync(
        OrderDbContext db,
        Guid sellerOrderId,
        Guid checkoutId,
        string orderNumber,
        Guid sellerPartyId)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".seller_orders
                 (seller_order_id, checkout_id, order_number, seller_party_id, status, subtotal_snapshot, tax_snapshot, discount_snapshot, grand_total_snapshot, currency)
             VALUES
                 ({sellerOrderId}, {checkoutId}, {orderNumber}, {sellerPartyId}, {"Placed"}, {0m}, {0m}, {0m}, {0m}, {"IRR"})
             """);
    }

    private static async Task InsertOrderLineAsync(
        OrderDbContext db,
        Guid orderLineId,
        Guid sellerOrderId,
        Guid sellerPartyId)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".order_lines
                 (line_id, seller_order_id, offer_id, catalog_variant_id, seller_party_id, quantity, unit_price_snapshot, line_total_snapshot, currency, tax_exclusive, price_id)
             VALUES
                 ({orderLineId}, {sellerOrderId}, {Guid.NewGuid()}, {Guid.NewGuid()}, {sellerPartyId}, {1m}, {100_000m}, {100_000m}, {"IRR"}, {true}, {Guid.NewGuid()})
             """);
    }
}
