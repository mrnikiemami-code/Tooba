using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain.ValueObjects;

/// <summary>
/// Domain type.
/// </summary>
public enum EntryType
{
    /// <summary>بستانکار فروشنده (مثلاً پس از پرداخت).</summary>
    Credit = 0,

    /// <summary>بدهکار فروشنده (مثلاً پس از refund).</summary>
    Debit = 1,
}
