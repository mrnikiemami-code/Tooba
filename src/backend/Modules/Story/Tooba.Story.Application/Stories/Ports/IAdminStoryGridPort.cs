using Tooba.BuildingBlocks.Grid;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Domain.Enums;

namespace Tooba.Story.Application.Stories.Ports;

/// <summary>
/// درز (port) گرید استوری Admin با صفحه‌بندی DB-native؛ پیاده‌سازی آن در Infrastructure قرار دارد
/// و Endpoints هرگز مستقیماً به آن دسترسی ندارد. نگاشت خطاهای شکل درخواست گرید به عهدهٔ پیاده‌سازی است.
/// </summary>
public interface IAdminStoryGridPort
{
    /// <summary>صفحه‌بندی server-side گرید استوری Admin در محدودهٔ tenant داده‌شده.</summary>
    /// <param name="tenantId">شناسهٔ tenant مؤثر که از درز مجوز resolve شده است.</param>
    /// <param name="reviewStatus">فیلتر اختیاری وضعیت بازبینی.</param>
    /// <param name="request">درخواست گرید که باید توسط پیاده‌سازی normalize شود.</param>
    /// <param name="cancellationToken">توکن لغو درخواست.</param>
    /// <returns>صفحهٔ نتیجهٔ گرید به همراه فرادادهٔ صفحه‌بندی.</returns>
    Task<GridPageResponse<AdminStorySnapshot>> QueryAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        GridQueryRequest request,
        CancellationToken cancellationToken);
}
