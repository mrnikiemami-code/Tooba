using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Promotion.Tests.Architecture;

public sealed class PromotionArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "ValueObjects", "Events", "Policies", "Merchandising"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Checkout", "Merchandising", "Promotions", "Commands", "Queries", "Errors"];
    private static readonly string[] AllowedContractsFolders = ["Checkout", "Merchandising", "Pricing", "Errors"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Queries", "Adapters", "Events", "Messaging", "DependencyInjection"];

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

    private static string PromotionRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Promotion");

    [Fact]
    public void Domain_csproj_has_no_application_infrastructure_or_endpoints_reference()
    {
        var refs = ProjectRefs("Tooba.Promotion.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Offer.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Pricing.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Infrastructure_uses_contracts_not_foreign_application()
    {
        var refs = ProjectRefs("Tooba.Promotion.Infrastructure");
        Assert.DoesNotContain(refs, r => r.Contains("Offer.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Pricing.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Offer.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Pricing.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Offer.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Pricing.Contracts", StringComparison.Ordinal));
    }

    [Fact]
    public void Promotion_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(ProjectRefs("Tooba.Promotion.Domain"), x => x.Contains("Tooba.Promotion.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("OfferDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("PricingDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(Sources("Tooba.Promotion.Contracts"), x => x.Text.Contains("namespace Tooba.Promotion.Domain", StringComparison.Ordinal));

        var directory = File.ReadAllText(Path.Combine(PromotionRoot(), "Tooba.Promotion.Infrastructure", "Directories", "PromotionDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        var merch = File.ReadAllText(Path.Combine(PromotionRoot(), "Tooba.Promotion.Infrastructure", "Directories", "MerchandisingCampaignDirectory.cs"));
        Assert.DoesNotContain("?? new SystemUtcClock()", merch, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", merch, StringComparison.Ordinal);

        AssertNoRootDump("Tooba.Promotion.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Promotion.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Promotion.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Promotion.Infrastructure", AllowedInfrastructureFolders);
        AssertNamespacesAlign("Tooba.Promotion.Domain", "Tooba.Promotion.Domain");
        AssertNamespacesAlign("Tooba.Promotion.Application", "Tooba.Promotion.Application");
        AssertNamespacesAlign("Tooba.Promotion.Contracts", "Tooba.Promotion.Contracts");
        AssertNamespacesAlign("Tooba.Promotion.Infrastructure", "Tooba.Promotion.Infrastructure");
        Assert.True(Directory.Exists(Path.Combine(PromotionRoot(), "Tooba.Promotion.Endpoints")));
        var endpointSources = Sources("Tooba.Promotion.Endpoints").ToList();
        Assert.Contains(endpointSources, x => x.Text.Contains("ISender", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointSources, x => x.Text.Contains("IPromotionDirectory", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointSources, x => x.Text.Contains("PlatformHttpException", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointSources, x => x.Text.Contains("ex.Message", StringComparison.Ordinal));
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(hostRoot, "Promotion", "PromotionEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Promotion", "PromotionPanelComposer.cs")));

        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("PromotionDbContext", StringComparison.Ordinal))
            .ToList();
        Assert.Empty(hostHits);

        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
                        || x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                        || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal)
                        || x.Text.Contains("?? new SystemUtcClock()", StringComparison.Ordinal)
                        || x.Text.Contains("?? new UuidV7IdGenerator()", StringComparison.Ordinal)
                        || x.Text.Contains("?? new ModuleCallTracer()", StringComparison.Ordinal))
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
            .Where(x => !x.Msg.StartsWith("inventory.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("promotion.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("merchandising.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(PromotionRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj") continue;
            Assert.Contains(name, allowedFolders);
        }
    }

    private static void AssertNamespacesAlign(string projectFolder, string nsPrefix)
    {
        var violations = new List<string>();
        foreach (var (path, text) in Sources(projectFolder))
        {
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
            if (!ns.StartsWith(expected, StringComparison.Ordinal))
                violations.Add($"{path}: ns={ns} expectedPrefix={expected}");
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Promotion.Domain")
            .Concat(Sources("Tooba.Promotion.Application"))
            .Concat(Sources("Tooba.Promotion.Contracts"))
            .Concat(Sources("Tooba.Promotion.Infrastructure"))
            .Concat(Sources("Tooba.Promotion.Endpoints"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(PromotionRoot(), projectFolder);
        if (!Directory.Exists(root))
            yield break;
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
        var csproj = Path.Combine(PromotionRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
