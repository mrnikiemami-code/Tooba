using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Cart.Tests.Architecture;

public sealed class CartArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Lifetime", "Conversion"];
    private static readonly string[] AllowedContractsFolders = ["Checkout"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Messaging", "DependencyInjection", "Events", "Security", "Migrations"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProductWorkspaceDevelopmentBootstrap.cs",
        "ModuleMigrationRegistry.cs",
    };

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

        throw new InvalidOperationException("repo.root.not_found");
    }

    private static string CartRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Cart");

    [Fact]
    public void Cart_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        AssertNoRootDump("Tooba.Cart.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Cart.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Cart.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Cart.Infrastructure", AllowedInfrastructureFolders);
        AssertNamespacesAlign("Tooba.Cart.Domain", "Tooba.Cart.Domain");
        AssertNamespacesAlign("Tooba.Cart.Application", "Tooba.Cart.Application");
        AssertNamespacesAlign("Tooba.Cart.Contracts", "Tooba.Cart.Contracts");
        AssertNamespacesAlign("Tooba.Cart.Infrastructure", "Tooba.Cart.Infrastructure");

        var domainRefs = ProjectRefs("Tooba.Cart.Domain");
        Assert.DoesNotContain(domainRefs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));

        var appRefs = ProjectRefs("Tooba.Cart.Application");
        Assert.DoesNotContain(appRefs, r => r.Contains(".Application", StringComparison.Ordinal) && !r.Contains("Cart.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(appRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(appRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(appRefs, r => r.Contains("Catalog.Contracts", StringComparison.Ordinal));
        Assert.Contains(appRefs, r => r.Contains("Inventory.Contracts", StringComparison.Ordinal));

        var infraRefs = ProjectRefs("Tooba.Cart.Infrastructure");
        Assert.DoesNotContain(infraRefs, r => r.Contains(".Application", StringComparison.Ordinal) && !r.Contains("Cart.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(infraRefs, r => r.Contains(".Domain", StringComparison.Ordinal) && !r.Contains("Cart.", StringComparison.Ordinal));
        Assert.DoesNotContain(infraRefs, r => r.Contains(".Infrastructure", StringComparison.Ordinal) && !r.Contains("Cart.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x =>
            x.Text.Contains("CatalogDbContext", StringComparison.Ordinal)
            || x.Text.Contains("InventoryDbContext", StringComparison.Ordinal)
            || x.Text.Contains("OfferDbContext", StringComparison.Ordinal)
            || x.Text.Contains("PricingDbContext", StringComparison.Ordinal));

        var directory = File.ReadAllText(Path.Combine(CartRoot(), "Tooba.Cart.Infrastructure", "Directories", "CartDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("CartDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host CartDbContext production authority: " + string.Join("; ", hostHits));

        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
                        || x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                        || x.Text.Contains("?? new SystemUtcClock()", StringComparison.Ordinal)
                        || x.Text.Contains("?? new UuidV7IdGenerator()", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(bypass.Count == 0, string.Join("; ", bypass));

        var silentCatch = AllProductionSources()
            .Where(x => Regex.IsMatch(x.Text, @"catch\s*\(\s*Exception\s*\)\s*\{|catch\s*\{\s*\}|catch\s*\([^)]+\)\s*\{\s*\}", RegexOptions.Multiline))
            .Select(x => x.Path)
            .ToList();
        Assert.True(silentCatch.Count == 0, "silent/empty catch: " + string.Join("; ", silentCatch));

        var localized = AllProductionSources()
            .SelectMany(x => Regex.Matches(x.Text, @"throw new \w+Exception\(\s*""([^""]*)""\s*\)")
                .Select(m => (x.Path, Msg: m.Groups[1].Value)))
            .Where(x => Regex.IsMatch(x.Msg, @"[\u0600-\u06FF]") || x.Msg.Contains(' ', StringComparison.Ordinal))
            .Where(x => !x.Msg.StartsWith("cart.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("offer.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(CartRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name is not null && !name.StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj" or "artifacts") continue;
            Assert.Contains(name, allowedFolders);
        }
    }

    private static void AssertNamespacesAlign(string projectFolder, string nsPrefix)
    {
        var violations = new List<string>();
        foreach (var (path, text) in Sources(projectFolder))
        {
            if (Path.GetFileName(path).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                continue;
            var ns = Regex.Match(text, @"^namespace\s+([\w.]+)", RegexOptions.Multiline).Groups[1].Value;
            if (string.IsNullOrEmpty(ns) || !ns.StartsWith(nsPrefix, StringComparison.Ordinal))
            {
                violations.Add($"{path}: ns={ns}");
                continue;
            }

            var rel = path.Replace('\\', '/');
            var marker = projectFolder.Replace('\\', '/') + "/";
            var idx = rel.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) continue;
            var under = rel[(idx + marker.Length)..];
            var folder = under.Split('/')[0];
            if (folder.EndsWith(".cs", StringComparison.Ordinal)) continue;
            var expected = nsPrefix + "." + folder;
            if (!ns.Equals(nsPrefix, StringComparison.Ordinal)
                && !ns.StartsWith(expected, StringComparison.Ordinal))
                violations.Add($"{path}: ns={ns} expectedPrefix={expected}");
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Cart.Domain")
            .Concat(Sources("Tooba.Cart.Application"))
            .Concat(Sources("Tooba.Cart.Contracts"))
            .Concat(Sources("Tooba.Cart.Infrastructure"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(CartRoot(), projectFolder);
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                continue;
            if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
                continue;
            yield return (Path.GetRelativePath(RepoRoot(), file), File.ReadAllText(file));
        }
    }

    private static IReadOnlyList<string> ProjectRefs(string projectFolder)
    {
        var csproj = Path.Combine(CartRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
