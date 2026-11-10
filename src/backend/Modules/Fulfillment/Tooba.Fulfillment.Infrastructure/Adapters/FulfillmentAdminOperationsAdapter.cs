using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Shipping;
using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;

namespace Tooba.Fulfillment.Infrastructure.Adapters;

/// <summary>
/// Contract-facing adapter over <see cref="IFulfillmentDirectory"/> so admin order callers never
/// reference Fulfillment Application/Domain types. Owner-side mapping keeps the wire shape stable.
/// Expected failures originate as <see cref="Tooba.BuildingBlocks.ContractOperationException"/> at
/// owning Domain/Directory; this adapter does not parse Message or promote InvalidOperationException.
/// </summary>
internal sealed class FulfillmentAdminOperationsAdapter(
    IFulfillmentDirectory directory,
    IFulfillmentShippedQuantityReader shipped,
    ShippingMethodsOptions shippingMethods) : IFulfillmentAdminOperations
{
    public async Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken) =>
        Map(await directory.GetAsync(fulfillmentId, cancellationToken));

    public async Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FulfillmentSnapshot> list =
            (await directory.ListForCheckoutAsync(checkoutId, cancellationToken)).Select(MapRequired).ToList();
        return list;
    }

    public async Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ConsolidatedPackageSnapshot> list =
            (await directory.GetPackagesForCheckoutAsync(checkoutId, cancellationToken)).Select(Map).ToList();
        return list;
    }

    public async Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ActivePackageMembershipSnapshot> list =
            (await directory.GetActiveMembershipByShipmentIdsAsync(shipmentIds, cancellationToken))
            .Select(x => new ActivePackageMembershipSnapshot(
                x.ShipmentId,
                x.ConsolidatedPackageId,
                x.PackageNumber,
                (ConsolidatedPackageOperationStatus)x.PackageStatus))
            .ToList();
        return list;
    }

    public IReadOnlyList<ShippingMethodOption> ListEnabledShippingMethods() =>
        ShippingMethodRegistry.Enabled(shippingMethods)
            .Select(x => new ShippingMethodOption(x.Code, x.LabelFa, x.ProviderKind))
            .ToList();

    public string ResolveShippingMethodLabel(string? code, string? fallbackDisplayName) =>
        ShippingMethodRegistry.ResolveLabel(code, fallbackDisplayName);

    public async Task<FulfillmentSnapshot> MarkProcessingAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.MarkProcessingAsync(fulfillmentId, actorUserId, cancellationToken));

    public async Task<FulfillmentSnapshot> ProcessSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.ProcessSelectionsAsync(
            fulfillmentId, actorUserId, Map(selections), cancellationToken));

    public async Task<FulfillmentSnapshot> UnprocessSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.UnprocessSelectionsAsync(
            fulfillmentId, actorUserId, Map(selections), cancellationToken));

    public async Task<FulfillmentSnapshot> PackSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.PackSelectionsAsync(
            fulfillmentId, actorUserId, Map(selections), cancellationToken));

    public async Task<FulfillmentSnapshot> UnpackSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.UnpackSelectionsAsync(
            fulfillmentId, actorUserId, Map(selections), cancellationToken));

    public async Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> items,
        string? shippingMethodCode,
        string? providerMetadataJson,
        CancellationToken cancellationToken,
        Guid? shippingGroupId = null,
        string? shippingDispatchKey = null)
    {
        var created = await directory.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            carrierDisplayName,
            items.Select(x => new AppModels.ShipmentLineCommand(x.OrderLineId, x.Quantity)).ToList(),
            cancellationToken,
            shippingMethodCode,
            providerMetadataJson,
            shippingGroupId,
            shippingDispatchKey);
        return new FulfillmentShipmentCreateResult(MapRequired(created.Snapshot), created.CreatedShipmentId);
    }

    public async Task<FulfillmentSnapshot> CancelShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.CancelShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken));

    public async Task<FulfillmentSnapshot> AssignTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.AssignTrackingAsync(
            fulfillmentId, shipmentId, actorUserId, trackingReference, cancellationToken));

    public async Task<FulfillmentSnapshot> CorrectTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.CorrectTrackingAsync(
            fulfillmentId, shipmentId, actorUserId, trackingReference, cancellationToken));

    public async Task<FulfillmentSnapshot> AssignAutomaticTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.AssignAutomaticTrackingAsync(
            fulfillmentId, shipmentId, actorUserId, cancellationToken));

    public async Task<FulfillmentSnapshot> DispatchShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.DispatchShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken));

    public async Task<FulfillmentSnapshot> DeliverShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.DeliverShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken));

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> RecordNonDeliveryAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string reason,
        string? note,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.RecordNonDeliveryAsync(
            fulfillmentId, shipmentId, actorUserId, reason, note, cancellationToken));

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> StartReturnAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.StartReturnAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken));

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> RecordReturnReceiptAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        IReadOnlyList<ReturnReceiptLineCommand> items,
        string? note,
        CancellationToken cancellationToken) =>
        MapRequired(await directory.RecordReturnReceiptAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            items.Select(x => new AppModels.ReturnReceiptLineCommand(
                x.OrderLineId,
                x.Quantity,
                (Domain.ValueObjects.ReturnReceiptCondition)x.Condition)).ToList(),
            note,
            cancellationToken));

    /// <inheritdoc />
    public async Task<FulfillmentShipmentCreateResult> CreateResendShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        Guid originalShipmentId,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken,
        string? shippingDispatchKey = null)
    {
        var created = await directory.CreateResendShipmentAsync(
            fulfillmentId,
            actorUserId,
            originalShipmentId,
            items.Select(x => new AppModels.ShipmentLineCommand(x.OrderLineId, x.Quantity)).ToList(),
            cancellationToken,
            shippingDispatchKey);
        return new FulfillmentShipmentCreateResult(MapRequired(created.Snapshot), created.CreatedShipmentId);
    }

    /// <inheritdoc />
    public async Task<PostDispatchIntentResult> ReconcilePostDispatchIntentAsync(
        PostDispatchIntent intent,
        CancellationToken cancellationToken)
    {
        var recorded = await directory.ReconcilePostDispatchIntentAsync(
            intent.Kind switch
            {
                PostDispatchIntentKind.RecordNonDelivery =>
                    AppModels.PostDispatchIntent.NonDelivery(intent.FulfillmentId, intent.ShipmentId, intent.Reason, intent.Note),
                PostDispatchIntentKind.StartReturn =>
                    AppModels.PostDispatchIntent.ReturnStart(intent.FulfillmentId, intent.ShipmentId),
                PostDispatchIntentKind.RecordReturnReceipt =>
                    AppModels.PostDispatchIntent.Receipt(
                        intent.FulfillmentId,
                        intent.ShipmentId,
                        (intent.ReceiptItems ?? [])
                            .Select(x => new AppModels.ReturnReceiptLineCommand(
                                x.OrderLineId,
                                x.Quantity,
                                (Domain.ValueObjects.ReturnReceiptCondition)x.Condition))
                            .ToList()),
                _ => AppModels.PostDispatchIntent.Resend(
                    intent.FulfillmentId,
                    intent.ShipmentId,
                    intent.OccurrenceKey,
                    (intent.ResendItems ?? [])
                        .Select(x => new AppModels.ShipmentLineCommand(x.OrderLineId, x.Quantity))
                        .ToList()),
            },
            cancellationToken);
        return new PostDispatchIntentResult(
            recorded.State switch
            {
                AppModels.PostDispatchIntentRecord.Conflict => PostDispatchIntentState.Conflict,
                AppModels.PostDispatchIntentRecord.Recorded => PostDispatchIntentState.Recorded,
                AppModels.PostDispatchIntentRecord.ResolvedCancelled => PostDispatchIntentState.ResolvedCancelled,
                _ => PostDispatchIntentState.NotRecorded,
            },
            Map(recorded.Snapshot),
            recorded.RecordedShipmentId,
            recorded.ConflictCode);
    }

    public async Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> shipmentIds,
        string? shippingMethodCode,
        string? trackingReference,
        string? note,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.CreateConsolidatedPackageAsync(
            checkoutId, shipmentIds, shippingMethodCode, trackingReference, note, actorUserId, cancellationToken));

    public async Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.CancelConsolidatedPackageAsync(consolidatedPackageId, actorUserId, cancellationToken));

    public async Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(
        Guid consolidatedPackageId,
        string trackingReference,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.AssignConsolidatedPackageTrackingAsync(
            consolidatedPackageId, trackingReference, actorUserId, cancellationToken));

    public async Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.DispatchConsolidatedPackageAsync(consolidatedPackageId, actorUserId, cancellationToken));

    public async Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.DeliverConsolidatedPackageAsync(consolidatedPackageId, actorUserId, cancellationToken));

    public Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        directory.AbortForCheckoutCancelAsync(checkoutId, cancellationToken);

    public Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        directory.ReactivateAfterOrderRestoreAsync(checkoutId, cancellationToken);

    public Task EnsureCreatedForPaidCheckoutAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken) =>
        directory.EnsureCreatedForPaidCheckoutAsync(checkoutId, sellerOrderIds, cancellationToken);

    public Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        directory.VoidUnstartedForCheckoutAsync(checkoutId, cancellationToken);

    public Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        directory.RebindActiveReservationsFromOrderAsync(checkoutId, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken) =>
        shipped.GetShippedByOrderLineIdsForCheckoutsAsync(checkoutIds, cancellationToken);

    private static IReadOnlyList<AppModels.FulfillmentSelectionCommand> Map(
        IReadOnlyList<FulfillmentSelectionCommand> selections) =>
        selections.Select(x => new AppModels.FulfillmentSelectionCommand(x.OrderLineId, x.Quantity)).ToList();

    private static FulfillmentSnapshot MapRequired(AppModels.FulfillmentSnapshot snapshot) =>
        Map(snapshot)!;

    private static FulfillmentSnapshot? Map(AppModels.FulfillmentSnapshot? snapshot) =>
        snapshot is null
            ? null
            : new FulfillmentSnapshot(
                snapshot.FulfillmentId,
                snapshot.SellerOrderId,
                snapshot.CheckoutId,
                snapshot.SellerPartyId,
                (FulfillmentOperationStatus)snapshot.Status,
                snapshot.RecipientName,
                snapshot.ContactMobile,
                snapshot.ProvinceName,
                snapshot.CityName,
                snapshot.PostalAddress,
                snapshot.PostalCode,
                snapshot.ShippingMethodCode,
                snapshot.ShippingMethodLabel,
                snapshot.Items
                    .Select(x => new FulfillmentItemSnapshot(
                        x.FulfillmentItemId,
                        x.OrderLineId,
                        x.QuantityOrdered,
                        x.QuantityShipped,
                        x.ReservationId,
                        x.QuantityPacked,
                        x.QuantityProcessing,
                        x.ShippingGroupId,
                        x.ReturnedUsableQuantity,
                        x.ResendCommittedQuantity,
                        x.ResendableQuantity,
                        x.DeliveredQuantity))
                    .ToList(),
                snapshot.Shipments.Select(Map).ToList(),
                snapshot.CreatedAt,
                snapshot.UpdatedAt,
                snapshot.PreferredTrackingReference);

    private static ShipmentSnapshot Map(AppModels.ShipmentSnapshot shipment) =>
        new(
            shipment.ShipmentId,
            (ShipmentOperationStatus)shipment.Status,
            shipment.CarrierDisplayName,
            shipment.TrackingReference,
            shipment.DispatchedAt,
            shipment.DeliveredAt,
            shipment.Items
                .Select(x => new ShipmentLineSnapshot(x.OrderLineId, x.Quantity, x.ResendableQuantity))
                .ToList(),
            shipment.CreatedAt,
            shipment.ShippingMethodCode,
            shipment.ShippingMethodLabel,
            shipment.ProviderMetadataJson,
            shipment.ProviderMetadataVersion,
            shipment.PreviousTrackingReference,
            shipment.ShippingGroupId,
            shipment.ShippingDispatchKey,
            shipment.ResendOfShipmentId,
            shipment.NonDeliveryReason,
            shipment.NonDeliveryNote,
            shipment.NonDeliveredAt,
            shipment.ReturnStartedAt,
            shipment.ReturnedAt,
            shipment.ReturnReceipt is null
                ? null
                : new ReturnReceiptSnapshot(
                    shipment.ReturnReceipt.ShipmentReturnReceiptId,
                    shipment.ReturnReceipt.ReceivedAt,
                    shipment.ReturnReceipt.ReceivedByUserId,
                    shipment.ReturnReceipt.Note,
                    shipment.ReturnReceipt.Items
                        .Select(x => new ReturnReceiptLineSnapshot(
                            x.OrderLineId,
                            x.Quantity,
                            (ReturnReceiptCondition)x.Condition))
                        .ToList()));

    private static ConsolidatedPackageSnapshot Map(AppModels.ConsolidatedPackageSnapshot package) =>
        new(
            package.ConsolidatedPackageId,
            package.PackageNumber,
            package.CheckoutId,
            (ConsolidatedPackageOperationStatus)package.Status,
            package.ShippingMethodCode,
            package.ShippingMethodLabel,
            package.TrackingReference,
            package.Note,
            package.CreatedBy,
            package.CreatedAt,
            package.UpdatedAt,
            package.DispatchedAt,
            package.DeliveredAt,
            package.CancelledAt,
            package.Members
                .Select(x => new ConsolidatedPackageMemberSnapshot(
                    x.ConsolidatedPackageMemberId,
                    x.ShipmentId,
                    x.SellerPartyId,
                    x.FulfillmentId,
                    x.JoinedAt,
                    x.ReleasedAt))
                .ToList());
}
