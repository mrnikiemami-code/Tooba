using Xunit;

namespace Tooba.Host.Tests;

public sealed class SellerOfferSaleWriteTests
{
    [Fact]
    public void Seller_panel_composer_has_no_offer_read_or_write_surface()
    {
        var names = typeof(Tooba.Host.Seller.SellerPanelComposer)
            .GetMethods()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("GetOfferAsync", names);
        Assert.DoesNotContain("ListOffersAsync", names);
        Assert.DoesNotContain("SetOfferPriceAsync", names);
        Assert.DoesNotContain("SetOfferInventoryAsync", names);
    }
}
