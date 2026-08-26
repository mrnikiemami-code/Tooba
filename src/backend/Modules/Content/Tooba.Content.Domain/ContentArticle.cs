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

/// <summary>مقالهٔ تحریری برای ریل خانه و مسیرهای عمومی آینده.</summary>
public sealed class ContentArticle
{
    /// <summary>حداکثر طول slug.</summary>
    public const int SlugMaxLength = 128;
    /// <summary>حداکثر طول عنوان.</summary>
    public const int TitleMaxLength = 200;
    /// <summary>حداکثر طول چکیده.</summary>
    public const int ExcerptMaxLength = 500;
    /// <summary>حداکثر طول نام نمایشی نویسنده.</summary>
    public const int AuthorDisplayNameMaxLength = 100;

    private ContentArticle() { }

    /// <summary>شناسهٔ پایدار مقاله.</summary>
    public Guid ArticleId { get; init; }
    /// <summary>slug پایدار برای URL.</summary>
    public string Slug { get; private set; } = string.Empty;
    /// <summary>عنوان فارسی.</summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>چکیدهٔ کوتاه.</summary>
    public string Excerpt { get; private set; } = string.Empty;
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
        Guid? coverMediaAssetId,
        string authorDisplayName,
        IReadOnlyList<string> tags,
        bool isFeatured,
        DateTimeOffset publishDate,
        DateTimeOffset now)
    {
        Validate(slug, title, excerpt, authorDisplayName);
        return new ContentArticle
        {
            ArticleId = UuidV7.New(),
            Slug = slug.Trim().ToLowerInvariant(),
            Title = title.Trim(),
            Excerpt = excerpt.Trim(),
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

    /// <summary>مقاله را برای نمایش عمومی منتشر می‌کند.</summary>
    public void Publish(DateTimeOffset now)
    {
        Status = ContentPublicationStatus.Published;
        UpdatedAt = now;
    }

    private static void Validate(string slug, string title, string excerpt, string authorDisplayName)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > SlugMaxLength)
            throw new InvalidOperationException("slug مقاله معتبر نیست.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > TitleMaxLength)
            throw new InvalidOperationException("عنوان مقاله معتبر نیست.");
        if (string.IsNullOrWhiteSpace(excerpt) || excerpt.Trim().Length > ExcerptMaxLength)
            throw new InvalidOperationException("چکیدهٔ مقاله معتبر نیست.");
        if (string.IsNullOrWhiteSpace(authorDisplayName) || authorDisplayName.Trim().Length > AuthorDisplayNameMaxLength)
            throw new InvalidOperationException("نام نمایشی نویسنده معتبر نیست.");
    }
}
