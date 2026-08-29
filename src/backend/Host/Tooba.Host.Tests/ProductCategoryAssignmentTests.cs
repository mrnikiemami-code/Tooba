using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// اختصاص محصول فقط به دستهٔ سطح سوم (TB-P07-T016).
/// </summary>
[Collection("PostgresSerial")]
public sealed class ProductCategoryAssignmentTests : IAsyncLifetime
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
                .WithDatabase("tooba_product_category_assign")
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
    public void Category_level_is_one_plus_ancestor_count()
    {
        var l1 = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var l2 = Guid.Parse("22222222-2222-7222-8222-222222222222");
        var l3 = Guid.Parse("33333333-3333-7333-8333-333333333333");
        var parentById = new Dictionary<Guid, Guid?>
        {
            [l1] = null,
            [l2] = l1,
            [l3] = l2,
        };

        Assert.Equal(1, CatalogCategoryTreeRules.GetCategoryLevel(l1, parentById));
        Assert.Equal(2, CatalogCategoryTreeRules.GetCategoryLevel(l2, parentById));
        Assert.Equal(3, CatalogCategoryTreeRules.GetCategoryLevel(l3, parentById));
        Assert.False(CatalogCategoryTreeRules.IsAssignableProductCategory(l1, parentById));
        Assert.False(CatalogCategoryTreeRules.IsAssignableProductCategory(l2, parentById));
        Assert.True(CatalogCategoryTreeRules.IsAssignableProductCategory(l3, parentById));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CatalogCategoryTreeRules.EnsureAssignableProductCategory(l1, parentById));
        Assert.Equal(CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa, ex.Message);
    }

    [SkippableFact]
    public async Task Assign_create_replace_reject_l1_l2_accept_l3()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-cat-assign-l3", "tenant-cat-assign-l3"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var l1 = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "کالای دیجیتال" }, CancellationToken.None);
        var l2 = await dir.CreateCategoryAsync(
            l1.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "موبایل و تبلت" }, CancellationToken.None);
        var l3 = await dir.CreateCategoryAsync(
            l2.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "گوشی موبایل" }, CancellationToken.None);
        var otherL1 = await dir.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "پوشاک" }, CancellationToken.None);
        var otherL2 = await dir.CreateCategoryAsync(
            otherL1.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "کفش" }, CancellationToken.None);
        var otherL3 = await dir.CreateCategoryAsync(
            otherL2.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "کفش ورزشی" }, CancellationToken.None);

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "phone-l3", null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی تست" }, CancellationToken.None);

        var rejectL1 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.AssignCategoryAsync(product.ProductId, l1.CategoryId, CancellationToken.None));
        Assert.Equal(CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa, rejectL1.Message);

        var rejectL2 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.AssignCategoryAsync(product.ProductId, l2.CategoryId, CancellationToken.None));
        Assert.Equal(CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa, rejectL2.Message);

        await dir.AssignCategoryAsync(product.ProductId, l3.CategoryId, CancellationToken.None);
        Assert.True(await db.ProductCategories.AnyAsync(
            x => x.ProductId == product.ProductId && x.CategoryId == l3.CategoryId));

        var rejectReplaceL1 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.ReplaceProductPrimaryCategoryAsync(product.ProductId, otherL1.CategoryId, CancellationToken.None));
        Assert.Equal(CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa, rejectReplaceL1.Message);

        var rejectReplaceL2 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.ReplaceProductPrimaryCategoryAsync(product.ProductId, otherL2.CategoryId, CancellationToken.None));
        Assert.Equal(CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa, rejectReplaceL2.Message);

        var impact = await dir.ReplaceProductPrimaryCategoryAsync(
            product.ProductId, otherL3.CategoryId, CancellationToken.None);
        Assert.Equal(otherL3.CategoryId, impact.NewCategoryId);
        Assert.True(await db.ProductCategories.AnyAsync(
            x => x.ProductId == product.ProductId && x.CategoryId == otherL3.CategoryId));

        await dir.AddProductAdditionalCategoryAsync(product.ProductId, l3.CategoryId, CancellationToken.None);
        Assert.True(await db.ProductCategories.AnyAsync(
            x => x.ProductId == product.ProductId
                 && x.CategoryId == l3.CategoryId
                 && x.Role == CatalogProductCategoryRole.Additional));
        Assert.True(await db.ProductCategories.AnyAsync(
            x => x.ProductId == product.ProductId
                 && x.CategoryId == otherL3.CategoryId
                 && x.Role == CatalogProductCategoryRole.Primary));

        var dupPrimary = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.AddProductAdditionalCategoryAsync(product.ProductId, otherL3.CategoryId, CancellationToken.None));
        Assert.Contains("دسته اصلی", dupPrimary.Message, StringComparison.Ordinal);

        await dir.RemoveProductAdditionalCategoryAsync(product.ProductId, l3.CategoryId, CancellationToken.None);
        Assert.False(await db.ProductCategories.AnyAsync(
            x => x.ProductId == product.ProductId && x.CategoryId == l3.CategoryId));

        var cannotRemovePrimary = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.RemoveProductAdditionalCategoryAsync(product.ProductId, otherL3.CategoryId, CancellationToken.None));
        Assert.Contains("دسته اصلی", cannotRemovePrimary.Message, StringComparison.Ordinal);
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }
}
