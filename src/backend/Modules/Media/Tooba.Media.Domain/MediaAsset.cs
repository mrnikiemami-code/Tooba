using Tooba.BuildingBlocks;

namespace Tooba.Media.Domain;

/// <summary>وضعیت پردازش دارایی رسانه.</summary>
public enum MediaAssetStatus
{
    /// <summary>باینری و فراداده آمادهٔ ارائه است.</summary>
    Ready = 0,

    /// <summary>آپلود یا ذخیره‌سازی ناموفق بوده است.</summary>
    Failed = 1,
}

/// <summary>دارایی رسانهٔ canonical؛ مالک باینری و فراداده در schema مستقل Media.</summary>
public sealed class MediaAsset
{
    /// <summary>حداکثر طول نام فایل اصلی.</summary>
    public const int OriginalFileNameMaxLength = 255;

    /// <summary>حداکثر طول کلید ذخیره‌سازی نسبی.</summary>
    public const int StorageKeyMaxLength = 512;

    /// <summary>حداکثر طول نوع محتوا.</summary>
    public const int ContentTypeMaxLength = 128;

    /// <summary>طول ثابت هش SHA-256 به صورت hex.</summary>
    public const int ChecksumSha256Length = 64;

    private MediaAsset()
    {
    }

    /// <summary>شناسهٔ پایدار دارایی.</summary>
    public Guid MediaAssetId { get; init; }

    /// <summary>کلید نسبی داخل object store؛ هرگز از نام کلاینت ساخته نمی‌شود.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    /// <summary>نام فایل اصلی برای نمایش Admin.</summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>MIME نرمال‌شده.</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>اندازهٔ بایت ذخیره‌شده.</summary>
    public long ByteSize { get; private set; }

    /// <summary>عرض پیکسل در صورت استخراج موفق؛ وگرنه null.</summary>
    public int? Width { get; private set; }

    /// <summary>ارتفاع پیکسل در صورت استخراج موفق؛ وگرنه null.</summary>
    public int? Height { get; private set; }

    /// <summary>هش SHA-256 هگزادسیمال اختیاری.</summary>
    public string? ChecksumSha256 { get; private set; }

    /// <summary>وضعیت Ready/Failed.</summary>
    public MediaAssetStatus Status { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>شناسهٔ کاربر آپلودکننده در صورت موجود بودن.</summary>
    public Guid? CreatedByUserId { get; init; }

    /// <summary>دارایی Ready جدید می‌سازد؛ شناسه اختیاری برای هم‌ترازی با کلید ذخیره‌سازی است.</summary>
    public static MediaAsset CreateReady(
        string storageKey,
        string originalFileName,
        string contentType,
        long byteSize,
        string? checksumSha256,
        int? width,
        int? height,
        Guid? createdByUserId,
        DateTimeOffset now,
        Guid? mediaAssetId = null)
    {
        Validate(storageKey, originalFileName, contentType, byteSize, checksumSha256);
        return new MediaAsset
        {
            MediaAssetId = mediaAssetId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            StorageKey = storageKey.Trim(),
            OriginalFileName = originalFileName.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            ByteSize = byteSize,
            Width = width,
            Height = height,
            ChecksumSha256 = checksumSha256,
            Status = MediaAssetStatus.Ready,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = createdByUserId,
        };
    }

    /// <summary>دارایی را Failed علامت می‌زند.</summary>
    public void MarkFailed(DateTimeOffset now)
    {
        Status = MediaAssetStatus.Failed;
        UpdatedAt = now;
    }

    private static void Validate(
        string storageKey,
        string originalFileName,
        string contentType,
        long byteSize,
        string? checksumSha256)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Trim().Length > StorageKeyMaxLength)
            throw new InvalidOperationException("کلید ذخیره‌سازی رسانه معتبر نیست.");
        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Trim().Length > OriginalFileNameMaxLength)
            throw new InvalidOperationException("نام فایل رسانه معتبر نیست.");
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Trim().Length > ContentTypeMaxLength)
            throw new InvalidOperationException("نوع محتوای رسانه معتبر نیست.");
        if (byteSize <= 0)
            throw new InvalidOperationException("اندازهٔ فایل رسانه معتبر نیست.");
        if (checksumSha256 is not null
            && (checksumSha256.Length != ChecksumSha256Length
                || checksumSha256.Any(ch => !Uri.IsHexDigit(ch))))
            throw new InvalidOperationException("checksum رسانه معتبر نیست.");
    }
}
