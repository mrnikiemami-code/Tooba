using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Shipping;

/// <summary>
/// TB-248-R1 / TB-249 / TB-249-R3: committed Order shipping-group facts for carrier shipment create and capabilities.
/// Fulfillment quantity eligibility is independent of package-completion completeness.
/// </summary>
public sealed class OrderCommittedCarrierShipmentFactsReader(
    OrderDbContext db,
    IFulfillmentAdminOperations fulfillment) : IOrderCommittedCarrierShipmentFactsReader
{
    /// <inheritdoc />
    public async Task<OrderCommittedCarrierShipmentFacts?> GetAsync(
        Guid checkoutId,
        Guid shippingGroupId,
        CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty || shippingGroupId == Guid.Empty)
        {
            return null;
        }

        var bundle = await LoadBundleAsync(checkoutId, cancellationToken);
        if (bundle is null)
        {
            return null;
        }

        var group = bundle.Groups.SingleOrDefault(x => x.ShippingGroupId == shippingGroupId);
        if (group is null)
        {
            return null;
        }

        IReadOnlyList<FulfillmentSnapshot> fulfillments;
        try
        {
            fulfillments = await fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        }
        catch
        {
            // Fail closed: missing fulfillment authority must not authorize carrier create.
            fulfillments = Array.Empty<FulfillmentSnapshot>();
        }

        return Map(group, bundle.Checkout, bundle.SellerOrders, fulfillments, bundle.LinePrices);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderCommittedCarrierShipmentFacts>> ListByCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty)
        {
            return Array.Empty<OrderCommittedCarrierShipmentFacts>();
        }

        var bundle = await LoadBundleAsync(checkoutId, cancellationToken);
        if (bundle is null)
        {
            return Array.Empty<OrderCommittedCarrierShipmentFacts>();
        }

        IReadOnlyList<FulfillmentSnapshot> fulfillments;
        try
        {
            fulfillments = await fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        }
        catch
        {
            fulfillments = Array.Empty<FulfillmentSnapshot>();
        }

        return bundle.Groups
            .OrderBy(x => x.ShippingGroupId)
            .Select(group => Map(group, bundle.Checkout, bundle.SellerOrders, fulfillments, bundle.LinePrices))
            .ToArray();
    }

    private async Task<CheckoutBundle?> LoadBundleAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var checkout = await db.Checkouts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (checkout is null)
        {
            return null;
        }

        var groups = await db.OrderShippingGroups
            .AsNoTracking()
            .Include(x => x.AcceptedEvidence)
            .Include(x => x.Lines)
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);

        var sellerOrders = await db.SellerOrders
            .AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .ToDictionaryAsync(x => x.SellerOrderId, cancellationToken);

        var sellerOrderIds = sellerOrders.Keys.ToArray();
        var linePrices = sellerOrderIds.Length == 0
            ? new Dictionary<Guid, decimal>()
            : await db.Lines
                .AsNoTracking()
                .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
                .Select(x => new { x.LineId, x.UnitPriceSnapshot })
                .ToDictionaryAsync(x => x.LineId, x => x.UnitPriceSnapshot, cancellationToken);

        return new CheckoutBundle(checkout, groups, sellerOrders, linePrices);
    }

    private static OrderCommittedCarrierShipmentFacts Map(
        Domain.Checkout.OrderShippingGroup group,
        Domain.Aggregates.CheckoutGroup checkout,
        IReadOnlyDictionary<Guid, Domain.Aggregates.SellerOrder> sellerOrders,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyDictionary<Guid, decimal> linePrices)
    {
        var evidence = group.AcceptedEvidence;
        var quote = evidence?.CarrierQuote;
        var hasQuote = quote is not null;
        var hasFullProvenance = quote is not null
            && !string.IsNullOrWhiteSpace(quote.OriginLocationCode)
            && !string.IsNullOrWhiteSpace(quote.DestinationLocationCode)
            && quote.WeightGrams > 0m
            && quote.LengthCentimeters > 0m
            && quote.WidthCentimeters > 0m
            && quote.HeightCentimeters > 0m;

        var transportMode = evidence is null
            ? "LegacyIncomplete"
            : hasQuote
                ? "CarrierApi"
                : "Manual";

        var owner = evidence?.Owner;
        var ownerPresent = owner is not null;
        var ownerKind = owner is null ? -1 : (int)owner.Kind;
        var ownerTenant = owner?.TenantKey;
        var ownerSeller = owner?.SellerPartyId;

        sellerOrders.TryGetValue(group.SellerOrderId, out var sellerOrder);
        var status = sellerOrder?.Status ?? SellerOrderStatus.Submitted;
        var statusName = status.ToString();
        var cancelled = status == SellerOrderStatus.Cancelled;
        var paymentLocked = status is SellerOrderStatus.PendingPayment
            or SellerOrderStatus.Submitted
            or SellerOrderStatus.ReservationRequested;

        var lineIds = group.Lines.Select(x => x.OrderLineId).OrderBy(x => x).ToArray();

        // Business / fulfillment constraints accumulate independently of package completeness.
        string? businessReason = null;
        if (cancelled)
        {
            businessReason = "cancelled";
        }
        else if (paymentLocked)
        {
            businessReason = "payment_locked";
        }
        else if (evidence is null)
        {
            businessReason = "legacy_incomplete";
        }
        else if (!ownerPresent)
        {
            businessReason = "owner_missing";
        }
        else if (!hasQuote)
        {
            businessReason = "not_carrier_api";
        }

        var (fulfillmentReason, executableLines, fulfillmentId, groupPackedTotal, packedLineValues) =
            ResolveFulfillmentEligibility(
                fulfillments,
                group.SellerOrderId,
                group.ShippingGroupId,
                lineIds,
                linePrices);

        if (businessReason is null && hasQuote && ownerPresent)
        {
            if (fulfillmentReason is not null)
            {
                businessReason = fulfillmentReason;
            }
        }
        else if (businessReason is null && (!hasQuote || !ownerPresent))
        {
            // Keep executable lines empty when not CarrierApi-ready; still fail closed on missing fulfillment for CarrierApi path only.
            executableLines = Array.Empty<OrderCommittedCarrierExecutableLine>();
            fulfillmentId = null;
            packedLineValues = Array.Empty<OrderCommittedCarrierLineValueFact>();
        }

        string? completenessReason = null;
        if (businessReason is null && hasQuote && !hasFullProvenance)
        {
            completenessReason = "package_completion_required";
        }

        var reason = businessReason ?? completenessReason;
        var canExecute = reason is null && hasQuote && ownerPresent && executableLines.Count > 0;

        return new OrderCommittedCarrierShipmentFacts(
            group.CheckoutId,
            group.ShippingGroupId,
            group.SellerPartyId,
            group.SellerOrderId,
            group.MethodCode,
            group.MethodLabel,
            transportMode,
            ownerKind,
            ownerTenant,
            ownerSeller,
            checkout.RecipientName,
            checkout.ContactMobile,
            checkout.PostalAddress,
            string.IsNullOrWhiteSpace(checkout.PostalCode) ? null : checkout.PostalCode.Trim(),
            checkout.ProvinceName,
            checkout.CityName,
            group.Currency,
            evidence?.PayableAtCheckoutAmount ?? group.ShippingChargeSnapshot,
            evidence?.CarrierCollectAmount ?? 0m,
            hasQuote,
            quote?.AccountId,
            quote?.ConfigurationVersion,
            quote?.ProviderCode,
            quote?.ServiceCode,
            quote?.QuoteInputIdentity,
            quote?.OriginLocationCode,
            quote?.DestinationLocationCode,
            quote?.WeightGrams,
            quote?.DeclaredValue,
            quote?.LengthCentimeters,
            quote?.WidthCentimeters,
            quote?.HeightCentimeters,
            statusName,
            cancelled,
            paymentLocked,
            canExecute,
            reason,
            lineIds,
            executableLines,
            fulfillmentId,
            groupPackedTotal,
            packedLineValues);
    }

    /// <summary>
    /// Computes remaining packed quantities using Fulfillment Created-shipment open allocations.
    /// Filters open allocations to this shipping group when shipment snapshots expose ShippingGroupId.
    /// </summary>
    internal static (
        string? Reason,
        IReadOnlyList<OrderCommittedCarrierExecutableLine> Lines,
        Guid? FulfillmentId,
        decimal? GroupPackedQuantityTotal,
        IReadOnlyList<OrderCommittedCarrierLineValueFact> PackedLineValues)
        ResolveFulfillmentEligibility(
            IReadOnlyList<FulfillmentSnapshot> fulfillments,
            Guid sellerOrderId,
            Guid shippingGroupId,
            IReadOnlyList<Guid> orderLineIds,
            IReadOnlyDictionary<Guid, decimal>? linePrices = null)
    {
        var prices = linePrices ?? new Dictionary<Guid, decimal>();
        var snapshot = fulfillments.FirstOrDefault(x => x.SellerOrderId == sellerOrderId);
        if (snapshot is null)
        {
            return ("fulfillment_unavailable", Array.Empty<OrderCommittedCarrierExecutableLine>(), null, null,
                Array.Empty<OrderCommittedCarrierLineValueFact>());
        }

        decimal? packedTotal = 0m;
        var packedValues = new List<OrderCommittedCarrierLineValueFact>(orderLineIds.Count);
        foreach (var lineId in orderLineIds)
        {
            var packedItem = snapshot.Items.FirstOrDefault(x => x.OrderLineId == lineId);
            if (packedItem is not null)
            {
                packedTotal += packedItem.QuantityPacked;
                prices.TryGetValue(lineId, out var unitPrice);
                packedValues.Add(new OrderCommittedCarrierLineValueFact(lineId, packedItem.QuantityPacked, unitPrice));
            }
        }

        if (snapshot.Status is FulfillmentOperationStatus.Delivered
            or FulfillmentOperationStatus.Cancelled
            or FulfillmentOperationStatus.Failed)
        {
            return ("fulfillment_exhausted", Array.Empty<OrderCommittedCarrierExecutableLine>(), snapshot.FulfillmentId,
                packedTotal, packedValues);
        }

        var openAllocated = snapshot.Shipments
            .Where(s => s.Status == ShipmentOperationStatus.Created)
            .Where(s => s.ShippingGroupId is null || s.ShippingGroupId == shippingGroupId)
            .SelectMany(s => s.Items)
            .GroupBy(i => i.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var lines = new List<OrderCommittedCarrierExecutableLine>();
        foreach (var lineId in orderLineIds)
        {
            var item = snapshot.Items.FirstOrDefault(x => x.OrderLineId == lineId);
            if (item is null)
            {
                return ("fulfillment_unavailable", Array.Empty<OrderCommittedCarrierExecutableLine>(), snapshot.FulfillmentId,
                    packedTotal, packedValues);
            }

            prices.TryGetValue(lineId, out var unitPrice);
            var remaining = item.QuantityPacked - openAllocated.GetValueOrDefault(lineId);
            if (remaining > 0m)
            {
                lines.Add(new OrderCommittedCarrierExecutableLine(lineId, remaining, unitPrice, item.QuantityPacked));
            }
        }

        if (lines.Count == 0)
        {
            return ("fulfillment_exhausted", Array.Empty<OrderCommittedCarrierExecutableLine>(), snapshot.FulfillmentId,
                packedTotal, packedValues);
        }

        return (null, lines, snapshot.FulfillmentId, packedTotal, packedValues);
    }

    private sealed record CheckoutBundle(
        Domain.Aggregates.CheckoutGroup Checkout,
        IReadOnlyList<Domain.Checkout.OrderShippingGroup> Groups,
        IReadOnlyDictionary<Guid, Domain.Aggregates.SellerOrder> SellerOrders,
        IReadOnlyDictionary<Guid, decimal> LinePrices);
}
