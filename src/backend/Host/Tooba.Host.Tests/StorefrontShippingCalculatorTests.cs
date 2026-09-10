using Tooba.Fulfillment.Application;
using Tooba.Host.Storefront;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>آزمون‌های متمرکز حداقل تحویل و قیمت ارسال فروشگاهی.</summary>
public sealed class StorefrontShippingCalculatorTests
{
    [Fact]
    public void Minimum_delivery_is_today_plus_max_prep_plus_lead()
    {
        var today = new DateOnly(2026, 9, 10);
        var min = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(today, maxSellerPreparationDays: 2, methodLeadDays: 3);
        Assert.Equal(new DateOnly(2026, 9, 15), min);
    }

    [Fact]
    public void Multi_seller_uses_slowest_preparation()
    {
        var options = new ShippingMethodsOptions
        {
            DefaultSellerPreparationDays = 1,
            SellerPreparationDaysByPartyId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["aaaaaaaa-aaaa-4aaa-8aaa-000000000001"] = 1,
                ["bbbbbbbb-bbbb-4bbb-8bbb-000000000002"] = 4,
            },
        };
        var max = StorefrontShippingCalculator.MaxSellerPreparationDays(
            [
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000001"),
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-000000000002"),
            ],
            options);
        Assert.Equal(4, max);
    }

    [Fact]
    public void Delivery_date_sublabel_is_jalali_persian_digits()
    {
        // 2026-09-14 Gregorian ≈ 1405/06/23 Jalali
        var label = StorefrontShippingCalculator.FormatDeliveryDateSubLabelFa(new DateOnly(2026, 9, 14));
        Assert.Contains("۱۴۰۵", label);
        Assert.DoesNotContain("2026", label);
        Assert.Equal("امروز", StorefrontShippingCalculator.FormatDeliveryDateLabelFa(new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 14)));
        Assert.Equal("فردا", StorefrontShippingCalculator.FormatDeliveryDateLabelFa(new DateOnly(2026, 9, 15), new DateOnly(2026, 9, 14)));
    }

    [Fact]
    public void Forged_earlier_delivery_is_rejected_later_accepted()
    {
        var min = new DateOnly(2026, 9, 12);
        Assert.Throws<InvalidOperationException>(() =>
            StorefrontShippingCalculator.EnsureDeliveryNotEarlier(new DateOnly(2026, 9, 11), min));
        StorefrontShippingCalculator.EnsureDeliveryNotEarlier(min, min);
        StorefrontShippingCalculator.EnsureDeliveryNotEarlier(new DateOnly(2026, 9, 14), min);
    }

    [Fact]
    public void Free_shipping_only_when_threshold_met()
    {
        var rate = new ShippingMethodRateOptions
        {
            Code = "post",
            BasePrice = 150_000m,
            FreeAboveSubtotal = 1_000_000m,
        };
        Assert.Equal(150_000m, StorefrontShippingCalculator.QuotePrice(rate, 500_000m));
        Assert.Equal(0m, StorefrontShippingCalculator.QuotePrice(rate, 1_000_000m));
    }

    [Fact]
    public void Disabled_or_unknown_destination_filtered_by_allowed_provinces()
    {
        var rate = new ShippingMethodRateOptions
        {
            Code = "snapp_courier",
            BasePrice = 90_000m,
            AllowedProvinces = ["تهران"],
        };
        Assert.True(StorefrontShippingCalculator.IsDestinationAllowed(rate, "تهران"));
        Assert.False(StorefrontShippingCalculator.IsDestinationAllowed(rate, "اصفهان"));
    }

    [Fact]
    public void Exact_method_rate_preferred_over_service_rate()
    {
        var options = new ShippingMethodsOptions
        {
            Rates =
            [
                new() { Code = "post", BasePrice = 150_000m, LeadDays = 3 },
                new() { Code = "post:express", BasePrice = 200_000m, LeadDays = 2 },
            ],
        };
        var rate = StorefrontShippingCalculator.ResolveRate("post:express", options);
        Assert.Equal(200_000m, rate.BasePrice);
        Assert.Equal(2, rate.LeadDays);
    }
}
