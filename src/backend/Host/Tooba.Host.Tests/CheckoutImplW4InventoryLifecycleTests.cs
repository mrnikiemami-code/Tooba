using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Infrastructure.Queries;
using Tooba.Promotion.Infrastructure.Messaging;
using Tooba.Promotion.Infrastructure.Adapters;
using Tooba.Promotion.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Messaging;
using Tooba.Inventory.Infrastructure.Adapters;
using Tooba.Inventory.Infrastructure.Directories;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-CHECKOUT-IMPL-W4 — Cancel/Restore/PaymentBridge behind Inventory.Contracts.</summary>
public sealed class CheckoutImplW4InventoryLifecycleTests
{
    private static string Root => FindRepoRoot();

    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void Order_Infrastructure_uses_lifecycle_contract_not_Inventory_Application()
    {
        var csproj = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/Tooba.Order.Infrastructure.csproj");
        Assert.DoesNotContain("Tooba.Inventory.Application", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Inventory.Contracts", csproj, StringComparison.Ordinal);

        var checkout = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/CheckoutDirectory.cs");
        Assert.Contains("IOrderInventoryLifecyclePort", checkout, StringComparison.Ordinal);
        Assert.Contains("ReleaseHeldReservationAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("ReacquireDurableHoldFromPreviousAsync", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Inventory.Application", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("IInventoryDirectory", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("CheckoutInventoryReservationAdapter", checkout, StringComparison.Ordinal);

        var bridge = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderPaymentBridge.cs");
        Assert.Contains("IOrderInventoryLifecyclePort", bridge, StringComparison.Ordinal);
        Assert.Contains("PromoteOrReacquireForManualPaymentReviewAsync", bridge, StringComparison.Ordinal);
        Assert.Contains("ReleaseIfHeldAsync", bridge, StringComparison.Ordinal);
        Assert.Contains("EnsurePaidDurableSupplyAsync", bridge, StringComparison.Ordinal);
        Assert.Contains("EnsurePaidDurableOrKeepPaidAsync", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Inventory.Application", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Inventory.Domain", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("IInventoryDirectory", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("StockReservationStatus", bridge, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_contracts_and_adapter_are_inventory_owned()
    {
        var contracts = Read("src/backend/Modules/Inventory/Tooba.Inventory.Contracts/OrderInventoryLifecycleContracts.cs");
        Assert.Contains("interface IOrderInventoryLifecyclePort", contracts, StringComparison.Ordinal);
        Assert.Contains("OrderInventoryPaidSupplyRequest", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Domain", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("IInventoryDirectory", contracts, StringComparison.Ordinal);

        var adapter = Read("src/backend/Modules/Inventory/Tooba.Inventory.Application/OrderInventoryLifecycleAdapter.cs");
        Assert.Contains("class OrderInventoryLifecycleAdapter : IOrderInventoryLifecyclePort", adapter, StringComparison.Ordinal);
        Assert.Contains("EnsureOrderSupplyAsync", adapter, StringComparison.Ordinal);
        Assert.Contains("OrderSupplyMode.EnsurePaidDurable", adapter, StringComparison.Ordinal);

        var module = Read("src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/InventoryModule.cs");
        Assert.Contains("IOrderInventoryLifecyclePort, OrderInventoryLifecycleAdapter", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Infra_to_Inventory_Application_edge_removed_from_baseline()
    {
        var baseline = Read("src/backend/Host/Tooba.Host.Tests/Baselines/tmar-infra-to-foreign-application.json");
        Assert.DoesNotContain(
            "Tooba.Order.Infrastructure -> Tooba.Inventory.Application",
            baseline,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_TransactionScope_and_checkout_reservation_port_remain()
    {
        var checkout = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/CheckoutDirectory.cs");
        Assert.Contains("using System.Transactions", checkout, StringComparison.Ordinal);
        Assert.Contains("ICheckoutInventoryReservationPort", checkout, StringComparison.Ordinal);
        var pm = Read("src/backend/Modules/Order/Tooba.Order.Application/CheckoutProcessManager.cs");
        Assert.Contains("TransactionScope", pm, StringComparison.Ordinal);
        Assert.Contains("ICheckoutInventoryReservationPort", pm, StringComparison.Ordinal);
    }

    [Fact]
    public void Frontend_production_tree_untouched_by_w4_contract_files()
    {
        var fe = Path.Combine(Root, "src", "frontend");
        Assert.True(Directory.Exists(fe));
        // Characterization: W4 deliverables live only under backend Inventory/Order modules.
        Assert.True(File.Exists(Path.Combine(Root, "src", "backend", "Modules", "Inventory", "Tooba.Inventory.Contracts", "OrderInventoryLifecycleContracts.cs")));
        Assert.True(File.Exists(Path.Combine(Root, "src", "backend", "Modules", "Inventory", "Tooba.Inventory.Application", "OrderInventoryLifecycleAdapter.cs")));
        Assert.False(Regex.IsMatch(
            Read("src/backend/Modules/Inventory/Tooba.Inventory.Contracts/OrderInventoryLifecycleContracts.cs"),
            @"src[/\\]frontend",
            RegexOptions.IgnoreCase));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
