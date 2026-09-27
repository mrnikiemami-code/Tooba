using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Reservation;

/// <summary>Catalog persistence for Admin reservation-policy settings + audit.</summary>
public sealed class StoreReservationPolicySettingsPort : IStoreReservationPolicySettingsPort
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;

    /// <summary>Creates the port.</summary>
    public StoreReservationPolicySettingsPort(CatalogDbContext catalog, IClock clock)
    {
        _catalog = catalog;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task SaveStoreOverridesAsync(
        ReservationPolicyOverrideWrite write,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var store = await _catalog.StoreHoldPolicySettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
        if (store is null)
        {
            store = StoreHoldPolicySettings.CreateDefault(now);
            _catalog.StoreHoldPolicySettings.Add(store);
        }

        RecordField("store", null, "InitialReservationHoldMinutes", store.InitialReservationHoldMinutes, write.InitialReservationHoldMinutes, actorUserId, now);
        RecordField("store", null, "RetryReservationHoldMinutes", store.RetryReservationHoldMinutes, write.RetryReservationHoldMinutes, actorUserId, now);
        RecordField("store", null, "MaxReservationCycles", store.MaxReservationCycles, write.MaxReservationCycles, actorUserId, now);
        store.ReplaceReservationCycle(
            write.InitialReservationHoldMinutes,
            write.RetryReservationHoldMinutes,
            write.MaxReservationCycles,
            now);
        await _catalog.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveCategoryOverrideAsync(
        Guid categoryId,
        ReservationPolicyOverrideWrite write,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        ReplaceOverrideAsync(
            ReservationCyclePolicyOverride.CategoryScope,
            categoryId,
            write,
            actorUserId,
            cancellationToken);

    /// <inheritdoc />
    public Task SaveOfferOverrideAsync(
        Guid offerId,
        ReservationPolicyOverrideWrite write,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        ReplaceOverrideAsync(
            ReservationCyclePolicyOverride.OfferScope,
            offerId,
            write,
            actorUserId,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReservationPolicyAuditItem>> ListAuditAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 200);
        return await _catalog.ReservationPolicyAuditEvents.AsNoTracking()
            .OrderByDescending(x => x.OccurredAt)
            .Take(limit)
            .Select(x => new ReservationPolicyAuditItem(
                x.EventId,
                x.Level,
                x.ScopeId,
                x.Field,
                x.OldOverride,
                x.NewOverride,
                x.ActorUserId,
                x.OccurredAt))
            .ToListAsync(cancellationToken);
    }

    private async Task ReplaceOverrideAsync(
        string scopeKind,
        Guid scopeId,
        ReservationPolicyOverrideWrite write,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var row = await _catalog.ReservationCyclePolicyOverrides
            .SingleOrDefaultAsync(x => x.ScopeKind == scopeKind && x.ScopeId == scopeId, cancellationToken);
        var oldInitial = row?.InitialReservationHoldMinutes;
        var oldRetry = row?.RetryReservationHoldMinutes;
        var oldMax = row?.MaxReservationCycles;
        var empty = write.InitialReservationHoldMinutes is null
            && write.RetryReservationHoldMinutes is null
            && write.MaxReservationCycles is null;
        if (empty)
        {
            if (row is not null)
            {
                _catalog.ReservationCyclePolicyOverrides.Remove(row);
            }
        }
        else if (row is null)
        {
            _catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
                scopeKind,
                scopeId,
                write.InitialReservationHoldMinutes,
                write.RetryReservationHoldMinutes,
                write.MaxReservationCycles,
                now));
        }
        else
        {
            row.Apply(
                write.InitialReservationHoldMinutes,
                write.RetryReservationHoldMinutes,
                write.MaxReservationCycles,
                now);
        }

        RecordField(scopeKind, scopeId, "InitialReservationHoldMinutes", oldInitial, write.InitialReservationHoldMinutes, actorUserId, now);
        RecordField(scopeKind, scopeId, "RetryReservationHoldMinutes", oldRetry, write.RetryReservationHoldMinutes, actorUserId, now);
        RecordField(scopeKind, scopeId, "MaxReservationCycles", oldMax, write.MaxReservationCycles, actorUserId, now);
        await _catalog.SaveChangesAsync(cancellationToken);
    }

    private void RecordField(
        string level,
        Guid? scopeId,
        string field,
        int? oldValue,
        int? newValue,
        Guid actorUserId,
        DateTimeOffset now)
    {
        if (oldValue == newValue)
        {
            return;
        }

        _catalog.ReservationPolicyAuditEvents.Add(
            ReservationPolicyAuditEvent.Create(level, scopeId, field, oldValue, newValue, actorUserId, now));
    }
}
