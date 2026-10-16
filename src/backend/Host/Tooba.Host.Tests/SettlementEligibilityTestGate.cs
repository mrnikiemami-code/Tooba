using Microsoft.Extensions.Configuration;
using Tooba.Order.Infrastructure.Integrations.Settlement;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Settlement.Application.Payouts.Ports;
using Tooba.Settlement.Infrastructure.Gateways;
using Tooba.Settlement.Infrastructure.Observability;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-143 — hand-built Settlement fixtures must pass the same per-event
/// eligibility gate the Host DI wires, so a manual fixture can never silently bypass the gate.
/// The Marketplace-edition configuration keeps the legacy "no channel filter" behaviour that these
/// R2 marketplace fixtures were written against.
/// </summary>
internal static class SettlementEligibilityTestGate
{
    internal static SettlementChannelLedgerEligibilityGate Marketplace(OrderDbContext orderDb) =>
        new(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Edition"] = "Marketplace",
            }).Build(),
            new OrderCheckoutSalesChannelSettlementBridge(orderDb),
            new AlwaysReadyLedgerSchema(),
            new SettlementInstrumentation());

    internal static SettlementChannelLedgerEligibilityGate SingleStoreWithCapability(OrderDbContext orderDb) =>
        new(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Edition"] = "SingleStore",
                ["Tooba:Settlement:MarketplaceChannelLedger:Enabled"] = "true",
            }).Build(),
            new OrderCheckoutSalesChannelSettlementBridge(orderDb),
            new AlwaysReadyLedgerSchema(),
            new SettlementInstrumentation());

    internal sealed class AlwaysReadyLedgerSchema : ISettlementLedgerSchemaReadiness
    {
        public Task<bool> IsLedgerReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
