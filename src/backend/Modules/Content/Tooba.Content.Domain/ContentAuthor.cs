using Tooba.BuildingBlocks;

namespace Tooba.Content.Domain;

/// <summary>نویسندهٔ مقاله — مالک Content.</summary>
public sealed class ContentAuthor
{
    /// <summary>حداکثر طول نام نمایشی.</summary>
    public const int DisplayNameMaxLength = 200;
    /// <summary>حداکثر طول slug.</summary>
    public const int SlugMaxLength = 128;
    /// <summary>حداکثر طول بیوگرافی کوتاه.</summary>
    public const int ShortBioMaxLength = 500;
    /// <summary>حداکثر طول بیوگرافی کامل.</summary>
    public const int FullBioMaxLength = 4000;
    /// <summary>حداکثر طول URL.</summary>
    public const int UrlMaxLength = 500;

    private ContentAuthor() { }

    /// <summary>شناسهٔ پایدار نویسنده.</summary>
    public Guid AuthorId { get; init; }
    /// <summary>نام نمایشی.</summary>
    public string DisplayName { get; private set; } = string.Empty;
    /// <summary>slug یکتا در کل سیستم.</summary>
    public string Slug { get; private set; } = string.Empty;
    /// <summary>فعال بودن برای انتساب جدید.</summary>
    public bool IsActive { get; private set; }
    /// <summary>مرجع تصویر پروفایل DAM.</summary>
    public Guid? ProfileImageMediaAssetId { get; private set; }
    /// <summary>مرجع تصویر کاور DAM.</summary>
    public Guid? CoverImageMediaAssetId { get; private set; }
    /// <summary>بیوگرافی کوتاه.</summary>
    public string? ShortBio { get; private set; }
    /// <summary>بیوگرافی کامل.</summary>
    public string? FullBio { get; private set; }
    /// <summary>آدرس وب‌سایت.</summary>
    public string? WebsiteUrl { get; private set; }
    /// <summary>آدرس اینستاگرام.</summary>
    public string? InstagramUrl { get; private set; }
    /// <summary>آدرس توییتر/X.</summary>
    public string? TwitterUrl { get; private set; }
    /// <summary>آدرس لینکدین.</summary>
    public string? LinkedInUrl { get; private set; }
    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>نویسندهٔ جدید می‌سازد.</summary>
    public static ContentAuthor Create(
        string displayName,
        string slug,
        string? shortBio,
        string? fullBio,
        Guid? profileImageMediaAssetId,
        Guid? coverImageMediaAssetId,
        string? websiteUrl,
        string? instagramUrl,
        string? twitterUrl,
        string? linkedInUrl,
        DateTimeOffset now)
    {
        ValidateFields(displayName, slug, shortBio, fullBio, websiteUrl, instagramUrl, twitterUrl, linkedInUrl);
        return new ContentAuthor
        {
            AuthorId = UuidV7.New(),
            DisplayName = displayName.Trim(),
            Slug = NormalizeSlug(slug),
            IsActive = true,
            ProfileImageMediaAssetId = profileImageMediaAssetId,
            CoverImageMediaAssetId = coverImageMediaAssetId,
            ShortBio = NormalizeOptional(shortBio, ShortBioMaxLength),
            FullBio = NormalizeOptional(fullBio, FullBioMaxLength),
            WebsiteUrl = NormalizeOptional(websiteUrl, UrlMaxLength),
            InstagramUrl = NormalizeOptional(instagramUrl, UrlMaxLength),
            TwitterUrl = NormalizeOptional(twitterUrl, UrlMaxLength),
            LinkedInUrl = NormalizeOptional(linkedInUrl, UrlMaxLength),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>فیلدهای عمومی را به‌روزرسانی می‌کند.</summary>
    public void Update(
        string displayName,
        string slug,
        string? shortBio,
        string? fullBio,
        Guid? profileImageMediaAssetId,
        Guid? coverImageMediaAssetId,
        string? websiteUrl,
        string? instagramUrl,
        string? twitterUrl,
        string? linkedInUrl,
        DateTimeOffset now)
    {
        ValidateFields(displayName, slug, shortBio, fullBio, websiteUrl, instagramUrl, twitterUrl, linkedInUrl);
        DisplayName = displayName.Trim();
        Slug = NormalizeSlug(slug);
        ProfileImageMediaAssetId = profileImageMediaAssetId;
        CoverImageMediaAssetId = coverImageMediaAssetId;
        ShortBio = NormalizeOptional(shortBio, ShortBioMaxLength);
        FullBio = NormalizeOptional(fullBio, FullBioMaxLength);
        WebsiteUrl = NormalizeOptional(websiteUrl, UrlMaxLength);
        InstagramUrl = NormalizeOptional(instagramUrl, UrlMaxLength);
        TwitterUrl = NormalizeOptional(twitterUrl, UrlMaxLength);
        LinkedInUrl = NormalizeOptional(linkedInUrl, UrlMaxLength);
        UpdatedAt = now;
    }

    /// <summary>نویسنده را غیرفعال می‌کند.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>slug را نرمال می‌کند.</summary>
    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();

    private static void ValidateFields(
        string displayName,
        string slug,
        string? shortBio,
        string? fullBio,
        string? websiteUrl,
        string? instagramUrl,
        string? twitterUrl,
        string? linkedInUrl)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > DisplayNameMaxLength)
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.InvalidDisplayName);
        }

        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > SlugMaxLength)
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.InvalidSlug);
        }

        if (shortBio is not null && shortBio.Trim().Length > ShortBioMaxLength)
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.InvalidShortBio);
        }

        if (fullBio is not null && fullBio.Trim().Length > FullBioMaxLength)
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.InvalidFullBio);
        }

        ValidateOptionalUrl(websiteUrl);
        ValidateOptionalUrl(instagramUrl);
        ValidateOptionalUrl(twitterUrl);
        ValidateOptionalUrl(linkedInUrl);
    }

    private static void ValidateOptionalUrl(string? value)
    {
        if (value is not null && value.Trim().Length > UrlMaxLength)
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.InvalidUrl);
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength
            ? throw new InvalidOperationException(ContentAuthorErrorCodes.InvalidField)
            : trimmed;
    }
}

/// <summary>کدهای خطای پایدار نویسندهٔ مقاله.</summary>
public static class ContentAuthorErrorCodes
{
    /// <summary>نویسنده یافت نشد.</summary>
    public const string NotFound = "content.author.not_found";
    /// <summary>slug تکراری.</summary>
    public const string SlugDuplicate = "content.author.slug_duplicate";
    /// <summary>نویسنده غیرفعال است.</summary>
    public const string Inactive = "content.author.inactive";
    /// <summary>نویسنده برای انتشار الزامی است.</summary>
    public const string RequiredForPublish = "content.author.required_for_publish";
    /// <summary>نام نمایشی نامعتبر.</summary>
    public const string InvalidDisplayName = "content.author.invalid_display_name";
    /// <summary>slug نامعتبر.</summary>
    public const string InvalidSlug = "content.author.invalid_slug";
    /// <summary>بیوگرافی کوتاه نامعتبر.</summary>
    public const string InvalidShortBio = "content.author.invalid_short_bio";
    /// <summary>بیوگرافی کامل نامعتبر.</summary>
    public const string InvalidFullBio = "content.author.invalid_full_bio";
    /// <summary>URL نامعتبر.</summary>
    public const string InvalidUrl = "content.author.invalid_url";
    /// <summary>فیلد نامعتبر.</summary>
    public const string InvalidField = "content.author.invalid_field";
}
