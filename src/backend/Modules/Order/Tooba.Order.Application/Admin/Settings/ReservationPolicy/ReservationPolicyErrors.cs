namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy;

/// <summary>Stable machine codes for Admin/Seller reservation-policy settings.</summary>
public static class ReservationPolicyErrors
{
    /// <summary>Invalid initial hold minutes.</summary>
    public const string InitialInvalid = "reservation.policy.initial.invalid";

    /// <summary>Invalid retry hold minutes.</summary>
    public const string RetryInvalid = "reservation.policy.retry.invalid";

    /// <summary>Invalid max cycles.</summary>
    public const string MaxInvalid = "reservation.policy.max.invalid";

    /// <summary>Seller mutate denied (no catalog permission).</summary>
    public const string SellerDenied = "reservation.policy.seller.denied";

    /// <summary>Explicit seller mutate permission id (absent from AccessControl catalog).</summary>
    public const string SellerMutatePermission = "reservation.policy.mutate";
}
