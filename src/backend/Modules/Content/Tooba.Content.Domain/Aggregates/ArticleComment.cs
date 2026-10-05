using Tooba.BuildingBlocks;
using Tooba.Content.Contracts.Enums;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Domain.Aggregates;

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
            throw new ContractOperationException(ContentErrorCodes.CommentInvalidPayload);
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
            throw new ContractOperationException(ContentErrorCodes.CommentInvalidTransition);

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
            throw new ContractOperationException(ContentErrorCodes.CommentInvalidTransition);
    }

    private static void EnsureModerator(Guid moderatorUserId)
    {
        if (moderatorUserId == Guid.Empty)
            throw new ContractOperationException(ContentErrorCodes.CommentForbidden);
    }

    private static string NormalizeRequired(string value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ContractOperationException(ContentErrorCodes.CommentInvalidPayload);
        var trimmed = value.Trim();
        if (trimmed.Length > max)
            throw new ContractOperationException(ContentErrorCodes.CommentInvalidPayload);
        return trimmed;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
