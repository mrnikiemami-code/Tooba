using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application;
using Tooba.Order.Application;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure;

    /// <summary>
    /// SoT eligibility مرجوعی: Paid + Delivered + پنجره ۳۰ روزه + باقیمانده؛ تسویه فروشنده هرگز چک نمی‌شود.
    /// </summary>
public sealed class ReturnEligibilityEvaluator : IReturnEligibilityEvaluator
{
    /// <summary>پنجرهٔ مرجوعی از آخرین تحویل.</summary>
    public static readonly TimeSpan ReturnWindow = TimeSpan.FromDays(30);

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
        var eligibleUntil = lastDeliveredAt + ReturnWindow;
        var now = DateTimeOffset.UtcNow;
        if (now > eligibleUntil)
        {
            return WithLines(
                orderContext,
                ReturnEligibilityReasonCodes.WindowExpired,
                eligible: false,
                eligibleUntil,
                lastDeliveredAt,
                fulfillment,
                await GetAlreadyReturnedQuantitiesAsync(sellerOrderId, cancellationToken));
        }

        var alreadyReturned = await GetAlreadyReturnedQuantitiesAsync(sellerOrderId, cancellationToken);
        var lines = BuildLines(orderContext, fulfillment, alreadyReturned);
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

    private static ReturnEligibilityResult WithLines(
        OrderReturnContextSnapshot orderContext,
        string reasonCode,
        bool eligible,
        DateTimeOffset? eligibleUntil,
        DateTimeOffset? lastDeliveredAt,
        FulfillmentReturnEligibilitySnapshot fulfillment,
        IReadOnlyDictionary<Guid, int> alreadyReturned) =>
        new(
            orderContext.SellerOrderId,
            orderContext.CheckoutId,
            eligible,
            reasonCode,
            eligibleUntil,
            lastDeliveredAt,
            BuildLines(orderContext, fulfillment, alreadyReturned));

    private static IReadOnlyList<ReturnLineEligibility> BuildLines(
        OrderReturnContextSnapshot orderContext,
        FulfillmentReturnEligibilitySnapshot fulfillment,
        IReadOnlyDictionary<Guid, int> alreadyReturned)
    {
        return orderContext.Lines.Select(line =>
        {
            fulfillment.DeliveredQuantities.TryGetValue(line.OrderLineId, out var delivered);
            alreadyReturned.TryGetValue(line.OrderLineId, out var returned);
            var remaining = Math.Max(0, delivered - returned);
            return new ReturnLineEligibility(line.OrderLineId, delivered, returned, remaining);
        }).ToArray();
    }
}
