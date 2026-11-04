using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Contracts.Shipping;

namespace Tooba.Order.Application.Admin.Operations.Policies;

/// <summary>
/// Application edge: trusted Post/Tipax metadata from Order facts with Fulfillment snapshot fallback.
/// </summary>
public static class AcceptedGroupTrustedProviderMetadataBuilder
{
    /// <summary>Builds trusted provider metadata JSON for Manual/Free parcel create.</summary>
    public static string? Build(
        string? fulfillmentRegistryMethodCode,
        OrderCommittedCarrierShipmentFacts facts,
        FulfillmentSnapshot fulfillment)
    {
        try
        {
            return AcceptedGroupTrustedProviderMetadata.BuildFromFactsWithFallback(
                fulfillmentRegistryMethodCode,
                facts,
                fulfillment.RecipientName,
                fulfillment.ContactMobile,
                fulfillment.PostalAddress,
                fulfillment.PostalCode,
                fulfillment.ProvinceName,
                fulfillment.CityName);
        }
        catch (AcceptedGroupParcelCreateException ex)
        {
            throw new AdminOrderOperationsException(ex.Code);
        }
    }
}
