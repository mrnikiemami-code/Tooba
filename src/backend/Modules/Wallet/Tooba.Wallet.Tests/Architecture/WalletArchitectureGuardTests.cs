using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Wallet.Tests.Architecture;

public sealed class WalletArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events", "Policies"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Commands", "Queries", "Handlers", "Payments", "Refunds"];
    private static readonly string[] AllowedContractsFolders = ["Payments", "Refunds", "Dtos", "Ports", "Errors"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Adapters", "Events", "Messaging", "DependencyInjection", "Migrations"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "WalletDevelopmentSeedHost.cs",
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

        throw new InvalidOperationException("repo root not found");
    }

    private static string WalletRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Wallet");

    [Fact]
    public void Domain_csproj_has_no_application_infrastructure_or_endpoints_reference()
    {
        var refs = ProjectRefs("Tooba.Wallet.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Order.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Returns.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Application_does_not_depend_on_foreign_application_or_infrastructure()
    {
        var refs = ProjectRefs("Tooba.Wallet.Application");
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Wallet.Contracts", StringComparison.Ordinal));
    }

    [Fact]
    public void Cross_module_payment_refund_ports_live_in_Contracts()
    {
        Assert.True(File.Exists(Path.Combine(WalletRoot(), "Tooba.Wallet.Contracts", "Payments", "WalletOrderPaymentPort.cs")));
        Assert.True(File.Exists(Path.Combine(WalletRoot(), "Tooba.Wallet.Contracts", "Refunds", "WalletRefundCreditPort.cs")));
        Assert.DoesNotContain(
            Sources("Tooba.Wallet.Application"),
            x => x.Text.Contains("interface IWalletOrderPaymentPort", StringComparison.Ordinal)
                 || x.Text.Contains("interface IWalletRefundCreditPort", StringComparison.Ordinal));
    }

    [Fact]
    public void Wallet_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(ProjectRefs("Tooba.Wallet.Domain"), x => x.Contains("Tooba.Wallet.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("PaymentDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(Sources("Tooba.Wallet.Contracts"), x => x.Text.Contains("namespace Tooba.Wallet.Domain", StringComparison.Ordinal));

        var directory = File.ReadAllText(Path.Combine(WalletRoot(), "Tooba.Wallet.Infrastructure", "Directories", "WalletDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        AssertNoRootDump("Tooba.Wallet.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Wallet.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Wallet.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Wallet.Infrastructure", AllowedInfrastructureFolders);
        AssertNamespacesAlign("Tooba.Wallet.Domain", "Tooba.Wallet.Domain");
        AssertNamespacesAlign("Tooba.Wallet.Application", "Tooba.Wallet.Application");
        AssertNamespacesAlign("Tooba.Wallet.Contracts", "Tooba.Wallet.Contracts");
        AssertNamespacesAlign("Tooba.Wallet.Infrastructure", "Tooba.Wallet.Infrastructure");

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("WalletDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host WalletDbContext production authority: " + string.Join("; ", hostHits));

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
            .Where(x => !x.Msg.StartsWith("wallet.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(WalletRoot(), project);
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
        Sources("Tooba.Wallet.Domain")
            .Concat(Sources("Tooba.Wallet.Application"))
            .Concat(Sources("Tooba.Wallet.Contracts"))
            .Concat(Sources("Tooba.Wallet.Infrastructure"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(WalletRoot(), projectFolder);
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
        var csproj = Path.Combine(WalletRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
