using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001 — Application foldering + namespace guards.</summary>
public sealed class OrderApplicationOrganizationGuardTests
{
    private static readonly string[] ForbiddenRootCapabilityFiles =
    [
        "CheckoutAbuseContracts.cs",
        "CheckoutCommitBarrier.cs",
        "CheckoutPayableInvariant.cs",
        "CheckoutProcessContracts.cs",
        "CheckoutProcessManager.cs",
        "IReservationCycleCoordinator.cs",
        "IUnpaidOrderExpiryReconciler.cs",
        "OrderContracts.cs",
        "ReservationCycleContracts.cs",
        "ReservationCycleCoordinator.cs",
        "ReservationCyclePolicyResolver.cs",
        "SellerOrderCancellationPolicy.cs",
    ];

    private static readonly (string RelativePath, string Namespace)[] RequiredAlignments =
    [
        ("Checkout/Abuse/CheckoutAbuseContracts.cs", "Tooba.Order.Application.Checkout.Abuse"),
        ("Checkout/Process/CheckoutProcessContracts.cs", "Tooba.Order.Application.Checkout.Process"),
        ("Checkout/Process/CheckoutProcessManager.cs", "Tooba.Order.Application.Checkout.Process"),
        ("Checkout/Process/CheckoutCommitBarrier.cs", "Tooba.Order.Application.Checkout.Process"),
        ("Checkout/Policies/CheckoutPayableInvariant.cs", "Tooba.Order.Application.Checkout.Policies"),
        ("Checkout/Contracts/CheckoutSnapshots.cs", "Tooba.Order.Application.Checkout.Contracts"),
        ("Checkout/Contracts/CheckoutDirectoryPorts.cs", "Tooba.Order.Application.Checkout.Contracts"),
        ("ReservationCycle/Contracts/IReservationCycleCoordinator.cs", "Tooba.Order.Application.ReservationCycle.Contracts"),
        ("ReservationCycle/Contracts/IUnpaidOrderExpiryReconciler.cs", "Tooba.Order.Application.ReservationCycle.Contracts"),
        ("ReservationCycle/Contracts/ReservationCycleContracts.cs", "Tooba.Order.Application.ReservationCycle.Contracts"),
        ("ReservationCycle/Services/ReservationCycleCoordinator.cs", "Tooba.Order.Application.ReservationCycle.Services"),
        ("ReservationCycle/Policies/ReservationCyclePolicyResolver.cs", "Tooba.Order.Application.ReservationCycle.Policies"),
        ("Seller/Policies/SellerOrderCancellationPolicy.cs", "Tooba.Order.Application.Seller.Policies"),
        ("PurchaseVerification/OrderPurchaseVerificationContracts.cs", "Tooba.Order.Application.PurchaseVerification"),
        ("Validation/OrderValidationCodes.cs", "Tooba.Order.Application.Validation"),
    ];

    [Fact]
    public void Forbidden_capability_files_are_absent_from_application_root()
    {
        var root = ApplicationRoot();
        foreach (var name in ForbiddenRootCapabilityFiles)
        {
            Assert.False(File.Exists(Path.Combine(root, name)), $"root still has {name}");
        }
    }

    [Fact]
    public void Remaining_root_cs_files_are_explicitly_allowlisted()
    {
        var rootFiles = Directory.GetFiles(ApplicationRoot(), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        // Post-closure: no unexplained capability dump at Application root.
        Assert.Empty(rootFiles);
    }

    [Fact]
    public void Moved_files_path_and_namespace_align()
    {
        foreach (var (relative, expectedNs) in RequiredAlignments)
        {
            var path = Path.Combine(ApplicationRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"missing {relative}");
            var text = File.ReadAllText(path);
            Assert.Contains($"namespace {expectedNs};", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Order.Application;", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Order.Application =", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_namespace_alias_workaround_for_order_application()
    {
        foreach (var file in Directory.GetFiles(ApplicationRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Application = Tooba.Order.Application", text, StringComparison.Ordinal);
            Assert.False(
                Regex.IsMatch(text, @"using\s+\w+\s*=\s*Tooba\.Order\.Application\s*;"),
                $"namespace alias workaround in {Path.GetRelativePath(ApplicationRoot(), file)}");
        }
    }

    private static string ApplicationRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application");

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
