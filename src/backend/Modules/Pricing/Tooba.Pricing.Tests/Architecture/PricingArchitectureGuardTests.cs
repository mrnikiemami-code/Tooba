using System.Xml.Linq;
using Xunit;

namespace Tooba.Pricing.Tests.Architecture;

public sealed class PricingArchitectureGuardTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string PricingRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Pricing");

    [Fact]
    public void Domain_csproj_has_no_application_infrastructure_or_endpoints_reference()
    {
        var refs = ProjectRefs("Tooba.Pricing.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Offer.Domain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Endpoints_csproj_does_not_reference_infrastructure()
    {
        var refs = ProjectRefs("Tooba.Pricing.Endpoints");
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Tooba.Pricing.Application", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_uses_offer_contracts_not_offer_application()
    {
        var refs = ProjectRefs("Tooba.Pricing.Infrastructure");
        Assert.DoesNotContain(refs, r => r.Contains("Offer.Application", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Offer.Contracts", StringComparison.Ordinal));
    }

    [Fact]
    public void No_pricing_production_source_exceeds_800_loc()
    {
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(PricingRoot(), "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal)) continue;
            if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase)) continue;
            if (n.Contains("/Tooba.Pricing.Tests/", StringComparison.OrdinalIgnoreCase)) continue;
            var loc = File.ReadAllLines(file).Length;
            if (loc > 800) violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{loc}");
        }

        Assert.True(violations.Count == 0, "Pricing >800 LOC: " + string.Join("; ", violations));
    }

    [Fact]
    public void Program_maps_pricing_module()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("MapPricingModule()", program, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> ProjectRefs(string projectFolder)
    {
        var csproj = Path.Combine(PricingRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
