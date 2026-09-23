using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Endpoints.Admin.Completeness;
using Tooba.Order.Endpoints.Admin.Customers;
using Tooba.Order.Endpoints.Admin.Detail;
using Tooba.Order.Endpoints.Admin.InventoryRecovery;
using Tooba.Order.Endpoints.Admin.Operations;
using Tooba.Order.Endpoints.Admin.OrdersGrid;
using Tooba.Order.Endpoints.Customer;
using Tooba.Order.Endpoints.Errors;
using Tooba.Order.Endpoints.Resources;
using Tooba.Order.Endpoints.Seller;
using Tooba.Order.Endpoints.Storefront;

namespace Tooba.Order.Endpoints;

public interface IOrderAdminAuthorizer
{
    Task<Guid> RequirePermissionAsync(
        HttpContext context,
        string permissionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// فقط مرز پنل مدیر (Tenant) بدون مجوز اضافه؛ برای مسیرهایی که پیش از انتقال هم
    /// همین بررسی را داشتند.
    /// </summary>
    Task<Guid> RequireAdminAsync(HttpContext context, CancellationToken cancellationToken);
}

public static class OrderEndpointModule
{
    public static IServiceCollection AddOrderEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, OrderErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, OrderErrorResourceSet>();
        return services;
    }

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        AdminOrderCompletenessEndpoints.Map(app);
        AdminOrdersGridEndpoints.Map(app);
        AdminCustomersEndpoints.Map(app);
        AdminOrderDetailEndpoints.Map(app);
        AdminOrderOperationsEndpoints.Map(app);
        AdminOrderInventoryRecoverySupplyEndpoints.Map(app);
        StorefrontOrderEndpoints.Map(app);
        CustomerOrderEndpoints.Map(app);
        SellerOrderEndpoints.Map(app);
        return app;
    }
}
