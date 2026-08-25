using Tooba.BuildingBlocks;

namespace Tooba.Reviews.Domain;

/// <summary>وضعیت چرخهٔ بررسی محصول؛ فقط Published برای عموم قابل مشاهده است.</summary>
public enum ReviewStatus
{
    /// <summary>در انتظار تصمیم مدیر.</summary>
    Pending = 0,
    /// <summary>منتشرشده برای نمایش عمومی.</summary>
    Published = 1,
    /// <summary>ردشده و غیرقابل نمایش عمومی.</summary>
    Rejected = 2,
}

/// <summary>بررسی محصول متعلق به یک کاربر احرازشده با تصویر ثابت خرید تأییدشده.</summary>
public sealed class ProductReview
{
    private ProductReview() { }

    /// <summary>شناسهٔ پایدار بررسی.</summary>
    public Guid ReviewId { get; init; }
    /// <summary>مرجع opaque محصول در Catalog.</summary>
    public Guid ProductId { get; init; }
    /// <summary>شناسهٔ داخلی نویسنده که هرگز در DTO عمومی قرار نمی‌گیرد.</summary>
    public Guid AuthorUserId { get; init; }
    /// <summary>نام امن و عمومی نویسنده.</summary>
    public string AuthorDisplayName { get; private set; } = string.Empty;
    /// <summary>امتیاز صحیح بین یک تا پنج.</summary>
    public int Rating { get; private set; }
    /// <summary>عنوان اختیاری بررسی.</summary>
    public string? Title { get; private set; }
    /// <summary>متن بررسی.</summary>
    public string Body { get; private set; } = string.Empty;
    /// <summary>وضعیت تعدیل محتوا.</summary>
    public ReviewStatus Status { get; private set; }
    /// <summary>تصویر ثابت نتیجهٔ اثبات خرید در زمان ثبت.</summary>
    public bool IsVerifiedPurchase { get; init; }
    /// <summary>شناسهٔ opaque سفارش اثبات‌کننده، در صورت وجود.</summary>
    public Guid? VerificationOrderId { get; init; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین تغییر UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }
    /// <summary>مدیر انجام‌دهندهٔ آخرین تعدیل؛ عمومی نیست.</summary>
    public Guid? ModeratedByUserId { get; private set; }
    /// <summary>زمان تعدیل UTC.</summary>
    public DateTimeOffset? ModeratedAt { get; private set; }
    /// <summary>دلیل داخلی رد؛ عمومی نیست.</summary>
    public string? ModerationReason { get; private set; }

    /// <summary>بررسی Pending معتبر می‌سازد.</summary>
    public static ProductReview Create(Guid productId, Guid authorUserId, string authorDisplayName, int rating,
        string? title, string body, bool verified, Guid? verificationOrderId, DateTimeOffset now)
    {
        if (productId == Guid.Empty || authorUserId == Guid.Empty) throw new InvalidOperationException("هویت محصول و نویسنده الزامی است.");
        if (rating is < 1 or > 5) throw new InvalidOperationException("امتیاز باید بین ۱ و ۵ باشد.");
        if (string.IsNullOrWhiteSpace(authorDisplayName) || authorDisplayName.Trim().Length > 100) throw new InvalidOperationException("نام نمایشی معتبر نیست.");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 4000) throw new InvalidOperationException("متن بررسی معتبر نیست.");
        if (title?.Trim().Length > 200) throw new InvalidOperationException("عنوان بررسی بیش از حد بلند است.");
        return new ProductReview
        {
            ReviewId = UuidV7.New(), ProductId = productId, AuthorUserId = authorUserId,
            AuthorDisplayName = authorDisplayName.Trim(), Rating = rating,
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(), Body = body.Trim(),
            Status = ReviewStatus.Pending, IsVerifiedPurchase = verified,
            VerificationOrderId = verified ? verificationOrderId : null, CreatedAt = now, UpdatedAt = now,
        };
    }

    /// <summary>بررسی Pending را با ثبت ممیزی منتشر می‌کند.</summary>
    public void Publish(Guid moderatorUserId, DateTimeOffset now)
    {
        EnsurePending(); Status = ReviewStatus.Published; ModeratedByUserId = moderatorUserId;
        ModeratedAt = now; ModerationReason = null; UpdatedAt = now;
    }

    /// <summary>بررسی Pending را با دلیل اجباری رد می‌کند.</summary>
    public void Reject(Guid moderatorUserId, string reason, DateTimeOffset now)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new InvalidOperationException("دلیل رد معتبر نیست.");
        Status = ReviewStatus.Rejected; ModeratedByUserId = moderatorUserId; ModeratedAt = now;
        ModerationReason = reason.Trim(); UpdatedAt = now;
    }

    private void EnsurePending()
    {
        if (Status != ReviewStatus.Pending) throw new InvalidOperationException("فقط بررسی Pending قابل تعدیل است.");
    }
}
