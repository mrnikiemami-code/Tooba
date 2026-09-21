using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts;
using Tooba.Party.Contracts;
using Tooba.Pricing.Contracts;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>
/// Decorates Offer golden-path cross-module gateways after all modules register.
/// </summary>
public static class OfferModuleCallTracingRegistration
{
    /// <summary>Wraps Catalog/Party/Pricing/Inventory gateways with <see cref="IModuleCallTracer"/>.</summary>
    public static IServiceCollection AddOfferModuleCallTracing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Decorate<ICatalogVariantLookup, TracedCatalogVariantLookup>(services);
        Decorate<ICatalogOfferReadGateway, TracedCatalogOfferReadGateway>(services);
        Decorate<IPartyLookup, TracedPartyLookup>(services);
        Decorate<IPriceLookupGateway, TracedPriceLookupGateway>(services);
        Decorate<ISellerOfferInventoryGateway, TracedSellerOfferInventoryGateway>(services);
        return services;
    }

    private static void Decorate<TService, TDecorator>(IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor is null)
        {
            return;
        }

        services.Remove(descriptor);

        var innerFactory = CreateInnerFactory<TService>(descriptor);
        services.AddScoped<TService>(sp =>
        {
            var inner = innerFactory(sp);
            var tracer = sp.GetRequiredService<IModuleCallTracer>();
            return (TService)Activator.CreateInstance(typeof(TDecorator), inner, tracer)!;
        });
    }

    private static Func<IServiceProvider, TService> CreateInnerFactory<TService>(ServiceDescriptor descriptor)
        where TService : class
    {
        if (descriptor.ImplementationInstance is TService instance)
        {
            return _ => instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return sp => (TService)descriptor.ImplementationFactory(sp);
        }

        if (descriptor.ImplementationType is not null)
        {
            var implType = descriptor.ImplementationType;
            return sp => (TService)ActivatorUtilities.CreateInstance(sp, implType);
        }

        throw new InvalidOperationException($"Cannot decorate {typeof(TService).Name}: unsupported descriptor.");
    }
}
