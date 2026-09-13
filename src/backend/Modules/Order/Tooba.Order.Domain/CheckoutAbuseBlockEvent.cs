using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain;

/// <summary>ممیزی مسدود شدن تسویه به‌خاطر سقف سفارش باز یا سهمیه رزرو.</summary>
public sealed class CheckoutAbuseBlockEvent
{
    /// <summary>شناسه رویداد.</summary>
    public Guid EventId { get; private set; }

    /// <summary>فروشگاه.</summary>
    public Guid StoreId { get; private set; }

    /// <summary>مشتری.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>open_unpaid یا reservation_commit.</summary>
    public string Kind { get; private set; } = "open_unpaid";

    /// <summary>شمارش مشاهده‌شده.</summary>
    public int CurrentCount { get; private set; }

    /// <summary>سقف تنظیم.</summary>
    public int MaxCount { get; private set; }

    /// <summary>زمان رخداد.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>ردیف ممیزی مسدود را می‌سازد.</summary>
    public static CheckoutAbuseBlockEvent Create(
        Guid storeId,
        Guid customerId,
        string kind,
        int currentCount,
        int maxCount,
        DateTimeOffset occurredAt) =>
        new()
        {
            EventId = UuidV7.New(),
            StoreId = storeId,
            CustomerId = customerId,
            Kind = kind,
            CurrentCount = currentCount,
            MaxCount = maxCount,
            OccurredAt = occurredAt,
        };
}
