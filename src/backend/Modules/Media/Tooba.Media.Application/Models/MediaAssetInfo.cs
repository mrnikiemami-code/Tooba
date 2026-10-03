namespace Tooba.Media.Application.Models;

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
    string? DisplayUrl = null,
    double? FocalPointX = null,
    double? FocalPointY = null);
