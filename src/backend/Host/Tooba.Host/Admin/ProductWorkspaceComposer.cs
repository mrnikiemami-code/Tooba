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
    public async Task<IReadOnlyList<AdminProductListItem>> ListAsync(CancellationToken cancellationToken) =>
        await BuildListItemsAsync(maxRows: 100, cancellationToken);

    private async Task<IReadOnlyList<AdminProductListItem>> BuildListItemsAsync(
        int? maxRows,
        CancellationToken cancellationToken)
    {
        var productQuery = _catalog.Products.AsNoTracking().OrderByDescending(x => x.UpdatedAt);
        var products = maxRows.HasValue
            ? await productQuery.Take(maxRows.Value).ToListAsync(cancellationToken)
            : await productQuery.ToListAsync(cancellationToken);
        var productIds = products.Select(x => x.ProductId).ToList();
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, productIds, cancellationToken);
        var variantRows = await _catalog.Variants.AsNoTracking().Select(x => new { x.ProductId, x.VariantId }).ToListAsync(cancellationToken);
        var offerRows = await _offers.Offers.AsNoTracking().Select(x => new { x.OfferId, x.CatalogVariantId }).ToListAsync(cancellationToken);
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
        var rows = await BuildListItemsAsync(maxRows: null, cancellationToken);
        var filtered = AdminProductGridEvaluator.Apply(rows, request);
        return AdminProductGridEvaluator.Page(filtered, request);
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
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(title) || title == "untitled")
        {
            warnings.Add("missing-title");
        }

        if (mediaViews.Count == 0)
        {
            warnings.Add("missing-image");
        }

        if (offers.All(o => o.Status != OfferStatus.Active))
        {
            warnings.Add("no-active-offer");
        }

        if (priceViews.Count == 0)
        {
            warnings.Add("قیمت فروشنده ثبت نشده است");
        }

        if (stockViews.All(s => s.Available <= 0))
        {
            warnings.Add("موجودی قابل‌فروش وجود ندارد");
        }

        if (string.IsNullOrWhiteSpace(product.SeoTitleSeam) || string.IsNullOrWhiteSpace(product.SlugSeam))
        {
            warnings.Add("عنوان جستجو یا نشانی صفحه ناقص است");
        }

        var purchasable = offers.Any(o => o.Status == OfferStatus.Active)
            && priceViews.Count > 0
            && stockViews.Any(s => s.Available > 0);
        var publication = new ProductPublicationView(product.Status.ToString(), purchasable, warnings);

        return new ProductWorkspaceView(
            product.ProductId,
            title,
            product.Status.ToString(),
            product.Kind.ToString(),
            brandName,
            categoryLinks.Select(l => categoryNames.GetValueOrDefault(l.CategoryId) ?? l.CategoryId.ToString("N")[..8]).ToList(),
            attrViews,
            variantViews,
            mediaViews,
            offerViews,
            priceViews,
            taxViews,
            stockViews,
            new ProductSeoView(product.SlugSeam, product.SeoTitleSeam, ""),
            publication,
            [new ProductHistoryItem("activity", "محصول از فهرست کاتالوگ باز شد.", product.UpdatedAt)],
            [new ProductHistoryItem("audit", "آخرین ذخیرهٔ مشخصات کاتالوگ ثبت شد.", product.UpdatedAt)],
            permissions,
            product.UpdatedAt,
            warnings,
            ["media-binary-upload", "product-video-upload", "promotion-write", "full-content-studio"]);
    }

    /// <summary>
    /// عنوان محلی محصول را با قفل خوش‌بینانه به‌روز می‌کند.
    /// </summary>
    /// <summary>
    /// محصول Catalog ساده با گونهٔ پیش‌فرض می‌سازد و منتشر می‌کند؛ قیمت/موجودی/Offer اینجا نیست.
    /// </summary>
    public async Task<ProductWorkspaceView> CreateSimpleProductAsync(
        AdminProductCreateRequest request,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        if (!permissions.CanEditCatalog || !permissions.CanPublish)
        {
            throw new PlatformHttpException(403, "Forbidden", "workspace.permission.denied");
        }

        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new PlatformHttpException(400, "عنوان محصول لازم است.", "workspace.product.title.missing");
        }

        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale.Trim();
        var slugSeed = string.IsNullOrWhiteSpace(request.Slug)
            ? $"demo-{Guid.NewGuid():N}"[..18]
            : request.Slug.Trim();
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [locale] = title };

        try
        {
            var product = await _catalogDirectory.CreateProductAsync(
                CatalogProductKind.PhysicalGood,
                slugSeed,
                null,
                names,
                cancellationToken);

            Guid categoryId;
            if (request.CategoryId is Guid requested && requested != Guid.Empty)
            {
                categoryId = requested;
            }
            else
            {
                categoryId = await _catalog.Categories.AsNoTracking()
                    .OrderBy(x => x.CategoryId)
                    .Select(x => x.CategoryId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (categoryId == Guid.Empty)
                {
                    var createdCategory = await _catalogDirectory.CreateCategoryAsync(
                        null,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [locale] = "عمومی" },
                        cancellationToken);
                    categoryId = createdCategory.CategoryId;
                }
            }

            await _catalogDirectory.AssignCategoryAsync(product.ProductId, categoryId, cancellationToken);
            await _catalogDirectory.PublishProductAsync(product.ProductId, cancellationToken);

            var axis = await _catalog.AttributeDefinitions.AsNoTracking()
                .Where(x => x.IsVariantAxis)
                .OrderBy(x => x.Code)
                .FirstOrDefaultAsync(cancellationToken);
            Guid definitionId;
            Guid optionId;
            if (axis is null)
            {
                definitionId = await _catalogDirectory.CreateAttributeDefinitionAsync(
                    "default_option",
                    CatalogAttributeValueKind.Enumeration,
                    isVariantAxis: true,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [locale] = "گزینه",
                        ["en-US"] = "Option",
                    },
                    cancellationToken);
                optionId = await _catalogDirectory.AddAttributeOptionAsync(
                    definitionId,
                    "standard",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [locale] = "استاندارد",
                        ["en-US"] = "Standard",
                    },
                    cancellationToken);
            }
            else
            {
                definitionId = axis.DefinitionId;
                optionId = await _catalog.AttributeOptions.AsNoTracking()
                    .Where(x => x.DefinitionId == definitionId)
                    .OrderBy(x => x.Code)
                    .Select(x => x.OptionId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (optionId == Guid.Empty)
                {
                    optionId = await _catalogDirectory.AddAttributeOptionAsync(
                        definitionId,
                        $"opt-{Guid.NewGuid():N}"[..12],
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            [locale] = "استاندارد",
                            ["en-US"] = "Standard",
                        },
                        cancellationToken);
                }
            }

            await _catalogDirectory.CreateVariantAsync(
                product.ProductId,
                $"{slugSeed}-DEFAULT",
                [(definitionId, "ignored", optionId)],
                cancellationToken);

            return (await GetAsync(product.ProductId, permissions, cancellationToken))!;
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "workspace.product.create.rejected");
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
        var media = await _catalog.MediaReferences.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
        return media.Select(m => new ProductMediaView(m.MediaAssetId, m.IsPrimary, m.DisplayOrder, m.AltText)).ToList();
    }

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
    /// ترتیب گالری را بازنویسی می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> ReorderMediaAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        var media = await _catalog.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        if (media.Count == 0)
        {
            throw new PlatformHttpException(404, "رسانه‌ای برای این محصول نیست.", "workspace.media.empty");
        }

        var existing = media.Select(x => x.MediaAssetId).ToHashSet();
        var ordered = orderedMediaAssetIds ?? [];
        if (ordered.Count != existing.Count || ordered.Any(id => !existing.Contains(id)) || ordered.Distinct().Count() != ordered.Count)
        {
            throw new PlatformHttpException(
                400,
                "فهرست ترتیب باید دقیقاً همهٔ رسانه‌های فعلی را بدون تکرار پوشش دهد.",
                "workspace.media.order.invalid");
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            media.Single(m => m.MediaAssetId == ordered[i]).DisplayOrder = i;
        }

        TouchProduct(productId);
        await _catalog.SaveChangesAsync(cancellationToken);
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
        var media = await _catalog.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var target = media.SingleOrDefault(x => x.MediaAssetId == mediaAssetId)
            ?? throw new PlatformHttpException(404, "رسانه روی این محصول پیدا نشد.", "workspace.media.missing");
        foreach (var row in media)
        {
            row.IsPrimary = row.MediaAssetId == target.MediaAssetId;
        }

        TouchProduct(productId);
        await _catalog.SaveChangesAsync(cancellationToken);
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
        var row = await _catalog.MediaReferences.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.MediaAssetId == mediaAssetId,
            cancellationToken)
            ?? throw new PlatformHttpException(404, "رسانه روی این محصول پیدا نشد.", "workspace.media.missing");
        row.AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        TouchProduct(productId);
        await _catalog.SaveChangesAsync(cancellationToken);
        return await ListMediaAsync(productId, cancellationToken);
    }

    /// <summary>
    /// مرجع رسانه را حذف می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<ProductMediaView>> DetachMediaAsync(
        Guid productId,
        Guid mediaAssetId,
        ProductWorkspacePermissions permissions,
        CancellationToken cancellationToken)
    {
        EnsureCatalogEdit(permissions);
        var media = await _catalog.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var row = media.SingleOrDefault(x => x.MediaAssetId == mediaAssetId)
            ?? throw new PlatformHttpException(404, "رسانه روی این محصول پیدا نشد.", "workspace.media.missing");
        var wasPrimary = row.IsPrimary;
        _catalog.MediaReferences.Remove(row);
        media.Remove(row);
        if (wasPrimary && media.Count > 0)
        {
            var next = media.OrderBy(x => x.DisplayOrder).First();
            next.IsPrimary = true;
        }

        TouchProduct(productId);
        await _catalog.SaveChangesAsync(cancellationToken);
        return await ListMediaAsync(productId, cancellationToken);
    }

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
