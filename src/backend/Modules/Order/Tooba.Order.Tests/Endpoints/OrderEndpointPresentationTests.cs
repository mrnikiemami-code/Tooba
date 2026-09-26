using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Completeness.Errors;
using Tooba.Order.Application.Customer;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.Seller;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Endpoints;
using Tooba.Order.Endpoints.Errors;
using Tooba.Order.Endpoints.Resources;
using Xunit;

namespace Tooba.Order.Tests.Endpoints;

/// <summary>کاتالوگ خطا و منابع محلی‌سازی Order در لایهٔ presentation.</summary>
public sealed class OrderEndpointPresentationTests
{
    private static readonly string[] CompletenessCodes =
    [
        AdminOrderCompletenessErrors.Missing,
        AdminOrderCompletenessErrors.InvalidNote,
        AdminOrderCompletenessErrors.DeleteForbidden,
        AdminOrderCompletenessErrors.InvoiceUnavailable,
        AdminOrderCompletenessErrors.ReceiptUnavailable,
    ];

    private static readonly string[] StorefrontCodes =
        typeof(StorefrontOrderErrors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

    private static readonly string[] CustomerCodes =
    [
        CustomerOrderErrors.Missing,
        CustomerOrderErrors.SessionRequired,
        CustomerOrderErrors.SupplyUnavailable,
        ReservationCycleErrors.RetryLimitReached,
    ];

    private static readonly string[] SellerCodes =
    [
        SellerOrderErrors.SellerMissing,
        SellerOrderErrors.ViewDenied,
        SellerOrderErrors.OrderMissing,
        SellerOrderErrors.ActorMissing,
        SellerOrderErrors.IdentityMissing,
        SellerOrderErrors.PartyViewDenied,
        "seller.authorization.denied",
        "seller.authorization.unavailable",
    ];

    private static readonly string[] AllCodes =
        CompletenessCodes.Concat(StorefrontCodes).Concat(CustomerCodes).Concat(SellerCodes).ToArray();

    /// <summary>
    /// Cross-cutting codes consumed by Order but canonically owned by FoundationErrorCatalogContributor.
    /// They must resolve through the composed catalog, not through a duplicate Order descriptor.
    /// </summary>
    private static readonly string[] SharedFoundationCodes =
    [
        StorefrontOrderErrors.CheckoutAuthenticationRequired,
        CustomerOrderErrors.SessionRequired,
        "seller.authorization.denied",
    ];

    private static readonly string[] OrderOwnedCodes =
        AllCodes.Where(c => !SharedFoundationCodes.Contains(c, StringComparer.Ordinal)).ToArray();

    [Fact]
    public void Presentation_registration_adds_catalog_and_resource_set()
    {
        var provider = new ServiceCollection().AddOrderEndpointPresentation().BuildServiceProvider();

        Assert.Contains(
            provider.GetServices<IErrorCatalogContributor>(),
            x => x is OrderErrorCatalogContributor);
        Assert.Contains(
            provider.GetServices<IErrorResourceSet>(),
            x => x is OrderErrorResourceSet);
    }

    [Fact]
    public void Every_order_owned_error_code_has_an_explicit_descriptor()
    {
        var descriptors = new OrderErrorCatalogContributor().Contribute();

        Assert.Equal(OrderOwnedCodes.Length, descriptors.Count);
        foreach (var code in OrderOwnedCodes)
        {
            var descriptor = Assert.Single(descriptors, x => x.Code == code);
            Assert.Equal(code, descriptor.LocalizationKey);
            Assert.InRange(descriptor.HttpStatus, 400, 599);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.SafeTitleFallback));
        }

        // Shared cross-cutting codes must not be re-registered by Order (single canonical owner).
        foreach (var shared in SharedFoundationCodes)
        {
            Assert.DoesNotContain(descriptors, x => x.Code == shared);
        }
    }

    [Fact]
    public void Composed_catalog_resolves_order_and_shared_codes_without_duplicates()
    {
        var catalog = new ErrorDefinitionCatalog(
        [
            new FoundationErrorCatalogContributor(),
            new OrderErrorCatalogContributor(),
        ]);

        foreach (var code in AllCodes)
        {
            Assert.True(catalog.TryGet(code, out _), "missing descriptor: " + code);
        }

        foreach (var shared in SharedFoundationCodes)
        {
            Assert.True(catalog.TryGet(shared, out _), "missing shared descriptor: " + shared);
        }
    }

    [Fact]
    public void Resource_set_owns_order_keys_only_and_resolves_fa_and_en()
    {
        var set = new OrderErrorResourceSet();
        Assert.True(set.Owns("order.note.invalid"));
        Assert.True(set.Owns("checkout.missing"));
        Assert.True(set.Owns("shipping.cart.stale"));
        Assert.True(set.Owns("pending.hide.active_hold"));
        Assert.True(set.Owns(StorefrontOrderErrors.PaymentMissing));
        Assert.False(set.Owns("offer.not_found"));
        Assert.False(set.Owns("payment.attempt.missing"));

        foreach (var code in AllCodes)
        {
            var english = set.GetString(code, CultureInfo.GetCultureInfo("en"));
            var persian = set.GetString(code, CultureInfo.GetCultureInfo("fa"));
            Assert.False(string.IsNullOrWhiteSpace(english), $"missing en resource for {code}");
            Assert.False(string.IsNullOrWhiteSpace(persian), $"missing fa resource for {code}");
            Assert.NotEqual(english, persian);
            Assert.Contains(persian!, ch => ch is >= '\u0600' and <= '\u06ff');
        }
    }
}
