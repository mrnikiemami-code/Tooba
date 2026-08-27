using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tooba.BuildingBlocks;

namespace Tooba.PageComposition.Domain;

/// <summary>کلید صفحهٔ پشتیبانی‌شده در نسخهٔ پایه.</summary>
public static class PageKeys
{
    /// <summary>صفحهٔ خانه.</summary>
    public const string Home = "home";
}

/// <summary>شناسهٔ پایدار Tenant برای Page Composition.</summary>
public static class PageCompositionTenantIds
{
    /// <summary>Tenant توسعهٔ store-alpha.</summary>
    public static readonly Guid StoreAlpha = Guid.Parse("a0000000-0001-4000-8000-000000000001");

    /// <summary>Tenant توسعهٔ store-beta برای تست جداسازی.</summary>
    public static readonly Guid StoreBeta = Guid.Parse("a0000000-0002-4000-8000-000000000002");

    /// <summary>کلید Tenant پیکربندی‌شده را به Guid پایدار نگاشت می‌کند.</summary>
    public static Guid FromTenantKey(string tenantKey)
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new InvalidOperationException("TenantId معتبر نیست.");

        if (string.Equals(tenantKey, "store-alpha", StringComparison.Ordinal))
            return StoreAlpha;
        if (string.Equals(tenantKey, "store-beta", StringComparison.Ordinal))
            return StoreBeta;

        var payload = Encoding.UTF8.GetBytes($"tooba:page-composition:{tenantKey.Trim()}");
        var hash = SHA256.HashData(payload);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}

/// <summary>کاتالوگ ثابت انواع section تأییدشده.</summary>
public static class SectionCatalog
{
    /// <summary>نوع section.</summary>
    public const string Hero = "hero";
    /// <summary>نوع section.</summary>
    public const string Stories = "stories";
    /// <summary>نوع section.</summary>
    public const string CategoryGrid = "category_grid";
    /// <summary>نوع section.</summary>
    public const string ProductRailFlash = "product_rail_flash";
    /// <summary>نوع section.</summary>
    public const string BestSellers = "best_sellers";
    /// <summary>نوع section.</summary>
    public const string ProductRailMostViewed = "product_rail_most_viewed";
    /// <summary>نوع section.</summary>
    public const string MiddleBanners = "middle_banners";
    /// <summary>نوع section.</summary>
    public const string Brands = "brands";
    /// <summary>نوع section.</summary>
    public const string NewestProducts = "newest_products";
    /// <summary>نوع section.</summary>
    public const string CustomerReviews = "customer_reviews";
    /// <summary>نوع section.</summary>
    public const string LatestArticles = "latest_articles";

    /// <summary>variant پیش‌فرض.</summary>
    public const string DefaultVariant = "default";

    /// <summary>حداکثر طول عنوان config.</summary>
    public const int TitleMaxLength = 120;
    /// <summary>حداقل itemCount.</summary>
    public const int ItemCountMin = 1;
    /// <summary>حداکثر itemCount.</summary>
    public const int ItemCountMax = 24;
    /// <summary>حداکثر طول href.</summary>
    public const int HrefMaxLength = 256;

    private static readonly string[] DefaultHomeSectionOrder =
    [
        Hero,
        Stories,
        CategoryGrid,
        ProductRailFlash,
        BestSellers,
        ProductRailMostViewed,
        MiddleBanners,
        Brands,
        NewestProducts,
        CustomerReviews,
        LatestArticles,
    ];

    private static readonly HashSet<string> ForbiddenConfigKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "css",
        "html",
        "js",
        "className",
    };

    private static readonly HashSet<string> AllowedConfigKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "title",
        "href",
        "itemCount",
        "sourceKind",
    };

    private static readonly HashSet<string> AllowedSourceKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "offers",
        "most_viewed",
        "new_arrivals",
    };

    private static readonly Dictionary<string, IReadOnlyList<string>> AllowedVariants =
        new(StringComparer.Ordinal)
        {
            [Hero] = [DefaultVariant],
            [Stories] = [DefaultVariant],
            [CategoryGrid] = [DefaultVariant],
            [ProductRailFlash] = [DefaultVariant],
            [BestSellers] = [DefaultVariant],
            [ProductRailMostViewed] = [DefaultVariant],
            [MiddleBanners] = [DefaultVariant],
            [Brands] = [DefaultVariant],
            [NewestProducts] = [DefaultVariant],
            [CustomerReviews] = [DefaultVariant],
            [LatestArticles] = [DefaultVariant],
        };

    /// <summary>همهٔ انواع section ثابت.</summary>
    public static IReadOnlyList<string> AllSectionTypes => DefaultHomeSectionOrder;

    /// <summary>ترتیب پیش‌فرض sectionهای خانه.</summary>
    public static IReadOnlyList<string> DefaultHomeSectionTypes => DefaultHomeSectionOrder;

    /// <summary>variantهای مجاز برای یک نوع section.</summary>
    public static IReadOnlyList<string> GetAllowedVariants(string sectionType)
    {
        EnsureKnownSectionType(sectionType);
        return AllowedVariants[sectionType];
    }

    /// <summary>نوع section ناشناخته را رد می‌کند.</summary>
    public static void EnsureKnownSectionType(string sectionType)
    {
        if (string.IsNullOrWhiteSpace(sectionType) || !AllowedVariants.ContainsKey(sectionType))
            throw new InvalidOperationException("نوع section در کاتالوگ تأییدشده نیست.");
    }

    /// <summary>variant را برای نوع section اعتبارسنجی می‌کند.</summary>
    public static void EnsureAllowedVariant(string sectionType, string variant)
    {
        EnsureKnownSectionType(sectionType);
        if (string.IsNullOrWhiteSpace(variant) || !AllowedVariants[sectionType].Contains(variant))
            throw new InvalidOperationException("variant section مجاز نیست.");
    }

    /// <summary>JSON config امن را اعتبارسنجی و نرمال می‌کند.</summary>
    public static string ValidateAndNormalizeConfiguration(string sectionType, string? configurationJson)
    {
        EnsureKnownSectionType(sectionType);
        if (string.IsNullOrWhiteSpace(configurationJson))
            return "{}";

        using var document = JsonDocument.Parse(configurationJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("پیکربندی section باید شیء JSON باشد.");

        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (ForbiddenConfigKeys.Contains(property.Name))
                throw new InvalidOperationException($"کلید config ممنوع است: {property.Name}");
            if (!AllowedConfigKeys.Contains(property.Name))
                throw new InvalidOperationException($"کلید config ناشناخته است: {property.Name}");

            switch (property.Name.ToLowerInvariant())
            {
                case "title":
                    if (property.Value.ValueKind != JsonValueKind.String)
                        throw new InvalidOperationException("title باید رشته باشد.");
                    var title = property.Value.GetString()?.Trim() ?? string.Empty;
                    if (title.Length == 0 || title.Length > TitleMaxLength)
                        throw new InvalidOperationException("title معتبر نیست.");
                    normalized["title"] = title;
                    break;
                case "href":
                    if (!SupportsHref(sectionType))
                        throw new InvalidOperationException("href برای این section مجاز نیست.");
                    if (property.Value.ValueKind != JsonValueKind.String)
                        throw new InvalidOperationException("href باید رشته باشد.");
                    var href = property.Value.GetString()?.Trim() ?? string.Empty;
                    if (href.Length == 0 || href.Length > HrefMaxLength)
                        throw new InvalidOperationException("href معتبر نیست.");
                    normalized["href"] = href;
                    break;
                case "itemcount":
                    if (!SupportsItemCount(sectionType))
                        throw new InvalidOperationException("itemCount برای این section مجاز نیست.");
                    if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out var itemCount))
                        throw new InvalidOperationException("itemCount باید عدد صحیح باشد.");
                    if (itemCount < ItemCountMin || itemCount > ItemCountMax)
                        throw new InvalidOperationException("itemCount خارج از بازهٔ مجاز است.");
                    normalized["itemCount"] = itemCount;
                    break;
                case "sourcekind":
                    if (!SupportsSourceKind(sectionType))
                        throw new InvalidOperationException("sourceKind برای این section مجاز نیست.");
                    if (property.Value.ValueKind != JsonValueKind.String)
                        throw new InvalidOperationException("sourceKind باید رشته باشد.");
                    var sourceKind = property.Value.GetString()?.Trim() ?? string.Empty;
                    if (!AllowedSourceKinds.Contains(sourceKind))
                        throw new InvalidOperationException("sourceKind مجاز نیست.");
                    normalized["sourceKind"] = sourceKind;
                    break;
            }
        }

        return JsonSerializer.Serialize(normalized);
    }

    /// <summary>متادیتای schema config برای catalog API.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ConfigSchemaMetadata =>
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["title"] = ["string", $"max:{TitleMaxLength}"],
            ["href"] = ["string", $"max:{HrefMaxLength}", "rails"],
            ["itemCount"] = ["integer", $"min:{ItemCountMin}", $"max:{ItemCountMax}", "rails", "articles"],
            ["sourceKind"] = ["enum:offers,most_viewed,new_arrivals", "rails"],
        };

    private static bool SupportsHref(string sectionType) =>
        sectionType is ProductRailFlash or ProductRailMostViewed or BestSellers or NewestProducts or LatestArticles;

    private static bool SupportsItemCount(string sectionType) =>
        sectionType is ProductRailFlash or ProductRailMostViewed or BestSellers or NewestProducts or LatestArticles;

    private static bool SupportsSourceKind(string sectionType) =>
        sectionType is ProductRailFlash or ProductRailMostViewed or NewestProducts;
}

/// <summary>تعریف صفحهٔ قابل ترکیب برای یک Tenant/locale.</summary>
public sealed class PageDefinition
{
    /// <summary>حداکثر طول PageKey.</summary>
    public const int PageKeyMaxLength = 64;
    /// <summary>حداکثر طول locale.</summary>
    public const int LocaleMaxLength = 16;

    private readonly List<PageSection> _sections = [];

    private PageDefinition() { }

    /// <summary>شناسهٔ پایدار تعریف صفحه.</summary>
    public Guid PageDefinitionId { get; init; }
    /// <summary>کلید صفحه مثل home.</summary>
    public string PageKey { get; private set; } = string.Empty;
    /// <summary>Tenant مالک.</summary>
    public Guid TenantId { get; init; }
    /// <summary>locale اختیاری؛ null یعنی همه localeها.</summary>
    public string? Locale { get; private set; }
    /// <summary>توکن همزمانی.</summary>
    public int VersionToken { get; private set; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }
    /// <summary>sectionهای صفحه.</summary>
    public IReadOnlyCollection<PageSection> Sections => _sections;

    /// <summary>تعریف صفحهٔ home با sectionهای پیش‌فرض می‌سازد.</summary>
    public static PageDefinition CreateDefaultHome(Guid tenantId, string? locale, DateTimeOffset now)
    {
        ValidatePageKey(PageKeys.Home);
        ValidateLocale(locale);
        var definition = new PageDefinition
        {
            PageDefinitionId = UuidV7.New(),
            PageKey = PageKeys.Home,
            TenantId = tenantId,
            Locale = NormalizeLocale(locale),
            VersionToken = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        definition.RestoreDefaultSections(now);
        return definition;
    }

    /// <summary>sectionها را با شناسه‌های داده‌شده مرتب می‌کند.</summary>
    public void ReorderSections(IReadOnlyList<Guid> sectionIdsInOrder, DateTimeOffset now)
    {
        if (sectionIdsInOrder.Count == 0)
            throw new InvalidOperationException("ترتیب section خالی است.");
        if (sectionIdsInOrder.Count != _sections.Count)
            throw new InvalidOperationException("ترتیب section با تعداد فعلی هم‌خوان نیست.");
        if (sectionIdsInOrder.Distinct().Count() != sectionIdsInOrder.Count)
            throw new InvalidOperationException("شناسهٔ section تکراری در ترتیب وجود دارد.");

        var lookup = _sections.ToDictionary(section => section.PageSectionId);
        for (var index = 0; index < sectionIdsInOrder.Count; index++)
        {
            if (!lookup.TryGetValue(sectionIdsInOrder[index], out var section))
                throw new InvalidOperationException("section برای مرتب‌سازی یافت نشد.");
            section.SetDisplayOrder(index, now);
        }

        _sections.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
        Touch(now);
    }

    /// <summary>نمایش section را تغییر می‌دهد.</summary>
    public void SetSectionVisibility(Guid sectionId, bool isVisible, DateTimeOffset now)
    {
        var section = RequireSection(sectionId);
        section.SetVisibility(isVisible, now);
        Touch(now);
    }

    /// <summary>پیکربندی section را به‌روزرسانی می‌کند.</summary>
    public void UpdateSectionConfiguration(Guid sectionId, string configurationJson, DateTimeOffset now)
    {
        var section = RequireSection(sectionId);
        section.UpdateConfiguration(configurationJson, now);
        Touch(now);
    }

    /// <summary>variant section را به‌روزرسانی می‌کند.</summary>
    public void UpdateSectionVariant(Guid sectionId, string variant, DateTimeOffset now)
    {
        var section = RequireSection(sectionId);
        section.UpdateVariant(variant, now);
        Touch(now);
    }

    /// <summary>section تأییدشدهٔ جدید اضافه می‌کند.</summary>
    public PageSection AddApprovedSection(string sectionType, string variant, string? configurationJson, DateTimeOffset now)
    {
        SectionCatalog.EnsureKnownSectionType(sectionType);
        SectionCatalog.EnsureAllowedVariant(sectionType, variant);
        var normalizedConfig = SectionCatalog.ValidateAndNormalizeConfiguration(sectionType, configurationJson);
        var order = _sections.Count == 0 ? 0 : _sections.Max(section => section.DisplayOrder) + 1;
        var section = PageSection.Create(
            PageDefinitionId,
            sectionType,
            variant,
            order,
            normalizedConfig,
            now);
        _sections.Add(section);
        Touch(now);
        return section;
    }

    /// <summary>section را حذف می‌کند.</summary>
    public void RemoveSection(Guid sectionId, DateTimeOffset now)
    {
        var index = _sections.FindIndex(section => section.PageSectionId == sectionId);
        if (index < 0)
            throw new InvalidOperationException("section یافت نشد.");
        _sections.RemoveAt(index);
        ReindexSections(now);
        Touch(now);
    }

    /// <summary>sectionهای پیش‌فرض خانه را بازمی‌گرداند.</summary>
    public void RestoreDefaultSections(DateTimeOffset now)
    {
        _sections.Clear();
        var order = 0;
        foreach (var sectionType in SectionCatalog.DefaultHomeSectionTypes)
        {
            _sections.Add(PageSection.Create(
                PageDefinitionId,
                sectionType,
                SectionCatalog.DefaultVariant,
                order++,
                "{}",
                now));
        }
        Touch(now);
    }

    /// <summary>sectionهای بارگذاری‌شده را به aggregate متصل می‌کند.</summary>
    public void AttachSections(IEnumerable<PageSection> sections)
    {
        _sections.Clear();
        _sections.AddRange(sections.OrderBy(section => section.DisplayOrder));
    }

    private PageSection RequireSection(Guid sectionId) =>
        _sections.FirstOrDefault(section => section.PageSectionId == sectionId)
        ?? throw new InvalidOperationException("section یافت نشد.");

    private void ReindexSections(DateTimeOffset now)
    {
        var ordered = _sections.OrderBy(section => section.DisplayOrder).ToList();
        for (var index = 0; index < ordered.Count; index++)
            ordered[index].SetDisplayOrder(index, now);
    }

    private void Touch(DateTimeOffset now)
    {
        VersionToken++;
        UpdatedAt = now;
    }

    private static void ValidatePageKey(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey) || pageKey.Trim().Length > PageKeyMaxLength)
            throw new InvalidOperationException("PageKey معتبر نیست.");
    }

    private static void ValidateLocale(string? locale)
    {
        if (locale is not null && (locale.Trim().Length == 0 || locale.Trim().Length > LocaleMaxLength))
            throw new InvalidOperationException("locale معتبر نیست.");
    }

    private static string? NormalizeLocale(string? locale) =>
        string.IsNullOrWhiteSpace(locale) ? null : locale.Trim();
}

/// <summary>یک section در ترکیب صفحه.</summary>
public sealed class PageSection
{
    /// <summary>حداکثر طول SectionType.</summary>
    public const int SectionTypeMaxLength = 64;
    /// <summary>حداکثر طول Variant.</summary>
    public const int VariantMaxLength = 64;
    /// <summary>حداکثر طول ConfigurationJson.</summary>
    public const int ConfigurationJsonMaxLength = 4000;

    private PageSection() { }

    /// <summary>شناسهٔ پایدار section.</summary>
    public Guid PageSectionId { get; init; }
    /// <summary>شناسهٔ PageDefinition والد.</summary>
    public Guid PageDefinitionId { get; init; }
    /// <summary>نوع section از کاتالوگ ثابت.</summary>
    public string SectionType { get; private set; } = string.Empty;
    /// <summary>ترتیب نمایش.</summary>
    public int DisplayOrder { get; private set; }
    /// <summary>آیا section قابل نمایش است.</summary>
    public bool IsVisible { get; private set; }
    /// <summary>variant تأییدشده.</summary>
    public string Variant { get; private set; } = SectionCatalog.DefaultVariant;
    /// <summary>پیکربندی JSON امن.</summary>
    public string ConfigurationJson { get; private set; } = "{}";

    /// <summary>section جدید می‌سازد.</summary>
    internal static PageSection Create(
        Guid pageDefinitionId,
        string sectionType,
        string variant,
        int displayOrder,
        string configurationJson,
        DateTimeOffset now)
    {
        SectionCatalog.EnsureKnownSectionType(sectionType);
        SectionCatalog.EnsureAllowedVariant(sectionType, variant);
        var normalizedConfig = SectionCatalog.ValidateAndNormalizeConfiguration(sectionType, configurationJson);
        return new PageSection
        {
            PageSectionId = UuidV7.New(),
            PageDefinitionId = pageDefinitionId,
            SectionType = sectionType,
            DisplayOrder = displayOrder,
            IsVisible = true,
            Variant = variant,
            ConfigurationJson = normalizedConfig,
        };
    }

    /// <summary>ترتیب نمایش را تنظیم می‌کند.</summary>
    internal void SetDisplayOrder(int displayOrder, DateTimeOffset now) => DisplayOrder = displayOrder;

    /// <summary>visibility را تغییر می‌دهد.</summary>
    internal void SetVisibility(bool isVisible, DateTimeOffset now) => IsVisible = isVisible;

    /// <summary>پیکربندی را به‌روزرسانی می‌کند.</summary>
    internal void UpdateConfiguration(string configurationJson, DateTimeOffset now)
    {
        ConfigurationJson = SectionCatalog.ValidateAndNormalizeConfiguration(SectionType, configurationJson);
    }

    /// <summary>variant را به‌روزرسانی می‌کند.</summary>
    internal void UpdateVariant(string variant, DateTimeOffset now)
    {
        SectionCatalog.EnsureAllowedVariant(SectionType, variant);
        Variant = variant;
    }
}
