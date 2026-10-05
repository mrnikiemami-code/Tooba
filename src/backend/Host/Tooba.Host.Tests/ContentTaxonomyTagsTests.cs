using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application.Articles.Commands;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Application.Authors.Commands;
using Tooba.Content.Application.Authors.Models;
using Tooba.Content.Application.Authors.Ports;
using Tooba.Content.Application.Categories.Commands;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Application.Tags.Commands;
using Tooba.Content.Application.Tags.Models;
using Tooba.Content.Application.Tags.Ports;
using Testcontainers.PostgreSql;


using Tooba.Localization.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Directories;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Content.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P08-T013: عمق ۲، انتساب L1/L2، و برچسب محتوا.</summary>
[Collection("PostgresSerial")]
public sealed class ContentTaxonomyTagsTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_content_taxonomy_tags")
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
    public async Task Category_depth_two_and_tags_language_rules()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var categories = new ContentCategoryDirectory(db);
        var tags = new ContentTagDirectory(db);
        var authors = new ContentAuthorDirectory(db);
        var languages = new PermissiveLanguageDirectory();
        var content = new ContentDirectory(db, languages, categories, authors, tags);

        var faRoot = await categories.CreateAsync(
            new CreateCategoryCommand("fa-IR", null, "راهنمای خرید", "buying-guide", null, null, 0),
            CancellationToken.None);
        var faChild = await categories.CreateAsync(
            new CreateCategoryCommand("fa-IR", faRoot.Id, "موبایل", "mobile", null, null, 1),
            CancellationToken.None);

        var level3 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.CreateAsync(
                new CreateCategoryCommand("fa-IR", faChild.Id, "آیفون", "iphone", null, null, 2),
                CancellationToken.None));
        Assert.Equal(ContentErrorCodes.CategoryMaxDepthExceeded, level3.Message);

        var otherRoot = await categories.CreateAsync(
            new CreateCategoryCommand("fa-IR", null, "اخبار", "news", null, null, 3),
            CancellationToken.None);
        var moveDepth = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.MoveAsync(new MoveCategoryCommand(faRoot.Id, otherRoot.Id), CancellationToken.None));
        // moving L1 with child under another L1 would make child depth 3
        Assert.Equal(ContentErrorCodes.CategoryMaxDepthExceeded, moveDepth.Message);

        var enRoot = await categories.CreateAsync(
            new CreateCategoryCommand("en-US", null, "Guides", "guides", null, null, 0),
            CancellationToken.None);
        var crossLang = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.CreateAsync(
                new CreateCategoryCommand("fa-IR", enRoot.Id, "bad", "bad-child", null, null, 4),
                CancellationToken.None));
        Assert.Equal(ContentErrorCodes.CategoryCrossLanguageParent, crossLang.Message);

        var author = await authors.CreateAsync(
            new CreateAuthorCommand("نویسنده", "tax-author", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        var articleL1 = await content.CreateAsync(
            new CreateArticleCommand(
                "tax-l1",
                "مقاله L1",
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
        Assert.Equal(faRoot.Id, articleL1.CategoryId);

        var articleL2 = await content.UpdateAsync(
            new UpdateArticleCommand(articleL1.ArticleId, 
                articleL1.Title,
                articleL1.Excerpt,
                articleL1.Body,
                articleL1.CoverMediaAssetId,
                author.Id,
                [],
                false,
                "fa-IR",
                null,
                null,
                faChild.Name,
                faChild.Id,
                null),
            CancellationToken.None);
        Assert.Equal(faChild.Id, articleL2.CategoryId);

        var mismatch = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            content.UpdateAsync(
                new UpdateArticleCommand(articleL2.ArticleId, 
                    articleL2.Title,
                    articleL2.Excerpt,
                    articleL2.Body,
                    articleL2.CoverMediaAssetId,
                    author.Id,
                    [],
                    false,
                    "fa-IR",
                    null,
                    null,
                    enRoot.Name,
                    enRoot.Id,
                    null),
                CancellationToken.None));
        Assert.Equal(ContentErrorCodes.CategoryLanguageMismatch, mismatch.Message);

        var tagA = await tags.CreateAsync(new CreateTagCommand("fa-IR", "راهنما", null), CancellationToken.None);
        var tagB = await tags.CreateAsync(new CreateTagCommand("fa-IR", "خرید", null), CancellationToken.None);
        var dup = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tags.CreateAsync(new CreateTagCommand("fa-IR", "  راهنما  ", null), CancellationToken.None));
        Assert.Equal(ContentErrorCodes.TagDuplicateName, dup.Message);

        var assigned = await tags.AssignToArticleAsync(articleL2.ArticleId, tagA.TagId, CancellationToken.None);
        Assert.Contains(assigned, t => t.TagId == tagA.TagId);
        var idempotent = await tags.AssignToArticleAsync(articleL2.ArticleId, tagA.TagId, CancellationToken.None);
        Assert.Equal(1, idempotent.Count);

        await tags.AssignToArticleAsync(articleL2.ArticleId, tagB.TagId, CancellationToken.None);
        var afterRemove = await tags.RemoveFromArticleAsync(articleL2.ArticleId, tagA.TagId, CancellationToken.None);
        Assert.DoesNotContain(afterRemove, t => t.TagId == tagA.TagId);
        Assert.Contains(afterRemove, t => t.TagId == tagB.TagId);

        var enTag = await tags.CreateAsync(new CreateTagCommand("en-US", "guide", null), CancellationToken.None);
        var tagLang = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tags.AssignToArticleAsync(articleL2.ArticleId, enTag.TagId, CancellationToken.None));
        Assert.Equal(ContentErrorCodes.TagLanguageMismatch, tagLang.Message);

        var search = await tags.SearchAsync("fa-IR", "خر", 10, true, CancellationToken.None);
        Assert.Contains(search, t => t.TagId == tagB.TagId);
    }

    private static ContentDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, ContentDbContext.Schema, typeof(ContentDbContext));
        return new ContentDbContext(options.Options);
    }

    private sealed class PermissiveLanguageDirectory : Tooba.Localization.Contracts.Ports.ILanguageActivationPort
    {
        public Task EnsureActiveAsync(string languageCode, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> IsActiveAsync(string languageCode, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}


