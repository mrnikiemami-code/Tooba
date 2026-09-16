using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>
/// Batch B industry Template Catalog seeds (TB-P10-T022-R12B).
/// Exact Fashion architecture/persistence conventions — isolated ID namespaces + media paths.
/// </summary>
internal static class IndustryBatchBTemplateCatalogSeed
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
        CreateTileCeramicPack(),
        CreateInteriorDecorPack(),
        CreateHomeAppliancesPack(),
    ];

    public static IReadOnlyList<string> SupportedKeys { get; } =
        Packs.Select(p => p.Key).ToArray();

    private static PackIds? FindPack(string templateKey) =>
        Packs.FirstOrDefault(p => p.Key == templateKey);

    public static string OriginFor(string templateKey) =>
        FindPack(templateKey)?.Origin
        ?? throw new ArgumentOutOfRangeException(nameof(templateKey), templateKey, "Unknown Batch B template key.");

    public static string MediaPublicUrl(string templateKey, int index)
    {
        var pack = FindPack(templateKey)
            ?? throw new ArgumentOutOfRangeException(nameof(templateKey), templateKey, "Unknown Batch B template key.");
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

    private static PackIds CreateTileCeramicPack() => new(
        Key: TileCeramicTemplateCatalogIds.Key,
        Origin: "tile-ceramic-template-catalog-persisted",
        NameFa: "کاشی و سرامیک",
        SlugPrefix: "tile-ceramic",
        MediaFolder: "template-tile-ceramic",
        LandingSlug: "tile-ceramic-template-sample",
        LandingTitle: "پیش‌نمایش قالب کاشی و سرامیک",
        BannerTitle: "بنرهای کاشی و سرامیک",
        BannerCtaA: "کلکسیون پرسلان مات",
        BannerTextA: "کف و دیوار با طرح‌های معاصر",
        BannerCtaB: "سرویس و آشپزخانه",
        BannerTextB: "کاشی‌های مقاوم رطوبت با بندکشی هماهنگ",
        HistoryActor: "TileCeramicTemplateCatalogSeed",
        TreeRoots:
        [
            "کاشی دیوار",
            "سرامیک کف",
            "پرسلان",
            "اسلب",
            "کاشی سرویس و حمام",
            "کاشی آشپزخانه",
            "فضای بیرونی",
            "چسب، بندکشی و متعلقات",
        ],
        ProductTitles:
        [
            "کاشی دیوار براق ۳۰×۶۰",
            "سرامیک کف مات ۶۰×۶۰",
            "پرسلان طرح سنگ ۱۲۰×۶۰",
            "اسلب کلس‌ماربل ۱۶۰×۳۲۰",
            "کاشی حمام ضدلغزش",
            "کاشی بین کابینتی آشپزخانه",
            "سرامیک فضای باز ضد یخ",
            "چسب کاشی پرسلان C2",
            "بندکشی اپوکسی سفید",
            "کاشی موزاییک شیشه‌ای",
            "سرامیک طرح چوب ۲۰×۱۲۰",
            "پرسلان پولیش‌شده ۸۰×۸۰",
            "اسلب کوارتز خاکستری",
            "کاشی مترو براق ۱۰×۳۰",
            "پرایمر سطح قبل از نصب",
        ],
        BrandNames:
        [
            "کاشی‌نگار",
            "سرام‌پارس",
            "پرسلان‌نو",
            "اسلب‌لاین",
            "بندکش‌کار",
            "موزاییک‌ویو",
        ],
        AttrCodes: ["finish", "size-class"],
        AttrNamesFa: ["پرداخت سطح", "کلاس ابعاد"],
        OptionCodes: ["gloss", "matte", "textured"],
        OptionNamesFa: ["براق", "مات", "بافت‌دار"],
        TagCodes: ["collection", "wet-area"],
        TagNamesFa: ["کلکسیون", "فضای مرطوب"],
        TextAttrValues: ["porcelain", "ceramic", "slab"],
        TemplateId: TileCeramicTemplateCatalogIds.TemplateId,
        LandingPageId: TileCeramicTemplateCatalogIds.LandingPageId,
        MediaAsset: TileCeramicTemplateCatalogIds.MediaAsset,
        BrandId: TileCeramicTemplateCatalogIds.BrandId,
        CategoryRoot: TileCeramicTemplateCatalogIds.CategoryRoot,
        CategoryMid: TileCeramicTemplateCatalogIds.CategoryMid,
        CategoryLeaf: TileCeramicTemplateCatalogIds.CategoryLeaf,
        ProductId: TileCeramicTemplateCatalogIds.ProductId,
        ProductMediaRef: TileCeramicTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: TileCeramicTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: TileCeramicTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: TileCeramicTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: TileCeramicTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: TileCeramicTemplateCatalogIds.BannerSectionId,
        ParityTag: TileCeramicTemplateParityIds.Tag,
        ParityAttrDef: TileCeramicTemplateParityIds.AttributeDefinition,
        ParityAttrOption: TileCeramicTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: TileCeramicTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: TileCeramicTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: TileCeramicTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: TileCeramicTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: TileCeramicTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: TileCeramicTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: TileCeramicTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: TileCeramicTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: TileCeramicTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: TileCeramicTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: TileCeramicTemplateParityIds.VariantAxis,
        ParityVariant: TileCeramicTemplateParityIds.Variant,
        ParityVariantAttrValue: TileCeramicTemplateParityIds.VariantAttrValue,
        ParityProductHistory: TileCeramicTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: TileCeramicTemplateParityIds.CategorySlugHistory);

    private static PackIds CreateInteriorDecorPack() => new(
        Key: InteriorDecorTemplateCatalogIds.Key,
        Origin: "interior-decor-template-catalog-persisted",
        NameFa: "دکوراسیون داخلی",
        SlugPrefix: "interior-decor",
        MediaFolder: "template-interior-decor",
        LandingSlug: "interior-decor-template-sample",
        LandingTitle: "پیش‌نمایش قالب دکوراسیون داخلی",
        BannerTitle: "بنرهای دکوراسیون داخلی",
        BannerCtaA: "مبل و نشیمن منتخب",
        BannerTextA: "چیدمان آرام با پارچه و چوب طبیعی",
        BannerCtaB: "فرش و تابلوفرش",
        BannerTextB: "فضای گرم با منسوجات و دیوارکوب",
        HistoryActor: "InteriorDecorTemplateCatalogSeed",
        TreeRoots:
        [
            "مبلمان",
            "تخت و سرویس خواب",
            "فرش",
            "تابلوفرش و دیوارکوب",
            "میز و صندلی",
            "روشنایی دکوراتیو",
            "پرده و منسوجات",
            "اکسسوری و دکور",
        ],
        ProductTitles:
        [
            "مبل راحتی سه‌نفره مخمل",
            "کاناپه ال‌شکل پارچه‌ای",
            "تخت دو نفره چوبی",
            "سرویس خواب پنج‌تکه",
            "فرش دستباف طرح هندسی",
            "فرش ماشینی شگی",
            "تابلوفرش دیواری ابریشمی",
            "دیوارکوب چوبی مینیمال",
            "میز ناهارخوری شش‌نفره",
            "صندلی راحتی دسته‌دار",
            "آباژور پایه‌بلند فلزی",
            "لوستر حلقه‌ای مدرن",
            "پرده حریر دولایه",
            "کوسن مخمل دکوراتیو",
            "آینه قدی قاب‌دار",
        ],
        BrandNames:
        [
            "خانهٔ چوب",
            "نشیمن‌نو",
            "فرش‌سرا",
            "نورخانه",
            "منسوج‌آرا",
            "دکورلاین",
        ],
        AttrCodes: ["material", "room"],
        AttrNamesFa: ["جنس", "فضا"],
        OptionCodes: ["living", "bedroom", "dining"],
        OptionNamesFa: ["نشیمن", "خواب", "ناهارخوری"],
        TagCodes: ["soft-furnish", "statement"],
        TagNamesFa: ["منسوجات", "نقطهٔ کانونی"],
        TextAttrValues: ["wood", "fabric", "metal"],
        TemplateId: InteriorDecorTemplateCatalogIds.TemplateId,
        LandingPageId: InteriorDecorTemplateCatalogIds.LandingPageId,
        MediaAsset: InteriorDecorTemplateCatalogIds.MediaAsset,
        BrandId: InteriorDecorTemplateCatalogIds.BrandId,
        CategoryRoot: InteriorDecorTemplateCatalogIds.CategoryRoot,
        CategoryMid: InteriorDecorTemplateCatalogIds.CategoryMid,
        CategoryLeaf: InteriorDecorTemplateCatalogIds.CategoryLeaf,
        ProductId: InteriorDecorTemplateCatalogIds.ProductId,
        ProductMediaRef: InteriorDecorTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: InteriorDecorTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: InteriorDecorTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: InteriorDecorTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: InteriorDecorTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: InteriorDecorTemplateCatalogIds.BannerSectionId,
        ParityTag: InteriorDecorTemplateParityIds.Tag,
        ParityAttrDef: InteriorDecorTemplateParityIds.AttributeDefinition,
        ParityAttrOption: InteriorDecorTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: InteriorDecorTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: InteriorDecorTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: InteriorDecorTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: InteriorDecorTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: InteriorDecorTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: InteriorDecorTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: InteriorDecorTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: InteriorDecorTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: InteriorDecorTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: InteriorDecorTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: InteriorDecorTemplateParityIds.VariantAxis,
        ParityVariant: InteriorDecorTemplateParityIds.Variant,
        ParityVariantAttrValue: InteriorDecorTemplateParityIds.VariantAttrValue,
        ParityProductHistory: InteriorDecorTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: InteriorDecorTemplateParityIds.CategorySlugHistory);

    private static PackIds CreateHomeAppliancesPack() => new(
        Key: HomeAppliancesTemplateCatalogIds.Key,
        Origin: "home-appliances-template-catalog-persisted",
        NameFa: "لوازم خانگی",
        SlugPrefix: "home-appliances",
        MediaFolder: "template-home-appliances",
        LandingSlug: "home-appliances-template-sample",
        LandingTitle: "پیش‌نمایش قالب لوازم خانگی",
        BannerTitle: "بنرهای لوازم خانگی",
        BannerCtaA: "یخچال و شستشو",
        BannerTextA: "برندمحور با مقایسهٔ کاربردی",
        BannerCtaB: "آشپزخانه و نظافت",
        BannerTextB: "پخت‌و‌پز، جارو و لوازم کوچک خانگی",
        HistoryActor: "HomeAppliancesTemplateCatalogSeed",
        TreeRoots:
        [
            "یخچال و فریزر",
            "ماشین لباسشویی و ظرفشویی",
            "پخت و پز",
            "جارو و نظافت",
            "تهویه و سرمایش",
            "لوازم برقی آشپزخانه",
            "صوتی و تصویری خانگی",
            "لوازم کوچک خانگی",
        ],
        ProductTitles:
        [
            "یخچال سایدبای‌ساید اینورتر",
            "فریزر ایستاده کم‌مصرف",
            "ماشین لباسشویی ۹ کیلویی",
            "ماشین ظرفشویی ۱۴ نفره",
            "اجاق‌گاز رومیزی پنج‌شعله",
            "فر توکار برقی",
            "جاروبرقی کیسه‌ای ۲۲۰۰ وات",
            "جارو رباتیک هوشمند",
            "کولر گازی ۱۲ هزار BTU",
            "پنکه پایه‌بلند بی‌صدا",
            "مخلوط‌کن بلندر حرفه‌ای",
            "مایکروویو دیجیتال ۳۰ لیتر",
            "تلویزیون LED ۵۵ اینچ",
            "ساندبار خانگی ۲٫۱",
            "اتو بخار مخزن‌دار",
        ],
        BrandNames:
        [
            "خانهٔ سرد",
            "شست‌نو",
            "پخت‌یار",
            "نظافت‌تک",
            "تهویه‌بان",
            "صوتی‌خانه",
        ],
        AttrCodes: ["energy", "capacity"],
        AttrNamesFa: ["برچسب انرژی", "ظرفیت"],
        OptionCodes: ["compact", "standard", "family"],
        OptionNamesFa: ["جمع‌وجور", "استاندارد", "خانوادگی"],
        TagCodes: ["inverter", "smart-home"],
        TagNamesFa: ["اینورتر", "خانه هوشمند"],
        TextAttrValues: ["A+++", "A++", "A+"],
        TemplateId: HomeAppliancesTemplateCatalogIds.TemplateId,
        LandingPageId: HomeAppliancesTemplateCatalogIds.LandingPageId,
        MediaAsset: HomeAppliancesTemplateCatalogIds.MediaAsset,
        BrandId: HomeAppliancesTemplateCatalogIds.BrandId,
        CategoryRoot: HomeAppliancesTemplateCatalogIds.CategoryRoot,
        CategoryMid: HomeAppliancesTemplateCatalogIds.CategoryMid,
        CategoryLeaf: HomeAppliancesTemplateCatalogIds.CategoryLeaf,
        ProductId: HomeAppliancesTemplateCatalogIds.ProductId,
        ProductMediaRef: HomeAppliancesTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: HomeAppliancesTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: HomeAppliancesTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: HomeAppliancesTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: HomeAppliancesTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: HomeAppliancesTemplateCatalogIds.BannerSectionId,
        ParityTag: HomeAppliancesTemplateParityIds.Tag,
        ParityAttrDef: HomeAppliancesTemplateParityIds.AttributeDefinition,
        ParityAttrOption: HomeAppliancesTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: HomeAppliancesTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: HomeAppliancesTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: HomeAppliancesTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: HomeAppliancesTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: HomeAppliancesTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: HomeAppliancesTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: HomeAppliancesTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: HomeAppliancesTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: HomeAppliancesTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: HomeAppliancesTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: HomeAppliancesTemplateParityIds.VariantAxis,
        ParityVariant: HomeAppliancesTemplateParityIds.Variant,
        ParityVariantAttrValue: HomeAppliancesTemplateParityIds.VariantAttrValue,
        ParityProductHistory: HomeAppliancesTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: HomeAppliancesTemplateParityIds.CategorySlugHistory);
}

/// <summary>اعمال دانه Batch B روی tenant توسعه.</summary>
internal static class IndustryBatchBTemplateCatalogSeedHost
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
            "industry-batch-b-template-catalog-seed"));
        await IndustryBatchBTemplateCatalogSeed.ApplyAsync(provider);
    }
}

