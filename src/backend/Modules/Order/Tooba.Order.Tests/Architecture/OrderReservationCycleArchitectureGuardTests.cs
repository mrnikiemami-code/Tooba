using Xunit;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Tests.Architecture;

/// <summary>TB-TMAR-ORDER-GOLDEN-001-R8 — reservation cycle policy/retry/expiry ownership guards.</summary>
public sealed class OrderReservationCycleArchitectureGuardTests
{
    [Fact]
    public void Host_coordinator_and_policy_resolver_are_absent()
    {
        var host = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(host, "ReservationCycleCoordinator.cs")));
        Assert.False(File.Exists(Path.Combine(host, "ReservationCyclePolicyResolver.cs")));
        var program = File.ReadAllText(Path.Combine(host, "Program.cs"));
        Assert.DoesNotContain("AddScoped<ReservationCycleCoordinator>", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ReservationCyclePolicyResolver>", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_owns_policy_via_Catalog_Contracts_only()
    {
        var root = FindRepoRoot();
        var resolver = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "ReservationCycle", "Policies", "ReservationCyclePolicyResolver.cs"));
        Assert.Contains("IReservationCycleHoldPolicyReader", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Domain", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.UtcNow", resolver, StringComparison.Ordinal);

        var contracts = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Contracts",
            "Reservation", "ReservationCycleHoldPolicyContracts.cs"));
        Assert.Contains("IReservationCycleHoldPolicyReader", contracts, StringComparison.Ordinal);
    }

    [Fact]
    public void Coordinator_uses_IClock_and_typed_fault_without_OrderDbContext()
    {
        var root = FindRepoRoot();
        var coordinator = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "ReservationCycle", "Services", "ReservationCycleCoordinator.cs"));
        Assert.Contains("_clock.UtcNow", coordinator, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException", coordinator, StringComparison.Ordinal);
        Assert.Contains("ReservationCycleErrors.RetryLimitReached", coordinator, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", coordinator, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", coordinator, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.UtcNow", coordinator, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", coordinator, StringComparison.Ordinal);
    }

    [Fact]
    public void Unpaid_expiry_is_owned_by_Order_and_absent_from_Host()
    {
        var root = FindRepoRoot();
        var host = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");
        Assert.False(
            File.Exists(Path.Combine(host, "UnpaidOrderExpiryHostedService.cs")),
            "Host must not host the Order unpaid-expiry worker after Order owns it");

        var reconciler = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "ReservationCycle", "UnpaidOrderExpiryReconciler.cs"));
        Assert.Contains("_clock.UtcNow", reconciler, StringComparison.Ordinal);
        Assert.Contains("IPaymentCustomerGateway", reconciler, StringComparison.Ordinal);
        Assert.Contains("IOrderPaymentProjectionPort", reconciler, StringComparison.Ordinal);
        Assert.Contains("IReservationCycleDirectory", reconciler, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", reconciler, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_policy_endpoints_owned_by_Order_without_CatalogDbContext()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints",
            "Admin", "Settings", "ReservationPolicyAdminEndpoints.cs"));
        Assert.DoesNotContain("is Tooba.Host.ReservationCyclePolicyResolver", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("IOrderAdminAuthorizer", endpoints, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "ReservationPolicyAdminEndpoints.cs")));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
