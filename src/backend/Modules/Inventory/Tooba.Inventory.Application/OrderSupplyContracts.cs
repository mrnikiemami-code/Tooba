#pragma warning disable CS1591
using Tooba.Inventory.Domain;

namespace Tooba.Inventory.Application;

/// <summary>حالت فراخوانی EnsureOrderSupply — انتخاب use-case نه UI.</summary>
public enum OrderSupplyMode
{
    CheckOnly = 0,
    EnsureReviewHold = 1,
    EnsurePaidDurable = 2,
    EnsureFulfillmentSupply = 3,
}

/// <summary>نتیجهٔ کلی Ensure (mutative یا check).</summary>
public enum OrderSupplyOutcome
{
    AlreadyReserved = 0,
    Reacquired = 1,
    Unavailable = 2,
    PartiallyUnavailable = 3,
    NotApplicable = 4,
    Conflict = 5,
}

/// <summary>وضعیت کسب‌وکاری تأمین سفارش (جدا از Payment/Order status).</summary>
public enum OrderSupplyStatusKind
{
    Reserved = 0,
    AvailableForReacquire = 1,
    Unavailable = 2,
    PartiallyUnavailable = 3,
    Fulfilled = 4,
    NotApplicable = 5,
}

/// <summary>خط ورودی Ensure/Check — Inventory به UI وابسته نیست.</summary>
public sealed record OrderSupplyLineInput(
    Guid OrderLineId,
    Guid OfferId,
    Guid? CurrentReservationId,
    decimal RemainingQuantity,
    string? ItemTitle,
    string? UnitCode);

/// <summary>درخواست EnsureOrderSupply.</summary>
public sealed record EnsureOrderSupplyRequest(
    Guid CheckoutId,
    OrderSupplyMode Mode,
    bool AllowReacquire,
    string Reason,
    string? CorrelationId,
    DateTimeOffset? ReviewExpiresAt,
    IReadOnlyList<OrderSupplyLineInput> Lines);

/// <summary>تشخیص کمبود یک خط.</summary>
public sealed record OrderSupplyLineShortage(
    Guid OrderLineId,
    string? ItemTitle,
    string? UnitCode,
    decimal Required,
    decimal Available,
    decimal Shortage,
    OrderSupplyStatusKind LineStatus,
    Guid? BoundReservationId);

/// <summary>نتیجهٔ EnsureOrderSupply.</summary>
public sealed record EnsureOrderSupplyResult(
    OrderSupplyOutcome Outcome,
    OrderSupplyStatusKind Status,
    IReadOnlyList<OrderSupplyLineShortage> Lines,
    IReadOnlyDictionary<Guid, Guid> NewBindingsByOrderLineId);

/// <summary>وضعیت تأمین فقط-خواندنی (CheckOnly).</summary>
public sealed record OrderSupplyStatus(
    Guid CheckoutId,
    OrderSupplyStatusKind Status,
    IReadOnlyList<OrderSupplyLineShortage> Lines);
