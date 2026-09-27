namespace Tooba.Catalog.Contracts.Reservation;

/// <summary>Store-level cart/payment hold-hour overrides (not reservation-cycle fields).</summary>
public sealed record StoreHoldPolicyHoursSnapshot(
    int? CartPersistenceHours,
    int? OnlinePaymentHoldHours,
    int? ManualPaymentInitialHoldHours,
    int? ManualPaymentReviewHoldHours);

/// <summary>Write payload for store cart/payment hold-hour overrides.</summary>
public sealed record StoreHoldPolicyHoursWrite(
    int? CartPersistenceHours,
    int? OnlinePaymentHoldHours,
    int? ManualPaymentInitialHoldHours,
    int? ManualPaymentReviewHoldHours);

/// <summary>
/// Catalog persistence boundary for StoreHoldPolicySettings cart/payment hour fields.
/// Reservation-cycle fields use <see cref="IStoreReservationPolicySettingsPort"/>.
/// </summary>
public interface IStoreHoldPolicySettingsPort
{
    /// <summary>Reads store cart/payment hold-hour overrides (null = inherit platform).</summary>
    Task<StoreHoldPolicyHoursSnapshot> GetHoursAsync(CancellationToken cancellationToken);

    /// <summary>Writes store cart/payment hold-hour overrides; creates singleton row when missing.</summary>
    Task SaveHoursAsync(StoreHoldPolicyHoursWrite write, CancellationToken cancellationToken);
}
