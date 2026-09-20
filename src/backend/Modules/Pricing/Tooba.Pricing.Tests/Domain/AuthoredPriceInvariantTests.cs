using Xunit;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Domain;

namespace Tooba.Pricing.Tests.Domain;

public sealed class AuthoredPriceInvariantTests
{
    [Fact]
    public void Create_starts_draft_without_tax_or_fx()
    {
        var price = AuthoredPrice.Create(
            Guid.NewGuid(),
            "IR",
            SalesChannel.Marketplace,
            1000m,
            "IRR",
            DateTimeOffset.Parse("2026-01-01Z"),
            null,
            DateTimeOffset.Parse("2026-01-01Z"));
        Assert.Equal(PriceStatus.Draft, price.Status);
        Assert.Equal(1000m, price.Amount);
        Assert.Equal(PriceQualifierKind.Base, price.QualifierKind);
        Assert.Contains(price.DomainEvents, e => e is PriceCreatedDomainEvent);
    }

    [Fact]
    public void Money_rejects_negative_amount()
    {
        Assert.Throws<InvalidOperationException>(() => Money.Create(-1m, "IRR"));
    }

    [Fact]
    public void MarketCode_rejects_empty()
    {
        Assert.Throws<InvalidOperationException>(() => MarketCode.Parse(" "));
    }
}
