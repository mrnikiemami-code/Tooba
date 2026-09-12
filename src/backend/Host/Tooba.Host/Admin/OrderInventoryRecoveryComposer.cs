#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;

namespace Tooba.Host.Admin;

public sealed class OrderInventoryRecoveryComposer
{
    public const string NotePrefixSucceeded = "[inventory_recovery:succeeded]";
    public const string NotePrefixFailed = "[inventory_recovery:failed_insufficient]";
    public const string NotePrefixManual = "[inventory_recovery:requires_manual_review]";
    public const string NotePrefixRequested = "[inventory_recovery:requested]";

    private readonly OrderDbContext _orders;
    private readonly IInventoryDirectory _inventory;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly IPaymentAdminDirectory _payments;
    private readonly ICheckoutDirectory _checkout;
    private readonly PaymentGatewayOptions _gateway;

    public OrderInventoryRecoveryComposer(
        OrderDbContext orders,
        IInventoryDirectory inventory,
        IFulfillmentDirectory fulfillment,
        IPaymentAdminDirectory payments,
        ICheckoutDirectory checkout,
        IOptions<PaymentGatewayOptions> gateway)
    {
        _orders = orders;
        _inventory = inventory;
        _fulfillment = fulfillment;
        _payments = payments;
        _checkout = checkout;
        _gateway = gateway.Value;
    }

    public async Task<OrderInventoryRecoveryAuditPage> AuditAsync(int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 200);
        var checkouts = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders).ThenInclude(x => x.Lines)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(Math.Max(take * 5, 100))
            .ToListAsync(cancellationToken);

        var rows = new List<OrderInventoryRecoveryAuditRow>();
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 0, ["B"] = 0, ["C"] = 0, ["Healthy"] = 0, ["NotEligible"] = 0,
        };

        foreach (var group in checkouts)
        {
            var assessment = await AssessAsync(group, cancellationToken);
            counts[assessment.ClassCode] = counts.GetValueOrDefault(assessment.ClassCode) + 1;
            if (assessment.ClassCode is "Healthy" or "NotEligible") continue;
            if (rows.Count < take)
            {
                rows.Add(new OrderInventoryRecoveryAuditRow(
                    assessment.CheckoutId, assessment.OrderNumbers, assessment.ClassCode,
                    assessment.OutcomeHint, assessment.NeedsRecovery, assessment.ReasonFa));
            }
        }

        return new OrderInventoryRecoveryAuditPage(counts, rows);
    }

    public async Task<OrderInventoryRecoveryAssessment> AssessCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var group = await LoadGroupAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");
        return await AssessAsync(group, cancellationToken);
    }

    public async Task<OrderInventoryRecoveryResult> RecoverAsync(
        Guid checkoutId, Guid actorUserId, string? reason, CancellationToken cancellationToken)
    {
        var group = await LoadGroupAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");

        await _checkout.AddNoteAsync(checkoutId, actorUserId, $"{NotePrefixRequested} {TrimReason(reason)}", cancellationToken);
        var assessment = await AssessAsync(group, cancellationToken);
        if (assessment.ClassCode is "Healthy") return Result("AlreadyHealthy", assessment, null);
        if (assessment.ClassCode is "NotEligible") return Result("NotEligible", assessment, null);
        if (assessment.ClassCode is "C")
        {
            await _checkout.AddNoteAsync(checkoutId, actorUserId, $"{NotePrefixManual} {assessment.ReasonFa}", cancellationToken);
            return Result("RequiresManualReview", assessment, assessment.ReasonFa);
        }

        if (!assessment.NeedsRecovery || assessment.Lines.Count == 0)
            return Result("AlreadyHealthy", assessment, null);

        var acquired = new List<Guid>();
        try
        {
            foreach (var line in assessment.Lines)
            {
                DateTimeOffset? expiresAt = assessment.ClassCode == "A"
                    ? DateTimeOffset.UtcNow.AddHours(Math.Clamp(_gateway.ManualPaymentReviewHoldHours, 1, 24 * 30))
                    : null;

                var receipt = await _inventory.ReserveAsync(
                    line.StockItemId, line.RemainingQuantity,
                    $"inventory-recovery-{line.OrderLineId:N}",
                    $"inventory-recovery-{line.OrderLineId:N}-{DateTimeOffset.UtcNow.UtcTicks}",
                    expiresAt, cancellationToken);
                acquired.Add(receipt.ReservationId);

                if (assessment.ClassCode == "B")
                    await _inventory.CommitReservationForPaidOrderAsync(receipt.ReservationId, cancellationToken);

                var orderLine = group.SellerOrders.SelectMany(o => o.Lines).Single(x => x.LineId == line.OrderLineId);
                orderLine.ReplaceReservation(receipt.ReservationId);
            }

            await _orders.SaveChangesAsync(cancellationToken);
            await _fulfillment.RebindActiveReservationsFromOrderAsync(checkoutId, cancellationToken);
            await _checkout.AddNoteAsync(checkoutId, actorUserId,
                $"{NotePrefixSucceeded} class={assessment.ClassCode} lines={assessment.Lines.Count}", cancellationToken);

            group = await LoadGroupAsync(checkoutId, cancellationToken) ?? group;
            return Result("Recovered", await AssessAsync(group, cancellationToken), null);
        }
        catch (Exception)
        {
            foreach (var reservationId in acquired)
            {
                try { await _inventory.ReleaseAsync(reservationId, cancellationToken); } catch { }
            }

            await _checkout.AddNoteAsync(checkoutId, actorUserId, $"{NotePrefixFailed} class={assessment.ClassCode}", cancellationToken);
            throw new PlatformHttpException(400,
                "موجودی این سفارش پس از ثبت پرداخت مشتری در دسترس نیست. سفارش نیازمند تعیین تکلیف موجودی یا بازگشت وجه است.",
                "inventory.recovery.insufficient");
        }
    }

    private async Task<OrderInventoryRecoveryAssessment> AssessAsync(CheckoutGroup group, CancellationToken cancellationToken)
    {
        var orderNumbers = string.Join(", ", group.SellerOrders.Select(x => x.OrderNumber));
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);

        if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
            return Assessment(group, orderNumbers, "NotEligible", false, "سفارش لغو شده است.", []);
        if (payment is null)
            return Assessment(group, orderNumbers, "C", false, "پرداخت نامشخص است.", []);
        if (payment.Status == PaymentStatus.Refunded)
            return Assessment(group, orderNumbers, "NotEligible", false, "پرداخت مسترد شده است.", []);

        var manual = ManualPaymentGateway.IsManual(payment.ProviderCode);
        var classA = manual && payment.Status == PaymentStatus.Pending
            && (!string.IsNullOrWhiteSpace(payment.CustomerTransferReference) || payment.EvidenceSubmittedAt is not null);
        var classB = payment.Status == PaymentStatus.Succeeded;

        if (payment.Status == PaymentStatus.Failed && payment.HasManualDepositRejection && !classA)
            return Assessment(group, orderNumbers, "NotEligible", false, "واریز رد شده و ادعای فعال ندارد.", []);
        if (!classA && !classB)
            return Assessment(group, orderNumbers, "NotEligible", false, "وضعیت پرداخت واجد شرایط بازیابی نیست.", []);

        var lines = new List<OrderInventoryRecoveryLineNeed>();
        var anyUnfulfilled = false;
        var anyUnhealthy = false;
        var ambiguous = false;

        foreach (var order in group.SellerOrders.Where(x => x.Status != SellerOrderStatus.Cancelled))
        {
            var fulfillment = fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId);
            foreach (var line in order.Lines)
            {
                var shipped = fulfillment?.Items.FirstOrDefault(i => i.OrderLineId == line.LineId)?.QuantityShipped ?? 0m;
                var remaining = line.Quantity - shipped;
                if (remaining <= 0) continue;
                anyUnfulfilled = true;

                if (line.ReservationId is not { } reservationId) { ambiguous = true; continue; }
                var existing = await _inventory.FindReservationAsync(reservationId, cancellationToken);
                if (existing is null) { ambiguous = true; continue; }

                var healthy = existing.Status == StockReservationStatus.Held
                    && (existing.ExpiresAt is null || existing.ExpiresAt > DateTimeOffset.UtcNow)
                    && existing.Quantity >= remaining;
                if (healthy) continue;

                if (existing.Status == StockReservationStatus.Consumed && remaining > 0) { ambiguous = true; continue; }

                if (existing.Status == StockReservationStatus.Released
                    || (existing.Status == StockReservationStatus.Held && existing.ExpiresAt is not null && existing.ExpiresAt <= DateTimeOffset.UtcNow))
                {
                    anyUnhealthy = true;
                    lines.Add(new OrderInventoryRecoveryLineNeed(
                        line.LineId, order.SellerOrderId, existing.StockItemId, remaining, reservationId, existing.Status.ToString()));
                    continue;
                }

                ambiguous = true;
            }
        }

        if (!anyUnfulfilled)
            return Assessment(group, orderNumbers, "NotEligible", false, "مقدار قابل تحویل باقی نمانده است.", []);
        if (ambiguous && !anyUnhealthy)
            return Assessment(group, orderNumbers, "C", false, "وضعیت رزرو مبهم است و بازیابی خودکار مجاز نیست.", []);
        if (!anyUnhealthy)
            return Assessment(group, orderNumbers, "Healthy", false, "رزرو فعال سالم است.", []);
        if (ambiguous)
            return Assessment(group, orderNumbers, "C", false, "برخی خطوط مبهم‌اند؛ بازیابی خودکار انجام نمی‌شود.", lines);

        var code = classB ? "B" : "A";
        return Assessment(group, orderNumbers, code, true,
            code == "B" ? "پرداخت موفق است ولی رزرو معتبر نیست." : "مدرک پرداخت ثبت شده ولی رزرو بررسی معتبر نیست.",
            lines);
    }

    private async Task<CheckoutGroup?> LoadGroupAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        await _orders.Checkouts.Include(x => x.SellerOrders).ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);

    private static OrderInventoryRecoveryAssessment Assessment(
        CheckoutGroup group, string orderNumbers, string classCode, bool needsRecovery, string reasonFa,
        IReadOnlyList<OrderInventoryRecoveryLineNeed> lines) =>
        new(group.CheckoutId, orderNumbers, classCode, needsRecovery, reasonFa, classCode, lines);

    private static OrderInventoryRecoveryResult Result(string outcome, OrderInventoryRecoveryAssessment assessment, string? messageFa) =>
        new(outcome, assessment.ClassCode, assessment.NeedsRecovery, messageFa ?? assessment.ReasonFa, assessment.OrderNumbers);

    private static string TrimReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? "admin" : reason.Trim()[..Math.Min(reason.Trim().Length, 120)];
}

public sealed record OrderInventoryRecoveryLineNeed(
    Guid OrderLineId, Guid SellerOrderId, Guid StockItemId, decimal RemainingQuantity, Guid PreviousReservationId, string PreviousStatus);

public sealed record OrderInventoryRecoveryAssessment(
    Guid CheckoutId, string OrderNumbers, string ClassCode, bool NeedsRecovery, string ReasonFa, string OutcomeHint,
    IReadOnlyList<OrderInventoryRecoveryLineNeed> Lines);

public sealed record OrderInventoryRecoveryResult(
    string Outcome, string ClassCode, bool NeedsRecovery, string MessageFa, string OrderNumbers);

public sealed record OrderInventoryRecoveryAuditRow(
    Guid CheckoutId, string OrderNumbers, string ClassCode, string OutcomeHint, bool NeedsRecovery, string ReasonFa);

public sealed record OrderInventoryRecoveryAuditPage(
    IReadOnlyDictionary<string, int> CountsByClass, IReadOnlyList<OrderInventoryRecoveryAuditRow> Candidates);
