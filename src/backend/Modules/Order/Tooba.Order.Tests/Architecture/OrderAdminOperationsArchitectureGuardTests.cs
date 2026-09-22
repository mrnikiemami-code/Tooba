using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Queries.GetAdminOrderOperations;
using Tooba.Order.Application.Admin.Operations.Queries.ListAdminOrderReturnEligibility;
using Tooba.Order.Application.Admin.Operations.Commands.CancelOrder;
using Tooba.Order.Application.Admin.Operations.Commands.ConfirmDeposit;
using Xunit;

namespace Tooba.Order.Tests.Architecture;

public sealed class OrderAdminOperationsArchitectureGuardTests
{
    [Fact]
    public void Host_admin_order_operations_files_are_absent()
    {
        var hostAdmin = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin");
        Assert.False(File.Exists(Path.Combine(hostAdmin, "AdminOrderOperationsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "AdminOrderOperationsComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "AdminOrderOperationsModels.cs")));
    }

    [Fact]
    public void Order_endpoints_own_ops_routes_via_ISender()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "AdminOrderOperationsEndpoints.cs"));
        Assert.Contains("/{checkoutId:guid}/operations", endpoints, StringComparison.Ordinal);
        Assert.Contains("return-eligibility", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Infrastructure", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetAdminOrderOperationsQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("CancelOrderCommand", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Handlers_and_path_namespace_align_offer_style()
    {
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Operations", "Queries",
            "GetAdminOrderOperations", "GetAdminOrderOperationsQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Operations", "Queries",
            "ListAdminOrderReturnEligibility", "ListAdminOrderReturnEligibilityQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Operations", "Commands",
            "CancelOrder", "CancelOrderCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Operations", "Commands",
            "ConfirmDeposit", "ConfirmDepositCommand.cs")));
        Assert.Equal(
            typeof(GetAdminOrderOperationsQuery).Namespace,
            "Tooba.Order.Application.Admin.Operations.Queries.GetAdminOrderOperations");
        Assert.Equal(
            typeof(CancelOrderCommand).Namespace,
            "Tooba.Order.Application.Admin.Operations.Commands.CancelOrder");
        Assert.Equal(
            typeof(ListAdminOrderReturnEligibilityQuery).Namespace,
            "Tooba.Order.Application.Admin.Operations.Queries.ListAdminOrderReturnEligibility");
        Assert.Equal(
            typeof(ConfirmDepositCommand).Namespace,
            "Tooba.Order.Application.Admin.Operations.Commands.ConfirmDeposit");
    }

    [Fact]
    public void Application_ops_path_uses_contracts_only_and_stable_semantic_errors()
    {
        var appSources = Directory.GetFiles(
            Path.Combine(OrderRoot(), "Tooba.Order.Application", "Admin", "Operations"),
            "*.cs",
            SearchOption.AllDirectories);
        foreach (var path in appSources)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("Tooba.Fulfillment.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Fulfillment.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Returns.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Settlement.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Payment.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OrderDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
        }

        var orchestrator = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Operations", "Services",
            "AdminOrderOperationsOrchestrator.cs"));
        Assert.Contains("SemanticError", orchestrator, StringComparison.Ordinal);
        Assert.DoesNotContain("MapFulfillmentException", orchestrator, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_keeps_thin_recovery_supply_routes_without_ops_authority()
    {
        var hostEndpoints = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin",
            "AdminOrderInventoryRecoverySupplyEndpoints.cs"));
        Assert.Contains("inventory-recovery", hostEndpoints, StringComparison.Ordinal);
        Assert.Contains("supply-status", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("/operations", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("return-eligibility", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminOrderOperationsOrchestrator", hostEndpoints, StringComparison.Ordinal);
    }

    private static string OrderRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo.root.not_found");
    }
}
