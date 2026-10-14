using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Xunit;

namespace Tooba.Shipping.Tests.Compatibility;

/// <summary>
/// اثبات سازگاری: Shipping می‌تواند برای <b>هر کد خدمتی که کاتالوگ Fulfillment امروز منتشر می‌کند</b>
/// یک سیاست نرخ بیان کند، بدون آنکه ردیف کاتالوگ را تکرار یا بازتعریف کند.
/// </summary>
/// <remarks>
/// این تست تنها جایی است که Fulfillment دیده می‌شود و آن هم صرفاً از Contracts و در پروژهٔ تست؛
/// هیچ پروژهٔ production در Shipping به Fulfillment وابسته نیست. اگر Fulfillment بعداً کد جدیدی
/// منتشر کند، این تست خودش شکست می‌خورد و کسی باید آگاهانه تصمیم بگیرد — یعنی سازگاری «اثبات‌شده»
/// است، نه «فرض‌شده».
/// </remarks>
public sealed class ServiceCodeCompatibilityTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Every_registry_service_code_is_expressible_as_a_shipping_policy()
    {
        Assert.NotEmpty(ShippingMethodRegistry.All);

        foreach (var definition in ShippingMethodRegistry.All)
        {
            var policy = ShippingRatePolicy.Create(
                definition.Code,
                ShippingDestinationScope.ForCountry(Country),
                0m,
                "IRR",
                0,
                0,
                null,
                Now);

            Assert.Equal(definition.Code, policy.ServiceCode);
        }
    }

    [Fact]
    public void Every_configured_rate_code_is_expressible_as_a_shipping_policy()
    {
        var options = new ShippingMethodsOptions();
        Assert.NotEmpty(options.Rates);

        foreach (var rate in options.Rates)
        {
            var policy = ShippingRatePolicy.Create(
                rate.Code,
                ShippingDestinationScope.ForCountry(Country),
                Math.Max(0m, rate.BasePrice),
                "IRR",
                options.DefaultSellerPreparationDays,
                Math.Max(0, rate.LeadDays),
                rate.FreeAboveSubtotal,
                Now);

            Assert.Equal(rate.Code, policy.ServiceCode);
            Assert.Equal(Math.Max(0m, rate.BasePrice), policy.ChargeAmount);
        }
    }

    [Fact]
    public void Shipping_does_not_duplicate_the_fulfillment_catalog_identity()
    {
        // The Shipping aggregates carry no display name, icon, colour, sort order or translation: identity
        // and presentation stay Fulfillment's. Only the code crosses the boundary, as an opaque key.
        var policyProperties = typeof(ShippingRatePolicy).GetProperties().Select(x => x.Name).ToArray();
        var grantProperties = typeof(ShippingCoverageGrant).GetProperties().Select(x => x.Name).ToArray();

        foreach (var presentationName in new[] { "Name", "Label", "IconKey", "ColorKey", "SortOrder", "Translations", "ProviderKind" })
        {
            Assert.DoesNotContain(presentationName, policyProperties);
            Assert.DoesNotContain(presentationName, grantProperties);
        }
    }

    [Fact]
    public void Legacy_empty_allowed_provinces_semantics_are_not_reused_by_this_module()
    {
        // The legacy live route reads an empty AllowedProvinces array as "unrestricted". Shipping must never
        // reproduce that: with no coverage grant at all, the engine denies. This test pins that difference so
        // a future refactor cannot quietly import the old meaning into the new authority.
        var legacy = new ShippingMethodRateOptions { Code = "post", AllowedProvinces = [] };
        Assert.Empty(legacy.AllowedProvinces);

        var quote = Tooba.Shipping.Domain.Rates.ShippingRatePolicyEngine.Quote(
            new ShippingQuoteRequest(
                "post",
                new Tooba.Geography.Contracts.Places.DestinationIdentifiers(Country),
                0m),
            [ShippingRatePolicy.Create("post", ShippingDestinationScope.ForCountry(Country), 150m, "IRR", 1, 2, null, Now)],
            []);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
    }
}
