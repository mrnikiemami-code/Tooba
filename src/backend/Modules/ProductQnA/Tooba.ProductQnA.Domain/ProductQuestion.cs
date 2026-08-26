using Tooba.BuildingBlocks;

namespace Tooba.ProductQnA.Domain;

/// <summary>وضعیت چرخهٔ پرسش محصول؛ فقط Published برای عموم قابل مشاهده است.</summary>
public enum ProductQuestionStatus
{
    /// <summary>در انتظار تصمیم مدیر.</summary>
    Pending = 0,
    /// <summary>منتشرشده برای نمایش عمومی.</summary>
    Published = 1,
    /// <summary>ردشده و غیرقابل نمایش عمومی.</summary>
    Rejected = 2,
}

/// <summary>وضعیت پاسخ پرسش؛ فقط Published در PDP نمایش داده می‌شود.</summary>
public enum ProductAnswerStatus
{
    /// <summary>در انتظار تصمیم مدیر.</summary>
    Pending = 0,
    /// <summary>منتشرشده برای نمایش عمومی.</summary>
    Published = 1,
}

/// <summary>پرسش مشتری دربارهٔ یک محصول منتشرشده.</summary>
public sealed class ProductQuestion
{
    /// <summary>حداکثر طول متن پرسش.</summary>
    public const int BodyMaxLength = 2000;
    /// <summary>حداکثر طول نام نمایشی نویسنده.</summary>
    public const int AuthorDisplayNameMaxLength = 100;

    private ProductQuestion() { }

    /// <summary>شناسهٔ پایدار پرسش.</summary>
    public Guid QuestionId { get; init; }
    /// <summary>مرجع opaque محصول در Catalog.</summary>
    public Guid ProductId { get; init; }
    /// <summary>شناسهٔ داخلی نویسنده که هرگز در DTO عمومی قرار نمی‌گیرد.</summary>
    public Guid AuthorUserId { get; init; }
    /// <summary>نام امن و عمومی نویسنده.</summary>
    public string AuthorDisplayName { get; private set; } = string.Empty;
    /// <summary>متن پرسش.</summary>
    public string Body { get; private set; } = string.Empty;
    /// <summary>وضعیت تعدیل محتوا.</summary>
    public ProductQuestionStatus Status { get; private set; }
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

    /// <summary>پرسش Pending معتبر می‌سازد.</summary>
    public static ProductQuestion Create(Guid productId, Guid authorUserId, string authorDisplayName, string body, DateTimeOffset now)
    {
        if (productId == Guid.Empty || authorUserId == Guid.Empty) throw new InvalidOperationException("هویت محصول و نویسنده الزامی است.");
        if (string.IsNullOrWhiteSpace(authorDisplayName) || authorDisplayName.Trim().Length > AuthorDisplayNameMaxLength)
            throw new InvalidOperationException("نام نمایشی معتبر نیست.");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > BodyMaxLength) throw new InvalidOperationException("متن پرسش معتبر نیست.");
        return new ProductQuestion
        {
            QuestionId = UuidV7.New(), ProductId = productId, AuthorUserId = authorUserId,
            AuthorDisplayName = authorDisplayName.Trim(), Body = body.Trim(),
            Status = ProductQuestionStatus.Pending, CreatedAt = now, UpdatedAt = now,
        };
    }

    /// <summary>پرسش Pending را با ثبت ممیزی منتشر می‌کند.</summary>
    public void Publish(Guid moderatorUserId, DateTimeOffset now)
    {
        EnsurePending();
        Status = ProductQuestionStatus.Published;
        ModeratedByUserId = moderatorUserId;
        ModeratedAt = now;
        ModerationReason = null;
        UpdatedAt = now;
    }

    /// <summary>پرسش Pending را با دلیل اجباری رد می‌کند.</summary>
    public void Reject(Guid moderatorUserId, string reason, DateTimeOffset now)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new InvalidOperationException("دلیل رد معتبر نیست.");
        Status = ProductQuestionStatus.Rejected;
        ModeratedByUserId = moderatorUserId;
        ModeratedAt = now;
        ModerationReason = reason.Trim();
        UpdatedAt = now;
    }

    private void EnsurePending()
    {
        if (Status != ProductQuestionStatus.Pending) throw new InvalidOperationException("فقط پرسش Pending قابل تعدیل است.");
    }
}

/// <summary>پاسخ مدیر یا فروشنده به یک پرسش محصول.</summary>
public sealed class ProductAnswer
{
    /// <summary>حداکثر طول متن پاسخ.</summary>
    public const int BodyMaxLength = 2000;
    /// <summary>حداکثر طول نام نمایشی نویسنده.</summary>
    public const int AuthorDisplayNameMaxLength = 100;

    private ProductAnswer() { }

    /// <summary>شناسهٔ پایدار پاسخ.</summary>
    public Guid AnswerId { get; init; }
    /// <summary>مرجع پرسش والد.</summary>
    public Guid QuestionId { get; init; }
    /// <summary>نام امن و عمومی نویسنده پاسخ.</summary>
    public string AuthorDisplayName { get; private set; } = string.Empty;
    /// <summary>متن پاسخ.</summary>
    public string Body { get; private set; } = string.Empty;
    /// <summary>وضعیت تعدیل محتوا.</summary>
    public ProductAnswerStatus Status { get; private set; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>پاسخ Pending معتبر می‌سازد.</summary>
    public static ProductAnswer Create(Guid questionId, string authorDisplayName, string body, DateTimeOffset now)
    {
        if (questionId == Guid.Empty) throw new InvalidOperationException("شناسهٔ پرسش الزامی است.");
        if (string.IsNullOrWhiteSpace(authorDisplayName) || authorDisplayName.Trim().Length > AuthorDisplayNameMaxLength)
            throw new InvalidOperationException("نام نمایشی معتبر نیست.");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > BodyMaxLength) throw new InvalidOperationException("متن پاسخ معتبر نیست.");
        return new ProductAnswer
        {
            AnswerId = UuidV7.New(), QuestionId = questionId,
            AuthorDisplayName = authorDisplayName.Trim(), Body = body.Trim(),
            Status = ProductAnswerStatus.Pending, CreatedAt = now,
        };
    }

    /// <summary>پاسخ Pending را منتشر می‌کند.</summary>
    public void Publish()
    {
        if (Status != ProductAnswerStatus.Pending) throw new InvalidOperationException("فقط پاسخ Pending قابل انتشار است.");
        Status = ProductAnswerStatus.Published;
    }
}
