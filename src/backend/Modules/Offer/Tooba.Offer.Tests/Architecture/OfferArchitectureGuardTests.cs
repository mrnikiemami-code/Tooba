using Xunit;
using System.Xml.Linq;

namespace Tooba.Offer.Tests.Architecture;

public sealed class OfferArchitectureGuardTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || File.Exists(Path.Combine(dir.FullName, ".git", "HEAD")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string OfferRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Offer");

    [Fact]
    public void Domain_csproj_has_no_application_or_infrastructure_reference()
    {
        var refs = ProjectRefs("Tooba.Offer.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Endpoints_csproj_does_not_reference_infrastructure()
    {
        var refs = ProjectRefs("Tooba.Offer.Endpoints");
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Tooba.Offer.Application", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_csproj_does_not_reference_host_or_endpoints()
    {
        var refs = ProjectRefs("Tooba.Offer.Application");
        Assert.DoesNotContain(refs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void No_offer_production_source_exceeds_800_loc()
    {
        var root = OfferRoot();
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (n.Contains("/Tooba.Offer.Tests/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var loc = File.ReadAllLines(file).Length;
            if (loc > 800)
            {
                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{loc}");
            }
        }

        Assert.True(violations.Count == 0, "Offer >800 LOC: " + string.Join("; ", violations));
    }

    [Fact]
    public void Host_seller_panel_does_not_map_offer_routes()
    {
        var hostEndpoints = Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Seller",
            "SellerPanelEndpoints.cs");
        var text = File.ReadAllText(hostEndpoints);
        Assert.DoesNotContain("MapGet(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/offers/", text, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> ProjectRefs(string projectFolder)
    {
        var csproj = Path.Combine(OfferRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
