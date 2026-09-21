using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain.ValueObjects;

/// <summary>
/// Domain type.
/// </summary>
public enum StatementStatus
{
    /// <summary>دوره باز و قابل جمع‌بندی.</summary>
    Open = 0,

    /// <summary>دوره بسته شده.</summary>
    Closed = 1,
}
