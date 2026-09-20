using Microsoft.AspNetCore.Http;

namespace Tooba.Offer.Endpoints.Seller;

/// <summary>
/// احراز Actor/Seller برای مسیرهای Offer؛ پیاده‌سازی در Host.
/// </summary>
public interface IOfferSellerAuthorizer
{
    /// <summary>
    /// Actor و SellerParty مجاز را پس از بررسی مجوز برمی‌گرداند.
    /// </summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
