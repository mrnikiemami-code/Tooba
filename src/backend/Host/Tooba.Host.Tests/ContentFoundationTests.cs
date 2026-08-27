using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>پوشش foundation Content: draft/publish/unpublish، slug یکتا و ریل خانه.</summary>
[Collection("PostgresSerial")]
public sealed class ContentFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_content")
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

    /// <summary>مرز schema و ثبت دایرکتوری Content.</summary>
    [Fact]
    public void Content_module_boundary_static_checks()
    {
        Assert.Equal("content", ContentDbContext.Schema);
        Assert.NotNull(typeof(IContentDirectory).GetMethod(nameof(IContentDirectory.ListPublishedAsync)));
        Assert.NotNull(typeof(IContentDirectory).GetMethod(nameof(IContentDirectory.GetPublishedBySlugAsync)));
        Assert.NotNull(typeof(IContentDirectory).GetMethod(nameof(IContentDirectory.UnpublishAsync)));
        Assert.Equal(nameof(ContentPublicationStatus.Draft), ContentPublicationStatus.Draft.ToString());
    }

    /// <summary>draft عمومی نیست؛ publish با slug دیده می‌شود؛ unpublish پنهان می‌کند؛ slug یکتا است؛ ریل خانه فقط Published.</summary>
    [SkippableFact]
    public async Task Draft_publish_unpublish_slug_and_home_listing_behave()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var directory = new ContentDirectory(db);
        var now = DateTimeOffset.Parse("2026-08-27T00:00:00Z");

        var draft = await directory.CreateAsync(
            new CreateArticleCommand(
                "draft-guide",
                "راهنمای پیش‌نویس",
                "چکیدهٔ پیش‌نویس",
                "بدنهٔ پیش‌نویس برای تست.",
                null,
                "تحریریه تست",
                ["تست"],
                false,
                now,
                ContentArticle.DefaultLocale,
                "SEO پیش‌نویس",
                "توضیح SEO پیش‌نویس",
                "راهنما"),
            CancellationToken.None);

        Assert.Equal(ContentPublicationStatus.Draft, draft.Status);
        Assert.Null(await directory.GetPublishedBySlugAsync("draft-guide", null, CancellationToken.None));
        Assert.Empty((await directory.ListPublishedAsync(1, 20, null, CancellationToken.None)).Items);
        Assert.Empty(await directory.ListPublishedForHomeAsync(6, CancellationToken.None));

        var published = await directory.PublishAsync(draft.ArticleId, CancellationToken.None);
        Assert.Equal(ContentPublicationStatus.Published, published.Status);

        var bySlug = await directory.GetPublishedBySlugAsync("draft-guide", ContentArticle.DefaultLocale, CancellationToken.None);
        Assert.NotNull(bySlug);
        Assert.Equal("بدنهٔ پیش‌نویس برای تست.", bySlug!.Body);
        Assert.Equal("SEO پیش‌نویس", bySlug.SeoTitle);
        Assert.Equal("راهنما", bySlug.Category);

        var listed = await directory.ListPublishedAsync(1, 20, "راهنما", CancellationToken.None);
        Assert.Single(listed.Items);
        Assert.Null(listed.Items[0].Body);

        var home = await directory.ListPublishedForHomeAsync(6, CancellationToken.None);
        Assert.Single(home);
        Assert.Equal("draft-guide", home[0].Slug);

        await directory.UnpublishAsync(draft.ArticleId, CancellationToken.None);
        Assert.Null(await directory.GetPublishedBySlugAsync("draft-guide", null, CancellationToken.None));
        Assert.Empty(await directory.ListPublishedForHomeAsync(6, CancellationToken.None));

        await directory.CreateAsync(
            new CreateArticleCommand(
                "unique-slug",
                "عنوان یک",
                "چکیده یک",
                "بدنه یک",
                null,
                "نویسنده",
                [],
                false,
                now,
                null,
                null,
                null,
                null),
            CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.CreateAsync(
                new CreateArticleCommand(
                    "unique-slug",
                    "عنوان دو",
                    "چکیده دو",
                    "بدنه دو",
                    null,
                    "نویسنده",
                    [],
                    false,
                    now,
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
}
