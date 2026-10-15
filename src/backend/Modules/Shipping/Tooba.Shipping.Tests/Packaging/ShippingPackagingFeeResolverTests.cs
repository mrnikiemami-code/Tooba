using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Packaging;
using Xunit;

namespace Tooba.Shipping.Tests.Packaging;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-075 — حل قطعی هزینهٔ بسته‌بندی: تقدم گزینه سپس عمق دامنه، ابهام
/// fail-closed، پیش‌فرض صفر، و استقلال کامل از ارسال رایگان.
/// </summary>
public sealed class ShippingPackagingFeeResolverTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly Guid Area = Guid.Parse("1f8050a8-c325-59fa-959f-82d76b5b2bc5");
    private static readonly Guid City = Guid.Parse("68255340-c69f-5d20-bf33-e8e0b24edde9");
    private static readonly Guid ServiceArea = Guid.Parse("3f8050a8-c325-59fa-959f-82d76b5b2bc5");
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 7, 0, 0, TimeSpan.Zero);

    private static ShippingDestinationScope AreaScope => ShippingDestinationScope.ForAdministrativeArea(Country, Area);

    private static ShippingDestinationScope CityScope => ShippingDestinationScope.ForCity(Country, Area, City);

    private static ShippingPackagingPolicy Policy(
        decimal fee,
        ShippingDestinationScope scope,
        string? option = null,
        ShippingFreightPayerMode? payer = null) =>
        ShippingPackagingPolicy.Create(
            "tipax",
            scope,
            fee,
            "IRR",
            payer,
            Now,
            packagingPolicyId: null,
            optionCode: option);

    private static ShippingPackagingFeeRequest Request(string? option = null, Guid? cityId = null) =>
        new("tipax", new DestinationIdentifiers(Country, Area, cityId), option);

    [Fact]
    public void No_matching_policy_is_the_documented_zero_default_not_a_hidden_charge()
    {
        var component = ShippingPackagingFeeResolver.Resolve(Request(), []);

        Assert.False(component.IsCharged);
        Assert.Equal(0m, component.FeeAmount);
        Assert.Null(component.CurrencyCode);
        Assert.Null(component.PackagingPolicyId);
        Assert.Equal(ShippingPackagingReasonCodes.NotConfigured, component.ReasonCode);
        Assert.Equal(ShippingFreightPayerMode.Prepaid, component.FreightPayerMode);
        Assert.Equal(ShippingPackagingChargePayer.Checkout, component.PackagingPayer);
    }

    [Fact]
    public void A_matching_policy_is_reported_with_its_fee_currency_and_payer_mode()
    {
        var policy = Policy(12_000m, AreaScope, payer: ShippingFreightPayerMode.CollectOnDelivery);

        var component = ShippingPackagingFeeResolver.Resolve(Request(), [policy]);

        Assert.True(component.IsCharged);
        Assert.Equal(12_000m, component.FeeAmount);
        Assert.Equal("IRR", component.CurrencyCode);
        Assert.Equal(policy.PackagingPolicyId, component.PackagingPolicyId);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, component.FreightPayerMode);
        Assert.Equal(ShippingPackagingChargePayer.Checkout, component.PackagingPayer);
        Assert.Equal(ShippingPackagingReasonCodes.Charged, component.ReasonCode);
    }

    [Fact]
    public void Option_specific_policy_beats_the_service_level_policy()
    {
        var serviceLevel = Policy(5_000m, AreaScope);
        var express = Policy(9_000m, AreaScope, option: "express");

        var expressComponent = ShippingPackagingFeeResolver.Resolve(Request("express"), [serviceLevel, express]);
        var standardComponent = ShippingPackagingFeeResolver.Resolve(Request("standard"), [serviceLevel, express]);

        Assert.Equal(9_000m, expressComponent.FeeAmount);
        // The service-level policy is the documented fallback for any other option.
        Assert.Equal(5_000m, standardComponent.FeeAmount);
        Assert.Equal(serviceLevel.PackagingPolicyId, standardComponent.PackagingPolicyId);
    }

    [Fact]
    public void Deepest_destination_scope_wins_even_though_the_city_policy_is_conditioned_by_nothing_else()
    {
        var areaPolicy = Policy(5_000m, AreaScope);
        var cityPolicy = Policy(8_000m, CityScope);

        var cityComponent = ShippingPackagingFeeResolver.Resolve(Request(cityId: City), [areaPolicy, cityPolicy]);
        var areaComponent = ShippingPackagingFeeResolver.Resolve(Request(), [areaPolicy, cityPolicy]);

        Assert.Equal(8_000m, cityComponent.FeeAmount);
        Assert.Equal(cityPolicy.PackagingPolicyId, cityComponent.PackagingPolicyId);
        Assert.Equal(5_000m, areaComponent.FeeAmount);
    }

    [Fact]
    public void Service_area_policy_outranks_city_and_area_policies_for_its_own_destination()
    {
        var areaPolicy = Policy(5_000m, AreaScope);
        var cityPolicy = Policy(8_000m, CityScope);
        var serviceAreaPolicy = Policy(11_000m, ShippingDestinationScope.ForServiceArea(Country, Area, City, ServiceArea));

        var component = ShippingPackagingFeeResolver.Resolve(
            new ShippingPackagingFeeRequest(
                "tipax",
                new DestinationIdentifiers(Country, Area, City, ServiceArea)),
            [areaPolicy, cityPolicy, serviceAreaPolicy]);

        Assert.Equal(11_000m, component.FeeAmount);
        Assert.Equal(serviceAreaPolicy.PackagingPolicyId, component.PackagingPolicyId);
    }

    [Fact]
    public void Two_equally_specific_active_policies_are_ambiguous_and_charge_nothing()
    {
        var first = Policy(5_000m, AreaScope);
        var second = Policy(7_000m, AreaScope);

        var component = ShippingPackagingFeeResolver.Resolve(Request(), [first, second]);

        // Never "first row wins": the pair is a data conflict that only a human may resolve.
        Assert.False(component.IsCharged);
        Assert.Equal(0m, component.FeeAmount);
        Assert.Null(component.PackagingPolicyId);
        Assert.Equal(ShippingPackagingReasonCodes.Ambiguous, component.ReasonCode);
    }

    [Fact]
    public void An_inactive_policy_is_ignored_so_deactivation_restores_the_zero_default()
    {
        var policy = Policy(12_000m, AreaScope);
        policy.Deactivate(Now);

        var component = ShippingPackagingFeeResolver.Resolve(Request(), [policy]);

        Assert.False(component.IsCharged);
        Assert.Equal(ShippingPackagingReasonCodes.NotConfigured, component.ReasonCode);
    }

    [Fact]
    public void A_policy_for_another_country_never_matches()
    {
        var otherCountry = ShippingDestinationScope.ForAdministrativeArea(Guid.NewGuid(), Area);
        var policy = Policy(12_000m, otherCountry);

        var component = ShippingPackagingFeeResolver.Resolve(Request(), [policy]);

        Assert.False(component.IsCharged);
    }

    [Fact]
    public void A_zero_fee_policy_is_still_an_explicit_charged_component()
    {
        // Zero is a real configured decision, distinct from "no policy": the caller can tell them apart.
        var policy = Policy(0m, AreaScope);

        var component = ShippingPackagingFeeResolver.Resolve(Request(), [policy]);

        Assert.True(component.IsCharged);
        Assert.Equal(0m, component.FeeAmount);
        Assert.Equal(policy.PackagingPolicyId, component.PackagingPolicyId);
    }

    // ------------------------------------------------------------------
    // Payer mode is a quote fact with a documented Prepaid default
    // ------------------------------------------------------------------

    [Fact]
    public void Payer_mode_defaults_to_prepaid_when_the_body_omits_it()
    {
        var policy = Policy(12_000m, AreaScope, payer: null);

        Assert.Equal(ShippingFreightPayerMode.Prepaid, policy.FreightPayerMode);
    }

    [Fact]
    public void An_out_of_contract_payer_mode_is_refused_with_a_stable_code()
    {
        var thrown = Assert.Throws<SemanticException>(() => ShippingPackagingPolicy.Create(
            "tipax",
            AreaScope,
            12_000m,
            "IRR",
            (ShippingFreightPayerMode)99,
            Now));

        Assert.Equal(ShippingErrorCodes.PackagingFreightPayerModeInvalid, thrown.Message);
    }

    [Fact]
    public void Negative_and_oversized_fees_and_bad_currency_are_refused_with_stable_codes()
    {
        Assert.Equal(
            ShippingErrorCodes.PackagingFeeNegative,
            Assert.Throws<SemanticException>(() => Policy(-1m, AreaScope)).Message);

        Assert.Equal(
            ShippingErrorCodes.PackagingFeeOutOfRange,
            Assert.Throws<SemanticException>(() => Policy(ShippingPackagingPolicy.FeeAmountMaxValue + 1m, AreaScope)).Message);

        Assert.Equal(
            ShippingErrorCodes.PackagingCurrencyInvalid,
            Assert.Throws<SemanticException>(() => ShippingPackagingPolicy.Create(
                "tipax",
                AreaScope,
                1m,
                "IR",
                null,
                Now)).Message);
    }

    [Fact]
    public void A_service_area_without_its_owning_city_is_refused_as_a_half_scope()
    {
        var thrown = Assert.Throws<SemanticException>(() => Policy(
            1m,
            new ShippingDestinationScope(Country, null, null, ServiceArea)));

        Assert.Equal(ShippingErrorCodes.CoverageScopeInvalid, thrown.Message);
    }

    // ------------------------------------------------------------------
    // Free shipping is a FREIGHT rule and must never silently zero packaging
    // ------------------------------------------------------------------

    [Fact]
    public void A_free_freight_quote_does_not_remove_the_packaging_component()
    {
        // The freight engine zeroes the carrier charge above the threshold, but packaging is an
        // independent commercial fact and stays exactly as configured.
        var packaging = Policy(12_000m, AreaScope);

        var component = ShippingPackagingFeeResolver.Resolve(Request(), [packaging]);

        Assert.Equal(12_000m, component.FeeAmount);
        Assert.Equal(ShippingPackagingReasonCodes.Charged, component.ReasonCode);
    }
}
