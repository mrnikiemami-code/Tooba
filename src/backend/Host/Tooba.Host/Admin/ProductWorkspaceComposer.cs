using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Tax.Infrastructure.Persistence;

using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;

namespace Tooba.Host.Admin;

/// <summary>
/// ترکیب خواندنی Workspace در Host. هر DbContext جدا پرس‌وجو می‌شود؛ SQL بین schemaها JOIN نمی‌شود.
/// </summary>
public sealed class ProductWorkspaceComposer
{
    private readonly CatalogDbContext _catalog;
    private readonly OfferDbContext _offers;
    private readonly PricingDbContext _prices;
    private readonly InventoryDbContext _inventory;
    private readonly TaxDbContext _tax;
    private readonly IPartyLookupGateway _parties;
    private readonly ICatalogDirectory _catalogDirectory;

    /// <summary>
    /// سازندهٔ ترکیب Host. جستجوی نام فروشنده جدا از Offer است و JOIN بین‌schema نیست.
    /// </summary>
    public ProductWorkspaceComposer(
        CatalogDbContext catalog,
        OfferDbContext offers,
        PricingDbContext prices,
        InventoryDbContext inventory,
        TaxDbContext tax,
        IPartyLookupGateway parties,
        ICatalogDirectory catalogDirectory)
    {
        _catalog = catalog;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
        _tax = tax;
        _parties = parties;
        _catalogDirectory = catalogDirectory;
    }

    /// <summary>
    /// فهرست محصولات Catalog برای ورود به Workspace.
    /// </summary>
    public async Task<IReadOnlyList<AdminProductListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var productIds = await _catalog.Products.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .Take(100)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken);
        return await BuildListItemsForProductIdsAsync(productIds, cancellationToken);
    }

    private async Task<IReadOnlyList<AdminProductListItem>> BuildListItemsForProductIdsAsync(
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var products = await _catalog.Products.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var byId = products.ToDictionary(x => x.ProductId);
        products = productIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, productIds, cancellationToken);
        var variantRows = await _catalog.Variants.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.VariantId })
            .ToListAsync(cancellationToken);
        var variantIds = variantRows.Select(x => x.VariantId).ToList();
        var offerRows = variantIds.Count == 0
            ? []
            : await _offers.Offers.AsNoTracking()
                .Where(x => variantIds.Contains(x.CatalogVariantId))
                .Select(x => new { x.OfferId, x.CatalogVariantId })
                .ToListAsync(cancellationToken);
        var offerIds = offerRows.Select(x => x.OfferId).ToList();
        var amountRows = offerIds.Count == 0
            ? []
            : await _prices.Prices.AsNoTracking()
                .Where(x => offerIds.Contains(x.OfferId))
                .Select(x => new { x.OfferId, x.Amount, x.Currency })
                .ToListAsync(cancellationToken);
        var unitRows = offerIds.Count == 0
            ? []
            : await _inventory.Positions.AsNoTracking()
                .Where(x => offerIds.Contains(x.OfferId))
                .Select(x => new { x.OfferId, x.OnHand, x.Reserved, x.LocationId })
                .ToListAsync(cancellationToken);
        var categoryLinks = productIds.Count == 0
            ? []
            : await _catalog.ProductCategories.AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId))
                .ToListAsync(cancellationToken);
        var categoryNames = await LoadNamesAsync(
            CatalogLocalizedOwnerKind.Category,
            categoryLinks.Select(x => x.CategoryId).Distinct().ToList(),
            cancellationToken);
        var mediaRows = productIds.Count == 0
            ? []
            : await _catalog.MediaReferences.AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId))
                .ToListAsync(cancellationToken);
        return products.Select(product =>
        {
            var variantIds = variantRows.Where(v => v.ProductId == product.ProductId).Select(v => v.VariantId).ToList();
            var productOffers = offerRows.Where(row => variantIds.Contains(row.CatalogVariantId)).ToList();
            var productOfferIds = productOffers.Select(row => row.OfferId).ToHashSet();
            var amounts = amountRows.Where(row => productOfferIds.Contains(row.OfferId)).ToList();
            var units = unitRows.Where(row => productOfferIds.Contains(row.OfferId)).ToList();
            var categories = categoryLinks
                .Where(link => link.ProductId == product.ProductId)
                .Select(link => categoryNames.GetValueOrDefault(link.CategoryId))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct()
                .ToList();
            var productMedia = mediaRows
                .Where(m => m.ProductId == product.ProductId)
                .OrderByDescending(m => m.IsPrimary)
                .ThenBy(m => m.DisplayOrder)
                .ToList();
            var primaryMedia = productMedia.FirstOrDefault(m => m.IsPrimary) ?? productMedia.FirstOrDefault();
            return new AdminProductListItem(
                product.ProductId,
                names.GetValueOrDefault(product.ProductId) ?? product.SlugSeam ?? product.ProductId.ToString("N")[..8],
                product.Status.ToString(),
                variantIds.Count,
                productOffers.Count,
                categories.Count == 0 ? "بدون دسته" : string.Join("، ", categories),
                FormatOfferAmountRange(amounts.Select(row => (row.Amount, row.Currency)).ToList()),
                units.Sum(row => row.OnHand - row.Reserved),
                units.Select(row => row.LocationId).Distinct().Count(),
                product.UpdatedAt,
                primaryMedia?.MediaAssetId);
        }).ToList();
    }

    /// <summary>
    /// فهرست محصولات Admin با قرارداد GridQuery/GridPage — فیلتر/مرتب‌سازی/صفحه‌بندی سمت Host.
    /// </summary>
    public async Task<GridPageResponse<AdminProductListItem>> QueryGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var engine = new AdminProductGridQueryEngine(_catalog, _offers, _prices, _inventory);
        var (pageIds, totalCount) = await engine.ResolvePageProductIdsAsync(request, cancellationToken);
        if (pageIds.Count == 0)
        {
            return new GridPageResponse<AdminProductListItem>([], request.Page, request.PageSize, totalCount);
        }

        var items = await BuildListItemsForProductIdsAsync(pageIds, cancellationToken);
        return new GridPageResponse<AdminProductListItem>(items, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// Workspace یک محصول را از برش‌های ماژولی می‌سازد.
    /// </summary>
    public async Task<ProductWorkspaceView?> GetAsync(Guid productId, ProductWorkspacePermissions permissions, CancellationToken cancellationToken)
    {
        var product = await _catalog.Products.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var productNames = await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, [productId], cancellationToken);
        var variants = await _catalog.Variants.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var variantIds = variants.Select(x => x.VariantId).ToList();
        var axes = await _catalog.VariantAttributeValues.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .ToListAsync(cancellationToken);
        var defs = await _catalog.AttributeDefinitions.AsNoTracking().ToDictionaryAsync(x => x.DefinitionId, cancellationToken);
        var productAttrs = await _catalog.ProductAttributeValues.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var media = await _catalog.MediaReferences.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var categoryLinks = await _catalog.ProductCategories.AsNoTracking().Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var categoryNames = await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, categoryLinks.Select(x => x.CategoryId).ToList(), cancellationToken);
        var brandName = product.BrandId is Guid brandId
            ? (await LoadNamesAsync(CatalogLocalizedOwnerKind.Brand, [brandId], cancellationToken)).GetValueOrDefault(brandId)
            : null;

        var offers = variantIds.Count == 0
            ? []
            : await _offers.Offers.AsNoTracking().Where(x => variantIds.Contains(x.CatalogVariantId)).ToListAsync(cancellationToken);
        var offerIds = offers.Select(x => x.OfferId).ToList();
        var prices = offerIds.Count == 0
            ? []
            : await _prices.Prices.AsNoTracking().Where(x => offerIds.Contains(x.OfferId)).ToListAsync(cancellationToken);
        var positions = offerIds.Count == 0
            ? []
            : await _inventory.Positions.AsNoTracking().Where(x => offerIds.Contains(x.OfferId)).ToListAsync(cancellationToken);
        var locationIds = positions.Select(x => x.LocationId).Distinct().ToList();
        var locations = locationIds.Count == 0
            ? []
            : await _inventory.Locations.AsNoTracking().Where(x => locationIds.Contains(x.LocationId)).ToListAsync(cancellationToken);
        var taxRows = offerIds.Count == 0
            ? []
            : await _tax.OfferClassifications.AsNoTracking().Where(x => offerIds.Contains(x.OfferId)).ToListAsync(cancellationToken);
        var taxCats = taxRows.Count == 0
            ? []
            : await _tax.Categories.AsNoTracking().Where(x => taxRows.Select(r => r.CategoryId).Contains(x.CategoryId)).ToListAsync(cancellationToken);

        var offerViews = new List<ProductOfferView>(offers.Count);
        foreach (var offer in offers)
        {
            var seller = await _parties.FindByIdAsync(offer.SellerPartyId, cancellationToken);
            offerViews.Add(new ProductOfferView(
                offer.OfferId,
                offer.CatalogVariantId,
                offer.SellerPartyId,
                seller?.DisplayName ?? "فروشنده",
                offer.Status.ToString(),
                offer.Channel.ToString(),
                offer.SellerSku));
        }
        var priceViews = prices.Select(p => new ProductPriceView(
            p.PriceId,
            p.OfferId,
            p.Market,
            p.Currency,
            p.Amount,
            p.Status.ToString(),
            p.ValidFrom,
            p.ValidTo)).ToList();
        var taxViews = taxRows.Select(row =>
        {
            var cat = taxCats.Single(c => c.CategoryId == row.CategoryId);
            return new ProductTaxView(row.OfferId, cat.CategoryId, cat.Code, cat.DisplayName);
        }).ToList();
        var stockViews = positions.Select(pos =>
        {
            var loc = locations.Single(l => l.LocationId == pos.LocationId);
            return new ProductStockView(pos.OfferId, loc.LocationId, loc.Code, loc.Name, pos.OnHand, pos.Reserved, pos.Available);
        }).ToList();

        var variantViews = variants.Select(v => new ProductVariantView(
            v.VariantId,
            v.CombinationFingerprint,
            v.Status.ToString(),
            v.CatalogCodeSeam,
            offers.Count(o => o.CatalogVariantId == v.VariantId),
            stockViews.Where(s => offers.Any(o => o.OfferId == s.OfferId && o.CatalogVariantId == v.VariantId))
                .Select(s => s.LocationId)
                .Distinct()
                .Count())).ToList();

        var attrViews = productAttrs.Select(a => new ProductAttributeView(
            defs.TryGetValue(a.DefinitionId, out var def) ? def.Code : a.DefinitionId.ToString("N")[..8],
            a.CanonicalValue,
            false)).Concat(axes.Select(a => new ProductAttributeView(
            defs.TryGetValue(a.DefinitionId, out var def) ? def.Code : "axis",
            a.CanonicalValue,
            true))).ToList();

        var mediaViews = media
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.DisplayOrder)
            .Select(m => new ProductMediaView(m.MediaAssetId, m.IsPrimary, m.DisplayOrder, m.AltText))
            .ToList();

        var title = productNames.GetValueOrDefault(productId) ?? product.SlugSeam ?? "untitled";
        var commercialWarnings = new List<string>();
        if (offers.All(o => o.Status != OfferStatus.Active))
        {
            commercialWarnings.Add("no-active-offer");
        }

        if (priceViews.Count == 0)
        {
            commercialWarnings.Add("قیمت فروشنده ثبت نشده است");
        }

        if (stockViews.All(s => s.Available <= 0))
        {
            commercialWarnings.Add("موجودی قابل‌فروش وجود ندارد");
        }

        var purchasable = offers.Any(o => o.Status == OfferStatus.Active)
            && priceViews.Count > 0
            && stockViews.Any(s => s.Available > 0);

        ProductPublishReadinessView aggregateReadiness;
        try
        {
            var readiness = await _catalogDirectory.GetProductPublishReadinessAsync(
                productId,
                "fa-IR",
                cancellationToken);
            aggregateReadiness = MapPublishReadiness(readiness);
        }
        catch (InvalidOperationException)
        {
            aggregateReadiness = new ProductPublishReadinessView(
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                [],
                ProductPublishRules.MessageNotReadyFa);
        }

        var catalogChecks = aggregateReadiness.MissingRequirements
            .Select(m => m.MessageFa)
            .ToList();
        if (catalogChecks.Count == 0 && !string.IsNullOrWhiteSpace(aggregateReadiness.MessageFa))
        {
            catalogChecks.Add(aggregateReadiness.MessageFa);
        }

        var publication = new ProductPublicationView(
            product.Status.ToString(),
            purchasable,
            catalogChecks,
            aggregateReadiness,
            product.UpdatedAt);

        var warnings = new List<string>(catalogChecks);
        warnings.AddRange(commercialWarnings);

        var primaryCategoryId = categoryLinks.Select(l => l.CategoryId).FirstOrDefault();
        Guid? primaryCategory = primaryCategoryId == Guid.Empty ? null : primaryCategoryId;
        var categoryPath = primaryCategory is Guid pcid
            ? await BuildCategoryPathAsync(pcid, cancellationToken)
            : null;
        var isPrimaryCategoryAssignable = false;
        if (primaryCategory is Guid assignableProbe)
        {
            var parentById = await _catalog.Categories.AsNoTracking()
                .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
            try
            {
                isPrimaryCategoryAssignable = CatalogCategoryTreeRules.IsAssignableProductCategory(
                    assignableProbe, parentById);
            }
            catch (InvalidOperationException)
            {
                isPrimaryCategoryAssignable = false;
            }

            if (!isPrimaryCategoryAssignable)
            {
                warnings.Add(CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa);
            }
        }

        var localizedRows = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product && x.OwnerId == productId)
            .ToListAsync(cancellationToken);
        var locales = localizedRows.Select(x => x.Locale).Append("fa-IR").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var translations = locales.Select(loc =>
        {
            string? Field(string key) => localizedRows
                .FirstOrDefault(r => r.Locale.Equals(loc, StringComparison.OrdinalIgnoreCase) && r.FieldKey == key)
                ?.Value;
            return new ProductTranslationView(
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

        return new ProductWorkspaceView(
            product.ProductId,
            title,
            product.Status.ToString(),
            product.Kind.ToString(),
            brandName,
            categoryLinks.Select(l => categoryNames.GetValueOrDefault(l.CategoryId) ?? "رده").ToList(),
            attrViews,
            variantViews,
            mediaViews,
            offerViews,
            priceViews,
            taxViews,
            stockViews,
            new ProductSeoView(product.SlugSeam, product.SeoTitleSeam, ""),
            publication,
            await BuildHistoryShellListsAsync(productId, cancellationToken),
            await BuildHistoryShellListsAsync(productId, cancellationToken, auditOnly: true),
            permissions,
            product.UpdatedAt,
            warnings,
            ["media-binary-upload", "product-video-upload", "promotion-write", "full-content-studio"],
            primaryCategory,
            categoryPath,
            product.SlugSeam,
            shortDescription,
            translations,
            isPrimaryCategoryAssignable,
            product.BrandId);
    }

    /// <summary>
    /// صفحهٔ تاریخچهٔ محصول برای تب تاریخچه (append-only، Catalog-only).
    /// </summary>
    public async Task<ProductHistoryPageView> GetHistoryPageAsync(
        Guid productId,
        string? section,
        int skip,
        int take,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanView)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        if (!await _catalog.Products.AsNoTracking().AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new PlatformHttpException(404, "Not Found", "workspace.product.missing");
        }

        try
        {
            var page = await _catalogDirectory.ListProductHistoryAsync(
                productId, section, skip, take, cancellationToken);
            return new ProductHistoryPageView(
                page.Items.Select(ToHistoryItemView).ToList(),
                page.TotalCount,
                page.Skip,
                page.Take);
        }
        catch (InvalidOperationException)
        {
            throw new PlatformHttpException(404, "Not Found", "workspace.product.missing");
        }
    }

    private async Task<IReadOnlyList<ProductHistoryItem>> BuildHistoryShellListsAsync(
        Guid productId,
        CancellationToken cancellationToken,
        bool auditOnly = false)
    {
        try
        {
            var page = await _catalogDirectory.ListProductHistoryAsync(
                productId, section: null, skip: 0, take: 20, cancellationToken);
            var rows = auditOnly
                ? page.Items.Where(x => x.Section is "lifecycle" or "seo" or "category").ToList()
                : page.Items.ToList();
            return rows.Select(x => new ProductHistoryItem(
                auditOnly ? "audit" : "activity",
                x.SummaryFa,
                x.OccurredAt,
                x.ActorDisplayName,
                x.SectionLabelFa,
                x.BeforeSummary,
                x.AfterSummary)).ToList();
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }

    private static ProductHistoryItemView ToHistoryItemView(ProductHistoryEntryDto row) =>
        new(
            row.HistoryId,
            row.EventType,
            row.Section,
            row.SectionLabelFa,
            row.SummaryFa,
            row.BeforeSummary,
            row.AfterSummary,
            row.ActorDisplayName,
            row.OccurredAt);

    private async Task<string> BuildCategoryPathAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var categories = await _catalog.Categories.AsNoTracking().ToListAsync(cancellationToken);
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
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, chain, cancellationToken);
        return string.Join(" > ", chain.Select(id => names.GetValueOrDefault(id) ?? "رده"));
    }

    /// <summary>
    /// محصول Catalog را به‌صورت پیش‌نویس می‌سازد؛ Category الزامی است و انتشار خودکار انجام نمی‌شود.
    /// </summary>
    public async Task<ProductWorkspaceView> CreateSimpleProductAsync(
        AdminProductCreateRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new PlatformHttpException(400, "عنوان محصول لازم است.", "workspace.product.title.missing");
        }

        if (request.CategoryId is not Guid categoryId || categoryId == Guid.Empty)
        {
            throw new PlatformHttpException(400, "انتخاب رده الزامی است.", "workspace.product.category.missing");
        }

        if (!await _catalog.Categories.AsNoTracking().AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new PlatformHttpException(400, "ردهٔ انتخاب‌شده معتبر نیست.", "workspace.product.category.invalid");
        }

        var parentById = await _catalog.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        if (!CatalogCategoryTreeRules.IsAssignableProductCategory(categoryId, parentById))
        {
            throw new PlatformHttpException(
                400,
                CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa,
                "workspace.product.category.level.invalid");
        }

        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale.Trim();
        var slugSeed = string.IsNullOrWhiteSpace(request.Slug)
            ? CatalogCategorySlugNormalizer.SlugifyFromName(title)
            : CatalogCategorySlugNormalizer.NormalizeSlug(request.Slug);
        if (string.IsNullOrWhiteSpace(slugSeed))
        {
            throw new PlatformHttpException(400, "نشانی صفحه نامعتبر است.", "workspace.product.slug.invalid");
        }

        if (await _catalog.Products.AsNoTracking().AnyAsync(x => x.SlugSeam == slugSeed, cancellationToken))
        {
            throw new PlatformHttpException(409, "این نشانی صفحه قبلاً استفاده شده است.", "workspace.product.slug.duplicate");
        }

        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [locale] = title };

        try
        {
            var product = await _catalogDirectory.CreateProductAsync(
                CatalogProductKind.PhysicalGood,
                slugSeed,
                null,
                names,
                cancellationToken);

            await _catalogDirectory.AssignCategoryAsync(product.ProductId, categoryId, cancellationToken);
            // Draft: بدون Publish خودکار؛ تنوع‌ها/رسانه در تسک‌های بعدی.
            return (await GetAsync(product.ProductId, permissions, cancellationToken))!;
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.product.create.rejected");
        }
    }

    /// <summary>
    /// هستهٔ محصول و ترجمهٔ locale فعال را به‌روز می‌کند.
    /// </summary>
    public async Task<ProductWorkspaceView> UpdateProductCoreAsync(
        Guid productId,
        AdminProductCoreUpdateRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        var product = await _catalog.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken)
            ?? throw new PlatformHttpException(404, "Not Found", "workspace.product.missing");
        if (product.UpdatedAt != request.ExpectedUpdatedAt)
        {
            throw new PlatformHttpException(409, "Conflict", "workspace.catalog.stale");
        }

        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new PlatformHttpException(400, "عنوان محصول لازم است.", "workspace.product.title.missing");
        }

        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale.Trim();
        var isPrimaryLocale = locale.Equals("fa-IR", StringComparison.OrdinalIgnoreCase);
        var previousSlug = product.SlugSeam;
        var previousTitle = (await _catalog.LocalizedTexts.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && x.OwnerId == productId
                    && x.FieldKey == "name"
                    && x.Locale == locale,
                cancellationToken))?.Value;

        // SlugSeam و SeoTitleSeam سراسری‌اند؛ فقط locale اصلی (fa-IR) آن‌ها را تغییر می‌دهد.
        string slug;
        if (isPrimaryLocale)
        {
            slug = string.IsNullOrWhiteSpace(request.Slug)
                ? CatalogCategorySlugNormalizer.SlugifyFromName(title)
                : CatalogCategorySlugNormalizer.NormalizeSlug(request.Slug);
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new PlatformHttpException(400, "نشانی صفحه نامعتبر است.", "workspace.product.slug.invalid");
            }

            if (await _catalog.Products.AsNoTracking()
                    .AnyAsync(x => x.ProductId != productId && x.SlugSeam == slug, cancellationToken))
            {
                throw new PlatformHttpException(409, "این نشانی صفحه قبلاً استفاده شده است.", "workspace.product.slug.duplicate");
            }
        }
        else
        {
            slug = product.SlugSeam ?? string.Empty;
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new PlatformHttpException(
                    400,
                    "ابتدا نشانی صفحهٔ محصول را در تب عمومی ذخیره کنید.",
                    "workspace.product.slug.missing");
            }
        }

        await UpsertLocalizedTextAsync(productId, "name", locale, title, cancellationToken);
        await UpsertLocalizedTextAsync(productId, "short_description", locale, request.ShortDescription, cancellationToken);
        await UpsertLocalizedTextAsync(productId, "full_description", locale, request.Description, cancellationToken);
        await UpsertLocalizedTextAsync(productId, "seo_title", locale, request.SeoTitle, cancellationToken);
        await UpsertLocalizedTextAsync(productId, "seo_description", locale, request.SeoDescription, cancellationToken);

        if (isPrimaryLocale)
        {
            product.TouchDescriptiveSeams(slug, request.SeoTitle?.Trim(), product.BrandId, DateTimeOffset.UtcNow);
        }
        else
        {
            // فقط UpdatedAt را برای concurrency جلو می‌بریم؛ seam سراسری دست‌نخورده می‌ماند.
            product.TouchDescriptiveSeams(product.SlugSeam, product.SeoTitleSeam, product.BrandId, DateTimeOffset.UtcNow);
        }
        await _catalog.SaveChangesAsync(cancellationToken);

        var beforeBits = new List<string>();
        var afterBits = new List<string>();
        if (!string.Equals(previousTitle, title, StringComparison.Ordinal))
        {
            beforeBits.Add($"عنوان: {previousTitle ?? "—"}");
            afterBits.Add($"عنوان: {title}");
        }

        if (!string.Equals(previousSlug, slug, StringComparison.Ordinal))
        {
            beforeBits.Add($"نشانی: {previousSlug ?? "—"}");
            afterBits.Add($"نشانی: {slug}");
        }

        var isLocalizedOnly = !isPrimaryLocale
            || (string.Equals(previousTitle, title, StringComparison.Ordinal)
                && string.Equals(previousSlug, slug, StringComparison.Ordinal));
        await _catalogDirectory.AppendProductHistoryAsync(
            productId,
            isLocalizedOnly
                ? ProductHistoryRules.EventLocalizedChanged
                : ProductHistoryRules.EventGeneralChanged,
            isLocalizedOnly ? ProductHistoryRules.SectionTranslations : ProductHistoryRules.SectionGeneral,
            isLocalizedOnly
                ? ProductHistoryRules.SummaryLocalizedFa
                : ProductHistoryRules.SummaryGeneralFa,
            beforeBits.Count == 0 ? null : string.Join(" · ", beforeBits),
            afterBits.Count == 0 ? null : string.Join(" · ", afterBits),
            cancellationToken);
        return (await GetAsync(productId, permissions, cancellationToken))!;
    }

    /// <summary>
    /// ردهٔ اصلی محصول را عوض می‌کند؛ بدون ConfirmSchemaImpact رد می‌شود اگر دادهٔ ویژگی/تنوع داشته باشد.
    /// </summary>
    public async Task<ProductWorkspaceView> AssignProductCategoryAsync(
        Guid productId,
        AdminProductCategoryAssignRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        var product = await _catalog.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken)
            ?? throw new PlatformHttpException(404, "Not Found", "workspace.product.missing");
        if (product.UpdatedAt != request.ExpectedUpdatedAt)
        {
            throw new PlatformHttpException(409, "Conflict", "workspace.catalog.stale");
        }

        if (!await _catalog.Categories.AsNoTracking().AnyAsync(x => x.CategoryId == request.CategoryId, cancellationToken))
        {
            throw new PlatformHttpException(400, "ردهٔ انتخاب‌شده معتبر نیست.", "workspace.product.category.invalid");
        }

        var parentById = await _catalog.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        if (!CatalogCategoryTreeRules.IsAssignableProductCategory(request.CategoryId, parentById))
        {
            throw new PlatformHttpException(
                400,
                CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa,
                "workspace.product.category.level.invalid");
        }

        var hasAttrValues = await _catalog.ProductAttributeValues.AsNoTracking()
            .AnyAsync(x => x.ProductId == productId, cancellationToken);
        var hasVariants = await _catalog.Variants.AsNoTracking()
            .AnyAsync(x => x.ProductId == productId, cancellationToken);
        if ((hasAttrValues || hasVariants) && !request.ConfirmSchemaImpact)
        {
            throw new PlatformHttpException(
                409,
                "تغییر رده ممکن است ویژگی‌ها یا تنوع‌های فعلی را تحت‌تأثیر قرار دهد. تأیید صریح لازم است.",
                "workspace.product.category.schema-impact");
        }

        try
        {
            var hasExistingCategory = await _catalog.ProductCategories.AsNoTracking()
                .AnyAsync(x => x.ProductId == productId, cancellationToken);
            if (hasExistingCategory)
            {
                await _catalogDirectory.ReplaceProductPrimaryCategoryAsync(
                    productId, request.CategoryId, cancellationToken);
            }
            else
            {
                await _catalogDirectory.AssignCategoryAsync(productId, request.CategoryId, cancellationToken);
            }

            product.TouchDescriptiveSeams(product.SlugSeam, product.SeoTitleSeam, product.BrandId, DateTimeOffset.UtcNow);
            await _catalog.SaveChangesAsync(cancellationToken);
            return (await GetAsync(productId, permissions, cancellationToken))!;
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.product.category.assign.rejected");
        }
    }

    /// <summary>
    /// برند Catalog را به محصول می‌چسباند یا جدا می‌کند.
    /// </summary>
    public async Task<ProductWorkspaceView> AssignProductBrandAsync(
        Guid productId,
        AdminProductBrandAssignRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        var product = await _catalog.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken)
            ?? throw new PlatformHttpException(404, "Not Found", "workspace.product.missing");
        if (product.UpdatedAt != request.ExpectedUpdatedAt)
        {
            throw new PlatformHttpException(409, "Conflict", "workspace.catalog.stale");
        }

        if (request.BrandId is { } brandId)
        {
            var exists = await _catalog.Brands.AsNoTracking().AnyAsync(x => x.BrandId == brandId, cancellationToken);
            if (!exists)
            {
                throw new PlatformHttpException(400, "برند انتخاب‌شده معتبر نیست.", "workspace.product.brand.invalid");
            }
        }

        var previous = product.BrandId;
        product.AssignBrand(request.BrandId, DateTimeOffset.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);

        await _catalogDirectory.AppendProductHistoryAsync(
            productId,
            ProductHistoryRules.EventGeneralChanged,
            ProductHistoryRules.SectionGeneral,
            "برند محصول به‌روزرسانی شد",
            previous is null ? "بدون برند" : "برند قبلی",
            request.BrandId is null ? "بدون برند" : "برند جدید",
            cancellationToken);

        return (await GetAsync(productId, permissions, cancellationToken))!;
    }

    /// <summary>
    /// فهرست برندها برای انتخابگر Admin.
    /// </summary>
    public Task<IReadOnlyList<AdminBrandOption>> ListBrandOptionsAsync(
        string? search,
        CancellationToken cancellationToken) =>
        ListBrandOptionsInternalAsync(search, cancellationToken);

    private async Task<IReadOnlyList<AdminBrandOption>> ListBrandOptionsInternalAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var brands = await _catalog.Brands.AsNoTracking().ToListAsync(cancellationToken);
        if (brands.Count == 0)
        {
            return [];
        }

        var brandIds = brands.Select(b => b.BrandId).ToList();
        var nameRows = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Brand
                && x.FieldKey == "name"
                && brandIds.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = nameRows
            .GroupBy(x => x.OwnerId)
            .ToDictionary(g => g.Key, g => g.First().Value);

        IEnumerable<AdminBrandOption> items = brands.Select(b =>
            new AdminBrandOption(
                b.BrandId,
                names.GetValueOrDefault(b.BrandId) ?? b.SlugSeam ?? "برند",
                b.Status.ToString()));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            items = items.Where(i => i.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        return items.OrderBy(i => i.Name, StringComparer.Ordinal).Take(200).ToList();
    }

    private async Task UpsertLocalizedTextAsync(
        Guid productId,
        string fieldKey,
        string locale,
        string? value,
        CancellationToken cancellationToken)
    {
        var normalized = value?.Trim();
        var row = await _catalog.LocalizedTexts.SingleOrDefaultAsync(
            x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.OwnerId == productId
                && x.FieldKey == fieldKey
                && x.Locale == locale,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (row is not null)
            {
                _catalog.LocalizedTexts.Remove(row);
            }

            return;
        }

        if (row is null)
        {
            _catalog.LocalizedTexts.Add(CatalogLocalizedText.Create(
                CatalogLocalizedOwnerKind.Product,
                productId,
                fieldKey,
                locale,
                normalized));
        }
        else
        {
            row.Value = normalized;
        }
    }

    /// <summary>
    /// عنوان محلی Catalog را با قفل خوش‌بینانه به‌روز می‌کند.
    /// </summary>
    public async Task<ProductWorkspaceView> UpdateCatalogTitleAsync(
        Guid productId,
        string locale,
        string title,
        DateTimeOffset expectedUpdatedAt,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        var product = await _catalog.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken)
            ?? throw new PlatformHttpException(404, "Not Found", "workspace.product.missing");
        if (product.UpdatedAt != expectedUpdatedAt)
        {
            throw new PlatformHttpException(409, "Conflict", "workspace.catalog.stale");
        }

        var row = await _catalog.LocalizedTexts.SingleOrDefaultAsync(
            x => x.OwnerKind == CatalogLocalizedOwnerKind.Product && x.OwnerId == productId && x.FieldKey == "name" && x.Locale == locale,
            cancellationToken);
        if (row is null)
        {
            _catalog.LocalizedTexts.Add(CatalogLocalizedText.Create(CatalogLocalizedOwnerKind.Product, productId, "name", locale, title));
        }
        else
        {
            row.Value = title.Trim();
        }

        product.UpdatedAt = DateTimeOffset.UtcNow;
        await _catalog.SaveChangesAsync(cancellationToken);
        await _catalogDirectory.AppendProductHistoryAsync(
            productId,
            ProductHistoryRules.EventLocalizedChanged,
            ProductHistoryRules.SectionGeneral,
            ProductHistoryRules.SummaryLocalizedFa,
            null,
            title.Trim(),
            cancellationToken);
        return (await GetAsync(productId, permissions, cancellationToken))!;
    }

    /// <summary>
    /// محصول را در Catalog منتشر می‌کند.
    /// </summary>
    public async Task<ProductWorkspaceView> PublishAsync(
        Guid productId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsurePublish(permissions);
        try
        {
            await _catalogDirectory.PublishProductAsync(productId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.product.publish.rejected");
        }

        return await RequireWorkspaceAsync(productId, permissions, cancellationToken);
    }

    /// <summary>
    /// انتشار را لغو و وضعیت را به پیش‌نویس می‌برد.
    /// </summary>
    public async Task<ProductWorkspaceView> UnpublishAsync(
        Guid productId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsurePublish(permissions);
        try
        {
            await _catalogDirectory.UnpublishProductAsync(productId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.product.unpublish.rejected");
        }

        return await RequireWorkspaceAsync(productId, permissions, cancellationToken);
    }

    /// <summary>
    /// محصول را آرشیو می‌کند.
    /// </summary>
    public async Task<ProductWorkspaceView> ArchiveAsync(
        Guid productId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsurePublish(permissions);
        try
        {
            await _catalogDirectory.ArchiveProductAsync(productId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.product.archive.rejected");
        }

        return await RequireWorkspaceAsync(productId, permissions, cancellationToken);
    }

    /// <summary>
    /// بازیابی صریح از بایگانی به پیش‌نویس.
    /// </summary>
    public async Task<ProductWorkspaceView> RestoreAsync(
        Guid productId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsurePublish(permissions);
        try
        {
            await _catalogDirectory.RestoreProductAsync(productId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.product.restore.rejected");
        }

        return await RequireWorkspaceAsync(productId, permissions, cancellationToken);
    }

    /// <summary>
    /// آمادگی تجمیعی انتشار Catalog-only.
    /// </summary>
    public async Task<ProductPublishReadinessView> GetPublishReadinessAsync(
        Guid productId,
        string? locale,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanView)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        await EnsureProductExistsAsync(productId, cancellationToken);
        try
        {
            var readiness = await _catalogDirectory.GetProductPublishReadinessAsync(
                productId,
                locale ?? "fa-IR",
                cancellationToken);
            return MapPublishReadiness(readiness);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(404, ex.Message, "workspace.product.missing");
        }
    }

    /// <summary>
    /// حذف امن؛ در صورت ارجاع Offer آرشیو نرم و تعارض فارسی.
    /// </summary>
    public async Task DeleteOrSoftArchiveAsync(
        Guid productId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        var product = await _catalog.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken)
            ?? throw new PlatformHttpException(404, "محصول پیدا نشد.", "workspace.product.missing");

        var variantIds = await _catalog.Variants.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.VariantId)
            .ToListAsync(cancellationToken);
        var hasOffers = variantIds.Count > 0
            && await _offers.Offers.AsNoTracking().AnyAsync(x => variantIds.Contains(x.CatalogVariantId), cancellationToken);

        if (hasOffers)
        {
            product.Archive(DateTimeOffset.UtcNow);
            await _catalog.SaveChangesAsync(cancellationToken);
            throw new PlatformHttpException(
                409,
                "حذف قطعی ممکن نیست چون پیشنهاد فروشنده به گونه‌های این محصول ارجاع دارد؛ محصول آرشیو شد.",
                "workspace.product.delete.referenced");
        }

        var media = await _catalog.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var productAttrs = await _catalog.ProductAttributeValues.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var axes = await _catalog.ProductVariantAxes.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var categories = await _catalog.ProductCategories.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var names = await _catalog.LocalizedTexts
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product && x.OwnerId == productId)
            .ToListAsync(cancellationToken);
        var variants = await _catalog.Variants.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var variantAttr = variantIds.Count == 0
            ? []
            : await _catalog.VariantAttributeValues.Where(x => variantIds.Contains(x.VariantId)).ToListAsync(cancellationToken);

        _catalog.MediaReferences.RemoveRange(media);
        _catalog.ProductAttributeValues.RemoveRange(productAttrs);
        _catalog.ProductVariantAxes.RemoveRange(axes);
        _catalog.ProductCategories.RemoveRange(categories);
        _catalog.LocalizedTexts.RemoveRange(names);
        _catalog.VariantAttributeValues.RemoveRange(variantAttr);
        _catalog.Variants.RemoveRange(variants);
        _catalog.Products.Remove(product);
        await _catalog.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// فهرست مرجع‌های رسانهٔ محصول.
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> ListMediaAsync(Guid productId, CancellationToken cancellationToken)
    {
        await EnsureProductExistsAsync(productId, cancellationToken);
        try
        {
            var state = await _catalogDirectory.GetProductMediaEditorStateAsync(productId, cancellationToken);
            return MapMediaViews(state.Items);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(404, ex.Message, "workspace.product.missing");
        }
    }

    /// <summary>
    /// آمادگی گالری رسانه برای انتشار بعدی.
    /// </summary>
    public async Task<ProductMediaReadinessView> GetMediaReadinessAsync(Guid productId, CancellationToken cancellationToken)
    {
        await EnsureProductExistsAsync(productId, cancellationToken);
        try
        {
            var readiness = await _catalogDirectory.GetProductMediaReadinessAsync(productId, cancellationToken);
            return new ProductMediaReadinessView(
                readiness.HasPrimaryImage,
                readiness.MediaCount,
                readiness.IsReady,
                readiness.MessageFa);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(404, ex.Message, "workspace.product.missing");
        }
    }

    /// <summary>
    /// فرادادهٔ SEO محصول برای locale فعال.
    /// </summary>
    public async Task<ProductSeoDetailView> GetSeoAsync(
        Guid productId,
        string? locale,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanView)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        try
        {
            var detail = await _catalogDirectory.GetProductSeoAsync(
                productId,
                locale ?? "fa-IR",
                cancellationToken);
            return MapSeoDetail(detail);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(404, ex.Message, "workspace.product.missing");
        }
    }

    /// <summary>
    /// به‌روزرسانی SEO محصول (SlugSeam سراسری + عنوان/توضیح محلی).
    /// </summary>
    public async Task<ProductSeoDetailView> UpdateSeoAsync(
        Guid productId,
        AdminProductSeoUpdateRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        try
        {
            var detail = await _catalogDirectory.UpdateProductSeoAsync(
                productId,
                new ProductSeoUpdateInput(
                    request.Locale,
                    request.Slug,
                    request.SeoTitle,
                    request.SeoDescription,
                    request.ExpectedUpdatedAt),
                cancellationToken);
            return MapSeoDetail(detail);
        }
        catch (InvalidOperationException ex)
        {
            if (string.Equals(ex.Message, "workspace.catalog.stale", StringComparison.Ordinal))
            {
                throw new PlatformHttpException(409, "Conflict", "workspace.catalog.stale");
            }

            if (ex.Message.Contains("قبلاً استفاده", StringComparison.Ordinal))
            {
                throw new PlatformHttpException(409, ex.Message, "workspace.product.slug.duplicate");
            }

            if (ex.Message.Contains("نامعتبر", StringComparison.Ordinal))
            {
                throw new PlatformHttpException(400, ex.Message, "workspace.product.slug.invalid");
            }

            if (ex.Message.Contains("Tenant", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("محصول", StringComparison.Ordinal))
            {
                throw new PlatformHttpException(404, ex.Message, "workspace.product.missing");
            }

            throw new PlatformHttpException(400, ex.Message, "workspace.product.seo.rejected");
        }
    }

    /// <summary>
    /// آمادگی SEO محصول برای locale فعال.
    /// </summary>
    public async Task<ProductSeoReadinessView> GetSeoReadinessAsync(
        Guid productId,
        string? locale,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanView)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        try
        {
            var readiness = await _catalogDirectory.GetProductSeoReadinessAsync(
                productId,
                locale ?? "fa-IR",
                cancellationToken);
            return new ProductSeoReadinessView(
                readiness.HasValidSlug,
                readiness.HasSeoTitleOrFallback,
                readiness.HasSeoDescription,
                readiness.HasLocalizedIdentity,
                readiness.IsReady,
                readiness.MessageFa);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(404, ex.Message, "workspace.product.missing");
        }
    }

    private static ProductSeoDetailView MapSeoDetail(ProductSeoDetail detail) =>
        new(
            detail.ProductId,
            detail.Locale,
            detail.Slug,
            detail.SeoTitle,
            detail.SeoDescription,
            detail.ProductName,
            detail.TitleFallback,
            detail.PublicPath,
            new ProductSeoReadinessView(
                detail.Readiness.HasValidSlug,
                detail.Readiness.HasSeoTitleOrFallback,
                detail.Readiness.HasSeoDescription,
                detail.Readiness.HasLocalizedIdentity,
                detail.Readiness.IsReady,
                detail.Readiness.MessageFa),
            detail.UpdatedAt);

    private static ProductPublishReadinessView MapPublishReadiness(ProductPublishReadiness readiness) =>
        new(
            readiness.IsReady,
            readiness.CategoryReady,
            readiness.TranslationReady,
            readiness.AttributeReady,
            readiness.VariantReady,
            readiness.MediaReady,
            readiness.SeoReady,
            readiness.MissingRequirements
                .Select(m => new ProductPublishMissingRequirementView(m.Code, m.MessageFa, m.WorkspaceTab))
                .ToList(),
            readiness.MessageFa);

    /// <summary>
    /// مرجع رسانهٔ مات اضافه می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> AttachMediaAsync(
        Guid productId,
        AdminProductMediaAttachRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        if (request.MediaAssetId == Guid.Empty)
        {
            throw new PlatformHttpException(400, "شناسهٔ رسانه لازم است.", "workspace.media.asset.missing");
        }

        try
        {
            await _catalogDirectory.AttachMediaReferenceAsync(
                productId,
                request.MediaAssetId,
                request.AltText,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.media.attach.rejected");
        }

        return await ListMediaAsync(productId, cancellationToken);
    }

    /// <summary>
    /// تصویر نمایشی بدون کتابخانهٔ Media می‌سازد و وصل می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> AttachPlaceholderMediaAsync(
        Guid productId,
        string? altText,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        try
        {
            await _catalogDirectory.AttachGeneratedPlaceholderMediaAsync(productId, altText, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.media.placeholder.rejected");
        }

        return await ListMediaAsync(productId, cancellationToken);
    }

    /// <summary>
    /// ترتیب گالری را بازنویسی می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> ReorderMediaAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        try
        {
            await _catalogDirectory.ReorderProductMediaAsync(productId, orderedMediaAssetIds ?? [], cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("رسانه‌ای برای این محصول نیست", StringComparison.Ordinal))
            {
                throw new PlatformHttpException(404, ex.Message, "workspace.media.empty");
            }

            if (ex.Message.Contains("فهرست ترتیب", StringComparison.Ordinal))
            {
                throw new PlatformHttpException(400, ex.Message, "workspace.media.order.invalid");
            }

            throw new PlatformHttpException(400, ex.Message, "workspace.media.order.rejected");
        }

        return await ListMediaAsync(productId, cancellationToken);
    }

    /// <summary>
    /// تصویر اصلی را تنظیم می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> SetPrimaryMediaAsync(
        Guid productId,
        Guid mediaAssetId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        try
        {
            await _catalogDirectory.SetProductPrimaryMediaAsync(productId, mediaAssetId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(404, ex.Message, "workspace.media.missing");
        }

        return await ListMediaAsync(productId, cancellationToken);
    }

    /// <summary>
    /// متن جایگزین رسانه را به‌روز می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> PatchMediaAltAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        try
        {
            await _catalogDirectory.PatchProductMediaAltAsync(productId, mediaAssetId, altText, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(404, ex.Message, "workspace.media.missing");
        }

        return await ListMediaAsync(productId, cancellationToken);
    }

    /// <summary>
    /// مرجع رسانه را حذف می‌کند (unassign؛ دارایی مشترک حذف نمی‌شود).
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> DetachMediaAsync(
        Guid productId,
        Guid mediaAssetId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        try
        {
            await _catalogDirectory.DetachProductMediaAsync(productId, mediaAssetId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(404, ex.Message, "workspace.media.missing");
        }

        return await ListMediaAsync(productId, cancellationToken);
    }

    private static IReadOnlyList<ProductMediaView> MapMediaViews(IReadOnlyList<ProductMediaAssignment> items) =>
        items
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(m => new ProductMediaView(m.MediaAssetId, m.IsPrimary, m.DisplayOrder, m.AltText))
            .ToList();

    /// <summary>
    /// گونهٔ جدید با محورها می‌سازد.
    /// </summary>
    public async Task<ProductWorkspaceView> CreateVariantAsync(
        Guid productId,
        AdminProductVariantCreateRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        if (request.Axes is null || request.Axes.Count == 0)
        {
            throw new PlatformHttpException(400, "حداقل یک محور برای گونه لازم است.", "workspace.variant.axes.missing");
        }

        try
        {
            await _catalogDirectory.CreateVariantAsync(
                productId,
                request.CatalogCodeSeam,
                request.Axes.Select(a => (a.DefinitionId, a.RawValue ?? string.Empty, a.EnumOptionId)).ToList(),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.variant.create.rejected");
        }

        return await RequireWorkspaceAsync(productId, permissions, cancellationToken);
    }

    /// <summary>
    /// وضعیت یا کد گونه را بدون شکستن اثرانگشت به‌روز می‌کند.
    /// </summary>
    public async Task<ProductWorkspaceView> PatchVariantAsync(
        Guid productId,
        Guid variantId,
        AdminProductVariantPatchRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        var variant = await _catalog.Variants.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.VariantId == variantId,
            cancellationToken)
            ?? throw new PlatformHttpException(404, "گونه پیدا نشد.", "workspace.variant.missing");

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<CatalogPublicationStatus>(request.Status.Trim(), ignoreCase: true, out var status))
            {
                throw new PlatformHttpException(400, "وضعیت گونه نامعتبر است.", "workspace.variant.status.invalid");
            }

            variant.SetStatus(status, DateTimeOffset.UtcNow);
        }

        if (request.CatalogCodeSeam is not null)
        {
            variant.UpdateCatalogCodeSeam(request.CatalogCodeSeam, DateTimeOffset.UtcNow);
        }

        TouchProduct(productId);
        await _catalog.SaveChangesAsync(cancellationToken);
        return await RequireWorkspaceAsync(productId, permissions, cancellationToken);
    }

    /// <summary>
    /// بازهٔ مبلغ پیشنهادها را برای فهرست می‌سازد. مبلغ روی هویت Product ذخیره نمی‌شود.
    /// </summary>
    private static string FormatOfferAmountRange(IReadOnlyList<(decimal Amount, string Currency)> rows)
    {
        if (rows.Count == 0)
        {
            return "بدون مبلغ";
        }

        var min = rows.Min(x => x.Amount);
        var max = rows.Max(x => x.Amount);
        var currency = rows[0].Currency;
        if (min == max)
        {
            return $"{min:0} {currency}".Trim();
        }

        return $"{min:0}–{max:0} {currency}".Trim();
    }

    private async Task EnsureProductExistsAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (!await _catalog.Products.AsNoTracking().AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new PlatformHttpException(404, "محصول پیدا نشد.", "workspace.product.missing");
        }
    }

    private async Task<ProductWorkspaceView> RequireWorkspaceAsync(
        Guid productId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken) =>
        await GetAsync(productId, permissions, cancellationToken)
        ?? throw new PlatformHttpException(404, "محصول پیدا نشد.", "workspace.product.missing");

    private static void EnsureCatalogEdit(ProductWorkspacePermissions permissions)
    {
        if (!permissions.CanEditCatalog)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }
    }

    private static void EnsurePublish(ProductWorkspacePermissions permissions)
    {
        if (!permissions.CanPublish)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }
    }

    private void TouchProduct(Guid productId)
    {
        var product = _catalog.Products.Local.SingleOrDefault(x => x.ProductId == productId);
        if (product is null)
        {
            product = _catalog.Products.Single(x => x.ProductId == productId);
        }

        product.UpdatedAt = DateTimeOffset.UtcNow;
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

        var rows = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == kind && ids.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.OwnerId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
    }
}
