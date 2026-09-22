using Tooba.Order.Application.Admin.Operations.Commands.CancelOrder;
using Tooba.Order.Application.Admin.Operations.Commands.ConfirmDeposit;
using Tooba.Order.Application.Admin.Operations.Queries.GetAdminOrderOperations;
using Tooba.Order.Application.Admin.Operations.Queries.ListAdminOrderReturnEligibility;
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
        Assert.False(File.Exists(Path.Combine(hostAdmin, "AdminFulfillmentCapabilityProjector.cs")));
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
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("when (ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.StartsWith", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapKnownOperationException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TryMapReturnCode", text, StringComparison.Ordinal);
        }

        var orchestrator = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Operations", "Services",
            "AdminOrderOperationsOrchestrator.cs"));
        Assert.Contains("SemanticError", orchestrator, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException", orchestrator, StringComparison.Ordinal);
        Assert.DoesNotContain("MapFulfillmentException", orchestrator, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_ops_has_no_central_ExecuteAsync_dispatcher()
    {
        var appSources = Directory.GetFiles(
            Path.Combine(OrderRoot(), "Tooba.Order.Application", "Admin", "Operations"),
            "*.cs",
            SearchOption.AllDirectories);
        foreach (var path in appSources)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("ExecuteAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExecuteCoreAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("with { Code =", text, StringComparison.Ordinal);
        }

        var orchestrator = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Operations", "Services",
            "AdminOrderOperationsOrchestrator.cs"));
        // Forbid ExecuteCoreAsync-style central dispatcher arms (mapping `code switch` helpers remain OK).
        Assert.DoesNotContain("=> await MarkProcessingCoreAsync", orchestrator, StringComparison.Ordinal);
        Assert.DoesNotContain("=> await MarkPackedCoreAsync", orchestrator, StringComparison.Ordinal);
        Assert.DoesNotContain("\"mark_processing\" => await", orchestrator, StringComparison.Ordinal);
        Assert.DoesNotContain("\"pack_selected\" => await", orchestrator, StringComparison.Ordinal);
        Assert.DoesNotContain("if (code == \"cancel\")", orchestrator, StringComparison.Ordinal);
        Assert.DoesNotContain("if (code == \"restore_deposit\")", orchestrator, StringComparison.Ordinal);
        Assert.DoesNotContain("var code = request.Code", orchestrator, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_handlers_call_typed_orchestrator_methods()
    {
        var commandsRoot = Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Operations", "Commands");
        var folders = Directory.GetDirectories(commandsRoot);
        Assert.True(folders.Length >= 27, $"expected >=27 command folders, got {folders.Length}");

        var typedSpotChecks = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CancelOrder"] = "CancelOrderAsync",
            ["ConfirmDeposit"] = "ConfirmDepositAsync",
            ["MarkFulfillmentProcessing"] = "MarkProcessingAsync",
            ["ApproveReturn"] = "ApproveReturnAsync",
            ["CreateShipment"] = "CreateShipmentAsync",
            ["RecoverInventoryReservation"] = "RecoverInventoryReservationAsync",
            ["PackFulfillmentSelected"] = "PackSelectedAsync",
        };

        foreach (var folder in folders)
        {
            var files = Directory.GetFiles(folder, "*Command.cs");
            Assert.True(files.Length == 1, $"expected one command file in {folder}");
            var text = File.ReadAllText(files[0]);
            Assert.Contains("IRequestHandler", text, StringComparison.Ordinal);
            Assert.DoesNotContain("with { Code =", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExecuteAsync", text, StringComparison.Ordinal);
            Assert.Matches(@"operations\.\w+Async\(", text);
        }

        foreach (var (folderName, method) in typedSpotChecks)
        {
            var path = Path.Combine(commandsRoot, folderName, $"{folderName}Command.cs");
            Assert.True(File.Exists(path), path);
            var text = File.ReadAllText(path);
            Assert.Contains($"operations.{method}(", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Host_recovery_supply_authority_files_are_absent()
    {
        var hostAdmin = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin");
        Assert.False(File.Exists(Path.Combine(hostAdmin, "OrderInventoryRecoveryComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "OrderSupplyComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "AdminOrderInventoryRecoverySupplyEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "HostAdminOrderOperationsInventoryRecoveryAdapter.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "HostAdminOrderOperationsSupplyAdapter.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "HostAdminOrderSupplyStatusReader.cs")));
    }

    [Fact]
    public void Order_endpoints_own_recovery_supply_routes_via_ISender()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "AdminOrderInventoryRecoverySupplyEndpoints.cs"));
        Assert.Contains("inventory-recovery/audit", endpoints, StringComparison.Ordinal);
        Assert.Contains("inventory-recovery", endpoints, StringComparison.Ordinal);
        Assert.Contains("supply-status", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("AuditOrderInventoryRecoveryQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("AssessOrderInventoryRecoveryQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetOrderSupplyStatusQuery", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Infrastructure", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Recovery_supply_cqrs_folders_align_offer_style()
    {
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "InventoryRecovery", "Queries",
            "AuditOrderInventoryRecovery", "AuditOrderInventoryRecoveryQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "InventoryRecovery", "Queries",
            "AssessOrderInventoryRecovery", "AssessOrderInventoryRecoveryQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Supply", "Queries",
            "GetOrderSupplyStatus", "GetOrderSupplyStatusQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "InventoryRecovery", "Commands",
            "RecoverOrderInventoryReservation", "RecoverOrderInventoryReservationCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Supply", "Commands",
            "EnsureOrderSupply", "EnsureOrderSupplyCommand.cs")));
    }

    [Fact]
    public void Application_recovery_supply_uses_contracts_only_and_no_message_parsing()
    {
        foreach (var folder in new[]
                 {
                     Path.Combine(OrderRoot(), "Tooba.Order.Application", "Admin", "InventoryRecovery"),
                     Path.Combine(OrderRoot(), "Tooba.Order.Application", "Admin", "Supply"),
                 })
        {
            foreach (var path in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(path);
                Assert.DoesNotContain("Tooba.Inventory.Application", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Tooba.Inventory.Infrastructure", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Tooba.Fulfillment.Application", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Tooba.Fulfillment.Infrastructure", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Tooba.Payment.Application", text, StringComparison.Ordinal);
                Assert.DoesNotContain("OrderDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
                Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
                Assert.DoesNotContain("exception.Message", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Contract_ops_adapters_do_not_parse_exception_message()
    {
        var adapters = new[]
        {
            Path.Combine(RepoRoot(), "src", "backend", "Modules", "Fulfillment",
                "Tooba.Fulfillment.Infrastructure", "Adapters", "FulfillmentAdminOperationsAdapter.cs"),
            Path.Combine(RepoRoot(), "src", "backend", "Modules", "Returns",
                "Tooba.Returns.Infrastructure", "Adapters", "ReturnAdminOperationsAdapter.cs"),
            Path.Combine(RepoRoot(), "src", "backend", "Modules", "Settlement",
                "Tooba.Settlement.Infrastructure", "Adapters", "SettlementOrderAccrualAdapter.cs"),
            Path.Combine(RepoRoot(), "src", "backend", "Modules", "Payment",
                "Tooba.Payment.Infrastructure", "Adapters", "PaymentHostContractBridge.cs"),
            Path.Combine(RepoRoot(), "src", "backend", "Modules", "Inventory",
                "Tooba.Inventory.Application", "Orders", "OrderInventoryLifecycleAdapter.cs"),
            Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order",
                "Tooba.Order.Infrastructure", "CheckoutDirectory.cs"),
            Path.Combine(RepoRoot(), "src", "backend", "BuildingBlocks",
                "Tooba.BuildingBlocks", "ContractOperationFault.cs"),
        };

        foreach (var path in adapters)
        {
            Assert.True(File.Exists(path), path);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("LooksLikeStableCode", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TryMapExact(ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TryMapExact(exception.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("when (ContractOperationFault", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (InvalidOperationException ex) when", text, StringComparison.Ordinal);
        }

        var fault = File.ReadAllText(adapters[^1]);
        Assert.DoesNotContain("LooksLikeStableCode", fault, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", fault, StringComparison.Ordinal);
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
