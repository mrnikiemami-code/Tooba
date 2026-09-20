using Microsoft.EntityFrameworkCore;
using Xunit;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure.Persistence;

namespace Tooba.Pricing.Tests.Infrastructure;

public sealed class PricingDbContextOwnershipTests
{
    [Fact]
    public void PricingDbContext_schema_is_pricing_and_maps_authored_price()
    {
        var options = new DbContextOptionsBuilder<PricingDbContext>()
            .UseInMemoryDatabase("pricing-ownership-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new PricingDbContext(options);
        Assert.Equal("pricing", PricingDbContext.Schema);
        Assert.NotNull(db.Model.FindEntityType(typeof(AuthoredPrice)));
    }
}
