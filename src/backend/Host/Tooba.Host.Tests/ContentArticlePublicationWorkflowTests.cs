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

/// <summary>TB-P08-T014: readiness gate، lifecycle history، schedule، preview.</summary>
[Collection("PostgresSerial")]
public sealed class ContentArticlePublicationWorkflowTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_content_publication")
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

    [SkippableFact]
    public async Task Readiness_gates_publish_and_history_records_lifecycle()
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
            new CreateContentAuthorCommand("تحریریه", "editorial-t014", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        var incomplete = await directory.CreateAsync(
            new CreateArticleCommand(
                "t014-incomplete",
                "عنوان",
                "چکیده",
                "",
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

        var notReady = await directory.GetPublishReadinessAsync(incomplete.ArticleId, CancellationToken.None);
        Assert.False(notReady.CanPublish);
        Assert.Contains(notReady.RequiredMissing, c => c.Key == ArticlePublicationCodes.Body);
        Assert.Contains(notReady.RequiredMissing, c => c.Key == ArticlePublicationCodes.Author);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.PublishAsync(incomplete.ArticleId, CancellationToken.None));

        var ready = await directory.CreateAsync(
            new CreateArticleCommand(
                "t014-ready",
                "عنوان آماده",
                "چکیده آماده",
                "<p>بدنه آماده</p>",
                null,
                author.Id,
                [],
                false,
                DateTimeOffset.UtcNow.AddDays(-1),
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var readiness = await directory.GetPublishReadinessAsync(ready.ArticleId, CancellationToken.None);
        Assert.True(readiness.CanPublish);

        var published = await directory.PublishAsync(ready.ArticleId, CancellationToken.None);
        Assert.Equal(ContentPublicationStatus.Published, published.Status);
        Assert.NotNull(await directory.GetPublishedBySlugAsync("t014-ready", "fa-IR", CancellationToken.None));

        var unpublished = await directory.UnpublishAsync(ready.ArticleId, CancellationToken.None);
        Assert.Equal(ContentPublicationStatus.Draft, unpublished.Status);
        Assert.Null(await directory.GetPublishedBySlugAsync("t014-ready", "fa-IR", CancellationToken.None));

        var republished = await directory.PublishAsync(ready.ArticleId, CancellationToken.None);
        Assert.Equal(ContentPublicationStatus.Published, republished.Status);

        var history = await directory.ListHistoryAsync(ready.ArticleId, 0, 20, CancellationToken.None);
        Assert.Contains(history.Items, e => e.EventType == ArticleHistoryRules.EventPublished);
        Assert.Contains(history.Items, e => e.EventType == ArticleHistoryRules.EventUnpublished);
        Assert.Contains(history.Items, e => e.EventType == ArticleHistoryRules.EventRepublished);
        Assert.Contains(history.Items, e => e.EventType == ArticleHistoryRules.EventDraftCreated);

        var preview = await directory.GetPreviewAsync(incomplete.ArticleId, CancellationToken.None);
        Assert.NotNull(preview);
        Assert.True(preview!.IsPreview);
        Assert.True(preview.RobotsNoIndex);
        Assert.Equal(ContentPublicationStatus.Draft, preview.Status);

        var scheduledDraft = await directory.CreateAsync(
            new CreateArticleCommand(
                "t014-scheduled",
                "زمان‌بندی",
                "چکیده",
                "<p>بدنه</p>",
                null,
                author.Id,
                [],
                false,
                DateTimeOffset.UtcNow.AddDays(3),
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);
        await directory.PublishAsync(scheduledDraft.ArticleId, CancellationToken.None);
        Assert.Null(await directory.GetPublishedBySlugAsync("t014-scheduled", "fa-IR", CancellationToken.None));
        var scheduledHistory = await directory.ListHistoryAsync(scheduledDraft.ArticleId, 0, 10, CancellationToken.None);
        Assert.Contains(scheduledHistory.Items, e => e.EventType == ArticleHistoryRules.EventScheduled);
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
