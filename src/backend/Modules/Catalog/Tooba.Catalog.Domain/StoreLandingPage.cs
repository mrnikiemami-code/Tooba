using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>وضعیت انتشار صفحهٔ Landing. فقط Draft و Published.</summary>
public enum StoreLandingPageStatus
{
    /// <summary>پیش‌نویس؛ مسیر عمومی ۴۰۴ است.</summary>
    Draft = 0,

    /// <summary>منتشرشده؛ با Store+Locale+Slug قابل حل است.</summary>
    Published = 1,
}

/// <summary>صفحهٔ Landing فروشگاه. محتوای اجرایی HTML/CSS/JS ندارد.</summary>
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

    /// <summary>کلید قالب رزروشده.</summary>
    public const string DefaultTemplateKey = "default";

    private StoreLandingPage()
    {
    }

    /// <summary>شناسهٔ پایدار صفحه.</summary>
    public Guid PageId { get; init; }

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
        DateTimeOffset now)
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
            Locale = StoreLandingPageSlug.NormalizeLocale(locale),
            Slug = normalizedSlug,
            Title = normalizedTitle,
            SeoTitle = NormalizeOptional(seoTitle, SeoTitleMaxLength),
            SeoDescription = NormalizeOptional(seoDescription, SeoDescriptionMaxLength),
            TemplateKey = DefaultTemplateKey,
            Status = StoreLandingPageStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای قابل ویرایش را به‌روز می‌کند.</summary>
    public void Update(
        string? slug,
        string? title,
        string? seoTitle,
        string? seoDescription,
        DateTimeOffset now)
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
