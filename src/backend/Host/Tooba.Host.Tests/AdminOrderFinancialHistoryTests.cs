using Tooba.Host.Admin;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P09-T007 — projection سابقه مالی سفارش: attribution، عدم تکرار، برچسب‌های انسانی.
/// </summary>
public sealed class AdminOrderFinancialHistoryTests
{
    [Fact]
    public void Financial_history_includes_payment_refund_seller_payout_and_adjustment_once()
    {
        var group = SeedCheckout(unitPrice: 100_000m);
        var order = group.SellerOrders.Single();
        var sellerNames = new Dictionary<Guid, string> { [order.SellerPartyId] = "فروشگاه آرمان" };
        var paymentId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-09-01T10:00:00Z");
        var payment = new AdminPaymentOpsView(
            paymentId,
            group.CheckoutId,
            "Succeeded",
            200_000m,
            "IRR",
            "wallet",
            "req-1",
            "txn-1",
            now.AddMinutes(-30),
            now.AddMinutes(-20),
            now.AddMinutes(-20),
            null,
            false);

        var policy = new CommissionPolicySnapshot { PolicyId = Guid.NewGuid(), PolicyName = "default", Rate = 0.1m };
        var credit = MapEntry(SettlementEntry.PostCreditFromPayment(
            Guid.NewGuid(), order.SellerPartyId, paymentId, order.SellerOrderId, 200_000m, "IRR", policy, "pay-1", now.AddMinutes(-15)));
        var debit = MapEntry(SettlementEntry.PostDebitFromRefund(
            Guid.NewGuid(), order.SellerPartyId, Guid.NewGuid(), order.SellerOrderId, 50_000m, "IRR", policy, "ref-1", now.AddMinutes(-5)));

        var returnId = Guid.NewGuid();
        var refunds = new[]
        {
            new AdminPanelComposer.OrderFinancialRefundInput(
                returnId, Guid.NewGuid(), 50_000m, "IRR", now.AddMinutes(-8), 50_000m, "rf-a"),
            // retry duplicate for same return — must collapse to one movement
            new AdminPanelComposer.OrderFinancialRefundInput(
                returnId, Guid.NewGuid(), 50_000m, "IRR", now.AddMinutes(-7), 50_000m, "rf-b"),
        };

        var settlementByOrder = new Dictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>>
        {
            [order.SellerOrderId] = [credit, debit],
        };

        var events = AdminPanelComposer.BuildFinancialEvents(group, sellerNames, payment, settlementByOrder, refunds);

        Assert.Equal(1, events.Count(x => x.EventType == "CustomerReceipt"));
        Assert.Equal(1, events.Count(x => x.EventType == "CustomerRefund"));
        Assert.Equal(1, events.Count(x => x.EventType == "SellerPayout"));
        Assert.Equal(1, events.Count(x => x.EventType == "SellerRefundAdjustment"));
        Assert.Equal("دریافت از مشتری", events.Single(x => x.EventType == "CustomerReceipt").Description);
        Assert.Equal("بازگشت وجه به مشتری", events.Single(x => x.EventType == "CustomerRefund").Description);
        Assert.Equal("واریز سهم فروشنده", events.Single(x => x.EventType == "SellerPayout").Description);
        Assert.Equal("کسر از حساب فروشنده بابت بازگشت وجه", events.Single(x => x.EventType == "SellerRefundAdjustment").Description);
        Assert.Equal(credit.NetAmount, events.Single(x => x.EventType == "SellerPayout").Amount);
        Assert.Equal(debit.NetAmount, events.Single(x => x.EventType == "SellerRefundAdjustment").Amount);
        Assert.DoesNotContain(events, x => x.Description.Contains("قابل پرداخت", StringComparison.Ordinal));
        Assert.All(events, e => Assert.DoesNotContain("refund", e.Description, StringComparison.OrdinalIgnoreCase));
        Assert.All(events, e => Assert.DoesNotContain("debit", e.Description, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Batch_settlement_amount_is_not_shown_per_order_only_order_net()
    {
        var group = SeedCheckout(unitPrice: 80_000m);
        var order = group.SellerOrders.Single();
        var sellerNames = new Dictionary<Guid, string> { [order.SellerPartyId] = "فروشنده الف" };
        var policy = new CommissionPolicySnapshot { PolicyId = Guid.NewGuid(), PolicyName = "default", Rate = 0m };
        var orderNet = 80_000m;
        var credit = MapEntry(SettlementEntry.PostCreditFromPayment(
            Guid.NewGuid(), order.SellerPartyId, Guid.NewGuid(), order.SellerOrderId, orderNet, "IRR", policy, "pay-order", DateTimeOffset.UtcNow));

        // Fake "batch total" must never appear — projection only sees this SellerOrder's entry NetAmount.
        const decimal batchTotal = 500_000m;
        var events = AdminPanelComposer.BuildFinancialEvents(
            group,
            sellerNames,
            payment: null,
            new Dictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>> { [order.SellerOrderId] = [credit] },
            Array.Empty<AdminPanelComposer.OrderFinancialRefundInput>());

        Assert.Single(events);
        Assert.Equal("SellerPayout", events[0].EventType);
        Assert.Equal(orderNet, events[0].Amount);
        Assert.NotEqual(batchTotal, events[0].Amount);
    }

    [Fact]
    public void Prior_financial_movements_remain_immutable_when_reprojected()
    {
        var group = SeedCheckout(unitPrice: 10_000m);
        var order = group.SellerOrders.Single();
        var sellerNames = new Dictionary<Guid, string> { [order.SellerPartyId] = "فروشنده" };
        var policy = new CommissionPolicySnapshot { PolicyId = Guid.NewGuid(), PolicyName = "default", Rate = 0.05m };
        var postedAt = DateTimeOffset.Parse("2026-08-01T12:00:00Z");
        var credit = MapEntry(SettlementEntry.PostCreditFromPayment(
            Guid.NewGuid(), order.SellerPartyId, Guid.NewGuid(), order.SellerOrderId, 20_000m, "IRR", policy, "imm-1", postedAt));
        var settlement = new Dictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>> { [order.SellerOrderId] = [credit] };
        var first = AdminPanelComposer.BuildFinancialEvents(
            group, sellerNames, null, settlement, Array.Empty<AdminPanelComposer.OrderFinancialRefundInput>());
        var second = AdminPanelComposer.BuildFinancialEvents(
            group, sellerNames, null, settlement, Array.Empty<AdminPanelComposer.OrderFinancialRefundInput>());
        Assert.Equal(first.Single().OccurredAt, second.Single().OccurredAt);
        Assert.Equal(first.Single().Amount, second.Single().Amount);
        Assert.Equal(first.Single().Reference, second.Single().Reference);
        Assert.Equal(first.Single().Description, second.Single().Description);
    }

    [Fact]
    public void Operational_scope_helpers_are_human_and_fa_digit()
    {
        Assert.Equal("فروشگاه آرمان — ۲ قلم", AdminOrderCompletenessComposer.FormatPackScopeFa("فروشگاه آرمان", 2));
        Assert.Equal("کالای X — تعداد ۲", AdminOrderCompletenessComposer.FormatProductQtyScopeFa("کالای X", 2));
        Assert.DoesNotContain("aaaaaaaa", AdminOrderCompletenessComposer.FormatPackScopeFa("فروشگاه", 1), StringComparison.OrdinalIgnoreCase);
    }

    private static SettlementEntrySnapshot MapEntry(SettlementEntry entry) =>
        new(
            entry.EntryId,
            entry.SettlementAccountId,
            entry.SellerPartyId,
            entry.EntryType,
            entry.GrossAmount,
            entry.CommissionAmount,
            entry.NetAmount,
            entry.Currency,
            entry.CommissionPolicySnapshot,
            entry.SourceType,
            entry.SourceId,
            entry.SellerOrderId,
            entry.PostedAt);

    private static CheckoutGroup SeedCheckout(decimal unitPrice)
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            2,
            unitPrice,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            0m,
            unitPrice * 2,
            null);
        var order = SellerOrder.Open(
            checkoutId,
            seller,
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
        return CheckoutGroup.Submit(
            checkoutId,
            $"idem-{checkoutId:N}",
            Guid.NewGuid(),
            OrderMode.OnlinePurchase,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            [order],
            DateTimeOffset.UtcNow,
            "گیرنده تست",
            "09120000000",
            "تهران",
            "تهران",
            "آدرس تست",
            "1234567890",
            "post",
            "پست");
    }
}
