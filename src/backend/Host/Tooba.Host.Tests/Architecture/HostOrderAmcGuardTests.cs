using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ORDER-AMC-001 — KEEP_AS_THIN_HOST_ORDER_STOREFRONT_ADAPTER.
/// </summary>
public sealed class HostOrderAmcGuardTests
{
    private static readonly string[] Allowlist =
    [
        "HostOrderStorefrontActor.cs",
    ];

    private static readonly Regex ForbiddenForeignLayers = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Payment|Reviews|Cart|Wallet|Support|ProductQnA|Preferences|Story|Wishlist|Fulfillment|Settlement|Notification|Returns|Promotion|Inventory|Pricing|Tax|Media|Content|User|OperatorProfile)\.(Domain|Infrastructure|Persistence)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Order_folder_matches_exact_retained_allowlist()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Order");
        Assert.True(Directory.Exists(folder), "Host/Order must remain PRESENT for KEEP disposition");

        var files = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Allowlist.OrderBy(x => x, StringComparer.Ordinal).ToArray(), files);
    }

    [Fact]
    public void Path_namespace_exact_and_thin_adapter_invariants()
    {
        var text = Read("HostOrderStorefrontActor.cs");
        var nsMatch = Regex.Match(text, @"^namespace\s+([^\s;{]+)", RegexOptions.Multiline);
        Assert.True(nsMatch.Success);
        Assert.Equal("Tooba.Host.Order", nsMatch.Groups[1].Value);

        Assert.Contains("class HostOrderStorefrontActor", text, StringComparison.Ordinal);
        Assert.Contains("class HostOrderStorefrontCheckoutIdentityGate", text, StringComparison.Ordinal);
        Assert.Contains("IOrderStorefrontActor", text, StringComparison.Ordinal);
        Assert.Contains("IOrderStorefrontCheckoutIdentityGate", text, StringComparison.Ordinal);
        Assert.Contains("StorefrontGuestActor.ActorId", text, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.CheckoutAuthenticationRequired", text, StringComparison.Ordinal);
        Assert.Contains("CheckoutIdentityGate", text, StringComparison.Ordinal);
        Assert.Contains("CartAccess", text, StringComparison.Ordinal);

        Assert.DoesNotContain("StorefrontCheckoutService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontOrderErrors", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("message.Contains", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GetRequiredService", text, StringComparison.Ordinal);

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            Assert.False(ForbiddenForeignLayers.IsMatch(line), line);
        }
    }

    [Fact]
    public void Program_registers_order_storefront_adapters()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("Tooba.Host.Order.HostOrderStorefrontActor", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Order.HostOrderStorefrontCheckoutIdentityGate", program, StringComparison.Ordinal);
        Assert.Contains("IOrderStorefrontActor", program, StringComparison.Ordinal);
        Assert.Contains("IOrderStorefrontCheckoutIdentityGate", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Sot_records_order_keep_disposition()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostOrderAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-ORDER-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_AS_THIN_HOST_ORDER_STOREFRONT_ADAPTER", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_ORDER_AMC_001_KEEP_THIN_HOST_ADAPTER", sot, StringComparison.Ordinal);
    }

    private static string Read(string fileName) =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Order", fileName));

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("repo root not found");
    }
}
