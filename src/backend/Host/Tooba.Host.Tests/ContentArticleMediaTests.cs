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

/// <summary>TB-P08-T005: رسانهٔ مقاله — گالری، SEO، featured، unassign.</summary>
[Collection("PostgresSerial")]
public sealed class ContentArticleMediaTests : IAsyncLifetime
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
                .WithDatabase("tooba_content_media")
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

    /// <summary>featured/gallery/seo، reorder، unassign و fallback SEO.</summary>
    [SkippableFact]
    public async Task Article_media_featured_gallery_seo_reorder_and_unassign()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var media = new PermissiveMediaValidator();
        var categories = new ContentCategoryDirectory(db);
        var authors = new ContentAuthorDirectory(db);
        var languages = new PermissiveLanguageDirectory();
        var content = new ContentDirectory(db, languages, categories, authors, new ContentTagDirectory(db));
        var articleMedia = new ContentArticleMediaDirectory(db, media);

        var author = await authors.CreateAsync(
            new CreateContentAuthorCommand("تحریریه", "editorial", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        var article = await content.CreateAsync(
            new CreateArticleCommand(
                "media-article",
                "مقاله رسانه",
                "چکیده",
                "<p>بدنه</p>",
                null,
                author.Id,
                [],
                false,
                DateTimeOffset.UtcNow,
                "fa-IR",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var featuredId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000001");
        var galleryA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000002");
        var galleryB = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000003");
        var seoId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000004");

        var ws = await articleMedia.AssignFeaturedAsync(article.ArticleId, featuredId, CancellationToken.None);
        Assert.Equal(featuredId, ws.FeaturedMediaAssetId);
        Assert.Equal(featuredId, ws.EffectiveSeoImageMediaAssetId);

        ws = await articleMedia.AddGalleryItemsAsync(article.ArticleId, [galleryA, galleryB], CancellationToken.None);
        Assert.Equal(2, ws.Gallery.Count);

        ws = await articleMedia.ReorderGalleryAsync(article.ArticleId, [galleryB, galleryA], CancellationToken.None);
        Assert.Equal(galleryB, ws.Gallery[0].MediaAssetId);

        ws = await articleMedia.PatchGalleryItemAsync(article.ArticleId, galleryA, "alt تست", "caption", CancellationToken.None);
        Assert.Equal("alt تست", ws.Gallery.Single(x => x.MediaAssetId == galleryA).AltText);

        ws = await articleMedia.AssignSeoImageAsync(article.ArticleId, seoId, CancellationToken.None);
        Assert.Equal(seoId, ws.EffectiveSeoImageMediaAssetId);

        ws = await articleMedia.RemoveGalleryItemAsync(article.ArticleId, galleryB, CancellationToken.None);
        Assert.Single(ws.Gallery);

        ws = await articleMedia.AssignFeaturedAsync(article.ArticleId, null, CancellationToken.None);
        Assert.Null(ws.FeaturedMediaAssetId);
        Assert.Equal(seoId, ws.EffectiveSeoImageMediaAssetId);

        var refs = await articleMedia.CountStructuredReferencesAsync(galleryA, CancellationToken.None);
        Assert.Equal(1, refs);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            content.UpdateAsync(
                article.ArticleId,
                new UpdateArticleCommand(
                    "مقاله",
                    "چکیده",
                    "<img src=\"data:image/png;base64,abc\" />",
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

    private static ContentDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, ContentDbContext.Schema, typeof(ContentDbContext));
        return new ContentDbContext(options.Options);
    }

    private sealed class PermissiveMediaValidator : IContentMediaAssetValidator
    {
        public Task EnsureReadyAssetExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
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
