namespace Tooba.Catalog.Contracts.Reservation;

/// <summary>Write payload for store/category/offer reservation-policy overrides.</summary>
public sealed record ReservationPolicyOverrideWrite(
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles);

/// <summary>Audit row for reservation-policy Admin settings (not cycle events).</summary>
public sealed record ReservationPolicyAuditItem(
    Guid EventId,
    string Level,
    Guid? ScopeId,
    string Field,
    string? OldOverride,
    string? NewOverride,
    Guid ActorUserId,
    DateTimeOffset OccurredAt);

/// <summary>
/// Catalog persistence boundary for Admin reservation-policy settings.
/// Order consumes this Contracts port only — no CatalogDbContext leakage.
/// </summary>
public interface IStoreReservationPolicySettingsPort
{
    /// <summary>Writes store-level reservation-cycle overrides and audit rows.</summary>
    Task SaveStoreOverridesAsync(
        ReservationPolicyOverrideWrite write,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Writes or clears a category-level override and audit rows.</summary>
    Task SaveCategoryOverrideAsync(
        Guid categoryId,
        ReservationPolicyOverrideWrite write,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Writes or clears an offer-level override and audit rows.</summary>
    Task SaveOfferOverrideAsync(
        Guid offerId,
        ReservationPolicyOverrideWrite write,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Lists recent reservation-policy settings audit events (newest first).</summary>
    Task<IReadOnlyList<ReservationPolicyAuditItem>> ListAuditAsync(
        int take,
        CancellationToken cancellationToken);
}
