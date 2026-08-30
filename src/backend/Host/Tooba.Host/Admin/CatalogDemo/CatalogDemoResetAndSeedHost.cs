using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>نتیجهٔ کامل reset+seed با اعتبارسنجی یکپارچگی.</summary>
public sealed record CatalogDemoResetAndSeedResult(
    CatalogDemoResetResult Reset,
    CatalogDemoSeedCounts Counts,
    IReadOnlyList<string> Plan);

/// <summary>
/// ارکستراسیون ایمنی → plan → reset → seed → integrity.
/// </summary>
public sealed class CatalogDemoResetAndSeedHost
{
    private readonly IHostEnvironment _environment;
    private readonly CatalogDemoSeedOptions _options;
    private readonly CatalogDemoResetService _reset;
    private readonly CatalogDemoSeedService _seed;
    private readonly CatalogDbContext _db;
    private readonly ILogger<CatalogDemoResetAndSeedHost> _logger;

    /// <summary>وابستگی‌ها را تزریق می‌کند.</summary>
    public CatalogDemoResetAndSeedHost(
        IHostEnvironment environment,
        IOptions<CatalogDemoSeedOptions> options,
        CatalogDemoResetService reset,
        CatalogDemoSeedService seed,
        CatalogDbContext db,
        ILogger<CatalogDemoResetAndSeedHost> logger)
    {
        _environment = environment;
        _options = options.Value;
        _reset = reset;
        _seed = seed;
        _db = db;
        _logger = logger;
    }

    /// <summary>نگهبان ایمنی fail-closed را اعمال می‌کند.</summary>
    public void EnsureSafetyOrThrow()
    {
        if (_environment.IsProduction())
        {
            throw new InvalidOperationException("Catalog demo reset/seed is blocked in Production.");
        }

        if (!_options.AllowResetAndSeed)
        {
            throw new InvalidOperationException(
                "Catalog demo reset/seed requires Tooba:CatalogDemo:AllowResetAndSeed=true.");
        }

        if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException(
                "Catalog demo reset/seed is allowed only in Development or Testing environments.");
        }
    }

    /// <summary>plan متنی قبل از جهش.</summary>
    public static IReadOnlyList<string> BuildPlan() =>
    [
        $"Reset demo/junk Catalog entities by seam markers ({CatalogDemoSeam.CategorySlugPrefix}*, {CatalogDemoSeam.BrandSlugPrefix}*, …).",
        $"Seed exactly {CatalogDemoMatrix.Roots.Count} L1 roots with varied L2/L3.",
        $"Seed >= {CatalogDemoMatrix.Brands.Count} brands and {CatalogDemoMatrix.Tags.Count} tags.",
        "Seed attribute definitions/options, L3 schemas, selected facets, MegaMenu, category media.",
        "Seed rich Draft publish-ready Products (3–5 per L3; TB-P07-T034).",
    ];

    /// <summary>reset+seed کامل با اعتبارسنجی.</summary>
    public async Task<CatalogDemoResetAndSeedResult> ExecuteAsync(
        bool printPlan,
        CancellationToken cancellationToken)
    {
        EnsureSafetyOrThrow();
        var plan = BuildPlan();
        if (printPlan)
        {
            foreach (var line in plan)
            {
                _logger.LogInformation("CatalogDemo plan: {Line}", line);
            }
        }

        var reset = await _reset.ResetAsync(cancellationToken);
        var counts = await _seed.SeedAsync(cancellationToken);
        ValidateIntegrity(counts);
        return new CatalogDemoResetAndSeedResult(reset, counts, plan);
    }

    /// <summary>فقط seed (بدون reset) — همچنان تحت نگهبان ایمنی.</summary>
    public async Task<CatalogDemoSeedCounts> SeedOnlyAsync(CancellationToken cancellationToken)
    {
        EnsureSafetyOrThrow();
        var counts = await _seed.SeedAsync(cancellationToken);
        ValidateIntegrity(counts);
        return counts;
    }

    /// <summary>شمارش وضعیت demo در برابر کل.</summary>
    public async Task<object> GetStatusAsync(CancellationToken cancellationToken)
    {
        var parentMap = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.CategoryId, c => c.ParentCategoryId, cancellationToken);
        var allCategories = parentMap.Count;
        var demoCategoryIds = await _db.CategoryTranslations.AsNoTracking()
            .Where(t => t.Slug.StartsWith(CatalogDemoSeam.CategorySlugPrefix))
            .Select(t => t.CategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var demoRoots = demoCategoryIds.Count(id => CatalogCategoryTreeRules.GetCategoryLevel(id, parentMap) == 1);
        var brandsTotal = await _db.Brands.CountAsync(cancellationToken);
        var brandsDemo = await _db.Brands.CountAsync(
            b => b.SlugSeam != null && b.SlugSeam.StartsWith(CatalogDemoSeam.BrandSlugPrefix),
            cancellationToken);
        var tagsTotal = await _db.Tags.CountAsync(cancellationToken);
        var tagsDemo = await _db.Tags.CountAsync(
            t => t.Code.StartsWith(CatalogDemoSeam.TagCodePrefix),
            cancellationToken);
        var attrsTotal = await _db.AttributeDefinitions.CountAsync(cancellationToken);
        var attrsDemo = await _db.AttributeDefinitions.CountAsync(
            d => d.Code.StartsWith(CatalogDemoSeam.AttributeCodePrefix),
            cancellationToken);
        var productsTotal = await _db.Products.CountAsync(cancellationToken);

        return new
        {
            rootsDemo = demoRoots,
            categoriesDemo = demoCategoryIds.Count,
            categoriesTotal = allCategories,
            brandsDemo,
            brandsTotal,
            tagsDemo,
            tagsTotal,
            attributesDemo = attrsDemo,
            attributesTotal = attrsTotal,
            productsTotal,
            allowResetAndSeed = _options.AllowResetAndSeed,
            environment = _environment.EnvironmentName,
        };
    }

    private static void ValidateIntegrity(CatalogDemoSeedCounts counts)
    {
        if (counts.Roots != 15)
        {
            throw new InvalidOperationException($"Expected exactly 15 demo roots, found {counts.Roots}.");
        }

        if (counts.Brands < 20)
        {
            throw new InvalidOperationException($"Expected at least 20 demo brands, found {counts.Brands}.");
        }

        if (counts.Tags < 30)
        {
            throw new InvalidOperationException($"Expected at least 30 demo tags, found {counts.Tags}.");
        }

        if (counts.L2 < 15)
        {
            throw new InvalidOperationException($"Expected every root to contribute L2 nodes; found {counts.L2}.");
        }

        if (counts.L3 < 15)
        {
            throw new InvalidOperationException($"Expected L3 leaves for product assignment; found {counts.L3}.");
        }

        if (counts.Products < 219 || counts.Products > 365)
        {
            throw new InvalidOperationException(
                $"Expected 219–365 demo products, found {counts.Products}.");
        }
    }
}
