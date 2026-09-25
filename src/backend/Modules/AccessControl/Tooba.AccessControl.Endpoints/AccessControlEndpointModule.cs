using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Tooba.AccessControl.Endpoints.Admin;
using Tooba.AccessControl.Endpoints.Seller;

namespace Tooba.AccessControl.Endpoints;

/// <summary>ترکیب نازک مالکیت HTTP ماژول Access Control.</summary>
public static class AccessControlEndpointModule
{
    /// <summary>مسیرهای ماژول Access Control را ثبت می‌کند.</summary>
    /// <param name="app">سازندهٔ مسیر.</param>
    /// <returns>همان سازندهٔ مسیر برای زنجیره‌سازی.</returns>
    public static IEndpointRouteBuilder MapAccessControlModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var admin = app.MapGroup("/v1/admin/access-control");
        AccessControlAdminEndpoints.Map(admin);
        var seller = app.MapGroup("/v1/seller/access-control");
        AccessControlSellerEndpoints.Map(seller);
        return app;
    }
}
