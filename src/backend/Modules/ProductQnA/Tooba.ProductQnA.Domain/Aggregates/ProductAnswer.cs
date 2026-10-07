using Tooba.BuildingBlocks;
using Tooba.ProductQnA.Contracts.Errors;
using Tooba.ProductQnA.Domain.Enums;

namespace Tooba.ProductQnA.Domain.Aggregates;

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
        if (questionId == Guid.Empty)
            throw Rejected();
        if (string.IsNullOrWhiteSpace(authorDisplayName) || authorDisplayName.Trim().Length > AuthorDisplayNameMaxLength)
            throw Rejected();
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > BodyMaxLength)
            throw Rejected();
        return new ProductAnswer
        {
            AnswerId = UuidV7.New(),
            QuestionId = questionId,
            AuthorDisplayName = authorDisplayName.Trim(),
            Body = body.Trim(),
            Status = ProductAnswerStatus.Pending,
            CreatedAt = now,
        };
    }

    /// <summary>پاسخ Pending را منتشر می‌کند.</summary>
    public void Publish()
    {
        if (Status != ProductAnswerStatus.Pending)
            throw Rejected();
        Status = ProductAnswerStatus.Published;
    }

    private static ContractOperationException Rejected() =>
        new(ProductQnAErrorCodes.Rejected);
}
