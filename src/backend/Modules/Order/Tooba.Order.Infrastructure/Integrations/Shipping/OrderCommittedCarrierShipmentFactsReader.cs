using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Shipping;

/// <summary>
/// TB-248-R1 / TB-249: committed Order shipping-group facts for carrier shipment create and capabilities.
/// </summary>
public sealed class OrderCommittedCarrierShipmentFactsReader(OrderDbContext db) : IOrderCommittedCarrierShipmentFactsReader
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
        return group is null ? null : Map(group, bundle.Checkout, bundle.SellerOrders);
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

        return bundle.Groups
            .OrderBy(x => x.ShippingGroupId)
            .Select(group => Map(group, bundle.Checkout, bundle.SellerOrders))
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

        return new CheckoutBundle(checkout, groups, sellerOrders);
    }

    private static OrderCommittedCarrierShipmentFacts Map(
        Domain.Checkout.OrderShippingGroup group,
        Domain.Aggregates.CheckoutGroup checkout,
        IReadOnlyDictionary<Guid, Domain.Aggregates.SellerOrder> sellerOrders)
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

        string? reason = null;
        if (cancelled)
        {
            reason = "cancelled";
        }
        else if (paymentLocked)
        {
            reason = "payment_locked";
        }
        else if (evidence is null)
        {
            reason = "legacy_incomplete";
        }
        else if (!ownerPresent)
        {
            reason = "owner_missing";
        }
        else if (!hasQuote)
        {
            reason = "not_carrier_api";
        }
        else if (!hasFullProvenance)
        {
            reason = hasQuote ? "package_completion_required" : "package_incomplete";
        }

        // Payment lock and cancelled are the only order-status blocks (see AdminFulfillmentCapabilityProjector).
        // Other seller-order statuses are not treated as automatically shippable; Shipping readiness owns carrier gates.
        var canExecute = reason is null && hasQuote && hasFullProvenance && ownerPresent;

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
            group.Lines.Select(x => x.OrderLineId).OrderBy(x => x).ToArray());
    }

    private sealed record CheckoutBundle(
        Domain.Aggregates.CheckoutGroup Checkout,
        IReadOnlyList<Domain.Checkout.OrderShippingGroup> Groups,
        IReadOnlyDictionary<Guid, Domain.Aggregates.SellerOrder> SellerOrders);
}
