using Tooba.Order.Contracts.Shipping;
using Tooba.Shipping.Application.Authoring.Models;

namespace Tooba.Shipping.Application.Carriers;

public static class ShippingCarrierOwnerScopeMatching
{
    private const int StoreOwner = 0;
    private const int MarketplaceOwner = 1;
    private const int SellerOwner = 2;

    public static bool Matches(ShippingAuthoringOwnerScope owner, OrderCommittedCarrierShipmentFacts facts)
        => owner.Kind switch
        {
            ShippingAuthoringOwnerKind.Store =>
                facts.OwnerKind == StoreOwner
                && string.Equals(facts.OwnerTenantKey, owner.TenantId, StringComparison.Ordinal),
            ShippingAuthoringOwnerKind.Marketplace =>
                facts.OwnerKind == MarketplaceOwner,
            ShippingAuthoringOwnerKind.Seller =>
                facts.OwnerKind == SellerOwner
                && facts.OwnerSellerPartyId == owner.SellerPartyId,
            _ => false,
        };
}
