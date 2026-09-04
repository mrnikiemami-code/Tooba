using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Localization.Application;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P08-T009: idempotency دانهٔ Content و پنهان بودن Scheduled از lookup عمومی.</summary>
[Collection("PostgresSerial")]
public sealed class ContentDevelopmentSeedIdempotencyTests : IAsyncLifetime
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
                .WithDatabase("tooba_content_seed")
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

    /// <summary>Apply دوبار شمارندهٔ مقالات را دو برابر نمی‌کند؛ Scheduled با GetPublishedBySlugAsync دیده نمی‌شود.</summary>
    [SkippableFact]
    public async Task Apply_twice_keeps_locale_counts_and_hides_scheduled()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        await using var provider = services.BuildServiceProvider();

        await ContentDevelopmentSeed.ApplyAsync(provider);
        var faAfterFirst = await db.Articles.CountAsync(a => a.Locale == ContentArticle.DefaultLocale);
        var enAfterFirst = await db.Articles.CountAsync(a => a.Locale == "en-US");
        Assert.True(faAfterFirst >= 5, $"expected fa-IR seed rows, got {faAfterFirst}");
        Assert.True(enAfterFirst >= 4, $"expected en-US seed rows, got {enAfterFirst}");

        await ContentDevelopmentSeed.ApplyAsync(provider);
        Assert.Equal(faAfterFirst, await db.Articles.CountAsync(a => a.Locale == ContentArticle.DefaultLocale));
        Assert.Equal(enAfterFirst, await db.Articles.CountAsync(a => a.Locale == "en-US"));

        var sharedSlugPair = await db.Articles
            .Where(a => a.Slug == "guide-online-shopping")
            .Select(a => a.Locale)
            .ToListAsync();
        Assert.Contains(ContentArticle.DefaultLocale, sharedSlugPair);
        Assert.Contains("en-US", sharedSlugPair);

        var scheduled = await db.Articles.SingleAsync(a => a.Slug == "scheduled-fa-festival-guide");
        Assert.Equal(ContentPublicationStatus.Published, scheduled.Status);
        Assert.True(scheduled.PublishDate > DateTimeOffset.UtcNow);

        var categories = new ContentCategoryDirectory(db);
        var authors = new ContentAuthorDirectory(db);
        var directory = new ContentDirectory(db, new PermissiveLanguageDirectory(), categories, authors, new ContentTagDirectory(db));
        Assert.Null(await directory.GetPublishedBySlugAsync(
            "scheduled-fa-festival-guide",
            ContentArticle.DefaultLocale,
            CancellationToken.None));
        Assert.NotNull(await directory.GetPublishedBySlugAsync(
            "guide-online-shopping",
            ContentArticle.DefaultLocale,
            CancellationToken.None));
        Assert.NotNull(await directory.GetPublishedBySlugAsync(
            "guide-online-shopping",
            "en-US",
            CancellationToken.None));
        Assert.Null(await directory.GetPublishedBySlugAsync(
            "draft-fa-shopping-notes",
            ContentArticle.DefaultLocale,
            CancellationToken.None));
        Assert.Null(await directory.GetPublishedBySlugAsync(
            "draft-en-buying-checklist",
            "en-US",
            CancellationToken.None));
    }

    /// <summary>TB-P08-T016-R1: برچسب‌های mojibake/? در Apply به برچسب تمیز + Archived/Inactive تبدیل می‌شوند.</summary>
    [SkippableFact]
    public async Task Apply_sanitizes_corrupted_category_author_and_article_labels()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();

        var now = DateTimeOffset.UtcNow;
        var corruptCategory = ContentCategory.Create(
            ContentArticle.DefaultLocale,
            null,
            "??????",
            "corrupt-cat-r1",
            null,
            null,
            99,
            null,
            null,
            null,
            now);
        db.Categories.Add(corruptCategory);

        var corruptAuthor = ContentAuthor.Create(
            "??????? b39688",
            "corrupt-author-r1",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            now);
        db.Authors.Add(corruptAuthor);

        var corruptArticle = ContentArticle.Create(
            "corrupt-article-r1",
            "?????? title",
            "?????? excerpt",
            "body text for repair",
            null,
            null,
            "Author Ok",
            Array.Empty<string>(),
            false,
            now,
            now,
            ContentArticle.DefaultLocale);
        db.Articles.Add(corruptArticle);
        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        await using var provider = services.BuildServiceProvider();
        await ContentDevelopmentSeed.ApplyAsync(provider);

        await db.Entry(corruptCategory).ReloadAsync();
        await db.Entry(corruptAuthor).ReloadAsync();
        await db.Entry(corruptArticle).ReloadAsync();

        Assert.Equal(ContentCategoryStatus.Archived, corruptCategory.Status);
        Assert.DoesNotContain("?", corruptCategory.Name);
        Assert.StartsWith("Archived seed", corruptCategory.Name, StringComparison.Ordinal);

        Assert.False(corruptAuthor.IsActive);
        Assert.DoesNotContain("?", corruptAuthor.DisplayName);
        Assert.StartsWith("Sample author", corruptAuthor.DisplayName, StringComparison.Ordinal);

        Assert.DoesNotContain("?", corruptArticle.Title);
        Assert.DoesNotContain("?", corruptArticle.Excerpt);
        Assert.Equal("پیش‌نویس اصلاح‌شده", corruptArticle.Title);

        var authorOnlyArticle = ContentArticle.Create(
            "corrupt-author-label-r1",
            "Clean title",
            "Clean excerpt",
            "body text for repair",
            null,
            null,
            "??????? leftover",
            Array.Empty<string>(),
            false,
            now,
            now,
            ContentArticle.DefaultLocale);
        db.Articles.Add(authorOnlyArticle);
        await db.SaveChangesAsync();
        await ContentDevelopmentSeed.ApplyAsync(provider);
        await db.Entry(authorOnlyArticle).ReloadAsync();
        Assert.Equal("Clean title", authorOnlyArticle.Title);
        Assert.Equal("نویسنده نمونه", authorOnlyArticle.AuthorDisplayName);
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
