using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Infrastructure.Grid;
using Tooba.Content.Infrastructure.Persistence;
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
        var request = ContentAdminGridPolicies.NormalizeArticles(
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
        db.Articles.Add(ContentArticle.Create("a", "Alpha", "e", "b", null, null, "a", [], false, now, now, "fa", null, null, "x"));
        db.Articles.Add(ContentArticle.Create("b", "Beta", "e", "b", null, null, "a", [], false, now, now, "fa", null, null, "x"));
        db.Articles.Add(ContentArticle.Create("c", "Gamma", "e", "b", null, null, "a", [], false, now, now, "fa", null, null, "x"));
        await db.SaveChangesAsync();

        var engine = new AdminContentGridQueryEngine(db);
        var request = ContentAdminGridPolicies.NormalizeArticles(
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
        var contentComposer = Path.GetFullPath(Path.Combine(
            root, "..", "..", "Modules", "Content", "Tooba.Content.Infrastructure", "Adapters", "ContentGridAdapters.cs"));
        var files = new[]
        {
            Path.Combine(root, "Admin", "Panel", "AdminPanelComposer.cs"),
            contentComposer,
            Path.Combine(root, "Reviews", "ReviewPanelComposer.cs"),
            Path.Combine(root, "Story", "StoryPanelComposer.cs"),
        };

        var returnsEndpoints = Path.Combine(
            root, "..", "..", "Modules", "Returns", "Tooba.Returns.Endpoints", "Admin", "ReturnAdminEndpoints.cs");
        Assert.True(File.Exists(returnsEndpoints), $"missing {returnsEndpoints}");
        var returnsText = File.ReadAllText(returnsEndpoints);
        Assert.DoesNotContain("AdminListGridPolicies.Returns", returnsText);
        Assert.DoesNotContain("ReturnPanelComposer", returnsText);
        Assert.False(Directory.Exists(Path.Combine(root, "Returns")));

        var settlementEndpoints = Path.Combine(
            root, "..", "..", "Modules", "Settlement", "Tooba.Settlement.Endpoints", "Admin", "SettlementAdminEndpoints.cs");
        Assert.True(File.Exists(settlementEndpoints), $"missing {settlementEndpoints}");
        var settlementText = File.ReadAllText(settlementEndpoints);
        Assert.DoesNotContain("AdminListGridPolicies.Payouts", settlementText);
        Assert.DoesNotContain("SettlementDbContext", settlementText);
        Assert.DoesNotContain("PartyDbContext", settlementText);
        Assert.DoesNotContain("BoundedListGridQueryEngine", settlementText);
        Assert.DoesNotContain("InMemoryGridQueryEngine", settlementText);
        Assert.False(File.Exists(Path.Combine(root, "Settlement", "SettlementEndpoints.cs")));

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
        var hostRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host"));
        var modules = Path.GetFullPath(Path.Combine(hostRoot, "..", "..", "Modules"));
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Grid")));

        var contentEngine = Path.Combine(modules, "Content", "Tooba.Content.Infrastructure", "Grid", "AdminContentGridQueryEngine.cs");
        var engines = new[]
        {
            contentEngine,
            Path.Combine(modules, "Story", "Tooba.Story.Infrastructure", "Grid", "AdminStoryGridQueryEngine.cs"),
            Path.Combine(modules, "Reviews", "Tooba.Reviews.Infrastructure", "Grid", "AdminReviewGridQueryEngine.cs"),
        };

        foreach (var path in engines)
        {
            Assert.True(File.Exists(path), $"missing {path}");
            var text = File.ReadAllText(path);
            Assert.True(
                text.Contains("EfGridQuery.PageAsync", StringComparison.Ordinal)
                || text.Contains("AdminEfGridQuery.PageAsync", StringComparison.Ordinal)
                || (text.Contains("Skip(", StringComparison.Ordinal)
                    && text.Contains("Take(", StringComparison.Ordinal)
                    && (text.Contains("CountAsync", StringComparison.Ordinal)
                        || text.Contains("Count(", StringComparison.Ordinal))),
                $"{Path.GetFileName(path)} must page via EfGridQuery.PageAsync or Count+Skip+Take");
            Assert.DoesNotContain("BoundedListGridQueryEngine", text);
            Assert.DoesNotContain("InMemoryGridQueryEngine", text);
        }

        var sellersEngine = File.ReadAllText(Path.Combine(
            modules, "Party", "Tooba.Party.Infrastructure", "Grid", "AdminSellersGridQueryEngine.cs"));
        Assert.DoesNotContain("OrderDbContext", sellersEngine, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", sellersEngine, StringComparison.Ordinal);
        Assert.Contains("IAdminSellerOrderCountPort", sellersEngine, StringComparison.Ordinal);
        Assert.Contains("Skip(", sellersEngine, StringComparison.Ordinal);
        Assert.Contains("Take(", sellersEngine, StringComparison.Ordinal);

        var moduleEngines = new[]
        {
            Path.Combine(modules, "Order", "Tooba.Order.Infrastructure", "Admin", "OrdersGrid", "AdminOrdersGridReader.cs"),
            Path.Combine(modules, "Order", "Tooba.Order.Infrastructure", "Admin", "Customers", "AdminCustomersGridReader.cs"),
            Path.Combine(modules, "Fulfillment", "Tooba.Fulfillment.Infrastructure", "Queries", "AdminFulfillmentWorkQueueQueryEngine.cs"),
            Path.Combine(modules, "Returns", "Tooba.Returns.Infrastructure", "Queries", "AdminReturnGridQueryEngine.cs"),
            Path.Combine(modules, "Settlement", "Tooba.Settlement.Infrastructure", "Queries", "AdminPayoutGridQueryEngine.cs"),
        };
        foreach (var path in moduleEngines)
        {
            Assert.True(File.Exists(path), $"missing {path}");
            var text = File.ReadAllText(path);
            Assert.True(
                text.Contains("EfGridQuery.PageAsync", StringComparison.Ordinal)
                || (text.Contains("CountAsync", StringComparison.Ordinal)
                    && text.Contains("Skip(", StringComparison.Ordinal)
                    && text.Contains("Take(", StringComparison.Ordinal)),
                $"{Path.GetFileName(path)} must page via EfGridQuery.PageAsync or CountAsync+Skip+Take");
        }

        var helper = File.ReadAllText(Path.Combine(
            hostRoot, "..", "..", "BuildingBlocks", "Tooba.Persistence", "Grid", "EfGridQuery.cs"));
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

