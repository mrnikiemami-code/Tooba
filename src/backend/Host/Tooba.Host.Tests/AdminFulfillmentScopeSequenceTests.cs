using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Admin;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T010: lifecycle sequence, exact selection, bulk homogeneity.</summary>
public sealed class AdminFulfillmentScopeSequenceTests
{
    [Fact]
    public void ReadyToProcess_cannot_pack_before_start_processing()
    {
        var line = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady(line, 2, now);
        var ex = Assert.Throws<InvalidOperationException>(() => unit.PackSelections([(line, 1)], now));
        Assert.Equal("fulfillment.pack.requires_processing", ex.Message);
        Assert.Equal(0, unit.Items.Single().QuantityPacked);
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, unit.Status);
    }

    [Fact]
    public void Process_one_line_does_not_process_siblings()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady([(l1, 1), (l2, 2)], now);
        unit.ProcessSelections([(l1, 1)], now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityProcessing);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l2).QuantityProcessing);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
        var packOther = Assert.Throws<InvalidOperationException>(() => unit.PackSelections([(l2, 1)], now));
        Assert.Equal("fulfillment.pack.requires_processing", packOther.Message);
        unit.UnprocessSelections([(l1, 1)], now);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l1).QuantityProcessing);
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, unit.Status);
    }

    [Fact]
    public void StartProcessing_then_pack_is_allowed()
    {
        var line = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady(line, 2, now);
        unit.MarkProcessing(now);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
        unit.PackSelections([(line, 2)], now);
        Assert.Equal(2, unit.Items.Single().QuantityPacked);
        Assert.Equal(FulfillmentStatus.Packed, unit.Status);
    }

    [Fact]
    public void Process_selected_changes_only_selected_line_and_can_unprocess()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var l3 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady([(l1, 1), (l2, 2), (l3, 3)], now);
        unit.ProcessSelections([(l1, 1)], now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityProcessing);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l2).QuantityProcessing);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l3).QuantityProcessing);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
        var packOther = Assert.Throws<InvalidOperationException>(() => unit.PackSelections([(l2, 1)], now));
        Assert.Equal("fulfillment.pack.requires_processing", packOther.Message);
        unit.PackSelections([(l1, 1)], now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityPacked);
        var blocked = Assert.Throws<InvalidOperationException>(() => unit.UnprocessSelections([(l1, 1)], now));
        Assert.Contains("بسته‌بندی", blocked.Message, StringComparison.Ordinal);
        unit.UnpackSelections([(l1, 1)], now);
        unit.UnprocessSelections([(l1, 1)], now);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l1).QuantityProcessing);
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, unit.Status);
    }

    [Fact]
    public void Pack_selected_changes_only_selected_line()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var l3 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady([(l1, 1), (l2, 2), (l3, 3)], now);
        unit.MarkProcessing(now);
        unit.PackSelections([(l1, 1)], now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityPacked);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l2).QuantityPacked);
        Assert.Equal(0, unit.Items.Single(x => x.OrderLineId == l3).QuantityPacked);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
    }

    [Fact]
    public void Partial_quantity_packs_exact_amount()
    {
        var line = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady(line, 3, now);
        unit.MarkProcessing(now);
        unit.PackSelections([(line, 1)], now);
        Assert.Equal(1, unit.Items.Single().QuantityPacked);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
    }

    [Fact]
    public void Pack_all_packs_every_eligible_line()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady([(l1, 1), (l2, 2)], now);
        unit.MarkProcessing(now);
        unit.MarkPacked(now);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == l1).QuantityPacked);
        Assert.Equal(2, unit.Items.Single(x => x.OrderLineId == l2).QuantityPacked);
        Assert.Equal(FulfillmentStatus.Packed, unit.Status);
    }

    [Fact]
    public void Oversized_quantity_rejected()
    {
        var line = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateReady(line, 2, now);
        unit.MarkProcessing(now);
        var ex = Assert.Throws<InvalidOperationException>(() => unit.PackSelections([(line, 3)], now));
        Assert.Equal(0, unit.Items.Single().QuantityPacked);
        Assert.Contains("بیشتر", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Homogeneous_packable_selection_accepted()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var snapshot = Snapshot(
            FulfillmentStatus.Processing,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), l1, 1, 0, null, 0, 1),
             new FulfillmentItemSnapshot(Guid.NewGuid(), l2, 2, 0, null, 0, 2)]);
        Assert.True(AdminOrderOperationsComposer.SelectionsAreHomogeneousPackable(
            snapshot,
            [new AdminOrderLineSelection(l1, 1), new AdminOrderLineSelection(l2, 1)]));
    }

    [Fact]
    public void Mixed_packable_and_already_packed_is_incompatible()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var snapshot = Snapshot(
            FulfillmentStatus.Processing,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), l1, 1, 0, null, 1, 1),
             new FulfillmentItemSnapshot(Guid.NewGuid(), l2, 2, 0, null, 0, 2)]);
        Assert.False(AdminOrderOperationsComposer.SelectionsAreHomogeneousPackable(
            snapshot,
            [new AdminOrderLineSelection(l1, 1), new AdminOrderLineSelection(l2, 1)]));
    }

    [Fact]
    public void Fulfillment_error_codes_are_human_fa()
    {
        Assert.Equal("ابتدا پردازش را شروع کنید.", AdminOrderOperationsComposer.FulfillmentOpToFa("fulfillment.pack.requires_processing"));
        Assert.Equal("این قلم هنوز در مرحله پردازش نیست.", AdminOrderOperationsComposer.FulfillmentOpToFa("fulfillment.pack.not_processing"));
        Assert.Equal("این قلم هنوز بسته‌بندی نشده است.", AdminOrderOperationsComposer.FulfillmentOpToFa("fulfillment.ship.not_packed"));
        Assert.Equal("تعداد انتخاب‌شده بیشتر از تعداد قابل عملیات است.", AdminOrderOperationsComposer.FulfillmentOpToFa("fulfillment.selection.qty_exceeded"));
        Assert.Equal("ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.", AdminOrderOperationsComposer.FulfillmentOpToFa("fulfillment.bulk.incompatible"));
        Assert.Equal("عملیات گروهی روی فروشندگان متفاوت مجاز نیست.", AdminOrderOperationsComposer.FulfillmentOpToFa("fulfillment.bulk.cross_seller"));
    }

    [Fact]
    public void Line_status_stays_mixed_after_partial_pack()
    {
        var ready = Snapshot(FulfillmentStatus.ReadyToFulfill, [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 2, 0, null, 0)]);
        Assert.Equal("ReadyToFulfill", AdminPanelComposer.LineOperationalStatus(SellerOrderStatus.Paid, ready, 0, 2, 0));
        var processing = Snapshot(FulfillmentStatus.Processing, [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 3, 1, null, 0, 3)]);
        Assert.Equal("ReadyToFulfill", AdminPanelComposer.LineOperationalStatus(SellerOrderStatus.Paid, processing, 0, 3, 0));
        Assert.Equal("Processing", AdminPanelComposer.LineOperationalStatus(SellerOrderStatus.Paid, processing, 1, 3, 3));
        Assert.Equal("Packed", AdminPanelComposer.LineOperationalStatus(SellerOrderStatus.Paid, processing, 3, 3, 3));
        Assert.Equal("PendingPayment", AdminPanelComposer.LineOperationalStatus(SellerOrderStatus.PendingPayment, null, 0, 2));
        Assert.Equal("Cancelled", AdminPanelComposer.LineOperationalStatus(SellerOrderStatus.Cancelled, null, 0, 2));
    }

    [Fact]
    public void Composer_source_keeps_pack_behind_processing()
    {
        var root = FindRepoRoot();
        var composer = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsComposer.cs"));
        Assert.Contains("pack_selected", composer, StringComparison.Ordinal);
        Assert.Contains("QuantityProcessing", composer, StringComparison.Ordinal);
        Assert.Contains("unprocess", composer, StringComparison.Ordinal);
        Assert.Contains("fulfillment.pack.requires_processing", composer, StringComparison.Ordinal);
    }

    private static FulfillmentUnit CreateReady(Guid lineId, decimal qty, DateTimeOffset now) =>
        CreateReady([(lineId, qty)], now);

    private static FulfillmentUnit CreateReady(IReadOnlyList<(Guid LineId, decimal Qty)> lines, DateTimeOffset now) =>
        FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "گیرنده",
            "+98912",
            "تهران",
            "تهران",
            "آدرس",
            "12345",
            "storefront-default",
            "ارسال",
            lines.Select(x => (x.LineId, x.Qty, (Guid?)Guid.NewGuid())).ToArray(),
            now);

    private static FulfillmentSnapshot Snapshot(
        FulfillmentStatus status,
        IReadOnlyList<FulfillmentItemSnapshot> items) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            items,
            []);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
