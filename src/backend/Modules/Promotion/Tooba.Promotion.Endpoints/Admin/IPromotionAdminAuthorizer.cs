using Microsoft.AspNetCore.Http; namespace Tooba.Promotion.Endpoints.Admin;
public interface IPromotionAdminAuthorizer { Task RequireAuthorizedAsync(HttpContext context,CancellationToken cancellationToken); }

