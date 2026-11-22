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

    /// <summary>Requires seller scope plus the existing product.create capability.</summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireCreateAsync(HttpContext httpContext, CancellationToken cancellationToken);

    /// <summary>Requires seller scope plus the existing product.edit capability.</summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireEditAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

