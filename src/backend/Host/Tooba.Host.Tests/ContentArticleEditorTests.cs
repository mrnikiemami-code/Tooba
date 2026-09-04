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

/// <summary>TB-P08-T011: draft-first create، CanChangeLocale، قفل پس از نویسنده/انتشار.</summary>
[Collection("PostgresSerial")]
public sealed class ContentArticleEditorTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_content_editor")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>ایجاد بدون نویسنده اجازهٔ تغییر locale می‌دهد؛ انتساب نویسنده و انتشار قفل می‌کند.</summary>
    [SkippableFact]
    public async Task Locale_change_allowed_for_pristine_draft_then_locks_with_author_or_publish()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var categories = new ContentCategoryDirectory(db);
        var authors = new ContentAuthorDirectory(db);
        var languages = new PermissiveLanguageDirectory();
        var content = new ContentDirectory(db, languages, categories, authors, new ContentTagDirectory(db));

        var draft = await content.CreateAsync(
            new CreateArticleCommand(
                "editor-draft",
                "پیش‌نویس",
                "چکیده",
                "",
                null,
                null,
                [],
                false,
                DateTimeOffset.Parse("2026-09-01T08:00:00Z"),
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);
        Assert.Null(draft.AuthorId);
        Assert.Equal(string.Empty, draft.AuthorDisplayName);

        var switched = await content.UpdateAsync(
            draft.ArticleId,
            new UpdateArticleCommand(
                "پیش‌نویس",
                "چکیده",
                "",
                null,
                null,
                [],
                false,
                "en-US",
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);
        Assert.Equal("en-US", switched.Locale);

        var author = await authors.CreateAsync(
            new CreateContentAuthorCommand("تحریریه", "editorial", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        // انتساب نویسنده با همان locale — سپس تغییر locale باید قفل شود.
        var withAuthor = await content.UpdateAsync(
            draft.ArticleId,
            new UpdateArticleCommand(
                "پیش‌نویس",
                "چکیده",
                "",
                null,
                author.Id,
                [],
                false,
                "en-US",
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);
        Assert.Equal(author.Id, withAuthor.AuthorId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            content.UpdateAsync(
                draft.ArticleId,
                new UpdateArticleCommand(
                    "پیش‌نویس",
                    "چکیده",
                    "",
                    null,
                    author.Id,
                    [],
                    false,
                    "fa-IR",
                    null,
                    null,
                    null,
                    null,
                    null),
                CancellationToken.None));

        var scheduled = DateTimeOffset.Parse("2026-09-15T12:00:00Z");
        var updated = await content.UpdateAsync(
            draft.ArticleId,
            new UpdateArticleCommand(
                "پیش‌نویس زمان‌بندی‌شده",
                "چکیده",
                "<p>بدنه آماده انتشار</p>",
                null,
                author.Id,
                [],
                false,
                "en-US",
                null,
                null,
                null,
                null,
                scheduled),
            CancellationToken.None);
        Assert.Equal(scheduled, updated.PublishDate);
        Assert.False(string.IsNullOrWhiteSpace(updated.Body));

        var published = await content.PublishAsync(draft.ArticleId, CancellationToken.None);
        Assert.Equal(ContentPublicationStatus.Published, published.Status);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            content.UpdateAsync(
                draft.ArticleId,
                new UpdateArticleCommand(
                    published.Title,
                    published.Excerpt,
                    published.Body,
                    null,
                    author.Id,
                    [],
                    false,
                    "fa-IR",
                    null,
                    null,
                    null,
                    null,
                    null),
                CancellationToken.None));
    }

    /// <summary>دسته با زبان ناسازگار باید language_mismatch بدهد؛ دسته هم‌زبان ذخیره می‌شود.</summary>
    [SkippableFact]
    public async Task Category_assign_requires_matching_language_and_succeeds_when_aligned()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var categories = new ContentCategoryDirectory(db);
        var authors = new ContentAuthorDirectory(db);
        var languages = new PermissiveLanguageDirectory();
        var content = new ContentDirectory(db, languages, categories, authors, new ContentTagDirectory(db));

        var faCategory = await categories.CreateAsync(
            new CreateContentCategoryCommand("fa-IR", null, "اخبار", "akhbar", null, null, 10),
            CancellationToken.None);
        var enCategory = await categories.CreateAsync(
            new CreateContentCategoryCommand("en-US", null, "News", "news", null, null, 10),
            CancellationToken.None);

        var draft = await content.CreateAsync(
            new CreateArticleCommand(
                "cat-align",
                "مقاله دسته",
                "چکیده",
                "",
                null,
                null,
                [],
                false,
                DateTimeOffset.Parse("2026-09-01T08:00:00Z"),
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var mismatch = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            content.UpdateAsync(
                draft.ArticleId,
                new UpdateArticleCommand(
                    "مقاله دسته",
                    "چکیده",
                    "",
                    null,
                    null,
                    [],
                    false,
                    "fa-IR",
                    null,
                    null,
                    null,
                    enCategory.Id,
                    null),
                CancellationToken.None));
        Assert.Equal(ContentCategoryErrorCodes.LanguageMismatch, mismatch.Message);

        var aligned = await content.UpdateAsync(
            draft.ArticleId,
            new UpdateArticleCommand(
                "مقاله دسته",
                "چکیده",
                "",
                null,
                null,
                [],
                false,
                "fa-IR",
                null,
                null,
                "اخبار",
                faCategory.Id,
                null),
            CancellationToken.None);
        Assert.Equal(faCategory.Id, aligned.CategoryId);
    }

    private static ContentDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, ContentDbContext.Schema, typeof(ContentDbContext));
        return new ContentDbContext(options.Options);
    }

    private sealed class PermissiveLanguageDirectory : ILanguageDirectory
    {
        public Task EnsureActiveLanguageCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<LanguageSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LanguageSnapshot>>([]);

        public Task<IReadOnlyList<LanguageAdminSnapshot>> ListAdminAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LanguageAdminSnapshot>>([]);

        public Task<LanguageSnapshot?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<LanguageSnapshot?>(null);

        public Task<LanguageAdminSnapshot?> GetAdminByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<LanguageAdminSnapshot?>(null);

        public Task<LanguageSnapshot> CreateAsync(CreateLanguageCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<LanguageSnapshot> UpdateAsync(string code, UpdateLanguageCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<LanguageSnapshot> PatchAsync(string code, PatchLanguageCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task BootstrapAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
