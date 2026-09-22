using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>HOST-FOLDER-001 — جلوگیری از dump بی‌ضابطه در ریشهٔ Tooba.Host.</summary>
public sealed class HostFolderStructureTests
{
    private static readonly HashSet<string> RootCsAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Program.cs",
        // W1 deferred: checkout/order/payment-adjacent root leftovers
        "CartExpiryHostedService.cs",
        "CartExpiryHostOptions.cs",
        "CheckoutReservationHoldPolicy.cs",
        "CommerceHoldPolicy.cs",
        "FulfillmentReturnsGridAliases.cs",
        "GlobalUsings.CartSettlementApp.cs",
        "GlobalUsings.CartSettlementDomain.cs",
        "OfferGlobalUsings.cs",
        "UnpaidOrderExpiryHostedService.cs",
        "UnpaidOrderExpiryHostOptions.cs",
        "PaymentReconciliationHostedService.cs",
        "PaymentReconciliationHostOptions.cs",
    };

    [Fact]
    public void Host_root_cs_files_are_limited_to_allowlist()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var rootCs = Directory.GetFiles(hostRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .Cast<string>()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var unexpected = rootCs.Where(n => !RootCsAllowlist.Contains(n)).ToArray();
        Assert.True(
            unexpected.Length == 0,
            "HOST-FOLDER-001 unexpected root .cs: " + string.Join(", ", unexpected));

        foreach (var allowed in RootCsAllowlist)
        {
            Assert.True(
                File.Exists(Path.Combine(hostRoot, allowed)),
                "allowlisted root file missing: " + allowed);
        }
    }

    [Fact]
    public void Host_gitignore_covers_runtime_log_artifacts()
    {
        var gitignore = File.ReadAllText(Path.Combine(FindRepoRoot(), ".gitignore"));
        Assert.Contains("src/backend/Host/Tooba.Host/*.log", gitignore, StringComparison.Ordinal);
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
