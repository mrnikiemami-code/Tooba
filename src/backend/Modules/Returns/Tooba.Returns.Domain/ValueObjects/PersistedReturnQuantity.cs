namespace Tooba.Returns.Domain.ValueObjects;

/// <summary>
/// TB-272-R1: the exact representable quantity of <c>returns.return_items.quantity</c>.
/// <para>
/// The column is <c>numeric(18,6)</c>, so a value with more than six fractional digits is silently rounded by the
/// provider. That rounding is not a business decision: an accepted request whose quantity cannot be stored exactly
/// would persist a different number of physical units than the caller asked for (0.0000004 becomes 0), and two
/// different raw quantities (1.0000001 and 1.0000002) would both persist as 1.000000 and become indistinguishable.
/// Ordinary creation therefore REFUSES such a quantity instead of rounding it; legitimate fractional quantities
/// (for example 0.000001) and trailing-zero forms (1 vs 1.000000) stay exactly representable and remain equal.
/// </para>
/// </summary>
public static class PersistedReturnQuantity
{
    /// <summary>Fractional scale of <c>returns.return_items.quantity</c>.</summary>
    public const int Scale = 6;

    /// <summary>
    /// True when <paramref name="quantity"/> survives the persisted scale without changing its value, i.e. the value
    /// the provider stores is the value the caller submitted.
    /// </summary>
    public static bool IsExactlyRepresentable(decimal quantity) =>
        quantity == decimal.Round(quantity, Scale, MidpointRounding.AwayFromZero);
}
