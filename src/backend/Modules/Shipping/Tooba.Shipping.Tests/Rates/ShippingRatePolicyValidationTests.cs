using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>اعتبارسنجی هویت و پارامترهای سیاست نرخ: مبلغ، ارز، زمان‌ها و انسجام دامنه.</summary>
public sealed class ShippingRatePolicyValidationTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Negative_charge_amount_is_rejected()
    {
        var error = Assert.Throws<SemanticException>(() => ShippingRatePolicy.Create(
            "post",
            ShippingDestinationScope.ForCountry(Country),
            -1m,
            "IRR",
            1,
            2,
            null,
            Now));

        Assert.Equal(ShippingErrorCodes.ChargeAmountNegative, error.Error.Code);
    }

    [Fact]
    public void Zero_charge_is_allowed_and_kept()
    {
        var policy = ShippingRatePolicy.Create(
            "in_person",
            ShippingDestinationScope.ForCountry(Country),
            0m,
            "IRR",
            0,
            0,
            null,
            Now);

        Assert.Equal(0m, policy.ChargeAmount);
        Assert.True(policy.IsActive);
    }

    [Fact]
    public void Negative_free_above_threshold_is_rejected()
    {
        var error = Assert.Throws<SemanticException>(() => ShippingRatePolicy.Create(
            "post",
            ShippingDestinationScope.ForCountry(Country),
            10m,
            "IRR",
            1,
            2,
            -0.01m,
            Now));

        Assert.Equal(ShippingErrorCodes.FreeAboveThresholdNegative, error.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_service_code_is_rejected(string serviceCode)
    {
        var error = Assert.Throws<SemanticException>(() => ShippingRatePolicy.Create(
            serviceCode,
            ShippingDestinationScope.ForCountry(Country),
            10m,
            "IRR",
            1,
            2,
            null,
            Now));

        Assert.Equal(ShippingErrorCodes.ServiceCodeRequired, error.Error.Code);
    }

    [Fact]
    public void Empty_country_identity_is_rejected()
    {
        var error = Assert.Throws<SemanticException>(() => ShippingRatePolicy.Create(
            "post",
            ShippingDestinationScope.ForCountry(Guid.Empty),
            10m,
            "IRR",
            1,
            2,
            null,
            Now));

        Assert.Equal(ShippingErrorCodes.DestinationCountryIdRequired, error.Error.Code);
    }

    [Theory]
    [InlineData("IR")]
    [InlineData("IRRX")]
    [InlineData("12$")]
    public void Currency_must_be_a_three_letter_code(string currencyCode)
    {
        var error = Assert.Throws<SemanticException>(() => ShippingRatePolicy.Create(
            "post",
            ShippingDestinationScope.ForCountry(Country),
            10m,
            currencyCode,
            1,
            2,
            null,
            Now));

        Assert.Equal(ShippingErrorCodes.CurrencyCodeInvalid, error.Error.Code);
    }

    [Fact]
    public void Negative_preparation_and_lead_days_are_rejected_separately()
    {
        var preparation = Assert.Throws<SemanticException>(() => ShippingRatePolicy.Create(
            "post", ShippingDestinationScope.ForCountry(Country), 10m, "IRR", -1, 2, null, Now));
        Assert.Equal(ShippingErrorCodes.PreparationDaysNegative, preparation.Error.Code);

        var lead = Assert.Throws<SemanticException>(() => ShippingRatePolicy.Create(
            "post", ShippingDestinationScope.ForCountry(Country), 10m, "IRR", 1, -2, null, Now));
        Assert.Equal(ShippingErrorCodes.LeadDaysNegative, lead.Error.Code);
    }

    [Fact]
    public void Service_area_scope_without_its_owning_city_is_rejected()
    {
        var halfScope = new ShippingDestinationScope(Country, ServiceAreaId: Guid.NewGuid());

        var error = Assert.Throws<SemanticException>(() => ShippingRatePolicy.Create(
            "post", halfScope, 10m, "IRR", 1, 2, null, Now));

        Assert.Equal(ShippingErrorCodes.CoverageScopeInvalid, error.Error.Code);

        var grantError = Assert.Throws<SemanticException>(() => ShippingCoverageGrant.Create(
            "post", halfScope, Now));
        Assert.Equal(ShippingErrorCodes.CoverageScopeInvalid, grantError.Error.Code);
    }

    [Fact]
    public void Preparation_and_lead_days_are_distinct_concepts_and_move_independently()
    {
        var policy = ShippingRatePolicy.Create(
            "tipax", ShippingDestinationScope.ForCountry(Country), 220m, "IRR", 1, 2, null, Now);

        policy.ChangeLeadTimes(3, 5, Now);

        Assert.Equal(3, policy.PreparationDays);
        Assert.Equal(5, policy.LeadDays);
    }

    [Fact]
    public void Deactivation_keeps_the_stable_identity()
    {
        var policy = ShippingRatePolicy.Create(
            "post", ShippingDestinationScope.ForCountry(Country), 10m, "IRR", 1, 2, null, Now, Guid.Parse("11111111-1111-4111-8111-111111111111"));

        policy.Deactivate(Now);

        Assert.False(policy.IsActive);
        Assert.Equal(Guid.Parse("11111111-1111-4111-8111-111111111111"), policy.RatePolicyId);
    }
}
