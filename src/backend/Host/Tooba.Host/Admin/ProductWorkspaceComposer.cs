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
        var products = await _catalog.Products.AsNoTracking().OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(cancellationToken);
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
                product.UpdatedAt);
        }).ToList();
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

        var title = productNames.GetValueOrDefault(productId) ?? product.SlugSeam ?? "untitled";
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(title) || title == "untitled")
        {
            warnings.Add("missing-title");
        }

        if (media.Count == 0)
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
            media.Select((m, i) => new ProductMediaView(m.MediaAssetId, i == 0)).ToList(),
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
            ["media-binary-upload", "promotion-write", "full-content-studio"]);
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
