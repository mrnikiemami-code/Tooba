using Tooba.BuildingBlocks;

namespace Tooba.Content.Domain;

/// <summary>وضعیت انتشار مقالهٔ تحریری.</summary>
public enum ContentPublicationStatus
{
    /// <summary>پیش‌نویس و غیرقابل نمایش عمومی.</summary>
    Draft = 0,
    /// <summary>منتشرشده برای نمایش عمومی.</summary>
    Published = 1,
}

/// <summary>مقالهٔ تحریری برای ریل خانه و مسیرهای عمومی تجاری.</summary>
/// <remarks>
/// هر مقاله یک موجودیت مستقل در یک زبان (Locale) است؛ انتشار فارسی همتای انگلیسی اجباری نمی‌سازد.
/// </remarks>
public sealed class ContentArticle
{
    /// <summary>حداکثر طول slug.</summary>
    public const int SlugMaxLength = 128;
    /// <summary>حداکثر طول عنوان.</summary>
    public const int TitleMaxLength = 200;
    /// <summary>حداکثر طول چکیده.</summary>
    public const int ExcerptMaxLength = 500;
    /// <summary>حداکثر طول بدنه.</summary>
    public const int BodyMaxLength = 50_000;
    /// <summary>حداکثر طول locale.</summary>
    public const int LocaleMaxLength = 16;
    /// <summary>حداکثر طول عنوان SEO.</summary>
    public const int SeoTitleMaxLength = 200;
    /// <summary>حداکثر طول توضیح SEO.</summary>
    public const int SeoDescriptionMaxLength = 500;
    /// <summary>حداکثر طول دسته.</summary>
    public const int CategoryMaxLength = 100;
    /// <summary>حداکثر طول نام نمایشی نویسنده.</summary>
    public const int AuthorDisplayNameMaxLength = 100;
    /// <summary>locale پیش‌فرض فارسی.</summary>
    public const string DefaultLocale = "fa-IR";

    private ContentArticle() { }

    /// <summary>شناسهٔ پایدار مقاله.</summary>
    public Guid ArticleId { get; init; }
    /// <summary>slug پایدار برای URL.</summary>
    public string Slug { get; private set; } = string.Empty;
    /// <summary>عنوان فارسی.</summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>چکیدهٔ کوتاه.</summary>
    public string Excerpt { get; private set; } = string.Empty;
    /// <summary>بدنهٔ HTML/متن مقاله.</summary>
    public string Body { get; private set; } = string.Empty;
    /// <summary>locale محتوا.</summary>
    public string Locale { get; private set; } = DefaultLocale;
    /// <summary>عنوان SEO اختیاری.</summary>
    public string? SeoTitle { get; private set; }
    /// <summary>توضیح SEO اختیاری.</summary>
    public string? SeoDescription { get; private set; }
    /// <summary>برچسب سادهٔ taxonomy.</summary>
    public string? Category { get; private set; }
    /// <summary>مرجع مات تصویر جلد.</summary>
    public Guid? CoverMediaAssetId { get; private set; }
    /// <summary>نام نمایشی نویسنده.</summary>
    public string AuthorDisplayName { get; private set; } = string.Empty;
    /// <summary>برچسب‌های CSV ساده.</summary>
    public string TagsCsv { get; private set; } = string.Empty;
    /// <summary>آیا در ریل خانه به‌عنوان ویژه نشان داده شود.</summary>
    public bool IsFeatured { get; private set; }
    /// <summary>وضعیت انتشار.</summary>
    public ContentPublicationStatus Status { get; private set; }
    /// <summary>زمان انتشار UTC.</summary>
    public DateTimeOffset PublishDate { get; private set; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>مقالهٔ Draft معتبر می‌سازد.</summary>
    public static ContentArticle Create(
        string slug,
        string title,
        string excerpt,
        string body,
        Guid? coverMediaAssetId,
        string authorDisplayName,
        IReadOnlyList<string> tags,
        bool isFeatured,
        DateTimeOffset publishDate,
        DateTimeOffset now,
        string? locale = null,
        string? seoTitle = null,
        string? seoDescription = null,
        string? category = null)
    {
        var resolvedLocale = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale.Trim();
        Validate(slug, title, excerpt, body, authorDisplayName, resolvedLocale, seoTitle, seoDescription, category);
        return new ContentArticle
        {
            ArticleId = UuidV7.New(),
            Slug = slug.Trim().ToLowerInvariant(),
            Title = title.Trim(),
            Excerpt = excerpt.Trim(),
            Body = body.Trim(),
            Locale = resolvedLocale,
            SeoTitle = NormalizeOptional(seoTitle, SeoTitleMaxLength),
            SeoDescription = NormalizeOptional(seoDescription, SeoDescriptionMaxLength),
            Category = NormalizeOptional(category, CategoryMaxLength),
            CoverMediaAssetId = coverMediaAssetId,
            AuthorDisplayName = authorDisplayName.Trim(),
            TagsCsv = string.Join(',', tags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0)),
            IsFeatured = isFeatured,
            Status = ContentPublicationStatus.Draft,
            PublishDate = publishDate,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای تحریری مقاله را به‌روزرسانی می‌کند.</summary>
    public void Update(
        string title,
        string excerpt,
        string body,
        string? seoTitle,
        string? seoDescription,
        string? category,
        Guid? coverMediaAssetId,
        string authorDisplayName,
        IReadOnlyList<string> tags,
        bool isFeatured,
        DateTimeOffset now,
        string? locale = null)
    {
        var resolvedLocale = string.IsNullOrWhiteSpace(locale) ? Locale : locale.Trim();
        Validate(Slug, title, excerpt, body, authorDisplayName, resolvedLocale, seoTitle, seoDescription, category);
        Title = title.Trim();
        Excerpt = excerpt.Trim();
        Body = body.Trim();
        Locale = resolvedLocale;
        SeoTitle = NormalizeOptional(seoTitle, SeoTitleMaxLength);
        SeoDescription = NormalizeOptional(seoDescription, SeoDescriptionMaxLength);
        Category = NormalizeOptional(category, CategoryMaxLength);
        CoverMediaAssetId = coverMediaAssetId;
        AuthorDisplayName = authorDisplayName.Trim();
        TagsCsv = string.Join(',', tags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0));
        IsFeatured = isFeatured;
        UpdatedAt = now;
    }

    /// <summary>مقاله را برای نمایش عمومی منتشر می‌کند.</summary>
    public void Publish(DateTimeOffset now)
    {
        Status = ContentPublicationStatus.Published;
        UpdatedAt = now;
    }

    /// <summary>مقاله را از انتشار خارج و به Draft برمی‌گرداند.</summary>
    public void Unpublish(DateTimeOffset now)
    {
        Status = ContentPublicationStatus.Draft;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new InvalidOperationException("مقدار اختیاری مقاله از سقف مجاز بلندتر است.");
        return trimmed;
    }

    private static void Validate(
        string slug,
        string title,
        string excerpt,
        string body,
        string authorDisplayName,
        string locale,
        string? seoTitle,
        string? seoDescription,
        string? category)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > SlugMaxLength)
            throw new InvalidOperationException("slug مقاله معتبر نیست.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > TitleMaxLength)
            throw new InvalidOperationException("عنوان مقاله معتبر نیست.");
        if (string.IsNullOrWhiteSpace(excerpt) || excerpt.Trim().Length > ExcerptMaxLength)
            throw new InvalidOperationException("چکیدهٔ مقاله معتبر نیست.");
        if (body is null || body.Trim().Length > BodyMaxLength)
            throw new InvalidOperationException("بدنهٔ مقاله معتبر نیست.");
        if (string.IsNullOrWhiteSpace(authorDisplayName) || authorDisplayName.Trim().Length > AuthorDisplayNameMaxLength)
            throw new InvalidOperationException("نام نمایشی نویسنده معتبر نیست.");
        if (string.IsNullOrWhiteSpace(locale) || locale.Trim().Length > LocaleMaxLength)
            throw new InvalidOperationException("locale مقاله معتبر نیست.");
        if (seoTitle is not null && seoTitle.Trim().Length > SeoTitleMaxLength)
            throw new InvalidOperationException("عنوان SEO مقاله معتبر نیست.");
        if (seoDescription is not null && seoDescription.Trim().Length > SeoDescriptionMaxLength)
            throw new InvalidOperationException("توضیح SEO مقاله معتبر نیست.");
        if (category is not null && category.Trim().Length > CategoryMaxLength)
            throw new InvalidOperationException("دستهٔ مقاله معتبر نیست.");
    }
}
