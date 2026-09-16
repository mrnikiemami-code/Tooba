using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>
/// Batch C industry Template Catalog seeds (TB-P10-T022-R12C).
/// Exact Fashion architecture/persistence conventions — isolated ID namespaces + media paths.
/// </summary>
internal static class IndustryBatchCTemplateCatalogSeed
{
    internal const string LocaleFa = "fa-IR";

    private sealed record PackIds(
        string Key,
        string Origin,
        string NameFa,
        string SlugPrefix,
        string MediaFolder,
        string LandingSlug,
        string LandingTitle,
        string BannerTitle,
        string BannerCtaA,
        string BannerTextA,
        string BannerCtaB,
        string BannerTextB,
        string HistoryActor,
        string[] TreeRoots,
        string[] ProductTitles,
        string[] BrandNames,
        string[] AttrCodes,
        string[] AttrNamesFa,
        string[] OptionCodes,
        string[] OptionNamesFa,
        string[] TagCodes,
        string[] TagNamesFa,
        string[] TextAttrValues,
        Guid TemplateId,
        Guid LandingPageId,
        Func<int, Guid> MediaAsset,
        Func<int, Guid> BrandId,
        Func<int, Guid> CategoryRoot,
        Func<int, int, Guid> CategoryMid,
        Func<int, int, int, Guid> CategoryLeaf,
        Func<int, Guid> ProductId,
        Func<int, Guid> ProductMediaRef,
        Func<int, Guid> ProductCategoryAssignment,
        Func<int, Guid> LocalizedProductName,
        Func<int, Guid> LocalizedBrandName,
        Func<int, int, int, Guid> CategoryTranslation,
        Func<int, Guid> BannerSectionId,
        Func<int, Guid> ParityTag,
        Func<int, Guid> ParityAttrDef,
        Func<int, int, Guid> ParityAttrOption,
        Func<int, Guid> ParityLocalizedAttrName,
        Func<int, Guid> ParityLocalizedTagName,
        Func<int, int, Guid> ParityLocalizedOptionName,
        Func<int, int, Guid> ParityProductTagAssignment,
        Func<int, int, Guid> ParityCategoryTagAssignment,
        Func<int, int, Guid> ParityCategoryBinding,
        Func<int, int, Guid> ParityCategoryFacet,
        Func<int, Guid> ParityMegaMenuItem,
        Func<int, Guid> ParityMegaMenuTranslation,
        Func<int, int, Guid> ParityProductAttrValue,
        Func<int, Guid> ParityVariantAxis,
        Func<int, Guid> ParityVariant,
        Func<int, Guid> ParityVariantAttrValue,
        Func<int, Guid> ParityProductHistory,
        Func<int, Guid> ParityCategorySlugHistory);

    private static readonly PackIds[] Packs =
    [
        CreateShoesPack(),
        CreatePlantsPack(),
        CreateBeautyPack(),
    ];

    public static IReadOnlyList<string> SupportedKeys { get; } =
        Packs.Select(p => p.Key).ToArray();

    private static PackIds? FindPack(string templateKey) =>
        Packs.FirstOrDefault(p => p.Key == templateKey);

    public static string OriginFor(string templateKey) =>
        FindPack(templateKey)?.Origin
        ?? throw new ArgumentOutOfRangeException(nameof(templateKey), templateKey, "Unknown Batch C template key.");

    public static string MediaPublicUrl(string templateKey, int index)
    {
        var pack = FindPack(templateKey)
            ?? throw new ArgumentOutOfRangeException(nameof(templateKey), templateKey, "Unknown Batch C template key.");
        var n = ((index - 1) % 8) + 1;
        return $"/images/{pack.MediaFolder}/{n}.jpg";
    }

    public static string? TryResolveMedia(string templateKey, Guid mediaAssetId)
    {
        var pack = FindPack(templateKey);
        if (pack is null) return null;
        for (var i = 1; i <= 8; i++)
        {
            if (mediaAssetId == pack.MediaAsset(i))
            {
                return MediaPublicUrl(templateKey, i);
            }
        }

        return null;
    }

    public static async Task ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var catalog = provider.GetRequiredService<CatalogDbContext>();
        var now = DateTimeOffset.UtcNow;
        foreach (var pack in Packs)
        {
            if (!await catalog.StoreTemplates.AnyAsync(x => x.Key == pack.Key, cancellationToken))
            {
                await SeedCoreAsync(catalog, pack, now, cancellationToken);
            }

            await EnsureStructuralParityAsync(catalog, pack, now, cancellationToken);
        }
    }

    private static async Task SeedCoreAsync(
        CatalogDbContext catalog,
        PackIds pack,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        catalog.StoreTemplates.Add(StoreTemplate.Create(pack.Key, pack.NameFa, now, pack.TemplateId));

        for (var b = 1; b <= pack.BrandNames.Length; b++)
        {
            var brandId = pack.BrandId(b);
            catalog.TemplateBrands.Add(new TemplateBrand
            {
                BrandId = brandId,
                TemplateId = pack.TemplateId,
                SlugSeam = $"{pack.SlugPrefix}-brand-{b}",
                Status = CatalogPublicationStatus.Published,
                LogoMediaAssetId = pack.MediaAsset(((b - 1) % 8) + 1),
                CreatedAt = now,
                UpdatedAt = now,
            });
            catalog.TemplateLocalizedTexts.Add(new TemplateLocalizedText
            {
                TextId = pack.LocalizedBrandName(b),
                OwnerKind = TemplateLocalizedOwnerKind.Brand,
                OwnerId = brandId,
                FieldKey = "name",
                Locale = LocaleFa,
                Value = pack.BrandNames[b - 1]!,
            });
        }

        var leafIds = new List<Guid>();
        var fa = CultureInfo.GetCultureInfo("fa-IR");
        for (var root = 1; root <= pack.TreeRoots.Length; root++)
        {
            var rootName = pack.TreeRoots[root - 1]!;
            var rootId = pack.CategoryRoot(root);
            AddCategory(catalog, pack, rootId, null, root, 0, 0, rootName, $"{pack.SlugPrefix}-root-{root}", root, now);
            for (var mid = 1; mid <= 2; mid++)
            {
                var midId = pack.CategoryMid(root, mid);
                var midName = $"{rootName} · سطح {(mid + 1).ToString("0", fa)}";
                AddCategory(catalog, pack, midId, rootId, root, mid, 0, midName, $"{pack.SlugPrefix}-mid-{root}-{mid}", mid, now);
                for (var leaf = 1; leaf <= 2; leaf++)
                {
                    var leafId = pack.CategoryLeaf(root, mid, leaf);
                    var leafName = $"{rootName} · زیر {(mid * 2 + leaf).ToString("0", fa)}";
                    AddCategory(catalog, pack, leafId, midId, root, mid, leaf, leafName, $"{pack.SlugPrefix}-leaf-{root}-{mid}-{leaf}", leaf, now);
                    leafIds.Add(leafId);
                }
            }
        }

        for (var n = 1; n <= pack.ProductTitles.Length; n++)
        {
            var productId = pack.ProductId(n);
            var categoryId = leafIds[(n - 1) % leafIds.Count];
            var brandId = pack.BrandId(((n - 1) % pack.BrandNames.Length) + 1);
            var mediaId = pack.MediaAsset(((n - 1) % 8) + 1);
            catalog.TemplateProducts.Add(new TemplateProduct
            {
                ProductId = productId,
                TemplateId = pack.TemplateId,
                Kind = CatalogProductKind.PhysicalGood,
                Status = CatalogPublicationStatus.Published,
                BrandId = brandId,
                SlugSeam = $"{pack.SlugPrefix}-prod-{n}",
                SeoTitleSeam = pack.ProductTitles[n - 1],
                UnitOfMeasureId = CanonicalUnits.Pcs,
                QuantityDecimalPlaces = 0,
                QuantityStep = null,
                CreatedAt = now,
                UpdatedAt = now,
            });
            catalog.TemplateLocalizedTexts.Add(new TemplateLocalizedText
            {
                TextId = pack.LocalizedProductName(n),
                OwnerKind = TemplateLocalizedOwnerKind.Product,
                OwnerId = productId,
                FieldKey = "name",
                Locale = LocaleFa,
                Value = pack.ProductTitles[n - 1]!,
            });
            catalog.TemplateProductCategories.Add(new TemplateProductCategory
            {
                AssignmentId = pack.ProductCategoryAssignment(n),
                ProductId = productId,
                CategoryId = categoryId,
                Role = CatalogProductCategoryRole.Primary,
            });
            catalog.TemplateProductMediaReferences.Add(new TemplateProductMediaReference
            {
                ReferenceId = pack.ProductMediaRef(n),
                ProductId = productId,
                MediaAssetId = mediaId,
                DisplayOrder = 0,
                IsPrimary = true,
                AltText = pack.ProductTitles[n - 1],
            });
        }

        catalog.TemplateStoreLandingPages.Add(new TemplateStoreLandingPage
        {
            PageId = pack.LandingPageId,
            TemplateId = pack.TemplateId,
            Locale = "fa",
            Slug = pack.LandingSlug,
            Title = pack.LandingTitle,
            SeoTitle = pack.NameFa,
            SeoDescription = "دادهٔ Template Catalog برای پیش‌نمایش نمونه",
            TemplateKey = pack.Key,
            Status = StoreLandingPageStatus.Published,
            CreatedAt = now,
            UpdatedAt = now,
        });

        var bannerJson = JsonSerializer.Serialize(new
        {
            title = pack.BannerTitle,
            heightPreset = "Medium",
            variantKey = "TwoEqual",
            items = new object[]
            {
                new
                {
                    imageUrl = MediaPublicUrl(pack.Key, 1),
                    mediaAssetId = pack.MediaAsset(1).ToString("D"),
                    href = "#",
                    title = pack.BannerCtaA,
                    text = pack.BannerTextA,
                    ctaLabel = "مشاهده",
                    focalPointX = 0.55,
                    focalPointY = 0.42,
                },
                new
                {
                    imageUrl = MediaPublicUrl(pack.Key, 2),
                    mediaAssetId = pack.MediaAsset(2).ToString("D"),
                    href = "#",
                    title = pack.BannerCtaB,
                    text = pack.BannerTextB,
                    ctaLabel = "مشاهده",
                    focalPointX = 0.45,
                    focalPointY = 0.4,
                },
            },
        });

        catalog.TemplateStoreLandingPageSections.Add(new TemplateStoreLandingPageSection
        {
            PageSectionId = pack.BannerSectionId(1),
            PageId = pack.LandingPageId,
            SectionType = "BannerShowcase",
            SortOrder = 40,
            IsEnabled = true,
            ConfigurationJson = bannerJson,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await catalog.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureStructuralParityAsync(
        CatalogDbContext catalog,
        PackIds pack,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await catalog.TemplateAttributeDefinitions.AnyAsync(
                x => x.TemplateId == pack.TemplateId,
                cancellationToken))
        {
            return;
        }

        var axisDef = pack.ParityAttrDef(1);
        var textDef = pack.ParityAttrDef(2);
        catalog.TemplateAttributeDefinitions.Add(new TemplateAttributeDefinition
        {
            DefinitionId = axisDef,
            TemplateId = pack.TemplateId,
            Code = pack.AttrCodes[0]!,
            ValueKind = CatalogAttributeValueKind.Enumeration,
            IsVariantAxis = true,
            IsFilterable = true,
            IsComparable = true,
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = now,
        });
        catalog.TemplateAttributeDefinitions.Add(new TemplateAttributeDefinition
        {
            DefinitionId = textDef,
            TemplateId = pack.TemplateId,
            Code = pack.AttrCodes[1]!,
            ValueKind = CatalogAttributeValueKind.Text,
            IsVariantAxis = false,
            IsFilterable = true,
            IsComparable = true,
            IsActive = true,
            DisplayOrder = 2,
            CreatedAt = now,
        });
        catalog.TemplateLocalizedTexts.Add(new TemplateLocalizedText
        {
            TextId = pack.ParityLocalizedAttrName(1),
            OwnerKind = TemplateLocalizedOwnerKind.AttributeDefinition,
            OwnerId = axisDef,
            FieldKey = "name",
            Locale = LocaleFa,
            Value = pack.AttrNamesFa[0]!,
        });
        catalog.TemplateLocalizedTexts.Add(new TemplateLocalizedText
        {
            TextId = pack.ParityLocalizedAttrName(2),
            OwnerKind = TemplateLocalizedOwnerKind.AttributeDefinition,
            OwnerId = textDef,
            FieldKey = "name",
            Locale = LocaleFa,
            Value = pack.AttrNamesFa[1]!,
        });

        for (var i = 0; i < pack.OptionCodes.Length; i++)
        {
            var optionId = pack.ParityAttrOption(1, i + 1);
            catalog.TemplateAttributeOptions.Add(new TemplateAttributeOption
            {
                OptionId = optionId,
                DefinitionId = axisDef,
                Code = pack.OptionCodes[i]!,
                DisplayOrder = i + 1,
                IsActive = true,
            });
            catalog.TemplateLocalizedTexts.Add(new TemplateLocalizedText
            {
                TextId = pack.ParityLocalizedOptionName(1, i + 1),
                OwnerKind = TemplateLocalizedOwnerKind.AttributeOption,
                OwnerId = optionId,
                FieldKey = "name",
                Locale = LocaleFa,
                Value = pack.OptionNamesFa[i]!,
            });
        }

        for (var t = 1; t <= pack.TagCodes.Length; t++)
        {
            var tagId = pack.ParityTag(t);
            catalog.TemplateTags.Add(new TemplateTag
            {
                TagId = tagId,
                TemplateId = pack.TemplateId,
                Code = pack.TagCodes[t - 1]!,
                SlugSeam = pack.TagCodes[t - 1],
                Status = CatalogPublicationStatus.Published,
                CreatedAt = now,
                UpdatedAt = now,
            });
            catalog.TemplateLocalizedTexts.Add(new TemplateLocalizedText
            {
                TextId = pack.ParityLocalizedTagName(t),
                OwnerKind = TemplateLocalizedOwnerKind.Tag,
                OwnerId = tagId,
                FieldKey = "name",
                Locale = LocaleFa,
                Value = pack.TagNamesFa[t - 1]!,
            });
        }

        for (var root = 1; root <= 8; root++)
        {
            var rootId = pack.CategoryRoot(root);
            catalog.TemplateCategoryAttributeBindings.Add(new TemplateCategoryAttributeBinding
            {
                BindingId = pack.ParityCategoryBinding(root, 1),
                CategoryId = rootId,
                DefinitionId = axisDef,
                DisplayOrder = 1,
                IsRequired = false,
                IsFilterable = true,
                IsVariantAxis = true,
                IsComparable = true,
                CreatedAt = now,
            });
            catalog.TemplateCategoryFacetConfigurations.Add(new TemplateCategoryFacetConfiguration
            {
                FacetConfigurationId = pack.ParityCategoryFacet(root, 1),
                CategoryId = rootId,
                DefinitionId = axisDef,
                DisplayType = CatalogFacetDisplayType.CheckboxList,
                SortOrder = 1,
                IsVisible = true,
                IsSearchable = true,
                IsCollapsedByDefault = false,
                ShowCounts = true,
                CreatedAt = now,
            });
            catalog.TemplateCategoryTagAssignments.Add(new TemplateCategoryTagAssignment
            {
                AssignmentId = pack.ParityCategoryTagAssignment(root, 1),
                CategoryId = rootId,
                TagId = pack.ParityTag(1),
            });
            var megaId = pack.ParityMegaMenuItem(root);
            catalog.TemplateMegaMenuItems.Add(new TemplateMegaMenuItem
            {
                MegaMenuItemId = megaId,
                ItemType = CatalogMegaMenuItemType.Category,
                CategoryId = rootId,
                SortOrder = root,
                IsVisible = true,
                IsFeatured = root <= 2,
                ImageMediaAssetId = pack.MediaAsset(((root - 1) % 8) + 1),
                CreatedAt = now,
            });
            catalog.TemplateMegaMenuItemTranslations.Add(new TemplateMegaMenuItemTranslation
            {
                MegaMenuItemTranslationId = pack.ParityMegaMenuTranslation(root),
                MegaMenuItemId = megaId,
                Locale = LocaleFa,
                ShortLabel = pack.TreeRoots[root - 1],
            });
            catalog.TemplateCategorySlugHistories.Add(new TemplateCategorySlugHistory
            {
                HistoryId = pack.ParityCategorySlugHistory(root),
                CategoryId = rootId,
                Locale = LocaleFa,
                OldSlug = $"{pack.SlugPrefix}-root-{root}-legacy",
                ChangedAt = now,
            });
        }

        for (var n = 1; n <= 15; n++)
        {
            var productId = pack.ProductId(n);
            var optionCode = pack.OptionCodes[(n - 1) % pack.OptionCodes.Length]!;
            catalog.TemplateProductAttributeValues.Add(new TemplateProductAttributeValue
            {
                ValueId = pack.ParityProductAttrValue(n, 2),
                ProductId = productId,
                DefinitionId = textDef,
                CanonicalValue = pack.TextAttrValues[(n - 1) % pack.TextAttrValues.Length]!,
            });
            catalog.TemplateProductTagAssignments.Add(new TemplateProductTagAssignment
            {
                AssignmentId = pack.ParityProductTagAssignment(n, 1),
                ProductId = productId,
                TagId = pack.ParityTag(((n - 1) % 2) + 1),
            });
            catalog.TemplateProductVariantAxes.Add(new TemplateProductVariantAxis
            {
                AxisId = pack.ParityVariantAxis(n),
                ProductId = productId,
                DefinitionId = axisDef,
                DisplayOrder = 0,
            });
            var variantId = pack.ParityVariant(n);
            var fingerprint = $"{axisDef:N}={optionCode}";
            catalog.TemplateVariants.Add(new TemplateVariant
            {
                VariantId = variantId,
                ProductId = productId,
                CombinationFingerprint = fingerprint,
                Status = CatalogPublicationStatus.Published,
                SortOrder = 0,
                IsDefault = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
            catalog.TemplateVariantAttributeValues.Add(new TemplateVariantAttributeValue
            {
                ValueId = pack.ParityVariantAttrValue(n),
                VariantId = variantId,
                DefinitionId = axisDef,
                CanonicalValue = optionCode,
            });
            catalog.TemplateProductHistoryEntries.Add(new TemplateProductHistoryEntry
            {
                HistoryId = pack.ParityProductHistory(n),
                ProductId = productId,
                EventType = "product.publish",
                Section = "lifecycle",
                SummaryFa = $"انتشار محصول قالب {pack.NameFa}",
                OccurredAt = now,
                ActorDisplayName = pack.HistoryActor,
            });
        }

        await catalog.SaveChangesAsync(cancellationToken);
    }

    private static void AddCategory(
        CatalogDbContext catalog,
        PackIds pack,
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
            TemplateId = pack.TemplateId,
            ParentCategoryId = parentId,
            Status = CatalogPublicationStatus.Published,
            SortOrder = sortOrder,
            IsVisible = true,
            ImageMediaAssetId = pack.MediaAsset(((root - 1) % 8) + 1),
            CreatedAt = now,
            UpdatedAt = now,
        });
        catalog.TemplateCategoryTranslations.Add(new TemplateCategoryTranslation
        {
            TranslationId = pack.CategoryTranslation(root, mid, leaf),
            CategoryId = categoryId,
            Locale = LocaleFa,
            Name = name,
            Slug = slug,
            UpdatedAt = now,
        });
    }

    private static PackIds CreateShoesPack() => new(
        Key: ShoesTemplateCatalogIds.Key,
        Origin: "shoes-template-catalog-persisted",
        NameFa: "کفش",
        SlugPrefix: "shoes",
        MediaFolder: "template-shoes",
        LandingSlug: "shoes-template-sample",
        LandingTitle: "پیش‌نمایش قالب کفش",
        BannerTitle: "بنرهای کفش",
        BannerCtaA: "کفش ورزشی و روزمره",
        BannerTextA: "مدل‌های مردانه و زنانه برای پیاده‌روی و تمرین",
        BannerCtaB: "بوت و رسمی",
        BannerTextB: "انتخاب‌های فصلی با مراقبت از کفش",
        HistoryActor: "ShoesTemplateCatalogSeed",
        TreeRoots:
        [
            "کفش مردانه",
            "کفش زنانه",
            "کفش بچگانه",
            "کفش ورزشی",
            "کفش رسمی",
            "صندل و دمپایی",
            "بوت و نیم‌بوت",
            "لوازم مراقبت از کفش",
        ],
        ProductTitles:
        [
            "کفش ورزشی رانینگ مردانه",
            "کفش کتانی روزمره زنانه",
            "کفش بچگانه چسبی سبک",
            "کفش فوتسال سالنی",
            "کفش رسمی چرمی مردانه",
            "کفش پاشنه‌دار زنانه",
            "صندل تابستانی بنددار",
            "دمپایی راحتی خانگی",
            "بوت چرم نیم‌ساق",
            "نیم‌بوت جیر زنانه",
            "واکس کفش بی‌رنگ",
            "اسپری ضدآب کفش",
            "کفی طبی نرم",
            "بند کفش تعویضی",
            "کیف مسافرتی کفش",
        ],
        BrandNames:
        [
            "گام‌نو",
            "پاپیون",
            "اسپورت‌گام",
            "چرم‌راه",
            "صندل‌یار",
            "مراقبت‌کفش",
        ],
        AttrCodes: ["fit", "material"],
        AttrNamesFa: ["فرم پا", "جنس رویه"],
        OptionCodes: ["narrow", "regular", "wide"],
        OptionNamesFa: ["باریک", "معمولی", "پهن"],
        TagCodes: ["seasonal", "sport"],
        TagNamesFa: ["فصلی", "ورزشی"],
        TextAttrValues: ["leather", "textile", "synthetic"],
        TemplateId: ShoesTemplateCatalogIds.TemplateId,
        LandingPageId: ShoesTemplateCatalogIds.LandingPageId,
        MediaAsset: ShoesTemplateCatalogIds.MediaAsset,
        BrandId: ShoesTemplateCatalogIds.BrandId,
        CategoryRoot: ShoesTemplateCatalogIds.CategoryRoot,
        CategoryMid: ShoesTemplateCatalogIds.CategoryMid,
        CategoryLeaf: ShoesTemplateCatalogIds.CategoryLeaf,
        ProductId: ShoesTemplateCatalogIds.ProductId,
        ProductMediaRef: ShoesTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: ShoesTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: ShoesTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: ShoesTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: ShoesTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: ShoesTemplateCatalogIds.BannerSectionId,
        ParityTag: ShoesTemplateParityIds.Tag,
        ParityAttrDef: ShoesTemplateParityIds.AttributeDefinition,
        ParityAttrOption: ShoesTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: ShoesTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: ShoesTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: ShoesTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: ShoesTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: ShoesTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: ShoesTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: ShoesTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: ShoesTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: ShoesTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: ShoesTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: ShoesTemplateParityIds.VariantAxis,
        ParityVariant: ShoesTemplateParityIds.Variant,
        ParityVariantAttrValue: ShoesTemplateParityIds.VariantAttrValue,
        ParityProductHistory: ShoesTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: ShoesTemplateParityIds.CategorySlugHistory);

    private static PackIds CreatePlantsPack() => new(
        Key: PlantsTemplateCatalogIds.Key,
        Origin: "plants-template-catalog-persisted",
        NameFa: "گل و گیاه",
        SlugPrefix: "plants",
        MediaFolder: "template-plants",
        LandingSlug: "plants-template-sample",
        LandingTitle: "پیش‌نمایش قالب گل و گیاه",
        BannerTitle: "بنرهای گل و گیاه",
        BannerCtaA: "گیاهان آپارتمانی تازه",
        BannerTextA: "سبز کردن فضا با گیاهان مقاوم و آسان‌نگهداری",
        BannerCtaB: "گلدان و خاک",
        BannerTextB: "ابزار باغبانی و باکس هدیه گل",
        HistoryActor: "PlantsTemplateCatalogSeed",
        TreeRoots:
        [
            "گیاهان آپارتمانی",
            "گل‌های شاخه‌ای",
            "کاکتوس و ساکولنت",
            "گیاهان فضای باز",
            "گلدان",
            "خاک و کود",
            "ابزار باغبانی",
            "هدیه و باکس گل",
        ],
        ProductTitles:
        [
            "سانسوریا برگ‌شمشیری",
            "پوتوس سبز رونده",
            "رز شاخه‌ای قرمز",
            "لیسیانتوس سفید",
            "کاکتوس گلدان کوچک",
            "ساکولنت مخلوط سینی",
            "شمعدانی فضای باز",
            "پیچک بالکنی",
            "گلدان سرامیکی مات",
            "گلدان پلاستیکی زهکشی‌دار",
            "خاک برگ غنی‌شده",
            "کود مایع گیاهان آپارتمانی",
            "قیچی هرس باغبانی",
            "آبپاش دستی یک‌لیتری",
            "باکس هدیه گل فصلی",
        ],
        BrandNames:
        [
            "سبزخانه",
            "گل‌سرا",
            "کاکتوس‌نو",
            "خاک‌یار",
            "باغچه‌بان",
            "باکس‌گل",
        ],
        AttrCodes: ["light", "pot-size"],
        AttrNamesFa: ["نیاز نوری", "اندازه گلدان"],
        OptionCodes: ["low-light", "bright", "outdoor"],
        OptionNamesFa: ["کم‌نور", "پرنور", "فضای باز"],
        TagCodes: ["indoor", "gift"],
        TagNamesFa: ["آپارتمانی", "هدیه"],
        TextAttrValues: ["easy-care", "seasonal", "decorative"],
        TemplateId: PlantsTemplateCatalogIds.TemplateId,
        LandingPageId: PlantsTemplateCatalogIds.LandingPageId,
        MediaAsset: PlantsTemplateCatalogIds.MediaAsset,
        BrandId: PlantsTemplateCatalogIds.BrandId,
        CategoryRoot: PlantsTemplateCatalogIds.CategoryRoot,
        CategoryMid: PlantsTemplateCatalogIds.CategoryMid,
        CategoryLeaf: PlantsTemplateCatalogIds.CategoryLeaf,
        ProductId: PlantsTemplateCatalogIds.ProductId,
        ProductMediaRef: PlantsTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: PlantsTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: PlantsTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: PlantsTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: PlantsTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: PlantsTemplateCatalogIds.BannerSectionId,
        ParityTag: PlantsTemplateParityIds.Tag,
        ParityAttrDef: PlantsTemplateParityIds.AttributeDefinition,
        ParityAttrOption: PlantsTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: PlantsTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: PlantsTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: PlantsTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: PlantsTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: PlantsTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: PlantsTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: PlantsTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: PlantsTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: PlantsTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: PlantsTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: PlantsTemplateParityIds.VariantAxis,
        ParityVariant: PlantsTemplateParityIds.Variant,
        ParityVariantAttrValue: PlantsTemplateParityIds.VariantAttrValue,
        ParityProductHistory: PlantsTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: PlantsTemplateParityIds.CategorySlugHistory);

    private static PackIds CreateBeautyPack() => new(
        Key: BeautyTemplateCatalogIds.Key,
        Origin: "beauty-template-catalog-persisted",
        NameFa: "آرایشی بهداشتی",
        SlugPrefix: "beauty",
        MediaFolder: "template-beauty",
        LandingSlug: "beauty-template-sample",
        LandingTitle: "پیش‌نمایش قالب آرایشی بهداشتی",
        BannerTitle: "بنرهای آرایشی بهداشتی",
        BannerCtaA: "مراقبت پوست روزانه",
        BannerTextA: "پاک‌کننده، مرطوب‌کننده و روتین فروشگاهی ساده",
        BannerCtaB: "آرایش و عطر",
        BannerTextB: "محصولات صورت، چشم، لب و بهداشت شخصی",
        HistoryActor: "BeautyTemplateCatalogSeed",
        TreeRoots:
        [
            "مراقبت پوست",
            "مراقبت مو",
            "آرایش صورت",
            "آرایش چشم",
            "آرایش لب",
            "عطر و خوشبوکننده",
            "بهداشت شخصی",
            "ابزار و اکسسوری آرایشی",
        ],
        ProductTitles:
        [
            "ژل شستشوی صورت ملایم",
            "کرم مرطوب‌کننده روزانه",
            "شامپو مراقبت مو معمولی",
            "نرم‌کننده مو سبک",
            "کرم پودر سبک مات",
            "پنکک فشرده فروشگاهی",
            "ریمل حجم‌دهنده",
            "سایه چشم پالت چهاررنگ",
            "رژ لب مات بلندمدت",
            "بالم لب مرطوب‌کننده",
            "ادکلن اسپری روزانه",
            "بادی میست میوه‌ای",
            "ژل دوش ملایم",
            "دئودورانت رولی",
            "براش آرایشی مجموعهٔ پایه",
        ],
        BrandNames:
        [
            "زیبایی‌نو",
            "موی‌آروم",
            "رنگ‌چهره",
            "چشم‌نما",
            "عطرخانه",
            "بهداشت‌روز",
        ],
        AttrCodes: ["finish", "skin-feel"],
        AttrNamesFa: ["پرداخت ظاهری", "حس روی پوست"],
        OptionCodes: ["matte", "glow", "sheer"],
        OptionNamesFa: ["مات", "درخشان", "شفاف"],
        TagCodes: ["daily-care", "makeup"],
        TagNamesFa: ["مراقبت روزانه", "آرایش"],
        TextAttrValues: ["cream", "liquid", "powder"],
        TemplateId: BeautyTemplateCatalogIds.TemplateId,
        LandingPageId: BeautyTemplateCatalogIds.LandingPageId,
        MediaAsset: BeautyTemplateCatalogIds.MediaAsset,
        BrandId: BeautyTemplateCatalogIds.BrandId,
        CategoryRoot: BeautyTemplateCatalogIds.CategoryRoot,
        CategoryMid: BeautyTemplateCatalogIds.CategoryMid,
        CategoryLeaf: BeautyTemplateCatalogIds.CategoryLeaf,
        ProductId: BeautyTemplateCatalogIds.ProductId,
        ProductMediaRef: BeautyTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: BeautyTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: BeautyTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: BeautyTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: BeautyTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: BeautyTemplateCatalogIds.BannerSectionId,
        ParityTag: BeautyTemplateParityIds.Tag,
        ParityAttrDef: BeautyTemplateParityIds.AttributeDefinition,
        ParityAttrOption: BeautyTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: BeautyTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: BeautyTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: BeautyTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: BeautyTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: BeautyTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: BeautyTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: BeautyTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: BeautyTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: BeautyTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: BeautyTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: BeautyTemplateParityIds.VariantAxis,
        ParityVariant: BeautyTemplateParityIds.Variant,
        ParityVariantAttrValue: BeautyTemplateParityIds.VariantAttrValue,
        ParityProductHistory: BeautyTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: BeautyTemplateParityIds.CategorySlugHistory);
}
/// <summary>اعمال دانه Batch C روی tenant توسعه.</summary>
internal static class IndustryBatchCTemplateCatalogSeedHost
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
            "industry-batch-c-template-catalog-seed"));
        await IndustryBatchCTemplateCatalogSeed.ApplyAsync(provider);
    }
}



