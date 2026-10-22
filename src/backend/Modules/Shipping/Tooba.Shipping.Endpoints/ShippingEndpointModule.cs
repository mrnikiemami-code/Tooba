using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Shipping.Endpoints.Admin;
using Tooba.Shipping.Endpoints.Errors;
using Tooba.Shipping.Endpoints.Resources;
using Tooba.Shipping.Endpoints.Seller;

namespace Tooba.Shipping.Endpoints;

/// <summary>ترکیب نازک HTTP برای Shipping: نگاشت مسیر و ثبت وابستگی‌های لایهٔ ارائه.</summary>
public static class ShippingEndpointModule
{
    /// <summary>مسیرهای ادمین و فروشندهٔ Shipping را زیر <c>/v1/admin</c> و <c>/v1/seller</c> می‌نگارد.</summary>
    /// <param name="app">سازندهٔ مسیر.</param>
    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var admin = app.MapGroup("/v1/admin");
        ShippingRateCoverageEndpoints.Map(admin);
        ShippingMethodFinancialStatusEndpoints.Map(admin);
        ShippingOperatingScheduleEndpoints.Map(admin);
        ShippingPackagingPolicyEndpoints.Map(admin);
        ShippingOperationalQuoteEndpoints.Map(admin);
        // Additive TB-188 admin read: effective edition, configured default currency and ACTIVE
        // content languages. It adds no write path, no owner selector and no new permission.
        ShippingAuthoringContextEndpoints.Map(admin);
        // Additive TB-189 admin read: the CURRENT server-authorized shipping rule owner scope.
        ShippingAuthoringOwnerEndpoints.Map(admin);
        // Additive TB-189 seller read: the same use case behind the real seller owner gate.
        var seller = app.MapGroup("/v1/seller");
        ShippingSellerAuthoringOwnerEndpoints.Map(seller);
        return app;
    }

    /// <summary>
    /// ثبت ارائهٔ Shipping: کاتالوگ خطای صریح <c>shipping.*</c>، مجموعهٔ منبع آن و درز مجوزدهی
    /// مالک نویسندگی.
    /// یک‌بار در مسیر ترکیب Host فراخوانی می‌شود؛ هیچ ثبت تکراری مجاز نیست.
    /// </summary>
    /// <param name="services">مجموعهٔ سرویس.</param>
    public static IServiceCollection AddShippingEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, ShippingErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, ShippingErrorResourceSet>();
        // Scope-free: the owner gate reads the same per-request trusted context as the admin adapter.
        services.AddScoped<IShippingAuthoringOwnerAuthorizer, ShippingAuthoringOwnerAuthorizer>();
        return services;
    }
}
