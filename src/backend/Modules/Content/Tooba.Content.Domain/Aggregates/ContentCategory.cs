using Tooba.BuildingBlocks;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Domain.Aggregates;

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
            throw new ContractOperationException(ContentErrorCodes.CategoryInvalidLanguage);
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            throw new ContractOperationException(ContentErrorCodes.CategoryInvalidName);
        }

        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > SlugMaxLength)
        {
            throw new ContractOperationException(ContentErrorCodes.CategoryInvalidSlug);
        }

        if (shortDescription is not null && shortDescription.Trim().Length > ShortDescriptionMaxLength)
        {
            throw new ContractOperationException(ContentErrorCodes.CategoryInvalidShortDescription);
        }

        if (description is not null && description.Trim().Length > DescriptionMaxLength)
        {
            throw new ContractOperationException(ContentErrorCodes.CategoryInvalidDescription);
        }

        if (seoTitle is not null && seoTitle.Trim().Length > SeoTitleMaxLength)
        {
            throw new ContractOperationException(ContentErrorCodes.CategoryInvalidSeoTitle);
        }

        if (seoDescription is not null && seoDescription.Trim().Length > SeoDescriptionMaxLength)
        {
            throw new ContractOperationException(ContentErrorCodes.CategoryInvalidSeoDescription);
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? throw new ContractOperationException(ContentErrorCodes.CategoryInvalidField) : trimmed;
    }
}
