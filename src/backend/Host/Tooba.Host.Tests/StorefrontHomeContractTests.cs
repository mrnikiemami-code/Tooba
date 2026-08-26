using System.Text.Json;
using Tooba.Host.Storefront;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قرارداد JSON خانه برای fidelity Shopeiva بدون dump کامل رده‌ها در ریل خانه.
/// </summary>
public sealed class StorefrontHomeContractTests
{
    [Fact]
    public void Home_page_json_exposes_home_categories_best_sellers_and_most_viewed()
    {
        var home = new StorefrontHomePage(
            Categories: [],
            FeaturedProducts: [],
            SpecialOffers: [],
            CampaignProducts: [],
            NewArrivals: [],
            ProductRail: [],
            Brands: [],
            HeroTitle: "توبا",
            HeroSubtitle: "زنده",
            HomeCategories: [new StorefrontCategoryItem(Guid.NewGuid(), null, "موبایل")],
            BestSellerColumns:
            [
                new StorefrontBestSellerColumn(Guid.NewGuid(), "موبایل", []),
            ],
            MostViewedProducts: []);

        var json = JsonSerializer.Serialize(home);
        Assert.Contains("homeCategories", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bestSellerColumns", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mostViewedProducts", json, StringComparison.OrdinalIgnoreCase);
    }
}
