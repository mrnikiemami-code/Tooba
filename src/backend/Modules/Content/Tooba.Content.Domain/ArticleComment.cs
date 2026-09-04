using Tooba.BuildingBlocks;

namespace Tooba.Content.Domain;

/// <summary>وضعیت تعدیل نظر مقاله.</summary>
public enum ArticleCommentStatus
{
    /// <summary>در انتظار بررسی.</summary>
    Pending = 0,
    /// <summary>تأییدشده و قابل نمایش عمومی (در صورت وجود سطح عمومی).</summary>
    Approved = 1,
    /// <summary>ردشده.</summary>
    Rejected = 2,
    /// <summary>پنهان اداری بدون حذف تاریخچه.</summary>
    Hidden = 3,
}

/// <summary>کدهای پایدار خطای تعدیل نظر مقاله.</summary>
public static class ArticleCommentCodes
{
    /// <summary>نظر یافت نشد.</summary>
    public const string NotFound = "content.comment.not_found";
    /// <summary>مقالهٔ مالک یافت نشد.</summary>
    public const string ArticleNotFound = "content.comment.article_not_found";
    /// <summary>انتقال وضعیت نامعتبر.</summary>
    public const string InvalidTransition = "content.comment.invalid_transition";
    /// <summary>بدنه یا نام نمایشی نامعتبر.</summary>
    public const string InvalidPayload = "content.comment.invalid_payload";
    /// <summary>تعدیل مجاز نیست.</summary>
    public const string Forbidden = "content.comment.forbidden";
}

/// <summary>نظر متعلق به یک مقالهٔ Content — مالکیت Content؛ بدون ناوبری ORM بین‌ماژولی.</summary>
public sealed class ArticleComment
{
    /// <summary>سقف طول نام نمایشی.</summary>
    public const int DisplayNameMaxLength = 120;
    /// <summary>سقف طول بدنه.</summary>
    public const int BodyMaxLength = 4000;
    /// <summary>سقف یادداشت تعدیل.</summary>
    public const int NoteMaxLength = 500;

    private ArticleComment() { }

    /// <summary>شناسهٔ پایدار UUID v7.</summary>
    public Guid CommentId { get; init; }

    /// <summary>مقالهٔ مالک.</summary>
    public Guid ArticleId { get; init; }

    /// <summary>مرجع اختیاری کاربر/طرف از طریق قرارداد (بدون ORM cross-module).</summary>
    public Guid? AuthorPartyId { get; init; }

    /// <summary>تصویر ثابت نام نمایشی برای UI.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>متن نظر.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>وضعیت تعدیل.</summary>
    public ArticleCommentStatus Status { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین تعدیل UTC.</summary>
    public DateTimeOffset? ModeratedAt { get; private set; }

    /// <summary>شناسهٔ تعدیل‌کننده (opaque).</summary>
    public Guid? ModeratedByUserId { get; private set; }

    /// <summary>یادداشت/دلیل اختیاری تعدیل.</summary>
    public string? ModerationNote { get; private set; }

    /// <summary>نظر Pending جدید می‌سازد.</summary>
    public static ArticleComment Create(
        Guid articleId,
        string displayName,
        string body,
        DateTimeOffset now,
        Guid? authorPartyId = null)
    {
        if (articleId == Guid.Empty)
            throw new InvalidOperationException($"{ArticleCommentCodes.InvalidPayload}:article");
        var name = NormalizeRequired(displayName, DisplayNameMaxLength, "displayName");
        var text = NormalizeRequired(body, BodyMaxLength, "body");
        return new ArticleComment
        {
            CommentId = UuidV7.New(),
            ArticleId = articleId,
            AuthorPartyId = authorPartyId == Guid.Empty ? null : authorPartyId,
            DisplayName = name,
            Body = text,
            Status = ArticleCommentStatus.Pending,
            CreatedAt = now,
        };
    }

    /// <summary>تأیید نظر.</summary>
    public void Approve(Guid moderatorUserId, DateTimeOffset now, string? note = null)
    {
        EnsureModerator(moderatorUserId);
        EnsureTransition(ArticleCommentStatus.Approved);
        ApplyModeration(ArticleCommentStatus.Approved, moderatorUserId, now, note);
    }

    /// <summary>رد نظر.</summary>
    public void Reject(Guid moderatorUserId, DateTimeOffset now, string? note = null)
    {
        EnsureModerator(moderatorUserId);
        EnsureTransition(ArticleCommentStatus.Rejected);
        ApplyModeration(ArticleCommentStatus.Rejected, moderatorUserId, now, note);
    }

    /// <summary>پنهان‌سازی اداری.</summary>
    public void Hide(Guid moderatorUserId, DateTimeOffset now, string? note = null)
    {
        EnsureModerator(moderatorUserId);
        EnsureTransition(ArticleCommentStatus.Hidden);
        ApplyModeration(ArticleCommentStatus.Hidden, moderatorUserId, now, note);
    }

    /// <summary>بازگردانی به انتظار بررسی.</summary>
    public void MarkPending(Guid moderatorUserId, DateTimeOffset now, string? note = null)
    {
        EnsureModerator(moderatorUserId);
        EnsureTransition(ArticleCommentStatus.Pending);
        ApplyModeration(ArticleCommentStatus.Pending, moderatorUserId, now, note);
    }

    private void ApplyModeration(ArticleCommentStatus next, Guid moderatorUserId, DateTimeOffset now, string? note)
    {
        Status = next;
        ModeratedByUserId = moderatorUserId;
        ModeratedAt = now;
        ModerationNote = string.IsNullOrWhiteSpace(note) ? null : Truncate(note.Trim(), NoteMaxLength);
    }

    private void EnsureTransition(ArticleCommentStatus next)
    {
        if (Status == next)
            throw new InvalidOperationException($"{ArticleCommentCodes.InvalidTransition}:{Status}->{next}");

        var allowed = Status switch
        {
            ArticleCommentStatus.Pending => next is ArticleCommentStatus.Approved
                or ArticleCommentStatus.Rejected
                or ArticleCommentStatus.Hidden,
            ArticleCommentStatus.Approved => next is ArticleCommentStatus.Hidden
                or ArticleCommentStatus.Rejected
                or ArticleCommentStatus.Pending,
            ArticleCommentStatus.Rejected => next is ArticleCommentStatus.Pending
                or ArticleCommentStatus.Approved
                or ArticleCommentStatus.Hidden,
            ArticleCommentStatus.Hidden => next is ArticleCommentStatus.Pending
                or ArticleCommentStatus.Approved
                or ArticleCommentStatus.Rejected,
            _ => false,
        };
        if (!allowed)
            throw new InvalidOperationException($"{ArticleCommentCodes.InvalidTransition}:{Status}->{next}");
    }

    private static void EnsureModerator(Guid moderatorUserId)
    {
        if (moderatorUserId == Guid.Empty)
            throw new InvalidOperationException(ArticleCommentCodes.Forbidden);
    }

    private static string NormalizeRequired(string value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{ArticleCommentCodes.InvalidPayload}:{field}");
        var trimmed = value.Trim();
        if (trimmed.Length > max)
            throw new InvalidOperationException($"{ArticleCommentCodes.InvalidPayload}:{field}");
        return trimmed;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
