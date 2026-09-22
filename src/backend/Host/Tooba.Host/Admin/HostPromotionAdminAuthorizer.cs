#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Promotion.Endpoints.Admin;
namespace Tooba.Host.Admin;
public sealed class HostPromotionAdminAuthorizer:IPromotionAdminAuthorizer
{
 public async Task RequireAuthorizedAsync(HttpContext context,CancellationToken cancellationToken){var session=context.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();var tenant=context.RequestServices.GetRequiredService<ICurrentTenant>();var guard=context.RequestServices.GetRequiredService<IAuthorizationGuard>();var environment=context.RequestServices.GetRequiredService<IHostEnvironment>();await AdminPanelAccess.RequireAuthorizedAsync(context.Request,session,tenant,guard,environment,cancellationToken);}
}

