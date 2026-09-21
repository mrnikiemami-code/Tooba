using Tooba.Pricing.Contracts;
using Tooba.Promotion.Infrastructure.Messaging;
using Tooba.Promotion.Infrastructure.Adapters;
using Tooba.Promotion.Infrastructure.Queries;
using Tooba.Promotion.Infrastructure.Directories;
using Tooba.Promotion.Contracts.Merchandising;
using Tooba.Promotion.Contracts.Checkout;
using Tooba.Promotion.Application.Merchandising;
using Tooba.Promotion.Application.Checkout;
using Tooba.Promotion.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure.DependencyInjection;

/// <summary>
/// ماژول Promotion: تعریف و ارزیابی تخفیف جدا از Pricing و Tax.
/// </summary>
public sealed class PromotionModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Promotion";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, PromotionOutboxRegistration>();
        services.AddScoped<IPromotionUseCaseGuard, OpenPromotionUseCaseGuard>();
        services.AddScoped<IPromotionRedemptionLedger, DeferredPromotionRedemptionLedger>();
        services.AddScoped<IPromotionDirectory, PromotionDirectory>();
        services.AddScoped<IPromotionEvaluator>(sp => sp.GetRequiredService<IPromotionDirectory>());
        services.AddScoped<ICheckoutPromotionPort, CheckoutPromotionAdapter>();
        services.AddScoped<IMerchandisingCampaignDirectory, MerchandisingCampaignDirectory>();
        services.AddScoped<IMerchandisingCampaignQuery, MerchandisingCampaignQuery>();
        services.AddScoped<Tooba.Pricing.Contracts.ICampaignCartPriceAuthority, CampaignCartPriceAuthority>();
        services.AddScoped<IPromotionSchemaMigrator, Adapters.PromotionSchemaMigrator>();
        services.AddDbContext<PromotionDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                PromotionDbContext.Schema,
                typeof(PromotionDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
