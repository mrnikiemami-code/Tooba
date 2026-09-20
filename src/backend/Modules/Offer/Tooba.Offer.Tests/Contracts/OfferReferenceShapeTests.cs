using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Domain.Events;
using Tooba.Offer.Domain.Aggregates;

﻿using Xunit;
using System.Text.Json;
using Tooba.Offer.Contracts;
using Tooba.Offer.Domain;

namespace Tooba.Offer.Tests.Contracts;

public sealed class OfferReferenceShapeTests
{
    [Fact]
    public void OfferReference_roundtrips_json_without_ef_types()
    {
        var original = new OfferReference(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            SalesChannel.Marketplace,
            OfferStatus.Active,
            "SKU",
            "Default",
            null,
            1,
            10);
        var json = JsonSerializer.Serialize(original);
        var copy = JsonSerializer.Deserialize<OfferReference>(json);
        Assert.NotNull(copy);
        Assert.Equal(original, copy);
    }
}
