using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>دانهٔ idempotent Template Catalog برای قالب Fashion (TB-P10-T022-R5).</summary>
internal static class FashionTemplateCatalogSeed
{
    internal const string LocaleFa = "fa-IR";
    internal const string Origin = "fashion-template-catalog-persisted";

    private static readonly string[] TreeRoots =
    [
        "زنانه",
        "مردانه",
        "بچگانه",
        "کفش",
        "کیف و اکسسوری",
        "ورزشی",
        "لباس رسمی",
        "فصل جدید",
    ];

    private static readonly string[] ProductTitles =
    [
        "مانتو کتان بهاره",
        "شومیز ابریشمی گل‌دار",
        "شلوار جین اسلیم",
        "کت بلیزر کلاسیک",
        "پیراهن نخی مردانه",
        "هودی پنبه‌ای اورسایز",
        "دامن پلیسه میدی",
        "تی‌شرت بیسیک رنگی",
        "کفش اسنیکر شهری",
        "بوت چرمی کوتاه",
        "کیف دوشی مینیمال",
        "شال نخی تابستانی",
        "ست ورزشی سبک",
        "لباس مجلسی ساده",
        "کاپشن سبک پاییزه",
    ];

    private static readonly string[] BrandNames =
    [
        "نوآ پوشاک",
        "ریتم استایل",
        "سادهٔ شهری",
        "گلبرگ",
        "خط فرم",
        "پنبه خانه",
    ];

    public static async Task ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var catalog = provider.GetRequiredService<CatalogDbContext>();
        var now = DateTimeOffset.UtcNow;

        if (await catalog.StoreTemplates.AnyAsync(x => x.Key == FashionTemplateCatalogIds.FashionKey, cancellationToken))
        {
            return;
        }

        catalog.StoreTemplates.Add(StoreTemplate.Create(
            FashionTemplateCatalogIds.FashionKey,
            "پوشاک",
            now,
            FashionTemplateCatalogIds.TemplateId));

        for (var b = 1; b <= BrandNames.Length; b++)
        {
            var brandId = FashionTemplateCatalogIds.BrandId(b);
            catalog.TemplateBrands.Add(new TemplateBrand
            {
                BrandId = brandId,
                TemplateId = FashionTemplateCatalogIds.TemplateId,
                SlugSeam = $"fashion-brand-{b}",
                Status = CatalogPublicationStatus.Published,
                LogoMediaAssetId = FashionTemplateCatalogIds.MediaAsset(((b - 1) % 8) + 1),
                CreatedAt = now,
                UpdatedAt = now,
            });
            catalog.TemplateLocalizedTexts.Add(new TemplateLocalizedText
            {
                TextId = FashionTemplateCatalogIds.LocalizedBrandName(b),
                OwnerKind = TemplateLocalizedOwnerKind.Brand,
                OwnerId = brandId,
                FieldKey = "name",
                Locale = LocaleFa,
                Value = BrandNames[b - 1]!,
            });
        }

        var leafIds = new List<Guid>();
        for (var root = 1; root <= TreeRoots.Length; root++)
        {
            var rootName = TreeRoots[root - 1]!;
            var rootId = FashionTemplateCatalogIds.CategoryRoot(root);
            AddCategory(catalog, rootId, null, root, 0, 0, rootName, $"fashion-root-{root}", root, now);
            for (var mid = 1; mid <= 2; mid++)
            {
                var midId = FashionTemplateCatalogIds.CategoryMid(root, mid);
                var midName = $"{rootName} · سطح {(mid + 1).ToString("0", System.Globalization.CultureInfo.GetCultureInfo("fa-IR"))}";
                AddCategory(catalog, midId, rootId, root, mid, 0, midName, $"fashion-mid-{root}-{mid}", mid, now);
                for (var leaf = 1; leaf <= 2; leaf++)
                {
                    var leafId = FashionTemplateCatalogIds.CategoryLeaf(root, mid, leaf);
                    var leafName = $"{rootName} · زیر {(mid * 2 + leaf).ToString("0", System.Globalization.CultureInfo.GetCultureInfo("fa-IR"))}";
                    AddCategory(catalog, leafId, midId, root, mid, leaf, leafName, $"fashion-leaf-{root}-{mid}-{leaf}", leaf, now);
                    leafIds.Add(leafId);
                }
            }
        }

        for (var n = 1; n <= ProductTitles.Length; n++)
        {
            var productId = FashionTemplateCatalogIds.ProductId(n);
            var categoryId = leafIds[(n - 1) % leafIds.Count];
            var brandId = FashionTemplateCatalogIds.BrandId(((n - 1) % BrandNames.Length) + 1);
            var mediaId = FashionTemplateCatalogIds.MediaAsset(((n - 1) % 8) + 1);
            catalog.TemplateProducts.Add(new TemplateProduct
            {
                ProductId = productId,
                TemplateId = FashionTemplateCatalogIds.TemplateId,
                Kind = CatalogProductKind.PhysicalGood,
                Status = CatalogPublicationStatus.Published,
                BrandId = brandId,
                SlugSeam = $"fashion-prod-{n}",
                SeoTitleSeam = ProductTitles[n - 1],
                UnitOfMeasureId = CanonicalUnits.Pcs,
                QuantityDecimalPlaces = 0,
                QuantityStep = null,
                CreatedAt = now,
                UpdatedAt = now,
            });
            catalog.TemplateLocalizedTexts.Add(new TemplateLocalizedText
            {
                TextId = FashionTemplateCatalogIds.LocalizedProductName(n),
                OwnerKind = TemplateLocalizedOwnerKind.Product,
                OwnerId = productId,
                FieldKey = "name",
                Locale = LocaleFa,
                Value = ProductTitles[n - 1]!,
            });
            catalog.TemplateProductCategories.Add(new TemplateProductCategory
            {
                AssignmentId = FashionTemplateCatalogIds.ProductCategoryAssignment(n),
                ProductId = productId,
                CategoryId = categoryId,
                Role = CatalogProductCategoryRole.Primary,
            });
            catalog.TemplateProductMediaReferences.Add(new TemplateProductMediaReference
            {
                ReferenceId = FashionTemplateCatalogIds.ProductMediaRef(n),
                ProductId = productId,
                MediaAssetId = mediaId,
                DisplayOrder = 0,
                IsPrimary = true,
                AltText = ProductTitles[n - 1],
            });
        }

        catalog.TemplateStoreLandingPages.Add(new TemplateStoreLandingPage
        {
            PageId = FashionTemplateCatalogIds.LandingPageId,
            TemplateId = FashionTemplateCatalogIds.TemplateId,
            Locale = "fa",
            Slug = "fashion-template-sample",
            Title = "پیش‌نمایش قالب پوشاک",
            SeoTitle = "قالب پوشاک",
            SeoDescription = "دادهٔ Template Catalog برای پیش‌نمایش نمونه",
            TemplateKey = "fashion",
            Status = StoreLandingPageStatus.Published,
            CreatedAt = now,
            UpdatedAt = now,
        });

        var bannerJson = JsonSerializer.Serialize(new
        {
            title = "بنرهای پوشاک",
            heightPreset = "Medium",
            variantKey = "TwoEqual",
            items = new object[]
            {
                new
                {
                    imageUrl = FashionTemplateMediaPaths.PublicUrl(1),
                    mediaAssetId = FashionTemplateCatalogIds.MediaAsset(1).ToString("D"),
                    href = "#",
                    title = "کمپین فصل جدید",
                    text = "انتخاب‌های تازه برای ویترین پوشاک",
                    ctaLabel = "مشاهده",
                },
                new
                {
                    imageUrl = FashionTemplateMediaPaths.PublicUrl(2),
                    mediaAssetId = FashionTemplateCatalogIds.MediaAsset(2).ToString("D"),
                    href = "#",
                    title = "تخفیف اکسسوری",
                    text = "کیف و شال‌های سبک",
                    ctaLabel = "مشاهده",
                },
            },
        });

        catalog.TemplateStoreLandingPageSections.Add(new TemplateStoreLandingPageSection
        {
            PageSectionId = FashionTemplateCatalogIds.BannerSectionId(1),
            PageId = FashionTemplateCatalogIds.LandingPageId,
            SectionType = "BannerShowcase",
            SortOrder = 40,
            IsEnabled = true,
            ConfigurationJson = bannerJson,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await catalog.SaveChangesAsync(cancellationToken);
    }

    private static void AddCategory(
        CatalogDbContext catalog,
        Guid categoryId,
        Guid? parentId,
        int root,
        int mid,
        int leaf,
        string name,
        string slug,
        int sortOrder,
        DateTimeOffset now)
    {
        catalog.TemplateCategories.Add(new TemplateCategory
        {
            CategoryId = categoryId,
            TemplateId = FashionTemplateCatalogIds.TemplateId,
            ParentCategoryId = parentId,
            Status = CatalogPublicationStatus.Published,
            SortOrder = sortOrder,
            IsVisible = true,
            ImageMediaAssetId = FashionTemplateCatalogIds.MediaAsset(((root - 1) % 8) + 1),
            CreatedAt = now,
            UpdatedAt = now,
        });
        catalog.TemplateCategoryTranslations.Add(new TemplateCategoryTranslation
        {
            TranslationId = FashionTemplateCatalogIds.CategoryTranslation(root, mid, leaf),
            CategoryId = categoryId,
            Locale = LocaleFa,
            Name = name,
            Slug = slug,
            UpdatedAt = now,
        });
    }
}

/// <summary>مسیرهای استاتیک محلی برای رسانهٔ قالب Fashion (بدون hotlink).</summary>
internal static class FashionTemplateMediaPaths
{
    public static string PublicUrl(int index)
    {
        var n = ((index - 1) % 8) + 1;
        return $"/images/fashion-template/{n}.jpg";
    }

    public static string? TryResolve(Guid mediaAssetId)
    {
        for (var i = 1; i <= 8; i++)
        {
            if (mediaAssetId == FashionTemplateCatalogIds.MediaAsset(i))
            {
                return PublicUrl(i);
            }
        }

        return null;
    }
}

/// <summary>اعمال دانه روی tenant توسعه.</summary>
internal static class FashionTemplateCatalogSeedHost
{
    public static async Task ApplyAsync(IServiceProvider root)
    {
        await using var scope = root.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
        {
            return;
        }

        var assigner = provider.GetRequiredService<ICommerceContextAssigner>();
        assigner.Assign(new CommerceContext(
            new EditionContext(registry.Edition, registry.DeploymentId),
            new TenantContext(
                tenant.TenantId,
                tenant.Status,
                tenant.ConnectionReference,
                tenant.DisplayName,
                tenant.ThemeReference,
                tenant.DefaultMarketReference,
                tenant.Hosts[0],
                tenant.PrimaryDomain),
            tenant.ConnectionReference,
            "fashion-template-catalog-seed"));
        await FashionTemplateCatalogSeed.ApplyAsync(provider);
    }
}
