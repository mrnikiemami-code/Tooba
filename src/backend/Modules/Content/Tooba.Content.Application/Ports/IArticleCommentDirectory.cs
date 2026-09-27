using Tooba.Content.Application.Models;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;

namespace Tooba.Content.Application.Ports;

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
