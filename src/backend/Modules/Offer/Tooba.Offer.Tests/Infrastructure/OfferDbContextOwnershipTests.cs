using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Domain.Events;
using Tooba.Offer.Domain.Aggregates;

﻿using Xunit;
using Microsoft.EntityFrameworkCore;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;

namespace Tooba.Offer.Tests.Infrastructure;

public sealed class OfferDbContextOwnershipTests
{
    [Fact]
    public void OfferDbContext_schema_is_offer_and_maps_seller_offer_only()
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>()
            .UseInMemoryDatabase("offer-ownership-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new OfferDbContext(options);
        Assert.Equal("offer", OfferDbContext.Schema);
        var entity = db.Model.FindEntityType(typeof(SellerOffer));
        Assert.NotNull(entity);
        Assert.Equal("offers", entity!.GetTableName());
    }
}
