using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>دایرکتوری نظرات مقاله — scoped به Article، بدون hard-delete تاریخچه.</summary>
public sealed class ArticleCommentDirectory : IArticleCommentDirectory
{
    private const int MaxTake = 50;
    private readonly ContentDbContext _db;

    /// <summary>دایرکتوری را می‌سازد.</summary>
    public ArticleCommentDirectory(ContentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<ArticleCommentPage> ListForArticleAsync(
        Guid articleId,
        ArticleCommentStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        await EnsureArticleExistsAsync(articleId, cancellationToken);
        skip = Math.Max(0, skip);
        take = Math.Clamp(take <= 0 ? 20 : take, 1, MaxTake);

        var query = _db.ArticleComments.AsNoTracking().Where(x => x.ArticleId == articleId);
        if (status is not null)
            query = query.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.DisplayName, $"%{term}%") ||
                EF.Functions.ILike(x.Body, $"%{term}%"));
        }

        var pendingCount = await _db.ArticleComments.AsNoTracking()
            .CountAsync(x => x.ArticleId == articleId && x.Status == ArticleCommentStatus.Pending, cancellationToken);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.CommentId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new ArticleCommentPage(rows.Select(Map).ToList(), total, skip, take, pendingCount);
    }

    /// <inheritdoc />
    public async Task<ArticleCommentAdminDto> CreateAsync(
        Guid articleId,
        CreateArticleCommentCommand command,
        CancellationToken cancellationToken)
    {
        await EnsureArticleExistsAsync(articleId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var entity = ArticleComment.Create(
            articleId,
            command.DisplayName,
            command.Body,
            now,
            command.AuthorPartyId);
        _db.ArticleComments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    /// <inheritdoc />
    public Task<ArticleCommentAdminDto> ApproveAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand command,
        CancellationToken cancellationToken) =>
        ModerateAsync(articleId, commentId, moderatorUserId, command, (c, now) => c.Approve(moderatorUserId, now, command.Note), cancellationToken);

    /// <inheritdoc />
    public Task<ArticleCommentAdminDto> RejectAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand command,
        CancellationToken cancellationToken) =>
        ModerateAsync(articleId, commentId, moderatorUserId, command, (c, now) => c.Reject(moderatorUserId, now, command.Note), cancellationToken);

    /// <inheritdoc />
    public Task<ArticleCommentAdminDto> HideAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand command,
        CancellationToken cancellationToken) =>
        ModerateAsync(articleId, commentId, moderatorUserId, command, (c, now) => c.Hide(moderatorUserId, now, command.Note), cancellationToken);

    /// <inheritdoc />
    public Task<ArticleCommentAdminDto> MarkPendingAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand command,
        CancellationToken cancellationToken) =>
        ModerateAsync(articleId, commentId, moderatorUserId, command, (c, now) => c.MarkPending(moderatorUserId, now, command.Note), cancellationToken);

    private async Task<ArticleCommentAdminDto> ModerateAsync(
        Guid articleId,
        Guid commentId,
        Guid moderatorUserId,
        ModerateArticleCommentCommand _,
        Action<ArticleComment, DateTimeOffset> apply,
        CancellationToken cancellationToken)
    {
        await EnsureArticleExistsAsync(articleId, cancellationToken);
        var entity = await _db.ArticleComments
            .FirstOrDefaultAsync(x => x.ArticleId == articleId && x.CommentId == commentId, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException(ArticleCommentCodes.NotFound);

        var now = DateTimeOffset.UtcNow;
        apply(entity, now);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private async Task EnsureArticleExistsAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var exists = await _db.Articles.AsNoTracking().AnyAsync(x => x.ArticleId == articleId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException(ArticleCommentCodes.ArticleNotFound);
    }

    private static ArticleCommentAdminDto Map(ArticleComment x) =>
        new(
            x.CommentId,
            x.ArticleId,
            x.AuthorPartyId,
            x.DisplayName,
            x.Body,
            x.Status,
            x.CreatedAt,
            x.ModeratedAt,
            x.ModeratedByUserId,
            x.ModerationNote);

}
