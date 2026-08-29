using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Events;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation Catalog: محصول توصیفی جدا از Offer/قیمت/موجودی و ایزولهٔ Tenant.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogFoundationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <summary>
    /// Postgres واقعی را برای isolation بالا می‌آورد.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_catalog_a")
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
    public void Product_has_no_price_stock_seller_or_offer_properties()
    {
        var names = typeof(CatalogProduct).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("ListPrice", names);
        Assert.DoesNotContain("Stock", names);
        Assert.DoesNotContain("Quantity", names);
        Assert.DoesNotContain("Inventory", names);
        Assert.DoesNotContain("SellerId", names);
        Assert.DoesNotContain("OfferId", names);
        Assert.DoesNotContain("IsPurchasable", names);
        Assert.Contains("Status", names);
    }

    [Fact]
    public void Catalog_projects_do_not_reference_masstransit_authzed_or_foreign_persistence()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Application"),
                     Path.Combine(root, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Infrastructure"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tooba.Identity", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Party", csproj, StringComparison.Ordinal);
        }

        Assert.Equal("catalog", CatalogDbContext.Schema);
        Assert.DoesNotContain("MassTransit", typeof(CatalogProduct).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("Authzed.Net", typeof(CatalogProduct).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("MassTransit", typeof(ICatalogDirectory).Assembly.GetReferencedAssemblies().Select(a => a.Name));
    }

    [Fact]
    public void Brand_is_descriptive_not_commercial()
    {
        var names = typeof(CatalogBrand).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("SellerId", names);
        Assert.DoesNotContain("Commission", names);
        Assert.DoesNotContain("Price", names);
        Assert.Contains("SlugSeam", names);
    }

    [SkippableFact]
    public async Task Catalog_persistence_invariants_and_tenant_isolation_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_catalog_b'";
            var exists = await cmd.ExecuteScalarAsync();
            if (exists is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_catalog_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_catalog_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-a", "tenant-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-b", "tenant-b"));

        await using var dbA = CreateCatalogDb(csA, commerceA);
        await using var dbB = CreateCatalogDb(csB, commerceB);
        await dbA.Database.EnsureCreatedAsync();
        await dbB.Database.EnsureCreatedAsync();

        var dirA = new CatalogDirectory(dbA, new OpenCatalogUseCaseGuard());
        var dirB = new CatalogDirectory(dbB, new OpenCatalogUseCaseGuard());
        var namesFaEn = new Dictionary<string, string> { ["fa-IR"] = "پیراهن", ["en-US"] = "Shirt" };

        var category = await dirA.CreateCategoryAsync(null, namesFaEn, CancellationToken.None);
        var child = await dirA.CreateCategoryAsync(category.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "تابستانی" }, CancellationToken.None);
        var leaf = await dirA.CreateCategoryAsync(child.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "نخی" }, CancellationToken.None);
        Assert.Equal(category.CategoryId, child.ParentCategoryId);
        Assert.Equal(child.CategoryId, leaf.ParentCategoryId);

        var brand = await dirA.CreateBrandAsync("acme", namesFaEn, CancellationToken.None);
        var colorId = await dirA.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await dirA.AddAttributeOptionAsync(colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "سیاه" }, CancellationToken.None);
        var white = await dirA.AddAttributeOptionAsync(colorId, "white", new Dictionary<string, string> { ["fa-IR"] = "سفید" }, CancellationToken.None);
        var weightId = await dirA.CreateAttributeDefinitionAsync(
            "weight_g",
            CatalogAttributeValueKind.Number,
            isVariantAxis: false,
            new Dictionary<string, string> { ["fa-IR"] = "وزن" },
            CancellationToken.None);

        var product = await dirA.CreateProductAsync(CatalogProductKind.PhysicalGood, "shirt-model-x", brand.BrandId, namesFaEn, CancellationToken.None);
        await dirA.AssignCategoryAsync(product.ProductId, leaf.CategoryId, CancellationToken.None);
        await dirA.AttachMediaReferenceAsync(product.ProductId, Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), CancellationToken.None);
        await dirA.SetProductAttributeAsync(product.ProductId, weightId, "250.5", null, CancellationToken.None);
        await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
            dirA, product.ProductId, "توضیح سئو پیراهن مدل X", CancellationToken.None);
        await dirA.PublishProductAsync(product.ProductId, CancellationToken.None);

        var v1 = await dirA.CreateVariantAsync(
            product.ProductId,
            "SHIRT-BLK",
            [(colorId, "ignored", black)],
            CancellationToken.None);
        await Assert.ThrowsAnyAsync<Exception>(() => dirA.CreateVariantAsync(
            product.ProductId,
            "SHIRT-BLK-2",
            [(colorId, "ignored", black)],
            CancellationToken.None));
        var v2 = await dirA.CreateVariantAsync(
            product.ProductId,
            "SHIRT-WHT",
            [(colorId, "ignored", white)],
            CancellationToken.None);
        Assert.NotEqual(v1.VariantId, v2.VariantId);
        Assert.Equal(product.ProductId, v1.ProductId);

        var locales = await dbA.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerId == product.ProductId && x.FieldKey == "name")
            .Select(x => x.Locale)
            .ToListAsync();
        Assert.Contains("fa-IR", locales);
        Assert.Contains("en-US", locales);

        var published = await dbA.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(CatalogPublicationStatus.Published, published.Status);

        var outbox = await dbA.OutboxMessages.AsNoTracking().ToListAsync();
        Assert.Contains(outbox, row => row.EventType == CatalogProductCreatedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == CatalogProductPublishedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == CatalogVariantCreatedIntegrationEvent.EventTypeName);
        Assert.DoesNotContain(outbox, row => row.Payload.Contains("Price", StringComparison.OrdinalIgnoreCase) && row.Payload.Contains("99.9"));

        var other = await dirB.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "other",
            null,
            new Dictionary<string, string> { ["en-US"] = "Other" },
            CancellationToken.None);
        Assert.Null(await dirB.FindProductAsync(product.ProductId, CancellationToken.None));
        Assert.Null(await dirA.FindProductAsync(other.ProductId, CancellationToken.None));
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

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
