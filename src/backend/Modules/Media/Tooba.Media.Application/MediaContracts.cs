namespace Tooba.Media.Application;

/// <summary>نتیجهٔ صفحه‌بندی‌شدهٔ عمومی Media.</summary>
public sealed record MediaPagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);

/// <summary>DTO عمومی فرادادهٔ دارایی رسانه برای Admin و ارجاعات مات.</summary>
public sealed record MediaAssetInfo(
    Guid MediaAssetId,
    string OriginalFileName,
    string ContentType,
    long ByteSize,
    int? Width,
    int? Height,
    DateTimeOffset CreatedAt,
    string? DisplayUrl = null);

/// <summary>ذخیره‌ساز باینری محلی یا ابری برای کلیدهای نسبی امن.</summary>
public interface IMediaObjectStore
{
    /// <summary>جریان را زیر کلید داده‌شده ذخیره می‌کند.</summary>
    Task SaveAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken);

    /// <summary>جریان خواندن برای کلید موجود برمی‌گرداند؛ در نبود null.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>وجود کلید را بررسی می‌کند.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);

    /// <summary>کلید را در صورت وجود حذف می‌کند.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

/// <summary>دایرکتوری canonical Media برای آپلود و پرس‌وجوی فراداده.</summary>
public interface IMediaDirectory
{
    /// <summary>جریان را اعتبارسنجی، ذخیره و به‌عنوان دارایی Ready ثبت می‌کند.</summary>
    Task<MediaAssetInfo> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        Guid? actorUserId,
        CancellationToken cancellationToken);

    /// <summary>کتابخانهٔ دارایی‌ها را با جستجوی اختیاری صفحه می‌کند.</summary>
    Task<MediaPagedResult<MediaAssetInfo>> QueryAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>فرادادهٔ یک دارایی Ready را برمی‌گرداند.</summary>
    Task<MediaAssetInfo?> GetAsync(Guid mediaAssetId, CancellationToken cancellationToken);

    /// <summary>فرادادهٔ چند دارایی را برمی‌گرداند.</summary>
    Task<IReadOnlyList<MediaAssetInfo>> GetManyAsync(
        IReadOnlyList<Guid> mediaAssetIds,
        CancellationToken cancellationToken);

    /// <summary>کلید ذخیره‌سازی داخلی برای ارائهٔ باینری؛ فقط برای Host.</summary>
    Task<string?> GetStorageKeyAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}
