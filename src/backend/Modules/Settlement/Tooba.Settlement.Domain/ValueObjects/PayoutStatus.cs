using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain.ValueObjects;

/// <summary>
/// Domain type.
/// </summary>
public enum PayoutStatus
{
    /// <summary>در انتظار پردازش.</summary>
    Pending = 0,

    /// <summary>در حال ارسال به درگاه.</summary>
    Processing = 1,

    /// <summary>موفق.</summary>
    Succeeded = 2,

    /// <summary>شکست.</summary>
    Failed = 3,
}
