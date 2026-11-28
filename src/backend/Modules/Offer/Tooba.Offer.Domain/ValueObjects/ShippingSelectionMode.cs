namespace Tooba.Offer.Domain.ValueObjects;

/// <summary>
/// Explicit offer-owned shipping method selection mode.
/// </summary>
/// <remarks>
/// <see cref="Automatic"/> means "every currently eligible method of the assigned owner/type", and carries
/// no link rows. <see cref="Specific"/> means "exactly the explicitly selected method identities" and is
/// only lawful with a nonempty set. A legacy offer without a stored value is treated as Automatic by
/// readers; nothing is inferred from the seller id or a method name.
/// </remarks>
public enum ShippingSelectionMode
{
    /// <summary>All eligible methods of the assigned owner and shipping type.</summary>
    Automatic = 0,

    /// <summary>Exactly the explicitly selected method identities.</summary>
    Specific = 1,
}
