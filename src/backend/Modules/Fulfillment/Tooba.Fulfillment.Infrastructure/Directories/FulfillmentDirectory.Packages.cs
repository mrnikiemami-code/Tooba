using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>
/// سطح بسته‌های تلفیقی ارسال (cohesive partial — AMSC-001 W2، بدون تغییر رفتار).
/// </summary>
public sealed partial class FulfillmentDirectory
{
    // ---------------------------------------------------------------------------------
    // Consolidated packages (ARCH-SIZE-001 split, AMSC-001 W2 — cohesive, behavior-preserving)
    // ---------------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var packages = await _db.ConsolidatedPackages.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var results = new List<ConsolidatedPackageSnapshot>(packages.Count);
        foreach (var package in packages)
        {
            results.Add(await MapPackageSnapshotAsync(package, cancellationToken));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        if (shipmentIds is null || shipmentIds.Count == 0)
        {
            return [];
        }

        var ids = shipmentIds.Distinct().ToArray();
        var members = await _db.ConsolidatedPackageMembers.AsNoTracking()
            .Where(x => ids.Contains(x.ShipmentId) && x.ReleasedAt == null)
            .ToListAsync(cancellationToken);
        if (members.Count == 0)
        {
            return [];
        }

        var packageIds = members.Select(x => x.ConsolidatedPackageId).Distinct().ToArray();
        var packages = await _db.ConsolidatedPackages.AsNoTracking()
            .Where(x => packageIds.Contains(x.ConsolidatedPackageId)
                && x.Status != ConsolidatedPackageStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var byId = packages.ToDictionary(x => x.ConsolidatedPackageId);
        return members
            .Where(m => byId.ContainsKey(m.ConsolidatedPackageId))
            .Select(m =>
            {
                var package = byId[m.ConsolidatedPackageId];
                return new ActivePackageMembershipSnapshot(
                    m.ShipmentId,
                    package.ConsolidatedPackageId,
                    package.PackageNumber,
                    package.Status);
            })
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> IsShipmentLockedByPackageAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var memberships = await GetActiveMembershipByShipmentIdsAsync([shipmentId], cancellationToken);
        return memberships.Any(x =>
            x.PackageStatus is ConsolidatedPackageStatus.Created or ConsolidatedPackageStatus.Dispatched);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> shipmentIds,
        string? shippingMethodCode,
        string? trackingReference,
        string? note,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (shipmentIds is null || shipmentIds.Count == 0)
        {
            throw new ContractOperationException("fulfillment.package.requires_multi_seller");
        }

        var distinctIds = shipmentIds.Distinct().ToArray();
        if (distinctIds.Length != shipmentIds.Count)
        {
            throw new ContractOperationException("fulfillment.package.duplicate_shipment");
        }

        var units = await _db.Fulfillments
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            throw new ContractOperationException("fulfillment.package.checkout_required");
        }

        var unitById = units.ToDictionary(x => x.FulfillmentId);
        var fulfillmentIds = units.Select(x => x.FulfillmentId).ToArray();
        var shipments = await _db.Shipments
            .Where(x => fulfillmentIds.Contains(x.FulfillmentId) && distinctIds.Contains(x.ShipmentId))
            .ToListAsync(cancellationToken);
        if (shipments.Count != distinctIds.Length)
        {
            throw new ContractOperationException("fulfillment.package.mixed_checkout");
        }

        var existingLocks = await GetActiveMembershipByShipmentIdsAsync(distinctIds, cancellationToken);
        if (existingLocks.Count > 0)
        {
            throw new ContractOperationException("fulfillment.package.shipment_already_member");
        }

        var memberSpecs = new List<(Guid ShipmentId, Guid SellerPartyId, Guid FulfillmentId)>(shipments.Count);
        var inheritedMethodCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? inheritedLabel = null;
        foreach (var shipment in shipments)
        {
            if (shipment.Status != ShipmentStatus.Created
                || shipment.DispatchedAt is not null)
            {
                throw new ContractOperationException("fulfillment.package.shipment_not_eligible");
            }

            if (!unitById.TryGetValue(shipment.FulfillmentId, out var unit)
                || unit.CheckoutId != checkoutId
                || unit.Status == FulfillmentStatus.Cancelled)
            {
                throw new ContractOperationException("fulfillment.package.shipment_not_eligible");
            }

            var shipmentMethod = (shipment.ShippingMethodCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(shipmentMethod))
            {
                throw new ContractOperationException("fulfillment.package.shipping_method_required");
            }

            inheritedMethodCodes.Add(shipmentMethod);
            inheritedLabel ??= string.IsNullOrWhiteSpace(shipment.ShippingMethodLabel)
                ? null
                : shipment.ShippingMethodLabel.Trim();
            memberSpecs.Add((shipment.ShipmentId, unit.SellerPartyId, unit.FulfillmentId));
        }

        if (inheritedMethodCodes.Count != 1)
        {
            throw new ContractOperationException("fulfillment.package.shipping_method_mismatch");
        }

        var inheritedCode = inheritedMethodCodes.Single();
        var requestedCode = shippingMethodCode?.Trim();
        if (!string.IsNullOrWhiteSpace(requestedCode)
            && !string.Equals(requestedCode, inheritedCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ContractOperationException("fulfillment.package.shipping_method_mismatch");
        }

        var definition = ShippingMethodRegistry.Find(inheritedCode)
            ?? throw new ContractOperationException("fulfillment.package.shipping_method_required");
        var methodLabel = string.IsNullOrWhiteSpace(inheritedLabel)
            ? ShippingMethodRegistry.ResolveLabel(definition.Code, null)
            : inheritedLabel;

        var now = _clock.UtcNow;
        var package = ConsolidatedPackage.Create(
            _ids.NewId(),
            () => _ids.NewId(),
            checkoutId,
            memberSpecs,
            definition.Code,
            methodLabel,
            trackingReference,
            note,
            actorUserId == Guid.Empty ? null : actorUserId,
            now);

        _db.ConsolidatedPackages.Add(package);
        _db.ConsolidatedPackageMembers.AddRange(package.Members);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new ContractOperationException("fulfillment.package.shipment_already_member", ex);
        }

        _telemetry.RecordTransition("consolidated_package_created");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        package.Cancel(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_cancelled");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(
        Guid consolidatedPackageId,
        string trackingReference,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        package.AssignTracking(trackingReference, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_tracking_assigned");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        if (package.Status is ConsolidatedPackageStatus.Dispatched or ConsolidatedPackageStatus.Delivered)
        {
            return await MapPackageSnapshotAsync(package, cancellationToken);
        }

        if (package.Status != ConsolidatedPackageStatus.Created)
        {
            throw new ContractOperationException("fulfillment.package.dispatch_invalid_state");
        }

        var activeMembers = package.Members.Where(x => x.IsActiveMembership).ToArray();
        if (activeMembers.Length == 0)
        {
            throw new ContractOperationException("fulfillment.package.member_state_changed");
        }

        foreach (var member in activeMembers)
        {
            var unit = await LoadMutableAsync(member.FulfillmentId, cancellationToken);
            var shipment = unit.Shipments.SingleOrDefault(x => x.ShipmentId == member.ShipmentId)
                ?? throw new ContractOperationException("fulfillment.package.member_state_changed");
            if (shipment.Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered)
            {
                continue;
            }

            if (shipment.Status != ShipmentStatus.Created)
            {
                throw new ContractOperationException("fulfillment.package.member_state_changed");
            }

            if (string.IsNullOrWhiteSpace(shipment.TrackingReference)
                && !string.IsNullOrWhiteSpace(package.TrackingReference))
            {
                var baseCode = package.TrackingReference!.Trim();
                var memberCode = $"{baseCode}-{member.ShipmentId.ToString("N")[..8].ToUpperInvariant()}";
                await AssignTrackingCoreAsync(
                    member.FulfillmentId,
                    member.ShipmentId,
                    actorUserId,
                    memberCode,
                    cancellationToken);
            }

            await DispatchShipmentCoreAsync(
                member.FulfillmentId,
                member.ShipmentId,
                actorUserId,
                cancellationToken);
        }

        var statuses = await LoadMemberShipmentStatusesAsync(
            activeMembers.Select(x => x.ShipmentId).ToArray(),
            cancellationToken);
        package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        package.MarkDispatched(_clock.UtcNow, statuses);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_dispatched");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        if (package.Status == ConsolidatedPackageStatus.Delivered)
        {
            return await MapPackageSnapshotAsync(package, cancellationToken);
        }

        if (package.Status != ConsolidatedPackageStatus.Dispatched)
        {
            throw new ContractOperationException("fulfillment.package.deliver_before_dispatch");
        }

        var activeMembers = package.Members.Where(x => x.IsActiveMembership).ToArray();
        foreach (var member in activeMembers)
        {
            await DeliverShipmentCoreAsync(
                member.FulfillmentId,
                member.ShipmentId,
                actorUserId,
                cancellationToken);
        }

        var statuses = await LoadMemberShipmentStatusesAsync(
            activeMembers.Select(x => x.ShipmentId).ToArray(),
            cancellationToken);
        package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        package.MarkDelivered(_clock.UtcNow, statuses);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_delivered");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task VoidActivePackagesForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var packages = await _db.ConsolidatedPackages
            .Where(x => x.CheckoutId == checkoutId && x.Status == ConsolidatedPackageStatus.Created)
            .ToListAsync(cancellationToken);
        if (packages.Count == 0)
        {
            return;
        }

        var now = _clock.UtcNow;
        foreach (var header in packages)
        {
            var package = await LoadMutablePackageAsync(header.ConsolidatedPackageId, cancellationToken);
            package.Cancel(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_voided_for_order_cancel");
    }

    private async Task EnsureShipmentNotLockedByPackageAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        if (await IsShipmentLockedByPackageAsync(shipmentId, cancellationToken))
        {
            throw new ContractOperationException("fulfillment.shipment.locked_by_consolidated_package");
        }
    }

    private async Task<ConsolidatedPackage> LoadMutablePackageAsync(
        Guid consolidatedPackageId,
        CancellationToken cancellationToken)
    {
        var package = await _db.ConsolidatedPackages
            .SingleOrDefaultAsync(x => x.ConsolidatedPackageId == consolidatedPackageId, cancellationToken)
            ?? throw new ContractOperationException("fulfillment.package.not_found");
        var members = await _db.ConsolidatedPackageMembers
            .Where(x => x.ConsolidatedPackageId == consolidatedPackageId)
            .ToListAsync(cancellationToken);
        package.AttachLoadedMembers(members);
        return package;
    }

    private async Task<IReadOnlyDictionary<Guid, ShipmentStatus>> LoadMemberShipmentStatusesAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        if (shipmentIds.Count == 0)
        {
            return new Dictionary<Guid, ShipmentStatus>();
        }

        var rows = await _db.Shipments.AsNoTracking()
            .Where(x => shipmentIds.Contains(x.ShipmentId))
            .Select(x => new { x.ShipmentId, x.Status })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.ShipmentId, x => x.Status);
    }

    private async Task<ConsolidatedPackageSnapshot> MapPackageSnapshotAsync(
        ConsolidatedPackage package,
        CancellationToken cancellationToken)
    {
        var members = package.Members.Count > 0
            ? package.Members
            : await _db.ConsolidatedPackageMembers.AsNoTracking()
                .Where(x => x.ConsolidatedPackageId == package.ConsolidatedPackageId)
                .ToListAsync(cancellationToken);
        return new ConsolidatedPackageSnapshot(
            package.ConsolidatedPackageId,
            package.PackageNumber,
            package.CheckoutId,
            package.Status,
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
            members.Select(x => new ConsolidatedPackageMemberSnapshot(
                x.ConsolidatedPackageMemberId,
                x.ShipmentId,
                x.SellerPartyId,
                x.FulfillmentId,
                x.JoinedAt,
                x.ReleasedAt)).ToArray());
    }
}
