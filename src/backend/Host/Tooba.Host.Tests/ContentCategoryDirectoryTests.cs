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
            new CreateCategoryCommand("fa-IR", null, "راهنما", "guide", null, null, 0),
            CancellationToken.None);
        var faChild = await categories.CreateAsync(
            new CreateCategoryCommand("fa-IR", faRoot.Id, "خرید", "buying", null, null, 1),
            CancellationToken.None);
        var enRoot = await categories.CreateAsync(
            new CreateCategoryCommand("en-US", null, "Guides", "guides", null, null, 0),
            CancellationToken.None);
        var author = await authors.CreateAsync(
            new CreateAuthorCommand("نویسنده", "article-author", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.CreateAsync(
                new CreateCategoryCommand("fa-IR", null, "راهنمای دیگر", "guide", null, null, 2),
                CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.MoveAsync(new MoveCategoryCommand(
                faChild.Id, enRoot.Id), CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categories.MoveAsync(new MoveCategoryCommand(
                faRoot.Id, faChild.Id), CancellationToken.None));

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

    private sealed class PermissiveLanguageDirectory : Tooba.Localization.Contracts.Ports.ILanguageActivationPort
    {
        public Task EnsureActiveAsync(string languageCode, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> IsActiveAsync(string languageCode, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}


