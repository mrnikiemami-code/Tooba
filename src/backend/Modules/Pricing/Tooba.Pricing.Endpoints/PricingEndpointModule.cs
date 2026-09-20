using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Tooba.Pricing.Endpoints;

/// <summary>
/// ترکیب باریک مسیرهای HTTP مالک Pricing.
/// مسیر عمومی Pricing در Host وجود نداشت؛ seller price write در Offer.Endpoints باقی است.
/// </summary>
public static class PricingEndpointModule
{
    /// <summary>
    /// نقطهٔ ترکیب Pricing را ثبت می‌کند (بدون route عمومی فعلی).
    /// </summary>
    public static IEndpointRouteBuilder MapPricingModule(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        _ = app.MapGroup("/v1/pricing");
        return app;
    }
}
