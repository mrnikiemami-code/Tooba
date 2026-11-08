using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;

namespace Tooba.Order.Application.Admin.Operations.Services;

/// <summary>
/// TB-253-R2: one coherent Marketplace operational group workflow.
/// <para>
/// Workers select reviewed remaining quantities and/or existing eligible parcels per constituent
/// group; the server creates/reuses exact group-bound parcels and then their common package.
/// Allocation identity is stable per checkout/group/line-quantity so retries, lost responses and
/// concurrent twins never duplicate parcels or packages.
/// </para>
/// <para>
/// The workflow owns its authority check so the caller cannot bypass it by projecting a ready action:
/// only <c>order.handle</c> or <c>fulfillment.manage</c> may run it.
/// </para>
/// </summary>
public sealed class MarketplaceOperationalGroupWorkflow(
    IFulfillmentAdminOperations fulfillment,
    IOrderCommittedCarrierShipmentFactsReader committedShippingGroups,
    IOrderShippingGroupHistoryReader shippingGroupHistory,
    IOrderAdminEffectiveAccessReader access)
{
    private readonly IFulfillmentAdminOperations _fulfillment = fulfillment;
    private readonly IOrderCommittedCarrierShipmentFactsReader _committedShippingGroups = committedShippingGroups;
    private readonly IOrderShippingGroupHistoryReader _shippingGroupHistory = shippingGroupHistory;
    private readonly IOrderAdminEffectiveAccessReader _access = access;

    public async Task<object> PrepareAsync(
        Guid checkoutId,
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var requestedIds = (request.OperationalGroupShippingGroupIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
        if (requestedIds.Length < 2)
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.ConsolidateRequiresMultiSeller);
        }

        var effective = await _access.GetAsync(actorUserId, cancellationToken);
        if (!AdminOrderOperationsOrchestrator.Has(effective, "order.handle")
            && !AdminOrderOperationsOrchestrator.Has(effective, "fulfillment.manage"))
        {
            throw new AdminOrderOperationsException("order.operation.denied");
        }

        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var facts = await _committedShippingGroups.ListByCheckoutAsync(checkoutId, cancellationToken);
        var history = await _shippingGroupHistory.GetForCheckoutAsync(checkoutId, cancellationToken);
        var factsById = facts.ToDictionary(x => x.ShippingGroupId);
        var historyById = history.ToDictionary(x => x.ShippingGroupId);
        if (requestedIds.Any(id => !factsById.ContainsKey(id) || !historyById.ContainsKey(id)))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign);
        }

        var operationalFacts = new List<MarketplaceOperationalDeliveryGroupAuthority.GroupFact>();
        foreach (var row in facts)
        {
            if (historyById.TryGetValue(row.ShippingGroupId, out var hist))
            {
                operationalFacts.Add(AdminOrderOperationalFacts.ToFact(checkoutId, row, hist));
            }
        }

        var requestedSet = requestedIds.OrderBy(x => x).ToArray();
        var match = MarketplaceOperationalDeliveryGroupAuthority
            .Project(operationalFacts)
            .FirstOrDefault(g => g.Members
                .Select(m => m.ShippingGroupId)
                .OrderBy(x => x)
                .SequenceEqual(requestedSet));
        if (match is null)
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign);
        }

        if (match.Members.Any(m => m.CreateParcelBlockReasonFa is not null
                && !m.CreateParcelEligible
                && !IsReuseCandidateExists(fulfillments, m.ShippingGroupId)))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.ConsolidateIncompatible);
        }

        if (factsById.Values.Any(f =>
                requestedIds.Contains(f.ShippingGroupId)
                && (f.IsCancelled || f.PaymentLocked)))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.ConsolidateIncompatible);
        }

        string acceptedMethod;
        try
        {
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed(match.Members);
            acceptedMethod = MarketplaceOperationalDeliveryGroupAuthority.DeriveAcceptedMethodCode(
                match.Members,
                request.ShippingMethodCode);
        }
        catch (MarketplaceOperationalDeliveryGroupException ex)
        {
            throw new AdminOrderOperationsException(ex.Code);
        }

        var requestedReuse = (request.ReuseShipmentIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
        var activeMemberships = requestedReuse.Length == 0
            ? new Dictionary<Guid, ActivePackageMembershipSnapshot>()
            : (await _fulfillment.GetActiveMembershipByShipmentIdsAsync(requestedReuse, cancellationToken))
                .ToDictionary(x => x.ShipmentId);

        var shipmentIds = new List<Guid>(requestedIds.Length * 2);
        var parcelResults = new List<object>(requestedIds.Length);
        foreach (var member in match.Members)
        {
            var snapshot = fulfillments.FirstOrDefault(x => x.SellerOrderId == member.SellerOrderId)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");

            var memberReuse = requestedReuse
                .Where(id => snapshot.Shipments.Any(s => s.ShipmentId == id))
                .ToArray();
            foreach (var reuseId in memberReuse)
            {
                var shipment = snapshot.Shipments.First(s => s.ShipmentId == reuseId);
                if (shipment.ShippingGroupId != member.ShippingGroupId
                    || shipment.Status != ShipmentOperationStatus.Created
                    || shipment.DispatchedAt is not null
                    || string.IsNullOrWhiteSpace(shipment.ShippingMethodCode)
                    || activeMemberships.ContainsKey(reuseId))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowShipmentForeign);
                }
            }

            var memberSelections = (request.Selections ?? Array.Empty<AdminOrderLineSelection>())
                .Where(sel => member.OrderLineIds.Contains(sel.OrderLineId))
                .ToArray();

            // Retry / lost response / concurrent twin: the reviewed quantity is already allocated to this
            // group's recorded parcel, so nothing remains packed. Reuse that exact parcel only when its
            // recorded line quantities equal the reviewed ones; otherwise it is a different allocation.
            var recordedForMember = RecordedMemberParcels(snapshot, member.ShippingGroupId);
            if (memberReuse.Length == 0
                && memberSelections.Length > 0
                && recordedForMember.Length > 0
                && RecordedQuantitiesMatch(snapshot, recordedForMember, memberSelections))
            {
                shipmentIds.AddRange(recordedForMember);
                parcelResults.Add(new
                {
                    shippingGroupId = member.ShippingGroupId,
                    shipmentIds = recordedForMember,
                    created = false,
                    replayed = true,
                });
                continue;
            }

            if (memberReuse.Length == 0 && memberSelections.Length == 0)
            {
                if (recordedForMember.Length == 0)
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired);
                }

                shipmentIds.AddRange(recordedForMember);
                parcelResults.Add(new
                {
                    shippingGroupId = member.ShippingGroupId,
                    shipmentIds = recordedForMember,
                    created = false,
                    replayed = true,
                });
                continue;
            }

            shipmentIds.AddRange(memberReuse);
            if (memberSelections.Length == 0)
            {
                parcelResults.Add(new { shippingGroupId = member.ShippingGroupId, shipmentIds = memberReuse, created = false });
                continue;
            }

            var bound = AcceptedGroupShipmentCreateBinder.Resolve(
                snapshot,
                new AdminOrderOperationRequest(
                    request.Code,
                    member.SellerOrderId,
                    snapshot.FulfillmentId,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    memberSelections,
                    null,
                    null,
                    null,
                    null,
                    member.ShippingGroupId),
                facts);

            if (bound.Lines.Count == 0)
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired);
            }

            var dispatchKey = MarketplaceOperationalDeliveryGroupAuthority.BuildMemberParcelDispatchKey(
                checkoutId,
                member.ShippingGroupId,
                bound.Lines
                    .Select(x => new MarketplaceOperationalDeliveryGroupAuthority.GroupLineQuantity(x.OrderLineId, x.Quantity))
                    .ToArray());
            var created = await _fulfillment.CreateShipmentAsync(
                snapshot.FulfillmentId,
                actorUserId,
                bound.CarrierDisplayName,
                bound.Lines,
                bound.ShippingMethodCode,
                providerMetadataJson: bound.TrustedProviderMetadataJson,
                cancellationToken,
                bound.ShippingGroupId,
                dispatchKey);
            if (!shipmentIds.Contains(created.CreatedShipmentId))
            {
                shipmentIds.Add(created.CreatedShipmentId);
            }

            parcelResults.Add(new
            {
                shippingGroupId = member.ShippingGroupId,
                shipmentIds = new[] { created.CreatedShipmentId }.Concat(memberReuse).ToArray(),
                created = true,
            });
        }

        var distinctShipments = shipmentIds.Distinct().ToArray();
        if (distinctShipments.Length < 2)
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.ConsolidateRequiresMultiSeller);
        }

        ConsolidatedPackageSnapshot package;
        var replayed = false;
        try
        {
            package = await _fulfillment.CreateConsolidatedPackageAsync(
                checkoutId,
                distinctShipments,
                acceptedMethod,
                trackingReference: null,
                note: null,
                actorUserId,
                cancellationToken);
        }
        catch (ContractOperationException ex) when (
            ex.Code is "fulfillment.package.shipment_already_member"
                or "fulfillment.package.duplicate_shipment")
        {
            // Retry/response-loss/concurrent twin: the common package already exists for this exact membership.
            package = await FindMatchingActivePackageAsync(checkoutId, distinctShipments, cancellationToken)
                ?? throw new AdminOrderOperationsException(ex.Code);
            replayed = true;
        }

        return new
        {
            ok = true,
            code = "prepare_operational_group",
            operationalGroupKey = match.OperationalGroupKey,
            operationLocator = MarketplaceOperationalDeliveryGroupAuthority.BuildWorkflowOperationLocator(
                checkoutId,
                requestedSet),
            consolidatedPackageId = package.ConsolidatedPackageId,
            packageNumber = package.PackageNumber,
            shippingMethodCode = package.ShippingMethodCode,
            shipmentIds = distinctShipments,
            members = parcelResults,
            replayed,
        };
    }

    private static bool IsReuseCandidateExists(
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        Guid shippingGroupId) =>
        fulfillments.Any(f => f.Shipments.Any(s =>
            s.ShippingGroupId == shippingGroupId
            && s.Status == ShipmentOperationStatus.Created
            && s.DispatchedAt is null
            && !string.IsNullOrWhiteSpace(s.ShippingMethodCode)));

    /// <summary>
    /// Parcels already recorded for one constituent group that are still open (created, not dispatched).
    /// A retry must reuse these instead of allocating again.
    /// </summary>
    private static Guid[] RecordedMemberParcels(FulfillmentSnapshot snapshot, Guid shippingGroupId) =>
        snapshot.Shipments
            .Where(s => s.ShippingGroupId == shippingGroupId
                && s.Status == ShipmentOperationStatus.Created
                && s.DispatchedAt is null
                && !string.IsNullOrWhiteSpace(s.ShippingMethodCode))
            .Select(s => s.ShipmentId)
            .Distinct()
            .ToArray();

    /// <summary>
    /// True when the recorded parcel lines are exactly the reviewed per-line quantities (aggregated).
    /// </summary>
    private static bool RecordedQuantitiesMatch(
        FulfillmentSnapshot snapshot,
        IReadOnlyCollection<Guid> recordedShipmentIds,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        var recorded = snapshot.Shipments
            .Where(s => recordedShipmentIds.Contains(s.ShipmentId))
            .SelectMany(s => s.Items)
            .GroupBy(i => i.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var reviewed = selections
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        return reviewed.Count > 0
            && reviewed.Count == recorded.Count
            && reviewed.All(kv => recorded.TryGetValue(kv.Key, out var quantity) && quantity == kv.Value);
    }

    private async Task<ConsolidatedPackageSnapshot?> FindMatchingActivePackageAsync(
        Guid checkoutId,
        IReadOnlyCollection<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        var expected = shipmentIds.OrderBy(x => x).ToArray();
        var packages = await _fulfillment.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
        return packages
            .Where(p => p.Status is ConsolidatedPackageOperationStatus.Created
                or ConsolidatedPackageOperationStatus.Dispatched)
            .FirstOrDefault(p => p.Members
                .Where(m => m.ReleasedAt is null)
                .Select(m => m.ShipmentId)
                .OrderBy(x => x)
                .SequenceEqual(expected));
    }
}
