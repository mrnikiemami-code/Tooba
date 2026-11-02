using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

public sealed class CheckoutDirectoryCarrierQuoteFreshnessTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Issued = DateTimeOffset.Parse("2026-11-02T12:00:00Z");

    [Fact]
    public void Fresh_identical_carrier_quotes_match_and_expired_or_changed_identity_is_refused()
    {
        var stored = ShippingGroupAcceptedCarrierQuote.Create(
            "tipax",
            AccountId,
            1,
            "7",
            "input-1",
            "IRR",
            137_500m,
            120_000m,
            12_500m,
            5_000m,
            0m,
            0m,
            Issued,
            Issued.AddMinutes(15),
            "101",
            "202",
            1500m,
            2_000_000m,
            20m,
            15m,
            10m,
            null);

        var live = Live(Issued.AddMinutes(15), "input-1", 137_500m);
        Assert.True(CheckoutDirectory.SameCarrierQuote(stored, live, Issued.AddMinutes(15)));
        Assert.False(CheckoutDirectory.SameCarrierQuote(stored, live, Issued.AddMinutes(15).AddTicks(1)));
        Assert.False(CheckoutDirectory.SameCarrierQuote(
            stored,
            Live(Issued.AddMinutes(15), "input-2", 137_500m),
            Issued.AddMinutes(1)));
        Assert.False(CheckoutDirectory.SameCarrierQuote(
            stored,
            Live(Issued.AddMinutes(15), "input-1", 140_000m),
            Issued.AddMinutes(1)));
        Assert.False(CheckoutDirectory.SameCarrierQuote(stored, null, Issued.AddMinutes(1)));
        Assert.True(CheckoutDirectory.SameCarrierQuote(null, null, Issued));
        Assert.False(CheckoutDirectory.SameCarrierQuote(null, live, Issued));
    }

    private static ShippingCapturedCarrierQuoteEvidence Live(
        DateTimeOffset freshUntil,
        string identity,
        decimal finalAmount)
        => new(
            "tipax",
            AccountId,
            1,
            "7",
            identity,
            "IRR",
            finalAmount,
            120_000m,
            12_500m,
            5_000m,
            0m,
            0m,
            Issued,
            freshUntil,
            "101",
            "202",
            1500m,
            2_000_000m,
            20m,
            15m,
            10m,
            null);
}
