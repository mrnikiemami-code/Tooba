using Microsoft.AspNetCore.Http; namespace Tooba.Promotion.Endpoints.Seller;
public interface IPromotionSellerAuthorizer { Task<Guid> RequireSellerPartyIdAsync(HttpContext context,CancellationToken cancellationToken); }

