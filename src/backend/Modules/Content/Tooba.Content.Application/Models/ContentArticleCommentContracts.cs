using Tooba.Content.Domain;

namespace Tooba.Content.Application.Models;

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
