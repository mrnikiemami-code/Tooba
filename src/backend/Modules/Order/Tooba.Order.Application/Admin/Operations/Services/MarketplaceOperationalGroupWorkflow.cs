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
/// TB-253-R2/R3: one coherent Marketplace operational group workflow.
/// </summary>
/// <remarks>
/// <para>
/// Workers review exact remaining packed quantities and/or existing eligible parcels per constituent
/// accepted group; the server validates the WHOLE immutable plan first, then creates/reuses exactly
/// the constituent parcels of ONE prebound workflow occurrence and their common package.
/// </para>
/// <para>
/// R3 corrections: the occurrence is prebound by the caller and is the only allocation identity
/// (payload hashes are no longer used, so a deliberate second identical package is never mistaken for
/// a retry). Every requested line and reuse parcel must be accounted for before the first write, so a
/// bad later member can never leave an earlier member allocated. Recovery reads the recorded parcels
/// by their occurrence key instead of sweeping currently-open parcels by aggregate quantity.
/// </para>
/// <para>
/// The workflow owns its authority check so the caller cannot bypass it by projecting a ready action:
/// only <c>order.handle</c> or <c>fulfillment.manage</c> may run it.
/// </para>
/// </remarks>
public sealed class MarketplaceOperationalGroupWorkflow(
    IFulfillmentAdminOperations fulfillment,
    IOrderCommittedCarrierShipmentFactsReader committedShippingGroups,
    IOrderShippingGroupHistoryReader shippingGroupHistory,
    IOrderAdminEffectiveAccessReader access)
{
    /// <summary>Bounded attempts to observe a concurrently created common package.</summary>
    private const int PackageObservationAttempts = 3;

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
        // 0. The occurrence must be prebound: without it, a response loss is indistinguishable from a
        //    brand new operation, which is exactly the defect this slice closes.
        var occurrenceId = request.OperationalGroupOccurrenceId ?? Guid.Empty;
        if (occurrenceId == Guid.Empty)
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowOccurrenceRequired);
        }

        var requestedIds = (request.OperationalGroupShippingGroupIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .ToArray();
        if (requestedIds.Distinct().Count() != requestedIds.Length)
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign);
        }

        requestedIds = requestedIds.Distinct().ToArray();
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

        // Remaining-quantity facts are read BEFORE the parcel snapshot on purpose: a request that
        // observes quantities already consumed by this same occurrence must also observe the parcels
        // that consumed them, otherwise a concurrent twin would look like a stale over-selection.
        var facts = await _committedShippingGroups.ListByCheckoutAsync(checkoutId, cancellationToken);
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
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
            .ToArray();
        if (requestedReuse.Distinct().Count() != requestedReuse.Length)
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowShipmentForeign);
        }

        // 1. Build and validate the COMPLETE plan before any write.
        var activeMemberships = requestedReuse.Length == 0
            ? new Dictionary<Guid, ActivePackageMembershipSnapshot>()
            : (await _fulfillment.GetActiveMembershipByShipmentIdsAsync(requestedReuse, cancellationToken))
                .Where(x => x.PackageStatus is not ConsolidatedPackageOperationStatus.Cancelled)
                .GroupBy(x => x.ShipmentId)
                .ToDictionary(g => g.Key, g => g.First());
        var plan = BuildPlan(
            checkoutId,
            occurrenceId,
            match,
            fulfillments,
            factsById,
            request,
            requestedReuse,
            activeMemberships);

        // 2. Execute the pre-validated plan. Each member allocation is bound to the occurrence key, so a
        //    retry/remount replays the recorded parcel and a changed request is a typed conflict.
        var shipmentIds = new List<Guid>();
        var parcelResults = new List<object>(plan.Count);
        foreach (var member in plan)
        {
            var snapshot = fulfillments.First(x => x.SellerOrderId == member.SellerOrderId);
            var recorded = RecordedOccurrenceParcels(snapshot, member.DispatchKey);

            if (member.Lines.Count == 0)
            {
                if (recorded.Length == 0)
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired);
                }

                shipmentIds.AddRange(recorded);
                parcelResults.Add(new
                {
                    shippingGroupId = member.ShippingGroupId,
                    shipmentIds = recorded,
                    created = false,
                    replayed = true,
                });
                continue;
            }

            if (recorded.Length > 0)
            {
                // Same occurrence: the recorded parcel must represent exactly this reviewed request.
                if (!RecordedQuantitiesMatch(snapshot, recorded, member.Lines))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict);
                }

                var replayedIds = recorded.Concat(member.ReuseShipmentIds)
                    .Distinct()
                    .ToArray();
                shipmentIds.AddRange(replayedIds);
                parcelResults.Add(new
                {
                    shippingGroupId = member.ShippingGroupId,
                    shipmentIds = replayedIds,
                    created = false,
                    replayed = true,
                });
                continue;
            }

            Guid createdShipmentId;
            try
            {
                var created = await _fulfillment.CreateShipmentAsync(
                    snapshot.FulfillmentId,
                    actorUserId,
                    member.CarrierDisplayName,
                    member.Lines,
                    member.ShippingMethodCode,
                    providerMetadataJson: member.TrustedProviderMetadataJson,
                    cancellationToken,
                    member.ShippingGroupId,
                    member.DispatchKey);
                createdShipmentId = created.CreatedShipmentId;
            }
            catch (ContractOperationException ex) when (IsIntentConflict(ex.Code))
            {
                var reloaded = await _fulfillment.GetAsync(snapshot.FulfillmentId, cancellationToken);
                var reconciled = reloaded is null
                    ? Array.Empty<Guid>()
                    : RecordedOccurrenceParcels(reloaded, member.DispatchKey);
                if (reconciled.Length == 0
                    || !RecordedQuantitiesMatch(reloaded!, reconciled, member.Lines))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict);
                }

                createdShipmentId = reconciled[0];
            }

            var memberIds = new[] { createdShipmentId }
                .Concat(member.ReuseShipmentIds)
                .Distinct()
                .ToArray();
            shipmentIds.AddRange(memberIds);
            parcelResults.Add(new
            {
                shippingGroupId = member.ShippingGroupId,
                shipmentIds = memberIds,
                created = true,
            });
        }

        var distinctShipments = shipmentIds.Distinct().ToArray();
        if (distinctShipments.Length < 2)
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.ConsolidateRequiresMultiSeller);
        }

        // 3. The common package. An already-member/duplicate refusal means this exact occurrence (or a
        //    concurrent twin of it) already owns the package; resolve THAT package, never another one.
        ConsolidatedPackageSnapshot? package = null;
        var replayed = false;
        for (var attempt = 1; attempt <= PackageObservationAttempts && package is null; attempt++)
        {
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
                package = await FindMatchingActivePackageAsync(checkoutId, distinctShipments, cancellationToken);
                replayed = package is not null;
                if (package is null && attempt < PackageObservationAttempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
                }
            }
        }

        if (package is null)
        {
            // Allocations are durably recorded; the package is unresolved. The caller must reconcile the
            // SAME occurrence rather than minting a new one or resending blindly.
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowPending);
        }

        return new
        {
            ok = true,
            code = "prepare_operational_group",
            operationalGroupKey = match.OperationalGroupKey,
            operationLocator = MarketplaceOperationalDeliveryGroupAuthority.BuildWorkflowOperationLocator(occurrenceId),
            occurrenceId,
            consolidatedPackageId = package.ConsolidatedPackageId,
            packageNumber = package.PackageNumber,
            shippingMethodCode = package.ShippingMethodCode,
            shipmentIds = distinctShipments,
            members = parcelResults,
            replayed,
        };
    }

    /// <summary>
    /// Builds and fully validates the immutable per-member plan. No fulfillment write happens here.
    /// </summary>
    private static IReadOnlyList<MemberPlan> BuildPlan(
        Guid checkoutId,
        Guid occurrenceId,
        MarketplaceOperationalDeliveryGroupAuthority.OperationalGroup match,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyDictionary<Guid, OrderCommittedCarrierShipmentFacts> factsById,
        AdminOrderOperationRequest request,
        IReadOnlyList<Guid> requestedReuse,
        IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot> activeMemberships)
    {
        var selections = (request.Selections ?? Array.Empty<AdminOrderLineSelection>()).ToArray();
        if (selections.Any(s => s.OrderLineId == Guid.Empty || s.Quantity <= 0m))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid);
        }

        if (selections
            .GroupBy(s => s.OrderLineId)
            .Any(g => g.Count() > 1))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid);
        }

        var membership = new Dictionary<Guid, Guid>();
        foreach (var member in match.Members)
        {
            foreach (var lineId in member.OrderLineIds)
            {
                if (!membership.TryAdd(lineId, member.ShippingGroupId))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid);
                }
            }
        }

        // Every requested line must belong to exactly one intended member of this exact group.
        if (selections.Any(s => !membership.ContainsKey(s.OrderLineId)))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid);
        }

        // Every requested reuse parcel must be accounted for exactly once and belong to this checkout.
        var reuseOwner = new Dictionary<Guid, Guid>();
        foreach (var reuseId in requestedReuse)
        {
            var owners = match.Members
                .Where(m => fulfillments
                    .Where(f => f.SellerOrderId == m.SellerOrderId)
                    .Any(f => f.Shipments.Any(s => s.ShipmentId == reuseId)))
                .Select(m => m.ShippingGroupId)
                .Distinct()
                .ToArray();
            if (owners.Length != 1)
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowShipmentForeign);
            }

            reuseOwner[reuseId] = owners[0];
        }

        var plans = new List<MemberPlan>(match.Members.Count);
        foreach (var member in match.Members)
        {
            var snapshot = fulfillments.FirstOrDefault(x => x.SellerOrderId == member.SellerOrderId)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");
            var facts = factsById[member.ShippingGroupId];
            if (facts.ExecutableLines is null)
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired);
            }

            var dispatchKey = MarketplaceOperationalDeliveryGroupAuthority.BuildMemberParcelDispatchKey(
                checkoutId,
                occurrenceId,
                member.ShippingGroupId);
            var recorded = RecordedOccurrenceParcels(snapshot, dispatchKey);
            var isReplay = recorded.Length > 0;

            var remaining = facts.ExecutableLines
                .GroupBy(x => x.OrderLineId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingPackedQuantity));

            var memberSelections = selections
                .Where(s => membership[s.OrderLineId] == member.ShippingGroupId)
                .ToArray();

            // Remaining packed quantity gates only a FIRST allocation. A replay re-presents quantities
            // this occurrence already consumed, so it is resolved from its recorded parcels; a changed
            // request on the same occurrence is a typed conflict instead of a stale-quantity refusal.
            if (!isReplay)
            {
                if (memberSelections.Length == 0 && !reuseOwner.Values.Contains(member.ShippingGroupId))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired);
                }

                if (memberSelections.Any(s =>
                        !remaining.TryGetValue(s.OrderLineId, out var left) || s.Quantity > left))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid);
                }
            }

            var memberReuse = requestedReuse
                .Where(id => reuseOwner[id] == member.ShippingGroupId)
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

            MemberPlan plan;
            if (isReplay)
            {
                // No create will happen: the recorded parcels are the authority. Lines are only used to
                // prove the re-presented request still matches what this occurrence actually allocated.
                var carrierLabel = string.IsNullOrWhiteSpace(facts.MethodLabel)
                    ? facts.MethodCode.Trim()
                    : facts.MethodLabel.Trim();
                plan = new MemberPlan(
                    member.ShippingGroupId,
                    member.SellerOrderId,
                    dispatchKey,
                    carrierLabel,
                    facts.MethodCode,
                    TrustedProviderMetadataJson: null,
                    memberSelections
                        .Select(s => new ShipmentLineCommand(s.OrderLineId, s.Quantity))
                        .ToArray(),
                    memberReuse);
                plans.Add(plan);
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
                    member.ShippingGroupId,
                    null,
                    null),
                factsById.Values.ToArray());

            if (memberSelections.Length > 0 && bound.Lines.Count == 0)
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired);
            }

            plans.Add(new MemberPlan(
                member.ShippingGroupId,
                member.SellerOrderId,
                dispatchKey,
                bound.CarrierDisplayName,
                bound.ShippingMethodCode,
                bound.TrustedProviderMetadataJson,
                bound.Lines,
                memberReuse));
        }

        return plans;
    }

    private static bool IsIntentConflict(string? code) =>
        code is "fulfillment.shipment.dispatch_key_conflict"
            or "fulfillment.shipment.dispatch_key_duplicate";

    /// <summary>
    /// Parcels recorded for this exact occurrence and constituent group. Only the occurrence key is used
    /// — never "all currently open parcels" and never aggregate-quantity guessing.
    /// </summary>
    private static Guid[] RecordedOccurrenceParcels(FulfillmentSnapshot snapshot, string dispatchKey) =>
        snapshot.Shipments
            .Where(s => !string.IsNullOrWhiteSpace(s.ShippingDispatchKey)
                && string.Equals(s.ShippingDispatchKey, dispatchKey, StringComparison.Ordinal)
                && s.Status != ShipmentOperationStatus.Cancelled)
            .Select(s => s.ShipmentId)
            .Distinct()
            .ToArray();

    /// <summary>
    /// True when the recorded occurrence parcels carry exactly the planned per-line quantities.
    /// </summary>
    private static bool RecordedQuantitiesMatch(
        FulfillmentSnapshot snapshot,
        IReadOnlyCollection<Guid> recordedShipmentIds,
        IReadOnlyList<ShipmentLineCommand> planned)
    {
        var recorded = snapshot.Shipments
            .Where(s => recordedShipmentIds.Contains(s.ShipmentId))
            .SelectMany(s => s.Items)
            .GroupBy(i => i.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var expected = planned
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        return expected.Count > 0
            && expected.Count == recorded.Count
            && expected.All(kv => recorded.TryGetValue(kv.Key, out var quantity) && quantity == kv.Value);
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

    private sealed record MemberPlan(
        Guid ShippingGroupId,
        Guid SellerOrderId,
        string DispatchKey,
        string CarrierDisplayName,
        string? ShippingMethodCode,
        string? TrustedProviderMetadataJson,
        IReadOnlyList<ShipmentLineCommand> Lines,
        IReadOnlyList<Guid> ReuseShipmentIds);
}
