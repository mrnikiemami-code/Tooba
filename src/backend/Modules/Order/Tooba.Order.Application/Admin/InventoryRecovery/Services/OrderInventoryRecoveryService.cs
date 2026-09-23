using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Order.Application.Admin.Completeness.History;
using Tooba.Order.Application.Admin.InventoryRecovery.Models;
using Tooba.Order.Application.Admin.Supply.Ports;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;
using Tooba.Payment.Contracts.Hold;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Admin.InventoryRecovery.Services;

/// <summary>ارزیابی و بازیابی رزرو موجودی سفارش‌های واجد شرایط.</summary>
public sealed class OrderInventoryRecoveryService
{
    public const string NotePrefixSucceeded = AdminOrderInventoryRecoveryNotePrefixes.Succeeded;
    public const string NotePrefixFailed = AdminOrderInventoryRecoveryNotePrefixes.Failed;
    public const string NotePrefixManual = AdminOrderInventoryRecoveryNotePrefixes.Manual;
    public const string NotePrefixRequested = AdminOrderInventoryRecoveryNotePrefixes.Requested;

    private const string ManualProviderCode = "manual";

    private readonly IOrderSupplyCheckoutStore _orders;
    private readonly IOrderInventoryLifecyclePort _inventory;
    private readonly IFulfillmentAdminOperations _fulfillment;
    private readonly IPaymentAdminGateway _payments;
    private readonly ICheckoutDirectory _checkout;
    private readonly IClock _clock;
    private readonly ICommerceHoldPolicySource? _holdPolicy;
    private readonly IReservationCycleDirectory? _cycles;
    private readonly IReservationCyclePolicyResolver? _cyclePolicy;

    /// <summary>سرویس بازیابی را به درزهای Order/Inventory/Fulfillment/Payment وصل می‌کند.</summary>
    public OrderInventoryRecoveryService(
        IOrderSupplyCheckoutStore orders,
        IOrderInventoryLifecyclePort inventory,
        IFulfillmentAdminOperations fulfillment,
        IPaymentAdminGateway payments,
        ICheckoutDirectory checkout,
        IClock clock,
        ICommerceHoldPolicySource? holdPolicy = null,
        IReservationCycleDirectory? cycles = null,
        IReservationCyclePolicyResolver? cyclePolicy = null)
    {
        _orders = orders;
        _inventory = inventory;
        _fulfillment = fulfillment;
        _payments = payments;
        _checkout = checkout;
        _clock = clock;
        _holdPolicy = holdPolicy;
        _cycles = cycles;
        _cyclePolicy = cyclePolicy;
    }

    /// <summary>Audit کاندیداهای بازیابی.</summary>
    public async Task<OrderInventoryRecoveryAuditPage> AuditAsync(int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 200);
        var checkouts = await _orders.ListRecentAsync(Math.Max(take * 5, 100), cancellationToken);

        var rows = new List<OrderInventoryRecoveryAuditRow>();
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 0, ["B"] = 0, ["C"] = 0, ["Healthy"] = 0, ["NotEligible"] = 0,
        };

        foreach (var group in checkouts)
        {
            var assessment = await AssessAsync(group, cancellationToken);
            counts[assessment.ClassCode] = counts.GetValueOrDefault(assessment.ClassCode) + 1;
            if (assessment.ClassCode is "Healthy" or "NotEligible")
            {
                continue;
            }

            if (rows.Count < take)
            {
                rows.Add(new OrderInventoryRecoveryAuditRow(
                    assessment.CheckoutId,
                    assessment.OrderNumbers,
                    assessment.ClassCode,
                    assessment.OutcomeHint,
                    assessment.NeedsRecovery,
                    assessment.ReasonFa));
            }
        }

        return new OrderInventoryRecoveryAuditPage(counts, rows);
    }

    /// <summary>ارزیابی یک checkout.</summary>
    public async Task<Result<OrderInventoryRecoveryAssessment>> AssessCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await _orders.GetAsync(checkoutId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<OrderInventoryRecoveryAssessment>(new SemanticError("order.operation.invalid"));
        }

        return Result.Success(await AssessAsync(group, cancellationToken));
    }

    /// <summary>اجرای بازیابی رزرو.</summary>
    public async Task<Result<OrderInventoryRecoveryResult>> RecoverAsync(
        Guid checkoutId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var group = await _orders.GetAsync(checkoutId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<OrderInventoryRecoveryResult>(new SemanticError("order.operation.invalid"));
        }

        await _checkout.AddNoteAsync(checkoutId, actorUserId, $"{NotePrefixRequested} {TrimReason(reason)}", cancellationToken);
        var assessment = await AssessAsync(group, cancellationToken);
        if (assessment.ClassCode is "Healthy")
        {
            return Result.Success(ToResult("AlreadyHealthy", assessment, null));
        }

        if (assessment.ClassCode is "NotEligible")
        {
            return Result.Success(ToResult("NotEligible", assessment, null));
        }

        if (assessment.ClassCode is "C")
        {
            await _checkout.AddNoteAsync(checkoutId, actorUserId, $"{NotePrefixManual} {assessment.ReasonFa}", cancellationToken);
            return Result.Success(ToResult("RequiresManualReview", assessment, assessment.ReasonFa));
        }

        if (!assessment.NeedsRecovery || assessment.Lines.Count == 0)
        {
            return Result.Success(ToResult("AlreadyHealthy", assessment, null));
        }

        var acquired = new List<Guid>();
        try
        {
            var bindings = new Dictionary<Guid, Guid>();
            foreach (var line in assessment.Lines)
            {
                DateTimeOffset? expiresAt = assessment.ClassCode == "A"
                    ? ResolveManualReviewExpiresAt()
                    : null;

                var now = _clock.UtcNow;
                var receipt = await _inventory.ReserveAsync(
                    line.StockItemId,
                    line.RemainingQuantity,
                    $"inventory-recovery-{line.OrderLineId:N}",
                    $"inventory-recovery-{line.OrderLineId:N}-{now.UtcTicks}",
                    expiresAt,
                    cancellationToken);
                acquired.Add(receipt.ReservationId);

                if (assessment.ClassCode == "B")
                {
                    await _inventory.CommitReservationForPaidOrderAsync(receipt.ReservationId, cancellationToken);
                }

                bindings[line.OrderLineId] = receipt.ReservationId;
            }

            await _orders.ReplaceReservationsAsync(checkoutId, bindings, cancellationToken);
            await _fulfillment.RebindActiveReservationsFromOrderAsync(checkoutId, cancellationToken);
            if (_cycles is not null)
            {
                var now = _clock.UtcNow;
                var policy = _cyclePolicy is null
                    ? new ReservationCyclePolicySnapshot(120, 120, 3, "platform")
                    : await _cyclePolicy.ResolveAsync(
                        group.SellerOrders.SelectMany(x => x.Lines)
                            .Select(x => new ReservationCyclePolicyLine(x.OfferId, x.CategoryIdSnapshot))
                            .ToArray(),
                        cancellationToken);
                await _cycles.StartAsync(
                    checkoutId,
                    ReservationCycleReason.HistoricalRecovery,
                    now,
                    now.AddMinutes(Math.Max(1, policy.InitialHoldMinutes)),
                    policy,
                    acquired,
                    actor: "historical-recovery",
                    correlationId: $"cycle-hist:{checkoutId:N}",
                    paymentAttemptId: null,
                    cancellationToken);
                if (assessment.ClassCode == "B")
                {
                    await _cycles.CloseActiveAsync(
                        checkoutId,
                        ReservationCycleStatus.CommittedPaid,
                        now,
                        cancellationToken);
                }
            }

            await _checkout.AddNoteAsync(
                checkoutId,
                actorUserId,
                $"{NotePrefixSucceeded} class={assessment.ClassCode} lines={assessment.Lines.Count}",
                cancellationToken);

            group = await _orders.GetAsync(checkoutId, cancellationToken) ?? group;
            return Result.Success(ToResult("Recovered", await AssessAsync(group, cancellationToken), null));
        }
        catch (ContractOperationException ex) when (IsExpectedRecoveryFault(ex.Code))
        {
            var rollbackFailures = await RollbackAcquiredAsync(acquired, cancellationToken);
            if (rollbackFailures.Count > 0)
            {
                throw new AggregateException(
                    "inventory.recovery.rollback_failed",
                    new Exception[] { ex }.Concat(rollbackFailures));
            }

            await _checkout.AddNoteAsync(
                checkoutId,
                actorUserId,
                $"{NotePrefixFailed} class={assessment.ClassCode}",
                cancellationToken);
            return Result.Failure<OrderInventoryRecoveryResult>(
                new SemanticError(MapRecoveryFaultCode(ex.Code)));
        }
        catch (Exception ex)
        {
            var rollbackFailures = await RollbackAcquiredAsync(acquired, cancellationToken);
            await _checkout.AddNoteAsync(
                checkoutId,
                actorUserId,
                $"{NotePrefixFailed} class={assessment.ClassCode}",
                cancellationToken);
            if (rollbackFailures.Count > 0)
            {
                throw new AggregateException(
                    "inventory.recovery.rollback_failed",
                    new Exception[] { ex }.Concat(rollbackFailures));
            }

            throw;
        }
    }

    private async Task<List<Exception>> RollbackAcquiredAsync(List<Guid> acquired, CancellationToken cancellationToken)
    {
        var rollbackFailures = new List<Exception>();
        foreach (var reservationId in acquired)
        {
            try
            {
                await _inventory.ReleaseHeldReservationAsync(reservationId, cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                rollbackFailures.Add(rollbackEx);
            }
        }

        return rollbackFailures;
    }

    private static bool IsExpectedRecoveryFault(string code) =>
        code is "inventory.supply.unavailable"
            or "inventory.recovery.insufficient"
            or "inventory.reservation.conflict"
            or "inventory.manual_review.unavailable"
            or "inventory.reservation.not_found"
            or "inventory.reservation.not_active";

    private static string MapRecoveryFaultCode(string code) =>
        code is "inventory.supply.unavailable" or "inventory.reservation.conflict"
            ? "inventory.recovery.insufficient"
            : code;

    private DateTimeOffset ResolveManualReviewExpiresAt()
    {
        if (_holdPolicy is not null)
        {
            return _holdPolicy.ResolveManualReviewExpiresAt(_clock.UtcNow);
        }

        return _clock.UtcNow.AddHours(48);
    }

    private async Task<OrderInventoryRecoveryAssessment> AssessAsync(
        OrderSupplyCheckoutSnapshot group,
        CancellationToken cancellationToken)
    {
        var orderNumbers = string.Join(", ", group.SellerOrders.Select(x => x.OrderNumber));
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);

        if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
        {
            return Assessment(group, orderNumbers, "NotEligible", false, "سفارش لغو شده است.", []);
        }

        if (payment is null)
        {
            return Assessment(group, orderNumbers, "C", false, "پرداخت نامشخص است.", []);
        }

        if (payment.Status == "Refunded")
        {
            return Assessment(group, orderNumbers, "NotEligible", false, "پرداخت مسترد شده است.", []);
        }

        var manual = string.Equals(payment.ProviderCode, ManualProviderCode, StringComparison.OrdinalIgnoreCase);
        var classA = manual && payment.Status == "Pending"
            && (!string.IsNullOrWhiteSpace(payment.CustomerTransferReference) || payment.EvidenceSubmittedAt is not null);
        var classB = payment.Status == "Succeeded";

        if (payment.Status == "Failed" && payment.HasManualDepositRejection && !classA)
        {
            return Assessment(group, orderNumbers, "NotEligible", false, "واریز رد شده و ادعای فعال ندارد.", []);
        }

        if (!classA && !classB)
        {
            return Assessment(group, orderNumbers, "NotEligible", false, "وضعیت پرداخت واجد شرایط بازیابی نیست.", []);
        }

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
                if (remaining <= 0)
                {
                    continue;
                }

                anyUnfulfilled = true;

                if (line.ReservationId is not { } reservationId)
                {
                    ambiguous = true;
                    continue;
                }

                var existing = await _inventory.FindReservationAsync(reservationId, cancellationToken);
                if (existing is null)
                {
                    ambiguous = true;
                    continue;
                }

                var now = _clock.UtcNow;
                var healthy = existing.Status == "Held"
                    && (existing.ExpiresAt is null || existing.ExpiresAt > now)
                    && existing.Quantity >= remaining;
                if (healthy)
                {
                    continue;
                }

                if (existing.Status == "Consumed" && remaining > 0)
                {
                    ambiguous = true;
                    continue;
                }

                if (existing.Status == "Released"
                    || (existing.Status == "Held" && existing.ExpiresAt is not null && existing.ExpiresAt <= now))
                {
                    anyUnhealthy = true;
                    lines.Add(new OrderInventoryRecoveryLineNeed(
                        line.LineId,
                        order.SellerOrderId,
                        existing.StockItemId,
                        remaining,
                        reservationId,
                        existing.Status));
                    continue;
                }

                ambiguous = true;
            }
        }

        if (!anyUnfulfilled)
        {
            return Assessment(group, orderNumbers, "NotEligible", false, "مقدار قابل تحویل باقی نمانده است.", []);
        }

        if (ambiguous && !anyUnhealthy)
        {
            return Assessment(group, orderNumbers, "C", false, "وضعیت رزرو مبهم است و بازیابی خودکار مجاز نیست.", []);
        }

        if (!anyUnhealthy)
        {
            return Assessment(group, orderNumbers, "Healthy", false, "رزرو فعال سالم است.", []);
        }

        if (ambiguous)
        {
            return Assessment(group, orderNumbers, "C", false, "برخی خطوط مبهم‌اند؛ بازیابی خودکار انجام نمی‌شود.", lines);
        }

        var code = classB ? "B" : "A";
        return Assessment(
            group,
            orderNumbers,
            code,
            true,
            code == "B"
                ? "پرداخت موفق است ولی رزرو معتبر نیست."
                : "مدرک پرداخت ثبت شده ولی رزرو بررسی معتبر نیست.",
            lines);
    }

    private static OrderInventoryRecoveryAssessment Assessment(
        OrderSupplyCheckoutSnapshot group,
        string orderNumbers,
        string classCode,
        bool needsRecovery,
        string reasonFa,
        IReadOnlyList<OrderInventoryRecoveryLineNeed> lines) =>
        new(group.CheckoutId, orderNumbers, classCode, needsRecovery, reasonFa, classCode, lines);

    private static OrderInventoryRecoveryResult ToResult(
        string outcome,
        OrderInventoryRecoveryAssessment assessment,
        string? messageFa) =>
        new(outcome, assessment.ClassCode, assessment.NeedsRecovery, messageFa ?? assessment.ReasonFa, assessment.OrderNumbers);

    private static string TrimReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? "admin" : reason.Trim()[..Math.Min(reason.Trim().Length, 120)];
}
