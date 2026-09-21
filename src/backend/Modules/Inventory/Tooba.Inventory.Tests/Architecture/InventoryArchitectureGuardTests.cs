using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Inventory.Tests.Architecture;

public sealed class InventoryArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events", "Policies"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Checkout", "Orders"];
    private static readonly string[] AllowedContractsFolders = ["Seller", "Checkout", "Orders", "Availability", "Errors", "Fulfillment", "Returns"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Adapters", "Events", "Messaging", "DependencyInjection"];

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

    private static string InventoryRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Inventory");

    [Fact]
    public void Domain_csproj_has_no_application_infrastructure_or_endpoints_reference()
    {
        var refs = ProjectRefs("Tooba.Inventory.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Seller_inventory_write_returns_result_without_direct_semantic_exception()
    {
        var contracts = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Contracts", "Seller", "SellerOfferInventoryContracts.cs"));
        Assert.Contains("Task<Result> SetInventoryAsync", contracts, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Infrastructure", "Directories", "InventoryDirectory.cs"));
        var start = directory.IndexOf("public async Task<Result> SetInventoryAsync", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = directory.IndexOf("public async Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityBatchAsync", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var body = directory[start..end];
        Assert.Contains("Result.Failure", body, StringComparison.Ordinal);
        Assert.Contains("Result.Success()", body, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new SemanticException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(ProjectRefs("Tooba.Inventory.Domain"), x => x.Contains("Tooba.Inventory.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("OfferDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(Sources("Tooba.Inventory.Contracts"), x => x.Text.Contains("namespace Tooba.Inventory.Domain", StringComparison.Ordinal));
        var directory = File.ReadAllText(Path.Combine(InventoryRoot(), "Tooba.Inventory.Infrastructure", "Directories", "InventoryDirectory.cs"));
        Assert.Contains("IModuleCallTracer", directory, StringComparison.Ordinal);
        Assert.Contains("IOfferLookupGateway", directory, StringComparison.Ordinal);
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new ModuleCallTracer()", directory, StringComparison.Ordinal);

        AssertNoRootDump("Tooba.Inventory.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Inventory.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Inventory.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Inventory.Infrastructure", AllowedInfrastructureFolders);
        AssertNamespacesAlign("Tooba.Inventory.Domain", "Tooba.Inventory.Domain");
        AssertNamespacesAlign("Tooba.Inventory.Application", "Tooba.Inventory.Application");
        AssertNamespacesAlign("Tooba.Inventory.Contracts", "Tooba.Inventory.Contracts");
        AssertNamespacesAlign("Tooba.Inventory.Infrastructure", "Tooba.Inventory.Infrastructure");

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("InventoryDbContext", StringComparison.Ordinal))
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

    [Fact]
    public void Checkout_release_adapter_has_no_silent_catch_and_directory_release_is_idempotent()
    {
        var adapter = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Application", "Checkout", "CheckoutInventoryReservationAdapter.cs"));
        Assert.DoesNotContain("catch (InvalidOperationException)", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception)", adapter, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Infrastructure", "Directories", "InventoryDirectory.cs"));
        var start = directory.IndexOf("public async Task ReleaseAsync(Guid reservationId", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = directory.IndexOf("public async Task ConsumeAsync", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var body = directory[start..end];
        Assert.Contains("StockReservationStatus.Released or StockReservationStatus.Consumed", body, StringComparison.Ordinal);
        Assert.Contains("return;", body, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", body, StringComparison.Ordinal);
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(InventoryRoot(), project);
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
        Sources("Tooba.Inventory.Domain")
            .Concat(Sources("Tooba.Inventory.Application"))
            .Concat(Sources("Tooba.Inventory.Contracts"))
            .Concat(Sources("Tooba.Inventory.Infrastructure"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(InventoryRoot(), projectFolder);
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
        var csproj = Path.Combine(InventoryRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
