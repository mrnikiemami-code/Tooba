namespace Tooba.Returns.Domain.ValueObjects;

/// <summary>
/// TB-277-R1: the OWNING UNIT quantum of a physically received quantity.
/// <para>
/// A return quantity is not only bounded by the persisted column (<c>numeric(18,6)</c>) — it is a number of things in
/// the unit the customer actually bought. A piece-count line accepts <c>1</c> but never <c>0.5</c>; a gram line with
/// three decimals accepts <c>0.125</c> but never <c>0.0001</c>. The persisted scale is a storage cap, never a unit
/// definition, so this rule is applied on top of <see cref="PersistedReturnQuantity"/> and refuses a mis-aligned
/// amount instead of rounding or clamping it.
/// </para>
/// <para>
/// Comparison is by DECIMAL VALUE, never by representation or scale: <c>1</c> and <c>1.000000</c> are the same whole
/// piece, so the trailing-zero form is accepted and stays equal to the stored quantity.
/// </para>
/// </summary>
public static class OwningUnitQuantity
{
    /// <summary>The widest unit precision the persisted quantity can represent at all.</summary>
    public const int MaxDecimalPlaces = PersistedReturnQuantity.Scale;

    /// <summary>
    /// True when <paramref name="quantity"/> is a positive, exactly persisted amount that lands exactly on the owning
    /// unit's quantum (<c>10^-decimalPlaces</c>). An unknown/out-of-range precision answers <c>false</c> rather than
    /// falling back to any default.
    /// </summary>
    public static bool IsAlignedTo(decimal quantity, int decimalPlaces)
    {
        if (decimalPlaces is < 0 or > MaxDecimalPlaces)
        {
            return false;
        }

        if (quantity <= 0m || !PersistedReturnQuantity.IsExactlyRepresentable(quantity))
        {
            return false;
        }

        // Scaling by the unit's own power of ten must land on an integer: 0.5 * 10^0 = 0.5 is refused, while
        // 1.000000 * 10^0 = 1.000000 == 1 stays lawful.
        var scaled = quantity * Pow10(decimalPlaces);
        return scaled == decimal.Truncate(scaled);
    }

    /// <summary>
    /// TB-278: the CREATION-time form of the same rule, used before a return request line is persisted.
    /// <para>
    /// When the owning order line answered its unit precision, the quantity must land exactly on that unit's quantum.
    /// When it did not answer, a fractional quantity is still refused (no default scale is invented) while a WHOLE
    /// quantity stays lawful — a whole piece is the same physical amount under every unit precision, so an unanswered
    /// metadata line must not block an ordinary customer return of intact items. The fractional part is never rounded
    /// or clamped away.
    /// </para>
    /// </summary>
    public static bool IsCreationQuantityAcceptable(decimal quantity, int? decimalPlaces)
    {
        if (decimalPlaces is not null)
        {
            return IsAlignedTo(quantity, decimalPlaces.Value);
        }

        return quantity > 0m
            && PersistedReturnQuantity.IsExactlyRepresentable(quantity)
            && quantity == decimal.Truncate(quantity);
    }

    private static decimal Pow10(int exponent)
    {
        var value = 1m;
        for (var i = 0; i < exponent; i++)
        {
            value *= 10m;
        }

        return value;
    }
}
