using System.Xml.Linq;
using Xunit;

namespace Tooba.Tax.Tests.Architecture;

public sealed class TaxArchitectureGuardTests
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

    private static string TaxRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Tax");

    [Fact]
    public void Domain_csproj_has_no_application_infrastructure_or_endpoints_reference()
    {
        var refs = ProjectRefs("Tooba.Tax.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Ceremonial_endpoints_project_stays_retired_internal_only()
    {
        // Tax owns zero HTTP routes and zero endpoint-reachable requests (AMSC-001 W0): the
        // ceremonial Endpoints project, its empty /v1/tax route group and its Host mapping were
        // removed by AMSC-001 W2. Tax is INTERNAL_ONLY and must not resurrect the ceremony.
        Assert.False(
            Directory.Exists(Path.Combine(TaxRoot(), "Tooba.Tax.Endpoints")),
            "the ceremonial Tooba.Tax.Endpoints project must stay retired");
        Assert.False(
            Directory.Exists(Path.Combine(TaxRoot(), "Tooba.Tax.Tests", "Endpoints")),
            "the Endpoints test folder must stay retired");
    }

    [Fact]
    public void No_tax_production_source_exceeds_800_loc()
    {
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(TaxRoot(), "*.cs", SearchOption.AllDirectories))
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

            if (n.Contains("/Tooba.Tax.Tests/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var loc = File.ReadAllLines(file).Length;
            if (loc > 800)
            {
                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{loc}");
            }
        }

        Assert.True(violations.Count == 0, "Tax >800 LOC: " + string.Join("; ", violations));
    }

    [Fact]
    public void Host_has_no_tax_owned_http_route_maps()
    {
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        foreach (var file in Directory.EnumerateFiles(hostRoot, "*Endpoints*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("MapGet(\"/tax", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapPost(\"/tax", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapGet(\"/taxes", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapPost(\"/taxes", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Program_no_longer_maps_a_tax_module_route_group()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.DoesNotContain("MapTaxModule()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Tax.Endpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/tax\"", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Tax_golden_boundaries_remain_clean()
    {
        // The Domain may reference only its OWN module Contracts (the canonical Contracts/Errors
        // stable-code home introduced by AMSC-001 W1); a foreign module Contracts reference stays forbidden.
        var domainRefs = ProjectRefs("Tooba.Tax.Domain");
        Assert.All(
            domainRefs.Where(x => x.Contains("Contracts", StringComparison.OrdinalIgnoreCase)),
            x => Assert.Contains("Tooba.Tax.Contracts", x, StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.DoesNotContain(Sources("Tooba.Tax.Contracts"), x => x.Text.Contains("namespace Tooba.Tax.Domain", StringComparison.Ordinal));
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("TaxDbContext", StringComparison.Ordinal))
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
        Sources("Tooba.Tax.Domain")
            .Concat(Sources("Tooba.Tax.Application"))
            .Concat(Sources("Tooba.Tax.Contracts"))
            .Concat(Sources("Tooba.Tax.Infrastructure"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(TaxRoot(), projectFolder);
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
        var csproj = Path.Combine(TaxRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
