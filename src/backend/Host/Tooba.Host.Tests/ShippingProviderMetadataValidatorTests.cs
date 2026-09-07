using Tooba.Fulfillment.Application;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T006: اعتبارسنجی متادیتای روش ارسال.</summary>
public sealed class ShippingProviderMetadataValidatorTests
{
    [Fact]
    public void Registry_lists_enabled_methods()
    {
        var enabled = ShippingMethodRegistry.Enabled(new ShippingMethodsOptions());
        Assert.Contains(enabled, x => x.Code == "post");
        Assert.Contains(enabled, x => x.Code == "tipax");
        Assert.Contains(enabled, x => x.Code == "snapp_courier");
        Assert.Contains(enabled, x => x.Code == "store_courier");
        Assert.Contains(enabled, x => x.Code == "in_person");
    }

    [Fact]
    public void Post_requires_recipient_and_address()
    {
        var json = ShippingProviderMetadataValidator.ValidateAndNormalize(
            "post",
            """{"recipientName":"علی","destinationAddress":"تهران","recipientPhone":"09121234567","postalCode":"1234567890"}""");
        Assert.Contains("علی", json);
    }

    [Fact]
    public void Tipax_requires_name_and_address()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ShippingProviderMetadataValidator.ValidateAndNormalize("tipax", """{"recipientName":"علی"}"""));
        var json = ShippingProviderMetadataValidator.ValidateAndNormalize(
            "tipax",
            """{"recipientName":"علی","fullAddress":"تهران","province":"تهران","city":"تهران"}""");
        Assert.Contains("علی", json);
    }

    [Fact]
    public void Courier_requires_pickup_and_destination()
    {
        var json = ShippingProviderMetadataValidator.ValidateAndNormalize(
            "snapp_courier",
            """{"pickupAddress":"مبدأ","destinationAddress":"مقصد"}""");
        Assert.Contains("مبدأ", json);
    }

    [Fact]
    public void In_person_requires_location()
    {
        var json = ShippingProviderMetadataValidator.ValidateAndNormalize(
            "in_person",
            """{"pickupLocation":"فروشگاه مرکزی","readyNote":"آماده"}""");
        Assert.Contains("فروشگاه", json);
    }

    [Fact]
    public void Store_courier_allows_optional_fields()
    {
        var json = ShippingProviderMetadataValidator.ValidateAndNormalize("store_courier", "{}");
        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public void Disabled_unknown_method_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ShippingProviderMetadataValidator.ValidateAndNormalize("unknown", "{}"));
    }
}
