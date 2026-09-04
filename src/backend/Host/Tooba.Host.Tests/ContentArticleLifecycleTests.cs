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

/// <summary>TB-P08-T007: حذف/بایگانی امن مقاله.</summary>
[Collection("PostgresSerial")]
public sealed class ContentArticleLifecycleTests : IAsyncLifetime
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
                .WithDatabase("tooba_content_lifecycle")
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

    /// <summary>پیش‌نویس حذف می‌شود؛ منتشرشده بایگانی می‌شود و عمومی نیست.</summary>
    [SkippableFact]
    public async Task Draft_delete_published_archive_and_public_visibility()
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

        var draft = await directory.CreateAsync(
            new CreateArticleCommand(
                "draft-delete-me",
                "پیش‌نویس",
                "چکیده",
                "بدنه",
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

        var published = await directory.CreateAsync(
            new CreateArticleCommand(
                "published-archive-me",
                "منتشر",
                "چکیده",
                "بدنه",
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
        var publishedSnapshot = await directory.PublishAsync(published.ArticleId, CancellationToken.None);

        Assert.True(ContentArticleLifecycleRules.CanHardDelete(draft.Status));
        Assert.False(ContentArticleLifecycleRules.CanHardDelete(publishedSnapshot.Status));
        Assert.True(ContentArticleLifecycleRules.CanArchive(publishedSnapshot.Status));

        await directory.DeleteDraftAsync(draft.ArticleId, CancellationToken.None);
        Assert.Null(await directory.GetByIdAsync(draft.ArticleId, CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.DeleteDraftAsync(published.ArticleId, CancellationToken.None));

        var archived = await directory.ArchiveAsync(published.ArticleId, CancellationToken.None);
        Assert.Equal(ContentPublicationStatus.Archived, archived.Status);
        Assert.Null(await directory.GetPublishedBySlugAsync("published-archive-me", "fa-IR", CancellationToken.None));
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
