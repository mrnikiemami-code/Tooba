#pragma warning disable CS1591
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;

namespace Tooba.Host.Storefront;

/// <summary>خواندن Sample Fashion فقط از Template Catalog — بدون union با Catalog عملیاتی.</summary>
public sealed class FashionTemplatePreviewQuery
{
    public const string Origin = FashionTemplateCatalogSeed.Origin;

    private readonly CatalogDbContext _catalog;

    public FashionTemplatePreviewQuery(CatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    public async Task<FashionTemplatePreviewDto?> GetFashionSampleAsync(CancellationToken cancellationToken = default)
    {
        var template = await _catalog.StoreTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == FashionTemplateCatalogIds.FashionKey && x.IsActive, cancellationToken);
        if (template is null)
        {
            return null;
        }

        var templateId = template.TemplateId;

        var categories = await (
            from c in _catalog.TemplateCategories.AsNoTracking()
            where c.TemplateId == templateId
            join t in _catalog.TemplateCategoryTranslations.AsNoTracking()
                on c.CategoryId equals t.CategoryId
            where t.Locale == FashionTemplateCatalogSeed.LocaleFa
            orderby c.SortOrder, t.Name
            select new FashionTemplateCategoryDto(
                c.CategoryId.ToString("D"),
                c.ParentCategoryId.HasValue ? c.ParentCategoryId.Value.ToString("D") : null,
                t.Name)).ToListAsync(cancellationToken);

        var productRows = await (
            from p in _catalog.TemplateProducts.AsNoTracking()
            where p.TemplateId == templateId
            join name in _catalog.TemplateLocalizedTexts.AsNoTracking().Where(x =>
                x.OwnerKind == TemplateLocalizedOwnerKind.Product
                && x.FieldKey == "name"
                && x.Locale == FashionTemplateCatalogSeed.LocaleFa)
                on p.ProductId equals name.OwnerId
            join assign in _catalog.TemplateProductCategories.AsNoTracking()
                .Where(x => x.Role == CatalogProductCategoryRole.Primary)
                on p.ProductId equals assign.ProductId
            join catName in _catalog.TemplateCategoryTranslations.AsNoTracking()
                .Where(x => x.Locale == FashionTemplateCatalogSeed.LocaleFa)
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
            return new FashionTemplateProductDto(
                row.ProductId.ToString("D"),
                row.SlugSeam ?? row.ProductId.ToString("D"),
                row.Title,
                row.CategoryName,
                row.CategoryId.ToString("D"),
                row.MediaAssetId.ToString("D"),
                FashionTemplateMediaPaths.TryResolve(row.MediaAssetId),
                $"template-fashion-offer-{n}",
                "template-fashion-seller",
                "نمایشگاه پوشاک آزمایشی",
                890_000 + n * 35_000,
                n % 3 == 0 ? 790_000 + n * 20_000 : null,
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
                && x.Locale == FashionTemplateCatalogSeed.LocaleFa)
                on b.BrandId equals name.OwnerId
            orderby b.SlugSeam
            select new { b.BrandId, b.SlugSeam, Name = name.Value, b.LogoMediaAssetId }).ToListAsync(cancellationToken);

        var brands = brandRows.Select((b, index) => new FashionTemplateBrandDto(
            b.BrandId.ToString("D"),
            b.SlugSeam ?? b.BrandId.ToString("D"),
            b.Name,
            3 + index,
            b.LogoMediaAssetId?.ToString("D"),
            b.LogoMediaAssetId is Guid logo ? FashionTemplateMediaPaths.TryResolve(logo) : null)).ToList();

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

        // Purity: ensure zero operational catalog IDs overlap (none should).
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
            Origin,
            template.TemplateId.ToString("D"),
            template.Key,
            template.Name,
            new FashionTemplatePageDto(
                landing?.PageId.ToString("D") ?? FashionTemplateCatalogIds.LandingPageId.ToString("D"),
                landing?.Locale ?? "fa",
                landing?.Slug ?? "fashion-template-sample",
                landing?.Title ?? "پیش‌نمایش قالب پوشاک",
                landing?.SeoTitle,
                landing?.SeoDescription,
                "fashion",
                sections),
            categories,
            products,
            brands,
            BuildCompositionFillers(products),
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

    private static FashionTemplateCompositionFillersDto BuildCompositionFillers(IReadOnlyList<FashionTemplateProductDto> products)
    {
        var media1 = FashionTemplateMediaPaths.PublicUrl(2);
        var media2 = FashionTemplateMediaPaths.PublicUrl(4);
        var media3 = FashionTemplateMediaPaths.PublicUrl(6);
        return new FashionTemplateCompositionFillersDto(
            [
                new FashionTemplateArticleDto(
                    "template-fashion-article-1",
                    "fashion-style-guide",
                    "راهنمای استایل فصل",
                    "چطور چند قطعه پایه را با هم ترکیب کنید.",
                    FashionTemplateCatalogIds.MediaAsset(2).ToString("D"),
                    media1,
                    "2026-03-01T00:00:00Z",
                    "تحریریه نمایشی",
                    ["استایل"],
                    true),
                new FashionTemplateArticleDto(
                    "template-fashion-article-2",
                    "fashion-fabric-care",
                    "مراقبت از پارچه‌های ظریف",
                    "نکات ساده برای ماندگاری لباس‌های روزمره.",
                    FashionTemplateCatalogIds.MediaAsset(4).ToString("D"),
                    media2,
                    "2026-02-12T00:00:00Z",
                    "تحریریه نمایشی",
                    ["مراقبت"],
                    false),
                new FashionTemplateArticleDto(
                    "template-fashion-article-3",
                    "fashion-trends",
                    "روندهای ملایم بهار",
                    "رنگ‌ها و برش‌هایی که در ویترین دیده می‌شوند.",
                    FashionTemplateCatalogIds.MediaAsset(6).ToString("D"),
                    media3,
                    "2026-01-20T00:00:00Z",
                    "تحریریه نمایشی",
                    ["ترند"],
                    false),
            ],
            [
                new FashionTemplateReviewDto(
                    "template-fashion-review-1",
                    "سارا",
                    5,
                    "کیفیت خوب",
                    "پارچه نرم بود و اندازه دقیق بود.",
                    true,
                    "2026-03-10T00:00:00Z",
                    products.ElementAtOrDefault(0)?.Title ?? "مانتو کتان بهاره",
                    products.ElementAtOrDefault(0)?.Slug ?? "fashion-prod-1"),
                new FashionTemplateReviewDto(
                    "template-fashion-review-2",
                    "نیما",
                    4,
                    "ارسال سریع",
                    "برای استفاده روزمره مناسب است.",
                    true,
                    "2026-03-08T00:00:00Z",
                    products.ElementAtOrDefault(2)?.Title ?? "شلوار جین اسلیم",
                    products.ElementAtOrDefault(2)?.Slug ?? "fashion-prod-3"),
                new FashionTemplateReviewDto(
                    "template-fashion-review-3",
                    "مینا",
                    5,
                    "رنگ عالی",
                    "با بقیه لباس‌هایم خوب ست شد.",
                    false,
                    "2026-03-05T00:00:00Z",
                    products.ElementAtOrDefault(11)?.Title ?? "شال نخی تابستانی",
                    products.ElementAtOrDefault(11)?.Slug ?? "fashion-prod-12"),
            ]);
    }
}

public sealed record FashionTemplatePreviewDto(
    string Origin,
    string TemplateId,
    string TemplateKey,
    string TemplateName,
    FashionTemplatePageDto Page,
    IReadOnlyList<FashionTemplateCategoryDto> Categories,
    IReadOnlyList<FashionTemplateProductDto> Products,
    IReadOnlyList<FashionTemplateBrandDto> Brands,
    FashionTemplateCompositionFillersDto CompositionFillers,
    FashionTemplatePurityDto Purity);

public sealed record FashionTemplatePageDto(
    string PageId,
    string Locale,
    string Slug,
    string Title,
    string? SeoTitle,
    string? SeoDescription,
    string TemplateKey,
    IReadOnlyList<FashionTemplateSectionDto> Sections);

public sealed record FashionTemplateSectionDto(
    string PageSectionId,
    string SectionType,
    int SortOrder,
    string ConfigurationJson);

public sealed record FashionTemplateCategoryDto(string CategoryId, string? ParentCategoryId, string Name);

public sealed record FashionTemplateProductDto(
    string ProductId,
    string Slug,
    string Title,
    string CategoryName,
    string CategoryId,
    string MediaAssetId,
    string? MediaUrl,
    string PrimaryOfferId,
    string SellerPartyId,
    string SellerDisplayName,
    decimal OfferAmountExclusiveOfTax,
    decimal? PromotionalAmountExclusiveOfTax,
    string Currency,
    int AvailableUnits,
    bool InStock,
    string? PromotionLabel,
    double AverageRating,
    int ReviewCount,
    string? BrandId);

public sealed record FashionTemplateBrandDto(
    string BrandId,
    string Slug,
    string Name,
    int ProductCount,
    string? LogoMediaAssetId,
    string? LogoUrl);

public sealed record FashionTemplateCompositionFillersDto(
    IReadOnlyList<FashionTemplateArticleDto> Articles,
    IReadOnlyList<FashionTemplateReviewDto> Reviews);

public sealed record FashionTemplateArticleDto(
    string ArticleId,
    string Slug,
    string Title,
    string Excerpt,
    string CoverMediaAssetId,
    string? CoverMediaUrl,
    string PublishDate,
    string AuthorDisplayName,
    IReadOnlyList<string> Tags,
    bool IsFeatured);

public sealed record FashionTemplateReviewDto(
    string PublicId,
    string AuthorDisplayName,
    int Rating,
    string Title,
    string Body,
    bool VerifiedPurchase,
    string CreatedAt,
    string ProductTitle,
    string ProductSlug);

public sealed record FashionTemplatePurityDto(
    int TemplateProductCount,
    int TemplateTopLevelCategoryCount,
    int TemplateBrandCount,
    int TemplateBannerItemCount,
    int OperationalProductIdHits,
    int OperationalCategoryIdHits,
    int OperationalBrandIdHits,
    bool IsPure);
