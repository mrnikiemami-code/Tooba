using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Host.Grid;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>اثبات DB-native: Count + Skip/Take قبل از materialize برای Content.</summary>
public sealed class AdminDbNativeGridQueryTests
{
    [Fact]
    public async Task Content_query_pages_before_full_materialization()
    {
        await using var db = CreateContentDb();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            db.Articles.Add(ContentArticle.Create(
                $"slug-{i}",
                $"Title {i}",
                "excerpt",
                "body",
                null,
                "author",
                [],
                false,
                now.AddMinutes(-i),
                now.AddMinutes(-i),
                "fa",
                null,
                null,
                "news"));
        }

        await db.SaveChangesAsync();

        var engine = new AdminContentGridQueryEngine(db);
        var request = AdminListGridPolicies.Content.Normalize(
            new GridQueryRequest(1, 2, null, [new GridSortRequest("updated", "desc")], [], null));

        var page = await engine.QueryAsync(request, CancellationToken.None);

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal("Title 0", page.Items[0].Title);
        Assert.Equal("Title 1", page.Items[1].Title);
    }

    [Fact]
    public async Task Content_query_applies_text_filter_before_paging()
    {
        await using var db = CreateContentDb();
        var now = DateTimeOffset.UtcNow;
        db.Articles.Add(ContentArticle.Create("a", "Alpha", "e", "b", null, "a", [], false, now, now, "fa", null, null, "x"));
        db.Articles.Add(ContentArticle.Create("b", "Beta", "e", "b", null, "a", [], false, now, now, "fa", null, null, "x"));
        db.Articles.Add(ContentArticle.Create("c", "Gamma", "e", "b", null, "a", [], false, now, now, "fa", null, null, "x"));
        await db.SaveChangesAsync();

        var engine = new AdminContentGridQueryEngine(db);
        var request = AdminListGridPolicies.Content.Normalize(
            new GridQueryRequest(
                1,
                20,
                null,
                [],
                [new GridFilterRequest("title", "contains", "Alph", null, null)],
                null));

        var page = await engine.QueryAsync(request, CancellationToken.None);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Alpha", page.Items[0].Title);
    }

    [Fact]
    public void Non_trivial_composers_do_not_call_in_memory_Execute()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host"));
        var files = new[]
        {
            Path.Combine(root, "Admin", "AdminPanelComposer.cs"),
            Path.Combine(root, "Content", "ContentPanelComposer.cs"),
            Path.Combine(root, "Fulfillment", "FulfillmentPanelComposer.cs"),
            Path.Combine(root, "Returns", "ReturnPanelComposer.cs"),
            Path.Combine(root, "Settlement", "SettlementPanelComposer.cs"),
            Path.Combine(root, "Reviews", "ReviewPanelComposer.cs"),
            Path.Combine(root, "Story", "StoryPanelComposer.cs"),
        };

        foreach (var file in files)
        {
            Assert.True(File.Exists(file), $"missing {file}");
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("AdminListGridPolicies.Orders.Execute", text);
            Assert.DoesNotContain("AdminListGridPolicies.Sellers.Execute", text);
            Assert.DoesNotContain("AdminListGridPolicies.Customers.Execute", text);
            Assert.DoesNotContain("AdminListGridPolicies.Fulfillments.Execute", text);
            Assert.DoesNotContain("AdminListGridPolicies.Returns.Execute", text);
            Assert.DoesNotContain("AdminListGridPolicies.Payouts.Execute", text);
            Assert.DoesNotContain("AdminListGridPolicies.Content.Execute", text);
            Assert.DoesNotContain("AdminListGridPolicies.Reviews.Execute", text);
            Assert.DoesNotContain("AdminListGridPolicies.Stories.Execute", text);
            Assert.DoesNotContain("BoundedListGridQueryEngine", text);
            Assert.DoesNotContain("InMemoryGridQueryEngine", text);
        }
    }

    [Fact]
    public void Non_trivial_engines_keep_iqueryable_until_page()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host", "Grid"));
        var engines = new[]
        {
            "AdminContentGridQueryEngine.cs",
            "AdminOrdersGridQueryEngine.cs",
            "AdminCustomersGridQueryEngine.cs",
            "AdminSellersGridQueryEngine.cs",
            "AdminFulfillmentGridQueryEngine.cs",
            "AdminReturnGridQueryEngine.cs",
            "AdminPayoutGridQueryEngine.cs",
            "AdminReviewGridQueryEngine.cs",
            "AdminStoryGridQueryEngine.cs",
        };

        foreach (var name in engines)
        {
            var path = Path.Combine(root, name);
            Assert.True(File.Exists(path), $"missing {path}");
            var text = File.ReadAllText(path);
            Assert.True(
                text.Contains("AdminEfGridQuery.PageAsync", StringComparison.Ordinal)
                || (text.Contains("CountAsync", StringComparison.Ordinal)
                    && text.Contains("Skip(", StringComparison.Ordinal)
                    && text.Contains("Take(", StringComparison.Ordinal)),
                $"{name} must page via AdminEfGridQuery.PageAsync or CountAsync+Skip+Take");
            Assert.DoesNotContain("BoundedListGridQueryEngine", text);
            Assert.DoesNotContain("InMemoryGridQueryEngine", text);
        }

        var helper = File.ReadAllText(Path.Combine(root, "AdminEfGridQuery.cs"));
        Assert.Contains("CountAsync", helper);
        Assert.Contains("Skip(", helper);
        Assert.Contains("Take(", helper);
    }

    private static ContentDbContext CreateContentDb()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ContentDbContext(options);
    }
}
