using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Contracts.Shipping;

namespace Tooba.Order.Application.Admin.Operations.Policies;

/// <summary>
/// TB-253-R1: one shared mapping from committed facts + history evidence to operational authority input.
/// Display fallbacks must not invent executable receiving identity.
/// </summary>
public static class OperationalDeliveryGroupFactMapper
{
    /// <summary>
    /// Maps one accepted group. Receiving date/ticks/timezone come only from accepted evidence
    /// (not MinimumDeliveryDate). Missing evidence leaves receiving fields null → fail closed on merge.
    /// </summary>
    public static MarketplaceOperationalDeliveryGroupAuthority.GroupFact Map(
        Guid checkoutId,
        OrderCommittedCarrierShipmentFacts? facts,
        OrderShippingGroupHistoryRecord history,
        string destinationFingerprint,
        bool createParcelEligible,
        string? createParcelBlockReasonFa)
    {
        var evidence = history.Evidence;
        var ownerKind = MarketplaceOperationalDeliveryGroupAuthority.ParseOwnerKind(
            evidence?.OwnerKind,
            facts?.OwnerKind);

        var transportMode = facts?.TransportMode
            ?? (evidence?.CarrierQuote is not null ? "CarrierApi" : "LegacyIncomplete");

        // Executable identity: only accepted receiving evidence. Never MinimumDeliveryDate.
        DateOnly? receivingDate = evidence?.ReceivingDate;
        long? open = evidence?.ReceivingOpenTicks;
        long? close = evidence?.ReceivingCloseTicks;
        string? tz = evidence?.ReceivingTimeZone;

        var fee = facts?.PayableAtCheckoutAmount
            ?? evidence?.PayableAtCheckoutAmount
            ?? history.ShippingChargeSnapshot;

        return new MarketplaceOperationalDeliveryGroupAuthority.GroupFact(
            history.ShippingGroupId,
            checkoutId,
            history.SellerOrderId,
            history.SellerPartyId,
            facts?.MethodCode ?? history.MethodCode,
            facts?.ServiceCode ?? history.ServiceCode,
            facts?.OptionCode ?? history.OptionCode,
            facts?.MethodLabel ?? history.MethodLabel,
            facts?.Currency ?? history.Currency,
            fee,
            ownerKind,
            evidence?.OwnerTenantKey ?? facts?.OwnerTenantKey,
            evidence?.OwnerSellerPartyId ?? facts?.OwnerSellerPartyId,
            receivingDate,
            open,
            close,
            tz,
            transportMode,
            facts?.OriginLocationCode,
            facts?.OriginPostalAddress,
            facts?.CarrierAccountId ?? evidence?.CarrierQuote?.AccountId,
            facts?.ProviderCode ?? evidence?.CarrierQuote?.ProviderCode,
            facts?.ProviderServiceCode ?? evidence?.CarrierQuote?.ServiceCode,
            facts?.ConfigurationVersion ?? evidence?.CarrierQuote?.ConfigurationVersion,
            destinationFingerprint,
            history.OrderLineIds,
            createParcelEligible,
            createParcelBlockReasonFa);
    }
}
