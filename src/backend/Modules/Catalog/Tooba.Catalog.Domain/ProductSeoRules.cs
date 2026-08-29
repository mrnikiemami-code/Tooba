namespace Tooba.Catalog.Domain;

/// <summary>
/// قوانین آمادگی و مسیر عمومی SEO محصول — بدون Offer/Price/Stock.
/// Slug عمومی سراسری است (SlugSeam)؛ عنوان/توضیح SEO محلی‌اند.
/// </summary>
public static class ProductSeoRules
{
    /// <summary>آدرس محصول (slug) ناقص است.</summary>
    public const string MessageSlugIncompleteFa = "آدرس محصول تکمیل نشده است";

    /// <summary>عنوان نتیجهٔ جستجو ناقص است.</summary>
    public const string MessageTitleIncompleteFa = "عنوان نتیجه جستجو تکمیل نشده است";

    /// <summary>توضیح نتیجهٔ جستجو ناقص است.</summary>
    public const string MessageDescriptionIncompleteFa = "توضیح نتیجه جستجو تکمیل نشده است";

    /// <summary>هویت محلی محصول ناقص است.</summary>
    public const string MessageIdentityIncompleteFa = "اطلاعات هویتی محصول تکمیل نشده است";

    /// <summary>SEO کامل است.</summary>
    public const string MessageReadyFa = "اطلاعات سئو کامل است";

    /// <summary>
    /// مسیر عمومی canonical فروشگاه: /{fa|en}/products/{slug}.
    /// شکل /product/ (مفرد) عمداً استفاده نمی‌شود.
    /// </summary>
    public static string BuildPublicPath(string locale, string? slug)
    {
        var prefix = ToStorefrontLocalePrefix(locale);
        var safeSlug = string.IsNullOrWhiteSpace(slug) ? string.Empty : slug.Trim();
        return string.IsNullOrEmpty(safeSlug)
            ? $"/{prefix}/products/"
            : $"/{prefix}/products/{safeSlug}";
    }

    /// <summary>locale Catalog → پیشوند دو حرفی مسیر فروشگاه.</summary>
    public static string ToStorefrontLocalePrefix(string locale)
    {
        var trimmed = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        if (trimmed.Equals("en", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        return "fa";
    }

    /// <summary>locale را برای LocalizedText نرمال می‌کند.</summary>
    public static string NormalizeLocale(string? locale)
    {
        var trimmed = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        if (trimmed.Equals("fa", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("fa-", StringComparison.OrdinalIgnoreCase))
        {
            return "fa-IR";
        }

        if (trimmed.Equals("en", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        return CatalogCategorySlugNormalizer.NormalizeLocale(trimmed);
    }

    /// <summary>
    /// آمادگی SEO را از فیلدهای موجود می‌سازد.
    /// عنوان خالی با نام محصول به‌عنوان fallback مستند پذیرفته می‌شود.
    /// </summary>
    public static ProductSeoReadinessSnapshot Evaluate(
        string? slug,
        string? seoTitle,
        string? seoDescription,
        string? productName)
    {
        var hasValidSlug = !string.IsNullOrWhiteSpace(slug);
        if (hasValidSlug)
        {
            try
            {
                _ = CatalogCategorySlugNormalizer.NormalizeSlug(slug!);
            }
            catch (Exception)
            {
                hasValidSlug = false;
            }
        }

        var hasIdentity = !string.IsNullOrWhiteSpace(productName);
        var hasTitleOrFallback = !string.IsNullOrWhiteSpace(seoTitle) || hasIdentity;
        var hasDescription = !string.IsNullOrWhiteSpace(seoDescription);
        var isReady = hasValidSlug && hasTitleOrFallback && hasDescription && hasIdentity;

        string messageFa;
        if (!hasValidSlug)
        {
            messageFa = MessageSlugIncompleteFa;
        }
        else if (!hasIdentity)
        {
            messageFa = MessageIdentityIncompleteFa;
        }
        else if (!hasTitleOrFallback)
        {
            messageFa = MessageTitleIncompleteFa;
        }
        else if (!hasDescription)
        {
            messageFa = MessageDescriptionIncompleteFa;
        }
        else
        {
            messageFa = MessageReadyFa;
        }

        return new ProductSeoReadinessSnapshot(
            hasValidSlug,
            hasTitleOrFallback,
            hasDescription,
            hasIdentity,
            isReady,
            messageFa);
    }
}

/// <summary>نتیجهٔ ارزیابی آمادگی SEO در Domain (بدون وابستگی Application).</summary>
public sealed record ProductSeoReadinessSnapshot(
    bool HasValidSlug,
    bool HasSeoTitleOrFallback,
    bool HasSeoDescription,
    bool HasLocalizedIdentity,
    bool IsReady,
    string MessageFa);
