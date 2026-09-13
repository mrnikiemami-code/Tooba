using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;

namespace Tooba.Host.Storefront;

/// <summary>
/// نگاشت تصویر در انتظار پرداخت. انقضا را جعل نمی‌کند و قابلیت را از سرور می‌سازد.
/// </summary>
public static class StorefrontPendingPaymentProjector
{
    /// <summary>ورودی سفارش برای نگاشت بدون DbContext.</summary>
    public sealed record CheckoutInput(
        Guid CheckoutId,
        Guid CartId,
        Guid PlacedByUserId,
        DateTimeOffset SubmittedAt,
        IReadOnlyList<SellerInput> Sellers);

    /// <summary>سفارش فروشنده فشرده.</summary>
    public sealed record SellerInput(
        string OrderNumber,
        SellerOrderStatus Status,
        decimal PayableAmount,
        string Currency,
        IReadOnlyList<LineInput> Lines);

    /// <summary>خط فشرده.</summary>
    public sealed record LineInput(string Title, decimal Quantity, Guid? MediaAssetId);

    /// <summary>آخرین پرداخت سفارش.</summary>
    public sealed record PaymentInput(
        Guid PaymentId,
        PaymentStatus Status,
        string ProviderCode,
        DateTimeOffset? EvidenceSubmittedAt,
        decimal Amount,
        string Currency);

    /// <summary>سفارش‌های قابل نمایش مشتری را از دستهٔ بارگذاری‌شده می‌سازد.</summary>
    public static StorefrontPendingPaymentPage Project(
        IReadOnlyList<CheckoutInput> checkouts,
        IReadOnlyDictionary<Guid, PaymentInput> payments,
        IReadOnlyDictionary<Guid, ReservationCycleProjection> cycles,
        DateTimeOffset serverNow)
    {
        var items = new List<StorefrontPendingPaymentItemView>();
        foreach (var checkout in checkouts.OrderByDescending(x => x.SubmittedAt))
        {
            var mapped = MapOne(checkout, payments.GetValueOrDefault(checkout.CheckoutId), cycles.GetValueOrDefault(checkout.CheckoutId), serverNow);
            if (mapped is not null)
            {
                items.Add(mapped);
            }
        }

        return new StorefrontPendingPaymentPage(serverNow, items);
    }

    private static StorefrontPendingPaymentItemView? MapOne(
        CheckoutInput checkout,
        PaymentInput? payment,
        ReservationCycleProjection? cycle,
        DateTimeOffset serverNow)
    {
        if (checkout.Sellers.Count > 0 && checkout.Sellers.All(x => x.Status == SellerOrderStatus.Cancelled))
        {
            return null;
        }

        if (cycle is { CurrentStatus: ReservationCycleStatus.ReleasedByCancel })
        {
            return null;
        }

        if (payment is not null && IsTerminalHidden(payment.Status))
        {
            return null;
        }

        var cycleActive = cycle is { CurrentStatus: ReservationCycleStatus.Active, SecondsRemaining: > 0 };
        var retryRemaining = cycle?.RetryCountRemaining ?? 0;
        var maxCycles = cycle?.EffectiveMaxCycles ?? 3;
        var retryLimit = !cycleActive && retryRemaining <= 0 && (cycle?.TotalCyclesCreated ?? 0) >= maxCycles;
        var isManual = ManualPaymentGateway.IsManual(payment?.ProviderCode);
        var awaitingReview = isManual
            && payment is { Status: PaymentStatus.Pending, EvidenceSubmittedAt: not null };
        var cycleEnded = !cycleActive && cycle is not null
            && (cycle.CurrentStatus is ReservationCycleStatus.Expired
                or ReservationCycleStatus.ReleasedByPolicy
                or ReservationCycleStatus.ReleasedByCancel
                || cycle.SecondsRemaining == 0);
        if (!cycleActive && cycle is { CurrentStatus: ReservationCycleStatus.Active, SecondsRemaining: 0 })
        {
            cycleEnded = true;
        }

        var canInitiate = !awaitingReview && !retryLimit && cycleActive;
        var canRetry = !awaitingReview && !retryLimit && cycleEnded;
        var primary = awaitingReview || retryLimit
            ? "none"
            : canInitiate
                ? "pay"
                : canRetry
                    ? "retryAfterExpiry"
                    : "none";
        var reservation = cycleActive ? "held" : cycleEnded || retryLimit ? "ended" : "none";
        var presentation = awaitingReview
            ? "awaitingReview"
            : retryLimit
                ? "retryLimit"
                : payment is { Status: PaymentStatus.Failed } && cycleActive
                    ? "failedRetryable"
                    : cycleEnded
                        ? "expiredRetryable"
                        : "awaitingPayment";
        var references = checkout.Sellers
            .Select(x => x.OrderNumber)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        var payable = checkout.Sellers.Sum(x => x.PayableAmount);
        var currency = checkout.Sellers.Select(x => x.Currency).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?? payment?.Currency
            ?? "IRR";
        var lines = checkout.Sellers
            .SelectMany(x => x.Lines)
            .Take(6)
            .Select(x => new StorefrontPendingPaymentLineView(
                string.IsNullOrWhiteSpace(x.Title) ? "کالا" : x.Title,
                x.Quantity,
                x.MediaAssetId))
            .ToArray();
        return new StorefrontPendingPaymentItemView(
            checkout.CheckoutId,
            checkout.CartId,
            references.Count == 0 ? checkout.CheckoutId.ToString("N")[..12] : string.Join(" / ", references),
            payable,
            currency,
            lines,
            presentation,
            canInitiate,
            canRetry,
            awaitingReview,
            primary,
            reservation,
            cycle?.CurrentCycleNumber,
            cycleActive ? cycle!.SecondsRemaining : 0,
            serverNow,
            cycleActive || cycleEnded ? cycle?.ExpiresAt : null,
            maxCycles,
            retryRemaining,
            cycle?.SupplyStatus,
            payment?.PaymentId,
            retryLimit);
    }

    private static bool IsTerminalHidden(PaymentStatus status) =>
        status is PaymentStatus.Succeeded
            or PaymentStatus.Cancelled
            or PaymentStatus.RefundPending
            or PaymentStatus.Refunded
            or PaymentStatus.RefundFailed;
}
