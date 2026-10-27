using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.Errors;
using Tooba.Offer.Domain.ValueObjects;
using Xunit;

namespace Tooba.Offer.Tests.Domain;

/// <summary>
/// TB-228: the offer is the ONLY assignment point for fulfillment responsibility, and it never infers one.
/// </summary>
public sealed class OfferFulfillmentAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid SellerId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid TypeId = Guid.Parse("01a1785e-01f3-7000-b50d-d6372e756f89");

    private static SellerOffer NewOffer() =>
        SellerOffer.Create(
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            SellerId,
            SalesChannel.Marketplace,
            "SKU-1",
            Now);

    [Fact]
    public void A_new_offer_is_unassigned_and_never_inferred_from_the_seller()
    {
        var offer = NewOffer();

        Assert.Null(offer.FulfillmentResponsibility);
        Assert.Null(offer.FulfillmentShippingTypeId);
        Assert.Equal(SellerId, offer.SellerPartyId);
    }

    [Fact]
    public void Platform_responsibility_stores_only_the_real_type_id()
    {
        var offer = NewOffer();
        var later = Now.AddMinutes(5);

        var result = offer.AssignFulfillment(FulfillmentResponsibility.Platform, TypeId, later);

        Assert.True(result.IsSuccess);
        Assert.Equal(FulfillmentResponsibility.Platform, offer.FulfillmentResponsibility);
        Assert.Equal(TypeId, offer.FulfillmentShippingTypeId);
        Assert.Equal(later, offer.UpdatedAt);
    }

    [Theory]
    [InlineData(FulfillmentResponsibility.Seller)]
    [InlineData(FulfillmentResponsibility.Platform)]
    public void Both_supported_families_are_lawful_assignments(FulfillmentResponsibility responsibility)
    {
        var offer = NewOffer();

        var result = offer.AssignFulfillment(responsibility, null, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(responsibility, offer.FulfillmentResponsibility);
        Assert.Null(offer.FulfillmentShippingTypeId);
    }

    [Fact]
    public void A_type_without_responsibility_is_refused_and_changes_nothing()
    {
        var offer = NewOffer();
        offer.AssignFulfillment(FulfillmentResponsibility.Seller, TypeId, Now);

        var result = offer.AssignFulfillment(null, TypeId, Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == OfferErrorCodes.FulfillmentTypeWithoutResponsibility);
        Assert.Equal(FulfillmentResponsibility.Seller, offer.FulfillmentResponsibility);
        Assert.Equal(TypeId, offer.FulfillmentShippingTypeId);
    }

    [Fact]
    public void Clearing_is_explicit_and_removes_both_parts()
    {
        var offer = NewOffer();
        offer.AssignFulfillment(FulfillmentResponsibility.Platform, TypeId, Now);

        var result = offer.AssignFulfillment(null, null, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Null(offer.FulfillmentResponsibility);
        Assert.Null(offer.FulfillmentShippingTypeId);
    }

    [Fact]
    public void An_empty_type_id_is_refused()
    {
        var offer = NewOffer();

        var result = offer.AssignFulfillment(FulfillmentResponsibility.Platform, Guid.Empty, Now);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == OfferErrorCodes.FulfillmentTypeInvalid);
        Assert.Null(offer.FulfillmentResponsibility);
    }

    [Fact]
    public void An_archived_offer_cannot_take_an_assignment()
    {
        var offer = NewOffer();
        offer.Archive(Now);

        var result = offer.AssignFulfillment(FulfillmentResponsibility.Platform, TypeId, Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == OfferErrorCodes.ArchivedCannotAssignFulfillment);
        Assert.Null(offer.FulfillmentResponsibility);
        Assert.Null(offer.FulfillmentShippingTypeId);
    }
}
