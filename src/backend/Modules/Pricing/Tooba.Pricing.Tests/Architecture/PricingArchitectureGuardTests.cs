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

    [Fact]
    public void Pricing_golden_boundaries_remain_clean()
    {
        var domainRefs = ProjectRefs("Tooba.Pricing.Domain");
        Assert.DoesNotContain(domainRefs, x => x.Contains("Tooba.Pricing.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, x => x.Contains("Offer.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(ProjectRefs("Tooba.Pricing.Infrastructure"), x => x.Contains("Offer.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ProjectRefs("Tooba.Pricing.Infrastructure"), x => x.Contains("OfferDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("OfferDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(Sources("Tooba.Pricing.Contracts"), x => x.Text.Contains("namespace Tooba.Pricing.Domain", StringComparison.Ordinal));
        var directory = File.ReadAllText(Path.Combine(PricingRoot(), "Tooba.Pricing.Infrastructure", "Adapters", "PriceDirectory.cs"));
        Assert.Contains("IModuleCallTracer", directory, StringComparison.Ordinal);
        Assert.Contains("IOfferLookupGateway", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("OfferDbContext", directory, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(PricingRoot(), "Tooba.Pricing.Endpoints", "Errors", "PricingErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(PricingRoot(), "Tooba.Pricing.Endpoints", "Resources", "PricingErrors.resx")));
        Assert.True(File.Exists(Path.Combine(PricingRoot(), "Tooba.Pricing.Endpoints", "Resources", "PricingErrors.fa.resx")));
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("PricingDbContext", StringComparison.Ordinal))
            .ToList();
        Assert.Empty(hostHits);
        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
                        || x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                        || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(bypass.Count == 0, string.Join("; ", bypass));
    }

    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Pricing.Domain")
            .Concat(Sources("Tooba.Pricing.Application"))
            .Concat(Sources("Tooba.Pricing.Contracts"))
            .Concat(Sources("Tooba.Pricing.Infrastructure"))
            .Concat(Sources("Tooba.Pricing.Endpoints"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(PricingRoot(), projectFolder);
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

            yield return (Path.GetRelativePath(RepoRoot(), file), File.ReadAllText(file));
        }
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
