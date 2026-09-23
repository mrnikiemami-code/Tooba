using Tooba.Catalog.Contracts;
using Tooba.Fulfillment.Contracts.History;
using Tooba.Order.Domain;
using Tooba.Party.Contracts;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.History;
using Tooba.Settlement.Contracts.History;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Admin.Completeness.History;

/// <summary>
/// تاریخچهٔ عملیاتی را از منابع موجود ترکیب می‌کند — بدون event store جدید و بدون DbContext بیگانه.
/// </summary>
public static partial class AdminOrderHistoryComposer
{
    /// <summary>
    /// همهٔ سطرهای تاریخچهٔ یک checkout را می‌سازد و قطعی مرتب می‌کند
    /// (<c>OccurredAt</c> نزولی، سپس <c>Kind</c> ترتیبی). صفحه‌بندی پس از این ادغام انجام می‌شود.
    /// </summary>
    public static async Task<IReadOnlyList<AdminOrderHistoryDraft>> ComposeAsync(
        CheckoutGroup group,
        IReadOnlyList<CheckoutOperationalNoteSnapshot> notes,
        PaymentAdminOperationalSnapshot? payment,
        IFulfillmentHistoryReader fulfillment,
        IReturnHistoryReader returns,
        ISettlementHistoryReader settlement,
        IPartyLookup parties,
        ICatalogVariantLookup catalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(notes);
        ArgumentNullException.ThrowIfNull(fulfillment);
        ArgumentNullException.ThrowIfNull(returns);
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(parties);
        ArgumentNullException.ThrowIfNull(catalog);

        var entries = new List<AdminOrderHistoryDraft>();
        AppendOrderLifecycle(entries, group);
        AppendPayment(entries, group, payment);

        var scope = await AdminOrderHistoryScope.LoadAsync(group, parties, catalog, cancellationToken);
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();

        AppendFulfillments(
            entries,
            group,
            scope,
            await fulfillment.ListFulfillmentsForCheckoutAsync(group.CheckoutId, cancellationToken));
        AppendConsolidatedPackages(
            entries,
            scope,
            await fulfillment.ListConsolidatedPackagesForCheckoutAsync(group.CheckoutId, cancellationToken));
        AppendReturns(
            entries,
            group,
            scope,
            await returns.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken));
        AppendSettlements(
            entries,
            group,
            await settlement.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken));
        AppendNotes(entries, notes);

        return entries
            .OrderByDescending(x => x.OccurredAt)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// یادداشت بازیابی موجودی را به نوع تاریخچه نگاشت می‌کند؛ false یعنی یادداشت داخلی معمولی است.
    /// </summary>
    public static bool TryMapInventoryRecoveryNote(
        string? body,
        out string kind,
        out string labelFa,
        out string labelEn)
    {
        kind = string.Empty;
        labelFa = string.Empty;
        labelEn = string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        if (body.StartsWith(AdminOrderInventoryRecoveryNotePrefixes.Requested, StringComparison.Ordinal))
        {
            (kind, labelFa, labelEn) =
                ("inventory_recovery_requested", "درخواست بازیابی موجودی", "Inventory Recovery Requested");
            return true;
        }

        if (body.StartsWith(AdminOrderInventoryRecoveryNotePrefixes.Succeeded, StringComparison.Ordinal))
        {
            (kind, labelFa, labelEn) =
                ("inventory_recovery_succeeded", "بازیابی موجودی موفق", "Inventory Recovery Succeeded");
            return true;
        }

        if (body.StartsWith(AdminOrderInventoryRecoveryNotePrefixes.Failed, StringComparison.Ordinal))
        {
            (kind, labelFa, labelEn) = (
                "inventory_recovery_failed_insufficient",
                "شکست بازیابی موجودی — کمبود موجودی",
                "Inventory Recovery Failed — Insufficient Inventory");
            return true;
        }

        if (body.StartsWith(AdminOrderInventoryRecoveryNotePrefixes.Manual, StringComparison.Ordinal))
        {
            (kind, labelFa, labelEn) = (
                "inventory_recovery_manual_review",
                "بازیابی موجودی نیازمند بررسی دستی",
                "Inventory Recovery Requires Manual Review");
            return true;
        }

        return false;
    }

    private static void AppendOrderLifecycle(List<AdminOrderHistoryDraft> entries, CheckoutGroup group)
    {
        var reference = $"Checkout {group.CheckoutId:N}"[..20];
        entries.Add(Draft(group.SubmittedAt, "order_created", "ثبت سفارش", "Order created", null, reference, reference));

        foreach (var order in group.SellerOrders.Where(x => x.Status == SellerOrderStatus.Cancelled))
        {
            entries.Add(Draft(
                group.SubmittedAt,
                "order_cancelled",
                "سفارش لغو شد",
                "Order cancelled",
                null,
                $"سفارش {order.OrderNumber}",
                $"Order {order.OrderNumber}"));
            entries.Add(Draft(
                group.SubmittedAt,
                "inventory_released",
                "رزرو موجودی آزاد شد",
                "Inventory reservation released",
                null,
                $"سفارش {order.OrderNumber}",
                $"Order {order.OrderNumber}"));
        }

        foreach (var order in group.SellerOrders.Where(x => x.LastRestoredAt is not null))
        {
            entries.Add(Draft(
                order.LastRestoredAt!.Value,
                "order_restored",
                "بازگردانی سفارش لغوشده",
                "Cancelled order restored",
                null,
                $"سفارش {order.OrderNumber}",
                $"Order {order.OrderNumber}"));
        }
    }

    private static void AppendNotes(
        List<AdminOrderHistoryDraft> entries,
        IReadOnlyList<CheckoutOperationalNoteSnapshot> notes)
    {
        foreach (var note in notes)
        {
            var actor = note.CreatedByUserId == Guid.Empty ? (Guid?)null : note.CreatedByUserId;
            var summary = AdminOrderHistoryFormatting.Truncate(note.Body, 120);
            if (TryMapInventoryRecoveryNote(note.Body, out var kind, out var labelFa, out var labelEn))
            {
                entries.Add(Draft(note.CreatedAt, kind, labelFa, labelEn, actor, summary, summary));
                continue;
            }

            entries.Add(Draft(
                note.CreatedAt,
                "operational_note",
                "یادداشت داخلی",
                "Internal note",
                actor,
                summary,
                summary));
        }
    }

    private static AdminOrderHistoryDraft Draft(
        DateTimeOffset occurredAt,
        string kind,
        string labelFa,
        string labelEn,
        Guid? actorUserId,
        string? summaryFa = null,
        string? summaryEn = null) =>
        new(occurredAt, kind, labelFa, labelEn, actorUserId, summaryFa, summaryEn);
}
