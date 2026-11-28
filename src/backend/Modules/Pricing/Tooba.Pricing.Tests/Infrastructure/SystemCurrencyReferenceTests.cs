using Microsoft.Extensions.Options;
using Tooba.Pricing.Infrastructure.Adapters;
using Xunit;

namespace Tooba.Pricing.Tests.Infrastructure;

/// <summary>
/// The authoritative system currency reference must be trusted configuration only and fail closed
/// for missing, malformed, display-unit or duplicate codes.
/// </summary>
public sealed class SystemCurrencyReferenceTests
{
    private static SystemCurrencyReference Reference(params PricingCurrencyEntry[] entries) =>
        new(Options.Create(new PricingCurrenciesOptions { Currencies = entries.ToList() }));

    private static PricingCurrencyEntry Entry(string code, int decimals = 2, bool active = true, int sort = 0) =>
        new() { Code = code, EnglishName = code, NativeName = code, DecimalPlaces = decimals, SortOrder = sort, Active = active };

    [Fact]
    public async Task Active_currencies_are_ordered_and_irr_has_zero_decimals()
    {
        var reference = Reference(Entry("USD", 2, sort: 2), Entry("IRR", 0, sort: 1));

        var rows = await reference.ListActiveAsync(CancellationToken.None);

        Assert.Collection(rows,
            row => Assert.Equal("IRR", row.Code),
            row => Assert.Equal("USD", row.Code));
        Assert.Equal(0, rows[0].DecimalPlaces);
        Assert.True(await reference.IsActiveAsync("usd", CancellationToken.None));
        Assert.False(await reference.IsActiveAsync("GBP", CancellationToken.None));
    }

    [Fact]
    public async Task Inactive_currency_is_not_selectable()
    {
        var reference = Reference(Entry("IRR", 0), Entry("USD", 2, active: false));

        Assert.False(await reference.IsActiveAsync("USD", CancellationToken.None));
        Assert.Single(await reference.ListActiveAsync(CancellationToken.None));
    }

    [Fact]
    public void Display_unit_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Reference(Entry("TMN", 0)));
    }

    [Fact]
    public void Duplicate_code_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Reference(Entry("USD"), Entry("usd")));
    }

    [Fact]
    public void Precision_must_match_the_pricing_money_policy()
    {
        Assert.Throws<InvalidOperationException>(() => Reference(Entry("USD", 3)));
        Assert.Throws<InvalidOperationException>(() => Reference(Entry("IRR", 2)));
    }

    [Fact]
    public async Task All_inactive_currencies_remain_inactive()
    {
        var reference = Reference(Entry("IRR", 0, active: false), Entry("USD", 2, active: false));

        Assert.Empty(await reference.ListActiveAsync(CancellationToken.None));
        Assert.False(await reference.IsActiveAsync("IRR", CancellationToken.None));
        Assert.False(await reference.IsActiveAsync("USD", CancellationToken.None));
    }

    [Fact]
    public async Task Empty_reference_fails_closed_without_inventing_currencies()
    {
        var reference = Reference();

        Assert.Empty(await reference.ListActiveAsync(CancellationToken.None));
        Assert.False(await reference.IsActiveAsync("IRR", CancellationToken.None));
    }
}
