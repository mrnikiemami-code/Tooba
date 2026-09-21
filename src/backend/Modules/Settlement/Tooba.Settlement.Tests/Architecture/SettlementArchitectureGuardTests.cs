using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Settlement.Tests.Architecture;

public sealed class SettlementArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Queries", "Commands", "Errors"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Messaging", "DependencyInjection", "Bridges", "Gateways", "Handlers",
            "Observability", "Queries", "Errors", "Migrations"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "MarketplaceDevelopmentBootstrap.cs",
        "ModuleMigrationRegistry.cs",
        "Program.cs",
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

    private static string SettlementRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Settlement");

    [Fact]
    public void Settlement_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        AssertNoRootDump("Tooba.Settlement.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Settlement.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Settlement.Infrastructure", AllowedInfrastructureFolders);
        AssertNamespacesAlign("Tooba.Settlement.Domain", "Tooba.Settlement.Domain");
        AssertNamespacesAlign("Tooba.Settlement.Application", "Tooba.Settlement.Application");
        AssertNamespacesAlign("Tooba.Settlement.Infrastructure", "Tooba.Settlement.Infrastructure");

        var appRefs = ProjectRefs("Tooba.Settlement.Application");
        Assert.DoesNotContain(appRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(appRefs, r => r.Contains(".Application", StringComparison.Ordinal) && !r.Contains("Settlement.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(appRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));

        var infraRefs = ProjectRefs("Tooba.Settlement.Infrastructure");
        Assert.Contains(infraRefs, r => r.Contains("Party.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Payment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Payment.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Payment.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Order.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Order.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Order.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Returns.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Returns.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Returns.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Party.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Party.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Party.Infrastructure", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(AllProductionSources(), x =>
            x.Text.Contains("PartyDbContext", StringComparison.Ordinal)
            || x.Text.Contains("PaymentDbContext", StringComparison.Ordinal)
            || x.Text.Contains("OrderDbContext", StringComparison.Ordinal)
            || x.Text.Contains("ReturnsDbContext", StringComparison.Ordinal));

        var directory = File.ReadAllText(Path.Combine(SettlementRoot(), "Tooba.Settlement.Infrastructure", "Directories", "SettlementDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostSettlementDbHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("SettlementDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostSettlementDbHits.Count == 0, "Host SettlementDbContext: " + string.Join("; ", hostSettlementDbHits));

        var settlementHostFiles = Directory.EnumerateFiles(Path.Combine(hostRoot, "Settlement"), "*.cs")
            .Concat(Directory.EnumerateFiles(Path.Combine(hostRoot, "Grid"), "*.cs"))
            .Where(path => Path.GetFileName(path).Contains("Settlement", StringComparison.OrdinalIgnoreCase)
                           || Path.GetFileName(path).Contains("Payout", StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var path in settlementHostFiles)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("PartyDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SettlementDbContext", text, StringComparison.Ordinal);
        }

        var endpoints = File.ReadAllText(Path.Combine(hostRoot, "Settlement", "SettlementEndpoints.cs"));
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(new { title", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("detail = ex.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("SettlementPanelComposer", endpoints, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(hostRoot, "Settlement", "SettlementPanelComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Grid", "AdminPayoutGridQueryEngine.cs")));
        Assert.True(File.Exists(Path.Combine(SettlementRoot(), "Tooba.Settlement.Infrastructure", "Queries", "AdminPayoutGridQueryEngine.cs")));

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
            .Where(x => !x.Msg.StartsWith("settlement.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("payout.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(SettlementRoot(), project);
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
        Sources("Tooba.Settlement.Domain")
            .Concat(Sources("Tooba.Settlement.Application"))
            .Concat(Sources("Tooba.Settlement.Infrastructure"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(SettlementRoot(), projectFolder);
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
        var csproj = Path.Combine(SettlementRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
