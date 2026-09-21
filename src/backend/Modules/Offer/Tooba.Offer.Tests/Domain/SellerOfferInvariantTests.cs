using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Contracts;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.Events;
using Tooba.Offer.Domain.ValueObjects;
using Xunit;

namespace Tooba.Offer.Tests.Domain;

public sealed class SellerOfferInvariantTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
    private static readonly Guid OfferId = Guid.Parse("01900000-0000-7000-8000-000000000001");

    [Fact]
    public void Create_starts_as_draft_without_price_or_stock()
    {
        var offer = SellerOffer.Create(
            OfferId,
            Guid.Parse("01900000-0000-7000-8000-000000000002"),
            Guid.Parse("01900000-0000-7000-8000-000000000003"),
            SalesChannel.Marketplace,
            "SKU-1",
            Now);
        Assert.Equal(OfferStatus.Draft, offer.Status);
        Assert.Equal(OfferId, offer.OfferId);
        Assert.Equal("SKU-1", offer.SellerSku);
        Assert.Contains(offer.DomainEvents, e => e is OfferCreatedDomainEvent);
    }

    [Fact]
    public void Archive_then_Activate_returns_stable_semantic_code()
    {
        var offer = SellerOffer.Create(OfferId, Guid.NewGuid(), Guid.NewGuid(), SalesChannel.Direct, null, Now);
        offer.Archive(Now);
        var failed = offer.Activate(Now);
        Assert.True(failed.IsFailure);
        Assert.Equal(OfferErrorCodes.ArchivedCannotActivate, failed.FirstError.Code);
    }

    [Fact]
    public void SetOrderQuantityLimits_rejects_min_above_max_with_stable_code()
    {
        var offer = SellerOffer.Create(OfferId, Guid.NewGuid(), Guid.NewGuid(), SalesChannel.Direct, null, Now);
        var failed = offer.SetOrderQuantityLimits(5, 2, Now);
        Assert.True(failed.IsFailure);
        Assert.Equal(OfferErrorCodes.MinQuantityExceedsMax, failed.FirstError.Code);
    }

    [Fact]
    public void SetOrderQuantityLimits_rejects_non_positive_bounds_with_stable_codes()
    {
        var offer = SellerOffer.Create(OfferId, Guid.NewGuid(), Guid.NewGuid(), SalesChannel.Direct, null, Now);
        Assert.Equal(
            OfferErrorCodes.MinQuantityInvalid,
            offer.SetOrderQuantityLimits(0, 2, Now).FirstError.Code);
        Assert.Equal(
            OfferErrorCodes.MaxQuantityInvalid,
            offer.SetOrderQuantityLimits(1, 0, Now).FirstError.Code);
    }

    [Fact]
    public void Activate_from_draft_succeeds()
    {
        var offer = SellerOffer.Create(OfferId, Guid.NewGuid(), Guid.NewGuid(), SalesChannel.Direct, null, Now);
        Assert.True(offer.Activate(Now).IsSuccess);
        Assert.Equal(OfferStatus.Active, offer.Status);
        Assert.Contains(offer.DomainEvents, e => e is OfferActivatedDomainEvent);
    }
}
