using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application.Articles.Commands;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Application.Authors.Commands;
using Tooba.Content.Application.Authors.Models;
using Tooba.Content.Application.Authors.Ports;
using Tooba.Content.Application.Categories.Commands;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Application.Tags.Commands;
using Tooba.Content.Application.Tags.Models;
using Tooba.Content.Application.Tags.Ports;
using Tooba.Content.Application.Comments.Commands;
using Tooba.Content.Application.Comments.Models;
using Tooba.Content.Application.Comments.Ports;
using Testcontainers.PostgreSql;


using Tooba.Localization.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Directories;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Application.Ports;
using Tooba.Persistence;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Contracts.Enums;
using Tooba.BuildingBlocks;
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

        var same = Assert.Throws<ContractOperationException>(() => comment.Approve(moderator, now.AddMinutes(2)));
        Assert.Contains(ContentErrorCodes.CommentInvalidTransition, same.Message, StringComparison.Ordinal);

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
            new CreateArticleCommentCommand(article.ArticleId, "اولی", "نظر قدیمی", null),
            CancellationToken.None);
        await Task.Delay(20);
        var newer = await comments.CreateAsync(
            new CreateArticleCommentCommand(article.ArticleId, "دومی", "نظر جدیدتر", null),
            CancellationToken.None);

        var page = await comments.ListForArticleAsync(article.ArticleId, null, null, 0, 20, CancellationToken.None);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.PendingCount);
        Assert.Equal(newer.CommentId, page.Items[0].CommentId);
        Assert.Equal(older.CommentId, page.Items[1].CommentId);

        var moderator = Guid.NewGuid();
        var approved = await comments.ApproveAsync(
            new ApproveArticleCommentCommand(article.ArticleId, newer.CommentId, moderator, null),
            CancellationToken.None);
        Assert.Equal(ArticleCommentStatus.Approved, approved.Status);

        var rejected = await comments.RejectAsync(
            new RejectArticleCommentCommand(article.ArticleId, older.CommentId, moderator, "نامناسب"),
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
        Assert.Equal(ContentErrorCodes.CommentArticleNotFound, missingArticle.Message);

        var missingComment = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            comments.HideAsync(
                new HideArticleCommentCommand(article.ArticleId, Guid.NewGuid(), moderator, null),
                CancellationToken.None));
        Assert.Equal(ContentErrorCodes.CommentNotFound, missingComment.Message);
    }

    private static ContentDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, ContentDbContext.Schema, typeof(ContentDbContext));
        return new ContentDbContext(options.Options);
    }

    private sealed class PermissiveLanguageDirectory : Tooba.Localization.Contracts.Ports.ILanguageActivationPort
    {
        public Task EnsureActiveAsync(string languageCode, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> IsActiveAsync(string languageCode, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}


