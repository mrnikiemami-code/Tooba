#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Promotion.Endpoints.Seller;
namespace Tooba.Host.Seller;
public sealed class HostPromotionSellerAuthorizer:IPromotionSellerAuthorizer
{
 public async Task<Guid> RequireSellerPartyIdAsync(HttpContext context,CancellationToken cancellationToken){var session=context.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();var guard=context.RequestServices.GetRequiredService<IAuthorizationGuard>();var environment=context.RequestServices.GetRequiredService<IHostEnvironment>();var (_,sellerPartyId)=await SellerPanelAccess.RequireAuthorizedAsync(context.Request,session,guard,environment,cancellationToken);return sellerPartyId;}
}

