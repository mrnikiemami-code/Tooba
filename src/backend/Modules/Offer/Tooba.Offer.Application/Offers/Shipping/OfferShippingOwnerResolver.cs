using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Offer.Application.Offers.Shipping;

/// <summary>
/// Trusted owner identity for the read-only eligible-method lookup.
/// </summary>
/// <remarks>
/// The owner is derived only from the real offer responsibility plus the trusted edition/tenant context and
/// the offer's own real seller identity. A body seller id is never a substitute for the aggregate owner.
/// Seller-owned methods resolve only in Marketplace; SingleStore is store-owned.
/// </remarks>
internal static class OfferShippingOwnerResolver
{
    internal static (ShippingCapturedQuoteOwnerKind Kind, string? TenantId, Guid? SellerPartyId)? Resolve(
        ICurrentEdition edition,
        ICurrentTenant tenant,
        OfferFulfillmentResponsibility? responsibility,
        Guid sellerPartyId)
    {
        switch (edition.Current?.Edition)
        {
            case ToobaEdition.SingleStore when responsibility == OfferFulfillmentResponsibility.Platform:
                var current = tenant.Current;
                var tenantId = current?.TenantId.Value;
                if (current is null || current.Status != TenantStatus.Active || string.IsNullOrWhiteSpace(tenantId))
                {
                    return null;
                }

                return (ShippingCapturedQuoteOwnerKind.Store, tenantId, null);

            case ToobaEdition.Marketplace when responsibility == OfferFulfillmentResponsibility.Platform:
                return tenant.Current is not null
                    ? null
                    : (ShippingCapturedQuoteOwnerKind.Marketplace, null, null);

            case ToobaEdition.Marketplace when responsibility == OfferFulfillmentResponsibility.Seller:
                return tenant.Current is not null || sellerPartyId == Guid.Empty
                    ? null
                    : (ShippingCapturedQuoteOwnerKind.Seller, null, sellerPartyId);

            default:
                return null;
        }
    }
}
