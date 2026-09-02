using Tooba.BuildingBlocks;

namespace Tooba.Content.Domain;

/// <summary>وضعیت دسته‌بندی مقاله.</summary>
public enum ContentCategoryStatus
{
    /// <summary>فعال و قابل انتساب.</summary>
    Active = 0,
    /// <summary>بایگانی‌شده.</summary>
    Archived = 1,
}

/// <summary>دسته‌بندی مقاله — مالک Content، مستقل از Catalog.</summary>
public sealed class ContentCategory
{
    /// <summary>حداکثر طول کد زبان.</summary>
    public const int LanguageCodeMaxLength = 16;
    /// <summary>حداکثر طول نام.</summary>
    public const int NameMaxLength = 200;
    /// <summary>حداکثر طول slug.</summary>
    public const int SlugMaxLength = 128;
    /// <summary>حداکثر طول توضیح کوتاه.</summary>
    public const int ShortDescriptionMaxLength = 500;
    /// <summary>حداکثر طول توضیح.</summary>
    public const int DescriptionMaxLength = 4000;
    /// <summary>حداکثر طول عنوان SEO.</summary>
    public const int SeoTitleMaxLength = 200;
    /// <summary>حداکثر طول توضیح SEO.</summary>
    public const int SeoDescriptionMaxLength = 500;

    private ContentCategory() { }

    /// <summary>شناسهٔ پایدار دسته.</summary>
    public Guid CategoryId { get; init; }
    /// <summary>کد زبان مالک.</summary>
    public string LanguageCode { get; private set; } = string.Empty;
    /// <summary>شناسهٔ والد اختیاری.</summary>
    public Guid? ParentCategoryId { get; private set; }
    /// <summary>نام نمایشی.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>slug یکتا در زبان.</summary>
    public string Slug { get; private set; } = string.Empty;
    /// <summary>توضیح کوتاه.</summary>
    public string? ShortDescription { get; private set; }
    /// <summary>توضیح کامل.</summary>
    public string? Description { get; private set; }
    /// <summary>وضعیت فعال/بایگانی.</summary>
    public ContentCategoryStatus Status { get; private set; }
    /// <summary>ترتیب نمایش.</summary>
    public int SortOrder { get; private set; }
    /// <summary>عنوان SEO.</summary>
    public string? SeoTitle { get; private set; }
    /// <summary>توضیح متا SEO.</summary>
    public string? SeoDescription { get; private set; }
    /// <summary>مرجع تصویر DAM.</summary>
    public Guid? ImageMediaAssetId { get; private set; }
    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>دستهٔ جدید می‌سازد.</summary>
    public static ContentCategory Create(
        string languageCode,
        Guid? parentCategoryId,
        string name,
        string slug,
        string? shortDescription,
        string? description,
        int sortOrder,
        string? seoTitle,
        string? seoDescription,
        Guid? imageMediaAssetId,
        DateTimeOffset now)
    {
        ValidateFields(languageCode, name, slug, shortDescription, description, seoTitle, seoDescription);
        return new ContentCategory
        {
            CategoryId = UuidV7.New(),
            LanguageCode = NormalizeLanguageCode(languageCode),
            ParentCategoryId = parentCategoryId,
            Name = name.Trim(),
            Slug = NormalizeSlug(slug),
            ShortDescription = NormalizeOptional(shortDescription, ShortDescriptionMaxLength),
            Description = NormalizeOptional(description, DescriptionMaxLength),
            Status = ContentCategoryStatus.Active,
            SortOrder = sortOrder,
            SeoTitle = NormalizeOptional(seoTitle, SeoTitleMaxLength),
            SeoDescription = NormalizeOptional(seoDescription, SeoDescriptionMaxLength),
            ImageMediaAssetId = imageMediaAssetId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای عمومی را به‌روزرسانی می‌کند.</summary>
    public void UpdateCore(
        string name,
        string slug,
        string? shortDescription,
        string? description,
        int sortOrder,
        ContentCategoryStatus status,
        DateTimeOffset now)
    {
        ValidateFields(LanguageCode, name, slug, shortDescription, description, SeoTitle, SeoDescription);
        Name = name.Trim();
        Slug = NormalizeSlug(slug);
        ShortDescription = NormalizeOptional(shortDescription, ShortDescriptionMaxLength);
        Description = NormalizeOptional(description, DescriptionMaxLength);
        SortOrder = sortOrder;
        Status = status;
        UpdatedAt = now;
    }

    /// <summary>متادیتای SEO را به‌روزرسانی می‌کند.</summary>
    public void UpdateSeo(string? seoTitle, string? seoDescription, DateTimeOffset now)
    {
        SeoTitle = NormalizeOptional(seoTitle, SeoTitleMaxLength);
        SeoDescription = NormalizeOptional(seoDescription, SeoDescriptionMaxLength);
        UpdatedAt = now;
    }

    /// <summary>والد را تنظیم می‌کند.</summary>
    public void SetParent(Guid? parentCategoryId, DateTimeOffset now)
    {
        ParentCategoryId = parentCategoryId;
        UpdatedAt = now;
    }

    /// <summary>تصویر DAM را تنظیم می‌کند.</summary>
    public void SetImage(Guid? imageMediaAssetId, DateTimeOffset now)
    {
        ImageMediaAssetId = imageMediaAssetId;
        UpdatedAt = now;
    }

    /// <summary>دسته را بایگانی می‌کند.</summary>
    public void Archive(DateTimeOffset now)
    {
        Status = ContentCategoryStatus.Archived;
        UpdatedAt = now;
    }

    /// <summary>کد زبان را نرمال می‌کند.</summary>
    public static string NormalizeLanguageCode(string code) => code.Trim();

    /// <summary>slug را نرمال می‌کند.</summary>
    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();

    private static void ValidateFields(
        string languageCode,
        string name,
        string slug,
        string? shortDescription,
        string? description,
        string? seoTitle,
        string? seoDescription)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || languageCode.Trim().Length > LanguageCodeMaxLength)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidLanguage);
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidName);
        }

        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > SlugMaxLength)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidSlug);
        }

        if (shortDescription is not null && shortDescription.Trim().Length > ShortDescriptionMaxLength)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidShortDescription);
        }

        if (description is not null && description.Trim().Length > DescriptionMaxLength)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidDescription);
        }

        if (seoTitle is not null && seoTitle.Trim().Length > SeoTitleMaxLength)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidSeoTitle);
        }

        if (seoDescription is not null && seoDescription.Trim().Length > SeoDescriptionMaxLength)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidSeoDescription);
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidField) : trimmed;
    }
}

/// <summary>کدهای خطای پایدار دسته‌بندی مقاله.</summary>
public static class ContentCategoryErrorCodes
{
    /// <summary>دسته یافت نشد.</summary>
    public const string NotFound = "content.category.not_found";
    /// <summary>slug تکراری در زبان.</summary>
    public const string SlugDuplicate = "content.category.slug_duplicate";
    /// <summary>چرخه در درخت.</summary>
    public const string CycleDetected = "content.category.cycle_detected";
    /// <summary>والد زبان متفاوت.</summary>
    public const string CrossLanguageParent = "content.category.cross_language_parent";
    /// <summary>والد خود گره.</summary>
    public const string SelfParent = "content.category.self_parent";
    /// <summary>والد از نسل فرزند.</summary>
    public const string DescendantParent = "content.category.descendant_parent";
    /// <summary>زبان مقاله با دسته نمی‌خواند.</summary>
    public const string LanguageMismatch = "content.category.language_mismatch";
    /// <summary>دسته مقاله دارد.</summary>
    public const string HasArticles = "content.category.has_articles";
    /// <summary>دسته فرزند دارد.</summary>
    public const string HasChildren = "content.category.has_children";
    /// <summary>زبان نامعتبر.</summary>
    public const string InvalidLanguage = "content.category.invalid_language";
    /// <summary>نام نامعتبر.</summary>
    public const string InvalidName = "content.category.invalid_name";
    /// <summary>slug نامعتبر.</summary>
    public const string InvalidSlug = "content.category.invalid_slug";
    /// <summary>توضیح کوتاه نامعتبر.</summary>
    public const string InvalidShortDescription = "content.category.invalid_short_description";
    /// <summary>توضیح نامعتبر.</summary>
    public const string InvalidDescription = "content.category.invalid_description";
    /// <summary>عنوان SEO نامعتبر.</summary>
    public const string InvalidSeoTitle = "content.category.invalid_seo_title";
    /// <summary>توضیح SEO نامعتبر.</summary>
    public const string InvalidSeoDescription = "content.category.invalid_seo_description";
    /// <summary>فیلد نامعتبر.</summary>
    public const string InvalidField = "content.category.invalid_field";
}
