using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Shipping.Endpoints.Admin;
using Tooba.Shipping.Endpoints.Errors;
using Tooba.Shipping.Endpoints.Resources;

namespace Tooba.Shipping.Endpoints;

/// <summary>ترکیب نازک HTTP برای Shipping: نگاشت مسیر و ثبت وابستگی‌های لایهٔ ارائه.</summary>
public static class ShippingEndpointModule
{
    /// <summary>مسیرهای ادمین Shipping را زیر <c>/v1/admin</c> می‌نگارد.</summary>
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
        return app;
    }

    /// <summary>
    /// ثبت ارائهٔ Shipping: کاتالوگ خطای صریح <c>shipping.*</c> و مجموعهٔ منبع آن.
    /// یک‌بار در مسیر ترکیب Host فراخوانی می‌شود؛ هیچ ثبت تکراری مجاز نیست.
    /// </summary>
    /// <param name="services">مجموعهٔ سرویس.</param>
    public static IServiceCollection AddShippingEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, ShippingErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, ShippingErrorResourceSet>();
        return services;
    }
}
