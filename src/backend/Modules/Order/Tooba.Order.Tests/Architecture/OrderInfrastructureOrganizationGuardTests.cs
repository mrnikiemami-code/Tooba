using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002 — Infrastructure capability foldering + namespace guards.</summary>
public sealed class OrderInfrastructureOrganizationGuardTests
{
    private static readonly string[] ForbiddenRootCapabilityFiles =
    [
        "CheckoutDirectory.cs",
        "CheckoutProcessTracker.cs",
        "CheckoutSubmitHost.cs",
        "OpenOrderUseCaseGuard.cs",
        "OrderFulfillmentBridge.cs",
        "OrderGridEnrichmentBridge.cs",
        "OrderNotificationBridge.cs",
        "OrderOutboxRegistration.cs",
        "OrderPaymentBridge.cs",
        "OrderPaymentSucceededHandler.cs",
        "OrderPurchaseVerificationGateway.cs",
        "OrderReturnBridge.cs",
        "ReservationCycleCheckoutLineSource.cs",
        "ReservationCycleDirectory.cs",
        "SellerOrderAuthBridge.cs",
        "UnpaidOrderExpiryReconciler.cs",
    ];

    private static readonly string[] AllowedRootCsFiles =
    [
        "OrderModule.cs",
    ];

    private static readonly string[] ForbiddenTopLevelFolders =
    [
        "CheckoutAbuse",
        "Payments",
        "Fulfillment",
    ];

    private static readonly (string RelativePath, string Namespace)[] RequiredAlignments =
    [
        ("Checkout/Abuse/CheckoutAbuseGate.cs", "Tooba.Order.Infrastructure.Checkout.Abuse"),
        ("Checkout/Persistence/CheckoutDirectory.cs", "Tooba.Order.Infrastructure.Checkout.Persistence"),
        ("Checkout/Persistence/CheckoutSubmitHost.cs", "Tooba.Order.Infrastructure.Checkout.Persistence"),
        ("Checkout/Persistence/CheckoutProcessTracker.cs", "Tooba.Order.Infrastructure.Checkout.Persistence"),
        ("ReservationCycle/ReservationCycleDirectory.cs", "Tooba.Order.Infrastructure.ReservationCycle"),
        ("ReservationCycle/ReservationCycleCheckoutLineSource.cs", "Tooba.Order.Infrastructure.ReservationCycle"),
        ("ReservationCycle/UnpaidOrderExpiryReconciler.cs", "Tooba.Order.Infrastructure.ReservationCycle"),
        ("Seller/SellerOrderAuthBridge.cs", "Tooba.Order.Infrastructure.Seller"),
        ("Customer/CustomerCheckoutOwnershipBridge.cs", "Tooba.Order.Infrastructure.Customer"),
        ("PurchaseVerification/OrderPurchaseVerificationGateway.cs", "Tooba.Order.Infrastructure.PurchaseVerification"),
        ("Integrations/Fulfillment/OrderFulfillmentBridge.cs", "Tooba.Order.Infrastructure.Integrations.Fulfillment"),
        ("Integrations/Payment/OrderPaymentBridge.cs", "Tooba.Order.Infrastructure.Integrations.Payment"),
        ("Integrations/Payment/CheckoutPaymentAccessBridge.cs", "Tooba.Order.Infrastructure.Integrations.Payment"),
        ("Integrations/Payment/PaymentAdminOrderEnrichmentBridge.cs", "Tooba.Order.Infrastructure.Integrations.Payment"),
        ("Integrations/Payment/OrderUnpaidRetrySupplyBridge.cs", "Tooba.Order.Infrastructure.Integrations.Payment"),
        ("Integrations/Returns/OrderReturnBridge.cs", "Tooba.Order.Infrastructure.Integrations.Returns"),
        ("Integrations/Notifications/OrderNotificationBridge.cs", "Tooba.Order.Infrastructure.Integrations.Notifications"),
        ("Integrations/GridEnrichment/OrderGridEnrichmentBridge.cs", "Tooba.Order.Infrastructure.Integrations.GridEnrichment"),
        ("Admin/Fulfillment/AdminOrderFulfillmentOperations.cs", "Tooba.Order.Infrastructure.Admin.Fulfillment"),
        ("Events/Payment/OrderPaymentSucceededHandler.cs", "Tooba.Order.Infrastructure.Events.Payment"),
        ("Guards/OpenOrderUseCaseGuard.cs", "Tooba.Order.Infrastructure.Guards"),
        ("Messaging/OrderOutboxRegistration.cs", "Tooba.Order.Infrastructure.Messaging"),
        ("Persistence/OrderDbContext.cs", "Tooba.Order.Infrastructure.Persistence"),
    ];

    [Fact]
    public void Forbidden_capability_files_are_absent_from_infrastructure_root()
    {
        var root = InfrastructureRoot();
        foreach (var name in ForbiddenRootCapabilityFiles)
        {
            Assert.False(File.Exists(Path.Combine(root, name)), $"root still has {name}");
        }
    }

    [Fact]
    public void Remaining_root_cs_files_are_explicitly_allowlisted()
    {
        var rootFiles = Directory.GetFiles(InfrastructureRoot(), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(AllowedRootCsFiles, rootFiles);
    }

    [Fact]
    public void Redundant_top_level_folders_are_gone()
    {
        foreach (var folder in ForbiddenTopLevelFolders)
        {
            Assert.False(
                Directory.Exists(Path.Combine(InfrastructureRoot(), folder)),
                $"redundant top-level folder remains: {folder}");
        }
    }

    [Fact]
    public void Moved_files_path_and_namespace_align()
    {
        foreach (var (relative, expectedNs) in RequiredAlignments)
        {
            var path = Path.Combine(InfrastructureRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"missing {relative}");
            var text = File.ReadAllText(path);
            Assert.Contains($"namespace {expectedNs};", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Order.Infrastructure;", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Payment_and_fulfillment_bridge_locations_are_not_duplicated()
    {
        var root = InfrastructureRoot();
        Assert.True(File.Exists(Path.Combine(root, "Integrations", "Payment", "OrderPaymentBridge.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Integrations", "Fulfillment", "OrderFulfillmentBridge.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Admin", "Fulfillment", "AdminOrderFulfillmentOperations.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, "Payments")));
        Assert.False(Directory.Exists(Path.Combine(root, "Fulfillment")));
    }

    [Fact]
    public void Persistence_migrations_path_unchanged()
    {
        var migrations = Path.Combine(InfrastructureRoot(), "Persistence", "Migrations");
        Assert.True(Directory.Exists(migrations));
        Assert.True(File.Exists(Path.Combine(InfrastructureRoot(), "Persistence", "OrderDbContext.cs")));
        Assert.Contains(
            Directory.GetFiles(migrations, "*.cs"),
            f => Path.GetFileName(f).Contains("OrderDbContextModelSnapshot", StringComparison.Ordinal));
    }

    [Fact]
    public void OrderModule_registers_key_services_once_and_stays_root()
    {
        var modulePath = Path.Combine(InfrastructureRoot(), "OrderModule.cs");
        Assert.True(File.Exists(modulePath));
        var module = File.ReadAllText(modulePath);
        Assert.Contains("namespace Tooba.Order.Infrastructure;", module, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(module, @"AddScoped<\s*ICheckoutDirectory\b").Count);
        Assert.Equal(1, Regex.Matches(module, @"AddScoped<\s*IOrderPaymentProjection\b").Count);
        Assert.Equal(1, Regex.Matches(module, @"AddScoped<\s*IAdminOrderFulfillmentOperations\b").Count);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<\s*IOutboxModuleRegistration\b").Count);
    }

    [Fact]
    public void No_infrastructure_namespace_alias_workaround()
    {
        foreach (var file in Directory.GetFiles(InfrastructureRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.False(
                Regex.IsMatch(text, @"using\s+\w+\s*=\s*Tooba\.Order\.Infrastructure\s*;"),
                $"namespace alias workaround in {Path.GetRelativePath(InfrastructureRoot(), file)}");
            Assert.DoesNotContain("Tooba.Host", text, StringComparison.Ordinal);
        }
    }

    private static string InfrastructureRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure");

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
}
