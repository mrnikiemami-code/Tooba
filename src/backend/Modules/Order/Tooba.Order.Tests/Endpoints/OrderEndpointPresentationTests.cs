using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Application.Admin.Completeness.Errors;
using Tooba.Order.Endpoints;
using Tooba.Order.Endpoints.Errors;
using Tooba.Order.Endpoints.Resources;
using Xunit;

namespace Tooba.Order.Tests.Endpoints;

/// <summary>کاتالوگ خطا و منابع محلی‌سازی Order در لایهٔ presentation.</summary>
public sealed class OrderEndpointPresentationTests
{
    private static readonly string[] AllCodes =
    [
        AdminOrderCompletenessErrors.Missing,
        AdminOrderCompletenessErrors.InvalidNote,
        AdminOrderCompletenessErrors.DeleteForbidden,
        AdminOrderCompletenessErrors.InvoiceUnavailable,
        AdminOrderCompletenessErrors.ReceiptUnavailable,
    ];

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
    public void Every_completeness_error_code_has_an_explicit_descriptor()
    {
        var descriptors = new OrderErrorCatalogContributor().Contribute();

        Assert.Equal(AllCodes.Length, descriptors.Count);
        foreach (var code in AllCodes)
        {
            var descriptor = Assert.Single(descriptors, x => x.Code == code);
            Assert.Equal(code, descriptor.LocalizationKey);
            Assert.InRange(descriptor.HttpStatus, 400, 499);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.SafeTitleFallback));
        }
    }

    [Fact]
    public void Resource_set_owns_order_keys_only_and_resolves_fa_and_en()
    {
        var set = new OrderErrorResourceSet();
        Assert.True(set.Owns("order.note.invalid"));
        Assert.False(set.Owns("offer.not_found"));

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
