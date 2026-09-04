using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P08-T003: نویسندهٔ مقاله — slug، deactivate و انتساب.</summary>
[Collection("PostgresSerial")]
public sealed class ContentAuthorDirectoryTests : IAsyncLifetime
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
                .WithDatabase("tooba_content_authors")
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

    /// <summary>slug یکتا، deactivate، انتساب جدید غیرفعال و publish با نویسندهٔ موجود.</summary>
    [SkippableFact]
    public async Task Author_rules_slug_deactivate_assignment_and_publish()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var authors = new ContentAuthorDirectory(db);
        var categories = new ContentCategoryDirectory(db);
        var languages = new PermissiveLanguageDirectory();
        var content = new ContentDirectory(db, languages, categories, authors, new ContentTagDirectory(db));

        var active = await authors.CreateAsync(
            new CreateContentAuthorCommand("تحریریه توبا", "tooba-editorial", null, null, null, null, null, null, null, null),
            CancellationToken.None);
        var second = await authors.CreateAsync(
            new CreateContentAuthorCommand("مریم احمدی", "maryam-ahmadi", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authors.CreateAsync(
                new CreateContentAuthorCommand("نام دیگر", "tooba-editorial", null, null, null, null, null, null, null, null),
                CancellationToken.None));

        var article = await content.CreateAsync(
            new CreateArticleCommand(
                "author-article",
                "مقاله",
                "چکیده",
                "بدنه",
                null,
                active.Id,
                [],
                false,
                DateTimeOffset.UtcNow,
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);
        Assert.Equal(active.Id, article.AuthorId);
        Assert.Equal("تحریریه توبا", article.AuthorDisplayName);

        await authors.DeactivateAsync(active.Id, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            content.CreateAsync(
                new CreateArticleCommand(
                    "inactive-author",
                    "مقاله",
                    "چکیده",
                    "بدنه",
                    null,
                    active.Id,
                    [],
                    false,
                    DateTimeOffset.UtcNow,
                    "fa-IR",
                    null,
                    null,
                    null,
                    null),
                CancellationToken.None));

        var updated = await content.UpdateAsync(
            article.ArticleId,
            new UpdateArticleCommand(
                "مقالهٔ به‌روز",
                "چکیده",
                "بدنه",
                null,
                active.Id,
                [],
                false,
                "fa-IR",
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);
        Assert.Equal(active.Id, updated.AuthorId);

        var published = await content.PublishAsync(article.ArticleId, CancellationToken.None);
        Assert.Equal(ContentPublicationStatus.Published, published.Status);

        var reassigned = await content.UpdateAsync(
            article.ArticleId,
            new UpdateArticleCommand(
                "مقالهٔ به‌روز",
                "چکیده",
                "بدنه",
                null,
                second.Id,
                [],
                false,
                "fa-IR",
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);
        Assert.Equal(second.Id, reassigned.AuthorId);

        var picker = await authors.GetPickerListAsync(null, activeOnly: true, CancellationToken.None);
        Assert.DoesNotContain(picker, x => x.Id == active.Id);
        Assert.Contains(picker, x => x.Id == second.Id);
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
