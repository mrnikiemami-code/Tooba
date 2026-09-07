using Tooba.Offer.Application;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T006: حاکمیت سیاست مرجوعی Offer.</summary>
public sealed class OfferReturnPolicyResolverTests
{
    [Fact]
    public void Default_resolves_store_window()
    {
        var resolver = new ReturnPolicyResolver(new ReturnPolicyOptions { DefaultReturnWindowDays = 7 });
        var resolved = resolver.ResolveForCheckout(OfferReturnPolicyChoices.Default, null);
        Assert.True(resolved.IsReturnable);
        Assert.Equal(7, resolved.WindowDays);
        Assert.Equal("platform_default", resolved.Source);
        Assert.Contains("روز پس از تحویل", resolved.LabelFa);
    }

    [Fact]
    public void Custom_allowed_when_override_enabled()
    {
        var resolver = new ReturnPolicyResolver(new ReturnPolicyOptions
        {
            SellerCanOverrideReturnPolicy = true,
            MinReturnWindowDays = 1,
            MaxReturnWindowDays = 30,
        });
        var resolved = resolver.ResolveForCheckout(OfferReturnPolicyChoices.Custom, 14);
        Assert.Equal(14, resolved.WindowDays);
        Assert.Equal("offer_override", resolved.Source);
    }

    [Fact]
    public void Custom_denied_when_override_disabled_falls_back_to_default()
    {
        var resolver = new ReturnPolicyResolver(new ReturnPolicyOptions
        {
            SellerCanOverrideReturnPolicy = false,
            DefaultReturnWindowDays = 7,
        });
        Assert.Throws<InvalidOperationException>(() =>
            resolver.ValidateOfferChoice(OfferReturnPolicyChoices.Custom, 10));
        var resolved = resolver.ResolveForCheckout(OfferReturnPolicyChoices.Custom, 10);
        Assert.Equal(7, resolved.WindowDays);
        Assert.Equal("platform_default", resolved.Source);
    }

    [Fact]
    public void Custom_rejects_below_min_and_above_max()
    {
        var resolver = new ReturnPolicyResolver(new ReturnPolicyOptions
        {
            MinReturnWindowDays = 3,
            MaxReturnWindowDays = 10,
        });
        Assert.Throws<InvalidOperationException>(() =>
            resolver.ValidateOfferChoice(OfferReturnPolicyChoices.Custom, 2));
        Assert.Throws<InvalidOperationException>(() =>
            resolver.ValidateOfferChoice(OfferReturnPolicyChoices.Custom, 11));
    }

    [Fact]
    public void NonReturnable_allowed_and_denied()
    {
        var allowed = new ReturnPolicyResolver(new ReturnPolicyOptions { AllowNonReturnableOffers = true });
        var ok = allowed.ResolveForCheckout(OfferReturnPolicyChoices.NonReturnable, null);
        Assert.False(ok.IsReturnable);
        Assert.Equal("غیرقابل مرجوعی", ok.LabelFa);

        var denied = new ReturnPolicyResolver(new ReturnPolicyOptions { AllowNonReturnableOffers = false });
        Assert.Throws<InvalidOperationException>(() =>
            denied.ValidateOfferChoice(OfferReturnPolicyChoices.NonReturnable, null));
    }

    [Fact]
    public void Category_restriction_wins()
    {
        var resolver = new ReturnPolicyResolver(new ReturnPolicyOptions());
        var resolved = resolver.ResolveForCheckout(OfferReturnPolicyChoices.Custom, 14, categoryForcesNonReturnable: true);
        Assert.False(resolved.IsReturnable);
        Assert.Equal("category_restriction", resolved.Source);
    }
}
