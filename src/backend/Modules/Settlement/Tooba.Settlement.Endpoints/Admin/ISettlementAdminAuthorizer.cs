using Microsoft.AspNetCore.Http;

namespace Tooba.Settlement.Endpoints.Admin;

/// <summary>
/// احراز Actor admin برای مسیرهای Settlement؛ پیاده‌سازی در Host.
/// </summary>
public interface ISettlementAdminAuthorizer
{
    /// <summary>
    /// Actor مدیر مجاز را پس از بررسی مجوز برمی‌گرداند.
    /// </summary>
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
