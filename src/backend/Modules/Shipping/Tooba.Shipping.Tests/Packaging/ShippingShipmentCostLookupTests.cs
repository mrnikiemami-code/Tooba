using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Infrastructure.Packaging;
using Xunit;

namespace Tooba.Shipping.Tests.Packaging;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-075 — نمای ترکیبی هزینهٔ مرسوله: جمع صریح دو مؤلفه، و امتناع از
/// جمع خوش‌بینانه وقتی حمل قیمت‌گذاری نشده، ارزها ناسازگارند، یا بسته‌بندی مبهم است.
/// </summary>
public sealed class ShippingShipmentCostLookupTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly Guid Area = Guid.Parse("1f8050a8-c325-59fa-959f-82d76b5b2bc5");

    private static ShippingShipmentCostRequest Request(decimal subtotal = 500_000m, string? currency = "IRR") =>
        new("tipax", new DestinationIdentifiers(Country, Area), subtotal, currency);

    private static ShippingQuote Quoted(decimal charge, string currency = "IRR") =>
        new(
            ShippingServiceabilityOutcome.Quoted,
            "tipax",
            charge,
            currency,
            1,
            2,
            null,
            ShippingReasonCodes.Quoted);

    private static ShippingQuote NotQuoted(ShippingServiceabilityOutcome outcome, string reason) =>
        new(outcome, "tipax", 0m, null, 0, 0, null, reason);

    private static ShippingPackagingFeeComponent Packaging(
        decimal fee,
        string? currency = "IRR",
        ShippingFreightPayerMode payer = ShippingFreightPayerMode.Prepaid) =>
        new(
            true,
            fee,
            currency,
            payer,
            ShippingPackagingChargePayer.Checkout,
            Guid.NewGuid(),
            ShippingPackagingReasonCodes.Charged);

    [Fact]
    public async Task Freight_and_packaging_are_combined_explicitly_when_both_are_resolvable()
    {
        var lookup = new ShippingShipmentCostLookup(
            new StubRateLookup(Quoted(120_000m)),
            new StubPackagingLookup(Packaging(12_000m)));

        var cost = await lookup.ResolveAsync(Request(), default);

        Assert.Equal(120_000m, cost.CarrierFreight.ChargeAmount);
        Assert.Equal(12_000m, cost.Packaging.FeeAmount);
        Assert.Equal("IRR", cost.CurrencyCode);
        Assert.Equal(132_000m, cost.CombinedAmount);
        Assert.True(cost.IsCombined);
        Assert.Equal(ShippingShipmentCostReasonCodes.Quoted, cost.ReasonCode);
    }

    [Fact]
    public async Task A_free_freight_quote_keeps_the_packaging_component_in_the_combination()
    {
        // Freight waived by its threshold (0) but packaging is independent, so the total is the packaging
        // fee alone - never silently zero.
        var lookup = new ShippingShipmentCostLookup(
            new StubRateLookup(Quoted(0m)),
            new StubPackagingLookup(Packaging(12_000m)));

        var cost = await lookup.ResolveAsync(Request(), default);

        Assert.Equal(0m, cost.CarrierFreight.ChargeAmount);
        Assert.Equal(12_000m, cost.Packaging.FeeAmount);
        Assert.Equal(12_000m, cost.CombinedAmount);
    }

    [Fact]
    public async Task An_unpriced_freight_leaves_the_combined_amount_null()
    {
        var lookup = new ShippingShipmentCostLookup(
            new StubRateLookup(NotQuoted(ShippingServiceabilityOutcome.NotCovered, ShippingReasonCodes.CoverageMissing)),
            new StubPackagingLookup(Packaging(12_000m)));

        var cost = await lookup.ResolveAsync(Request(), default);

        Assert.Null(cost.CombinedAmount);
        Assert.False(cost.IsCombined);
        Assert.Equal(ShippingShipmentCostReasonCodes.FreightNotQuoted, cost.ReasonCode);
        // The freight result is passed through untouched so the caller can still inspect the outcome.
        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, cost.CarrierFreight.Outcome);
    }

    [Fact]
    public async Task Mismatched_component_currencies_are_not_summed()
    {
        var lookup = new ShippingShipmentCostLookup(
            new StubRateLookup(Quoted(120_000m, "IRR")),
            new StubPackagingLookup(Packaging(12_000m, "USD")));

        var cost = await lookup.ResolveAsync(Request(), default);

        Assert.Null(cost.CombinedAmount);
        Assert.Null(cost.CurrencyCode);
        Assert.Equal(ShippingShipmentCostReasonCodes.CurrencyMismatch, cost.ReasonCode);
    }

    [Fact]
    public async Task Ambiguous_packaging_policies_block_the_combined_amount()
    {
        var ambiguous = ShippingPackagingFeeComponent.Zero(ShippingPackagingReasonCodes.Ambiguous);
        var lookup = new ShippingShipmentCostLookup(
            new StubRateLookup(Quoted(120_000m)),
            new StubPackagingLookup(ambiguous));

        var cost = await lookup.ResolveAsync(Request(), default);

        Assert.Null(cost.CombinedAmount);
        Assert.Equal(ShippingShipmentCostReasonCodes.PackagingAmbiguous, cost.ReasonCode);
    }

    [Fact]
    public async Task An_unconfigured_packaging_identity_still_yields_the_freight_only_total()
    {
        var notConfigured = ShippingPackagingFeeComponent.Zero(ShippingPackagingReasonCodes.NotConfigured);
        var lookup = new ShippingShipmentCostLookup(
            new StubRateLookup(Quoted(120_000m)),
            new StubPackagingLookup(notConfigured));

        var cost = await lookup.ResolveAsync(Request(), default);

        Assert.Equal(120_000m, cost.CombinedAmount);
        Assert.False(cost.Packaging.IsCharged);
    }

    private sealed class StubRateLookup(ShippingQuote quote) : IShippingRateQuoteLookup
    {
        public Task<ShippingQuote> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken)
            => Task.FromResult(quote);
    }

    private sealed class StubPackagingLookup(ShippingPackagingFeeComponent component) : IShippingPackagingFeeLookup
    {
        public Task<ShippingPackagingFeeComponent> ResolveAsync(
            ShippingPackagingFeeRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(component);
    }
}
