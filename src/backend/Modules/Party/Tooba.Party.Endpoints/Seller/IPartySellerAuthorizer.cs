using Microsoft.AspNetCore.Http;

namespace Tooba.Party.Endpoints.Seller;

/// <summary>
/// درز خنثی احراز و قابلیت فروشنده برای مسیرهای تنظیمات Party.
/// <para>
/// پیاده‌سازی در Host است. Party هیچ سیاست امنیت پلتفرم را مالک نیست؛ این درز فقط
/// Actor مجاز و نتیجهٔ قابلیت نام‌دار را عبور می‌دهد.
/// </para>
/// </summary>
public interface IPartySellerAuthorizer
{
    /// <summary>
    /// Actor احرازشده و SellerPartyId مجاز را همراه با نتیجهٔ قابلیت مدیریت برمی‌گرداند.
    /// <c>seller.settings.view</c> الزامی است؛ نبود <c>seller.settings.manage</c> فقط
    /// <c>CanManage=false</c> می‌دهد و مسیر خواندن را رد نمی‌کند (رفتار قبلی Host).
    /// </summary>
    Task<(Guid ActorUserId, Guid SellerPartyId, bool CanManage)> RequireViewAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Actor احرازشده و SellerPartyId مجاز را برمی‌گرداند و قابلیت
    /// <c>seller.settings.manage</c> را الزام می‌کند؛ در نبود آن ۴۰۳ fail-closed است.
    /// </summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireManageAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);
}
