using Tooba.Content.Application.Comments.Commands;
using Tooba.Content.Application.Comments.Models;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;

namespace Tooba.Content.Application.Comments.Ports;

/// <summary>قابلیت فهرست و تعدیل نظرات مقاله.</summary>
public interface IArticleCommentDirectory
{
    Task<ArticleCommentPage> ListForArticleAsync(
        Guid articleId, ArticleCommentStatus? status, string? search,
        int skip, int take, CancellationToken cancellationToken);

    Task<ArticleCommentAdminDto> CreateAsync(
        CreateArticleCommentCommand command, CancellationToken cancellationToken);

    Task<ArticleCommentAdminDto> ApproveAsync(
        ApproveArticleCommentCommand command, CancellationToken cancellationToken);

    Task<ArticleCommentAdminDto> RejectAsync(
        RejectArticleCommentCommand command, CancellationToken cancellationToken);

    Task<ArticleCommentAdminDto> HideAsync(
        HideArticleCommentCommand command, CancellationToken cancellationToken);

    Task<ArticleCommentAdminDto> MarkPendingAsync(
        MarkArticleCommentPendingCommand command, CancellationToken cancellationToken);
}
