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

/// <summary>TB-P08-T008: مسیر عمومی دسته/نویسنده و فیلتر ListPublished.</summary>
[Collection("PostgresSerial")]
public sealed class ContentTaxonomyPublicRoutingTests : IAsyncLifetime
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
                .WithDatabase("tooba_content_taxonomy_public")
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

    [Fact]
    public void ResolveContentLocale_maps_fa_en_aliases()
    {
        Assert.Equal("fa-IR", ContentTaxonomySeoRules.ResolveContentLocale(null));
        Assert.Equal("fa-IR", ContentTaxonomySeoRules.ResolveContentLocale(""));
        Assert.Equal("fa-IR", ContentTaxonomySeoRules.ResolveContentLocale("fa"));
        Assert.Equal("fa-IR", ContentTaxonomySeoRules.ResolveContentLocale("fa-IR"));
        Assert.Equal("en-US", ContentTaxonomySeoRules.ResolveContentLocale("en"));
        Assert.Equal("en-US", ContentTaxonomySeoRules.ResolveContentLocale("en-US"));
    }

    [SkippableFact]
    public async Task Category_and_author_public_routing_plus_list_filters()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var categories = new ContentCategoryDirectory(db);
        var authors = new ContentAuthorDirectory(db);
        var directory = new ContentDirectory(db, new PermissiveLanguageDirectory(), categories, authors, new ContentTagDirectory(db));

        var faCategory = await categories.CreateAsync(
            new CreateContentCategoryCommand("fa-IR", null, "راهنما", "guide", "کوتاه", null, 0),
            CancellationToken.None);
        var enCategory = await categories.CreateAsync(
            new CreateContentCategoryCommand("en-US", null, "Guides", "guide", "Short", null, 0),
            CancellationToken.None);
        var inactiveCategory = await categories.CreateAsync(
            new CreateContentCategoryCommand("fa-IR", null, "بایگانی‌شونده", "archived-cat", null, null, 5),
            CancellationToken.None);
        await categories.UpdateAsync(
            inactiveCategory.Id,
            new UpdateContentCategoryCommand(
                inactiveCategory.Name,
                inactiveCategory.Slug,
                null,
                null,
                inactiveCategory.SortOrder,
                nameof(ContentCategoryStatus.Archived)),
            CancellationToken.None);

        var activeAuthor = await authors.CreateAsync(
            new CreateContentAuthorCommand("نویسنده فعال", "active-writer", "bio", null, null, null, null, null, null, null),
            CancellationToken.None);
        var otherAuthor = await authors.CreateAsync(
            new CreateContentAuthorCommand("نویسنده دیگر", "other-writer", null, null, null, null, null, null, null, null),
            CancellationToken.None);
        var inactiveAuthor = await authors.CreateAsync(
            new CreateContentAuthorCommand("غیرفعال", "inactive-writer", null, null, null, null, null, null, null, null),
            CancellationToken.None);
        await authors.DeactivateAsync(inactiveAuthor.Id, CancellationToken.None);

        var faOnly = await categories.CreateAsync(
            new CreateContentCategoryCommand("fa-IR", null, "فقط فارسی", "fa-only", null, null, 2),
            CancellationToken.None);

        var faPublic = await categories.GetPublicBySlugAsync("fa-IR", "guide", CancellationToken.None);
        Assert.NotNull(faPublic);
        Assert.Equal(faCategory.Id, faPublic!.CategoryId);
        Assert.Equal("/fa/blogs/category/guide", faPublic.CanonicalPath);
        var enPublic = await categories.GetPublicBySlugAsync("en-US", "guide", CancellationToken.None);
        Assert.NotNull(enPublic);
        Assert.Equal(enCategory.Id, enPublic!.CategoryId);
        Assert.Equal("/en/blogs/category/guide", enPublic.CanonicalPath);
        Assert.Null(await categories.GetPublicBySlugAsync("en-US", "fa-only", CancellationToken.None));
        Assert.Null(await categories.GetPublicBySlugAsync("fa-IR", "archived-cat", CancellationToken.None));
        Assert.Null(await categories.GetPublicBySlugAsync("en-US", "missing", CancellationToken.None));
        Assert.Equal(faOnly.Id, (await categories.GetPublicBySlugAsync("fa-IR", "fa-only", CancellationToken.None))!.CategoryId);

        var authorPublic = await authors.GetPublicBySlugAsync("active-writer", "en", CancellationToken.None);
        Assert.NotNull(authorPublic);
        Assert.Equal("/en/blogs/author/active-writer", authorPublic!.CanonicalPath);
        Assert.Null(await authors.GetPublicBySlugAsync("inactive-writer", "fa-IR", CancellationToken.None));

        var past = DateTimeOffset.UtcNow.AddHours(-1);
        var faArticle = await directory.CreateAsync(
            new CreateArticleCommand(
                "taxonomy-fa",
                "مقاله فارسی",
                "چکیده",
                "بدنه",
                null,
                activeAuthor.Id,
                [],
                false,
                past,
                "fa-IR",
                null,
                null,
                faCategory.Name,
                faCategory.Id),
            CancellationToken.None);
        var enArticle = await directory.CreateAsync(
            new CreateArticleCommand(
                "taxonomy-en",
                "English article",
                "Excerpt",
                "Body",
                null,
                activeAuthor.Id,
                [],
                false,
                past,
                "en-US",
                null,
                null,
                enCategory.Name,
                enCategory.Id),
            CancellationToken.None);
        var otherArticle = await directory.CreateAsync(
            new CreateArticleCommand(
                "other-author-fa",
                "دیگر",
                "چکیده",
                "بدنه",
                null,
                otherAuthor.Id,
                [],
                false,
                past,
                "fa-IR",
                null,
                null,
                faCategory.Name,
                faCategory.Id),
            CancellationToken.None);
        await directory.PublishAsync(faArticle.ArticleId, CancellationToken.None);
        await directory.PublishAsync(enArticle.ArticleId, CancellationToken.None);
        await directory.PublishAsync(otherArticle.ArticleId, CancellationToken.None);

        var byCategory = await directory.ListPublishedAsync(
            1, 20, null, "fa-IR", faCategory.Id, null, CancellationToken.None);
        Assert.Equal(2, byCategory.Items.Count);
        Assert.All(byCategory.Items, item => Assert.Equal(faCategory.Id, item.CategoryId));
        Assert.All(byCategory.Items, item => Assert.Equal("guide", item.CategorySlug));
        Assert.All(byCategory.Items, item => Assert.NotNull(item.AuthorSlug));

        var byAuthor = await directory.ListPublishedAsync(
            1, 20, null, null, null, activeAuthor.Id, CancellationToken.None);
        Assert.Equal(2, byAuthor.Items.Count);
        Assert.All(byAuthor.Items, item => Assert.Equal(activeAuthor.Id, item.AuthorId));
        Assert.All(byAuthor.Items, item => Assert.Equal("active-writer", item.AuthorSlug));
        Assert.Contains(byAuthor.Items, item => item.Locale == "fa-IR");
        Assert.Contains(byAuthor.Items, item => item.Locale == "en-US");

        var authorFaOnly = await directory.ListPublishedAsync(
            1, 20, null, "fa-IR", null, activeAuthor.Id, CancellationToken.None);
        Assert.Single(authorFaOnly.Items);
        Assert.Equal("taxonomy-fa", authorFaOnly.Items[0].Slug);

        var detail = await directory.GetPublishedBySlugAsync("taxonomy-fa", "fa-IR", CancellationToken.None);
        Assert.NotNull(detail);
        Assert.Equal("guide", detail!.CategorySlug);
        Assert.Equal("active-writer", detail.AuthorSlug);
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
