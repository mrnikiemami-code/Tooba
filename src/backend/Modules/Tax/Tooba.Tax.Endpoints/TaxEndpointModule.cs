using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Tooba.Tax.Endpoints;

/// <summary>
/// ترکیب باریک مسیرهای HTTP مالک Tax.
/// در وضعیت فعلی هیچ مسیر عمومی Tax در Host وجود نداشت؛ این ماژول نقطهٔ ترکیب آینده است.
/// </summary>
public static class TaxEndpointModule
{
    /// <summary>
    /// مسیرهای Tax را ثبت می‌کند (فعلاً بدون route عمومی؛ مالکیت Endpoints برقرار است).
    /// </summary>
    public static IEndpointRouteBuilder MapTaxModule(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        // No public Tax HTTP routes existed in Host to extract.
        // Keep composition point for HOST-MODULE-ENDPOINT-001 compliance.
        _ = app.MapGroup("/v1/tax");
        return app;
    }
}
