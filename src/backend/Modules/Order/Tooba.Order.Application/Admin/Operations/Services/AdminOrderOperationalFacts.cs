using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Contracts.Shipping;

namespace Tooba.Order.Application.Admin.Operations.Services;

/// <summary>
/// TB-253-R2: single mapping seam from committed Order shipping facts + history to the operational
/// delivery group authority, shared by projection and the group workflow command.
/// </summary>
internal static class AdminOrderOperationalFacts
{
    public static MarketplaceOperationalDeliveryGroupAuthority.GroupFact ToFact(
        Guid checkoutId,
        OrderCommittedCarrierShipmentFacts facts,
        OrderShippingGroupHistoryRecord history) =>
        OperationalDeliveryGroupFactMapper.Map(
            checkoutId,
            facts,
            history,
            MarketplaceOperationalDeliveryGroupAuthority.BuildDestinationFingerprint(
                facts.ProvinceName,
                facts.CityName,
                facts.PostalCode,
                facts.RecipientAddress),
            facts.ExecutableLines.Count > 0 && !facts.IsCancelled && !facts.PaymentLocked,
            facts.IneligibilityReasonCode);
}
