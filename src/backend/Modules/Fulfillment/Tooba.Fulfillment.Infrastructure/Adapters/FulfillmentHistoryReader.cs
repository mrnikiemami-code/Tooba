using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts.History;

namespace Tooba.Fulfillment.Infrastructure.Adapters;

/// <summary>
/// Exposes the owning Fulfillment directory as the contracts-only history reader.
/// </summary>
internal sealed class FulfillmentHistoryReader(IFulfillmentDirectory directory) : IFulfillmentHistoryReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<FulfillmentHistoryRecord>> ListFulfillmentsForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var fulfillments = await directory.ListForCheckoutAsync(checkoutId, cancellationToken);
        return fulfillments.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsolidatedPackageHistoryRecord>> ListConsolidatedPackagesForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var packages = await directory.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
        return packages.Select(Map).ToList();
    }

    private static FulfillmentHistoryRecord Map(FulfillmentSnapshot snapshot) =>
        new(
            snapshot.FulfillmentId,
            snapshot.SellerOrderId,
            snapshot.SellerPartyId,
            snapshot.Status.ToString(),
            snapshot.CreatedAt,
            snapshot.UpdatedAt,
            snapshot.Items
                .Select(x => new FulfillmentHistoryItem(x.OrderLineId, x.QuantityOrdered, x.QuantityPacked))
                .ToList(),
            snapshot.Shipments.Select(Map).ToList());

    private static FulfillmentHistoryShipment Map(ShipmentSnapshot snapshot) =>
        new(
            snapshot.ShipmentId,
            snapshot.Status.ToString(),
            snapshot.CarrierDisplayName,
            snapshot.ShippingMethodLabel,
            snapshot.TrackingReference,
            snapshot.PreviousTrackingReference,
            snapshot.CreatedAt,
            snapshot.DispatchedAt,
            snapshot.DeliveredAt,
            snapshot.Items
                .Select(x => new FulfillmentHistoryShipmentLine(x.OrderLineId, x.Quantity))
                .ToList());

    private static ConsolidatedPackageHistoryRecord Map(ConsolidatedPackageSnapshot snapshot) =>
        new(
            snapshot.ConsolidatedPackageId,
            snapshot.PackageNumber,
            snapshot.CreatedBy,
            snapshot.CreatedAt,
            snapshot.DispatchedAt,
            snapshot.DeliveredAt,
            snapshot.CancelledAt,
            snapshot.Members
                .Select(x => new ConsolidatedPackageHistoryMember(x.ShipmentId, x.SellerPartyId, x.JoinedAt))
                .ToList());
}
