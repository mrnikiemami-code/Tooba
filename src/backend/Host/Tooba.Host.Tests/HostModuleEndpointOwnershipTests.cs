using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// HOST-MODULE-ENDPOINT-001 durable umbrella: COMPLETE HTTP-owning modules own Endpoints → ISender → Application.
/// </summary>
public sealed class HostModuleEndpointOwnershipTests
{
    private sealed record ModuleEndpointManifest(
        string Module,
        string EndpointsProjectFolder,
        string HostMapCall,
        string? LegacyHostEndpointRelativePath,
        bool RequireCompleteCqrs);

    /// <summary>
    /// Canonical COMPLETE HTTP-owning modules for this recovery wave.
    /// Offer is endpoint-owned but not COMPLETE — included for ownership smoke only.
    /// </summary>
    private static readonly ModuleEndpointManifest[] CompleteHttpModules =
    [
        new("Cart", "Tooba.Cart.Endpoints", "MapCartEndpoints()", "Cart/CartEndpoints.cs", true),
        new("Settlement", "Tooba.Settlement.Endpoints", "MapSettlementEndpoints()", "Settlement/SettlementEndpoints.cs", true),
        new("Fulfillment", "Tooba.Fulfillment.Endpoints", "MapFulfillmentEndpoints()", "Fulfillment/FulfillmentEndpoints.cs", true),
        new("Returns", "Tooba.Returns.Endpoints", "MapReturnEndpoints()", "Returns/ReturnEndpoints.cs", true),
        new("Notification", "Tooba.Notification.Endpoints", "MapNotificationEndpoints()", "Notification/NotificationEndpoints.cs", true),
        new("Support", "Tooba.Support.Endpoints", "MapSupportEndpoints()", "Support/SupportEndpoints.cs", true),
        new("Wallet", "Tooba.Wallet.Endpoints", "MapWalletEndpoints()", "Wallet/WalletEndpoints.cs", true),
    ];

    [Fact]
    public void SellerPanelEndpoints_do_not_map_offer_http_routes()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Seller",
            "SellerPanelEndpoints.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("MapGet(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/offers/", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers/{offerId:guid}/price\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers/{offerId:guid}/inventory\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_maps_offer_tax_pricing_modules()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("MapOfferModule()", text, StringComparison.Ordinal);
        Assert.Contains("MapTaxModule()", text, StringComparison.Ordinal);
        Assert.Contains("MapPricingModule()", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Complete_http_modules_own_endpoints_isender_and_mediatr()
    {
        var root = FindRepoRoot();
        var program = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        var hostRoot = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");

        foreach (var m in CompleteHttpModules)
        {
            var moduleRoot = Path.Combine(root, "src", "backend", "Modules", m.Module);
            var endpointsProj = Path.Combine(moduleRoot, m.EndpointsProjectFolder, m.EndpointsProjectFolder + ".csproj");
            Assert.True(File.Exists(endpointsProj), "missing Endpoints csproj: " + endpointsProj);

            Assert.Contains(m.HostMapCall, program, StringComparison.Ordinal);

            if (!string.IsNullOrWhiteSpace(m.LegacyHostEndpointRelativePath))
            {
                var legacy = Path.Combine(hostRoot, m.LegacyHostEndpointRelativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.False(File.Exists(legacy), "legacy Host endpoint must be absent: " + legacy);
            }

            var refs = ProjectRefs(endpointsProj);
            Assert.Contains(refs, r => r.Contains(".Application", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, r => r.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase));

            var endpointSources = EnumerateCs(Path.Combine(moduleRoot, m.EndpointsProjectFolder)).ToList();
            Assert.Contains(endpointSources, x => x.Text.Contains("ISender", StringComparison.Ordinal));

            if (m.RequireCompleteCqrs)
            {
                var appRoot = Path.Combine(moduleRoot, $"Tooba.{m.Module}.Application");
                var appSources = EnumerateCs(appRoot).ToList();
                Assert.Contains(appSources, x => x.Text.Contains("IRequestHandler<", StringComparison.Ordinal));
                Assert.Contains(appSources, x => x.Text.Contains("using MediatR", StringComparison.Ordinal));
            }
        }
    }

    private static IReadOnlyList<string> ProjectRefs(string csproj)
    {
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }

    private static IEnumerable<(string Path, string Text)> EnumerateCs(string root)
    {
        if (!Directory.Exists(root))
            yield break;
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                continue;
            yield return (file, File.ReadAllText(file));
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))
                || File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
