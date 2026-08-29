using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Pricing.Application;
using Tooba.ProductQnA.Infrastructure;
using Tooba.Content.Infrastructure;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Host.Storefront;

/// <summary>
/// جمع‌بندی شمارشی دانهٔ نمایشی فروشگاه برای شواهد و تست.
/// این جمع‌بندی گزارش وضعیت است و منبع حقیقت تجاری نیست؛ قیمت و موجودی در آن نگه‌داری نمی‌شود.
/// </summary>
/// <param name="TopLevelCategories">تعداد ردهٔ ریشهٔ منتشرشده که در Mega Menu قابل انتخاب است.</param>
/// <param name="ChildCategories">تعداد ردهٔ سطح دوم منتشرشده.</param>
/// <param name="ThirdLevelCategories">تعداد ردهٔ سطح سوم منتشرشده.</param>
/// <param name="PublishedProducts">تعداد محصول منتشرشدهٔ Catalog؛ نه تعداد Offer.</param>
/// <param name="PublishedBrands">تعداد برند منتشرشدهٔ تحریری.</param>
/// <param name="Offers">تعداد Offer قطعی که دانه برای این ماتریس ایجاد می‌کند.</param>
/// <param name="AlreadySeeded">اگر true باشد اجرای جاری چیزی ننوشته و فقط وضعیت موجود را گزارش کرده است.</param>
internal sealed record StorefrontDemoSeedSummary(
    int TopLevelCategories,
    int ChildCategories,
    int ThirdLevelCategories,
    int PublishedProducts,
    int PublishedBrands,
    int Offers,
    bool AlreadySeeded);

/// <summary>
/// دانهٔ نمایشی Development برای عمق واقعی درخت رده، برند و کارت محصول فروشگاه.
/// این دانه فقط در محیط Development اجرا می‌شود و معنای bootstrap تولیدی را عوض نمی‌کند.
/// همهٔ نوشتن‌ها از قرارداد دایرکتوری ماژول مالک انجام می‌شود: Catalog محصول توصیفی،
/// Offer عرضهٔ فروشنده، Pricing مبلغ روی OfferId و Inventory موجودی روی OfferId.
/// Product هیچ‌گاه قیمت یا موجودی نمی‌گیرد و هیچ JOIN بین schemaهای ماژول نوشته نمی‌شود.
/// </summary>
internal static class StorefrontDemoCatalogBootstrap
{
    /// <summary>
    /// slug نگهبان idempotency. تا وقتی این محصول در Catalog باشد، دانه دوباره نوشته نمی‌شود
    /// تا راه‌اندازی مکرر Development دادهٔ تکراری نسازد.
    /// </summary>
    internal const string SentinelProductSlug = "demo-mobile-1";

    /// <summary>
    /// بازار و کانال قطعی دانهٔ نمایشی؛ فروشگاه عمومی با همین ترکیب خوانده می‌شود.
    /// </summary>
    private const string DemoMarket = "IR";

    /// <summary>
    /// شروع اعتبار قیمت‌های نمایشی. مقدار ثابت است تا شکل دانه به ساعت اجرا وابسته نشود.
    /// </summary>
    private static readonly DateTimeOffset PriceValidFrom = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// شناسه‌های مات رسانهٔ جانشین. دانه فایل واقعی نمی‌سازد و ادعای دارایی اختصاصی تولید نمی‌کند.
    /// </summary>
    private static readonly Guid[] PlaceholderMedia =
    [
        Guid.Parse("d0d0d0d0-0001-4000-8000-000000000001"),
        Guid.Parse("d0d0d0d0-0002-4000-8000-000000000002"),
        Guid.Parse("d0d0d0d0-0003-4000-8000-000000000003"),
        Guid.Parse("d0d0d0d0-0004-4000-8000-000000000004"),
    ];

    /// <summary>
    /// schemaهای Development را فرض‌گرفته و دانهٔ نمایشی را روی Tenant توسعه اجرا می‌کند.
    /// در Production صدا زده نمی‌شود و پس از bootstrap اصلی Development اجرا می‌شود.
    /// </summary>
    /// <param name="services">ریشهٔ سرویس برنامه؛ scope مستقل ساخته می‌شود تا DbContext درخواستی آلوده نشود.</param>
    /// <returns>جمع‌بندی شمارشی دانه برای شواهد.</returns>
    /// <exception cref="InvalidOperationException">اگر Tenant توسعه فعال نباشد، دانه fail-closed می‌شود.</exception>
    public static async Task<StorefrontDemoSeedSummary> ApplyAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
        {
            throw new InvalidOperationException("Storefront demo seed requires Active tenant store-alpha.");
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
            "storefront-demo-seed"));

        var summary = await SeedAsync(
            provider.GetRequiredService<CatalogDbContext>(),
            provider.GetRequiredService<ICatalogDirectory>(),
            provider.GetRequiredService<IPartyDirectory>(),
            provider.GetRequiredService<IOfferDirectory>(),
            provider.GetRequiredService<IPriceDirectory>(),
            provider.GetRequiredService<IInventoryDirectory>(),
            provider.GetRequiredService<ITaxDirectory>(),
            provider.GetRequiredService<TaxDbContext>(),
            CancellationToken.None);
        await EnsureDemoTaxCoverageAsync(
            provider.GetRequiredService<OfferDbContext>(),
            provider.GetRequiredService<TaxDbContext>(),
            provider.GetRequiredService<ITaxDirectory>(),
            CancellationToken.None);
        // پرسش‌وپاسخ نمایشی پس از وجود demo-mobile-1؛ همان CommerceContext همین scope.
        await ProductQnADevelopmentSeed.ApplyAsync(provider);
        await ContentDevelopmentSeed.ApplyAsync(provider);
        return summary;
    }

    /// <summary>
    /// ماتریس نمایشی را از قرارداد ماژول‌ها می‌نویسد. اگر slug نگهبان موجود باشد هیچ نوشتنی انجام نمی‌شود،
    /// بنابراین اجرای دوباره دادهٔ تکراری نمی‌سازد. همهٔ slug، SKU و مبلغ‌ها قطعی‌اند و از تصادف یا ساعت اجرا مشتق نمی‌شوند.
    /// </summary>
    /// <param name="catalogRead">فقط برای شمارش و بررسی نگهبان در schema همان ماژول Catalog؛ نوشتن از دایرکتوری انجام می‌شود.</param>
    /// <param name="catalog">قرارداد نوشتن Catalog.</param>
    /// <param name="parties">قرارداد نوشتن Party برای سازمان فروشندهٔ نمایشی.</param>
    /// <param name="offers">قرارداد نوشتن Offer؛ هویت فروشنده اینجاست نه در Catalog.</param>
    /// <param name="prices">قرارداد نوشتن Pricing؛ مبلغ فقط با کلید OfferId نوشته می‌شود.</param>
    /// <param name="inventory">قرارداد نوشتن Inventory؛ موجودی فقط با کلید OfferId نوشته می‌شود.</param>
    /// <param name="tax">قرارداد Tax برای طبقه و قاعدهٔ نمایشی روی Offer.</param>
    /// <param name="taxDb">خواندن idempotent طبقه/قاعده در schema tax.</param>
    /// <param name="cancellationToken">توکن لغو عملیات.</param>
    /// <returns>جمع‌بندی شمارشی وضعیت پس از اجرا.</returns>
    public static async Task<StorefrontDemoSeedSummary> SeedAsync(
        CatalogDbContext catalogRead,
        ICatalogDirectory catalog,
        IPartyDirectory parties,
        IOfferDirectory offers,
        IPriceDirectory prices,
        IInventoryDirectory inventory,
        ITaxDirectory tax,
        TaxDbContext taxDb,
        CancellationToken cancellationToken)
    {
        if (await catalogRead.Products.AsNoTracking()
                .AnyAsync(product => product.SlugSeam == SentinelProductSlug, cancellationToken))
        {
            await EnrichLocalizedDescriptionsAsync(catalogRead, catalog, cancellationToken);
            await EnsureThirdLevelCategoriesAsync(catalogRead, catalog, cancellationToken);
            await EnrichBrandLogosAsync(catalogRead, cancellationToken);
            return await SummarizeAsync(catalogRead, alreadySeeded: true, cancellationToken);
        }

        var brandIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var brandIndex = 0;
        foreach (var brand in StorefrontDemoCatalogMatrix.Brands)
        {
            var reference = await catalog.CreateBrandAsync(
                brand.Slug,
                new Dictionary<string, string> { ["fa-IR"] = brand.PersianName, ["en-US"] = brand.LatinName },
                cancellationToken);
            await catalog.PublishBrandAsync(reference.BrandId, cancellationToken);
            var brandEntity = await catalogRead.Brands.SingleAsync(item => item.BrandId == reference.BrandId, cancellationToken);
            brandEntity.LogoMediaAssetId = PlaceholderMedia[brandIndex % PlaceholderMedia.Length];
            brandIndex++;
            brandIds.Add(brand.Key, reference.BrandId);
        }
        await catalogRead.SaveChangesAsync(cancellationToken);

        var packDefinitionId = await catalog.CreateAttributeDefinitionAsync(
            "demo_pack",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "بستهٔ عرضه", ["en-US"] = "Supply pack" },
            cancellationToken);
        var standardPackOptionId = await catalog.AddAttributeOptionAsync(
            packDefinitionId,
            "standard",
            new Dictionary<string, string> { ["fa-IR"] = "استاندارد", ["en-US"] = "Standard" },
            cancellationToken);
        var specialPackOptionId = await catalog.AddAttributeOptionAsync(
            packDefinitionId,
            "special",
            new Dictionary<string, string> { ["fa-IR"] = "بستهٔ ویژه", ["en-US"] = "Special pack" },
            cancellationToken);
        var originDefinitionId = await catalog.CreateAttributeDefinitionAsync(
            "demo_origin",
            CatalogAttributeValueKind.Text,
            isVariantAxis: false,
            new Dictionary<string, string> { ["fa-IR"] = "مناسب برای", ["en-US"] = "Recommended use" },
            cancellationToken);

        var sellerPartyIds = new List<Guid>();
        foreach (var seller in StorefrontDemoCatalogMatrix.Sellers)
        {
            var organization = await parties.CreateOrganizationAsync(seller.DisplayName, seller.LegalName, cancellationToken);
            sellerPartyIds.Add(organization.PartyId);
        }

        var locationIds = new List<Guid>();
        foreach (var location in StorefrontDemoCatalogMatrix.Locations)
        {
            locationIds.Add(await inventory.CreateLocationAsync(location.Code, location.Name, cancellationToken));
        }

        var offerCount = 0;
        var productOrdinal = 0;
        foreach (var family in StorefrontDemoCatalogMatrix.Families)
        {
            var familyCategory = await catalog.CreateCategoryAsync(
                null,
                new Dictionary<string, string> { ["fa-IR"] = family.Name },
                cancellationToken);
            await catalog.PublishCategoryAsync(familyCategory.CategoryId, cancellationToken);

            foreach (var child in family.Children)
            {
                var childCategory = await catalog.CreateCategoryAsync(
                    familyCategory.CategoryId,
                    new Dictionary<string, string> { ["fa-IR"] = child.Name },
                    cancellationToken);
                await catalog.PublishCategoryAsync(childCategory.CategoryId, cancellationToken);

                var leafCategories = new List<(Guid CategoryId, string Slug)>();
                for (var leafIndex = 0; leafIndex < child.Products.Count; leafIndex++)
                {
                    var spec = child.Products[leafIndex];
                    var leafCategory = await catalog.CreateCategoryAsync(
                        childCategory.CategoryId,
                        new Dictionary<string, string> { ["fa-IR"] = spec.Name },
                        cancellationToken);
                    await catalog.PublishCategoryAsync(leafCategory.CategoryId, cancellationToken);
                    leafCategories.Add((leafCategory.CategoryId, $"demo-{child.Token}-{leafIndex + 1}"));
                }

                for (var index = 0; index < child.Products.Count; index++)
                {
                    var spec = child.Products[index];
                    var slug = leafCategories[index].Slug;
                    var leafCategoryId = leafCategories[index].CategoryId;
                    var product = await catalog.CreateProductAsync(
                        CatalogProductKind.PhysicalGood,
                        slug,
                        spec.BrandKey is null ? null : brandIds[spec.BrandKey],
                        new Dictionary<string, string> { ["fa-IR"] = spec.Name },
                        cancellationToken);
                    await catalog.AssignCategoryAsync(product.ProductId, leafCategoryId, cancellationToken);
                    await catalog.UpsertProductLocalizedFieldAsync(
                        product.ProductId,
                        "short_description",
                        new Dictionary<string, string> { ["fa-IR"] = $"{spec.Name} با عرضهٔ معتبر فروشندگان توبا" },
                        cancellationToken);
                    await catalog.UpsertProductLocalizedFieldAsync(
                        product.ProductId,
                        "full_description",
                        new Dictionary<string, string>
                        {
                            ["fa-IR"] = $"{spec.Name} برای استفادهٔ روزمره انتخاب شده است. قیمت هر فروشنده و موجودی قابل فروش به‌صورت زنده از ماژول‌های مالک خوانده می‌شود."
                        },
                        cancellationToken);
                    await catalog.SetProductAttributeAsync(
                        product.ProductId,
                        originDefinitionId,
                        child.Name,
                        enumOptionId: null,
                        cancellationToken);
                    await catalog.AttachMediaReferenceAsync(
                        product.ProductId,
                        PlaceholderMedia[productOrdinal % PlaceholderMedia.Length],
                        cancellationToken);
                    await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
                        catalog, product.ProductId, $"توضیح سئو {spec.Name}", cancellationToken);
                    await catalog.PublishProductAsync(product.ProductId, cancellationToken);
                    var variant = await catalog.CreateVariantAsync(
                        product.ProductId,
                        $"DEMO-{child.Token.ToUpperInvariant()}-{index + 1}",
                        [(packDefinitionId, "ignored", standardPackOptionId)],
                        cancellationToken);

                    var amount = child.BasePrice + (index * (child.BasePrice / 10m));
                    await PublishOfferAsync(
                        offers,
                        prices,
                        inventory,
                        tax,
                        taxDb,
                        variant.VariantId,
                        sellerPartyIds[productOrdinal % sellerPartyIds.Count],
                        $"{child.Token.ToUpperInvariant()}-{index + 1}-A",
                        amount,
                        locationIds[productOrdinal % locationIds.Count],
                        6 + (productOrdinal % 5),
                        cancellationToken);
                    offerCount++;

                    if (productOrdinal < 3)
                    {
                        var specialVariant = await catalog.CreateVariantAsync(
                            product.ProductId,
                            $"DEMO-{child.Token.ToUpperInvariant()}-{index + 1}-SPECIAL",
                            [(packDefinitionId, "ignored", specialPackOptionId)],
                            cancellationToken);
                        await PublishOfferAsync(
                            offers,
                            prices,
                            inventory,
                            tax,
                            taxDb,
                            specialVariant.VariantId,
                            sellerPartyIds[productOrdinal % sellerPartyIds.Count],
                            $"{child.Token.ToUpperInvariant()}-{index + 1}-SPECIAL",
                            amount + (child.BasePrice / 25m),
                            locationIds[productOrdinal % locationIds.Count],
                            3 + productOrdinal,
                            cancellationToken);
                        offerCount++;
                    }

                    // نخستین محصول هر ردهٔ فرزند عرضهٔ فروشندهٔ دوم می‌گیرد تا رفتار Marketplace
                    // با فروشندگان متفاوت روی همان گونه قابل مشاهده باشد؛ Offer دوم هویت مستقل است.
                    if (index == 0)
                    {
                        await PublishOfferAsync(
                            offers,
                            prices,
                            inventory,
                            tax,
                            taxDb,
                            variant.VariantId,
                            sellerPartyIds[(productOrdinal + 1) % sellerPartyIds.Count],
                            $"{child.Token.ToUpperInvariant()}-{index + 1}-B",
                            amount - (child.BasePrice / 20m),
                            locationIds[(productOrdinal + 1) % locationIds.Count],
                            4,
                            cancellationToken);
                        offerCount++;
                    }

                    productOrdinal++;
                }
            }
        }

        var summary = await SummarizeAsync(catalogRead, alreadySeeded: false, cancellationToken);
        return summary with { Offers = offerCount };
    }

    /// <summary>
    /// لوگوی برندهای نمایشی را روی پایگاه Development قدیمی نیز idempotent تنظیم می‌کند.
    /// </summary>
    private static async Task EnrichBrandLogosAsync(CatalogDbContext catalogRead, CancellationToken cancellationToken)
    {
        var brands = await catalogRead.Brands.AsNoTracking()
            .Where(brand => brand.Status == CatalogPublicationStatus.Published)
            .OrderBy(brand => brand.SlugSeam)
            .ToListAsync(cancellationToken);
        var index = 0;
        foreach (var brand in brands)
        {
            if (brand.LogoMediaAssetId is not null)
            {
                index++;
                continue;
            }

            var tracked = await catalogRead.Brands.SingleAsync(item => item.BrandId == brand.BrandId, cancellationToken);
            tracked.LogoMediaAssetId = PlaceholderMedia[index % PlaceholderMedia.Length];
            index++;
        }

        await catalogRead.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// شرح‌های نسخهٔ جدید دانه را روی پایگاه Development قدیمی نیز به‌صورت upsert و بدون تکرار غنی می‌کند.
    /// </summary>
    private static async Task EnrichLocalizedDescriptionsAsync(
        CatalogDbContext catalogRead,
        ICatalogDirectory catalog,
        CancellationToken cancellationToken)
    {
        var products = await catalogRead.Products.AsNoTracking()
            .Where(product => product.SlugSeam != null && product.SlugSeam.StartsWith("demo-"))
            .ToListAsync(cancellationToken);
        var names = await catalogRead.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.Product && text.FieldKey == "name")
            .ToListAsync(cancellationToken);
        foreach (var product in products)
        {
            var name = names.FirstOrDefault(text => text.OwnerId == product.ProductId && text.Locale.StartsWith("fa"))?.Value
                ?? product.SlugSeam
                ?? "کالای نمایشی";
            await catalog.UpsertProductLocalizedFieldAsync(
                product.ProductId,
                "short_description",
                new Dictionary<string, string> { ["fa-IR"] = $"{name} با عرضهٔ معتبر فروشندگان توبا" },
                cancellationToken);
            await catalog.UpsertProductLocalizedFieldAsync(
                product.ProductId,
                "full_description",
                new Dictionary<string, string>
                {
                    ["fa-IR"] = $"{name} برای استفادهٔ روزمره انتخاب شده است. قیمت هر فروشنده و موجودی قابل فروش به‌صورت زنده از ماژول‌های مالک خوانده می‌شود."
                },
                cancellationToken);
        }
    }

    /// <summary>
    /// یک عرضهٔ کامل و قابل نمایش می‌سازد: Offer فعال، قیمت فعال در بازهٔ اعتبار، موقعیت موجودی و طبقهٔ مالیاتی.
    /// هر بخش در ماژول مالک خودش نوشته می‌شود و کلید مشترکشان فقط OfferId است.
    /// </summary>
    private static async Task PublishOfferAsync(
        IOfferDirectory offers,
        IPriceDirectory prices,
        IInventoryDirectory inventory,
        ITaxDirectory tax,
        TaxDbContext taxDb,
        Guid variantId,
        Guid sellerPartyId,
        string skuSuffix,
        decimal amount,
        Guid locationId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var offer = await offers.CreateOfferAsync(
            variantId,
            sellerPartyId,
            SalesChannel.Marketplace,
            $"DEMO-{skuSuffix}",
            cancellationToken);
        await offers.ActivateAsync(offer.OfferId, cancellationToken);

        var price = await prices.CreatePriceAsync(
            offer.OfferId,
            DemoMarket,
            SalesChannel.Marketplace,
            amount,
            "IRR",
            PriceValidFrom,
            null,
            cancellationToken);
        await prices.ActivateAsync(price.PriceId, cancellationToken);

        var stockItemId = await inventory.OpenPositionAsync(offer.OfferId, locationId, cancellationToken);
        await inventory.AdjustAsync(
            stockItemId,
            StockAdjustmentKind.Increase,
            quantity,
            "storefront-demo-seed-receipt",
            null,
            cancellationToken);

        var taxCategory = await EnsureStandardTaxCategoryAsync(tax, taxDb, cancellationToken);
        await EnsureStandardTaxRuleAsync(tax, taxDb, taxCategory.CategoryId, cancellationToken);
        await tax.AssignOfferCategoryAsync(offer.OfferId, taxCategory.CategoryId, cancellationToken);
    }

    /// <summary>
    /// برای پایگاه‌های Development که قبلاً دانه شده‌اند، طبقه/قاعدهٔ مالیاتی استاندارد و
    /// انتساب Offerهای DEMO را تضمین می‌کند تا Checkout با TAX_NO_APPLICABLE_RULE fail-closed نشود.
    /// </summary>
    private static async Task EnsureDemoTaxCoverageAsync(
        OfferDbContext offers,
        TaxDbContext taxDb,
        ITaxDirectory tax,
        CancellationToken cancellationToken)
    {
        var category = await EnsureStandardTaxCategoryAsync(tax, taxDb, cancellationToken);
        await EnsureStandardTaxRuleAsync(tax, taxDb, category.CategoryId, cancellationToken);

        var demoOfferIds = await offers.Offers.AsNoTracking()
            .Where(offer => offer.SellerSku != null && offer.SellerSku.StartsWith("DEMO-"))
            .Select(offer => offer.OfferId)
            .ToListAsync(cancellationToken);
        foreach (var offerId in demoOfferIds)
        {
            await tax.AssignOfferCategoryAsync(offerId, category.CategoryId, cancellationToken);
        }
    }

    private static async Task<TaxCategoryReference> EnsureStandardTaxCategoryAsync(
        ITaxDirectory tax,
        TaxDbContext taxDb,
        CancellationToken cancellationToken)
    {
        var existing = await taxDb.Categories.AsNoTracking()
            .FirstOrDefaultAsync(category => category.Code == "standard" || category.Code == "standard-demo", cancellationToken);
        if (existing is not null)
        {
            return new TaxCategoryReference(existing.CategoryId, existing.Code, existing.DisplayName);
        }

        return await tax.CreateCategoryAsync("standard", "استاندارد", cancellationToken);
    }

    private static async Task EnsureStandardTaxRuleAsync(
        ITaxDirectory tax,
        TaxDbContext taxDb,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var active = await taxDb.Rules.AsNoTracking()
            .AnyAsync(
                rule => rule.CategoryId == categoryId
                    && rule.Jurisdiction == "IR-NAT"
                    && rule.Market == DemoMarket
                    && rule.Status == TaxRuleStatus.Active,
                cancellationToken);
        if (active)
        {
            return;
        }

        var rule = await tax.CreateRuleAsync(
            "IR-NAT",
            DemoMarket,
            categoryId,
            TaxRuleKind.Percentage,
            0.09m,
            PriceValidFrom,
            null,
            10,
            TaxOverridePolicy.Disabled,
            cancellationToken);
        await tax.ActivateRuleAsync(rule.RuleId, cancellationToken);
    }

    /// <summary>
    /// شمارش وضعیت منتشرشدهٔ Catalog را برای شواهد می‌خواند. فقط schema همین ماژول خوانده می‌شود.
    /// </summary>
    private static async Task<StorefrontDemoSeedSummary> SummarizeAsync(
        CatalogDbContext catalogRead,
        bool alreadySeeded,
        CancellationToken cancellationToken)
    {
        var publishedCategories = await catalogRead.Categories.AsNoTracking()
            .Where(category => category.Status == CatalogPublicationStatus.Published)
            .Select(category => new { category.CategoryId, category.ParentCategoryId })
            .ToListAsync(cancellationToken);
        var rootIds = publishedCategories
            .Where(category => category.ParentCategoryId is null)
            .Select(category => category.CategoryId)
            .ToHashSet();
        var secondLevelIds = publishedCategories
            .Where(category => category.ParentCategoryId is Guid parentId && rootIds.Contains(parentId))
            .Select(category => category.CategoryId)
            .ToHashSet();
        var thirdLevelCount = publishedCategories.Count(category =>
            category.ParentCategoryId is Guid parentId && secondLevelIds.Contains(parentId));
        var products = await catalogRead.Products.AsNoTracking()
            .CountAsync(product => product.Status == CatalogPublicationStatus.Published, cancellationToken);
        var brands = await catalogRead.Brands.AsNoTracking()
            .CountAsync(brand => brand.Status == CatalogPublicationStatus.Published, cancellationToken);
        return new StorefrontDemoSeedSummary(
            rootIds.Count,
            secondLevelIds.Count,
            thirdLevelCount,
            products,
            brands,
            StorefrontDemoCatalogMatrix.ExpectedOfferCount,
            alreadySeeded);
    }

    /// <summary>
    /// برای پایگاه‌های Development قبلی که فقط دو سطح داشتند، برگ‌های سطح سوم را idempotent اضافه می‌کند
    /// و محصولات demo- را به برگ‌های متناظر منتقل می‌کند.
    /// </summary>
    private static async Task EnsureThirdLevelCategoriesAsync(
        CatalogDbContext catalogRead,
        ICatalogDirectory catalog,
        CancellationToken cancellationToken)
    {
        var categories = await catalogRead.Categories.AsNoTracking()
            .Where(category => category.Status == CatalogPublicationStatus.Published)
            .ToListAsync(cancellationToken);
        var names = await catalogRead.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.Category && text.FieldKey == "name")
            .ToListAsync(cancellationToken);
        string Name(Guid categoryId) =>
            names.FirstOrDefault(text => text.OwnerId == categoryId && text.Locale.StartsWith("fa"))?.Value ?? string.Empty;

        foreach (var family in StorefrontDemoCatalogMatrix.Families)
        {
            var root = categories.SingleOrDefault(category =>
                category.ParentCategoryId is null && Name(category.CategoryId) == family.Name);
            if (root is null)
            {
                continue;
            }

            foreach (var child in family.Children)
            {
                var secondLevel = categories.SingleOrDefault(category =>
                    category.ParentCategoryId == root.CategoryId && Name(category.CategoryId) == child.Name);
                if (secondLevel is null)
                {
                    continue;
                }

                var existingLeaves = categories
                    .Where(category => category.ParentCategoryId == secondLevel.CategoryId)
                    .ToList();
                if (existingLeaves.Count >= child.Products.Count)
                {
                    continue;
                }

                var leafCategories = new List<(Guid CategoryId, string Slug)>();
                for (var leafIndex = 0; leafIndex < child.Products.Count; leafIndex++)
                {
                    var spec = child.Products[leafIndex];
                    var existing = existingLeaves.FirstOrDefault(category => Name(category.CategoryId) == spec.Name);
                    if (existing is not null)
                    {
                        leafCategories.Add((existing.CategoryId, $"demo-{child.Token}-{leafIndex + 1}"));
                        continue;
                    }

                    var leafCategory = await catalog.CreateCategoryAsync(
                        secondLevel.CategoryId,
                        new Dictionary<string, string> { ["fa-IR"] = spec.Name },
                        cancellationToken);
                    await catalog.PublishCategoryAsync(leafCategory.CategoryId, cancellationToken);
                    leafCategories.Add((leafCategory.CategoryId, $"demo-{child.Token}-{leafIndex + 1}"));
                }

                var demoProducts = await catalogRead.Products.AsNoTracking()
                    .Where(product => product.SlugSeam != null && product.SlugSeam.StartsWith($"demo-{child.Token}-"))
                    .Select(product => new { product.ProductId, product.SlugSeam })
                    .ToListAsync(cancellationToken);
                foreach (var demoProduct in demoProducts)
                {
                    var leafIndex = int.Parse(demoProduct.SlugSeam!.Split('-')[^1], System.Globalization.CultureInfo.InvariantCulture) - 1;
                    if (leafIndex < 0 || leafIndex >= leafCategories.Count)
                    {
                        continue;
                    }

                    var currentLinks = await catalogRead.ProductCategories.AsNoTracking()
                        .Where(link => link.ProductId == demoProduct.ProductId)
                        .Select(link => link.CategoryId)
                        .ToListAsync(cancellationToken);
                    var targetLeafId = leafCategories[leafIndex].CategoryId;
                    if (!currentLinks.Contains(targetLeafId))
                    {
                        await catalog.AssignCategoryAsync(demoProduct.ProductId, targetLeafId, cancellationToken);
                    }
                }
            }
        }
    }
}
