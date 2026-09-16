using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>
/// Batch A industry Template Catalog seeds (TB-P10-T022-R12A).
/// Exact Fashion architecture/persistence conventions — isolated ID namespaces + media paths.
/// </summary>
internal static class IndustryBatchATemplateCatalogSeed
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
        CreateAutoPartsPack(),
        CreateBuildingMaterialsPack(),
        CreateToolsHardwarePack(),
    ];

    public static IReadOnlyList<string> SupportedKeys { get; } =
        Packs.Select(p => p.Key).ToArray();

    private static PackIds? FindPack(string templateKey) =>
        Packs.FirstOrDefault(p => p.Key == templateKey);

    public static string OriginFor(string templateKey) =>
        FindPack(templateKey)?.Origin
        ?? throw new ArgumentOutOfRangeException(nameof(templateKey), templateKey, "Unknown Batch A template key.");

    public static string MediaPublicUrl(string templateKey, int index)
    {
        var pack = FindPack(templateKey)
            ?? throw new ArgumentOutOfRangeException(nameof(templateKey), templateKey, "Unknown Batch A template key.");
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

    private static PackIds CreateAutoPartsPack() => new(
        Key: AutoPartsTemplateCatalogIds.Key,
        Origin: "auto-parts-template-catalog-persisted",
        NameFa: "لوازم یدکی خودرو",
        SlugPrefix: "auto-parts",
        MediaFolder: "template-auto-parts",
        LandingSlug: "auto-parts-template-sample",
        LandingTitle: "پیش‌نمایش قالب لوازم یدکی خودرو",
        BannerTitle: "بنرهای لوازم یدکی",
        BannerCtaA: "قطعات موتوری منتخب",
        BannerTextA: "فیلتر، شمع و قطعات مصرفی خودرو",
        BannerCtaB: "ترمز و جلوبندی",
        BannerTextB: "ایمنی مسیر با قطعات استاندارد",
        HistoryActor: "AutoPartsTemplateCatalogSeed",
        TreeRoots:
        [
            "موتور و قطعات موتوری",
            "جلوبندی و تعلیق",
            "ترمز",
            "برق و الکترونیک خودرو",
            "فیلترها و مصرفی",
            "سیستم خنک‌کننده",
            "بدنه و تزئینات",
            "انتقال قدرت",
        ],
        ProductTitles:
        [
            "فیلتر روغن موتور استاندارد",
            "شمع ایریدیوم چهارتایی",
            "لنت ترمز جلو سرامیکی",
            "دیسک ترمز خنک‌شونده",
            "کمک‌فنر جلو گازی",
            "سیبک فرمان چپ",
            "باتری اتمی ۷۴ آمپر",
            "دینام دینامو ۱۲ ولت",
            "فیلتر هوای کابین",
            "ترموستات ۸۲ درجه",
            "رادیاتور آلومینیومی",
            "آینه بغل برقی راست",
            "کیت کلاچ کامل",
            "تسمه تایمینگ تقویت‌شده",
            "روغن موتور ۵W-۳۰ سنتتیک",
        ],
        BrandNames:
        [
            "پارت‌تک",
            "ایمن‌چرخ",
            "موتورپارس",
            "فیلترنو",
            "برق‌خودرو",
            "جلوبندکار",
        ],
        AttrCodes: ["fitment", "material"],
        AttrNamesFa: ["سازگاری خودرو", "جنس"],
        OptionCodes: ["sedan", "suv", "pickup"],
        OptionNamesFa: ["سدان", "شاسی‌بلند", "وانت"],
        TagCodes: ["oem-fit", "consumable"],
        TagNamesFa: ["سازگار OEM", "مصرفی"],
        TextAttrValues: ["steel", "ceramic", "aluminum"],
        TemplateId: AutoPartsTemplateCatalogIds.TemplateId,
        LandingPageId: AutoPartsTemplateCatalogIds.LandingPageId,
        MediaAsset: AutoPartsTemplateCatalogIds.MediaAsset,
        BrandId: AutoPartsTemplateCatalogIds.BrandId,
        CategoryRoot: AutoPartsTemplateCatalogIds.CategoryRoot,
        CategoryMid: AutoPartsTemplateCatalogIds.CategoryMid,
        CategoryLeaf: AutoPartsTemplateCatalogIds.CategoryLeaf,
        ProductId: AutoPartsTemplateCatalogIds.ProductId,
        ProductMediaRef: AutoPartsTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: AutoPartsTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: AutoPartsTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: AutoPartsTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: AutoPartsTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: AutoPartsTemplateCatalogIds.BannerSectionId,
        ParityTag: AutoPartsTemplateParityIds.Tag,
        ParityAttrDef: AutoPartsTemplateParityIds.AttributeDefinition,
        ParityAttrOption: AutoPartsTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: AutoPartsTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: AutoPartsTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: AutoPartsTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: AutoPartsTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: AutoPartsTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: AutoPartsTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: AutoPartsTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: AutoPartsTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: AutoPartsTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: AutoPartsTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: AutoPartsTemplateParityIds.VariantAxis,
        ParityVariant: AutoPartsTemplateParityIds.Variant,
        ParityVariantAttrValue: AutoPartsTemplateParityIds.VariantAttrValue,
        ParityProductHistory: AutoPartsTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: AutoPartsTemplateParityIds.CategorySlugHistory);

    private static PackIds CreateBuildingMaterialsPack() => new(
        Key: BuildingMaterialsTemplateCatalogIds.Key,
        Origin: "building-materials-template-catalog-persisted",
        NameFa: "لوازم ساختمانی",
        SlugPrefix: "building-materials",
        MediaFolder: "template-building-materials",
        LandingSlug: "building-materials-template-sample",
        LandingTitle: "پیش‌نمایش قالب لوازم ساختمانی",
        BannerTitle: "بنرهای مصالح ساختمانی",
        BannerCtaA: "سیمان و ملات آماده",
        BannerTextA: "تأمین مصالح پایه برای پروژهٔ ساختمانی",
        BannerCtaB: "عایق و اتصالات",
        BannerTextB: "لوله، در و پوشش سقف استاندارد",
        HistoryActor: "BuildingMaterialsTemplateCatalogSeed",
        TreeRoots:
        [
            "مصالح پایه",
            "سیمان و ملات",
            "عایق",
            "لوله و اتصالات",
            "در و پنجره",
            "سقف و پوشش",
            "چسب و مواد شیمیایی ساختمان",
            "تجهیزات کارگاهی ساختمانی",
        ],
        ProductTitles:
        [
            "سیمان پرتلند تیپ ۲ پاکتی",
            "ماسه شسته دانه‌بندی‌شده",
            "ملات آماده کاشی",
            "عایق رطوبتی رولی",
            "پشم سنگ تخته‌ای",
            "لوله PVC فشارقوی",
            "اتصالات برنجی نیم‌اینچ",
            "درب ضدسرقت روکش‌دار",
            "پنجره دوجداره UPVC",
            "ورق گالوانیزه سقف",
            "تایل سفالی شیروانی",
            "چسب کاشی پودری",
            "رنگ نمای اکریلیک",
            "داربست مدولار سبک",
            "فرغون فلزی ساختمانی",
        ],
        BrandNames:
        [
            "بتن‌یار",
            "عایق‌بان",
            "لوله‌ساز",
            "درنمای",
            "سقف‌پوش",
            "چسب‌کار",
        ],
        AttrCodes: ["grade", "finish"],
        AttrNamesFa: ["گرید", "پرداخت"],
        OptionCodes: ["standard", "premium", "heavy-duty"],
        OptionNamesFa: ["استاندارد", "پرمیوم", "سنگین‌کار"],
        TagCodes: ["bulk", "project"],
        TagNamesFa: ["عمده", "پروژه‌ای"],
        TextAttrValues: ["cement", "metal", "polymer"],
        TemplateId: BuildingMaterialsTemplateCatalogIds.TemplateId,
        LandingPageId: BuildingMaterialsTemplateCatalogIds.LandingPageId,
        MediaAsset: BuildingMaterialsTemplateCatalogIds.MediaAsset,
        BrandId: BuildingMaterialsTemplateCatalogIds.BrandId,
        CategoryRoot: BuildingMaterialsTemplateCatalogIds.CategoryRoot,
        CategoryMid: BuildingMaterialsTemplateCatalogIds.CategoryMid,
        CategoryLeaf: BuildingMaterialsTemplateCatalogIds.CategoryLeaf,
        ProductId: BuildingMaterialsTemplateCatalogIds.ProductId,
        ProductMediaRef: BuildingMaterialsTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: BuildingMaterialsTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: BuildingMaterialsTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: BuildingMaterialsTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: BuildingMaterialsTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: BuildingMaterialsTemplateCatalogIds.BannerSectionId,
        ParityTag: BuildingMaterialsTemplateParityIds.Tag,
        ParityAttrDef: BuildingMaterialsTemplateParityIds.AttributeDefinition,
        ParityAttrOption: BuildingMaterialsTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: BuildingMaterialsTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: BuildingMaterialsTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: BuildingMaterialsTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: BuildingMaterialsTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: BuildingMaterialsTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: BuildingMaterialsTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: BuildingMaterialsTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: BuildingMaterialsTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: BuildingMaterialsTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: BuildingMaterialsTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: BuildingMaterialsTemplateParityIds.VariantAxis,
        ParityVariant: BuildingMaterialsTemplateParityIds.Variant,
        ParityVariantAttrValue: BuildingMaterialsTemplateParityIds.VariantAttrValue,
        ParityProductHistory: BuildingMaterialsTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: BuildingMaterialsTemplateParityIds.CategorySlugHistory);

    private static PackIds CreateToolsHardwarePack() => new(
        Key: ToolsHardwareTemplateCatalogIds.Key,
        Origin: "tools-hardware-template-catalog-persisted",
        NameFa: "ابزار و یراق",
        SlugPrefix: "tools-hardware",
        MediaFolder: "template-tools-hardware",
        LandingSlug: "tools-hardware-template-sample",
        LandingTitle: "پیش‌نمایش قالب ابزار و یراق",
        BannerTitle: "بنرهای ابزار و یراق",
        BannerCtaA: "ابزار برقی کارگاهی",
        BannerTextA: "دریل، فرز و تجهیزات اندازه‌گیری",
        BannerCtaB: "یراق و پیچ و مهره",
        BannerTextB: "ایمنی و ابزار دستی حرفه‌ای",
        HistoryActor: "ToolsHardwareTemplateCatalogSeed",
        TreeRoots:
        [
            "ابزار برقی",
            "ابزار دستی",
            "ابزار اندازه‌گیری",
            "یراق‌آلات",
            "پیچ و مهره",
            "ابزار برش",
            "تجهیزات ایمنی",
            "ابزار کارگاهی",
        ],
        ProductTitles:
        [
            "دریل چکشی ۱۸ ولتی",
            "فرز انگشتی دور متغیر",
            "آچار فرانسه ۱۰ اینچی",
            "مجموعه پیچ‌گوشتی مغناطیسی",
            "متر لیزری ۳۰ متری",
            "تراز لیزری خطی",
            "لولا گازور آرام‌بند",
            "قفل کتابی فولادی",
            "پیچ خودکار ۴×۴۰",
            "مهره شش‌گوش M8",
            "تیغه اره دیسکی چوب",
            "کاتر صنعتی تیغه‌ای",
            "کلاه ایمنی ساختمانی",
            "دستکش ضدبرش سطح ۵",
            "گیره رومیزی کارگاهی",
        ],
        BrandNames:
        [
            "ابزارنو",
            "یراق‌پلاس",
            "برش‌تک",
            "ایمنی‌کار",
            "متریزان",
            "کارگاه‌یار",
        ],
        AttrCodes: ["power", "size"],
        AttrNamesFa: ["توان", "اندازه"],
        OptionCodes: ["cordless", "corded", "manual"],
        OptionNamesFa: ["شارژی", "برقی", "دستی"],
        TagCodes: ["workshop", "safety"],
        TagNamesFa: ["کارگاهی", "ایمنی"],
        TextAttrValues: ["steel", "carbide", "polymer"],
        TemplateId: ToolsHardwareTemplateCatalogIds.TemplateId,
        LandingPageId: ToolsHardwareTemplateCatalogIds.LandingPageId,
        MediaAsset: ToolsHardwareTemplateCatalogIds.MediaAsset,
        BrandId: ToolsHardwareTemplateCatalogIds.BrandId,
        CategoryRoot: ToolsHardwareTemplateCatalogIds.CategoryRoot,
        CategoryMid: ToolsHardwareTemplateCatalogIds.CategoryMid,
        CategoryLeaf: ToolsHardwareTemplateCatalogIds.CategoryLeaf,
        ProductId: ToolsHardwareTemplateCatalogIds.ProductId,
        ProductMediaRef: ToolsHardwareTemplateCatalogIds.ProductMediaRef,
        ProductCategoryAssignment: ToolsHardwareTemplateCatalogIds.ProductCategoryAssignment,
        LocalizedProductName: ToolsHardwareTemplateCatalogIds.LocalizedProductName,
        LocalizedBrandName: ToolsHardwareTemplateCatalogIds.LocalizedBrandName,
        CategoryTranslation: ToolsHardwareTemplateCatalogIds.CategoryTranslation,
        BannerSectionId: ToolsHardwareTemplateCatalogIds.BannerSectionId,
        ParityTag: ToolsHardwareTemplateParityIds.Tag,
        ParityAttrDef: ToolsHardwareTemplateParityIds.AttributeDefinition,
        ParityAttrOption: ToolsHardwareTemplateParityIds.AttributeOption,
        ParityLocalizedAttrName: ToolsHardwareTemplateParityIds.LocalizedAttrName,
        ParityLocalizedTagName: ToolsHardwareTemplateParityIds.LocalizedTagName,
        ParityLocalizedOptionName: ToolsHardwareTemplateParityIds.LocalizedOptionName,
        ParityProductTagAssignment: ToolsHardwareTemplateParityIds.ProductTagAssignment,
        ParityCategoryTagAssignment: ToolsHardwareTemplateParityIds.CategoryTagAssignment,
        ParityCategoryBinding: ToolsHardwareTemplateParityIds.CategoryBinding,
        ParityCategoryFacet: ToolsHardwareTemplateParityIds.CategoryFacet,
        ParityMegaMenuItem: ToolsHardwareTemplateParityIds.MegaMenuItem,
        ParityMegaMenuTranslation: ToolsHardwareTemplateParityIds.MegaMenuTranslation,
        ParityProductAttrValue: ToolsHardwareTemplateParityIds.ProductAttrValue,
        ParityVariantAxis: ToolsHardwareTemplateParityIds.VariantAxis,
        ParityVariant: ToolsHardwareTemplateParityIds.Variant,
        ParityVariantAttrValue: ToolsHardwareTemplateParityIds.VariantAttrValue,
        ParityProductHistory: ToolsHardwareTemplateParityIds.ProductHistory,
        ParityCategorySlugHistory: ToolsHardwareTemplateParityIds.CategorySlugHistory);
}

/// <summary>اعمال دانه Batch A روی tenant توسعه.</summary>
internal static class IndustryBatchATemplateCatalogSeedHost
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
            "industry-batch-a-template-catalog-seed"));
        await IndustryBatchATemplateCatalogSeed.ApplyAsync(provider);
    }
}
