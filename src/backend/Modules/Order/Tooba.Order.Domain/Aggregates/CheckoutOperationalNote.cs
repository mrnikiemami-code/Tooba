using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Aggregates;

/// <summary>
/// یادداشت عملیاتی داخلی روی checkout. فقط برای اپراتور؛ soft-delete مجاز طبق قاعدهٔ مشاهده.
/// </summary>
public sealed class CheckoutOperationalNote
{
    /// <summary>سازندهٔ EF.</summary>
    private CheckoutOperationalNote()
    {
    }

    /// <summary>شناسهٔ یادداشت.</summary>
    public Guid NoteId { get; init; }

    /// <summary>checkout مالک.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>متن یادداشت (حداکثر ۲۰۰۰ نویسه).</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>کاربر ثبت‌کننده.</summary>
    public Guid CreatedByUserId { get; init; }

    /// <summary>زمان ثبت UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان soft-delete؛ تهی یعنی فعال.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>کاربر حذف‌کننده.</summary>
    public Guid? DeletedByUserId { get; private set; }

    /// <summary>حداکثر طول مجاز متن.</summary>
    public const int MaxBodyLength = 2000;

    /// <summary>یادداشت می‌سازد.</summary>
    public static CheckoutOperationalNote Create(
        Guid checkoutId,
        Guid createdByUserId,
        string body,
        DateTimeOffset now)
    {
        if (checkoutId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ checkout نامعتبر است.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ کاربر ثبت‌کننده نامعتبر است.");
        }

        var trimmed = (body ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("متن یادداشت خالی است.");
        }

        if (trimmed.Length > MaxBodyLength)
        {
            throw new InvalidOperationException($"متن یادداشت حداکثر {MaxBodyLength} نویسه است.");
        }

        return new CheckoutOperationalNote
        {
            NoteId = UuidV7.New(),
            CheckoutId = checkoutId,
            Body = trimmed,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
        };
    }

    /// <summary>soft-delete توسط نویسنده وقتی قفل نشده باشد.</summary>
    public void SoftDelete(Guid actorUserId, DateTimeOffset now)
    {
        if (DeletedAt is not null)
        {
            throw new InvalidOperationException("یادداشت قبلاً حذف شده است.");
        }

        if (actorUserId != CreatedByUserId)
        {
            throw new InvalidOperationException("فقط نویسنده می‌تواند یادداشت را حذف کند.");
        }

        DeletedAt = now;
        DeletedByUserId = actorUserId;
    }
}

