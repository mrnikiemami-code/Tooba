using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tooba.Payment.Infrastructure;
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
        var composer = CreateComposer(new PaymentGatewayOptions
        {
            Mode = "Sandbox",
            DefaultProvider = "fake",
            ManualCardToCardEnabled = true,
        });
        var page = composer.ListPaymentMethods();
        Assert.Contains(page.Methods, m => m.Code == "gateway");
        Assert.Contains(page.Methods, m => m.Code == "manual");
        Assert.True(page.ManualCardToCardEnabled);
    }

    [Fact]
    public void Disabled_mode_omits_gateway()
    {
        var composer = CreateComposer(new PaymentGatewayOptions
        {
            Mode = "Disabled",
            DefaultProvider = "webhook",
            ManualCardToCardEnabled = true,
        });
        var page = composer.ListPaymentMethods();
        Assert.DoesNotContain(page.Methods, m => m.Code == "gateway");
        Assert.Contains(page.Methods, m => m.Code == "manual");
    }

    [Fact]
    public void Webhook_without_config_omits_gateway()
    {
        var composer = CreateComposer(new PaymentGatewayOptions
        {
            Mode = "Webhook",
            DefaultProvider = "webhook",
            InitiateBaseUrl = "",
            WebhookSigningSecret = "",
            ManualCardToCardEnabled = false,
        });
        var page = composer.ListPaymentMethods();
        Assert.Empty(page.Methods);
    }

    [Fact]
    public void Webhook_configured_offers_gateway()
    {
        var composer = CreateComposer(new PaymentGatewayOptions
        {
            Mode = "Webhook",
            DefaultProvider = "webhook",
            InitiateBaseUrl = "https://payments.example/pay",
            WebhookSigningSecret = "secret",
            ManualCardToCardEnabled = false,
        });
        var page = composer.ListPaymentMethods();
        Assert.Contains(page.Methods, m => m.Code == "gateway");
        Assert.DoesNotContain(page.Methods, m => m.Code == "manual");
    }

    [Fact]
    public void Production_environment_does_not_enable_sandbox_simulator()
    {
        var composer = new Storefront.StorefrontPaymentComposer(
            checkouts: null!,
            payments: null!,
            orderPayments: null!,
            wallets: null!,
            gatewayOptions: Options.Create(new PaymentGatewayOptions { Mode = "Sandbox" }),
            session: new CurrentAuthenticatedSession(),
            environment: new TestHostEnvironment { EnvironmentName = Environments.Production },
            media: null!,
            logger: NullLogger<Storefront.StorefrontPaymentComposer>.Instance);
        Assert.False(composer.IsSandboxSimulatorEnabled());
    }

    [Fact]
    public void Development_sandbox_mode_enables_simulator()
    {
        var composer = CreateComposer(new PaymentGatewayOptions { Mode = "Sandbox" });
        Assert.True(composer.IsSandboxSimulatorEnabled());
    }

    private static Storefront.StorefrontPaymentComposer CreateComposer(PaymentGatewayOptions options)
    {
        return new Storefront.StorefrontPaymentComposer(
            checkouts: null!,
            payments: null!,
            orderPayments: null!,
            wallets: null!,
            gatewayOptions: Options.Create(options),
            session: new CurrentAuthenticatedSession(),
            environment: new TestHostEnvironment(),
            media: null!,
            logger: NullLogger<Storefront.StorefrontPaymentComposer>.Instance);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
