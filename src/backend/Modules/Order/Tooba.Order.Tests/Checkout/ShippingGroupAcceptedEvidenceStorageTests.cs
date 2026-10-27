using Microsoft.EntityFrameworkCore;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231 — اثبات رابطه‌ای شاهد فشرده روی یک پایگاه‌دادهٔ
/// دورانداختنیِ متعلق به همین تسک (127.0.0.1:15433/order_tb231/tb231).
/// </summary>
/// <remarks>
/// همهٔ حقایق با مادی‌ساز واقعی و همان زنجیرهٔ مهاجرت Order اجرا می‌شوند؛ هیچ fixture درون‌حافظه‌ای
/// جانشین ذخیره‌سازی نمی‌شود و هیچ تراکنش بین‌ماژولی ساخته نمی‌شود.
/// </remarks>
public sealed class ShippingGroupAcceptedEvidenceStorageTests
{
    [Fact]
    public async Task Two_groups_on_one_snapshot_persist_distinct_facts_with_unchanged_membership()
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
                payable: groups[0].ShippingAmount),
            ShippingGroupAcceptedEvidenceTests.Now);
        groups[1].ReplaceAcceptedEvidence(
            ShippingGroupAcceptedEvidenceTests.Evidence(
                ShippingGroupAcceptedEvidenceTests.AreaB,
                ShippingGroupAcceptedEvidenceTests.RowB,
                freight: 80_000m,
                packaging: 10_000m,
                payable: groups[1].ShippingAmount,
                start: new DateOnly(2026, 10, 31),
                end: new DateOnly(2026, 11, 7),
                selection: 4),
            ShippingGroupAcceptedEvidenceTests.Now);

        await using (var write = CreateContext(target.ConnectionString))
        {
            await write.Database.MigrateAsync();
            await ResetAsync(write);
            await InsertOwnersAsync(write, checkoutId, sellerOrderA, sellerOrderB);

            var orderLineA = ShippingGroupAcceptedEvidenceTests.OrderLineId(1);
            var orderLineB = ShippingGroupAcceptedEvidenceTests.OrderLineId(2);
            await InsertOrderLineAsync(write, orderLineA, sellerOrderA, ShippingGroupAcceptedEvidenceTests.SellerA);
            await InsertOrderLineAsync(write, orderLineB, sellerOrderB, ShippingGroupAcceptedEvidenceTests.SellerB);

            var plans = groups.Select(ShippingGroupAcceptedEvidenceTests.Plan).ToArray();
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

        var first = stored.Single(x => x.SellerPartyId == ShippingGroupAcceptedEvidenceTests.SellerA);
        var second = stored.Single(x => x.SellerPartyId == ShippingGroupAcceptedEvidenceTests.SellerB);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.AreaA, first.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.RowA, first.AcceptedEvidence!.RateRowId);
        Assert.Equal(120_000m, first.AcceptedEvidence!.FreightAmount);
        Assert.Equal(5_000m, first.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(125_000m, first.AcceptedEvidence!.PayableAtCheckoutAmount);
        Assert.Equal(new DateOnly(2026, 10, 29), first.AcceptedEvidence!.ReceivingStartDate);
        Assert.Equal(3, first.AcceptedEvidence!.SelectionVersion);

        Assert.Equal(ShippingGroupAcceptedEvidenceTests.AreaB, second.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.RowB, second.AcceptedEvidence!.RateRowId);
        Assert.Equal(80_000m, second.AcceptedEvidence!.FreightAmount);
        Assert.Equal(10_000m, second.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(new DateOnly(2026, 11, 7), second.AcceptedEvidence!.ReceivingEndDate);
        Assert.Equal(4, second.AcceptedEvidence!.SelectionVersion);

        // هویت گروه/عضویت با افزودن شاهد عوض نشده است.
        Assert.Equal(groups[0].ShippingGroupId, first.ShippingGroupId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.OrderLineId(1), first.Lines.Single().OrderLineId);
        Assert.Equal(ShippingGroupAcceptedEvidenceTests.OrderLineId(2), second.Lines.Single().OrderLineId);
    }

    [Fact]
    public async Task Draft_replacement_never_rewrites_the_stored_order_group_facts()
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

        // جایگزینی پیش‌نویس با snapshot/مبلغ تازه: فقط شاهد همان پیش‌نویس عوض می‌شود.
        var replacement = ShippingGroupAcceptedEvidenceTests.Evidence(
            ShippingGroupAcceptedEvidenceTests.AreaB,
            ShippingGroupAcceptedEvidenceTests.RowB,
            freight: 150_000m,
            packaging: 7_000m,
            snapshot: Guid.Parse("01a17d0e-3333-7000-9000-000000000001"),
            payable: groups[0].ShippingAmount,
            selection: 9);

        await using (var rewrite = CreateContext(target.ConnectionString))
        {
            var tracked = await rewrite.ShippingDrafts
                .Include(x => x.ShippingGroups)
                .SingleAsync(x => x.CartId == ShippingGroupAcceptedEvidenceTests.Cart);
            tracked.ShippingGroups.Single().ReplaceAcceptedEvidence(
                replacement,
                ShippingGroupAcceptedEvidenceTests.Now.AddHours(1));
            await rewrite.SaveChangesAsync();
        }

        await using var read = CreateContext(target.ConnectionString);
        var storedOrder = await read.OrderShippingGroups
            .SingleAsync(x => x.CheckoutId == checkoutId);
        var storedDraft = await read.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .SingleAsync(x => x.CartId == ShippingGroupAcceptedEvidenceTests.Cart);

        // سفارشِ ثبت‌شده همان snapshot/مبلغ/تاریخ قدیمی را نگه می‌دارد.
        Assert.Equal(accepted.SnapshotId, storedOrder.AcceptedEvidence!.SnapshotId);
        Assert.Equal(120_000m, storedOrder.AcceptedEvidence!.FreightAmount);
        Assert.Equal(5_000m, storedOrder.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(new DateOnly(2026, 10, 29), storedOrder.AcceptedEvidence!.ReceivingStartDate);
        Assert.Equal(3, storedOrder.AcceptedEvidence!.SelectionVersion);

        // پیش‌نویس شاهد تازه را دارد؛ این فقط پیش‌نویس است، نه تاریخچه.
        Assert.Equal(replacement.SnapshotId, storedDraft.ShippingGroups.Single().AcceptedEvidence!.SnapshotId);
        Assert.Equal(150_000m, storedDraft.ShippingGroups.Single().AcceptedEvidence!.FreightAmount);
        Assert.Equal(9, storedDraft.ShippingGroups.Single().AcceptedEvidence!.SelectionVersion);
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
    public async Task Evidence_storage_is_compact_scalars_not_authored_json()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = CreateContext(target.ConnectionString);
        await db.Database.MigrateAsync();

        foreach (var table in new[] { "cart_shipping_groups", "order_shipping_groups" })
        {
            var evidenceColumns = await db.Database
                .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema = 'order' AND table_name = {table} AND column_name LIKE 'accepted\\_%'")
                .SingleAsync();
            Assert.Equal(19, evidenceColumns);

            var structured = await db.Database
                .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema = 'order' AND table_name = {table} AND data_type IN ('json', 'jsonb')")
                .SingleAsync();
            Assert.Equal(0, structured);

            var nullable = await db.Database
                .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema = 'order' AND table_name = {table} AND column_name LIKE 'accepted\\_%' AND is_nullable = 'NO'")
                .SingleAsync();
            Assert.Equal(0, nullable);
        }

        var snapshotIndex = await db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname = 'order' AND tablename = 'order_shipping_groups' AND indexname = 'ix_order_shipping_groups_accepted_snapshot_id'")
            .SingleAsync();
        Assert.Equal(1, snapshotIndex);
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

