using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>وضعیت انتشار صفحهٔ فروشگاه. فقط Draft و Published.</summary>
public enum StoreLandingPageStatus
{
    /// <summary>پیش‌نویس؛ مسیر عمومی ۴۰۴ است.</summary>
    Draft = 0,

    /// <summary>منتشرشده؛ با Store+Locale+Slug قابل حل است.</summary>
    Published = 1,
}

/// <summary>نوع صریح صفحهٔ فروشگاه: خانه یا فرود.</summary>
public enum StorePageType
{
    /// <summary>صفحهٔ فرود؛ مسیر کاننیکال /landing/{slug}.</summary>
    Landing = 0,

    /// <summary>صفحهٔ خانه؛ مسیر کاننیکال /؛ حداکثر یک فعال در هر Store.</summary>
    Home = 1,
}

/// <summary>صفحهٔ فروشگاه (Home یا Landing). محتوای اجرایی HTML/CSS/JS ندارد.</summary>
public sealed class StoreLandingPage
{
    /// <summary>حداکثر طول slug.</summary>
    public const int SlugMaxLength = 128;

    /// <summary>حداکثر طول عنوان.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>حداکثر طول locale.</summary>
    public const int LocaleMaxLength = 16;

    /// <summary>حداکثر طول عنوان SEO.</summary>
    public const int SeoTitleMaxLength = 200;

    /// <summary>حداکثر طول توضیح SEO.</summary>
    public const int SeoDescriptionMaxLength = 500;

    /// <summary>حداکثر طول URL کاننیکال/OG.</summary>
    public const int UrlMaxLength = 500;

    /// <summary>حداکثر طول H1.</summary>
    public const int H1MaxLength = 200;

    /// <summary>کلید قالب رزروشده.</summary>
    public const string DefaultTemplateKey = "default";

    private StoreLandingPage()
    {
    }

    /// <summary>شناسهٔ پایدار صفحه.</summary>
    public Guid PageId { get; init; }

    /// <summary>نوع صریح Home یا Landing؛ هرگز null/مبهم نیست.</summary>
    public StorePageType PageType { get; private set; } = StorePageType.Landing;

    /// <summary>locale کاننیکال fa یا en.</summary>
    public string Locale { get; private set; } = "fa";

    /// <summary>slug نرمال‌شده.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>عنوان انسانی.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>عنوان SEO اختیاری.</summary>
    public string? SeoTitle { get; private set; }

    /// <summary>توضیح SEO اختیاری.</summary>
    public string? SeoDescription { get; private set; }

    /// <summary>آیا موتور جستجو ایندکس کند.</summary>
    public bool RobotsIndex { get; private set; } = true;

    /// <summary>آیا موتور جستجو پیوندها را دنبال کند.</summary>
    public bool RobotsFollow { get; private set; } = true;

    /// <summary>بازنویسی اختیاری URL کاننیکال.</summary>
    public string? CanonicalUrl { get; private set; }

    /// <summary>عنوان OpenGraph اختیاری.</summary>
    public string? OgTitle { get; private set; }

    /// <summary>توضیح OpenGraph اختیاری.</summary>
    public string? OgDescription { get; private set; }

    /// <summary>تصویر OpenGraph اختیاری (URL).</summary>
    public string? OgImageUrl { get; private set; }

    /// <summary>بازنویسی اختیاری H1 اولیه؛ null یعنی عنوان صفحه.</summary>
    public string? PrimaryH1 { get; private set; }

    /// <summary>کلید قالب آینده؛ الان فقط default.</summary>
    public string TemplateKey { get; private set; } = DefaultTemplateKey;

    /// <summary>وضعیت انتشار.</summary>
    public StoreLandingPageStatus Status { get; private set; } = StoreLandingPageStatus.Draft;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>صفحهٔ پیش‌نویس جدید می‌سازد.</summary>
    public static StoreLandingPage Create(
        string? locale,
        string? slug,
        string? title,
        string? seoTitle,
        string? seoDescription,
        DateTimeOffset now,
        string? pageType = null,
        bool? robotsIndex = null,
        bool? robotsFollow = null,
        string? canonicalUrl = null,
        string? ogTitle = null,
        string? ogDescription = null,
        string? ogImageUrl = null,
        string? primaryH1 = null)
    {
        var normalizedSlug = StoreLandingPageSlug.Normalize(slug);
        if (!StoreLandingPageSlug.IsValid(normalizedSlug))
        {
            throw new PlatformHttpException(400, "آدرس صفحه معتبر نیست.", "landing.slug.invalid");
        }

        if (StoreLandingPageSlug.IsReserved(normalizedSlug))
        {
            throw new PlatformHttpException(400, "این آدرس برای مسیرهای سامانه رزرو شده است.", "landing.slug.reserved");
        }

        var normalizedTitle = NormalizeTitle(title);
        return new StoreLandingPage
        {
            PageId = UuidV7.New(),
            PageType = ParsePageType(pageType),
            Locale = StoreLandingPageSlug.NormalizeLocale(locale),
            Slug = normalizedSlug,
            Title = normalizedTitle,
            SeoTitle = NormalizeOptional(seoTitle, SeoTitleMaxLength),
            SeoDescription = NormalizeOptional(seoDescription, SeoDescriptionMaxLength),
            RobotsIndex = robotsIndex ?? true,
            RobotsFollow = robotsFollow ?? true,
            CanonicalUrl = NormalizeOptional(canonicalUrl, UrlMaxLength),
            OgTitle = NormalizeOptional(ogTitle, SeoTitleMaxLength),
            OgDescription = NormalizeOptional(ogDescription, SeoDescriptionMaxLength),
            OgImageUrl = NormalizeOptional(ogImageUrl, UrlMaxLength),
            PrimaryH1 = NormalizeOptional(primaryH1, H1MaxLength),
            TemplateKey = DefaultTemplateKey,
            Status = StoreLandingPageStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای قابل ویرایش را به‌روز می‌کند (نوع صفحه فقط از مسیر SetHome تغییر می‌کند).</summary>
    public void Update(
        string? slug,
        string? title,
        string? seoTitle,
        string? seoDescription,
        DateTimeOffset now,
        bool? robotsIndex = null,
        bool? robotsFollow = null,
        string? canonicalUrl = null,
        string? ogTitle = null,
        string? ogDescription = null,
        string? ogImageUrl = null,
        string? primaryH1 = null)
    {
        var normalizedSlug = StoreLandingPageSlug.Normalize(slug ?? Slug);
        if (!StoreLandingPageSlug.IsValid(normalizedSlug))
        {
            throw new PlatformHttpException(400, "آدرس صفحه معتبر نیست.", "landing.slug.invalid");
        }

        if (StoreLandingPageSlug.IsReserved(normalizedSlug))
        {
            throw new PlatformHttpException(400, "این آدرس برای مسیرهای سامانه رزرو شده است.", "landing.slug.reserved");
        }

        Slug = normalizedSlug;
        Title = NormalizeTitle(title);
        SeoTitle = NormalizeOptional(seoTitle, SeoTitleMaxLength);
        SeoDescription = NormalizeOptional(seoDescription, SeoDescriptionMaxLength);
        if (robotsIndex.HasValue)
        {
            RobotsIndex = robotsIndex.Value;
        }

        if (robotsFollow.HasValue)
        {
            RobotsFollow = robotsFollow.Value;
        }

        CanonicalUrl = NormalizeOptional(canonicalUrl, UrlMaxLength);
        OgTitle = NormalizeOptional(ogTitle, SeoTitleMaxLength);
        OgDescription = NormalizeOptional(ogDescription, SeoDescriptionMaxLength);
        OgImageUrl = NormalizeOptional(ogImageUrl, UrlMaxLength);
        PrimaryH1 = NormalizeOptional(primaryH1, H1MaxLength);
        UpdatedAt = now;
    }

    /// <summary>نوع صفحه را به Home یا Landing تنظیم می‌کند (فقط مسیر Home).</summary>
    public void SetPageType(StorePageType pageType, DateTimeOffset now)
    {
        PageType = pageType;
        UpdatedAt = now;
    }

    /// <summary>صفحه را منتشر می‌کند.</summary>
    public void Publish(DateTimeOffset now)
    {
        Status = StoreLandingPageStatus.Published;
        UpdatedAt = now;
    }

    /// <summary>صفحه را به پیش‌نویس برمی‌گرداند.</summary>
    public void Unpublish(DateTimeOffset now)
    {
        Status = StoreLandingPageStatus.Draft;
        UpdatedAt = now;
    }

    /// <summary>آیا برای انتخاب به‌عنوان خانه واجد شرایط است.</summary>
    public bool IsEligibleHome => Status == StoreLandingPageStatus.Published;

    /// <summary>H1 اولیهٔ صفحه را برمی‌گرداند.</summary>
    public string ResolvePrimaryH1() => string.IsNullOrWhiteSpace(PrimaryH1) ? Title : PrimaryH1!;

    /// <summary>نوع صفحه را از رشتهٔ Admin پارس می‌کند؛ پیش‌فرض Landing.</summary>
    public static StorePageType ParsePageType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return StorePageType.Landing;
        }

        if (string.Equals(raw.Trim(), "Home", StringComparison.OrdinalIgnoreCase))
        {
            return StorePageType.Home;
        }

        if (string.Equals(raw.Trim(), "Landing", StringComparison.OrdinalIgnoreCase))
        {
            return StorePageType.Landing;
        }

        throw new PlatformHttpException(400, "نوع صفحه باید Home یا Landing باشد.", "landing.pagetype.invalid");
    }

    private static string NormalizeTitle(string? title)
    {
        var value = title?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            throw new PlatformHttpException(400, "عنوان صفحه لازم است.", "landing.title.required");
        }

        return value.Length > TitleMaxLength ? value[..TitleMaxLength] : value;
    }

    private static string? NormalizeOptional(string? raw, int max)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.Length > max ? value[..max] : value;
    }
}
