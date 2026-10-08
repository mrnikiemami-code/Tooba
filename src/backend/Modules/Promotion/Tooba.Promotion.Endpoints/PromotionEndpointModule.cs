using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Promotion.Contracts.Errors;
using Tooba.Promotion.Endpoints.Admin;
using Tooba.Promotion.Endpoints.Errors;
using Tooba.Promotion.Endpoints.Seller;

namespace Tooba.Promotion.Endpoints;

/// <summary>ثبت presentation ماژول Promotion: مسیرها، کاتالوگ خطا و مجموعهٔ منابع خطا.</summary>
public static class PromotionEndpointModule
{
    /// <summary>مسیرهای Promotion را نگاشت می‌کند.</summary>
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        PromotionSellerEndpoints.Map(app);
        PromotionAdminEndpoints.Map(app);
        MerchandisingCampaignAdminEndpoints.Map(app);
        return app;
    }

    /// <summary>کاتالوگ خطا، مجموعهٔ منابع خطا و مجوزدهی ادمین را ثبت می‌کند.</summary>
    public static IServiceCollection AddPromotionEndpointPresentation(this IServiceCollection services)
    {
        services.AddSingleton<IErrorCatalogContributor, PromotionErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, PromotionErrorResourceSet>();
        services.AddScoped<IPromotionAdminAuthorizer, PromotionAdminAuthorizer>();
        return services;
    }
}
