using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.CategoryChanges.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Catalog persistence for Admin category-change preview and primary-category replace.</summary>
public sealed class CategoryChangeDirectory : ICategoryChangeDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly ICatalogActorContext? _actor;

    /// <summary>Creates the directory.</summary>
    public CategoryChangeDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        ICatalogActorContext? actor = null)
    {
        _db = db;
        _guard = guard;
        _actor = actor;
    }

    /// <inheritdoc />
    public async Task<Result<CategoryChangeImpact>> PreviewAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<CategoryChangeImpact>(new SemanticError(CatalogErrorCodes.ProductMissing));
        }

        if (!await _db.Categories.AnyAsync(x => x.CategoryId == newCategoryId, cancellationToken))
        {
            return Result.Failure<CategoryChangeImpact>(new SemanticError(CatalogErrorCodes.CategoryMissing));
        }

        var assignable = await TryEnsureAssignableProductCategoryAsync(newCategoryId, cancellationToken);
        if (assignable.IsFailure)
        {
            return Result.Failure<CategoryChangeImpact>(assignable.Errors);
        }

        var schema = await ResolveEffectiveBindingsAsync(newCategoryId, cancellationToken);
        var values = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var axes = await _db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var report = CatalogCategorySchemaResolver.PreviewCategoryChange(values, axes, schema);
        return Result.Success(new CategoryChangeImpact(
            productId,
            newCategoryId,
            report.OrphanAttributeValues.Select(x => new OrphanProductAttributeValue(x.DefinitionId, x.CanonicalValue)).ToList(),
            report.InvalidVariantAxisDefinitionIds));
    }

    /// <inheritdoc />
    public async Task<Result<CategoryChangeImpactReport>> PreviewReportAsync(
        Guid productId,
        Guid newCategoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var impactResult = await PreviewAsync(productId, newCategoryId, cancellationToken);
        if (impactResult.IsFailure)
        {
            return Result.Failure<CategoryChangeImpactReport>(impactResult.Errors);
        }

        var impact = impactResult.Value;
        var newSchema = await ResolveEffectiveBindingsAsync(newCategoryId, cancellationToken);
        var values = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var allowed = newSchema.Select(x => x.DefinitionId).ToHashSet();
        var compatibleValues = values.Where(v => allowed.Contains(v.DefinitionId)).ToList();
        var compatiblePreserved = compatibleValues.Count;

        var presentIds = values.Select(v => v.DefinitionId).ToHashSet();
        var newlyRequired = newSchema
            .Where(x => x.IsRequired && !x.IsVariantAxis && x.Definition.IsActive && !presentIds.Contains(x.DefinitionId))
            .ToList();

        var orphanDefIds = impact.OrphanAttributeValues.Select(x => x.DefinitionId).Distinct().ToArray();
        var preservedDefIds = compatibleValues.Select(v => v.DefinitionId).Distinct().ToArray();
        var labelIds = orphanDefIds
            .Concat(newlyRequired.Select(x => x.DefinitionId))
            .Concat(preservedDefIds)
            .Distinct()
            .ToArray();
        var names = await GetAttributeDefinitionNamesAsync(labelIds, normalizedLocale, cancellationToken);

        var orphanOptionIds = new List<Guid>();
        foreach (var orphan in impact.OrphanAttributeValues)
        {
            foreach (var part in orphan.CanonicalValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var oid))
                {
                    orphanOptionIds.Add(oid);
                }
            }
        }

        var optionNames = await GetAttributeOptionNamesAsync(orphanOptionIds, normalizedLocale, cancellationToken);
        var definitions = orphanDefIds.Length == 0
            ? new Dictionary<Guid, CatalogAttributeDefinition>()
            : await _db.AttributeDefinitions.AsNoTracking()
                .Where(x => orphanDefIds.Contains(x.DefinitionId))
                .ToDictionaryAsync(x => x.DefinitionId, cancellationToken);

        var orphanSummaries = impact.OrphanAttributeValues.Select(o =>
        {
            definitions.TryGetValue(o.DefinitionId, out var def);
            var display = FormatOrphanDisplay(def, o.CanonicalValue, optionNames);
            return new CategoryChangeOrphanSummary(
                o.DefinitionId,
                names.GetValueOrDefault(o.DefinitionId) ?? def?.Code ?? o.DefinitionId.ToString("N"),
                display);
        }).ToList();

        var newlyRequiredLabels = newlyRequired
            .Select(x => names.GetValueOrDefault(x.DefinitionId) ?? x.Definition.Code)
            .ToList();
        var preservedAttributes = preservedDefIds
            .Select(id => names.GetValueOrDefault(id) ?? id.ToString("N"))
            .ToList();
        var removedAttributes = orphanSummaries
            .Select(o => string.IsNullOrWhiteSpace(o.DisplayValue)
                ? o.LocalizedName
                : $"{o.LocalizedName}: {o.DisplayValue}")
            .ToList();

        var activeVariantCount = await _db.Variants.AsNoTracking()
            .CountAsync(
                x => x.ProductId == productId && x.Status != CatalogPublicationStatus.Archived,
                cancellationToken);
        var newAxisIds = newSchema.Where(x => x.IsVariantAxis).Select(x => x.DefinitionId).ToHashSet();
        var currentAxes = await _db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.DefinitionId)
            .ToListAsync(cancellationToken);
        var axesChanged = impact.InvalidVariantAxisDefinitionIds.Count > 0
            || currentAxes.Any(id => !newAxisIds.Contains(id))
            || (currentAxes.Count > 0 && !currentAxes.ToHashSet().SetEquals(newAxisIds));
        var variantCompatible = !axesChanged;
        var variantImpactCount = axesChanged ? activeVariantCount : 0;
        var preservedVariantCount = variantCompatible ? activeVariantCount : 0;
        var affectedVariantCount = variantCompatible ? 0 : activeVariantCount;
        var variantImpactFa = variantImpactCount > 0
            ? $"{ToPersianDigits(variantImpactCount)} تنوع تحت تأثیر تغییر محورها قرار می‌گیرد و حذف خودکار نمی‌شود"
            : null;

        var currentCategoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        string? currentCategoryPath = null;
        if (currentCategoryId is Guid currentId)
        {
            currentCategoryPath = await BuildCategoryPathAsync(currentId, normalizedLocale, cancellationToken);
        }

        var targetCategoryPath = await BuildCategoryPathAsync(newCategoryId, normalizedLocale, cancellationToken);

        var memberships = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var additionalMembershipPromoted = memberships.Any(
            x => x.CategoryId == newCategoryId && x.Role == CatalogProductCategoryRole.Additional);
        var otherDisplayRemainCount = memberships.Count(
            x => x.Role == CatalogProductCategoryRole.Additional && x.CategoryId != newCategoryId);

        var readinessBlockers = new List<string>();
        foreach (var label in newlyRequiredLabels)
        {
            readinessBlockers.Add($"ویژگی الزامی «{label}» باید تکمیل شود");
        }

        if (!variantCompatible && affectedVariantCount > 0)
        {
            readinessBlockers.Add(
                $"{ToPersianDigits(affectedVariantCount)} تنوع نیاز به بازبینی دارند و آمادگی انتشار برقرار نیست");
        }

        var productStatus = await _db.Products.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);
        var structuralIncompatibility = impact.OrphanAttributeValues.Count > 0
            || newlyRequired.Count > 0
            || !variantCompatible;
        if (productStatus == CatalogPublicationStatus.Published && structuralIncompatibility)
        {
            readinessBlockers.Add("محصول پس از مهاجرت به‌خاطر ناسازگاری ساختاری از انتشار خارج می‌شود");
        }

        var messageParts = new List<string>
        {
            $"{ToPersianDigits(compatiblePreserved)} مقدار حفظ می‌شود",
            $"{ToPersianDigits(impact.OrphanAttributeValues.Count)} ویژگی دیگر در دسته جدید وجود ندارد",
            $"{ToPersianDigits(newlyRequired.Count)} ویژگی الزامی جدید باید تکمیل شود",
        };
        if (variantImpactFa is not null)
        {
            messageParts.Add(variantImpactFa);
        }

        if (additionalMembershipPromoted)
        {
            messageParts.Add("دسته هدف از «نمایش در این دسته» به دسته اصلی ارتقا می‌یابد");
        }

        var messageFa = string.Join("\n", messageParts);

        return Result.Success(new CategoryChangeImpactReport(
            productId,
            newCategoryId,
            compatiblePreserved,
            impact.OrphanAttributeValues.Count,
            newlyRequired.Count,
            orphanSummaries,
            newlyRequiredLabels,
            impact.InvalidVariantAxisDefinitionIds,
            messageFa,
            variantImpactCount,
            variantImpactFa,
            currentCategoryId,
            currentCategoryPath,
            targetCategoryPath,
            preservedAttributes,
            newlyRequiredLabels,
            removedAttributes,
            newlyRequiredLabels,
            variantCompatible,
            preservedVariantCount,
            affectedVariantCount,
            additionalMembershipPromoted,
            otherDisplayRemainCount,
            readinessBlockers));
    }

    /// <inheritdoc />
    public async Task<Result<CategoryChangeImpact>> ReplacePrimaryAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var impactResult = await PreviewAsync(productId, newCategoryId, cancellationToken);
        if (impactResult.IsFailure)
        {
            return impactResult;
        }

        var previewResult = await PreviewReportAsync(productId, newCategoryId, "fa-IR", cancellationToken);
        if (previewResult.IsFailure)
        {
            return Result.Failure<CategoryChangeImpact>(previewResult.Errors);
        }

        var impact = impactResult.Value;
        var preview = previewResult.Value;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await _db.ProductCategories.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);

            var existingAsAdditional = existing.FirstOrDefault(
                x => x.CategoryId == newCategoryId && x.Role == CatalogProductCategoryRole.Additional);
            if (existingAsAdditional is not null)
            {
                _db.ProductCategories.Remove(existingAsAdditional);
                existing.Remove(existingAsAdditional);
            }

            var currentPrimary = existing.FirstOrDefault(x => x.Role == CatalogProductCategoryRole.Primary);
            if (currentPrimary is not null)
            {
                if (currentPrimary.CategoryId == newCategoryId)
                {
                    await tx.CommitAsync(cancellationToken);
                    return Result.Success(impact);
                }

                _db.ProductCategories.Remove(currentPrimary);
            }

            _db.ProductCategories.Add(
                CatalogProductCategory.Assign(productId, newCategoryId, CatalogProductCategoryRole.Primary));

            var orphanDefIds = impact.OrphanAttributeValues.Select(x => x.DefinitionId).ToHashSet();
            if (orphanDefIds.Count > 0)
            {
                var orphanRows = await _db.ProductAttributeValues
                    .Where(x => x.ProductId == productId && orphanDefIds.Contains(x.DefinitionId))
                    .ToListAsync(cancellationToken);
                _db.ProductAttributeValues.RemoveRange(orphanRows);
            }

            var now = DateTimeOffset.UtcNow;
            var newSchema = await ResolveEffectiveBindingsAsync(newCategoryId, cancellationToken);
            var newAxisIds = newSchema.Where(x => x.IsVariantAxis).Select(x => x.DefinitionId).ToHashSet();
            var currentAxes = await _db.ProductVariantAxes
                .Where(x => x.ProductId == productId)
                .ToListAsync(cancellationToken);
            var axesChanged = !preview.VariantCompatible;
            var invalidAxes = currentAxes.Where(a => !newAxisIds.Contains(a.DefinitionId)).ToList();
            if (invalidAxes.Count > 0)
            {
                _db.ProductVariantAxes.RemoveRange(invalidAxes);
            }

            if (axesChanged)
            {
                var variants = await _db.Variants
                    .Where(x => x.ProductId == productId && x.Status != CatalogPublicationStatus.Archived)
                    .ToListAsync(cancellationToken);
                ClearDefaultFlags(variants, now);
                foreach (var variant in variants)
                {
                    if (variant.Status != CatalogPublicationStatus.Draft)
                    {
                        variant.SetStatus(CatalogPublicationStatus.Draft, now);
                    }
                    else
                    {
                        variant.UpdatedAt = now;
                    }
                }
            }

            var structuralIncompatibility = impact.OrphanAttributeValues.Count > 0
                || preview.NewlyRequiredMissingCount > 0
                || axesChanged;
            var unpublishedForSafety = false;
            var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
            if (product.Status == CatalogPublicationStatus.Published && structuralIncompatibility)
            {
                product.Unpublish(now);
                unpublishedForSafety = true;
                QueueProductHistory(
                    productId,
                    ProductHistoryRules.EventUnpublished,
                    ProductHistoryRules.SectionLifecycle,
                    ProductHistoryRules.SummaryUnpublishedByMigrationFa,
                    ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Published),
                    ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft));
            }

            var beforePath = preview.CurrentCategoryPath ?? "بدون دسته اصلی";
            var afterSummary = ProductHistoryRules.FormatCategoryMigrationAfterSummaryFa(
                preview.TargetCategoryPath ?? "دسته جدید",
                preview.CompatiblePreservedCount,
                preview.NewlyRequiredMissingCount,
                preview.OrphanCount,
                preview.AffectedVariantCount,
                unpublishedForSafety);
            QueueProductHistory(
                productId,
                ProductHistoryRules.EventCategoryChanged,
                ProductHistoryRules.SectionCategory,
                ProductHistoryRules.SummaryCategoryMigrationFa,
                beforePath,
                afterSummary);

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Result.Success(impact);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Result> TryEnsureAssignableProductCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        if (!CatalogCategoryTreeRules.IsAssignableProductCategory(categoryId, parentById))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryAssignmentLevelInvalid));
        }

        return Result.Success();
    }

    private void QueueProductHistory(
        Guid productId,
        string eventType,
        string section,
        string summaryFa,
        string? beforeSummary,
        string? afterSummary)
    {
        _db.ProductHistoryEntries.Add(CatalogProductHistoryEntry.Create(
            productId,
            eventType,
            section,
            summaryFa,
            DateTimeOffset.UtcNow,
            _actor?.ActorUserId,
            _actor?.ActorDisplayName,
            beforeSummary,
            afterSummary));
    }

    private static void ClearDefaultFlags(IEnumerable<CatalogVariant> variants, DateTimeOffset now)
    {
        foreach (var variant in variants.Where(x => x.IsDefault))
        {
            variant.SetDefault(false, now);
        }
    }

    private async Task<Guid?> ResolvePrimaryCategoryIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var categoryId = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId && x.Role == CatalogProductCategoryRole.Primary)
            .Select(x => x.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);
        return categoryId == Guid.Empty ? null : categoryId;
    }

    private async Task<string> BuildCategoryPathAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var byId = categories.ToDictionary(x => x.CategoryId);
        var chain = new List<Guid>();
        var current = categoryId;
        var seen = new HashSet<Guid>();
        while (byId.TryGetValue(current, out var node) && seen.Add(current))
        {
            chain.Add(current);
            if (node.ParentCategoryId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        chain.Reverse();
        var names = await GetCategoryNamesAsync(chain, locale, cancellationToken);
        return string.Join(" > ", chain.Select(id => names.GetValueOrDefault(id) ?? "رده"));
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = categoryIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Category
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetAttributeOptionNamesAsync(
        IReadOnlyCollection<Guid> optionIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (optionIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = optionIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.AttributeOption
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
        var options = await _db.AttributeOptions.AsNoTracking()
            .Where(x => ids.Contains(x.OptionId))
            .Select(x => new { x.OptionId, x.Code })
            .ToListAsync(cancellationToken);
        foreach (var opt in options)
        {
            names.TryAdd(opt.OptionId, opt.Code);
        }

        return names;
    }

    private static string FormatOrphanDisplay(
        CatalogAttributeDefinition? definition,
        string canonical,
        IReadOnlyDictionary<Guid, string> optionNames)
    {
        if (definition?.ValueKind == CatalogAttributeValueKind.Enumeration)
        {
            var parts = canonical.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join("، ", parts.Select(p =>
                Guid.TryParse(p, out var oid) ? optionNames.GetValueOrDefault(oid) ?? p : p));
        }

        if (definition?.ValueKind == CatalogAttributeValueKind.Boolean
            && bool.TryParse(canonical, out var b))
        {
            return b ? "بله" : "خیر";
        }

        return canonical;
    }

    private static string ToPersianDigits(int value)
    {
        var s = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.Create(s.Length, s, static (span, src) =>
        {
            for (var i = 0; i < src.Length; i++)
            {
                var c = src[i];
                span[i] = c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c;
            }
        });
    }

    private async Task<IReadOnlyList<CatalogEffectiveSchemaBinding>> ResolveEffectiveBindingsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(x => x.CategoryId);
        var bindings = await _db.CategoryAttributeBindings.AsNoTracking().ToListAsync(cancellationToken);
        var definitions = await _db.AttributeDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        var definitionsById = definitions.ToDictionary(x => x.DefinitionId);
        return CatalogCategorySchemaResolver.ResolveEffectiveSchema(categoryId, categoriesById, bindings, definitionsById);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetAttributeDefinitionNamesAsync(
        IReadOnlyCollection<Guid> definitionIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (definitionIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = definitionIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.AttributeDefinition
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
        var definitions = await _db.AttributeDefinitions.AsNoTracking()
            .Where(x => ids.Contains(x.DefinitionId))
            .Select(x => new { x.DefinitionId, x.Code })
            .ToListAsync(cancellationToken);
        foreach (var def in definitions)
        {
            names.TryAdd(def.DefinitionId, def.Code);
        }

        return names;
    }
}
