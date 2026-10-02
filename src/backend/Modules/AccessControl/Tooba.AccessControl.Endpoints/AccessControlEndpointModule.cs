using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.AccessControl.Endpoints.Admin;
using Tooba.AccessControl.Endpoints.Errors;
using Tooba.AccessControl.Endpoints.Resources;
using Tooba.AccessControl.Endpoints.Seller;
using Tooba.AccessControl.Endpoints.Seller.Development;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.AccessControl.Endpoints;

/// <summary>ترکیب نازک مالکیت HTTP ماژول Access Control.</summary>
public static class AccessControlEndpointModule
{
    /// <summary>مسیرهای ماژول Access Control را ثبت می‌کند.</summary>
    public static IEndpointRouteBuilder MapAccessControlModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var admin = app.MapGroup("/v1/admin/access-control");
        AccessControlAdminEndpoints.Map(admin);
        var adminSeller = app.MapGroup("/v1/admin/sellers/{sellerId:guid}/access-control");
        AccessControlAdminSellerEndpoints.Map(adminSeller);
        var seller = app.MapGroup("/v1/seller/access-control");
        AccessControlSellerEndpoints.Map(seller);
        var sellerDevelopment = app.MapGroup("/v1/seller");
        SellerDevContextEndpoints.Map(sellerDevelopment);
        return app;
    }

    /// <summary>ثبت presentation seams (error catalog/resources).</summary>
    public static IServiceCollection AddAccessControlEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, AccessControlErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, AccessControlErrorResourceSet>();
        return services;
    }
}
