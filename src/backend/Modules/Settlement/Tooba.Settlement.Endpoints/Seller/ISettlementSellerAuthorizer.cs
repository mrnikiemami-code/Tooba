using Microsoft.AspNetCore.Http;

namespace Tooba.Settlement.Endpoints.Seller;

/// <summary>
/// احراز Actor/Seller برای مسیرهای Settlement؛ پیاده‌سازی در Host.
/// </summary>
public interface ISettlementSellerAuthorizer
{
    /// <summary>
    /// Actor و SellerParty مجاز را پس از بررسی مجوز برمی‌گرداند.
    /// </summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);
}
