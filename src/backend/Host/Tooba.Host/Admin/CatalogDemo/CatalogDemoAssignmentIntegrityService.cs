using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>گزارش ممیزی انتساب محصول↔دسته (سطح و یکتایی Primary).</summary>
public sealed record CatalogAssignmentIntegrityAudit(
    int TotalProducts,
    int PrimaryAtL1OrL2,
    int DisplayAtL1OrL2,
    int DuplicatePrimaryAndAdditional,
    int MultiplePrimary,
    int MissingPrimary,
    int OrphanAssignments,
    IReadOnlyList<Guid> PrimaryAtL1OrL2ProductIds,
    IReadOnlyList<Guid> DisplayAtL1OrL2ProductIds,
    IReadOnlyList<Guid> DuplicateProductIds,
    IReadOnlyList<Guid> MultiplePrimaryProductIds,
    IReadOnlyList<Guid> MissingPrimaryProductIds,
    IReadOnlyList<Guid> OrphanAssignmentProductIds);

/// <summary>نتیجهٔ پاکسازی غیر Production.</summary>
public sealed record CatalogAssignmentIntegrityCleanupResult(
    CatalogAssignmentIntegrityAudit Before,
    CatalogAssignmentIntegrityAudit After,
    int InvalidDisplayRemoved,
    int DuplicateAdditionalRemoved,
    int PrimariesRepairedToL3,
    int ProductsDeleted,
    IReadOnlyList<Guid> DeletedProductIds);

/// <summary>
/// ممیزی و پاکسازی انتساب نامعتبر L1/L2 و تکراری‌ها (TB-P07-T037) — فقط دادهٔ غیر Production.
/// </summary>
public sealed class CatalogDemoAssignmentIntegrityService
{
    private readonly CatalogDbContext _db;
    private readonly CatalogDemoResetService _reset;
    private readonly ILogger<CatalogDemoAssignmentIntegrityService> _logger;

    /// <summary>وابستگی‌ها را تزریق می‌کند.</summary>
    public CatalogDemoAssignmentIntegrityService(
        CatalogDbContext db,
        CatalogDemoResetService reset,
        ILogger<CatalogDemoAssignmentIntegrityService> logger)
    {
        _db = db;
        _reset = reset;
        _logger = logger;
    }

    /// <summary>ممیزی کامل انتساب‌ها نسبت به سطح درخت.</summary>
    public async Task<CatalogAssignmentIntegrityAudit> AuditAsync(CancellationToken cancellationToken)
    {
        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.CategoryId, c => c.ParentCategoryId, cancellationToken);
        var categoryIds = parentById.Keys.ToHashSet();

        var products = await _db.Products.AsNoTracking()
            .Select(p => p.ProductId)
            .ToListAsync(cancellationToken);
        var links = await _db.ProductCategories.AsNoTracking().ToListAsync(cancellationToken);
        var byProduct = links.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var primaryBad = new List<Guid>();
        var displayBad = new List<Guid>();
        var duplicates = new List<Guid>();
        var multiPrimary = new List<Guid>();
        var missingPrimary = new List<Guid>();
        var orphanProducts = new HashSet<Guid>();
        var displayBadCount = 0;

        foreach (var productId in products)
        {
            if (!byProduct.TryGetValue(productId, out var productLinks))
            {
                missingPrimary.Add(productId);
                continue;
            }

            foreach (var link in productLinks)
            {
                if (!categoryIds.Contains(link.CategoryId))
                {
                    orphanProducts.Add(productId);
                }
            }

            var primaries = productLinks.Where(x => x.Role == CatalogProductCategoryRole.Primary).ToList();
            if (primaries.Count == 0)
            {
                missingPrimary.Add(productId);
            }
            else if (primaries.Count > 1)
            {
                multiPrimary.Add(productId);
            }

            var primaryId = primaries.FirstOrDefault()?.CategoryId;
            if (primaryId is Guid pid)
            {
                if (categoryIds.Contains(pid)
                    && !CatalogCategoryTreeRules.IsAssignableProductCategory(pid, parentById))
                {
                    primaryBad.Add(productId);
                }
            }

            foreach (var add in productLinks.Where(x => x.Role == CatalogProductCategoryRole.Additional))
            {
                if (primaryId is Guid p && add.CategoryId == p)
                {
                    duplicates.Add(productId);
                }

                if (!categoryIds.Contains(add.CategoryId))
                {
                    continue;
                }

                if (!CatalogCategoryTreeRules.IsAssignableProductCategory(add.CategoryId, parentById))
                {
                    displayBadCount++;
                    if (!displayBad.Contains(productId))
                    {
                        displayBad.Add(productId);
                    }
                }
            }
        }

        // Orphan rows whose product was already deleted should still count via links without product.
        foreach (var link in links)
        {
            if (!products.Contains(link.ProductId) || !categoryIds.Contains(link.CategoryId))
            {
                orphanProducts.Add(link.ProductId);
            }
        }

        return new CatalogAssignmentIntegrityAudit(
            products.Count,
            primaryBad.Count,
            displayBadCount,
            duplicates.Distinct().Count(),
            multiPrimary.Count,
            missingPrimary.Count,
            orphanProducts.Count,
            primaryBad,
            displayBad,
            duplicates.Distinct().ToList(),
            multiPrimary,
            missingPrimary,
            orphanProducts.ToList());
    }

    /// <summary>
    /// پاکسازی deterministic: حذف display نامعتبر، حذف duplicate Additional==Primary،
    /// تعمیر Primary فقط وقتی دقیقاً یک Additional سطح ۳ موجود است؛ در غیر این صورت حذف محصول.
    /// </summary>
    public async Task<CatalogAssignmentIntegrityCleanupResult> CleanupAsync(CancellationToken cancellationToken)
    {
        var before = await AuditAsync(cancellationToken);
        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.CategoryId, c => c.ParentCategoryId, cancellationToken);
        var categoryIds = parentById.Keys.ToHashSet();

        var links = await _db.ProductCategories.ToListAsync(cancellationToken);
        var invalidDisplayRemoved = 0;
        var duplicateAdditionalRemoved = 0;
        var primariesRepaired = 0;
        var toDelete = new HashSet<Guid>();

        // 1) Invalid / duplicate Additional rows
        foreach (var link in links.Where(x => x.Role == CatalogProductCategoryRole.Additional).ToList())
        {
            var primary = links.FirstOrDefault(x =>
                x.ProductId == link.ProductId && x.Role == CatalogProductCategoryRole.Primary);
            if (primary is not null && primary.CategoryId == link.CategoryId)
            {
                _db.ProductCategories.Remove(link);
                links.Remove(link);
                duplicateAdditionalRemoved++;
                continue;
            }

            if (!categoryIds.Contains(link.CategoryId)
                || !CatalogCategoryTreeRules.IsAssignableProductCategory(link.CategoryId, parentById))
            {
                _db.ProductCategories.Remove(link);
                links.Remove(link);
                invalidDisplayRemoved++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Refresh tracked links after removals
        links = await _db.ProductCategories.ToListAsync(cancellationToken);
        var byProduct = links.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.ToList());
        var allProductIds = await _db.Products.Select(p => p.ProductId).ToListAsync(cancellationToken);

        foreach (var productId in allProductIds)
        {
            byProduct.TryGetValue(productId, out var productLinks);
            productLinks ??= [];

            var primaries = productLinks.Where(x => x.Role == CatalogProductCategoryRole.Primary).ToList();
            var additionals = productLinks.Where(x => x.Role == CatalogProductCategoryRole.Additional).ToList();

            if (primaries.Count > 1)
            {
                // Ambiguous — do not guess; delete demo product.
                toDelete.Add(productId);
                continue;
            }

            if (primaries.Count == 0)
            {
                var validAdds = additionals
                    .Where(a => categoryIds.Contains(a.CategoryId)
                        && CatalogCategoryTreeRules.IsAssignableProductCategory(a.CategoryId, parentById))
                    .ToList();
                if (validAdds.Count == 1)
                {
                    var promote = validAdds[0];
                    _db.ProductCategories.Remove(promote);
                    _db.ProductCategories.Add(
                        CatalogProductCategory.Assign(productId, promote.CategoryId, CatalogProductCategoryRole.Primary));
                    primariesRepaired++;
                    continue;
                }

                toDelete.Add(productId);
                continue;
            }

            var primary = primaries[0];
            var primaryOk = categoryIds.Contains(primary.CategoryId)
                && CatalogCategoryTreeRules.IsAssignableProductCategory(primary.CategoryId, parentById);
            if (primaryOk)
            {
                continue;
            }

            var candidateAdds = additionals
                .Where(a => categoryIds.Contains(a.CategoryId)
                    && CatalogCategoryTreeRules.IsAssignableProductCategory(a.CategoryId, parentById))
                .ToList();
            if (candidateAdds.Count == 1)
            {
                var target = candidateAdds[0].CategoryId;
                _db.ProductCategories.Remove(primary);
                _db.ProductCategories.Remove(candidateAdds[0]);
                _db.ProductCategories.Add(
                    CatalogProductCategory.Assign(productId, target, CatalogProductCategoryRole.Primary));
                primariesRepaired++;
                continue;
            }

            toDelete.Add(productId);
        }

        // Orphan assignment rows (category missing)
        var orphans = await _db.ProductCategories
            .Where(pc => !_db.Categories.Any(c => c.CategoryId == pc.CategoryId))
            .ToListAsync(cancellationToken);
        if (orphans.Count > 0)
        {
            foreach (var orphan in orphans)
            {
                toDelete.Add(orphan.ProductId);
            }

            _db.ProductCategories.RemoveRange(orphans);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var deleted = 0;
        var deletedIds = toDelete.ToList();
        if (deletedIds.Count > 0)
        {
            deleted = await _reset.DeleteProductsByIdsAsync(deletedIds, cancellationToken);
            _logger.LogWarning(
                "Assignment integrity deleted {Count} products with non-deterministic/invalid Primary (TB-P07-T037).",
                deleted);
        }

        var after = await AuditAsync(cancellationToken);
        return new CatalogAssignmentIntegrityCleanupResult(
            before,
            after,
            invalidDisplayRemoved,
            duplicateAdditionalRemoved,
            primariesRepaired,
            deleted,
            deletedIds);
    }

    /// <summary>پس از seed باید همهٔ شمارنده‌های نامعتبر صفر باشند؛ در غیر این صورت throw.</summary>
    public async Task EnsureCleanOrThrowAsync(CancellationToken cancellationToken)
    {
        var audit = await AuditAsync(cancellationToken);
        if (audit.PrimaryAtL1OrL2 != 0
            || audit.DisplayAtL1OrL2 != 0
            || audit.DuplicatePrimaryAndAdditional != 0
            || audit.MultiplePrimary != 0
            || audit.MissingPrimary != 0
            || audit.OrphanAssignments != 0)
        {
            throw new InvalidOperationException(
                "Catalog assignment integrity failed after seed: "
                + $"primaryL1L2={audit.PrimaryAtL1OrL2}, displayL1L2={audit.DisplayAtL1OrL2}, "
                + $"dupPrimaryAdditional={audit.DuplicatePrimaryAndAdditional}, multiPrimary={audit.MultiplePrimary}, "
                + $"missingPrimary={audit.MissingPrimary}, orphans={audit.OrphanAssignments}.");
        }
    }
}
