using Tooba.Catalog.Infrastructure;
using Tooba.Identity.Infrastructure;
using Tooba.ModuleContracts;
using Tooba.Offer.Infrastructure;
using Tooba.Party.Infrastructure;
using Tooba.PlatformProbe.Infrastructure;
using Tooba.Pricing.Infrastructure;
using Tooba.Inventory.Infrastructure;
using Tooba.Cart.Infrastructure;
using Tooba.Order.Infrastructure;
using Tooba.Tax.Infrastructure;
using Tooba.Promotion.Infrastructure;
using Tooba.Payment.Infrastructure;
using Tooba.Reviews.Infrastructure;

namespace Tooba.Host;

/// <summary>
/// ریشهٔ ترکیب صریح ماژول‌ها. فهرست دستی است تا استخراج بعدی سرویس بدون وابستگی مصرف‌کننده به جزئیات in-process بماند.
/// منطق کسب‌وکار اینجا نوشته نمی‌شود.
/// </summary>
internal static class ToobaModuleComposition
{
    /// <summary>
    /// ماژول‌های ثبت‌شده در این استقرار. کشف اسمبلی انجام نمی‌شود.
    /// </summary>
    public static IReadOnlyList<IToobaModule> Modules { get; } =
    [
        new PlatformProbeModule(),
        new IdentityModule(),
        new PartyModule(),
        new CatalogModule(),
        new OfferModule(),
        new PricingModule(),
        new InventoryModule(),
        new CartModule(),
        new OrderModule(),
        new TaxModule(),
        new PromotionModule(),
        new ReviewsModule(),
        new PaymentModule(),
    ];

    /// <summary>
    /// هر ماژول را به ترتیب فهرست صریح ثبت می‌کند. Host فقط ترکیب می‌کند.
    /// </summary>
    public static IServiceCollection AddToobaModules(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        foreach (var module in Modules)
        {
            module.AddServices(services, configuration, environment);
        }

        return services;
    }
}
