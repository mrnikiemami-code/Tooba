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
/// TB-253-R2/R3/R4: one coherent Marketplace operational group workflow.
/// </summary>
/// <remarks>
/// <para>
/// Workers review exact remaining packed quantities and/or existing eligible parcels per constituent
/// accepted group; the server validates the WHOLE immutable plan first, then creates/reuses exactly
/// the constituent parcels of ONE prebound workflow occurrence and their common package.
/// </para>
/// <para>
/// R3 restored occurrence identity (payload hashes are never the allocation identity) and made every
/// requested line/reuse parcel accounted for before the first write.
/// </para>
/// <para>
/// R4 corrects the recovery contract: the COMPLETE reviewed envelope (actor scope, checkout, member
/// set, per-line quantities, explicit reuse ids, accepted method) is durably claimed before any
/// allocation. Same occurrence + different envelope is a typed conflict even when the changed member
/// was never allocated; same occurrence + same envelope replays the exact recorded links without
/// depending on current remaining quantities. Explicit reuse-only execution never calls the create
/// binder, and reuse ownership is resolved by the shipment's own shipping group.
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
    IOrderAdminEffectiveAccessReader access,
    IOrderOperationalGroupOccurrenceStore occurrences)
{
    /// <summary>Bounded attempts to observe a concurrently created common package.</summary>
    private const int PackageObservationAttempts = 3;

    private readonly IFulfillmentAdminOperations _fulfillment = fulfillment;
    private readonly IOrderCommittedCarrierShipmentFactsReader _committedShippingGroups = committedShippingGroups;
    private readonly IOrderShippingGroupHistoryReader _shippingGroupHistory = shippingGroupHistory;
    private readonly IOrderAdminEffectiveAccessReader _access = access;
    private readonly IOrderOperationalGroupOccurrenceStore _occurrences = occurrences;

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

        // 1. Build the COMPLETE immutable envelope and claim it durably BEFORE any allocation. The
        //    claim also proves that a later call under this occurrence carries the exact same request.
        var envelope = BuildEnvelope(
            occurrenceId,
            checkoutId,
            actorUserId,
            match,
            acceptedMethod,
            request,
            requestedReuse,
            fulfillments);
        var fingerprint = OperationalGroupOccurrenceEnvelopeCodec.Fingerprint(envelope);

        // A brand-new occurrence must not be frozen by an invalid plan: prove the whole plan fits
        // before claiming it, so a typed refusal leaves no claimed row behind. An EXISTING occurrence
        // is never re-validated against remaining quantity here, because its own recorded allocations
        // lawfully consumed that quantity.
        var known = await _occurrences.GetAsync(occurrenceId, cancellationToken);
        if (known is null)
        {
            var probe = new OperationalGroupOccurrenceRecord(
                occurrenceId,
                checkoutId,
                actorUserId,
                match.OperationalGroupKey,
                fingerprint,
                EnvelopeJson: string.Empty,
                Completed: false,
                ConsolidatedPackageId: null,
                PackageNumber: null,
                ShippingMethodCode: acceptedMethod,
                Allocations: []);
            _ = BuildPlan(
                checkoutId,
                occurrenceId,
                match,
                fulfillments,
                factsById,
                envelope,
                probe,
                await LoadActiveMembershipsAsync(envelope, cancellationToken));
        }

        var occurrence = await _occurrences.ClaimAsync(envelope, fingerprint, cancellationToken);

        if (occurrence.CheckoutId != checkoutId
            || occurrence.ActorUserId != actorUserId
            || !string.Equals(occurrence.OperationalGroupKey, match.OperationalGroupKey, StringComparison.Ordinal)
            || !string.Equals(occurrence.EnvelopeFingerprint, fingerprint, StringComparison.Ordinal))
        {
            // Same occurrence, different actor/group/envelope — including a change to a member that was
            // never allocated or to the reuse set. Refused BEFORE any new write.
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict);
        }

        if (occurrence.Completed)
        {
            return await ReplayCompletedAsync(
                checkoutId,
                match,
                occurrence,
                fulfillments,
                cancellationToken);
        }

        // 2. Build and validate the COMPLETE plan from the DURABLE envelope (never from current
        //    remaining quantity guesses) before any member write.
        var activeMemberships = await LoadActiveMembershipsAsync(envelope, cancellationToken);
        var plan = BuildPlan(
            checkoutId,
            occurrenceId,
            match,
            fulfillments,
            factsById,
            envelope,
            occurrence,
            activeMemberships);

        // 3. Execute the pre-validated plan. Each member allocation is bound to the occurrence key and
        //    recorded durably immediately, so a failure after member A still recovers A's exact parcel.
        var shipmentIds = new List<Guid>();
        var parcelResults = new List<object>(plan.Count);
        foreach (var member in plan)
        {
            var snapshot = fulfillments.First(x => x.SellerOrderId == member.SellerOrderId);

            if (member.RecordedAllocation is { } recorded)
            {
                var replayedIds = recorded.CreatedShipmentIds
                    .Concat(recorded.ReusedShipmentIds)
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

            if (member.Kind == MemberPlanKind.ReuseOnly)
            {
                // Explicit reuse-only: exactly the reviewed reusable parcels, NEVER a create binder call
                // and never a new parcel.
                var recordedReuse = await _occurrences.RecordMemberAllocationAsync(
                    occurrenceId,
                    new OperationalGroupOccurrenceAllocation(
                        member.ShippingGroupId,
                        CreatedShipmentIds: [],
                        member.ReuseShipmentIds),
                    cancellationToken);
                var reuseIds = recordedReuse.ReusedShipmentIds.Distinct().ToArray();
                shipmentIds.AddRange(reuseIds);
                parcelResults.Add(new
                {
                    shippingGroupId = member.ShippingGroupId,
                    shipmentIds = reuseIds,
                    created = false,
                    replayed = false,
                    reused = true,
                });
                continue;
            }

            if (member.Kind == MemberPlanKind.ReconcileRecorded)
            {
                // R5: link the EXACT parcel recorded under this occurrence key before the remaining-
                // quantity gate could hide it. No replacement parcel is ever created here.
                var reconciledLink = await _occurrences.RecordMemberAllocationAsync(
                    occurrenceId,
                    new OperationalGroupOccurrenceAllocation(
                        member.ShippingGroupId,
                        member.ScannedShipmentIds ?? [],
                        member.ReuseShipmentIds),
                    cancellationToken);
                var reconciledLinkIds = reconciledLink.CreatedShipmentIds
                    .Concat(reconciledLink.ReusedShipmentIds)
                    .Distinct()
                    .ToArray();
                shipmentIds.AddRange(reconciledLinkIds);
                parcelResults.Add(new
                {
                    shippingGroupId = member.ShippingGroupId,
                    shipmentIds = reconciledLinkIds,
                    created = false,
                    replayed = true,
                    reconciled = true,
                });
                continue;
            }

            // A parcel already written under this occurrence key but not yet recorded (crash between
            // the durable create and the durable member link) is reconciled, never re-created.
            var keyParcels = RecordedOccurrenceParcels(snapshot, member.DispatchKey);
            if (keyParcels.Length > 0)
            {
                if (member.Lines.Count == 0
                    || !RecordedQuantitiesMatch(snapshot, keyParcels, member.Lines))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict);
                }

                var reconciled = await _occurrences.RecordMemberAllocationAsync(
                    occurrenceId,
                    new OperationalGroupOccurrenceAllocation(
                        member.ShippingGroupId,
                        keyParcels,
                        member.ReuseShipmentIds),
                    cancellationToken);
                var reconciledIds = reconciled.CreatedShipmentIds
                    .Concat(reconciled.ReusedShipmentIds)
                    .Distinct()
                    .ToArray();
                shipmentIds.AddRange(reconciledIds);
                parcelResults.Add(new
                {
                    shippingGroupId = member.ShippingGroupId,
                    shipmentIds = reconciledIds,
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
                    || member.Lines.Count == 0
                    || !RecordedQuantitiesMatch(reloaded!, reconciled, member.Lines))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict);
                }

                createdShipmentId = reconciled[0];
            }

            var recordedCreate = await _occurrences.RecordMemberAllocationAsync(
                occurrenceId,
                new OperationalGroupOccurrenceAllocation(
                    member.ShippingGroupId,
                    [createdShipmentId],
                    member.ReuseShipmentIds),
                cancellationToken);

            var memberIds = recordedCreate.CreatedShipmentIds
                .Concat(recordedCreate.ReusedShipmentIds)
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

        // 4. The common package. An already-member/duplicate refusal means this exact occurrence (or a
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

        await _occurrences.RecordCompletedAsync(
            occurrenceId,
            package.ConsolidatedPackageId,
            package.PackageNumber,
            package.ShippingMethodCode ?? acceptedMethod,
            cancellationToken);

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
    /// Reads a COMPLETED occurrence's exact recorded result. No allocation, no remaining-quantity gate,
    /// no create-readiness gate — a completed operation is answered from its own durable links.
    /// </summary>
    private async Task<object> ReplayCompletedAsync(
        Guid checkoutId,
        MarketplaceOperationalDeliveryGroupAuthority.OperationalGroup match,
        OperationalGroupOccurrenceRecord occurrence,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        CancellationToken cancellationToken)
    {
        if (occurrence.ConsolidatedPackageId is not Guid packageId
            || string.IsNullOrWhiteSpace(occurrence.PackageNumber))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowPending);
        }

        var packages = await _fulfillment.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
        var package = packages.FirstOrDefault(p =>
            p.ConsolidatedPackageId == packageId
            && p.Status is ConsolidatedPackageOperationStatus.Created
                or ConsolidatedPackageOperationStatus.Dispatched);
        if (package is null)
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowPending);
        }

        var memberResults = new List<object>(match.Members.Count);
        var shipmentIds = new List<Guid>();
        foreach (var member in match.Members.OrderBy(m => m.ShippingGroupId))
        {
            var allocation = occurrence.Allocations
                .FirstOrDefault(a => a.ShippingGroupId == member.ShippingGroupId)
                ?? throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowPending);
            var ids = allocation.CreatedShipmentIds
                .Concat(allocation.ReusedShipmentIds)
                .Distinct()
                .ToArray();
            if (ids.Length == 0)
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowPending);
            }

            // The recorded ids must still be real, non-cancelled parcels of that member's fulfillment.
            var snapshot = fulfillments.FirstOrDefault(x => x.SellerOrderId == member.SellerOrderId)
                ?? throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowPending);
            if (ids.Any(id => snapshot.Shipments.All(s =>
                    s.ShipmentId != id || s.Status == ShipmentOperationStatus.Cancelled)))
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowPending);
            }

            shipmentIds.AddRange(ids);
            memberResults.Add(new
            {
                shippingGroupId = member.ShippingGroupId,
                shipmentIds = ids,
                created = false,
                replayed = true,
            });
        }

        return new
        {
            ok = true,
            code = "prepare_operational_group",
            operationalGroupKey = match.OperationalGroupKey,
            operationLocator = MarketplaceOperationalDeliveryGroupAuthority.BuildWorkflowOperationLocator(
                occurrence.OccurrenceId),
            occurrenceId = occurrence.OccurrenceId,
            consolidatedPackageId = packageId,
            packageNumber = occurrence.PackageNumber,
            shippingMethodCode = package.ShippingMethodCode ?? occurrence.ShippingMethodCode,
            shipmentIds = shipmentIds.Distinct().ToArray(),
            members = memberResults,
            replayed = true,
        };
    }

    /// <summary>
    /// Builds the immutable reviewed envelope: exact member set, per-line quantities and reuse ids.
    /// </summary>
    private static OperationalGroupOccurrenceEnvelope BuildEnvelope(
        Guid occurrenceId,
        Guid checkoutId,
        Guid actorUserId,
        MarketplaceOperationalDeliveryGroupAuthority.OperationalGroup match,
        string acceptedMethod,
        AdminOrderOperationRequest request,
        IReadOnlyList<Guid> requestedReuse,
        IReadOnlyList<FulfillmentSnapshot> fulfillments)
    {
        var selections = (request.Selections ?? Array.Empty<AdminOrderLineSelection>()).ToArray();
        if (selections.Any(s => s.OrderLineId == Guid.Empty || s.Quantity <= 0m))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid);
        }

        if (selections.GroupBy(s => s.OrderLineId).Any(g => g.Count() > 1))
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

        if (selections.Any(s => !membership.ContainsKey(s.OrderLineId)))
        {
            throw new AdminOrderOperationsException(
                MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid);
        }

        // Reuse ownership is the shipment's OWN shipping group — never sellerOrderId alone, because one
        // seller may hold several accepted groups in the same seller order.
        var reuseOwner = new Dictionary<Guid, Guid>();
        foreach (var reuseId in requestedReuse)
        {
            var owners = match.Members
                .Where(m => fulfillments
                    .Where(f => f.SellerOrderId == m.SellerOrderId)
                    .Any(f => f.Shipments.Any(s =>
                        s.ShipmentId == reuseId
                        && s.ShippingGroupId == m.ShippingGroupId)))
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

        var members = new List<OperationalGroupOccurrenceMember>(match.Members.Count);
        foreach (var member in match.Members.OrderBy(m => m.ShippingGroupId))
        {
            members.Add(new OperationalGroupOccurrenceMember(
                member.ShippingGroupId,
                selections
                    .Where(s => membership[s.OrderLineId] == member.ShippingGroupId)
                    .OrderBy(s => s.OrderLineId)
                    .Select(s => new OperationalGroupOccurrenceSelection(s.OrderLineId, s.Quantity))
                    .ToArray(),
                requestedReuse
                    .Where(id => reuseOwner[id] == member.ShippingGroupId)
                    .OrderBy(id => id)
                    .ToArray()));
        }

        return new OperationalGroupOccurrenceEnvelope(
            occurrenceId,
            checkoutId,
            actorUserId,
            match.OperationalGroupKey,
            acceptedMethod,
            members);
    }

    private async Task<IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot>> LoadActiveMembershipsAsync(
        OperationalGroupOccurrenceEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var reuseIds = envelope.Members
            .SelectMany(m => m.ReuseShipmentIds)
            .Distinct()
            .ToArray();
        if (reuseIds.Length == 0)
        {
            return new Dictionary<Guid, ActivePackageMembershipSnapshot>();
        }

        return (await _fulfillment.GetActiveMembershipByShipmentIdsAsync(reuseIds, cancellationToken))
            .Where(x => x.PackageStatus is not ConsolidatedPackageOperationStatus.Cancelled)
            .GroupBy(x => x.ShipmentId)
            .ToDictionary(g => g.Key, g => g.First());
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
        OperationalGroupOccurrenceEnvelope envelope,
        OperationalGroupOccurrenceRecord occurrence,
        IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot> activeMemberships)
    {
        // Reuse ids already recorded for THIS exact occurrence are the reconciliation case allowed to
        // reference an already-packaged parcel; anything else packaged is foreign.
        var recordedReuse = occurrence.Allocations
            .SelectMany(a => a.ReusedShipmentIds)
            .ToHashSet();

        var plans = new List<MemberPlan>(match.Members.Count);
        foreach (var member in match.Members.OrderBy(m => m.ShippingGroupId))
        {
            var envelopeMember = envelope.Members.First(m => m.ShippingGroupId == member.ShippingGroupId);
            var snapshot = fulfillments.FirstOrDefault(x => x.SellerOrderId == member.SellerOrderId)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");
            var facts = factsById[member.ShippingGroupId];

            var dispatchKey = MarketplaceOperationalDeliveryGroupAuthority.BuildMemberParcelDispatchKey(
                checkoutId,
                occurrenceId,
                member.ShippingGroupId);
            var recordedAllocation = occurrence.Allocations
                .FirstOrDefault(a => a.ShippingGroupId == member.ShippingGroupId);

            var lines = envelopeMember.Selections
                .Select(s => new ShipmentLineCommand(s.OrderLineId, s.Quantity))
                .ToArray();
            var reuse = envelopeMember.ReuseShipmentIds;

            foreach (var reuseId in reuse)
            {
                var shipment = snapshot.Shipments.FirstOrDefault(s => s.ShipmentId == reuseId);
                if (shipment is null
                    || shipment.ShippingGroupId != member.ShippingGroupId
                    || shipment.Status != ShipmentOperationStatus.Created
                    || shipment.DispatchedAt is not null
                    || string.IsNullOrWhiteSpace(shipment.ShippingMethodCode))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowShipmentForeign);
                }

                if (activeMemberships.ContainsKey(reuseId) && !recordedReuse.Contains(reuseId))
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowShipmentForeign);
                }
            }

            if (recordedAllocation is not null)
            {
                if (recordedAllocation.CreatedShipmentIds.Count == 0
                    && recordedAllocation.ReusedShipmentIds.Count == 0)
                {
                    throw new AdminOrderOperationsException(
                        MarketplaceOperationalDeliveryGroupAuthority.WorkflowPending);
                }

                plans.Add(new MemberPlan(
                    member.ShippingGroupId,
                    member.SellerOrderId,
                    dispatchKey,
                    string.Empty,
                    facts.MethodCode,
                    TrustedProviderMetadataJson: null,
                    Lines: [],
                    ReuseShipmentIds: [],
                    MemberPlanKind.Replay,
                    recordedAllocation));
                continue;
            }

            // R5: a member whose parcel was durably created under THIS occurrence key but whose durable
            // member link was never written (crash between the Fulfillment create and the Order link)
            // must reconcile that EXACT recorded parcel before any remaining-quantity gate. Otherwise the
            // consumed quantity makes the envelope unsatisfiable and the recorded parcel becomes
            // unreachable. Only the exact occurrence key is inspected — never open parcels in general —
            // and mismatched evidence is a typed conflict with zero new allocation.
            if (recordedAllocation is null)
            {
                var occurrenceParcels = RecordedOccurrenceParcels(snapshot, dispatchKey);
                if (occurrenceParcels.Length > 0)
                {
                    if (lines.Length == 0
                        || !RecordedParcelEvidenceMatches(
                            snapshot,
                            occurrenceParcels,
                            member.ShippingGroupId,
                            envelope.AcceptedMethodCode,
                            lines))
                    {
                        throw new AdminOrderOperationsException(
                            MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict);
                    }

                    plans.Add(new MemberPlan(
                        member.ShippingGroupId,
                        member.SellerOrderId,
                        dispatchKey,
                        string.Empty,
                        facts.MethodCode,
                        TrustedProviderMetadataJson: null,
                        lines,
                        reuse,
                        MemberPlanKind.ReconcileRecorded,
                        RecordedAllocation: null,
                        ScannedShipmentIds: occurrenceParcels));
                    continue;
                }
            }

            if (lines.Length == 0 && reuse.Count == 0)
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired);
            }

            if (lines.Length == 0)
            {
                // Explicit reuse-only: no line selection exists, so the create binder must not be called
                // (it would invent a selection or refuse an exhausted member).
                plans.Add(new MemberPlan(
                    member.ShippingGroupId,
                    member.SellerOrderId,
                    dispatchKey,
                    string.Empty,
                    facts.MethodCode,
                    TrustedProviderMetadataJson: null,
                    Lines: [],
                    reuse,
                    MemberPlanKind.ReuseOnly,
                    RecordedAllocation: null));
                continue;
            }

            // First allocation of new quantity: remaining packed quantity is authoritative.
            var remaining = facts.ExecutableLines?
                .GroupBy(x => x.OrderLineId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingPackedQuantity))
                ?? throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired);
            if (lines.Any(s =>
                    !remaining.TryGetValue(s.OrderLineId, out var left) || s.Quantity > left))
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid);
            }

            var bound = AcceptedGroupShipmentCreateBinder.Resolve(
                snapshot,
                new AdminOrderOperationRequest(
                    "prepare_operational_group",
                    member.SellerOrderId,
                    snapshot.FulfillmentId,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    lines
                        .Select(x => new AdminOrderLineSelection(x.OrderLineId, x.Quantity))
                        .ToArray(),
                    null,
                    null,
                    null,
                    null,
                    member.ShippingGroupId,
                    null,
                    null,
                    null),
                factsById.Values.ToArray());

            if (bound.Lines.Count == 0)
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
                reuse,
                MemberPlanKind.Create,
                RecordedAllocation: null));
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

    /// <summary>
    /// R5: the exact recorded occurrence parcels provably belong to this member and match the immutable
    /// durable envelope (own shipping group, non-cancelled, accepted method, exact per-line quantities).
    /// </summary>
    private static bool RecordedParcelEvidenceMatches(
        FulfillmentSnapshot snapshot,
        IReadOnlyCollection<Guid> recordedShipmentIds,
        Guid shippingGroupId,
        string acceptedMethodCode,
        IReadOnlyList<ShipmentLineCommand> planned)
    {
        var parcels = snapshot.Shipments
            .Where(s => recordedShipmentIds.Contains(s.ShipmentId))
            .ToArray();
        if (parcels.Length == 0) return false;
        if (parcels.Any(p => p.ShippingGroupId != shippingGroupId)) return false;
        if (parcels.Any(p => p.Status != ShipmentOperationStatus.Created)) return false;
        if (!string.IsNullOrWhiteSpace(acceptedMethodCode)
            && parcels.Any(p => !string.Equals(
                p.ShippingMethodCode, acceptedMethodCode, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return RecordedQuantitiesMatch(snapshot, recordedShipmentIds, planned);
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

    private enum MemberPlanKind
    {
        Create,
        ReuseOnly,
        Replay,
        /// <summary>R5: the member's parcel already exists under this occurrence key but has no link.</summary>
        ReconcileRecorded,
    }

    private sealed record MemberPlan(
        Guid ShippingGroupId,
        Guid SellerOrderId,
        string DispatchKey,
        string CarrierDisplayName,
        string? ShippingMethodCode,
        string? TrustedProviderMetadataJson,
        IReadOnlyList<ShipmentLineCommand> Lines,
        IReadOnlyList<Guid> ReuseShipmentIds,
        MemberPlanKind Kind,
        OperationalGroupOccurrenceAllocation? RecordedAllocation,
        IReadOnlyList<Guid>? ScannedShipmentIds = null);
}
