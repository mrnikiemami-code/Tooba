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

/// <summary>پوشش foundation Content: draft/publish/unpublish، slug یکتا و ریل خانه.</summary>
[Collection("PostgresSerial")]
public sealed class ContentFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_content")
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

    /// <summary>مرز schema و ثبت دایرکتوری Content.</summary>
    [Fact]
    public void Content_module_boundary_static_checks()
    {
        Assert.Equal("content", ContentDbContext.Schema);
        Assert.NotNull(typeof(IContentDirectory).GetMethod(nameof(IContentDirectory.ListPublishedAsync)));
        Assert.NotNull(typeof(IContentDirectory).GetMethod(nameof(IContentDirectory.GetPublishedBySlugAsync)));
        Assert.NotNull(typeof(IContentDirectory).GetMethod(nameof(IContentDirectory.UnpublishAsync)));
        Assert.Equal(nameof(ContentPublicationStatus.Draft), ContentPublicationStatus.Draft.ToString());
    }

    /// <summary>draft عمومی نیست؛ publish با slug دیده می‌شود؛ unpublish پنهان می‌کند؛ slug یکتا است؛ ریل خانه فقط Published.</summary>
    [SkippableFact]
    public async Task Draft_publish_unpublish_slug_and_home_listing_behave()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var categories = new ContentCategoryDirectory(db);
        var authors = new ContentAuthorDirectory(db);
        var directory = new ContentDirectory(db, new PermissiveLanguageDirectory(), categories, authors, new ContentTagDirectory(db));
        var now = DateTimeOffset.Parse("2026-08-27T00:00:00Z");

        var author = await authors.CreateAsync(
            new CreateContentAuthorCommand("تحریریه تست", "test-editorial", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        var draft = await directory.CreateAsync(
            new CreateArticleCommand(
                "draft-guide",
                "راهنمای پیش‌نویس",
                "چکیدهٔ پیش‌نویس",
                "بدنهٔ پیش‌نویس برای تست.",
                null,
                author.Id,
                ["تست"],
                false,
                now,
                ContentArticle.DefaultLocale,
                "SEO پیش‌نویس",
                "توضیح SEO پیش‌نویس",
                "راهنما",
                null),
            CancellationToken.None);

        Assert.Equal(ContentPublicationStatus.Draft, draft.Status);
        Assert.Null(await directory.GetPublishedBySlugAsync("draft-guide", ContentArticle.DefaultLocale, CancellationToken.None));
        Assert.Empty((await directory.ListPublishedAsync(1, 20, null, null, null, null, CancellationToken.None)).Items);
        Assert.Empty(await directory.ListPublishedForHomeAsync(6, null, CancellationToken.None));

        var published = await directory.PublishAsync(draft.ArticleId, CancellationToken.None);
        Assert.Equal(ContentPublicationStatus.Published, published.Status);

        var bySlug = await directory.GetPublishedBySlugAsync("draft-guide", ContentArticle.DefaultLocale, CancellationToken.None);
        Assert.NotNull(bySlug);
        Assert.Equal("بدنهٔ پیش‌نویس برای تست.", bySlug!.Body);
        Assert.Equal("SEO پیش‌نویس", bySlug.SeoTitle);
        Assert.Equal("راهنما", bySlug.Category);

        var listed = await directory.ListPublishedAsync(1, 20, "راهنما", ContentArticle.DefaultLocale, null, null, CancellationToken.None);
        Assert.Single(listed.Items);
        Assert.Null(listed.Items[0].Body);

        var home = await directory.ListPublishedForHomeAsync(6, ContentArticle.DefaultLocale, CancellationToken.None);
        Assert.Single(home);
        Assert.Equal("draft-guide", home[0].Slug);

        await directory.UnpublishAsync(draft.ArticleId, CancellationToken.None);
        Assert.Null(await directory.GetPublishedBySlugAsync("draft-guide", ContentArticle.DefaultLocale, CancellationToken.None));
        Assert.Empty(await directory.ListPublishedForHomeAsync(6, ContentArticle.DefaultLocale, CancellationToken.None));

        await directory.CreateAsync(
            new CreateArticleCommand(
                "unique-slug",
                "عنوان یک",
                "چکیده یک",
                "بدنه یک",
                null,
                author.Id,
                [],
                false,
                now,
                ContentArticle.DefaultLocale,
                null,
                null,
                null,
                null),
            CancellationToken.None);
        var enArticle = await directory.CreateAsync(
            new CreateArticleCommand(
                "unique-slug",
                "English title",
                "English excerpt",
                "English body",
                null,
                author.Id,
                [],
                false,
                now,
                "en-US",
                null,
                null,
                null,
                null),
            CancellationToken.None);
        Assert.Equal("unique-slug", enArticle.Slug);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.CreateAsync(
                new CreateArticleCommand(
                    "unique-slug",
                    "عنوان دو",
                    "چکیده دو",
                    "بدنه دو",
                    null,
                    author.Id,
                    [],
                    false,
                    now,
                    ContentArticle.DefaultLocale,
                    null,
                    null,
                    null,
                    null),
                CancellationToken.None));
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
