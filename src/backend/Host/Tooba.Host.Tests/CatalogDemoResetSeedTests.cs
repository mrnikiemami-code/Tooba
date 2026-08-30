using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin.CatalogDemo;
using Tooba.Media.Infrastructure;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P07-T033: ایمنی و foundation دانه Catalog Demo.</summary>
[Collection("PostgresSerial")]
public sealed class CatalogDemoResetSeedTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;
    private string? _tempRoot;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "tooba-catalog-demo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_catalog_demo")
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

        if (_tempRoot is not null && Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
            catch
            {
                // ignore
            }
        }
    }

    [Fact]
    public void Production_guard_blocks()
    {
        var host = CreateHost(envName: "Production", allow: true);
        var ex = Assert.Throws<InvalidOperationException>(() => host.EnsureSafetyOrThrow());
        Assert.Contains("Production", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Opt_in_required()
    {
        var host = CreateHost(envName: "Development", allow: false);
        var ex = Assert.Throws<InvalidOperationException>(() => host.EnsureSafetyOrThrow());
        Assert.Contains("AllowResetAndSeed", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Architecture_smoke_no_product_price_or_stock()
    {
        Assert.DoesNotContain("Price", typeof(CatalogProduct).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("Stock", typeof(CatalogProduct).GetProperties().Select(p => p.Name));
        Assert.Equal(15, CatalogDemoMatrix.Roots.Count);
        Assert.True(CatalogDemoMatrix.Brands.Count >= 20);
        Assert.InRange(CatalogDemoMatrix.Tags.Count, 30, 50);
        Assert.All(CatalogDemoMatrix.Roots, r => Assert.NotEmpty(r.Children));
        Assert.Contains(CatalogDemoMatrix.Roots, r => r.Children.Count == 1);
        Assert.Contains(CatalogDemoMatrix.Roots, r => r.Children.Count >= 2);
    }

    [SkippableFact]
    public async Task Development_opt_in_succeeds_idempotent_and_integrity()
    {
        Skip.If(!_dockerAvailable || _container is null || _tempRoot is null,
            "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-catalog-demo", "tenant-catalog-demo"));

        await using var catalogDb = CreateCatalogDb(cs, commerce);
        await catalogDb.Database.EnsureCreatedAsync();
        await using var mediaDb = CreateMediaDb(cs);
        await mediaDb.Database.MigrateAsync();

        var store = new LocalFileMediaStore(_tempRoot);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Media:MaxUploadBytes"] = "5000000",
                ["Tooba:CatalogDemo:AllowResetAndSeed"] = "true",
            })
            .Build();
        var mediaDirectory = new MediaDirectory(mediaDb, store, config);
        var catalogDirectory = new CatalogDirectory(catalogDb, new OpenCatalogUseCaseGuard());
        var mediaFactory = new CatalogDemoMediaFactory(mediaDirectory);
        var reset = new CatalogDemoResetService(catalogDb, mediaDb, store);
        var productSeed = new CatalogDemoProductSeedService(
            catalogDirectory,
            catalogDb,
            mediaFactory,
            NullLogger<CatalogDemoProductSeedService>.Instance);
        var seed = new CatalogDemoSeedService(
            catalogDirectory,
            catalogDb,
            mediaFactory,
            productSeed,
            NullLogger<CatalogDemoSeedService>.Instance);
        var options = Options.Create(new CatalogDemoSeedOptions { AllowResetAndSeed = true });
        var env = new StubHostEnvironment("Development");
        var host = new CatalogDemoResetAndSeedHost(
            env,
            options,
            reset,
            seed,
            catalogDb,
            NullLogger<CatalogDemoResetAndSeedHost>.Instance);

        var first = await host.ExecuteAsync(printPlan: false, CancellationToken.None);
        Assert.Equal(15, first.Counts.Roots);
        Assert.True(first.Counts.Brands >= 20);
        Assert.True(first.Counts.Tags >= 30);
        Assert.True(first.Counts.L3 > 0);
        Assert.InRange(first.Counts.Products, 219, 365);

        var products = await catalogDb.Products.AsNoTracking().ToListAsync();
        Assert.Equal(first.Counts.Products, products.Count);
        Assert.All(products, p => Assert.Equal(CatalogPublicationStatus.Draft, p.Status));
        Assert.Equal(0, products.Count(p => p.Status == CatalogPublicationStatus.Published));
        Assert.Equal(0, products.Count(p => p.Status == CatalogPublicationStatus.Archived));
        Assert.All(
            products,
            p => Assert.StartsWith(CatalogDemoSeam.ProductSlugPrefix, p.SlugSeam ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        var parentMap = await catalogDb.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.CategoryId, c => c.ParentCategoryId);
        var demoCategoryIds = await catalogDb.CategoryTranslations.AsNoTracking()
            .Where(t => t.Slug.StartsWith(CatalogDemoSeam.CategorySlugPrefix))
            .Select(t => t.CategoryId)
            .Distinct()
            .ToListAsync();
        foreach (var id in demoCategoryIds)
        {
            var level = CatalogCategoryTreeRules.GetCategoryLevel(id, parentMap);
            if (level == 3)
            {
                Assert.True(CatalogCategoryTreeRules.IsAssignableProductCategory(id, parentMap));
            }
            else
            {
                Assert.False(CatalogCategoryTreeRules.IsAssignableProductCategory(id, parentMap));
            }
        }

        // Every L3 has 3–5 demo products (by slug suffix pattern demo-prod-{key}-{n}).
        var leafIds = demoCategoryIds
            .Where(id => CatalogCategoryTreeRules.GetCategoryLevel(id, parentMap) == 3)
            .ToList();
        foreach (var leafId in leafIds)
        {
            var count = await catalogDb.ProductCategories.AsNoTracking()
                .CountAsync(pc => pc.CategoryId == leafId && pc.Role == CatalogProductCategoryRole.Primary);
            Assert.InRange(count, 3, 5);
        }

        var sample = products[0];
        var mediaCount = await catalogDb.MediaReferences.AsNoTracking().CountAsync(m => m.ProductId == sample.ProductId);
        Assert.Equal(5, mediaCount);
        Assert.Equal(1, await catalogDb.MediaReferences.AsNoTracking()
            .CountAsync(m => m.ProductId == sample.ProductId && m.IsPrimary));

        var rootsWithMedia = await catalogDb.Categories.AsNoTracking()
            .Where(c => c.ParentCategoryId == null)
            .Join(
                catalogDb.CategoryTranslations.AsNoTracking()
                    .Where(t => t.Slug.StartsWith(CatalogDemoSeam.CategorySlugPrefix)),
                c => c.CategoryId,
                t => t.CategoryId,
                (c, _) => c)
            .Distinct()
            .ToListAsync();
        Assert.Equal(15, rootsWithMedia.Count);
        Assert.All(rootsWithMedia, c =>
        {
            Assert.NotNull(c.ImageMediaAssetId);
            Assert.NotNull(c.IconMediaAssetId);
            Assert.NotNull(c.BannerMediaAssetId);
        });

        var second = await host.ExecuteAsync(printPlan: false, CancellationToken.None);
        Assert.Equal(first.Counts.Roots, second.Counts.Roots);
        Assert.Equal(first.Counts.Brands, second.Counts.Brands);
        Assert.Equal(first.Counts.Tags, second.Counts.Tags);
        Assert.Equal(first.Counts.Products, second.Counts.Products);
        Assert.Equal(15, second.Counts.Roots);
        Assert.Equal(first.Counts.Products, await catalogDb.Products.CountAsync());
        Assert.Equal(0, await catalogDb.Products.CountAsync(p => p.Status == CatalogPublicationStatus.Published));

        // Seed بدون reset باید بدون تکرار ریشه/محصول بماند.
        var replay = await host.SeedOnlyAsync(CancellationToken.None);
        Assert.Equal(15, replay.Roots);
        Assert.Equal(first.Counts.Products, replay.Products);
        Assert.True(replay.IdempotentReplay);
        Assert.Equal(first.Counts.Products, await catalogDb.Products.CountAsync());
    }

    [SkippableFact]
    public async Task Testing_environment_with_opt_in_succeeds()
    {
        Skip.If(!_dockerAvailable || _container is null || _tempRoot is null,
            "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-catalog-demo-t", "tenant-catalog-demo-t"));

        await using var catalogDb = CreateCatalogDb(cs, commerce);
        await catalogDb.Database.EnsureCreatedAsync();
        await using var mediaDb = CreateMediaDb(cs);
        try
        {
            await mediaDb.Database.MigrateAsync();
        }
        catch
        {
            // schema ممکن است از تست قبلی موجود باشد.
        }

        var store = new LocalFileMediaStore(_tempRoot);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Media:MaxUploadBytes"] = "5000000",
            })
            .Build();
        var mediaDirectory = new MediaDirectory(mediaDb, store, config);
        var catalogDirectory = new CatalogDirectory(catalogDb, new OpenCatalogUseCaseGuard());
        var mediaFactory = new CatalogDemoMediaFactory(mediaDirectory);
        var productSeed = new CatalogDemoProductSeedService(
            catalogDirectory,
            catalogDb,
            mediaFactory,
            NullLogger<CatalogDemoProductSeedService>.Instance);
        var host = new CatalogDemoResetAndSeedHost(
            new StubHostEnvironment("Testing"),
            Options.Create(new CatalogDemoSeedOptions { AllowResetAndSeed = true }),
            new CatalogDemoResetService(catalogDb, mediaDb, store),
            new CatalogDemoSeedService(
                catalogDirectory,
                catalogDb,
                mediaFactory,
                productSeed,
                NullLogger<CatalogDemoSeedService>.Instance),
            catalogDb,
            NullLogger<CatalogDemoResetAndSeedHost>.Instance);

        var result = await host.ExecuteAsync(printPlan: false, CancellationToken.None);
        Assert.Equal(15, result.Counts.Roots);
    }

    private static CatalogDemoResetAndSeedHost CreateHost(string envName, bool allow) =>
        new(
            new StubHostEnvironment(envName),
            Options.Create(new CatalogDemoSeedOptions { AllowResetAndSeed = allow }),
            reset: null!,
            seed: null!,
            db: null!,
            NullLogger<CatalogDemoResetAndSeedHost>.Instance);

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

    private static MediaDbContext CreateMediaDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, MediaDbContext.Schema, typeof(MediaDbContext));
        return new MediaDbContext(options.Options);
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(string name) => EnvironmentName = name;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
