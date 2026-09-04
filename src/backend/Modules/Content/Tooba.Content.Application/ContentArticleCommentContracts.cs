using Tooba.Content.Domain;

namespace Tooba.Content.Application;

/// <summary>ردیف Admin برای نظر مقاله.</summary>
public sealed record ArticleCommentAdminDto(
    Guid CommentId,
    Guid ArticleId,
    Guid? AuthorPartyId,
    string DisplayName,
    string Body,
    ArticleCommentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModeratedAt,
    Guid? ModeratedByUserId,
    string? ModerationNote);

/// <summary>صفحهٔ نظرات مقاله.</summary>
public sealed record ArticleCommentPage(
    IReadOnlyList<ArticleCommentAdminDto> Items,
    int TotalCount,
    int Skip,
    int Take,
    int PendingCount);

/// <summary>فرمان ایجاد نظر Admin/Dev (بدون فرم عمومی).</summary>
public sealed record CreateArticleCommentCommand(
    string DisplayName,
    string Body,
    Guid? AuthorPartyId = null);

/// <summary>فرمان تعدیل با یادداشت اختیاری.</summary>
public sealed record ModerateArticleCommentCommand(string? Note = null);

/// <summary>قابلیت فهرست و تعدیل نظرات مقاله.</summary>
public interface IArticleCommentDirectory
{
    /// <summary>فهرست صفحه‌بندی‌شدهٔ نظرات یک مقاله (جدیدترین اول).</summary>
    Task<ArticleCommentPage> ListForArticleAsync(
        Guid articleId,
        ArticleCommentStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>ایجاد نظر Pending برای smoke/admin (بدون حذف تاریخچه).</summary>
    Task<ArticleCommentAdminDto> CreateAsync(
        Guid articleId,
        CreateArticleCommentCommand command,
        CancellationToken cancellationToken);

    /// <summary>تأیید.</summary>
    Task<ArticleCommentAdminDto> ApproveAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand command,
        CancellationToken cancellationToken);

    /// <summary>رد.</summary>
    Task<ArticleCommentAdminDto> RejectAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand command,
        CancellationToken cancellationToken);

    /// <summary>پنهان.</summary>
    Task<ArticleCommentAdminDto> HideAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand command,
        CancellationToken cancellationToken);

    /// <summary>بازگشت به Pending.</summary>
    Task<ArticleCommentAdminDto> MarkPendingAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand command,
        CancellationToken cancellationToken);
}
