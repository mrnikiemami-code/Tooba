using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain;

/// <summary>
/// رویداد تغییرناپذیر شروع موفق رزرو Cycle #1. لغو/پنهان/پرداخت آن را حذف نمی‌کند.
/// </summary>
public sealed class CheckoutReservationCommit
{
    /// <summary>شناسه رویداد.</summary>
    public Guid EventId { get; private set; }

    /// <summary>فروشگاه.</summary>
    public Guid StoreId { get; private set; }

    /// <summary>مشتری احرازشده.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>سفارش فروشندهٔ مرجع.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>گروه تسویه.</summary>
    public Guid CheckoutId { get; private set; }

    /// <summary>همیشه ۱ برای این سیاست.</summary>
    public int ReservationCycleNumber { get; private set; }

    /// <summary>زمان رخداد.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>منبع عمل.</summary>
    public string Source { get; private set; } = "order-commit";

    /// <summary>رویداد Cycle #1 را می‌سازد.</summary>
    public static CheckoutReservationCommit Create(
        Guid storeId,
        Guid customerId,
        Guid orderId,
        Guid checkoutId,
        DateTimeOffset occurredAt,
        string source = "order-commit") =>
        new()
        {
            EventId = UuidV7.New(),
            StoreId = storeId,
            CustomerId = customerId,
            OrderId = orderId,
            CheckoutId = checkoutId,
            ReservationCycleNumber = 1,
            OccurredAt = occurredAt,
            Source = string.IsNullOrWhiteSpace(source) ? "order-commit" : source.Trim(),
        };
}
