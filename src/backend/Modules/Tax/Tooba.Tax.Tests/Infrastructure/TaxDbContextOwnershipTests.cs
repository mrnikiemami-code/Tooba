using Microsoft.EntityFrameworkCore;
using Xunit;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Tax.Tests.Infrastructure;

public sealed class TaxDbContextOwnershipTests
{
    [Fact]
    public void TaxDbContext_schema_is_tax_and_maps_owned_entities()
    {
        var options = new DbContextOptionsBuilder<TaxDbContext>()
            .UseInMemoryDatabase("tax-ownership-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new TaxDbContext(options);
        Assert.Equal("tax", TaxDbContext.Schema);
        Assert.NotNull(db.Model.FindEntityType(typeof(TaxCategory)));
        Assert.NotNull(db.Model.FindEntityType(typeof(TaxRule)));
        Assert.NotNull(db.Model.FindEntityType(typeof(TaxOfferClassification)));
    }
}
