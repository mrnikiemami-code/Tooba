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
    IReadOnlyList<string> Plan,
    CatalogAssignmentIntegrityAudit AssignmentIntegrity);

/// <summary>
/// ارکستراسیون ایمنی → plan → reset → seed → integrity.
/// </summary>
public sealed class CatalogDemoResetAndSeedHost
{
    private readonly IHostEnvironment _environment;
    private readonly CatalogDemoSeedOptions _options;
    private readonly CatalogDemoResetService _reset;
    private readonly CatalogDemoSeedService _seed;
    private readonly CatalogDemoAssignmentIntegrityService _assignmentIntegrity;
    private readonly CatalogDbContext _db;
    private readonly ILogger<CatalogDemoResetAndSeedHost> _logger;

    /// <summary>وابستگی‌ها را تزریق می‌کند.</summary>
    public CatalogDemoResetAndSeedHost(
        IHostEnvironment environment,
        IOptions<CatalogDemoSeedOptions> options,
        CatalogDemoResetService reset,
        CatalogDemoSeedService seed,
        CatalogDemoAssignmentIntegrityService assignmentIntegrity,
        CatalogDbContext db,
        ILogger<CatalogDemoResetAndSeedHost> logger)
    {
        _environment = environment;
        _options = options.Value;
        _reset = reset;
        _seed = seed;
        _assignmentIntegrity = assignmentIntegrity;
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
        $"Reset ALL Catalog Products in non-production demo Catalog, then demo/junk categories/brands/tags/attrs/media by seam ({CatalogDemoSeam.CategorySlugPrefix}*, {CatalogDemoSeam.BrandSlugPrefix}*, …).",
        $"Seed exactly {CatalogDemoMatrix.Roots.Count} L1 roots with varied L2/L3.",
        $"Seed >= {CatalogDemoMatrix.Brands.Count} brands and {CatalogDemoMatrix.Tags.Count} tags.",
        "Seed attribute definitions/options, L3 schemas, selected facets, MegaMenu, category media.",
        "Seed rich Draft publish-ready Products (3–5 per L3; TB-P07-T034); residual Published Products must not survive reset.",
        "Enforce L3-only Primary/display membership; zero L1/L2 product assignments (TB-P07-T037).",
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
        await EnsureProductLifecycleCleanAsync(counts.Products, cancellationToken);
        await _assignmentIntegrity.EnsureCleanOrThrowAsync(cancellationToken);
        var assignmentIntegrity = await _assignmentIntegrity.AuditAsync(cancellationToken);
        return new CatalogDemoResetAndSeedResult(reset, counts, plan, assignmentIntegrity);
    }

    /// <summary>فقط seed (بدون reset) — همچنان تحت نگهبان ایمنی.</summary>
    public async Task<CatalogDemoSeedCounts> SeedOnlyAsync(CancellationToken cancellationToken)
    {
        EnsureSafetyOrThrow();
        var counts = await _seed.SeedAsync(cancellationToken);
        ValidateIntegrity(counts);
        await _assignmentIntegrity.EnsureCleanOrThrowAsync(cancellationToken);
        return counts;
    }

    /// <summary>ممیزی انتساب سطح دسته برای Admin evidence.</summary>
    public Task<CatalogAssignmentIntegrityAudit> AuditAssignmentsAsync(CancellationToken cancellationToken) =>
        _assignmentIntegrity.AuditAsync(cancellationToken);

    /// <summary>پاکسازی انتساب نامعتبر در غیر Production (بدون reset کامل).</summary>
    public Task<CatalogAssignmentIntegrityCleanupResult> CleanupAssignmentsAsync(CancellationToken cancellationToken)
    {
        EnsureSafetyOrThrow();
        return _assignmentIntegrity.CleanupAsync(cancellationToken);
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
        var productsDraft = await _db.Products.CountAsync(
            p => p.Status == CatalogPublicationStatus.Draft,
            cancellationToken);
        var productsPublished = await _db.Products.CountAsync(
            p => p.Status == CatalogPublicationStatus.Published,
            cancellationToken);
        var productsArchived = await _db.Products.CountAsync(
            p => p.Status == CatalogPublicationStatus.Archived,
            cancellationToken);
        var productsDemo = await _db.Products.CountAsync(
            p => p.SlugSeam != null && p.SlugSeam.StartsWith(CatalogDemoSeam.ProductSlugPrefix),
            cancellationToken);

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
            productsDemo,
            productsDraft,
            productsPublished,
            productsArchived,
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

    /// <summary>پس از seed، کل Catalog Product lifecycle را با شمارش دانه هم‌تراز می‌کند.</summary>
    public async Task EnsureProductLifecycleCleanAsync(int expectedProducts, CancellationToken cancellationToken)
    {
        var total = await _db.Products.CountAsync(cancellationToken);
        var published = await _db.Products.CountAsync(
            p => p.Status == CatalogPublicationStatus.Published,
            cancellationToken);
        var archived = await _db.Products.CountAsync(
            p => p.Status == CatalogPublicationStatus.Archived,
            cancellationToken);
        var draft = await _db.Products.CountAsync(
            p => p.Status == CatalogPublicationStatus.Draft,
            cancellationToken);

        if (total != expectedProducts)
        {
            throw new InvalidOperationException(
                $"Expected Product total={expectedProducts}, found {total}.");
        }

        if (published != 0)
        {
            throw new InvalidOperationException($"Expected Published=0, found {published}.");
        }

        if (archived != 0)
        {
            throw new InvalidOperationException($"Expected Archived=0, found {archived}.");
        }

        if (draft != expectedProducts)
        {
            throw new InvalidOperationException(
                $"Expected Draft={expectedProducts}, found {draft}.");
        }
    }
}
