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

/// <summary>TB-P08-T006: مسیر عمومی locale+slug، زمان‌بندی، canonical و بدون fallback.</summary>
[Collection("PostgresSerial")]
public sealed class ContentArticlePublicRoutingTests : IAsyncLifetime
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
                .WithDatabase("tooba_content_public")
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
            await _container.DisposeAsync();
    }

    /// <summary>locale+slug lookup، scheduling، slug per locale، no cross-locale fallback.</summary>
    [SkippableFact]
    public async Task Public_article_routing_locale_slug_scheduling_and_canonical()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var directory = new ContentDirectory(
            db,
            new PermissiveLanguageDirectory(),
            new ContentCategoryDirectory(db),
            new ContentAuthorDirectory(db),
            new ContentTagDirectory(db));

        var author = await new ContentAuthorDirectory(db).CreateAsync(
            new CreateContentAuthorCommand("تحریریه", "editorial", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        var past = DateTimeOffset.UtcNow.AddDays(-1);
        var future = DateTimeOffset.UtcNow.AddDays(2);

        var fa = await directory.CreateAsync(
            new CreateArticleCommand(
                "shared-topic",
                "موضوع فارسی",
                "چکیده",
                "<p>fa</p>",
                null,
                author.Id,
                [],
                false,
                past,
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);
        var en = await directory.CreateAsync(
            new CreateArticleCommand(
                "shared-topic",
                "English topic",
                "Excerpt",
                "<p>en</p>",
                null,
                author.Id,
                [],
                false,
                past,
                "en-US",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        await directory.PublishAsync(fa.ArticleId, CancellationToken.None);
        await directory.PublishAsync(en.ArticleId, CancellationToken.None);

        var faPublic = await directory.GetPublishedBySlugAsync("shared-topic", "fa-IR", CancellationToken.None);
        var enPublic = await directory.GetPublishedBySlugAsync("shared-topic", "en-US", CancellationToken.None);
        Assert.NotNull(faPublic);
        Assert.NotNull(enPublic);
        Assert.Equal("/fa/blogs/shared-topic", faPublic!.CanonicalPath);
        Assert.Equal("/en/blogs/shared-topic", enPublic!.CanonicalPath);
        Assert.Equal("fa-IR", faPublic.Locale);
        Assert.Equal("en-US", enPublic!.Locale);
        Assert.Null(await directory.GetPublishedBySlugAsync("shared-topic", null, CancellationToken.None));

        var scheduled = await directory.CreateAsync(
            new CreateArticleCommand(
                "future-post",
                "آینده",
                "چکیده",
                "بدنه",
                null,
                author.Id,
                [],
                false,
                future,
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);
        await directory.PublishAsync(scheduled.ArticleId, CancellationToken.None);
        Assert.Null(await directory.GetPublishedBySlugAsync("future-post", "fa-IR", CancellationToken.None));

        await directory.UnpublishAsync(fa.ArticleId, CancellationToken.None);
        Assert.Null(await directory.GetPublishedBySlugAsync("shared-topic", "fa-IR", CancellationToken.None));
        Assert.NotNull(await directory.GetPublishedBySlugAsync("shared-topic", "en-US", CancellationToken.None));

        var faListed = await directory.ListPublishedAsync(1, 20, null, "fa-IR", null, null, CancellationToken.None);
        Assert.DoesNotContain(faListed.Items, item => item.Slug == "shared-topic");
        Assert.DoesNotContain(faListed.Items, item => item.Slug == "future-post");
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
