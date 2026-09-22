using Microsoft.AspNetCore.Routing; using Microsoft.Extensions.DependencyInjection; using Tooba.BuildingBlocks.Presentation.Errors; using Tooba.Promotion.Endpoints.Admin; using Tooba.Promotion.Endpoints.Errors; using Tooba.Promotion.Endpoints.Seller;
namespace Tooba.Promotion.Endpoints;
public static class PromotionEndpointModule
{
 public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder app){PromotionSellerEndpoints.Map(app);PromotionAdminEndpoints.Map(app);return app;}
 public static IServiceCollection AddPromotionEndpointPresentation(this IServiceCollection services){services.AddSingleton<IErrorCatalogContributor,PromotionErrorCatalogContributor>();return services;}
}
