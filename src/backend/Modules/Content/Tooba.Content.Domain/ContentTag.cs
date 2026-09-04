using Tooba.BuildingBlocks;

namespace Tooba.Content.Domain;

/// <summary>برچسب محتوا — مالک Content، مستقل از Catalog.</summary>
public sealed class ContentTag
{
    /// <summary>حداکثر طول کد زبان.</summary>
    public const int LanguageCodeMaxLength = 16;
    /// <summary>حداکثر طول نام نمایشی.</summary>
    public const int NameMaxLength = 100;
    /// <summary>حداکثر طول نام نرمال‌شده.</summary>
    public const int NormalizedNameMaxLength = 100;
    /// <summary>حداکثر طول slug اختیاری.</summary>
    public const int SlugMaxLength = 128;

    private ContentTag() { }

    /// <summary>شناسهٔ پایدار برچسب.</summary>
    public Guid TagId { get; init; }
    /// <summary>کد زبان مالک.</summary>
    public string LanguageCode { get; private set; } = string.Empty;
    /// <summary>نام نمایشی.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>نام نرمال‌شده برای یکتایی.</summary>
    public string NormalizedName { get; private set; } = string.Empty;
    /// <summary>slug اختیاری (فعلاً بدون مسیر عمومی).</summary>
    public string? Slug { get; private set; }
    /// <summary>فعال بودن برای انتساب جدید.</summary>
    public bool IsActive { get; private set; }
    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>برچسب جدید می‌سازد.</summary>
    public static ContentTag Create(
        string languageCode,
        string name,
        DateTimeOffset now,
        string? slug = null)
    {
        Validate(languageCode, name);
        var trimmedName = name.Trim();
        return new ContentTag
        {
            TagId = UuidV7.New(),
            LanguageCode = NormalizeLanguageCode(languageCode),
            Name = trimmedName,
            NormalizedName = NormalizeName(trimmedName),
            Slug = string.IsNullOrWhiteSpace(slug) ? null : NormalizeSlug(slug),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>برچسب را غیرفعال می‌کند.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>کد زبان را نرمال می‌کند.</summary>
    public static string NormalizeLanguageCode(string code) => code.Trim();

    /// <summary>نام را برای یکتایی نرمال می‌کند.</summary>
    public static string NormalizeName(string name) =>
        string.Join(' ', name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToLowerInvariant();

    /// <summary>slug را نرمال می‌کند.</summary>
    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();

    private static void Validate(string languageCode, string name)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || languageCode.Trim().Length > LanguageCodeMaxLength)
        {
            throw new InvalidOperationException(ContentTagErrorCodes.InvalidLanguage);
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            throw new InvalidOperationException(ContentTagErrorCodes.InvalidName);
        }
    }
}

/// <summary>انتساب many-to-many برچسب به مقاله.</summary>
public sealed class ArticleTag
{
    private ArticleTag() { }

    /// <summary>شناسهٔ مقاله.</summary>
    public Guid ArticleId { get; init; }
    /// <summary>شناسهٔ برچسب.</summary>
    public Guid TagId { get; init; }
    /// <summary>زمان انتساب.</summary>
    public DateTimeOffset AssignedAt { get; init; }

    /// <summary>انتساب جدید می‌سازد.</summary>
    public static ArticleTag Create(Guid articleId, Guid tagId, DateTimeOffset now) =>
        new()
        {
            ArticleId = articleId,
            TagId = tagId,
            AssignedAt = now,
        };
}

/// <summary>کدهای خطای پایدار برچسب محتوا.</summary>
public static class ContentTagErrorCodes
{
    /// <summary>برچسب یافت نشد.</summary>
    public const string NotFound = "content.tag.not_found";
    /// <summary>نام تکراری در زبان.</summary>
    public const string DuplicateName = "content.tag.duplicate_name";
    /// <summary>زبان برچسب با مقاله نمی‌خواند.</summary>
    public const string LanguageMismatch = "content.tag.language_mismatch";
    /// <summary>برچسب غیرفعال برای انتساب جدید.</summary>
    public const string Inactive = "content.tag.inactive";
    /// <summary>زبان نامعتبر.</summary>
    public const string InvalidLanguage = "content.tag.invalid_language";
    /// <summary>نام نامعتبر.</summary>
    public const string InvalidName = "content.tag.invalid_name";
    /// <summary>مقاله یافت نشد.</summary>
    public const string ArticleNotFound = "content.tag.article_not_found";
}
