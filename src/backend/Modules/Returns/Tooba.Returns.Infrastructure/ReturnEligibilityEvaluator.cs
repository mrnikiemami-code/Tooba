using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application;
using Tooba.Order.Application;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure;

/// <summary>
/// SoT eligibility مرجوعی: Paid + Delivered + پنجرهٔ snapshot خط + باقیمانده؛ تسویه فروشنده هرگز چک نمی‌شود.
/// </summary>
public sealed class ReturnEligibilityEvaluator : IReturnEligibilityEvaluator
{
    /// <summary>پنجرهٔ پیش‌فرض وقتی snapshot خط در دسترس نباشد (legacy).</summary>
    public static readonly TimeSpan ReturnWindow = TimeSpan.FromDays(7);

    private readonly IOrderReturnReader _orders;
    private readonly IFulfillmentReturnReader _fulfillment;
    private readonly ReturnsDbContext _db;

    /// <summary>ارزیاب را به درز Order/Fulfillment و schema returns وصل می‌کند.</summary>
    public ReturnEligibilityEvaluator(
        IOrderReturnReader orders,
        IFulfillmentReturnReader fulfillment,
        ReturnsDbContext db)
    {
        _orders = orders;
        _fulfillment = fulfillment;
        _db = db;
    }

    /// <inheritdoc />
    public async Task<ReturnEligibilityResult> EvaluateAsync(Guid sellerOrderId, CancellationToken cancellationToken)
    {
        var orderContext = await _orders.GetReturnContextAsync(sellerOrderId, cancellationToken);
        if (orderContext is null)
        {
            return new ReturnEligibilityResult(
                sellerOrderId,
                Guid.Empty,
                false,
                ReturnEligibilityReasonCodes.OrderMissing,
                null,
                null,
                []);
        }

        if (!orderContext.IsPaid)
        {
            return Empty(
                orderContext,
                ReturnEligibilityReasonCodes.NotPaid,
                null,
                null);
        }

        var fulfillment = await _fulfillment.GetEligibilityAsync(sellerOrderId, cancellationToken);
        if (fulfillment is null)
        {
            return Empty(
                orderContext,
                ReturnEligibilityReasonCodes.FulfillmentMissing,
                null,
                null);
        }

        if (fulfillment.LastDeliveredAt is null)
        {
            return Empty(
                orderContext,
                ReturnEligibilityReasonCodes.NotDelivered,
                null,
                null);
        }

        var lastDeliveredAt = fulfillment.LastDeliveredAt.Value;
        var alreadyReturned = await GetAlreadyReturnedQuantitiesAsync(sellerOrderId, cancellationToken);
        var lines = BuildLines(orderContext, fulfillment, alreadyReturned, lastDeliveredAt, DateTimeOffset.UtcNow);
        var anyReturnablePolicy = orderContext.Lines.Any(x => x.IsReturnableSnapshot);
        if (!anyReturnablePolicy)
        {
            return new ReturnEligibilityResult(
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                false,
                ReturnEligibilityReasonCodes.NothingReturnable,
                null,
                lastDeliveredAt,
                lines);
        }

        var eligibleUntil = orderContext.Lines
            .Where(x => x.IsReturnableSnapshot)
            .Select(x => lastDeliveredAt.AddDays(x.ReturnWindowDaysSnapshot > 0 ? x.ReturnWindowDaysSnapshot : ReturnWindow.TotalDays))
            .DefaultIfEmpty(lastDeliveredAt + ReturnWindow)
            .Max();

        var now = DateTimeOffset.UtcNow;
        if (now > eligibleUntil)
        {
            return new ReturnEligibilityResult(
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                false,
                ReturnEligibilityReasonCodes.WindowExpired,
                eligibleUntil,
                lastDeliveredAt,
                lines);
        }

        if (lines.All(x => x.RemainingReturnableQuantity <= 0))
        {
            return new ReturnEligibilityResult(
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                false,
                ReturnEligibilityReasonCodes.NothingReturnable,
                eligibleUntil,
                lastDeliveredAt,
                lines);
        }

        return new ReturnEligibilityResult(
            orderContext.SellerOrderId,
            orderContext.CheckoutId,
            true,
            ReturnEligibilityReasonCodes.Eligible,
            eligibleUntil,
            lastDeliveredAt,
            lines);
    }

    /// <summary>تعداد مرجوعی فعال/تکمیل‌شده به ازای هر خط.</summary>
    public async Task<Dictionary<Guid, int>> GetAlreadyReturnedQuantitiesAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var activeStatuses = new[]
        {
            ReturnRequestStatus.Requested,
            ReturnRequestStatus.Approved,
            ReturnRequestStatus.RefundProcessing,
            ReturnRequestStatus.Completed,
        };
        var requestIds = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId && activeStatuses.Contains(x.Status))
            .Select(x => x.ReturnRequestId)
            .ToListAsync(cancellationToken);
        if (requestIds.Count == 0)
        {
            return [];
        }

        var items = await _db.ReturnItems.AsNoTracking()
            .Where(x => requestIds.Contains(x.ReturnRequestId))
            .ToListAsync(cancellationToken);
        return items
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
    }

    private static ReturnEligibilityResult Empty(
        OrderReturnContextSnapshot orderContext,
        string reasonCode,
        DateTimeOffset? eligibleUntil,
        DateTimeOffset? lastDeliveredAt) =>
        new(
            orderContext.SellerOrderId,
            orderContext.CheckoutId,
            false,
            reasonCode,
            eligibleUntil,
            lastDeliveredAt,
            []);

    private static IReadOnlyList<ReturnLineEligibility> BuildLines(
        OrderReturnContextSnapshot orderContext,
        FulfillmentReturnEligibilitySnapshot fulfillment,
        IReadOnlyDictionary<Guid, int> alreadyReturned,
        DateTimeOffset lastDeliveredAt,
        DateTimeOffset now)
    {
        return orderContext.Lines.Select(line =>
        {
            fulfillment.DeliveredQuantities.TryGetValue(line.OrderLineId, out var delivered);
            alreadyReturned.TryGetValue(line.OrderLineId, out var returned);
            if (!line.IsReturnableSnapshot)
            {
                return new ReturnLineEligibility(line.OrderLineId, delivered, returned, 0);
            }

            if (delivered <= 0)
            {
                // تعداد تحویل‌نشده ساعت مرجوعی ندارد.
                return new ReturnLineEligibility(line.OrderLineId, delivered, returned, 0);
            }

            DateTimeOffset lineDeliveredAt = lastDeliveredAt;
            if (fulfillment.LineDeliveredAt is not null
                && fulfillment.LineDeliveredAt.TryGetValue(line.OrderLineId, out var specific))
            {
                lineDeliveredAt = specific;
            }

            var windowDays = line.ReturnWindowDaysSnapshot > 0
                ? line.ReturnWindowDaysSnapshot
                : (int)ReturnWindow.TotalDays;
            var lineUntil = lineDeliveredAt.AddDays(windowDays);
            if (now > lineUntil)
            {
                return new ReturnLineEligibility(line.OrderLineId, delivered, returned, 0);
            }

            var remaining = Math.Max(0, delivered - returned);
            return new ReturnLineEligibility(line.OrderLineId, delivered, returned, remaining);
        }).ToArray();
    }
}
