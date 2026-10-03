using Tooba.Media.Application.Models;

namespace Tooba.Media.Application.Ports;

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

    /// <summary>کتابخانهٔ دارایی‌ها را با جستجوی اختیاری و فیلتر ContentType صفحه می‌کند.</summary>
    Task<MediaPagedResult<MediaAssetInfo>> QueryAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string? contentTypePrefix = null);

    /// <summary>فرادادهٔ یک دارایی Ready را برمی‌گرداند.</summary>
    Task<MediaAssetInfo?> GetAsync(Guid mediaAssetId, CancellationToken cancellationToken);

    /// <summary>فرادادهٔ چند دارایی را برمی‌گرداند.</summary>
    Task<IReadOnlyList<MediaAssetInfo>> GetManyAsync(
        IReadOnlyList<Guid> mediaAssetIds,
        CancellationToken cancellationToken);

    /// <summary>کلید ذخیره‌سازی داخلی برای ارائهٔ باینری.</summary>
    Task<string?> GetStorageKeyAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}
