using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.MegaMenu.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for MegaMenu Admin + Storefront operations.</summary>
public sealed class MegaMenuDirectory : IMegaMenuDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;

    /// <summary>Creates the directory.</summary>
    public MegaMenuDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<Result<CategoryMegaMenuConfigurationView>> GetCategoryConfigurationAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<CategoryMegaMenuConfigurationView>(
                new SemanticError(CatalogErrorCodes.MegaMenuCategoryMissing));
        }

        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var translation = await _db.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId && x.Locale == normalizedLocale, cancellationToken);

        var item = await _db.MegaMenuItems.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);

        if (item is null)
        {
            var defaultTitle = translation?.Name
                ?? await ResolveCategoryDisplayNameAsync(categoryId, normalizedLocale, cancellationToken);
            var previewSlug = translation?.Slug ?? string.Empty;
            return Result.Success(new CategoryMegaMenuConfigurationView(
                categoryId,
                false,
                null,
                null,
                null,
                0,
                false,
                false,
                null,
                null,
                defaultTitle,
                null,
                null,
                null,
                BuildUiCategoryRoute(normalizedLocale, previewSlug),
                0,
                category.Status == CatalogPublicationStatus.Published,
                category.IsVisible));
        }

        var overrideRow = await _db.MegaMenuItemTranslations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.MegaMenuItemId == item.MegaMenuItemId && x.Locale == normalizedLocale,
                cancellationToken);

        var allItems = await _db.MegaMenuItems.AsNoTracking().ToListAsync(cancellationToken);
        var itemsById = allItems.ToDictionary(x => x.MegaMenuItemId);
        var level = ComputePresentationLevel(item.MegaMenuItemId, itemsById);
        var parentPath = item.ParentMegaMenuItemId is Guid parentId
            ? await BuildMenuPathAsync(parentId, normalizedLocale, itemsById, cancellationToken)
            : null;

        var displayTitle = overrideRow?.TitleOverride ?? translation?.Name
            ?? await ResolveCategoryDisplayNameAsync(categoryId, normalizedLocale, cancellationToken);
        var slug = translation?.Slug ?? string.Empty;

        return Result.Success(new CategoryMegaMenuConfigurationView(
            categoryId,
            true,
            item.MegaMenuItemId,
            item.ParentMegaMenuItemId,
            parentPath,
            item.SortOrder,
            item.IsVisible,
            item.IsFeatured,
            item.ImageMediaAssetId,
            item.IconMediaAssetId,
            displayTitle,
            overrideRow?.TitleOverride,
            overrideRow?.BadgeText,
            overrideRow?.ShortLabel,
            BuildUiCategoryRoute(normalizedLocale, slug),
            level,
            category.Status == CatalogPublicationStatus.Published,
            category.IsVisible));
    }

    /// <inheritdoc />
    public async Task<Result> UpsertBindingAsync(
        Guid categoryId,
        string locale,
        CategoryMegaMenuBindingInput input,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(input);
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.MegaMenuCategoryMissing));
        }

        var allItems = await _db.MegaMenuItems.ToListAsync(cancellationToken);
        var duplicate = allItems.SingleOrDefault(x => x.CategoryId == categoryId);
        var itemsById = allItems.ToDictionary(x => x.MegaMenuItemId);

        CatalogMegaMenuItem item;
        if (duplicate is null)
        {
            item = CatalogMegaMenuItem.BindCategory(
                categoryId,
                input.ParentMegaMenuItemId,
                input.SortOrder,
                input.IsVisible,
                input.IsFeatured,
                input.ImageMediaAssetId,
                input.IconMediaAssetId,
                DateTimeOffset.UtcNow);
            allItems.Add(item);
            itemsById[item.MegaMenuItemId] = item;
            _db.MegaMenuItems.Add(item);
        }
        else
        {
            item = duplicate;
            item.ParentMegaMenuItemId = input.ParentMegaMenuItemId;
            item.SortOrder = input.SortOrder;
            item.IsVisible = input.IsVisible;
            item.IsFeatured = input.IsFeatured;
            item.ImageMediaAssetId = input.ImageMediaAssetId;
            item.IconMediaAssetId = input.IconMediaAssetId;
        }

        var placement = CatalogMegaMenuTreeRules.ValidatePlacement(
            item.MegaMenuItemId,
            item.ParentMegaMenuItemId,
            itemsById);
        if (placement != CatalogMegaMenuPlacementViolation.None)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.MegaMenuPlacementInvalid));
        }

        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        await UpsertMegaMenuTranslationAsync(item.MegaMenuItemId, normalizedLocale, input, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveBindingAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var item = await _db.MegaMenuItems.SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (item is null)
        {
            return Result.Success();
        }

        var children = await _db.MegaMenuItems
            .Where(x => x.ParentMegaMenuItemId == item.MegaMenuItemId)
            .ToListAsync(cancellationToken);
        if (children.Count > 0)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.MegaMenuRemoveHasChildren));
        }

        var translations = await _db.MegaMenuItemTranslations
            .Where(x => x.MegaMenuItemId == item.MegaMenuItemId)
            .ToListAsync(cancellationToken);
        _db.MegaMenuItemTranslations.RemoveRange(translations);
        _db.MegaMenuItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MegaMenuPlacementOption>> ListPlacementOptionsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var allItems = await _db.MegaMenuItems.AsNoTracking().ToListAsync(cancellationToken);
        var items = allItems.Where(x => x.CategoryId != categoryId).ToList();
        if (items.Count == 0)
        {
            return Array.Empty<MegaMenuPlacementOption>();
        }

        var itemsById = allItems.ToDictionary(x => x.MegaMenuItemId);
        var categoryIds = items.Select(x => x.CategoryId).Distinct().ToArray();
        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(x => categoryIds.Contains(x.CategoryId) && x.Locale == normalizedLocale)
            .ToDictionaryAsync(x => x.CategoryId, cancellationToken);

        var options = new List<MegaMenuPlacementOption>();
        foreach (var item in items.OrderBy(x => x.SortOrder))
        {
            var level = ComputePresentationLevel(item.MegaMenuItemId, itemsById);
            if (level >= CatalogMegaMenuTreeRules.MaxPresentationDepth)
            {
                continue;
            }

            var label = translations.TryGetValue(item.CategoryId, out var tr) ? tr.Name : "—";
            var path = await BuildMenuPathAsync(item.MegaMenuItemId, normalizedLocale, itemsById, cancellationToken);
            options.Add(new MegaMenuPlacementOption(item.MegaMenuItemId, item.CategoryId, label, path, level));
        }

        return options;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StorefrontMegaMenuItem>> GetStorefrontMenuAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var uiSegment = MapUiLocaleSegment(normalizedLocale);
        var items = await _db.MegaMenuItems.AsNoTracking().ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return Array.Empty<StorefrontMegaMenuItem>();
        }

        var categories = await _db.Categories.AsNoTracking().ToDictionaryAsync(x => x.CategoryId, cancellationToken);
        var categoryIds = items.Select(x => x.CategoryId).Distinct().ToArray();
        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(x => categoryIds.Contains(x.CategoryId) && x.Locale == normalizedLocale)
            .ToDictionaryAsync(x => x.CategoryId, cancellationToken);

        var itemIds = items.Select(x => x.MegaMenuItemId).ToArray();
        var overrides = await _db.MegaMenuItemTranslations.AsNoTracking()
            .Where(x => itemIds.Contains(x.MegaMenuItemId) && x.Locale == normalizedLocale)
            .ToDictionaryAsync(x => x.MegaMenuItemId, cancellationToken);

        var composed = CatalogMegaMenuComposer.ComposeStorefrontMenu(
            items,
            categories,
            translations,
            overrides,
            normalizedLocale,
            uiSegment);

        return composed.Select(x => new StorefrontMegaMenuItem(
            x.MegaMenuItemId,
            x.ParentMegaMenuItemId,
            x.CategoryId,
            x.Title,
            x.Destination,
            x.IsFeatured,
            x.IconMediaAssetId,
            x.ImageMediaAssetId,
            x.SortOrder)).ToList();
    }

    private async Task UpsertMegaMenuTranslationAsync(
        Guid megaMenuItemId,
        string locale,
        CategoryMegaMenuBindingInput input,
        CancellationToken cancellationToken)
    {
        var existing = await _db.MegaMenuItemTranslations.SingleOrDefaultAsync(
            x => x.MegaMenuItemId == megaMenuItemId && x.Locale == locale,
            cancellationToken);
        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(input.TitleOverride)
                && string.IsNullOrWhiteSpace(input.BadgeText)
                && string.IsNullOrWhiteSpace(input.ShortLabel))
            {
                return;
            }

            _db.MegaMenuItemTranslations.Add(CatalogMegaMenuItemTranslation.Create(
                megaMenuItemId,
                locale,
                input.TitleOverride,
                input.BadgeText,
                input.ShortLabel));
            return;
        }

        existing.TitleOverride = string.IsNullOrWhiteSpace(input.TitleOverride) ? null : input.TitleOverride.Trim();
        existing.BadgeText = string.IsNullOrWhiteSpace(input.BadgeText) ? null : input.BadgeText.Trim();
        existing.ShortLabel = string.IsNullOrWhiteSpace(input.ShortLabel) ? null : input.ShortLabel.Trim();
        if (existing.TitleOverride is null && existing.BadgeText is null && existing.ShortLabel is null)
        {
            _db.MegaMenuItemTranslations.Remove(existing);
        }
    }

    private async Task<string> ResolveCategoryDisplayNameAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var translation = await _db.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId && x.Locale == locale, cancellationToken);
        if (translation is not null && !string.IsNullOrWhiteSpace(translation.Name))
        {
            return translation.Name;
        }

        var legacy = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Category
                && x.OwnerId == categoryId
                && x.FieldKey == "name"
                && x.Locale == locale)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return legacy ?? "—";
    }

    private async Task<string> BuildMenuPathAsync(
        Guid megaMenuItemId,
        string locale,
        IReadOnlyDictionary<Guid, CatalogMegaMenuItem> itemsById,
        CancellationToken cancellationToken)
    {
        var segments = new List<string>();
        var current = megaMenuItemId;
        var seen = new HashSet<Guid>();
        while (itemsById.TryGetValue(current, out var item))
        {
            if (!seen.Add(current))
            {
                break;
            }

            var name = await ResolveCategoryDisplayNameAsync(item.CategoryId, locale, cancellationToken);
            segments.Add(name);
            if (item.ParentMegaMenuItemId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        segments.Reverse();
        return string.Join(" › ", segments);
    }

    private static int ComputePresentationLevel(
        Guid megaMenuItemId,
        IReadOnlyDictionary<Guid, CatalogMegaMenuItem> itemsById)
    {
        var depth = 0;
        var current = megaMenuItemId;
        var seen = new HashSet<Guid>();
        while (itemsById.TryGetValue(current, out var item))
        {
            depth++;
            if (item.ParentMegaMenuItemId is not Guid parent || !seen.Add(current))
            {
                break;
            }

            current = parent;
        }

        return depth;
    }

    private static string MapUiLocaleSegment(string locale)
    {
        if (locale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        if (locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            return "ar";
        }

        return "fa";
    }

    private static string BuildUiCategoryRoute(string locale, string slug)
    {
        var clean = slug.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(clean))
        {
            return string.Empty;
        }

        return $"/{MapUiLocaleSegment(locale)}/category/{clean}";
    }
}
