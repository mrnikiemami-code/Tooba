using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Contracts.Settlement;
using Tooba.Payment.Contracts.Events;
using Tooba.Returns.Contracts.Events;
using Tooba.Settlement.Application.Payouts.Ports;
using Tooba.Settlement.Infrastructure.DependencyInjection;
using Tooba.Settlement.Infrastructure.Gateways;
using Tooba.Settlement.Infrastructure.Observability;
using Xunit;

namespace Tooba.Settlement.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-143 — دروازهٔ ثبت ledger تسویه: قابلیت ثبت‌نام (deployment) از واجد
/// شرایط بودن per-event (کانال فروش پایدار + آمادگی schema) جدا است و هیچ relation ناموجود تسویه
/// پرس‌وجو نمی‌شود.
/// </summary>
public sealed class SettlementChannelLedgerEligibilityTests
{
    [Fact]
    public void SingleStore_registration_is_off_until_the_capability_is_explicitly_enabled()
    {
        var single = Build(("Tooba:Edition", "SingleStore"));

        Assert.DoesNotContain(single, d => d.ServiceType == typeof(IIntegrationEventHandler<PaymentSucceededIntegrationEvent>));
        Assert.DoesNotContain(single, d => d.ServiceType == typeof(IIntegrationEventHandler<RefundSucceededIntegrationEvent>));
    }

    [Fact]
    public void SingleStore_registration_opens_only_for_the_explicit_capability_flag()
    {
        var enabled = Build(
            ("Tooba:Edition", "SingleStore"),
            (SettlementMarketplaceChannelLedgerOptions.SectionName + ":Enabled", "true"));

        Assert.Contains(enabled, d => d.ServiceType == typeof(IIntegrationEventHandler<PaymentSucceededIntegrationEvent>));
        Assert.Contains(enabled, d => d.ServiceType == typeof(IIntegrationEventHandler<RefundSucceededIntegrationEvent>));
        Assert.Contains(enabled, d => d.ServiceType == typeof(ISettlementChannelLedgerEligibilityGate));
        Assert.Contains(enabled, d => d.ServiceType == typeof(ISettlementLedgerSchemaReadiness));
    }

    [Fact]
    public void Marketplace_edition_registration_is_not_weakened_by_the_capability_flag()
    {
        var market = Build(("Tooba:Edition", "Marketplace"));

        Assert.Contains(market, d => d.ServiceType == typeof(IIntegrationEventHandler<PaymentSucceededIntegrationEvent>));
        Assert.Contains(market, d => d.ServiceType == typeof(IIntegrationEventHandler<RefundSucceededIntegrationEvent>));
    }

    [Fact]
    public async Task Marketplace_edition_keeps_legacy_per_event_behaviour_without_any_channel_or_schema_work()
    {
        var channels = new StaticChannelReader(SalesChannel.Direct);
        var gate = new SettlementChannelLedgerEligibilityGate(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Edition"] = "Marketplace",
            }).Build(),
            channels,
            new StaticSchemaReadiness(false),
            new SettlementInstrumentation());

        var decision = await gate.EvaluateAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(SettlementChannelLedgerEligibility.Eligible, decision);
        Assert.Equal(0, channels.Reads);
    }

    [Fact]
    public async Task SingleStore_capability_accepts_only_a_persisted_marketplace_channel_sale()
    {
        var checkoutId = Guid.NewGuid();
        var gate = Gate(
            config: ("Tooba:Edition", "SingleStore"),
            channel: SalesChannel.Marketplace,
            ledgerReady: true);

        Assert.Equal(
            SettlementChannelLedgerEligibility.Eligible,
            await gate.EvaluateAsync(checkoutId, CancellationToken.None));
    }

    [Fact]
    public async Task SingleStore_capability_rejects_an_ordinary_retail_channel_sale()
    {
        var gate = Gate(
            config: ("Tooba:Edition", "SingleStore"),
            channel: SalesChannel.Direct,
            ledgerReady: true);

        Assert.Equal(
            SettlementChannelLedgerEligibility.SaleChannelNotMarketplace,
            await gate.EvaluateAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task SingleStore_capability_rejects_an_unknown_checkout_instead_of_guessing()
    {
        var gate = Gate(
            config: ("Tooba:Edition", "SingleStore"),
            channel: null,
            ledgerReady: true);

        Assert.Equal(
            SettlementChannelLedgerEligibility.SaleChannelNotMarketplace,
            await gate.EvaluateAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task SingleStore_capability_fails_closed_when_the_ledger_schema_is_not_activated()
    {
        var gate = Gate(
            config: ("Tooba:Edition", "SingleStore"),
            channel: SalesChannel.Marketplace,
            ledgerReady: false);

        Assert.Equal(
            SettlementChannelLedgerEligibility.SchemaNotReady,
            await gate.EvaluateAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private static ServiceCollection Build(params (string Key, string Value)[] settings)
    {
        var services = new ServiceCollection();
        new SettlementModule().AddServices(
            services,
            new ConfigurationBuilder().AddInMemoryCollection(settings
                .ToDictionary(x => x.Key, x => (string?)x.Value)).Build(),
            new HostEnvironmentStub());
        return services;
    }

    private static SettlementChannelLedgerEligibilityGate Gate(
        (string Key, string Value) config,
        SalesChannel? channel,
        bool ledgerReady) =>
        new(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [config.Key] = config.Value,
                [SettlementMarketplaceChannelLedgerOptions.SectionName + ":Enabled"] = "true",
            }).Build(),
            new StaticChannelReader(channel),
            new StaticSchemaReadiness(ledgerReady),
            new SettlementInstrumentation());

    private sealed class StaticChannelReader(SalesChannel? channel) : IOrderCheckoutSalesChannelSettlementReader
    {
        public int Reads { get; private set; }

        public Task<OrderCheckoutSalesChannelSnapshot?> GetChannelAsync(
            Guid checkoutId,
            CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult(channel is null
                ? null
                : new OrderCheckoutSalesChannelSnapshot(checkoutId, channel.Value));
        }
    }

    private sealed class StaticSchemaReadiness(bool ready) : ISettlementLedgerSchemaReadiness
    {
        public Task<bool> IsLedgerReadyAsync(CancellationToken cancellationToken) => Task.FromResult(ready);
    }

    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tooba.Settlement.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
