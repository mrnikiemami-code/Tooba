using Tooba.BuildingBlocks;

namespace Tooba.Notification.Domain;

/// <summary>
/// نوع گیرندهٔ اعلان تراکنشی. با نقش Identity یکی نیست.
/// </summary>
public enum NotificationRecipientKind
{
    /// <summary>خریدار / مشتری.</summary>
    Customer = 1,

    /// <summary>فروشندهٔ مالک سفارش یا رویداد.</summary>
    Seller = 2,
}

/// <summary>
/// اعلان پایدار تراکنشی. لاگ فنی، audit یا analytics نیست.
/// Tenant از اتصال commerce محیط جاری جدا می‌شود؛ ردیف TenantId نگه نمی‌دارد
/// (هم‌تراز Reviews/Settlement؛ Story استثنا است).
/// </summary>
public sealed class UserNotification
{
    private UserNotification()
    {
    }

    /// <summary>شناسهٔ پایدار اعلان.</summary>
    public Guid NotificationId { get; init; }

    /// <summary>نوع گیرنده.</summary>
    public NotificationRecipientKind RecipientKind { get; init; }

    /// <summary>
    /// کلید اصلی گیرنده: Party خریدار (یا surrogate بازیگر وقتی BuyerPartyId خالی است)
    /// یا Party فروشنده.
    /// </summary>
    public Guid RecipientPartyId { get; init; }

    /// <summary>Actor اختیاری برای فیلتر پنل مشتری.</summary>
    public Guid? RecipientActorUserId { get; init; }

    /// <summary>نوع معنایی پایدار (مثلاً payment.succeeded).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>JSON ساختاریافتهٔ امن بدون HTML.</summary>
    public string PayloadJson { get; init; } = "{}";

    /// <summary>مسیر نسبی allowlist‌شده.</summary>
    public string TargetRoute { get; init; } = string.Empty;

    /// <summary>آیا خوانده شده.</summary>
    public bool IsRead { get; private set; }

    /// <summary>زمان خواندن.</summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>کلید idempotency از EventId منبع.</summary>
    public string SourceEventId { get; init; } = string.Empty;

    /// <summary>نوع قرارداد Integration منبع.</summary>
    public string SourceType { get; init; } = string.Empty;

    /// <summary>حذف نرم.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>زمان حذف نرم.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>اعلان خوانده‌نشدهٔ جدید می‌سازد.</summary>
    public static UserNotification Create(
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        string type,
        string payloadJson,
        string targetRoute,
        string sourceEventId,
        string sourceType,
        DateTimeOffset now)
    {
        if (recipientPartyId == Guid.Empty)
        {
            throw new InvalidOperationException("گیرندهٔ اعلان الزامی است.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new InvalidOperationException("نوع اعلان الزامی است.");
        }

        if (string.IsNullOrWhiteSpace(sourceEventId))
        {
            throw new InvalidOperationException("SourceEventId برای idempotency الزامی است.");
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new InvalidOperationException("SourceType الزامی است.");
        }

        if (string.IsNullOrWhiteSpace(targetRoute) || !targetRoute.StartsWith('/'))
        {
            throw new InvalidOperationException("TargetRoute باید مسیر نسبی امن باشد.");
        }

        return new UserNotification
        {
            NotificationId = UuidV7.New(),
            RecipientKind = recipientKind,
            RecipientPartyId = recipientPartyId,
            RecipientActorUserId = recipientActorUserId,
            Type = type.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson.Trim(),
            TargetRoute = targetRoute.Trim(),
            IsRead = false,
            CreatedAt = now,
            SourceEventId = sourceEventId.Trim(),
            SourceType = sourceType.Trim(),
        };
    }

    /// <summary>خوانده‌شدن را به‌صورت idempotent علامت می‌زند.</summary>
    public bool MarkRead(DateTimeOffset now)
    {
        if (IsDeleted)
        {
            return false;
        }

        if (IsRead)
        {
            return false;
        }

        IsRead = true;
        ReadAt = now;
        return true;
    }

    /// <summary>حذف نرم idempotent.</summary>
    public bool SoftDelete(DateTimeOffset now)
    {
        if (IsDeleted)
        {
            return false;
        }

        IsDeleted = true;
        DeletedAt = now;
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = now;
        }

        return true;
    }
}
