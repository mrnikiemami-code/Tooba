using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Endpoints.Errors;
using Tooba.Order.Endpoints.Resources;

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
        AdminOrderOperationsEndpoints.Map(app);
        AdminOrderInventoryRecoverySupplyEndpoints.Map(app);
        return app;
    }
}
