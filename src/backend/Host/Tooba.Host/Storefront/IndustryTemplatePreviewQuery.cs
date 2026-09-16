#pragma warning disable CS1591
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;

namespace Tooba.Host.Storefront;

/// <summary>خواندن Sample Batch A/B فقط از Template Catalog — بدون union با Catalog عملیاتی.</summary>
public sealed class IndustryTemplatePreviewQuery
{
    private readonly CatalogDbContext _catalog;

    public IndustryTemplatePreviewQuery(CatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    public async Task<FashionTemplatePreviewDto?> GetSampleAsync(
        string templateKey,
        CancellationToken cancellationToken = default)
    {
        if (!IndustryPersistedTemplateCatalog.IsSupported(templateKey))
        {
            return null;
        }

        var template = await _catalog.StoreTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == templateKey && x.IsActive, cancellationToken);
        if (template is null)
        {
            return null;
        }

        var templateId = template.TemplateId;
        var origin = IndustryPersistedTemplateCatalog.OriginFor(templateKey);

        var categories = await (
            from c in _catalog.TemplateCategories.AsNoTracking()
            where c.TemplateId == templateId
            join t in _catalog.TemplateCategoryTranslations.AsNoTracking()
                on c.CategoryId equals t.CategoryId
            where t.Locale == IndustryPersistedTemplateCatalog.LocaleFa
            orderby c.SortOrder, t.Name
            select new FashionTemplateCategoryDto(
                c.CategoryId.ToString("D"),
                c.ParentCategoryId.HasValue ? c.ParentCategoryId.Value.ToString("D") : null,
                t.Name,
                c.ImageMediaAssetId.HasValue ? c.ImageMediaAssetId.Value.ToString("D") : null,
                c.ImageMediaAssetId.HasValue
                    ? IndustryPersistedTemplateCatalog.TryResolveMedia(templateKey, c.ImageMediaAssetId.Value)
                    : null)).ToListAsync(cancellationToken);

        var productRows = await (
            from p in _catalog.TemplateProducts.AsNoTracking()
            where p.TemplateId == templateId
            join name in _catalog.TemplateLocalizedTexts.AsNoTracking().Where(x =>
                x.OwnerKind == TemplateLocalizedOwnerKind.Product
                && x.FieldKey == "name"
                && x.Locale == IndustryPersistedTemplateCatalog.LocaleFa)
                on p.ProductId equals name.OwnerId
            join assign in _catalog.TemplateProductCategories.AsNoTracking()
                .Where(x => x.Role == CatalogProductCategoryRole.Primary)
                on p.ProductId equals assign.ProductId
            join catName in _catalog.TemplateCategoryTranslations.AsNoTracking()
                .Where(x => x.Locale == IndustryPersistedTemplateCatalog.LocaleFa)
                on assign.CategoryId equals catName.CategoryId
            join media in _catalog.TemplateProductMediaReferences.AsNoTracking()
                .Where(x => x.IsPrimary)
                on p.ProductId equals media.ProductId
            orderby p.SlugSeam
            select new
            {
                p.ProductId,
                p.SlugSeam,
                Title = name.Value,
                CategoryId = assign.CategoryId,
                CategoryName = catName.Name,
                media.MediaAssetId,
                p.BrandId,
            }).ToListAsync(cancellationToken);

        var products = productRows.Select((row, index) =>
        {
            var n = index + 1;
            var basePrice = templateKey switch
            {
                "auto-parts" => 420_000 + n * 28_000,
                "building-materials" => 180_000 + n * 45_000,
                "tools-hardware" => 350_000 + n * 32_000,
                "tile-ceramic" => 290_000 + n * 38_000,
                "interior-decor" => 1_250_000 + n * 95_000,
                _ => 4_800_000 + n * 210_000,
            };
            return new FashionTemplateProductDto(
                row.ProductId.ToString("D"),
                row.SlugSeam ?? row.ProductId.ToString("D"),
                row.Title,
                row.CategoryName,
                row.CategoryId.ToString("D"),
                row.MediaAssetId.ToString("D"),
                IndustryPersistedTemplateCatalog.TryResolveMedia(templateKey, row.MediaAssetId),
                $"template-{templateKey}-offer-{n}",
                $"template-{templateKey}-seller",
                $"نمایشگاه {template.Name} آزمایشی",
                basePrice,
                n % 3 == 0 ? basePrice - 40_000 : null,
                "IRR",
                12,
                true,
                n % 3 == 0 ? "پیشنهاد ویژه" : null,
                4.2 + (n % 5) * 0.1,
                8 + n,
                row.BrandId?.ToString("D"));
        }).ToList();

        var brandRows = await (
            from b in _catalog.TemplateBrands.AsNoTracking()
            where b.TemplateId == templateId
            join name in _catalog.TemplateLocalizedTexts.AsNoTracking().Where(x =>
                x.OwnerKind == TemplateLocalizedOwnerKind.Brand
                && x.FieldKey == "name"
                && x.Locale == IndustryPersistedTemplateCatalog.LocaleFa)
                on b.BrandId equals name.OwnerId
            orderby b.SlugSeam
            select new { b.BrandId, b.SlugSeam, Name = name.Value, b.LogoMediaAssetId }).ToListAsync(cancellationToken);

        var brands = brandRows.Select((b, index) => new FashionTemplateBrandDto(
            b.BrandId.ToString("D"),
            b.SlugSeam ?? b.BrandId.ToString("D"),
            b.Name,
            3 + index,
            b.LogoMediaAssetId?.ToString("D"),
            b.LogoMediaAssetId is Guid logo
                ? IndustryPersistedTemplateCatalog.TryResolveMedia(templateKey, logo)
                : null)).ToList();

        var landing = await _catalog.TemplateStoreLandingPages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TemplateId == templateId, cancellationToken);
        var sections = landing is null
            ? []
            : await _catalog.TemplateStoreLandingPageSections.AsNoTracking()
                .Where(x => x.PageId == landing.PageId && x.IsEnabled)
                .OrderBy(x => x.SortOrder)
                .Select(x => new FashionTemplateSectionDto(x.PageSectionId.ToString("D"), x.SectionType, x.SortOrder, x.ConfigurationJson))
                .ToListAsync(cancellationToken);

        var bannerItemCount = sections
            .Where(x => x.SectionType == "BannerShowcase")
            .Select(x =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(x.ConfigurationJson);
                    return doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array
                        ? items.GetArrayLength()
                        : 0;
                }
                catch
                {
                    return 0;
                }
            })
            .Sum();

        var templateProductIds = products.Select(x => Guid.Parse(x.ProductId)).ToList();
        var templateCategoryIds = categories.Select(x => Guid.Parse(x.CategoryId)).ToList();
        var templateBrandIds = brands.Select(x => Guid.Parse(x.BrandId)).ToList();

        var operationalProductHits = templateProductIds.Count == 0
            ? 0
            : await _catalog.Products.AsNoTracking().CountAsync(x => templateProductIds.Contains(x.ProductId), cancellationToken);
        var operationalCategoryHits = templateCategoryIds.Count == 0
            ? 0
            : await _catalog.Categories.AsNoTracking().CountAsync(x => templateCategoryIds.Contains(x.CategoryId), cancellationToken);
        var operationalBrandHits = templateBrandIds.Count == 0
            ? 0
            : await _catalog.Brands.AsNoTracking().CountAsync(x => templateBrandIds.Contains(x.BrandId), cancellationToken);

        return new FashionTemplatePreviewDto(
            origin,
            template.TemplateId.ToString("D"),
            template.Key,
            template.Name,
            new FashionTemplatePageDto(
                landing?.PageId.ToString("D") ?? Guid.Empty.ToString("D"),
                landing?.Locale ?? "fa",
                landing?.Slug ?? $"{templateKey}-template-sample",
                landing?.Title ?? $"پیش‌نمایش قالب {template.Name}",
                landing?.SeoTitle,
                landing?.SeoDescription,
                templateKey,
                sections),
            categories,
            products,
            brands,
            BuildCompositionFillers(templateKey, products),
            new FashionTemplatePurityDto(
                products.Count,
                categories.Count(c => c.ParentCategoryId is null),
                brands.Count,
                bannerItemCount,
                operationalProductHits,
                operationalCategoryHits,
                operationalBrandHits,
                operationalProductHits + operationalCategoryHits + operationalBrandHits == 0));
    }

    private static FashionTemplateCompositionFillersDto BuildCompositionFillers(
        string templateKey,
        IReadOnlyList<FashionTemplateProductDto> products)
    {
        var media1 = IndustryPersistedTemplateCatalog.MediaPublicUrl(templateKey, 2);
        var media2 = IndustryPersistedTemplateCatalog.MediaPublicUrl(templateKey, 4);
        var media3 = IndustryPersistedTemplateCatalog.MediaPublicUrl(templateKey, 6);
        var packLabel = templateKey switch
        {
            "auto-parts" => "لوازم یدکی",
            "building-materials" => "ساختمانی",
            "tools-hardware" => "ابزار",
            "tile-ceramic" => "کاشی",
            "interior-decor" => "دکوراسیون",
            _ => "لوازم خانگی",
        };
        return new FashionTemplateCompositionFillersDto(
            [
                new FashionTemplateArticleDto(
                    $"template-{templateKey}-article-1",
                    $"{templateKey}-guide-1",
                    $"راهنمای انتخاب {packLabel}",
                    "نکات کاربردی برای خرید مطمئن در این صنعت.",
                    Guid.Empty.ToString("D"),
                    media1,
                    "2026-03-01T00:00:00Z",
                    "تحریریه نمایشی",
                    ["راهنما"],
                    true),
                new FashionTemplateArticleDto(
                    $"template-{templateKey}-article-2",
                    $"{templateKey}-guide-2",
                    $"نگهداری محصولات {packLabel}",
                    "روش‌های ساده برای دوام بیشتر کالاهای پرمصرف.",
                    Guid.Empty.ToString("D"),
                    media2,
                    "2026-02-12T00:00:00Z",
                    "تحریریه نمایشی",
                    ["نگهداری"],
                    false),
                new FashionTemplateArticleDto(
                    $"template-{templateKey}-article-3",
                    $"{templateKey}-guide-3",
                    $"ترندهای بازار {packLabel}",
                    "آنچه در ویترین حرفه‌ای‌ها دیده می‌شود.",
                    Guid.Empty.ToString("D"),
                    media3,
                    "2026-01-20T00:00:00Z",
                    "تحریریه نمایشی",
                    ["بازار"],
                    false),
            ],
            [
                new FashionTemplateReviewDto(
                    $"template-{templateKey}-review-1",
                    "رضا",
                    5,
                    "کیفیت خوب",
                    "برای استفاده حرفه‌ای مناسب بود.",
                    true,
                    "2026-03-10T00:00:00Z",
                    products.ElementAtOrDefault(0)?.Title ?? "محصول نمونه",
                    products.ElementAtOrDefault(0)?.Slug ?? $"{templateKey}-prod-1"),
                new FashionTemplateReviewDto(
                    $"template-{templateKey}-review-2",
                    "مریم",
                    4,
                    "ارسال سریع",
                    "بسته‌بندی استاندارد و سالم بود.",
                    true,
                    "2026-03-08T00:00:00Z",
                    products.ElementAtOrDefault(2)?.Title ?? "محصول نمونه",
                    products.ElementAtOrDefault(2)?.Slug ?? $"{templateKey}-prod-3"),
                new FashionTemplateReviewDto(
                    $"template-{templateKey}-review-3",
                    "علی",
                    5,
                    "قیمت مناسب",
                    "نسبت کیفیت به قیمت قابل قبول است.",
                    false,
                    "2026-03-05T00:00:00Z",
                    products.ElementAtOrDefault(11)?.Title ?? "محصول نمونه",
                    products.ElementAtOrDefault(11)?.Slug ?? $"{templateKey}-prod-12"),
            ]);
    }
}
