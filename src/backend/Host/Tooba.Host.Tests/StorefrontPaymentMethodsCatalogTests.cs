using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.Payment.Infrastructure.Adapters;
using Tooba.Payment.Infrastructure.Providers;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T003 — کاتالوگ روش پرداخت ویترین فقط روش‌های عملیاتی Store را نشان می‌دهد.
/// </summary>
public sealed class StorefrontPaymentMethodsCatalogTests
{
    [Fact]
    public void Sandbox_mode_offers_gateway_and_manual_when_enabled()
    {
        var catalog = CreateCatalog(new PaymentGatewayOptions
        {
            Mode = "Sandbox",
            DefaultProvider = "fake",
            ManualCardToCardEnabled = true,
        });
        Assert.True(catalog.IsOnlineGatewayOffered());
        Assert.True(catalog.ManualCardToCardEnabled);
    }

    [Fact]
    public void Disabled_mode_omits_gateway()
    {
        var catalog = CreateCatalog(new PaymentGatewayOptions
        {
            Mode = "Disabled",
            DefaultProvider = "webhook",
            ManualCardToCardEnabled = true,
        });
        Assert.False(catalog.IsOnlineGatewayOffered());
        Assert.True(catalog.ManualCardToCardEnabled);
    }

    [Fact]
    public void Webhook_without_config_omits_gateway()
    {
        var catalog = CreateCatalog(new PaymentGatewayOptions
        {
            Mode = "Webhook",
            DefaultProvider = "webhook",
            InitiateBaseUrl = "",
            WebhookSigningSecret = "",
            ManualCardToCardEnabled = false,
        });
        Assert.False(catalog.IsOnlineGatewayOffered());
    }

    [Fact]
    public void Webhook_configured_offers_gateway()
    {
        var catalog = CreateCatalog(new PaymentGatewayOptions
        {
            Mode = "Webhook",
            DefaultProvider = "webhook",
            InitiateBaseUrl = "https://payments.example/pay",
            WebhookSigningSecret = "secret",
            ManualCardToCardEnabled = false,
        });
        Assert.True(catalog.IsOnlineGatewayOffered());
        Assert.False(catalog.ManualCardToCardEnabled);
    }

    [Fact]
    public void Production_environment_does_not_enable_sandbox_simulator()
    {
        var catalog = new PaymentGatewayCatalogAdapter(
            Options.Create(new PaymentGatewayOptions { Mode = "Sandbox" }),
            new TestHostEnvironment { EnvironmentName = Environments.Production });
        Assert.False(catalog.IsSandboxSimulatorEnabled());
    }

    [Fact]
    public void Development_sandbox_mode_enables_simulator()
    {
        var catalog = CreateCatalog(new PaymentGatewayOptions { Mode = "Sandbox" });
        Assert.True(catalog.IsSandboxSimulatorEnabled());
    }

    private static PaymentGatewayCatalogAdapter CreateCatalog(PaymentGatewayOptions options) =>
        new(Options.Create(options), new TestHostEnvironment());

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
