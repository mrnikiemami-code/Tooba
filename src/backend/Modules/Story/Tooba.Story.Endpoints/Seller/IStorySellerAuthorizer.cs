namespace Tooba.Story.Endpoints.Seller;

/// <summary>
/// درز مجوز خنثی برای بخش فروشندهٔ Story.
/// پیاده‌سازی آن در Host و بر پایهٔ <c>ISellerPanelAccess</c> انجام می‌شود؛ بنابراین این پروژه
/// هیچ وابستگی مستقیمی به Host ندارد و در زمان استخراج میکروسرویس فقط همین درز باید تأمین شود.
/// </summary>
public interface IStorySellerAuthorizer
{
    /// <summary>
    /// هویت فروشندهٔ مجاز را الزامی می‌کند و شناسهٔ کاربر و شناسهٔ party فروشنده را برمی‌گرداند.
    /// در صورت نبود دسترسی، به‌صورت fail-closed خطا صادر می‌شود و هیچ مقدار پیش‌فرضی ساخته نمی‌شود.
    /// </summary>
    /// <param name="httpContext">زمینهٔ HTTP درخواست جاری.</param>
    /// <param name="cancellationToken">توکن لغو درخواست.</param>
    /// <returns>جفت شناسهٔ کاربر actor و شناسهٔ party فروشنده.</returns>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        Microsoft.AspNetCore.Http.HttpContext httpContext,
        CancellationToken cancellationToken);
}
