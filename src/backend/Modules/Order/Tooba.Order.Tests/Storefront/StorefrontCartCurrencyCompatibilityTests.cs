using Tooba.Cart.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Services;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-TMAR-CART-MULTICURRENCY-LINES-001-R1 — Order boundary single-currency compatibility.
/// Order multi-currency is deferred, so the boundary accepts exactly one distinct non-empty line
/// currency and never treats Cart.DefaultCurrency as transaction authority.
/// </summary>
public sealed class StorefrontCartCurrencyCompatibilityTests
{
    [Fact]
    public void Single_currency_cart_resolves_the_sole_line_currency_not_default_currency()
    {
        var cart = Cart(defaultCurrency: "IRR", lines: [Line("USD")], totals: [("USD", 10m)]);
        Assert.Equal("USD", StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart));
        Assert.NotEqual(cart.DefaultCurrency, StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart));
    }

    [Fact]
    public void Single_currency_cart_reads_only_the_matching_per_currency_total()
    {
        var cart = Cart(defaultCurrency: "IRR", lines: [Line("USD")], totals: [("USD", 10m)]);
        var (currency, subtotal) = StorefrontCartCurrencyCompatibility.ResolveSoleCurrencyAndSubtotal(cart);
        Assert.Equal("USD", currency);
        Assert.Equal(10m, subtotal);
    }

    [Fact]
    public void Mixed_currency_cart_fails_closed_with_typed_stable_error()
    {
        var cart = Cart(
            defaultCurrency: "IRR",
            lines: [Line("USD"), Line("IRR")],
            totals: [("IRR", 500_000m), ("USD", 10m)]);
        var exception = Assert.Throws<StorefrontOrderException>(
            () => StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart));
        Assert.Equal(StorefrontOrderErrors.CheckoutMultiCurrencyNotSupported, exception.Code);
    }

    [Fact]
    public void Mixed_currency_cart_never_computes_a_cross_currency_subtotal()
    {
        var cart = Cart(
            defaultCurrency: "IRR",
            lines: [Line("USD"), Line("IRR")],
            totals: [("IRR", 500_000m), ("USD", 10m)]);
        var exception = Assert.Throws<StorefrontOrderException>(
            () => StorefrontCartCurrencyCompatibility.ResolveSoleCurrencyAndSubtotal(cart));
        Assert.Equal(StorefrontOrderErrors.CheckoutMultiCurrencyNotSupported, exception.Code);
    }

    [Fact]
    public void Missing_line_currency_fails_closed_without_default_fallback()
    {
        var cart = Cart(defaultCurrency: "IRR", lines: [Line(null)], totals: []);
        var exception = Assert.Throws<StorefrontOrderException>(
            () => StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart));
        Assert.Equal(StorefrontOrderErrors.CheckoutMultiCurrencyNotSupported, exception.Code);
    }

    [Fact]
    public void Totals_in_another_currency_than_the_sole_line_currency_fail_closed()
    {
        var cart = Cart(
            defaultCurrency: "IRR",
            lines: [Line("USD")],
            totals: [("USD", 10m), ("IRR", 3m)]);
        Assert.Throws<StorefrontOrderException>(
            () => StorefrontCartCurrencyCompatibility.ResolveSoleCurrencyAndSubtotal(cart));
    }

    [Fact]
    public void Snapshot_sole_currency_comes_from_quoted_line_currency()
    {
        var snapshot = Snapshot(defaultCurrency: "IRR", quotedCurrencies: ["USD", "USD"]);
        Assert.Equal("USD", StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(snapshot));
    }

    [Fact]
    public void Snapshot_mixed_quoted_currencies_fail_closed()
    {
        var snapshot = Snapshot(defaultCurrency: "IRR", quotedCurrencies: ["USD", "IRR"]);
        Assert.Throws<StorefrontOrderException>(
            () => StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(snapshot));
    }

    private static CartPage Cart(
        string defaultCurrency,
        IReadOnlyList<CartLineView> lines,
        IReadOnlyList<(string Currency, decimal Total)> totals) =>
        new(
            Guid.NewGuid(),
            1,
            "IR",
            defaultCurrency,
            "Marketplace",
            lines.Count,
            totals.Select(x => new CartCurrencyTotal(x.Currency, x.Total)).ToArray(),
            lines,
            null);

    private static CartLineView Line(string? currency) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "کالا",
            "فروشنده",
            null,
            1,
            10m,
            10m,
            currency!,
            true);

    private static CartSnapshot Snapshot(string defaultCurrency, IReadOnlyList<string?> quotedCurrencies) =>
        new(
            Guid.NewGuid(),
            CartStatus.Active,
            CartAccessKind.Guest,
            null,
            "IR",
            defaultCurrency,
            SalesChannel.Marketplace,
            null,
            CartConversionIntent.None,
            1,
            quotedCurrencies.Select(currency => new CartLineSnapshot(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                null,
                10m,
                currency,
                true,
                Guid.NewGuid(),
                DateTimeOffset.UnixEpoch)).ToArray());
}
