using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;

namespace Tooba.Content.Application.Comments.Models;

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
