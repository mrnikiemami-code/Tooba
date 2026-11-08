using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Admin.Operations;

/// <summary>
/// TB-253-R4: durable Order-owned storage for the immutable Marketplace operational-group occurrence.
/// </summary>
/// <remarks>
/// The claim row is inserted before any Fulfillment allocation; member links are appended immediately
/// after each durable allocation. A concurrent twin receives the SAME recorded ids (unique primary
/// key + reload) instead of overwriting them, so "allocate once" is a storage invariant, not a race.
/// </remarks>
internal sealed class OrderOperationalGroupOccurrenceStore(OrderDbContext db, IClock clock)
    : IOrderOperationalGroupOccurrenceStore
{
    /// <inheritdoc />
    public async Task<OperationalGroupOccurrenceRecord> ClaimAsync(
        OperationalGroupOccurrenceEnvelope envelope,
        string envelopeFingerprint,
        CancellationToken cancellationToken)
    {
        var existing = await GetAsync(envelope.OccurrenceId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var entity = OrderOperationalGroupOccurrence.Claim(
            envelope.OccurrenceId,
            envelope.CheckoutId,
            envelope.ActorUserId,
            envelope.OperationalGroupKey,
            envelopeFingerprint,
            OperationalGroupOccurrenceEnvelopeCodec.CanonicalJson(envelope),
            envelope.AcceptedMethodCode,
            clock.UtcNow);
        db.OperationalGroupOccurrences.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent twin claimed the same occurrence first. Its envelope is the authority.
            db.Entry(entity).State = EntityState.Detached;
        }

        return await GetAsync(envelope.OccurrenceId, cancellationToken)
            ?? throw new InvalidOperationException(
                "order.operational_group.occurrence.claim_unreadable");
    }

    /// <inheritdoc />
    public async Task<OperationalGroupOccurrenceAllocation> RecordMemberAllocationAsync(
        Guid occurrenceId,
        OperationalGroupOccurrenceAllocation allocation,
        CancellationToken cancellationToken)
    {
        var existing = await db.OperationalGroupOccurrenceMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OccurrenceId == occurrenceId && x.ShippingGroupId == allocation.ShippingGroupId,
                cancellationToken);
        if (existing is not null)
        {
            return ToAllocation(existing);
        }

        var entity = OrderOperationalGroupOccurrenceMember.Record(
            occurrenceId,
            allocation.ShippingGroupId,
            OperationalGroupOccurrenceAllocationJson.Serialize(allocation.CreatedShipmentIds),
            OperationalGroupOccurrenceAllocationJson.Serialize(allocation.ReusedShipmentIds),
            allocation.CreatedShipmentIds.Count > 0,
            clock.UtcNow);
        db.OperationalGroupOccurrenceMembers.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            db.Entry(entity).State = EntityState.Detached;
            var recorded = await db.OperationalGroupOccurrenceMembers
                .AsNoTracking()
                .SingleAsync(
                    x => x.OccurrenceId == occurrenceId && x.ShippingGroupId == allocation.ShippingGroupId,
                    cancellationToken);
            return ToAllocation(recorded);
        }

        await MarkInProgressAsync(occurrenceId, cancellationToken);
        return allocation;
    }

    /// <inheritdoc />
    public async Task RecordCompletedAsync(
        Guid occurrenceId,
        Guid consolidatedPackageId,
        string packageNumber,
        string shippingMethodCode,
        CancellationToken cancellationToken)
    {
        var entity = await db.OperationalGroupOccurrences
            .SingleOrDefaultAsync(x => x.OccurrenceId == occurrenceId, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException("order.operational_group.occurrence.claim_missing");
        }

        entity.Complete(consolidatedPackageId, packageNumber, shippingMethodCode, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OperationalGroupOccurrenceRecord?> GetAsync(
        Guid occurrenceId,
        CancellationToken cancellationToken)
    {
        var entity = await db.OperationalGroupOccurrences
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OccurrenceId == occurrenceId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var members = await db.OperationalGroupOccurrenceMembers
            .AsNoTracking()
            .Where(x => x.OccurrenceId == occurrenceId)
            .ToListAsync(cancellationToken);

        return new OperationalGroupOccurrenceRecord(
            entity.OccurrenceId,
            entity.CheckoutId,
            entity.ActorUserId,
            entity.OperationalGroupKey,
            entity.EnvelopeFingerprint,
            entity.EnvelopeJson,
            string.Equals(entity.State, OrderOperationalGroupOccurrence.CompletedState, StringComparison.Ordinal),
            entity.ConsolidatedPackageId,
            entity.PackageNumber,
            entity.ShippingMethodCode,
            members.Select(ToAllocation).ToArray());
    }

    private async Task MarkInProgressAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        var entity = await db.OperationalGroupOccurrences
            .SingleOrDefaultAsync(x => x.OccurrenceId == occurrenceId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.MarkInProgress(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static OperationalGroupOccurrenceAllocation ToAllocation(
        OrderOperationalGroupOccurrenceMember member) =>
        new(
            member.ShippingGroupId,
            OperationalGroupOccurrenceAllocationJson.Deserialize(member.ShipmentIdsJson),
            OperationalGroupOccurrenceAllocationJson.Deserialize(member.ReusedShipmentIdsJson));

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException { SqlState: "23505" };
}
