using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Notification.Endpoints.Customer;
using Tooba.Notification.Endpoints.Errors;
using Tooba.Notification.Endpoints.Seller;

namespace Tooba.Notification.Endpoints;

/// <summary>Thin composition for Notification HTTP ownership.</summary>
public static class NotificationEndpointModule
{
    /// <summary>Maps Notification customer/seller routes.</summary>
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var customer = app.MapGroup("/v1/customer/notifications");
        NotificationCustomerEndpoints.Map(customer);
        var seller = app.MapGroup("/v1/seller/notifications");
        NotificationSellerEndpoints.Map(seller);
        return app;
    }

    /// <summary>Registers Notification error catalog for ApiResponseFactory.</summary>
    public static IServiceCollection AddNotificationEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, NotificationErrorCatalogContributor>();
        return services;
    }
}
