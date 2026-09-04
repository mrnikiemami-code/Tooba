using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P08-T002: دسته‌بندی مقاله — زبان، slug، چرخه و archive.</summary>
[Collection("PostgresSerial")]
public sealed class ContentCategoryDirectoryTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_content_categories")
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

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Category_rules_slug_parent_language_archive_and_article_match()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var categories = new ContentCategoryDirectory(db);
        var authors = new ContentAuthorDirectory(db);
        var languages = new PermissiveLanguageDirectory();
        var content = new ContentDirectory(db, languages, categories, authors, new ContentTagDirectory(db));

        var faRoot = await categories.CreateAsync(
            new CreateContentCategoryCommand("fa-IR", null, "راهنما", "guide", null, null, 0),
            CancellationToken.None);
        var faChild = await categories.CreateAsync(
            new CreateContentCategoryCommand("fa-IR", faRoot.Id, "خرید", "buying", null, null, 1),
            CancellationToken.None);
        var enRoot = await categories.CreateAsync(
            new CreateContentCategoryCommand("en-US", null, "Guides", "guides", null, null, 0),
            CancellationToken.None);
        var author = await authors.CreateAsync(
            new CreateContentAuthorCommand("نویسنده", "article-author", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.CreateAsync(
                new CreateContentCategoryCommand("fa-IR", null, "راهنمای دیگر", "guide", null, null, 2),
                CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.MoveAsync(
                faChild.Id,
                new MoveContentCategoryCommand(enRoot.Id),
                CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.MoveAsync(
                faRoot.Id,
                new MoveContentCategoryCommand(faChild.Id),
                CancellationToken.None));

        var article = await content.CreateAsync(
            new CreateArticleCommand(
                "cat-article",
                "مقاله",
                "چکیده",
                "بدنه",
                null,
                author.Id,
                [],
                false,
                DateTimeOffset.UtcNow,
                "fa-IR",
                null,
                null,
                faRoot.Name,
                faRoot.Id),
            CancellationToken.None);
        Assert.Equal(faRoot.Id, article.CategoryId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            content.CreateAsync(
                new CreateArticleCommand(
                    "wrong-lang",
                    "Article",
                    "Excerpt",
                    "Body",
                    null,
                    author.Id,
                    [],
                    false,
                    DateTimeOffset.UtcNow,
                    "en-US",
                    null,
                    null,
                    faRoot.Name,
                    faRoot.Id),
                CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.ArchiveAsync(faRoot.Id, CancellationToken.None));

        await categories.ArchiveAsync(faChild.Id, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.ArchiveAsync(faRoot.Id, CancellationToken.None));
    }

    private static ContentDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, ContentDbContext.Schema, typeof(ContentDbContext));
        return new ContentDbContext(options.Options);
    }

    private sealed class PermissiveLanguageDirectory : Tooba.Localization.Application.ILanguageDirectory
    {
        public Task EnsureActiveLanguageCodeAsync(string code, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Tooba.Localization.Application.LanguageSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tooba.Localization.Application.LanguageSnapshot>>([]);
        public Task<IReadOnlyList<Tooba.Localization.Application.LanguageAdminSnapshot>> ListAdminAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tooba.Localization.Application.LanguageAdminSnapshot>>([]);
        public Task<Tooba.Localization.Application.LanguageSnapshot?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<Tooba.Localization.Application.LanguageSnapshot?>(null);
        public Task<Tooba.Localization.Application.LanguageAdminSnapshot?> GetAdminByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<Tooba.Localization.Application.LanguageAdminSnapshot?>(null);
        public Task<Tooba.Localization.Application.LanguageSnapshot> CreateAsync(Tooba.Localization.Application.CreateLanguageCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Tooba.Localization.Application.LanguageSnapshot> UpdateAsync(string code, Tooba.Localization.Application.UpdateLanguageCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Tooba.Localization.Application.LanguageSnapshot> PatchAsync(string code, Tooba.Localization.Application.PatchLanguageCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task BootstrapAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
