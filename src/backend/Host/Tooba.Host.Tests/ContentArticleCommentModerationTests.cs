using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Localization.Application;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P08-T015: ArticleComment transitions، paging، auth codes.</summary>
[Collection("PostgresSerial")]
public sealed class ContentArticleCommentModerationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_content_comments")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    [Fact]
    public void Domain_transitions_are_stable_and_reject_same_status()
    {
        var now = DateTimeOffset.UtcNow;
        var comment = ArticleComment.Create(Guid.NewGuid(), "خواننده", "متن نظر معتبر", now);
        Assert.Equal(ArticleCommentStatus.Pending, comment.Status);

        var moderator = Guid.NewGuid();
        comment.Approve(moderator, now.AddMinutes(1));
        Assert.Equal(ArticleCommentStatus.Approved, comment.Status);
        Assert.Equal(moderator, comment.ModeratedByUserId);

        var same = Assert.Throws<InvalidOperationException>(() => comment.Approve(moderator, now.AddMinutes(2)));
        Assert.Contains(ArticleCommentCodes.InvalidTransition, same.Message, StringComparison.Ordinal);

        comment.Hide(moderator, now.AddMinutes(3), "پنهان اداری");
        Assert.Equal(ArticleCommentStatus.Hidden, comment.Status);
        Assert.Equal("پنهان اداری", comment.ModerationNote);

        comment.MarkPending(moderator, now.AddMinutes(4));
        Assert.Equal(ArticleCommentStatus.Pending, comment.Status);

        comment.Reject(moderator, now.AddMinutes(5));
        Assert.Equal(ArticleCommentStatus.Rejected, comment.Status);
    }

    [SkippableFact]
    public async Task Directory_moderates_and_pages_newest_first()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var articles = new ContentDirectory(
            db,
            new PermissiveLanguageDirectory(),
            new ContentCategoryDirectory(db),
            new ContentAuthorDirectory(db),
            new ContentTagDirectory(db));
        var comments = new ArticleCommentDirectory(db);

        var article = await articles.CreateAsync(
            new CreateArticleCommand(
                "t015-comments",
                "عنوان نظرات",
                "چکیده",
                "<p>بدنه</p>",
                null,
                null,
                [],
                false,
                DateTimeOffset.UtcNow.AddDays(-1),
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var older = await comments.CreateAsync(
            article.ArticleId,
            new CreateArticleCommentCommand("اولی", "نظر قدیمی"),
            CancellationToken.None);
        await Task.Delay(20);
        var newer = await comments.CreateAsync(
            article.ArticleId,
            new CreateArticleCommentCommand("دومی", "نظر جدیدتر"),
            CancellationToken.None);

        var page = await comments.ListForArticleAsync(article.ArticleId, null, null, 0, 20, CancellationToken.None);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.PendingCount);
        Assert.Equal(newer.CommentId, page.Items[0].CommentId);
        Assert.Equal(older.CommentId, page.Items[1].CommentId);

        var moderator = Guid.NewGuid();
        var approved = await comments.ApproveAsync(
            article.ArticleId,
            newer.CommentId,
            moderator,
            new ModerateArticleCommentCommand(),
            CancellationToken.None);
        Assert.Equal(ArticleCommentStatus.Approved, approved.Status);

        var rejected = await comments.RejectAsync(
            article.ArticleId,
            older.CommentId,
            moderator,
            new ModerateArticleCommentCommand("نامناسب"),
            CancellationToken.None);
        Assert.Equal(ArticleCommentStatus.Rejected, rejected.Status);

        var pendingOnly = await comments.ListForArticleAsync(
            article.ArticleId, ArticleCommentStatus.Pending, null, 0, 20, CancellationToken.None);
        Assert.Equal(0, pendingOnly.TotalCount);

        var search = await comments.ListForArticleAsync(
            article.ArticleId, null, "جدیدتر", 0, 20, CancellationToken.None);
        Assert.Equal(1, search.TotalCount);
        Assert.Equal(newer.CommentId, search.Items[0].CommentId);

        var missingArticle = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            comments.ListForArticleAsync(Guid.NewGuid(), null, null, 0, 10, CancellationToken.None));
        Assert.Equal(ArticleCommentCodes.ArticleNotFound, missingArticle.Message);

        var missingComment = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            comments.HideAsync(article.ArticleId, Guid.NewGuid(), moderator, new ModerateArticleCommentCommand(), CancellationToken.None));
        Assert.Equal(ArticleCommentCodes.NotFound, missingComment.Message);
    }

    private static ContentDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, ContentDbContext.Schema, typeof(ContentDbContext));
        return new ContentDbContext(options.Options);
    }

    private sealed class PermissiveLanguageDirectory : ILanguageDirectory
    {
        public Task EnsureActiveLanguageCodeAsync(string code, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<LanguageSnapshot>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<LanguageSnapshot>>([]);
        public Task<IReadOnlyList<LanguageAdminSnapshot>> ListAdminAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<LanguageAdminSnapshot>>([]);
        public Task<LanguageSnapshot?> GetByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult<LanguageSnapshot?>(null);
        public Task<LanguageAdminSnapshot?> GetAdminByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult<LanguageAdminSnapshot?>(null);
        public Task<LanguageSnapshot> CreateAsync(CreateLanguageCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<LanguageSnapshot> UpdateAsync(string code, UpdateLanguageCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<LanguageSnapshot> PatchAsync(string code, PatchLanguageCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task BootstrapAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
