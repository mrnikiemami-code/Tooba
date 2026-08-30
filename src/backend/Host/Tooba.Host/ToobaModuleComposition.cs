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
using Tooba.ProductQnA.Infrastructure;
using Tooba.BulkInquiry.Infrastructure;
using Tooba.Wishlist.Infrastructure;
using Tooba.AddressBook.Infrastructure;
using Tooba.CustomerProfile.Infrastructure;
using Tooba.UserPreference.Infrastructure;
using Tooba.OperatorProfile.Infrastructure;
using Tooba.Content.Infrastructure;
using Tooba.Media.Infrastructure;
using Tooba.PageComposition.Infrastructure;
using global::Tooba.Story.Infrastructure;
using Tooba.Fulfillment.Infrastructure;
using Tooba.Returns.Infrastructure;
using Tooba.Settlement.Infrastructure;
using Tooba.Notification.Infrastructure;
using Tooba.AccessControl.Infrastructure;
using Tooba.Support.Infrastructure;
using Tooba.Wallet.Infrastructure;

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
        new ProductQnAModule(),
        new BulkInquiryModule(),
        new WishlistModule(),
        new AddressBookModule(),
        new CustomerProfileModule(),
        new UserPreferenceModule(),
        new OperatorProfileModule(),
        new ContentModule(),
        new MediaModule(),
        new PageCompositionModule(),
        new StoryModule(),
        new PaymentModule(),
        new FulfillmentModule(),
        new ReturnsModule(),
        new SettlementModule(),
        new SupportModule(),
        new WalletModule(),
        new NotificationModule(),
        new AccessControlModule(),
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
