namespace Tooba.Notification.Contracts.Errors;

/// <summary>
/// Stable Notification machine error codes owned by the Notification module boundary.
/// The strings are emitted by Notification Domain/Application/Infrastructure, resolved by the
/// canonical composed error catalog and consumed by foreign modules through the Notification
/// Contracts ports; they must never be renamed or repurposed.
/// </summary>
public static class NotificationErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        Missing,
        TargetRouteEmpty,
        TargetRouteUnsafe,
        TargetRouteNotAllowed,
        RecipientKindInvalid,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code declared by this Notification catalog.
    /// Used by the module composition seam so Notification faults map to <c>Result</c> while codes
    /// owned by another module propagate untouched to the canonical global exception boundary.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>Notification was not found for the recipient scope. HTTP 404.</summary>
    public const string Missing = "notification.missing";

    /// <summary>Deep-link target route must be supplied. HTTP 400 (Business).</summary>
    public const string TargetRouteEmpty = "notification.target_route.empty";

    /// <summary>Deep-link target route failed the safety allow-list. HTTP 400 (Business).</summary>
    public const string TargetRouteUnsafe = "notification.target_route.unsafe";

    /// <summary>Deep-link target route is outside the allowed route prefixes. HTTP 400 (Business).</summary>
    public const string TargetRouteNotAllowed = "notification.target_route.not_allowed";

    /// <summary>Recipient kind value is outside the known Customer/Seller set. HTTP 400 (Business).</summary>
    public const string RecipientKindInvalid = "notification.recipient_kind.invalid";
}

/// <summary>
/// Convenience alias preserving the previously consumed shared code name. The
/// <c>customer.session.required</c> descriptor remains owned and registered exclusively by the
/// foundation catalog (shared cross-cutting code; duplicate usage allowed, single owner).
/// </summary>
public static class NotificationSharedErrorCodes
{
    /// <summary>Authenticated customer session required — Foundation-owned descriptor.</summary>
    public const string CustomerSessionRequired = Tooba.BuildingBlocks.Presentation.Errors.FoundationErrorCodes.CustomerSessionRequired;
}
