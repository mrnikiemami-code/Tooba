using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001 — durable guard for the six evacuated
/// root-level Host business-boundary files.
/// </summary>
public sealed class HostRootGlobalBoundariesGuardTests
{
    [Fact]
    public void Requested_root_host_files_are_absent()
    {
        var host = HostRoot();
        foreach (var file in new[]
                 {
                     "CheckoutReservationHoldPolicy.cs",
                     "CommerceHoldPolicy.cs",
                     "GlobalUsings.SettlementApp.cs",
                     "GlobalUsings.SettlementDomain.cs",
                     "UnpaidOrderExpiryHostedService.cs",
                     "UnpaidOrderExpiryHostOptions.cs",
                 })
        {
            Assert.False(File.Exists(Path.Combine(host, file)), $"Host root residue remains: {file}");
        }
    }

    [Fact]
    public void Commerce_hold_source_is_payment_owned_and_catalog_boundary_is_contracts_only()
    {
        var payment = Read(
            "src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/CommerceHoldPolicySource.cs");

        Assert.Contains("namespace Tooba.Payment.Infrastructure.Adapters;", payment, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Catalog.Contracts.Reservation;", payment, StringComparison.Ordinal);
        Assert.Contains("IStoreHoldPolicyHoursReader", payment, StringComparison.Ordinal);
        Assert.Contains("ICommerceHoldPolicySource", payment, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", payment, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", payment, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Domain", payment, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_checkout_adapter_consumes_payment_contract_only()
    {
        var adapter = Read(
            "src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Payment/CheckoutReservationHoldPolicyAdapter.cs");

        Assert.Contains("using Tooba.Payment.Contracts.Hold;", adapter, StringComparison.Ordinal);
        Assert.Contains("ICommerceHoldPolicySource", adapter, StringComparison.Ordinal);
        Assert.Contains("ICheckoutReservationHoldPolicy", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Application", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Infrastructure", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", adapter, StringComparison.Ordinal);
    }

    [Fact]
    public void Unpaid_expiry_worker_is_order_owned_and_host_independent()
    {
        var worker = Read(
            "src/backend/Modules/Order/Tooba.Order.Infrastructure/ReservationCycle/UnpaidOrderExpiryWorker.cs");
        var options = Read(
            "src/backend/Modules/Order/Tooba.Order.Infrastructure/ReservationCycle/UnpaidOrderExpiryWorkerOptions.cs");

        Assert.Contains("namespace Tooba.Order.Infrastructure.ReservationCycle;", worker, StringComparison.Ordinal);
        Assert.Contains("IUnpaidOrderExpiryReconciler", worker, StringComparison.Ordinal);
        Assert.Contains("IWorkerCommerceContextFactory", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", worker, StringComparison.Ordinal);

        Assert.Contains("Tooba:UnpaidOrderExpiry", options, StringComparison.Ordinal);
        Assert.Contains("PollIntervalSeconds", options, StringComparison.Ordinal);
        Assert.Contains("BatchSize", options, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", options, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_program_no_longer_registers_evacuated_business_types()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");

        Assert.DoesNotContain("UnpaidOrderExpiryHostOptions", program, StringComparison.Ordinal);
        Assert.DoesNotContain("UnpaidOrderExpiryHostedService", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CommerceHoldPolicy", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CheckoutReservationHoldPolicy", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Settlement_global_using_shims_are_zero()
    {
        var host = HostRoot();
        var globalUsings = Directory.GetFiles(host, "GlobalUsings*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText)
            .ToArray();

        Assert.DoesNotContain(globalUsings, x =>
            x.Contains("global using Tooba.Settlement.Application", StringComparison.Ordinal)
            || x.Contains("global using Tooba.Settlement.Infrastructure", StringComparison.Ordinal)
            || x.Contains("global using Tooba.Settlement.Domain", StringComparison.Ordinal));
    }

    private static string HostRoot() =>
        RepoFile("src/backend/Host/Tooba.Host");

    private static string Read(string relative) =>
        File.ReadAllText(RepoFile(relative));

    private static string RepoFile(string relative) =>
        Path.Combine(FindRepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tooba.sln"))
                || File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
