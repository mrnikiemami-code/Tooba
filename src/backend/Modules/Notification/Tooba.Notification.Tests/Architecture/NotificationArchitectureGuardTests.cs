using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Notification.Tests.Architecture;

public sealed class NotificationArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events", "Policies"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Commands", "Queries", "Rendering", "Services"];
    private static readonly string[] AllowedContractsFolders = ["Commands", "Dtos", "Ports", "Copy", "Routes", "Events"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Projectors", "Handlers", "Observability", "Messaging", "DependencyInjection", "Migrations"];

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

    private static string NotificationRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Notification");

    [Fact]
    public void Domain_has_no_contracts_application_infrastructure_or_host_refs()
    {
        var refs = ProjectRefs("Tooba.Notification.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Contracts_has_no_domain_application_infrastructure_or_host_refs()
    {
        var refs = ProjectRefs("Tooba.Notification.Contracts");
        Assert.DoesNotContain(refs, r => r.Contains("Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Application_has_no_foreign_application_or_infrastructure()
    {
        var refs = ProjectRefs("Tooba.Notification.Application");
        Assert.DoesNotContain(refs, r => r.Contains("Payment.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Fulfillment.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Returns.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Infrastructure_uses_public_contracts_not_foreign_application()
    {
        var refs = ProjectRefs("Tooba.Notification.Infrastructure");
        Assert.Contains(refs, r => r.Contains("Payment.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Fulfillment.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Returns.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Order.Contracts", StringComparison.Ordinal));

        Assert.DoesNotContain(refs, r => r.Contains("Order.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Order.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Order.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Fulfillment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Returns.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Fulfillment.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Returns.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Fulfillment.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Returns.Infrastructure", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Order.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("OrderDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("PaymentDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("FulfillmentDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("ReturnsDbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void Order_Contracts_notification_surface_is_clean()
    {
        var orderContractsRoot = Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Contracts");
        Assert.True(Directory.Exists(orderContractsRoot));

        var csproj = Path.Combine(orderContractsRoot, "Tooba.Order.Contracts.csproj");
        var doc = XDocument.Load(csproj);
        var refs = doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
        Assert.DoesNotContain(refs, r => r.Contains("Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));

        var rootCs = Directory.EnumerateFiles(orderContractsRoot, "*.cs", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToArray();
        Assert.True(rootCs.Length == 0, "Order.Contracts root dumping-ground: " + string.Join(", ", rootCs));

        var sources = Directory.EnumerateFiles(orderContractsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                var n = f.Replace('\\', '/');
                return !n.Contains("/bin/", StringComparison.Ordinal) && !n.Contains("/obj/", StringComparison.Ordinal);
            })
            .Select(f => (Path: Path.GetRelativePath(RepoRoot(), f), Text: File.ReadAllText(f)))
            .ToArray();

        Assert.DoesNotContain(sources, x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.Contains(sources, x => x.Path.Replace('\\', '/').Contains("/Notifications/", StringComparison.Ordinal));
        Assert.All(sources, x =>
        {
            var ns = Regex.Match(x.Text, @"^namespace\s+([\w.]+)", RegexOptions.Multiline).Groups[1].Value;
            Assert.StartsWith("Tooba.Order.Contracts.Notifications", ns, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Notification_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("PaymentDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(Sources("Tooba.Notification.Contracts"), x => x.Text.Contains("namespace Tooba.Notification.Domain", StringComparison.Ordinal));

        var directory = File.ReadAllText(Path.Combine(NotificationRoot(), "Tooba.Notification.Infrastructure", "Directories", "NotificationDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        AssertNoRootDump("Tooba.Notification.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Notification.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Notification.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Notification.Infrastructure", AllowedInfrastructureFolders);
        AssertNamespacesAlign("Tooba.Notification.Domain", "Tooba.Notification.Domain");
        AssertNamespacesAlign("Tooba.Notification.Application", "Tooba.Notification.Application");
        AssertNamespacesAlign("Tooba.Notification.Contracts", "Tooba.Notification.Contracts");
        AssertNamespacesAlign("Tooba.Notification.Infrastructure", "Tooba.Notification.Infrastructure");

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
            .Where(x => !x.Msg.StartsWith("notification.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(NotificationRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj" or "artifacts" || name.StartsWith('.')) continue;
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

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Notification.Domain")
            .Concat(Sources("Tooba.Notification.Application"))
            .Concat(Sources("Tooba.Notification.Contracts"))
            .Concat(Sources("Tooba.Notification.Infrastructure"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(NotificationRoot(), projectFolder);
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
        var csproj = Path.Combine(NotificationRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
