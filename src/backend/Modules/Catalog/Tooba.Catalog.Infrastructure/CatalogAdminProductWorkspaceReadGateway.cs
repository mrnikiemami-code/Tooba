using Tooba.Catalog.Application.ProductHistory.Ports;
using Tooba.Catalog.Application.ProductPublishing.Ports;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// Catalog-owned Admin ProductWorkspace aggregate read gateway.
/// Exposes semantic projections only — never EF/domain entities.
/// </summary>
public sealed class CatalogAdminProductWorkspaceReadGateway(
    CatalogDbContext catalog,
    IProductPublishReadinessReader publishReadiness,
    IProductHistoryReader history) : ICatalogAdminProductWorkspaceReadGateway
{
    /// <inheritdoc />
    public async Task<CatalogAdminProductWorkspaceSnapshot?> GetAggregateSnapshotAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await catalog.Products.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var productNames = await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, [productId], cancellationToken);
        var variants = await catalog.Variants.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var variantIds = variants.Select(x => x.VariantId).ToList();
        var axes = await catalog.VariantAttributeValues.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .ToListAsync(cancellationToken);
        var defs = await catalog.AttributeDefinitions.AsNoTracking()
            .ToDictionaryAsync(x => x.DefinitionId, cancellationToken);
        var productAttrs = await catalog.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var media = await catalog.MediaReferences.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var categoryLinks = await catalog.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.Role)
            .ThenBy(x => x.AssignmentId)
            .ToListAsync(cancellationToken);
        var categoryNames = await LoadNamesAsync(
            CatalogLocalizedOwnerKind.Category,
            categoryLinks.Select(x => x.CategoryId).ToList(),
            cancellationToken);
        var brandName = product.BrandId is Guid brandId
            ? (await LoadNamesAsync(CatalogLocalizedOwnerKind.Brand, [brandId], cancellationToken))
                .GetValueOrDefault(brandId)
            : null;

        var attrViews = productAttrs.Select(a => new CatalogAdminProductAttribute(
            defs.TryGetValue(a.DefinitionId, out var def) ? def.Code : a.DefinitionId.ToString("N")[..8],
            a.CanonicalValue,
            false)).Concat(axes.Select(a => new CatalogAdminProductAttribute(
            defs.TryGetValue(a.DefinitionId, out var def) ? def.Code : "axis",
            a.CanonicalValue,
            true))).ToList();

        var mediaViews = media
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.DisplayOrder)
            .Select(m => new CatalogAdminProductMedia(m.MediaAssetId, m.IsPrimary, m.DisplayOrder, m.AltText))
            .ToList();

        var title = productNames.GetValueOrDefault(productId) ?? product.SlugSeam ?? "untitled";

        CatalogAdminPublishReadiness aggregateReadiness;
        var readinessResult = await publishReadiness.GetAsync(productId, "fa-IR", cancellationToken);
        if (readinessResult.IsFailure)
        {
            aggregateReadiness = new CatalogAdminPublishReadiness(
                false, false, false, false, false, false, false, [], ProductPublishRules.MessageNotReadyFa);
        }
        else
        {
            var readiness = readinessResult.Value;
            aggregateReadiness = new CatalogAdminPublishReadiness(
                readiness.IsReady,
                readiness.CategoryReady,
                readiness.TranslationReady,
                readiness.AttributeReady,
                readiness.VariantReady,
                readiness.MediaReady,
                readiness.SeoReady,
                readiness.MissingRequirements
                    .Select(m => new CatalogAdminPublishMissingRequirement(m.Code, m.MessageFa, m.WorkspaceTab))
                    .ToList(),
                readiness.MessageFa);
        }

        var catalogChecks = aggregateReadiness.MissingRequirements
            .Select(m => m.MessageFa)
            .ToList();
        if (catalogChecks.Count == 0 && !string.IsNullOrWhiteSpace(aggregateReadiness.MessageFa))
        {
            catalogChecks.Add(aggregateReadiness.MessageFa);
        }

        var primaryCategoryId = categoryLinks
            .Where(l => l.Role == CatalogProductCategoryRole.Primary)
            .Select(l => l.CategoryId)
            .FirstOrDefault();
        Guid? primaryCategory = primaryCategoryId == Guid.Empty ? null : primaryCategoryId;
        var categoryPath = primaryCategory is Guid pcid
            ? await BuildCategoryPathAsync(pcid, cancellationToken)
            : null;
        var isPrimaryCategoryAssignable = false;
        string? assignabilityWarning = null;
        if (primaryCategory is Guid assignableProbe)
        {
            var parentById = await catalog.Categories.AsNoTracking()
                .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
            isPrimaryCategoryAssignable =
                CatalogCategoryTreeRules.TryIsAssignableProductCategory(
                    assignableProbe, parentById, out var assignable)
                && assignable;

            if (!isPrimaryCategoryAssignable)
            {
                assignabilityWarning = CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa;
            }
        }

        var localizedRows = await catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product && x.OwnerId == productId)
            .ToListAsync(cancellationToken);
        var locales = localizedRows.Select(x => x.Locale).Append("fa-IR").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var translations = locales.Select(loc =>
        {
            string? Field(string key) => localizedRows
                .FirstOrDefault(r => r.Locale.Equals(loc, StringComparison.OrdinalIgnoreCase) && r.FieldKey == key)
                ?.Value;
            return new CatalogAdminProductTranslation(
                loc,
                Field("name") ?? (loc.Equals("fa-IR", StringComparison.OrdinalIgnoreCase) ? title : string.Empty),
                loc.Equals("fa-IR", StringComparison.OrdinalIgnoreCase) ? product.SlugSeam : null,
                Field("short_description"),
                Field("full_description"),
                Field("seo_title") ?? (loc.Equals("fa-IR", StringComparison.OrdinalIgnoreCase) ? product.SeoTitleSeam : null),
                Field("seo_description"));
        }).Where(t =>
            !string.IsNullOrWhiteSpace(t.Name)
            || !string.IsNullOrWhiteSpace(t.Slug)
            || !string.IsNullOrWhiteSpace(t.ShortDescription)
            || !string.IsNullOrWhiteSpace(t.Description)
            || !string.IsNullOrWhiteSpace(t.SeoTitle)
            || !string.IsNullOrWhiteSpace(t.SeoDescription)).ToList();

        var shortDescription = localizedRows
            .FirstOrDefault(r => r.FieldKey == "short_description" && r.Locale.Equals("fa-IR", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        var categoryAssignments = new List<CatalogAdminCategoryAssignment>();
        foreach (var link in categoryLinks)
        {
            var path = await BuildCategoryPathAsync(link.CategoryId, cancellationToken);
            categoryAssignments.Add(new CatalogAdminCategoryAssignment(
                link.CategoryId,
                path,
                link.Role == CatalogProductCategoryRole.Primary ? "Primary" : "Additional"));
        }

        var variantViews = variants.Select(v => new CatalogAdminProductVariant(
            v.VariantId,
            v.CombinationFingerprint,
            v.Status.ToString(),
            v.CatalogCodeSeam)).ToList();

        return new CatalogAdminProductWorkspaceSnapshot(
            product.ProductId,
            title,
            product.Status.ToString(),
            product.Kind.ToString(),
            brandName,
            product.BrandId,
            product.SlugSeam,
            product.SeoTitleSeam,
            shortDescription,
            product.UpdatedAt,
            product.UnitOfMeasureId,
            product.QuantityDecimalPlaces,
            product.QuantityStep,
            await ResolveUnitCodeAsync(product.UnitOfMeasureId, cancellationToken),
            await ResolveUnitDisplayAsync(product.UnitOfMeasureId, cancellationToken),
            await ListUnitOptionsAsync(product.UnitOfMeasureId, cancellationToken),
            categoryLinks
                .OrderBy(l => l.Role)
                .Select(l => categoryNames.GetValueOrDefault(l.CategoryId) ?? "رده")
                .ToList(),
            primaryCategory,
            categoryPath,
            isPrimaryCategoryAssignable,
            assignabilityWarning,
            categoryAssignments,
            attrViews,
            variantViews,
            mediaViews,
            translations,
            aggregateReadiness,
            catalogChecks,
            await BuildHistoryShellListsAsync(productId, cancellationToken),
            await BuildHistoryShellListsAsync(productId, cancellationToken, auditOnly: true));
    }

    private async Task<IReadOnlyList<CatalogAdminHistoryItem>> BuildHistoryShellListsAsync(
        Guid productId,
        CancellationToken cancellationToken,
        bool auditOnly = false)
    {
        var pageResult = await history.ListAsync(productId, section: null, skip: 0, take: 20, cancellationToken);
        if (pageResult.IsFailure)
        {
            return [];
        }

        var rows = auditOnly
            ? pageResult.Value.Items.Where(x => x.Section is "lifecycle" or "seo" or "category").ToList()
            : pageResult.Value.Items.ToList();
        return rows.Select(x => new CatalogAdminHistoryItem(
            auditOnly ? "audit" : "activity",
            x.SummaryFa,
            x.OccurredAt,
            x.ActorDisplayName,
            x.SectionLabelFa,
            x.BeforeSummary,
            x.AfterSummary,
            x.HistoryId)).ToList();
    }

    private async Task<string> BuildCategoryPathAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var map = await BuildCategoryPathMapAsync([categoryId], cancellationToken);
        return map.GetValueOrDefault(categoryId) ?? "رده";
    }

    private async Task<IReadOnlyDictionary<Guid, string>> BuildCategoryPathMapAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var parentById = await catalog.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.CategoryId, c => c.ParentCategoryId, cancellationToken);

        var neededNames = new HashSet<Guid>();
        foreach (var id in categoryIds)
        {
            var current = id;
            var guard = 0;
            while (parentById.ContainsKey(current) && neededNames.Add(current))
            {
                if (parentById[current] is not Guid parent)
                {
                    break;
                }

                current = parent;
                if (++guard > parentById.Count + 2)
                {
                    break;
                }
            }
        }

        var names = await LoadNamesAsync(
            CatalogLocalizedOwnerKind.Category,
            neededNames.ToList(),
            cancellationToken);

        var paths = new Dictionary<Guid, string>(categoryIds.Count);
        foreach (var id in categoryIds.Distinct())
        {
            if (!parentById.ContainsKey(id))
            {
                paths[id] = names.GetValueOrDefault(id) ?? "رده";
                continue;
            }

            var chain = new List<Guid>();
            var current = id;
            var seen = new HashSet<Guid>();
            while (parentById.ContainsKey(current) && seen.Add(current))
            {
                chain.Add(current);
                if (parentById[current] is not Guid parent)
                {
                    break;
                }

                current = parent;
            }

            chain.Reverse();
            paths[id] = string.Join(" > ", chain.Select(cid => names.GetValueOrDefault(cid) ?? "رده"));
        }

        return paths;
    }

    private async Task<Dictionary<Guid, string>> LoadNamesAsync(
        CatalogLocalizedOwnerKind kind,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == kind && ids.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.OwnerId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
    }

    private async Task<string?> ResolveUnitCodeAsync(Guid unitId, CancellationToken cancellationToken) =>
        (await catalog.UnitsOfMeasure.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken))?.Code;

    private async Task<string?> ResolveUnitDisplayAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var translations = await catalog.UnitOfMeasureTranslations.AsNoTracking()
            .Where(x => x.UnitOfMeasureId == unitId)
            .ToListAsync(cancellationToken);
        return translations.FirstOrDefault()?.Name
            ?? await ResolveUnitCodeAsync(unitId, cancellationToken);
    }

    private async Task<IReadOnlyList<CatalogAdminUnitOption>> ListUnitOptionsAsync(
        Guid? currentUnitId,
        CancellationToken cancellationToken)
    {
        var units = await catalog.UnitsOfMeasure.AsNoTracking()
            .Where(x => x.IsActive || (currentUnitId.HasValue && x.UnitOfMeasureId == currentUnitId))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            return [];
        }

        var ids = units.Select(x => x.UnitOfMeasureId).ToArray();
        var translations = await catalog.UnitOfMeasureTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.UnitOfMeasureId))
            .ToListAsync(cancellationToken);
        var byUnit = translations.GroupBy(x => x.UnitOfMeasureId).ToDictionary(g => g.Key, g => g.ToList());
        return units.Select(u =>
        {
            byUnit.TryGetValue(u.UnitOfMeasureId, out var rows);
            var picked = rows?.FirstOrDefault();
            return new CatalogAdminUnitOption(
                u.UnitOfMeasureId,
                u.Code,
                picked?.Name ?? u.Code,
                picked?.ShortName ?? u.Code);
        }).ToList();
    }
}
