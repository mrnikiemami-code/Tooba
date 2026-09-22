namespace Tooba.Order.Application.Admin.Supply.Models;

/// <summary>حالت فراخوانی EnsureOrderSupply — انتخاب use-case نه UI.</summary>
public enum OrderSupplyMode
{
    CheckOnly = 0,
    EnsureReviewHold = 1,
    EnsurePaidDurable = 2,
    EnsureFulfillmentSupply = 3,
    EnsureUnpaidRetryHold = 4,
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

/// <summary>وضعیت تأمین فقط-خواندنی.</summary>
public sealed record OrderSupplyStatus(
    Guid CheckoutId,
    OrderSupplyStatusKind Status,
    IReadOnlyList<OrderSupplyLineShortage> Lines);

/// <summary>نتیجهٔ EnsureOrderSupply.</summary>
public sealed record EnsureOrderSupplyResult(
    OrderSupplyOutcome Outcome,
    OrderSupplyStatusKind Status,
    IReadOnlyList<OrderSupplyLineShortage> Lines,
    IReadOnlyDictionary<Guid, Guid> NewBindingsByOrderLineId);

/// <summary>پیام‌های پایدار UX تأمین.</summary>
public static class OrderSupplyMessages
{
    /// <summary>متن فارسی وضعیت تأمین.</summary>
    public static string MessageFa(OrderSupplyStatusKind status) =>
        status switch
        {
            OrderSupplyStatusKind.Reserved => "موجودی موردنیاز این سفارش رزرو شده است.",
            OrderSupplyStatusKind.AvailableForReacquire =>
                "رزرو قبلی فعال نیست، اما موجودی لازم در حال حاضر قابل تأمین است.",
            OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable =>
                "یک یا چند قلم این سفارش در حال حاضر قابل تأمین نیست.",
            OrderSupplyStatusKind.Fulfilled => "موجودی موردنیاز این سفارش تأمین و تکمیل شده است.",
            _ => "تأمین موجودی برای این سفارش موضوعیت ندارد.",
        };
}
