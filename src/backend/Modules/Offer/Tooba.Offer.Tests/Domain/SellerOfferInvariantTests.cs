using Xunit;
using Tooba.Offer.Domain;
using Tooba.Offer.Contracts;

namespace Tooba.Offer.Tests.Domain;

public sealed class SellerOfferInvariantTests
{
    [Fact]
    public void Create_starts_as_draft_without_price_or_stock()
    {
        var offer = SellerOffer.Create(Guid.NewGuid(), Guid.NewGuid(), SalesChannel.Marketplace, "SKU-1", DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        Assert.Equal(OfferStatus.Draft, offer.Status);
        Assert.Equal("SKU-1", offer.SellerSku);
        Assert.Contains(offer.DomainEvents, e => e is OfferCreatedDomainEvent);
    }

    [Fact]
    public void Archive_then_Activate_throws()
    {
        var offer = SellerOffer.Create(Guid.NewGuid(), Guid.NewGuid(), SalesChannel.Direct, null, DateTimeOffset.UtcNow);
        offer.Archive(DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => offer.Activate(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SetOrderQuantityLimits_rejects_min_above_max()
    {
        var offer = SellerOffer.Create(Guid.NewGuid(), Guid.NewGuid(), SalesChannel.Direct, null, DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => offer.SetOrderQuantityLimits(5, 2, DateTimeOffset.UtcNow));
    }
}
