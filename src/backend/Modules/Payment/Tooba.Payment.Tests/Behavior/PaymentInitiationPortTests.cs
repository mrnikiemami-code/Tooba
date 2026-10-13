using Microsoft.Extensions.Configuration;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.Payment.Application.Storefront.Models;
using Tooba.Payment.Application.Storefront.Orchestration;
using Tooba.Payment.Contracts.Storefront;
using Tooba.Payment.Infrastructure.Adapters;
using Tooba.Payment.Infrastructure.DependencyInjection;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// Focused coverage for the new Contracts-only initiation seam (TB-P0-CHECKOUT-START-PAYMENT-S2-012R).
/// Scope is limited to the port actually implemented: mapping fidelity and the DI seam. Behavioural
/// ownership (actor policy, provider eligibility, wallet/manual/gateway routing, idempotency and
/// persistence) intentionally stays with the canonical StorefrontPaymentOrchestrator and is not
/// re-tested here.
/// </summary>
public sealed class PaymentInitiationPortTests
{
    private static StorefrontPaymentInitiationDto Sample() => new(
        PaymentId: Guid.Parse("11111111-1111-4111-8111-111111111111"),
        AttemptId: Guid.Parse("22222222-2222-4222-8222-222222222222"),
        CheckoutId: Guid.Parse("33333333-3333-4333-8333-333333333333"),
        Status: "PendingPayment",
        ProviderCode: Tooba.Payment.Contracts.Storefront.PaymentProviderCodes.GatewayCatalog,
        ProviderRequestReference: "psp-req-42",
        RedirectUrl: "https://psp.example/pay/42",
        Amount: 1234567.89m,
        Currency: "IRR",
        RequiresPspRedirect: true);

    [Fact]
    public void Contracts_snapshot_carries_every_identity_money_and_redirect_field_unchanged()
    {
        var dto = Sample();

        var snapshot = PaymentInitiationAdapter.Map(dto);

        Assert.Equal(dto.PaymentId, snapshot.PaymentId);
        Assert.Equal(dto.AttemptId, snapshot.AttemptId);
        Assert.Equal(dto.CheckoutId, snapshot.CheckoutId);
        Assert.Equal(dto.Status, snapshot.Status);
        Assert.Equal(dto.ProviderCode, snapshot.ProviderCode);
        Assert.Equal(dto.ProviderRequestReference, snapshot.ProviderRequestReference);
        Assert.Equal(dto.RedirectUrl, snapshot.RedirectUrl);
        Assert.Equal(dto.Amount, snapshot.Amount);
        Assert.Equal(dto.Currency, snapshot.Currency);
        Assert.True(snapshot.RequiresPspRedirect);
    }

    [Fact]
    public void Contracts_snapshot_does_not_invent_psp_redirect_when_orchestrator_declines_it()
    {
        var dto = Sample() with { RequiresPspRedirect = false, Status = "PendingPayment" };

        var snapshot = PaymentInitiationAdapter.Map(dto);

        Assert.False(snapshot.RequiresPspRedirect);
        Assert.Equal("PendingPayment", snapshot.Status);
    }

    [Fact]
    public void Mapping_fails_closed_when_orchestrator_result_is_missing()
    {
        Assert.Throws<ArgumentNullException>(() => PaymentInitiationAdapter.Map(null!));
    }

    [Fact]
    public void Adapter_delegates_only_to_the_canonical_orchestrator_and_exposes_no_second_path()
    {
        var adapter = typeof(PaymentInitiationAdapter);

        Assert.True(typeof(IPaymentInitiationPort).IsAssignableFrom(adapter));
        var dependency = Assert.Single(adapter.GetConstructors()).GetParameters();
        var only = Assert.Single(dependency);
        Assert.Equal(typeof(Func<StorefrontPaymentOrchestrator>), only.ParameterType);
        Assert.Single(adapter.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
    }

    [Fact]
    public void Payment_module_registers_the_contracts_initiation_port()
    {
        var services = new ServiceCollection();

        new PaymentModule().AddServices(
            services,
            new ConfigurationBuilder().Build(),
            new TestHostEnvironment { EnvironmentName = Environments.Development });

        var descriptor = Assert.Single(services.Where(d => d.ServiceType == typeof(IPaymentInitiationPort)));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Null(descriptor.ImplementationType);

        // The module must expose the port even when the Host-owned orchestrator is not yet resolvable:
        // the deferred factory is what keeps module DI validation independent of Host registration.
        services.AddScoped(_ => (StorefrontPaymentOrchestrator)null!);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<PaymentInitiationAdapter>(scope.ServiceProvider.GetRequiredService<IPaymentInitiationPort>());
    }

    /// <summary>Minimal development environment so module registration runs on the non-production branch.</summary>
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tooba.Payment.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
