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
    public void Endpoints_csproj_does_not_reference_infrastructure()
    {
        var refs = ProjectRefs("Tooba.Tax.Endpoints");
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Tooba.Tax.Application", StringComparison.Ordinal));
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
    public void Program_maps_tax_module()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("MapTaxModule()", program, StringComparison.Ordinal);
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
