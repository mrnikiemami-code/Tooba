using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Storefront;
using Tooba.Payment.Infrastructure.Adapters;
using Tooba.Payment.Infrastructure.DependencyInjection;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// TB-P0-CHECKOUT-PROVIDER-ALIGNMENT-027 — رزولوشن انتخاب‌گر روش پرداخت پیش از commit:
/// bare «gateway» به Provider متعارف همان کاتالوگ درگاه حل می‌شود و انتخاب‌گر ناشناخته/غایب/مسدود
/// امن‌بسته (بدون Provider جایگزین ساکت) رد می‌شود. هیچ پیش‌فرض/مدت/fallback جدیدی اینجا ساخته نمی‌شود.
/// </summary>
public sealed class PaymentProviderSelectionAdapterTests
{
    private const string StoreDefaultProvider = "zarin-sandbox";

    [Fact]
    public async Task Wallet_selector_resolves_to_its_own_stable_code_without_touching_the_gateway_catalog()
    {
        var catalog = new CatalogFake { DefaultProvider = StoreDefaultProvider };
        var selection = await Resolve(catalog, requestedProviderCode: null, useWallet: true);

        Assert.NotNull(selection);
        Assert.Equal(PaymentProviderCodes.Wallet, selection!.CanonicalProviderCode);
    }

    [Fact]
    public async Task Neutral_gateway_alias_resolves_to_the_exact_catalog_default_provider()
    {
        var catalog = new CatalogFake { DefaultProvider = StoreDefaultProvider, OnlineGatewayOffered = true };

        var selection = await Resolve(catalog, PaymentProviderCodes.GatewayCatalog, useWallet: false);

        Assert.NotNull(selection);
        Assert.Equal(StoreDefaultProvider, selection!.CanonicalProviderCode);
        Assert.Equal(PaymentProviderCodes.GatewayCatalog, selection.RequestedSelector);
        Assert.NotEqual(PaymentProviderCodes.GatewayCatalog, selection.CanonicalProviderCode);
    }

    [Fact]
    public async Task Gateway_alias_fails_closed_when_the_online_gateway_is_not_offered()
    {
        var catalog = new CatalogFake { DefaultProvider = StoreDefaultProvider, OnlineGatewayOffered = false };

        Assert.Null(await Resolve(catalog, PaymentProviderCodes.GatewayCatalog, useWallet: false));
    }

    [Fact]
    public async Task Gateway_alias_fails_closed_when_no_concrete_default_provider_is_configured()
    {
        var catalog = new CatalogFake { DefaultProvider = "   ", OnlineGatewayOffered = true };

        Assert.Null(await Resolve(catalog, PaymentProviderCodes.GatewayCatalog, useWallet: false));
    }

    [Fact]
    public async Task Manual_selector_resolves_only_while_card_to_card_is_enabled()
    {
        var enabled = new CatalogFake { ManualCardToCardEnabled = true };
        var selection = await Resolve(enabled, PaymentProviderCodes.Manual, useWallet: false);
        Assert.NotNull(selection);
        Assert.Equal(PaymentProviderCodes.Manual, selection!.CanonicalProviderCode);

        var disabled = new CatalogFake { ManualCardToCardEnabled = false };
        Assert.Null(await Resolve(disabled, PaymentProviderCodes.Manual, useWallet: false));
    }

    [Fact]
    public async Task Absent_selector_fails_closed_instead_of_silently_selecting_a_provider()
    {
        var catalog = new CatalogFake { DefaultProvider = StoreDefaultProvider, OnlineGatewayOffered = true };

        Assert.Null(await Resolve(catalog, requestedProviderCode: null, useWallet: false));
        Assert.Null(await Resolve(catalog, requestedProviderCode: "   ", useWallet: false));
    }

    [Theory]
    [InlineData("unknown-provider")]
    [InlineData("wallet-ish")]
    [InlineData("MANUAL_CARD")]
    public async Task Unknown_or_foreign_selector_is_never_silently_remapped(string selector)
    {
        var catalog = new CatalogFake
        {
            DefaultProvider = StoreDefaultProvider,
            OnlineGatewayOffered = true,
            ManualCardToCardEnabled = true,
        };

        Assert.Null(await Resolve(catalog, selector, useWallet: false));
    }

    [Fact]
    public void The_single_provider_authority_is_the_orchestrator_catalog_and_the_adapter_reuses_it()
    {
        // Proof of "no duplicated policy": the orchestrator's gateway-alias branch and its default both
        // come from IPaymentGatewayCatalogPort.DefaultProvider, and this adapter reads the same member.
        var orchestrator = File.ReadAllText(OrchestratorPath());
        Assert.Contains("var providerCode = _catalog.DefaultProvider;", orchestrator, StringComparison.Ordinal);
        Assert.Contains("providerCode = _catalog.DefaultProvider;", orchestrator, StringComparison.Ordinal);

        var adapter = File.ReadAllText(AdapterPath());
        Assert.Contains("catalog.DefaultProvider", adapter, StringComparison.Ordinal);
        Assert.Contains("catalog.IsOnlineGatewayOffered()", adapter, StringComparison.Ordinal);
        Assert.Contains("catalog.ManualCardToCardEnabled", adapter, StringComparison.Ordinal);

        // No second source of truth / hardcoded provider or duration may appear in the adapter.
        foreach (var forbidden in new[] { "\"fake\"", "\"gateway\"", "\"wallet\"", "\"manual\"", "AddHours", "TimeSpan.From", "Environment." })
        {
            Assert.DoesNotContain(forbidden, adapter, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Payment_module_registers_the_contracts_only_provider_selection_port()
    {
        var services = new ServiceCollection();

        new PaymentModule().AddServices(
            services,
            new ConfigurationBuilder().Build(),
            new TestHostEnvironment { EnvironmentName = Environments.Development });

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IPaymentProviderSelectionPort));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(PaymentProviderSelectionAdapter), descriptor.ImplementationType);
    }

    private static Task<StorefrontProviderSelection?> Resolve(
        IPaymentGatewayCatalogPort catalog,
        string? requestedProviderCode,
        bool useWallet) =>
        new PaymentProviderSelectionAdapter(catalog).ResolveAsync(requestedProviderCode, useWallet, CancellationToken.None);

    private static string AdapterPath() => Path.Combine(
        ModuleRoot(), "Tooba.Payment.Infrastructure", "Adapters", "PaymentProviderSelectionAdapter.cs");

    private static string OrchestratorPath() => Path.Combine(
        ModuleRoot(), "Tooba.Payment.Application", "Storefront", "Orchestration", "StorefrontPaymentOrchestrator.cs");

    private static string ModuleRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Payment");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    /// <summary>Only the catalog authority is faked; no provider/duration rule is re-implemented.</summary>
    private sealed class CatalogFake : IPaymentGatewayCatalogPort
    {
        public string DefaultProvider { get; set; } = "fake";

        public bool ManualCardToCardEnabled { get; set; }

        public string ManualProofRequirement { get; set; } = "Optional";

        public string ManualPaymentInstructions { get; set; } = string.Empty;

        public string StoreDisplayName { get; set; } = "Tooba";

        public int ManualPaymentReviewHoldHours { get; set; } = 24;

        public bool OnlineGatewayOffered { get; set; } = true;

        public bool IsOnlineGatewayOffered() => OnlineGatewayOffered;

        public bool IsSandboxSimulatorEnabled() => false;

        public void MarkSandboxDecline(string providerRequestReference)
        {
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Tooba.Payment.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
