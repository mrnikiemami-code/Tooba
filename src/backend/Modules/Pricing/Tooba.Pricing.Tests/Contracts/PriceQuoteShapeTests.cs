using System.Text.Json;
using Xunit;
using Tooba.Offer.Domain;
using Tooba.Pricing.Contracts;

namespace Tooba.Pricing.Tests.Contracts;

public sealed class PriceQuoteShapeTests
{
    [Fact]
    public void PriceQuote_roundtrips_json()
    {
        var original = new PriceQuote(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "IR",
            SalesChannel.Direct,
            42m,
            "IRR",
            true,
            true);
        var json = JsonSerializer.Serialize(original);
        var copy = JsonSerializer.Deserialize<PriceQuote>(json);
        Assert.NotNull(copy);
        Assert.Equal(original, copy);
    }
}
