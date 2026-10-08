using Microsoft.AspNetCore.Http;

namespace Tooba.Promotion.Endpoints.Seller;

/// <summary>
/// درز احراز هویت فروشنده برای مسیرهای Promotion (پیاده‌سازی در Host platform seam).
/// </summary>
public interface IPromotionSellerAuthorizer
{
    /// <summary>
    /// شناسهٔ Party فروشندهٔ احراز‌شده را برمی‌گرداند.
    /// </summary>
    Task<Guid> RequireSellerPartyIdAsync(HttpContext context, CancellationToken cancellationToken);
}
