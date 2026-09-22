using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Payment.Endpoints.Admin;
using Tooba.Payment.Endpoints.Errors;
using Tooba.Payment.Endpoints.Storefront;
using Tooba.Payment.Endpoints.Webhooks;

namespace Tooba.Payment.Endpoints;

/// <summary>Thin composition for Payment HTTP ownership.</summary>
public static class PaymentEndpointModule
{
    /// <summary>Maps Payment storefront/admin/webhook routes.</summary>
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        PaymentStorefrontEndpoints.Map(app);
        PaymentAdminEndpoints.Map(app);
        PaymentWebhookEndpoints.Map(app);
        return app;
    }

    /// <summary>Registers Payment error catalog for ApiResponseFactory.</summary>
    public static IServiceCollection AddPaymentEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, PaymentErrorCatalogContributor>();
        return services;
    }
}
