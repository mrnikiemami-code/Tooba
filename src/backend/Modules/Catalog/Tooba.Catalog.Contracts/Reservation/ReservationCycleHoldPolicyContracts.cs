namespace Tooba.Catalog.Contracts.Reservation;

/// <summary>Scope kind constants for reservation-cycle hold overrides.</summary>
public static class ReservationCycleHoldOverrideScopes
{
    /// <summary>Offer-level override.</summary>
    public const string Offer = "offer";

    /// <summary>Category-level override.</summary>
    public const string Category = "category";
}

/// <summary>Store-level reservation hold override (null fields inherit platform).</summary>
public sealed record StoreReservationHoldOverrideSnapshot(
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles);

/// <summary>Offer or category reservation hold override snapshot.</summary>
public sealed record ReservationCycleHoldOverrideSnapshot(
    string ScopeKind,
    Guid ScopeId,
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles);

/// <summary>
/// Catalog boundary for reservation-cycle hold overrides — no Catalog DbContext leakage into Order.
/// </summary>
public interface IReservationCycleHoldPolicyReader
{
    /// <summary>Loads the singleton store reservation hold override, or null when absent.</summary>
    Task<StoreReservationHoldOverrideSnapshot?> GetStoreOverrideAsync(CancellationToken cancellationToken);

    /// <summary>Loads offer/category overrides for the given scope ids.</summary>
    Task<IReadOnlyList<ReservationCycleHoldOverrideSnapshot>> GetOverridesAsync(
        IReadOnlyList<Guid> offerIds,
        IReadOnlyList<Guid> categoryIds,
        CancellationToken cancellationToken);
}
